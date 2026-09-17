using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Orts.TimetableEditor.Model;

namespace Orts.TimetableEditor.ViewModel
{
    public class DisposeViewModel : BasicClass
    {
        private ConsistsAndPaths _ConsAndPaths;
        private bool _DisposeForms = false;
        private bool _DisposeTriggers = false;
        private bool _DisposeStatic = false;
        private bool _DisposeStable = false;
        private bool _DisposePool = false;
        private bool _DisposeAttach = false;
        private DisposeCommand _SelectedCommand;
        private string _SelectedPossibleCommand;

        public ObservableCollection<DisposeCommand> Commands { get; set; }
        public ObservableCollection<string> AvailableTrains { get; set; }
        public ObservableCollection<string> PoolTrains { get; set; }
        public ObservableCollection<string> PossibleCommands { get; set; }

        public MyICommand AddCommand { get; set; }
        public MyICommand RemoveCommand { get; set; }
        public DisposeViewModel()
        {
            Commands = new ObservableCollection<DisposeCommand>();
            AvailableTrains = new ObservableCollection<string>();
            PoolTrains = new ObservableCollection<string>();
            PossibleCommands = new ObservableCollection<string>();
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

        public bool DisposeForms
        {
            get 
            { 
                return _DisposeForms; 
            } 
            set 
            { 
                _DisposeForms = value; 
                OnPropertyChanged(nameof(DisposeForms));
                if(DisposeForms)
                {
                    Commands.Clear();
                    SetFormsCommands();
                }
            }
        }

        public bool DisposeTriggers
        {
            get
            {
                return _DisposeTriggers;
            }
            set
            {
                _DisposeTriggers = value;
                OnPropertyChanged(nameof(DisposeTriggers));
                if (DisposeTriggers)
                {
                    Commands.Clear();
                    SetTriggersCommands();
                }
            }
        }

        public bool DisposeStatic
        {
            get
            {
                return _DisposeStatic;
            }
            set
            {
                _DisposeStatic = value;
                OnPropertyChanged(nameof(DisposeStatic));
                if( DisposeStatic )
                {
                    Commands.Clear();
                    SetStaticCommands();
                }
            }
        }

        public bool DisposeStable
        {
            get
            {
                return _DisposeStable;
            }
            set
            {
                _DisposeStable = value;
                OnPropertyChanged(nameof(DisposeStable));
                if( DisposeStable )
                {
                    Commands.Clear();
                    SetStableCommands();
                }
            }
        }

        public bool DisposePool
        {
            get
            {
                return _DisposePool;
            }
            set
            {
                _DisposePool = value;
                OnPropertyChanged(nameof(DisposePool));
                if (DisposePool)
                {
                    Commands.Clear();
                    SetPoolCommands();
                }
            }
        }

        public bool DisposeAttach
        {
            get
            {
                return _DisposeAttach;
            }
            set
            {
                _DisposeAttach = value;
                OnPropertyChanged(nameof(DisposeAttach));
                if(DisposeAttach)
                {
                    Commands.Clear();
                    SetAttachCommands();
                }
            }
        }

        public DisposeCommand SelectedCommand
        {
            get
            {
                return _SelectedCommand;
            }
            set
            {
                _SelectedCommand = value;
                OnPropertyChanged(nameof(SelectedCommand));
                //RemoveCommand.RaiseCanExecuteChanged();
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
            }
        }

        public void ParseDispose(string disposestring)
        {
            string[] reg = Regex.Split(disposestring, @"(?=[$])");
            for (int i = 1; i < reg.Length; i++)
            {
                reg[i] = reg[i].Trim();                
                if (i == 1)
                {
                    string[] cmd1 = reg[i].Split('=');
                    switch (cmd1[0])
                    {
                        case "$forms":
                            {
                                DisposeForms = true;
                                SetFormsCommands();
                                break;
                            }
                        case "$triggers":
                            {
                                DisposeTriggers = true; 
                                SetTriggersCommands();
                                break;
                            }
                        case "$static": 
                            {                                 
                                DisposeStatic = true;
                                SetStaticCommands();
                                break; 
                            }
                        case "$stable":
                            {
                                DisposeStable = true;
                                SetStableCommands();
                                break;
                            }
                        case "$pool": 
                            { 
                                DisposePool = true; 
                                SetPoolCommands();
                                break; 
                            }
                        case "$attach": 
                            { 
                                DisposeAttach = true;
                                SetAttachCommands();
                                break; 
                            }
                    }
                    Commands.Clear();
                }                
                DisposeCommand cmd = new DisposeCommand();
                cmd.newCommand(reg[i], AvailableTrains, PoolTrains, ConsistsAndPaths);
                Commands.Add(cmd);
            }
        }

        public void GetAvailableCons(DataRow firstRow, int ColCount, int ownColumn)
        {
            bool read = false;
            AvailableTrains.Clear();
            AvailableTrains.Add("");
            for (int i = 0; i < ColCount; i++)
            {
                if (read)
                {
                    if (firstRow[i].ToString() != "" && firstRow[i].ToString() != "$static" && i != ownColumn)
                    {
                        AvailableTrains.Add(firstRow[i].ToString());
                    }
                }
                if (firstRow[i].ToString() == "#comment")
                {
                    read = true;
                }
            }
        }

        private void SetFormsCommands()
        {
            Commands.Clear();
            DisposeCommand cmd = new DisposeCommand();
            cmd.newCommand("$forms", AvailableTrains, PoolTrains, ConsistsAndPaths);
            Commands.Add(cmd);
            PossibleCommands.Clear();
            PossibleCommands.Add("");
            PossibleCommands.Add("$pickup");
            PossibleCommands.Add("$detach");
            PossibleCommands.Add("$transfer");
        }

        private void SetTriggersCommands()
        {
            Commands.Clear();
            DisposeCommand cmd = new DisposeCommand();
            cmd.newCommand("$triggers", AvailableTrains, PoolTrains, ConsistsAndPaths);
            Commands.Add(cmd);
            PossibleCommands.Clear();
            PossibleCommands.Add("");
            PossibleCommands.Add("$detach");
        }

        private void SetStaticCommands()
        {
            Commands.Clear();
            DisposeCommand cmd = new DisposeCommand();
            cmd.newCommand("$static", AvailableTrains, PoolTrains, ConsistsAndPaths);
            Commands.Add(cmd);
            PossibleCommands.Clear();
            PossibleCommands.Add("");
            PossibleCommands.Add("$detach");
        }

        private void SetStableCommands()
        {
            Commands.Clear();
            DisposeCommand cmd = new DisposeCommand();
            cmd.newCommand("$stable", AvailableTrains, PoolTrains, ConsistsAndPaths);
            Commands.Add(cmd);
            PossibleCommands.Clear();
        }

        private void SetPoolCommands()
        {
            Commands.Clear();
            DisposeCommand cmd = new DisposeCommand();
            cmd.newCommand("$pool", AvailableTrains, PoolTrains, ConsistsAndPaths);
            Commands.Add(cmd);
            PossibleCommands.Clear();
            PossibleCommands.Add("");
        }

        private void SetAttachCommands()
        {
            Commands.Clear();
            DisposeCommand cmd = new DisposeCommand();
            cmd.newCommand("$attach", AvailableTrains, PoolTrains, ConsistsAndPaths);
            Commands.Add(cmd);
            PossibleCommands.Clear();
            PossibleCommands.Add("");
            PossibleCommands.Add("$detach");
        }

        private void OnAddCommand()
        {
            DisposeCommand cmd = new DisposeCommand();
            cmd.Name = SelectedPossibleCommand.ToString();
            cmd.newCommand(SelectedPossibleCommand.ToString(), AvailableTrains, PoolTrains, ConsistsAndPaths);
            Commands.Add(cmd);
        }

        private bool CanAddCommand()
        {
            return true;
        }

        private void OnRemoveCommand()
        {
        }

        private bool CanRemoveCommand() 
        { 
            return true; 
        }
    }
}
