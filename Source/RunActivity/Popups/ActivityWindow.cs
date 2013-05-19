// COPYRIGHT 2011, 2012 by the Open Rails project.
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
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;

namespace ORTS.Popups
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

        public ActivityWindow( WindowManager owner )
            : base(owner, 400, 180, "Activity Events")
        {
            Activity = Owner.Viewer.Simulator.ActivityRun;
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            var vbox = base.Layout( layout ).AddLayoutVertical();
            {
                var hbox = vbox.AddLayoutHorizontal( 100 );
                var scrollbox = hbox.AddLayoutScrollboxVertical( hbox.RemainingWidth );
                scrollbox.Add( Message = new TextFlow( scrollbox.RemainingWidth - ControlLayoutScrollbox.ScrollbarSize, "" ) );
                MessageScroller = (ControlLayoutScrollbox)hbox.Controls.Last();
            }
            vbox.AddHorizontalSeparator();
            var height = vbox.RemainingHeight / 2;
            var width = vbox.RemainingWidth / 3;
            {
                var hbox = vbox.AddLayoutHorizontal( height );
                hbox.Add( ResumeLabel = new Label( width, height, "", LabelAlignment.Center ) );
                hbox.Add( CloseLabel = new Label( width, height, "", LabelAlignment.Center ) );
                hbox.Add( QuitLabel = new Label( width, height, "", LabelAlignment.Center ) );
                ResumeLabel.Click += new Action<Control, Point>( ResumeActivity_Click );
                CloseLabel.Click += new Action<Control, Point>( CloseBox_Click );
                QuitLabel.Click += new Action<Control, Point>( QuitActivity_Click );
            }
            vbox.AddHorizontalSeparator();
            {
                var hbox = vbox.AddLayoutHorizontal( height );
                var width2 = hbox.RemainingWidth / 2;
                {
                    var vbox2 = hbox.AddLayoutVertical( width2 );
                    vbox2.Add( EventNameLabel = new Label( vbox2.RemainingWidth, height, "", LabelAlignment.Left ) );
                }
                {
                    var vbox2 = hbox.AddLayoutVertical( width2 );
                    vbox2.Add( StatusLabel = new Label( vbox2.RemainingWidth, height, "", LabelAlignment.Left ) );
                    StatusLabel.Color = Color.LightSalmon;
                }
            }
            return vbox;
        }

        void ResumeActivity_Click(Control arg1, Point arg2)
        {
            TimeSpan diff = DateTime.Now - PopupTime;
            new ResumeActivityCommand( Owner.Viewer.Log, EventNameLabel.Text, diff.TotalMilliseconds / 1000 );
        }

        void CloseBox_Click(Control arg1, Point arg2)
        {
            TimeSpan diff = DateTime.Now - PopupTime;
            new CloseAndResumeActivityCommand( Owner.Viewer.Log, EventNameLabel.Text, diff.TotalMilliseconds / 1000 );
        }

        void QuitActivity_Click(Control arg1, Point arg2)
        {
            TimeSpan diff = DateTime.Now - PopupTime;
            new QuitActivityCommand( Owner.Viewer.Log, EventNameLabel.Text, diff.TotalMilliseconds / 1000 );
        }

        public void QuitActivity() {
            this.Visible = false;
            this.Activity.IsActivityWindowOpen = this.Visible;
            this.Activity.TriggeredEvent = null;
            Owner.Viewer.Simulator.Paused = false;   // Move to Viewer3D?
            this.Activity.IsActivityResumed = !Owner.Viewer.Simulator.Paused;
            Activity.IsSuccessful = false;
            Activity.IsComplete = true;
            if( Owner.Viewer.IsReplaying ) Owner.Viewer.Simulator.Confirmer.Confirm( CabControl.Activity, CabSetting.Off );
        }

        public void CloseBox() {
            this.Visible = false;
            this.Activity.IsActivityWindowOpen = this.Visible;
            this.Activity.TriggeredEvent = null;
            Owner.Viewer.Simulator.Paused = false;   // Move to Viewer3D?
            this.Activity.IsActivityResumed = !Owner.Viewer.Simulator.Paused;
            if( Owner.Viewer.IsReplaying ) Owner.Viewer.Simulator.Confirmer.Confirm( CabControl.Activity, CabSetting.On );
        }

        public void ResumeActivity() {
            this.Activity.TriggeredEvent = null;
            Owner.Viewer.Simulator.Paused = false;   // Move to Viewer3D?
            Activity.IsActivityResumed = !Owner.Viewer.Simulator.Paused;
            if( Owner.Viewer.IsReplaying ) Owner.Viewer.Simulator.Confirmer.Confirm( CabControl.Activity, CabSetting.On );
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame( elapsedTime, updateFull );

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
                            ComposeMenu(e.ParsedObject.Name, String.Format("This activity has ended {0}.\nFor a detailed evaluation, see the Help Window (F1).", Activity.IsSuccessful ? "successfully" : "without success"));
                            EndMenu();
                    }
                        else
                        {
                            var text = e.ParsedObject.Outcomes.DisplayMessage;
                            if (!String.IsNullOrEmpty(text))
                            {
                                if (Activity.ReopenActivityWindow)
                                {
                                    ComposeMenu( e.ParsedObject.Name, text );
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
                                    if (!Owner.Viewer.Simulator.Paused)
                                    {
                                        Owner.Viewer.Simulator.Paused = true;
                                        ComposeMenu( e.ParsedObject.Name, text );
                                        ResumeMenu();
                                        PopupTime = DateTime.Now;
                                    }
                                }
                                Visible = Owner.Viewer.HelpWindow.ActivityUpdated = true;
                            } else {
                                // Cancel the event as pop-up not needed.
                                Activity.TriggeredEvent = null;
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
            ResumeLabel.Text = "Resume";
            CloseLabel.Text = "Resume and close box";
            QuitLabel.Text = "Quit activity";
            StatusLabel.Text = "Status: Activity paused";
            StatusLabel.Color = Color.LightSalmon;
        }

        // <CJComment> At this point, would like to change dialog box background from solid to see-through,
        // but don't know how.
        // </CJComment>
        void CloseMenu()
        {
            ResumeLabel.Text = "";
            CloseLabel.Text = "Close box";
            QuitLabel.Text = "Quit activity";
            StatusLabel.Text = "Status: Activity resumed";
            StatusLabel.Color = Color.LightGreen;
        }

        void EndMenu()
        {
            ResumeLabel.Text = "";
            CloseLabel.Text = "";
            QuitLabel.Text = "End Activity";
            StatusLabel.Text = "Status: Activity paused";
            StatusLabel.Color = Color.LightSalmon;
        }

        void ComposeMenu(string eventLabel, string message)
        {
            EventNameLabel.Text = "Event: " + eventLabel;
            MessageScroller.SetScrollPosition(0);
            Message.Text = message;
        }
    }
}
