using System;
using System.Diagnostics;
using System.Windows.Forms;
using Orts.Formats.OR;

namespace ActivityEditor.Route_Metadata
{
    public partial class StationDisplay : Form
    {
        StationItem station;
        public StationDisplay(StationItem item)
        {
            int countPaths = 0;
            station = item;
            InitializeComponent();
            SuspendLayout();
            tablePaths.SuspendLayout();
            StationName.Text = station.nameStation;
            ClearTable();

            int cntPath = this.tablePaths.RowCount;
            foreach (var pointArea in item.stationArea)
            {
                if (pointArea.IsInterface())
                {
                    countPaths += pointArea.getStationConnector().stationPaths.getPaths().Count;
                }
            }

            if (countPaths > 0 && countPaths != item.StationPathsHelper.DefinedPath.Count)
            {
                this.label7.Text = "Please Load Paths:";
            }
            else
            {
                label7.Text = "List of Paths seems OK:";
            }

            foreach (var originPoint in station.StationPathsHelper.DefinedPath)
            {
                foreach (var destinPoint in originPoint.Value)
                {
                    foreach (var possibility in destinPoint.Value)
                    {
                        AddPathData(originPoint.Key, possibility.Value);
                    }
                }
            }
            tablePaths.ResumeLayout(false);
            tablePaths.PerformLayout();
            ResumeLayout(false);
#if false
		    foreach (var pointArea in item.stationArea)
            {
                if (pointArea.IsInterface() && pointArea.getStationConnector().stationPaths.getPaths().Count > 0)
                {
                    AddPathData(pointArea.stationConnector.getLabel(), pointArea.stationConnector.stationPaths);
                }
            }
            foreach (AEBufferItem buffer in item.insideBuffers)
            {
                AddPathData(buffer.NameBuffer, buffer.stationPaths);
            }
  
#endif        
        }

        public void ClearTable()
        {
            for (int cnt = tablePaths.RowCount - 1; cnt >= 0; cnt--)
            {
                tablePaths.RowStyles.RemoveAt(cnt);
            }
            tablePaths.RowStyles.Clear();  //first clear rowStyles
            tablePaths.RowCount = 0;
        }

        private void QuitStationConfig(object sender, EventArgs e)
        {
            Close();
        }

        public void AddPathData(string label, StationPath path)
        {
            Stopwatch stopWatch = new Stopwatch();
            TimeSpan ts;

            double passingYard = 0;
            int cntRows = tablePaths.RowCount;
            if (path == null)
                return;

            Label CinBoth = new Label() { Text = "--", Anchor = AnchorStyles.Left, AutoSize = true };
            if (path.MainPath)
                CinBoth.Text = "M - " + label;
            else
                CinBoth.Text = label;

            int nbrGlobalItem = path.ComponentItem.Count - 1;
            Label CoutBoth = new Label() { Text = "--", Anchor = AnchorStyles.Left, AutoSize = true };
            GlobalItem lastItem = (GlobalItem)(path.ComponentItem[nbrGlobalItem]);
            if (lastItem.GetType() == typeof(AEBufferItem) && lastItem.associateNode.TrEndNode)
            {
                CoutBoth.Text = ((AEBufferItem)lastItem).NameBuffer;
            }
            else if (lastItem.GetType() == typeof(TrackSegment) && lastItem.associateNode.TrJunctionNode == null)
            {
                CoutBoth.Text = ((TrackSegment)(lastItem)).HasConnector.label;
                if ((path.ComponentItem[0]).GetType() == typeof(TrackSegment))
                    passingYard = Math.Round(path.PassingYard, 1);
            }
            else
                CoutBoth.Text = "###";
            //TextBox t1 = new TextBox();

            //l1.Text = "field : ";
            stopWatch.Start();
            tablePaths.RowCount++;
            this.tablePaths.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 30 is the rows space
            this.tablePaths.Controls.Add(CinBoth, 0, cntRows);  // add label in column0
            this.tablePaths.Controls.Add(CoutBoth, 1, cntRows);  // add textbox in column1
            double yard = Math.Round(path.Platform, 1);
            Label platformLength = new Label() { Text = yard.ToString(), Anchor = AnchorStyles.Left, AutoSize = true };
            this.tablePaths.Controls.Add(platformLength, 2, cntRows);
            yard = Math.Round(path.Siding, 1);
            Label sidingLength = new Label() { Text = yard.ToString(), Anchor = AnchorStyles.Left, AutoSize = true };
            this.tablePaths.Controls.Add(sidingLength, 3, cntRows);

            Label nbrPlatform = new Label() { Text = path.NbrPlatform.ToString(), Anchor = AnchorStyles.Left, AutoSize = true };
            this.tablePaths.Controls.Add(nbrPlatform, 4, cntRows);
            Label nbrSiding = new Label() { Text = path.NbrSiding.ToString(), Anchor = AnchorStyles.Left, AutoSize = true };
            this.tablePaths.Controls.Add(nbrSiding, 5, cntRows);
            Label passingYardLength = new Label() { Text = passingYard.ToString(), Anchor = AnchorStyles.Left, AutoSize = true };
            this.tablePaths.Controls.Add(passingYardLength, 6, cntRows);
            CinBoth = new Label() { Text = "--", Anchor = AnchorStyles.Left, AutoSize = true };
            cntRows++;
            ts = stopWatch.Elapsed;
        }

        private void LoadPaths(object sender, EventArgs e)
        {
            SuspendLayout();
            tablePaths.SuspendLayout();
            station.StationPathsHelper.Reload();
            foreach (var originPoint in station.StationPathsHelper.DefinedPath)
            {
                foreach (var destinPoint in originPoint.Value)
                {
                    foreach (var possibility in destinPoint.Value)
                    {
                        AddPathData(originPoint.Key, possibility.Value);
                    }
                }
            }
            this.label7.Text = "Loaded:";
            tablePaths.ResumeLayout(false);
            tablePaths.PerformLayout();
            ResumeLayout(false);
        }
    }
}
