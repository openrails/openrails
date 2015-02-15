// COPYRIGHT 2010, 2011, 2012, 2013 by the Open Rails project.
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

/// This module parses the sigcfg file and builds an object model based on signal details
/// 
/// Author: Stefan PAITONI
/// 
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ORTS;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using ORTS.Common;
using LibAE.Formats;

namespace ORTS.Formats
{
    public class MSTSData
    {
        public TDBFile TDB { get; protected set; }
        public TRKFile TRK { get; protected set; }
        public TSectionDatFile TSectionDat { get; protected set; }
        public SIGCFGFile SIGCFG { get; protected set; }
        public string RoutePath { get; set; }
        public string MstsPath { get; set; }
        public AESignals Signals { get; protected set; }

        public MSTSData (string mstsPath, string Route)
        {
            MstsPath = mstsPath;
            RoutePath = Route;
            TRK = new TRKFile(MSTS.MSTSPath.GetTRKFileName(RoutePath));
            string routePath = Path.Combine(Route, TRK.Tr_RouteFile.FileName);
            TDB = new TDBFile(RoutePath + @"\" + TRK.Tr_RouteFile.FileName + ".tdb");
            SIGCFG = new SIGCFGFile(RoutePath + @"\sigcfg.dat");
            if (Directory.Exists(MstsPath + @"\GLOBAL") && File.Exists(MstsPath + @"\GLOBAL\TSECTION.DAT"))
                TSectionDat = new TSectionDatFile(MstsPath + @"\GLOBAL\TSECTION.DAT");
            else
                TSectionDat = new TSectionDatFile(RoutePath + @"\GLOBAL\TSECTION.DAT");
            if (File.Exists(RoutePath + @"\TSECTION.DAT"))
                TSectionDat.AddRouteTSectionDatFile(RoutePath + @"\TSECTION.DAT");
            Signals = new AESignals (this, SIGCFG);
        }
    }
}