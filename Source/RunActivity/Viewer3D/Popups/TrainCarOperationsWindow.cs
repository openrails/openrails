// COPYRIGHT 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
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

// This file is the responsibility of the 3D & Environment Team.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Common;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems.Brakes;
using Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS;
using ORTS.Common;
using ORTS.Common.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Orts.Viewer3D.Popups.TrainCarOperationsWindow;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;

namespace Orts.Viewer3D.Popups
{
    public class TrainCarOperationsWindow : Window
    {
        const int CarListPadding = 2;
        internal static Texture2D ArrowRight;
        internal static Texture2D ArrowLeft;
        //internal static Texture2D BatChanging;//TO DO:
        internal static Texture2D BattAlwaysOn;
        internal static Texture2D BattOff;
        internal static Texture2D BattOn;
        internal static Texture2D BleedOffValveOpened;
        internal static Texture2D BleedOffValveClosed;
        internal static Texture2D BleedOffValveNotAvailable;
        internal static Texture2D BrakeHoseCon;
        internal static Texture2D BrakeHoseDis;
        internal static Texture2D BrakeHoseFirstDis;
        internal static Texture2D BrakeHoseLastDis;
        internal static Texture2D Coupler;
        internal static Texture2D CouplerFront;
        internal static Texture2D CouplerRear;
        internal static Texture2D Empty;
        internal static Texture2D ETSconnected;
        internal static Texture2D ETSdisconnected;
        internal static Texture2D FrontAngleCockClosed;
        internal static Texture2D FrontAngleCockOpened;
        internal static Texture2D HandBrakeNotAvailable;
        internal static Texture2D HandBrakeNotSet;
        internal static Texture2D HandBrakeSet;
        internal static Texture2D MUconnected;
        internal static Texture2D MUdisconnected;
        internal static Texture2D PowerChanging;
        internal static Texture2D PowerOff;
        internal static Texture2D PowerOn;
        internal static Texture2D RearAngleCockClosed;
        internal static Texture2D RearAngleCockOpened;

        public int DisplaySizeY;
        public bool LayoutUpdated = false;
        public bool AllSymbolsMode = true;
        public int LocoRowCount;
        public int RowsCount;
        public int SeparatorCount;
        public int SpacerRowCount;
        public int SymbolsRowCount;

        public int CarPositionVisible;
        public ControlLayout Client;
        public int CurrentCarPosition;
        public int LabelTop;
        public bool LastRowVisible = false;
        public int LocalScrollPosition;
        public int SelectedCarPosition;
        const int SymbolSize = 16;

        public int WindowHeightMax;
        public int WindowHeightMin;
        public int WindowWidthMax;
        public int WindowWidthMin;
        public int CurrentDisplaySizeY;
        public bool IsFullScreen = false;
        public int RowHeight;
        public int OldPositionHeight;
        public bool UpdateTrainCarOperation = false;
        public List<int> LabelPositionTop = new List<int>();

        //Rectangle carLabelPosition;
        public string CarLabelText;
        public int CarPosition;
        public int CarUIDLenght;
        public int DesiredHeight;
        public static bool FontChanged;
        public static bool FontToBold;
        public ControlLayoutVertical Vbox;

        //Electrical power
        public string BatteryStatus;
        public string CircuitBreakerState;
        public bool MainPowerSupplyOn;
        public string PowerSupplyStatus;
        public bool PowerSupplyUpdating;
        public bool SupplyStatusChanged;
        public bool UpdatingPowerSupply = false;

        public bool WarningEnabled = false;
        public bool CarIdClicked = false;
        public bool ModifiedSetting
        {
            set;
            get;
        }

        public List<bool> WarningCarPosition = new List<bool>();
        public struct ListLabel
        {
            public string CarID;
            public int CarIDWidth;
        }
        public List<ListLabel> Labels = new List<ListLabel>();

        Train PlayerTrain;
        int LastPlayerTrainCars;
        bool LastPlayerLocomotiveFlippedState;

        public TrainCarOperationsWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + CarListPadding + (owner.TextFontDefault.Height * 15), Window.DecorationSize.Y + (owner.TextFontDefault.Height * 5), Viewer.Catalog.GetString("Train Car Operations"))
        {
            WindowHeightMin = Location.Height;
            WindowHeightMax = Location.Height + (owner.TextFontDefault.Height * 20);
            WindowWidthMin = Location.Width;
            WindowWidthMax = Location.Width + (owner.TextFontDefault.Height * 20);
            CurrentDisplaySizeY = Owner.Viewer.DisplaySize.Y;
        }
        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            outf.Write(Location.X);
            outf.Write(Location.Y);
            outf.Write(Location.Width);
            outf.Write(Location.Height);

            outf.Write(SelectedCarPosition);
        }
        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            Rectangle LocationRestore;
            LocationRestore.X = inf.ReadInt32();
            LocationRestore.Y = inf.ReadInt32();
            LocationRestore.Width = inf.ReadInt32();
            LocationRestore.Height = inf.ReadInt32();

            SelectedCarPosition = inf.ReadInt32();

            // Display window
            SizeTo(LocationRestore.Width, LocationRestore.Height);
            MoveTo(LocationRestore.X, LocationRestore.Y);
        }
        protected internal override void Initialize()
        {
            base.Initialize();

            // Reset window size
            if (Visible) UpdateWindowSize();

            if (Coupler == null)
            {
                // TO DO: This should happen on the loader thread.
                ArrowRight = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsArrowLeft.png"));
                ArrowLeft = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsArrowRight.png"));

                Coupler = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsCoupler.png"));//-
                CouplerFront = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsCouplerFront.png"));
                CouplerRear = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsCouplerRear.png"));

                Empty = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsEmpty.png"));

                HandBrakeNotAvailable = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsHandBrakeNotAvailable.png"));
                HandBrakeNotSet = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsHandBrakeNoSet.png"));//-
                HandBrakeSet = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsHandBrakeSet.png"));//-

                BrakeHoseCon = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsBrakeHoseCon.png"));
                BrakeHoseDis = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsBrakeHoseDis.png"));
                BrakeHoseFirstDis = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsBrakeHoseFirstDis.png"));
                BrakeHoseLastDis = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsBrakeHoseLastDis.png"));

                FrontAngleCockOpened = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsFrontAngleCockOpened.png"));
                FrontAngleCockClosed = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsFrontAngleCockClosed.png"));

                BleedOffValveClosed = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsBleedOffValveClosed.png"));//-
                BleedOffValveOpened = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsBleedOffValveOpened.png"));//-
                BleedOffValveNotAvailable = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsBleedOffValveNotAvailable.png"));

                RearAngleCockClosed = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsRearAngleCockClosed.png"));
                RearAngleCockOpened = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsRearAngleCockOpened.png"));

                PowerChanging = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsPowerChanging.png"));
                PowerOff = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsPowerOff.png"));
                PowerOn = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsPowerOn.png"));

                MUconnected = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsMUconnected.png"));
                MUdisconnected = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsMUdisconnected.png"));

                ETSconnected = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsETSconnected.png"));
                ETSdisconnected = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsETSdisconnected.png"));

                BattOff = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsBattOff.png"));
                BattOn = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsBattOn.png"));
                BattAlwaysOn = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsBattAlwaysOn.png"));
            }
        }
        private void UpdateWindowSize()
        {
            ModifyWindowSize();
        }
        void updateLayoutSize()
        {
            Labels.Clear();

            if (PlayerTrain != null)
            {
                int carPosition = 0;
                foreach (var car in PlayerTrain.Cars)
                {
                    var carUid = car.CarID;
                    var carUIDWidth = FontToBold ? Owner.TextFontDefaultBold.MeasureString(carUid.TrimEnd())
                                : Owner.TextFontDefault.MeasureString(carUid.TrimEnd());
                    Labels.Add(new ListLabel
                    {
                        CarID = carUid,
                        CarIDWidth = carUIDWidth
                    });
                    carPosition++;
                }
                ModifyWindowSize();
            }
        }

        /// <summary>
        /// Modify window size
        /// </summary>
        private void ModifyWindowSize()
        {
            if (SymbolsRowCount > 0)
            {
                var textWidth = Owner.TextFontDefault.Height;

                CarUIDLenght = Labels.Max(x => x.CarIDWidth);

                DisplaySizeY = Owner.Viewer.DisplaySize.Y;
                IsFullScreen = Owner.Viewer.RenderProcess.isFullScreen;

                // Validates rows with windows DPI settings
                var dpiScale = System.Drawing.Graphics.FromHwnd(IntPtr.Zero).DpiY / 96;
                var separatorSize = ControlLayout.SeparatorSize;

                DesiredHeight = FontToBold ? (Owner.TextFontDefaultBold.Height * (RowsCount + 1)) + (separatorSize * (SeparatorCount + 3))
                    : (Owner.TextFontDefault.Height * (RowsCount + 1)) + (separatorSize * (SeparatorCount + 3));
                var desiredWidth = (SymbolsRowCount * SymbolSize) + (SpacerRowCount * (textWidth / 2)) + CarUIDLenght + (textWidth * 2);

                // Takes the height of the TrainOperationsViewer as the margin area.
                RowHeight = textWidth + separatorSize;
                var midlayoutRow = (CarPositionVisible * RowHeight) / 2;
                var marginTraincarViewer = Owner.Viewer.TrainCarOperationsViewerWindow.windowHeight + 32;//32 = imageHeight
                marginTraincarViewer = dpiScale == 1 ? marginTraincarViewer + (IsFullScreen ? separatorSize : separatorSize)
                    : dpiScale == 1.75 ? marginTraincarViewer + (IsFullScreen ? separatorSize : textWidth)
                    : marginTraincarViewer;
                WindowHeightMax = DisplaySizeY - marginTraincarViewer;

                var newHeight = MathHelper.Clamp(DesiredHeight, ((textWidth + separatorSize) * 3) + separatorSize, WindowHeightMax);
                var newWidth = MathHelper.Clamp(desiredWidth, 100, WindowWidthMax);

                // Move the dialog up if we're expanding it, or down if not; this keeps the center in the same place.
                var newTop = Location.Y + ((Location.Height - newHeight) / 2);

                // Display window
                SizeTo(newWidth, newHeight);
                MoveTo(Location.X, newTop);
            }
        }
        protected override ControlLayout Layout(ControlLayout layout)
        {
            var textHeight = Owner.TextFontDefault.Height;
            var trainCarViewer = Owner.Viewer.TrainCarOperationsViewerWindow;
            Vbox = base.Layout(layout).AddLayoutVertical();
            LocoRowCount = 0;
            SpacerRowCount = 0;
            SymbolsRowCount = 0;

            WarningCarPosition.Clear();

            if (PlayerTrain != null)
            {
                int carPosition = 0;
                ControlLayout scrollbox;

                scrollbox = PlayerTrain.Cars.Count == 1 ? Vbox.AddLayoutHorizontal(Vbox.RemainingHeight)
                            : Vbox.AddLayoutScrollboxVertical(Vbox.RemainingWidth);

                {
                    var line = Vbox.AddLayoutHorizontalLineOfText();
                    void AddSpace()
                    {
                        line.AddSpace(textHeight / 2, line.RemainingHeight);
                    }

                    if (LabelPositionTop.Count == 0)
                    {
                        var n = scrollbox.Position.Y;// first row
                        for (var i = 0; i < PlayerTrain.Cars.Count; i++)
                        {   // Position of each row
                            LabelPositionTop.Add(n);
                            n += (textHeight + ControlLayout.SeparatorSize);
                        }
                    }

                    // reset WarningCarPosition
                    WarningCarPosition = Enumerable.Repeat(false, PlayerTrain.Cars.Count).ToList();

                    foreach (var car in PlayerTrain.Cars)
                    {
                        TrainCar trainCar = PlayerTrain.Cars[carPosition];
                        BrakeSystem brakeSystem = (trainCar as MSTSWagon).BrakeSystem;
                        MSTSLocomotive locomotive = trainCar as MSTSLocomotive;
                        MSTSWagon wagon = trainCar as MSTSWagon;

                        bool isElectricDieselLocomotive = (trainCar is MSTSElectricLocomotive) || (trainCar is MSTSDieselLocomotive);

                        line = scrollbox.AddLayoutHorizontalLineOfText();
                        {
                            var carLabel = new buttonLoco(CarUIDLenght + textHeight, textHeight, Owner.Viewer, car, carPosition, LabelAlignment.Center);
                            carLabel.Click += new Action<Control, Point>(carLabel_Click);

                            if (car == PlayerTrain.LeadLocomotive || car is MSTSLocomotive || car.WagonType == TrainCar.WagonTypes.Tender) carLabel.Color = Color.Green;

                            if (car.BrakesStuck || ((car is MSTSLocomotive) && (car as MSTSLocomotive).PowerReduction > 0)) carLabel.Color = Color.Red;

                            line.Add(new buttonArrowLeft(0, 0, SymbolSize, Owner.Viewer, carPosition));
                            AddSpace();
                            if (car != PlayerTrain.Cars.First())
                            {
                                line.Add(new buttonFrontBrakeHose(0, 0, SymbolSize, Owner.Viewer, car, carPosition));
                                line.Add(new buttonFrontAngleCock(0, 0, SymbolSize, Owner.Viewer, car, carPosition));
                                AddSpace();
                                line.Add(new buttonCouplerFront(0, 0, SymbolSize, Owner.Viewer, car));
                            }
                            else
                            {
                                line.Add(new buttonFrontBrakeHose(0, 0, SymbolSize, Owner.Viewer, car, carPosition));
                                line.Add(new buttonFrontAngleCock(0, 0, SymbolSize, Owner.Viewer, car, carPosition));
                                AddSpace();
                                line.Add(new buttonCouplerFront(0, 0, SymbolSize, Owner.Viewer, car));
                            }
                            line.Add(carLabel);
                            if (car != PlayerTrain.Cars.Last())
                            {
                                line.Add(new buttonCouplerRear(0, 0, SymbolSize, Owner.Viewer, car));
                                AddSpace();
                                line.Add(new buttonRearAngleCock(0, 0, SymbolSize, Owner.Viewer, car, carPosition));
                                line.Add(new buttonRearBrakeHose(0, 0, SymbolSize, Owner.Viewer, car, carPosition));
                            }
                            else
                            {
                                line.Add(new buttonCouplerRear(0, 0, SymbolSize, Owner.Viewer, car));
                                AddSpace();
                                line.Add(new buttonRearAngleCock(0, 0, SymbolSize, Owner.Viewer, car, carPosition));
                                line.Add(new buttonRearBrakeHose(0, 0, SymbolSize, Owner.Viewer, car, carPosition));
                            }

                            AddSpace();

                            // Handbrake
                            line.Add(new buttonHandBrake(0, 0, SymbolSize, Owner.Viewer, carPosition));
                            AddSpace();

                            // Bleed Off Valve
                            line.Add(new buttonBleedOffValve(0, 0, SymbolSize, Owner.Viewer, carPosition));
                            AddSpace();

                            if (AllSymbolsMode)//Allows to display all symbols
                            {
                                // Electric Train Supply Connection (ETS)
                                if (PlayerTrain.Cars.Count() > 1 && wagon.PowerSupply != null)
                                {
                                    line.Add(new buttonToggleElectricTrainSupplyCable(0, 0, SymbolSize, Owner.Viewer, carPosition));
                                    AddSpace();
                                }
                                if (isElectricDieselLocomotive)
                                {
                                    if (locomotive.GetMultipleUnitsConfiguration() != null)
                                    {
                                        line.Add(new buttonToggleMU(0, 0, SymbolSize, Owner.Viewer, carPosition));
                                        AddSpace();
                                    }
                                    line.Add(new buttonTogglePower(0, 0, SymbolSize, Owner.Viewer, carPosition));
                                    AddSpace();

                                    if ((wagon != null) && (wagon.PowerSupply is IPowerSupply))
                                    {
                                        line.Add(new buttonToggleBatterySwitch(0, 0, SymbolSize, Owner.Viewer, carPosition));
                                        AddSpace();
                                    }
                                }
                            }

                            line.Add(new buttonArrowRight(0, 0, textHeight, Owner.Viewer, carPosition));
                            AddSpace();
                            AddSpace();
                            // Set color to cyan
                            var color = carLabel.Color;
                            if (WarningCarPosition[carPosition])
                            {
                                carLabel.Color = Color.Cyan;
                                WarningEnabled = true;
                            }
                            else
                                carLabel.Color = color;

                            if (car != PlayerTrain.Cars.Last())
                                scrollbox.AddHorizontalSeparator();

                            carPosition++;
                        }
                        // Recount
                        var tempspacerRowCount = line.Controls.Where(c => c is Spacer).Count();
                        var templocoRowCount = line.Controls.Where(c => c is Label).Count();
                        var tempsymbolsRowCount = line.Controls.Where(c => c is Image).Count();
                        SpacerRowCount = SpacerRowCount < tempspacerRowCount ? tempspacerRowCount : SpacerRowCount;
                        LocoRowCount = LocoRowCount < templocoRowCount ? templocoRowCount : LocoRowCount;
                        SymbolsRowCount = SymbolsRowCount < tempsymbolsRowCount ? tempsymbolsRowCount : SymbolsRowCount;
                    }
                    Client = ControlLayoutScrollboxVertical.NewClient;
                    RowsCount = Client.Controls.Where(c => c is ControlLayoutHorizontal).Count();
                    SeparatorCount = Client.Controls.Where(c => c is Separator).Count();
                }
            }
            return Vbox;
        }
        public void topCarPositionVisible()
        {   // Allows to find the last carposition, visible at the window bottom.
            if ((!LastRowVisible && LabelTop >= 0) || LabelTop > DisplaySizeY)
            {
                for (int carPosition = 0; carPosition < RowsCount; carPosition++)
                {
                    var labelTop = carPosition * RowHeight;
                    if (labelTop > Vbox.Position.Height - Owner.TextFontDefault.Height)
                    {
                        if (!LastRowVisible && labelTop > 0)
                        {
                            LastRowVisible = true;
                            CarPositionVisible = carPosition - 1;
                            OldPositionHeight = Vbox.Position.Height;
                            break;
                        }
                    }
                }               
            }
        }
        public void localScrollLayout(int selectedCarPosition)
        {
            var trainCarViewer = Owner.Viewer.TrainCarOperationsViewerWindow;

            LocalScrollPosition = 0;

            if (CarPositionVisible > 0 && selectedCarPosition >= CarPositionVisible)// && selectedCarPosition != currentCarPosition)// Not related with CarID
            {
                Client = ControlLayoutScrollboxVertical.NewClient;
                var xcarPosition = CarPositionVisible;

                while (selectedCarPosition != xcarPosition)
                {
                    SetScrollPosition(LocalScrollPosition + RowHeight);
                    xcarPosition++;
                }
                CurrentCarPosition = trainCarViewer.CarPosition;// Not related with CarID
            }
        }
        public int ScrollSize
        {
            get
            {
                return Client.CurrentTop - Vbox.Position.Height;
            }
        }
        public void SetScrollPosition(int position)
        {
            position = Math.Max(0, Math.Min(Math.Max(0, ScrollSize), position));
            Client.MoveBy(0, LocalScrollPosition - position);
            LocalScrollPosition = position;
        }
        void carLabel_Click(Control arg1, Point arg2)
        {
        }
        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            if (updateFull)
            {
                var trainCarViewer = Owner.Viewer.TrainCarOperationsViewerWindow;
                var carOperations = Owner.Viewer.CarOperationsWindow;

                trainCarViewer.TrainCarOperationsChanged = !trainCarViewer.Visible && trainCarViewer.TrainCarOperationsChanged ? false : trainCarViewer.TrainCarOperationsChanged;

                CurrentDisplaySizeY = DisplaySizeY;
                if (Owner.Viewer.DisplaySize.Y != DisplaySizeY || ModifiedSetting || trainCarViewer.CouplerChanged)
                {
                    LastRowVisible = false;
                    SupplyStatusChanged = false;
                    Layout();
                    updateLayoutSize();
                }
                if (OldPositionHeight != Vbox.Position.Height)
                {
                    LastRowVisible = false;
                    topCarPositionVisible();
                    localScrollLayout(SelectedCarPosition);
                }

                UserCommand ? controlDiesel = GetPressedKey(UserCommand.ControlDieselHelper, UserCommand.ControlDieselPlayer, UserCommand.ControlInitializeBrakes);
                if (controlDiesel == UserCommand.ControlDieselHelper || controlDiesel == UserCommand.ControlDieselPlayer || controlDiesel == UserCommand.ControlInitializeBrakes)
                {
                    Layout();
                    PowerSupplyStatus = Owner.Viewer.PlayerTrain.Cars[CarPosition].GetStatus();
                    ModifiedSetting = true;
                }

                var carsCountChanged = Owner.Viewer.PlayerTrain.Cars.Count != LastPlayerTrainCars;
                if (PlayerTrain != Owner.Viewer.PlayerTrain || carsCountChanged || (Owner.Viewer.PlayerLocomotive != null &&
                    LastPlayerLocomotiveFlippedState != Owner.Viewer.PlayerLocomotive.Flipped))
                {
                    PlayerTrain = Owner.Viewer.PlayerTrain;
                    if (LastPlayerTrainCars != Owner.Viewer.PlayerTrain.Cars.Count || !LayoutUpdated)
                    {
                        LayoutUpdated = true;
                        Layout();
                        localScrollLayout(SelectedCarPosition);
                        updateLayoutSize();
                        ModifiedSetting = carsCountChanged;
                    }

                    LastPlayerTrainCars = Owner.Viewer.PlayerTrain.Cars.Count;
                    if (Owner.Viewer.PlayerLocomotive != null) LastPlayerLocomotiveFlippedState = Owner.Viewer.PlayerLocomotive.Flipped;
                }
                // Updates power supply status
                else if (SelectedCarPosition <= CarPositionVisible && SelectedCarPosition == CarPosition)
                {
                    var powerSupplyStatusChanged = PowerSupplyStatus != null && PowerSupplyStatus != Owner.Viewer.PlayerTrain.Cars[CarPosition].GetStatus();
                    var batteyStatusChanged = BatteryStatus != null && BatteryStatus != Owner.Viewer.PlayerTrain.Cars[CarPosition].GetStatus();
                    var circuitBreakerStateChanged = CircuitBreakerState != null && CircuitBreakerState != (Owner.Viewer.PlayerTrain.Cars[CarPosition] as MSTSElectricLocomotive).ElectricPowerSupply.CircuitBreaker.State.ToString();

                    if (powerSupplyStatusChanged || batteyStatusChanged || circuitBreakerStateChanged)
                    {
                        var Status = Owner.Viewer.PlayerTrain.Cars[CarPosition].GetStatus();
                        if (Status != null && Status != PowerSupplyStatus)
                        {
                            PowerSupplyStatus = Status;
                            Layout();
                        }
                    }
                }
                if (trainCarViewer.TrainCarOperationsChanged || trainCarViewer.TrainCarOperationsChanged || trainCarViewer.RearBrakeHoseChanged
                    || trainCarViewer.FrontBrakeHoseChanged || ModifiedSetting || CarIdClicked || carOperations.CarOperationChanged)
                {
                    Layout();
                    localScrollLayout(SelectedCarPosition);
                    updateLayoutSize();
                    ModifiedSetting = false;
                    CarIdClicked = false;
                    // Avoids bug
                    trainCarViewer.TrainCarOperationsChanged = WarningEnabled;
                    carOperations.CarOperationChanged = carOperations.Visible && carOperations.CarOperationChanged;
                }

                if (CarPosition != trainCarViewer.CarPosition)
                {
                    // Required to scroll the main window from the web version
                    UpdateTrainCarOperation = true;
                    CarPosition = trainCarViewer.CarPosition;
                    LabelTop = LabelPositionTop[SelectedCarPosition];
                    Layout();
                    localScrollLayout(SelectedCarPosition);
                }
                //Resize this window after the font has been changed externally
                else if (MultiPlayerWindow.FontChanged)
                {
                    MultiPlayerWindow.FontChanged = false;
                    FontToBold = !FontToBold;
                    UpdateWindowSize();
                }
            }
        }
        public void updateWarningCarPosition(int carPosition, Texture2D texture, Texture2D symbolSet)
        {
            var trainCarOperations = Owner.Viewer.TrainCarOperationsWindow;
            if (!trainCarOperations.WarningCarPosition[carPosition])
            {
                trainCarOperations.WarningCarPosition[carPosition] = texture == symbolSet;
            }
        }
        private static UserCommand? GetPressedKey(params UserCommand[] keysToTest) => keysToTest
            .Where((UserCommand key) => UserInput.IsDown(key))
            .FirstOrDefault();
    }
    class buttonArrowRight : Image
    {
        readonly Viewer Viewer;
        readonly TrainCarOperationsViewerWindow TrainCarViewer;
        readonly int CarPosition;
        public buttonArrowRight(int x, int y, int size, Viewer viewer, int carPosition)
            : base(x, y, size, size)
        {
            Viewer = viewer;
            TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
            CarPosition = carPosition;

            // Coupler changed requires to modify the arrow position
            var trainCarViewerCarPosition = TrainCarViewer.CouplerChanged ? TrainCarViewer.NewCarPosition : TrainCarViewer.CarPosition;
            if (TrainCarViewer.CouplerChanged && trainCarViewerCarPosition == CarPosition)
            {
                Texture = TrainCarViewer.Visible ? Viewer.TrainCarOperationsWindow.AllSymbolsMode ? ArrowRight : ArrowLeft : Empty;
                Viewer.TrainCarOperationsWindow.CarIdClicked = true;
                TrainCarViewer.CarPosition = trainCarViewerCarPosition;
            }
            else
                Texture = TrainCarViewer.Visible && trainCarViewerCarPosition == CarPosition? Viewer.TrainCarOperationsWindow.AllSymbolsMode ? ArrowRight : ArrowLeft : Empty;
            
            Source = new Rectangle(0, 0, size, size);
            Click += new Action<Control, Point>(buttonArrowRight_Click);
        }
        void buttonArrowRight_Click(Control arg1, Point arg2)
        {
            Viewer.TrainCarOperationsWindow.AllSymbolsMode = !Viewer.TrainCarOperationsWindow.AllSymbolsMode;
            if (TrainCarViewer.Visible && TrainCarViewer.CarPosition == CarPosition)
            {
                Texture = Viewer.TrainCarOperationsWindow.AllSymbolsMode ? ArrowRight : ArrowLeft;
                Viewer.TrainCarOperationsWindow.ModifiedSetting = true;
            }
        }
    }
    class buttonArrowLeft : Image
    {
        readonly Viewer Viewer;
        readonly TrainCarOperationsViewerWindow TrainCarViewer;
        readonly int CarPosition;
        public buttonArrowLeft(int x, int y, int size, Viewer viewer, int carPosition)
            : base(x, y, size, size)
        {
            Viewer = viewer;
            TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
            CarPosition = carPosition;
            Texture = Empty;
            // Coupler changed requires to modify arrow position
            var trainCarViewerCarPosition = TrainCarViewer.CouplerChanged ? TrainCarViewer.NewCarPosition : TrainCarViewer.CarPosition;
            if (TrainCarViewer.CouplerChanged && trainCarViewerCarPosition == CarPosition)
            {
                Texture = trainCarViewerCarPosition == CarPosition ? ArrowLeft : Empty;
                Viewer.TrainCarOperationsWindow.CarIdClicked = true;
                TrainCarViewer.CarPosition = trainCarViewerCarPosition;
            }
            else
                Texture = trainCarViewerCarPosition == CarPosition ? ArrowLeft : Empty;

            Source = new Rectangle(0, 0, size, size);
            Click += new Action<Control, Point>(buttonArrowLeft_Click);
        }
        void buttonArrowLeft_Click(Control arg1, Point arg2)
        {
            FontChanged = true;
            FontToBold = !FontToBold;
            Viewer.TrainCarOperationsWindow.ModifiedSetting = true;
        }
    }
    class buttonCouplerFront : Image
    {
        readonly Viewer Viewer;
        readonly bool First;
        public buttonCouplerFront(int x, int y, int size, Viewer viewer, TrainCar car)
            : base(x, y, size, size)
        {
            Viewer = viewer;
            First = car == Viewer.PlayerTrain.Cars.First();
            Texture = First ? CouplerFront : Coupler;
            Source = new Rectangle(0, 0, size, size);
        }
    }
    class buttonCouplerRear : Image
    {
        readonly Viewer Viewer;
        readonly bool Last;
        public buttonCouplerRear(int x, int y, int size, Viewer viewer, TrainCar car)
            : base(x, y, size, size)
        {
            Viewer = viewer;
            Last = car == Viewer.PlayerTrain.Cars.Last();
            Texture = Last ? CouplerRear : Coupler;
            Source = new Rectangle(0, 0, size, size);
        }
    }
    class buttonLoco : Label
    {
        readonly Viewer Viewer;
        readonly CarOperationsWindow CarOperations;
        readonly TrainCarOperationsWindow TrainCar;
        readonly TrainCarOperationsViewerWindow TrainCarViewer;
        readonly int CarPosition;
        public buttonLoco(int x, int y, Viewer viewer, TrainCar car, int carPosition, LabelAlignment alignment)
            : base(x, y, "", alignment)
        {
            Viewer = viewer;
            CarOperations = Viewer.CarOperationsWindow;
            TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
            TrainCar = Viewer.TrainCarOperationsWindow;
            CarPosition = carPosition;
            Text = car.CarID;
            Click += new Action<Control, Point>(buttonLabel_Click);
        }

        public void buttonLabel_Click(Control arg1, Point arg2)
        {
            Control control = arg1;

            TrainCar.CarLabelText = Text;
            TrainCarViewer.CarPosition = CarPosition;
            TrainCarViewer.Visible = true;
            TrainCar.CarIdClicked = true;

            // required by localScrollLayout()
            TrainCar.SelectedCarPosition = CarPosition;
            TrainCar.LabelTop = control.Position.Top;

            //Sync CarOperation
            if (CarOperations.Visible)
                CarOperations.CarPosition = CarPosition;
        }
    }
    class buttonHandBrake : Image
    {
        readonly Viewer Viewer;
        public buttonHandBrake(int x, int y, int size, Viewer viewer, int carPosition)
            : base(x, y, size, size)
        {
            Viewer = viewer;
            Texture = (viewer.PlayerTrain.Cars[carPosition] as MSTSWagon).HandBrakePresent ? (viewer.PlayerTrain.Cars[carPosition] as MSTSWagon).GetTrainHandbrakeStatus() ? HandBrakeSet : HandBrakeNotSet : HandBrakeNotAvailable;
            Source = new Rectangle(0, 0, size, size);

            var trainCarOperations = Viewer.TrainCarOperationsWindow;
            if (!trainCarOperations.WarningCarPosition[carPosition])
            {
                trainCarOperations.updateWarningCarPosition(carPosition, Texture, HandBrakeSet);
            }
        }
    }
    class buttonFrontBrakeHose : Image
    {
        readonly Viewer Viewer;
        readonly TrainCarOperationsViewerWindow TrainCarViewer;
        readonly CarOperationsWindow CarOperations;
        readonly int CarPosition;
        readonly bool First;
        public buttonFrontBrakeHose(int x, int y, int size, Viewer viewer, TrainCar car, int carPosition)
            : base(x, y, size, size)
        {
            Viewer = viewer;
            TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
            CarOperations = Viewer.CarOperationsWindow;

            CarPosition = carPosition;
            First = car == viewer.PlayerTrain.Cars.First();
            Texture = First ? BrakeHoseFirstDis : (viewer.PlayerTrain.Cars[carPosition] as MSTSWagon).BrakeSystem.FrontBrakeHoseConnected ? BrakeHoseCon : BrakeHoseDis;

            // Allows compatibility with CarOperationWindow
            var brakeHoseChanged = CarOperations.FrontBrakeHoseChanged || CarOperations.RearBrakeHoseChanged;
            if (brakeHoseChanged && !TrainCarViewer.Visible && CarOperations.Visible && CarOperations.CarPosition >= 1 && CarOperations.CarPosition == CarPosition)
            {
                var rearBrakeHose = CarOperations.RearBrakeHoseChanged;
                if (rearBrakeHose)
                {
                    new WagonBrakeHoseRearConnectCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarOperations.CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarOperations.CarPosition] as MSTSWagon).BrakeSystem.RearBrakeHoseConnected);
                    CarOperations.RearBrakeHoseChanged = false;
                }
                else
                {
                    new WagonBrakeHoseRearConnectCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarOperations.CarPosition - 1] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarOperations.CarPosition - 1] as MSTSWagon).BrakeSystem.RearBrakeHoseConnected);
                    CarOperations.FrontBrakeHoseChanged = false;
                }
                Viewer.TrainCarOperationsWindow.ModifiedSetting = true;
                TrainCarViewer.TrainCarOperationsChanged = true;
            }

            // Updates from train operations viewer
            var viewerCarposition = TrainCarViewer.CarPosition;
            if (TrainCarViewer.Visible && TrainCarViewer.FrontBrakeHoseChanged && viewerCarposition >= 1)
            {
                new WagonBrakeHoseRearConnectCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[viewerCarposition - 1] as MSTSWagon), !(Viewer.PlayerTrain.Cars[viewerCarposition - 1] as MSTSWagon).BrakeSystem.RearBrakeHoseConnected);
                TrainCarViewer.FrontBrakeHoseChanged = false;
            }
            Source = new Rectangle(0, 0, size, size);

            var trainCarOperations = Viewer.TrainCarOperationsWindow;
            if (!First && !trainCarOperations.WarningCarPosition[carPosition])
            {
                trainCarOperations.updateWarningCarPosition(carPosition, Texture, BrakeHoseDis);
            }
        }
    }
    class buttonRearBrakeHose : Image
    {
        readonly Viewer Viewer;
        readonly TrainCarOperationsViewerWindow TrainCarViewer;
        readonly bool Last;
        public buttonRearBrakeHose(int x, int y, int size, Viewer viewer, TrainCar car, int carPosition)
            : base(x, y, size, size)
        {
            Viewer = viewer;
            TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
            Last = car == viewer.PlayerTrain.Cars.Last();
            Texture = Last ? BrakeHoseLastDis : (viewer.PlayerTrain.Cars[carPosition] as MSTSWagon).BrakeSystem.RearBrakeHoseConnected ? BrakeHoseCon : BrakeHoseDis;

            // Update from viewer
            var viewerCarposition = TrainCarViewer.CarPosition;
            if (TrainCarViewer.Visible && TrainCarViewer.RearBrakeHoseChanged && viewerCarposition < Viewer.PlayerTrain.Cars.Count - 1)
            {
                new WagonBrakeHoseConnectCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[viewerCarposition + 1] as MSTSWagon), !(Viewer.PlayerTrain.Cars[viewerCarposition + 1] as MSTSWagon).BrakeSystem.FrontBrakeHoseConnected);
                TrainCarViewer.RearBrakeHoseChanged = false;
            }
            Source = new Rectangle(0, 0, size, size);

            var trainCarOperations = Viewer.TrainCarOperationsWindow;
            if (!Last && !trainCarOperations.WarningCarPosition[carPosition])
            {
                trainCarOperations.updateWarningCarPosition(carPosition, Texture, BrakeHoseDis);
            }
        }
    }
    class buttonFrontAngleCock : Image
    {
        readonly Viewer Viewer;
        readonly TrainCarOperationsViewerWindow TrainCarViewer;
        readonly bool First;
        public buttonFrontAngleCock(int x, int y, int size, Viewer viewer, TrainCar car, int carPosition)
            : base(x, y, size, size)
        {
            Viewer = viewer;
            TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
            First = car == viewer.PlayerTrain.Cars.First();
            Texture = !TrainCarViewer.TrainCarOperationsChanged && First ? FrontAngleCockClosed : (viewer.PlayerTrain.Cars[carPosition] as MSTSWagon).BrakeSystem.AngleCockAOpen ? FrontAngleCockOpened : FrontAngleCockClosed;
            Source = new Rectangle(0, 0, size, size);

            var trainCarOperations = Viewer.TrainCarOperationsWindow;
            if (!First && !trainCarOperations.WarningCarPosition[carPosition])
            {
                trainCarOperations.updateWarningCarPosition(carPosition, Texture, FrontAngleCockClosed);
            }
        }
    }
    class buttonRearAngleCock : Image
    {
        readonly Viewer Viewer;
        readonly bool Last;
        public buttonRearAngleCock(int x, int y, int size, Viewer viewer, TrainCar car, int carPosition)
            : base(x, y, size, size)
        {
            Viewer = viewer;
            Last = car == viewer.PlayerTrain.Cars.Last();
            Texture = Last ? RearAngleCockClosed : (viewer.PlayerTrain.Cars[carPosition] as MSTSWagon).BrakeSystem.AngleCockBOpen ? RearAngleCockOpened : RearAngleCockClosed;
            Source = new Rectangle(0, 0, size, size);

            var trainCarOperations = Viewer.TrainCarOperationsWindow;
            if (!Last && !trainCarOperations.WarningCarPosition[carPosition])
            {
                trainCarOperations.updateWarningCarPosition(carPosition, Texture, RearAngleCockClosed);
            }
        }
    }
    class buttonBleedOffValve : Image
    {
        readonly Viewer Viewer;
        readonly int CarPosition;

        public buttonBleedOffValve(int x, int y, int size, Viewer viewer, int carPosition)
            : base(x, y, size, size)
        {
            Viewer = viewer;
            CarPosition = carPosition;
            var carOperationsPosition = Viewer.CarOperationsWindow.CarPosition;
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem is SingleTransferPipe
                    || Viewer.PlayerTrain.Cars.Count() == 1)
            {
                Texture = BleedOffValveNotAvailable;
            }
            else
            {
                if (!Viewer.TrainCarOperationsViewerWindow.Visible && Viewer.CarOperationsWindow.Visible && CarPosition == carOperationsPosition)
                    Texture = (viewer.PlayerTrain.Cars[carOperationsPosition] as MSTSWagon).BrakeSystem.BleedOffValveOpen ? BleedOffValveOpened : BleedOffValveClosed;
                else
                    Texture = (viewer.PlayerTrain.Cars[carPosition] as MSTSWagon).BrakeSystem.BleedOffValveOpen ? BleedOffValveOpened : BleedOffValveClosed;
            }
            Source = new Rectangle(0, 0, size, size);

            var trainCarOperations = Viewer.TrainCarOperationsWindow;
            if (!trainCarOperations.WarningCarPosition[carPosition])
            {
                trainCarOperations.updateWarningCarPosition(carPosition, Texture, BleedOffValveOpened);
            }
        }
    }
    class buttonTogglePower : Image
    {
        readonly Viewer Viewer;
        readonly TrainCarOperationsWindow TrainCarOperations;
        readonly TrainCarOperationsViewerWindow TrainCarViewer;
        readonly int CarPosition;

        public buttonTogglePower(int x, int y, int size, Viewer viewer, int carPosition)
            : base(x, y, size, size)
        {
            Viewer = viewer;
            TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
            TrainCarOperations = Viewer.TrainCarOperationsWindow;
            CarPosition = carPosition;

            if ((Viewer.PlayerTrain.Cars[CarPosition] is MSTSElectricLocomotive)
                || (Viewer.PlayerTrain.Cars[CarPosition] is MSTSDieselLocomotive))
            {
                Texture = LocomotiveStatus(CarPosition);
                if (CarPosition == TrainCarViewer.CarPosition)
                {
                    MSTSLocomotive locomotive = Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive;
                    TrainCarOperations.MainPowerSupplyOn = locomotive.LocomotivePowerSupply.MainPowerSupplyOn;
                }
            }
            else
                Texture = Empty;

            Source = new Rectangle(0, 0, size, size);
        }
        public Texture2D LocomotiveStatus(int CarPosition)
        {
            string locomotiveStatus = Viewer.PlayerTrain.Cars[CarPosition].GetStatus();
            foreach (string data in locomotiveStatus.Split('\n').Where((string d) => !string.IsNullOrWhiteSpace(d)))
            {
                string[] parts = data.Split(new string[] { " = " }, 2, StringSplitOptions.None);
                string keyPart = parts[0];
                string valuePart = parts?[1];
                if (keyPart.Contains(Viewer.Catalog.GetString("Engine")))
                {
                    TrainCarOperations.PowerSupplyStatus = locomotiveStatus;

                    Texture = valuePart.Contains(Viewer.Catalog.GetString("Running")) ? PowerOn
                       : valuePart.Contains(Viewer.Catalog.GetString("Stopped")) ? PowerOff
                       : PowerChanging;

                    if (CarPosition == TrainCarViewer.CarPosition)
                    {
                        TrainCarOperations.PowerSupplyUpdating = Texture == PowerChanging;
                    }
                    break;
                }
                else if (keyPart.Contains(Viewer.Catalog.GetString("Power")))
                {
                    TrainCarViewer.PowerSupplyStatus = locomotiveStatus;
                    var powerStatus = valuePart.Contains(Viewer.Catalog.GetString("On"));
                    Texture = powerStatus ? PowerOn : PowerOff;
                    if (CarPosition == TrainCarViewer.CarPosition)
                        TrainCarOperations.SupplyStatusChanged = TrainCarOperations.MainPowerSupplyOn != powerStatus;

                    break;
                }
            }
            return Texture;
        }
    }
    class buttonToggleMU : Image
    {
        readonly Viewer Viewer;
        readonly int CarPosition;

        public buttonToggleMU(int x, int y, int size, Viewer viewer, int carPosition)
            : base(x, y, size, size)
        {
            Viewer = viewer;
            CarPosition = carPosition;

            var multipleUnitsConfiguration = Viewer.PlayerLocomotive.GetMultipleUnitsConfiguration();
            if ((Viewer.PlayerTrain.Cars[CarPosition] is MSTSDieselLocomotive) && multipleUnitsConfiguration != null)
            {
                Texture = (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).RemoteControlGroup == 0 && multipleUnitsConfiguration != "1" ? MUconnected : MUdisconnected;
            }
            else
            {
                Texture = Empty;
            }
            Source = new Rectangle(0, 0, size, size);
        }
    }
    class buttonToggleElectricTrainSupplyCable : Image
    {
        readonly Viewer Viewer;
        readonly int CarPosition;

        public buttonToggleElectricTrainSupplyCable(int x, int y, int size, Viewer viewer, int carPosition)
            : base(x, y, size, size)
        {
            Viewer = viewer;
            CarPosition = carPosition;

            MSTSWagon wagon = Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon;

            if (wagon.PowerSupply != null && Viewer.PlayerTrain.Cars.Count() > 1)
            {
                Texture = wagon.PowerSupply.FrontElectricTrainSupplyCableConnected ? ETSconnected : ETSdisconnected;
            }
            else
            {
                Texture = Empty;
            }
            Source = new Rectangle(0, 0, size, size);
        }
    }
    class buttonToggleBatterySwitch : Image
    {
        readonly Viewer Viewer;
        readonly int CarPosition;

        public buttonToggleBatterySwitch(int x, int y, int size, Viewer viewer, int carPosition)
            : base(x, y, size, size)
        {
            Viewer = viewer;
            CarPosition = carPosition;

            if (Viewer.PlayerTrain.Cars[CarPosition] is MSTSWagon wagon
                && wagon.PowerSupply is IPowerSupply)
            {
                if (wagon.PowerSupply.BatterySwitch.Mode == BatterySwitch.ModeType.AlwaysOn)
                {
                    Texture = BattAlwaysOn;
                }
                else
                {
                    Texture = locomotiveStatus(CarPosition);
                }
            }
            else
            {
                Texture = Empty;
            }
            Source = new Rectangle(0, 0, size, size);
        }
        public Texture2D locomotiveStatus(int CarPosition)
        {
            string locomotiveStatus = Viewer.PlayerTrain.Cars[CarPosition].GetStatus();
            foreach (string data in locomotiveStatus.Split('\n').Where((string d) => !string.IsNullOrWhiteSpace(d)))
            {
                string[] parts = data.Split(new string[] { " = " }, 2, StringSplitOptions.None);
                string keyPart = parts[0];
                string valuePart = parts?[1];
                if (keyPart.Contains(Viewer.Catalog.GetString("Battery")))
                {
                    Viewer.TrainCarOperationsWindow.BatteryStatus = locomotiveStatus;
                    Texture = valuePart.Contains(Viewer.Catalog.GetString("On")) ? BattOn : BattOff;
                    break;
                }
            }
            return Texture;
        }
    }
}
