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
    public class StaticShape: DrawableGameComponent
    {
        public WorldPosition Location;
        
        public SharedShape SharedShape;
        public Viewer Viewer;

        /// <summary>
        /// Construct and initialize the class
        /// </summary>
        public StaticShape(Viewer viewer, string path, WorldPosition position): base( viewer )
        {
            Viewer = viewer;
            Location = position;
            SharedShape = SharedShapeManager.Get(Viewer, path);
        }

        public override void Draw(GameTime gameTime)
        {
            SharedShape.Draw(Location);
            base.Draw( gameTime);
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
        public PoseableShape(Viewer viewer, string path, WorldPosition initialPosition)
            : base(viewer, path, initialPosition)
        {
            XNAMatrices = new Matrix[SharedShape.Matrices.Length];
            for (int iMatrix = 0; iMatrix < SharedShape.Matrices.Length; ++iMatrix)
                XNAMatrices[iMatrix] = SharedShape.Matrices[iMatrix];
        }


        public override void Draw(GameTime gameTime)
        {
            SharedShape.Draw(Location, XNAMatrices);
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
                    //xnaPose = Matrix.CreateFromQuaternion(q) *xnaPose;  //TODO, was this, but pantographs weren't moving properly
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
        public AnimatedShape(Viewer viewer, string path, WorldPosition initialPosition)
            : base(viewer, path, initialPosition)
        {
        }

        public override void Update(GameTime gameTime)
        {
            // if the shape has animations
            if (SharedShape.Animations != null && SharedShape.Animations[0].FrameCount > 1)
            {
                // Compute the animation key based on framerate etc
                // ie, with 8 frames of animation, the key will advance from 0 to 8 at the specified speed.
                AnimationKey += ((float)SharedShape.Animations[0].FrameRate / 10f) * (float)gameTime.ElapsedGameTime.TotalMilliseconds / 1000.0f;
                while (AnimationKey >= SharedShape.Animations[0].FrameCount) AnimationKey -= SharedShape.Animations[0].FrameCount;
                while (AnimationKey < -0.00001) AnimationKey += SharedShape.Animations[0].FrameCount;

                // Update the pose for each matrix
                for (int iMatrix = 0; iMatrix < SharedShape.Matrices.Length; ++iMatrix)
                    AnimateMatrix( iMatrix, AnimationKey);
            }
        }
    }


    public class SwitchTrackShape : PoseableShape
    {
        TrJunctionNode TrJunctionNode;  // has data on current aligment for the switch
        uint MainRoute;                  // 0 or 1 - which route is considered the main route

        protected float AnimationKey = 0.0f;  // tracks position of points as they move left and right


        public SwitchTrackShape(Viewer viewer, string path, WorldPosition position, TrJunctionNode trj ): base( viewer, path, position )
        {
            TrJunctionNode = trj;
            TrackShape TS = viewer.Simulator.TSectionDat.TrackShapes.Get(TrJunctionNode.ShapeIndex);
            MainRoute = TS.MainRoute;
        }

        public override void Update(GameTime gameTime)
        {
            // ie, with 2 frames of animation, the key will advance from 0 to 1
            if (TrJunctionNode.SelectedRoute == MainRoute)
            {
                if (AnimationKey > 0.001) AnimationKey -= 0.002f * gameTime.ElapsedGameTime.Milliseconds;
                if (AnimationKey < 0.001) AnimationKey = 0;
            }
            else
            {
                if (AnimationKey < 0.999) AnimationKey += 0.002f * gameTime.ElapsedGameTime.Milliseconds;
                if (AnimationKey > 0.999) AnimationKey = 1.0f;
            }

            // Update the pose
            for (int iMatrix = 0; iMatrix < SharedShape.Matrices.Length; ++iMatrix)
                AnimateMatrix(iMatrix, AnimationKey);
        }
    } // class SwitchTrackShape

    /// <summary>
    /// Conserves memory by sharing the basic shape data with multiple instances in the scene.
    /// </summary>
    public class SharedShapeManager 
    {
        public static SharedShape Get(Viewer viewer, string path)
        {
            if (!SharedShapes.ContainsKey(path))
            {
                // We haven't set up this shape yet, so go ahead and add it
                SharedShape shape = new SharedShape(viewer, path);    
                SharedShapes.Add(path, shape );
                return shape;
            }
            else
            {
                // The shape is already set up
                return SharedShapes[path];
            }
        }

        private static Dictionary<string, SharedShape> SharedShapes = new Dictionary<string, SharedShape>();

    }

  

    public class SharedShape
    {
        Viewer Viewer;
        string FilePath;
        GraphicsDevice GraphicsDevice;

        private float ViewingDistance;
        private float ViewSphereRadius;

        // This data is common to all instances of the shape
        public int[] Hierarchy;
        public string[] MatrixNames;
        public Matrix[] Matrices;               // the original natural pose for this shape - shared by all instances
        public animations Animations = null;

        // TODO, collect matrix data into sets - static objects get only one set, others have multiple sets for each instance

        // one for each primitive used in the shape TODO, change to a structure
        private Texture2D[] Textures=new Texture2D[0];  
        private int[] TextureOptions = new int[0];
        private VertexDeclaration[] VertexDeclarations = new VertexDeclaration[0];
        private VertexBuffer[] VertexBuffers = new VertexBuffer[0];
        private IndexBuffer[] IndexBuffers = new IndexBuffer[0];
        private int[] IndexCounts = new int[0];  // the number of indexes in the index buffer for each primitive
        private int[] VertexCounts = new int[0]; // the number of vertices in the vertex buffer for each primitive
        protected int[] PrimMatrixIndex = new int[0]; // index into the instance matrix list which provides pose for this primitive
        int[] MinVertex = new int[0];           // the first vertex index used by this primitive
        int[] NumVertices = new int[0];         // the number of vertex indexes used by this primitive


        public SharedShape(Viewer viewer, string path )
            
        {
            Viewer = viewer;
            GraphicsDevice = viewer.GraphicsDevice;
            FilePath = path;
            LoadContent();
        }

        /// <summary>
        /// Only one copy of the model is loaded regardless of how many copies are placed in the scene.
        /// </summary>
        protected void LoadContent()
        {
            try
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

                // FilePath = null;  TODO, should we free up this memory?

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
                Textures = new Texture2D[primCount];
                TextureOptions = new int[primCount];

                VertexBuffers = new VertexBuffer[primCount];
                VertexDeclarations = new VertexDeclaration[primCount];
                IndexBuffers = new IndexBuffer[primCount];
                IndexCounts = new int[primCount];
                VertexCounts = new int[primCount];
                PrimMatrixIndex = new int[primCount];
                MinVertex = new int[primCount];
                NumVertices = new int[primCount];

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
                        prim_state prim_state = sFile.shape.prim_states[ primitive.prim_state_idx ];
                        vtx_state vtx_state = sFile.shape.vtx_states[ prim_state.ivtx_state];
                        try
                        {
                            if (prim_state.tex_idxs.Length == 0)
                            {
                                Textures[iPrim] = Viewer.Content.Load<Texture2D>("blank");  // untextured objects get a blank texture
                            }
                            else
                            {
                                texture texture = sFile.shape.textures[prim_state.tex_idxs[0]];
                                string imageName = sFile.shape.images[texture.iImage];
                                Textures[iPrim] = SharedTextureManager.Get(GraphicsDevice, textureFolder + @"\" + imageName);
                            }
                        }
                        catch( System.Exception error )
                        {
                            Console.Error.WriteLine( "while loading " + this.FilePath + "\r\n"+error.Message);
                            Textures[iPrim] = Viewer.Content.Load<Texture2D>("blank");  // untextured objects get a blank texture
                        }

                        TextureOptions[iPrim] = 0;
                        // eliminate diffuse color on trees
                        if ( vtx_state.LightMatIdx == -9 || vtx_state.LightMatIdx == -10 )
                            TextureOptions[iPrim] |= 1;
                        // transparency test   TODO, add capability to handle alpha blending properly
                        if (prim_state.alphatestmode == 1 || sFile.shape.shader_names[prim_state.ishader].StartsWith( "BlendA",StringComparison.OrdinalIgnoreCase))
                            TextureOptions[iPrim] |= 2;


                        int iMatrix = sFile.shape.vtx_states[sFile.shape.prim_states[primitive.prim_state_idx].ivtx_state].imatrix;
                        PrimMatrixIndex[iPrim] = iMatrix;

                        int indexCount = primitive.indexed_trilist.vertex_idxs.Count * 3;

                        short[] indexData = new short[indexCount];

                        int iIndex = 0;
                        foreach (vertex_idx vertex_idx in primitive.indexed_trilist.vertex_idxs)
                        {
                            indexData[iIndex++] = (short)vertex_idx.a;
                            indexData[iIndex++] = (short)vertex_idx.b;
                            indexData[iIndex++] = (short)vertex_idx.c;
                        }

                        IndexCounts[iPrim] = indexCount;
                        VertexCounts[iPrim] = vertexCount;

                        IndexBuffers[iPrim] = new IndexBuffer(GraphicsDevice, sizeof(short) * indexCount, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
                        IndexBuffers[iPrim].SetData<short>(indexData);

                        VertexBuffers[iPrim] = subObjectVertexBuffer;
                        VertexDeclarations[iPrim] = subObjectVertexDeclaration;

                        // Record range of vertices involved in this primitive as MinVertex and NumVertices
                        // Extract this from the MSTS vertex_set statement
                        bool vertex_set_found = false;
                        foreach( vertex_set vertex_set in sub_object.vertex_sets )
                            if (vertex_set.VtxStateIdx == prim_state.ivtx_state)
                            {
                                MinVertex[iPrim] = vertex_set.StartVtxIdx;
                                NumVertices[iPrim] = vertex_set.VtxCount;
                                vertex_set_found = true;
                                break;
                            }
                        if (!vertex_set_found)
                            throw new System.Exception("vertex_set not found for vtx_state = " + prim_state.ivtx_state.ToString());

                        ++iPrim;
                    }
                }
            }
            catch (System.Exception error)
            {
                Console.Error.WriteLine("Error loading shape: " + FilePath + "\r\n\r\n" + error);
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
        public void Draw(WorldPosition location)
        {
            Draw(location, Matrices);
        }

        /// <summary>
        /// This is called by the individual instances of the shape when it should draw itself at the specified location
        /// with individual matrices animated as shown.
        /// </summary>
        public void Draw( WorldPosition location, Matrix[] animatedXNAMatrices )
        {
            // These are used to optimize texture changes
            Texture2D currentTexture = null;
            int currentTextureOptions = -1;
                    
            // Locate relative to the camera
            int dTileX = location.TileX - Viewer.Camera.TileX;
            int dTileZ = location.TileZ - Viewer.Camera.TileZ;
            Matrix xnaDTileTranslation = Matrix.CreateTranslation(dTileX * 2048, 0, -dTileZ * 2048);  // object is offset from camera this many tiles
            xnaDTileTranslation = location.XNAMatrix * xnaDTileTranslation;
            
            // Cull
            if (!Viewer.Camera.CanSee(xnaDTileTranslation, ViewSphereRadius, ViewingDistance))  
                return;


            // for each primitive
            for (int iPrim = 0; iPrim < IndexBuffers.Length; ++iPrim) 
            {
                int indexCount = IndexCounts[iPrim];    // TODO, can we calculate these instead of saving them?
                int vertexCount = VertexCounts[iPrim];
                int minVertex = MinVertex[iPrim];
                int numVertices = NumVertices[iPrim];

                // MSTS practice is to draw prims in reverse file order, this bias attempted to ensure they render in the correct order
                GraphicsDevice.RenderState.DepthBias = 0; // 0.000001f * (float)(IndexBuffers.Length - iPrim) / (float)IndexBuffers.Length;

                // Where in the world is the center of this primitive
                // Compute the matrix heiarchy   TODO - optimize this by precomputing, but for now this is simpler and clearer
                // starting with an identity matrix
                // walk the hierarchy and precompute the final matrix locations for each node
                // finally including the overall instance location in the calculation
                Matrix xnaMatrix = Matrix.Identity;
                int iNode = PrimMatrixIndex[iPrim];
                while (iNode != -1)
                {
                    xnaMatrix *= animatedXNAMatrices[iNode];
                    iNode = Hierarchy[iNode];
                }
                xnaMatrix *= xnaDTileTranslation;

                Viewer.SceneryShader.SetMatrix(xnaMatrix, Viewer.Camera.XNAView, Viewer.Camera.XNAProjection);
                  
                // Configure gpu state, with as much optimization as possible
                if (Viewer.RenderState != 2) 
                {
                    Viewer.RenderState = 2;
                    
                    GraphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
                    GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
                    GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
                    GraphicsDevice.VertexSamplerStates[0].AddressU = TextureAddressMode.Wrap;
                    GraphicsDevice.VertexSamplerStates[0].AddressV = TextureAddressMode.Wrap;
                    GraphicsDevice.RenderState.AlphaFunction = CompareFunction.Always;
                    GraphicsDevice.RenderState.AlphaTestEnable = true;
                    GraphicsDevice.RenderState.ReferenceAlpha = 200;  // setting this to 128, chain link fences become solid at distance, at 200, they become transparent
                    GraphicsDevice.RenderState.AlphaFunction = CompareFunction.GreaterEqual;        // if alpha > reference, then skip processing this pixel

                    Viewer.SetupFog();
                }
         
                VertexDeclaration vertexDeclaration = VertexDeclarations[iPrim];
                VertexBuffer vertexBuffer = VertexBuffers[iPrim];
                GraphicsDevice.VertexDeclaration = vertexDeclaration;
                GraphicsDevice.Vertices[0].SetSource(vertexBuffer, 0, vertexDeclaration.GetVertexStrideSize(0));

                IndexBuffer indexBuffer = IndexBuffers[iPrim];
                GraphicsDevice.Indices = indexBuffer;

                Texture2D texture = Textures[iPrim];  
                if (currentTexture != texture)
                {
                    Viewer.SceneryShader.Texture = texture;
                    currentTexture = texture;
                }

                int textureOptions = TextureOptions[iPrim];
                if (currentTextureOptions != textureOptions)
                {
                    currentTextureOptions = textureOptions;
                    if ( (textureOptions & 1) == 1)
                    {
                        Viewer.SceneryShader.CurrentTechnique = Viewer.SceneryShader.Techniques[1];
                    }
                    else
                    {
                        Viewer.SceneryShader.CurrentTechnique = Viewer.SceneryShader.Techniques[0];
                    }
                    if ( (textureOptions & 2) == 2)
                    {
                        GraphicsDevice.RenderState.AlphaTestEnable = true;
                    }
                    else
                    {
                        GraphicsDevice.RenderState.AlphaTestEnable = false;
                    }
                }

                // With the GPU configured, now we can draw the primitive
                Viewer.SceneryShader.Begin();
                for (int p = 0; p < Viewer.SceneryShader.CurrentTechnique.Passes.Count; ++p)
                {
                    EffectPass pass = Viewer.SceneryShader.CurrentTechnique.Passes[p];
                    pass.Begin();
                    GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, minVertex, numVertices, 0, indexCount / 3);
                    pass.End();
                }
                Viewer.SceneryShader.End();

            } // for each primitive
             
        }// Draw()


    }// class SharedShape

}
