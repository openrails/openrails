using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Orts.TimetableEditor.Views;
using Orts.TimetableEditor.Model;
using System.Windows.Documents;

namespace Orts.TimetableEditor.ViewModel
{
    public class TimetableViewModel : BasicClass
    {
        private DataTable timetable;
        private char delimiter;
        public ObservableCollection<string> StationFiles { get; set; }
        public ObservableCollection<string> Stations { get; set; }
        public ObservableCollection<string> OtherTrains { get; set; }
        public ObservableCollection<string> PoolTrains { get; set; }
        private string activityPath;
        private string routePath;
        private string pathsPath;
        private string consistPath;
        private string tdbfilename;
        private string selectedStationFile;
        private string selectedStation;
        private string ttFileName;
        private string ttname;
        private DataRow currentRow;
        private string selectedCell;
        private int currentRowIndex;
        private int currentColumnIndex;
        private string currentColumnIndexText;
        private Visibility showNewStationsButton;
        private Visibility showAdjustStationsButtons;
        private ConsistsAndPaths consandpaths;
        public MyICommand NewStationFile { get; set; }
        public MyICommand MoveStationUp { get; set; }
        public MyICommand MoveStationDown { get; set; }
        public MyICommand SaveNewStationFile { get; set; }
        public MyICommand StationsOK { get; set; }
        public MyICommand AddRow { get; set; }
        public MyICommand DeleteRow { get; set; }
        public MyICommand InsertRow { get; set; }
        public MyICommand AddCol { get; set; }
        public MyICommand DeleteCol { get; set; }
        public MyICommand InsertCol { get; set; }
        public MyICommand EditStations { get; set; }
        public MyICommand EditTrain { get; set; }
        public MyICommand Paste {  get; set; }
        public MyICommand Dispose { get; set; }
        public TimetableViewModel()
        {
            StationFiles = new ObservableCollection<string>();
            Stations = new ObservableCollection<string>();
            OtherTrains = new ObservableCollection<string>();
            PoolTrains = new ObservableCollection<string>();
            NewStationFile = new MyICommand(OnCreateNewStationFile, CanCreateNewStationFile);
            MoveStationUp = new MyICommand(OnMoveStationUp, CanMoveStationUp);
            MoveStationDown = new MyICommand(OnMoveStationDown, CanMoveStationDown);
            SaveNewStationFile = new MyICommand(OnSaveNewStationFile, CanSaveNewStationFile);
            StationsOK = new MyICommand(OnStationsOk, CanStationsOk);
            AddRow = new MyICommand(OnAddRow, CanAddRow);
            InsertRow = new MyICommand(OnInsertRow, CanInsertRow);
            DeleteRow = new MyICommand(OnDeleteRow, CanDeleteRow);
            AddCol = new MyICommand(OnAddCol, CanAddCol);
            InsertCol = new MyICommand(OnInsertCol, CanInsertCol);
            DeleteCol = new MyICommand(OnDeleteCol, CanDeleteCol);
            EditStations = new MyICommand(OnEditStations, CanEditStations);
            EditTrain = new MyICommand(OnEditTrain, CanEditTrain);
            Paste = new MyICommand(OnPaste, CanPaste);
            Dispose = new MyICommand(OnDispose, CanDispose);

            Timetable = new DataTable();
        }

        public DataTable Timetable
        {
            get
            {
                return timetable;
            }
            set
            {
                timetable = value;
                OnPropertyChanged(nameof(Timetable));
            }
        }

        public DataRow CurrentRow
        {
            get
            {
                return currentRow;
            }
            set
            {
                currentRow = value;
                OnPropertyChanged(nameof(CurrentRow));
                MessageBox.Show(Timetable.Rows.IndexOf(CurrentRow).ToString());
            }
        }

        public int CurrentRowIndex
        {
            get
            {
                return currentRowIndex;
            }
            set
            {
                currentRowIndex = value;
                OnPropertyChanged(nameof(CurrentRowIndex));
                InsertRow.RaiseCanExecuteChanged();
                DeleteRow.RaiseCanExecuteChanged();
                InsertCol.RaiseCanExecuteChanged();
                DeleteCol.RaiseCanExecuteChanged();
                EditTrain.RaiseCanExecuteChanged();
                Dispose.RaiseCanExecuteChanged();
            }
        }

        public string SelectedCell
        {
            get
            {
                return selectedCell;
            }
            set
            {
                selectedCell = value;
                OnPropertyChanged(nameof(SelectedCell));
                MessageBox.Show(SelectedCell);
            }
        }

        public int CurrentColumnIndex
        {
            get
            {
                return currentColumnIndex;
            }
            set
            {
                currentColumnIndex = value;
                OnPropertyChanged(nameof(CurrentColumnIndex));
                EditStations.RaiseCanExecuteChanged();
                EditTrain.RaiseCanExecuteChanged();
                Dispose.RaiseCanExecuteChanged();
                if (CurrentColumnIndex < 27)
                {
                    CurrentColumnIndexText = coltitle(CurrentColumnIndex);
                }
                else
                {
                    string n1 = coltitle(CurrentColumnIndex / 26);
                    string n2 = coltitle(CurrentColumnIndex % 26);
                    CurrentColumnIndexText = n1 + n2;
                }
            }
        }

        public string CurrentColumnIndexText
        {
            get
            {
                return currentColumnIndexText;
            }
            set
            {
                currentColumnIndexText = value;
                OnPropertyChanged(nameof(CurrentColumnIndexText));
            }
        }

        public string RoutePath
        {
            get
            {
                return routePath;
            }
            set
            {
                routePath = value;
                OnPropertyChanged(nameof(RoutePath));
            }
        }

        public string TdbFileName
        {
            get
            {
                return TdbFileName;
            }
            set
            {
                tdbfilename = value;
                OnPropertyChanged(nameof(TdbFileName));
            }
        }

        public ConsistsAndPaths ConsAndPaths
        {
            get
            {
                return consandpaths;
            }
            set
            {
                consandpaths = value;
                OnPropertyChanged(nameof(ConsAndPaths));
            }
        }

        public string SelectedStationFile
        {
            get
            {
                return selectedStationFile;
            }
            set
            {
                selectedStationFile = value;
                OnPropertyChanged(nameof(SelectedStationFile));
                StationsOK.RaiseCanExecuteChanged();
                if (selectedStationFile != null)
                {
                    loadStationFile(SelectedStationFile);
                }
            }
        }

        public string SelectedStation
        {
            get
            {
                return selectedStation;
            }
            set
            {
                selectedStation = value;
                OnPropertyChanged(nameof(SelectedStation));
                MoveStationDown.RaiseCanExecuteChanged();
                MoveStationUp.RaiseCanExecuteChanged();
            }
        }

        public string TTFileName
        {
            get
            {
                return ttFileName;
            }
            set
            {
                ttFileName = value;
                OnPropertyChanged(nameof(TTFileName));
                SaveNewStationFile.RaiseCanExecuteChanged();
            }
        }

        public string TTName
        {
            get
            {
                return ttname;
            }
            set
            {
                ttname = value;
                OnPropertyChanged(nameof(TTName));
                StationsOK.RaiseCanExecuteChanged();
            }
        }

        public Visibility ShowNewStationsButton
        {
            get
            {
                return showNewStationsButton;
            }
            set
            {
                showNewStationsButton = value;
                OnPropertyChanged(nameof(ShowNewStationsButton));
                MoveStationDown.RaiseCanExecuteChanged();
                MoveStationUp.RaiseCanExecuteChanged();
            }
        }

        public Visibility ShowAdjustStationsButtons
        {
            get
            {
                return showAdjustStationsButtons;
            }
            set
            {
                showAdjustStationsButtons = value;
                OnPropertyChanged(nameof(ShowAdjustStationsButtons));
                MoveStationDown.RaiseCanExecuteChanged();
                MoveStationUp.RaiseCanExecuteChanged();
            }
        }

        public void newTimetable(string tdbfilename, string routepath)
        {
            RoutePath = routepath;
            activityPath = routePath + "\\" + "Activities\\OpenRails";
            if (Directory.Exists(activityPath) == false)
            {
                Directory.CreateDirectory(activityPath);
            }
            string pt = RoutePath;
            pathsPath = pt + "\\" + "Paths";
            pt = pt + ".tst";
            pt = Path.GetDirectoryName(pt);
            pt = pt + ".tst";
            pt = Path.GetDirectoryName(pt);
            consistPath = pt;
            TdbFileName = tdbfilename;
            StationsView stationsView = new StationsView();
            stationsView.DataContext = this;

            bool hasFiles = loadStationFiles();
            if (hasFiles == true)
            {
                ShowNewStationsButton = Visibility.Visible;
                ShowAdjustStationsButtons = Visibility.Collapsed;
            }
            else
            {
                ShowNewStationsButton = Visibility.Collapsed;
                ShowAdjustStationsButtons = Visibility.Visible;
                extractStations(TdbFileName);
            }
            stationsView.ShowDialog();
        }

        public void LoadTimetable(string tdbfilename, string routepath, string timetablefile)
        {            
            RoutePath=routepath;
            activityPath = routePath + "\\" + "Activities\\OpenRails";
            delimiter=';';
            using (var reader = new StreamReader(timetablefile))
            {
                string getheader = reader.ReadLine();
                string[] headers = getheader.Split(delimiter);
                if (headers.Length == 1)
                {
                    delimiter = ',';
                    headers = getheader.Split(delimiter);                   
                }
                if (headers.Length == 1)
                {
                    delimiter = '\t';
                    headers = getheader.Split(delimiter);
                }
                Timetable.Columns.Add("_");
                foreach (string header in headers)
                {
                    Timetable.Columns.Add(header);
                }
                DataRow frow = Timetable.NewRow();
                frow[0] = "";
                for(int i=0; i<headers.Length; i++)
                {
                    frow[i+1] = headers[i];
                }
                Timetable.Rows.Add(frow);
                while (!reader.EndOfStream)
                {
                    string line = (Timetable.Rows.Count + 1).ToString() + delimiter + reader.ReadLine();
                    string[] row = line.Split(delimiter);
                    DataRow dr = Timetable.NewRow();
                    for (int i = 0; i < headers.Length; i++)
                    {
                        dr[i] = row[i];
                    }
                    Timetable.Rows.Add(dr);
                }
            }
            renumberColumns();
            renumberRows();
            int cn = 0;
            int rn = 0;
            DataRow rowt = Timetable.Rows[0];
            for (int c = 0; c < Timetable.Columns.Count; c++)
            {
                if (rowt[c].ToString() == "#comment")
                {
                    cn = c;
                    break;
                }
            }
            for (int r = 0; r < Timetable.Rows.Count; r++)
            {
                if (Timetable.Rows[r][1].ToString()=="#comment")
                {
                    rn = r;
                    break;
                }
            }
            rowt = Timetable.Rows[rn];
            TTName = rowt[cn].ToString();            
        }

        public void SaveTimetable(string filename)
        {
            if(delimiter=='\0')
            {
                delimiter = ';';
            }
            using (var writer = new StreamWriter(filename))
            {
                for (int r=0; r < Timetable.Rows.Count;r++)
                {
                    DataRow row = Timetable.Rows[r];
                    for(int c=1;c< Timetable.Columns.Count;c++)
                    {
                        writer.Write(row[c]);
                        if(c< Timetable.Columns.Count-1)
                        {
                            writer.Write(delimiter);
                        }
                    }
                    writer.Write(writer.NewLine);
                }
                writer.Close();
            }
        }

        private bool loadStationFiles()
        {
            StationFiles.Clear();
            bool res = false;
            if (Directory.Exists(activityPath))
            {
                string[] sfiles = Directory.GetFiles(activityPath);
                foreach (string file in sfiles)
                {
                    if (file.ToLower().EndsWith(".stations"))
                    {
                        StationFiles.Add(Path.GetFileName(file));
                        res = true;
                    }
                }
            }
            return res;
        }

        private void loadStationFile(string file)
        {
            Stations.Clear();
            using (StreamReader sr = new StreamReader(activityPath + "\\" + file))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    Stations.Add(line);
                }
            }
        }

        private void extractStations(string filename)
        {
            Stations.Clear();
            using (var reader = new StreamReader(filename))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (line.Contains("Station (") == true)
                    {
                        line = line.Replace("Station (", "");
                        line = line.Replace(")", "");
                        line = line.Replace("\"", "");
                        line = line.Trim();
                        if (Stations.IndexOf(line) == -1)
                        {
                            Stations.Add(line);
                        }
                    }
                }
            }
            SaveNewStationFile.RaiseCanExecuteChanged();
        }

        public void renumberColumns()
        {
            for (int i = 0; i < Timetable.Columns.Count; i++)
            {
                Timetable.Columns[i].ColumnName = Timetable.Columns[i].ColumnName + "_";
            }
            for (int i = 0; i < Timetable.Columns.Count; i++)
            {
                if (i < 26)
                {
                    Timetable.Columns[i].ColumnName = coltitle(i);
                }
                else
                {
                    string n1 = coltitle(i / 26);
                    string n2 = coltitle(i % 26);
                    Timetable.Columns[i].ColumnName = n1 + n2;
                }
            }
        }

        private string coltitle(int nr)
        {
            string res = "A";
            switch (nr)
            {
                case 0: res = "A"; break;
                case 1: res = "B"; break;
                case 2: res = "C"; break;
                case 3: res = "D"; break;
                case 4: res = "E"; break;
                case 5: res = "F"; break;
                case 6: res = "G"; break;
                case 7: res = "H"; break;
                case 8: res = "I"; break;
                case 9: res = "J"; break;
                case 10: res = "K"; break;
                case 11: res = "L"; break;
                case 12: res = "M"; break;
                case 13: res = "N"; break;
                case 14: res = "O"; break;
                case 15: res = "P"; break;
                case 16: res = "Q"; break;
                case 17: res = "R"; break;
                case 18: res = "S"; break;
                case 19: res = "T"; break;
                case 20: res = "U"; break;
                case 21: res = "V"; break;
                case 22: res = "W"; break;
                case 23: res = "X"; break;
                case 24: res = "Y"; break;
                case 25: res = "Z"; break;
            }
            return res;
        }

        private int colnumber(string name)
        {
            int nr = 0;
            switch(name)
            {
                case "A": nr = 0;break;
                case "B": nr = 1;break;
                case "C": nr = 2;break;
                case "D": nr = 3;break;
                case "E": nr = 4;break;
                case "F": nr = 5;break;
                case "G": nr = 6;break;
                case "H": nr = 7;break;
                case "I": nr = 8;break;
                case "J": nr = 9;break;
                case "K": nr = 10;break;
                case "L": nr = 11;break;
                case "M": nr = 12;break;
                case "N": nr = 13;break;
                case "O": nr = 14;break;
                case "P": nr = 15;break;
                case "Q": nr = 16;break;
                case "R": nr = 17;break;
                case "S": nr = 18;break;
                case "T": nr = 19;break;
                case "U": nr = 20;break;
                case "V": nr = 21;break;
                case "W": nr = 22;break;
                case "X": nr = 23;break;
                case "Y": nr = 24;break;
                case "Z": nr = 25;break;
            }
            return nr;
        }

        private void renumberRows()
        {
            Timetable.Columns[0].ReadOnly = false;
            for (int i = 0; i < Timetable.Rows.Count; i++)
            {
                DataRow row = Timetable.Rows[i];
                row[0]=(i+1).ToString();
            }
            Timetable.Columns[0].ReadOnly = true;
        }

        public void GetAllTrains(ObservableCollection<string> alltrains)
        {
            bool start = false;
            DataRow row = Timetable.Rows[0];
            for(int i=1;i<Timetable.Columns.Count;i++)
            {
                if(start)
                {
                    if (row[i]!="")
                    {
                        alltrains.Add(row[i].ToString()+":"+TTName);
                    }
                }
                if (row[i].ToString()=="#comment") start = true;
            }
        }

        public void GetUsedConsists(ObservableCollection<string> ConsForZip)
        {
            for (int r=1; r<Timetable.Rows.Count; r++)
            {
                DataRow row =Timetable.Rows[r];
                if (row[1].ToString() == "#consist")
                {
                    for(int c=2;c<Timetable.Columns.Count;c++)
                    {
                        if (row[c].ToString().Contains("+"))
                        {
                            TrainViewModel tvm = new TrainViewModel();
                            tvm.ConsistToList(row[c].ToString().Trim());
                            foreach(var con in tvm.Con)
                            {
                                ConsForZip.Add(con.Name);
                            }
                        }
                        else
                        {
                            string con = row[c].ToString().Trim();
                            if (con!="" && ConsForZip.IndexOf(con)==-1)
                            ConsForZip.Add(row[c].ToString());
                        }
                    }
                    break;
                }
            }
        }

        public void GetUsedPaths(ObservableCollection<string> PathsForZip)
        {
            for (int r=1; r<Timetable.Rows.Count;r++)
            {
                DataRow row = Timetable.Rows[r];
                if (row[1].ToString() == "#path")
                {
                    for(int c=2;c<Timetable.Columns.Count;c++)
                    {
                        string pat = row[c].ToString().Trim();
                        if(pat!="" && PathsForZip.IndexOf(pat)==-1)
                        {
                            PathsForZip.Add(pat);
                        }
                    }
                    break;
                }
            }
        }

        private void OnCreateNewStationFile()
        {
            extractStations(tdbfilename);
            ShowNewStationsButton = Visibility.Collapsed;
            ShowAdjustStationsButtons = Visibility.Visible;
            SelectedStationFile = null;
        }
        private bool CanCreateNewStationFile()
        {
            return true;
        }

        private void OnMoveStationUp()
        {
            Stations.Move(Stations.IndexOf(SelectedStation), Stations.IndexOf(SelectedStation) - 1);
            MoveStationDown.RaiseCanExecuteChanged();
            MoveStationUp.RaiseCanExecuteChanged();
        }
        private bool CanMoveStationUp()
        {
            bool res = false;
            if (SelectedStation != null)
            {
                if (Stations.IndexOf(SelectedStation) > 0)
                {
                    res = true;
                }
            }
            return res;
        }

        private void OnMoveStationDown()
        {
            Stations.Move(Stations.IndexOf(SelectedStation), Stations.IndexOf(SelectedStation) + 1);
            MoveStationDown.RaiseCanExecuteChanged();
            MoveStationUp.RaiseCanExecuteChanged();
        }
        private bool CanMoveStationDown()
        {
            bool res = false;
            if (SelectedStation != null)
            {
                if (Stations.IndexOf(SelectedStation) < Stations.Count - 1)
                {
                    res = true;
                }
            }
            return res;
        }

        private void OnSaveNewStationFile()
        {
            string fname = Path.GetFileNameWithoutExtension(TTFileName);
            bool found = false;
            foreach (string StationFile in StationFiles)
            {
                if (StationFile.ToLower() == fname.ToLower() + ".stations")
                {
                    found = true;
                }
            }
            if (found == true)
            {
                if (MessageBox.Show("Overwrite File", "File already exists", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    found = false;
                }
            }
            if (found == false)
            {
                using (StreamWriter sw = new StreamWriter(activityPath + "\\" + fname + ".stations"))
                {
                    foreach (string station in Stations)
                    {
                        sw.WriteLine(station);
                    }
                }
                ShowAdjustStationsButtons = Visibility.Collapsed;
                ShowNewStationsButton = Visibility.Visible;
            }
            loadStationFiles();
            SelectedStationFile = StationFiles[StationFiles.IndexOf(fname + ".stations")];
        }
        private bool CanSaveNewStationFile()
        {
            return TTFileName != null;
        }

        private void OnStationsOk()
        {
            Timetable.Columns.Add("_");
            Timetable.Columns[0].ReadOnly = true;
            Timetable.Columns.Add("A");
            Timetable.Columns.Add("B");
            Timetable.Columns.Add("C");
            DataRow dataRow = Timetable.NewRow();
            dataRow[0] = (Timetable.Rows.Count+1).ToString();
            dataRow[3] = "#comment";
            Timetable.Rows.Add(dataRow);
            dataRow = Timetable.NewRow();
            dataRow[0]= (Timetable.Rows.Count + 1).ToString();
            dataRow[1] = "#comment";
            dataRow[3] = TTName;
            Timetable.Rows.Add(dataRow);
            dataRow = Timetable.NewRow();
            dataRow[0]= (Timetable.Rows.Count + 1).ToString();
            dataRow[1] = "#path";
            Timetable.Rows.Add(dataRow);
            dataRow = Timetable.NewRow();
            dataRow[0] = (Timetable.Rows.Count + 1).ToString();
            dataRow[1] = "#consist";
            Timetable.Rows.Add(dataRow);
            dataRow = Timetable.NewRow();
            dataRow[0] = (Timetable.Rows.Count + 1).ToString();
            dataRow[1] = "#comment";
            Timetable.Rows.Add(dataRow);
            dataRow = Timetable.NewRow();
            dataRow[0]= (Timetable.Rows.Count + 1).ToString();
            Timetable.Rows.Add(dataRow);
            foreach (string station in Stations)
            {
                dataRow = Timetable.NewRow();
                dataRow[0] = (Timetable.Rows.Count + 1).ToString();
                dataRow[1] = station;
                Timetable.Rows.Add(dataRow);
            }
            dataRow = Timetable.NewRow();
            dataRow[0]= (Timetable.Rows.Count + 1).ToString();
            Timetable.Rows.Add(dataRow);
            dataRow = Timetable.NewRow();
            dataRow[0] = (Timetable.Rows.Count + 1).ToString();
            dataRow[1] = "#dispose";
            Timetable.Rows.Add(dataRow);
        }
        private bool CanStationsOk()
        {
            bool res = false;
            if (SelectedStationFile != null)
            {
                if (TTName != null)
                {
                    if (TTName != "")
                    {
                        res = true;
                    }
                }
            }
            return res;
        }

        private void OnAddRow()
        {
            DataRow dataRow = Timetable.NewRow();
            Timetable.Rows.Add();
            renumberRows();
        }
        private bool CanAddRow()
        {
          return true;
        }
        private void OnInsertRow()
        {
            DataRow dataRow = Timetable.NewRow();
            Timetable.Rows.InsertAt(dataRow, CurrentRowIndex);
            renumberRows();
        }
        private bool CanInsertRow()
        {
            bool res = false;
            if (CurrentRowIndex > 0)
            {
                res = true;
            }
            return res;
        }
        private void OnDeleteRow()
        {
            if (MessageBox.Show("Delete current Row?", "Delete Row", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Timetable.Rows[CurrentRowIndex].Delete();
                renumberRows();
            }
        }
        private bool CanDeleteRow()
        {
            bool res = false;
            if(CurrentRowIndex>0)
            {
                res = true;
            }
            return res;
        }
        private void OnAddCol()
        {
            DataTable old;
            Timetable.Columns.Add("");
            renumberColumns();
            old = Timetable;           
            Timetable = null;
            Timetable = old;
            OnPropertyChanged(nameof(Timetable));
        }
        private bool CanAddCol()
        {
            return true;
        }
        private void OnInsertCol()
        {
            DataTable old;
            Timetable.Columns.Add("").SetOrdinal(CurrentColumnIndex);
            renumberColumns();
            old = Timetable;
            Timetable = null;
            Timetable = old;
            OnPropertyChanged(nameof(Timetable));
        }
        private bool CanInsertCol()
        {
            return true;
        }
        private void OnDeleteCol()
        {
            if (MessageBox.Show("Delete current column?", "Delete Column", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                DataTable old;
                Timetable.Columns.Remove(Timetable.Columns[CurrentColumnIndex]);
                renumberColumns();
                old = Timetable;
                Timetable = null;
                Timetable = old;
                OnPropertyChanged(nameof(Timetable));
            }
        }
        private bool CanDeleteCol()
        {
            return true;
        }

        private void OnEditStations()
        {
            StationParamsViewModel stParams = new StationParamsViewModel();
            foreach(DataRow row in Timetable.Rows)
            {
                if (!row[1].ToString().StartsWith("#") && row[1].ToString()!="")
                {
                    Station station = new Station();
                    station.newStation(row[1].ToString());
                    stParams.Stations.Add(station);
                }
            }
            StationParametersView st = new StationParametersView();
            st.DataContext = stParams;
            st.ShowDialog();
            if(st.DialogResult==true)
            {
                foreach(Station station in stParams.Stations)
                {
                    foreach(DataRow row in Timetable.Rows)
                    {
                        if(!row[1].ToString().StartsWith("#") && row[1].ToString()!="")
                        {
                            if (Station.GetStationName(row[1].ToString())==station.Name)
                            {
                                row[1] = station.Name + station.Stationstring();
                            }
                        }
                    }
                }
            }
        }

        private bool CanEditStations()
        {
            if(CurrentColumnIndex==1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void OnEditTrain()
        {
            if(ConsAndPaths.Consists.Count==0 || ConsAndPaths.Paths.Count==0)
            {
                waiting waitingscreen = new waiting();
                waitingscreen.Show();
                if(ConsAndPaths.Consists.Count==0)
                {
                    ConsAndPaths.loadConsists();
                }                
                if(ConsAndPaths.Paths.Count==0)
                {
                    ConsAndPaths.loadPaths();
                }
                waitingscreen.Close();
            }
            TrainViewModel model = new TrainViewModel();           
            model.ConsistsAndPaths = ConsAndPaths;
            model.sortConsists();
            model.sortPaths();
            model.PoolTrains = PoolTrains;
            TrainView trains = new TrainView();
            for (int r = 0; r < Timetable.Rows.Count; r++)
            {
                if(r==0)
                {
                    model.TrainName = Timetable.Rows[r][CurrentColumnIndex].ToString();
                    model.GetAvailableCons(Timetable.Rows[r], Timetable.Columns.Count, CurrentColumnIndex);
                    if(OtherTrains.Count>0)
                    {
                        foreach(string tr in OtherTrains)
                        {
                            model.AvailableTrains.Add(tr);
                        }
                    }
                }
                if (Timetable.Rows[r][1].ToString() == "#comment")
                {
                    model.Comment = Timetable.Rows[r][CurrentColumnIndex].ToString();
                }
                if (Timetable.Rows[r][1].ToString()=="#path")
                {
                    model.ChosenTrainPath = Timetable.Rows[r][CurrentColumnIndex].ToString();
                }
                if (Timetable.Rows[r][1].ToString()=="#consist")
                {
                    model.ConsistToList(Timetable.Rows[r][CurrentColumnIndex].ToString());                  
                }
                if (Timetable.Rows[r][1].ToString() == "#start")
                { 
                    model.StartConsist(Timetable.Rows[r][CurrentColumnIndex].ToString());
                }
            }
            trains.DataContext = model;
            trains.ShowDialog();
            if(trains.DialogResult==true)
            {
                bool firstcomment = true;
                for(int r=0; r<Timetable.Rows.Count;r++)
                {
                    if (Timetable.Rows[r][1].ToString()=="#consist")
                    {
                        string con = "";
                        foreach(Consist consist in model.Con)
                        {
                            if(con!="")
                            {
                                con = con + "+";
                            }
                            con = con + consist.GetConsist().Trim();
                        }
                        Timetable.Rows[r][CurrentColumnIndex] = con;
                    }
                    if (Timetable.Rows[r][1].ToString()=="#start")
                    {
                        Timetable.Rows[r][CurrentColumnIndex] = model.GetStartString().Trim();
                    }
                    if (Timetable.Rows[r][1].ToString()=="#path")
                    {
                        Timetable.Rows[r][CurrentColumnIndex] = model.ChosenTrainPath.Trim();
                    }
                    if (Timetable.Rows[r][1].ToString()=="#comment" && firstcomment)
                    {
                        Timetable.Rows[r][CurrentColumnIndex] = model.Comment.Trim();
                        firstcomment = false;
                    }
                    if (r==0)
                    {
                        Timetable.Rows[r][CurrentColumnIndex] = model.TrainName.Trim();
                    }
                }
            }
        }

        private bool CanEditTrain()
        {
            bool res = false;
            if(CurrentColumnIndex > 2)
            {
                DataRow dataRow = Timetable.Rows[0];
                if (!dataRow[CurrentColumnIndex].ToString().StartsWith("#"))
                {
                    res = true;
                }
            }
            return res;
        }

        private void OnPaste()
        {
            string[] lines = Clipboard.GetText().Split('\n');
            int rows = lines.Length-1;
            int columns = 0;
            for(int i=0; i<lines.Count()-1;i++)
            {
                string[] cols = lines[i].Split('\t');
                columns = cols.Length;
            }
            if (CurrentColumnIndex + columns > Timetable.Columns.Count || CurrentRowIndex + rows > Timetable.Rows.Count)
            {
                MessageBox.Show("Not enough room for data. Paste canceled");
            }
            else
            {
                for (int r = 0; r < lines.Count(); r++)
                {
                    string[] cols = lines[r].Split('\t');
                    for (int c = 0; c < cols.Count(); c++)
                    {
                        DataRow dataRow = Timetable.Rows[CurrentRowIndex + r];
                        dataRow[currentColumnIndex + c] = cols[c].Trim();
                    }
                }
            }
        }

        private bool CanPaste()
        {
            return true;
        }

        private void OnDispose()
        {
            if (ConsAndPaths.Consists.Count == 0 || ConsAndPaths.Paths.Count == 0)
            {
                waiting waitingscreen = new waiting();
                waitingscreen.Show();
                if (ConsAndPaths.Consists.Count == 0)
                {
                    ConsAndPaths.loadConsists();
                }
                if (ConsAndPaths.Paths.Count == 0)
                {
                    ConsAndPaths.loadPaths();
                }
                waitingscreen.Close();
            }
            DisposeViewModel dispvm = new DisposeViewModel();            
            dispvm.ConsistsAndPaths = ConsAndPaths;
            dispvm.PoolTrains = PoolTrains;
            DisposeView disp = new DisposeView();
            disp.DataContext = dispvm;
            for (int r = 0; r < Timetable.Rows.Count; r++)
            {
                if(r==0)
                {
                    dispvm.GetAvailableCons(Timetable.Rows[r], Timetable.Columns.Count, CurrentColumnIndex);
                    if (OtherTrains.Count > 0)
                    {
                        foreach (string tr in OtherTrains)
                        {
                            dispvm.AvailableTrains.Add(tr);
                        }
                    }
                }
                if (Timetable.Rows[r][1].ToString() == "#dispose")
                {
                    dispvm.ParseDispose(Timetable.Rows[r][CurrentColumnIndex].ToString());
                }
            }
            disp.ShowDialog();
        }

        private bool CanDispose()
        {
            bool res = false;
            if (CurrentColumnIndex > 2)
            {
                DataRow dataRow = Timetable.Rows[0];
                if (!dataRow[CurrentColumnIndex].ToString().StartsWith("#"))
                {
                    res = true;
                }
            }
            return res;
        }
    }
}
