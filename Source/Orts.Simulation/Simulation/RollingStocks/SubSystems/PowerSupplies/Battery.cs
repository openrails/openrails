
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
        protected float PowerW; // Power demanded by the power supply, not implemented
        protected float MaxChargingPowerW = 10000;
        protected float ChargingVoltageV;
        protected bool IsCharging
        {
            get
            {
                if (Car is MSTSLocomotive locomotive)
                {
                    return locomotive.LocomotivePowerSupply.AuxiliaryPowerSupplyOn;
                }
                else if (Car.PowerSupply != null)
                {
                    return Car.PowerSupply.ElectricTrainSupplyOn;
                }
                return false;
            }
        }
        public Interpolator ChargeVoltageCurve;
        public PowerSupplyState State;
        public Battery(MSTSWagon wagon)
        {
            Car = wagon;
            BatterySwitch = new BatterySwitch(wagon);
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
                                MaxChargingPowerW = stf.ReadFloatBlock(STFReader.UNITS.Power, MaxChargingPowerW);
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
            ChargeVoltageCurve = other?.ChargeVoltageCurve;
        }
        public virtual void Initialize()
        {
            CurrentCapacityJ = 0.7f * MaxCapacityJ;
            if (ChargingVoltageV == 0) ChargingVoltageV = NominalVoltageV * 1.15f;
            if (ChargeVoltageCurve == null)
            {
                ChargeVoltageCurve = new Interpolator(
                    new float[] {0, MaxCapacityJ * 0.1f, MaxCapacityJ}, 
                    new float[] {0, 0.95f * NominalVoltageV, 1.05f * NominalVoltageV});
            }

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

            if (IsCharging)
            {
                if (ChargeVoltageCurve[CurrentCapacityJ] < ChargingVoltageV && CurrentCapacityJ < MaxCapacityJ)
                {
                    // Charge the battery
                    float energyJ = MaxChargingPowerW * elapsedClockSeconds;
                    if (CurrentCapacityJ + energyJ > MaxCapacityJ)
                        energyJ = MaxCapacityJ - CurrentCapacityJ;
                    CurrentCapacityJ += energyJ;
                }
                // When charging, voltage is that of the battery charger
                VoltageV = ChargingVoltageV;
            }
            else
            {
                if (PowerW > 0 && State == PowerSupplyState.PowerOn)
                {
                    // Discharge the battery
                    float energyJ = PowerW * elapsedClockSeconds;
                    if (CurrentCapacityJ - energyJ > 0)
                        energyJ = CurrentCapacityJ;
                    CurrentCapacityJ -= energyJ;
                }
                // If battery is draining, voltage will depend on the total charge
                // Voltage also depends on power drawn, not implemented
                VoltageV = ChargeVoltageCurve[CurrentCapacityJ];
            }
        }
    }
}
