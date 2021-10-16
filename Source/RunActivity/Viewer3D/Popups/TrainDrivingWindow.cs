// COPYRIGHT 2019, 2020 by the Open Rails project.
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

using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems.Brakes;
using ORTS.Common;
using ORTS.Common.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Orts.Viewer3D.Popups
{
    public class TrainDrivingWindow : Window
    {
        /// <summary>
        /// A Train Driving row with data fields.
        /// </summary>
        public struct ListLabel
        {
            public string FirstCol;
            public int FirstColWidth;
            public string LastCol;
            public int LastColWidth;
            public string SymbolCol;
            public bool ChangeColWidth;
            public string KeyPressed;
        }
        public List<ListLabel> labels = new List<ListLabel>();

        /// <summary>
        /// Table of Colors to client-side color codes.
        /// </summary>
        /// <remarks>
        /// Compare codes with index.css.
        /// </remarks>
        private static readonly Dictionary<Color, string> ColorCode = new Dictionary<Color, string>
        {
            { Color.Yellow, "???" },
            { Color.Green, "??!" },
            { Color.Black, "?!?" },
            { Color.PaleGreen, "?!!" },
            { Color.White, "!??" },
            { Color.Orange, "!!?" },
            { Color.OrangeRed, "!!!" },
            { Color.Cyan, "%%%" },
            { Color.Brown, "%$$" },
            { Color.LightGreen, "%%$" },
            { Color.Blue, "$%$" },
            { Color.LightSkyBlue, "$$$" },
        };

        /// <summary>
        /// Table of Colors to control layout color codes.
        /// </summary>
        /// <remarks>
        /// Compare codes with index.css.
        /// </remarks>
        private static readonly Dictionary<string, Color> ColorCodeCtrl = new Dictionary<string, Color>
        {
            { "???", Color.Yellow },
            { "??!", Color.Green },
            { "?!?", Color.Black },
            { "?!!", Color.PaleGreen },
            { "!??", Color.White },
            { "!!?", Color.Orange },
            { "!!!", Color.OrangeRed },
            { "%%%", Color.Cyan },
            { "%$$", Color.Brown },
            { "%%$", Color.LightGreen },
            { "$%$", Color.Blue },
            { "$$$", Color.LightSkyBlue },
        };

        private static class Symbols
        {
            public const string ArrowUp = "▲";
            public const string SmallArrowUp = "△";
            public const string ArrowDown = "▼";
            public const string SmallArrowDown = "▽";
            public const string End = "▬";
            public const string EndLower = "▖";
            public const string ArrowToRight = "►";
            public const string SmallDiamond = "●";
            public const string GradientDown = "\u2198";
            public const string GradientUp = "\u2197";
        }

        private static readonly Dictionary<string, string> FirstColToAbbreviated = new Dictionary<string, string>()
        {
            [Viewer.Catalog.GetString("AI Fireman")] = Viewer.Catalog.GetString("AIFR"),
            [Viewer.Catalog.GetString("Autopilot")] = Viewer.Catalog.GetString("AUTO"),
            [Viewer.Catalog.GetString("Battery switch")] = Viewer.Catalog.GetString("BATT"),
            [Viewer.Catalog.GetString("Boiler pressure")] = Viewer.Catalog.GetString("PRES"),
            [Viewer.Catalog.GetString("Boiler water glass")] = Viewer.Catalog.GetString("WATR"),
            [Viewer.Catalog.GetString("Boiler water level")] = Viewer.Catalog.GetString("LEVL"),
            [Viewer.Catalog.GetString("Circuit breaker")] = Viewer.Catalog.GetString("CIRC"),
            [Viewer.Catalog.GetString("Cylinder cocks")] = Viewer.Catalog.GetString("CCOK"),
            [Viewer.Catalog.GetString("Direction")] = Viewer.Catalog.GetString("DIRC"),
            [Viewer.Catalog.GetString("DerailCoeff")] = Viewer.Catalog.GetString("DRLC"),
            [Viewer.Catalog.GetString("Doors open")] = Viewer.Catalog.GetString("DOOR"),
            [Viewer.Catalog.GetString("Dynamic brake")] = Viewer.Catalog.GetString("BDYN"),
            [Viewer.Catalog.GetString("Electric train supply")] = Viewer.Catalog.GetString("TSUP"),
            [Viewer.Catalog.GetString("Engine brake")] = Viewer.Catalog.GetString("BLOC"),
            [Viewer.Catalog.GetString("Engine")] = Viewer.Catalog.GetString("ENGN"),
            [Viewer.Catalog.GetString("Fire mass")] = Viewer.Catalog.GetString("FIRE"),
            [Viewer.Catalog.GetString("Fixed gear")] = Viewer.Catalog.GetString("GEAR"),
            [Viewer.Catalog.GetString("Fuel levels")] = Viewer.Catalog.GetString("FUEL"),
            [Viewer.Catalog.GetString("Gear")] = Viewer.Catalog.GetString("GEAR"),
            [Viewer.Catalog.GetString("Gradient")] = Viewer.Catalog.GetString("GRAD"),
            [Viewer.Catalog.GetString("Grate limit")] = Viewer.Catalog.GetString("GRAT"),
            [Viewer.Catalog.GetString("Master key")] = Viewer.Catalog.GetString("MAST"),
            [Viewer.Catalog.GetString("Pantographs")] = Viewer.Catalog.GetString("PANT"),
            [Viewer.Catalog.GetString("Power")] = Viewer.Catalog.GetString("POWR"),
            [Viewer.Catalog.GetString("Regulator")] = Viewer.Catalog.GetString("REGL"),
            [Viewer.Catalog.GetString("Replay")] = Viewer.Catalog.GetString("RPLY"),
            [Viewer.Catalog.GetString("Retainers")] = Viewer.Catalog.GetString("RETN"),
            [Viewer.Catalog.GetString("Reverser")] = Viewer.Catalog.GetString("REVR"),
            [Viewer.Catalog.GetString("Sander")] = Viewer.Catalog.GetString("SAND"),
            [Viewer.Catalog.GetString("Speed")] = Viewer.Catalog.GetString("SPED"),
            [Viewer.Catalog.GetString("Steam usage")] = Viewer.Catalog.GetString("STEM"),
            [Viewer.Catalog.GetString("Throttle")] = Viewer.Catalog.GetString("THRO"),
            [Viewer.Catalog.GetString("Time")] = Viewer.Catalog.GetString("TIME"),
            [Viewer.Catalog.GetString("Traction cut-off relay")] = Viewer.Catalog.GetString("TRAC"),
            [Viewer.Catalog.GetString("Train brake")] = Viewer.Catalog.GetString("BTRN"),
            [Viewer.Catalog.GetString("Wheel")] = Viewer.Catalog.GetString("WHEL")
        };

        private static readonly Dictionary<string, string> LastColToAbbreviated = new Dictionary<string, string>()
        {
            [Viewer.Catalog.GetString("(absolute)")] = Viewer.Catalog.GetString("(Abs.)"),
            [Viewer.Catalog.GetString("apply Service")] = Viewer.Catalog.GetString("Apply"),
            [Viewer.Catalog.GetString("Apply Quick")] = Viewer.Catalog.GetString("ApplQ"),
            [Viewer.Catalog.GetString("Apply Slow")] = Viewer.Catalog.GetString("ApplS"),
            [Viewer.Catalog.GetString("coal")] = Viewer.Catalog.GetString("c"),
            [Viewer.Catalog.GetString("Emergency Braking Push Button")] = Viewer.Catalog.GetString("EmerBPB"),
            [Viewer.Catalog.GetString("Lap Self")] = Viewer.Catalog.GetString("LapS"),
            [Viewer.Catalog.GetString("Minimum Reduction")] = Viewer.Catalog.GetString("MRedc"),
            [Viewer.Catalog.GetString("(safe range)")] = Viewer.Catalog.GetString("(safe)"),
            [Viewer.Catalog.GetString("skid")] = Viewer.Catalog.GetString("Skid"),
            [Viewer.Catalog.GetString("slip warning")] = Viewer.Catalog.GetString("Warning"),
            [Viewer.Catalog.GetString("slip")] = Viewer.Catalog.GetString("Slip"),
            [Viewer.Catalog.GetString("water")] = Viewer.Catalog.GetString("w")
        };

        bool ctrlAIFiremanOn = false; //AIFireman On
        bool ctrlAIFiremanOff = false;//AIFireman Off
        bool ctrlAIFiremanReset = false;//AIFireman Reset
        double clockAIFireTime; //AIFireman reset timing

        bool grateLabelVisible = false;// Grate label visible
        double clockGrateTime; // Grate hide timing

        bool wheelLabelVisible = false;// Wheel label visible
        double clockWheelTime; // Wheel hide timing

        bool derailLabelVisible = false;// DerailCoeff label visible
        double clockDerailTime; //  DerailCoeff label visible

        bool doorsLabelVisible = false; // Doors label visible
        double clockDoorsTime; // Doors hide timing

        bool ResizeWindow = false;
        bool UpdateDataEnded = false;
        const int TextSize = 15;
        int FirstColLenght = 0;
        int FirstColOverFlow = 0;
        int LastColLenght = 0;
        int LastColOverFlow = 0;
        int LinesCount = 0;
        int maxFirstColWidth = 0;
        int maxLastColWidth = 0;
        int WindowHeightMin = 0;
        int WindowHeightMax = 0;
        int WindowWidthMin = 0;
        int WindowWidthMax = 0;

        Label indicator;
        LabelMono indicatorMono;
        Label ExpandWindow;
        Label LabelFontToBold;

        public bool normalTextMode = true;// Standard text
        public bool TrainDrivingUpdating = false;
        public static bool MonoFont;
        public static bool FontChanged;
        public static bool FontToBold;
        string keyPressed;// display a symbol when a control key is pressed.

        public TrainDrivingWindow(WindowManager owner)
                    : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 10, Window.DecorationSize.Y + owner.TextFontDefault.Height * 10, Viewer.Catalog.GetString("Train Driving Info"))
        {
            WindowHeightMin = Location.Height;
            WindowHeightMax = Location.Height + owner.TextFontDefault.Height * 20;
            WindowWidthMin = Location.Width;
            WindowWidthMax = Location.Width + owner.TextFontDefault.Height * 20;
        }

        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            outf.Write(normalTextMode);
            outf.Write(Location.X);
            outf.Write(Location.Y);
            outf.Write(Location.Width);
            outf.Write(Location.Height);
            outf.Write(clockAIFireTime);
            outf.Write(ctrlAIFiremanOn);
            outf.Write(ctrlAIFiremanOff);
            outf.Write(ctrlAIFiremanReset);
            outf.Write(clockWheelTime);
            outf.Write(wheelLabelVisible);
            outf.Write(clockDerailTime);
            outf.Write(derailLabelVisible);
            outf.Write(clockDoorsTime);
            outf.Write(doorsLabelVisible);
        }

        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            Rectangle LocationRestore;
            normalTextMode = inf.ReadBoolean();
            LocationRestore.X = inf.ReadInt32();
            LocationRestore.Y = inf.ReadInt32();
            LocationRestore.Width = inf.ReadInt32();
            LocationRestore.Height = inf.ReadInt32();
            clockAIFireTime = inf.ReadDouble();
            ctrlAIFiremanOn = inf.ReadBoolean();
            ctrlAIFiremanOff = inf.ReadBoolean();
            ctrlAIFiremanReset = inf.ReadBoolean();
            clockWheelTime = inf.ReadDouble();
            wheelLabelVisible = inf.ReadBoolean();
            clockDerailTime = inf.ReadDouble();
            derailLabelVisible = inf.ReadBoolean();
            clockDoorsTime = inf.ReadDouble();
            doorsLabelVisible = inf.ReadBoolean();

            // Display window
            SizeTo(LocationRestore.Width, LocationRestore.Height);
            MoveTo(LocationRestore.X, LocationRestore.Y);
        }

        protected internal override void Initialize()
        {
            base.Initialize();
            // Reset window size
            UpdateWindowSize();
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            var vbox = base.Layout(layout).AddLayoutVertical();
            if (labels.Count > 0)
            {
                var colWidth = labels.Max(x => x.FirstColWidth) + (normalTextMode? 15: 20);
                var TimeHboxPositionY = 0;
                foreach (var data in labels.ToList())
                {
                    if (data.FirstCol.Contains("NwLn"))
                    {
                        var hbox = vbox.AddLayoutHorizontalLineOfText();
                        hbox.Add(new Label(colWidth * 2, hbox.RemainingHeight, " "));
                    }
                    else if (data.FirstCol.Contains("Sprtr"))
                    {
                        vbox.AddHorizontalSeparator();
                    }
                    else
                    {
                        var hbox = vbox.AddLayoutHorizontalLineOfText();
                        var FirstCol = data.FirstCol;
                        var LastCol = data.LastCol;
                        var symbolCol = data.SymbolCol;

                        if (ColorCodeCtrl.Keys.Any(FirstCol.EndsWith) || ColorCodeCtrl.Keys.Any(LastCol.EndsWith) || ColorCodeCtrl.Keys.Any(data.KeyPressed.EndsWith) || ColorCodeCtrl.Keys.Any(data.SymbolCol.EndsWith))
                        {
                            var colorFirstColEndsWith = ColorCodeCtrl.Keys.Any(FirstCol.EndsWith) ? ColorCodeCtrl[FirstCol.Substring(FirstCol.Length - 3)] : Color.White;
                            var colorLastColEndsWith = ColorCodeCtrl.Keys.Any(LastCol.EndsWith) ? ColorCodeCtrl[LastCol.Substring(LastCol.Length - 3)] : Color.White;
                            var colorKeyPressed = ColorCodeCtrl.Keys.Any(data.KeyPressed.EndsWith) ? ColorCodeCtrl[data.KeyPressed.Substring(data.KeyPressed.Length - 3)] : Color.White;
                            var colorSymbolCol = ColorCodeCtrl.Keys.Any(data.SymbolCol.EndsWith) ? ColorCodeCtrl[data.SymbolCol.Substring(data.SymbolCol.Length - 3)] : Color.White;

                            // Erase the color code at the string end
                            FirstCol = ColorCodeCtrl.Keys.Any(FirstCol.EndsWith) ? FirstCol.Substring(0, FirstCol.Length - 3) : FirstCol;
                            LastCol = ColorCodeCtrl.Keys.Any(LastCol.EndsWith) ? LastCol.Substring(0, LastCol.Length - 3) : LastCol;
                            keyPressed = ColorCodeCtrl.Keys.Any(data.KeyPressed.EndsWith) ? data.KeyPressed.Substring(0, data.KeyPressed.Length - 3) : data.KeyPressed;
                            symbolCol = ColorCodeCtrl.Keys.Any(data.SymbolCol.EndsWith) ? data.SymbolCol.Substring(0, data.SymbolCol.Length - 3) : data.SymbolCol;

                            // Apply color to FirstCol
                            if (normalTextMode)
                            {   // Apply color to FirstCol
                                hbox.Add(indicator = new Label(TextSize, hbox.RemainingHeight, keyPressed, LabelAlignment.Center));
                                indicator.Color = colorKeyPressed;
                                hbox.Add(indicator = new Label(colWidth, hbox.RemainingHeight, FirstCol));
                                indicator.Color = colorFirstColEndsWith;
                            }
                            else
                            {   // Use constant width font
                                hbox.Add(indicator = new Label(TextSize, hbox.RemainingHeight, keyPressed, LabelAlignment.Center));
                                indicator.Color = colorKeyPressed;
                                hbox.Add(indicatorMono = new LabelMono(colWidth, hbox.RemainingHeight, FirstCol));
                                indicatorMono.Color = colorFirstColEndsWith;
                            }

                            if (data.KeyPressed != null && data.KeyPressed != "")
                            {
                                hbox.Add(indicator = new Label(-TextSize, 0, TextSize, hbox.RemainingHeight, keyPressed, LabelAlignment.Right));
                                indicator.Color = colorKeyPressed;
                            }

                            if (data.SymbolCol != null && data.SymbolCol != "")
                            {
                                hbox.Add(indicator = new Label(-(TextSize + 3), 0, TextSize, hbox.RemainingHeight, symbolCol, LabelAlignment.Right));
                                indicator.Color = colorSymbolCol;
                            }

                            // Apply color to LastCol
                            hbox.Add(indicator = new Label(colWidth, hbox.RemainingHeight, LastCol));
                            indicator.Color = colorFirstColEndsWith == Color.White ? colorLastColEndsWith : colorFirstColEndsWith;
                        }
                        else
                        {   // blanck space
                            var keyPressed = "";
                            hbox.Add(indicator = new Label(TextSize, hbox.RemainingHeight, keyPressed, LabelAlignment.Center));
                            indicator.Color = Color.White; // Default color

                            //Avoids troubles when the Main Scale (Windows DPI settings) is not set to 100%
                            if (LastCol.Contains(':')) TimeHboxPositionY = hbox.Position.Y;

                            if (normalTextMode)
                            {
                                hbox.Add(indicator = new Label(colWidth, hbox.RemainingHeight, FirstCol));
                                indicator.Color = Color.White; // Default color
                            }
                            else
                            {
                                hbox.Add(indicatorMono = new LabelMono(colWidth, hbox.RemainingHeight, FirstCol));
                                indicatorMono.Color = Color.White; // Default color
                            }

                            // Font to bold, clickable label
                            if (hbox.Position.Y == TimeHboxPositionY && LastCol.Contains(':')) // Time line.
                            {
                                hbox.Add(LabelFontToBold = new Label(Owner.TextFontDefault.MeasureString(LastCol) - (normalTextMode ? 5 : 3), hbox.RemainingHeight, LastCol));
                                LabelFontToBold.Color = Color.White;
                                LabelFontToBold.Click += new Action<Control, Point>(FontToBold_Click);
                            }
                            else
                            {
                                hbox.Add(indicator = new Label(colWidth, hbox.RemainingHeight, LastCol));
                                indicator.Color = Color.White; // Default color
                            }
                        }

                        // Clickable symbol
                        if (hbox.Position.Y == TimeHboxPositionY)
                        {
                            var expandWindow = normalTextMode ? '\u25C4' : '\u25BA';// ◀ : ▶
                            hbox.Add(ExpandWindow = new Label(hbox.RemainingWidth - TextSize, 0, TextSize, hbox.RemainingHeight, expandWindow.ToString(), LabelAlignment.Right));
                            ExpandWindow.Color = Color.Yellow;
                            ExpandWindow.Click += new Action<Control, Point>(ExpandWindow_Click);
                        }
                        // Separator line
                        if (data.FirstCol.Contains("Sprtr"))
                        {
                            hbox.AddHorizontalSeparator();
                        }
                    }
                }
            }
            return vbox;
        }

        void FontToBold_Click(Control arg1, Point arg2)
        {
            FontChanged = true;
            FontToBold = !FontToBold;
            UpdateWindowSize();
        }

        void ExpandWindow_Click(Control arg1, Point arg2)
        {
            normalTextMode = !normalTextMode;
            UpdateWindowSize();
        }

        public override void TabAction() => CycleMode();

        /// <summary>
        /// Change between full and abbreviated text mode.
        /// </summary>
        public void CycleMode()
        {
            normalTextMode = !normalTextMode;
            UpdateWindowSize();
        }

        private void UpdateWindowSize()
        {
            labels = TrainDrivingWindowList(Owner.Viewer, normalTextMode).ToList();
            ModifyWindowSize();
        }

        /// <summary>
        /// Modify window size
        /// </summary>
        private void ModifyWindowSize()
        {
            if (labels.Count > 0)
            {
                var textwidth = Owner.TextFontDefault.Height;
                FirstColLenght = labels.Max(x => x.FirstColWidth);
                LastColLenght = labels.Max(x => x.LastColWidth);

                // Validates rows with windows DPI settings
                var dpiOffset = (System.Drawing.Graphics.FromHwnd(IntPtr.Zero).DpiY / 96) > 1.00f ? 1 : 0;// values from testing
                var rowCount = labels.Where(x => !string.IsNullOrWhiteSpace(x.FirstCol.ToString()) || !string.IsNullOrWhiteSpace(x.LastCol.ToString())).Count() - dpiOffset;
                var desiredHeight = FontToBold ? Owner.TextFontDefaultBold.Height * rowCount
                    : Owner.TextFontDefault.Height * rowCount;

                var desiredWidth = FirstColLenght + LastColLenght + 45;// interval between firstcol and lastcol

                var newHeight = (int)MathHelper.Clamp(desiredHeight, (normalTextMode ? WindowHeightMin : 100), WindowHeightMax);
                var newWidth = (int)MathHelper.Clamp(desiredWidth, (normalTextMode ? WindowWidthMin : 100), WindowWidthMax);

                // Move the dialog up if we're expanding it, or down if not; this keeps the center in the same place.
                var newTop = Location.Y + (Location.Height - newHeight) / 2;

                // Display window
                SizeTo(newWidth, newHeight);
                MoveTo(Location.X, newTop);
            }
        }

        /// <summary>
        /// Sanitize the fields of a <see cref="ListLabel"/> in-place.
        /// </summary>
        /// <param name="label">A reference to the <see cref="ListLabel"/> to check.</param>
        private void CheckLabel(ref ListLabel label, bool normalMode)
        {
            void CheckString(ref string s) => s = s ?? "";
            CheckString(ref label.FirstCol);
            CheckString(ref label.LastCol);
            CheckString(ref label.SymbolCol);
            CheckString(ref label.KeyPressed);

            UpdateColsWidth(label, normalMode);
        }

        private void UpdateColsWidth(ListLabel label, bool normalmode)
        {
            if (!UpdateDataEnded)
            {
                if (!normalTextMode)
                {
                    foreach (KeyValuePair<string, string> mapping in FirstColToAbbreviated)
                        label.FirstCol = label.FirstCol.Replace(mapping.Key, mapping.Value);
                    foreach (KeyValuePair<string, string> mapping in LastColToAbbreviated)
                        label.LastCol = label.LastCol.Replace(mapping.Key, mapping.Value);
                }
                var firstColWidth = 0;
                var lastColWidth = 0;
                var firstCol = label.FirstCol;
                var lastCol = label.LastCol;
                var symbolCol = label.SymbolCol;
                var keyPressed = label.KeyPressed;
                var changeColwidth = label.ChangeColWidth;

                if (!firstCol.Contains("Sprtr"))
                {
                    if (ColorCodeCtrl.Keys.Any(firstCol.EndsWith))
                    {
                        var tempFirstCol = firstCol.Substring(0, firstCol.Length - 3);
                        firstColWidth = FontToBold ? Owner.TextFontDefaultBold.MeasureString(tempFirstCol.TrimEnd())
                            : !normalTextMode? Owner.TextFontMonoSpacedBold.MeasureString(tempFirstCol.TrimEnd())
                            : Owner.TextFontDefault.MeasureString(tempFirstCol.TrimEnd());
                    }
                    else
                    {
                        firstColWidth = FontToBold ? Owner.TextFontDefaultBold.MeasureString(firstCol.TrimEnd())
                            : !normalTextMode ? Owner.TextFontMonoSpacedBold.MeasureString(firstCol.TrimEnd())
                            : Owner.TextFontDefault.MeasureString(firstCol.TrimEnd());
                    }

                    if (ColorCodeCtrl.Keys.Any(lastCol.EndsWith))
                    {
                        var tempLastCol = lastCol.Substring(0, lastCol.Length - 3);
                        lastColWidth = FontToBold ? Owner.TextFontDefaultBold.MeasureString(tempLastCol.TrimEnd())
                            : Owner.TextFontDefault.MeasureString(tempLastCol.TrimEnd());
                    }
                    else
                    {
                        lastColWidth = FontToBold ? Owner.TextFontDefaultBold.MeasureString(lastCol.TrimEnd())
                            : Owner.TextFontDefault.MeasureString(lastCol.TrimEnd());
                    }

                    //Set a minimum value for LastColWidth to avoid overlap between time value and clickable symbol
                    if (labels.Count == 1)
                    {
                        lastColWidth = labels.First().LastColWidth + 15;// time value + clickable symbol
                    }
                }

                labels.Add(new ListLabel
                {
                    FirstCol = firstCol,
                    FirstColWidth = firstColWidth,
                    LastCol = lastCol,
                    LastColWidth = lastColWidth,
                    SymbolCol = symbolCol,
                    ChangeColWidth = changeColwidth,
                    KeyPressed = keyPressed
                });

                //ResizeWindow, when the string spans over the right boundary of the window
                if (!ResizeWindow)
                {
                    if (maxFirstColWidth < firstColWidth) FirstColOverFlow = maxFirstColWidth;
                    if (maxLastColWidth < lastColWidth) LastColOverFlow = maxLastColWidth;
                    ResizeWindow = true;
                }
            }
            else
            {
                if (this.Visible)
                {
                    // Detect Autopilot is on to avoid flickering when slim window is displayed
                    var AutopilotOn = Owner.Viewer.PlayerLocomotive.Train.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING ? true : false;

                    //ResizeWindow, when the string spans over the right boundary of the window
                    maxFirstColWidth = labels.Max(x => x.FirstColWidth);
                    maxLastColWidth = labels.Max(x => x.LastColWidth);

                    if (!ResizeWindow & (FirstColOverFlow != maxFirstColWidth || (!AutopilotOn && LastColOverFlow != maxLastColWidth)))
                    {
                        LastColOverFlow = maxLastColWidth;
                        FirstColOverFlow = maxFirstColWidth;
                        ResizeWindow = true;
                    }
                }
            }
        }

        /// <summary>
        /// Retrieve a formatted list <see cref="ListLabel"/>s to be displayed as an in-browser Track Monitor.
        /// </summary>
        /// <param name="viewer">The Viewer to read train data from.</param>
        /// <returns>A list of <see cref="ListLabel"/>s, one per row of the popup.</returns>
        public IEnumerable<ListLabel> TrainDrivingWindowList(Viewer viewer, bool normalTextMode)
        {
            bool useMetric = viewer.MilepostUnitsMetric;
            labels = new List<ListLabel>();
            void AddLabel(ListLabel label)
            {
                CheckLabel(ref label, normalTextMode);
            }
            void AddSeparator() => AddLabel(new ListLabel
            {
                FirstCol = "Sprtr",
            });

            TrainCar trainCar = viewer.PlayerLocomotive;
            Train train = trainCar.Train;
            string trainBrakeStatus = trainCar.GetTrainBrakeStatus();
            string dynamicBrakeStatus = trainCar.GetDynamicBrakeStatus();
            string engineBrakeStatus = trainCar.GetEngineBrakeStatus();
            MSTSLocomotive locomotive = (MSTSLocomotive)trainCar;
            string locomotiveStatus = locomotive.GetStatus();
            bool combinedControlType = locomotive.CombinedControlType == MSTSLocomotive.CombinedControl.ThrottleDynamic;
            bool showMUReverser = Math.Abs(train.MUReverserPercent) != 100f;
            bool showRetainers = train.RetainerSetting != RetainerSetting.Exhaust;
            bool stretched = train.Cars.Count > 1 && train.NPull == train.Cars.Count - 1;
            bool bunched = !stretched && train.Cars.Count > 1 && train.NPush == train.Cars.Count - 1;
            Train.TrainInfo trainInfo = train.GetTrainInfo();

            labels.Clear();
            UpdateDataEnded = false;

            if (!normalTextMode)
            {
                var newBrakeStatus = new System.Text.StringBuilder(trainBrakeStatus);
                trainBrakeStatus = newBrakeStatus
                      .Replace(Viewer.Catalog.GetString("bar"), string.Empty)
                      .Replace(Viewer.Catalog.GetString("inHg"), string.Empty)
                      .Replace(Viewer.Catalog.GetString("kgf/cm²"), string.Empty)
                      .Replace(Viewer.Catalog.GetString("kPa"), string.Empty)
                      .Replace(Viewer.Catalog.GetString("psi"), string.Empty)
                      .Replace(Viewer.Catalog.GetString("lib./pal."), string.Empty)//cs locales
                      .Replace(Viewer.Catalog.GetString("pal.rtuti"), string.Empty)
                      .ToString();
            }

            // First Block
            // Client and server may have a time difference.
            AddLabel(new ListLabel
            {
                FirstCol = Viewer.Catalog.GetString("Time"),
                LastCol = FormatStrings.FormatTime(viewer.Simulator.ClockTime + (MultiPlayer.MPManager.IsClient() ? MultiPlayer.MPManager.Instance().serverTimeDifference : 0)),
            });
            if (viewer.Simulator.IsReplaying)
            {
                AddLabel(new ListLabel
                {
                    FirstCol = Viewer.Catalog.GetString("Replay"),
                    LastCol = FormatStrings.FormatTime(viewer.Log.ReplayEndsAt - viewer.Simulator.ClockTime),
                });
            }

            Color speedColor;
            if (locomotive.SpeedMpS < trainInfo.allowedSpeedMpS - 1f)
                speedColor = Color.White;
            else if (locomotive.SpeedMpS < trainInfo.allowedSpeedMpS)
                speedColor = Color.PaleGreen;
            else if (locomotive.SpeedMpS < trainInfo.allowedSpeedMpS + 5f)
                speedColor = Color.Orange;
            else
                speedColor = Color.OrangeRed;
            AddLabel(new ListLabel
            {
                FirstCol = Viewer.Catalog.GetString("Speed"),
                LastCol = $"{FormatStrings.FormatSpeedDisplay(locomotive.SpeedMpS, useMetric)}{ColorCode[speedColor]}",
            });

            // Gradient info
            if (normalTextMode)
            {
                float gradient = -trainInfo.currentElevationPercent;
                const float minSlope = 0.00015f;
                string gradientIndicator;
                if (gradient < -minSlope)
                    gradientIndicator = $"{gradient:F1}%{Symbols.GradientDown}{ColorCode[Color.LightSkyBlue]}";
                else if (gradient > minSlope)
                    gradientIndicator = $"{gradient:F1}%{Symbols.GradientUp}{ColorCode[Color.Yellow]}";
                else
                    gradientIndicator = $"{gradient:F1}%";
                AddLabel(new ListLabel
                {
                    FirstCol = Viewer.Catalog.GetString("Gradient"),
                    LastCol = gradientIndicator,
                });
            }
            // Separator
            AddSeparator();

            // Second block
            // Direction
            {
                UserCommand? reverserCommand = GetPressedKey(UserCommand.ControlBackwards, UserCommand.ControlForwards);
                string reverserKey = "";
                if (reverserCommand == UserCommand.ControlBackwards || reverserCommand == UserCommand.ControlForwards)
                {
                    bool moving = Math.Abs(trainCar.SpeedMpS) > 1;
                    bool nonSteamEnd = trainCar.EngineType != TrainCar.EngineTypes.Steam && trainCar.Direction == Direction.N && (trainCar.ThrottlePercent >= 1 || moving);
                    bool steamEnd = locomotive is MSTSSteamLocomotive steamLocomotive2 && steamLocomotive2.CutoffController.MaximumValue == Math.Abs(train.MUReverserPercent / 100);
                    if (reverserCommand != null && (nonSteamEnd || steamEnd))
                        reverserKey = Symbols.End + ColorCode[Color.Yellow];
                    else if (reverserCommand == UserCommand.ControlBackwards)
                        reverserKey = Symbols.ArrowDown + ColorCode[Color.Yellow];
                    else if (reverserCommand == UserCommand.ControlForwards)
                        reverserKey = Symbols.ArrowUp + ColorCode[Color.Yellow];
                    else
                        reverserKey = "";
                }
                string reverserIndicator = showMUReverser ? $"{Round(Math.Abs(train.MUReverserPercent))}% " : "";
                AddLabel(new ListLabel
                {
                    FirstCol = Viewer.Catalog.GetString(locomotive.EngineType == TrainCar.EngineTypes.Steam ? "Reverser" : "Direction"),
                    LastCol = $"{reverserIndicator}{FormatStrings.Catalog.GetParticularString("Reverser", GetStringAttribute.GetPrettyName(locomotive.Direction))}",
                    KeyPressed = reverserKey,
                    SymbolCol = ""//reverserKey,
                });
            }

            // Throttle
            {
                UserCommand? throttleCommand = GetPressedKey(UserCommand.ControlThrottleDecrease, UserCommand.ControlThrottleIncrease);
                string throttleKey;
                bool upperLimit = throttleCommand == UserCommand.ControlThrottleIncrease && locomotive.ThrottleController.MaximumValue == trainCar.ThrottlePercent / 100;
                bool lowerLimit = throttleCommand == UserCommand.ControlThrottleDecrease && trainCar.ThrottlePercent == 0;
                if (locomotive.DynamicBrakePercent < 1 && (upperLimit || lowerLimit))
                    throttleKey = Symbols.End + ColorCode[Color.Yellow];
                else if (locomotive.DynamicBrakePercent > -1)
                    throttleKey = Symbols.EndLower + ColorCode[Color.Yellow];
                else if (throttleCommand == UserCommand.ControlThrottleIncrease)
                    throttleKey = Symbols.ArrowUp + ColorCode[Color.Yellow];
                else if (throttleCommand == UserCommand.ControlThrottleDecrease)
                    throttleKey = Symbols.ArrowDown + ColorCode[Color.Yellow];
                else
                    throttleKey = "";

                AddLabel(new ListLabel
                {
                    FirstCol = Viewer.Catalog.GetString(locomotive is MSTSSteamLocomotive ? "Regulator" : "Throttle"),
                    LastCol = $"{Round(locomotive.ThrottlePercent)}%",
                    KeyPressed = throttleKey,
                    SymbolCol = ""//throttleKey,
                });
            }

            // Cylinder Cocks
            if (locomotive is MSTSSteamLocomotive steamLocomotive)
            {
                string cocksIndicator, cocksKey;
                if (steamLocomotive.CylinderCocksAreOpen)
                {
                    cocksIndicator = Viewer.Catalog.GetString("Open") + ColorCode[Color.Orange];
                    cocksKey = Symbols.ArrowToRight + ColorCode[Color.Yellow];
                }
                else
                {
                    cocksIndicator = Viewer.Catalog.GetString("Closed") + ColorCode[Color.White];
                    cocksKey = "";
                }
                AddLabel(new ListLabel
                {
                    FirstCol = Viewer.Catalog.GetString("Cylinder cocks"),
                    LastCol = cocksIndicator,
                    KeyPressed = cocksKey,
                    SymbolCol = ""//cocksKey,
                });
            }

            // Sander
            if (locomotive.GetSanderOn())
            {
                bool sanderBlocked = locomotive.AbsSpeedMpS > locomotive.SanderSpeedOfMpS;
                string sanderKey = Symbols.ArrowToRight + ColorCode[Color.Yellow];
                AddLabel(new ListLabel
                {
                    FirstCol = Viewer.Catalog.GetString("Sander"),
                    LastCol = sanderBlocked ? Viewer.Catalog.GetString("Blocked") + ColorCode[Color.OrangeRed] : Viewer.Catalog.GetString("On") + ColorCode[Color.Orange],
                    KeyPressed = sanderKey,
                    SymbolCol = ""//sanderKey,
                });
            }
            else
            {
                AddLabel(new ListLabel
                {
                    FirstCol = Viewer.Catalog.GetString("Sander"),
                    LastCol = Viewer.Catalog.GetString("Off"),
                    KeyPressed = "",
                    SymbolCol = "",
                });
            }

            AddSeparator();

            // Train Brake multi-lines
            // TODO: A better algorithm
            //var brakeStatus = Owner.Viewer.PlayerLocomotive.GetTrainBrakeStatus();
            //steam loco
            string brakeInfoValue = "";
            int index = 0;

            if (trainBrakeStatus.Contains(Viewer.Catalog.GetString("EQ")))
            {
                string brakeKey;
                switch (GetPressedKey(UserCommand.ControlTrainBrakeDecrease, UserCommand.ControlTrainBrakeIncrease))
                {
                    case UserCommand.ControlTrainBrakeDecrease:
                        brakeKey = Symbols.ArrowDown + ColorCode[Color.Yellow];
                        break;
                    case UserCommand.ControlTrainBrakeIncrease:
                        brakeKey = Symbols.ArrowUp + ColorCode[Color.Yellow];
                        break;
                    default:
                        brakeKey = "";
                        break;
                }
                brakeInfoValue = trainBrakeStatus.Substring(0, trainBrakeStatus.IndexOf(Viewer.Catalog.GetString("EQ"))).TrimEnd();
                AddLabel(new ListLabel
                {
                    FirstCol = Viewer.Catalog.GetString("Train brake"),
                    LastCol = $"{brakeInfoValue}{ColorCode[Color.Cyan]}",
                    KeyPressed = brakeKey,
                    SymbolCol = ""//brakeKey,
                });

                index = trainBrakeStatus.IndexOf(Viewer.Catalog.GetString("EQ"));
                brakeInfoValue = trainBrakeStatus.Substring(index, trainBrakeStatus.IndexOf(Viewer.Catalog.GetString("BC")) - index).TrimEnd();

                AddLabel(new ListLabel
                {
                    LastCol = brakeInfoValue,
                });
                if (trainBrakeStatus.Contains(Viewer.Catalog.GetString("EOT")))
                {
                    int indexOffset = Viewer.Catalog.GetString("EOT").Length + 1;
                    index = trainBrakeStatus.IndexOf(Viewer.Catalog.GetString("BC"));
                    brakeInfoValue = trainBrakeStatus.Substring(index, trainBrakeStatus.IndexOf(Viewer.Catalog.GetString("EOT")) - index).TrimEnd();
                    AddLabel(new ListLabel
                    {
                        LastCol = brakeInfoValue,
                    });
                    index = trainBrakeStatus.IndexOf(Viewer.Catalog.GetString("EOT")) + indexOffset;
                    brakeInfoValue = trainBrakeStatus.Substring(index, trainBrakeStatus.Length - index).TrimStart();
                    AddLabel(new ListLabel
                    {
                        LastCol = brakeInfoValue,
                    });
                }
                else
                {
                    index = trainBrakeStatus.IndexOf(Viewer.Catalog.GetString("BC"));
                    brakeInfoValue = trainBrakeStatus.Substring(index, trainBrakeStatus.Length - index).TrimEnd();
                    AddLabel(new ListLabel
                    {
                        LastCol = brakeInfoValue,
                    });
                }
            }
            else if (trainBrakeStatus.Contains(Viewer.Catalog.GetString("Lead")))
            {
                int indexOffset = Viewer.Catalog.GetString("Lead").Length + 1;
                brakeInfoValue = trainBrakeStatus.Substring(0, trainBrakeStatus.IndexOf(Viewer.Catalog.GetString("Lead"))).TrimEnd();
                AddLabel(new ListLabel
                {
                    FirstCol = Viewer.Catalog.GetString("Train brake"),
                    LastCol = $"{brakeInfoValue}{ColorCode[Color.Cyan]}",
                });

                index = trainBrakeStatus.IndexOf(Viewer.Catalog.GetString("Lead")) + indexOffset;
                if (trainBrakeStatus.Contains(Viewer.Catalog.GetString("EOT")))
                {
                    brakeInfoValue = trainBrakeStatus.Substring(index, trainBrakeStatus.IndexOf(Viewer.Catalog.GetString("EOT")) - index).TrimEnd();
                    AddLabel(new ListLabel
                    {
                        LastCol = brakeInfoValue,
                    });

                    index = trainBrakeStatus.IndexOf(Viewer.Catalog.GetString("EOT")) + indexOffset;
                    brakeInfoValue = trainBrakeStatus.Substring(index, trainBrakeStatus.Length - index).TrimEnd();
                    AddLabel(new ListLabel
                    {
                        LastCol = brakeInfoValue,
                    });
                }
                else
                {
                    brakeInfoValue = trainBrakeStatus.Substring(index, trainBrakeStatus.Length - index).TrimEnd();
                    AddLabel(new ListLabel
                    {
                        LastCol = brakeInfoValue,
                    });
                }
            }
            else if (trainBrakeStatus.Contains(Viewer.Catalog.GetString("BC")))
            {
                brakeInfoValue = trainBrakeStatus.Substring(0, trainBrakeStatus.IndexOf(Viewer.Catalog.GetString("BC"))).TrimEnd();
                AddLabel(new ListLabel
                {
                    FirstCol = Viewer.Catalog.GetString("Train brake"),
                    LastCol = $"{brakeInfoValue}{ColorCode[Color.Cyan]}",
                });

                index = trainBrakeStatus.IndexOf(Viewer.Catalog.GetString("BC"));
                brakeInfoValue = trainBrakeStatus.Substring(index, trainBrakeStatus.Length - index).TrimEnd();

                AddLabel(new ListLabel
                {
                    LastCol = brakeInfoValue,
                });
            }

            if (showRetainers)
            {
                AddLabel(new ListLabel
                {
                    FirstCol = Viewer.Catalog.GetString("Retainers"),
                    LastCol = $"{train.RetainerPercent} {Viewer.Catalog.GetString(GetStringAttribute.GetPrettyName(train.RetainerSetting))}",
                });
            }

            if (engineBrakeStatus.Contains(Viewer.Catalog.GetString("BC")))
            {
                AddLabel(new ListLabel
                {
                    FirstCol = Viewer.Catalog.GetString("Engine brake"),
                    LastCol = engineBrakeStatus.Substring(0, engineBrakeStatus.IndexOf("BC")) + ColorCode[Color.Cyan],
                });
                index = engineBrakeStatus.IndexOf(Viewer.Catalog.GetString("BC"));
                brakeInfoValue = engineBrakeStatus.Substring(index, engineBrakeStatus.Length - index).TrimEnd();
                AddLabel(new ListLabel
                {
                    FirstCol = Viewer.Catalog.GetString(""),
                    LastCol = $"{brakeInfoValue}{ColorCode[Color.White]}",
                });
            }
            else
            {
                AddLabel(new ListLabel
                {
                    FirstCol = Viewer.Catalog.GetString("Engine brake"),
                    LastCol = $"{engineBrakeStatus}{ColorCode[Color.Cyan]}",
                });
            }

            if (dynamicBrakeStatus != null && locomotive.IsLeadLocomotive())
            {
                if (locomotive.DynamicBrakePercent >= 0)
                {
                    AddLabel(new ListLabel
                    {
                        FirstCol = Viewer.Catalog.GetString("Dynamic brake"),
                        LastCol = locomotive.DynamicBrake ? dynamicBrakeStatus : Viewer.Catalog.GetString("Setup") + ColorCode[Color.Cyan],
                    });
                }
                else
                {
                    AddLabel(new ListLabel
                    {
                        FirstCol = Viewer.Catalog.GetString("Dynamic brake"),
                        LastCol = Viewer.Catalog.GetString("Off"),
                    });
                }
            }

            AddSeparator();

            if (locomotiveStatus != null)
            {
                foreach (string data in locomotiveStatus.Split('\n').Where((string d) => !string.IsNullOrWhiteSpace(d)))
                {
                    string[] parts = data.Split(new string[] { " = " }, 2, StringSplitOptions.None);
                    string keyPart = parts[0];
                    string valuePart = parts?[1];
                    if (Viewer.Catalog.GetString(keyPart).StartsWith(Viewer.Catalog.GetString("Boiler pressure")))
                    {
                        MSTSSteamLocomotive steamLocomotive2 = (MSTSSteamLocomotive)locomotive;
                        float bandUpper = steamLocomotive2.PreviousBoilerHeatOutBTUpS * 1.025f; // find upper bandwidth point
                        float bandLower = steamLocomotive2.PreviousBoilerHeatOutBTUpS * 0.975f; // find lower bandwidth point - gives a total 5% bandwidth

                        string heatIndicator;
                        if (steamLocomotive2.BoilerHeatInBTUpS > bandLower && steamLocomotive2.BoilerHeatInBTUpS < bandUpper)
                            heatIndicator = $"{Symbols.SmallDiamond}{ColorCode[Color.White]}";
                        else if (steamLocomotive2.BoilerHeatInBTUpS < bandLower)
                            heatIndicator = $"{Symbols.SmallArrowDown}{ColorCode[Color.Cyan]}";
                        else if (steamLocomotive2.BoilerHeatInBTUpS > bandUpper)
                            heatIndicator = $"{Symbols.SmallArrowUp}{ColorCode[Color.Orange]}";
                        else
                            heatIndicator = ColorCode[Color.White];

                        AddLabel(new ListLabel
                        {
                            FirstCol = Viewer.Catalog.GetString("Boiler pressure"),
                            LastCol = Viewer.Catalog.GetString(valuePart),
                            SymbolCol = heatIndicator,
                        });
                    }
                    else if (!normalTextMode && Viewer.Catalog.GetString(parts[0]).StartsWith(Viewer.Catalog.GetString("Fuel levels")))
                    {
                        AddLabel(new ListLabel
                        {
                            FirstCol = keyPart.EndsWith("?") || keyPart.EndsWith("!") ? Viewer.Catalog.GetString(keyPart.Substring(0, keyPart.Length - 3)) : Viewer.Catalog.GetString(keyPart),
                            LastCol = valuePart.Length > 1 ? Viewer.Catalog.GetString(valuePart.Replace(" ", string.Empty)) : "",
                        });
                    }
                    else if (keyPart.StartsWith(Viewer.Catalog.GetString("Gear")))
                    {
                        string gearKey;
                        switch (GetPressedKey(UserCommand.ControlGearDown, UserCommand.ControlGearUp))
                        {
                            case UserCommand.ControlGearDown:
                                gearKey = Symbols.ArrowDown + ColorCode[Color.Yellow];
                                break;
                            case UserCommand.ControlGearUp:
                                gearKey = Symbols.ArrowUp + ColorCode[Color.Yellow];
                                break;
                            default:
                                gearKey = "";
                                break;
                        }

                        AddLabel(new ListLabel
                        {
                            FirstCol = Viewer.Catalog.GetString(keyPart),
                            LastCol = valuePart != null ? Viewer.Catalog.GetString(valuePart) : "",
                            KeyPressed = gearKey,
                            SymbolCol = gearKey,
                        });
                    }
                    else if (parts.Contains(Viewer.Catalog.GetString("Pantographs")))
                    {
                        string pantoKey;
                        switch (GetPressedKey(UserCommand.ControlPantograph1))
                        {
                            case UserCommand.ControlPantograph1:
                                string arrow = parts[1].StartsWith(Viewer.Catalog.GetString("Up")) ? Symbols.ArrowUp : Symbols.ArrowDown;
                                pantoKey = arrow + ColorCode[Color.Yellow];
                                break;
                            default:
                                pantoKey = "";
                                break;
                        }

                        AddLabel(new ListLabel
                        {
                            FirstCol = Viewer.Catalog.GetString(keyPart),
                            LastCol = valuePart != null ? Viewer.Catalog.GetString(valuePart) : "",
                            KeyPressed = pantoKey,
                        });
                    }
                    else if (parts.Contains(Viewer.Catalog.GetString("Engine")))
                    {
                        AddLabel(new ListLabel
                        {
                            FirstCol = Viewer.Catalog.GetString(keyPart),
                            LastCol = valuePart != null ? $"{Viewer.Catalog.GetString(valuePart)}{ColorCode[Color.White]}" : "",
                        });
                    }
                    else
                    {
                        AddLabel(new ListLabel
                        {
                            FirstCol = keyPart.EndsWith("?") || keyPart.EndsWith("!") ? Viewer.Catalog.GetString(keyPart.Substring(0, keyPart.Length - 3)) : Viewer.Catalog.GetString(keyPart),
                            LastCol = valuePart != null ? Viewer.Catalog.GetString(valuePart) : "",
                        });
                    }
                }
            }

            AddSeparator();

            // FPS
            if (normalTextMode)
            {
                AddLabel(new ListLabel
                {
                    FirstCol = Viewer.Catalog.GetString("FPS"),
                    LastCol = $"{Math.Floor(viewer.RenderProcess.FrameRate.SmoothedValue)}",
                });
            }

            // Messages
            // Autopilot
            bool autopilot = locomotive.Train.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING;
            AddLabel(new ListLabel
            {
                FirstCol = Viewer.Catalog.GetString("Autopilot"),
                LastCol = autopilot ? Viewer.Catalog.GetString("On") + ColorCode[Color.Yellow] : Viewer.Catalog.GetString("Off"),
            });

            //AI Fireman
            if (locomotive is MSTSSteamLocomotive steamLocomotive3)
            {
                string aifireKey;
                aifireKey = Symbols.ArrowToRight + ColorCode[Color.Yellow];
                switch (GetPressedKey(UserCommand.ControlAIFireOn, UserCommand.ControlAIFireOff, UserCommand.ControlAIFireReset))
                {
                    case UserCommand.ControlAIFireOn:
                        ctrlAIFiremanReset = ctrlAIFiremanOff = false;
                        ctrlAIFiremanOn = true;
                        break;
                    case UserCommand.ControlAIFireOff:
                        ctrlAIFiremanReset = ctrlAIFiremanOn = false;
                        ctrlAIFiremanOff = true;
                        break;
                    case UserCommand.ControlAIFireReset:
                        ctrlAIFiremanOn = ctrlAIFiremanOff = false;
                        ctrlAIFiremanReset = true;
                        clockAIFireTime = Owner.Viewer.Simulator.ClockTime;
                        break;
                    default:
                        aifireKey = "";
                        break;
                }

                // waiting time to hide the reset label
                if (ctrlAIFiremanReset && clockAIFireTime + 5 < Owner.Viewer.Simulator.ClockTime)
                    ctrlAIFiremanReset = false;

                if (ctrlAIFiremanReset || ctrlAIFiremanOn || ctrlAIFiremanOff)
                {
                    AddLabel(new ListLabel
                    {
                        FirstCol = Viewer.Catalog.GetString("AI Fireman") + ColorCode[Color.White],
                        LastCol = ctrlAIFiremanOn ? Viewer.Catalog.GetString("On") : ctrlAIFiremanOff ? Viewer.Catalog.GetString("Off") : ctrlAIFiremanReset ? Viewer.Catalog.GetString("Reset") + ColorCode[Color.Cyan] : "",
                        KeyPressed = aifireKey
                    });
                }
            }

            // Grate limit
            if (locomotive is MSTSSteamLocomotive steamLocomotive1)
            {
                if (steamLocomotive1.IsGrateLimit && steamLocomotive1.GrateCombustionRateLBpFt2 > steamLocomotive1.GrateLimitLBpFt2)
                {
                    grateLabelVisible = true;
                    clockGrateTime = Owner.Viewer.Simulator.ClockTime;

                    AddLabel(new ListLabel
                    {
                        FirstCol = Viewer.Catalog.GetString("Grate limit"),
                        LastCol = Viewer.Catalog.GetString("Exceeded") + ColorCode[Color.OrangeRed],
                    });
                }
                else
                {
                    // delay to hide the grate label
                    if (grateLabelVisible && clockGrateTime + 3 < Owner.Viewer.Simulator.ClockTime)
                        grateLabelVisible = false;

                    if (grateLabelVisible)
                    {
                        AddLabel(new ListLabel
                        {
                            FirstCol = Viewer.Catalog.GetString("Grate limit") + ColorCode[Color.White],
                            LastCol = Viewer.Catalog.GetString("Normal") + ColorCode[Color.White]
                        });
                    }
                }
            }


            // Wheel
            if (train.IsWheelSlip || train.IsWheelSlipWarninq || train.IsBrakeSkid)
            {
                wheelLabelVisible = true;
                clockWheelTime = Owner.Viewer.Simulator.ClockTime;
            }

            if (train.IsWheelSlip)
            {
                AddLabel(new ListLabel
                {
                    FirstCol = Viewer.Catalog.GetString("Wheel"),
                    LastCol = Viewer.Catalog.GetString("slip") + ColorCode[Color.OrangeRed],
                });
            }
            else if (train.IsWheelSlipWarninq)
            {
                AddLabel(new ListLabel
                {
                    FirstCol = Viewer.Catalog.GetString("Wheel"),
                    LastCol = Viewer.Catalog.GetString("slip warning") + ColorCode[Color.Yellow],
                });
            }
            else if (train.IsBrakeSkid)
            {
                AddLabel(new ListLabel
                {
                    FirstCol = Viewer.Catalog.GetString("Wheel"),
                    LastCol = Viewer.Catalog.GetString("skid") + ColorCode[Color.OrangeRed],
                });
            }
            else
            {
                // delay to hide the wheel label
                if (wheelLabelVisible && clockWheelTime + 3 < Owner.Viewer.Simulator.ClockTime)
                    wheelLabelVisible = false;

                if (wheelLabelVisible)
                {
                    AddLabel(new ListLabel
                    {
                        FirstCol = Viewer.Catalog.GetString("Wheel") + ColorCode[Color.White],
                        LastCol = Viewer.Catalog.GetString("Normal") + ColorCode[Color.White]
                    });
                }
            }

            //Derailment Coefficient. Changed the float value output by a text label.
            var maxDerailCoeff = 0.0f;
            var carIDerailCoeff = "";
            for (var i = 0; i < train.Cars.Count; i++)
            {
                var carDerailCoeff = train.Cars[i].DerailmentCoefficient;
                carDerailCoeff = float.IsInfinity(carDerailCoeff) || float.IsNaN(carDerailCoeff) ? 0 : carDerailCoeff;
                if (carDerailCoeff > maxDerailCoeff)
                {
                    maxDerailCoeff = carDerailCoeff;
                    carIDerailCoeff = train.Cars[i].CarID;
                }
            }

            if (maxDerailCoeff > 0.66)
            {
                derailLabelVisible = true;
                clockDerailTime = Owner.Viewer.Simulator.ClockTime;
            }

            if (maxDerailCoeff > 0.66)
            {
                if (maxDerailCoeff > 1)
                {
                    AddLabel(new ListLabel
                    {
                        FirstCol = Viewer.Catalog.GetString("DerailCoeff"),
                        LastCol = $"{Viewer.Catalog.GetString("Derailed")} {carIDerailCoeff}" + ColorCode[Color.OrangeRed],
                    });
                }
                else if (maxDerailCoeff < 1 && maxDerailCoeff > 0.66)
                {
                    AddLabel(new ListLabel
                    {
                        FirstCol = Viewer.Catalog.GetString("DerailCoeff"),
                        LastCol = $"{Viewer.Catalog.GetString("Warning")} {carIDerailCoeff}" + ColorCode[Color.Yellow],
                    });
                }
            }
            else
            {
                // delay to hide the derailcoeff label
                if (derailLabelVisible && clockDerailTime + 3 < Owner.Viewer.Simulator.ClockTime)
                    derailLabelVisible = false;

                if (derailLabelVisible)
                {
                    AddLabel(new ListLabel
                    {
                        FirstCol = Viewer.Catalog.GetString("DerailCoeff") + ColorCode[Color.White],
                        LastCol = Viewer.Catalog.GetString("Normal") + ColorCode[Color.White]
                    });
                }
            }


            // Doors
            var wagon = (MSTSWagon)locomotive;
            if (wagon.DoorLeftOpen || wagon.DoorRightOpen)
            {
                var status = new List<string>();
                bool flipped = locomotive.GetCabFlipped();
                doorsLabelVisible = true;
                clockDoorsTime = Owner.Viewer.Simulator.ClockTime;
                if (wagon.DoorLeftOpen)
                    status.Add(Viewer.Catalog.GetString(Viewer.Catalog.GetString(flipped ? "Right" : "Left")));
                if (wagon.DoorRightOpen)
                    status.Add(Viewer.Catalog.GetString(Viewer.Catalog.GetString(flipped ? "Left" : "Right")));

                AddLabel(new ListLabel
                {
                    FirstCol = Viewer.Catalog.GetString("Doors open"),
                    LastCol = string.Join(" ", status) + ColorCode[locomotive.AbsSpeedMpS > 0.1f ? Color.OrangeRed : Color.Yellow],
                });
            }
            else
            {
                // delay to hide the doors label
                if (doorsLabelVisible && clockDoorsTime + 3 < Owner.Viewer.Simulator.ClockTime)
                    doorsLabelVisible = false;

                if (doorsLabelVisible)
                {
                    AddLabel(new ListLabel
                    {
                        FirstCol = Viewer.Catalog.GetString("Doors open") + ColorCode[Color.White],
                        LastCol = Viewer.Catalog.GetString("Closed") + ColorCode[Color.White]
                    });
                }
            }

            AddLabel(new ListLabel());
            UpdateDataEnded = true;
            return labels;
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            var MovingCurrentWindow = UserInput.IsMouseLeftButtonDown &&
                   UserInput.MouseX >= Location.X && UserInput.MouseX <= Location.X + Location.Width &&
                   UserInput.MouseY >= Location.Y && UserInput.MouseY <= Location.Y + Location.Height ?
                   true : false;

            // Avoid to updateFull when the window is moving
            if (!MovingCurrentWindow && !TrainDrivingUpdating && updateFull)
            {
                TrainDrivingUpdating = true;
                labels = TrainDrivingWindowList(Owner.Viewer, normalTextMode).ToList();
                TrainDrivingUpdating = false;

                // Ctrl + F (FiringIsManual)
                if (ResizeWindow || LinesCount != labels.Count())
                {
                    ResizeWindow = false;
                    UpdateWindowSize();
                    LinesCount = labels.Count();
                }
                //Resize this window after the font has been changed externally
                if (MultiPlayerWindow.FontChanged)
                {
                    MultiPlayerWindow.FontChanged = false;
                    FontToBold = !FontToBold;
                    UpdateWindowSize();
                }
                //Update Layout
                Layout();
            }
        }

        private static string Round(float x) => $"{Math.Round(x):F0}";

        private static UserCommand? GetPressedKey(params UserCommand[] keysToTest) => keysToTest
            .Where((UserCommand key) => UserInput.IsDown(key))
            .FirstOrDefault();
    }
}
