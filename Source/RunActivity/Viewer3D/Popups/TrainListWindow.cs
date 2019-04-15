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

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Simulation.AIs;
using Orts.Simulation.Physics;
using ORTS.Common;
using ORTS.Common.Input;

namespace Orts.Viewer3D.Popups
{
    public class TrainListWindow : Window
    {
        public TrainListWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 20, Window.DecorationSize.Y + owner.TextFontDefault.Height * 30, Viewer.Catalog.GetString("Train List"))
        {
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            var vbox = base.Layout(layout).AddLayoutVertical();
            if (Owner.Viewer.Simulator.Activity != null || Owner.Viewer.Simulator.TimetableMode)
            {
                var colWidth = (vbox.RemainingWidth - vbox.TextHeight * 2) / 5;
                {
                    var line = vbox.AddLayoutHorizontalLineOfText();
                    line.Add(new Label(colWidth, line.RemainingHeight, Viewer.Catalog.GetString("Number")));
                    line.Add(new Label(colWidth * 3, line.RemainingHeight, Viewer.Catalog.GetString("Service Name"), LabelAlignment.Left));
                    line.Add(new Label(colWidth, line.RemainingHeight, Viewer.Catalog.GetString("Viewed"), LabelAlignment.Right));
                }
                vbox.AddHorizontalSeparator();
                var scrollbox = vbox.AddLayoutScrollboxVertical(vbox.RemainingWidth);
                var train0 = Owner.Viewer.Simulator.Trains.Find(item => item.IsActualPlayerTrain);
                if (train0 != null)
                {
                    TrainLabel number, name, viewed;
                    var line = scrollbox.AddLayoutHorizontalLineOfText();
                    line.Add(number = new TrainLabel(colWidth, line.RemainingHeight, Owner.Viewer, train0, train0.Number.ToString(), LabelAlignment.Left));
                    line.Add(name = new TrainLabel(colWidth * 4 - Owner.TextFontDefault.Height, line.RemainingHeight, Owner.Viewer, train0, train0.Name, LabelAlignment.Left));
                    if (train0 == Owner.Viewer.SelectedTrain)
                    {
                        line.Add(viewed = new TrainLabel(Owner.TextFontDefault.Height, line.RemainingHeight, Owner.Viewer, train0, "*", LabelAlignment.Right));
                        viewed.Color = Color.Red;
                    }
                    if (Owner.Viewer.Simulator.IsAutopilotMode)
                    {
                        number.Color = train0.IsPlayable ? Color.LightGreen : Color.White;
                        name.Color = train0.IsPlayable ? Color.LightGreen : Color.White;
                    }
                    if (train0 is AITrain && (train0 as AITrain).MovementState == AITrain.AI_MOVEMENT_STATE.SUSPENDED)
                    {
                        number.Color = Color.Orange;
                        name.Color = Color.Orange;
                    }
                    if (train0.IsActualPlayerTrain)
                    {
                        number.Color = Color.Red;
                        name.Color = Color.Red;
                    }

                }
                foreach (var thisTrain in Owner.Viewer.Simulator.AI.AITrains)
                {
                    if (thisTrain.MovementState != AITrain.AI_MOVEMENT_STATE.AI_STATIC && thisTrain.TrainType != Train.TRAINTYPE.PLAYER
                        && ! (thisTrain.TrainType == Train.TRAINTYPE.AI_INCORPORATED && !thisTrain.IncorporatingTrain.IsPathless))
                    {
                        var line = scrollbox.AddLayoutHorizontalLineOfText();
                        TrainLabel number, name, viewed;
                        line.Add(number = new TrainLabel(colWidth, line.RemainingHeight, Owner.Viewer, thisTrain, thisTrain.Number.ToString(), LabelAlignment.Left));
                        line.Add(name = new TrainLabel(colWidth * 4 - Owner.TextFontDefault.Height, line.RemainingHeight, Owner.Viewer, thisTrain, thisTrain.Name, LabelAlignment.Left));
                        if (thisTrain == Owner.Viewer.SelectedTrain)
                        {
                            line.Add(viewed = new TrainLabel(Owner.TextFontDefault.Height, line.RemainingHeight, Owner.Viewer, thisTrain, "*", LabelAlignment.Right));
                            viewed.Color = Color.Red;
                        }
                        if (Owner.Viewer.Simulator.IsAutopilotMode)
                        {
                            number.Color = thisTrain.IsPlayable ? Color.LightGreen : Color.White;
                            name.Color = thisTrain.IsPlayable ? Color.LightGreen : Color.White;
                        }
                        if (thisTrain.MovementState == AITrain.AI_MOVEMENT_STATE.SUSPENDED)
                        {
                            number.Color = Color.Orange;
                            name.Color = Color.Orange;
                        }
                        if (thisTrain.IsActualPlayerTrain)
                        {
                            number.Color = Color.Red;
                            name.Color = Color.Red;
                        }
                    }
                }

                // Now list static trains with loco and cab
                if (Owner.Viewer.Simulator.IsAutopilotMode)
                {
                    foreach (var thisTrain in Owner.Viewer.Simulator.Trains)
                    {
                        if (thisTrain.TrainType == Train.TRAINTYPE.STATIC && thisTrain.IsPlayable)
                        {
                            var line = scrollbox.AddLayoutHorizontalLineOfText();
                            TrainLabel number, name, viewed;
                            line.Add(number = new TrainLabel(colWidth, line.RemainingHeight, Owner.Viewer, thisTrain, thisTrain.Number.ToString(), LabelAlignment.Left));
                            line.Add(name = new TrainLabel(colWidth * 4 - Owner.TextFontDefault.Height, line.RemainingHeight, Owner.Viewer, thisTrain, thisTrain.Name, LabelAlignment.Left));
                            if (thisTrain == Owner.Viewer.SelectedTrain)
                            {
                                line.Add(viewed = new TrainLabel(Owner.TextFontDefault.Height, line.RemainingHeight, Owner.Viewer, thisTrain, "*", LabelAlignment.Right));
                                viewed.Color = Color.Red;
                            }
                            number.Color = Color.Yellow;
                            name.Color = Color.Yellow;
                         }
                    }
                }
            }
            return vbox;
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            if (updateFull && (Owner.Viewer.Simulator.Activity != null || Owner.Viewer.Simulator.TimetableMode) && Owner.Viewer.Simulator.AI.aiListChanged)
            {
                Owner.Viewer.Simulator.AI.aiListChanged = false;
                Layout();
            }
        }
    }

    class TrainLabel : Label
    {
        readonly Viewer Viewer;
        readonly Train PickedTrainFromList;

        public TrainLabel(int width, int height, Viewer viewer, Train train, String trainName, LabelAlignment alignment)
            : base(width, height, trainName, alignment)
        {
            Viewer = viewer;
            PickedTrainFromList = train;
            Click += new Action<Control, Point>(TrainListLabel_Click);
        }

        void TrainListLabel_Click(Control arg1, Point arg2)
        {
            if (PickedTrainFromList != null && PickedTrainFromList.ControlMode == Train.TRAIN_CONTROL.TURNTABLE)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Train in turntable not aligned to a track can't be selected"));
                return;
            }
            if (PickedTrainFromList != null && Viewer.PlayerLocomotive != null && Viewer.PlayerLocomotive.Train != null && Viewer.PlayerLocomotive.Train.ControlMode == Train.TRAIN_CONTROL.TURNTABLE)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Player train can't be switched when in turntable not aligned to a track"));
                return;
            }
            Viewer.Simulator.TrainSwitcher.SuspendOldPlayer = false;
            if (PickedTrainFromList != null && PickedTrainFromList != Viewer.SelectedTrain)
            {
                //Ask for change of viewed train
                Viewer.Simulator.TrainSwitcher.PickedTrainFromList = PickedTrainFromList;
                Viewer.Simulator.TrainSwitcher.ClickedTrainFromList = true;

            }
            if (PickedTrainFromList != null && (PickedTrainFromList == Viewer.SelectedTrain || (PickedTrainFromList.TrainType == Train.TRAINTYPE.AI_INCORPORATED && 
                (PickedTrainFromList as AITrain).IncorporatingTrain.IsPathless && (PickedTrainFromList as AITrain).IncorporatingTrain == Viewer.SelectedTrain)) && !PickedTrainFromList.IsActualPlayerTrain &&
                Viewer.Simulator.IsAutopilotMode && PickedTrainFromList.IsPlayable)
            {
                if (UserInput.IsDown(UserCommand.GameSuspendOldPlayer))
                    Viewer.Simulator.TrainSwitcher.SuspendOldPlayer = true;
                //Ask for change of driven train
                Viewer.Simulator.TrainSwitcher.SelectedAsPlayer = PickedTrainFromList;
                Viewer.Simulator.TrainSwitcher.ClickedSelectedAsPlayer = true;
            }
            else if (PickedTrainFromList != null && PickedTrainFromList != Viewer.SelectedTrain)
            {
                //Ask for change of viewed train
                Viewer.Simulator.TrainSwitcher.PickedTrainFromList = PickedTrainFromList;
                Viewer.Simulator.TrainSwitcher.ClickedTrainFromList = true;

            }
        }
    }
 }
