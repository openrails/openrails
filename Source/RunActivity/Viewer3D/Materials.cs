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
using glTFLoader.Schema;
using Orts.Viewer3D;
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
                            if (defaultTexture != SharedMaterialManager.MissingTexture)
                            {
                                // This texture has been set to use a non-standard default texture,
                                // use this default texture instead of searching other folders
                                return defaultTexture;
                            }
                            // When a texture is not found, there is no special missing texture defined,
                            // and it is in a selector directory (e.g. "Snow"), we go up a level and
                            // try again. This repeats a fixed number of times, or until we run
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
#if DEBUG
            texture.SetData(new[] { Color.Magenta});
#else
            texture.SetData(new[] { Color.Gray });
#endif
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
        public readonly BloomShader BloomShader;
        public readonly DebugShader DebugShader;
        public readonly CabShader CabShader;

        public static Texture2D MissingTexture;
        public static Texture2D DefaultSnowTexture;
        public static Texture2D DefaultDMSnowTexture;
        public static Texture2D WhiteTexture;
        public static Texture2D BlackTexture;

        static Texture2D EnvironmentMapSpecularDay;
        static Texture2D BrdfLutTexture;

        static readonly Vector3[] ShDay = new[]
        {
            new Vector3(0.933248f,  0.827154f,  0.734785f), // L0,0
            new Vector3(0.149667f,  0.182847f,  0.222186f), // L1,-1
            new Vector3(0.072382f,  0.070470f,  0.052508f), // L1,0
            new Vector3(0.009977f,  0.018481f,  0.022093f), // L1,1
            new Vector3(-0.004932f, -0.003692f, 0.000172f), // L2,-2
            new Vector3(-0.025539f, -0.016006f, -0.009408f),// L2,-1
            new Vector3(0.015451f,  0.015465f,  0.024591f), // L2,0
            new Vector3(0.006221f,  0.011580f,  0.012703f), // L2,1
            new Vector3(0.027432f,  0.010342f,  0.003231f)  // L2,2
        };

        static readonly Vector3[] ShNight = new[]
        {
            new Vector3(0.080f, 0.110f, 0.180f),    // L0,0
            new Vector3(0.015f, 0.025f, 0.045f),    // L1,-1
            new Vector3(-0.020f, -0.030f, -0.060f), // L1,0
            new Vector3(0.010f, 0.015f, 0.025f),    // L1,1
            new Vector3(-0.002f, -0.003f, -0.005f), // L2,-2
            new Vector3(-0.005f, -0.007f, -0.012f), // L2,-1
            new Vector3(0.004f, 0.006f, 0.010f),    // L2,0
            new Vector3(0.002f, 0.003f, 0.005f),    // L2,1
            new Vector3(0.005f, 0.007f, 0.012f)     // L2,2
        };

        readonly Vector3[] ShActual = new Vector3[9];

        Matrix ShRed, ShGreen, ShBlue;

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
            BloomShader = new BloomShader(viewer.RenderProcess.GraphicsDevice);
            DebugShader = new DebugShader(viewer.RenderProcess.GraphicsDevice);
            CabShader = new CabShader(viewer.RenderProcess.GraphicsDevice, Vector4.One, Vector4.One, Vector3.One, Vector3.One);

            MissingTexture = SharedTextureManager.GetInternalMissingTexture(viewer.RenderProcess.GraphicsDevice);

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

        public Material Load(string materialName, string textureName = null, int options = 0, float mipMapBias = 0f, Effect effect = null, Gltf gltfFile = null)
        {
            var materialKey = (materialName, textureName?.ToLowerInvariant(), options, mipMapBias, effect);
            if (!Materials.ContainsKey(materialKey))
            {
                switch (materialName)
                {
                    case "Bloom":
                        Materials[materialKey] = new BloomMaterial(Viewer);
                        break;
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
                    case "PBR":
                        Materials[materialKey] = new PbrMaterial(Viewer, textureName?.ToLowerInvariant(), (SceneryMaterialOptions)options, mipMapBias, gltfFile);
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

            SetFrameSphericalHarmonics();

            if (EnvironmentMapSpecularDay == null)
            {
                // TODO: split the equirectangular specular panorama image to a cube map for saving the pixel shader instructions of converting the
                // cartesian cooridinates to polar for sampling. Couldn't find a converter though that also supports RGBD color encoding.
                // RGBD is an encoding where a divider [0..1] is stored in the alpha channel to reconstruct the High Dynamic Range of the RGB colors.
                // A HDR to TGA-RGBD converter is available here: https://seenax.com/portfolio/cpp.php , this can be further converted to PNG by e.g. GIMP.
                EnvironmentMapSpecularDay = Texture2D.FromStream(Viewer.GraphicsDevice, File.OpenRead(Path.Combine(Viewer.Game.ContentPath, "EnvMapDay/specular-RGBD.png")));
            }
            if (BrdfLutTexture == null)
            {
                using (var stream = File.OpenRead(Path.Combine(Viewer.Game.ContentPath, $"EnvMapDay/brdfLUT.png")))
                {
                    BrdfLutTexture = Viewer.TextureManager.GetSrgbTexture(Viewer.GraphicsDevice, stream);
                }
            }

            SceneryShader.BrdfLutTexture = BrdfLutTexture;

            if (sunDirection.Y > -0.05)
            {
                SceneryShader.EnvironmentMapSpecularTexture = EnvironmentMapSpecularDay;
            }
            else
            {
                SceneryShader.EnvironmentMapSpecularTexture = BlackTexture;
            }

            SceneryShader.Fog = FogColor;

            var ambientLightIntensity = Viewer.Simulator.Weather.AmbientLightingIntensity;

            if (Viewer.Settings.UseMSTSEnv == false)
            {
                SceneryShader.Overcast = new Vector2(Viewer.Simulator.Weather.CloudCoverFactor, ambientLightIntensity);
                ParticleEmitterShader.SetFog(Viewer.Simulator.Weather.VisibilityM, ref SharedMaterialManager.FogColor);
                SceneryShader.SetViewerPos(Viewer.Camera.XnaLocation(Viewer.Camera.CameraWorldLocation), Viewer.Simulator.Weather.VisibilityM);
            }
            else
            {
                SceneryShader.Overcast = new Vector2(Viewer.World.MSTSSky.mstsskyovercastFactor, ambientLightIntensity);
                ParticleEmitterShader.SetFog(Viewer.World.MSTSSky.mstsskyfogDistance, ref SharedMaterialManager.FogColor);
                SceneryShader.SetViewerPos(Viewer.Camera.XnaLocation(Viewer.Camera.CameraWorldLocation), Viewer.World.MSTSSky.mstsskyfogDistance);
            }
        }

        public void SetSphericalHarmonics(Vector3[] shDay, Vector3[] shNight = null, float nightDay = 1)
        {
            for (int i = 0; i < ShActual.Length; i++)
                ShActual[i] = Vector3.Lerp(shNight?.ElementAtOrDefault(i) ?? Vector3.Zero, shDay?.ElementAtOrDefault(i) ?? Vector3.Zero, nightDay);

            ShRed = getSHMatrix(ShActual, 0);
            ShGreen = getSHMatrix(ShActual, 1);
            ShBlue = getSHMatrix(ShActual, 2);

            // Spherical harmonics calculation for IBL diffuse ambient lighting.
            Matrix getSHMatrix(Vector3[] harmonics, int channel)
            {
                float c(int index) => channel == 0 ? harmonics[index].X : (channel == 1 ? harmonics[index].Y : harmonics[index].Z);
                return new Matrix(
                     c(8), c(4), c(7), c(3),
                     c(4), -c(8), c(5), c(1),
                     c(7), c(5), c(6), c(2),
                     c(3), c(1), c(2), c(0)
                );
            }

            SceneryShader.ShRed = ShRed;
            SceneryShader.ShGreen = ShGreen;
            SceneryShader.ShBlue = ShBlue;
        }

        public void SetFrameSphericalHarmonics()
        {
            var sunWeight = MathHelper.Clamp((sunDirection.Y + 0.15f) / 0.3f, 0, 1);
            SetSphericalHarmonics(ShDay, ShNight, sunWeight);
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
        public SceneryMaterialOptions Options;
        readonly float MipMapBias;
        protected Texture2D Texture;
        private readonly string TexturePath;
        protected Texture2D NightTexture;
        byte AceAlphaBits;   // the number of bits in the ace file's alpha channel 
        readonly float LightingSpecular;
        protected float LightingDiffuse;
        protected bool HasNormals;
        protected bool HasTangents;

        protected RasterizerState RasterizerState;
        protected BlendState BlendState;
        protected DepthStencilState DepthStencilStateOpaquePass;
        protected DepthStencilState DepthStencilStateTransparentPass;
        protected SamplerState SamplerStateBaseColor;

        protected EffectTechnique Technique;
        EffectTechnique VegetationTechnique;

        public static readonly DepthStencilState DepthReadCompareLess = new DepthStencilState
        {
            DepthBufferEnable = true,
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
            HasNormals = true;
            HasTangents = false;

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
                shader.SetVegetationMaterial(LightingDiffuse);

            shader.ImageTextureIsNight = NightTexture != null && NightTexture != SharedMaterialManager.MissingTexture && IsNightTimeOrUnderground();
            shader.ImageTexture = shader.ImageTextureIsNight ? NightTexture : Texture;
            shader.LightingSpecular = LightingSpecular;
            shader.LightingDiffuse = LightingDiffuse;
            shader.HasNormals = HasNormals;
            shader.HasTangents = HasTangents;

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
        protected Texture2D MetallicRoughnessTexture;
        protected Texture2D NormalTexture;
        protected Texture2D OcclusionTexture;
        protected Texture2D EmissiveTexture;
        protected Texture2D ClearcoatTexture;
        protected Texture2D ClearcoatRoughnessTexture;
        protected Texture2D ClearcoatNormalTexture;
        protected Texture2D SpecularTexture;
        protected Texture2D SpecularColorTexture;

        /// <summary>
        /// x: baseColor, y: roughness-metallic, z: normal, w: emissive
        /// </summary>
        protected readonly Vector4 TexCoords1;
        /// <summary>
        /// x: clearcoat, y: clearcoat-roughness, z: clearcoat-normal, w: occlusion
        /// </summary>
        protected readonly Vector4 TexCoords2;
        /// <summary>
        /// x: specular, y: specularColor, z: transmission, w: texture-packing
        /// </summary>
        protected readonly Vector4 TexCoords3;
        // Texture packing:
        // 0: occlusion (R), roughnessMetallic (GB) together, normal (RGB) separate, this is the standard
        // 1: roughnessMetallicOcclusion together, normal (RGB) separate
        // 2: normalRoughnessMetallic (RG+B+A) together, occlusion (R) separate
        // 3: occlusionRoughnessMetallic together, normal (RGB) separate
        // 4: roughnessMetallicOcclusion together, normal (RG) 2 channel separate
        // 5: occlusionRoughnessMetallic together, normal (RG) 2 channel separate

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

        bool EmissiveFollowsDayNightCycle = false;
        readonly Gltf GltfFile;
        readonly string ShapeFilePath;
        readonly string ShapeFileDir;
        // baseColor texture is 8 bit sRGB + A. Needs decoding to linear in the shader.
        // metallicRoughness texture: G = roughness, B = metalness, linear, may be > 8 bit.
        // normal texture is RGB linear, B should be >= 0.5. All channels need mapping from the [0.0..1.0] to the [-1.0..1.0] range, = sampledValue * 2.0 - 1.0
        // occlusion texture is linear R channel only, = 1.0 + strength * (sampledValue - 1.0)
        // emissive texture is 8 bit sRGB. Needs decoding to linear in the shader.
        // clearcoat texture is R channel only, linear.
        // clearcoatRoughness texture is G channel only, linear.
        // clearcoatNormal texture is RGB linear.
        // specular strength is A channel only, linear.
        // specularColor is storged in the RGB channels, encoded in sRGB.
        int BaseColorTextureIndex = -1;
        int MetallicRoughnessTextureIndex = -1;
        int NormalTextureIndex = -1;
        int OcclusionTextureIndex = -1;
        int EmissiveTextureIndex = -1;
        int ClearcoatTextureIndex = -1;
        int ClearcoatRoughnessTextureIndex = -1;
        int ClearcoatNormalTextureIndex = -1;
        int SpecularTextureIndex = -1;
        int SpecularColorTextureIndex = -1;

        Vector3[] SphericalHarmonics;

        protected readonly SamplerState SamplerStateMetallicRoughness;
        protected readonly SamplerState SamplerStateNormal;
        protected readonly SamplerState SamplerStateOcclusion;
        protected readonly SamplerState SamplerStateEmissive;
        protected readonly SamplerState SamplerStateClearcoat;
        protected readonly SamplerState SamplerStateClearcoatRoughness;
        protected readonly SamplerState SamplerStateClearcoatNormal;
        protected readonly SamplerState SamplerStateSpecular;
        protected readonly SamplerState SamplerStateSpecularColor;

        readonly Vector4[] MorphConfig = new Vector4[2];
        readonly Vector4[] MorphWeights = new Vector4[2];

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

        static readonly Dictionary<(TextureFilter, TextureAddressMode, TextureAddressMode), SamplerState> GltfSamplerStates = new Dictionary<(TextureFilter, TextureAddressMode, TextureAddressMode), SamplerState>()
        {
            [(TextureFilter.Linear, TextureAddressMode.Wrap, TextureAddressMode.Wrap)] = new SamplerState { Filter = TextureFilter.Linear, AddressU = TextureAddressMode.Wrap, AddressV = TextureAddressMode.Wrap, MaxAnisotropy = 16 },
        };

        static readonly string[] StandardTextureExtensionFilter = new[] { ".png", ".jpg", ".jpeg" };
        static readonly string[] DdsTextureExtensionFilter = new[] { ".dds" };

        public PbrMaterial(Viewer viewer, string materialUniqueId, SceneryMaterialOptions options, float mipMapBias, Gltf gltfFile)
            : base(viewer, null, options, mipMapBias, 0)
        {
            GltfFile = gltfFile;
            var info = materialUniqueId.Split('#');
            var materialRef = int.Parse(info[1].Trim('#'));
            ShapeFilePath = info[0].Trim('#');
            ShapeFileDir = Path.GetDirectoryName(ShapeFilePath);

            var material = gltfFile.Materials[materialRef];

            if (!(gltfFile.ExtensionsUsed?.Contains("KHR_materials_unlit") & material.Extensions?.ContainsKey("KHR_materials_unlit") ?? false))
                options |= SceneryMaterialOptions.Diffuse;

            switch (material.AlphaMode)
            {
                case glTFLoader.Schema.Material.AlphaModeEnum.BLEND:
                    options |= SceneryMaterialOptions.AlphaBlendingBlend;
                    break;
                case glTFLoader.Schema.Material.AlphaModeEnum.MASK:
                    options |= SceneryMaterialOptions.AlphaTest;
                    DefaultAlphaCutOff = (int)(material.AlphaCutoff * 255f);
                    break;
                case glTFLoader.Schema.Material.AlphaModeEnum.OPAQUE:
                default: break;
            }

            MaterialNormalTextureInfo msftNormalInfo = null;
            TextureInfo msftOrmInfo = null;
            TextureInfo msftRmoInfo = null;
            object extension = null;
            if (gltfFile.ExtensionsUsed?.Contains("MSFT_packing_normalRoughnessMetallic") & material.Extensions?.TryGetValue("MSFT_packing_normalRoughnessMetallic", out extension) ?? false)
                msftNormalInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<MSFT_packing_normalRoughnessMetallic>(extension.ToString(), GltfShape.PopulateDefaults)?.NormalRoughnessMetallicTexture;
            else if (gltfFile.ExtensionsUsed?.Contains("MSFT_packing_occlusionRoughnessMetallic") & material.Extensions?.TryGetValue("MSFT_packing_occlusionRoughnessMetallic", out extension) ?? false)
            {
                var ext = Newtonsoft.Json.JsonConvert.DeserializeObject<MSFT_packing_occlusionRoughnessMetallic>(extension.ToString(), GltfShape.PopulateDefaults);
                msftOrmInfo = ext?.OcclusionRoughnessMetallicTexture;
                msftRmoInfo = ext?.RoughnessMetallicOcclusionTexture;
                msftNormalInfo = ext?.NormalTexture;
            }

            var iblLightIndex = -1;
            if (material.Extras?.TryGetValue("OPENRAILS_material_image_based_light", out extension) ?? false && extension is int)
            {
                iblLightIndex = Convert.ToInt32(extension);
            }
            else if ((gltfFile.Scenes?.ElementAtOrDefault(gltfFile.Scene ?? 0)?.Extensions?.TryGetValue("EXT_lights_image_based", out extension) ?? false) && extension is EXT_lights_image_based lightImageBased)
            {
                iblLightIndex = lightImageBased.Light;
            }

            if (iblLightIndex >= 0)
            {
                if ((gltfFile.ExtensionsUsed?.Contains("EXT_lights_image_based") & GltfFile.Extensions?.TryGetValue("EXT_lights_image_based", out extension) ?? false) && extension is EXT_lights_image_based lightsImageBased)
                {
                    var lights = lightsImageBased.Lights;
                    var light = lights.ElementAtOrDefault(iblLightIndex);

                    if (light?.IrradianceCoefficients != null)
                    {
                        SphericalHarmonics = SphericalHarmonics ?? new Vector3[9];
                        var intensity = light.Intensity;
                        for (var i = 0; i < SphericalHarmonics.Length; i++)
                        {
                            var c = light.IrradianceCoefficients.ElementAtOrDefault(i);
                            SphericalHarmonics[i] = Vector3.Multiply(new Vector3(c?.ElementAtOrDefault(0) ?? 0, c?.ElementAtOrDefault(1) ?? 0, c?.ElementAtOrDefault(2) ?? 0), intensity);
                        }
                    }
                }
            }

            if (material.Extras?.TryGetValue("OPENRAILS_material_day_night_switch", out extension) ?? false && extension is bool)
                EmissiveFollowsDayNightCycle = (bool)extension;

            TexCoords3.W =
                msftOrmInfo != null ? msftNormalInfo != null ? 5 : 3 :
                msftRmoInfo != null ? msftNormalInfo != null ? 4 : 1 :
                                      msftNormalInfo != null ? 2 : 0;

            KHR_materials_clearcoat clearcoat = null;
            if (gltfFile.ExtensionsUsed?.Contains("KHR_materials_clearcoat") & material.Extensions?.TryGetValue("KHR_materials_clearcoat", out extension) ?? false)
                clearcoat = Newtonsoft.Json.JsonConvert.DeserializeObject<KHR_materials_clearcoat>(extension.ToString(), GltfShape.PopulateDefaults);

            KHR_materials_specular specular = null;
            if (gltfFile.ExtensionsUsed?.Contains("KHR_materials_specular") & material.Extensions?.TryGetValue("KHR_materials_specular", out extension) ?? false)
                specular = Newtonsoft.Json.JsonConvert.DeserializeObject<KHR_materials_specular>(extension.ToString(), GltfShape.PopulateDefaults);

            KHR_materials_ior ior = null;
            if (gltfFile.ExtensionsUsed?.Contains("KHR_materials_ior") & material.Extensions?.TryGetValue("KHR_materials_ior", out extension) ?? false)
                ior = Newtonsoft.Json.JsonConvert.DeserializeObject<KHR_materials_ior>(extension.ToString(), GltfShape.PopulateDefaults);

            var emissiveStrength = 1f;
            if (gltfFile.ExtensionsUsed?.Contains("KHR_materials_emissive_strength") & material.Extensions?.TryGetValue("KHR_materials_emissive_strength", out extension) ?? false)
                emissiveStrength = Newtonsoft.Json.JsonConvert.DeserializeObject<KHR_materials_emissive_strength>(extension.ToString(), GltfShape.PopulateDefaults)?.EmissiveStrength ?? 1;

            (TexCoords1.X, BaseColorTextureIndex, SamplerStateBaseColor) = GetTextureInfo(gltfFile, material.PbrMetallicRoughness?.BaseColorTexture, anisotropic: true);
            (TexCoords1.Y, MetallicRoughnessTextureIndex, SamplerStateMetallicRoughness) = GetTextureInfo(gltfFile, msftRmoInfo ?? msftOrmInfo ?? material.PbrMetallicRoughness?.MetallicRoughnessTexture);
            (TexCoords1.Z, NormalTextureIndex, SamplerStateNormal) = GetTextureInfo(gltfFile, msftNormalInfo ?? material.NormalTexture);
            (TexCoords1.W, EmissiveTextureIndex, SamplerStateEmissive) = GetTextureInfo(gltfFile, material.EmissiveTexture);
            (TexCoords2.W, OcclusionTextureIndex, SamplerStateOcclusion) = msftOrmInfo != null
                ? GetTextureInfo(gltfFile, msftOrmInfo)
                : GetTextureInfo(gltfFile, material.OcclusionTexture);
            (TexCoords2.X, ClearcoatTextureIndex, SamplerStateClearcoat) = GetTextureInfo(gltfFile, clearcoat?.ClearcoatTexture);
            (TexCoords2.Y, ClearcoatRoughnessTextureIndex, SamplerStateClearcoatRoughness) = GetTextureInfo(gltfFile, clearcoat?.ClearcoatRoughnessTexture);
            (TexCoords2.Z, ClearcoatNormalTextureIndex, SamplerStateClearcoatNormal) = GetTextureInfo(gltfFile, clearcoat?.ClearcoatNormalTexture);
            (TexCoords3.X, SpecularTextureIndex, SamplerStateSpecular) = GetTextureInfo(gltfFile, specular?.SpecularTexture);
            (TexCoords3.Y, SpecularColorTextureIndex, SamplerStateSpecularColor) = GetTextureInfo(gltfFile, specular?.SpecularColorTexture);

            if (NormalTextureIndex == -1)
                TexCoords1.Z = -1;
            if (ClearcoatNormalTextureIndex == -1)
                TexCoords2.Z = -1;

            BaseColorFactor = MemoryMarshal.Cast<float, Vector4>(material.PbrMetallicRoughness?.BaseColorFactor ?? new[] { 1f, 1f, 1f, 1f })[0];
            MetallicFactor = material.PbrMetallicRoughness?.MetallicFactor ?? 1f;
            RoughnessFactor = material.PbrMetallicRoughness?.RoughnessFactor ?? 1f;
            NormalScale = material.NormalTexture?.Scale ?? 1f;
            OcclusionStrength = material.OcclusionTexture?.Strength ?? 1;
            EmissiveFactor = Vector3.Min(MemoryMarshal.Cast<float, Vector3>(material.EmissiveFactor ?? new[] { 0f, 0f, 0f })[0], Vector3.One) * emissiveStrength;
            ClearcoatFactor = clearcoat?.ClearcoatFactor ?? 0;
            ClearcoatRoughnessFactor = clearcoat?.ClearcoatRoughnessFactor ?? 0;
            ClearcoatNormalScale = clearcoat?.ClearcoatNormalTexture?.Scale ?? 1f;
            SpecularFactor = specular?.SpecularFactor ?? 1f;
            SpecularColorFactor = MemoryMarshal.Cast<float, Vector3>(specular?.SpecularColorFactor ?? new[] { 1f, 1f, 1f })[0];
            Ior = ior?.Ior ?? 1.5f;

            if (SpecularFactor == 0)
                ClearcoatFactor = 0;

            if (Ior == 0)
                Ior = float.PositiveInfinity; // By the specification

            LightingDiffuse = (options & SceneryMaterialOptions.Diffuse) != 0 ? 1 : 0;
            HasNormals = (options & SceneryMaterialOptions.PbrHasNormals) != 0;
            HasTangents = (options & SceneryMaterialOptions.PbrHasTangents) != 0;

            RasterizerState = material.DoubleSided ? RasterizerState.CullNone :
                ((options & SceneryMaterialOptions.PbrCullClockWise) != 0) ? RasterizerState.CullClockwise : RasterizerState.CullCounterClockwise;

            if ((options & SceneryMaterialOptions.PbrCullClockWise) == 0)
            {
                NormalScale = -NormalScale;
                ClearcoatNormalScale = -ClearcoatNormalScale;
            }

            var shader = Viewer.MaterialManager.SceneryShader;

            if ((options & SceneryMaterialOptions.PbrHasMorphTargets) != 0)
                Technique = shader.Techniques["PbrMorphed"];
            else if ((options & SceneryMaterialOptions.PbrHasSkin) != 0)
                Technique = shader.Techniques["PbrSkinned"];
            else if ((options & SceneryMaterialOptions.PbrHasTexCoord1) != 0)
                Technique = shader.Techniques["PbrNormalMap"];
            else
                Technique = shader.Techniques["PbrBaseColorMap"];

            Options = options;

            SetupStates();
            SetupSorting();
        }

        public override bool GetBlending() => (Options & SceneryMaterialOptions.AlphaBlendingBlend) != 0;

        public void LoadTextures()
        {
            if (Texture == null || Texture == SharedMaterialManager.MissingTexture)
                Texture = GetTexture(BaseColorTextureIndex, SharedMaterialManager.WhiteTexture, true);

            MetallicRoughnessTexture = MetallicRoughnessTexture ?? GetTexture(MetallicRoughnessTextureIndex, SharedMaterialManager.WhiteTexture, false);
            NormalTexture = NormalTexture ?? GetTexture(NormalTextureIndex, SharedMaterialManager.WhiteTexture, false);
            OcclusionTexture = OcclusionTexture ?? GetTexture(OcclusionTextureIndex, SharedMaterialManager.WhiteTexture, false);
            EmissiveTexture = EmissiveTexture ?? GetTexture(EmissiveTextureIndex, SharedMaterialManager.WhiteTexture, true);
            ClearcoatTexture = ClearcoatTexture ?? GetTexture(ClearcoatTextureIndex, SharedMaterialManager.WhiteTexture, false);
            ClearcoatRoughnessTexture = ClearcoatRoughnessTexture ?? GetTexture(ClearcoatRoughnessTextureIndex, SharedMaterialManager.WhiteTexture, false);
            ClearcoatNormalTexture = ClearcoatNormalTexture ?? GetTexture(ClearcoatNormalTextureIndex, SharedMaterialManager.WhiteTexture, false);
            SpecularTexture = SpecularTexture ?? GetTexture(SpecularTextureIndex, SharedMaterialManager.WhiteTexture, false);
            SpecularColorTexture = SpecularColorTexture ?? GetTexture(SpecularColorTextureIndex, SharedMaterialManager.WhiteTexture, true);
        }

        Texture2D GetTexture(int? textureIndex, Texture2D defaultTexture, bool srgbColors)
        {
            if (textureIndex != null && textureIndex >= 0)
            {
                var texture = GltfFile.Textures[(int)textureIndex];
                var source = texture?.Source;
                var extensionFilter = StandardTextureExtensionFilter;
                object extension = null;
                if (GltfFile.ExtensionsUsed?.Contains("MSFT_texture_dds") & texture?.Extensions?.TryGetValue("MSFT_texture_dds", out extension) ?? false)
                {
                    var ext = Newtonsoft.Json.JsonConvert.DeserializeObject<MSFT_texture_dds>(extension.ToString(), GltfShape.PopulateDefaults);
                    source = ext?.Source ?? source;
                    extensionFilter = DdsTextureExtensionFilter;
                }
                if (source != null)
                {
                    var image = GltfFile.Images[(int)source];
                    if (image.Uri != null)
                    {
                        var imagePath = source != null ? Path.Combine(ShapeFileDir, Uri.UnescapeDataString(image.Uri)) : "";

                        // The standard accordance must be preserved, must not load a dds texture where only a jpg or png is allowed.
                        if (extensionFilter != null && !extensionFilter.Contains(Path.GetExtension(imagePath).ToLowerInvariant()))
                            return defaultTexture;

                        if (File.Exists(imagePath))
                        {
                            // We refuse to load textures containing "../" in their path, because although it would be possible,
                            // it would break compatibility with the existing glTF viewers, including the Windows 3D Viewer,
                            // the VS Code glTF Tools and the reference Khronos glTF-Sample-Viewer.
                            var strippedImagePath = imagePath.Replace("../", "").Replace(@"..\", "").Replace("..", "");
                            if (File.Exists(strippedImagePath))
                                return Viewer.TextureManager.Get(strippedImagePath, defaultTexture, srgb: srgbColors);

                            Trace.TraceWarning($"glTF: refusing to load texture {imagePath} in file {ShapeFilePath}, using \"../\" in the path is discouraged due to compatibility reasons.");
                            return SharedMaterialManager.MissingTexture;
                        }
                        else
                        {
                            try
                            {
                                using (var stream = glTFLoader.Interface.OpenImageFile(GltfFile, (int)source, ShapeFilePath))
                                {
                                    var texture2D = srgbColors
                                        ? Viewer.TextureManager.GetSrgbTexture(Viewer.GraphicsDevice, stream)
                                        : Texture2D.FromStream(Viewer.GraphicsDevice, stream);
                                    texture2D.Name = imagePath;
                                    return texture2D;
                                }
                            }
                            catch
                            {
                                Trace.TraceWarning($"glTF: missing texture {imagePath} in file {ShapeFilePath}");
                                return SharedMaterialManager.MissingTexture;
                            }
                        }
                    }
                    else if (image.BufferView != null)
                    {
                        try
                        {
                            using (var stream = glTFLoader.Interface.OpenImageFile(GltfFile, (int)source, ShapeFilePath))
                            {
                                var texture2D = srgbColors
                                    ? Viewer.TextureManager.GetSrgbTexture(Viewer.GraphicsDevice, stream)
                                    : Texture2D.FromStream(Viewer.GraphicsDevice, stream);
                                texture2D.Name = $"{ShapeFilePath}:{image.BufferView}";
                                return texture2D;
                            }
                        }
                        catch
                        {
                            Trace.TraceWarning($"glTF: missing image {image.BufferView} in file {ShapeFilePath}");
                            return SharedMaterialManager.MissingTexture;
                        }
                    }
                }
            }
            return defaultTexture;
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            var shader = Viewer.MaterialManager.SceneryShader;

            shader.CurrentTechnique = Technique;

            var lightsOn = !EmissiveFollowsDayNightCycle || IsNightTimeOrUnderground();

            shader.ImageTexture = Texture;
            shader.NormalTexture = NormalTexture;
            shader.EmissiveTexture = EmissiveTexture;
            shader.OcclusionTexture = OcclusionTexture;
            shader.MetallicRoughnessTexture = MetallicRoughnessTexture;

            shader.BaseColorFactor = BaseColorFactor;
            shader.EmissiveIorFactor = new Vector4(
                lightsOn && EmissiveFactor.LengthSquared() > 0 ? EmissiveFactor : Vector3.Zero,
                float.IsPositiveInfinity(Ior) ? 1 : Ior < 1 ? 0 : (float)Math.Pow((Ior - 1) / (Ior + 1), 2));
            shader.OcclusionFactor = new Vector4(OcclusionStrength, RoughnessFactor, MetallicFactor, NormalScale);
            shader.HasNormals = HasNormals;
            shader.HasTangents = HasTangents;
            shader.ClearcoatFactor = ClearcoatFactor;
            if (ClearcoatFactor > 0 && RenderProcess.CLEARCOAT)
            {
                shader.ClearcoatTexture = ClearcoatTexture;
                shader.ClearcoatRoughnessTexture = ClearcoatRoughnessTexture;
                shader.ClearcoatNormalTexture = ClearcoatNormalTexture;

                shader.ClearcoatRoughnessFactor = ClearcoatRoughnessFactor;
                shader.ClearcoatNormalScale = ClearcoatNormalScale;
            }
            shader.SpecularFactor = new Vector4(SpecularColorFactor, SpecularFactor);
            if (SpecularFactor > 0)
            {
                shader.SpecularTexture = SpecularTexture;
                shader.SpecularColorTexture = SpecularColorTexture;
            }
            shader.TextureCoordinates1 = TexCoords1;
            shader.TextureCoordinates2 = TexCoords2;
            shader.TextureCoordinates3 = TexCoords3;

            shader.LightingDiffuse = LightingDiffuse;

            if (lightsOn && SphericalHarmonics != null)
                Viewer.MaterialManager.SetSphericalHarmonics(SphericalHarmonics);

            var transparentPass = previousMaterial != null;

            shader.ReferenceAlpha = transparentPass ? ReferenceAlphaTransparentPass : DefaultAlphaCutOff;
            graphicsDevice.DepthStencilState = transparentPass ? DepthStencilStateTransparentPass : DepthStencilStateOpaquePass;
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

        static SamplerState GetNewSamplerState((TextureFilter filter, TextureAddressMode addressU, TextureAddressMode addressV) samplerAttributes)
        {
            return new SamplerState
            {
                Filter = samplerAttributes.filter,
                AddressU = samplerAttributes.addressU,
                AddressV = samplerAttributes.addressV,
                MaxAnisotropy = 16,
            };
        }

        (int texCoord, int textureIndex, SamplerState samplerState) GetTextureInfo(Gltf gltf, int? texCoord, int? index, bool anisotropic = false)
        {
            var texture = index ?? -1;
            if (texture == -1)
                texCoord = 0;
            var samplerState = GltfSamplerStates.ElementAtOrDefault(0).Value; // default to linear wrap
            if (gltf.Samplers?.ElementAtOrDefault(gltf.Textures?.ElementAtOrDefault(index ?? -1)?.Sampler ?? -1) is Sampler sampler)
            {
                var samplerStateTuple = (GetTextureFilter(sampler), GetTextureAddressMode(sampler.WrapS), GetTextureAddressMode(sampler.WrapT));
                // Currently it isn't possible to set a glTF to anisotropic filtering, so this is a hack against the spec:
                if (anisotropic && samplerStateTuple.Item1 == TextureFilter.Linear)
                    samplerStateTuple.Item1 = TextureFilter.Anisotropic;
                if (!GltfSamplerStates.TryGetValue(samplerStateTuple, out samplerState))
                    GltfSamplerStates.Add(samplerStateTuple, samplerState = GetNewSamplerState(samplerStateTuple));
            }
            return (texCoord ?? 0, texture, samplerState);
        }
        (int texCoord, int textureIndex, SamplerState samplerState) GetTextureInfo(Gltf gltf, TextureInfo textureInfo, bool anisotropic = false)
            => GetTextureInfo(gltf, textureInfo?.TexCoord, textureInfo?.Index, anisotropic);
        (int texCoord, int textureIndex, SamplerState samplerState) GetTextureInfo(Gltf gltf, MaterialNormalTextureInfo textureInfo)
            => GetTextureInfo(gltf, textureInfo?.TexCoord, textureInfo?.Index, false);
        (int texCoord, int textureIndex, SamplerState samplerState) GetTextureInfo(Gltf gltf, MaterialOcclusionTextureInfo textureInfo)
            => GetTextureInfo(gltf, textureInfo?.TexCoord, textureInfo?.Index, false);

        TextureAddressMode GetTextureAddressMode(Sampler.WrapTEnum wrapEnum) => GetTextureAddressMode((Sampler.WrapSEnum)wrapEnum);
        TextureAddressMode GetTextureAddressMode(Sampler.WrapSEnum wrapEnum)
        {
            //if (Shape.MsfsFlavoured) return TextureAddressMode.Clamp;
            switch (wrapEnum)
            {
                case Sampler.WrapSEnum.REPEAT: return TextureAddressMode.Wrap;
                case Sampler.WrapSEnum.CLAMP_TO_EDGE: return TextureAddressMode.Clamp;
                case Sampler.WrapSEnum.MIRRORED_REPEAT: return TextureAddressMode.Mirror;
                default: return TextureAddressMode.Wrap;
            }
        }

        TextureFilter GetTextureFilter(Sampler sampler)
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
                        {
                            var morphingData = gltfPrimitive.GetMorphingData();
                            MemoryMarshal.Cast<float, Vector4>(morphingData.Item1).CopyTo(MorphConfig);
                            MemoryMarshal.Cast<float, Vector4>(morphingData.Item2).CopyTo(MorphWeights);
                            shader.MorphConfig = MorphConfig;
                            shader.MorphWeights = MorphWeights;
                        }
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

            if (SphericalHarmonics != null)
                Viewer.MaterialManager.SetFrameSphericalHarmonics();
        }
    }

    public class BloomMaterial : Material
    {
        EffectPass ShaderPassExtract;
        EffectPass ShaderPassExtractLuminance;
        EffectPass ShaderPassDownsample;
        EffectPass ShaderPassUpsample;
        EffectPass ShaderPassUpsampleLuminance;
        EffectPass ShaderPassMerge;
        EffectPass ShaderPass;
        BloomShader Shader;
        VertexBuffer BloomVertexBuffer;
        bool UseLuminance = false;
        static readonly float[] Strengths = new[] { 0.5f, 1, 2, 1, 2 };
        static readonly float[] Radiuses = new[] { 1.0f, 2, 2, 4, 4 };
        
        float StrengthMultiplier = 1f;

        public enum Pass
        {
            Extract,
            DownSample,
            UpSample,
            Merge
        }

        readonly BlendState Merge = new BlendState()
        {
            ColorBlendFunction = BlendFunction.Add,
            ColorSourceBlend = Blend.BlendFactor,
            ColorDestinationBlend = Blend.BlendFactor,
            BlendFactor = new Color(1f, 1f, 1f)
        };

        public BloomMaterial(Viewer viewer) : base(viewer, null)
        {
            BloomVertexBuffer = new VertexBuffer(Viewer.RenderProcess.GraphicsDevice, typeof(VertexPositionTexture), 4, BufferUsage.WriteOnly);
            BloomVertexBuffer.SetData(new[] {
                new VertexPositionTexture(new Vector3(-1, +1, 0), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(-1, -1, 0), new Vector2(0, 1)),
                new VertexPositionTexture(new Vector3(+1, +1, 0), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(+1, -1, 0), new Vector2(1, 1)),
            });
            Shader = Viewer.MaterialManager.BloomShader;
        }

        public void SetState(GraphicsDevice graphicsDevice, Texture2D sourceTexture, Texture2D bloomTexture, RenderTarget2D targetTexture, Pass pass)
        {
            SetState(graphicsDevice, sourceTexture, targetTexture, pass);

            graphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Transparent, 1, 0);
            Shader.BloomTexture = bloomTexture;
        }

        public void SetState(GraphicsDevice graphicsDevice, Texture2D sourceTexture, RenderTarget2D targetTexture, Pass pass, float bloomStrength, float bloomRadius)
        {
            SetState(graphicsDevice, sourceTexture, targetTexture, pass);
            Shader.Radius = bloomRadius;
            Shader.Strength = bloomStrength;
        }

        public void SetState(GraphicsDevice graphicsDevice, Texture2D sourceTexture, RenderTarget2D targetTexture, Pass pass)
        {
            ShaderPassExtract = ShaderPassExtract ?? Shader.Techniques["Extract"].Passes[0];
            ShaderPassExtractLuminance = ShaderPassExtractLuminance ?? Shader.Techniques["ExtractLuminance"].Passes[0];
            ShaderPassDownsample = ShaderPassDownsample ?? Shader.Techniques["Downsample"].Passes[0];
            ShaderPassUpsample = ShaderPassUpsample ?? Shader.Techniques["Upsample"].Passes[0];
            ShaderPassUpsampleLuminance = ShaderPassUpsampleLuminance ?? Shader.Techniques["UpsampleLuminance"].Passes[0];
            ShaderPassMerge = ShaderPassMerge ?? Shader.Techniques["Merge"].Passes[0];

            switch (pass)
            {
                case Pass.Extract: Shader.CurrentTechnique = Shader.Techniques[UseLuminance ? "ExtractLuminance" : "Extract"]; ShaderPass = UseLuminance ? ShaderPassExtractLuminance : ShaderPassExtract; break;
                case Pass.UpSample: Shader.CurrentTechnique = Shader.Techniques[UseLuminance ? "UpsampleLuminance" : "Upsample"]; ShaderPass = UseLuminance ? ShaderPassUpsampleLuminance : ShaderPassUpsample; break;
                case Pass.DownSample: Shader.CurrentTechnique = Shader.Techniques["Downsample"]; ShaderPass = ShaderPassDownsample; break;
                case Pass.Merge: Shader.CurrentTechnique = Shader.Techniques["Merge"]; ShaderPass = ShaderPassMerge; break;
            }

            graphicsDevice.RasterizerState = RasterizerState.CullNone;
            graphicsDevice.BlendState = pass == Pass.UpSample ? BlendState.Additive : BlendState.Opaque;
            graphicsDevice.DepthStencilState = pass == Pass.Extract ? BloomStencilState : DepthStencilState.Default;

            Shader.ScreenTexture = sourceTexture;
            graphicsDevice.SetRenderTarget(targetTexture);
        }

        public void Render(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.SetVertexBuffer(BloomVertexBuffer);
            ShaderPass.Apply();
            graphicsDevice.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            graphicsDevice.BlendState = BlendState.NonPremultiplied;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
        }

        public void ApplyBloom(GraphicsDevice graphicsDevice, RenderTarget2D screen, RenderTarget2D mip0, RenderTarget2D mip1, RenderTarget2D mip2, RenderTarget2D mip3, RenderTarget2D mip4, RenderTarget2D mip5, RenderTarget2D result)
        {
            Shader.InverseResolution = new Vector2(1f / screen.Width, 1f / screen.Height);

            // Extracting is not needed with deferred bloom shading.
            // Extract the pixels to be bloomed
            //SetState(graphicsDevice, screen, mip0, Pass.Extract);
            //Render(graphicsDevice);

            SetState(graphicsDevice, mip0, mip1, Pass.DownSample);
            Render(graphicsDevice);

            Shader.InverseResolution *= 2;
            SetState(graphicsDevice, mip1, mip2, Pass.DownSample);
            Render(graphicsDevice);

            Shader.InverseResolution *= 2;
            SetState(graphicsDevice, mip2, mip3, Pass.DownSample);
            Render(graphicsDevice);

            Shader.InverseResolution *= 2;
            SetState(graphicsDevice, mip3, mip4, Pass.DownSample);
            Render(graphicsDevice);

            Shader.InverseResolution *= 2;
            SetState(graphicsDevice, mip4, mip5, Pass.DownSample);
            Render(graphicsDevice);

            SetState(graphicsDevice, mip5, mip4, Pass.UpSample, Strengths[4] * StrengthMultiplier, Radiuses[4]);
            Render(graphicsDevice);
            
            Shader.InverseResolution /= 2;
            SetState(graphicsDevice, mip4, mip3, Pass.UpSample, Strengths[3] * StrengthMultiplier, Radiuses[3]);
            Render(graphicsDevice);
            
            Shader.InverseResolution /= 2;
            SetState(graphicsDevice, mip3, mip2, Pass.UpSample, Strengths[2] * StrengthMultiplier, Radiuses[2]);
            Render(graphicsDevice);
            
            Shader.InverseResolution /= 2;
            SetState(graphicsDevice, mip2, mip1, Pass.UpSample, Strengths[1] * StrengthMultiplier, Radiuses[1]);
            Render(graphicsDevice);
            
            Shader.InverseResolution /= 2;
            SetState(graphicsDevice, mip1, mip0, Pass.UpSample, Strengths[0] * StrengthMultiplier, Radiuses[0]);
            Render(graphicsDevice);

            SetState(graphicsDevice, screen, mip0, result, Pass.Merge);
            Render(graphicsDevice);
        }

        public DepthStencilState BloomStencilState = new DepthStencilState()
        {
            StencilEnable = true,
            StencilMask = 0x08,
            StencilFunction = CompareFunction.Greater,
        };
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

        readonly Vector4[] MorphConfig = new Vector4[2];
        readonly Vector4[] MorphWeights = new Vector4[2];

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
                        {
                            var morphingData = gltfPrimitive.GetMorphingData();
                            MemoryMarshal.Cast<float, Vector4>(morphingData.Item1).CopyTo(MorphConfig);
                            MemoryMarshal.Cast<float, Vector4>(morphingData.Item2).CopyTo(MorphWeights);
                            shader.MorphConfig = MorphConfig;
                            shader.MorphWeights = MorphWeights;
                        }
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
