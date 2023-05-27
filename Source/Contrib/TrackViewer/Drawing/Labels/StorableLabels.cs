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
using Newtonsoft.Json;

using ORTS.Common;

namespace ORTS.TrackViewer.Drawing.Labels
{
    /// <summary>
    /// This is a data structure to store user-labels that can be shown on a track.
    /// The data must be easy to read and write from file
    /// </summary>
    class StorableLabels
    {
        /// <summary> The 'list' of labels </summary>
        [JsonProperty("LabelList")]
        public IEnumerable<StorableLabel> Labels { get { return _labels; } }

        /// <summary> The internal list of labels </summary>
        [JsonIgnore]
        private List<StorableLabel> _labels = new List<StorableLabel>();

        /// <summary>
        /// Constructor
        /// </summary>
        public StorableLabels() { }

        /// <summary>
        /// Add a label
        /// </summary>
        /// <param name="location">The location in MSTS coordinates of the label</param>
        /// <param name="text">The text of the label</param>
        public void Add(WorldLocation location, string text)
        {
            var newLabel = new StorableLabel(location, text);
            _labels.Add(newLabel);
        }

        /// <summary>
        /// Replace an existing label with a new one in the same place. This acts as a modification of the label which itself
        /// is not possible since labels are immutable
        /// </summary>
        /// <param name="oldLabel">The label to be replaced (should already be in the list of labels</param>
        /// <param name="newLabel">The new lable to put in the list</param>
        internal void Replace(StorableLabel oldLabel, StorableLabel newLabel)
        {
            int index = _labels.IndexOf(oldLabel);
            if (index != -1)
            {
                _labels[index] = newLabel;
            }
        }

        /// <summary>
        /// Remove all labels that either have no text set or have no worldlocation set
        /// </summary>
        /// <returns>Wether items were removed</returns>
        internal bool Sanitize()
        {
            int itemsRemoved = _labels.RemoveAll((label) => (label.LabelText == null || label.WorldLocation == WorldLocation.None));
            return (itemsRemoved > 0);
        }

        /// <summary>
        /// Remove a label from the list
        /// </summary>
        /// <param name="oldLabel">the label to remove</param>
        internal void Delete(StorableLabel oldLabel)
        {
            if (_labels.Contains(oldLabel))
            {
                _labels.Remove(oldLabel);

            }
        }
    }

    #region StorableLabel
    /// <summary>
    /// Struct to store a single label that can be drawn upon the track
    /// </summary>
    [Serializable]
    public struct StorableLabel
    {
        /// <summary> The text of the label </summary>
        [JsonProperty("LabelText")]
        public string LabelText;
        /// <summary> The MSTS location of the label </summary>
        [JsonProperty("WorldLocation")]
        public WorldLocation WorldLocation { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="location">The location in MSTS coordinates of the label</param>
        /// <param name="text">The text of the label</param>
        public StorableLabel(WorldLocation location, string text)
        {
            LabelText = text;
            WorldLocation = location;
        }

    }
    #endregion
}
