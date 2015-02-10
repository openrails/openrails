// COPYRIGHT 2014, 2015 by the Open Rails project.
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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;

namespace ORTS.TrackViewer.Editing
{
    /// <summary>
    /// Interaction logic for WaitPointDialog.xaml
    /// This is a small dialog to edit the details for waitpoint
    /// </summary>
    public sealed partial class WaitPointDialog : Window
    {
        /// <summary>
        /// Create the dialog to edit the metadata of the waitpoint dialog.
        /// </summary>
        /// <param name="mouseX">Current X-location of the mouse to determine popup location</param>
        /// <param name="mouseY">Current Y-location of the mouse to determine popu location</param>
        /// <param name="currentWaitTimeS">Current value of the wait time, might encode openrails advanced shunting</param>
        public WaitPointDialog(int mouseX, int mouseY, int currentWaitTimeS)
        {
            InitializeComponent();
            this.Left = mouseX;
            this.Top = mouseY;

            untilTimeHours.Text = "1";
            untilTimeMinutes.Text = "1";
            uncoupleCars.Text = "1";
            uncoupleWaitSeconds.Text = "1";
            waitTimeMinutes.Text = "1";
            waitTimeSeconds.Text = "1";
            
            if (currentWaitTimeS >= 30000 && currentWaitTimeS < 40000)
            {
                // Absolute time to wait until. 
                // waitTimeS (in decimal notation) = 3HHMM  (hours and minutes)
                int hours = (currentWaitTimeS / 100) % 100;
                int minutes = currentWaitTimeS % 100;
                untilTimeHours.Text = hours.ToString(System.Globalization.CultureInfo.CurrentCulture);
                untilTimeMinutes.Text = minutes.ToString(System.Globalization.CultureInfo.CurrentCulture);
                selectUntil.IsChecked = true;
            }
            else if (currentWaitTimeS >= 40000 && currentWaitTimeS < 60000)
            {
                int nCars = (currentWaitTimeS / 100) % 100;
                int seconds = currentWaitTimeS % 100;
                uncoupleCars.Text = nCars.ToString(System.Globalization.CultureInfo.CurrentCulture);
                uncoupleWaitSeconds.Text = seconds.ToString(System.Globalization.CultureInfo.CurrentCulture);
                selectUncouple.IsChecked = true;
                keepRear.IsChecked = (currentWaitTimeS >= 50000);
            }
            else
            {
                int minutes = currentWaitTimeS / 60;
                int seconds = currentWaitTimeS - 60 * minutes;
                waitTimeMinutes.Text = minutes.ToString(System.Globalization.CultureInfo.CurrentCulture);
                waitTimeSeconds.Text = seconds.ToString(System.Globalization.CultureInfo.CurrentCulture);
                selectWait.IsChecked = true;
            }

            optionEnabling();

        }

        ///<summary>Return the selected wait time in seconds</summary>
        public int GetWaitTime()
        {
            
            

            if (selectUntil.IsChecked == true)
            {
                // coding is 3HHMM
                return 30000 +
                    100 * Convert.ToInt32(untilTimeHours.Text, System.Globalization.CultureInfo.CurrentCulture)
                        + Convert.ToInt32(untilTimeMinutes.Text, System.Globalization.CultureInfo.CurrentCulture);
            }

            if (selectUncouple.IsChecked == true)
            {
                // coding is 4NNSS or 5NNSS. 5NNSS is supported in an hidden way only, because it might change in the future
                return ((keepRear.IsChecked == true) ? 50000 : 40000) +
                    100 * Convert.ToInt32(uncoupleCars.Text, System.Globalization.CultureInfo.CurrentCulture)
                        + Convert.ToInt32(uncoupleWaitSeconds.Text, System.Globalization.CultureInfo.CurrentCulture);
            }

            //if (selectWait.IsChecked == true)
            {
                // default calculation
                return 60 * Convert.ToInt32(waitTimeMinutes.Text, System.Globalization.CultureInfo.CurrentCulture)
                          + Convert.ToInt32(waitTimeSeconds.Text, System.Globalization.CultureInfo.CurrentCulture);
            }
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
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
                buttonOK_Click(sender, e);
            }
        }

        private void option_CheckChanged(object sender, RoutedEventArgs e)
        {
            optionEnabling();
        }

        private void optionEnabling()
        {
            waitTimeMinutes.IsEnabled = false;
            waitTimeSeconds.IsEnabled = false;
            untilTimeHours.IsEnabled = false;
            untilTimeMinutes.IsEnabled = false;
            uncoupleCars.IsEnabled = false;
            uncoupleWaitSeconds.IsEnabled = false;
            keepRear.IsEnabled = false;

            if (selectWait.IsChecked == true)
            {
                waitTimeMinutes.IsEnabled = true;
                waitTimeSeconds.IsEnabled = true;
                waitTimeMinutes.Focus();
            }

            if (selectUntil.IsChecked == true)
            {
                untilTimeHours.IsEnabled = true;
                untilTimeMinutes.IsEnabled = true;
                untilTimeMinutes.Focus();
            }

            if (selectUncouple.IsChecked == true)
            {
                uncoupleWaitSeconds.IsEnabled = true;
                uncoupleCars.IsEnabled = true;
                keepRear.IsEnabled = true;
                uncoupleWaitSeconds.Focus();
            }
        }
 
    }
}
