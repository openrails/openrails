// COPYRIGHT 2018 by the Open Rails project.
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
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;

using ORTS.Common;
using ORTS.TrackViewer.UserInterface;

namespace ORTS.TrackViewer.Drawing.Labels
{
    /// <summary>
    /// Labels are just text that can be added to a track for making it easier for a developer to show certain things on his route,
    /// other than platforms, stations or sidings. They can be placed at will. But they do not affect at all the ORTS simulation.
    /// They are really stand-alone.
    /// The list of labels can be saved or loaded to .json. This will not be done automatically.
    /// The labels can be modified either w.r.t. to their location (dragging) or w.r.t. their text.
    /// </summary>
    public class DrawLabels
    {
        #region private fields
        /// <summary>This is the list of labels that we need to draw and possibly modify </summary>
        private StorableLabels labels = new StorableLabels();

        /// <summary>The area we are drawing on</summary>
        private DrawArea drawArea;
        /// <summary></summary>
        private int fontHeight;
        /// <summary>The label that is currently closest to the mouse and hence is to be used for dragging or editing</summary>
        private StorableLabel closestToMouseLabel;

        //Dragging
        /// <summary>The label that is to be replaced</summary>
        private StorableLabel draggingLabelToReplace;
        /// <summary>The label that we are dragging, so this is constantly updated</summary>
        private StorableLabel draggingLabel;
        /// <summary>The MSTS location where the mouse started during dragging</summary>
        private WorldLocation draggingStartLocation;
        /// <summary>Are we currently dragging?</summary>
        private bool dragging = false;   // draggingLabelToReplace is not nullable, so we cannot use that

        private TrackViewer TrackViewer;

        private ContextMenu ContextMenu;

        private MenuItem EditLabelMenuItem;

        public MenuItem SetLocationMenuItem;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fontHeight">The height of the font so we can correct during drawing</param>
        public DrawLabels(TrackViewer trackViewer, int fontHeight)
        {
            this.fontHeight = fontHeight;
            TrackViewer = trackViewer;

            CreateContextMenu();
        }
        #endregion

        #region Drawing
        /// <summary>
        /// Draw all the available labels onto the screen (drawArea).
        /// </summary>
        /// <param name="drawArea">The area we are drawing on</param>
        public void Draw(DrawArea drawArea)
        {
            this.drawArea = drawArea;
            if (!Properties.Settings.Default.showLabels) return;
            float closestDistanceSquared = float.MaxValue;

            foreach (StorableLabel label in labels.Labels)
            {
                if (!dragging || !label.Equals(draggingLabelToReplace))
                {
                    DrawLabel(label);
                }
                float distanceSquared = WorldLocation.GetDistanceSquared2D(label.WorldLocation, drawArea.MouseLocation);
                if (distanceSquared < closestDistanceSquared)
                {
                    closestDistanceSquared = distanceSquared;
                    closestToMouseLabel = label;
                }
            }

            if (dragging)
            {
                DrawLabel(draggingLabel);
            }
        }

        /// <summary>
        /// Helper routine to just draw one label
        /// </summary>
        /// <param name="label">The lable to draw</param>
        private void DrawLabel(StorableLabel label)
        {
            drawArea.DrawExpandingString(label.WorldLocation, label.LabelText, 0, -fontHeight / 2);
        }

        #endregion

        #region New and Editing
        /// <summary>
        /// Call this when adding a new label
        /// </summary>
        /// <param name="mouseX">Current X-location of the mouse to determine popup location</param>
        /// <param name="mouseY">Current Y-location of the mouse to determine popu location</param>
        internal void AddLabel(int mouseX, int mouseY)
        {
            var labelInputPopup = new EditLabel("<label>", mouseX, mouseY,
                (newLabelText) => labels.Add(drawArea.MouseLocation, newLabelText),
                allowDelete: false);
            TrackViewer.Localize(labelInputPopup);
            labelInputPopup.ShowDialog();
        }

        private void CreateContextMenu()
        {
            ContextMenu = new ContextMenu();
            EditLabelMenuItem = new MenuItem
            {
                Header = "Edit label",
                IsCheckable = false
            };
            EditLabelMenuItem.Click += new RoutedEventHandler((sender, e) => ModifyLabel(closestToMouseLabel,
                TrackViewer.Window.ClientBounds.Left + TVUserInput.MouseLocationX,
                TrackViewer.Window.ClientBounds.Left + TVUserInput.MouseLocationY));

            ContextMenu.Items.Add(EditLabelMenuItem);

            SetLocationMenuItem = new MenuItem() { Header = "View scene here" };
            SetLocationMenuItem.Click += new RoutedEventHandler((sender, e) => TrackViewer.menuControl.MenuSceneWindow_Click(sender, e));
            SetLocationMenuItem.Click += new RoutedEventHandler(async (sender, e) => await TrackViewer.SceneView?.SetCameraLocation(
                SetLocationMenuItem.CommandParameter as WorldLocation? ?? new WorldLocation()));
            ContextMenu.Items.Add(SetLocationMenuItem);
        }

        /// <summary>
        /// Popup a context menu that allows you to edit the text of a label
        /// </summary>
        /// <param name="mouseX">Current X-location of the mouse to determine popup location</param>
        /// <param name="mouseY">Current Y-location of the mouse to determine popup location</param>
        internal void PopupContextMenu(int mouseX, int mouseY, WorldLocation mouseLocation)
        {
            if (!Properties.Settings.Default.showLabels || labels.Labels.Count() == 0)
                EditLabelMenuItem.Visibility = Visibility.Collapsed;
            else
                EditLabelMenuItem.Visibility = Visibility.Visible;

            SetLocationMenuItem.CommandParameter = mouseLocation;

            var visible = false;
            foreach (MenuItem item in ContextMenu.Items)
                visible |= item.Visibility == Visibility.Visible;

            if (visible)
            {
                ContextMenu.PlacementRectangle = new Rect((double)mouseX, (double)mouseY, 20, 20);
                ContextMenu.IsOpen = true;
            }
        }

        /// <summary>
        /// Callback that is called when the user clicks on the menuitem connected to a new label
        /// </summary>
        /// <param name="oldLabel">The old label that needs to be replaced</param>
        /// <param name="mouseX">Current X-location of the mouse to determine popup location</param>
        /// <param name="mouseY">Current Y-location of the mouse to determine popu location</param>
        private void ModifyLabel(StorableLabel oldLabel, int mouseX, int mouseY)
        {
            var labelInputPopup = new EditLabel(oldLabel.LabelText, mouseX, mouseY,
                (newLabelText) =>
                {
                    if (newLabelText == null)
                    {
                        labels.Delete(oldLabel);
                    }
                    else
                    {
                        var newLabel = new StorableLabel(oldLabel.WorldLocation, newLabelText);
                        labels.Replace(oldLabel, newLabel);
                    }
                }, allowDelete: true);
            TrackViewer.Localize(labelInputPopup);
            labelInputPopup.ShowDialog();
        }

        #endregion

        #region Saving and Loading
        /// <summary>
        /// Ask the user for a filename of a .json file and save all the labels to that file
        /// </summary>
        internal void SaveLabels()
        {
            string filename = GetSaveFileName();
            if (filename == string.Empty) return;
            WriteJson(filename);
        }

        /// <summary>
        /// Ask the user for a filename to be used for saving
        /// </summary>
        /// <returns>Either the filename or empty when cancelled by the user</returns>
        private string GetSaveFileName() {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                OverwritePrompt = true,
                FileName = "labels.json",
                DefaultExt = ".json",
                Filter = "JSON Files (.json)|*.json"
            };
            return (dialog.ShowDialog() == true) ? dialog.FileName : string.Empty;
        }

        /// <summary>
        /// Save all the labels to the given .json file
        /// </summary>
        /// <param name="fileName">The filename to be used for writing</param>
        private void WriteJson(string fileName)
        {
            JsonSerializer serializer = new JsonSerializer
            {
                //PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                //TypeNameHandling = TypeNameHandling.All,
                Formatting = Formatting.Indented
            };
            using (StreamWriter wr = new StreamWriter(fileName))
            {
                using (JsonWriter writer = new JsonTextWriter(wr))
                {
                    serializer.Serialize(writer, this.labels);
                }
            }
        }

        /// <summary>
        /// Ask the user for a filename to be used for loading and load all the labels from that file
        /// </summary>
        internal void LoadLabels()
        {
            string filename = GetLoadFileName();
            if (filename == string.Empty) return;
            LoadJson(filename);
        }

        /// <summary>
        /// Ask the user for a filename to be used for loading
        /// </summary>
        /// <returns>Either the filename or empty when cancelled by the user</returns>
        private string GetLoadFileName() {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                FileName = "labels.json",
                DefaultExt = ".json",
                Filter = "JSON Files (.json)|*.json"
            };
            return (dialog.ShowDialog() == true) ? dialog.FileName : string.Empty;
        }

        /// <summary>
        /// Load all the labels from the given .json file
        /// </summary>
        /// <param name="fileName">The filename to be used for reading</param>
        private void LoadJson(string fileName)
        {
            StorableLabels labelsNew = null;
            try
            {
                labelsNew = JsonConvert.DeserializeObject<StorableLabels>(File.ReadAllText(fileName));
            }
            catch
            {
                MessageBox.Show(TrackViewer.catalog.GetString("The .json file could not be read properly."));
                return;
            }

            bool itemsWereRemoved = labelsNew.Sanitize();
            int itemsLeft = labelsNew.Labels.Count();
            string message = string.Empty;
            if (itemsLeft > 0)
            {
                labels = labelsNew;
                if (itemsWereRemoved)
                {
                    message = TrackViewer.catalog.GetString("Some labels could not be read properly.");
                }
            }
            else
            {
                if (itemsWereRemoved)
                {
                    message = TrackViewer.catalog.GetString("None of the labels could be read properly");
                }
                else
                {
                    message = TrackViewer.catalog.GetString("There were no labels read");
                }
            }

            if (message != string.Empty)
            {
                MessageBox.Show(message);
            }

        }
        #endregion

        #region Dragging
        /// <summary>
        /// Initiate dragging
        /// </summary>
        internal void OnLeftMouseClick()
        {
            draggingStartLocation = drawArea.MouseLocation;
            draggingLabelToReplace = closestToMouseLabel;
            dragging = true;
        }

        /// <summary>
        /// Do the dragging itself, meaning moving the label to a different position and show that
        /// </summary>
        internal void OnLeftMouseMoved()
        {
            if (!dragging) return;

            //We have three locations. Where the original/reference label. Where the dragging starts, and where the mouse now is
            //The new location is then 'original' + 'current' - 'start'.
            draggingStartLocation.NormalizeTo(drawArea.MouseLocation.TileX, drawArea.MouseLocation.TileZ);
            WorldLocation shiftedLocation = new WorldLocation(
                draggingLabelToReplace.WorldLocation.TileX + drawArea.MouseLocation.TileX - draggingStartLocation.TileX,
                draggingLabelToReplace.WorldLocation.TileZ + drawArea.MouseLocation.TileZ - draggingStartLocation.TileZ,
                draggingLabelToReplace.WorldLocation.Location.X + drawArea.MouseLocation.Location.X - draggingStartLocation.Location.X,
                0,
                draggingLabelToReplace.WorldLocation.Location.Z + drawArea.MouseLocation.Location.Z - draggingStartLocation.Location.Z
                );
            shiftedLocation.Normalize();
            draggingLabel = new StorableLabel(shiftedLocation, draggingLabelToReplace.LabelText);
        }

        /// <summary>
        /// Finish the dragging, and updated the dragged label
        /// </summary>
        internal void OnLeftMouseRelease()
        {
            if (!dragging) return;
            // replace the reference label with the new dragging one
            labels.Replace(draggingLabelToReplace, draggingLabel);
            dragging = false;
        }

        /// <summary>
        /// Cancel the dragging, so do not update the label
        /// </summary>
        internal void OnLeftMouseCancel()
        {
            dragging = false;
        }

        #endregion
    }
}
