// COPYRIGHT 2009, 2010, 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

/* SCENERY
 * 
 * Scenery objects are specified in WFiles located in the WORLD folder of the route.
 * Each WFile describes scenery for a 2048 meter square region of the route.
 * This assembly is responsible for loading and unloading the WFiles as 
 * the camera moves over the route.  
 * 
 * Loaded WFiles are each represented by an instance of the WorldFile class. 
 * 
 * A SceneryDrawer object is created by the Viewer. Each time SceneryDrawer.Update is 
 * called, it disposes of WorldFiles that have gone out of range, and creates new 
 * WorldFile objects for WFiles that have come into range.
 * 
 * Currently the SceneryDrawer. Update is called 10 times a second from a background 
 * thread in the Viewer class.
 * 
 * SceneryDrawer loads the WFile in which the viewer is located, and the 8 WFiles 
 * surrounding the viewer.
 * 
 * When a WorldFile object is created, it creates StaticShape objects for each scenery
 * item.  The StaticShape objects add themselves to the Viewer's content list, sharing
 * mesh files and textures wherever possible.
 * 
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using MSTS;

namespace ORTS
{
    /// <summary>
    /// Handles loading and unloading of WFiles as the viewer moves across the route.
    /// </summary>
    /// Maintains an array of the loaded WorldFiles.  As the camera moves, Update
    /// scans the array, removing WorldFiles that are out of range, and creating new
    /// WorldFile objects for WFiles that come into range.
    public class SceneryDrawer
    {
        private Viewer3D Viewer;  // the viewer that we are tracking
        private int viewerTileX, viewerTileZ;  // the location of the viewer when the current set of wFiles was loaded
        private int lastViewerTileX, lastViewerTileZ;
        public WorldFile[] WorldFiles = new WorldFile[9];  // surrounding wFiles, not in any particular order, null when empty

        /// <summary>
        /// Scenery objects will be loaded into this viewer.
        /// </summary>
        public SceneryDrawer(Viewer3D viewer)
        {
            Viewer = viewer;

            // initialize 
            for (int i = 0; i < WorldFiles.Length; ++i)
                WorldFiles[i] = null;
        }

        /// <summary>
        /// Called 10 times per second when its safe to read volatile data
        /// from the simulator and viewer classes in preparation
        /// for the Load call.  Copy data to local storage for use 
        /// in the next load call.
        /// Executes in the UpdaterProcess thread.
        /// </summary>
        public void LoadPrep()
        {
            viewerTileX = Viewer.Camera.TileX;
            viewerTileZ = Viewer.Camera.TileZ;
        }

        /// <summary>
        /// Called 10 times a second to load graphics content
        /// that comes and goes as the player and trains move.
        /// Called from background LoaderProcess Thread
        /// Do not access volatile data from the simulator 
        /// and viewer classes during the Load call ( see
        /// LoadPrep() )
        /// Executes in the LoaderProcess thread.
        /// </summary>
        public void Load(RenderProcess renderProcess)
        {
            if (viewerTileX != lastViewerTileX || viewerTileZ != lastViewerTileZ)   // if the camera has moved into a new tile
            {
                lastViewerTileX = viewerTileX;
                lastViewerTileZ = viewerTileZ;

                // scan the wFiles array, removing any out of range
                // THREAD SAFETY WARNING - UpdateProcess could read this array at any time
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
                            Trace.Write("w");
							WorldFiles[i].DisposeCrossing(); //added to tell some crossings they are out of range
                            WorldFiles[i] = null;
                            // World sounds - By GeorgeS
                            if (Viewer.WorldSounds != null) Viewer.WorldSounds.RemoveByTile(tile.TileX, tile.TileZ);
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
            // THREAD SAFETY WARNING - UpdateProcess could read this array at any time
            for (int i = 0; i < WorldFiles.Length; ++i)
                if (WorldFiles[i] == null)  // we found one
                {
                    Trace.Write("W");
                    WorldFiles[i] = new WorldFile(Viewer, tileX, tileZ);
                    // Load world sounds - By GeorgeS
					if (Viewer.WorldSounds != null) Viewer.WorldSounds.AddByTile(tileX, tileZ);
                    return;
                }

            // otherwise we didn't find an available spot - this shouldn't happen
            Debug.Fail("Program Bug - didn't expect TerrainTiles array to be full.");
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            // THREAD SAFETY WARNING - LoaderProcess could write to this array at any time
            // its OK to iterate through this array because LoaderProcess never changes the size
            foreach (WorldFile wFile in WorldFiles)
            {
                if (wFile != null)
                {
                    if (Viewer.Camera.InFOV(new Vector3((wFile.TileX - Viewer.Camera.TileX) * 2048, 0, (wFile.TileZ - Viewer.Camera.TileZ) * 2048), 1448))
                    {
                        wFile.PrepareFrame(frame, elapsedTime);
                        foreach (DynatrackDrawer dTrack in wFile.dTrackList)
                            dTrack.PrepareFrame(frame, elapsedTime);
                        foreach (ForestDrawer forest in wFile.forestList)
                            forest.PrepareFrame(frame, elapsedTime);
						foreach (CarSpawner spawner in wFile.carSpawners)
							spawner.SpawnCars(elapsedTime.ClockSeconds);
                    }
                }
            }
        }
    } // SceneryDrawer


    /// <summary>
    /// Represents a loaded WFile.
    /// </summary>
    public class WorldFile: IDisposable
    {
        public int TileX, TileZ;

        public List<StaticShape> SceneryObjects = new List<StaticShape>();

        // Dynamic track objects in the world file
        public struct DyntrackParams
        {
            public int isCurved;
            public float param1;
            public float param2;
        }
        public List<DynatrackDrawer> dTrackList = new List<DynatrackDrawer>();
        public List<ForestDrawer> forestList = new List<ForestDrawer>();
		public List<CarSpawner> carSpawners = new List<CarSpawner>();
		public List<SidingLabel> sidings = new List<SidingLabel>();
        public List<PlatformLabel> platforms = new List<PlatformLabel>();

        /// <summary>
        /// Open the specified WFile and load all the scenery objects into the viewer.
        /// If the file doesn't exist, then return an empty WorldFile object.
        /// </summary>
        public WorldFile( Viewer3D viewer, int tileX, int tileZ )
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
                if (worldObject.StaticDetailLevel > viewer.Settings.WorldObjectDensity)
                    continue;

                // determine the full file path to the shape file for this scenery object 
                string shapeFilePath;
                if (worldObject.GetType() == typeof(MSTS.TrackObj))
                    shapeFilePath = viewer.Simulator.BasePath + @"\global\shapes\" + worldObject.FileName;
                // Skip dynamic track: no shape file
                else if (worldObject.GetType() == typeof(MSTS.DyntrackObj))
                    shapeFilePath = null;
                else
                    shapeFilePath = viewer.Simulator.RoutePath + @"\shapes\" + worldObject.FileName;

                // get the position of the scenery object into ORTS coordinate space
                WorldPosition worldMatrix;
				if (worldObject.Matrix3x3 != null)
					worldMatrix = WorldPositionFromMSTSLocation(WFile.TileX, WFile.TileZ, worldObject.Position, worldObject.Matrix3x3);
				else if (worldObject.QDirection != null)
					worldMatrix = WorldPositionFromMSTSLocation(WFile.TileX, WFile.TileZ, worldObject.Position, worldObject.QDirection);
				else
				{
					Trace.TraceError("Object {1} is missing Matrix3x3 and QDirection in {0}", WFileName, worldObject.UID);
					continue;
				}


                if (worldObject.GetType() == typeof(MSTS.TrackObj))
                {
                    TrackObj trackObj = (TrackObj)worldObject;
                    if (trackObj.JNodePosn != null)
                    {
                        // switch tracks need a link to the simulator engine so they can animate the points
                        TrJunctionNode TRJ = viewer.Simulator.TDB.GetTrJunctionNode(TileX, TileZ, (int)trackObj.UID);
                        SceneryObjects.Add(new SwitchTrackShape(viewer, shapeFilePath, worldMatrix, TRJ));
						if (Program.Simulator.Settings.Wire == true && Program.Simulator.TRK.Tr_RouteFile.Electrified == true)
						{
							Wire.DecomposeStaticWire(viewer, dTrackList, trackObj, worldMatrix);
						}
                    }
                    else // it's some type of track other than a switch track
                    {
                        SceneryObjects.Add(new StaticTrackShape(viewer, shapeFilePath, worldMatrix));
						if (Program.Simulator.Settings.Wire == true && Program.Simulator.TRK.Tr_RouteFile.Electrified == true)
						{
							Wire.DecomposeStaticWire(viewer, dTrackList, trackObj, worldMatrix);
						}
                    }
                }
                else if (worldObject.GetType() == typeof(MSTS.DyntrackObj))
                {
					if (Program.Simulator.Settings.Wire == true && Program.Simulator.TRK.Tr_RouteFile.Electrified == true)
					{
						Wire.DecomposeDynamicWire(viewer, dTrackList, (DyntrackObj)worldObject, worldMatrix);
					}
					// Add DyntrackDrawers for individual subsections
                    Dynatrack.Decompose(viewer, dTrackList, (DyntrackObj)worldObject, worldMatrix);

                } // end else if DyntrackObj
                else if (worldObject.GetType() == typeof(MSTS.ForestObj))
                {
                    ForestObj forestObj = (ForestObj)worldObject;
                    forestList.Add(new ForestDrawer(viewer, forestObj, worldMatrix));
                }
				else if (worldObject.GetType() == typeof(MSTS.SignalObj))
				{
					var shadowCaster = (worldObject.StaticFlags & (uint)StaticFlag.AnyShadow) != 0 || viewer.Settings.ShadowAllShapes;
					SceneryObjects.Add(new SignalShape(viewer, (SignalObj)worldObject, shapeFilePath, worldMatrix, shadowCaster ? ShapeFlags.ShadowCaster : ShapeFlags.None));
				}
				else if (worldObject.GetType() == typeof(MSTS.LevelCrossingObj))
				{
					SceneryObjects.Add(new LevelCrossingShape(viewer, shapeFilePath, worldMatrix, (LevelCrossingObj) worldObject, viewer.Simulator.LevelCrossings.LevelCrossingObjects));
				}
				else if (worldObject.GetType() == typeof(MSTS.CarSpawnerObj))
				{
					if (viewer.Simulator.RDB != null && viewer.Simulator.CarSpawnerFile != null)
						carSpawners.Add(new CarSpawner((CarSpawnerObj)worldObject, worldMatrix));
					else
						Trace.TraceWarning("Ignored car spawner {1} in {0} because route has no RDB or carspawn.dat.", WFileName, worldObject.UID);
				}
				else if (worldObject.GetType() == typeof(MSTS.SidingObj)) {
					sidings.Add(new SidingLabel(viewer, worldMatrix, (SidingObj)worldObject));
				} 
                else if (worldObject.GetType() == typeof(MSTS.PlatformObj)) {
                    platforms.Add(new PlatformLabel(viewer, worldMatrix, (PlatformObj)worldObject));
                } 
                else // It's some other type of object - not one of the above.
				{
					var shadowCaster = (worldObject.StaticFlags & (uint)StaticFlag.AnyShadow) != 0 || viewer.Settings.ShadowAllShapes;
					SceneryObjects.Add(new StaticShape(viewer, shapeFilePath, worldMatrix, shadowCaster ? ShapeFlags.ShadowCaster : ShapeFlags.None));
				}
            }

        } // WorldFile constructor

		//treat level crossing which is out of range
		public void DisposeCrossing()
		{
			LevelCrossingShape tempCrossingShape;
			foreach (StaticShape shape in SceneryObjects)
			{
				if (shape.GetType() == typeof(LevelCrossingShape)) 
				{
					tempCrossingShape = (LevelCrossingShape)shape;
					tempCrossingShape.crossingObj.inrange = false;
					if (tempCrossingShape.Sound != null)
					{
						tempCrossingShape.Viewer.SoundProcess.RemoveSoundSource(tempCrossingShape.Sound); //remove the sound
					}
				}
			}
		}

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            foreach (StaticShape shape in SceneryObjects)
                shape.PrepareFrame(frame, elapsedTime);
        }

        /// <summary>
        /// MSTS WFiles represent some location with a position, quaternion and tile coordinates
        /// This converts it to the ORTS WorldPosition representation
        /// </summary>
        public WorldPosition WorldPositionFromMSTSLocation(int tileX, int tileZ, STFPositionItem MSTSPosition, STFQDirectionItem MSTSQuaternion )
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
}
