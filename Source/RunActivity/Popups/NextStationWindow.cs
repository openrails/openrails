/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

/// Author: James Ross
/// 
/// Next Station; used to display the current and next timetable entries.
/// 

using System;

namespace ORTS.Popups
{
	public class NextStationWindow : Window
	{
		readonly Viewer3D Viewer;

		Label CurrentTime;
		Label StationPlatform;

		Label StationPreviousName;
		Label StationPreviousArriveScheduled;
		Label StationPreviousArriveActual;
		Label StationPreviousDepartScheduled;
		Label StationPreviousDepartActual;

		Label StationCurrentName;
		Label StationCurrentArriveScheduled;
		Label StationCurrentArriveActual;
		Label StationCurrentDepartScheduled;

		Label StationNextName;
		Label StationNextArriveScheduled;
		Label StationNextDepartScheduled;

		Label Message;

		public NextStationWindow(WindowManager owner)
			: base(owner, 400, 135, "Next Station")
		{
			Viewer = owner.Viewer;
			AlignBottom();
			AlignLeft();
		}

		protected override ControlLayout Layout(ControlLayout layout)
		{
			var vbox = base.Layout(layout).AddLayoutVertical();
			var boxWidth = vbox.RemainingWidth / 7;
			{
				var hbox = vbox.AddLayoutHorizontal(16);
				hbox.Add(StationPlatform = new Label(boxWidth * 6, hbox.RemainingHeight, ""));
				hbox.Add(CurrentTime = new Label(boxWidth, hbox.RemainingHeight, "", LabelAlignment.Center));
			}
			vbox.AddHorizontalSeparator();
			{
				var hbox = vbox.AddLayoutHorizontal(16);
				hbox.Add(new Label(boxWidth * 3, hbox.RemainingHeight, "Station"));
				hbox.Add(new Label(boxWidth, hbox.RemainingHeight, "Arrive", LabelAlignment.Center));
				hbox.Add(new Label(boxWidth, hbox.RemainingHeight, "Actual", LabelAlignment.Center));
				hbox.Add(new Label(boxWidth, hbox.RemainingHeight, "Depart", LabelAlignment.Center));
				hbox.Add(new Label(boxWidth, hbox.RemainingHeight, "Actual", LabelAlignment.Center));
			}
			{
				var hbox = vbox.AddLayoutHorizontal(16);
				hbox.Add(StationPreviousName = new Label(boxWidth * 3, hbox.RemainingHeight, ""));
				hbox.Add(StationPreviousArriveScheduled = new Label(boxWidth, hbox.RemainingHeight, "", LabelAlignment.Center));
				hbox.Add(StationPreviousArriveActual = new Label(boxWidth, hbox.RemainingHeight, "", LabelAlignment.Center));
				hbox.Add(StationPreviousDepartScheduled = new Label(boxWidth, hbox.RemainingHeight, "", LabelAlignment.Center));
				hbox.Add(StationPreviousDepartActual = new Label(boxWidth, hbox.RemainingHeight, "", LabelAlignment.Center));
			}
			{
				var hbox = vbox.AddLayoutHorizontal(16);
				hbox.Add(StationCurrentName = new Label(boxWidth * 3, hbox.RemainingHeight, ""));
				hbox.Add(StationCurrentArriveScheduled = new Label(boxWidth, hbox.RemainingHeight, "", LabelAlignment.Center));
				hbox.Add(StationCurrentArriveActual = new Label(boxWidth, hbox.RemainingHeight, "", LabelAlignment.Center));
				hbox.Add(StationCurrentDepartScheduled = new Label(boxWidth, hbox.RemainingHeight, "", LabelAlignment.Center));
				hbox.AddSpace(boxWidth, hbox.RemainingHeight);
			}
			{
				var hbox = vbox.AddLayoutHorizontal(16);
				hbox.Add(StationNextName = new Label(boxWidth * 3, hbox.RemainingHeight, ""));
				hbox.Add(StationNextArriveScheduled = new Label(boxWidth, hbox.RemainingHeight, "", LabelAlignment.Center));
				hbox.AddSpace(boxWidth, hbox.RemainingHeight);
				hbox.Add(StationNextDepartScheduled = new Label(boxWidth, hbox.RemainingHeight, "", LabelAlignment.Center));
				hbox.AddSpace(boxWidth, hbox.RemainingHeight);
			}
			vbox.AddHorizontalSeparator();
			{
				var hbox = vbox.AddLayoutHorizontal(16);
				hbox.Add(Message = new Label(boxWidth * 7, hbox.RemainingHeight, ""));
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

                ActivityTaskPassengerStopAt Current = act.Current == null ? act.Last as ActivityTaskPassengerStopAt : act.Current as ActivityTaskPassengerStopAt;

                at = Current != null ? Current.PrevTask as ActivityTaskPassengerStopAt : null;
				if (at != null)
				{
					StationPreviousName.Text = at.PlatformEnd1.Station;
					StationPreviousArriveScheduled.Text = at.SchArrive.ToString("HH:mm:ss");
					StationPreviousArriveActual.Text = at.ActArrive.HasValue ? at.ActArrive.Value.ToString("HH:mm:ss") : "(missed)";
					StationPreviousDepartScheduled.Text = at.SchDepart.ToString("HH:mm:ss");
					StationPreviousDepartActual.Text = at.ActDepart.HasValue ? at.ActDepart.Value.ToString("HH:mm:ss") : "(missed)";
				}
				else
				{
					StationPreviousName.Text = "";
					StationPreviousArriveScheduled.Text = "";
					StationPreviousArriveActual.Text = "";
					StationPreviousDepartScheduled.Text = "";
					StationPreviousDepartActual.Text = "";
				}

				at = Current;
				if (at != null)
				{
					StationPlatform.Text = at.PlatformEnd1.PlatformName;
					StationCurrentName.Text = at.PlatformEnd1.Station;
					StationCurrentArriveScheduled.Text = at.SchArrive.ToString("HH:mm:ss");
					StationCurrentArriveActual.Text = at.ActArrive.HasValue ? at.ActArrive.Value.ToString("HH:mm:ss") : "";
					StationCurrentDepartScheduled.Text = at.SchDepart.ToString("HH:mm:ss");
					Message.Text = at.DisplayMessage;
				}
				else
				{
					StationPlatform.Text = "";
					StationCurrentName.Text = "";
					StationCurrentArriveScheduled.Text = "";
					StationCurrentArriveActual.Text = "";
					StationCurrentDepartScheduled.Text = "";
					Message.Text = "";
				}

				at = Current != null ? Current.NextTask as ActivityTaskPassengerStopAt : null;
				if (at != null)
				{
					StationNextName.Text = at.PlatformEnd1.Station;
					StationNextArriveScheduled.Text = at.SchArrive.ToString("HH:mm:ss");
					StationNextDepartScheduled.Text = at.SchDepart.ToString("HH:mm:ss");
				}
				else
				{
					StationNextName.Text = "";
					StationNextArriveScheduled.Text = "";
					StationNextDepartScheduled.Text = "";
				}

                if (act.IsFinished)
                {
                    Message.Text = "Activity completed.";
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
