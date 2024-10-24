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
using ORTS.Common;
using ORTS.Scripting.Api;

namespace Orts.Simulation.RollingStocks.SubSystems.Controllers
{
    public class PowerLimitationSelectorPosition
    {
        public string Name;
        public float PowerW;

        public PowerLimitationSelectorPosition(string name, float powerW)
        {
            Name = name;
            PowerW = powerW;
        }
    }

    public class ScriptedPowerLimitationSelector : ISubSystem<ScriptedPowerLimitationSelector>
    {
        public ScriptedLocomotivePowerSupply PowerSupply;
        public MSTSLocomotive Locomotive => PowerSupply.Locomotive;
        public Simulator Simulator => Locomotive.Simulator;

        /// <summary>
        /// List of all selector positions
        /// </summary>
        public List<PowerLimitationSelectorPosition> Positions = new List<PowerLimitationSelectorPosition>();
        /// <summary>
        /// Current position of the selector
        /// </summary>
        public PowerLimitationSelectorPosition Position;

        public int PositionId => Positions.IndexOf(Position);

        /// <summary>
        /// Relative path to the script from the script directory of the locomotive
        /// </summary>
        public string ScriptName;

        public PowerLimitationSelector Script;

        public ScriptedPowerLimitationSelector(ScriptedLocomotivePowerSupply powerSupply)
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
                            float powerW = float.MaxValue;
                            bool defaultPosition = false;

                            stf.MustMatch("(");
                            stf.ParseBlock(new[]
                            {
                                new STFReader.TokenProcessor("name", () => name = stf.ReadStringBlock(null)),
                                new STFReader.TokenProcessor("maxpower", () => powerW = stf.ReadFloatBlock(STFReader.UNITS.Power, float.MaxValue)),
                                new STFReader.TokenProcessor("default", () => defaultPosition = Convert.ToBoolean(stf.ReadBoolBlock(false))),
                            });

                            if (name != null && !Positions.Exists(x => x.Name == name))
                            {
                                var pos = new PowerLimitationSelectorPosition(name, powerW);
                                Positions.Add(pos);
                                if (defaultPosition) Position = pos;
                            }
                            else
                            {
                                Trace.TraceWarning($"Ignored power limitation selector position with name {name}");
                            }
                        }),
                    });
                }),
            });
        }

        public void Copy(ScriptedPowerLimitationSelector other)
        {
            foreach (PowerLimitationSelectorPosition position in other.Positions)
            {
                Positions.Add(position);
            }
            Position = other.Position;

            ScriptName = other.ScriptName;
        }

        public void Initialize()
        {
            if (Position == null) Position = Positions.FirstOrDefault();

            if (ScriptName == "Default")
            {
                Script = new DefaultPowerLimitationSelector();
            }
            else if (Script == null && ScriptName != null)
            {
                string[] pathArray = { Path.Combine(Path.GetDirectoryName(Locomotive.WagFilePath), "Script") };
                Script = Simulator.ScriptManager.Load(pathArray, ScriptName) as PowerLimitationSelector;
            }

            if (Script == null) Script = new DefaultPowerLimitationSelector();

            Script?.AttachToHost(this);
            Script?.Initialize();
        }

        public void InitializeMoving()
        {
            Script?.InitializeMoving();
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write(Position?.Name);
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
    public class DefaultPowerLimitationSelector : PowerLimitationSelector
    {
        public override void HandleEvent(PowerSupplyEvent evt)
        {
            switch (evt)
            {
                case PowerSupplyEvent.IncreasePowerLimitationSelectorPosition:
                    IncreasePosition();
                    break;

                case PowerSupplyEvent.DecreasePowerLimitationSelectorPosition:
                    DecreasePosition();
                    break;
            }
        }
        string GetPositionName()
        {
            if (Position == null) return "";
            if (string.IsNullOrEmpty(Position.Name)) return FormatStrings.FormatPower(Position.PowerW, true, false, false);
            return Position.Name;
        }

        private void IncreasePosition()
        {
            if (Positions.Count < 2) return;

            int index = Positions.IndexOf(Position);

            if (index < Positions.Count - 1)
            {
                index++;

                Position = Positions[index];

                SignalEvent(Event.PowerLimitationSelectorIncrease);
                Message(CabControl.None, Simulator.Catalog.GetStringFmt("Power selector changed to {0}", GetPositionName()));
            }
        }

        private void DecreasePosition()
        {
            if (Positions.Count < 2) return;

            int index = Positions.IndexOf(Position);

            if (index > 0)
            {
                index--;

                Position = Positions[index];

                SignalEvent(Event.PowerLimitationSelectorDecrease);
                Message(CabControl.None, Simulator.Catalog.GetStringFmt("Power selector changed to {0}", GetPositionName()));
            }
        }
    }
}
