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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSTS;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System.IO;

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
        // state variables
        float SteamUsageLBpS;       // steam used in cylinders
        float BoilerHeatBTU;        // total heat in water and steam in boiler
        float BoilerMassLB;         // total mass of water and steam in boiler
        float BoilerPressurePSI;    // boiler pressure calculated from heat and mass
        float WaterFraction;        // fraction of boiler volume occupied by water
        float Evaporation;          // steam generation rate

        // configuration parameters and precomputed values
        float MaxBoilerPressurePSI = 180f;  // maximum boiler pressure, safety valve setting
        float BoilerVolumeFT3;      // total space in boiler that can hold water and steam
        int NumCylinders = 2;       // number of cylinders
        float CylinderStrokeM;      // stroke of piston
        float CylinderDiameterM;    // diameter of piston
        float MaxBoilerOutputLBpH;  // maximum boiler steam generation rate
        float ExhaustLimitLBpH;     // steam usage rate that causing increased back pressure
        float BasicSteamUsageLBpS;  // steam used for auxiliary stuff
        float SteamUsageFactor;     // precomputed multiplier for calculating steam used in cylinders
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

        public MSTSSteamLocomotive(string wagFile)
            : base(wagFile)
        {
            //Console.WriteLine(" {0} {1} {2} {3}", NumCylinders, CylinderDiameterM, CylinderStrokeM, DriverWheelRadiusM);
            //Console.WriteLine(" {0} {1} {2} {3} {4}", MaxBoilerPressurePSI,MaxBoilerOutputLBpH,ExhaustLimitLBpH,BasicSteamUsageLBpS,BoilerVolumeFT3);
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
                CylinderPressureDrop.ScaleX(ExhaustLimitLBpH / 3600);
            }
            if (BackPressure == null)
            {   // this table is not based on measurements
                BackPressure = new Interpolator(3);
                BackPressure[0] = 0;
                BackPressure[1] = 6;
                BackPressure[1.2f] = 30;
                BackPressure.ScaleX(ExhaustLimitLBpH / 3600);
            }
            if (BurnRate == null)
            {
                BurnRate = new Interpolator(2);
                BurnRate[0] = 2;
                BurnRate[775] = 180;    // inverse of maximum EvaporationRate
                BurnRate.ScaleX(MaxBoilerOutputLBpH / 775 / 3600);
                BurnRate.ScaleY(MaxBoilerOutputLBpH / 775 / 3600);
            }
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
                EvaporationRate.ScaleX(MaxBoilerOutputLBpH / 775 / 3600);
                EvaporationRate.ScaleY(MaxBoilerOutputLBpH / 775 / 3600);
            }
        }

        /// <summary>
        /// Parse the wag file parameters required for the simulator and viewer classes
        /// </summary>
        public override void Parse(string lowercasetoken, STFReader f)
        {
            switch (lowercasetoken)
            {
                case "engine(numcylinders": NumCylinders = f.ReadIntBlock(); break;
                case "engine(cylinderstroke": CylinderStrokeM = f.ReadFloatBlock(); break;
                case "engine(cylinderdiameter": CylinderDiameterM = f.ReadFloatBlock(); break;
                case "engine(boilervolume": BoilerVolumeFT3 = ParseFT3(f.ReadStringBlock(),f); break;
                case "engine(maxboilerpressure": MaxBoilerPressurePSI = ParsePSI(f.ReadStringBlock(),f); break;
                case "engine(maxboileroutput": MaxBoilerOutputLBpH = ParseLBpH(f.ReadStringBlock(),f); break;
                case "engine(exhaustlimit": ExhaustLimitLBpH = ParseLBpH(f.ReadStringBlock(),f); break;
                case "engine(basicsteamusage": BasicSteamUsageLBpS = ParseLBpH(f.ReadStringBlock(),f)/3600; break;
                default: base.Parse(lowercasetoken, f); break;
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
            if (speed == 0 && cutoff < .5f)
                MotiveForceN = 0;   // valves assumed to be closed
            // usage calculated as moving average to minimize chance of oscillation
            SteamUsageLBpS = .6f * SteamUsageLBpS + .4f * speed * SteamUsageFactor * (cutoff + .07f) * (CylinderSteamDensity[cylinderPressure] - CylinderSteamDensity[backPressure]);

            float burnRate = BurnRate[SteamUsageLBpS];
            Evaporation = EvaporationRate[burnRate];
            float steamHeat = SteamHeat[BoilerPressurePSI];
            float steamDensity = SteamDensity[BoilerPressurePSI];
            float waterDensity = WaterDensity[BoilerPressurePSI];
            BoilerHeatBTU += elapsedClockSeconds * (Evaporation - SteamUsageLBpS - BasicSteamUsageLBpS) * steamHeat;
            // mass assumed constant for automatic firing and injectors
            //BoilerMassLB -= elapsedClockSeconds * (SteamUsageLBpS + BasicSteamUsageLBpS);
            WaterFraction = (BoilerMassLB / BoilerVolumeFT3 - steamDensity) / (waterDensity - steamDensity);
            float waterHeat = (BoilerHeatBTU / BoilerVolumeFT3 - (1 - WaterFraction) * steamDensity * steamHeat) / (WaterFraction * waterDensity);
            BoilerPressurePSI = Heat2Pressure[waterHeat];
            if (BoilerPressurePSI > MaxBoilerPressurePSI)
            {
                BoilerHeatBTU = ((WaterHeat[MaxBoilerPressurePSI] * WaterFraction * waterDensity) + (1 - WaterFraction) * steamDensity * steamHeat) * BoilerVolumeFT3;
                BoilerPressurePSI = MaxBoilerPressurePSI;
            }
        }

        public override string GetStatus()
        {
            float evap= Evaporation*3600;
            float usage= (SteamUsageLBpS+BasicSteamUsageLBpS)*3600;
            return string.Format("Pressure = {0}PSI\nGeneration = {1}\nUsage = {2}",
                BoilerPressurePSI.ToString("F0"),evap.ToString("F0"),usage.ToString("F0"));
                //BoilerHeatBTU,BoilerMassLB,WaterFraction.ToString("F2"));
        }
        public void ChangeReverser(float percent)
        {
            Train.MUReverserPercent += percent;
            if (Train.MUReverserPercent >= 0)
                Train.MUDirection = Direction.Forward;
            else
                Train.MUDirection = Direction.Reverse;
            if (Train.MUReverserPercent < -90) Train.MUReverserPercent = -90;
            if (Train.MUReverserPercent > 90) Train.MUReverserPercent = 90;
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
            // for example
            // if (UserInput.IsPressed(Keys.W)) Locomotive.SetDirection(Direction.Forward);
            if (UserInput.IsPressed(Keys.W)) SteamLocomotive.ChangeReverser(10);
            else if (UserInput.IsPressed(Keys.S)) SteamLocomotive.ChangeReverser(-10);
            else base.HandleUserInput(elapsedTime);
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
