// COPYRIGHT 2014, 2015 by the Open Rails project.
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

using System.IO;
using Orts.Formats.Msts;

namespace Orts.Formats.OR
{
    public class MSTSData
    {
        public TrackDatabaseFile TDB { get; protected set; }
        public RouteFile TRK { get; protected set; }
        public TrackSectionsFile TSectionDat { get; protected set; }
        public SignalConfigurationFile SIGCFG { get; protected set; }
        public string RoutePath { get; set; }
        public string MstsPath { get; set; }
        public AESignals Signals { get; protected set; }

        public MSTSData(string mstsPath, string Route)
        {
            MstsPath = mstsPath;
            RoutePath = Route;
            TRK = new RouteFile(MSTS.MSTSPath.GetTRKFileName(RoutePath));
            string routePath = Path.Combine(Route, TRK.Tr_RouteFile.FileName);
            TDB = new TrackDatabaseFile(RoutePath + @"\" + TRK.Tr_RouteFile.FileName + ".tdb");

            string ORfilepath = System.IO.Path.Combine(RoutePath, "OpenRails");

            if (File.Exists(ORfilepath + @"\sigcfg.dat"))
            {
                SIGCFG = new SignalConfigurationFile(ORfilepath + @"\sigcfg.dat", true);
            }
            else
            {
                SIGCFG = new SignalConfigurationFile(RoutePath + @"\sigcfg.dat", false);
            }

            if (Directory.Exists(RoutePath + @"\Openrails") && File.Exists(RoutePath + @"\Openrails\TSECTION.DAT"))
                TSectionDat = new TrackSectionsFile(RoutePath + @"\Openrails\TSECTION.DAT");
            else if (Directory.Exists(RoutePath + @"\GLOBAL") && File.Exists(RoutePath + @"\GLOBAL\TSECTION.DAT"))
                TSectionDat = new TrackSectionsFile(RoutePath + @"\GLOBAL\TSECTION.DAT");
            else
                TSectionDat = new TrackSectionsFile(MstsPath + @"\GLOBAL\TSECTION.DAT");
            if (File.Exists(RoutePath + @"\TSECTION.DAT"))
                TSectionDat.AddRouteTSectionDatFile(RoutePath + @"\TSECTION.DAT");
            Signals = new AESignals(this, SIGCFG);
        }
    }
}