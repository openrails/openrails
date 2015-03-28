// COPYRIGHT 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
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
using ORTS.Common;
using System;
using System.Linq;

namespace ORTS.Viewer3D.Popups
{
    public class ActivityWindow : Window
    {
        Activity Activity;
        ControlLayoutScrollbox MessageScroller;
        TextFlow Message;
        Label EventNameLabel;
        Label ResumeLabel;
        Label CloseLabel;
        Label QuitLabel;
        Label StatusLabel;
        DateTime PopupTime;

        public ActivityWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 25, Window.DecorationSize.Y + owner.TextFontDefault.Height * 8 + ControlLayout.SeparatorSize * 2, Viewer.Catalog.GetString("Activity Events"))
        {
            Activity = Owner.Viewer.Simulator.ActivityRun;
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            var vbox = base.Layout(layout).AddLayoutVertical();
            {
                var hbox = vbox.AddLayoutHorizontal(Owner.TextFontDefault.Height * 6);
                var scrollbox = hbox.AddLayoutScrollboxVertical(hbox.RemainingWidth);
                scrollbox.Add(Message = new TextFlow(scrollbox.RemainingWidth - scrollbox.TextHeight, ""));
                MessageScroller = (ControlLayoutScrollbox)hbox.Controls.Last();
            }
            vbox.AddHorizontalSeparator();
            {
                var hbox = vbox.AddLayoutHorizontalLineOfText();
                var boxWidth = hbox.RemainingWidth / 3;
                hbox.Add(ResumeLabel = new Label(boxWidth, hbox.RemainingHeight, "", LabelAlignment.Center));
                hbox.Add(CloseLabel = new Label(boxWidth, hbox.RemainingHeight, "", LabelAlignment.Center));
                hbox.Add(QuitLabel = new Label(boxWidth, hbox.RemainingHeight, "", LabelAlignment.Center));
                ResumeLabel.Click += new Action<Control, Point>(ResumeActivity_Click);
                CloseLabel.Click += new Action<Control, Point>(CloseBox_Click);
                QuitLabel.Click += new Action<Control, Point>(QuitActivity_Click);
            }
            vbox.AddHorizontalSeparator();
            {
                var hbox = vbox.AddLayoutHorizontalLineOfText();
                var boxWidth = hbox.RemainingWidth / 2;
                hbox.Add(EventNameLabel = new Label(boxWidth, hbox.RemainingHeight, "", LabelAlignment.Left));
                hbox.Add(StatusLabel = new Label(boxWidth, hbox.RemainingHeight, "", LabelAlignment.Left));
                StatusLabel.Color = Color.LightSalmon;
            }
            return vbox;
        }

        void ResumeActivity_Click(Control arg1, Point arg2)
        {
            TimeSpan diff = DateTime.Now - PopupTime;
            if (Owner.Viewer.Simulator.Paused)
                new ResumeActivityCommand(Owner.Viewer.Log, EventNameLabel.Text, diff.TotalMilliseconds / 1000);
            //it's a toggle click
            else
                new PauseActivityCommand(Owner.Viewer.Log, EventNameLabel.Text, diff.TotalMilliseconds / 1000);
        }

        void CloseBox_Click(Control arg1, Point arg2)
        {
            TimeSpan diff = DateTime.Now - PopupTime;
            new CloseAndResumeActivityCommand(Owner.Viewer.Log, EventNameLabel.Text, diff.TotalMilliseconds / 1000);
        }

        void QuitActivity_Click(Control arg1, Point arg2)
        {
            TimeSpan diff = DateTime.Now - PopupTime;
            new QuitActivityCommand(Owner.Viewer.Log, EventNameLabel.Text, diff.TotalMilliseconds / 1000);
        }

        public void QuitActivity()
        {
            this.Visible = false;
            this.Activity.IsActivityWindowOpen = this.Visible;
            this.Activity.TriggeredEvent = null;
            Owner.Viewer.Simulator.Paused = false;   // Move to Viewer3D?
            this.Activity.IsActivityResumed = !Owner.Viewer.Simulator.Paused;
            Activity.IsComplete = true;
            if (Owner.Viewer.IsReplaying) Owner.Viewer.Simulator.Confirmer.Confirm(CabControl.Activity, CabSetting.Off);
            Owner.Viewer.Game.PopState();
        }

        public void CloseBox()
        {
            this.Visible = false;
            this.Activity.IsActivityWindowOpen = this.Visible;
            this.Activity.TriggeredEvent = null;
            Owner.Viewer.Simulator.Paused = false;   // Move to Viewer3D?
            this.Activity.IsActivityResumed = !Owner.Viewer.Simulator.Paused;
            if (Owner.Viewer.IsReplaying) Owner.Viewer.Simulator.Confirmer.Confirm(CabControl.Activity, CabSetting.On);
        }

        public void ResumeActivity()
        {
            this.Activity.TriggeredEvent = null;
            Owner.Viewer.Simulator.Paused = false;   // Move to Viewer3D?
            Activity.IsActivityResumed = !Owner.Viewer.Simulator.Paused;
            if (Owner.Viewer.IsReplaying) Owner.Viewer.Simulator.Confirmer.Confirm(CabControl.Activity, CabSetting.On);
            ResumeMenu();
        }

        public void PauseActivity()
        {
            Owner.Viewer.Simulator.Paused = true;   // Move to Viewer3D?
            Activity.IsActivityResumed = !Owner.Viewer.Simulator.Paused;
            if (Owner.Viewer.IsReplaying) Owner.Viewer.Simulator.Confirmer.Confirm(CabControl.Activity, CabSetting.On);
            ResumeMenu();
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            if (updateFull)
            {
                var act = Owner.Viewer.Simulator.ActivityRun;
                if (act != null)
                {
                    var e = Activity.ReopenActivityWindow ? Activity.LastTriggeredEvent : Activity.TriggeredEvent;
                    if (e != null)
                    {
                        if (Activity.IsComplete)
                        {
                            Visible = Activity.IsActivityWindowOpen = Owner.Viewer.HelpWindow.ActivityUpdated = Owner.Viewer.Simulator.Paused = true;
                            ComposeMenu(e.ParsedObject.Name, Viewer.Catalog.GetStringFmt("This activity has ended {0}.\nFor a detailed evaluation, see the Help Window (F1).",
                                Activity.IsSuccessful ? Viewer.Catalog.GetString("") : Viewer.Catalog.GetString("without success")));
                            EndMenu();
                        }
                        else
                        {
                            var text = e.ParsedObject.Outcomes.DisplayMessage;
                            if (!String.IsNullOrEmpty(text))
                            {
                                if (Activity.ReopenActivityWindow)
                                {
                                    ComposeMenu(e.ParsedObject.Name, text);
                                    if (Activity.IsActivityResumed)
                                    {
                                        ResumeActivity();
                                        CloseMenu();
                                    }
                                    else
                                    {
                                        Owner.Viewer.Simulator.Paused = true;
                                        ResumeMenu();
                                        PopupTime = DateTime.Now;
                                    }
                                }
                                else
                                {
                                    // Only needs updating the first time through
                                    if (!Owner.Viewer.Simulator.Paused && Visible == false)
                                    {
                                        Owner.Viewer.Simulator.Paused = e.ParsedObject.ORTSContinue < 0? true : false;
                                        if (e.ParsedObject.ORTSContinue != 0)
                                        {
                                            ComposeMenu(e.ParsedObject.Name, text);
                                            if (e.ParsedObject.ORTSContinue < 0) ResumeMenu();
                                            else NoPauseMenu();
                                        }
                                        PopupTime = DateTime.Now;
                                    }
                                }
                                Visible = Owner.Viewer.HelpWindow.ActivityUpdated = true;
                            }
                            else
                            {
                                // Cancel the event as pop-up not needed.
                                Activity.TriggeredEvent = null;
                            }
                            TimeSpan diff1 = DateTime.Now - PopupTime;
                            if (Visible && e.ParsedObject.ORTSContinue >= 0 && diff1.TotalSeconds >= e.ParsedObject.ORTSContinue && !Owner.Viewer.Simulator.Paused)
                            {
                                CloseBox();
                            }
                        }
                    }
                    Activity.IsActivityResumed = !Owner.Viewer.Simulator.Paused;
                    Activity.IsActivityWindowOpen = Visible;
                    Activity.ReopenActivityWindow = false;
                }
            }
        }

        // <CJComment> Would like the dialog box background as solid black to indicate "simulator paused",
        // and change it later to see-through, if box is left on-screen when simulator resumes.
        // Don't know how.
        // </CJComment>
        void ResumeMenu()
        {
            ResumeLabel.Text = Owner.Viewer.Simulator.Paused ? Viewer.Catalog.GetString("Resume") : Viewer.Catalog.GetString("Pause");
            CloseLabel.Text = Owner.Viewer.Simulator.Paused ? Viewer.Catalog.GetString("Resume and close box") : Viewer.Catalog.GetString("Close box");
            QuitLabel.Text = Viewer.Catalog.GetString("Quit activity");
            StatusLabel.Text = Owner.Viewer.Simulator.Paused ? Viewer.Catalog.GetString("Status: Activity paused") : Viewer.Catalog.GetString("Status: Activity resumed");
            StatusLabel.Color = Owner.Viewer.Simulator.Paused ? Color.LightSalmon : Color.LightGreen;
        }

        // <CJComment> At this point, would like to change dialog box background from solid to see-through,
        // but don't know how.
        // </CJComment>
        void CloseMenu()
        {
            ResumeLabel.Text = "";
            CloseLabel.Text = Viewer.Catalog.GetString("Close box");
            QuitLabel.Text = Viewer.Catalog.GetString("Quit activity");
            StatusLabel.Text = Viewer.Catalog.GetString("Status: Activity resumed");
            StatusLabel.Color = Color.LightGreen;
        }

        void EndMenu()
        {
            ResumeLabel.Text = "";
            CloseLabel.Text = Viewer.Catalog.GetString("Resume and close box");
            QuitLabel.Text = Viewer.Catalog.GetString("End Activity");
            StatusLabel.Text = Viewer.Catalog.GetString("Status: Activity paused");
            StatusLabel.Color = Color.LightSalmon;
        }

        void NoPauseMenu()
        {
            ResumeLabel.Text = Viewer.Catalog.GetString("Pause");
            CloseLabel.Text = Viewer.Catalog.GetString("Close box");
            QuitLabel.Text = Viewer.Catalog.GetString("Quit activity");
            StatusLabel.Text = Viewer.Catalog.GetString("Status: Activity running");
            StatusLabel.Color = Color.LightGreen;
        }

        void ComposeMenu(string eventLabel, string message)
        {
            EventNameLabel.Text = Viewer.Catalog.GetStringFmt("Event: {0}", eventLabel);
            MessageScroller.SetScrollPosition(0);
            Message.Text = message;
        }
    }
}
