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
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
            blowHornSeconds.Text = "1";

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
            else if (currentWaitTimeS == 60001)
            {
                selectJoinSplit.IsChecked = true;
            }
            else if (currentWaitTimeS == 60002)
            {
                aiRequestPassRed.IsChecked = true;
            }
            else if (currentWaitTimeS >= 60011 && currentWaitTimeS <= 60021)
            {
                int seconds = currentWaitTimeS - 60010;
                blowHornSeconds.Text = seconds.ToString(System.Globalization.CultureInfo.CurrentCulture);
                selectBlowHorn.IsChecked = true;
            }
            else
            {
                int minutes = currentWaitTimeS / 60;
                int seconds = currentWaitTimeS - 60 * minutes;
                waitTimeMinutes.Text = minutes.ToString(System.Globalization.CultureInfo.CurrentCulture);
                waitTimeSeconds.Text = seconds.ToString(System.Globalization.CultureInfo.CurrentCulture);
                selectWait.IsChecked = true;
            }

            OptionEnabling();

        }

        ///<summary>Return the selected wait time in seconds</summary>
        public int GetWaitTime()
        {

            if (selectUntil.IsChecked == true)
            {
                // coding is 3HHMM
                return 30000 +
                    100 * GetIntOrZero(untilTimeHours.Text) + GetIntOrZero(untilTimeMinutes.Text);
            }

            if (selectUncouple.IsChecked == true)
            {
                // coding is 4NNSS or 5NNSS.
                return ((keepRear.IsChecked == true) ? 50000 : 40000) +
                    100 * GetIntOrZero(uncoupleCars.Text) + GetIntOrZero(uncoupleWaitSeconds.Text);
            }

            if (selectJoinSplit.IsChecked == true)
            {
                // coding is only one number
                return 60001;
            }

            if (aiRequestPassRed.IsChecked == true)
            {
                // coding is only one number
                return 60002;
            }
            if (selectBlowHorn.IsChecked == true)
            {
                // coding is 60011 to 60021; 60021 used for American Horn Sequence
                int seconds = GetIntOrZero(blowHornSeconds.Text);
                if (seconds < 0)
                {
                    // we need to allow 0 itself (which we get from an empty string) otherwise it is not even possible to go from '1' to '2' or so.
                    seconds = 1;
                    blowHornSeconds.Text = "1";
                }
                if (seconds > 11)
                {
                    seconds = 11;
                    blowHornSeconds.Text = "11";
                }
                return 60010 + seconds;
            }

            //if (selectWait.IsChecked == true)
            {
                // default calculation
                return 60 * GetIntOrZero(waitTimeMinutes.Text) + GetIntOrZero(waitTimeSeconds.Text);
            }
        }

        int GetIntOrZero(string inputText)
        {
            int returnValue;
            try
            {
                returnValue = Convert.ToInt32(inputText, System.Globalization.CultureInfo.CurrentCulture);
            }
            catch
            {
                returnValue = 0;
            }
            return returnValue;
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
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
                ButtonOK_Click(sender, e);
            }
            UpdateWaitTime();
        }

        private void Option_CheckChanged(object sender, RoutedEventArgs e)
        {
            OptionEnabling();
        }

        private void OptionEnabling()
        {
            waitTimeMinutes.IsEnabled = false;
            waitTimeSeconds.IsEnabled = false;
            untilTimeHours.IsEnabled = false;
            untilTimeMinutes.IsEnabled = false;
            uncoupleCars.IsEnabled = false;
            uncoupleWaitSeconds.IsEnabled = false;
            blowHornSeconds.IsEnabled = false;
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

            if (selectBlowHorn.IsChecked == true)
            {
                blowHornSeconds.IsEnabled = true;
                blowHornSeconds.Focus();
            }

            UpdateWaitTime();
        }

        private void UpdateWaitTime()
        {
            try
            {
                int waitTime = GetWaitTime();
                WaitTimeDecimal.Content = String.Format("{0:D5},", waitTime);
                WaitTimeHexadecimal.Content = String.Format("{0:x4}.", waitTime);
            }
            catch { }
        }


        private void TwoDigits_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            int ww = GetIntOrZero(textBox.Text);
            if (ww > 99)
            {
                textBox.Text = "99";
            }
            UpdateWaitTime();
        }

    }
}
