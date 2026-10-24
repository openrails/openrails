// COPYRIGHT 2020 by the Open Rails project.
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
using System.Linq;
using Orts.Parsers.Msts;
using Orts.Simulation.Physics;
using ORTS.Scripting.Api;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{

    public abstract class ScriptedLocomotivePowerSupply : ScriptedPowerSupply, ILocomotivePowerSupply
    {
        public LocomotivePowerSupply LocomotiveScript => AbstractScript as LocomotivePowerSupply;
        public MSTSLocomotive Locomotive => Car as MSTSLocomotive;

        public MasterKey MasterKey { get; protected set; }
        public ElectricTrainSupplySwitch ElectricTrainSupplySwitch { get; protected set; }

        public abstract PowerSupplyType Type { get; }
        protected string ScriptName = "Default";
        public string ParametersFileName { get; protected set; }

        public PowerSupplyState MainPowerSupplyState { get; set; } = PowerSupplyState.PowerOff;
        public bool MainPowerSupplyOn => MainPowerSupplyState == PowerSupplyState.PowerOn;
        public float MaximumPowerW;
        public float AvailableTractionPowerW = float.MaxValue;
        public bool DynamicBrakeAvailable { get; set; } = false;
        public float PowerSupplyDynamicBrakePercent { get; set; } = -1;
        public float MaximumDynamicBrakePowerW { get; set; } = 0;
        public float MaxThrottlePercent { get; set; } = 100;
        public float ThrottleReductionPercent { get; set; } = 0;

        public PowerSupplyState CabPowerSupplyState { get; set; } = PowerSupplyState.PowerOff;
        public bool CabPowerSupplyOn => CabPowerSupplyState == PowerSupplyState.PowerOn;

        public float PowerOnDelayS { get; protected set; } = 0f;
        public float AuxPowerOnDelayS { get; protected set; } = 0f;
        public float AuxContinuousPowerW { get; protected set; } = 0f;

        public bool ServiceRetentionButton { get; set; } = false;
        public bool ServiceRetentionCancellationButton { get; set; } = false;
        public bool ServiceRetentionActive { get; set; } = false;
        public Dictionary<int, float> CabDisplayControls = new Dictionary<int, float>();

        // generic power supply commands
        public Dictionary<int, bool> PowerSupplyCommandButtonDown = new Dictionary<int, bool>();
        public Dictionary<int, bool> PowerSupplyCommandSwitchOn = new Dictionary<int, bool>();
        // List of customized control strings;
        public Dictionary<int, string> CustomizedCabviewControlNames = new Dictionary<int, string>();

        protected ScriptedLocomotivePowerSupply(MSTSLocomotive locomotive) : base(locomotive)
        {
            ElectricTrainSupplyDirection = PowerSupplyDirection.Source;

            MasterKey = new MasterKey(Locomotive);
            ElectricTrainSupplySwitch = new ElectricTrainSupplySwitch(Locomotive);
        }

        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortspowersupply":
                    ScriptName = stf.ReadStringBlock(null);
                    break;

                case "engine(ortspowersupplyparameters":
                    ParametersFileName = stf.ReadStringBlock(null);
                    break;

                case "engine(ortspowerondelay":
                    PowerOnDelayS = stf.ReadFloatBlock(STFReader.UNITS.Time, null);
                    break;

                case "engine(ortsauxpowerondelay":
                    AuxPowerOnDelayS = stf.ReadFloatBlock(STFReader.UNITS.Time, null);
                    break;
                
                case "engine(ortsauxcontinuouspower":
                    AuxContinuousPowerW = stf.ReadFloatBlock(STFReader.UNITS.Power, null);
                    break;

                case "engine(ortsbattery":
                    Battery.Parse(lowercasetoken, stf);
                    break;
                case "engine(ortsmasterkey(mode":
                case "engine(ortsmasterkey(delayoff":
                case "engine(ortsmasterkey(headlightcontrol":
                    MasterKey.Parse(lowercasetoken, stf);
                    break;

                case "engine(ortselectrictrainsupply(mode":
                    ElectricTrainSupplySwitch.Parse(lowercasetoken, stf);
                    break;
            }
        }

        public override void Copy(IPowerSupply other)
        {
            base.Copy(other);
            if (other is ScriptedLocomotivePowerSupply scriptedOther)
            {
                MasterKey.Copy(scriptedOther.MasterKey);
                ElectricTrainSupplySwitch.Copy(scriptedOther.ElectricTrainSupplySwitch);

                ScriptName = scriptedOther.ScriptName;
                ParametersFileName = scriptedOther.ParametersFileName;

                PowerOnDelayS = scriptedOther.PowerOnDelayS;
                AuxPowerOnDelayS = scriptedOther.AuxPowerOnDelayS;
                AuxContinuousPowerW = scriptedOther.AuxContinuousPowerW;
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            MasterKey.Initialize();
            ElectricTrainSupplySwitch.Initialize();
        }

        /// <summary>
        /// Initialization when simulation starts with moving train
        /// <\summary>
        public override void InitializeMoving()
        {
            MasterKey.InitializeMoving();
            ElectricTrainSupplySwitch.InitializeMoving();

            MainPowerSupplyState = PowerSupplyState.PowerOn;
            if (Locomotive.IsLeadLocomotive())
            {
                CabPowerSupplyState = PowerSupplyState.PowerOn;
            }
            base.InitializeMoving();
        }

        public override void Save(BinaryWriter outf)
        {
            base.Save(outf);

            MasterKey.Save(outf);
            ElectricTrainSupplySwitch.Save(outf);
    
            outf.Write(MainPowerSupplyState.ToString());
            outf.Write(CabPowerSupplyState.ToString());
        }

        public override void Restore(BinaryReader inf)
        {
            base.Restore(inf);

            MasterKey.Restore(inf);
            ElectricTrainSupplySwitch.Restore(inf);

            MainPowerSupplyState = (PowerSupplyState)Enum.Parse(typeof(PowerSupplyState), inf.ReadString());
            CabPowerSupplyState = (PowerSupplyState)Enum.Parse(typeof(PowerSupplyState), inf.ReadString());
        }

        public override void Update(float elapsedClockSeconds)
        {
            base.Update(elapsedClockSeconds);

            MasterKey.Update(elapsedClockSeconds);
            ElectricTrainSupplySwitch.Update(elapsedClockSeconds);
        }

        public PowerSupplyState GetPowerStatus() => LocomotiveScript?.GetPowerStatus() ?? PowerSupplyState.Unavailable;

        public void HandleEventFromControlCar(PowerSupplyEvent evt)
        {
            LocomotiveScript?.HandleEventFromControlCar(evt);
        }

        public void HandleEventFromControlCar(PowerSupplyEvent evt, int id)
        {
            LocomotiveScript?.HandleEventFromControlCar(evt, id);
        }

        public void HandleEventFromTcs(PowerSupplyEvent evt)
        {
            LocomotiveScript?.HandleEventFromTcs(evt);
        }

        public void HandleEventFromTcs(PowerSupplyEvent evt, int id)
        {
            LocomotiveScript?.HandleEventFromTcs(evt, id);
        }

        public void HandleEventFromTcs(PowerSupplyEvent evt, string message)
        {
            LocomotiveScript?.HandleEventFromTcs(evt, message);
        }

        public void HandleEventFromOtherLocomotive(int locoIndex, PowerSupplyEvent evt)
        {
            LocomotiveScript?.HandleEventFromOtherLocomotive(locoIndex, evt);
        }

        public void HandleEventFromOtherLocomotive(int locoIndex, PowerSupplyEvent evt, int id)
        {
            LocomotiveScript?.HandleEventFromOtherLocomotive(locoIndex, evt, id);
        }
        
        // Converts the generic string (e.g. ORTS_POWER_SUPPLY5) shown when browsing with the mouse on a PowerSupply control
        // to a customized string defined in the script
        public string GetDisplayString(int commandIndex)
        {
            if (CustomizedCabviewControlNames.TryGetValue(commandIndex - 1, out string name)) return name;
            return "ORTS_POWER_SUPPLY"+commandIndex;
        }
    }

}
