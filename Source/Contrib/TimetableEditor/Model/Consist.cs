using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orts.TimetableEditor.Model
{
    public class Consist : INotifyPropertyChanged
    {
        private string _Name;
        private bool _reversed;

        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name= value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public bool Reversed
        {
            get
            {
                return _reversed;
            }
            set
            {
                _reversed= value;
                OnPropertyChanged(nameof(Reversed));
            }
        }

        public string GetConsist()
        {
            string res = Name;
            if (Reversed)
            {
                res = Name + " $reverse";
            }
            return res;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
