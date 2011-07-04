// COPYRIGHT 2009, 2010, 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ORTS
{
	public class Materials
    {
        public static SceneryShader SceneryShader = null;
        public static SkyShader SkyShader = null;
        public static ParticleEmitterShader ParticleEmitterShader = null;
        public static PrecipShader PrecipShader = null;
        public static LightGlowShader LightGlowShader = null;
        public static LightConeShader LightConeShader = null;
		public static SpriteBatchMaterial SpriteBatchMaterial = null;
		public static SpriteBatchLineMaterial SpriteBatchLineMaterial = null;
		public static ActivityInforMaterial DrawInforMaterial = null;
		private static Dictionary<string, WaterMaterial> WaterMaterials = new Dictionary<string, WaterMaterial>();
        private static SkyMaterial SkyMaterial = null;
        private static PrecipMaterial PrecipMaterial = null;
        private static LightGlowMaterial LightGlowMaterial = null;
        private static LightConeMaterial LightConeMaterial = null;
        private static Dictionary<string, TerrainMaterial> TerrainMaterials = new Dictionary<string, TerrainMaterial>();
        private static Dictionary<string, ForestMaterial> ForestMaterials = new Dictionary<string, ForestMaterial>();
        private static Dictionary<string, SceneryMaterial> SceneryMaterials = new Dictionary<string, SceneryMaterial>();
		private static Dictionary<string, SignalLightMaterial> SignalLightMaterials = new Dictionary<string, SignalLightMaterial>();
        public static Texture2D MissingTexture = null;  // sub this when we are missing the required texture
		public static Material YellowMaterial = null;   // for debug and experiments
        public static ShadowMapMaterial ShadowMapMaterial = null;
        public static ShadowMapShader ShadowMapShader = null;
        public static Color FogColor = new Color(110, 110, 110, 255);
        public static float FogCoeff = 0.75f;
		public static PopupWindowMaterial PopupWindowMaterial = null;
		public static PopupWindowShader PopupWindowShader = null;
		private static bool IsInitialized = false;
        
        /// <summary>
        /// THREAD SAFETY:  XNA Content Manager is not thread safe and must only be called from the Game thread.
        /// ( per Shawn Hargreaves )
        /// </summary>
        /// <param name="renderProcess"></param>
		public static void Initialize(RenderProcess renderProcess)
        {
            SceneryShader = new SceneryShader(renderProcess.GraphicsDevice, renderProcess.Content);
            if (File.Exists(renderProcess.Viewer.Simulator.RoutePath + @"\TERRTEX\microtex.ace"))
                SceneryShader.NormalMap_Tex = MSTS.ACEFile.Texture2DFromFile(renderProcess.GraphicsDevice, renderProcess.Viewer.Simulator.RoutePath + @"\TERRTEX\microtex.ace");
            SkyShader = new SkyShader(renderProcess.GraphicsDevice, renderProcess.Content);
            ParticleEmitterShader = new ParticleEmitterShader(renderProcess.GraphicsDevice, renderProcess.Content);
            PrecipShader = new PrecipShader(renderProcess.GraphicsDevice, renderProcess.Content);
            LightGlowShader = new LightGlowShader(renderProcess.GraphicsDevice, renderProcess.Content);
            LightConeShader = new LightConeShader(renderProcess.GraphicsDevice, renderProcess.Content);
			SpriteBatchMaterial = new SpriteBatchMaterial(renderProcess);
			SpriteBatchLineMaterial = new SpriteBatchLineMaterial(renderProcess);
			DrawInforMaterial = new ActivityInforMaterial(renderProcess);
			// WaterMaterial here.
            SkyMaterial = new SkyMaterial(renderProcess);
            PrecipMaterial = new PrecipMaterial(renderProcess);
            LightGlowMaterial = new LightGlowMaterial(renderProcess);
            LightConeMaterial = new LightConeMaterial(renderProcess);
            MissingTexture = renderProcess.Content.Load<Texture2D>("blank");
            YellowMaterial = new YellowMaterial(renderProcess);
            ShadowMapMaterial = new ShadowMapMaterial(renderProcess);
            ShadowMapShader = new ShadowMapShader(renderProcess.GraphicsDevice, renderProcess.Content);
			PopupWindowMaterial = new PopupWindowMaterial(renderProcess);
			PopupWindowShader = new PopupWindowShader(renderProcess.GraphicsDevice, renderProcess.Content);
            IsInitialized = true;
        }

		public static Material Load(RenderProcess renderProcess, string materialName)
        {
            return Load(renderProcess, materialName, null, 0, 0);
        }
		public static Material Load(RenderProcess renderProcess, string materialName, string textureName)
        {
            return Load(renderProcess, materialName, textureName, 0, 0);
        }

		public static Material Load(RenderProcess renderProcess, string materialName, string textureName, int options)
        {
            return Load(renderProcess, materialName, textureName, options, 0);
        }

        public static Material Load(RenderProcess renderProcess, string materialName, string textureName, int options, float mipMapBias)
        {
            System.Diagnostics.Debug.Assert(IsInitialized, "Must initialize Materials before using.");
            if (!IsInitialized)             // this shouldn't happen, but if it does
            {
                Trace.TraceWarning("Program Bug: Must initialize Materials before using.");
                Initialize(renderProcess);  // warn, and do it now rather than fail
            }

            if (textureName != null)
                textureName = textureName.ToLower();

            switch (materialName)
            {
				case "SpriteBatch":
					return SpriteBatchMaterial;
				case "SpriteBatchLine":
					return SpriteBatchLineMaterial;
				case "DrawInforMaterial":
					return DrawInforMaterial;
				case "Terrain":
                    if (!TerrainMaterials.ContainsKey(textureName))
                    {
                        TerrainMaterial material = new TerrainMaterial(renderProcess, textureName);
                        TerrainMaterials.Add(textureName, material);
                        return material;
                    }
                    else
                    {
                        return TerrainMaterials[textureName];
                    }
                case "SceneryMaterial":
                    string key;
                    if (textureName != null)
                        key = options.ToString() + ":" + mipMapBias.ToString() + ":" + textureName;
                    else
                        key = options.ToString() + ":";
                    if (!SceneryMaterials.ContainsKey(key))
                    {
                        SceneryMaterial sceneryMaterial = new SceneryMaterial(renderProcess, textureName, options, mipMapBias);
                        SceneryMaterials.Add(key, sceneryMaterial);
                        return sceneryMaterial;
                    }
                    else
                    {
                        return SceneryMaterials[key];
                    }
                case "WaterMaterial":
					if (!WaterMaterials.ContainsKey(textureName))
					{
						WaterMaterial material = new WaterMaterial(renderProcess, textureName);
						WaterMaterials.Add(textureName, material);
						return material;
					}
					else
					{
						return WaterMaterials[textureName];
					}
                case "SkyMaterial":
                    return SkyMaterial;
                case "ParticleEmitterMaterial":
                        return new ParticleEmitterMaterial(renderProcess);
                case "PrecipMaterial":
                    return PrecipMaterial;
                case "LightGlowMaterial":
                    return LightGlowMaterial;
                case "LightConeMaterial":
                    return LightConeMaterial;
                case "ForestMaterial":
                    if (!ForestMaterials.ContainsKey(textureName))
                    {
                        ForestMaterial material = new ForestMaterial(renderProcess, textureName);
                        ForestMaterials.Add(textureName, material);
                        return material;
                    }
                    else
                    {
                        return ForestMaterials[textureName];
                    }
				case "SignalLightMaterial":
					if (!SignalLightMaterials.ContainsKey(textureName))
                    {
						var material = new SignalLightMaterial(renderProcess, textureName);
						SignalLightMaterials.Add(textureName, material);
                        return material;
                    }
                    else
                    {
						return SignalLightMaterials[textureName];
                    }
                default:
                    return Load(renderProcess, "SceneryMaterial");
            }
        }

        public static float ViewingDistance = 3000;  // TODO, this is awkward, viewer must set this to control fog

        static internal Vector3 sunDirection;
        static int lastLightState = 0;
		static double fadeStartTimer = 0;
		static float fadeDuration = -1;
		internal static void UpdateShaders(RenderProcess renderProcess, GraphicsDevice graphicsDevice)
		{
			sunDirection = renderProcess.Viewer.SkyDrawer.solarDirection;
			SceneryShader.LightVector = sunDirection;

			// Headlight illumination
            if (renderProcess.Viewer.PlayerLocomotiveViewer != null
                && renderProcess.Viewer.PlayerLocomotiveViewer.lightDrawer != null
                && renderProcess.Viewer.PlayerLocomotiveViewer.lightDrawer.HasLightCone)
            {
                var lightDrawer = renderProcess.Viewer.PlayerLocomotiveViewer.lightDrawer;
                var currentLightState = renderProcess.Viewer.PlayerLocomotive.Headlight;
                if (currentLightState != lastLightState)
                {
                    if (currentLightState > lastLightState)
                    {
                        if (lightDrawer.LightConeFadeIn > 0)
                        {
                            fadeStartTimer = renderProcess.Viewer.Simulator.ClockTime;
                            fadeDuration = lightDrawer.LightConeFadeIn;
                        }
                    }
                    else
                    {
                        if (lightDrawer.LightConeFadeOut > 0)
                        {
                            fadeStartTimer = renderProcess.Viewer.Simulator.ClockTime;
                            fadeDuration = -lightDrawer.LightConeFadeOut;
                        }
                    }
                    lastLightState = currentLightState;
                }
                if (currentLightState == 0 && lightDrawer.LightConeFadeOut == 0)
                    // This occurs when switching locos and needs to be handled or we get lingering light.
                    SceneryShader.SetHeadlightOff();
                else
                    SceneryShader.SetHeadlight(ref lightDrawer.LightConePosition, ref lightDrawer.LightConeDirection, lightDrawer.LightConeDistance, lightDrawer.LightConeMinDotProduct, (float)(renderProcess.Viewer.Simulator.ClockTime - fadeStartTimer), fadeDuration, ref lightDrawer.LightConeColor);
            }
            else
            {
                SceneryShader.SetHeadlightOff();
            }
			// End headlight illumination

			SceneryShader.Overcast = renderProcess.Viewer.SkyDrawer.overcast;
			SceneryShader.ViewerPos = renderProcess.Viewer.Camera.XNALocation(renderProcess.Viewer.Camera.CameraWorldLocation);

			SceneryShader.SetFog(ViewingDistance * 0.5f * FogCoeff, ref Materials.FogColor);
		}
    }

    public class SharedTextureManager
    {
        private static Dictionary<string, Texture2D> SharedTextures = new Dictionary<string, Texture2D>();

        public static Texture2D Get(GraphicsDevice device, string path)
        {
            if (path == null)
                return Materials.MissingTexture;

            if (!SharedTextures.ContainsKey(path))
            {
                try
                {
                    Texture2D texture = MSTS.ACEFile.Texture2DFromFile(device, path);
                    SharedTextures.Add(path, texture);
                    return texture;
                }
                catch (Exception error)
                {
					Trace.TraceInformation(path);
					Trace.WriteLine(error);
					return Materials.MissingTexture;
                }
            }
            else
            {
                return SharedTextures[path];
            }
        }
    }

	public abstract class Material
	{
		readonly string Key;

		protected Material(string key)
		{
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

		public virtual bool GetBlending(RenderPrimitive renderPrimitive) { return false; }
		public virtual Texture2D GetShadowTexture(RenderPrimitive renderPrimitive) { return null; }
	}

	public class EmptyMaterial : Material
	{
		public EmptyMaterial()
			: base(null)
		{
		}
	}

    public class BasicMaterial : Material
    {
        public BasicMaterial(string key)
            : base(key)
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
        public BasicBlendedMaterial(string key)
            : base(key)
        {
        }

        public override bool GetBlending(RenderPrimitive renderPrimitive)
        {
            return true;
        }
    }

    public class SpriteBatchMaterial : BasicBlendedMaterial
    {
        public SpriteBatch SpriteBatch;

        public SpriteBatchMaterial(RenderProcess renderProcess)
            : base(null)
        {
            SpriteBatch = new SpriteBatch(renderProcess.GraphicsDevice);
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
            rs.AlphaFunction = CompareFunction.Always;
            rs.AlphaTestEnable = false;
            rs.DepthBufferEnable = true;
            rs.DestinationBlend = Blend.Zero;
            rs.SourceBlend = Blend.One;
        }
	}

	//Material to draw lines, which needs to open z-buffer
	public class SpriteBatchLineMaterial : Material
	{
		public SpriteBatch SpriteBatch;
		public Texture2D Texture;
		public RenderProcess RenderProcess;  // for diagnostics only

		public SpriteBatchLineMaterial(RenderProcess renderProcess)
			: base(null)
		{
			RenderProcess = renderProcess;
			SpriteBatch = new SpriteBatch(renderProcess.GraphicsDevice);
			Texture = new Texture2D(SpriteBatch.GraphicsDevice, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
			Texture.SetData(new[] { Color.White });
		}

		public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
		{
			float scaling = (float)graphicsDevice.PresentationParameters.BackBufferHeight / RenderProcess.GraphicsDeviceManager.PreferredBackBufferHeight;
			Vector3 screenScaling = new Vector3(scaling);
			Matrix xForm = Matrix.CreateScale(screenScaling);
			SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.SaveState, xForm);
			SpriteBatch.GraphicsDevice.RenderState.DepthBufferEnable = true;//want to line to have z-buffer effect
		}

		public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
		{
			foreach (var item in renderItems)
			{
				item.RenderPrimitive.Draw(graphicsDevice);
			}
		}

		public override void ResetState(GraphicsDevice graphicsDevice)
		{
			SpriteBatch.End();//DepthBufferEnable will be restored to previous state
		}

		public override bool GetBlending(RenderPrimitive renderPrimitive)
		{
			return true;
		}
	}

	public class SceneryMaterial : Material
    {
		readonly int Options = 0;
		readonly float MipMapBias = 0;
        readonly SceneryShader SceneryShader;
		readonly Texture2D Texture;
		readonly Texture2D nightTexture = null;
		bool isNightEnabled = false;
		readonly public RenderProcess RenderProcess;  // for diagnostics only
		IEnumerator<EffectPass> ShaderPassesDarkShade;
		IEnumerator<EffectPass> ShaderPassesFullBright;
		IEnumerator<EffectPass> ShaderPassesHalfBright;
		IEnumerator<EffectPass> ShaderPassesImage;
		IEnumerator<EffectPass> ShaderPassesVegetation;
		IEnumerator<EffectPass> ShaderPasses;

		public SceneryMaterial(RenderProcess renderProcess, string texturePath, int options, float mipMapBias)
			: base(String.Format("{0}:{1:X}:{2}", texturePath, options, mipMapBias))
		{
            RenderProcess = renderProcess;
            SceneryShader = Materials.SceneryShader;
            Options = options;
            MipMapBias = mipMapBias;
			// note: texturePath may be null if the object isn't textured, results in default 'blank texture' being loaded.
            Texture = SharedTextureManager.Get(renderProcess.GraphicsDevice, texturePath);
            if (texturePath != null)
            {
                int idx = texturePath.LastIndexOf("textures");
                if (idx > 0)
                {
                    string strTexname;
                    string nightTexturePath = texturePath.Remove(idx + 9);
                    idx = texturePath.LastIndexOf(@"\");
                    strTexname = texturePath.Remove(0, idx);
                    nightTexturePath += "night";
                    nightTexturePath += strTexname;
                    if (File.Exists(nightTexturePath))
                        nightTexture = SharedTextureManager.Get(renderProcess.GraphicsDevice, nightTexturePath);
                }
            }
        }

		public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
		{
            var rs = graphicsDevice.RenderState;
            rs.CullMode = CullMode.CullCounterClockwiseFace;
			graphicsDevice.SamplerStates[0].MipMapLevelOfDetailBias = 0;

			if (ShaderPassesDarkShade == null) ShaderPassesDarkShade = SceneryShader.Techniques["DarkShade"].Passes.GetEnumerator();
			if (ShaderPassesFullBright == null) ShaderPassesFullBright = SceneryShader.Techniques["FullBright"].Passes.GetEnumerator();
			if (ShaderPassesHalfBright == null) ShaderPassesHalfBright = SceneryShader.Techniques["HalfBright"].Passes.GetEnumerator();
			if (ShaderPassesImage == null) ShaderPassesImage = SceneryShader.Techniques[RenderProcess.Viewer.Settings.ShaderModel >= 3 ? "ImagePS3" : "ImagePS2"].Passes.GetEnumerator();
			if (ShaderPassesVegetation == null) ShaderPassesVegetation = SceneryShader.Techniques["Vegetation"].Passes.GetEnumerator();

			/////////////// MATERIAL OPTIONS //////////////////
			//
			// Material options are specified in a 32-bit int named "options"
			// Following are the bit assignments:
			// (name, dec value, hex, bits)
			// 
			// SHADERS bits 0 through 3 (allow for future shaders)
			// Diffuse            1     0x0001      0000 0000 0000 0001
			// Tex                2     0x0002      0000 0000 0000 0010
			// TexDiff            3     0x0003      0000 0000 0000 0011
			// BlendATex          4     0x0004      0000 0000 0000 0100
			// AddAtex            5     0x0005      0000 0000 0000 0101
			// BlendATexDiff      6     0x0006      0000 0000 0000 0110
			// AddATexDiff        7     0x0007      0000 0000 0000 0111
			// AND mask          15     0x000f      0000 0000 0000 1111
			//
			// LIGHTING  bits 4 through 7 ( >> 4 )
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
			// ALPHA TEST bit 8 ( >> 8 )
			// None               0     0x0000      0000 0000 0000 0000
			// Trans            256     0x0100      0000 0001 0000 0000
			// AND mask         256     0x0100      0000 0001 0000 0000
			//
			// Z BUFFER bits 9 and 10 ( >> 9 )
			// None               0     0x0000      0000 0000 0000 0000
			// Normal           512     0x0200      0000 0010 0000 0000
			// Write Only      1024     0x0400      0000 0100 0000 0000
			// Test Only       1536     0x0600      0000 0110 0000 0000
			// AND mask        1536     0x0600      0000 0110 0000 0000
			//
			// TEXTURE ADDRESS MODE bits 11 and 12 ( >> 11 )
			// Wrap               0     0x0000      0000 0000 0000 0000             
			// Mirror          2048     0x0800      0000 1000 0000 0000
			// Clamp           4096     0x1000      0001 0000 0000 0000
			// Border          6144     0x1800      0001 1000 0000 0000
			// AND mask        6144     0x1800      0001 1000 0000 0000
			//
			// NIGHT TEXTURE bit 13 ( >> 13 )
			// Disabled           0     0x0000      0000 0000 0000 0000
			// Enabled         8192     0x2000      0010 0000 0000 0000
			//

			var shaders = Options & 0x000f;
			var lighting = (Options & 0x00f0) >> 4;
			var alphaTest = (Options & 0x0100) >> 8;
			var textureAddressMode = (Options & 0x1800) >> 11;

			switch (shaders)
			{
				case 1: // Diffuse
				case 3: // TexDiff
				case 6: // BlendATexDiff
				case 7: // AddATexDiff
					SceneryShader.LightingDiffuse = 1;
					break;
				default:
					SceneryShader.LightingDiffuse = 0;
					break;
			}

			switch (lighting)
			{
				case 1: // DarkShade
					SceneryShader.CurrentTechnique = SceneryShader.Techniques["DarkShade"];
					ShaderPasses = ShaderPassesDarkShade;
					break;
				case 2: // OptHalfBright
					SceneryShader.CurrentTechnique = SceneryShader.Techniques["HalfBright"];
					ShaderPasses = ShaderPassesHalfBright;
					break;
				case 3: // Cruciform
				case 4: // CruciformLong
					SceneryShader.CurrentTechnique = SceneryShader.Techniques["Vegetation"];
					ShaderPasses = ShaderPassesVegetation;
					break;
				case 5: // OptFullBright
					SceneryShader.CurrentTechnique = SceneryShader.Techniques["FullBright"];
					ShaderPasses = ShaderPassesFullBright;
					break;
				default:
					SceneryShader.CurrentTechnique = SceneryShader.Techniques[RenderProcess.Viewer.Settings.ShaderModel >= 3 ? "ImagePS3" : "ImagePS2"];
					ShaderPasses = ShaderPassesImage;
					break;
			}

			switch (lighting)
			{
				case 6: // OptSpecular750
					SceneryShader.LightingSpecular = 750;
					break;
				case 7: // OptSpecular25
					SceneryShader.LightingSpecular = 25;
					break;
				case 8: // OptSpecular0
				default:
					SceneryShader.LightingSpecular = 0;
					break;
			}

			if (alphaTest != 0)
			{
				// Transparency test
				rs.AlphaTestEnable = true;
				rs.AlphaFunction = CompareFunction.GreaterEqual;        // if alpha > reference, then skip processing this pixel
				rs.ReferenceAlpha = 200;  // setting this to 128, chain link fences become solid at distance, at 200, they become
			}
			else if (shaders >= 4)
			{
				// Translucency
				rs.AlphaTestEnable = true;
				rs.AlphaFunction = CompareFunction.GreaterEqual;
				rs.ReferenceAlpha = 10;  // ie lightcode is 9 in full transparent areas
				rs.AlphaBlendEnable = true;
				rs.SourceBlend = Blend.SourceAlpha;
				rs.DestinationBlend = Blend.InverseSourceAlpha;
				rs.SeparateAlphaBlendEnabled = true;
				rs.AlphaSourceBlend = Blend.Zero;
				rs.AlphaDestinationBlend = Blend.One;
			}

			// Texture addressing
			switch (textureAddressMode)
			{
				case 0: // wrap
					graphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
					graphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
					break;
				case 1: // mirror
					graphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Mirror;
					graphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Mirror;
					break;
				case 2: // clamp
					graphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Clamp;
					graphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Clamp;
					break;
				case 3: // border
					graphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Border;
					graphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Border;
					break;
			}

			// Night texture toggle
			if ((Options & 0x2000) >> 13 == 1)
				isNightEnabled = true;

			if (Materials.sunDirection.Y < 0.0f && nightTexture != null && isNightEnabled) // Night
			{
				SceneryShader.ImageMap_Tex = nightTexture;
				SceneryShader.IsNight_Tex = true;
			}
			else
			{
				SceneryShader.ImageMap_Tex = Texture;
				SceneryShader.IsNight_Tex = false;
			}

			SceneryShader.Apply();

			if (MipMapBias < -1)
				graphicsDevice.SamplerStates[0].MipMapLevelOfDetailBias = -1;   // clamp to -1 max
			else
				graphicsDevice.SamplerStates[0].MipMapLevelOfDetailBias = MipMapBias;
		}

		public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            Matrix viewProj = XNAViewMatrix * XNAProjectionMatrix;

            // With the GPU configured, now we can draw the primitive
            SceneryShader.Begin();
			ShaderPasses.Reset();
			while (ShaderPasses.MoveNext())
            {
				ShaderPasses.Current.Begin();

                foreach (RenderItem item in renderItems)
                {
                    SceneryShader.SetMatrix(ref item.XNAMatrix, ref XNAViewMatrix, ref viewProj);
                    SceneryShader.ZBias = item.RenderPrimitive.ZBias;
                    SceneryShader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
				ShaderPasses.Current.End();
            }
            SceneryShader.End();
        }

		public override void ResetState(GraphicsDevice graphicsDevice)
		{
			SceneryShader.IsNight_Tex = false;
			SceneryShader.LightingDiffuse = 1;
			SceneryShader.LightingSpecular = 0;
			SceneryShader.Apply();

            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = false;
			rs.AlphaDestinationBlend = Blend.Zero;
			rs.AlphaFunction = CompareFunction.Always;
			rs.AlphaSourceBlend = Blend.One;
			rs.AlphaTestEnable = false;
			rs.DestinationBlend = Blend.Zero;
			rs.ReferenceAlpha = 0;
			rs.SeparateAlphaBlendEnabled = false;
			rs.SourceBlend = Blend.One;
		}

		public override bool GetBlending(RenderPrimitive renderPrimitive)
		{
			// Transparency test
			int alphaTest = (Options & 0x0100) >> 8;
			if (alphaTest != 0)
				return false;

			// Translucency
			int shaders = Options & 0x000f;
			if (alphaTest == 0 && shaders >= 4)
				return true;

			return false;
		}

		public override Texture2D GetShadowTexture(RenderPrimitive renderPrimitive)
		{
			if (Materials.sunDirection.Y < 0.0f && nightTexture != null && isNightEnabled) // Night
				return nightTexture;
			
			return Texture;
		}
	}

	public class TerrainMaterial : Material
    {
        readonly SceneryShader SceneryShader;
        readonly Texture2D PatchTexture;
        readonly public RenderProcess RenderProcess;  // for diagnostics only
		IEnumerator<EffectPass> ShaderPasses;

        public TerrainMaterial(RenderProcess renderProcess, string terrainTexture)
			: base(terrainTexture)
		{
            SceneryShader = Materials.SceneryShader;
            PatchTexture = SharedTextureManager.Get(renderProcess.GraphicsDevice, terrainTexture);
            RenderProcess = renderProcess;
        }

		public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
		{
			SceneryShader.CurrentTechnique = SceneryShader.Techniques[RenderProcess.Viewer.Settings.ShaderModel >= 3 ? "TerrainPS3" : "TerrainPS2"];
			if (ShaderPasses == null) ShaderPasses = SceneryShader.Techniques[RenderProcess.Viewer.Settings.ShaderModel >= 3 ? "TerrainPS3" : "TerrainPS2"].Passes.GetEnumerator();
			SceneryShader.ImageMap_Tex = PatchTexture;

			graphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
			graphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
			graphicsDevice.SamplerStates[0].MipMapLevelOfDetailBias = 0;

            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = false;
			rs.AlphaTestEnable = false;
			rs.CullMode = CullMode.CullCounterClockwiseFace;
		}

		public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            Matrix viewproj = XNAViewMatrix * XNAProjectionMatrix;

            SceneryShader.Begin();
			ShaderPasses.Reset();
			while (ShaderPasses.MoveNext())
			{
				ShaderPasses.Current.Begin();
				foreach (RenderItem item in renderItems)
				{
					SceneryShader.SetMatrix(ref item.XNAMatrix, ref XNAViewMatrix, ref viewproj);
					SceneryShader.ZBias = item.RenderPrimitive.ZBias;
					SceneryShader.CommitChanges();
					item.RenderPrimitive.Draw(graphicsDevice);
				}
				ShaderPasses.Current.End();
			}
            SceneryShader.End();
        }
	}

    public class SkyMaterial : Material
    {
        SkyShader SkyShader;
        Texture2D skyTexture;
        Texture2D starTextureN;
        Texture2D starTextureS;
        Texture2D moonTexture;
        Texture2D moonMask;
        Texture2D cloudTexture;
        private Matrix XNAMoonMatrix;
        public RenderProcess RenderProcess;
		IEnumerator<EffectPass> ShaderPasses;

		public SkyMaterial(RenderProcess renderProcess)
			: base(null)
		{
            RenderProcess = renderProcess;
            SkyShader = Materials.SkyShader;
            skyTexture = renderProcess.Content.Load<Texture2D>("SkyDome1");
            starTextureN = renderProcess.Content.Load<Texture2D>("Starmap_N");
            starTextureS = renderProcess.Content.Load<Texture2D>("Starmap_S");
            moonTexture = renderProcess.Content.Load<Texture2D>("MoonMap");
            moonMask = renderProcess.Content.Load<Texture2D>("MoonMask");
            cloudTexture = renderProcess.Content.Load<Texture2D>("Clouds01");
        }

		public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
			if (ShaderPasses == null) ShaderPasses = SkyShader.Techniques["SkyTechnique"].Passes.GetEnumerator();

            // Adjust Fog color for day-night conditions and overcast
            FogDay2Night(
                RenderProcess.Viewer.SkyDrawer.solarDirection.Y,
                RenderProcess.Viewer.SkyDrawer.overcast);
            Materials.FogCoeff = RenderProcess.Viewer.SkyDrawer.fogCoeff;

            SkyShader.SkyTexture = skyTexture;
            SkyShader.StarTexture = skyTexture;
            SkyShader.SunDirection = RenderProcess.Viewer.SkyDrawer.solarDirection;
            if (RenderProcess.Viewer.SkyDrawer.latitude > 0)
                SkyShader.StarTexture = starTextureN;
            else
                SkyShader.StarTexture = starTextureS;
            SkyShader.SunpeakColor = RenderProcess.Viewer.SkyDrawer.sunpeakColor;
            SkyShader.SunriseColor = RenderProcess.Viewer.SkyDrawer.sunriseColor;
            SkyShader.SunsetColor = RenderProcess.Viewer.SkyDrawer.sunsetColor;
            SkyShader.Time = (float)RenderProcess.Viewer.Simulator.ClockTime / 100000;
            SkyShader.MoonScale = SkyConstants.skyRadius / 20;
            SkyShader.MoonTexture = moonTexture;
            SkyShader.MoonMaskTexture = moonMask;
            SkyShader.Random = RenderProcess.Viewer.SkyDrawer.moonPhase;
            SkyShader.CloudTexture = cloudTexture;
            SkyShader.Overcast = RenderProcess.Viewer.SkyDrawer.overcast;
            SkyShader.WindSpeed = RenderProcess.Viewer.SkyDrawer.windSpeed;
            SkyShader.WindDirection = RenderProcess.Viewer.SkyDrawer.windDirection;

            // Sky dome
            SkyShader.CurrentTechnique = SkyShader.Techniques["SkyTechnique"];
            RenderProcess.Viewer.SkyDrawer.SkyMesh.drawIndex = 1;

            Matrix viewXNASkyProj = XNAViewMatrix * Camera.XNASkyProjection;

            SkyShader.Begin();
			ShaderPasses.Reset();
			while (ShaderPasses.MoveNext())
            {
				ShaderPasses.Current.Begin();
                foreach (var item in renderItems)
                {
                    Matrix wvp = item.XNAMatrix * viewXNASkyProj;
                    SkyShader.SetMatrix(ref wvp, ref XNAViewMatrix);
                    SkyShader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
				ShaderPasses.Current.End();
            }
            SkyShader.End();

            // Moon
            SkyShader.CurrentTechnique = SkyShader.Techniques["MoonTechnique"];
            RenderProcess.Viewer.SkyDrawer.SkyMesh.drawIndex = 2;

            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = true;
            rs.CullMode = CullMode.CullClockwiseFace;
            rs.DestinationBlend = Blend.InverseSourceAlpha;
            rs.SourceBlend = Blend.SourceAlpha;

            // Send the transform matrices to the shader
            int skyRadius = RenderProcess.Viewer.SkyDrawer.SkyMesh.skyRadius;
            int cloudRadiusDiff = RenderProcess.Viewer.SkyDrawer.SkyMesh.cloudDomeRadiusDiff;
            XNAMoonMatrix = Matrix.CreateTranslation(RenderProcess.Viewer.SkyDrawer.lunarDirection * (skyRadius - (cloudRadiusDiff / 2)));
            Matrix XNAMoonMatrixView = XNAMoonMatrix * XNAViewMatrix;

            SkyShader.Begin();
			ShaderPasses.Reset();
			while (ShaderPasses.MoveNext())
            {
				ShaderPasses.Current.Begin();
                foreach (var item in renderItems)
                {
                    Matrix wvp = item.XNAMatrix * XNAMoonMatrixView * Camera.XNASkyProjection;
                    SkyShader.SetMatrix(ref wvp, ref XNAViewMatrix);
                    SkyShader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
				ShaderPasses.Current.End();
            }
            SkyShader.End();

            // Clouds
            SkyShader.CurrentTechnique = SkyShader.Techniques["CloudTechnique"];
            RenderProcess.Viewer.SkyDrawer.SkyMesh.drawIndex = 3;

            rs.CullMode = CullMode.CullCounterClockwiseFace;

            SkyShader.Begin();
			ShaderPasses.Reset();
			while (ShaderPasses.MoveNext())
            {
				ShaderPasses.Current.Begin();
                foreach (var item in renderItems)
                {
                    Matrix wvp = item.XNAMatrix * viewXNASkyProj;
                    SkyShader.SetMatrix(ref wvp, ref XNAViewMatrix);
                    SkyShader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
				ShaderPasses.Current.End();
            }
            SkyShader.End();
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = false;
            rs.DestinationBlend = Blend.Zero;
            rs.SourceBlend = Blend.One;
        }

		public override bool GetBlending(RenderPrimitive renderPrimitive)
		{
			return false;
		}

        /// <summary>
        /// This function darkens the fog color as night begins to fall
        /// as well as with increasing overcast.
        /// </summary>
        /// <param name="sunHeight">The Y value of the sunlight vector</param>
        /// <param name="overcast">The amount of overcast</param>
        private void FogDay2Night(float sunHeight, float overcast)
        {
            // We'll work with floating-point values, then convert to a "Color" object
            const float nightStart = 0.15f; // The sun's Y value where it begins to get dark
            const float nightFinish = -0.05f; // The Y value where darkest fog color is reached and held steady
            Vector3 startColor; // Original daytime fog color - must be preserved!
            Vector3 finishColor; //Darkest nighttime fog color
            Vector3 floatColor; // A scratchpad variable

            // These should be user defined in the Environment files (future)
            startColor = new Vector3(0.647f, 0.651f, 0.655f);
            finishColor = new Vector3(0.05f, 0.05f, 0.05f);

            if (sunHeight > nightStart)
                floatColor = startColor;
            else if (sunHeight < nightFinish)
                floatColor = finishColor;
            else
            {
                float amount = (sunHeight - nightFinish) / (nightStart - nightFinish);
                floatColor.X = MathHelper.Lerp(finishColor.X, startColor.X, amount);
                floatColor.Y = MathHelper.Lerp(finishColor.Y, startColor.Y, amount);
                floatColor.Z = MathHelper.Lerp(finishColor.Z, startColor.Z, amount);
            }

            // Adjust fog color for overcast
            floatColor *= (1 - 0.5f * overcast);

            // Convert color format
            Materials.FogColor.R = (byte)(floatColor.X * 255);
            Materials.FogColor.G = (byte)(floatColor.Y * 255);
            Materials.FogColor.B = (byte)(floatColor.Z * 255);
        }
    }

    public class ParticleEmitterMaterial : Material
    {
        ParticleEmitterShader particleEmitterShader;
        public Texture2D texture = null;
        public RenderProcess renderProcess;

        public ParticleEmitterMaterial(RenderProcess renderProcess)
            : base(null)
        {
            this.renderProcess = renderProcess;
            particleEmitterShader = Materials.ParticleEmitterShader;
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            particleEmitterShader.CurrentTime = (float)renderProcess.Viewer.Simulator.GameTime;

            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = true;
            rs.DepthBufferWriteEnable = false;
            rs.DestinationBlend = Blend.InverseSourceAlpha;
            rs.SourceBlend = Blend.SourceAlpha;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            particleEmitterShader.Begin();

            foreach (EffectPass pass in particleEmitterShader.CurrentTechnique.Passes)
            {
                pass.Begin();

                foreach (var item in renderItems)
                {
                    ParticleEmitter emitter = (ParticleEmitter)item.RenderPrimitive;
                    particleEmitterShader.CameraTileXY = emitter.CameraTileXZ;
                    particleEmitterShader.ColorTint = emitter.ColorTint;
                    particleEmitterShader.EmitDirection = emitter.EmitterData.Direction;
                    particleEmitterShader.EmitSize = emitter.EmitterData.NozzleWidth;
                    particleEmitterShader.Texture = texture;

                    particleEmitterShader.SetMatrix(item.XNAMatrix, ref XNAViewMatrix, ref XNAProjectionMatrix);
                    particleEmitterShader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }

                pass.End();
            }

            particleEmitterShader.End();
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = false;
            rs.DepthBufferWriteEnable = true;
            rs.DestinationBlend = Blend.Zero;
            rs.SourceBlend = Blend.One;
        }

        public override bool GetBlending(RenderPrimitive renderPrimitive)
        {
            return true;
        }
    }

	public class PrecipMaterial : Material
    {
        PrecipShader PrecipShader;
        Texture2D rainTexture;
        Texture2D snowTexture;
        public RenderProcess RenderProcess;
		IEnumerator<EffectPass> ShaderPasses;

		public PrecipMaterial(RenderProcess renderProcess)
			: base(null)
		{
			RenderProcess = renderProcess;
            PrecipShader = Materials.PrecipShader;
            rainTexture = renderProcess.Content.Load<Texture2D>("Raindrop");
            snowTexture = renderProcess.Content.Load<Texture2D>("Snowflake");
        }

		public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
		{
            PrecipShader.CurrentTechnique = PrecipShader.Techniques["RainTechnique"];
			if (ShaderPasses == null) ShaderPasses = PrecipShader.Techniques["RainTechnique"].Passes.GetEnumerator();
            PrecipShader.WeatherType = (int)RenderProcess.Viewer.Simulator.Weather;
            PrecipShader.SunDirection = RenderProcess.Viewer.SkyDrawer.solarDirection;
            PrecipShader.ViewportHeight = RenderProcess.Viewer.DisplaySize.Y;
            PrecipShader.CurrentTime = (float)RenderProcess.Viewer.Simulator.ClockTime;
            switch (RenderProcess.Viewer.Simulator.Weather)
            {
                case MSTS.WeatherType.Snow:
                    PrecipShader.PrecipTexture = snowTexture;
                    break;
                case MSTS.WeatherType.Rain:
                    PrecipShader.PrecipTexture = rainTexture;
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
			if (RenderProcess.Viewer.Simulator.Weather == MSTS.WeatherType.Clear)
				return;

            PrecipShader.Begin();
			ShaderPasses.Reset();
			while (ShaderPasses.MoveNext())
            {
                ShaderPasses.Current.Begin();
                foreach (var item in renderItems)
                {
                    PrecipShader.SetMatrix(item.XNAMatrix, ref XNAViewMatrix, ref Camera.XNASkyProjection);
                    PrecipShader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                ShaderPasses.Current.End();
            }
            PrecipShader.End();
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

		public override bool GetBlending(RenderPrimitive renderPrimitive)
		{
			return true;
		}
	}

	public class ForestMaterial : Material
    {
        public readonly RenderProcess RenderProcess;  // for diagnostics only
        readonly Texture2D TreeTexture = null;
		IEnumerator<EffectPass> ShaderPasses;

		public ForestMaterial(RenderProcess renderProcess, string treeTexture)
			: base(treeTexture)
		{
            RenderProcess = renderProcess;
            TreeTexture = SharedTextureManager.Get(renderProcess.GraphicsDevice, treeTexture);
        }

		public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
		{
			Materials.SceneryShader.CurrentTechnique = Materials.SceneryShader.Techniques["Forest"];
			if (ShaderPasses == null) ShaderPasses = Materials.SceneryShader.Techniques["Forest"].Passes.GetEnumerator();
			Materials.SceneryShader.ImageMap_Tex = TreeTexture;

            var rs = graphicsDevice.RenderState;
			rs.AlphaFunction = CompareFunction.GreaterEqual;
            rs.AlphaTestEnable = true;
			rs.ReferenceAlpha = 200;
		}

		public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
			var shader = Materials.SceneryShader;
			var viewproj = XNAViewMatrix * XNAProjectionMatrix;

			shader.Begin();
			ShaderPasses.Reset();
			while (ShaderPasses.MoveNext())
            {
                ShaderPasses.Current.Begin();
                foreach (var item in renderItems)
                {
					shader.SetMatrix(ref item.XNAMatrix, ref XNAViewMatrix, ref viewproj);
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
			rs.AlphaFunction = CompareFunction.Always;
            rs.AlphaTestEnable = false;
			rs.ReferenceAlpha = 0;
		}

		public override Texture2D GetShadowTexture(RenderPrimitive renderPrimitive)
		{
			return TreeTexture;
		}
	}

	public class LightGlowMaterial : Material
    {
        LightGlowShader LightGlowShader;
        Texture2D lightGlowTexture;
        public RenderProcess RenderProcess;

		public LightGlowMaterial(RenderProcess renderProcess)
			: base(null)
		{
            RenderProcess = renderProcess;
            LightGlowShader = Materials.LightGlowShader;
            lightGlowTexture = renderProcess.Content.Load<Texture2D>("Lightglow");
        }

		public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
		{
            LightGlowShader.CurrentTechnique = LightGlowShader.Techniques["LightGlow"];
            LightGlowShader.LightGlowTexture = lightGlowTexture;

            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = true;
            rs.DepthBufferWriteEnable = false;
            rs.DestinationBlend = Blend.InverseSourceAlpha;
			rs.SourceBlend = Blend.SourceAlpha;
		}

		public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            LightGlowShader.Begin();
            foreach (EffectPass pass in LightGlowShader.CurrentTechnique.Passes)
            {
                pass.Begin();

                foreach (var item in renderItems)
                {
                    Matrix wvp = item.XNAMatrix * XNAViewMatrix * Camera.XNASkyProjection;
                    LightGlowShader.SetMatrix(ref wvp);
                    LightGlowShader.SetFade(((LightMesh)item.RenderPrimitive).Fade);
                    LightGlowShader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                pass.End();
            }
            LightGlowShader.End();
        }

		public override void ResetState(GraphicsDevice graphicsDevice)
		{
            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = false;
            rs.DepthBufferWriteEnable = true;
            rs.DestinationBlend = Blend.Zero;
			rs.SourceBlend = Blend.One;
		}

		public override bool GetBlending(RenderPrimitive renderPrimitive)
		{
			return true;
		}
	}
    
    public class LightConeMaterial : Material
    {
        readonly RenderProcess RenderProcess;
        readonly LightConeShader LightConeShader;

        public LightConeMaterial(RenderProcess renderProcess)
            : base(null)
        {
            RenderProcess = renderProcess;
            LightConeShader = Materials.LightConeShader;
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            LightConeShader.CurrentTechnique = LightConeShader.Techniques["LightCone"];

            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = true;
            rs.DepthBufferWriteEnable = false;
            rs.StencilEnable = true;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            LightConeShader.Begin();
            foreach (EffectPass pass in LightConeShader.CurrentTechnique.Passes)
            {
                pass.Begin();
                foreach (var item in renderItems)
                {
                    Matrix wvp = item.XNAMatrix * XNAViewMatrix * Camera.XNASkyProjection;
                    LightConeShader.SetMatrix(ref wvp);
                    LightConeShader.SetFade(((LightMesh)item.RenderPrimitive).Fade);
                    LightConeShader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                pass.End();
            }
            LightConeShader.End();
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = false;
            rs.CullMode = CullMode.CullCounterClockwiseFace;
            rs.DestinationBlend = Blend.Zero;
            rs.DepthBufferFunction = CompareFunction.LessEqual;
            rs.DepthBufferWriteEnable = true;
            rs.SourceBlend = Blend.One;
            rs.StencilEnable = false;
            rs.StencilFunction = CompareFunction.Always;
            rs.StencilPass = StencilOperation.Keep;
        }
    }

	public class WaterMaterial : Material
    {
        public readonly RenderProcess RenderProcess;  // for diagnostics only
        readonly Texture2D WaterTexture;
		IEnumerator<EffectPass> ShaderPasses;

		public WaterMaterial(RenderProcess renderProcess, string waterTexturePath)
			: base(waterTexturePath)
		{
			RenderProcess = renderProcess;
			WaterTexture = SharedTextureManager.Get(renderProcess.GraphicsDevice, waterTexturePath);
		}

		public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
		{
			Materials.SceneryShader.CurrentTechnique = Materials.SceneryShader.Techniques[RenderProcess.Viewer.Settings.ShaderModel >= 3 ? "ImagePS3" : "ImagePS2"];
			if (ShaderPasses == null) ShaderPasses = Materials.SceneryShader.Techniques[RenderProcess.Viewer.Settings.ShaderModel >= 3 ? "ImagePS3" : "ImagePS2"].Passes.GetEnumerator();
			Materials.SceneryShader.ImageMap_Tex = WaterTexture;

			graphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
			graphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
			graphicsDevice.SamplerStates[0].MipMapLevelOfDetailBias = 0;

            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = true;
			rs.DestinationBlend = Blend.InverseSourceAlpha;
			rs.SourceBlend = Blend.SourceAlpha;

			graphicsDevice.VertexDeclaration = WaterTile.PatchVertexDeclaration;
		}

		public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
			var shader = Materials.SceneryShader;
			var viewproj = XNAViewMatrix * XNAProjectionMatrix;

			shader.Begin();
			ShaderPasses.Reset();
			while (ShaderPasses.MoveNext())
            {
                ShaderPasses.Current.Begin();
                foreach (var item in renderItems)
                {
					shader.SetMatrix(ref item.XNAMatrix, ref XNAViewMatrix, ref viewproj);
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

		public override bool GetBlending(RenderPrimitive renderPrimitive)
		{
			return true;
		}
	}

	public class ShadowMapMaterial : Material
    {
		public readonly RenderProcess RenderProcess;  // for diagnostics only
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

		public ShadowMapMaterial(RenderProcess renderProcess)
			: base(null)
		{
			RenderProcess = renderProcess;

            var shadowMapResolution = renderProcess.Viewer.Settings.ShadowMapResolution;
            BlurVertexDeclaration = new VertexDeclaration(renderProcess.GraphicsDevice, VertexPositionNormalTexture.VertexElements);
            BlurVertexBuffer = new VertexBuffer(renderProcess.GraphicsDevice, typeof(VertexPositionNormalTexture), 4, BufferUsage.WriteOnly);
            BlurVertexBuffer.SetData(new[] {
				new VertexPositionNormalTexture(new Vector3(-1, +1, 0), Vector3.Zero, new Vector2(0, 0)),
				new VertexPositionNormalTexture(new Vector3(+1, +1, 0), Vector3.Zero, new Vector2(shadowMapResolution, 0)),
				new VertexPositionNormalTexture(new Vector3(+1, -1, 0), Vector3.Zero, new Vector2(shadowMapResolution, shadowMapResolution)),
				new VertexPositionNormalTexture(new Vector3(-1, -1, 0), Vector3.Zero, new Vector2(0, shadowMapResolution)),
			});
        }

		public void SetState(GraphicsDevice graphicsDevice, Mode mode)
		{
			var shader = Materials.ShadowMapShader;
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
			var shader = Materials.ShadowMapShader;
			var viewproj = XNAViewMatrix * XNAProjectionMatrix;

            shader.SetData(ref XNAViewMatrix);
            shader.Begin();
			ShaderPasses.Reset();
			while (ShaderPasses.MoveNext())
			{
				ShaderPasses.Current.Begin();
				foreach (var item in renderItems)
				{
					var wvp = item.XNAMatrix * viewproj;
					shader.SetData(ref wvp, item.Material.GetShadowTexture(item.RenderPrimitive));
					shader.CommitChanges();
					item.RenderPrimitive.Draw(graphicsDevice);
				}
				ShaderPasses.Current.End();
			}
			shader.End();
		}

		public Texture2D ApplyBlur(GraphicsDevice graphicsDevice, Texture2D shadowMap, RenderTarget2D renderTarget, DepthStencilBuffer stencilBuffer, DepthStencilBuffer normalStencilBuffer)
		{
			var wvp = Matrix.Identity;

			var shader = Materials.ShadowMapShader;
			shader.CurrentTechnique = shader.Techniques["ShadowMapBlur"];
            shader.SetBlurData(ref wvp, ref wvp);
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

		public PopupWindowMaterial(RenderProcess renderProcess)
			: base(null)
		{
		}

		public void SetState(GraphicsDevice graphicsDevice, Texture2D screen)
		{
			var shader = Materials.PopupWindowShader;
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
            var shader = Materials.PopupWindowShader;

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

		public override bool GetBlending(RenderPrimitive renderPrimitive)
		{
			return true;
		}
	}

	public class YellowMaterial : Material
    {
        static BasicEffect basicEffect = null;
        RenderProcess RenderProcess;

		public YellowMaterial(RenderProcess renderProcess)
			: base(null)
		{
            RenderProcess = renderProcess;
            if (basicEffect == null)
            {
                basicEffect = new BasicEffect(renderProcess.GraphicsDevice, null);
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

	//Material to draw train car numbers, sidings and platforms
	public class ActivityInforMaterial : Material
	{
		public SpriteBatch SpriteBatch;
		public Texture2D Texture;
		public SpriteFont Font;
		public RenderProcess RenderProcess;  // for diagnostics only
		Viewer3D Viewer;

		//texts are aligned as table cells, but they can be either in table A or table B
		public List<Vector2>[] AlignedTextA;
		public List<Vector2>[] AlignedTextB;
		public float LineSpacing;

		public ActivityInforMaterial(RenderProcess renderProcess)
			: base(null)
		{
			RenderProcess = renderProcess;
			Viewer = RenderProcess.Viewer;
			SpriteBatch = new SpriteBatch(renderProcess.GraphicsDevice);
			Texture = new Texture2D(SpriteBatch.GraphicsDevice, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
			Texture.SetData(new[] { Color.White });
			Font = renderProcess.Content.Load<SpriteFont>("ArialMedium");
            LineSpacing = Font.LineSpacing * 3 / 4;
			if (LineSpacing < 10) LineSpacing = 10; //if spacing between text lines is too small
		}

		public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
		{
			float scaling = (float)graphicsDevice.PresentationParameters.BackBufferHeight / RenderProcess.GraphicsDeviceManager.PreferredBackBufferHeight;
			Vector3 screenScaling = new Vector3(scaling);
			Matrix xForm = Matrix.CreateScale(screenScaling);
			SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.SaveState, xForm);
			SpriteBatch.GraphicsDevice.RenderState.DepthBufferEnable = true;//want the line to have z-buffer effect
		}

		public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
		{
			//texts are put to a virtual table on the screen, which has one column each row, and texts in the same row
			//do not overlap each other. to put more information, we created two tables, and a primitive can use either of them. 
			//For example, train car name use AlignedTextA, and siding names use AlignedTextB
			//each rending process, we will clear the text in the tables before put texts in
			if (AlignedTextA == null || Viewer.GraphicsDevice.Viewport.Height / (int)LineSpacing + 1 != AlignedTextA.Length)
			{
				AlignedTextA = new List<Vector2>[Viewer.GraphicsDevice.Viewport.Height / (int)LineSpacing + 1];
				for (var i = 0; i < AlignedTextA.Length; i++) AlignedTextA[i] = new List<Vector2>();
			}
			else
			{
				foreach (List<Vector2> ls in AlignedTextA) ls.Clear();
			}

			if (AlignedTextB == null || Viewer.GraphicsDevice.Viewport.Height / (int)LineSpacing + 1 != AlignedTextB.Length)
			{
				AlignedTextB = new List<Vector2>[Viewer.GraphicsDevice.Viewport.Height / (int)LineSpacing + 1];
				for (var i = 0; i < AlignedTextB.Length; i++) AlignedTextB[i] = new List<Vector2>();
			}
			else
			{
				foreach (List<Vector2> ls in AlignedTextB) ls.Clear();
			}

			foreach (var item in renderItems)
			{
				item.RenderPrimitive.Draw(graphicsDevice);
			}
		}

		public override void ResetState(GraphicsDevice graphicsDevice)
		{
			SpriteBatch.End();//DepthBufferEnable will be restored to previous state
		}

		public override bool GetBlending(RenderPrimitive renderPrimitive)
		{
			return true;
		}
	}
}
