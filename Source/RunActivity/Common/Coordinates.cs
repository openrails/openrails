/*
 * 
 * COORDINATE SYSTEMS - XNA uses a different coordinate system than MSTS.  In XNA, +ve Z is toward the camera, 
 * whereas in MSTS it is the opposite.  As a result you will see the sign of all Z coordinates gets negated
 * and matrices are adjusted as they are loaded into XNA.  In addition the winding order of triangles is reversed in XNA.
 * Generally - X,Y,Z coordinates, vectors, quaternions, and angles will be expressed using MSTS coordinates 
 * unless otherwise noted with the prefix XNA.  Matrix's are usually constructed using XNA coordinates so they can be 
 * used directly in XNA draw routines.  So most matrix's will have XNA prepended to their name.
 * 
 * WorldCoordinates
 * X increases to the east
 * Y increases up
 * Z increases to the north
 * AX increases tilting down
 * AY increases turning to the right
 * 
 * LEXICON
 * Location - the x,y,z point where the center of the object is located - usually a Vector3
 * Pose - the orientation of an object in 3D, ie tilt, rotation - usually an XNAMatrix
 * Position - combines pose and location
 * WorldLocation - adds tile coordinates to a Location
 * WorldPosition - adds tile coordinates to a Position
 */
/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;


namespace ORTS
{
    /// <summary>
    /// Represents the position and orientation of an object
    /// including what tile, and the matrix within the tile
    /// </summary>
    public class WorldPosition
    {
        public int TileX;
        public int TileZ;
        public Matrix XNAMatrix = Matrix.Identity;   // relative to center of tile

        public WorldPosition()
        {
        }

        public WorldPosition(WorldPosition copy) // Copy constructor
        {
            TileX = copy.TileX;
            TileZ = copy.TileZ;
            XNAMatrix = copy.XNAMatrix;
        }

        public WorldPosition(WorldLocation copy)
        {
            TileX = copy.TileX;
            TileZ = copy.TileZ;
            Location = copy.Location;
        }

        public WorldLocation WorldLocation   // provided in MSTS coordinates
        {
            get
            {
                WorldLocation worldLocation = new WorldLocation();
                worldLocation.TileX = TileX;
                worldLocation.TileZ = TileZ;
                worldLocation.Location = XNAMatrix.Translation;
                worldLocation.Location.Z *= -1;  // convert to MSTS coordinates
                return worldLocation;
            }
        }

        public Vector3 Location
        {
            get
            {
                Vector3 location = XNAMatrix.Translation;
                location.Z *= -1;  // convert to MSTS coordinates
                return location;
            }
            set
            {
                value.Z *= -1;
                XNAMatrix.Translation = value;
            }
        }

        /// <summary>
        /// Ensure tile coordinates are within tile boundaries
        /// </summary>
        public void Normalize()
        {
            var TileLocation = XNAMatrix.Translation;
            while (TileLocation.X > 1024) { TileLocation.X -= 2048; TileX++; }
            while (TileLocation.X < -1024) { TileLocation.X += 2048; TileX--; }
            while (TileLocation.Z > 1024) { TileLocation.Z -= 2048; TileZ++; }
            while (TileLocation.Z < -1024) { TileLocation.Z += 2048; TileZ--; }
            XNAMatrix.Translation = TileLocation;
        }

        public void NormalizeTo(int tileX, int tileZ)
        {
            Vector3 TileLocation = XNAMatrix.Translation;
            while (TileX < tileX) { TileLocation.X -= 2048; TileX++; }
            while (TileX > tileX) { TileLocation.X += 2048; TileX--; }
            while (TileZ < tileZ) { TileLocation.Z -= 2048; TileZ++; }
            while (TileZ > tileZ) { TileLocation.Z += 2048; TileZ--; }
            XNAMatrix.Translation = TileLocation;
        }

        public override string ToString()
        {
            return WorldLocation.ToString();
        }
    }

    public class WorldLocation
    {
        public int TileX;
        public int TileZ;
        public Vector3 Location = new Vector3();  // relative to center of tile in MSTS coordinates

        public WorldLocation()
        {
        }

        public WorldLocation(WorldLocation worldLocation)
        {
            TileX = worldLocation.TileX;
            TileZ = worldLocation.TileZ;
            Location = worldLocation.Location;
        }

        public WorldLocation(int tileX, int tileZ, float x, float y, float z)
        {
            TileX = tileX;
            TileZ = tileZ;
            Location.X = x;
            Location.Y = y;
            Location.Z = z;
        }

        public WorldLocation(int tileX, int tileZ, Vector3 location)
        {
            TileX = tileX;
            TileZ = tileZ;
            Location = location;
        }

        /// <summary>
        /// Ensure tile coordinates are within tile boundaries
        /// </summary>
        public void Normalize()
        {
            while (Location.X > 1024) { Location.X -= 2048; TileX++; }
            while (Location.X < -1024) { Location.X += 2048; TileX--; }
            while (Location.Z > 1024) { Location.Z -= 2048; TileZ++; }
            while (Location.Z < -1024) { Location.Z += 2048; TileZ--; }
        }

        public void NormalizeTo(int tileX, int tileZ)
        {
            while (TileX < tileX) { Location.X -= 2048; TileX++; }
            while (TileX > tileX) { Location.X += 2048; TileX--; }
            while (TileZ < tileZ) { Location.Z -= 2048; TileZ++; }
            while (TileZ > tileZ) { Location.Z += 2048; TileZ--; }
        }

        public static bool Within(WorldLocation location1, WorldLocation location2, float distance)
        {
            return GetDistanceSquared(location1, location2) < distance * distance;
        }

        public static float GetDistanceSquared(WorldLocation location1, WorldLocation location2)
        {
            var dx = location1.Location.X - location2.Location.X;
            var dy = location1.Location.Y - location2.Location.Y;
            var dz = location1.Location.Z - location2.Location.Z;
            dx += 2048 * (location1.TileX - location2.TileX);
            dz += 2048 * (location1.TileZ - location2.TileZ);
            return dx * dx + dy * dy + dz * dz;
        }

        public static Vector3 GetDistance(WorldLocation location1, WorldLocation location2)
        {
            return new Vector3(location2.Location.X - location1.Location.X + (location2.TileX - location1.TileX) * 2048, location2.Location.Y - location1.Location.Y, location2.Location.Z - location1.Location.Z + (location2.TileZ - location1.TileZ) * 2048);
        }

        public static Vector2 GetDistance2D(WorldLocation location1, WorldLocation location2)
        {
            return new Vector2(location2.Location.X - location1.Location.X + (location2.TileX - location1.TileX) * 2048, location2.Location.Z - location1.Location.Z + (location2.TileZ - location1.TileZ) * 2048);
        }

        public override string ToString()
        {
            return String.Format("{{TileX:{0} TileZ:{1} X:{2} Y:{3} Z:{4}}}", TileX, TileZ, Location.X, Location.Y, Location.Z);
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write(TileX);
            outf.Write(TileZ);
            outf.Write(Location.X);
            outf.Write(Location.Y);
            outf.Write(Location.Z);
        }

        public void Restore(BinaryReader inf)
        {
            TileX = inf.ReadInt32();
            TileZ = inf.ReadInt32();
            Location.X = inf.ReadSingle();
            Location.Y = inf.ReadSingle();
            Location.Z = inf.ReadSingle();
        }
    }
}
