using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Orts.TimetableEditor.Model
{
    public class DataFile : INotifyPropertyChanged
    {
        private string _fileName;
        private string _Name;
        private string _Type;

        public string FileName
        {
            get
            { 
                return _fileName; 
            }
            set 
            { 
                _fileName = value; 
                OnPropertyChanged(nameof(FileName));
            }
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

        public string Type
        {
            get
            {
                return _Type;
            }
            set
            {
                _Type = value;
                OnPropertyChanged(nameof(Type));
            }
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

    public class ConsistsAndPaths : INotifyPropertyChanged
    {
        private string _ConsistsPath;
        private string _PathsPath;

        public ObservableCollection<DataFile> Consists { get; set; }
        public ObservableCollection<DataFile> Paths { get; set; }

        public string ConsistsPath
        {
            get
            {
                return _ConsistsPath;
            }
            set
            {
                _ConsistsPath = value;
                OnPropertyChanged(nameof(ConsistsPath));
            }
        }

        public string PathsPath
        { 
            get
            {
                return _PathsPath;
            }
            set
            {
                _PathsPath = value;
                OnPropertyChanged(nameof(PathsPath));
            }
        }

        public ConsistsAndPaths()
        {
            Consists = new ObservableCollection<DataFile>();
            Paths = new ObservableCollection<DataFile>();
        }

        public void PathsToString(ObservableCollection<string> paths)
        {
            paths.Clear();
            foreach (DataFile dataFile in Paths)
            {
                paths.Add(dataFile.Name);
            }
        }

        public void loadPaths()
        {
            Paths.Clear();
            string[] files = Directory.GetFiles(PathsPath, "*.pat");
            foreach(string file in files)
            {
                bool siding = false;
                string name = "";
                using (StreamReader sr = new StreamReader(file))
                {
                    string line;
                    while((line = sr.ReadLine()) != null)
                    {
                        if(line.Contains("TrPathName"))
                        {
                            name = line.Replace("TrPathName", "");
                            name = name.Replace("\"", "");
                            name = name.Replace("(","");
                            name = name.Replace(")", "");
                            name = name.Trim();
                        }
                    }
                    if(name=="")
                    {
                        name = Path.GetFileNameWithoutExtension(file);
                    }
                    if(file.StartsWith("sid_"))
                    {
                        siding = true;
                    }
                    DataFile pt = new DataFile();
                    pt.Name = name;
                    pt.FileName = Path.GetFileNameWithoutExtension(file);
                    if(siding)
                    {
                        pt.Type = "s";
                    }
                    else
                    {
                        pt.Type = "n";
                    }
                    Paths.Add(pt);
                }
            }
        }

        public void loadConsists()
        {
            Consists.Clear();
            string[] files = Directory.GetFiles(ConsistsPath, "*.con");
            foreach (string file in files)
            {
                bool engine = false;
                string name = "";
                using (StreamReader sr = new StreamReader(file))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if(line.Contains("Name"))
                        {
                            name = line.Replace("Name","");
                            name = name.Replace("\"", "");
                            name = name.Replace("(", "");
                            name = name.Replace(")", "");
                            name = name.Trim();
                        }
                        if(line.Trim().StartsWith("Engine"))
                        {
                            engine = true;
                        }
                    }
                    if(name=="")
                    {
                        name = Path.GetFileNameWithoutExtension(file);                       
                    }
                    DataFile consist = new DataFile();
                    {
                        consist.Name = name;
                        consist.FileName = Path.GetFileNameWithoutExtension(file);
                        if(engine==false)
                        {
                            consist.Type = "w";
                        }
                        else
                        {
                            consist.Type = "e";
                        }
                        Consists.Add(consist);
                    }
                }

            }
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
