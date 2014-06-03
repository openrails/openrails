using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LibAE.Formats;

namespace ActivityEditor.Route_Metadata
{
    public partial class StationDisplay : Form
    {
        StationItem station;
        public StationDisplay(StationItem item)
        {
            station = item;
            InitializeComponent();
            this.StationName.Text = station.nameStation;
            ClearTable();
            
            int cntPath = this.tablePaths.RowCount;

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
        }

        public void ClearTable()
        {
            for (int cnt = tablePaths.RowCount - 1; cnt >= 0; cnt--)
            {
                tablePaths.RowStyles.RemoveAt(cnt);
            }
            this.tablePaths.RowStyles.Clear();  //first clear rowStyles
            tablePaths.RowCount = 0;
        }

        private void QuitStationConfig(object sender, EventArgs e)
        {
            Close();
        }

        public void AddPathData(string label, StationPaths paths)
        {
            int cntPath = 0;
            double passingYard = 0;
            int cntRows = tablePaths.RowCount;
            if (paths == null || paths.getPaths().Count <= 0)
                return;

            Label CinBoth = new Label() { Text = "--", Anchor = AnchorStyles.Left, AutoSize = true };
            if (paths.getPaths()[0].MainPath)
                CinBoth.Text = "M - " + label;
            else
                CinBoth.Text = label;

            do
            {
                int nbrGlobalItem = paths.getPaths()[cntPath].ComponentItem.Count - 1;
                Label CoutBoth = new Label() { Text = "--", Anchor = AnchorStyles.Left, AutoSize = true };
                GlobalItem lastItem = (GlobalItem)(paths.getPaths()[cntPath].ComponentItem[nbrGlobalItem]);
                if (lastItem.GetType() == typeof(AEBufferItem) && lastItem.associateNode.TrEndNode)
                {
                    CoutBoth.Text = ((AEBufferItem)lastItem).NameBuffer;
                }
                else if (lastItem.GetType() == typeof(TrackSegment) && lastItem.associateNode.TrJunctionNode == null)
                {
                    CoutBoth.Text = ((TrackSegment)(lastItem)).HasConnector.label;
                    if ((paths.getPaths()[cntPath].ComponentItem[0]).GetType() == typeof(TrackSegment))
                        passingYard = Math.Round(paths.getPaths()[cntPath].PassingYard, 1);
                }
                else
                    CoutBoth.Text = "###";
                //TextBox t1 = new TextBox();

                //l1.Text = "field : ";
                tablePaths.RowCount++;
                this.tablePaths.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 30 is the rows space
                this.tablePaths.Controls.Add(CinBoth, 0, cntRows);  // add label in column0
                this.tablePaths.Controls.Add(CoutBoth, 1, cntRows);  // add textbox in column1
                double yard = Math.Round(paths.getPaths()[cntPath].Platform, 1);
                Label platformLength = new Label() { Text = yard.ToString(), Anchor = AnchorStyles.Left, AutoSize = true };
                this.tablePaths.Controls.Add(platformLength, 2, cntRows);
                yard = Math.Round (paths.getPaths()[cntPath].Siding,1);
                Label sidingLength = new Label(){ Text = yard.ToString(), Anchor = AnchorStyles.Left, AutoSize = true };
                this.tablePaths.Controls.Add(sidingLength, 3, cntRows);

                Label nbrPlatform = new Label() { Text = paths.getPaths()[cntPath].NbrPlatform.ToString(), Anchor = AnchorStyles.Left, AutoSize = true };
                this.tablePaths.Controls.Add(nbrPlatform, 4, cntRows);
                Label nbrSiding = new Label() { Text = paths.getPaths()[cntPath].NbrSiding.ToString(), Anchor = AnchorStyles.Left, AutoSize = true };
                this.tablePaths.Controls.Add(nbrSiding, 5, cntRows);
                Label passingYardLength = new Label(){ Text = passingYard.ToString(), Anchor = AnchorStyles.Left, AutoSize = true };
                this.tablePaths.Controls.Add(passingYardLength, 6, cntRows);
                CinBoth = new Label() { Text = "--", Anchor = AnchorStyles.Left, AutoSize = true };
                cntRows++;
                cntPath++;
            } while (cntPath < paths.getPaths().Count);
        }
    }
}
