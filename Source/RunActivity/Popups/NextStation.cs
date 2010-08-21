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

		public NextStation(PopupWindows owner)
			: base(owner, 400, 135, "Next Station")
		{
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
				hbox.Add(CurrentTime = new PopupLabel(boxWidth, hbox.RemainingHeight, "00:00:00", PopupLabelAlignment.Right));
				hbox.AddSpace(boxWidth * 2, hbox.RemainingHeight);
				hbox.Add(new PopupLabel(boxWidth, hbox.RemainingHeight, "Next Station:"));
				hbox.Add(new PopupLabel(boxWidth, hbox.RemainingHeight, "?", PopupLabelAlignment.Right));
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
				hbox.Add(new PopupLabel(boxWidth * 2, hbox.RemainingHeight, "<previous station>"));
				hbox.Add(new PopupLabel(boxWidth, hbox.RemainingHeight, "?", PopupLabelAlignment.Right));
				hbox.Add(new PopupLabel(boxWidth, hbox.RemainingHeight, "?", PopupLabelAlignment.Right));
				hbox.Add(new PopupLabel(boxWidth, hbox.RemainingHeight, "?", PopupLabelAlignment.Right));
				hbox.Add(new PopupLabel(boxWidth, hbox.RemainingHeight, "?", PopupLabelAlignment.Right));
			}
			{
				var hbox = vbox.AddLayoutHorizontal(16);
				hbox.Add(new PopupLabel(boxWidth * 2, hbox.RemainingHeight, "<next station>"));
				hbox.Add(new PopupLabel(boxWidth, hbox.RemainingHeight, "?", PopupLabelAlignment.Right));
				hbox.Add(new PopupLabel(boxWidth, hbox.RemainingHeight, "?", PopupLabelAlignment.Right));
				hbox.Add(new PopupLabel(boxWidth, hbox.RemainingHeight, "?", PopupLabelAlignment.Right));
				hbox.AddSpace(boxWidth, hbox.RemainingHeight);
			}
			{
				var hbox = vbox.AddLayoutHorizontal(16);
				hbox.Add(new PopupLabel(boxWidth * 2, hbox.RemainingHeight, "<further station>"));
				hbox.Add(new PopupLabel(boxWidth, hbox.RemainingHeight, "?", PopupLabelAlignment.Right));
				hbox.AddSpace(boxWidth, hbox.RemainingHeight);
				hbox.Add(new PopupLabel(boxWidth, hbox.RemainingHeight, "?", PopupLabelAlignment.Right));
				hbox.AddSpace(boxWidth, hbox.RemainingHeight);
			}
			vbox.AddHorizontalSeparator();
			{
				var hbox = vbox.AddLayoutHorizontal(16);
				hbox.Add(new PopupLabel(boxWidth, hbox.RemainingHeight, "Loading Time:"));
				hbox.Add(new PopupLabel(boxWidth, hbox.RemainingHeight, "?", PopupLabelAlignment.Right));
			}
			return vbox;
		}

		public void UpdateText(ElapsedTime elapsedTime, double clockTime, Func<double, string> timeFormatter)
		{
			CurrentTime.Text = timeFormatter(clockTime);
		}
	}
}
