using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Orts.TimetableEditor.Model;
using System.Text.RegularExpressions;

namespace Orts.TimetableEditor.ViewModel
{
    public class TrainViewModel : BasicClass
    {
        private ConsistsAndPaths _ConsAndPaths;
        private string _TrainName;
        private string _comment;
        private DataFile _selectedTrainPath;
        private string _chosenTrainPath;
        private bool _showNormalPaths;
        private bool _showSidingPaths;
        private string _consiststring;
        private bool _multiConsist;
        private bool _showEngines;
        private bool _showWagons;
        private DataFile _selectedConsist;
        private int _selectedConsistIndex;
        private bool _ModusTime;
        private bool _ModusStatic;
        private string _StartTime;
        private string _CreateTime;
        private bool _CreateCommand;
        private bool _AheadCommand;
        private bool _PoolCommand;
        private string _SelectedPossibleCommand;
        private TimetableCommand _SelectedCommand;

        public ObservableCollection<DataFile> TrainPathsSorted { get; set; }
        public ObservableCollection<DataFile> ConsistsSorted { get; set; }
        public ObservableCollection<Consist> Con { get; set; }
        public ObservableCollection<string> AvailableTrains {  get; set; }
        public ObservableCollection<TimetableCommand> Commands { get; set; }
        public ObservableCollection<string> PossibleCommands { get; set; }
        public ObservableCollection<string> PoolTrains { get; set; }
        public MyICommand AddConsist { get; set; }
        public MyICommand RemoveConsist { get; set; }
        public MyICommand MoveConsistUp { get; set; }
        public MyICommand MoveConsistsDown { get; set; }
        public MyICommand AddCommand { get; set; }
        public MyICommand RemoveCommand { get; set; }
        public TrainViewModel()
        {
            TrainPathsSorted = new ObservableCollection<DataFile>();
            ConsistsSorted = new ObservableCollection<DataFile>();
            Con = new ObservableCollection<Consist>();
            AvailableTrains = new ObservableCollection<string>();
            Commands = new ObservableCollection<TimetableCommand>();
            PossibleCommands = new ObservableCollection<string>();
            PoolTrains = new ObservableCollection<string>();
            //PoolTrains.Add("dummy");
            ShowEngines = true;
            ShowWagons = true;
            ShowNormalPaths = true;
            ShowSidingsPaths = true;
            AddConsist = new MyICommand(OnAddConsist, CanAddConsist);
            RemoveConsist = new MyICommand(OnRemoveConsist, CanRemoveConsist);
            MoveConsistsDown = new MyICommand(OnMoveConsistDown, CanMoveConsistDown);
            MoveConsistUp = new MyICommand(OnMoveConsistUp, CanMoveConsistUp);
            AddCommand = new MyICommand(OnAddCommand, CanAddCommand);
            RemoveCommand = new MyICommand(OnRemoveCommand, CanRemoveCommand);
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

        public string TrainName
        {
            get
            {
                return _TrainName;
            }
            set
            {
                _TrainName = value;
                OnPropertyChanged(nameof(TrainName));
            }
        }

        public string Comment
        {
            get
            {
                return _comment;
            }
            set
            {
                _comment = value;
                OnPropertyChanged(nameof(Comment));
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

        public bool ShowNormalPaths
        {
            get
            {
                return _showNormalPaths;
            }
            set
            {
                _showNormalPaths = value;
                OnPropertyChanged(nameof(ShowNormalPaths));
                sortPaths();
            }
        }

        public bool ShowSidingsPaths
        {
            get
            {
                return _showSidingPaths;
            }
            set
            {
                _showSidingPaths = value;
                OnPropertyChanged(nameof(ShowSidingsPaths));
                sortPaths();
            }
        }

        public string ConsistString
        {
            get
            {
                return _consiststring;
            }
            set
            {
                _consiststring = value;
                OnPropertyChanged(nameof(ConsistString));
            }
        }

        public bool MultiConsist
        {
            get
            {
                return _multiConsist;
            }
            set
            {
                _multiConsist = value;
                OnPropertyChanged(nameof(MultiConsist));
            }
        }

        public bool ShowEngines
        {
            get
            {
                return _showEngines;
            }
            set
            {
                _showEngines = value;
                OnPropertyChanged(nameof(ShowEngines));
                sortConsists();
            }
        }

        public bool ShowWagons
        {
            get
            {
                return _showWagons;
            }
            set
            {
                _showWagons = value;
                OnPropertyChanged(nameof(ShowWagons));
                sortConsists();
            }
        }

        public DataFile SelectedConsist
        {
            get
            {
                return _selectedConsist;
            }
            set
            {
                _selectedConsist = value;
                OnPropertyChanged(nameof(SelectedConsist));
                AddConsist.RaiseCanExecuteChanged();
            }
        }

        public int SelectedConsistIndex
        {
            get
            {
                return _selectedConsistIndex;
            }
            set
            {
                _selectedConsistIndex = value;
                OnPropertyChanged(nameof(SelectedConsistIndex));
                MoveConsistsDown.RaiseCanExecuteChanged();
                MoveConsistUp.RaiseCanExecuteChanged();
                RemoveConsist.RaiseCanExecuteChanged();
            }
        }

        public bool ModusTime
        {
            get
            {
                return _ModusTime;
            }
            set
            {
                _ModusTime = value;
                OnPropertyChanged(nameof(ModusTime));
                if(ModusTime==true)
                {
                    ModusStatic = false;
                }
                Commands.Clear();
                SetPossibleCommands();
            }
        }

        public bool ModusStatic
        {
            get
            {
                return _ModusStatic;
            }
            set
            {
                _ModusStatic = value;
                OnPropertyChanged(nameof(ModusStatic));
                if(ModusStatic==true)
                {
                    ModusTime = false;
                }
                Commands.Clear();
                PossibleCommands.Clear();
                TimetableCommand staticCommand = new TimetableCommand();
                staticCommand.newCommand("$static", AvailableTrains, PoolTrains);
                Commands.Add(staticCommand);
            }
        }

        public string StartTime
        {
            get
            {
                return _StartTime;
            }
            set
            {
                _StartTime = value;
                OnPropertyChanged(nameof(StartTime));
            }
        }
        public string CreateTime
        {
            get
            {
                return _CreateTime;
            }
            set
            {
                _CreateTime = value;
                OnPropertyChanged(nameof(CreateTime));
            }
        }

        public bool CreateCommand
        {
            get
            {
                return _CreateCommand;
            }
            set
            {
                _CreateCommand = value;
                OnPropertyChanged(nameof(CreateCommand));
            }
        }

        public bool AheadCommand
        {
            get
            {
                return _AheadCommand;
            }
            set
            {
                _AheadCommand = value;
                OnPropertyChanged(nameof(AheadCommand));
            }
        }

        public bool PoolCommand
        {
            get
            {
                return _PoolCommand;
            }
            set
            {
                _PoolCommand = value;
                OnPropertyChanged(nameof(PoolCommand));
            }
        }

        public string SelectedPossibleCommand
        {
            get
            {
                return _SelectedPossibleCommand;
            }
            set
            {
                _SelectedPossibleCommand = value;
                OnPropertyChanged(nameof(SelectedPossibleCommand));
                AddCommand.RaiseCanExecuteChanged();
            }
        }
        public TimetableCommand SelectedCommand
        {
            get
            {
                return _SelectedCommand;
            }
            set
            {
                _SelectedCommand = value;
                OnPropertyChanged(nameof(SelectedCommand));
                RemoveCommand.RaiseCanExecuteChanged();
            }
        }

        public void GetAvailableCons(DataRow firstRow, int ColCount, int ownColumn)
        {
            bool read = false;
            AvailableTrains.Clear();
            AvailableTrains.Add("");
            for(int i=0; i<ColCount;i++)
            {
                if (read)
                {
                    if (firstRow[i].ToString()!="" && firstRow[i].ToString()!="$static" && i!=ownColumn)
                    {
                        AvailableTrains.Add(firstRow[i].ToString());
                    }
                }
                if (firstRow[i].ToString()=="#comment")
                {
                    read = true;
                }
            }
        }

        public void sortConsists()
        {
            if (ConsistsAndPaths != null)
            {
                ConsistsSorted.Clear();
                foreach (DataFile con in ConsistsAndPaths.Consists)
                {
                    if (con.Type == "e" && ShowEngines)
                    {
                        ConsistsSorted.Add(con);
                    }
                    if (con.Type == "w" && ShowWagons)
                    {
                        ConsistsSorted.Add(con);
                    }
                }

            }
        }

        public void sortPaths()
        {
            if (ConsistsAndPaths != null)
            {
                TrainPathsSorted.Clear();
                foreach (DataFile pat in ConsistsAndPaths.Paths)
                {
                    if (pat.Type == "n" && ShowNormalPaths)
                    {
                        TrainPathsSorted.Add(pat);
                    }
                    if (pat.Type == "s" && ShowSidingsPaths)
                    {
                        TrainPathsSorted.Add(pat);
                    }
                }
            }
        }

        public void ConsistToList(string con)
        {
            Con.Clear();
            if (con.Length > 0)
            {
                bool intrain = false;
                int po = 0;
                int lpo = 0;
                for (int p = 0; p < con.Length; p++)
                {
                    if (con[p] == '<')
                    {
                        intrain = true;
                    }
                    if (con[p] == '+' && intrain == false)
                    {
                        // hier muss kopiert werden
                        lpo = p;
                        Consist consist = new Consist();
                        consist.Name = con.Substring(po, p - po);
                        po = p + 1;
                        Con.Add(consist);
                    }
                    if (con[p] == '>')
                    {
                        intrain = false;
                    }
                    lpo = p;
                }
                Consist consist1 = new Consist();
                consist1.Name = con.Substring(po, lpo - po + 1);
                Con.Add(consist1);
                foreach (Consist cn in Con)
                {
                    if (cn.Name.Contains("$reverse"))
                    {
                        cn.Name = cn.Name.Replace("$reverse", "").Trim();
                        cn.Reversed = true;
                    }
                }
            }
        }

        private void SetPossibleCommands()
        {
            PossibleCommands.Clear();
            PossibleCommands.Add("$create");
            PossibleCommands.Add("$next");
            PossibleCommands.Add("$pool");
        }

        public void StartConsist(string scon)
        {
            string[] reg = Regex.Split(scon, @"(?=[$])");
            for(int i=0; i<reg.Length; i++)
            {
                reg[i] = reg[i].Trim();                
            }
            if (scon.Contains("$static"))
            {
                ModusStatic = true;
                TimetableCommand cmd = Commands[0];
                cmd.newCommand(reg[1], AvailableTrains, PoolTrains);
            }
            else
            {
                ModusTime = true;
                StartTime= reg[0];
                for(int i = 1; i<reg.Length; i++)
                {
                    TimetableCommand cmd = new TimetableCommand();
                    cmd.newCommand(reg[i], AvailableTrains, PoolTrains);
                    if(cmd.Name=="$pool")
                    {
                        cmd.Items = PoolTrains;
                    }
                    Commands.Add(cmd);
                }
            }
        }

        private void OnAddConsist()
        {
            if (MultiConsist == false)
            {
                Con.Clear();
            }
            string conname = SelectedConsist.FileName;
            if (conname.Contains("+"))
            {
                conname = "<" + conname + ">";
            }
            Con.Add(new Consist { Name = conname, Reversed = false });
        }

        public string GetStartString()
        {
            string ret = "";
            if(ModusTime == true)
            {
                ret += StartTime;
            }
            foreach (TimetableCommand cmd in Commands)
            {
                ret += cmd.GetCommandString();
            }
            return ret;
        }

        private bool CanAddConsist()
        {
            return SelectedConsist != null;
        }

        private void OnRemoveConsist()
        {
            if (MessageBox.Show("Really?", "Remove Item", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Con.RemoveAt(SelectedConsistIndex);
            }
        }

        private bool CanRemoveConsist()
        {
            return SelectedConsistIndex>-1;
        }

        private void OnMoveConsistDown()
        {
            Con.Move(SelectedConsistIndex, SelectedConsistIndex + 1);
        }
        private bool CanMoveConsistDown()
        {
            bool res = false;
            if(SelectedConsistIndex<Con.Count-1)
            {
                res = true;
            }
            return res;
        }
        private void OnMoveConsistUp()
        {
            Con.Move(SelectedConsistIndex, SelectedConsistIndex - 1);
        }
        private bool CanMoveConsistUp()
        {
            bool res = false;
            if (SelectedConsistIndex >0)
            {
               res = true;
            }
            return res;
        }

        private void OnAddCommand()
        {
            TimetableCommand cmd = new TimetableCommand();
            cmd.Name = SelectedPossibleCommand.ToString();
            cmd.newCommand(SelectedPossibleCommand.ToString(),AvailableTrains,PoolTrains);
            Commands.Add(cmd);
        }
        private bool CanAddCommand()
        {
            return SelectedPossibleCommand != null;
        }

        private void OnRemoveCommand()
        { 
            Commands.Remove(SelectedCommand);
        }

        private bool CanRemoveCommand()
        {
            return SelectedCommand != null;
        }
    }
}
