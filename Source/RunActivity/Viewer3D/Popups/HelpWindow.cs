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
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Simulation;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using ORTS.Common;
using ORTS.Common.Input;
using ORTS.Settings;
using Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS;

namespace Orts.Viewer3D.Popups
{
    public class HelpWindow : Window
    {
        public bool ActivityUpdated;
        public int lastLastEventID = -1;

        Dictionary<int, string> DbfEvalTaskName = new Dictionary<int, string>();//Debrief eval
        Dictionary<int, string> DbfEvalTaskLocation = new Dictionary<int, string>();//Debrief eval
        Dictionary<int, string> DbfEvalTaskStatus = new Dictionary<int, string>();//Debrief eval
        Dictionary<int, string> DbfEvalStationName = new Dictionary<int, string>();//Debrief eval
        Dictionary<int, string> DbfEvalSchArrive = new Dictionary<int, string>();//Debrief eval
        Dictionary<int, string> DbfEvalSchDepart = new Dictionary<int, string>();//Debrief eval
        Dictionary<int, string> DbfEvalActArrive = new Dictionary<int, string>();//Debrief eval
        Dictionary<int, string> DbfEvalActDepart = new Dictionary<int, string>();//Debrief eval

        Dictionary<string, double> DbfEvalValues = new Dictionary<string, double>();//Debrief eval

        ControlLayout scrollbox;
        ControlLayoutHorizontal line;

        bool lDebriefEvalFile = false;//Debrief eval
        bool ldbfevalupdateautopilottime = false;//Debrief eval
        bool actualStatusVisible = false; //Debrief eval
        bool dbfevalActivityEnded = false; //Debrief eval
        Label indicator;//Debrief eval
        Label statusLabel;//Debrief eval
        public string Star;//Debrief eval
        StreamWriter wDbfEval;//Debrief eval
        public static float DbfEvalDistanceTravelled = 0;//Debrief eval

        // for Train Info tab
        private Train LastPlayerTrain = null;
        private int LastPlayerTrainCarCount = 0;
        private static Texture2D TrainInfoSpriteSheet = null;
        private class CarInfo { public readonly float MassKg; public readonly bool IsEngine; public CarInfo(float mass, bool isEng) { MassKg = mass; IsEngine = isEng; } }

        List<TabData> Tabs = new List<TabData>();
        int ActiveTab;

        private enum FuelTypes { Coal, DieselOil, Kwhr }
        private FuelTypes FuelType;

        public void LogSeparator(int nCols)
        {
            if (!lDebriefEvalFile) wDbfEval.WriteLine(new String('-', nCols));
        }

        public HelpWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 42, Window.DecorationSize.Y + owner.TextFontDefault.Height * 24, Viewer.Catalog.GetString("Help"))
        {
            Tabs.Add(new TabData(Tab.KeyboardShortcuts, Viewer.Catalog.GetString("Key Commands"), (cl) =>
            {
                var scrollbox = cl.AddLayoutScrollboxVertical(cl.RemainingWidth);
                var keyWidth = scrollbox.RemainingWidth / InputSettings.KeyboardLayout[0].Length;
                var keyHeight = 3 * keyWidth;
                InputSettings.DrawKeyboardMap((rowBox) =>
                {
                }, (keyBox, keyScanCode, keyName) =>
                {
                    var color = Owner.Viewer.Settings.Input.GetScanCodeColor(keyScanCode);
                    if (color == Color.Transparent)
                        color = Color.Black;

                    InputSettings.Scale(ref keyBox, keyWidth, keyHeight);
                    scrollbox.Add(new Key(keyBox.Left - scrollbox.CurrentLeft, keyBox.Top - scrollbox.CurrentTop, keyBox.Width - 1, keyBox.Height - 1, keyName, color));
                });
                foreach (UserCommand command in Enum.GetValues(typeof(UserCommand)))
                {
                    var line = scrollbox.AddLayoutHorizontalLineOfText();
                    var width = line.RemainingWidth / 2; // Split pane into 2 equal parts
                    var offset = (int)(line.RemainingWidth * 0.10); // Offset pushes command keys 10% rightwards to allow for longer command descriptions.
                    line.Add(new Label(width + offset, line.RemainingHeight, InputSettings.GetPrettyLocalizedName(command)));
                    line.Add(new Label(width - offset, line.RemainingHeight, Owner.Viewer.Settings.Input.Commands[(int)command].ToString()));
                }
            }));
            if (owner.Viewer.Simulator.Activity != null)
            {
                Tabs.Add(new TabData(Tab.ActivityBriefing, Viewer.Catalog.GetString("Briefing"), (cl) =>
                {
                    var scrollbox = cl.AddLayoutScrollboxVertical(cl.RemainingWidth);
                    if (owner.Viewer.Simulator.Activity != null &&
                        owner.Viewer.Simulator.Activity.Tr_Activity != null &&
                        owner.Viewer.Simulator.Activity.Tr_Activity.Tr_Activity_Header != null &&
                        owner.Viewer.Simulator.Activity.Tr_Activity.Tr_Activity_Header.Briefing.Length > 0)
                    {
                        scrollbox.Add(new TextFlow(scrollbox.RemainingWidth, owner.Viewer.Simulator.Activity.Tr_Activity.Tr_Activity_Header.Briefing));
                    }
                }));
                Tabs.Add(new TabData(Tab.ActivityTimetable, Viewer.Catalog.GetString("Timetable"), (cl) =>
                {
                    var colWidth = (cl.RemainingWidth - cl.TextHeight) / 7;
                    {
                        var line = cl.AddLayoutHorizontalLineOfText();
                        line.Add(new Label(colWidth * 3, line.RemainingHeight, Viewer.Catalog.GetString("Station")));
                        line.Add(new Label(colWidth, line.RemainingHeight, Viewer.Catalog.GetString("Arrive"), LabelAlignment.Center));
                        line.Add(new Label(colWidth, line.RemainingHeight, Viewer.Catalog.GetString("Actual"), LabelAlignment.Center));
                        line.Add(new Label(colWidth, line.RemainingHeight, Viewer.Catalog.GetString("Depart"), LabelAlignment.Center));
                        line.Add(new Label(colWidth, line.RemainingHeight, Viewer.Catalog.GetString("Actual"), LabelAlignment.Center));
                    }
                    cl.AddHorizontalSeparator();
                    var scrollbox = cl.AddLayoutScrollboxVertical(cl.RemainingWidth);
                    if (owner.Viewer.Simulator.ActivityRun != null)
                    {
                        foreach (var task in owner.Viewer.Simulator.ActivityRun.Tasks)
                        {
                            var stopAt = task as ActivityTaskPassengerStopAt;
                            if (stopAt != null)
                            {
                                Label arrive, depart;
                                var line = scrollbox.AddLayoutHorizontalLineOfText();
                                line.Add(new Label(colWidth * 3, line.RemainingHeight, stopAt.PlatformEnd1.Station));
                                line.Add(new Label(colWidth, line.RemainingHeight, stopAt.SchArrive.ToString("HH:mm:ss"), LabelAlignment.Center));
                                line.Add(arrive = new Label(colWidth, line.RemainingHeight, stopAt.ActArrive.HasValue ? stopAt.ActArrive.Value.ToString("HH:mm:ss") : stopAt.IsCompleted.HasValue && task.NextTask != null ? Viewer.Catalog.GetString("(missed)") : "", LabelAlignment.Center));
                                line.Add(new Label(colWidth, line.RemainingHeight, stopAt.SchDepart.ToString("HH:mm:ss"), LabelAlignment.Center));
                                line.Add(depart = new Label(colWidth, line.RemainingHeight, stopAt.ActDepart.HasValue ? stopAt.ActDepart.Value.ToString("HH:mm:ss") : stopAt.IsCompleted.HasValue && task.NextTask != null ? Viewer.Catalog.GetString("(missed)") : "", LabelAlignment.Center));
                                arrive.Color = NextStationWindow.GetArrivalColor(stopAt.SchArrive, stopAt.ActArrive);
                                depart.Color = NextStationWindow.GetDepartColor(stopAt.SchDepart, stopAt.ActDepart);
                            }
                        }
                    }
                }));
                Tabs.Add(new TabData(Tab.ActivityWorkOrders, Viewer.Catalog.GetString("Work Orders"), (cl) =>
                {
                    var colWidth = (cl.RemainingWidth - cl.TextHeight) / 20;
                    {
                        var line = cl.AddLayoutHorizontalLineOfText();
                        line.Add(new Label(colWidth * 4, line.RemainingHeight, Viewer.Catalog.GetString("Task")));
                        line.Add(new Label(colWidth * 6, line.RemainingHeight, Viewer.Catalog.GetString("Car(s)")));
                        line.Add(new Label(colWidth * 7, line.RemainingHeight, Viewer.Catalog.GetString("Location")));
                        line.Add(new Label(colWidth * 6, line.RemainingHeight, Viewer.Catalog.GetString("Status")));
                    }
                    cl.AddHorizontalSeparator();
                    var scrollbox = cl.AddLayoutScrollboxVertical(cl.RemainingWidth);
                    var separatorShown = false;
                    if (owner.Viewer.Simulator.ActivityRun.EventList != null)
                    {
                        foreach (var @event in owner.Viewer.Simulator.ActivityRun.EventList)
                        {
                            var eventAction = @event.ParsedObject as Orts.Formats.Msts.EventCategoryAction;
                            if (eventAction != null)
                            {
                                if (separatorShown)
                                    scrollbox.AddHorizontalSeparator();
                                var line = scrollbox.AddLayoutHorizontalLineOfText();
                                // Task column
                                switch (eventAction.Type)
                                {
                                    case Orts.Formats.Msts.EventType.AssembleTrain:
                                    case Orts.Formats.Msts.EventType.AssembleTrainAtLocation:
                                        line.Add(new Label(colWidth * 4, line.RemainingHeight, Viewer.Catalog.GetString("Assemble Train")));
                                        if (eventAction.Type == Orts.Formats.Msts.EventType.AssembleTrainAtLocation)
                                        {
                                            line = scrollbox.AddLayoutHorizontalLineOfText();
                                            line.Add(new Label(colWidth * 4, line.RemainingHeight, Viewer.Catalog.GetString("At Location")));
                                        }
                                        break;
                                    case Orts.Formats.Msts.EventType.DropOffWagonsAtLocation:
                                        line.Add(new Label(colWidth * 4, line.RemainingHeight, Viewer.Catalog.GetString("Drop Off")));
                                        break;
                                    case Orts.Formats.Msts.EventType.PickUpPassengers:
                                    case Orts.Formats.Msts.EventType.PickUpWagons:
                                        line.Add(new Label(colWidth * 4, line.RemainingHeight, Viewer.Catalog.GetString("Pick Up")));
                                        break;
                                }
                                if (eventAction.WagonList != null)
                                {
                                    var location = "";
                                    var locationShown = false;
                                    var wagonIdx = 0;
                                    var locationFirst = "";
                                    foreach (Orts.Formats.Msts.WorkOrderWagon wagonItem in eventAction.WagonList.WorkOrderWagonList)
                                    {
                                        if (locationShown)
                                        {
                                            line = scrollbox.AddLayoutHorizontalLineOfText();
                                            line.AddSpace(colWidth * 4, 0);
                                        }

                                        // Car(s) column
                                        // Wagon.UiD contains train and wagon indexes packed into single 32-bit value, e.g. 32678 - 0
                                        var trainIndex = wagonItem.UID >> 16;         // Extract upper 16 bits
                                        var wagonIndex = wagonItem.UID & 0x0000FFFF;  // Extract lower 16 bits
                                        var wagonName = trainIndex.ToString() + " - " + wagonIndex.ToString();
                                        var wagonType = "";
                                        var wagonFound = false;
                                        if (owner.Viewer.Simulator.Activity.Tr_Activity.Tr_Activity_File.ActivityObjects != null)
                                        {
                                            foreach (Orts.Formats.Msts.ActivityObject activityObject in owner.Viewer.Simulator.Activity.Tr_Activity.Tr_Activity_File.ActivityObjects.ActivityObjectList)
                                            {
                                                if (activityObject.ID == trainIndex)
                                                {
                                                    foreach (Orts.Formats.Msts.Wagon trainWagon in activityObject.Train_Config.TrainCfg.WagonList)
                                                    {
                                                        if (trainWagon.UiD == wagonIndex)
                                                        {
                                                            wagonType = trainWagon.Name;
                                                            wagonFound = true;
                                                            break;
                                                        }
                                                    }
                                                }
                                                if (wagonFound)
                                                    break;
                                            }
                                            if (!wagonFound)
                                            {
                                                foreach (var car in owner.Viewer.PlayerTrain.Cars)
                                                {
                                                    if (car.UiD == wagonItem.UID)
                                                    {
                                                        wagonType = Path.GetFileNameWithoutExtension(car.WagFilePath);
                                                        wagonFound = true;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                        line.Add(new Label(colWidth * 6, line.RemainingHeight, wagonName));
                                        // Because of its potential size, wagonType information would overlap with the location information.
                                        // Until a better formatting process can be established, line.add for wagonType is commented out.
                                        //line.Add(new Label(colWidth * 7, line.RemainingHeight, wagonType));

                                        // Location column
                                        if (locationShown &&
                                            !((eventAction.Type == Orts.Formats.Msts.EventType.PickUpPassengers) || (eventAction.Type == Orts.Formats.Msts.EventType.PickUpWagons)))
                                        {
                                            line.AddSpace(colWidth * 7, 0);
                                        }
                                        else
                                        {
                                            var sidingId = eventAction.Type == Orts.Formats.Msts.EventType.AssembleTrainAtLocation
                                                || eventAction.Type == Orts.Formats.Msts.EventType.DropOffWagonsAtLocation
                                                ? (uint)eventAction.SidingId : wagonItem.SidingId;
                                            foreach (var item in owner.Viewer.Simulator.TDB.TrackDB.TrItemTable)
                                            {
                                                var siding = item as Orts.Formats.Msts.SidingItem;
                                                if (siding != null && siding.TrItemId == sidingId)
                                                {
                                                    location = siding.ItemName;
                                                    break;
                                                }
                                            }
                                            if (locationFirst != location)
                                            {
                                                line.Add(new Label(colWidth * 7, line.RemainingHeight, location));
                                            }
                                            else if (location == "" | (eventAction.Type == Orts.Formats.Msts.EventType.PickUpPassengers) || (eventAction.Type == Orts.Formats.Msts.EventType.PickUpWagons))
                                                line.AddSpace(colWidth * 7, 0);
                                            locationFirst = location;
                                            locationShown = true;
                                        }
                                        // Status column
                                        if (@event.TimesTriggered == 1 && wagonIdx == 0)
                                        {
                                            line.Add(new Label(colWidth * 6, line.RemainingHeight, Viewer.Catalog.GetString("Done")));
                                        }
                                        else line.Add(new Label(colWidth, line.RemainingHeight, ""));
                                        wagonIdx++;

                                    }
                                }

                                separatorShown = true;
                            }
                        }
                    }
                }));

                Tabs.Add(new TabData(Tab.ActivityEvaluation, Viewer.Catalog.GetString("Evaluation"), (cl) =>
                {
                    if (owner.Viewer.Simulator.ActivityRun.EventList != null && owner.Viewer.Simulator.Activity.Tr_Activity.Tr_Activity_Header.Name != null)
                    {
                        var txtinfo = "";
                        var locomotive = Owner.Viewer.Simulator.PlayerLocomotive;
                        int dbfeval = 0;//Debrief eval
                        int nmissedstation = 0;//Debrief eval
                        string labeltext = "";
                        int noverspeedcoupling = Simulator.DbfEvalOverSpeedCoupling;

                        // Detect at arrival              
                        int dbfstationstopsremaining = 0;
                        Train playerTrain = Owner.Viewer.Simulator.PlayerLocomotive.Train;

                        scrollbox = cl.AddLayoutScrollboxVertical(cl.RemainingWidth);
                        var colWidth = (cl.RemainingWidth - cl.TextHeight) / 7;
                        line = scrollbox.AddLayoutHorizontalLineOfText();
                        Label indicator;
                        if (!actualStatusVisible)
                        {
                            //Activity name
                            txtinfo = "Activity: " + owner.Viewer.Simulator.Activity.Tr_Activity.Tr_Activity_Header.Name.ToString();
                            line.Add(new Label(colWidth, line.RemainingHeight, txtinfo));
                            line = scrollbox.AddLayoutHorizontalLineOfText();
                            labeltext = "Startime: " + owner.Viewer.Simulator.Activity.Tr_Activity.Tr_Activity_Header.StartTime.FormattedStartTime();
                            line.Add(new Label(colWidth * 2, line.RemainingHeight, labeltext));
                            labeltext = "Estimated time to complete: " + owner.Viewer.Simulator.Activity.Tr_Activity.Tr_Activity_Header.Duration.FormattedDurationTimeHMS();
                            line.Add(new Label(colWidth * 2, line.RemainingHeight, labeltext));
                            line = scrollbox.AddLayoutHorizontalLineOfText();
                        }
                        //Options status
                        bool lcurvespeeddependent = owner.Viewer.Settings.CurveSpeedDependent;
                        bool lbreakcouplers = owner.Viewer.Settings.BreakCouplers;

                        //Activity status
                        bool dbfevalisfinished = owner.Viewer.Simulator.ActivityRun.IsFinished;
                        bool dbfevalissuccessful = owner.Viewer.Simulator.ActivityRun.IsSuccessful;
                        bool dbfevaliscompleted = owner.Viewer.Simulator.ActivityRun.IsComplete;
                        line.Add(indicator = new Label(colWidth, line.RemainingHeight, dbfevalisfinished | dbfevalissuccessful | dbfevaliscompleted ? Viewer.Catalog.GetString("This Activity has ended.") : Viewer.Catalog.GetString("")));
                        indicator.Color = dbfevalisfinished | dbfevalissuccessful | dbfevaliscompleted ? Color.LightSalmon : Color.Black;
                        line = scrollbox.AddLayoutHorizontalLineOfText();
                        line.AddHorizontalSeparator();

                        //Timetable
                        if (!actualStatusVisible)
                        {
                            line = scrollbox.AddLayoutHorizontalLineOfText();
                            line.Add(new Label(colWidth * 0, line.RemainingHeight, Viewer.Catalog.GetString("Timetable:")));
                        }
                        if (owner.Viewer.Simulator.ActivityRun != null)
                        {
                            foreach (var task in owner.Viewer.Simulator.ActivityRun.Tasks)
                            {
                                var stopAt = task as ActivityTaskPassengerStopAt;
                                if (stopAt != null)
                                {
                                    Label arrive = new Label(colWidth, line.RemainingWidth, "");
                                    Label depart = new Label(colWidth, line.RemainingWidth, "");

                                    //Debrief eval. Avoid to reolad data
                                    arrive.Text = stopAt.ActArrive.HasValue ? stopAt.ActArrive.Value.ToString("HH:mm:ss") : stopAt.IsCompleted.HasValue && task.NextTask != null ? "(missed)" : "";
                                    depart.Text = stopAt.ActDepart.HasValue ? stopAt.ActDepart.Value.ToString("HH:mm:ss") : stopAt.IsCompleted.HasValue && task.NextTask != null ? "(missed)" : "";
                                    if (!DbfEvalStationName.ContainsKey(dbfeval))
                                    {
                                        DbfEvalStationName.Add(dbfeval, stopAt.PlatformEnd1.Station);
                                        DbfEvalSchArrive.Add(dbfeval, stopAt.SchArrive.ToString("HH:mm:ss"));
                                        DbfEvalActArrive.Add(dbfeval, arrive.Text);
                                        DbfEvalSchDepart.Add(dbfeval, stopAt.SchDepart.ToString("HH:mm:ss"));
                                        DbfEvalActDepart.Add(dbfeval, depart.Text);
                                    }
                                    else if (DbfEvalActArrive[dbfeval] != arrive.Text | DbfEvalActDepart[dbfeval] != depart.Text)
                                    {
                                        DbfEvalActArrive[dbfeval] = arrive.Text;
                                        DbfEvalActDepart[dbfeval] = depart.Text;
                                    }
                                }
                                dbfeval++;
                            }
                            //Station stops
                            if (!actualStatusVisible)
                            {
                                line = scrollbox.AddLayoutHorizontalLineOfText();
                                line.Add(new Label(colWidth * 0, line.RemainingHeight, Viewer.Catalog.GetString("- Station stops: ") + DbfEvalStationName.Count));
                            }
                            //Station stops remaining
                            foreach (var item in DbfEvalActArrive)
                            {
                                if (item.Value == "")
                                    dbfstationstopsremaining++;
                            }
                            if (!actualStatusVisible)
                            {
                                line = scrollbox.AddLayoutHorizontalLineOfText();
                                line.Add(new Label(colWidth * 3, line.RemainingHeight, Viewer.Catalog.GetString("- Remaining station stops: ") + dbfstationstopsremaining.ToString()));

                                //Current delay
                                line.Add(new Label(colWidth, line.RemainingHeight, playerTrain.Delay != null ? Viewer.Catalog.GetPluralStringFmt("Current Delay: {0} minute", "Current Delay: {0} minutes", (long)playerTrain.Delay.Value.TotalMinutes) : "Current Delay: 0 minute"));
                                line = scrollbox.AddLayoutHorizontalLineOfText();
                                line = line.AddLayoutHorizontalLineOfText();
                            }
                            //Missed station stops                            
                            foreach (var item in DbfEvalActDepart)
                            {
                                if (item.Value == Viewer.Catalog.GetString("(missed)"))
                                    nmissedstation++;
                            }
                            if (!actualStatusVisible)
                            {
                                line.Add(indicator = new Label(colWidth * 2, line.RemainingHeight, Viewer.Catalog.GetString("- Missed station stops: ") + nmissedstation));
                                indicator.Color = nmissedstation > 0 ? Color.LightSalmon : Color.White;
                                line.Add(new Label(colWidth, line.RemainingHeight, nmissedstation > 0 ? Viewer.Catalog.GetString("Station") : Viewer.Catalog.GetString(""), LabelAlignment.Left));

                                line = scrollbox.AddLayoutHorizontalLineOfText();

                                foreach (var item in DbfEvalActDepart)
                                {
                                    if (item.Value == Viewer.Catalog.GetString("(missed)"))
                                    {
                                        line.Add(indicator = new Label(colWidth * 2, line.RemainingHeight, Viewer.Catalog.GetString(" ")));
                                        line.Add(indicator = new Label(colWidth, line.RemainingHeight, DbfEvalStationName[item.Key], LabelAlignment.Left));
                                        indicator.Color = Color.LightSalmon;
                                        line = scrollbox.AddLayoutHorizontalLineOfText();
                                    }
                                }
                            }
                            if (!actualStatusVisible)
                            {
                                line.AddHorizontalSeparator();

                                //Work orders
                                line = scrollbox.AddLayoutHorizontalLineOfText();
                                line.Add(new Label(colWidth * 0, line.RemainingHeight, Viewer.Catalog.GetString("Work orders:")));
                                line = scrollbox.AddLayoutHorizontalLineOfText();
                            }
                            //--------------------------------------------------------
                            dbfeval = 0;//Debrief eval
                            bool dbfevalexist = false;//Debrief eval

                            foreach (var @event in owner.Viewer.Simulator.ActivityRun.EventList)
                            {
                                var dbfevaltaskname = "";
                                var dbfevaltasklocation = "";
                                var dbfevaltaskstatus = "";
                                var eventAction = @event.ParsedObject as Orts.Formats.Msts.EventCategoryAction;
                                if (eventAction != null)
                                {

                                    //line = scrollbox.AddLayoutHorizontalLineOfText();
                                    // Task column
                                    switch (eventAction.Type)
                                    {
                                        case Orts.Formats.Msts.EventType.AssembleTrain:
                                        case Orts.Formats.Msts.EventType.AssembleTrainAtLocation:
                                            dbfevaltaskname = Viewer.Catalog.GetString("Assemble Train"); dbfevalexist = true;
                                            if (eventAction.Type == Orts.Formats.Msts.EventType.AssembleTrainAtLocation)
                                            {
                                                dbfevaltaskname = Viewer.Catalog.GetString("Assemble Train At Location");
                                            }
                                            break;
                                        case Orts.Formats.Msts.EventType.DropOffWagonsAtLocation:
                                            dbfevaltaskname = Viewer.Catalog.GetString("Drop Off"); dbfevalexist = true;
                                            break;
                                        case Orts.Formats.Msts.EventType.PickUpPassengers:
                                        case Orts.Formats.Msts.EventType.PickUpWagons:
                                            dbfevaltaskname = Viewer.Catalog.GetString("Pick Up"); dbfevalexist = true;
                                            break;
                                    }
                                    if (eventAction.WagonList != null)
                                    {
                                        var location = "";
                                        var locationFirst = "";
                                        foreach (Orts.Formats.Msts.WorkOrderWagon wagonItem in eventAction.WagonList.WorkOrderWagonList)
                                        {
                                            var sidingId = eventAction.Type == Orts.Formats.Msts.EventType.AssembleTrainAtLocation
                                                || eventAction.Type == Orts.Formats.Msts.EventType.DropOffWagonsAtLocation
                                                ? (uint)eventAction.SidingId : wagonItem.SidingId;
                                            foreach (var item in owner.Viewer.Simulator.TDB.TrackDB.TrItemTable)
                                            {
                                                var siding = item as Orts.Formats.Msts.SidingItem;
                                                if (siding != null && siding.TrItemId == sidingId)
                                                {
                                                    location = siding.ItemName;
                                                    break;
                                                }
                                            }
                                            if (locationFirst != location)
                                                dbfevaltasklocation = location;

                                            locationFirst = location;
                                            // Status column
                                            if (@event.TimesTriggered == 1)
                                                dbfevaltaskstatus = Viewer.Catalog.GetString("Done");

                                        }
                                    }
                                    if (dbfevalexist)
                                    {
                                        if (!DbfEvalTaskName.ContainsKey(dbfeval) && !(dbfevaltaskname == "" && dbfevaltasklocation == "" && dbfevaltaskstatus == ""))
                                        {
                                            DbfEvalTaskName.Add(dbfeval, dbfevaltaskname);
                                            DbfEvalTaskLocation.Add(dbfeval, dbfevaltasklocation);
                                            DbfEvalTaskStatus.Add(dbfeval, dbfevaltaskstatus);
                                        }
                                        else if (dbfevaltaskstatus != "" && DbfEvalTaskStatus[dbfeval] != dbfevaltaskstatus)
                                        {
                                            DbfEvalTaskStatus[dbfeval] = dbfevaltaskstatus;
                                        }
                                        dbfeval++;
                                    }
                                }
                            }
                        }

                        //--------------------------------------------------------
                        //Task count
                        if (!actualStatusVisible)
                        {
                            line.Add(indicator = new Label(colWidth, line.RemainingHeight, Viewer.Catalog.GetString("- Tasks: " + DbfEvalTaskName.Count)));
                            line = scrollbox.AddLayoutHorizontalLineOfText();
                        }
                        //Task accomplished
                        int ndbfEvalTaskAccomplished = 0;
                        foreach (var item in DbfEvalTaskStatus)
                        {
                            if (item.Value == Viewer.Catalog.GetString("Done"))
                                ndbfEvalTaskAccomplished++;
                        }
                        if (!actualStatusVisible)
                        {
                            line.Add(indicator = new Label(colWidth, line.RemainingHeight, Viewer.Catalog.GetString("- Accomplished: ") + ndbfEvalTaskAccomplished));
                            line = scrollbox.AddLayoutHorizontalLineOfText();
                            line.Add(indicator = new Label(colWidth, line.RemainingHeight, Viewer.Catalog.GetString("- Coupling speed limits: ") + noverspeedcoupling));
                            line = scrollbox.AddLayoutHorizontalLineOfText();
                            line.AddHorizontalSeparator();
                        }
                        //-------------------------------------------------------------
                        // Current Debrief Eval data                        
                        line = scrollbox.AddLayoutHorizontalLineOfText();
                        if (!actualStatusVisible)
                            statusLabel = new Label(colWidth * 2, line.RemainingHeight, "Actual status: (▼)");
                        else
                            statusLabel = new Label(colWidth * 2, line.RemainingHeight, "Actual status: (▲)");

                        statusLabel.Click += status_Click;
                        line.Add(statusLabel);

                        DbfEvalValues.Clear();
                        if (actualStatusVisible)
                        {
                            ShowEvaluation(locomotive, nmissedstation, dbfstationstopsremaining, playerTrain, colWidth, lcurvespeeddependent, lbreakcouplers, ndbfEvalTaskAccomplished);
                        }

                        if (dbfevaliscompleted | dbfevalisfinished | dbfevalissuccessful)
                        {
                            ReportEvaluation(owner, cl, locomotive, nmissedstation, labeltext, noverspeedcoupling, dbfstationstopsremaining, playerTrain, colWidth, indicator, lcurvespeeddependent, lbreakcouplers, ndbfEvalTaskAccomplished);
                        }
                    }
                }));
            }
            if (owner.Viewer.Simulator.TimetableMode)
            {
                Tabs.Add(new TabData(Tab.TimetableBriefing, Viewer.Catalog.GetString("Briefing"), (cl) =>
                {
                    var tTTrain = owner.Viewer.SelectedTrain as Orts.Simulation.Timetables.TTTrain;
                    var scrollbox = cl.AddLayoutScrollboxVertical(cl.RemainingWidth);
                    var briefing = tTTrain?.Briefing ?? "";
                    var textFlow = new TextFlow(scrollbox.RemainingWidth, briefing);
                    scrollbox.Add(textFlow);
                }));
            }
            Tabs.Add(new TabData(Tab.LocomotiveProcedures, Viewer.Catalog.GetString("Procedures"), (cl) =>
            {
                var scrollbox = cl.AddLayoutScrollboxVertical(cl.RemainingWidth);
                if (owner.Viewer.Simulator.PlayerLocomotive != null &&
                    owner.Viewer.Simulator.PlayerLocomotive is MSTSLocomotive &&
                    ((MSTSLocomotive)owner.Viewer.Simulator.PlayerLocomotive).EngineOperatingProcedures != null &&
                    ((MSTSLocomotive)owner.Viewer.Simulator.PlayerLocomotive).EngineOperatingProcedures.Length > 0)
                {
                    scrollbox.Add(new TextFlow(scrollbox.RemainingWidth, ((MSTSLocomotive)owner.Viewer.Simulator.PlayerLocomotive).EngineOperatingProcedures));
                }
            }));
            Tabs.Add(new TabData(Tab.TrainInfo, Viewer.Catalog.GetString("Train Info"), (cl) =>
            {
                var labelWidth = cl.TextHeight * 13;
                var scrollbox = cl.AddLayoutScrollboxVertical(cl.RemainingWidth);
                if (Owner.Viewer.PlayerTrain != null)
                {
                    string name = Owner.Viewer.PlayerTrain.Name;
                    if (!String.IsNullOrEmpty(Owner.Viewer.Simulator.conFileName))
                        name += "    (" + Viewer.Catalog.GetString("created from") + " " + Path.GetFileName(Owner.Viewer.Simulator.conFileName) + ")";
                    var line = scrollbox.AddLayoutHorizontalLineOfText();
                    line.Add(new Label(labelWidth, line.RemainingHeight, Viewer.Catalog.GetString("Name:"), LabelAlignment.Left));
                    line.Add(new Label(line.RemainingWidth, line.RemainingHeight, name, LabelAlignment.Left));

                    if (Owner.Viewer.PlayerTrain.Cars != null) { AddAggregatedTrainInfo(Owner.Viewer.PlayerTrain, scrollbox, labelWidth); }
                }
            }));
        }

        private void ReportEvaluation(WindowManager owner, ControlLayout cl, TrainCar locomotive, int nmissedstation, string labeltext, int noverspeedcoupling, int dbfstationstopsremaining, Train playerTrain, int colWidth, Label indicator, bool lcurvespeeddependent, bool lbreakcouplers, int ndbfEvalTaskAccomplished)
        {
            line = scrollbox.AddLayoutHorizontalLineOfText();
            actualStatusVisible = false;//Enable scroll
            dbfevalActivityEnded = true;

            //If Autopilot control then update recorded time
            if (!ldbfevalupdateautopilottime && 
                (owner.Viewer.PlayerLocomotive.Train.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING || owner.Viewer.PlayerLocomotive.Train.Autopilot))
            {
                Viewer.DbfEvalAutoPilotTimeS = Viewer.DbfEvalAutoPilotTimeS + (owner.Viewer.Simulator.ClockTime - Viewer.DbfEvalIniAutoPilotTimeS);
                ldbfevalupdateautopilottime = true;
            }
            //---------------------------------------------------------
            //Report
            //----------------------------------------------------------
            colWidth = (cl.RemainingWidth - cl.TextHeight) / 14;

            var activityname = owner.Viewer.Simulator.Activity.Tr_Activity.Tr_Activity_Header.Name.ToString().Trim();

            //Create file.
            if (!lDebriefEvalFile) wDbfEval = new StreamWriter(File.Open(Program.EvaluationFilename, FileMode.Create), System.Text.Encoding.UTF8);

            labeltext = "";
            //--------------------------------------------------------------------------------
            writeline();
            LogSeparator(80);
            consolewltext("This is a Debrief Eval for " + Application.ProductName);
            LogSeparator(80);
            //--------------------------------------------------------------------------------
            consolewltext("Version      = " + (VersionInfo.Version.Length > 0 ? VersionInfo.Version : "<none>"));
            consolewltext("Build        = " + VersionInfo.Build);
            if (Program.EvaluationFilename.Length > 0)
                consolewltext("Debrief file = " + Program.EvaluationFilename);

            consolewltext("Executable   = " + Path.GetFileName(ApplicationInfo.ProcessFile));
            LogSeparator(80);
            line.AddHorizontalSeparator();

            //Report
            line = scrollbox.AddLayoutHorizontalLineOfText();
            line.Add(indicator = new Label(colWidth, line.RemainingHeight, Viewer.Catalog.GetString("A report file was created:")));
            indicator.Color = Color.LightGreen;
            line = scrollbox.AddLayoutHorizontalLineOfText();
            //line.Add(indicator = new Label(colWidth, line.RemainingHeight, Viewer.Catalog.GetString(filename)));
            line.Add(indicator = new Label(colWidth, line.RemainingHeight, Viewer.Catalog.GetString(Program.EvaluationFilename)));
            indicator.Color = Color.LightGreen;
            line = scrollbox.AddLayoutHorizontalLineOfText();
            labeltext = "-------------";
            outmesssagecolorcenter(labeltext, colWidth * 14, Color.Yellow, true);
            labeltext = "Debrief eval.";
            outmesssagecolorcenter(labeltext, colWidth * 14, Color.Yellow, true);
            labeltext = "-------------";
            outmesssagecolorcenter(labeltext, colWidth * 14, Color.Yellow, true);
            writeline();

            line = scrollbox.AddLayoutHorizontalLineOfText();
            line = scrollbox.AddLayoutHorizontalLineOfText();
            //Information:
            labeltext = "0-Information:";
            outmesssagecolor(labeltext, colWidth, Color.Yellow, true, 0, 0);

            //Activity
            labeltext = "  Route=" + owner.Viewer.Simulator.RouteName.ToString();
            outmesssage(labeltext, colWidth * 3, true, 0);
            labeltext = "  Activity=" + activityname;
            outmesssage(labeltext, colWidth * 3, true, 0);
            labeltext = "  Difficulty=" + owner.Viewer.Simulator.Activity.Tr_Activity.Tr_Activity_Header.Difficulty.ToString();
            outmesssage(labeltext, colWidth * 3, true, 0);
            labeltext = "  Startime=" + owner.Viewer.Simulator.Activity.Tr_Activity.Tr_Activity_Header.StartTime.FormattedStartTime().ToString();
            outmesssage(labeltext, colWidth * 3, true, 0);
            string sEstimatedTime = owner.Viewer.Simulator.Activity.Tr_Activity.Tr_Activity_Header.Duration.FormattedDurationTimeHMS().ToString();
            double estimatedTime = TimeSpan.Parse(sEstimatedTime).TotalSeconds;
            labeltext = "  Estimated Time=" + sEstimatedTime;
            outmesssage(labeltext, colWidth * 3, true, 0);
            //TODO: find an existing function to do it.
            double iniTime = owner.Viewer.Simulator.Activity.Tr_Activity.Tr_Activity_Header.StartTime.Hour * 3600;
            iniTime = iniTime + owner.Viewer.Simulator.Activity.Tr_Activity.Tr_Activity_Header.StartTime.Minute * 60;
            iniTime = iniTime + owner.Viewer.Simulator.Activity.Tr_Activity.Tr_Activity_Header.StartTime.Second;
            double elapsedTime = Owner.Viewer.Simulator.ClockTime - iniTime;
            labeltext = "  Elapsed Time=" + FormatStrings.FormatTime(elapsedTime);
            outmesssage(labeltext, colWidth * 3, true, 0);
            //estimatedTime
            bool bEstimatedTime = elapsedTime > iniTime ? true : false;

            //Auto pilot time.
            double autoPilotTime = Viewer.DbfEvalAutoPilotTimeS;
            //Less usage better value
            int nautoPilotTime = autoPilotTime > 0 ? Convert.ToInt16(autoPilotTime * 100 / elapsedTime) : 0;
            labeltext = "  Autopilot Time=" + FormatStrings.FormatTime(autoPilotTime);
            outmesssage(labeltext, colWidth * 3, true, 0);

            // var locomotive = Owner.Viewer.Simulator.PlayerLocomotive;                            
            var cars = Owner.Viewer.PlayerTrain.Cars;
            bool ismetric = locomotive.IsMetric;
            bool isuk = locomotive.IsUK;
            float distancetravelled = locomotive.DistanceM + DbfEvalDistanceTravelled;
            //Distance travelled
            labeltext = "  Travelled=" + FormatStrings.FormatDistanceDisplay(distancetravelled, ismetric);
            outmesssage(labeltext, colWidth * 3, true, 0);

            float nDieselvolume = 0, nCoalvolume = 0;
            float nDiesellevel = 0, nCoallevel = 0;
            float nDieselburned = 0, nCoalburned = 0;
            float nCoalBurnedPerc = 0, nWaterBurnedPerc = 0;
            List<string> cEnginetype = new List<string>();
            foreach (var item in cars)//Consist engines
            {
                if (item.EngineType == TrainCar.EngineTypes.Diesel)
                {//Fuel Diesel
                    nDieselvolume = nDieselvolume + (item as MSTSDieselLocomotive).MaxDieselLevelL;
                    nDiesellevel = nDiesellevel + (item as MSTSDieselLocomotive).DieselLevelL;
                    nDieselburned = nDieselvolume - nDiesellevel;
                    cEnginetype.Add("Diesel");
                    FuelType = FuelTypes.DieselOil;
                }

                if (item.EngineType == TrainCar.EngineTypes.Steam && item.AuxWagonType == "Engine")
                {//Fuel Steam
                    nCoalvolume = nCoalvolume + (item as MSTSSteamLocomotive).MaxTenderFuelMassKG;
                    nCoallevel = nCoallevel + (item as MSTSSteamLocomotive).TenderFuelMassKG;
                    nCoalburned = nCoalvolume - nCoallevel;
                    nCoalBurnedPerc = 1 - ((item as MSTSSteamLocomotive).TenderFuelMassKG / (item as MSTSSteamLocomotive).MaxTenderFuelMassKG);
                    cEnginetype.Add("Steam");

                    nWaterBurnedPerc = 1 - ((item as MSTSSteamLocomotive).CombinedTenderWaterVolumeUKG / (item as MSTSSteamLocomotive).MaxTotalCombinedWaterVolumeUKG);
                    FuelType = FuelTypes.Coal;
                }
                if (item.EngineType == TrainCar.EngineTypes.Electric)
                {
                    cEnginetype.Add("Electric");
                    FuelType = FuelTypes.Kwhr;
                }
            }
            //Consist engine type  
            int ncountdiesel = cEnginetype.Where(s => s == "Diesel").Count();
            int ncountsteam = cEnginetype.Where(s => s == "Steam").Count();
            int ncountelectric = cEnginetype.Where(s => s == "Electric").Count();
            labeltext = "  Consist engine=" + (ncountdiesel > 0 ? ncountdiesel + " " + Viewer.Catalog.GetString("Diesel.") + " " : "") + (ncountsteam > 0 ? ncountsteam + " " + Viewer.Catalog.GetString("Steam.") + " " : "") + (ncountelectric > 0 ? ncountelectric + " " + Viewer.Catalog.GetString("Electric.") : "");
            outmesssage(labeltext, colWidth * 3, true, 0);

            if (FuelType == FuelTypes.DieselOil)
            {
                labeltext = "  Burned Diesel=" + FormatStrings.FormatFuelVolume(nDieselburned, ismetric, isuk);
                outmesssage(labeltext, colWidth * 3, true, 0);
            }

            if (FuelType == FuelTypes.Coal)
            {
                labeltext = "  Burned Coal=" + FormatStrings.FormatMass(nCoalburned, ismetric) + " (" + nCoalBurnedPerc.ToString("0.##%") + ")";
                outmesssage(labeltext, colWidth * 3, true, 0);
                labeltext = "  Water consumption=" + nWaterBurnedPerc.ToString("0.##%");
                outmesssage(labeltext, colWidth * 3, true, 0);
            }

            line = scrollbox.AddLayoutHorizontalLineOfText();
            writeline();

            //Station Arrival, Departure, Passing Evaluation. 100.                              
            labeltext = "1-Station Arrival, Departure, Passing Evaluation:";
            outmesssagecolor(labeltext, colWidth, Color.Yellow, true, 6, 0);
            //Station
            double nstationmissed = 0;
            double nstationdelayed = 0;
            double nstationarrival = DbfEvalStationName.Count - dbfstationstopsremaining - nmissedstation;
            int ndepartbeforeboarding = ActivityTaskPassengerStopAt.DbfEvalDepartBeforeBoarding.Count;
            var stationmissed = "";

            if (DbfEvalStationName.Count > 0)
            {
                //Activity station stops
                labeltext = "  Station Arrival=" + nstationarrival.ToString();
                outmesssage(labeltext, colWidth * 4, true, 1);

                //Delayed. -0.2 per second. 
                if (playerTrain.Delay != null)
                {
                    nstationdelayed = 0.2 * (long)playerTrain.Delay.Value.TotalSeconds;//second
                    labeltext = "  Delay=" + FormatStrings.FormatTime(playerTrain.Delay.Value.TotalSeconds);
                    outmesssagecolor(labeltext, colWidth * 4, nmissedstation > 0 ? Color.LightSalmon : Color.White, true, 1, 0);
                }
                //Missed station stops. -20.
                line = scrollbox.AddLayoutHorizontalLineOfText();
                labeltext = "  Missed station stops=" + nmissedstation + (nmissedstation > 0 ? nmissedstation > 1 ? "    Stations" : "    Station" : "");
                outmesssagecolor(labeltext, colWidth * 4, nmissedstation > 0 ? Color.LightSalmon : Color.White, false, 1, 1);
                nstationmissed = 20 * nmissedstation;

                foreach (var item in DbfEvalActDepart)
                {
                    if (item.Value == "(missed)")
                    {
                        line = scrollbox.AddLayoutHorizontalLineOfText();
                        outmesssage("  ", colWidth * 5, false, 3);
                        labeltext = DbfEvalStationName[item.Key];
                        outmesssagecolor(labeltext, colWidth, Color.LightSalmon, false, 3, 1);
                        //store station missed name
                        stationmissed = stationmissed + labeltext + ", ";
                    }
                }

                //Station departure before passenger boarding completed. -80.                                
                labeltext = "  Departure before passenger boarding completed=" + ndepartbeforeboarding;
                outmesssage(labeltext, colWidth * 8, true, 1);
                ndepartbeforeboarding = 80 * ndepartbeforeboarding;
            }
            else
            {
                labeltext = "  No Station stops.";
                outmesssage(labeltext, colWidth, true, 1);
            }

            //Station Arrival, Departure, Passing Evaluation. Overall Rating.
            double nstationdelmisbef = nstationdelayed + nstationmissed + ndepartbeforeboarding;
            int nstatarrdeppaseval = DbfEvalStationName.Count != dbfstationstopsremaining && (nstationdelmisbef) <= 100 ? (nstationarrival == DbfEvalStationName.Count && nstationdelmisbef == 0) ? 100 : Convert.ToInt16(100 - nstationdelmisbef) : 0;
            labeltext = DbfEvalStationName.Count != dbfstationstopsremaining ? "  Overall rating total=" + nstatarrdeppaseval : "  Overall rating total=0";
            outmesssagecolor(labeltext, colWidth * 4, Color.Yellow, true, 1, 1);

            //Work orders. 100.
            colWidth = (cl.RemainingWidth - cl.TextHeight) / 7;
            line = scrollbox.AddLayoutHorizontalLineOfText();
            labeltext = "2-Work orders:";
            outmesssagecolor(labeltext, colWidth, Color.Yellow, true, 6, 0);
            //Work orders sheet.
            if (DbfEvalTaskName.Count > 0)
            {
                var widthValue = DbfEvalTaskName.ContainsValue(Viewer.Catalog.GetString("Assemble Train At Location")) ? 3 : 2;
                line = scrollbox.AddLayoutHorizontalLineOfText();
                colWidth = (scrollbox.RemainingWidth - cl.TextHeight) / 20;
                labeltext = "  Task";
                outmesssagecolor(labeltext, colWidth * 7, Color.Gray, false, widthValue, 0);
                labeltext = "Location";
                outmesssagecolor(labeltext, colWidth * 8, Color.Gray, false, 2, 0);
                labeltext = "Status";
                outmesssagecolor(labeltext, colWidth, Color.Gray, false, 2, 1);
                foreach (KeyValuePair<int, string> item in DbfEvalTaskName)
                {
                    line = scrollbox.AddLayoutHorizontalLineOfText();
                    labeltext = "  " + item.Value;
                    outmesssage(labeltext, colWidth * 7, false, widthValue);
                    labeltext = DbfEvalTaskLocation.ContainsKey(item.Key) && DbfEvalTaskLocation[item.Key] != "" ? DbfEvalTaskLocation[item.Key].ToString() : "-";
                    outmesssage(labeltext, colWidth * 8, false, 2);
                    labeltext = DbfEvalTaskStatus.ContainsKey(item.Key) && DbfEvalTaskStatus[item.Key] != "" ? DbfEvalTaskStatus[item.Key].ToString() : "-";
                    outmesssage(labeltext, colWidth, false, 2);
                    writeline();
                }
                //Coupling Over Speed > 1.5 MpS (5.4Kmh 3.3Mph)
                colWidth = (cl.RemainingWidth - cl.TextHeight) / 14;
                labeltext = "  Coupling speed limits=" + noverspeedcoupling.ToString();
                outmesssage(labeltext, colWidth * 4, true, 2);
            }
            else
            {
                labeltext = "  No Tasks.";
                outmesssage(labeltext, colWidth, true, 1);
            }
            //Work orders. 100. Overall Rating.
            colWidth = (cl.RemainingWidth - cl.TextHeight) / 14;
            int nworkorderseval = DbfEvalTaskName.Count > 0 ? ((100 / DbfEvalTaskName.Count) * ndbfEvalTaskAccomplished) - (noverspeedcoupling * 5) : 0;
            nworkorderseval = nworkorderseval > 0 ? nworkorderseval : 0;
            labeltext = "  Overall rating total=" + nworkorderseval;
            outmesssagecolor(labeltext, colWidth * 4, Color.Yellow, true, 2, 1);

            //----------------------
            //SPEED Evaluation. 100.
            //----------------------
            line = scrollbox.AddLayoutHorizontalLineOfText();
            labeltext = "3-Speed Evaluation:";
            outmesssagecolor(labeltext, colWidth, Color.Yellow, true, 6, 0);
            //Over Speed
            //Track Monitor Red.
            int nspeedred = TrackMonitor.DbfEvalOverSpeed;
            labeltext = "  Over Speed=" + nspeedred;
            outmesssage(labeltext, colWidth * 4, true, 2);
            //Over Speed Time.
            //Track Monitor Red. -1.5 per second. dbfEvalOverSpeedTimeS.
            double nspeedredtime = TrackMonitor.DbfEvalOverSpeedTimeS;
            labeltext = "  Over Speed (Time)=" + FormatStrings.FormatTime(nspeedredtime);
            outmesssage(labeltext, colWidth * 4, true, 2);
            nspeedredtime = 1.5 * nspeedredtime;

            //SPEED Evaluation. 100. Overall Rating.
            int nSpeedEval = Convert.ToInt16(100 - nspeedredtime);
            labeltext = "  Overall rating total=" + nSpeedEval;
            outmesssagecolor(labeltext, colWidth * 4, Color.Yellow, true, 2, 1);

            //-----------------------------------------------------
            //Freight Durability/Passenger Comfort Evaluation. 100.
            //-----------------------------------------------------
            line = scrollbox.AddLayoutHorizontalLineOfText();
            labeltext = "4-Freight Durability/Passenger Comfort Evaluation:";
            outmesssagecolor(labeltext, colWidth, Color.Yellow, true, 6, 0);

            //Durability                            
            //Curve speeds exceeded. -3. dbfEvalTravellingTooFast
            int ncurvespeedexceeded = TrainCar.DbfEvalTravellingTooFast;
            labeltext = lcurvespeeddependent ? "  Curve speeds exceeded=" + ncurvespeedexceeded : "  Curve dependent speed limit (Disabled)";
            outmesssage(labeltext, colWidth * 4, true, 4);
            ncurvespeedexceeded = 3 * ncurvespeedexceeded;

            //Hose Breaks.  -7. dbfEvalTravellingTooFastBrakeHose
            int nhosebreaks = TrainCar.DbfEvalTravellingTooFastSnappedBrakeHose;
            labeltext = lcurvespeeddependent ? "  Hose breaks=" + nhosebreaks : "  Curve dependent speed limit (Disabled)";
            outmesssage(labeltext, colWidth * 4, true, 4);
            nhosebreaks = 7 * nhosebreaks;

            //Coupler Breaks. -10.
            int ncouplerbreaks = Train.NumOfCouplerBreaks;
            labeltext = (lbreakcouplers ? "  Coupler breaks=" : "  Coupler overloaded=") + ncouplerbreaks;
            outmesssage(labeltext, colWidth * 4, true, 4);
            ncouplerbreaks = 10 * ncouplerbreaks;

            //Train Overturned. -20.
            int ntrainoverturned = TrainCar.DbfEvalTrainOverturned;
            labeltext = "  Train Overturned=" + ntrainoverturned;
            outmesssage(labeltext, colWidth * 4, true, 4);
            ntrainoverturned = 20 * ntrainoverturned;

            //Freight Durability/Passenger Comfort Evaluation. 100. Overall Rating.
            int nFdurPasconfEval = (100 - (ncurvespeedexceeded + nhosebreaks + ncouplerbreaks + ntrainoverturned > 100 ? 100 : ncurvespeedexceeded + nhosebreaks + ncouplerbreaks + ntrainoverturned));
            labeltext = "  Overall rating total=" + nFdurPasconfEval;
            outmesssagecolor(labeltext, colWidth * 4, Color.Yellow, true, 4, 1);

            //------------------------------------------
            //EMERGENCY/PENALTY Actions Evaluation. 100.
            //------------------------------------------
            line = scrollbox.AddLayoutHorizontalLineOfText();
            labeltext = "5-Emergency/Penalty Actions Evaluation:";
            outmesssagecolor(labeltext, colWidth, Color.Yellow, true, 6, 0);

            //Full Brake applications under 5MPH / 8KMH. 2.
            int nfullbrakeappunder8kmh = Simulation.RollingStocks.MSTSLocomotive.DbfEvalFullTrainBrakeUnder8kmh;
            labeltext = "  Full Train Brake applications under 5MPH/8KMH=" + nfullbrakeappunder8kmh;
            outmesssage(labeltext, colWidth * 8, true, 5);
            nfullbrakeappunder8kmh = 2 * nfullbrakeappunder8kmh;

            //Emergency applications MOVING. 20.
            int nebpbmoving = RollingStock.MSTSLocomotiveViewer.DbfEvalEBPBmoving;
            labeltext = "  Emergency applications while moving=" + nebpbmoving;
            outmesssage(labeltext, colWidth * 8, true, 5);
            nebpbmoving = 20 * nebpbmoving;

            //Emergency applications while STOPPED. 5.
            int nebpbstopped = RollingStock.MSTSLocomotiveViewer.DbfEvalEBPBstopped;
            labeltext = "  Emergency applications while stopped=" + nebpbstopped;
            outmesssage(labeltext, colWidth * 8, true, 5);
            nebpbstopped = 5 * nebpbstopped;

            //Alerter Penalty applications above 16KMH ~ 10MPH. 35.                            
            int nfullbrakeabove16kmh = Simulation.RollingStocks.SubSystems.ScriptedTrainControlSystem.DbfevalFullBrakeAbove16kmh;
            labeltext = "  Alerter applications above 10MPH/16KMH=" + nfullbrakeabove16kmh;
            outmesssage(labeltext, colWidth * 8, true, 5);
            nfullbrakeabove16kmh = 35 * nfullbrakeabove16kmh;

            //Emergency/Penalty Actions Evaluation. 100. Overall Rating.
            int nepactionseval = nebpbstopped + nebpbmoving + nfullbrakeappunder8kmh + nfullbrakeabove16kmh;
            nepactionseval = 100 - (nepactionseval > 100 ? 100 : nepactionseval);
            labeltext = "  Overall rating total=" + nepactionseval;
            outmesssagecolor(labeltext, colWidth * 8, Color.Yellow, true, 5, 1);
            line = scrollbox.AddLayoutHorizontalLineOfText();
            line = scrollbox.AddLayoutHorizontalLineOfText();

            //-------------------------
            //R A T I N G  &  S T A R S
            //-------------------------
            line.AddHorizontalSeparator();

            colWidth = (cl.RemainingWidth - cl.TextHeight) / 7;
            line = scrollbox.AddLayoutHorizontalLineOfText();
            labeltext = "Rating & Stars";
            outmesssagecolorcenter(labeltext, colWidth * 7, Color.Yellow, true);
            labeltext = "****************";
            outmesssagecolorcenter(labeltext, colWidth * 7, Color.Yellow, true);
            writeline();
            line = scrollbox.AddLayoutHorizontalLineOfText();
            line = scrollbox.AddLayoutHorizontalLineOfText();

            //Station Arrival, Departure, Passing Evaluation. 100
            labeltext = "1- Station Arrival, Departure, Passing Evaluation=" + (nstatarrdeppaseval > 0 ? DrawStar(nstatarrdeppaseval) : " .");
            outmesssagecolor(labeltext, colWidth * 4, Color.Yellow, false, 6, 1);
            line = scrollbox.AddLayoutHorizontalLineOfText();
            line = scrollbox.AddLayoutHorizontalLineOfText();

            //Work orders Evaluation. 100                            
            labeltext = "2- Work orders Evaluation=" + (nworkorderseval > 0 ? DrawStar(nworkorderseval) : " .");
            outmesssagecolor(labeltext, colWidth * 4, Color.Yellow, false, 6, 1);
            line = scrollbox.AddLayoutHorizontalLineOfText();
            line = scrollbox.AddLayoutHorizontalLineOfText();

            //SPEED Evaluation. 100                            
            labeltext = "3- Speed Evaluation=" + (nSpeedEval > 0 ? DrawStar(nSpeedEval) : " .");
            outmesssagecolor(labeltext, colWidth * 4, Color.Yellow, false, 6, 1);
            line = scrollbox.AddLayoutHorizontalLineOfText();
            line = scrollbox.AddLayoutHorizontalLineOfText();

            //Freight Durability/Passenger Comfort Evaluation. 100.
            labeltext = "4- Freight Durability/Passenger Comfort Evaluation=" + (nFdurPasconfEval > 0 ? DrawStar(nFdurPasconfEval) : " .");
            outmesssagecolor(labeltext, colWidth * 4, Color.Yellow, false, 6, 1);
            line = scrollbox.AddLayoutHorizontalLineOfText();
            line = scrollbox.AddLayoutHorizontalLineOfText();

            //Emergency/Penalty Actions Evaluation. 100.
            labeltext = "5- Emergency/Penalty Actions Evaluation=" + (nepactionseval > 0 ? DrawStar(nepactionseval) : " .");
            outmesssagecolor(labeltext, colWidth * 4, Color.Yellow, false, 6, 1);
            line = scrollbox.AddLayoutHorizontalLineOfText();
            line = scrollbox.AddLayoutHorizontalLineOfText();

            labeltext = "";
            line.AddHorizontalSeparator();
            line = scrollbox.AddLayoutHorizontalLineOfText();

            writeline();
            LogSeparator(80);
            writeline();

            if (!lDebriefEvalFile)
            {
                lDebriefEvalFile = true;
                wDbfEval.Close();
                System.Diagnostics.Process.Start("notepad.exe", Program.EvaluationFilename);    //Show debrief eval file
            }
        }

        private void ShowEvaluation(TrainCar locomotive, int nmissedstation, int dbfstationstopsremaining, Train playerTrain, int colWidth, bool lcurvespeeddependent, bool lbreakcouplers, int ndbfEvalTaskAccomplished)
        {
            DbfEvalValues.Add("Train Overturned", TrainCar.DbfEvalTrainOverturned);
            DbfEvalValues.Add("Alerter applications above 10MPH/16KMH", Simulation.RollingStocks.SubSystems.ScriptedTrainControlSystem.DbfevalFullBrakeAbove16kmh);
            DbfEvalValues.Add("Auto pilot (Time)", Viewer.DbfEvalAutoPilotTimeS);
            DbfEvalValues.Add(lbreakcouplers ? "Coupler breaks" : "Coupler overloaded", Train.NumOfCouplerBreaks);
            DbfEvalValues.Add("Coupling speed limits", Simulator.DbfEvalOverSpeedCoupling);
            DbfEvalValues.Add(lcurvespeeddependent ? "Curve speeds exceeded" : "Curve dependent speed limit (Disabled)", lcurvespeeddependent ? TrainCar.DbfEvalTravellingTooFast : 0);
            if (playerTrain.Delay != null) DbfEvalValues.Add("Activity, current delay", (long)playerTrain.Delay.Value.TotalMinutes);

            DbfEvalValues.Add("Departure before passenger boarding completed", ActivityTaskPassengerStopAt.DbfEvalDepartBeforeBoarding.Count);
            DbfEvalValues.Add("Distance travelled", DbfEvalDistanceTravelled + locomotive.DistanceM);
            DbfEvalValues.Add("Emergency applications while moving", RollingStock.MSTSLocomotiveViewer.DbfEvalEBPBmoving);
            DbfEvalValues.Add("Emergency applications while stopped", RollingStock.MSTSLocomotiveViewer.DbfEvalEBPBstopped);
            DbfEvalValues.Add("Full Train Brake applications under 5MPH/8KMH", MSTSLocomotive.DbfEvalFullTrainBrakeUnder8kmh);
            if (lcurvespeeddependent) DbfEvalValues.Add("Hose breaks", TrainCar.DbfEvalTravellingTooFastSnappedBrakeHose);

            DbfEvalValues.Add("Over Speed", TrackMonitor.DbfEvalOverSpeed);
            DbfEvalValues.Add("Over Speed (Time)", TrackMonitor.DbfEvalOverSpeedTimeS);
            if (DbfEvalStationName.Count > 0)
            {
                DbfEvalValues.Add("Station stops missed", nmissedstation);
                DbfEvalValues.Add("Station stops remaining", dbfstationstopsremaining);
            }
            if (DbfEvalTaskName.Count > 0)
            {
                DbfEvalValues.Add((Viewer.Catalog.GetPluralStringFmt("Task", "Tasks", DbfEvalTaskName.Count)), DbfEvalTaskName.Count);
                DbfEvalValues.Add((Viewer.Catalog.GetPluralStringFmt("Task accomplished", "Tasks accomplished", ndbfEvalTaskAccomplished)), ndbfEvalTaskAccomplished);
            }
            //TO DO: water consumption is not ready.
            //DbfEvalValues.Add("Water consumption", MSTSSteamLocomotive.DbfEvalCumulativeWaterConsumptionLbs);
            var train = Program.Viewer.PlayerLocomotive.Train;//Debrief Eval
            train.DbfEvalValueChanged = true;

            line = scrollbox.AddLayoutHorizontalLineOfText();

            //List sorted by Key
            foreach (KeyValuePair<string, double> pair in DbfEvalValues.OrderBy(i => i.Key))
            {
                line.Add(new Label(colWidth * 4, line.RemainingHeight, Viewer.Catalog.GetString("- " + pair.Key)));
                line.Add(new Label(colWidth, line.RemainingHeight, Viewer.Catalog.GetString("= " + (pair.Key.Contains("Time") ? FormatStrings.FormatTime(pair.Value) : pair.Key.Contains("Activity") ? (Viewer.Catalog.GetPluralStringFmt("{0} minute", "{0} minutes", (long)playerTrain.Delay.Value.TotalMinutes)) : pair.Key.Contains("Distance") ? FormatStrings.FormatDistanceDisplay(Convert.ToSingle(pair.Value), locomotive.IsMetric) : pair.Value.ToString()))));

                line = scrollbox.AddLayoutHorizontalLineOfText();
            }

            line.AddHorizontalSeparator();
        }

        private void status_Click(Control arg1, Point arg2)
        {
            if (actualStatusVisible)
                actualStatusVisible = false;
            else
                actualStatusVisible = true;

            Layout();
        }

        string DrawStar(int value)
        {
            string star;
            string starBlack = " ★ ★ ★ ★ ★";
            string starWhite = " ☆ ☆ ☆ ☆ ☆";

            star = (starBlack.Substring(0, value / 10).ToString() + starWhite.Substring(value / 10, 10 - (value/10)).ToString());
            return star;
        }

        private void outmesssagecolorcenter( string text, int colW, Color color, bool lScroll)
        {
            if (lScroll)
            {
                line = scrollbox.AddLayoutHorizontalLineOfText();
                if (!lDebriefEvalFile) wDbfEval.WriteLine(text.PadLeft(40 + text.Length / 2));
            }
            else
                if (!lDebriefEvalFile) wDbfEval.Write(text.PadLeft(40 + text.Length / 2));

            line.Add(indicator = new Label(colW, line.RemainingHeight, Viewer.Catalog.GetString(text), LabelAlignment.Center));
            indicator.Color =color;
        }    
        
        private void outmesssagecolor(string text, int colW, Color color, bool bScroll, int nmargin, int nwriteline)
        {
            string[] atext = text.Split('=');
            int[] nvalmargin = { 16, 22, 23, 30, 33, 48, 50 };

            if (bScroll)
            {
                line = scrollbox.AddLayoutHorizontalLineOfText();
                if (!lDebriefEvalFile) wDbfEval.WriteLine(atext.Length > 1 ? atext[0].PadRight(nvalmargin[nmargin]) + " = " + atext[1]: text);
            }
            else
            {
                if (!lDebriefEvalFile) wDbfEval.Write(atext.Length > 1 ? atext[0].PadRight(nvalmargin[nmargin]) + " = " + atext[1]: text.PadRight(nvalmargin[nmargin]));
            }

            if (!lDebriefEvalFile)
            {
                for (int i = 0; i < nwriteline; i++)
                    writeline();
            }

            if (atext.Length > 1)
            {
                line.Add(indicator = new Label(colW, line.RemainingHeight, Viewer.Catalog.GetString(atext[0])));
                indicator.Color = color;
                line.Add(indicator = new Label(colW, line.RemainingHeight, Viewer.Catalog.GetString(" = " + atext[1])));
                indicator.Color = color;
            }
            else
            {
                line.Add(indicator = new Label(colW, line.RemainingHeight, Viewer.Catalog.GetString(text)));
                indicator.Color = color;
            }
        }

        private void outmesssage(string text, int colW, bool bScroll, int nmargin)
        {
            string[] atext = text.Split('=');
            int[] nvalmargin = { 16, 22, 23, 30, 33, 48, 50 };

            if (bScroll)
            {
                line = scrollbox.AddLayoutHorizontalLineOfText();
                if (!lDebriefEvalFile)
                    wDbfEval.WriteLine(atext.Length > 1 ? atext[0].PadRight(nvalmargin[nmargin]) + " = " + atext[1] : text.PadRight(nvalmargin[nmargin]));
            }
            else
                if (!lDebriefEvalFile) wDbfEval.Write(text.PadRight(nvalmargin[nmargin]));

            if (atext.Length > 1)
            {
                line.Add(new Label(colW, line.RemainingHeight, Viewer.Catalog.GetString(atext[0])));
                line.Add(new Label(colW, line.RemainingHeight, Viewer.Catalog.GetString(" = " + atext[1])));
            }
            else
            {
                line.Add(new Label(colW, line.RemainingHeight, Viewer.Catalog.GetString(text)));
            }
        }
        
        private void consolewltext(string text)
        {
            if (!lDebriefEvalFile) wDbfEval.WriteLine(text + ".");
        }

        private void writeline()
        {
            if (!lDebriefEvalFile) wDbfEval.WriteLine();
        }

        /// <summary>
        /// Add aggregated info to the Train Info layout.
        /// Loops through the train cars and aggregates the info.
        /// </summary>
        private void AddAggregatedTrainInfo(Train playerTrain, ControlLayout scrollbox, int labelWidth)
        {
            string numEngines = ""; // using "+" between DPU sets
            int numCars = 0;
            int numAxles = 0;
            var totMassKg = playerTrain.MassKg;
            float trailingMassKg = 0f;
            string sectionMass = ""; // using "  +  " between wagon sets
            var lengthM = playerTrain.Length;
            var maxSpeedMps = playerTrain.TrainMaxSpeedMpS;
            float totPowerW = 0f;
            float totMaxTractiveEffortN = 0f;
            float totMaxContTractiveEffortN = 0f;
            string sectionMaxContTractiveForce = ""; // using "  +  " between DPU sets
            float totMaxDynamicBrakeForceN = 0f;
            string sectionMaxDynamicBrakeForce = ""; // using "  +  " between DPU sets
            float maxBrakeForceN = 0f;
            float lowestCouplerStrengthN = 9.999e8f;  // impossible high force
            float lowestDerailForceN = 9.999e8f;  // impossible high force

            int engCount = 0; string countSeparator = ""; // when set, indicates that subsequent engines are in a separate block
            float engMaxContTractiveForceN = 0f; float engMaxDynBrakeForceN = 0f; string forceSeparator = "";  // when set, indicates that subsequent engines are in a separate block

            float wagSectionMassKg = 0f; string massSeparator = ""; // when set, indicates that subsequent engines are in a separate block
            int numOperativeBrakes = 0;
            bool isMetric = false; bool isUK = false;  // isImperial* does not seem to be used in simulation

            if (TrainInfoSpriteSheet == null) { TrainInfoSpriteSheet = SharedTextureManager.LoadInternal(Owner.Viewer.RenderProcess.GraphicsDevice, Path.Combine(Owner.Viewer.ContentPath, "TrainInfoSprites.png")); }
            const int spriteWidth = 6; const int spriteHeight = 26;
            var carInfoList = new List<CarInfo>(playerTrain.Cars.Count);

            foreach (var car in playerTrain.Cars)
            {
                // ignore (legacy) EOT
                if (car.WagonType == TrainCar.WagonTypes.EOT || car.CarLengthM < 1.1f) { continue; }

                var wag = car is MSTSWagon ? (MSTSWagon)car : null;
                var eng = car is MSTSLocomotive ? (MSTSLocomotive)car : null;

                var isEng = (car.WagonType == TrainCar.WagonTypes.Engine && eng != null && eng.MaxForceN > 25000);  // count legacy driving trailers as wagons

                if (car.IsMetric) { isMetric = true; }; if (car.IsUK) { isUK = true; }

                if (isEng)
                {
                    engCount++;
                    numAxles += eng.LocoNumDrvAxles + eng.GetWagonNumAxles();
                    totPowerW += eng.MaxPowerW;
                    totMaxTractiveEffortN += eng.MaxForceN;
                    totMaxContTractiveEffortN += eng.MaxContinuousForceN > 0 ? eng.MaxContinuousForceN : eng.MaxForceN;
                    engMaxContTractiveForceN += eng.MaxContinuousForceN > 0 ? eng.MaxContinuousForceN : eng.MaxForceN;
                    totMaxDynamicBrakeForceN += eng.MaxDynamicBrakeForceN;
                    engMaxDynBrakeForceN += eng.MaxDynamicBrakeForceN;

                    // hanlde transition from wagons to engines
                    if (wagSectionMassKg > 0)
                    {
                        sectionMass += massSeparator + FormatStrings.FormatLargeMass(wagSectionMassKg, isMetric, isUK);
                        wagSectionMassKg = 0f; massSeparator = "  +  ";
                    }
                }
                else if (wag != null)
                {
                    numCars++;
                    numAxles += wag.GetWagonNumAxles();
                    wagSectionMassKg += wag.MassKG;
                    trailingMassKg += wag.MassKG;
                    if (wag.MaxBrakeForceN > 0 && car.BrakeSystem != null && !(car.BrakeSystem is SingleTransferPipe) && !(car.BrakeSystem is ManualBraking)) { numOperativeBrakes++; }

                    // handle transition from engines to wagons
                    if (engCount > 0 || (engCount == 0 && numCars == 0))
                    {
                        numEngines += countSeparator + engCount.ToString();
                        engCount = 0; countSeparator = "+";
                        sectionMaxContTractiveForce += forceSeparator + FormatStrings.FormatForce(engMaxContTractiveForceN, isMetric);
                        sectionMaxDynamicBrakeForce += forceSeparator + FormatStrings.FormatForce(engMaxDynBrakeForceN, isMetric);
                        engMaxContTractiveForceN = 0f; engMaxDynBrakeForceN = 0f;  forceSeparator = "  +  ";
                    }
                }

                // wag and eng
                if (wag != null)
                {
                    maxBrakeForceN += wag.MaxBrakeForceN;
                    var couplerStrength = GetMinCouplerStrength(wag);
                    if (couplerStrength < lowestCouplerStrengthN) { lowestCouplerStrengthN = couplerStrength; }
                    var derailForce = GetDerailForce(wag);
                    if (derailForce < lowestDerailForceN) { lowestDerailForceN = derailForce; }

                    carInfoList.Add(new CarInfo(wag.MassKG, isEng));
                }
            }

            if (engCount > 0) { numEngines = numEngines + countSeparator + engCount.ToString(); }
            if (String.IsNullOrEmpty(numEngines)) { numEngines = "0"; }
            if (engMaxContTractiveForceN > 0) { sectionMaxContTractiveForce += forceSeparator + FormatStrings.FormatForce(engMaxContTractiveForceN, isMetric); }
            if (engMaxDynBrakeForceN > 0) { sectionMaxDynamicBrakeForce += forceSeparator + FormatStrings.FormatForce(engMaxDynBrakeForceN, isMetric); }
            if (wagSectionMassKg > 0) { sectionMass += massSeparator + FormatStrings.FormatLargeMass(wagSectionMassKg, isMetric, isUK); }

            var line = scrollbox.AddLayoutHorizontalLineOfText();
            line.Add(new Label(labelWidth, line.RemainingHeight, Viewer.Catalog.GetString("Number of Engines:"), LabelAlignment.Left));
            line.Add(new Label(line.RemainingWidth, line.RemainingHeight, numEngines, LabelAlignment.Left));

            line = scrollbox.AddLayoutHorizontalLineOfText();
            line.Add(new Label(labelWidth, line.RemainingHeight, Viewer.Catalog.GetString("Number of Cars:"), LabelAlignment.Left));
            line.Add(new Label(line.RemainingWidth, line.RemainingHeight, numCars.ToString(), LabelAlignment.Left));

            line = scrollbox.AddLayoutHorizontalLineOfText();
            line.Add(new Label(labelWidth, line.RemainingHeight, Viewer.Catalog.GetString("Number of Axles:"), LabelAlignment.Left));
            line.Add(new Label(line.RemainingWidth, line.RemainingHeight, numAxles.ToString(), LabelAlignment.Left));

            line = scrollbox.AddLayoutHorizontalLineOfText();
            line.Add(new Label(labelWidth, line.RemainingHeight, Viewer.Catalog.GetString("Total Weight:"), LabelAlignment.Left));
            string massValue = FormatStrings.FormatLargeMass(totMassKg, isMetric, isUK) + "    (" + sectionMass + ")";
            line.Add(new Label(line.RemainingWidth, line.RemainingHeight, massValue, LabelAlignment.Left));

            line = scrollbox.AddLayoutHorizontalLineOfText();
            line.Add(new Label(labelWidth, line.RemainingHeight, Viewer.Catalog.GetString("Total Length:"), LabelAlignment.Left));
            line.Add(new Label(line.RemainingWidth, line.RemainingHeight, FormatStrings.FormatShortDistanceDisplay(lengthM, isMetric), LabelAlignment.Left));

            line = scrollbox.AddLayoutHorizontalLineOfText();
            line.Add(new Label(labelWidth, line.RemainingHeight, Viewer.Catalog.GetString("Maximum Speed:"), LabelAlignment.Left));
            line.Add(new Label(line.RemainingWidth, line.RemainingHeight, FormatStrings.FormatSpeedLimit(maxSpeedMps, isMetric), LabelAlignment.Left));

            scrollbox.AddHorizontalSeparator();

            line = scrollbox.AddLayoutHorizontalLineOfText();
            line.Add(new Label(labelWidth, line.RemainingHeight, Viewer.Catalog.GetString("Max Power:"), LabelAlignment.Left));
            line.Add(new Label(line.RemainingWidth, line.RemainingHeight, FormatStrings.FormatPower(totPowerW, isMetric, false, false), LabelAlignment.Left));

            line = scrollbox.AddLayoutHorizontalLineOfText();
            line.Add(new Label(labelWidth, line.RemainingHeight, Viewer.Catalog.GetString("Max Tractive Effort:"), LabelAlignment.Left));
            line.Add(new Label(line.RemainingWidth, line.RemainingHeight, FormatStrings.FormatForce(totMaxTractiveEffortN, isMetric), LabelAlignment.Left));

            line = scrollbox.AddLayoutHorizontalLineOfText();
            line.Add(new Label(labelWidth, line.RemainingHeight, Viewer.Catalog.GetString("Max Continuous Tractive Effort:"), LabelAlignment.Left));
            line.Add(new Label(line.RemainingWidth, line.RemainingHeight, sectionMaxContTractiveForce, LabelAlignment.Left));

            line = scrollbox.AddLayoutHorizontalLineOfText();
            line.Add(new Label(labelWidth, line.RemainingHeight, Viewer.Catalog.GetString("Max Dynamic Brake Force:"), LabelAlignment.Left));
            line.Add(new Label(line.RemainingWidth, line.RemainingHeight, sectionMaxDynamicBrakeForce, LabelAlignment.Left));

            if (!isMetric)
            {
                float hpt = trailingMassKg > 0f ? W.ToHp(totPowerW) / Kg.ToTUS(trailingMassKg) : 0f;
                line = scrollbox.AddLayoutHorizontalLineOfText();
                line.Add(new Label(labelWidth, line.RemainingHeight, Viewer.Catalog.GetString("Horespower per Ton:"), LabelAlignment.Left));
                line.Add(new Label(line.RemainingWidth, line.RemainingHeight, string.Format("{0:0.0}", hpt), LabelAlignment.Left));

                float tpob = numOperativeBrakes > 0 ? Kg.ToTUS(trailingMassKg) / numOperativeBrakes : 0;
                line = scrollbox.AddLayoutHorizontalLineOfText();
                line.Add(new Label(labelWidth, line.RemainingHeight, Viewer.Catalog.GetString("Tons per Operative Brake:"), LabelAlignment.Left));
                line.Add(new Label(line.RemainingWidth, line.RemainingHeight, string.Format("{0:0}", tpob), LabelAlignment.Left));

                // The next two metrics are based on UP's equivalent powered axles and equivalent dynamic brake axles. These are based on
                // tractive effort, and thus provide a more accurate metric than horsepower per ton. UP uses 10k lbf for a standard axle.
                // TODO: EPA and EDBA should really be defined in the eng file, and not calculated here.
                // TODO: It would be great to also show the minimum TpEPA and TpEDBA for the current train-path. But that is hard to calculate,
                //       and should really be defined in the path file.

                // tons per equivalent powered axle (TPA or TpEPA)
                float tpepa = totMaxContTractiveEffortN > 0 ? Kg.ToTUS(trailingMassKg) / (N.ToLbf(totMaxContTractiveEffortN) / 10000f) : 0;
                line = scrollbox.AddLayoutHorizontalLineOfText();
                line.Add(new Label(labelWidth, line.RemainingHeight, Viewer.Catalog.GetString("Tons per EPA:"), LabelAlignment.Left));
                line.Add(new Label(line.RemainingWidth, line.RemainingHeight, string.Format("{0:0}", tpepa), LabelAlignment.Left));

                // tons per equivalent dynamic brake axle (TpEDBA)
                float tpedba = totMaxDynamicBrakeForceN > 0 ? Kg.ToTUS(trailingMassKg) / (N.ToLbf(totMaxDynamicBrakeForceN) / 10000f) : 0;
                line = scrollbox.AddLayoutHorizontalLineOfText();
                line.Add(new Label(labelWidth, line.RemainingHeight, Viewer.Catalog.GetString("Tons per EDBA:"), LabelAlignment.Left));
                line.Add(new Label(line.RemainingWidth, line.RemainingHeight, string.Format("{0:0}", tpedba), LabelAlignment.Left));
            }

            scrollbox.AddHorizontalSeparator();

            line = scrollbox.AddLayoutHorizontalLineOfText();
            line.Add(new Label(labelWidth, line.RemainingHeight, Viewer.Catalog.GetString("Lowest Coupler Strength:"), LabelAlignment.Left));
            line.Add(new Label(line.RemainingWidth, line.RemainingHeight, FormatStrings.FormatForce(lowestCouplerStrengthN, isMetric), LabelAlignment.Left));

            line = scrollbox.AddLayoutHorizontalLineOfText();
            line.Add(new Label(labelWidth, line.RemainingHeight, Viewer.Catalog.GetString("Lowest Derail Force:"), LabelAlignment.Left));
            line.Add(new Label(line.RemainingWidth, line.RemainingHeight, FormatStrings.FormatForce(lowestDerailForceN, isMetric), LabelAlignment.Left));

            scrollbox.AddHorizontalSeparator();

            // weight graph
            line = scrollbox.AddLayoutHorizontalLineOfText();
            line.Add(new Label(line.RemainingWidth, line.RemainingHeight, Viewer.Catalog.GetString("Car Weight (front on left):"), LabelAlignment.Left));
            scrollbox.AddSpace(scrollbox.RemainingWidth, 2);
            var hscrollbox = scrollbox.AddLayoutScrollboxHorizontal(SystemInformation.HorizontalScrollBarHeight + spriteHeight + 2);
            var vbox = hscrollbox.AddLayoutVertical(carInfoList.Count * spriteWidth + 2);
            var weightbox = vbox.AddLayoutHorizontal(spriteHeight + 2);
            foreach (var car in carInfoList)
            {
                int spriteIdx = (int)Math.Floor(car.MassKg / 15000f);
                if (spriteIdx < 0) { spriteIdx = 0; } else if (spriteIdx > 11) { spriteIdx = 11; }
                var image = new Image(spriteWidth, spriteHeight);
                if (car.IsEngine) { image.Source = new Rectangle(spriteIdx * spriteWidth, 0, spriteWidth, spriteHeight); }
                else { image.Source = new Rectangle(spriteIdx * spriteWidth, spriteHeight, spriteWidth, spriteHeight); }
                image.Texture = TrainInfoSpriteSheet;
                weightbox.Add(image);
            }
        }

        /// <summary>
        /// Get the lowest coupler strength for a car.
        /// </summary>
        private float GetMinCouplerStrength(MSTSWagon wag)
        {
            float couplerStrength = 1e10f;  // default from TrainCar.GetCouplerBreak2N()
            foreach (var coupler in wag.Couplers)
            {
                // ignore unrealistically low values; some cars have Break2N lower than Break1N
                if (coupler.Break1N > 99f && coupler.Break1N < couplerStrength) { couplerStrength = coupler.Break1N; }
                if (coupler.Break2N > 99f && coupler.Break2N < couplerStrength) { couplerStrength = coupler.Break2N; }
            }
            return couplerStrength;
        }

        /// <summary>
        /// Calculate the lowest force it takes to derail the car (on a curve).
        /// It is equivalent to the vertical force at the wheel.
        /// </summary>
        private float GetDerailForce(MSTSWagon wag)
        {
            // see TotalWagonVerticalDerailForceN in TrainCar.cs
            float derailForce = 2.0e5f;  // 45k lbf on wheel
            int numWheels = wag.GetWagonNumAxles() * 2;
            if (numWheels > 2) { derailForce = wag.MassKG / numWheels * wag.GetGravitationalAccelerationMpS2(); }
            return derailForce;
        }

        public override void TabAction()
        {
            ActiveTab = (ActiveTab + 1) % Tabs.Count;
            Layout();
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            int scrollPosition = 0;
            if (Tabs[ActiveTab].Vbox != null)
            {
                foreach (var control in Tabs[ActiveTab].Vbox.Controls)
                {
                    if (control is ControlLayoutScrollboxVertical controlLayoutScrollboxVertical)
                    {
                        scrollPosition = controlLayoutScrollboxVertical.GetScrollPosition();
                    }
                }
            }

            var vbox = base.Layout(layout).AddLayoutVertical();

            if (Tabs.Count > 0)
            {
                var hbox = vbox.AddLayoutHorizontalLineOfText();
                var tabWidth = hbox.RemainingWidth / Tabs.Count;
                for (var i = 0; i < Tabs.Count; i++)
                {
                    var label = new Label(tabWidth, hbox.RemainingHeight, Tabs[i].TabLabel, LabelAlignment.Center) { Color = ActiveTab == i ? Color.White : Color.Gray, Tag = i };
                    label.Click += label_Click;
                    hbox.Add(label);
                    
                }
                vbox.AddHorizontalSeparator();
                Tabs[ActiveTab].Layout(vbox);

                foreach (var control in vbox.Controls)
                {
                    if (control is ControlLayoutScrollboxVertical controlLayoutScrollboxVertical)
                    {
                        controlLayoutScrollboxVertical.SetScrollPosition(scrollPosition);
                    }
                }
            }

            Tabs[ActiveTab].Vbox = vbox;
            return vbox;
        }

        void label_Click(Control control, Point point)
        {
            ActiveTab = (int)control.Tag;
            Layout();
        }

        enum Tab
        {
            KeyboardShortcuts,
            ActivityBriefing,
            ActivityTimetable,
            ActivityWorkOrders,
            ActivityEvaluation,
            TimetableBriefing,
            LocomotiveProcedures,
            TrainInfo,
        }

        class TabData
        {
            public readonly Tab Tab;
            public readonly string TabLabel;
            public readonly Action<ControlLayout> Layout;
            public Orts.Viewer3D.Popups.ControlLayoutVertical Vbox;

            public TabData(Tab tab, string tabLabel, Action<ControlLayout> layout)
            {
                Tab = tab;
                TabLabel = tabLabel;
                Layout = layout;
                Vbox = null;
            }
        }

        
        ActivityTask LastActivityTask;
        bool StoppedAt;
        
        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            // Uncomment this statement to reduce framerate during play for testing
            //System.Threading.Thread.Sleep(40); // Press F1 to force framerate below 25 fps

            base.PrepareFrame(elapsedTime, updateFull);

            if (updateFull && (Tabs[ActiveTab].Tab == Tab.ActivityTimetable | Tabs[ActiveTab].Tab == Tab.ActivityEvaluation) && Owner.Viewer.Simulator.ActivityRun != null)
            {
                if (LastActivityTask != Owner.Viewer.Simulator.ActivityRun.Current || StoppedAt != GetStoppedAt(LastActivityTask))
                {
                    LastActivityTask = Owner.Viewer.Simulator.ActivityRun.Current;
                    StoppedAt = GetStoppedAt(LastActivityTask);
                    Layout();
                }
            }
            else if (updateFull && (Tabs[ActiveTab].Tab == Tab.ActivityWorkOrders | Tabs[ActiveTab].Tab == Tab.ActivityEvaluation) && Owner.Viewer.Simulator.ActivityRun != null)
            {
                if (Owner.Viewer.Simulator.ActivityRun.EventList != null)
                {
                    if (Owner.Viewer.Simulator.ActivityRun.LastTriggeredEvent != null && (Owner.Viewer.HelpWindow.lastLastEventID == -1 ||
                        (Owner.Viewer.Simulator.ActivityRun.LastTriggeredEvent.ParsedObject.ID != Owner.Viewer.HelpWindow.lastLastEventID)))
                    {
                        lastLastEventID = Owner.Viewer.Simulator.ActivityRun.LastTriggeredEvent.ParsedObject.ID;
                        Layout();
                    }
                }
            }
            else if (Tabs[ActiveTab].Tab == Tab.ActivityEvaluation && Owner.Viewer.Simulator.ActivityRun != null && !dbfevalActivityEnded)//Debrief Eval
            {

                var train = Program.Viewer.PlayerLocomotive.Train;//Debrief Eval
                if (train.DbfEvalValueChanged)//Debrief Eval
                {
                    train.DbfEvalValueChanged = false;//Debrief Eval
                    Layout();
                }
            }
            else if (Tabs[ActiveTab].Tab == Tab.TrainInfo && Owner.Viewer.PlayerTrain != null)
            {
                if (LastPlayerTrain == null || Owner.Viewer.PlayerTrain != LastPlayerTrain || LastPlayerTrainCarCount != Owner.Viewer.PlayerTrain.Cars.Count)
                {
                    LastPlayerTrain = Owner.Viewer.PlayerTrain; LastPlayerTrainCarCount = Owner.Viewer.PlayerTrain.Cars.Count;
                    Layout();
                }
            }
            if (this.ActivityUpdated == true) //true value is set in ActivityWindow.cs
            {
                this.ActivityUpdated = false;
                Layout();
            }
            //UpdateActivityStatus();
        }

        static bool GetStoppedAt(ActivityTask task)
        {
            var stopAtTask = task as ActivityTaskPassengerStopAt;
            if (stopAtTask != null)
                return stopAtTask.ActArrive != null;
            return false;
        }
    }

    class Key : Label
    {
        public Color KeyColor;

        public Key(int x, int y, int width, int height, string text, Color keyColor)
            : base(x, y, width, height, text, LabelAlignment.Center)
        {
            KeyColor = keyColor;
        }

        internal override void Draw(SpriteBatch spriteBatch, Point offset)
        {
            spriteBatch.Draw(WindowManager.WhiteTexture, new Rectangle(offset.X + Position.Left, offset.Y + Position.Top, Position.Width, Position.Height), Color.White);
            spriteBatch.Draw(WindowManager.WhiteTexture, new Rectangle(offset.X + Position.Left + 1, offset.Y + Position.Top + 1, Position.Width - 2, Position.Height - 2), KeyColor);
            base.Draw(spriteBatch, offset);
        }
    }
}
