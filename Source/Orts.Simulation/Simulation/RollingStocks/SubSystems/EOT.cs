// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
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


using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using Orts.Simulation.AIs;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using Orts.Simulation.Signalling;
using Orts.Simulation.Timetables;
using ORTS.Common;
using ORTS.Scripting.Api;
using ORTS.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Event = Orts.Common.Event;

namespace Orts.Simulation.RollingStocks.SubSystems
{
    public enum EOTLevel
    {
        NoComm,
        OneWay,
        TwoWay
    }

    public class FullEOTPaths : List<string>
    {
         public FullEOTPaths (string eotPath)
        {
            var directories = Directory.GetDirectories(eotPath);
            foreach (var directory in directories)
            {
                var files = Directory.GetFiles(directory, "*.eot");
                foreach (var file in files)
                {
                    Add(file);
                }
            }
        }
    }
    public class EOT : MSTSWagon
    {
        public enum EOTstate
        {
            Disarmed,
            CommTestOn,
            Armed,
            LocalTestOn,
            ArmNow,
            ArmedTwoWay
        }

        public float CommTestDelayS { get; protected set; } = 5f;
        public float LocalTestDelayS { get; protected set; } = 25f;

        public static Random IDRandom = new Random();
        public int ID;
        public EOTstate EOTState;
        public bool EOTEmergencyBrakingOn = false;
        public EOTLevel EOTLevel;

        protected Timer DelayTimer;

        public EOT(Simulator simulator, string wagPath)
            : base(simulator, wagPath)
        {
            EOTState = EOTstate.Disarmed;
            ID = IDRandom.Next(0, 99999);
            DelayTimer = new Timer(this);
        }

        public override void Initialize(bool reinitialize = false)
        {
            base.Initialize(reinitialize);
        }

        public override void InitializeMoving()
        {
            base.InitializeMoving();
            InitializeLevel();
        }

        public void InitializeLevel()
        {
            switch (EOTLevel)
            {
                case EOTLevel.OneWay:
                    EOTState = EOTstate.Armed;
                    break;
                case EOTLevel.TwoWay:
                    EOTState = EOTstate.ArmedTwoWay;
                    break;
                default:
                    break;
            }
        }

        public override void Update(float elapsedClockSeconds)
        {
            UpdateState();
            if (Train.Simulator.PlayerLocomotive.Train == Train && EOTState == EOTstate.ArmedTwoWay &&
                (EOTEmergencyBrakingOn ||
                (Train.Simulator.PlayerLocomotive as MSTSLocomotive).TrainBrakeController.GetStatus().ToLower().StartsWith("emergency")))
            {
                // Simulate EOT opening brake pipe to atmosphere by instantly opening rear anglecock
                this.BrakeSystem.AngleCockBOpen = true;
                this.BrakeSystem.AngleCockBOpenAmount = 1;
            }
            else
            {
                this.BrakeSystem.AngleCockBOpen = false;
            }
            base.Update(elapsedClockSeconds);
        }

        private void UpdateState()
        {
            switch (EOTState)
            {
                case EOTstate.Disarmed:
                    break;
                case EOTstate.CommTestOn:
                    if (DelayTimer.Triggered)
                    {
                        DelayTimer.Stop();
                        EOTState = EOTstate.Armed;
                    }
                    break;
                case EOTstate.Armed:
                    if (EOTLevel == EOTLevel.TwoWay)
                    {
                        if (DelayTimer == null)
                            DelayTimer = new Timer(this);
                        DelayTimer.Setup(LocalTestDelayS);
                        EOTState = EOTstate.LocalTestOn;
                        DelayTimer.Start();
                    }
                    break;
                case EOTstate.LocalTestOn:
                    if (DelayTimer.Triggered)
                    {
                        DelayTimer.Stop();
                        EOTState = EOTstate.ArmNow;
                    }
                    break;
                case EOTstate.ArmNow:
                    break;
                case EOTstate.ArmedTwoWay:
                    break;
            }
        }

        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "ortseot(level":
                    stf.MustMatch("(");
                    var eotLevel = stf.ReadString();
                    try
                    {
                        EOTLevel = (EOTLevel)Enum.Parse(typeof(EOTLevel), eotLevel.First().ToString().ToUpper() + eotLevel.Substring(1));
                    }
                    catch
                    {
                        STFException.TraceWarning(stf, "Skipped unknown EOT Level " + eotLevel);
                    }
                    break;
                default:
                    base.Parse(lowercasetoken, stf);
                    break;
            }
        }

        public override void Save(BinaryWriter outf)
        {
            outf.Write(ID);
            outf.Write((int)EOTState);
            base.Save(outf);
        }

        public override void Restore(BinaryReader inf)
        {
            ID = inf.ReadInt32();
            EOTState = (EOTstate)(inf.ReadInt32());
            DelayTimer = new Timer(this);
            switch (EOTState)
            {
                case EOTstate.CommTestOn:
                    // restart timer
                    DelayTimer.Setup(CommTestDelayS);
                    DelayTimer.Start();
                    break;
                case EOTstate.LocalTestOn:
                    // restart timer
                    DelayTimer.Setup(LocalTestDelayS);
                    DelayTimer.Start();
                    break;
                default:
                    break;
            }
            base.Restore(inf);
            if (Train != null) Train.EOT = this;
        }

        public override void Copy(MSTSWagon copy)
        {
            base.Copy(copy);
            EOT eotcopy = (EOT)copy;
            EOTLevel = eotcopy.EOTLevel;
        }

        public float GetDataOf(CabViewControl cvc)
        {
            float data = 0;
            switch (cvc.ControlType.Type)
            {
                case CABViewControlTypes.ORTS_EOT_ID:
                    data = ID;
                    break;
                case CABViewControlTypes.ORTS_EOT_STATE_DISPLAY:
                    data = (float)(int)EOTState;
                    break;
                case CABViewControlTypes.ORTS_EOT_EMERGENCY_BRAKE:
                    data = EOTEmergencyBrakingOn ? 1 : 0;
                    break;
            }
            return data;
        }

        public void CommTest()
        {
            if (EOTState == EOTstate.Disarmed &&
                (EOTLevel == EOTLevel.OneWay || EOTLevel == EOTLevel.TwoWay))
            {
                if (DelayTimer == null)
                    DelayTimer = new Timer(this);
                DelayTimer.Setup(CommTestDelayS);
                EOTState = EOTstate.CommTestOn;
                DelayTimer.Start();
            }
        }

        public void Disarm()
        {
            EOTState = EOTstate.Disarmed;
        }

        public void ArmTwoWay()
        {
            if (EOTState == EOTstate.ArmNow)
                EOTState = EOTstate.ArmedTwoWay;
        }

        public void EmergencyBrake (bool toState)
        {
            if (EOTState == EOTstate.ArmedTwoWay)
            {
                EOTEmergencyBrakingOn = toState;
            }
        }

    }
}
