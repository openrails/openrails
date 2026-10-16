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
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using ORTS.Common;
using System;
using System.Linq;

namespace Orts.Viewer3D.Popups
{
    public class TrainOperationsWindow : Window
    {
        const int CarListPadding = 2;
        internal static Texture2D CouplerTexture;
        Train PlayerTrain;
        int LastPlayerTrainCars;
        bool LastPlayerLocomotiveFlippedState;
        public bool TrainOperationsChanged { get; set; }

        public TrainOperationsWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 37, Window.DecorationSize.Y + CarListPadding + owner.TextFontDefault.Height * 2, Viewer.Catalog.GetString("Train Operations"))
        {
        }

        protected internal override void Initialize()
        {
            base.Initialize();
            if (CouplerTexture == null)
                // TODO: This should happen on the loader thread.
                CouplerTexture = SharedTextureManager.LoadInternal(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsCoupler.png"));
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            var textHeight = Owner.TextFontDefault.Height;

            var hbox = base.Layout(layout).AddLayoutHorizontal();
            var scrollbox = hbox.AddLayoutScrollboxHorizontal(hbox.RemainingHeight);
            if (PlayerTrain != null)
            {
                int carPosition = 0;
                foreach (var car in PlayerTrain.Cars)
                {
                    var carLabel = new TrainOperationsLabel(textHeight * 6, textHeight, Owner.Viewer, car, carPosition, LabelAlignment.Center);
                    carLabel.Click += new Action<Control, Point>(carLabel_Click);

                    if (car == PlayerTrain.LeadLocomotive) carLabel.Color = Color.Green;
                    if (car.BrakesStuck || ((car is MSTSLocomotive) && (car as MSTSLocomotive).PowerReduction > 0)) carLabel.Color = Color.Red;

                    scrollbox.Add(carLabel);
                    if (car != PlayerTrain.Cars.Last())
                        scrollbox.Add(new TrainOperationsCoupler(0, 0, textHeight, Owner.Viewer, carPosition));
                    carPosition++;
                }
            }
            return hbox;
        }

        void carLabel_Click(Control arg1, Point arg2)
        {

        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            if (updateFull)
            {
                if (PlayerTrain != Owner.Viewer.PlayerTrain || Owner.Viewer.PlayerTrain.Cars.Count != LastPlayerTrainCars || (Owner.Viewer.PlayerLocomotive != null &&
                    LastPlayerLocomotiveFlippedState != Owner.Viewer.PlayerLocomotive.Flipped))
                {
                    PlayerTrain = Owner.Viewer.PlayerTrain;
                    LastPlayerTrainCars = Owner.Viewer.PlayerTrain.Cars.Count;
                    if (Owner.Viewer.PlayerLocomotive != null) LastPlayerLocomotiveFlippedState = Owner.Viewer.PlayerLocomotive.Flipped;
                    Layout();
                }
            }
        }
    }

    class TrainOperationsCoupler : Image
    {
        readonly Viewer Viewer;
        readonly int CarPosition;

        public TrainOperationsCoupler(int x, int y, int size, Viewer viewer, int carPosition)
            : base(x, y, size, size)
        {
            Viewer = viewer;
            CarPosition = carPosition;
            Texture = TrainOperationsWindow.CouplerTexture;
            Source = new Rectangle(0, 0, size, size);
            Click += new Action<Control, Point>(TrainOperationsCoupler_Click);
        }

        void TrainOperationsCoupler_Click(Control arg1, Point arg2)
        {
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

    class TrainOperationsLabel : Label
    {
        readonly Viewer Viewer;
        readonly int CarPosition;

        public TrainOperationsLabel(int x, int y, Viewer viewer, TrainCar car, int carPosition, LabelAlignment alignment)
            : base(x, y, "", alignment)
        {
            Viewer = viewer;
            CarPosition = carPosition;
            Text = car.CarID;
            Click += new Action<Control, Point>(TrainOperationsLabel_Click);
        }

        void TrainOperationsLabel_Click(Control arg1, Point arg2)
        {
            Viewer.CarOperationsWindow.CarPosition = CarPosition;
            Viewer.CarOperationsWindow.Visible = true;
            Viewer.TrainOperationsWindow.TrainOperationsChanged = true;
        }
    }
}
