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
using Orts.MultiPlayer;

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
        const int heightInLinesOfText = 1;
        bool UpdateDataEnded = false;

        public static bool MultiplayerUpdating = false;

        const int TextSize = 15;
        public int keyPresLenght;
        public int OffSetX = 0;
        int maxFirstColWidth = 0;
        int maxLastColWidth = 0;
        int WindowHeightMax = 0;
        int WindowHeightMin = 0;
        int WindowWidthMin = 0;
        int WindowWidthMax = 0;

        string keyPressed;// display a symbol when a control key is pressed.

        Label indicator;
        Label LabelFontToBold;
        public static bool FontChanged;
        public static bool FontToBold = false;
        public static bool MonoFont;

        /// <summary>
        /// A Multiplayer row with data fields.
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
        List<ListLabel> labels = new List<ListLabel>();


        public MultiPlayerWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 10, Window.DecorationSize.Y + (owner.TextFontDefault.Height * (heightInLinesOfText)), Viewer.Catalog.GetString("MultiPlayer Info"))
        {
            WindowHeightMin = Location.Height;
            WindowHeightMax = Location.Height + owner.TextFontDefault.Height * 20; // 20 lines
            WindowWidthMin = Location.Width;
            WindowWidthMax = Location.Width + owner.TextFontDefault.Height * 20; // 20 char
        }

        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            outf.Write(Location.X);
            outf.Write(Location.Y);
            outf.Write(Location.Width);
            outf.Write(Location.Height);
        }

        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            Rectangle LocationRestore;
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
            if(!Owner.Viewer.Simulator.TimetableMode)
                UpdateWindowSize();
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            // Display main HUD data
            var vbox = base.Layout(layout).AddLayoutVertical();
            if (labels.Count > 0)
            {
                var colWidth = labels.Max(x => x.FirstColWidth) + (FontToBold ? 19 : 16);
                var TimeHboxPositionY = 0;
                foreach (var data in labels)
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
                        var SymbolCol = data.SymbolCol;

                        {   // blanck space
                            keyPressed = "";
                            hbox.Add(indicator = new Label(TextSize, hbox.RemainingHeight, keyPressed, LabelAlignment.Center));
                            indicator.Color = Color.White; // Default color

                            //Avoids troubles when the Main Scale (Windows DPI settings) is not set to 100%
                            if (FirstCol.Contains(Viewer.Catalog.GetString("Time"))) TimeHboxPositionY = hbox.Position.Y;

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

        private void UpdateWindowSize()
        {
            labels = MultiPlayerWindowList(Owner.Viewer).ToList();
            ModifyWindowSize();
        }

        /// <summary>
        /// Modify window size
        /// </summary>
        private void ModifyWindowSize()
        {
            if (labels.Count > 0)
            {
                FirstColLenght = labels.Max(x => x.FirstColWidth);
                LastColLenght = labels.Max(x => x.LastColWidth);

                // Validates rows with windows DPI settings
                var dpiOffset = (System.Drawing.Graphics.FromHwnd(IntPtr.Zero).DpiY / 96) > 1.00f ? 1 : 0;// values from testing
                var rowCount = labels.Where(x => x.FirstCol != null || x.FirstColWidth == 0).Count() - dpiOffset;
                var desiredHeight = FontToBold ? Owner.TextFontDefaultBold.Height * rowCount
                    : Owner.TextFontDefault.Height * rowCount;

                var desiredWidth = FirstColLenght + LastColLenght + 35;

                var newHeight = (int)MathHelper.Clamp(desiredHeight, WindowHeightMin, WindowHeightMax);
                var newWidth = (int)MathHelper.Clamp(desiredWidth, WindowWidthMin, WindowWidthMax);

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
        private void CheckLabel(ref ListLabel label)
        {
            void CheckString(ref string s) => s = s ?? "";
            CheckString(ref label.FirstCol);
            CheckString(ref label.LastCol);
            CheckString(ref label.SymbolCol);
            CheckString(ref label.KeyPressed);

            UpdateColsWidth(label);
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
        private void UpdateColsWidth(ListLabel label)
        {
            if (!UpdateDataEnded)
            {
                var firstColWidth = 0;
                var lastColWidth = 0;
                var firstCol = label.FirstCol;
                var lastCol = label.LastCol;
                var symbolCol = label.SymbolCol;
                var keyPressed = label.KeyPressed;
                var changeColwidth = label.ChangeColWidth;

                if (!firstCol.Contains("Sprtr"))
                {
                    firstColWidth = FontToBold ? Owner.TextFontDefaultBold.MeasureString(firstCol.TrimEnd())
                        : Owner.TextFontDefault.MeasureString(firstCol.TrimEnd());

                    if (firstCol.ToUpper().Contains("TIME"))
                    {
                        lastColWidth = FontToBold ? Owner.TextFontDefaultBold.MeasureString(lastCol.TrimEnd())
                        : Owner.TextFontDefault.MeasureString(lastCol.TrimEnd());
                    }
                    //Set a minimum value for LastColWidth to avoid overlap between time value
                    if (labels.Count == 1)
                    {
                        lastColWidth = labels.First().LastColWidth + 15;// time value +  symbol
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
        }

        /// <summary>
        /// Retrieve a formatted list <see cref="ListLabel"/>s to be displayed as an in-browser Track Monitor.
        /// </summary>
        /// <param name="viewer">The Viewer to read train data from.</param>
        /// <returns>A list of <see cref="ListLabel"/>s, one per row of the popup.</returns>
        public IEnumerable<ListLabel> MultiPlayerWindowList(Viewer viewer)
        {
            //Update data
            keyPressed = "";
            labels = new List<ListLabel>();
            void AddLabel(ListLabel label)
            {
                CheckLabel(ref label);
            }
            void AddSeparator() => AddLabel(new ListLabel
            {
                FirstCol = "Sprtr",
            });

            labels.Clear();
            UpdateDataEnded = false;
            // First Block
            // Client and server may have a time difference.
            var time = FormatStrings.FormatTime(viewer.Simulator.ClockTime + (MultiPlayer.MPManager.IsClient() ? MultiPlayer.MPManager.Instance().serverTimeDifference : 0));
            AddLabel(new ListLabel
            {
                FirstCol = $"{Viewer.Catalog.GetString("Time")}: {time}",
                LastCol = ""
            });

            // Separator
            AddSeparator();

            // MultiPlayer
            if (Orts.MultiPlayer.MPManager.IsMultiPlayer())
            {
                var text = Orts.MultiPlayer.MPManager.Instance().GetOnlineUsersInfo();
                var multiPlayerStatus = Orts.MultiPlayer.MPManager.IsServer()
                    ? $"{Viewer.Catalog.GetString("Dispatcher")} ({Orts.MultiPlayer.MPManager.Server.UserName})" : Orts.MultiPlayer.MPManager.Instance().AmAider
                    ? Viewer.Catalog.GetString("Helper") : Orts.MultiPlayer.MPManager.IsClient()
                    ? $"{Viewer.Catalog.GetString("Client")} ({Orts.MultiPlayer.MPManager.Client.UserName})" : "";

                var status = $"{Viewer.Catalog.GetString("Status")}: {multiPlayerStatus}";

                AddLabel(new ListLabel
                {
                    FirstCol = status,
                    LastCol = ""
                });

                AddLabel(new ListLabel());

                // Number of player and trains
                foreach (var t in text.Split('\t'))
                {
                    AddLabel(new ListLabel
                    {
                        FirstCol = $"{t}",
                        LastCol = ""
                    });
                }
                AddLabel(new ListLabel());
            }
            else if (Orts.MultiPlayer.MPManager.Simulator.Confirmer != null)
            {
                var status = $"{Viewer.Catalog.GetString("Status")}: {MPManager.Catalog.GetString("Connection to the server is lost, will play as single mode")}";
                AddLabel(new ListLabel
                {
                    FirstCol = status,
                    LastCol = ""
                });

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
            if (!MovingCurrentWindow && !MultiplayerUpdating && updateFull)
            {
                MultiplayerUpdating = true;
                labels = MultiPlayerWindowList(Owner.Viewer).ToList();
                MultiplayerUpdating = false;

                // Ctrl + F (FiringIsManual)
                if (ResizeWindow || LinesCount != labels.Count())
                {
                    ResizeWindow = false;
                    UpdateWindowSize();
                    LinesCount = labels.Count();
                }
                //Resize this window after the font has been changed externally
                if (TrainDrivingWindow.FontChanged)
                {
                    TrainDrivingWindow.FontChanged = false;
                    FontToBold = !FontToBold;
                    UpdateWindowSize();
                }
                //Update Layout
                Layout();
            }
        }
    }
}