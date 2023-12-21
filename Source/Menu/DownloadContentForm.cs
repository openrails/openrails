// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
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
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using GNU.Gettext;
using LibGit2Sharp;
using ORTS.Settings;
using Newtonsoft.Json;
using System.Linq;
using System.Security.Policy;

namespace ORTS
{
    public partial class DownloadContentForm : Form
    {
        private readonly GettextResourceManager Catalog = new GettextResourceManager("Menu");
        private readonly UserSettings Settings;
        private readonly IDictionary<string, RouteSettings.Route> Routes;

        private string RouteName;

        public DownloadContentForm(UserSettings settings)
        {
            InitializeComponent();

            Settings = settings;
            Routes = settings.Routes.Routes;

            for (int index = 0; index < Routes.Count; index++)
            {
                string routeName = Routes.ElementAt(index).Key;
                RouteSettings.Route route = Routes.ElementAt(index).Value;
                dataGridViewDownloadContent.Rows.Add(new string[] { routeName, route.DateInstalled, route.Url });
            }

            Add("OR CPV", "https://github.com/cpvirtual/OR_CPV.git", "");
            Add("NewForest Route V3", "https://github.com/rickloader/NewForestRouteV3.git", "");
            Add("MidEast Coast", "https://github.com/MECoast/MECoast.git", "");
            Add("Demo Model 1", "https://github.com/cjakeman/Demo-Model-1.git", "Demo Model 1");
            Add("Chiltern Route V2", "https://github.com/DocMartin7644/Chiltern-Route-v2.git", "");

            InstallPathTextBox.Text = settings.Content.InstallPath;
        }

        public void Add(string routeName, string url, string subDirectory)
        {
            RouteSettings.Route route = Routes.ContainsKey(routeName) ? Routes[routeName] : null;
            if (route == null)
            {
                dataGridViewDownloadContent.Rows.Add(new string[] { routeName, "", url });
                Routes.Add(routeName, new RouteSettings.Route("", url, subDirectory));
            }
        }

        void dataGridViewDownloadContent_SelectionChanged(object sender, EventArgs e)
        {
            RouteName = dataGridViewDownloadContent.CurrentRow.Cells[0].Value.ToString();

            DownloadContentButton.Enabled = string.IsNullOrWhiteSpace(Routes[RouteName].DateInstalled);
        }

        private void InstallPathButton_Click(object sender, EventArgs e)
        {
            using (var folderBrowser = new FolderBrowserDialog())
            {
                folderBrowser.SelectedPath = InstallPathTextBox.Text;
                folderBrowser.Description = "Main Path where route is to be installed";
                folderBrowser.ShowNewFolderButton = true;
                if (folderBrowser.ShowDialog(this) == DialogResult.OK)
                {
                    InstallPathTextBox.Text = folderBrowser.SelectedPath;
                }
            }
        }

        private void DownloadContentButton_Click(object sender, EventArgs e)
        {
            string installPath = InstallPathTextBox.Text;
            string installPathRoute = Path.Combine(installPath, RouteName);
            string message;

            message = Catalog.GetStringFmt("Route to be installed in \"{0}\", are you sure?", installPathRoute);

            // various checks for the directory where the route is installed

            if (MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
            {
                // cancelled
                return;
            }

            if (!Directory.Exists(installPath))
            {
                message = Catalog.GetStringFmt("Directory \"{0}\" does not exist", installPath);
                MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Directory.Exists(installPathRoute))
            {
                try
                {
                    Directory.CreateDirectory(installPathRoute);
                }
                catch (Exception createDirectoryException)
                {
                    message = Catalog.GetStringFmt("Directory \"{0}\" cannot be created: {1}",
                        installPathRoute, createDirectoryException.Message);
                    MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else 
            {
                if ((Directory.GetFiles(installPathRoute).Length != 0) ||
                    (Directory.GetDirectories(installPathRoute).Length != 0))
                {
                    message = Catalog.GetStringFmt("Directory \"{0}\" exists and is not empty", installPathRoute);
                    MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            Settings.Content.InstallPath = installPath;

            // check if filesystem is case sensitive
            // ok, this check will be wrong if both upper- and lowercase named route directories exist
            bool directoryCaseInsensitive = Directory.Exists(installPathRoute.ToUpper()) && Directory.Exists(installPathRoute.ToLower());

            // set json route filename

            Settings.Content.RouteJsonName = Path.Combine(installPath, "ORRoute.json");

            // the download

            Cursor.Current = Cursors.WaitCursor;

            dataGridViewDownloadContent.CurrentRow.Cells[1].Value = Catalog.GetString("Installing...");
            this.Refresh();

            try
            {
                Repository.Clone(Routes[RouteName].Url, installPathRoute);
            }
            catch (LibGit2SharpException libGit2SharpException)
            {
                {
                    message = Catalog.GetStringFmt("Error during download: {0}", libGit2SharpException.Message);
                    MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            // insert row in Options, tab Content

            if (!string.IsNullOrWhiteSpace(Routes[RouteName].SubDirectory))
            {
                installPathRoute = Path.Combine(installPathRoute, Routes[RouteName].SubDirectory);
            }

            bool updated = false;
            int index = 0;
            while (!updated)
            {
                string routeName = "";
                bool routeNameFound = false;
                foreach (KeyValuePair<string, string> folderSetting in Settings.Folders.Folders)
                {
                    if (index == 0) {
                        routeName = RouteName;
                    } else
                    {
                        routeName = string.Format("{0} ({1})", RouteName, index);
                    }
                    if (folderSetting.Key == routeName) {
                        if (folderSetting.Value.Equals(installPathRoute, 
                            directoryCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                        {
                           updated = true;
                        }
                        else
                        {
                           routeNameFound = true;
                        }
                    }
                }
                if (!updated)
                {
                    if (routeNameFound)
                    {
                        index++;
                    }
                    else
                    {
                        Settings.Folders.Folders[routeName] = installPathRoute;
                        updated = true;
                    }
                }
            }

            string dateTimeNowStr = DateTime.Now.ToString(CultureInfo.CurrentCulture.DateTimeFormat);
            dataGridViewDownloadContent.CurrentRow.Cells[1].Value = dateTimeNowStr;

            Routes[RouteName].DateInstalled = dateTimeNowStr;

            Settings.Folders.Save();
            Settings.Routes.Save();

            MessageBox.Show(Catalog.GetString("Route installed"), Catalog.GetString("Done"), MessageBoxButtons.OK, MessageBoxIcon.Asterisk);

            Close();
        }
    }
}
