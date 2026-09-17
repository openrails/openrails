// COPYRIGHT 2023 by the Open Rails project.
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
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EmbedIO.WebSockets;
using Newtonsoft.Json;
using Orts.Common;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using Orts.Viewer3D.Popups;
using Orts.Viewer3D.RollingStock;
using ORTS.Scripting.Api;

namespace Orts.Viewer3D.WebServices
{
    public class TrainCarOperationsWebpage : WebSocketModule
    {
        private class OperationsStatus
        {
            // operation = button on Train Car Operations webpage
            public class Operation
            {
                public bool Enabled;
                public string Filename;
                public string Functionname;
                public int CarPosition;
            }

            public int AmountOfCars = 0;

            // each car has a list of operations for the Train Car Operations webpage
            // --> one row on the webpage
            public List<Operation>[] Status;

            // array of car id strings, last column of the row 
            public string[] CarId;
            public string[] CarIdColor;

            public OperationsStatus(int amountOfCars)
            {
                AmountOfCars = amountOfCars;

                Status = new List<Operation>[AmountOfCars];
                CarId = new string[amountOfCars];
                CarIdColor = new string[amountOfCars];
                for (int i = 0; i < amountOfCars; i++)
                {
                    Status[i] = new List<Operation>();
                    CarId[i] = "";
                    CarIdColor[i] = "";
                }
            }
        }

        private readonly Viewer Viewer;

        // static fields because during the restore this objcet not yet created
        public static bool TrainCarFromRestore;
        public static bool TrainCarSelectedFromRestore;
        public static int TrainCarSelectedPositionFromRestore;

        public bool TrainCarSelected;
        public int TrainCarSelectedPosition { get; set; }
        public string CurrentCarID { get; set; }

        public int Connections = 0;

        private OperationsStatus StatusPrevious;
        private OperationsStatus StatusCurrent;

        public class OperationsSend
        {
            public class Operation
            {
                [JsonProperty("Row")]
                public int Row;

                [JsonProperty("Column")]
                public int Column;

                [JsonProperty("Enabled")]
                public bool Enabled;

                [JsonProperty("Filename")]
                public string Filename;
            }

            [JsonProperty("Type")]
            public string Type;

            [JsonProperty("Rows")]
            public int Rows;

            [JsonProperty("Columns")]
            public int Columns;

            [JsonProperty("Operations")]
            public List<Operation> Operations;

            [JsonProperty("CarId")]
            public string[] CarId;

            [JsonProperty("CarIdColor")]
            public string[] CarIdColor;

            public OperationsSend(int amountOfCars)
            {
                Operations = new List<Operation>();
                CarId = new string[amountOfCars];
                CarIdColor = new string[amountOfCars];
                for (int i = 0; i < amountOfCars; i++)
                {
                    CarId[i] = "";
                    CarIdColor[i] = "";
                }
            }
        }

        private bool MessageReceived = false;
        private int Row;
        private int Column;

        private bool ConnectionOpened;

        public TrainCarOperationsWebpage(string url, Viewer viewer) :
            base(url, true)
        {
            Viewer = viewer;
            AddProtocol("json");
        }

        /// <inheritdoc />
        protected override Task OnClientConnectedAsync(IWebSocketContext context)
        {
            Trace.TraceInformation("TrainCarOperationsWebpage, client connected");
            Connections++;
            ConnectionOpened = true;

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task OnClientDisconnectedAsync(IWebSocketContext context)
        {
            Trace.TraceInformation("TrainCarOperationsWebpage, client disconnected");
            Connections--;

            return Task.CompletedTask;
        }

        protected override Task OnMessageReceivedAsync(IWebSocketContext context, byte[] rxBuffer,
            IWebSocketReceiveResult rxResult)
        {
            var data = Encoding.GetString(rxBuffer);

            if (!MessageReceived)
            {
                MessageReceived = true;
                Row = int.Parse(data.Split(':')[1]);
                Column = int.Parse(data.Split(':')[2]);
            }

            return Task.CompletedTask;
        }

        public async Task BroadcastEvent(OperationsSend operationsSend)
        {
            if (Connections > 0)
            {
                try
                {
                    string jsonSend = JsonConvert.SerializeObject(operationsSend);
                    await BroadcastAsync(jsonSend).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Trace.TraceInformation(
                        "TrainCarOperationsWebpage.BroadcastEvent, Json serialize or Broadcast error:");
                    Trace.WriteLine(e);
                }
            }
        }

        double LastPrepareRealTime;
        public void handleReceiveAndSend()
        {
            if ((Viewer.PlayerTrain != null))
            {
                if (Connections > 0)
                {
                    if (Viewer.RealTime - LastPrepareRealTime >= 0.25)
                    {
                        LastPrepareRealTime = Viewer.RealTime;
                        try
                        {
                            handleReceive();
                            handleSend();
                        }
                        catch (Exception error)
                        {
                            // some timing error causes an exception sometimes
                            // just silently ignore but log the exception
                            Trace.TraceWarning(error.ToString());
                        }
                    }
                }
            }
        }

        public void Save(BinaryWriter outf) 
        {
            outf.Write(TrainCarSelected);
            // rwf-rr: temporary fix for bug 2121985
            if (TrainCarSelectedPosition >= Viewer.PlayerTrain.Cars.Count)
            {
                Trace.TraceWarning("TrainCarOperationsWebpage.TrainCarSelectedPosition {0} out of range [0..{1}]", TrainCarSelectedPosition, Viewer.PlayerTrain.Cars.Count - 1);
                TrainCarSelectedPosition = Viewer.PlayerTrain.Cars.Count - 1;
            }
            outf.Write(TrainCarSelectedPosition);
        }

        public static void Restore(BinaryReader inf)
        {
            TrainCarSelectedFromRestore = inf.ReadBoolean();
            TrainCarSelectedPositionFromRestore = inf.ReadInt32();

            TrainCarFromRestore = true;
        }

        private void handleReceive()
        {
            if (MessageReceived)
            {
                handleButtonReceived();
                MessageReceived = false;
            }
        }

        private void handleButtonReceived()
        {
            Type buttonType = Type.GetType("Orts.Viewer3D.WebServices.TrainCarOperationsWebpage");
            string methodName = StatusPrevious.Status[Row][Column].Functionname;

            object buttonObject = this;
            MethodInfo method = buttonType.GetMethod(methodName);
            object[] objects = new object[1];
            objects[0] = (int)StatusPrevious.Status[Row][Column].CarPosition;
            method.Invoke(buttonObject, objects);

            Viewer.TrainCarOperationsViewerWindow.TrainCarOperationsChanged = true;
            Viewer.CarOperationsWindow.CarOperationChanged = true;
        }

        private void handleSend()
        {
            StatusCurrent = new OperationsStatus(Viewer.PlayerTrain.Cars.Count);

            fillStatus(StatusCurrent);
            fillAndSendAsync(StatusCurrent, StatusPrevious);
        }

        private void fillStatus(OperationsStatus operationStatus)
        {
            // Apply reveral point when TrainCarOperations/Viewer windows are not visible
            // Makes this Webpage version, more autonoumus
            if (!Viewer.TrainCarOperationsWindow.Visible && !Viewer.TrainCarOperationsViewerWindow.Visible && Viewer.IsFormationReversed)
            {
                Viewer.IsFormationReversed = false;
                _ = new FormationReversed(Viewer, Viewer.PlayerTrain);
            }

            int carPosition = 0;

            foreach (TrainCar trainCar in Viewer.PlayerTrain.Cars)
            {
                fillStatusArrowLeft(carPosition);

                fillStatusFrontBrakeHose(carPosition);
                fillStatusFrontAngleCock(carPosition);

                addSpace(carPosition);

                fillStatusCouplerFront(carPosition);
                fillStatusLoco(carPosition);
                fillStatusCouplerRear(carPosition);

                addSpace(carPosition);

                fillStatusRearAngleCock(carPosition);
                fillStatusRearBrakeHose(carPosition);

                addSpace(carPosition);

                fillStatusHandBrake(carPosition);

                addSpace(carPosition);

                fillStatusBleedOffValve(carPosition);

                addSpace(carPosition);

                if (Viewer.PlayerTrain.Cars.Count() > 1 && trainCar.PowerSupply != null)
                {
                    fillStatusToggleElectricTrainSupplyCable(carPosition);
                    addSpace(carPosition);
                }
                else
                {
                    addEmpty(carPosition);
                    addSpace(carPosition);
                }

                if ((trainCar is MSTSElectricLocomotive) || (trainCar is MSTSDieselLocomotive))
                {
                    if ((trainCar as MSTSLocomotive).GetMultipleUnitsConfiguration() != null)
                    {
                        fillStatusToggleMU(carPosition);

                        addSpace(carPosition);
                    }
                    else
                    {
                        addEmpty(carPosition);
                        addSpace(carPosition);
                    }

                    fillStatusPower(carPosition);

                    addSpace(carPosition);

                    if (((trainCar as MSTSLocomotive) != null) && ((trainCar as MSTSLocomotive).PowerSupply is IPowerSupply))
                    {
                        fillStatusBatterySwitch(carPosition);

                        addSpace(carPosition);
                    }
                    else
                    {
                        addEmpty(carPosition);
                        addSpace(carPosition);
                    }
                }
                else
                {
                    addEmpty(carPosition);
                    addSpace(carPosition);
                    addEmpty(carPosition);
                    addSpace(carPosition);
                    addEmpty(carPosition);
                    addSpace(carPosition);
                }

                operationStatus.CarId[carPosition] = getCarId(trainCar, carPosition);

                carPosition++;
            }
        }

        private string getCarId(TrainCar trainCar, int carPosition)
        {
            MSTSLocomotive locomotive = trainCar as MSTSLocomotive;
            MSTSWagon wagon = trainCar as MSTSWagon;
            var isDiesel = trainCar is MSTSDieselLocomotive;
            var isElectric = trainCar is MSTSElectricLocomotive;
            var isSteam = trainCar is MSTSSteamLocomotive;
            var isEngine = isDiesel || isElectric || isSteam;
            var wagonType = isEngine ? $"  {Viewer.Catalog.GetString(locomotive.WagonType.ToString())}" + $": {Viewer.Catalog.GetString(locomotive.EngineType.ToString())}"
                : $"  {Viewer.Catalog.GetString(wagon.WagonType.ToString())}: {wagon.MainShapeFileName.Replace(".s", "").ToLower()}";
            return ($"{Viewer.Catalog.GetString("Car ID")} {(carPosition >= Viewer.PlayerTrain.Cars.Count ? " " : Viewer.PlayerTrain.Cars[carPosition].CarID + wagonType)}");
        }

        private void addSpace(int carPosition)
        {
            StatusCurrent.Status[carPosition].Add(
                new OperationsStatus.Operation
                {
                    Enabled = false,
                    Filename = "TrainOperationsEmpty32_16.png",
                    Functionname = "",
                    CarPosition = 0
                });
        }
        private void addEmpty(int carPosition)
        {
            StatusCurrent.Status[carPosition].Add(
                new OperationsStatus.Operation
                {
                    Enabled = false,
                    Filename = "TrainOperationsEmpty32.png",
                    Functionname = "",
                    CarPosition = 0
                });
        }

        private void fillAndSendAsync(OperationsStatus statusCurrent, OperationsStatus statusPrevious)
        {
            OperationsSend operationsSend = new OperationsSend(statusCurrent.AmountOfCars);
            bool all = false;
            bool changed = false;

            if (ConnectionOpened || (statusPrevious == null))
            {
                all = true;
                ConnectionOpened = false;
            }
            else
            {
                if (statusCurrent.AmountOfCars != statusPrevious.AmountOfCars)
                {
                    all = true;
                }
                else
                {
                    for (int i = 0; i < statusCurrent.AmountOfCars; i++)
                    {
                        if (!statusCurrent.CarId[0].Equals(statusPrevious.CarId[0]) || 
                            !statusCurrent.CarIdColor[0].Equals(statusPrevious.CarIdColor[0]))
                        {
                            all = true;
                        }
                    }
                }
            }

            operationsSend.Rows = statusCurrent.AmountOfCars;
            operationsSend.Columns = statusCurrent.Status[0].Count;

            operationsSend.Type = all ? "init" : "update";

            for (int i = 0; i < statusCurrent.AmountOfCars; i++)
            {
                for (int j = 0; j < statusCurrent.Status[i].Count; j++)
                {
                    if ((all) || (statusCurrent.Status[i].ElementAt(j).Filename != statusPrevious.Status[i].ElementAt(j).Filename))
                    {
                        operationsSend.Operations.Add(
                            new OperationsSend.Operation
                            {
                                Row = i,
                                Column = j,
                                Enabled = statusCurrent.Status[i].ElementAt(j).Enabled,
                                Filename = statusCurrent.Status[i].ElementAt(j).Filename,
                            });
                        changed = true;
                    }
                }
            }

            for (int i = 0; i < statusCurrent.AmountOfCars; i++)
            {
                if (all)
                {
                    operationsSend.CarId[i] = StatusCurrent.CarId[i];
                }
                operationsSend.CarIdColor[i] = StatusCurrent.CarIdColor[i];
            }

            if (changed)
            {
                StatusPrevious = StatusCurrent;
                var _ = BroadcastEvent(operationsSend);
            }

        }

        //
        // yellow arrow
        //
        private void fillStatusArrowLeft(int carPosition)
        {
            TrainCarOperationsWindow TrainCar = Viewer.TrainCarOperationsWindow;
            TrainCarOperationsViewerWindow TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
            string filename;

            if (TrainCarFromRestore)
            {
                TrainCarSelected = TrainCarSelectedFromRestore;
                TrainCarSelectedPosition = TrainCarSelectedPositionFromRestore;

                TrainCarFromRestore = false;
            }

            if (Viewer.TrainCarOperationsWindow.Visible) { 
                // take the carposition of the Traincar Operations Window
                TrainCarSelected = true;
                TrainCarSelectedPosition = Viewer.TrainCarOperationsWindow.SelectedCarPosition;
            }
            else
            {
                // select traincar on webpage when traincar operations window (F9) not visible
                if (Viewer.Camera.AttachedCar != null && !(Viewer.Camera is CabCamera) && Viewer.Camera != Viewer.ThreeDimCabCamera)
                {
                    var currentCameraCarID = Viewer.Camera.AttachedCar.CarID;
                    if (Viewer.PlayerTrain != null)
                    {
                        TrainCarSelected = true;
                        TrainCarSelectedPosition = Viewer.PlayerTrain.Cars.TakeWhile(x => x.CarID != currentCameraCarID).Count();
                        TrainCarViewer.CarPosition = TrainCar.SelectedCarPosition = TrainCarSelectedPosition;
                    }
                }
            }

            if (TrainCarSelected && (carPosition == TrainCarSelectedPosition))
            {
                filename = "TrainOperationsArrowRight32.png";
                CurrentCarID = Viewer.PlayerTrain.Cars[carPosition].CarID;// Requiered by OSDCars.cs
            }
            else
            {
                filename = "TrainOperationsEmpty32.png";
            }

            StatusCurrent.Status[carPosition].Add(
                new OperationsStatus.Operation
                {
                    Enabled = false,
                    Filename = filename,
                    Functionname = "",
                    CarPosition = 0
                });
        }

        //
        // front brake hose
        //
        private void fillStatusFrontBrakeHose(int carPosition)
        {
            TrainCar trainCar = Viewer.PlayerTrain.Cars[carPosition];

            string filename;

            bool first = trainCar == Viewer.PlayerTrain.Cars.First();

            if (first)
            {
                filename = "TrainOperationsBrakeHoseFirstDis32.png";
            }
            else
            {
                if (trainCar.BrakeSystem.FrontBrakeHoseConnected)
                {
                    filename = "TrainOperationsBrakeHoseCon32.png";
                }
                else
                {
                    filename = "TrainOperationsBrakeHoseDis32.png";
                    StatusCurrent.CarIdColor[carPosition] = "Cyan";
                }
            }

            StatusCurrent.Status[carPosition].Add(
                new OperationsStatus.Operation
                {
                    Enabled = !first,
                    Filename = filename,
                    Functionname = "buttonFrontBrakeHoseClick",
                    CarPosition = carPosition
                });
        }

        public void buttonFrontBrakeHoseClick(int carPosition)
        {
            TrainCar trainCar = Viewer.PlayerTrain.Cars[carPosition];

            bool first = trainCar == Viewer.PlayerTrain.Cars.First();
            if (first) return;

            TrainCar trainCarOneBefore = Viewer.PlayerTrain.Cars[carPosition - 1];

            new WagonBrakeHoseConnectCommand(Viewer.Log, (trainCar as MSTSWagon), !(trainCar as MSTSWagon).BrakeSystem.FrontBrakeHoseConnected);

            if (trainCar.BrakeSystem.FrontBrakeHoseConnected)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front brake hose connected"));
            }
            else
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front brake hose disconnected"));
            }

            new WagonBrakeHoseRearConnectCommand(Viewer.Log, (trainCarOneBefore as MSTSWagon), trainCar.BrakeSystem.FrontBrakeHoseConnected);
        }

        //
        // front angle cock
        //
        private void fillStatusFrontAngleCock(int carPosition)
        {
            TrainCar trainCar = Viewer.PlayerTrain.Cars[carPosition];
            bool first = trainCar == Viewer.PlayerTrain.Cars.First();
            var carAngleCockAOpenAmount = Viewer.PlayerTrain.Cars[carPosition].BrakeSystem.AngleCockAOpenAmount;

            if (trainCar.BrakeSystem is VacuumSinglePipe)
            {
                StatusCurrent.Status[carPosition].Add(
                    new OperationsStatus.Operation
                    {
                        Enabled = false,
                        Filename = "TrainOperationsFrontAngleCockNotAvailable32.png",
                        Functionname = "",
                        CarPosition = carPosition
                    });
            }
            else
            {
                StatusCurrent.Status[carPosition].Add(
                new OperationsStatus.Operation
                {
                    Enabled = true,
                    Filename = carAngleCockAOpenAmount >= 1 ? "TrainOperationsFrontAngleCockOpened32.png"
                        : carAngleCockAOpenAmount <= 0 ? "TrainOperationsFrontAngleCockClosed32.png"
                        : "TrainOperationsFrontAngleCockPartial32.png",
                    Functionname = "buttonFrontAngleCockClick",
                    CarPosition = carPosition
                });
            }

            if (first)
            {
                if (trainCar.BrakeSystem.AngleCockAOpen)
                {
                    StatusCurrent.CarIdColor[carPosition] = "Cyan";
                }
            }
            else
            {
                if (!trainCar.BrakeSystem.AngleCockAOpen)
                {
                    StatusCurrent.CarIdColor[carPosition] = "Cyan";
                }
            }
        }

        public void buttonFrontAngleCockClick(int carPosition)
        {
            TrainCar trainCar = Viewer.PlayerTrain.Cars[carPosition];

            new ToggleAngleCockACommand(Viewer.Log, (trainCar as MSTSWagon), !(trainCar as MSTSWagon).BrakeSystem.AngleCockAOpen);

            if (trainCar.BrakeSystem.AngleCockAOpen)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front angle cock opened"));
            }
            else
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front angle cock closed"));
            }
        }

        //
        // coupler front
        //
        private void fillStatusCouplerFront(int carPosition)
        {
            TrainCar trainCar = Viewer.PlayerTrain.Cars[carPosition];

            bool first = trainCar == Viewer.PlayerTrain.Cars.First();
            bool disableCouplers = false;
            bool isSteam = Viewer.PlayerTrain.Cars[carPosition] is MSTSSteamLocomotive;
            bool isTender = Viewer.PlayerTrain.Cars[carPosition].WagonType == MSTSWagon.WagonTypes.Tender;

            if (isSteam || isTender)
            {
                var carFlipped = Viewer.PlayerTrain.Cars[carPosition].Flipped;
                disableCouplers = isSteam ? carFlipped : !carFlipped;
            }

            StatusCurrent.Status[carPosition].Add(
                new OperationsStatus.Operation
                {
                    Enabled = !(first || disableCouplers),
                    Filename = first ? "TrainOperationsCouplerFront32.png" : disableCouplers ? "TrainOperationsCouplerNotAvailable32.png" : "TrainOperationsCoupler32.png",
                    Functionname = "buttonCouplerFrontClick",
                    CarPosition = carPosition
                });
        }

        public void buttonCouplerFrontClick(int carPosition)
        {
            TrainCarOperationsWindow TrainCar = Viewer.TrainCarOperationsWindow;
            TrainCarOperationsViewerWindow TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;

            if (Viewer.Simulator.TimetableMode)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("In Timetable Mode uncoupling using this window is not allowed"));
            }
            else
            {
                // Uncouple only the selected car indicated by left yellow arrow
                if (TrainCarSelectedPosition != carPosition)
                    return;

                if (TrainCarSelectedPosition >= carPosition)
                {
                    TrainCarSelected = !TrainCar.Visible;
                }

                new UncoupleCommand(Viewer.Log, carPosition - 1);

                TrainCarViewer.CouplerChanged = TrainCar.CouplerClicked = Viewer.IsDownCameraChanged = true;// Update the car's position
                TrainCarViewer.NewCarPosition = carPosition - 1;
                if (Viewer.CarOperationsWindow.CarPosition > carPosition - 1)
                {
                    Viewer.CarOperationsWindow.Visible = false;
                }
                if (TrainCarSelectedPosition >= carPosition)
                {
                    TrainCarSelectedPosition = TrainCarViewer.NewCarPosition;
                }
                Viewer.FrontCamera.CameraOutsidePosition();
            }
        }

        //
        // loco
        //
        private void fillStatusLoco(int carPosition)
        {
            MSTSWagon wagon = Viewer.PlayerTrain.Cars[carPosition] as MSTSWagon;

            StatusCurrent.Status[carPosition].Add(
                new OperationsStatus.Operation
                {
                    Enabled = true,
                    Filename = (wagon == Viewer.PlayerTrain.LeadLocomotive || wagon is MSTSLocomotive) ? "TrainOperationsLocoGreen32.png"
                        : wagon.BrakesStuck || ((wagon is MSTSLocomotive) && (wagon as MSTSLocomotive).PowerReduction > 0) ? "TrainOperationsLocoRed32.png"
                        : "TrainOperationsLoco32.png",
                    Functionname = "buttonLocoClick",
                    CarPosition = carPosition
                });
        }

        public void buttonLocoClick(int carPosition)
        {
            TrainCarOperationsWindow TrainCar = Viewer.TrainCarOperationsWindow;
            TrainCarOperationsViewerWindow TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;

            if (TrainCarSelected)
            {
                if (TrainCarSelectedPosition == carPosition)
                {
                    TrainCarSelected = !TrainCar.Visible;
                } 
                else
                {
                    TrainCarSelectedPosition = carPosition;
                }
            }
            else
            {
                TrainCarSelected = true;
                TrainCarSelectedPosition = carPosition;
            }

            TrainCarViewer.CarPosition = carPosition;
            TrainCar.SelectedCarPosition = carPosition;
            Viewer.FrontCamera.Activate();
        }

        //
        // coupler rear
        //
        private void fillStatusCouplerRear(int carPosition)
        {
            TrainCar trainCar = Viewer.PlayerTrain.Cars[carPosition];

            bool last = trainCar == Viewer.PlayerTrain.Cars.Last();
            var DisableCouplers = false;
            bool isSteamAndHasTender = (Viewer.PlayerTrain.Cars[carPosition] is MSTSSteamLocomotive) &&
                (carPosition + 1 < Viewer.PlayerTrain.Cars.Count) && (Viewer.PlayerTrain.Cars[carPosition + 1].WagonType == MSTSWagon.WagonTypes.Tender);
            var isTender = Viewer.PlayerTrain.Cars[carPosition].WagonType == MSTSWagon.WagonTypes.Tender;

            if (isSteamAndHasTender || isTender)
            {
                var carFlipped = Viewer.PlayerTrain.Cars[carPosition].Flipped;
                DisableCouplers = isSteamAndHasTender ? !carFlipped : carFlipped;
            }

            StatusCurrent.Status[carPosition].Add(
                new OperationsStatus.Operation
                {
                    Enabled = !(last || DisableCouplers),
                    Filename = last ? "TrainOperationsCouplerRear32.png" : DisableCouplers ? "TrainOperationsCouplerNotAvailable32.png" : "TrainOperationsCoupler32.png",
                    Functionname = "buttonCouplerRearClick",
                    CarPosition = carPosition
                });
        }

        public void buttonCouplerRearClick(int carPosition)
        {
            TrainCarOperationsWindow TrainCar = Viewer.TrainCarOperationsWindow;
            TrainCarOperationsViewerWindow TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;

            if (Viewer.Simulator.TimetableMode)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("In Timetable Mode uncoupling using this window is not allowed"));
            }
            else
            {
                // Uncouple only the selected car indicated by left yellow arrow
                if (TrainCarSelectedPosition != carPosition)
                    return;

                if (TrainCarSelectedPosition >= carPosition)
                {
                    TrainCarSelected = !TrainCar.Visible;
                }

                new UncoupleCommand(Viewer.Log, carPosition);

                TrainCarViewer.CouplerChanged = TrainCar.CouplerClicked = Viewer.IsDownCameraChanged = true;// Update the car's position
                if (Viewer.CarOperationsWindow.CarPosition > carPosition)
                    Viewer.CarOperationsWindow.Visible = false;

                TrainCarViewer.NewCarPosition = carPosition;
                Viewer.FrontCamera.CameraOutsidePosition();
            }
        }

        //
        // rear angle cock
        // 
        private void fillStatusRearAngleCock(int carPosition)
        {
            TrainCar trainCar = Viewer.PlayerTrain.Cars[carPosition];
            bool last = trainCar == Viewer.PlayerTrain.Cars.Last();
            var carAngleCockBOpenAmount = Viewer.PlayerTrain.Cars[carPosition].BrakeSystem.AngleCockBOpenAmount;

            if (trainCar.BrakeSystem is VacuumSinglePipe)
            {
                StatusCurrent.Status[carPosition].Add(
                    new OperationsStatus.Operation
                    {
                        Enabled = false,
                        Filename = "TrainOperationsRearAngleCockNotAvailable32.png",
                        Functionname = "",
                        CarPosition = carPosition
                    });
            }
            else
            {
                StatusCurrent.Status[carPosition].Add(
                    new OperationsStatus.Operation
                    {
                        Enabled = true,
                        Filename = carAngleCockBOpenAmount >= 1 ? "TrainOperationsRearAngleCockOpened32.png"
                            : carAngleCockBOpenAmount <= 0 ? "TrainOperationsRearAngleCockClosed32.png"
                            : "TrainOperationsRearAngleCockPartial32.png",
                        Functionname = "buttonRearAngleCockClick",
                        CarPosition = carPosition
                    });
            }
            if (last)
            {
                if (trainCar.BrakeSystem.AngleCockBOpen)
                {
                    StatusCurrent.CarIdColor[carPosition] = "Cyan";
                }
            } 
            else
            {
                if (!trainCar.BrakeSystem.AngleCockBOpen)
                {
                    StatusCurrent.CarIdColor[carPosition] = "Cyan";
                }
            }
        }

        public void buttonRearAngleCockClick(int carPosition)
        {
            TrainCar trainCar = Viewer.PlayerTrain.Cars[carPosition];

            new ToggleAngleCockBCommand(Viewer.Log, (trainCar as MSTSWagon), !(trainCar as MSTSWagon).BrakeSystem.AngleCockBOpen);

            if (trainCar.BrakeSystem.AngleCockBOpen)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Rear angle cock opened"));
            }
            else
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Rear angle cock closed"));
            }
        }

        //
        // rear brake hose
        //
        private void fillStatusRearBrakeHose(int carPosition)
        {
            TrainCar trainCar = Viewer.PlayerTrain.Cars[carPosition];

            string filename;

            bool last = trainCar == Viewer.PlayerTrain.Cars.Last();

            if (last)
            {
                filename = "TrainOperationsBrakeHoseLastDis32.png";
            } else
            {
                if (trainCar.BrakeSystem.RearBrakeHoseConnected)
                {
                    filename = "TrainOperationsBrakeHoseCon32.png";
                }
                else
                {
                    filename = "TrainOperationsBrakeHoseDis32.png";
                    StatusCurrent.CarIdColor[carPosition] = "Cyan";
                }
            }

            StatusCurrent.Status[carPosition].Add(
                new OperationsStatus.Operation
                {
                    Enabled = !last,
                    Filename = filename,
                    Functionname = "buttonRearBrakeHoseClick",
                    CarPosition = carPosition
                });
        }

        public void buttonRearBrakeHoseClick(int carPosition)
        {
            TrainCar trainCar = Viewer.PlayerTrain.Cars[carPosition];

            bool last = trainCar == Viewer.PlayerTrain.Cars.Last();
            if (last) return;

            TrainCar trainCarOneAfter = Viewer.PlayerTrain.Cars[carPosition + 1];

            new WagonBrakeHoseRearConnectCommand(Viewer.Log, (trainCar as MSTSWagon), !(trainCar as MSTSWagon).BrakeSystem.RearBrakeHoseConnected);

            if ((trainCar as MSTSWagon).BrakeSystem.RearBrakeHoseConnected)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Rear brake hose connected"));
            }
            else
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Rear brake hose disconnected"));
            }

            new WagonBrakeHoseConnectCommand(Viewer.Log, (trainCarOneAfter as MSTSWagon), (trainCar as MSTSWagon).BrakeSystem.RearBrakeHoseConnected);
        }

        //
        // hand brake
        // 
        private void fillStatusHandBrake(int carPosition)
        {
            TrainCar trainCar = Viewer.PlayerTrain.Cars[carPosition];

            string filename;
            bool enabled = false;

            if ((trainCar as MSTSWagon).MSTSBrakeSystem.HandBrakePresent)
            {
                enabled = true;
                if ((trainCar as MSTSWagon).GetTrainHandbrakeStatus())
                {
                    filename = "TrainOperationsHandBrakeSet32.png";
                    StatusCurrent.CarIdColor[carPosition] = "Cyan";
                }
                else
                {
                    filename = "TrainOperationsHandBrakeNoSet32.png";
                }
            } 
            else
            {
                filename = "TrainOperationsHandBrakeNotAvailable32.png";
            }

            StatusCurrent.Status[carPosition].Add(
                    new OperationsStatus.Operation
                    {
                        Enabled = enabled,
                        Filename = filename,
                        Functionname = "buttonHandBrakeClick",
                        CarPosition = carPosition
                    });
        }

        public void buttonHandBrakeClick(int carPosition)
        {
            TrainCar trainCar = Viewer.PlayerTrain.Cars[carPosition];

            if (Viewer.Simulator.TimetableMode)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("In Timetable Mode uncoupling using this window is not allowed"));
            }
            else
            {
                new WagonHandbrakeCommand(Viewer.Log, (trainCar as MSTSWagon), !(trainCar as MSTSWagon).GetTrainHandbrakeStatus());

                if ((trainCar as MSTSWagon).GetTrainHandbrakeStatus())
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Handbrake set"));
                }
                else
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Handbrake off"));
                }
            }
        }

        //
        // bleed off valve
        // 
        private void fillStatusBleedOffValve(int carPosition)
        {
            TrainCar trainCar = Viewer.PlayerTrain.Cars[carPosition];

            string filename;
            bool enabled;

            if ((trainCar as MSTSWagon).BrakeSystem is SingleTransferPipe
                || Viewer.PlayerTrain.Cars.Count() == 1)
            {
                enabled = false;
                filename = "TrainOperationsBleedOffValveNotAvailable32.png";
            }
            else
            {
                enabled = true;
                filename = (trainCar as MSTSWagon).BrakeSystem.BleedOffValveOpen ? "TrainOperationsBleedOffValveOpened32.png" : "TrainOperationsBleedOffValveClosed32.png";

                if ((trainCar as MSTSWagon).BrakeSystem.BleedOffValveOpen)
                {
                    StatusCurrent.CarIdColor[carPosition] = "Cyan";
                }
            }

            StatusCurrent.Status[carPosition].Add(
                    new OperationsStatus.Operation
                    {
                        Enabled = enabled,
                        Filename = filename,
                        Functionname = "buttonBleedOffValveClick",
                        CarPosition = carPosition
                    });
        }

        public void buttonBleedOffValveClick(int carPosition)
        {
            TrainCar trainCar = Viewer.PlayerTrain.Cars[carPosition];

            new ToggleBleedOffValveCommand(Viewer.Log, (trainCar as MSTSWagon), !(trainCar as MSTSWagon).BrakeSystem.BleedOffValveOpen);

            if ((trainCar as MSTSWagon).BrakeSystem.BleedOffValveOpen)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Bleed off valve opened"));
            }
            else
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Bleed off valve closed"));
            }
        }

        //
        // ETS electric train supply cable
        //
        private void fillStatusToggleElectricTrainSupplyCable(int carPosition)
        {
            TrainCar trainCar = Viewer.PlayerTrain.Cars[carPosition];

            bool enabled = false;
            string filename;

            if ((trainCar as MSTSWagon).PowerSupply != null && Viewer.PlayerTrain.Cars.Count() > 1)
            {
                enabled = true;
                filename = (trainCar as MSTSWagon).PowerSupply.FrontElectricTrainSupplyCableConnected ? "TrainOperationsETSconnected32.png" : "TrainOperationsETSdisconnected32.png";
            }
            else
            {
                filename = "TrainOperationsEmpty32.png";
            }

            StatusCurrent.Status[carPosition].Add(
                    new OperationsStatus.Operation
                    {
                        Enabled = enabled,
                        Filename = filename,
                        Functionname = "ToggleElectricTrainSupplyCableClick",
                        CarPosition = carPosition
                    });
        }

        public void ToggleElectricTrainSupplyCableClick(int carPosition)
        {
            TrainCar trainCar = Viewer.PlayerTrain.Cars[carPosition];

            new ConnectElectricTrainSupplyCableCommand(Viewer.Log, (trainCar as MSTSWagon), !trainCar.PowerSupply.FrontElectricTrainSupplyCableConnected);

            if (trainCar.PowerSupply.FrontElectricTrainSupplyCableConnected)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front ETS cable connected"));
            }
            else
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front ETS cable disconnected"));
            }
        }

        //
        // MU
        //
        private void fillStatusToggleMU(int carPosition)
        {
            TrainCar trainCar = Viewer.PlayerTrain.Cars[carPosition];

            string filename;
            bool enabled = false;

            var multipleUnitsConfiguration = Viewer.PlayerLocomotive.GetMultipleUnitsConfiguration();

            if (trainCar is MSTSDieselLocomotive && multipleUnitsConfiguration != null)
            {
                enabled = true;
                filename = Viewer.TrainCarOperationsWindow.ModifiedSetting || ((trainCar as MSTSLocomotive).RemoteControlGroup == 0 && multipleUnitsConfiguration != "1") ?
                    "TrainOperationsMUconnected32.png" : "TrainOperationsMUdisconnected32.png";
            }
            else
            {
                filename = "TrainOperationsEmpty32.png";

            }
            StatusCurrent.Status[carPosition].Add(
                    new OperationsStatus.Operation
                    {
                        Enabled = enabled,
                        Filename = filename,
                        Functionname = "buttonToggleMUClick",
                        CarPosition = carPosition
                    });
        }

        public void buttonToggleMUClick(int carPosition)
        {
            TrainCar trainCar = Viewer.PlayerTrain.Cars[carPosition];
            MSTSLocomotive locomotive = trainCar as MSTSLocomotive;

            new ToggleMUCommand(Viewer.Log, locomotive, locomotive.RemoteControlGroup < 0);

            if (locomotive.RemoteControlGroup == 0)
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("MU signal connected"));
            else
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("MU signal disconnected"));
        }

        //
        // power
        // 
        private void fillStatusPower(int carPosition)
        {
            TrainCar trainCar = Viewer.PlayerTrain.Cars[carPosition];

            string filename = "TrainOperationsEmpty32.png";

            if (trainCar is MSTSElectricLocomotive)
            {
                switch ((trainCar as MSTSElectricLocomotive).ElectricPowerSupply.CircuitBreaker.State)
                {
                    case CircuitBreakerState.Closing:
                        filename = "TrainOperationsPowerChanging32.png";
                        break;
                    case CircuitBreakerState.Open:
                        filename = "TrainOperationsPowerOff32.png";
                        break;
                    case CircuitBreakerState.Closed:
                        filename = "TrainOperationsPowerOn32.png";
                        break;
                }
            }

            if (trainCar is MSTSDieselLocomotive)
            {
                switch ((trainCar as MSTSDieselLocomotive).DieselEngines.State)
                {
                    case DieselEngineState.Stopped:
                        filename = "TrainOperationsPowerOff32.png";
                        break;
                    case DieselEngineState.Starting:
                        filename = "TrainOperationsPowerChanging32.png";
                        break;
                    case DieselEngineState.Running:
                        filename = "TrainOperationsPowerOn32.png";
                        break;
                    case DieselEngineState.Stopping:
                        filename = "TrainOperationsPowerChanging32.png";
                        break;
                }
            }

            StatusCurrent.Status[carPosition].Add(
                new OperationsStatus.Operation
                {
                    Enabled = true,
                    Filename = filename,
                    Functionname = "buttonTogglePowerClick",
                    CarPosition = carPosition
            });
        }

        public void buttonTogglePowerClick(int carPosition)
        {
            TrainCar trainCar = Viewer.PlayerTrain.Cars[carPosition];
            MSTSLocomotive locomotive = trainCar as MSTSLocomotive;

            new PowerCommand(Viewer.Log, locomotive, !locomotive.LocomotivePowerSupply.MainPowerSupplyOn);

            var mainPowerSupplyOn = locomotive.LocomotivePowerSupply.MainPowerSupplyOn;
            if (mainPowerSupplyOn)
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Power OFF command sent"));
            else
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Power ON command sent"));
        }

        //
        // battery switch
        //
        private void fillStatusBatterySwitch(int carPosition)
        {
            TrainCar trainCar = Viewer.PlayerTrain.Cars[carPosition];

            bool enabled = false;
            string filename;

            if ((trainCar is MSTSWagon wagon) && (wagon.PowerSupply is IPowerSupply))
            {
                MSTSLocomotive locomotive = trainCar as MSTSLocomotive;

                if (locomotive.LocomotivePowerSupply.BatterySwitch.Mode == Simulation.RollingStocks.SubSystems.PowerSupplies.BatterySwitch.ModeType.AlwaysOn)
                {
                    filename = "TrainOperationsBattAlwaysOn32.png";
                }
                else
                {
                    enabled = true;
                    if (locomotive.LocomotivePowerSupply.BatterySwitch.On)
                    {
                        filename = "TrainOperationsBattOn32.png";
                    }
                    else
                    {
                        filename = "TrainOperationsBattOff32.png";
                    }
                }
            }
            else
            {
                filename = "TrainOperationsEmpty32.png";
            }

            StatusCurrent.Status[carPosition].Add(
                    new OperationsStatus.Operation
                    {
                        Enabled = enabled,
                        Filename = filename,
                        Functionname = "ToggleBatterySwitchClick",
                        CarPosition = carPosition
                    });
        }

        public void ToggleBatterySwitchClick(int carPosition)
        {
            TrainCar trainCar = Viewer.PlayerTrain.Cars[carPosition];

            new ToggleBatterySwitchCommand(Viewer.Log, (trainCar as MSTSWagon), !trainCar.PowerSupply.BatterySwitch.On);

            if (trainCar.PowerSupply.BatterySwitch.On)
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Switch off battery command sent"));
            else
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Switch on battery command sent"));
        }
    }
}
