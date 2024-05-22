// COPYRIGHT 2022 by the Open Rails project.
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using Orts.Common;
using Orts.Parsers.Msts;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using ORTS.Scripting.Api;

namespace Orts.Simulation.RollingStocks.SubSystems.Controllers
{
    public class PantographSelectorPosition
    {
        public string Name;

        public PantographSelectorPosition(string name)
        {
            Name = name;
        }
    }

    public class ScriptedPantographSelector : ISubSystem<ScriptedPantographSelector>
    {
        public ScriptedLocomotivePowerSupply PowerSupply;
        public MSTSLocomotive Locomotive => PowerSupply.Locomotive;
        public Simulator Simulator => Locomotive.Simulator;

        /// <summary>
        /// List of all selector positions
        /// </summary>
        public List<PantographSelectorPosition> Positions = new List<PantographSelectorPosition>();
        /// <summary>
        /// Current position of the selector
        /// </summary>
        public PantographSelectorPosition Position;

        public int PositionId => Positions.IndexOf(Position);

        /// <summary>
        /// Relative path to the script from the script directory of the locomotive
        /// </summary>
        public string ScriptName;

        public PantographSelector Script;

        public ScriptedPantographSelector(ScriptedLocomotivePowerSupply powerSupply)
        {
            PowerSupply = powerSupply;
        }

        public void Parse(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new[]
            {
                new STFReader.TokenProcessor("script", () => { ScriptName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("selectorpositions", () =>
                {
                    stf.MustMatch("(");
                    stf.ParseBlock(new []
                    {
                        new STFReader.TokenProcessor("selectorposition", () =>
                        {
                            string name = null;
                            bool defaultPosition = false;

                            stf.MustMatch("(");
                            stf.ParseBlock(new[]
                            {
                                new STFReader.TokenProcessor("name", () => name = stf.ReadStringBlock(null)),
                                new STFReader.TokenProcessor("default", () => defaultPosition = Convert.ToBoolean(stf.ReadBoolBlock(false))),
                            });

                            if (name != null && !Positions.Exists(x => x.Name == name))
                            {
                                var pos = new PantographSelectorPosition(name);
                                Positions.Add(pos);
                                if (defaultPosition) Position = pos;
                            }
                            else
                            {
                                Trace.TraceWarning($"Ignored pantograph selector position with name {name}");
                            }
                        }),
                    });
                }),
            });
        }

        public void Copy(ScriptedPantographSelector other)
        {
            foreach (PantographSelectorPosition position in other.Positions)
            {
                Positions.Add(position);
            }

            ScriptName = other.ScriptName;
        }

        public void Initialize()
        {
            if (Position == null) Position = Positions.FirstOrDefault();

            if (ScriptName == "Default")
            {
                Script = new DefaultPantographSelector(false);
            }
            else if (ScriptName == "Circular")
            {
                Script = new DefaultPantographSelector(true);
            }
            else if (Script == null && ScriptName != null)
            {
                string[] pathArray = { Path.Combine(Path.GetDirectoryName(Locomotive.WagFilePath), "Script") };
                Script = Simulator.ScriptManager.Load(pathArray, ScriptName) as PantographSelector;
            }

            if (Script == null) Script = new DefaultPantographSelector(false);

            Script?.AttachToHost(this);
            Script?.Initialize();
        }

        public void InitializeMoving()
        {
            Script?.InitializeMoving();
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write(Position.Name);
        }

        public void Restore(BinaryReader inf)
        {
            string name = inf.ReadString();
            Position = Positions.FirstOrDefault(x => x.Name == name) ?? Positions.FirstOrDefault();
        }

        public void Update(float elapsedClockSeconds)
        {
            Script?.Update(elapsedClockSeconds);
        }

        public void HandleEvent(PowerSupplyEvent evt)
        {
            Script?.HandleEvent(evt);
        }

        public void HandleEvent(PowerSupplyEvent evt, int id)
        {
            Script?.HandleEvent(evt, id);
        }
    }

    public class DefaultPantographSelector : PantographSelector
    {
        public readonly bool IsCircular;
        public DefaultPantographSelector(bool circular)
        {
            IsCircular = circular;
        }
        public override void HandleEvent(PowerSupplyEvent evt)
        {
            switch (evt)
            {
                case PowerSupplyEvent.IncreasePantographSelectorPosition:
                    IncreasePosition();
                    break;

                case PowerSupplyEvent.DecreasePantographSelectorPosition:
                    DecreasePosition();
                    break;
            }
        }
        private void IncreasePosition()
        {
            if (Positions.Count < 2) return;

            int index = Positions.IndexOf(Position);
            int newIndex = index;

            if (index < Positions.Count - 1) newIndex++;
            else if (IsCircular) newIndex = 0;

            if (index != newIndex)
            {
                Position = Positions[newIndex];

                SignalEvent(Event.PantographSelectorIncrease);
                Message(CabControl.None, Simulator.Catalog.GetStringFmt("Pantograph selector changed to {0}", Position.Name));
            }
        }

        private void DecreasePosition()
        {
            if (Positions.Count < 2) return;

            int index = Positions.IndexOf(Position);
            int newIndex = index;

            if (index > 0) newIndex--;
            else if (IsCircular) newIndex = Positions.Count - 1;

            if (index != newIndex)
            {
                Position = Positions[newIndex];

                SignalEvent(Event.PantographSelectorDecrease);
                Message(CabControl.None, Simulator.Catalog.GetStringFmt("Pantograph selector changed to {0}", Position.Name));
            }
        }
    }
}
