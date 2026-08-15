
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.Xna.Framework;
using Orts.Common;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using ORTS.Scripting.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Event = Orts.Common.Event;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{
    public class Battery : ISubSystem<Battery>
    {
        TrainCar Car;
        public BatterySwitch BatterySwitch { get; protected set; }
        public float NominalVoltageV = 72;
        public float VoltageV { get; protected set; }
        public float CurrentCapacityJ { get; protected set; }
        public float MaxCapacityJ = 3.6e7f;
        public PowerSupplyLoad BatteryCharger;
        protected float LowVoltageSupplyPowerW => Car.PowerSupply.LowVoltagePowerSupplyPowerW;
        protected float PowerFlowW; // Charging (positive) or discharging (negative) power to the battery
        protected float ChargingVoltageV;
        protected bool IsChargerOn => (Car.PowerSupply?.AuxiliaryPowerSupplyOn ?? false) && BatteryCharger.PowerReduction < 1;
        public Interpolator ChargeVoltageCurve;
        public PowerSupplyState State;
        public Battery(MSTSWagon wagon)
        {
            Car = wagon;
            BatterySwitch = new BatterySwitch(wagon);
            BatteryCharger = new PowerSupplyLoad(PowerSupplyLoadType.BatteryCharger);
        }
        public virtual void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortsbattery":
                case "wagon(ortsbattery":
                    stf.MustMatch("(");
                    while (!stf.EndOfBlock())
                    {
                        lowercasetoken = stf.ReadItem().ToLower();
                        switch (lowercasetoken)
                        {
                            case "voltage":
                                NominalVoltageV = stf.ReadFloatBlock(STFReader.UNITS.Voltage, NominalVoltageV);
                                break;
                            case "maxcapacity":
                                MaxCapacityJ = stf.ReadFloatBlock(STFReader.UNITS.Energy, MaxCapacityJ);
                                break;
                            case "chargerpower":
                                BatteryCharger.MaxPowerW = stf.ReadFloatBlock(STFReader.UNITS.Power, 0);
                                break;
                            case "chargervoltage":
                                ChargingVoltageV = stf.ReadFloatBlock(STFReader.UNITS.Voltage, 0);
                                break;
                            case "chargevoltagecurve":
                                ChargeVoltageCurve = new Interpolator(stf);
                                break;
                            case "mode":
                            case "delay":
                            case "defaulton":
                                BatterySwitch.Parse(lowercasetoken, stf);
                                break;
                            case "(":
                                stf.SkipRestOfBlock();
                                break;
                        }
                    }
                    break;
            }
        }
        public virtual void Copy(Battery other)
        {
            BatterySwitch.Copy(other.BatterySwitch);
            NominalVoltageV = other.NominalVoltageV;
            MaxCapacityJ = other.MaxCapacityJ;
            ChargeVoltageCurve = other.ChargeVoltageCurve;
            BatteryCharger.MaxPowerW = other.BatteryCharger.MaxPowerW;
        }
        public virtual void Initialize()
        {
            Car.PowerSupply.AuxiliaryPowerSupplyLoads.Add(BatteryCharger);
            CurrentCapacityJ = MaxCapacityJ;
            if (ChargingVoltageV == 0) ChargingVoltageV = NominalVoltageV * 1.15f;
            if (ChargeVoltageCurve == null)
            {
                ChargeVoltageCurve = new Interpolator(
                    new float[] {0, MaxCapacityJ * 0.1f, MaxCapacityJ}, 
                    new float[] {0, 0.95f * NominalVoltageV, 1.05f * NominalVoltageV});
            }
            if (BatteryCharger.MaxPowerW == 0) BatteryCharger.MaxPowerW = 10000;

            BatterySwitch.Initialize();
        }
        public virtual void InitializeMoving()
        {
            BatterySwitch.InitializeMoving();
        }
        public virtual void Save(BinaryWriter outf)
        {
            BatterySwitch.Save(outf);
            outf.Write(CurrentCapacityJ);
        }
        public virtual void Restore(BinaryReader inf)
        {
            BatterySwitch.Restore(inf);
            CurrentCapacityJ = inf.ReadSingle();
        }
        public virtual void Update(float elapsedClockSeconds)
        {
            BatterySwitch.Update(elapsedClockSeconds);

            if (State != PowerSupplyState.PowerOn)
            {
                PowerFlowW = 0;
                VoltageV = ChargeVoltageCurve[CurrentCapacityJ];
                BatteryCharger.CurrentPowerW = 0;
            }
            else if (IsChargerOn)
            {
                float chargerPowerW = BatteryCharger.MaxPowerW * (1 - BatteryCharger.PowerReduction);
                if ((ChargeVoltageCurve[CurrentCapacityJ] < ChargingVoltageV && CurrentCapacityJ < MaxCapacityJ) || LowVoltageSupplyPowerW > chargerPowerW)
                {
                    PowerFlowW = chargerPowerW - LowVoltageSupplyPowerW;
                }
                else
                {
                    PowerFlowW = 0;
                    chargerPowerW = LowVoltageSupplyPowerW;
                }
                BatteryCharger.CurrentPowerW = chargerPowerW;
                VoltageV = ChargingVoltageV;
            }
            else
            {
                PowerFlowW = -LowVoltageSupplyPowerW;
                VoltageV = ChargeVoltageCurve[CurrentCapacityJ];
                BatteryCharger.CurrentPowerW = 0;
            }
            if (PowerFlowW != 0)
            {
                float energyJ = PowerFlowW * elapsedClockSeconds;
                if (CurrentCapacityJ + energyJ > MaxCapacityJ)
                {
                    energyJ = MaxCapacityJ - CurrentCapacityJ;
                    PowerFlowW = energyJ / elapsedClockSeconds;
                }
                else if (CurrentCapacityJ + energyJ < 0)
                {
                    energyJ = -CurrentCapacityJ;
                    PowerFlowW = energyJ / elapsedClockSeconds;
                }
                CurrentCapacityJ += energyJ;
            }
        }
    }
}
