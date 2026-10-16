using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Orts.TimetableEditor.ViewModel;
using System.IO;
using Orts.TimetableEditor.Model;
using Orts.TimetableEditor.Views;
using static System.Net.Mime.MediaTypeNames;

namespace Orts.TimetableEditor
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<TabItem> tabs;
        private MainViewModel mainViewModel;
        private string RoutePath;
        private string tdbfilename;
        private string consistpath;
        public MainWindow()
        {
            InitializeComponent();
            mainViewModel = new MainViewModel();
            MainView.DataContext = mainViewModel;            
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void GetConsistsPath()
        {
            string[] patharray = RoutePath.Split(Path.DirectorySeparatorChar);
            consistpath = patharray[0];
            for (int i = 1; i < patharray.Length - 2; i++)
            {
                consistpath = consistpath + "\\" + patharray[i];
            }
            consistpath = consistpath + "\\" + "Trains\\Consists";            
        }

        private void Menu_NewTimetable(object sender, RoutedEventArgs e)
        {
            if (RoutePath == null || RoutePath == "")
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "*.tdb|*.tdb";
                if (openFileDialog.ShowDialog() == true)
                {
                    RoutePath = Path.GetDirectoryName(openFileDialog.FileName);
                    tdbfilename = openFileDialog.FileName;
                    mainViewModel.NewTimetable(tdbfilename, RoutePath);
                    GetConsistsPath();
                    mainViewModel.ConsistsAndPaths.ConsistsPath = consistpath;
                    mainViewModel.ConsistsAndPaths.PathsPath = RoutePath + "\\Paths";
                    Menu_Save.IsEnabled = true;
                    MenuItem_Pools.IsEnabled = true;
                    Menu_ZipTimetable.IsEnabled = true;
                }
            }
            else
            {
                mainViewModel.NewTimetable(tdbfilename,RoutePath);

            }
        }

        private void Menu_LoadTimetableList(object sender, RoutedEventArgs e)
        {
            if(RoutePath==null || RoutePath=="")
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Open Rails Timetable List|*.timetablelist-or";
                if(openFileDialog.ShowDialog() == true)
                {
                    string path = Path.GetDirectoryName(openFileDialog.FileName);
                    if (path.ToLower().Contains("activities\\openrails"))
                    {
                        RoutePath = path.Substring(0, path.Length - "activities\\openrails\\".Length);
                        string[] files = Directory.GetFiles(RoutePath);
                        foreach (string file in files)
                        {
                            if (file.ToLower().EndsWith(".tdb"))
                            {
                                tdbfilename = file;
                            }
                        }
                        GetConsistsPath();
                        mainViewModel.ConsistsAndPaths.ConsistsPath = consistpath;
                        mainViewModel.ConsistsAndPaths.PathsPath = RoutePath + "\\Paths";
                        string timetablesListFile = File.ReadAllText(openFileDialog.FileName);
                        string[] timetables = timetablesListFile.Split('\n');
                        foreach(string timetable in timetables)
                        {
                            if(!timetable.StartsWith("#") && timetable!="")
                            {
                                mainViewModel.LoadTimetable(tdbfilename, RoutePath, RoutePath + "\\activities\\openrails\\" + timetable.Trim());
                            }
                        }
                        Menu_Save.IsEnabled = true;
                        Menu_SaveList.IsEnabled = true;
                        Menu_CloseTT.IsEnabled = true;
                        MenuItem_Pools.IsEnabled = true;
                        Menu_ZipTimetable.IsEnabled = true;
                    }
                }
            }
        }

        private void Menu_LoadTimetable(object sender, RoutedEventArgs e)
        {
            if (RoutePath == null || RoutePath == "")
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Open Rails Timetable|*.timetable-or;*.timetable_or";
                if (openFileDialog.ShowDialog() == true)
                {
                    string path = Path.GetDirectoryName(openFileDialog.FileName);
                    if (path.ToLower().Contains("activities\\openrails"))
                    {

                        RoutePath = path.Substring(0, path.Length - "activities\\openrails\\".Length);
                        string[] files = Directory.GetFiles(RoutePath);
                        foreach (string file in files)
                        {
                            if (file.ToLower().EndsWith(".tdb"))
                            {
                                tdbfilename = file;
                            }
                        }
                        GetConsistsPath();
                        mainViewModel.LoadTimetable(tdbfilename, RoutePath, openFileDialog.FileName);
                        mainViewModel.ConsistsAndPaths.ConsistsPath = consistpath;
                        mainViewModel.ConsistsAndPaths.PathsPath = RoutePath + "\\Paths";
                        mainViewModel.LoadPoolTrains(path);
                    }
                    Menu_Save.IsEnabled = true;
                    Menu_SaveList.IsEnabled = true;
                    Menu_CloseTT.IsEnabled = true;
                    MenuItem_Pools.IsEnabled = true;
                    Menu_ZipTimetable.IsEnabled = true;
                }
            }
            else
            {
                OpenSaveViewModel osvm = new OpenSaveViewModel();
                osvm.SetModus("Open");
                List<string> filter = new List<string>();
                filter.Add("*.timetable-or");
                filter.Add("*.timetable_or");
                osvm.ListFiles(RoutePath + "\\activities\\openrails", filter);
                OpenSaveView osv = new OpenSaveView();
                osv.Title = "Open file";
                osv.DataContext = osvm;
                osv.ShowDialog();
                if(osv.DialogResult==true)
                {
                    mainViewModel.LoadTimetable(tdbfilename, RoutePath, RoutePath + "\\activities\\openrails\\" + osvm.SelectedFileName);
                    Menu_Save.IsEnabled=true;
                    Menu_SaveList.IsEnabled = true;
                    Menu_CloseTT.IsEnabled = true;
                    MenuItem_Pools.IsEnabled = true;
                    Menu_ZipTimetable.IsEnabled = true;
                }
            }            
        }

        private void Menu_SaveTimetable(object sender, RoutedEventArgs e)
        {
            OpenSaveViewModel osvm = new OpenSaveViewModel();
            osvm.SetModus("Save");
            List<string> filter = new List<string>();
            filter.Add("*.timetable-or");
            filter.Add("*.timetable_or");
            osvm.ListFiles(RoutePath + "\\activities\\openrails", filter);
            if (mainViewModel.Tabs[mainViewModel.SelectedTabIndex].Header.ToString().Contains(".timetable"))
            {
                string head = mainViewModel.Tabs[mainViewModel.SelectedTabIndex].Header.ToString();
                osvm.SelectedFileName = head;
            }
            OpenSaveView osv = new OpenSaveView();
            osv.Title = "Save file";
            osv.DataContext = osvm;
            osv.ShowDialog();
            string ext = Path.GetExtension(osvm.Filename);
            if(ext!=".timetable-or" && ext!=".timetable_or")
            {
                osvm.Filename = Path.ChangeExtension(osvm.Filename, ".timetable-or");
            }
            if (osv.DialogResult == true)
            {
                bool can = true;                
                if (File.Exists(RoutePath + "\\activities\\openrails\\" + osvm.Filename))
                //if(osvm.Files.IndexOf(osvm.Filename)!=-1)
                {
                    if(MessageBox.Show("File already exists", "Overwrite File?",MessageBoxButton.YesNo)==MessageBoxResult.No)
                    {
                        can = false;
                    }
                }
                if(can)
                {
                    mainViewModel.SaveTimetable(RoutePath + "\\activities\\openrails\\" + osvm.Filename);
                }
            }
        }

        private void Menu_CloseTimetable(object sender, RoutedEventArgs e)
        {
            if(mainViewModel.SelectedTabIndex>-1)
            {
                int oldtabindex = mainViewModel.SelectedTabIndex;
                if(MessageBox.Show("Do you really want to close the timetable?","Close Timetable",MessageBoxButton.YesNo)==MessageBoxResult.Yes)
                {
                    mainViewModel.Tables.RemoveAt(mainViewModel.SelectedTabIndex);
                    mainViewModel.Tabs.RemoveAt(mainViewModel.SelectedTabIndex);
                    if(mainViewModel.Tabs.Count>0)
                    {
                        if(oldtabindex>0)
                        {
                            mainViewModel.SelectedTabIndex = oldtabindex - 1;
                        }
                        else
                        {
                            mainViewModel.SelectedTabIndex = oldtabindex;
                        }
                    }
                    else
                    {
                        Menu_Save.IsEnabled = false;
                        Menu_SaveList.IsEnabled = false;
                        Menu_CloseTT.IsEnabled = false;
                        MenuItem_Pools.IsEnabled = false;
                        Menu_ZipTimetable.IsEnabled = false;
                    }
                }
            }
        }

        private void Menu_SaveTimetableList(object sender, RoutedEventArgs e)
        {
            OpenSaveViewModel osvm = new OpenSaveViewModel();
            osvm.SetModus("Save");
            List<string> filter = new List<string>();
            filter.Add("*.timetablelist-or");
            osvm.ListFiles(RoutePath + "\\activities\\openrails", filter);
            OpenSaveView osv = new OpenSaveView();
            osv.Title = "Save file";
            osv.DataContext = osvm;
            osv.ShowDialog();
            if (osv.DialogResult == true)
            {
                bool can = true;
                if(File.Exists(RoutePath + "\\activities\\openrails\\" + osvm.Filename + ".timetablelist-or"))
                {
                    if (MessageBox.Show("File already exists", "Overwrite File?", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        can = false;
                    }
                }
                if(can)
                {
                    mainViewModel.SaveTimetablelist(RoutePath + "\\activities\\openrails\\" + osvm.Filename + ".timetablelist-or");
                }
            }
        }

        private void Pool_Click(object sender, RoutedEventArgs e)
        {
            PoolViewModel poolvm = new PoolViewModel();
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                poolvm.ReadPoolFile(openFileDialog.FileName);
            }
            PoolView poolView = new PoolView();
            poolView.DataContext = poolvm;
            poolView.ShowDialog();
        }

        private void New_Pool_Click(object sender, RoutedEventArgs e)
        {
            PoolViewModel poolvm = new PoolViewModel();
            poolvm.RoutePath = RoutePath + "\\activities\\openrails\\";
            poolvm.ConsistsAndPaths = mainViewModel.ConsistsAndPaths;
            if (poolvm.ConsistsAndPaths.Consists.Count == 0 || poolvm.ConsistsAndPaths.Paths.Count == 0)
            {
                waiting waitingscreen = new waiting();
                waitingscreen.Show();
                // actually we don't need the consists here
                if (poolvm.ConsistsAndPaths.Consists.Count == 0)
                {
                    poolvm.ConsistsAndPaths.loadConsists();
                }
                if (poolvm.ConsistsAndPaths.Paths.Count == 0)
                {
                    poolvm.ConsistsAndPaths.loadPaths();
                }                
                waitingscreen.Close();
            }
            poolvm.CreatePool();
            PoolView poolView = new PoolView();
            poolView.DataContext = poolvm;
            poolView.ShowDialog();
        }

        private void New_Turntable_Pool_Click(object sender, RoutedEventArgs e)
        {
            PoolViewModel poolvm = new PoolViewModel();
            poolvm.RoutePath = RoutePath + "\\activities\\openrails\\";
            poolvm.ConsistsAndPaths = mainViewModel.ConsistsAndPaths;
            if (poolvm.ConsistsAndPaths.Consists.Count == 0 || poolvm.ConsistsAndPaths.Paths.Count == 0)
            {
                waiting waitingscreen = new waiting();
                waitingscreen.Show();
                // actually we don't need the consists here
                if (poolvm.ConsistsAndPaths.Consists.Count == 0)
                {
                    poolvm.ConsistsAndPaths.loadConsists();
                }
                if (poolvm.ConsistsAndPaths.Paths.Count == 0)
                {
                    poolvm.ConsistsAndPaths.loadPaths();
                }
                waitingscreen.Close();
            }
            poolvm.CreateTurntablePool();
            PoolView poolView = new PoolView();
            poolView.DataContext = poolvm;
            poolView.ShowDialog();
        }

        private void Open_Pool_CLick(object sender, RoutedEventArgs e)
        {
            PoolViewModel poolvm = new PoolViewModel();            
            poolvm.ConsistsAndPaths = mainViewModel.ConsistsAndPaths;
            if (poolvm.ConsistsAndPaths.Consists.Count == 0 || poolvm.ConsistsAndPaths.Paths.Count == 0)
            {
                waiting waitingscreen = new waiting();
                waitingscreen.Show();
                // actually we don't need the consists here
                if (poolvm.ConsistsAndPaths.Consists.Count == 0)
                {
                    poolvm.ConsistsAndPaths.loadConsists();
                }
                if (poolvm.ConsistsAndPaths.Paths.Count == 0)
                {
                    poolvm.ConsistsAndPaths.loadPaths();
                }
                waitingscreen.Close();
            }
            OpenSaveViewModel openSave = new OpenSaveViewModel();
            openSave.SetModus("Open");
            List<string> filter = new List<string>();
            filter.Add("*.pool-or");
            filter.Add("*.turntable-or");
            openSave.ListFiles(RoutePath + "\\activities\\openrails", filter);
            OpenSaveView osv = new OpenSaveView();
            osv.Title = "Open file";
            osv.DataContext = openSave;
            osv.ShowDialog();
            if (osv.DialogResult == true)
            {
                if (Path.GetExtension(openSave.Filename)==".turntable-or")
                {
                    poolvm.TurntableMode = true;
                }
                poolvm.ReadPoolFile(RoutePath + "\\activities\\openrails\\" + openSave.Filename);               
                PoolView poolView = new PoolView();
                poolView.DataContext = poolvm;
                poolView.ShowDialog();
                if (poolView.DialogResult == true)
                {
                    mainViewModel.PoolTrains.Clear();
                    mainViewModel.LoadPoolTrains(RoutePath + "\\activities\\openrails\\");
                }
            }
        }

        private void Menu_Zip_Timetable(object sender, RoutedEventArgs e)
        {
            // This function is not complete yet. Therefore it is hidden in the menu
            mainViewModel.GetUsedPathsAndCons();
        }
    }
}
