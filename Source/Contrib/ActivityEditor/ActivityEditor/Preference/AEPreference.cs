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
                if (routePaths != null && routePaths.Count > 0 && MSTSPath.Length > 0 && AEPath.Length > 0)
                    return true;
                else
                    return false;
            }
        }
        [XmlIgnore]
        public UserSettings settings;

        public List<string> routePaths { get; set; }
        public string MSTSPath { get; set; }
        public string AEPath { get; set; }
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
            routePaths = new List<string>();
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
            using (StreamWriter wr = new StreamWriter("ActivityEditor.pref.xml"))
            {
                xs.Serialize(wr, this);
            }
            return true;
        }

        static public AEPreference loadXml()
        {
            AEPreference p;
            XmlSerializer xs = new XmlSerializer(typeof(AEPreference));
            try
            {
                using (StreamReader rd = new StreamReader("ActivityEditor.pref.xml"))
                {
                    p = xs.Deserialize(rd) as AEPreference;
                }
            }
            catch (IOException e)
            {
                p = new AEPreference();
            }
            return p;
        }

        public void setSettings(UserSettings settings)
        {
            this.settings = settings;
        }

        public bool CheckPrefValidity()
        {
            if (MSTSPath == "" || AEPath == "" || routePaths.Count() <= 0)
                return false;
            return true;
        }
    }
}
