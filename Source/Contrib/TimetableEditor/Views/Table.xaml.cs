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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Orts.TimetableEditor.ViewModel;

namespace Orts.TimetableEditor.Views
{
    /// <summary>
    /// Interaktionslogik für Table.xaml
    /// </summary>
    public partial class Table : UserControl
    {
        public Table()
        {
            InitializeComponent();
        }

        private void dataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (dataGrid.SelectedCells.Count > 0)
            {
                DataGridCellInfo firstcell = dataGrid.SelectedCells[0];
                var viewModel = this.DataContext as TimetableViewModel;
                viewModel.CurrentColumnIndex = firstcell.Column.DisplayIndex;
                viewModel.CurrentRowIndex = dataGrid.Items.IndexOf(firstcell.Item);
            }
        }
    }
}
