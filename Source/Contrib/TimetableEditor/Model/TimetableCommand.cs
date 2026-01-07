using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace Orts.TimetableEditor.Model
{
    public class TimetableCommand : BasicClass
    {
        private string _Name;
        private string _Value;
        private bool _hasValue;
        private bool _used;
        private bool _hasItems;
        private bool _CanHaveParameters;
        private bool _HasParameters;
        private string _selectedItem;
        private TimetableParameter _selectedParameter;
        private string _selectedPossibleParameter;

        public MyICommand SetValue { get; set; }
        public MyICommand AddParameter { get; set; }
        public MyICommand RemoveParameter { get; set; }

        public ObservableCollection<TimetableParameter> Parameters { get; set; }
        public ObservableCollection<string> Items { get; set; }
        public ObservableCollection<string> AvailableTrains { get; set; }
        public ObservableCollection<string> PoolTrains { get; set; }
        public ObservableCollection<string> PossibleParameters { get; set; }
        public ObservableCollection<string> PossibleParameterValues { get; set; }

        public TimetableCommand()
        {
            Parameters = new ObservableCollection<TimetableParameter>();
            Items = new ObservableCollection<string>();
            AvailableTrains = new ObservableCollection<string>();
            PoolTrains = new ObservableCollection<string>();
            PossibleParameters = new ObservableCollection<string>();
            PossibleParameterValues = new ObservableCollection<string>();
            SetValue = new MyICommand(OnSetValue, CanSetValue);
            AddParameter = new MyICommand(OnAddParameter, CanAddParameter);
            RemoveParameter = new MyICommand(OnRemoveParameter, CanRemoveParameter);
        }

        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name = value;
                OnPropertyChanged(nameof(Name));
                PossibleParameters.Clear();
                if (Name == "$create")
                {
                    PossibleParameters.Add("");
                    PossibleParameters.Add("/ahead");
                }
                if (Name == "$pool")
                {
                    PossibleParameters.Add("");
                    PossibleParameters.Add("/direction");
                }
                if (Name == "$static")
                {
                    PossibleParameters.Add("");
                    PossibleParameters.Add("/pool");
                    PossibleParameters.Add("/ahead");
                }
                if (Name == "$forms")
                {
                    PossibleParameters.Add("");

                }
            }
        }

        public bool HasValue
        {
            get
            {
                return (Value != null);
            }
        }

        public string Value
        {
            get
            {
                return _Value;
            }
            set
            {
                _Value = value;
                OnPropertyChanged(nameof(Value));
                OnPropertyChanged(nameof(HasValue));
            }
        }

        public bool Used
        {
            get
            {
                return _used;
            }
            set
            {
                _used = value;
                OnPropertyChanged(nameof(Used));
            }
        }

        public bool HasItems
        {
            get
            {
                return Items.Count > 0;
            }
        }

        public bool CanHaveParameters
        {
            get
            {
                return _CanHaveParameters;
            }
            set
            {
                _CanHaveParameters = value;
                OnPropertyChanged(nameof(CanHaveParameters));
            }
        }

        public bool HasParameters
        {
            get
            {
                return Parameters.Count > 0;
            }
        }

        public string SelectedItem
        {
            get
            {
                return _selectedItem;
            }
            set
            {
                _selectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));
                SetValue.RaiseCanExecuteChanged();
            }
        }

        public TimetableParameter SelectedParameter
        {
            get
            {
                return _selectedParameter;
            }
            set
            {
                _selectedParameter = value;
                OnPropertyChanged(nameof(SelectedParameter));
                RemoveParameter.RaiseCanExecuteChanged();
            }
        }

        public string SelectedPossibleParameter
        {
            get
            {
                return _selectedPossibleParameter;
            }
            set
            {
                _selectedPossibleParameter = value;
                OnPropertyChanged(nameof(SelectedPossibleParameter));
                AddParameter.RaiseCanExecuteChanged();
            }
        }

        private void SetParameterItems(TimetableParameter parameter)
        {
            if (parameter.ParameterName == "/pool")
            {
                parameter.Items = PoolTrains;
            }
            if (parameter.ParameterName == "/ahead")
            {
                parameter.Items = AvailableTrains;
            }
            if (parameter.ParameterName == "/direction")
            {
                parameter.Items.Add("forward");
                parameter.Items.Add("backward");
            }
        }

        public void newCommand(string cmd, ObservableCollection<string> Trains, ObservableCollection<string> TrainsFromPool)
        {
            AvailableTrains = Trains;
            PoolTrains = TrainsFromPool;
            newCommand(cmd);
        }

        public void newCommand(string cmd)
        {
            string[] para = Regex.Split(cmd, @"(?=[/])");
            PossibleParameters.Clear();
            for (int i = 0; i < para.Length; i++)
            {
                para[i] = para[i].Trim();
            }
            if (para[0].Contains("="))
            {
                string[] valname = para[0].Split('=');
                Name = valname[0].Trim();
                Value = valname[1].Trim();
            }
            else
            {
                Name = para[0];
            }
            if (Name == "$create" || Name == "$pool")
            {
                CanHaveParameters = true;
                if (Value == null)
                {
                    Value = "";
                }
                if (Name == "$pool")
                {
                    Items = PoolTrains;
                }
            }
            if (Name == "$static")
            {
                CanHaveParameters = true;
            }
            if (para.Length > 1)
            {
                for (int i = 1; i < para.Length; i++)
                {
                    TimetableParameter parameter = new TimetableParameter();
                    parameter.newParameter(para[i]);
                    SetParameterItems(parameter);
                    Parameters.Add(parameter);
                }
            }
        }

        public string GetCommandString()
        {
            string ret = " " + Name;
            if (Value != null)
            {
                ret += "=" + Value;
            }
            if (Parameters.Count > 0)
            {
                foreach (TimetableParameter parameter in Parameters)
                {
                    ret += parameter.GetParameterString();
                }
            }
            return ret;
        }

        private void OnSetValue()
        {
            Value = SelectedItem;
        }

        private bool CanSetValue()
        {
            return SelectedItem != null;
        }

        private void OnAddParameter()
        {
            TimetableParameter para = new TimetableParameter();
            para.ParameterName = SelectedPossibleParameter;
            SetParameterItems(para);
            Parameters.Add(para);
        }

        private bool CanAddParameter()
        {
            return SelectedPossibleParameter != null;
        }

        private void OnRemoveParameter()
        {
            Parameters.Remove(SelectedParameter);
        }

        private bool CanRemoveParameter()
        {
            return SelectedParameter != null;
        }
    }
}
