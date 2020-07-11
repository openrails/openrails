// COPYRIGHT 2014, 2018 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using ORTS.TrackViewer.UserInterface;

namespace ORTS.TrackViewer.Drawing
{
    #region class DrawColors
    /// <summary>
    /// This class contains the definition of all colors used during drawing.
    /// By having that in one class it is much easier to change colors in case that is needed.
    /// Different colorScheme's are supported. This means that drawing, for instance, a track
    /// can be done from different parts of the program using a different colorscheme, such that the drawing gets the color
    /// belonging to that colorscheme. This allows reusing the drawing routine and still get different colors.
    /// The colorschemes themselves are also defined here.
    /// 
    /// </summary>
    /// <remarks> Currently both colorschemes and colors are identified by a  string. Possibly it is better to use enumerations.
    /// </remarks>
    static class DrawColors
    {

        public static ColorScheme colorsNormal    = new ColorScheme();
        public static ColorScheme colorsHighlight = new ColorScheme(HighlightType.Highlight);
        public static ColorScheme colorsHotlight  = new ColorScheme(HighlightType.Hotlight);  // even more highlighting
        public static ColorScheme colorsRoads = new ColorScheme();
        public static ColorScheme colorsRoadsHighlight = new ColorScheme(HighlightType.Highlight);
        public static ColorScheme colorsRoadsHotlight = new ColorScheme(HighlightType.Hotlight);
        public static ColorScheme colorsPathMain = new ColorScheme();
        public static ColorScheme colorsPathSiding = new ColorScheme();

        static ColorsGroupTrack trackGroupFlat = new ColorsGroupTrack();
        static ColorsGroupTrack roadTrackGroupFlat = new ColorsGroupTrack();
        static ColorsGroupTrack trackGroupColoured = new ColorsGroupTrack();
        static ColorsGroupTrack roadTrackGroupColoured = new ColorsGroupTrack();
        static ColorsGroupTrack trackGroupTerrain = new ColorsGroupTrack();
        static ColorsGroupTrack roadTrackGroupTerrain = new ColorsGroupTrack();
        
        static ColorsGroupBackground backgroundWithTilesGroup = new ColorsGroupBackground();
        static ColorsGroupBackground backgroundWithoutTilesGroup = new ColorsGroupBackground();

        public static ColorWithHighlights otherPathsReferenceColor;

        /// <summary>
        /// Do the initialization of the settings (set the defaults)
        /// </summary>
        public static void Initialize(IPreferenceChanger preferenceChanger)
        {
            SetBasicColors(preferenceChanger);
            SetTrackColors(preferenceChanger);
            SetItemColors(preferenceChanger);
            SetPathColors(preferenceChanger);
            SetBackgroundColors(preferenceChanger);
            SetShadedColors(preferenceChanger);

            SetColoursFromOptions(true, false, false); //just a default
         }

        private static void SetPathColors(IPreferenceChanger preferenceChanger)
        {
            ColorWithHighlights brokenPath = new ColorWithHighlights(Color.Salmon, 40);
            brokenPath.MakeIntoUserPreference(preferenceChanger, "brokenpath",
                TrackViewer.catalog.GetString("Select broken path color"));

            ColorWithHighlights brokenNode = new ColorWithHighlights(Color.Red, 40);

            ColorWithHighlights pathMain = new ColorWithHighlights(Color.Yellow, 20);
            pathMain.MakeIntoUserPreference(preferenceChanger, "pathmain", 
                TrackViewer.catalog.GetString("Select path color (main)"));
            ColorsGroupTrack pathMainGroup = new ColorsGroupTrack
            {
                TrackCurved = pathMain,
                TrackStraight = pathMain,
                BrokenNode = brokenNode,
                BrokenPath = brokenPath
            };
            colorsPathMain.TrackColors = pathMainGroup;

            ColorWithHighlights pathSiding = new ColorWithHighlights(Color.Orange, 20);
            pathSiding.MakeIntoUserPreference(preferenceChanger, "pathsiding", 
                TrackViewer.catalog.GetString("Select path color (siding)"));
            ColorsGroupTrack pathSidingGroup = new ColorsGroupTrack
            {
                TrackCurved = pathSiding,
                TrackStraight = pathSiding,
                BrokenNode = brokenNode,
                BrokenPath = brokenPath
            };
            colorsPathSiding.TrackColors = pathSidingGroup;


            
        }

        private static void SetTrackColors(IPreferenceChanger preferenceChanger)
        {
            ColorWithHighlights trackColorColouredStraight = new ColorWithHighlights(Color.Black, Color.Red, Color.LightGoldenrodYellow);
            ColorWithHighlights trackColorColouredCurved = new ColorWithHighlights(Color.Green, Color.Tomato, Color.LightGoldenrodYellow);
            ColorWithHighlights roadTrackColorColouredStraight = new ColorWithHighlights(Color.Gray, 40);
            ColorWithHighlights roadTrackColorColouredCurved = new ColorWithHighlights(Color.DarkOliveGreen, 40);

            trackGroupColoured.TrackStraight = trackColorColouredStraight;
            trackGroupColoured.TrackCurved = trackColorColouredCurved;
            roadTrackGroupColoured.TrackStraight = roadTrackColorColouredStraight;
            roadTrackGroupColoured.TrackCurved = roadTrackColorColouredCurved;

            ColorWithHighlights trackColorFlat = new ColorWithHighlights(Color.Black, Color.Tomato, Color.Red);
            ColorWithHighlights roadTrackColorFlat = new ColorWithHighlights(Color.DarkGray, 20);
            trackGroupFlat.TrackStraight = trackColorFlat;
            trackGroupFlat.TrackCurved = trackColorFlat;
            roadTrackGroupFlat.TrackStraight = roadTrackColorFlat;
            roadTrackGroupFlat.TrackCurved = roadTrackColorFlat;

            ColorWithHighlights trackColorTerrain = new ColorWithHighlights(Color.Aquamarine, 0);
            ColorWithHighlights roadTrackColorTerrain = new ColorWithHighlights(Color.LightGray, 0);
            trackGroupTerrain.TrackStraight = trackColorTerrain;
            trackGroupTerrain.TrackCurved = trackColorTerrain;
            roadTrackGroupTerrain.TrackStraight = roadTrackColorTerrain;
            roadTrackGroupTerrain.TrackCurved = roadTrackColorTerrain;

            trackColorColouredStraight.MakeIntoUserPreference(preferenceChanger, "track_colored_straight", TrackViewer.catalog.GetString("Select color for multi-colored straight tracks"), true);
            trackColorColouredCurved.MakeIntoUserPreference(preferenceChanger, "track_colored_curved", TrackViewer.catalog.GetString("Select color for multi-colored curved tracks"), true);
            trackColorFlat.MakeIntoUserPreference(preferenceChanger, "track_flat", TrackViewer.catalog.GetString("Select color for mono-colored tracks"), true);
            trackColorTerrain.MakeIntoUserPreference(preferenceChanger, "track_terrain", TrackViewer.catalog.GetString("Select color for tracks on terrain"));

            roadTrackColorColouredStraight.MakeIntoUserPreference(preferenceChanger, "road_colored_straight", TrackViewer.catalog.GetString("Select color for multi-colored straight roads"), true);
            roadTrackColorColouredCurved.MakeIntoUserPreference(preferenceChanger, "road_colored_curved", TrackViewer.catalog.GetString("Select color for multi-colored curved roads"), true);
            roadTrackColorFlat.MakeIntoUserPreference(preferenceChanger, "road_flat", TrackViewer.catalog.GetString("Select color for mono-colored roads"), true);
            roadTrackColorTerrain.MakeIntoUserPreference(preferenceChanger, "road_terrain", TrackViewer.catalog.GetString("Select color for roads on terrain"));
        }

        private static void SetBackgroundColors(IPreferenceChanger preferenceChanger)
        {
            ColorWithHighlights fixedBackgroundColor = new ColorWithHighlights(Color.White, 20);
            ColorWithHighlights changingBackgroundColor = new ColorWithHighlights(Color.PaleGreen, 20);
            changingBackgroundColor.MakeIntoUserPreference(preferenceChanger, "background", 
                TrackViewer.catalog.GetString("Select background color"));
            backgroundWithoutTilesGroup.Tile = changingBackgroundColor;
            backgroundWithoutTilesGroup.ClearWindow = changingBackgroundColor;
            backgroundWithTilesGroup.Tile = changingBackgroundColor;
            backgroundWithTilesGroup.ClearWindow = fixedBackgroundColor;
        }

        private static void SetBasicColors(IPreferenceChanger preferenceChanger)
        {
            ColorsGroupBasic basicColors = new ColorsGroupBasic();

            ColorWithHighlights basicColor;

            basicColor = new ColorWithHighlights(Color.Black, 40);
            basicColor.MakeIntoUserPreference(preferenceChanger, "text",
                TrackViewer.catalog.GetString("Select text color"));
            basicColors.Text = basicColor;
        }

        private static void SetItemColors(IPreferenceChanger preferenceChanger)
        {
            ColorsGroupBasic itemColors = new ColorsGroupBasic();

            ColorWithHighlights itemColor;

            itemColor = new ColorWithHighlights(Color.Black, 40);
            itemColor.MakeIntoUserPreference(preferenceChanger, "text", 
                TrackViewer.catalog.GetString("Select item text color"));
            itemColors.Text = itemColor;

            itemColor = new ColorWithHighlights(Color.Blue, 120);
            itemColor.MakeIntoUserPreference(preferenceChanger, "junction", 
                TrackViewer.catalog.GetString("Select junction color"));
            itemColors.Junction = itemColor;

            itemColor = new ColorWithHighlights(Color.LimeGreen, 40);
            itemColor.MakeIntoUserPreference(preferenceChanger, "endnode", 
                TrackViewer.catalog.GetString("Select endnode color"));
            itemColors.EndNode = itemColor;

            itemColor = new ColorWithHighlights(Color.Sienna, 40);
            itemColor.MakeIntoUserPreference(preferenceChanger, "siding", 
                TrackViewer.catalog.GetString("Select siding color"));
            itemColors.Siding = itemColor;

            itemColor = new ColorWithHighlights(Color.Gray, 40);
            itemColor.MakeIntoUserPreference(preferenceChanger, "crossing", 
                TrackViewer.catalog.GetString("Select crossing color"));
            itemColors.Crossing = itemColor;

            itemColor = new ColorWithHighlights(Color.DarkGray, 40);
            itemColor.MakeIntoUserPreference(preferenceChanger, "roadcrossing", 
                TrackViewer.catalog.GetString("Select road crossing color"));
            itemColors.RoadCrossing = itemColor;

            itemColor = new ColorWithHighlights(Color.Purple, 40);
            itemColor.MakeIntoUserPreference(preferenceChanger, "speedpost", 
                TrackViewer.catalog.GetString("Select speedpost color"));
            itemColors.Speedpost = itemColor;

            itemColor = new ColorWithHighlights(Color.Blue, 40);
            itemColors.CandidateNode = itemColor;

            itemColor = new ColorWithHighlights(Color.Purple, 40);
            itemColors.ActiveNode = itemColor;

            itemColor = new ColorWithHighlights(Color.LightBlue, 40);
            itemColors.ClearWindowInset = itemColor;

            colorsNormal.TrackItemColors = itemColors;
            colorsHighlight.TrackItemColors = itemColors;
            colorsHotlight.TrackItemColors = itemColors;
            colorsPathMain.TrackItemColors = itemColors;
            colorsPathSiding.TrackItemColors = itemColors;
            colorsRoads.TrackItemColors = itemColors;
            colorsRoadsHighlight.TrackItemColors = itemColors;
            colorsRoadsHotlight.TrackItemColors = itemColors;

        }

        /// <summary>
        /// Set the track coloring depening on user choise
        /// </summary>
        /// <param name="doColoring">Boolean describing whether tracks will be colored or not (i.e. using a flat color)</param>
        /// <param name="doTiles">Boolean describing whether tiles will be shown or not</param>
        /// <param name="showTerrain">Boolean describing whether terrain is shown</param>
        public static void SetColoursFromOptions(bool doColoring, bool doTiles, bool showTerrain)
        {
            if (doTiles)
            {
                colorsNormal.BackgroundColors = backgroundWithTilesGroup;
            }
            else
            {
                colorsNormal.BackgroundColors = backgroundWithoutTilesGroup;
            }

            if (doColoring)
            {
                colorsNormal.TrackColors = trackGroupColoured;
                colorsHighlight.TrackColors = trackGroupColoured;
                colorsHotlight.TrackColors = trackGroupColoured;

                colorsRoads.TrackColors = roadTrackGroupColoured;
                colorsRoadsHighlight.TrackColors = roadTrackGroupColoured;
                colorsRoadsHotlight.TrackColors = roadTrackGroupColoured;
            }
            else if (showTerrain)
            {
                colorsNormal.TrackColors = trackGroupTerrain;
                colorsHighlight.TrackColors = trackGroupTerrain;
                colorsHotlight.TrackColors = trackGroupTerrain;

                colorsRoads.TrackColors = roadTrackGroupTerrain;
                colorsRoadsHighlight.TrackColors = roadTrackGroupTerrain;
                colorsRoadsHotlight.TrackColors = roadTrackGroupTerrain;
            }
            else
            {
                colorsNormal.TrackColors = trackGroupFlat;
                colorsHighlight.TrackColors = trackGroupFlat;
                colorsHotlight.TrackColors = trackGroupFlat;

                colorsRoads.TrackColors = roadTrackGroupFlat;
                colorsRoadsHighlight.TrackColors = roadTrackGroupFlat;
                colorsRoadsHotlight.TrackColors = roadTrackGroupFlat;
            }
        }

        /// <summary>
        /// Set the colors that (can) appear multiple times, each time with another shade.
        /// </summary>
        static void SetShadedColors(IPreferenceChanger preferenceChanger)
        {
            otherPathsReferenceColor = new ColorWithHighlights(Color.Fuchsia, 0);
            otherPathsReferenceColor.MakeIntoUserPreference(preferenceChanger, "otherPaths",
                TrackViewer.catalog.GetString("Select other-paths color"));
        }

        public static ColorScheme ShadeColor(ColorWithHighlights referenceColor, int index, int count)
        {
            ColorWithHighlights trackColor = ColorWithHighlights.ReshadeColorWithHighlights(referenceColor, index, count);

            ColorsGroupTrack colorsGroup = new ColorsGroupTrack
            {
                TrackStraight = trackColor,
                TrackCurved = trackColor,
                BrokenNode = trackColor,
                BrokenPath = trackColor
            };

            ColorScheme shadedColorScheme = new ColorScheme
            {
                TrackColors = colorsGroup
            };

            return shadedColorScheme;
        }
    }
    #endregion

    #region ColorGroup classes
    class ColorsGroupBasic {
        public ColorWithHighlights Junction { get; set; }
        public ColorWithHighlights EndNode { get; set; }
        public ColorWithHighlights Crossing { get; set; }
        public ColorWithHighlights RoadCrossing { get; set; }
        public ColorWithHighlights Speedpost { get; set; }
        public ColorWithHighlights Siding { get; set; }

        public ColorWithHighlights Text { get; set; }
        public ColorWithHighlights ClearWindowInset { get; set; }

        public ColorWithHighlights ActiveNode { get; set; }
        public ColorWithHighlights CandidateNode { get; set; }
    }

    class ColorsGroupBackground
    {
        public ColorWithHighlights Tile { get; set; }
        public ColorWithHighlights ClearWindow { get; set; }
    }

    class ColorsGroupTrack
    {
        public ColorWithHighlights BrokenPath { get; set; }
        public ColorWithHighlights BrokenNode { get; set; }
        public ColorWithHighlights TrackStraight { get; set; }
        public ColorWithHighlights TrackCurved { get; set; }
    }
    #endregion

    #region class ColorScheme
    /// <summary>
    /// Class to store colors used for drawing tracks etc. 
    /// Exists to facilitate drawing the same thing multiple times but in different colors, like 
    /// highlight colors, path colors, ...
    /// </summary>
    class ColorScheme
    {
        public ColorsGroupBasic TrackItemColors {get; set;}
        public ColorsGroupBackground BackgroundColors { get; set; }
        public ColorsGroupTrack TrackColors { get; set; }
        
        public Color Junction { get { return TrackItemColors.Junction.Colors[highlightType]; } }
        public Color EndNode { get { return TrackItemColors.EndNode.Colors[highlightType]; } }
        public Color Crossing { get { return TrackItemColors.Crossing.Colors[highlightType]; } }
        public Color RoadCrossing { get { return TrackItemColors.RoadCrossing.Colors[highlightType]; } }
        public Color Speedpost { get { return TrackItemColors.Speedpost.Colors[highlightType]; } }
        public Color Siding { get { return TrackItemColors.Siding.Colors[highlightType]; } }

        public Color ActiveNode { get { return TrackItemColors.ActiveNode.Colors[highlightType]; } }
        public Color CandidateNode { get { return TrackItemColors.CandidateNode.Colors[highlightType]; } }

        public Color Text { get { return TrackItemColors.Text.Colors[highlightType]; } }
        public Color ClearWindowInset { get { return TrackItemColors.ClearWindowInset.Colors[highlightType]; } }

        public Color TrackStraight { get { return TrackColors.TrackStraight.Colors[highlightType]; } }
        public Color TrackCurved { get { return TrackColors.TrackCurved.Colors[highlightType]; } }
        public Color BrokenPath { get { return TrackColors.BrokenPath.Colors[highlightType]; } }
        public Color BrokenNode { get { return TrackColors.BrokenNode.Colors[highlightType]; } }
        
        public Color ClearWindow { get { return BackgroundColors.ClearWindow.Colors[highlightType]; } }
        public Color Tile { get { return BackgroundColors.Tile.Colors[highlightType]; } }

        public Color None { get { return Color.White; } }       
        
        private static Dictionary<HighlightType, string> nameExtensions = new Dictionary<HighlightType, string>
        {
            {HighlightType.Normal, ""},
            {HighlightType.Highlight, "Highlight"},
            {HighlightType.Hotlight, "Hotlight"}
        };

        /// <summary>
        /// An extension in string format that can be used to distinguish texture names
        /// </summary>
        public string NameExtension { get { return nameExtensions[highlightType]; } }

        /// <summary>
        /// Store whether this is a normal or highlight variant
        /// </summary>
        private HighlightType highlightType;

        /// <summary>
        /// Constructor
        /// </summary>
        public ColorScheme()
        {
            highlightType = HighlightType.Normal;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type">The highligh type for this colorscheme</param>
        public ColorScheme(HighlightType type)
        {
            highlightType = type;
        }
    }

    #endregion

    #region class ColorWithHighlights
    enum HighlightType
    {
        Normal,
        Highlight,
        Hotlight,
    }
    
    /// <summary>
    /// Class to store not only a color but also its highlighted variants
    /// </summary>
    class ColorWithHighlights
    {
        /// <summary>
        /// The current normal, highlight and hotlight colors.
        /// </summary>
        public IDictionary<HighlightType,Color> Colors { get; private set; }
        
        //Some things we store to be used when color is changed using preference.
        private byte highlightDelta;
        private Color defaultColor;
        private String defaultColorName;

        const string defaultOptionExtension = " (default)";

        // map names of colors to actual color objects.
        static readonly Dictionary<string, Color> namedColors =
                typeof(Color).GetProperties()
                     .Where(prop => prop.PropertyType == typeof(Color))
                     .ToDictionary(prop => prop.Name,
                                   prop => (Color)prop.GetValue(null, null));

        /// <summary>
        /// Common part of constructor
        /// </summary>
        private ColorWithHighlights()
        {
            Colors = new Dictionary<HighlightType, Color>();
        }

        /// <summary>
        /// Constructor using a normal color and the amount of high light 
        /// </summary>
        /// <param name="normalColor">The normal color</param>
        /// <param name="givenHighlightDelta">Delta between normal and highlight color, in R, G, and B</param>
        public ColorWithHighlights(Color normalColor, byte givenHighlightDelta)
            : this()
        {
            ChangeColors(normalColor, givenHighlightDelta);
            defaultColor = normalColor;
        }

        /// <summary>
        /// Constructor using using three different colors
        /// </summary>
        /// <param name="normalColor">Color for normal</param>
        /// <param name="highlightColor">Color for highlights</param>
        /// <param name="hotlightColor">Color for hot lights</param>
        public ColorWithHighlights(Color normalColor, Color highlightColor, Color hotlightColor)
            : this()
        {
            ChangeColors(normalColor, highlightColor, hotlightColor);
            defaultColor = normalColor;
        }

        /// <summary>
        /// Change/Set the colors, using a normal color and the amount of high light 
        /// </summary>
        /// <param name="normalColor">The normal color</param>
        /// <param name="givenHighlightDelta">Delta between normal and highlight color, in R, G, and B</param>
        public void ChangeColors(Color normalColor, byte givenHighlightDelta)
        {
            ChangeColors(normalColor,
                         Highlighted(normalColor, givenHighlightDelta),
                         Highlighted(normalColor, (byte)(2 * givenHighlightDelta)));
            highlightDelta = givenHighlightDelta;
        }

        /// <summary>
        /// Change/Set the colors, using three different colors
        /// </summary>
        /// <param name="normalColor">Color for normal</param>
        /// <param name="highlightColor">Color for highlights</param>
        /// <param name="hotlightColor">Color for hot lights</param>
        public void ChangeColors(Color normalColor, Color highlightColor, Color hotlightColor)
        {
            Colors[HighlightType.Normal] = normalColor;
            Colors[HighlightType.Highlight] = highlightColor;
            Colors[HighlightType.Hotlight] = hotlightColor;
        }

                /// <summary>
        /// Make this color with highlights changable via some preference changing mechanism
        /// </summary>
        /// <param name="preferenceChanger">The object that can change a preference</param>
        /// <param name="name">name of the preference (for coding)</param>
        /// <param name="description">Description of the preference, as given to the user</param>
        public void MakeIntoUserPreference(IPreferenceChanger preferenceChanger, string name, string description)
        {
            MakeIntoUserPreference(preferenceChanger, name, description, false);
        }
        /// <summary>
        /// Make this color with highlights changable via some preference changing mechanism
        /// </summary>
        /// <param name="preferenceChanger">The object that can change a preference</param>
        /// <param name="name">name of the preference (for coding)</param>
        /// <param name="description">Description of the preference, as given to the user</param>
        /// <param name="normalOnly">if set only the normal color (non-highlighted) is changeable using the preference</param>
        public void MakeIntoUserPreference(IPreferenceChanger preferenceChanger, string name, string description, bool normalOnly)
        {
            List<string> colorOptions = namedColors.Keys.ToList();
            defaultColorName = FindColorName(defaultColor);
            string defaultColorOption = defaultColorName + defaultOptionExtension;
            colorOptions.Insert(0, defaultColorOption);

            var callBack = normalOnly ? new StringPreferenceDelegate(PreferenceChangedCallbackNormalOnly): new StringPreferenceDelegate(PreferenceChangedCallback);
            preferenceChanger.AddStringPreference(name, description, colorOptions.ToArray(), defaultColorOption, callBack);
        }

        /// <summary>
        /// This is the callback that will be called when a new preference has been set, for which only the normal color needs to be changed
        /// </summary>
        /// <param name="selectedColorName">The name of the color that has been selected</param>
        private void PreferenceChangedCallback(string selectedColorName)
        {
            if (selectedColorName == defaultColorName + defaultOptionExtension)
            {
                selectedColorName = defaultColorName;
            }
            if (namedColors.ContainsKey(selectedColorName))
            {
                ChangeColors(namedColors[selectedColorName], highlightDelta);
            }
        }

        /// <summary>
        /// This is the callback that will be called when a new preference has been set.
        /// </summary>
        /// <param name="selectedColorName">The name of the color that has been selected</param>
        private void PreferenceChangedCallbackNormalOnly(string selectedColorName)
        {
            if (selectedColorName == defaultColorName + defaultOptionExtension)
            {
                selectedColorName = defaultColorName;
            }
            if (namedColors.ContainsKey(selectedColorName))
            {
                ChangeColors(namedColors[selectedColorName], this.Colors[HighlightType.Highlight], this.Colors[HighlightType.Hotlight]);
            }
        }

        /// <summary>
        /// Find the name of a given color.
        /// Note, it is possible that a color is known under two different names. We will get only one.
        /// </summary>
        /// <param name="color">The color for which you want the name</param>
        private static string FindColorName(Color color)
        {           
            //
            foreach (KeyValuePair<string, Color> entry in namedColors)
            {
                if (entry.Value == color)
                {
                    return entry.Key;
                }
            }
            throw new ArgumentException("Color is not found, which means namedColors is not initialized correctly");
        }

        /// <summary>
        /// Make a highlight variant of the color (making it more white).
        /// </summary>
        /// <param name="color">reference color</param>
        /// <param name="offset">value that is added to each color channel</param>
        /// <returns>Scaled color</returns>
        public static Color Highlighted(Color color, byte offset)
        {
            Color newColor = new Color
            {
                A = color.A
            };
            byte effectiveOffset = (byte)((color.A > 128) ? offset : 0);
            newColor.B = (byte)Math.Min(color.B + effectiveOffset, 255);
            newColor.R = (byte)Math.Min(color.R + effectiveOffset, 255);
            newColor.G = (byte)Math.Min(color.G + effectiveOffset, 255);
            return newColor;
            //return new Color(color.ToVector4() * scale);
        }

        public static ColorWithHighlights ReshadeColorWithHighlights(ColorWithHighlights original, int index, int count)
        {
            Color reshadedNominal = ReshadedColor(original.Colors[HighlightType.Normal], index, count);
            return new ColorWithHighlights(reshadedNominal, 0);
        }

        /// <summary>
        /// Change the complete color to be part of a set of shaded colors
        /// </summary>
        /// <param name="color">Original non-shaded color</param>
        /// <param name="index">Index describing which of the colors we need to make in a spectrum</param>
        /// <param name="count">Total amount of colors in the spectrion</param>
        /// <returns>The changed-color</returns>
        public static Color ReshadedColor(Color color, int index, int count)
        {
            Color newColor = new Color
            {
                A = color.A,

                R = ReshadeSingleChannel(color.R, index, count),
                G = ReshadeSingleChannel(color.G, index, count),
                B = ReshadeSingleChannel(color.B, index, count)
            };

            return newColor;
        }

        /// <summary>
        /// Return a changed single channel of a color to be part of a set of shaded colors
        /// </summary>
        /// <param name="original">Original non-shaded color</param>
        /// <param name="index">Index describing which of the colors we need to make in a spectrum</param>
        /// <param name="count">Total amount of colors in the spectrion</param>
        static byte ReshadeSingleChannel(byte original, int index, int count)
        {
            float newvalue;
            if (original >= 128)
            {
                newvalue = original * (count + index + 1) / (2 * count);
            }
            else
            {
                newvalue = 255 - ((255-original) * (count + index + 1) / (2 * count));
            }
            return (byte)newvalue;
        }

    }
    #endregion

}
