/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

/// Author: James Ross
/// 
/// Help; used to display the keyboard shortcuts and other help.
/// 

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
                var chWidth = scrollbox.RemainingWidth / UserInput.KeyboardLayout[0].Length;
                var chHeight = 3 * chWidth;
                foreach (var keyboardLine in UserInput.KeyboardLayout)
                {
                    scrollbox.AddSpace(0, 2);
                    var line = scrollbox.AddLayoutHorizontal(chHeight);
                    var index = keyboardLine.IndexOf('[');
                    var lastIndex = -1;
                    while (index != -1)
                    {
                        var indexEnd = keyboardLine.IndexOf(']', index);

                        var scanCodeString = keyboardLine.Substring(index + 1, 3).Trim();
                        var scanCode = scanCodeString.Length > 0 ? int.Parse(scanCodeString, NumberStyles.HexNumber) : 0;
                        var keyName = UserInput.GetScanCodeKeyName(scanCode);
                        // Only allow F-keys to show >1 character names. The rest we'll remove for now.
                        if ((keyName.Length > 1) && !new[] { 0x3B, 0x3C, 0x3D, 0x3E, 0x3F, 0x40, 0x41, 0x42, 0x43, 0x44, 0x57, 0x58 }.Contains(scanCode))
                            keyName = "";

                        var color = UserInput.GetScanCodeColor(scanCode);
                        if (color == Color.TransparentBlack)
                            color = Color.Black;
                        line.Add(new Key(chWidth * (index - lastIndex - 1) + 2, 0, chWidth * (indexEnd - index + 1) - 2, chHeight, keyName, color));
                        lastIndex = indexEnd;
                        index = keyboardLine.IndexOf('[', indexEnd);
                    }
                }
                scrollbox.AddSpace(0, chWidth);
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
                //Tabs.Add(new TabData(Tab.ActivityWorkOrders, "Work Orders", (cl) =>
                //{
                //    var scrollbox = cl.AddLayoutScrollboxVertical(cl.RemainingWidth);
                //}));
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

        public void UpdateText(ElapsedTime elapsedTime)
        {
            if (Tabs[ActiveTab].Tab == Tab.ActivityTimetable &&
                Owner.Viewer.Simulator.ActivityRun != null)
            {
                if (LastActivityTask != Owner.Viewer.Simulator.ActivityRun.Current ||
                    StoppedAt != GetStoppedAt(LastActivityTask))
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
