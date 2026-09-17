using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Orts.TimetableEditor.Views
{
    /// <summary>
    /// Interaktionslogik für InputBox.xaml
    /// </summary>
    public partial class InputBox : Window
    {
        public InputBox()
        {
            InitializeComponent();
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            if(Input.Text!="")
            {
                DialogResult = true;
                Close();
            }
        }

        private void HasValue_Click(object sender, RoutedEventArgs e)
        {
            if(HasValue.IsChecked==true)
            {
                InputValue.Visibility = Visibility.Visible;
            }
            else
            {
                InputValue.Visibility = Visibility.Collapsed;
            }
        }
    }
}
