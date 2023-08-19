// COPYRIGHT 2010, 2011, 2012, 2013 by the Open Rails project.
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
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using Orts.Simulation.RollingStocks;
using ORTS.Common;
using ORTS.Common.Input;
using RailDriver;

namespace Orts.Viewer3D
{
    /// <summary>
    /// Class to get data from RailDriver and translate it into something useful for UserInput
    /// </summary>
    public class UserInputRailDriver : IDataHandler, IErrorHandler
    {
        PIEDevice Device;                   // Our RailDriver
        byte[] WriteBuffer;                 // Buffer for sending data to RailDriver
        bool Active;                        // True when RailDriver values are used to control player loco
        RailDriverState State;              // Interpreted data from RailDriver passed to UserInput

        // calibration values, defaults for the developer's RailDriver
        float FullReversed = 225;
        float Neutral = 116;
        float FullForward = 60;
        float FullThrottle = 229;
        float ThrottleIdle = 176;
        float DynamicBrake = 42;
        float DynamicBrakeSetup = 119;
        float AutoBrakeRelease = 216;
        float FullAutoBrake = 79;
        float EmergencyBrake = 58;
        float IndependentBrakeRelease = 213;
        float BailOffEngagedRelease = 179;
        float IndependentBrakeFull = 30;
        float BailOffEngagedFull = 209;
        float BailOffDisengagedRelease = 109;
        float BailOffDisengagedFull = 121;
        float Rotary1Position1 = 73;
        float Rotary1Position2 = 135;
        float Rotary1Position3 = 180;
        float Rotary2Position1 = 86;
        float Rotary2Position2 = 145;
        float Rotary2Position3 = 189;

        /// <summary>
        /// Tries to find a RailDriver and initialize it
        /// </summary>
        /// <param name="basePath"></param>
        public UserInputRailDriver(string basePath)
        {
            try
            {
                PIEDevice[] devices = PIEDevice.EnumeratePIE();
                for (int i = 0; i < devices.Length; i++)
                {
                    if (devices[i].HidUsagePage == 0xc && devices[i].Pid == 210)
                    {
                        Device = devices[i];
                        Device.SetupInterface();
                        Device.SetErrorCallback(this);
                        Device.SetDataCallback(this);
                        WriteBuffer = new byte[Device.WriteLength];
                        State = new RailDriverState();
                        SetLEDs(0x40, 0x40, 0x40);
                        ReadCalibrationData(basePath);
                        break;
                    }
                }
            }
            catch (Exception error)
            {
                Device = null;
                Trace.WriteLine(error);
            }
        }

        /// <summary>
        /// Data callback, called when RailDriver data is available
        /// </summary>
        /// <param name="data"></param>
        /// <param name="sourceDevice"></param>
        public void HandleHidData(byte[] data, PIEDevice sourceDevice, int error)
        {
            if (sourceDevice != Device)
                return;
            State.SaveButtonData();

            State.Direction.Value = Percentage(data[1], FullReversed, Neutral, FullForward) / 100;;
            State.Throttle.Value = Percentage(data[2], ThrottleIdle, FullThrottle) / 100;
            State.DynamicBrake.Value = Percentage(data[2], ThrottleIdle, DynamicBrakeSetup, DynamicBrake) / 100;;
            State.TrainBrake.Value = Percentage(data[3], AutoBrakeRelease, FullAutoBrake) / 100;;
            State.EngineBrake.Value = Percentage(data[4], IndependentBrakeRelease, IndependentBrakeFull) / 100;;
            
            float a = State.EngineBrake.Value;
            float calOff = (1 - a) * BailOffDisengagedRelease + a * BailOffDisengagedFull;
            float calOn = (1 - a) * BailOffEngagedRelease + a * BailOffEngagedFull;
            State.Commands[UserCommand.ControlBailOff].IsDown = Percentage(data[5], calOff, calOn) > 50;
            State.Commands[UserCommand.ControlWiper].IsDown = (int)(.01 * Percentage(data[6], Rotary1Position1, Rotary1Position2, Rotary1Position3) + 2.5) != 1;
            State.Lights.Value = (int)(.01 * Percentage(data[7], Rotary2Position1, Rotary2Position2, Rotary2Position3) + 2.5);
            State.AddButtonData(data);

            if (State.Activation.IsPressed)
            {
                State.Activation.Changed = false;
                Active = !Active;
                EnableSpeaker(Active);
                if (Active)
                {
                    SetLEDs(0x80, 0x80, 0x80);
                    LEDSpeed = -1;
                    UserInput.RDState = State;
                }
                else
                {
                    SetLEDs(0x40, 0x40, 0x40);
                    UserInput.RDState = null;
                }
            }
            State.Activation.Changed = false;
        }
        
        /// <summary>
        /// Error callback
        /// </summary>
        /// <param name="error"></param>
        /// <param name="sourceDevice"></param>
        public void HandleHidError(PIEDevice sourceDevice, int error)
        {
            Trace.TraceWarning("RailDriver Error: {0}", error);
        }

        static float Percentage(float x, float x0, float x100)
        {
            float p= 100 * (x - x0) / (x100 - x0);
            if (p < 5)
                return 0;
            if (p > 95)
                return 100;
            return p;
        }
        
        static float Percentage(float x, float xminus100, float x0, float xplus100)
        {
            float p = 100 * (x - x0) / (xplus100 - x0);
            if (p < 0)
                p = 100 * (x - x0) / (x0 - xminus100);
            if (p < -95)
                return -100;
            if (p > 95)
                return 100;
            return p;
        }

        /// <summary>
        /// Set the RailDriver LEDs to the specified values
        /// led1 is the right most
        /// </summary>
        /// <param name="led1"></param>
        /// <param name="led2"></param>
        /// <param name="led3"></param>
        void SetLEDs(byte led1, byte led2, byte led3)
        {
            if (Device == null)
                return;
            for (int i = 0; i < WriteBuffer.Length; i++)
                WriteBuffer[i] = 0;
            WriteBuffer[1] = 134;
            WriteBuffer[2] = led1;
            WriteBuffer[3] = led2;
            WriteBuffer[4] = led3;
            Device.WriteData(WriteBuffer);
        }

        /// <summary>
        /// Turns raildriver speaker on or off
        /// </summary>
        /// <param name="on"></param>
        void EnableSpeaker(bool on)
        {
            if (Device == null)
                return;
            for (int i = 0; i < WriteBuffer.Length; i++)
                WriteBuffer[i] = 0;
            WriteBuffer[1] = 133;
            WriteBuffer[7] = (byte) (on ? 1 : 0);
            Device.WriteData(WriteBuffer);
        }

        // LED values for digits 0 to 9
        byte[] LEDDigits = { 0x3f, 0x06, 0x5b, 0x4f, 0x66, 0x6d, 0x7d, 0x07, 0x7f, 0x6f };
        // LED values for digits 0 to 9 with decimal point
        byte[] LEDDigitsPoint = { 0xbf, 0x86, 0xdb, 0xcf, 0xe6, 0xed, 0xfd, 0x87, 0xff, 0xef };
        int LEDSpeed = -1;      // speed in tenths displayed on RailDriver LED

        /// <summary>
        /// Updates speed display on RailDriver LED
        /// </summary>
        /// <param name="playerLoco"></param>
        public void Update(TrainCar playerLoco)
        {
            if (!Active || playerLoco == null || Device == null)
                return;
            float speed = 10 * MpS.FromMpS(playerLoco.SpeedMpS, playerLoco.IsMetric);
            int s = (int) (speed >= 0 ? speed + .5 : -speed + .5);
            if (s != LEDSpeed)
            {
                if (s < 100)
                    SetLEDs(LEDDigits[s % 10], LEDDigitsPoint[s / 10], 0);
                else if (s < 1000)
                    SetLEDs(LEDDigits[s % 10], LEDDigitsPoint[(s / 10) % 10], LEDDigits[(s / 100) % 10]);
                else if (s < 10000)
                    SetLEDs(LEDDigitsPoint[(s / 10) % 10], LEDDigits[(s / 100) % 10], LEDDigits[(s / 1000) % 10]);
                LEDSpeed = s;
            }
        }

        /// <summary>
        /// Reads RailDriver calibration data from a ModernCalibration.rdm file
        /// This file is not in the usual STF format, but the STFReader can handle it okay.
        /// </summary>
        /// <param name="basePath"></param>
        void ReadCalibrationData(string basePath)
        {
            string file = basePath + "\\ModernCalibration.rdm";
            if (!File.Exists(file))
            {
                RegistryKey RK = Registry.LocalMachine.OpenSubKey("SOFTWARE\\PI Engineering\\PIBUS");
                if (RK != null)
                {
                    string dir = (string)RK.GetValue("RailDriver", null, RegistryValueOptions.None);
                    if (dir != null)
                        file = dir + "\\..\\controller\\ModernCalibration.rdm";
                }

                if (!File.Exists(file))
                {
                    SetLEDs(0, 0, 0);
                    Trace.TraceWarning("Cannot find RailDriver calibration file {0}", file);
                    return;
                }
            }
			// TODO: This is... kinda weird and cool at the same time. STF parsing being used on RailDriver's calebration file. Probably should be a dedicated parser, though.
            STFReader reader = new STFReader(file, false);
            while (!reader.Eof)
            {
                string token = reader.ReadItem();
                if (token == "Position")
                {
                    string name = reader.ReadItem();
                    int min= -1;
                    int max= -1;
                    while (token != "}")
                    {
                        token = reader.ReadItem();
                        if (token == "Min")
                            min = reader.ReadInt(-1);
                        else if (token == "Max")
                            max = reader.ReadInt(-1);
                    }
                    if (min >= 0 && max >= 0)
                    {
                        float v = .5f * (min + max);
                        switch (name)
                        {
                            case "Full Reversed": FullReversed = v; break;
                            case "Neutral": Neutral = v; break;
                            case "Full Forward": FullForward = v; break;
                            case "Full Throttle": FullThrottle = v; break;
                            case "Throttle Idle": ThrottleIdle = v; break;
                            case "Dynamic Brake": DynamicBrake = v; break;
                            case "Dynamic Brake Setup": DynamicBrakeSetup = v; break;
                            case "Auto Brake Released": AutoBrakeRelease = v; break;
                            case "Full Auto Brake (CS)": FullAutoBrake = v; break;
                            case "Emergency Brake (EMG)": EmergencyBrake = v; break;
                            case "Independent Brake Released": IndependentBrakeRelease = v; break;
                            case "Bail Off Engaged (in Released position)": BailOffEngagedRelease = v; break;
                            case "Independent Brake Full": IndependentBrakeFull = v; break;
                            case "Bail Off Engaged (in Full position)": BailOffEngagedFull = v; break;
                            case "Bail Off Disengaged (in Released position)": BailOffDisengagedRelease = v; break;
                            case "Bail Off Disengaged (in Full position)": BailOffDisengagedFull = v; break;
                            case "Rotary Switch 1-Position 1(OFF)": Rotary1Position1 = v; break;
                            case "Rotary Switch 1-Position 2(SLOW)": Rotary1Position2 = v; break;
                            case "Rotary Switch 1-Position 3(FULL)": Rotary1Position3 = v; break;
                            case "Rotary Switch 2-Position 1(OFF)": Rotary2Position1 = v; break;
                            case "Rotary Switch 2-Position 2(DIM)": Rotary2Position2 = v; break;
                            case "Rotary Switch 2-Position 3(FULL)": Rotary2Position3 = v; break;
                            default: STFException.TraceInformation(reader, "Skipped unknown calibration value " + name); break;
                        }
                    }
                }
            }
        }

        public void Shutdown()
        {
            if (Device == null)
                return;
            SetLEDs(0, 0, 0);
        }
    }

    /// <summary>
    /// Processed RailDriver data sent to UserInput class
    /// </summary>
    public class RailDriverState : ExternalDeviceState
    {
        byte[] ButtonData;
        public ExternalDeviceCabControl Direction = new ExternalDeviceCabControl();      // -100 (reverse) to 100 (forward)
        public ExternalDeviceCabControl Throttle = new ExternalDeviceCabControl();       // 0 to 100
        public ExternalDeviceCabControl DynamicBrake = new ExternalDeviceCabControl();   // 0 to 100 if active otherwise less than 0
        public ExternalDeviceCabControl TrainBrake = new ExternalDeviceCabControl();     // 0 (release) to 100 (CS), does not include emergency
        public ExternalDeviceCabControl EngineBrake = new ExternalDeviceCabControl();    // 0 to 100
        public ExternalDeviceCabControl Lights = new ExternalDeviceCabControl();                  // lights rotary, 1 off, 2 dim, 3 full

        public RailDriverButton Activation;

        public RailDriverState()
        {
            ButtonData = new byte[6];

            // top row of blue buttons left to right

            Commands[UserCommand.GamePauseMenu] = new RailDriverButton(0, 0x01);
            Commands[UserCommand.GameSave] = new RailDriverButton(0, 0x02);

            Commands[UserCommand.DisplayTrackMonitorWindow] = new RailDriverButton(0, 0x08);

            Commands[UserCommand.DisplaySwitchWindow] = new RailDriverButton(0, 0x40);
            Commands[UserCommand.DisplayTrainOperationsWindow] = new RailDriverButton(0, 0x80);
            Commands[UserCommand.DisplayNextStationWindow] = new RailDriverButton(1, 0x01);

            Commands[UserCommand.DisplayCompassWindow] = new RailDriverButton(1, 0x08);
            Commands[UserCommand.GameSwitchAhead] = new RailDriverButton(1, 0x10);
            Commands[UserCommand.GameSwitchBehind] = new RailDriverButton(1, 0x20);

            // bottom row of blue buttons left to right

            Commands[UserCommand.CameraToggleShowCab] = new RailDriverButton(1, 0x80);       // Btn 16 Default Legend Hide Cab Panel

            Commands[UserCommand.CameraCab] = new RailDriverButton(2, 0x01);                 // Btn 17 Default Legend Frnt Cab View
            Commands[UserCommand.CameraOutsideFront] = new RailDriverButton(2, 0x02);        // Btn 18 Default Legend Ext View 1
            Commands[UserCommand.CameraOutsideRear] = new RailDriverButton(2, 0x04);         // Btn 19 Default Legend Ext.View 2
            Commands[UserCommand.CameraCarPrevious] = new RailDriverButton(2, 0x08);         // Btn 20 Default Legend FrontCoupler

            Commands[UserCommand.CameraCarNext] = new RailDriverButton(2, 0x10);             // Btn 21 Default Legend Rear Coupler
            Commands[UserCommand.CameraTrackside] = new RailDriverButton(2, 0x20);           // Btn 22 Default Legend Track View      
            Commands[UserCommand.CameraPassenger] = new RailDriverButton(2, 0x40);           // Btn 23 Default Legend Passgr View      
            Commands[UserCommand.CameraBrakeman] = new RailDriverButton(2, 0x80);            // Btn 24 Default Legend Coupler View

            Commands[UserCommand.CameraFree] = new RailDriverButton(3, 0x01);                // Btn 25 Default Legend Yard View
            Commands[UserCommand.GameClearSignalForward] = new RailDriverButton(3, 0x02);    // Btn 26 Default Legend Request Pass
            //Commands[UserCommand. load passengers] = new RailDriverUserCommand(3, 0x04);        // Btn 27 Default Legend Load/Unload
            //Commands[UserCommand. ok] = new RailDriverUserCommand(3, 0x08);                     // Btn 28 Default Legend OK

            // controls to right of blue buttons

            Commands[UserCommand.CameraZoomIn] = new RailDriverButton(3, 0x10);
            Commands[UserCommand.CameraZoomOut] = new RailDriverButton(3, 0x20);
            Commands[UserCommand.CameraPanUp] = new RailDriverButton(3, 0x40);
            Commands[UserCommand.CameraPanRight] = new RailDriverButton(3, 0x80);
            Commands[UserCommand.CameraPanDown] = new RailDriverButton(4, 0x01);
            Commands[UserCommand.CameraPanLeft] = new RailDriverButton(4, 0x02);

            // buttons on top left

            Commands[UserCommand.ControlGearUp] = new RailDriverButton(4, 0x04);
            Commands[UserCommand.ControlGearDown] = new RailDriverButton(4, 0x08);
            Commands[UserCommand.ControlEmergencyPushButton] = new RailDriverButton(4, 0x30);
            Commands[UserCommand.ControlAlerter] = new RailDriverButton(4, 0x40);
            Commands[UserCommand.ControlSander] = new RailDriverButton(4, 0x80);
            Commands[UserCommand.ControlPantograph1] = new RailDriverButton(5, 0x01);
            Commands[UserCommand.ControlBellToggle] = new RailDriverButton(5, 0x02);
            Commands[UserCommand.ControlHorn] = new RailDriverButton(5, 0x0c);//either of two bits

            Commands[UserCommand.ControlBailOff] = new ExternalDeviceButton();
            Commands[UserCommand.ControlWiper] = new ExternalDeviceButton();

            Activation = new RailDriverButton(1, 0x40);

            CabControls[(new CabViewControlType(CABViewControlTypes.DIRECTION), -1)] = Direction;
            CabControls[(new CabViewControlType(CABViewControlTypes.THROTTLE), -1)] = Throttle;
            CabControls[(new CabViewControlType(CABViewControlTypes.TRAIN_BRAKE), -1)] = TrainBrake;
            CabControls[(new CabViewControlType(CABViewControlTypes.ENGINE_BRAKE), -1)] = EngineBrake;
            CabControls[(new CabViewControlType(CABViewControlTypes.DYNAMIC_BRAKE), -1)] = DynamicBrake;
            CabControls[(new CabViewControlType(CABViewControlTypes.FRONT_HLIGHT), -1)] = Lights;
        }

        /// <summary>
        /// Saves the latest button data and prepares to get new data
        /// </summary>
        public void SaveButtonData()
        {
            for (int i = 0; i < ButtonData.Length; i++)
            {
                ButtonData[i] = 0;
            }
        }

        /// <summary>
        /// Ors in new button data
        /// </summary>
        /// <param name="data"></param>
        public void AddButtonData(byte[] data)
        {
            for (int i = 0; i < ButtonData.Length; i++)
                ButtonData[i] |= data[i + 8];
            foreach (var button in Commands.Values)
            {
                if (button is RailDriverButton rd) rd.Update(ButtonData);
            }
            Activation.Update(ButtonData);
        }

        public override string ToString()
        {
            string s= String.Format("{0} {1} {2} {3} {4}", Direction, Throttle, DynamicBrake, TrainBrake, EngineBrake);
            for (int i = 0; i < 6; i++)
                s += " " + ButtonData[i];
            return s;
        }
    }

    public class RailDriverButton : ExternalDeviceButton
    {
        int Index;
        byte Mask;
        public RailDriverButton(int index, byte mask)
        {
            Index = index;
            Mask = mask;
        }
        public void Update(byte[] data)
        {
            IsDown = (data[Index] & Mask) != 0;
        }
    }

}
