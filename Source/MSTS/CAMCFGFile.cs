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
using Microsoft.Xna.Framework;

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
                    case "cameraoffset": CameraOffset = f.ReadVector3Block(STFReader.UNITS.None, CameraOffset); break;
                    case "direction": Direction = f.ReadVector3Block(STFReader.UNITS.None, Direction); break;
                    case "objectoffset": ObjectOffset = f.ReadVector3Block(STFReader.UNITS.None, ObjectOffset); break;
                    case "rotationlimit": RotationLimit = f.ReadVector3Block(STFReader.UNITS.None, RotationLimit); break;
                    case "description": Description = f.ReadItemBlock(null); break;
                    case "fov": Fov = f.ReadFloatBlock(STFReader.UNITS.None, null); break;
                    case "zclip": ZClip = f.ReadFloatBlock(STFReader.UNITS.None, null); break;
                    case "wagonnum": WagonNum = f.ReadIntBlock(STFReader.UNITS.None, null); break;
                    default: f.SkipBlock(); break;
                }
                token = f.ReadItem();
            }
        }

        public string CamType;
        public string CamControl;
        public Vector3 CameraOffset = new Vector3();
        public Vector3 Direction = new Vector3();
	    public float Fov = 55f;
	    public float ZClip =0.1f;
	    public int WagonNum =-1;
        public Vector3 ObjectOffset = new Vector3();
        public Vector3 RotationLimit = new Vector3();
	    public string Description ="";

    }


}
