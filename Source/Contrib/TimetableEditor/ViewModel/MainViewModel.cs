using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using TimeTableEditor.Views;
using TimeTableEditor.ViewModel;
using System.ComponentModel;
using TimeTableEditor.Model;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Data;
using System.IO.Packaging;

namespace TimeTableEditor.ViewModel
{
    public class MainViewModel : BasicClass
    {
        public ObservableCollection<TabItem> Tabs { get; set; }
        public ObservableCollection<TimeTableViewModel> Tables { get; set; }
        public ObservableCollection<string> AllTrains { get; set; }
        public ObservableCollection<string> PoolTrains { get; set; }
        public ObservableCollection<string> ConsForZip { get; set; }
        public ObservableCollection<string> PathsForZip { get; set; }
        private int selectedTabIndex;
        private ConsistsAndPaths consandpaths;
        private string _RoutePath;
        
        public MainViewModel()
        {
            Tabs = new ObservableCollection<TabItem>();
            Tables = new ObservableCollection<TimeTableViewModel>();
            AllTrains = new ObservableCollection<string>();
            PoolTrains = new ObservableCollection<string>();
            ConsistsAndPaths = new ConsistsAndPaths();
            PathsForZip = new ObservableCollection<string>();
            ConsForZip = new ObservableCollection<string>();
        }

        public ConsistsAndPaths ConsistsAndPaths
        {
            get
            {
                return consandpaths;
            }
            set
            {
                consandpaths = value;
                OnPropertyChanged(nameof(ConsistsAndPaths));
            }
        }

        public void NewTimeTable(string tdbfilename, string routepath)
        {
            TimeTableViewModel viewModel = new TimeTableViewModel();
            viewModel.newTimeTable(tdbfilename, routepath);
            viewModel.ConsAndPaths = ConsistsAndPaths;
            Tables.Add(viewModel);
            Table table = new Table();
            table.DataContext = viewModel;
            Tabs.Add(new TabItem { Header = viewModel.TTName, Content = table });
            SelectedTabIndex = Tabs.Count - 1;
        }

        public void LoadTimeTable(string tdbfilename, string routepath, string timetablefile)
        {
            RoutePath=routepath;
            TimeTableViewModel viewModel = new TimeTableViewModel();
            viewModel.LoadTimeTable(tdbfilename , routepath , timetablefile);
            viewModel.ConsAndPaths = ConsistsAndPaths;
            Tables.Add(viewModel);
            Table table = new Table();
            table.DataContext = viewModel;
            string header = Path.GetFileName(timetablefile).Replace("_","__");
            Tabs.Add(new TabItem { Header = header, Content = table });
            SelectedTabIndex = Tabs.Count - 1;
        }

        public void SaveTimeTable(string filename)
        {
            Tables[SelectedTabIndex].SaveTimeTable(filename);
        }

        public void SaveTimeTablelist(string filename)
        {
            using (var writer = new StreamWriter(filename))
            {
                foreach(TabItem tab in Tabs)
                {
                    writer.WriteLine(tab.Header);
                }
            }
        }

        public int SelectedTabIndex
        {
            get
            {
                return selectedTabIndex;
            }
            set
            {
                selectedTabIndex = value;
                OnPropertyChanged(nameof(SelectedTabIndex));
                if(SelectedTabIndex>-1)
                {
                    Tables[SelectedTabIndex].PoolTrains = PoolTrains;
                    if (Tabs.Count>1)
                    {
                        AllTrains.Clear();
                        for(int i=0; i<Tabs.Count; i++)
                        {
                            if(i!=SelectedTabIndex)
                            {
                                Tables[i].GetAllTrains(AllTrains);
                            }
                        }
                        Tables[SelectedTabIndex].OtherTrains=AllTrains;                                                
                    }
                }
            }
        }

        public string RoutePath
        {
            get
            {
                return _RoutePath;
            }
            set
            {
                _RoutePath = value;
                OnPropertyChanged(nameof(RoutePath));
            }
        }

        public void LoadPoolTrains(string path)
        {
            PoolTrains.Clear();
            string[] poolfiles = Directory.GetFiles(path, "*.pool-or");
            string[] turntablepoolfiles = Directory.GetFiles(path, "*.turntable-or");
            if (poolfiles.Length > 0)
            {
                foreach (string file in poolfiles)
                {
                    string[] pool = File.ReadAllText(file).Split('\n');
                    char delimiter = '\t';
                    string[] delimtest = pool[0].Split(delimiter);
                    if (delimtest.Length == 1)
                    {
                        delimiter = ';';
                        delimtest = pool[0].Split(delimiter);
                        if (delimtest.Length == 1)
                        {
                            delimiter = ',';
                        }
                    }
                    foreach (string line in pool)
                    {
                        string[] lines = line.Split(delimiter);
                        if (lines.Length > 1)
                        {
                            if (lines[0].StartsWith("#name")) PoolTrains.Add(lines[1].Trim());
                        }
                    }
                }
            }
            if (turntablepoolfiles.Length > 0)
            {
                foreach (string file in turntablepoolfiles)
                {
                    string[] pool = File.ReadAllText(turntablepoolfiles[0]).Split('\n');
                    char delimiter = '\t';
                    string[] delimtest = pool[0].Split(delimiter);
                    if (delimtest.Length == 1)
                    {
                        delimiter = ';';
                        delimtest = pool[0].Split(delimiter);
                        if (delimtest.Length == 1)
                        {
                            delimiter = ',';
                        }
                    }
                    foreach (string line in pool)
                    {
                        string[] lines = line.Split(delimiter);
                        if (lines.Length > 1)
                        {
                            if (lines[0].StartsWith("#name")) PoolTrains.Add(lines[1].Trim());
                        }
                    }
                }
            }
        }

        public void GetUsedPathsAndCons()
        {            
            string[] RouteNameArray = RoutePath.Split('\\');
            string RouteName = RouteNameArray[RouteNameArray.Length-1];
            ConsForZip.Clear();
            PathsForZip.Clear();
            foreach (TimeTableViewModel tvm in Tables)
            {
                tvm.GetUsedConsists(ConsForZip);
                tvm.GetUsedPaths(PathsForZip);
            }
            for(int i=0; i<ConsForZip.Count; i++)
            {
                ConsForZip[i] = ConsistsAndPaths.ConsistsPath+"\\" + ConsForZip[i] + ".con";
            }
            for(int i=0; i<PathsForZip.Count; i++)
            {
                PathsForZip[i] = ConsistsAndPaths.PathsPath + "\\" + PathsForZip[i] + ".pat";
            }           
            if(File.Exists("test.zip"))
            {
                File.Delete("test.zip");
            }
            var zip = ZipFile.Open("test.zip", ZipArchiveMode.Create);
            foreach(string file in ConsForZip)
            {
                if (File.Exists(file))
                    zip.CreateEntryFromFile(file, "Consists\\" + Path.GetFileName(file), CompressionLevel.Optimal);
                else MessageBox.Show(file, "File not found");
            }           
            foreach(string file in PathsForZip)
            {
                if (File.Exists(file))
                    zip.CreateEntryFromFile(file, "Routes\\" + RouteName + "\\Paths\\" + Path.GetFileName(file), CompressionLevel.Optimal);
                else MessageBox.Show(file, "File not found");
            }
            zip.Dispose();
        }
    }
}
