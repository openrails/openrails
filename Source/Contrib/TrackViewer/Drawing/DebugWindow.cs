using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ORTS.TrackViewer.Drawing
{
    /// <summary>
    /// This enables the drawing of a debug string on the main TrackViewer screen.
    /// Obviously this is something that should only be used for debug, and therefore be temporary.
    /// To use it, you have to set DrawString to a value.
    /// </summary>
    public class DebugWindow
    {
        private static List<DebugWindow> debugWindows = new List<DebugWindow>();

        /// <summary>
        /// Draw all available debug windows
        /// </summary>
        public static void DrawAll() {
            //Just a safety. Normally there should be no DebugWindows once released.
            if (!System.Diagnostics.Debugger.IsAttached) return;
            foreach (DebugWindow window in debugWindows)
            {
                window.Draw();
            }
        }

        /// <summary>Stored location (in pixels) of where the string will be drawn</summary>
        private Vector2 startLocation;

        /// <summary>The string that will be drawn. Set this to what you want to show during debug</summary>
        public string DrawString { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="windowOffsetX">Offset of the string to drawn in X-direction (in pixels), from top-left</param>
        /// <param name="windowOffsetY">Offset of the string to drawn in Y-direction (in pixels), from top-left</param>
        public DebugWindow(int windowOffsetX, int windowOffsetY)
        {
            startLocation = new Vector2(windowOffsetX, windowOffsetY);

            this.DrawString = String.Empty;
            debugWindows.Add(this);
        }

        /// <summary>
        /// This simply draws the debug string.
        /// </summary>
        private void Draw()
        {
            BasicShapes.DrawString(startLocation, Color.Black, DrawString);
        }
    }
}
