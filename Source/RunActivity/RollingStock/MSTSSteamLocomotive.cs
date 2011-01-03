/* STEAM LOCOMOTIVE CLASSES
 * 
 * The Locomotive is represented by two classes:
 *  MSTSDieselLocomotiveSimulator - defines the behaviour, ie physics, motion, power generated etc
 *  MSTSDieselLocomotiveViewer - defines the appearance in a 3D viewer. The viewer doesn't
 *  get attached to the car until it comes into viewing range.
 *  
 * Both these classes derive from corresponding classes for a basic locomotive
 *  LocomotiveSimulator - provides for movement, basic controls etc
 *  LocomotiveViewer - provides basic animation for running gear, wipers, etc
 * 
 */
/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework.Input;
using MSTS;

namespace ORTS
{
    ///////////////////////////////////////////////////
    ///   SIMULATION BEHAVIOUR
    ///////////////////////////////////////////////////

    /// <summary>
    /// Adds physics and control for a steam locomotive
    /// </summary>
    public class MSTSSteamLocomotive: MSTSLocomotive
    {
        //Configure a default cutoff controller
        //IF none is specified, this will be used, otherwise those values will be overwritten
        MSTSNotchController CutoffController = new MSTSNotchController(-0.9f, 0.9f, 0.1f);
        MSTSNotchController Injector1Controller = new MSTSNotchController(0, 1, 0.1f);
        MSTSNotchController Injector2Controller = new MSTSNotchController(0, 1, 0.1f);
        MSTSNotchController BlowerController = new MSTSNotchController(0, 1, 0.1f);
        MSTSNotchController DamperController = new MSTSNotchController(0, 1, 0.1f);
        MSTSNotchController FiringRateController = new MSTSNotchController(0, 1, 0.1f);
        bool Injector1On = false;
        bool Injector2On = false;
        bool CylinderCocksOpen = false;
        bool ManualFiring = false;

        // state variables
        float SteamUsageLBpS;       // steam used in cylinders
        float BlowerSteamUsageLBpS; // steam used by blower
        float BoilerHeatBTU;        // total heat in water and steam in boiler
        float BoilerMassLB;         // total mass of water and steam in boiler
        float BoilerPressurePSI;    // boiler pressure calculated from heat and mass
        float WaterFraction;        // fraction of boiler volume occupied by water
        float Evaporation;          // steam generation rate
        float FireMassKG;
        float FireRatio;
        bool SafetyOn = false;

        // eng file configuration parameters
        float MaxBoilerPressurePSI = 180f;  // maximum boiler pressure, safety valve setting
        float BoilerVolumeFT3;      // total space in boiler that can hold water and steam
        int NumCylinders = 2;       // number of cylinders
        float CylinderStrokeM;      // stroke of piston
        float CylinderDiameterM;    // diameter of piston
        float MaxBoilerOutputLBpH;  // maximum boiler steam generation rate
        float ExhaustLimitLBpH;     // steam usage rate that causing increased back pressure
        float BasicSteamUsageLBpS;  // steam used for auxiliary stuff
        float IdealFireMassKG;        // target fire mass
        float MaxFiringRateKGpS;
        float SafetyValveUsageLBpS;
        float SafetyValveDropPSI;

        // precomputed values
        float SteamUsageFactor;     // precomputed multiplier for calculating steam used in cylinders
        float BlowerSteamUsageFactor;
        float InjectorRateLBpS;
        Interpolator ForceFactor1;  // negative pressure part of tractive force given cutoff
        Interpolator ForceFactor2;  // positive pressure part of tractive force given cutoff
        Interpolator CylinderPressureDrop;  // pressure drop from throttle to cylinders given usage
        Interpolator BackPressure;  // back pressure in cylinders given usage
        Interpolator CylinderSteamDensity;  // steam density in cylinders given pressure (could be super heated)
        Interpolator SteamDensity;  // saturated steam density given pressure
        Interpolator WaterDensity;  // water density given pressure
        Interpolator SteamHeat;     // total heat in saturated steam given pressure
        Interpolator WaterHeat;     // total heat in water given pressure
        Interpolator Heat2Pressure; // pressure given total heat in water (inverse of WaterHeat)
        Interpolator BurnRate;      // fuel burn rate given steam usage
        Interpolator EvaporationRate;   // steam generation rate given fuel burn rate
        Interpolator EvaporationFactor;

		public MSTSSteamLocomotive(Simulator simulator, string wagFile, TrainCar previousCar)
            : base(simulator, wagFile, previousCar)
        {
            //Console.WriteLine(" {0} {1} {2} {3}", NumCylinders, CylinderDiameterM, CylinderStrokeM, DriverWheelRadiusM);
            //Console.WriteLine(" {0} {1} {2} {3} {4}", MaxBoilerPressurePSI,MaxBoilerOutputLBpH,ExhaustLimitLBpH,BasicSteamUsageLBpS,BoilerVolumeFT3);
            if (NumCylinders < 0 && ZeroError(NumCylinders, "NumCylinders", wagFile))
                NumCylinders = 0;
            if (ZeroError(CylinderDiameterM, "CylinderDiammeter", wagFile))
                CylinderDiameterM= 1;
            if (ZeroError(CylinderStrokeM, "CylinderStroke", wagFile))
                CylinderStrokeM= 1;
            if (ZeroError(DriverWheelRadiusM, "WheelRadius", wagFile))
                DriverWheelRadiusM= 1;
            if (ZeroError(MaxBoilerPressurePSI, "MaxBoilerPressure", wagFile))
                MaxBoilerPressurePSI= 1;
            if (ZeroError(MaxBoilerOutputLBpH, "MaxBoilerOutput", wagFile))
                MaxBoilerOutputLBpH= 1;
            if (ZeroError(ExhaustLimitLBpH, "ExhaustLimit", wagFile))
                ExhaustLimitLBpH = MaxBoilerOutputLBpH;
            if (ZeroError(BoilerVolumeFT3, "BoilerVolume", wagFile))
                BoilerVolumeFT3 = 1;

            SteamUsageFactor = 2 * NumCylinders * 3.281f * CylinderDiameterM / 2 * 3.281f * CylinderDiameterM / 2 *
                3.281f * CylinderStrokeM / (2 * DriverWheelRadiusM);
            SteamDensity = SteamTable.SteamDensityInterpolator();
            WaterDensity = SteamTable.WaterDensityInterpolator();
            SteamHeat = SteamTable.SteamHeatInterpolator();
            WaterHeat = SteamTable.WaterHeatInterpolator();
            CylinderSteamDensity = SteamTable.SteamDensityInterpolator();
            Heat2Pressure = SteamTable.WaterHeat2PressureInterpolator();
            BoilerPressurePSI = MaxBoilerPressurePSI;
            SteamUsageLBpS = 0;
            //BoilerVolumeFT3 *= .25f;
            WaterFraction = .8f;
            BoilerMassLB= WaterFraction*BoilerVolumeFT3*WaterDensity[MaxBoilerPressurePSI] + (1-WaterFraction)*BoilerVolumeFT3*SteamDensity[MaxBoilerPressurePSI];
            BoilerHeatBTU = WaterFraction * BoilerVolumeFT3 * WaterDensity[BoilerPressurePSI]*WaterHeat[BoilerPressurePSI] + (1-WaterFraction) * BoilerVolumeFT3 * SteamDensity[BoilerPressurePSI]*SteamHeat[BoilerPressurePSI];
            //Console.WriteLine("initstate {0} {1}", BoilerMassLB, BoilerHeatBTU);
            // the next two tables are the average over a full wheel rotation calculated using numeric integration
            // they depend on valve geometry and main rod length etc
            if (ForceFactor1 == null)
            {
                ForceFactor1 = new Interpolator(11);
                ForceFactor1[.200f] = -.428043f;
                ForceFactor1[.265f] = -.453624f;
                ForceFactor1[.330f] = -.479480f;
                ForceFactor1[.395f] = -.502123f;
                ForceFactor1[.460f] = -.519346f;
                ForceFactor1[.525f] = -.535572f;
                ForceFactor1[.590f] = -.550099f;
                ForceFactor1[.655f] = -.564719f;
                ForceFactor1[.720f] = -.579431f;
                ForceFactor1[.785f] = -.593737f;
                ForceFactor1[.850f] = -.607703f;
                ForceFactor1.ScaleY(4.4482f * (float)Math.PI / 4 * 39.372f * 39.372f *
                    NumCylinders * CylinderDiameterM * CylinderDiameterM * CylinderStrokeM / (2 * DriverWheelRadiusM));
            }
            if (ForceFactor2 == null)
            {
                ForceFactor2 = new Interpolator(11);
                ForceFactor2[.200f] = .371714f;
                ForceFactor2[.265f] = .429217f;
                ForceFactor2[.330f] = .476195f;
                ForceFactor2[.395f] = .512149f;
                ForceFactor2[.460f] = .536852f;
                ForceFactor2[.525f] = .554344f;
                ForceFactor2[.590f] = .565618f;
                ForceFactor2[.655f] = .573383f;
                ForceFactor2[.720f] = .579257f;
                ForceFactor2[.785f] = .584714f;
                ForceFactor2[.850f] = .591967f;
                ForceFactor2.ScaleY(4.4482f * (float)Math.PI / 4 * 39.372f * 39.372f *
                    NumCylinders * CylinderDiameterM * CylinderDiameterM * CylinderStrokeM / (2 * DriverWheelRadiusM));
            }
            if (CylinderPressureDrop == null)
            {   // this table is not based on measurements
                CylinderPressureDrop = new Interpolator(5);
                CylinderPressureDrop[0] = 0;
                CylinderPressureDrop[.2f] = 0;
                CylinderPressureDrop[.5f] = 2;
                CylinderPressureDrop[1] = 10;
                CylinderPressureDrop[2] = 20;
                CylinderPressureDrop.ScaleX(ExhaustLimitLBpH);
                CylinderPressureDrop.ScaleX(1 / 3600f);
            }
            if (BackPressure == null)
            {   // this table is not based on measurements
                BackPressure = new Interpolator(3);
                BackPressure[0] = 0;
                BackPressure[1] = 6;
                BackPressure[1.2f] = 30;
                BackPressure.ScaleX(ExhaustLimitLBpH);
                BackPressure.ScaleX(1 / 3600f);
            }
            float maxevap = 775;
            float maxburn = 180;
            float grateArea = MaxBoilerOutputLBpH / maxevap;
            if (EvaporationRate == null)
            {   // this table is from page 112 of The Steam Locomotive by R. P. Johnson (the 13000 BTU line) 
                EvaporationRate = new Interpolator(11);
                EvaporationRate[0] = 0;
                EvaporationRate[20] = 170;
                EvaporationRate[40] = 315;
                EvaporationRate[60] = 440;
                EvaporationRate[80] = 550;
                EvaporationRate[100] = 630;
                EvaporationRate[120] = 700;
                EvaporationRate[140] = 740;
                EvaporationRate[160] = 770;
                EvaporationRate[180] = 775;
                EvaporationRate[200] = 760;
                EvaporationRate.ScaleX(grateArea / 3600 / 2.2046f);
                EvaporationRate.ScaleY(grateArea / 3600);
            }
            if (BurnRate == null)
            {
                BurnRate = new Interpolator(2);
                BurnRate[0] = 2;
                BurnRate[maxevap] = maxburn;    // inverse of maximum EvaporationRate
                BurnRate.ScaleX(grateArea / 3600);
                BurnRate.ScaleY(grateArea / 3600 / 2.2046f);
            }
            if (EvaporationFactor == null)
            {
                EvaporationFactor = new Interpolator(4);
                EvaporationFactor[0] = .9f;
                EvaporationFactor[1] = 1;
                EvaporationFactor[2] = .6f;
                EvaporationFactor[3] = .6f;
            }
            BlowerSteamUsageFactor = .1f * EvaporationRate.MaxX() / MaxBoilerPressurePSI;
            InjectorRateLBpS = maxevap * grateArea / 3600;
            FireMassKG = IdealFireMassKG;
            if (MaxFiringRateKGpS == 0)
                MaxFiringRateKGpS = maxburn * grateArea / 3600 / 2.2046f;
            //Trace.WriteLine(string.Format("fire {0} {1} {2}", FireMassKG, MaxFiringRateKGpS, SafetyValveUsageLBpS));
        }
        public bool ZeroError(float v, string name, string wagFile)
        {
            if (v > 0)
                return false;
            Trace.TraceError("Error in " + wagFile + "\r\n   Steam engine value "+name+" must be defined and greater than zero.");
            return true;
        }

        /// <summary>
        /// Parse the wag file parameters required for the simulator and viewer classes
        /// </summary>
        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(numcylinders": NumCylinders = stf.ReadIntBlock(STFReader.UNITS.None, null); break;
                case "engine(cylinderstroke": CylinderStrokeM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "engine(cylinderdiameter": CylinderDiameterM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "engine(boilervolume": BoilerVolumeFT3 = stf.ReadFloatBlock(STFReader.UNITS.Volume, null); break;
                case "engine(maxboilerpressure": MaxBoilerPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.Pressure, null); break;
                case "engine(maxboileroutput": MaxBoilerOutputLBpH = stf.ReadFloatBlock(STFReader.UNITS.MassRate, null); break;
                case "engine(exhaustlimit": ExhaustLimitLBpH = stf.ReadFloatBlock(STFReader.UNITS.MassRate, null); break;
                case "engine(basicsteamusage": BasicSteamUsageLBpS = stf.ReadFloatBlock(STFReader.UNITS.MassRate, null) / 3600; break;
                case "engine(safetyvalvessteamusage": SafetyValveUsageLBpS = stf.ReadFloatBlock(STFReader.UNITS.MassRate, null) / 3600; break;
                case "engine(safetyvalvepressuredifference": SafetyValveDropPSI = stf.ReadFloatBlock(STFReader.UNITS.Pressure, null); break;
                case "engine(idealfiremass": IdealFireMassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "engine(steamfiremanmaxpossiblefiringrate": MaxFiringRateKGpS = stf.ReadFloatBlock(STFReader.UNITS.MassRate, null) / 2.2046f / 3600; break;
                case "engine(enginecontrollers(cutoff": CutoffController.Parse(stf); break;
                case "engine(enginecontrollers(injector1water": Injector1Controller.Parse(stf); break;
                case "engine(enginecontrollers(injector2water": Injector2Controller.Parse(stf); break;
                case "engine(enginecontrollers(blower": BlowerController.Parse(stf); break;
                case "engine(enginecontrollers(dampersfront": DamperController.Parse(stf); break;
                case "engine(enginecontrollers(shovel": FiringRateController.Parse(stf); break;
                case "engine(forcefactor1": ForceFactor1 = new Interpolator(stf); break;
                case "engine(forcefactor2": ForceFactor2 = new Interpolator(stf); break;
                case "engine(cylinderpressuredrop": CylinderPressureDrop = new Interpolator(stf); break;
                case "engine(backpressure": BackPressure = new Interpolator(stf); break;
                case "engine(burnrate": BurnRate = new Interpolator(stf); break;
                case "engine(evaporationrate": EvaporationRate = new Interpolator(stf); break;
                default: base.Parse(lowercasetoken, stf); break;
            }
        }

        /// <summary>
        /// This initializer is called when we are making a new copy of a car already
        /// loaded in memory.  We use this one to speed up loading by eliminating the
        /// need to parse the wag file multiple times.
        /// NOTE:  you must initialize all the same variables as you parsed above
        /// </summary>
        public override void InitializeFromCopy(MSTSWagon copy)
        {
            MSTSSteamLocomotive locoCopy = (MSTSSteamLocomotive)copy;
            SteamUsageLBpS = locoCopy.SteamUsageLBpS;
            BoilerHeatBTU = locoCopy.BoilerHeatBTU;
            BoilerMassLB = locoCopy.BoilerMassLB;
            BoilerPressurePSI = locoCopy.BoilerPressurePSI;
            WaterFraction = locoCopy.WaterFraction;
            Evaporation = locoCopy.Evaporation;
            MaxBoilerPressurePSI = locoCopy.MaxBoilerPressurePSI;
            BoilerVolumeFT3 = locoCopy.BoilerVolumeFT3;
            NumCylinders = locoCopy.NumCylinders;
            CylinderStrokeM = locoCopy.CylinderStrokeM;
            CylinderDiameterM = locoCopy.CylinderDiameterM;
            MaxBoilerOutputLBpH = locoCopy.MaxBoilerOutputLBpH;
            ExhaustLimitLBpH = locoCopy.ExhaustLimitLBpH;
            BasicSteamUsageLBpS = locoCopy.BasicSteamUsageLBpS;
            SteamUsageFactor = locoCopy.SteamUsageFactor;
            SafetyValveUsageLBpS = locoCopy.SafetyValveUsageLBpS;
            SafetyValveDropPSI = locoCopy.SafetyValveDropPSI;
            IdealFireMassKG = locoCopy.IdealFireMassKG;
            MaxFiringRateKGpS = locoCopy.MaxFiringRateKGpS;
            ForceFactor1 = new Interpolator(locoCopy.ForceFactor1);
            ForceFactor2 = new Interpolator(locoCopy.ForceFactor2);
            CylinderPressureDrop = new Interpolator(locoCopy.CylinderPressureDrop);
            BackPressure = new Interpolator(locoCopy.BackPressure);
            CylinderSteamDensity = new Interpolator(locoCopy.CylinderSteamDensity);
            SteamDensity = new Interpolator(locoCopy.SteamDensity);
            WaterDensity = new Interpolator(locoCopy.WaterDensity);
            SteamHeat = new Interpolator(locoCopy.SteamHeat);
            WaterHeat = new Interpolator(locoCopy.WaterHeat);
            Heat2Pressure = new Interpolator(locoCopy.Heat2Pressure);
            BurnRate = new Interpolator(locoCopy.BurnRate);
            EvaporationRate = new Interpolator(locoCopy.EvaporationRate);
            EvaporationFactor = new Interpolator(locoCopy.EvaporationFactor);
            CutoffController = (MSTSNotchController)locoCopy.CutoffController.Clone();
            Injector1Controller = (MSTSNotchController)locoCopy.Injector1Controller.Clone();
            Injector2Controller = (MSTSNotchController)locoCopy.Injector2Controller.Clone();
            BlowerController = (MSTSNotchController)locoCopy.BlowerController.Clone();
            DamperController = (MSTSNotchController)locoCopy.DamperController.Clone();
            FiringRateController = (MSTSNotchController)locoCopy.FiringRateController.Clone();

            base.InitializeFromCopy(copy);  // each derived level initializes its own variables
        }

        /// <summary>
        /// We are saving the game.  Save anything that we'll need to restore the 
        /// status later.
        /// </summary>
        public override void Save(BinaryWriter outf)
        {
            outf.Write(SteamUsageLBpS);
            outf.Write(BoilerHeatBTU);
            outf.Write(BoilerMassLB);
            outf.Write(BoilerPressurePSI);
            outf.Write(WaterFraction);
            outf.Write(Evaporation);
            outf.Write(MaxBoilerPressurePSI);
            outf.Write(BoilerVolumeFT3);
            outf.Write(NumCylinders);
            outf.Write(CylinderStrokeM);
            outf.Write(CylinderDiameterM);
            outf.Write(MaxBoilerOutputLBpH);
            outf.Write(ExhaustLimitLBpH);
            outf.Write(BasicSteamUsageLBpS);
            outf.Write(SteamUsageFactor);
            ForceFactor1.Save(outf);
            ForceFactor2.Save(outf);
            CylinderPressureDrop.Save(outf);
            BackPressure.Save(outf);
            CylinderSteamDensity.Save(outf);
            SteamDensity.Save(outf);
            WaterDensity.Save(outf);
            SteamHeat.Save(outf);
            WaterHeat.Save(outf);
            Heat2Pressure.Save(outf);
            BurnRate.Save(outf);
            EvaporationRate.Save(outf);
            ControllerFactory.Save(CutoffController, outf);
            ControllerFactory.Save(Injector1Controller, outf);
            ControllerFactory.Save(Injector2Controller, outf);
            ControllerFactory.Save(BlowerController, outf);
            ControllerFactory.Save(DamperController, outf);
            ControllerFactory.Save(FiringRateController, outf);
            base.Save(outf);
        }

        /// <summary>
        /// We are restoring a saved game.  The TrainCar class has already
        /// been initialized.   Restore the game state.
        /// </summary>
		public override void Restore(BinaryReader inf)
        {
            SteamUsageLBpS = inf.ReadSingle();
            BoilerHeatBTU = inf.ReadSingle();
            BoilerMassLB = inf.ReadSingle();
            BoilerPressurePSI = inf.ReadSingle();
            WaterFraction = inf.ReadSingle();
            Evaporation = inf.ReadSingle();
            MaxBoilerPressurePSI = inf.ReadSingle();
            BoilerVolumeFT3 = inf.ReadSingle();
            NumCylinders = inf.ReadInt32();
            CylinderStrokeM = inf.ReadSingle();
            CylinderDiameterM = inf.ReadSingle();
            MaxBoilerOutputLBpH = inf.ReadSingle();
            ExhaustLimitLBpH = inf.ReadSingle();
            BasicSteamUsageLBpS = inf.ReadSingle();
            SteamUsageFactor = inf.ReadSingle();
            ForceFactor1 = new Interpolator(inf);
            ForceFactor2 = new Interpolator(inf);
            CylinderPressureDrop = new Interpolator(inf);
            BackPressure = new Interpolator(inf);
            CylinderSteamDensity = new Interpolator(inf);
            SteamDensity = new Interpolator(inf);
            WaterDensity = new Interpolator(inf);
            SteamHeat = new Interpolator(inf);
            WaterHeat = new Interpolator(inf);
            Heat2Pressure = new Interpolator(inf);
            BurnRate = new Interpolator(inf);
            EvaporationRate = new Interpolator(inf);
            CutoffController = (MSTSNotchController)ControllerFactory.Restore(Simulator, inf);
            Injector1Controller = (MSTSNotchController)ControllerFactory.Restore(Simulator, inf);
            Injector2Controller = (MSTSNotchController)ControllerFactory.Restore(Simulator, inf);
            BlowerController = (MSTSNotchController)ControllerFactory.Restore(Simulator, inf);
            DamperController = (MSTSNotchController)ControllerFactory.Restore(Simulator, inf);
            FiringRateController = (MSTSNotchController)ControllerFactory.Restore(Simulator, inf);
            base.Restore(inf);
        }

        /// <summary>
        /// Create a viewer for this locomotive.   Viewers are only attached
        /// while the locomotive is in viewing range.
        /// </summary>
        public override TrainCarViewer GetViewer(Viewer3D viewer)
        {
            return new MSTSSteamLocomotiveViewer( viewer, this );
        }

        /// <summary>
        /// This is a periodic update to calculate physics 
        /// parameters and update the base class's MotiveForceN 
        /// and FrictionForceN values based on throttle settings
        /// etc for the locomotive.
        /// </summary>
        public override void Update(float elapsedClockSeconds)
        {
            if (this.IsLeadLocomotive())
            {
                Train.MUReverserPercent = CutoffController.Update(elapsedClockSeconds) * 100.0f;
                if (Train.MUReverserPercent >= 0)
                    Train.MUDirection = Direction.Forward;
                else
                    Train.MUDirection = Direction.Reverse;
            }
            else
                CutoffController.Update(elapsedClockSeconds);
            Injector1Controller.Update(elapsedClockSeconds);
            Injector2Controller.Update(elapsedClockSeconds);
            BlowerController.Update(elapsedClockSeconds);
            DamperController.Update(elapsedClockSeconds);
            FiringRateController.Update(elapsedClockSeconds);

            base.Update(elapsedClockSeconds);

            Variable1 = Math.Abs(SpeedMpS);   // Steam loco's seem to need this.
            Variable2 = 50;   // not sure what this ones for ie in an SMS file

            float throttle = ThrottlePercent / 100;
            float cutoff = Math.Abs(Train.MUReverserPercent / 100);
            if (cutoff > ForceFactor2.MaxX())
                cutoff= ForceFactor2.MaxX();
            float speed = Math.Abs(Train.SpeedMpS);
            if (speed > 2 && (Train.MUReverserPercent==100 || Train.MUReverserPercent==-100))
            {   // AI cutoff adjustment logic, also used for steam MU'd with non-steam
                cutoff= throttle*ForceFactor2.MaxX()*2/speed;
                float min = ForceFactor2.MinX();
                if (cutoff < min)
                {
                    throttle = cutoff/min;
                    cutoff= min;
                }
                else
                    throttle= 1;
            }
            float cylinderPressure = throttle * BoilerPressurePSI - CylinderPressureDrop[SteamUsageLBpS];
            float backPressure = BackPressure[SteamUsageLBpS];
            MotiveForceN = (Direction == Direction.Forward ? 1 : -1) *
                (backPressure * ForceFactor1[cutoff] + cylinderPressure * ForceFactor2[cutoff]);
            if (float.IsNaN(MotiveForceN))
                MotiveForceN = 0;
            LimitMotiveForce();
            if (speed == 0 && cutoff < .5f)
                MotiveForceN = 0;   // valves assumed to be closed
            // usage calculated as moving average to minimize chance of oscillation
            SteamUsageLBpS = .6f * SteamUsageLBpS + .4f * speed * SteamUsageFactor * (cutoff + .07f) * (CylinderSteamDensity[cylinderPressure] - CylinderSteamDensity[backPressure]);
            float steamHeat = SteamHeat[BoilerPressurePSI];
            float steamDensity = SteamDensity[BoilerPressurePSI];
            float waterDensity = WaterDensity[BoilerPressurePSI];
            if (ManualFiring)
            {
                BlowerSteamUsageLBpS = BlowerSteamUsageFactor * BlowerController.CurrentValue * BoilerPressurePSI;
                BoilerMassLB -= elapsedClockSeconds * (SteamUsageLBpS + BlowerSteamUsageLBpS + BasicSteamUsageLBpS);
                if (Injector1On)
                    BoilerMassLB += elapsedClockSeconds * Injector1Controller.CurrentValue * InjectorRateLBpS;
                if (Injector2On)
                    BoilerMassLB += elapsedClockSeconds * Injector2Controller.CurrentValue * InjectorRateLBpS;
                if (BoilerPressurePSI > MaxBoilerPressurePSI)
                    SafetyOn = true;
                else if (BoilerPressurePSI < MaxBoilerPressurePSI - SafetyValveDropPSI)
                    SafetyOn = false;
                if (SafetyOn)
                {
                    BoilerMassLB -= elapsedClockSeconds * SafetyValveUsageLBpS;
                    BoilerHeatBTU -= elapsedClockSeconds * SafetyValveUsageLBpS * steamHeat / steamDensity;
                }
            }
            else
                BlowerSteamUsageLBpS = SteamUsageLBpS < BasicSteamUsageLBpS ? BasicSteamUsageLBpS - SteamUsageLBpS : 0; // automatic blower
            // mass assumed constant for automatic firing and injectors

            float burnRate = BurnRate[SteamUsageLBpS + BlowerSteamUsageLBpS];
            if (!ManualFiring)
                Evaporation = EvaporationRate[burnRate];  // automatic fire at maximum rate
            else if (IdealFireMassKG > 0)
            {   // coal fire
                if (FireMassKG < IdealFireMassKG)
                    burnRate *= FireMassKG / IdealFireMassKG;
                FireRatio = FireMassKG / IdealFireMassKG;
                burnRate *= 1 - .5f * DamperController.CurrentValue;
                Evaporation = EvaporationRate[burnRate];
                Evaporation *= EvaporationFactor[FireRatio];
                FireMassKG += elapsedClockSeconds * (MaxFiringRateKGpS * FiringRateController.CurrentValue - burnRate);
                if (FireMassKG < 0)
                    FireMassKG = 0;
                else if (FireMassKG > 2 * IdealFireMassKG)
                    FireMassKG = 2 * IdealFireMassKG;
            }
            else
            {   // oil fire
                FireRatio = burnRate / MaxFiringRateKGpS;
                if (burnRate > MaxFiringRateKGpS)
                    burnRate = MaxFiringRateKGpS;
                Evaporation = EvaporationRate[burnRate];
                Evaporation *= EvaporationFactor[FireRatio];
            }
            BoilerHeatBTU += elapsedClockSeconds * (Evaporation - SteamUsageLBpS - BasicSteamUsageLBpS) * steamHeat;
            WaterFraction = (BoilerMassLB / BoilerVolumeFT3 - steamDensity) / (waterDensity - steamDensity);
            float waterHeat = (BoilerHeatBTU / BoilerVolumeFT3 - (1 - WaterFraction) * steamDensity * steamHeat) / (WaterFraction * waterDensity);
            BoilerPressurePSI = Heat2Pressure[waterHeat];
            if (!ManualFiring && BoilerPressurePSI > MaxBoilerPressurePSI)
            {
                BoilerHeatBTU = ((WaterHeat[MaxBoilerPressurePSI] * WaterFraction * waterDensity) + (1 - WaterFraction) * steamDensity * steamHeat) * BoilerVolumeFT3;
                BoilerPressurePSI = MaxBoilerPressurePSI;
            }
        }

        public override string GetStatus()
        {
			float evap = Evaporation * 3600;
			float usage = (SteamUsageLBpS + BlowerSteamUsageLBpS + BasicSteamUsageLBpS) * 3600;
            if (SafetyOn)
                usage += SafetyValveUsageLBpS * 3600;
			StringBuilder result = new StringBuilder();
            result.AppendFormat("Boiler Pressure = {0:F1} PSI\nSteam Generation = {1:F0} lb/h\nSteam Usage = {2:F0} lb/h", BoilerPressurePSI, evap, usage);
            //BoilerHeatBTU,BoilerMassLB,WaterFraction.ToString("F2"));
            if (ManualFiring)
            {
                result.AppendFormat("\nWater Level = {0:F0} %", WaterFraction * 100);
                if (IdealFireMassKG > 0)
                    result.AppendFormat("\nFire Mass = {0:F0} %", FireMassKG / IdealFireMassKG * 100);
                else
                    result.AppendFormat("\nFire Ratio = {0:F0} %", FireRatio * 100);
                result.Append("\nInjectors =");
                if (Injector1On)
                    result.AppendFormat(" {0:F0} %", Injector1Controller.CurrentValue*100);
                else
                    result.Append(" Off");
                if (Injector2On)
                    result.AppendFormat(" {0:F0} %", Injector2Controller.CurrentValue * 100);
                else
                    result.Append(" Off");
                result.AppendFormat("\nBlower = {0:F0} %", BlowerController.CurrentValue * 100);
                result.AppendFormat("\nDamper = {0:F0} %", DamperController.CurrentValue * 100);
                result.AppendFormat("\nFiring Rate = {0:F0} %", FiringRateController.CurrentValue * 100);
            }
            return result.ToString();
        }

        public void StartReverseIncrease()
        {
            CutoffController.StartIncrease();
        }

        public void StopReverseIncrease()
        {
            CutoffController.StopIncrease();
        }

        public void StartReverseDecrease()
        {
            CutoffController.StartDecrease();
        }

        public void StopReverseDecrease()
        {
            CutoffController.StopDecrease();
        }

        public void SetCutoffPercent(float percent)
        {
            Train.MUReverserPercent = CutoffController.SetRDPercent(percent);
            if (Train.MUReverserPercent >= 0)
                Train.MUDirection = Direction.Forward;
            else
                Train.MUDirection = Direction.Reverse;
        }

        public void StartInjector1Increase()
        {
            Injector1Controller.StartIncrease();
        }
        public void StopInjector1Increase()
        {
            Injector1Controller.StopIncrease();
        }
        public void StartInjector1Decrease()
        {
            Injector1Controller.StartDecrease();
        }
        public void StopInjector1Decrease()
        {
            Injector1Controller.StopDecrease();
        }
        public void ToggleInjector1()
        {
            Injector1On = !Injector1On;
        }

        public void StartInjector2Increase()
        {
            Injector2Controller.StartIncrease();
        }
        public void StopInjector2Increase()
        {
            Injector2Controller.StopIncrease();
        }
        public void StartInjector2Decrease()
        {
            Injector2Controller.StartDecrease();
        }
        public void StopInjector2Decrease()
        {
            Injector2Controller.StopDecrease();
        }
        public void ToggleInjector2()
        {
            Injector2On = !Injector2On;
        }

        public void StartBlowerIncrease()
        {
            BlowerController.StartIncrease();
        }
        public void StopBlowerIncrease()
        {
            BlowerController.StopIncrease();
        }
        public void StartBlowerDecrease()
        {
            BlowerController.StartDecrease();
        }
        public void StopBlowerDecrease()
        {
            BlowerController.StopDecrease();
        }

        public void StartDamperIncrease()
        {
            DamperController.StartIncrease();
        }
        public void StopDamperIncrease()
        {
            DamperController.StopIncrease();
        }
        public void StartDamperDecrease()
        {
            DamperController.StartDecrease();
        }
        public void StopDamperDecrease()
        {
            DamperController.StopDecrease();
        }

        public void StartFiringRateIncrease()
        {
            FiringRateController.StartIncrease();
        }
        public void StopFiringRateIncrease()
        {
            FiringRateController.StopIncrease();
        }
        public void StartFiringRateDecrease()
        {
            FiringRateController.StartDecrease();
        }
        public void StopFiringRateDecrease()
        {
            FiringRateController.StopDecrease();
        }

        public void ToggleCylinderCocks()
        {
            CylinderCocksOpen = !CylinderCocksOpen;
        }

        public void ToggleManualFiring()
        {
            ManualFiring = !ManualFiring;
        }

        /// <summary>
        /// Used when someone want to notify us of an event
        /// </summary>
        public override void SignalEvent(EventID eventID)
        {
            switch (eventID)
            {
                // for example
                // case EventID.BellOn: Bell = true; break;
                // case EventID.BellOff: Bell = false; break;
                default: break;
            }
            base.SignalEvent(eventID);
        }
    } // class SteamLocomotive

    ///////////////////////////////////////////////////
    ///   3D VIEW
    ///////////////////////////////////////////////////

    /// <summary>
    /// Adds any special steam loco animation to the basic LocomotiveViewer class
    /// </summary>
    class MSTSSteamLocomotiveViewer : MSTSLocomotiveViewer
    {
        MSTSSteamLocomotive SteamLocomotive { get{ return (MSTSSteamLocomotive)Car;}}

        public MSTSSteamLocomotiveViewer(Viewer3D viewer, MSTSSteamLocomotive car)
            : base(viewer, car)
        {
        }

        /// <summary>
        /// A keyboard or mouse click has occured. Read the UserInput
        /// structure to determine what was pressed.
        /// </summary>
        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
			if (UserInput.IsPressed(UserCommands.ControlReverserForward))
                SteamLocomotive.StartReverseIncrease();
			else if (UserInput.IsReleased(UserCommands.ControlReverserForward))
                SteamLocomotive.StopReverseIncrease();
			else if (UserInput.IsPressed(UserCommands.ControlReverserBackwards))
                SteamLocomotive.StartReverseDecrease();
			else if (UserInput.IsReleased(UserCommands.ControlReverserBackwards))
                SteamLocomotive.StopReverseDecrease();
            if (UserInput.IsPressed(UserCommands.ControlInjector1Increase))
                SteamLocomotive.StartInjector1Increase();
            else if (UserInput.IsReleased(UserCommands.ControlInjector1Increase))
                SteamLocomotive.StopInjector1Increase();
            else if (UserInput.IsPressed(UserCommands.ControlInjector1Decrease))
                SteamLocomotive.StartInjector1Decrease();
            else if (UserInput.IsReleased(UserCommands.ControlInjector1Decrease))
                SteamLocomotive.StopInjector1Decrease();
            if (UserInput.IsPressed(UserCommands.ControlInjector1))
                SteamLocomotive.ToggleInjector1();
            if (UserInput.IsPressed(UserCommands.ControlInjector2Increase))
                SteamLocomotive.StartInjector2Increase();
            else if (UserInput.IsReleased(UserCommands.ControlInjector2Increase))
                SteamLocomotive.StopInjector2Increase();
            else if (UserInput.IsPressed(UserCommands.ControlInjector2Decrease))
                SteamLocomotive.StartInjector2Decrease();
            else if (UserInput.IsReleased(UserCommands.ControlInjector2Decrease))
                SteamLocomotive.StopInjector2Decrease();
            if (UserInput.IsPressed(UserCommands.ControlInjector2))
                SteamLocomotive.ToggleInjector2();
            if (UserInput.IsPressed(UserCommands.ControlBlowerIncrease))
                SteamLocomotive.StartBlowerIncrease();
            else if (UserInput.IsReleased(UserCommands.ControlBlowerIncrease))
                SteamLocomotive.StopBlowerIncrease();
            else if (UserInput.IsPressed(UserCommands.ControlBlowerDecrease))
                SteamLocomotive.StartBlowerDecrease();
            else if (UserInput.IsReleased(UserCommands.ControlBlowerDecrease))
                SteamLocomotive.StopBlowerDecrease();
            if (UserInput.IsPressed(UserCommands.ControlDamperIncrease))
                SteamLocomotive.StartDamperIncrease();
            else if (UserInput.IsReleased(UserCommands.ControlDamperIncrease))
                SteamLocomotive.StopDamperIncrease();
            else if (UserInput.IsPressed(UserCommands.ControlDamperDecrease))
                SteamLocomotive.StartDamperDecrease();
            else if (UserInput.IsReleased(UserCommands.ControlDamperDecrease))
                SteamLocomotive.StopDamperDecrease();
            if (UserInput.IsPressed(UserCommands.ControlFiringRateIncrease))
                SteamLocomotive.StartFiringRateIncrease();
            else if (UserInput.IsReleased(UserCommands.ControlFiringRateIncrease))
                SteamLocomotive.StopFiringRateIncrease();
            else if (UserInput.IsPressed(UserCommands.ControlFiringRateDecrease))
                SteamLocomotive.StartFiringRateDecrease();
            else if (UserInput.IsReleased(UserCommands.ControlFiringRateDecrease))
                SteamLocomotive.StopFiringRateDecrease();
            if (UserInput.IsPressed(UserCommands.ControlCylinderCocks))
                SteamLocomotive.ToggleCylinderCocks();
            if (UserInput.IsPressed(UserCommands.ControlFiring))
                SteamLocomotive.ToggleManualFiring();

            if (UserInput.RDState != null && UserInput.RDState.Changed)
                SteamLocomotive.SetCutoffPercent(UserInput.RDState.DirectionPercent);

            base.HandleUserInput(elapsedTime);
        }

        /// <summary>
        /// We are about to display a video frame.  Calculate positions for 
        /// animated objects, and add their primitives to the RenderFrame list.
        /// </summary>
        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            base.PrepareFrame(frame, elapsedTime);
        }

        /// <summary>
        /// This doesn't function yet.
        /// </summary>
        public override void Unload()
        {
            base.Unload();
        }


    } // class SteamLocomotiveViewer

}
