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
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Orts.Viewer3D.Popups.TrainCarOperationsWindow;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using Orts.Viewer3D.RollingStock;
using Orts.MultiPlayer;
using Orts.Viewer3D;
using System.Diagnostics;
using ORTS.Scripting.Api;

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
        internal static Texture2D BrakeHoseFirstCon;
        internal static Texture2D BrakeHoseRearDis;
        internal static Texture2D BrakeHoseRearCon;
        internal static Texture2D Coupler;
        internal static Texture2D CouplerFront;
        internal static Texture2D CouplerNotAvailable;
        internal static Texture2D CouplerRear;
        internal static Texture2D Empty;
        internal static Texture2D ETSconnected;
        internal static Texture2D ETSdisconnected;
        internal static Texture2D FrontAngleCockClosed;
        internal static Texture2D FrontAngleCockOpened;
        internal static Texture2D FrontAngleCockPartial;
        internal static Texture2D FrontAngleCockNotAvailable;
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
        internal static Texture2D RearAngleCockPartial;
        internal static Texture2D RearAngleCockNotAvailable;

        public bool AngleCockAPartiallyEnabled;
        public bool AngleCockBPartiallyEnabled;
        public bool AllSymbolsMode = true;
        public int DisplaySizeY;
        public bool LayoutUpdated;
        public int LocoRowCount;
        public int RowsCount;
        public int SeparatorCount;
        public int SpacerRowCount;
        public int SymbolsRowCount;

        public ControlLayout Client;
        public bool CarPositionChanged;
        public int CarPositionVisible;
        public int CurrentCarPosition;
        public int LabelTop;
        public bool LastRowVisible;
        public int LocalScrollPosition;
        public int SelectedCarPosition = 0;
        const int SymbolSize = 16;
        public bool UpdateFlipped;
        public bool FrontActive;
        public bool BackActive;

        public bool BackCameraActivated;
        public bool CabCameraEnabled;
        public int CurrentDisplaySizeY;
        public bool FrontCameraActivated;
        public bool IsFullScreen;
        public int OldPositionHeight;
        public int RowHeight;
        public Rectangle LayoutLocation;
        public Rectangle OldLocation;
        public bool LayoutMoved;
        public bool UpdateTrainCarOperation;
        public int WindowHeightMax;
        public int WindowHeightMin;
        public int WindowWidthMax;
        public int WindowWidthMin;
        public List<int> LabelPositionTop = new List<int>();

        //Rectangle carLabelPosition;
        public ControlLayoutVertical Vbox;
        public string CarLabelText;
        public int CarPosition;
        public int CarUIDLenght;
        public int DesiredHeight;
        public static bool FontChanged;
        public static bool FontToBold;
        public string LastCarIDSelected;// Required when reversal
        public int OldCarPosition;
        public bool IsLocoAtFront;
        public bool CouplerClicked;

        //Electrical power
        public bool BatterySwitchOn;
        public PowerSupplyState PowerSupplyStatus;

        public bool CarIdClicked;
        public bool WarningEnabled;
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
        public bool LastPlayerLocomotiveFlippedState;

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

            // rwf-rr: temporary fix for bug 2121985
            if (SelectedCarPosition >= Owner.Viewer.PlayerTrain.Cars.Count)
            {
                Trace.TraceWarning("TrainCarOperationsWindow.SelectedCarPosition {0} out of range [0..{1}]", SelectedCarPosition, Owner.Viewer.PlayerTrain.Cars.Count - 1);
                SelectedCarPosition = Owner.Viewer.PlayerTrain.Cars.Count - 1;
            }
            outf.Write(SelectedCarPosition);
            outf.Write(Owner.Viewer.FrontCamera.IsCameraFront);
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
            Owner.Viewer.FrontCamera.IsCameraFront = inf.ReadBoolean();
            CabCameraEnabled = Owner.Viewer.Camera is CabCamera || Owner.Viewer.Camera == Owner.Viewer.ThreeDimCabCamera;

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
                var GraphicsDeviceRender = Owner.Viewer.RenderProcess.GraphicsDevice;
                var TrainOperationsPath = System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperations\\TrainOperationsMap.png");

                // TO DO: This should happen on the loader thread.
                //                                                                        texture rectangles : X, Y, width, height
                ArrowRight = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(48, 112, 16, 16));
                ArrowLeft = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(48, 64, 16, 16));

                Coupler = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(32, 48, 16, 16));
                CouplerFront = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(16, 48, 16, 16));
                CouplerRear = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(48, 48, 16, 16));
                CouplerNotAvailable = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(0, 48, 16, 16));

                Empty = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(48, 0, 16, 16));

                HandBrakeNotAvailable = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(0, 64, 16, 16));
                HandBrakeNotSet = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(32, 64, 16, 16));
                HandBrakeSet = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(16, 64, 16, 16));

                BrakeHoseCon = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(0, 32, 16, 16));
                BrakeHoseDis = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(16, 32, 16, 16));
                BrakeHoseFirstDis = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(32, 32, 16, 16));
                BrakeHoseRearDis = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(48, 32, 16, 16));
                BrakeHoseFirstCon = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(0, 144, 16, 16));
                BrakeHoseRearCon = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(16, 144, 16, 16));

                FrontAngleCockOpened = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(32, 96, 16, 16));
                FrontAngleCockClosed = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(0, 96, 16, 16));
                FrontAngleCockPartial = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(0, 128, 16, 16));
                FrontAngleCockNotAvailable = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(32, 128, 16, 16));

                BleedOffValveClosed = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(16, 16, 16, 16));
                BleedOffValveOpened = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(32, 16, 16, 16));
                BleedOffValveNotAvailable = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(0, 16, 16, 16));

                RearAngleCockClosed = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(16, 96, 16, 16));
                RearAngleCockOpened = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(48, 96, 16, 16));
                RearAngleCockPartial = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(16, 128, 16, 16));
                RearAngleCockNotAvailable = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(48, 128, 16, 16));

                PowerChanging = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(32, 112, 16, 16));
                PowerOff = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(16, 112, 16, 16));
                PowerOn = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(0, 112, 16, 16));

                MUconnected = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(32, 80, 16, 16));
                MUdisconnected = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(48, 80, 16, 16));

                ETSconnected = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(0, 80, 16, 16));
                ETSdisconnected = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(16, 80, 16, 16));

                BattOff = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(16, 0, 16, 16));
                BattOn = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(32, 0, 16, 16));
                BattAlwaysOn = SharedTextureManager.LoadInternal(GraphicsDeviceRender, TrainOperationsPath, new Rectangle(0, 0, 16, 16));
            }
        }
        private void UpdateWindowSize()
        {
            ModifyWindowSize();
        }
        public void updateLayoutSize()
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

                    // Avoids crash when the PlayerTrain was changed from the Train List window
                    if (LabelPositionTop.Count == 0 || PlayerTrain.Cars.Count != LabelPositionTop.Count)
                    {
                        LabelPositionTop.Clear();
                        var n = scrollbox.Position.Y;// first row
                        for (var i = 0; i < PlayerTrain.Cars.Count; i++)
                        {   // Position of each row
                            LabelPositionTop.Add(n);
                            n += (textHeight + ControlLayout.SeparatorSize);
                        }
                    }

                    // Init LastCarIDSelected
                    if (LastCarIDSelected == null)
                    {
                        LastCarIDSelected = PlayerTrain.Cars[SelectedCarPosition].CarID;
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

                            // Left arrow
                            line.Add(new buttonArrowLeft(0, 0, SymbolSize, Owner.Viewer, carPosition));
                            AddSpace();

                            // Front brake hose
                            line.Add(new buttonFrontBrakeHose(0, 0, SymbolSize, Owner.Viewer, car, carPosition));
                            // Front angle cock
                            line.Add(new buttonFrontAngleCock(0, 0, SymbolSize, Owner.Viewer, car, carPosition));
                            AddSpace();

                            // Front coupler
                            line.Add(new buttonCouplerFront(0, 0, SymbolSize, Owner.Viewer, car, carPosition));
                            // Car label
                            line.Add(carLabel);
                            // Rear coupler
                            line.Add(new buttonCouplerRear(0, 0, SymbolSize, Owner.Viewer, car, carPosition));
                            AddSpace();

                            // Rear angle cock
                            line.Add(new buttonRearAngleCock(0, 0, SymbolSize, Owner.Viewer, car, carPosition));
                            // Rear brake hose
                            line.Add(new buttonRearBrakeHose(0, 0, SymbolSize, Owner.Viewer, car, carPosition));
                            AddSpace();

                            // Handbrake
                            line.Add(new buttonHandBrake(0, 0, SymbolSize, Owner.Viewer, carPosition));
                            AddSpace();

                            // Bleed off valve
                            line.Add(new buttonBleedOffValve(0, 0, SymbolSize, Owner.Viewer, carPosition));
                            AddSpace();

                            if (AllSymbolsMode)//Allows to display all symbols
                            {
                                // Electric train supply connection (ETS)
                                if (PlayerTrain.Cars.Count() > 1 && wagon.PowerSupply != null)
                                {
                                    line.Add(new buttonToggleElectricTrainSupplyCable(0, 0, SymbolSize, Owner.Viewer, carPosition));
                                    AddSpace();
                                }
                                if (isElectricDieselLocomotive)
                                {
                                    line.Add(new buttonToggleMU(0, 0, SymbolSize, Owner.Viewer, carPosition));
                                    AddSpace();

                                    line.Add(new buttonTogglePower(0, 0, SymbolSize, Owner.Viewer, carPosition));
                                    AddSpace();

                                    if ((wagon != null) && (wagon.PowerSupply is IPowerSupply))
                                    {
                                        line.Add(new buttonToggleBatterySwitch(0, 0, SymbolSize, Owner.Viewer, carPosition));
                                        AddSpace();
                                    }
                                }
                            }
                            // Right arrow
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

                    // Allows to resize the window according to the carPosition value.
                    if (RowsCount > carPosition) RowsCount = carPosition;
                    if (SeparatorCount > carPosition - 1) SeparatorCount = carPosition - 1;
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
                OldPositionHeight = Vbox.Position.Height;// optimizes PrepareFrame()
            }
        }
        public void localScrollLayout(int selectedCarPosition)
        {
            var trainCarViewer = Owner.Viewer.TrainCarOperationsViewerWindow;

            LocalScrollPosition = 0;

            if (CarPositionVisible > 0 && selectedCarPosition >= CarPositionVisible)// Not related with CarID
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

            if (UserInput.IsPressed(UserCommand.CameraCarNext) || UserInput.IsPressed(UserCommand.CameraCarPrevious)
                || UserInput.IsPressed(UserCommand.CameraCarFirst) || UserInput.IsPressed(UserCommand.CameraCarLast)
                || UserInput.IsDown(UserCommand.CameraOutsideFront) || UserInput.IsDown(UserCommand.CameraOutsideRear))
                CarPositionChanged = true;

            if (OldLocation != Location)
            {
                OldLocation = Location;
                LayoutMoved = true;
            }

            if (updateFull)
            {
                var trainCarViewer = Owner.Viewer.TrainCarOperationsViewerWindow;
                var carOperations = Owner.Viewer.CarOperationsWindow;
                var trainCarWebpage = Owner.Viewer.TrainCarOperationsWebpage;
                var isFormationReversed = Owner.Viewer.IsFormationReversed;

                CabCameraEnabled = Owner.Viewer.Camera is CabCamera || Owner.Viewer.Camera == Owner.Viewer.ThreeDimCabCamera;
                if (CarIdClicked && !CabCameraEnabled && !trainCarViewer.Visible && (!FrontActive || !BackActive))
                {
                    SetCameraView();
                }

                if (!Owner.Viewer.FirstLoop || Owner.Viewer.IsCameraPositionUpdated)
                {
                    Owner.Viewer.CameraF9Reference = Owner.Viewer.FrontCamera.IsCameraFront;
                    var currentCameraCarID = Owner.Viewer.Camera.AttachedCar.CarID;
                    var currentCameraPosition = 0;
                    if (PlayerTrain != null)
                    {
                        currentCameraPosition = PlayerTrain.Cars.TakeWhile(x => x.CarID != currentCameraCarID).Count();
                    }

                    Owner.Viewer.FirstLoop = true;
                    if (Owner.Viewer.CameraF9Reference)
                    {
                        SelectedCarPosition = SelectedCarPosition == 0 ? Owner.Viewer.CameraOutsideFrontPosition
                            : SelectedCarPosition != 0 ? SelectedCarPosition
                            : currentCameraPosition;
                    }
                    else
                    {
                        SelectedCarPosition = Owner.Viewer.CameraOutsideRearPosition;
                    }
                    CarPositionChanged = true;
                    trainCarViewer.CouplerChanged = false;
                    Owner.Viewer.IsCameraPositionUpdated = false;
                }

                // Allows interaction with <Alt>+<PageDown> and <Alt>+<PageUP>.
                if (CarPositionChanged && Owner.Viewer.Camera.AttachedCar != null && !(Owner.Viewer.Camera is CabCamera) && Owner.Viewer.Camera != Owner.Viewer.ThreeDimCabCamera && (trainCarViewer.Visible || Visible))
                {
                    var currentCameraCarID = Owner.Viewer.Camera.AttachedCar.CarID;
                    if (PlayerTrain != null && (currentCameraCarID != trainCarViewer.CurrentCarID || CarPosition != trainCarViewer.CarPosition))
                    {
                        trainCarViewer.CurrentCarID = LastCarIDSelected = currentCameraCarID;
                        trainCarViewer.CarPosition = CarPosition = PlayerTrain.Cars.TakeWhile(x => x.CarID != currentCameraCarID).Count();
                        SelectedCarPosition = CarPosition;
                        CarPositionChanged = true;
                    }
                }

                trainCarViewer.TrainCarOperationsChanged = !trainCarViewer.Visible && trainCarViewer.TrainCarOperationsChanged ? false : trainCarViewer.TrainCarOperationsChanged;

                CurrentDisplaySizeY = DisplaySizeY;
                if (Owner.Viewer.DisplaySize.Y != DisplaySizeY || ModifiedSetting || trainCarViewer.CouplerChanged)
                {
                    LastRowVisible = false;
                    Layout();
                    updateLayoutSize();

                    // rwf-rr: potential partial fix for bug 2121985
                    // if (trainCarViewer.CouplerChanged && CarPosition >= Owner.Viewer.PlayerTrain.Cars.Count)
                    // {
                    //     SelectedCarPosition = CarPosition = Owner.Viewer.PlayerTrain.Cars.Count - 1;
                    //     LastCarIDSelected = PlayerTrain.Cars[SelectedCarPosition].CarID;
                    // }
                }
                if (OldPositionHeight != Vbox.Position.Height)
                {
                    LastRowVisible = false;
                    topCarPositionVisible();
                    localScrollLayout(SelectedCarPosition);
                }

                // Restore LastCarIDSelected (F9) after returning from different camera views
                if (CarIdClicked && Owner.Viewer.Camera.AttachedCar != null && Owner.Viewer.Camera.AttachedCar.CarID != LastCarIDSelected)
                {
                    trainCarViewer.CurrentCarID = LastCarIDSelected;
                    trainCarViewer.CarPosition = CarPosition = PlayerTrain.Cars.TakeWhile(x => x.CarID != LastCarIDSelected).Count();
                    SelectedCarPosition = CarPosition;
                    trainCarViewer.TrainCarOperationsChanged = true;
                    SetCameraView();
                }

                UserCommand? controlDiesel = GetPressedKey(UserCommand.ControlDieselHelper, UserCommand.ControlDieselPlayer, UserCommand.ControlInitializeBrakes);
                if (controlDiesel == UserCommand.ControlDieselHelper || controlDiesel == UserCommand.ControlDieselPlayer || controlDiesel == UserCommand.ControlInitializeBrakes)
                {
                    Layout();
                    var locomotive = Owner.Viewer.PlayerTrain.Cars[Owner.Viewer.PlayerTrain.Cars.Count > CarPosition ? CarPosition : CarPosition - 1] as MSTSLocomotive;
                    if (locomotive != null) PowerSupplyStatus = locomotive.LocomotivePowerSupply.GetPowerStatus();
                    ModifiedSetting = true;
                }

                var carsCountChanged = Owner.Viewer.PlayerTrain.Cars.Count != LastPlayerTrainCars;
                if (PlayerTrain != Owner.Viewer.PlayerTrain || carsCountChanged || (Owner.Viewer.PlayerLocomotive != null &&
                    LastPlayerLocomotiveFlippedState != isFormationReversed))
                {
                    PlayerTrain = Owner.Viewer.PlayerTrain;
                    if (LastPlayerTrainCars != Owner.Viewer.PlayerTrain.Cars.Count)
                    {
                        Layout();
                        localScrollLayout(SelectedCarPosition);
                        updateLayoutSize();
                        ModifiedSetting = carsCountChanged;
                    }

                    LastPlayerTrainCars = PlayerTrain.Cars.Count;

                    // Checks if the lead locomotive is at the front of the train.
                    var LeadLocoIndex = PlayerTrain.Cars.FindIndex(x => x.CarID == Owner.Viewer.PlayerLocomotive.CarID);
                    var firstCarIdIndex = PlayerTrain.Cars.FindIndex(x => x.CarID == PlayerTrain.Cars[0].CarID);
                    var lastCarIdSelectedIndex = PlayerTrain.Cars.FindIndex(x => x.CarID == LastCarIDSelected);
                    lastCarIdSelectedIndex = lastCarIdSelectedIndex < 0 && CouplerClicked ? 0 : lastCarIdSelectedIndex;

                    IsLocoAtFront = (!IsLocoAtFront && LeadLocoIndex == firstCarIdIndex)
                        || (IsLocoAtFront && LeadLocoIndex <= firstCarIdIndex)
                        || LeadLocoIndex < lastCarIdSelectedIndex;

                    if (lastCarIdSelectedIndex < 0)
                    {// It assigns a valid value to the lastCarIdSelectedIndex variable.
                        var currentCarID = trainCarViewer.CurrentCarID != null ? trainCarViewer.CurrentCarID : PlayerTrain.Cars[LastPlayerTrainCars - 1].CarID;
                        lastCarIdSelectedIndex = PlayerTrain.Cars.FindIndex(x => x.CarID == currentCarID);
                    }

                    SelectedCarPosition = IsLocoAtFront
                        ? CouplerClicked && lastCarIdSelectedIndex != 0 ? LastPlayerTrainCars - 1
                        : lastCarIdSelectedIndex < 0 ? LastPlayerTrainCars - 1 : lastCarIdSelectedIndex
                        : CouplerClicked ? 0 : lastCarIdSelectedIndex;

                    CouplerClicked = false; CarPosition = trainCarViewer.CarPosition = SelectedCarPosition;
                    trainCarViewer.CurrentCarID = PlayerTrain.Cars.Count > CarPosition ? PlayerTrain.Cars[CarPosition].CarID : "";
                    Layout();
                }
                // Updates power supply status
                else if (SelectedCarPosition <= CarPositionVisible && SelectedCarPosition == CarPosition)
                {
                    var carposition = Owner.Viewer.PlayerTrain.Cars.Count > CarPosition ? CarPosition : CarPosition - 1;
                    if (Owner.Viewer.PlayerTrain.Cars[carposition] is MSTSWagon wagon && wagon.PowerSupply != null)
                    {
                        var powerSupplyStatusChanged = wagon is MSTSLocomotive locomotive && PowerSupplyStatus != locomotive.LocomotivePowerSupply.GetPowerStatus();
                        var batteyStatusChanged = wagon.PowerSupply.BatterySwitch.On != BatterySwitchOn;

                        if (powerSupplyStatusChanged || batteyStatusChanged)
                        {
                            if (wagon is MSTSLocomotive) PowerSupplyStatus = (wagon as MSTSLocomotive).LocomotivePowerSupply.GetPowerStatus();
                            BatterySwitchOn = wagon.PowerSupply.BatterySwitch.On;
                            Layout();
                        }
                    }
                }

                if (trainCarViewer.TrainCarOperationsChanged || trainCarViewer.RearBrakeHoseChanged
                    || trainCarViewer.FrontBrakeHoseChanged || ModifiedSetting || CarIdClicked || carOperations.CarOperationChanged)
                {
                    Layout();
                    localScrollLayout(SelectedCarPosition);
                    updateLayoutSize();
                    ModifiedSetting = false;
                    // Avoids bug
                    trainCarViewer.TrainCarOperationsChanged = WarningEnabled;
                    carOperations.CarOperationChanged = carOperations.Visible && carOperations.CarOperationChanged;
                }

                if ((!trainCarViewer.Visible || trainCarViewer.UpdateTCOLayout) && (CarIdClicked || (LastPlayerLocomotiveFlippedState != isFormationReversed)))
                {   // Apply the reveral point to layout
                    _ = new FormationReversed(Owner.Viewer, PlayerTrain);
                }

                if (trainCarViewer.TrainCarOperationsChanged || trainCarViewer.RearBrakeHoseChanged
                    || trainCarViewer.FrontBrakeHoseChanged || ModifiedSetting || CarIdClicked || carOperations.CarOperationChanged)
                {
                    Layout();
                    localScrollLayout(SelectedCarPosition);
                    updateLayoutSize();
                    ModifiedSetting = false;
                    // Avoids bug
                    trainCarViewer.TrainCarOperationsChanged = WarningEnabled;
                    carOperations.CarOperationChanged = carOperations.Visible && carOperations.CarOperationChanged;
                    CarIdClicked = false;
                }

                if (CarPositionChanged || (trainCarWebpage != null && CarPosition != trainCarViewer.CarPosition && trainCarWebpage.Connections > 0))
                {
                    // Required to scroll the main window from the web version
                    UpdateTrainCarOperation = true;
                    CarPosition = PlayerTrain.Cars.Count > trainCarViewer.CarPosition ? trainCarViewer.CarPosition : trainCarViewer.CarPosition - 1;
                    SelectedCarPosition = CarPositionChanged ? CarPosition : Owner.Viewer.PlayerTrain.Cars.Count > SelectedCarPosition ? SelectedCarPosition : CarPosition;
                    LabelTop = LabelPositionTop[SelectedCarPosition];
                    Layout();
                    localScrollLayout(SelectedCarPosition);
                    CarPositionChanged = false;
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

        public void SetCameraView()
        {
            if (Owner.Viewer.FrontCamera.AttachedCar != null)
            {
                Owner.Viewer.FrontCamera.Activate();
                BackActive = false;
                FrontActive = true;
            }
            else if (Owner.Viewer.BackCamera.AttachedCar != null)
            {
                Owner.Viewer.BackCamera.Activate();
                BackActive = true;
                FrontActive = false;
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

    class FormationReversed
    {
        readonly Viewer Viewer;
        readonly TrainCarOperationsWindow TrainCar;
        readonly TrainCarOperationsViewerWindow TrainCarViewer;
        public FormationReversed(Viewer viewer, Train PlayerTrain)
        {
            Viewer = viewer;
            TrainCar = Viewer.TrainCarOperationsWindow;
            TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
            var currentCameraCarID = Viewer.Camera.AttachedCar != null ? Viewer.Camera.AttachedCar.CarID : TrainCar.LastCarIDSelected;

            TrainCarViewer.CurrentCarID = TrainCar.LastCarIDSelected;
            TrainCarViewer.CarPosition = TrainCar.CarPosition = PlayerTrain.Cars.TakeWhile(x => x.CarID != TrainCar.LastCarIDSelected).Count();

            if (TrainCar.CabCameraEnabled)// Displays camera 1
            {  // Setting the camera view
                TrainCar.CabCameraEnabled = false;
            }
            else if (TrainCar.OldCarPosition != TrainCar.SelectedCarPosition || (TrainCar.CarIdClicked && TrainCar.CarPosition == 0))
            {
                TrainCar.SetCameraView();
                TrainCar.OldCarPosition = TrainCar.SelectedCarPosition;
            }

            if (PlayerTrain.Cars.Count > TrainCar.CarPosition)
            {
                TrainCarViewer.CarPosition = TrainCar.SelectedCarPosition = TrainCar.CarPosition = PlayerTrain.Cars.TakeWhile(x => x.CarID != TrainCar.LastCarIDSelected).Count();
            }
            else
            {
                TrainCarViewer.CarPosition = TrainCar.SelectedCarPosition = TrainCar.CarPosition = 0;
                TrainCarViewer.CurrentCarID = PlayerTrain.Cars[0].CarID;
            }

            // Scroll LabelTop
            TrainCar.LabelTop = TrainCar.LabelPositionTop[TrainCar.SelectedCarPosition];
            Viewer.FrontCamera.IsCameraFront = Viewer.FrontCamera.AttachedCar != null;
            TrainCar.Layout();
            // Calculates the top car position visible
            TrainCar.LastRowVisible = false;
            TrainCar.topCarPositionVisible();
            TrainCar.localScrollLayout(TrainCar.SelectedCarPosition);
            TrainCar.updateLayoutSize();

            // Reset
            Viewer.IsFormationReversed = false;
            TrainCar.LastPlayerLocomotiveFlippedState = Viewer.IsFormationReversed;
            TrainCar.CarIdClicked = false;
            TrainCarViewer.UpdateTCOLayout = false;
        }
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
        readonly bool DisableCouplers;
        public buttonCouplerFront(int x, int y, int size, Viewer viewer, TrainCar car, int carPosition)
            : base(x, y, size, size)
        {
            Viewer = viewer;
            First = car == Viewer.PlayerTrain.Cars.First();
            var CurrentCar = Viewer.PlayerTrain.Cars[carPosition]; ;

            var isSteamAndHasTender = false;
            if (CurrentCar is MSTSSteamLocomotive)
            {
                var validTenderPosition = CurrentCar.Flipped ? carPosition - 1 > -1 : carPosition + 1 < Viewer.PlayerTrain.Cars.Count;
                isSteamAndHasTender = validTenderPosition && (Viewer.PlayerTrain.Cars[carPosition + (CurrentCar.Flipped ? -1 : 1)].WagonType == MSTSWagon.WagonTypes.Tender);
            }
            var isTender = CurrentCar.WagonType == MSTSWagon.WagonTypes.Tender;

            if (isSteamAndHasTender || isTender)
            {
                var carFlipped = CurrentCar.Flipped;
                DisableCouplers = isSteamAndHasTender ? carFlipped : !carFlipped;
            }
            Texture = First ? CouplerFront : DisableCouplers ? CouplerNotAvailable : Coupler;
            Source = new Rectangle(0, 0, size, size);
        }
    }
    class buttonCouplerRear : Image
    {
        readonly Viewer Viewer;
        readonly bool Last;
        readonly bool DisableCouplers;
        public buttonCouplerRear(int x, int y, int size, Viewer viewer, TrainCar car, int carPosition)
            : base(x, y, size, size)
        {
            Viewer = viewer;
            Last = car == Viewer.PlayerTrain.Cars.Last();
            var CurrentCar = Viewer.PlayerTrain.Cars[carPosition];

            var isSteamAndHasTender = (CurrentCar is MSTSSteamLocomotive) &&
                (carPosition + 1 < Viewer.PlayerTrain.Cars.Count) && (Viewer.PlayerTrain.Cars[carPosition + 1].WagonType == MSTSWagon.WagonTypes.Tender);
            var isTender = CurrentCar.WagonType == MSTSWagon.WagonTypes.Tender;
            if (isSteamAndHasTender || isTender)
            {
                var carFlipped = CurrentCar.Flipped;
                DisableCouplers = isSteamAndHasTender ? !carFlipped : carFlipped;
            }
            Texture = Last ? CouplerRear : DisableCouplers ? CouplerNotAvailable : Coupler;
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

            TrainCar.CarLabelText = TrainCarViewer.CurrentCarID = TrainCar.LastCarIDSelected = Text;
            TrainCarViewer.CarPosition = TrainCar.SelectedCarPosition = Viewer.PlayerTrain.Cars.TakeWhile(x => x.CarID != Text).Count();
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
            Texture = (viewer.PlayerTrain.Cars[carPosition] as MSTSWagon).MSTSBrakeSystem.HandBrakePresent ? (viewer.PlayerTrain.Cars[carPosition] as MSTSWagon).GetTrainHandbrakeStatus() ? HandBrakeSet : HandBrakeNotSet : HandBrakeNotAvailable;
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
            var CurrentCar = Viewer.PlayerTrain.Cars[carPosition];
            CarPosition = carPosition;
            First = car == viewer.PlayerTrain.Cars.First();
            Texture = First ? BrakeHoseFirstDis : (CurrentCar as MSTSWagon).BrakeSystem.FrontBrakeHoseConnected ? BrakeHoseCon : BrakeHoseDis;

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

        public buttonRearBrakeHose(int x, int y, int size, Viewer viewer, TrainCar car, int carPosition)
            : base(x, y, size, size)
        {
            Viewer = viewer;
            TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
            var Last = car == viewer.PlayerTrain.Cars.Last();
            Texture = Last ? BrakeHoseRearDis : (viewer.PlayerTrain.Cars[carPosition] as MSTSWagon).BrakeSystem.RearBrakeHoseConnected ? BrakeHoseCon : BrakeHoseDis;

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
        public buttonFrontAngleCock(int x, int y, int size, Viewer viewer, TrainCar car, int carPosition)
            : base(x, y, size, size)
        {
            Viewer = viewer;
            TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
            var CurrentCar = Viewer.PlayerTrain.Cars[carPosition];
            var First = car == viewer.PlayerTrain.Cars.First();

            if (CurrentCar.BrakeSystem is VacuumSinglePipe)
            {
                Texture = FrontAngleCockNotAvailable;
            }
            else
            {
                var carAngleCockAOpenAmount = (CurrentCar as MSTSWagon).BrakeSystem.AngleCockAOpenAmount;
                var carAngleCockAOpen = (CurrentCar as MSTSWagon).BrakeSystem.AngleCockAOpen;
                Texture = carAngleCockAOpenAmount > 0 && carAngleCockAOpenAmount < 1 ? FrontAngleCockPartial
                    : carAngleCockAOpen ? FrontAngleCockOpened
                    : FrontAngleCockClosed;
            }
            Source = new Rectangle(0, 0, size, size);

            var trainCarOperations = Viewer.TrainCarOperationsWindow;
            if (!trainCarOperations.WarningCarPosition[carPosition])
            {
                trainCarOperations.updateWarningCarPosition(carPosition, Texture, First ? FrontAngleCockOpened : FrontAngleCockClosed);
                trainCarOperations.updateWarningCarPosition(carPosition, Texture, FrontAngleCockPartial);
            }
        }
    }
    class buttonRearAngleCock : Image
    {
        readonly Viewer Viewer;
        public buttonRearAngleCock(int x, int y, int size, Viewer viewer, TrainCar car, int carPosition)
            : base(x, y, size, size)
        {
            Viewer = viewer;
            var CurrentCar = Viewer.PlayerTrain.Cars[carPosition];
            var Last = car == viewer.PlayerTrain.Cars.Last();

            if (CurrentCar.BrakeSystem is VacuumSinglePipe)
            {
                Texture = RearAngleCockNotAvailable;
            }
            else
            {
                var carAngleCockBOpenAmount = (CurrentCar as MSTSWagon).BrakeSystem.AngleCockBOpenAmount;
                var carAngleCockBOpen = (CurrentCar as MSTSWagon).BrakeSystem.AngleCockBOpen;
                Texture = carAngleCockBOpenAmount > 0 && carAngleCockBOpenAmount < 1 ? RearAngleCockPartial
                    : carAngleCockBOpen ? RearAngleCockOpened
                    : RearAngleCockClosed;
            }
            Source = new Rectangle(0, 0, size, size);

            var trainCarOperations = Viewer.TrainCarOperationsWindow;
            if (!trainCarOperations.WarningCarPosition[carPosition])
            {
                trainCarOperations.updateWarningCarPosition(carPosition, Texture, Last ? RearAngleCockOpened: RearAngleCockClosed);
                trainCarOperations.updateWarningCarPosition(carPosition, Texture, RearAngleCockPartial);
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
            var CurrentCar = Viewer.PlayerTrain.Cars[CarPosition];

            var carOperationsPosition = Viewer.CarOperationsWindow.CarPosition;
            if ((CurrentCar as MSTSWagon).BrakeSystem is SingleTransferPipe || Viewer.PlayerTrain.Cars.Count() == 1)
            {
                Texture = BleedOffValveNotAvailable;
            }
            else
            {
                if (!Viewer.TrainCarOperationsViewerWindow.Visible && Viewer.CarOperationsWindow.Visible && CarPosition == carOperationsPosition)
                    Texture = (viewer.PlayerTrain.Cars[carOperationsPosition] as MSTSWagon).BrakeSystem.BleedOffValveOpen ? BleedOffValveOpened : BleedOffValveClosed;
                else
                    Texture = (CurrentCar as MSTSWagon).BrakeSystem.BleedOffValveOpen ? BleedOffValveOpened : BleedOffValveClosed;
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
        readonly MSTSLocomotive Locomotive;
        public buttonTogglePower(int x, int y, int size, Viewer viewer, int carPosition)
            : base(x, y, size, size)
        {
            Viewer = viewer;
            TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
            TrainCarOperations = Viewer.TrainCarOperationsWindow;
            CarPosition = carPosition;
            Locomotive = Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive;

            if (Locomotive is MSTSDieselLocomotive || Locomotive is MSTSElectricLocomotive)
            {
                var powerStatus = Locomotive.LocomotivePowerSupply.GetPowerStatus();
                Texture = powerStatus == PowerSupplyState.PowerOn ? PowerOn : powerStatus == PowerSupplyState.PowerOff ? PowerOff : PowerChanging;
                TrainCarOperations.PowerSupplyStatus = powerStatus;
            }
            else
                Texture = Empty;

            Source = new Rectangle(0, 0, size, size);
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

            if (Viewer.PlayerTrain.Cars[CarPosition] is MSTSLocomotive)
            {
                Texture = (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).RemoteControlGroup == 0 ? MUconnected : MUdisconnected;
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
        readonly MSTSWagon Wagon;

        public buttonToggleBatterySwitch(int x, int y, int size, Viewer viewer, int carPosition)
            : base(x, y, size, size)
        {
            Viewer = viewer;
            CarPosition = carPosition;
            Wagon = Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon;

            if (Wagon?.PowerSupply is IPowerSupply)
            {
                if (Wagon.PowerSupply.BatterySwitch.Mode == BatterySwitch.ModeType.AlwaysOn)
                {
                    Texture = BattAlwaysOn;
                }
                else
                {
                    bool on = Wagon.PowerSupply.BatterySwitch.On;
                    Viewer.TrainCarOperationsWindow.BatterySwitchOn = on;
                    Texture = on ? BattOn : BattOff;
                }
            }
            else
            {
                Texture = Empty;
            }
            Source = new Rectangle(0, 0, size, size);
        }
    }
}
