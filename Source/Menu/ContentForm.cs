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
using GNU.Gettext.WinForms;

namespace Menu
{
    public partial class ContentForm : Form
    {
        private readonly GettextResourceManager Catalog;
        private readonly UserSettings Settings;
        private readonly string BaseDocumentationUrl;

        private readonly IDictionary<string, ContentRouteSettings.Route> AutoInstallRoutes;

        private string AutoInstallRouteName;

        private readonly string ImageTempFilename;
        private Thread ImageThread;
        private readonly string InfoTempFilename;

        private bool AutoInstallClosingBlocked;

        //attribute used to refresh UI
        private readonly SynchronizationContext AutoInstallSynchronizationContext;

        public bool AutoInstallDoingTheSumOfTheFileBytes = false;

        private readonly string ManualInstallBrouwseDir;

        private bool In_dataGridViewManualInstall_SelectionChanged = false;
        private bool In_buttonManualInstallAdd_Click = false;

        private bool ManualInstallChangesMade = false;

        private readonly bool NoInternet = false;

        public ContentForm(UserSettings settings, string baseDocumentationUrl)
        {
            InitializeComponent();

            // Windows 2000 and XP should use 8.25pt Tahoma, while Windows
            // Vista and later should use 9pt "Segoe UI". We'll use the
            // Message Box font to allow for user-customizations, though.
            Font = SystemFonts.MessageBoxFont;

            this.FormBorderStyle = FormBorderStyle.FixedSingle; // remove resize control
            this.MaximizeBox = false; // disable maximize button

            Catalog = new GettextResourceManager("Menu");
            Localizer.Localize(this, Catalog);

            Settings = settings;
            BaseDocumentationUrl = baseDocumentationUrl;

            //
            // "Auto Installed" tab
            //
            string errorMsg = "";
            Settings.Content.ContentRouteSettings.LoadContent(ref errorMsg);
            if (!string.IsNullOrEmpty(errorMsg))
            {
                string message = Catalog.GetStringFmt("Failed to access the content repository (check Internet connection); automatic download not available. Error: {0}", errorMsg);
                MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                NoInternet = true;
            }
            AutoInstallRoutes = Settings.Content.ContentRouteSettings.Routes;
            for (int index = 0; index < AutoInstallRoutes.Count; index++)
            {
                string routeName = AutoInstallRoutes.ElementAt(index).Key;
                ContentRouteSettings.Route route = AutoInstallRoutes.ElementAt(index).Value;
                int indexAdded = dataGridViewAutoInstall.Rows.Add(new string[] {
                    routeName,
                    route.Installed ? route.DateInstalled.ToString(CultureInfo.CurrentCulture.DateTimeFormat) : "",
                    route.Url });
                dataGridViewAutoInstall.Rows[indexAdded].Cells[2].ToolTipText = route.Url;
                if (!route.Installed)
                {
                    changeManualInstallRoute(routeName);
                }
            }

            dataGridViewAutoInstall.Sort(dataGridViewAutoInstall.Columns[0], ListSortDirection.Ascending);

            if (string.IsNullOrEmpty(settings.ContentInstallPath))
            {
                settings.ContentInstallPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Open Rails", "Content");
            }
            textBoxAutoInstallPath.Text = settings.ContentInstallPath;

            ImageTempFilename = Path.GetTempFileName();
            InfoTempFilename = Path.GetTempFileName();
            File.Delete(InfoTempFilename);
            InfoTempFilename = Path.ChangeExtension(ImageTempFilename, "html");

            AutoInstallSynchronizationContext = SynchronizationContext.Current;

            // set focus to datagridview so that arrow keys can be used to scroll thru the list
            dataGridViewAutoInstall.Select();

            // tab "Auto Installed" does not contain a Cancel button
            // it's too difficult and not logical to rollback an "Auto Installed" route
            buttonCancel.Hide();

            //
            // "Manually Installed" tab
            //
            foreach (var folder in Settings.Folders.Folders)
            {
                int indexAdded = dataGridViewManualInstall.Rows.Add(new string[] {
                    folder.Key,
                    folder.Value});
                dataGridViewManualInstall.Rows[indexAdded].Cells[1].ToolTipText = folder.Value;
                if (AutoInstallRoutes.ContainsKey(folder.Key))
                {     
                    dataGridViewManualInstall.Rows[indexAdded].DefaultCellStyle.ForeColor = Color.Gray;
                }
            }
            dataGridViewManualInstall.Sort(dataGridViewManualInstall.Columns[0], ListSortDirection.Ascending);

            ManualInstallBrouwseDir = determineBrowseDir();
            buttonCancel.Enabled = false;

            setTextBoxesManualInstall();

            if (NoInternet)
            {
                // open the Manually Installed tab since the Auto Installed tab needs internet
                tabControlContent.SelectTab(tabPageManuallyInstall);
            }

            if (dataGridViewAutoInstall.Rows.Count == 0) 
            {
                // disable all auto install buttons for a complete empty auto install grid
                // might be the case when no internet available
                DisableAutoInstallButtonsWithoutWait();
            }
        }

        void changeManualInstallRoute(string Route)
        {
            bool found = false;

            foreach (var folder in Settings.Folders.Folders)
            {
                if (folder.Key == Route)
                {
                    // Route found in folder settings
                    found = true;
                }
            }

            if (found)
            {
                // search for the next route (1), (2) etc. not found in folder settings
                int seqNr = 1;
                string route = Route + " (" + seqNr + ")";
                while (found)
                {
                    found = false;
                    foreach (var folder in Settings.Folders.Folders)
                    {
                        if (folder.Key == route)
                        {
                            found = true;
                            seqNr++;
                            route = Route + " (" + seqNr + ")";
                        }
                    }
                }
                Settings.Folders.Folders[route] = Settings.Folders.Folders[Route];
                Settings.Folders.Folders.Remove(Route);
            }
        }

        private void tabControlContent_Selecting(object sender, TabControlCancelEventArgs e)
        {
            switch ((sender as TabControl).SelectedTab.Name)
            {
                case "tabPageAutoInstall":
                    dataGridViewAutoInstall.Select();
                    buttonCancel.Hide();
                    break;
                case "tabPageManuallyInstall":
                    dataGridViewManualInstall.Select();
                    buttonCancel.Show();
                    buttonCancel.Enabled = ManualInstallChangesMade;
                    break;
            }
        }

        private void pbContent_Click(object sender, EventArgs e)
        {
            string fileName = "";
            if (tabControlContent.SelectedTab == tabControlContent.TabPages["tabPageAutoInstall"])
            {
                fileName = BaseDocumentationUrl + "/start.html#content";
            }
            if (tabControlContent.SelectedTab == tabControlContent.TabPages["tabPageManuallyInstall"])
            {
                fileName = BaseDocumentationUrl + "/start.html#installation-profiles";
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = true
            });
        }

        #region Auto Installed
        //
        // "Auto Installed" tab
        //

        private void dataGridViewAutoInstall_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Tab && !(e.Modifiers == Keys.Shift))
            {
                // pressing tab in this datagridview now takes you into the next field
                // default unwanted behaviour is going thru the rows/columns of the datagridview
                textBoxAutoInstallPath.Select();
                e.SuppressKeyPress = true;
            }
            if (e.KeyCode == Keys.Tab && e.Modifiers == Keys.Shift)
            {
                // same for pressing shift-tab
                tabControlContent.Select();
                e.SuppressKeyPress = true;
            }
        }

        private void dataGridViewAutoInstall_SelectionChanged(object sender, EventArgs e)
        {
            DisableAutoInstallButtons();

            AutoInstallRouteName = dataGridViewAutoInstall.CurrentRow.Cells[0].Value.ToString();

            // picture box handling

            if (pictureBoxAutoInstallRoute.Image != null)
            {
                pictureBoxAutoInstallRoute.Image.Dispose();
                pictureBoxAutoInstallRoute.Image = null;
            }

            if (!string.IsNullOrEmpty(AutoInstallRoutes[AutoInstallRouteName].Image))
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
                            myWebClient.DownloadFile(AutoInstallRoutes[AutoInstallRouteName].Image, ImageTempFilename);
                            }
                        if (File.Exists(ImageTempFilename))
                        {
                            pictureBoxAutoInstallRoute.Image = new Bitmap(ImageTempFilename);
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

            textBoxAutoInstallRoute.Text = AutoInstallRoutes[AutoInstallRouteName].Description;

            // buttons
            EnableAutoInstalButtons();

            // set focus back to datagridview
            dataGridViewAutoInstall.Focus();
        }

        private void textBoxAutoInstallPath_TextChanged(object sender, EventArgs e)
        {
            Settings.ContentInstallPath = textBoxAutoInstallPath.Text;
        }

        private void buttonAutoInstallBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowser = new FolderBrowserDialog())
            {
                folderBrowser.SelectedPath = textBoxAutoInstallPath.Text;
                folderBrowser.Description = Catalog.GetString("Main Path where route is to be installed");
                folderBrowser.ShowNewFolderButton = true;
                if (folderBrowser.ShowDialog(this) == DialogResult.OK)
                {
                    textBoxAutoInstallPath.Text = folderBrowser.SelectedPath;
                }
            }
        }

        private void buttonAutoInstallInfo_Click(object sender, EventArgs e)
        {
            writeAndStartInfoFile();
        }

        private void writeAndStartInfoFile()
        {
            using (StreamWriter outputFile = new StreamWriter(InfoTempFilename))
            {
                ContentRouteSettings.Route route = AutoInstallRoutes[AutoInstallRouteName];

                outputFile.WriteLine(string.Format("<title>OR: {0}</title>", AutoInstallRouteName));
                outputFile.WriteLine(string.Format("<h3>{0}:</h3>", AutoInstallRouteName));

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
                    outputFile.WriteLine(Catalog.GetStringFmt("- " + Catalog.GetString("From:") + "{0}<br>", route.Url));
                    if (route.InstallSize > 0)
                    {
                        outputFile.WriteLine("- " + Catalog.GetStringFmt("Install size: {0} GB",
                            (route.InstallSize / (1024.0 * 1024 * 1024)).ToString("N")) + "<br></p>");
                    }
                }
                if (route.getDownloadType() == ContentRouteSettings.DownloadType.zip)
                {
                    outputFile.WriteLine(Catalog.GetString("<p>Downloadable: zip format<br>"));
                    outputFile.WriteLine(Catalog.GetStringFmt("- From: {0}<br>", route.Url));
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
                    outputFile.WriteLine(Catalog.GetStringFmt("- " + Catalog.GetString("At") + ": {0}<br>", route.DateInstalled.ToString(CultureInfo.CurrentCulture.DateTimeFormat)));
                    outputFile.WriteLine(Catalog.GetStringFmt("- " + Catalog.GetString("In") + ": \"{0}\"<br>", route.DirectoryInstalledIn));
                    outputFile.WriteLine(Catalog.GetStringFmt("- " + Catalog.GetString("Content name") + ": \"{0}\"<br>", route.ContentName));
                    outputFile.WriteLine(Catalog.GetStringFmt("- " + Catalog.GetString("Content Directory") + ": \"{0}\"<br></p>", route.ContentDirectory));
                }

                outputFile.WriteLine("<p>" + Catalog.GetString("Start options") + ":<br>");
                if (string.IsNullOrWhiteSpace(route.Start.Route))
                {
                    // no start information
                    outputFile.WriteLine("- " + Catalog.GetString("None") + "<br></p>");
                }
                else
                {
                    outputFile.WriteLine("- " + Catalog.GetString("Installation profile") + ": " + AutoInstallRouteName + "<br>");
                    outputFile.WriteLine("- " + Catalog.GetString("Route") + ": " + route.Start.Route + "<br>");
                    outputFile.WriteLine("- " + Catalog.GetString("Activity") + ": " + route.Start.Activity + "<br>");
                    if (!string.IsNullOrEmpty(route.Start.Locomotive))
                        outputFile.WriteLine("- " + Catalog.GetString("Locomotive") + ": " + route.Start.Locomotive + "<br>");
                    if (!string.IsNullOrEmpty(route.Start.Consist))
                        outputFile.WriteLine("- " + Catalog.GetString("Consist") + ": " + route.Start.Consist + "<br>");
                    if (!string.IsNullOrEmpty(route.Start.StartingAt))
                        outputFile.WriteLine("- " + Catalog.GetString("Starting at") + ": " + route.Start.StartingAt + "<br>");
                    if (!string.IsNullOrEmpty(route.Start.HeadingTo))
                        outputFile.WriteLine("- " + Catalog.GetString("Heading to") + ": " + route.Start.HeadingTo + "<br>");
                    if (!string.IsNullOrEmpty(route.Start.Time))
                        outputFile.WriteLine("- " + Catalog.GetString("Time") + ": " + route.Start.Time + "<br>");
                    if (!string.IsNullOrEmpty(route.Start.Season))
                        outputFile.WriteLine("- " + Catalog.GetString("Season") + ": " + route.Start.Season + "<br>");
                    if (!string.IsNullOrEmpty(route.Start.Weather))
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
                    if (!string.IsNullOrEmpty(route.DirectoryInstalledIn))
                    {
                        outputFile.WriteLine("<p>" + Catalog.GetStringFmt("Directory {0} does not exist", route.DirectoryInstalledIn) + "</b><br></p>");
                    }
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

        private async void buttonAutoInstallInstall_Click(object sender, EventArgs e)
        {
            ContentRouteSettings.Route route = AutoInstallRoutes[AutoInstallRouteName];

            string installPath = textBoxAutoInstallPath.Text;
            if (installPath.EndsWith(@"\"))
            {
                installPath = installPath.Remove(installPath.Length - 1, 1);
            }
            installPath = Path.GetFullPath(installPath);

            string installPathRoute = Path.Combine(installPath, AutoInstallRouteName);
            string message;

            DisableAutoInstallButtons();

            message = Catalog.GetStringFmt("Route to be installed in \"{0}\", are you sure?", installPathRoute);
            if (MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                // cancelled
                EnableAutoInstalButtons();
                return;
            }

            // various checks for the directory where the route is installed

            string pathDirectoryExe = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);

            string topPathDirectoryExe = determineTopDirectory(pathDirectoryExe.Substring(6));
            string topInstallPath = determineTopDirectory(installPath);

            if (isWrongPath(installPath, Catalog)) {
                // cancelled
                EnableAutoInstalButtons();
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
                        EnableAutoInstalButtons();
                        return;
                    }
                }
            }
            catch (System.IO.DriveNotFoundException)
            {
                message = Catalog.GetStringFmt("Drive not available");
                MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                // cancelled
                EnableAutoInstalButtons();
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
                    EnableAutoInstalButtons();
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
                    EnableAutoInstalButtons();
                    return;
                }
            }

            // the download

            dataGridViewAutoInstall.CurrentRow.Cells[1].Value = Catalog.GetString("Installing...");
            Refresh();

            if (!await Task.Run(() => downloadRoute(installPathRoute)))
            {
                EnableAutoInstalButtons();
                return;
            }

            // insert row in Options, tab Content

            if (!insertRowInOptions(installPathRoute))
            {
                EnableAutoInstalButtons();
                return;
            }

            route.Installed = true;

            DateTime dateTimeNow = DateTime.Now;
            dataGridViewAutoInstall.CurrentRow.Cells[1].Value = dateTimeNow.ToString(CultureInfo.CurrentCulture.DateTimeFormat);
            route.DateInstalled = dateTimeNow;

            route.DirectoryInstalledIn = installPathRoute;

            Settings.Folders.Save();
            Settings.Content.Save();

            MainForm mainForm = ((MainForm)Owner);
            mainForm.LoadFolderListWithoutTask();
            mainForm.comboBoxFolder.SelectedIndex = determineSelectedIndex(mainForm.comboBoxFolder, AutoInstallRouteName);

            MessageBox.Show(Catalog.GetString("Route installed."),
            Catalog.GetString("Done"), MessageBoxButtons.OK, MessageBoxIcon.Asterisk);

            Refresh();

            EnableAutoInstalButtons();
        }

        private bool downloadRoute(string installPathRoute)
        {
            ContentRouteSettings.Route route = AutoInstallRoutes[AutoInstallRouteName];
            bool returnValue = false;

            Thread downloadThread = new Thread(() =>
            {
                if (route.getDownloadType() == ContentRouteSettings.DownloadType.github)
                {
                    returnValue = doTheClone(installPathRoute);
                }
                if (route.getDownloadType() == ContentRouteSettings.DownloadType.zip)
                {
                    returnValue = doTheZipDownload(route.Url, Path.Combine(installPathRoute, AutoInstallRouteName + ".zip"));
                }
            });
            // start download in thread to be able to show the progress in the main thread
            downloadThread.Start();

            while (downloadThread.IsAlive)
            {
                TotalBytes = 0;

                sumMB(installPathRoute);
                AutoInstallSynchronizationContext.Post(new SendOrPostCallback(o =>
                {
                    ((MainForm)Owner).Cursor = Cursors.WaitCursor;

                    dataGridViewAutoInstall.CurrentRow.Cells[1].Value = 
                        string.Format("Downloaded: {0} kB", (string)o);
                }), (TotalBytes / 1024).ToString("N0"));

                Stopwatch sw = Stopwatch.StartNew();
                while ((downloadThread.IsAlive) && (sw.ElapsedMilliseconds <= 1000)) { }
            }

            if (returnValue)
            {
                if (route.Url.EndsWith(".zip"))
                {
                    Thread installThread = new Thread(() =>
                    {
                        returnValue = doTheUnzipInstall(Path.Combine(installPathRoute, AutoInstallRouteName + ".zip"), installPathRoute);
                    });
                    installThread.Start();

                    long bytesZipfile = TotalBytes;

                    while (installThread.IsAlive)
                    {
                        TotalBytes = -bytesZipfile;
                        sumMB(installPathRoute);
                        AutoInstallSynchronizationContext.Post(new SendOrPostCallback(o =>
                        {
                            ((MainForm)Owner).Cursor = Cursors.WaitCursor;

                            dataGridViewAutoInstall.CurrentRow.Cells[1].Value = 
                                string.Format("Installed: {0} kB", (string)o);
                        }), (TotalBytes / 1024).ToString("N0"));

                        Stopwatch sw = Stopwatch.StartNew();
                        while ((installThread.IsAlive) && (sw.ElapsedMilliseconds <= 1000)) { }
                    }
                }
            }

            AutoInstallSynchronizationContext.Post(new SendOrPostCallback(o =>
            {
                dataGridViewAutoInstall.CurrentRow.Cells[1].Value = "";
            }), "");


            return returnValue;
        }

        private bool doTheClone(string installPathRoute)
        {
            try
            {
                Repository.Clone(AutoInstallRoutes[AutoInstallRouteName].Url, installPathRoute);
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
                    try
                    {
                        sumMB(directoryName);
                    }
                    catch (Exception)
                    {
                        return false;
                    }
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

            string routeName = AutoInstallRouteName;
            int index = 0;
            var sortedFolders = Settings.Folders.Folders.OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value);
            foreach (KeyValuePair<string, string> folderSetting in sortedFolders)
            {
                if (folderSetting.Key == routeName)
                {
                    if (!folderSetting.Value.Equals(installPathRouteReal, StringComparison.OrdinalIgnoreCase))
                    {
                        index++;
                        routeName = string.Format("{0} ({1})", AutoInstallRouteName, index);
                    }
                }
            }
            Settings.Folders.Folders[routeName] = installPathRouteReal;
            AutoInstallRoutes[AutoInstallRouteName].ContentName = routeName;
            AutoInstallRoutes[AutoInstallRouteName].ContentDirectory = installPathRouteReal;

            // insert into Manually Installed data grid
            int indexAdded = dataGridViewManualInstall.Rows.Add(new string[] {
                routeName,
                installPathRouteReal
            });
            dataGridViewManualInstall.Rows[indexAdded].Cells[1].ToolTipText = installPathRouteReal;
            dataGridViewManualInstall.Rows[indexAdded].DefaultCellStyle.ForeColor = Color.Gray;
            dataGridViewManualInstall.Sort(dataGridViewManualInstall.Columns[0], ListSortDirection.Ascending);

            return true;
        }

        private void buttonAutoInstallUpdate_Click(object sender, EventArgs e)
        {
            ContentRouteSettings.Route route = AutoInstallRoutes[AutoInstallRouteName];
            string message;

            DisableAutoInstallButtons();

            List<string> commitStrings = getCommits(route.DirectoryInstalledIn);
            if (commitStrings.Count > 0)
            {
                message = Catalog.GetString("Remote updates found. Do you want to continue?");
                if (MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                {
                    // cancelled
                    EnableAutoInstalButtons();
                    return;
                }
                if (areThereChangedAddedFiles(route))
                {
                    message = Catalog.GetString("Changed or added local files found, Update might fail. Do you want to continue?");
                    if (MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                    {
                        // cancelled
                        EnableAutoInstalButtons();
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
            mainForm.comboBoxFolder.SelectedIndex = determineSelectedIndex(mainForm.comboBoxFolder, AutoInstallRouteName);

            Refresh();

            EnableAutoInstalButtons();
        }

        private bool doThePull(ContentRouteSettings.Route route)
        {
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

        private async void buttonAutoInstallDelete_Click(object sender, EventArgs e)
        {
            ContentRouteSettings.Route route = AutoInstallRoutes[AutoInstallRouteName];
            string message;

            DisableAutoInstallButtons();

            message = Catalog.GetStringFmt("Directory \"{0}\" is to be deleted, are you really sure?", route.DirectoryInstalledIn);
            if (MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                // cancelled
                EnableAutoInstalButtons();
                return;
            }

            if (Directory.Exists(route.DirectoryInstalledIn)) {

                if (areThereChangedAddedFiles(route))
                {
                    message = Catalog.GetStringFmt("Changed or added local files found in Directory \"{0}\". Do you want to continue?", route.DirectoryInstalledIn);
                    if (MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                    {
                        // cancelled
                        EnableAutoInstalButtons();
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
                    int index = findIndexDgvManualInstall(route.ContentName);
                    if (index > -1)
                    {
                        dataGridViewManualInstall.Rows.RemoveAt(index);
                    }
                }
                Settings.Folders.Save();
            }

            route.Installed = false;
            route.DateInstalled = DateTime.MinValue;
            route.DirectoryInstalledIn = "";

            // remove from registry, but leave entry in Routes for this route name
            ContentRouteSettings.Route routeSaved = route;
            Settings.Content.Save();
            AutoInstallRoutes[AutoInstallRouteName] = routeSaved;

            dataGridViewAutoInstall.CurrentRow.Cells[1].Value = "";

            MainForm mainForm = ((MainForm)Owner);
            mainForm.LoadFolderListWithoutTask();

            Refresh();

            EnableAutoInstalButtons();
        }

        private void deleteRoute(string directoryInstalledIn)
        {
            Thread deleteThread = new Thread(() =>
            {
                ContentRouteSettings.directoryDelete(directoryInstalledIn, ref AutoInstallDoingTheSumOfTheFileBytes);
            });
            // start delete in thread to be able to show the progress in the main thread
            deleteThread.Start();

            while (deleteThread.IsAlive)
            {
                // this will stop the delete until the sum is done
                AutoInstallDoingTheSumOfTheFileBytes = true;

                TotalBytes = 0;
                if (sumMB(directoryInstalledIn))
                {
                    AutoInstallSynchronizationContext.Post(new SendOrPostCallback(o =>
                    {
                        ((MainForm)Owner).Cursor = Cursors.WaitCursor;

                        dataGridViewAutoInstall.CurrentRow.Cells[1].Value =
                            string.Format("Left: {0} kB", (string)o);
                    }), (TotalBytes / 1024).ToString("N0"));
                }

                AutoInstallDoingTheSumOfTheFileBytes = false;

                Stopwatch sw = Stopwatch.StartNew();
                while (deleteThread.IsAlive && (sw.ElapsedMilliseconds <= 1000)) { }
            }
        }

        void buttonAutoInstallOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void DisableAutoInstallButtons()
        {
            setCursorToWaitCursor();
            DisableAutoInstallButtonsWithoutWait();
        }

        private void DisableAutoInstallButtonsWithoutWait()
        {
            dataGridViewAutoInstall.Enabled = false;
            textBoxAutoInstallPath.Enabled = false;
            buttonAutoInstallBrowse.Enabled = false;
            buttonAutoInstallInfo.Enabled = false;
            buttonAutoInstallInstall.Enabled = false;
            buttonAutoInstallUpdate.Enabled = false;
            buttonAutoInstallDelete.Enabled = false;
            buttonOK.Enabled = false;
        }

        private void setCursorToWaitCursor()
        {
            AutoInstallClosingBlocked = true;

            ((MainForm)Owner).Cursor = Cursors.WaitCursor;
        }

        private void EnableAutoInstalButtons()
        { 
            ContentRouteSettings.Route route = AutoInstallRoutes[AutoInstallRouteName];

            dataGridViewAutoInstall.Enabled = true;
            textBoxAutoInstallPath.Enabled = true;
            buttonAutoInstallBrowse.Enabled = true;
            buttonAutoInstallInfo.Enabled = !NoInternet;
            buttonAutoInstallInstall.Enabled = !(route.Installed || NoInternet);
            buttonAutoInstallUpdate.Enabled = route.Installed && (route.getDownloadType() == ContentRouteSettings.DownloadType.github) && !NoInternet;
            buttonAutoInstallDelete.Enabled = route.Installed;
            buttonOK.Enabled = true;

            setCursorToDefaultCursor();
        }

        private void setCursorToDefaultCursor()
        {
            ((MainForm)Owner).Cursor = Cursors.Default;

            AutoInstallClosingBlocked = false;
        }

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
                        comboboxName = ((ORTS.Menu.Folder)comboBox.Items[index]).Name;
                        break;
                    case "Route":
                        comboboxName = ((ORTS.Menu.Route)comboBox.Items[index]).Name;
                        break;
                    case "DefaultExploreActivity":
                        comboboxName = ((ORTS.Menu.Activity)comboBox.Items[index]).Name;
                        break;
                    case "Locomotive":
                        comboboxName = ((ORTS.Menu.Locomotive)comboBox.Items[index]).Name;
                        break;
                    case "Consist":
                        comboboxName = ((ORTS.Menu.Consist)comboBox.Items[index]).Name;
                        break;
                    case "String":
                        comboboxName = (string)comboBox.Items[index];
                        break;
                    case "Path":
                        comboboxName = ((ORTS.Menu.Path)comboBox.Items[index]).End;
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
        #endregion

        #region Manually Installed
        //
        // "Manually Installed" tab
        //

        private void dataGridViewManualInstall_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Tab && !(e.Modifiers == Keys.Shift))
            {
                // pressing tab in this datagridview now takes you into the next field
                // default unwanted behaviour is going thru the rows/columns of the datagridview
                buttonManualInstallAdd.Select();
                e.SuppressKeyPress = true;
            }
            if (e.KeyCode == Keys.Tab && e.Modifiers == Keys.Shift)
            {
                // same for pressing shift-tab
                tabControlContent.Select();
                e.SuppressKeyPress = true;
            }
        }

        private void dataGridViewManualInstall_SelectionChanged(object sender, EventArgs e)
        {
            if (!In_buttonManualInstallAdd_Click)
            {
                In_dataGridViewManualInstall_SelectionChanged = true;
                setTextBoxesManualInstall();
                In_dataGridViewManualInstall_SelectionChanged = false;
            }
        }

        private void textBoxManualInstallRoute_TextChanged(object sender, EventArgs e)
        {
            if (!In_dataGridViewManualInstall_SelectionChanged)
            {
                // only update the grid when user is filling/changing the route in the textbox
                dataGridViewManualInstall.CurrentRow.Cells[0].Value = textBoxManualInstallRoute.Text;
                ManualInstallChangesMade = true;
                buttonCancel.Enabled = true;
            }
        }

        private void textBoxManualInstallRoute_Leave(object sender, EventArgs e)
        {
            string route = textBoxManualInstallRoute.Text.Trim();
            textBoxManualInstallRoute.Text = determineUniqueRoute(route);
            if (textBoxManualInstallRoute.Text != route)
            {
                dataGridViewManualInstall.CurrentRow.Cells[0].Value = textBoxManualInstallRoute.Text;
            }
        }

        private void textBoxManualInstallPath_TextChanged(object sender, EventArgs e)
        {
            if (!In_dataGridViewManualInstall_SelectionChanged)
            {
                // only update the grid when user is filling/changing the path in the textbox
                if (!isWrongPath(textBoxManualInstallPath.Text, Catalog))
                {
                    dataGridViewManualInstall.CurrentRow.Cells[1].Value = textBoxManualInstallPath.Text;
                }
            }
        }

        private void buttonManualInstallBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowser = new FolderBrowserDialog())
            {
                folderBrowser.SelectedPath = textBoxManualInstallPath.Text;
                folderBrowser.ShowNewFolderButton = false;
                folderBrowser.Description = Catalog.GetString("Path where the route is installed");
                if (folderBrowser.ShowDialog(this) == DialogResult.OK)
                {
                    textBoxManualInstallPath.Text = folderBrowser.SelectedPath;
                }
            }
        }

        private void buttonManualInstallAdd_Click(object sender, EventArgs e)
        {
            In_buttonManualInstallAdd_Click = true;

            int currentIndex = -1;
            if (dataGridViewManualInstall.CurrentCell != null)
            {
                currentIndex = dataGridViewManualInstall.CurrentCell.RowIndex;
            }
            int addedIndex = dataGridViewManualInstall.Rows.Add();
            dataGridViewManualInstall.Rows[addedIndex].Selected = true;
            dataGridViewManualInstall.FirstDisplayedScrollingRowIndex = addedIndex;
            dataGridViewManualInstall.CurrentCell = dataGridViewManualInstall.Rows[addedIndex].Cells[0];

            using (FolderBrowserDialog folderBrowser = new FolderBrowserDialog())
            {
                folderBrowser.SelectedPath = ManualInstallBrouwseDir;
                folderBrowser.ShowNewFolderButton = false;
                folderBrowser.Description = Catalog.GetString("Path where the route is installed");
                if (folderBrowser.ShowDialog(this) == DialogResult.OK)
                {
                    // OK, fill the empty row
                    string route = determineUniqueRoute(Path.GetFileName(folderBrowser.SelectedPath));
                    dataGridViewManualInstall.CurrentRow.Cells[0].Value = route;
                    dataGridViewManualInstall.CurrentRow.Cells[1].Value = folderBrowser.SelectedPath;
                    ManualInstallChangesMade = true;
                    buttonCancel.Enabled = ManualInstallChangesMade;
                }
                else
                {
                    if (currentIndex > -1)
                    {
                        // Cancel, set focus back to where is was and remove the empty row
                        dataGridViewManualInstall.CurrentCell = dataGridViewManualInstall.Rows[currentIndex].Cells[0];
                        dataGridViewManualInstall.Rows[currentIndex].Selected = true;
                        dataGridViewManualInstall.FirstDisplayedScrollingRowIndex = currentIndex;
                    }

                    dataGridViewManualInstall.Rows.Remove(dataGridViewManualInstall.Rows[addedIndex]);
                }
            }

            setTextBoxesManualInstall();

            In_buttonManualInstallAdd_Click = false;
        }

        private void buttonManualInstallDelete_Click(object sender, EventArgs e)
        {
            if (dataGridViewManualInstall.CurrentRow != null)
            {
                dataGridViewManualInstall.Rows.Remove(dataGridViewManualInstall.CurrentRow);
                ManualInstallChangesMade = true;
                buttonCancel.Enabled = ManualInstallChangesMade;
            }
        }

        private void setTextBoxesManualInstall()
        {
            if (dataGridViewManualInstall.CurrentRow != null)
            {
                string route = dataGridViewManualInstall.CurrentRow.Cells[0].Value.ToString();
                string path = dataGridViewManualInstall.CurrentRow.Cells[1].Value.ToString();

                if (!AutoInstallRoutes.ContainsKey(route))
                {
                    // route not automatically installed
                    textBoxManualInstallRoute.Text = route;
                    textBoxManualInstallPath.Text = path;
                    textBoxManualInstallRoute.Enabled = true;
                    textBoxManualInstallPath.Enabled = true;
                    buttonManualInstallBrowse.Enabled = true;
                    buttonManualInstallDelete.Enabled = true;
                }
                else
                {
                    // route automatically installed
                    textBoxManualInstallRoute.Text = "";
                    textBoxManualInstallPath.Text = "";
                    textBoxManualInstallRoute.Enabled = false;
                    textBoxManualInstallPath.Enabled = false;
                    buttonManualInstallBrowse.Enabled = false;
                    buttonManualInstallDelete.Enabled = false;
                }
            }
            else
            {
                // empty form, like after installing OR
                textBoxManualInstallRoute.Text = "";
                textBoxManualInstallPath.Text = "";
                textBoxManualInstallRoute.Enabled = false;
                textBoxManualInstallPath.Enabled = false;
                buttonManualInstallBrowse.Enabled = false;
                buttonManualInstallDelete.Enabled = false;
            }
        }

        string determineUniqueRoute(string Route)
        {
            string route = Route;
            long seqNr = 0;
            bool foundUniqueRoute = false;

            if (AutoInstallRoutes.ContainsKey(route))
            {
                // route already exists in the AutoInstall routes
                seqNr = 1;
                route = Route + " (" + seqNr + ")";
            }

            while (!foundUniqueRoute)
            {
                bool found = false;
                for (int i = 0; i < dataGridViewManualInstall.Rows.Count; i++)
                {
                    if ((!dataGridViewManualInstall.Rows[i].Selected) && (dataGridViewManualInstall.Rows[i].Cells[0].Value.ToString() == route))
                    {
                        seqNr++;
                        route = Route + " (" + seqNr + ")";
                        found = true;
                    }
                }
                if (!found)
                {
                    foundUniqueRoute = true;
                }
            }

            return route;
        }

        string determineBrowseDir()
        {
            Dictionary<string, int> subDirs = new Dictionary<string, int>();
            foreach (var folder in Settings.Folders.Folders)
            {
                var parentDir = Directory.GetParent(folder.Value);
                if (parentDir == null) { continue; }  // ignore content in top-level dir
                string subDir = parentDir.ToString();
                if (subDirs.ContainsKey(subDir))
                {
                    subDirs[subDir] += 1;
                }
                else
                {
                    subDirs.Add(subDir, 1);
                }
            }

            int max = 0;
            foreach (var subDir in subDirs)
            {
                if (subDir.Value > max)
                {
                    max = subDir.Value;
                }
            }

            string browseDir = "";
            foreach (var subDir in subDirs)
            {
                if (subDir.Value == max)
                {
                    browseDir = subDir.Key;
                }
            }

            return browseDir;
        }

        #endregion

        private static bool isWrongPath(string path, GettextResourceManager catalog)
        {
            if (path.ToLower().Contains(Application.StartupPath.ToLower()))
            {
                // check added because a succesful Update operation will empty the Open Rails folder and lose any content stored within it.
                MessageBox.Show(catalog.GetString
                        ($"Cannot use content from any folder which lies inside the Open Rails program folder {Application.StartupPath}\n\n"),
                    "Invalid content location",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return true;
            }

            return false;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            string message = Catalog.GetString("Cancel: changes made in the 'Manually Installed' tab will not be saved, are you sure?");
            if (MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
            {
                // not sure, cancel the cancel
                this.DialogResult = DialogResult.None;
            }
            else 
            {
                // sure to cancel the changes
                ManualInstallChangesMade = false;
            }
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            // save "Manually Installed" tab changes into the registry/ini file via Settings.Folders.Folders

            // loop settings to check if a setting is deleted or changed
            foreach (var folder in Settings.Folders.Folders.ToList())
            {
                string route = folder.Key;
                string path = folder.Value;
                if (!AutoInstallRoutes.ContainsKey(folder.Key))
                {
                    // not part of the "Auto Installed" routes
                    string pathInSettings = findPathInDgvManualInstall(route);
                    if (string.IsNullOrEmpty(pathInSettings))
                    {
                        // deleted, remove from settings
                        Settings.Folders.Folders.Remove(route);
                    }
                    else
                    {
                        if (!path.Equals(pathInSettings))
                        {
                            // path changed
                            Settings.Folders.Folders[route] = pathInSettings;
                        }
                    }
                }
            }

            // loop datagridview to find new entries

            foreach (DataGridViewRow row in dataGridViewManualInstall.Rows)
            {
                string route = row.Cells[0].Value.ToString();
                string path = row.Cells[1].Value.ToString();

                if (!AutoInstallRoutes.ContainsKey(route))
                {
                    // not part of the "Auto Installed" routes
                    if (!Settings.Folders.Folders.ContainsKey(route))
                    {
                        Settings.Folders.Folders.Add(route, path);

                        // set the main form to this added route
                        MainForm mainForm = ((MainForm)Owner);
                        mainForm.LoadFolderListWithoutTask();
                        mainForm.comboBoxFolder.SelectedIndex = determineSelectedIndex(mainForm.comboBoxFolder, route);
                    }
                }
            }

            Settings.Save();

            ManualInstallChangesMade = false;

            this.Close();
        }

        private string findPathInDgvManualInstall(string route)
        {
            foreach (DataGridViewRow row in dataGridViewManualInstall.Rows)
            {
                if (row.Cells[0].Value.ToString().Equals(route))
                {
                    // return Path found
                    return row.Cells[1].Value.ToString();
                }
            }

            return "";
        }

        private int findIndexDgvManualInstall(string route)
        {
            for (int i = 0; i < dataGridViewManualInstall.Rows.Count; i++)
            {
                if (dataGridViewManualInstall.Rows[i].Cells[0].Value.ToString().Equals(route)) 
                {
                    return i;
                }
            }
            return - 1;
        }

        private void DownloadContentForm_FormClosing(object sender, FormClosingEventArgs formClosingEventArgs)
        {
            if (ManualInstallChangesMade)
            {
                string message = Catalog.GetString("Cancel: changes made in the 'Manually Installed' tab will not be saved, are you sure?");
                if (MessageBox.Show(message, Catalog.GetString("Attention"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                {
                    // not sure, cancel the cancel
                    formClosingEventArgs.Cancel = true;
                    return;
                }
            }

            if (AutoInstallClosingBlocked)
            {
                // cancelled event, so continue
                formClosingEventArgs.Cancel = true;
                return;
            }

            try
            {
                if (pictureBoxAutoInstallRoute.Image != null)
                {
                    pictureBoxAutoInstallRoute.Image.Dispose();
                    pictureBoxAutoInstallRoute.Image = null;
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
