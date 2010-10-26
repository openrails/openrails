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
                using (STFReader f = new STFReader(filename))
                {
                    while (!f.EOF)
                        switch (f.ReadItem().ToLower())
                        {
                            case "tr_routefile": Tr_RouteFile = new Tr_RouteFile(f); break;
                            case "_OpenRails": ORTRKData = new ORTRKData(f); break;
                            case "(": f.SkipRestOfBlock(); break;
                        }
                    if (Tr_RouteFile == null) throw new STFException(f, "Missing Tr_RouteFile");
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

        public ORTRKData(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "maxviewingdistance": MaxViewingDistance = f.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                    case "(": f.SkipRestOfBlock(); break;
                }
        }
    }

    public class Tr_RouteFile
    {
        public Tr_RouteFile(STFReader f)
        {
            f.MustMatch("(");
            while(!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "routeid": RouteID = f.ReadItemBlock(null); break;
                    case "name": Name = f.ReadItemBlock(null); break;
                    case "filename": FileName = f.ReadItemBlock(null); break;
                    case "description": Description = f.ReadItemBlock(null); break;
                    case "maxlinevoltage": MaxLineVoltage = f.ReadDoubleBlock(STFReader.UNITS.None, null); break;
                    case "routestart": if (RouteStart == null) RouteStart = new RouteStart(f); break; // take only the first - ignore any others
                    case "environment": Environment = new TRKEnvironment(f); break;
                    case "milepostunitskilometers": MilepostUnitsMetric = true; break;
                    case "(": f.SkipRestOfBlock(); break;
                }
            if (RouteID == null) throw new STFException(f, "Missing RouteID");
            if (Name == null) throw new STFException(f, "Missing Name");
            if (Description == null) throw new STFException(f, "Missing Description");
            if (RouteStart == null) throw new STFException(f, "Missing RouteStart");
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
        public RouteStart(STFReader f)
        {
            f.MustMatch("(");
            WX = f.ReadDouble(STFReader.UNITS.None, null);   // tilex
            WZ = f.ReadDouble(STFReader.UNITS.None, null);   // tilez
            X = f.ReadDouble(STFReader.UNITS.None, null);
            Z = f.ReadDouble(STFReader.UNITS.None, null);
            while (f.ReadItem() != ")") ; // discard extra parameters - users frequently describe location here
        }
        public double WX, WZ, X, Z;
    }

    public class TRKEnvironment
    {
        string[] ENVFileNames = new string[12];

        public TRKEnvironment(STFReader f)
        {
            f.MustMatch("(");
            for( int i = 0; i < 12; ++i )
            {
                f.ReadItem();
                f.MustMatch("(");
                ENVFileNames[i] = f.ReadItem();
                f.MustMatch(")");
            }
            f.MustMatch(")");
        }

        public string ENVFileName( SeasonType seasonType, WeatherType weatherType )
        {
            int index = (int)seasonType * 3 + (int)weatherType;
            return ENVFileNames[index];
        }
    }

}
