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
        private SD.Brush brRed = new SD.SolidBrush(SD.Color.FromArgb(255, 255, 40, 40));
        private SD.Brush brGreen = new SD.SolidBrush(SD.Color.FromArgb(255, 0, 255, 196));
        private SD.Brush brAmber = new SD.SolidBrush(SD.Color.FromArgb(255, 255, 200, 0));

        private float distance;
        private SD.Font font = new SD.Font("Courier", 12, SD.FontStyle.Bold);

        public TrackMonitor( int x, int y)
            : base(x, y, 70, 150)
        {
            this.UseGraphics();
            InitialiseWindow(device);

        }
        private void InitialiseWindow(GraphicsDevice device)
        {
            this.Distance = 10000;
            this.Aspect = 1;
            this.AlignTop();
            this.AlignRignt();
        }

        //  Km to next signal. To do: cater for imperial measure
        public float Distance
        {
            set
            {
                distance = value / 1000;
            }
        }

        // Displays aspect.
        public int Aspect
        {
            set
            {
                SD.Graphics GR = this.puGraphics;
                GR.FillRectangle(SD.Brushes.Black, new SD.Rectangle(0, 0, 70, 150));
                switch (value)
                {
                    case 1:
                        GR.FillEllipse(brRed, new SD.Rectangle(20, 85, 20, 20));
                        break;
                    case 2:
                        GR.FillEllipse(brAmber, new SD.Rectangle(20, 60, 20, 20));
                        break;
                    case 3:
                        GR.FillEllipse(brAmber, new SD.Rectangle(20, 10, 20, 20));
                        GR.FillEllipse(brAmber, new SD.Rectangle(20, 60, 20, 20));
                        break;
                    case 4:
                        GR.FillEllipse(brGreen, new SD.Rectangle(20, 35, 20, 20));
                        break;
                }
                string sDist = distance.ToString("F2").PadLeft(5); ;
                GR.DrawString(sDist, font, SD.Brushes.White, 10, 110);
                this.UpdateGraphics();
            }
        }

    }
}
