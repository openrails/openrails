/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

/// Autor Laurie Heath
/// 
/// Track Monitor; used to display signal aspects speed limits etc.
/// 


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using SD = System.Drawing;
using SDI = System.Drawing.Imaging;
using MSTS;

namespace ORTS
{
	public class TrackMonitor : PopupWindow
	{
		PopupLabel SignalDistance;
		PopupImage SignalAspect;
		PopupLabel POILabel;
		PopupLabel POIDistance;

		static readonly Dictionary<DispatcherPOIType, string> DispatcherPOILabels = InitDispatcherPOILabels();
		static Dictionary<DispatcherPOIType, string> InitDispatcherPOILabels()
		{
			return new Dictionary<DispatcherPOIType, string> {
				{ DispatcherPOIType.Unknown, "" },
				{ DispatcherPOIType.OffPath, "Off Path" },
				{ DispatcherPOIType.StationStop, "Station:" },
				{ DispatcherPOIType.ReversePoint, "Reverser:" },
				{ DispatcherPOIType.EndOfAuthorization, "End of Auth:" },
				{ DispatcherPOIType.Stop, "???" },
			};
		}

		public TrackMonitor(PopupWindows owner)
			: base(owner, 150, 300, "Track Monitor")
		{
			AlignTop();
			AlignRight();
		}

		protected override PopupControlLayout Layout(PopupControlLayout layout)
		{
			var vbox = base.Layout(layout).AddLayoutVertical();
			{
				var hbox = vbox.AddLayoutHorizontal(16);
				hbox.Add(new PopupLabel(hbox.RemainingWidth / 2, hbox.RemainingHeight, "Signal:"));
				hbox.Add(SignalDistance = new PopupLabel(hbox.RemainingWidth - 18, hbox.RemainingHeight, "0m", PopupLabelAlignment.Right));
				hbox.AddSpace(2, 0);
				hbox.Add(SignalAspect = new PopupImage(hbox.RemainingWidth, hbox.RemainingHeight, null));
			}
			{
				var hbox = vbox.AddLayoutHorizontal(16);
				hbox.Add(POILabel = new PopupLabel(hbox.RemainingWidth / 2, hbox.RemainingHeight, "POI:"));
				hbox.Add(POIDistance = new PopupLabel(hbox.RemainingWidth - 18, hbox.RemainingHeight, "0m", PopupLabelAlignment.Right));
			}
			return vbox;
		}

		public void Update(float signalDistance, int signalAspect, DispatcherPOIType poiType, float poiDistance)
		{
			SignalDistance.Text = String.Format("{0:N0}m", signalDistance);
			POILabel.Text = DispatcherPOILabels[poiType];
			POIDistance.Text = poiType == DispatcherPOIType.Unknown || poiType == DispatcherPOIType.OffPath ? "" : String.Format("{0:N0}m", poiDistance);
		}

		// Displays aspect.
		//public int Aspect
		//{
		//    set
		//    {
		//        //SD.Graphics GR = this.puGraphics;
		//        //GR.FillRectangle(SD.Brushes.Black, new SD.Rectangle(0, 0, 70, 150));
		//        //switch (value)
		//        //{
		//        //    case 1:
		//        //        GR.FillEllipse(brRed, new SD.Rectangle(20, 85, 20, 20));
		//        //        break;
		//        //    case 2:
		//        //        GR.FillEllipse(brAmber, new SD.Rectangle(20, 60, 20, 20));
		//        //        break;
		//        //    case 3:
		//        //        GR.FillEllipse(brAmber, new SD.Rectangle(20, 10, 20, 20));
		//        //        GR.FillEllipse(brAmber, new SD.Rectangle(20, 60, 20, 20));
		//        //        break;
		//        //    case 4:
		//        //        GR.FillEllipse(brGreen, new SD.Rectangle(20, 35, 20, 20));
		//        //        break;
		//        //}
		//        //string sDist = distance.ToString("F2").PadLeft(5); ;
		//        //GR.DrawString(sDist, font, SD.Brushes.White, 10, 110);
		//        //this.UpdateGraphics();
		//    }
		//}
	}
}
