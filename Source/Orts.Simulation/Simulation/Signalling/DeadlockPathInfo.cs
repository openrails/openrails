// COPYRIGHT 2021 by the Open Rails project.
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

using System;
using System.Collections.Generic;
using System.IO;
using Orts.Simulation.Physics;

namespace Orts.Simulation.Signalling
{
    public class DeadlockPathInfo
    {
        public Train.TCSubpathRoute Path;      // actual path
        public string Name;                    // name of path
        public List<string> Groups;            // groups of which this path is a part
        public float UsefullLength;            // path usefull length
        public int EndSectionIndex;            // index of linked end section
        public int LastUsefullSectionIndex;    // Index in Path for last section which can be used before stop position
        public List<int> AllowedTrains;        // list of train for which path is valid (ref. is train/subpath index); -1 indicates public path

        public DeadlockPathInfo(Train.TCSubpathRoute thisPath, int pathIndex)
        {
            Path = new Train.TCSubpathRoute(thisPath);
            Name = String.Empty;
            Groups = new List<string>();

            UsefullLength = 0.0f;
            EndSectionIndex = -1;
            LastUsefullSectionIndex = -1;
            AllowedTrains = new List<int>();

            Path[0].UsedAlternativePath = pathIndex;
        }

        /// <summary>
        /// Constructor for restore
        /// </summary>
        public DeadlockPathInfo(BinaryReader inf)
        {
            Path = new Train.TCSubpathRoute(inf);
            Name = inf.ReadString();

            Groups = new List<string>();
            int totalGroups = inf.ReadInt32();
            for (int iGroup = 0; iGroup <= totalGroups - 1; iGroup++)
            {
                string thisGroup = inf.ReadString();
                Groups.Add(thisGroup);
            }

            UsefullLength = inf.ReadSingle();
            EndSectionIndex = inf.ReadInt32();
            LastUsefullSectionIndex = inf.ReadInt32();

            AllowedTrains = new List<int>();
            int totalIndex = inf.ReadInt32();
            for (int iIndex = 0; iIndex <= totalIndex - 1; iIndex++)
            {
                int thisIndex = inf.ReadInt32();
                AllowedTrains.Add(thisIndex);
            }
        }

        public void Save(BinaryWriter outf)
        {
            Path.Save(outf);
            outf.Write(Name);

            outf.Write(Groups.Count);
            foreach (string groupName in Groups)
            {
                outf.Write(groupName);
            }

            outf.Write(UsefullLength);
            outf.Write(EndSectionIndex);
            outf.Write(LastUsefullSectionIndex);

            outf.Write(AllowedTrains.Count);
            foreach (int thisIndex in AllowedTrains)
            {
                outf.Write(thisIndex);
            }
        }
    }
}
