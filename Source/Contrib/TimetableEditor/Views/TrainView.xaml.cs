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
using Orts.TimetableEditor.Model;
using Orts.TimetableEditor.ViewModel;

namespace Orts.TimetableEditor.Views
{
    /// <summary>
    /// Interaktionslogik für TrainView.xaml
    /// </summary>
    public partial class TrainView : Window
    {
        public TrainView()
        {
            InitializeComponent();
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Consists_ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TrainViewModel trainViewModel = this.DataContext as TrainViewModel;
            if(trainViewModel.MultiConsist==false)
            {
                trainViewModel.Con.Clear();
            }
            string conname = trainViewModel.SelectedConsist.FileName;
            if (conname.Contains("+"))
            {
                conname = "<" + conname + ">";
            }
            trainViewModel.Con.Add(new Consist { Name = conname, Reversed = false });

        }

        private void PathListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TrainViewModel trainViewModel = this.DataContext as TrainViewModel;
            if (trainViewModel.SelectedTrainPath != null)
            {
                trainViewModel.ChosenTrainPath = trainViewModel.SelectedTrainPath.Name;
            }
        }
    }
}
