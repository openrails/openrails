using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace TimeTableEditor.Model
{
    public class Station : BasicClass
    {
        private string _Name;

        public ObservableCollection<TimeTableCommand> Commands { get; set; }

        public Station()
        {
            Commands = new ObservableCollection<TimeTableCommand>();            
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
            }
        }

        public void newStation(string desc)
        {            
            string[] statarray = desc.Split('$');
            Name = statarray[0].Trim();
            if (statarray.Length > 1)
            {
                for (int i = 1; i < statarray.Length; i++)
                {
                    statarray[i] = "$" + statarray[i];
                    TimeTableCommand cmd = new TimeTableCommand();
                    cmd.newCommand(statarray[i]);
                    Commands.Add(cmd);
                }                
            }
        }

        public static string GetStationName(string stationstring)
        {
            string[] statarray = stationstring.Split('$');
            return statarray[0].Trim();
        }

        public string Stationstring()
        {
            string str = "";
            foreach(TimeTableCommand cmd in Commands)
            {
                str = str + " " + cmd.Name;
                if(cmd.HasValue)
                {
                    str = str + "=" + cmd.Value;
                }
            }
            return str;
        }

    }
}
