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
using Orts.Viewer3D.Processes;
using ORTS.Common;
using ORTS.Common.Input;
using ORTS.Settings;

namespace Orts.Viewer3D
{
    /// <summary>
    /// Process RailDriver data sent to UserInput class
    /// </summary>
    public class RailDriverState : ExternalDeviceState
    {
        private readonly RailDriverDevice railDriverInstance;
        private static RailDriverSettings settings;

        // calibration values, defaults for the developer's RailDriver
        private readonly (byte, byte, byte) reverser, dynamicBrake, wipers, headlight;
        private readonly (byte, byte) throttle, autoBrake, independentBrake, emergencyBrake, bailoffDisengaged, bailoffEngaged;
        private readonly bool fullRangeThrottle;
        private byte[] readBuffer;
        public ExternalDeviceCabControl Direction = new ExternalDeviceCabControl();      // -100 (reverse) to 100 (forward)
        public ExternalDeviceCabControl Throttle = new ExternalDeviceCabControl();       // 0 to 100
        public ExternalDeviceCabControl DynamicBrake = new ExternalDeviceCabControl();   // 0 to 100 if active otherwise less than 0
        public ExternalDeviceCabControl TrainBrake = new ExternalDeviceCabControl();     // 0 (release) to 100 (CS), does not include emergency
        public ExternalDeviceCabControl EngineBrake = new ExternalDeviceCabControl();    // 0 to 100
        public ExternalDeviceCabControl Wipers = new ExternalDeviceCabControl();                  // wiper rotary, 1 off, 2 slow, 3 full
        public ExternalDeviceCabControl Lights = new ExternalDeviceCabControl();                  // lights rotary, 1 off, 2 dim, 3 full
        ExternalDeviceButton BailOff;
        public RailDriverState(Game game)
        {
            try
            {
                railDriverInstance = RailDriverDevice.Instance;
                if (railDriverInstance.Enabled)
                {
                    settings = game.Settings.RailDriver;
                    byte cutOff = settings.CalibrationSettings[(int)RailDriverCalibrationSetting.CutOffDelta];

                    byte[] calibrationSettings = settings.CalibrationSettings;

                    if (Convert.ToBoolean(calibrationSettings[(int)RailDriverCalibrationSetting.ReverseReverser]))
                        reverser = (calibrationSettings[(byte)RailDriverCalibrationSetting.ReverserFullForward], calibrationSettings[(byte)RailDriverCalibrationSetting.ReverserNeutral], calibrationSettings[(byte)RailDriverCalibrationSetting.ReverserFullReversed]);
                    else
                        reverser = (calibrationSettings[(byte)RailDriverCalibrationSetting.ReverserFullReversed], calibrationSettings[(byte)RailDriverCalibrationSetting.ReverserNeutral], calibrationSettings[(byte)RailDriverCalibrationSetting.ReverserFullForward]);
                    reverser = UpdateCutOff(reverser, cutOff);

                    if (Convert.ToBoolean(calibrationSettings[(int)RailDriverCalibrationSetting.FullRangeThrottle]))
                    {
                        if (Convert.ToBoolean(calibrationSettings[(int)RailDriverCalibrationSetting.ReverseThrottle]))
                            throttle = (calibrationSettings[(byte)RailDriverCalibrationSetting.DynamicBrake], calibrationSettings[(byte)RailDriverCalibrationSetting.ThrottleFull]);
                        else
                            throttle = (calibrationSettings[(byte)RailDriverCalibrationSetting.ThrottleFull], calibrationSettings[(byte)RailDriverCalibrationSetting.DynamicBrake]);
                        fullRangeThrottle = true;
                    }
                    else
                    {
                        if (Convert.ToBoolean(calibrationSettings[(int)RailDriverCalibrationSetting.ReverseThrottle]))
                        {
                            throttle = (calibrationSettings[(byte)RailDriverCalibrationSetting.DynamicBrake], calibrationSettings[(byte)RailDriverCalibrationSetting.DynamicBrakeSetup]);
                            dynamicBrake = (calibrationSettings[(byte)RailDriverCalibrationSetting.DynamicBrakeSetup], calibrationSettings[(byte)RailDriverCalibrationSetting.ThrottleIdle], calibrationSettings[(byte)RailDriverCalibrationSetting.ThrottleFull]);
                        }
                        else
                        {
                            throttle = (calibrationSettings[(byte)RailDriverCalibrationSetting.ThrottleIdle], calibrationSettings[(byte)RailDriverCalibrationSetting.ThrottleFull]);
                            dynamicBrake = (calibrationSettings[(byte)RailDriverCalibrationSetting.ThrottleIdle], calibrationSettings[(byte)RailDriverCalibrationSetting.DynamicBrakeSetup], calibrationSettings[(byte)RailDriverCalibrationSetting.DynamicBrake]);
                        }
                    }
                    throttle = UpdateCutOff(throttle, cutOff);
                    dynamicBrake = UpdateCutOff(dynamicBrake, cutOff);

                    if (Convert.ToBoolean(calibrationSettings[(int)RailDriverCalibrationSetting.ReverseAutoBrake]))
                        autoBrake = (calibrationSettings[(byte)RailDriverCalibrationSetting.AutoBrakeFull], calibrationSettings[(byte)RailDriverCalibrationSetting.AutoBrakeRelease]);
                    else
                        autoBrake = (calibrationSettings[(byte)RailDriverCalibrationSetting.AutoBrakeRelease], calibrationSettings[(byte)RailDriverCalibrationSetting.AutoBrakeFull]);
                    if (Convert.ToBoolean(calibrationSettings[(int)RailDriverCalibrationSetting.ReverseIndependentBrake]))
                        independentBrake = (calibrationSettings[(byte)RailDriverCalibrationSetting.IndependentBrakeFull], calibrationSettings[(byte)RailDriverCalibrationSetting.IndependentBrakeRelease]);
                    else
                        independentBrake = (calibrationSettings[(byte)RailDriverCalibrationSetting.IndependentBrakeRelease], calibrationSettings[(byte)RailDriverCalibrationSetting.IndependentBrakeFull]);
                    autoBrake = UpdateCutOff(autoBrake, cutOff);
                    independentBrake = UpdateCutOff(independentBrake, cutOff);

                    emergencyBrake = (calibrationSettings[(byte)RailDriverCalibrationSetting.AutoBrakeFull], calibrationSettings[(byte)RailDriverCalibrationSetting.EmergencyBrake]);
                    emergencyBrake = UpdateCutOff(emergencyBrake, cutOff);

                    wipers = (calibrationSettings[(byte)RailDriverCalibrationSetting.Rotary1Position1], calibrationSettings[(byte)RailDriverCalibrationSetting.Rotary1Position2], calibrationSettings[(byte)RailDriverCalibrationSetting.Rotary1Position3]);
                    headlight = (calibrationSettings[(byte)RailDriverCalibrationSetting.Rotary2Position1], calibrationSettings[(byte)RailDriverCalibrationSetting.Rotary2Position2], calibrationSettings[(byte)RailDriverCalibrationSetting.Rotary2Position3]);

                    bailoffDisengaged = (calibrationSettings[(byte)RailDriverCalibrationSetting.BailOffDisengagedRelease], calibrationSettings[(byte)RailDriverCalibrationSetting.BailOffDisengagedFull]);
                    bailoffEngaged = (calibrationSettings[(byte)RailDriverCalibrationSetting.BailOffEngagedRelease], calibrationSettings[(byte)RailDriverCalibrationSetting.BailOffEngagedFull]);
                    bailoffDisengaged = UpdateCutOff(bailoffDisengaged, cutOff);
                    bailoffEngaged = UpdateCutOff(bailoffEngaged, cutOff);

                    readBuffer = railDriverInstance.GetReadBuffer();

                    railDriverInstance.SetLeds(RailDriverDisplaySign.Hyphen, RailDriverDisplaySign.Hyphen, RailDriverDisplaySign.Hyphen);

                    for (int i=0; i<settings.UserCommands.Length; i++)
                    {
                        var userCommand = (UserCommand)i;
                        byte button = settings.UserCommands[i];
                        if (button >= 0 && button != byte.MaxValue)
                        {
                            RegisterCommand(userCommand, new RailDriverButton(button));
                            if (userCommand == UserCommand.ControlHorn || userCommand == UserCommand.ControlEmergencyPushButton)
                            {
                                RegisterCommand(userCommand, new RailDriverButton((byte)(button+1)));
                            }
                        }
                    }
                }
            }
            catch (Exception error)
            {
                railDriverInstance = null;
                Trace.WriteLine(error);
            }
            
            BailOff = new ExternalDeviceButton();
            RegisterCommand(UserCommand.ControlBailOff, BailOff);

            CabControls[(new CabViewControlType(CABViewControlTypes.DIRECTION), -1)] = Direction;
            CabControls[(new CabViewControlType(CABViewControlTypes.THROTTLE), -1)] = Throttle;
            CabControls[(new CabViewControlType(CABViewControlTypes.TRAIN_BRAKE), -1)] = TrainBrake;
            CabControls[(new CabViewControlType(CABViewControlTypes.ENGINE_BRAKE), -1)] = EngineBrake;
            CabControls[(new CabViewControlType(CABViewControlTypes.DYNAMIC_BRAKE), -1)] = DynamicBrake;
            CabControls[(new CabViewControlType(CABViewControlTypes.WIPERS), -1)] = Wipers;
            CabControls[(new CabViewControlType(CABViewControlTypes.FRONT_HLIGHT), -1)] = Lights;
        }
        public void Update()
        {
            if (railDriverInstance != null && railDriverInstance.Enabled && 0 == railDriverInstance.ReadCurrentData(ref readBuffer))
            {
                if (Active)
                {
                    Direction.Value = Percentage(readBuffer[1], reverser) / 100;
                    Throttle.Value = Percentage(readBuffer[2], throttle) / 100;
                    if (!fullRangeThrottle)
                        DynamicBrake.Value = Percentage(readBuffer[2], dynamicBrake) / 100;
                    float trainBrake = Percentage(readBuffer[3], autoBrake) / 100;
                    if (trainBrake >= 1 && Percentage(readBuffer[3], emergencyBrake) > 50)
                        TrainBrake.Value = 2;
                    else
                        TrainBrake.Value = trainBrake;
                    EngineBrake.Value = Percentage(readBuffer[4], independentBrake) / 100;
                    float a = EngineBrake.Value;
                    float calOff = (1 - a) * bailoffDisengaged.Item1 + a * bailoffDisengaged.Item2;
                    float calOn = (1 - a) * bailoffEngaged.Item1 + a * bailoffEngaged.Item2;
                    BailOff.IsDown = Percentage(readBuffer[5], calOff, calOn) > 50;
                    Wipers.Value = (int)(.01 * Percentage(readBuffer[6], wipers) + 2.5) == 1 ? 0 : 1;
                    Lights.Value = (int)(.01 * Percentage(readBuffer[7], headlight) + 2.5);
                }
                foreach (var command in Commands.Keys)
                {
                    var buttonList = Commands[command];
                    foreach (var button in buttonList)
                    {
                        if (button is RailDriverButton rd && (Active || command == UserCommand.GameExternalCabController))
                        {
                            rd.Update(readBuffer);
                        }
                    }
                }
            }
        }
        public void Activate()
        {
            if (railDriverInstance.Enabled)
            {
                Active = !Active;
                railDriverInstance.EnableSpeaker(Active);
                if (Active)
                {
                    railDriverInstance.SetLeds(0x39, 0x09, 0x0F);
                }
                else
                {
                    railDriverInstance.SetLeds(RailDriverDisplaySign.Hyphen, RailDriverDisplaySign.Hyphen, RailDriverDisplaySign.Hyphen);
                }
            }
        }

        private static float Percentage(float x, float x0, float x100)
        {
            float p = 100 * (x - x0) / (x100 - x0);
            if (p < 0)
                return 0;
            if (p > 100)
                return 100;
            return p;
        }

        private static float Percentage(byte value, (byte p0, byte p100) range)
        {
            float p = 100 * (value - range.p0) / (range.p100- range.p0);
            if (p < 0)
                return 0;
            if (p > 100)
                return 100;
            return p;
        }

        private static float Percentage(byte value, (byte p100Minus, byte p0, byte p100Plus) range) 
        {
            float p = 100 * (value - range.p0) / (range.p100Plus - range.p0);
            if (p < 0)
                p = 100 * (value - range.p0) / (range.p0 - range.p100Minus);
            if (p < -100)
                return -100;
            if (p > 100)
                return 100;
            return p;
        }

        private (byte, byte) UpdateCutOff((byte, byte) range, byte cutOff)
        {
            if (range.Item1 < range.Item2)
            {
                range.Item1 += cutOff;
                range.Item2 -= cutOff;
            }
            else
            {
                range.Item2 += cutOff;
                range.Item1 -= cutOff;
            }
            return range;
        }

        private (byte, byte, byte) UpdateCutOff((byte, byte, byte) range, byte cutOff)
        {
            if (range.Item1 < range.Item3)
            {
                range.Item1 += cutOff;
                range.Item3 -= cutOff;
            }
            else
            {
                range.Item3 += cutOff;
                range.Item1 -= cutOff;
            }
            return range;
        }

        public bool Active { get; private set; }

        /// <summary>
        /// Updates speed display on RailDriver LED
        /// </summary>
        /// <param name="speed"></param>
        public void ShowSpeed(float speed)
        {
            if (Active)
                railDriverInstance?.SetLedsNumeric(Math.Abs(speed));
        }

        public void Shutdown()
        {
            if (railDriverInstance.Enabled)
            {
                railDriverInstance?.ClearDisplay();
                railDriverInstance?.Shutdown();
            }
        }
    }

    public class RailDriverButton : ExternalDeviceButton
    {
        int Index;
        byte Mask;
        public RailDriverButton(byte button)
        {
            Index = 8 + button / 8;
            Mask = (byte)(1 << (button % 8));
        }
        public void Update(byte[] data)
        {
            IsDown = (data[Index] & Mask) != 0;
        }
    }

}
