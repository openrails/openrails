// COPYRIGHT 2014 by the Open Rails project.
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
//
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace ORTS.TrackViewer.UserInterface
{
    /// <summary>
    /// The kind of items that can be searched from the search control
    /// </summary>
    public enum SearchableItem
    {
        /// <summary>Search for (rail) track node</summary>
        TrackNode,
        /// <summary>Search for (rail) track item</summary>
        TrackItem,
        /// <summary>Search for road track node</summary>
        TrackNodeRoad,
        /// <summary>Search for road track item</summary>
        TrackItemRoad,
    }

    /// <summary>
    /// Interaction logic for SearchWindow.xaml
    /// This contains the callbacks needed for the (small) searchwindow/searchcontrol
    /// </summary>
    public sealed partial class SearchControl : Window
    {
        TrackViewer trackViewer;
        SearchableItem searchItem;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="trackViewer">Trackviewer so we can perform a callback</param>
        /// <param name="searchItem">Type of item that needs to be searched</param>
        public SearchControl(TrackViewer trackViewer, SearchableItem searchItem)
        {
            InitializeComponent();
            this.trackViewer = trackViewer;
            this.searchItem = searchItem;
            textboxIndex.Focus();
            this.Top = trackViewer.Window.ClientBounds.Top + 20;
            this.Left = trackViewer.Window.ClientBounds.Left + 0;
            switch (searchItem)
            {
                case SearchableItem.TrackNode:
                    labelType.Content = "trackNode index = ";
                    break;
                case SearchableItem.TrackItem:
                    labelType.Content = "trackItem index = ";
                    break;
                case SearchableItem.TrackNodeRoad:
                    labelType.Content = "road trackNode index = ";
                    break;
                case SearchableItem.TrackItemRoad:
                    labelType.Content = "road trackItem index = ";
                    break;
            }
        }

        /// <summary>
        /// If the search button has been clicked, validate the input and call the trackViewewer callback
        /// </summary>
        private void ButtonSearch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int Index = Convert.ToInt32(textboxIndex.Text, System.Globalization.CultureInfo.CurrentCulture);
                switch (searchItem)
                {
                    case SearchableItem.TrackNode:
                        trackViewer.CenterAroundTrackNode(Index);
                        break;
                    case SearchableItem.TrackItem:
                        trackViewer.CenterAroundTrackItem(Index);
                        break;
                    case SearchableItem.TrackNodeRoad:
                        trackViewer.CenterAroundTrackNodeRoad(Index);
                        break;
                    case SearchableItem.TrackItemRoad:
                        trackViewer.CenterAroundTrackItemRoad(Index);
                        break;

                }
            }
            catch
            {   // clear text if not an integer. Should not happen because we limt possible input
                textboxIndex.Text = "";
            }
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        /// <summary>
        /// Make sure we only allow digits to be typed
        /// </summary>
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        /// <summary>
        /// Handle an enter/return press on the textbox. If return is pressed, just do the same as the search button.
        /// </summary>
        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                ButtonSearch_Click(sender, e);
            }
        }
    }
}
