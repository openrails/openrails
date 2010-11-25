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
            using (STFReader stf = new STFReader(filename, false))
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("camera", ()=>{ Cameras.Add(new Camera(stf)); }),
                });
        }
        public ArrayList Cameras = new ArrayList(8);
    }

    /// <summary>
    /// Individual camera object from the config file
    /// </summary>
    public class Camera
    {
        public Camera(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("camtype", ()=>{ CamType = stf.ReadStringBlock(null); CamControl = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("cameraoffset", ()=>{ CameraOffset = stf.ReadVector3Block(STFReader.UNITS.None, CameraOffset); }),
                new STFReader.TokenProcessor("direction", ()=>{ Direction = stf.ReadVector3Block(STFReader.UNITS.None, Direction); }),
                new STFReader.TokenProcessor("objectoffset", ()=>{ ObjectOffset = stf.ReadVector3Block(STFReader.UNITS.None, ObjectOffset); }),
                new STFReader.TokenProcessor("rotationlimit", ()=>{ RotationLimit = stf.ReadVector3Block(STFReader.UNITS.None, RotationLimit); }),
                new STFReader.TokenProcessor("description", ()=>{ Description = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("fov", ()=>{ Fov = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("zclip", ()=>{ ZClip = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("wagonnum", ()=>{ WagonNum = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
            });
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
