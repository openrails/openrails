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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Orts.Parsers.Msts;
using ORTS.Scripting.Api;
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
            switch (lowercasetoken)
            {
                case "wagon(ortspantographs":
                    List.Clear();

                    stf.MustMatch("(");
                    stf.ParseBlock(
                        new[] {
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
                    )
                }
            );
        }

        public void Copy(Pantograph pantograph)
        {
            State = pantograph.State;
            DelayS = pantograph.DelayS;
            TimeS = pantograph.TimeS;
        }

        public void Restore(BinaryReader inf)
        {
            State = (PantographState)Enum.Parse(typeof(PantographState), inf.ReadString());
            DelayS = inf.ReadSingle();
            TimeS = inf.ReadSingle();
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
