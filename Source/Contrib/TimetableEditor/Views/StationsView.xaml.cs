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
    /// Interaktionslogik für StationsView.xaml
    /// </summary>
    public partial class StationsView : Window
    {
        public StationsView()
        {
            InitializeComponent();
        }
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
