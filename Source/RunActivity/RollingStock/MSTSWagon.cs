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

/*
 *    TrainCarSimulator
 *    
 *    TrainCarViewer
 *    
 *  Every TrainCar generates a FrictionForce.
 *  
 *  The viewer is a separate class object since there could be multiple 
 *  viewers potentially on different devices for a single car. 
 *  
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using MSTS;

namespace ORTS
{

///////////////////////////////////////////////////
///   SIMULATION BEHAVIOUR
///////////////////////////////////////////////////


    /// <summary>
    /// Represents the physical motion and behaviour of the car.
    /// </summary>
    
    public class MSTSWagon: TrainCar
    {
		public bool Pan = false;     // false = down; some wagon has pantograph
		public bool Pan1Up = false; // if the forwards pantograph is up
        public bool Pan2Up = false; // if the backwards pantograph is up
        public bool DoorLeftOpen = false;
		public bool DoorRightOpen = false;
		public bool MirrorOpen = false;

        // simulation parameters
        public float Variable1 = 0.0f;  // used to convey status to soundsource
        public float Variable2 = 0.0f;
        public float Variable3 = 0.0f;

        // wag file data
        public string MainShapeFileName = null;
        public string FreightShapeFileName = null;
        public float FreightAnimHeight = 0;
        public string InteriorShapeFileName = null; // passenger view shape file name
        public string MainSoundFileName = null;
        public string InteriorSoundFileName = null;
        public float WheelRadiusM = 1;          // provide some defaults in case it's missing from the wag
        public float DriverWheelRadiusM = 1.5f;    // provide some defaults in case i'ts missing from the wag
        public float Friction0N = 0;    // static friction
        public bool IsStandStill = true;
        public float DavisAN = 0;       // davis equation constant
        public float DavisBNSpM = 0;    // davis equation constant for speed
        public float DavisCNSSpMM = 0;  // davis equation constant for speed squared
        public List<MSTSCoupling> Couplers = new List<MSTSCoupling>();
        public float Adhesion1 = .27f;   // 1st MSTS adhesion value
        public float Adhesion2 = .49f;   // 2nd MSTS adhesion value
        public float Adhesion3 = 2;   // 3rd MSTS adhesion value
        public float Curtius_KnifflerA = 7.5f;               //Curtius-Kniffler constants                   A
        public float Curtius_KnifflerB = 44.0f;              // (adhesion coeficient)       umax = ---------------------  + C
        public float Curtius_KnifflerC = 0.161f;             //                                      speedMpS * 3.6 + B
        public float AdhesionK = 0.7f;   //slip characteristics slope
        //public AntislipControl AntislipControl = AntislipControl.None;
        public float AxleInertiaKgm2 = 0;   //axle inertia
        public float WheelSpeedMpS = 0;
        public float SlipWarningTresholdPercent = 70;
        public float NumWheelsBrakingFactor = 4;   // MSTS braking factor loosely based on the number of braked wheels. Not used yet.

        public MSTSBrakeSystem MSTSBrakeSystem { get { return (MSTSBrakeSystem)base.BrakeSystem; } }

        public MSTSWagon(Simulator simulator, string wagFilePath)
            : base(simulator, wagFilePath)
        {
            if (CarManager.LoadedCars.ContainsKey(wagFilePath))
            {
                InitializeFromCopy(CarManager.LoadedCars[wagFilePath]);
            }
            else
            {
                InitializeFromWagFile(wagFilePath);
                CarManager.LoadedCars.Add(wagFilePath, this);
            }
        }

        /// <summary>
        /// This initializer is called when we haven't loaded this type of car before
        /// and must read it new from the wag file.
        /// </summary>
        public virtual void InitializeFromWagFile(string wagFilePath)
        {
            string dir = Path.GetDirectoryName(wagFilePath);
            string file = Path.GetFileName(wagFilePath);
            string orFile = dir + @"\openrails\" + file;
            if (File.Exists(orFile))
                wagFilePath = orFile;
            using (STFReader stf = new STFReader(wagFilePath, true))
                while (!stf.Eof)
                {
                    string token = stf.ReadItem();
                    Parse(stf.Tree.ToLower(), stf);
                }
             if (BrakeSystem == null)
                    BrakeSystem = new AirSinglePipe(this);
        }

        string brakeSystemType = null;

        /// <summary>
        /// Parse the wag file parameters required for the simulator and viewer classes
        /// </summary>
        public virtual void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "wagon(wagonshape": MainShapeFileName = stf.ReadStringBlock(null); break;
		        case "wagon(type":
		            stf.MustMatch("(");
		            string typeString = stf.ReadString();
		            IsFreight = String.Compare(typeString,"Freight") == 0 ? true : false;
		            break;
                case "wagon(freightanim":
                    stf.MustMatch("(");
                    FreightShapeFileName = stf.ReadString();
                    FreightAnimHeight = stf.ReadFloat(STFReader.UNITS.Distance, null) - stf.ReadFloat(STFReader.UNITS.Distance, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(size":
                    stf.MustMatch("(");
                    stf.ReadFloat(STFReader.UNITS.Distance, null);
                    Height = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    Length = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(mass": MassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "wagon(wheelradius": WheelRadiusM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "engine(wheelradius": DriverWheelRadiusM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(sound": MainSoundFileName = stf.ReadStringBlock(null); break;
                case "wagon(friction": ParseFriction(stf); break;
                case "wagon(brakesystemtype":
                    brakeSystemType = stf.ReadStringBlock(null).ToLower();
                    BrakeSystem = MSTSBrakeSystem.Create(brakeSystemType, this);
                    break;
                case "wagon(coupling":
                    Couplers.Add(new MSTSCoupling());
                    break;
                case "wagon(coupling(couplinghasrigidconnection":
                    Couplers[Couplers.Count - 1].Rigid = stf.ReadBoolBlock(true);
                    break;
                case "wagon(coupling(spring(stiffness":
                    stf.MustMatch("(");
                    Couplers[Couplers.Count - 1].SetStiffness(stf.ReadFloat(STFReader.UNITS.Stiffness, null), stf.ReadFloat(STFReader.UNITS.Stiffness, null));
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(coupling(spring(break":
                    stf.MustMatch("(");
                    Couplers[Couplers.Count - 1].SetBreak(stf.ReadFloat(STFReader.UNITS.Force, null), stf.ReadFloat(STFReader.UNITS.Force, null));
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(coupling(spring(r0":
                    stf.MustMatch("(");
                    Couplers[Couplers.Count - 1].SetR0(stf.ReadFloat(STFReader.UNITS.Distance, null), stf.ReadFloat(STFReader.UNITS.Distance, null));
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(adhesion":  // Permits correct spelling
                case "wagon(adheasion":
                    stf.MustMatch("(");
                    Adhesion1 = stf.ReadFloat(STFReader.UNITS.Any, null);
                    Adhesion2 = stf.ReadFloat(STFReader.UNITS.Any, null);
                    Adhesion3 = stf.ReadFloat(STFReader.UNITS.Any, null);
                    stf.ReadFloat(STFReader.UNITS.Any, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(or_adhesion(curtius_kniffler":   
                    stf.MustMatch("(");                      //e.g. Wagon ( OR_adhesion ( Curtius_Kniffler ( 7.5 44 0.161 0.7 ) ) )
                    Curtius_KnifflerA = stf.ReadFloat(STFReader.UNITS.Any, 7.5f);   if (Curtius_KnifflerA <= 0) Curtius_KnifflerA = 7.5f;
                    Curtius_KnifflerB = stf.ReadFloat(STFReader.UNITS.Any, 44.0f);  if (Curtius_KnifflerB <= 0) Curtius_KnifflerB = 44.0f;
                    Curtius_KnifflerC = stf.ReadFloat(STFReader.UNITS.Any, 0.161f); if (Curtius_KnifflerC <= 0) Curtius_KnifflerA = 0.161f;
                    AdhesionK = stf.ReadFloat(STFReader.UNITS.Any, 0.7f);           if (AdhesionK <= 0) AdhesionK = 0.7f;
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(or_adhesion(slipwarningtreshold":
                    stf.MustMatch("(");
                    SlipWarningTresholdPercent = stf.ReadFloat(STFReader.UNITS.Any, 70.0f); if (SlipWarningTresholdPercent <= 0) SlipWarningTresholdPercent = 70.0f ; 
                    stf.ReadFloat(STFReader.UNITS.Any, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(or_adhesion(antislip":
                    stf.MustMatch("(");
                    //AntislipControl = stf.ReadStringBlock(null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(or_adhesion(wheelset(axle(inertia":
                    stf.MustMatch("(");                    
                    AxleInertiaKgm2 = stf.ReadFloat(STFReader.UNITS.Any, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(or_adhesion(wheelset(axle(radius":
                    stf.MustMatch("(");
                    // <CJComment> Shouldn't this be "WheelRadiusM = " ? </CJComment>
                    AxleInertiaKgm2 = stf.ReadFloatBlock(STFReader.UNITS.Distance, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(lights":
                    if (Simulator.Settings.TrainLights)
                    {
                        try { Lights = new LightCollection(stf); }
                        catch { Lights = null; }
                    }
                    else
                        stf.SkipBlock();
                    break;
                case "wagon(inside": ParseWagonInside(stf); break;
                case "wagon(numwheels": NumWheelsBrakingFactor = stf.ReadFloatBlock(STFReader.UNITS.None, 4.0f); break;
                default:
                    if (MSTSBrakeSystem != null)
                        MSTSBrakeSystem.Parse(lowercasetoken, stf);
                    break;
            }
        }

        /// <summary>
        /// This initializer is called when we are making a new copy of a car already
        /// loaded in memory.  We use this one to speed up loading by eliminating the
        /// need to parse the wag file multiple times.
        /// 
        /// IMPORTANT NOTE:  everything you initialized in parse, must be initialized here
        /// </summary>
        public virtual void InitializeFromCopy(MSTSWagon copy)
        {
            MainShapeFileName = copy.MainShapeFileName;
            FreightShapeFileName = copy.FreightShapeFileName;
            FreightAnimHeight = copy.FreightAnimHeight;
            IsFreight = copy.IsFreight;
            InteriorShapeFileName = copy.InteriorShapeFileName;
            MainSoundFileName = copy.MainSoundFileName;
            InteriorSoundFileName = copy.InteriorSoundFileName;
            WheelRadiusM = copy.WheelRadiusM;
            DriverWheelRadiusM = copy.DriverWheelRadiusM;
            Friction0N = copy.Friction0N;
            DavisAN = copy.DavisAN;
            DavisBNSpM = copy.DavisBNSpM;
            DavisCNSSpMM = copy.DavisCNSSpMM;
            Length = copy.Length;
			Height = copy.Height;
            MassKG = copy.MassKG;
            Adhesion1 = copy.Adhesion1;
            Adhesion2 = copy.Adhesion2;
            Adhesion3 = copy.Adhesion3;
            Curtius_KnifflerA = copy.Curtius_KnifflerA;
            Curtius_KnifflerB = copy.Curtius_KnifflerB;
            Curtius_KnifflerC = copy.Curtius_KnifflerC;
            AdhesionK = copy.AdhesionK;
            AxleInertiaKgm2 = copy.AxleInertiaKgm2;
            SlipWarningTresholdPercent = copy.SlipWarningTresholdPercent;
            Lights = copy.Lights;
            foreach (ViewPoint passengerViewPoint in copy.PassengerViewpoints)
                PassengerViewpoints.Add(passengerViewPoint);
            foreach (ViewPoint headOutViewPoint in copy.HeadOutViewpoints)
                HeadOutViewpoints.Add(headOutViewPoint);
            foreach (MSTSCoupling coupler in copy.Couplers)
                Couplers.Add(coupler);

            brakeSystemType = copy.brakeSystemType;
            BrakeSystem = MSTSBrakeSystem.Create(brakeSystemType, this);
            MSTSBrakeSystem.InitializeFromCopy(copy.BrakeSystem);
        }
        private void ParseWagonInside(STFReader stf)
        {
            ViewPoint passengerViewPoint = new ViewPoint();
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("sound", ()=>{ InteriorSoundFileName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("passengercabinfile", ()=>{ InteriorShapeFileName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("passengercabinheadpos", ()=>{ passengerViewPoint.Location = stf.ReadVector3Block(STFReader.UNITS.Distance, new Vector3()); }),
                new STFReader.TokenProcessor("rotationlimit", ()=>{ passengerViewPoint.RotationLimit = stf.ReadVector3Block(STFReader.UNITS.None, new Vector3()); }),
                new STFReader.TokenProcessor("startdirection", ()=>{ passengerViewPoint.StartDirection = stf.ReadVector3Block(STFReader.UNITS.None, new Vector3()); }),
            });
            PassengerViewpoints.Add(passengerViewPoint);
        }
        public void ParseFriction(STFReader stf)
        {
            stf.MustMatch("(");
            float c1 = stf.ReadFloat(STFReader.UNITS.Resistance, null);
            float e1 = stf.ReadFloat(STFReader.UNITS.None, null);
            float v2 = stf.ReadFloat(STFReader.UNITS.Speed,null);
            float c2 = stf.ReadFloat(STFReader.UNITS.Resistance, null);
            float e2 = stf.ReadFloat(STFReader.UNITS.None, null);
            stf.SkipRestOfBlock();
            if (v2 < 0 || v2 > 4.4407f)
            {   // not fcalc ignore friction and use default davis equation
                // Starting Friction 
                //
                //                      Above Freezing   Below Freezing
                //    Journal Bearing      25 lb/ton        35 lb/ton   (short ton)
                //     Roller Bearing       5 lb/ton        15 lb/ton
                //
                // [2009-10-25 from http://www.arema.org/publications/pgre/ ]
                //Friction0N = MassKG * 30f /* lb/ton */ * 4.84e-3f;  // convert lbs/short-ton to N/kg 
                DavisAN = 6.3743f * MassKG / 1000 + 128.998f * 4;
                DavisBNSpM = .49358f * MassKG / 1000;
                DavisCNSSpMM = .11979f * 100 / 10.76f;
                Friction0N = DavisAN * 2.0f;            //More firendly to high load trains and the new physics
            }
            else
            {   // probably fcalc, recover approximate davis equation
                float mps1 = v2;
                float mps2 = 80 * .44704f;
                float s = mps2 - mps1;
                float x1 = mps1 * mps1;
                float x2 = mps2 * mps2;
                float sx = (x2 - x1) / 2;
                float y0 = c1 * (float)Math.Pow(mps1, e1) + c2 * mps1;
                float y1 = c2 * (float)Math.Pow(mps1, e2) * mps1;
                float y2 = c2 * (float)Math.Pow(mps2, e2) * mps2;
                float sy = y0 * (mps2 - mps1) + (y2 - y1) / (1 + e2);
                y1 *= mps1;
                y2 *= mps2;
                float syx = y0 * (x2 - x1) / 2 + (y2 - y1) / (2 + e2);
                x1 *= mps1;
                x2 *= mps2;
                float sx2 = (x2 - x1) / 3;
                y1 *= mps1;
                y2 *= mps2;
                float syx2 = y0 * (x2 - x1) / 3 + (y2 - y1) / (3 + e2);
                x1 *= mps1;
                x2 *= mps2;
                float sx3 = (x2 - x1) / 4;
                x1 *= mps1;
                x2 *= mps2;
                float sx4 = (x2 - x1) / 5;
                float s1 = syx - sy * sx / s;
                float s2 = sx * sx2 / s - sx3;
                float s3 = sx2 - sx * sx / s;
                float s4 = syx2 - sy * sx2 / s;
                float s5 = sx2 * sx2 / s - sx4;
                float s6 = sx3 - sx * sx2 / s;
                DavisCNSSpMM = (s1 * s6 - s3 * s4) / (s3 * s5 - s2 * s6);
                DavisBNSpM = (s1 + DavisCNSSpMM * s2) / s3;
                DavisAN = (sy - DavisBNSpM * sx - DavisCNSSpMM * sx2) / s;
                Friction0N = c1;
                if (e1 < 0)
                    Friction0N *= (float)Math.Pow(.0025 * .44704, e1);
            }
        }
        public float ParseFloat(string token)
        {   // is there a better way to ignore any suffix?
            while (token.Length > 0)
            {
                try
                {
                    return float.Parse(token);
                }
                catch (System.Exception)
                {
                    token = token.Substring(0, token.Length - 1);
                }
            }
            return 0;
        }

        /// <summary>
        /// We are saving the game.  Save anything that we'll need to restore the 
        /// status later.
        /// </summary>
        public override void Save(BinaryWriter outf)
        {
            outf.Write(Variable1);
            outf.Write(Variable2);
            outf.Write(Variable3);
            outf.Write(Friction0N);
            outf.Write(DavisAN);
            outf.Write(DavisBNSpM);
            outf.Write(DavisCNSSpMM);
            outf.Write(Couplers.Count);
            foreach (MSTSCoupling coupler in Couplers)
                coupler.Save(outf);
            base.Save(outf);
        }

        /// <summary>
        /// We are restoring a saved game.  The TrainCar class has already
        /// been initialized.   Restore the game state.
        /// </summary>
        public override void Restore(BinaryReader inf)
        {
            Variable1 = inf.ReadSingle();
            Variable2 = inf.ReadSingle();
            Variable3 = inf.ReadSingle();
            Friction0N = inf.ReadSingle();
            DavisAN = inf.ReadSingle();
            DavisBNSpM = inf.ReadSingle();
            DavisCNSSpMM = inf.ReadSingle();
            int n = inf.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                Couplers.Add(new MSTSCoupling());
                Couplers[i].Restore(inf);
            }
            base.Restore(inf);
        }


        public override TrainCarViewer GetViewer(Viewer3D viewer)
        {
            // warning - don't assume there is only one viewer, or that there are any viewers at all.
            // Best practice is not to give the TrainCar class any knowledge of its viewers.
            return new MSTSWagonViewer(viewer, this);
        }

        public override void Update( float elapsedClockSeconds )
        {
            base.Update(elapsedClockSeconds);

            float s = Math.Abs(SpeedMpS);
            if (s > 0.1)
                IsStandStill = false;
            if (s == 0.0)
                IsStandStill = true;

            if(IsStandStill)
                FrictionForceN = Friction0N;
            else
                FrictionForceN = DavisAN + s * (DavisBNSpM + s * DavisCNSSpMM);

            foreach (MSTSCoupling coupler in Couplers)
            {
                if (-CouplerForceU > coupler.Break1N)
                {
                    CouplerOverloaded = true;
                }
                else
                    CouplerOverloaded = false;
            }

            MSTSBrakeSystem.Update(elapsedClockSeconds);
        }

        public override void SignalEvent(Event evt)
        {
            switch (evt)
            {
                case Event.Pantograph1Up: { Pan1Up = true; Pan = Pan1Up || Pan2Up; break; }
                case Event.Pantograph1Down: { Pan1Up = false; Pan = Pan1Up || Pan2Up; break; }
                case Event.Pantograph2Up: { Pan2Up = true; Pan = Pan1Up || Pan2Up; break; }
                case Event.Pantograph2Down: { Pan2Up = false; Pan = Pan1Up || Pan2Up; break; }
            }

            // TODO: This should be moved to TrainCar probably.
            foreach (var eventHandler in EventHandlers) // e.g. for HandleCarEvent() in Sounds.cs
                eventHandler.HandleEvent(evt);

            base.SignalEvent(evt);
        }

        // <CJComment> Expected pantograph handling to be in MSTSElectricLocomotive.cs,
        // but guess that some trains have pantographs on non-motorised cars </CJComment>
        public void ToggleFirstPantograph() {
    		Pan1Up = !Pan1Up;
            if( Simulator.PlayerLocomotive == this ) //inform everyone else in the train
                foreach( TrainCar car in Train.Cars )
                    if( car != this && car is MSTSWagon ) ((MSTSWagon)car).Pan1Up = Pan1Up;
            if( Pan1Up ) {
                SignalEvent(Event.Pantograph1Up);
            } else {
                SignalEvent(Event.Pantograph1Down);
            }
        }

        public void ToggleSecondPantograph() {
            Pan2Up = !Pan2Up;
            if( Simulator.PlayerLocomotive == this ) //inform everyone else in the train
                foreach( TrainCar car in Train.Cars )
                    if( car != this && car is MSTSWagon ) ((MSTSWagon)car).Pan2Up = Pan2Up;
            if( Pan2Up ) {
                SignalEvent(Event.Pantograph2Up);
            } else {
                SignalEvent(Event.Pantograph2Down);
            }
        }
        
        public void ToggleDoorsLeft() {
            DoorLeftOpen = !DoorLeftOpen;
            if( Simulator.PlayerLocomotive == this ) {//inform everyone else in the train
                foreach( TrainCar car in Train.Cars ) {
                    if (car != this && car is MSTSWagon)
                    {
                        ((MSTSWagon)car).DoorLeftOpen = DoorLeftOpen;
                    }
                }
                /*if (MSTSWagon.DoorLeftOpen) Car.SignalEvent(EventID.DoorOpen);
                else Car.SignalEvent(EventID.DoorClose);*/
                //comment out, but can be added back to animate sound
                Simulator.Confirmer.Confirm( CabControl.DoorsLeft, DoorLeftOpen ? CabSetting.On : CabSetting.Off );
            }
        }

        public void ToggleDoorsRight() {
            DoorRightOpen = !DoorRightOpen;
            if( Simulator.PlayerLocomotive == this ) { //inform everyone else in the train
                foreach( TrainCar car in Train.Cars ) {
                    if( car != this && car is MSTSWagon ) ((MSTSWagon)car).DoorRightOpen = DoorRightOpen;
                }
                /*if (MSTSWagon.DoorLeftOpen) Car.SignalEvent(EventID.DoorOpen);
                else Car.SignalEvent(EventID.DoorClose);*/
                //comment out, but can be added back to animate sound
                Simulator.Confirmer.Confirm( CabControl.DoorsRight, DoorRightOpen ? CabSetting.On : CabSetting.Off );
            }
        }

        public void ToggleMirrors() {
            MirrorOpen = !MirrorOpen;
            Simulator.Confirmer.Confirm( CabControl.Mirror, MirrorOpen ? CabSetting.On : CabSetting.Off );
        }

        // sound sources and viewers can register themselves to get direct notification of an event
        public List<EventHandler> EventHandlers = new List<EventHandler>();

        public MSTSCoupling Coupler
        {
            get
            {
                if (Couplers.Count == 0) return null;
                if (Flipped && Couplers.Count > 1) return Couplers[1];
                return Couplers[0];
            }
        }
        public override float GetCouplerZeroLengthM()
        {
            return Coupler != null ? Coupler.R0 : base.GetCouplerZeroLengthM();
        }

        public override float GetCouplerStiffnessNpM()
        {
            return Coupler != null && Coupler.R0 == 0 ? 7 * (Coupler.Stiffness1NpM + Coupler.Stiffness2NpM) : base.GetCouplerStiffnessNpM();
        }

        public override float GetMaximumCouplerSlack1M()
        {
            if (Coupler == null)
                return base.GetMaximumCouplerSlack1M();
            return Coupler.Rigid ? 0.0001f : Coupler.R0Diff;
        }

        public override float GetMaximumCouplerSlack2M()
        {
            if (Coupler == null)
                return base.GetMaximumCouplerSlack2M();
            return Coupler.Rigid ? 0.0002f : base.GetMaximumCouplerSlack2M();
        }
        public override void CopyCoupler(TrainCar other)
        {
            base.CopyCoupler(other);
            MSTSCoupling coupler = new MSTSCoupling();
            coupler.R0 = other.GetCouplerZeroLengthM();
            coupler.R0Diff = other.GetMaximumCouplerSlack1M();
            coupler.Rigid = coupler.R0Diff < .0002f;
            coupler.Stiffness1NpM = other.GetCouplerStiffnessNpM() / 7;
            coupler.Stiffness2NpM = 0;
            Couplers[0]= coupler;
            if (Couplers.Count > 1)
                Couplers.RemoveAt(1);
        }
    }

    public class MSTSCoupling
    {
        public bool Rigid = false;
        public float R0 = 0;
        public float R0Diff = .012f;
        public float Stiffness1NpM = 1e7f;
        public float Stiffness2NpM = 2e7f;
        public float Break1N = 1e10f;
        public float Break2N = 1e10f;

        public MSTSCoupling()
        {
        }
        public MSTSCoupling(MSTSCoupling copy)
        {
            Rigid = copy.Rigid;
            R0 = copy.R0;
            R0Diff = copy.R0Diff;
            Break1N = copy.Break1N;
            Break2N = copy.Break2N;
        }
        public void SetR0(float a, float b)
        {
            R0 = a;
            if (a == 0)
                R0Diff = b / 2 * Stiffness2NpM / (Stiffness1NpM + Stiffness2NpM);
            else
                R0Diff = .012f;
            if (R0Diff < .001)
                R0Diff = .001f;
            else if (R0Diff > .1)
                R0Diff = .1f;
        }
        public void SetStiffness(float a, float b)
        {
            if (a + b < 0)
                return;
            Stiffness1NpM = a;
            Stiffness2NpM = b;
        }

        public void SetBreak(float a, float b)
        {
            if (a + b < 0)
                return;
            Break1N = a;
            Break2N = b;
        }

        /// <summary>
        /// We are saving the game.  Save anything that we'll need to restore the 
        /// status later.
        /// </summary>
        public void Save(BinaryWriter outf)
        {
            outf.Write(Rigid);
            outf.Write(R0);
            outf.Write(R0Diff);
            outf.Write(Stiffness1NpM);
            outf.Write(Stiffness2NpM);
            outf.Write(Break1N);
            outf.Write(Break2N);
        }

        /// <summary>
        /// We are restoring a saved game.  The TrainCar class has already
        /// been initialized.   Restore the game state.
        /// </summary>
        public void Restore(BinaryReader inf)
        {
            Rigid = inf.ReadBoolean();
            R0 = inf.ReadSingle();
            R0Diff = inf.ReadSingle();
            Stiffness1NpM = inf.ReadSingle();
            Stiffness2NpM = inf.ReadSingle();
            Break1N = inf.ReadSingle();
            Break2N = inf.ReadSingle();
        }
    }

    /// <summary>
    /// Support for animating any sub-part of a wagon or locomotive. Supports both on/off toggled animations and continuous-running ones.
    /// </summary>
    public class AnimatedPart
    {
        // Shape that we're animating.
        readonly PoseableShape PoseableShape;

        // Number of animation key-frames that are used by this part. This is calculated from the matrices provided.
        int FrameCount = 0;

        // Current frame of the animation.
        float AnimationKey = 0;

        // List of the matrices we're animating for this part.
        List<int> MatrixIndexes = new List<int>();

        /// <summary>
        /// Construct with a link to the shape that contains the animated parts 
        /// </summary>
        public AnimatedPart(PoseableShape poseableShape)
        {
            PoseableShape = poseableShape;
        }

        /// <summary>
        /// All the matrices associated with this part are added during initialization by the MSTSWagon constructor
        /// </summary>
        public void AddMatrix(int matrix)
        {
            if (matrix < 0) return;
            MatrixIndexes.Add(matrix);
            UpdateFrameCount(matrix);
        }

        void UpdateFrameCount(int matrix)
        {
            if (PoseableShape.SharedShape.Animations != null
                && PoseableShape.SharedShape.Animations.Count > 0
                && PoseableShape.SharedShape.Animations[0].anim_nodes.Count > matrix
                && PoseableShape.SharedShape.Animations[0].anim_nodes[matrix].controllers.Count > 0
                && PoseableShape.SharedShape.Animations[0].anim_nodes[matrix].controllers[0].Count > 0)
            {
                FrameCount = Math.Max(FrameCount, PoseableShape.SharedShape.Animations[0].anim_nodes[matrix].controllers[0].ToArray().Cast<KeyPosition>().Last().Frame);
            }
            for (var i = 0; i < PoseableShape.Hierarchy.Length; i++)
                if (PoseableShape.Hierarchy[i] == matrix)
                    UpdateFrameCount(i);
        }

        /// <summary>
        /// Ensure the shape file contained parts of this type 
        /// and those parts have an animation section.
        /// </summary>
        public bool Empty()
        {
            return MatrixIndexes.Count == 0;
        }

        void SetFrame(float frame)
        {
            AnimationKey = frame;
            foreach (var matrix in MatrixIndexes)
                PoseableShape.AnimateMatrix(matrix, AnimationKey);
        }

        /// <summary>
        /// Sets the animation to a particular frame whilst clamping it to the frame count range.
        /// </summary>
        public void SetFrameClamp(float frame)
        {
            if (frame > FrameCount) frame = FrameCount;
            if (frame < 0) frame = 0;
            SetFrame(frame);
        }

        /// <summary>
        /// Sets the animation to a particular frame whilst cycling back to the start as input goes beyond the last frame.
        /// </summary>
        public void SetFrameCycle(float frame)
        {
            // Animates from 0-FrameCount then FrameCount-0 for values of 0>=frame<=2*FrameCount.
            SetFrameClamp(FrameCount - Math.Abs(frame - FrameCount));
        }

        /// <summary>
        /// Sets the animation to a particular frame whilst wrapping it around the frame count range.
        /// </summary>
        public void SetFrameWrap(float frame)
        {
            // Wrap the frame around 0-FrameCount without hanging when FrameCount=0.
            while (FrameCount > 0 && frame < 0) frame += FrameCount;
            if (frame < 0) frame = 0;
            frame %= FrameCount;
            SetFrame(frame);
        }

        /// <summary>
        /// Bypass the normal slow transition and jump the part immediately to this new state
        /// </summary>
        public void SetState(bool state)
        {
            SetFrame(state ? FrameCount : 0);
        }

        /// <summary>
        /// Updates an animated part that toggles between two states (e.g. pantograph, doors, mirrors).
        /// </summary>
        public void UpdateState(bool state, ElapsedTime elapsedTime)
        {
            SetFrameClamp(AnimationKey + (state ? 1 : -1) * elapsedTime.ClockSeconds);
        }

        /// <summary>
        /// Updates an animated part that loops (e.g. running gear), changing by the given amount.
        /// </summary>
        public void UpdateLoop(float change)
        {
            if (PoseableShape.SharedShape.Animations == null || PoseableShape.SharedShape.Animations.Count == 0 || FrameCount == 0)
                return;

            // The speed of rotation is set at 8 frames of animation per rotation at 30 FPS (so 16 frames = 60 FPS, etc.).
            var frameRate = PoseableShape.SharedShape.Animations[0].FrameRate * 8 / 30f;
            SetFrameWrap(AnimationKey + change * frameRate);
        }

        /// <summary>
        /// Updates an animated part that loops only when enabled (e.g. wipers).
        /// </summary>
        public void UpdateLoop(bool running, ElapsedTime elapsedTime)
        {
            if (PoseableShape.SharedShape.Animations == null || PoseableShape.SharedShape.Animations.Count == 0 || FrameCount == 0)
                return;

            // The speed of cycling is set at 1.5 frames of animation per second at 30 FPS.
            var frameRate = PoseableShape.SharedShape.Animations[0].FrameRate * 1.5f / 30f;
            if (running || (AnimationKey > 0 && AnimationKey + elapsedTime.ClockSeconds < FrameCount))
                SetFrameWrap(AnimationKey + elapsedTime.ClockSeconds * frameRate);
            else
                SetFrame(0);
        }

        /// <summary>
        /// Swap the pointers around.
        /// </summary>
        public static void Swap(ref AnimatedPart a, ref AnimatedPart b)
        {
            AnimatedPart temp = a;
            a = b;
            b = temp;
        }
    }



    ///////////////////////////////////////////////////
    ///   3D VIEW
    ///////////////////////////////////////////////////

    /// <summary>
    /// Note:  we need a separate viewer class since there could be multiple viewers
    /// for a single traincar, or possibly none
    /// </summary>

    public class MSTSWagonViewer: TrainCarViewer
    {
        protected PoseableShape TrainCarShape;
        protected AnimatedShape FreightShape;
        protected AnimatedShape InteriorShape;
        protected List<SoundSourceBase> SoundSources = new List<SoundSourceBase>();

        // Wheels are rotated by hand instead of in the shape file.
        float WheelRotationR;
        List<int> WheelPartIndexes = new List<int>();

        // Everything else is animated through the shape file.
        AnimatedPart RunningGear;
		AnimatedPart Pantograph1;
		AnimatedPart Pantograph2;
		AnimatedPart LeftDoor;
		AnimatedPart RightDoor;
		AnimatedPart Mirrors;
        protected AnimatedPart Wipers;

        protected MSTSWagon MSTSWagon { get { return (MSTSWagon) Car; } }
        protected Viewer3D _Viewer3D;

        bool HasFirstPanto = false;
        public MSTSWagonViewer(Viewer3D viewer, MSTSWagon car)
            : base(viewer, car)
        {
            _Viewer3D = viewer;
            var wagonFolderSlash = Path.GetDirectoryName(car.WagFilePath) + @"\";
            var shapePath = wagonFolderSlash + car.MainShapeFileName;

            TrainCarShape = new PoseableShape(viewer, shapePath + '\0' + wagonFolderSlash, car.WorldPosition, ShapeFlags.ShadowCaster);

            if (car.FreightShapeFileName != null)
                FreightShape = new AnimatedShape(viewer, wagonFolderSlash + car.FreightShapeFileName + '\0' + wagonFolderSlash, car.WorldPosition, ShapeFlags.ShadowCaster);

            if (car.InteriorShapeFileName != null)
                InteriorShape = new AnimatedShape(viewer, wagonFolderSlash + car.InteriorShapeFileName + '\0' + wagonFolderSlash, car.WorldPosition);

            RunningGear = new AnimatedPart(TrainCarShape);
            Pantograph1 = new AnimatedPart(TrainCarShape);
            Pantograph2 = new AnimatedPart(TrainCarShape);
            LeftDoor = new AnimatedPart(TrainCarShape);
            RightDoor = new AnimatedPart(TrainCarShape);
            Mirrors = new AnimatedPart(TrainCarShape);
            Wipers = new AnimatedPart(TrainCarShape);

            LoadCarSounds(wagonFolderSlash);
            if (!(MSTSWagon is MSTSLocomotive))
                LoadTrackSounds();

            // Adding all loaded SoundSource to the main sound update thread
            _Viewer3D.SoundProcess.AddSoundSource(this, SoundSources);

            // Determine if it has first pantograph. So we can match unnamed panto parts correctly
            for (var i = 0; i < TrainCarShape.Hierarchy.Length; i++)
                if (TrainCarShape.SharedShape.MatrixNames[i].Contains('1')) {
                    if (TrainCarShape.SharedShape.MatrixNames[i].ToUpper().StartsWith("PANTO")) { HasFirstPanto = true; break; }
                }

            // Match up all the matrices with their parts.
            for (var i = 0; i < TrainCarShape.Hierarchy.Length; i++)
                if (TrainCarShape.Hierarchy[i] == -1)
                    MatchMatrixToPart(car, i);

            car.SetUpWheels();

            // If we have two pantographs, 2 is the forwards pantograph, unlike when there's only one.
            if (!car.Flipped && !Pantograph1.Empty() && !Pantograph2.Empty())
                AnimatedPart.Swap(ref Pantograph1, ref Pantograph2);

            // If the car is flipped, the doors should be corrected to match the rest of the train.
            if (car.Flipped)
                AnimatedPart.Swap(ref RightDoor, ref LeftDoor);

            Pantograph1.SetState(MSTSWagon.Pan1Up);
            Pantograph2.SetState(MSTSWagon.Pan2Up);
            LeftDoor.SetState(MSTSWagon.DoorLeftOpen);
            RightDoor.SetState(MSTSWagon.DoorRightOpen);
            Mirrors.SetState(MSTSWagon.MirrorOpen);
        }

        void MatchMatrixToPart(MSTSWagon car, int matrix)
        {
            var matrixName = TrainCarShape.SharedShape.MatrixNames[matrix].ToUpper();
            // Gate all RunningGearPartIndexes on this!
            var matrixAnimated = TrainCarShape.SharedShape.Animations != null && TrainCarShape.SharedShape.Animations.Count > 0 && TrainCarShape.SharedShape.Animations[0].anim_nodes.Count > matrix && TrainCarShape.SharedShape.Animations[0].anim_nodes[matrix].controllers.Count > 0;
            if (matrixName.StartsWith("WHEELS") && matrixName.Length == 7 | matrixName.Length == 8)
            {
                var m = TrainCarShape.SharedShape.GetMatrixProduct(matrix);
                //someone uses wheel to animate fans, thus check if the wheel is not too high (lower than 3m), will animate it as real wheel
                if (m.M42 < 3)
                {
                    var id = 0;
                    if (matrixName.Length == 8)
                        Int32.TryParse(matrixName.Substring(6, 1), out id);
                    if (matrixName.Length == 8 || !matrixAnimated)
                        WheelPartIndexes.Add(matrix);
                    else
                        RunningGear.AddMatrix(matrix);
                    var pmatrix = TrainCarShape.SharedShape.GetParentMatrix(matrix);
                    car.AddWheelSet(m.M43, id, pmatrix);
                }
            }
            else if (matrixName.StartsWith("BOGIE"))
            {
                var id = 1;
                Int32.TryParse(matrixName.Substring(5), out id);
                var m = TrainCarShape.SharedShape.GetMatrixProduct(matrix);
                car.AddBogie(m.M43, matrix, id);

                // Bogies contain wheels!
                for (var i = 0; i < TrainCarShape.Hierarchy.Length; i++)
                    if (TrainCarShape.Hierarchy[i] == matrix)
                        MatchMatrixToPart(car, i);
            }
            else if (matrixName.StartsWith("WIPER")) // wipers
            {
                Wipers.AddMatrix(matrix);
            }
            else if (matrixName.StartsWith("DOOR")) // doors (left / right)
            {
                if (matrixName.StartsWith("DOOR_D") || matrixName.StartsWith("DOOR_E") || matrixName.StartsWith("DOOR_F"))
                    LeftDoor.AddMatrix(matrix);
                else if (matrixName.StartsWith("DOOR_A") || matrixName.StartsWith("DOOR_B") || matrixName.StartsWith("DOOR_C"))
                    RightDoor.AddMatrix(matrix);
            }
            else if (matrixName.StartsWith("PANTOGRAPH")) //pantographs (1/2)
            {

                switch (matrixName)
                {
                    case "PANTOGRAPHBOTTOM1":
                    case "PANTOGRAPHBOTTOM1A":
                    case "PANTOGRAPHBOTTOM1B":
                    case "PANTOGRAPHMIDDLE1":
                    case "PANTOGRAPHMIDDLE1A":
                    case "PANTOGRAPHMIDDLE1B":
                    case "PANTOGRAPHTOP1":
                    case "PANTOGRAPHTOP1A":
                    case "PANTOGRAPHTOP1B":
                        Pantograph1.AddMatrix(matrix);
                        break;
                    case "PANTOGRAPHBOTTOM2":
                    case "PANTOGRAPHBOTTOM2A":
                    case "PANTOGRAPHBOTTOM2B":
                    case "PANTOGRAPHMIDDLE2":
                    case "PANTOGRAPHMIDDLE2A":
                    case "PANTOGRAPHMIDDLE2B":
                    case "PANTOGRAPHTOP2":
                    case "PANTOGRAPHTOP2A":
                    case "PANTOGRAPHTOP2B":
                        Pantograph2.AddMatrix(matrix);
                        break;
                    default ://someone used other language
                        if (matrixName.Contains("1"))
                            Pantograph1.AddMatrix(matrix);
                        else if (matrixName.Contains("2"))
                            Pantograph2.AddMatrix(matrix);
                        else
                        {
                            if (HasFirstPanto) Pantograph1.AddMatrix(matrix); //some may have no first panto, will put it as panto 2
                            else Pantograph2.AddMatrix(matrix);
                        }
                        break;
                }
            }
            else if (matrixName.StartsWith("MIRROR")) // mirrors
            {
                Mirrors.AddMatrix(matrix);
            }
            else if (matrixName.StartsWith("PANTO"))  // TODO, not sure why this is needed, see above!
            {
                Trace.TraceInformation("Pantrograph matrix with unusual name {1} in shape {0}", TrainCarShape.SharedShape.FilePath, matrixName);
                if (matrixName.Contains("1"))
                    Pantograph1.AddMatrix(matrix);
                else if (matrixName.Contains("2"))
                    Pantograph2.AddMatrix(matrix);
                else
                {
                    if (HasFirstPanto) Pantograph1.AddMatrix(matrix); //some may have no first panto, will put it as panto 2
                    else Pantograph2.AddMatrix(matrix);
                }
            }
            else
            {
                if (matrixAnimated && matrix != 0)
                    RunningGear.AddMatrix(matrix);

                for (var i = 0; i < TrainCarShape.Hierarchy.Length; i++)
                    if (TrainCarShape.Hierarchy[i] == matrix)
                        MatchMatrixToPart(car, i);
            }
        }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
			// Pantograph
			if (UserInput.IsPressed(UserCommands.ControlPantograph1))
			{
                new PantographCommand(Viewer.Log, 1, !MSTSWagon.Pan1Up);
			}
			if (UserInput.IsPressed(UserCommands.ControlPantograph2))
			{
                new PantographCommand(Viewer.Log, 2, !MSTSWagon.Pan1Up);
			}
			if (UserInput.IsPressed(UserCommands.ControlDoorLeft)) //control door (or only left)
			{
                new ToggleDoorsLeftCommand(Viewer.Log);
			}
			if (UserInput.IsPressed(UserCommands.ControlDoorRight)) //control right door
			{
                new ToggleDoorsRightCommand(Viewer.Log);
            }
			if (UserInput.IsPressed(UserCommands.ControlMirror))    // The mirrors on trams which swing out at platforms
			{
                new ToggleMirrorsCommand(Viewer.Log);
            }
		}

        /// <summary>
        /// Called at the full frame rate
        /// elapsedTime is time since last frame
        /// Executes in the UpdaterThread
        /// </summary>
        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            Pantograph1.UpdateState(MSTSWagon.Pan1Up, elapsedTime);
            Pantograph2.UpdateState(MSTSWagon.Pan2Up, elapsedTime);
            LeftDoor.UpdateState(MSTSWagon.DoorLeftOpen, elapsedTime);
            RightDoor.UpdateState(MSTSWagon.DoorRightOpen, elapsedTime);
            Mirrors.UpdateState(MSTSWagon.MirrorOpen, elapsedTime);
            UpdateAnimation(frame, elapsedTime);
        }


        private void UpdateAnimation( RenderFrame frame, ElapsedTime elapsedTime )
        {
            float distanceTravelledM;
            if (MSTSWagon.IsDriveable && MSTSWagon.Simulator.UseAdvancedAdhesion)
                distanceTravelledM = MSTSWagon.WheelSpeedMpS * elapsedTime.ClockSeconds;
            else
                distanceTravelledM = MSTSWagon.SpeedMpS * elapsedTime.ClockSeconds;

            // Running gear animation
            if (!RunningGear.Empty() && MSTSWagon.DriverWheelRadiusM > 0.001)
                RunningGear.UpdateLoop(distanceTravelledM / MathHelper.TwoPi / MSTSWagon.DriverWheelRadiusM);

            // Wheel animation
            if (WheelPartIndexes.Count > 0)
            {
                var wheelCircumferenceM = MathHelper.TwoPi * MSTSWagon.WheelRadiusM;
                var rotationalDistanceR = MathHelper.TwoPi * distanceTravelledM / wheelCircumferenceM;  // in radians
                WheelRotationR = MathHelper.WrapAngle(WheelRotationR - rotationalDistanceR);
                var wheelRotationMatrix = Matrix.CreateRotationX(WheelRotationR);
                foreach (var iMatrix in WheelPartIndexes)
                    TrainCarShape.XNAMatrices[iMatrix] = wheelRotationMatrix * TrainCarShape.SharedShape.Matrices[iMatrix];
            }

            // truck angle animation
            foreach (var p in Car.Parts)
            {
                if (p.iMatrix <= 0)
                    continue;
                Matrix m = Matrix.Identity;
                m.Translation= TrainCarShape.SharedShape.Matrices[p.iMatrix].Translation;
                m.M11 = p.Cos;
                m.M13 = p.Sin;
                m.M31 = -p.Sin;
                m.M33 = p.Cos;

                //if car vibrate, the bogie will stay on track, thus reverse it back (Car.SuperElevationMatrix holds the inverse)
                if ((Program.Simulator.CarVibrating > 0 || (this.Car.Train != null && this.Car.Train.tilted)) && p.bogie) TrainCarShape.XNAMatrices[p.iMatrix] = Car.SuperElevationMatrix * m;
                else TrainCarShape.XNAMatrices[p.iMatrix] = m;
            }

            if (FreightShape != null)
            {
                if (FreightShape.XNAMatrices.Length > 0)
                    FreightShape.XNAMatrices[0].M42 = MSTSWagon.FreightAnimHeight;
                FreightShape.PrepareFrame(frame, elapsedTime);
            }

            // Control visibility of passenger cabin when inside it
            if (Viewer.Camera.AttachedCar == this.MSTSWagon
                 && //( Viewer.ViewPoint == Viewer.ViewPoints.Cab ||  // TODO, restore when we complete cab views - 
                     Viewer.Camera.Style == Camera.Styles.Passenger)
            {
                // We are in the passenger cabin
                if (InteriorShape != null)
                    InteriorShape.PrepareFrame(frame, elapsedTime);
                else
                    TrainCarShape.PrepareFrame(frame, elapsedTime);
            }
            else
            {
                // Skip drawing if CAB view - draw 2D view instead - by GeorgeS
                if (Viewer.Camera.AttachedCar == this.MSTSWagon &&
                    Viewer.Camera.Style == Camera.Styles.Cab)
                    return;
                
                // We are outside the passenger cabin
                TrainCarShape.PrepareFrame(frame, elapsedTime);
            }

        }



        /// <summary>
        /// Unload and release the car - its not longer being displayed
        /// </summary>
        public virtual void Unload()
        {
            // Removing sound sources from sound update thread
            _Viewer3D.SoundProcess.RemoveSoundSource(this);
            SoundSources.Clear();
        }


        /// <summary>
        /// Load the various car sounds
        /// </summary>
        /// <param name="wagonFolderSlash"></param>
        private void LoadCarSounds(string wagonFolderSlash)
        {
            if( MSTSWagon.MainSoundFileName != null ) LoadCarSound(wagonFolderSlash, MSTSWagon.MainSoundFileName );
            if (MSTSWagon.InteriorSoundFileName != null) LoadCarSound(wagonFolderSlash, MSTSWagon.InteriorSoundFileName);
        }


        /// <summary>
        /// Load the car sound, attach it to the car
        /// check first in the wagon folder, then the global folder for the sound.
        /// If not found, report a warning.
        /// </summary>
        /// <param name="wagonFolderSlash"></param>
        /// <param name="filename"></param>
        protected void LoadCarSound(string wagonFolderSlash, string filename)
        {
            if (filename == null)
                return;
            string smsFilePath = wagonFolderSlash + @"sound\" + filename;
            if (!File.Exists(smsFilePath))
                smsFilePath = Viewer.Simulator.BasePath + @"\sound\" + filename;
            if (!File.Exists(smsFilePath))
            {
                Trace.TraceWarning("Cannot find {1} car sound file {0}", filename, wagonFolderSlash);
                return;
            }

            try
            {
                SoundSources.Add(new SoundSource(Viewer, MSTSWagon, smsFilePath));
            }
            catch (Exception error)
            {
                Trace.WriteLine(new FileLoadException(smsFilePath, error));
            }
        }

        /// <summary>
        /// Load the inside and outside sounds for the default level 0 track type.
        /// </summary>
        private void LoadTrackSounds()
        {
            if (Viewer.TTypeDatFile.Count > 0)  // TODO, still have to figure out if this should be part of the car, or train, or track
            {
                if (!string.IsNullOrEmpty(MSTSWagon.InteriorSoundFileName))
                    LoadTrackSound(Viewer.TTypeDatFile[0].InsideSound);

                LoadTrackSound(Viewer.TTypeDatFile[0].OutsideSound);
            }
        }

        /// <summary>
        /// Load the sound source, attach it to the car.
        /// Check first in route\SOUND folder, then in base\SOUND folder.
        /// </summary>
        /// <param name="filename"></param>
        private void LoadTrackSound(string filename)
        {
            if (filename == null)
                return;
            string path = Viewer.Simulator.RoutePath + @"\SOUND\" + filename;
            if (!File.Exists(path))
                path = Viewer.Simulator.BasePath + @"\SOUND\" + filename;
            if (!File.Exists(path))
            {
                Trace.TraceWarning("Cannot find track sound file {0}", filename);
                return;
            }
            SoundSources.Add(new SoundSource(Viewer, MSTSWagon, path));
        }

        internal override void Mark()
        {
            TrainCarShape.Mark();
            if (FreightShape != null)
                FreightShape.Mark();
            if (InteriorShape != null)
                InteriorShape.Mark();
        }

    } // class carshape


    /// <summary>
    /// Utility class to avoid loading the wag file multiple times
    /// </summary>
    public class CarManager
    {
        public static Dictionary<string, MSTSWagon> LoadedCars = new Dictionary<string, MSTSWagon>();
    }


} // namespace ORTS
