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

using Orts.Parsers.Msts;
using ORTS.Common;
using ORTS.Scripting.Api;
using Orts.Simulation.AIs;
using Orts.Simulation.Timetables;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Event = Orts.Common.Event;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{
    public class Pantographs : ISubSystem<Pantographs>
    {
        public static readonly int MinPantoID = 1; // minimum value of PantoID, applies to Pantograph 1
        public static readonly int MaxPantoID = 4; // maximum value of PantoID, applies to Pantograph 4
        readonly MSTSWagon Wagon;

        public List<Pantograph> List = new List<Pantograph>();

        public Pantographs(MSTSWagon wagon)
        {
            Wagon = wagon;
        }

        public void Parse(string lowercasetoken, STFReader stf)
        {
            bool nopantoswap = false;
            switch (lowercasetoken)
            {
                case "wagon(ortspantographs":
                    List.Clear();

                    stf.MustMatch("(");
                    stf.ParseBlock(
                        new[] {
                            new STFReader.TokenProcessor(
                                "nopantoswap", () => {nopantoswap = stf.ReadBoolBlock(true); }
                                ),
                            new STFReader.TokenProcessor(
                                "pantograph",
                                () => {
                                    List.Add(new Pantograph(Wagon));
                                    List.Last().Parse(stf);
                                }
                            )
                        }
                    );

                    if (List.Count() == 0)
                        throw new InvalidDataException("ORTSPantographs block with no pantographs");

                    break;
            }

            // set no panto swap for all pantographs
            foreach (var panto in List)
            {
                panto.NoPantoSwap = false;
                if (panto.PantoDirectionInfo != null && panto.PantoDirectionInfo.Count > 0)
                {
                    panto.NoPantoSwap = true;
                }
                else if (nopantoswap)
                {
                    panto.NoPantoSwap = true;
                }
            }
        }

        public void Copy(Pantographs pantographs)
        {
            List.Clear();

            foreach (Pantograph pantograph in pantographs.List)
            {
                List.Add(new Pantograph(Wagon));
                List.Last().Copy(pantograph);
            }
        }

        public void Restore(BinaryReader inf)
        {
            List.Clear();

            int n = inf.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                List.Add(new Pantograph(Wagon));
                List.Last().Restore(inf);
            }
        }

        public void Initialize()
        {
            while (List.Count() < 2)
            {
                Add(new Pantograph(Wagon));
            }

            foreach (Pantograph pantograph in List)
            {
                pantograph.Initialize();
            }
        }

        public void InitializeMoving()
        {
            foreach (Pantograph pantograph in List)
            {
                if (pantograph != null)
                {
                    pantograph.InitializeMoving();

                    break;
                }
            }
        }

        public void Update(float elapsedClockSeconds)
        {
            foreach (Pantograph pantograph in List)
            {
                pantograph.Update(elapsedClockSeconds);
            }
        }

        public void HandleEvent(PowerSupplyEvent evt)
        {
            foreach (Pantograph pantograph in List)
            {
                pantograph.HandleEvent(evt);
            }
        }

        public void HandleEvent(PowerSupplyEvent evt, int id)
        {
            if (id <= List.Count)
            {
                List[id - 1].HandleEvent(evt);
            }
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write(List.Count());
            foreach (Pantograph pantograph in List)
            {
                pantograph.Save(outf);
            }
        }

        #region ListManipulation

        public void Add(Pantograph pantograph)
        {
            List.Add(pantograph);
        }

        public int Count { get { return List.Count; } }

        public Pantograph this[int i]
        {
            get
            {
                if (i <= 0 || i > List.Count)
                    return null;
                else
                    return List[i - 1];
            }
        }

        #endregion

        public PantographState State
        {
            get
            {
                PantographState state = PantographState.Down;

                foreach (Pantograph pantograph in List)
                {
                    if (pantograph.State > state)
                        state = pantograph.State;
                }

                return state;
            }
        }
    }

    public class Pantograph : ISubSystem<Pantograph>
    {
        readonly MSTSWagon Wagon;
        protected Simulator Simulator => Wagon.Simulator;

        public PantographState State { get; private set; }
        public float DelayS { get; private set; }
        public float TimeS { get; private set; }
        public bool CommandUp
        {
            get
            {
                bool value;

                switch (State)
                {
                    default:
                    case PantographState.Down:
                    case PantographState.Lowering:
                        value = false;
                        break;

                    case PantographState.Up:
                    case PantographState.Raising:
                        value = true;
                        break;
                }

                return value;
            }
        }
        public int Id
        {
            get
            {
                return Wagon.Pantographs.List.IndexOf(this) + 1;
            }
        }

        // directional information
        public enum PantoDirections   // possible directions
        {
            Forward,
            Backward,
            Stopped,
        }

        public struct PantoDirectionDetails // direction details
        {
            public PantoDirections Direction;
            public float MaxSpeed;
        }

        public List<PantoDirectionDetails> PantoDirectionInfo = null; // list holding direction details

        // no swap setting
        public bool NoPantoSwap = false;

        public Pantograph(MSTSWagon wagon)
        {
            Wagon = wagon;

            State = PantographState.Down;
            DelayS = 0f;
            TimeS = 0f;
        }

        public void Parse(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(
                new[] {
                    new STFReader.TokenProcessor(
                        "delay",
                        () => {
                            DelayS = stf.ReadFloatBlock(STFReader.UNITS.Time, null);
                        }
                    ),
                    new STFReader.TokenProcessor(
                        "directions",
                        () =>
                        {
                            PantoDirectionInfo = ReadPantoDirectionInfo(stf, Wagon);
                        })
                }
            );
        }

        // process direction info
        private static List<PantoDirectionDetails> ReadPantoDirectionInfo(STFReader stf, MSTSWagon wagon)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(null);
            List<PantoDirectionDetails> pantoInfo = new List<PantoDirectionDetails>();
            bool validinfo = false;

            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("direction",
                () =>
                {
                    if (pantoInfo.Count > count)
                    {
                        STFException.TraceWarning(stf, "Skipped extra pantograph direction info for " + wagon.RealWagFilePath);
                    }
                    else
                    {
                        PantoDirectionDetails? thisDetail = ReadPantoDirectionDetails (stf);
                        if (thisDetail.HasValue)
                        {
                            pantoInfo.Add(thisDetail.Value);
                            validinfo = true;
                        }
                        else
                        {
                            STFException.TraceWarning(stf, "Invalid pantograph direction information for " + wagon.RealWagFilePath);
                        }
                    }
                })
            });

            if (!validinfo) pantoInfo = null;
            return (pantoInfo);
        }

        private static PantoDirectionDetails? ReadPantoDirectionDetails(STFReader stf)
        {
            stf.MustMatch("(");
            PantoDirectionDetails pantoDetails = new PantoDirectionDetails();
            bool validInfo = true;

            string directionstring = stf.ReadString();
            try
            {
                pantoDetails.Direction = (PantoDirections)Enum.Parse(typeof(PantoDirections), directionstring, true);
            }
            catch (ArgumentException)
            {
                STFException.TraceInformation(stf, "Skipped unknown pantograph direction " + directionstring);
                validInfo = false;
            }

            if (validInfo)
            {
                stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("speedmph", ()=>{ pantoDetails.MaxSpeed = MpS.FromMpH(stf.ReadFloatBlock(STFReader.UNITS.None, 0)); }),
                new STFReader.TokenProcessor("speedkph", ()=>{ pantoDetails.MaxSpeed = MpS.FromKpH(stf.ReadFloatBlock(STFReader.UNITS.None, 0)); }),
            });
            }

            if (validInfo)
            {
                return (pantoDetails);
            }
            else
            {
                return (null);
            }
        }

        public void Copy(Pantograph pantograph)
        {
            State = pantograph.State;
            DelayS = pantograph.DelayS;
            TimeS = pantograph.TimeS;

            if (pantograph.PantoDirectionInfo != null)
            {
                PantoDirectionInfo = new List<PantoDirectionDetails>();
                foreach (var thisInfo in pantograph.PantoDirectionInfo)
                {
                    PantoDirectionInfo.Add(thisInfo);
                }
            }

            NoPantoSwap = pantograph.NoPantoSwap;
        }

        public void Restore(BinaryReader inf)
        {
            State = (PantographState)Enum.Parse(typeof(PantographState), inf.ReadString());
            DelayS = inf.ReadSingle();
            TimeS = inf.ReadSingle();

            int i = inf.ReadInt32();
            if (i >= 0)
            {
                PantoDirectionInfo = new List<PantoDirectionDetails>();
                for (int ii = 0; ii < i; ii++)
                {
                    PantoDirectionDetails thisInfo = new PantoDirectionDetails();
                    thisInfo.Direction = (PantoDirections)inf.ReadInt32();
                    thisInfo.MaxSpeed = inf.ReadSingle();
                    PantoDirectionInfo.Add(thisInfo);
                }
            }

            NoPantoSwap = inf.ReadBoolean();
        }

        public void InitializeMoving()
        {
            State = PantographState.Up;
        }

        public void Initialize()
        {

        }

        public void Update(float elapsedClockSeconds)
        {
            switch (State)
            {
                case PantographState.Lowering:
                    TimeS -= elapsedClockSeconds;

                    if (TimeS <= 0f)
                    {
                        TimeS = 0f;
                        State = PantographState.Down;
                    }
                    break;

                case PantographState.Raising:
                    TimeS += elapsedClockSeconds;

                    if (TimeS >= DelayS)
                    {
                        TimeS = DelayS;
                        State = PantographState.Up;
                    }
                    break;

                case PantographState.Up:
                    // for AI trains : lower panto on max speed
                    if (!Wagon.Train.IsActualPlayerTrain && PantoDirectionInfo != null && PantoDirectionInfo.Count > 0)
                    {
                        bool reqlower = false;

                        foreach (var thisInfo in PantoDirectionInfo)
                        {
                            switch (thisInfo.Direction)
                            {
                                case PantoDirections.Forward:
                                    if (!Wagon.Flipped && thisInfo.MaxSpeed != 0 && Math.Abs(Wagon.SpeedMpS) > thisInfo.MaxSpeed)
                                    {
                                        reqlower = true;
                                    }
                                    break;

                                case PantoDirections.Backward:
                                    if (Wagon.Flipped && thisInfo.MaxSpeed != 0 && Math.Abs(Wagon.SpeedMpS) > thisInfo.MaxSpeed)
                                    {
                                        reqlower = true;
                                    }
                                    break;
                            }
                        }
                        if (reqlower) HandleEvent(PowerSupplyEvent.LowerPantograph);
                    }
                    break;

                case PantographState.Down:
                    // for AI trains : raise panto on stop if power is on
                    if (!Wagon.Train.IsActualPlayerTrain && PantoDirectionInfo != null && PantoDirectionInfo.Count > 0)
                    {
                        foreach (var thisInfo in PantoDirectionInfo)
                        {
                            switch (thisInfo.Direction)
                            {
                                case PantoDirections.Stopped:
                                    if (Math.Abs(Wagon.SpeedMpS) < 0.1f)
                                    {
                                        HandleEvent(PowerSupplyEvent.RaisePantograph);
                                    }
                                    break;
                            }
                        }
                    }
                    break;
            }
        }

        public void HandleEvent(PowerSupplyEvent evt)
        {
            Event soundEvent = Event.None;

            switch (evt)
            {
                case PowerSupplyEvent.LowerPantograph:
                    if (State == PantographState.Up || State == PantographState.Raising)
                    {
                        State = PantographState.Lowering;

                        switch (Id)
                        {
                            default:
                            case 1:
                                soundEvent = Event.Pantograph1Down;
                                Confirm(CabControl.Pantograph1, CabSetting.Off);
                                break;

                            case 2:
                                soundEvent = Event.Pantograph2Down;
                                Confirm(CabControl.Pantograph2, CabSetting.Off);
                                break;

                            case 3:
                                soundEvent = Event.Pantograph3Down;
                                Confirm(CabControl.Pantograph3, CabSetting.Off);
                                break;

                            case 4:
                                soundEvent = Event.Pantograph4Down;
                                Confirm(CabControl.Pantograph4, CabSetting.Off);
                                break;
                        }
                    }

                    break;

                case PowerSupplyEvent.RaisePantograph:
                    if (State == PantographState.Down || State == PantographState.Lowering)
                    {
                        State = PantographState.Raising;

                        switch (Id)
                        {
                            default:
                            case 1:
                                soundEvent = Event.Pantograph1Up;
                                Confirm(CabControl.Pantograph1, CabSetting.On);
                                break;

                            case 2:
                                soundEvent = Event.Pantograph2Up;
                                Confirm(CabControl.Pantograph2, CabSetting.On);
                                break;

                            case 3:
                                soundEvent = Event.Pantograph3Up;
                                Confirm(CabControl.Pantograph3, CabSetting.On);
                                break;

                            case 4:
                                soundEvent = Event.Pantograph4Up;
                                Confirm(CabControl.Pantograph4, CabSetting.On);
                                break;
                        }

                        if (!Simulator.TRK.Tr_RouteFile.Electrified)
                            Simulator.Confirmer.Information(Simulator.Catalog.GetString("Pantograph raised even though this route is not electrified"));
                    }
                    break;

                case PowerSupplyEvent.RaisePantographConditional:
                    // check speed and direction if this panto is to be raised
                    bool raiseValid = true;
                    if (PantoDirectionInfo != null && PantoDirectionInfo.Count > 0)
                    {
                        raiseValid = false;
                        foreach (var thisInfo in PantoDirectionInfo)
                        {
                            switch (thisInfo.Direction)
                            {
                                case PantoDirections.Forward:
                                    if (!Wagon.Flipped)
                                    {
                                        if (thisInfo.MaxSpeed == 0 || Math.Abs(Wagon.SpeedMpS) < thisInfo.MaxSpeed)
                                        {
                                            raiseValid = true;
                                        }
                                    }
                                    break;

                                case PantoDirections.Backward:
                                    if (Wagon.Flipped)
                                    {
                                        if (thisInfo.MaxSpeed == 0 || Math.Abs(Wagon.SpeedMpS) < thisInfo.MaxSpeed)
                                        {
                                            raiseValid = true;
                                        }
                                    }
                                    break;

                                case PantoDirections.Stopped:
                                    if (Wagon.SpeedMpS < 1f)
                                    {
                                        raiseValid = true;
                                    }
                                    break;
                            }
                        }
                    }

                    if (raiseValid && (State == PantographState.Down || State == PantographState.Lowering))
                    {
                        State = PantographState.Raising;

                        switch (Id)
                        {
                            default:
                            case 1:
                                soundEvent = Event.Pantograph1Up;
                                Confirm(CabControl.Pantograph1, CabSetting.On);
                                break;

                            case 2:
                                soundEvent = Event.Pantograph2Up;
                                Confirm(CabControl.Pantograph2, CabSetting.On);
                                break;

                            case 3:
                                soundEvent = Event.Pantograph3Up;
                                Confirm(CabControl.Pantograph3, CabSetting.On);
                                break;

                            case 4:
                                soundEvent = Event.Pantograph4Up;
                                Confirm(CabControl.Pantograph4, CabSetting.On);
                                break;
                        }

                        if (!Simulator.TRK.Tr_RouteFile.Electrified)
                            Simulator.Confirmer.Information(Simulator.Catalog.GetString("Pantograph raised even though this route is not electrified"));
                    }
                    break;
            }

            if (soundEvent != Event.None)
            {
                try
                {
                    foreach (var eventHandler in Wagon.EventHandlers)
                    {
                        eventHandler.HandleEvent(soundEvent);
                    }
                }
                catch (Exception error)
                {
                    Trace.TraceInformation("Sound event skipped due to thread safety problem " + error.Message);
                }
            }
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write(State.ToString());
            outf.Write(DelayS);
            outf.Write(TimeS);

            if (PantoDirectionInfo == null)
            {
                outf.Write(-1);
            }
            else
            {
                outf.Write(PantoDirectionInfo.Count);
                foreach (var thisInfo in PantoDirectionInfo)
                {
                    outf.Write((int)thisInfo.Direction);
                    outf.Write(thisInfo.MaxSpeed);
                }
            }

            outf.Write(NoPantoSwap);
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
