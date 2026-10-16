using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Orts.TimetableEditor.ViewModel
{
    public class OpenSaveViewModel : INotifyPropertyChanged
    {
        public MyICommand OKClick { get; set; }
        public ObservableCollection<string> Files { get; set; }

        private string _SelectedFileName;
        private string _OKButtonText;
        private string _Filename;
        public OpenSaveViewModel()
        {
            Files = new ObservableCollection<string>();
            OKButtonText = "OK";
            OKClick = new MyICommand(OnOkClick, CanOkClick);
        }

        public string SelectedFileName
        {
            get
            {
                return _SelectedFileName;
            }
            set
            {
                _SelectedFileName = value;
                OnPropertyChanged(nameof(SelectedFileName));
                OKClick.RaiseCanExecuteChanged();
                if(SelectedFileName != null )
                {
                    //Filename = SelectedFileName.Substring(0, SelectedFileName.IndexOf(".timetable"));
                    Filename = SelectedFileName;
                }
            }
        }

        public string Filename
        {
            get
            {
                return _Filename;
            }
            set
            {
                _Filename = value;
                OnPropertyChanged(nameof(Filename));
                OKClick.RaiseCanExecuteChanged();
            }
        }

        public string OKButtonText
        {
            get
            {
                return _OKButtonText;
            }
            set
            {
                _OKButtonText = value;
                OnPropertyChanged(nameof(OKButtonText));
            }
        }

        public void ListFiles(string path, List<string> filter)
        {
            Files.Clear();
            foreach (string filter1 in filter)
            {
                string[] files = Directory.GetFiles(path, filter1);
                foreach (string file in files)
                {
                    Files.Add(Path.GetFileName(file));
                }
            }
        }

        public void SetModus(string modus)
        {
            OKButtonText = modus;
        }

        private void OnOkClick()
        {

        }

        private bool CanOkClick()
        {
            return SelectedFileName != null || Filename !=null;
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
