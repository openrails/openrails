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

        public ActivityWindow(WindowManager owner)
            : base(owner, 400, 180, "Activity Events")
        {
            Activity = Owner.Viewer.Simulator.ActivityRun;
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            var vbox = base.Layout(layout).AddLayoutVertical();
            {
                var hbox = vbox.AddLayoutHorizontal(100);
                var scrollbox = hbox.AddLayoutScrollboxVertical(hbox.RemainingWidth);
                scrollbox.Add(Message = new TextFlow(scrollbox.RemainingWidth - ControlLayoutScrollbox.ScrollbarSize, ""));
                MessageScroller = (ControlLayoutScrollbox)hbox.Controls.Last();
            }
            vbox.AddHorizontalSeparator();
            var height = vbox.RemainingHeight / 2;
            var width = vbox.RemainingWidth / 3;
            {
                var hbox = vbox.AddLayoutHorizontal(height);
                hbox.Add(ResumeLabel = new Label(width, height, "", LabelAlignment.Center));
                hbox.Add(CloseLabel = new Label(width, height, "", LabelAlignment.Center));
                hbox.Add(QuitLabel = new Label(width, height, "", LabelAlignment.Center));
                ResumeLabel.Click += new Action<Control, Point>(ResumeActivity_Click);
                CloseLabel.Click += new Action<Control, Point>(CloseBox_Click);
                QuitLabel.Click += new Action<Control, Point>(QuitActivity_Click);
            }
            vbox.AddHorizontalSeparator();
            {
                var hbox = vbox.AddLayoutHorizontal(height);
                var width2 = hbox.RemainingWidth / 2;
                {
                    var vbox2 = hbox.AddLayoutVertical(width2);
                    vbox2.Add(EventNameLabel = new Label(vbox2.RemainingWidth, height, "", LabelAlignment.Left));
                }
                {
                    var vbox2 = hbox.AddLayoutVertical(width2);
                    vbox2.Add(StatusLabel = new Label(vbox2.RemainingWidth, height, "", LabelAlignment.Left));
                    StatusLabel.Color = Color.LightSalmon;
                }
            }
            return vbox;
        }

        void ResumeActivity_Click(Control arg1, Point arg2)
        {
            ResumeActivity();
            CloseMenu();
        }

        void CloseBox_Click(Control arg1, Point arg2)
        {
            CloseBox();
        }

        void QuitActivity_Click(Control arg1, Point arg2)
        {
            CloseBox();
            Activity.IsSuccessful = false;
            Activity.IsComplete = true;
        }

        void CloseBox()
        {
            Visible = false;
            Activity.IsActivityWindowOpen = Visible;
            ResumeActivity();
        }

        void ResumeActivity()
        {
            Activity.TriggeredEvent = null;
            Owner.Viewer.Simulator.Paused = false;   // Move to Viewer3D?
            Activity.IsActivityResumed = !Owner.Viewer.Simulator.Paused;
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
                                    }
                                }
                                else
                                {
                                    // Only needs updating the first time through
                                    if (!Owner.Viewer.Simulator.Paused)
                                    {
                                        Owner.Viewer.Simulator.Paused = true;
                                        ComposeMenu(e.ParsedObject.Name, text);
                                        ResumeMenu();
                                    }
                                }
                                Visible = Owner.Viewer.HelpWindow.ActivityUpdated = true;
                            }
                            else
                            {
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

        // <CJ comment> Would like the dialog box background as solid black to indicate "simulator paused",
        // and change it later to see-through, if box is left on-screen when simulator resumes.
        // Don't know how.
        // </CJ comment>
        void ResumeMenu()
        {
            ResumeLabel.Text = "Resume";
            CloseLabel.Text = "Resume and close box";
            QuitLabel.Text = "Quit activity";
            StatusLabel.Text = "Status: Activity paused";
            StatusLabel.Color = Color.LightSalmon;
        }

        // <CJ comment> At this point, would like to change dialog box background from solid to see-through,
        // but don't know how.
        // </CJ comment>
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
