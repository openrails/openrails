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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ORTS.Common;
using ORTS.Common.Input;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems.Brakes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using System.Threading;
using System.IO;

namespace Orts.Viewer3D.Popups
{
    public class MultiPlayerWindow : Window
    {
        bool ResizeWindow;
        int FirstColLenght = 0;
        int FirstColOverFlow = 0;
        int LastColLenght = 0;
        int LastColOverFlow = 0;
        int LinesCount = 0;
        const int TrainDrivingInfoHeightInLinesOfText = 1;
        bool UpdateDataEnded = false;

        public static bool StandardHUD = true;// Standard full text or not.

        int WindowHeightMax = 0;
        int WindowHeightMin = 0;
        int WindowWidthMin = 0;
        int WindowWidthMax = 0;

        char expandWindow;
        const int TextSize = 15;
        public int keyPresLenght;
        public int OffSetX = 0;
        string keyPressed;// display a symbol when a control key is pressed.

        Label ExpandWindow;
        Label indicator;
        Label LabelFontToBold;
        public static bool FontToBold;
        public static bool MonoFont;

        public struct ListLabel
        {
            public string FirstCol { get; set; }
            public int FirstColWidth { get; set; }
            public string LastCol { get; set; }
            public int LastColWidth { get; set; }
            public string SymbolCol { get; set; }
            public bool ChangeColWidth { get; set; }
            public string keyPressed { get; set; }
        }
        List<ListLabel> ListToLabel = new List<ListLabel>();

        // Change text color
        Dictionary<string, Color> ColorCode = new Dictionary<string, Color>
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

        public MultiPlayerWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 10, Window.DecorationSize.Y + (owner.TextFontDefault.Height * (TrainDrivingInfoHeightInLinesOfText)), Viewer.Catalog.GetString("MultiPlayer Info"))
        {
            WindowHeightMin = Location.Height;
            WindowHeightMax = Location.Height + owner.TextFontDefault.Height * 20; // 20 lines
            WindowWidthMin = Location.Width;
            WindowWidthMax = Location.Width + owner.TextFontDefault.Height * 20; // 20 char
        }

        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            outf.Write(StandardHUD);
            outf.Write(Location.X);
            outf.Write(Location.Y);
            outf.Write(Location.Width);
            outf.Write(Location.Height);
        }

        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            Rectangle LocationRestore;
            StandardHUD = inf.ReadBoolean();
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
            // Reset window size
            UpdateWindowSize();
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            // Display main HUD data
            var vbox = base.Layout(layout).AddLayoutVertical();
            if (ListToLabel.Count > 0)
            {
                var colWidth = ListToLabel.Max(x => x.FirstColWidth) + (StandardHUD ? FontToBold ? 19 : 16 : 8);
                var TimeHboxPositionY = 0;
                foreach (var data in ListToLabel)
                {
                    if (data.FirstCol.Contains(Viewer.Catalog.GetString("NwLn")))
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
                        var SymbolCol = data.SymbolCol;

                        if (ColorCode.Keys.Any(FirstCol.EndsWith) || ColorCode.Keys.Any(LastCol.EndsWith) || ColorCode.Keys.Any(data.keyPressed.EndsWith) || ColorCode.Keys.Any(data.SymbolCol.EndsWith))
                        {
                            //var colorFirstColEndsWith = ColorCode.Keys.Any(FirstCol.EndsWith) ? ColorCode[FirstCol.Substring(FirstCol.Length - 3)] : Color.White;
                            //var colorLastColEndsWith = ColorCode.Keys.Any(LastCol.EndsWith) ? ColorCode[LastCol.Substring(LastCol.Length - 3)] : Color.White;
                            //var colorKeyPressed = ColorCode.Keys.Any(data.keyPressed.EndsWith) ? ColorCode[data.keyPressed.Substring(data.keyPressed.Length - 3)] : Color.White;
                            //var colorSymbolCol = ColorCode.Keys.Any(data.SymbolCol.EndsWith) ? ColorCode[data.SymbolCol.Substring(data.SymbolCol.Length - 3)] : Color.White;

                            //// Erase the color code at the string end
                            //FirstCol = ColorCode.Keys.Any(FirstCol.EndsWith) ? FirstCol.Substring(0, FirstCol.Length - 3) : FirstCol;
                            //LastCol = ColorCode.Keys.Any(LastCol.EndsWith) ? LastCol.Substring(0, LastCol.Length - 3) : LastCol;
                            //keyPressed = ColorCode.Keys.Any(data.keyPressed.EndsWith) ? data.keyPressed.Substring(0, data.keyPressed.Length - 3) : data.keyPressed;
                            //SymbolCol = ColorCode.Keys.Any(data.SymbolCol.EndsWith) ? data.SymbolCol.Substring(0, data.SymbolCol.Length - 3) : data.SymbolCol;

                            //hbox.Add(indicator = new Label(TextSize, hbox.RemainingHeight, keyPressed, LabelAlignment.Center));
                            //indicator.Color = colorKeyPressed;
                            //hbox.Add(indicator = new Label(colWidth, hbox.RemainingHeight, FirstCol));
                            //indicator.Color = colorFirstColEndsWith;

                            //if (data.keyPressed != null && data.keyPressed != "")
                            //{
                            //    hbox.Add(indicator = new Label(-TextSize, 0, TextSize, hbox.RemainingHeight, keyPressed, LabelAlignment.Right));
                            //    indicator.Color = colorKeyPressed;
                            //}

                            //if (data.SymbolCol != null && data.SymbolCol != "")
                            //{
                            //    hbox.Add(indicator = new Label(-(TextSize + 3), 0, TextSize, hbox.RemainingHeight, SymbolCol, LabelAlignment.Right));
                            //    indicator.Color = colorSymbolCol;
                            //}

                            //// Apply color to LastCol
                            //hbox.Add(indicator = new Label(colWidth, hbox.RemainingHeight, LastCol));
                            //indicator.Color = colorFirstColEndsWith == Color.White ? colorLastColEndsWith : colorFirstColEndsWith;
                        }
                        else
                        {   // blanck space
                            keyPressed = "";
                            hbox.Add(indicator = new Label(TextSize, hbox.RemainingHeight, keyPressed, LabelAlignment.Center));
                            indicator.Color = Color.White; // Default color

                            //Avoids troubles when the Main Scale (Windows DPI settings) is not set to 100%
                            if (FirstCol.Contains(StandardHUD? Viewer.Catalog.GetString("Time"): Viewer.Catalog.GetString("Status"))) TimeHboxPositionY = hbox.Position.Y;

                            hbox.Add(indicator = new Label(colWidth, hbox.RemainingHeight, FirstCol));
                            indicator.Color = Color.White; // Default color

                            // Font to bold
                            if (hbox.Position.Y == TimeHboxPositionY && FirstCol.Contains(Viewer.Catalog.GetString("Time"))) // Time line.
                            {
                                hbox.Add(LabelFontToBold = new Label(-colWidth, 0, data.FirstColWidth, hbox.RemainingHeight, " "));
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
            FontToBold = FontToBold ? false : true;
        }

        void ExpandWindow_Click(Control arg1, Point arg2)
        {
            StandardHUD = StandardHUD ? false : true;
            UpdateWindowSize();
        }

        private void UpdateWindowSize()
        {
            UpdateData();
            ModifyWindowSize();
        }

        /// <summary>
        /// Modify window size
        /// </summary>
        private void ModifyWindowSize()
        {
            if (ListToLabel.Count > 0)
            {
                var textwidth = Owner.TextFontDefault.Height;
                FirstColLenght = ListToLabel.Max(x => x.FirstColWidth);
                LastColLenght = ListToLabel.Max(x => x.LastColWidth);

                var desiredHeight = FontToBold? Owner.TextFontDefaultBold.Height * (ListToLabel.Count(x => x.FirstCol != null) + 2)
                    : Owner.TextFontDefault.Height * (ListToLabel.Count(x => x.FirstCol != null) + 2);

                var desiredWidth = FirstColLenght + LastColLenght + (StandardHUD? FontToBold? 30 : 35 : 60);

                var newHeight = (int)MathHelper.Clamp(desiredHeight, (StandardHUD ? WindowHeightMin : 55), WindowHeightMax);
                var newWidth = (int)MathHelper.Clamp(desiredWidth, (StandardHUD ? WindowWidthMin : 100), WindowWidthMax);

                // Move the dialog up if we're expanding it, or down if not; this keeps the center in the same place.
                var newTop = Location.Y + (Location.Height - newHeight) / 2;

                // Display window
                SizeTo(newWidth, newHeight);
                MoveTo(Location.X, newTop);
            }
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
        ///
        private void InfoToLabel(string firstkeyactivated, string firstcol, string lastcol, string symbolcol, bool changecolwidth, string lastkeyactivated)
        {
            if (!UpdateDataEnded)
            {
                var firstColWidth = 0;
                var lastColWidth = 0;

                if (!firstcol.Contains("Sprtr"))
                {
                    if (firstcol.Contains("?") || firstcol.Contains("!") || firstcol.Contains("$"))
                    {
                        firstColWidth = FontToBold ? Owner.TextFontDefaultBold.MeasureString(firstcol.Replace("?", "").Replace("!", "").Replace("$", "").TrimEnd())
                            : Owner.TextFontDefault.MeasureString(firstcol.Replace("?", "").Replace("!", "").Replace("$", "").TrimEnd());
                    }
                    else
                    {
                        firstColWidth = FontToBold ? Owner.TextFontDefaultBold.MeasureString(firstcol.TrimEnd())
                            : Owner.TextFontDefault.MeasureString(firstcol.TrimEnd());
                    }
                    if (firstcol.ToUpper().Contains("TIME"))
                    {
                        if (lastcol.Contains("?") || lastcol.Contains("!") || lastcol.Contains("$"))
                        {
                            lastColWidth = FontToBold ? Owner.TextFontDefaultBold.MeasureString(lastcol.Replace("?", "").Replace("!", "").Replace("$", "").TrimEnd())
                                : Owner.TextFontDefault.MeasureString(lastcol.Replace("?", "").Replace("!", "").Replace("$", "").TrimEnd());
                        }
                        else
                        {
                            lastColWidth = FontToBold ? Owner.TextFontDefaultBold.MeasureString(lastcol.TrimEnd())
                                : Owner.TextFontDefault.MeasureString(lastcol.TrimEnd());
                        }
                    }
                    //Set a minimum value for LastColWidth to avoid overlap between time value and clickable symbol
                    if (ListToLabel.Count == 1)
                    {
                        lastColWidth = ListToLabel.First().LastColWidth + 15;// time value + clickable symbol
                    }
                }

                ListToLabel.Add(new ListLabel
                {
                    FirstCol = firstcol,
                    FirstColWidth = firstColWidth,
                    LastCol = lastcol,
                    LastColWidth = lastColWidth,
                    SymbolCol = symbolcol,
                    ChangeColWidth = changecolwidth,
                    keyPressed = keyPressed
                });
            }
            else
            {
                //ResizeWindow, when the string spans over the right boundary of the window
                var maxFirstColWidth = ListToLabel.Max(x => x.FirstColWidth);
                var maxLastColWidth = ListToLabel.Max(x => x.LastColWidth);

                if (!ResizeWindow & (FirstColOverFlow != maxFirstColWidth || (LastColOverFlow != maxLastColWidth)))
                {
                    LastColOverFlow = maxLastColWidth;
                    FirstColOverFlow = maxFirstColWidth;
                    ResizeWindow = true;
                }
            }
        }

        private void UpdateData()
        {   //Update data
            expandWindow = '\u23FA';// ⏺ toggle window

            keyPressed = "";
            ListToLabel.Clear();
            UpdateDataEnded = false;

			// First Block
            // Client and server may have a time difference.
            keyPressed = "";
            if (StandardHUD)
            {
                if (Orts.MultiPlayer.MPManager.IsClient())
                    InfoToLabel(keyPressed, Viewer.Catalog.GetString("Time") + ": " + FormatStrings.FormatTime(Owner.Viewer.Simulator.ClockTime + Orts.MultiPlayer.MPManager.Instance().serverTimeDifference), "", "", false, keyPressed);
                else
                {
                    InfoToLabel(keyPressed, Viewer.Catalog.GetString("Time") + ": " + FormatStrings.FormatTime(Owner.Viewer.Simulator.ClockTime), "", "", false, keyPressed);
                }
            }

            // MultiPlayer
            if (Orts.MultiPlayer.MPManager.IsMultiPlayer())
            {
                var text = Orts.MultiPlayer.MPManager.Instance().GetOnlineUsersInfo();

                if (StandardHUD)
                {
                    InfoToLabel("", Viewer.Catalog.GetString("Sprtr"), "", "", false, keyPressed);
                    InfoToLabel(" ", Viewer.Catalog.GetString("MultiPlayerStatus:") + " " + (Orts.MultiPlayer.MPManager.IsServer()
                        ? Viewer.Catalog.GetString("Dispatcher") : Orts.MultiPlayer.MPManager.Instance().AmAider
                        ? Viewer.Catalog.GetString("Helper") : Orts.MultiPlayer.MPManager.IsClient()
                        ? Viewer.Catalog.GetString("Client") : ""), "", "", true, keyPressed);
                }
                else
                {
                    InfoToLabel(" ", Viewer.Catalog.GetString("Status:") + " " + (Orts.MultiPlayer.MPManager.IsServer()
                        ? Viewer.Catalog.GetString("Dispatcher") : Orts.MultiPlayer.MPManager.Instance().AmAider
                        ? Viewer.Catalog.GetString("Helper") : Orts.MultiPlayer.MPManager.IsClient()
                        ? Viewer.Catalog.GetString("Client") : ""), "", "", true, keyPressed);
                }
                // Number of player and trains
                InfoToLabel("", "NwLn", "", "", false, keyPressed);
                foreach (var t in text.Split('\t'))
                {
                    if (StandardHUD)
                    {
                        InfoToLabel(" ", (t), "", "", true, keyPressed);
                    }
                    else
                    {
                        InfoToLabel(" ", (t), "", "", true, keyPressed);
                        break;
                    }
                }
            }

            UpdateDataEnded = true;
            keyPressed = "";
            InfoToLabel(keyPressed, "", "", "", true, keyPressed);
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            var MovingCurrentWindow = UserInput.IsMouseLeftButtonDown &&
                   UserInput.MouseX >= Location.X && UserInput.MouseX <= Location.X + Location.Width &&
                   UserInput.MouseY >= Location.Y && UserInput.MouseY <= Location.Y + Location.Height ?
                   true : false;

            if (!MovingCurrentWindow & updateFull)
            {
                UpdateData();

                // Ctrl + F (FiringIsManual)
                if (ResizeWindow || LinesCount != ListToLabel.Count())
                {
                    ResizeWindow = false;
                    UpdateWindowSize();
                    LinesCount = ListToLabel.Count();
                }

                //Update Layout
                Layout();
            }
        }
    }
}