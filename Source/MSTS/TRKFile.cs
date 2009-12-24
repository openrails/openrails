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
            STFReader f = new STFReader(filename);
            try
            {
                string token = f.ReadToken();
                while (token != "") // EOF
                {
                    if (token == "(") f.SkipBlock();
                    else if (0 == String.Compare(token, "Tr_RouteFile", true)) Tr_RouteFile = new Tr_RouteFile(f);
                    else if (0 == String.Compare(token, "_OpenRails", true)) ORTRKData = new ORTRKData(f);
                    else f.SkipBlock();
                    token = f.ReadToken();
                }
                if (Tr_RouteFile == null) throw (new STFError(f, "Missing Tr_RouteFile"));
            }
            finally
            {
                f.Close();
                if (ORTRKData == null)
                    ORTRKData = new ORTRKData();
            }
        }
        public Tr_RouteFile Tr_RouteFile;
        public ORTRKData ORTRKData = null;
    }

    public class ORTRKData
    {
        public float MaxViewingDistance = float.MaxValue;  // disables local route override

        public ORTRKData()
        {
        }

        public ORTRKData(STFReader f)
        {
            f.VerifyStartOfBlock();
            while (!f.EndOfBlock())
            {
                string token = f.ReadToken();
                switch (token.ToLower())
                {
                    case "maxviewingdistance": MaxViewingDistance = f.ReadFloatBlock(); break;
                    default: f.SkipUnknownBlock(token); break;
                }
            }
        }
    }

    public class Tr_RouteFile
    {
        public Tr_RouteFile(STFReader f)
        {
            f.VerifyStartOfBlock();
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));
                else if (0 == String.Compare(token, "RouteID", true)) RouteID = f.ReadStringBlock();
                else if (0 == String.Compare(token, "Name", true)) Name = f.ReadStringBlock();
                else if (0 == String.Compare(token, "FileName", true)) FileName = f.ReadStringBlock();
                else if (0 == String.Compare(token, "Description", true)) Description = f.ReadStringBlock();
                else if (0 == String.Compare(token, "RouteStart", true) && RouteStart == null) RouteStart = new RouteStart(f); // take only the first - ignore any others
                else if (0 == String.Compare(token, "Environment", true)) Environment = new TRKEnvironment(f);
                else f.SkipBlock();
                token = f.ReadToken();
            }
            if (RouteID == null) throw (new STFError(f, "Missing RouteID"));
            if (Name == null) throw (new STFError(f, "Missing Name"));
            if (Description == null) throw (new STFError(f, "Missing Description"));
            if (RouteStart == null) throw (new STFError(f, "Missing RouteStart"));
        }
        public string RouteID;  // ie JAPAN1  - used for TRK file and route folder name
        public string FileName; // ie OdakyuSE - used for MKR,RDB,REF,RIT,TDB,TIT
        public string Name;
        public string Description;
        public RouteStart RouteStart;
        public TRKEnvironment Environment;
    }


    public class RouteStart
    {
        public RouteStart(STFReader f)
        {
            f.VerifyStartOfBlock();
            WX = f.ReadDouble();   // tilex
            WZ = f.ReadDouble();   // tilez
            X = f.ReadDouble();
            Z = f.ReadDouble();
            while (f.ReadToken() != ")") ; // discard extra parameters - users frequently describe location here
        }
        public double WX, WZ, X, Z;
    }

    public class TRKEnvironment
    {
        string[] ENVFileNames = new string[12];

        public TRKEnvironment(STFReader f)
        {
            f.VerifyStartOfBlock();
            for( int i = 0; i < 12; ++i )
            {
                f.ReadToken();
                f.VerifyStartOfBlock();
                ENVFileNames[i] = f.ReadToken();
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
