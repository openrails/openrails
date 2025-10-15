// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
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
         /// <summary>
        /// Indicates whether a brake is present or not when Manual Braking is selected.
        /// </summary>
        public bool ManualBrakePresent;

        public ManualBraking(TrainCar car)
        {
            Car = car;

        }

        float ManualMaxBrakeValue = 100.0f;
        float ManualReleaseRateValuepS;
        float ManualMaxApplicationRateValuepS;
        float ManualBrakingDesiredFraction;
        float EngineBrakeDesiredFraction;
        float ManualBrakingCurrentFraction;
        float EngineBrakingCurrentFraction;
        float SteamBrakeCompensation;
        bool LocomotiveSteamBrakeFitted = false;
        float SteamBrakePressurePSI = 0;
        float SteamBrakeCylinderPressurePSI = 0;
        float BrakeForceFraction;
        public override void SetBrakeEquipment(List<string> equipment)
        {
            ManualBrakePresent = equipment.Contains("manual_brake");
            base.SetBrakeEquipment(equipment);
        }
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
            base.InitializeFromCopy(copy);
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
            if (ManualBrakePresent)
                DebugType = "M";
            else
                DebugType = "-";

            // Changes brake type if locomotive fitted with steam brakes
            if (Car is MSTSSteamLocomotive)
            {
                var locoident = Car as MSTSSteamLocomotive;
                if (locoident.SteamEngineBrakeFitted)
                {
                    DebugType = "S";
                }
            }

            // Changes brake type if tender fitted with steam brakes
            if (Car.WagonType == MSTSWagon.WagonTypes.Tender)
            {
                var wagonid = Car as MSTSWagon;
                // Find the associated steam locomotive for this tender
                if (wagonid.TendersSteamLocomotive == null) wagonid.FindTendersSteamLocomotive();

                if (wagonid.TendersSteamLocomotive != null)
                {
                    if (wagonid.TendersSteamLocomotive.SteamEngineBrakeFitted) // if steam brakes are fitted to the associated locomotive, then add steam brakes here.
                    {
                        DebugType = "S";
                    }
                }
            }
        }

        public override void Update(float elapsedClockSeconds)
        {
            MSTSLocomotive lead = (MSTSLocomotive)Car.Train.LeadLocomotive;
            float BrakemanBrakeSettingValue = 0;
            float EngineBrakeSettingValue = 0;
            ManualBrakingDesiredFraction = 0;

            SteamBrakeCompensation = 1.0f;

            // Process manual braking on all cars
            if (lead != null && lead.BrakemanBrakeController != null)
            {
                BrakemanBrakeSettingValue = lead.BrakemanBrakeController.CurrentValue;
            }

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

            BrakeForceFraction = ManualBrakingCurrentFraction / ManualMaxBrakeValue;

            // If car is a locomotive or tender, then process engine brake
            if (Car.WagonType == MSTSWagon.WagonTypes.Engine || Car.WagonType == MSTSWagon.WagonTypes.Tender) // Engine brake
            {
                if (lead != null && lead.EngineBrakeController != null)
                {
                    EngineBrakeSettingValue = lead.EngineBrakeController.CurrentValue;
                    if (lead.SteamEngineBrakeFitted)
                    {
                        LocomotiveSteamBrakeFitted = true;
                        EngineBrakeDesiredFraction = EngineBrakeSettingValue * lead.MaxBoilerPressurePSI;
                    }
                    else
                    {
                        EngineBrakeDesiredFraction = EngineBrakeSettingValue * ManualMaxBrakeValue;
                    }


                    if (EngineBrakingCurrentFraction < EngineBrakeDesiredFraction)
                    {

                        EngineBrakingCurrentFraction += elapsedClockSeconds * lead.EngineBrakeController.ApplyRatePSIpS;
                        if (EngineBrakingCurrentFraction > EngineBrakeDesiredFraction)
                        {
                            EngineBrakingCurrentFraction = EngineBrakeDesiredFraction;
                        }

                    }
                    else if (EngineBrakingCurrentFraction > EngineBrakeDesiredFraction)
                    {
                        EngineBrakingCurrentFraction -= elapsedClockSeconds * lead.EngineBrakeController.ReleaseRatePSIpS;
                        if (EngineBrakingCurrentFraction < 0)
                        {
                            EngineBrakingCurrentFraction = 0;
                        }
                    }

                    if (lead.SteamEngineBrakeFitted)
                    {
                        SteamBrakeCompensation = lead.BoilerPressurePSI / lead.MaxBoilerPressurePSI;
                        SteamBrakePressurePSI = EngineBrakeSettingValue * SteamBrakeCompensation * lead.MaxBoilerPressurePSI;
                        SteamBrakeCylinderPressurePSI = EngineBrakingCurrentFraction * SteamBrakeCompensation; // For display purposes
                        BrakeForceFraction = EngineBrakingCurrentFraction / lead.MaxBoilerPressurePSI; // Manual braking value overwritten by engine calculated value
                    }
                    else
                    {
                        BrakeForceFraction = EngineBrakingCurrentFraction / ManualMaxBrakeValue;
                    }
                }
            }

            if (!Car.BrakesStuck)
            {
                Car.BrakeShoeForceN = Car.MaxBrakeForceN * Math.Min(BrakeForceFraction, 1);
                if (Car.BrakeShoeForceN < Car.MaxHandbrakeForceN * HandbrakePercent / 100)
                {
                    Car.BrakeShoeForceN = Car.MaxHandbrakeForceN * HandbrakePercent / 100;
                }
            }
            else
            {
                Car.BrakeShoeForceN = Math.Max(Car.MaxBrakeForceN, Car.MaxHandbrakeForceN / 2);
            }

            float brakeShoeFriction = Car.GetBrakeShoeFrictionFactor();
            Car.HuDBrakeShoeFriction = Car.GetBrakeShoeFrictionCoefficientHuD();

            Car.BrakeRetardForceN = Car.BrakeShoeForceN * brakeShoeFriction; // calculates value of force applied to wheel, independent of wheel skid

        }


        // Get the brake BC & BP for EOT conditions
        public override string GetStatus(Dictionary<BrakeSystemComponent, PressureUnit> units)
        {
            string s = Simulator.Catalog.GetString("Manual Brake");
            return s;
        }

        // Get Brake information for train
        public override string GetFullStatus(BrakeSystem lastCarBrakeSystem, Dictionary<BrakeSystemComponent, PressureUnit> units)
        {
            string s = Simulator.Catalog.GetString("Manual Brake");
            return s;
        }


        // This overides the information for each individual wagon in the extended HUD  
        public override string[] GetDebugStatus(Dictionary<BrakeSystemComponent, PressureUnit> units)
        {
            // display differently depending upon whether manual brake is present or not

            if (ManualBrakePresent && LocomotiveSteamBrakeFitted)
            {
                return new string[] {
                DebugType,
                string.Format("{0:F0}", FormatStrings.FormatPressure(SteamBrakeCylinderPressurePSI, PressureUnit.PSI,  PressureUnit.PSI, true)),
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty, // Spacer because the state above needs 2 columns.
                HandBrakePresent ? string.Format("{0:F0}%", HandbrakePercent) : string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                };
            }
            else if (ManualBrakePresent) // Just manual brakes fitted
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
                HandBrakePresent ? string.Format("{0:F0}%", HandbrakePercent) : string.Empty,
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
                HandBrakePresent ? string.Format("{0:F0}%", HandbrakePercent) : string.Empty,
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
            if (!HandBrakePresent)
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
            if (LocomotiveSteamBrakeFitted)
            {
                return SteamBrakeCylinderPressurePSI;
            }
            else
            {
                return ManualBrakingCurrentFraction;
            }
        }

        public override float GetCylVolumeM3()
        {
            return 0;
        }

        public override float GetTotalCylVolumeM3()
        {
            return 0;
        }

        public override float GetNormalizedCylTravel()
        {
            return Car.BrakeShoeForceN > 0.0f ? 1.0f : 0.0f;
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
