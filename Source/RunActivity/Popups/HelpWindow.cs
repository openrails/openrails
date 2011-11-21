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
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;

namespace ORTS.Popups
{
	public class HelpWindow : Window
	{
        const int TextHeight = 16;

		public bool ActivityUpdated = false;

        List<TabData> Tabs = new List<TabData>();
        int ActiveTab;

		string statusText = "";

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
                    var colWidth = (cl.RemainingWidth - ControlLayoutScrollbox.ScrollbarSize) / 7;
                    {
                        var line = cl.AddLayoutHorizontal(TextHeight);
                        line.Add(new Label(colWidth * 3, line.RemainingHeight, "Station"));
                        line.Add(new Label(colWidth, line.RemainingHeight, "Arrive", LabelAlignment.Center));
                        line.Add(new Label(colWidth, line.RemainingHeight, "Actual", LabelAlignment.Center));
                        line.Add(new Label(colWidth, line.RemainingHeight, "Depart", LabelAlignment.Center));
                        line.Add(new Label(colWidth, line.RemainingHeight, "Actual", LabelAlignment.Center));
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
                                var line = scrollbox.AddLayoutHorizontal(TextHeight);
                                line.Add(new Label(colWidth * 3, line.RemainingHeight, stopAt.PlatformEnd1.Station));
                                line.Add(new Label(colWidth, line.RemainingHeight, stopAt.SchArrive.ToString("HH:mm:ss"), LabelAlignment.Center));
                                line.Add(arrive = new Label(colWidth, line.RemainingHeight, stopAt.ActArrive.HasValue ? stopAt.ActArrive.Value.ToString("HH:mm:ss") : stopAt.IsCompleted.HasValue && task.NextTask != null ? "(missed)" : "", LabelAlignment.Center));
                                line.Add(new Label(colWidth, line.RemainingHeight, stopAt.SchDepart.ToString("HH:mm:ss"), LabelAlignment.Center));
                                line.Add(depart = new Label(colWidth, line.RemainingHeight, stopAt.ActDepart.HasValue ? stopAt.ActDepart.Value.ToString("HH:mm:ss") : stopAt.IsCompleted.HasValue && task.NextTask != null ? "(missed)" : "", LabelAlignment.Center));
                                arrive.Color = NextStationWindow.GetArrivalColor(stopAt.SchArrive, stopAt.ActArrive);
                                depart.Color = NextStationWindow.GetDepartColor(stopAt.SchDepart, stopAt.ActDepart);
                            }
                        }
                    }
                }));
                Tabs.Add(new TabData(Tab.ActivityWorkOrders, "Work Orders", (cl) =>
                {
                    var colWidth = (cl.RemainingWidth - ControlLayoutScrollbox.ScrollbarSize) / 20;
                    {
                        var line = cl.AddLayoutHorizontal(TextHeight);
                        line.Add(new Label(colWidth * 4, line.RemainingHeight, "Task"));
                        line.Add(new Label(colWidth * 9, line.RemainingHeight, "Car(s)"));
						line.Add(new Label(colWidth * 6, line.RemainingHeight, "Location"));
						line.Add(new Label(colWidth * 6, line.RemainingHeight, "Status"));
                    }
                    cl.AddHorizontalSeparator();
                    var scrollbox = cl.AddLayoutScrollboxVertical(cl.RemainingWidth);
                    var separatorShown = false;
                    if (owner.Viewer.Simulator.ActivityRun.EventList != null)
                    {
                        foreach (var @event in owner.Viewer.Simulator.ActivityRun.EventList)
                        {
                            var eventAction = @event.ParsedObject as MSTS.EventCategoryAction;
                            if (eventAction != null)
                            {
                                if (separatorShown)
                                    scrollbox.AddHorizontalSeparator();
                                var line = scrollbox.AddLayoutHorizontal(TextHeight);
                                // Task column
                                switch (eventAction.Type)
                                {
                                    case MSTS.EventType.AssembleTrain:
                                    case MSTS.EventType.AssembleTrainAtLocation:
                                        line.Add(new Label(colWidth * 4, line.RemainingHeight, "Assemble Train"));
                                        if (eventAction.Type == MSTS.EventType.AssembleTrainAtLocation)
                                        {
                                            line = scrollbox.AddLayoutHorizontal(TextHeight);
                                            line.Add(new Label(colWidth * 4, line.RemainingHeight, "At Location"));
                                        }
                                        break;
                                    case MSTS.EventType.DropOffWagonsAtLocation:
                                        line.Add(new Label(colWidth * 4, line.RemainingHeight, "Drop Off"));
                                        break;
                                    case MSTS.EventType.PickUpPassengers:
                                    case MSTS.EventType.PickUpWagons:
                                        line.Add(new Label(colWidth * 4, line.RemainingHeight, "Pick Up"));
                                        break;
                                }
								Activity act = Owner.Viewer.Simulator.ActivityRun;
                                if (eventAction.WagonList != null) {
                                    var location = "";
                                    var locationShown = false;
									var wagonIdx = 0;
                                    foreach (MSTS.WorkOrderWagon wagonItem in eventAction.WagonList.WorkOrderWagonList) {
                                        if (locationShown) {
                                            line = scrollbox.AddLayoutHorizontal(TextHeight);
                                            line.AddSpace(colWidth * 4, 0);
                                        }

                                        // Car(s) column
                                        // Wagon.UiD contains train and wagon indexes packed into single 32-bit value, e.g. 32678 - 0
                                        var trainIndex = wagonItem.UID >> 16;         // Extract upper 16 bits
                                        var wagonIndex = wagonItem.UID & 0x0000FFFF;  // Extract lower 16 bits
                                        var wagonName = trainIndex.ToString() + " - " + wagonIndex.ToString();
                                        var wagonType = "";
                                        var wagonFound = false;
                                        foreach (MSTS.ActivityObject activityObject in owner.Viewer.Simulator.Activity.Tr_Activity.Tr_Activity_File.ActivityObjects.ActivityObjectList) {
                                            if (activityObject.ID == trainIndex) {
                                                foreach (MSTS.Wagon trainWagon in activityObject.Train_Config.TrainCfg.WagonList) {
                                                    if (trainWagon.UiD == wagonIndex) {
                                                        wagonType = trainWagon.Name;
                                                        wagonFound = true;
                                                        break;
                                                    }
                                                }
                                            }
                                            if (wagonFound)
                                                break;
                                        }
                                        if (!wagonFound) {
                                            foreach (var car in owner.Viewer.PlayerTrain.Cars) {
                                                if (car.UiD == wagonItem.UID) {
                                                    wagonType = Path.GetFileNameWithoutExtension(car.WagFilePath);
                                                    wagonFound = true;
                                                    break;
                                                }
                                            }
                                        }
                                        line.Add(new Label(colWidth * 3, line.RemainingHeight, wagonName));
                                        line.Add(new Label(colWidth * 6, line.RemainingHeight, wagonType));

                                        // Location column
                                        if (locationShown) {
                                            line.AddSpace(colWidth * 6, 0);
                                        } else {
                                            var sidingId = eventAction.Type == MSTS.EventType.AssembleTrainAtLocation
                                                || eventAction.Type == MSTS.EventType.DropOffWagonsAtLocation
                                                ? (uint)eventAction.SidingId : wagonItem.SidingId;
                                            foreach (var item in owner.Viewer.Simulator.TDB.TrackDB.TrItemTable) {
                                                var siding = item as MSTS.SidingItem;
                                                if (siding != null && siding.TrItemId == sidingId) {
                                                    location = siding.ItemName;
                                                    break;
                                                }
                                            }
                                            line.Add(new Label(colWidth * 6, line.RemainingHeight, location));
                                            locationShown = true;
                                        }
										// Status column
										if (@event.TimesTriggered == 1 &&wagonIdx == 0) line.Add(new Label(colWidth, line.RemainingHeight, "Done"));
										else line.Add(new Label(colWidth, line.RemainingHeight, ""));
										wagonIdx++;

                                    }
                                }

                                separatorShown = true;
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
			if (this.ActivityUpdated == true) //true value is set in ActivityWindow.cs
			{
				this.ActivityUpdated = false;
				Layout();
			}
			//UpdateActivityStatus();
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
