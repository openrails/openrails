// COPYRIGHT 2010, 2011, 2012 by the Open Rails project.
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

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MSTS;
using ORTS.Menu;
using Path = ORTS.Menu.Path;

namespace ORTS
{
	public partial class ExploreForm : Form
	{
		readonly Folder Folder;
		readonly Route Route;
		readonly ExploreActivity ExploreActivity;

        List<Path> Paths = new List<Path>();
		List<Consist> Consists = new List<Consist>();
        Task<List<Path>> PathLoader;
		Task<List<Consist>> ConsistLoader;
        private ListViewColumnSorter lvwColumnSorter;

		public ExploreForm(Folder folder, Route route, ExploreActivity exploreActivity)
		{
			InitializeComponent();
            lvwColumnSorter = new ListViewColumnSorter();
            ConsistsListView.ListViewItemSorter = lvwColumnSorter;

			// Windows 2000 and XP should use 8.25pt Tahoma, while Windows
			// Vista and later should use 9pt "Segoe UI". We'll use the
			// Message Box font to allow for user-customizations, though.
			Font = SystemFonts.MessageBoxFont;

			Folder = folder;
			Route = route;
			ExploreActivity = exploreActivity;

			LoadPaths();

			LoadConsists();

			listSeason.SelectedIndex = exploreActivity.Season;
			listWeather.SelectedIndex = exploreActivity.Weather;
			numericHour.Value = exploreActivity.StartHour;
			numericMinute.Value = exploreActivity.StartMinute;
		}

		public ExploreActivity NewExploreActivity
		{
			get
			{
                return new ExploreActivity(listPaths.SelectedIndex >= 0 ? Paths[listPaths.SelectedIndex] : null, FindConsistFromIndex(), listSeason.SelectedIndex, listWeather.SelectedIndex, (int)numericHour.Value, (int)numericMinute.Value);
			}
		}

        //find consists from the list's selected index
        Consist FindConsistFromIndex()
        {
            if (this.ConsistsListView.SelectedIndices[0] < 0) return null;

            var conName = this.ConsistsListView.SelectedItems[0].SubItems[1].Text;
            var index = Consists.FindIndex(c => c.Name.Replace('\t', ' ') == conName);
            if (index >= 0) return Consists[index];
            return null;
        }

		void LoadPaths()
		{
			if (PathLoader != null)
				PathLoader.Cancel();

			listPaths.Items.Clear();
            buttonOk.Enabled = false;
			var route = Route;
			var exploreActivity = ExploreActivity;
            PathLoader = new Task<List<Path>>(this, () => Path.GetPaths(route).OrderBy(p => p.ToString()).ToList(), (paths) =>
			{
				Paths = paths;
				foreach (var path in Paths)
					listPaths.Items.Add(path);
                var selectionIndex = exploreActivity.Path != null ? Paths.FindIndex(p => p.FilePath == exploreActivity.Path.FilePath) : -1;
                if (selectionIndex >= 0)
                    listPaths.SelectedIndex = selectionIndex;
                else if (Paths.Count > 0)
                    listPaths.SelectedIndex = 0;
                else
                    listPaths.ClearSelected();
                buttonOk.Enabled = listPaths.Items.Count > 0 && ConsistsListView.Items.Count > 0;
            });
		}

		void LoadConsists()
		{
			if (ConsistLoader != null)
				ConsistLoader.Cancel();

            this.ConsistsListView.Items.Clear();
            buttonOk.Enabled = false;
            var folder = Folder;
			var exploreActivity = ExploreActivity;
			ConsistLoader = new Task<List<Consist>>(this, () => Consist.GetConsists(folder).OrderBy(c => c.ToString()).ToList(), (consists) =>
			{
				Consists = consists;
                foreach (var consist in Consists)
                {
                    if (ConsistsHasEngine(consist))
                    {
                        ListViewItem LVI = new ListViewItem(ConsistsFirstEngine(consist));
                        LVI.SubItems.Add(consist.ToString());
                        ConsistsListView.Items.Add(LVI);
                    }
                }
                lvwColumnSorter.SortColumn = 0;
                lvwColumnSorter.Order = SortOrder.Ascending;
                ConsistsListView.Sort();
                var selectionIndex = exploreActivity.Consist != null ? Consists.FindIndex(c => c.FilePath == exploreActivity.Consist.FilePath) : -1;
                ConsistsListView.SelectedItems.Clear();
                if (selectionIndex >= 0)
                {
                    ConsistsListView.Focus();
                    this.ConsistsListView.Items[ConsistsIndex(Consists[selectionIndex])].Selected = true;
                    ConsistsListView.Select();
                }
                else if (Consists.Count > 0)
                    this.ConsistsListView.Items[0].Selected = true;
                buttonOk.Enabled = listPaths.Items.Count > 0 && this.ConsistsListView.Items.Count > 0;
            });
		}

        //check whether consist has engine
        bool ConsistsHasEngine(Consist con)
        {
            try
            {
                var WagonList = con.CONFile.Train.TrainCfg.WagonList;
                foreach (var wagon in WagonList)
                {
                    if (wagon.IsEngine) return true;
                }
            }
            catch { return true; }
            return false;
        }

        //check the name of the first engine
        string ConsistsFirstEngine(Consist con)
        {
            try
            {
                var WagonList = con.CONFile.Train.TrainCfg.WagonList;
                foreach (var wagon in WagonList)
                {
                    if (wagon.IsEngine) return wagon.Name;
                }
            }
            catch { return "N/A"; }
            return "N/A";
        }

        //find the index of a consist in the sorted list
        int ConsistsIndex(Consist con)
        {
            try
            {
                var index = 0;
                foreach (var item in this.ConsistsListView.Items)
                {
                    if (con.Name == ((ListViewItem)item).SubItems[1].Text) return index;
                    index++;
                }
            }
            catch { return 0; }
            return 0;
        }
        
        void ExploreForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (PathLoader != null)
				PathLoader.Cancel();
			if (ConsistLoader != null)
				ConsistLoader.Cancel();
		}

        private void ConsistsViewColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Determine if clicked column is already the column that is being sorted.
            if (e.Column == lvwColumnSorter.SortColumn)
            {
                // Reverse the current sort direction for this column.
                if (lvwColumnSorter.Order == SortOrder.Ascending)
                {
                    lvwColumnSorter.Order = SortOrder.Descending;
                }
                else
                {
                    lvwColumnSorter.Order = SortOrder.Ascending;
                }
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                lvwColumnSorter.SortColumn = e.Column;
                lvwColumnSorter.Order = SortOrder.Ascending;
            }

            // Perform the sort with these new sort options.
            this.ConsistsListView.Sort();
        }
	}

    // Implements the manual sorting of items by columns.
    class ListViewColumnSorter  : System.Collections.IComparer
    {
        /// <summary>
        /// Specifies the column to be sorted
        /// </summary>
        private int ColumnToSort;
        /// <summary>
        /// Specifies the order in which to sort (i.e. 'Ascending').
        /// </summary>
        private SortOrder OrderOfSort;
        /// <summary>
        /// Case insensitive comparer object
        /// </summary>

        public ListViewColumnSorter()
        {
            // Initialize the column to '0'
            ColumnToSort = 0;

            // Initialize the sort order to 'none'
            OrderOfSort = SortOrder.None;

        }

        /// <summary>
        /// This method is inherited from the IComparer interface.  It compares the two objects passed using a case insensitive comparison.
        /// </summary>
        /// <param name="x">First object to be compared</param>
        /// <param name="y">Second object to be compared</param>
        /// <returns>The result of the comparison. "0" if equal, negative if 'x' is less than 'y' and positive if 'x' is greater than 'y'</returns>
        public int Compare(object x, object y)
        {
            int compareResult;
            ListViewItem listviewX, listviewY;

            // Cast the objects to be compared to ListViewItem objects
            listviewX = (ListViewItem)x;
            listviewY = (ListViewItem)y;

            // Compare the two items
            compareResult = string.Compare(listviewX.SubItems[ColumnToSort].Text, listviewY.SubItems[ColumnToSort].Text, true);

            // Calculate correct return value based on object comparison
            if (OrderOfSort == SortOrder.Ascending)
            {
                // Ascending sort is selected, return normal result of compare operation
                return compareResult;
            }
            else if (OrderOfSort == SortOrder.Descending)
            {
                // Descending sort is selected, return negative result of compare operation
                return (-compareResult);
            }
            else
            {
                // Return '0' to indicate they are equal
                return 0;
            }
        }

        /// <summary>
        /// Gets or sets the number of the column to which to apply the sorting operation (Defaults to '0').
        /// </summary>
        public int SortColumn
        {
            set
            {
                ColumnToSort = value;
            }
            get
            {
                return ColumnToSort;
            }
        }

        /// <summary>
        /// Gets or sets the order of sorting to apply (for example, 'Ascending' or 'Descending').
        /// </summary>
        public SortOrder Order
        {
            set
            {
                OrderOfSort = value;
            }
            get
            {
                return OrderOfSort;
            }
        }
    }
}
