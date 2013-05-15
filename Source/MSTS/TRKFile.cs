// COPYRIGHT 2009, 2010, 2011, 2013 by the Open Rails project.
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSTS
{
    public class TRKFile
    {
        public TRKFile(string filename)
        {
            try
            {
                using (STFReader stf = new STFReader(filename, false))
                {
                    stf.ParseFile(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("tr_routefile", ()=>{ Tr_RouteFile = new Tr_RouteFile(stf); }),
                        new STFReader.TokenProcessor("_OpenRails", ()=>{ ORTRKData = new ORTRKData(stf); }),
                    });
                    //TODO This should be changed to STFException.TraceError() with defaults values created
                    if (Tr_RouteFile == null) throw new STFException(stf, "Missing Tr_RouteFile");
                }
            }
            finally
            {
                if (ORTRKData == null)
                    ORTRKData = new ORTRKData();
            }
        }
        public Tr_RouteFile Tr_RouteFile;
        public ORTRKData ORTRKData;
    }

    public class ORTRKData
    {
        public float MaxViewingDistance = float.MaxValue;  // disables local route override

        public ORTRKData()
        {
        }

        public ORTRKData(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("maxviewingdistance", ()=>{ MaxViewingDistance = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
            });
        }
    }

    public class Tr_RouteFile
    {
        public Tr_RouteFile(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("routeid", ()=>{ RouteID = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("filename", ()=>{ FileName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("description", ()=>{ Description = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("maxlinevoltage", ()=>{ MaxLineVoltage = stf.ReadDoubleBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("routestart", ()=>{ if (RouteStart == null) RouteStart = new RouteStart(stf); }),
                new STFReader.TokenProcessor("environment", ()=>{ Environment = new TRKEnvironment(stf); }),
                new STFReader.TokenProcessor("milepostunitskilometers", ()=>{ MilepostUnitsMetric = true; }),
				new STFReader.TokenProcessor("electrified", ()=>{ Electrified = stf.ReadBoolBlock(false); }),
                new STFReader.TokenProcessor("overheadwireheight", ()=>{ OverheadWireHeight = stf.ReadDoubleBlock(STFReader.UNITS.None, 6.0);}),
 				new STFReader.TokenProcessor("speedlimit", ()=>{ SpeedLimit = stf.ReadDoubleBlock(STFReader.UNITS.None, 500.0); }),
                new STFReader.TokenProcessor("defaultcrossingsms", ()=>{ DefaultCrossingSMS = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("defaultcoaltowersms", ()=>{ DefaultCoalTowerSMS = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("defaultdieseltowersms", ()=>{ DefaultDieselTowerSMS = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("defaultwatertowersms", ()=>{ DefaultWaterTowerSMS = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("defaultsignalsms", ()=>{ DefaultSignalSMS = stf.ReadStringBlock(null); }),
           });
            //TODO This should be changed to STFException.TraceError() with defaults values created
            if (RouteID == null) throw new STFException(stf, "Missing RouteID");
            if (Name == null) throw new STFException(stf, "Missing Name");
            if (Description == null) throw new STFException(stf, "Missing Description");
            if (RouteStart == null) throw new STFException(stf, "Missing RouteStart");
        }

        public string RouteID;  // ie JAPAN1  - used for TRK file and route folder name
        public string FileName; // ie OdakyuSE - used for MKR,RDB,REF,RIT,TDB,TIT
        public string Name;
        public string Description;
        public RouteStart RouteStart;
        public TRKEnvironment Environment;
		public bool MilepostUnitsMetric = false;
        public double MaxLineVoltage = 0;
		public bool Electrified = true;
		public double OverheadWireHeight = 6.0;
		public double SpeedLimit = 500.0f; //global speed limit m/s.
        public string DefaultCrossingSMS;
        public string DefaultCoalTowerSMS;
        public string DefaultDieselTowerSMS;
        public string DefaultWaterTowerSMS;
        public string DefaultSignalSMS;
    }


    public class RouteStart
    {
        public RouteStart(STFReader stf)
        {
            stf.MustMatch("(");
            WX = stf.ReadDouble(STFReader.UNITS.None, null);   // tilex
            WZ = stf.ReadDouble(STFReader.UNITS.None, null);   // tilez
            X = stf.ReadDouble(STFReader.UNITS.None, null);
            Z = stf.ReadDouble(STFReader.UNITS.None, null);
            stf.SkipRestOfBlock();
        }
        public double WX, WZ, X, Z;
    }

    public class TRKEnvironment
    {
        string[] ENVFileNames = new string[12];

        public TRKEnvironment(STFReader stf)
        {
            stf.MustMatch("(");
            for( int i = 0; i < 12; ++i )
            {
                string s = stf.ReadString();
                ENVFileNames[i] = stf.ReadStringBlock(null);
            }
            stf.SkipRestOfBlock();
        }

        public string ENVFileName( SeasonType seasonType, WeatherType weatherType )
        {
            int index = (int)seasonType * 3 + (int)weatherType;
            return ENVFileNames[index];
        }
    }

}
