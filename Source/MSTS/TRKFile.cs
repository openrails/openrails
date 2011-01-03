/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

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
