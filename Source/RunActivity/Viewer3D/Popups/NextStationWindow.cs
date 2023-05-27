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
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Orts.Simulation;
using Orts.Simulation.Physics;
using Orts.Simulation.Signalling;
using Orts.Simulation.Timetables;
using ORTS.Common;

namespace Orts.Viewer3D.Popups
{
    public class NextStationWindow : Window
    {
        Label CurrentTime;
        Label StationPlatform;
        Label CurrentDelay;

        Label StationPreviousName;
        Label StationPreviousDistance;
        Label StationPreviousArriveScheduled;
        Label StationPreviousArriveActual;
        Label StationPreviousDepartScheduled;
        Label StationPreviousDepartActual;

        Label StationCurrentName;
        Label StationCurrentDistance;
        Label StationCurrentArriveScheduled;
        Label StationCurrentArriveActual;
        Label StationCurrentDepartScheduled;

        Label StationNextName;
        Label StationNextDistance;


        Label StationNextArriveScheduled;
        Label StationNextDepartScheduled;

        Label Message;

        public NextStationWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 34, Window.DecorationSize.Y + owner.TextFontDefault.Height * 6 + ControlLayout.SeparatorSize * 2, Viewer.Catalog.GetString("Next Station"))
        {
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            var vbox = base.Layout(layout).AddLayoutVertical();
            var boxWidth = vbox.RemainingWidth / 8;
            {
                var hbox = vbox.AddLayoutHorizontalLineOfText();
                hbox.Add(StationPlatform = new Label(boxWidth * 3, hbox.RemainingHeight, "", LabelAlignment.Left));
                hbox.Add(CurrentDelay = new Label(boxWidth * 4, hbox.RemainingHeight, ""));
                hbox.Add(CurrentTime = new Label(boxWidth, hbox.RemainingHeight, "", LabelAlignment.Center));
            }
            vbox.AddHorizontalSeparator();
            {
                var hbox = vbox.AddLayoutHorizontalLineOfText();
                hbox.Add(new Label(boxWidth * 3, hbox.RemainingHeight, Viewer.Catalog.GetString("Station")));
                hbox.Add(new Label(boxWidth, hbox.RemainingHeight, Viewer.Catalog.GetString("Distance"), LabelAlignment.Center));
                hbox.Add(new Label(boxWidth, hbox.RemainingHeight, Viewer.Catalog.GetString("Arrive"), LabelAlignment.Center));
                hbox.Add(new Label(boxWidth, hbox.RemainingHeight, Viewer.Catalog.GetString("Actual"), LabelAlignment.Center));
                hbox.Add(new Label(boxWidth, hbox.RemainingHeight, Viewer.Catalog.GetString("Depart"), LabelAlignment.Center));
                hbox.Add(new Label(boxWidth, hbox.RemainingHeight, Viewer.Catalog.GetString("Actual"), LabelAlignment.Center));
            }
            {
                var hbox = vbox.AddLayoutHorizontalLineOfText();
                hbox.Add(StationPreviousName = new Label(boxWidth * 3, hbox.RemainingHeight, ""));
                hbox.Add(StationPreviousDistance = new Label(boxWidth, hbox.RemainingHeight, "", LabelAlignment.Center));
                hbox.Add(StationPreviousArriveScheduled = new Label(boxWidth, hbox.RemainingHeight, "", LabelAlignment.Center));
                hbox.Add(StationPreviousArriveActual = new Label(boxWidth, hbox.RemainingHeight, "", LabelAlignment.Center));
                hbox.Add(StationPreviousDepartScheduled = new Label(boxWidth, hbox.RemainingHeight, "", LabelAlignment.Center));
                hbox.Add(StationPreviousDepartActual = new Label(boxWidth, hbox.RemainingHeight, "", LabelAlignment.Center));
            }
            {
                var hbox = vbox.AddLayoutHorizontalLineOfText();
                hbox.Add(StationCurrentName = new Label(boxWidth * 3, hbox.RemainingHeight, ""));
                hbox.Add(StationCurrentDistance = new Label(boxWidth, hbox.RemainingHeight, "", LabelAlignment.Center));
                hbox.Add(StationCurrentArriveScheduled = new Label(boxWidth, hbox.RemainingHeight, "", LabelAlignment.Center));
                hbox.Add(StationCurrentArriveActual = new Label(boxWidth, hbox.RemainingHeight, "", LabelAlignment.Center));
                hbox.Add(StationCurrentDepartScheduled = new Label(boxWidth, hbox.RemainingHeight, "", LabelAlignment.Center));
                hbox.AddSpace(boxWidth, hbox.RemainingHeight);
            }
            {
                var hbox = vbox.AddLayoutHorizontalLineOfText();
                hbox.Add(StationNextName = new Label(boxWidth * 3, hbox.RemainingHeight, ""));
                hbox.Add(StationNextDistance = new Label(boxWidth, hbox.RemainingHeight, "", LabelAlignment.Center));
                hbox.Add(StationNextArriveScheduled = new Label(boxWidth, hbox.RemainingHeight, "", LabelAlignment.Center));
                hbox.AddSpace(boxWidth, hbox.RemainingHeight);
                hbox.Add(StationNextDepartScheduled = new Label(boxWidth, hbox.RemainingHeight, "", LabelAlignment.Center));
                hbox.AddSpace(boxWidth, hbox.RemainingHeight);
            }
            vbox.AddHorizontalSeparator();
            {
                var hbox = vbox.AddLayoutHorizontalLineOfText();
                hbox.Add(Message = new Label(boxWidth * 7, hbox.RemainingHeight, ""));
            }
            return vbox;
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            if (updateFull)
            {
                CurrentTime.Text = FormatStrings.FormatTime(Owner.Viewer.Simulator.ClockTime);
                Activity act = Owner.Viewer.Simulator.ActivityRun;
                Train playerTrain = Owner.Viewer.Simulator.PlayerLocomotive.Train;
                if (playerTrain.Delay.HasValue)
                {
                    CurrentDelay.Text = Viewer.Catalog.GetPluralStringFmt("Current Delay: {0} minute", "Current Delay: {0} minutes", (long)playerTrain.Delay.Value.TotalMinutes);
                }
                else
                {
                    CurrentDelay.Text = "";
                }

                bool metric = Owner.Viewer.MilepostUnitsMetric;
                bool InfoAvail = false;

                ActivityTaskPassengerStopAt at = null;
                ActivityTaskPassengerStopAt Current = null;

                // timetable information
                if (playerTrain.CheckStations)
                {
                    TTTrain playerTimetableTrain = playerTrain as TTTrain;

                    // train name
                    StationPlatform.Text = String.Concat(playerTimetableTrain.Name.Substring(0, Math.Min(playerTimetableTrain.Name.Length, 20)));

                    if (playerTimetableTrain.ControlMode == Train.TRAIN_CONTROL.INACTIVE || playerTimetableTrain.MovementState == Simulation.AIs.AITrain.AI_MOVEMENT_STATE.AI_STATIC)
                    {
                        // no info available
                        StationPreviousName.Text = "";
                        StationPreviousArriveScheduled.Text = "";
                        StationPreviousArriveActual.Text = "";
                        StationPreviousDepartScheduled.Text = "";
                        StationPreviousDepartActual.Text = "";
                        StationPreviousDistance.Text = "";

                        StationCurrentName.Text = "";
                        StationCurrentArriveScheduled.Text = "";
                        StationCurrentArriveActual.Text = "";
                        StationCurrentDepartScheduled.Text = "";
                        StationCurrentDistance.Text = "";

                        StationNextName.Text = "";
                        StationNextArriveScheduled.Text = "";
                        StationNextDepartScheduled.Text = "";
                        StationNextDistance.Text = "";

                        bool validMessage = false;

                        if (playerTimetableTrain.NeedAttach != null && playerTimetableTrain.NeedAttach.ContainsKey(-1))
                        {
                            List<int> attachTrains = playerTimetableTrain.NeedAttach[-1];
                            TTTrain otherTrain = playerTimetableTrain.GetOtherTTTrainByNumber(attachTrains[0]);
                            if (otherTrain == null)
                            {
                                if (playerTimetableTrain.Simulator.AutoGenDictionary.ContainsKey(attachTrains[0]))
                                {
                                    otherTrain = playerTimetableTrain.Simulator.AutoGenDictionary[attachTrains[0]] as TTTrain;
                                }
                            }

                            if (otherTrain == null)
                            {
                                Message.Text = Viewer.Catalog.GetString("Waiting for train to attach");
                                Message.Color = Color.Orange;
                                validMessage = true;
                            }
                            else
                            {
                                Message.Text = String.Concat(Viewer.Catalog.GetString("Waiting for train to attach : "), otherTrain.Name);
                                Message.Color = Color.Orange;
                                validMessage = true;
                            }
                        }

                        if (!validMessage && playerTimetableTrain.NeedTrainTransfer.Count > 0)
                        {
                            foreach (TrackCircuitSection occSection in playerTimetableTrain.OccupiedTrack)
                            {
                                if (playerTimetableTrain.NeedTrainTransfer.ContainsKey(occSection.Index))
                                {

                                    Message.Text = Viewer.Catalog.GetString("Waiting for transfer");
                                    Message.Color = Color.Orange;
                                    break;
                                }
                            }
                        }

                        if (!validMessage)
                        {
                            Message.Color = Color.White;

                            if (playerTimetableTrain.ActivateTime.HasValue)
                            {
                                DateTime activateDT = new DateTime((long)(Math.Pow(10, 7) * playerTimetableTrain.ActivateTime.Value));
                                if (playerTimetableTrain.ControlMode == Train.TRAIN_CONTROL.INACTIVE)
                                {
                                    Message.Text = Viewer.Catalog.GetString("Train inactive.");
                                }
                                else if (playerTimetableTrain.MovementState == Simulation.AIs.AITrain.AI_MOVEMENT_STATE.AI_STATIC)
                                {
                                    Message.Text = Viewer.Catalog.GetString("Train static.");
                                }
                                else
                                {
                                    Message.Text = Viewer.Catalog.GetString("Train not active.");
                                }

                                // set activation message or time
                                if (playerTimetableTrain.TriggeredActivationRequired)
                                {
                                    Message.Text = String.Concat(Message.Text, Viewer.Catalog.GetString(" Activated by other train."));
                                }
                                else
                                {
                                    Message.Text = String.Concat(Message.Text, Viewer.Catalog.GetString(" Activation time : "), activateDT.ToString("HH:mm:ss"));
                                }
                            }
                            else
                            {
                                Message.Text = Viewer.Catalog.GetString("Train has terminated.");
                            }
                        }
                    }
                    else
                    {
                        // previous stop
                        if (playerTimetableTrain.PreviousStop == null)
                        {
                            StationPreviousName.Text = "";
                            StationPreviousArriveScheduled.Text = "";
                            StationPreviousArriveActual.Text = "";
                            StationPreviousDepartScheduled.Text = "";
                            StationPreviousDepartActual.Text = "";
                            StationPreviousDistance.Text = "";
                        }
                        else
                        {
                            StationPreviousName.Text = playerTimetableTrain.PreviousStop.PlatformItem.Name;
                            StationPreviousArriveScheduled.Text = playerTimetableTrain.PreviousStop.arrivalDT.ToString("HH:mm:ss");
                            if (playerTimetableTrain.PreviousStop.ActualArrival >= 0)
                            {
                                DateTime actArrDT = new DateTime((long)(Math.Pow(10, 7) * playerTimetableTrain.PreviousStop.ActualArrival));
                                StationPreviousArriveActual.Text = actArrDT.ToString("HH:mm:ss");
                                StationPreviousArriveActual.Color = actArrDT < playerTimetableTrain.PreviousStop.arrivalDT ? Color.LightGreen : Color.LightSalmon;
                                DateTime actDepDT = new DateTime((long)(Math.Pow(10, 7) * playerTimetableTrain.PreviousStop.ActualDepart));
                                StationPreviousDepartActual.Text = actDepDT.ToString("HH:mm:ss");
                                StationPreviousDepartActual.Color = actDepDT > playerTimetableTrain.PreviousStop.arrivalDT ? Color.LightGreen : Color.LightSalmon;
                            }
                            else
                            {
                                StationPreviousArriveActual.Text = Viewer.Catalog.GetString("(missed)");
                                StationPreviousArriveActual.Color = Color.LightSalmon;
                                StationPreviousDepartActual.Text = "";
                            }
                            StationPreviousDepartScheduled.Text = playerTimetableTrain.PreviousStop.departureDT.ToString("HH:mm:ss");
                            StationPreviousDistance.Text = "";
                        }

                        if (playerTimetableTrain.StationStops == null || playerTimetableTrain.StationStops.Count == 0)
                        {
                            StationCurrentName.Text = "";
                            StationCurrentArriveScheduled.Text = "";
                            StationCurrentArriveActual.Text = "";
                            StationCurrentDepartScheduled.Text = "";
                            StationCurrentDistance.Text = "";

                            StationNextName.Text = "";
                            StationNextArriveScheduled.Text = "";
                            StationNextDepartScheduled.Text = "";
                            StationNextDistance.Text = "";

                            Message.Text = Viewer.Catalog.GetString("No more stations.");
                            Message.Color = Color.White;
                        }
                        else
                        {
                            StationCurrentName.Text = playerTimetableTrain.StationStops[0].PlatformItem.Name;
                            StationCurrentArriveScheduled.Text = playerTimetableTrain.StationStops[0].arrivalDT.ToString("HH:mm:ss");
                            if (playerTimetableTrain.StationStops[0].ActualArrival >= 0)
                            {
                                DateTime actArrDT = new DateTime((long)(Math.Pow(10, 7) * playerTimetableTrain.StationStops[0].ActualArrival));
                                StationCurrentArriveActual.Text = actArrDT.ToString("HH:mm:ss");
                                StationCurrentArriveActual.Color = actArrDT < playerTimetableTrain.StationStops[0].arrivalDT ? Color.LightGreen : Color.LightSalmon;

                            }
                            else
                            {
                                StationCurrentArriveActual.Text = "";
                            }
                            StationCurrentDepartScheduled.Text = playerTimetableTrain.StationStops[0].departureDT.ToString("HH:mm:ss");
                            StationCurrentDistance.Text = FormatStrings.FormatDistanceDisplay(playerTimetableTrain.StationStops[0].DistanceToTrainM, metric);
                            Message.Text = playerTimetableTrain.DisplayMessage;
                            Message.Color = playerTimetableTrain.DisplayColor;

                            if (playerTimetableTrain.StationStops.Count >= 2)
                            {
                                StationNextName.Text = playerTimetableTrain.StationStops[1].PlatformItem.Name;
                                StationNextArriveScheduled.Text = playerTimetableTrain.StationStops[1].arrivalDT.ToString("HH:mm:ss");
                                StationNextDepartScheduled.Text = playerTimetableTrain.StationStops[1].departureDT.ToString("HH:mm:ss");
                                StationNextDistance.Text = "";
                            }
                            else
                            {
                                StationNextName.Text = "";
                                StationNextArriveScheduled.Text = "";
                                StationNextDepartScheduled.Text = "";
                                StationNextDistance.Text = "";
                            }
                        }

                        // check transfer details
                        bool transferValid = false;
                        string TransferMessage = String.Empty;

                        if (playerTimetableTrain.TransferStationDetails != null && playerTimetableTrain.TransferStationDetails.Count > 0 &&
                            playerTimetableTrain.StationStops != null && playerTimetableTrain.StationStops.Count > 0)
                        {
                            if (playerTimetableTrain.TransferStationDetails.ContainsKey(playerTimetableTrain.StationStops[0].PlatformReference))
                            {
                                TransferInfo thisTransfer = playerTimetableTrain.TransferStationDetails[playerTimetableTrain.StationStops[0].PlatformReference];
                                TransferMessage = Viewer.Catalog.GetString("Transfer units at next station with train ");
                                TransferMessage = String.Concat(TransferMessage, thisTransfer.TransferTrainName);
                                transferValid = true;
                            }
                        }
                        else if (playerTimetableTrain.TransferTrainDetails != null && playerTimetableTrain.TransferTrainDetails.Count > 0)
                        {
                            foreach (KeyValuePair<int, List<TransferInfo>> transferDetails in playerTimetableTrain.TransferTrainDetails)
                            {
                                TransferInfo thisTransfer = transferDetails.Value[0];
                                TransferMessage = Viewer.Catalog.GetString("Transfer units with train ");
                                TransferMessage = String.Concat(TransferMessage, thisTransfer.TransferTrainName);
                                transferValid = true;
                                break;  // only show first
                            }
                        }

                        // attach details
                        if (playerTimetableTrain.AttachDetails != null)
                        {
                            bool attachDetailsValid = false;

                            // attach is not at station - details valid
                            if (playerTimetableTrain.AttachDetails.StationPlatformReference < 0)
                            {
                                attachDetailsValid = true;
                            }
                            // no further stations - details valid
                            if (playerTimetableTrain.StationStops == null || playerTimetableTrain.StationStops.Count <= 0)
                            {
                                attachDetailsValid = true;
                            }
                            // attach is at next station - details valid
                            else if (playerTimetableTrain.AttachDetails.StationPlatformReference == playerTimetableTrain.StationStops[0].PlatformReference)
                            {
                                attachDetailsValid = true;
                            }

                            if (attachDetailsValid)
                            {
                                if (playerTimetableTrain.AttachDetails.Valid)
                                {
                                    Message.Text = Viewer.Catalog.GetString("Train is to attach to : ");
                                    Message.Text = String.Concat(Message.Text, playerTimetableTrain.AttachDetails.AttachTrainName);
                                    Message.Color = Color.Orange;
                                }
                                else
                                {
                                    Message.Text = Viewer.Catalog.GetString("Train is to attach to : ");
                                    Message.Text = String.Concat(Message.Text, playerTimetableTrain.AttachDetails.AttachTrainName);
                                    Message.Text = String.Concat(Message.Text, Viewer.Catalog.GetString(" ; other train not yet ready"));
                                    Message.Color = Color.Orange;
                                }
                            }
                        }

                        // general details
                        else if (playerTimetableTrain.PickUpStaticOnForms)
                        {
                            Message.Text = Viewer.Catalog.GetString("Train is to pickup train at end of path");
                            Message.Color = Color.Orange;
                        }
                        else if (playerTimetableTrain.NeedPickUp)
                        {
                            Message.Text = Viewer.Catalog.GetString("Pick up train ahead");
                            Message.Color = Color.Orange;
                        }
                        else if (transferValid)
                        {
                            Message.Text = String.Copy(TransferMessage);
                            Message.Color = Color.Orange;
                        }
                        else if (playerTimetableTrain.NeedTransfer)
                        {
                            Message.Text = Viewer.Catalog.GetString("Transfer units with train ahead");
                            Message.Color = Color.Orange;
                        }
                    }
                }

                // activity mode - switched train
                else if (!Owner.Viewer.Simulator.TimetableMode && playerTrain != Owner.Viewer.Simulator.OriginalPlayerTrain)
                {
                    // train name
                    StationPlatform.Text = String.Concat(playerTrain.Name.Substring(0, Math.Min(playerTrain.Name.Length, 20)));

                    if (playerTrain.ControlMode == Train.TRAIN_CONTROL.INACTIVE)
                    {
                        // no info available
                        StationPreviousName.Text = "";
                        StationPreviousArriveScheduled.Text = "";
                        StationPreviousArriveActual.Text = "";
                        StationPreviousDepartScheduled.Text = "";
                        StationPreviousDepartActual.Text = "";
                        StationPreviousDistance.Text = "";

                        StationCurrentName.Text = "";
                        StationCurrentArriveScheduled.Text = "";
                        StationCurrentArriveActual.Text = "";
                        StationCurrentDepartScheduled.Text = "";
                        StationCurrentDistance.Text = "";

                        StationNextName.Text = "";
                        StationNextArriveScheduled.Text = "";
                        StationNextDepartScheduled.Text = "";
                        StationNextDistance.Text = "";

                        Message.Text = Viewer.Catalog.GetString("Train not active.");
                        Message.Color = Color.White;

                        if (playerTrain.GetType() == typeof(TTTrain))
                        {
                            TTTrain playerTimetableTrain = playerTrain as TTTrain;
                            if (playerTimetableTrain.ActivateTime.HasValue)
                            {
                                DateTime activateDT = new DateTime((long)(Math.Pow(10, 7) * playerTimetableTrain.ActivateTime.Value));
                                Message.Text = String.Concat(Message.Text, Viewer.Catalog.GetString(" Activation time : "), activateDT.ToString("HH:mm:ss"));
                            }
                        }
                    }
                    else
                    {
                        // previous stop
                        if (playerTrain.PreviousStop == null)
                        {
                            StationPreviousName.Text = "";
                            StationPreviousArriveScheduled.Text = "";
                            StationPreviousArriveActual.Text = "";
                            StationPreviousDepartScheduled.Text = "";
                            StationPreviousDepartActual.Text = "";
                            StationPreviousDistance.Text = "";
                        }
                        else
                        {
                            StationPreviousName.Text = playerTrain.PreviousStop.PlatformItem.Name;
                            StationPreviousArriveScheduled.Text = playerTrain.PreviousStop.arrivalDT.ToString("HH:mm:ss");
                            if (playerTrain.PreviousStop.ActualArrival >= 0)
                            {
                                DateTime actArrDT = new DateTime((long)(Math.Pow(10, 7) * playerTrain.PreviousStop.ActualArrival));
                                StationPreviousArriveActual.Text = actArrDT.ToString("HH:mm:ss");
                                StationPreviousArriveActual.Color = actArrDT < playerTrain.PreviousStop.arrivalDT ? Color.LightGreen : Color.LightSalmon;
                                DateTime actDepDT = new DateTime((long)(Math.Pow(10, 7) * playerTrain.PreviousStop.ActualDepart));
                                StationPreviousDepartActual.Text = actDepDT.ToString("HH:mm:ss");
                                StationPreviousDepartActual.Color = actDepDT > playerTrain.PreviousStop.arrivalDT ? Color.LightGreen : Color.LightSalmon;
                            }
                            else
                            {
                                StationPreviousArriveActual.Text = Viewer.Catalog.GetString("(missed)");
                                StationPreviousArriveActual.Color = Color.LightSalmon;
                                StationPreviousDepartActual.Text = "";
                            }
                            StationPreviousDepartScheduled.Text = playerTrain.PreviousStop.departureDT.ToString("HH:mm:ss");
                            StationPreviousDistance.Text = "";
                        }

                        if (playerTrain.StationStops == null || playerTrain.StationStops.Count == 0)
                        {
                            StationCurrentName.Text = "";
                            StationCurrentArriveScheduled.Text = "";
                            StationCurrentArriveActual.Text = "";
                            StationCurrentDepartScheduled.Text = "";
                            StationCurrentDistance.Text = "";

                            StationNextName.Text = "";
                            StationNextArriveScheduled.Text = "";
                            StationNextDepartScheduled.Text = "";
                            StationNextDistance.Text = "";

                            Message.Text = Viewer.Catalog.GetString("No more stations.");
                        }
                        else
                        {
                            StationCurrentName.Text = playerTrain.StationStops[0].PlatformItem.Name;
                            StationCurrentArriveScheduled.Text = playerTrain.StationStops[0].arrivalDT.ToString("HH:mm:ss");
                            if (playerTrain.StationStops[0].ActualArrival >= 0)
                            {
                                DateTime actArrDT = new DateTime((long)(Math.Pow(10, 7) * playerTrain.StationStops[0].ActualArrival));
                                StationCurrentArriveActual.Text = actArrDT.ToString("HH:mm:ss");
                                StationCurrentArriveActual.Color = actArrDT < playerTrain.StationStops[0].arrivalDT ? Color.LightGreen : Color.LightSalmon;

                            }
                            else
                            {
                                StationCurrentArriveActual.Text = "";
                            }
                            StationCurrentDepartScheduled.Text = playerTrain.StationStops[0].departureDT.ToString("HH:mm:ss");
                            StationCurrentDistance.Text = FormatStrings.FormatDistanceDisplay(playerTrain.StationStops[0].DistanceToTrainM, metric);
                            Message.Text = playerTrain.DisplayMessage;
                            Message.Color = playerTrain.DisplayColor;

                            if (playerTrain.StationStops.Count >= 2)
                            {
                                StationNextName.Text = playerTrain.StationStops[1].PlatformItem.Name;
                                StationNextArriveScheduled.Text = playerTrain.StationStops[1].arrivalDT.ToString("HH:mm:ss");
                                StationNextDepartScheduled.Text = playerTrain.StationStops[1].departureDT.ToString("HH:mm:ss");
                                StationNextDistance.Text = "";
                            }
                            else
                            {
                                StationNextName.Text = "";
                                StationNextArriveScheduled.Text = "";
                                StationNextDepartScheduled.Text = "";
                                StationNextDistance.Text = "";
                            }
                        }
                    }
                }

                // activity information
                if (act != null && playerTrain == Owner.Viewer.Simulator.OriginalPlayerTrain)
                {
                    Current = act.Current == null ? act.Last as ActivityTaskPassengerStopAt : act.Current as ActivityTaskPassengerStopAt;

                    at = Current != null ? Current.PrevTask as ActivityTaskPassengerStopAt : null;
                    InfoAvail = true;
                }

                if (InfoAvail)
                {
                    if (at != null)
                    {
                        StationPreviousName.Text = at.PlatformEnd1.Station;
                        StationPreviousArriveScheduled.Text = at.SchArrive.ToString("HH:mm:ss");
                        StationPreviousArriveActual.Text = at.ActArrive.HasValue ? at.ActArrive.Value.ToString("HH:mm:ss") : Viewer.Catalog.GetString("(missed)");
                        StationPreviousArriveActual.Color = GetArrivalColor(at.SchArrive, at.ActArrive);
                        StationPreviousDepartScheduled.Text = at.SchDepart.ToString("HH:mm:ss");
                        StationPreviousDepartActual.Text = at.ActDepart.HasValue ? at.ActDepart.Value.ToString("HH:mm:ss") : Viewer.Catalog.GetString("(missed)");
                        StationPreviousDepartActual.Color = GetDepartColor(at.SchDepart, at.ActDepart);

                        StationPreviousDistance.Text = "";
                        if (playerTrain.StationStops.Count > 0 && playerTrain.StationStops[0].PlatformItem != null &&
                            String.Compare(playerTrain.StationStops[0].PlatformItem.Name, StationPreviousName.Text) == 0 &&
                            playerTrain.StationStops[0].DistanceToTrainM > 0 && playerTrain.StationStops[0].DistanceToTrainM != 9999999f)
                        {
                            StationPreviousDistance.Text = FormatStrings.FormatDistanceDisplay(playerTrain.StationStops[0].DistanceToTrainM, metric);
                        }
                    }
                    else
                    {
                        StationPreviousName.Text = "";
                        StationPreviousArriveScheduled.Text = "";
                        StationPreviousArriveActual.Text = "";
                        StationPreviousDepartScheduled.Text = "";
                        StationPreviousDepartActual.Text = "";
                        StationPreviousDistance.Text = "";
                    }

                    at = Current;
                    if (at != null)
                    {
                        StationPlatform.Text = at.PlatformEnd1.ItemName;
                        StationCurrentName.Text = at.PlatformEnd1.Station;
                        StationCurrentArriveScheduled.Text = at.SchArrive.ToString("HH:mm:ss");
                        StationCurrentArriveActual.Text = at.ActArrive.HasValue ? at.ActArrive.Value.ToString("HH:mm:ss") : "";
                        StationCurrentArriveActual.Color = GetArrivalColor(at.SchArrive, at.ActArrive);
                        StationCurrentDepartScheduled.Text = at.SchDepart.ToString("HH:mm:ss");
                        Message.Color = at.DisplayColor;
                        Message.Text = at.DisplayMessage;

                        StationCurrentDistance.Text = "";
                        if (playerTrain.StationStops.Count > 0 && playerTrain.StationStops[0].PlatformItem != null &&
                            String.Compare(playerTrain.StationStops[0].PlatformItem.Name, StationCurrentName.Text) == 0 &&
                            playerTrain.StationStops[0].DistanceToTrainM > 0 && playerTrain.StationStops[0].DistanceToTrainM != 9999999f)
                        {
                            StationCurrentDistance.Text = FormatStrings.FormatDistanceDisplay(playerTrain.StationStops[0].DistanceToTrainM, metric);
                        }
                    }
                    else
                    {
                        StationPlatform.Text = "";
                        StationCurrentName.Text = "";
                        StationCurrentArriveScheduled.Text = "";
                        StationCurrentArriveActual.Text = "";
                        StationCurrentDepartScheduled.Text = "";
                        StationCurrentDistance.Text = "";
                        Message.Text = "";
                    }

                    at = Current != null ? Current.NextTask as ActivityTaskPassengerStopAt : null;
                    if (at != null)
                    {
                        StationNextName.Text = at.PlatformEnd1.Station;
                        StationNextArriveScheduled.Text = at.SchArrive.ToString("HH:mm:ss");
                        StationNextDepartScheduled.Text = at.SchDepart.ToString("HH:mm:ss");

                        StationNextDistance.Text = "";
                        if (playerTrain.StationStops.Count > 0 && playerTrain.StationStops[0].PlatformItem != null &&
                            String.Compare(playerTrain.StationStops[0].PlatformItem.Name, StationNextName.Text) == 0 &&
                            playerTrain.StationStops[0].DistanceToTrainM > 0 && playerTrain.StationStops[0].DistanceToTrainM != 9999999f)
                        {
                            StationNextDistance.Text = FormatStrings.FormatDistanceDisplay(playerTrain.StationStops[0].DistanceToTrainM, metric);
                        }
                    }
                    else
                    {
                        StationNextName.Text = "";
                        StationNextArriveScheduled.Text = "";
                        StationNextDepartScheduled.Text = "";
                        StationNextDistance.Text = "";
                    }

                    if (act != null && act.IsComplete)
                    {
                        Message.Text = Viewer.Catalog.GetString("Activity completed.");
                    }
                }
            }
        }

        public static Color GetArrivalColor(DateTime expected, DateTime? actual)
        {
            if (actual.HasValue && actual.Value <= expected)
                return Color.LightGreen;
            return Color.LightSalmon;
        }

        public static Color GetDepartColor(DateTime expected, DateTime? actual)
        {
            if (actual.HasValue && actual.Value >= expected)
                return Color.LightGreen;
            return Color.LightSalmon;
        }
    }
}
