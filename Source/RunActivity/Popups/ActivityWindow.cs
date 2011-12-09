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

namespace ORTS.Popups {
    public class ActivityWindow : Window {
        public Activity Activity;
        public TextFlow Message;
        public Label EventNameLabel;
        public Label ResumeLabel;
        public Label CloseLabel;
        public Label QuitLabel;
        public Label StatusLabel;

        // <CJ comment> Would like the dialog box background as solid black to indicate "simulator paused",
        // and change it later to see-through, if box is left on-screen when simulator resumes.
        // Don't know how.
        public ActivityWindow(WindowManager owner)
            : base(owner, 400, 180, "Activity Events") {
            Activity = Owner.Viewer.Simulator.ActivityRun;
        }

        protected override ControlLayout Layout(ControlLayout layout) {
            var vbox = base.Layout(layout).AddLayoutVertical();
            {
                var hbox = vbox.AddLayoutHorizontal(100);
                var scrollbox = hbox.AddLayoutScrollboxVertical(hbox.RemainingWidth);
                scrollbox.Add(Message = new TextFlow(scrollbox.RemainingWidth - ControlLayoutScrollbox.ScrollbarSize, ""));
            }
            vbox.AddHorizontalSeparator();
            var height = vbox.RemainingHeight / 2;
            var width = vbox.RemainingWidth / 3;
            {
                var hbox = vbox.AddLayoutHorizontal(height);
                hbox.Add(ResumeLabel = new Label(width, height, "Resume", LabelAlignment.Center));
                hbox.Add(CloseLabel = new Label(width, height, "Resume and close box", LabelAlignment.Center));
                hbox.Add(QuitLabel = new Label(width, height, "Quit activity", LabelAlignment.Center));
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
                    vbox2.Add(StatusLabel = new Label(vbox2.RemainingWidth, height, "Status: Activity paused", LabelAlignment.Left));
                    this.StatusLabel.Color = Color.LightSalmon;
                }
            }
            return vbox;
        }

        void ResumeActivity_Click(Control arg1, Point arg2) {
            ResumeLabel.Text = "";
            CloseLabel.Text = "Close box";
            ResumeActivity();
        }

        void CloseBox_Click(Control arg1, Point arg2) {
            this.Visible = false;
            ResumeActivity();
        }

        void QuitActivity_Click(Control arg1, Point arg2) {
            this.Visible = false;
            ResumeActivity();
            this.Activity.IsComplete = true;
            this.Activity.IsSuccessful = false;
        }

        // <CJ comment> At this point, would like to change dialog box background from solid to see-through,
        // but don't know how.
        void ResumeActivity() {
            this.Activity.TriggeredEvent = null;
            Owner.Viewer.Simulator.Paused = false;   // Move to Viewer3D?
            this.StatusLabel.Text = "Status: Activity resumed";
            this.StatusLabel.Color = Color.LightGreen;
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull) {
            base.PrepareFrame(elapsedTime, updateFull);

            if (updateFull) {
                Activity act = Owner.Viewer.Simulator.ActivityRun;
                if (act != null) {

                    var e = this.Activity.TriggeredEvent;
                    if (e != null) {
                        string text = e.ParsedObject.Outcomes.DisplayMessage;
                        if (text != null && text != "" && Owner.Viewer.Simulator.Paused == false) {
                            Owner.Viewer.Simulator.Paused = true;   // Move to Viewer3D?
                            this.EventNameLabel.Text = "Event: " + e.ParsedObject.Name;
                            this.Message.Text = text;
                            this.ResumeLabel.Text = "Resume";
                            this.CloseLabel.Text = "Resume and close box";
                            this.StatusLabel.Text = "Status: Activity paused";
                            this.StatusLabel.Color = Color.LightSalmon;
							Owner.Viewer.HelpWindow.ActivityUpdated = true;
							this.Visible = true;

                        } else { 
                            // Cancel the event as pop-up not needed.
                            this.Activity.TriggeredEvent = null;
                        }
                    }

                    if (this.Activity.IsComplete) {
                        string Outcome = "This activity has ended ";
                        if (this.Activity.IsSuccessful) {
                            Outcome += "successfully.";
                        } else {
                            Outcome += "without success.";
                        }

                        this.Visible = true;
                        this.Message.Text = Outcome + "\nFor a detailed evaluation, see the Help Window (F1)";
                        this.ResumeLabel.Text = "";
                        this.CloseLabel.Text = "";
                        this.QuitLabel.Text = "End Activity";
                    }
                }
            }
        }
    }
}