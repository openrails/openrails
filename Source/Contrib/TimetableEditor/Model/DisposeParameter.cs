using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Orts.TimetableEditor.Model
{
    public class DisposeParameter : BasicClass
    {
        private string _ParameterName;
        private string _ParameterValue;
        private bool _used;
        private bool _hasValue;
        private string _selectedItem;
        public MyICommand SetValue { get; set; }
        public ObservableCollection<string> Items { get; set; }

        public DisposeParameter()
        {
            Items = new ObservableCollection<string>();
            SetValue = new MyICommand(OnSetValue, CanSetValue);
        }

        public string ParameterName
        {
            get
            {
                return _ParameterName;
            }
            set
            {
                _ParameterName = value;
                OnPropertyChanged(nameof(ParameterName));
            }
        }

        public string ParameterValue
        {
            get
            {
                return _ParameterValue;
            }
            set
            {
                _ParameterValue = value;
                OnPropertyChanged(nameof(ParameterValue));
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

        public bool HasValue
        {
            get
            {
                return ParameterValue != null;
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

        public void newParameter(string par)
        {
            string[] pdev = par.Split('=');
            ParameterName = pdev[0];
            if (pdev.Length > 1)
            {
                ParameterValue = pdev[1];
            }
        }

        public string GetParameterString()
        {
            string ret = " " + ParameterName;
            if (ParameterValue != null)
            {
                ret = ret + "=" + ParameterValue;
            }
            return ret;
        }

        private void OnSetValue()
        {
            ParameterValue = SelectedItem;
        }

        private bool CanSetValue()
        {
            return SelectedItem != null;
        }
    }
}
