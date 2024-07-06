
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.Xna.Framework;
using Orts.Common;
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
        public BatterySwitch BatterySwitch { get; protected set; }
        public float NominalVoltageV = 72;
        public float VoltageV { get; protected set; }
        public float CurrentCapacityJ { get; protected set; }
        public float MaxCapacityJ = 3.6e7f;
        protected float EnergyFlowW; // < 0 discharging, > 0 charging
        public Interpolator ChargeVoltageCurve;
        public PowerSupplyState State;
        public Battery(MSTSWagon wagon)
        {
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
            CurrentCapacityJ = MaxCapacityJ;
            if (ChargeVoltageCurve == null)
            {
                ChargeVoltageCurve = new Interpolator(new float[] {0, MaxCapacityJ}, new float[] {0, NominalVoltageV});
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
            outf.Write(EnergyFlowW);
        }
        public virtual void Restore(BinaryReader inf)
        {
            BatterySwitch.Restore(inf);
            CurrentCapacityJ = inf.ReadSingle();
            EnergyFlowW = inf.ReadSingle();
        }
        public virtual void Update(float elapsedClockSeconds)
        {
            BatterySwitch.Update(elapsedClockSeconds);

            CurrentCapacityJ = MathHelper.Clamp(CurrentCapacityJ + EnergyFlowW * elapsedClockSeconds, 0, MaxCapacityJ);
            VoltageV = ChargeVoltageCurve[CurrentCapacityJ];
        }

    }
}