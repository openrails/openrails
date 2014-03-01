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
    public partial class UncouplePointDialog : Window
    {
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

        public int GetNCars()
        {
            if ((bool)selectUncouple.IsChecked)
            {
                return - Convert.ToInt32(Ncars.Text);
            }
            else
            {
                return Convert.ToInt32(Ncars.Text);
            }
            
        }
        public int GetWaitTime()
        {
            return Convert.ToInt32(waitTimeS.Text);
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
