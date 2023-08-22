using System.Drawing;
using System.Windows.Forms;
using Orts.Formats.Msts;

namespace Orts.Viewer3D.Debugging
{
    public class MapThemeProvider
    {
        public ThemeStyle LightTheme;
        public ThemeStyle DarkTheme;

        public void InitializeThemes()
        {
            LightTheme = new ThemeStyle
            {
                BackColor = Color.Transparent,
                ForeColor = SystemColors.ControlText,
                PanelBackColor = SystemColors.Control,
                FlatStyle = FlatStyle.Standard,
                MapCanvasColor = Color.White,
                TrackColor = Color.FromArgb(46, 64, 83),
            };

            DarkTheme = new ThemeStyle
            {
                BackColor = Color.FromArgb(44, 62, 80),
                ForeColor = Color.FromArgb(247, 249, 249),
                PanelBackColor = Color.FromArgb(28, 40, 51),
                FlatStyle = FlatStyle.Flat,
                MapCanvasColor = Color.FromArgb(44, 62, 80),
                TrackColor = Color.FromArgb(234, 236, 238),
            };
        }
    }

    public class ThemeStyle
    {
        public Color BackColor;
        public Color ForeColor;
        public Color PanelBackColor;
        public FlatStyle FlatStyle;
        public Color MapCanvasColor;
        public Color TrackColor;
    }
}


