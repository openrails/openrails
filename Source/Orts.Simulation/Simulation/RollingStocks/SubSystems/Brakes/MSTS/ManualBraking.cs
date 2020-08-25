using Orts.Common;
using Orts.Parsers.Msts;
using Orts.Simulation.Physics;
using ORTS.Common;
using ORTS.Scripting.Api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS
{
    public class ManualBraking : MSTSBrakeSystem
    {
        TrainCar Car;
        protected string DebugType = string.Empty;
        float HandbrakePercent;

        public ManualBraking(TrainCar car)
        {
            Car = car;

        }

        float ManualMaxBrakeValue = 100.0f;
        float ManualReleaseRateValuepS;
        float ManualMaxApplicationRateValuepS;
        float ManualBrakingDesiredFraction;
        float ManualBrakingCurrentFraction;
        float ManualMaxBrakeForceN;

        public override bool GetHandbrakeStatus()
        {
            return HandbrakePercent > 0;
        }

        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "wagon(maxreleaserate": ManualReleaseRateValuepS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(maxapplicationrate": ManualMaxApplicationRateValuepS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
            }
        }

        public override void InitializeFromCopy(BrakeSystem copy)
        {
            ManualBraking thiscopy = (ManualBraking)copy;
            ManualMaxApplicationRateValuepS = thiscopy.ManualMaxApplicationRateValuepS;
            ManualReleaseRateValuepS = thiscopy.ManualReleaseRateValuepS;

        }

        public override void Save(BinaryWriter outf)
        {
            outf.Write(ManualBrakingCurrentFraction);
        }

        public override void Restore(BinaryReader inf)
        {
            ManualBrakingCurrentFraction = inf.ReadSingle();
        }

        public override void Initialize(bool handbrakeOn, float maxPressurePSI, float fullServPressurePSI, bool immediateRelease)
        {
            if ((Car as MSTSWagon).ManualBrakePresent)
                DebugType = "M";
            else
                DebugType = "-";
        }

        public override void Update(float elapsedClockSeconds)
        {
            MSTSLocomotive lead = (MSTSLocomotive)Car.Train.LeadLocomotive;
            float BrakemanBrakeSettingValue = lead.BrakemanBrakeController.CurrentValue;

            ManualBrakingDesiredFraction = BrakemanBrakeSettingValue * ManualMaxBrakeValue;

            if (ManualBrakingCurrentFraction < ManualBrakingDesiredFraction)
            {
                ManualBrakingCurrentFraction += ManualMaxApplicationRateValuepS;
                if (ManualBrakingCurrentFraction > ManualBrakingDesiredFraction)
                {
                    ManualBrakingCurrentFraction = ManualBrakingDesiredFraction;
                }

            }
            else if (ManualBrakingCurrentFraction > ManualBrakingDesiredFraction)
            {
                ManualBrakingCurrentFraction -= ManualReleaseRateValuepS;
                if (ManualBrakingCurrentFraction < 0)
                {
                    ManualBrakingCurrentFraction = 0;
                }

            }

            // Trace.TraceInformation("Manual Braking - CarId {0} BrakeSetting {1} MaxBrakeValue {2} DesiredFraction {3} CurrentFraction {4} MaxBrakeForce {5}", Car.CarID, ManualBrakeSettingValue, ManualMaxBrakeValue, ManualBrakingDesiredFraction, ManualBrakingCurrentFraction, Car.MaxBrakeForceN);

            float f;
            if (!Car.BrakesStuck)
            {
                f = Car.MaxBrakeForceN * Math.Min(ManualBrakingCurrentFraction / ManualMaxBrakeValue, 1);
                if (f < Car.MaxHandbrakeForceN * HandbrakePercent / 100)
                    f = Car.MaxHandbrakeForceN * HandbrakePercent / 100;
            }
            else f = Math.Max(Car.MaxBrakeForceN, Car.MaxHandbrakeForceN / 2);
            Car.BrakeRetardForceN = f * Car.BrakeShoeRetardCoefficientFrictionAdjFactor; // calculates value of force applied to wheel, independent of wheel skid
            if (Car.BrakeSkid) // Test to see if wheels are skiding to excessive brake force
            {
                Car.BrakeForceN = f * Car.SkidFriction;   // if excessive brakeforce, wheel skids, and loses adhesion
            }
            else
            {
                Car.BrakeForceN = f * Car.BrakeShoeCoefficientFrictionAdjFactor; // In advanced adhesion model brake shoe coefficient varies with speed, in simple model constant force applied as per value in WAG file, will vary with wheel skid.
            }

        }


        // Get the brake BC & BP for EOT conditions
        public override string GetStatus(Dictionary<BrakeSystemComponent, PressureUnit> units)
        {
            string s = "Manual Brake";
            return s;
        }

        // Get Brake information for train
        public override string GetFullStatus(BrakeSystem lastCarBrakeSystem, Dictionary<BrakeSystemComponent, PressureUnit> units)
        {
            string s = "Manual Brake";
            return s;
        }


        // This overides the information for each individual wagon in the extended HUD  
        public override string[] GetDebugStatus(Dictionary<BrakeSystemComponent, PressureUnit> units)
        {
            // display differently depending upon whether manual brake is present or not

            if ((Car as MSTSWagon).ManualBrakePresent)
            {
                return new string[] {
                DebugType,
                string.Format("{0:F0} %", ManualBrakingCurrentFraction),
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty, // Spacer because the state above needs 2 columns.
                (Car as MSTSWagon).HandBrakePresent ? string.Format("{0:F0}%", HandbrakePercent) : string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                };
            }
            else
            {
                return new string[] {
                DebugType,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty, // Spacer because the state above needs 2 columns.
                (Car as MSTSWagon).HandBrakePresent ? string.Format("{0:F0}%", HandbrakePercent) : string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                };
            }


        }





        // Required to override BrakeSystem
        public override void AISetPercent(float percent)
        {
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            //  Car.Train.EqualReservoirPressurePSIorInHg = Vac.FromPress(OneAtmospherePSI - MaxForcePressurePSI * (1 - percent / 100));
        }

        public override void SetHandbrakePercent(float percent)
        {
            if (!(Car as MSTSWagon).HandBrakePresent)
            {
                HandbrakePercent = 0;
                return;
            }
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            HandbrakePercent = percent;
        }

        public override float GetCylPressurePSI()
        {
            return ManualBrakingCurrentFraction;
        }

        public override float GetCylVolumeM3()
        {
            return 0;
        }

        public override float GetVacResVolume()
        {
            return 0;
        }

        public override float GetVacBrakeCylNumber()
        {
            return 0;
        }


        public override float GetVacResPressurePSI()
        {
            return 0;
        }

        public override bool IsBraking()
        {
            return false;
        }

        public override void CorrectMaxCylPressurePSI(MSTSLocomotive loco)
        {

        }

        public override void SetRetainer(RetainerSetting setting)
        {
        }

        public override float InternalPressure(float realPressure)
        {
            return Vac.ToPress(realPressure);
        }

        public override void PropagateBrakePressure(float elapsedClockSeconds)
        {

        }

        public override void InitializeMoving() // used when initial speed > 0
        {

        }

        public override void LocoInitializeMoving() // starting conditions when starting speed > 0
        {

        }


    }
}