/* ELECTRIC LOCOMOTIVE CLASSES
 * 
 * The locomotive is represented by two classes:
 *  ...Simulator - defines the behaviour, ie physics, motion, power generated etc
 *  ...Viewer - defines the appearance in a 3D viewer
 * 
 * The ElectricLocomotive classes add to the basic behaviour provided by:
 *  LocomotiveSimulator - provides for movement, throttle controls, direction controls etc
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
using ORTS.Popups;  // needed for Confirmation

namespace ORTS
{
    ///////////////////////////////////////////////////
    ///   SIMULATION BEHAVIOUR
    ///////////////////////////////////////////////////


    /// <summary>
    /// Adds pantograph control to the basic LocomotiveSimulator functionality
    /// </summary>
    public class MSTSElectricLocomotive: MSTSLocomotive
    {

        public bool PantographFirstUp = false;
        public bool PantographSecondUp = false;
        public float PantographFirstDelay = 0.0f;
        public float PantographSecondDelay = 0.0f;
        

        public IIRFilter VoltageFilter;
        public float VoltageV = 0.0f;

		public MSTSElectricLocomotive(Simulator simulator, string wagFile, TrainCar previousCar)
			: base(simulator, wagFile, previousCar)
        {
            VoltageFilter = new IIRFilter(IIRFilter.FilterTypes.Butterworth, 1, IIRFilter.HzToRad(0.7f), 0.001f);
        }

        /// <summary>
        /// Parse the wag file parameters required for the simulator and viewer classes
        /// </summary>
        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                // for example
                //case "engine(sound": CabSoundFileName = stf.ReadStringBlock(); break;
                //case "engine(cabview": CVFFileName = stf.ReadStringBlock(); break;
                
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
            // for example
            //CabSoundFileName = locoCopy.CabSoundFileName;
            //CVFFileName = locoCopy.CVFFileName;
            MSTSElectricLocomotive locoCopy = (MSTSElectricLocomotive) copy;
            PantographFirstUp = locoCopy.PantographFirstUp;
            PantographSecondUp = locoCopy.PantographSecondUp;
            PantographFirstDelay = locoCopy.PantographFirstDelay;
            PantographSecondDelay = locoCopy.PantographSecondDelay;
            

            VoltageFilter = locoCopy.VoltageFilter;
            VoltageV = locoCopy.VoltageV;

            base.InitializeFromCopy(copy);  // each derived level initializes its own variables
        }

        /// <summary>
        /// We are saving the game.  Save anything that we'll need to restore the 
        /// status later.
        /// </summary>
        public override void Save(BinaryWriter outf)
        {
            outf.Write(PantographFirstUp);
            outf.Write(PantographSecondUp);
            outf.Write(PowerOn);

            outf.Write(Pan);
            outf.Write(FrontPanUp);
            outf.Write(AftPanUp);
            outf.Write(NumPantograph);

            base.Save(outf);
        }

        /// <summary>
        /// We are restoring a saved game.  The TrainCar class has already
        /// been initialized.   Restore the game state.
        /// </summary>
        public override void Restore(BinaryReader inf)
        {
            PantographFirstUp = inf.ReadBoolean();
            PantographSecondUp = inf.ReadBoolean();
            PowerOn = inf.ReadBoolean();

            Pan = inf.ReadBoolean();
            FrontPanUp = inf.ReadBoolean();
            AftPanUp = inf.ReadBoolean();
            NumPantograph = (int)inf.ReadSingle();

            //if (inf.ReadBoolean()) SignalEvent(EventID.PantographUp);
            base.Restore(inf);
        }

        /// <summary>
        /// Create a viewer for this locomotive.   Viewers are only attached
        /// while the locomotive is in viewing range.
        /// </summary>
        public override TrainCarViewer GetViewer(Viewer3D viewer)
        {
            return new MSTSElectricLocomotiveViewer(viewer, this);
        }

        /// <summary>
        /// This is a periodic update to calculate physics 
        /// parameters and update the base class's MotiveForceN 
        /// and FrictionForceN values based on throttle settings
        /// etc for the locomotive.
        /// </summary>

        public override void Update(float elapsedClockSeconds)
        {

            if (!(PantographFirstUp || PantographSecondUp))
            {
                PowerOn = false;
                if ((PantographFirstDelay -= elapsedClockSeconds) < 0.0f) PantographFirstDelay = 0.0f;
                if ((PantographSecondDelay -= elapsedClockSeconds) < 0.0f) PantographSecondDelay = 0.0f;
            }
            else
            {
                if (PantographFirstUp)
                {
                    if ((PantographFirstDelay -= elapsedClockSeconds) < 0.0f)
                    {
                        PowerOn = true;
                        PantographFirstDelay = 0.0f;
                    }
                }
                else
                    if ((PantographFirstDelay -= elapsedClockSeconds) < 0.0f) PantographFirstDelay = 0.0f;

                if (PantographSecondUp)
                {
                    if ((PantographSecondDelay -= elapsedClockSeconds) < 0.0f)
                    {
                        PowerOn = true;
                        PantographSecondDelay = 0.0f;
                    }
                }
                else
                    if ((PantographSecondDelay -= elapsedClockSeconds) < 0.0f) PantographSecondDelay = 0.0f;
            }
 
            if (PowerOn)
                VoltageV = VoltageFilter.Filter((float)Program.Simulator.TRK.Tr_RouteFile.MaxLineVoltage, elapsedClockSeconds);
            else
                VoltageV = VoltageFilter.Filter(0.0f, elapsedClockSeconds);

            base.Update(elapsedClockSeconds);
            Variable2 = Variable1;
        }

        /// <summary>
        /// Used when someone want to notify us of an event
        /// </summary>
        public override void SignalEvent( EventID eventID)
        {
            do  // Like 'switch' (i.e. using 'break' is more efficient than a sequence of 'if's) but doesn't need constant EventID.<values>
            {
                // for example
                // case EventID.BellOn: Bell = true; break;
                // case EventID.BellOff: Bell = false; break;
                if (eventID == EventID.Pantograph1Down) { SetPantographFirst(false); break; }
                if (eventID == EventID.Pantograph2Down) { SetPantographSecond(false); break; }
                if (eventID == EventID.Pantograph1Up) { SetPantographFirst(true); break; }
                if (eventID == EventID.Pantograph2Up) { SetPantographSecond(true); break; }
            } while( false );  // Never repeats

            base.SignalEvent( eventID );
        }

        public void SetPantographFirst( bool up)
        {
            if (PantographFirstUp != up)
                PantographFirstDelay += PowerOnDelay;
            PantographFirstUp = up;
        }

        public void SetPantographSecond( bool up)
        {
            if (PantographSecondUp != up)
                PantographSecondDelay += PowerOnDelay;
            PantographSecondUp = up;
        }

        public override float GetDataOf(CabViewControl cvc)
        {
            float data;

            switch (cvc.ControlType)
            {
                case CABViewControlTypes.LINE_VOLTAGE:
                    {
                        if (Pan)
                        {
                            //data = (float)Program.Simulator.TRK.Tr_RouteFile.MaxLineVoltage;
                            data = VoltageV;
                            if (cvc.Units == CABViewControlUnits.KILOVOLTS)
                                data /= 1000;
                        }
                        else
                            data = 0;
                        break;
                    }
                case CABViewControlTypes.PANTOGRAPH:
                case CABViewControlTypes.PANTO_DISPLAY:
                    {
                        data = Pan ? 1 : 0;
                        break;
                    }
                default:
                    {
                        data = base.GetDataOf(cvc);
                        break;
                    }
            }

            return data;
        }

        public override string GetStatus()
        {
            var result = new StringBuilder();
            result.AppendFormat("Pantographs = {0}{1}\n", PantographFirstUp ? "1st up " : "", PantographSecondUp ? "2nd up " : "");
            if ((PantographFirstDelay > 0.0f) || (PantographSecondDelay > 0.0f))
                result.AppendFormat("Electric power = {0}", PowerOn ? "Switching in progress" : "Switching in progress");
            else
                result.AppendFormat("Electric power = {0}", PowerOn ? "On" : "Off");
            return result.ToString();
        }


    } // class ElectricLocomotive

    ///////////////////////////////////////////////////
    ///   3D VIEW
    ///////////////////////////////////////////////////


    /// <summary>
    /// Adds pantograph animation to the basic LocomotiveViewer class
    /// </summary>
    public class MSTSElectricLocomotiveViewer : MSTSLocomotiveViewer
    {

        MSTSElectricLocomotive ElectricLocomotive;

        public MSTSElectricLocomotiveViewer(Viewer3D viewer, MSTSElectricLocomotive car)
            : base(viewer, car)
        {
            ElectricLocomotive = car;
        }

        /// <summary>
        /// A keyboard or mouse click has occured. Read the UserInput
        /// structure to determine what was pressed.
        /// </summary>
        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            bool newState = false;
            if (UserInput.IsPressed(UserCommands.ControlPantographFirst))
            {
                // Raise or lower the first pantograph on all locomotives in the train
                newState = !ElectricLocomotive.PantographFirstUp;
                bool confirmAction = false;
                foreach( TrainCar traincar in ElectricLocomotive.Train.Cars )
                {
                    if( traincar.GetType() == typeof( MSTSElectricLocomotive ) ) {
                        ((MSTSElectricLocomotive)traincar).SetPantographFirst( newState );
                        confirmAction = true;
                    }
                }
                if( confirmAction ) {
                    if( ElectricLocomotive.NumPantograph > 0 ) {
                        this.Viewer.Simulator.Confirmer.Confirm( CabControl.Pantograph1, newState == true ? CabSetting.On : CabSetting.Off );
                    } else {
                        this.Viewer.Simulator.Confirmer.Confirm( CabControl.Power, newState == true ? CabSetting.On : CabSetting.Off );
                    }
                }
            }
            if (UserInput.IsPressed(UserCommands.ControlPantographSecond))
            {
                // Raise or lower the second pantograph on all locomotives in the train
                newState = !ElectricLocomotive.PantographSecondUp;
                bool confirmAction = false;
                foreach( TrainCar traincar in ElectricLocomotive.Train.Cars )
                {
                    if (traincar.GetType() == typeof(MSTSElectricLocomotive))
                        ((MSTSElectricLocomotive)traincar).SetPantographSecond(newState);
                }
                if( confirmAction ) { this.Viewer.Simulator.Confirmer.Confirm( CabControl.Pantograph2, newState == true ? CabSetting.On : CabSetting.Off ); }
            }

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


    } // class ElectricLocomotiveViewer

}
