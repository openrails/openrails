/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 
/// Principal Author:
///    Wayne Campbell
/// Contributors:
///    Rick Grout
/// 

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
        /// This constructor is for objects described by a MSTS shape file
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

        public SharedShape.VertexBufferSet VertexBufferSet;
        public IndexBuffer IndexBuffer;
        public int IndexCount;          // the number of indexes in the index buffer for each primitive
        public int MinVertex;           // the first vertex index used by this primitive
        public int NumVertices;         // the number of vertex indexes used by this primitive
        public int iHierarchy;          // index into the hiearchy array which provides pose for this primitive
        public int[] Hierarchy;         // the hierarchy from the sub_object

        /// <summary>
        /// This is called when the game should draw itself.
        /// Executes in RenderProcess thread.
        /// </summary>
        public override void Draw(GraphicsDevice graphicsDevice)
        {
            // TODO consider sorting by Vertex set so we can reduce the number of SetSources required.
            graphicsDevice.VertexDeclaration = VertexBufferSet.Declaration;
            graphicsDevice.Vertices[0].SetSource(VertexBufferSet.Buffer, 0, VertexBufferSet.Declaration.GetVertexStrideSize(0));
            graphicsDevice.Indices = IndexBuffer;
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, MinVertex, NumVertices, 0, IndexCount / 3);
        }
    }

    public class SharedShape
    {
        Viewer3D Viewer;
        string FilePath;
        private static int isNightEnabled = 0;
        
        public string textureFolder;  // Temporary

        // This data is common to all instances of the shape
        public string[] MatrixNames;
        public Matrix[] Matrices;               // the original natural pose for this shape - shared by all instances
        public animations Animations = null;

        private LodControl[] LodControls = null;
        
        /// <summary>
        /// Create an empty shape used as a sub when the shape won't load
        /// </summary>
        /// <param name="viewer"></param>
        public SharedShape(Viewer3D viewer)
        {
            Viewer = viewer;
            FilePath = "Empty";
            MatrixNames = new string[0];
            Matrices = new Matrix[0];
            Animations = null;
            LodControls = new LodControl[0];
        }

        /// <summary>
        /// MSTS shape from shape file
        /// </summary>
        /// <param name="viewer"></param>
        /// <param name="path">Path to shape's S file</param>
        public SharedShape(Viewer3D viewer, string path)
        {
            Viewer = viewer;
            FilePath = path;
            LoadContent(path);
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

            // Determine the correct texture folder. 
            // Trainsets have their textures in the same folder as the shape, 
            // route scenery has their textures in a separate textures folder
            int season = (int)Viewer.Simulator.Season;
            int weather = (int)Viewer.Simulator.Weather;
            if (FilePath.ToUpper().Contains(@"\TRAINS\TRAINSET\"))   // TODO this is pretty crude
                textureFolder = Path.GetDirectoryName(FilePath);
            else
            {
                // Check the SD file for alternative texture specification
                int altTex = 0; // Default
                string SDfilePath = FilePath + "d";
                SDFile SDFile = new SDFile(SDfilePath);
                altTex = SDFile.shape.ESD_Alternative_Texture;
                Helpers helper = new Helpers();
                textureFolder = helper.GetTextureFolder(Viewer, altTex);
                if (altTex == 257) isNightEnabled = 1;
            }

            int matrixCount = sFile.shape.matrices.Count;
            MatrixNames = new string[matrixCount];
            Matrices = new Matrix[matrixCount];
            for (int i = 0; i < matrixCount; ++i)
            {
                MatrixNames[i] = sFile.shape.matrices[i].Name.ToUpper();
                Matrices[i] = XNAMatrixFromMSTS(sFile.shape.matrices[i]);
            }
            Animations = sFile.shape.animations;

            LodControls = new LodControl[sFile.shape.lod_controls.Count];

            for (int i = 0; i < sFile.shape.lod_controls.Count; ++ i )
                LodControls[i] = new LodControl( sFile.shape.lod_controls[i], sFile , this );

            if (LodControls.Length == 0)
                throw new System.Exception("Shape file missing lod_control section");

            textureFolder = null;  // release it

        } // LoadContent

        public class LodControl
        {
            public DistanceLevel[] DistanceLevels;

            public LodControl( lod_control MSTSlod_control, SFile sFile, SharedShape sharedShape )
            {
                DistanceLevels = new DistanceLevel[ MSTSlod_control.distance_levels.Count ];

                for ( int i = 0; i < MSTSlod_control.distance_levels.Count; ++i )
                    DistanceLevels[i] = new DistanceLevel( MSTSlod_control.distance_levels[i], sFile, sharedShape );

                if (DistanceLevels.Length == 0)
                    throw new System.Exception("Shape file missing distance_level");
            }
        }

        public class DistanceLevel
        {
            public float ViewingDistance;
            public float ViewSphereRadius;
            public SubObject[] SubObjects;
            private int PrimCount = 0;  // used for auto ZBias

            public DistanceLevel( distance_level MSTSdistance_level, SFile sFile, SharedShape sharedShape )
            {
                SubObjects = new SubObject[ MSTSdistance_level.sub_objects.Count ];
                ViewingDistance = MSTSdistance_level.distance_level_header.dlevel_selection;
                // TODO, work out ViewShereRadius from all sub_object radius and centers.
                if (sFile.shape.volumes.Count > 0)
                    ViewSphereRadius = sFile.shape.volumes[0].Radius;
                else
                    ViewSphereRadius = 100;
                int[] Hierarchy = MSTSdistance_level.distance_level_header.hierarchy;
                for( int i = 0; i < MSTSdistance_level.sub_objects.Count; ++i )
                    SubObjects[i] = new SubObject( ref PrimCount, MSTSdistance_level.sub_objects[i], Hierarchy, sFile , sharedShape);

                if (SubObjects.Length == 0)
                    throw new System.Exception("Shape file missing sub_object");
            }
        }

        public class SubObject
        {
            public ShapePrimitive[] ShapePrimitives;
            public VertexBufferSet[] VertexBufferSets;
            
            public SubObject( ref int dLevelPrimCount, sub_object sub_object, int[] hierarchy, SFile sFile, SharedShape sharedShape )
            {
                // get a total count of drawing primitives
                int primCount = sub_object.primitives.Count;

                // set up the buffers to hold the drawing primtives
                ShapePrimitives = new ShapePrimitive[primCount];

                int iV =  sub_object.sub_object_header.VolIdx;

                /* TODO COMPLETE THIS
                VertexBufferSets = new VertexBufferSet[ sub_object.vertex_sets.Count ];
                for( int i = 0; i < sub_object.vertex_sets.Count; ++i )
                    VertexBufferSets[i] = new VertexBufferSet( sub_object.vertex_sets[i], sFile, sub_object, graphicsDevice );
                 */ 
                VertexBufferSets = new VertexBufferSet[1];
                VertexBufferSets[0] = new VertexBufferSet( sFile, sub_object, sharedShape.Viewer.GraphicsDevice );

                /////////////// MATERIAL OPTIONS //////////////////
                //
                // Material options are specified in a 32-bit int named "options"
                // Following are the bit assignments:
                // (name, dec value, hex, bits)
                // 
                // NAMED SHADERS  bits 0 through 3 (allow for future shaders)
                // Diffuse            1     0x0001      0000 0000 0000 0001
                // Tex                2     0x0002      0000 0000 0000 0010
                // TexDiff            3     0x0003      0000 0000 0000 0011
                // BlendATex          4     0x0004      0000 0000 0000 0100
                // AddAtex            5     0x0005      0000 0000 0000 0101
                // BlendATexDiff      6     0x0006      0000 0000 0000 0110
                // AddATexDiff        7     0x0007      0000 0000 0000 0111
                // AND mask          15     0x000f      0000 0000 0000 1111
                //
                // LIGHTING  bits 4 through 7 ( << 4 )
                // DarkShade         16     0x0010      0000 0000 0001 0000
                // OptHalfBright     32     0x0020      0000 0000 0010 0000
                // CruciformLong     48     0x0030      0000 0000 0011 0000
                // Cruciform         64     0x0040      0000 0000 0100 0000
                // OptFullBright     80     0x0050      0000 0000 0101 0000
                // OptSpecular750    96     0x0060      0000 0000 0110 0000
                // OptSpecular25    112     0x0070      0000 0000 0111 0000
                // OptSpecular0     128     0x0080      0000 0000 1000 0000
                // AND mask         240     0x00f0      0000 0000 1111 0000 
                //
                // ALPHA TEST bit 8 ( << 8 )
                // None               0     0x0000      0000 0000 0000 0000
                // Trans            256     0x0100      0000 0001 0000 0000
                // AND mask         256     0x0100      0000 0001 0000 0000
                //
                // Z BUFFER bits 9 and 10 ( << 9 )
                // None               0     0x0000      0000 0000 0000 0000
                // Normal           512     0x0200      0000 0010 0000 0000
                // Write Only      1024     0x0400      0000 0100 0000 0000
                // Test Only       1536     0x0600      0000 0110 0000 0000
                // AND mask        1536     0x0600      0000 0110 0000 0000
                //
                // TEXTURE ADDRESS MODE bits 11 and 12 ( << 11 )
                // Wrap               0     0x0000      0000 0000 0000 0000 
                // Mirror          2048     0x0800      0000 1000 0000 0000
                // Clamp           4096     0x1000      0001 0000 0000 0000
                // Border          6144     0x1800      0001 1000 0000 0000
                // AND mask        6144     0x1800      0001 1000 0000 0000
                //
                // NIGHT TEXTURE bit 13 ( << 13 )
                // Disabled           0     0x0000      0000 0000 0000 0000
                // Enabled         8192     0x2000      0010 0000 0000 0000
                //

                // For each primitive, set up an effect and index buffer
                int iPrim = 0;
                foreach (primitive primitive in sub_object.primitives)
                {
                    ShapePrimitive shapePrimitive = new ShapePrimitive();
                    shapePrimitive.Hierarchy = hierarchy;

                    prim_state prim_state = sFile.shape.prim_states[primitive.prim_state_idx];
                    vtx_state vtx_state = sFile.shape.vtx_states[prim_state.ivtx_state];
                    VertexBufferSet vertexBufferSet = VertexBufferSets[0]; //TODO temp code uses one big bufferset
                    light_model_cfg light_model_cfg = sFile.shape.light_model_cfgs[vtx_state.LightCfgIdx];

                    // Select a material
                    int options = 0;

                    // Texture addressing
                    if (light_model_cfg.uv_ops.Count > 0)
                    {
                        uv_op uv_op = light_model_cfg.uv_ops[0];
                        options |= uv_op.TexAddrMode - 1 << 11; // Zero based
                    }

                    // Transparency test  
                    if (prim_state.alphatestmode == 1)
                        options |= 0x0100;

                    // Named shaders
                    int namedShader = 3; // Default is TexDiff
                    switch (sFile.shape.shader_names[prim_state.ishader])
                    {
                        case "Diffuse":
                            namedShader = 1;
                            break;
                        case "Tex":
                            namedShader = 2;
                            break;
                        case "TexDiff":
                        default:
                            namedShader = 3;
                            break;
                        case "BlendATex":
                            namedShader = 4;
                            break;
                        case "AddATex":
                            namedShader = 5;
                            break;
                        case "BlendATexDiff":
                            namedShader = 6;
                            break;
                        case "AddATexDiff":
                            namedShader = 7;
                            break;
                    }
                    options |= namedShader;

                    // Lighting model
                    options |= (13 + vtx_state.LightMatIdx) << 4;

                    // Night texture toggle
                    if (isNightEnabled == 1) // ESD_Alternative_Texture = 257
                        options |= (isNightEnabled) << 13;

                    if (prim_state.tex_idxs.Length == 0)
                    {   // untextured objects get a blank texture
                        shapePrimitive.Material = (SceneryMaterial)Materials.Load(sharedShape.Viewer.RenderProcess, "SceneryMaterial", null, options);
                    }
                    else
                    {
                        texture texture = sFile.shape.textures[prim_state.tex_idxs[0]];
                        string imageName = sFile.shape.images[texture.iImage];
                        if (File.Exists(sharedShape.textureFolder + @"\" + imageName))
                        {
                            shapePrimitive.Material = Materials.Load(sharedShape.Viewer.RenderProcess,
                                "SceneryMaterial", sharedShape.textureFolder + @"\" + imageName, options, texture.MipMapLODBias);
                        }
                        else 
                        { // Use file in base texture folder
                            int i = sharedShape.textureFolder.LastIndexOf(@"\");
                            string str = sharedShape.textureFolder.Remove(i);
                            shapePrimitive.Material = Materials.Load(sharedShape.Viewer.RenderProcess,
                                "SceneryMaterial", str + @"\" + imageName, options, texture.MipMapLODBias);
                        }
                    }

                    shapePrimitive.ZBias = prim_state.ZBias; // -(float)dLevelPrimCount * 0.00001f;  //Auto zbias causes issues

                    int iMatrix = vtx_state.imatrix;
                    shapePrimitive.iHierarchy = iMatrix;

                    int indexCount = primitive.indexed_trilist.vertex_idxs.Count * 3;

                    short[] indexData = new short[indexCount];

                    int iIndex = 0;
                    foreach (vertex_idx vertex_idx in primitive.indexed_trilist.vertex_idxs)
                    {
                        indexData[iIndex++] = (short)(vertex_idx.a);
                        indexData[iIndex++] = (short)(vertex_idx.b);
                        indexData[iIndex++] = (short)(vertex_idx.c);
                    }

                    shapePrimitive.IndexCount = indexCount;

                    shapePrimitive.IndexBuffer = new IndexBuffer(sharedShape.Viewer.GraphicsDevice, sizeof(short) * indexCount, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
                    shapePrimitive.IndexBuffer.SetData<short>(indexData);

                    shapePrimitive.VertexBufferSet = vertexBufferSet;

                    // Record range of vertices involved in this primitive as MinVertex and NumVertices
                    // TODO Extract this from the sub_object header
                    bool vertex_set_found = false;
                    foreach (vertex_set vertex_set in sub_object.vertex_sets)
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
                    ++dLevelPrimCount;
                }
            }
        }

        public class VertexBufferSet
        {
            public VertexBuffer Buffer;
            public VertexDeclaration Declaration;
            public int VertexCount;        // the number of vertices in the vertex buffer for each set

            public VertexBufferSet( vertex_set vertex_set, SFile sFile, sub_object sub_object, GraphicsDevice graphicsDevice )
            {
                VertexCount = vertex_set.VtxCount;
                VertexPositionNormalTexture[] vertexData = new VertexPositionNormalTexture[VertexCount];
                        // TODO - deal with vertex sets that have various numbers of texture coordinates - ie 0, 1, 2 etc
                for (int i = 0; i < VertexCount; ++i)
                {
                    MSTS.vertex MSTSvertex = sub_object.vertices[i + vertex_set.StartVtxIdx];
                    vertexData[i] = XNAVertexPositionNormalTextureFromMSTS(MSTSvertex, sFile.shape);
                }
                Declaration = new VertexDeclaration(graphicsDevice, VertexPositionNormalTexture.VertexElements);
                Buffer = new VertexBuffer(graphicsDevice, VertexPositionNormalTexture.SizeInBytes * vertexData.Length, BufferUsage.WriteOnly);
                Buffer.SetData(vertexData);
            }

            // temporary version that creates one vertex buffer for entire subObject
            public VertexBufferSet( SFile sFile, sub_object sub_object, GraphicsDevice graphicsDevice)
            {
                VertexCount = sub_object.vertices.Count;
                VertexPositionNormalTexture[] vertexData = new VertexPositionNormalTexture[VertexCount];
                // TODO - deal with vertex sets that have various numbers of texture coordinates - ie 0, 1, 2 etc
                for (int i = 0; i < VertexCount; ++i)
                {
                    MSTS.vertex MSTSvertex = sub_object.vertices[i];
                    vertexData[i] = XNAVertexPositionNormalTextureFromMSTS(MSTSvertex, sFile.shape);
                }
                Declaration = new VertexDeclaration(graphicsDevice, VertexPositionNormalTexture.VertexElements);
                Buffer = new VertexBuffer(graphicsDevice, VertexPositionNormalTexture.SizeInBytes * vertexData.Length, BufferUsage.WriteOnly);
                Buffer.SetData(vertexData);
            }

            public static VertexPositionTexture XNAVertexPositionTextureFromMSTS(vertex MSTSvertex, shape MSTSshape)
            {
                MSTS.point MSTSPosition = MSTSshape.points[MSTSvertex.ipoint];
                MSTS.uv_point MSTSTextureCoordinate;
                if (MSTSvertex.vertex_uvs.Length > 0)  // there are files without UVS points - ie mst-sawmill-wh.s in BECR route
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
                    MSTSTextureCoordinate = new uv_point(0, 0);  // TODO use a simpler vertex description when no UV's in use

                VertexPositionNormalTexture XNAVertex = new VertexPositionNormalTexture();
                XNAVertex.Position = new Vector3(MSTSPosition.X, MSTSPosition.Y, -MSTSPosition.Z);
                XNAVertex.Normal = new Vector3(MSTSNormal.X, MSTSNormal.Y, -MSTSNormal.Z);
                XNAVertex.TextureCoordinate = new Vector2(MSTSTextureCoordinate.U, MSTSTextureCoordinate.V);

                return XNAVertex;
            }
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

            Vector3 mstsLocation = new Vector3(xnaDTileTranslation.Translation.X, xnaDTileTranslation.Translation.Y, -xnaDTileTranslation.Translation.Z);

            foreach (LodControl lodControl in LodControls)
            {
                if (Viewer.Camera.InFOV(mstsLocation, lodControl.DistanceLevels[0].ViewSphereRadius))
                {
                    foreach (DistanceLevel distanceLevel in lodControl.DistanceLevels)
                    {
                        if (Viewer.Camera.InRange(mstsLocation, distanceLevel.ViewingDistance + distanceLevel.ViewSphereRadius))
                        {
                            foreach (SubObject subObject in distanceLevel.SubObjects)
                            {
                                // for each primitive
                                for (int iPrim = 0; iPrim < subObject.ShapePrimitives.Length; ++iPrim)
                                {
                                    ShapePrimitive shapePrimitive = subObject.ShapePrimitives[iPrim];
                                    Matrix xnaMatrix = Matrix.Identity;
                                    int iNode = shapePrimitive.iHierarchy;
                                    while (iNode != -1)
                                    {
                                        xnaMatrix *= animatedXNAMatrices[iNode];         // TODO, can we reduce memory allocations during this matrix math
                                        iNode = shapePrimitive.Hierarchy[iNode];
                                    }
                                    xnaMatrix *= xnaDTileTranslation;

                                    frame.AddPrimitive(shapePrimitive.Material, shapePrimitive, ref xnaMatrix );
                                } // for each primitive
                            }
                            break; // only draw one distance level.
                        }
                    }
                }
            }
        }// PrepareFrame()

        public Matrix GetMatrixProduct(int iNode)
        {
            int[] h= LodControls[0].DistanceLevels[0].SubObjects[0].ShapePrimitives[0].Hierarchy;
            Matrix matrix = Matrix.Identity;
            while (iNode != -1)
            {
                matrix *= Matrices[iNode];
                iNode = h[iNode];
            }
            return matrix;
        }

    }// class SharedShape
}
