// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

/// This module ...
/// 
/// Author: Stéfan Paitoni
/// Updates : 
/// 
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Orts.Formats.OR;

namespace ActivityEditor.Engine
{
    public partial class StationInterface : Form
    {
        public List<string> AllowedDirections;
        public bool ToRemove = false;
        StationConnector stationConnector;

        public StationInterface(StationConnector info)
        {
            stationConnector = info;
            List<string> availValues = stationConnector.getAllowedDirections();
            InitializeComponent();
            AllowCB.DataSource = availValues;
            AllowCB.Text = availValues[(int)stationConnector.getDirConnector()];
            this.ConnectionLabel.Text = stationConnector.getLabel();
            this.TCSectionBox.Text = "Not yet implemented";
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            //if (MessageBox.Show("Really Quit?", "Exit", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                if (this.ConnectionLabel.Text == "" || this.ConnectionLabel.Text == "Please, give a name")
                {
                    this.ConnectionLabel.Text = "Please, give a name";
                }
                else
                {
                    stationConnector.setLabel(this.ConnectionLabel.Text);
                    stationConnector.setDirConnector(this.AllowCB.Text);

                    Close();
                }
            }
        }

        private void RemButton_Click(object sender, EventArgs e)
        {
            ToRemove = true;
            Close();
        }
    }
}
