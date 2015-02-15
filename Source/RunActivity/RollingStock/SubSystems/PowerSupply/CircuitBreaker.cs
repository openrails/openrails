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
using System.IO;
using Orts.Parsers.Msts;
using ORTS.Scripting.Api;

namespace ORTS
{

    public class ScriptedCircuitBreaker
    {
        readonly MSTSElectricLocomotive Locomotive;
        readonly Simulator Simulator;

        public bool Activated = false;
        string ScriptName = "Automatic";
        CircuitBreaker Script;

        private float DelayS = 0f;

        public CircuitBreakerState State { get; private set; }
        public bool DriverCloseAuthorization { get; private set; }

        public ScriptedCircuitBreaker(MSTSElectricLocomotive locomotive)
        {
            Simulator = locomotive.Simulator;
            Locomotive = locomotive;
        }

        public void Copy(ScriptedCircuitBreaker other)
        {
            ScriptName = other.ScriptName;
            State = other.State;
            DelayS = other.DelayS;
        }

        public void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortscircuitbreaker":
                    if (Locomotive.Train as AITrain == null)
                    {
                        ScriptName = stf.ReadStringBlock(null);
                    }
                    break;

                case "engine(ortscircuitbreakerclosingdelay":
                    DelayS = stf.ReadFloatBlock(STFReader.UNITS.Time, null);
                    break;
            }
        }

        public void Restore(BinaryReader inf)
        {
            ScriptName = inf.ReadString();
            State = (CircuitBreakerState) Enum.Parse(typeof(CircuitBreakerState), inf.ReadString());
            DelayS = inf.ReadSingle();
        }

        public void Initialize()
        {
            if (!Activated)
            {
                if (ScriptName != null && ScriptName != "Automatic")
                {
                    var pathArray = new string[] { Path.Combine(Path.GetDirectoryName(Locomotive.WagFilePath), "Script") };
                    Script = Simulator.ScriptManager.Load(pathArray, ScriptName) as CircuitBreaker;
                }
                if (Script == null)
                {
                    Script = new AutomaticCircuitBreaker() as CircuitBreaker;
                }

                Script.ClockTime = () => (float)Simulator.ClockTime;
                Script.DistanceM = () => Locomotive.DistanceM;
                Script.CurrentState = () => State;
                Script.CurrentPantographState = () => Locomotive.Pantographs.State;
                Script.CurrentPowerSupplyState = () => Locomotive.PowerSupply.State;
                Script.TCSCloseAuthorization = () => {
                    MSTSLocomotive locomotive = Locomotive.Train.LeadLocomotive as MSTSLocomotive;
                    if (locomotive != null)
                        return locomotive.TrainControlSystem.PowerAuthorization;
                    else
                        return false;
                };
                Script.DriverCloseAuthorization = () => DriverCloseAuthorization;
                Script.ClosingDelayS = () => DelayS;

                Script.SetCurrentState = (value) => State = value;
                Script.SetDriverCloseAuthorization = (value) => DriverCloseAuthorization = value;

                Script.Initialize();
                Activated = true;
            }
        }

        public void InitializeMoving()
        {
            State = CircuitBreakerState.Closed;
        }

        public void Update(float elapsedSeconds)
        {
            if (Locomotive.Train.TrainType == Train.TRAINTYPE.AI || Locomotive.Train.TrainType == Train.TRAINTYPE.AI_AUTOGENERATE
                || Locomotive.Train.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING)
            {
                State = CircuitBreakerState.Closed;
            }
            else
            {
                if (Script != null)
                {
                    Script.Update(elapsedSeconds);
                }
            }
        }

        public void HandleEvent(PowerSupplyEvent evt)
        {
            if (Script == null)
                return;

            Script.HandleEvent(evt);
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write(ScriptName);
            outf.Write(State.ToString());
            outf.Write(DelayS);
        }
    }

    class AutomaticCircuitBreaker : CircuitBreaker
    {
        private Timer ClosingTimer;

        public override void Initialize()
        {
            ClosingTimer = new Timer(this);
            ClosingTimer.Setup(ClosingDelayS());

            SetDriverCloseAuthorization(true);
        }

        public override void Update(float elapsedSeconds)
        {
            if (TCSCloseAuthorization() && CurrentPantographState() == PantographState.Up)
            {
                if (!ClosingTimer.Started)
                    ClosingTimer.Start();

                SetCurrentState(ClosingTimer.Triggered ? CircuitBreakerState.Closed : CircuitBreakerState.Open);
            }
            else
            {
                if (ClosingTimer.Started)
                    ClosingTimer.Stop();

                SetCurrentState(CircuitBreakerState.Open);
            }
        }

        public override void HandleEvent(PowerSupplyEvent evt)
        {
            // Nothing to do since it is automatic
        }
    }
}
