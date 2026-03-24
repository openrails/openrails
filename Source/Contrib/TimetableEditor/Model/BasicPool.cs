using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orts.TimetableEditor.Model
{
    public class BasicPool : BasicClass
    {
        private string _comment;
        private string _name;
        private string _storage;
        private string _access;
        private string _maxunits;
        private string _settings;

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

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value; 
                OnPropertyChanged(nameof(Name));
            }
        }

        public string Storage
        {
            get
            {
                return _storage;
            }
            set
            {
                _storage = value;
                OnPropertyChanged(nameof(Storage));
            }
        }

        public string Access
        {
            get
            {
                return _access;
            }
            set
            {
                _access = value;
                OnPropertyChanged(nameof(Access));
            }
        }

        public string Maxunits
        {
            get
            {
                return _maxunits;
            }
            set
            {
                _maxunits = value;
                OnPropertyChanged(nameof(Maxunits));
            }
        }

        public string Settings
        {
            get
            {
                return _settings;
            }
            set
            {
                _settings = value;
                OnPropertyChanged(nameof(Settings));
            }
        }
    }
}
