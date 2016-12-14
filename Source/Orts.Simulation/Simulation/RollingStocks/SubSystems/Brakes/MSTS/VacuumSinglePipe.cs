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

using Orts.Common;
using Orts.Parsers.Msts;
using Orts.Simulation.Physics;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS
{
    public class VacuumSinglePipe : MSTSBrakeSystem
    {
        readonly static float OneAtmospherePSI = Bar.ToPSI(1);
        //const float OneAtmosphereKPa = 100;
        //const float OneAtmosphereInHg = 30;
        float MaxHandbrakeForceN;
        float MaxBrakeForceN = 89e3f;
        //float MaxForcePressurePSI = 21 * OneAtmospherePSIA / OneAtmosphereInHg;// relative pressure difference for max brake force
        float MaxForcePressurePSI = KPa.ToPSI(KPa.FromInHg(21));    // relative pressure difference for max brake force
        TrainCar Car;
        float HandbrakePercent;
        float CylPressurePSIA;
        float VacResPressurePSIA;  // vacuum reservior pressure with piston in released position
        // defaults based on information in http://www.lmsca.org.uk/lms-coaches/LMSRAVB.pdf
        int NumCylinders = 2;
        // brake cylinder volume with piston in applied position
        float CylVol = (float)((18 / 2) * (18 / 2) * 4.5 * Math.PI);
        // vacuum reservior volume with piston in released position
        float VacResVol = (float)((24 / 2) * (24 / 2) * 16 * Math.PI);
        float PipeVol = (float)((2 / 2) * (2 / 2) * 70 * 12 * Math.PI);
        // volume units need to be consistent but otherwise don't matter, defaults are cubic inches
        bool HasDirectAdmissionValue = false;
        float MaxReleaseRatePSIpS = 2.5f;
        float MaxApplicationRatePSIpS = 2.5f;
      //  float PipeTimeFactorS = .003f; // copied from air single pipe, probably not accurate
     //   float ReleaseTimeFactorS = 1.009f; // copied from air single pipe, but close to modern ejector data
    //    float ApplyChargingRatePSIpS = 4;
        bool TrainBrakePressureChanging = false;
        bool BrakePipePressureChanging = false;
        int SoundTriggerCounter = 0;
        float prevCylPressurePSIA = 0f;
        float prevBrakePipePressurePSI = 0f;
     //   private float TrainBrakePipeLeakPSIpS;


        public VacuumSinglePipe(TrainCar car)
        {
            Car = car;
            // taking into account very short (fake) cars to prevent NaNs in brake line pressures
            BrakePipeVolumeM3 = (0.050f * 0.050f * (float)Math.PI / 4f) * Math.Max(5.0f, (1 + car.CarLengthM)); // Using (2") pipe
        }

        public override bool GetHandbrakeStatus()
        {
            return HandbrakePercent > 0;
        }

        public override void InitializeFromCopy(BrakeSystem copy)
        {
            VacuumSinglePipe thiscopy = (VacuumSinglePipe)copy;
            MaxBrakeForceN = thiscopy.MaxBrakeForceN;
            MaxHandbrakeForceN = thiscopy.MaxHandbrakeForceN;
            MaxForcePressurePSI = thiscopy.MaxForcePressurePSI;
            MaxReleaseRatePSIpS = thiscopy.MaxReleaseRatePSIpS;
            MaxApplicationRatePSIpS = thiscopy.MaxApplicationRatePSIpS;
          //  ApplyChargingRatePSIpS = thiscopy.ApplyChargingRatePSIpS;
         //   PipeTimeFactorS = thiscopy.PipeTimeFactorS;
          //  ReleaseTimeFactorS = thiscopy.ReleaseTimeFactorS;
            NumCylinders = thiscopy.NumCylinders;
            CylVol = thiscopy.CylVol;
            PipeVol = thiscopy.PipeVol;
            VacResVol = thiscopy.VacResVol;
            HasDirectAdmissionValue = thiscopy.HasDirectAdmissionValue;
        }

        // convert vacuum in inhg to pressure in psia
        public static float V2P(float v)
        {
            return OneAtmospherePSI - Bar.ToPSI(Bar.FromInHg(v));
        }
        // convert pressure in psia to vacuum in inhg
        public static float P2V(float p)
        {
            return Bar.ToInHg(Bar.FromPSI(OneAtmospherePSI - p));
        }
        // return vacuum reservior pressure adjusted for piston movement
        float VacResPressureAdjPSIA()
        {
            if (VacResPressurePSIA >= CylPressurePSIA)
                return VacResPressurePSIA;
            float p = VacResPressurePSIA / (1 - CylVol / VacResVol);
            return p < CylPressurePSIA ? p : CylPressurePSIA;
        }

        public override string GetStatus(Dictionary<BrakeSystemComponent, PressureUnit> units)
        {
            return string.Format(" BP {0}", FormatStrings.FormatPressure(P2V(BrakeLine1PressurePSI), PressureUnit.InHg, PressureUnit.InHg, false));
        }

        public override string GetFullStatus(BrakeSystem lastCarBrakeSystem, Dictionary<BrakeSystemComponent, PressureUnit> units)
        {
            string s = string.Format(" V {0}", FormatStrings.FormatPressure(Car.Train.EqualReservoirPressurePSIorInHg, PressureUnit.InHg, PressureUnit.InHg, true));
            if (lastCarBrakeSystem != null && lastCarBrakeSystem != this)
                s += " EOT " + lastCarBrakeSystem.GetStatus(units);
            if (HandbrakePercent > 0)
                s += string.Format(" Handbrake {0:F0}%", HandbrakePercent);
            return s;
        }

        public override string[] GetDebugStatus(Dictionary<BrakeSystemComponent, PressureUnit> units)
        {
            return new string[] {
                "1V",
                FormatStrings.FormatPressure(P2V(CylPressurePSIA), PressureUnit.InHg, PressureUnit.InHg, true),
                FormatStrings.FormatPressure(P2V(BrakeLine1PressurePSI), PressureUnit.InHg, PressureUnit.InHg, true),
                FormatStrings.FormatPressure(P2V(VacResPressureAdjPSIA()), PressureUnit.InHg, PressureUnit.InHg, true),
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                HandbrakePercent > 0 ? string.Format("{0:F0}%", HandbrakePercent) : string.Empty,
                FrontBrakeHoseConnected ? "I" : "T",
                string.Format("A{0} B{1}", AngleCockAOpen ? "+" : "-", AngleCockBOpen ? "+" : "-"),
            };
        }

        public override float GetCylPressurePSI()
        {
            return KPa.ToPSI(KPa.FromInHg(P2V(CylPressurePSIA)));
        }

        public override float GetCylVolumeM3()
        {
            return CylVol;
        }

        public override float GetVacResPressurePSI()
        {
            return VacResPressureAdjPSIA();
        }

        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "wagon(maxhandbrakeforce": MaxHandbrakeForceN = stf.ReadFloatBlock(STFReader.UNITS.Force, null); break;
                case "wagon(maxbrakeforce": MaxBrakeForceN = stf.ReadFloatBlock(STFReader.UNITS.Force, null); break;
                case "wagon(brakecylinderpressureformaxbrakebrakeforce": MaxForcePressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultInHg, null); break;
                case "wagon(maxreleaserate": MaxReleaseRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultInHgpS, null); break;
           //     case "wagon(maxapplicationrate": ApplyChargingRatePSIpS = MaxApplicationRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultInHgpS, null); break;
           //     case "engine(pipetimefactor": PipeTimeFactorS = stf.ReadFloatBlock(STFReader.UNITS.Time, null); break;
          //      case "engine(releasetimefactor": ReleaseTimeFactorS = stf.ReadFloatBlock(STFReader.UNITS.Time, null); break;
          
                // OpenRails specific parameters
                case "wagon(brakepipevolume": BrakePipeVolumeM3 = Me3.FromFt3(stf.ReadFloatBlock(STFReader.UNITS.VolumeDefaultFT3, null)); break;
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
            AngleCockAOpen = inf.ReadBoolean();
            AngleCockBOpen = inf.ReadBoolean();
            BleedOffValveOpen = inf.ReadBoolean();
        }

        public override void Initialize(bool handbrakeOn, float maxVacuumInHg, float fullServVacuumInHg, bool immediateRelease)
        {
            CylPressurePSIA = BrakeLine1PressurePSI = V2P(fullServVacuumInHg);
            VacResPressurePSIA = V2P(maxVacuumInHg);
            HandbrakePercent = handbrakeOn & (Car as MSTSWagon).HandBrakePresent ? 100 : 0;
            //CylVolumeM3 = MaxForcePressurePSI * MaxBrakeForceN * 0.00000059733491f; //an average volume (M3) of air used in brake cylinder for 1 N brake force.
        }

        public override void InitializeMoving() // used when initial speed > 0
        {
            BrakeLine1PressurePSI = V2P(Car.Train.EqualReservoirPressurePSIorInHg);
            BrakeLine2PressurePSI = 0;
            BrakeLine3PressurePSI = 0;
/*            if (Car.Train.AITrainBrakePercent == 0)
            {
                CylPressurePSIA = 0;
                Car.BrakeForceN = 0;
            }
            else */
            CylPressurePSIA = V2P(Car.Train.EqualReservoirPressurePSIorInHg);
            VacResPressurePSIA = V2P(Car.Train.EqualReservoirPressurePSIorInHg);
            HandbrakePercent = 0;
        }

        public override void LocoInitializeMoving() // starting conditions when starting speed > 0
        {
            VacResPressurePSIA = V2P(Car.Train.EqualReservoirPressurePSIorInHg);
        }

        public override void Update(float elapsedClockSeconds)
        {
            if (BrakeLine1PressurePSI < VacResPressurePSIA)
            {
                float dp = elapsedClockSeconds * MaxReleaseRatePSIpS * CylVol / VacResVol;
                float vr = NumCylinders * VacResVol / PipeVol;
                if (VacResPressurePSIA - dp < BrakeLine1PressurePSI + dp * vr)
                    dp = (VacResPressurePSIA - BrakeLine1PressurePSI) / (1 + vr);
                VacResPressurePSIA -= dp;
                BrakeLine1PressurePSI += dp * vr;
                CylPressurePSIA = VacResPressurePSIA;
            }
            else if (BrakeLine1PressurePSI < CylPressurePSIA)
            {
                float dp = elapsedClockSeconds * MaxReleaseRatePSIpS;
                float vr = NumCylinders * CylVol / PipeVol;
                if (CylPressurePSIA - dp < BrakeLine1PressurePSI + dp * vr)
                    dp = (CylPressurePSIA - BrakeLine1PressurePSI) / (1 + vr);
                CylPressurePSIA -= dp;
                BrakeLine1PressurePSI += dp * vr;
            }
            else if (BrakeLine1PressurePSI > CylPressurePSIA)
            {
                float dp = elapsedClockSeconds * MaxApplicationRatePSIpS;
                float vr = 2 * CylVol / PipeVol;
                if (CylPressurePSIA + dp > BrakeLine1PressurePSI - dp * vr)
                    dp = (BrakeLine1PressurePSI - CylPressurePSIA) / (1 + vr);
                CylPressurePSIA += dp;
                if (!HasDirectAdmissionValue)
                    BrakeLine1PressurePSI -= dp * vr;
            }
            float vrp = VacResPressureAdjPSIA();
            float f = CylPressurePSIA <= vrp ? 0 : MaxBrakeForceN * Math.Min((CylPressurePSIA - vrp) / MaxForcePressurePSI, 1);
            if (f < MaxHandbrakeForceN * HandbrakePercent / 100)
                f = MaxHandbrakeForceN * HandbrakePercent / 100;
            Car.BrakeRetardForceN = f * Car.BrakeShoeRetardCoefficientFrictionAdjFactor; // calculates value of force applied to wheel, independent of wheel skid
            if (Car.BrakeSkid) // Test to see if wheels are skiding due to excessive brake force
            {
                Car.BrakeForceN = f * Car.SkidFriction;   // if excessive brakeforce, wheel skids, and loses adhesion
            }
            else
            {
                Car.BrakeForceN = f * Car.BrakeShoeCoefficientFrictionAdjFactor; // In advanced adhesion model brake shoe coefficient varies with speed, in simple model constant force applied as per value in WAG file, will vary with wheel skid.
            }

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

                if ( Math.Abs(BrakeLine1PressurePSI-prevBrakePipePressurePSI)> 0.05f /*BrakeLine1PressurePSI > prevBrakePipePressurePSI*/)
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
            // Brake pressures are calculated on the lead locomotive first, and then propogated along each wagon in the consist.

            var train = trainCar.Train;
            var lead = trainCar as MSTSLocomotive;
            var brakePipeTimeFactorS = lead == null ? 0.003f : lead.BrakePipeTimeFactorS;
            // train.BrakeLine1PressurePSI is really vacuum in inHg
            float DesiredPipeVacuum = V2P(train.EqualReservoirPressurePSIorInHg);
            int nSteps = (int)(elapsedClockSeconds * 2 / brakePipeTimeFactorS + 1);
            float TrainPipeTimeVariationS = elapsedClockSeconds / nSteps;
            for (int i = 0; i < nSteps; i++)
            {

                if (lead != null)
                {
                    // Allow for leaking train air brakepipe in lead locomotive
                    float TrainPipeLeakLossPSI = TrainPipeTimeVariationS * lead.TrainBrakePipeLeakPSIorInHgpS;

                    if (lead.BrakeSystem.BrakeLine1PressurePSI - TrainPipeLeakLossPSI > 0 && lead.TrainBrakePipeLeakPSIorInHgpS != 0) // if train brake pipe has pressure in it, ensure result will not be negative if loss is subtracted
                    {
                        lead.BrakeSystem.BrakeLine1PressurePSI -= TrainPipeLeakLossPSI;
                    }

                    // Vacuum Pipe is < Desired value - increase brake pipe value - release brakes??
                    if (lead.BrakeSystem.BrakeLine1PressurePSI < DesiredPipeVacuum)
                    {
                        //  float TrainPipePressureDiffPSI = TrainPipeTimeVariationS * lead.BrakeSystem.ApplyChargingRatePSIpS;
                        float TrainPipePressureDiffPSI = TrainPipeTimeVariationS * lead.BrakePipeChargingRatePSIorInHgpS;
                        if (lead.BrakeSystem.BrakeLine1PressurePSI + TrainPipePressureDiffPSI > DesiredPipeVacuum)
                            TrainPipePressureDiffPSI = DesiredPipeVacuum - lead.BrakeSystem.BrakeLine1PressurePSI;
                        lead.BrakeSystem.BrakeLine1PressurePSI += TrainPipePressureDiffPSI;
                    }

                    // Vacuum Pipe is < Desired value - decrease brake pipe value - apply brakes??
                    else if (lead.BrakeSystem.BrakeLine1PressurePSI > DesiredPipeVacuum)
                    {
                        // lead.BrakeSystem.BrakeLine1PressurePSI *= (1 - TrainPipeTimeVariationS / ReleaseTimeFactorS);
                        lead.BrakeSystem.BrakeLine1PressurePSI *= (1 - TrainPipeTimeVariationS / lead.BrakeServiceTimeFactorS);
                        if (lead.BrakeSystem.BrakeLine1PressurePSI < DesiredPipeVacuum)
                            lead.BrakeSystem.BrakeLine1PressurePSI = DesiredPipeVacuum;
                    }
                }

                // Propogate lead brake line pressure from lead locomotive along the train to each car
                TrainCar car0 = train.Cars[0];
                float p0 = car0.BrakeSystem.BrakeLine1PressurePSI;
                float brakePipeVolumeM30 = car0.BrakeSystem.BrakePipeVolumeM3;
                train.TotalTrainBrakePipeVolumeM3 = 0.0f; // initialise train brake pipe volume

#if DEBUG_TRAIN_PIPE_LEAK

                Trace.TraceInformation("======================================= Train Pipe Leak (VacuumSinglePipe) ===============================================");
                Trace.TraceInformation("Charging Rate {0}  ServiceTimeFactor {1}", lead.BrakePipeChargingRatePSIpS, lead.BrakeServiceTimeFactorS);
                Trace.TraceInformation("Before:  CarID {0}  TrainPipeLeak {1} Lead BrakePipe Pressure {2}", trainCar.CarID, lead.TrainBrakePipeLeakPSIpS, lead.BrakeSystem.BrakeLine1PressurePSI);
                Trace.TraceInformation("Brake State {0}", lead.TrainBrakeController.TrainBrakeControllerState);
                Trace.TraceInformation("Small Ejector {0} Large Ejector {1}", lead.SmallSteamEjectorIsOn, lead.LargeSteamEjectorIsOn);

#endif

                foreach (TrainCar car in train.Cars)
                {
                    train.TotalTrainBrakePipeVolumeM3 += car.BrakeSystem.BrakePipeVolumeM3; // Calculate total brake pipe volume of train
                    
                    float p1 = car.BrakeSystem.BrakeLine1PressurePSI;
                    if (car.BrakeSystem.FrontBrakeHoseConnected && car.BrakeSystem.AngleCockAOpen && car0.BrakeSystem.AngleCockBOpen)
                    {
                        float TrainPipePressureDiffPropogationPSI = TrainPipeTimeVariationS * (p1 - p0) / brakePipeTimeFactorS;
                        car.BrakeSystem.BrakeLine1PressurePSI -= TrainPipePressureDiffPropogationPSI;
                        car0.BrakeSystem.BrakeLine1PressurePSI += TrainPipePressureDiffPropogationPSI;
                    }
                    if (!car.BrakeSystem.FrontBrakeHoseConnected) // Car front brake hose not connected
                    {
                        if (car.BrakeSystem.AngleCockAOpen)  //  AND Front brake cock opened
                            car.BrakeSystem.BrakeLine1PressurePSI -= TrainPipeTimeVariationS * (p1 - OneAtmospherePSI) / brakePipeTimeFactorS;
                        if (car0.BrakeSystem.AngleCockBOpen && car != car0)  //  AND Rear cock of wagon opened, and car is not the first wagon
                            car0.BrakeSystem.BrakeLine1PressurePSI -= TrainPipeTimeVariationS * (p0 - OneAtmospherePSI) / brakePipeTimeFactorS;
                    }
                    if (car == train.Cars[train.Cars.Count - 1] && car.BrakeSystem.AngleCockBOpen)  // Last car in train and rear cock of wagon open
                        car.BrakeSystem.BrakeLine1PressurePSI -= TrainPipeTimeVariationS * (p1 - OneAtmospherePSI) / brakePipeTimeFactorS;
                    p0 = p1;
                    car0 = car;
                }
            }
        }

        public override float InternalPressure(float realPressure)
        {
            return V2P(realPressure);
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
        public override void SetRetainer(RetainerSetting setting)
        {
        }

        public override void AISetPercent(float percent)
        {
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            Car.Train.EqualReservoirPressurePSIorInHg = P2V(OneAtmospherePSI - MaxForcePressurePSI * (1 - percent / 100));
        }
    }
}
