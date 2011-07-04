// COPYRIGHT 2010, 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;

namespace ORTS.Popups
{
	public class HelpWindow : Window
	{
        const int TextHeight = 16;

        List<TabData> Tabs = new List<TabData>();
        int ActiveTab;

		public HelpWindow(WindowManager owner)
			: base(owner, 600, 450, "Help")
		{
            Tabs.Add(new TabData(Tab.KeyboardShortcuts, "Key Commands", (cl) =>
            {
                var scrollbox = cl.AddLayoutScrollboxVertical(cl.RemainingWidth);
                var keyWidth = scrollbox.RemainingWidth / UserInput.KeyboardLayout[0].Length;
                var keyHeight = 3 * keyWidth;
                UserInput.DrawKeyboardMap((rowBox) =>
                {
                }, (keyBox, keyScanCode, keyName) =>
                {
                    var color = UserInput.GetScanCodeColor(keyScanCode);
                    if (color == Color.TransparentBlack)
                        color = Color.Black;

                    UserInput.Scale(ref keyBox, keyWidth, keyHeight);
                    scrollbox.Add(new Key(keyBox.Left - scrollbox.CurrentLeft, keyBox.Top - scrollbox.CurrentTop, keyBox.Width - 1, keyBox.Height - 1, keyName, color));
                });
                foreach (UserCommands command in Enum.GetValues(typeof(UserCommands)))
                {
                    var line = scrollbox.AddLayoutHorizontal(TextHeight);
                    var width = line.RemainingWidth / 2;
                    line.Add(new Label(width, line.RemainingHeight, UserInput.FormatCommandName(command)));
                    line.Add(new Label(width, line.RemainingHeight, UserInput.Commands[(int)command].ToString()));
                }
            }));
            if (owner.Viewer.Simulator.Activity != null)
            {
                Tabs.Add(new TabData(Tab.ActivityBriefing, "Briefing", (cl) =>
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
                Tabs.Add(new TabData(Tab.ActivityTimetable, "Timetable", (cl) =>
                {
                    var scrollbox = cl.AddLayoutScrollboxVertical(cl.RemainingWidth);
                    if (owner.Viewer.Simulator.ActivityRun != null &&
                        owner.Viewer.Simulator.ActivityRun.Tasks.Count > 0)
                    {
                        var colWidth = scrollbox.RemainingWidth / 7;
                        {
                            var hbox = scrollbox.AddLayoutHorizontal(TextHeight);
                            hbox.Add(new Label(colWidth * 3, hbox.RemainingHeight, "Station"));
                            hbox.Add(new Label(colWidth, hbox.RemainingHeight, "Arrive", LabelAlignment.Center));
                            hbox.Add(new Label(colWidth, hbox.RemainingHeight, "Actual", LabelAlignment.Center));
                            hbox.Add(new Label(colWidth, hbox.RemainingHeight, "Depart", LabelAlignment.Center));
                            hbox.Add(new Label(colWidth, hbox.RemainingHeight, "Actual", LabelAlignment.Center));
                        }
                        scrollbox.AddHorizontalSeparator();
                        foreach (var task in owner.Viewer.Simulator.ActivityRun.Tasks)
                        {
                            var stopAt = task as ActivityTaskPassengerStopAt;
                            if (stopAt != null)
                            {
                                Label arrive, depart;
                                var hbox = scrollbox.AddLayoutHorizontal(TextHeight);
                                hbox.Add(new Label(colWidth * 3, hbox.RemainingHeight, stopAt.PlatformEnd1.Station));
                                hbox.Add(new Label(colWidth, hbox.RemainingHeight, stopAt.SchArrive.ToString("HH:mm:ss"), LabelAlignment.Center));
                                hbox.Add(arrive = new Label(colWidth, hbox.RemainingHeight, stopAt.ActArrive.HasValue ? stopAt.ActArrive.Value.ToString("HH:mm:ss") : stopAt.IsCompleted.HasValue && task.NextTask != null ? "(missed)" : "", LabelAlignment.Center));
                                hbox.Add(new Label(colWidth, hbox.RemainingHeight, stopAt.SchDepart.ToString("HH:mm:ss"), LabelAlignment.Center));
                                hbox.Add(depart = new Label(colWidth, hbox.RemainingHeight, stopAt.ActDepart.HasValue ? stopAt.ActDepart.Value.ToString("HH:mm:ss") : stopAt.IsCompleted.HasValue && task.NextTask != null ? "(missed)" : "", LabelAlignment.Center));
                                arrive.Color = NextStationWindow.GetArrivalColor(stopAt.SchArrive, stopAt.ActArrive);
                                depart.Color = NextStationWindow.GetDepartColor(stopAt.SchDepart, stopAt.ActDepart);
                            }
                        }
                    }
                }));
                Tabs.Add(new TabData(Tab.ActivityWorkOrders, "Work Orders", (cl) =>
                {
                    // <CJ comment>
                    // Would like to add the scrollbox after the column headings, so that they remain visible as data is scrolled.
                    // However, can't see how to do that.
                    // If I knew how, I would also apply this to the timetable tab.
                    // </CJ comment>
                    var scrollbox = cl.AddLayoutScrollboxVertical(cl.RemainingWidth);
                    var line = scrollbox.AddLayoutHorizontal(TextHeight * 1 / 2);
                    var width = line.RemainingWidth / 5;
                    line = scrollbox.AddLayoutHorizontal(TextHeight);
                    line.Add(new Label(width, line.RemainingHeight, " Task", LabelAlignment.Center));
                    line.Add(new Label(width, line.RemainingHeight, "Car(s)", LabelAlignment.Center));
                    // Allow double width for "car(s)" to include wagon type.
                    line.Add(new Label(width * 2, line.RemainingHeight, "Location", LabelAlignment.Center));
                    line.Add(new Label(width, line.RemainingHeight, "Status", LabelAlignment.Center));
                    line = scrollbox.AddLayoutHorizontal(TextHeight * 1);

                    foreach (var Event in owner.Viewer.Simulator.Activity.Tr_Activity.Tr_Activity_File.Events) {
                        if (Event is MSTS.EventCategoryAction) {
                            var actionEvent = Event as MSTS.EventCategoryAction;
                            scrollbox.AddHorizontalSeparator();
                            line = scrollbox.AddLayoutHorizontal(TextHeight * 1);
                            switch (actionEvent.EventType) {
                                case MSTS.EventType.DropOffWagonsAtLocation:
                                    line.Add(new Label(width, line.RemainingHeight, " Drop Off"));
                                    break;
                                case MSTS.EventType.AssembleTrain:
                                    line.Add(new Label(width, line.RemainingHeight, " Assemble Train"));
                                    break;
                                case MSTS.EventType.PickUpWagons:
                                    line.Add(new Label(width, line.RemainingHeight, " Pick Up"));
                                    break;
                                case MSTS.EventType.AssembleTrainAtLocation:
                                    line.Add(new Label(width, line.RemainingHeight, " Assemble Train"));
                                    line = scrollbox.AddLayoutHorizontal(TextHeight * 1);
                                    line.Add(new Label(width, line.RemainingHeight, " At Location"));
                                    break;
                                case MSTS.EventType.MakeAPickup:
                                    // do nothing
                                    break;
                                case MSTS.EventType.PickUpPassengers:
                                    // do nothing
                                    break;
                                case MSTS.EventType.ReachSpeed:
                                    // do nothing
                                    break;
                                case MSTS.EventType.StopAtFinalStation:
                                    // do nothing
                                    break;
                            }
                            if (actionEvent.WagonList != null) {    // else passenger-only routes will crash when user selects Work Orders
                                uint sidingId = 0;
                                string wagonType = "";
                                string location = "";
                                Boolean locationShown = false;
                                foreach (var wagonItem in actionEvent.WagonList.Wagons) {
                                    var workOrderWagon = wagonItem as MSTS.WorkOrderWagon;
                                    Boolean found = false;

                                    // For "Car(s)" field, find wagon name and siding id
                                    // Different way to find these for "drop off" and "pick up"
                                    // "Drop off" wagons and sidings
                                    if (actionEvent.EventType == MSTS.EventType.DropOffWagonsAtLocation
                                        || actionEvent.EventType == MSTS.EventType.AssembleTrain
                                        || actionEvent.EventType == MSTS.EventType.AssembleTrainAtLocation) {
                                        // Consider only the user's train, Trains[0]
                                        var playerTrain = owner.Viewer.Simulator.Trains[0];
                                        foreach (var trainWagon in playerTrain.Cars) {
                                            if (workOrderWagon.UID == trainWagon.UiD) {
                                                line.Add(new Label(width, line.RemainingHeight, trainWagon.CarID));
                                                // <CJ comment>
                                                // Extracting the wagon type from the .WagFilePath property as done below is clumsy.
                                                // The .con file contains the attributes "wagon type" and "wagon type filename" (which are usually identical).
                                                // There is code to parse both of these attributes in ACTFile.cs
                                                // but can't see how to access those objects from here.
                                                // </CJ comment>
                                                wagonType = trainWagon.WagFilePath;
                                                wagonType = wagonType.Substring(1 + wagonType.LastIndexOf("\\"));  // Extract filename from full path
                                                wagonType = wagonType.Substring(0, wagonType.IndexOf("."));        // Trim off extension ".wag"
                                                // *.act file has an irregular data structure and location information (which is usually
                                                // the siding name) is kept in different places depending on the type of event.
                                                if (actionEvent.EventType == MSTS.EventType.AssembleTrain) {
                                                    location = workOrderWagon.Description;
                                                } else {
                                                    sidingId = (System.UInt16)(actionEvent.SidingId);
                                                }
                                                break;
                                            }
                                        }
                                    } else { // For "pick up", the values are held elsewhere.
                                        //
                                        // ROUTES\<route folder>\ACTIVITIES\<activity file> contains:
                                        //   Tr_Activity ( 
                                        //     Tr_Activity_File ( 
                                        //       ActivityObjects (
                                        //         ActivityObject (
                                        //           Train_Config (
                                        //             TrainCfg ( 
                                        //               Wagon ( 
                                        //                 WagonData ( <wagon title> <wagon filename> )
                                        //                 UiD ( <uid> ) ) ) )
                                        //           ID ( <train id> ) ) ) )          
                                        //
                                        // Wagon.UiD contains train and wagon indexes packed into single 32-bit value, e.g. 32678 - 0
                                        //
                                        var trainIndex = (System.UInt16)(workOrderWagon.UID >> 16);         // Extract upper 16 bits
                                        var wagonIndex = (System.UInt16)(workOrderWagon.UID & 0x0000FFFF);  // Extract lower 16 bits
                                        line.Add(new Label(width, line.RemainingHeight, trainIndex.ToString() + " - " + wagonIndex.ToString()));
                                        foreach (MSTS.ActivityObject ActivityObject in owner.Viewer.Simulator.Activity.Tr_Activity.Tr_Activity_File.ActivityObjects) {
                                            found = false;
                                            MSTS.Train_Config train_config = ActivityObject.Train_Config;
                                            MSTS.TrainCfg trainCfg = train_config.TrainCfg;
                                            if (ActivityObject.ID == trainIndex) {
                                                foreach (MSTS.Wagon trainWagon in trainCfg.Wagons) {
                                                    if (trainWagon.UiD == wagonIndex) {
                                                        wagonType = trainWagon.Name;
                                                        found = true;
                                                        break;
                                                    }
                                                }
                                            }
                                            if (found == true) { break; }
                                        }
                                        sidingId = workOrderWagon.SidingItem;
                                    }
                                    line.Add(new Label(width, line.RemainingHeight, wagonType));
                                    
                                    // For "location" field, add siding name
                                    if (locationShown == true) {
                                        // don't repeat the siding name
                                    } else {
                                        found = false;
                                        foreach (var trItem in owner.Viewer.Simulator.TDB.TrackDB.TrItemTable) {
                                            if (trItem is MSTS.SidingItem) {
                                                MSTS.SidingItem siding = trItem as MSTS.SidingItem;
                                                if (siding.TrItemId == sidingId) {
                                                    line.Add(new Label(width, line.RemainingHeight, siding.ItemName));
                                                    locationShown = true;
                                                    found = true;
                                                    break;
                                                }
                                            }
                                        }
                                        if (found == false) {
                                            line.Add(new Label(width, line.RemainingHeight, location));
                                            locationShown = true;
                                        }
                                    }

                                    // For "Status" field, add data here 
                                    
                                    // Add a new line and step across to "Wagon" field.
                                    line = scrollbox.AddLayoutHorizontal(TextHeight * 1);
                                    line.Add(new Label(width, line.RemainingHeight, ""));
                                }
                            }
                        }
                    }
                }));
            }
            Tabs.Add(new TabData(Tab.LocomotiveProcedures, "Procedures", (cl) =>
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
        }

        public override void TabAction()
        {
            ActiveTab = (ActiveTab + 1) % Tabs.Count;
            Layout();
        }

		protected override ControlLayout Layout(ControlLayout layout)
		{
			var vbox = base.Layout(layout).AddLayoutVertical();

            if (Tabs.Count > 0)
            {
                var hbox = vbox.AddLayoutHorizontal(TextHeight);
                var tabWidth = hbox.RemainingWidth / Tabs.Count;
                for (var i = 0; i < Tabs.Count; i++)
                {
                    var label = new Label(tabWidth, TextHeight, Tabs[i].TabLabel, LabelAlignment.Center) { Color = ActiveTab == i ? Color.White : Color.Gray, Tag = i };
                    label.Click += label_Click;
                    hbox.Add(label);
                }
                vbox.AddHorizontalSeparator();

                Tabs[ActiveTab].Layout(vbox);
            }

            return vbox;
        }

        void label_Click(Control control, Point point)
        {
            ActiveTab = (int)control.Tag;
            Layout();
        }

        enum Tab
        {
            ActivityBriefing,
            ActivityTimetable,
            ActivityWorkOrders,
            LocomotiveProcedures,
            KeyboardShortcuts,
        }

        class TabData
        {
            public readonly Tab Tab;
            public readonly string TabLabel;
            public readonly Action<ControlLayout> Layout;

            public TabData(Tab tab, string tabLabel, Action<ControlLayout> layout)
            {
                Tab = tab;
                TabLabel = tabLabel;
                Layout = layout;
            }
        }

        ActivityTask LastActivityTask;
        bool StoppedAt;

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            if (updateFull && Tabs[ActiveTab].Tab == Tab.ActivityTimetable && Owner.Viewer.Simulator.ActivityRun != null)
            {
                if (LastActivityTask != Owner.Viewer.Simulator.ActivityRun.Current || StoppedAt != GetStoppedAt(LastActivityTask))
                {
                    LastActivityTask = Owner.Viewer.Simulator.ActivityRun.Current;
                    StoppedAt = GetStoppedAt(LastActivityTask);
                    Layout();
                }
            }
        }

        bool GetStoppedAt(ActivityTask task)
        {
            if (task is ActivityTaskPassengerStopAt)
                return ((ActivityTaskPassengerStopAt)task).ActArrive != null;
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
