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

using System.IO;
using Orts.Parsers.Msts;
using ORTS.Scripting.Api;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{
    /// <summary>
    /// Basic power supply class for steam locomotives
    /// For electrical systems powered by battery
    /// </summary>
    public class SteamPowerSupply : ILocomotivePowerSupply
    {
        public TrainCar Car { get; }
        public MSTSSteamLocomotive Locomotive => Car as MSTSSteamLocomotive;
        public PowerSupplyType Type => PowerSupplyType.Steam;

        public Pantographs Pantographs => Locomotive.Pantographs;
        public BatterySwitch BatterySwitch { get; protected set; }
        public MasterKey MasterKey { get; protected set; }
        public ElectricTrainSupplySwitch ElectricTrainSupplySwitch => null;

        public PowerSupplyState MainPowerSupplyState
        {
            get
            {
                return PowerSupplyState.PowerOn;
            }
            set{}
        }

        public bool MainPowerSupplyOn => true;
        public bool DynamicBrakeAvailable
        {
            get
            {
                return false;
            }
            set{}
        }

        public PowerSupplyState AuxiliaryPowerSupplyState
        {
            get
            {
                return PowerSupplyState.PowerOn;
            }
            set{}
        }

        public bool AuxiliaryPowerSupplyOn => true;

        public PowerSupplyState LowVoltagePowerSupplyState
        {
            get
            {
                return BatterySwitch.On ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff;
            }
            set{}
        }

        public bool LowVoltagePowerSupplyOn => LowVoltagePowerSupplyState == PowerSupplyState.PowerOn;

        public PowerSupplyState BatteryState
        {
            get
            {
                return BatterySwitch.On ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff;
            }
            set{}
        }

        public bool BatteryOn => BatteryState == PowerSupplyState.PowerOn;

        public PowerSupplyState CabPowerSupplyState
        {
            get
            {
                return MasterKey.On ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff;
            }
            set{}
        }

        public bool CabPowerSupplyOn => CabPowerSupplyState == PowerSupplyState.PowerOn;

        public PowerSupplyState ElectricTrainSupplyState
        {
            get
            {
                return PowerSupplyState.Unavailable;
            }
            set{}
        }

        public bool ElectricTrainSupplyOn => false;
        public bool FrontElectricTrainSupplyCableConnected { get => false; set { } }
        public float ElectricTrainSupplyPowerW => 0f;

        public bool ServiceRetentionButton
        {
            get
            {
                return false;
            }
            set{}
        }

        public bool ServiceRetentionCancellationButton
        {
            get
            {
                return false;
            }
            set{}
        }

        public bool ServiceRetentionActive
        {
            get
            {
                return false;
            }
            set{}
        }

        public SteamPowerSupply(MSTSSteamLocomotive locomotive)
        {
            Car = locomotive;

            BatterySwitch = new BatterySwitch(Locomotive);
            MasterKey = new MasterKey(Locomotive);
        }

        public virtual void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortsbattery(mode":
                case "engine(ortsbattery(delay":
                case "engine(ortsbattery(defaulton":
                    BatterySwitch.Parse(lowercasetoken, stf);
                    break;
                case "engine(ortsmasterkey(mode":
                case "engine(ortsmasterkey(delayoff":
                case "engine(ortsmasterkey(headlightcontrol":
                    MasterKey.Parse(lowercasetoken, stf);
                    break;
            }
        }

        public void Copy(IPowerSupply other)
        {
            if (other is SteamPowerSupply steamOther)
            {
                BatterySwitch.Copy(steamOther.BatterySwitch);
                MasterKey.Copy(steamOther.MasterKey);
            }
        }

        public void Initialize()
        {
            BatterySwitch.Initialize();
            MasterKey.Initialize();
        }

        public virtual void InitializeMoving()
        {
            BatterySwitch.InitializeMoving();
            MasterKey.InitializeMoving();
        }

        public void Save(BinaryWriter outf)
        {
            BatterySwitch.Save(outf);
            MasterKey.Save(outf);
        }

        public void Restore(BinaryReader inf)
        {
            BatterySwitch.Restore(inf);
            MasterKey.Restore(inf);
        }

        public void Update(float elapsedClockSeconds)
        {
        }

        public void HandleEvent(PowerSupplyEvent evt)
        {
            BatterySwitch.HandleEvent(evt);
            MasterKey.HandleEvent(evt);
        }

        public void HandleEvent(PowerSupplyEvent evt, int id)
        {
        }

        public void HandleEventFromTcs(PowerSupplyEvent evt, string message)
        {
        }

        public void HandleEventFromLeadLocomotive(PowerSupplyEvent evt)
        {
        }

        public void HandleEventFromLeadLocomotive(PowerSupplyEvent evt, int id)
        {
        }
    }
}
