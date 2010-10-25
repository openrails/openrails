/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 
/// Read the camera config file (msts)\global\camcfg.dat - Paul Gausden Dec 2009
/// This class reads the config file into a list of camera objects


using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

namespace MSTS
{
    /// <summary>
    /// Object used by ORTS.Cameras to set up views (3dviewer\camera.cs)
    /// </summary>
    public class CAMCFGFile
    {
        public CAMCFGFile(string filename)
        {
            using (STFReader f = new STFReader(filename))
            {
                while (!f.EOF)
                    switch (f.ReadItem().ToLower())
                    {
                        case "camera": Cameras.Add(new camera(f)); break;
                        case "(": f.SkipRestOfBlock(); break;
                    }
            }
        }
        public ArrayList Cameras = new ArrayList(8);
    }

    /// <summary>
    /// Individual camera object from the config file
    /// </summary>
    public class camera
    {
        public camera(STFReader f)
        {
            f.MustMatch("(");
            string token = f.ReadItem();

            while (token != ")")
            {
                switch (token.ToLower())
                {
                    case "camtype": CamType = f.ReadItemBlock(null); CamControl = f.ReadItemBlock(null); break;
                    case "cameraoffset": CameraOffset=new vector(f.ReadFloatBlock(STFReader.UNITS.Any, null),f.ReadFloatBlock(STFReader.UNITS.Any, null),f.ReadFloatBlock(STFReader.UNITS.Any, null)); break;
                    case "direction": Direction = new vector(f.ReadFloatBlock(STFReader.UNITS.Any, null), f.ReadFloatBlock(STFReader.UNITS.Any, null), f.ReadFloatBlock(STFReader.UNITS.Any, null)); break;
                    case "objectoffset": ObjectOffset = new vector(f.ReadFloatBlock(STFReader.UNITS.Any, null), f.ReadFloatBlock(STFReader.UNITS.Any, null), f.ReadFloatBlock(STFReader.UNITS.Any, null)); break;
                    case "rotationlimit": RotationLimit = new vector(f.ReadFloatBlock(STFReader.UNITS.Any, null), f.ReadFloatBlock(STFReader.UNITS.Any, null), f.ReadFloatBlock(STFReader.UNITS.Any, null)); break;
                    case "description": Description = f.ReadItemBlock(null); break;
                    case "fov": Fov = f.ReadFloatBlock(STFReader.UNITS.Any, null); break;
                    case "zclip": ZClip = f.ReadFloatBlock(STFReader.UNITS.Any, null); break;
                    case "wagonnum": WagonNum = f.ReadIntBlock(STFReader.UNITS.Any, null); break;
                    default: f.SkipBlock(); break;
                }
                token = f.ReadItem();
            }
        }

        public string CamType;
        public string CamControl;
        public vector CameraOffset = new vector(0f,0f,0f);
  	    public vector Direction =new vector( 0f,0f,0f );
	    public float Fov = 55f;
	    public float ZClip =0.1f;
	    public int WagonNum =-1;
	    public vector ObjectOffset = new vector ( 0f, 0f, 0f);
	    public vector RotationLimit = new vector ( 0f, 0f, 0f);
	    public string Description ="";

    }


}
