// COPYRIGHT 2009, 2010 by the Open Rails project.
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

// Read the camera config file (msts)\global\camcfg.dat - Paul Gausden Dec 2009
// This class reads the config file into a list of camera objects


using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Orts.Parsers.Msts;

namespace Orts.Formats.Msts
{
    /// <summary>
    /// Object used by ORTS.Cameras to set up views (3dviewer\camera.cs)
    /// </summary>
    public class CameraConfigurationFile
    {
        public List<Camera> Cameras = new List<Camera>();

        public CameraConfigurationFile(string filename)
        {
            using (STFReader stf = new STFReader(filename, false))
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("camera", ()=>{ Cameras.Add(new Camera(stf)); }),
                });
        }
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
                new STFReader.TokenProcessor("wagonnum", ()=>{ WagonNum = stf.ReadIntBlock(null); }),
            });
        }

        public string CamType;
        public string CamControl;
        public Vector3 CameraOffset = new Vector3();
        public Vector3 Direction = new Vector3();
        public float Fov = 55f;
        public float ZClip = 0.1f;
        public int WagonNum = -1;
        public Vector3 ObjectOffset = new Vector3();
        public Vector3 RotationLimit = new Vector3();
        public string Description = "";

    }


}
