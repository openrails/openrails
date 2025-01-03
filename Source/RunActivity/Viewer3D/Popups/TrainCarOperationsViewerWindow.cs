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
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using Orts.Viewer3D.RollingStock;
using ORTS.Common;
using ORTS.Common.Input;
using ORTS.Scripting.Api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Orts.Viewer3D.Popups
{
    public class TrainCarOperationsViewerWindow : Window
    {
        const int CarListPadding = 2;
        internal static Texture2D BattAlwaysOn32;
        internal static Texture2D BattOn32;
        internal static Texture2D BattOff32;
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
        internal static Texture2D CouplerNotAvailable;
        internal static Texture2D Empty;
        internal static Texture2D ETSconnected32;
        internal static Texture2D ETSdisconnected32;
        internal static Texture2D FrontAngleCockOpened;
        internal static Texture2D FrontAngleCockClosed;
        internal static Texture2D FrontAngleCockPartial;
        internal static Texture2D HandBrakeSet;
        internal static Texture2D HandBrakeNotSet;
        internal static Texture2D HandBrakeNotAvailable;
        internal static Texture2D LocoSymbol;
        internal static Texture2D LocoSymbolGreen;
        internal static Texture2D LocoSymbolRed;
        internal static Texture2D MUconnected;
        internal static Texture2D MUdisconnected;
        internal static Texture2D PowerOn;
        internal static Texture2D PowerOff;
        internal static Texture2D PowerChanging;
        internal static Texture2D RearAngleCockOpened;
        internal static Texture2D RearAngleCockClosed;
        internal static Texture2D RearAngleCockPartial;
        internal static Texture2D ResetBrakesOff;
        internal static Texture2D ResetBrakesOn;
        internal static Texture2D ResetBrakesWarning;

        public List<bool> AngleCockAPartiallyOpened = new List<bool>();
        public List<bool> AngleCockBPartiallyOpened = new List<bool>();
        public string BatteryStatus;
        string CircuitBreakerState;
        public int LocoRowCount;
        public string PowerSupplyStatus;
        public int RowsCount;
        public int SpacerRowCount;
        public int SymbolsRowCount;
        public int CurrentNewWidth;
        public bool BrakeHoseCarCoupling;

        const int SymbolWidth = 32;
        public static bool FontChanged;
        public static bool FontToBold;
        public int DisplaySizeY;
        public bool DisplayReSized = false;
        public int WindowHeightMin;
        public int WindowHeightMax;
        public int WindowWidthMin;
        public int WindowWidthMax;
        public bool CabCameraEnabled;
        public int windowHeight { get; set; } //required by TrainCarWindow
        public int CarPosition
        {
            set;
            get;
        }
        public string CurrentCarID
        {
            set;
            get;
        }
        public int NewCarPosition
        {
            set;
            get;
        }
        public bool TrainCarOperationsChanged
        {
            set;
            get;
        }
        public bool FrontBrakeHoseChanged
        {
            set;
            get;
        }
        public bool RearBrakeHoseChanged
        {
            set;
            get;
        }
        public bool CouplerChanged
        {
            set;
            get;
        } = false;
        public struct ListLabel
        {
            public string CarID;
            public int CarIDWidth;
        }
        public List<ListLabel> Labels = new List<ListLabel>();

        Train PlayerTrain;
        bool LastPlayerLocomotiveFlippedState;
        int LastPlayerTrainCars;
        int OldCarPosition;
        bool ResetAllSymbols;
        public TrainCarOperationsViewerWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + CarListPadding + ((owner.TextFontDefault.Height + 12) * 20), Window.DecorationSize.Y + ((owner.TextFontDefault.Height + 12) * 2), Viewer.Catalog.GetString("Train Operations Viewer"))
        {
            WindowHeightMin = Location.Height;
            WindowHeightMax = Location.Height + (owner.TextFontDefault.Height * 20);
            WindowWidthMin = Location.Width;
            WindowWidthMax = Location.Width + (owner.TextFontDefault.Height * 20);
        }

        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            outf.Write(Location.X);
            outf.Write(Location.Y);
            outf.Write(Location.Width);
            outf.Write(Location.Height);

            outf.Write(CarPosition);
            outf.Write(ResetAllSymbols);
        }
        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            Rectangle LocationRestore;
            LocationRestore.X = inf.ReadInt32();
            LocationRestore.Y = inf.ReadInt32();
            LocationRestore.Width = inf.ReadInt32();
            LocationRestore.Height = inf.ReadInt32();

            CarPosition = inf.ReadInt32();
            ResetAllSymbols = inf.ReadBoolean();

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
                // texture rectangles :                    X, Y, width, height
                Rectangle BattAlwaysOnRect = new Rectangle(0, 0, 32, 32);
                Rectangle BattOffRect = new Rectangle(32, 0, 32, 32);
                Rectangle BattOnRect = new Rectangle(64, 0, 32, 32);

                Rectangle EmptyRect = new Rectangle(96, 0, 32, 32);

                Rectangle BleedOffValveNotAvailableRect = new Rectangle(0, 32, 32, 32);
                Rectangle BleedOffValveClosedRect = new Rectangle(32, 32, 32, 32);
                Rectangle BleedOffValveOpenedRect = new Rectangle(64, 32, 32, 32);

                Rectangle BrakeHoseConRect = new Rectangle(0, 64, 32, 32);
                Rectangle BrakeHoseDisRect = new Rectangle(32, 64, 32, 32);
                Rectangle BrakeHoseFirstDisRect = new Rectangle(64, 64, 32, 32);
                Rectangle BrakeHoseLastDisRect = new Rectangle(96, 64, 32, 32);

                Rectangle CouplerNotAvailableRect = new Rectangle(0, 96, 32, 32);
                Rectangle CouplerFrontRect = new Rectangle(32, 96, 32, 32);
                Rectangle CouplerRect = new Rectangle(64, 96, 32, 32);
                Rectangle CouplerRearRect = new Rectangle(96, 96, 32, 32);

                Rectangle LocoSymbolRect = new Rectangle(0, 128, 64, 32);
                Rectangle LocoSymbolGreenRect = new Rectangle(64, 128, 64, 32);
                Rectangle LocoSymbolRedRect = new Rectangle(0, 256, 64, 32);

                Rectangle HandBrakeNotAvailableRect = new Rectangle(0, 160, 32, 32);
                Rectangle HandBrakeSetRect = new Rectangle(32, 160, 32, 32);
                Rectangle HandBrakeNotSetRect = new Rectangle(64, 160, 32, 32);

                Rectangle ETSconnected32Rect = new Rectangle(0, 192, 32, 32);
                Rectangle ETSdisconnected32Rect = new Rectangle(32, 192, 32, 32);
                Rectangle MUconnectedRect = new Rectangle(64, 192, 32, 32);
                Rectangle MUdisconnectedRect = new Rectangle(96, 192, 32, 32);

                Rectangle FrontAngleCockClosedRect = new Rectangle(0, 224, 32, 32);
                Rectangle RearAngleCockClosedRect = new Rectangle(32, 224, 32, 32);
                Rectangle FrontAngleCockOpenedRect = new Rectangle(64, 224, 32, 32);
                Rectangle RearAngleCockOpenedRect = new Rectangle(96, 224, 32, 32);
                Rectangle FrontAngleCockPartialRect = new Rectangle(96, 160, 32, 32);
                Rectangle RearAngleCockPartialRect = new Rectangle(96, 288, 32, 32);

                Rectangle PowerOnRect = new Rectangle(0, 288, 32, 32);
                Rectangle PowerOffRect = new Rectangle(32, 288, 32, 32);
                Rectangle PowerChangingRect = new Rectangle(64, 288, 32, 32);

                Rectangle ResetBrakesOffRect = new Rectangle(64, 256, 32, 32);
                Rectangle ResetBrakesOnRect = new Rectangle(96, 256, 32, 32);
                Rectangle ResetBrakesWarningRect = new Rectangle(96, 32, 32, 32);

                var GraphicsDeviceRender = Owner.Viewer.RenderProcess.GraphicsDevice;
                var TrainOperationsPath = System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperations\\TrainOperationsMap32.png");

                // TODO: This should happen on the loader thread.
                BattAlwaysOn32 = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, BattAlwaysOnRect);
                BattOn32 = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, BattOnRect);
                BattOff32 = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, BattOffRect);

                BleedOffValveClosed = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, BleedOffValveClosedRect);
                BleedOffValveOpened = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, BleedOffValveOpenedRect);
                BleedOffValveNotAvailable = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, BleedOffValveNotAvailableRect);

                BrakeHoseCon = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, BrakeHoseConRect);
                BrakeHoseDis = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, BrakeHoseDisRect);
                BrakeHoseFirstDis = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, BrakeHoseFirstDisRect);
                BrakeHoseLastDis = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, BrakeHoseLastDisRect);

                Coupler = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, CouplerRect);
                CouplerFront = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, CouplerFrontRect);
                CouplerRear = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, CouplerRearRect);
                CouplerNotAvailable = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, CouplerNotAvailableRect);

                Empty = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, EmptyRect);

                FrontAngleCockClosed = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, FrontAngleCockClosedRect);
                FrontAngleCockOpened = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, FrontAngleCockOpenedRect);
                FrontAngleCockPartial = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, FrontAngleCockPartialRect);

                HandBrakeNotAvailable = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, HandBrakeNotAvailableRect);
                HandBrakeNotSet = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, HandBrakeNotSetRect);
                HandBrakeSet = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, HandBrakeSetRect);

                LocoSymbol = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, LocoSymbolRect);
                LocoSymbolGreen = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, LocoSymbolGreenRect);
                LocoSymbolRed = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, LocoSymbolRedRect);

                MUconnected = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, MUconnectedRect);
                MUdisconnected = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, MUdisconnectedRect);

                ETSconnected32 = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, ETSconnected32Rect);
                ETSdisconnected32 = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, ETSdisconnected32Rect);

                RearAngleCockClosed = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, RearAngleCockClosedRect);
                RearAngleCockOpened = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, RearAngleCockOpenedRect);
                RearAngleCockPartial = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, RearAngleCockPartialRect);

                ResetBrakesOff = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, ResetBrakesOffRect);
                ResetBrakesOn = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, ResetBrakesOnRect);
                ResetBrakesWarning = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, ResetBrakesWarningRect);

                PowerOn = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, PowerOnRect);
                PowerOff = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, PowerOffRect);
                PowerChanging = SharedTextureManager.Get(GraphicsDeviceRender, TrainOperationsPath, PowerChangingRect);
            }
            UpdateWindowSize();
        }
        private void UpdateWindowSize()
        {
            ModifyWindowSize();
        }

        /// <summary>
        /// Modify window size
        /// </summary>
        private void ModifyWindowSize()
        {
            if (SymbolsRowCount > 0)
            {
                DisplaySizeY = Owner.Viewer.DisplaySize.Y;
                var desiredHeight = FontToBold ? Owner.TextFontDefaultBold.Height * RowsCount
                    : (Owner.TextFontDefault.Height * RowsCount) + SymbolWidth;
                var desiredWidth = (SymbolsRowCount * SymbolWidth) + (SpacerRowCount * (SymbolWidth / 2)) + (LocoRowCount * (SymbolWidth * 2));

                var newHeight = (int)MathHelper.Clamp(desiredHeight, 80, WindowHeightMax);
                var newWidth = (int)MathHelper.Clamp(desiredWidth, 100, WindowWidthMax);

                // Move the dialog up if we're expanding it, or down if not; this keeps the center in the same place.
                var newTop = Location.Y + ((Location.Height - newHeight) / 2);

                // Display window
                SizeTo(newWidth, newHeight);
                var locationX = Location.X;
                var locationY = newTop;
                if (Owner.Viewer.TrainCarOperationsWindow.LayoutMoved || newWidth != CurrentNewWidth || DisplayReSized)
                {
                    CkeckCollision(newWidth, newHeight, ref locationX, ref locationY);
                    Owner.Viewer.TrainCarOperationsWindow.LayoutMoved = false;
                    CurrentNewWidth = newWidth;
                    DisplayReSized = false;
                }
                MoveTo(locationX, locationY);
            }
        }
        public ControlLayoutVertical Vbox;
        protected override ControlLayout Layout(ControlLayout layout)
        {
            Label buttonClose;
            var textHeight = Owner.TextFontDefault.Height;
            textHeight = MathHelper.Clamp(textHeight, SymbolWidth, Owner.TextFontDefault.Height);
            Vbox = base.Layout(layout).AddLayoutVertical();

            if (PlayerTrain != null && PlayerTrain.Cars.Count() > CarPosition)
            {
                TrainCar trainCar = PlayerTrain.Cars[CarPosition];
                BrakeSystem brakeSystem = (trainCar as MSTSWagon).BrakeSystem;
                MSTSLocomotive locomotive = trainCar as MSTSLocomotive;
                MSTSWagon wagon = trainCar as MSTSWagon;

                // reset AngleCockAPartiallyOpened
                AngleCockAPartiallyOpened = Enumerable.Repeat(false, PlayerTrain.Cars.Count).ToList();
                // reset AngleCockBPartiallyOpened
                AngleCockBPartiallyOpened = Enumerable.Repeat(false, PlayerTrain.Cars.Count).ToList();

                bool isElectricDieselLocomotive = (trainCar is MSTSElectricLocomotive) || (trainCar is MSTSDieselLocomotive);

                {
                    var isDiesel = trainCar is MSTSDieselLocomotive;
                    var isElectric = trainCar is MSTSElectricLocomotive;
                    var isSteam = trainCar is MSTSSteamLocomotive;
                    var isEngine = isDiesel || isElectric || isSteam;
                    var wagonType = isEngine ? $"  {Viewer.Catalog.GetString(locomotive.WagonType.ToString())}" + $": {Viewer.Catalog.GetString(locomotive.EngineType.ToString())}"
                        : $"  {Viewer.Catalog.GetString(wagon.WagonType.ToString())}: {wagon.MainShapeFileName.Replace(".s", "").ToLower()}";

                    Vbox.Add(buttonClose = new Label(Vbox.RemainingWidth, Owner.TextFontDefault.Height, $"{Viewer.Catalog.GetString("Car ID")} {(CarPosition >= PlayerTrain.Cars.Count ? " " : PlayerTrain.Cars[CarPosition].CarID + wagonType)}", LabelAlignment.Center));
                    CurrentCarID = CarPosition >= PlayerTrain.Cars.Count ? " " : PlayerTrain.Cars[CarPosition].CarID;
                    buttonClose.Click += new Action<Control, Point>(buttonClose_Click);
                    buttonClose.Color = Owner.Viewer.TrainCarOperationsWindow.WarningCarPosition.Find(x => x == true) ? Color.Cyan : Color.White;
                    Vbox.AddHorizontalSeparator();
                }

                SpacerRowCount = SymbolsRowCount = 0;

                var line = Vbox.AddLayoutHorizontal(Vbox.RemainingHeight);
                var addspace = 0;
                void AddSpace(bool full)
                {
                    line.AddSpace(textHeight / (full ? 1 : 2), line.RemainingHeight);
                    addspace++;
                }

                {
                    var car = PlayerTrain.Cars[CarPosition];
                    //Reset brakes
                    var warningCarPos = Owner.Viewer.TrainCarOperationsWindow.WarningCarPosition.Where(x => x == true).Count();
                    line.Add(new buttonInitializeBrakes(0, 0, textHeight, Owner.Viewer, warningCarPos));

                    if (car != PlayerTrain.Cars.First())
                        AddSpace(false);

                    //Front brake hose
                    line.Add(new buttonFrontBrakeHose(0, 0, textHeight, Owner.Viewer, car, CarPosition));
                    // Front angle cock
                    line.Add(new buttonFrontAngleCock(0, 0, textHeight, Owner.Viewer, car, CarPosition));

                    if (car != PlayerTrain.Cars.First())
                        AddSpace(false);

                    // Front coupler
                    line.Add(new buttonCouplerFront(0, 0, textHeight, Owner.Viewer, car, CarPosition));
                    // Loco label
                    line.Add(new buttonLoco(0, 0, textHeight, Owner.Viewer, car));
                    // Rear coupler
                    line.Add(new buttonCouplerRear(0, 0, textHeight, Owner.Viewer, car, CarPosition));
                    AddSpace(false);
                    // Rear angle cock
                    line.Add(new buttonRearAngleCock(0, 0, textHeight, Owner.Viewer, car, CarPosition));
                    // Rear brake hose
                    line.Add(new buttonRearBrakeHose(0, 0, textHeight, Owner.Viewer, car, CarPosition));
                    AddSpace(false);
                    // Handbrake
                    line.Add(new buttonHandBrake(0, 0, textHeight, Owner.Viewer, CarPosition));
                    AddSpace(false);
                    // Bleed off valve
                    line.Add(new buttonBleedOffValve(0, 0, textHeight, Owner.Viewer, CarPosition));
                    AddSpace(false);

                    // Electric Train Supply Connection
                    if (PlayerTrain.Cars.Count() > 1 && wagon.PowerSupply != null)
                    {
                        line.Add(new ToggleElectricTrainSupplyCable(0, 0, textHeight, Owner.Viewer, CarPosition));
                        AddSpace(false);
                    }
                    if (isElectricDieselLocomotive)
                    {
                        if (locomotive.GetMultipleUnitsConfiguration() != null)
                        {
                            line.Add(new buttonToggleMU(0, 0, textHeight, Owner.Viewer, CarPosition));
                            AddSpace(false);
                        }
                        line.Add(new buttonTogglePower(0, 0, textHeight, Owner.Viewer, CarPosition));
                        AddSpace(false);
                        if ((wagon != null) && (wagon.PowerSupply is IPowerSupply))
                        {
                            line.Add(new ToggleBatterySwitch(0, 0, textHeight, Owner.Viewer, CarPosition));
                            AddSpace(false);
                        }
                    }
                    buttonClose.Color = Owner.Viewer.TrainCarOperationsWindow.WarningCarPosition.Find(x => x == true) ? Color.Cyan : Color.White;

                    RowsCount = Vbox.Controls.Count();
                    SpacerRowCount = line.Controls.Where(c => c is Orts.Viewer3D.Popups.Spacer).Count();
                    LocoRowCount = line.Controls.Where(c => c is Orts.Viewer3D.Popups.TrainCarOperationsViewerWindow.buttonLoco).Count() + 1;
                    SymbolsRowCount = line.Controls.Count() - SpacerRowCount - LocoRowCount;
                }
            }
            return Vbox;
        }
        void buttonClose_Click(Control arg1, Point arg2)
        {
            OldCarPosition = CarPosition;
            Visible = false;
        }
        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);
            if (UserInput.IsPressed(UserCommand.CameraCarNext) && CarPosition > 0)
                CarPosition--;
            else if (UserInput.IsPressed(UserCommand.CameraCarPrevious) && CarPosition < Owner.Viewer.PlayerTrain.Cars.Count - 1)
                CarPosition++;
            else if (UserInput.IsPressed(UserCommand.CameraCarFirst))
                CarPosition = 0;
            else if (UserInput.IsPressed(UserCommand.CameraCarLast))
                CarPosition = Owner.Viewer.PlayerTrain.Cars.Count - 1;

            if (Owner.Viewer.TrainCarOperationsWindow.LayoutMoved)
            {
                UpdateWindowSize();
            }

            if (updateFull)
            {
                var carOperations = Owner.Viewer.CarOperationsWindow;
                var trainCarOperations = Owner.Viewer.TrainCarOperationsWindow;

                if (CouplerChanged || PlayerTrain != Owner.Viewer.PlayerTrain || Owner.Viewer.PlayerTrain.Cars.Count != LastPlayerTrainCars || (Owner.Viewer.PlayerLocomotive != null &&
                LastPlayerLocomotiveFlippedState != Owner.Viewer.PlayerLocomotive.Flipped))
                {
                    CouplerChanged = false;
                    PlayerTrain = Owner.Viewer.PlayerTrain;

                    LastPlayerTrainCars = Owner.Viewer.PlayerTrain.Cars.Count;
                    CarPosition = CarPosition >= LastPlayerTrainCars ? LastPlayerTrainCars - 1 : CarPosition;
                    if (Owner.Viewer.PlayerLocomotive != null) LastPlayerLocomotiveFlippedState = Owner.Viewer.PlayerLocomotive.Flipped;

                    Layout();
                    UpdateWindowSize();
                }

                if (DisplaySizeY != Owner.Viewer.DisplaySize.Y)
                {
                    DisplayReSized = true;
                    UpdateWindowSize();
                }

                TrainCar trainCar = Owner.Viewer.PlayerTrain.Cars[CarPosition];
                bool isElectricDieselLocomotive = (trainCar is MSTSElectricLocomotive) || (trainCar is MSTSDieselLocomotive);

                if (OldCarPosition != CarPosition || TrainCarOperationsChanged || carOperations.CarOperationChanged
                    || trainCarOperations.CarIdClicked || carOperations.RearBrakeHoseChanged || carOperations.FrontBrakeHoseChanged)
                {
                    // Updates CarPosition
                    CarPosition = CouplerChanged ? NewCarPosition : CarPosition;

                    if (CabCameraEnabled)// Displays camera 1
                    {
                        CabCameraEnabled = false;
                    }
                    else if (OldCarPosition != CarPosition || (trainCarOperations.CarIdClicked && CarPosition == 0))
                    {
                        if (Owner.Viewer.FrontCamera.AttachedCar != null && Owner.Viewer.FrontCamera.IsCameraFront)
                            Owner.Viewer.FrontCamera.Activate();

                        if (Owner.Viewer.BackCamera.AttachedCar != null && !Owner.Viewer.FrontCamera.IsCameraFront)
                            Owner.Viewer.BackCamera.Activate();
                    }
                    OldCarPosition = CarPosition;
                    Layout();
                    UpdateWindowSize();
                    TrainCarOperationsChanged = false;

                    // Avoids bug
                    carOperations.CarOperationChanged = carOperations.Visible && carOperations.CarOperationChanged;
                }
                // Updates power supply status
                else if (isElectricDieselLocomotive &&
                     (PowerSupplyStatus != null && PowerSupplyStatus != Owner.Viewer.PlayerTrain.Cars[CarPosition].GetStatus()
                      || (BatteryStatus != null && BatteryStatus != Owner.Viewer.PlayerTrain.Cars[CarPosition].GetStatus())
                      || (CircuitBreakerState != null && CircuitBreakerState != (trainCar as MSTSElectricLocomotive).ElectricPowerSupply.CircuitBreaker.State.ToString())))
                {
                    Layout();
                    UpdateWindowSize();
                    TrainCarOperationsChanged = true;
                }

                for (var position = 0; position < Owner.Viewer.PlayerTrain.Cars.Count; position++)
                {
                    if (trainCarOperations.WarningCarPosition.Count > position && trainCarOperations.WarningCarPosition[position])
                    {
                        var carAngleCockAOpenAmount = Owner.Viewer.PlayerTrain.Cars[position].BrakeSystem.AngleCockAOpenAmount;
                        var carAngleCockBOpenAmount = Owner.Viewer.PlayerTrain.Cars[position].BrakeSystem.AngleCockBOpenAmount;
                        if (carAngleCockAOpenAmount >= 1 && AngleCockAPartiallyOpened[position])
                        {
                            AngleCockAPartiallyOpened[position] = false;
                            Layout();
                            TrainCarOperationsChanged = true;
                        }
                        if (carAngleCockBOpenAmount >= 1 && AngleCockBPartiallyOpened[position])
                        {
                            AngleCockBPartiallyOpened[position] = false;
                            Layout();
                            TrainCarOperationsChanged = true;
                        }
                        AngleCockAPartiallyOpened[position] = carAngleCockAOpenAmount < 1 && carAngleCockAOpenAmount > 0;
                        AngleCockBPartiallyOpened[position] = carAngleCockBOpenAmount < 1 && carAngleCockBOpenAmount > 0;
                    }
                }

                //required by traincarwindow to ModifyWindowSize()
                windowHeight = Vbox != null ? Vbox.Position.Height : 0;
            }
        }

        class buttonLoco : Image
        {
            readonly Viewer Viewer;
            public buttonLoco(int x, int y, int size, Viewer viewer, TrainCar car)
               : base(x, y, size * 2, size)
            {
                Viewer = viewer;
                Texture = (car == Viewer.PlayerTrain.LeadLocomotive || car is MSTSLocomotive || car.WagonType == TrainCar.WagonTypes.Tender) ? LocoSymbolGreen
                    : car.BrakesStuck || ((car is MSTSLocomotive) && (car as MSTSLocomotive).PowerReduction > 0) ? LocoSymbolRed
                    : LocoSymbol;
                Source = new Rectangle(0, 0, size * 2, size);
            }
        }
        class buttonCouplerFront : Image
        {
            readonly Viewer Viewer;
            readonly TrainCarOperationsViewerWindow TrainCarViewer;
            readonly int CarPosition;
            readonly bool First;
            readonly TrainCar Car;
            public buttonCouplerFront(int x, int y, int size, Viewer viewer, TrainCar car, int carPosition)
                : base(x, y, size, size)
            {
                Viewer = viewer;
                TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
                CarPosition = carPosition;
                Car = car;
                First = car == Viewer.PlayerTrain.Cars.First();
                Texture = First ? CouplerFront : Car.WagonType == MSTSWagon.WagonTypes.Tender ? CouplerNotAvailable : Coupler;
                Source = new Rectangle(0, 0, size, size);
                Click += new Action<Control, Point>(TrainCarOperationsCouplerFront_Click);
            }

            void TrainCarOperationsCouplerFront_Click(Control arg1, Point arg2)
            {
                if (First || Car.WagonType == MSTSWagon.WagonTypes.Tender) return;

                if (Viewer.Simulator.TimetableMode)
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("In Timetable Mode uncoupling using this window is not allowed"));
                }
                else
                {
                    new UncoupleCommand(Viewer.Log, CarPosition - 1);
                    TrainCarViewer.CouplerChanged = true;
                    TrainCarViewer.NewCarPosition = CarPosition - 1;
                    if (Viewer.CarOperationsWindow.CarPosition > CarPosition - 1)
                        Viewer.CarOperationsWindow.Visible = false;
                }
            }
        }
        class buttonCouplerRear : Image
        {
            readonly Viewer Viewer;
            readonly int CarPosition;
            readonly bool Last;
            public buttonCouplerRear(int x, int y, int size, Viewer viewer, TrainCar car, int carPosition)
                : base(x, y, size, size)
            {
                Viewer = viewer;
                CarPosition = carPosition;
                Last = car == Viewer.PlayerTrain.Cars.Last();
                Texture = Last ? CouplerRear : Coupler;
                Source = new Rectangle(0, 0, size, size);
                Click += new Action<Control, Point>(TrainCarOperationsCouplerRear_Click);
            }

            void TrainCarOperationsCouplerRear_Click(Control arg1, Point arg2)
            {
                if (Last) return;

                if (Viewer.Simulator.TimetableMode)
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("In Timetable Mode uncoupling using this window is not allowed"));
                }
                else
                {
                    new UncoupleCommand(Viewer.Log, CarPosition);
                    if (Viewer.CarOperationsWindow.CarPosition > CarPosition)
                        Viewer.CarOperationsWindow.Visible = false;
                }
            }
        }
        class buttonLabel : Label
        {
            readonly Viewer Viewer;
            readonly TrainCarOperationsViewerWindow TrainCarViewer;

            public buttonLabel(int x, int y, Viewer viewer, TrainCar car, LabelAlignment alignment)
                : base(x, y, "", alignment)
            {
                Viewer = viewer;
                TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
                Text = car.CarID;
                Click += new Action<Control, Point>(TrainCarOperationsLabel_Click);
            }

            void TrainCarOperationsLabel_Click(Control arg1, Point arg2)
            {
                TrainCarViewer.Visible = false;
            }
        }

        class buttonInitializeBrakes : Image
        {
            readonly Viewer Viewer;
            readonly TrainCarOperationsViewerWindow TrainCarViewer;
            readonly int WarningCars;
            public buttonInitializeBrakes(int x, int y, int size, Viewer viewer, int warningCars)
                : base(x, y, size, size)
            {
                Viewer = viewer;
                TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
                WarningCars = warningCars;
                Texture = WarningCars > 2 ? ResetBrakesOn : WarningCars == 0 ? ResetBrakesOff : ResetBrakesWarning;
                Source = new Rectangle(0, 0, size, size);
                Click += new Action<Control, Point>(buttonInitializeBrakes_Click);
            }

            void buttonInitializeBrakes_Click(Control arg1, Point arg2)
            {

                if (WarningCars <= 2) return;

                if (Texture == ResetBrakesOn)
                {
                    TrainCarViewer.PlayerTrain.ConnectBrakeHoses();

                    // Reset Handbrakes
                    foreach (var car in Viewer.PlayerTrain.Cars)
                    {
                        if ((car as MSTSWagon).HandBrakePresent && (car as MSTSWagon).GetTrainHandbrakeStatus())
                        {
                            new WagonHandbrakeCommand(Viewer.Log, (car as MSTSWagon), !(car as MSTSWagon).GetTrainHandbrakeStatus());
                            Texture = HandBrakeNotSet;
                        }
                    }
                    //Refresh all symbols
                    TrainCarViewer.TrainCarOperationsChanged = true;
                }
                TrainCarViewer.ResetAllSymbols = !TrainCarViewer.ResetAllSymbols;
                Texture = TrainCarViewer.ResetAllSymbols ? ResetBrakesOn : ResetBrakesOff;
            }
        }
        class buttonHandBrake : Image
        {
            readonly Viewer Viewer;
            readonly TrainCarOperationsViewerWindow TrainCarViewer;
            readonly int CarPosition;

            public buttonHandBrake(int x, int y, int size, Viewer viewer, int carPosition)
                : base(x, y, size, size)
            {
                Viewer = viewer;
                TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
                CarPosition = carPosition;
                Texture = (viewer.PlayerTrain.Cars[carPosition] as MSTSWagon).HandBrakePresent ? (viewer.PlayerTrain.Cars[carPosition] as MSTSWagon).GetTrainHandbrakeStatus() ? HandBrakeSet : HandBrakeNotSet : HandBrakeNotAvailable;
                Source = new Rectangle(0, 0, size, size);
                Click += new Action<Control, Point>(buttonHandBrake_Click);
            }
            void buttonHandBrake_Click(Control arg1, Point arg2)
            {
                if (Viewer.Simulator.TimetableMode)
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("In Timetable Mode uncoupling using this window is not allowed"));
                }
                else
                {
                    if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).HandBrakePresent)
                    {
                        new WagonHandbrakeCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).GetTrainHandbrakeStatus());
                        if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).GetTrainHandbrakeStatus())
                        {
                            Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Handbrake set"));
                            Texture = HandBrakeSet;
                        }
                        else
                        {
                            Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Handbrake off"));
                            Texture = HandBrakeNotSet;
                        }
                        TrainCarViewer.TrainCarOperationsChanged = true;
                    }
                }
            }
        }
        class buttonFrontBrakeHose : Image
        {
            readonly Viewer Viewer;
            readonly TrainCarOperationsViewerWindow TrainCarViewer;
            readonly TrainCarOperationsWindow TrainCar;
            readonly CarOperationsWindow CarOperations;

            readonly int CarPosition;
            readonly bool First;

            public buttonFrontBrakeHose(int x, int y, int size, Viewer viewer, TrainCar car, int carPosition)
                : base(x, y, size, size)
            {
                Viewer = viewer;
                TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
                TrainCar = Viewer.TrainCarOperationsWindow;
                CarOperations = Viewer.CarOperationsWindow;
                CarPosition = carPosition;
                First = car == viewer.PlayerTrain.Cars.First();
                Texture = First ? BrakeHoseFirstDis : (viewer.PlayerTrain.Cars[carPosition] as MSTSWagon).BrakeSystem.FrontBrakeHoseConnected ? BrakeHoseCon : BrakeHoseDis;
                // Allows compatibility with CarOperationWindow
                var brakeHoseChanged = CarOperations.FrontBrakeHoseChanged || CarOperations.RearBrakeHoseChanged;
                if (brakeHoseChanged && CarOperations.Visible && CarOperations.CarPosition >= 1 && CarOperations.CarPosition == CarPosition)
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
                    TrainCar.ModifiedSetting = true;
                    TrainCarViewer.TrainCarOperationsChanged = true;
                }

                Source = new Rectangle(0, 0, size, size);
                Click += new Action<Control, Point>(buttonFrontBrakeHose_Click);
            }

            void buttonFrontBrakeHose_Click(Control arg1, Point arg2)
            {
                if (First) return;

                new WagonBrakeHoseConnectCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.FrontBrakeHoseConnected);
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.FrontBrakeHoseConnected)
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front brake hose connected"));
                    Texture = BrakeHoseCon;
                }
                else
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front brake hose disconnected"));
                    Texture = BrakeHoseDis;
                }
                TrainCarViewer.FrontBrakeHoseChanged = true;
                TrainCarViewer.TrainCarOperationsChanged = true;
            }
        }
        class buttonRearBrakeHose : Image
        {
            readonly Viewer Viewer;
            readonly TrainCarOperationsViewerWindow TrainCarViewer;
            readonly int CarPosition;
            readonly bool Last;
            public buttonRearBrakeHose(int x, int y, int size, Viewer viewer, TrainCar car, int carPosition)
                : base(x, y, size, size)
            {
                Viewer = viewer;
                TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
                CarPosition = carPosition;
                Last = car == viewer.PlayerTrain.Cars.Last();
                Texture = Last ? BrakeHoseLastDis : (viewer.PlayerTrain.Cars[carPosition] as MSTSWagon).BrakeSystem.RearBrakeHoseConnected ? BrakeHoseCon : BrakeHoseDis;
                Source = new Rectangle(0, 0, size, size);
                Click += new Action<Control, Point>(buttonRearBrakeHose_Click);
            }

            void buttonRearBrakeHose_Click(Control arg1, Point arg2)
            {
                if (Last) return;

                new WagonBrakeHoseRearConnectCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.RearBrakeHoseConnected);
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.RearBrakeHoseConnected)
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Rear brake hose connected"));
                    Texture = BrakeHoseCon;
                }
                else
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Rear brake hose disconnected"));
                    Texture = BrakeHoseDis;
                }
                TrainCarViewer.RearBrakeHoseChanged = true;
                TrainCarViewer.TrainCarOperationsChanged = true;
            }
        }
        class buttonFrontAngleCock : Image
        {
            readonly Viewer Viewer;
            readonly TrainCarOperationsViewerWindow TrainCarViewer;
            readonly int CarPosition;
            readonly bool First;
            public buttonFrontAngleCock(int x, int y, int size, Viewer viewer, TrainCar car, int carPosition)
                : base(x, y, size, size)
            {
                Viewer = viewer;
                TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
                CarPosition = carPosition;
                First = car == Viewer.PlayerTrain.Cars.First();
                var carAngleCockAOpenAmount = Viewer.PlayerTrain.Cars[CarPosition].BrakeSystem.AngleCockAOpenAmount;

                Texture = First ? FrontAngleCockClosed
                    : carAngleCockAOpenAmount >= 1 ? FrontAngleCockOpened
                    : carAngleCockAOpenAmount <= 0 ? FrontAngleCockClosed
                    : FrontAngleCockPartial;
                Source = new Rectangle(0, 0, size, size);
                Click += new Action<Control, Point>(buttonFrontAngleCock_Click);
            }

            void buttonFrontAngleCock_Click(Control arg1, Point arg2)
            {
                if (First) return;

                new ToggleAngleCockACommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.AngleCockAOpen);
                var carAngleCockAOpenAmount = Viewer.PlayerTrain.Cars[CarPosition].BrakeSystem.AngleCockAOpenAmount;

                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.AngleCockAOpen && carAngleCockAOpenAmount >= 1)
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front angle cock opened"));
                    Texture = FrontAngleCockOpened;
                }
                else
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front angle cock closed"));
                    Texture = FrontAngleCockClosed;
                }
                TrainCarViewer.TrainCarOperationsChanged = true;
            }
        }
        class buttonRearAngleCock : Image
        {
            readonly Viewer Viewer;
            readonly TrainCarOperationsViewerWindow TrainCarViewer;
            readonly int CarPosition;
            readonly bool Last;
            public buttonRearAngleCock(int x, int y, int size, Viewer viewer, TrainCar car, int carPosition)
                : base(x, y, size, size)
            {
                Viewer = viewer;
                TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
                CarPosition = carPosition;
                Last = car == Viewer.PlayerTrain.Cars.Last();
                var carAngleCockBOpenAmount = (viewer.PlayerTrain.Cars[carPosition] as MSTSWagon).BrakeSystem.AngleCockBOpenAmount;

                Texture = Last ? RearAngleCockClosed
                    : carAngleCockBOpenAmount >= 1 ? RearAngleCockOpened
                    : carAngleCockBOpenAmount <= 0 ? RearAngleCockClosed
                    : RearAngleCockPartial;

                Source = new Rectangle(0, 0, size, size);

                Click += new Action<Control, Point>(buttonRearAngleCock_Click);
            }

            void buttonRearAngleCock_Click(Control arg1, Point arg2)
            {
                if (Last) return;

                new ToggleAngleCockBCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.AngleCockBOpen);
                var carAngleCockBOpenAmount = (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.AngleCockBOpenAmount;

                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.AngleCockBOpen && carAngleCockBOpenAmount >= 1)
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Rear angle cock opened"));
                    Texture = RearAngleCockOpened;
                }
                else
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Rear angle cock closed"));
                    Texture = RearAngleCockClosed;
                }
                TrainCarViewer.TrainCarOperationsChanged = true;
            }
        }
        class buttonBleedOffValve : Image
        {
            readonly Viewer Viewer;
            readonly TrainCarOperationsViewerWindow TrainCarViewer;
            readonly int CarPosition;

            public buttonBleedOffValve(int x, int y, int size, Viewer viewer, int carPosition)
                : base(x, y, size, size)
            {
                Viewer = viewer;
                TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
                CarPosition = carPosition;
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem is SingleTransferPipe
                    || Viewer.PlayerTrain.Cars.Count() == 1)
                {
                    Texture = BleedOffValveNotAvailable;
                }
                else
                {
                    Texture = (viewer.PlayerTrain.Cars[carPosition] as MSTSWagon).BrakeSystem.BleedOffValveOpen ? BleedOffValveOpened : BleedOffValveClosed;
                }
                Source = new Rectangle(0, 0, size, size);
                Click += new Action<Control, Point>(buttonBleedOffValve_Click);
            }

            void buttonBleedOffValve_Click(Control arg1, Point arg2)
            {
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem is SingleTransferPipe
                    || Viewer.PlayerTrain.Cars.Count() == 1)
                {
                    Texture = BleedOffValveNotAvailable;
                    return;
                }
                new ToggleBleedOffValveCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BleedOffValveOpen);
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BleedOffValveOpen)
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Bleed off valve opened"));
                    Texture = BleedOffValveOpened;
                }
                else
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Bleed off valve closed"));
                    Texture = BleedOffValveClosed;
                }
                TrainCarViewer.TrainCarOperationsChanged = true;
            }
        }
        class ToggleElectricTrainSupplyCable : Image
        {
            readonly Viewer Viewer;
            readonly TrainCarOperationsViewerWindow TrainCarViewer;
            readonly int CarPosition;

            public ToggleElectricTrainSupplyCable(int x, int y, int size, Viewer viewer, int carPosition)
                : base(x, y, size, size)
            {
                Viewer = viewer;
                TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
                CarPosition = carPosition;

                MSTSWagon wagon = Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon;

                if (wagon.PowerSupply != null && Viewer.PlayerTrain.Cars.Count() > 1)
                {
                    Texture = wagon.PowerSupply.FrontElectricTrainSupplyCableConnected ? ETSconnected32 : ETSdisconnected32;
                }
                else
                {
                    Texture = Empty;
                }
                Source = new Rectangle(0, 0, size, size);
                Click += new Action<Control, Point>(ToggleElectricTrainSupplyCable_Click);
            }
            void ToggleElectricTrainSupplyCable_Click(Control arg1, Point arg2)
            {
                if (Viewer.PlayerTrain.Cars.Count() == 1)
                    return;

                MSTSWagon wagon = Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon;

                if (wagon.PowerSupply != null)
                {
                    new ConnectElectricTrainSupplyCableCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !wagon.PowerSupply.FrontElectricTrainSupplyCableConnected);
                    if (wagon.PowerSupply.FrontElectricTrainSupplyCableConnected)
                        Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front ETS cable connected"));
                    else
                        Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front ETS cable disconnected"));

                    Texture = wagon.PowerSupply.FrontElectricTrainSupplyCableConnected ? ETSconnected32 : ETSdisconnected32;
                    TrainCarViewer.TrainCarOperationsChanged = true;
                }
                else
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("This car doesn't have an ETS system"));
                }
            }
        }
        class buttonToggleMU : Image
        {
            readonly Viewer Viewer;
            readonly TrainCarOperationsViewerWindow TrainCarViewer;
            readonly int CarPosition;

            public buttonToggleMU(int x, int y, int size, Viewer viewer, int carPosition)
                : base(x, y, size, size)
            {
                Viewer = viewer;
                TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
                CarPosition = carPosition;

                var multipleUnitsConfiguration = Viewer.PlayerLocomotive.GetMultipleUnitsConfiguration();
                if (Viewer.PlayerTrain.Cars[CarPosition] is MSTSDieselLocomotive && multipleUnitsConfiguration != null)
                {
                    Texture = Viewer.TrainCarOperationsWindow.ModifiedSetting || ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).RemoteControlGroup == 0 && multipleUnitsConfiguration != "1") ? MUconnected : MUdisconnected;
                }
                else
                {
                    Texture = Empty;
                }
                Source = new Rectangle(0, 0, size, size);
                Click += new Action<Control, Point>(buttonToggleMU_Click);
            }
            void buttonToggleMU_Click(Control arg1, Point arg2)
            {
                if (Viewer.PlayerTrain.Cars[CarPosition] is MSTSDieselLocomotive)
                {
                    MSTSLocomotive locomotive = Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive;

                    new ToggleMUCommand(Viewer.Log, locomotive, locomotive.RemoteControlGroup < 0);
                    if (locomotive.RemoteControlGroup == 0)
                        Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("MU signal connected"));
                    else
                        Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("MU signal disconnected"));

                    Texture = locomotive.RemoteControlGroup == 0 ? MUconnected : MUdisconnected;
                    TrainCarViewer.TrainCarOperationsChanged = true;
                }
                else
                    Viewer.Simulator.Confirmer.Warning(Viewer.Catalog.GetString("No MU command for this type of car!"));
            }
        }
        class buttonTogglePower : Image
        {
            readonly Viewer Viewer;
            readonly TrainCarOperationsViewerWindow TrainCarViewer;
            readonly int CarPosition;

            public buttonTogglePower(int x, int y, int size, Viewer viewer, int carPosition)
                : base(x, y, size, size)
            {
                Viewer = viewer;
                TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
                CarPosition = carPosition;

                if ((Viewer.PlayerTrain.Cars[CarPosition] is MSTSElectricLocomotive) || (Viewer.PlayerTrain.Cars[CarPosition] is MSTSDieselLocomotive))
                {
                    Texture = locomotiveStatus1(CarPosition);
                }
                else
                {
                    Texture = Empty;
                }
                Source = new Rectangle(0, 0, size, size);
                Click += new Action<Control, Point>(buttonTogglePower_Click);
            }
            void buttonTogglePower_Click(Control arg1, Point arg2)
            {
                if ((Viewer.PlayerTrain.Cars[CarPosition] is MSTSElectricLocomotive)
                    || (Viewer.PlayerTrain.Cars[CarPosition] is MSTSDieselLocomotive))
                {
                    MSTSLocomotive locomotive = Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive;

                    new PowerCommand(Viewer.Log, locomotive, !locomotive.LocomotivePowerSupply.MainPowerSupplyOn);
                    var mainPowerSupplyOn = locomotive.LocomotivePowerSupply.MainPowerSupplyOn;
                    if (mainPowerSupplyOn)
                        Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Power OFF command sent"));
                    else
                        Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Power ON command sent"));

                    Texture = locomotiveStatus1(CarPosition);
                    TrainCarViewer.TrainCarOperationsChanged = true;
                }
                else
                    Viewer.Simulator.Confirmer.Warning(Viewer.Catalog.GetString("No power command for this type of car!"));
            }
            public Texture2D locomotiveStatus1(int CarPosition)
            {
                string locomotiveStatus = Viewer.PlayerTrain.Cars[CarPosition].GetStatus();
                foreach (string data in locomotiveStatus.Split('\n').Where((string d) => !string.IsNullOrWhiteSpace(d)))
                {
                    string[] parts = data.Split(new string[] { " = " }, 2, StringSplitOptions.None);
                    string keyPart = parts[0];
                    string valuePart = parts?[1];
                    if (keyPart.Contains(Viewer.Catalog.GetParticularString("DieselEngine","Engine")))
                    {
                        TrainCarViewer.PowerSupplyStatus = locomotiveStatus;
                        Texture = valuePart.Contains(Viewer.Catalog.GetParticularString("DieselEngine", "Running")) ? PowerOn
                           : valuePart.Contains(Viewer.Catalog.GetParticularString("DieselEngine", "Stopped")) ? PowerOff
                           : PowerChanging;
                        break;
                    }

                    MSTSElectricLocomotive locomotive = Viewer.PlayerTrain.Cars[CarPosition] as MSTSElectricLocomotive;
                    switch (locomotive.ElectricPowerSupply.CircuitBreaker.State)
                    {
                        case ORTS.Scripting.Api.CircuitBreakerState.Closed:
                            Texture = PowerOn;
                            break;
                        case ORTS.Scripting.Api.CircuitBreakerState.Closing:
                            Texture = PowerChanging;
                            break;
                        case ORTS.Scripting.Api.CircuitBreakerState.Open:
                            Texture = PowerOff;
                            break;
                    }
                    TrainCarViewer.CircuitBreakerState = locomotive.ElectricPowerSupply.CircuitBreaker.State.ToString();
                }
                return Texture;
            }
        }
        class ToggleBatterySwitch : Image
        {
            readonly Viewer Viewer;
            readonly TrainCarOperationsViewerWindow TrainCarViewer;
            readonly int CarPosition;

            public ToggleBatterySwitch(int x, int y, int size, Viewer viewer, int carPosition)
                : base(x, y, size, size)
            {
                Viewer = viewer;
                TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
                CarPosition = carPosition;

                if (Viewer.PlayerTrain.Cars[CarPosition] is MSTSWagon wagon
                    && wagon.PowerSupply is IPowerSupply)
                {
                    if (wagon.PowerSupply.BatterySwitch.Mode == BatterySwitch.ModeType.AlwaysOn)
                    {
                        Texture = BattAlwaysOn32;
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
                Click += new Action<Control, Point>(ToggleBatterySwitch_Click);
            }
            void ToggleBatterySwitch_Click(Control arg1, Point arg2)
            {
                if (Viewer.PlayerTrain.Cars[CarPosition] is MSTSWagon wagon
                    && wagon.PowerSupply is IPowerSupply)
                {
                    if (wagon.PowerSupply.BatterySwitch.Mode == BatterySwitch.ModeType.AlwaysOn)
                    {
                        return;
                    }
                    else
                    {
                        new ToggleBatterySwitchCommand(Viewer.Log, wagon, !wagon.PowerSupply.BatterySwitch.On);

                        if (wagon.PowerSupply.BatterySwitch.On)
                            Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Switch off battery command sent"));
                        else
                            Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Switch on battery command sent"));

                        Texture = locomotiveStatus(CarPosition);
                    }
                    TrainCarViewer.TrainCarOperationsChanged = true;
                }
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
                        TrainCarViewer.BatteryStatus = locomotiveStatus;
                        Texture = valuePart.Contains(Viewer.Catalog.GetString("On")) ? BattOn32 : BattOff32;
                        break;
                    }
                }
                return Texture;
            }
        }
        public void CkeckCollision(int newWidth, int newHeight, ref int locationX, ref int locationY)
        {
            var trainCarOperations = Owner.Viewer.TrainCarOperationsWindow;
            var trainOperationsViewer = Owner.Viewer.TrainCarOperationsViewerWindow;
            var tcoX = trainCarOperations.Location.X;
            var tcoY = trainCarOperations.Location.Y;
            var tcoWidth = trainCarOperations.Location.Width;
            var tcoHeight = trainCarOperations.Location.Height;
            var tcoLocation = new Rectangle(tcoX, tcoY, tcoWidth, tcoHeight);
            var tovLocation = new Rectangle(trainOperationsViewer.Location.X, trainOperationsViewer.Location.Y, newWidth, newHeight);
            var newX = trainOperationsViewer.Location.X;
            var newY = trainOperationsViewer.Location.Y;

            // logic to apply
            var displaySizeX = Owner.Viewer.DisplaySize.X;
            var DisplaySizeY = Owner.Viewer.DisplaySize.Y;
            var halfDisplaySizeY = DisplaySizeY / 2;
            var topMarging = tcoLocation.Y;
            var bottomMarging = DisplaySizeY - (tcoLocation.Y + tcoLocation.Height);
            var leftMarging = tcoLocation.X;
            var rightMarging = displaySizeX - tcoLocation.X - tcoLocation.Width;

            if (topMarging >= tovLocation.Height && halfDisplaySizeY > tcoLocation.Y)// Top marging available
            {
                //StepCode = "Left00";
                newY = tcoLocation.Y - tovLocation.Height;
                newX = tcoLocation.X;
            }
            else if (bottomMarging >= tovLocation.Height && halfDisplaySizeY < tcoLocation.Y)// Bottom marging available
            {
                //StepCode = "Left01";
                newY = tcoLocation.Y + tcoLocation.Height;
                newX = tcoLocation.X;
            }
            else if (leftMarging > rightMarging && leftMarging >= tovLocation.Width)
            {
                //StepCode = "Right02";
                newX = tcoLocation.X - tovLocation.Width;
                newY = halfDisplaySizeY > tcoLocation.Y ? tcoLocation.Y : tcoLocation.Y + tcoLocation.Height - tovLocation.Height;
            }
            else if (leftMarging < rightMarging && rightMarging >= tovLocation.Width)
            {
                //StepCode = "Left03";
                newX = tcoLocation.X + tcoLocation.Width;
                newY = halfDisplaySizeY < tcoLocation.Y ? tcoLocation.Y + tcoLocation.Height - tovLocation.Height : tcoLocation.Y;
            }
            else if (leftMarging <= tovLocation.Width && rightMarging <= tovLocation.Width)
            {
                //StepCode = "NoEspace00";
                newX = tcoLocation.X;
                newY = halfDisplaySizeY > tcoLocation.Y ? tcoLocation.Y + tcoLocation.Height : tcoLocation.Y - tovLocation.Height;
            }
            locationX = newX;
            locationY = newY;
        }
    }
}
