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
		readonly Viewer3D Viewer;

		PopupLabel CurrentTime;
		PopupLabel StationPlatform;

		PopupLabel StationPreviousName;
		PopupLabel StationPreviousArriveScheduled;
		PopupLabel StationPreviousArriveActual;
		PopupLabel StationPreviousDepartScheduled;
		PopupLabel StationPreviousDepartActual;

		PopupLabel StationCurrentName;
		PopupLabel StationCurrentArriveScheduled;
		PopupLabel StationCurrentArriveActual;
		PopupLabel StationCurrentDepartScheduled;

		PopupLabel StationNextName;
		PopupLabel StationNextArriveScheduled;
		PopupLabel StationNextDepartScheduled;

		PopupLabel Message;

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
				hbox.Add(new PopupLabel(boxWidth, hbox.RemainingHeight, "Time:"));
				hbox.Add(CurrentTime = new PopupLabel(boxWidth, hbox.RemainingHeight, "", PopupLabelAlignment.Right));
				hbox.AddSpace(boxWidth, hbox.RemainingHeight);
				hbox.Add(new PopupLabel(boxWidth, hbox.RemainingHeight, "Next:"));
				hbox.Add(StationPlatform = new PopupLabel(boxWidth * 2, hbox.RemainingHeight, "", PopupLabelAlignment.Right));
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
				hbox.Add(StationPreviousName = new PopupLabel(boxWidth * 2, hbox.RemainingHeight, ""));
				hbox.Add(StationPreviousArriveScheduled = new PopupLabel(boxWidth, hbox.RemainingHeight, "", PopupLabelAlignment.Right));
				hbox.Add(StationPreviousArriveActual = new PopupLabel(boxWidth, hbox.RemainingHeight, "", PopupLabelAlignment.Right));
				hbox.Add(StationPreviousDepartScheduled = new PopupLabel(boxWidth, hbox.RemainingHeight, "", PopupLabelAlignment.Right));
				hbox.Add(StationPreviousDepartActual = new PopupLabel(boxWidth, hbox.RemainingHeight, "", PopupLabelAlignment.Right));
			}
			{
				var hbox = vbox.AddLayoutHorizontal(16);
				hbox.Add(StationCurrentName = new PopupLabel(boxWidth * 2, hbox.RemainingHeight, ""));
				hbox.Add(StationCurrentArriveScheduled = new PopupLabel(boxWidth, hbox.RemainingHeight, "", PopupLabelAlignment.Right));
				hbox.Add(StationCurrentArriveActual = new PopupLabel(boxWidth, hbox.RemainingHeight, "", PopupLabelAlignment.Right));
				hbox.Add(StationCurrentDepartScheduled = new PopupLabel(boxWidth, hbox.RemainingHeight, "", PopupLabelAlignment.Right));
				hbox.AddSpace(boxWidth, hbox.RemainingHeight);
			}
			{
				var hbox = vbox.AddLayoutHorizontal(16);
				hbox.Add(StationNextName = new PopupLabel(boxWidth * 2, hbox.RemainingHeight, ""));
				hbox.Add(StationNextArriveScheduled = new PopupLabel(boxWidth, hbox.RemainingHeight, "", PopupLabelAlignment.Right));
				hbox.AddSpace(boxWidth, hbox.RemainingHeight);
				hbox.Add(StationNextDepartScheduled = new PopupLabel(boxWidth, hbox.RemainingHeight, "", PopupLabelAlignment.Right));
				hbox.AddSpace(boxWidth, hbox.RemainingHeight);
			}
			vbox.AddHorizontalSeparator();
			{
				var hbox = vbox.AddLayoutHorizontal(16);
				hbox.Add(Message = new PopupLabel(boxWidth * 6, hbox.RemainingHeight, ""));
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
