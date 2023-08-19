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
using System.Xml.Serialization;
using ORTS;
using ORTS.Settings;
using LibAE.Formats;
using Orts.Formats.OR;

namespace ActivityEditor.Preference
{
    [Serializable()]
    public class AEPreference
    {
        [XmlIgnore]
        public bool classFilled
        {
            get
            {
                if (RoutePaths != null && RoutePaths.Count > 0 && MSTSPath.Length > 0)
                    return true;
                else
                    return false;
            }
        }
        [XmlIgnore]
        public UserSettings settings;
        [XmlIgnore]
        public ORConfig orConfig;   //  OR base configuration for routes
        [XmlIgnore]
        public bool SaveConfig = false;
        [XmlIgnore]
        public ActEditor ActEditor { get; set; }


        public List<string> RoutePaths { get; set; }
        public string MSTSPath { get; set; }
        public string AEPath { get; set; }  //  No more editable, given through user settings
        public bool ShowAllSignal = false;
        public bool ShowSnapCircle = false;
        public int SnapCircle = 0;
        public bool ShowPlSiLabel = false;
        public float PlSiZoom = 1;
        public bool ShowTiles { get; set; }
        public bool ShowSnapLine { get; set; }
        public bool ShowSnapInfo { get; set; }
        public bool ShowRuler { get; set; }
        public bool ShowTrackInfo { get; set; }
        public bool ActivateHorn { get; set; }
        [XmlIgnore]
        public ActionContainer ActionContainer 
        {
            get
            {
                if (ActEditor != null && ActEditor.selectedViewer != null && ActEditor.selectedViewer.Simulator != null)
                {
                    return ActEditor.selectedViewer.Simulator.GetOrRouteConfig().ActionContainer;
                }
                return null;
            }
            protected set { } 
        }
        //  Info for AuxAction option window.
        [XmlIgnore]
        public List<string> AvailableActions 
        {
            get
            {
                if (ActionContainer != null)
                    return ActionContainer.AvailableActions;
                return new List<string>();
            }
            set { }
        }
        [XmlIgnore]
        public List<string> UsedActions
        {
            get
            {
                if (ActionContainer != null)
                    return ActionContainer.UsedActions;
                return new List<string>();
            }
            set { }
        }

        public bool AllSignalProperty
        { 
            get 
            { 
                return ShowAllSignal; 
            } 
            set 
            { 
                ShowAllSignal = value; 
            } 
        }

        public AEPreference()
        {
            RoutePaths = new List<string>();
            AvailableActions = new List<string>();
            UsedActions = new List<string>();

            MSTSPath = "";
            AEPath = "";
            ShowAllSignal = true;
            ShowSnapCircle = true;
            ShowPlSiLabel = true;
            ShowTiles = false;
            ShowSnapLine = false;
            ShowSnapInfo = false;
            ShowRuler = true;
            ShowTrackInfo = true;
        }

        ~AEPreference()
        {
            saveXml();
        }

        public void setSnapCircle(int diam)
        {
            SnapCircle = diam;
        }

        public int getSnapCircle()
        {
            return SnapCircle;
        }

        public void setPlSiZoom(float size)
        {
            PlSiZoom = size;
        }

        public float getPlSiZoom()
        {
            return PlSiZoom;
        }

        public bool saveXml()
        {
            XmlSerializer xs = new XmlSerializer(typeof(AEPreference));
            string completeFileName = Path.Combine(AEPath, "ActivityEditor.pref.xml");

            using (StreamWriter wr = new StreamWriter(completeFileName))
            {
                xs.Serialize(wr, this);
            }
            return true;
        }

        static public AEPreference loadXml()
        {
            AEPreference p;
            try
            {
                XmlSerializer xs = new XmlSerializer(typeof(AEPreference));
                string completeFileName = Path.Combine(UserSettings.UserDataFolder, "ActivityEditor.pref.xml");

                using (StreamReader rd = new StreamReader(completeFileName))
                {
                    p = xs.Deserialize(rd) as AEPreference;
                }
            }
            catch
            {
                p = new AEPreference();
            }
            return p;
        }

        public void CompleteSettings(UserSettings settings)
        {
            this.settings = settings;
            orConfig = ORConfig.LoadConfig(UserSettings.UserDataFolder);
            AEPath = UserSettings.UserDataFolder;
        }

        public void UpdateConfig()
        {

        }

        public bool CheckPrefValidity()
        {
            if (MSTSPath == "" || RoutePaths.Count() <= 0)
                return false;
            return true;
        }

        public void UpdateORConfig()
        {
        }

        public bool RemoveGenAction(int indx)
        {
            if (ActionContainer != null)
                return ActionContainer.RemoveGenAction(indx);
            return false;
        }

        public void AddGenAction(string name)
        {
            if (ActionContainer != null)
                ActionContainer.AddGenAction(name);
        }

        public string GetComment(string name)
        {
            string comment = "Some comment";
            if (ActionContainer != null)
            {
                comment = ActionContainer.GetComment(name);
            }
            return comment;
        }

        public AuxActionRef GetAction(int indx)
        {
            if (ActionContainer != null)
                return ActionContainer.GetAction(indx);
            return null;
        }

    }
}
