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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Viewer3D.Common;
using Orts.Viewer3D.Popups;
using Orts.Viewer3D.Processes;
using ORTS.Common;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Orts.Viewer3D
{
    [CallOnThread("Loader")]
    public class SharedTextureManager
    {
        const int SelectorDirectoryMaxDepth = 5;
        readonly HashSet<string> SelectorDirectoryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "Autumn",
            "AutumnSnow",
            "Snow",
            "Spring",
            "SpringSnow",
            "Winter",
            "WinterSnow",
        };

        readonly Viewer Viewer;
        readonly GraphicsDevice GraphicsDevice;
        Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();
        Dictionary<string, bool> TextureMarks = new Dictionary<string, bool>();

        [CallOnThread("Render")]
        internal SharedTextureManager(Viewer viewer, GraphicsDevice graphicsDevice)
        {
            Viewer = viewer;
            GraphicsDevice = graphicsDevice;
        }

        /// <summary>
        /// Loads a game texture file; DO NOT use with internal data, use <see cref="LoadInternal(GraphicsDevice, string)"/> instead.
        /// </summary>
        /// <returns>The <see cref="Texture2D"/> created from the given <paramref name="path"/> or a missing placeholder.</returns>
        public Texture2D Get(string path, bool required = false)
        {
            return Get(path, SharedMaterialManager.MissingTexture, required);
        }

        /// <summary>
        /// Loads a game texture file; DO NOT use with internal data, use <see cref="LoadInternal(GraphicsDevice, string)"/> instead.
        /// </summary>
        /// <returns>The <see cref="Texture2D"/> created from the given <paramref name="path"/> or a missing placeholder.</returns>
        public Texture2D Get(string path, Texture2D defaultTexture, bool required = false, bool srgb = false)
        {
            if (Thread.CurrentThread.Name != "Loader Process")
                Trace.TraceError("SharedTextureManager.Get incorrectly called by {0}; must be Loader Process or crashes will occur.", Thread.CurrentThread.Name);

            if (string.IsNullOrEmpty(path)) return defaultTexture;

            var textureKey = path.ToLowerInvariant();
            if (Textures.ContainsKey(textureKey)) return Textures[textureKey];

            // DO NOT add additional formats here without explicit approval
            // - DDS is used for newer, Open Rails-specific content or GLTF files
            // - ACE is used for older, MSTS-specific content
            // - JPEG and PNG is the only allowed format in the core GLTF specification
            switch (Path.GetExtension(textureKey))
            {
                case ".dds":
                case ".ace":
                    try
                    {
                        var depthPath = path;
                        for (var depth = 0; depth < SelectorDirectoryMaxDepth; depth++)
                        {
                            var dds = Path.ChangeExtension(depthPath, ".dds");
                            var ace = Path.ChangeExtension(depthPath, ".ace");
                            if (File.Exists(dds))
                            {
                                DDSLib.DDSFromFile(dds, GraphicsDevice, true, out Texture2D texture, srgb);
                                texture.Name = path;
                                return Textures[textureKey] = texture;
                            }
                            if (File.Exists(ace))
                            {
                                var texture = Formats.Msts.AceFile.Texture2DFromFile(GraphicsDevice, ace);
                                texture.Name = path;
                                return Textures[textureKey] = texture;
                            }
                            // When a texture is not found, and it is in a selector directory (e.g. "Snow"), we
                            // go up a level and try again. This repeats a fixed number of times, or until we run
                            // out of known selector directories.
                            var directory = Path.GetDirectoryName(depthPath);
                            if (string.IsNullOrEmpty(directory) || !SelectorDirectoryNames.Contains(Path.GetFileName(directory))) break;
                            depthPath = Path.Combine(Path.GetDirectoryName(directory), Path.GetFileName(depthPath));
                        }
                        if (required) Trace.TraceWarning("Ignored missing texture file: {0}", path);
                    }
                    catch (Exception error)
                    {
                        Trace.WriteLine(new FileLoadException(path, error));
                    }
                    return defaultTexture;
                case ".jpg":
                case ".jpeg":
                case ".png":
                    using (var stream = File.OpenRead(path))
                    {
                        var texture = srgb
                            ? GetSrgbTexture(GraphicsDevice, stream)
                            : Texture2D.FromStream(GraphicsDevice, stream);
                        texture.Name = path;
                        //return Textures[textureKey] = texture; // FIXME: loads a wrong texture for some glTF files.
                        return texture;
                    }
                default:
                    Trace.TraceWarning("Ignored unsupported texture file: {0}", path);
                    return defaultTexture;
            }
        }

        public Texture2D GetSrgbTexture(GraphicsDevice graphicsDevice, Stream stream)
        {
            using (var temp = Texture2D.FromStream(graphicsDevice, stream))
            {
                var srgbTex = new Texture2D(graphicsDevice, temp.Width, temp.Height, false, SurfaceFormat.ColorSRgb);
                var data = new Color[temp.Width * temp.Height];
                temp.GetData(data);
                srgbTex.SetData(data);

                return srgbTex;
            }
        }

        // Internal callers expect a new `Texture2D` for every load so we must also provide a new missing texture for each.
        internal static Texture2D GetInternalMissingTexture(GraphicsDevice graphicsDevice)
        {
            var texture = new Texture2D(graphicsDevice, 1, 1);
            texture.SetData(new[] { Color.Magenta });
            return texture;
        }

        /// <summary>
        /// Loads an internal texture file; DO NOT use with game data, use <see cref="Get(string, bool)"/> instead.
        /// </summary>
        /// <returns>The <see cref="Texture2D"/> created from the given <paramref name="path"/> or a missing placeholder.</returns>
        public static Texture2D LoadInternal(GraphicsDevice graphicsDevice, string path)
        {
            if (!File.Exists(path))
            {
                Trace.TraceError("Missing internal file: {0}", path);
                return GetInternalMissingTexture(graphicsDevice);
            }

            // DO NOT add additional formats here without explicit approval
            // - BMP is used for ETCS/DMI
            // - PNG is used for everything else
            switch (Path.GetExtension(path))
            {
                case ".bmp":
                case ".png":
                    return Texture2D.FromFile(graphicsDevice, path);
                default:
                    Trace.TraceError("Unsupported internal file: {0}", path);
                    return GetInternalMissingTexture(graphicsDevice);
            }
        }

        /// <summary>
        /// Loads an internal texture file; DO NOT use with game data, use <see cref="Get(string, bool)"/> instead.
        /// </summary>
        /// <returns>The <see cref="Texture2D"/> created from the given <paramref name="path"/> or a missing placeholder.</returns>
        public static Texture2D LoadInternal(GraphicsDevice graphicsDevice, string path, Microsoft.Xna.Framework.Rectangle MapRectangle)
        {
            if (string.IsNullOrEmpty(path)) return SharedMaterialManager.MissingTexture;

            path = path.ToLowerInvariant();
            var ext = Path.GetExtension(path);

            using (var stream = File.OpenRead(path))
            {
                if (ext == ".bmp" || ext == ".png")
                {
                    using (var image = System.Drawing.Image.FromStream(stream))
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            var mapRectangle = new System.Drawing.Rectangle
                            {
                                Height = MapRectangle.Height,
                                Width = MapRectangle.Width,
                                X = MapRectangle.X,
                                Y = MapRectangle.Y
                            };
                            var imageRect = new Bitmap(image).Clone(mapRectangle, image.PixelFormat);
                            imageRect.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                            memoryStream.Seek(0, SeekOrigin.Begin);
                            return Texture2D.FromStream(graphicsDevice, memoryStream);
                        }
                    }
                }
                else
                {
                    Trace.TraceWarning("Unsupported texture format: {0}", path);
                    return SharedMaterialManager.MissingTexture;
                }
            }
        }

        public void Mark()
        {
            TextureMarks.Clear();
            foreach (var path in Textures.Keys)
                TextureMarks.Add(path, false);
        }

        public void Mark(Texture2D texture)
        {
            foreach (var key in Textures.Keys)
            {
                if (Textures[key] == texture)
                {
                    TextureMarks[key] = true;
                    break;
                }
            }
        }

        public void Sweep()
        {
            foreach (var path in TextureMarks.Where(kvp => !kvp.Value).Select(kvp => kvp.Key))
            {
                Textures[path].Dispose();
                Textures.Remove(path);
            }
        }

        [CallOnThread("Updater")]
        public string GetStatus()
        {
            return Viewer.Catalog.GetPluralStringFmt("{0:F0} texture", "{0:F0} textures", Textures.Keys.Count);
        }
    }

    [CallOnThread("Loader")]
    public class SharedMaterialManager
    {
        readonly Viewer Viewer;
        IDictionary<(string, string, int, float, Effect), Material> Materials = new Dictionary<(string, string, int, float, Effect), Material>();
        IDictionary<(string, string, int, float, Effect), bool> MaterialMarks = new Dictionary<(string, string, int, float, Effect), bool>();

        public readonly LightConeShader LightConeShader;
        public readonly LightGlowShader LightGlowShader;
        public readonly ParticleEmitterShader ParticleEmitterShader;
        public readonly PopupWindowShader PopupWindowShader;
        public readonly PrecipitationShader PrecipitationShader;
        public readonly SceneryShader SceneryShader;
        public readonly ShadowMapShader ShadowMapShader;
        public readonly SkyShader SkyShader;
        public readonly DebugShader DebugShader;
        public readonly CabShader CabShader;

        public static Texture2D MissingTexture;
        public static TextureCube BlackCubeTexture;
        public static Texture2D DefaultSnowTexture;
        public static Texture2D DefaultDMSnowTexture;
        public static Texture2D WhiteTexture;
        public static Texture2D BlackTexture;

        [CallOnThread("Render")]
        public SharedMaterialManager(Viewer viewer)
        {
            Viewer = viewer;
            // TODO: Move to Loader process.
            LightConeShader = new LightConeShader(viewer.RenderProcess.GraphicsDevice);
            LightGlowShader = new LightGlowShader(viewer.RenderProcess.GraphicsDevice);
            ParticleEmitterShader = new ParticleEmitterShader(viewer.RenderProcess.GraphicsDevice);
            PopupWindowShader = new PopupWindowShader(viewer, viewer.RenderProcess.GraphicsDevice);
            PrecipitationShader = new PrecipitationShader(viewer.RenderProcess.GraphicsDevice);
            SceneryShader = new SceneryShader(viewer.RenderProcess.GraphicsDevice);
            var microtexPath = viewer.Simulator.RoutePath + @"\TERRTEX\microtex";
            try
            {
                if (File.Exists(microtexPath + ".dds"))
                {
                    DDSLib.DDSFromFile(microtexPath + ".dds", viewer.GraphicsDevice, true, out Texture2D microtex, false);
                    SceneryShader.OverlayTexture = microtex;
                }
                else if (File.Exists(microtexPath + ".ace"))
                {
                    SceneryShader.OverlayTexture = Formats.Msts.AceFile.Texture2DFromFile(viewer.GraphicsDevice, microtexPath + ".ace");
                }
            }
            catch (InvalidDataException error)
            {
                Trace.TraceWarning("Skipped texture with error: {1} in {0}", microtexPath, error.Message);
            }
            catch (Exception error)
            {
                Trace.WriteLine(new FileLoadException(microtexPath, error));
            }
            ShadowMapShader = new ShadowMapShader(viewer.RenderProcess.GraphicsDevice);
            SkyShader = new SkyShader(viewer.RenderProcess.GraphicsDevice);
            DebugShader = new DebugShader(viewer.RenderProcess.GraphicsDevice);
            CabShader = new CabShader(viewer.RenderProcess.GraphicsDevice, Vector4.One, Vector4.One, Vector3.One, Vector3.One);

            MissingTexture = SharedTextureManager.GetInternalMissingTexture(viewer.RenderProcess.GraphicsDevice);
            BlackCubeTexture = new TextureCube(Viewer.GraphicsDevice, 2, false, SurfaceFormat.Color);
            for (var i = 0; i < 6; i++)
                    BlackCubeTexture.SetData((CubeMapFace)i, Enumerable.Repeat(Color.Black, 2 * 2).ToArray());

            // Managing default snow textures
            var defaultSnowTexturePath = viewer.Simulator.RoutePath + @"\TERRTEX\SNOW\ORTSDefaultSnow.ace";
            DefaultSnowTexture = Viewer.TextureManager.Get(defaultSnowTexturePath);
            var defaultDMSnowTexturePath = viewer.Simulator.RoutePath + @"\TERRTEX\SNOW\ORTSDefaultDMSnow.ace";
            DefaultDMSnowTexture = Viewer.TextureManager.Get(defaultDMSnowTexturePath);

            WhiteTexture = new Texture2D(viewer.RenderProcess.GraphicsDevice, 1, 1);
            WhiteTexture.SetData(new[] { Color.White });
            WhiteTexture.Name = nameof(WhiteTexture);

            BlackTexture = new Texture2D(viewer.RenderProcess.GraphicsDevice, 1, 1);
            BlackTexture.SetData(new[] { Color.Black });
            BlackTexture.Name = nameof(BlackTexture);
        }

        public Material Load(string materialName, string textureName = null, int options = 0, float mipMapBias = 0f, Effect effect = null)
        {
            var materialKey = (materialName, textureName?.ToLowerInvariant(), options, mipMapBias, effect);
            if (!Materials.ContainsKey(materialKey))
            {
                switch (materialName)
                {
                    case "Debug":
                        Materials[materialKey] = new HUDGraphMaterial(Viewer);
                        break;
                    case "DebugNormals":
                        Materials[materialKey] = new DebugNormalMaterial(Viewer);
                        break;
                    case "Forest":
                        Materials[materialKey] = new ForestMaterial(Viewer, textureName);
                        break;
                    case "Label3D":
                        Materials[materialKey] = new Label3DMaterial(Viewer);
                        break;
                    case "LightCone":
                        Materials[materialKey] = new LightConeMaterial(Viewer);
                        break;
                    case "LightGlow":
                        Materials[materialKey] = new LightGlowMaterial(Viewer, textureName);
                        break;
                    case "PopupWindow":
                        Materials[materialKey] = new PopupWindowMaterial(Viewer);
                        break;
                    case "ParticleEmitter":
                        Materials[materialKey] = new ParticleEmitterMaterial(Viewer, textureName);
                        break;
                    case "Precipitation":
                        Materials[materialKey] = new PrecipitationMaterial(Viewer);
                        break;
                    case "Scenery":
                        Materials[materialKey] = new SceneryMaterial(Viewer, textureName, (SceneryMaterialOptions)options, mipMapBias);
                        break;
                    case "ShadowMap":
                        Materials[materialKey] = new ShadowMapMaterial(Viewer);
                        break;
                    case "SignalLight":
                        Materials[materialKey] = new SignalLightMaterial(Viewer, textureName);
                        break;
                    case "SignalLightGlow":
                        Materials[materialKey] = new SignalLightGlowMaterial(Viewer);
                        break;
                    case "Sky":
                        Materials[materialKey] = new SkyMaterial(Viewer);
                        break;
                    case "MSTSSky":
                        Materials[materialKey] = new MSTSSkyMaterial(Viewer);
                        break;
                    case "SpriteBatch":
                        Materials[materialKey] = new SpriteBatchMaterial(Viewer, effect: effect);
                        break;
                    case "Terrain":
                        Materials[materialKey] = new TerrainMaterial(Viewer, textureName, SharedMaterialManager.MissingTexture);
                        break;
                    case "TerrainShared":
                        Materials[materialKey] = new TerrainSharedMaterial(Viewer, textureName);
                        break;
                    case "TerrainSharedDistantMountain":
                        Materials[materialKey] = new TerrainSharedDistantMountain(Viewer, textureName);
                        break;
                    case "Transfer":
                        Materials[materialKey] = new TransferMaterial(Viewer, textureName);
                        break;
                    case "Water":
                        Materials[materialKey] = new WaterMaterial(Viewer, textureName);
                        break;
                    case "Screen":
                        Materials[materialKey] = new ScreenMaterial(Viewer, textureName, options);
                        break;
                    default:
                        Trace.TraceInformation("Skipped unknown material type {0}", materialName);
                        Materials[materialKey] = new YellowMaterial(Viewer);
                        break;
                }
            }
            return Materials[materialKey];
        }

        /// <summary>
        /// This method is used to initialize a metallic-roughness material from a glTF 2.0 source. 
        /// </summary>
        public Material Load(string materialName, string materialUniqueId, int options, float mipMapBias,
            Texture2D baseColorTexture, Vector4 baseColorFactor,
            Texture2D metallicRoughnessTexture, float metallicFactor, float roughnessFactor,
            Texture2D normalTexture, float normalScale,
            Texture2D occlusionTexture, float occlusionStrength,
            Texture2D emissiveTexture, Vector3 emissiveFactor,
            Texture2D clearcoatTexture, float clearcoatFactor,
            Texture2D clearcoatRoughnessTexture, float clearcoatRoughnessFactor,
            Texture2D clearcoatNormalTexture, float clearcoatNormalScale,
            Texture2D specularTexture, float specularFactor,
            Texture2D specularColorTexture, Vector3 specularColorFactor, float ior,
            float referenceAlpha, bool doubleSided,
            Vector4 texCoords1, Vector4 texCoords2, Vector4 texCoords3,
            (TextureFilter, TextureAddressMode, TextureAddressMode) samplerStateBaseColor,
            (TextureFilter, TextureAddressMode, TextureAddressMode) samplerStateMetallicRoughness,
            (TextureFilter, TextureAddressMode, TextureAddressMode) samplerStateNormal,
            (TextureFilter, TextureAddressMode, TextureAddressMode) samplerStateOcclusion,
            (TextureFilter, TextureAddressMode, TextureAddressMode) samplerStateEmissive,
            (TextureFilter, TextureAddressMode, TextureAddressMode) samplerStateClearcoat,
            (TextureFilter, TextureAddressMode, TextureAddressMode) samplerStateClearcoatRoughness,
            (TextureFilter, TextureAddressMode, TextureAddressMode) samplerStateClearcoatNormal,
            (TextureFilter, TextureAddressMode, TextureAddressMode) samplerStateSpecular,
            (TextureFilter, TextureAddressMode, TextureAddressMode) samplerStateSpecularColor)
        {
            var materialKey = (materialName, materialUniqueId?.ToLower(), options, mipMapBias, (Effect)null);

            if (!Materials.ContainsKey(materialKey))
            {
                switch (materialName)
                {
                    case "PBR":
                        Materials[materialKey] = new PbrMaterial(Viewer, materialUniqueId?.ToLower(), (SceneryMaterialOptions)options, mipMapBias,
                            baseColorTexture, baseColorFactor,
                            metallicRoughnessTexture, metallicFactor, roughnessFactor,
                            normalTexture, normalScale,
                            occlusionTexture, occlusionStrength,
                            emissiveTexture, emissiveFactor,
                            clearcoatTexture, clearcoatFactor,
                            clearcoatRoughnessTexture, clearcoatRoughnessFactor,
                            clearcoatNormalTexture, clearcoatNormalScale,
                            specularTexture, specularFactor,
                            specularColorTexture, specularColorFactor, ior,
                            referenceAlpha, doubleSided,
                            texCoords1, texCoords2, texCoords3,
                            samplerStateBaseColor,
                            samplerStateMetallicRoughness,
                            samplerStateNormal,
                            samplerStateOcclusion,
                            samplerStateEmissive,
                            samplerStateClearcoat,
                            samplerStateClearcoatRoughness,
                            samplerStateClearcoatNormal,
                            samplerStateSpecular,
                            samplerStateSpecularColor);
                        break;
                    default:
                        Trace.TraceInformation("Skipped unknown material type {0}", materialName);
                        Materials[materialKey] = new YellowMaterial(Viewer);
                        break;
                }
            }
            return Materials[materialKey];
        }

        public bool LoadNightTextures()
        {
            int count = 0;
            foreach (SceneryMaterial material in from material in Materials.Values
                                                 where material is SceneryMaterial
                                                 select material)
            {
                if (material.LoadNightTexture())
                    count++;
                if (count >= 20)
                {
                    count = 0;
                    // retest if there is enough free memory left;
                    var remainingMemorySpace = Viewer.LoadMemoryThreshold - Viewer.Game.HostProcess.CPUMemoryWorkingSet;
                    if (remainingMemorySpace < 0)
                    {
                        return false; // too bad, no more space, other night textures won't be loaded
                    }
                }
            }
            return true;
        }

        public bool LoadDayTextures()
        {
            int count = 0;
            foreach (SceneryMaterial material in from material in Materials.Values
                                                 where material is SceneryMaterial
                                                 select material)
            {
                if (material.LoadDayTexture())
                    count++;
                if (count >= 20)
                {
                    count = 0;
                    // retest if there is enough free memory left;
                    var remainingMemorySpace = Viewer.LoadMemoryThreshold - Viewer.Game.HostProcess.CPUMemoryWorkingSet;
                    if (remainingMemorySpace < 0)
                    {
                        return false; // too bad, no more space, other night textures won't be loaded
                    }
                }
            }
            return true;
        }

        public void Mark()
        {
            MaterialMarks.Clear();
            foreach (var path in Materials.Keys)
                MaterialMarks.Add(path, false);
        }

        public void Mark(Material material)
        {
            foreach (var key in Materials.Keys)
            {
                if (Materials[key] == material)
                {
                    MaterialMarks[key] = true;
                    break;
                }
            }
        }

        public void Sweep()
        {
            foreach (var path in MaterialMarks.Where(kvp => !kvp.Value).Select(kvp => kvp.Key))
                Materials.Remove(path);
        }

        public void LoadPrep()
        {
            if (Viewer.Settings.UseMSTSEnv == false)
            {
                Viewer.World.Sky.LoadPrep();
                sunDirection = Viewer.World.Sky.SolarDirection;
            }
            else
            {
                Viewer.World.MSTSSky.LoadPrep();
                sunDirection = Viewer.World.MSTSSky.mstsskysolarDirection;
            }
        }


        [CallOnThread("Updater")]
        public string GetStatus()
        {
            return Viewer.Catalog.GetPluralStringFmt("{0:F0} material", "{0:F0} materials", Materials.Keys.Count);
        }

        public static Color FogColor = new Color(110, 110, 110, 255);

        internal Vector3 sunDirection;

        internal void UpdateShaders()
        {
            if (Viewer.Settings.UseMSTSEnv == false)
                sunDirection = Viewer.World.Sky.SolarDirection;
            else
                sunDirection = Viewer.World.MSTSSky.mstsskysolarDirection;

            SceneryShader.SetLightVector_ZFar(sunDirection, Viewer.Settings.ViewingDistance);
            if (sunDirection.Y > -0.05)
            {
                SceneryShader.EnvironmentMapSpecularTexture = GltfShape.EnvironmentMapSpecularDay;
                SceneryShader.EnvironmentMapDiffuseTexture = GltfShape.EnvironmentMapDiffuseDay;
                SceneryShader.BrdfLutTexture = GltfShape.BrdfLutTexture;
            }
            else
            {
                SceneryShader.EnvironmentMapSpecularTexture = BlackTexture;
                SceneryShader.EnvironmentMapDiffuseTexture = BlackCubeTexture;
                SceneryShader.BrdfLutTexture = BlackTexture;
            }

            if (Viewer.Settings.UseMSTSEnv == false)
            {
                SceneryShader.Overcast = Viewer.Simulator.Weather.CloudCoverFactor;
                SceneryShader.SetFog(Viewer.Simulator.Weather.VisibilityM, ref SharedMaterialManager.FogColor);
                ParticleEmitterShader.SetFog(Viewer.Simulator.Weather.VisibilityM, ref SharedMaterialManager.FogColor);
                SceneryShader.ViewerPos = Viewer.Camera.XnaLocation(Viewer.Camera.CameraWorldLocation);
            }
            else
            {
                SceneryShader.Overcast = Viewer.World.MSTSSky.mstsskyovercastFactor;
                SceneryShader.SetFog(Viewer.World.MSTSSky.mstsskyfogDistance, ref SharedMaterialManager.FogColor);
                ParticleEmitterShader.SetFog(Viewer.Simulator.Weather.VisibilityM, ref SharedMaterialManager.FogColor);
                SceneryShader.ViewerPos = Viewer.Camera.XnaLocation(Viewer.Camera.CameraWorldLocation);
            }
        }
    }

    public abstract class Material
    {
        public readonly Viewer Viewer;
        public readonly string Key;


        protected Material(Viewer viewer, string key)
        {
            Viewer = viewer;
            Key = key;
            SetSortingMaterialId(this);
        }

        public override string ToString()
        {
            if (String.IsNullOrEmpty(Key))
                return GetType().Name;
            return String.Format("{0}({1})", GetType().Name, Key);
        }

        public virtual void SetState(GraphicsDevice graphicsDevice, Material previousMaterial) { }
        public virtual void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix) { }
        public virtual void ResetState(GraphicsDevice graphicsDevice) { }

        public virtual bool GetBlending() { return false; }
        public virtual Texture2D GetShadowTexture() { return null; }
        public virtual SamplerState GetShadowTextureAddressMode() { return SamplerState.LinearWrap; }
        public int KeyLengthRemainder() //used as a "pseudorandom" number
        {
            if (String.IsNullOrEmpty(Key))
                return 0;
            return Key.Length % 10;
        }

        [CallOnThread("Loader")]
        public virtual void Mark()
        {
            Viewer.MaterialManager.Mark(this);
        }

        /// <summary>
        /// [63 - 56] Shader/Effect ID (8 bit = 256 states)
        /// [55 - 52] RasterizerState ID Culling or Wireframe (4 bit = 16 states)
        /// [51 - 48] BlendState ID (4 bit = 16 states)
        /// [47 - 40] DepthStencilState ID (8 bit = 256 states)
        /// [39 - 32] SamplerState ID (8 bit)
        /// [31 - 16] Texture ID (16 bit)
        /// [15 - 00] Material ID (16 bit)
        /// </summary>
        public ulong SortingKey { get; private set; }

        protected void SetSortingEffectId(EffectTechnique technique)
        {
            var id = RenderSortHelper.GetEffectId(technique);
            SortingKey &= ~(0xFFUL << 56);
            SortingKey |= ((ulong)id << 56);
        }

        protected void SetSortingRasterizerStateId(RasterizerState state)
        {
            var id = RenderSortHelper.GetRasterizerId(state);
            SortingKey &= ~(0xFUL << 52);
            SortingKey |= ((ulong)(id & 0xF) << 52);
        }

        protected void SetSortingBlendStateId(BlendState state)
        {
            var id = RenderSortHelper.GetBlendId(state);
            SortingKey &= ~(0xFUL << 48);
            SortingKey |= ((ulong)(id & 0xF) << 48);
        }

        protected void SetSortingDepthStencilStateId(DepthStencilState state)
        {
            var id = RenderSortHelper.GetDepthStencilId(state);
            SortingKey &= ~(0xFFUL << 40);
            SortingKey |= ((ulong)(id & 0xFF) << 40);
        }

        protected void SetSortingSamplerStateId(SamplerState state)
        {
            var id = RenderSortHelper.GetSamplerId(state);
            SortingKey &= ~(0xFFUL << 32);
            SortingKey |= ((ulong)(id & 0xFF) << 32);
        }

        protected void SetSortingTextureId(string texture)
        {
            var id = RenderSortHelper.GetTextureId(texture);
            SortingKey &= ~(0xFFFFUL << 16);
            SortingKey |= ((ulong)(id & 0xFFFF) << 16);
        }

        protected void SetSortingMaterialId(Material material)
        {
            var id = RenderSortHelper.GetMaterialId(material);
            SortingKey &= ~0xFFFFUL;
            SortingKey |= (ulong)id & 0xFFFF;
        }
    }

    public class EmptyMaterial : Material
    {
        public EmptyMaterial(Viewer viewer)
            : base(viewer, null)
        {
        }
    }

    public class BasicMaterial : Material
    {
        public BasicMaterial(Viewer viewer, string key)
            : base(viewer, key)
        {
            SetSortingTextureId("Basic");
            SetSortingBlendStateId(BlendState.Opaque);
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            foreach (var item in renderItems)
                item.RenderPrimitive.Draw(graphicsDevice);
        }
    }

    public class BasicBlendedMaterial : BasicMaterial
    {
        public BasicBlendedMaterial(Viewer viewer, string key)
            : base(viewer, key)
        {
            SetSortingTextureId("BasicBlended");
            SetSortingBlendStateId(BlendState.NonPremultiplied);
        }

        public override bool GetBlending()
        {
            return true;
        }
    }

    public class SpriteBatchMaterial : BasicBlendedMaterial
    {
        public readonly SpriteBatch SpriteBatch;

        readonly BlendState BlendState = BlendState.NonPremultiplied;
        readonly Effect Effect;

        public SpriteBatchMaterial(Viewer viewer, Effect effect = null)
            : base(viewer, null)
        {
            SpriteBatch = new SpriteBatch(Viewer.RenderProcess.GraphicsDevice);
            Effect = effect;
            SetSortingEffectId(Effect?.CurrentTechnique);
            SetSortingBlendStateId(BlendState);
        }

        public SpriteBatchMaterial(Viewer viewer, BlendState blendState, Effect effect = null)
            : this(viewer, effect: effect)
        {
            BlendState = blendState;
            SetSortingBlendStateId(BlendState);
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState, effect: Effect);
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            SpriteBatch.End();

            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
        }
    }

    [Flags]
    public enum SceneryMaterialOptions
    {
        None = 0,
        // Diffuse
        Diffuse = 0x1,
        // Alpha test
        AlphaTest = 0x2,
        // Blending
        AlphaBlendingNone = 0x0,
        AlphaBlendingBlend = 0x4,
        AlphaBlendingAdd = 0x8,
        AlphaBlendingMask = 0xC,
        // Shader
        ShaderImage = 0x00,
        ShaderDarkShade = 0x10,
        ShaderHalfBright = 0x20,
        ShaderFullBright = 0x30,
        ShaderVegetation = 0x40,
        ShaderMask = 0x70,
        // Lighting
        Specular0 = 0x000,
        Specular25 = 0x080,
        Specular750 = 0x100,
        SpecularMask = 0x180,
        // Texture address mode
        TextureAddressModeWrap = 0x000,
        TextureAddressModeMirror = 0x200,
        TextureAddressModeClamp = 0x400,
        TextureAddressModeBorder = 0x600,
        TextureAddressModeMask = 0x600,
        // Night texture
        NightTexture = 0x800,

        PbrHasIndices = 0x01000,
        PbrHasNormals = 0x02000,
        PbrHasTangents = 0x04000,
        PbrHasSkin = 0x08000,
        PbrCullClockWise = 0x10000,
        PbrHasTexCoord1 = 0x20000,
        PbrHasMorphTargets = 0x40000,

        // Texture to be shown in tunnels and underground (used for 3D cab night textures)
        UndergroundTexture = 0x40000000,
    }

    public class SceneryMaterial : Material
    {
        public readonly SceneryMaterialOptions Options;
        readonly float MipMapBias;
        protected Texture2D Texture;
        private readonly string TexturePath;
        protected Texture2D NightTexture;
        byte AceAlphaBits;   // the number of bits in the ace file's alpha channel 
        readonly float LightingSpecular;
        protected float LightingDiffuse;

        protected RasterizerState RasterizerState;
        protected BlendState BlendState;
        protected DepthStencilState DepthStencilStateOpaquePass;
        protected DepthStencilState DepthStencilStateTransparentPass;
        protected SamplerState SamplerStateBaseColor;

        protected EffectTechnique Technique;
        EffectTechnique VegetationTechnique;

        public static readonly DepthStencilState DepthReadCompareLess = new DepthStencilState
        {
            DepthBufferWriteEnable = false,
            DepthBufferFunction = CompareFunction.Less,
        };
        private static readonly Dictionary<TextureAddressMode, Dictionary<float, SamplerState>> SamplerStates = new Dictionary<TextureAddressMode, Dictionary<float, SamplerState>>();
        protected int DefaultAlphaCutOff;
        protected readonly int ReferenceAlphaTransparentPass = 10; // ie default lightcone's are 9 in full transparent areas


        public SceneryMaterial(Viewer viewer, string texturePath, SceneryMaterialOptions options, float mipMapBias, int alphaCutOff)
            : base(viewer, String.Format("{0}:{1:X}:{2}", texturePath, options, mipMapBias))
        {
            Options = options;
            MipMapBias = mipMapBias;
            TexturePath = texturePath;
            Texture = SharedMaterialManager.MissingTexture;
            NightTexture = SharedMaterialManager.MissingTexture;
            DefaultAlphaCutOff = alphaCutOff;
        }

        public SceneryMaterial(Viewer viewer, string texturePath, SceneryMaterialOptions options, float mipMapBias)
            : this(viewer, texturePath, options, mipMapBias, 200)
        {
            // <CSComment> if "trainset" is in the path (true for night textures for 3DCabs) deferred load of night textures is disabled 
            if (!String.IsNullOrEmpty(texturePath) && (Options & SceneryMaterialOptions.NightTexture) != 0 && ((!viewer.IsDaytime && !viewer.IsNighttime)
                || TexturePath.Contains(@"\trainset\")))
            {
                var nightTexturePath = Helpers.GetNightTextureFile(Viewer.Simulator, texturePath);
                if (!String.IsNullOrEmpty(nightTexturePath))
                    NightTexture = Viewer.TextureManager.Get(nightTexturePath);
                Texture = Viewer.TextureManager.Get(texturePath, true);
            }
            else if ((Options & SceneryMaterialOptions.NightTexture) != 0 && viewer.IsDaytime)
            {
                viewer.NightTexturesNotLoaded = true;
                Texture = Viewer.TextureManager.Get(texturePath, true);
            }
            else if ((Options & SceneryMaterialOptions.NightTexture) != 0 && viewer.IsNighttime)
            {
                var nightTexturePath = Helpers.GetNightTextureFile(Viewer.Simulator, texturePath);
                if (!String.IsNullOrEmpty(nightTexturePath))
                    NightTexture = Viewer.TextureManager.Get(nightTexturePath);
                if (NightTexture != SharedMaterialManager.MissingTexture)
                {
                    viewer.DayTexturesNotLoaded = true;
                }
            }
            else Texture = Viewer.TextureManager.Get(texturePath, true);

            switch (Options & SceneryMaterialOptions.SpecularMask)
            {
                case SceneryMaterialOptions.Specular0: LightingSpecular = 0; break;
                case SceneryMaterialOptions.Specular25: LightingSpecular = 25; break;
                case SceneryMaterialOptions.Specular750: LightingSpecular = 750; break;
                default: throw new InvalidDataException("Options has unexpected SceneryMaterialOptions.SpecularMask value.");
            }

            LightingDiffuse = (Options & SceneryMaterialOptions.Diffuse) != 0 ? 1 : 0;

            // Record the number of bits in the alpha channel of the original ace file
            var texture = SharedMaterialManager.MissingTexture;
            if (Texture != SharedMaterialManager.MissingTexture && Texture != null) texture = Texture;
            else if (NightTexture != SharedMaterialManager.MissingTexture && NightTexture != null) texture = NightTexture;
            if (texture.Tag != null && texture.Tag.GetType() == typeof(Orts.Formats.Msts.AceInfo))
                AceAlphaBits = ((Orts.Formats.Msts.AceInfo)texture.Tag).AlphaBits;
            else
                AceAlphaBits = 0;

            RasterizerState = RasterizerState.CullCounterClockwise;
            SamplerStateBaseColor = GetSamplerStateBaseColor();

            var shader = Viewer.MaterialManager.SceneryShader;

            switch (Options & SceneryMaterialOptions.ShaderMask)
            {
                case SceneryMaterialOptions.ShaderImage: Technique = shader.Techniques["Image"]; break;
                case SceneryMaterialOptions.ShaderDarkShade: Technique = shader.Techniques["DarkShade"]; break;
                case SceneryMaterialOptions.ShaderHalfBright: Technique = shader.Techniques["HalfBright"]; break;
                case SceneryMaterialOptions.ShaderFullBright: Technique = shader.Techniques["FullBright"]; break;
                case SceneryMaterialOptions.ShaderVegetation | SceneryMaterialOptions.ShaderFullBright:
                case SceneryMaterialOptions.ShaderVegetation: Technique = VegetationTechnique = shader.Techniques["Vegetation"]; break;
                default:
                    break;
            }

            SetupStates(); // Needs to have the AceAlphaBits preset
            SetupSorting();
        }

        protected void SetupStates()
        {
            var needsTransparentBlending = GetBlending();
            if (needsTransparentBlending && (Options & SceneryMaterialOptions.AlphaBlendingMask) == SceneryMaterialOptions.AlphaBlendingAdd)
            {
                BlendState = BlendState.Additive;
                DepthStencilStateOpaquePass = DepthStencilStateTransparentPass = DepthStencilState.DepthRead;
                DefaultAlphaCutOff = ReferenceAlphaTransparentPass;
            }
            else if (needsTransparentBlending)
            {
                BlendState = BlendState.NonPremultiplied;
                DepthStencilStateOpaquePass = DepthStencilStateTransparentPass = DepthStencilState.Default;
                DefaultAlphaCutOff = 250;

                if ((Options & SceneryMaterialOptions.AlphaBlendingMask) == SceneryMaterialOptions.AlphaBlendingBlend)
                    DepthStencilStateTransparentPass = DepthReadCompareLess;
            }
            else
            {
                BlendState = BlendState.Opaque;
                DepthStencilStateOpaquePass = DepthStencilStateTransparentPass = DepthStencilState.Default;
                if ((Options & SceneryMaterialOptions.AlphaTest) == 0)
                    DefaultAlphaCutOff = -1;
            }
        }

        protected void SetupSorting()
        {
            SetSortingEffectId(Technique);
            SetSortingBlendStateId(BlendState);
            SetSortingDepthStencilStateId(DepthStencilStateOpaquePass);
            SetSortingRasterizerStateId(RasterizerState);
            SetSortingSamplerStateId(SamplerStateBaseColor);
            SetSortingTextureId(Texture.Name);
        }

        public bool LoadNightTexture()
        {
            bool oneMore = false;
            if (((Options & SceneryMaterialOptions.NightTexture) != 0) && (NightTexture == SharedMaterialManager.MissingTexture))
            {
                var nightTexturePath = Helpers.GetNightTextureFile(Viewer.Simulator, TexturePath);
                if (!String.IsNullOrEmpty(nightTexturePath))
                {
                    NightTexture = Viewer.TextureManager.Get(nightTexturePath);
                    oneMore = true;
                }
            }
            return oneMore;
        }

        public bool LoadDayTexture()
        {
            bool oneMore = false;
            if (Texture == SharedMaterialManager.MissingTexture && !String.IsNullOrEmpty(TexturePath))
            {
                Texture = Viewer.TextureManager.Get(TexturePath);
                oneMore = true;
            }
            return oneMore;
        }

        protected bool IsNightTimeOrUnderground() => (Options & SceneryMaterialOptions.UndergroundTexture) != 0 && (Viewer.MaterialManager.sunDirection.Y < -0.085f || Viewer.Camera.IsUnderground) || Viewer.MaterialManager.sunDirection.Y < -(float)KeyLengthRemainder() / 5000f;

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            var shader = Viewer.MaterialManager.SceneryShader;

            shader.CurrentTechnique = Technique;

            if (shader.CurrentTechnique == VegetationTechnique)
                shader.SetVegetationMaterial();

            shader.ImageTextureIsNight = NightTexture != null && NightTexture != SharedMaterialManager.MissingTexture && IsNightTimeOrUnderground();
            shader.ImageTexture = shader.ImageTextureIsNight ? NightTexture : Texture;
            shader.LightingSpecular = LightingSpecular;
            shader.LightingDiffuse = LightingDiffuse;

            var transparentPass = previousMaterial != null;

            shader.ReferenceAlpha = !transparentPass ? DefaultAlphaCutOff : ReferenceAlphaTransparentPass;
            graphicsDevice.DepthStencilState = !transparentPass ? DepthStencilStateOpaquePass : DepthStencilStateTransparentPass;
            graphicsDevice.RasterizerState = RasterizerState;
            graphicsDevice.BlendState = BlendState;
            graphicsDevice.SamplerStates[(int)SceneryShader.Samplers.BaseColor] = SamplerStateBaseColor;
            // ShaderPasses.Current.Apply() would overwrite the SamplerStates, but by removing the fix states and
            // leaving only the declaration in the shader, the sampler states can be set here instead.
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.SceneryShader;
            var passes = shader.CurrentTechnique.Passes;
            for (int i = 0; i < passes.Count; i++)
            {
                foreach (var item in renderItems)
                {
                    shader.SetMatrix(item.XNAMatrix);
                    shader.ZBias = item.RenderPrimitive.ZBias;
                    passes[i].Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            var shader = Viewer.MaterialManager.SceneryShader;
            shader.ImageTextureIsNight = false;
            shader.LightingDiffuse = 1;
            shader.LightingSpecular = 0;
            shader.ReferenceAlpha = 0;

            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
        }

        /// <summary>
        /// Return true if this material requires alpha blending
        /// </summary>
        /// <returns></returns>
        public override bool GetBlending()
        {
            bool alphaTestRequested = (Options & SceneryMaterialOptions.AlphaTest) != 0;            // the artist requested alpha testing for this material
            bool alphaBlendRequested = (Options & SceneryMaterialOptions.AlphaBlendingMask) != 0;   // the artist specified a blend capable shader

            return alphaBlendRequested                                   // the material is using a blend capable shader   
                    && (AceAlphaBits > 1                                    // and the original ace has more than 1 bit of alpha
                          || (AceAlphaBits == 1 && !alphaTestRequested));    //  or its just 1 bit, but with no alphatesting, we must blend it anyway

            // To summarize, assuming we are using a blend capable shader ..
            //     0 bits of alpha - never blend
            //     1 bit of alpha - only blend if the alpha test wasn't requested
            //     >1 bit of alpha - always blend
        }

        public override Texture2D GetShadowTexture()
        {
            var timeOffset = ((float)KeyLengthRemainder()) / 5000f; // TODO for later use for pseudorandom texture switch time
            if (NightTexture != null && NightTexture != SharedMaterialManager.MissingTexture && (((Options & SceneryMaterialOptions.UndergroundTexture) != 0 &&
                (Viewer.MaterialManager.sunDirection.Y < -0.085f || Viewer.Camera.IsUnderground)) || Viewer.MaterialManager.sunDirection.Y < 0.0f - ((float)KeyLengthRemainder()) / 5000f))
                return NightTexture;

            return Texture;
        }

        public override SamplerState GetShadowTextureAddressMode() { return SamplerStateBaseColor; }

        public SamplerState GetSamplerStateBaseColor()
        {
            var mipMapBias = MipMapBias < -1 ? -1 : MipMapBias;
            TextureAddressMode textureAddressMode;
            switch (Options & SceneryMaterialOptions.TextureAddressModeMask)
            {
                case SceneryMaterialOptions.TextureAddressModeWrap:
                    textureAddressMode = TextureAddressMode.Wrap; break;
                case SceneryMaterialOptions.TextureAddressModeMirror:
                    textureAddressMode = TextureAddressMode.Mirror; break;
                case SceneryMaterialOptions.TextureAddressModeClamp:
                    textureAddressMode = TextureAddressMode.Clamp; break;
                case SceneryMaterialOptions.TextureAddressModeBorder:
                    textureAddressMode = TextureAddressMode.Border; break;
                default:
                    throw new InvalidDataException("Options has unexpected SceneryMaterialOptions.TextureAddressModeMask value.");
            }

            if (!SamplerStates.ContainsKey(textureAddressMode))
                SamplerStates.Add(textureAddressMode, new Dictionary<float, SamplerState>());

            if (!SamplerStates[textureAddressMode].ContainsKey(mipMapBias))
                SamplerStates[textureAddressMode].Add(mipMapBias, new SamplerState
                {
                    AddressU = textureAddressMode,
                    AddressV = textureAddressMode,
                    Filter = TextureFilter.Anisotropic,
                    MaxAnisotropy = 16,
                    MipMapLevelOfDetailBias = mipMapBias
                });

            return SamplerStates[textureAddressMode][mipMapBias];

        }

        public override void Mark()
        {
            Viewer.TextureManager.Mark(Texture);
            Viewer.TextureManager.Mark(NightTexture);
            base.Mark();
        }
    }

    public class PbrMaterial : SceneryMaterial
    {
        protected readonly Texture2D MetallicRoughnessTexture;
        protected readonly Texture2D NormalTexture;
        protected readonly Texture2D OcclusionTexture;
        protected readonly Texture2D EmissiveTexture;
        protected readonly Texture2D ClearcoatTexture;
        protected readonly Texture2D ClearcoatRoughnessTexture;
        protected readonly Texture2D ClearcoatNormalTexture;
        protected readonly Texture2D SpecularTexture;
        protected readonly Texture2D SpecularColorTexture;

        protected readonly Vector4 TexCoords1;
        protected readonly Vector4 TexCoords2;
        protected readonly Vector4 TexCoords3;

        // Animatable attributes
        protected Vector4 BaseColorFactor;
        protected float MetallicFactor;
        protected float RoughnessFactor;
        protected float NormalScale;
        protected float OcclusionStrength;
        protected Vector3 EmissiveFactor;
        protected float ClearcoatFactor;
        protected float ClearcoatRoughnessFactor;
        protected float ClearcoatNormalScale;
        protected float SpecularFactor;
        protected Vector3 SpecularColorFactor;
        protected float Ior;

        bool EmissiveFollowsDayNightCycle;

        protected readonly SamplerState SamplerStateMetallicRoughness;
        protected readonly SamplerState SamplerStateNormal;
        protected readonly SamplerState SamplerStateOcclusion;
        protected readonly SamplerState SamplerStateEmissive;
        protected readonly SamplerState SamplerStateClearcoat;
        protected readonly SamplerState SamplerStateClearcoatRoughness;
        protected readonly SamplerState SamplerStateClearcoatNormal;
        protected readonly SamplerState SamplerStateSpecular;
        protected readonly SamplerState SamplerStateSpecularColor;


        // Animation actuators:
        public void SetAlphaCutoff(float value) => DefaultAlphaCutOff = (int)(value * 255f);
        public void SetBaseColorFactor(Vector4 value) => BaseColorFactor = value;
        public void SetMetallicFactor(float value) => MetallicFactor = value;
        public void SetRoughnessFactor(float value) => RoughnessFactor = value;
        public void SetNormalScale(float value) => NormalScale = value;
        public void SetOcclusionSrtength(float value) => OcclusionStrength = value;
        public void SetEmissiveFactor(Vector3 value) => EmissiveFactor = value;
        public void SetClearcoatFactor(float value) => ClearcoatFactor = value;
        public void SetClearcoatRoughnessFactor(float value) => ClearcoatRoughnessFactor = value;
        public void SetClearcoatNormalScale(float value) => ClearcoatNormalScale = value;
        public void SetSpecularFactor(float value) => SpecularFactor = value;
        public void SetSpecularColorFactor(Vector3 value) => SpecularColorFactor = value;
        public void SetIor(float value) => Ior = value;

        static readonly Dictionary<(TextureFilter, TextureAddressMode, TextureAddressMode), SamplerState> GltfSamplerStates = new Dictionary<(TextureFilter, TextureAddressMode, TextureAddressMode), SamplerState>();

        public PbrMaterial(Viewer viewer, string materialUniqueId, SceneryMaterialOptions options, float mipMapBias,
            Texture2D baseColorTexture, Vector4 baseColorFactor,
            Texture2D metallicRoughnessTexture, float metallicFactor, float roughnessFactor,
            Texture2D normalTexture, float normalScale,
            Texture2D occlusionTexture, float occlusionStrength,
            Texture2D emissiveTexture, Vector3 emissiveFactor,
            Texture2D clearcoatTexture, float clearcoatFactor,
            Texture2D clearcoatRoughnessTexture, float clearcoatRoughnessFactor,
            Texture2D clearcoatNormalTexture, float clearcoatNormalScale,
            Texture2D specularTexture, float specularFactor,
            Texture2D specularColorTexture, Vector3 specularColorFactor, float ior,
            float referenceAlpha, bool doubleSided,
            Vector4 texCoords1, Vector4 texCoords2, Vector4 texCoords3,
            (TextureFilter, TextureAddressMode, TextureAddressMode) samplerStateBaseColor,
            (TextureFilter, TextureAddressMode, TextureAddressMode) samplerStateMetallicRoughness,
            (TextureFilter, TextureAddressMode, TextureAddressMode) samplerStateNormal,
            (TextureFilter, TextureAddressMode, TextureAddressMode) samplerStateOcclusion,
            (TextureFilter, TextureAddressMode, TextureAddressMode) samplerStateEmissive,
            (TextureFilter, TextureAddressMode, TextureAddressMode) samplerStateClearcoat,
            (TextureFilter, TextureAddressMode, TextureAddressMode) samplerStateClearcoatRoughness,
            (TextureFilter, TextureAddressMode, TextureAddressMode) samplerStateClearcoatNormal,
            (TextureFilter, TextureAddressMode, TextureAddressMode) samplerStateSpecular,
            (TextureFilter, TextureAddressMode, TextureAddressMode) samplerStateSpecularColor)
            : base(viewer, null, options, mipMapBias, (int)(referenceAlpha * 255f))
        {
            Texture = baseColorTexture;
            BaseColorFactor = baseColorFactor;
            MetallicRoughnessTexture = metallicRoughnessTexture;
            MetallicFactor = metallicFactor;
            RoughnessFactor = roughnessFactor;
            NormalTexture = normalTexture;
            NormalScale = normalScale;
            OcclusionTexture = occlusionTexture;
            OcclusionStrength = occlusionStrength;
            EmissiveTexture = emissiveTexture;
            EmissiveFactor = emissiveFactor;
            ClearcoatTexture = clearcoatTexture;
            ClearcoatFactor = clearcoatFactor;
            ClearcoatRoughnessTexture = clearcoatRoughnessTexture;
            ClearcoatRoughnessFactor = clearcoatRoughnessFactor;
            ClearcoatNormalTexture = clearcoatNormalTexture;
            ClearcoatNormalScale = clearcoatNormalScale;
            SpecularTexture = specularTexture;
            SpecularFactor = specularFactor;
            SpecularColorTexture = specularColorTexture;
            SpecularColorFactor = specularColorFactor;
            Ior = ior;
            TexCoords1 = texCoords1;
            TexCoords2 = texCoords2;
            TexCoords3 = texCoords3;

            LightingDiffuse = (Options & SceneryMaterialOptions.Diffuse) != 0 ? 1 : 0;

            RasterizerState = doubleSided ? RasterizerState.CullNone :
                ((Options & SceneryMaterialOptions.PbrCullClockWise) != 0) ? RasterizerState.CullClockwise : RasterizerState.CullCounterClockwise;

            samplerStateBaseColor.Item1 = ChangeToAnisitropic(samplerStateBaseColor.Item1);

            if (!GltfSamplerStates.TryGetValue(samplerStateBaseColor, out SamplerStateBaseColor)) GltfSamplerStates.Add(samplerStateBaseColor, SamplerStateBaseColor = GetNewSamplerState(samplerStateBaseColor));
            if (!GltfSamplerStates.TryGetValue(samplerStateMetallicRoughness, out SamplerStateMetallicRoughness)) GltfSamplerStates.Add(samplerStateMetallicRoughness, SamplerStateMetallicRoughness = GetNewSamplerState(samplerStateMetallicRoughness));
            if (!GltfSamplerStates.TryGetValue(samplerStateNormal, out SamplerStateNormal)) GltfSamplerStates.Add(samplerStateNormal, SamplerStateNormal = GetNewSamplerState(samplerStateNormal));
            if (!GltfSamplerStates.TryGetValue(samplerStateOcclusion, out SamplerStateOcclusion)) GltfSamplerStates.Add(samplerStateOcclusion, SamplerStateOcclusion = GetNewSamplerState(samplerStateOcclusion));
            if (!GltfSamplerStates.TryGetValue(samplerStateEmissive, out SamplerStateEmissive)) GltfSamplerStates.Add(samplerStateEmissive, SamplerStateEmissive = GetNewSamplerState(samplerStateEmissive));
            if (!GltfSamplerStates.TryGetValue(samplerStateClearcoat, out SamplerStateClearcoat)) GltfSamplerStates.Add(samplerStateClearcoat, SamplerStateClearcoat = GetNewSamplerState(samplerStateClearcoat));
            if (!GltfSamplerStates.TryGetValue(samplerStateClearcoatRoughness, out SamplerStateClearcoatRoughness)) GltfSamplerStates.Add(samplerStateClearcoatRoughness, SamplerStateClearcoatRoughness = GetNewSamplerState(samplerStateClearcoatRoughness));
            if (!GltfSamplerStates.TryGetValue(samplerStateClearcoatNormal, out SamplerStateClearcoatNormal)) GltfSamplerStates.Add(samplerStateClearcoatNormal, SamplerStateClearcoatNormal = GetNewSamplerState(samplerStateClearcoatNormal));
            if (!GltfSamplerStates.TryGetValue(samplerStateSpecular, out SamplerStateSpecular)) GltfSamplerStates.Add(samplerStateSpecular, SamplerStateSpecular = GetNewSamplerState(samplerStateSpecular));
            if (!GltfSamplerStates.TryGetValue(samplerStateSpecularColor, out SamplerStateSpecularColor)) GltfSamplerStates.Add(samplerStateSpecularColor, SamplerStateSpecularColor = GetNewSamplerState(samplerStateSpecularColor));

            var shader = Viewer.MaterialManager.SceneryShader;

            if ((Options & SceneryMaterialOptions.PbrHasMorphTargets) != 0)
                Technique = shader.Techniques["PbrMorphed"];
            else if ((Options & SceneryMaterialOptions.PbrHasSkin) != 0)
                Technique = shader.Techniques["PbrSkinned"];
            else if ((Options & SceneryMaterialOptions.PbrHasTexCoord1) != 0)
                Technique = shader.Techniques["PbrNormalMap"];
            else
                Technique = shader.Techniques["PbrBaseColorMap"];

            SetupStates();
            SetupSorting();
        }

        public override bool GetBlending() => (Options & SceneryMaterialOptions.AlphaBlendingBlend) != 0;
        
        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            var shader = Viewer.MaterialManager.SceneryShader;

            shader.CurrentTechnique = Technique;;
            shader.ImageTexture = Texture;
            shader.BaseColorFactor = BaseColorFactor;
            shader.NormalTexture = NormalTexture;
            shader.NormalScale = NormalScale;
            shader.EmissiveTexture = EmissiveTexture;
            shader.EmissiveFactor = !EmissiveFollowsDayNightCycle || IsNightTimeOrUnderground() ? EmissiveFactor : Vector3.Zero;
            shader.OcclusionTexture = OcclusionTexture;
            shader.MetallicRoughnessTexture = MetallicRoughnessTexture;
            shader.OcclusionFactor = new Vector3(OcclusionStrength == 2 ? 0 : OcclusionStrength, RoughnessFactor, MetallicFactor);
            shader.HasNormals = (Options & SceneryMaterialOptions.PbrHasNormals) != 0;
            shader.HasTangents = (Options & SceneryMaterialOptions.PbrHasTangents) != 0;
            shader.ClearcoatFactor = ClearcoatFactor;
            if (ClearcoatFactor > 0 && RenderProcess.CLEARCOAT)
            {
                shader.ClearcoatTexture = ClearcoatTexture;
                shader.ClearcoatRoughnessTexture = ClearcoatRoughnessTexture;
                shader.ClearcoatRoughnessFactor = ClearcoatRoughnessFactor;
                shader.ClearcoatNormalTexture = ClearcoatNormalTexture;
                shader.ClearcoatNormalScale = ClearcoatNormalScale;
            }
            if (SpecularFactor > 0)
            {
                shader.SpecularFactor = new Vector4(SpecularColorFactor, SpecularFactor);
                shader.SpecularTexture = SpecularTexture;
                shader.SpecularColorTexture = SpecularColorTexture;
            }
            shader.IorFactor = Ior;
            shader.TextureCoordinates1 = TexCoords1;
            shader.TextureCoordinates2 = TexCoords2;
            shader.TextureCoordinates3 = TexCoords3;

            shader.LightingDiffuse = LightingDiffuse;

            var transparentPass = previousMaterial != null;

            shader.ReferenceAlpha = !transparentPass ? DefaultAlphaCutOff : ReferenceAlphaTransparentPass;
            graphicsDevice.DepthStencilState = !transparentPass ? DepthStencilStateOpaquePass : DepthStencilStateTransparentPass;
            graphicsDevice.RasterizerState = RasterizerState;
            graphicsDevice.BlendState = BlendState;

            // ShaderPasses.Current.Apply() would overwrite the SamplerStates, but by removing the fix states and
            // leaving only the declaration in the shader, the sampler states can be set here instead.
            graphicsDevice.SamplerStates[(int)SceneryShader.Samplers.BaseColor] = SamplerStateBaseColor;
            graphicsDevice.SamplerStates[(int)SceneryShader.Samplers.MetallicRoughness] = SamplerStateMetallicRoughness;
            graphicsDevice.SamplerStates[(int)SceneryShader.Samplers.Occlusion] = SamplerStateOcclusion;
            graphicsDevice.SamplerStates[(int)SceneryShader.Samplers.Normal] = SamplerStateNormal;
            graphicsDevice.SamplerStates[(int)SceneryShader.Samplers.Emissive] = SamplerStateEmissive;
            if (ClearcoatFactor > 0 && RenderProcess.CLEARCOAT)
            {
                graphicsDevice.SamplerStates[(int)SceneryShader.Samplers.Clearcoat] = SamplerStateClearcoat;
                graphicsDevice.SamplerStates[(int)SceneryShader.Samplers.ClearcoatRoughness] = SamplerStateClearcoatRoughness;
                graphicsDevice.SamplerStates[(int)SceneryShader.Samplers.ClearcoatNormal] = SamplerStateClearcoatNormal;
            }
            if (SpecularFactor > 0)
            {
                graphicsDevice.SamplerStates[(int)SceneryShader.Samplers.Specular] = SamplerStateSpecular;
                graphicsDevice.SamplerStates[(int)SceneryShader.Samplers.SpecularColor] = SamplerStateSpecularColor;
            }
        }

        // Currently isn't possible to set a glTF to anisotropic filtering, so this is a hack against the spec:
        static TextureFilter ChangeToAnisitropic(TextureFilter textureFilter) => textureFilter == TextureFilter.Linear ? TextureFilter.Anisotropic : textureFilter;

        static SamplerState GetNewSamplerState((TextureFilter, TextureAddressMode, TextureAddressMode) samplerAttributes)
        {
           return new SamplerState
            {
                Filter = samplerAttributes.Item1,
                AddressU = samplerAttributes.Item2,
                AddressV = samplerAttributes.Item3,
                MaxAnisotropy = 16,
            };
        }
        
        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.SceneryShader;

            var passes = shader.CurrentTechnique.Passes;
            for (int i = 0; i < passes.Count; i++)
            {
                foreach (var item in renderItems)
                {
                    shader.SetMatrix(item.XNAMatrix);
                    shader.ZBias = item.RenderPrimitive.ZBias;

                    if (item.RenderPrimitive is GltfShape.GltfPrimitive gltfPrimitive)
                    {
                        gltfPrimitive.BonesTexture?.SetData(MemoryMarshal.Cast<Matrix, Vector4>(gltfPrimitive.RenderBonesRendered).ToArray());
                        shader.BonesTexture = gltfPrimitive.BonesTexture;
                        shader.HasSkin = gltfPrimitive.BonesTexture != null;

                        if (gltfPrimitive.HasMorphTargets())
                            (shader.MorphConfig, shader.MorphWeights) = gltfPrimitive.GetMorphingData();
                    }

                    passes[i].Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            base.ResetState(graphicsDevice);
            graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
        }
    }

    public class ShadowMapMaterial : Material
    {
        VertexBuffer BlurVertexBuffer;
        static readonly SamplerState ShadowMapSamplerState = new SamplerState
        {
            Filter = TextureFilter.LinearMipPoint,
            AddressU = TextureAddressMode.Wrap,
            AddressV = TextureAddressMode.Wrap,
        };

        public enum Mode
        {
            Normal,
            Pbr,
            PbrSkinned,
            PbrMorphed,
            Forest,
            Blocker,
        }

        public ShadowMapMaterial(Viewer viewer)
            : base(viewer, null)
        {
            var shadowMapResolution = Viewer.Settings.ShadowMapResolution;
            BlurVertexBuffer = new VertexBuffer(Viewer.RenderProcess.GraphicsDevice, typeof(VertexPositionTexture), 4, BufferUsage.WriteOnly);
            BlurVertexBuffer.SetData(new[] {
                new VertexPositionTexture(new Vector3(-1, +1, 0), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(-1, -1, 0), new Vector2(0, shadowMapResolution)),
                new VertexPositionTexture(new Vector3(+1, +1, 0), new Vector2(shadowMapResolution, 0)),
                new VertexPositionTexture(new Vector3(+1, -1, 0), new Vector2(shadowMapResolution, shadowMapResolution)),
            });
        }

        public void SetState(GraphicsDevice graphicsDevice, Mode mode)
        {
            var shader = Viewer.MaterialManager.ShadowMapShader;
            shader.CurrentTechnique = shader.Techniques[
                mode == Mode.Forest ? "ShadowMapForest" : 
                mode == Mode.Blocker ? "ShadowMapBlocker" : 
                mode == Mode.Pbr ? "ShadowMapNormalMap" :
                mode == Mode.PbrSkinned ? "ShadowMapSkinned" :
                mode == Mode.PbrMorphed ? "ShadowMapMorphed" :
                "ShadowMap"];

            // ShaderPasses.Current.Apply() would overwrite the SamplerStates. but by removing the fix states and
            // leaving only the declaration in the shader, the sampler states can be set here instead.
            graphicsDevice.SamplerStates[0] = GetShadowTextureAddressMode();
            graphicsDevice.SamplerStates[1] = ShadowMapSamplerState;
            graphicsDevice.RasterizerState = mode == Mode.Blocker ? RasterizerState.CullClockwise : RasterizerState.CullCounterClockwise;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
            graphicsDevice.BlendState = BlendState.Opaque;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.ShadowMapShader;
            var passes = shader.CurrentTechnique.Passes;
            for (int i = 0; i < passes.Count; i++)
            {
                foreach (var item in renderItems)
                {
                    shader.SetData(item.XNAMatrix, item.Material.GetShadowTexture());

                    if (item.RenderPrimitive is GltfShape.GltfPrimitive gltfPrimitive)
                    {
                        if (gltfPrimitive.BonesTexture != null)
                        {
                            shader.BonesTexture = gltfPrimitive.BonesTexture;
                            shader.HasSkin = gltfPrimitive.BonesTexture != null;
                        }

                        if (gltfPrimitive.HasMorphTargets())
                            (shader.MorphConfig, shader.MorphWeights) = gltfPrimitive.GetMorphingData();
                    }

                    passes[i].Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }
        }

        public void ApplyBlur(GraphicsDevice graphicsDevice, RenderTarget2D shadowMap, RenderTarget2D renderTarget, int shadowMapIndex)
        {
            var shader = Viewer.MaterialManager.ShadowMapShader;
            shader.CurrentTechnique = shader.Techniques["ShadowMapBlur"];

            graphicsDevice.RasterizerState = RasterizerState.CullNone;
            graphicsDevice.DepthStencilState = DepthStencilState.None;
            graphicsDevice.SetVertexBuffer(BlurVertexBuffer);

            var passes = shader.CurrentTechnique.Passes;
            for (int i = 0; i < passes.Count; i++)
            {
                shader.SetBlurData(renderTarget, shadowMapIndex);
                passes[i].Apply();
                graphicsDevice.SetRenderTarget(shadowMap, shadowMapIndex);
                graphicsDevice.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);

                graphicsDevice.SetRenderTarget(null);
            }

            graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
        }
    }

    public class PopupWindowMaterial : Material
    {
        IEnumerator<EffectPass> ShaderPassesPopupWindow;
        IEnumerator<EffectPass> ShaderPassesPopupWindowGlass;
        IEnumerator<EffectPass> ShaderPasses;

        public PopupWindowMaterial(Viewer viewer)
            : base(viewer, null)
        {
            SetSortingEffectId(Viewer.MaterialManager.PopupWindowShader.Techniques["PopupWindow"]);
        }

        public void SetState(GraphicsDevice graphicsDevice, Texture2D screen)
        {
            var shader = Viewer.MaterialManager.PopupWindowShader;
            shader.CurrentTechnique = screen == null ? shader.Techniques["PopupWindow"] : shader.Techniques["PopupWindowGlass"];
            if (ShaderPassesPopupWindow == null) ShaderPassesPopupWindow = shader.Techniques["PopupWindow"].Passes.GetEnumerator();
            if (ShaderPassesPopupWindowGlass == null) ShaderPassesPopupWindowGlass = shader.Techniques["PopupWindowGlass"].Passes.GetEnumerator();
            ShaderPasses = screen == null ? ShaderPassesPopupWindow : ShaderPassesPopupWindowGlass;
            shader.Screen = screen;
            shader.GlassColor = Color.Black;

            graphicsDevice.BlendState = BlendState.NonPremultiplied;
            graphicsDevice.RasterizerState = RasterizerState.CullNone;
            graphicsDevice.DepthStencilState = DepthStencilState.None;
        }

        public void Render(GraphicsDevice graphicsDevice, RenderPrimitive renderPrimitive, ref Matrix XNAWorldMatrix, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.PopupWindowShader;

            Matrix wvp = XNAWorldMatrix * XNAViewMatrix * XNAProjectionMatrix;
            shader.SetMatrix(XNAWorldMatrix, ref wvp);

            ShaderPasses.Reset();
            while (ShaderPasses.MoveNext())
            {
                ShaderPasses.Current.Apply();
                renderPrimitive.Draw(graphicsDevice);
            }
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
        }

        public override bool GetBlending()
        {
            return true;
        }
    }

    public class ScreenMaterial : SceneryMaterial
    {
        RollingStock.CabViewControlRenderer ScreenRenderer;
        public readonly int HierarchyIndex;

        public ScreenMaterial(Viewer viewer, string key, int hierarchyIndex)
            : base(viewer, key, SceneryMaterialOptions.ShaderFullBright, 0)
        {
            HierarchyIndex = hierarchyIndex;
            SetSortingTextureId("Screen");
        }

        public void Set2DRenderer(RollingStock.CabViewControlRenderer cabViewControlRenderer)
        {
            ScreenRenderer = cabViewControlRenderer;
            Texture = new RenderTarget2D(Viewer.GraphicsDevice,
                (int)ScreenRenderer.Control.Width, (int)ScreenRenderer.Control.Height, false, SurfaceFormat.Color, DepthFormat.None, 8, RenderTargetUsage.DiscardContents);
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            if (ScreenRenderer != null)
            {
                var originalRenderTargets = graphicsDevice.GetRenderTargets();
                graphicsDevice.SetRenderTarget(Texture as RenderTarget2D);
                ScreenRenderer.ControlView.SpriteBatch.Begin();
                ScreenRenderer.Draw(graphicsDevice);
                ScreenRenderer.ControlView.SpriteBatch.End();
                graphicsDevice.SetRenderTargets(originalRenderTargets);
            }

            Viewer.MaterialManager.SceneryShader.ImageTexture = Texture;

            base.Render(graphicsDevice, renderItems, ref XNAViewMatrix, ref XNAProjectionMatrix);
        }
    }

    public class YellowMaterial : Material
    {
        static BasicEffect basicEffect;

        public YellowMaterial(Viewer viewer)
            : base(viewer, null)
        {
            if (basicEffect == null)
            {
                basicEffect = new BasicEffect(Viewer.RenderProcess.GraphicsDevice);
                basicEffect.Alpha = 1.0f;
                basicEffect.DiffuseColor = new Vector3(197.0f / 255.0f, 203.0f / 255.0f, 37.0f / 255.0f);
                basicEffect.SpecularColor = new Vector3(0.25f, 0.25f, 0.25f);
                basicEffect.SpecularPower = 5.0f;
                basicEffect.AmbientLightColor = new Vector3(0.2f, 0.2f, 0.2f);

                basicEffect.DirectionalLight0.Enabled = true;
                basicEffect.DirectionalLight0.DiffuseColor = Vector3.One * 0.8f;
                basicEffect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(1.0f, -1.0f, -1.0f));
                basicEffect.DirectionalLight0.SpecularColor = Vector3.One;

                basicEffect.DirectionalLight1.Enabled = true;
                basicEffect.DirectionalLight1.DiffuseColor = new Vector3(0.5f, 0.5f, 0.5f);
                basicEffect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(-1.0f, -1.0f, 1.0f));
                basicEffect.DirectionalLight1.SpecularColor = new Vector3(0.5f, 0.5f, 0.5f);

                basicEffect.LightingEnabled = true;
            }

            SetSortingEffectId(basicEffect.CurrentTechnique);
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {

            basicEffect.View = XNAViewMatrix;
            basicEffect.Projection = XNAProjectionMatrix;

            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                foreach (var item in renderItems)
                {
                    basicEffect.World = item.XNAMatrix;
                    pass.Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }
        }
    }

    public class SolidColorMaterial : Material
    {
        BasicEffect basicEffect;

        public SolidColorMaterial(Viewer viewer, float a, float r, float g, float b)
            : base(viewer, null)
        {
            if (basicEffect == null)
            {
                basicEffect = new BasicEffect(Viewer.RenderProcess.GraphicsDevice);
                basicEffect.Alpha = a;
                basicEffect.DiffuseColor = new Vector3(r, g, b);
                basicEffect.SpecularColor = new Vector3(0.25f, 0.25f, 0.25f);
                basicEffect.SpecularPower = 5.0f;
                basicEffect.AmbientLightColor = new Vector3(0.2f, 0.2f, 0.2f);

                basicEffect.DirectionalLight0.Enabled = true;
                basicEffect.DirectionalLight0.DiffuseColor = Vector3.One * 0.8f;
                basicEffect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(1.0f, -1.0f, -1.0f));
                basicEffect.DirectionalLight0.SpecularColor = Vector3.One;

                basicEffect.DirectionalLight1.Enabled = true;
                basicEffect.DirectionalLight1.DiffuseColor = new Vector3(0.5f, 0.5f, 0.5f);
                basicEffect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(-1.0f, -1.0f, 1.0f));
                basicEffect.DirectionalLight1.SpecularColor = new Vector3(0.5f, 0.5f, 0.5f);

                basicEffect.LightingEnabled = true;
            }

            SetSortingEffectId(basicEffect.CurrentTechnique);
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {

            basicEffect.View = XNAViewMatrix;
            basicEffect.Projection = XNAProjectionMatrix;

            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                foreach (var item in renderItems)
                {
                    basicEffect.World = item.XNAMatrix;
                    pass.Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }
        }
    }

    public class Label3DMaterial : SpriteBatchMaterial
    {
        public readonly Texture2D Texture;
        public readonly WindowTextFont Font;
        public readonly WindowTextFont BigFont;

        readonly List<Rectangle> TextBoxes = new List<Rectangle>();

        public Label3DMaterial(Viewer viewer)
            : base(viewer)
        {
            Texture = new Texture2D(SpriteBatch.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            Texture.SetData(new[] { Color.White });
            Font = Viewer.WindowManager.TextManager.GetScaled("Arial", 12, System.Drawing.FontStyle.Bold, 1);
            BigFont = Viewer.WindowManager.TextManager.GetScaled("Arial", 24, System.Drawing.FontStyle.Bold, 2);
            
            SetSortingTextureId("White");
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);
            SpriteBatch.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            TextBoxes.Clear();
            base.Render(graphicsDevice, renderItems, ref XNAViewMatrix, ref XNAProjectionMatrix);
        }

        public override bool GetBlending()
        {
            return true;
        }

        public Point GetTextLocation(int x, int y, string text)
        {
            // Start with a box in the location specified.
            var textBox = new Rectangle(x, y, Font.MeasureString(text), Font.Height);
            textBox.X -= textBox.Width / 2;
            textBox.Inflate(5, 2);
            // Find all the existing boxes which overlap with the new box, as if its top was extended upwards to infinity.
            var boxes = TextBoxes.Where(box => box.Top <= textBox.Bottom && box.Right >= textBox.Left && box.Left <= textBox.Right).OrderBy(box => -box.Top);
            // For each possible colliding box, if it does collide, shift the new box above it.
            foreach (var box in boxes)
                if (box.Top <= textBox.Bottom && box.Bottom >= textBox.Top)
                    textBox.Y = box.Top - textBox.Height;
            // And we're done.
            TextBoxes.Add(textBox);
            return new Point(textBox.X + 5, textBox.Y + 2);
        }
    }

    public class DebugNormalMaterial : Material
    {
        IEnumerator<EffectPass> ShaderPassesGraph;

        public DebugNormalMaterial(Viewer viewer)
            : base(viewer, null)
        {
            SetSortingEffectId(Viewer.MaterialManager.DebugShader.Techniques["Normal"]);
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            var shader = Viewer.MaterialManager.DebugShader;
            shader.CurrentTechnique = shader.Techniques["Normal"];
            if (ShaderPassesGraph == null) ShaderPassesGraph = shader.Techniques["Normal"].Passes.GetEnumerator();
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.DebugShader;
            var viewproj = XNAViewMatrix * XNAProjectionMatrix;

            ShaderPassesGraph.Reset();
            while (ShaderPassesGraph.MoveNext())
            {
                foreach (var item in renderItems)
                {
                    shader.SetMatrix(item.XNAMatrix, ref viewproj);
                    ShaderPassesGraph.Current.Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }
        }
    }
}
