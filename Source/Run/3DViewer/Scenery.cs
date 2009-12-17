/* SCENERY
 * 
 * Scenery objects are specified in WFiles located in the WORLD folder of the route.
 * Each WFile describes scenery for a 2048 meter square region of the route.
 * This assembly is responsible for loading and unloading the WFiles as 
 * the camera moves over the route.  
 * 
 * Loaded WFiles are each represented by an instance of the WorldFile class. 
 * 
 * A SceneryDrawer object is created by the Viewer.  Each time SceneryDrawer.Update is 
 * called, it disposes of WorldFile's that have gone out of range, and create's new 
 * WorldFile objects for WFiles that have come into range.
 * 
 * Currently the SceneryDrawer.Update is called 10 times a second from a background 
 * thread in the Viewer class.
 * 
 * SceneryDrawer loads the WFile in which the viewer is located, and the 8 WFiles 
 * surrounding the viewer.
 * 
 * When a WorldFile object is created, it creates StaticShape objects for each scenery
 * item.  The StaticShape objects add themselves to the Viewer's content list, sharing
 * mesh files and textures whereever possible.
 * 
 */
/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using MSTS;
using System.Threading;

namespace ORTS
{
    /// <summary>
    /// Represents a loaded WFile.
    /// </summary>
    public class WorldFile: IDisposable
    {
        public int TileX, TileZ;

        /// <summary>
        /// Open the specified WFile and load all the scenery objects into the viewer.
        /// If the file doesn't exist, then return an empty WorldFile object.
        /// </summary>
        public WorldFile( Viewer viewer, int tileX, int tileZ )
        {
            TileX = tileX;
            TileZ = tileZ;

            // determine file path to the WFile at the specified tile coordinates
            string WFileName = WorldFileNameFromTileCoordinates(tileX, tileZ);
            string WFilePath = viewer.Simulator.RoutePath + @"\WORLD\" + WFileName;

            // if there isn't a file, then return with an empty WorldFile object
            if (!File.Exists(WFilePath))
                return;

            // read the world file 
            WFile WFile = new WFile(WFilePath);

            // create all the individual scenery objects specified in the WFile
            foreach (WorldObject worldObject in WFile.Tr_Worldfile)
            {
                if (worldObject.StaticDetailLevel > viewer.WorldObjectDensity)
                    continue;

                // determine the full file path to the shape file for this scenery object 
                string shapeFilePath;
                if (worldObject.GetType() == typeof(MSTS.TrackObj))
                    shapeFilePath = viewer.Simulator.BasePath + @"\global\shapes\" + worldObject.FileName;
                else
                    shapeFilePath = viewer.Simulator.RoutePath + @"\shapes\" + worldObject.FileName;

                // get the position of the scenery object into ORTS coordinate space
                WorldPosition worldMatrix;
                if( worldObject.QDirection == null )
                    worldMatrix = WorldPositionFromMSTSLocation(WFile.TileX,WFile.TileZ, worldObject.Position, worldObject.Matrix3x3);
                else
                    worldMatrix = WorldPositionFromMSTSLocation(WFile.TileX,WFile.TileZ,worldObject.Position, worldObject.QDirection);


                if (worldObject.GetType() == typeof(MSTS.TrackObj))
                {
                    TrackObj trackObj = (TrackObj)worldObject;
                    if (trackObj.JNodePosn != null)
                    {
                        // switch tracks need a link to the simulator engine so they can animate the points
                        TrJunctionNode TRJ = viewer.Simulator.TDB.GetTrJunctionNode( TileX, TileZ, (int)trackObj.UID );
                        viewer.Components.Add(new SwitchTrackShape(viewer, shapeFilePath, worldMatrix, TRJ ));

                    }
                    else // its some type of track other than a switch track
                    {
                        viewer.Components.Add(new StaticShape(viewer, shapeFilePath, worldMatrix));
                    }
                }
                else // its some other type of oject - not a track object
                {
                    viewer.Components.Add(new StaticShape(viewer, shapeFilePath, worldMatrix));
                }
            }
        } //class WorldFile

        /// <summary>
        /// MSTS WFiles represent some location with a position, quaternion and tile coordinates
        /// This converts it to the ORTS WorldPosition representation
        /// </summary>
        private WorldPosition WorldPositionFromMSTSLocation(int tileX, int tileZ, STFPositionItem MSTSPosition, STFQDirectionItem MSTSQuaternion )
        {
            Quaternion XNAQuaternion = new Quaternion((float)MSTSQuaternion.A, (float)MSTSQuaternion.B, -(float)MSTSQuaternion.C, (float)MSTSQuaternion.D);
            Vector3 XNAPosition = new Vector3((float)MSTSPosition.X, (float)MSTSPosition.Y, -(float)MSTSPosition.Z);
            Matrix XNAMatrix = Matrix.CreateFromQuaternion(XNAQuaternion);
            XNAMatrix *= Matrix.CreateTranslation(XNAPosition);

            WorldPosition worldMatrix = new WorldPosition();
            worldMatrix.TileX = tileX;
            worldMatrix.TileZ = tileZ;
            worldMatrix.XNAMatrix = XNAMatrix;

            return worldMatrix;
        }

        /// <summary>
        /// MSTS WFiles represent some location with a position, 3x3 matrix and tile coordinates
        /// This converts it to the ORTS WorldPosition representation
        /// </summary>
        private WorldPosition WorldPositionFromMSTSLocation(int tileX, int tileZ, STFPositionItem MSTSPosition, Matrix3x3 MSTSMatrix)
        {

            Vector3 XNAPosition = new Vector3((float)MSTSPosition.X, (float)MSTSPosition.Y, -(float)MSTSPosition.Z);
            Matrix XNAMatrix = Matrix.Identity;
            XNAMatrix.M11 = MSTSMatrix.AX;
            XNAMatrix.M12 = MSTSMatrix.AY;
            XNAMatrix.M13 = -MSTSMatrix.AZ;
            XNAMatrix.M14 = 0;
            XNAMatrix.M21 = MSTSMatrix.BX;
            XNAMatrix.M22 = MSTSMatrix.BY;
            XNAMatrix.M23 = -MSTSMatrix.BZ;
            XNAMatrix.M24 = 0;
            XNAMatrix.M31 = -MSTSMatrix.CX;
            XNAMatrix.M32 = -MSTSMatrix.CY;
            XNAMatrix.M33 = MSTSMatrix.CZ;
            XNAMatrix.M34 = 0;
            XNAMatrix.M41 = 0;
            XNAMatrix.M42 = 0;
            XNAMatrix.M43 = 0;
            XNAMatrix.M44 = 1;
            XNAMatrix *= Matrix.CreateTranslation(XNAPosition);

            WorldPosition worldMatrix = new WorldPosition();
            worldMatrix.TileX = tileX;
            worldMatrix.TileZ = tileZ;
            worldMatrix.XNAMatrix = XNAMatrix;

            return worldMatrix;
        }

        /// <summary>
        /// Unload the scenery objects related to this WFile.
        /// </summary>
        public void Dispose()
        {
            // TODO, reduce reference count for each object used on this WFile and remove the object when it is unused
        }

        /// <summary>
        /// Build a w filename from tile X and Z coordinates.
        /// Returns a string eg "w-011283+014482.w"
        /// </summary>
        private string WorldFileNameFromTileCoordinates(int tileX, int tileZ)
        {
            string filename = "w" + FormatTileCoordinate(tileX) + FormatTileCoordinate(tileZ) + ".w";
            return filename;
        }

        /// <summary>
        /// For building a filename from tile X and Z coordinates.
        /// Returns the string representation of a coordinate
        /// eg "+014482"
        /// </summary>
        private string FormatTileCoordinate(int tileCoord)
        {
            string sign = "+";
            if (tileCoord < 0)
            {
                sign = "-";
                tileCoord *= -1;
            }
            return sign + tileCoord.ToString("000000");
        }


    } // class WorldFile


    /// <summary>
    /// Handles loading and unloading of WFiles as the viewer moves across the route.
    /// </summary>
    /// Maintains an array of the loaded WorldFiles.  As the camera moves, Update
    /// scans the array, removing WorldFiles that are out of range, and creating new
    /// WorldFile objects for WFiles that come into range.
    public class SceneryDrawer
    {
        private Viewer Viewer;  // the viewer that we are tracking
        private int viewerTileX, viewerTileZ;  // the location of the viewer when the current set of wFiles was loaded
        private WorldFile[] WorldFiles = new WorldFile[9];  // surrounding wFiles, not in any particular order, null when empty

        /// <summary>
        /// Scenery objects will be loaded into this viewer.
        /// </summary>
        public SceneryDrawer(Viewer viewer)
        {
            Viewer = viewer;

            // initialize 
            for (int i = 0; i < WorldFiles.Length; ++i)
                WorldFiles[i] = null;
        }

        /// <summary>
        /// Called from the separate Loader thread in Viewer
        /// Loads and unloads wFiles based on Viewer location.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            // TODO, reading camera's location should be locked to ensure its atomic

            if (viewerTileX != Viewer.Camera.TileX || viewerTileZ != Viewer.Camera.TileZ)   // skip this if the camera hasn't moved
            {
                viewerTileX = Viewer.Camera.TileX;
                viewerTileZ = Viewer.Camera.TileZ;

                // scan the wFiles array, removing any out of range
                for (int i = 0; i < WorldFiles.Length; ++i)
                {
                    WorldFile tile = WorldFiles[i];
                    if (tile != null)
                    {
                        // check if the wFile is in range ( ie viewer wFile, or surrounding wFile )
                        if (Math.Abs(tile.TileX - viewerTileX) > 1
                          || Math.Abs(tile.TileZ - viewerTileZ) > 1)
                        {
                            // if not, unload the wFile
                            Console.Write("w");
                            WorldFiles[i].Dispose();
                            WorldFiles[i] = null;
                        }
                    }
                }

                // add in wFiles in range
                LoadAt(viewerTileX - 1, viewerTileZ + 1);
                LoadAt(viewerTileX, viewerTileZ + 1);
                LoadAt(viewerTileX + 1, viewerTileZ + 1);
                LoadAt(viewerTileX - 1, viewerTileZ);
                LoadAt(viewerTileX, viewerTileZ);
                LoadAt(viewerTileX + 1, viewerTileZ);
                LoadAt(viewerTileX - 1, viewerTileZ - 1);
                LoadAt(viewerTileX, viewerTileZ - 1);
                LoadAt(viewerTileX + 1, viewerTileZ - 1);
            }

        }

        /// <summary>
        /// If the specified wFile isn't already loaded, then
        /// load it into any available location in the 
        /// WorldFiles array.
        /// </summary>
        private void LoadAt(int tileX, int tileZ)
        {
            // return if this wFile is already loaded
            foreach (WorldFile tile in WorldFiles)   // check every wFile
                if (tile != null)
                    if (tile.TileX == tileX && tile.TileZ == tileZ)  // return if its the one we want
                        return;

            // find an available spot in the WorldFiles array
            for (int i = 0; i < WorldFiles.Length; ++i)
                if (WorldFiles[i] == null)  // we found one
                {
                    Console.Write("W");
                    WorldFiles[i] = new WorldFile( Viewer, tileX, tileZ);
                    return;
                }

            // otherwise we didn't find an available spot - this shouldn't happen
            throw new System.Exception("Program Bug - didn't expect WordFiles array to be full.");
        }




    }


}
