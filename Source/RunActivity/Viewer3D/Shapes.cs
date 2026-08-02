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
using Orts.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Event = Orts.Common.Event;
using Events = Orts.Common.Events;
using System.Collections;

namespace Orts.Viewer3D
{
    [CallOnThread("Loader")]
    public class SharedShapeManager
    {
        readonly Viewer Viewer;

        Dictionary<string, SharedShape> Shapes = new Dictionary<string, SharedShape>();
        Dictionary<string, bool> ShapeMarks = new Dictionary<string, bool>();
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
            if (!Shapes.ContainsKey(path))
            {
                try
                {
                    var extension = Path.GetExtension(path.Split('\0')[0]).ToLower();
                    Shapes.Add(path, extension == ".gltf" || extension == ".glb" ? new GltfShape(Viewer, path) : new SharedShape(Viewer, path));
                }
                catch (Exception error)
                {
                    Trace.WriteLine(new FileLoadException(path, error));
                    Shapes.Add(path, EmptyShape);
                }
            }
            return Shapes[path];
        }

        public void Mark()
        {
            ShapeMarks.Clear();
            foreach (var path in Shapes.Keys)
                ShapeMarks.Add(path, false);
        }

        public void Mark(SharedShape shape)
        {
            foreach (var key in Shapes.Keys)
            {
                if (Shapes[key] == shape)
                {
                    ShapeMarks[key] = true;
                    break;
                }
            }
        }

        public void Sweep()
        {
            foreach (var path in ShapeMarks.Where(kvp => !kvp.Value).Select(kvp => kvp.Key))
            {
                Shapes[path].Dispose();
                Shapes.Remove(path);
            }
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
            matrix *= shapes.FirstOrDefault()?.SharedShape.ForwardZDirection ?? Matrix.Identity;

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

        public void ConditionallyPrepareFrame(RenderFrame frame, ElapsedTime elapsedTime, Dictionary<int, bool> matrixVisible = null)
        {
            SharedShape.PrepareFrame(frame, Location, XNAMatrices, Flags, matrixVisible);
        }

        /// <summary>
        /// Adjust the pose of the specified node to the frame position specifed by key.
        /// </summary>
        public void AnimateMatrix(int iMatrix, float key, bool skipChildrenAnimations = false)
        {
            if (SharedShape is GltfShape gltfShape)
            {
                if (!gltfShape.HasAnimation(iMatrix))
                {
                    if (!SeenShapeAnimationError.ContainsKey(SharedShape.FilePath))
                        Trace.TraceInformation("No animation number {1} in shape {0}", SharedShape.FilePath, iMatrix);
                    SeenShapeAnimationError[SharedShape.FilePath] = true;
                    return;
                }
                else
                {
                    // iMatrix is the number of the animation, not the number of the node,
                    // key is the time, not the number of the frame.
                    gltfShape.Animate(iMatrix, key, XNAMatrices);
                }
                return;
            }

            // Animate the given matrix.
            AnimateOneMatrix(iMatrix, key);

            if (skipChildrenAnimations)
                return;

            // Animate all child nodes in the hierarchy too.
            for (var i = 0; i < Hierarchy.Length; i++)
                if (Hierarchy[i] == iMatrix)
                    AnimateMatrix(i, key, false);
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
        AnimatedPart AnimatedPart;

        public AnimatedShape(Viewer viewer, string path, WorldPosition initialPosition, ShapeFlags flags, float frameRateDivisor = 1.0f)
            : base(viewer, path, initialPosition, flags)
        {
            if (SharedShape.HasAnimations())
            {
                AnimatedPart = new AnimatedPart(this);
                AnimatedPart.AddAnimations();
                AnimatedPart.SetMstsSpeed(30.0f / frameRateDivisor, true, false);
            }
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            AnimatedPart?.UpdateLoop(1, elapsedTime);
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
            if (!SharedShape.HasAnimations())
            {
                if (!SeenShapeAnimationError.ContainsKey(SharedShape.FilePath))
                    Trace.TraceInformation("Ignored missing animations data in shape {0}", SharedShape.FilePath);
                SeenShapeAnimationError[SharedShape.FilePath] = true;
                return;  // animation is missing
            }

            if (!SharedShape.HasAnimation(iMatrix))
            {
                if (!SeenShapeAnimationError.ContainsKey(SharedShape.FilePath))
                    Trace.TraceInformation("Ignored out of bounds matrix {1} in shape {0}", SharedShape.FilePath, iMatrix);
                SeenShapeAnimationError[SharedShape.FilePath] = true;
                return;  // mismatched matricies
            }

            var anim_node = SharedShape.Animations[0].anim_nodes[iMatrix];
            var animName = anim_node.Name.ToLowerInvariant();

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
        readonly AnimatedPart AnimatedPart;
        readonly TrJunctionNode TrJunctionNode;  // has data on current aligment for the switch
        readonly uint MainRoute;                 // 0 or 1 - which route is considered the main route

        public SwitchTrackShape(Viewer viewer, string path, WorldPosition position, TrJunctionNode trj)
            : base(viewer, path, position, ShapeFlags.AutoZBias)
        {
            TrJunctionNode = trj;
            TrackShape TS = viewer.Simulator.TSectionDat.TrackShapes.Get(TrJunctionNode.ShapeIndex);
            MainRoute = TS.MainRoute;

            if (SharedShape.HasAnimations())
            {
                AnimatedPart = new AnimatedPart(this);
                AnimatedPart.AddAnimations();
                AnimatedPart.SetMstsSpeed(2.0f, false, false);

                // MSTS shape format junction animations are tricky, they consist of 3 animation nodes.
                // 0: main, 1: diverging, 2: main again. Go till frame 1 only.
                if (!(SharedShape is GltfShape))
                    AnimatedPart.MaxFrame = 1;
            }
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            AnimatedPart?.UpdateState(TrJunctionNode.SelectedRoute != MainRoute, elapsedTime);
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
        readonly AnimatedPart AnimatedPart;
        readonly bool Looped;

        bool Opening = true;

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
                    var soundPath = ORTSPaths.GetFileFromFolders(new[] { viewer.Simulator.RoutePath, viewer.Simulator.BasePath }, @"\\sound\\" + soundFileName);
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
            Crossing = viewer.Simulator.LevelCrossings.CreateLevelCrossing(
                position,
                from tid in CrossingObj.trItemIDList where tid.db == 0 select tid.dbID,
                from tid in CrossingObj.trItemIDList where tid.db == 1 select tid.dbID,
                CrossingObj.levelCrParameters.warningTime,
                CrossingObj.levelCrParameters.minimumDistance);

            if (SharedShape.HasAnimations())
            {
                // LOOPED COSSINGS (animTiming < 0)
                //     MSTS plays through all the frames of the animation for "closed" and sits on frame 0 for "open". The
                //     speed of animation is the normal speed (frame rate at 30FPS) scaled by the timing value. Since the
                //     timing value is negative, the animation actually plays in reverse.
                // NON-LOOPED CROSSINGS (animTiming > 0)
                //     MSTS plays through the first 1.0 seconds of the animation forwards for closing and backwards for
                //     opening. The number of frames defined doesn't matter; the animation is limited by time so the frame
                //     rate (based on 30FPS) is what's needed.
                Looped = CrossingObj.levelCrTiming.animTiming < 0;
                AnimatedPart = new AnimatedPart(this);
                AnimatedPart.AddAnimations();
                AnimatedPart.SetMstsSpeed(1.0f / CrossingObj.levelCrTiming.animTiming, true, false);
                AnimatedPart.SetGltfSpeed(1.0f / Math.Abs(CrossingObj.levelCrTiming.animTiming));

                if (!Looped && SharedShape?.Animations?.FirstOrDefault()?.FrameRate is int frameRate)
                    AnimatedPart.MaxFrame = frameRate / 30f; // Clamped to max 1 s for shape format
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
                Sound?.HandleEvent(Opening ? Event.CrossingOpening : Event.CrossingClosing);
            }

            if (Looped)
                AnimatedPart?.UpdateLoop(Opening ? 0f : 1f, elapsedTime);
            else
                AnimatedPart?.UpdateState(Opening ? 0f : 1f, elapsedTime);

            SharedShape.PrepareFrame(frame, Location, XNAMatrices, Flags);
        }
    }

    public class HazzardShape : PoseableShape
    {
        readonly HazardObj HazardObj;
        readonly Hazzard Hazzard;
        readonly AnimatedPart AnimatedPart;

        float Moved = 0f;
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
            if (SharedShape.HasAnimations())
            {
                AnimatedPart = new AnimatedPart(this);
                AnimatedPart.AddAnimations();
                AnimatedPart.SetMstsSpeed(24.0f, false, false);
            }
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
            switch (Hazzard.state)
            {
                case Hazzard.State.Idle1:
                    CurrentRange = Hazzard.HazFile.Tr_HazardFile.Idle_Key / AnimatedPart.MaxFrame; break;
                case Hazzard.State.Idle2:
                    CurrentRange = Hazzard.HazFile.Tr_HazardFile.Idle_Key2 / AnimatedPart.MaxFrame; break;
                case Hazzard.State.LookLeft:
                    CurrentRange = Hazzard.HazFile.Tr_HazardFile.Surprise_Key_Left / AnimatedPart.MaxFrame; break;
                case Hazzard.State.LookRight:
                    CurrentRange = Hazzard.HazFile.Tr_HazardFile.Surprise_Key_Right / AnimatedPart.MaxFrame; break;
                case Hazzard.State.Scared:
                default:
                    CurrentRange = Hazzard.HazFile.Tr_HazardFile.Success_Scarper_Key / AnimatedPart.MaxFrame;
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

            DelayHazAnimation += elapsedTime.ClockSeconds;

            AnimatedPart.UpdateState(CurrentRange.Y, elapsedTime);

            var currentKeyFraction = AnimatedPart.AnimationKeyFraction();
            //AnimatedPart.SetState(MathHelper.Clamp(currentKeyFraction, CurrentRange.X, CurrentRange.Y));

            if (Hazzard.state == Hazzard.State.Idle1 || Hazzard.state == Hazzard.State.Idle2)
            {
                if (DelayHazAnimation > 5f)
                {
                    if (currentKeyFraction < CurrentRange.X || currentKeyFraction > CurrentRange.Y)
                    {
                        AnimatedPart.SetState(CurrentRange.X);
                        DelayHazAnimation = 0;
                    }
                }
            }

            if (Hazzard.state == Hazzard.State.LookLeft || Hazzard.state == Hazzard.State.LookRight)
            {
                AnimatedPart.SetState(MathHelper.Clamp(currentKeyFraction, CurrentRange.X, CurrentRange.Y));
            }

            if (Hazzard.state == Hazzard.State.Scared)
            {
                if (currentKeyFraction < CurrentRange.X || currentKeyFraction > CurrentRange.Y)
                    AnimatedPart.SetState(CurrentRange.X);
            }

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
        AnimatedPart AnimatedPartX;
        AnimatedPart AnimatedPartY;
        AnimatedPart AnimatedPartZ;
        AnimatedPart AnimatedPartCable;
        AnimatedPart AnimatedPartGrabber01;
        AnimatedPart AnimatedPartGrabber02;

        Vector3 AnimationXYZSpan;
        Vector3 AnimationGrabber01Span;
        Vector3 AnimationGrabber02Span;
        Vector3 AnimationXYZStart;
        Vector3 AnimationGrabber01Start;
        Vector3 AnimationGrabber02Start;

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

            AnimatedPartX = new AnimatedPart(this);
            AnimatedPartY = new AnimatedPart(this);
            AnimatedPartZ = new AnimatedPart(this);
            AnimatedPartCable = new AnimatedPart(this);
            AnimatedPartGrabber01 = new AnimatedPart(this);
            AnimatedPartGrabber02 = new AnimatedPart(this);

            AnimatedPartX.AddAnimation("XAXIS");
            AnimatedPartY.AddAnimation("YAXIS");
            AnimatedPartZ.AddAnimation("ZAXIS");
            AnimatedPartCable.AddAnimation("CABLE*");
            AnimatedPartGrabber01.AddAnimation("GRABBER01");
            AnimatedPartGrabber02.AddAnimation("GRABBER02");

            AnimatedPartX.SetMstsSpeed(1.0f / FuelPickupItemObj.PickupAnimData.AnimationSpeed, false, true);
            AnimatedPartY.SetMstsSpeed(1.0f / FuelPickupItemObj.PickupAnimData.AnimationSpeed, false, true);
            AnimatedPartZ.SetMstsSpeed(1.0f / FuelPickupItemObj.PickupAnimData.AnimationSpeed, false, true);
            AnimatedPartCable.SetMstsSpeed(1.0f / FuelPickupItemObj.PickupAnimData.AnimationSpeed, false, true);
            AnimatedPartGrabber01.SetMstsSpeed(1.0f / FuelPickupItemObj.PickupAnimData.AnimationSpeed, false, true);
            AnimatedPartGrabber02.SetMstsSpeed(1.0f / FuelPickupItemObj.PickupAnimData.AnimationSpeed, false, true);

            AnimatedPartX.SetMstsAnimationOptions(AnimatedPart.MstsAnimationOptions.SkipChildrenAnimations | AnimatedPart.MstsAnimationOptions.MaxFrameFromKeyframeOne);
            AnimatedPartY.SetMstsAnimationOptions(AnimatedPart.MstsAnimationOptions.SkipChildrenAnimations | AnimatedPart.MstsAnimationOptions.MaxFrameFromKeyframeOne);
            AnimatedPartZ.SetMstsAnimationOptions(AnimatedPart.MstsAnimationOptions.SkipChildrenAnimations | AnimatedPart.MstsAnimationOptions.MaxFrameFromKeyframeOne);
            AnimatedPartCable.SetMstsAnimationOptions(AnimatedPart.MstsAnimationOptions.SkipChildrenAnimations | AnimatedPart.MstsAnimationOptions.MaxFrameFromKeyframeOne);
            AnimatedPartGrabber01.SetMstsAnimationOptions(AnimatedPart.MstsAnimationOptions.SkipChildrenAnimations | AnimatedPart.MstsAnimationOptions.MaxFrameFromKeyframeOne);
            AnimatedPartGrabber02.SetMstsAnimationOptions(AnimatedPart.MstsAnimationOptions.SkipChildrenAnimations | AnimatedPart.MstsAnimationOptions.MaxFrameFromKeyframeOne);

            SharedShape.GetAnimationOutputMinMax(AnimatedPartX.MatrixIndexes.FirstOrDefault(), out var minX, out var maxX, out var startX);
            SharedShape.GetAnimationOutputMinMax(AnimatedPartY.MatrixIndexes.FirstOrDefault(), out var minY, out var maxY, out var startY);
            SharedShape.GetAnimationOutputMinMax(AnimatedPartZ.MatrixIndexes.FirstOrDefault(), out var minZ, out var maxZ, out var startZ);
            SharedShape.GetAnimationOutputMinMax(AnimatedPartGrabber01.MatrixIndexes.FirstOrDefault(), out var minG01, out var maxG01, out AnimationGrabber01Start);
            SharedShape.GetAnimationOutputMinMax(AnimatedPartGrabber02.MatrixIndexes.FirstOrDefault(), out var minG02, out var maxG02, out AnimationGrabber02Start);

            AnimationXYZStart = new Vector3(startX.X, startY.Y, startZ.Z);
            AnimationXYZSpan = new Vector3(maxX.X - minX.X, maxY.Y - minY.Y, maxZ.Z - minZ.Z);
            AnimationGrabber01Span = maxG01 - minG01;
            AnimationGrabber02Span = maxG02 - minG02;

            var key = GetStateFromPosition(Vector3.Zero);

            AnimatedPartX.SetState(key.X);
            AnimatedPartY.SetState(key.Y);
            AnimatedPartZ.SetState(key.Z);

            var absAnimationMatrix = XNAMatrices[AnimatedPartY.GetFirstTargetNode()];
            Matrix.Multiply(ref absAnimationMatrix, ref XNAMatrices[AnimatedPartX.GetFirstTargetNode()], out absAnimationMatrix);
            Matrix.Multiply(ref absAnimationMatrix, ref XNAMatrices[AnimatedPartZ.GetFirstTargetNode()], out absAnimationMatrix);
            Matrix.Multiply(ref absAnimationMatrix, ref Location.XNAMatrix, out absAnimationMatrix);

            ContainerHandlingItem = Viewer.Simulator.ContainerManager.ContainerHandlingItems[FuelPickupItemObj.TrItemIDList[0].dbID];
            ContainerHandlingItem.PassSpanParameters(maxZ.Z, minZ.Z, minG01.Z - maxG01.Z, minG02.Z - maxG02.Z);
            ContainerHandlingItem.ReInitPositionOffset(absAnimationMatrix);

            key = GetStateFromPosition(ContainerHandlingItem.PickingSurfaceRelativeTopStartPosition);

            AnimatedPartX.SetState(key.X);
            AnimatedPartY.SetState(key.Y);
            AnimatedPartZ.SetState(key.Z);
            AnimatedPartCable.SetState(key.Y);
            AnimatedPartGrabber01.SetState(0);
            AnimatedPartGrabber02.SetState(0);
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (FuelPickupItemObj.UID == MSTSWagon.RefillProcess.ActivePickupObjectUID)
            {
                var key = GetStateFromPosition(new Vector3(ContainerHandlingItem.TargetX, ContainerHandlingItem.TargetY, ContainerHandlingItem.TargetZ));
                var keyGrabber01 = GetStateFromPosition(ContainerHandlingItem.TargetGrabber01, AnimationGrabber01Start.Z, AnimationGrabber01Span.Z);
                var keyGrabber02 = GetStateFromPosition(ContainerHandlingItem.TargetGrabber02, AnimationGrabber02Start.Z, AnimationGrabber02Span.Z);

                AnimatedPartX.SlowDownFactor = Math.Abs(AnimatedPartX.AnimationKeyFraction() - key.X) > slowDownThreshold / AnimatedPartX.MaxFrame ? 1.0f : 0.25f;
                AnimatedPartY.SlowDownFactor = Math.Abs(AnimatedPartY.AnimationKeyFraction() - key.Y) > slowDownThreshold / AnimatedPartY.MaxFrame ? 1.0f : 0.25f;
                AnimatedPartZ.SlowDownFactor = Math.Abs(AnimatedPartZ.AnimationKeyFraction() - key.Z) > slowDownThreshold / AnimatedPartZ.MaxFrame ? 1.0f : 0.25f;
                AnimatedPartCable.SlowDownFactor = Math.Abs(AnimatedPartCable.AnimationKeyFraction() - key.Y) > slowDownThreshold / AnimatedPartCable.MaxFrame ? 1.0f : 0.25f;
                AnimatedPartGrabber01.SlowDownFactor = Math.Abs(AnimatedPartGrabber01.AnimationKeyFraction() - keyGrabber01) > slowDownThreshold / AnimatedPartGrabber01.MaxFrame ? 1.0f : 0.25f;
                AnimatedPartGrabber02.SlowDownFactor = Math.Abs(AnimatedPartGrabber02.AnimationKeyFraction() - keyGrabber02) > slowDownThreshold / AnimatedPartGrabber02.MaxFrame ? 1.0f : 0.25f;

                AnimatedPartX.UpdateState(key.X, elapsedTime);
                AnimatedPartY.UpdateState(key.Y, elapsedTime);
                AnimatedPartZ.UpdateState(key.Z, elapsedTime);
                AnimatedPartCable.UpdateState(key.Y, elapsedTime);
                AnimatedPartGrabber01.UpdateState(keyGrabber01, elapsedTime);
                AnimatedPartGrabber02.UpdateState(keyGrabber02, elapsedTime);

                if (AnimatedPartX.AnimationKeyFraction() == key.X) ContainerHandlingItem.MoveX = false;
                if (AnimatedPartY.AnimationKeyFraction() == key.Y) ContainerHandlingItem.MoveY = false;
                if (AnimatedPartZ.AnimationKeyFraction() == key.Z) ContainerHandlingItem.MoveZ = false;
                if (AnimatedPartGrabber01.AnimationKeyFraction() == keyGrabber01 && AnimatedPartGrabber02 .AnimationKeyFraction() == keyGrabber02) ContainerHandlingItem.MoveGrabber = false;
            }

            ContainerHandlingItem.ActualX = AnimatedPartX.AnimationKeyFraction() * AnimationXYZSpan.X + AnimationXYZStart.X;
            ContainerHandlingItem.ActualY = AnimatedPartY.AnimationKeyFraction() * AnimationXYZSpan.Y + AnimationXYZStart.Y;
            ContainerHandlingItem.ActualZ = AnimatedPartZ.AnimationKeyFraction() * AnimationXYZSpan.Z + AnimationXYZStart.Z;
            ContainerHandlingItem.ActualGrabber01 = AnimatedPartGrabber01.AnimationKeyFraction() * AnimationGrabber01Span.Z + AnimationGrabber01Start.Z;
            ContainerHandlingItem.ActualGrabber02 = AnimatedPartGrabber02.AnimationKeyFraction() * AnimationGrabber02Span.Z + AnimationGrabber02Start.Z;

            SharedShape.PrepareFrame(frame, Location, XNAMatrices, Flags);

            if (ContainerHandlingItem.ContainerAttached)
            {
                var absAnimationMatrix = XNAMatrices[AnimatedPartY.GetFirstTargetNode()];
                Matrix.Multiply(ref absAnimationMatrix, ref XNAMatrices[AnimatedPartX.GetFirstTargetNode()], out absAnimationMatrix);
                Matrix.Multiply(ref absAnimationMatrix, ref XNAMatrices[AnimatedPartZ.GetFirstTargetNode()], out absAnimationMatrix);
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

        Vector3 GetStateFromPosition(Vector3 input)
        {
            var output = (input - AnimationXYZStart) / AnimationXYZSpan;

            output.X = Math.Abs(output.X);
            output.Y = Math.Abs(output.Y);
            output.Z = Math.Abs(output.Z);

            return output;
        }

        float GetStateFromPosition(float input, float start, float span)
        {
            return Math.Abs((input - start) / span);
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
        readonly AnimatedPart AnimatedPart;
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
            Turntable.TurntableFrameRate = SharedShape.Animations?.FirstOrDefault()?.FrameRate;
            for (var imatrix = 0; imatrix < SharedShape.GetAnimationNamesCount(); ++imatrix)
            {
                if (SharedShape.MatrixNames[imatrix].ToLower() == turntable.Animations[0].ToLower())
                {
                    IAnimationMatrix = imatrix;
                    break;
                }
            }
            if (viewer.Simulator.TRK.Tr_RouteFile.DefaultTurntableSMS != null)
            {
                var soundPath = ORTSPaths.GetFileFromFolders(new[] { viewer.Simulator.RoutePath, viewer.Simulator.BasePath }, @"\\sound\\" + viewer.Simulator.TRK.Tr_RouteFile.DefaultTurntableSMS);
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

            AnimatedPart = new AnimatedPart(this);
            AnimatedPart.AddAnimations();
            AnimatedPart.SetMstsSpeed(30f, true, false); // Seems like the FrameRate is used directly here, unlike in other classes where it is rated to 30.
            AnimatedPart.SetFrameWrap(Turntable.YAngle / MathHelper.TwoPi * AnimatedPart.MaxFrame);

            var absAnimationMatrix = XNAMatrices.ElementAtOrDefault(SharedShape.GetAnimationTargetNode(IAnimationMatrix));
            Matrix.Multiply(ref absAnimationMatrix, ref Location.XNAMatrix, out absAnimationMatrix);
            Turntable.ReInitTrainPositions(absAnimationMatrix);
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (Turntable.AlignToRemote)
            {
                AnimatedPart.SetFrameWrap(Turntable.YAngle / MathHelper.TwoPi * AnimatedPart.MaxFrame);
                Turntable.AlignToRemote = false;
            }
            else
            {
                if (Turntable.GoToTarget || Turntable.GoToAutoTarget)
                    AnimatedPart.SetFrameWrap(Turntable.TargetY / MathHelper.TwoPi * AnimatedPart.MaxFrame);
                else if (Turntable.Counterclockwise)
                    AnimatedPart.UpdateLoop(1, elapsedTime);
                else if (Turntable.Clockwise)
                    AnimatedPart.UpdateLoop(-1, elapsedTime);

                // Used if Turntable cannot turn 360 degrees, counting in minus rotation direction.
                // Thus e.g. MaxAngle 40 deg will result the animation to allow from 360 (=0) to 320 degrees. 
                if (Turntable.MaxAngle > 0)
                {
                    var maxAngleState = -Turntable.MaxAngle / MathHelper.TwoPi + 1;

                    if (maxAngleState > 0.5f && AnimatedPart.AnimationKeyFraction() < maxAngleState ||
                        maxAngleState < 0.5f && AnimatedPart.AnimationKeyFraction() > maxAngleState)
                    {
                        if (AnimatedPart.AnimationKeyFraction() > 0.5f)
                            AnimatedPart.SetState(maxAngleState > 0.5f ? maxAngleState : 0);
                        else
                            AnimatedPart.SetState(maxAngleState < 0.5f ? maxAngleState : 0);
                    }
                }
                Turntable.YAngle = MathHelper.WrapAngle(AnimatedPart.AnimationKeyFraction() * MathHelper.TwoPi);

                if ((Turntable.Clockwise || Turntable.Counterclockwise || Turntable.AutoClockwise || Turntable.AutoCounterclockwise) && !Rotating)
                {
                    Rotating = true;
                    Sound?.HandleEvent(Turntable.TrainsOnMovingTable.Count == 1 &&
                        Turntable.TrainsOnMovingTable[0].FrontOnBoard && Turntable.TrainsOnMovingTable[0].BackOnBoard
                        ? Event.MovingTableMovingLoaded : Event.MovingTableMovingEmpty);
                }
                else if ((!Turntable.Clockwise && !Turntable.Counterclockwise && !Turntable.AutoClockwise && !Turntable.AutoCounterclockwise && Rotating))
                {
                    Rotating = false;
                    Sound?.HandleEvent(Event.MovingTableStopped);
                }
            }

            var absAnimationMatrix = XNAMatrices.ElementAtOrDefault(SharedShape.GetAnimationTargetNode(IAnimationMatrix));
            Matrix.Multiply(ref absAnimationMatrix, ref Location.XNAMatrix, out absAnimationMatrix);
            Turntable.PerformUpdateActions(absAnimationMatrix);

            SharedShape.PrepareFrame(frame, Location, XNAMatrices, Flags);
        }
    }

    public class TransfertableShape : PoseableShape
    {
        readonly AnimatedPart AnimatedPart;
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
            for (var imatrix = 0; imatrix < SharedShape.GetAnimationNamesCount(); ++imatrix)
            {
                if (SharedShape.MatrixNames[imatrix].ToLower() == transfertable.Animations[0].ToLower())
                {
                    IAnimationMatrix = imatrix;
                    break;
                }
            }
            if (viewer.Simulator.TRK.Tr_RouteFile.DefaultTurntableSMS != null)
            {
                var soundPath = ORTSPaths.GetFileFromFolders(new[] { viewer.Simulator.RoutePath, viewer.Simulator.BasePath }, @"\\sound\\" + viewer.Simulator.TRK.Tr_RouteFile.DefaultTurntableSMS);
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

            AnimatedPart = new AnimatedPart(this);
            AnimatedPart.AddAnimations();
            AnimatedPart.SetMstsSpeed(30f, true, false);
            AnimatedPart.SetFrameClamp((Transfertable.OffsetPos - Transfertable.CenterOffsetComponent) / Transfertable.Span * AnimatedPart.MaxFrame);

            var absAnimationMatrix = XNAMatrices.ElementAtOrDefault(SharedShape.GetAnimationTargetNode(IAnimationMatrix));
            Matrix.Multiply(ref absAnimationMatrix, ref Location.XNAMatrix, out absAnimationMatrix);
            Transfertable.ReInitTrainPositions(absAnimationMatrix);
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            var animation = SharedShape.Animations[0];
            if (Transfertable.AlignToRemote)
            {
                AnimatedPart.SetFrameClamp((Transfertable.OffsetPos - Transfertable.CenterOffsetComponent) / Transfertable.Span * AnimatedPart.MaxFrame);
                Transfertable.AlignToRemote = false;
            }
            else
            {
                if (Transfertable.GoToTarget)
                    AnimatedPart.SetFrameClamp((Transfertable.TargetOffset - Transfertable.CenterOffsetComponent) / Transfertable.Span * AnimatedPart.MaxFrame);
                else if (Transfertable.Forward)
                    AnimatedPart.UpdateState(1, elapsedTime);
                else if (Transfertable.Reverse)
                    AnimatedPart.UpdateState(0, elapsedTime);

                Transfertable.OffsetPos = AnimatedPart.AnimationKeyFraction() * Transfertable.Span + Transfertable.CenterOffsetComponent;

                if ((Transfertable.Forward || Transfertable.Reverse) && !Translating)
                {
                    Translating = true;
                    Sound?.HandleEvent(Transfertable.TrainsOnMovingTable.Count == 1 &&
                        Transfertable.TrainsOnMovingTable[0].FrontOnBoard && Transfertable.TrainsOnMovingTable[0].BackOnBoard
                        ? Event.MovingTableMovingLoaded : Event.MovingTableMovingEmpty);
                }
                else if ((!Transfertable.Forward && !Transfertable.Reverse && Translating))
                {
                    Translating = false;
                    Sound?.HandleEvent(Event.MovingTableStopped);
                }
            }

            var absAnimationMatrix = XNAMatrices.ElementAtOrDefault(SharedShape.GetAnimationTargetNode(IAnimationMatrix));
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

        protected internal IndexBuffer IndexBuffer;
        protected internal int PrimitiveCount;
        protected internal int PrimitiveOffset;
        protected internal PrimitiveType PrimitiveType;

        protected internal readonly VertexBufferBinding[] VertexBufferBindings;

        public ShapePrimitive() { }
        
        public ShapePrimitive(VertexBufferBinding[] vertexBufferBindings) => VertexBufferBindings = vertexBufferBindings;

        public ShapePrimitive(Material material, SharedShape.VertexBufferSet vertexBufferSet, IndexBuffer indexBuffer, int primitiveCount, int[] hierarchy, int hierarchyIndex)
            : this(material, vertexBufferSet, new VertexBufferBinding[0], indexBuffer, primitiveCount, hierarchy, hierarchyIndex)
        { }

        public ShapePrimitive(Material material, SharedShape.VertexBufferSet vertexBufferSet, VertexBufferBinding[] vertexBufferBindings, IndexBuffer indexBuffer, int primitiveCount, int[] hierarchy, int hierarchyIndex)
            : this(vertexBufferBindings.Prepend(new VertexBufferBinding(vertexBufferSet.Buffer)).Append(new VertexBufferBinding(GetDummyVertexBuffer(material.Viewer.GraphicsDevice))).ToArray())
        {
            Material = material;
            IndexBuffer = indexBuffer;
            PrimitiveCount = primitiveCount;
            Hierarchy = hierarchy;
            HierarchyIndex = hierarchyIndex;
            PrimitiveType = PrimitiveType.TriangleList;
        }

        public ShapePrimitive(Material material, SharedShape.VertexBufferSet vertexBufferSet, IList<ushort> indexData, GraphicsDevice graphicsDevice, int[] hierarchy, int hierarchyIndex)
            : this(material, vertexBufferSet, new VertexBufferBinding[0], indexData, graphicsDevice, hierarchy, hierarchyIndex)
        { }

        public ShapePrimitive(Material material, SharedShape.VertexBufferSet vertexBufferSet, VertexBufferBinding[] vertexBufferBindings, IList<ushort> indexData, GraphicsDevice graphicsDevice, int[] hierarchy, int hierarchyIndex)
            : this(material, vertexBufferSet, vertexBufferBindings, null, indexData.Count / 3, hierarchy, hierarchyIndex)
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
                if (IndexBuffer != null)
                {
                    graphicsDevice.Indices = IndexBuffer;
                    graphicsDevice.DrawIndexedPrimitives(PrimitiveType, baseVertex: 0, startIndex: PrimitiveOffset, primitiveCount: PrimitiveCount);
                }
                else
                {
                    graphicsDevice.DrawPrimitives(PrimitiveType, 0, PrimitiveCount);
                }
            }
        }

        public void SetMaterial(Material material)
        {
            Material = material;
        }

        [CallOnThread("Loader")]
        public virtual void Mark()
        {
            Material?.Mark();
        }

        public void Dispose()
        {
            var dummyInstanceBuffer = RenderPrimitive.GetDummyVertexBuffer(null);
            for (var i = 0; i < VertexBufferBindings.Length; i++)
                if (VertexBufferBindings[i].VertexBuffer != dummyInstanceBuffer)
                    VertexBufferBindings[i].VertexBuffer?.Dispose();
            IndexBuffer?.Dispose();
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
            VertexBufferBindings.FirstOrDefault().VertexBuffer?.SetData(data);
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

        protected int VertexBufferStride;
        protected IndexBuffer IndexBuffer;
        protected int PrimitiveCount;
        protected internal int PrimitiveOffset;
        protected PrimitiveType PrimitiveType;

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
            IndexBuffer = shapePrimitive.IndexBuffer;
            PrimitiveCount = shapePrimitive.PrimitiveCount;
            PrimitiveOffset = shapePrimitive.PrimitiveOffset;
            PrimitiveType = shapePrimitive.PrimitiveType;

            InstanceDeclaration = new VertexDeclaration(ShapeInstanceData.SizeInBytes, ShapeInstanceData.VertexElements);
            InstanceBuffer = new VertexBuffer(graphicsDevice, InstanceDeclaration, positions.Length, BufferUsage.WriteOnly);
            InstanceBuffer.SetData(positions);
            InstanceCount = positions.Length;

            var instanceBufferBinding = new VertexBufferBinding(InstanceBuffer, 0, 1);

            VertexBufferBindings = shapePrimitive.VertexBufferBindings.ToArray();
            var dummyInstanceBuffer = RenderPrimitive.GetDummyVertexBuffer(graphicsDevice);
            var position = -1;
            for (var i = 0; i < VertexBufferBindings.Length; i++)
                if (VertexBufferBindings[i].VertexBuffer == dummyInstanceBuffer)
                    position = i;
            if (position == -1)
                VertexBufferBindings.Append(instanceBufferBinding);
            else
                VertexBufferBindings[position] = instanceBufferBinding;
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.Indices = IndexBuffer;
            graphicsDevice.SetVertexBuffers(VertexBufferBindings);
            graphicsDevice.DrawInstancedPrimitives(PrimitiveType, baseVertex: 0, startIndex: PrimitiveOffset, PrimitiveCount, InstanceCount);
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
        public List<string> ImageNames; // Names of textures without paths or file extensions
        public Matrix[] Matrices = new Matrix[0];  // the original natural pose for this shape - shared by all instances
        public animations Animations;
        public LodControl[] LodControls;
        public bool HasNightSubObj;
        public int RootSubObjectIndex = 0;
        //public bool negativeBogie = false;
        public string SoundFileName = "";
        public float CustomAnimationFPS = 8;

        /// <summary>
        /// Store for matrixes needed to be reused in later calculations, e.g. for 3d cabview mouse control
        /// </summary>
        public readonly Dictionary<int, Matrix> StoredResultMatrixes = new Dictionary<int, Matrix>();

        public virtual Matrix ForwardZDirection => Matrix.Identity;

        readonly protected Viewer Viewer;
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
        protected virtual void LoadContent()
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

            ImageNames = new List<string>(sFile.shape.images.ConvertAll(img => Path.GetFileNameWithoutExtension(img)));

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

            public LodControl() { }

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

            public DistanceLevel() { }

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
                { "Diffuse", SceneryMaterialOptions.Diffuse },
                { "Tex", SceneryMaterialOptions.ShaderFullBright },
                { "TexDiff", SceneryMaterialOptions.Diffuse },
                { "BlendATex", SceneryMaterialOptions.AlphaBlendingBlend | SceneryMaterialOptions.ShaderFullBright},
                { "BlendATexDiff", SceneryMaterialOptions.AlphaBlendingBlend | SceneryMaterialOptions.Diffuse },
                { "AddATex", SceneryMaterialOptions.AlphaBlendingAdd | SceneryMaterialOptions.ShaderFullBright},
                { "AddATexDiff", SceneryMaterialOptions.AlphaBlendingAdd | SceneryMaterialOptions.Diffuse },
                { "GlossMap", SceneryMaterialOptions.Diffuse },
            };

            static readonly SceneryMaterialOptions[] VertexLightModeMap = new[] {
                SceneryMaterialOptions.ShaderDarkShade, // -12
                SceneryMaterialOptions.ShaderHalfBright, // -11
                SceneryMaterialOptions.ShaderVegetation, // -10 only env. light, no direct light
                SceneryMaterialOptions.ShaderVegetation, // -9 only env. light half bright, no direct light
                SceneryMaterialOptions.ShaderFullBright, // -8 no direct light
                SceneryMaterialOptions.Specular750, // -7 specular, no direct light
                SceneryMaterialOptions.Specular25, // -6 specular half bright, no direct light
                SceneryMaterialOptions.None, // -5
                SceneryMaterialOptions.ShaderVegetation, // -4 only env. light, no direct light
                SceneryMaterialOptions.Specular750, // -3 specular
                SceneryMaterialOptions.Specular25, // -2 specular half bright
                SceneryMaterialOptions.None // -1
            };

            public ShapePrimitive[] ShapePrimitives;

            public SubObject() { }

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

                    color diffuseColor = new color { R = 1, G = 1, B = 1, A = 1 };
                    float metallicFactor = 0;
                    float roughnessFactor = 1;

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
                    {
                        options |= ShaderNames[sFile.shape.shader_names[primitiveState.ishader]];

                        if (sFile.shape.shader_names[primitiveState.ishader] == "GlossMap")
                        {
                            metallicFactor = 1;
                            roughnessFactor = 0;
                            options |= SceneryMaterialOptions.PbrHasIndices | SceneryMaterialOptions.PbrHasNormals;
                        }
                    }
                    else if (!ShapeWarnings.Contains("shader_name:" + sFile.shape.shader_names[primitiveState.ishader]))
                    {
                        Trace.TraceInformation("Skipped unknown shader name {1} first seen in shape {0}", sharedShape.FilePath, sFile.shape.shader_names[primitiveState.ishader]);
                        ShapeWarnings.Add("shader_name:" + sFile.shape.shader_names[primitiveState.ishader]);
                    }

                    if (12 + vertexState.LightMatIdx >= 0 && 12 + vertexState.LightMatIdx < VertexLightModeMap.Length)
                        options |= VertexLightModeMap[12 + vertexState.LightMatIdx];
                    else if (vertexState.LightMatIdx >= 0 && sFile.shape.light_materials.ElementAtOrDefault(vertexState.LightMatIdx) is light_material lightMaterial
                        && sFile.shape.colors.ElementAtOrDefault(lightMaterial.DiffColIdx) is color color)
                    {
                        diffuseColor = color;
                        options |= SceneryMaterialOptions.PbrHasIndices | SceneryMaterialOptions.PbrHasNormals;
                    }
                    else if (!ShapeWarnings.Contains("lighting_model:" + vertexState.LightMatIdx))
                    {
                        Trace.TraceInformation("Skipped unknown lighting model index {1} first seen in shape {0}", sharedShape.FilePath, vertexState.LightMatIdx);
                        ShapeWarnings.Add("lighting_model:" + vertexState.LightMatIdx);
                    }

                    if ((textureFlags & Helpers.TextureFlags.Night) != 0)
                        options |= SceneryMaterialOptions.NightTexture;

                    if ((textureFlags & Helpers.TextureFlags.Underground) != 0)
                        options |= SceneryMaterialOptions.UndergroundTexture;

                    texture texture = null;
                    string texturePath = null;

                    if (primitiveState.tex_idxs.Length != 0)
                    {
                        texture = sFile.shape.textures[primitiveState.tex_idxs[0]];
                        var imageName = sFile.shape.images[texture.iImage];
                        if (String.IsNullOrEmpty(sharedShape.ReferencePath))
                            texturePath = Helpers.GetRouteTextureFile(sharedShape.Viewer.Simulator, textureFlags, imageName);
                        else
                            texturePath = Helpers.GetTextureFile(sharedShape.Viewer.Simulator, textureFlags, sharedShape.ReferencePath, imageName);
                    }

                    Material material;
                    if ((options & SceneryMaterialOptions.PbrHasIndices) > 0)
                    {
                        // Special PBR rendering path for non-textured or glossy materials
                        var gltf = new glTFLoader.Schema.Gltf
                        {
                            Materials = new[]
                            {
                                new glTFLoader.Schema.Material
                                {
                                    AlphaMode = glTFLoader.Schema.Material.AlphaModeEnum.OPAQUE,
                                    EmissiveFactor = new[] { 0f, 0f, 0f },
                                    PbrMetallicRoughness = new glTFLoader.Schema.MaterialPbrMetallicRoughness
                                    {
                                        BaseColorFactor = new[] { diffuseColor.R, diffuseColor.G, diffuseColor.B, diffuseColor.A },
                                        MetallicFactor = metallicFactor,
                                        RoughnessFactor = roughnessFactor
                                    }
                                }
                        },
                            Samplers = new[] { GltfShape.GltfSubObject.DefaultGltfSampler },
                            Scenes = new[] { new glTFLoader.Schema.Scene { Nodes = new[] { 0 } } },
                        };

                        material = sharedShape.Viewer.MaterialManager.Load("PBR",
                            $"{sharedShape.FilePath}#0#{vertexState.LightMatIdx}",
                            (int)options, 0, null, gltf);
                        var baseColorTexture = texturePath == null ? null : sharedShape.Viewer.TextureManager.Get(texturePath);
                        (material as PbrMaterial).LoadTextures(baseColorTexture);
                    }
                    else
                    {
                        // Standard rendering path for traditional textured materials
                        material = sharedShape.Viewer.MaterialManager.Load("Scenery", texturePath, (int)options, texture?.MipMapLODBias ?? 0);
                    }

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
            public VertexBufferSet() { }

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

        public void PrepareFrame(RenderFrame frame, WorldPosition location, Matrix[] animatedXNAMatrices, ShapeFlags flags, Dictionary<int, bool> matrixVisible = null)
        {
            PrepareFrame(frame, location, animatedXNAMatrices, null, flags, matrixVisible);
        }

        public void PrepareFrame(RenderFrame frame, WorldPosition location, Matrix[] animatedXNAMatrices, bool[] subObjVisible, ShapeFlags flags, Dictionary<int, bool> matrixVisible = null)
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
                if (!(lodControl.DistanceLevels.ElementAtOrDefault(displayDetailLevel) is DistanceLevel distanceLevel) || !Viewer.Camera.InFov(mstsLocation, distanceLevel.ViewSphereRadius))
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
                {
                    // Not minimum detail, so find the correct level (with scaling by LODBias)
                    if (this is GltfShape gltfShape)
                    {
                        // glTF lod-ding is based on minimum screen coverage.
                        // Checking from level 0 to less detailed
                        while (displayDetailLevel > 0 && Viewer.Camera.BiggerThan(xnaDTileTranslation, gltfShape.BoundingBoxNodes, gltfShape.MinimumScreenCoverages[displayDetailLevel - 1]))
                            displayDetailLevel--;
                        gltfShape.SetLod(displayDetailLevel);
                    }
                    else
                    {
                        // .s lod-ding is based on distance levels
                        while ((displayDetailLevel > 0) && Viewer.Camera.InRange(mstsLocation, lodControl.DistanceLevels[displayDetailLevel - 1].ViewSphereRadius, lodControl.DistanceLevels[displayDetailLevel - 1].ViewingDistance * lodBias))
                            displayDetailLevel--;
                    }
                }

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
                        var hi = shapePrimitive.HierarchyIndex;
                        if (matrixVisible != null && matrixVisible.TryGetValue(hi, out var visible) && !visible) continue;

                        var xnaMatrix = SetRenderMatrices(shapePrimitive, animatedXNAMatrices, ref xnaDTileTranslation);

                        if (StoredResultMatrixes.ContainsKey(shapePrimitive.HierarchyIndex))
                            StoredResultMatrixes[shapePrimitive.HierarchyIndex] = xnaMatrix;

                        // TODO make shadows depend on shape overrides

                        var interior = (flags & ShapeFlags.Interior) != 0;
                        frame.AddAutoPrimitive(mstsLocation, distanceDetail.ViewSphereRadius, distanceDetail.ViewingDistance * lodBias, shapePrimitive.Material, shapePrimitive, interior ? RenderPrimitiveGroup.Interior : RenderPrimitiveGroup.World, ref xnaMatrix, flags);
                    }
                }
            }
        }

        public virtual Matrix SetRenderMatrices(ShapePrimitive shapePrimitive, Matrix[] animatedXNAMatrices, ref Matrix xnaDTileTranslation)
        {
            var xnaMatrix = Matrix.Identity;
            var hi = shapePrimitive.HierarchyIndex;
            while (hi >= 0 && hi < shapePrimitive.Hierarchy.Length)
            {
                Matrix.Multiply(ref xnaMatrix, ref animatedXNAMatrices[hi], out xnaMatrix);
                hi = shapePrimitive.Hierarchy[hi];
            }
            Matrix.Multiply(ref xnaMatrix, ref xnaDTileTranslation, out xnaMatrix);
            return xnaMatrix;
        }

        public virtual Matrix GetMatrixProduct(int iNode)
        {
            var h = GetModelHierarchy();
            Matrix matrix = Matrix.Identity;
            if (h != null && h.Length > iNode)
            {
                while (iNode != -1)
                {
                    matrix *= Matrices[iNode];
                    iNode = h[iNode];
                }
            }
            return matrix;
        }

        public int[] GetModelHierarchy() => LodControls?.FirstOrDefault()?.DistanceLevels?.FirstOrDefault()?.SubObjects?.FirstOrDefault()?.ShapePrimitives?.FirstOrDefault()?.Hierarchy;

        /// <summary>
        /// This method is part of the animation handling. Gets the parent that will be animated, for finding a bogie for wheels.
        /// </summary>
        public int GetParentMatrix(int iNode) => GetModelHierarchy()?.ElementAtOrDefault(iNode) ?? -1;

        /// <summary>
        /// Searches for the parent animation.
        /// </summary>
        /// <param name="animationId">For stf files it is the node id, for gltf files it is the animation id.</param>
        /// <returns>The parent animation id.</returns>
        public virtual int GetAnimationParent(int animationId) => GetParentMatrix(animationId);

        /// <summary>
        /// Tells whether the animation is an internal sequence defined within the shape, or is just a tag that needs external animation.
        /// </summary>
        /// <param name="animationId">For stf files it is the node id, for gltf files it is the animation id.</param>
        /// <returns>true if there is no internal seqence defined in the shape.</returns>
        public virtual bool IsAnimationArticulation(int animationId) =>
            !(Animations?.FirstOrDefault()?.anim_nodes is anim_nodes a && a.Count > animationId && a.ElementAtOrDefault(animationId)?.controllers is controllers c && c.Count > 0);

        /// <summary>
        /// Returns the parent animation id.
        /// </summary>
        /// <param name="animationId">For stf files it is the node id, for gltf files it is the animation id.</param>
        /// <returns>Returns for stf files the node id itself, for gltf files the target node id of the animation.</returns>
        public virtual int GetAnimationTargetNode(int animationId) => animationId;

        public virtual int GetAnimationNamesCount() => LodControls?.FirstOrDefault()?.DistanceLevels?.FirstOrDefault()?.SubObjects?.FirstOrDefault().ShapePrimitives?.FirstOrDefault()?.Hierarchy?.Length ?? 0;

        public virtual bool HasAnimations() => Animations != null;

        public virtual bool HasAnimation(int animationId) => Animations?.FirstOrDefault()?.anim_nodes?.ElementAtOrDefault(animationId)?.controllers?.Count > 0;

        public virtual float GetAnimationLength(int animationId) => Animations?.FirstOrDefault()?.anim_nodes?.ElementAtOrDefault(animationId)?.controllers?
            .Select(c => c.LastOrDefault()?.Frame ?? 0).DefaultIfEmpty(0).Max() ?? 0;

        public virtual void GetAnimationOutputMinMax(int animationId, out Vector3 min, out Vector3 max, out Vector3 start)
        {
            min = max = start = Vector3.Zero;

            if (!(Animations?.FirstOrDefault()?.anim_nodes?.ElementAtOrDefault(animationId)?.controllers?.FirstOrDefault() is IList keys) || keys.Count == 0)
                return;

            var v = (linear_key)keys[0];
            min = max = start = new Vector3(v.X, v.Y, v.Z);

            for (int i = 1; i < keys.Count; i++)
            {
                v = (linear_key)keys[i];

                if (v.X < min.X) min.X = v.X;
                if (v.Y < min.Y) min.Y = v.Y;
                if (v.Z < min.Z) min.Z = v.Z;

                if (v.X > max.X) max.X = v.X;
                if (v.Y > max.Y) max.Y = v.Y;
                if (v.Z > max.Z) max.Z = v.Z;
            }
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
