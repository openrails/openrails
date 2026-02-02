// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
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

// This file is the responsibility of the 3D & Environment Team. 

// Experimental code which collapses unnecessarily duplicated primitives when loading shapes.
// WANRING: Slower and not guaranteed to work!
//#define OPTIMIZE_SHAPES_ON_LOAD

// Prints out lots of diagnostic information about the construction of shapes, with regards their sub-objects and hierarchies.
//#define DEBUG_SHAPE_HIERARCHY

// Adds bright green arrows to all normal shapes indicating the direction of their normals.
//#define DEBUG_SHAPE_NORMALS

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Formats.Msts;
using Orts.Simulation;
using Orts.Simulation.RollingStocks;
using Orts.Viewer3D.Common;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Event = Orts.Common.Event;
using Events = Orts.Common.Events;

namespace Orts.Viewer3D
{
    [CallOnThread("Loader")]
    public class SharedShapeManager
    {
        readonly Viewer Viewer;

        Dictionary<string, SharedShape> Shapes = new Dictionary<string, SharedShape>();
        HashSet<SharedShape> MarkedShapes = new HashSet<SharedShape>();
        SharedShape EmptyShape;

        [CallOnThread("Render")]
        internal SharedShapeManager(Viewer viewer)
        {
            Viewer = viewer;
            EmptyShape = new SharedShape(Viewer);
        }

        public SharedShape Get(string path)
        {
            if (Thread.CurrentThread.Name != "Loader Process")
                Trace.TraceError("SharedShapeManager.Get incorrectly called by {0}; must be Loader Process or crashes will occur.", Thread.CurrentThread.Name);

            if (path == null || path == EmptyShape.FilePath)
                return EmptyShape;

            path = path.ToLowerInvariant();
            if (!Shapes.ContainsKey(path) || Shapes[path].StaleData)
            {
                try
                {
                    Shapes[path] = new SharedShape(Viewer, path);
                }
                catch (Exception error)
                {
                    Trace.WriteLine(new FileLoadException(path, error));
                    Shapes[path] = EmptyShape;
                }
            }
            return Shapes[path];
        }

        public void Mark()
        {
            MarkedShapes.Clear();
        }

        public void Mark(SharedShape shape)
        {
            if (shape != null)
                MarkedShapes.Add(shape);
        }

        public void Sweep()
        {
            // If a shape isn't in the list of marked shapes, it is no longer in use
            List<string> shapeKeys = Shapes.Keys.ToList();
            foreach (string key in shapeKeys)
                if (!MarkedShapes.Contains(Shapes[key]))
                {
                    Shapes[key].Dispose();
                    Shapes.Remove(key);
                }
        }

        /// <summary>
        /// Sets the stale data flag for ALL shared shapes to the given bool
        /// (default true)
        /// </summary>
        public void SetAllStale(bool stale = true)
        {
            foreach (SharedShape shape in Shapes.Values)
                shape.StaleData = stale;
        }

        /// <summary>
        /// Sets the stale data flag for shapes using a shape file from the given set of paths
        /// </summary>
        /// <returns>bool indicating if any shape changed from fresh to stale</returns>
        public bool MarkStale(HashSet<string> sPaths)
        {
            // The same shape file may be used by multiple shared shapes, need to iterate to check each shared shape
            bool found = false;

            foreach (string shapeKey in Shapes.Keys)
            {
                string shapeFile = shapeKey;

                // Shapes specify a shape location and a texture location, only check against the shape location
                if (shapeKey.Contains('\0'))
                    shapeFile = shapeKey.Split('\0')[0];

                if (!Shapes[shapeKey].StaleData && (sPaths.Contains(Shapes[shapeKey].FilePath) || sPaths.Contains(shapeFile)))
                {
                    // Mark shape as stale so it gets reloaded
                    Shapes[shapeKey].StaleData = true;
                    found = true;

                    Trace.TraceInformation("Shape file {0} was updated on disk and will be reloaded.", shapeFile);
                }
                // Continue scanning, there may be multiple matching shapes
            }

            return found;
        }

        /// <summary>
        /// Checks all shapes for stale materials and sets the stale data flag if any materials are stale
        /// </summary>
        /// <returns>bool indicating if any shape changed from fresh to stale</returns>
        public bool CheckStale()
        {
            // The same materials may be used by multiple shared shapes, need to iterate to check each shared shape
            bool found = false;

            foreach (SharedShape shape in Shapes.Values)
            {
                if (!shape.StaleData)
                {
                    foreach (Material material in shape.Materials)
                    {
                        if (material.StaleData)
                        {
                            // Found a match to an affected material; mark shape as stale so it gets reloaded
                            shape.StaleData = true;
                            found = true;

                            Trace.TraceInformation("Texture used by shape file {0} was updated on disk, shape will be reloaded.", shape.FilePath);

                            break;
                        }
                    }
                }
                // Continue scanning, there may be multiple shapes with stale materials
            }

            return found;
        }

        [CallOnThread("Updater")]
        public string GetStatus()
        {
            return Viewer.Catalog.GetPluralStringFmt("{0:F0} shape", "{0:F0} shapes", Shapes.Keys.Count);
        }
    }

    [Flags]
    public enum ShapeFlags
    {
        None = 0,
        // Shape casts a shadow (scenery objects according to RE setting, and all train objects).
        ShadowCaster = 1,
        // Shape needs automatic z-bias to keep it out of trouble.
        AutoZBias = 2,
        // Shape is an interior and must be rendered in a separate group.
        Interior = 4,
        // NOTE: Use powers of 2 for values!
    }

    public class StaticShape
    {
        public readonly Viewer Viewer;
        public readonly WorldPosition Location;
        public readonly ShapeFlags Flags;
        public readonly SharedShape SharedShape;

        /// <summary>
        /// Construct and initialize the class
        /// This constructor is for objects described by a MSTS shape file
        /// </summary>
        public StaticShape(Viewer viewer, string path, WorldPosition position, ShapeFlags flags)
        {
            Viewer = viewer;
            Location = position;
            Flags = flags;

            if (path != null)
            {
                // Use resolved path, without any 'up one level' ("..\\") calls
                if (path.Contains('\0'))
                {
                    string[] dualPath = path.Split('\0');
                    path = Path.GetFullPath(dualPath[0]) + '\0' + Path.GetFullPath(dualPath[1]);
                }
                else
                    path = Path.GetFullPath(path);
            }

            SharedShape = Viewer.ShapeManager.Get(path);
        }

        public virtual void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            SharedShape.PrepareFrame(frame, Location, Flags);
        }

        [CallOnThread("Loader")]
        public virtual void Unload()
        {
        }

        [CallOnThread("Loader")]
        internal virtual void Mark()
        {
            SharedShape.Mark();
        }
    }

    public class SharedStaticShapeInstance : StaticShape
    {
        readonly bool HasNightSubObj;
        readonly float ObjectRadius;
        readonly float ObjectViewingDistance;
        readonly ShapePrimitiveInstances[] Primitives;

        public SharedStaticShapeInstance(Viewer viewer, string path, List<StaticShape> shapes)
            : base(viewer, path, GetCenterLocation(shapes), shapes[0].Flags)
        {
            HasNightSubObj = shapes[0].SharedShape.HasNightSubObj;

            if (shapes[0].SharedShape.LodControls.Length > 0)
            {
                // We need both ends of the distance levels. We render the first but view as far as the last.
                var dlHighest = shapes[0].SharedShape.LodControls[0].DistanceLevels.First();
                var dlLowest = shapes[0].SharedShape.LodControls[0].DistanceLevels.Last();

                // Object radius should extend from central location to the furthest instance location PLUS the actual object radius.
                ObjectRadius = shapes.Max(s => (Location.Location - s.Location.Location).Length()) + dlHighest.ViewSphereRadius;

                // Object viewing distance is easy because it's based on the outside of the object radius.
                if (viewer.Settings.LODViewingExtension)
                    // Set to MaxValue so that an object never disappears.
                    // Many MSTS objects had a LOD of 2km which is the maximum distance that MSTS can handle.
                    // Open Rails can handle greater distances, so we override the lowest-detail LOD to make sure OR shows shapes further away than 2km.
                    // See http://www.elvastower.com/forums/index.php?/topic/35301-menu-options/page__view__findpost__p__275531
                    ObjectViewingDistance = float.MaxValue;
                else
                    ObjectViewingDistance = dlLowest.ViewingDistance;
            }

            // Create all the primitives for the shared shape.
            var prims = new List<ShapePrimitiveInstances>();
            foreach (var lod in shapes[0].SharedShape.LodControls)
                for (var subObjectIndex = 0; subObjectIndex < lod.DistanceLevels[0].SubObjects.Length; subObjectIndex++)
                    foreach (var prim in lod.DistanceLevels[0].SubObjects[subObjectIndex].ShapePrimitives)
                        prims.Add(new ShapePrimitiveInstances(viewer.GraphicsDevice, prim, GetMatricies(shapes, prim), subObjectIndex));
            Primitives = prims.ToArray();
        }

        static WorldPosition GetCenterLocation(List<StaticShape> shapes)
        {
            var tileX = shapes.Min(s => s.Location.TileX);
            var tileZ = shapes.Min(s => s.Location.TileZ);
            Debug.Assert(tileX == shapes.Max(s => s.Location.TileX));
            Debug.Assert(tileZ == shapes.Max(s => s.Location.TileZ));
            var minX = shapes.Min(s => s.Location.Location.X);
            var maxX = shapes.Max(s => s.Location.Location.X);
            var minY = shapes.Min(s => s.Location.Location.Y);
            var maxY = shapes.Max(s => s.Location.Location.Y);
            var minZ = shapes.Min(s => s.Location.Location.Z);
            var maxZ = shapes.Max(s => s.Location.Location.Z);
            return new WorldPosition() { TileX = tileX, TileZ = tileZ, Location = new Vector3((minX + maxX) / 2, (minY + maxY) / 2, (minZ + maxZ) / 2) };
        }

        Matrix[] GetMatricies(List<StaticShape> shapes, ShapePrimitive shapePrimitive)
        {
            var matrix = Matrix.Identity;
            var hi = shapePrimitive.HierarchyIndex;
            while (hi >= 0 && hi < shapePrimitive.Hierarchy.Length && shapePrimitive.Hierarchy[hi] != -1)
            {
                matrix *= SharedShape.Matrices[hi];
                hi = shapePrimitive.Hierarchy[hi];
            }

            var matricies = new Matrix[shapes.Count];
            for (var i = 0; i < shapes.Count; i++)
                matricies[i] = matrix * shapes[i].Location.XNAMatrix * Matrix.CreateTranslation(-Location.Location.X, -Location.Location.Y, Location.Location.Z);

            return matricies;
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            var dTileX = Location.TileX - Viewer.Camera.TileX;
            var dTileZ = Location.TileZ - Viewer.Camera.TileZ;
            var mstsLocation = Location.Location + new Vector3(dTileX * 2048, 0, dTileZ * 2048);
            var xnaMatrix = Matrix.CreateTranslation(mstsLocation.X, mstsLocation.Y, -mstsLocation.Z);
            foreach (var primitive in Primitives)
                if (primitive.SubObjectIndex != 1 || !HasNightSubObj || Viewer.MaterialManager.sunDirection.Y < 0)
                    frame.AddAutoPrimitive(mstsLocation, ObjectRadius, ObjectViewingDistance, primitive.Material, primitive, RenderPrimitiveGroup.World, ref xnaMatrix, Flags);
        }
    }

    public class StaticTrackShape : StaticShape
    {
        public StaticTrackShape(Viewer viewer, string path, WorldPosition position)
            : base(viewer, path, position, ShapeFlags.AutoZBias)
        {
        }
    }

    /// <summary>
    /// Has a heirarchy of objects that can be moved by adjusting the XNAMatrices
    /// at each node.
    /// </summary>
    public class PoseableShape : StaticShape
    {
        protected static Dictionary<string, bool> SeenShapeAnimationError = new Dictionary<string, bool>();

        public Matrix[] XNAMatrices = new Matrix[0];  // the positions of the subobjects

        public readonly int[] Hierarchy;

        public PoseableShape(Viewer viewer, string path, WorldPosition initialPosition, ShapeFlags flags)
            : base(viewer, path, initialPosition, flags)
        {
            if (SharedShape.Matrices.Length > 0)
            {
                XNAMatrices = new Matrix[SharedShape.Matrices.Length];
                for (int iMatrix = 0; iMatrix < SharedShape.Matrices.Length; ++iMatrix)
                    XNAMatrices[iMatrix] = SharedShape.Matrices[iMatrix];
            }
            else // If the shape file is missing or fails to load, we need some default data to prevent crashes
            {
                if (path != null && path != "Empty")
                {
                    string location = path;
                    if (path != null && path.Contains('\0'))
                        location = path.Split('\0')[0];

                    Trace.TraceWarning("Couldn't load shape {0} file may be corrupt", location);
                }
                // The 0th matrix should always be the identity matrix
                XNAMatrices = new Matrix[1];
                XNAMatrices[0] = Matrix.Identity;
            }

            if (SharedShape.LodControls.Length > 0 && SharedShape.LodControls[0].DistanceLevels.Length > 0 && SharedShape.LodControls[0].DistanceLevels[0].SubObjects.Length > 0 && SharedShape.LodControls[0].DistanceLevels[0].SubObjects[0].ShapePrimitives.Length > 0)
                Hierarchy = SharedShape.LodControls[0].DistanceLevels[0].SubObjects[0].ShapePrimitives[0].Hierarchy;
            else
                Hierarchy = new int[0];
        }

        public PoseableShape(Viewer viewer, string path, WorldPosition initialPosition)
            : this(viewer, path, initialPosition, ShapeFlags.None)
        {
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            SharedShape.PrepareFrame(frame, Location, XNAMatrices, Flags);
        }

        public void ConditionallyPrepareFrame(RenderFrame frame, ElapsedTime elapsedTime, bool[] matrixVisible = null)
        {
            SharedShape.PrepareFrame(frame, Location, XNAMatrices, Flags, matrixVisible);
        }

        /// <summary>
        /// Adjust the pose of the specified node to the frame position specifed by key.
        /// </summary>
        public void AnimateMatrix(int iMatrix, float key)
        {
            // Animate the given matrix.
            AnimateOneMatrix(iMatrix, key);

            // Animate all child nodes in the hierarchy too.
            for (var i = 0; i < Hierarchy.Length; i++)
                if (Hierarchy[i] == iMatrix)
                    AnimateMatrix(i, key);
        }

        protected virtual void AnimateOneMatrix(int iMatrix, float key)
        {
            if (SharedShape.Animations == null || SharedShape.Animations.Count == 0)
            {
                if (!SeenShapeAnimationError.ContainsKey(SharedShape.FilePath))
                    Trace.TraceInformation("Ignored missing animations data in shape {0}", SharedShape.FilePath);
                SeenShapeAnimationError[SharedShape.FilePath] = true;
                return;  // animation is missing
            }

            if (iMatrix < 0 || iMatrix >= SharedShape.Animations[0].anim_nodes.Count || iMatrix >= XNAMatrices.Length)
            {
                if (!SeenShapeAnimationError.ContainsKey(SharedShape.FilePath))
                    Trace.TraceInformation("Ignored out of bounds matrix {1} in shape {0}", SharedShape.FilePath, iMatrix);
                SeenShapeAnimationError[SharedShape.FilePath] = true;
                return;  // mismatched matricies
            }

            var anim_node = SharedShape.Animations[0].anim_nodes[iMatrix];
            if (anim_node.controllers.Count == 0)
                return;  // missing controllers

            // Start with the intial pose in the shape file.
            var xnaPose = SharedShape.Matrices[iMatrix];

            foreach (controller controller in anim_node.controllers)
            {
                // Determine the frame index from the current frame ('key'). We will be interpolating between two key
                // frames (the items in 'controller') so we need to find the last one LESS than the current frame
                // and interpolate with the one after it.
                var index = 0;
                for (var i = 0; i < controller.Count; i++)
                    if (controller[i].Frame <= key)
                        index = i;
                    else if (controller[i].Frame > key) // Optimisation, not required for algorithm.
                        break;

                var position1 = controller[index];
                var position2 = index + 1 < controller.Count ? controller[index + 1] : controller[index];
                var frame1 = position1.Frame;
                var frame2 = position2.Frame;

                // Make sure to clamp the amount, as we can fall outside the frame range. Also ensure there's a
                // difference between frame1 and frame2 or we'll crash.
                var amount = frame1 < frame2 ? MathHelper.Clamp((key - frame1) / (frame2 - frame1), 0, 1) : 0;

                if (position1.GetType() == typeof(slerp_rot))  // rotate the existing matrix
                {
                    slerp_rot MSTS1 = (slerp_rot)position1;
                    slerp_rot MSTS2 = (slerp_rot)position2;
                    Quaternion XNA1 = new Quaternion(MSTS1.X, MSTS1.Y, -MSTS1.Z, MSTS1.W);
                    Quaternion XNA2 = new Quaternion(MSTS2.X, MSTS2.Y, -MSTS2.Z, MSTS2.W);
                    Quaternion q = Quaternion.Slerp(XNA1, XNA2, amount);
                    Vector3 location = xnaPose.Translation;
                    xnaPose = Matrix.CreateFromQuaternion(q);
                    xnaPose.Translation = location;
                }
                else if (position1.GetType() == typeof(linear_key))  // a key sets an absolute position, vs shifting the existing matrix
                {
                    linear_key MSTS1 = (linear_key)position1;
                    linear_key MSTS2 = (linear_key)position2;
                    Vector3 XNA1 = new Vector3(MSTS1.X, MSTS1.Y, -MSTS1.Z);
                    Vector3 XNA2 = new Vector3(MSTS2.X, MSTS2.Y, -MSTS2.Z);
                    Vector3 v = Vector3.Lerp(XNA1, XNA2, amount);
                    xnaPose.Translation = v;
                }
                else if (position1.GetType() == typeof(tcb_key)) // a tcb_key sets an absolute rotation, vs rotating the existing matrix
                {
                    tcb_key MSTS1 = (tcb_key)position1;
                    tcb_key MSTS2 = (tcb_key)position2;
                    Quaternion XNA1 = new Quaternion(MSTS1.X, MSTS1.Y, -MSTS1.Z, MSTS1.W);
                    Quaternion XNA2 = new Quaternion(MSTS2.X, MSTS2.Y, -MSTS2.Z, MSTS2.W);
                    Quaternion q = Quaternion.Slerp(XNA1, XNA2, amount);
                    Vector3 location = xnaPose.Translation;
                    xnaPose = Matrix.CreateFromQuaternion(q);
                    xnaPose.Translation = location;
                }
            }
            XNAMatrices[iMatrix] = xnaPose;  // update the matrix
        }
    }

    /// <summary>
    /// An animated shape has a continuous repeating motion defined
    /// in the animations of the shape file.
    /// </summary>
    public class AnimatedShape : PoseableShape
    {
        protected float AnimationKey;  // advances with time
        protected float FrameRateMultiplier = 1; // e.g. in passenger view shapes MSTS divides by 30 the frame rate; this is the inverse

        /// <summary>
        /// Construct and initialize the class
        /// </summary>
        public AnimatedShape(Viewer viewer, string path, WorldPosition initialPosition, ShapeFlags flags, float frameRateDivisor = 1.0f)
            : base(viewer, path, initialPosition, flags)
        {
            FrameRateMultiplier = 1 / frameRateDivisor;
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            // if the shape has animations
            if (SharedShape.Animations?.Count > 0 && SharedShape.Animations[0].FrameCount > 0)
            {
                AnimationKey += SharedShape.Animations[0].FrameRate * elapsedTime.ClockSeconds * FrameRateMultiplier;
                while (AnimationKey > SharedShape.Animations[0].FrameCount) AnimationKey -= SharedShape.Animations[0].FrameCount;
                while (AnimationKey < 0) AnimationKey += SharedShape.Animations[0].FrameCount;

                // Update the pose for each matrix
                for (var matrix = 0; matrix < SharedShape.Matrices.Length; ++matrix)
                    AnimateMatrix(matrix, AnimationKey);
            }
            SharedShape.PrepareFrame(frame, Location, XNAMatrices, Flags);
        }
    }

        //Class AnalogClockShape to animate analog OR-Clocks as child of AnimatedShape <- PoseableShape <- StaticShape
    public class AnalogClockShape : AnimatedShape
    {
        public AnalogClockShape(Viewer viewer, string path, WorldPosition initialPosition, ShapeFlags flags, float frameRateDivisor = 1.0f)
            : base(viewer, path, initialPosition, flags)
        {
        }

        protected override void AnimateOneMatrix(int iMatrix, float key)
        {
            if (SharedShape.Animations == null || SharedShape.Animations.Count == 0)
            {
                if (!SeenShapeAnimationError.ContainsKey(SharedShape.FilePath))
                    Trace.TraceInformation("Ignored missing animations data in shape {0}", SharedShape.FilePath);
                SeenShapeAnimationError[SharedShape.FilePath] = true;
                return;  // animation is missing
            }

            if (iMatrix < 0 || iMatrix >= SharedShape.Animations[0].anim_nodes.Count || iMatrix >= XNAMatrices.Length)
            {
                if (!SeenShapeAnimationError.ContainsKey(SharedShape.FilePath))
                    Trace.TraceInformation("Ignored out of bounds matrix {1} in shape {0}", SharedShape.FilePath, iMatrix);
                SeenShapeAnimationError[SharedShape.FilePath] = true;
                return;  // mismatched matricies
            }

            var anim_node = SharedShape.Animations[0].anim_nodes[iMatrix];
            if (anim_node.controllers.Count == 0)
                    return;  // missing controllers

            // Start with the intial pose in the shape file.
            var xnaPose = SharedShape.Matrices[iMatrix];

            foreach (controller controller in anim_node.controllers)
            {
                // Determine the frame index from the current frame ('key'). We will be interpolating between two key
                // frames (the items in 'controller') so we need to find the last one LESS than the current frame
                // and interpolate with the one after it.
                var index = 0;
                for (var i = 0; i < controller.Count; i++)
                    if (controller[i].Frame <= key)
                        index = i;
                    else if (controller[i].Frame > key) // Optimisation, not required for algorithm.
                        break;

                //OR-Clock-hands Animation -------------------------------------------------------------------------------------------------------------
                var animName = anim_node.Name.ToLowerInvariant();
                if (animName.IndexOf("hand_clock") > -1)           //anim_node seems to be an OR-Clock-hand-matrix of an analog OR-Clock
                {
                    int gameTimeInSec = Convert.ToInt32((long)TimeSpan.FromSeconds(Viewer.Simulator.ClockTime).Ticks / 100000); //Game time as integer in milliseconds
                    int clockHour = gameTimeInSec / 360000 % 24;                          //HOUR of Game time
                    gameTimeInSec %= 360000;                                                //Game time by Modulo 360000 -> resultes minutes as rest
                    int clockMinute = gameTimeInSec / 6000;                                 //MINUTE of Game time
                    gameTimeInSec %= 6000;                                                  //Game time by Modulo 6000 -> resultes seconds as rest
                    int clockSecond = gameTimeInSec / 100;                                  //SECOND of Game time
                    int clockCenti = (gameTimeInSec - clockSecond * 100);                   //CENTI-SECOND of Game time
                    int clockQuadrant = 0;                                                  //Preset: Start with Anim-Control 0 (first quadrant of OR-Clock)
                    bool calculateClockHand = false;                                        //Preset: No drawing of a new matrix by default
                    float quadrantAmount = 1;                                               //Preset: Represents part of the way from position1 to position2 (float Value between 0 and 1)
                    if (animName.StartsWith("orts_chand_clock")) //Shape matrix is a CentiSecond Hand (continuous moved second hand) of an analog OR-clock
                    {
                        clockQuadrant = (int)clockSecond / 15;                              //Quadrant of the clock / Key-Index of anim_node (int Values: 0, 1, 2, 3)
                        quadrantAmount = (float)(clockSecond - (clockQuadrant * 15)) / 15;  //Seconds      Percentage quadrant related (float Value between 0 and 1) 
                        quadrantAmount += ((float)clockCenti / 100 / 15);                   //CentiSeconds Percentage quadrant related (float Value between 0 and 0.0666666)
                        if (controller.Count == 0 || clockQuadrant < 0 || clockQuadrant + 1 > controller.Count - 1)
                            clockQuadrant = 0;  //If controller.Count dosen't match
                        calculateClockHand = true;                                          //Calculate the new Hand position (Quaternion) below
                    }
                    else if (animName.StartsWith("orts_shand_clock")) //Shape matrix is a Second Hand of an analog OR-clock
                    {
                        clockQuadrant = (int)clockSecond / 15;                              //Quadrant of the clock / Key-Index of anim_node (int Values: 0, 1, 2, 3)
                        quadrantAmount = (float)(clockSecond - (clockQuadrant * 15)) / 15;  //Percentage quadrant related (float Value between 0 and 1) 
                        if (controller.Count == 0 || clockQuadrant < 0 || clockQuadrant + 1 > controller.Count - 1)
                            clockQuadrant = 0;  //If controller.Count doesn't match
                        calculateClockHand = true;                                          //Calculate the new Hand position (Quaternion) below
                    }
                    else if (animName.StartsWith("orts_mhand_clock")) //Shape matrix is a Minute Hand of an analog OR-clock
                    {
                        clockQuadrant = (int)clockMinute / 15;                              //Quadrant of the clock / Key-Index of anim_node (Values: 0, 1, 2, 3)
                        quadrantAmount = (float)(clockMinute - (clockQuadrant * 15)) / 15;  //Percentage quadrant related (Value between 0 and 1)
                        if (controller.Count == 0 || clockQuadrant < 0 || clockQuadrant + 1 > controller.Count - 1)
                            clockQuadrant = 0; //If controller.Count dosen't match
                        calculateClockHand = true;                                          //Calculate the new Hand position (Quaternion) below
                    }
                    else if (animName.StartsWith("orts_hhand_clock")) //Shape matrix is an Hour Hand of an analog OR-clock
                    {
                        clockHour %= 12;                                                    //Reduce 24 to 12 format
                        clockQuadrant = (int)clockHour / 3;                                 //Quadrant of the clock / Key-Index of anim_node (Values: 0, 1, 2, 3)
                        quadrantAmount = (float)(clockHour - (clockQuadrant * 3)) / 3;      //Percentage quadrant related (Value between 0 and 1)
                        quadrantAmount += (((float)1 / 3) * ((float)clockMinute / 60));     //add fine minute-percentage for Hour Hand between the full hours
                        if (controller.Count == 0 || clockQuadrant < 0 || clockQuadrant + 1 > controller.Count - 1)
                            clockQuadrant = 0; //If controller.Count doesn't match
                        calculateClockHand = true;                                          //Calculate the new Hand position (Quaternion) below
                    }
                    if (calculateClockHand == true & controller.Count > 0)                  //Calculate new Hand position as usual OR-style (Slerp-animation with Quaternions)
                    {
                        var position1 = controller[clockQuadrant];
                        var position2 = controller[clockQuadrant + 1];
                        if (position1 is slerp_rot sr1 && position2 is slerp_rot sr2)  //OR-Clock anim.node has slerp keys
                        {
                            Quaternion XNA1 = new Quaternion(sr1.X, sr1.Y, -sr1.Z, sr1.W);
                            Quaternion XNA2 = new Quaternion(sr2.X, sr2.Y, -sr2.Z, sr2.W);
                            Quaternion q = Quaternion.Slerp(XNA1, XNA2, quadrantAmount);
                            Vector3 location = xnaPose.Translation;
                            xnaPose = Matrix.CreateFromQuaternion(q);
                            xnaPose.Translation = location;
                        }
                        else if (position1 is linear_key lk1 && position2 is linear_key lk2) //OR-Clock anim.node has tcb keys
                        {
                            Vector3 XNA1 = new Vector3(lk1.X, lk1.Y, -lk1.Z);
                            Vector3 XNA2 = new Vector3(lk2.X, lk2.Y, -lk2.Z);
                            Vector3 v = Vector3.Lerp(XNA1, XNA2, quadrantAmount);
                            xnaPose.Translation = v;
                        }
                        else if (position1 is tcb_key tk1 && position2 is tcb_key tk2) //OR-Clock anim.node has tcb keys
                        {
                            Quaternion XNA1 = new Quaternion(tk1.X, tk1.Y, -tk1.Z, tk1.W);
                            Quaternion XNA2 = new Quaternion(tk2.X, tk2.Y, -tk2.Z, tk2.W);
                            Quaternion q = Quaternion.Slerp(XNA1, XNA2, quadrantAmount);
                            Vector3 location = xnaPose.Translation;
                            xnaPose = Matrix.CreateFromQuaternion(q);
                            xnaPose.Translation = location;
                        }
                    }
                }
            }
            XNAMatrices[iMatrix] = xnaPose;  // update the matrix
        }
    }

    public class SwitchTrackShape : PoseableShape
    {
        protected float AnimationKey;  // tracks position of points as they move left and right

        TrJunctionNode TrJunctionNode;  // has data on current aligment for the switch
        uint MainRoute;                  // 0 or 1 - which route is considered the main route

        public SwitchTrackShape(Viewer viewer, string path, WorldPosition position, TrJunctionNode trj)
            : base(viewer, path, position, ShapeFlags.AutoZBias)
        {
            TrJunctionNode = trj;
            TrackShape TS = viewer.Simulator.TSectionDat.TrackShapes.Get(TrJunctionNode.ShapeIndex);
            MainRoute = TS.MainRoute;
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            // ie, with 2 frames of animation, the key will advance from 0 to 1
            if (TrJunctionNode.SelectedRoute == MainRoute)
            {
                if (AnimationKey > 0.001) AnimationKey -= 0.002f * elapsedTime.ClockSeconds * 1000.0f;
                if (AnimationKey < 0.001) AnimationKey = 0;
            }
            else
            {
                if (AnimationKey < 0.999) AnimationKey += 0.002f * elapsedTime.ClockSeconds * 1000.0f;
                if (AnimationKey > 0.999) AnimationKey = 1.0f;
            }

            // Update the pose
            for (int iMatrix = 0; iMatrix < SharedShape.Matrices.Length; ++iMatrix)
                AnimateMatrix(iMatrix, AnimationKey);

            SharedShape.PrepareFrame(frame, Location, XNAMatrices, Flags);
        }
    }

    public class SpeedPostShape : PoseableShape
    {
        SpeedPostObj SpeedPostObj;  // has data on current aligment for the switch
        VertexPositionNormalTexture[] VertexList;
        int NumVertices;
        int NumIndices;
        public short[] TriangleListIndices;// Array of indices to vertices for triangles

        protected float AnimationKey;  // tracks position of points as they move left and right
        ShapePrimitive shapePrimitive;
        public SpeedPostShape(Viewer viewer, string path, WorldPosition position, SpeedPostObj spo)
            : base(viewer, path, position)
        {

            SpeedPostObj = spo;
            var maxVertex = SpeedPostObj.Sign_Shape.NumShapes * 48;// every face has max 7 digits, each has 2 triangles
            var material = viewer.MaterialManager.Load("Scenery", Helpers.GetRouteTextureFile(viewer.Simulator, Helpers.TextureFlags.None, SpeedPostObj.Speed_Digit_Tex), (int)(SceneryMaterialOptions.None | SceneryMaterialOptions.AlphaBlendingBlend), 0);

            // Create and populate a new ShapePrimitive
            NumVertices = NumIndices = 0;
            var i = 0; var id = -1; var size = SpeedPostObj.Text_Size.Size; var idlocation = 0;
            id = SpeedPostObj.GetTrItemID(idlocation);
            while (id >= 0)
            {
                SpeedPostItem item;
                string speed = "";
                try
                {
                    item = (SpeedPostItem)(viewer.Simulator.TDB.TrackDB.TrItemTable[id]);
                }
                catch
                {
                    throw;  // Error to be handled in Scenery.cs
                }

                //determine what to show: speed or number used in German routes
                if (item.ShowNumber)
                {
                    speed += item.DisplayNumber;
                    if (!item.ShowDot) speed.Replace(".", "");
                }
                else
                {
                    //determine if the speed is for passenger or freight
                    if (item.IsFreight == true && item.IsPassenger == false) speed += "F";
                    else if (item.IsFreight == false && item.IsPassenger == true) speed += "P";

                    if (item != null) speed += item.SpeedInd;
                }
                VertexList = new VertexPositionNormalTexture[maxVertex];
                TriangleListIndices = new short[maxVertex / 2 * 3]; // as is NumIndices

                for (i = 0; i < SpeedPostObj.Sign_Shape.NumShapes; i++)
                {
                    //start position is the center of the text
                    var start = new Vector3(SpeedPostObj.Sign_Shape.ShapesInfo[4 * i + 0], SpeedPostObj.Sign_Shape.ShapesInfo[4 * i + 1], SpeedPostObj.Sign_Shape.ShapesInfo[4 * i + 2]);
                    var rotation = SpeedPostObj.Sign_Shape.ShapesInfo[4 * i + 3];

                    //find the left-most of text
                    Vector3 offset;
                    if (Math.Abs(SpeedPostObj.Text_Size.DY) > 0.01) offset = new Vector3(0 - size / 2, 0, 0);
                    else offset = new Vector3(0, 0 - size / 2, 0);
                    offset.X -= speed.Length * SpeedPostObj.Text_Size.DX / 2;

                    offset.Y -= speed.Length * SpeedPostObj.Text_Size.DY / 2;

                    for (var j = 0; j < speed.Length; j++)
                    {
                        var tX = GetTextureCoordX(speed[j]); var tY = GetTextureCoordY(speed[j]);
                        var rot = Matrix.CreateRotationY(-rotation);

                        //the left-bottom vertex
                        Vector3 v = new Vector3(offset.X, offset.Y, 0.01f);
                        v = Vector3.Transform(v, rot);
                        v += start; Vertex v1 = new Vertex(v.X, v.Y, v.Z, 0, 0, -1, tX, tY);

                        //the right-bottom vertex
                        v.X = offset.X + size; v.Y = offset.Y; v.Z = 0.01f;
                        v = Vector3.Transform(v, rot);
                        v += start; Vertex v2 = new Vertex(v.X, v.Y, v.Z, 0, 0, -1, tX + 0.25f, tY);

                        //the right-top vertex
                        v.X = offset.X + size; v.Y = offset.Y + size; v.Z = 0.01f;
                        v = Vector3.Transform(v, rot);
                        v += start; Vertex v3 = new Vertex(v.X, v.Y, v.Z, 0, 0, -1, tX + 0.25f, tY - 0.25f);

                        //the left-top vertex
                        v.X = offset.X; v.Y = offset.Y + size; v.Z = 0.01f;
                        v = Vector3.Transform(v, rot);
                        v += start; Vertex v4 = new Vertex(v.X, v.Y, v.Z, 0, 0, -1, tX, tY - 0.25f);

                        //memory may not be enough
                        if (NumVertices > maxVertex - 4)
                        {
                            VertexPositionNormalTexture[] TempVertexList = new VertexPositionNormalTexture[maxVertex + 128];
                            short[] TempTriangleListIndices = new short[(maxVertex + 128) / 2 * 3]; // as is NumIndices
                            for (var k = 0; k < maxVertex; k++) TempVertexList[k] = VertexList[k];
                            for (var k = 0; k < maxVertex / 2 * 3; k++) TempTriangleListIndices[k] = TriangleListIndices[k];
                            TriangleListIndices = TempTriangleListIndices;
                            VertexList = TempVertexList;
                            maxVertex += 128;
                        }

                        //create first triangle
                        TriangleListIndices[NumIndices++] = (short)NumVertices;
                        TriangleListIndices[NumIndices++] = (short)(NumVertices + 2);
                        TriangleListIndices[NumIndices++] = (short)(NumVertices + 1);
                        // Second triangle:
                        TriangleListIndices[NumIndices++] = (short)NumVertices;
                        TriangleListIndices[NumIndices++] = (short)(NumVertices + 3);
                        TriangleListIndices[NumIndices++] = (short)(NumVertices + 2);

                        //create vertex
                        VertexList[NumVertices].Position = v1.Position; VertexList[NumVertices].Normal = v1.Normal; VertexList[NumVertices].TextureCoordinate = v1.TexCoord;
                        VertexList[NumVertices + 1].Position = v2.Position; VertexList[NumVertices + 1].Normal = v2.Normal; VertexList[NumVertices + 1].TextureCoordinate = v2.TexCoord;
                        VertexList[NumVertices + 2].Position = v3.Position; VertexList[NumVertices + 2].Normal = v3.Normal; VertexList[NumVertices + 2].TextureCoordinate = v3.TexCoord;
                        VertexList[NumVertices + 3].Position = v4.Position; VertexList[NumVertices + 3].Normal = v4.Normal; VertexList[NumVertices + 3].TextureCoordinate = v4.TexCoord;
                        NumVertices += 4;
                        offset.X += SpeedPostObj.Text_Size.DX; offset.Y += SpeedPostObj.Text_Size.DY; //move to next digit
                    }

                }
                idlocation++;
                id = SpeedPostObj.GetTrItemID(idlocation);
            }
            //create the shape primitive
            var newTList = new short[NumIndices];
            Array.Copy(TriangleListIndices, newTList, NumIndices);
            var newVList = new VertexPositionNormalTexture[NumVertices];
            Array.Copy(VertexList, newVList, NumVertices);
            IndexBuffer IndexBuffer = new IndexBuffer(viewer.GraphicsDevice, typeof(short),
                                                            NumIndices, BufferUsage.WriteOnly);
            IndexBuffer.SetData(newTList);
            shapePrimitive = new ShapePrimitive(material, new SharedShape.VertexBufferSet(newVList, viewer.GraphicsDevice), IndexBuffer, NumIndices / 3, new[] { -1 }, 0);

        }

        static float GetTextureCoordX(char c)
        {
            float x = (c - '0') % 4 * 0.25f;
            if (c == '.') x = 0;
            else if (c == 'P') x = 0.5f;
            else if (c == 'F') x = 0.75f;
            if (x < 0) x = 0;
            if (x > 1) x = 1;
            return x;
        }

        static float GetTextureCoordY(char c)
        {
            if (c == '0' || c == '1' || c == '2' || c == '3') return 0.25f;
            if (c == '4' || c == '5' || c == '6' || c == '7') return 0.5f;
            if (c == '8' || c == '9' || c == 'P' || c == 'F') return 0.75f;
            return 1.0f;
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            // Offset relative to the camera-tile origin
            int dTileX = this.Location.TileX - Viewer.Camera.TileX;
            int dTileZ = this.Location.TileZ - Viewer.Camera.TileZ;
            Vector3 tileOffsetWrtCamera = new Vector3(dTileX * 2048, 0, -dTileZ * 2048);

            // Initialize xnaXfmWrtCamTile to object-tile to camera-tile translation:
            Matrix xnaXfmWrtCamTile = Matrix.CreateTranslation(tileOffsetWrtCamera);
            xnaXfmWrtCamTile = this.Location.XNAMatrix * xnaXfmWrtCamTile; // Catenate to world transformation
            // (Transformation is now with respect to camera-tile origin)

            // TODO: Make this use AddAutoPrimitive instead.
            frame.AddPrimitive(this.shapePrimitive.Material, this.shapePrimitive, RenderPrimitiveGroup.World, ref xnaXfmWrtCamTile, ShapeFlags.None);

            // if there is no animation, that's normal and so no animation missing error is displayed
            if (SharedShape.Animations == null || SharedShape.Animations.Count == 0)
            {
                if (!SeenShapeAnimationError.ContainsKey(SharedShape.FilePath))
                    SeenShapeAnimationError[SharedShape.FilePath] = true;
            }
            // Update the pose
            for (int iMatrix = 0; iMatrix < SharedShape.Matrices.Length; ++iMatrix)
                AnimateMatrix(iMatrix, AnimationKey);

            SharedShape.PrepareFrame(frame, Location, XNAMatrices, Flags);
        }

        internal override void Mark()
        {
            shapePrimitive.Mark();
            base.Mark();
        }
    } // class SpeedPostShape

    public class LevelCrossingShape : PoseableShape
    {
        readonly LevelCrossingObj CrossingObj;
        readonly SoundSource Sound;
        readonly LevelCrossing Crossing;

        readonly float AnimationFrames;
        readonly float AnimationSpeed;
        bool Opening = true;
        float AnimationKey;

        public LevelCrossingShape(Viewer viewer, string path, WorldPosition position, ShapeFlags shapeFlags, LevelCrossingObj crossingObj)
            : base(viewer, path, position, shapeFlags)
        {
            CrossingObj = crossingObj;
            if (!CrossingObj.silent)
            {
                var soundFileName = "";
                if (CrossingObj.SoundFileName != "") soundFileName = CrossingObj.SoundFileName;
                else if (SharedShape.SoundFileName != "") soundFileName = SharedShape.SoundFileName;
                else if (viewer.Simulator.TRK.Tr_RouteFile.DefaultCrossingSMS != null) soundFileName = viewer.Simulator.TRK.Tr_RouteFile.DefaultCrossingSMS;
                if (soundFileName != "")
                {
                    var soundPath = viewer.Simulator.RoutePath + @"\\sound\\" + soundFileName;
                    try
                    {
                        Sound = new SoundSource(viewer, position.WorldLocation, Events.Source.MSTSCrossing, soundPath);
                        viewer.SoundProcess.AddSoundSources(this, new List<SoundSourceBase>() { Sound });
                    }
                    catch
                    {
                        soundPath = viewer.Simulator.BasePath + @"\\sound\\" + soundFileName;
                        try
                        {
                            Sound = new SoundSource(viewer, position.WorldLocation, Events.Source.MSTSCrossing, soundPath);
                            viewer.SoundProcess.AddSoundSources(this, new List<SoundSourceBase>() { Sound });
                        }
                        catch (Exception error)
                        {
                            Trace.WriteLine(new FileLoadException(soundPath, error));
                        }
                    }
                }
            }
            Crossing = viewer.Simulator.LevelCrossings.CreateLevelCrossing(
                position,
                from tid in CrossingObj.trItemIDList where tid.db == 0 select tid.dbID,
                from tid in CrossingObj.trItemIDList where tid.db == 1 select tid.dbID,
                CrossingObj.levelCrParameters.warningTime,
                CrossingObj.levelCrParameters.minimumDistance);
            // If there are no animations, we leave the frame count and speed at 0 and nothing will try to animate.
            if (SharedShape.Animations != null && SharedShape.Animations.Count > 0)
            {
                // LOOPED COSSINGS (animTiming < 0)
                //     MSTS plays through all the frames of the animation for "closed" and sits on frame 0 for "open". The
                //     speed of animation is the normal speed (frame rate at 30FPS) scaled by the timing value. Since the
                //     timing value is negative, the animation actually plays in reverse.
                // NON-LOOPED CROSSINGS (animTiming > 0)
                //     MSTS plays through the first 1.0 seconds of the animation forwards for closing and backwards for
                //     opening. The number of frames defined doesn't matter; the animation is limited by time so the frame
                //     rate (based on 30FPS) is what's needed.
                AnimationFrames = CrossingObj.levelCrTiming.animTiming < 0 ? SharedShape.Animations[0].FrameCount : SharedShape.Animations[0].FrameRate / 30f;
                AnimationSpeed = SharedShape.Animations[0].FrameRate / 30f / CrossingObj.levelCrTiming.animTiming;
            }
        }

        public override void Unload()
        {
            if (Sound != null)
            {
                Viewer.SoundProcess.RemoveSoundSources(this);
                Sound.Dispose();
            }
            base.Unload();
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (CrossingObj.visible != true)
                return;

            if (Opening == Crossing.HasTrain)
            {
                Opening = !Crossing.HasTrain;
                if (Sound != null) Sound.HandleEvent(Opening ? Event.CrossingOpening : Event.CrossingClosing);
            }

            if (Opening)
                AnimationKey -= elapsedTime.ClockSeconds * AnimationSpeed;
            else
                AnimationKey += elapsedTime.ClockSeconds * AnimationSpeed;

            if (CrossingObj.levelCrTiming.animTiming < 0)
            {
                // Stick to frame 0 for "open" and loop for "closed".
                if (Opening) AnimationKey = 0;
                if (AnimationKey < 0) AnimationKey += AnimationFrames;
            }
            if (AnimationKey < 0) AnimationKey = 0;
            if (AnimationKey > AnimationFrames) AnimationKey = AnimationFrames;

            for (var i = 0; i < SharedShape.Matrices.Length; ++i)
                AnimateMatrix(i, AnimationKey);

            SharedShape.PrepareFrame(frame, Location, XNAMatrices, Flags);
        }
    }

    public class HazzardShape : PoseableShape
    {
        readonly HazardObj HazardObj;
        readonly Hazzard Hazzard;

        readonly int AnimationFrames;
        float Moved = 0f;
        float AnimationKey;
        float DelayHazAnimation;

        public static HazzardShape CreateHazzard(Viewer viewer, string path, WorldPosition position, ShapeFlags shapeFlags, HazardObj hObj)
        {
            var h = viewer.Simulator.HazzardManager.AddHazzardIntoGame(hObj.itemId, hObj.FileName);
            if (h == null) return null;
            return new HazzardShape(viewer, viewer.Simulator.BasePath + @"\Global\Shapes\" + h.HazFile.Tr_HazardFile.FileName + "\0" + viewer.Simulator.BasePath + @"\Global\Textures", position, shapeFlags, hObj, h);

        }

        public HazzardShape(Viewer viewer, string path, WorldPosition position, ShapeFlags shapeFlags, HazardObj hObj, Hazzard h)
            : base(viewer, path, position, shapeFlags)
        {
            HazardObj = hObj;
            Hazzard = h;
            AnimationFrames = SharedShape.Animations[0].FrameCount;
        }

        public override void Unload()
        {
            Viewer.Simulator.HazzardManager.RemoveHazzardFromGame(HazardObj.itemId);
            base.Unload();
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (Hazzard == null) return;
            Vector2 CurrentRange;
            AnimationKey += elapsedTime.ClockSeconds * 24f;
            DelayHazAnimation += elapsedTime.ClockSeconds;
            switch (Hazzard.state)
            {
                case Hazzard.State.Idle1:
                    CurrentRange = Hazzard.HazFile.Tr_HazardFile.Idle_Key; break;
                case Hazzard.State.Idle2:
                    CurrentRange = Hazzard.HazFile.Tr_HazardFile.Idle_Key2; break;
                case Hazzard.State.LookLeft:
                    CurrentRange = Hazzard.HazFile.Tr_HazardFile.Surprise_Key_Left; break;
                case Hazzard.State.LookRight:
                    CurrentRange = Hazzard.HazFile.Tr_HazardFile.Surprise_Key_Right; break;
                case Hazzard.State.Scared:
                default:
                    CurrentRange = Hazzard.HazFile.Tr_HazardFile.Success_Scarper_Key;
                    if (Moved < Hazzard.HazFile.Tr_HazardFile.Distance)
                    {
                        var m = Hazzard.HazFile.Tr_HazardFile.Speed * elapsedTime.ClockSeconds;
                        Moved += m;
                        this.HazardObj.Position.Move(this.HazardObj.QDirection, m);
                        Location.Location = new Vector3(this.HazardObj.Position.X, this.HazardObj.Position.Y, this.HazardObj.Position.Z);
                    }
                    else { Moved = 0; Hazzard.state = Hazzard.State.Idle1; }
                    break;
            }

            if (Hazzard.state == Hazzard.State.Idle1 || Hazzard.state == Hazzard.State.Idle2)
            {
                if (DelayHazAnimation > 5f)
                {
                    if (AnimationKey < CurrentRange.X)
                    {
                        AnimationKey = CurrentRange.X;
                        DelayHazAnimation = 0;
                    }

                    if (AnimationKey > CurrentRange.Y)
                    {
                        AnimationKey = CurrentRange.X;
                        DelayHazAnimation = 0;
                    }
                }
            }

            if (Hazzard.state == Hazzard.State.LookLeft || Hazzard.state == Hazzard.State.LookRight)
            {
                if (AnimationKey < CurrentRange.X) AnimationKey = CurrentRange.X;
                if (AnimationKey > CurrentRange.Y) AnimationKey = CurrentRange.Y;
            }

            if (Hazzard.state == Hazzard.State.Scared)
            {
                if (AnimationKey < CurrentRange.X) AnimationKey = CurrentRange.X;

                if (AnimationKey > CurrentRange.Y) AnimationKey = CurrentRange.X;
            }

            for (var i = 0; i < SharedShape.Matrices.Length; ++i)
                AnimateMatrix(i, AnimationKey);

            //var pos = this.HazardObj.Position;

            SharedShape.PrepareFrame(frame, Location, XNAMatrices, Flags);
        }
    }

    public class FuelPickupItemShape : PoseableShape
    {
        protected PickupObj FuelPickupItemObj;
        protected FuelPickupItem FuelPickupItem;
        protected SoundSource Sound;
        protected float FrameRate;
        protected WorldPosition Position;

        protected int AnimationFrames;
        protected float AnimationKey;

        public FuelPickupItemShape(Viewer viewer, string path, WorldPosition position, ShapeFlags shapeFlags, PickupObj fuelpickupitemObj)
            : base(viewer, path, position, shapeFlags)
        {
            FuelPickupItemObj = fuelpickupitemObj;
            Position = position;
            Initialize();
        }

        public virtual void Initialize()
        {
            if (Viewer.Simulator.TRK.Tr_RouteFile.DefaultDieselTowerSMS != null && FuelPickupItemObj.PickupType == 7) // Testing for Diesel PickupType
            {
                var soundPath = Viewer.Simulator.RoutePath + @"\\sound\\" + Viewer.Simulator.TRK.Tr_RouteFile.DefaultDieselTowerSMS;
                try
                {
                    Sound = new SoundSource(Viewer, Position.WorldLocation, Events.Source.MSTSFuelTower, soundPath);
                    Viewer.SoundProcess.AddSoundSources(this, new List<SoundSourceBase>() { Sound });
                }
                catch
                {
                    soundPath = Viewer.Simulator.BasePath + @"\\sound\\" + Viewer.Simulator.TRK.Tr_RouteFile.DefaultDieselTowerSMS;
                    try
                    {
                        Sound = new SoundSource(Viewer, Position.WorldLocation, Events.Source.MSTSFuelTower, soundPath);
                        Viewer.SoundProcess.AddSoundSources(this, new List<SoundSourceBase>() { Sound });
                    }
                    catch (Exception error)
                    {
                        Trace.WriteLine(new FileLoadException(soundPath, error));
                    }
                }
            }
            if (Viewer.Simulator.TRK.Tr_RouteFile.DefaultWaterTowerSMS != null && FuelPickupItemObj.PickupType == 5) // Testing for Water PickupType
            {
                var soundPath = Viewer.Simulator.RoutePath + @"\\sound\\" + Viewer.Simulator.TRK.Tr_RouteFile.DefaultWaterTowerSMS;
                try
                {
                    Sound = new SoundSource(Viewer, Position.WorldLocation, Events.Source.MSTSFuelTower, soundPath);
                    Viewer.SoundProcess.AddSoundSources(this, new List<SoundSourceBase>() { Sound });
                }
                catch
                {
                    soundPath = Viewer.Simulator.BasePath + @"\\sound\\" + Viewer.Simulator.TRK.Tr_RouteFile.DefaultWaterTowerSMS;
                    try
                    {
                        Sound = new SoundSource(Viewer, Position.WorldLocation, Events.Source.MSTSFuelTower, soundPath);
                        Viewer.SoundProcess.AddSoundSources(this, new List<SoundSourceBase>() { Sound });
                    }
                    catch (Exception error)
                    {
                        Trace.WriteLine(new FileLoadException(soundPath, error));
                    }
                }
            }
            if (Viewer.Simulator.TRK.Tr_RouteFile.DefaultCoalTowerSMS != null && (FuelPickupItemObj.PickupType == 6 || FuelPickupItemObj.PickupType == 2))
            {
                var soundPath = Viewer.Simulator.RoutePath + @"\\sound\\" + Viewer.Simulator.TRK.Tr_RouteFile.DefaultCoalTowerSMS;
                try
                {
                    Sound = new SoundSource(Viewer, Position.WorldLocation, Events.Source.MSTSFuelTower, soundPath);
                    Viewer.SoundProcess.AddSoundSources(this, new List<SoundSourceBase>() { Sound });
                }
                catch
                {
                    soundPath = Viewer.Simulator.BasePath + @"\\sound\\" + Viewer.Simulator.TRK.Tr_RouteFile.DefaultCoalTowerSMS;
                    try
                    {
                        Sound = new SoundSource(Viewer, Position.WorldLocation, Events.Source.MSTSFuelTower, soundPath);
                        Viewer.SoundProcess.AddSoundSources(this, new List<SoundSourceBase>() { Sound });
                    }
                    catch (Exception error)
                    {
                        Trace.WriteLine(new FileLoadException(soundPath, error));
                    }
                }
            }
            FuelPickupItem = Viewer.Simulator.FuelManager.CreateFuelStation(Position, from tid in FuelPickupItemObj.TrItemIDList where tid.db == 0 select tid.dbID);
            AnimationFrames = 1;
            FrameRate = 1;
            if (SharedShape.Animations != null && SharedShape.Animations.Count > 0 && SharedShape.Animations[0].anim_nodes != null && SharedShape.Animations[0].anim_nodes.Count > 0)
            {
                FrameRate = SharedShape.Animations[0].FrameCount / FuelPickupItemObj.PickupAnimData.AnimationSpeed;
                foreach (var anim_node in SharedShape.Animations[0].anim_nodes)
                    if (anim_node.Name == "ANIMATED_PARTS")
                    {
                        AnimationFrames = SharedShape.Animations[0].FrameCount;
                        break;
                    }
            }
        }

        public override void Unload()
        {
            if (Sound != null)
            {
                Viewer.SoundProcess.RemoveSoundSources(this);
                Sound.Dispose();
            }
            base.Unload();
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {

            // 0 can be used as a setting for instant animation.
            if (FuelPickupItem.ReFill() && FuelPickupItemObj.UID == MSTSWagon.RefillProcess.ActivePickupObjectUID)
            {
                if (AnimationKey == 0 && Sound != null) Sound.HandleEvent(Event.FuelTowerDown);
                if (FuelPickupItemObj.PickupAnimData.AnimationSpeed == 0) AnimationKey = 1.0f;
                else if (AnimationKey < AnimationFrames)
                    AnimationKey += elapsedTime.ClockSeconds * FrameRate;
            }

            if (!FuelPickupItem.ReFill() && AnimationKey > 0)
            {
                if (AnimationKey == AnimationFrames && Sound != null)
                {
                    Sound.HandleEvent(Event.FuelTowerTransferEnd);
                    Sound.HandleEvent(Event.FuelTowerUp);
                }
                AnimationKey -= elapsedTime.ClockSeconds * FrameRate;
            }

            if (AnimationKey < 0)
            {
                AnimationKey = 0;
            }
            if (AnimationKey > AnimationFrames)
            {
                AnimationKey = AnimationFrames;
                if (Sound != null) Sound.HandleEvent(Event.FuelTowerTransferStart);
            }

            for (var i = 0; i < SharedShape.Matrices.Length; ++i)
                AnimateMatrix(i, AnimationKey);

            SharedShape.PrepareFrame(frame, Location, XNAMatrices, Flags);
        }
    } // End Class FuelPickupItemShape

    public class ContainerHandlingItemShape : FuelPickupItemShape
    {
        protected float AnimationKeyX;
        protected float AnimationKeyY;
        protected float AnimationKeyZ;
        protected float AnimationKeyGrabber01;
        protected float AnimationKeyGrabber02;
        protected int IAnimationMatrixX;
        protected int IAnimationMatrixY;
        protected int IAnimationMatrixZ;
        protected int IGrabber01;
        protected int IGrabber02;
        protected controller controllerX;
        protected controller controllerY;
        protected controller controllerZ;
        protected controller controllerGrabber01;
        protected controller controllerGrabber02;
        protected float slowDownThreshold = 0.03f;
        // To detect transitions that trigger sounds
        protected bool OldMoveX;
        protected bool OldMoveY;
        protected bool OldMoveZ;


        protected ContainerHandlingItem ContainerHandlingItem;
        public ContainerHandlingItemShape(Viewer viewer, string path, WorldPosition position, ShapeFlags shapeFlags, PickupObj fuelpickupitemObj)
                        : base(viewer, path, position, shapeFlags, fuelpickupitemObj)
        {
        }

        public override void Initialize()
        {
            for (var imatrix = 0; imatrix < SharedShape.Matrices.Length; ++imatrix)
            {
                if (SharedShape.MatrixNames[imatrix].ToLower() == "zaxis")
                    IAnimationMatrixZ = imatrix;
                else if (SharedShape.MatrixNames[imatrix].ToLower() == "xaxis")
                    IAnimationMatrixX = imatrix;
                else if (SharedShape.MatrixNames[imatrix].ToLower() == "yaxis")
                    IAnimationMatrixY = imatrix;
                else if (SharedShape.MatrixNames[imatrix].ToLower() == "grabber01")
                    IGrabber01 = imatrix;
                else if (SharedShape.MatrixNames[imatrix].ToLower() == "grabber02")
                    IGrabber02 = imatrix;
            }

            controllerX = SharedShape.Animations[0].anim_nodes[IAnimationMatrixX].controllers[0];
            controllerY = SharedShape.Animations[0].anim_nodes[IAnimationMatrixY].controllers[0];
            controllerZ = SharedShape.Animations[0].anim_nodes[IAnimationMatrixZ].controllers[0];
            controllerGrabber01 = SharedShape.Animations[0].anim_nodes[IGrabber01].controllers[0];
            controllerGrabber02 = SharedShape.Animations[0].anim_nodes[IGrabber02].controllers[0];
            AnimationKeyX = Math.Abs((0 - ((linear_key)controllerX[0]).X) / (((linear_key)controllerX[1]).X - ((linear_key)controllerX[0]).X)) * controllerX[1].Frame;
            AnimationKeyY = Math.Abs((0 - ((linear_key)controllerY[0]).Y) / (((linear_key)controllerY[1]).Y - ((linear_key)controllerY[0]).Y)) * controllerY[1].Frame;
            AnimationKeyZ = Math.Abs((0 - ((linear_key)controllerZ[0]).Z) / (((linear_key)controllerZ[1]).Z - ((linear_key)controllerZ[0]).Z)) * controllerZ[1].Frame;
            if (FuelPickupItemObj.CraneSound != null)
            {
                var soundPath = Viewer.Simulator.RoutePath + @"\\sound\\" + FuelPickupItemObj.CraneSound;
                try
                {
                    Sound = new SoundSource(Viewer, Position.WorldLocation, Events.Source.ORTSContainerCrane, soundPath);
                    Viewer.SoundProcess.AddSoundSources(this, new List<SoundSourceBase>() { Sound });
                }
                catch
                {
                    soundPath = Viewer.Simulator.BasePath + @"\\sound\\containercrane.sms";
                    try
                    {
                        Sound = new SoundSource(Viewer, Position.WorldLocation, Events.Source.ORTSContainerCrane, soundPath);
                        Viewer.SoundProcess.AddSoundSources(this, new List<SoundSourceBase>() { Sound });
                    }
                    catch
                    {
                        Trace.TraceWarning("Cannot find sound file {0}", soundPath);
                    }
                }
            }
            else
            {
                var soundPath = Viewer.Simulator.BasePath + @"\\sound\\containercrane.sms";
                try
                {
                    Sound = new SoundSource(Viewer, Position.WorldLocation, Events.Source.ORTSContainerCrane, soundPath);
                    Viewer.SoundProcess.AddSoundSources(this, new List<SoundSourceBase>() { Sound });
                }
                catch
                {
                    Trace.TraceWarning("Cannot find sound file {0}", soundPath);
                }
            }
            ContainerHandlingItem = Viewer.Simulator.ContainerManager.ContainerHandlingItems[FuelPickupItemObj.TrItemIDList[0].dbID];
            AnimationFrames = 1;
            FrameRate = 1;
            if (SharedShape.Animations != null && SharedShape.Animations.Count > 0 && SharedShape.Animations[0].anim_nodes != null && SharedShape.Animations[0].anim_nodes.Count > 0)
            {
                FrameRate = SharedShape.Animations[0].FrameCount / FuelPickupItemObj.PickupAnimData.AnimationSpeed;
                foreach (var anim_node in SharedShape.Animations[0].anim_nodes)
                    if (anim_node.Name == "ANIMATED_PARTS")
                    {
                        AnimationFrames = SharedShape.Animations[0].FrameCount;
                        break;
                    }
            }
            AnimateOneMatrix(IAnimationMatrixX, AnimationKeyX);
            AnimateOneMatrix(IAnimationMatrixY, AnimationKeyY);
            AnimateOneMatrix(IAnimationMatrixZ, AnimationKeyZ);

            var absAnimationMatrix = XNAMatrices[IAnimationMatrixY];
            Matrix.Multiply(ref absAnimationMatrix, ref XNAMatrices[IAnimationMatrixX], out absAnimationMatrix);
            Matrix.Multiply(ref absAnimationMatrix, ref XNAMatrices[IAnimationMatrixZ], out absAnimationMatrix);
            Matrix.Multiply(ref absAnimationMatrix, ref Location.XNAMatrix, out absAnimationMatrix);
            ContainerHandlingItem.PassSpanParameters(((linear_key)controllerZ[0]).Z, ((linear_key)controllerZ[1]).Z,
                ((linear_key)controllerGrabber01[0]).Z - ((linear_key)controllerGrabber01[1]).Z, ((linear_key)controllerGrabber02[0]).Z - ((linear_key)controllerGrabber02[1]).Z);
            ContainerHandlingItem.ReInitPositionOffset(absAnimationMatrix);

            AnimationKeyX = Math.Abs((ContainerHandlingItem.PickingSurfaceRelativeTopStartPosition.X - ((linear_key)controllerX[0]).X) / (((linear_key)controllerX[1]).X - ((linear_key)controllerX[0]).X)) * controllerX[1].Frame;
            AnimationKeyY = Math.Abs((ContainerHandlingItem.PickingSurfaceRelativeTopStartPosition.Y - ((linear_key)controllerY[0]).Y) / (((linear_key)controllerY[1]).Y - ((linear_key)controllerY[0]).Y)) * controllerY[1].Frame;
            AnimationKeyZ = Math.Abs((ContainerHandlingItem.PickingSurfaceRelativeTopStartPosition.Z - ((linear_key)controllerZ[0]).Z) / (((linear_key)controllerZ[1]).Z - ((linear_key)controllerZ[0]).Z)) * controllerZ[1].Frame;
            AnimateOneMatrix(IAnimationMatrixX, AnimationKeyX);
            AnimateOneMatrix(IAnimationMatrixY, AnimationKeyY);
            AnimateOneMatrix(IAnimationMatrixZ, AnimationKeyZ);
            for (var imatrix = 0; imatrix < SharedShape.Matrices.Length; ++imatrix)
            {
                if (SharedShape.MatrixNames[imatrix].ToLower().StartsWith("cable"))
                    AnimateOneMatrix(imatrix, AnimationKeyY);
                if (SharedShape.MatrixNames[imatrix].ToLower().StartsWith("grabber"))
                    AnimateOneMatrix(imatrix, 0);
            }
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {

            // 0 can be used as a setting for instant animation.
            /*           if (ContainerHandlingItem.ReFill() && FuelPickupItemObj.UID == MSTSWagon.RefillProcess.ActivePickupObjectUID)
                       {
                           if (AnimationKey == 0 && Sound != null) Sound.HandleEvent(Event.FuelTowerDown);
                           if (FuelPickupItemObj.PickupAnimData.AnimationSpeed == 0) AnimationKey = 1.0f;
                           else if (AnimationKey < AnimationFrames)
                               AnimationKey += elapsedTime.ClockSeconds * FrameRate;
                       }

                       if (!ContainerHandlingItem.ReFill() && AnimationKey > 0)
                       {
                           if (AnimationKey == AnimationFrames && Sound != null)
                           {
                               Sound.HandleEvent(Event.FuelTowerTransferEnd);
                               Sound.HandleEvent(Event.FuelTowerUp);
                           }
                           AnimationKey -= elapsedTime.ClockSeconds * FrameRate;
                       }

                       if (AnimationKey < 0)
                       {
                           AnimationKey = 0;
                       }
                       if (AnimationKey > AnimationFrames)
                       {
                           AnimationKey = AnimationFrames;
                           if (Sound != null) Sound.HandleEvent(Event.FuelTowerTransferStart);
                       }

                       for (var i = 0; i < SharedShape.Matrices.Length; ++i)
                           AnimateMatrix(i, AnimationKey);
            */
            if (FuelPickupItemObj.UID == MSTSWagon.RefillProcess.ActivePickupObjectUID)
            {
                float tempFrameRate;
                if (ContainerHandlingItem.MoveX)
                {
                    var animationTarget = Math.Abs((ContainerHandlingItem.TargetX - ((linear_key)controllerX[0]).X) / (((linear_key)controllerX[1]).X - ((linear_key)controllerX[0]).X)) * controllerX[1].Frame;
                    //                    if (AnimationKey == 0 && Sound != null) Sound.HandleEvent(Event.FuelTowerDown);
                    tempFrameRate = Math.Abs(AnimationKeyX - animationTarget) > slowDownThreshold ? FrameRate : FrameRate / 4;
                    if (AnimationKeyX < animationTarget)
                    {
                        AnimationKeyX += elapsedTime.ClockSeconds * tempFrameRate;
                        // don't oscillate!
                        if (AnimationKeyX >= animationTarget)
                        {
                            AnimationKeyX = animationTarget;
                            ContainerHandlingItem.MoveX = false;
                        }
                    }
                    else if (AnimationKeyX > animationTarget)
                    {
                        AnimationKeyX -= elapsedTime.ClockSeconds * tempFrameRate;
                        if (AnimationKeyX <= animationTarget)
                        {
                            AnimationKeyX = animationTarget;
                            ContainerHandlingItem.MoveX = false;
                        }
                    }
                    else
                        ContainerHandlingItem.MoveX = false;
                    if (AnimationKeyX < 0)
                        AnimationKeyX = 0;
                }

                if (ContainerHandlingItem.MoveY)
                {
                    var animationTarget = Math.Abs((ContainerHandlingItem.TargetY - ((linear_key)controllerY[0]).Y) / (((linear_key)controllerY[1]).Y - ((linear_key)controllerY[0]).Y)) * controllerY[1].Frame;
                    tempFrameRate = Math.Abs(AnimationKeyY - animationTarget) > slowDownThreshold ? FrameRate : FrameRate / 4;
                    if (AnimationKeyY < animationTarget)
                    {
                        AnimationKeyY += elapsedTime.ClockSeconds * tempFrameRate;
                        if (AnimationKeyY >= animationTarget)
                        {
                            AnimationKeyY = animationTarget;
                            ContainerHandlingItem.MoveY = false;
                        }
                    }
                    else if (AnimationKeyY > animationTarget)
                    {
                        AnimationKeyY -= elapsedTime.ClockSeconds * tempFrameRate;
                        if (AnimationKeyY <= animationTarget)
                        {
                            AnimationKeyY = animationTarget;
                            ContainerHandlingItem.MoveY = false;
                        }
                    }
                    else
                        ContainerHandlingItem.MoveY = false;
                    if (AnimationKeyY < 0)
                        AnimationKeyY = 0;
                }

                if (ContainerHandlingItem.MoveZ)
                {
                    var animationTarget = Math.Abs((ContainerHandlingItem.TargetZ - ((linear_key)controllerZ[0]).Z) / (((linear_key)controllerZ[1]).Z - ((linear_key)controllerZ[0]).Z)) * controllerZ[1].Frame;
                    tempFrameRate = Math.Abs(AnimationKeyZ - animationTarget) > slowDownThreshold ? FrameRate : FrameRate / 4;
                    if (AnimationKeyZ < animationTarget)
                    {
                        AnimationKeyZ += elapsedTime.ClockSeconds * tempFrameRate;
                        if (AnimationKeyZ >= animationTarget)
                        {
                            AnimationKeyZ = animationTarget;
                            ContainerHandlingItem.MoveZ = false;
                        }
                    }
                    else if (AnimationKeyZ > animationTarget)
                    {
                        AnimationKeyZ -= elapsedTime.ClockSeconds * tempFrameRate;
                        if (AnimationKeyZ <= animationTarget)
                        {
                            AnimationKeyZ = animationTarget;
                            ContainerHandlingItem.MoveZ = false;
                        }
                    }
                    else
                        ContainerHandlingItem.MoveZ = false;
                    if (AnimationKeyZ < 0)
                        AnimationKeyZ = 0;
                }

                if (ContainerHandlingItem.MoveGrabber)
                {
                    var animationTarget = Math.Abs((ContainerHandlingItem.TargetGrabber01 - ((linear_key)controllerGrabber01[0]).Z + ((linear_key)controllerGrabber01[1]).Z) / (((linear_key)controllerGrabber01[1]).Z - ((linear_key)controllerGrabber01[0]).Z)) * controllerGrabber01[1].Frame;
                    tempFrameRate = Math.Abs(AnimationKeyGrabber01 - animationTarget) > slowDownThreshold ? FrameRate : FrameRate / 4;
                    if (AnimationKeyGrabber01 < animationTarget)
                    {
                        AnimationKeyGrabber01 += elapsedTime.ClockSeconds * tempFrameRate;
                        if (AnimationKeyGrabber01 >= animationTarget)
                        {
                            AnimationKeyGrabber01 = animationTarget;
                        }
                    }
                    else if (AnimationKeyGrabber01 > animationTarget)
                    {
                        AnimationKeyGrabber01 -= elapsedTime.ClockSeconds * tempFrameRate;
                        if (AnimationKeyGrabber01 <= animationTarget)
                        {
                            AnimationKeyGrabber01 = animationTarget;
                        }
                    }
                    if (AnimationKeyGrabber01 < 0)
                        AnimationKeyGrabber01 = 0;
                    var animationTarget2 = Math.Abs((ContainerHandlingItem.TargetGrabber02 - ((linear_key)controllerGrabber02[0]).Z + ((linear_key)controllerGrabber02[1]).Z) / (((linear_key)controllerGrabber02[1]).Z - ((linear_key)controllerGrabber02[0]).Z)) * controllerGrabber02[1].Frame;
                    tempFrameRate = Math.Abs(AnimationKeyGrabber01 - animationTarget2) > slowDownThreshold ? FrameRate : FrameRate / 4;
                    if (AnimationKeyGrabber02 < animationTarget2)
                    {
                        AnimationKeyGrabber02 += elapsedTime.ClockSeconds * tempFrameRate;
                        if (AnimationKeyGrabber02 >= animationTarget2)
                        {
                            AnimationKeyGrabber02 = animationTarget2;
                        }
                    }
                    else if (AnimationKeyGrabber02 > animationTarget2)
                    {
                        AnimationKeyGrabber02 -= elapsedTime.ClockSeconds * tempFrameRate;
                        if (AnimationKeyGrabber02 <= animationTarget2)
                        {
                            AnimationKeyGrabber02 = animationTarget2;
                        }
                    }
                    if (animationTarget == AnimationKeyGrabber01 && animationTarget2 == AnimationKeyGrabber02)
                        ContainerHandlingItem.MoveGrabber = false;
                    if (AnimationKeyGrabber02 < 0)
                        AnimationKeyGrabber02 = 0;
                }
            }
            ContainerHandlingItem.ActualX = (((linear_key)controllerX[1]).X - ((linear_key)controllerX[0]).X) * AnimationKeyX / controllerX[1].Frame + ((linear_key)controllerX[0]).X;
            ContainerHandlingItem.ActualY = (((linear_key)controllerY[1]).Y - ((linear_key)controllerY[0]).Y) * AnimationKeyY / controllerY[1].Frame + ((linear_key)controllerY[0]).Y;
            ContainerHandlingItem.ActualZ = (((linear_key)controllerZ[1]).Z - ((linear_key)controllerZ[0]).Z) * AnimationKeyZ / controllerZ[1].Frame + ((linear_key)controllerZ[0]).Z;
            ContainerHandlingItem.ActualGrabber01 = (((linear_key)controllerGrabber01[1]).Z - ((linear_key)controllerGrabber01[0]).Z) * AnimationKeyGrabber01 / controllerGrabber01[1].Frame + ((linear_key)controllerGrabber01[0]).Z;
            ContainerHandlingItem.ActualGrabber02 = (((linear_key)controllerGrabber02[1]).Z - ((linear_key)controllerGrabber02[0]).Z) * AnimationKeyGrabber02 / controllerGrabber02[1].Frame + ((linear_key)controllerGrabber02[0]).Z;

            AnimateOneMatrix(IAnimationMatrixX, AnimationKeyX);
            AnimateOneMatrix(IAnimationMatrixY, AnimationKeyY);
            AnimateOneMatrix(IAnimationMatrixZ, AnimationKeyZ);
            for (var imatrix = 0; imatrix < SharedShape.Matrices.Length; ++imatrix)
            {
                if (SharedShape.MatrixNames[imatrix].ToLower().StartsWith("cable"))
                    AnimateOneMatrix(imatrix, AnimationKeyY);
                else if (SharedShape.MatrixNames[imatrix].ToLower().StartsWith("grabber01"))
                    AnimateOneMatrix(imatrix, AnimationKeyGrabber01);
                else if (SharedShape.MatrixNames[imatrix].ToLower().StartsWith("grabber02"))
                    AnimateOneMatrix(imatrix, AnimationKeyGrabber02);
            }

            SharedShape.PrepareFrame(frame, Location, XNAMatrices, Flags);
            if (ContainerHandlingItem.ContainerAttached)
            {
                var absAnimationMatrix = XNAMatrices[IAnimationMatrixY];
                Matrix.Multiply(ref absAnimationMatrix, ref XNAMatrices[IAnimationMatrixX], out absAnimationMatrix);
                Matrix.Multiply(ref absAnimationMatrix, ref XNAMatrices[IAnimationMatrixZ], out absAnimationMatrix);
                Matrix.Multiply(ref absAnimationMatrix, ref Location.XNAMatrix, out absAnimationMatrix);
                ContainerHandlingItem.TransferContainer(absAnimationMatrix);
            }


            // let's make some noise

            if (!OldMoveX && ContainerHandlingItem.MoveX)
                Sound?.HandleEvent(Event.CraneXAxisMove);
            if (OldMoveX && !ContainerHandlingItem.MoveX)
                Sound?.HandleEvent(Event.CraneXAxisSlowDown);
            if (!OldMoveY && ContainerHandlingItem.MoveY)
                Sound?.HandleEvent(Event.CraneYAxisMove);
            if (OldMoveY && !ContainerHandlingItem.MoveY)
                Sound?.HandleEvent(Event.CraneYAxisSlowDown);
            if (!OldMoveZ && ContainerHandlingItem.MoveZ)
                Sound?.HandleEvent(Event.CraneZAxisMove);
            if (OldMoveZ && !ContainerHandlingItem.MoveZ)
                Sound?.HandleEvent(Event.CraneZAxisSlowDown);
            if (OldMoveY && !ContainerHandlingItem.MoveY && !(ContainerHandlingItem.TargetY == ContainerHandlingItem.PickingSurfaceRelativeTopStartPosition.Y))
                Sound?.HandleEvent(Event.CraneYAxisDown);
            OldMoveX = ContainerHandlingItem.MoveX;
            OldMoveY = ContainerHandlingItem.MoveY;
            OldMoveZ = ContainerHandlingItem.MoveZ;
        }

    }


    public class RoadCarShape : AnimatedShape
    {
        public RoadCarShape(Viewer viewer, string path)
            : base(viewer, path, new WorldPosition(), ShapeFlags.ShadowCaster)
        {
        }
    }

    public class TurntableShape : PoseableShape
    {
        protected float AnimationKey;  // advances with time
        protected Turntable Turntable; // linked turntable data
        readonly SoundSource Sound;
        bool Rotating = false;
        protected int IAnimationMatrix = -1; // index of animation matrix

        /// <summary>
        /// Construct and initialize the class
        /// </summary>
        public TurntableShape(Viewer viewer, string path, WorldPosition initialPosition, ShapeFlags flags, Turntable turntable, double startingY)
            : base(viewer, path, initialPosition, flags)
        {
            Turntable = turntable;
            Turntable.StartingY = (float)startingY;
            Turntable.TurntableFrameRate = SharedShape.Animations[0].FrameRate;
            AnimationKey = (Turntable.YAngle / (float)Math.PI * 1800.0f + 3600) % 3600.0f;
            for (var imatrix = 0; imatrix < SharedShape.Matrices.Length; ++imatrix)
            {
                if (SharedShape.MatrixNames[imatrix].ToLower() == turntable.Animations[0].ToLower())
                {
                    IAnimationMatrix = imatrix;
                    break;
                }
            }
            if (viewer.Simulator.TRK.Tr_RouteFile.DefaultTurntableSMS != null)
            {
                var soundPath = viewer.Simulator.RoutePath + @"\\sound\\" + viewer.Simulator.TRK.Tr_RouteFile.DefaultTurntableSMS;
                try
                {
                    Sound = new SoundSource(viewer, initialPosition.WorldLocation, Events.Source.ORTSTurntable, soundPath);
                    viewer.SoundProcess.AddSoundSources(this, new List<SoundSourceBase>() { Sound });
                }
                catch
                {
                    soundPath = viewer.Simulator.BasePath + @"\\sound\\" + viewer.Simulator.TRK.Tr_RouteFile.DefaultTurntableSMS;
                    try
                    {
                        Sound = new SoundSource(viewer, initialPosition.WorldLocation, Events.Source.ORTSTurntable, soundPath);
                        viewer.SoundProcess.AddSoundSources(this, new List<SoundSourceBase>() { Sound });
                    }
                    catch (Exception error)
                    {
                        Trace.WriteLine(new FileLoadException(soundPath, error));
                    }
                }
            }
            for (var matrix = 0; matrix < SharedShape.Matrices.Length; ++matrix)
                AnimateMatrix(matrix, AnimationKey);

            var absAnimationMatrix = XNAMatrices[IAnimationMatrix];
            Matrix.Multiply(ref absAnimationMatrix, ref Location.XNAMatrix, out absAnimationMatrix);
            Turntable.ReInitTrainPositions(absAnimationMatrix);
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            float nextKey;
            var animation = SharedShape.Animations[0];
            if (Turntable.AlignToRemote)
            {
                AnimationKey = (Turntable.YAngle / (float)Math.PI * 1800.0f + 3600) % 3600.0f;
                if (AnimationKey < 0)
                    AnimationKey += animation.FrameCount;
                Turntable.AlignToRemote = false;
            }
            else
            {
                if (Turntable.GoToTarget || Turntable.GoToAutoTarget)
                {
                    nextKey = Turntable.TargetY / (2 * (float)Math.PI) * animation.FrameCount;
                }
                else
                {
                    float moveFrames;
                    if (Turntable.Counterclockwise)
                        moveFrames = animation.FrameRate * elapsedTime.ClockSeconds;
                    else if (Turntable.Clockwise)
                        moveFrames = -animation.FrameRate * elapsedTime.ClockSeconds;
                    else
                        moveFrames = 0;
                    nextKey = AnimationKey + moveFrames;
                }
                AnimationKey = nextKey % animation.FrameCount;
                if (AnimationKey < 0)
                    AnimationKey += animation.FrameCount;
                // used if Turntable cannot turn 360 degrees
                if (Turntable.MaxAngle > 0 && AnimationKey != 0)
                {
                    if (AnimationKey < -SharedShape.Animations[0].FrameCount * Turntable.MaxAngle / (2 * Math.PI) + animation.FrameCount)
                    {
                        if (AnimationKey > 20)
                            AnimationKey = -SharedShape.Animations[0].FrameCount * Turntable.MaxAngle / (float)(2 * Math.PI) + animation.FrameCount;
                        else
                            AnimationKey = 0;
                    }
                }
                Turntable.YAngle = MathHelper.WrapAngle(nextKey / animation.FrameCount * 2 * (float)Math.PI);

                if ((Turntable.Clockwise || Turntable.Counterclockwise || Turntable.AutoClockwise || Turntable.AutoCounterclockwise) && !Rotating)
                {
                    Rotating = true;
                    if (Sound != null) Sound.HandleEvent(Turntable.TrainsOnMovingTable.Count == 1 &&
                        Turntable.TrainsOnMovingTable[0].FrontOnBoard && Turntable.TrainsOnMovingTable[0].BackOnBoard ? Event.MovingTableMovingLoaded : Event.MovingTableMovingEmpty);
                }
                else if ((!Turntable.Clockwise && !Turntable.Counterclockwise && !Turntable.AutoClockwise && !Turntable.AutoCounterclockwise && Rotating))
                {
                    Rotating = false;
                    if (Sound != null) Sound.HandleEvent(Event.MovingTableStopped);
                }
            }
            // Update the pose for each matrix
            for (var matrix = 0; matrix < SharedShape.Matrices.Length; ++matrix)
                AnimateMatrix(matrix, AnimationKey);

            var absAnimationMatrix = XNAMatrices[IAnimationMatrix];
            Matrix.Multiply(ref absAnimationMatrix, ref Location.XNAMatrix, out absAnimationMatrix);
            Turntable.PerformUpdateActions(absAnimationMatrix);
            SharedShape.PrepareFrame(frame, Location, XNAMatrices, Flags);
        }
    }

    public class TransfertableShape : PoseableShape
    {
        protected float AnimationKey;  // advances with time
        protected Transfertable Transfertable; // linked turntable data
        readonly SoundSource Sound;
        bool Translating = false;
        protected int IAnimationMatrix = -1; // index of animation matrix

        /// <summary>
        /// Construct and initialize the class
        /// </summary>
        public TransfertableShape(Viewer viewer, string path, WorldPosition initialPosition, ShapeFlags flags, Transfertable transfertable)
            : base(viewer, path, initialPosition, flags)
        {
            Transfertable = transfertable;
            AnimationKey = (Transfertable.OffsetPos - Transfertable.CenterOffsetComponent) / Transfertable.Span * SharedShape.Animations[0].FrameCount;
            for (var imatrix = 0; imatrix < SharedShape.Matrices.Length; ++imatrix)
            {
                if (SharedShape.MatrixNames[imatrix].ToLower() == transfertable.Animations[0].ToLower())
                {
                    IAnimationMatrix = imatrix;
                    break;
                }
            }
            if (viewer.Simulator.TRK.Tr_RouteFile.DefaultTurntableSMS != null)
            {
                var soundPath = viewer.Simulator.RoutePath + @"\\sound\\" + viewer.Simulator.TRK.Tr_RouteFile.DefaultTurntableSMS;
                try
                {
                    Sound = new SoundSource(viewer, initialPosition.WorldLocation, Events.Source.ORTSTurntable, soundPath);
                    viewer.SoundProcess.AddSoundSources(this, new List<SoundSourceBase>() { Sound });
                }
                catch
                {
                    soundPath = viewer.Simulator.BasePath + @"\\sound\\" + viewer.Simulator.TRK.Tr_RouteFile.DefaultTurntableSMS;
                    try
                    {
                        Sound = new SoundSource(viewer, initialPosition.WorldLocation, Events.Source.ORTSTurntable, soundPath);
                        viewer.SoundProcess.AddSoundSources(this, new List<SoundSourceBase>() { Sound });
                    }
                    catch (Exception error)
                    {
                        Trace.WriteLine(new FileLoadException(soundPath, error));
                    }
                }
            }
            for (var matrix = 0; matrix < SharedShape.Matrices.Length; ++matrix)
                AnimateMatrix(matrix, AnimationKey);

            var absAnimationMatrix = XNAMatrices[IAnimationMatrix];
            Matrix.Multiply(ref absAnimationMatrix, ref Location.XNAMatrix, out absAnimationMatrix);
            Transfertable.ReInitTrainPositions(absAnimationMatrix);
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            var animation = SharedShape.Animations[0];
            if (Transfertable.AlignToRemote)
            {
                AnimationKey = (Transfertable.OffsetPos - Transfertable.CenterOffsetComponent) / Transfertable.Span * SharedShape.Animations[0].FrameCount;
                if (AnimationKey < 0)
                    AnimationKey = 0;
                Transfertable.AlignToRemote = false;
            }
            else
            {
                if (Transfertable.GoToTarget)
                {
                    AnimationKey = (Transfertable.TargetOffset - Transfertable.CenterOffsetComponent) / Transfertable.Span * SharedShape.Animations[0].FrameCount;
                }

                else if (Transfertable.Forward)
                {
                    AnimationKey += SharedShape.Animations[0].FrameRate * elapsedTime.ClockSeconds;
                }
                else if (Transfertable.Reverse)
                {
                    AnimationKey -= SharedShape.Animations[0].FrameRate * elapsedTime.ClockSeconds;
                }
                if (AnimationKey > SharedShape.Animations[0].FrameCount) AnimationKey = SharedShape.Animations[0].FrameCount;
                if (AnimationKey < 0) AnimationKey = 0;

                Transfertable.OffsetPos = AnimationKey / SharedShape.Animations[0].FrameCount * Transfertable.Span + Transfertable.CenterOffsetComponent;

                if ((Transfertable.Forward || Transfertable.Reverse) && !Translating)
                {
                    Translating = true;
                    if (Sound != null) Sound.HandleEvent(Transfertable.TrainsOnMovingTable.Count == 1 &&
                        Transfertable.TrainsOnMovingTable[0].FrontOnBoard && Transfertable.TrainsOnMovingTable[0].BackOnBoard ? Event.MovingTableMovingLoaded : Event.MovingTableMovingEmpty);
                }
                else if ((!Transfertable.Forward && !Transfertable.Reverse && Translating))
                {
                    Translating = false;
                    if (Sound != null) Sound.HandleEvent(Event.MovingTableStopped);
                }
            }

            // Update the pose for each matrix
            for (var matrix = 0; matrix < SharedShape.Matrices.Length; ++matrix)
                AnimateMatrix(matrix, AnimationKey);

            var absAnimationMatrix = XNAMatrices[IAnimationMatrix];
            Matrix.Multiply(ref absAnimationMatrix, ref Location.XNAMatrix, out absAnimationMatrix);
            Transfertable.PerformUpdateActions(absAnimationMatrix, Location);
            SharedShape.PrepareFrame(frame, Location, XNAMatrices, Flags);
        }
    }

    public class ShapePrimitive : RenderPrimitive, IDisposable
    {
        public Material Material { get; protected set; }
        public int[] Hierarchy { get; protected set; } // the hierarchy from the sub_object
        public int HierarchyIndex { get; protected set; } // index into the hiearchy array which provides pose for this primitive

        protected internal VertexBuffer VertexBuffer;
        protected internal IndexBuffer IndexBuffer;
        protected internal int PrimitiveCount;

        readonly VertexBufferBinding[] VertexBufferBindings;

        public ShapePrimitive()
        {
        }

        public ShapePrimitive(Material material, SharedShape.VertexBufferSet vertexBufferSet, IndexBuffer indexBuffer, int primitiveCount, int[] hierarchy, int hierarchyIndex)
        {
            Material = material;
            VertexBuffer = vertexBufferSet.Buffer;
            IndexBuffer = indexBuffer;
            PrimitiveCount = primitiveCount;
            Hierarchy = hierarchy;
            HierarchyIndex = hierarchyIndex;

            VertexBufferBindings = new[] { new VertexBufferBinding(VertexBuffer), new VertexBufferBinding(GetDummyVertexBuffer(material.Viewer.GraphicsDevice)) };
        }

        public ShapePrimitive(Material material, SharedShape.VertexBufferSet vertexBufferSet, IList<ushort> indexData, GraphicsDevice graphicsDevice, int[] hierarchy, int hierarchyIndex)
            : this(material, vertexBufferSet, null, indexData.Count / 3, hierarchy, hierarchyIndex)
        {
            IndexBuffer = new IndexBuffer(graphicsDevice, typeof(short), indexData.Count, BufferUsage.WriteOnly);
            IndexBuffer.SetData(indexData.ToArray());
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            if (PrimitiveCount > 0)
            {
                // TODO consider sorting by Vertex set so we can reduce the number of SetSources required.
                graphicsDevice.SetVertexBuffers(VertexBufferBindings);
                graphicsDevice.Indices = IndexBuffer;
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, baseVertex: 0, startIndex: 0, primitiveCount: PrimitiveCount);
            }
        }

        public void SetMaterial(Material material)
        {
            Material = material;
        }

        [CallOnThread("Loader")]
        public virtual void Mark()
        {
            Material.Mark();
        }

        public void Dispose()
        {
            VertexBuffer.Dispose();
            IndexBuffer.Dispose();
            PrimitiveCount = 0;
        }
    }

    /// <summary>
    /// A <c>ShapePrimitive</c> that permits manipulation of vertex and index buffers to change geometry efficiently.
    /// It permits also change of material
    /// </summary>
    public class MutableShapePrimitive : ShapePrimitive
    {
        /// <remarks>
        /// Buffers cannot be expanded, so take care to properly set <paramref name="maxVertices"/> and <paramref name="maxIndices"/>,
        /// which define the maximum sizes of the vertex and index buffers, respectively.
        /// </remarks>
        public MutableShapePrimitive(Material material, int maxVertices, int maxIndices, int[] hierarchy, int hierarchyIndex)
            : base(material: material,
                   vertexBufferSet: new SharedShape.VertexBufferSet(new VertexPositionNormalTexture[maxVertices], material.Viewer.GraphicsDevice),
                   indexData: new ushort[maxIndices],
                   graphicsDevice: material.Viewer.GraphicsDevice,
                   hierarchy: hierarchy,
                   hierarchyIndex: hierarchyIndex) { }

        public void SetVertexData(VertexPositionNormalTexture[] data, int minVertexIndex, int numVertices, int primitiveCount)
        {
            VertexBuffer.SetData(data);
            PrimitiveCount = primitiveCount;
        }

        public void SetIndexData(short[] data)
        {
            IndexBuffer.SetData(data);
        }
    }

    struct ShapeInstanceData
    {
#pragma warning disable 0649
        public Matrix World;
#pragma warning restore 0649

        public static readonly VertexElement[] VertexElements = {
            new VertexElement(sizeof(float) * 0, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1),
            new VertexElement(sizeof(float) * 4, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 2),
            new VertexElement(sizeof(float) * 8, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 3),
            new VertexElement(sizeof(float) * 12, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 4),
        };

        public static int SizeInBytes = sizeof(float) * 16;
    }

    public class ShapePrimitiveInstances : RenderPrimitive
    {
        public Material Material { get; protected set; }
        public int[] Hierarchy { get; protected set; } // the hierarchy from the sub_object
        public int HierarchyIndex { get; protected set; } // index into the hiearchy array which provides pose for this primitive
        public int SubObjectIndex { get; protected set; }

        protected VertexBuffer VertexBuffer;
        protected VertexDeclaration VertexDeclaration;
        protected int VertexBufferStride;
        protected IndexBuffer IndexBuffer;
        protected int PrimitiveCount;

        protected VertexBuffer InstanceBuffer;
        protected VertexDeclaration InstanceDeclaration;
        protected int InstanceBufferStride;
        protected int InstanceCount;

        readonly VertexBufferBinding[] VertexBufferBindings;

        internal ShapePrimitiveInstances(GraphicsDevice graphicsDevice, ShapePrimitive shapePrimitive, Matrix[] positions, int subObjectIndex)
        {
            Material = shapePrimitive.Material;
            Hierarchy = shapePrimitive.Hierarchy;
            HierarchyIndex = shapePrimitive.HierarchyIndex;
            SubObjectIndex = subObjectIndex;
            VertexBuffer = shapePrimitive.VertexBuffer;
            VertexDeclaration = shapePrimitive.VertexBuffer.VertexDeclaration;
            IndexBuffer = shapePrimitive.IndexBuffer;
            PrimitiveCount = shapePrimitive.PrimitiveCount;

            InstanceDeclaration = new VertexDeclaration(ShapeInstanceData.SizeInBytes, ShapeInstanceData.VertexElements);
            InstanceBuffer = new VertexBuffer(graphicsDevice, InstanceDeclaration, positions.Length, BufferUsage.WriteOnly);
            InstanceBuffer.SetData(positions);
            InstanceCount = positions.Length;

            VertexBufferBindings = new[] { new VertexBufferBinding(VertexBuffer), new VertexBufferBinding(InstanceBuffer, 0, 1) };
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.Indices = IndexBuffer;
            graphicsDevice.SetVertexBuffers(VertexBufferBindings);
            graphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleList, baseVertex: 0, startIndex: 0, PrimitiveCount, InstanceCount);
        }
    }

#if DEBUG_SHAPE_NORMALS
    public class ShapeDebugNormalsPrimitive : ShapePrimitive
    {
        public ShapeDebugNormalsPrimitive(Material material, SharedShape.VertexBufferSet vertexBufferSet, List<ushort> indexData, GraphicsDevice graphicsDevice, int[] hierarchy, int hierarchyIndex)
        {
            Material = material;
            VertexBuffer = vertexBufferSet.DebugNormalsBuffer;
            VertexDeclaration = vertexBufferSet.DebugNormalsDeclaration;
            VertexBufferStride = vertexBufferSet.DebugNormalsDeclaration.GetVertexStrideSize(0);
            var debugNormalsIndexBuffer = new List<ushort>(indexData.Count * SharedShape.VertexBufferSet.DebugNormalsVertexPerVertex);
            for (var i = 0; i < indexData.Count; i++)
                for (var j = 0; j < SharedShape.VertexBufferSet.DebugNormalsVertexPerVertex; j++)
                    debugNormalsIndexBuffer.Add((ushort)(indexData[i] * SharedShape.VertexBufferSet.DebugNormalsVertexPerVertex + j));
            IndexBuffer = new IndexBuffer(graphicsDevice, typeof(short), debugNormalsIndexBuffer.Count, BufferUsage.WriteOnly);
            IndexBuffer.SetData(debugNormalsIndexBuffer.ToArray());
            MinVertexIndex = indexData.Min() * SharedShape.VertexBufferSet.DebugNormalsVertexPerVertex;
            NumVerticies = (indexData.Max() - indexData.Min() + 1) * SharedShape.VertexBufferSet.DebugNormalsVertexPerVertex;
            PrimitiveCount = indexData.Count / 3 * SharedShape.VertexBufferSet.DebugNormalsVertexPerVertex;
            Hierarchy = hierarchy;
            HierarchyIndex = hierarchyIndex;
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            if (PrimitiveCount > 0)
            {
                graphicsDevice.VertexDeclaration = VertexDeclaration;
                graphicsDevice.Vertices[0].SetSource(VertexBuffer, 0, VertexBufferStride);
                graphicsDevice.Indices = IndexBuffer;
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, MinVertexIndex, NumVerticies, 0, PrimitiveCount);
            }
        }

        [CallOnThread("Loader")]
        public virtual void Mark()
        {
            Material.Mark();
        }
    }
#endif

    public class SharedShape : IDisposable
    {
        static List<string> ShapeWarnings = new List<string>();

        // This data is common to all instances of the shape
        public List<string> MatrixNames = new List<string>();
        public List<Material> Materials = new List<Material>();
        public List<string> ImageNames; // Names of textures without paths or file extensions
        public Matrix[] Matrices = new Matrix[0];  // the original natural pose for this shape - shared by all instances
        public animations Animations;
        public LodControl[] LodControls;
        public bool HasNightSubObj;
        public bool StaleData = false;
        public int RootSubObjectIndex = 0;
        //public bool negativeBogie = false;
        public string SoundFileName = "";
        public float CustomAnimationFPS = 8;

        /// <summary>
        /// Store for matrixes needed to be reused in later calculations, e.g. for 3d cabview mouse control
        /// </summary>
        public readonly Dictionary<int, Matrix> StoredResultMatrixes = new Dictionary<int, Matrix>();


        readonly Viewer Viewer;
        public readonly string FilePath;
        public readonly string ReferencePath;

        /// <summary>
        /// Create an empty shape used as a sub when the shape won't load
        /// </summary>
        /// <param name="viewer"></param>
        public SharedShape(Viewer viewer)
        {
            Viewer = viewer;
            FilePath = "Empty";
            LodControls = new LodControl[0];
        }

        /// <summary>
        /// MSTS shape from shape file
        /// </summary>
        /// <param name="viewer"></param>
        /// <param name="filePath">Path to shape's S file</param>
        public SharedShape(Viewer viewer, string filePath)
        {
            Viewer = viewer;
            FilePath = filePath;
            if (filePath.Contains('\0'))
            {
                var parts = filePath.Split('\0');
                FilePath = parts[0];
                ReferencePath = parts[1];
            }
            LoadContent();
        }

        /// <summary>
        /// Only one copy of the model is loaded regardless of how many copies are placed in the scene.
        /// </summary>
        void LoadContent()
        {
            var filePath = FilePath;
            // commented lines allow reading the animation block from an additional file in an Openrails subfolder
//           string dir = Path.GetDirectoryName(filePath);
//            string file = Path.GetFileName(filePath);
//            string orFilePath = dir + @"\openrails\" + file;
            var sFile = new ShapeFile(filePath, Viewer.Settings.SuppressShapeWarnings);
//            if (file.ToLower().Contains("turntable") && File.Exists(orFilePath))
//            {
//                sFile.ReadAnimationBlock(orFilePath);
//            }


            var textureFlags = Helpers.TextureFlags.None;
            if (File.Exists(FilePath + "d"))
            {
                var sdFile = new ShapeDescriptorFile(FilePath + "d");
                textureFlags = (Helpers.TextureFlags)sdFile.shape.ESD_Alternative_Texture;
                if (FilePath != null && FilePath.Contains("\\global\\")) textureFlags |= Helpers.TextureFlags.SnowTrack;//roads and tracks are in global, as MSTS will always use snow texture in snow weather
                HasNightSubObj = sdFile.shape.ESD_SubObj;
                if ((textureFlags & Helpers.TextureFlags.Night) != 0 && FilePath.Contains("\\trainset\\"))
                    textureFlags |= Helpers.TextureFlags.Underground;
                SoundFileName = sdFile.shape.ESD_SoundFileName;
                CustomAnimationFPS = sdFile.shape.ESD_CustomAnimationFPS;
            }

            var matrixCount = sFile.shape.matrices.Count;
            MatrixNames.Capacity = matrixCount;
            Matrices = new Matrix[matrixCount];
            for (var i = 0; i < matrixCount; ++i)
            {
                MatrixNames.Add(sFile.shape.matrices[i].Name.ToUpper());
                Matrices[i] = XNAMatrixFromMSTS(sFile.shape.matrices[i]);
            }
            Animations = sFile.shape.animations;

            ImageNames = new List<string>(sFile.shape.images.ConvertAll(img => Path.GetFileNameWithoutExtension(img).ToLowerInvariant()));

#if DEBUG_SHAPE_HIERARCHY
            var debugShapeHierarchy = new StringBuilder();
            debugShapeHierarchy.AppendFormat("Shape {0}:\n", Path.GetFileNameWithoutExtension(FilePath).ToUpper());
            for (var i = 0; i < MatrixNames.Count; ++i)
                debugShapeHierarchy.AppendFormat("  Matrix {0,-2}: {1}\n", i, MatrixNames[i]);
            for (var i = 0; i < sFile.shape.prim_states.Count; ++i)
                debugShapeHierarchy.AppendFormat("  PState {0,-2}: flags={1,-8:X8} shader={2,-15} alpha={3,-2} vstate={4,-2} lstate={5,-2} zbias={6,-5:F3} zbuffer={7,-2} name={8}\n", i, sFile.shape.prim_states[i].flags, sFile.shape.shader_names[sFile.shape.prim_states[i].ishader], sFile.shape.prim_states[i].alphatestmode, sFile.shape.prim_states[i].ivtx_state, sFile.shape.prim_states[i].LightCfgIdx, sFile.shape.prim_states[i].ZBias, sFile.shape.prim_states[i].ZBufMode, sFile.shape.prim_states[i].Name);
            for (var i = 0; i < sFile.shape.vtx_states.Count; ++i)
                debugShapeHierarchy.AppendFormat("  VState {0,-2}: flags={1,-8:X8} lflags={2,-8:X8} lstate={3,-2} material={4,-3} matrix2={5,-2}\n", i, sFile.shape.vtx_states[i].flags, sFile.shape.vtx_states[i].LightFlags, sFile.shape.vtx_states[i].LightCfgIdx, sFile.shape.vtx_states[i].LightMatIdx, sFile.shape.vtx_states[i].Matrix2);
            for (var i = 0; i < sFile.shape.light_model_cfgs.Count; ++i)
            {
                debugShapeHierarchy.AppendFormat("  LState {0,-2}: flags={1,-8:X8} uv_ops={2,-2}\n", i, sFile.shape.light_model_cfgs[i].flags, sFile.shape.light_model_cfgs[i].uv_ops.Count);
                for (var j = 0; j < sFile.shape.light_model_cfgs[i].uv_ops.Count; ++j)
                    debugShapeHierarchy.AppendFormat("    UV OP {0,-2}: texture_address_mode={1,-2}\n", j, sFile.shape.light_model_cfgs[i].uv_ops[j].TexAddrMode);
            }
            Console.Write(debugShapeHierarchy.ToString());
#endif
            LodControls = (from lod_control lod in sFile.shape.lod_controls
                           select new LodControl(lod, textureFlags, sFile, this)).ToArray();
            if (LodControls.Length == 0)
                throw new InvalidDataException("Shape file missing lod_control section");
            else if (LodControls[0].DistanceLevels.Length > 0 && LodControls[0].DistanceLevels[0].SubObjects.Length > 0)
            {
                // Zero the position offset of the root matrix for compatibility with MSTS
                if (LodControls[0].DistanceLevels[0].SubObjects[0].ShapePrimitives.Length > 0 && LodControls[0].DistanceLevels[0].SubObjects[0].ShapePrimitives[0].Hierarchy[0] == -1)
                {
                    Matrices[0].M41 = 0;
                    Matrices[0].M42 = 0;
                    Matrices[0].M43 = 0;
                }
                // Look for root subobject, it is not necessarily the first (see ProTrain signal)
                for (int soIndex = 0; soIndex <= LodControls[0].DistanceLevels[0].SubObjects.Length - 1; soIndex++)
                {
                    sub_object subObject = sFile.shape.lod_controls[0].distance_levels[0].sub_objects[soIndex];
                    if (subObject.sub_object_header.geometry_info.geometry_node_map[0] == 0)
                    {
                        RootSubObjectIndex = soIndex;
                        break;
                    }
                }
            }
        }

        public class LodControl : IDisposable
        {
            public DistanceLevel[] DistanceLevels;

            public LodControl(lod_control MSTSlod_control, Helpers.TextureFlags textureFlags, ShapeFile sFile, SharedShape sharedShape)
            {
#if DEBUG_SHAPE_HIERARCHY
                Console.WriteLine("  LOD control:");
#endif
                DistanceLevels = (from distance_level level in MSTSlod_control.distance_levels
                                  select new DistanceLevel(level, textureFlags, sFile, sharedShape)).ToArray();
                if (DistanceLevels.Length == 0)
                    throw new InvalidDataException("Shape file missing distance_level");
            }

            [CallOnThread("Loader")]
            internal void Mark()
            {
                foreach (var dl in DistanceLevels)
                {
                    dl.Mark();
                }
            }

            public void Dispose()
            {
                foreach (var dl in DistanceLevels)
                {
                    dl.Dispose();
                }
            }
        }

        public class DistanceLevel : IDisposable
        {
            public float ViewingDistance;
            public float ViewSphereRadius;
            public SubObject[] SubObjects;

            public DistanceLevel(distance_level MSTSdistance_level, Helpers.TextureFlags textureFlags, ShapeFile sFile, SharedShape sharedShape)
            {
#if DEBUG_SHAPE_HIERARCHY
                Console.WriteLine("    Distance level {0}: hierarchy={1}", MSTSdistance_level.distance_level_header.dlevel_selection, String.Join(" ", MSTSdistance_level.distance_level_header.hierarchy.Select(i => i.ToString()).ToArray()));
#endif
                ViewingDistance = MSTSdistance_level.distance_level_header.dlevel_selection;
                // TODO, work out ViewShereRadius from all sub_object radius and centers.
                if (sFile.shape.volumes.Count > 0)
                    ViewSphereRadius = sFile.shape.volumes[0].Radius;
                else
                    ViewSphereRadius = 100;

                var index = 0;
#if DEBUG_SHAPE_HIERARCHY
                var subObjectIndex = 0;
                SubObjects = (from sub_object obj in MSTSdistance_level.sub_objects
                              select new SubObject(obj, ref index, MSTSdistance_level.distance_level_header.hierarchy, textureFlags, subObjectIndex++, sFile, sharedShape)).ToArray();
#else
                SubObjects = (from sub_object obj in MSTSdistance_level.sub_objects
                              select new SubObject(obj, ref index, MSTSdistance_level.distance_level_header.hierarchy, textureFlags, sFile, sharedShape)).ToArray();
#endif
                if (SubObjects.Length == 0)
                    throw new InvalidDataException("Shape file missing sub_object");
            }

            [CallOnThread("Loader")]
            internal void Mark()
            {
                foreach (var so in SubObjects)
                {
                    so.Mark();
                }
            }

            public void Dispose()
            {
                foreach (var so in SubObjects)
                {
                    so.Dispose();
                }
            }
        }

        public class SubObject : IDisposable
        {
            static readonly SceneryMaterialOptions[] UVTextureAddressModeMap = new[] {
                SceneryMaterialOptions.TextureAddressModeWrap,
                SceneryMaterialOptions.TextureAddressModeMirror,
                SceneryMaterialOptions.TextureAddressModeClamp,
                SceneryMaterialOptions.TextureAddressModeBorder,
            };

            static readonly Dictionary<string, SceneryMaterialOptions> ShaderNames = new Dictionary<string, SceneryMaterialOptions> {
                { "Tex", SceneryMaterialOptions.ShaderFullBright },
                { "TexDiff", SceneryMaterialOptions.Diffuse },
                { "BlendATex", SceneryMaterialOptions.AlphaBlendingBlend | SceneryMaterialOptions.ShaderFullBright},
                { "BlendATexDiff", SceneryMaterialOptions.AlphaBlendingBlend | SceneryMaterialOptions.Diffuse },
                { "AddATex", SceneryMaterialOptions.AlphaBlendingAdd | SceneryMaterialOptions.ShaderFullBright},
                { "AddATexDiff", SceneryMaterialOptions.AlphaBlendingAdd | SceneryMaterialOptions.Diffuse },
            };

            static readonly SceneryMaterialOptions[] VertexLightModeMap = new[] {
                SceneryMaterialOptions.ShaderDarkShade,
                SceneryMaterialOptions.ShaderHalfBright,
                SceneryMaterialOptions.ShaderVegetation, // Not certain this is right.
                SceneryMaterialOptions.ShaderVegetation,
                SceneryMaterialOptions.ShaderFullBright,
                SceneryMaterialOptions.None | SceneryMaterialOptions.Specular750,
                SceneryMaterialOptions.None | SceneryMaterialOptions.Specular25,
                SceneryMaterialOptions.None | SceneryMaterialOptions.None,
            };

            public ShapePrimitive[] ShapePrimitives;

#if DEBUG_SHAPE_HIERARCHY
            public SubObject(sub_object sub_object, ref int totalPrimitiveIndex, int[] hierarchy, Helpers.TextureFlags textureFlags, int subObjectIndex, SFile sFile, SharedShape sharedShape)
#else
            public SubObject(sub_object sub_object, ref int totalPrimitiveIndex, int[] hierarchy, Helpers.TextureFlags textureFlags, ShapeFile sFile, SharedShape sharedShape)
#endif
            {
#if DEBUG_SHAPE_HIERARCHY
                var debugShapeHierarchy = new StringBuilder();
                debugShapeHierarchy.AppendFormat("      Sub object {0}:\n", subObjectIndex);
#endif
                var vertexBufferSet = new VertexBufferSet(sub_object, sFile, sharedShape.Viewer.GraphicsDevice);
#if DEBUG_SHAPE_NORMALS
                var debugNormalsMaterial = sharedShape.Viewer.MaterialManager.Load("DebugNormals");
#endif

#if OPTIMIZE_SHAPES_ON_LOAD
                var primitiveMaterials = sub_object.primitives.Cast<primitive>().Select((primitive) =>
#else
                var primitiveIndex = 0;
#if DEBUG_SHAPE_NORMALS
                ShapePrimitives = new ShapePrimitive[sub_object.primitives.Count * 2];
#else
                ShapePrimitives = new ShapePrimitive[sub_object.primitives.Count];
#endif
                foreach (primitive primitive in sub_object.primitives)
#endif
                {
                    var primitiveState = sFile.shape.prim_states[primitive.prim_state_idx];
                    var vertexState = sFile.shape.vtx_states[primitiveState.ivtx_state];
                    var lightModelConfiguration = sFile.shape.light_model_cfgs[vertexState.LightCfgIdx];
                    var options = SceneryMaterialOptions.None;

                    // Validate hierarchy position.
                    var hierarchyIndex = vertexState.imatrix;
                    while (hierarchyIndex != -1)
                    {
                        if (hierarchyIndex < 0 || hierarchyIndex >= hierarchy.Length)
                        {
                            var hierarchyList = new List<int>();
                            hierarchyIndex = vertexState.imatrix;
                            while (hierarchyIndex >= 0 && hierarchyIndex < hierarchy.Length)
                            {
                                hierarchyList.Add(hierarchyIndex);
                                hierarchyIndex = hierarchy[hierarchyIndex];
                            }
                            hierarchyList.Add(hierarchyIndex);
                            Trace.TraceWarning("Ignored invalid primitive hierarchy {1} in shape {0}", sharedShape.FilePath, String.Join(" ", hierarchyList.Select(hi => hi.ToString()).ToArray()));
                            break;
                        }
                        hierarchyIndex = hierarchy[hierarchyIndex];
                    }

                    if (lightModelConfiguration.uv_ops.Count > 0)
                        if (lightModelConfiguration.uv_ops[0].TexAddrMode - 1 >= 0 && lightModelConfiguration.uv_ops[0].TexAddrMode - 1 < UVTextureAddressModeMap.Length)
                            options |= UVTextureAddressModeMap[lightModelConfiguration.uv_ops[0].TexAddrMode - 1];
                        else if (!ShapeWarnings.Contains("texture_addressing_mode:" + lightModelConfiguration.uv_ops[0].TexAddrMode))
                        {
                            Trace.TraceInformation("Skipped unknown texture addressing mode {1} first seen in shape {0}", sharedShape.FilePath, lightModelConfiguration.uv_ops[0].TexAddrMode);
                            ShapeWarnings.Add("texture_addressing_mode:" + lightModelConfiguration.uv_ops[0].TexAddrMode);
                        }

                    if (primitiveState.alphatestmode == 1)
                        options |= SceneryMaterialOptions.AlphaTest;

                    if (ShaderNames.ContainsKey(sFile.shape.shader_names[primitiveState.ishader]))
                        options |= ShaderNames[sFile.shape.shader_names[primitiveState.ishader]];
                    else if (!ShapeWarnings.Contains("shader_name:" + sFile.shape.shader_names[primitiveState.ishader]))
                    {
                        Trace.TraceInformation("Skipped unknown shader name {1} first seen in shape {0}", sharedShape.FilePath, sFile.shape.shader_names[primitiveState.ishader]);
                        ShapeWarnings.Add("shader_name:" + sFile.shape.shader_names[primitiveState.ishader]);
                    }

                    if (12 + vertexState.LightMatIdx >= 0 && 12 + vertexState.LightMatIdx < VertexLightModeMap.Length)
                        options |= VertexLightModeMap[12 + vertexState.LightMatIdx];
                    else if (!ShapeWarnings.Contains("lighting_model:" + vertexState.LightMatIdx))
                    {
                        Trace.TraceInformation("Skipped unknown lighting model index {1} first seen in shape {0}", sharedShape.FilePath, vertexState.LightMatIdx);
                        ShapeWarnings.Add("lighting_model:" + vertexState.LightMatIdx);
                    }

                    if ((textureFlags & Helpers.TextureFlags.Night) != 0)
                        options |= SceneryMaterialOptions.NightTexture;

                    if ((textureFlags & Helpers.TextureFlags.Underground) != 0)
                        options |= SceneryMaterialOptions.UndergroundTexture;

                    Material material;
                    if (primitiveState.tex_idxs.Length != 0)
                    {
                        var texture = sFile.shape.textures[primitiveState.tex_idxs[0]];
                        var imageName = sFile.shape.images[texture.iImage];
                        if (String.IsNullOrEmpty(sharedShape.ReferencePath))
                            material = sharedShape.Viewer.MaterialManager.Load("Scenery", Helpers.GetRouteTextureFile(sharedShape.Viewer.Simulator, textureFlags, imageName), (int)options, texture.MipMapLODBias);
                        else
                            material = sharedShape.Viewer.MaterialManager.Load("Scenery", Helpers.GetTextureFile(sharedShape.Viewer.Simulator, textureFlags, sharedShape.ReferencePath, imageName), (int)options, texture.MipMapLODBias);
                    }
                    else
                    {
                        material = sharedShape.Viewer.MaterialManager.Load("Scenery", null, (int)options);
                    }

                    sharedShape.Materials.Add(material);

#if DEBUG_SHAPE_HIERARCHY
                    debugShapeHierarchy.AppendFormat("        Primitive {0,-2}: pstate={1,-2} vstate={2,-2} lstate={3,-2} matrix={4,-2}", primitiveIndex, primitive.prim_state_idx, primitiveState.ivtx_state, vertexState.LightCfgIdx, vertexState.imatrix);
                    var debugMatrix = vertexState.imatrix;
                    while (debugMatrix >= 0)
                    {
                        debugShapeHierarchy.AppendFormat(" {0}", sharedShape.MatrixNames[debugMatrix]);
                        debugMatrix = hierarchy[debugMatrix];
                    }
                    debugShapeHierarchy.Append("\n");
#endif

#if OPTIMIZE_SHAPES_ON_LOAD
                    return new { Key = material.ToString() + "/" + vertexState.imatrix.ToString(), Primitive = primitive, Material = material, HierachyIndex = vertexState.imatrix };
                }).ToArray();
#else
                    if (primitive.indexed_trilist.vertex_idxs.Count == 0)
                    {
                        Trace.TraceWarning("Skipped primitive with 0 indices in {0}", sharedShape.FilePath);
                        continue;
                    }

                    var indexData = new List<ushort>(primitive.indexed_trilist.vertex_idxs.Count * 3);
                    foreach (vertex_idx vertex_idx in primitive.indexed_trilist.vertex_idxs)
                        foreach (var index in new[] { vertex_idx.a, vertex_idx.b, vertex_idx.c })
                            indexData.Add((ushort)index);

                    ShapePrimitives[primitiveIndex] = new ShapePrimitive(material, vertexBufferSet, indexData, sharedShape.Viewer.GraphicsDevice, hierarchy, vertexState.imatrix);
                    ShapePrimitives[primitiveIndex].SortIndex = ++totalPrimitiveIndex;
                    ++primitiveIndex;
#if DEBUG_SHAPE_NORMALS
                    ShapePrimitives[primitiveIndex] = new ShapeDebugNormalsPrimitive(debugNormalsMaterial, vertexBufferSet, indexData, sharedShape.Viewer.GraphicsDevice, hierarchy, vertexState.imatrix);
                    ShapePrimitives[primitiveIndex].SortIndex = totalPrimitiveIndex;
                    ++primitiveIndex;
#endif
                }
#endif

#if OPTIMIZE_SHAPES_ON_LOAD
                var indexes = new Dictionary<string, List<short>>(sub_object.primitives.Count);
                foreach (var primitiveMaterial in primitiveMaterials)
                {
                    var baseIndex = 0;
                    var indexData = new List<short>(0);
                    if (indexes.TryGetValue(primitiveMaterial.Key, out indexData))
                    {
                        baseIndex = indexData.Count;
                        indexData.Capacity += primitiveMaterial.Primitive.indexed_trilist.vertex_idxs.Count * 3;
                    }
                    else
                    {
                        indexData = new List<short>(primitiveMaterial.Primitive.indexed_trilist.vertex_idxs.Count * 3);
                        indexes.Add(primitiveMaterial.Key, indexData);
                    }

                    var primitiveState = sFile.shape.prim_states[primitiveMaterial.Primitive.prim_state_idx];
                    foreach (vertex_idx vertex_idx in primitiveMaterial.Primitive.indexed_trilist.vertex_idxs)
                    {
                        indexData.Add((short)vertex_idx.a);
                        indexData.Add((short)vertex_idx.b);
                        indexData.Add((short)vertex_idx.c);
                    }
                }

                ShapePrimitives = new ShapePrimitive[indexes.Count];
                var primitiveIndex = 0;
                foreach (var index in indexes)
                {
                    var indexBuffer = new IndexBuffer(sharedShape.Viewer.GraphicsDevice, typeof(short), index.Value.Count, BufferUsage.WriteOnly);
                    indexBuffer.SetData(index.Value.ToArray());
                    var primitiveMaterial = primitiveMaterials.First(d => d.Key == index.Key);
                    ShapePrimitives[primitiveIndex] = new ShapePrimitive(primitiveMaterial.Material, vertexBufferSet, indexBuffer, index.Value.Min(), index.Value.Max() - index.Value.Min() + 1, index.Value.Count / 3, hierarchy, primitiveMaterial.HierachyIndex);
                    ++primitiveIndex;
                }
                if (sub_object.primitives.Count != indexes.Count)
                    Trace.TraceInformation("{1} -> {2} primitives in {0}", sharedShape.FilePath, sub_object.primitives.Count, indexes.Count);
#else
                if (primitiveIndex < ShapePrimitives.Length)
                    ShapePrimitives = ShapePrimitives.Take(primitiveIndex).ToArray();
#endif

#if DEBUG_SHAPE_HIERARCHY
                Console.Write(debugShapeHierarchy.ToString());
#endif
            }

            [CallOnThread("Loader")]
            internal void Mark()
            {
                foreach (var prim in ShapePrimitives)
                {
                    prim.Mark();
                }
            }

            public void Dispose()
            {
                foreach (var prim in ShapePrimitives)
                {
                    prim.Dispose();
                }
            }
        }

        public class VertexBufferSet
        {
            public VertexBuffer Buffer;

#if DEBUG_SHAPE_NORMALS
            public VertexBuffer DebugNormalsBuffer;
            public VertexDeclaration DebugNormalsDeclaration;
            public int DebugNormalsVertexCount;
            public const int DebugNormalsVertexPerVertex = 3 * 4;
#endif

            public VertexBufferSet(VertexPositionNormalTexture[] vertexData, GraphicsDevice graphicsDevice)
            {
                Buffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionNormalTexture), vertexData.Length, BufferUsage.WriteOnly);
                Buffer.SetData(vertexData);
            }

#if DEBUG_SHAPE_NORMALS
            public VertexBufferSet(VertexPositionNormalTexture[] vertexData, VertexPositionColor[] debugNormalsVertexData, GraphicsDevice graphicsDevice)
                :this(vertexData, graphicsDevice)
            {
                DebugNormalsVertexCount = debugNormalsVertexData.Length;
                DebugNormalsDeclaration = new VertexDeclaration(graphicsDevice, VertexPositionColor.VertexElements);
                DebugNormalsBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionColor), DebugNormalsVertexCount, BufferUsage.WriteOnly);
                DebugNormalsBuffer.SetData(debugNormalsVertexData);
            }
#endif

            public VertexBufferSet(sub_object sub_object, ShapeFile sFile, GraphicsDevice graphicsDevice)
#if DEBUG_SHAPE_NORMALS
                : this(CreateVertexData(sub_object, sFile.shape), CreateDebugNormalsVertexData(sub_object, sFile.shape), graphicsDevice)
#else
                : this(CreateVertexData(sub_object, sFile.shape), graphicsDevice)
#endif
            {
            }

            static VertexPositionNormalTexture[] CreateVertexData(sub_object sub_object, shape shape)
            {
                // TODO - deal with vertex sets that have various numbers of texture coordinates - ie 0, 1, 2 etc
                return (from vertex vertex in sub_object.vertices
                        select XNAVertexPositionNormalTextureFromMSTS(vertex, shape)).ToArray();
            }

            static VertexPositionNormalTexture XNAVertexPositionNormalTextureFromMSTS(vertex vertex, shape shape)
            {
                var position = shape.points[vertex.ipoint];
                var normal = shape.normals[vertex.inormal];
                // TODO use a simpler vertex description when no UV's in use
                var texcoord = vertex.vertex_uvs.Length > 0 ? shape.uv_points[vertex.vertex_uvs[0]] : new uv_point(0, 0);

                return new VertexPositionNormalTexture()
                {
                    Position = new Vector3(position.X, position.Y, -position.Z),
                    Normal = new Vector3(normal.X, normal.Y, -normal.Z),
                    TextureCoordinate = new Vector2(texcoord.U, texcoord.V),
                };
            }

#if DEBUG_SHAPE_NORMALS
            static VertexPositionColor[] CreateDebugNormalsVertexData(sub_object sub_object, shape shape)
            {
                var vertexData = new List<VertexPositionColor>();
                foreach (vertex vertex in sub_object.vertices)
                {
                    var position = new Vector3(shape.points[vertex.ipoint].X, shape.points[vertex.ipoint].Y, -shape.points[vertex.ipoint].Z);
                    var normal = new Vector3(shape.normals[vertex.inormal].X, shape.normals[vertex.inormal].Y, -shape.normals[vertex.inormal].Z);
                    var right = Vector3.Cross(normal, Math.Abs(normal.Y) > 0.5 ? Vector3.Left : Vector3.Up);
                    var up = Vector3.Cross(normal, right);
                    right /= 50;
                    up /= 50;
                    vertexData.Add(new VertexPositionColor(position + right, Color.LightGreen));
                    vertexData.Add(new VertexPositionColor(position + normal, Color.LightGreen));
                    vertexData.Add(new VertexPositionColor(position + up, Color.LightGreen));
                    vertexData.Add(new VertexPositionColor(position + up, Color.LightGreen));
                    vertexData.Add(new VertexPositionColor(position + normal, Color.LightGreen));
                    vertexData.Add(new VertexPositionColor(position - right, Color.LightGreen));
                    vertexData.Add(new VertexPositionColor(position - right, Color.LightGreen));
                    vertexData.Add(new VertexPositionColor(position + normal, Color.LightGreen));
                    vertexData.Add(new VertexPositionColor(position - up, Color.LightGreen));
                    vertexData.Add(new VertexPositionColor(position - up, Color.LightGreen));
                    vertexData.Add(new VertexPositionColor(position + normal, Color.LightGreen));
                    vertexData.Add(new VertexPositionColor(position + right, Color.LightGreen));
                }
                return vertexData.ToArray();
            }
#endif
        }

        static Matrix XNAMatrixFromMSTS(matrix MSTSMatrix)
        {
            var XNAMatrix = Matrix.Identity;

            XNAMatrix.M11 = MSTSMatrix.AX;
            XNAMatrix.M12 = MSTSMatrix.AY;
            XNAMatrix.M13 = -MSTSMatrix.AZ;
            XNAMatrix.M21 = MSTSMatrix.BX;
            XNAMatrix.M22 = MSTSMatrix.BY;
            XNAMatrix.M23 = -MSTSMatrix.BZ;
            XNAMatrix.M31 = -MSTSMatrix.CX;
            XNAMatrix.M32 = -MSTSMatrix.CY;
            XNAMatrix.M33 = MSTSMatrix.CZ;
            XNAMatrix.M41 = MSTSMatrix.DX;
            XNAMatrix.M42 = MSTSMatrix.DY;
            XNAMatrix.M43 = -MSTSMatrix.DZ;

            return XNAMatrix;
        }

        public void PrepareFrame(RenderFrame frame, WorldPosition location, ShapeFlags flags)
        {
            PrepareFrame(frame, location, Matrices, null, flags);
        }

        public void PrepareFrame(RenderFrame frame, WorldPosition location, Matrix[] animatedXNAMatrices, ShapeFlags flags, bool[] matrixVisible = null)
        {
            PrepareFrame(frame, location, animatedXNAMatrices, null, flags, matrixVisible);
        }

        public void PrepareFrame(RenderFrame frame, WorldPosition location, Matrix[] animatedXNAMatrices, bool[] subObjVisible, ShapeFlags flags, bool[] matrixVisible = null)
        {
            var lodBias = ((float)Viewer.Settings.LODBias / 100 + 1);

            // Locate relative to the camera
            var dTileX = location.TileX - Viewer.Camera.TileX;
            var dTileZ = location.TileZ - Viewer.Camera.TileZ;
            var mstsLocation = location.Location;
            mstsLocation.X += dTileX * 2048;
            mstsLocation.Z += dTileZ * 2048;
            var xnaDTileTranslation = location.XNAMatrix;
            xnaDTileTranslation.M41 += dTileX * 2048;
            xnaDTileTranslation.M43 -= dTileZ * 2048;

            foreach (var lodControl in LodControls)
            {
                // Start with the furthest away distance, then look for a nearer one in range of the camera.
                var displayDetailLevel = lodControl.DistanceLevels.Length - 1;

                // If this LOD group is not in the FOV, skip the whole LOD group.
                // TODO: This might imair some shadows.
                if (!Viewer.Camera.InFov(mstsLocation, lodControl.DistanceLevels[displayDetailLevel].ViewSphereRadius))
                    continue;

                // We choose the distance level (LOD) to display first:
                //   - LODBias = 100 means we always use the highest detail.
                //   - LODBias < 100 means we operate as normal (using the highest detail in-range of the camera) but
                //     scaling it by LODBias.
                //
                // However, for the viewing distance (and view sphere), we use a slightly different calculation:
                //   - LODBias = 100 means we always use the *lowest* detail viewing distance.
                //   - LODBias < 100 means we operate as normal (see above).
                //
                // The reason for this disparity is that LODBias = 100 is special, because it means "always use
                // highest detail", but this by itself is not useful unless we keep using the normal (LODBias-scaled)
                // viewing distance - right down to the lowest detail viewing distance. Otherwise, we'll scale the
                // highest detail viewing distance up by 100% and then the object will just disappear!

                if (Viewer.Settings.LODBias == 100)
                    // Maximum detail!
                    displayDetailLevel = 0;
                else if (Viewer.Settings.LODBias > -100)
                    // Not minimum detail, so find the correct level (with scaling by LODBias)
                    while ((displayDetailLevel > 0) && Viewer.Camera.InRange(mstsLocation, lodControl.DistanceLevels[displayDetailLevel - 1].ViewSphereRadius, lodControl.DistanceLevels[displayDetailLevel - 1].ViewingDistance * lodBias))
                        displayDetailLevel--;

                var displayDetail = lodControl.DistanceLevels[displayDetailLevel];
                var distanceDetail = Viewer.Settings.LODBias == 100
                    ? lodControl.DistanceLevels[lodControl.DistanceLevels.Length - 1]
                    : displayDetail;

                // If set, extend the lowest LOD to the maximum viewing distance.
                if (Viewer.Settings.LODViewingExtension && displayDetailLevel == lodControl.DistanceLevels.Length - 1)
                    // Set to MaxValue so that an object never disappears.
                    // Many MSTS objects had a LOD of 2km which is the maximum distance that MSTS can handle.
                    // Open Rails can handle greater distances, so we override the lowest-detail LOD to make sure OR shows shapes further away than 2km.
                    // See http://www.elvastower.com/forums/index.php?/topic/35301-menu-options/page__view__findpost__p__275531
                    distanceDetail.ViewingDistance = float.MaxValue;

                for (var i = 0; i < displayDetail.SubObjects.Length; i++)
                {
                    var subObject = displayDetail.SubObjects[i];

                    // The 1st subobject (note that index 0 is the main object itself) is hidden during the day if HasNightSubObj is true.
                    if ((subObjVisible != null && !subObjVisible[i]) || (i == 1 && HasNightSubObj && Viewer.MaterialManager.sunDirection.Y >= 0))
                        continue;

                    foreach (var shapePrimitive in subObject.ShapePrimitives)
                    {
                        var xnaMatrix = Matrix.Identity;
                        var hi = shapePrimitive.HierarchyIndex;
                        if (matrixVisible != null && !matrixVisible[hi]) continue;
                        while (hi >= 0 && hi < shapePrimitive.Hierarchy.Length)
                        {
                            Matrix.Multiply(ref xnaMatrix, ref animatedXNAMatrices[hi], out xnaMatrix);
                            hi = shapePrimitive.Hierarchy[hi];
                        }
                        Matrix.Multiply(ref xnaMatrix, ref xnaDTileTranslation, out xnaMatrix);

                        if (StoredResultMatrixes.ContainsKey(shapePrimitive.HierarchyIndex))
                            StoredResultMatrixes[shapePrimitive.HierarchyIndex] = xnaMatrix;

                        // TODO make shadows depend on shape overrides

                        var interior = (flags & ShapeFlags.Interior) != 0;
                        frame.AddAutoPrimitive(mstsLocation, distanceDetail.ViewSphereRadius, distanceDetail.ViewingDistance * lodBias, shapePrimitive.Material, shapePrimitive, interior ? RenderPrimitiveGroup.Interior : RenderPrimitiveGroup.World, ref xnaMatrix, flags);
                    }
                }
            }
        }

        public Matrix GetMatrixProduct(int iNode)
        {
            int[] h = LodControls[0].DistanceLevels[0].SubObjects[0].ShapePrimitives[0].Hierarchy;
            Matrix matrix = Matrix.Identity;
            while (iNode != -1)
            {
                matrix *= Matrices[iNode];
                iNode = h[iNode];
            }
            return matrix;
        }

        public int GetParentMatrix(int iNode)
        {
            return LodControls[0].DistanceLevels[0].SubObjects[0].ShapePrimitives[0].Hierarchy[iNode];
        }

        [CallOnThread("Loader")]
        internal void Mark()
        {
            Viewer.ShapeManager.Mark(this);
            foreach (var lod in LodControls)
            {
                lod.Mark();
            }
        }

        public void Dispose()
        {
            foreach (var lod in LodControls)
            {
                lod.Dispose();
            }
        }
    }

    public class TrItemLabel
    {
        public readonly WorldPosition Location;
        public readonly string ItemName;

        /// <summary>
        /// Construct and initialize the class.
        /// This constructor is for the labels of track items in TDB and W Files such as sidings and platforms.
        /// </summary>
        public TrItemLabel(Viewer viewer, WorldPosition position, TrObject trObj)
        {
            Location = position;
            var i = 0;
            while (true)
            {
                var trID = trObj.getTrItemID(i);
                if (trID < 0)
                    break;
                var trItem = viewer.Simulator.TDB.TrackDB.TrItemTable[trID];
                if (trItem == null)
                    continue;
                ItemName = trItem.ItemName;
                i++;
            }
        }
    }
}
