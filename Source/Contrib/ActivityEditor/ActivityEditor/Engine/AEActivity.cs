// 
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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AEWizard;
using LibAE;
using LibAE.Formats;
using System.Xml.Serialization;
using Orts.Formats.OR;

namespace ActivityEditor.Engine
{
    public class AEActivity
    {
        public AERouteConfig aeRouteConfig;
        public ActivityInfo activityInfo;
        private bool tagPanelVisibility = true;
        private bool stationPanelVisibility = true;
        public bool ActivityClosing = false;

        public bool saveEnabled = false;
        public bool saveAsEnabled = false;
        public bool loadEnabled = true;    //  Maybe to change as preference ComponentItem setted
        public System.Windows.Forms.SaveFileDialog SaveActivity;


        public AEActivity()
        {
        }

        public string getActivityName()
        {
            return activityInfo.ActivityName;
        }

        public void LoadPanels(System.Windows.Forms.GroupBox activityBox)
        {
            activityBox.Visible = true;
        }

        public void LoadActivity(ActivityInfo activity)
        {

            activityInfo = activity;
            Load();
            return;
        }

        public void Save()
        {
            aeRouteConfig.SaveRoute();
            saveEnabled = true;
        }

        public void Load()
        {
            saveEnabled = true;
            saveAsEnabled = true;
        }

        public void New()
        {
            saveAsEnabled = true;
        }

        public bool GetTagVisibility()
        {
            return tagPanelVisibility;
        }

        public void SetTagVisibility(bool info)
        {
            tagPanelVisibility = info;
        }

        public bool GetStationVisibility()
        {
            return stationPanelVisibility;
        }

        public void SetStationVisibility(bool info)
        {
            stationPanelVisibility = info;
        }

        public void ClosegActivity()
        {
            DialogResult result = MessageBox.Show("Do you want to save?", "Save activity ?",
                                    MessageBoxButtons.OKCancel);
            switch (result)
            {
                case DialogResult.OK:
                    {
                        if (activityInfo.FileName == null || activityInfo.FileName.Length > 0)
                            if (!SaveAs(activityInfo))
                            {
                                activityInfo.saveActivity();
                            }
                        break;
                    }
                case DialogResult.Cancel:
                    {
                        break;
                    }
            }
        }

        public bool SaveAs(ActivityInfo activity)
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ActEditor));
            this.SaveActivity = new System.Windows.Forms.SaveFileDialog();
            this.SaveActivity.InitialDirectory = activity.RoutePath;
            this.SaveActivity.DefaultExt = "act.json";
            resources.ApplyResources(this.SaveActivity, "SaveActivity");

            if (this.SaveActivity.ShowDialog() == DialogResult.OK)
            {
                activity.saveActivity(this.SaveActivity.FileName);
                return true;
            }
            return false;
        }

        public void AddActItem(GlobalItem item)
        {
            activityInfo.AddActItem(item);
        }

        public List<GlobalItem> getActItem()
        {
            return activityInfo.ActItem;
        }
    }
}
