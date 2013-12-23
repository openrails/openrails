// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ORTS.Popups;

namespace ORTS
{
    [CallOnThread("Loader")]
    public class SharedTextureManager
    {
        readonly Viewer3D Viewer;
        readonly GraphicsDevice GraphicsDevice;
        Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();
        Dictionary<string, bool> TextureMarks;

        [CallOnThread("Render")]
        internal SharedTextureManager(Viewer3D viewer, GraphicsDevice graphicsDevice)
        {
            Viewer = viewer;
            GraphicsDevice = graphicsDevice;
        }

        public Texture2D Get(string path)
        {
            if (Thread.CurrentThread.Name != "Loader Process")
                Trace.TraceError("SharedTextureManager.Get incorrectly called by {0}; must be Loader Process or crashes will occur.", Thread.CurrentThread.Name);

            if (path == null || path == "")
                return SharedMaterialManager.MissingTexture;

            path = path.ToLowerInvariant();
            if (!Textures.ContainsKey(path))
            {
                try
                {
                    var texture = MSTS.ACEFile.Texture2DFromFile(GraphicsDevice, path);
                    Textures.Add(path, texture);
                    Thread.Sleep(Viewer.Settings.LoadingDelay);
                    return texture;
                }
                catch (InvalidDataException error)
                {
                    Trace.TraceWarning("Skipped texture with error: {1} in {0}", path, error.Message);
                    return SharedMaterialManager.MissingTexture;
                }
                catch (Exception error)
                {
                    if (File.Exists(path))
                        Trace.WriteLine(new FileLoadException(path, error));
                    else
                        Trace.TraceWarning("Ignored missing texture file {0}", path);
                    return SharedMaterialManager.MissingTexture;
                }
            }
            else
            {
                return Textures[path];
            }
        }

        public void Mark()
        {
            TextureMarks = new Dictionary<string, bool>(Textures.Count);
            foreach (var path in Textures.Keys)
                TextureMarks.Add(path, false);
        }

        public void Mark(Texture2D texture)
        {
            if (Textures.ContainsValue(texture))
                TextureMarks[Textures.First(kvp => kvp.Value == texture).Key] = true;
        }

        public void Sweep()
        {
            foreach (var path in TextureMarks.Where(kvp => !kvp.Value).Select(kvp => kvp.Key))
                Textures.Remove(path);
        }

        [CallOnThread("Updater")]
        public string GetStatus()
        {
            return String.Format("{0:F0} textures", Textures.Keys.Count);
        }
    }

    [CallOnThread("Loader")]
    public class SharedMaterialManager
    {
        readonly Viewer3D Viewer;
        Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        Dictionary<string, bool> MaterialMarks = new Dictionary<string, bool>();

        public readonly LightConeShader LightConeShader;
        public readonly LightGlowShader LightGlowShader;
        public readonly ParticleEmitterShader ParticleEmitterShader;
        public readonly PopupWindowShader PopupWindowShader;
        public readonly PrecipShader PrecipShader;
        public readonly SceneryShader SceneryShader;
        public readonly ShadowMapShader ShadowMapShader;
        public readonly SkyShader SkyShader;
        public readonly DebugShader DebugShader;

        public static Texture2D MissingTexture;

        [CallOnThread("Render")]
        public SharedMaterialManager(Viewer3D viewer)
        {
            Viewer = viewer;
            // TODO: Move to Loader process.
            LightConeShader = new LightConeShader(viewer.RenderProcess.GraphicsDevice);
            LightGlowShader = new LightGlowShader(viewer.RenderProcess.GraphicsDevice);
            ParticleEmitterShader = new ParticleEmitterShader(viewer.RenderProcess.GraphicsDevice);
            PopupWindowShader = new PopupWindowShader(viewer, viewer.RenderProcess.GraphicsDevice);
            PrecipShader = new PrecipShader(viewer.RenderProcess.GraphicsDevice);
            SceneryShader = new SceneryShader(viewer.RenderProcess.GraphicsDevice);
            var microtexPath = viewer.Simulator.RoutePath + @"\TERRTEX\microtex.ace";
            if (File.Exists(microtexPath))
            {
                try
                {
                    SceneryShader.OverlayTexture = MSTS.ACEFile.Texture2DFromFile(viewer.GraphicsDevice, microtexPath);
                }
                catch (InvalidDataException error)
                {
                    Trace.TraceWarning("Skipped texture with error: {1} in {0}", microtexPath, error.Message);
                }
                catch (Exception error)
                {
                    Trace.WriteLine(new FileLoadException(microtexPath, error));
                }
            }
            ShadowMapShader = new ShadowMapShader(viewer.RenderProcess.GraphicsDevice);
            SkyShader = new SkyShader(viewer.RenderProcess.GraphicsDevice);
            DebugShader = new DebugShader(viewer.RenderProcess.GraphicsDevice);

            // TODO: This should happen on the loader thread.
            MissingTexture = Texture2D.FromFile(viewer.RenderProcess.GraphicsDevice, Path.Combine(viewer.ContentPath, "blank.bmp"));
        }

        public Material Load(string materialName)
        {
            return Load(materialName, null, 0, 0);
        }

        public Material Load(string materialName, string textureName)
        {
            return Load(materialName, textureName, 0, 0);
        }

        public Material Load(string materialName, string textureName, int options)
        {
            return Load(materialName, textureName, options, 0);
        }

        public Material Load(string materialName, string textureName, int options, float mipMapBias)
        {
            if (textureName != null)
                textureName = textureName.ToLower();

            var materialKey = String.Format("{0}:{1}:{2}:{3}", materialName, textureName, options, mipMapBias);

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
                        Materials[materialKey] = new LightGlowMaterial(Viewer);
                        break;
                    case "PopupWindow":
                        Materials[materialKey] = new PopupWindowMaterial(Viewer);
                        break;
                    case "ParticleEmitter":
                        Materials[materialKey] = new ParticleEmitterMaterial(Viewer);
                        break;
                    case "Precip":
                        Materials[materialKey] = new PrecipMaterial(Viewer);
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
                    case "Sky":
                        Materials[materialKey] = new SkyMaterial(Viewer);
                        break;
                    case "SpriteBatch":
                        Materials[materialKey] = new SpriteBatchMaterial(Viewer);
                        break;
                    case "Terrain":
                        Materials[materialKey] = new TerrainMaterial(Viewer, textureName);
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
                    default:
                        Trace.TraceInformation("Skipped unknown material type {0}", materialName);
                        Materials[materialKey] = new YellowMaterial(Viewer);
                        break;
                }
            }
            return Materials[materialKey];
        }

        public void Mark()
        {
            MaterialMarks = new Dictionary<string, bool>(Materials.Count);
            foreach (var path in Materials.Keys)
                MaterialMarks.Add(path, false);
        }

        public void Mark(Material material)
        {
            if (Materials.ContainsValue(material))
                MaterialMarks[Materials.First(kvp => kvp.Value == material).Key] = true;
        }

        public void Sweep()
        {
            foreach (var path in MaterialMarks.Where(kvp => !kvp.Value).Select(kvp => kvp.Key))
                Materials.Remove(path);
        }

        [CallOnThread("Updater")]
        public string GetStatus()
        {
            return String.Format("{0:F0} materials", Materials.Keys.Count);
        }

        public static Color FogColor = new Color(110, 110, 110, 255);
        public static float FogCoeff = 0.75f;

        internal Vector3 sunDirection;
        bool lastLightState;
        double fadeStartTimer;
        float fadeDuration = -1;
        internal void UpdateShaders()
        {
            sunDirection = Viewer.World.Sky.solarDirection;
            SceneryShader.LightVector = sunDirection;

            // Headlight illumination
            if (Viewer.PlayerLocomotiveViewer != null
                && Viewer.PlayerLocomotiveViewer.lightDrawer != null
                && Viewer.PlayerLocomotiveViewer.lightDrawer.HasLightCone)
            {
                var lightDrawer = Viewer.PlayerLocomotiveViewer.lightDrawer;
                var lightState = lightDrawer.IsLightConeActive;
                if (lightState != lastLightState)
                {
                    if (lightDrawer.LightConeFadeIn > 0)
                    {
                        fadeStartTimer = Viewer.Simulator.GameTime;
                        fadeDuration = lightDrawer.LightConeFadeIn;
                    }
                    else if (lightDrawer.LightConeFadeOut > 0)
                    {
                        fadeStartTimer = Viewer.Simulator.GameTime;
                        fadeDuration = -lightDrawer.LightConeFadeOut;
                    }
                    lastLightState = lightState;
                }
                else if (!lastLightState && fadeDuration < 0 && Viewer.Simulator.GameTime > fadeStartTimer - fadeDuration)
                {
                    fadeDuration = 0;
                }
                if (!lightState && fadeDuration == 0)
                    // This occurs when switching locos and needs to be handled or we get lingering light.
                    SceneryShader.SetHeadlightOff();
                else
                    SceneryShader.SetHeadlight(ref lightDrawer.LightConePosition, ref lightDrawer.LightConeDirection, lightDrawer.LightConeDistance, lightDrawer.LightConeMinDotProduct, (float)(Viewer.Simulator.GameTime - fadeStartTimer), fadeDuration, ref lightDrawer.LightConeColor);
            }
            else
            {
                SceneryShader.SetHeadlightOff();
            }
            // End headlight illumination

            SceneryShader.Overcast = Viewer.World.Sky.overcast;
            SceneryShader.ViewerPos = Viewer.Camera.XNALocation(Viewer.Camera.CameraWorldLocation);

            if (Viewer.Settings.DistantMountains)
                SceneryShader.SetFog(Viewer.Settings.ViewingDistance * FogCoeff * 4, ref SharedMaterialManager.FogColor);
            else
                SceneryShader.SetFog(Viewer.Settings.ViewingDistance * FogCoeff / 2, ref SharedMaterialManager.FogColor);
        }
    }

    public abstract class Material
    {
        public readonly Viewer3D Viewer;
        readonly string Key;

        protected Material(Viewer3D viewer, string key)
        {
            Viewer = viewer;
            Key = key;
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
        public virtual TextureAddressMode GetShadowTextureAddressMode() { return TextureAddressMode.Wrap; }

        [CallOnThread("Loader")]
        public virtual void Mark()
        {
            Viewer.MaterialManager.Mark(this);
        }
    }

    public class EmptyMaterial : Material
    {
        public EmptyMaterial(Viewer3D viewer)
            : base(viewer, null)
        {
        }
    }

    public class BasicMaterial : Material
    {
        public BasicMaterial(Viewer3D viewer, string key)
            : base(viewer, key)
        {
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            foreach (var item in renderItems)
                item.RenderPrimitive.Draw(graphicsDevice);
        }
    }

    public class BasicBlendedMaterial : BasicMaterial
    {
        public BasicBlendedMaterial(Viewer3D viewer, string key)
            : base(viewer, key)
        {
        }

        public override bool GetBlending()
        {
            return true;
        }
    }

    public class SpriteBatchMaterial : BasicBlendedMaterial
    {
        public readonly SpriteBatch SpriteBatch;

        public SpriteBatchMaterial(Viewer3D viewer)
            : base(viewer, null)
        {
            SpriteBatch = new SpriteBatch(Viewer.RenderProcess.GraphicsDevice);
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            SpriteBatch.End();

            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = false;
            rs.DepthBufferEnable = true;
            rs.DestinationBlend = Blend.Zero;
            rs.SourceBlend = Blend.One;
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
    }

    public class SceneryMaterial : Material
    {
        readonly SceneryMaterialOptions Options;
        readonly float MipMapBias;
        readonly Texture2D Texture;
        readonly Texture2D NightTexture;
        byte AceAlphaBits;   // the number of bits in the ace file's alpha channel 
        IEnumerator<EffectPass> ShaderPassesDarkShade;
        IEnumerator<EffectPass> ShaderPassesFullBright;
        IEnumerator<EffectPass> ShaderPassesHalfBright;
        IEnumerator<EffectPass> ShaderPassesImage;
        IEnumerator<EffectPass> ShaderPassesVegetation;
        IEnumerator<EffectPass> ShaderPasses;

        public SceneryMaterial(Viewer3D viewer, string texturePath, SceneryMaterialOptions options, float mipMapBias)
            : base(viewer, String.Format("{0}:{1:X}:{2}", texturePath, options, mipMapBias))
        {
            Options = options;
            MipMapBias = mipMapBias;
            Texture = Viewer.TextureManager.Get(texturePath);
            if (!String.IsNullOrEmpty(texturePath) && (Options & SceneryMaterialOptions.NightTexture) != 0)
            {
                var nightTexturePath = Helpers.GetNightTextureFile(Viewer.Simulator, texturePath);
                if (!String.IsNullOrEmpty(nightTexturePath))
                    NightTexture = Viewer.TextureManager.Get(nightTexturePath.ToLower());
            }

            // Record the number of bits in the alpha channel of the original ace file
            if (Texture != null && Texture.Tag != null && Texture.Tag.GetType() == typeof(MSTS.AceInfo))
                AceAlphaBits = ((MSTS.AceInfo)Texture.Tag).AlphaBits;
            else
                AceAlphaBits = 0;

        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            var rs = graphicsDevice.RenderState;
            rs.CullMode = CullMode.CullCounterClockwiseFace;
            graphicsDevice.SamplerStates[0].MipMapLevelOfDetailBias = 0;

            var shader = Viewer.MaterialManager.SceneryShader;
            if (ShaderPassesDarkShade == null) ShaderPassesDarkShade = shader.Techniques["DarkShade"].Passes.GetEnumerator();
            if (ShaderPassesFullBright == null) ShaderPassesFullBright = shader.Techniques["FullBright"].Passes.GetEnumerator();
            if (ShaderPassesHalfBright == null) ShaderPassesHalfBright = shader.Techniques["HalfBright"].Passes.GetEnumerator();
            if (ShaderPassesImage == null) ShaderPassesImage = shader.Techniques[Viewer.Settings.ShaderModel >= 3 ? "ImagePS3" : "ImagePS2"].Passes.GetEnumerator();
            if (ShaderPassesVegetation == null) ShaderPassesVegetation = shader.Techniques["Vegetation"].Passes.GetEnumerator();

            shader.LightingDiffuse = (Options & SceneryMaterialOptions.Diffuse) != 0 ? 1 : 0;

            // Set up for alpha blending and alpha test 

            if (GetBlending())
            {
                // Skip blend for near transparent alpha's (eliminates sorting issues for many simple alpha'd textures )
                if (previousMaterial == null  // Search for opaque pixels in alpha blended polygons
                    && (Options & SceneryMaterialOptions.AlphaBlendingMask) != SceneryMaterialOptions.AlphaBlendingAdd)
                {
                    rs.AlphaBlendEnable = false;
                    shader.ReferenceAlpha = 250;
                    rs.DepthBufferWriteEnable = true;
                    rs.DepthBufferFunction = CompareFunction.LessEqual;
                }
                else // Alpha blended pixels only
                {
                    shader.ReferenceAlpha = 10;  // ie default lightcone's are 9 in full transparent areas

                    // Set up for blending
                    rs.AlphaBlendEnable = true;
                    rs.DepthBufferWriteEnable = false;
                    rs.SourceBlend = Blend.SourceAlpha;
                    if ((Options & SceneryMaterialOptions.AlphaBlendingMask) == SceneryMaterialOptions.AlphaBlendingBlend)
                    {
                        rs.DestinationBlend = Blend.InverseSourceAlpha; // AlphaBlend
                        rs.DepthBufferFunction = CompareFunction.Less; // To avoid processing already drawn opaque pixels
                    }
                    else
                    {
                        rs.DestinationBlend = Blend.One; // Additive
                        rs.DepthBufferFunction = CompareFunction.LessEqual;
                    }
                    rs.DestinationBlend = (Options & SceneryMaterialOptions.AlphaBlendingMask) == SceneryMaterialOptions.AlphaBlendingBlend ? Blend.InverseSourceAlpha : Blend.One;

                    rs.SeparateAlphaBlendEnabled = true;
                    rs.AlphaSourceBlend = Blend.Zero;
                    rs.AlphaDestinationBlend = Blend.One;
                }
            }
            else
            {
                rs.AlphaBlendEnable = false;
                if ((Options & SceneryMaterialOptions.AlphaTest) != 0)
                {
                    // Transparency testing is enabled
                    shader.ReferenceAlpha = 200;  // setting this to 128, chain link fences become solid at distance, at 200, they become
                }
                else
                {
                    shader.ReferenceAlpha = 0;
                }
            }


            switch (Options & SceneryMaterialOptions.ShaderMask)
            {
                case SceneryMaterialOptions.ShaderImage:
                    shader.CurrentTechnique = shader.Techniques[Viewer.Settings.ShaderModel >= 3 ? "ImagePS3" : "ImagePS2"];
                    ShaderPasses = ShaderPassesImage;
                    break;
                case SceneryMaterialOptions.ShaderDarkShade:
                    shader.CurrentTechnique = shader.Techniques["DarkShade"];
                    ShaderPasses = ShaderPassesDarkShade;
                    break;
                case SceneryMaterialOptions.ShaderHalfBright:
                    shader.CurrentTechnique = shader.Techniques["HalfBright"];
                    ShaderPasses = ShaderPassesHalfBright;
                    break;
                case SceneryMaterialOptions.ShaderFullBright:
                    shader.CurrentTechnique = shader.Techniques["FullBright"];
                    ShaderPasses = ShaderPassesFullBright;
                    break;
                case SceneryMaterialOptions.ShaderVegetation:
                    shader.CurrentTechnique = shader.Techniques["Vegetation"];
                    ShaderPasses = ShaderPassesVegetation;
                    break;
                default:
                    throw new InvalidDataException("Options has unexpected SceneryMaterialOptions.ShaderMask value.");
            }

            switch (Options & SceneryMaterialOptions.SpecularMask)
            {
                case SceneryMaterialOptions.Specular0:
                    shader.LightingSpecular = 0;
                    break;
                case SceneryMaterialOptions.Specular25:
                    shader.LightingSpecular = 25;
                    break;
                case SceneryMaterialOptions.Specular750:
                    shader.LightingSpecular = 750;
                    break;
                default:
                    throw new InvalidDataException("Options has unexpected SceneryMaterialOptions.SpecularMask value.");
            }

            graphicsDevice.SamplerStates[0].AddressU = graphicsDevice.SamplerStates[0].AddressV = GetShadowTextureAddressMode();

            if (NightTexture != null && Viewer.MaterialManager.sunDirection.Y < 0.0f)
            {
                shader.ImageTexture = NightTexture;
                shader.ImageTextureIsNight = true;
            }
            else
            {
                shader.ImageTexture = Texture;
                shader.ImageTextureIsNight = false;
            }

            if (MipMapBias < -1)
                graphicsDevice.SamplerStates[0].MipMapLevelOfDetailBias = -1;   // clamp to -1 max
            else
                graphicsDevice.SamplerStates[0].MipMapLevelOfDetailBias = MipMapBias;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.SceneryShader;
            var viewProj = XNAViewMatrix * XNAProjectionMatrix;

            shader.SetViewMatrix(ref XNAViewMatrix);
            shader.Begin();
            ShaderPasses.Reset();
            while (ShaderPasses.MoveNext())
            {
                ShaderPasses.Current.Begin();
                foreach (var item in renderItems)
                {
                    shader.SetMatrix(ref item.XNAMatrix, ref viewProj);
                    shader.ZBias = item.RenderPrimitive.ZBias;
                    shader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                ShaderPasses.Current.End();
            }
            shader.End();
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            var shader = Viewer.MaterialManager.SceneryShader;
            shader.ImageTextureIsNight = false;
            shader.LightingDiffuse = 1;
            shader.LightingSpecular = 0;
            shader.ReferenceAlpha = 0;

            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = false;
            rs.AlphaDestinationBlend = Blend.Zero;
            rs.AlphaSourceBlend = Blend.One;
            rs.DepthBufferFunction = CompareFunction.LessEqual;
            rs.DepthBufferWriteEnable = true;
            rs.DestinationBlend = Blend.Zero;
            rs.SeparateAlphaBlendEnabled = false;
            rs.SourceBlend = Blend.One;
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
            if (NightTexture != null && Viewer.MaterialManager.sunDirection.Y < 0.0f)
                return NightTexture;

            return Texture;
        }

        public override TextureAddressMode GetShadowTextureAddressMode()
        {
            switch (Options & SceneryMaterialOptions.TextureAddressModeMask)
            {
                case SceneryMaterialOptions.TextureAddressModeWrap:
                    return TextureAddressMode.Wrap;
                case SceneryMaterialOptions.TextureAddressModeMirror:
                    return TextureAddressMode.Mirror;
                case SceneryMaterialOptions.TextureAddressModeClamp:
                    return TextureAddressMode.Clamp;
                case SceneryMaterialOptions.TextureAddressModeBorder:
                    return TextureAddressMode.Border;
                default:
                    throw new InvalidDataException("Options has unexpected SceneryMaterialOptions.TextureAddressModeMask value.");
            }
        }

        public override void Mark()
        {
            Viewer.TextureManager.Mark(Texture);
            Viewer.TextureManager.Mark(NightTexture);
            base.Mark();
        }
    }

    public class TerrainMaterial : Material
    {
        readonly Texture2D PatchTexture;
        readonly Texture2D PatchTextureOverlay;
        IEnumerator<EffectPass> ShaderPasses;

        public TerrainMaterial(Viewer3D viewer, string terrainTexture)
            : base(viewer, terrainTexture)
        {
            var textures = terrainTexture.Split('\0');
            PatchTexture = Viewer.TextureManager.Get(textures[0]);
            PatchTextureOverlay = textures.Length > 1 ? Viewer.TextureManager.Get(textures[1]) : null;
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            var shader = Viewer.MaterialManager.SceneryShader;
            shader.CurrentTechnique = shader.Techniques[Viewer.Settings.ShaderModel >= 3 ? "TerrainPS3" : "TerrainPS2"];
            if (ShaderPasses == null) ShaderPasses = shader.Techniques[Viewer.Settings.ShaderModel >= 3 ? "TerrainPS3" : "TerrainPS2"].Passes.GetEnumerator();
            shader.ImageTexture = PatchTexture;
            shader.OverlayTexture = PatchTextureOverlay;

            var samplerState = graphicsDevice.SamplerStates[0];
            samplerState.AddressU = TextureAddressMode.Wrap;
            samplerState.AddressV = TextureAddressMode.Wrap;

            graphicsDevice.VertexDeclaration = TerrainPatch.SharedPatchVertexDeclaration;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.SceneryShader;
            var viewproj = XNAViewMatrix * XNAProjectionMatrix;

            shader.SetViewMatrix(ref XNAViewMatrix);
            shader.Begin();
            ShaderPasses.Reset();
            while (ShaderPasses.MoveNext())
            {
                ShaderPasses.Current.Begin();
                foreach (var item in renderItems)
                {
                    shader.SetMatrix(ref item.XNAMatrix, ref viewproj);
                    shader.ZBias = item.RenderPrimitive.ZBias;
                    shader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                ShaderPasses.Current.End();
            }
            shader.End();
        }

        public override void Mark()
        {
            Viewer.TextureManager.Mark(PatchTexture);
            Viewer.TextureManager.Mark(PatchTextureOverlay);
            base.Mark();
        }
    }

    public class TerrainSharedMaterial : TerrainMaterial
    {
        public TerrainSharedMaterial(Viewer3D viewer, string terrainTexture)
            : base(viewer, terrainTexture)
        {
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            base.SetState(graphicsDevice, previousMaterial);
            graphicsDevice.Indices = TerrainPatch.SharedPatchIndexBuffer;
        }
    }

    public class TerrainSharedDistantMountain : TerrainSharedMaterial
    {
        public TerrainSharedDistantMountain(Viewer3D viewer, string terrainTexture)
            : base(viewer, terrainTexture)
        {
        }

		public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
		{
			base.SetState(graphicsDevice, previousMaterial);

			var rs = graphicsDevice.RenderState;
			rs.CullMode = CullMode.None;
		}

		public override void ResetState(GraphicsDevice graphicsDevice)
		{
			base.ResetState(graphicsDevice);

			var rs = graphicsDevice.RenderState;
			rs.CullMode = CullMode.CullCounterClockwiseFace;
		}
    }

    public class SkyMaterial : Material
    {
        SkyShader SkyShader;
        Texture2D SkyTexture;
        Texture2D StarTextureN;
        Texture2D StarTextureS;
        Texture2D MoonTexture;
        Texture2D MoonMask;
        Texture2D CloudTexture;
        private Matrix XNAMoonMatrix;
        IEnumerator<EffectPass> ShaderPassesSky;
        IEnumerator<EffectPass> ShaderPassesMoon;
        IEnumerator<EffectPass> ShaderPassesClouds;

        public SkyMaterial(Viewer3D viewer)
            : base(viewer, null)
        {
            SkyShader = Viewer.MaterialManager.SkyShader;
            // TODO: This should happen on the loader thread.
            SkyTexture = Texture2D.FromFile(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "SkyDome1.png"));
            StarTextureN = Texture2D.FromFile(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "Starmap_N.png"));
            StarTextureS = Texture2D.FromFile(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "Starmap_S.png"));
            MoonTexture = Texture2D.FromFile(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "MoonMap.tga"));
            MoonMask = Texture2D.FromFile(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "MoonMask.png"));
            CloudTexture = Texture2D.FromFile(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "Clouds01.tga"));

            ShaderPassesSky = SkyShader.Techniques["Sky"].Passes.GetEnumerator();
            ShaderPassesMoon = SkyShader.Techniques["Moon"].Passes.GetEnumerator();
            ShaderPassesClouds = SkyShader.Techniques["Clouds"].Passes.GetEnumerator();

            SkyShader.SkyMapTexture = SkyTexture;
            SkyShader.StarMapTexture = StarTextureN;
            SkyShader.MoonMapTexture = MoonTexture;
            SkyShader.MoonMaskTexture = MoonMask;
            SkyShader.CloudMapTexture = CloudTexture;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            // Adjust Fog color for day-night conditions and overcast
            FogDay2Night(
                Viewer.World.Sky.solarDirection.Y,
                Viewer.World.Sky.overcast);
            SharedMaterialManager.FogCoeff = Viewer.World.Sky.fogCoeff;

            //if (Viewer.Settings.DistantMountains) SharedMaterialManager.FogCoeff *= (3 * (5 - Viewer.Settings.DistantMountainsFogValue) + 0.5f);

            if (Viewer.World.Sky.latitude > 0) // TODO: Use a dirty flag to determine if it is necessary to set the texture again
                SkyShader.StarMapTexture = StarTextureN;
            else
                SkyShader.StarMapTexture = StarTextureS;
            SkyShader.Random = Viewer.World.Sky.moonPhase; // Keep setting this before LightVector for the preshader to work correctly
            SkyShader.LightVector = Viewer.World.Sky.solarDirection;
            SkyShader.Time = (float)Viewer.Simulator.ClockTime / 100000;
            SkyShader.MoonScale = SkyConstants.skyRadius / 20;
            SkyShader.Overcast = Viewer.World.Sky.overcast;
            SkyShader.WindSpeed = Viewer.World.Sky.windSpeed;
            SkyShader.WindDirection = Viewer.World.Sky.windDirection; // Keep setting this after Time and Windspeed. Calculating displacement here.

            // Sky dome
            var rs = graphicsDevice.RenderState;
            rs.DepthBufferWriteEnable = false;

            SkyShader.CurrentTechnique = SkyShader.Techniques["Sky"];
            Viewer.World.Sky.SkyMesh.drawIndex = 1;

            Matrix viewXNASkyProj = XNAViewMatrix * Camera.XNASkyProjection;

            SkyShader.SetViewMatrix(ref XNAViewMatrix);
            SkyShader.Begin();
            ShaderPassesSky.Reset();
            while (ShaderPassesSky.MoveNext())
            {
                ShaderPassesSky.Current.Begin();
                foreach (var item in renderItems)
                {
                    Matrix wvp = item.XNAMatrix * viewXNASkyProj;
                    SkyShader.SetMatrix(ref wvp);
                    SkyShader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                ShaderPassesSky.Current.End();
            }
            SkyShader.End();

            // Moon
            SkyShader.CurrentTechnique = SkyShader.Techniques["Moon"];
            Viewer.World.Sky.SkyMesh.drawIndex = 2;

            rs.AlphaBlendEnable = true;
            rs.CullMode = CullMode.CullClockwiseFace;
            rs.DestinationBlend = Blend.InverseSourceAlpha;
            rs.SourceBlend = Blend.SourceAlpha;

            // Send the transform matrices to the shader
            int skyRadius = Viewer.World.Sky.SkyMesh.skyRadius;
            int cloudRadiusDiff = Viewer.World.Sky.SkyMesh.cloudDomeRadiusDiff;
            XNAMoonMatrix = Matrix.CreateTranslation(Viewer.World.Sky.lunarDirection * (skyRadius - (cloudRadiusDiff / 2)));
            Matrix XNAMoonMatrixView = XNAMoonMatrix * XNAViewMatrix;

            SkyShader.Begin();
            ShaderPassesMoon.Reset();
            while (ShaderPassesMoon.MoveNext())
            {
                ShaderPassesMoon.Current.Begin();
                foreach (var item in renderItems)
                {
                    Matrix wvp = item.XNAMatrix * XNAMoonMatrixView * Camera.XNASkyProjection;
                    SkyShader.SetMatrix(ref wvp);
                    SkyShader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                ShaderPassesMoon.Current.End();
            }
            SkyShader.End();

            // Clouds
            SkyShader.CurrentTechnique = SkyShader.Techniques["Clouds"];
            Viewer.World.Sky.SkyMesh.drawIndex = 3;

            rs.CullMode = CullMode.CullCounterClockwiseFace;

            SkyShader.Begin();
            ShaderPassesClouds.Reset();
            while (ShaderPassesClouds.MoveNext())
            {
                ShaderPassesClouds.Current.Begin();
                foreach (var item in renderItems)
                {
                    Matrix wvp = item.XNAMatrix * viewXNASkyProj;
                    SkyShader.SetMatrix(ref wvp);
                    SkyShader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                ShaderPassesClouds.Current.End();
            }
            SkyShader.End();
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = false;
            rs.DepthBufferWriteEnable = true;
            rs.DestinationBlend = Blend.Zero;
            rs.SourceBlend = Blend.One;
        }

        public override bool GetBlending()
        {
            return false;
        }

        const float nightStart = 0.15f; // The sun's Y value where it begins to get dark
        const float nightFinish = -0.05f; // The Y value where darkest fog color is reached and held steady
        
        // These should be user defined in the Environment files (future)
        static Vector3 startColor = new Vector3(0.647f, 0.651f, 0.655f); // Original daytime fog color - must be preserved!
        static Vector3 finishColor = new Vector3(0.05f, 0.05f, 0.05f); //Darkest nighttime fog color
        
        /// <summary>
        /// This function darkens the fog color as night begins to fall
        /// as well as with increasing overcast.
        /// </summary>
        /// <param name="sunHeight">The Y value of the sunlight vector</param>
        /// <param name="overcast">The amount of overcast</param>
        static void FogDay2Night(float sunHeight, float overcast)
        {
            Vector3 floatColor;

            if (sunHeight > nightStart)
                floatColor = startColor;
            else if (sunHeight < nightFinish)
                floatColor = finishColor;
            else
            {
                var amount = (sunHeight - nightFinish) / (nightStart - nightFinish);
                floatColor = Vector3.Lerp(finishColor, startColor, amount);
            }

            // Adjust fog color for overcast
            floatColor *= (1 - 0.5f * overcast);
            SharedMaterialManager.FogColor.R = (byte)(floatColor.X * 255);
            SharedMaterialManager.FogColor.G = (byte)(floatColor.Y * 255);
            SharedMaterialManager.FogColor.B = (byte)(floatColor.Z * 255);
        }

        public override void Mark()
        {
            Viewer.TextureManager.Mark(SkyTexture);
            Viewer.TextureManager.Mark(StarTextureN);
            Viewer.TextureManager.Mark(StarTextureS);
            Viewer.TextureManager.Mark(MoonTexture);
            Viewer.TextureManager.Mark(MoonMask);
            Viewer.TextureManager.Mark(CloudTexture);
            base.Mark();
        }
    }

    public class ParticleEmitterMaterial : Material
    {
        public Texture2D Texture;

        IEnumerator<EffectPass> ShaderPasses;

        public ParticleEmitterMaterial(Viewer3D viewer)
            : base(viewer, null)
        {
            ShaderPasses = Viewer.MaterialManager.ParticleEmitterShader.Techniques["ParticleEmitterTechnique"].Passes.GetEnumerator();
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            var shader = Viewer.MaterialManager.ParticleEmitterShader;
            shader.CurrentTime = (float)Viewer.Simulator.GameTime;
            shader.LightVector = Viewer.World.Sky.solarDirection;

            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = true;
            rs.DepthBufferWriteEnable = false;
            rs.DestinationBlend = Blend.InverseSourceAlpha;
            rs.SourceBlend = Blend.SourceAlpha;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.ParticleEmitterShader;

            ShaderPasses.Reset();
            shader.Begin();
            while (ShaderPasses.MoveNext())
            {
                ShaderPasses.Current.Begin();
                foreach (var item in renderItems)
                {
                    var emitter = (ParticleEmitter)item.RenderPrimitive;
                    shader.CameraTileXY = emitter.CameraTileXZ;
                    shader.EmitDirection = emitter.XNADirection;
                    shader.EmitSize = emitter.EmitterData.NozzleWidth;
                    shader.Texture = Texture;
                    shader.SetMatrix(item.XNAMatrix, ref XNAViewMatrix, ref XNAProjectionMatrix);
                    shader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                ShaderPasses.Current.End();
            }
            shader.End();
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = false;
            rs.DepthBufferWriteEnable = true;
            rs.DestinationBlend = Blend.Zero;
            rs.SourceBlend = Blend.One;
        }

        public override bool GetBlending()
        {
            return true;
        }

        public override void Mark()
        {
            Viewer.TextureManager.Mark(Texture);
            base.Mark();
        }
    }

    public class PrecipMaterial : Material
    {
        Texture2D RainTexture;
        Texture2D SnowTexture;
        IEnumerator<EffectPass> ShaderPasses;

        public PrecipMaterial(Viewer3D viewer)
            : base(viewer, null)
        {
            // TODO: This should happen on the loader thread.
            RainTexture = Texture2D.FromFile(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "Raindrop.png"));
            SnowTexture = Texture2D.FromFile(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "Snowflake.png"));
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            var shader = Viewer.MaterialManager.PrecipShader;
            shader.CurrentTechnique = shader.Techniques["RainTechnique"];
            if (ShaderPasses == null) ShaderPasses = shader.Techniques["RainTechnique"].Passes.GetEnumerator();
            shader.WeatherType = (int)Viewer.Simulator.Weather;
            shader.SunDirection = Viewer.World.Sky.solarDirection;
            shader.ViewportHeight = Viewer.DisplaySize.Y;
            shader.CurrentTime = (float)Viewer.Simulator.GameTime;
            switch (Viewer.Simulator.Weather)
            {
                case MSTS.WeatherType.Snow:
                    shader.PrecipTexture = SnowTexture;
                    break;
                case MSTS.WeatherType.Rain:
                    shader.PrecipTexture = RainTexture;
                    break;
                // Safe? or need a default here? If so, what?
            }

            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = true;
            rs.DepthBufferWriteEnable = false;
            rs.DestinationBlend = Blend.InverseSourceAlpha;
            rs.PointSpriteEnable = true;
            rs.SourceBlend = Blend.SourceAlpha;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            if (Viewer.Simulator.Weather == MSTS.WeatherType.Clear)
                return;

            var shader = Viewer.MaterialManager.PrecipShader;

            shader.Begin();
            ShaderPasses.Reset();
            while (ShaderPasses.MoveNext())
            {
                ShaderPasses.Current.Begin();
                foreach (var item in renderItems)
                {
                    shader.SetMatrix(item.XNAMatrix, ref XNAViewMatrix, ref Camera.XNASkyProjection);
                    shader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                ShaderPasses.Current.End();
            }
            shader.End();
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = false;
            rs.DepthBufferWriteEnable = true;
            rs.DestinationBlend = Blend.Zero;
            rs.PointSpriteEnable = false;
            rs.SourceBlend = Blend.One;
        }

        public override bool GetBlending()
        {
            return true;
        }

        public override void Mark()
        {
            Viewer.TextureManager.Mark(RainTexture);
            Viewer.TextureManager.Mark(SnowTexture);
            base.Mark();
        }
    }

    public class ForestMaterial : Material
    {
        readonly Texture2D TreeTexture;
        IEnumerator<EffectPass> ShaderPasses;

        public ForestMaterial(Viewer3D viewer, string treeTexture)
            : base(viewer, treeTexture)
        {
            TreeTexture = Viewer.TextureManager.Get(treeTexture);
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            var shader = Viewer.MaterialManager.SceneryShader;
            shader.CurrentTechnique = shader.Techniques["Forest"];
            if (ShaderPasses == null) ShaderPasses = shader.Techniques["Forest"].Passes.GetEnumerator();
            shader.ImageTexture = TreeTexture;
            shader.ReferenceAlpha = 200;

            graphicsDevice.SamplerStates[0].AddressU = graphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Clamp;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.SceneryShader;
            var viewproj = XNAViewMatrix * XNAProjectionMatrix;

            shader.SetViewMatrix(ref XNAViewMatrix);
            shader.Begin();
            ShaderPasses.Reset();
            while (ShaderPasses.MoveNext())
            {
                ShaderPasses.Current.Begin();
                foreach (var item in renderItems)
                {
                    shader.SetMatrix(ref item.XNAMatrix, ref viewproj);
                    shader.ZBias = item.RenderPrimitive.ZBias;
                    shader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                ShaderPasses.Current.End();
            }
            shader.End();
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            Viewer.MaterialManager.SceneryShader.ReferenceAlpha = 0;
        }

        public override Texture2D GetShadowTexture()
        {
            return TreeTexture;
        }

        public override void Mark()
        {
            Viewer.TextureManager.Mark(TreeTexture);
            base.Mark();
        }
    }

    public class LightGlowMaterial : Material
    {
        readonly Texture2D LightGlowTexture;

        public LightGlowMaterial(Viewer3D viewer)
            : base(viewer, null)
        {
            // TODO: This should happen on the loader thread.
            LightGlowTexture = Texture2D.FromFile(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "Lightglow.png"));
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            var shader = Viewer.MaterialManager.LightGlowShader;
            shader.CurrentTechnique = shader.Techniques["LightGlow"];
            shader.LightGlowTexture = LightGlowTexture;

            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = true;
            rs.DepthBufferWriteEnable = false;
            rs.DestinationBlend = Blend.InverseSourceAlpha;
            rs.SourceBlend = Blend.SourceAlpha;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.LightGlowShader;

            shader.Begin();
            foreach (var pass in shader.CurrentTechnique.Passes)
            {
                pass.Begin();
                foreach (var item in renderItems)
                {
                    Matrix wvp = item.XNAMatrix * XNAViewMatrix * Camera.XNASkyProjection;
                    shader.SetMatrix(ref wvp);
                    shader.SetFade(((LightMesh)item.RenderPrimitive).Fade);
                    shader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                pass.End();
            }
            shader.End();
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = false;
            rs.DepthBufferWriteEnable = true;
            rs.DestinationBlend = Blend.Zero;
            rs.SourceBlend = Blend.One;
        }

        public override bool GetBlending()
        {
            return true;
        }

        public override void Mark()
        {
            Viewer.TextureManager.Mark(LightGlowTexture);
            base.Mark();
        }
    }

    public class LightConeMaterial : Material
    {
        public LightConeMaterial(Viewer3D viewer)
            : base(viewer, null)
        {
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            var shader = Viewer.MaterialManager.LightConeShader;
            shader.CurrentTechnique = shader.Techniques["LightCone"];

            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = true;
            rs.DepthBufferWriteEnable = false;
            rs.StencilEnable = true;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.LightConeShader;

            shader.Begin();
            foreach (var pass in shader.CurrentTechnique.Passes)
            {
                pass.Begin();
                foreach (var item in renderItems)
                {
                    Matrix wvp = item.XNAMatrix * XNAViewMatrix * Camera.XNASkyProjection;
                    shader.SetMatrix(ref wvp);
                    shader.SetFade(((LightMesh)item.RenderPrimitive).Fade);
                    shader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                pass.End();
            }
            shader.End();
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = false;
            rs.DepthBufferWriteEnable = true;
            rs.StencilEnable = false;
        }
    }

    public class WaterMaterial : Material
    {
        readonly Texture2D WaterTexture;
        IEnumerator<EffectPass> ShaderPasses;

        public WaterMaterial(Viewer3D viewer, string waterTexturePath)
            : base(viewer, waterTexturePath)
        {
            WaterTexture = Viewer.TextureManager.Get(waterTexturePath);
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            var shader = Viewer.MaterialManager.SceneryShader;
            shader.CurrentTechnique = shader.Techniques[Viewer.Settings.ShaderModel >= 3 ? "ImagePS3" : "ImagePS2"];
            if (ShaderPasses == null) ShaderPasses = shader.Techniques[Viewer.Settings.ShaderModel >= 3 ? "ImagePS3" : "ImagePS2"].Passes.GetEnumerator();
            shader.ImageTexture = WaterTexture;

            var samplerState = graphicsDevice.SamplerStates[0];
            samplerState.AddressU = TextureAddressMode.Wrap;
            samplerState.AddressV = TextureAddressMode.Wrap;
            samplerState.MipMapLevelOfDetailBias = 0;

            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = true;
            rs.DestinationBlend = Blend.InverseSourceAlpha;
            rs.SourceBlend = Blend.SourceAlpha;

            graphicsDevice.VertexDeclaration = WaterTile.PatchVertexDeclaration;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.SceneryShader;
            var viewproj = XNAViewMatrix * XNAProjectionMatrix;

            shader.SetViewMatrix(ref XNAViewMatrix);
            shader.Begin();
            ShaderPasses.Reset();
            while (ShaderPasses.MoveNext())
            {
                ShaderPasses.Current.Begin();
                foreach (var item in renderItems)
                {
                    shader.SetMatrix(ref item.XNAMatrix, ref viewproj);
                    shader.ZBias = item.RenderPrimitive.ZBias;
                    shader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                ShaderPasses.Current.End();
            }
            shader.End();
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = false;
            rs.DestinationBlend = Blend.Zero;
            rs.SourceBlend = Blend.One;
        }

        public override bool GetBlending()
        {
            return true;
        }

        public override void Mark()
        {
            Viewer.TextureManager.Mark(WaterTexture);
            base.Mark();
        }
    }

    public class ShadowMapMaterial : Material
    {
        IEnumerator<EffectPass> ShaderPassesShadowMap;
        IEnumerator<EffectPass> ShaderPassesShadowMapForest;
        IEnumerator<EffectPass> ShaderPassesShadowMapBlocker;
        IEnumerator<EffectPass> ShaderPasses;
        IEnumerator<EffectPass> ShaderPassesBlur;
        VertexDeclaration BlurVertexDeclaration;
        VertexBuffer BlurVertexBuffer;

        public enum Mode
        {
            Normal,
            Forest,
            Blocker,
        }

        public ShadowMapMaterial(Viewer3D viewer)
            : base(viewer, null)
        {
            var shadowMapResolution = Viewer.Settings.ShadowMapResolution;
            BlurVertexDeclaration = new VertexDeclaration(Viewer.RenderProcess.GraphicsDevice, VertexPositionNormalTexture.VertexElements);
            BlurVertexBuffer = new VertexBuffer(Viewer.RenderProcess.GraphicsDevice, typeof(VertexPositionNormalTexture), 4, BufferUsage.WriteOnly);
            BlurVertexBuffer.SetData(new[] {
				new VertexPositionNormalTexture(new Vector3(-1, +1, 0), Vector3.Zero, new Vector2(0, 0)),
				new VertexPositionNormalTexture(new Vector3(+1, +1, 0), Vector3.Zero, new Vector2(shadowMapResolution, 0)),
				new VertexPositionNormalTexture(new Vector3(+1, -1, 0), Vector3.Zero, new Vector2(shadowMapResolution, shadowMapResolution)),
				new VertexPositionNormalTexture(new Vector3(-1, -1, 0), Vector3.Zero, new Vector2(0, shadowMapResolution)),
			});
        }

        public void SetState(GraphicsDevice graphicsDevice, Mode mode)
        {
            var shader = Viewer.MaterialManager.ShadowMapShader;
            shader.CurrentTechnique = shader.Techniques[mode == Mode.Forest ? "ShadowMapForest" : mode == Mode.Blocker ? "ShadowMapBlocker" : "ShadowMap"];
            if (ShaderPassesShadowMap == null) ShaderPassesShadowMap = shader.Techniques["ShadowMap"].Passes.GetEnumerator();
            if (ShaderPassesShadowMapForest == null) ShaderPassesShadowMapForest = shader.Techniques["ShadowMapForest"].Passes.GetEnumerator();
            if (ShaderPassesShadowMapBlocker == null) ShaderPassesShadowMapBlocker = shader.Techniques["ShadowMapBlocker"].Passes.GetEnumerator();
            ShaderPasses = mode == Mode.Forest ? ShaderPassesShadowMapForest : mode == Mode.Blocker ? ShaderPassesShadowMapBlocker : ShaderPassesShadowMap;

            var rs = graphicsDevice.RenderState;
            rs.CullMode = mode == Mode.Blocker ? CullMode.CullClockwiseFace : CullMode.CullCounterClockwiseFace;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.ShadowMapShader;
            var viewproj = XNAViewMatrix * XNAProjectionMatrix;
            var samplerState = graphicsDevice.SamplerStates[0];
            var lastSamplerState = samplerState.AddressU;

            shader.SetData(ref XNAViewMatrix);
            shader.Begin();
            ShaderPasses.Reset();
            while (ShaderPasses.MoveNext())
            {
                ShaderPasses.Current.Begin();
                foreach (var item in renderItems)
                {
                    var wvp = item.XNAMatrix * viewproj;
                    shader.SetData(ref wvp, item.Material.GetShadowTexture());
                    shader.CommitChanges();
                    var newSamplerState = item.Material.GetShadowTextureAddressMode();
                    if (lastSamplerState != newSamplerState)
                    {
                        samplerState.AddressU = samplerState.AddressV = newSamplerState;
                        lastSamplerState = newSamplerState;
                    }
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                ShaderPasses.Current.End();
            }
            shader.End();
        }

        public Texture2D ApplyBlur(GraphicsDevice graphicsDevice, Texture2D shadowMap, RenderTarget2D renderTarget, DepthStencilBuffer stencilBuffer, DepthStencilBuffer normalStencilBuffer)
        {
            var wvp = Matrix.Identity;

            var shader = Viewer.MaterialManager.ShadowMapShader;
            shader.CurrentTechnique = shader.Techniques["ShadowMapBlur"];
            shader.SetBlurData(ref wvp);
            if (ShaderPassesBlur == null) ShaderPassesBlur = shader.CurrentTechnique.Passes.GetEnumerator();

            var rs = graphicsDevice.RenderState;
            rs.CullMode = CullMode.None;
            rs.DepthBufferEnable = false;
            rs.DepthBufferWriteEnable = false;
            graphicsDevice.VertexDeclaration = BlurVertexDeclaration;
            graphicsDevice.Vertices[0].SetSource(BlurVertexBuffer, 0, VertexPositionNormalTexture.SizeInBytes);
            graphicsDevice.DepthStencilBuffer = stencilBuffer;

            shader.Begin();
            ShaderPassesBlur.Reset();
            while (ShaderPassesBlur.MoveNext())
            {
                graphicsDevice.SetRenderTarget(0, renderTarget);

                shader.SetBlurData(shadowMap);
                shader.CommitChanges();

                ShaderPassesBlur.Current.Begin();
                graphicsDevice.DrawPrimitives(PrimitiveType.TriangleFan, 0, 2);
                ShaderPassesBlur.Current.End();

                graphicsDevice.SetRenderTarget(0, null);
                shadowMap = renderTarget.GetTexture();
            }
            shader.End();

            rs.CullMode = CullMode.CullCounterClockwiseFace;
            rs.DepthBufferEnable = true;
            rs.DepthBufferWriteEnable = true;
            graphicsDevice.DepthStencilBuffer = normalStencilBuffer;

            return shadowMap;
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            var rs = graphicsDevice.RenderState;
            rs.CullMode = CullMode.CullCounterClockwiseFace;
        }
    }

    public class PopupWindowMaterial : Material
    {
        IEnumerator<EffectPass> ShaderPassesPopupWindow;
        IEnumerator<EffectPass> ShaderPassesPopupWindowGlass;
        IEnumerator<EffectPass> ShaderPasses;

        public PopupWindowMaterial(Viewer3D viewer)
            : base(viewer, null)
        {
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

            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = true;
            rs.CullMode = CullMode.None;
            rs.DepthBufferEnable = false;
            rs.DestinationBlend = Blend.InverseSourceAlpha;
            rs.SourceBlend = Blend.SourceAlpha;
        }

        public void Render(GraphicsDevice graphicsDevice, RenderPrimitive renderPrimitive, ref Matrix XNAWorldMatrix, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.PopupWindowShader;

            Matrix wvp = XNAWorldMatrix * XNAViewMatrix * XNAProjectionMatrix;
            shader.SetMatrix(XNAWorldMatrix, ref wvp);

            shader.Begin();
            ShaderPasses.Reset();
            while (ShaderPasses.MoveNext())
            {
                ShaderPasses.Current.Begin();
                renderPrimitive.Draw(graphicsDevice);
                ShaderPasses.Current.End();
            }
            shader.End();
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = false;
            rs.CullMode = CullMode.CullCounterClockwiseFace;
            rs.DepthBufferEnable = true;
            rs.DestinationBlend = Blend.Zero;
            rs.SourceBlend = Blend.One;
        }

        public override bool GetBlending()
        {
            return true;
        }
    }

    public class YellowMaterial : Material
    {
        static BasicEffect basicEffect;

        public YellowMaterial(Viewer3D viewer)
            : base(viewer, null)
        {
            if (basicEffect == null)
            {
                basicEffect = new BasicEffect(Viewer.RenderProcess.GraphicsDevice, null);
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
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            graphicsDevice.VertexDeclaration = WaterTile.PatchVertexDeclaration;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {

            basicEffect.View = XNAViewMatrix;
            basicEffect.Projection = XNAProjectionMatrix;

            basicEffect.Begin();
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Begin();

                foreach (var item in renderItems)
                {
                    basicEffect.World = item.XNAMatrix;
                    basicEffect.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                pass.End();
            }
            basicEffect.End();
        }
    }

    public class Label3DMaterial : SpriteBatchMaterial
    {
        public readonly Texture2D Texture;
        public readonly WindowTextFont Font;

        readonly List<Rectangle> TextBoxes = new List<Rectangle>();

        public Label3DMaterial(Viewer3D viewer)
            : base(viewer)
        {
            Texture = new Texture2D(SpriteBatch.GraphicsDevice, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
            Texture.SetData(new[] { Color.White });
            Font = Viewer.WindowManager.TextManager.Get("Arial", 12, System.Drawing.FontStyle.Bold, 1);
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            var scaling = (float)graphicsDevice.PresentationParameters.BackBufferHeight / Viewer.RenderProcess.GraphicsDeviceManager.PreferredBackBufferHeight;
            SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None, Matrix.CreateScale(scaling));

            var rs = graphicsDevice.RenderState;
            rs.DepthBufferEnable = true;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            TextBoxes.Clear();
            base.Render(graphicsDevice, renderItems, ref XNAViewMatrix, ref XNAProjectionMatrix);
        }

        public override bool GetBlending()
        {
            return false;
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

        public DebugNormalMaterial(Viewer3D viewer)
            : base(viewer, null)
        {
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

            shader.Begin();
            ShaderPassesGraph.Reset();
            while (ShaderPassesGraph.MoveNext())
            {
                ShaderPassesGraph.Current.Begin();
                foreach (var item in renderItems)
                {
                    shader.SetMatrix(ref item.XNAMatrix, ref viewproj);
                    shader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                ShaderPassesGraph.Current.End();
            }
            shader.End();
        }
    }
}
