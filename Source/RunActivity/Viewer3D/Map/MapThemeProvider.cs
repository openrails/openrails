using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Orts.Viewer3D.Map
{
    public class MapThemeProvider
    {
        public void InitializeThemes()
        {
            var LightTheme = new ThemeStyle
            {
                BackColor = Color.Transparent,
                ForeColor = SystemColors.ControlText,
                PanelBackColor = SystemColors.Control,
                FlatStyle = FlatStyle.Standard,
                MapCanvasColor = Color.White,
                TrackColor = Color.FromArgb(46, 64, 83),
            };

            var DarkTheme = new ThemeStyle
            {
                BackColor = Color.FromArgb(44, 62, 80),
                ForeColor = Color.FromArgb(247, 249, 249),
                PanelBackColor = Color.FromArgb(28, 40, 51),
                FlatStyle = FlatStyle.Flat,
                MapCanvasColor = Color.FromArgb(44, 62, 80),
                TrackColor = Color.FromArgb(234, 236, 238),
            };

            // Reference for "solarized" themes: https://github.com/altercation/solarized?tab=readme-ov-file#the-values
            var LightSolarizedTheme = new ThemeStyle
            {
                BackColor = Color.FromArgb(253, 246, 227),
                ForeColor = Color.FromArgb(101, 123, 131),
                PanelBackColor = Color.FromArgb(238, 232, 213),
                FlatStyle = FlatStyle.Flat,
                MapCanvasColor = Color.FromArgb(253, 246, 227),
                TrackColor = Color.FromArgb(88, 110, 117),
            };

            var DarkSolarizedTheme = new ThemeStyle
            {
                BackColor = Color.FromArgb(0, 43, 54),
                ForeColor = Color.FromArgb(131, 148, 150),
                PanelBackColor = Color.FromArgb(28, 40, 51),
                FlatStyle = FlatStyle.Flat,
                MapCanvasColor = Color.FromArgb(0, 43, 54),
                TrackColor = Color.FromArgb(147, 161, 161),
            };

            Themes.Add("light", LightTheme);
            Themes.Add("light-solarized", LightSolarizedTheme);
            Themes.Add("dark-solarized", DarkSolarizedTheme);
            Themes.Add("dark", DarkTheme);
        }

        private readonly Dictionary<string, ThemeStyle> Themes = new Dictionary<string, ThemeStyle>();

        public ThemeStyle GetTheme(string themeName)
        {
            if (Themes.TryGetValue(themeName, out var theme))
            {
                return theme;
            }

            // Handle the case when the theme doesn't exist
            return null;
        }

        public string[] GetThemes()
        {
            return Themes.Keys.ToArray();
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


