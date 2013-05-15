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

        public MSTSElectricLocomotive(Simulator simulator, string wagFile)
            : base(simulator, wagFile)
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
            outf.Write(Pan1Up);
            outf.Write(Pan2Up);

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
            if (PowerOn)
                SignalEvent(Event.EnginePowerOn);
            Pan = inf.ReadBoolean();
            Pan1Up = inf.ReadBoolean();
            Pan2Up = inf.ReadBoolean();
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
                if (PowerOn)
                    SignalEvent(Event.EnginePowerOff);
                PowerOn = false;
                CompressorOn = false;
                if ((PantographFirstDelay -= elapsedClockSeconds) < 0.0f) PantographFirstDelay = 0.0f;
                if ((PantographSecondDelay -= elapsedClockSeconds) < 0.0f) PantographSecondDelay = 0.0f;
            }
            else
            {
                if (PantographFirstUp)
                {
                    if ((PantographFirstDelay -= elapsedClockSeconds) < 0.0f)
                    {
                        if (!PowerOn)
                            SignalEvent(Event.EnginePowerOn);
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
                        if (!PowerOn)
                            SignalEvent(Event.EnginePowerOn);
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
            //Variable2 = Variable1 * 100F ;
            //Variable2 = Math.Abs(MotiveForceN) / MaxForceN * 100F ;

            if ( ThrottlePercent == 0f ) Variable2 = 0;
            else 
            {
                float dV2;
                dV2 = Math.Abs(MotiveForceN) / MaxForceN * 100f - Variable2;
                float max = 2f;
                if (dV2 > max) dV2 = max;
                else if (dV2 < -max) dV2 = -max;
                Variable2 += dV2;
            }
            Variable3 = 0;
            if ( DynamicBrakePercent > 0)
                Variable3 = Math.Abs(MotiveForceN) / MaxDynamicBrakeForceN;
        }

        /// <summary>
        /// Used when someone want to notify us of an event
        /// </summary>
        public override void SignalEvent(Event evt)
        {
            switch (evt)
            {
                case Event.Pantograph1Up: { SetPantographFirst(true); break; }
                case Event.Pantograph1Down: { SetPantographFirst(false); break; }
                case Event.Pantograph2Up: { SetPantographSecond(true); break; }
                case Event.Pantograph2Down: { SetPantographSecond(false); break; }
            }

            base.SignalEvent(evt);
        }

        /// <summary>
        /// Raise or lower the first pantograph on all locomotives in the train
        /// </summary>
        /// <param name="raise"></param>
        public void SetPantographs( int item, bool raise ) {
            foreach( TrainCar traincar in Train.Cars ) {
                if( traincar.GetType() == typeof( MSTSElectricLocomotive ) ) {
                    if( item == 1 ) ((MSTSElectricLocomotive)traincar).SetPantographFirst( raise );
                    if( item == 2 ) ((MSTSElectricLocomotive)traincar).SetPantographSecond( raise );
                }
            }
            if( item == 1 ) this.Simulator.Confirmer.Confirm( CabControl.Pantograph1, raise == true ? CabSetting.On : CabSetting.Off );
            if( item == 2 ) this.Simulator.Confirmer.Confirm( CabControl.Pantograph2, raise == true ? CabSetting.On : CabSetting.Off );
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
            if( UserInput.IsPressed( UserCommands.ControlPantograph1 ) ) {
                new PantographCommand( _Viewer3D.Log, 1, !ElectricLocomotive.PantographFirstUp );
                return; // I.e. Skip the call to base.HandleUserInput()
            }
            if( UserInput.IsPressed( UserCommands.ControlPantograph2 ) ) {
                new PantographCommand( _Viewer3D.Log, 2, !ElectricLocomotive.PantographSecondUp );
                return;
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
