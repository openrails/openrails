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
using Orts.TimetableEditor.ViewModel;

namespace Orts.TimetableEditor.Views
{
    /// <summary>
    /// Interaktionslogik für PoolView.xaml
    /// </summary>
    public partial class PoolView : Window
    {
        public PoolView()
        {
            InitializeComponent();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void PathListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            PoolViewModel pvm = this.DataContext as PoolViewModel;
            if(pvm.SelectedTrainPath!=null)
            {
                pvm.SelectedItem.Value = pvm.SelectedTrainPath.Name;
            }
        }
    }
}
