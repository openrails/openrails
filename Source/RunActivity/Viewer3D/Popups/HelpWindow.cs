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
using Orts.Simulation;
using Orts.Simulation.RollingStocks;
using ORTS.Common;
using ORTS.Settings;
using System;
using System.Collections.Generic;
using System.IO;

namespace Orts.Viewer3D.Popups
{
    public class HelpWindow : Window
    {
        public bool ActivityUpdated;
        public int lastLastEventID = -1;

        List<TabData> Tabs = new List<TabData>();
        int ActiveTab;

        public HelpWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 37, Window.DecorationSize.Y + owner.TextFontDefault.Height * 24, Viewer.Catalog.GetString("Help"))
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
                    if (color == Color.TransparentBlack)
                        color = Color.Black;

                    InputSettings.Scale(ref keyBox, keyWidth, keyHeight);
                    scrollbox.Add(new Key(keyBox.Left - scrollbox.CurrentLeft, keyBox.Top - scrollbox.CurrentTop, keyBox.Width - 1, keyBox.Height - 1, keyName, color));
                });
                foreach (UserCommands command in Enum.GetValues(typeof(UserCommands)))
                {
                    var line = scrollbox.AddLayoutHorizontalLineOfText();
                    var width = line.RemainingWidth / 2;
                    line.Add(new Label(width, line.RemainingHeight, InputSettings.GetPrettyLocalizedName(command)));
                    line.Add(new Label(width, line.RemainingHeight, Owner.Viewer.Settings.Input.Commands[(int)command].ToString()));
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
                                                line.Add(new Label(colWidth * 7, line.RemainingHeight, location));
                                            else if ((eventAction.Type == Orts.Formats.Msts.EventType.PickUpPassengers) || (eventAction.Type == Orts.Formats.Msts.EventType.PickUpWagons))
                                                line.AddSpace(colWidth * 7, 0);
                                            locationFirst = location;
                                            locationShown = true;
                                        }
                                        // Status column
                                        if (@event.TimesTriggered == 1 && wagonIdx == 0) line.Add(new Label(colWidth * 6, line.RemainingHeight, Viewer.Catalog.GetString("Done")));
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
            else if (updateFull && Tabs[ActiveTab].Tab == Tab.ActivityWorkOrders && Owner.Viewer.Simulator.ActivityRun != null)
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
