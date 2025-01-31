// COPYRIGHT 2014 by the Open Rails project.
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
using Orts.Simulation.RollingStocks;
using ORTS.Common;
using ORTS.Settings;
using System.Collections.Generic;

namespace Orts.Viewer3D.Popups
{
    public class OSDCars : LayeredWindow
    {
        Matrix Identity = Matrix.Identity;

        internal const float MaximumDistance = OSDLocations.MaximumDistanceSiding;
        internal const float MinimumDistance = OSDLocations.MinimumDistance;

        public enum DisplayState
        {
            Trains = 0x1,
            Cars = 0x2,
        }
        private readonly SavingProperty<int> StateProperty;
        private DisplayState State
        {
            get => (DisplayState)StateProperty.Value;
            set
            {
                StateProperty.Value = (int)value;
            }
        }

        Dictionary<TrainCar, LabelPrimitive> Labels = new Dictionary<TrainCar, LabelPrimitive>();

        public OSDCars(WindowManager owner)
            : base(owner, 0, 0, "OSD Cars")
        {
            StateProperty = owner.Viewer.Settings.GetSavingProperty<int>("OSDCarsState");
        }

        public override bool Interactive
        {
            get
            {
                return false;
            }
        }

        public override void TabAction()
        {
            if (State == DisplayState.Trains) State = DisplayState.Cars;
            else if (State == DisplayState.Cars) State = DisplayState.Trains;
        }

        public override void PrepareFrame(RenderFrame frame, ORTS.Common.ElapsedTime elapsedTime, bool updateFull)
        {
            if (updateFull)
            {
                var labels = Labels;
                var newLabels = new Dictionary<TrainCar, LabelPrimitive>(labels.Count);
                var cars = Owner.Viewer.World.Trains.Cars;
                var cameraLocation = Owner.Viewer.Camera.CameraWorldLocation;
                var carID = Owner.Viewer.TrainCarOperationsViewerWindow.CurrentCarID;
                //data from Webpage
                var trainCarWebpage = Owner.Viewer.TrainCarOperationsWebpage;
                var carIDWebpage = trainCarWebpage != null && trainCarWebpage.Connections > 0 ? trainCarWebpage.CurrentCarID : "";
                bool isVisibleTrainCarViewerOrWebpage = (Owner.Viewer.TrainCarOperationsWindow.Visible && !Owner.Viewer.TrainCarOperationsViewerWindow.Visible) || Owner.Viewer.TrainCarOperationsViewerWindow.Visible || (trainCarWebpage != null && trainCarWebpage.Connections > 0 && Owner.Viewer.TrainCarOperationsWebpage.TrainCarSelected);
                foreach (var car in cars.Keys)
                {
                    // Calculates distance between camera and platform label.
                    var distance = WorldLocation.GetDistance(car.WorldPosition.WorldLocation, cameraLocation).Length();
                    var carBodyLength = car.CarBodyLengthM < 21 ? 21 : car.CarBodyLengthM;
                    if ((!isVisibleTrainCarViewerOrWebpage || !car.IsPlayerTrain) && distance <= MaximumDistance)
                    {
                        if ((State == DisplayState.Cars) || (State == DisplayState.Trains && (car.Train == null || car.Train.FirstCar == car)))
                        {
                            if (labels.ContainsKey(car))
                                newLabels[car] = labels[car];
                            else
                                newLabels[car] = new LabelPrimitive(Owner.Label3DMaterial, Color.Blue, Color.White, car.CarHeightM, false) { Position = car.WorldPosition };

                            newLabels[car].InsideBox = false;
                            newLabels[car].Text = State == DisplayState.Cars || car.Train == null ? car.CarID : car.Train.Name;

                            // Change color with distance.
                            var ratio = (MathHelper.Clamp(distance, MinimumDistance, MaximumDistance) - MinimumDistance) / (MaximumDistance - MinimumDistance);
                            newLabels[car].Color.A = newLabels[car].Outline.A = (byte)MathHelper.Lerp(255, 0, ratio);
                        }
                    }
                    else if (car.IsPlayerTrain && isVisibleTrainCarViewerOrWebpage && (car.CarID == carID || car.CarID == carIDWebpage) && distance <= carBodyLength)
                    {
                        if ((State == DisplayState.Cars) || (State == DisplayState.Trains && (car.Train == null || car.Train.FirstCar == car)))
                        {
                            if (labels.ContainsKey(car))
                                newLabels[car] = labels[car];
                            else
                                newLabels[car] = new LabelPrimitive(Owner.Label3DMaterial, Color.Blue, Color.White, car.CarHeightM, true) { Position = car.WorldPosition };

                            newLabels[car].InsideBox = true;
                            newLabels[car].Text = State == DisplayState.Cars || car.Train == null ? car.CarID : car.Train.Name;
                        }
                    }
                }
                Labels = newLabels;
            }

            foreach (var primitive in Labels.Values)
            {
                frame.AddPrimitive(Owner.Label3DMaterial, primitive, RenderPrimitiveGroup.Labels, ref Identity);
            }
        }

        public DisplayState CurrentDisplayState
        {
            get
            {
                return State;
            }
        }
    }
}
