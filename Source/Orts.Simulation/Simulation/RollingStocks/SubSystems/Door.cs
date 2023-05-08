// COPYRIGHT 2022 by the Open Rails project.
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
using System.IO;
using Orts.Common;
using Orts.Parsers.Msts;
using ORTS.Common;
using ORTS.Scripting.Api;

namespace Orts.Simulation.RollingStocks.SubSystems
{
    // Door state: do not change the order of this enum
    public enum DoorState
    {
        [GetParticularString("Door", "Closed")] Closed,
        [GetParticularString("Door", "Closing")] Closing,
        [GetParticularString("Door", "Opening")] Opening,
        [GetParticularString("Door", "Open")] Open,
    }
    public enum DoorSide
    {
        Left,
        Right,
        Both
    }
    public class Doors : ISubSystem<Doors>
    {
        public Door RightDoor;
        public Door LeftDoor;

        public Doors(MSTSWagon wagon)
        {
            LeftDoor = new Door(wagon, DoorSide.Left);
            RightDoor = new Door(wagon, DoorSide.Right);
        }

        public virtual void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "wagon(ortsdoors(closingdelay": 
                {
                    float delayS = stf.ReadFloatBlock(STFReader.UNITS.Time, 0f);
                    LeftDoor.ClosingDelayS = delayS;
                    RightDoor.ClosingDelayS = delayS;
                    break;
                }
                case "wagon(ortsdoors(openingdelay": 
                {
                    float delayS = stf.ReadFloatBlock(STFReader.UNITS.Time, 0f);
                    LeftDoor.OpeningDelayS = delayS;
                    RightDoor.OpeningDelayS = delayS;
                    break;
                }
            }
        }

        public void Copy(Doors other)
        {
            LeftDoor.Copy(other.LeftDoor);
            RightDoor.Copy(other.RightDoor);
        }

        public virtual void Initialize()
        {
            LeftDoor.Initialize();
            RightDoor.Initialize();
        }

        /// <summary>
        /// Initialization when simulation starts with moving train
        /// </summary>
        public virtual void InitializeMoving()
        {
        }

        public virtual void Save(BinaryWriter outf)
        {
            LeftDoor.Save(outf);
            RightDoor.Save(outf);
        }

        public virtual void Restore(BinaryReader inf)
        {
            LeftDoor.Restore(inf);
            RightDoor.Restore(inf);
        }

        public virtual void Update(float elapsedClockSeconds)
        {
            LeftDoor.Update(elapsedClockSeconds);
            RightDoor.Update(elapsedClockSeconds);
        }

        public static DoorSide FlippedDoorSide(DoorSide trainSide)
        {
            switch(trainSide)
            {
                case DoorSide.Left:
                    return DoorSide.Right;
                case DoorSide.Right:
                    return DoorSide.Left;
                default:
                    return DoorSide.Both;
            }
        }
    }
    public class Door : ISubSystem<Door>
    {
        
        // Parameters
        public float OpeningDelayS { get; set; } = 0f;
        public float ClosingDelayS { get; set; } = 0f;

        // Variables
        readonly MSTSWagon Wagon;
        protected Simulator Simulator => Wagon.Simulator;
        public readonly DoorSide Side;
        protected Timer OpeningTimer;
        protected Timer ClosingTimer;
        
        public DoorState State { get; protected set; } = DoorState.Closed;
        public bool Locked {get; protected set; }

        public Door(MSTSWagon wagon, DoorSide side)
        {
            Wagon = wagon;
            Side = side;

            OpeningTimer = new Timer(wagon);
            ClosingTimer = new Timer(wagon);
        }

        public virtual void Parse(string lowercasetoken, STFReader stf)
        {
        }

        public void Copy(Door other)
        {
            ClosingDelayS = other.ClosingDelayS;
            OpeningDelayS = other.OpeningDelayS;
        }

        public virtual void Initialize()
        {
            ClosingTimer.Setup(ClosingDelayS);
            OpeningTimer.Setup(OpeningDelayS);
        }

        /// <summary>
        /// Initialization when simulation starts with moving train
        /// </summary>
        public virtual void InitializeMoving()
        {
        }

        public virtual void Save(BinaryWriter outf)
        {
            outf.Write((int)State);
            outf.Write(Locked);
        }

        public virtual void Restore(BinaryReader inf)
        {
            State = (DoorState)inf.ReadInt32();
            Locked = inf.ReadBoolean();
        }

        public virtual void Update(float elapsedClockSeconds)
        {
            switch(State)
            {
                case DoorState.Opening:
                    if (!OpeningTimer.Started) OpeningTimer.Start();
                    if (OpeningTimer.Triggered)
                    {
                        State = DoorState.Open;
                        OpeningTimer.Stop();
                    }
                    break;
                case DoorState.Closing:
                    if (!ClosingTimer.Started) ClosingTimer.Start();
                    if (ClosingTimer.Triggered)
                    {
                        State = DoorState.Closed;
                        ClosingTimer.Stop();
                    }
                    break;
            }
        }
        public void SetDoorLock(bool lck)
        {
            Locked = lck;
            if (lck) SetDoor(false);
        }
        public void SetDoor(bool open)
        {
            switch (State)
            {
                case DoorState.Closed:
                case DoorState.Closing:
                    if (!Locked && open)
                    {
                        ClosingTimer.Stop();
                        State = DoorState.Opening;
                        Wagon.SignalEvent(Event.DoorOpen);
                        bool driverRightSide = (Side == DoorSide.Right) ^ Wagon.GetCabFlipped();
                        Confirm(driverRightSide ? CabControl.DoorsRight : CabControl.DoorsLeft, CabSetting.On);
                    }
                    break;
                case DoorState.Open:
                case DoorState.Opening:
                    if (!open)
                    {
                        OpeningTimer.Stop();
                        State = DoorState.Closing;
                        Wagon.SignalEvent(Event.DoorClose);
                        bool driverRightSide = (Side == DoorSide.Right) ^ Wagon.GetCabFlipped();
                        Confirm(driverRightSide ? CabControl.DoorsRight : CabControl.DoorsLeft, CabSetting.Off);
                    }
                    break;
            }
        }
        protected void Confirm(CabControl control, CabSetting setting)
        {
            if (Wagon == Simulator.PlayerLocomotive)
            {
                Simulator.Confirmer?.Confirm(control, setting);
            }
        }
    }
}
