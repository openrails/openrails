using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace Orts.TimetableEditor.Model
{
    public class DisposeCommand : BasicClass
    {
        private string _Name;
        private string _Value;
        private bool _hasValue;
        private bool _used;
        private bool _hasItems;
        private bool _CanHaveParameters;
        private bool _HasParameters;
        private string _selectedItem;
        private DisposeParameter _selectedParameter;
        private string _selectedPossibleParameter;
        private ConsistsAndPaths _ConsAndPaths;

        public MyICommand SetValue { get; set; }
        public MyICommand AddParameter { get; set; }
        public MyICommand RemoveParameter { get; set; }

        public ObservableCollection<DisposeParameter> Parameters { get; set; }
        public ObservableCollection<string> Items { get; set; }
        public ObservableCollection<string> AvailableTrains { get; set; }
        public ObservableCollection<string> PoolTrains { get; set; }
        public ObservableCollection<string> PossibleParameters { get; set; }
        public ObservableCollection<string> PossibleParameterValues { get; set; }
        public ObservableCollection<string> Paths { get; set; }

        public DisposeCommand()
        {
            Parameters = new ObservableCollection<DisposeParameter>();
            Items = new ObservableCollection<string>();
            AvailableTrains = new ObservableCollection<string>();
            PoolTrains = new ObservableCollection<string>();
            PossibleParameters = new ObservableCollection<string>();
            PossibleParameterValues = new ObservableCollection<string>();
            Paths = new ObservableCollection<string>();
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
                if (Name == "$forms" )
                {
                    CanHaveParameters = true;
                    if (Value == null)
                    {
                        Value = "";
                    }
                    PossibleParameters.Add("");
                    PossibleParameters.Add("/runround");
                    PossibleParameters.Add("/rrtime");
                    PossibleParameters.Add("/closeup");
                    PossibleParameters.Add("/setstop");
                    PossibleParameters.Add("/atstation");
                    PossibleParameters.Add("/speed");
                    Items = AvailableTrains;
                }
                if(Name == "$triggers")
                {
                    CanHaveParameters = true;
                    PossibleParameters.Add("");
                    PossibleParameters.Add("/runround");
                    PossibleParameters.Add("/rrtime");
                    PossibleParameters.Add("/closeup");
                    PossibleParameters.Add("/setstop");
                    PossibleParameters.Add("/atstation");
                    PossibleParameters.Add("/speed");
                }
                if( Name == "$static" )
                {
                    CanHaveParameters = false;
                    PossibleParameters.Add("");
                    PossibleParameters.Add("/closeup");
                }
                if (Name == "$stable")
                {
                    CanHaveParameters = true;
                    PossibleParameters.Add("");
                    PossibleParameters.Add("/out_path");
                    PossibleParameters.Add("/out_time");
                    PossibleParameters.Add("/in_path");
                    PossibleParameters.Add("/in_time");
                    PossibleParameters.Add("/static");
                    PossibleParameters.Add("/runround");
                    PossibleParameters.Add("/rrtime");
                    PossibleParameters.Add("/rrpos");
                    PossibleParameters.Add("/forms");
                    PossibleParameters.Add("/triggers");
                    PossibleParameters.Add("/speed");
                    PossibleParameters.Add("/name");
                }
                if ( Name == "$detach" )
                {
                    CanHaveParameters = true;
                    PossibleParameters.Add("");
                    PossibleParameters.Add("/power");
                    PossibleParameters.Add("/leadingpower");
                    PossibleParameters.Add("/allleadingpower");
                    PossibleParameters.Add("/trailingpower");
                    PossibleParameters.Add("/alltrailingpower");
                    PossibleParameters.Add("/nonpower");
                    PossibleParameters.Add("/units");
                    PossibleParameters.Add("/static");
                    PossibleParameters.Add("/forms");
                }
                if ( Name == "$pickup" )
                {
                    CanHaveParameters = true;
                    PossibleParameters.Add("");
                    PossibleParameters.Add("/static");
                }
                if ( Name == "$attach")
                {
                    CanHaveParameters = false;
                    Items = AvailableTrains;
                }
                if ( Name == "$transfer" )
                {
                    CanHaveParameters = true;
                    PossibleParameters.Add("");
                    PossibleParameters.Add("/give");
                    PossibleParameters.Add("/take");
                    PossibleParameters.Add("/keep");
                    PossibleParameters.Add("/leave");
                    PossibleParameters.Add("/onepower");
                    PossibleParameters.Add("/allpower");
                    PossibleParameters.Add("/nonpower");
                    PossibleParameters.Add("/units");                    
                }
                if ( Name == "$activate" )
                {
                    CanHaveParameters = false;
                    Items = AvailableTrains;
                }
                if (Name == "$pool")
                {
                    CanHaveParameters = true;
                    Items = PoolTrains;
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

        public DisposeParameter SelectedParameter
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

        private void SetParameterItems(DisposeParameter parameter)
        {            
            if (parameter.ParameterName == "/pool")
            {
                parameter.Items = PoolTrains;
            }
            if (parameter.ParameterName == "/direction")
            {
                parameter.Items.Add("forward");
                parameter.Items.Add("backward");
            }
            if (parameter.ParameterName == "/units")
            {
                if (parameter.ParameterValue == null)
                {
                    parameter.ParameterValue = "";
                }
            }
            if(parameter.ParameterName=="/forms")
            {
                parameter.Items = AvailableTrains;
            }
            if(parameter.ParameterName == "/runround")
            {
                ConsistsAndPaths.PathsToString(parameter.Items);                
            }
            if(parameter.ParameterName=="/out_path")
            {
                ConsistsAndPaths.PathsToString(parameter.Items);
            }
            if(parameter.ParameterName=="/in_path")
            {
                ConsistsAndPaths.PathsToString(parameter.Items);
            }
            if (parameter.ParameterName == "/rrtime")
            {
                if (parameter.ParameterValue == null)
                {
                    parameter.ParameterValue = "";
                }
            }
            if (parameter.ParameterName == "/out_time")
            {
                if (parameter.ParameterValue == null)
                {
                    parameter.ParameterValue = "";
                }
            }
            if (parameter.ParameterName == "/in_time")
            {
                if (parameter.ParameterValue == null)
                {
                    parameter.ParameterValue = "";
                }
            }
            if(parameter.ParameterName == "/rrpos")
            {
                parameter.Items.Add("out");
                parameter.Items.Add("stable");
                parameter.Items.Add("in");
            }
            if(parameter.ParameterName == "/speed")
            {
                if (parameter.ParameterValue == null)
                {
                    parameter.ParameterValue = "";
                }
            }
            if (parameter.ParameterName == "/name")
            {
                if (parameter.ParameterValue == null)
                {
                    parameter.ParameterValue = "";
                }
            }
            if (parameter.ParameterName == "/triggers")
            {
                parameter.Items = AvailableTrains;
            }
        }

        public void newCommand(string cmd, ObservableCollection<string> Trains, ObservableCollection<string> TrainsFromPool, ConsistsAndPaths ConsPaths)
        {
            AvailableTrains = Trains;
            PoolTrains = TrainsFromPool;
            ConsistsAndPaths = ConsPaths;
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
            if (Name == "$pool")
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
            if (para.Length > 1)
            {
                for (int i = 1; i < para.Length; i++)
                {
                    DisposeParameter parameter = new DisposeParameter();
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
                foreach (DisposeParameter parameter in Parameters)
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
            DisposeParameter para = new DisposeParameter();
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
