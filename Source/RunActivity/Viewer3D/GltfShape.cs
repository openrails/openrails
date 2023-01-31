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
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.ComponentModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Viewer3D.Common;
using Orts.Viewer3D.Processes;
using ORTS.Common;
using glTFLoader.Schema;
using Orts.Simulation.AIs;

namespace Orts.Viewer3D
{
    public class GltfShape : SharedShape
    {
        public static bool EnableAnimations { get; set; }

        public static List<string> ExtensionsSupported = new List<string>
        {
            "KHR_lights_punctual",
            "KHR_materials_unlit",
            "KHR_materials_clearcoat",
            "MSFT_lod",
            "MSFT_texture_dds",
            "MSFT_packing_normalRoughnessMetallic",
            "MSFT_packing_occlusionRoughnessMetallic",
        };

        string FileDir { get; set; }
        int SkeletonRootNode;
        public bool MsfsFlavoured;

        internal ImmutableArray<Vector3> Scales;
        internal ImmutableArray<Quaternion> Rotations;
        internal ImmutableArray<Vector3> Translations;
        internal ImmutableArray<float[]> Weights;

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

        readonly List<GltfAnimation> GltfAnimations = new List<GltfAnimation>();
        
        public float[] MinimumScreenCoverages = new[] { 0f };
        public readonly Vector4[] BoundingBoxNodes = new Vector4[8];

        /// <summary>
        /// All vertex buffers in a gltf file. The key is the accessor number.
        /// </summary>
        internal Dictionary<int, VertexBufferBinding> VertexBuffers = new Dictionary<int, VertexBufferBinding>();
        internal Dictionary<int, byte[]> BinaryBuffers = new Dictionary<int, byte[]>();

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
        }

        protected override void LoadContent()
        {
            Trace.Write("G");
            var textureFlags = Helpers.TextureFlags.None;

            Dictionary<int, string> externalLods = new Dictionary<int, string>();

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
                        externalLods[lod] = file;
                }
            }

            if (!externalLods.Any())
                externalLods.Add(0, FilePath);
            LodControls = new[] { new GltfLodControl(this, externalLods) };

            if (EnvironmentMapSpecularDay == null)
            {
                // TODO: split the equirectangular specular panorama image to a cube map for saving the pixel shader instructions of converting the
                // cartesian cooridinates to polar for sampling. Couldn't find a converter though, that also supports RGBD color encoding.
                // RGBD is an encoding where a divider is stored in the alpha channel to reconstruct the High Dynamic Range of the RGB colors.
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

        public override Matrix SetRenderMatrices(ShapePrimitive baseShapePrimitive, Matrix[] animatedMatrices, ref Matrix tileTranslation, out Matrix[] bones)
        {
            var shapePrimitive = baseShapePrimitive as GltfPrimitive;
            bones = Enumerable.Repeat(Matrix.Identity, Math.Min(RenderProcess.MAX_BONES, shapePrimitive.Joints.Length)).ToArray();
            for (var j = 0; j < bones.Length; j++)
            {
                bones[j] = shapePrimitive.InverseBindMatrices[j];
                var hi = shapePrimitive.Joints[j];
                while (hi >= 0 && hi < shapePrimitive.Hierarchy.Length)
                {
                    Matrix.Multiply(ref bones[j], ref animatedMatrices[hi], out bones[j]);
                    hi = shapePrimitive.Hierarchy[hi];
                }
                Matrix.Multiply(ref bones[j], ref PlusZToForward, out bones[j]);

                // The ConsistGenerator is used to show all the Khronos sample models for testing purposes. However they need adjustments to show them all at once.
                if (ConsistGenerator.GeneratedRun && SampleModelsAdjustments.TryGetValue(Path.GetFileNameWithoutExtension(FilePath), out var adjustment))
                    Matrix.Multiply(ref bones[j], ref adjustment, out bones[j]);
                
                Matrix.Multiply(ref bones[j], ref tileTranslation, out bones[j]);
            }

            // Skinned primitive
            if (shapePrimitive.Joints.Length > 1)
                return Matrix.Identity;

            // Non-skinned primitive
            var matrix = bones[0];
            bones = null;
            return matrix;
        }

        internal Func<BinaryReader, float> GetNormalizedReader(Accessor.ComponentTypeEnum componentType)
        {
            switch (componentType)
            {
                case Accessor.ComponentTypeEnum.UNSIGNED_BYTE: return (br) => br.ReadByte() / 255.0f;
                case Accessor.ComponentTypeEnum.UNSIGNED_SHORT: return (br) => br.ReadUInt16() / 65535.0f;
                case Accessor.ComponentTypeEnum.BYTE: return (br) => Math.Max(br.ReadSByte() / 127.0f, -1.0f);
                // Component type 5122 "SHORT" is a 16 bit int by the glTF specification, but is used as a 16 bit float (half) by asobo-msfs: 
                case Accessor.ComponentTypeEnum.SHORT: return (br) => MsfsFlavoured ? ToTwoByteFloat(br.ReadBytes(2)) : Math.Max(br.ReadInt16() / 32767.0f, -1.0f); // the prior is br.ReadHalf() in fact
                case Accessor.ComponentTypeEnum.FLOAT:
                default: return (br) => br.ReadSingle();
            }
        }

        internal static Func<BinaryReader, ushort> GetIntegerReader(AccessorSparseIndices.ComponentTypeEnum componentTypeEnum) => GetIntegerReader((Accessor.ComponentTypeEnum)componentTypeEnum);
        internal static Func<BinaryReader, ushort> GetIntegerReader(Accessor.ComponentTypeEnum componentTypeEnum)
        {
            switch (componentTypeEnum)
            {
                case Accessor.ComponentTypeEnum.BYTE: return (br) => (ushort)br.ReadSByte();
                case Accessor.ComponentTypeEnum.UNSIGNED_INT: return (br) => (ushort)br.ReadUInt32();
                case Accessor.ComponentTypeEnum.UNSIGNED_BYTE: return (br) => br.ReadByte();
                case Accessor.ComponentTypeEnum.UNSIGNED_SHORT:
                default: return (br) => br.ReadUInt16();
            }
        }

        static float ToTwoByteFloat(byte[] bytes) // Hi, Lo
        {
            var intVal = BitConverter.ToInt32(new byte[] { bytes[0], bytes[1], 0, 0 }, 0);

            int mant = intVal & 0x03ff;
            int exp = intVal & 0x7c00;
            if (exp == 0x7c00) exp = 0x3fc00;
            else if (exp != 0)
            {
                exp += 0x1c000;
                if (mant == 0 && exp > 0x1c400)
                    return BitConverter.ToSingle(BitConverter.GetBytes((intVal & 0x8000) << 16 | exp << 13 | 0x3ff), 0);
            }
            else if (mant != 0)
            {
                exp = 0x1c400;
                do
                {
                    mant <<= 1;
                    exp -= 0x400;
                } while ((mant & 0x400) == 0);
                mant &= 0x3ff;
            }
            return BitConverter.ToSingle(BitConverter.GetBytes((intVal & 0x8000) << 16 | (exp | mant) << 13), 0);
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

                    if (shape.MatrixNames.Count < (gltfFile.Animations?.Length ?? 0))
                        shape.MatrixNames.AddRange(Enumerable.Repeat("", gltfFile.Animations.Length - shape.MatrixNames.Count));

                    if (gltfFile.ExtensionsRequired != null)
                    {
                        var unsupportedExtensions = new List<string>();
                        foreach (var extensionRequired in gltfFile.ExtensionsRequired)
                            if (!ExtensionsSupported.Contains(extensionRequired))
                                unsupportedExtensions.Add($"\"{extensionRequired}\"");
                        if (unsupportedExtensions.Any())
                            Trace.TraceWarning($"glTF required extension {string.Join(", ", unsupportedExtensions)} is unsupported in file {externalLods[id]}");
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
                        }
                    }
                    if (internalLodsNumber > 0)
                    {
                        for (var i = 0; i < internalLodsNumber; i++)
                            distanceLevels.Add(new GltfDistanceLevel(shape, i, gltfFile, externalLods[id]));
                        // Use the internal lods instead of the externals, if available.
                        break;
                    }
                    distanceLevels.Add(new GltfDistanceLevel(shape, id, gltfFile, externalLods[id]));
                    shape.BinaryBuffers.Clear();
                    shape.VertexBuffers.Clear();
                }
                DistanceLevels = distanceLevels.ToArray();

                // Sweep the resources not used anymore
                shape.BinaryBuffers = null;
                shape.VertexBuffers = null;
            }
        }

        public class GltfDistanceLevel : DistanceLevel
        {
            // See the glTF specification at https://www.khronos.org/registry/glTF/specs/2.0/glTF-2.0.html
            readonly Gltf Gltf;
            readonly string GltfDir;
            readonly string GltfFileName;
            Dictionary<int, byte[]> BinaryBuffers => Shape.BinaryBuffers;

            /// <summary>
            /// All inverse bind matrices in a gltf file. The key is the accessor number.
            /// </summary>
            internal Dictionary<int, Matrix[]> AllInverseBindMatrices = new Dictionary<int, Matrix[]>();

            internal readonly ImmutableArray<Matrix> Matrices;
            readonly ImmutableArray<Vector3> Scales;
            readonly ImmutableArray<Quaternion> Rotations;
            readonly ImmutableArray<Vector3> Translations;
            readonly ImmutableArray<float[]> Weights;

            internal readonly Viewer Viewer;
            internal readonly GltfShape Shape;

            static readonly Stack<int> TempStack = new Stack<int>(); // (nodeNumber, parentIndex)

            public GltfDistanceLevel(GltfShape shape, int lodId, Gltf gltfFile, string gltfFileName)
            {
                ViewingDistance = float.MaxValue; // glTF is using screen coverage, so this one is set for not getting into the way accidentally
                ViewSphereRadius = 100;
                var morphWarning = false;

                Shape = shape;
                Viewer = shape.Viewer;

                Gltf = gltfFile;
                GltfDir = Path.GetDirectoryName(gltfFileName);
                GltfFileName = gltfFileName;

                KHR_lights gltfLights = null;
                object extension = null;
                if (gltfFile.Extensions?.TryGetValue("KHR_lights_punctual", out extension) ?? false)
                    gltfLights = Newtonsoft.Json.JsonConvert.DeserializeObject<KHR_lights>(extension.ToString());

                Weights = gltfFile.Nodes.Select(node => gltfFile.Meshes?.ElementAtOrDefault(node.Mesh ?? -1)?.Weights).ToImmutableArray();
                Scales = gltfFile.Nodes.Select(node => node.Scale == null ? Vector3.One : new Vector3(node.Scale[0], node.Scale[1], node.Scale[2])).ToImmutableArray();
                Rotations = gltfFile.Nodes.Select(node => node.Rotation == null ? Quaternion.Identity : new Quaternion(node.Rotation[0], node.Rotation[1], node.Rotation[2], node.Rotation[3])).ToImmutableArray();
                Translations = gltfFile.Nodes.Select(node => node.Translation == null ? Vector3.Zero : new Vector3(node.Translation[0], node.Translation[1], node.Translation[2])).ToImmutableArray();
                Matrices = gltfFile.Nodes.Select((node, i) => node.Matrix == null ? Matrix.Identity : new Matrix(
                    node.Matrix[0], node.Matrix[1], node.Matrix[2], node.Matrix[3],
                    node.Matrix[4], node.Matrix[5], node.Matrix[6], node.Matrix[7],
                    node.Matrix[8], node.Matrix[9], node.Matrix[10], node.Matrix[11],
                    node.Matrix[12], node.Matrix[13], node.Matrix[14], node.Matrix[15])
                    * Matrix.CreateScale(Scales[i]) * Matrix.CreateFromQuaternion(Rotations[i]) * Matrix.CreateTranslation(Translations[i])).ToImmutableArray();

                var hierarchy = Enumerable.Repeat(-1, gltfFile.Nodes.Length).ToArray();
                var parents = new Dictionary<int, int>();
                var lods = Enumerable.Repeat(-1, gltfFile.Nodes.Length).ToArray(); // -1: common; 0, 1, 3, etc.: the lod the node belongs to
                Dictionary<int, (string name, float radius)> articulations = null;
                var meshes = new Dictionary<int, Node>();
                var lights = new Dictionary<int, KHR_lights_punctual>();

                TempStack.Clear();
                Array.ForEach(gltfFile.Scenes.ElementAtOrDefault(gltfFile.Scene ?? 0).Nodes, node => TempStack.Push(node));
                while (TempStack.Any())
                {
                    var nodeNumber = TempStack.Pop();
                    var node = gltfFile.Nodes[nodeNumber];
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
                                // The node defined in the MSFT_lod extension is a substitute of the current one, not an additional new step-to.
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
                                lights.Add(nodeNumber, gltfLights.lights[(int)lightId]);
                        }

                        if ((node.Extras?.TryGetValue("OPENRAILS_animation_name", out extension) ?? false) && extension is string name)
                        {
                            var radius = 0f;
                            if ((node.Extras?.TryGetValue("OPENRAILS_animation_wheelradius", out extension) ?? false) && extension is string wheelRadius)
                                float.TryParse(wheelRadius, out radius);

                            articulations = articulations ?? new Dictionary<int, (string, float)>();
                            articulations.Add(nodeNumber, (name, radius));
                        }
                    }
                }
                var subObjects = new List<SubObject>();
                foreach (var hierIndex in meshes.Keys)
                {
                    var node = meshes[hierIndex];
                    var mesh = gltfFile.Meshes[(int)node.Mesh];
                    var skin = node.Skin != null ? gltfFile.Skins[(int)node.Skin] : null;

                    for (var i = 0; i < mesh.Primitives.Length; i++)
                        subObjects.Add(new GltfSubObject(mesh.Primitives[i], $"{mesh.Name}[{i}]", hierIndex, hierarchy, Helpers.TextureFlags.None, gltfFile, shape, this, skin));
                }
                foreach (var hierIndex in lights.Keys)
                {
                    subObjects.Add(new GltfSubObject(lights[hierIndex], hierIndex, hierarchy, gltfFile, shape, this));
                }
                SubObjects = subObjects.ToArray();

                if (lodId == 0)
                {
                    shape.Matrices = Matrices.ToArray();
                    shape.Scales = Scales;
                    shape.Rotations = Rotations;
                    shape.Translations = Translations;
                    shape.Weights = Weights;

                    if (SubObjects.FirstOrDefault() is GltfSubObject gltfSubObject)
                    {
                        var minPosition = Vector4.Transform(gltfSubObject.MinPosition, Matrices[gltfSubObject.HierarchyIndex]);
                        var maxPosition = Vector4.Transform(gltfSubObject.MaxPosition, Matrices[gltfSubObject.HierarchyIndex]);
                        foreach (GltfSubObject subObject in SubObjects.Cast<GltfSubObject>())
                        {
                            var soMinPosition = Vector4.Transform(subObject.MinPosition, Matrices[subObject.HierarchyIndex]);
                            var soMaxPosition = Vector4.Transform(subObject.MaxPosition, Matrices[subObject.HierarchyIndex]);
                            minPosition = Vector4.Min(minPosition, soMinPosition);
                            maxPosition = Vector4.Max(maxPosition, soMaxPosition);
                        }
                        shape.BoundingBoxNodes[0] = minPosition;
                        shape.BoundingBoxNodes[1] = new Vector4(minPosition.X, minPosition.Y, maxPosition.Z, 1);
                        shape.BoundingBoxNodes[2] = new Vector4(minPosition.X, maxPosition.Y, maxPosition.Z, 1);
                        shape.BoundingBoxNodes[3] = new Vector4(minPosition.X, maxPosition.Y, minPosition.Z, 1);
                        shape.BoundingBoxNodes[4] = new Vector4(maxPosition.X, minPosition.Y, minPosition.Z, 1);
                        shape.BoundingBoxNodes[5] = new Vector4(maxPosition.X, minPosition.Y, maxPosition.Z, 1);
                        shape.BoundingBoxNodes[6] = new Vector4(maxPosition.X, maxPosition.Y, minPosition.Z, 1);
                        shape.BoundingBoxNodes[7] = maxPosition;
                    }

                    for (var j = 0; j < (gltfFile.Animations?.Length ?? 0); j++)
                    {
                        var gltfAnimation = gltfFile.Animations[j];

                        // Use MatrixNames for storing animation and articulation names.
                        // Here the MatrixNames are not bound to nodes (and matrices), but rather to the animation number.
                        shape.MatrixNames[j] = gltfAnimation.Name ?? "";
                        var animation = new GltfAnimation(gltfAnimation.Name);

                        for (var k = 0; k < gltfAnimation.Channels.Length; k++)
                        {
                            var gltfChannel = gltfAnimation.Channels[k];
                            if (gltfChannel.Target.Node == null) // then this is defined by an extension, which is not supported here.
                                continue;

                            var channel = new GltfAnimationChannel();
                            animation.Channels.Add(channel);

                            channel.Path = gltfChannel.Target.Path;
                            channel.TargetNode = (int)gltfChannel.Target.Node;

                            var sampler = gltfAnimation.Samplers[gltfChannel.Sampler];
                            var inputAccessor = gltfFile.Accessors[sampler.Input];
                            channel.TimeArray = new float[inputAccessor.Count];
                            channel.TimeMin = inputAccessor.Min[0];
                            channel.TimeMax = inputAccessor.Max[0];
                            var readInput = shape.GetNormalizedReader(inputAccessor.ComponentType);
                            using (var br = new BinaryReader(GetBufferView(inputAccessor, out _)))
                            {
                                for (var i = 0; i < inputAccessor.Count; i++)
                                    channel.TimeArray[i] = readInput(br);
                            }

                            var outputAccessor = gltfFile.Accessors[sampler.Output];
                            switch (channel.Path)
                            {
                                case AnimationChannelTarget.PathEnum.rotation: channel.OutputQuaternion = new Quaternion[outputAccessor.Count]; break;
                                case AnimationChannelTarget.PathEnum.scale:
                                case AnimationChannelTarget.PathEnum.translation: channel.OutputVector3 = new Vector3[outputAccessor.Count]; break;
                                case AnimationChannelTarget.PathEnum.weights: channel.OutputWeights = new float[outputAccessor.Count]; break;
                            }
                            var readOutput = shape.GetNormalizedReader(outputAccessor.ComponentType);
                            using (var br = new BinaryReader(GetBufferView(outputAccessor, out _)))
                            {
                                for (var i = 0; i < outputAccessor.Count; i++)
                                    switch (channel.Path)
                                    {
                                        case AnimationChannelTarget.PathEnum.rotation: channel.OutputQuaternion[i] = new Quaternion(readOutput(br), readOutput(br), readOutput(br), readOutput(br)); break;
                                        case AnimationChannelTarget.PathEnum.scale:
                                        case AnimationChannelTarget.PathEnum.translation: channel.OutputVector3[i] = new Vector3(readOutput(br), readOutput(br), readOutput(br)); break;
                                        case AnimationChannelTarget.PathEnum.weights: channel.OutputWeights[i] = readOutput(br); morphWarning = true; break;
                                    }
                            }
                            channel.Interpolation = sampler.Interpolation;
                        }
                        shape.GltfAnimations.Add(animation);
                    }
                    if (morphWarning)
                        Trace.TraceInformation($"glTF morphing animation is unsupported in file {gltfFileName}");

                    if (articulations != null)
                    {
                        if (shape.MatrixNames.Count < (gltfFile.Animations?.Length ?? 0) + articulations.Count)
                            shape.MatrixNames.AddRange(Enumerable.Repeat("", (gltfFile.Animations?.Length ?? 0) + articulations.Count - shape.MatrixNames.Count));

                        foreach (var nodeNumber in articulations.Keys)
                        {
                            var animation = shape.GltfAnimations.FirstOrDefault(a => a.Name == articulations[nodeNumber].name);
                            if (animation == null)
                            {
                                animation = new GltfAnimation(articulations[nodeNumber].name) { ExtrasWheelRadius = articulations[nodeNumber].radius };
                                shape.GltfAnimations.Add(animation);
                                shape.MatrixNames[shape.GltfAnimations.Count - 1] = articulations[nodeNumber].name ?? "";
                            }
                            animation.Channels.Add(new GltfAnimationChannel() { TargetNode = nodeNumber });
                        }
                    }

//if (shape.GltfAnimations.Count > 0) { shape.GltfAnimations.Add(shape.GltfAnimations[0]); shape.MatrixNames[shape.GltfAnimations.Count - 1] = "ORTSITEM1CONTINUOUS"; }
                }
            }

            internal Stream GetBufferView(AccessorSparseIndices accessor, out int? byteStride) => GetBufferView(accessor.BufferView, accessor.ByteOffset, out byteStride);
            internal Stream GetBufferView(AccessorSparseValues accessor, out int? byteStride) => GetBufferView(accessor.BufferView, accessor.ByteOffset, out byteStride);
            internal Stream GetBufferView(Accessor accessor, out int? byteStride) => GetBufferView(accessor.BufferView, accessor.ByteOffset, out byteStride);
            internal Stream GetBufferView(int? bufferViewNumber, int accessorByteOffset, out int? byteStride)
            {
                byteStride = null;
                if (bufferViewNumber == null)
                    return Stream.Null;
                var bufferView = Gltf.BufferViews[(int)bufferViewNumber];
                byteStride = bufferView.ByteStride;
                if (!BinaryBuffers.TryGetValue(bufferView.Buffer, out var bytes))
                    BinaryBuffers.Add(bufferView.Buffer, bytes = glTFLoader.Interface.LoadBinaryBuffer(Gltf, bufferView.Buffer, GltfFileName));
                var stream = new MemoryStream(bytes);
                stream.Seek(bufferView.ByteOffset + accessorByteOffset, SeekOrigin.Begin);
                return stream;
            }

            internal int GetSizeInBytes(Accessor accessor) => GetComponentNumber(accessor.Type) * GetCompenentSizeInBytes(accessor.ComponentType);
            
            int GetComponentNumber(Accessor.TypeEnum type)
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

            int GetCompenentSizeInBytes(Accessor.ComponentTypeEnum componentType)
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

            public GltfSubObject(KHR_lights_punctual light, int hierarchyIndex, int[] hierarchy, Gltf gltfFile, GltfShape shape, GltfDistanceLevel distanceLevel)
            {
                ShapePrimitives = new[] { new GltfPrimitive(light, gltfFile, distanceLevel, hierarchyIndex, hierarchy) };
            }

            public GltfSubObject(MeshPrimitive meshPrimitive, string name, int hierarchyIndex, int[] hierarchy, Helpers.TextureFlags textureFlags, Gltf gltfFile, GltfShape shape, GltfDistanceLevel distanceLevel, Skin skin)
            {
                var material = meshPrimitive.Material == null ? DefaultGltfMaterial : gltfFile.Materials[(int)meshPrimitive.Material];

                var options = SceneryMaterialOptions.None;

                if (!material.Extensions?.ContainsKey("KHR_materials_unlit") ?? true)
                    options |= SceneryMaterialOptions.Diffuse;

                if (skin != null)
                {
                    options |= SceneryMaterialOptions.PbrHasSkin;
                    shape.SkeletonRootNode = skin.Skeleton ?? 0;
                }

                if (!shape.MsfsFlavoured && distanceLevel.Matrices[hierarchyIndex].Determinant() > 0)
                    // This is according to the glTF spec
                    options |= SceneryMaterialOptions.PbrCullClockWise;
                else if (shape.MsfsFlavoured && distanceLevel.Matrices[hierarchyIndex].Determinant() < 0)
                    // Msfs seems to be using this reversed
                    options |= SceneryMaterialOptions.PbrCullClockWise;

                var referenceAlpha = 0f;
                var doubleSided = material.DoubleSided;

                switch (material.AlphaMode)
                {
                    case glTFLoader.Schema.Material.AlphaModeEnum.BLEND: options |= SceneryMaterialOptions.AlphaBlendingBlend; break;
                    case glTFLoader.Schema.Material.AlphaModeEnum.MASK: options |= SceneryMaterialOptions.AlphaTest; referenceAlpha = material.AlphaCutoff; break;
                    case glTFLoader.Schema.Material.AlphaModeEnum.OPAQUE:
                    default: break;
                }

                Vector4 texCoords1 = Vector4.Zero; // x: baseColor, y: roughness-metallic, z: normal, w: emissive
                Vector4 texCoords2 = Vector4.Zero; // x: clearcoat, y: clearcoat-roughness, z: clearcoat-normal, w: occlusion

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
                // metallicRoughness texture G = roughness, B = metalness, linear, may be > 8 bit
                // normal texture is RGB linear, B should be >= 0.5. All channels need mapping from the [0.0..1.0] to the [-1.0..1.0] range, = sampledValue * 2.0 - 1.0
                // occlusion texture R channel only, = 1.0 + strength * (sampledValue - 1.0)
                // emissive texture 8 bit sRGB. Needs decoding to linear in the shader.
                // clearcoat texture R channel only
                // clearcoatRoughness texture G channel only
                Texture2D baseColorTexture = null, metallicRoughnessTexture = null, normalTexture = null, occlusionTexture = null, emissiveTexture = null, clearcoatTexture = null, clearcoatRoughnessTexture = null, clearcoatNormalTexture = null;
                (TextureFilter, TextureAddressMode, TextureAddressMode) baseColorSamplerState = default, metallicRoughnessSamplerState = default, normalSamplerState = default, occlusionSamplerState = default, emissiveSamplerState = default, clearcoatSamplerState = default, clearcoatRoughnessSamplerState = default, clearcoatNormalSamplerState = default;

                KHR_materials_clearcoat clearcoat = null;
                if (material.Extensions?.TryGetValue("KHR_materials_clearcoat", out extension) ?? false)
                    clearcoat = Newtonsoft.Json.JsonConvert.DeserializeObject<KHR_materials_clearcoat>(extension.ToString());

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

                var baseColorFactor = material.PbrMetallicRoughness?.BaseColorFactor ?? new[] { 1f, 1f, 1f, 1f };
                var baseColorFactorVector = new Vector4(baseColorFactor[0], baseColorFactor[1], baseColorFactor[2], baseColorFactor[3]);
                var metallicFactor = material.PbrMetallicRoughness?.MetallicFactor ?? 1f;
                var roughtnessFactor = material.PbrMetallicRoughness?.RoughnessFactor ?? 1f;
                var normalScale = material.NormalTexture?.Scale ?? 0; // Must be 0 only if the textureInfo is missing, otherwise it must have the default value 1.
                var occlusionStrength = material.OcclusionTexture?.Strength ?? 0; // Must be 0 only if the textureInfo is missing, otherwise it must have the default value 1.
                var emissiveFactor = material.EmissiveFactor ?? new[] { 0f, 0f, 0f };
                var emissiveFactorVector = new Vector3(emissiveFactor[0], emissiveFactor[1], emissiveFactor[2]);
                var clearcoatFactor = clearcoat?.ClearcoatFactor ?? 0;
                var clearcoatRoughnessFactor = clearcoat?.ClearcoatRoughnessFactor ?? 0;
                var clearcoatNormalScale = clearcoat?.ClearcoatNormalTexture?.Scale ?? 1;

                switch (baseColorSamplerState.Item2)
                {
                    case TextureAddressMode.Wrap: options |= SceneryMaterialOptions.TextureAddressModeWrap; break;
                    case TextureAddressMode.Clamp: options |= SceneryMaterialOptions.TextureAddressModeClamp; break;
                    case TextureAddressMode.Mirror: options |= SceneryMaterialOptions.TextureAddressModeMirror; break;
                }

                var indexBufferSet = new GltfIndexBufferSet();
                ushort[] indexData = null;

                if (meshPrimitive.Indices != null)
                {
                    var accessor = gltfFile.Accessors[(int)meshPrimitive.Indices];
                    indexData = new ushort[accessor.Count];
                    var read = GetIntegerReader(accessor.ComponentType);
                    using (var br = new BinaryReader(distanceLevel.GetBufferView(accessor, out _)))
                    {
                        for (var i = 0; i < indexData.Length; i++)
                            indexData[i] = read(br);
                    }
                    if (accessor.Sparse != null)
                    {
                        var readI = GetIntegerReader(accessor.Sparse.Indices.ComponentType);
                        using (var bri = new BinaryReader(distanceLevel.GetBufferView(accessor.Sparse.Indices, out _)))
                        using (var br = new BinaryReader(distanceLevel.GetBufferView(accessor.Sparse.Values, out _)))
                        {
                            for (var i = 0; i < accessor.Sparse.Count; i++)
                                indexData[readI(bri)] = read(br);
                        }
                    }
                    indexBufferSet.IndexBuffer = new IndexBuffer(shape.Viewer.GraphicsDevice, typeof(ushort), indexData.Length, BufferUsage.None);
                    indexBufferSet.IndexBuffer.SetData(indexData);
                    indexBufferSet.IndexBuffer.Name = name;
                    options |= SceneryMaterialOptions.PbrHasIndices;
                }

                var vertexAttributes = new List<VertexBufferBinding>();

                VertexPosition[] vertexPositions = null;
                VertexNormal[] vertexNormals = null;
                VertexTextureDiffuse[] vertexTextureUvs = null;
                VertexBuffer vertexBufferTextureUvs = null;

                if (meshPrimitive.Attributes.TryGetValue("POSITION", out var accessorNumber))
                {
                    if (!shape.VertexBuffers.TryGetValue(accessorNumber, out var vertexBufferBinding) || meshPrimitive.Attributes.ContainsKey("COLOR_0"))
                    {
                        // The condition COLOR_0 is here to allow the mesh to go through the normalmap pipeline with calculating tangents, where the positions are needed anyways.
                        var accessor = gltfFile.Accessors[accessorNumber];
                        var componentSizeInBytes = distanceLevel.GetSizeInBytes(accessor);
                        vertexPositions = new VertexPosition[accessor.Count];
                        var read = shape.GetNormalizedReader(accessor.ComponentType);
                        using (var br = new BinaryReader(distanceLevel.GetBufferView(accessor, out var byteStride)))
                        {
                            var seek = byteStride != null ? (int)byteStride - componentSizeInBytes : 0;
                            for (var i = 0; i < vertexPositions.Length; i++)
                                vertexPositions[i] = new VertexPosition(distanceLevel.ReadVector3(read, br, accessor.Type, seek));
                        }
                        if (accessor.Sparse != null)
                        {
                            var readI = GetIntegerReader(accessor.Sparse.Indices.ComponentType);
                            using (var bri = new BinaryReader(distanceLevel.GetBufferView(accessor.Sparse.Indices, out _)))
                            using (var br = new BinaryReader(distanceLevel.GetBufferView(accessor.Sparse.Values, out var byteStride)))
                            {
                                var seek = byteStride != null ? (int)byteStride - componentSizeInBytes : 0;
                                for (var i = 0; i < accessor.Sparse.Count; i++)
                                    vertexPositions[readI(bri)] = new VertexPosition(distanceLevel.ReadVector3(read, br, accessor.Type, seek));
                            }
                        }
                        if (vertexBufferBinding.VertexBuffer == null)
                        {
                            var vertexBuffer = new VertexBuffer(shape.Viewer.GraphicsDevice, typeof(VertexPosition), vertexPositions.Length, BufferUsage.None);
                            vertexBuffer.SetData(vertexPositions);
                            vertexBuffer.Name = "POSITION";
                            vertexBufferBinding = new VertexBufferBinding(vertexBuffer);
                        }
                        if (!shape.VertexBuffers.ContainsKey(accessorNumber))
                            shape.VertexBuffers.Add(accessorNumber, vertexBufferBinding);
                        MinPosition = new Vector4(accessor.Min[0], accessor.Min[1], accessor.Min[2], 1);
                        MaxPosition = new Vector4(accessor.Max[0], accessor.Max[1], accessor.Max[2], 1);
                        HierarchyIndex = hierarchyIndex;
                    }
                    vertexAttributes.Add(vertexBufferBinding);
                }
                else
                {
                    throw new NotImplementedException("One of the glTF mesh primitives has no positions.");
                }

                if (meshPrimitive.Attributes.TryGetValue("NORMAL", out accessorNumber))
                {
                    if (!shape.VertexBuffers.TryGetValue(accessorNumber, out var vertexBufferBinding))
                    {
                        var accessor = gltfFile.Accessors[accessorNumber];
                        var componentSizeInBytes = distanceLevel.GetSizeInBytes(accessor);
                        vertexNormals = new VertexNormal[accessor.Count];
                        var read = shape.GetNormalizedReader(accessor.ComponentType);
                        using (var br = new BinaryReader(distanceLevel.GetBufferView(accessor, out var byteStride)))
                        {
                            var seek = byteStride != null ? (int)byteStride - componentSizeInBytes : 0;
                            for (var i = 0; i < vertexNormals.Length; i++)
                                vertexNormals[i] = new VertexNormal(distanceLevel.ReadVector3(read, br, accessor.Type, seek));
                        }
                        if (accessor.Sparse != null)
                        {
                            var readI = GetIntegerReader(accessor.Sparse.Indices.ComponentType);
                            using (var bri = new BinaryReader(distanceLevel.GetBufferView(accessor.Sparse.Indices, out _)))
                            using (var br = new BinaryReader(distanceLevel.GetBufferView(accessor.Sparse.Values, out var byteStride)))
                            {
                                var seek = byteStride != null ? (int)byteStride - componentSizeInBytes : 0;
                                for (var i = 0; i < accessor.Sparse.Count; i++)
                                    vertexNormals[readI(bri)] = new VertexNormal(distanceLevel.ReadVector3(read, br, accessor.Type, seek));
                            }
                        }
                        var vertexBuffer = new VertexBuffer(shape.Viewer.GraphicsDevice, typeof(VertexNormal), vertexNormals.Length, BufferUsage.None);
                        vertexBuffer.SetData(vertexNormals);
                        vertexBuffer.Name = "NORMAL";
                        vertexBufferBinding = new VertexBufferBinding(vertexBuffer);
                        shape.VertexBuffers.Add(accessorNumber, vertexBufferBinding);
                    }
                    vertexAttributes.Add(vertexBufferBinding);
                    options |= SceneryMaterialOptions.PbrHasNormals;
                }
                else
                {
                    vertexNormals = new VertexNormal[vertexAttributes.First().VertexBuffer.VertexCount];
                    vertexNormals.Initialize();
                    var vertexBuffer = new VertexBuffer(shape.Viewer.GraphicsDevice, typeof(VertexNormal), vertexNormals.Length, BufferUsage.None);
                    vertexBuffer.SetData(vertexNormals);
                    vertexBuffer.Name = "NORMAL_DUMMY";
                    vertexAttributes.Add(new VertexBufferBinding(vertexBuffer));
                }

                if (meshPrimitive.Attributes.TryGetValue("TEXCOORD_0", out accessorNumber))
                {
                    if (!shape.VertexBuffers.TryGetValue(accessorNumber, out var vertexBufferBinding))
                    {
                        var accessor = gltfFile.Accessors[accessorNumber];
                        var componentSizeInBytes = distanceLevel.GetSizeInBytes(accessor);
                        vertexTextureUvs = new VertexTextureDiffuse[accessor.Count];
                        var read = shape.GetNormalizedReader(accessor.ComponentType);
                        using (var br = new BinaryReader(distanceLevel.GetBufferView(accessor, out var byteStride)))
                        {
                            var seek = byteStride != null ? (int)byteStride - componentSizeInBytes : 0;
                            for (var i = 0; i < vertexTextureUvs.Length; i++)
                                vertexTextureUvs[i] = new VertexTextureDiffuse(distanceLevel.ReadVector2(read, br, accessor.Type, seek));
                        }
                        if (accessor.Sparse != null)
                        {
                            var readI = GetIntegerReader(accessor.Sparse.Indices.ComponentType);
                            using (var bri = new BinaryReader(distanceLevel.GetBufferView(accessor.Sparse.Indices, out _)))
                            using (var br = new BinaryReader(distanceLevel.GetBufferView(accessor.Sparse.Values, out var byteStride)))
                            {
                                var seek = byteStride != null ? (int)byteStride - componentSizeInBytes : 0;
                                for (var i = 0; i < accessor.Sparse.Count; i++)
                                    vertexTextureUvs[readI(bri)] = new VertexTextureDiffuse(distanceLevel.ReadVector2(read, br, accessor.Type, seek));
                            }
                        }
                        vertexBufferTextureUvs = new VertexBuffer(shape.Viewer.GraphicsDevice, typeof(VertexTextureDiffuse), vertexTextureUvs.Length, BufferUsage.None);
                        vertexBufferTextureUvs.SetData(vertexTextureUvs);
                        vertexBufferTextureUvs.Name = "TEXCOORD_0";
                        vertexBufferBinding = new VertexBufferBinding(vertexBufferTextureUvs);
                        shape.VertexBuffers.Add(accessorNumber, vertexBufferBinding);
                    }
                    vertexAttributes.Add(vertexBufferBinding);
                }
                else
                {
                    vertexTextureUvs = new VertexTextureDiffuse[vertexAttributes.First().VertexBuffer.VertexCount];
                    vertexTextureUvs.Initialize();
                    vertexBufferTextureUvs = new VertexBuffer(shape.Viewer.GraphicsDevice, typeof(VertexTextureDiffuse), vertexTextureUvs.Length, BufferUsage.None);
                    vertexBufferTextureUvs.SetData(vertexTextureUvs);
                    vertexBufferTextureUvs.Name = "TEXCOORD_DUMMY";
                    vertexAttributes.Add(new VertexBufferBinding(vertexBufferTextureUvs));
                }

                if (meshPrimitive.Attributes.TryGetValue("TANGENT", out accessorNumber))
                {
                    if (!shape.VertexBuffers.TryGetValue(accessorNumber, out var vertexBufferBinding))
                    {
                        var accessor = gltfFile.Accessors[accessorNumber];
                        var componentSizeInBytes = distanceLevel.GetSizeInBytes(accessor);
                        var vertexData = new VertexTangent[accessor.Count];
                        var read = shape.GetNormalizedReader(accessor.ComponentType);
                        using (var br = new BinaryReader(distanceLevel.GetBufferView(accessor, out var byteStride)))
                        {
                            var seek = byteStride != null ? (int)byteStride - componentSizeInBytes : 0;
                            for (var i = 0; i < vertexData.Length; i++)
                                vertexData[i] = new VertexTangent(distanceLevel.ReadVector4(read, br, accessor.Type, seek));
                        }
                        if (accessor.Sparse != null)
                        {
                            var readI = GetIntegerReader(accessor.Sparse.Indices.ComponentType);
                            using (var bri = new BinaryReader(distanceLevel.GetBufferView(accessor.Sparse.Indices, out _)))
                            using (var br = new BinaryReader(distanceLevel.GetBufferView(accessor.Sparse.Values, out var byteStride)))
                            {
                                var seek = byteStride != null ? (int)byteStride - componentSizeInBytes : 0;
                                for (var i = 0; i < accessor.Sparse.Count; i++)
                                    vertexData[readI(bri)] = new VertexTangent(distanceLevel.ReadVector4(read, br, accessor.Type, seek));
                            }
                        }
                        var vertexBuffer = new VertexBuffer(shape.Viewer.GraphicsDevice, typeof(VertexTangent), vertexData.Length, BufferUsage.None);
                        vertexBuffer.SetData(vertexData);
                        vertexBuffer.Name = "TANGENT";
                        vertexBufferBinding = new VertexBufferBinding(vertexBuffer);
                        shape.VertexBuffers.Add(accessorNumber, vertexBufferBinding);
                    }
                    vertexAttributes.Add(vertexBufferBinding);
                    options |= SceneryMaterialOptions.PbrHasTangents;
                }
                else if (vertexTextureUvs != null && normalScale != 0
                    || (options & SceneryMaterialOptions.PbrHasSkin) != 0 || meshPrimitive.Attributes.ContainsKey("COLOR_0"))
                {
                    // The condition of "COLOR_0" above is just because in the current state we can run the vertex colors through the VERTEX_INPUT_NORMALMAP pipeline.
                    // More vertex and shadowmap shader iterations would be needed to opt this out, but "COLOR_0" is assumed to be rare, so we go this way...
                    var vertexData = CalculateTangents(indexData, vertexPositions, vertexNormals, vertexTextureUvs);
                    var vertexBuffer = new VertexBuffer(shape.Viewer.GraphicsDevice, typeof(VertexTangent), vertexData.Length, BufferUsage.WriteOnly);
                    vertexBuffer.SetData(vertexData);
                    vertexBuffer.Name = "TANGENT_CALCULATED";
                    vertexAttributes.Add(new VertexBufferBinding(vertexBuffer));
                    options |= SceneryMaterialOptions.PbrHasTangents;
                }

                if (meshPrimitive.Attributes.TryGetValue("TEXCOORD_1", out accessorNumber) &&
                    (texCoords1.X == 1 || texCoords1.Y == 1 || texCoords1.Z == 1 || texCoords1.W == 1) && // To eliminate possible spare buffers (model problem actually)
                    (options & SceneryMaterialOptions.PbrHasTangents) != 0) // This is just because currently we can use the texcoords 1 only through the VERTEX_INPUT_NORMALMAP pipeline.
                {
                    if (!shape.VertexBuffers.TryGetValue(accessorNumber, out var vertexBufferBinding))
                    {
                        var accessor = gltfFile.Accessors[accessorNumber];
                        var componentSizeInBytes = distanceLevel.GetSizeInBytes(accessor);
                        var vertexData = new VertexTextureMetallic[accessor.Count];
                        var read = shape.GetNormalizedReader(accessor.ComponentType);
                        using (var br = new BinaryReader(distanceLevel.GetBufferView(accessor, out var byteStride)))
                        {
                            var seek = byteStride != null ? (int)byteStride - componentSizeInBytes : 0;
                            for (var i = 0; i < vertexData.Length; i++)
                                vertexData[i] = new VertexTextureMetallic(distanceLevel.ReadVector2(read, br, accessor.Type, seek));
                        }
                        if (accessor.Sparse != null)
                        {
                            var readI = GetIntegerReader(accessor.Sparse.Indices.ComponentType);
                            using (var bri = new BinaryReader(distanceLevel.GetBufferView(accessor.Sparse.Indices, out _)))
                            using (var br = new BinaryReader(distanceLevel.GetBufferView(accessor.Sparse.Values, out var byteStride)))
                            {
                                var seek = byteStride != null ? (int)byteStride - componentSizeInBytes : 0;
                                for (var i = 0; i < accessor.Sparse.Count; i++)
                                    vertexData[readI(bri)] = new VertexTextureMetallic(distanceLevel.ReadVector2(read, br, accessor.Type, seek));
                            }
                        }
                        var vertexBuffer = new VertexBuffer(shape.Viewer.GraphicsDevice, typeof(VertexTextureMetallic), vertexData.Length, BufferUsage.None);
                        vertexBuffer.SetData(vertexData);
                        vertexBuffer.Name = "TEXCOORD_1";
                        vertexBufferBinding = new VertexBufferBinding(vertexBuffer);
                        shape.VertexBuffers.Add(accessorNumber, vertexBufferBinding);
                    }
                    vertexAttributes.Add(vertexBufferBinding);
                }
                else if ((options & SceneryMaterialOptions.PbrHasTangents) != 0)
                {
                    // In the shader pipeline, where the tangents are defined, also needs to be a secondary texture coordinate
                    var vertexBufferBindings = vertexAttributes.Where(va => va.VertexBuffer.Name == "TEXCOORD_0");
                    if (vertexBufferBindings.Any())
                        vertexAttributes.Add(vertexBufferBindings.First());
                    else
                    {
                        var vertexBuffer = new VertexBuffer(shape.Viewer.GraphicsDevice, typeof(VertexTextureMetallic), vertexAttributes.First().VertexBuffer.VertexCount, BufferUsage.None);
                        vertexBuffer.Name = "TEXCOORD_1_DUMMY";
                        vertexAttributes.Add(new VertexBufferBinding(vertexBuffer));
                    }
                }

                if (meshPrimitive.Attributes.TryGetValue("JOINTS_0", out accessorNumber))
                {
                    if (!shape.VertexBuffers.TryGetValue(accessorNumber, out var vertexBufferBinding))
                    {
                        var accessor = gltfFile.Accessors[accessorNumber];
                        var componentSizeInBytes = distanceLevel.GetSizeInBytes(accessor);
                        var vertexData = new VertexJoint[accessor.Count];
                        var read = GetIntegerReader(accessor.ComponentType);
                        using (var br = new BinaryReader(distanceLevel.GetBufferView(accessor, out var byteStride)))
                        {
                            var seek = byteStride != null ? (int)byteStride - componentSizeInBytes : 0;
                            for (var i = 0; i < vertexData.Length; i++)
                            {
                                if (i > 0 && seek > 0) br.BaseStream.Seek(seek, SeekOrigin.Current);
                                vertexData[i] = new VertexJoint(new Vector4(read(br), read(br), read(br), read(br)));
                            }
                        }
                        if (accessor.Sparse != null)
                        {
                            var readI = GetIntegerReader(accessor.Sparse.Indices.ComponentType);
                            using (var bri = new BinaryReader(distanceLevel.GetBufferView(accessor.Sparse.Indices, out _)))
                            using (var br = new BinaryReader(distanceLevel.GetBufferView(accessor.Sparse.Values, out var byteStride)))
                            {
                                var seek = byteStride != null ? (int)byteStride - componentSizeInBytes : 0;
                                for (var i = 0; i < accessor.Sparse.Count; i++)
                                {
                                    if (i > 0 && seek > 0) br.BaseStream.Seek(seek, SeekOrigin.Current);
                                    vertexData[readI(bri)] = new VertexJoint(new Vector4(read(br), read(br), read(br), read(br)));
                                }
                            }
                        }
                        var vertexBuffer = new VertexBuffer(shape.Viewer.GraphicsDevice, typeof(VertexJoint), vertexData.Length, BufferUsage.None);
                        vertexBuffer.SetData(vertexData);
                        vertexBuffer.Name = "JOINTS_0";
                        vertexBufferBinding = new VertexBufferBinding(vertexBuffer);
                        shape.VertexBuffers.Add(accessorNumber, vertexBufferBinding);
                    }
                    vertexAttributes.Add(vertexBufferBinding);
                }

                if (meshPrimitive.Attributes.TryGetValue("WEIGHTS_0", out accessorNumber))
                {
                    if (!shape.VertexBuffers.TryGetValue(accessorNumber, out var vertexBufferBinding))
                    {
                        var accessor = gltfFile.Accessors[accessorNumber];
                        var componentSizeInBytes = distanceLevel.GetSizeInBytes(accessor);
                        var vertexData = new VertexWeight[accessor.Count];
                        var read = shape.GetNormalizedReader(accessor.ComponentType);
                        using (var br = new BinaryReader(distanceLevel.GetBufferView(accessor, out var byteStride)))
                        {
                            var seek = byteStride != null ? (int)byteStride - componentSizeInBytes : 0;
                            for (var i = 0; i < vertexData.Length; i++)
                                vertexData[i] = new VertexWeight(distanceLevel.ReadVector4(read, br, accessor.Type, seek));
                        }
                        if (accessor.Sparse != null)
                        {
                            var readI = GetIntegerReader(accessor.Sparse.Indices.ComponentType);
                            using (var bri = new BinaryReader(distanceLevel.GetBufferView(accessor.Sparse.Indices, out _)))
                            using (var br = new BinaryReader(distanceLevel.GetBufferView(accessor.Sparse.Values, out var byteStride)))
                            {
                                var seek = byteStride != null ? (int)byteStride - componentSizeInBytes : 0;
                                for (var i = 0; i < accessor.Sparse.Count; i++)
                                    vertexData[readI(bri)] = new VertexWeight(distanceLevel.ReadVector4(read, br, accessor.Type, seek));
                            }
                        }
                        var vertexBuffer = new VertexBuffer(shape.Viewer.GraphicsDevice, typeof(VertexWeight), vertexData.Length, BufferUsage.None);
                        vertexBuffer.SetData(vertexData);
                        vertexBuffer.Name = "WEIGHTS_0";
                        vertexBufferBinding = new VertexBufferBinding(vertexBuffer);
                        shape.VertexBuffers.Add(accessorNumber, vertexBufferBinding);
                    }
                    vertexAttributes.Add(vertexBufferBinding);
                }

                if (meshPrimitive.Attributes.TryGetValue("COLOR_0", out accessorNumber))
                {
                    if (!shape.VertexBuffers.TryGetValue(accessorNumber, out var vertexBufferBinding))
                    {
                        var accessor = gltfFile.Accessors[accessorNumber];
                        var componentSizeInBytes = distanceLevel.GetSizeInBytes(accessor);
                        var vertexData = new VertexColor4[accessor.Count];
                        var read = shape.GetNormalizedReader(accessor.ComponentType);
                        using (var br = new BinaryReader(distanceLevel.GetBufferView(accessor, out var byteStride)))
                        {
                            var seek = byteStride != null ? (int)byteStride - componentSizeInBytes : 0;
                            for (var i = 0; i < vertexData.Length; i++)
                                vertexData[i] = new VertexColor4(distanceLevel.ReadVector4(read, br, accessor.Type, seek));
                        }
                        if (accessor.Sparse != null)
                        {
                            var readI = GetIntegerReader(accessor.Sparse.Indices.ComponentType);
                            using (var bri = new BinaryReader(distanceLevel.GetBufferView(accessor.Sparse.Indices, out _)))
                            using (var br = new BinaryReader(distanceLevel.GetBufferView(accessor.Sparse.Values, out var byteStride)))
                            {
                                var seek = byteStride != null ? (int)byteStride - componentSizeInBytes : 0;
                                for (var i = 0; i < accessor.Sparse.Count; i++)
                                    vertexData[readI(bri)] = new VertexColor4(distanceLevel.ReadVector4(read, br, accessor.Type, seek));
                            }
                        }
                        var vertexBuffer = new VertexBuffer(shape.Viewer.GraphicsDevice, typeof(VertexColor4), vertexData.Length, BufferUsage.None);
                        vertexBuffer.SetData(vertexData);
                        vertexBuffer.Name = "COLOR_0";
                        vertexBufferBinding = new VertexBufferBinding(vertexBuffer);
                        shape.VertexBuffers.Add(accessorNumber, vertexBufferBinding);
                    }
                    vertexAttributes.Add(vertexBufferBinding);
                }
                else if ((options & (SceneryMaterialOptions.PbrHasTangents | SceneryMaterialOptions.PbrHasSkin)) != 0)
                {
                    // In the shader where the tangents are defined also needs to be a COLOR_0
                    var vertexData = Enumerable.Repeat(new VertexColor4(Vector4.One), vertexAttributes.First().VertexBuffer.VertexCount).ToArray();
                    var vertexBuffer = new VertexBuffer(shape.Viewer.GraphicsDevice, typeof(VertexColor4), vertexData.Length, BufferUsage.None);
                    vertexBuffer.SetData(vertexData);
                    vertexBuffer.Name = "COLOR_DUMMY";
                    vertexAttributes.Add(new VertexBufferBinding(vertexBuffer));
                }

                // This is the dummy instance buffer at the end of the vertex buffers
                vertexAttributes.Add(new VertexBufferBinding(RenderPrimitive.GetDummyVertexBuffer(shape.Viewer.GraphicsDevice)));

                var verticesDrawn = meshPrimitive.Indices == null ? vertexAttributes.First().VertexBuffer.VertexCount : indexData.Length;
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
                    baseColorTexture, baseColorFactorVector,
                    metallicRoughnessTexture, metallicFactor, roughtnessFactor,
                    normalTexture, normalScale,
                    occlusionTexture, occlusionStrength,
                    emissiveTexture, emissiveFactorVector,
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

                ShapePrimitives = new[] { new GltfPrimitive(sceneryMaterial, vertexAttributes, gltfFile, distanceLevel, indexBufferSet, skin, hierarchyIndex, hierarchy, texCoords1, texCoords2, texturePacking) };
                ShapePrimitives[0].SortIndex = 0;
            }
        }
        
        public class GltfPrimitive : ShapePrimitive
        {
            public readonly int[] Joints; // Replaces ShapePrimitive.HierarchyIndex for non-skinned primitives
            public readonly Matrix[] InverseBindMatrices;

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
                : this(new EmptyMaterial(distanceLevel.Viewer), Enumerable.Empty<VertexBufferBinding>().ToList(), gltfFile, distanceLevel, new GltfIndexBufferSet(), null, hierarchyIndex, hierarchy, Vector4.Zero, Vector4.Zero, 0)
            {
                Light = new ShapeLight
                {
                    Name = light.name,
                    Type = light.type,
                    Color = light.color != null && light.color.Length > 2 ? new Vector3(light.color[0], light.color[1], light.color[2]) : Vector3.Zero,
                    Intensity = light.intensity,
                    Range = light.range,
                };
                if (Light.Type == LightMode.Spot && light.spot != null)
                {
                    Light.InnerConeCos = (float)Math.Cos(light.spot.innerConeAngle);
                    Light.OuterConeCos = (float)Math.Cos(light.spot.outerConeAngle);
                }
            }

            public GltfPrimitive(Material material, List<VertexBufferBinding> vertexAttributes, Gltf gltfFile, GltfDistanceLevel distanceLevel, GltfIndexBufferSet indexBufferSet, Skin skin, int hierarchyIndex, int[] hierarchy, Vector4 texCoords1, Vector4 texCoords2, int texturePacking)
                : base(vertexAttributes.ToArray())
            {
                Material = material;
                IndexBuffer = indexBufferSet.IndexBuffer;
                PrimitiveCount = indexBufferSet.PrimitiveCount;
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
                    InverseBindMatrices = new[] { Matrix.Identity };
                }
                else
                {
                    // Skinned model
                    Joints = skin.Joints;

                    if (!distanceLevel.AllInverseBindMatrices.TryGetValue((int)skin.InverseBindMatrices, out InverseBindMatrices))
                    {
                        if (skin.InverseBindMatrices == null)
                        {
                            InverseBindMatrices = Enumerable.Repeat(Matrix.Identity, Joints.Length).ToArray();
                        }
                        else
                        {
                            var accessor = gltfFile.Accessors[(int)skin.InverseBindMatrices];
                            InverseBindMatrices = new Matrix[accessor.Count];
                            var read = distanceLevel.Shape.GetNormalizedReader(accessor.ComponentType);
                            using (var br = new BinaryReader(distanceLevel.GetBufferView(accessor, out _)))
                            {
                                for (var i = 0; i < InverseBindMatrices.Length; i++)
                                    InverseBindMatrices[i] = new Matrix(
                                        read(br), read(br), read(br), read(br),
                                        read(br), read(br), read(br), read(br),
                                        read(br), read(br), read(br), read(br),
                                        read(br), read(br), read(br), read(br));
                            }
                        }
                        distanceLevel.AllInverseBindMatrices.Add((int)skin.InverseBindMatrices, InverseBindMatrices);
                    }
                }
            }
        }
        
        public static VertexTangent[] CalculateTangents(ushort[] indices, VertexPosition[] vertexPosition, VertexNormal[] vertexNormal, VertexTextureDiffuse[] vertexTexture)
        {
            var vertexCount = vertexPosition.Length;

            var tan1 = new Vector3[vertexCount];
            var tan2 = new Vector3[vertexCount];

            var indicesLength = indices?.Length ?? vertexPosition.Length;

            for (var a = 0; a < indicesLength; a += 3)
            {
                var i1 = indices?[a + 0] ?? a + 0;
                var i2 = indices?[a + 1] ?? a + 1;
                var i3 = indices?[a + 2] ?? a + 2;

                var v1 = vertexPosition[i1].Position;
                var v2 = vertexPosition[i2].Position;
                var v3 = vertexPosition[i3].Position;

                var w1 = vertexTexture[i1].TextureCoordinate;
                var w2 = vertexTexture[i2].TextureCoordinate;
                var w3 = vertexTexture[i3].TextureCoordinate;

                // Need to invert the normal map Y coordinates to pass the test
                // https://github.com/KhronosGroup/glTF-Sample-Models/tree/master/2.0/NormalTangentTest
                w1.Y = -w1.Y;
                w2.Y = -w2.Y;
                w3.Y = -w3.Y;

                float x1 = v2.X - v1.X;
                float x2 = v3.X - v1.X;
                float y1 = v2.Y - v1.Y;
                float y2 = v3.Y - v1.Y;
                float z1 = v2.Z - v1.Z;
                float z2 = v3.Z - v1.Z;

                float s1 = w2.X - w1.X;
                float s2 = w3.X - w1.X;
                float t1 = w2.Y - w1.Y;
                float t2 = w3.Y - w1.Y;

                float div = s1 * t2 - s2 * t1;
                float r = div == 0.0f ? 0.0f : 1.0f / div; 

                Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                tan1[i1] += sdir;
                tan1[i2] += sdir;
                tan1[i3] += sdir;

                tan2[i1] += tdir;
                tan2[i2] += tdir;
                tan2[i3] += tdir;
            }

            var tangents = new VertexTangent[vertexCount];
            
            for (var a = 0; a < vertexCount; ++a)
            {
                Vector3 n = vertexNormal[a].Normal;
                Vector3 t = tan1[a];

                var tangentsW = Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f ? -1.0f : 1.0f;

                var tangent = Vector3.Normalize(t - n * Vector3.Dot(n, t));
                tangents[a] = new VertexTangent(new Vector4(tangent, tangentsW));
            }

            return tangents;
        }

        public struct GltfIndexBufferSet
        {
            public IndexBuffer IndexBuffer;
            public int PrimitiveCount;
            public PrimitiveType PrimitiveType;
        }
        
        public struct VertexPosition : IVertexType
        {
            Vector3 VertexData;
            public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(SizeInBytes,
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0));
            public VertexPosition(Vector3 data) { VertexData = data; }
            public Vector3 Position { get { return VertexData; } set { VertexData = value; } }
            VertexDeclaration IVertexType.VertexDeclaration { get { return VertexDeclaration; } }
            public const int SizeInBytes = sizeof(float) * 3;
        }
        
        public struct VertexNormal : IVertexType
        {
            Vector3 VertexData;
            public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(SizeInBytes,
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0));
            public VertexNormal(Vector3 data) { VertexData = data; }
            public Vector3 Normal { get { return VertexData; } set { VertexData = value; } }
            VertexDeclaration IVertexType.VertexDeclaration { get { return VertexDeclaration; } }
            public const int SizeInBytes = sizeof(float) * 3;
        }

        public struct VertexColor3 : IVertexType
        {
            Vector3 VertexData;
            public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(SizeInBytes,
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Color, 0));
            public VertexColor3(Vector3 data) { VertexData = data; }
            public Vector3 Color { get { return VertexData; } set { VertexData = value; } }
            VertexDeclaration IVertexType.VertexDeclaration { get { return VertexDeclaration; } }
            public const int SizeInBytes = sizeof(float) * 3;
        }

        public struct VertexColor4 : IVertexType
        {
            Vector4 VertexData;
            public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(SizeInBytes,
                new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.Color, 0));
            public VertexColor4(Vector4 data) { VertexData = data; }
            public Vector4 Color { get { return VertexData; } set { VertexData = value; } }
            VertexDeclaration IVertexType.VertexDeclaration { get { return VertexDeclaration; } }
            public const int SizeInBytes = sizeof(float) * 4;
        }

        public struct VertexJoint : IVertexType
        {
            Vector4 VertexData;
            public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(SizeInBytes,
                new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.BlendIndices, 0));
            public VertexJoint(Vector4 data) { VertexData = data; }
            public Vector4 Joint { get { return VertexData; } set { VertexData = value; } }
            VertexDeclaration IVertexType.VertexDeclaration { get { return VertexDeclaration; } }
            public const int SizeInBytes = sizeof(float) * 4;
        }

        public struct UIntVector4
        {
            public uint X;
            public uint Y;
            public uint Z;
            public uint W;

            public UIntVector4(uint x, uint y, uint z, uint w) => (X, Y, Z, W) = (x, y, z, w);
        }

        public struct VertexWeight : IVertexType
        {
            Vector4 VertexData;
            public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(SizeInBytes,
                new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0));
            public VertexWeight(Vector4 data) { VertexData = data; }
            public Vector4 Weight { get { return VertexData; } set { VertexData = value; } }
            VertexDeclaration IVertexType.VertexDeclaration { get { return VertexDeclaration; } }
            public const int SizeInBytes = sizeof(float) * 4;
        }

        public struct VertexTangent : IVertexType
        {
            Vector4 VertexData;
            public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(SizeInBytes,
                new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.Tangent, 0));
            public VertexTangent(Vector4 data) { VertexData = data; }
            public Vector4 Tangent { get { return VertexData; } set { VertexData = value; } }
            VertexDeclaration IVertexType.VertexDeclaration { get { return VertexDeclaration; } }
            public const int SizeInBytes = sizeof(float) * 4;
        }

        public struct VertexTextureDiffuse : IVertexType
        {
            Vector2 VertexData;
            public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(SizeInBytes,
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0));
            public VertexTextureDiffuse(Vector2 data) { VertexData = data; }
            public Vector2 TextureCoordinate { get { return VertexData; } set { VertexData = value; } }
            VertexDeclaration IVertexType.VertexDeclaration { get { return VertexDeclaration; } }
            public const int SizeInBytes = sizeof(float) * 2;
        }

        public struct VertexTextureMetallic : IVertexType
        {
            Vector2 VertexData;
            public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(SizeInBytes,
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1));
            public VertexTextureMetallic(Vector2 data) { VertexData = data; }
            public Vector2 TextureCoordinate { get { return VertexData; } set { VertexData = value; } }
            VertexDeclaration IVertexType.VertexDeclaration { get { return VertexDeclaration; } }
            public const int SizeInBytes = sizeof(float) * 2;
        }

        public struct VertexTextureNormalMap : IVertexType
        {
            Vector2 VertexData;
            public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(SizeInBytes,
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 5));
            public VertexTextureNormalMap(Vector2 data) { VertexData = data; }
            public Vector2 TextureCoordinate { get { return VertexData; } set { VertexData = value; } }
            VertexDeclaration IVertexType.VertexDeclaration { get { return VertexDeclaration; } }
            public const int SizeInBytes = sizeof(float) * 2;
        }

        public struct VertexTextureSpecularMap : IVertexType
        {
            Vector2 VertexData;
            public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(SizeInBytes,
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 6));
            public VertexTextureSpecularMap(Vector2 data) { VertexData = data; }
            public Vector2 TextureCoordinate { get { return VertexData; } set { VertexData = value; } }
            VertexDeclaration IVertexType.VertexDeclaration { get { return VertexDeclaration; } }
            public const int SizeInBytes = sizeof(float) * 2;
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
        public override int GetAnimationNamesCount() => EnableAnimations ? GltfAnimations?.Count ?? 0 : 0;

        public bool HasAnimation(int number) => GltfAnimations?.ElementAtOrDefault(number)?.Channels?.FirstOrDefault() != null;
        public float GetAnimationLength(int number) => GltfAnimations?.ElementAtOrDefault(number)?.Channels?.Select(c => c.TimeMax).Max() ?? 0;

        /// <summary>
        /// Calculate the animation matrices of a glTF animation.
        /// </summary>
        /// <param name="animationNumber">The number of the animation to advance.</param>
        /// <param name="time">Actual time in the animation clip in seconds.</param>
        public void Animate(int animationNumber, float time, Matrix[] animatedMatrices)
        {
            if (!EnableAnimations)
                return;

            foreach (var channel in GltfAnimations[animationNumber].Channels)
            {
                // Interpolating between two frames
                var frame1 = 0;
                for (var i = 0; i < channel.TimeArray.Length; i++)
                    if (channel.TimeArray[i] <= time)
                        frame1 = i;
                    else if (channel.TimeArray[i] > time)
                        break;

                var frame2 = Math.Min(channel.TimeArray.Length - 1, frame1 + 1);
                var time1 = channel.TimeArray[frame1];
                var time2 = channel.TimeArray[frame2];

                float amount;
                switch (channel.Interpolation)
                {
                    // See the formula for cubic spline: https://www.khronos.org/registry/glTF/specs/2.0/glTF-2.0.html#interpolation-cubic
                    case AnimationSampler.InterpolationEnum.CUBICSPLINE:
                    case AnimationSampler.InterpolationEnum.LINEAR: amount = time1 < time2 ? MathHelper.Clamp((time - time1) / (time2 - time1), 0, 1) : 0; break;
                    case AnimationSampler.InterpolationEnum.STEP: amount = 0; break;
                    default: amount = 0; break;
                }
                // Matrix.Decompose() gives wrong result, so must go with the individually stored transforms. It is guaranteed by the spec that the animation targeted nodes have these set.
                var scale = Scales[channel.TargetNode];
                var rotation = Rotations[channel.TargetNode];
                var translation = Translations[channel.TargetNode];
                switch (channel.Path)
                {
                    case AnimationChannelTarget.PathEnum.translation:
                        translation = channel.Interpolation == AnimationSampler.InterpolationEnum.CUBICSPLINE
                            ? Vector3.Hermite(channel.OutputVector3[Property(frame1)], channel.OutputVector3[OutTangent(frame2)], channel.OutputVector3[Property(frame2)], channel.OutputVector3[InTangent(frame2)], amount)
                            : Vector3.Lerp(channel.OutputVector3[frame1], channel.OutputVector3[frame2], amount);
                        break;
                    case AnimationChannelTarget.PathEnum.rotation:
                        rotation = channel.Interpolation == AnimationSampler.InterpolationEnum.CUBICSPLINE
                            ? CsInterp(channel.OutputQuaternion[Property(frame1)], channel.OutputQuaternion[OutTangent(frame2)], channel.OutputQuaternion[Property(frame2)], channel.OutputQuaternion[InTangent(frame2)], amount)
                            : Quaternion.Slerp(channel.OutputQuaternion[frame1], channel.OutputQuaternion[frame2], amount);
                        break;
                    case AnimationChannelTarget.PathEnum.scale:
                         scale = channel.Interpolation == AnimationSampler.InterpolationEnum.CUBICSPLINE
                            ? Vector3.Hermite(channel.OutputVector3[Property(frame1)], channel.OutputVector3[OutTangent(frame2)], channel.OutputVector3[Property(frame2)], channel.OutputVector3[InTangent(frame2)], amount)
                            : Vector3.Lerp(channel.OutputVector3[frame1], channel.OutputVector3[frame2], amount);
                        break;
                    case AnimationChannelTarget.PathEnum.weights:
                    default: break;
                }
                animatedMatrices[channel.TargetNode] = Matrix.CreateScale(scale) * Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(translation);
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
        /// It is used to store the adjustments needed for the Khronos sample models for being able to show them all at once in a single consist.
        /// For this to work 'git clone https://github.com/KhronosGroup/glTF-Sample-Models.git' to the MSTS/TRAINS/TRAINSET folder, so that the
        /// models will be available in e.g. MSTS/TRAINS/TRAINSET/glTF-Sample-Models/2.0/... folder. Then start like:
        /// RunActivity.exe -start -explorer "C:\Devel\MSTS\ROUTES\USA2\PATHS\tut6path.pat" "glTF-Sample-Models" 12:00 1 0
        /// </summary>
        static readonly Dictionary<string, Matrix> SampleModelsAdjustments = new Dictionary<string, Matrix>
        {
            { "2CylinderEngine".ToLower(), Matrix.CreateScale(0.01f) * Matrix.CreateTranslation(0, 2, 0) },
            { "ABeautifulGame".ToLower(), Matrix.CreateScale(10) },
            { "AnimatedCube".ToLower(), Matrix.CreateTranslation(0, 2, 0) },
            { "AnimatedMorphCube".ToLower(), Matrix.CreateTranslation(0, 2, 0) },
            { "AnimatedMorphSphere".ToLower(), Matrix.CreateTranslation(0, 2, 0) },
            { "AntiqueCamera".ToLower(), Matrix.CreateScale(0.5f) },
            { "AttenuationTest".ToLower(), Matrix.CreateScale(0.3f) * Matrix.CreateTranslation(0, 4, 0) },
            { "Avocado".ToLower(), Matrix.CreateScale(50) },
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
            { "ClearCoatTest".ToLower(), Matrix.CreateScale(0.5f) * Matrix.CreateTranslation(0, 3, 0) },
            { "Corset".ToLower(), Matrix.CreateScale(40) * Matrix.CreateTranslation(0, 1, 0) },
            { "Cube".ToLower(), Matrix.CreateTranslation(0, 2, 0) },
            { "DamagedHelmet".ToLower(), Matrix.CreateTranslation(0, 2, 0) },
            { "DragonAttenuation".ToLower(), Matrix.CreateTranslation(0, 2, 0) },
            { "EmissiveStrengthTest".ToLower(), Matrix.CreateScale(0.5f) * Matrix.CreateTranslation(0, 3, 0) },
            { "FlightHelmet".ToLower(), Matrix.CreateScale(5) },
            { "Fox".ToLower(), Matrix.CreateScale(0.02f) },
            { "GearboxAssy".ToLower(), Matrix.CreateScale(0.5f) * Matrix.CreateTranslation(80, -5, 0) },
            { "GlamVelvetSofa".ToLower(), Matrix.CreateScale(2) },
            { "InterpolationTest".ToLower(), Matrix.CreateScale(0.5f) * Matrix.CreateTranslation(0, 2, 0) },
            { "IridescenceDielectricSpheres".ToLower(), Matrix.CreateScale(0.2f) * Matrix.CreateTranslation(0, 4, 0) },
            { "IridescenceLamp".ToLower(), Matrix.CreateScale(5) },
            { "IridescenceMetallicSpheres".ToLower(), Matrix.CreateScale(0.2f) * Matrix.CreateTranslation(0, 4, 0) },
            { "IridescenceSuzanne".ToLower(), Matrix.CreateTranslation(0, 2, 0) },
            { "IridescentDishWithOlives".ToLower(), Matrix.CreateScale(10) },
            { "Lantern".ToLower(), Matrix.CreateScale(0.2f) },
            { "MaterialsVariantsShoe".ToLower(), Matrix.CreateScale(5) },
            { "MetalRoughSpheres".ToLower(), Matrix.CreateTranslation(0, 5, 0) },
            { "MetalRoughSpheresNoTextures".ToLower(), Matrix.CreateTranslation(0, 5, 0) },
            { "MorphPrimitivesTest".ToLower(), Matrix.CreateScale(2) * Matrix.CreateTranslation(0, 1, 0) },
            { "MosquitoInAmber".ToLower(), Matrix.CreateScale(25) * Matrix.CreateTranslation(0, 1, 0) },
            { "MultiUVTest".ToLower(), Matrix.CreateTranslation(0, 2, 0) },
            { "NormalTangentMirrorTest".ToLower(), Matrix.CreateScale(2) * Matrix.CreateTranslation(0, 2, 0) },
            { "NormalTangentTest".ToLower(), Matrix.CreateScale(2) * Matrix.CreateTranslation(0, 2, 0) },
            { "OrientationTest".ToLower(), Matrix.CreateScale(0.2f) * Matrix.CreateTranslation(0, 2, 0) },
            { "ReciprocatingSaw".ToLower(), Matrix.CreateScale(0.02f) * Matrix.CreateTranslation(0, 3, 0) },
            { "RecursiveSkeletons".ToLower(), Matrix.CreateScale(0.05f) },
            { "RiggedSimple".ToLower(), Matrix.CreateTranslation(0, 8, 0) },
            { "SciFiHelmet".ToLower(), Matrix.CreateTranslation(0, 2, 0) },
            { "SheenChair".ToLower(), Matrix.CreateScale(2) },
            { "SheenCloth".ToLower(), Matrix.CreateScale(20) },
            { "SpecGlossVsMetalRough".ToLower(), Matrix.CreateScale(5) },
            { "SpecularTest".ToLower(), Matrix.CreateScale(3) },
            { "StainedGlassLamp".ToLower(), Matrix.CreateScale(3) },
            { "Suzanne".ToLower(), Matrix.CreateTranslation(0, 2, 0) },
            { "TextureCoordinateTest".ToLower(), Matrix.CreateTranslation(0, 2, 0) },
            { "TextureEncodingTest".ToLower(), Matrix.CreateTranslation(0, 6, 0) },
        };
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
        public int TargetNode; // ref to index in hierarchy, e.g. Matrices(index)
        public AnimationChannelTarget.PathEnum Path; // e.g. rotation or tcb_rot, translation or linear_pos, scale, tcb_pos
        public AnimationSampler.InterpolationEnum Interpolation;
        public float[] TimeArray;
        public float TimeMin;
        public float TimeMax;
        public Vector3[] OutputVector3;
        public Quaternion[] OutputQuaternion;
        public float[] OutputWeights;
    }
}
