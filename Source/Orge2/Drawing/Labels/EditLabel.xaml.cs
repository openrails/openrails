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
using System.Windows;
using System.Windows.Input;

namespace ORTS.Orge.Drawing.Labels
{
    /// <summary>
    /// Interaction logic for EditLabel.xaml
    /// </summary>
    public partial class EditLabel : Window
    {
        private Action<string> callback;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="initialText">The initial text to show</param>
        /// <param name="mouseX">Current X-location of the mouse to determine popup location</param>
        /// <param name="mouseY">Current Y-location of the mouse to determine popu location</param>
        /// <param name="newLabelTextCallback">The callback that will be called with a new or updated text. In case of a delete, the callback will be called with 'null'</param>
        /// <param name="allowDelete">Should the delete button be present?</param>
        public EditLabel(string initialText, int mouseX, int mouseY, Action<string> newLabelTextCallback, bool allowDelete)
        {
            callback = newLabelTextCallback;
            InitializeComponent();
            Left = mouseX;
            Top = mouseY;
            textboxLabel.Text = initialText;
            textboxLabel.Focus();
            textboxLabel.SelectAll();
            if (!allowDelete) { buttonDelete.Visibility = Visibility.Hidden; }
        }

        /// <summary>
        /// The user clicked OK, so execute the callback that was registered
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            callback(textboxLabel.Text);
        }

        /// <summary>
        /// Cancel the editing, which just means closing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        
        /// <summary>
        /// Delete the label
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            callback(null);
        }
        
        /// <summary>
        /// Handle an enter/return press on the textbox. If return is pressed, just do the same as the save button.
        /// </summary>
        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                ButtonOK_Click(sender, e);
            }
        }

    }
}
