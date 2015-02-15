// COPYRIGHT 2012, 2013 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Orts.Formats.Msts;
using ORTS.Common;

namespace ORTS
{
	public class HazzardManager
	{
		readonly int hornDist = 200;
		readonly int approachDist = 160;
		readonly Simulator Simulator;
		public readonly Dictionary<int, Hazzard> Hazzards;
		public readonly Dictionary<int, Hazzard> CurrentHazzards;
		public readonly Dictionary<string, HAZFile> HazFiles;
		List<int> InterestedHazzards;//those hazards is closed to player, needs to listen to horn
		public HazzardManager(Simulator simulator)
		{
			Simulator = simulator;
			InterestedHazzards = new List<int>();
			CurrentHazzards = new Dictionary<int, Hazzard>();
			HazFiles = new Dictionary<string, HAZFile>();
			Hazzards = simulator.TDB != null && simulator.TDB.TrackDB != null ? GetHazardsFromDB(simulator.TDB.TrackDB.TrackNodes, simulator.TDB.TrackDB.TrItemTable) : new Dictionary<int, Hazzard>();
		}

		static Dictionary<int, Hazzard> GetHazardsFromDB(TrackNode[] trackNodes, TrItem[] trItemTable)
		{
			return (from trackNode in trackNodes
					where trackNode != null && trackNode.TrVectorNode != null && trackNode.TrVectorNode.NoItemRefs > 0
					from itemRef in trackNode.TrVectorNode.TrItemRefs.Distinct()
					where trItemTable[itemRef] != null && trItemTable[itemRef].ItemType == TrItem.trItemType.trHAZZARD
					select new KeyValuePair<int, Hazzard>(itemRef, new Hazzard(trackNode, trItemTable[itemRef])))
					.ToDictionary(_ => _.Key, _ => _.Value);
		}

		[CallOnThread("Loader")]
		public Hazzard AddHazzardIntoGame(int itemID, string hazFileName)
		{
			try
			{
				if (!CurrentHazzards.ContainsKey(itemID))
				{
					if (HazFiles.ContainsKey(hazFileName)) Hazzards[itemID].HazFile = HazFiles[hazFileName];
					else
					{
						var hazF = new HAZFile(Simulator.RoutePath + "\\" + hazFileName);
						HazFiles.Add(hazFileName, hazF);
						Hazzards[itemID].HazFile = hazF;
					}
					//based on act setting for frequency
					if (Hazzards[itemID].animal == true && Simulator.Activity != null)
					{
						if (Program.Random.Next(100) > Simulator.Activity.Tr_Activity.Tr_Activity_Header.Animals) return null;
					}
					else if (Simulator.Activity != null)
					{
						if (Program.Random.Next(100) > Simulator.Activity.Tr_Activity.Tr_Activity_Header.Animals) return null;
					}
					else //in explore mode
					{
						if (Hazzards[itemID].animal == false) return null;//not show worker in explore mode
						if (Program.Random.Next(100) > 20) return null;//show 10% animals
					}
					CurrentHazzards.Add(itemID, Hazzards[itemID]);
					return Hazzards[itemID];//successfully added the hazard with associated haz file
				}
			}
			catch { }
			return null;
		}

		public void RemoveHazzardFromGame(int itemID)
		{
			try
			{
				if (CurrentHazzards.ContainsKey(itemID))
				{
					CurrentHazzards.Remove(itemID);
				}
			}
			catch { };
		}

		[CallOnThread("Updater")]
		public void Update(float elapsedClockSeconds)
		{
			var playerLocation = Simulator.PlayerLocomotive.WorldPosition.WorldLocation;

			foreach (var haz in Hazzards)
			{
				haz.Value.Update(playerLocation, approachDist);
			}
		}

		public void Horn()
		{
			var playerLocation = Simulator.PlayerLocomotive.WorldPosition.WorldLocation;
			foreach (var haz in Hazzards)
			{
				if (WorldLocation.Within(haz.Value.Location, playerLocation, hornDist))
				{
					haz.Value.state = Hazzard.State.LookLeft;
				}
			}
		}
	}

	public class Hazzard
	{
        readonly TrackNode TrackNode;

        internal WorldLocation Location;
		public HAZFile HazFile { get { return hazF; } set { hazF = value; if (hazF.Tr_HazardFile.Workers != null) animal = false; else animal = true; } }
		public HAZFile hazF;
		public enum State { Idle1, Idle2, LookLeft, LookRight, Scared };
		public State state;
		public bool animal = true;

		public Hazzard(TrackNode trackNode, TrItem trItem)
        {
            TrackNode = trackNode;
            Location = new WorldLocation(trItem.TileX, trItem.TileZ, trItem.X, trItem.Y, trItem.Z);
			state = State.Idle1;
        }

		public void Update(WorldLocation playerLocation, int approachDist)
		{
			if (state == State.Idle1)
			{
				if (Program.Random.Next(10) == 0) state = State.Idle2;
			}
			else if (state == State.Idle2)
			{
				if (Program.Random.Next(5) == 0) state = State.Idle1;
			}

			if (WorldLocation.Within(Location, playerLocation, approachDist) && state < State.LookLeft)
			{
				state = State.LookRight;
			}

		}
	}
}
