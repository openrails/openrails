// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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

// Debug for Vacuum operation - Train Pipe Leak
//#define DEBUG_TRAIN_PIPE_LEAK

using Microsoft.Xna.Framework;
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
    public class VacuumSinglePipe : MSTSBrakeSystem
    {
        protected readonly static float OneAtmospherePSI = Bar.ToPSI(1);
        protected float MaxForcePressurePSI = KPa.ToPSI(KPa.FromInHg(21));    // relative pressure difference for max brake force
        protected TrainCar Car;
        protected float HandbrakePercent;
        protected float CylPressurePSIA;
        // Commented out as never used
        //float BrakeCutOffPSIA;
        //float BrakeRestorePSIA; 
        protected float VacResPressurePSIA;  // vacuum reservior pressure with piston in released position
        // defaults based on information in http://www.lmsca.org.uk/lms-coaches/LMSRAVB.pdf
        public int CylCount = 2;
        // brake cylinder diameter
        protected float CylDiameterM = Me.FromIn(18);
        // brake cylinder stroke length
        protected float CylStrokeM = Me.FromIn(4.5f);
        // brake cylinder volume with piston in applied position
        protected float CylVolumeM3;
        // total volume of all brake cylinders
        protected float TotalCylVolumeM3;
        // vacuum reservior volume with piston in released position
        public float VacResVolM3 = Me3.FromIn3((float)((24 / 2) * (24 / 2) * 16 * Math.PI));
        // volume units need to be consistent but otherwise don't matter, defaults are cubic inches
        bool HasDirectAdmissionValue = false;
        float DirectAdmissionValve = 0.0f;
        protected float MaxReleaseRatePSIpS = 2.5f;
        protected float MaxApplicationRatePSIpS = 2.5f;
        protected float LargeEjectorChargingRate;
        protected bool TrainBrakePressureChanging = false;
        protected bool BrakePipePressureChanging = false;
        protected int SoundTriggerCounter = 0;
        protected float prevCylPressurePSIA = 0f;
        protected float prevBrakePipePressurePSI = 0f;
        bool LocomotiveSteamBrakeFitted = false;
        float SteamBrakeCylinderPressurePSI = 0;
        float SteamBrakeCompensation;
        float SteamBrakingCurrentFraction;

        public VacuumSinglePipe(TrainCar car)
        {
            Car = car;
            // taking into account very short (fake) cars to prevent NaNs in brake line pressures
            base.BrakePipeVolumeM3 = (0.050f * 0.050f * (float)Math.PI / 4f) * Math.Max(5.0f, (1 + car.CarLengthM)); // Using (2") pipe
        }

        public override bool GetHandbrakeStatus()
        {
            return HandbrakePercent > 0;
        }

        public override void InitializeFromCopy(BrakeSystem copy)
        {
            base.InitializeFromCopy(copy);
            VacuumSinglePipe thiscopy = (VacuumSinglePipe)copy;
            MaxForcePressurePSI = thiscopy.MaxForcePressurePSI;
            MaxReleaseRatePSIpS = thiscopy.MaxReleaseRatePSIpS;
            MaxApplicationRatePSIpS = thiscopy.MaxApplicationRatePSIpS;
            CylCount = thiscopy.CylCount;
            CylDiameterM = thiscopy.CylDiameterM;
            CylStrokeM = thiscopy.CylStrokeM;
            CylVolumeM3 = thiscopy.CylVolumeM3;
            TotalCylVolumeM3 = thiscopy.TotalCylVolumeM3;
            BrakePipeVolumeM3 = thiscopy.BrakePipeVolumeM3;
            VacResVolM3 = thiscopy.VacResVolM3;
            HasDirectAdmissionValue = thiscopy.HasDirectAdmissionValue;
        }

        // return vacuum reservior pressure adjusted for piston movement
        // this section works out from the brake cylinder movement the amount of volume change in the reservoir, and hence the drop in vacuum in the reservoir. 
        // Normally the reservoir is a closed space during brake application, and thus vacuum is not lost, but simply varied with volume change
        protected float VacResPressureAdjPSIA()
        {
            if (VacResPressurePSIA >= CylPressurePSIA)
            {
                return VacResPressurePSIA;           
            }
            // TODO - review for a better approach
            // Calculate the new vacuum based upon the volume reduction in the reservoir due to brake cylinder movement
            // Using Boyles formula: PsVs = PfVf, and a starting pressure equal to 1 psi calculate the change in pressure
            float PressureChange = VacResVolM3 / (VacResVolM3 - (BrakeCylFraction * TotalCylVolumeM3));
            // Pressure Change should represent the incremental variation as the barke cylinder moves. 
            // Pressure is not linear and reversed compared to vacuum values, and hence more work maybe required to tidy this section of code up.
            float p = VacResPressurePSIA + PressureChange;
            return p < CylPressurePSIA ? p : CylPressurePSIA;
        }

        public override string GetStatus(Dictionary<BrakeSystemComponent, PressureUnit> units) // Status for last car in Main HUD
            => $" {Simulator.Catalog.GetString("BP")} {FormatStrings.FormatPressure(Vac.FromPress(BrakeLine1PressurePSI), PressureUnit.InHg, PressureUnit.InHg, false)}";

        public override string GetFullStatus(BrakeSystem lastCarBrakeSystem, Dictionary<BrakeSystemComponent, PressureUnit> units)  // Status for Main HUD view (calls above as well)
        {
            string s;
            // display depending upon whether an EQ reservoir fitted
            if ( Car.Train.EQEquippedVacLoco)
            {
                // The equalising pressure operates between 0 (Apply) and full pipe vacuum (12.278psi = 25 inHg - Release), which is the reverse of above, 
                // so it needs to be mapped to provide a desired vacuum of 2.278 psi = 25 inhg = Release and 14.503psi = 0 inhg = Apply
                MSTSLocomotive lead = (MSTSLocomotive)Car.Train.LeadLocomotive;
                float MaxVacuumPipeLevelPSI = lead == null ? Bar.ToPSI(Bar.FromInHg(21)) : lead.TrainBrakeController.MaxPressurePSI;
                float ValveFraction = 1 - (Car.Train.EqualReservoirPressurePSIorInHg / MaxVacuumPipeLevelPSI);
                ValveFraction = MathHelper.Clamp(ValveFraction, 0.0f, 1.0f); // Keep fraction within bounds

                float DisplayEqualReservoirPressurePSIorInHg = (ValveFraction * (OneAtmospherePSI - (OneAtmospherePSI - MaxVacuumPipeLevelPSI))) + (OneAtmospherePSI - MaxVacuumPipeLevelPSI);

                s = $" {Simulator.Catalog.GetString("EQ")} {FormatStrings.FormatPressure(Vac.FromPress(DisplayEqualReservoirPressurePSIorInHg), PressureUnit.InHg, PressureUnit.InHg, true)}"
                    + $" {Simulator.Catalog.GetString("V")} {FormatStrings.FormatPressure(Vac.FromPress(BrakeLine1PressurePSI), PressureUnit.InHg, PressureUnit.InHg, true)}";
            }
            else // No EQ reservoir by default
            {
                s = $" {Simulator.Catalog.GetString("Lead BP")} {FormatStrings.FormatPressure(Vac.FromPress(BrakeLine1PressurePSI), PressureUnit.InHg, PressureUnit.InHg, true)}";
            }

            //            string s = string.Format(" V {0}", FormatStrings.FormatPressure(Car.Train.EqualReservoirPressurePSIorInHg, PressureUnit.InHg, PressureUnit.InHg, true));
            if (lastCarBrakeSystem != null && lastCarBrakeSystem != this)
                s += $" {Simulator.Catalog.GetString("EOT")} {lastCarBrakeSystem.GetStatus(units)}";
            if (HandbrakePercent > 0)
                s += $" {Simulator.Catalog.GetString("Handbrake")} {HandbrakePercent:F0}%";
            return s;
        }

        public override string[] GetDebugStatus(Dictionary<BrakeSystemComponent, PressureUnit> units)  // status for each car in the train
        {
            if (LocomotiveSteamBrakeFitted)
            {
                return new string[] {
                "S",
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
                FrontBrakeHoseConnected ? "I" : "T",
                string.Format("A{0} B{1}", AngleCockAOpen ? "+" : "-", AngleCockBOpen ? "+" : "-"),
                BleedOffValveOpen ? Simulator.Catalog.GetString("Open") : string.Empty,
                };
            }
            else
            {

                return new string[] {
                "1V",
                FormatStrings.FormatPressure(Vac.FromPress(CylPressurePSIA), PressureUnit.InHg, PressureUnit.InHg, true),
                FormatStrings.FormatPressure(Vac.FromPress(BrakeLine1PressurePSI), PressureUnit.InHg, PressureUnit.InHg, true),
                FormatStrings.FormatPressure(Vac.FromPress(VacResPressureAdjPSIA()), PressureUnit.InHg, PressureUnit.InHg, true),
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                HandBrakePresent ? string.Format("{0:F0}%", HandbrakePercent) : string.Empty,
                FrontBrakeHoseConnected ? "I" : "T",
                string.Format("A{0} B{1}", AngleCockAOpen ? "+" : "-", AngleCockBOpen ? "+" : "-"),
                BleedOffValveOpen ? Simulator.Catalog.GetString("Open") : string.Empty,
                };
            }
        }

        public override float GetCylPressurePSI()
        {
            if (LocomotiveSteamBrakeFitted && (Car.WagonType == MSTSWagon.WagonTypes.Engine || Car.WagonType == MSTSWagon.WagonTypes.Tender))
            {
                return SteamBrakeCylinderPressurePSI;
            }
            else
            {
                return KPa.ToPSI(KPa.FromInHg(Vac.FromPress(CylPressurePSIA)));
            }
                        
        }

        public override float GetCylVolumeM3()
        {
            return CylVolumeM3;
        }
        public override float GetTotalCylVolumeM3()
        {
            return TotalCylVolumeM3;
        }

        public override float GetNormalizedCylTravel()
        {
            return CylPressurePSIA > VacResPressureAdjPSIA() ? 1.0f : 0.0f;
        }

        public override float GetVacResVolume()
        {
            return VacResVolM3;
        }

        public override float GetVacBrakeCylNumber()
        {
            return CylCount;
        }
        

        public override float GetVacResPressurePSI()
        {
            return VacResPressureAdjPSIA();
        }

        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "wagon(brakecylinderpressureformaxbrakebrakeforce": MaxForcePressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultInHg, null); break;
                case "wagon(maxreleaserate": MaxReleaseRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultInHgpS, null); break;
                case "wagon(maxapplicationrate": MaxApplicationRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultInHgpS, null); break;
                case "wagon(ortsdirectadmissionvalve": DirectAdmissionValve = stf.ReadFloatBlock(STFReader.UNITS.None, null);
                    if(DirectAdmissionValve == 1.0f)
                    {
                        HasDirectAdmissionValue = true;
                    }
                    else
                    {
                        HasDirectAdmissionValue = false;
                    }
                    break;
                // OpenRails specific parameters
                case "wagon(brakepipevolume": BrakePipeVolumeM3 = Me3.FromFt3(stf.ReadFloatBlock(STFReader.UNITS.VolumeDefaultFT3, null)); break;
                case "wagon(ortsauxiliaryrescapacity":
                case "wagon(ortsauxilaryrescapacity": VacResVolM3 = Me3.FromFt3(stf.ReadFloatBlock(STFReader.UNITS.VolumeDefaultFT3, null)); break;
                case "wagon(ortsbrakecylindersize":
                case "wagon(ortsbrakecylinderdiameter": CylDiameterM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(ortsbrakecylinderpistontravel": CylStrokeM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(ortsnumberbrakecylinders": CylCount = stf.ReadIntBlock(null); break;
            }
        }

        public override void Save(BinaryWriter outf)
        {
            outf.Write(BrakeLine1PressurePSI);
            outf.Write(BrakeLine2PressurePSI);
            outf.Write(BrakeLine3PressurePSI);
            outf.Write(CylPressurePSIA);
            outf.Write(VacResPressurePSIA);
            outf.Write(FrontBrakeHoseConnected);
            outf.Write(RearBrakeHoseConnected);
            outf.Write(AngleCockAOpen);
            outf.Write(AngleCockBOpen);
            outf.Write(BleedOffValveOpen);
        }

        public override void Restore(BinaryReader inf)
        {
            BrakeLine1PressurePSI = inf.ReadSingle();
            BrakeLine2PressurePSI = inf.ReadSingle();
            BrakeLine3PressurePSI = inf.ReadSingle();
            CylPressurePSIA = inf.ReadSingle();
            VacResPressurePSIA = inf.ReadSingle();
            FrontBrakeHoseConnected = inf.ReadBoolean();
            RearBrakeHoseConnected = inf.ReadBoolean();
            AngleCockAOpen = inf.ReadBoolean();
            AngleCockBOpen = inf.ReadBoolean();
            BleedOffValveOpen = inf.ReadBoolean();
        }

        public override void Initialize(bool handbrakeOn, float maxVacuumInHg, float fullServVacuumInHg, bool immediateRelease)
        {
            CylPressurePSIA = BrakeLine1PressurePSI = Vac.ToPress(fullServVacuumInHg);
            VacResPressurePSIA = Vac.ToPress(maxVacuumInHg);
            HandbrakePercent = handbrakeOn && HandBrakePresent ? 100 : 0;
            BrakeLine3PressurePSI = BrakeLine1PressurePSI;  // Initialise engine brake as same value on train
            //CylVolumeM3 = MaxForcePressurePSI * MaxBrakeForceN * 0.00000059733491f; //an average volume (M3) of air used in brake cylinder for 1 N brake force.
            Car.Train.PreviousCarCount = Car.Train.Cars.Count;

        }

        public override void Initialize()
        {
            // Calculate brake cylinder volume from given quantities
            CylVolumeM3 = (float)((Math.PI * (CylDiameterM * CylDiameterM) / 4.0f) * CylStrokeM);

            // Calculate total volume of all brake cylinders now so we don't need to later
            TotalCylVolumeM3 = CylVolumeM3 * CylCount;
        }

        public override void InitializeMoving() // used when initial speed > 0
        {

            BrakeLine1PressurePSI = Vac.ToPress(Car.Train.EqualReservoirPressurePSIorInHg);
            BrakeLine2PressurePSI = 0;
//            BrakeLine3PressurePSI = V2P(Car.Train.EqualReservoirPressurePSIorInHg);
/*            if (Car.Train.AITrainBrakePercent == 0)
            {
                CylPressurePSIA = 0;
                Car.BrakeForceN = 0;
            }
            else */
            CylPressurePSIA = Vac.ToPress(Car.Train.EqualReservoirPressurePSIorInHg);
            VacResPressurePSIA = Vac.ToPress(Car.Train.EqualReservoirPressurePSIorInHg);
            HandbrakePercent = 0;
        }

        public override void LocoInitializeMoving() // starting conditions when starting speed > 0
        {
            VacResPressurePSIA = Vac.ToPress(Car.Train.EqualReservoirPressurePSIorInHg);
        }

        public override void Update(float elapsedClockSeconds)
        {
            // Identify the lead locomotive as we don't want to change the BP pressure as this is catered for in the charging rates, etc
            bool LeadLoco = false;
            bool EngineBrake = false;
            MSTSLocomotive lead = (MSTSLocomotive)Car.Train.LeadLocomotive;
            if (lead != null)
            {
                LeadLoco = true;
                if (lead.EngineBrakeFitted)
                {
                    EngineBrake = true;  // set to overcome potential null errors with lead var.
                }

                if (lead.SteamEngineBrakeFitted && (Car.WagonType == MSTSWagon.WagonTypes.Engine || Car.WagonType == MSTSWagon.WagonTypes.Tender))
                {
                    // The steam brake uses the existing code for the train brake and engine brake. It models a Gresham and Craven MkIV steam brake valve.
                    // Engine brake moves in association with Engine Brake Controller, and uses the apply and release delays for steam brake force movement
                    // Train brake also moves the steam brake, and again brake is delayed by the engine brake delay values.

                    LocomotiveSteamBrakeFitted = true;

                    // Steam brake operation is impacted by boiler pressure, a drop in boiler pressure will reduce the force applied
                    SteamBrakeCompensation = lead.BoilerPressurePSI / lead.MaxBoilerPressurePSI;

                    float SteamBrakeDesiredFraction;

                    float MaximumVacuumPressureValue = OneAtmospherePSI - lead.TrainBrakeController.MaxPressurePSI; // As model uses air pressure this equates to minimum air pressure
                    float MinimumVacuumPressureValue = OneAtmospherePSI; // As model uses air pressure this equates to maximum air pressure
                    float EngineBrakePipeFraction = (lead.BrakeSystem.BrakeLine3PressurePSI - MaximumVacuumPressureValue) / (MinimumVacuumPressureValue - MaximumVacuumPressureValue);
                    EngineBrakePipeFraction = MathHelper.Clamp(EngineBrakePipeFraction, 0.0f, 1.0f); // Keep fraction within bounds

                    float TrainBrakePipeFraction = (lead.BrakeSystem.BrakeLine1PressurePSI - MaximumVacuumPressureValue) / (MinimumVacuumPressureValue - MaximumVacuumPressureValue);
                    TrainBrakePipeFraction = MathHelper.Clamp(TrainBrakePipeFraction, 0.0f, 1.0f); // Keep fraction within bounds

                    float conversionFactor = (MinimumVacuumPressureValue - MaximumVacuumPressureValue); // factor to scale application and release values to match pressure values in engine brake nethod

                    // Calculate the steam brake application and release rates for different brake scenarios, ie engine or train, etc
                    if (TrainBrakePipeFraction > EngineBrakePipeFraction) // Train brake is primary control
                    {
                        SteamBrakeDesiredFraction = TrainBrakePipeFraction;
                        if (SteamBrakingCurrentFraction < SteamBrakeDesiredFraction) // Brake application, increase steam brake pressure to max value as appropriate
                        {

                            var diff = elapsedClockSeconds * lead.EngineBrakeController.ApplyRatePSIpS / conversionFactor;

                            SteamBrakingCurrentFraction += diff;
                            if (SteamBrakingCurrentFraction > 1.0f)
                            {
                                SteamBrakingCurrentFraction = 1.0f;
                            }

                        }
                        else if (SteamBrakingCurrentFraction > SteamBrakeDesiredFraction) // Brake release, decrease steam brake pressure to min value as appropriate
                        {

                            var diff = elapsedClockSeconds * lead.EngineBrakeController.ReleaseRatePSIpS / conversionFactor;

                            SteamBrakingCurrentFraction -= diff;

                            if (SteamBrakingCurrentFraction < 0)
                            {
                                SteamBrakingCurrentFraction = 0;
                            }

                        }

                        SteamBrakeCylinderPressurePSI = SteamBrakingCurrentFraction * SteamBrakeCompensation * lead.MaxBoilerPressurePSI; // For display purposes
                        Car.PreviousSteamBrakeCylinderPressurePSI = SteamBrakeCylinderPressurePSI;
                    }
                    else // Engine brake is primary control
                    {
                        // Allow smooth change over if train brake has been applied and then released, but engine brake is also applied to a ceratin value
                        if (lead.EngineBrakeController.CurrentValue > 0 && Car.PreviousSteamBrakeCylinderPressurePSI >= SteamBrakeCylinderPressurePSI && SteamBrakeCylinderPressurePSI > 0)
                        {

                            float equivalentEngineBrakePipeFraction = SteamBrakeCylinderPressurePSI / (SteamBrakeCompensation * lead.MaxBoilerPressurePSI);
                            float equivalentBrakeLine3PressurePSI = equivalentEngineBrakePipeFraction * (MinimumVacuumPressureValue - MaximumVacuumPressureValue) + MaximumVacuumPressureValue;

                            lead.BrakeSystem.BrakeLine3PressurePSI = equivalentBrakeLine3PressurePSI; // If engine brake on, then don't allow engine brake pressure to drop when reducing train brake pressure

                            EngineBrakePipeFraction = SteamBrakingCurrentFraction;
                            Car.PreviousSteamBrakeCylinderPressurePSI = 0; // set to zero so that this loop is not executed again until train brake is activated
                        }

                        // Engine only brake applied
                        SteamBrakeCylinderPressurePSI = EngineBrakePipeFraction * SteamBrakeCompensation * lead.MaxBoilerPressurePSI;
                        SteamBrakingCurrentFraction = EngineBrakePipeFraction; // keep track of fraction.
                    }

                    // Forces steam brake pressure and force to zero if both brakes are off
                    if (lead.EngineBrakeController.CurrentValue == 0 && lead.TrainBrakeController.CurrentValue == 0)
                    {
                        SteamBrakeDesiredFraction = 0;

                        if (SteamBrakingCurrentFraction > SteamBrakeDesiredFraction)
                        {
                            SteamBrakingCurrentFraction -= elapsedClockSeconds * lead.EngineBrakeController.ReleaseRatePSIpS;

                            if (SteamBrakingCurrentFraction < 0)
                            {
                                SteamBrakingCurrentFraction = 0;
                            }
                        }

                        SteamBrakeCylinderPressurePSI = SteamBrakingCurrentFraction * SteamBrakeCompensation * lead.MaxBoilerPressurePSI;
                    }
                }

                // Brake cuts power
                // IN real life cutting of power is an electric relay with a pressure sensor. The moment that vacuum drops below BO set point the contacts open and power to the 
                // traction motors instantly drops to zero. The driver's power handle remains in whatever position it was in. If the brakes are then released the relay restores power to
                // the traction motors back to whatever the throttle position happens to be.
                // Convert restore and cutoff limit values to a value on our "pressure" scale
                float BrakeCutoffPressurePSI = OneAtmospherePSI - lead.BrakeCutsPowerAtBrakePipePressurePSI;
                float BrakeRestorePressurePSI = OneAtmospherePSI - lead.BrakeRestoresPowerAtBrakePipePressurePSI;

                if (Car is MSTSLocomotive locomotive && locomotive.DoesVacuumBrakeCutPower)
                {
                    // There are three zones of operation - (note logic reversed - O InHg = 14.73psi, and eg 21 InHg = 4.189psi)
                    // Cutoff - exceeds set value, eg 12.5InHg (= 8.5psi)
                    // Between cutoff and restore levels - only if cutoff has triggerd
                    // Restore - when value exceeds set value, eg 17InHg (= 6.36 psi) - resets throttle
                    if (BrakeLine1PressurePSI < BrakeRestorePressurePSI)
                    {
                        locomotive.TrainControlSystem.BrakeSystemTractionAuthorization = true;
                    }
                    else if (BrakeLine1PressurePSI > BrakeCutoffPressurePSI)
                    {
                        locomotive.TrainControlSystem.BrakeSystemTractionAuthorization = false;
                    }
                }
            }

            // Brake information is updated for each vehicle

            if (EngineBrake && (Car.WagonType == MSTSWagon.WagonTypes.Engine || Car.WagonType == MSTSWagon.WagonTypes.Tender)) // Only apples when an engine brake is in place, otherwise processed to next loop
            {
                // The engine brake can only be applied when the train brake is released or partially released. It cannot be released whilever the train brake is applied.
                if (lead.TrainBrakeController.CurrentValue == 0 && lead.EngineBrakeController.CurrentValue > 0) // If train brake is completely released & Engine brake is applied
                {
                    CylPressurePSIA = BrakeLine3PressurePSI;
                }
                else if (lead.TrainBrakeController.CurrentValue > 0) // if train brake is applied, then set engine brake to the higher of either the train brake or engine brake
                {
                    if (BrakeLine3PressurePSI > BrakeLine1PressurePSI)
                    {
                        CylPressurePSIA = BrakeLine3PressurePSI;
                    }
                    else
                    {
                        CylPressurePSIA = BrakeLine1PressurePSI;
                    }
                }
                else // normally only the train brake will drive the cylinder pressure
                {
                    CylPressurePSIA = BrakeLine1PressurePSI;
                }

                // Adjust vacuum reservoir if necessary
                if (BrakeLine1PressurePSI < VacResPressurePSIA)
                {
                    float dp = elapsedClockSeconds * MaxApplicationRatePSIpS * (TotalCylVolumeM3) / VacResVolM3;
                    float vr = VacResVolM3 / BrakePipeVolumeM3;
                    if (VacResPressurePSIA - dp < BrakeLine1PressurePSI + dp * vr)
                    {
                        dp = (VacResPressurePSIA - BrakeLine1PressurePSI) / (1 + vr);
                    }

                    VacResPressurePSIA -= dp;
                }
            }
            else
            {

                if (BleedOffValveOpen)
                {
                    // the following reduces the brake cylinder and vacuum reservoir to 0inHg if the bleed valve is operated
                    float dp = elapsedClockSeconds * MaxApplicationRatePSIpS;

                    VacResPressurePSIA = Math.Min(VacResPressurePSIA + dp, OneAtmospherePSI);

                    CylPressurePSIA = Math.Min(CylPressurePSIA + dp, OneAtmospherePSI);

                }
                else if (BrakeLine1PressurePSI < VacResPressurePSIA)
                {
                    float dp = elapsedClockSeconds * MaxApplicationRatePSIpS * (TotalCylVolumeM3) / VacResVolM3;
                    float vr = VacResVolM3 / BrakePipeVolumeM3;
                    if (VacResPressurePSIA - dp < BrakeLine1PressurePSI + dp * vr)
                    {
                        dp = (VacResPressurePSIA - BrakeLine1PressurePSI) / (1 + vr);
                    }
                    VacResPressurePSIA -= dp;

                    if (LeadLoco == false)
                    {
                        BrakeLine1PressurePSI += dp * vr; // don't adjust the BP pressure if this is the lead locomotive
                    }

                    CylPressurePSIA = VacResPressurePSIA;
                }
                else if (BrakeLine1PressurePSI < CylPressurePSIA) // Increase BP pressure, hence vacuum brakes are being released
                {
                    float dp = elapsedClockSeconds * MaxReleaseRatePSIpS;
                    float vr = TotalCylVolumeM3 / BrakePipeVolumeM3;
                    if (CylPressurePSIA - dp < BrakeLine1PressurePSI + dp * vr)
                        dp = (CylPressurePSIA - BrakeLine1PressurePSI) / (1 + vr);
                    CylPressurePSIA -= dp;

                    if (LeadLoco == false)
                    {
                        BrakeLine1PressurePSI += dp * vr;
                    }
                }
                else if (BrakeLine1PressurePSI > CylPressurePSIA)  // Decrease BP pressure, hence vacuum brakes are being applied
                {
                    float dp = elapsedClockSeconds * MaxApplicationRatePSIpS;
                    float vr = TotalCylVolumeM3 / BrakePipeVolumeM3;
                    if (CylPressurePSIA + dp > BrakeLine1PressurePSI - dp * vr)
                        dp = (BrakeLine1PressurePSI - CylPressurePSIA) / (1 + vr);
                    CylPressurePSIA += dp;
                    if (!HasDirectAdmissionValue)
                        BrakeLine1PressurePSI -= dp * vr;
                }
            }
           
            // Record HUD display values for brake cylidners depending upon whether they are wagons or locomotives/tenders (which are subject to their own engine brakes)   
            if (Car.WagonType == MSTSWagon.WagonTypes.Engine || Car.WagonType == MSTSWagon.WagonTypes.Tender)
            {
                Car.Train.HUDLocomotiveBrakeCylinderPSI = CylPressurePSIA;
                Car.Train.HUDWagonBrakeCylinderPSI = Car.Train.HUDLocomotiveBrakeCylinderPSI;  // Initially set Wagon value same as locomotive, will be overwritten if a wagon is attached
            }
            else
            {
                // Record the Brake Cylinder pressure in first wagon, as EOT is also captured elsewhere, and this will provide the two extremeties of the train
                // Identifies the first wagon based upon the previously identified UiD 
                if (Car.UiD == Car.Train.FirstCarUiD)
                {
                    Car.Train.HUDWagonBrakeCylinderPSI = CylPressurePSIA; // In Vacuum HUD BP is actually supposed to be dispalayed
                }

            }

            // If wagons are not attached to the locomotive, then set wagon BC pressure to same as locomotive in the Train brake line
            if (!Car.Train.WagonsAttached && (Car.WagonType == MSTSWagon.WagonTypes.Engine || Car.WagonType == MSTSWagon.WagonTypes.Tender))
            {
                Car.Train.HUDWagonBrakeCylinderPSI = CylPressurePSIA;
            }

                float vrp = VacResPressureAdjPSIA();

            if (!Car.BrakesStuck)
            {
                // depending upon whether steam brake fitted or not, calculate brake force to be applied
                if (LocomotiveSteamBrakeFitted && (Car.WagonType == MSTSWagon.WagonTypes.Engine || Car.WagonType == MSTSWagon.WagonTypes.Tender))
                {
                    var leadLocomotiveMaxBoilerPressurePSI = lead == null ? 200.0f : (lead.MaxBoilerPressurePSI);
                    Car.BrakeShoeForceN = Car.MaxBrakeForceN * Math.Min(SteamBrakeCylinderPressurePSI / leadLocomotiveMaxBoilerPressurePSI, 1);
                }
                else
                {
                    Car.BrakeShoeForceN = CylPressurePSIA <= vrp ? 0 : Car.MaxBrakeForceN * Math.Min((CylPressurePSIA - vrp) / MaxForcePressurePSI, 1);
                }

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

            // sound trigger checking runs every 4th update, to avoid the problems caused by the jumping BrakeLine1PressurePSI value, and also saves cpu time :)
            if (SoundTriggerCounter >= 4)
            {
                SoundTriggerCounter = 0;
                if (CylPressurePSIA != prevCylPressurePSIA)
                {
                    if (!TrainBrakePressureChanging)
                    {
                        if (CylPressurePSIA > prevCylPressurePSIA)
                            Car.SignalEvent(Event.TrainBrakePressureIncrease);
                        else
                            Car.SignalEvent(Event.TrainBrakePressureDecrease);
                            TrainBrakePressureChanging = !TrainBrakePressureChanging;
                    }

                }
                else if (TrainBrakePressureChanging)
                {
                    TrainBrakePressureChanging = !TrainBrakePressureChanging;
                    Car.SignalEvent(Event.TrainBrakePressureStoppedChanging);
                }

                if ( Math.Abs(BrakeLine1PressurePSI-prevBrakePipePressurePSI)> 0.05) /*BrakeLine1PressurePSI > prevBrakePipePressurePSI*/
                {
                    if (!BrakePipePressureChanging)
                    {
                        if (BrakeLine1PressurePSI > prevBrakePipePressurePSI)
                            Car.SignalEvent(Event.BrakePipePressureIncrease);
                        else
                            Car.SignalEvent(Event.BrakePipePressureDecrease);
                        BrakePipePressureChanging = !BrakePipePressureChanging;
                    }

                }
                else if (BrakePipePressureChanging)
                {
                    BrakePipePressureChanging = !BrakePipePressureChanging;
                    Car.SignalEvent(Event.BrakePipePressureStoppedChanging);
                }
                prevCylPressurePSIA = CylPressurePSIA;
                prevBrakePipePressurePSI = BrakeLine1PressurePSI;
            }
            SoundTriggerCounter++;
        }

        public override void PropagateBrakePressure(float elapsedClockSeconds)
        {
            PropagateBrakeLinePressures(elapsedClockSeconds, Car, TwoPipes);
        }


        protected static void PropagateBrakeLinePressures(float elapsedClockSeconds, TrainCar trainCar, bool twoPipes)
        {
            // Called by train update physics
            // Brake pressures are calculated on the lead locomotive first, and then propagated along each vehicle in the consist.

            var train = trainCar.Train;
            var lead = trainCar as MSTSLocomotive;

            var brakePipeTimeFactorS = lead == null ? 0.0015f : lead.BrakePipeTimeFactorS;

            // train.BrakeLine1PressurePSI is really vacuum in inHg
            // BP is charged and discharged between approx 4.185psi = 21 InHg (or 25InHg, as set by user) and 14.5 psi (atmospheric pressure)
            // The resulting air pressures are then converted to a respective vacuum value where - 14.5psi (atmospheric pressure) = 0 InHg, and approx 4.185psi = 21 InHg.
            // Brakes are applied when vaccum is destroyed, ie 0 InHg, Brakes released when vacuum established ie 21 or 25 InHg

            float SmallEjectorChargingRateInHgpS = lead == null ? 10.0f : (lead.SmallEjectorBrakePipeChargingRatePSIorInHgpS); // Set value for small ejector to operate - fraction set in steam locomotive
            float LargeEjectorChargingRateInHgpS = lead == null ? 10.0f : (lead.LargeEjectorBrakePipeChargingRatePSIorInHgpS); // Set value for large ejector to operate - fraction set in steam locomotive

            // Vaccum brake effectiveness decreases with increases in altitude because the atmospheric pressure increases as altitude increases.
            // The formula for decrease in pressure:  P = P0 * Exp (- Mgh/RT) - https://www.math24.net/barometric-formula/

            float massearthair = 0.02896f; // Molar mass of Earth's air = M = 0.02896 kg/mol
            // float sealevelpressure = 101325f; // Average sea level pressure = P0 = 101,325 kPa
            float sealevelpressure = 101325f; // Average sea level pressure = P0 = 101,325 kPa
            float gravitationalacceleration = 9.807f; // Gravitational acceleration = g = 9.807 m/s^2
            float standardtemperature = 288.15f; // Standard temperature = T = 288.15 K
            float universalgasconstant = 8.3143f; // Universal gas constant = R = 8.3143 (N*m/mol*K)
            float height = lead == null ? 10.0f : lead.CarHeightAboveSeaLevelM;
            float alititudereducedvacuum = 0;

            alititudereducedvacuum = sealevelpressure * (float)Math.Exp((-1.0f * massearthair * gravitationalacceleration * height) / (standardtemperature * universalgasconstant));

            float vacuumreductionfactor = alititudereducedvacuum / sealevelpressure;

            float InitialMaxVacuumPipeLevelPSI = lead == null ? Bar.ToPSI(Bar.FromInHg(21)) : lead.TrainBrakeController.MaxPressurePSI;

            float MaxVacuumPipeLevelPSI = InitialMaxVacuumPipeLevelPSI * vacuumreductionfactor;

            // Desired Vacuum pipe level must operate between full vacuum level (eg 2.278 psi = 25 inhg = Release) and atmospheric pressure (14.503psi = 0 inhg = Apply). 
            // The equalising pressure operates between 0 (Apply) and full pipe vacuum (12.278psi = 25 inHg - Release), which is the reverse of above, so it needs to be mapped to
            // provide a desired vacuum of 2.278 psi = 25 inhg = Release and 14.503psi = 0 inhg = Apply
            // Hence Desired = Control Vale % * Vacuum Rise + base Vacuum.
            float ValveFraction = 1 - (train.EqualReservoirPressurePSIorInHg / MaxVacuumPipeLevelPSI);
            ValveFraction = MathHelper.Clamp(ValveFraction, 0.0f, 1.0f); // Keep fraction within bounds

            float DesiredPipeVacuum = (ValveFraction * (OneAtmospherePSI - (OneAtmospherePSI - MaxVacuumPipeLevelPSI))) + (OneAtmospherePSI - MaxVacuumPipeLevelPSI);

            float TrainPipeLeakLossPSI = lead == null ? 0.0f : (lead.TrainBrakePipeLeakPSIorInHgpS);

            float TempTrainPipePSI = lead == null ? 5.0f : lead.BrakeSystem.BrakeLine1PressurePSI;
            float TempTotalTrainBrakePipeVolumeM3 = 0.0f; // initialise train brake pipe volume
            float TempTotalTrainBrakeCylinderVolumeM3 = 0.0f;
            float TempCurrentBrakeCylinderVolumeM3 = 0.0f;
            float TempCurrentBrakePipeVolumeM3 = 0.0f;
            float AdjbrakePipeTimeFactorS = 0.0f;
            float AdjBrakePipeDischargeTimeFactor = 0.0f;

            // Initialise parameters for calculating brake system adjustments
            float AdjLargeEjectorChargingRateInHgpS = 0.0f;
            float AdjSmallEjectorChargingRateInHgpS = 0.0f;
            float AdjVacuumPumpChargingRateInHgpS = 0.0f;
            float AdjHighSExhausterChargingRateInHgpS = 0.0f;
            float AdjLowSExhausterChargingRateInHgpS = 0.0f;
            float AdjBrakeServiceTimeFactorPSIpS = 0.0f;
            float AdjBrakeEmergencyTimeFactorPSIpS = 0.0f;
            float AdjTrainPipeLeakLossPSI = 0.0f;
            float TempbrakePipeTimeMultFactor = 0.0f;
            float RunningNetBPLossGainPSI = 0.0f;     // The net value of the losses and gains in the brake pipe for quick release position: eg Net = Lg Ejector + Sm Ejector + Vac Pump - BP Loss
            float ReleaseNetBPLossGainPSI = 0.0f;   // The net value of the losses and gains in the brake pipe for release position: eg Net = Lg Ejector + Sm Ejector + Vac Pump - BP Loss
            float QuickReleaseNetBPLossGainPSI = 0.0f;   // The net value of the losses and gains in the brake pipe for quick release position: eg Net = Lg Ejector + Sm Ejector + Vac Pump - BP Loss
            float LapNetBPLossGainPSI = 0.0f;   // The net value of the losses and gains in the brake pipe for lap position: eg Net = Lg Ejector + Sm Ejector + Vac Pump - BP Loss
            float EQReleaseNetBPLossGainPSI = 0.0f;   // The net value of the losses and gains in the brake pipe for EQ release position: eg Net = Lg Ejector + Sm Ejector + Vac Pump - BP Loss

            train.EQEquippedVacLoco = lead == null ? false : lead.VacuumBrakeEQFitted;

            foreach (TrainCar car in train.Cars)
            {

                // Calculate train brake system volumes
                TempTotalTrainBrakePipeVolumeM3 += car.BrakeSystem.BrakePipeVolumeM3; // Calculate total brake pipe volume of train

                // If vehicle is not a vacuum piped vehicle then calculate both volume of train pipe and BC, otherwise for vacuum piped vehicles only calculate train pipe
                if (car.CarBrakeSystemType != "vacuum_piped")
                {
                    TempTotalTrainBrakeCylinderVolumeM3 += car.BrakeSystem.GetTotalCylVolumeM3(); // Calculate total brake cylinder volume of train

                    car.BrakeSystem.BrakeCylFraction = 1.0f - (car.BrakeSystem.GetCylPressurePSI() / (MaxVacuumPipeLevelPSI));
                    car.BrakeSystem.BrakeCylFraction = MathHelper.Clamp(car.BrakeSystem.BrakeCylFraction, 0.01f, 1.0f); // Keep fraction within bounds

                    TempCurrentBrakeCylinderVolumeM3 += (car.BrakeSystem.GetTotalCylVolumeM3() * car.BrakeSystem.BrakeCylFraction);
                }

            }

            float BrakePipeFraction = ((TempTrainPipePSI - (OneAtmospherePSI - MaxVacuumPipeLevelPSI)) / (MaxVacuumPipeLevelPSI));
            BrakePipeFraction = MathHelper.Clamp(BrakePipeFraction, 0.01f, 1.0f); // Keep fraction within bounds

            TempCurrentBrakePipeVolumeM3 = TempTotalTrainBrakePipeVolumeM3 * BrakePipeFraction; // Current Volume of air in train pipe
            train.TotalTrainBrakePipeVolumeM3 = TempTotalTrainBrakePipeVolumeM3;
            train.TotalTrainBrakeCylinderVolumeM3 = TempTotalTrainBrakeCylinderVolumeM3;
            train.TotalTrainBrakeSystemVolumeM3 = TempTotalTrainBrakePipeVolumeM3 + TempTotalTrainBrakeCylinderVolumeM3;
            train.TotalCurrentTrainBrakeSystemVolumeM3 = TempCurrentBrakeCylinderVolumeM3 + TempCurrentBrakePipeVolumeM3;

            // This section sets up the number of iterative steps that the propagation process goes through. nSteps is tied to the volume ratio so that instability is not introduced
            // If nSteps is small and BrakeServiceTimeFactorS is small then instability will be introduced, and BP will fluctuate to different values
            int nSteps;
            float nStepsFraction;
            nStepsFraction = (Me3.FromFt3(200.0f) / train.TotalTrainBrakeSystemVolumeM3);
            float nStepsWhole = (elapsedClockSeconds * nStepsFraction) / brakePipeTimeFactorS + 1;
            nSteps = (int)(nStepsWhole);
            float TrainPipeTimeVariationS = elapsedClockSeconds / nSteps;

            // Calculate adjusted values based upon the train brake system volume
            if (lead != null)
            {

                // Calculate brake system volume of the train, and then adjust accordingly the BP charging, discharging and propogation rates.
                // The reference brake system is assumed to be 200ft^3, as ejector specifications suggest that a standard ejector can evacuate a brake system to 21InHg in about 60 seconds
                // BrakePipeChargingRatePSIorInHgpS - trains of less then 200ft^3 will have higher charging rates, ie less time to charge BP
                // BrakeServiceTimeFactorS / BrakeEmergencyTimeFactorS  - trains of less then 200ft^3 will have lower factors, ie less time to discharge BP
                AdjLargeEjectorChargingRateInHgpS = (Me3.FromFt3(200.0f) / train.TotalTrainBrakeSystemVolumeM3) * LargeEjectorChargingRateInHgpS;
                AdjSmallEjectorChargingRateInHgpS = (Me3.FromFt3(200.0f) / train.TotalTrainBrakeSystemVolumeM3) * SmallEjectorChargingRateInHgpS;
                AdjVacuumPumpChargingRateInHgpS = (Me3.FromFt3(200.0f) / train.TotalTrainBrakeSystemVolumeM3) * lead.VacuumPumpChargingRateInHgpS;
                AdjHighSExhausterChargingRateInHgpS = (Me3.FromFt3(200.0f) / train.TotalTrainBrakeSystemVolumeM3) * lead.ExhausterHighSBPChargingRatePSIorInHgpS;
                AdjLowSExhausterChargingRateInHgpS = (Me3.FromFt3(200.0f) / train.TotalTrainBrakeSystemVolumeM3) * lead.ExhausterLowSBPChargingRatePSIorInHgpS;
                AdjTrainPipeLeakLossPSI = (train.TotalTrainBrakeSystemVolumeM3 / Me3.FromFt3(200.0f)) * lead.TrainBrakePipeLeakPSIorInHgpS;
                AdjBrakeServiceTimeFactorPSIpS = (train.TotalTrainBrakeSystemVolumeM3 / Me3.FromFt3(200.0f)) * lead.BrakeServiceTimeFactorPSIpS;
                AdjBrakeEmergencyTimeFactorPSIpS = (train.TotalTrainBrakeSystemVolumeM3 / Me3.FromFt3(200.0f)) * lead.BrakeEmergencyTimeFactorPSIpS;
                AdjBrakeEmergencyTimeFactorPSIpS = MathHelper.Clamp(AdjBrakeEmergencyTimeFactorPSIpS, 1.0f, AdjBrakeEmergencyTimeFactorPSIpS);  // Make sure service time does not go below 1, as this causes too faster operation for light engines
                TempbrakePipeTimeMultFactor = train.TotalTrainBrakeSystemVolumeM3 / Me3.FromFt3(200.0f);
                AdjbrakePipeTimeFactorS = TempbrakePipeTimeMultFactor * brakePipeTimeFactorS;
                AdjBrakePipeDischargeTimeFactor = TempbrakePipeTimeMultFactor * lead.BrakePipeDischargeTimeFactor;

     
                // This section determines whether small ejector or vacuum pump is going to counteract brake pipe leakage - only applies to steam locomotives

                if (lead.EngineType == TrainCar.EngineTypes.Steam)
                {
                    if (!lead.SmallEjectorControllerFitted)
                    {
                        AdjSmallEjectorChargingRateInHgpS = 0.0f; // If small ejector not fitted, then set input from ejector to zero
                    }

                    // Zero vacuum pump (turn off) if BP is at full vacuum, or if Vacuum drops below 3InHg from max operating vacuum
                    if (lead.VacuumPumpFitted && (lead.BrakeSystem.BrakeLine1PressurePSI + (TrainPipeTimeVariationS * AdjVacuumPumpChargingRateInHgpS) > OneAtmospherePSI ||
                        Vac.FromPress(lead.BrakeSystem.BrakeLine1PressurePSI) < Vac.FromPress(OneAtmospherePSI - (MaxVacuumPipeLevelPSI - KPa.ToPSI(KPa.FromInHg(3))))))
                    {
                        AdjVacuumPumpChargingRateInHgpS = 0.0f; // Set vacuum pump to zero, as vacuum is being maintained, ie pump is off
                        lead.VacuumPumpOperating = false;
                    }
                    else if (lead.VacuumPumpFitted)
                    {
                        lead.VacuumPumpOperating = true;
                    }
                    else
                    {
                        AdjVacuumPumpChargingRateInHgpS = 0.0f; // Set vacuum pump to zero, as vacuum is not fitted
                        lead.VacuumPumpOperating = false;
                    }

                    RunningNetBPLossGainPSI = (AdjTrainPipeLeakLossPSI - (AdjSmallEjectorChargingRateInHgpS + AdjVacuumPumpChargingRateInHgpS));
                }

                // Calculate the net loss/gain in terms of charging the BP - applies in regard to RELEASE type brake positions
                // In release - for diesel and electric use low speed exhauster, for steam use Large ejector (small can be turned on individually by driver)
                ReleaseNetBPLossGainPSI = (AdjLowSExhausterChargingRateInHgpS + AdjLargeEjectorChargingRateInHgpS + AdjSmallEjectorChargingRateInHgpS + AdjVacuumPumpChargingRateInHgpS) - AdjTrainPipeLeakLossPSI;

                // Calculate the net loss/gain in terms of charging the BP - applies in regard to QUICK RELEASE type brake positions
                // In release - for diesel and electric use low speed exhauster, for steam use Large ejector (small can be turned on individually by driver)
                QuickReleaseNetBPLossGainPSI = (AdjHighSExhausterChargingRateInHgpS + AdjLargeEjectorChargingRateInHgpS + AdjSmallEjectorChargingRateInHgpS + AdjVacuumPumpChargingRateInHgpS) - AdjTrainPipeLeakLossPSI;


                // Calculate the net loss/gain in terms of charging the BP - applies in regard to LAP type brake positions
                // In lap - for diesel, steam and electric use BP leakage if included in ENG file
                LapNetBPLossGainPSI = AdjTrainPipeLeakLossPSI;

                // Calculate the net loss/gain in terms of charging the BP - applies in regard to EQ Release positions
                // Assume that EQ reservoir only fitted to diesel or electric locomotives
                if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.FullQuickRelease || (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Release && lead.VacuumExhausterPressed))
                {
                    // Full Quick release - assumption that exhauster is in high speed mode
                    EQReleaseNetBPLossGainPSI = (AdjHighSExhausterChargingRateInHgpS + AdjLargeEjectorChargingRateInHgpS + AdjSmallEjectorChargingRateInHgpS + AdjVacuumPumpChargingRateInHgpS) - AdjTrainPipeLeakLossPSI;
                }
                else
                {
                    // Release - assumption that exhauster is in low speed mode
                    EQReleaseNetBPLossGainPSI = (AdjLowSExhausterChargingRateInHgpS + AdjLargeEjectorChargingRateInHgpS + AdjSmallEjectorChargingRateInHgpS + AdjVacuumPumpChargingRateInHgpS) - AdjTrainPipeLeakLossPSI;
                }


                // Provide a HUD view for comparison only in Steam Locomotive Information - shows all pluses and minuses
                lead.HUDNetBPLossGainPSI = (lead.ExhausterHighSBPChargingRatePSIorInHgpS + LargeEjectorChargingRateInHgpS + SmallEjectorChargingRateInHgpS + lead.VacuumPumpChargingRateInHgpS) - lead.TrainBrakePipeLeakPSIorInHgpS;

            }

            // For each iterative step, calculate lead locomotive pressures, and propagate them along the train
            // Train brake pipe volume will be calculated, and used to vary timing response parameters, thus simulating variations in train length
            for (int i = 0; i < nSteps; i++)
            {
                // Calculate train pipe pressure at lead locomotive.
                // If a straight vacuum brake, then calculate lead brake pressure in straightvacuumsinglepipe class.
                if (lead != null && lead.CarBrakeSystemType != "straight_vacuum_single_pipe")
                {

                    // When brakeController put into Running position the RunningLock ensures that brake pipe matches the Equalising Reservoir (Desired Vacuum) before
                    // locking the system into the Running position.
                    if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Running && DesiredPipeVacuum == lead.BrakeSystem.BrakeLine1PressurePSI && !lead.BrakeSystem.ControllerRunningLock)
                    {
                        lead.BrakeSystem.ControllerRunningLock = true;
                    }
                    else if (lead.TrainBrakeController.TrainBrakeControllerState != ControllerState.Running) // Only reset lock when moved to another controller position
                    {
                        lead.BrakeSystem.ControllerRunningLock = false;
                    }

                    /*  // For testing purposes
                          Trace.TraceInformation("Brake Test - Volume {0} Release Rate {1} Charging Rate {2}", train.TotalTrainBrakeSystemVolumeM3, ReleaseNetBPLossGainPSI, lead.BrakePipeChargingRatePSIorInHgpS);
                          Trace.TraceInformation("Large Ejector Raw {0} Large Ejector (VB) {1} Ad Large Ejector {2}", lead.LargeEjectorBrakePipeChargingRatePSIorInHgpS, LargeEjectorChargingRateInHgpS, AdjLargeEjectorChargingRateInHgpS);
                          Trace.TraceInformation("Small Ejector Raw {0} Small Ejector (VB) {1} Ad Small Ejector {2}", lead.SmallEjectorBrakePipeChargingRatePSIorInHgpS, SmallEjectorChargingRateInHgpS, AdjSmallEjectorChargingRateInHgpS);
                          Trace.TraceInformation("Pipe Loss - Raw {0} Adj {1}", lead.TrainBrakePipeLeakPSIorInHgpS, AdjTrainPipeLeakLossPSI);
                    */

                    // Adjust brake pipe pressure according to various brake controls. Two modes are considered
                    //  - EQ where brake system is fitted with EQ reservoir, and lead locomotive uses the equalising pressure to set brake pipe
                    // - Non EQ, where no equalising reservoir is fitted, and brake controller must be held in release or application position until brake pipe reaches the desired vacuum
                    if (lead.VacuumBrakeEQFitted) // Is an equalising reservoir fitted
                    {
                        if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.VacApplyContServ)
                        {
                            // Vac Apply Cont Service - allows brake to be applied with an increasing rate. In other words the further that the valve is opened then the faster the brakes are applied.
                            // Emergency operation would be equivalent to 100%, normal operation may only require the brake controller to be set at less then 50%
                            // Brake valve control position is determined by DesiredPipeVacuum pressure, and the full pressure is used to determine a fraction. This fraction is then used to determine
                            // the size of the valve opening.

                            // This section uses a linear transition between the zero application rate (at 0% on control valve) and the emergency application rate (at 100% on control valve)
                            // Thus as the valve is opened further then the rate at which the vacuum is destroyed increases
                            float VacuumPressureDifference = (OneAtmospherePSI - MaxVacuumPipeLevelPSI);
                            float BrakeValveOpeningFraction = (DesiredPipeVacuum - VacuumPressureDifference) / MaxVacuumPipeLevelPSI;
                            float ApplyIncreaseGradient = TrainPipeTimeVariationS / AdjBrakeEmergencyTimeFactorPSIpS;
                            float VacApplyServiceTimeFactorS = (1 + ApplyIncreaseGradient * BrakeValveOpeningFraction);
                            VacApplyServiceTimeFactorS = MathHelper.Clamp(VacApplyServiceTimeFactorS, 1.0f, VacApplyServiceTimeFactorS);  // Make sure service time does not go below 1

                            if (VacApplyServiceTimeFactorS != 0)  // Don't make any changes if increase value is zero
                            {
                                // Adjust brake pipe value as appropriate
                                lead.BrakeSystem.BrakeLine1PressurePSI *= VacApplyServiceTimeFactorS;
                                if (lead.BrakeSystem.BrakeLine1PressurePSI > OneAtmospherePSI)
                                    lead.BrakeSystem.BrakeLine1PressurePSI = OneAtmospherePSI;
                            }
                        }

                        // Vacuum Pipe is < Desired value - increase brake pipe pressure (decrease vacuum value) - PSI goes from approx 4.189 to 14.5 - applying brakes

                        if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Emergency && lead.BrakeSystem.BrakeLine1PressurePSI < DesiredPipeVacuum)
                        {
                            // In emergency position brake pipe vacuum is reduced based upon the emergency time factor
                            lead.BrakeSystem.BrakeLine1PressurePSI *= (1 + TrainPipeTimeVariationS / AdjBrakeEmergencyTimeFactorPSIpS);

                            if (lead.BrakeSystem.BrakeLine1PressurePSI > OneAtmospherePSI)
                                lead.BrakeSystem.BrakeLine1PressurePSI = OneAtmospherePSI;
                        }

                        else if (lead.BrakeSystem.BrakeLine1PressurePSI < DesiredPipeVacuum)
                        {
                            // Vacuum Pipe is < Desired value - increase brake pipe pressure (decrease vacuum value) - PSI goes from approx 4.189 to 14.5 - applying brakes
                            lead.BrakeSystem.BrakeLine1PressurePSI *= (1 + TrainPipeTimeVariationS / AdjBrakeServiceTimeFactorPSIpS);
                            if (lead.BrakeSystem.BrakeLine1PressurePSI > DesiredPipeVacuum)
                                lead.BrakeSystem.BrakeLine1PressurePSI = DesiredPipeVacuum;
                        }

                        else if (lead.BrakeSystem.BrakeLine1PressurePSI > DesiredPipeVacuum) // Releasing brakes
                        {
                            // Exhauster flag
                            lead.VacuumExhausterIsOn = true;

                            // Vacuum Pipe is < Desired value - decrease brake pipe value pressure - PSI goes from 14.5 to 4.189 - releasing brakes

                            float TrainPipePressureDiffPSI = TrainPipeTimeVariationS * EQReleaseNetBPLossGainPSI; // Exhauster needs to be considered

                            // If Diff is going to take BP vacuum below desired pipe vacuum value, then only do the difference between the two
                            if (lead.BrakeSystem.BrakeLine1PressurePSI - TrainPipePressureDiffPSI < DesiredPipeVacuum)
                            {
                                TrainPipePressureDiffPSI = lead.BrakeSystem.BrakeLine1PressurePSI - DesiredPipeVacuum;
                            }


                            // If Diff is going to take BP vacuum below the value in the Main Reservoir, then only do the difference between the two (remember this is in PSIA)
//                            if (lead.BrakeSystem.BrakeLine1PressurePSI - TrainPipePressureDiffPSI < lead.VacuumMainResVacuumPSIAorInHg)
//                            {
//                                TrainPipePressureDiffPSI = lead.VacuumMainResVacuumPSIAorInHg - lead.BrakeSystem.BrakeLine1PressurePSI;
//                            }


//                            if (TrainPipePressureDiffPSI < 0 || lead.VacuumMainResVacuumPSIAorInHg > lead.BrakeSystem.BrakeLine1PressurePSI)
//                                TrainPipePressureDiffPSI = 0;

                            // Adjust brake pipe pressure based upon pressure differential
                            // If pipe leakage and brake control valve is in LAP position then pipe is connected to main reservoir and maintained at equalising pressure from reservoir
                            // All other brake states will have the brake pipe connected to the main reservoir, and therefore leakage will be compenstaed by air from main reservoir
                            // Modern self lap brakes will maintain pipe pressure using air from main reservoir

                            if (lead.TrainBrakeController.TrainBrakeControllerState != ControllerState.Lap)
                            {
                                lead.BrakeSystem.BrakeLine1PressurePSI -= TrainPipePressureDiffPSI;  // Increase brake pipe pressure to cover loss in vacuum pipe
//                                float VolDiffM3 = (train.TotalTrainBrakeSystemVolumeM3 / lead.VacuumBrakesMainResVolumeM3);
//                                lead.VacuumMainResVacuumPSIAorInHg += TrainPipePressureDiffPSI * VolDiffM3;
//                                if (lead.VacuumMainResVacuumPSIAorInHg > OneAtmospherePSI)
//                                    lead.VacuumMainResVacuumPSIAorInHg = OneAtmospherePSI; // Ensure Main Res does not go negative
                            }
                            // else in LAP psoition brake pipe is isolated, and thus brake pipe pressure decreases, but reservoir remains at same pressure
                            else
                            {
                                lead.VacuumExhausterIsOn = false;
                            }
                        }
                    }

                    else  // No equalising reservoir fitted
                    {

                        if (lead.EngineType == TrainCar.EngineTypes.Steam && lead.TrainBrakePipeLeakPSIorInHgpS != 0 && (lead.BrakeSystem.BrakeLine1PressurePSI + (TrainPipeTimeVariationS * RunningNetBPLossGainPSI)) < OneAtmospherePSI && lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Running)
                        {
                            // Allow for leaking train brakepipe (value is determined for lead locomotive) 
                            // For diesel and electric locomotives assume that the Vacuum pump is automatic, and therefore bp leakage has no discernable impact.
                            lead.BrakeSystem.BrakeLine1PressurePSI += TrainPipeTimeVariationS * RunningNetBPLossGainPSI;
                        }

                        // Lap position for diesels and electric locomotives
                        // In this position the BP is isolated from the small ejector or exhauster, and hence will suffer leakage
                        else if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Lap && (lead.BrakeSystem.BrakeLine1PressurePSI + (TrainPipeTimeVariationS * AdjTrainPipeLeakLossPSI)) < OneAtmospherePSI)
                        {
                            lead.BrakeSystem.BrakeLine1PressurePSI += TrainPipeTimeVariationS * LapNetBPLossGainPSI;
                        }

                        // If no leakage, ie not in Running position, adjust the train pipe up and down as appropriate.
                        // Brake Controller is in Emergency position - fast increase brake pipe pressure (decrease vacuum value) - PSI goes from approx 4.189 to 14.5 - applying brakes
                        else if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Emergency)
                        {
                            lead.BrakeSystem.BrakeLine1PressurePSI *= (1 + TrainPipeTimeVariationS / AdjBrakeEmergencyTimeFactorPSIpS);

                            if (lead.BrakeSystem.BrakeLine1PressurePSI > OneAtmospherePSI)
                                lead.BrakeSystem.BrakeLine1PressurePSI = OneAtmospherePSI;
                        }


                        // Brake Controller is in Apply position - increase brake pipe pressure (decrease vacuum value) - PSI goes from approx 4.189 to 14.5 - applying brakes
                        else if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Apply)
                        {
                            lead.BrakeSystem.BrakeLine1PressurePSI *= (1 + TrainPipeTimeVariationS / AdjBrakeServiceTimeFactorPSIpS);
                            if (lead.BrakeSystem.BrakeLine1PressurePSI > OneAtmospherePSI)
                                lead.BrakeSystem.BrakeLine1PressurePSI = OneAtmospherePSI;
                        }

                        // Brake Controller is in Release position - decrease brake pipe value pressure - PSI goes from 14.5 to 4.189 - releasing brakes
                        else if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Release)
                        {
                            
                            float TrainPipePressureDiffPSI = 0;
                            if (lead.EngineType == TrainCar.EngineTypes.Diesel || lead.EngineType == TrainCar.EngineTypes.Electric || lead.EngineType == TrainCar.EngineTypes.Control)
                            {
                                // diesel and electric locomotives use vacuum exhauster
                                TrainPipePressureDiffPSI = TrainPipeTimeVariationS * EQReleaseNetBPLossGainPSI;
                            }
                            else
                            {
                                // steam locomotives use vacuum ejector
                                TrainPipePressureDiffPSI = TrainPipeTimeVariationS * ReleaseNetBPLossGainPSI;
                            }
                            lead.BrakeSystem.BrakeLine1PressurePSI -= TrainPipePressureDiffPSI;
                        }

                        // Brake Controller is in Fast Release position - decrease brake pipe value pressure - PSI goes from 14.5 to 4.189 - releasing brakes
                        else if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.FullQuickRelease)
                        {
                            float TrainPipePressureDiffPSI = TrainPipeTimeVariationS * QuickReleaseNetBPLossGainPSI;
                            lead.BrakeSystem.BrakeLine1PressurePSI -= TrainPipePressureDiffPSI;
                        }

                        // Brake Controller is in Lap position - increase brake pipe pressure (decrease vacuum value) - PSI goes from approx 4.189 to 14.5 due to leakage - applying brakes
                        else if (lead.TrainBrakePipeLeakPSIorInHgpS != 0 && (lead.BrakeSystem.BrakeLine1PressurePSI + (TrainPipeTimeVariationS * TrainPipeLeakLossPSI)) < OneAtmospherePSI && lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Lap)
                        {
                            lead.BrakeSystem.BrakeLine1PressurePSI += TrainPipeTimeVariationS * TrainPipeLeakLossPSI; // Pipe pressure will increase (ie vacuum is destroyed) due to leakage, no compensation as BP is isolated from everything
                        }

                        else if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.VacContServ)
                        {
                            // Vac Cont Service allows the brake to be moved continuously between the ON and OFF position. Once stationary the brake will be held at the level set
                            // Simulates turning steam onto the ejector, and adjusting the rate to get desired outcome out of ejector

                            if (lead.BrakeSystem.BrakeLine1PressurePSI < DesiredPipeVacuum)
                            {
                                // Vacuum Pipe is < Desired value - increase brake pipe pressure (decrease vacuum value) - PSI goes from approx 4.189 to 14.5 - applying brakes
                                lead.BrakeSystem.BrakeLine1PressurePSI *= (1 + TrainPipeTimeVariationS / AdjBrakeServiceTimeFactorPSIpS);
                                if (lead.BrakeSystem.BrakeLine1PressurePSI > OneAtmospherePSI)
                                    lead.BrakeSystem.BrakeLine1PressurePSI = OneAtmospherePSI;
                            }
                            else if (lead.BrakeSystem.BrakeLine1PressurePSI > DesiredPipeVacuum)
                            {
                                // Vacuum Pipe is > Desired value - decrease brake pipe value pressure - PSI goes from 14.5 to 4.189 - releasing brakes
                                float TrainPipePressureDiffPSI = TrainPipeTimeVariationS * ReleaseNetBPLossGainPSI;
                                if (lead.BrakeSystem.BrakeLine1PressurePSI - TrainPipePressureDiffPSI < DesiredPipeVacuum)
                                    TrainPipePressureDiffPSI = lead.BrakeSystem.BrakeLine1PressurePSI - DesiredPipeVacuum;
                                lead.BrakeSystem.BrakeLine1PressurePSI -= TrainPipePressureDiffPSI;

                            }
                        }
                        else if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.VacApplyContServ)
                        {
                            // Vac Apply Cont Service - allows brake to be applied with an increasing rate. In other words the further that the valve is opened then the faster the brakes are applied.
                            // Emergency operation would be equivalent to 100%, normal operation may only require the brake controller to be set at less then 50%
                            // Brake valve control position is determined by DesiredPipeVacuum pressure, and the full pressure is used to determine a fraction. This fraction is then used to determine
                            // the size of the valve opening.

                            // This section uses a linear transition between the zero application rate (at 0% on control valve) and the emergency application rate (at 100% on control valve)
                            // Thus as the valve is opened further then the rate at which the vacuum is destroyed increases
                            float VacuumPressureDifference = (OneAtmospherePSI - MaxVacuumPipeLevelPSI);
                            float BrakeValveOpeningFraction = (DesiredPipeVacuum - VacuumPressureDifference) / MaxVacuumPipeLevelPSI;
                            float ApplyIncreaseGradient = TrainPipeTimeVariationS / AdjBrakeEmergencyTimeFactorPSIpS;
                            float VacApplyServiceTimeFactorS = (1 + ApplyIncreaseGradient * BrakeValveOpeningFraction);
                            VacApplyServiceTimeFactorS = MathHelper.Clamp(VacApplyServiceTimeFactorS, 1.0f, VacApplyServiceTimeFactorS);  // Make sure service time does not go below 1

                            if (VacApplyServiceTimeFactorS != 0)  // Don't make any changes if increase value is zero
                            {
                                // Adjust brake pipe value as appropriate
                                lead.BrakeSystem.BrakeLine1PressurePSI *= VacApplyServiceTimeFactorS;
                                if (lead.BrakeSystem.BrakeLine1PressurePSI > OneAtmospherePSI)
                                    lead.BrakeSystem.BrakeLine1PressurePSI = OneAtmospherePSI;
                            }
                        }
                    }
                    // Keep brake line within relevant limits - ie between 21 or 25 InHg and Atmospheric pressure.
                    lead.BrakeSystem.BrakeLine1PressurePSI = MathHelper.Clamp(lead.BrakeSystem.BrakeLine1PressurePSI, OneAtmospherePSI - MaxVacuumPipeLevelPSI, OneAtmospherePSI);

                }

                // Propogate lead brake line pressure from lead locomotive along the train to each car
                TrainCar car0 = train.Cars[0];

                float p0 = car0.BrakeSystem.BrakeLine1PressurePSI;
                p0 = MathHelper.Clamp(p0, OneAtmospherePSI - MaxVacuumPipeLevelPSI, OneAtmospherePSI);
                float Car0brakePipeVolumeM3 = car0.BrakeSystem.BrakePipeVolumeM3;
                float Car0brakeCylVolumeM3 = car0.BrakeSystem.GetCylVolumeM3();
                float Car0numBrakeCyl = car0.BrakeSystem.GetVacBrakeCylNumber();

#if DEBUG_TRAIN_PIPE_LEAK

                Trace.TraceInformation("======================================= Train Pipe Leak (VacuumSinglePipe) ===============================================");
                Trace.TraceInformation("Charging Rate {0}  ServiceTimeFactor {1}", lead.BrakePipeChargingRatePSIorInHgpS, lead.BrakeServiceTimeFactorS);
                Trace.TraceInformation("Before:  CarID {0}  TrainPipeLeak {1} Lead BrakePipe Pressure {2}", trainCar.CarID, lead.TrainBrakePipeLeakPSIorInHgpS, lead.BrakeSystem.BrakeLine1PressurePSI);
                Trace.TraceInformation("Brake State {0}", lead.TrainBrakeController.TrainBrakeControllerState);
                Trace.TraceInformation("Small Ejector {0} Large Ejector {1}", lead.SmallSteamEjectorIsOn, lead.LargeSteamEjectorIsOn);

#endif

                foreach (TrainCar car in train.Cars)
                {
                    float Car0BrakeSytemVolumeM30 = 0.0f;
                    float CarBrakeSytemVolumeM3 = 0.0f;
                    float CarnumBrakeCyl = car.BrakeSystem.GetVacBrakeCylNumber();
                    float CarbrakeCylVolumeM3 = car.BrakeSystem.GetCylVolumeM3();
                    float CarbrakePipeVolumeM3 = car.BrakeSystem.BrakePipeVolumeM3;

                    // This section calculates the current brake system volumes on each vehicle
                    // These volumes are converted to a fraction which then is used to proportion the change in vacuum to each car along the train
                    // If the vehicle has a brake cylinder fitted then calculate the car brake system volume ( brake cylinder and BP). 
                    //This value is used later to average the pressure during propagation along the train.

                    Car0BrakeSytemVolumeM30 = Car0brakePipeVolumeM3 / (Car0brakePipeVolumeM3 + car.BrakeSystem.BrakePipeVolumeM3);

                    CarBrakeSytemVolumeM3 = CarbrakePipeVolumeM3 / (Car0brakePipeVolumeM3 + car.BrakeSystem.BrakePipeVolumeM3);

                    float p1 = car.BrakeSystem.BrakeLine1PressurePSI;
                    p1 = MathHelper.Clamp(p1, OneAtmospherePSI - MaxVacuumPipeLevelPSI, OneAtmospherePSI);

                    // This section is for normal train brake operation provided the TP is intact. Note if a valve along the train is closed, effectively creating a 
                    // "closed section", then this section will be skipped and the pressure will remain the same.
                    if (car == train.Cars[0] || car.BrakeSystem.FrontBrakeHoseConnected && car.BrakeSystem.AngleCockAOpen && car0.BrakeSystem.AngleCockBOpen)
                    {

                        // Check to see if extra cars have just been coupled to train, if so initialise brake pressures - assume brake pipe is at atmospheric pressure - ie brakes are on
                        if (car.Train.Cars.Count > car.Train.PreviousCarCount && car.Train.PreviousCarCount != 0)
                        {
                            car0.BrakeSystem.BrakeLine1PressurePSI = OneAtmospherePSI;
                            p0 = OneAtmospherePSI;
                            car.BrakeSystem.BrakeLine1PressurePSI = OneAtmospherePSI;
                            p1 = OneAtmospherePSI;
                        }

                        float TrainPipePressureDiffPropogationPSI;
                        if (AdjbrakePipeTimeFactorS == 0) // Check to make sure that TrainPipePressureDiffPropogationPSI is calculated as a valid number, ie not NaN
                        {
                            TrainPipePressureDiffPropogationPSI = 0.0f;
                        }
                        else
                        {
                            TrainPipePressureDiffPropogationPSI = TrainPipeTimeVariationS * (p1 - p0) / AdjbrakePipeTimeFactorS;
                        }

                        // Check to see if BP Pipe Diff pressure is an invalid number, typically when coupling new cars
                        if (float.IsNaN(TrainPipePressureDiffPropogationPSI))
                        {
                            if (car.Train.Cars.Count > car.Train.PreviousCarCount && car.Train.PreviousCarCount != 0)
                            {
                                TrainPipePressureDiffPropogationPSI = 0.0f;
                            }
                        }
                        else
                        {
                            // The brake pipe is evacuated at a quicker rate then it is charged at - PressDiff increased to represent this
                            if (TrainPipePressureDiffPropogationPSI < 0)
                                TrainPipePressureDiffPropogationPSI *= AdjBrakePipeDischargeTimeFactor;
                        }

                        // The locomotive BP should not be changed during the propagation process, as it is calculated above, and acts as the reference. This ensures that the BP vacuum setting calculated
                        // above for the locomotive remains as an accurate timing value
                        // Two scenarios considered, one locomotive is lead vehicle, or locomotive is in the train consist somewhere
                        if (train.Cars[0] == lead) // locomotive at head of train
                        {
                            if (car != lead) // Don't change BP pressure on the locomotive car in either direction if the locomotive is at the head of the train
                            {
                                // Start propagating pressure along train BP by averaging pressure across each car down the train
                                if ((car0 == lead) && train.TrainBPIntact) // For the car after the locomotive, only decrease the car itself, and not the locomotive. 
                                                                           // If previous car BP pressure is increased then the total proagation time is increased, as there is a "fight" between the lead BP pressure, 
                                                                           // and the propagation BP pressure as it evens out along the train
                                {
                                    car.BrakeSystem.BrakeLine1PressurePSI -= TrainPipePressureDiffPropogationPSI * Car0BrakeSytemVolumeM30;
                                    car.BrakeSystem.BrakeLine1PressurePSI = MathHelper.Clamp(car.BrakeSystem.BrakeLine1PressurePSI, OneAtmospherePSI - MaxVacuumPipeLevelPSI, OneAtmospherePSI);

                                }
                                else  // For all other "normal" cars
                                {
                                    car.BrakeSystem.BrakeLine1PressurePSI -= TrainPipePressureDiffPropogationPSI * Car0BrakeSytemVolumeM30;
                                    car.BrakeSystem.BrakeLine1PressurePSI = MathHelper.Clamp(car.BrakeSystem.BrakeLine1PressurePSI, OneAtmospherePSI - MaxVacuumPipeLevelPSI, OneAtmospherePSI);
                                    // These lines allow pressure propagation from the rear of the train twoards the front
                                    car0.BrakeSystem.BrakeLine1PressurePSI += TrainPipePressureDiffPropogationPSI * CarBrakeSytemVolumeM3;
                                    car0.BrakeSystem.BrakeLine1PressurePSI = MathHelper.Clamp(car0.BrakeSystem.BrakeLine1PressurePSI, OneAtmospherePSI - MaxVacuumPipeLevelPSI, OneAtmospherePSI);
                                }
                            }
                        }
                        else // if the locomotive is located elsewhere in train then we need to disable change to BP pressure on the locomotive car but maintain forward and rearwards pressure propagation on adjoining cars
                        {
                            // Start propagating pressure along train BP by averaging pressure across each car down the train
                            if ((car0 == lead) && train.TrainBPIntact) // For the car after the locomotive, only decrease the car itself, and not the locomotive. 
                                                                       // If previous car BP pressure is increased then the total proagation time is increased, 
                                                                       // as there is a "fight" between the lead BP pressure, and the propagation BP pressure as it evens out along the train
                            {
                                car.BrakeSystem.BrakeLine1PressurePSI -= TrainPipePressureDiffPropogationPSI * Car0BrakeSytemVolumeM30;
                                car.BrakeSystem.BrakeLine1PressurePSI = MathHelper.Clamp(car.BrakeSystem.BrakeLine1PressurePSI, OneAtmospherePSI - MaxVacuumPipeLevelPSI, OneAtmospherePSI);
                            }

                            else if ((car == lead) && train.TrainBPIntact) // For the locomotive, as it is not the lead car, it needs to change the pressure of the car in front of it.
                            {
                                // These lines allow pressure propagation from the rear of the train twoards the front
                                car0.BrakeSystem.BrakeLine1PressurePSI += TrainPipePressureDiffPropogationPSI * CarBrakeSytemVolumeM3;
                                car0.BrakeSystem.BrakeLine1PressurePSI = MathHelper.Clamp(car0.BrakeSystem.BrakeLine1PressurePSI, OneAtmospherePSI - MaxVacuumPipeLevelPSI, OneAtmospherePSI);

                            }
                            else  // For all other "normal" cars
                            {
                                car.BrakeSystem.BrakeLine1PressurePSI -= TrainPipePressureDiffPropogationPSI * Car0BrakeSytemVolumeM30;
                                car.BrakeSystem.BrakeLine1PressurePSI = MathHelper.Clamp(car.BrakeSystem.BrakeLine1PressurePSI, OneAtmospherePSI - MaxVacuumPipeLevelPSI, OneAtmospherePSI);
                                // These lines allow pressure propagation from the rear of the train twoards the front
                                car0.BrakeSystem.BrakeLine1PressurePSI += TrainPipePressureDiffPropogationPSI * CarBrakeSytemVolumeM3;
                                car0.BrakeSystem.BrakeLine1PressurePSI = MathHelper.Clamp(car0.BrakeSystem.BrakeLine1PressurePSI, OneAtmospherePSI - MaxVacuumPipeLevelPSI, OneAtmospherePSI);
                            }
                        }
                    }

                    // The following section adjusts the brake pipe pressure if the BP is disconnected or broken, eg when shunting, etc. 
                    // If it has broken then brake pipe pressure will rise (vacuum goes to 0 InHg), and brakes will apply
                    if (!car.BrakeSystem.FrontBrakeHoseConnected) // Brake pipe broken
                    {
                        if (car.BrakeSystem.AngleCockAOpen)  //  AND Front brake cock opened
                        {

                            // release vacuum pressure if train brake pipe is "open". Make sure that we stay within bound
                            if ((car.BrakeSystem.BrakeLine1PressurePSI + (TrainPipeTimeVariationS * (p1) / AdjbrakePipeTimeFactorS)) > OneAtmospherePSI)
                            {
                                car.BrakeSystem.BrakeLine1PressurePSI = OneAtmospherePSI;
                            }
                            else
                            {
                                car.BrakeSystem.BrakeLine1PressurePSI += TrainPipeTimeVariationS * (p1) / AdjbrakePipeTimeFactorS;
                            }
                        }

                        if (car0.BrakeSystem.AngleCockBOpen && car != car0)  //  AND Rear cock of wagon opened, and car is not the previous wagon
                                                                             // appears to be the case when a locomotive (steam?) connects to the rear of the train.
                        {

                            // release vacuum pressure if train brake pipe is "open". Make sure that we stay within bound
                            if ((car0.BrakeSystem.BrakeLine1PressurePSI + (TrainPipeTimeVariationS * (p0) / AdjbrakePipeTimeFactorS)) > OneAtmospherePSI)
                            {
                                car0.BrakeSystem.BrakeLine1PressurePSI = OneAtmospherePSI;
                            }
                            else
                            {
                                car0.BrakeSystem.BrakeLine1PressurePSI += TrainPipeTimeVariationS * (p0) / AdjbrakePipeTimeFactorS;
                            }

                            train.Cars[0].BrakeSystem.BrakeLine1PressurePSI = car0.BrakeSystem.BrakeLine1PressurePSI;
                        }
                        car.BrakeSystem.CarBPIntact = false;
                    }
                    else
                    {
                        car.BrakeSystem.CarBPIntact = true;
                    }

                    // Allows for locomotive to be uncouled, and brakes to apply, even though brake hose is not shown disconnected.
                    // If positioned at front of train
                    if (car0.BrakeSystem.AngleCockAOpen && car == train.Cars[0])
                    {
                        // release vacuum pressure if train brake pipe is "open". Make sure that we stay within bound
                        if ((car0.BrakeSystem.BrakeLine1PressurePSI + (TrainPipeTimeVariationS * (p0) / AdjbrakePipeTimeFactorS)) > OneAtmospherePSI)
                        {
                            car0.BrakeSystem.BrakeLine1PressurePSI = OneAtmospherePSI;
                        }
                        else
                        {
                            car0.BrakeSystem.BrakeLine1PressurePSI += TrainPipeTimeVariationS * (p0) / AdjbrakePipeTimeFactorS;
                        }

                        car.BrakeSystem.CarBPIntact = false;
                    }
                    else
                    {
                        car.BrakeSystem.CarBPIntact = true;
                    }


                    // This monitors the last car in the train, and if the valve is open then BP pressure will be maintained at atmospheric (eg brakes in applied state)
                    // When valve is closed then pressure will be able to drop, and return to normal
                    if (car == train.Cars[train.Cars.Count - 1] && car.BrakeSystem.AngleCockBOpen)
                    {
                        // Test to make sure that BP pressure stays within reasonable bounds
                        if (AdjbrakePipeTimeFactorS == 0)
                        {
                            car.BrakeSystem.BrakeLine1PressurePSI = p1;
                        }
                        else if ((car.BrakeSystem.BrakeLine1PressurePSI + (TrainPipeTimeVariationS * (p1) / AdjbrakePipeTimeFactorS)) > OneAtmospherePSI)
                        {
                            car.BrakeSystem.BrakeLine1PressurePSI = OneAtmospherePSI;
                        }
                        else
                        {
                            car.BrakeSystem.BrakeLine1PressurePSI += TrainPipeTimeVariationS * (p1) / AdjbrakePipeTimeFactorS;
                        }

                        car.BrakeSystem.CarBPIntact = false;
                    }
                    else
                    {
                        car.BrakeSystem.CarBPIntact = true;
                    }

                    // Keep relevant brake line within relevant limits - ie 21 or 25 InHg (approx 4.185 psi) and 0 InHg (Atmospheric pressure)
                    car0.BrakeSystem.BrakeLine1PressurePSI = MathHelper.Clamp(car0.BrakeSystem.BrakeLine1PressurePSI, OneAtmospherePSI - MaxVacuumPipeLevelPSI, OneAtmospherePSI);
                    car.BrakeSystem.BrakeLine1PressurePSI = MathHelper.Clamp(car.BrakeSystem.BrakeLine1PressurePSI, OneAtmospherePSI - MaxVacuumPipeLevelPSI, OneAtmospherePSI);
                    train.Cars[0].BrakeSystem.BrakeLine1PressurePSI = MathHelper.Clamp(train.Cars[0].BrakeSystem.BrakeLine1PressurePSI, OneAtmospherePSI - MaxVacuumPipeLevelPSI, OneAtmospherePSI);
                    // Prepare to move values along one car in the train
                    car0 = car;
                    p0 = car.BrakeSystem.BrakeLine1PressurePSI;
                    Car0brakePipeVolumeM3 = CarbrakePipeVolumeM3;
                    Car0brakeCylVolumeM3 = CarbrakeCylVolumeM3;
                    Car0numBrakeCyl = CarnumBrakeCyl;
                }
                // Record the current number of cars in the train. This will allow comparison to determine if other cars are coupled to the train
                train.PreviousCarCount = train.Cars.Count;
            }

            // Test to see if the brake pipe is intact or has been opened.
            for (int i = 0; i < train.Cars.Count; i++)
            {
                if (train.Cars[i].BrakeSystem.CarBPIntact == false)
                {
                    train.TrainBPIntact = false;
                    break;
                }
                else
                {
                    train.TrainBPIntact = true;
                }
            }

            // **************  Engine Brake *************
            // Propagate engine brake pipe (#3) data

            train.FindLeadLocomotives(out int first, out int last);
            int continuousFromInclusive = 0;
            int continuousToExclusive = train.Cars.Count;

            for (int i = 0; i < train.Cars.Count; i++)
            {

                if (lead != null)
                {

                    // Next section forces wagons not condidered to be locomotives or tenders out of this calculation and thus their Brakeline3 values set to zero. This used above to identify which BC to change
                    BrakeSystem brakeSystem = train.Cars[i].BrakeSystem;
                    if (lead.EngineBrakeFitted)
                    {

                        if (i < first && (!train.Cars[i + 1].BrakeSystem.FrontBrakeHoseConnected || !brakeSystem.AngleCockBOpen || !train.Cars[i + 1].BrakeSystem.AngleCockAOpen))
                        {
                            if (continuousFromInclusive < i + 1)
                            {
                                continuousFromInclusive = i + 1;
                                brakeSystem.BrakeLine3PressurePSI = 0;
                            }
                            continue;
                        }
                        if (i > last && i > 0 && (!brakeSystem.FrontBrakeHoseConnected || !brakeSystem.AngleCockAOpen || !train.Cars[i - 1].BrakeSystem.AngleCockBOpen))
                        {
                            if (continuousToExclusive > i)
                                continuousToExclusive = i;
                            brakeSystem.BrakeLine3PressurePSI = 0;
                            continue;
                        }

                        // Collect and propagate engine brake pipe (3) data
                        // This appears to be calculating the engine brake cylinder pressure???
                        if (i < first || i > last) // This loop rarely used as the above exclusion and inclusion process excludes non-locomotive cars
                        {
                            brakeSystem.BrakeLine3PressurePSI = 0;
                        }
                        else
                        {

                            // Engine Brake Controller is in Apply position - increase brake pipe pressure (decrease vacuum value) - PSI goes from approx 4.189 to 14.5 - applying brakes
                            if (lead.EngineBrakeController.TrainBrakeControllerState == ControllerState.Apply)
                            {
                                brakeSystem.BrakeLine3PressurePSI += elapsedClockSeconds * lead.EngineBrakeController.ApplyRatePSIpS;
                                if (brakeSystem.BrakeLine3PressurePSI > OneAtmospherePSI)
                                    brakeSystem.BrakeLine3PressurePSI = OneAtmospherePSI;
                            }

                            // Engine Brake Controller is in Apply position - increase brake pipe pressure (decrease vacuum value) - PSI goes from approx 4.189 to 14.5 - applying brakes
                            if (lead.EngineBrakeController.TrainBrakeControllerState == ControllerState.Emergency)
                            {
                                brakeSystem.BrakeLine3PressurePSI += elapsedClockSeconds * lead.EngineBrakeController.EmergencyRatePSIpS;
                                if (brakeSystem.BrakeLine3PressurePSI > OneAtmospherePSI)
                                    brakeSystem.BrakeLine3PressurePSI = OneAtmospherePSI;
                            }

                            // Engine Brake Controller is in Release position - decrease brake pipe value pressure - PSI goes from 14.5 to 4.189 - releasing brakes
                            else if (lead.EngineBrakeController.TrainBrakeControllerState == ControllerState.Release)
                            {
                                float EnginePipePressureDiffPSI = elapsedClockSeconds * lead.EngineBrakeController.ReleaseRatePSIpS;
                                brakeSystem.BrakeLine3PressurePSI -= EnginePipePressureDiffPSI;
                                if (brakeSystem.BrakeLine3PressurePSI < OneAtmospherePSI - MaxVacuumPipeLevelPSI)
                                    brakeSystem.BrakeLine3PressurePSI = OneAtmospherePSI - MaxVacuumPipeLevelPSI;
                            }
                            else if (lead.EngineBrakeController.TrainBrakeControllerState == ControllerState.VacContServ || lead.EngineBrakeController.TrainBrakeControllerState == ControllerState.BrakeNotch)
                            {
                                // Vac Cont Service allows the brake to be moved continuously between the ON and OFF position. Once stationary the brake will be held at the level set
                                // Simulates turning steam onto the ejector, and adjusting the rate to get desired outcome out of ejector

                                // Desired Vacuum pipe level must operate between full vacuum level (eg 2.278 psi = 25 inhg = Release) and atmospheric pressure (14.503psi = 0 inhg = Apply). 
                                // The equalising pressure operates between 0 (Apply) and full pipe vacuum (12.278psi = 25 inHg - Release), which is the reverse of above, so it needs to be mapped to
                                // provide a desired vacuum of 2.278 psi = 25 inhg = Release and 14.503psi = 0 inhg = Apply
                                // Hence Desired = Control Vale % * Vacuum Rise + base Vacuum.

                                // Calculate desired brake pressure from engine brake valve setting
                                float BrakeSettingValue = lead.EngineBrakeController.CurrentValue;
                                float EngineDesiredPipeVacuum = (BrakeSettingValue * (OneAtmospherePSI - (OneAtmospherePSI - MaxVacuumPipeLevelPSI))) + (OneAtmospherePSI - MaxVacuumPipeLevelPSI);

                                if (lead.BrakeSystem.BrakeLine3PressurePSI < EngineDesiredPipeVacuum)
                                {
                                    // Vacuum Pipe is < Desired value - increase brake pipe pressure (decrease vacuum value) - PSI goes from approx 4.189 to 14.5 - applying brakes
                                    brakeSystem.BrakeLine3PressurePSI += elapsedClockSeconds * lead.EngineBrakeController.ApplyRatePSIpS;
                                    if (brakeSystem.BrakeLine3PressurePSI > OneAtmospherePSI)
                                        brakeSystem.BrakeLine3PressurePSI = OneAtmospherePSI;
                                }
                                else if (lead.BrakeSystem.BrakeLine3PressurePSI > EngineDesiredPipeVacuum)
                                {
                                    // Vacuum Pipe is > Desired value - decrease brake pipe value pressure - PSI goes from 14.5 to 4.189 - releasing brakes
                                    float EnginePipePressureDiffPSI = elapsedClockSeconds * lead.EngineBrakeController.ReleaseRatePSIpS;

                                    brakeSystem.BrakeLine3PressurePSI -= EnginePipePressureDiffPSI;

                                    if (brakeSystem.BrakeLine3PressurePSI < OneAtmospherePSI - MaxVacuumPipeLevelPSI)
                                        brakeSystem.BrakeLine3PressurePSI = OneAtmospherePSI - MaxVacuumPipeLevelPSI;

                                }
                            }

                        }
                    }
                    else
                    {
                        brakeSystem.BrakeLine3PressurePSI = 0; // Set engine brake line to zero if no engine brake fitted
                    }
                }
            }

        }

        public override float InternalPressure(float realPressure)
        {
            return Vac.ToPress(realPressure);
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
        public override void SetRetainer(RetainerSetting setting)
        {
        }

        public override void AISetPercent(float percent)
        {
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            Car.Train.EqualReservoirPressurePSIorInHg = Vac.FromPress(OneAtmospherePSI - MaxForcePressurePSI * (1 - percent / 100));
        }

        public override bool IsBraking()
        {
            if (CylPressurePSIA < MaxForcePressurePSI * 0.7)
                return true;
            return false;
        }

        public override void CorrectMaxCylPressurePSI(MSTSLocomotive loco)
        {

        }
    }
}
