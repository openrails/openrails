/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 
/// WARNING - This file needs to be rewritten to use STF class 


using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace MSTS
{
    /// <summary>
    /// Utility class to avoid loading multiple copies of the same file.
    /// </summary>
    public class SharedWAGFileManager
    {
        private static Dictionary<string, WAGFile> SharedWAGFiles = new Dictionary<string, WAGFile>();

        public static WAGFile Get(string path)
        {
            if (!SharedWAGFiles.ContainsKey(path))
            {
                WAGFile wagFile = new WAGFile(path);
                SharedWAGFiles.Add(path, wagFile);
                return wagFile;
            }
            else
            {
                return SharedWAGFiles[path];
            }
        }
    }

	// TODO - this is an incomplete parse of the wag file.
	public class WAGFile
	{
        public string Folder;  // Folder name only, not full path
        public bool IsEngine { get { return Engine != null; } }
        public bool HasInsideView { get { return Wagon.Inside != null; } }

        public WagonClass Wagon;
        public EngineClass Engine = null;


        public WAGFile(string filenamewithpath)
		{
			WagFile( filenamewithpath );
		}

		public void WagFile( string filenamewithpath )
		{
            Folder = Path.GetFileName( Path.GetDirectoryName(filenamewithpath) );

            STFReader f = new STFReader(filenamewithpath);
            while (!f.EndOfBlock())
            {
                string token = f.ReadToken();
                switch (token.ToLower())
                {
                    case "wagon": Wagon = new WagonClass(f); break;
                    case "engine": Engine = new EngineClass(f); break;
                    default: f.SkipUnknownBlock(token); break;
                }
            }
            f.Close();
		}

        public class WagonClass
        {
            public string Label;   // Appears at the top of the file right after the wagon statement, use in consists
            public string Name;    // users free form description of the car
            public string WagonShape;
            public string FreightAnim;
            public float Length = 12.0f; // meters - default for improperly parsed files
            public float WheelRadiusM = 0.42f; // meters 
            public InsideClass Inside = null;
            public float MassKG = 30e3f;
            public float MaxBrakeForceN;
            public string Sound = null;

            public WagonClass(STFReader f)
            {
                f.VerifyStartOfBlock();

                Label = f.ReadToken();

                while (!f.EndOfBlock())
                {
                    string token = f.ReadToken();
                    switch (token.ToLower())
                    {
                        case "wagonshape": WagonShape = f.ReadStringBlock(); break;
                        case "freightanim": f.VerifyStartOfBlock(); FreightAnim = f.ReadToken(); f.SkipRestOfBlock(); break; // TODO complete parse
                        case "size": f.VerifyStartOfBlock(); f.ReadFloat(); f.ReadFloat(); Length = f.ReadFloat(); f.VerifyEndOfBlock();  break;
                        case "mass": MassKG = f.ReadFloatBlock(); break;
                        case "wheelradius": WheelRadiusM = f.ReadFloatBlock(); break;
                        case "name": Name = f.ReadStringBlock(); break;
                        case "sound": Sound = f.ReadStringBlock(); break;
                        case "maxbrakeforce": MaxBrakeForceN = f.ReadFloatBlock(); break;
                        case "inside": Inside = new InsideClass( f ); break;
                        default: f.SkipBlock(); break; //TODO complete parse and replace with f.SkipUnknownToken(token); break;
                    }
                }
            }

            public class InsideClass
            {
                public string Sound = null;
                public string PassengerCabinFile = null;
                public Vector3 PassengerCabinHeadPos;

                public InsideClass(STFReader f)
                {
                    f.VerifyStartOfBlock();
                    while (!f.EndOfBlock())
                    {
                        string token = f.ReadToken();
                        switch (token.ToLower())
                        {
                            case "sound": Sound = f.ReadStringBlock(); break;
                            case "passengercabinfile": PassengerCabinFile = f.ReadStringBlock(); break;
                            case "passengercabinheadpos": PassengerCabinHeadPos = f.ReadVector3Block(); break;
                            default: f.SkipBlock(); break; // TODO complete parse and replace with SkipUnknownToken(..
                        }
                    }
                }
            }

        } // class WAGFile.Wagon

        public class EngineClass
        {
            public string Sound = null;
            public string Type = null;
            public float DriverWheelRadiusM = 1.0f; // meters TODO, read from eng section of file
            public string CabView = null;
            public string Label = null;

            public EngineClass(STFReader f)
            {
                f.VerifyStartOfBlock();
                Label = f.ReadToken();
                while (!f.EndOfBlock())
                {
                    string token = f.ReadToken();
                    switch (token.ToLower())
                    {
                        case "sound": Sound = f.ReadStringBlock(); break;
                        case "cabview": CabView = f.ReadStringBlock(); break;
                        case "type": Type = f.ReadStringBlock(); break;
                        default: f.SkipBlock(); break; // TODO complete parse and replace with f.SkipUnknownBlock ...
                    }
                }
            }
        } // class WAGFile.Engine

    }// class WAGFile


} // namespace MSTS

        