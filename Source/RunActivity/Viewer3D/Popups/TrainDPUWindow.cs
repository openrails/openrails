// COPYRIGHT 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
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

// This file is the responsibility of the 3D & Environment Team.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using ORTS.Common;
using ORTS.Common.Input;

namespace Orts.Viewer3D.Popups
{
    public class TrainDpuWindow : Window
    {
        bool ResizeWindow = false;
        bool UpdateDataEnded = false;
        int FirstColLenght = 0;
        int FirstColOverFlow = 0;
        int LastColLenght = 0;
        int LastColOverFlow = 0;
        int LastPlayerTrainCars;
        int dpiOffset = 0;

        public bool normalTextMode = true;// Standard text
        public bool normalVerticalMode = true;// vertical window size
        public bool TrainDpuUpdating = false;
        int dieselLocomotivesCount = 0;
        int maxFirstColWidth = 0;
        int maxLastColWidth = 0;
        int WindowHeightMin = 0;
        int WindowHeightMax = 0;
        int WindowWidthMin = 0;
        int WindowWidthMax = 0;

        const int TextSize = 15;
        public int keyPresLenght;
        public int OffSetX = 0;

        Label ExpandWindow;
        Label VerticalWindow;
        Label indicator;
        LabelMono indicatorMono;
        Label LabelFontToBold;
        public static bool FontChanged;
        public static bool FontToBold;
        public static bool MonoFont;

        /// <summary>
        /// A Train Dpu row with data fields.
        /// </summary>
        public struct ListLabel
        {
            public string FirstCol;
            public int FirstColWidth;
            public List<string> LastCol;
            public List<int> LastColWidth;
            public List<string> SymbolCol;
            public bool ChangeColWidth;
            public string KeyPressed;
        }

        public List<ListLabel> labels = new List<ListLabel>();

        public List<ListLabel> TempListToLabel = new List<ListLabel>();// used when listtolabel is changing

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

        // Change text color
        readonly Dictionary<string, Color> ColorCodeCtrl = new Dictionary<string, Color>
        {
            { "!!!", Color.OrangeRed },
            { "!!?", Color.Orange },
            { "!??", Color.White },
            { "?!?", Color.Black },
            { "???", Color.Yellow },
            { "??!", Color.Green },
            { "?!!", Color.PaleGreen },
            { "$$$", Color.LightSkyBlue},
            { "%%%", Color.Cyan}
        };

        private static class Symbols
        {
            public const string Fence = "\u2590";
            public const string ArrowUp = "▲";
            public const string ArrowDown = "▼";
            public const string ArrowToRight = "\u25BA";
            public const string ArrowToLeft = "\u25C4";
        }

        readonly Dictionary<string, string> FirstColToAbbreviated = new Dictionary<string, string>()
        {
            [Viewer.Catalog.GetString("Flow")] = Viewer.Catalog.GetString("FLOW"),//
            [Viewer.Catalog.GetString("Fuel")] = Viewer.Catalog.GetString("FUEL"),//
            [Viewer.Catalog.GetString("Load")] = Viewer.Catalog.GetString("LOAD"),//
            [Viewer.Catalog.GetString("Loco Groups")] = Viewer.Catalog.GetString("GRUP"),
            [Viewer.Catalog.GetString("Oil Pressure")] = Viewer.Catalog.GetString("OIL"),//
            [Viewer.Catalog.GetString("Power")] = Viewer.Catalog.GetString("POWR"),//
            [Viewer.Catalog.GetString("Remote")] = Viewer.Catalog.GetString("RMT"),//
            [Viewer.Catalog.GetString("RPM")] = Viewer.Catalog.GetString("RPM"),//
            [Viewer.Catalog.GetString("Reverser")] = Viewer.Catalog.GetString("REVR"),//
            [Viewer.Catalog.GetString("Status")] = Viewer.Catalog.GetString("STAT"),//
            [Viewer.Catalog.GetString("Temperature")] = Viewer.Catalog.GetString("TEMP"),//
            [Viewer.Catalog.GetString("Throttle")] = Viewer.Catalog.GetString("THRO"),//
            [Viewer.Catalog.GetString("Time")] = Viewer.Catalog.GetString("TIME"),//
            [Viewer.Catalog.GetString("Tractive Effort")] = Viewer.Catalog.GetString("TRACT")//
        };

        readonly Dictionary<string, string> LastColToAbbreviated = new Dictionary<string, string>()
        {
            [Viewer.Catalog.GetString("Forward")] = Viewer.Catalog.GetString("Forw."),
            [Viewer.Catalog.GetString("Idle")] = Viewer.Catalog.GetString("Idle"),
            [Viewer.Catalog.GetString("Running")] = Viewer.Catalog.GetString("Runn")
        };

        public TrainDpuWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 10, Window.DecorationSize.Y + owner.TextFontDefault.Height * 10, Viewer.Catalog.GetString("Train Dpu Info"))
        {
            WindowHeightMin = Location.Height / 2;
            WindowHeightMax = Location.Height + owner.TextFontDefault.Height * 20;
            WindowWidthMin = Location.Width;
            WindowWidthMax = Location.Width + owner.TextFontDefault.Height * 20;
        }

        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            outf.Write(normalTextMode);
            outf.Write(normalVerticalMode);
            outf.Write(Location.X);
            outf.Write(Location.Y);
            outf.Write(Location.Width);
            outf.Write(Location.Height);
        }

        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            Rectangle LocationRestore;
            normalTextMode = inf.ReadBoolean();
            normalVerticalMode = inf.ReadBoolean();
            LocationRestore.X = inf.ReadInt32();
            LocationRestore.Y = inf.ReadInt32();
            LocationRestore.Width = inf.ReadInt32();
            LocationRestore.Height = inf.ReadInt32();

            // Display window
            SizeTo(LocationRestore.Width, LocationRestore.Height);
            MoveTo(LocationRestore.X, LocationRestore.Y);
        }

        protected internal override void Initialize()
        {
            base.Initialize();
            if (Visible)
            {   // Reset window size
                UpdateWindowSize();
            }
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            // Display main DUP data
            var vbox = base.Layout(layout).AddLayoutVertical();
            if (labels.Count > 0)
            {
                var colWidth = labels.Max(x => x.FirstColWidth) + TextSize;// right space

                var lastColLenght = 0;
                var LastColLenght = 0;
                foreach (var data in labels.Where(x => x.LastColWidth != null && x.LastColWidth.Count > 0))
                {
                    lastColLenght = data.LastColWidth.Max(x => x);
                    LastColLenght = lastColLenght > LastColLenght ? lastColLenght : LastColLenght;
                }

                var lastWidth = LastColLenght + TextSize / 2;
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
                        var FirstCol = " " + data.FirstCol;
                        var LastCol = data.LastCol;
                        var SymbolCol = data.SymbolCol;
                        var locoGroups = new[] { Viewer.Catalog.GetString("Loco Groups"), Viewer.Catalog.GetString("GRUP") }.Any(s => FirstCol.Contains(s));

                        if (LastCol != null && LastCol[0] != null)
                        {
                            //Avoids troubles when the Main Scale (Windows DPI settings) is not set to 100%
                            if (locoGroups)
                                TimeHboxPositionY = hbox.Position.Y;

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

                            for (int i = 0; i < data.LastCol.Count; i++)
                            {
                                var colorFirstColEndsWith = ColorCodeCtrl.Keys.Any(FirstCol.EndsWith) ? ColorCodeCtrl[FirstCol.Substring(FirstCol.Length - 3)] : Color.White;
                                var colorLastColEndsWith = ColorCodeCtrl.Keys.Any(LastCol[i].EndsWith) ? ColorCodeCtrl[LastCol[i].Substring(LastCol[i].Length - 3)] : Color.White;
                                var colorSymbolCol = ColorCodeCtrl.Keys.Any(data.SymbolCol[i].EndsWith) ? ColorCodeCtrl[data.SymbolCol[i].Substring(data.SymbolCol[i].Length - 3)] : Color.White;

                                // Erase the color code at the string end
                                SymbolCol[i] = ColorCodeCtrl.Keys.Any(data.SymbolCol[i].EndsWith) ? data.SymbolCol[i].Substring(0, data.SymbolCol[i].Length - 3) : data.SymbolCol[i];
                                LastCol[i] = ColorCodeCtrl.Keys.Any(LastCol[i].EndsWith) ? LastCol[i].Substring(0, LastCol[i].Length - 3) : LastCol[i];

                                if (SymbolCol[i].Contains(Symbols.Fence))
                                {
                                    hbox.Add(indicator = new Label(-(TextSize / 2), 0, TextSize, hbox.RemainingHeight, Symbols.Fence, LabelAlignment.Left));
                                    indicator.Color = Color.Green;

                                    // Apply color to LastCol
                                    var lastCol = LastCol[i].Replace("|", " ");
                                    hbox.Add(indicator = new Label(lastWidth, hbox.RemainingHeight, lastCol, locoGroups ? LabelAlignment.Center : LabelAlignment.Left));//center
                                    indicator.Color = colorFirstColEndsWith == Color.White ? colorLastColEndsWith : colorFirstColEndsWith;
                                }
                                else
                                {
                                    // Font to bold, clickable label
                                    if (hbox.Position.Y == TimeHboxPositionY && i == 0)
                                    {
                                        hbox.Add(LabelFontToBold = new Label(lastWidth, hbox.RemainingHeight, LastCol[i], locoGroups ? LabelAlignment.Center : LabelAlignment.Left));
                                        LabelFontToBold.Click += new Action<Control, Point>(FontToBold_Click);
                                    }
                                    else
                                    {
                                        if (i > 0)
                                        {
                                            hbox.Add(indicator = new Label(-(TextSize / 2), 0, TextSize, hbox.RemainingHeight, SymbolCol[i], LabelAlignment.Left));
                                            indicator.Color = colorSymbolCol;
                                        }
                                        hbox.Add(indicator = new Label(lastWidth, hbox.RemainingHeight, LastCol[i], locoGroups ? LabelAlignment.Center : LabelAlignment.Left));
                                        indicator.Color = colorLastColEndsWith;
                                    }
                                }
                            }
                        }

                        // Clickable symbol
                        if (hbox.Position.Y == TimeHboxPositionY)
                        {
                            var verticalWindow = normalVerticalMode ? Symbols.ArrowDown : Symbols.ArrowUp;// ▲ : ▶
                            hbox.Add(VerticalWindow = new Label(hbox.RemainingWidth - (TextSize * 2), 0, TextSize, hbox.RemainingHeight, verticalWindow.ToString(), LabelAlignment.Right));
                            VerticalWindow.Color = Color.Yellow;
                            VerticalWindow.Click += new Action<Control, Point>(VerticalWindow_Click);

                            var expandWindow = normalTextMode ? Symbols.ArrowToLeft : Symbols.ArrowToRight;// ◀ : ▶
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
            }// close
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

        void VerticalWindow_Click(Control arg1, Point arg2)
        {
            normalVerticalMode = !normalVerticalMode;
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
            labels = TrainDPUWindowList(Owner.Viewer, normalTextMode).ToList();
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

                var lastColLenght = 0;
                foreach (var data in labels.Where(x => x.LastColWidth != null && x.LastColWidth.Count > 0))
                {
                    lastColLenght = data.LastColWidth.Max(x => x) + TextSize / 2;
                    LastColLenght = lastColLenght > LastColLenght ? lastColLenght : LastColLenght;
                }

                // Validates rows with windows DPI settings
                dpiOffset = (System.Drawing.Graphics.FromHwnd(IntPtr.Zero).DpiY / 96) > 1.00f ? 1 : 0;// values from testing
                var rowCount = labels.Where(x => !string.IsNullOrEmpty(x.FirstCol)).Count() - dpiOffset;

                var desiredHeight = FontToBold ? (Owner.TextFontDefaultBold.Height + 2) * (rowCount + 1)
                    : (Owner.TextFontDefault.Height + 2) * (rowCount + 1);
                var desiredWidth = FirstColLenght + (LastColLenght * (dieselLocomotivesCount + 1));// interval between firstcol and lastcol
                var normalMode = normalTextMode && normalVerticalMode;
                var newHeight = desiredHeight < WindowHeightMin ? desiredHeight + Owner.TextFontDefault.Height * 2
                    : (int)MathHelper.Clamp(desiredHeight, (normalMode ? WindowHeightMin : 100), WindowHeightMax);

                var newWidth = (int)MathHelper.Clamp(desiredWidth, (normalTextMode ? WindowWidthMin : 100), WindowWidthMax + (Owner.Viewer.DisplaySize.X / 2));

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

            if (label.LastCol != null)
            {
                for (int i = 0; i < label.LastCol.Count; i++)
                {
                    var LastCol = label.LastCol[i];
                    CheckString(ref LastCol);
                    label.LastCol[i] = LastCol;
                }
            }

            if (label.SymbolCol != null)
            {
                for (int i = 0; i < label.SymbolCol.Count; i++)
                {
                    var symbolCol = label.SymbolCol[i];
                    CheckString(ref symbolCol);
                    label.SymbolCol[i] = symbolCol;
                }
            }
            CheckString(ref label.KeyPressed);

            UpdateColsWidth(label, normalMode);
        }

        /// <summary>
        /// Display info according to the full text window or the slim text window
        /// </summary>
        /// <param name="firstkeyactivated"></param>
        /// <param name="firstcol"></param>
        /// <param name="lastcol"></param>
        /// <param name="symbolcol"></param>
        /// <param name="changecolwidth"></param>
        /// <param name="lastkeyactivated"></param>

        private void UpdateColsWidth(ListLabel label, bool normalmode)
        {
            if (!UpdateDataEnded)
            {
                if (!normalTextMode)
                {
                    foreach (KeyValuePair<string, string> mapping in FirstColToAbbreviated)
                        label.FirstCol = label.FirstCol.Replace(mapping.Key, mapping.Value);
                    foreach (KeyValuePair<string, string> mapping in LastColToAbbreviated)
                    {
                        if (label.LastCol != null)
                        {
                            for (int i = 0; i < label.LastCol.Count; i++)
                            {
                                label.LastCol[i] = label.LastCol[i].Replace(mapping.Key, mapping.Value);
                            }
                        }
                    }
                }
                var firstCol = label.FirstCol;
                var firstColWidth = 0;
                var lastCol = label.LastCol;
                List<int> lastColWidth = new List<int>();
                var symbolCol = label.SymbolCol;
                var keyPressed = label.KeyPressed;
                var changeColwidth = label.ChangeColWidth;

                if (!firstCol.Contains("Sprtr"))
                {
                    if (ColorCodeCtrl.Keys.Any(firstCol.EndsWith))
                    {
                        var tempFirstCol = firstCol.Substring(0, firstCol.Length - 3);
                        firstColWidth = FontToBold ? Owner.TextFontDefaultBold.MeasureString(tempFirstCol.TrimEnd())
                            : !normalTextMode ? Owner.TextFontMonoSpacedBold.MeasureString(tempFirstCol.TrimEnd())
                            : Owner.TextFontDefault.MeasureString(tempFirstCol.TrimEnd());
                    }
                    else
                    {
                        firstColWidth = FontToBold ? Owner.TextFontDefaultBold.MeasureString(firstCol.TrimEnd())
                            : !normalTextMode ? Owner.TextFontMonoSpacedBold.MeasureString(firstCol.TrimEnd())
                            : Owner.TextFontDefault.MeasureString(firstCol.TrimEnd());
                    }

                    if (label.LastCol != null)
                    {
                        foreach (string data in label.LastCol)
                        {
                            if (data != null)
                            {
                                data.Replace("|", "");
                                if (ColorCodeCtrl.Keys.Any(data.EndsWith))
                                {
                                    var tempLastCol = data.Substring(0, data.Length - 3);
                                    lastColWidth.Add(FontToBold ? Owner.TextFontDefaultBold.MeasureString(tempLastCol.TrimEnd())
                                        : Owner.TextFontDefault.MeasureString(tempLastCol.TrimEnd()));
                                }
                                else
                                {
                                    lastColWidth.Add(FontToBold ? Owner.TextFontDefaultBold.MeasureString(data.TrimEnd())
                                        : Owner.TextFontDefault.MeasureString(data.TrimEnd()));
                                }
                            }
                        }
                    }
                }

                //Set a minimum value for LastColWidth to avoid overlap between time value and clickable symbol
                if (labels.Count == 1)//&& lastColWidth.Count > 0)
                {
                    lastColWidth.Add(labels[0].LastColWidth[0] + (TextSize * 3) + dpiOffset * 10);// time value + clickable symbol
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

                    if (label.LastColWidth != null)
                    {
                        for (int i = 0; i < label.LastColWidth.Count; i++)
                        {
                            if (maxLastColWidth < lastColWidth[i]) LastColOverFlow = maxLastColWidth;
                        }
                    }
                    ResizeWindow = true;
                }
            }
            else
            {
                if (Visible)
                {
                    // Detect Autopilot is on to avoid flickering when slim window is displayed
                    var AutopilotOn = Owner.Viewer.PlayerLocomotive.Train.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING ? true : false;

                    //ResizeWindow, when the string spans over the right boundary of the window
                    maxFirstColWidth = labels.Max(x => x.FirstColWidth);
                    maxLastColWidth = labels.Max(x => x.LastColWidth[0]);

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
        public IEnumerable<ListLabel> TrainDPUWindowList(Viewer viewer, bool normalTextMode)
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
            MSTSLocomotive locomotive = (MSTSLocomotive)trainCar;
            var multipleUnitsConfiguration = locomotive.GetMultipleUnitsConfiguration();
            var lastCol = new List<string>();
            var symbolCol = new List<string>();
            var notDpuTrain = false;

            labels.Clear();
            UpdateDataEnded = false;

            // Distributed Power
            if (multipleUnitsConfiguration != null)
            {
                lastCol = new List<string>();
                symbolCol = new List<string>();
                char[] multipleUnits = multipleUnitsConfiguration.Replace(" ", "").ToCharArray();
                symbolCol.Add("");//first symbol empty
                foreach (char ch in multipleUnits)
                {
                    if (ch.ToString() != " ")
                    {
                        if (Char.IsDigit(ch))
                        {
                            lastCol.Add(ch.ToString()); continue;
                        }
                        else
                            symbolCol.Add(ch == '|' ? Symbols.Fence + ColorCode[Color.Green] : ch == '–' ? ch.ToString() : "");
                    }
                }

                // allows to draw the second fence
                lastCol.Add("");
                symbolCol.Add("");
                AddLabel(new ListLabel
                {
                    FirstCol = Viewer.Catalog.GetString("Loco Groups"),
                    SymbolCol = symbolCol,
                    LastCol = lastCol
                });
                AddSeparator();
            }
            else
            {
                lastCol = new List<string>();
                symbolCol = new List<string>();
                lastCol.Add("");
                symbolCol.Add("");
                AddLabel(new ListLabel
                {
                    FirstCol = Viewer.Catalog.GetString(" Distributed power management not available with this player train. "),
                    SymbolCol = symbolCol,
                    LastCol = lastCol
                });
                notDpuTrain = true;
            }

            if (locomotive != null && !notDpuTrain)
            {
                int numberOfDieselLocomotives = 0;
                int maxNumberOfEngines = 0;
                for (var i = 0; i < train.Cars.Count; i++)
                {
                    if (train.Cars[i] is MSTSDieselLocomotive)
                    {
                        numberOfDieselLocomotives++;
                        maxNumberOfEngines = Math.Max(maxNumberOfEngines, (train.Cars[i] as MSTSDieselLocomotive).DieselEngines.Count);
                    }
                }
                if (numberOfDieselLocomotives > 0)
                {
                    var dieselLoco = MSTSDieselLocomotive.GetDpuHeader(normalVerticalMode, numberOfDieselLocomotives, maxNumberOfEngines).Replace("\t", "");
                    string[] dieselLocoHeader = dieselLoco.Split('\n');
                    string[,] tempStatus = new string[numberOfDieselLocomotives, dieselLocoHeader.Length];
                    var k = 0;
                    var dpUnitId = 0;
                    var dpUId = -1;
                    for (var i = 0; i < train.Cars.Count; i++)
                    {
                        if (train.Cars[i] is MSTSDieselLocomotive)
                        {
                            if (dpUId != (train.Cars[i] as MSTSLocomotive).DPUnitID)
                            {
                                var status = (train.Cars[i] as MSTSDieselLocomotive).GetDpuStatus(normalVerticalMode).Split('\t');
                                var fence = ((dpUnitId != (dpUnitId = train.Cars[i].RemoteControlGroup)) ? "|" : " ");
                                for (var j = 0; j < status.Length; j++)
                                {
                                    // fence
                                    tempStatus[k, j] = fence + status[j];
                                }
                                dpUId = (train.Cars[i] as MSTSLocomotive).DPUnitID;
                                k++;
                            }
                        }
                    }

                    dieselLocomotivesCount = k;// only leaders loco group
                    for (var j = 0; j < dieselLocoHeader.Count(); j++)
                    {
                        lastCol = new List<string>();
                        symbolCol = new List<string>();

                        for (int i = 0; i < dieselLocomotivesCount; i++)
                        {
                            symbolCol.Add(tempStatus[i, j] != null && tempStatus[i, j].Contains("|") ? Symbols.Fence + ColorCode[Color.Green] : " ");
                            lastCol.Add(tempStatus[i, j]);
                        }

                        // allows to draw the second fence
                        lastCol.Add("");
                        symbolCol.Add(" ");

                        AddLabel(new ListLabel
                        {
                            FirstCol = dieselLocoHeader[j],
                            SymbolCol = symbolCol,
                            LastCol = lastCol
                        });
                    }
                }
                AddLabel(new ListLabel());
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
            if (!MovingCurrentWindow && !TrainDpuUpdating && updateFull)
            {
                TrainDpuUpdating = true;
                labels = TrainDPUWindowList(Owner.Viewer, normalTextMode).ToList();
                TrainDpuUpdating = false;

                //Resize this window when the cars count has been changed
                if (Owner.Viewer.PlayerTrain.Cars.Count != LastPlayerTrainCars)
                {
                    LastPlayerTrainCars = Owner.Viewer.PlayerTrain.Cars.Count;
                    UpdateWindowSize();
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