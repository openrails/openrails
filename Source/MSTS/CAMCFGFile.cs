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
            STFReader f = new STFReader(filename);
            try
            {
                string token = f.ReadToken();
                while (!f.EndOfBlock())
                {
                    if (token == ")") throw (new STFError(f, "Unexpected )"));
                    else if (token == "(") f.SkipBlock();
                        
                    else if (0 == String.Compare(token, "camera", true))
                    {
                        // add this camera to the list
                        cam = new camera(f);
                        Cameras.Add(cam);
                    }
                    else f.SkipBlock();
                    token = f.ReadToken();
                }
            }
            finally
            {
                f.Close();
            }
        }
        private camera cam;
        public ArrayList Cameras = new ArrayList(8);
    }

    /// <summary>
    /// Individual camera object from the config file
    /// </summary>
    public class camera
    {
        public camera(STFReader f)
        {
            f.VerifyStartOfBlock();
            string token = f.ReadToken();

            while (token != ")")
            {
                switch (token.ToLower())
                {
                    case "camtype": CamType=f.ReadStringBlock(); CamControl=f.ReadStringBlock(); break;
                    case "cameraoffset": CameraOffset=new vector(f.ReadFloatBlock(),f.ReadFloatBlock(),f.ReadFloatBlock()); break;
                    case "direction": Direction = new vector(f.ReadFloatBlock(), f.ReadFloatBlock(), f.ReadFloatBlock()); break;
                    case "objectoffset": ObjectOffset = new vector(f.ReadFloatBlock(), f.ReadFloatBlock(), f.ReadFloatBlock()); break;
                    case "rotationlimit": RotationLimit = new vector(f.ReadFloatBlock(), f.ReadFloatBlock(), f.ReadFloatBlock()); break;
                    case "description": Description = f.ReadStringBlock(); break;
                    case "fov": Fov = f.ReadFloatBlock(); break;
                    case "zclip": ZClip = f.ReadFloatBlock(); break;
                    case "wagonnum": WagonNum = f.ReadIntBlock(); break;
                    default: f.SkipBlock(); break;
                }
                token = f.ReadToken();
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
