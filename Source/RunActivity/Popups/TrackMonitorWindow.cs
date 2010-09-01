/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

/// Author: Laurie Heath
/// 
/// Track Monitor; used to display signal aspects speed limits etc.
/// 


using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ORTS.Popups
{
	public enum TrackMonitorSignalAspect
	{
		None,
		Clear,
		Warning,
		Stop,
	}

	public class TrackMonitorWindow : Window
	{
		Label SpeedCurrent;
		Label SpeedProjected;
		Label SignalDistance;
		Image SignalAspect;
		Label POILabel;
		Label POIDistance;

		float LastSpeedMpS;

		static readonly Dictionary<TrackMonitorSignalAspect, Rectangle> SignalAspectSources = InitSignalAspectSources();
		static Dictionary<TrackMonitorSignalAspect, Rectangle> InitSignalAspectSources()
		{
			return new Dictionary<TrackMonitorSignalAspect,Rectangle> {
				{ TrackMonitorSignalAspect.None, new Rectangle(0, 0, 16, 16) },
				{ TrackMonitorSignalAspect.Clear, new Rectangle(16, 0, 16, 16) },
				{ TrackMonitorSignalAspect.Warning, new Rectangle(0, 16, 16, 16) },
				{ TrackMonitorSignalAspect.Stop, new Rectangle(16, 16, 16, 16) },
			};
		}

		static readonly Dictionary<DispatcherPOIType, string> DispatcherPOILabels = InitDispatcherPOILabels();
		static Dictionary<DispatcherPOIType, string> InitDispatcherPOILabels()
		{
			return new Dictionary<DispatcherPOIType, string> {
				{ DispatcherPOIType.Unknown, "" },
				{ DispatcherPOIType.OffPath, "Off Path" },
				{ DispatcherPOIType.StationStop, "Station:" },
				{ DispatcherPOIType.ReversePoint, "Reverser:" },
				{ DispatcherPOIType.EndOfAuthorization, "End of Auth:" },
				{ DispatcherPOIType.Stop, "Stop:" },
			};
		}

		public TrackMonitorWindow(WindowManager owner)
			: base(owner, 150, 300, "Track Monitor")
		{
			AlignTop();
			AlignRight();
			SignalAspect.Texture = owner.Viewer.RenderProcess.Content.Load<Texture2D>("SignalAspects");
		}

		protected override ControlLayout Layout(ControlLayout layout)
		{
			var vbox = base.Layout(layout).AddLayoutVertical();
			{
				var hbox = vbox.AddLayoutHorizontal(16);
				hbox.Add(new Label(hbox.RemainingWidth / 2, hbox.RemainingHeight, "Speed:"));
				hbox.Add(SpeedCurrent = new Label(hbox.RemainingWidth, hbox.RemainingHeight, "", LabelAlignment.Right));
			}
			{
				var hbox = vbox.AddLayoutHorizontal(16);
				hbox.Add(new Label(hbox.RemainingWidth / 2, hbox.RemainingHeight, "Projected:"));
				hbox.Add(SpeedProjected= new Label(hbox.RemainingWidth, hbox.RemainingHeight, "", LabelAlignment.Right));
			}
			vbox.AddHorizontalSeparator();
			{
				var hbox = vbox.AddLayoutHorizontal(16);
				hbox.Add(new Label(hbox.RemainingWidth / 2, hbox.RemainingHeight, "Signal:"));
				hbox.Add(SignalDistance = new Label(hbox.RemainingWidth - 18, hbox.RemainingHeight, "0m", LabelAlignment.Right));
				hbox.AddSpace(2, 0);
				hbox.Add(SignalAspect = new Image(hbox.RemainingWidth, hbox.RemainingHeight));
			}
			{
				var hbox = vbox.AddLayoutHorizontal(16);
				hbox.Add(POILabel = new Label(hbox.RemainingWidth / 2, hbox.RemainingHeight, "POI:"));
				hbox.Add(POIDistance = new Label(hbox.RemainingWidth - 18, hbox.RemainingHeight, "0m", LabelAlignment.Right));
			}
			return vbox;
		}

		string FormatSpeed(float speed, bool metric)
		{
			return String.Format(metric ? "{0:F1}kph" : "{0:F1}mph", MpS.FromMpS(speed, metric));
		}

		string FormatDistance(float distance, bool metric)
		{
			if (metric)
			{
				// <0.1 kilometers, show meters.
				if (Math.Abs(distance) < 100)
					return String.Format("{0:N0}m", distance);
				return String.Format("{0:F1}km", distance / 1000.000);
			}
			// <0.1 miles, show yards.
			if (Math.Abs(distance) < 160.9344)
				return String.Format("{0:N0}yd", distance * 1.093613298337708);
			return String.Format("{0:F1}mi", distance / 1609.344);
		}

		public void UpdateText(ElapsedTime elapsedTime, bool milepostUnitsMetric, float speedMpS, float signalDistance, TrackMonitorSignalAspect signalAspect, DispatcherPOIType poiType, float poiDistance)
		{
			var speedProjectedMpS = speedMpS + 60 * (speedMpS - LastSpeedMpS) / elapsedTime.ClockSeconds;
			speedProjectedMpS = speedMpS > 0 ? Math.Max(0, speedProjectedMpS) : Math.Min(0, speedProjectedMpS);
			SpeedCurrent.Text = FormatSpeed(speedMpS, milepostUnitsMetric);
			SpeedProjected.Text = FormatSpeed(speedProjectedMpS, milepostUnitsMetric);
			LastSpeedMpS = speedMpS;

			SignalDistance.Text = signalAspect == TrackMonitorSignalAspect.None ? "" : FormatDistance(signalDistance, milepostUnitsMetric);
			SignalAspect.Source = SignalAspectSources[signalAspect];

			POILabel.Text = DispatcherPOILabels[poiType];
			POIDistance.Text = poiType == DispatcherPOIType.Unknown || poiType == DispatcherPOIType.OffPath ? "" : FormatDistance(poiDistance, milepostUnitsMetric);
		}
	}
}
