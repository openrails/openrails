using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orts.TimetableEditor.Model;
using System.IO;
using System.Windows;
using Orts.TimetableEditor.Views;

namespace Orts.TimetableEditor.ViewModel
{
    public class PoolViewModel : BasicClass
    {
        private char delimiter;
        private bool _turntablemode;
        private PoolItem _SelectedItem;
        private ConsistsAndPaths _ConsAndPaths;
        private DataFile _selectedTrainPath;
        private string _chosenTrainPath;
        private Visibility _ShowPathList;
        private string _RoutePath;
        private string _Filename;
        public ObservableCollection<PoolItem> PoolItems { get; set; }

        public MyICommand AddPool {  get; set; }
        public MyICommand AddRow { get; set; }
        public MyICommand InsertRow { get; set; }
        public MyICommand RemoveRow { get; set; }
        public MyICommand SaveCommand { get; set; }

        public PoolViewModel() 
        {
            delimiter = '\t';
            PoolItems = new ObservableCollection<PoolItem>();
            TurntableMode = false;
            AddPool = new MyICommand(OnAddPool, CanAddPool);
            AddRow = new MyICommand(OnAddRow, CanAddRow);
            InsertRow = new MyICommand(OnInsertRow, CanInsertRow);
            RemoveRow = new MyICommand(OnRemoveRow, CanRemoveRow);
            SaveCommand = new MyICommand(OnSaveCommand, CanSaveCommand);
            ShowPathList = Visibility.Collapsed;
        }

        public bool TurntableMode
        {
            get
            {
                return _turntablemode;
            }
            set
            {
                _turntablemode = value;
                OnPropertyChanged(nameof(TurntableMode));
            }
        }

        public PoolItem SelectedItem 
        {
            get
            { 
                return _SelectedItem;
            }
            set
            { 
                _SelectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));
                InsertRow.RaiseCanExecuteChanged();
                RemoveRow.RaiseCanExecuteChanged();
                if(SelectedItem != null)
                {
                    if(SelectedItem.Name=="#access" ||  SelectedItem.Name=="#storage")
                    {
                        ShowPathList = Visibility.Visible;
                    }
                    else
                    {
                        ShowPathList = Visibility.Collapsed;
                    }
                }
            } 
        }

        public ConsistsAndPaths ConsistsAndPaths
        {
            get
            {
                return _ConsAndPaths;
            }
            set
            {
                _ConsAndPaths = value;
                OnPropertyChanged(nameof(ConsistsAndPaths));
            }
        }

        public DataFile SelectedTrainPath
        {
            get
            {
                return _selectedTrainPath;
            }
            set
            {
                _selectedTrainPath = value;
                OnPropertyChanged(nameof(SelectedTrainPath));
            }
        }

        public string ChosenTrainPath
        {
            get
            {
                return _chosenTrainPath;
            }
            set
            {
                _chosenTrainPath = value;
                OnPropertyChanged(nameof(ChosenTrainPath));
            }
        }

        public Visibility ShowPathList
        {
            get
            {
                return _ShowPathList;
            }
            set
            {
                _ShowPathList = value;
                OnPropertyChanged(nameof(ShowPathList));
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
                OnPropertyChanged(nameof(Path));
            }
        }

        public string Filename
        {
            get
            {
                return _Filename;
            }
            set
            {
                _Filename = value;
                OnPropertyChanged(nameof(Filename));
            }
        }

        public void CreatePool()
        {
            PoolItems.Add(new PoolItem { Name = "#comment", Value = "" });
            PoolItems.Add(new PoolItem { Name = "#name", Value = "" });
            PoolItems.Add(new PoolItem { Name = "#storage", Value = "" });
            PoolItems.Add(new PoolItem { Name = "#access", Value = "" });
            PoolItems.Add(new PoolItem { Name = "#maxunits", Value = "" });
            PoolItems.Add(new PoolItem { Name = "#settings", Value = "" });
        }

        public void CreateTurntablePool()
        {
            PoolItems.Add(new PoolItem { Name = "#comment", Value = "" });
            PoolItems.Add(new PoolItem { Name = "#name", Value = "" });
            PoolItems.Add(new PoolItem { Name = "#worldfile", Value = "" });
            PoolItems.Add(new PoolItem { Name = "#uid", Value = "" });
            PoolItems.Add(new PoolItem { Name = "#storage", Value = "" });
            PoolItems.Add(new PoolItem { Name = "#access", Value = "" });
            PoolItems.Add(new PoolItem { Name = "#maxunits", Value = "" });
            PoolItems.Add(new PoolItem { Name = "#speedmph", Value = "" });
        }

        public void ReadPoolFile(string filename)
        {
            RoutePath = Path.GetDirectoryName(filename)+"\\";
            //RoutePath = Path.GetFullPath(filename);
            Filename = Path.GetFileName(filename);
            string[] pool = File.ReadAllText(filename).Split('\n');
            delimiter = '\t';
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
                    PoolItem item = new PoolItem();
                    item.Name = lines[0].Trim();
                    item.Value = lines[1].Trim();
                    PoolItems.Add(item);
                }
            }
        }

        public void SavePoolFile(string filename)
        {
            string file = "";
            foreach(PoolItem item in PoolItems)
            {
                file += item.Name + delimiter + item.Value + "\n";
            }
            File.WriteAllText(filename,file);
        }

        private void OnAddPool()
        {
            if(TurntableMode)
            {
                CreateTurntablePool();
            }
            else
            {
                CreatePool();
            }
        }

        private bool CanAddPool()
        {
            return true;
        }

        private void OnAddRow()
        {
            PoolItem item = new PoolItem();
            PoolItems.Add(item);
        }

        private bool CanAddRow()
        {
            return true;
        }

        private void OnInsertRow()
        {
            PoolItem item = new PoolItem();
            PoolItems.Insert(PoolItems.IndexOf(SelectedItem), item);
        }

        private bool CanInsertRow()
        {
            return true;
        }

        private void OnRemoveRow()
        {
            if (MessageBox.Show("Do you really want to remove this row?", "Remove row", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                PoolItems.Remove(SelectedItem);
            }
        }

        private bool CanRemoveRow()
        {
            return SelectedItem != null;
        }

        private void OnSaveCommand()
        {
            if(Filename==null || Filename=="")
            {
                OpenSaveViewModel osvm = new OpenSaveViewModel();
                OpenSaveView osv = new OpenSaveView();
                osv.DataContext = osvm;
                osvm.SetModus("Save");
                List<string> filter = new List<string>();
                if (TurntableMode) filter.Add("*.turntable-or"); else filter.Add("*.pool-or");
                osvm.ListFiles(RoutePath, filter);
                if(osv.ShowDialog()==true)
                {
                    bool can = true;                    
                    if (!TurntableMode && File.Exists(RoutePath + Path.GetFileNameWithoutExtension(osvm.Filename) + ".pool-or"))
                    {
                       if (MessageBox.Show("File already exists", "Overwrite File?", MessageBoxButton.YesNo) == MessageBoxResult.No)
                       {
                           can = false;
                       }
                    }
                    if (TurntableMode && File.Exists(RoutePath + Path.GetFileNameWithoutExtension(osvm.Filename) + ".turntable-or"))
                    {
                        if (MessageBox.Show("File already exists", "Overwrite File?", MessageBoxButton.YesNo) == MessageBoxResult.No)
                        {
                            can = false;
                        }
                    }
                    if (can)
                    {
                        if (TurntableMode)
                        {                            
                            SavePoolFile(RoutePath + Path.GetFileNameWithoutExtension(osvm.Filename) + ".turntable-or");
                        }
                        else
                        {
                            SavePoolFile(RoutePath + Path.GetFileNameWithoutExtension(osvm.Filename) + ".pool-or");
                        }
                    }
                }
            }
            else
            {
                SavePoolFile(RoutePath+Filename);
            }
        }

        private bool CanSaveCommand()
        {
            return true;
        }
    }
}
