// COPYRIGHT 2014 by the Open Rails project.
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Viewer3D.Common;
using Orts.Viewer3D.Processes;
using ORTS.Common;
using glTFLoader.Schema;
using System.Collections.Concurrent;

namespace Orts.Viewer3D
{
    public class GltfShape : SharedShape
    {
        public static readonly List<string> ExtensionsSupported = new List<string>
        {
            "KHR_animation_pointer",
            "KHR_lights_punctual",
            "KHR_materials_unlit",
            "KHR_materials_clearcoat",
            "KHR_materials_emissive_strength",
            "KHR_node_visibility",
            "MSFT_lod",
            "MSFT_texture_dds",
            "MSFT_packing_normalRoughnessMetallic",
            "MSFT_packing_occlusionRoughnessMetallic",
        };

        // Settings
        public static bool EnableAnimations { get; set; }
        public static bool ShapeWarnings { get; set; }
        public static bool TangentsAlwaysCalculatedPerPixel { get; set; }

        string FileDir { get; set; }
        public bool MsfsFlavoured;

        internal Vector3[] Scales;
        internal Quaternion[] Rotations;
        internal Vector3[] Translations;
        internal float[][] Weights;
        internal bool[] NodeVisibility;

        // Need these at load time to connect the pointers
        internal Dictionary<int, PbrMaterial> Materials = new Dictionary<int, PbrMaterial>();
        internal Dictionary<int, GltfPrimitive> Meshes = new Dictionary<int, GltfPrimitive>();
        internal Dictionary<int, StaticLight> Lights = new Dictionary<int, StaticLight>();

        /// <summary>
        /// glTF specification declares that the model's forward is +Z. However OpenRails uses -Z as forward,
        /// so we need to apply a 180 degree rotation to turn around every model matrix to conform the spec.
        /// </summary>
        static Matrix PlusZToForward = Matrix.CreateFromAxisAngle(Vector3.UnitY, MathHelper.Pi);
        static readonly string[] StandardTextureExtensionFilter = new[] { ".png", ".jpg", ".jpeg" };
        static readonly string[] DdsTextureExtensionFilter = new[] { ".dds" };
        public static Texture2D EnvironmentMapSpecularDay;
        public static TextureCube EnvironmentMapDiffuseDay;
        public static Texture2D BrdfLutTexture;
        static readonly Dictionary<string, CubeMapFace> EnvironmentMapFaces = new Dictionary<string, CubeMapFace>
        {
            ["px"] = CubeMapFace.PositiveX,
            ["nx"] = CubeMapFace.NegativeX,
            ["py"] = CubeMapFace.PositiveY,
            ["ny"] = CubeMapFace.NegativeY,
            ["pz"] = CubeMapFace.PositiveZ,
            ["nz"] = CubeMapFace.NegativeZ
        };

        List<GltfAnimation> GltfAnimations;
        public Vector4[] BoundingBoxNodes;

        Dictionary<int, string> ExternalLods = new Dictionary<int, string>();
        public float[] MinimumScreenCoverages = new[] { 0f };
        internal int LastLod = -1;
        internal bool LodChanged;

        /// <summary>
        /// glTF shape from file
        /// </summary>
        /// <param name="viewer"></param>
        /// <param name="filePath">Path to shape's glTF file</param>
        public GltfShape(Viewer viewer, string filePath)
            : base(viewer, filePath)
        {
            // In glTF the animation frames are measured in seconds, so the default FPS value must be 1 second per second.
            CustomAnimationFPS = 1;
            EnableAnimations = viewer.Game.Settings.GltfAnimations;
            ShapeWarnings = !viewer.Game.Settings.SuppressShapeWarnings;

            if (ConsistGenerator.GltfVisualTestRun)
            {
                if (!SampleModelsAdjustments.TryGetValue(Path.GetFileNameWithoutExtension(FilePath), out SampleModelsAdjustment))
                    SampleModelsAdjustment = Matrix.Identity;
            }
        }

        protected override void LoadContent()
        {
            Trace.Write("G");

            TangentsAlwaysCalculatedPerPixel = Viewer.Game.Settings.GltfTangentsAlwaysCalculatedPerPixel;

            FileDir = Path.GetDirectoryName(FilePath);
            var inputFilename = Path.GetFileNameWithoutExtension(FilePath).ToUpper();
            if (inputFilename.Contains("_LOD"))
                inputFilename = inputFilename.Substring(0, inputFilename.Length - 6); // to strip the "_LOD00" from the end
            var files = Directory.GetFiles(FileDir);
            Match match;
            foreach (var file in files)
            {
                if ((match = Regex.Match(Path.GetFileName(file.ToUpper()), inputFilename + @"_LOD(\d\d).GLTF$")).Success)
                {
                    if (int.TryParse(match.Groups[1].Value, out var lod))
                        ExternalLods[lod] = file;
                }
            }

            if (!ExternalLods.Any())
                ExternalLods.Add(0, FilePath);
            LodControls = new[] { new GltfLodControl(this, ExternalLods) };
            SetLod(0);

            if (EnvironmentMapSpecularDay == null)
            {
                // TODO: split the equirectangular specular panorama image to a cube map for saving the pixel shader instructions of converting the
                // cartesian cooridinates to polar for sampling. Couldn't find a converter though that also supports RGBD color encoding.
                // RGBD is an encoding where a divider [0..1] is stored in the alpha channel to reconstruct the High Dynamic Range of the RGB colors.
                // A HDR to TGA-RGBD converter is available here: https://seenax.com/portfolio/cpp.php , this can be further converted to PNG by e.g. GIMP.
                EnvironmentMapSpecularDay = Texture2D.FromStream(Viewer.GraphicsDevice, File.OpenRead(Path.Combine(Viewer.Game.ContentPath, "EnvMapDay/specular-RGBD.png")));
                // Possible TODO: replace the diffuse map with spherical harmonics coefficients (9x float3), as defined in EXT_lights_image_based.
                // See shader implementation e.g. here: https://github.com/CesiumGS/cesium/pull/7172
                EnvironmentMapDiffuseDay = new TextureCube(Viewer.GraphicsDevice, 128, false, SurfaceFormat.ColorSRgb);
                foreach (var face in EnvironmentMapFaces.Keys)
                {
                    // How to do this more efficiently?
                    using (var stream = File.OpenRead(Path.Combine(Viewer.Game.ContentPath, $"EnvMapDay/diffuse_{face}_0.jpg")))
                    {
                        var tex = Texture2D.FromStream(Viewer.GraphicsDevice, stream);
                        var data = new Color[tex.Width * tex.Height];
                        tex.GetData(data);
                        EnvironmentMapDiffuseDay.SetData(EnvironmentMapFaces[face], data);
                        tex.Dispose();
                    }
                }
            }
            if (BrdfLutTexture == null)
            {
                using (var stream = File.OpenRead(Path.Combine(Viewer.Game.ContentPath, $"EnvMapDay/brdfLUT.png")))
                {
                    BrdfLutTexture = Texture2D.FromStream(Viewer.GraphicsDevice, stream);
                }
            }
        }

        public override Matrix SetRenderMatrices(ShapePrimitive baseShapePrimitive, Matrix[] animatedMatrices, ref Matrix tileTranslation)
        {
            var shapePrimitive = baseShapePrimitive as GltfPrimitive;

            if (LodChanged && ExternalLods.Count > 1)
            {
                Array.Resize(ref animatedMatrices, Matrices.Length);
                Array.Copy(Matrices, animatedMatrices, Matrices.Length);
            }
            LodChanged = false;

            var bone = Matrix.Identity;
            for (var j = 0; j < shapePrimitive.Joints.Length; j++)
            {
                bone = MsfsFlavoured ? Matrix.Identity : shapePrimitive.InverseBindMatrices[j];
                var hi = shapePrimitive.Joints[j];
                while (hi >= 0 && hi < shapePrimitive.Hierarchy.Length)
                {
                    if (!NodeVisibility[hi])
                        bone = new Matrix();

                    Matrix.Multiply(ref bone, ref animatedMatrices[hi], out bone);
                    hi = shapePrimitive.Hierarchy[hi];
                }
                Matrix.Multiply(ref bone, ref PlusZToForward, out bone);

                // The ConsistGenerator is used to show all the Khronos sample models for testing purposes. However they need adjustments to show them all at once.
                if (ConsistGenerator.GltfVisualTestRun)
                    bone *= SampleModelsAdjustment;
                
                Matrix.Multiply(ref bone, ref tileTranslation, out bone);

                shapePrimitive.RenderBonesAltered?.SetValue(bone, j);
            }
            (shapePrimitive.RenderBonesAltered, shapePrimitive.RenderBonesRendered) = (shapePrimitive.RenderBonesRendered, shapePrimitive.RenderBonesAltered);

            return shapePrimitive.Joints.Length == 1 ? bone : Matrix.Identity; // unskinned vs. skinned
        }

        public GltfDistanceLevel SetLod(int lodId)
        {
            var lod = LodControls.FirstOrDefault()?.DistanceLevels?.ElementAtOrDefault(lodId) as GltfDistanceLevel;
            if (lodId != LastLod && lod != null)
            {
                Matrices = lod.Matrices;
                Scales = lod.Scales; ;
                Rotations = lod.Rotations;
                Translations = lod.Translations;
                Weights = lod.Weights;
                MatrixNames = lod.MatrixNames;
                BoundingBoxNodes = lod.BoundingBoxNodes;
                GltfAnimations = lod.GltfAnimations;
                NodeVisibility = lod.NodeVisibility;
                LodChanged = true;
                LastLod = lodId;

                lod.ConnectPointers();
            }
            return lod;
        }

        public class GltfLodControl : LodControl
        {
            readonly Dictionary<string, Gltf> Gltfs = new Dictionary<string, Gltf>();
            static readonly float[] DefaultScreenCoverages = new[] { 0.2f, 0.05f, 0.001f, 0f, 0f, 0f, 0f, 0f, 0f, 0f };

            public GltfLodControl(GltfShape shape, Dictionary<int, string> externalLods)
            {
                var distanceLevels = new List<GltfDistanceLevel>();
                foreach (var id in externalLods.Keys)
                {
                    var gltfFile = glTFLoader.Interface.LoadModel(externalLods[id]);
                    Gltfs.Add(externalLods[id], gltfFile);

                    if (gltfFile.ExtensionsRequired != null)
                    {
                        var unsupportedExtensions = new List<string>();
                        foreach (var extensionRequired in gltfFile.ExtensionsRequired)
                            if (!ExtensionsSupported.Contains(extensionRequired))
                                unsupportedExtensions.Add($"\"{extensionRequired}\"");
                        if (unsupportedExtensions.Any())
                        {
                            var message = $"glTF required extension {string.Join(", ", unsupportedExtensions)} is unsupported in file {externalLods[id]}";
                            if (ConsistGenerator.GltfVisualTestRun)
                                Trace.TraceWarning(message);
                            else
                                throw new NotImplementedException(message);
                        }
                    }

                    if (gltfFile.Asset?.Extensions?.ContainsKey("ASOBO_asset_optimized") ?? false)
                        shape.MsfsFlavoured = true;

                    var internalLodsNumber = 0;
                    if (id == 0)
                    {
                        var rootNodeNumber = gltfFile.Scenes.ElementAtOrDefault(gltfFile.Scene ?? 0).Nodes?.First();
                        if (rootNodeNumber != null)
                        {
                            var rootNode = gltfFile.Nodes[(int)rootNodeNumber];
                            object extension = null;
                            if (rootNode.Extensions?.TryGetValue("MSFT_lod", out extension) ?? false)
                            {
                                var ext = Newtonsoft.Json.JsonConvert.DeserializeObject<MSFT_lod>(extension.ToString());
                                if (ext?.Ids != null)
                                    internalLodsNumber = ext.Ids.Length + 1;
                                var screenCoverages = DefaultScreenCoverages;
                                if (rootNode.Extras?.TryGetValue("MSFT_screencoverage", out extension) ?? false)
                                    screenCoverages = Newtonsoft.Json.JsonConvert.DeserializeObject<float[]>(extension.ToString());
                                shape.MinimumScreenCoverages = new float[internalLodsNumber];
                                Array.Copy(screenCoverages, shape.MinimumScreenCoverages, internalLodsNumber);
                            }
                            else
                            {
                                shape.MinimumScreenCoverages = new float[externalLods.Count];
                                Array.Copy(DefaultScreenCoverages, shape.MinimumScreenCoverages, shape.MinimumScreenCoverages.Length);
                            }
                        }
                    }
                    if (internalLodsNumber > 0)
                    {
                        for (var i = 0; i < internalLodsNumber; i++)
                            distanceLevels.Add(new GltfDistanceLevel(shape, i, gltfFile, externalLods[id], distanceLevels.FirstOrDefault()));
                        // Use the internal lods instead of the externals, if available.
                        // Sweep the resources not used anymore
                        distanceLevels.Last().BinaryBuffers.Clear();
                        break;
                    }
                    distanceLevels.Add(new GltfDistanceLevel(shape, id, gltfFile, externalLods[id], null));
                    distanceLevels.Last().BinaryBuffers.Clear();
                }
                DistanceLevels = distanceLevels.ToArray();
            }
        }

        public class GltfDistanceLevel : DistanceLevel
        {
            // See the glTF specification at https://www.khronos.org/registry/glTF/specs/2.0/glTF-2.0.html
            readonly Gltf GltfFile;
            readonly string GltfDir;
            readonly string GltfFileName;
            
            internal Dictionary<int, VertexBufferBinding> VertexBufferBindings;
            internal Dictionary<int, IndexBuffer> IndexBuffers;
            internal Dictionary<int, byte[]> BinaryBuffers;

            /// <summary>
            /// All inverse bind matrices in a gltf file. The key is the accessor number.
            /// </summary>
            internal Dictionary<int, Matrix[]> InverseBindMatrices = new Dictionary<int, Matrix[]>();

            // When setting the actual distance level, these collections need to be assigned to the shape
            internal Matrix[] Matrices;
            internal Vector3[] Scales;
            internal Quaternion[] Rotations;
            internal Vector3[] Translations;
            internal float[][] Weights;
            internal List<string> MatrixNames;
            internal Vector4[] BoundingBoxNodes;
            internal List<GltfAnimation> GltfAnimations;
            internal bool[] NodeVisibility;

            internal readonly Viewer Viewer;
            internal readonly GltfShape Shape;

            static readonly string[] TestControls = new[] { "WIPER", "ORTSITEM1CONTINUOUS", "ORTSITEM2CONTINUOUS" };

            // These are only temporary variables needed during the construction
            readonly Stack<int> TempStack = new Stack<int>();
            readonly List<VertexElement> VertexElements = new List<VertexElement>();
            readonly Dictionary<int, string> Accessors = new Dictionary<int, string>();
            string DebugName = "";
            internal const int BUFFER_INDEX_OFFSET = 10000; // Arbitrary number

            public GltfDistanceLevel(GltfShape shape, int lodId, Gltf gltfFile, string gltfFileName, GltfDistanceLevel lod0DetailLevel)
            {
                ViewingDistance = float.MaxValue; // glTF is using screen coverage, so this one is set for not getting into the way accidentally
                ViewSphereRadius = 100;
                Shape = shape;
                Viewer = shape.Viewer;
                GltfFile = gltfFile;
                GltfDir = Path.GetDirectoryName(gltfFileName);
                GltfFileName = gltfFileName;

                GetBinaryData(shape, gltfFile, gltfFileName, lod0DetailLevel);

                var hierarchy = Enumerable.Repeat(-1, GltfFile.Nodes.Length).ToArray();
                var parents = new Dictionary<int, int>();
                var lods = Enumerable.Repeat(-1, GltfFile.Nodes.Length).ToArray(); // -1: common; 0, 1, 3, etc.: the lod the node belongs to
                var meshes = new Dictionary<int, Node>();
                var lights = new Dictionary<int, int>();

                TempStack.Clear();
                Array.ForEach(GltfFile.Scenes.ElementAtOrDefault(GltfFile.Scene ?? 0).Nodes, node => TempStack.Push(node));
                KHR_lights gltfLights = null;
                object extension = null;
                if (GltfFile.Extensions?.TryGetValue("KHR_lights_punctual", out extension) ?? false)
                    gltfLights = Newtonsoft.Json.JsonConvert.DeserializeObject<KHR_lights>(extension.ToString());

                while (TempStack.Any())
                {
                    var nodeNumber = TempStack.Pop();
                    var node = GltfFile.Nodes[nodeNumber];
                    var parent = hierarchy[nodeNumber];
                    if (parent > -1 && lods[parent] > -1)
                        lods[nodeNumber] = lods[parent];

                    if (node.Children != null)
                    {
                        foreach (var child in node.Children)
                        {
                            hierarchy[child] = nodeNumber;
                            TempStack.Push(child);
                        }
                    }

                    if (node.Extensions?.TryGetValue("MSFT_lod", out extension) ?? false)
                    {
                        var ext = Newtonsoft.Json.JsonConvert.DeserializeObject<MSFT_lod>(extension.ToString());
                        var ids = ext?.Ids;
                        if (ids.Any())
                        {
                            lods[nodeNumber] = 0;
                            for (var j = 0; j < ids.Length; j++)
                            {
                                // The node defined in the MSFT_lod extension is a substitute to the actual one, not an additional new step-to.
                                lods[ids[j]] = j + 1;
                                hierarchy[ids[j]] = parent;
                                TempStack.Push(ids[j]);
                            }
                        }
                    }

                    // Collect meshes and lights belonging to the common root or the specific lod only:
                    if (lods[nodeNumber] == -1 || lods[nodeNumber] == lodId)
                    {
                        if (node.Mesh != null)
                            meshes.Add(nodeNumber, node);

                        if (node.Extensions?.TryGetValue("KHR_lights_punctual", out extension) ?? false)
                        {
                            var lightId = Newtonsoft.Json.JsonConvert.DeserializeObject<KHR_lights_punctual_index>(extension.ToString())?.light;
                            if (lightId != null)
                                lights.Add(nodeNumber, (int)lightId);
                        }
                    }
                }

                GltfSubObject subObject;
                var subObjects = new List<SubObject>();
                foreach (var hierIndex in meshes.Keys)
                {
                    var node = meshes[hierIndex];
                    var mesh = GltfFile.Meshes[(int)node.Mesh];
                    var skin = node.Skin != null ? GltfFile.Skins[(int)node.Skin] : null;

                    for (var i = 0; i < mesh.Primitives.Length; i++)
                    {
                        subObjects.Add(subObject = new GltfSubObject(mesh.Primitives[i], $"{mesh.Name}[{i}]", hierIndex, hierarchy, Helpers.TextureFlags.None, GltfFile, Shape, this, skin));
                        if (!shape.Meshes.ContainsKey((int)node.Mesh) && subObject.ShapePrimitives?.FirstOrDefault() is GltfPrimitive primitive)
                            shape.Meshes.Add((int)node.Mesh, primitive);
                    }
                }
                foreach (var hierIndex in lights.Keys)
                {
                    subObjects.Add(subObject = new GltfSubObject(gltfLights.lights[lights[hierIndex]], hierIndex, hierarchy, GltfFile, Shape, this));
                    if (!shape.Lights.ContainsKey(lights[hierIndex]))
                        shape.Lights.Add(lights[hierIndex], subObject.ShapePrimitives?.FirstOrDefault()?.AttachedLight);
                }
                SubObjects = subObjects.ToArray();

                var minPosition = SubObjects.Cast<GltfSubObject>().Select(so => Vector4.Transform(so.MinPosition, Matrices.ElementAt(so.HierarchyIndex)))
                    .Aggregate((pos1, pos2) => Vector4.Min(pos1, pos2));
                var maxPosition = SubObjects.Cast<GltfSubObject>().Select(so => Vector4.Transform(so.MaxPosition, Matrices.ElementAt(so.HierarchyIndex)))
                    .Aggregate((pos1, pos2) => Vector4.Max(pos1, pos2));

                BoundingBoxNodes[0] = minPosition;
                BoundingBoxNodes[1] = new Vector4(minPosition.X, minPosition.Y, maxPosition.Z, 1);
                BoundingBoxNodes[2] = new Vector4(minPosition.X, maxPosition.Y, maxPosition.Z, 1);
                BoundingBoxNodes[3] = new Vector4(minPosition.X, maxPosition.Y, minPosition.Z, 1);
                BoundingBoxNodes[4] = new Vector4(maxPosition.X, minPosition.Y, minPosition.Z, 1);
                BoundingBoxNodes[5] = new Vector4(maxPosition.X, minPosition.Y, maxPosition.Z, 1);
                BoundingBoxNodes[6] = new Vector4(maxPosition.X, maxPosition.Y, minPosition.Z, 1);
                BoundingBoxNodes[7] = maxPosition;

                if (ConsistGenerator.GltfVisualTestRun)
                {
                    // Assign the first three animations to Wipers [V], Item1Continuous [Shift+,], Item2Continuous [Shift+.] respectively,
                    // because these are the ones capable of playing a loop.
                    for (var i = 0; i < GltfAnimations.Count; i++)
                        MatrixNames[i] = TestControls[i % TestControls.Length];
                }
            }

            void GetBinaryData(GltfShape shape, Gltf gltfFile, string gltfFileName, GltfDistanceLevel lod0DetailLevel)
            {
                if (lod0DetailLevel == null)
                    GetBinaryData(shape, gltfFile, gltfFileName);
                else
                {
                    Weights = lod0DetailLevel.Weights;
                    Scales = lod0DetailLevel.Scales; ;
                    Rotations = lod0DetailLevel.Rotations;
                    Translations = lod0DetailLevel.Translations;
                    Matrices = lod0DetailLevel.Matrices;
                    BoundingBoxNodes = lod0DetailLevel.BoundingBoxNodes;
                    GltfAnimations = lod0DetailLevel.GltfAnimations;
                    MatrixNames = lod0DetailLevel.MatrixNames;
                    IndexBuffers = lod0DetailLevel.IndexBuffers;
                    VertexBufferBindings = lod0DetailLevel.VertexBufferBindings;
                    BinaryBuffers = lod0DetailLevel.BinaryBuffers;
                }
            }

            void GetBinaryData(GltfShape shape, Gltf gltfFile, string gltfFileName)
            {
                MatrixNames = new List<string>();
                BoundingBoxNodes = new Vector4[8];
                GltfAnimations = new List<GltfAnimation>();
                VertexBufferBindings = new Dictionary<int, VertexBufferBinding>();
                IndexBuffers = new Dictionary<int, IndexBuffer>();
                BinaryBuffers = new Dictionary<int, byte[]>();

                Weights = gltfFile.Nodes.Select(node => node.Weights ?? gltfFile.Meshes?.ElementAtOrDefault(node.Mesh ?? -1)?.Weights
                    ?? new float[gltfFile.Meshes?.ElementAtOrDefault(node.Mesh ?? -1)?.Primitives?.FirstOrDefault(p => p.Targets != null && p.Targets.Length > 0)?.Targets?.Length ?? 0]).ToArray();
                Scales = gltfFile.Nodes.Select(node => node.Scale == null ? Vector3.One : MemoryMarshal.Cast<float, Vector3>(node.Scale)[0]).ToArray();
                Rotations = gltfFile.Nodes.Select(node => node.Rotation == null ? Quaternion.Identity : MemoryMarshal.Cast<float, Quaternion>(node.Rotation)[0]).ToArray();
                Translations = gltfFile.Nodes.Select(node => node.Translation == null ? Vector3.Zero : MemoryMarshal.Cast<float, Vector3>(node.Translation)[0]).ToArray();
                Matrices = gltfFile.Nodes.Select((node, i) => node.Matrix == null ? Matrix.Identity : MemoryMarshal.Cast<float, Matrix>(node.Matrix)[0]
                    * Matrix.CreateScale(Scales.ElementAt(i)) * Matrix.CreateFromQuaternion(Rotations.ElementAt(i)) * Matrix.CreateTranslation(Translations.ElementAt(i))).ToArray();

                // Substitute the sparse data to its place.
                for (var a = 0; a < gltfFile.Accessors.Length; a++)
                {
                    if (gltfFile.Accessors[a] is var accessor && accessor.Sparse != null)
                    {
                        // Sparse buffers may index into a null buffer, so create a real one for these.
                        if (GetBufferViewSpan(accessor.BufferView, 0) is var buffer && buffer.IsEmpty)
                        {
                            BinaryBuffers.Add(BUFFER_INDEX_OFFSET + a, new byte[accessor.Count * GetSizeInBytes(accessor)]);
                            buffer = BinaryBuffers.Last().Value.AsSpan();
                        }
                        // It might have already been processed in another distance level.
                        var sparseValues = GetBufferViewSpan(accessor.Sparse?.Values);
                        var sparseIndices = GetBufferViewSpan(accessor.Sparse?.Indices);
                        var byteStride = gltfFile.BufferViews.ElementAtOrDefault(accessor.BufferView ?? -1)?.ByteStride ?? GetSizeInBytes(accessor);
                        switch (accessor.Sparse.Indices.ComponentType)
                        {
                            case AccessorSparseIndices.ComponentTypeEnum.UNSIGNED_BYTE:
                                for (var i = 0; i < accessor.Sparse.Count; i++)
                                    sparseValues.Slice(i * byteStride, byteStride).CopyTo(buffer.Slice(accessor.ByteOffset + sparseIndices[i] * byteStride, byteStride));
                                break;
                            case AccessorSparseIndices.ComponentTypeEnum.UNSIGNED_INT:
                                var indicesUi = MemoryMarshal.Cast<byte, uint>(sparseIndices);
                                for (var i = 0; i < accessor.Sparse.Count; i++)
                                    sparseValues.Slice(i * byteStride, byteStride).CopyTo(buffer.Slice(accessor.ByteOffset + (int)indicesUi[i] * byteStride, byteStride));
                                break;
                            case AccessorSparseIndices.ComponentTypeEnum.UNSIGNED_SHORT:
                                var indicesUs = MemoryMarshal.Cast<byte, ushort>(sparseIndices);
                                for (var i = 0; i < accessor.Sparse.Count; i++)
                                    sparseValues.Slice(i * byteStride, byteStride).CopyTo(buffer.Slice(accessor.ByteOffset + indicesUs[i] * byteStride, byteStride));
                                break;
                        }
                    }
                }

                // Shovel all binary index & vertex attribute data over to the GPU. Such bufferViews are VertexBuffers/IndexBuffers in fact.
                // The byteStride is constant throughout a vertex attribute bufferView, but may vary in index bufferViews.
                // An accessor is the vertex attribute, is has a byteOffset within the bufferView. If byteOffset < byteStride, then the accessors are interleaved.
                var primitives = gltfFile.Meshes.SelectMany(m => m.Primitives);
                var bufferViews = primitives
                    .SelectMany(p => p.Attributes)
                    .Concat(primitives // Upload all the morph targets too in this sole pass.
                        .SelectMany(p => p.Targets ?? new Dictionary<string, int>[0])
                        .SelectMany(t => t.Select(d => new KeyValuePair<string, int>("POSITION", d.Value)))) // Create all morph target vertex buffers with POSITION semantic, so they bind to the right slots.
                    .OrderBy(a => gltfFile.Accessors[a.Value].ByteOffset)
                    .Distinct()
                    .GroupBy(a => gltfFile.Accessors[a.Value].BufferView ?? -1);

                foreach (var bufferView in bufferViews)
                {
                    var byteStride = gltfFile.BufferViews.ElementAtOrDefault(bufferView.Key)?.ByteStride ?? GetSizeInBytes(gltfFile.Accessors[bufferView.First().Value]);

                    // Trigger the loading of the binary buffer.
                    GetBufferViewSpan(bufferView.Key, 0);
                    
                    var attributes = bufferView.GetEnumerator();
                    var loop = attributes.MoveNext();
                    var semantic = GetVertexElementSemantic(attributes.Current.Key, out var index);
                    do
                    {
                        DebugName = "";
                        VertexElements.Clear();
                        Accessors.Clear();
                        var previousOffset = gltfFile.Accessors[attributes.Current.Value].ByteOffset;
                        var currentOffset = previousOffset;
                        var previousAttribute = attributes.Current.Key;
                        var bindingKey = attributes.Current.Value + (int)semantic * BUFFER_INDEX_OFFSET;

                        // For interleaved data, multiple vertexElements and multiple accessors will be in a single vertexBuffer.
                        // For non-interleaved data, we create a distinct vertexBuffer for each accessor.
                        // A bufferView may consist of a series of (non-interleaved) accessors of POSITION:NORMAL:POSITION:NORMAL:POSITION:NORMAL etc. (See: 2CylinderEngine)
                        // Also e.g. TEXCOORDS_0 and TEXCOORDS_1 may refer to the same accessor.
                        // Also POSITION and NORMAL may refer to the same accessor (see: PrimitiveModeNormalsTest), however due to the MonoGame limitations
                        // we cannot use the same VertexBuffer with two different VertexElementUsage-s, duplicate ones are needed with the same data.
                        do
                        {
                            if (!Accessors.ContainsKey(attributes.Current.Value) && !VertexBufferBindings.ContainsKey(bindingKey))
                            {
                                VertexElements.Add(new VertexElement(gltfFile.Accessors[attributes.Current.Value].ByteOffset - previousOffset,
                                    GetVertexElementFormat(gltfFile.Accessors[attributes.Current.Value], shape.MsfsFlavoured), semantic, index));
                                if (Debugger.IsAttached) DebugName = (DebugName != "" ? DebugName + "," : "") + attributes.Current.Key;
                            }
                            // Multiple accessors to same bufferview with same semantic. Will reuse the VertexBuffer:
                            //while (loop && previousOffset == currentOffset && attributes.Current.Key == previousAttribute)
                            {
                                if (!Accessors.ContainsKey(attributes.Current.Value) && !VertexBufferBindings.ContainsKey(bindingKey))
                                {
                                    Accessors.Add(attributes.Current.Value, attributes.Current.Key);
                                }
                                loop = attributes.MoveNext();
                                semantic = GetVertexElementSemantic(attributes.Current.Key, out index);
                                currentOffset = gltfFile.Accessors[attributes.Current.Value].ByteOffset;
                            }
                        }
                        while (loop && previousOffset < currentOffset && currentOffset < previousOffset + byteStride);

                        if (VertexBufferBindings.ContainsKey(bindingKey))
                            continue;

                        if (Debugger.IsAttached) DebugName += ":" + Path.GetFileNameWithoutExtension(gltfFileName);

                        var vertexCount = Accessors.Max(a => gltfFile.Accessors[a.Key].Count);
                        var vertexBuffer = new VertexBuffer(shape.Viewer.GraphicsDevice, new VertexDeclaration(byteStride, VertexElements.ToArray()), vertexCount, BufferUsage.None) { Name = DebugName };

                        byte[] binaryBuffer = null;
                        if (gltfFile.BufferViews.ElementAtOrDefault(bufferView.Key) is var bv && bv != null)
                        {
                            var byteOffset = bv.ByteOffset + Accessors.Min(a => gltfFile.Accessors[a.Key].ByteOffset);
                            vertexBuffer.SetData(BinaryBuffers[bv.Buffer], byteOffset, vertexCount * byteStride);
                        }
                        else if (Accessors.Any(a => BinaryBuffers.TryGetValue(BUFFER_INDEX_OFFSET + a.Key, out binaryBuffer)))
                        {
                            vertexBuffer.SetData(binaryBuffer);
                        }
                        else
                        {
                            // This shouldn't happen, just in case...
                            vertexBuffer.Dispose();
                            continue;
                        }

                        var vertexBufferBinding = new VertexBufferBinding(vertexBuffer);
                        VertexBufferBindings.Add(bindingKey, vertexBufferBinding);

                        if (Accessors.Count > 1)
                        {
                            // Multiple accessors may refer to the same bufferview, in which case we may reuse the same VertexBuffer. (See: PrimitiveModeNormalsTest)
                            // If the accessors ByteOffset is greater than the first, then it is interleaved, leave it alone. If eaqual, then reuse.
                            // But we need to add it the the bindigs with all keys, otherwise it would not be found.
                            var minByteOffset = Accessors.Min(a => gltfFile.Accessors[a.Key].ByteOffset);
                            for (var i = 1; i < Accessors.Count; i++)
                            {
                                var accessor = Accessors.ElementAt(i);
                                if (gltfFile.Accessors[accessor.Key].ByteOffset == minByteOffset)
                                {
                                    semantic = GetVertexElementSemantic(accessor.Value, out _);
                                    bindingKey = accessor.Key + (int)semantic * BUFFER_INDEX_OFFSET;
                                    VertexBufferBindings.Add(bindingKey, vertexBufferBinding);
                                }
                            }
                        }
                    }
                    while (loop);
                }

                var indexBufferViews = primitives
                    .OrderBy(p => gltfFile.Accessors?.ElementAtOrDefault(p.Indices ?? -1)?.ByteOffset ?? -1)
                    .GroupBy(p => gltfFile.Accessors?.ElementAtOrDefault(p.Indices ?? -1)?.BufferView ?? -1)
                    .Where(i => i.Key != -1);

                foreach (var indexBufferView in indexBufferViews)
                {
                    // Trigger the loading of the binary buffer.
                    GetBufferViewSpan(indexBufferView.Key, 0);
                    
                    // Both uint and ushort type indices may exist in a single bufferView, so byteStride is not constant. (See: ToyCar)
                    // In MonoGame we cannot create a hybrid buffer, so creating the buffer on a per-accessor basis.
                    foreach (var p in indexBufferView)
                    {
                        if (IndexBuffers.ContainsKey((int)p.Indices))
                            continue;
                        var accessor = gltfFile.Accessors?.ElementAtOrDefault((int)p.Indices);
                        var bufferView = gltfFile.BufferViews?.ElementAtOrDefault(indexBufferView.Key);
                        var indexBuffer = new IndexBuffer(shape.Viewer.GraphicsDevice, GetIndexElementSize(accessor.ComponentType), accessor.Count, BufferUsage.None);

                        // 8 bit indices are unsupported in Direct3D, so we must convert them to 16 bits. GetIndexElementSize() reports twice the length automatically in this case.
                        if (accessor.ComponentType == Accessor.ComponentTypeEnum.UNSIGNED_BYTE)
                            indexBuffer.SetData(BinaryBuffers[bufferView.Buffer].Skip(bufferView.ByteOffset + accessor.ByteOffset).Take(accessor.Count).Select(b => (ushort)b).ToArray());
                        else
                            indexBuffer.SetData(BinaryBuffers[bufferView.Buffer], bufferView.ByteOffset + accessor.ByteOffset, accessor.Count * GetComponentSizeInBytes(accessor.ComponentType));

                        IndexBuffers.Add((int)p.Indices, indexBuffer);
                    }
                }

                for (var j = 0; j < (gltfFile.Animations?.Length ?? 0); j++)
                {
                    var gltfAnimation = gltfFile.Animations[j];

                    // Use MatrixNames for storing animation and articulation names.
                    // Here the MatrixNames are not bound to nodes (and matrices), but rather to the animation number.
                    var animation = new GltfAnimation(gltfAnimation.Name ?? "");
                    MatrixNames.Add(animation.Name);

                    for (var k = 0; k < gltfAnimation.Channels.Length; k++)
                    {
                        var gltfChannel = gltfAnimation.Channels[k];
                        
                        object pointerExtension = null;
                        string pointer = null;
                        if (gltfChannel.Target.Extensions?.TryGetValue("KHR_animation_pointer", out pointerExtension) ?? false)
                            pointer = Newtonsoft.Json.JsonConvert.DeserializeObject<KHR_animation_pointer>(pointerExtension.ToString()).Pointer;
                        else if (gltfChannel.Target.Node == null)
                            continue;

                        var sampler = gltfAnimation.Samplers[gltfChannel.Sampler];
                        var inputAccessor = gltfFile.Accessors[sampler.Input];
                        var outputAccessor = gltfFile.Accessors[sampler.Output];

                        var inputFloats = new Span<float>(new float[inputAccessor.Count * GetComponentNumber(inputAccessor.Type)]);
                        Denormalize(inputAccessor, GetBufferViewSpan(inputAccessor), ref inputFloats);
                        var outputFloats = new Span<float>(new float[outputAccessor.Count * GetComponentNumber(outputAccessor.Type)]);
                        Denormalize(outputAccessor, GetBufferViewSpan(outputAccessor), ref outputFloats);

                        GltfAnimationChannel channel;
                        animation.Channels.Add(channel = new GltfAnimationChannel
                        {
                            Interpolation = shape.MsfsFlavoured ? AnimationSampler.InterpolationEnum.LINEAR : sampler.Interpolation,
                            Path = gltfChannel.Target.Path,
                            TargetNode = gltfChannel.Target.Node,
                            TimeArray = inputFloats.ToArray(),
                            TimeMin = inputAccessor.Min[0],
                            TimeMax = inputAccessor.Max[0],
                            OutputQuaternion = gltfChannel.Target.Path == AnimationChannelTarget.PathEnum.rotation ?
                                MemoryMarshal.Cast<float, Quaternion>(outputFloats).ToArray() : null,
                            OutputVector3 = gltfChannel.Target.Path == AnimationChannelTarget.PathEnum.scale || gltfChannel.Target.Path == AnimationChannelTarget.PathEnum.translation ?
                                MemoryMarshal.Cast<float, Vector3>(outputFloats).ToArray() : null,
                            OutputFloats = gltfChannel.Target.Path == AnimationChannelTarget.PathEnum.weights ? outputFloats.ToArray() : null,
                            Pointer = gltfChannel.Target.Path == AnimationChannelTarget.PathEnum.pointer ? pointer : null,
                        });

                        if (!string.IsNullOrEmpty(channel.Pointer))
                        {
                            var pointerSpan = channel.Pointer.AsSpan();
                            var s2 = pointerSpan.Slice(1).IndexOf('/') + 1;
                            var s3 = pointerSpan.Slice(s2 + 1).IndexOf('/');

                            foreach (var template in PointerTemplates.Keys)
                            {
                                var templateSpan = template.AsSpan();
                                var c2 = templateSpan.IndexOf('{');
                                var c3 = templateSpan.IndexOf('}');
                                if (pointerSpan.Slice(0, s2 + 1).SequenceEqual(templateSpan.Slice(0, c2)) &&
                                    pointerSpan.Slice(s2 + s3 + 1).SequenceEqual(templateSpan.Slice(c3 + 1)))
                                {
                                    switch (PointerTemplates[template])
                                    {
                                        case PointerTypes.Boolean:
                                        case PointerTypes.FloatVector:
                                        case PointerTypes.Float: channel.OutputFloats = outputFloats.ToArray(); break;
                                        case PointerTypes.Quaternion: channel.OutputQuaternion = MemoryMarshal.Cast<float, Quaternion>(outputFloats).ToArray(); break;
                                        case PointerTypes.Vector3: channel.OutputVector3 = MemoryMarshal.Cast<float, Vector3>(outputFloats).ToArray(); break;
                                        case PointerTypes.Vector4: channel.OutputVector4 = MemoryMarshal.Cast<float, Vector4>(outputFloats).ToArray(); break;
                                        default: break;
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    GltfAnimations.Add(animation);
                }

                object extension = null;
                var names = gltfFile.Nodes.Select((n, i) => ((n.Extras?.TryGetValue("OPENRAILS_animation_name", out extension) ?? false) && extension is string a ? a : null, i)).Where(n => n.Item1 != null);
                var dimensions = gltfFile.Nodes.Select(n => (n.Extras?.TryGetValue("OPENRAILS_animation_wheelradius", out extension) ?? false) && extension is string dim && float.TryParse(dim, out var d) ? d : 0f);
                GltfAnimations.AddRange(names.Select(n => new GltfAnimation(n.Item1) { ExtrasWheelRadius = dimensions.ElementAt(n.i), Channels = { new GltfAnimationChannel() { TargetNode = n.i } } }));
                MatrixNames.AddRange(names.Select(n => n.Item1));
                NodeVisibility = gltfFile.Nodes.Select(n => 
                    (n.Extensions?.TryGetValue("KHR_node_visibility", out extension) ?? false) &&
                        Newtonsoft.Json.JsonConvert.DeserializeObject<KHR_node_Visibility>(extension.ToString()).Visible is bool v ? v : true).ToArray();
            }

            internal void ConnectPointers()
            {
                foreach (var animation in GltfAnimations)
                {
                    foreach (var channel in animation.Channels)
                    {
                        bool isMatch(string element, out int matchIndex, out ReadOnlySpan<char> matchProperty)
                        {
                            var pointer = channel.Pointer.AsSpan();
                            if (pointer.StartsWith(element.AsSpan()))
                            {
                                var p2 = pointer.Slice(element.Length);
                                var i1 = p2.Slice(0, p2.IndexOf('/'));
                                matchProperty = p2.Slice(p2.IndexOf('/'));

                                Debug.Assert(!p2.StartsWith("/".AsSpan()));
                                Debug.Assert(!i1.Contains("/".AsSpan(), StringComparison.OrdinalIgnoreCase));
                                Debug.Assert(matchProperty.StartsWith("/".AsSpan()));

                                return int.TryParse(i1.ToString(), out matchIndex);
                            }
                            else
                            {
                                matchIndex = -1;
                                matchProperty = default;
                                return false;
                            }
                        }

                        if (isMatch("/nodes/", out var index, out var property))
                        {
                            channel.TargetNode = index;
                            switch (property.ToString())
                            {
                                case "/translation": channel.Path = AnimationChannelTarget.PathEnum.translation; break;
                                case "/rotation": channel.Path = AnimationChannelTarget.PathEnum.rotation; break;
                                case "/scale": channel.Path = AnimationChannelTarget.PathEnum.scale; break;
                                case "/weights": channel.Path = AnimationChannelTarget.PathEnum.weights; break;
                                case "/extensions/KHR_node_visibility/visible": channel.SetTargetFloat = (f) => NodeVisibility?.SetValue(f > 0, (int)channel.TargetNode); break;
                                default:
                                    if (property.StartsWith("/weights/".AsSpan()) && property.LastIndexOf('/') is var i2 && i2 < property.Length
                                        && int.TryParse(property.Slice(i2).ToString(), out var index2))
                                    {
                                        channel.SetTargetFloat = (f) => Shape.Weights[(int)channel.TargetNode][index2] = f;
                                    }
                                    break;
                            }
                        }
                        else if (isMatch("/meshes/", out index, out property))
                        {
                            channel.TargetNode = Shape.Meshes[index].HierarchyIndex;
                            if (property.SequenceEqual("/weights".AsSpan()))
                            {
                                channel.Path = AnimationChannelTarget.PathEnum.weights;
                            }
                            else if (property.StartsWith("/weights/".AsSpan()) && property.LastIndexOf('/') is var i2 && i2 < property.Length
                                && int.TryParse(property.Slice(i2).ToString(), out var index2))
                            {
                                channel.SetTargetFloat = (f) => Shape.Weights[(int)channel.TargetNode][index2] = f;
                            }
                        }
                        else if (isMatch("/materials/", out index, out property))
                        {
                            var material = Shape.Materials[index];
                            switch (property.ToString())
                            {
                                case "/alphaCutoff": channel.SetTargetFloat = material.SetAlphaCutoff; break;
                                case "/emissiveFactor": channel.SetTargetVector3 = material.SetEmissiveFactor; break;
                                case "/normalTexture/scale": channel.SetTargetFloat = material.SetNormalScale; break;
                                case "/occlusionTexture/strength": channel.SetTargetFloat = material.SetOcclusionSrtength; break;
                                case "/pbrMetallicRoughness/baseColorFactor": channel.SetTargetVector4 = material.SetBaseColorFactor; break;
                                case "/pbrMetallicRoughness/metallicFactor": channel.SetTargetFloat = material.SetMetallicFactor; break;
                                case "/pbrMetallicRoughness/roughnessFactor": channel.SetTargetFloat = material.SetRoughnessFactor; break;
                                case "/extensions/KHR_materials_clearcoat/clearcoatFactor": channel.SetTargetFloat = material.SetClearcoatFactor; break;
                                case "/extensions/KHR_materials_clearcoat/clearcoatRoughnessFactor": channel.SetTargetFloat = material.SetClearcoatRoughnessFactor; break;
                                case "/extensions/KHR_materials_clearcoat/clearcoatNormalTexture/scale": channel.SetTargetFloat = material.SetClearcoatNormalScale; break;
                                default: channel.SetTargetFloat = null; channel.SetTargetVector3 = null; channel.SetTargetVector4 = null; break;
                            }
                        }
                        else if (isMatch("/extensions/KHR_lights_punctual/lights/", out index, out property))
                        {
                            var light = Shape.Lights[index];
                            switch (property.ToString())
                            {
                                case "/color": channel.SetTargetVector3 = (c) => light.ColorX = c; break;
                                case "/intensity": channel.SetTargetFloat = (f) => light.IntensityX = f; break;
                                case "/range": channel.SetTargetFloat = (f) => light.RangeX = f; break;
                                case "/spot/innerConeAngle": channel.SetTargetFloat = (f) => light.InnerConeAngleX = f; break;
                                case "/spot/outerConeAngle": channel.SetTargetFloat = (f) => light.OuterConeAngleX = f; break;
                                default: channel.SetTargetFloat = null; channel.SetTargetVector3 = null; channel.SetTargetVector4 = null; break;
                            }
                        }
                        if (channel.TargetNode != null)
                        {
                            switch (channel.Path)
                            {
                                case AnimationChannelTarget.PathEnum.rotation: channel.SetTargetQuaternion = (v) => Rotations[(int)channel.TargetNode] = v; break;
                                case AnimationChannelTarget.PathEnum.translation: channel.SetTargetVector3 = (v) => Translations[(int)channel.TargetNode] = v; break;
                                case AnimationChannelTarget.PathEnum.scale: channel.SetTargetVector3 = (v) => Scales[(int)channel.TargetNode] = v; break;
                                case AnimationChannelTarget.PathEnum.weights:
                                    channel.SetTargetFloatVector = (i, f) => Weights[(int)channel.TargetNode][i] = f;
                                    channel.TargetFloatVectorLength = Weights[(int)channel.TargetNode].Length;
                                    break;
                            }
                            continue;
                        }
                    }
                }
            }

            internal static void Denormalize(Accessor accessor, Span<byte> bytes, ref Span<float> floats)
            {
                switch (accessor.ComponentType)
                {
                    case Accessor.ComponentTypeEnum.BYTE: for (var i = 0; i < accessor.Count; i++) floats[i] = Math.Max(bytes[i] / 127f, -1f); break;
                    case Accessor.ComponentTypeEnum.UNSIGNED_BYTE: for (var i = 0; i < accessor.Count; i++) floats[i] = bytes[i] / 255f; break;
                    case Accessor.ComponentTypeEnum.UNSIGNED_SHORT: for (var i = 0; i < accessor.Count; i++) floats[i] = MemoryMarshal.Read<ushort>(bytes.Slice(i * sizeof(ushort))) / 65535f; break;
                    case Accessor.ComponentTypeEnum.SHORT: for (var i = 0; i < accessor.Count; i++) floats[i] = Math.Max(MemoryMarshal.Read<short>(bytes.Slice(i * sizeof(short))) / 32767f, -1f); break;
                    case Accessor.ComponentTypeEnum.FLOAT:
                    default: floats = MemoryMarshal.Cast<byte, float>(bytes.Slice(0, floats.Length * sizeof(float))); break;
                }
            }

            internal Span<byte> GetBufferViewSpan(AccessorSparseIndices accessor) => GetBufferViewSpan(accessor?.BufferView, accessor?.ByteOffset ?? 0);
            internal Span<byte> GetBufferViewSpan(AccessorSparseValues accessor) => GetBufferViewSpan(accessor?.BufferView, accessor?.ByteOffset ?? 0);
            internal Span<byte> GetBufferViewSpan(Accessor accessor) => GetBufferViewSpan(accessor.BufferView, accessor.ByteOffset);
            internal Span<byte> GetBufferViewSpan(int? bufferViewNumber, int accessorByteOffset)
            {
                if (bufferViewNumber == null)
                    return Span<byte>.Empty;
                var bufferView = GltfFile.BufferViews[(int)bufferViewNumber];
                if (!BinaryBuffers.TryGetValue(bufferView.Buffer, out var bytes))
                    BinaryBuffers.Add(bufferView.Buffer, bytes = glTFLoader.Interface.LoadBinaryBuffer(GltfFile, bufferView.Buffer, GltfFileName));
                return bytes.AsSpan(bufferView.ByteOffset + accessorByteOffset);
            }

            internal int GetSizeInBytes(Accessor accessor)
            {
                var componentNumber = GetComponentNumber(accessor.Type);
                var size = componentNumber * GetComponentSizeInBytes(accessor.ComponentType);
                var padding = componentNumber == 1 ? 0 : size % 4; // do not add padding to the index buffers
                return size + padding;
            }
            
            internal int GetComponentNumber(Accessor.TypeEnum type)
            {
                switch (type)
                {
                    case Accessor.TypeEnum.SCALAR: return 1;
                    case Accessor.TypeEnum.VEC2: return 2;
                    case Accessor.TypeEnum.VEC3: return 3;
                    case Accessor.TypeEnum.VEC4:
                    case Accessor.TypeEnum.MAT2: return 4;
                    case Accessor.TypeEnum.MAT3: return 9;
                    case Accessor.TypeEnum.MAT4: return 16;
                    default: return 1;
                }
            }

            internal int GetComponentSizeInBytes(Accessor.ComponentTypeEnum componentType)
            {
                switch (componentType)
                {
                    case Accessor.ComponentTypeEnum.BYTE:
                    case Accessor.ComponentTypeEnum.UNSIGNED_BYTE: return 1;
                    case Accessor.ComponentTypeEnum.SHORT:
                    case Accessor.ComponentTypeEnum.UNSIGNED_SHORT: return 2;
                    case Accessor.ComponentTypeEnum.UNSIGNED_INT:
                    case Accessor.ComponentTypeEnum.FLOAT:
                    default: return 4;
                }
            }

            IndexElementSize GetIndexElementSize(Accessor.ComponentTypeEnum componentType)
            {
                switch (componentType)
                {
                    case Accessor.ComponentTypeEnum.UNSIGNED_INT: return IndexElementSize.ThirtyTwoBits;
                    case Accessor.ComponentTypeEnum.UNSIGNED_SHORT: return IndexElementSize.SixteenBits;
                    case Accessor.ComponentTypeEnum.UNSIGNED_BYTE: if (ShapeWarnings) Trace.TraceInformation($"glTF: Unsupported 8 bit index size in file {GltfFileName}, converting it to 16 bits."); return IndexElementSize.SixteenBits;
                    default: return IndexElementSize.SixteenBits;
                }
            }

            VertexElementFormat GetVertexElementFormat(Accessor accessor, bool msfsFlavoured = false)
            {
                // UNSIGNED_INT is reserved for the index buffers.
                switch (accessor.Type)
                {
                    case Accessor.TypeEnum.SCALAR when accessor.ComponentType == Accessor.ComponentTypeEnum.FLOAT: return VertexElementFormat.Single;

                    case Accessor.TypeEnum.VEC2 when accessor.ComponentType == Accessor.ComponentTypeEnum.SHORT && !msfsFlavoured: return accessor.Normalized ? VertexElementFormat.NormalizedShort2 : VertexElementFormat.Short2;
                    case Accessor.TypeEnum.VEC2 when accessor.ComponentType == Accessor.ComponentTypeEnum.UNSIGNED_SHORT && !msfsFlavoured: return accessor.Normalized ? VertexElementFormat.NormalizedShort2 : VertexElementFormat.Short2;
                    case Accessor.TypeEnum.VEC2 when accessor.ComponentType == Accessor.ComponentTypeEnum.FLOAT: return VertexElementFormat.Vector2;

                    case Accessor.TypeEnum.VEC3 when accessor.ComponentType == Accessor.ComponentTypeEnum.FLOAT: return VertexElementFormat.Vector3;

                    case Accessor.TypeEnum.VEC4 when accessor.ComponentType == Accessor.ComponentTypeEnum.BYTE && !msfsFlavoured: return accessor.Normalized ? VertexElementFormat.Color : VertexElementFormat.Byte4;
                    case Accessor.TypeEnum.VEC4 when accessor.ComponentType == Accessor.ComponentTypeEnum.UNSIGNED_BYTE && !msfsFlavoured: return accessor.Normalized ? VertexElementFormat.Color : VertexElementFormat.Byte4;
                    case Accessor.TypeEnum.VEC4 when accessor.ComponentType == Accessor.ComponentTypeEnum.SHORT && !msfsFlavoured: return accessor.Normalized ? VertexElementFormat.NormalizedShort4 : VertexElementFormat.Short4;
                    case Accessor.TypeEnum.VEC4 when accessor.ComponentType == Accessor.ComponentTypeEnum.UNSIGNED_SHORT && !msfsFlavoured: return accessor.Normalized ? VertexElementFormat.NormalizedShort4 : VertexElementFormat.Short4;
                    case Accessor.TypeEnum.VEC4 when accessor.ComponentType == Accessor.ComponentTypeEnum.FLOAT: return VertexElementFormat.Vector4;

                    // MSFS twisted out, reversed definitions:
                    // non-normalized, VEC3 FLOAT, VEC4 BYTE, VEC2 SHORT, VEC4 USHORT, SCALAR FLOAT
                    case Accessor.TypeEnum.VEC2 when accessor.ComponentType == Accessor.ComponentTypeEnum.SHORT && msfsFlavoured: return accessor.Normalized ? VertexElementFormat.Short2 : VertexElementFormat.HalfVector2;
                    case Accessor.TypeEnum.VEC2 when accessor.ComponentType == Accessor.ComponentTypeEnum.UNSIGNED_SHORT && msfsFlavoured: return accessor.Normalized ? VertexElementFormat.Short2 : VertexElementFormat.HalfVector2;
                    case Accessor.TypeEnum.VEC4 when accessor.ComponentType == Accessor.ComponentTypeEnum.BYTE && msfsFlavoured: return VertexElementFormat.Color;
                    case Accessor.TypeEnum.VEC4 when accessor.ComponentType == Accessor.ComponentTypeEnum.UNSIGNED_BYTE && msfsFlavoured: return VertexElementFormat.Color;
                    case Accessor.TypeEnum.VEC4 when accessor.ComponentType == Accessor.ComponentTypeEnum.SHORT && msfsFlavoured: return accessor.Normalized ? VertexElementFormat.Short4 : VertexElementFormat.HalfVector4;
                    case Accessor.TypeEnum.VEC4 when accessor.ComponentType == Accessor.ComponentTypeEnum.UNSIGNED_SHORT && msfsFlavoured: return accessor.Normalized ? VertexElementFormat.Short4 : VertexElementFormat.HalfVector4;

                    default: Trace.TraceWarning($"glTF: Unknown vertex attribute format is found in file {GltfFileName}"); return VertexElementFormat.Single;
                }
            }

            internal static VertexElementUsage GetVertexElementSemantic(string semantic, out int index)
            {
                var split = semantic.Split('_');
                if (!int.TryParse(split.ElementAtOrDefault(1), out index))
                    index = 0;
                if (!VertexElementSemantic.TryGetValue(split.FirstOrDefault(), out var result))
                    result = VertexElementUsage.TextureCoordinate;
                return result;
            }

            static readonly Dictionary<string, VertexElementUsage> VertexElementSemantic = new Dictionary<string, VertexElementUsage>
            {
                ["POSITION"] = VertexElementUsage.Position,
                ["NORMAL"] = VertexElementUsage.Normal,
                ["TANGENT"] = VertexElementUsage.Tangent,
                ["TEXCOORD"] = VertexElementUsage.TextureCoordinate,
                ["COLOR"] = VertexElementUsage.Color,
                ["JOINTS"] = VertexElementUsage.BlendIndices,
                ["WEIGHTS"] = VertexElementUsage.BlendWeight,
            };

            readonly List<float> BufferValues = new List<float>();
            void ReadBuffer(Func<BinaryReader, float> read, BinaryReader br, Accessor.TypeEnum sourceType, int seek)
            {
                BufferValues.Clear();
                for (var i = 0; i < GetComponentNumber(sourceType); i++)
                    BufferValues.Add(read(br));
                br.BaseStream.Seek(seek, SeekOrigin.Current);
            }

            internal Vector2 ReadVector2(Func<BinaryReader, float> read, BinaryReader br, Accessor.TypeEnum sourceType, int seek)
            {
                ReadBuffer(read, br, sourceType, seek);
                return new Vector2(BufferValues.ElementAtOrDefault(0), BufferValues.ElementAtOrDefault(1));
            }

            internal Vector3 ReadVector3(Func<BinaryReader, float> read, BinaryReader br, Accessor.TypeEnum sourceType, int seek)
            {
                ReadBuffer(read, br, sourceType, seek);
                return new Vector3(BufferValues.ElementAtOrDefault(0), BufferValues.ElementAtOrDefault(1), BufferValues.ElementAtOrDefault(2));
            }

            internal Vector4 ReadVector4(Func<BinaryReader, float> read, BinaryReader br, Accessor.TypeEnum sourceType, int seek)
            {
                ReadBuffer(read, br, sourceType, seek);
                return new Vector4(BufferValues.ElementAtOrDefault(0), BufferValues.ElementAtOrDefault(1), BufferValues.ElementAtOrDefault(2), BufferValues.ElementAtOrDefault(3));
            }

            internal Texture2D GetTexture(Gltf gltf, int? textureIndex, Texture2D defaultTexture)
            {
                if (textureIndex != null)
                {
                    var texture = gltf.Textures[(int)textureIndex];
                    var source = texture?.Source;
                    var extensionFilter = StandardTextureExtensionFilter;
                    object extension = null;
                    if (texture?.Extensions?.TryGetValue("MSFT_texture_dds", out extension) ?? false)
                    {
                        var ext = Newtonsoft.Json.JsonConvert.DeserializeObject<MSFT_texture_dds>(extension.ToString());
                        source = ext?.Source ?? source;
                        extensionFilter = DdsTextureExtensionFilter;
                    }
                    if (source != null)
                    {
                        var image = gltf.Images[(int)source];
                        if (image.Uri != null)
                        {
                            var imagePath = source != null ? Path.Combine(GltfDir, Uri.UnescapeDataString(image.Uri)) : "";
                            if (File.Exists(imagePath))
                            {
                                // We refuse to load textures containing "../" in their path, because although it would be possible,
                                // it would break compatibility with the existing glTF viewers, including the Windows 3D Viewer,
                                // the VS Code glTF Tools and the reference Khronos glTF-Sample-Viewer.
                                var strippedImagePath = imagePath.Replace("../", "").Replace(@"..\", "").Replace("..", "");
                                if (File.Exists(strippedImagePath))
                                    return Viewer.TextureManager.Get(strippedImagePath, defaultTexture, false, extensionFilter);
                                else
                                    Trace.TraceWarning($"glTF: refusing to load texture {imagePath} in file {GltfFileName}, using \"../\" in the path is discouraged due to compatibility reasons.");
                            }
                            else
                            {
                                try
                                {
                                    using (var stream = glTFLoader.Interface.OpenImageFile(gltf, (int)source, GltfFileName))
                                        return Texture2D.FromStream(Viewer.GraphicsDevice, stream);
                                }
                                catch
                                {
                                    Trace.TraceWarning($"glTF: missing texture {imagePath} in file {GltfFileName}");
                                }
                            }
                        }
                        else if (image.BufferView != null)
                        {
                            try
                            {
                                using (var stream = glTFLoader.Interface.OpenImageFile(gltf, (int)source, GltfFileName))
                                    return Texture2D.FromStream(Viewer.GraphicsDevice, stream);
                            }
                            catch
                            {
                                Trace.TraceWarning($"glTF: missing image {image.BufferView} in file {GltfFileName}");
                            }
                        }
                    }
                }
                return defaultTexture;
            }

            internal TextureFilter GetTextureFilter(Sampler sampler)
            {
                if (sampler.MagFilter == Sampler.MagFilterEnum.LINEAR && sampler.MinFilter == Sampler.MinFilterEnum.LINEAR)
                    return TextureFilter.Linear;
                if (sampler.MagFilter == Sampler.MagFilterEnum.LINEAR && sampler.MinFilter == Sampler.MinFilterEnum.LINEAR_MIPMAP_LINEAR)
                    return TextureFilter.Linear;
                if (sampler.MagFilter == Sampler.MagFilterEnum.LINEAR && sampler.MinFilter == Sampler.MinFilterEnum.LINEAR_MIPMAP_NEAREST)
                    return TextureFilter.LinearMipPoint;
                if (sampler.MagFilter == Sampler.MagFilterEnum.LINEAR && sampler.MinFilter == Sampler.MinFilterEnum.NEAREST_MIPMAP_LINEAR)
                    return TextureFilter.MinPointMagLinearMipLinear;
                if (sampler.MagFilter == Sampler.MagFilterEnum.LINEAR && sampler.MinFilter == Sampler.MinFilterEnum.NEAREST_MIPMAP_NEAREST)
                    return TextureFilter.MinPointMagLinearMipPoint;
                if (sampler.MagFilter == Sampler.MagFilterEnum.NEAREST && sampler.MinFilter == Sampler.MinFilterEnum.LINEAR_MIPMAP_LINEAR)
                    return TextureFilter.MinLinearMagPointMipLinear;
                if (sampler.MagFilter == Sampler.MagFilterEnum.NEAREST && sampler.MinFilter == Sampler.MinFilterEnum.LINEAR_MIPMAP_NEAREST)
                    return TextureFilter.MinLinearMagPointMipPoint;
                if (sampler.MagFilter == Sampler.MagFilterEnum.NEAREST && sampler.MinFilter == Sampler.MinFilterEnum.NEAREST_MIPMAP_LINEAR)
                    return TextureFilter.PointMipLinear;
                if (sampler.MagFilter == Sampler.MagFilterEnum.NEAREST && sampler.MinFilter == Sampler.MinFilterEnum.NEAREST_MIPMAP_NEAREST)
                    return TextureFilter.Point;
                if (sampler.MagFilter == Sampler.MagFilterEnum.NEAREST && sampler.MinFilter == Sampler.MinFilterEnum.NEAREST)
                    return TextureFilter.Point;

                if (sampler.MagFilter == Sampler.MagFilterEnum.LINEAR && sampler.MinFilter == Sampler.MinFilterEnum.NEAREST)
                    return TextureFilter.MinPointMagLinearMipLinear;
                if (sampler.MagFilter == Sampler.MagFilterEnum.NEAREST && sampler.MinFilter == Sampler.MinFilterEnum.LINEAR)
                    return TextureFilter.MinLinearMagPointMipLinear;

                return TextureFilter.Linear;
            }

            internal (int, Texture2D, (TextureFilter, TextureAddressMode, TextureAddressMode)) GetTextureInfo(Gltf gltf, int? texCoord, int? index, Texture2D defaultTexture)
            {
                var texture = GetTexture(gltf, index, defaultTexture);
                var sampler = gltf.Samplers?.ElementAtOrDefault(gltf.Textures?.ElementAtOrDefault(index ?? -1)?.Sampler ?? -1) ?? GltfSubObject.DefaultGltfSampler;
                var samplerState = (GetTextureFilter(sampler), GetTextureAddressMode(sampler.WrapS), GetTextureAddressMode(sampler.WrapT));
                return (texCoord ?? 0, texture, samplerState);
            }
            internal (int, Texture2D, (TextureFilter, TextureAddressMode, TextureAddressMode)) GetTextureInfo(Gltf gltf, TextureInfo textureInfo, Texture2D defaultTexture)
                => GetTextureInfo(gltf, textureInfo?.TexCoord, textureInfo?.Index, defaultTexture);
            internal (int, Texture2D, (TextureFilter, TextureAddressMode, TextureAddressMode)) GetTextureInfo(Gltf gltf, MaterialNormalTextureInfo textureInfo, Texture2D defaultTexture)
                => GetTextureInfo(gltf, textureInfo?.TexCoord, textureInfo?.Index, defaultTexture);
            internal (int, Texture2D, (TextureFilter, TextureAddressMode, TextureAddressMode)) GetTextureInfo(Gltf gltf, MaterialOcclusionTextureInfo textureInfo, Texture2D defaultTexture)
                => GetTextureInfo(gltf, textureInfo?.TexCoord, textureInfo?.Index, defaultTexture);

            internal TextureAddressMode GetTextureAddressMode(Sampler.WrapTEnum wrapEnum) => GetTextureAddressMode((Sampler.WrapSEnum)wrapEnum);
            internal TextureAddressMode GetTextureAddressMode(Sampler.WrapSEnum wrapEnum)
            {
                if (Shape.MsfsFlavoured) return TextureAddressMode.Clamp;
                switch (wrapEnum)
                {
                    case Sampler.WrapSEnum.REPEAT: return TextureAddressMode.Wrap;
                    case Sampler.WrapSEnum.CLAMP_TO_EDGE: return TextureAddressMode.Clamp;
                    case Sampler.WrapSEnum.MIRRORED_REPEAT: return TextureAddressMode.Mirror;
                    default: return TextureAddressMode.Wrap;
                }
            }
        }

        public class GltfSubObject : SubObject
        {
            public readonly Vector4 MinPosition;
            public readonly Vector4 MaxPosition;
            public readonly int HierarchyIndex;

            static readonly ConcurrentQueue<VertexBuffer> Disposables = new ConcurrentQueue<VertexBuffer>();

            public static readonly glTFLoader.Schema.Material DefaultGltfMaterial = new glTFLoader.Schema.Material
            {
                AlphaCutoff = 0.5f,
                DoubleSided = false,
                AlphaMode = glTFLoader.Schema.Material.AlphaModeEnum.OPAQUE,
                EmissiveFactor = new[] {0f, 0f, 0f},
                Name = nameof(DefaultGltfMaterial)
            };
            public static readonly glTFLoader.Schema.Sampler DefaultGltfSampler = new glTFLoader.Schema.Sampler
            {
                MagFilter = Sampler.MagFilterEnum.LINEAR,
                MinFilter = Sampler.MinFilterEnum.LINEAR_MIPMAP_LINEAR,
                WrapS = Sampler.WrapSEnum.REPEAT,
                WrapT = Sampler.WrapTEnum.REPEAT,
                Name = nameof(DefaultGltfSampler)
            };
            
            static readonly List<string> MorphTargetAttributes = new List<string>() { "POSITION", "NORMAL", "TANGENT", "TEXCOORD_0", "TEXCOORD_1", "COLOR_0" };

            public GltfSubObject(KHR_lights_punctual light, int hierarchyIndex, int[] hierarchy, Gltf gltfFile, GltfShape shape, GltfDistanceLevel distanceLevel)
            {
                HierarchyIndex = hierarchyIndex;
                ShapePrimitives = new[] { new GltfPrimitive(light, gltfFile, distanceLevel, hierarchyIndex, hierarchy) };
            }

            public GltfSubObject(MeshPrimitive meshPrimitive, string name, int hierarchyIndex, int[] hierarchy, Helpers.TextureFlags textureFlags, Gltf gltfFile, GltfShape shape, GltfDistanceLevel distanceLevel, Skin skin)
            {
                HierarchyIndex = hierarchyIndex;
                
                var material = meshPrimitive.Material == null ? DefaultGltfMaterial : gltfFile.Materials[(int)meshPrimitive.Material];

                var options = SceneryMaterialOptions.None;

                if (!material.Extensions?.ContainsKey("KHR_materials_unlit") ?? true)
                    options |= SceneryMaterialOptions.Diffuse;

                if (skin != null)
                    options |= SceneryMaterialOptions.PbrHasSkin;
                if (meshPrimitive.Targets != null && meshPrimitive.Targets.Length > 0)
                    options |= SceneryMaterialOptions.PbrHasMorphTargets;

                var flipNormals = 1f;
                if (!shape.MsfsFlavoured && distanceLevel.Matrices.ElementAt(hierarchyIndex).Determinant() > 0)
                    // This is according to the glTF spec
                    options |= SceneryMaterialOptions.PbrCullClockWise;
                else if (shape.MsfsFlavoured && distanceLevel.Matrices.ElementAt(hierarchyIndex).Determinant() < 0)
                    // Msfs seems to be using this reversed
                    options |= SceneryMaterialOptions.PbrCullClockWise;
                else
                    flipNormals = -1f;

                var referenceAlpha = 0f;
                var doubleSided = material.DoubleSided;

                switch (material.AlphaMode)
                {
                    case glTFLoader.Schema.Material.AlphaModeEnum.BLEND: options |= SceneryMaterialOptions.AlphaBlendingBlend; break;
                    case glTFLoader.Schema.Material.AlphaModeEnum.MASK: options |= SceneryMaterialOptions.AlphaTest; referenceAlpha = material.AlphaCutoff; break;
                    case glTFLoader.Schema.Material.AlphaModeEnum.OPAQUE:
                    default: break;
                }

                var texCoords1 = Vector4.Zero; // x: baseColor, y: roughness-metallic, z: normal, w: emissive
                var texCoords2 = Vector4.Zero; // x: clearcoat, y: clearcoat-roughness, z: clearcoat-normal, w: occlusion

                MaterialNormalTextureInfo msftNormalInfo = null;
                TextureInfo msftOrmInfo = null;
                TextureInfo msftRmoInfo = null;
                object extension = null;
                if (material.Extensions?.TryGetValue("MSFT_packing_normalRoughnessMetallic", out extension) ?? false)
                    msftNormalInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<MSFT_packing_normalRoughnessMetallic>(extension.ToString())?.NormalRoughnessMetallicTexture;
                else if (material.Extensions?.TryGetValue("MSFT_packing_occlusionRoughnessMetallic", out extension) ?? false)
                {
                    var ext = Newtonsoft.Json.JsonConvert.DeserializeObject<MSFT_packing_occlusionRoughnessMetallic>(extension.ToString());
                    msftOrmInfo = ext?.OcclusionRoughnessMetallicTexture;
                    msftRmoInfo = ext?.RoughnessMetallicOcclusionTexture;
                    msftNormalInfo = ext?.NormalTexture;
                }
                // 0: occlusion (R), roughnessMetallic (GB) together, normal (RGB) separate, this is the standard
                // 1: roughnessMetallicOcclusion together, normal (RGB) separate
                // 2: normalRoughnessMetallic (RG+B+A) together, occlusion (R) separate
                // 3: occlusionRoughnessMetallic together, normal (RGB) separate
                // 4: roughnessMetallicOcclusion together, normal (RG) 2 channel separate
                // 5: occlusionRoughnessMetallic together, normal (RG) 2 channel separate
                var texturePacking =
                    msftOrmInfo != null ? msftNormalInfo != null ? 5 : 3 :
                    msftRmoInfo != null ? msftNormalInfo != null ? 4 : 1 :
                                          msftNormalInfo != null ? 2 : 0;

                // baseColor texture is 8 bit sRGB + A. Needs decoding to linear in the shader.
                // metallicRoughness texture: G = roughness, B = metalness, linear, may be > 8 bit.
                // normal texture is RGB linear, B should be >= 0.5. All channels need mapping from the [0.0..1.0] to the [-1.0..1.0] range, = sampledValue * 2.0 - 1.0
                // occlusion texture is R channel only, = 1.0 + strength * (sampledValue - 1.0)
                // emissive texture is 8 bit sRGB. Needs decoding to linear in the shader.
                // clearcoat texture is R channel only.
                // clearcoatRoughness texture is G channel only.
                Texture2D baseColorTexture = null, metallicRoughnessTexture = null, normalTexture = null, occlusionTexture = null, emissiveTexture = null, clearcoatTexture = null, clearcoatRoughnessTexture = null, clearcoatNormalTexture = null;
                (TextureFilter, TextureAddressMode, TextureAddressMode) baseColorSamplerState = default, metallicRoughnessSamplerState = default, normalSamplerState = default, occlusionSamplerState = default, emissiveSamplerState = default, clearcoatSamplerState = default, clearcoatRoughnessSamplerState = default, clearcoatNormalSamplerState = default;

                KHR_materials_clearcoat clearcoat = null;
                if (material.Extensions?.TryGetValue("KHR_materials_clearcoat", out extension) ?? false)
                    clearcoat = Newtonsoft.Json.JsonConvert.DeserializeObject<KHR_materials_clearcoat>(extension.ToString());

                var emissiveStrength = 1f;
                if (material.Extensions?.TryGetValue("KHR_materials_emissive_strength", out extension) ?? false)
                    emissiveStrength = Newtonsoft.Json.JsonConvert.DeserializeObject<KHR_materials_emissive_strength>(extension.ToString())?.EmissiveStrength ?? 1;

                (texCoords1.X, baseColorTexture, baseColorSamplerState) = distanceLevel.GetTextureInfo(gltfFile, material.PbrMetallicRoughness?.BaseColorTexture, SharedMaterialManager.WhiteTexture);
                (texCoords1.Y, metallicRoughnessTexture, metallicRoughnessSamplerState) = distanceLevel.GetTextureInfo(gltfFile, msftRmoInfo ?? msftOrmInfo ?? material.PbrMetallicRoughness?.MetallicRoughnessTexture, SharedMaterialManager.WhiteTexture);
                (texCoords1.Z, normalTexture, normalSamplerState) = distanceLevel.GetTextureInfo(gltfFile, msftNormalInfo ?? material.NormalTexture, SharedMaterialManager.WhiteTexture);
                (texCoords1.W, emissiveTexture, emissiveSamplerState) = distanceLevel.GetTextureInfo(gltfFile, material.EmissiveTexture, SharedMaterialManager.WhiteTexture);
                (texCoords2.W, occlusionTexture, occlusionSamplerState) = msftOrmInfo != null
                    ? distanceLevel.GetTextureInfo(gltfFile, msftOrmInfo, SharedMaterialManager.WhiteTexture)
                    : distanceLevel.GetTextureInfo(gltfFile, material.OcclusionTexture, SharedMaterialManager.WhiteTexture);
                (texCoords2.X, clearcoatTexture, clearcoatSamplerState) = distanceLevel.GetTextureInfo(gltfFile, clearcoat?.ClearcoatTexture, SharedMaterialManager.WhiteTexture);
                (texCoords2.Y, clearcoatRoughnessTexture, clearcoatRoughnessSamplerState) = distanceLevel.GetTextureInfo(gltfFile, clearcoat?.ClearcoatRoughnessTexture, SharedMaterialManager.WhiteTexture);
                (texCoords2.Z, clearcoatNormalTexture, clearcoatNormalSamplerState) = distanceLevel.GetTextureInfo(gltfFile, clearcoat?.ClearcoatNormalTexture, SharedMaterialManager.WhiteTexture);

                var baseColorFactor = MemoryMarshal.Cast<float, Vector4>(material.PbrMetallicRoughness?.BaseColorFactor ?? new[] { 1f, 1f, 1f, 1f })[0];
                var metallicFactor = material.PbrMetallicRoughness?.MetallicFactor ?? 1f;
                var roughtnessFactor = material.PbrMetallicRoughness?.RoughnessFactor ?? 1f;
                var normalScale = flipNormals * (material.NormalTexture?.Scale ?? 2f); // 2 indicates the textureInfo is missing, otherwise it gets the default 1
                var occlusionStrength = material.OcclusionTexture?.Strength ?? 2; // 2 indicates the textureInfo is missing, otherwise it gets the default 1
                var emissiveFactor = MemoryMarshal.Cast<float, Vector3>(material.EmissiveFactor ?? new[] { 0f, 0f, 0f })[0] * emissiveStrength;
                var clearcoatFactor = clearcoat?.ClearcoatFactor ?? 0;
                var clearcoatRoughnessFactor = clearcoat?.ClearcoatRoughnessFactor ?? 0;
                var clearcoatNormalScale = flipNormals * (clearcoat?.ClearcoatNormalTexture?.Scale ?? 2f); // 2 indicates the textureInfo is missing, otherwise it gets the default 1

                switch (baseColorSamplerState.Item2)
                {
                    case TextureAddressMode.Wrap: options |= SceneryMaterialOptions.TextureAddressModeWrap; break;
                    case TextureAddressMode.Clamp: options |= SceneryMaterialOptions.TextureAddressModeClamp; break;
                    case TextureAddressMode.Mirror: options |= SceneryMaterialOptions.TextureAddressModeMirror; break;
                }

                var indexBufferSet = new GltfIndexBufferSet();
                var indexCount = 0;

                if (gltfFile.Accessors.ElementAtOrDefault(meshPrimitive.Indices ?? -1) is var accessor && accessor != null)
                {
                    indexBufferSet.IndexBuffer = distanceLevel.IndexBuffers[(int)meshPrimitive.Indices];
                    indexBufferSet.PrimitiveOffset = 0;
                    indexCount = accessor.Count;
                    options |= SceneryMaterialOptions.PbrHasIndices;
                }

                var vertexAttributes = meshPrimitive.Attributes
                    .SelectMany(a => distanceLevel.VertexBufferBindings
                    .Where(kvp => kvp.Key == a.Value + (int)GltfDistanceLevel.GetVertexElementSemantic(a.Key, out _) * GltfDistanceLevel.BUFFER_INDEX_OFFSET)
                    .Select(kvp => kvp.Value))
                    .ToList();
                var vertexCount = vertexAttributes.FirstOrDefault().VertexBuffer?.VertexCount ?? 0;

                // Currently the below PBR vertex input combinations are possible. Any model must use one of those pipelines.
                // If vertex attribute buffer(s) are missing to match one of these, dummy one(s) must be added.
                // The order of the vertex buffers is unimportant, the shader will attach by semantics.
                // If more combinations to be added in the future, there must be support for them both in SceneryShader and ShadowMapShader.
                //
                // ===================================================================================
                // PositionNormalTexture | NormalMapColor     | Skinned            | Morphed          
                // ===================================================================================
                //  Position             | Position           | Position           | All skinned + 8 morph targets, can be any of:
                //  Normal               | Normal             | Normal             |                  
                //  TexCoords_0          | TexCoords_0        | TexCoords_0        | Position         
                //                       | Tangent            | Tangent            | Normal           
                //                       | TexCoords_1        | TexCoords_1        | Tangent          
                //                       | Color_0            | Color_0            | TexCoords_0      
                //                       |                    | Joints_0           | TexCoords_1      
                //                       |                    | Weights_0          | Color_0          
                // ===================================================================================
                //
                // So e.g. for a primitive with only Position, Normal, Color_0 attributes present,
                // dummy TexCoord_0, TexCoord_1 and Tangent buffers have to be added to match the NormalMapColor pipeline.

                // Cannot proceed without vertex Positions, nor can add fake ones, exiting.
                if (!meshPrimitive.Attributes.TryGetValue("POSITION", out var accessorPosition))
                    throw new InvalidDataException("One of the glTF mesh primitives has no positions.");
                else
                {
                    var a = gltfFile.Accessors[accessorPosition];
                    MinPosition = new Vector4(a.Min[0], a.Min[1], a.Min[2], 1);
                    MaxPosition = new Vector4(a.Max[0], a.Max[1], a.Max[2], 1);
                }

                // Cannot proceed without Normal either, must add a dummy one.
                if (!vertexAttributes.Any(a => a.VertexBuffer.VertexDeclaration.GetVertexElements().Any(e => e.VertexElementUsage == VertexElementUsage.Normal)))
                {
                    vertexAttributes.Add(new VertexBufferBinding(new VertexBuffer(shape.Viewer.GraphicsDevice,
                        new VertexDeclaration(new VertexElement(0, VertexElementFormat.Color, VertexElementUsage.Normal, 0)), vertexCount, BufferUsage.None) { Name = "NORMAL_DUMMY" }));
                    // Do not set the SceneryMaterialOptions.PbrHasNormals flag here, so that the shader will know to calculate its own normals. (See: Fox)
                }
                else
                    options |= SceneryMaterialOptions.PbrHasNormals;

                // Cannot proceed without TexCoord_0 neither, must add a dummy one.
                if (!vertexAttributes.Any(a => a.VertexBuffer.VertexDeclaration.GetVertexElements().Any(e => e.VertexElementUsage == VertexElementUsage.TextureCoordinate && e.UsageIndex == 0)))
                {
                    vertexAttributes.Add(new VertexBufferBinding(new VertexBuffer(shape.Viewer.GraphicsDevice,
                        new VertexDeclaration(new VertexElement(0, VertexElementFormat.NormalizedShort2, VertexElementUsage.TextureCoordinate, 0)), vertexCount, BufferUsage.None) { Name = "TEXCOORD_0_DUMMY" }));
                }

                // If we have skins or Color_0 or TexCoord_1, but don't have Tangent, then we must add a dummy one to run through the NormalMapColor pipeline. (See: MorphStressTest with spare TexCoord_1)
                if ((options & (SceneryMaterialOptions.PbrHasSkin | SceneryMaterialOptions.PbrHasMorphTargets)) != 0 || meshPrimitive.Attributes.ContainsKey("COLOR_0") || meshPrimitive.Attributes.ContainsKey("TEXCOORD_1"))
                {
                    options |= SceneryMaterialOptions.PbrHasTexCoord1; // With this we request to call the NormalMapColor pipeline

                    if (!vertexAttributes.Any(a => a.VertexBuffer.VertexDeclaration.GetVertexElements().Any(e => e.VertexElementUsage == VertexElementUsage.Tangent)))
                        vertexAttributes.Add(new VertexBufferBinding(new VertexBuffer(shape.Viewer.GraphicsDevice,
                            new VertexDeclaration(new VertexElement(0, VertexElementFormat.Color, VertexElementUsage.Tangent, 0)), vertexCount, BufferUsage.WriteOnly)
                        { Name = "TANGENT_DUMMY" })); // VertexElementFormat.Color is actually unsigned Byte4 normalized.
                    else if (!TangentsAlwaysCalculatedPerPixel)
                        options |= SceneryMaterialOptions.PbrHasTangents;
                }

                if (meshPrimitive.Attributes.ContainsKey("TANGENT"))
                {
                    // Per-pixel tangent calculation gives better result. In that case we are not using the provided Tangent vertex attribute at all, so it can be removed. (See BoomBox)
                    if (TangentsAlwaysCalculatedPerPixel && !meshPrimitive.Attributes.ContainsKey("COLOR_0") && !meshPrimitive.Attributes.ContainsKey("TEXCOORD_1")
                        && (options & (SceneryMaterialOptions.PbrHasSkin | SceneryMaterialOptions.PbrHasMorphTargets)) == 0)
                    {
                        var removables = vertexAttributes.Where(b => {
                            var ve = b.VertexBuffer.VertexDeclaration.GetVertexElements();
                            return ve.Count() == 1 && ve.Any(e => e.VertexElementUsage == VertexElementUsage.Tangent);
                        });
                        foreach (var r in removables)
                            Disposables.Enqueue(r.VertexBuffer);
                        if (vertexAttributes.RemoveAll(b => removables.Contains(b)) == 0)
                            // Unsuccessful removal because the buffers are interleaved
                            options |= SceneryMaterialOptions.PbrHasTangents;
                    }
                    else
                        options |= SceneryMaterialOptions.PbrHasTangents;
                }

                // When we have a Tangent, must also make sure to have TexCoord_1 and Color_0
                if ((options & (SceneryMaterialOptions.PbrHasTangents | SceneryMaterialOptions.PbrHasSkin | SceneryMaterialOptions.PbrHasMorphTargets)) != 0
                    || meshPrimitive.Attributes.ContainsKey("COLOR_0") || meshPrimitive.Attributes.ContainsKey("TEXCOORD_1"))
                {
                    if (vertexAttributes.Sum(a => a.VertexBuffer.VertexDeclaration.GetVertexElements().Count(e => e.VertexElementUsage == VertexElementUsage.TextureCoordinate)) < 2)
                    {
                        vertexAttributes.Add(new VertexBufferBinding(new VertexBuffer(shape.Viewer.GraphicsDevice,
                            new VertexDeclaration(new VertexElement(0, VertexElementFormat.NormalizedShort2, VertexElementUsage.TextureCoordinate, 1)), vertexCount, BufferUsage.None) { Name = "TEXCOORD_1_DUMMY" }));
                    }
                    if (!vertexAttributes.Any(a => a.VertexBuffer.VertexDeclaration.GetVertexElements().Any(e => e.VertexElementUsage == VertexElementUsage.Color)))
                    {
                        var vertexBuffer = new VertexBuffer(shape.Viewer.GraphicsDevice,
                            new VertexDeclaration(new VertexElement(0, VertexElementFormat.Color, VertexElementUsage.Color, 0)), vertexCount, BufferUsage.None) { Name = "COLOR_0_DUMMY" };
                        vertexBuffer.SetData(Enumerable.Repeat(byte.MaxValue, vertexCount * 4).ToArray()); // Init the colors with white, because it is a multiplier to the sampled colors.
                        vertexAttributes.Add(new VertexBufferBinding(vertexBuffer));
                    }
                    options |= SceneryMaterialOptions.PbrHasTexCoord1;
                }

                // Indicates what position in the target a specific vertex attribute is located at. Element [6] is set to 1 if the primitive has a skin. [7] is the targets count. [8] is the attributes count per target.
                var morphConfig = new int[9];

                // When having morph targets, make sure we have exactly 8 of them.
                if ((options & SceneryMaterialOptions.PbrHasMorphTargets) != 0)
                {
                    // Have to add fake JOINT_0 and WEIGHTS_0 buffers in case we don't have them.
                    if ((options & SceneryMaterialOptions.PbrHasSkin) == 0)
                    {
                        vertexAttributes.Add(new VertexBufferBinding(new VertexBuffer(shape.Viewer.GraphicsDevice,
                            new VertexDeclaration(new VertexElement(0, VertexElementFormat.Byte4, VertexElementUsage.BlendIndices, 0)), vertexCount, BufferUsage.None) { Name = "JOINTS_DUMMY" }));
                        vertexAttributes.Add(new VertexBufferBinding(new VertexBuffer(shape.Viewer.GraphicsDevice,
                            new VertexDeclaration(new VertexElement(0, VertexElementFormat.Color, VertexElementUsage.BlendWeight, 0)), vertexCount, BufferUsage.None) { Name = "WEIGHTS_DUMMY" }));
                    }
                    else
                    {
                        morphConfig[6] = 1;
                    }

                    vertexAttributes.AddRange(meshPrimitive.Targets
                        .SelectMany(t => t)
                        .Select(kvp => distanceLevel.VertexBufferBindings[kvp.Value]));

                    // Bind more buffers if needed for being exactly 8 of them. The content doesn't matter, they will not be accessed anyway, it is just to fool the shader binding mechanism.
                    for (var i = 0; i < RenderProcess.MAX_MORPH_BUFFERS - meshPrimitive.Targets.Length * meshPrimitive.Targets[0].Count; i++)
                        vertexAttributes.Add(vertexAttributes.Last());

                    for (var i = 0; i < 6; i++)
                        morphConfig[i] = -1;
                    var arrangement = meshPrimitive.Targets[0]
                        .Select((kvp, i) => (sourcePosition: MorphTargetAttributes.IndexOf(kvp.Key), targetPosition: i));
                    foreach (var (sourcePosition, targetPosition) in arrangement)
                        morphConfig[sourcePosition] = targetPosition;

                    morphConfig[7] = meshPrimitive.Targets.Length;
                    morphConfig[8] = meshPrimitive.Targets[0].Count;
                }

                // Remove the unused TexCoord_2, _3, etc. attributes, because they might create display artifacts.
                // Most probably these are model errors anyway. (See: MosquitoInAmber with spare TexCoord_2)
                var remove = vertexAttributes.Where(b => b.VertexBuffer.VertexDeclaration.GetVertexElements().Any(e => e.VertexElementUsage == VertexElementUsage.TextureCoordinate && e.UsageIndex > 1));
                foreach (var r in remove)
                    Disposables.Enqueue(r.VertexBuffer);
                vertexAttributes.RemoveAll(b => remove.Contains(b));

                // Sweep
                while (Disposables.TryDequeue(out var vb))
                    vb.Dispose();

                // This is the dummy instance buffer at the end of the vertex buffers. Models with morph targets cannot be instanced, because then the vs_4_0 input limit (16) is exceeded, shader uses 20 inputs.
                if ((options & SceneryMaterialOptions.PbrHasMorphTargets) == 0)
                    vertexAttributes.Add(new VertexBufferBinding(RenderPrimitive.GetDummyVertexBuffer(shape.Viewer.GraphicsDevice)));

                var verticesDrawn = indexCount > 0 ? indexCount : vertexCount;
                switch (meshPrimitive.Mode)
                {
                    case MeshPrimitive.ModeEnum.TRIANGLE_STRIP: indexBufferSet.PrimitiveType = PrimitiveType.TriangleStrip; indexBufferSet.PrimitiveCount = verticesDrawn - 2; break;
                    case MeshPrimitive.ModeEnum.TRIANGLES: indexBufferSet.PrimitiveType = PrimitiveType.TriangleList; indexBufferSet.PrimitiveCount = verticesDrawn / 3; break;
                    case MeshPrimitive.ModeEnum.LINE_STRIP: indexBufferSet.PrimitiveType = PrimitiveType.LineStrip; indexBufferSet.PrimitiveCount = verticesDrawn - 1; break;
                    case MeshPrimitive.ModeEnum.LINES: indexBufferSet.PrimitiveType = PrimitiveType.LineList; indexBufferSet.PrimitiveCount = verticesDrawn / 2; break;
                    default: indexBufferSet.PrimitiveType = PrimitiveType.LineList; indexBufferSet.PrimitiveCount = verticesDrawn / 2; break;
                }

                var key = $"{shape.FilePath}#{material.Name}#{meshPrimitive.Material ?? -1}";

                var sceneryMaterial = shape.Viewer.MaterialManager.Load("PBR", key, (int)options, 0,
                    baseColorTexture, baseColorFactor,
                    metallicRoughnessTexture, metallicFactor, roughtnessFactor,
                    normalTexture, normalScale,
                    occlusionTexture, occlusionStrength,
                    emissiveTexture, emissiveFactor,
                    clearcoatTexture, clearcoatFactor,
                    clearcoatRoughnessTexture, clearcoatRoughnessFactor,
                    clearcoatNormalTexture, clearcoatNormalScale,
                    referenceAlpha, doubleSided,
                    baseColorSamplerState,
                    metallicRoughnessSamplerState,
                    normalSamplerState,
                    occlusionSamplerState,
                    emissiveSamplerState,
                    clearcoatSamplerState,
                    clearcoatRoughnessSamplerState,
                    clearcoatNormalSamplerState);

                if (meshPrimitive.Material != null && !shape.Materials.ContainsKey((int)meshPrimitive.Material) && sceneryMaterial is PbrMaterial pbrMaterial)
                    shape.Materials.Add((int)meshPrimitive.Material, pbrMaterial);

                ShapePrimitives = new[] { new GltfPrimitive(sceneryMaterial, vertexAttributes, gltfFile, distanceLevel, indexBufferSet, skin, hierarchyIndex, hierarchy, texCoords1, texCoords2, texturePacking, morphConfig) };
                ShapePrimitives[0].SortIndex = 0;
            }

        }


        public class GltfPrimitive : ShapePrimitive
        {
            public readonly int[] Joints; // Replaces ShapePrimitive.HierarchyIndex for non-skinned primitives
            public readonly Matrix[] InverseBindMatrices;
            static readonly Matrix[] UnskinnedInverseBindMatrix = new[] { Matrix.Identity };

            /// <summary>
            /// It is used in <see cref="SetRenderMatrices"/> to store the animated bones until uploading them to GPU.
            /// To avoid garbage creation, this array must be reused each time for a new frame calculation.
            /// However the data usually is not getting uploaded before a new calculation begins, so two set of bones needs to be stored:
            /// one is being recalculated while the other is waiting for the upload.
            /// </summary>
            public Matrix[] RenderBonesAltered;
            public Matrix[] RenderBonesRendered;

            /// <summary>
            /// When the primitive has a skin, this texture is used to upload the current bones matrices to GPU.
            /// Each row of the texture contains one bone matrix in 4 RGBA32F pixels.
            /// </summary>
            public readonly Texture2D BonesTexture;
            
            /// <summary>
            /// Indicates what position in the target a specific vertex attribute is located at. Element [6] is set to 1 if the primitive has a skin.
            /// [7] is the targets count. [8] is the attributes count per target.
            /// </summary>
            readonly int[] MorphConfig;
            readonly float[] MorphWeights;
            readonly int MaxActiveMorphTargets;

            /// <summary>
            /// x: baseColor, y: roughness-metallic, z: normal, w: emissive
            /// </summary>
            public readonly Vector4 TexCoords1;
            public readonly Vector4 TexCoords2;

            /// <summary>
            /// 0: occlusion (R), roughnessMetallic (GB) together, normal (RGB) separate, this is the standard
            /// 1: roughnessMetallicOcclusion together, normal (RGB) separate
            /// 2: normalRoughnessMetallic (RG+B+A) together, occlusion (R) separate
            /// 3: occlusionRoughnessMetallic together, normal (RGB) separate
            /// 4: roughnessMetallicOcclusion together, normal (RG) 2 channel separate
            /// 5: occlusionRoughnessMetallic together, normal (RG) 2 channel separate
            /// </summary>
            public readonly float TexturePacking;


            public GltfPrimitive(KHR_lights_punctual light, Gltf gltfFile, GltfDistanceLevel distanceLevel, int hierarchyIndex, int[] hierarchy)
                : this(new EmptyMaterial(distanceLevel.Viewer), Enumerable.Empty<VertexBufferBinding>().ToList(), gltfFile, distanceLevel, new GltfIndexBufferSet(), null, hierarchyIndex, hierarchy, Vector4.Zero, Vector4.Zero, 0, Array.Empty<int>())
            {
                object extension = null;
                AttachedLight = new StaticLight
                {
                    Name = light.name,
                    Type = light.type,
                    Color = light.color != null && light.color.Length > 2 ? new Vector3(light.color[0], light.color[1], light.color[2]) : Vector3.Zero,
                    Intensity = light.intensity,
                    Range = light.range == 0 ? 2000 : light.range,
                    InnerConeAngle = light.spot?.innerConeAngle ?? 0,
                    OuterConeAngle = light.spot?.outerConeAngle ?? MathHelper.PiOver4,
                    ManagedName = (light.Extras?.TryGetValue("OPENRAILS_light_name", out extension) ?? false) && extension is string a ? a : null,
                };
                if (AttachedLight.ManagedName != null)
                    distanceLevel.MatrixNames.Add(AttachedLight.ManagedName);
            }

            public GltfPrimitive(Material material, List<VertexBufferBinding> vertexAttributes, Gltf gltfFile, GltfDistanceLevel distanceLevel, GltfIndexBufferSet indexBufferSet, Skin skin, int hierarchyIndex, int[] hierarchy, Vector4 texCoords1, Vector4 texCoords2, int texturePacking, int[] morphConfig)
                : base(vertexAttributes.ToArray())
            {
                Material = material;
                IndexBuffer = indexBufferSet.IndexBuffer;
                PrimitiveCount = indexBufferSet.PrimitiveCount;
                PrimitiveOffset = indexBufferSet.PrimitiveOffset;
                PrimitiveType = indexBufferSet.PrimitiveType;
                Hierarchy = hierarchy;
                HierarchyIndex = hierarchyIndex;
                TexCoords1 = texCoords1;
                TexCoords2 = texCoords2;
                TexturePacking = texturePacking;

                if (skin == null)
                {
                    // Non-skinned model
                    Joints = new[] { HierarchyIndex };
                    InverseBindMatrices = UnskinnedInverseBindMatrix;
                }
                else
                {
                    // Skinned model
                    Joints = skin.Joints;
                    RenderBonesAltered = Enumerable.Repeat(Matrix.Identity, Joints.Length).ToArray();
                    RenderBonesRendered = Enumerable.Repeat(Matrix.Identity, Joints.Length).ToArray();
                    BonesTexture = Joints.Length > 0 ? new Texture2D(material.Viewer.GraphicsDevice, 4, Joints.Length, false, SurfaceFormat.Vector4) : null;

                    if (!distanceLevel.InverseBindMatrices.TryGetValue((int)skin.InverseBindMatrices, out InverseBindMatrices))
                    {
                        if (skin.InverseBindMatrices == null)
                        {
                            InverseBindMatrices = Enumerable.Repeat(Matrix.Identity, Joints.Length).ToArray();
                        }
                        else
                        {
                            var accessor = gltfFile.Accessors[(int)skin.InverseBindMatrices];
                            Span<float> inputFloats = stackalloc float[accessor.Count * distanceLevel.GetComponentNumber(accessor.Type)];
                            GltfDistanceLevel.Denormalize(accessor, distanceLevel.GetBufferViewSpan(accessor), ref inputFloats);
                            InverseBindMatrices = MemoryMarshal.Cast<float, Matrix>(inputFloats).ToArray();
                        }
                        distanceLevel.InverseBindMatrices.Add((int)skin.InverseBindMatrices, InverseBindMatrices);
                    }
                }
                MorphConfig = morphConfig;
                MorphWeights = distanceLevel.Weights[hierarchyIndex];
                MaxActiveMorphTargets = MorphConfig.ElementAtOrDefault(8) != 0 ? RenderProcess.MAX_MORPH_BUFFERS / MorphConfig.ElementAtOrDefault(8) : 0;
            }

            /// <summary>
            /// Used in case of morphing. Only 8 morph targets can be bound at any time, so need to select the actually active ones.
            /// </summary>
            VertexBufferBinding[] ActiveVertexBufferBindings;
            int[] ActiveWeightIndices;
            float[] ActiveWeights;

            /// <summary>
            /// Select the max. 8 active morph targets and compile the active buffer binding.
            /// </summary>
            /// <returns>(morphConfig, weights)</returns>
            public (int[], float[]) GetMorphingData()
            {
                if (MorphWeights.Length <= MaxActiveMorphTargets)
                    return (MorphConfig, MorphWeights);

                ActiveVertexBufferBindings = ActiveVertexBufferBindings ?? VertexBufferBindings.ToArray();
                Array.Resize(ref ActiveVertexBufferBindings, 16); // This is what the vs_4_0 can handle.
                ActiveWeightIndices = ActiveWeightIndices ?? new int[RenderProcess.MAX_MORPH_BUFFERS];
                ActiveWeights = ActiveWeights ?? new float[RenderProcess.MAX_MORPH_BUFFERS];

                // First select the weight indices
                var w = 0;
                for (var i = 0; i < MorphWeights.Length; i++)
                {
                    if (MorphWeights[i] == 0)
                        continue;
                    if (w < MaxActiveMorphTargets)
                    {
                        ActiveWeightIndices[w++] = i;
                    }
                    else
                    {
                        // Drop targets with the lowest weights if needed, according to the glTF spec recommendation.
                        var smallestWeight = 0;
                        for (var j = 0; j < MaxActiveMorphTargets; j++)
                        {
                            if (MorphWeights[ActiveWeightIndices[j]] < MorphWeights[ActiveWeightIndices[smallestWeight]])
                                smallestWeight = j;
                        }
                        if (MorphWeights[i] > MorphWeights[ActiveWeightIndices[smallestWeight]])
                            ActiveWeightIndices[smallestWeight] = i;
                    }
                }
                MorphConfig[7] = w;

                // Recompile the buffer binding and the weights from the indices
                for (var i = 0; i < w; i++)
                {
                    for (var j = 0; j < MorphConfig[8]; j++)
                        ActiveVertexBufferBindings[8 + MorphConfig[8] * i + j] = VertexBufferBindings[8 + MorphConfig[8] * ActiveWeightIndices[i] + j];
                    ActiveWeights[i] = MorphWeights[ActiveWeightIndices[i]];
                }
                // Fill up the rest of ActiveVertexBufferBindings with anything, as a padding.
                for (var i = 8 + MorphConfig[8] * w; i < 16; i++)
                    ActiveVertexBufferBindings[i] = VertexBufferBindings[8];

                return (MorphConfig, ActiveWeights);
            }

            public bool HasMorphTargets() => MorphWeights.Length > 0;

            public override void Draw(GraphicsDevice graphicsDevice)
            {
                if (PrimitiveCount > 0)
                {
                    // Need to bind only the active morph targets attributes
                    graphicsDevice.SetVertexBuffers(ActiveVertexBufferBindings ?? VertexBufferBindings);
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
        }

        public struct GltfIndexBufferSet
        {
            public IndexBuffer IndexBuffer;
            public int PrimitiveOffset;
            public int PrimitiveCount;
            public PrimitiveType PrimitiveType;
        }
        
        class MSFT_texture_dds
        {
            public int Source { get; set; }
        }

        class MSFT_packing_normalRoughnessMetallic
        {
            public MaterialNormalTextureInfo NormalRoughnessMetallicTexture { get; set; }
        }

        class MSFT_packing_occlusionRoughnessMetallic
        {
            public TextureInfo OcclusionRoughnessMetallicTexture { get; set; }
            public TextureInfo RoughnessMetallicOcclusionTexture { get; set; }
            public MaterialNormalTextureInfo NormalTexture { get; set; }
        }

        class MSFT_lod
        {
            public int[] Ids { get; set; }
        }

        class KHR_lights_punctual_index
        {
            public int light { get; set; }
        }

        public class KHR_lights_punctual
        {
            public LightMode type { get; set; }
            [DefaultValue("")]
            public string name { get; set; }
            [DefaultValue(new[] { 1f, 1f, 1f })]
            public float[] color { get; set; }
            [DefaultValue(1)]
            public float intensity { get; set; }
            [DefaultValue(0)]
            public float range { get; set; }

            public KHR_lights_punctual_spot spot { get; set; }

            public Dictionary<string, object> Extras { get; set; }
        }

        public class KHR_lights_punctual_spot
        {
            [DefaultValue(0)]
            public float innerConeAngle { get; set; }
            [DefaultValue(MathHelper.PiOver4)]
            public float outerConeAngle { get; set; }
        }

        class KHR_lights
        {
            public KHR_lights_punctual[] lights { get; set; }
        }

        public class KHR_materials_clearcoat
        {
            public float ClearcoatFactor { get; set; }
            public TextureInfo ClearcoatTexture { get; set; }
            public float ClearcoatRoughnessFactor { get; set; }
            public TextureInfo ClearcoatRoughnessTexture { get; set; }
            public MaterialNormalTextureInfo ClearcoatNormalTexture { get; set; }
        }

        public class KHR_materials_emissive_strength
        {
            [DefaultValue(1)]
            public float EmissiveStrength { get; set; }
        }

        class KHR_node_Visibility
        {
            public bool Visible { get; set; }
        }

        public class KHR_animation_pointer
        {
            [DefaultValue("")]
            public string Pointer { get; set; }
        }

        /// <summary>
        /// This method is part of the animation handling. Gets the parent that will be animated, for finding a bogie for wheels.
        /// </summary>
        public override int GetAnimationParent(int animationNumber)
        {
            var node = GetAnimationTargetNode(animationNumber);
            var nodeAnimation = -1;
            var h = GetModelHierarchy();
            do
            {
                node = h?.ElementAtOrDefault(node) ?? -1;
                nodeAnimation = GltfAnimations.FindIndex(a => a.Channels?.FirstOrDefault()?.TargetNode == node);
            }
            while (node > -1 && nodeAnimation == -1);
            return nodeAnimation;
        }

        public override Matrix GetMatrixProduct(int iNode) => base.GetMatrixProduct(iNode) * PlusZToForward;
        public override bool IsAnimationArticulation(int number) => GltfAnimations?.ElementAtOrDefault(number)?.Channels?.FirstOrDefault()?.TimeArray == null;
        public override int GetAnimationTargetNode(int animationId) => GltfAnimations?.ElementAtOrDefault(animationId)?.Channels?.FirstOrDefault()?.TargetNode ?? 0;
        public override int GetAnimationNamesCount() => EnableAnimations || ConsistGenerator.GltfVisualTestRun ? GltfAnimations?.Count ?? 0 : 0;

        public bool HasAnimation(int number) => GltfAnimations?.ElementAtOrDefault(number)?.Channels?.FirstOrDefault() != null;
        public float GetAnimationLength(int number) => GltfAnimations?.ElementAtOrDefault(number)?.Channels?.Select(c => c.TimeMax).Max() ?? 0;

        /// <summary>
        /// Calculate the animation matrices of a glTF animation.
        /// </summary>
        /// <param name="animationNumber">The number of the animation to advance.</param>
        /// <param name="time">Actual time in the animation clip in seconds.</param>
        public void Animate(int animationNumber, float time, Matrix[] animatedMatrices)
        {
            if (!EnableAnimations && !ConsistGenerator.GltfVisualTestRun)
                return;

            foreach (var channel in GltfAnimations[animationNumber].Channels)
            {
                // Interpolating between two frames
                var frame1 = 0;
                for (var i = 0; i < channel.TimeArray.Length; i++)
                {
                    if (channel.TimeArray[i] <= time)
                        frame1 = i;
                    else if (channel.TimeArray[i] > time)
                        break;
                }

                var frame2 = Math.Min(channel.TimeArray.Length - 1, frame1 + 1);
                var time1 = channel.TimeArray[frame1];
                var time2 = channel.TimeArray[frame2];

                float amount = 0;
                if (channel.Interpolation != AnimationSampler.InterpolationEnum.STEP && time1 < time2)
                    amount = MathHelper.Clamp((time - time1) / (time2 - time1), 0, 1);

                // See the formula for cubic the spline: https://www.khronos.org/registry/glTF/specs/2.0/glTF-2.0.html#interpolation-cubic
                channel.SetTargetVector3?.Invoke(channel.Interpolation == AnimationSampler.InterpolationEnum.CUBICSPLINE
                    ? Vector3.Hermite(channel.OutputVector3[Property(frame1)], channel.OutputVector3[OutTangent(frame2)], channel.OutputVector3[Property(frame2)], channel.OutputVector3[InTangent(frame2)], amount)
                    : Vector3.Lerp(channel.OutputVector3[frame1], channel.OutputVector3[frame2], amount));
                channel.SetTargetVector4?.Invoke(channel.Interpolation == AnimationSampler.InterpolationEnum.CUBICSPLINE
                    ? Vector4.Hermite(channel.OutputVector4[Property(frame1)], channel.OutputVector4[OutTangent(frame2)], channel.OutputVector4[Property(frame2)], channel.OutputVector4[InTangent(frame2)], amount)
                    : Vector4.Lerp(channel.OutputVector4[frame1], channel.OutputVector4[frame2], amount));
                channel.SetTargetFloat?.Invoke(channel.Interpolation == AnimationSampler.InterpolationEnum.CUBICSPLINE
                    ? MathHelper.Hermite(channel.OutputFloats[Property(frame1)], channel.OutputFloats[OutTangent(frame2)], channel.OutputFloats[Property(frame2)], channel.OutputFloats[InTangent(frame2)], amount)
                    : MathHelper.Lerp(channel.OutputFloats[frame1], channel.OutputFloats[frame2], amount));
                channel.SetTargetQuaternion?.Invoke(((channel.Interpolation == AnimationSampler.InterpolationEnum.CUBICSPLINE
                    ? CsInterp(channel.OutputQuaternion[Property(frame1)], channel.OutputQuaternion[OutTangent(frame2)], channel.OutputQuaternion[Property(frame2)], channel.OutputQuaternion[InTangent(frame2)], amount)
                    : Quaternion.Slerp(channel.OutputQuaternion[frame1], channel.OutputQuaternion[frame2], amount))
                    is var q && q != new Quaternion(0, 0, 0, 0)) ? Quaternion.Normalize(q) : Quaternion.Identity);
                if (channel.SetTargetFloatVector != null && channel.TargetFloatVectorLength > 0)
                {
                    var count = channel.TargetFloatVectorLength;
                    for (var i = 0; i < count; i++)
                        channel.SetTargetFloatVector(i, channel.Interpolation == AnimationSampler.InterpolationEnum.CUBICSPLINE
                            ? MathHelper.Hermite(channel.OutputFloats[Property(frame1) * count + i], channel.OutputFloats[OutTangent(frame2) * count + i], channel.OutputFloats[Property(frame2) * count + i], channel.OutputFloats[InTangent(frame2) * count + i], amount)
                            : MathHelper.Lerp(channel.OutputFloats[frame1 * count + i], channel.OutputFloats[frame2 * count + i], amount));
                }
                if (channel.Path == AnimationChannelTarget.PathEnum.rotation || channel.Path == AnimationChannelTarget.PathEnum.scale || channel.Path == AnimationChannelTarget.PathEnum.translation)
                    animatedMatrices[(int)channel.TargetNode] = Matrix.CreateScale(Scales[(int)channel.TargetNode]) * Matrix.CreateFromQuaternion(Rotations[(int)channel.TargetNode]) * Matrix.CreateTranslation(Translations[(int)channel.TargetNode]);
            }
        }

        // Cubic spline helpers
        static readonly Func<int, int> InTangent = (frame) => frame * 3;
        static readonly Func<int, int> Property = (frame) => frame * 3 + 1;
        static readonly Func<int, int> OutTangent = (frame) => frame * 3 + 2;
        static readonly Func<float, float> A = (t) => 2*t*t*t - 3*t*t + 1;
        static readonly Func<float, float> B = (t) => t*t*t - 2*t*t + t;
        static readonly Func<float, float> C = (t) => -2*t*t*t + 3*t*t;
        static readonly Func<float, float> D = (t) => t*t*t - t*t;
        static readonly Func<Quaternion, Quaternion, Quaternion, Quaternion, float, Quaternion> CsInterp = (v1, b1, v2, a2, t) =>
            Quaternion.Normalize(Quaternion.Multiply(v1, A(t)) + Quaternion.Multiply(b1, B(t)) + Quaternion.Multiply(v2, C(t)) + Quaternion.Multiply(a2, D(t)));

        /// <summary>
        /// The pointers in the glTF animations are used to animate the properties of the materials and lights.
        /// </summary>
        static readonly Dictionary<string, PointerTypes> PointerTemplates = new Dictionary<string, PointerTypes>()
        {
            // Core pointers
            { "/materials/{}/alphaCutoff", PointerTypes.Float },
            { "/materials/{}/emissiveFactor", PointerTypes.Vector3 },
            { "/materials/{}/normalTexture/scale", PointerTypes.Float },
            { "/materials/{}/occlusionTexture/strength", PointerTypes.Float },
            { "/materials/{}/pbrMetallicRoughness/baseColorFactor", PointerTypes.Vector4 },
            { "/materials/{}/pbrMetallicRoughness/metallicFactor", PointerTypes.Float },
            { "/materials/{}/pbrMetallicRoughness/roughnessFactor", PointerTypes.Float },
            { "/meshes/{}/weights", PointerTypes.FloatVector },
            { "/meshes/{}/weights/{}", PointerTypes.Float },
            { "/nodes/{}/translation", PointerTypes.Vector3 },
            { "/nodes/{}/rotation", PointerTypes.Quaternion },
            { "/nodes/{}/scale", PointerTypes.Vector3 },
            { "/nodes/{}/weights", PointerTypes.FloatVector },
            { "/nodes/{}/weights/{}", PointerTypes.Float },
            { "/nodes/{}/extensions/KHR_node_visibility/visible", PointerTypes.Boolean },

            // Extension pointers
            { "/extensions/KHR_lights_punctual/lights/{}/color", PointerTypes.Vector3 },
            { "/extensions/KHR_lights_punctual/lights/{}/intensity", PointerTypes.Float },
            { "/extensions/KHR_lights_punctual/lights/{}/range", PointerTypes.Float },
            { "/extensions/KHR_lights_punctual/lights/{}/spot/innerConeAngle", PointerTypes.Float },
            { "/extensions/KHR_lights_punctual/lights/{}/spot/outerConeAngle", PointerTypes.Float },

        };

        enum PointerTypes
        {
            FloatVector,
            Float,
            Quaternion,
            Vector3,
            Vector4,
            Boolean
        }

        /// <summary>
        /// Adjustments needed for the Khronos sample assets models.
        /// </summary>
        static readonly Dictionary<string, Matrix> SampleModelsAdjustments = new Dictionary<string, Matrix>
        {
            { "2CylinderEngine".ToLower(), Matrix.CreateScale(0.005f) * Matrix.CreateTranslation(0, 2, 0) },
            { "ABeautifulGame".ToLower(), Matrix.CreateScale(10) },
            { "AnimatedColorsCube".ToLower(), Matrix.CreateTranslation(0, 2, 0) },
            { "AnimatedCube".ToLower(), Matrix.CreateTranslation(0, 2, 0) },
            { "AnimatedMorphCube".ToLower(), Matrix.CreateTranslation(0, 2, 0) },
            { "AnimatedMorphSphere".ToLower(), Matrix.CreateTranslation(0, 2, 0) },
            { "AnimatedTriangle".ToLower(), Matrix.CreateTranslation(0, 1, 0) },
            { "AnimationPointerUVs".ToLower(), Matrix.CreateTranslation(0, 5, 0) },
            { "AnisotropyBarnLamp".ToLower(), Matrix.CreateScale(10f) * Matrix.CreateTranslation(0, 2, 0) },
            { "AntiqueCamera".ToLower(), Matrix.CreateScale(0.5f) },
            { "AnisotropyRotationTest".ToLower(), Matrix.CreateTranslation(0, 5, 0) },
            { "AnisotropyStrengthTest".ToLower(), Matrix.CreateTranslation(0, 1, 0) },
            { "AttenuationTest".ToLower(), Matrix.CreateScale(0.3f) * Matrix.CreateTranslation(0, 4, 0) },
            { "Avocado".ToLower(), Matrix.CreateScale(30) },
            { "BarramundiFish".ToLower(), Matrix.CreateScale(10) },
            { "BoomBox".ToLower(), Matrix.CreateScale(100) * Matrix.CreateTranslation(0, 2, 0) },
            { "BoomBoxWithAxes".ToLower(), Matrix.CreateScale(100) * Matrix.CreateTranslation(0, 2, 0) },
            { "Box".ToLower(), Matrix.CreateTranslation(0, 1, 0) },
            { "Box With Spaces".ToLower(), Matrix.CreateTranslation(0, 2, 0) },
            { "BoxAnimated".ToLower(), Matrix.CreateTranslation(0, 1, 0) },
            { "BoxInterleaved".ToLower(), Matrix.CreateTranslation(0, 1, 0) },
            { "BoxTextured".ToLower(), Matrix.CreateTranslation(0, 1, 0) },
            { "BoxTexturedNonPowerOfTwo".ToLower(), Matrix.CreateTranslation(0, 1, 0) },
            { "BoxVertexColors".ToLower(), Matrix.CreateTranslation(0, 1, 0) },
            { "Buggy".ToLower(), Matrix.CreateScale(0.02f) * Matrix.CreateTranslation(0, 1, 0) },
            { "ChairDamaskPurplegold".ToLower(), Matrix.CreateScale(3f) },
            { "ClearCoatTest".ToLower(), Matrix.CreateScale(0.5f) * Matrix.CreateTranslation(0, 3, 0) },
            { "CompareAlphaCoverage".ToLower(), Matrix.CreateTranslation(0, 1, 0) },
            { "CompareAmbientOcclusion".ToLower(), Matrix.CreateScale(3) * Matrix.CreateTranslation(0, 1, 0) },
            { "CompareBaseColor".ToLower(), Matrix.CreateTranslation(0, 1, 0) },
            { "CompareClearcoat".ToLower(), Matrix.CreateTranslation(0, 1, 0) },
            { "CompareDispersion".ToLower(), Matrix.CreateTranslation(0, 1, 0) },
            { "CompareEmissiveStrength".ToLower(), Matrix.CreateTranslation(0, 1, 0) },
            { "CompareIor".ToLower(), Matrix.CreateTranslation(0, 1, 0) },
            { "CompareIridescence".ToLower(), Matrix.CreateTranslation(0, 1, 0) },
            { "CompareMetallic".ToLower(), Matrix.CreateTranslation(0, 1, 0) },
            { "CompareNormal".ToLower(), Matrix.CreateTranslation(0, 1, 0) },
            { "CompareRoughness".ToLower(), Matrix.CreateTranslation(0, 1, 0) },
            { "CompareSheen".ToLower(), Matrix.CreateTranslation(0, 1, 0) },
            { "CompareSpecular".ToLower(), Matrix.CreateTranslation(0, 1, 0) },
            { "CompareTransmission".ToLower(), Matrix.CreateTranslation(0, 1, 0) },
            { "CompareVolume".ToLower(), Matrix.CreateTranslation(0, 1, 0) },
            { "Corset".ToLower(), Matrix.CreateScale(30) },
            { "Cube".ToLower(), Matrix.CreateTranslation(0, 2, 0) },
            { "DamagedHelmet".ToLower(), Matrix.CreateTranslation(0, 2, 0) },
            { "DiffuseTransmissionPlant".ToLower(), Matrix.CreateScale(4) * Matrix.CreateTranslation(0, 1, 0) },
            { "DiffuseTransmissionTeacup".ToLower(), Matrix.CreateScale(8) * Matrix.CreateTranslation(0, 1, 0) },
            { "DirectionalLight".ToLower(), Matrix.CreateTranslation(0, 1, 0) },
            { "DispersionTest".ToLower(), Matrix.CreateScale(16) * Matrix.CreateTranslation(0, 1, 0) },
            { "DragonAttenuation".ToLower(), Matrix.CreateTranslation(0, 1, 0) },
            { "DragonDispersion".ToLower(), Matrix.CreateTranslation(0, 1, 0) },
            { "EmissiveStrengthTest".ToLower(), Matrix.CreateScale(0.5f) * Matrix.CreateTranslation(0, 3, 0) },
            { "EnvironmentTest".ToLower(), Matrix.CreateScale(0.5f) },
            { "FlightHelmet".ToLower(), Matrix.CreateScale(5) },
            { "Fox".ToLower(), Matrix.CreateScale(0.02f) },
            { "GearboxAssy".ToLower(), Matrix.CreateScale(0.5f) * Matrix.CreateTranslation(80, -5, 0) },
            { "GlamVelvetSofa".ToLower(), Matrix.CreateScale(2) },
            { "GlassBrokenWindow".ToLower(), Matrix.CreateScale(5) },
            { "GlassHurricaneCandleHolder".ToLower(), Matrix.CreateScale(5) },
            { "GlassVaseFlowers".ToLower(), Matrix.CreateScale(10) },
            { "InterpolationTest".ToLower(), Matrix.CreateScale(0.5f) * Matrix.CreateTranslation(0, 2, 0) },
            { "IORTestGrid".ToLower(), Matrix.CreateScale(5) * Matrix.CreateTranslation(0, 2, 0) },
            { "IridescenceAbalone".ToLower(), Matrix.CreateScale(5) * Matrix.CreateTranslation(0, 1, 0) },
            { "IridescenceDielectricSpheres".ToLower(), Matrix.CreateScale(0.2f) * Matrix.CreateTranslation(0, 3, 0) },
            { "IridescenceLamp".ToLower(), Matrix.CreateScale(5) },
            { "IridescenceMetallicSpheres".ToLower(), Matrix.CreateScale(0.2f) * Matrix.CreateTranslation(0, 3, 0) },
            { "IridescenceSuzanne".ToLower(), Matrix.CreateTranslation(0, 2, 0) },
            { "IridescentDishWithOlives".ToLower(), Matrix.CreateScale(10) },
            { "Lantern".ToLower(), Matrix.CreateScale(0.2f) },
            { "MandarinOrange".ToLower(), Matrix.CreateScale(30) },
            { "MaterialsVariantsShoe".ToLower(), Matrix.CreateScale(5) },
            { "MeshPrimitiveModes".ToLower(), Matrix.CreateTranslation(0, 5, 0) },
            { "MetalRoughSpheres".ToLower(), Matrix.CreateTranslation(0, 5, 0) },
            { "MetalRoughSpheresNoTextures".ToLower(), Matrix.CreateScale(800) * Matrix.CreateTranslation(0, 1, 0) },
            { "MorphPrimitivesTest".ToLower(), Matrix.CreateScale(2) * Matrix.CreateTranslation(0, 1, 0) },
            { "MosquitoInAmber".ToLower(), Matrix.CreateScale(25) * Matrix.CreateTranslation(0, 1, 0) },
            { "MultiUVTest".ToLower(), Matrix.CreateTranslation(0, 2, 0) },
            { "NegativeScaleTest".ToLower(), Matrix.CreateTranslation(0, 4, 0) },
            { "NormalTangentMirrorTest".ToLower(), Matrix.CreateScale(2) * Matrix.CreateTranslation(0, 2, 0) },
            { "NormalTangentTest".ToLower(), Matrix.CreateScale(2) * Matrix.CreateTranslation(0, 2, 0) },
            { "OrientationTest".ToLower(), Matrix.CreateScale(0.2f) * Matrix.CreateTranslation(0, 2, 0) },
            { "PlaysetLightTest".ToLower(), Matrix.CreateScale(30) },
            { "PointLightIntensityTest".ToLower(), Matrix.CreateTranslation(0, 5, 0) },
            { "PotOfCoals".ToLower(), Matrix.CreateScale(30) },
            { "PotOfCoalsAnimationPointer".ToLower(), Matrix.CreateScale(30) },
            { "PrimitiveModeNormalsTest".ToLower(), Matrix.CreateScale(0.5f) * Matrix.CreateTranslation(0, 5, 0) },
            { "ReciprocatingSaw".ToLower(), Matrix.CreateScale(0.01f) * Matrix.CreateTranslation(0, 3, 0) },
            { "RecursiveSkeletons".ToLower(), Matrix.CreateScale(0.05f) },
            { "RiggedSimple".ToLower(), Matrix.CreateTranslation(0, 5, 0) },
            { "SciFiHelmet".ToLower(), Matrix.CreateTranslation(0, 2, 0) },
            { "SheenChair".ToLower(), Matrix.CreateScale(2) },
            { "SheenCloth".ToLower(), Matrix.CreateScale(50) },
            { "SheenTestGrid".ToLower(), Matrix.CreateScale(5) * Matrix.CreateTranslation(0, 4, 0)},
            { "SpecGlossVsMetalRough".ToLower(), Matrix.CreateScale(7) * Matrix.CreateTranslation(0, 2, 0) },
            { "SpecularTest".ToLower(), Matrix.CreateScale(5) * Matrix.CreateTranslation(0, 2, 0) },
            { "StainedGlassLamp".ToLower(), Matrix.CreateScale(3) },
            { "SunglassesKhronos".ToLower(), Matrix.CreateScale(30) },
            { "Suzanne".ToLower(), Matrix.CreateTranslation(0, 2, 0) },
            { "TextureCoordinateTest".ToLower(), Matrix.CreateTranslation(0, 2, 0) },
            { "TextureEncodingTest".ToLower(), Matrix.CreateScale(0.5f) * Matrix.CreateTranslation(0, 3, 0) },
            { "TextureLinearInterpolationTest".ToLower(), Matrix.CreateTranslation(0, 2, 0) },
            { "TextureSettingsTest".ToLower(), Matrix.CreateScale(0.5f) * Matrix.CreateTranslation(0, 4, 0) },
            { "TextureTransformMultiTest".ToLower(), Matrix.CreateScale(2) * Matrix.CreateTranslation(0, 4, 0) },
            { "TextureTransformTest".ToLower(), Matrix.CreateTranslation(0, 2, 0) },
            { "ToyCar".ToLower(), Matrix.CreateScale(100) * Matrix.CreateTranslation(0, 2, 0)},
            { "TransmissionRoughnessTest".ToLower(), Matrix.CreateScale(5) * Matrix.CreateTranslation(0, 3, 0) },
            { "TransmissionTest".ToLower(), Matrix.CreateScale(3) * Matrix.CreateTranslation(0, 2, 0) },
            { "TwoSidedPlane".ToLower(), Matrix.CreateTranslation(0, 1, 0) },
            { "UnlitTest".ToLower(), Matrix.CreateTranslation(0, 2, 0) },
            { "VertexColorTest".ToLower(), Matrix.CreateScale(2) * Matrix.CreateTranslation(0, 2, 0) },
            { "WaterBottle".ToLower(), Matrix.CreateScale(7) * Matrix.CreateTranslation(0, 2, 0) },
        };
        readonly Matrix SampleModelsAdjustment;
    }

    class GltfAnimation
    {
        public string Name;
        public List<GltfAnimationChannel> Channels = new List<GltfAnimationChannel>();

        /// <summary>
        /// Used for calculating rotation-to-speed type animations,
        /// either rotationAngle = travelDistance / wheelRadius
        /// or numRotations = travelDistance / wheelRadius / 2𝜋
        /// </summary>
        public float ExtrasWheelRadius;

        public GltfAnimation(string name)
        {
            Name = name;
        }
    }

    class GltfAnimationChannel
    {
        public int? TargetNode;
        public AnimationChannelTarget.PathEnum Path;
        public AnimationSampler.InterpolationEnum Interpolation;
        public float[] TimeArray;
        public float TimeMin;
        public float TimeMax;
        public Vector3[] OutputVector3;
        public Vector4[] OutputVector4;
        public Quaternion[] OutputQuaternion;
        public float[] OutputFloats;

        public string Pointer;
        public Action<float> SetTargetFloat;
        public Action<Vector3> SetTargetVector3;
        public Action<Vector4> SetTargetVector4;
        public Action<Quaternion> SetTargetQuaternion;
        public Action<int, float> SetTargetFloatVector;
        public int TargetFloatVectorLength;
    }
}
