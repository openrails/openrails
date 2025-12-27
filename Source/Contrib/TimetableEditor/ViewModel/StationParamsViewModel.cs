using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TimeTableEditor.Model;
using TimeTableEditor.Views;

namespace TimeTableEditor.ViewModel
{
    public class StationParamsViewModel : BasicClass
    {
        public ObservableCollection<Station> Stations { get; set; }
        public ObservableCollection<string> PossibleCommands { get; set; }
        private Station selectedStation;
        private Visibility commandsVisible;
        private TimeTableCommand selectedCommand;
        private string selectedPossibleCommand;

        public MyICommand OK_Button {  get; set; }
        public MyICommand AddCommand { get; set; }
        public MyICommand RemoveCommand { get; set; }
        public StationParamsViewModel()
        {
            Stations = new ObservableCollection<Station>();
            CommandsVisible = Visibility.Collapsed;
            PossibleCommands = new ObservableCollection<string>();
            AddCommand = new MyICommand(OnAddCommand, CanAddCommand);
            RemoveCommand = new MyICommand(OnRemoveCommand, CanRemoveCommand);
            CreateCommands();
        }

        public Station SelectedStation
        {
            get
            {
                return selectedStation;
            }
            set
            {
                selectedStation = value;
                OnPropertyChanged(nameof(SelectedStation));
                if (selectedStation != null)
                {
                    CommandsVisible = Visibility.Visible;
                }
            }
        }

        public Visibility CommandsVisible
        {
            get
            {
                return commandsVisible;
            }
            set
            {
                commandsVisible = value;
                OnPropertyChanged(nameof(CommandsVisible));
            }
        }

        public TimeTableCommand SelectedCommand
        {
            get
            {
                return selectedCommand;
            }
            set
            {
                selectedCommand = value;
                OnPropertyChanged(nameof(SelectedCommand));
                RemoveCommand.RaiseCanExecuteChanged();
            }
        }

        public string SelectedPossibleCommand
        {
            get
            {
                return selectedPossibleCommand;
            }
            set
            {
                selectedPossibleCommand = value;
                OnPropertyChanged(nameof(SelectedPossibleCommand));
                AddCommand.RaiseCanExecuteChanged();
            }
        }

        public void AddStation(string desc)
        {
            string[] stationarr = desc.Split('$');
            Station station = new Station();
        }

        public void CreateCommands()
        {
            PossibleCommands.Clear();
            PossibleCommands.Add("$nohold");
            PossibleCommands.Add("$hold");
            PossibleCommands.Add("$forcehold");
            PossibleCommands.Add("$forcewait");
            PossibleCommands.Add("$nowaitsignal");
            PossibleCommands.Add("$terminal");
            PossibleCommands.Add("$closeupsignal");
            PossibleCommands.Add("$extendplatformtosignal");
            PossibleCommands.Add("$restrictplatformtosignal");
            PossibleCommands.Add("$stoptime");
            PossibleCommands.Add("OTHER");
        }

        private void OnAddCommand()
        {
            if(selectedPossibleCommand!="OTHER")
            {
                TimeTableCommand newcmd = new TimeTableCommand();
                newcmd.Name = SelectedPossibleCommand;
                if (newcmd.Name == "$stoptime")
                {
                    newcmd.Value = "";
                }
                SelectedStation.Commands.Add(newcmd);
            }
            else
            {
                InputBox inp = new InputBox();
                inp.Title = "New command";
                inp.Question.Text = "Please enter the new Command";
                inp.ShowDialog();
                if(inp.DialogResult==true)
                {
                    TimeTableCommand newcmd = new TimeTableCommand();
                    newcmd.Name = inp.Input.Text;
                    if (inp.HasValue.IsChecked == true)
                    {
                        newcmd.Value = inp.InputValue.Text;
                    }
                    SelectedStation.Commands.Add(newcmd);
                }
            }
        }
        private bool CanAddCommand()
        {
            return SelectedPossibleCommand != null;
        }

        private void OnRemoveCommand()
        {
            if (MessageBox.Show("Remove item?", "Remove?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                SelectedStation.Commands.Remove(SelectedCommand);
            }
        }
        private bool CanRemoveCommand()
        {
            return SelectedCommand != null;
        }
    }
}
