// COPYRIGHT 2009, 2010, 2011, 2012 by the Open Rails project.
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
using System.Linq;
using MSTS;

namespace ORTS
{
    public class SceneryDrawer
    {
        readonly Viewer3D Viewer;

        // THREAD SAFETY:
        //   All accesses must be done in local variables. No modifications to the objects are allowed except by
        //   assignment of a new instance (possibly cloned and then modified).
        public List<WorldFile> WorldFiles = new List<WorldFile>();
        int TileX;
        int TileZ;
        int VisibleTileX;
        int VisibleTileZ;

        public SceneryDrawer(Viewer3D viewer)
        {
            Viewer = viewer;
        }

        [CallOnThread("Loader")]
        public void Load()
        {
            if (TileX != VisibleTileX || TileZ != VisibleTileZ)
            {
                TileX = VisibleTileX;
                TileZ = VisibleTileZ;
                var worldFiles = WorldFiles;
                var newWorldFiles = new List<WorldFile>();
                var oldWorldFiles = new List<WorldFile>(worldFiles);
                var needed = (int)Math.Ceiling((float)Viewer.Settings.ViewingDistance / 2048f);
                for (var x = -needed; x <= needed; x++)
                {
                    for (var z = -needed; z <= needed; z++)
                    {
                        var tile = worldFiles.FirstOrDefault(t => t.TileX == TileX + x && t.TileZ == TileZ + z);
                        if (tile == null)
                            tile = LoadWorldFile(TileX + x, TileZ + z);
                        newWorldFiles.Add(tile);
                        oldWorldFiles.Remove(tile);
                    }
                }
                foreach (var tile in oldWorldFiles)
                    tile.Dispose();
                WorldFiles = newWorldFiles;
            }
        }

        [CallOnThread("Loader")]
        internal void Mark()
        {
            var worldFiles = WorldFiles;
            foreach (var tile in worldFiles)
                tile.Mark();
        }

        [CallOnThread("Updater")]
        public void Update(ElapsedTime elapsedTime)
        {
            var worldFiles = WorldFiles;
            foreach (var worldFile in worldFiles)
                worldFile.Update(elapsedTime);
        }

        [CallOnThread("Updater")]
        public void LoadPrep()
        {
            VisibleTileX = Viewer.Camera.TileX;
            VisibleTileZ = Viewer.Camera.TileZ;
        }

        [CallOnThread("Updater")]
        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            var worldFiles = WorldFiles;
            foreach (var worldFile in worldFiles)
                if (Viewer.Camera.InFOV(new Vector3((worldFile.TileX - Viewer.Camera.TileX) * 2048, 0, (worldFile.TileZ - Viewer.Camera.TileZ) * 2048), 1448))
                    worldFile.PrepareFrame(frame, elapsedTime);
        }

        WorldFile LoadWorldFile(int tileX, int tileZ)
        {
            Trace.Write("W");
            return new WorldFile(Viewer, tileX, tileZ);
        }
    }

    [CallOnThread("Loader")]
    public class WorldFile : IDisposable
    {
        // Dynamic track objects in the world file
        public struct DyntrackParams
        {
            public int isCurved;
            public float param1;
            public float param2;
        }

        public readonly int TileX, TileZ;
        public List<StaticShape> sceneryObjects = new List<StaticShape>();
        public List<DynatrackDrawer> dTrackList = new List<DynatrackDrawer>();
        public List<ForestDrawer> forestList = new List<ForestDrawer>();
        public List<RoadCarSpawner> carSpawners = new List<RoadCarSpawner>();
        public List<TrItemLabel> sidings = new List<TrItemLabel>();
        public List<TrItemLabel> platforms = new List<TrItemLabel>();

        readonly Viewer3D Viewer;

        /// <summary>
        /// Open the specified WFile and load all the scenery objects into the viewer.
        /// If the file doesn't exist, then return an empty WorldFile object.
        /// </summary>
        public WorldFile(Viewer3D viewer, int tileX, int tileZ)
        {
            Viewer = viewer;
            TileX = tileX;
            TileZ = tileZ;

            Viewer.Tiles.Load(tileX, tileZ);

            // determine file path to the WFile at the specified tile coordinates
            var WFileName = WorldFileNameFromTileCoordinates(tileX, tileZ);
            var WFilePath = viewer.Simulator.RoutePath + @"\World\" + WFileName;

            // if there isn't a file, then return with an empty WorldFile object
            if (!File.Exists(WFilePath))
                return;

            // read the world file 
            var WFile = new WFile(WFilePath);

            // create all the individual scenery objects specified in the WFile
            foreach (WorldObject worldObject in WFile.Tr_Worldfile)
            {
                if (worldObject.StaticDetailLevel > viewer.Settings.WorldObjectDensity)
                    continue;

                // Get the position of the scenery object into ORTS coordinate space.
                WorldPosition worldMatrix;
                if (worldObject.Matrix3x3 != null)
                    worldMatrix = WorldPositionFromMSTSLocation(WFile.TileX, WFile.TileZ, worldObject.Position, worldObject.Matrix3x3);
                else if (worldObject.QDirection != null)
                    worldMatrix = WorldPositionFromMSTSLocation(WFile.TileX, WFile.TileZ, worldObject.Position, worldObject.QDirection);
                else
                {
                    Trace.TraceWarning("{0} scenery object {1} is missing Matrix3x3 and QDirection", WFileName, worldObject.UID);
                    continue;
                }

                var shadowCaster = (worldObject.StaticFlags & (uint)StaticFlag.AnyShadow) != 0 || viewer.Settings.ShadowAllShapes;
                var animated = (worldObject.StaticFlags & (uint)StaticFlag.Animate) != 0;
                var global = (worldObject is TrackObj) || (worldObject.StaticFlags & (uint)StaticFlag.Global) != 0;

                // TransferObj have a FileName but it is not a shape, so we need to avoid sanity-checking it as if it was.
                var fileNameIsNotShape = (worldObject is TransferObj);

                // Determine the file path to the shape file for this scenery object and check it exists as expected.
                var shapeFilePath = fileNameIsNotShape || String.IsNullOrEmpty(worldObject.FileName) ? null : global ? viewer.Simulator.BasePath + @"\Global\Shapes\" + worldObject.FileName : viewer.Simulator.RoutePath + @"\Shapes\" + worldObject.FileName;
                if (shapeFilePath != null)
                {
                    shapeFilePath = Path.GetFullPath(shapeFilePath);
                    if (!File.Exists(shapeFilePath))
                    {
                        Trace.TraceWarning("{0} scenery object {1} with StaticFlags {3:X8} references non-existant {2}", WFileName, worldObject.UID, shapeFilePath, worldObject.StaticFlags);
                        shapeFilePath = null;
                    }
                }

                try
                {
                    if (worldObject.GetType() == typeof(MSTS.TrackObj))
                    {
                        var trackObj = (TrackObj)worldObject;
                        // Switch tracks need a link to the simulator engine so they can animate the points.
                        var trJunctionNode = trackObj.JNodePosn != null ? viewer.Simulator.TDB.GetTrJunctionNode(TileX, TileZ, (int)trackObj.UID) : null;
                        // We might not have found the junction node; if so, fall back to the static track shape.
                        if (trJunctionNode != null)
                            sceneryObjects.Add(new SwitchTrackShape(viewer, shapeFilePath, worldMatrix, trJunctionNode));
                        else
                            sceneryObjects.Add(new StaticTrackShape(viewer, shapeFilePath, worldMatrix));
                        if (viewer.Simulator.Settings.Wire == true && viewer.Simulator.TRK.Tr_RouteFile.Electrified == true)
                        {
                            int success = Wire.DecomposeStaticWire(viewer, dTrackList, trackObj, worldMatrix);
                            //if cannot draw wire, try to see if it is converted. modified for DynaTrax
                            if (success == 0 && trackObj.FileName.Contains("Dyna")) Wire.DecomposeConvertedDynamicWire(viewer, dTrackList, trackObj, worldMatrix);
                        }
                    }
                    else if (worldObject.GetType() == typeof(MSTS.DyntrackObj))
                    {
                        if (viewer.Simulator.Settings.Wire == true && viewer.Simulator.TRK.Tr_RouteFile.Electrified == true)
                            Wire.DecomposeDynamicWire(viewer, dTrackList, (DyntrackObj)worldObject, worldMatrix);
                        // Add DyntrackDrawers for individual subsections
                        Dynatrack.Decompose(viewer, dTrackList, (DyntrackObj)worldObject, worldMatrix);

                    } // end else if DyntrackObj
                    else if (worldObject.GetType() == typeof(MSTS.ForestObj))
                    {
                        if (!(worldObject as MSTS.ForestObj).IsYard)
                            forestList.Add(new ForestDrawer(viewer, (ForestObj)worldObject, worldMatrix));
                    }
                    else if (worldObject.GetType() == typeof(MSTS.SignalObj))
                    {
                        sceneryObjects.Add(new SignalShape(viewer, (SignalObj)worldObject, shapeFilePath, worldMatrix, shadowCaster ? ShapeFlags.ShadowCaster : ShapeFlags.None));
                    }
                    else if (worldObject.GetType() == typeof(MSTS.TransferObj))
                    {
                        sceneryObjects.Add(new TransferShape(viewer, (TransferObj)worldObject, worldMatrix));
                    }
                    else if (worldObject.GetType() == typeof(MSTS.LevelCrossingObj))
                    {
                        sceneryObjects.Add(new LevelCrossingShape(viewer, shapeFilePath, worldMatrix, shadowCaster ? ShapeFlags.ShadowCaster : ShapeFlags.None, (LevelCrossingObj)worldObject));
                    }
                    else if (worldObject.GetType() == typeof(MSTS.SpeedPostObj))
                    {
                        sceneryObjects.Add(new SpeedPostShape(viewer, shapeFilePath, worldMatrix, (SpeedPostObj)worldObject));
                    }
                    else if (worldObject.GetType() == typeof(MSTS.CarSpawnerObj))
                    {
                        carSpawners.Add(new RoadCarSpawner(viewer, worldMatrix, (CarSpawnerObj)worldObject));
                    }
                    else if (worldObject.GetType() == typeof(MSTS.SidingObj))
                    {
                        sidings.Add(new TrItemLabel(viewer, worldMatrix, (SidingObj)worldObject));
                    }
                    else if (worldObject.GetType() == typeof(MSTS.PlatformObj))
                    {
                        platforms.Add(new TrItemLabel(viewer, worldMatrix, (PlatformObj)worldObject));
                    }
                    else if (worldObject.GetType() == typeof(MSTS.StaticObj))
                    {
                        if (animated)
                            sceneryObjects.Add(new AnimatedShape(viewer, shapeFilePath, worldMatrix, shadowCaster ? ShapeFlags.ShadowCaster : ShapeFlags.None));
                        else
                            sceneryObjects.Add(new StaticShape(viewer, shapeFilePath, worldMatrix, shadowCaster ? ShapeFlags.ShadowCaster : ShapeFlags.None));
                    }
                    else // It's some other type of object - not one of the above.
                    {
                        sceneryObjects.Add(new StaticShape(viewer, shapeFilePath, worldMatrix, shadowCaster ? ShapeFlags.ShadowCaster : ShapeFlags.None));
                    }
                }
                catch (Exception error)
                {
                    Trace.TraceWarning("{0} scenery object {1} failed to load", worldMatrix, worldObject.UID);
                    Trace.WriteLine(error);
                }
            }

            if (Viewer.World.Sounds != null) Viewer.World.Sounds.AddByTile(TileX, TileZ);
        }

        #region IDisposable Members

        public void Dispose()
        {
            DisposeAndClearList(ref sceneryObjects);
            DisposeAndClearList(ref dTrackList);
            DisposeAndClearList(ref forestList);
            DisposeAndClearList(ref carSpawners);
            DisposeAndClearList(ref sidings);
            DisposeAndClearList(ref platforms);
            if (Viewer.World.Sounds != null) Viewer.World.Sounds.RemoveByTile(TileX, TileZ);
            // TODO: Do we need this when we don't have a destructor or Finalize()?
            GC.SuppressFinalize(true);
        }

        void DisposeAndClearList<T>(ref List<T> objects)
        {
            foreach (var obj in objects)
                if (obj is IDisposable)
                    (obj as IDisposable).Dispose();
            objects = new List<T>();
        }

        #endregion

        [CallOnThread("Loader")]
        internal void Mark()
        {
            foreach (var shape in sceneryObjects)
                shape.Mark();
            foreach (var dTrack in dTrackList)
                dTrack.Mark();
            foreach (var forest in forestList)
                forest.Mark();
        }

        [CallOnThread("Updater")]
        public void Update(ElapsedTime elapsedTime)
        {
            foreach (var spawner in carSpawners)
                spawner.Update(elapsedTime);
        }

        [CallOnThread("Updater")]
        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            foreach (var shape in sceneryObjects)
                shape.PrepareFrame(frame, elapsedTime);
            foreach (var dTrack in dTrackList)
                dTrack.PrepareFrame(frame, elapsedTime);
            foreach (var forest in forestList)
                forest.PrepareFrame(frame, elapsedTime);
        }

        /// <summary>
        /// MSTS WFiles represent some location with a position, quaternion and tile coordinates
        /// This converts it to the ORTS WorldPosition representation
        /// </summary>
        WorldPosition WorldPositionFromMSTSLocation(int tileX, int tileZ, STFPositionItem MSTSPosition, STFQDirectionItem MSTSQuaternion)
        {
            var XNAQuaternion = new Quaternion((float)MSTSQuaternion.A, (float)MSTSQuaternion.B, -(float)MSTSQuaternion.C, (float)MSTSQuaternion.D);
            var XNAPosition = new Vector3((float)MSTSPosition.X, (float)MSTSPosition.Y, -(float)MSTSPosition.Z);
            var XNAMatrix = Matrix.CreateFromQuaternion(XNAQuaternion);
            XNAMatrix *= Matrix.CreateTranslation(XNAPosition);

            var worldMatrix = new WorldPosition();
            worldMatrix.TileX = tileX;
            worldMatrix.TileZ = tileZ;
            worldMatrix.XNAMatrix = XNAMatrix;

            return worldMatrix;
        }

        /// <summary>
        /// MSTS WFiles represent some location with a position, 3x3 matrix and tile coordinates
        /// This converts it to the ORTS WorldPosition representation
        /// </summary>
        WorldPosition WorldPositionFromMSTSLocation(int tileX, int tileZ, STFPositionItem MSTSPosition, Matrix3x3 MSTSMatrix)
        {
            var XNAPosition = new Vector3((float)MSTSPosition.X, (float)MSTSPosition.Y, -(float)MSTSPosition.Z);
            var XNAMatrix = Matrix.Identity;
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

            var worldMatrix = new WorldPosition();
            worldMatrix.TileX = tileX;
            worldMatrix.TileZ = tileZ;
            worldMatrix.XNAMatrix = XNAMatrix;

            return worldMatrix;
        }

        /// <summary>
        /// Build a w filename from tile X and Z coordinates.
        /// Returns a string eg "w-011283+014482.w"
        /// </summary>
        string WorldFileNameFromTileCoordinates(int tileX, int tileZ)
        {
            var filename = "w" + FormatTileCoordinate(tileX) + FormatTileCoordinate(tileZ) + ".w";
            return filename;
        }

        /// <summary>
        /// For building a filename from tile X and Z coordinates.
        /// Returns the string representation of a coordinate
        /// eg "+014482"
        /// </summary>
        string FormatTileCoordinate(int tileCoord)
        {
            var sign = "+";
            if (tileCoord < 0)
            {
                sign = "-";
                tileCoord *= -1;
            }
            return sign + tileCoord.ToString("000000");
        }
    }
}
