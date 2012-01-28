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

        public ActivityWindow( WindowManager owner )
            : base( owner, 400, 180, "Activity Events" ) {
            Activity = Owner.Viewer.Simulator.ActivityRun;
        }

        protected override ControlLayout Layout( ControlLayout layout ) {
            var vbox = base.Layout( layout ).AddLayoutVertical();
            {
                var hbox = vbox.AddLayoutHorizontal( 100 );
                var scrollbox = hbox.AddLayoutScrollboxVertical( hbox.RemainingWidth );
                scrollbox.Add( Message = new TextFlow( scrollbox.RemainingWidth - ControlLayoutScrollbox.ScrollbarSize, "" ) );
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
                    this.StatusLabel.Color = Color.LightSalmon;
                }
            }
            return vbox;
        }

        void ResumeActivity_Click( Control arg1, Point arg2 ) {
            ResumeActivity();
            CloseMenu();
        }

        void CloseBox_Click( Control arg1, Point arg2 ) {
            CloseBox();
        }

        void QuitActivity_Click( Control arg1, Point arg2 ) {
            CloseBox();
            this.Activity.IsSuccessful = false;
            this.Activity.IsComplete = true;
        }

        void CloseBox() {
            this.Visible = false;
            this.Activity.IsActivityWindowOpen = this.Visible;
            ResumeActivity();
        }

        void ResumeActivity() {
            this.Activity.TriggeredEvent = null;
            Owner.Viewer.Simulator.Paused = false;   // Move to Viewer3D?
            this.Activity.IsActivityResumed = !Owner.Viewer.Simulator.Paused;
        }

        public override void PrepareFrame( ElapsedTime elapsedTime, bool updateFull ) {
            base.PrepareFrame( elapsedTime, updateFull );

            EventWrapper e;

            if( updateFull ) {
                Activity act = Owner.Viewer.Simulator.ActivityRun;
                if( act != null ) {

                    if( this.Activity.ReopenActivityWindow ) {
                        e = this.Activity.LastTriggeredEvent;
                    } else {
                        e = this.Activity.TriggeredEvent;
                    }
                    if( e != null ) {
                        if( this.Activity.IsComplete ) {
                            Owner.Viewer.Simulator.Paused = true;
                            string Outcome = "This activity has ended ";
                            if( this.Activity.IsSuccessful ) {
                                Outcome += "successfully.";
                            } else {
                                Outcome += "without success.";
                            }
                            this.Visible = true;
                            this.Activity.IsActivityWindowOpen = true;
                            ComposeMenu( e.ParsedObject.Name, Outcome + "\nFor a detailed evaluation, see the Help Window (F1)" );
                            EndMenu();
                            Owner.Viewer.HelpWindow.ActivityUpdated = true;
                            this.Visible = true;
                        } else {
                            string text = e.ParsedObject.Outcomes.DisplayMessage;
                            if( text != null && text != "" ) {
                                if( this.Activity.ReopenActivityWindow ) {
                                    ComposeMenu( e.ParsedObject.Name, text );
                                    if( this.Activity.IsActivityResumed ) {
                                        ResumeActivity();
                                        CloseMenu();
                                    } else {
                                        Owner.Viewer.Simulator.Paused = true;
                                        ResumeMenu();
                                    }
                                } else {
                                    // Only needs updating the first time through
                                    if( Owner.Viewer.Simulator.Paused == false ) {
                                        Owner.Viewer.Simulator.Paused = true;
                                        ComposeMenu( e.ParsedObject.Name, text );
                                        ResumeMenu();
                                    }
                                }
                                Owner.Viewer.HelpWindow.ActivityUpdated = true;
                                this.Visible = true;
                            } else {
                                // Cancel the event as pop-up not needed.
                                this.Activity.TriggeredEvent = null;
                            }
                        }
                    }
                    this.Activity.IsActivityResumed = !Owner.Viewer.Simulator.Paused;
                    this.Activity.IsActivityWindowOpen = this.Visible;
                    this.Activity.ReopenActivityWindow = false;
                }
            }
        }

        // <CJ comment> Would like the dialog box background as solid black to indicate "simulator paused",
        // and change it later to see-through, if box is left on-screen when simulator resumes.
        // Don't know how.
        void ResumeMenu() {
            this.ResumeLabel.Text = "Resume";
            this.CloseLabel.Text = "Resume and close box";
            this.QuitLabel.Text = "Quit activity";
            this.StatusLabel.Text = "Status: Activity paused";
            this.StatusLabel.Color = Color.LightSalmon;
        }

        // <CJ comment> At this point, would like to change dialog box background from solid to see-through,
        // but don't know how.
        void CloseMenu() {
            this.ResumeLabel.Text = "";
            this.CloseLabel.Text = "Close box";
            this.QuitLabel.Text = "Quit activity";
            this.StatusLabel.Text = "Status: Activity resumed";
            this.StatusLabel.Color = Color.LightGreen;
        }

        void EndMenu() {
            this.ResumeLabel.Text = "";
            this.CloseLabel.Text = "";
            this.QuitLabel.Text = "End Activity";
            this.StatusLabel.Text = "Status: Activity paused";
            this.StatusLabel.Color = Color.LightSalmon;
        }

        void ComposeMenu( string eventLabel, string message ) {
            this.EventNameLabel.Text = "Event: " + eventLabel;
            this.Message.Text = message;
        }
    }
}