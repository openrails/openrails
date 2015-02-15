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

using System;
using System.IO;
using Orts.Parsers.Msts;
using ORTS.Common;

namespace ORTS
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
        float PipeTimeFactorS = .003f; // copied from air single pipe, probably not accurate
        float ReleaseTimeFactorS = 1.009f; // copied from air single pipe, but close to modern ejector data
        float ApplyChargingRatePSIpS = 4;

        public VacuumSinglePipe(TrainCar car)
        {
            Car = car;
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
            ApplyChargingRatePSIpS = thiscopy.ApplyChargingRatePSIpS;
            PipeTimeFactorS = thiscopy.PipeTimeFactorS;
            ReleaseTimeFactorS = thiscopy.ReleaseTimeFactorS;
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

        public override string GetStatus(PressureUnit unit)
        {
            return string.Format(" BP {0}", FormatStrings.FormatPressure(P2V(BrakeLine1PressurePSI), PressureUnit.InHg, PressureUnit.InHg, false));
        }

        public override string GetFullStatus(BrakeSystem lastCarBrakeSystem, PressureUnit unit)
        {
            string s = string.Format(" V {0}", FormatStrings.FormatPressure(Car.Train.BrakeLine1PressurePSIorInHg, PressureUnit.InHg, PressureUnit.InHg, true));
            if (lastCarBrakeSystem != null && lastCarBrakeSystem != this)
                s += " EOT " + lastCarBrakeSystem.GetStatus(unit);
            if (HandbrakePercent > 0)
                s += string.Format(" Handbrake {0:F0}%", HandbrakePercent);
            return s;
        }

        public override string[] GetDebugStatus(PressureUnit unit)
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
            return 0;
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
                case "wagon(maxapplicationrate": ApplyChargingRatePSIpS = MaxApplicationRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultInHgpS, null); break;
                case "engine(pipetimefactor": PipeTimeFactorS = stf.ReadFloatBlock(STFReader.UNITS.Time, null); break;
                case "engine(releasetimefactor": ReleaseTimeFactorS = stf.ReadFloatBlock(STFReader.UNITS.Time, null); break;
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
        }

        public override void InitializeMoving() // used when initial speed > 0
        {
            BrakeLine1PressurePSI = V2P(Car.Train.BrakeLine1PressurePSIorInHg);
            BrakeLine2PressurePSI = 0;
            BrakeLine3PressurePSI = 0;
/*            if (Car.Train.AITrainBrakePercent == 0)
            {
                CylPressurePSIA = 0;
                Car.BrakeForceN = 0;
            }
            else */
            CylPressurePSIA = V2P(Car.Train.BrakeLine1PressurePSIorInHg);
            VacResPressurePSIA = V2P(Car.Train.BrakeLine1PressurePSIorInHg);
            HandbrakePercent = 0;
        }

        public override void LocoInitializeMoving() // starting conditions when starting speed > 0
        {
            VacResPressurePSIA = V2P(Car.Train.BrakeLine1PressurePSIorInHg);
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
            Car.BrakeForceN = f;
        }

        public override void PropagateBrakePressure(float elapsedClockSeconds)
        {
            Train train = Car.Train;
            // train.BrakeLine1PressurePSI is really vacuum in inHg
            float psia = V2P(train.BrakeLine1PressurePSIorInHg);
            int nSteps = (int)(elapsedClockSeconds * 2 / PipeTimeFactorS + 1);
            float dt = elapsedClockSeconds / nSteps;
            for (int i = 0; i < nSteps; i++)
            {
                if (BrakeLine1PressurePSI < psia)
                {
                    float dp = dt * ApplyChargingRatePSIpS;
                    if (BrakeLine1PressurePSI + dp > psia)
                        dp = psia - BrakeLine1PressurePSI;
                    BrakeLine1PressurePSI += dp;
                }
                else if (BrakeLine1PressurePSI > psia)
                {
                    BrakeLine1PressurePSI *= (1 - dt / ReleaseTimeFactorS);
                    if (BrakeLine1PressurePSI < psia)
                        BrakeLine1PressurePSI = psia;
                }
                TrainCar car0 = Car.Train.Cars[0];
                float p0 = car0.BrakeSystem.BrakeLine1PressurePSI;
                foreach (TrainCar car in train.Cars)
                {
                    float p1 = car.BrakeSystem.BrakeLine1PressurePSI;
                    if (car.BrakeSystem.FrontBrakeHoseConnected && car.BrakeSystem.AngleCockAOpen && car0.BrakeSystem.AngleCockBOpen)
                    {
                        float dp = dt * (p1 - p0) / PipeTimeFactorS;
                        car.BrakeSystem.BrakeLine1PressurePSI -= dp;
                        car0.BrakeSystem.BrakeLine1PressurePSI += dp;
                    }
                    if (!car.BrakeSystem.FrontBrakeHoseConnected)
                    {
                        if (car.BrakeSystem.AngleCockAOpen)
                            car.BrakeSystem.BrakeLine1PressurePSI -= dt * (p1 - OneAtmospherePSI) / PipeTimeFactorS;
                        if (car0.BrakeSystem.AngleCockBOpen && car != car0)
                            car0.BrakeSystem.BrakeLine1PressurePSI -= dt * (p0 - OneAtmospherePSI) / PipeTimeFactorS;
                    }
                    if (car == train.Cars[train.Cars.Count - 1] && car.BrakeSystem.AngleCockBOpen)
                        car.BrakeSystem.BrakeLine1PressurePSI -= dt * (p1 - OneAtmospherePSI) / PipeTimeFactorS;
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
            Car.Train.BrakeLine1PressurePSIorInHg = P2V(OneAtmospherePSI - MaxForcePressurePSI * (1 - percent / 100));
        }
    }
}
