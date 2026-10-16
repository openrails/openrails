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
using Orts.Simulation;
using ORTS.Common;
using System;
using System.Linq;

namespace Orts.Viewer3D.Popups
{
    public class ActivityWindow : Window
    {
        int WindowHeightMin = 0;
        int WindowHeightMax = 0;

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
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 40, Window.DecorationSize.Y + owner.TextFontDefault.Height * 12 /* 10 lines + 2 lines of controls */ + ControlLayout.SeparatorSize * 2, Viewer.Catalog.GetString("Activity Events"))
        {
            WindowHeightMin = Location.Height;
            WindowHeightMax = Location.Height + owner.TextFontDefault.Height * 10; // Add another 10 lines for longer messages.
            Activity = Owner.Viewer.Simulator.ActivityRun;
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            var originalMessage = Message == null ? null : Message.Text;
            var originalResumeLabel = ResumeLabel == null ? null : ResumeLabel.Text;
            var originalCloseLabel = CloseLabel == null ? null : CloseLabel.Text;
            var originalQuitLabel = QuitLabel == null ? null : QuitLabel.Text;
            var originalEventNameLabel = EventNameLabel == null ? null : EventNameLabel.Text;
            var originalStatusLabel = StatusLabel == null ? null : StatusLabel.Text;
            var originalStatusLabelColor = StatusLabel == null ? null : new Color?(StatusLabel.Color);

            var vbox = base.Layout(layout).AddLayoutVertical();
            {
                var hbox = vbox.AddLayoutHorizontal(vbox.RemainingHeight - (ControlLayout.SeparatorSize + vbox.TextHeight) * 2);
                var scrollbox = hbox.AddLayoutScrollboxVertical(hbox.RemainingWidth);
                scrollbox.Add(Message = new TextFlow(scrollbox.RemainingWidth - scrollbox.TextHeight, originalMessage));
                MessageScroller = (ControlLayoutScrollbox)hbox.Controls.Last();
            }
            vbox.AddHorizontalSeparator();
            {
                var hbox = vbox.AddLayoutHorizontalLineOfText();
                var boxWidth = hbox.RemainingWidth / 3;
                hbox.Add(ResumeLabel = new Label(boxWidth, hbox.RemainingHeight, originalResumeLabel, LabelAlignment.Center));
                hbox.Add(CloseLabel = new Label(boxWidth, hbox.RemainingHeight, originalCloseLabel, LabelAlignment.Center));
                hbox.Add(QuitLabel = new Label(boxWidth, hbox.RemainingHeight, originalQuitLabel, LabelAlignment.Center));
                ResumeLabel.Click += new Action<Control, Point>(ResumeActivity_Click);
                CloseLabel.Click += new Action<Control, Point>(CloseBox_Click);
                QuitLabel.Click += new Action<Control, Point>(QuitActivity_Click);
            }
            vbox.AddHorizontalSeparator();
            {
                var hbox = vbox.AddLayoutHorizontalLineOfText();
                var boxWidth = hbox.RemainingWidth / 2;
                hbox.Add(EventNameLabel = new Label(boxWidth, hbox.RemainingHeight, originalEventNameLabel, LabelAlignment.Left));
                hbox.Add(StatusLabel = new Label(boxWidth, hbox.RemainingHeight, originalStatusLabel, LabelAlignment.Left));
                StatusLabel.Color = originalStatusLabelColor.HasValue ? originalStatusLabelColor.Value : Color.LightSalmon;
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
            if (Owner.Viewer.Simulator.IsReplaying) Owner.Viewer.Simulator.Confirmer.Confirm(CabControl.Activity, CabSetting.Off);
            Owner.Viewer.Game.PopState();
        }

        public void CloseBox()
        {
            this.Visible = false;
            this.Activity.IsActivityWindowOpen = this.Visible;
            this.Activity.TriggeredEvent = null;
            Activity.NewMsgFromNewPlayer = false;
            Owner.Viewer.Simulator.Paused = false;   // Move to Viewer3D?
            this.Activity.IsActivityResumed = !Owner.Viewer.Simulator.Paused;
            if (Owner.Viewer.Simulator.IsReplaying) Owner.Viewer.Simulator.Confirmer.Confirm(CabControl.Activity, CabSetting.On);
        }

        public void ResumeActivity()
        {
            this.Activity.TriggeredEvent = null;
            Owner.Viewer.Simulator.Paused = false;   // Move to Viewer3D?
            Activity.IsActivityResumed = !Owner.Viewer.Simulator.Paused;
            if (Owner.Viewer.Simulator.IsReplaying) Owner.Viewer.Simulator.Confirmer.Confirm(CabControl.Activity, CabSetting.On);
            ResumeMenu();
        }

        public void PauseActivity()
        {
            Owner.Viewer.Simulator.Paused = true;   // Move to Viewer3D?
            Activity.IsActivityResumed = !Owner.Viewer.Simulator.Paused;
            if (Owner.Viewer.Simulator.IsReplaying) Owner.Viewer.Simulator.Confirmer.Confirm(CabControl.Activity, CabSetting.On);
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
                            if (!Owner.Viewer.HelpWindow.Visible)
                            {//Show evaluation info.
                                Owner.Viewer.HelpWindow.Visible = false;
                                //TO DO: Change next lines to one line. 
                                Owner.Viewer.HelpWindow.TabAction();
                                Owner.Viewer.HelpWindow.TabAction();
                                Owner.Viewer.HelpWindow.TabAction();
                                Owner.Viewer.HelpWindow.TabAction();
                            }
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

                                if ((Owner.Viewer.ActivityEventsWebpage != null) && (Owner.Viewer.ActivityEventsWebpage.isConnectionOpen))
                                {
                                    if (Owner.Viewer.ActivityEventsWebpage.isInitHandled)
                                    {
                                        Owner.Viewer.ActivityEventsWebpage.handleSendActivityEvent(e.ParsedObject.Name, text);
                                        Activity.IsActivityResumed = true;
                                        ResumeActivity();
                                    }
                                }
                                else
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
                                        Visible = Owner.Viewer.HelpWindow.ActivityUpdated = true;
                                    }
                                    else
                                    {
                                        // Only needs updating the first time through
                                        if (!Owner.Viewer.Simulator.Paused && Visible == false)
                                        {
                                            Owner.Viewer.Simulator.Paused = e.ParsedObject.ORTSContinue < 0 ? true : false;
                                            if (e.ParsedObject.ORTSContinue != 0)
                                            {
                                                ComposeMenu(e.ParsedObject.Name, text);
                                                if (e.ParsedObject.ORTSContinue < 0) ResumeMenu();
                                                else NoPauseMenu();
                                            }
                                            PopupTime = DateTime.Now;
                                            Visible = Owner.Viewer.HelpWindow.ActivityUpdated = true;
                                        }
                                    }
                                }
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
                    else if (Activity.NewMsgFromNewPlayer)
                    {
                        // Displays messages related to actual player train, when not coincident with initial player train
                        var text = Activity.MsgFromNewPlayer;
                        if (!String.IsNullOrEmpty(text))
                        {
                            if ((Owner.Viewer.ActivityEventsWebpage != null) && (Owner.Viewer.ActivityEventsWebpage.isConnectionOpen))
                            {
                                if (Owner.Viewer.ActivityEventsWebpage.isInitHandled)
                                {
                                    Owner.Viewer.ActivityEventsWebpage.handleSendActivityEvent(e.ParsedObject.Name, text);
                                    Activity.IsActivityResumed = true;
                                    ResumeActivity();
                                }
                            }
                            else
                            {
                                if (Activity.ReopenActivityWindow)
                                {
                                    ComposeActualPlayerTrainMenu(Owner.Viewer.PlayerTrain.Name, text);
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
                                        ComposeActualPlayerTrainMenu(Owner.Viewer.PlayerTrain.Name, text);
                                        NoPauseMenu();
                                        PopupTime = DateTime.Now;
                                    }
                                    else if (Owner.Viewer.Simulator.Paused)
                                    {
                                        ResumeMenu();
                                    }
                                }
                                Visible = true;
                            }
                        }
                        else
                        {
                            Activity.NewMsgFromNewPlayer = false;
                        }
                        TimeSpan diff1 = DateTime.Now - PopupTime;
                        if (Visible && diff1.TotalSeconds >= 10 && !Owner.Viewer.Simulator.Paused)
                        {
                            CloseBox();
                            Activity.NewMsgFromNewPlayer = false;
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
            ResizeDialog();
        }

        void ComposeActualPlayerTrainMenu(string trainName, string message)
        {
            EventNameLabel.Text = Viewer.Catalog.GetStringFmt("Train: {0}", trainName.Substring(0, Math.Min(trainName.Length, 20)));
            MessageScroller.SetScrollPosition(0);
            Message.Text = message;
            ResizeDialog();
        }

        void ResizeDialog()
        {
            var desiredHeight = Location.Height + Message.Position.Height - MessageScroller.Position.Height;
            var newHeight = (int)MathHelper.Clamp(desiredHeight, WindowHeightMin, WindowHeightMax);
            // Move the dialog up if we're expanding it, or down if not; this keeps the center in the same place.
            var newTop = Location.Y + (Location.Height - newHeight) / 2;
            SizeTo(Location.Width, newHeight);
            MoveTo(Location.X, newTop);
        }
    }
}
