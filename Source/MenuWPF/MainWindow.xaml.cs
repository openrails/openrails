using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Data;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using MSTS;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Windows.Media.Effects;
using System.Drawing;
using System.Diagnostics;
using System.Collections;

namespace MenuWPF
{
	/// <summary>
	/// New Windows Presentation Foundation Main Menu for Open Rails
	/// </summary>
	public partial class MainWindow : Window
    {
        #region Members

        public const string FolderDataFileName = "folder.dat";
        private BackgroundWorker bgWork;
        private Dictionary<EngineInfo, List<CONFile>> EnginesWithConsists;
        private DataTable Paths;
        private ImageSource bgImage;
        private ProgressionWindow winProg;
        private ImageSource defaultImage;
        private bool closedSwitch = false;
        #region ex-Program class
        const string RunActivityProgram = "runactivity.exe";

        public static string Version;         // ie "0.6.1"
        public static string Build;           // ie "0.0.3661.19322 Sat 01/09/2010  10:44 AM"
        public static string RegistryKey;     // ie @"SOFTWARE\OpenRails\ORTS"
        public static string UserDataFolder;  // ie @"C:\Users\Wayne\AppData\Roaming\Open Rails"
        #endregion

        string FolderDataFile;
        List<Folder> Folders;
        List<Route> Routes;
        List<Activity> Activities;

        public Folder SelectedFolder 
        { 
            get 
            { 
                return cboFolder.SelectedIndex < 0 ? null : GetSelectedFolder(); 
            } 
        }

        

        public Route SelectedRoute { get { return listBoxRoutes.SelectedIndex < 0 ? null : Routes[listBoxRoutes.SelectedIndex]; } }
        public Activity SelectedActivity { get { return listBoxActivities.SelectedIndex < 0 ? null : Activities[listBoxActivities.SelectedIndex]; } set { if (listBoxActivities.SelectedIndex >= 0) Activities[listBoxActivities.SelectedIndex] = value; } }
        
        //public Folder SelectedFolder { get { return Folders[0]; } }
        
        public class Folder
        {
            public readonly string Name;
            public readonly string Path;

            public Folder(string name, string path)
            {
                Name = name;
                Path = path;
            }
        }

        public class Route
        {
            public readonly string Name;
            public readonly string Path;
            public readonly TRKFile TRKFile;
            public readonly Folder Folder;  //added to concatenate with the originary MSTS installation folder

            public Route(string name, string path, TRKFile trkFile, Folder folder)
            {
                Name = name;
                Path = path;
                TRKFile = trkFile;
                Folder = folder;
            }

            public override string ToString()
            {
                return this.Name;
            }
        }

        public class Activity
        {
            public readonly string Name;
            public readonly string FileName;
            public readonly ACTFile ACTFile;

            public Activity(string name, string fileName, ACTFile actFile)
            {
                Name = name;
                FileName = fileName;
                ACTFile = actFile;
            }

            public override string ToString()
            {
                return this.Name;
            }
        }

        public class ExploreActivity : Activity
        {
            public readonly string Path;
            public readonly string Consist;
            public readonly int StartHour;
            public readonly int Season;
            public readonly int Weather;

            public ExploreActivity(string path, string consist, int season, int weather, int startHour)
                : base("- Explore Route -", null, null)
            {
                Path = path;
                Consist = consist;
                Season = season;
                Weather = weather;
                StartHour = startHour;
            }

            public ExploreActivity()
                : this("", "", 0, 0, 12)
            {
            }
        }
        #endregion

        #region Constructor
        public MainWindow()
		{
			this.InitializeComponent();
            bgWork = new BackgroundWorker();
            bgWork.DoWork += new DoWorkEventHandler(bgWork_DoWork);
            bgWork.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWork_RunWorkerCompleted);
            SetBuildRevision();
            UserDataFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), System.Windows.Forms.Application.ProductName);
            if (!Directory.Exists(UserDataFolder)) Directory.CreateDirectory(UserDataFolder);
            
            RegistryKey = "SOFTWARE\\OpenRails\\ORTS";
            // Set title to show revision or build info.
            //Content = String.Format(Revision == "000" ? "{0} BUILD {2}" : "{0} V{1}", AppDomain.CurrentDomain.FriendlyName, Revision, Build);
            defaultImage = ((ImageBrush)this.Background).ImageSource;
            bgImage = ((ImageBrush)this.Background).ImageSource;
            RegistryKey RK = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (RK != null && RK.GetValue("BackgroundImage", "") != null)
            {
                if (System.IO.File.Exists(RK.GetValue("BackgroundImage", "").ToString()))
                {
                    bgImage = new BitmapImage(new Uri(RK.GetValue("BackgroundImage", "").ToString(), UriKind.Absolute));
                    ((ImageBrush)this.Background).ImageSource = bgImage;
                }
                RK.Close();
            }
            
            FolderDataFile = UserDataFolder + @"\" + FolderDataFileName;
            //Load the folders
            LoadFolders();
            

            CleanupPre021();
        }

        
        #endregion

        #region Event Handlers
        private void bgWork_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            winProg.DoClose();
        }

        private void bgWork_DoWork(object sender, DoWorkEventArgs e)
        {
            LoadRoutes();
        }

        private void winMain_Closing(object sender, CancelEventArgs e)
        {
            if (closedSwitch == false)
            {
                if (MessageBox.Show("Are you sure you want to quit Open Rails?", "Confirmation", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                {
                    e.Cancel = false;
                }
                else
                {
                    e.Cancel = true;
                }
            }
            else
            {
                e.Cancel = false;
            }
        }

        private void btnStart_Click(object sender, System.Windows.RoutedEventArgs e)
		{
            if (SelectedFolder == null)
            {
                MessageBox.Show("Please select a folder first!", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else if (cboEngine.SelectedIndex == -1)
            {
                MessageBox.Show("Player service has unknown locomotive!", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else if (cboConsist.SelectedIndex == -1)
            {
                MessageBox.Show("Player service has no consist!", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else if (cboPath.SelectedIndex == -1 || String.IsNullOrEmpty(cboPath.Text))
            {
                MessageBox.Show("Invalid starting location!", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else if (cboHeading.SelectedIndex == -1 || String.IsNullOrEmpty(cboHeading.Text))
            {
                MessageBox.Show("Invalid heading direction!", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else
            {
                MainStart(false);

            }
		}

        private void btnDescription_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (cboEngine.SelectedItem != null)
            {
                var eng = from en in EnginesWithConsists
                          where en.Key.Name == cboEngine.SelectedItem.ToString()
                          select en.Key;
                EngineInfo info = eng.Single();

                FlowDocument doc = new FlowDocument();
                string[] lines = info.Description.Replace("\"", "").Replace("\t", "").Replace("\\n", "\n").Replace("+", "").Replace("\r", "").Split(new String[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    Paragraph p = new Paragraph();
                    p.Inlines.Add(new Run(line.Trim()));
                    doc.Blocks.Add(p);
                }
                doc.FontFamily = System.Windows.SystemFonts.MessageFontFamily;
                lines = null;
                EngineInfoWindow winEngine = new EngineInfoWindow(doc, bgImage);
                winEngine.ShowDialog();
            }
        }

        private void listBoxRoutes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Paths = FillPaths(Routes[listBoxRoutes.SelectedIndex].Path);
                if (gridParams.Visibility == Visibility.Hidden)
                {
                    gridParams.Visibility = Visibility.Visible;
                }
                //Fill the starting location comboBox
                DataRow[] rows = Paths.Select("", "Start");
                foreach (DataRow dr in rows)
                {
                    if (!cboPath.Items.Contains(dr["Start"].ToString()))
                    {
                        cboPath.Items.Add(dr["Start"].ToString());
                    }
                }
                cboPath.SelectedIndex = 0;
                cboEngine.ItemsSource = null;
                //Manage the engine list whether the route is electrified or not
                if (Routes[listBoxRoutes.SelectedIndex].TRKFile.Tr_RouteFile.MaxLineVoltage <= 0)
                {
                    var eng = from en in EnginesWithConsists
                              where en.Key.Type != EngineType.Electric
                              orderby en.Key.Name
                              select en.Key.Name;
                    cboEngine.ItemsSource = eng.ToList();
                }
                else
                {
                    var eng = from en in EnginesWithConsists
                              orderby en.Key.Name
                              select en.Key.Name;
                    cboEngine.ItemsSource = eng.ToList();
                }
                cboEngine.SelectedIndex = 0;
                LoadActivities();
                DisplayRouteDetails();
            }
            catch
            {
                if (listBoxRoutes.SelectedIndex == -1)
                {
                    gridParams.Visibility = Visibility.Hidden;
                    btnMenuStyle.Visibility = Visibility.Visible;
                    listBoxActivities.ItemsSource = null;
                    docActivityDescription.Document.Blocks.Clear();
                    docRouteDetail.Document.Blocks.Clear();
                }
            }
        }

        private void listBoxActivities_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Load activity details
            if (listBoxActivities.SelectedIndex != -1)
            {
                DisplayActivityDetails();
            }
        }
        private void cboEngine_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                cboConsist.Items.Clear();
                var con = from f in EnginesWithConsists
                          where f.Key.Name == cboEngine.SelectedItem.ToString()
                          select f.Value;

                foreach (CONFile consist in con.Single())
                {
                    cboConsist.Items.Add(consist);
                }
                cboConsist.SelectedIndex = 0;
            }
            catch
            {
            }
        }

        private void cboPath_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                cboHeading.Items.Clear();
                DataRow[] rows = Paths.Select("Start = '" + cboPath.SelectedItem.ToString().Replace("'", "''") + "'", "End");
                foreach (DataRow dr in rows)
                {
                    if (!cboHeading.Items.Contains(dr["End"].ToString()))
                    {
                        cboHeading.Items.Add(dr["End"].ToString());
                    }
                }
                cboHeading.SelectedIndex = 0;
            }
            catch
            {
            }
        }

        private void cboFolder_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (bgWork.IsBusy == false)
            {
                winProg = new ProgressionWindow();
                bgWork.RunWorkerAsync();
                winProg.Show();
            }
        }

        private void winMain_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void itemSimulation_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OptionsWindow winOptions = new OptionsWindow(RegistryKey, FolderDataFile, 0);

            winOptions.ShowDialog();
            CheckBGImageChanged();
            if (winOptions.FoldersChanged)
            {
                LoadFolders();
                if (Folders.Count > 1)
                {
                    cboFolder.SelectedIndex = 0;
                }
            }
        }

        private void itemTrainStore_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OptionsWindow winOptions = new OptionsWindow(RegistryKey, FolderDataFile, 1);

            winOptions.ShowDialog();
            CheckBGImageChanged();
            if (winOptions.FoldersChanged)
            {
                LoadFolders();
                if (Folders.Count > 1)
                {
                    cboFolder.SelectedIndex = 0;
                }
            }
        }

        private void itemAudio_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OptionsWindow winOptions = new OptionsWindow(RegistryKey, FolderDataFile, 2);

            winOptions.ShowDialog();
            CheckBGImageChanged();
            if (winOptions.FoldersChanged)
            {
                LoadFolders();
                if (Folders.Count > 1)
                {
                    cboFolder.SelectedIndex = 0;
                }
            }
        }

        private void itemVideo_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OptionsWindow winOptions = new OptionsWindow(RegistryKey, FolderDataFile, 3);

            winOptions.ShowDialog();
            CheckBGImageChanged();
            if (winOptions.FoldersChanged)
            {
                LoadFolders();
                if (Folders.Count > 1)
                {
                    cboFolder.SelectedIndex = 0;
                }
            }
        }

        private void itemStart_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            btnStart_Click(itemStart, new RoutedEventArgs());
        }

        private void itemQuit_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void itemUserManual_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // TODO: Add event handler implementation here.
        }

        private void itemAbout_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // TODO: Add event handler implementation here.
        }

        private void btnMenuStyle_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            using (RegistryKey RK = Registry.CurrentUser.OpenSubKey(RegistryKey, true))
            {
                if (RK != null)
                {
                    RK.SetValue("LauncherMenu", 1);
                }
            }
            Process.Start(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), "Menu.exe"));
            closedSwitch = true;
            Close();
            
        }

        private void itemResume_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            MainStart(true);
        }

        #endregion

        #region Methods

        #region MainStart
        /// <summary>
        /// Old method of Windows Form Application to start the program, 
        /// now included in the main window.
        /// </summary>
        void MainStart(bool resume)
        {
            try
            {
                string parameter;

                if (resume)
                {
                    parameter = "-resume";
                }
                else
                {
                    if (SelectedActivity is ExploreActivity)
                    {
                        int hour = 10;
                        int mins = 0;
                        Regex reg = new Regex("^([0-1][0-9]|[2][0-3]):([0-5][0-9])$"); //Match a string format of HH:MM
                        if (reg.IsMatch(cboStartingTime.Text))
                        {
                            int.TryParse(cboStartingTime.Text.Trim().Substring(0, cboStartingTime.Text.Trim().IndexOf(':')), out hour);
                            int.TryParse(cboStartingTime.Text.Trim().Substring(cboStartingTime.Text.Trim().IndexOf(':') + 1), out mins);
                        }
                        else
                        {
                            MessageBox.Show("Invalid starting time", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            return;
                        }
                        parameter = String.Format("\"{0}\" \"{1}\" {2}:{3} {4} {5}", GetPathFileName(cboPath.SelectedItem.ToString(), cboHeading.SelectedItem.ToString()), SelectedFolder.Path + @"\trains\consists\" + ((CONFile)cboConsist.SelectedItem).FileName + ".con", hour, mins, cboSeason.SelectedIndex, cboWeather.SelectedIndex);
                    }
                    else
                        parameter = String.Format("\"{0}\"", SelectedActivity.FileName);
                }

                // find the RunActivity program, normally in the startup path, 
                //  but while debugging it will be in an adjacent directory
                string RunActivityFolder = AppDomain.CurrentDomain.BaseDirectory.ToLower();

                System.Diagnostics.ProcessStartInfo objPSI = new System.Diagnostics.ProcessStartInfo();
                objPSI.FileName = System.IO.Path.Combine(RunActivityFolder, RunActivityProgram);
                objPSI.Arguments = parameter;
                objPSI.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal; // or Hidden, Maximized or Normal 
                objPSI.WorkingDirectory = RunActivityFolder;

                System.Diagnostics.Process objProcess = System.Diagnostics.Process.Start(objPSI);

                while (objProcess.HasExited == false)
                    System.Threading.Thread.Sleep(100);

                int retVal = objProcess.ExitCode;
                
            }
            catch (Exception error)
            {
                MessageBox.Show(error.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Utilities

        /// <summary>
		/// Set up the global Build and Revision variables
		/// from assembly data and the revision.txt file.
		/// </summary>
		private void SetBuildRevision()
		{
            try
            {
                using (StreamReader f = new StreamReader("Version.txt"))
                {
                    Version = f.ReadLine();
                }

                using (StreamReader f = new StreamReader("Revision.txt"))
                {
                    var line = f.ReadLine();
                    var revision = line.Substring(11, line.IndexOf('$', 11) - 11).Trim();
                    if (revision != "000")
                        Version += "." + revision;
                    else
                        Version = "";

                    System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                    Build = assembly.GetName().Version.ToString();  // from assembly
                    Build = Build + " " + f.ReadLine();  // date
                    Build = Build + " " + f.ReadLine(); // time
                }
            }
            catch
            {
                Version = "";
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                Build = assembly.GetName().Version.ToString();
            }
            finally
            {
                this.Title = String.Format(Version.Length > 0 ? "{0} {1}" : "{0} BUILD {2}", this.Title, Version, Build);
            }
		}

        //methods imported from the old Menu Windows Forms Application
        void CleanupPre021()
        {
            // Handle cleanup from pre version 0021
            if (null != Registry.CurrentUser.OpenSubKey("SOFTWARE\\ORTS"))
                Registry.CurrentUser.DeleteSubKeyTree("SOFTWARE\\ORTS");

            if (!File.Exists(FolderDataFile))
            {
                // Handle name change that occured at version 0021
                string oldFolderDataFileName = UserDataFolder + @"\..\ORTS\folder.dat";
                try
                {
                    if (File.Exists(oldFolderDataFileName))
                    {
                        File.Copy(oldFolderDataFileName, FolderDataFile);
                        Directory.Delete(System.IO.Path.GetDirectoryName(oldFolderDataFileName), true);
                    }
                }
                catch
                {
                }
            }
        }

        private void CheckBGImageChanged()
        {
            RegistryKey RK = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (RK.GetValue("BackgroundImage", "") != null)
            {
                if (System.IO.File.Exists(RK.GetValue("BackgroundImage", "").ToString()))
                {
                    bgImage = new BitmapImage(new Uri(RK.GetValue("BackgroundImage", "").ToString(), UriKind.Absolute));
                    ((ImageBrush)this.Background).ImageSource = bgImage;
                }
                else
                {
                    bgImage = defaultImage;
                    ((ImageBrush)this.Background).ImageSource = defaultImage;
                }
            }
            else
            {
                bgImage = defaultImage;
                ((ImageBrush)this.Background).ImageSource = defaultImage;
            }
            RK.Close();
        }
        #endregion

        #region Folders
        //============================================================================================
        /// <summary>
        /// Gets the selected folder from the list.
        /// </summary>
        /// <returns>Folder SelectedFolder</returns>
        private Folder GetSelectedFolder()
        {
            var fold = from f in Folders
                       where f.Name == cboFolder.SelectedItem.ToString()
                       select f;
            try
            {
                return fold.First();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Loads all folders from the configuration file
        /// </summary>
        private void LoadFolders()
        {
            Folders = new List<Folder>();
            
            if (File.Exists(FolderDataFile))
            {
                try
                {
                    cboFolder.Items.Clear();
                    using (var inf = new BinaryReader(File.Open(FolderDataFile, FileMode.Open)))
                    {
                        var count = inf.ReadInt32();
                        for (var i = 0; i < count; ++i)
                        {
                            var path = inf.ReadString();
                            var name = inf.ReadString();
                            Folders.Add(new Folder(name, path));
                            cboFolder.Items.Add(name);
                        }
                    }
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            if (Folders.Count == 0)
            {
                try
                {
                    Folders.Add(new Folder("- Default -", MSTSPath.Base()));
                    cboFolder.Items.Add("- Default -");
                    
                }
                catch (Exception)
                {
                    MessageBox.Show("Microsoft Train Simulator doesn't appear to be installed.\nGo to Options >> Folders, then click on 'Add' to point Open Rails at your Microsoft Train Simulator folder.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            if (Folders.Count == 1)
            {
                cboFolder.SelectedIndex = 0;
                System.Windows.Forms.Application.DoEvents();
            }

            Folders = Folders.OrderBy(f => f.Name).ToList();

            
        }

        #endregion

        #region Routes
        //===========================================================================
       

        private void LoadRoutes()
        {
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(new LoadRoutesDelegate(LoadRoutes));
            }
            else
            {
                Routes = new List<Route>();
                try
                {
                    foreach (string directory in Directory.GetDirectories(SelectedFolder.Path + @"\ROUTES"))
                    {
                        try
                        {
                            TRKFile trkFile = new TRKFile(MSTSPath.GetTRKFileName(directory));
                            Routes.Add(new Route(trkFile.Tr_RouteFile.Name, directory, trkFile, Folders[cboFolder.SelectedIndex]));
                        }
                        catch
                        {
                        }
                    }

                    Routes = Routes.OrderBy(r => r.Name).ToList();

                    try
                    {
                        FillConsists();
                        listBoxRoutes.ItemsSource = null;
                    }
                    catch
                    {
                    }
                    listBoxRoutes.ItemsSource = Routes;
                    //foreach (var route in Routes)
                    //    listBoxRoutes.Items.Add(route.Name);

                    if (Routes.Count > 0)
                    {
                        listBoxRoutes.SelectedIndex = 0;
                    }
                    else
                        listBoxRoutes.UnselectAll();

                    //if (Routes.Count == 0)   //for what does this serve ? If no route, no game !! ??
                    //LoadActivities();
                }
                catch (IOException)
                {
                    listBoxRoutes.ItemsSource = null;
                    //The Routes directory does not exist
                }
                catch (NullReferenceException)
                {
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        

        //===========================================================================================
        private void DisplayRouteDetails()
        {
            if (listBoxRoutes.SelectedIndex >= 0)
            {
                FlowDocument flowDoc = new FlowDocument();
                //Generate the FlowDocument from the text parsed from the TRK files
                Paragraph pTitle = new Paragraph();
                pTitle.Inlines.Add(new Bold(new Run(Routes[listBoxRoutes.SelectedIndex].TRKFile.Tr_RouteFile.Name)));
                string[] lines = Routes[listBoxRoutes.SelectedIndex].TRKFile.Tr_RouteFile.Description.Split(new String[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                flowDoc.Blocks.Add(pTitle);

                foreach (string line in lines)
                {
                    Paragraph paragraph = new Paragraph();
                    paragraph.Inlines.Add(new Run(line));
                    flowDoc.Blocks.Add(paragraph);
                }
                //#7FFFFFFF
                flowDoc.FontFamily = FontFamily;
                flowDoc.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(Convert.ToByte("00", 16), 251, 251, 251));
                docRouteDetail.Document = flowDoc;
                lines = null;
            }
        }
        #endregion

        #region Activities
        //===========================================================================
        private void LoadActivities()
        {
            Activities = new List<Activity>();

            if (SelectedRoute != null)
            {
                try
                {
                    Activities.Add(new ExploreActivity());
                    foreach (var file in Directory.GetFiles(SelectedRoute.Path + @"\ACTIVITIES", "*.act"))
                    {
                        if (System.IO.Path.GetFileName(file).StartsWith("ITR_e1_s1_w1_t1", StringComparison.OrdinalIgnoreCase))
                            continue;
                        try
                        {
                            var actFile = new ACTFile(file, false);
                            Activities.Add(new Activity(actFile.Tr_Activity.Tr_Activity_Header.Name, file, actFile));
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                catch (IOException)
                {
                    //The Activity Folder does not exist
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.ToString(), AppDomain.CurrentDomain.FriendlyName);
                }
            }

            Activities = Activities.OrderBy(a => a.Name).ToList();
            listBoxActivities.ItemsSource = Activities;
            
            //foreach (var activity in Activities)
            //    listBoxActivities.Items.Add(activity.Name);

            if (Activities.Count > 0)
                listBoxActivities.SelectedIndex = 0;
            else
                listBoxActivities.UnselectAll();
        }

        //================================================================================
        private void DisplayActivityDetails()
        {
            try
            {
                if (listBoxActivities.SelectedIndex > 0)
                {
                    //Display activity details
                    docActivityDescription.Document.Blocks.Clear();
                    string[] lines = Activities[listBoxActivities.SelectedIndex].ACTFile.Tr_Activity.Tr_Activity_Header.Description.Split(new String[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                    {
                        Paragraph p = new Paragraph();
                        p.Inlines.Add(new Run(line));
                        docActivityDescription.Document.Blocks.Add(p);
                    }
                    lines = null;
                    cboStartingTime.Text = Activities[listBoxActivities.SelectedIndex].ACTFile.Tr_Activity.Tr_Activity_Header.StartTime.FormattedStartTime();
                    cboStartingTime.Visibility = Visibility.Hidden;
                    lblActStartingTime.Visibility = Visibility.Visible;
                    lblActStartingTime.Content = cboStartingTime.Text;
                    //Show the activity special fields
                    lblDescription.Visibility = Visibility.Visible;
                    lblDifficulty.Visibility = Visibility.Visible;
                    lblDuration.Visibility = Visibility.Visible;
                    labelDifficulty.Visibility = Visibility.Visible;
                    labelDuration.Visibility = Visibility.Visible;
                    docActivityDescription.Visibility = Visibility.Visible;
                    //================================
                    labelDuration.Content = Activities[listBoxActivities.SelectedIndex].ACTFile.Tr_Activity.Tr_Activity_Header.Duration.FormattedDurationTime();
                    cboSeason.SelectedIndex = (int)Activities[listBoxActivities.SelectedIndex].ACTFile.Tr_Activity.Tr_Activity_Header.Season;
                    cboSeason.Visibility = Visibility.Hidden;
                    lblActSeason.Visibility = Visibility.Visible;
                    lblActSeason.Content = cboSeason.Text;
                    cboWeather.SelectedIndex = (int)Activities[listBoxActivities.SelectedIndex].ACTFile.Tr_Activity.Tr_Activity_Header.Weather;
                    cboWeather.Visibility = Visibility.Hidden;
                    lblActWeather.Visibility = Visibility.Visible;
                    lblActWeather.Content = cboWeather.Text;
                    labelDifficulty.Content = Activities[listBoxActivities.SelectedIndex].ACTFile.Tr_Activity.Tr_Activity_Header.Difficulty.ToString();

                    //cboEngine.SelectedIndex = -1;
                    cboEngine.Visibility = Visibility.Hidden;
                    lblActLocomotive.Visibility = Visibility.Visible;

                    cboPath.Visibility = Visibility.Hidden;
                    lblActStartingAt.Visibility = Visibility.Visible;
                    cboHeading.Visibility = Visibility.Hidden;
                    lblActHeading.Visibility = Visibility.Visible;
                    cboConsist.Visibility = Visibility.Hidden;
                    lblActConsist.Visibility = Visibility.Visible;
                    //Display the engine and the consist
                    string service = Activities[listBoxActivities.SelectedIndex].ACTFile.Tr_Activity.Tr_Activity_File.Player_Service_Definition.Name;
                    using (StreamReader sr = new StreamReader(SelectedRoute.Path + "\\SERVICES\\" + service + ".srv"))
                    {
                        string consist = ParseTag("Train_Config", sr.ReadToEnd());
                        sr.Close();
                        try
                        {
                            StreamReader sr2 = new StreamReader(SelectedFolder.Path + @"\trains\consists\" + consist + ".con");
                            string content = sr2.ReadToEnd();
                            //string consistName = ParseTag("Name", content);
                            string engineID = ParseTag("EngineData", ParseTag("Engine", content, "", true));
                            engineID = engineID.Substring(0, engineID.IndexOf(" "));
                            var engName = from en in EnginesWithConsists
                                          where en.Key.ID == engineID
                                          select en.Key.Name;
                            if (engName.Count() > 0)
                            {
                                cboEngine.SelectedIndex = cboEngine.Items.IndexOf(engName.Single());
                                System.Windows.Forms.Application.DoEvents();
                                lblActLocomotive.Content = cboEngine.SelectedItem.ToString();
                                var selcon = from cons in (cboConsist.Items.SourceCollection).Cast<CONFile>()
                                             where cons.FileName.Equals(consist, StringComparison.CurrentCultureIgnoreCase)
                                             select cons;
                                cboConsist.SelectedIndex = cboConsist.Items.IndexOf(selcon.SingleOrDefault());
                                lblActConsist.Content = cboConsist.SelectedItem.ToString();
                            }
                            else
                            {
                                cboEngine.Text = "UNKNOWN";
                                lblActLocomotive.Content = "UNKNOWN";
                                cboConsist.SelectedIndex = -1;
                                lblActConsist.Content = "n/a";
                            }
                        }
                        catch
                        {
                            cboEngine.Text = "UNKNOWN";
                            lblActLocomotive.Content = "UNKNOWN";
                            cboConsist.SelectedIndex = -1;
                            lblActConsist.Content = "n/a";
                        }
                    }
                    //Display the starting point and the heading direction
                    string pathID = Activities[listBoxActivities.SelectedIndex].ACTFile.Tr_Activity.Tr_Activity_Header.PathID;
                    DataRow[] rows = Paths.Select("PathID = '" + pathID + "'");
                    if (rows.Length > 0)
                    {
                        cboPath.SelectedIndex = cboPath.Items.IndexOf(rows[0]["Start"].ToString());
                        System.Windows.Forms.Application.DoEvents();
                        lblActStartingAt.Content = cboPath.SelectedItem.ToString();
                        cboHeading.SelectedIndex = cboHeading.Items.IndexOf(rows[0]["End"].ToString());
                        lblActHeading.Content = cboHeading.SelectedItem.ToString();
                    }
                    btnStart.Visibility = Visibility.Visible;
                }
                else
                {
                    docActivityDescription.Document.Blocks.Clear();
                    cboStartingTime.SelectedIndex = 10;
                    cboStartingTime.Visibility = Visibility.Visible;
                    lblActStartingTime.Visibility = Visibility.Hidden;

                    //Hide the activity special fields
                    lblDescription.Visibility = Visibility.Hidden;
                    lblDifficulty.Visibility = Visibility.Hidden;
                    lblDuration.Visibility = Visibility.Hidden;
                    labelDifficulty.Visibility = Visibility.Hidden;
                    labelDuration.Visibility = Visibility.Hidden;
                    docActivityDescription.Visibility = Visibility.Hidden;
                    //================================
                    cboSeason.SelectedIndex = 1;
                    cboSeason.Visibility = Visibility.Visible;
                    lblActSeason.Visibility = Visibility.Hidden;
                    cboWeather.SelectedIndex = 0;
                    lblActWeather.Visibility = Visibility.Hidden;
                    cboWeather.Visibility = Visibility.Visible;
                    cboPath.SelectedIndex = 0;
                    cboPath.Visibility = Visibility.Visible;
                    lblActStartingAt.Visibility = Visibility.Hidden;
                    //cboEngine.SelectedIndex = 0;
                    cboEngine.Visibility = Visibility.Visible;
                    lblActLocomotive.Visibility = Visibility.Hidden;
                    cboHeading.Visibility = Visibility.Visible;
                    lblActHeading.Visibility = Visibility.Hidden;

                    cboConsist.Visibility = Visibility.Visible;
                    lblActConsist.Visibility = Visibility.Hidden;
                    if (listBoxActivities.SelectedIndex == 0) btnStart.Visibility = Visibility.Visible;
                    else btnStart.Visibility = Visibility.Hidden;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                btnStart.Visibility = Visibility.Hidden;
            }
        }
        #endregion

        #region Paths
        /// <summary>
        /// Method to fill the paths combo box
        /// </summary>
        /// <param name="route">The path of the route to load the paths for</param>
        DataTable FillPaths(string route)
        {
            DataTable paths = new DataTable();
            paths.Columns.Add("PathID");
            paths.Columns.Add("Start");
            paths.Columns.Add("End");
            paths.Columns.Add("PathFileName");

            string[] patfiles = Directory.GetFiles(route + @"\paths", "*.pat");
            cboPath.Items.Clear();
            cboHeading.Items.Clear();
            foreach (string file in patfiles)
            {
                var patFile = new PATFile(file);
                if (patFile.IsPlayerPath)
                {
                    paths.Rows.Add(patFile.PathID, patFile.Start, patFile.End, file);
                }

            }
            paths.AcceptChanges();
            
            patfiles = null;
            return paths;
        }
        /// <summary>
        /// Gets the pathID
        /// </summary>
        /// <param name="start">The Starting Location</param>
        /// <param name="end">The Heading Towards Location</param>
        /// <returns>PathID</returns>
        private string GetPathID(string start, string end)
        {
            DataRow[] rows = Paths.Select("Start = '" + start.Replace("'", "''") + "' AND End = '" + end.Replace("'", "''") + "'");
            if (rows.Length > 0)
            {
                return rows[0]["PathID"].ToString();
            }
            else
            {
                return string.Empty;
            }
        }
        /// <summary>
        /// Gets the path file name in case it is different from the pathID
        /// </summary>
        /// <param name="start">The Starting Location</param>
        /// <param name="end">The Heading Towards Location</param>
        /// <returns>PathID</returns>
        private string GetPathFileName(string start, string end)
        {
            DataRow[] rows = Paths.Select("Start = '" + start.Replace("'", "''") + "' AND End = '" + end.Replace("'", "''") + "'");
            if (rows.Length > 0)
            {
                return rows[0]["PathFileName"].ToString();
            }
            else
            {
                return string.Empty;
            }
        }
        #endregion

        #region Consists
        /// <summary>
        /// Method to fill the list of engines and consists
        /// </summary>
        private void FillConsists()
        {
            if (this.Dispatcher.CheckAccess())
            {
                
                string[] confiles = Directory.GetFiles(SelectedFolder.Path + @"\trains\consists", "*.con");
                winProg.MaxValue = confiles.Length;
                EnginesWithConsists = new Dictionary<EngineInfo, List<CONFile>>(new EngineInfoEqualityComparer());
                foreach (string file in confiles)
                {
                    using (StreamReader sr = new StreamReader(file))
                    {
                        string fileContent = sr.ReadToEnd();
                        //Check that the consist file contains an engine
                        if (fileContent.ToLower().Contains("enginedata"))
                        {
                            string couple = fileContent.Substring(fileContent.ToLower().IndexOf("enginedata") + 10);
                            couple = couple.Substring(couple.IndexOf("(") + 1);
                            couple = couple.Substring(0, couple.IndexOf(")")).Trim();
                            string key = "";
                            if (couple.Substring(0, 1) == "\"")
                            {
                                key = couple.Substring(1);
                                key = key.Substring(0, key.IndexOf("\"")).Trim();
                            }
                            else
                            {
                                key = couple.Substring(0, couple.IndexOf(" "));
                            }

                            string engineFolder = couple.Substring(key.Length);
                            engineFolder = engineFolder.Replace("\"", "").Trim();
                            //Check if the engine file exists
                            if (File.Exists(SelectedFolder.Path + @"\trains\trainset\" + engineFolder + "\\" + key + ".eng"))
                            {
                                StreamReader srEngine = new StreamReader(SelectedFolder.Path + @"\trains\trainset\" + engineFolder + "\\" + key + ".eng");
                                string engineContent = srEngine.ReadToEnd();
                                EngineInfo engKey = GetEngineInfo(engineContent);
                                engKey.ID = key;
                                if (String.IsNullOrEmpty(engKey.Name)) engKey.Name = engKey.ID;
                                //Check if it contains a Cabview => player driveable engine
                                if (engineContent.ToLower().Contains("cabview") && engineContent.ToLower().IndexOf("cabview") < engineContent.ToLower().IndexOf("description"))
                                {
                                    
                                    if (EnginesWithConsists.ContainsKey(engKey))
                                    {
                                        EnginesWithConsists[engKey].Add(new CONFile(file));
                                    }
                                    else
                                    {
                                        EnginesWithConsists.Add(engKey, new List<CONFile>());
                                        EnginesWithConsists[engKey].Add(new CONFile(file));
                                        //cboEngine.Items.Add(engKey.Name);
                                    }
                                    
                                }
                                srEngine.Close();
                                srEngine = null;
                                engineContent = null;
                            }
                            else
                            {
                                //Display error message ?
                            }
                            
                        }
                    }
                    winProg.IncreaseBy(1);
                    System.Windows.Forms.Application.DoEvents();
                }
                    //cboConsist.Items.Add(System.IO.Path.GetFileName(file));
                    //consists.Add(file);
                confiles = null;
                cboEngine.SelectionChanged += new SelectionChangedEventHandler(cboEngine_SelectionChanged);
                
                
            }
            else
            {
                this.Dispatcher.Invoke(new FillConsistsDelegate(FillConsists));
            }
        }

        /// <summary>
        /// Create an EngineInfo object with the data found in the eng file
        /// </summary>
        /// <param name="engContent">The text content of the eng file</param>
        /// <returns>EngineInfo</returns>
        private EngineInfo GetEngineInfo(string engContent)
        {
            EngineInfo eng = new EngineInfo();

            try
            {
                eng.Coupling = (CouplingType)Enum.Parse(typeof(CouplingType), ParseTag("Type", ParseTag("Coupling", engContent)), true);
                eng.Description = ParseTag("Description", engContent);
                /*eng.FreightAnim = ParseTag("FreightAnim", engContent);
                //eng.ID = ParseTag("Wagon", engContent);
                string len = ParseTag("Size", engContent);
                len = len.Substring(len.LastIndexOf(" ") + 1);
                len = len.Replace("m", "").Replace('.', ',');
                eng.Length = double.Parse(len);

                string ms = ParseTag("Mass", engContent);
                eng.Mass = double.Parse(ms.Substring(0, ms.IndexOf("t")).Replace('.', ','));

                string mcf = ParseTag("MaxContinuousForce", engContent).ToLower();
                if (mcf.Contains("kn")) eng.MaxContinuousForce = double.Parse(mcf.Substring(0, mcf.IndexOf("kn")).Replace('.', ',').Trim());
                else if (mcf.Contains("lbf")) eng.MaxContinuousForce = double.Parse(mcf.Substring(0, mcf.IndexOf("lbf")).Replace('.', ',').Trim());
                
                string mf = ParseTag("MaxForce", engContent).ToLower();
                if (mf.Contains("kn")) eng.MaxForce = double.Parse(mf.Substring(0, mf.IndexOf("kn")).Replace('.', ',').Trim());
                else if (mf.Contains("lbf")) eng.MaxForce = double.Parse(mf.Substring(0, mf.IndexOf("lbf")).Replace('.', ',').Trim());
                
                string mp = ParseTag("MaxPower", engContent).ToLower();
                if (mp.Contains("kw")) eng.MaxPower = double.Parse(mp.Substring(0, mp.IndexOf("kw")).Replace('.', ',').Trim());
                else if (mp.Contains("hp")) eng.MaxPower = double.Parse(mp.Substring(0, mp.IndexOf("hp")).Replace('.', ',').Trim());*/
                eng.Name = ParseTag("Name", engContent);
                //eng.Shape = ParseTag("WagonShape", engContent);
                eng.Type = (EngineType)Enum.Parse(typeof(EngineType), ParseTag("Type", ParseTag("Engine (", engContent.Substring(engContent.IndexOf("Engine") + 6), "Engine(", true)), true);
                
            }
            catch
            {

            }
            return eng;
        }
        #endregion

        #region Parsings
        /// <summary>
        /// Gets the value between the brackets ( ) of a given tag
        /// </summary>
        /// <param name="tagName">The name of the tag</param>
        /// <param name="fileContent">The text where to search the tag and its value</param>
        /// <returns>Value of the tag</returns>
        private string ParseTag(string tagName, string fileContent)
        {
            string tagValue = "";
            if (fileContent.Contains(tagName))
            {
                tagValue = fileContent.Substring(fileContent.IndexOf(tagName) + tagName.Length);
                //Count the number of ( and ). The numbers must match to properly close the tag

                tagValue = tagValue.Substring(tagValue.IndexOf("(") + 1);
                int counter = 0;
                for (int i = 0; i < tagValue.Length; i++)
                {
                    if (tagValue.Substring(i, 1) == "(")
                    {
                        counter++;
                    }
                    else if (tagValue.Substring(i, 1) == ")")
                    {
                        counter--;
                    }
                    if (counter == -1)
                    {
                        counter = i;
                        break;
                    }
                }

                tagValue = tagValue.Substring(0, counter).Trim();
                tagValue = tagValue.Replace("\"", "");
            }
            return tagValue;
        }

        /// <summary>
        /// Gets the value between the brackets ( ) of a given tag
        /// </summary>
        /// <param name="tagName">The name of the tag</param>
        /// <param name="fileContent">The text where to search the tag and its value</param>
        /// <param name="alternateTag">If the first tag is not found look for a second tag</param>
        /// <param name="parentTag">true if the tag contains another tag to search</param>
        /// <returns>Value of the tag</returns>
        private string ParseTag(string tagName, string fileContent, string alternateTag, bool parentTag)
        {
            string tagValue = "";
            if (fileContent.Contains(tagName))
            {
                tagValue = fileContent.Substring(fileContent.IndexOf(tagName) + tagName.Length);
                tagValue = tagValue.Substring(tagValue.IndexOf("(") + 1);
                if (!parentTag)
                {
                    if (tagValue.Contains(")")) tagValue = tagValue.Substring(0, tagValue.IndexOf(")")).Trim();
                    tagValue = tagValue.Replace("\"", "").Trim();
                }
            }
            else if (fileContent.Contains(alternateTag))
            {
                tagValue = fileContent.Substring(fileContent.IndexOf(alternateTag) + alternateTag.Length);
                tagValue = tagValue.Substring(tagValue.IndexOf("(") + 1);
                if (!parentTag)
                {
                    if (tagValue.Contains(")")) tagValue = tagValue.Substring(0, tagValue.IndexOf(")")).Trim();
                    tagValue = tagValue.Replace("\"", "").Trim();
                }
            }
            return tagValue;
        }

        
        

        #endregion

        #endregion

        #region Delegates

        private delegate void FillConsistsDelegate();

        private delegate void LoadRoutesDelegate();

        #endregion

        

    }
}