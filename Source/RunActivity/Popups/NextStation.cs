/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

/// Autor James Ross
/// 
/// Next Station; used to display the current and next timetable entries.
/// 


using System;
namespace ORTS
{
	public class NextStation : PopupWindow
	{
		PopupLabel CurrentTime;
        Viewer3D Viewer;

        PopupLabel lblPlatform;
        PopupLabel lblMessage;

        PopupLabel lblPrevSchArrive;
        PopupLabel lblPrevSchDepart;
        PopupLabel lblPrevActArrive;
        PopupLabel lblPrevActDepart;

        PopupLabel lblActSchArrive;
        PopupLabel lblActSchDepart;
        PopupLabel lblActActArrive;

        PopupLabel lblNxtSchArrive;
        PopupLabel lblNxtSchDepart;

        public NextStation(PopupWindows owner)
			: base(owner, 400, 135, "Next Station")
		{
            Viewer = owner.Viewer;
            AlignBottom();
			AlignLeft();
		}

		protected override PopupControlLayout Layout(PopupControlLayout layout)
		{
			var vbox = base.Layout(layout).AddLayoutVertical();
			var boxWidth = vbox.RemainingWidth / 6;
			{
				var hbox = vbox.AddLayoutHorizontal(16);
                lblPlatform = new PopupLabel(boxWidth, hbox.RemainingHeight, "?", PopupLabelAlignment.Right);
				hbox.Add(new PopupLabel(boxWidth, hbox.RemainingHeight, "Time:"));
				hbox.Add(CurrentTime = new PopupLabel(boxWidth, hbox.RemainingHeight, "00:00:00", PopupLabelAlignment.Right));
				hbox.AddSpace(boxWidth * 2, hbox.RemainingHeight);
				hbox.Add(new PopupLabel(boxWidth, hbox.RemainingHeight, "Next Station:"));
				hbox.Add(lblPlatform);
			}
			vbox.AddHorizontalSeparator();
			{
				var hbox = vbox.AddLayoutHorizontal(16);
				hbox.Add(new PopupLabel(boxWidth * 2, hbox.RemainingHeight, "Station"));
				hbox.Add(new PopupLabel(boxWidth, hbox.RemainingHeight, "Arrive", PopupLabelAlignment.Right));
				hbox.Add(new PopupLabel(boxWidth, hbox.RemainingHeight, "Actual", PopupLabelAlignment.Right));
				hbox.Add(new PopupLabel(boxWidth, hbox.RemainingHeight, "Depart", PopupLabelAlignment.Right));
				hbox.Add(new PopupLabel(boxWidth, hbox.RemainingHeight, "Actual", PopupLabelAlignment.Right));
			}
			{
				var hbox = vbox.AddLayoutHorizontal(16);
                lblPrevSchArrive = new PopupLabel(boxWidth, hbox.RemainingHeight, "?", PopupLabelAlignment.Right);
                lblPrevSchDepart = new PopupLabel(boxWidth, hbox.RemainingHeight, "?", PopupLabelAlignment.Right);
                lblPrevActArrive = new PopupLabel(boxWidth, hbox.RemainingHeight, "?", PopupLabelAlignment.Right);
                lblPrevActDepart = new PopupLabel(boxWidth, hbox.RemainingHeight, "?", PopupLabelAlignment.Right);
				hbox.Add(new PopupLabel(boxWidth * 2, hbox.RemainingHeight, "<previous station>"));
				hbox.Add(lblPrevSchArrive);
				hbox.Add(lblPrevActArrive);
				hbox.Add(lblPrevSchDepart);
				hbox.Add(lblPrevActDepart);
			}
			{
				var hbox = vbox.AddLayoutHorizontal(16);
                lblActSchArrive = new PopupLabel(boxWidth, hbox.RemainingHeight, "?", PopupLabelAlignment.Right);
                lblActSchDepart = new PopupLabel(boxWidth, hbox.RemainingHeight, "?", PopupLabelAlignment.Right);
                lblActActArrive = new PopupLabel(boxWidth, hbox.RemainingHeight, "?", PopupLabelAlignment.Right);
                hbox.Add(new PopupLabel(boxWidth * 2, hbox.RemainingHeight, "<next station>"));
				hbox.Add(lblActSchArrive);
				hbox.Add(lblActActArrive);
				hbox.Add(lblActSchDepart);
				hbox.AddSpace(boxWidth, hbox.RemainingHeight);
			}
			{
				var hbox = vbox.AddLayoutHorizontal(16);
                lblNxtSchArrive = new PopupLabel(boxWidth, hbox.RemainingHeight, "?", PopupLabelAlignment.Right);
                lblNxtSchDepart = new PopupLabel(boxWidth, hbox.RemainingHeight, "?", PopupLabelAlignment.Right);
                hbox.Add(new PopupLabel(boxWidth * 2, hbox.RemainingHeight, "<further station>"));
				hbox.Add(lblNxtSchArrive);
				hbox.AddSpace(boxWidth, hbox.RemainingHeight);
				hbox.Add(lblNxtSchDepart);
				hbox.AddSpace(boxWidth, hbox.RemainingHeight);
			}
			vbox.AddHorizontalSeparator();
			{
				var hbox = vbox.AddLayoutHorizontal(16);
                lblMessage = new PopupLabel(boxWidth * 5, hbox.RemainingHeight, "?", PopupLabelAlignment.Left);
				hbox.Add(new PopupLabel(boxWidth, hbox.RemainingHeight, "Message:"));
				hbox.Add(lblMessage);
			}
			return vbox;
		}

		public void UpdateText(ElapsedTime elapsedTime, double clockTime, Func<double, string> timeFormatter)
		{
			CurrentTime.Text = timeFormatter(clockTime);
            Activity act = Viewer.Simulator.ActivityRun;
            if (act != null)
            {
                ActivityTaskPassengerStopAt at = null;
                at = act.Current.PrevTask as ActivityTaskPassengerStopAt;
                if (at != null)
                {
                    lblPrevSchArrive.Text = at.SchArrive.ToString("HH:mm:ss");

                    if (at.ActArrive.HasValue)
                        lblPrevActArrive.Text = at.ActArrive.Value.ToString("HH:mm:ss");
                    else
                        lblPrevActArrive.Text = "-";

                    lblPrevSchDepart.Text = at.SchDepart.ToString("HH:mm:ss");

                    if (at.ActDepart.HasValue)
                        lblPrevActDepart.Text = at.ActDepart.Value.ToString("HH:mm:ss");
                    else
                        lblPrevActDepart.Text = "-";
                }
                else
                {
                    lblPrevSchArrive.Text = "-";
                    lblPrevActArrive.Text = "-";
                    lblPrevSchDepart.Text = "-";
                    lblPrevActDepart.Text = "-";
                }

                at = act.Current as ActivityTaskPassengerStopAt;
                if (at != null)
                {
                    lblActSchArrive.Text = at.SchArrive.ToString("HH:mm:ss");

                    if (at.ActArrive.HasValue)
                        lblActActArrive.Text = at.ActArrive.Value.ToString("HH:mm:ss");
                    else
                        lblActActArrive.Text = "-";

                    lblActSchDepart.Text = at.SchDepart.ToString("HH:mm:ss");

                    lblPlatform.Text = at.PlatformEnd1.PlatformName;
                    lblMessage.Text = at.DisplayMessage;
                }
                else
                {
                    lblActSchArrive.Text = "-";
                    lblActActArrive.Text = "-";
                    lblActSchDepart.Text = "-";
                    lblPlatform.Text = "-";
                    lblMessage.Text = "";
                }

                at = act.Current.NextTask as ActivityTaskPassengerStopAt;
                if (at != null)
                {
                    lblNxtSchArrive.Text = at.SchArrive.ToString("HH:mm:ss");
                    lblNxtSchDepart.Text = at.SchDepart.ToString("HH:mm:ss");
                }
                else
                {
                    lblNxtSchArrive.Text = "-";
                    lblNxtSchDepart.Text = "-";
                }
            }
		}

        public void UpdateSound()
        {
            Activity act = Viewer.Simulator.ActivityRun;
            if (act != null)
            {
                ActivityTask at = act.Current;
                if (at != null)
                {
                    if (at.SoundNotify != -1)
                    {
                        Viewer.IngameSounds.HandleEvent(at.SoundNotify);
                        at.SoundNotify = -1;
                    }
                }
            }
        }
	}
}
