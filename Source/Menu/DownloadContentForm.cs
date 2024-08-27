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
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.ComponentModel;
using System.Net;
using System.IO.Compression;
using System.Drawing;
using System.Threading.Tasks;

namespace ORTS
{
    public partial class DownloadContentForm : Form
    {
        private readonly GettextResourceManager Catalog;
        private readonly UserSettings Settings;
        private readonly IDictionary<string, ContentRouteSettings.Route> Routes;

        private string RouteName;

        private readonly string ImageTempFilename;
        private Thread ImageThread;
        private readonly string InfoTempFilename;

        private bool ClosingBlocked;

        //attribute used to refresh UI
        private readonly SynchronizationContext SynchronizationContext;

        public DownloadContentForm(UserSettings settings)
        {
            InitializeComponent();

            this.FormBorderStyle = FormBorderStyle.FixedSingle; // remove resize control
            this.MaximizeBox = false; // disable maximize button

            Catalog = new GettextResourceManager("Menu");
            Settings = settings;

            Settings.Content.ContentRouteSettings.LoadContent();
            Routes = Settings.Content.ContentRouteSettings.Routes;
            for (int index = 0; index < Routes.Count; index++)
            {
                string routeName = Routes.ElementAt(index).Key;
                ContentRouteSettings.Route route = Routes.ElementAt(index).Value;
                dataGridViewDownloadContent.Rows.Add(new string[] { 
                    routeName, 
                    route.Installed ? route.DateInstalled.ToString(CultureInfo.CurrentCulture.DateTimeFormat) : "", 
                    route.Url });
            }

            dataGridViewDownloadContent.Sort(dataGridViewDownloadContent.Columns[0], ListSortDirection.Ascending);

            for (int index = 0; index < Routes.Count; index++)
            {
                DataGridViewRow row = dataGridViewDownloadContent.Rows[index];
                DataGridViewCell cell = row.Cells[2];
                cell.ToolTipText = cell.Value.ToString();
            }

            if (string.IsNullOrEmpty(settings.ContentInstallPath))
            {
                settings.ContentInstallPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Open Rails", "Content");
            }

            InstallPathTextBox.Text = settings.ContentInstallPath;

            ImageTempFilename = Path.GetTempFileName();
            InfoTempFilename = Path.GetTempFileName();
            File.Delete(InfoTempFilename);
            InfoTempFilename = Path.ChangeExtension(ImageTempFilename, "html");

            SynchronizationContext = SynchronizationContext.Current;
        }

        #region SelectionChanged
        private void dataGridViewDownloadContent_SelectionChanged(object sender, EventArgs e)
        {
            DisableButtons();

            RouteName = dataGridViewDownloadContent.CurrentRow.Cells[0].Value.ToString();

            // picture box handling

            if (pictureBoxRoute.Image != null)
            {
                pictureBoxRoute.Image.Dispose();
                pictureBoxRoute.Image = null;
            }

            if (!string.IsNullOrEmpty(Routes[RouteName].Image))
            {
                if (ImageThread != null)
                {
                    if (ImageThread.IsAlive)
                    {
                        ImageThread.Abort();
                    }
                }

                // use a thread as grabbing the picture from the web might take a few seconds
                ImageThread = new Thread(() =>
                {
                    // wait one second as the user might scroll through the list
                    Stopwatch sw = Stopwatch.StartNew();
                    while (sw.ElapsedMilliseconds <= 1000) { }

                    try
                    {
                        using (WebClient myWebClient = new WebClient())
                        {
                            myWebClient.DownloadFile(Routes[RouteName].Image, ImageTempFilename);
                        }
                        if (File.Exists(ImageTempFilename))
                        {
                            pictureBoxRoute.Image = new Bitmap(ImageTempFilename);
                        }
                    }
                    catch
                    {
                        // route lives whithout a picture, not such a problem
                    }
                });
                ImageThread.Start();
            }

            // text box with description

            textBoxRoute.Text = Routes[RouteName].Description;

            // buttons
            EnableButtons();
        }
        #endregion

        #region InstallPathTextBox
        private void InstallPathTextBox_TextChanged(object sender, EventArgs e)
        {
            Settings.ContentInstallPath = InstallPathTextBox.Text;
        }
        #endregion

        #region InstallPathBrowseButton
        private void InstallPathBrowseButton_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowser = new FolderBrowserDialog())
            {
                folderBrowser.SelectedPath = InstallPathTextBox.Text;
                folderBrowser.Description = Catalog.GetString("Main Path where route is to be installed");
                folderBrowser.ShowNewFolderButton = true;
                if (folderBrowser.ShowDialog(this) == DialogResult.OK)
                {
                    InstallPathTextBox.Text = folderBrowser.SelectedPath;
                }
            }
        }
        #endregion

        #region DownloadContentButton
        private async void DownloadContentButton_Click(object sender, EventArgs e)
        {
            ContentRouteSettings.Route route = Routes[RouteName];

            string installPath = InstallPathTextBox.Text;
            if (installPath.EndsWith(@"\"))
            {
                installPath = installPath.Remove(installPath.Length - 1, 1);
            }
            installPath = Path.GetFullPath(installPath);

            string installPathRoute = Path.Combine(installPath, RouteName);
            string message;

            DisableButtons();

            message = Catalog.GetStringFmt("Route to be installed in \"{0}\", are you sure?", installPathRoute);
            if (MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                // cancelled
                EnableButtons();
                return;
            }

            // various checks for the directory where the route is installed

            string pathDirectoryExe = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);

            string topPathDirectoryExe = determineTopDirectory(pathDirectoryExe.Substring(6));
            string topInstallPath = determineTopDirectory(installPath);

            if (OptionsForm.ContentFolder.isWrongPath(installPath, Catalog)) {
                // cancelled
                EnableButtons();
                return;
            }

            try
            {
                DriveInfo dInfo = new DriveInfo(installPathRoute);

                long size = route.InstallSize + route.DownloadSize;

                if (size > (dInfo.AvailableFreeSpace * 1.1))
                {
                    message = Catalog.GetStringFmt("Not enough diskspace on drive {0} ({1}), available {2} kB, needed {3} kB, still continue?",
                        dInfo.Name,
                        dInfo.VolumeLabel,
                        (dInfo.AvailableFreeSpace / 1024).ToString("N0"),
                        (size / 1024).ToString("N0"));

                    if (MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    {
                        // cancelled
                        EnableButtons();
                        return;
                    }
                }
            }
            catch (System.IO.DriveNotFoundException)
            {
                message = Catalog.GetStringFmt("Drive not available");
                MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                // cancelled
                EnableButtons();
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
                    EnableButtons();
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
                    EnableButtons();
                    return;
                }
            }

            // the download

            dataGridViewDownloadContent.CurrentRow.Cells[1].Value = Catalog.GetString("Installing...");
            Refresh();

            if (!await Task.Run(() => downloadRoute(installPathRoute)))
            {
                EnableButtons();
                return;
            }

            // insert row in Options, tab Content

            if (!insertRowInOptions(installPathRoute))
            {
                EnableButtons();
                return;
            }

            route.Installed = true;

            DateTime dateTimeNow = DateTime.Now;
            dataGridViewDownloadContent.CurrentRow.Cells[1].Value = dateTimeNow.ToString(CultureInfo.CurrentCulture.DateTimeFormat);
            route.DateInstalled = dateTimeNow;

            route.DirectoryInstalledIn = installPathRoute;

            Settings.Folders.Save();
            Settings.Content.Save();

            MainForm mainForm = ((MainForm)Owner);
            mainForm.LoadFolderListWithoutTask();
            mainForm.comboBoxFolder.SelectedIndex = determineSelectedIndex(mainForm.comboBoxFolder, RouteName);

            MessageBox.Show(Catalog.GetString("Route installed."),
            Catalog.GetString("Done"), MessageBoxButtons.OK, MessageBoxIcon.Asterisk);

            Refresh();

            EnableButtons();
        }

        private bool downloadRoute(string installPathRoute)
        {
            ContentRouteSettings.Route route = Routes[RouteName];
            bool returnValue = false;

            Thread downloadThread = new Thread(() =>
            {
                if (route.getDownloadType() == ContentRouteSettings.DownloadType.github)
                {
                    returnValue = doTheClone(installPathRoute);
                }
                if (route.getDownloadType() == ContentRouteSettings.DownloadType.zip)
                {
                    returnValue = doTheZipDownload(route.Url, Path.Combine(installPathRoute, RouteName + ".zip"));
                }
            });
            // start download in thread to be able to show the progress in the main thread
            downloadThread.Start();

            while (downloadThread.IsAlive)
            {
                Stopwatch sw = Stopwatch.StartNew();

                TotalBytes = 0;
                sumMB(installPathRoute);
                SynchronizationContext.Post(new SendOrPostCallback(o =>
                {
                    dataGridViewDownloadContent.CurrentRow.Cells[1].Value = 
                        string.Format("Downloaded: {0} kB", (string)o);
                }), (TotalBytes / 1024).ToString("N0"));

                while ((downloadThread.IsAlive) && (sw.ElapsedMilliseconds <= 1000)) { }
            }

            if (returnValue)
            {
                if (route.Url.EndsWith(".zip"))
                {
                    Thread installThread = new Thread(() =>
                    {
                        returnValue = doTheUnzipInstall(Path.Combine(installPathRoute, RouteName + ".zip"), installPathRoute);
                    });
                    installThread.Start();

                    long bytesZipfile = TotalBytes;

                    while (installThread.IsAlive)
                    {
                        Stopwatch sw = Stopwatch.StartNew();

                        TotalBytes = -bytesZipfile;
                        sumMB(installPathRoute);
                        SynchronizationContext.Post(new SendOrPostCallback(o =>
                        {
                            dataGridViewDownloadContent.CurrentRow.Cells[1].Value = 
                                string.Format("Installed: {0} kB", (string)o);
                        }), (TotalBytes / 1024).ToString("N0"));

                        while ((installThread.IsAlive) && (sw.ElapsedMilliseconds <= 1000)) { }
                    }
                }
            }

            SynchronizationContext.Post(new SendOrPostCallback(o =>
            {
                dataGridViewDownloadContent.CurrentRow.Cells[1].Value = "";
            }), "");


            return returnValue;
        }

        private bool doTheClone(string installPathRoute)
        {
            try
            {
                Repository.Clone(Routes[RouteName].Url, installPathRoute);
            }
            catch (LibGit2SharpException libGit2SharpException)
            {
                {
                    string message = Catalog.GetStringFmt("Error during github download: {0}", libGit2SharpException.Message);
                    MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            return true;
        }

        private bool doTheZipDownload(string url, string installPathRouteZipfileName)
        {
            try
            {
                WebClient myWebClient = new WebClient();
                myWebClient.DownloadFile(url, installPathRouteZipfileName);
            }
            catch (Exception error)
            {
                {
                    string message = Catalog.GetStringFmt("Error during download zipfile {0}: {1}", url, error.Message);
                    MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            return true;
        }

        private bool doTheUnzipInstall(string installPathRouteZipfileName, string installPathRoute)
        {
            try
            {
                ZipFile.ExtractToDirectory(installPathRouteZipfileName, installPathRoute);
                File.Delete(installPathRouteZipfileName);
            }
            catch (Exception error)
            {
                {
                    string message = Catalog.GetStringFmt("Error during unzip zipfile {0}: {1}", installPathRouteZipfileName, error.Message);
                    MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            return true;
        }

        long TotalBytes = 0;

        private bool sumMB(string path)
        {
            try 
            {
                foreach (string fileName in Directory.GetFiles(path))
                {
                    try
                    {
                        TotalBytes += new FileInfo(fileName).Length;
                    }
                    catch (Exception)
                    {
                        // this summing is also used during the delete,
                        // so sometimes the file is already gone
                    }
                }

                foreach (string directoryName in Directory.GetDirectories(path))
                {
                    sumMB(directoryName);
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private bool insertRowInOptions(string installPathRoute)
        {
            // sometimes the route is located one directory level deeper, determine real installPathRoute
            string installPathRouteReal;

            if (Directory.Exists(Path.Combine(installPathRoute, "routes")))
            {
                installPathRouteReal = installPathRoute;
            }
            else
            {
                string[] directories = Directory.GetDirectories(installPathRoute);
                int indexDirectories = 0;
                while ((indexDirectories < directories.Length) &&
                    !Directory.Exists(Path.Combine(directories[indexDirectories], "routes")))
                {
                    indexDirectories++;
                }
                if (indexDirectories < directories.Length)
                {
                    installPathRouteReal = Path.Combine(installPathRoute, directories[indexDirectories]);
                }
                else
                {
                    string message = Catalog.GetString("Incorrect route, directory \"routes\" not found");
                    MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            string routeName = RouteName;
            int index = 0;
            var sortedFolders = Settings.Folders.Folders.OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value);
            foreach (KeyValuePair<string, string> folderSetting in sortedFolders)
            {
                if (folderSetting.Key == routeName)
                {
                    if (!folderSetting.Value.Equals(installPathRouteReal, StringComparison.OrdinalIgnoreCase))
                    {
                        index++;
                        routeName = string.Format("{0} ({1})", RouteName, index);
                    }
                }
            }
            Settings.Folders.Folders[routeName] = installPathRouteReal;
            Routes[RouteName].ContentName = routeName;
            Routes[RouteName].ContentDirectory = installPathRouteReal;

            return true;
        }

        #endregion

        #region InfoButton
        private void InfoButton_Click(object sender, EventArgs e)
        {
            writeAndStartInfoFile();
        }

        private void writeAndStartInfoFile()
        {
            using (StreamWriter outputFile = new StreamWriter(InfoTempFilename))
            {
                ContentRouteSettings.Route route = Routes[RouteName];

                outputFile.WriteLine(string.Format("<title>OR: {0}</title>", RouteName));
                outputFile.WriteLine(string.Format("<h3>{0}:</h3>", RouteName));

                string description = route.Description.Replace("\n", "<br />");
                outputFile.WriteLine(string.Format("<p>{0}</p>", description));

                outputFile.WriteLine(string.Format("<img height='350' width='600' src = '{0}'/>", route.Image));

                if (string.IsNullOrWhiteSpace(route.AuthorUrl))
                {
                    outputFile.WriteLine(string.Format("<p>" + Catalog.GetString("created by") + ": {0}</p>",
                        route.AuthorName));
                }
                else
                {
                    outputFile.WriteLine(string.Format("<p>" + Catalog.GetString("created by") + ": <a href='{0}'>{1}</a></p>",
                        route.AuthorUrl, route.AuthorName));
                }

                if (!string.IsNullOrWhiteSpace(route.Screenshot))
                {
                    outputFile.WriteLine(string.Format("<p>" + Catalog.GetString("screenshots") + ": <a href='{0}'>{1}</a></p>",
                        route.Screenshot, route.Screenshot));
                }

                if (route.getDownloadType() == ContentRouteSettings.DownloadType.github)
                {
                    outputFile.WriteLine("<p>" + Catalog.GetString("Downloadable: GitHub format") + "<br>");
                    outputFile.WriteLine(string.Format("- " + Catalog.GetString("From:") + "{0}<br>", route.Url));
                    if (route.InstallSize > 0)
                    {
                        outputFile.WriteLine("- " + Catalog.GetStringFmt("Install size: {0} GB",
                            (route.InstallSize / (1024.0 * 1024 * 1024)).ToString("N")) + "<br></p>");
                    }
                }
                if (route.getDownloadType() == ContentRouteSettings.DownloadType.zip)
                {
                    outputFile.WriteLine(string.Format("<p>Downloadable: zip format<br>"));
                    outputFile.WriteLine(string.Format("- From: {0}<br>", route.Url));
                    if (route.InstallSize > 0)
                    {
                        outputFile.WriteLine("- " + Catalog.GetStringFmt("Install size: {0} GB",
                            (route.InstallSize / (1024.0 * 1024 * 1024)).ToString("N")) + "<br>");
                    }
                    if (route.DownloadSize > 0)
                    {
                        outputFile.WriteLine("- " + Catalog.GetStringFmt("Download size: {0} GB",
                            (route.DownloadSize / (1024.0 * 1024 * 1024)).ToString("N")) + "<br></p>");
                    }
                }

                if (route.Installed)
                {
                    outputFile.WriteLine("<p>" + Catalog.GetString("Installed") + ":<br>");
                    outputFile.WriteLine(string.Format("- " + Catalog.GetString("At") + ": {0}<br>", route.DateInstalled.ToString(CultureInfo.CurrentCulture.DateTimeFormat)));
                    outputFile.WriteLine(string.Format("- " + Catalog.GetString("In") + ": \"{0}\"<br>", route.DirectoryInstalledIn));
                    outputFile.WriteLine(string.Format("- " + Catalog.GetString("Content name") + ": \"{0}\"<br>", route.ContentName));
                    outputFile.WriteLine(string.Format("- " + Catalog.GetString("Content Directory") + ": \"{0}\"<br></p>", route.ContentDirectory));
                }

                outputFile.WriteLine("<p>" + Catalog.GetString("Start options") + ":<br>");
                if (string.IsNullOrWhiteSpace(route.Start.Route))
                {
                    // no start information
                    outputFile.WriteLine("- " + Catalog.GetString("None") + "<br></p>");
                }
                else
                {
                    outputFile.WriteLine("- " + Catalog.GetString("Installation profile") + ": " + RouteName + "<br>");
                    outputFile.WriteLine("- " + Catalog.GetString("Activity") + ": " + route.Start.Activity + "<br>");
                    outputFile.WriteLine("- " + Catalog.GetString("Route") + ": " + route.Start.Route + "<br>");
                    outputFile.WriteLine("- " + Catalog.GetString("Locomotive") + ": " + route.Start.Locomotive + "<br>");
                    outputFile.WriteLine("- " + Catalog.GetString("Consist") + ": " + route.Start.Consist + "<br>");
                    outputFile.WriteLine("- " + Catalog.GetString("Starting at") + ": " + route.Start.StartingAt + "<br>");
                    outputFile.WriteLine("- " + Catalog.GetString("Heading to") + ": " + route.Start.HeadingTo + "<br>");
                    outputFile.WriteLine("- " + Catalog.GetString("Time") + ": " + route.Start.Time + "<br>");
                    outputFile.WriteLine("- " + Catalog.GetString("Season") + ": " + route.Start.Season + "<br>");
                    outputFile.WriteLine("- " + Catalog.GetString("Weather") + ": " + route.Start.Weather + "<br></p>");
                }

                if (Directory.Exists(route.DirectoryInstalledIn))
                {
                    if (route.Installed && route.getDownloadType() == ContentRouteSettings.DownloadType.zip)
                    {
                        infoChangedAndAddedFileForZipDownloadType(route, outputFile);
                    }
                    if (route.Installed && route.getDownloadType() == ContentRouteSettings.DownloadType.github)
                    {
                        bool bothLocalAsRemoteUpdatesFound = infoChangedAndAddedFileForGitHubDownloadType(route, outputFile);
                        if (bothLocalAsRemoteUpdatesFound)
                        {
                            outputFile.WriteLine("<p><b>" + Catalog.GetString("ATTENTION: both local and remote updates found.") + "</b><br>");
                            outputFile.WriteLine("<b>" + Catalog.GetString("Update might fail if the update tries to overwrite a changed local file.") + "</b><br>");
                            outputFile.WriteLine("<b>" + Catalog.GetString("If that's the case you are on your own!") + "</b><br>");
                            outputFile.WriteLine("<b>" + Catalog.GetString("Get a git expert at your desk or just Delete and Install the route again.") + "</b><br></p>");
                        }
                    }
                }
                else
                {
                    outputFile.WriteLine("<p>" + Catalog.GetStringFmt("Directory {0} does not exist", route.DirectoryInstalledIn) + "</b><br></p>");
                }
            }
            try
            {
                // show html file in default browser
                Process.Start(InfoTempFilename);
            }
            catch (Exception e) 
            {
                string message = Catalog.GetStringFmt("Error opening html page {0} in browser. Error: {1}", InfoTempFilename, e.ToString());
                MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            }
        }

        private void infoChangedAndAddedFileForZipDownloadType(ContentRouteSettings.Route route, StreamWriter outputFile)
        {
            List<string> changedAndAddedFiles = getChangedFiles(route.DirectoryInstalledIn, route.DateInstalled);
            outputFile.WriteLine("<p id='changed'>" + Catalog.GetString("Changed local file(s) after the install (timestamp check)") + ":<br>");
            if (changedAndAddedFiles.Count == 0)
            {
                outputFile.WriteLine("- " + Catalog.GetString("No changed files found") + "<br></p>");
            }
            else
            {
                foreach (string changedFile in changedAndAddedFiles)
                {
                    outputFile.WriteLine(changedFile + "<br>");
                }
                outputFile.WriteLine("</p>");
            }
            changedAndAddedFiles = getAddedFiles(route.DirectoryInstalledIn, route.DateInstalled);
            outputFile.WriteLine("<p>" + Catalog.GetString("Added local file(s) after the install (timestamp check)") + ":<br>");
            if (changedAndAddedFiles.Count == 0)
            {
                outputFile.WriteLine("- " + Catalog.GetString("No added files found") + "<br></p>");
            }
            else
            {
                foreach (string changedFile in changedAndAddedFiles)
                {
                    outputFile.WriteLine(changedFile + "<br>");
                }
                outputFile.WriteLine("</p>");
            }
        }

        private bool infoChangedAndAddedFileForGitHubDownloadType(ContentRouteSettings.Route route, StreamWriter outputFile)
        {
            bool changedFound = false;
            bool remoteUpdateFound = false;

            List<string> changedAndAddedFiles = getChangedGitFiles(route.DirectoryInstalledIn);
            outputFile.WriteLine("<p>" + Catalog.GetString("Changed local file(s) after the install (git status)") + ":<br>");
            if (changedAndAddedFiles.Count == 0)
            {
                outputFile.WriteLine("- " + Catalog.GetString("No changed files found") + "<br></p>");
            }
            else
            {
                changedFound = true;
                foreach (string changedFile in changedAndAddedFiles)
                {
                    outputFile.WriteLine(changedFile + "<br>");
                }
                outputFile.WriteLine("</p>");
            }
            changedAndAddedFiles = getAddedGitFiles(route.DirectoryInstalledIn);
            outputFile.WriteLine("<p>" + Catalog.GetString("Added local file(s) after the install (git status)") + ":<br>");
            if (changedAndAddedFiles.Count == 0)
            {
                outputFile.WriteLine("- " + Catalog.GetString("No added files found") + "<br></p>");
            }
            else
            {
                changedFound = true;
                foreach (string changedFile in changedAndAddedFiles)
                {
                    outputFile.WriteLine(changedFile + "<br>");
                }
                outputFile.WriteLine("</p>");
                    }

            outputFile.WriteLine("<p>" + Catalog.GetString("Remote GitHub Updates available:") + "<br>");

            List<string> commitStrings = getCommits(route.DirectoryInstalledIn);
            if (commitStrings.Count > 0)
            {
                remoteUpdateFound = true;
                for (int index = 0; index < commitStrings.Count; index += 4)
                {
                    outputFile.WriteLine(string.Format("- commit {0}<br>", commitStrings[index]));
                    outputFile.WriteLine(Catalog.GetStringFmt("On {0} by {1}",
                        commitStrings[index + 1], commitStrings[index + 2]) + ":<br>");
                    outputFile.WriteLine(commitStrings[index + 3] + "<br><br>");
                }
                outputFile.WriteLine("</p>");
            }
                else
                {
                outputFile.WriteLine("- " + Catalog.GetString("No updates found") + "<br></p>");
            }

            return changedFound && remoteUpdateFound;
        }
        #endregion

        private int determineSelectedIndex(ComboBox comboBox, string compareWith)
        {
            bool found = false;
            int index = 0;

            string classOfItem;
            if (comboBox.Items.Count > 0)
            {
                classOfItem = comboBox.Items[0].GetType().Name;
            }
            else
            {
                throw new StartNotFound(Catalog.GetStringFmt(compareWith));
            }

            if ((classOfItem == "DefaultExploreActivity") && (compareWith == "- Explore Route -"))
            {
                // "- Explore Route -" gets translated,
                // so it might not be found in the combobox
                found = true;
                index = 0;
            }
            if ((classOfItem == "DefaultExploreActivity") && (compareWith == "+ Explore in Activity Mode +"))
            {
                // "+ Explore in Activity Mode +" gets translated
                found = true;
                index = 1;
            }

            while (!found && (index < comboBox.Items.Count))
            {
                string comboboxName = "";
                switch (classOfItem)
                {
                    case "Folder":
                        comboboxName = ((Menu.Folder)comboBox.Items[index]).Name;
                        break;
                    case "Route":
                        comboboxName = ((Menu.Route)comboBox.Items[index]).Name;
                        break;
                    case "DefaultExploreActivity":
                        comboboxName = ((Menu.Activity)comboBox.Items[index]).Name;
                        break;
                    case "Locomotive":
                        comboboxName = ((Menu.Locomotive)comboBox.Items[index]).Name;
                        break;
                    case "Consist":
                        comboboxName = ((Menu.Consist)comboBox.Items[index]).Name;
                        break;
                    case "String":
                        comboboxName = (string)comboBox.Items[index];
                        break;
                    case "Path":
                        comboboxName = ((Menu.Path)comboBox.Items[index]).End;
                        break;
                    case "KeyedComboBoxItem":
                        comboboxName = ((MainForm.KeyedComboBoxItem)comboBox.Items[index]).Value;
                        break;
                }

                if (comboboxName == compareWith)
                {
                    found = true;
                }
                else
                {
                    index++;
                }
            }
            if (found)
            {
                comboBox.SelectedIndex = index;
            }
            else
            {
                if (classOfItem == "Route")
                {
                    // if a route is not found and amount of routes is 1
                    // then default to this one and only route
                    if (comboBox.Items.Count == 1)
                    {
                        index = 0;
                    }
                    else
                    {
                        throw new StartNotFound(Catalog.GetStringFmt(compareWith));
                    }
                }
                else
                {
                    throw new StartNotFound(Catalog.GetStringFmt(compareWith));
                }
            }

            return index;
        }

        private class StartNotFound : Exception
        {
            public StartNotFound(string message)
                : base(message)
            { }
        }

        #region DeleteButton
        private async void DeleteButton_Click(object sender, EventArgs e)
        {
            ContentRouteSettings.Route route = Routes[RouteName];
            string message;

            DisableButtons();

            message = Catalog.GetStringFmt("Directory \"{0}\" is to be deleted, are you really sure?", route.DirectoryInstalledIn);
            if (MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                // cancelled
                EnableButtons();
                return;
            }

            if (Directory.Exists(route.DirectoryInstalledIn)) {

                if (areThereChangedAddedFiles(route))
                {
                    message = Catalog.GetStringFmt("Changed or added local files found in Directory \"{0}\". Do you want to continue?", route.DirectoryInstalledIn);
                    if (MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                    {
                        // cancelled
                        EnableButtons();
                        return;
                    }
                }

                await Task.Run(() => deleteRoute(route.DirectoryInstalledIn));
            }

            if (Settings.Folders.Folders.ContainsKey(route.ContentName))
            {
                if (Settings.Folders.Folders[route.ContentName] == route.ContentDirectory)
                {
                    Settings.Folders.Folders.Remove(route.ContentName);
                }
                Settings.Folders.Save();
            }

            route.Installed = false;
            route.DateInstalled = DateTime.MinValue;
            route.DirectoryInstalledIn = "";

            // remove from registry, but leave entry in Routes for this route name
            ContentRouteSettings.Route routeSaved = route;
            Settings.Content.Save();
            Routes[RouteName] = routeSaved;

            dataGridViewDownloadContent.CurrentRow.Cells[1].Value = "";

            MainForm mainForm = ((MainForm)Owner);
            mainForm.LoadFolderListWithoutTask();

            Refresh();

            EnableButtons();
        }

        private void deleteRoute(string directoryInstalledIn)
        {
            Thread deleteThread = new Thread(() =>
            {
                ContentRouteSettings.directoryDelete(directoryInstalledIn);
            });
            // start delete in thread to be able to show the progress in the main thread
            deleteThread.Start();

            while (deleteThread.IsAlive)
            {
                Stopwatch sw = Stopwatch.StartNew();

                TotalBytes = 0;
                if (sumMB(directoryInstalledIn))
                {
                    SynchronizationContext.Post(new SendOrPostCallback(o =>
                    {
                        dataGridViewDownloadContent.CurrentRow.Cells[1].Value =
                            string.Format("Left: {0} kB", (string)o);
                    }), (TotalBytes / 1024).ToString("N0"));
                }

                while (deleteThread.IsAlive && (sw.ElapsedMilliseconds <= 1000)) { }
            }
        }
        #endregion

        #region UpdateButton
        private void updateButton_Click(object sender, EventArgs e)
        {
            ContentRouteSettings.Route route = Routes[RouteName];
            string message;

            DisableButtons();

            List<string> commitStrings = getCommits(route.DirectoryInstalledIn);
            if (commitStrings.Count > 0)
            {
                message = Catalog.GetString("Remote updates found. Do you want to continue?");
                if (MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                {
                    // cancelled
                    EnableButtons();
                    return;
                }
                if (areThereChangedAddedFiles(route))
                {
                    message = Catalog.GetString("Changed or added local files found, Update might fail. Do you want to continue?");
                    if (MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                    {
                        // cancelled
                        EnableButtons();
                        return;
                    }
                }
                if (doThePull(route)) 
                {
                    message = Catalog.GetString("Updates installed");
                    MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                message = Catalog.GetString("No updates found");
                MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            MainForm mainForm = ((MainForm)Owner);
            mainForm.comboBoxFolder.SelectedIndex = determineSelectedIndex(mainForm.comboBoxFolder, RouteName);

            Refresh();

            EnableButtons();
        }

        private bool doThePull(ContentRouteSettings.Route route) {

            try
            {
                using (var repo = new Repository(route.DirectoryInstalledIn))
                {
                    LibGit2Sharp.PullOptions options = new LibGit2Sharp.PullOptions
                    {
                        FetchOptions = new FetchOptions()
                    };

                    // User information to create a merge commit
                    var signature = new LibGit2Sharp.Signature(
                        new Identity("Open Rails", "www.openrails.org"), DateTimeOffset.Now);

                    // Pull
                    Commands.Pull(repo, signature, options);
                }
            }
            catch (LibGit2SharpException libGit2SharpException)
            {
                {
                    string message = Catalog.GetStringFmt("Error during github pull: {0}", libGit2SharpException.Message);
                    MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region okButton
        void okButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }
        #endregion

        private void DisableButtons()
        {
            setCursorToWaitCursor();

            dataGridViewDownloadContent.Enabled = false;
            InstallPathTextBox.Enabled = false;
            InstallPathBrowseButton.Enabled = false;
            infoButton.Enabled = false;
            downloadContentButton.Enabled = false;
            updateButton.Enabled = false;
            deleteButton.Enabled = false;
            okButton.Enabled = false;
        }

        private void setCursorToWaitCursor()
        {
            ClosingBlocked = true;

            MainForm mainForm = (MainForm)Owner;
            mainForm.Cursor = Cursors.WaitCursor;
        }

        private void EnableButtons()
        { 
            ContentRouteSettings.Route route = Routes[RouteName];

            dataGridViewDownloadContent.Enabled = true;
            InstallPathTextBox.Enabled = true;
            InstallPathBrowseButton.Enabled = true;
            infoButton.Enabled = true;
            downloadContentButton.Enabled = !route.Installed;
            updateButton.Enabled = route.Installed && (route.getDownloadType() == ContentRouteSettings.DownloadType.github);
            deleteButton.Enabled = route.Installed;
            okButton.Enabled = true;

            setCursorToDefaultCursor();
        }

        private void setCursorToDefaultCursor()
        {
            MainForm mainForm = (MainForm)Owner;
            mainForm.Cursor = Cursors.Default;

            ClosingBlocked = false;
        }

        private bool areThereChangedAddedFiles(ContentRouteSettings.Route route)
        {
            if (route.getDownloadType() == ContentRouteSettings.DownloadType.zip)
            {
                return 
                    (getChangedFiles(route.DirectoryInstalledIn, route.DateInstalled).Count > 0) ||
                    (getAddedFiles(route.DirectoryInstalledIn, route.DateInstalled).Count > 0);
            }
            if (route.getDownloadType() == ContentRouteSettings.DownloadType.github)
            {
                return
                    (getChangedGitFiles(route.DirectoryInstalledIn).Count > 0) ||
                    (getAddedGitFiles(route.DirectoryInstalledIn).Count > 0);
            }

            return false;
        }

        //
        // example:
        //  input:  D:\OpenRailsMaster\Program
        //  output: D:\OpenRailsMaster
        //
        private static string determineTopDirectory(string directoryName)
        {
            string tmpDirectoryName = directoryName;
            string topDirectoryName = directoryName;

            while (Directory.GetParent(tmpDirectoryName) != null)
            {
                topDirectoryName = tmpDirectoryName;
                tmpDirectoryName = Directory.GetParent(tmpDirectoryName).ToString();
            }

            return topDirectoryName;
        }

        private static List<string> getChangedFiles(string directoryName, DateTime dateInstalled)
        {
            return getChangedAndAddedFiles(directoryName, dateInstalled, true);
        }

        private static List<string> getAddedFiles(string directoryName, DateTime dateInstalled)
        {
            return getChangedAndAddedFiles(directoryName, dateInstalled, false);
        }

        private static List<string> getChangedAndAddedFiles(string directoryName, DateTime dateInstalled, bool checkForChanged)
        {
            List<string> changedFiles = new List<string>();

            if (Directory.Exists(directoryName))
            {
                getChangedAndAddedFilesDeeper(directoryName, dateInstalled, changedFiles, checkForChanged);
            }

            return changedFiles;
        }

        private static void getChangedAndAddedFilesDeeper(string directoryName, DateTime dateInstalled, List<string> changedFiles, bool checkForChanged)
        {
            foreach (string filename in Directory.GetFiles(directoryName))
            {
                FileInfo fi = new FileInfo(filename);
                if (checkForChanged)
                {
                    if ((fi.LastWriteTime > dateInstalled) && (fi.CreationTime < dateInstalled))
                    {
                        changedFiles.Add(fi.FullName);
                    }
                }
                else
                {
                    // check for new files, creation date after date installed
                    if (fi.CreationTime > dateInstalled)
                    {
                        changedFiles.Add(fi.FullName);
                    }
                }
            }
            foreach (string subDirectoryName in Directory.GetDirectories(directoryName))
            {
                getChangedAndAddedFilesDeeper(subDirectoryName, dateInstalled, changedFiles, checkForChanged);
            }
        }

        private static List<string> getChangedGitFiles(string directoryName)
        {
            return getChangedAndAddedGitFiles(directoryName, true);
        }

        private static List<string> getAddedGitFiles(string directoryName)
        {
            return getChangedAndAddedGitFiles(directoryName, false);
        }

        private static List<string> getChangedAndAddedGitFiles(string directoryName, bool checkForChanged)
        {
            List<string> changedFiles = new List<string>();

            using (var repo = new Repository(directoryName))
            {
                foreach (var item in repo.RetrieveStatus(new LibGit2Sharp.StatusOptions()))
                {
                    if (checkForChanged)
                    {
                        if (item.State == FileStatus.NewInWorkdir)
                        {
                            changedFiles.Add(item.FilePath);
                        }
                    }
                    else
                    {
                        if (item.State == FileStatus.ModifiedInWorkdir)
                        {
                            changedFiles.Add(item.FilePath);
                        }
                    }
                }
            }

            return changedFiles;
        }

        private List<string> getCommits(string installPathRoute)
        {
            List<string> commits = new List<string>();

            try
            {
                using (var repo = new Repository(installPathRoute))
                {
                    var options = new FetchOptions
                    {
                        Prune = true,
                        TagFetchMode = TagFetchMode.Auto
                    };
                    var remote = repo.Network.Remotes["origin"];
                    var msg = "Fetching remote";
                    var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                    Commands.Fetch(repo, remote.Name, refSpecs, options, msg);

                    var filter = new CommitFilter
                    {
                        ExcludeReachableFrom = repo.Branches["main"],
                        IncludeReachableFrom = repo.Branches["origin/Head"],
                    };
                    // at first I tried to create a List<Commit>, but that resulted
                    // in some strange access violation when reading the list
                    // --> hence this workaround
                    foreach (Commit commit in repo.Commits.QueryBy(filter))
                    {
                        commits.Add(commit.Id.ToString());
                        commits.Add(commit.Author.When.ToString(CultureInfo.CurrentCulture.DateTimeFormat));
                        commits.Add(commit.Author.Name);
                        commits.Add(commit.Message);
                    }
                }
            }
            catch (LibGit2SharpException libGit2SharpException)
            {
                {
                    string message = Catalog.GetStringFmt("Error during GitHub pull, updates might not be installed, error: {0}", libGit2SharpException.Message);
                    MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            return commits;
        }

        private void DownloadContentForm_FormClosing(object sender, FormClosingEventArgs formClosingEventArgs)
        {
            if (ClosingBlocked)
            {
                // cancelled event, so continue
                formClosingEventArgs.Cancel = true;
                return;
            }

            try
            {
                if (pictureBoxRoute.Image != null)
                {
                    pictureBoxRoute.Image.Dispose();
                    pictureBoxRoute.Image = null;
                }
                File.Delete(ImageTempFilename);
                File.Delete(InfoTempFilename);
            }
            catch
            {
                // just ignore, the files are in the user temp directory anyway
            }
        }
    }
}

