// COPYRIGHT 2009, 2010, 2011, 2012 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

// Experimental code which collapses unnecessarily duplicated primitives when loading shapes.
// WANRING: Slower and not guaranteed to work!
//#define OPTIMIZE_SHAPES_ON_LOAD

// Prints out lots of diagnostic information about the construction of shapes, with regards their sub-objects and hierarchies.
//#define DEBUG_SHAPE_HIERARCHY

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MSTS;
using MSTSMath;

namespace ORTS
{
    [CallOnThread("Loader")]
    public class SharedShapeManager
    {
        readonly Viewer3D Viewer;

        Dictionary<string, SharedShape> Shapes = new Dictionary<string, SharedShape>();
        Dictionary<string, bool> ShapeMarks;
        SharedShape EmptyShape;

        [CallOnThread("Render")]
        internal SharedShapeManager(Viewer3D viewer)
        {
            Viewer = viewer;
            EmptyShape = new SharedShape(Viewer);
        }

        public SharedShape Get(string path)
        {
            if (Thread.CurrentThread.Name != "Loader Process")
                Trace.TraceError("SharedShapeManager.Get incorrectly called by {0}; must be Loader Process or crashes will occur.", Thread.CurrentThread.Name);

            if (path == null)
                return EmptyShape;

            path = path.ToLowerInvariant();
            if (!Shapes.ContainsKey(path))
            {
                try
                {
                    Shapes.Add(path, new SharedShape(Viewer, path));
                }
                catch (Exception error)
                {
                    Trace.TraceInformation(path);
                    Trace.WriteLine(error);
                    Shapes.Add(path, EmptyShape);
                }
            }
            return Shapes[path];
        }

        public void Mark()
        {
            ShapeMarks = new Dictionary<string, bool>(Shapes.Count);
            foreach (var path in Shapes.Keys)
                ShapeMarks.Add(path, false);
        }

        public void Mark(SharedShape shape)
        {
            if (Shapes.ContainsValue(shape))
                ShapeMarks[Shapes.First(kvp => kvp.Value == shape).Key] = true;
        }

        public void Sweep()
        {
            foreach (var path in ShapeMarks.Where(kvp => !kvp.Value).Select(kvp => kvp.Key))
                Shapes.Remove(path);
        }

        [CallOnThread("Updater")]
        public string GetStatus()
        {
            return String.Format("{0:F0} shapes", Shapes.Keys.Count);
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
        // NOTE: Use powers of 2 for values!
    }

    public class StaticShape
    {
        public readonly Viewer3D Viewer;
        public readonly WorldPosition Location;
        public readonly ShapeFlags Flags;
        public readonly SharedShape SharedShape;

        /// <summary>
        /// Construct and initialize the class
        /// This constructor is for objects described by a MSTS shape file
        /// </summary>
        public StaticShape(Viewer3D viewer, string path, WorldPosition position, ShapeFlags flags)
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
        internal virtual void Mark()
        {
            SharedShape.Mark();
        }
    }

    public class StaticTrackShape : StaticShape
    {
        public StaticTrackShape(Viewer3D viewer, string path, WorldPosition position)
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
        public Matrix[] XNAMatrices = new Matrix[0];  // the positions of the subobjects

        public PoseableShape(Viewer3D viewer, string path, WorldPosition initialPosition, ShapeFlags flags)
            : base(viewer, path, initialPosition, flags)
        {
            XNAMatrices = new Matrix[SharedShape.Matrices.Length];
            for (int iMatrix = 0; iMatrix < SharedShape.Matrices.Length; ++iMatrix)
                XNAMatrices[iMatrix] = SharedShape.Matrices[iMatrix];
        }

        public PoseableShape(Viewer3D viewer, string path, WorldPosition initialPosition)
            : this(viewer, path, initialPosition, ShapeFlags.None)
        {
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            SharedShape.PrepareFrame(frame, Location, XNAMatrices, Flags);
        }

        /// <summary>
        /// Adjust the pose of the specified node to the frame position specifed by key.
        /// </summary>
        public void AnimateMatrix(int iMatrix, float key)
        {
            if (SharedShape.Animations == null)
                return;  // animation is missing

            if (iMatrix >= SharedShape.Animations[0].anim_nodes.Count)
                return;  // mismatched matricies

            anim_node anim_node = SharedShape.Animations[0].anim_nodes[iMatrix];
            if (anim_node.controllers.Count == 0)
                return;  // missing controllers

            Matrix xnaPose = SharedShape.Matrices[iMatrix]; // start with the intial pose in the shape file

            foreach (controller controller in anim_node.controllers)
            {
                // determine the frame number and transition amount
                int iKey1 = 0;
                for (int i = 0; i < controller.Count; ++i)
                    if (controller[i].Frame <= key + 0.0001)
                        iKey1 = i;
                    else
                        break;
                KeyPosition position1 = controller[iKey1];
                float frame1 = position1.Frame;

                int iKey2 = iKey1 + 1;
                if (iKey2 >= controller.Count)
                    iKey2 = 0;
                KeyPosition position2 = controller[iKey2];
                float frame2 = position2.Frame;
                if (iKey2 == 0)
                    frame2 = SharedShape.Animations[0].FrameCount; //changed at V148 by Doug, was controller.Count;

                float amount;
                if (Math.Abs(frame2 - frame1) > 0.0001)
                    amount = (key - frame1) / Math.Abs(frame2 - frame1);
                else
                    amount = 0;

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
        protected float AnimationKey = 0.0f;  // advances with time

        /// <summary>
        /// Construct and initialize the class
        /// </summary>
        public AnimatedShape(Viewer3D viewer, string path, WorldPosition initialPosition, ShapeFlags flags)
            : base(viewer, path, initialPosition, flags)
        {
        }

        public AnimatedShape(Viewer3D viewer, string path, WorldPosition initialPosition)
            : this(viewer, path, initialPosition, ShapeFlags.None)
        {
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            // if the shape has animations
            if (SharedShape.Animations != null && SharedShape.Animations.Count > 0 && SharedShape.Animations[0].FrameCount > 1)
            {
                // Compute the animation key based on framerate etc
                // ie, with 8 frames of animation, the key will advance from 0 to 8 at the specified speed.
                AnimationKey += (float)SharedShape.Animations[0].FrameRate * elapsedTime.ClockSeconds;
                while (AnimationKey >= SharedShape.Animations[0].FrameCount) AnimationKey -= SharedShape.Animations[0].FrameCount;
                while (AnimationKey < -0.00001) AnimationKey += SharedShape.Animations[0].FrameCount;

                // Update the pose for each matrix
                for (int iMatrix = 0; iMatrix < SharedShape.Matrices.Length; ++iMatrix)
                    AnimateMatrix(iMatrix, AnimationKey);
            }
            SharedShape.PrepareFrame(frame, Location, XNAMatrices, Flags);
        }
    }

    public class SwitchTrackShape : PoseableShape
    {
        protected float AnimationKey = 0.0f;  // tracks position of points as they move left and right

        TrJunctionNode TrJunctionNode;  // has data on current aligment for the switch
        uint MainRoute;                  // 0 or 1 - which route is considered the main route

        public SwitchTrackShape(Viewer3D viewer, string path, WorldPosition position, TrJunctionNode trj)
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

        protected float AnimationKey = 0.0f;  // tracks position of points as they move left and right
        ShapePrimitive shapePrimitive;
        public SpeedPostShape(Viewer3D viewer, string path, WorldPosition position, SpeedPostObj spo)
            : base(viewer, path, position)
        {

            SpeedPostObj = spo;
            var maxVertex = SpeedPostObj.Sign_Shape.NumShapes * 48;// every face has max 7 digits, each has 2 triangles
            var material = viewer.MaterialManager.Load("Scenery", Helpers.GetRouteTextureFile(viewer.Simulator, Helpers.TextureFlags.None, SpeedPostObj.Speed_Digit_Tex), (int)(SceneryMaterialOptions.None | SceneryMaterialOptions.AlphaBlendingBlend), 0);

            // Create and populate a new ShapePrimitive
            NumVertices = NumIndices = 0;
            var i = 0; var id = -1; var size = SpeedPostObj.Text_Size.Size; var idlocation = 0;
            id = SpeedPostObj.getTrItemID(idlocation);
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

                        //the left-bottom vertex
                        Vector3 v = new Vector3(offset.X, offset.Y, 0);
                        M.Rotate2D(rotation, ref v.X, ref v.Z);
                        v += start; Vertex v1 = new Vertex(v.X, v.Y, v.Z + 0.01f, 0, 0, -1, tX, tY);

                        //the right-bottom vertex
                        v.X = offset.X + size; v.Y = offset.Y; v.Z = 0;
                        M.Rotate2D(rotation, ref v.X, ref v.Z);
                        v += start; Vertex v2 = new Vertex(v.X, v.Y, v.Z + 0.01f, 0, 0, -1, tX + 0.25f, tY);

                        //the right-top vertex
                        v.X = offset.X + size; v.Y = offset.Y + size; v.Z = 0;
                        M.Rotate2D(rotation, ref v.X, ref v.Z);
                        v += start; Vertex v3 = new Vertex(v.X, v.Y, v.Z + 0.01f, 0, 0, -1, tX + 0.25f, tY - 0.25f);

                        //the left-top vertex
                        v.X = offset.X; v.Y = offset.Y + size; v.Z = 0;
                        M.Rotate2D(rotation, ref v.X, ref v.Z);
                        v += start; Vertex v4 = new Vertex(v.X, v.Y, v.Z + 0.01f, 0, 0, -1, tX, tY - 0.25f);

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
                id = SpeedPostObj.getTrItemID(idlocation);
            }
            //create the shape primitive
            short[] newTList = new short[NumIndices];
            for (i = 0; i < NumIndices; i++) newTList[i] = TriangleListIndices[i];
            VertexPositionNormalTexture[] newVList = new VertexPositionNormalTexture[NumVertices];
            for (i = 0; i < NumVertices; i++) newVList[i] = VertexList[i];
            IndexBuffer IndexBuffer = new IndexBuffer(viewer.GraphicsDevice, typeof(short),
                                                            NumIndices, BufferUsage.WriteOnly);
            IndexBuffer.SetData(newTList);
            shapePrimitive = new ShapePrimitive(material, new SharedShape.VertexBufferSet(newVList, viewer.GraphicsDevice), IndexBuffer, 0, NumVertices, NumIndices / 3, new[] { -1 }, 0);

        }

        float GetTextureCoordX(char c)
        {
            float x = (c - '0') % 4 * 0.25f;
            if (c == '.') x = 0;
            else if (c == 'P') x = 0.5f;
            else if (c == 'F') x = 0.75f;
            if (x < 0) x = 0;
            if (x > 1) x = 1;
            return x;
        }

        float GetTextureCoordY(char c)
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

            frame.AddPrimitive(this.shapePrimitive.Material, this.shapePrimitive,
                        RenderPrimitiveGroup.World, ref xnaXfmWrtCamTile, ShapeFlags.None);

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

    public class LevelCrossingShape : PoseableShape, IDisposable
    {
        readonly LevelCrossingObj CrossingObj;
        readonly SoundSource Sound;
        readonly LevelCrossing Crossing;

        readonly int AnimationFrames;
        bool Opening = true;
        float AnimationKey = 0;

        public LevelCrossingShape(Viewer3D viewer, string path, WorldPosition position, ShapeFlags shapeFlags, LevelCrossingObj crossingObj)
            : base(viewer, path, position, shapeFlags)
        {
            CrossingObj = crossingObj;
            if (!CrossingObj.silent)
            {
                try
                {
                    Sound = new SoundSource(viewer, position.WorldLocation, viewer.Simulator.RoutePath + @"\\sound\\crossing.sms");
                    viewer.SoundProcess.AddSoundSource(this, new List<SoundSourceBase>() { Sound });
                }
                catch (Exception error)
                {
                    Trace.WriteLine(error);
                }
            }
            Crossing = viewer.Simulator.LevelCrossings.CreateLevelCrossing(
                position,
                from tid in CrossingObj.trItemIDList where tid.db == 0 select tid.dbID,
                from tid in CrossingObj.trItemIDList where tid.db == 1 select tid.dbID,
                CrossingObj.levelCrParameters.warningTime,
                CrossingObj.levelCrParameters.minimumDistance);
            AnimationFrames = CrossingObj.levelCrTiming.animTiming < 0 ? SharedShape.Animations[0].FrameCount : 1;
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (!CrossingObj.silent)
                Viewer.SoundProcess.RemoveSoundSource(Sound);
        }

        #endregion

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (CrossingObj.visible != true)
                return;

            if (Opening == Crossing.HasTrain)
            {
                Opening = !Crossing.HasTrain;
                if (!CrossingObj.silent) Sound.HandleEvent(Opening ? 4 : 3);
            }

            // Looping when animTiming < 0 (forwards then backwards then forwards again).
            if (CrossingObj.levelCrTiming.animTiming < 0)
            {
                if (Opening)
                    AnimationKey = 0;
                else
                    AnimationKey -= elapsedTime.ClockSeconds / CrossingObj.levelCrTiming.animTiming;
                if (AnimationKey > AnimationFrames) AnimationKey -= AnimationFrames;
            }
            else
            {
                if (Opening)
                    AnimationKey -= elapsedTime.ClockSeconds / CrossingObj.levelCrTiming.animTiming;
                else
                    AnimationKey += elapsedTime.ClockSeconds / CrossingObj.levelCrTiming.animTiming;
            }
            if (AnimationKey < 0) AnimationKey = 0;
            if (AnimationKey > AnimationFrames) AnimationKey = 1;

            for (var i = 0; i < SharedShape.Matrices.Length; ++i)
                AnimateMatrix(i, AnimationKey);

            SharedShape.PrepareFrame(frame, Location, XNAMatrices, Flags);
        }
    }

    public class RoadCarShape : AnimatedShape
    {
        public RoadCarShape(Viewer3D viewer, string path)
            : base(viewer, path, new WorldPosition())
        {
        }
    }

    public class ShapePrimitive : RenderPrimitive
    {
        public Material Material { get; private set; }
        public int[] Hierarchy { get; private set; } // the hierarchy from the sub_object
        public int HierarchyIndex { get; private set; } // index into the hiearchy array which provides pose for this primitive

        VertexBuffer VertexBuffer;
        VertexDeclaration VertexDeclaration;
        int VertexBufferStride;
        IndexBuffer IndexBuffer;
        int MinVertexIndex;
        int NumVerticies;
        int PrimitiveCount;

        public ShapePrimitive()
        {
        }

        public ShapePrimitive(Material material, SharedShape.VertexBufferSet vertexBufferSet, IndexBuffer indexBuffer, int minVertexIndex, int numVerticies, int primitiveCount, int[] hierarchy, int hierarchyIndex)
        {
            Material = material;
            VertexBuffer = vertexBufferSet.Buffer;
            VertexDeclaration = vertexBufferSet.Declaration;
            VertexBufferStride = vertexBufferSet.Declaration.GetVertexStrideSize(0);
            IndexBuffer = indexBuffer;
            MinVertexIndex = minVertexIndex;
            NumVerticies = numVerticies;
            PrimitiveCount = primitiveCount;
            Hierarchy = hierarchy;
            HierarchyIndex = hierarchyIndex;
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            if (PrimitiveCount > 0)
            {
                // TODO consider sorting by Vertex set so we can reduce the number of SetSources required.
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

    public class SharedShape
    {
        static List<string> ShapeWarnings = new List<string>();

        // This data is common to all instances of the shape
        public List<string> MatrixNames = new List<string>();
        public Matrix[] Matrices = new Matrix[0];  // the original natural pose for this shape - shared by all instances
        public animations Animations;
        public LodControl[] LodControls;
        public bool HasNightSubObj;

        readonly Viewer3D Viewer;
        readonly string FilePath;

        /// <summary>
        /// Create an empty shape used as a sub when the shape won't load
        /// </summary>
        /// <param name="viewer"></param>
        public SharedShape(Viewer3D viewer)
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
        public SharedShape(Viewer3D viewer, string filePath)
        {
            Viewer = viewer;
            FilePath = filePath;
            LoadContent();
        }

        /// <summary>
        /// Only one copy of the model is loaded regardless of how many copies are placed in the scene.
        /// </summary>
        void LoadContent()
        {
            Trace.Write("S");
            var sFile = new SFile(FilePath);

            var textureFlags = Helpers.TextureFlags.None;
            if (File.Exists(FilePath + "d"))
            {
                var sdFile = new SDFile(FilePath + "d");
                textureFlags = (Helpers.TextureFlags)sdFile.shape.ESD_Alternative_Texture;
                HasNightSubObj = sdFile.shape.ESD_SubObj;
            }
            if (FilePath.ToUpperInvariant().Contains(@"\TRAINS\TRAINSET\"))
                textureFlags |= Helpers.TextureFlags.TrainSet;

            var matrixCount = sFile.shape.matrices.Count;
            MatrixNames.Capacity = matrixCount;
            Matrices = new Matrix[matrixCount];
            for (var i = 0; i < matrixCount; ++i)
            {
                MatrixNames.Add(sFile.shape.matrices[i].Name.ToUpper());
                Matrices[i] = XNAMatrixFromMSTS(sFile.shape.matrices[i]);
            }
            Animations = sFile.shape.animations;

#if DEBUG_SHAPE_HIERARCHY
            Console.WriteLine("Shape {0}:", Path.GetFileNameWithoutExtension(FilePath).ToUpper());
            for (var i = 0; i < MatrixNames.Count; ++i)
                Console.WriteLine("  Matrix {0,-2} {1}", i, MatrixNames[i]);
#endif
            LodControls = (from lod_control lod in sFile.shape.lod_controls
                           select new LodControl(lod, textureFlags, sFile, this)).ToArray();
            if (LodControls.Length == 0)
                throw new InvalidDataException("Shape file missing lod_control section");
        }

        public class LodControl
        {
            public DistanceLevel[] DistanceLevels;

            public LodControl(lod_control MSTSlod_control, Helpers.TextureFlags textureFlags, SFile sFile, SharedShape sharedShape)
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
                    dl.Mark();
            }
        }

        public class DistanceLevel
        {
            public float ViewingDistance;
            public float ViewSphereRadius;
            public SubObject[] SubObjects;

            public DistanceLevel(distance_level MSTSdistance_level, Helpers.TextureFlags textureFlags, SFile sFile, SharedShape sharedShape)
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

#if DEBUG_SHAPE_HIERARCHY
                var index = 0;
                SubObjects = (from sub_object obj in MSTSdistance_level.sub_objects
                              select new SubObject(obj, MSTSdistance_level.distance_level_header.hierarchy, textureFlags, index++, sFile, sharedShape)).ToArray();
#else
                SubObjects = (from sub_object obj in MSTSdistance_level.sub_objects
                              select new SubObject(obj, MSTSdistance_level.distance_level_header.hierarchy, textureFlags, sFile, sharedShape)).ToArray();
#endif
                if (SubObjects.Length == 0)
                    throw new InvalidDataException("Shape file missing sub_object");
            }

            [CallOnThread("Loader")]
            internal void Mark()
            {
                foreach (var so in SubObjects)
                    so.Mark();
            }
        }

        public class SubObject
        {
            static readonly SceneryMaterialOptions[] UVTextureAddressModeMap = new[] {
                SceneryMaterialOptions.None,
                SceneryMaterialOptions.TextureAddressModeMirror,
                SceneryMaterialOptions.TextureAddressModeClamp,
                SceneryMaterialOptions.TextureAddressModeBorder,
            };

            static readonly Dictionary<string, SceneryMaterialOptions> ShaderNames = new Dictionary<string, SceneryMaterialOptions> {
                { "Tex", SceneryMaterialOptions.None },
                { "TexDiff", SceneryMaterialOptions.Diffuse },
                { "BlendATex", SceneryMaterialOptions.AlphaBlendingBlend },
                { "BlendATexDiff", SceneryMaterialOptions.AlphaBlendingBlend | SceneryMaterialOptions.Diffuse },
                { "AddATex", SceneryMaterialOptions.AlphaBlendingAdd },
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
            public SubObject(sub_object sub_object, int[] hierarchy, Helpers.TextureFlags textureFlags, int index, SFile sFile, SharedShape sharedShape)
#else
            public SubObject(sub_object sub_object, int[] hierarchy, Helpers.TextureFlags textureFlags, SFile sFile, SharedShape sharedShape)
#endif
            {
#if DEBUG_SHAPE_HIERARCHY
                Console.WriteLine("      Sub object {0}:", index);
#endif
                var vertexBufferSet = new VertexBufferSet(sub_object, sFile, sharedShape.Viewer.GraphicsDevice);


#if OPTIMIZE_SHAPES_ON_LOAD
                var primitiveMaterials = sub_object.primitives.Cast<primitive>().Select((primitive) =>
#else
                var primitiveIndex = 0;
                ShapePrimitives = new ShapePrimitive[sub_object.primitives.Count];
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
                            Trace.TraceWarning("Skipped unknown texture addressing mode {1} first seen in shape {0}", sharedShape.FilePath, lightModelConfiguration.uv_ops[0].TexAddrMode);
                            ShapeWarnings.Add("texture_addressing_mode:" + lightModelConfiguration.uv_ops[0].TexAddrMode);
                        }

                    if (primitiveState.alphatestmode == 1)
                        options |= SceneryMaterialOptions.AlphaTest;

                    if (ShaderNames.ContainsKey(sFile.shape.shader_names[primitiveState.ishader]))
                        options |= ShaderNames[sFile.shape.shader_names[primitiveState.ishader]];
                    else if (!ShapeWarnings.Contains("shader_name:" + sFile.shape.shader_names[primitiveState.ishader]))
                    {
                        Trace.TraceWarning("Skipped unknown shader name {1} first seen in shape {0}", sharedShape.FilePath, sFile.shape.shader_names[primitiveState.ishader]);
                        ShapeWarnings.Add("shader_name:" + sFile.shape.shader_names[primitiveState.ishader]);
                    }

                    if (12 + vertexState.LightMatIdx >= 0 && 12 + vertexState.LightMatIdx < VertexLightModeMap.Length)
                        options |= VertexLightModeMap[12 + vertexState.LightMatIdx];
                    else if (!ShapeWarnings.Contains("lighting_model:" + vertexState.LightMatIdx))
                    {
                        Trace.TraceWarning("Skipped unknown lighting model index {1} first seen in shape {0}", sharedShape.FilePath, vertexState.LightMatIdx);
                        ShapeWarnings.Add("lighting_model:" + vertexState.LightMatIdx);
                    }

                    if ((textureFlags & Helpers.TextureFlags.Night) != 0)
                        options |= SceneryMaterialOptions.NightTexture;

                    Material material;
                    if (primitiveState.tex_idxs.Length != 0)
                    {
                        var texture = sFile.shape.textures[primitiveState.tex_idxs[0]];
                        var imageName = sFile.shape.images[texture.iImage];
                        material = sharedShape.Viewer.MaterialManager.Load("Scenery", Helpers.GetShapeTextureFile(sharedShape.Viewer.Simulator, textureFlags, sharedShape.FilePath, imageName), (int)options, texture.MipMapLODBias);
                    }
                    else
                    {
                        material = sharedShape.Viewer.MaterialManager.Load("Scenery", null, (int)options);
                    }

#if DEBUG_SHAPE_HIERARCHY
                    Console.Write("        Primitive {0}: prim_state_idx={1,-2} ivtx_state={2,-2} imatrix={3,-2}", primitiveIndex, primitive.prim_state_idx, primitiveState.ivtx_state, vertexState.imatrix);
                    var debugMatrix = vertexState.imatrix;
                    while (debugMatrix >= 0)
                    {
                        Console.Write(" {0}", sharedShape.MatrixNames[debugMatrix]);
                        debugMatrix = hierarchy[debugMatrix];
                    }
                    Console.WriteLine();
#endif

#if OPTIMIZE_SHAPES_ON_LOAD
                    return new { Key = material.ToString() + "/" + vertexState.imatrix.ToString(), Primitive = primitive, Material = material, HierachyIndex = vertexState.imatrix };
                }).ToArray();
#else
                    var indexData = new List<ushort>(primitive.indexed_trilist.vertex_idxs.Count * 3);
                    foreach (vertex_idx vertex_idx in primitive.indexed_trilist.vertex_idxs)
                    {
                        indexData.Add((ushort)vertex_idx.a);
                        indexData.Add((ushort)vertex_idx.b);
                        indexData.Add((ushort)vertex_idx.c);
                    }

                    var indexBuffer = new IndexBuffer(sharedShape.Viewer.GraphicsDevice, typeof(short), indexData.Count, BufferUsage.WriteOnly);
                    indexBuffer.SetData(indexData.ToArray());
                    ShapePrimitives[primitiveIndex] = new ShapePrimitive(material, vertexBufferSet, indexBuffer, indexData.Min(), indexData.Max() - indexData.Min() + 1, indexData.Count / 3, hierarchy, vertexState.imatrix);
                    ++primitiveIndex;
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
#endif
            }

            [CallOnThread("Loader")]
            internal void Mark()
            {
                foreach (var prim in ShapePrimitives)
                    prim.Mark();
            }
        }

        public class VertexBufferSet
        {
            public VertexBuffer Buffer;
            public VertexDeclaration Declaration;
            public int VertexCount;

            public VertexBufferSet(VertexPositionNormalTexture[] vertexData, GraphicsDevice graphicsDevice)
            {
                VertexCount = vertexData.Length;
                Declaration = new VertexDeclaration(graphicsDevice, VertexPositionNormalTexture.VertexElements);
                Buffer = new VertexBuffer(graphicsDevice, VertexPositionNormalTexture.SizeInBytes * VertexCount, BufferUsage.WriteOnly);
                Buffer.SetData(vertexData);
            }

            public VertexBufferSet(sub_object sub_object, SFile sFile, GraphicsDevice graphicsDevice)
                : this(CreateVertexData(sub_object, sFile), graphicsDevice)
            {
            }

            static VertexPositionNormalTexture[] CreateVertexData(sub_object sub_object, SFile sFile)
            {
                // TODO - deal with vertex sets that have various numbers of texture coordinates - ie 0, 1, 2 etc
                return (from vertex vertex in sub_object.vertices
                        select XNAVertexPositionNormalTextureFromMSTS(vertex, sFile.shape)).ToArray();
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
        }

        Matrix XNAMatrixFromMSTS(matrix MSTSMatrix)
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

        public void PrepareFrame(RenderFrame frame, WorldPosition location, Matrix[] animatedXNAMatrices, ShapeFlags flags)
        {
            PrepareFrame(frame, location, animatedXNAMatrices, null, flags);
        }

        public void PrepareFrame(RenderFrame frame, WorldPosition location, Matrix[] animatedXNAMatrices, bool[] subObjVisible, ShapeFlags flags)
        {
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
                var chosenDistanceLevelIndex = lodControl.DistanceLevels.Length - 1;
                // If this LOD group is not in the FOV, skip the whole LOD group.
                if (!Viewer.Camera.InFOV(mstsLocation, lodControl.DistanceLevels[chosenDistanceLevelIndex].ViewSphereRadius))
                    continue;
                while ((chosenDistanceLevelIndex > 0) && Viewer.Camera.InRange(mstsLocation, lodControl.DistanceLevels[chosenDistanceLevelIndex - 1].ViewSphereRadius, lodControl.DistanceLevels[chosenDistanceLevelIndex - 1].ViewingDistance))
                    chosenDistanceLevelIndex--;
                var chosenDistanceLevel = lodControl.DistanceLevels[chosenDistanceLevelIndex];
                // The 1st subobject (note that index 0 is the main object itself) is hidden during the day if HasNightSubObj is true.
                foreach (var subObject in chosenDistanceLevel.SubObjects.Where((so, i) => (subObjVisible == null || subObjVisible[i]) && (i != 1 || !HasNightSubObj || Viewer.MaterialManager.sunDirection.Y < 0)))
                {
                    foreach (var shapePrimitive in subObject.ShapePrimitives)
                    {
                        var xnaMatrix = Matrix.Identity;
                        var hi = shapePrimitive.HierarchyIndex;
                        while (hi >= 0 && hi < shapePrimitive.Hierarchy.Length && shapePrimitive.Hierarchy[hi] != -1)
                        {
                            Matrix.Multiply(ref xnaMatrix, ref animatedXNAMatrices[hi], out xnaMatrix);
                            hi = shapePrimitive.Hierarchy[hi];
                        }
                        Matrix.Multiply(ref xnaMatrix, ref xnaDTileTranslation, out xnaMatrix);

                        // TODO make shadows depend on shape overrides

                        frame.AddAutoPrimitive(mstsLocation, chosenDistanceLevel.ViewSphereRadius, chosenDistanceLevel.ViewingDistance,
                            shapePrimitive.Material, shapePrimitive, RenderPrimitiveGroup.World, ref xnaMatrix, flags);
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
                lod.Mark();
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
        public TrItemLabel(Viewer3D viewer, WorldPosition position, TrObject trObj)
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