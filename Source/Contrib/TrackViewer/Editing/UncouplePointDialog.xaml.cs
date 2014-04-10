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
    /// Interaction logic for UncouplePointDialog.xaml
    /// </summary>
    public sealed partial class UncouplePointDialog : Window
    {
        /// <summary>Return the selected wait-times (in seconds)</summary>
        public int GetWaitTime
        {
            get
            {
                return Convert.ToInt32(waitTimeS.Text, System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        /// <summary> Return the Number of Cars to couple/uncouple that has been selected (negative for uncouple)</summary>
        public int GetNCars
        {
            get
            {
                if ((bool)selectUncouple.IsChecked)
                {
                    return -Convert.ToInt32(Ncars.Text, System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    return Convert.ToInt32(Ncars.Text, System.Globalization.CultureInfo.InvariantCulture);
                }
            }
        }

        /// <summary>
        /// Create the Dialog to edit the details of an (un)couple point
        /// </summary>
        /// <param name="mouseX">x-location of the mouse</param>
        /// <param name="mouseY">y-location of the mouse</param>
        /// <param name="currentNcars">Current value of number of cards to couple (negative for uncouple)</param>
        /// <param name="currentWaitTimeS">Current wait time</param>
        public UncouplePointDialog(int mouseX, int mouseY, int currentNcars, int currentWaitTimeS)
        {
            InitializeComponent();
            this.Left = mouseX;
            this.Top = mouseY;
            Ncars.Focus();
            if (currentNcars > 0)
            {
                selectCouple.IsChecked = true;
                Ncars.Text = currentNcars.ToString();
            }
            else
            {
                selectUncouple.IsChecked = true;
                Ncars.Text = (-currentNcars).ToString();
            }
            waitTimeS.Text = currentWaitTimeS.ToString();
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

    }
}
