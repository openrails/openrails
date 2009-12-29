/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.Collections;
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

namespace ORTS
{
    public class StaticShape
    {
        public WorldPosition Location;
        
        public SharedShape SharedShape;
        public Viewer3D Viewer;

        /// <summary>
        /// Construct and initialize the class
        /// </summary>
        public StaticShape(Viewer3D viewer, string path, WorldPosition position)
        {
            Viewer = viewer;
            Location = position;
            SharedShape = SharedShapeManager.Get(Viewer, path);
        }

        public virtual void PrepareFrame(RenderFrame frame, float elapsedSeconds )
        {
            SharedShape.PrepareFrame(frame, Location);
        }
    }


    /// <summary>
    /// Has a heirarchy of objects that can be moved by adjusting the XNAMatrices
    /// at each node.
    /// </summary>
    public class PoseableShape : StaticShape
    {
        public Matrix[] XNAMatrices = null;  // the positions of the subobjects

        /// <summary>
        /// Construct and initialize the class
        /// </summary>
        public PoseableShape(Viewer3D viewer, string path, WorldPosition initialPosition)
            : base(viewer, path, initialPosition)
        {
            XNAMatrices = new Matrix[SharedShape.Matrices.Length];
            for (int iMatrix = 0; iMatrix < SharedShape.Matrices.Length; ++iMatrix)
                XNAMatrices[iMatrix] = SharedShape.Matrices[iMatrix];
        }


        public override void PrepareFrame(RenderFrame frame, float elapsedSeconds )
        {
            SharedShape.PrepareFrame( frame, Location, XNAMatrices);
        }

        /// <summary>
        /// Adjust the pose of the specified node to the frame position specifed by key.
        /// </summary>
        /// <param name="initialPose"></param>
        /// <param name="anim_node"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public void AnimateMatrix( int iMatrix, float key)
        {
            if (SharedShape.Animations == null )
                return;  // animation is missing

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
                    frame2 = controller.Count;

                float amount = (key - frame1) / (frame2 - frame1);


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
                if (position1.GetType() == typeof(tcb_key)) // a tcb_key sets an absolute rotation, vs rotating the existing matrix
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
        public AnimatedShape(Viewer3D viewer, string path, WorldPosition initialPosition)
            : base(viewer, path, initialPosition)
        {
        }

        public override void PrepareFrame(RenderFrame frame, float elapsedSeconds )
        {
            // if the shape has animations
            if (SharedShape.Animations != null && SharedShape.Animations[0].FrameCount > 1)
            {
                // Compute the animation key based on framerate etc
                // ie, with 8 frames of animation, the key will advance from 0 to 8 at the specified speed.
                AnimationKey += ((float)SharedShape.Animations[0].FrameRate / 10f) * elapsedSeconds;
                while (AnimationKey >= SharedShape.Animations[0].FrameCount) AnimationKey -= SharedShape.Animations[0].FrameCount;
                while (AnimationKey < -0.00001) AnimationKey += SharedShape.Animations[0].FrameCount;

                // Update the pose for each matrix
                for (int iMatrix = 0; iMatrix < SharedShape.Matrices.Length; ++iMatrix)
                    AnimateMatrix(iMatrix, AnimationKey);
            }
            SharedShape.PrepareFrame(frame, Location, XNAMatrices);
        }
    }


    public class SwitchTrackShape : PoseableShape
    {
        TrJunctionNode TrJunctionNode;  // has data on current aligment for the switch
        uint MainRoute;                  // 0 or 1 - which route is considered the main route

        protected float AnimationKey = 0.0f;  // tracks position of points as they move left and right


        public SwitchTrackShape(Viewer3D viewer, string path, WorldPosition position, TrJunctionNode trj ): base( viewer, path, position )
        {
            TrJunctionNode = trj;
            TrackShape TS = viewer.Simulator.TSectionDat.TrackShapes.Get(TrJunctionNode.ShapeIndex);
            MainRoute = TS.MainRoute;
        }

        public override void PrepareFrame(RenderFrame frame, float elapsedClockSeconds )
        {
            // ie, with 2 frames of animation, the key will advance from 0 to 1
            if (TrJunctionNode.SelectedRoute == MainRoute)
            {
                if (AnimationKey > 0.001) AnimationKey -= 0.002f * elapsedClockSeconds*1000.0f;
                if (AnimationKey < 0.001) AnimationKey = 0;
            }
            else
            {
                if (AnimationKey < 0.999) AnimationKey += 0.002f * elapsedClockSeconds*1000.0f;
                if (AnimationKey > 0.999) AnimationKey = 1.0f;
            }

            // Update the pose
            for (int iMatrix = 0; iMatrix < SharedShape.Matrices.Length; ++iMatrix)
                AnimateMatrix(iMatrix, AnimationKey);

            SharedShape.PrepareFrame(frame, Location, XNAMatrices);
        }
    } // class SwitchTrackShape

    /// <summary>
    /// Conserves memory by sharing the basic shape data with multiple instances in the scene.
    /// </summary>
    public class SharedShapeManager 
    {
        public static SharedShape Get(Viewer3D viewer, string path)
        {
            if (  !SharedShapes.ContainsKey(path))
            {
                // We haven't set up this shape yet, so go ahead and add it
                try
                {
                    SharedShape shape = new SharedShape(viewer, path);
                    SharedShapes.Add(path, shape);
                    return shape;
                }
                catch (System.Exception error)
                {
                    Console.Error.WriteLine("Error loading shape: " + path + "\r\n   " + error.Message);
                    if (EmptyShape == null)
                        EmptyShape = new SharedShape(viewer);
                    SharedShapes.Add(path, EmptyShape);
                    return EmptyShape;
                }
            }
            else
            {
                // The shape is already set up
                return SharedShapes[path];
            }
        }

        private static Dictionary<string, SharedShape> SharedShapes = new Dictionary<string, SharedShape>();

        private static SharedShape EmptyShape = null;
    }



    public class ShapePrimitive : RenderPrimitive
    {
        public Material Material;

        public VertexDeclaration VertexDeclaration;
        public VertexBuffer VertexBuffer;
        public IndexBuffer IndexBuffer;
        public  int IndexCount;         // the number of indexes in the index buffer for each primitive
        public  int VertexCount;        // the number of vertices in the vertex buffer for each primitive
        public int PrimMatrixIndex;     // index into the instance matrix list which provides pose for this primitive
        public int MinVertex;           // the first vertex index used by this primitive
        public int NumVertices;         // the number of vertex indexes used by this primitive

        /// <summary>
        /// This is called when the game should draw itself.
        /// Executes in RenderProcess thread.
        /// </summary>
        public void Draw(GraphicsDevice graphicsDevice)
        {
            // TODO consider sorting by Vertex set so we can reduce the number of SetSources required.
            graphicsDevice.VertexDeclaration = VertexDeclaration;
            graphicsDevice.Vertices[0].SetSource(VertexBuffer, 0, VertexDeclaration.GetVertexStrideSize(0));
            graphicsDevice.Indices = IndexBuffer;
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, MinVertex, NumVertices, 0, IndexCount / 3);
        }

    }



    public class SharedShape
    {
        Viewer3D Viewer;
        string FilePath;
        GraphicsDevice GraphicsDevice;

        private float ViewingDistance;
        private float ViewSphereRadius;

        // This data is common to all instances of the shape
        public int[] Hierarchy;
        public string[] MatrixNames;
        public Matrix[] Matrices;               // the original natural pose for this shape - shared by all instances
        public animations Animations = null;

        // This is the data unique for each primitive
        ShapePrimitive[] ShapePrimitives;

        /// <summary>
        /// Create an empty shape
        /// </summary>
        /// <param name="viewer"></param>
        public SharedShape(Viewer3D viewer)
        {
            Viewer = viewer;
            FilePath = "Empty";
            GraphicsDevice = viewer.GraphicsDevice;
            ViewingDistance = 100;
            ViewSphereRadius = 10;
            Hierarchy = new int[0];
            MatrixNames = new string[0];
            Matrices = new Matrix[0];
            Animations = null;
            ShapePrimitives = new ShapePrimitive[0];
        }

        public SharedShape(Viewer3D viewer, string path )
            
        {
            Viewer = viewer;
            GraphicsDevice = viewer.GraphicsDevice;
            FilePath = path;
            LoadContent( path);
        }

        /// <summary>
        /// Only one copy of the model is loaded regardless of how many copies are placed in the scene.
        /// </summary>
        private void LoadContent( string FilePath)
        {
            // TODO a temp fix for trackobj's converted to static objects
            if (!File.Exists(FilePath))
            {
                string globalPath = Viewer.Simulator.RoutePath + @"\GLOBAL\SHAPES\" + Path.GetFileName(FilePath);
                if (!File.Exists(globalPath))
                {
                    globalPath = Viewer.Simulator.BasePath + @"\GLOBAL\SHAPES\" + Path.GetFileName(FilePath);
                    if (!File.Exists(globalPath))
                        throw new System.Exception("Can't find file " + FilePath);
                }
                FilePath = globalPath;
            }
            Console.Write( "S" );
            SFile sFile = new SFile(FilePath);

            // determine the correct texture folder, 
            //    trainsets have their textures in the same folder as the shape, 
            //    route scenery has their textures in a separate textures folder
            string textureFolder;
            if (FilePath.ToUpper().Contains(@"\TRAINS\TRAINSET\"))   // TODO this is pretty crude
                textureFolder = Path.GetDirectoryName(FilePath);
            else
                textureFolder = Viewer.Simulator.RoutePath + @"\textures";  // TODO, and this shouldn't be hard coded

            // for now, use one load, but set it to the farthest viewing distance
            ViewingDistance = sFile.shape.lod_controls[0].distance_levels[sFile.shape.lod_controls[0].distance_levels.Count-1].distance_level_header.dlevel_selection;
            ViewSphereRadius = sFile.shape.volumes[0].Radius;

            // get a total count of drawing primitives
            int primCount = 0;
            foreach (sub_object sub_object in sFile.shape.lod_controls[0].distance_levels[0].sub_objects)
                primCount += sub_object.primitives.Count;

            // set up the buffers to hold the drawing primtives
            ShapePrimitives = new ShapePrimitive[primCount]; 

            // Hierarchy and matrix names are common to all instances
            Hierarchy = sFile.shape.lod_controls[0].distance_levels[0].distance_level_header.hierarchy;
            MatrixNames = new string[Hierarchy.Length];
            Matrices = new Matrix[Hierarchy.Length];
            for (int i = 0; i < Hierarchy.Length; ++i)
            {
                MatrixNames[i] = sFile.shape.matrices[i].Name.ToUpper();
                Matrices[i] = XNAMatrixFromMSTS(sFile.shape.matrices[i]);
            }
            Animations = sFile.shape.animations;


            // read in the drawing primitives
            int iPrim = 0;
            foreach (sub_object sub_object in sFile.shape.lod_controls[0].distance_levels[0].sub_objects)
            {
                int vertexCount = sub_object.vertices.Count;

                // Set up one vertex buffer for each sub_object, all primitives will share this buffer
                VertexPositionNormalTexture[] vertexData = new VertexPositionNormalTexture[vertexCount];

                for (int iVert = 0; iVert < vertexCount; ++iVert)
                {
                    MSTS.vertex MSTSvertex = sub_object.vertices[iVert];
                    vertexData[iVert] = XNAVertexPositionNormalTextureFromMSTS(MSTSvertex, sFile.shape);
                }
                VertexDeclaration subObjectVertexDeclaration = new VertexDeclaration(this.GraphicsDevice, VertexPositionNormalTexture.VertexElements);
                VertexBuffer subObjectVertexBuffer = new VertexBuffer(GraphicsDevice, VertexPositionNormalTexture.SizeInBytes * vertexData.Length, BufferUsage.WriteOnly);
                subObjectVertexBuffer.SetData(vertexData);

                // For each primitive, set up an effect and index buffer
                foreach (primitive primitive in sub_object.primitives)
                {
                    ShapePrimitive shapePrimitive = new ShapePrimitive();

                    prim_state prim_state = sFile.shape.prim_states[ primitive.prim_state_idx ];
                    vtx_state vtx_state = sFile.shape.vtx_states[ prim_state.ivtx_state];

                    int options = 0;
                    // eliminate diffuse color on trees
                    if (vtx_state.LightMatIdx == -9 || vtx_state.LightMatIdx == -10)
                        options |= 1;
                    // transparency test   TODO, add capability to handle alpha blending properly
                    if (prim_state.alphatestmode == 1 
                        || sFile.shape.shader_names[prim_state.ishader].StartsWith("BlendA", StringComparison.OrdinalIgnoreCase))
                        options |= 2;

                    if (prim_state.tex_idxs.Length == 0)
                    {   // untextured objects get a blank texture
                        shapePrimitive.Material = (SceneryMaterial)Materials.Load( Viewer.RenderProcess, "SceneryMaterial", null, options );  
                    }
                    else
                    {
                        texture texture = sFile.shape.textures[prim_state.tex_idxs[0]];
                        string imageName = sFile.shape.images[texture.iImage];
                        shapePrimitive.Material = Materials.Load( Viewer.RenderProcess, 
                            "SceneryMaterial", textureFolder + @"\" + imageName, options);
                    }

                    int iMatrix = sFile.shape.vtx_states[sFile.shape.prim_states[primitive.prim_state_idx].ivtx_state].imatrix;
                    shapePrimitive.PrimMatrixIndex = iMatrix;

                    int indexCount = primitive.indexed_trilist.vertex_idxs.Count * 3;

                    short[] indexData = new short[indexCount];

                    int iIndex = 0;
                    foreach (vertex_idx vertex_idx in primitive.indexed_trilist.vertex_idxs)
                    {
                        indexData[iIndex++] = (short)vertex_idx.a;
                        indexData[iIndex++] = (short)vertex_idx.b;
                        indexData[iIndex++] = (short)vertex_idx.c;
                    }

                    shapePrimitive.IndexCount = indexCount;
                    shapePrimitive.VertexCount = vertexCount;

                    shapePrimitive.IndexBuffer = new IndexBuffer(GraphicsDevice, sizeof(short) * indexCount, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
                    shapePrimitive.IndexBuffer.SetData<short>(indexData);

                    shapePrimitive.VertexBuffer = subObjectVertexBuffer;
                    shapePrimitive.VertexDeclaration = subObjectVertexDeclaration;

                    // Record range of vertices involved in this primitive as MinVertex and NumVertices
                    // TODO Extract this from the sub_object header
                    bool vertex_set_found = false;
                    foreach( vertex_set vertex_set in sub_object.vertex_sets )
                        if (vertex_set.VtxStateIdx == prim_state.ivtx_state)
                        {
                            shapePrimitive.MinVertex = vertex_set.StartVtxIdx;
                            shapePrimitive.NumVertices = vertex_set.VtxCount;
                            vertex_set_found = true;
                            break;
                        }
                    if (!vertex_set_found)
                        throw new System.Exception("vertex_set not found for vtx_state = " + prim_state.ivtx_state.ToString());
                    ShapePrimitives[iPrim] = shapePrimitive;
                    ++iPrim;
                }
            }
        }

        private VertexPositionTexture XNAVertexPositionTextureFromMSTS(vertex MSTSvertex, shape MSTSshape)
        {

            MSTS.point MSTSPosition = MSTSshape.points[MSTSvertex.ipoint];
            MSTS.uv_point MSTSTextureCoordinate;
            if( MSTSvertex.vertex_uvs.Length > 0 )  // there are files without UVS points - ie mst-sawmill-wh.s in BECR route
                MSTSTextureCoordinate = MSTSshape.uv_points[MSTSvertex.vertex_uvs[0]];
            else
                MSTSTextureCoordinate = MSTSshape.uv_points[0];

            VertexPositionTexture XNAVertex = new VertexPositionTexture();
            XNAVertex.Position = new Vector3(MSTSPosition.X, MSTSPosition.Y, -MSTSPosition.Z);
            XNAVertex.TextureCoordinate = new Vector2(MSTSTextureCoordinate.U, MSTSTextureCoordinate.V);

            return XNAVertex;
        }

        private VertexPositionNormalTexture XNAVertexPositionNormalTextureFromMSTS(vertex MSTSvertex, shape MSTSshape)
        {

            MSTS.point MSTSPosition = MSTSshape.points[MSTSvertex.ipoint];
            MSTS.vector MSTSNormal = MSTSshape.normals[MSTSvertex.inormal];
            MSTS.uv_point MSTSTextureCoordinate;
            if (MSTSvertex.vertex_uvs.Length > 0)
                MSTSTextureCoordinate = MSTSshape.uv_points[MSTSvertex.vertex_uvs[0]];
            else
                MSTSTextureCoordinate = new uv_point(0,0);  // TODO use a simpler vertex description when no UV's in use

            VertexPositionNormalTexture XNAVertex = new VertexPositionNormalTexture();
            XNAVertex.Position = new Vector3(MSTSPosition.X, MSTSPosition.Y, -MSTSPosition.Z);
            XNAVertex.Normal = new Vector3(MSTSNormal.X, MSTSNormal.Y, -MSTSNormal.Z);
            XNAVertex.TextureCoordinate = new Vector2(MSTSTextureCoordinate.U, MSTSTextureCoordinate.V);

            return XNAVertex;
        }
        private Matrix XNAMatrixFromMSTS(MSTS.matrix MSTSMatrix)
        {
            Matrix XNAMatrix = Matrix.Identity;

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

        /// <summary>
        /// This is called by the individual instances of the shape when it should draw itself at the specified location
        /// </summary>
        public void PrepareFrame( RenderFrame frame, WorldPosition location)
        {
            PrepareFrame(frame, location, Matrices);
        }

        /// <summary>
        /// This is called by the individual instances of the shape when it should draw itself at the specified location
        /// with individual matrices animated as shown.
        /// </summary>
        public void PrepareFrame( RenderFrame frame, WorldPosition location, Matrix[] animatedXNAMatrices )
        {
            // Locate relative to the camera
            int dTileX = location.TileX - Viewer.Camera.TileX;
            int dTileZ = location.TileZ - Viewer.Camera.TileZ;
            Matrix xnaDTileTranslation = Matrix.CreateTranslation(dTileX * 2048, 0, -dTileZ * 2048);  // object is offset from camera this many tiles
            xnaDTileTranslation = location.XNAMatrix * xnaDTileTranslation;
            
            // Cull
            if (!Viewer.Camera.CanSee(xnaDTileTranslation, ViewSphereRadius, ViewingDistance))  
                return;

            // for each primitive
            for (int iPrim = 0; iPrim < ShapePrimitives.Length; ++iPrim) 
            {
                ShapePrimitive shapePrimitive = ShapePrimitives[iPrim];
                Matrix xnaMatrix = Matrix.Identity;
                int iNode = shapePrimitive.PrimMatrixIndex;
                while (iNode != -1)
                {
                    xnaMatrix *= animatedXNAMatrices[iNode];         // TODO, can we reduce memory allocations during this matrix math
                    iNode = Hierarchy[iNode];
                }
                xnaMatrix *= xnaDTileTranslation;

                frame.AddPrimitive(shapePrimitive.Material, shapePrimitive, ref xnaMatrix);
            } // for each primitive
             
        }// PrepareFrame()


    }// class SharedShape

}
