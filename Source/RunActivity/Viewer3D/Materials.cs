/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 
/// Principal Author:
///    Wayne Campbell
/// Contributors:
///    Rick Grout
///    Walt Niehoff
///     

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ORTS
{
    #region Materials class
    public class Materials
    {
        public static SceneryShader SceneryShader = null;
        public static SkyShader SkyShader = null;
        public static PrecipShader PrecipShader = null;
        public static LightGlowShader LightGlowShader = null;
        public static SpriteBatchMaterial SpriteBatchMaterial = null;
		private static Dictionary<string, WaterMaterial> WaterMaterials = new Dictionary<string, WaterMaterial>();
		private static SkyMaterial SkyMaterial = null;
        private static PrecipMaterial PrecipMaterial = null;
        private static DynatrackMaterial DynatrackMaterial = null;
        private static LightGlowMaterial LightGlowMaterial = null;
        private static Dictionary<string, TerrainMaterial> TerrainMaterials = new Dictionary<string, TerrainMaterial>();
        private static Dictionary<string, ForestMaterial> ForestMaterials = new Dictionary<string, ForestMaterial>();
        private static Dictionary<string, SceneryMaterial> SceneryMaterials = new Dictionary<string, SceneryMaterial>();
        public static Texture2D MissingTexture = null;  // sub this when we are missing the required texture
        private static Material YellowMaterial = null;   // for debug and experiments
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
            SceneryShader.NormalMap_Tex = MSTS.ACEFile.Texture2DFromFile(renderProcess.GraphicsDevice, 
                                                        renderProcess.Viewer.Simulator.RoutePath + @"\TERRTEX\microtex.ace");
            SkyShader = new SkyShader(renderProcess.GraphicsDevice, renderProcess.Content);
            PrecipShader = new PrecipShader(renderProcess.GraphicsDevice, renderProcess.Content);
            LightGlowShader = new LightGlowShader(renderProcess.GraphicsDevice, renderProcess.Content);
            SpriteBatchMaterial = new SpriteBatchMaterial(renderProcess);
            // WaterMaterial here.
            SkyMaterial = new SkyMaterial(renderProcess);
            PrecipMaterial = new PrecipMaterial(renderProcess);
            DynatrackMaterial = new DynatrackMaterial(renderProcess);
            LightGlowMaterial = new LightGlowMaterial(renderProcess);
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

        public static Material Load(RenderProcess renderProcess, string materialName, string textureName, int options, float mipMapBias )
        {
            System.Diagnostics.Debug.Assert(IsInitialized, "Must initialize Materials before using.");
            if (!IsInitialized)             // this shouldn't happen, but if it does
            {
                Trace.TraceWarning("Program Bug: Must initialize Materials before using.");
                Initialize(renderProcess);  // warn, and do it now rather than fail
            }

            if( textureName != null )
                textureName = textureName.ToLower();

            switch (materialName)
            {
                case "SpriteBatch":
                    return SpriteBatchMaterial;
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
                case "PrecipMaterial":
                    return PrecipMaterial;
                case "DynatrackMaterial":
                    return DynatrackMaterial;
                case "LightGlowMaterial":
                    return LightGlowMaterial;
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
                default:
                    return Load(renderProcess, "SceneryMaterial");
            }
        }

        public static float ViewingDistance = 3000;  // TODO, this is awkward, viewer must set this to control fog

        static internal Vector3 sunDirection;
        static Vector3 headlightPosition;
        static Vector3 headlightDirection;
        static int lastLightState = 0, currentLightState = 0;
		static double fadeStartTimer = 0;
		static float fadeDuration = -1;
		internal static void UpdateShaders(RenderProcess renderProcess, GraphicsDevice graphicsDevice)
		{
			sunDirection = renderProcess.Viewer.SkyDrawer.solarDirection;
			SceneryShader.LightVector = sunDirection;

			// Headlight illumination
			if (renderProcess.Viewer.PlayerLocomotiveViewer != null
				&& renderProcess.Viewer.PlayerLocomotiveViewer.lightGlowDrawer != null
				&& renderProcess.Viewer.PlayerLocomotiveViewer.lightGlowDrawer.lightMesh.hasHeadlight)
			{
				currentLightState = renderProcess.Viewer.PlayerLocomotive.Headlight;
				if (currentLightState != lastLightState)
				{
					if (currentLightState == 2 && lastLightState == 1)
					{
						fadeStartTimer = renderProcess.Viewer.Simulator.ClockTime;
						fadeDuration = renderProcess.Viewer.PlayerLocomotiveViewer.lightGlowDrawer.lightconeFadein;
					}
					else if (currentLightState == 1 && lastLightState == 2)
					{
						fadeStartTimer = renderProcess.Viewer.Simulator.ClockTime;
						fadeDuration = -renderProcess.Viewer.PlayerLocomotiveViewer.lightGlowDrawer.lightconeFadeout;
					}
					lastLightState = currentLightState;
				}
				headlightPosition = renderProcess.Viewer.PlayerLocomotiveViewer.lightGlowDrawer.xnaLightconeLoc;
				headlightDirection = renderProcess.Viewer.PlayerLocomotiveViewer.lightGlowDrawer.xnaLightconeDir;

				SceneryShader.SetHeadlight(ref headlightPosition, ref headlightDirection, (float)(renderProcess.Viewer.Simulator.ClockTime - fadeStartTimer), fadeDuration);
			}
			// End headlight illumination

			SceneryShader.Overcast = renderProcess.Viewer.SkyDrawer.overcast;

			SceneryShader.SetFog(ViewingDistance * 0.5f * FogCoeff, ref Materials.FogColor);
		}
    }
    #endregion

    #region Shared texture manager
    public class SharedTextureManager
    {
        private static Dictionary<string, Texture2D> SharedTextures = new Dictionary<string, Texture2D>();

        public static Texture2D Get(GraphicsDevice device, string path)
        {
            if (path == null)
                return Materials.MissingTexture;

            if (!SharedTextures.ContainsKey(path))
            {
                try { 
                    Texture2D texture = MSTS.ACEFile.Texture2DFromFile(device, path);
                    SharedTextures.Add(path, texture);
                    return texture;
                }
                catch (Exception error)
                {
					Trace.WriteLine(path);
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
    #endregion

    #region Material interface
	public abstract class Material
	{
		public virtual void SetState(GraphicsDevice graphicsDevice, Material previousMaterial) { }
		public virtual void Render(GraphicsDevice graphicsDevice, List<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix) { }
		public virtual void ResetState(GraphicsDevice graphicsDevice) { }

		public virtual bool GetBlending(RenderPrimitive renderPrimitive) { return false; }
		public virtual Texture2D GetShadowTexture(RenderPrimitive renderPrimitive) { return null; }
	}
    #endregion

    #region Empty material
	public class EmptyMaterial : Material
	{
	}
    #endregion

    #region Sprite batch material
	public class SpriteBatchMaterial : Material
	{
		public SpriteBatch SpriteBatch;
		public SpriteFont DefaultFont;
		public RenderProcess RenderProcess;  // for diagnostics only

		public SpriteBatchMaterial(RenderProcess renderProcess)
		{
			RenderProcess = renderProcess;
			SpriteBatch = new SpriteBatch(renderProcess.GraphicsDevice);
			DefaultFont = renderProcess.Content.Load<SpriteFont>("Arial");
		}

		public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
		{
			float scaling = (float)graphicsDevice.PresentationParameters.BackBufferHeight / RenderProcess.GraphicsDeviceManager.PreferredBackBufferHeight;
			Vector3 screenScaling = new Vector3(scaling);
			Matrix xForm = Matrix.CreateScale(screenScaling);
			RenderProcess.RenderStateChangesCount++;
			SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.SaveState, xForm);
		}

        public override void Render(GraphicsDevice graphicsDevice, List<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
		{
            foreach (var item in renderItems)
            {
                item.RenderPrimitive.Draw(graphicsDevice);
            }
		}

		public override void ResetState(GraphicsDevice graphicsDevice)
		{
			SpriteBatch.End();
		}

        public override bool GetBlending(RenderPrimitive renderPrimitive)
        {
            return true;
        }
	}
    #endregion

    #region Scenery material
    public class SceneryMaterial : Material
    {
		readonly int Options = 0;
		readonly float MipMapBias = 0;
        readonly SceneryShader SceneryShader;
		readonly Texture2D Texture;
		readonly Texture2D nightTexture = null;
		bool isNightEnabled = false;
		readonly public RenderProcess RenderProcess;  // for diagnostics only

        public SceneryMaterial(RenderProcess renderProcess, string texturePath, int options, float mipMapBias)  
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
			int prevOptions = -1;

			if (previousMaterial == null || this.GetType() != previousMaterial.GetType())
			{
				RenderProcess.RenderStateChangesCount++;
				graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
				graphicsDevice.SamplerStates[0].MipMapLevelOfDetailBias = 0;
			}
			else
			{
				prevOptions = ((SceneryMaterial)previousMaterial).Options;
			}

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

			if (prevOptions != Options)
			{
				// Lighting model
				int lighting = (Options & 0x00f0) >> 4;
				SceneryShader.CurrentTechnique = SceneryShader.Techniques["Image"]; // Default
				switch (lighting)
				{
					case 1: // DarkShade (-12)
						SceneryShader.CurrentTechnique = SceneryShader.Techniques["DarkShade"];
						break;
					case 2: // OptHalfBright (-11)
						SceneryShader.CurrentTechnique = SceneryShader.Techniques["HalfBright"];
						break;
					case 4: // CruciformLong (-10)
					case 3: // Cruciform (-9)
						SceneryShader.CurrentTechnique = SceneryShader.Techniques["Vegetation"];
						break;
					case 5: // OptFullBright (-8)
						SceneryShader.CurrentTechnique = SceneryShader.Techniques["FullBright"];
						break;
					case 6: // OptSpecular750 (-7)
						// TODO: SceneryShader.SpecularPower = 64;
						break;
					case 7: // OptSpecular25 (-6)
						// TODO: SceneryShader.SpecularPower = 128;
						break;
					case 8: // OptSpecular0 (-5)
					default:
						// TODO: SceneryShader.SpecularPower = 0;
						break;
				}

				// Transparency test
				int alphaTest = (Options & 0x0100) >> 8;
				if (alphaTest == 0)
				{
					graphicsDevice.RenderState.AlphaBlendEnable = false;
					graphicsDevice.RenderState.AlphaTestEnable = false;
				}
				else
				{
					graphicsDevice.RenderState.AlphaBlendEnable = false;
					graphicsDevice.RenderState.AlphaTestEnable = true;
					graphicsDevice.RenderState.AlphaFunction = CompareFunction.GreaterEqual;        // if alpha > reference, then skip processing this pixel
					graphicsDevice.RenderState.ReferenceAlpha = 200;  // setting this to 128, chain link fences become solid at distance, at 200, they become
				}

				// Translucency
				int shaders = Options & 0x000f;
				if (alphaTest == 0 && shaders >= 4)
				{
					graphicsDevice.RenderState.AlphaTestEnable = true;
					graphicsDevice.RenderState.AlphaFunction = CompareFunction.GreaterEqual;
					graphicsDevice.RenderState.ReferenceAlpha = 10;  // ie lightcode is 9 in full transparent areas
					graphicsDevice.RenderState.AlphaBlendEnable = true;
					graphicsDevice.RenderState.BlendFunction = BlendFunction.Add;
					graphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
					graphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
					graphicsDevice.RenderState.SeparateAlphaBlendEnabled = true;
					graphicsDevice.RenderState.AlphaSourceBlend = Blend.Zero;
					graphicsDevice.RenderState.AlphaDestinationBlend = Blend.One;
					graphicsDevice.RenderState.AlphaBlendOperation = BlendFunction.Add;
				}

				// Texture addressing
				int wrapping = (Options & 0x1800) >> 11;
				switch (wrapping)
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
			}

			RenderProcess.ImageChangesCount++;

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

			if (MipMapBias < -1)
				graphicsDevice.SamplerStates[0].MipMapLevelOfDetailBias = -1;   // clamp to -1 max
			else
				graphicsDevice.SamplerStates[0].MipMapLevelOfDetailBias = MipMapBias;
		}

        public override void Render(GraphicsDevice graphicsDevice, List<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
			var lighting = (Options & 0x00f0) >> 4;
			switch (lighting)
			{
				case 1: // DarkShade (-12)
				case 2: // OptHalfBright (-11)
				case 4: // CruciformLong (-10)
				case 3: // Cruciform (-9)
				case 5: // OptFullBright (-8)
					break;
				case 6: // OptSpecular750 (-7)
				case 7: // OptSpecular25 (-6)
				case 8: // OptSpecular0 (-5)
				default:
					SceneryShader.ViewerPos = new Vector3(XNAViewMatrix.M41, XNAViewMatrix.M42, XNAViewMatrix.M43);
					break;
			}

            Matrix viewProj = XNAViewMatrix * XNAProjectionMatrix;

            // With the GPU configured, now we can draw the primitive
            SceneryShader.Begin();
            foreach (EffectPass pass in SceneryShader.CurrentTechnique.Passes)
            {
                pass.Begin();

                foreach(RenderItem item in renderItems)
                {
                    SceneryShader.SetMatrix(item.XNAMatrix, ref XNAViewMatrix, ref viewProj);
                    SceneryShader.ZBias = item.RenderPrimitive.ZBias;
                    SceneryShader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }

                pass.End();
            }
            SceneryShader.End();
        }

		public override void ResetState(GraphicsDevice graphicsDevice)
		{
			SceneryShader.IsNight_Tex = false;
		}

		public override bool GetBlending(RenderPrimitive renderPrimitive)
		{
			// Transparency test
			int alphaTest = (Options & 0x0100) >> 8;
			if (alphaTest != 0)
				return true;

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
    #endregion

    #region Terrain material
    public class TerrainMaterial : Material
    {
        readonly SceneryShader SceneryShader;
        readonly Texture2D PatchTexture;
        readonly public RenderProcess RenderProcess;  // for diagnostics only

        public TerrainMaterial(RenderProcess renderProcess, string terrainTexture )
        {
            SceneryShader = Materials.SceneryShader;
            PatchTexture = SharedTextureManager.Get(renderProcess.GraphicsDevice, terrainTexture);
            RenderProcess = renderProcess;
        }

		public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
		{
			if (previousMaterial == null || this.GetType() != previousMaterial.GetType())
			{
				RenderProcess.RenderStateChangesCount++;

				graphicsDevice.SamplerStates[0].MipMapLevelOfDetailBias = 0;

				graphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
				graphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;

				graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
				SceneryShader.CurrentTechnique = SceneryShader.Techniques["Terrain"];
				graphicsDevice.RenderState.AlphaTestEnable = false;
				graphicsDevice.RenderState.AlphaBlendEnable = false;

				graphicsDevice.VertexDeclaration = TerrainPatch.PatchVertexDeclaration;
				graphicsDevice.Indices = TerrainPatch.PatchIndexBuffer;
			}

			RenderProcess.ImageChangesCount++;
			SceneryShader.ImageMap_Tex = PatchTexture;
		}

        public override void Render(GraphicsDevice graphicsDevice, List<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            Matrix viewproj = XNAViewMatrix * XNAProjectionMatrix;

            SceneryShader.Begin();
            foreach (EffectPass pass in SceneryShader.CurrentTechnique.Passes)
            {
                pass.Begin();

                foreach (RenderItem item in renderItems)
                {
                    SceneryShader.SetMatrix(item.XNAMatrix, ref XNAViewMatrix, ref viewproj);
                    SceneryShader.ZBias = item.RenderPrimitive.ZBias;
                    SceneryShader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }


                pass.End();
            }
            SceneryShader.End();
        }
	}
    #endregion

    #region Sky material
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
        private Matrix XNAMoonWorldMatrix;
        public RenderProcess RenderProcess;

        public SkyMaterial(RenderProcess renderProcess)
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

        public override void Render(GraphicsDevice graphicsDevice, List<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            

            // Adjust Fog color for day-night conditions and overcast
            FogDay2Night(
                RenderProcess.Viewer.SkyDrawer.solarDirection.Y,
                RenderProcess.Viewer.SkyDrawer.overcast);
            Materials.FogCoeff = RenderProcess.Viewer.SkyDrawer.fogCoeff;

            SkyShader.CurrentTechnique = SkyShader.Techniques["SkyTechnique"];
            SkyShader.SkyTexture = skyTexture;
            SkyShader.StarTexture = skyTexture;

            // Variables passed from SkyDrawer
            SkyShader.SunDirection = RenderProcess.Viewer.SkyDrawer.solarDirection;
            if (RenderProcess.Viewer.SkyDrawer.latitude > 0)
                SkyShader.StarTexture = starTextureN;
            else
                SkyShader.StarTexture = starTextureS;
            SkyShader.SunpeakColor = RenderProcess.Viewer.SkyDrawer.sunpeakColor;
            SkyShader.SunriseColor = RenderProcess.Viewer.SkyDrawer.sunriseColor;
            SkyShader.SunsetColor = RenderProcess.Viewer.SkyDrawer.sunsetColor;
            SkyShader.Time = (float)RenderProcess.Viewer.Simulator.ClockTime/100000;
            SkyShader.MoonScale = SkyConstants.skyRadius / 20;

            // Save existing render state
            bool fogEnable = graphicsDevice.RenderState.FogEnable;
            CullMode cullMode = graphicsDevice.RenderState.CullMode;
            // Set render state for drawing sky
            graphicsDevice.RenderState.FogEnable = false;
            graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;

            // Sky dome
            RenderProcess.RenderStateChangesCount++;
            RenderProcess.ImageChangesCount++;
            RenderProcess.Viewer.SkyDrawer.SkyMesh.drawIndex = 1;

            Matrix viewXNASkyProj = XNAViewMatrix * Camera.XNASkyProjection;

            SkyShader.Begin();
            foreach (EffectPass pass in SkyShader.CurrentTechnique.Passes)
            {
                pass.Begin();

                foreach (var item in renderItems)
                {
                    Matrix wvp = item.XNAMatrix * viewXNASkyProj;
                    SkyShader.SetMatrix(ref wvp, ref XNAViewMatrix);
                    SkyShader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                pass.End();
            }
            SkyShader.End();

            // Moon
            // Send the transform matrices to the shader
            int skyRadius = RenderProcess.Viewer.SkyDrawer.SkyMesh.skyRadius;
            int cloudRadiusDiff = RenderProcess.Viewer.SkyDrawer.SkyMesh.cloudDomeRadiusDiff;
            XNAMoonMatrix = Matrix.CreateTranslation(RenderProcess.Viewer.SkyDrawer.lunarDirection * (skyRadius - (cloudRadiusDiff / 2)));
            Matrix XNAMoonMatrixView = XNAMoonMatrix * XNAViewMatrix;

            // Shader setup
            SkyShader.CurrentTechnique = SkyShader.Techniques["MoonTechnique"];
            SkyShader.MoonTexture = moonTexture;
            SkyShader.MoonMaskTexture = moonMask;
            SkyShader.Random = RenderProcess.Viewer.SkyDrawer.moonPhase;

            // Save the existing alpha render state
            bool alphaBlendEnable = graphicsDevice.RenderState.AlphaBlendEnable;
            Blend destinationBlend = graphicsDevice.RenderState.DestinationBlend;
            Blend sourceBlend = graphicsDevice.RenderState.SourceBlend;
            bool alphaTestEnable = graphicsDevice.RenderState.AlphaTestEnable;
            // Set alpha render state for drawing the moon and clouds
            graphicsDevice.RenderState.AlphaBlendEnable = true;
            graphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
            graphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
            graphicsDevice.RenderState.AlphaTestEnable = false;
            graphicsDevice.RenderState.CullMode = CullMode.CullClockwiseFace;

            RenderProcess.RenderStateChangesCount++;
            RenderProcess.ImageChangesCount++;
            RenderProcess.Viewer.SkyDrawer.SkyMesh.drawIndex = 2;
            SkyShader.Begin();
            foreach (EffectPass pass in SkyShader.CurrentTechnique.Passes)
            {
                pass.Begin();

                foreach (var item in renderItems)
                {
                    Matrix wvp = item.XNAMatrix * XNAMoonMatrixView * Camera.XNASkyProjection;
                    SkyShader.SetMatrix(ref wvp, ref XNAViewMatrix);
                    SkyShader.CommitChanges();

                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                pass.End();
            }
            SkyShader.End();

            // Clouds
           
            // Shader setup
            SkyShader.CurrentTechnique = SkyShader.Techniques["CloudTechnique"];
            SkyShader.CloudTexture = cloudTexture;
            SkyShader.Overcast = RenderProcess.Viewer.SkyDrawer.overcast;
            SkyShader.WindSpeed = RenderProcess.Viewer.SkyDrawer.windSpeed;
            SkyShader.WindDirection = RenderProcess.Viewer.SkyDrawer.windDirection;
            graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;

            RenderProcess.RenderStateChangesCount++;
            RenderProcess.ImageChangesCount++;
            RenderProcess.Viewer.SkyDrawer.SkyMesh.drawIndex = 3;
            SkyShader.Begin();
            foreach (EffectPass pass in SkyShader.CurrentTechnique.Passes)
            {
                pass.Begin();

                foreach (var item in renderItems)
                {
                    Matrix wvp = item.XNAMatrix * viewXNASkyProj;
                    SkyShader.SetMatrix(ref wvp, ref XNAViewMatrix);
                    SkyShader.CommitChanges();

                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                pass.End();
            }
            SkyShader.End();

            // Restore the pre-existing render state
            graphicsDevice.RenderState.AlphaBlendEnable = alphaBlendEnable;
            graphicsDevice.RenderState.DestinationBlend = destinationBlend;
            graphicsDevice.RenderState.SourceBlend = sourceBlend;
            graphicsDevice.RenderState.AlphaTestEnable = alphaTestEnable;
            graphicsDevice.RenderState.CullMode = cullMode;
            graphicsDevice.RenderState.FogEnable = fogEnable;
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
    #endregion

    #region Precipitation material
    public class PrecipMaterial : Material
    {
        PrecipShader PrecipShader;
        Texture2D rainTexture;
        Texture2D snowTexture;
        public RenderProcess RenderProcess;

        public PrecipMaterial(RenderProcess renderProcess)
        {
            RenderProcess = renderProcess;
            PrecipShader = Materials.PrecipShader;
            rainTexture = renderProcess.Content.Load<Texture2D>("Raindrop");
            snowTexture = renderProcess.Content.Load<Texture2D>("Snowflake");
        }

        public override void Render(GraphicsDevice graphicsDevice, List<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            int weatherType = RenderProcess.Viewer.PrecipDrawer.weatherType;
            if (weatherType == 0) return; // Clear weather

            

            RenderProcess.RenderStateChangesCount++;
            RenderProcess.ImageChangesCount++;

            PrecipShader.CurrentTechnique = PrecipShader.Techniques["RainTechnique"];

            // Variables passed from PrecipDrawer
            PrecipShader.WeatherType = weatherType;
            PrecipShader.SunDirection = RenderProcess.Viewer.SkyDrawer.solarDirection;
            PrecipShader.ViewportHeight = (int)RenderProcess.Viewer.DisplaySize.Y;
            PrecipShader.CurrentTime = (float)RenderProcess.Viewer.Simulator.ClockTime;
            switch (weatherType)
            {
                case 1:
                    PrecipShader.PrecipTexture = snowTexture;
                    break;
                case 2:
                    PrecipShader.PrecipTexture = rainTexture;
                    break;
                // Safe? or need a default here? If so, what?
            }

            // Save the existing render state
            bool AlphaBlendEnable = graphicsDevice.RenderState.AlphaBlendEnable;
            bool AlphaTestEnable = graphicsDevice.RenderState.AlphaTestEnable;
            Blend DestinationBlend = graphicsDevice.RenderState.DestinationBlend;
            Blend SourceBlend = graphicsDevice.RenderState.SourceBlend;
            BlendFunction AlphaBlendOperation = graphicsDevice.RenderState.AlphaBlendOperation;
            bool DepthBufferEnable = graphicsDevice.RenderState.DepthBufferEnable;
            // Set render state for drawing precipitation
            graphicsDevice.RenderState.AlphaBlendEnable = true;
            graphicsDevice.RenderState.AlphaBlendOperation = BlendFunction.Add;
            graphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
            graphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
            graphicsDevice.RenderState.AlphaTestEnable = false;
            graphicsDevice.RenderState.DepthBufferEnable = true;
            // Enable point sprites
            graphicsDevice.RenderState.PointSpriteEnable = true;

            PrecipShader.Begin();
            foreach (EffectPass pass in PrecipShader.CurrentTechnique.Passes)
            {
                pass.Begin();

                foreach (var item in renderItems)
                {
                    PrecipShader.SetMatrix(item.XNAMatrix, ref XNAViewMatrix, ref Camera.XNASkyProjection);
                    PrecipShader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                pass.End();
            }
            PrecipShader.End();

            // Restore the pre-existing render state
            graphicsDevice.RenderState.PointSpriteEnable = false;
            graphicsDevice.RenderState.AlphaBlendEnable = AlphaBlendEnable;
            graphicsDevice.RenderState.AlphaBlendOperation = AlphaBlendOperation;
            graphicsDevice.RenderState.AlphaTestEnable = AlphaTestEnable;
            graphicsDevice.RenderState.DestinationBlend = DestinationBlend;
            graphicsDevice.RenderState.SourceBlend = SourceBlend;
            graphicsDevice.RenderState.DepthBufferEnable = DepthBufferEnable;
        }

        // Is this needed? PrecipMaterial doesn't change any of these render states.
		public override void ResetState(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.RenderState.DepthBufferFunction = CompareFunction.LessEqual;
            graphicsDevice.RenderState.DepthBufferWriteEnable = true;
        }

		public override bool GetBlending(RenderPrimitive renderPrimitive)
		{
			return true;
		}
	}
	#endregion

    #region Dynatrack material
    public class DynatrackMaterial : Material
    {
        SceneryShader SceneryShader;
        Texture2D image1;
        Texture2D image1s;
        Texture2D image2;
        string texturePath;
        public RenderProcess RenderProcess;

        public DynatrackMaterial(RenderProcess renderProcess)
        {
            TrProfile profile = renderProcess.Viewer.Simulator.TrackProfile;
            
            RenderProcess = renderProcess;
            SceneryShader = Materials.SceneryShader;
            texturePath = RenderProcess.Viewer.Simulator.RoutePath + @"\textures" + @"\" + profile.Image1Name; 
            image1 = SharedTextureManager.Get(renderProcess.GraphicsDevice, texturePath);
            texturePath = RenderProcess.Viewer.Simulator.RoutePath + @"\textures\snow" + @"\" + profile.Image1sName; 
            if (File.Exists(texturePath))
                image1s = SharedTextureManager.Get(renderProcess.GraphicsDevice, texturePath);
            else // Use file in base texture folder
                image1s = image1;
            texturePath = RenderProcess.Viewer.Simulator.RoutePath + @"\textures" + @"\" + profile.Image2Name; 
            image2 = SharedTextureManager.Get(renderProcess.GraphicsDevice, texturePath);
        }

        public override void Render(GraphicsDevice graphicsDevice, List<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            RenderProcess.RenderStateChangesCount++;
            RenderProcess.ImageChangesCount++;

            // Save existing render state
            CullMode cullMode = graphicsDevice.RenderState.CullMode;
            bool alphaBlendEnable = graphicsDevice.RenderState.AlphaBlendEnable;
            Blend destinationBlend = graphicsDevice.RenderState.DestinationBlend;
            Blend sourceBlend = graphicsDevice.RenderState.SourceBlend;
            bool alphaTestEnable = graphicsDevice.RenderState.AlphaTestEnable;

            Matrix viewproj = XNAViewMatrix * XNAProjectionMatrix;

            // This is BAD BAD BAD, visibility testing shouldn't be in here.
            foreach (var item in renderItems)
            {

                DynatrackMesh mesh = (DynatrackMesh)item.RenderPrimitive;
                Vector3 mstsLocation = mesh.MSTSLODCenter;

                // Test if object behind camera:
                if (RenderProcess.Viewer.Camera.InFOV(mstsLocation, mesh.ObjectRadius))
                {
                    for (uint i = 0; i < mesh.LODGrid.Length; i++)
                    {
                        bool test = RenderProcess.Viewer.Camera.InRange(mstsLocation,
                                            mesh.TrProfile.TrProfileLODItems[i].CutoffRadius);
                        if (!test) continue;
                        //TODO: All of the content of the switch block below needs to be
                        //      replaced by general state handling, driven from track profile.
                        //      BUT I'LL DEFER TO THE DEMANDS OF TRACK DESIGNERS FOR NOW.

                        // The following switch block serves pro tempore to get the job done
                        switch (i)
                        {
                            case 0: // Ballast
                                SceneryShader.CurrentTechnique = SceneryShader.Techniques["Image"];

                                if (RenderProcess.Viewer.Simulator.Weather == MSTS.WeatherType.Snow ||
                                    RenderProcess.Viewer.Simulator.Season == MSTS.SeasonType.Winter)
                                    SceneryShader.ImageMap_Tex = image1s;
                                else
                                    SceneryShader.ImageMap_Tex = image1;

                                // Set render state for drawing ballast
                                graphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
                                graphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
                                graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;

                                // TODO: SceneryShader.SpecularPower = 0;
                                break;
                            case 1: // Rail tops
                                SceneryShader.ImageMap_Tex = image2;
                                // TODO: SceneryShader.SpecularPower = 64;
                                break;
                            case 2: // Rail sides
                                // TODO: SceneryShader.SpecularPower = 0;
                                break;

                            default: continue;
                        } // end switch i

                        // The following are controlled by options in the track profile
                        graphicsDevice.SamplerStates[0].MipMapLevelOfDetailBias =
                                    mesh.TrProfile.TrProfileLODItems[i].MipMapLevelOfDetailBias;
                        graphicsDevice.RenderState.AlphaBlendEnable =
                            mesh.TrProfile.TrProfileLODItems[i].AlphaBlendEnable;
                        graphicsDevice.RenderState.AlphaTestEnable =
                            mesh.TrProfile.TrProfileLODItems[i].AlphaTestEnable;

                        SceneryShader.SetMatrix(item.XNAMatrix, ref XNAViewMatrix, ref viewproj);
                        SceneryShader.ZBias = item.RenderPrimitive.ZBias;

                        mesh.DrawIndex = (int)i;
                        SceneryShader.Begin();
                        foreach (EffectPass pass in SceneryShader.CurrentTechnique.Passes)
                        {
                            pass.Begin();
                            item.RenderPrimitive.Draw(graphicsDevice);
                            pass.End();
                        }
                        SceneryShader.End();
                    } // end for i
                } // end if not behind camera
            }
               
            // Restore the pre-existing render state
            graphicsDevice.RenderState.AlphaBlendEnable = alphaBlendEnable;
            graphicsDevice.RenderState.DestinationBlend = destinationBlend;
            graphicsDevice.RenderState.SourceBlend = sourceBlend;
            graphicsDevice.RenderState.AlphaTestEnable = alphaTestEnable;
            graphicsDevice.RenderState.CullMode = cullMode;
        }

		public override bool GetBlending(RenderPrimitive renderPrimitive)
		{
			return true;
		}
    }
    #endregion

    #region Forest material
    public class ForestMaterial : Material
    {
        SceneryShader SceneryShader;
        Texture2D TreeTexture = null;
        public RenderProcess RenderProcess;  // for diagnostics only
        public ForestDrawer drawer;

        public ForestMaterial(RenderProcess renderProcess, string treeTexture)
        {
            RenderProcess = renderProcess;
            SceneryShader = Materials.SceneryShader;
            TreeTexture = SharedTextureManager.Get(renderProcess.GraphicsDevice, treeTexture);
        }

		public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
		{
			if (previousMaterial == null || this.GetType() != previousMaterial.GetType())
			{
				RenderProcess.RenderStateChangesCount++;
				graphicsDevice.RenderState.AlphaBlendEnable = false;
				graphicsDevice.RenderState.AlphaTestEnable = true;
				graphicsDevice.RenderState.AlphaFunction = CompareFunction.GreaterEqual;
				graphicsDevice.RenderState.ReferenceAlpha = 200;
			}

			RenderProcess.ImageChangesCount++;
			SceneryShader.ImageMap_Tex = TreeTexture;
		}

        public override void Render(GraphicsDevice graphicsDevice, List<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            Matrix viewproj = XNAViewMatrix * XNAProjectionMatrix;

            SceneryShader.CurrentTechnique = SceneryShader.Techniques["Forest"];

            SceneryShader.Begin();
            foreach (EffectPass pass in SceneryShader.CurrentTechnique.Passes)
            {
                pass.Begin();

                foreach (var item in renderItems)
                {
                    SceneryShader.SetMatrix(item.XNAMatrix, ref XNAViewMatrix, ref viewproj);
                    SceneryShader.ZBias = item.RenderPrimitive.ZBias;
                    SceneryShader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                pass.End();
            }
            SceneryShader.End();
        }
	}
    #endregion

    #region LightGlow material
    public class LightGlowMaterial : Material
    {
        LightGlowShader LightGlowShader;
        Texture2D lightGlowTexture;
        public RenderProcess RenderProcess;
        int lastLightState = 0, currentLightState = 0;
        double fadeTimer = 0;

        public LightGlowMaterial(RenderProcess renderProcess)
        {
            RenderProcess = renderProcess;
            LightGlowShader = Materials.LightGlowShader;
            lightGlowTexture = renderProcess.Content.Load<Texture2D>("Lightglow");
        }

        public override void Render(GraphicsDevice graphicsDevice, List<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            

            RenderProcess.RenderStateChangesCount++;
            RenderProcess.ImageChangesCount++;

            LightGlowShader.CurrentTechnique = LightGlowShader.Techniques["LightGlow"];
            LightGlowShader.LightGlowTexture = lightGlowTexture;

            // Lights fade-in / fade-out
            currentLightState = RenderProcess.Viewer.PlayerLocomotive.Headlight;
            if (currentLightState != lastLightState)
            {
                if (currentLightState == 1 && lastLightState == 0)
                    LightGlowShader.StateChange = 1;
                else if (currentLightState == 2 && lastLightState == 1)
                    LightGlowShader.StateChange = 2;
                else if (currentLightState == 1 && lastLightState == 2)
                    LightGlowShader.StateChange = 3;
                else if (currentLightState == 0 && lastLightState == 1)
                    LightGlowShader.StateChange = 4;
                // Reset fade timer
                fadeTimer = RenderProcess.Viewer.Simulator.ClockTime;
                lastLightState = currentLightState;
            }
            LightGlowShader.FadeTime = (float)(RenderProcess.Viewer.Simulator.ClockTime - fadeTimer);

            // Set render state for drawing lights
            graphicsDevice.RenderState.AlphaBlendEnable = true;
            graphicsDevice.RenderState.AlphaBlendOperation = BlendFunction.Add;
            graphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
            graphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
            graphicsDevice.RenderState.AlphaTestEnable = false;
            graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;

            LightGlowShader.Begin();
            foreach (EffectPass pass in LightGlowShader.CurrentTechnique.Passes)
            {
                pass.Begin();

                foreach (var item in renderItems)
                {
                    Matrix wvp = item.XNAMatrix * XNAViewMatrix * Camera.XNASkyProjection;
                    LightGlowShader.SetMatrix(ref wvp);
                    LightGlowShader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                pass.End();
            }
            LightGlowShader.End();
        }

		public override bool GetBlending(RenderPrimitive renderPrimitive)
		{
			return true;
		}
	}
    #endregion
    
    #region Water material
    public class WaterMaterial : Material
    {
        public readonly RenderProcess RenderProcess;  // for diagnostics only
        readonly SceneryShader SceneryShader;
        readonly Texture2D WaterTexture;

		public WaterMaterial(RenderProcess renderProcess, string waterTexturePath)
		{
			RenderProcess = renderProcess;
			SceneryShader = Materials.SceneryShader;
			WaterTexture = SharedTextureManager.Get(renderProcess.GraphicsDevice, waterTexturePath);
		}

		public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
		{
			RenderProcess.RenderStateChangesCount++;

			graphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
			graphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
			graphicsDevice.SamplerStates[0].MipMapLevelOfDetailBias = 0;

			graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
			SceneryShader.CurrentTechnique = SceneryShader.Techniques["Image"];
			graphicsDevice.RenderState.AlphaTestEnable = false;
			graphicsDevice.RenderState.AlphaBlendEnable = true;
			graphicsDevice.RenderState.AlphaBlendOperation = BlendFunction.Add;
			graphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
			graphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;

			RenderProcess.ImageChangesCount++;
			SceneryShader.ImageMap_Tex = WaterTexture;

			graphicsDevice.VertexDeclaration = WaterTile.PatchVertexDeclaration;
		}

        public override void Render(GraphicsDevice graphicsDevice, List<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
			var viewproj = XNAViewMatrix * XNAProjectionMatrix;

            SceneryShader.Begin();
            foreach (var pass in SceneryShader.CurrentTechnique.Passes)
            {
                pass.Begin();
                foreach (var item in renderItems)
                {
                    SceneryShader.SetMatrix(item.XNAMatrix, ref XNAViewMatrix, ref viewproj);
                    SceneryShader.ZBias = item.RenderPrimitive.ZBias;
                    SceneryShader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                pass.End();
            }
            SceneryShader.End();
        }

		public override bool GetBlending(RenderPrimitive renderPrimitive)
		{
			return true;
		}
	}
    #endregion

    #region Shadow Map material
    public class ShadowMapMaterial : Material
    {
		readonly public RenderProcess RenderProcess;  // for diagnostics only

        public ShadowMapMaterial(RenderProcess renderProcess)
        {
			RenderProcess = renderProcess;
        }

        public void SetState(GraphicsDevice graphicsDevice, bool blocker)
        {
            var shader = Materials.ShadowMapShader;
            shader.CurrentTechnique = shader.Techniques[blocker ? "ShadowMapBlocker" : "ShadowMap"];

            RenderState rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = false;
            rs.AlphaTestEnable = false;
            rs.CullMode = blocker ? CullMode.CullClockwiseFace : CullMode.CullCounterClockwiseFace;
            rs.DepthBufferFunction = CompareFunction.LessEqual;
            rs.DepthBufferWriteEnable = true;
            rs.DestinationBlend = Blend.Zero;
            rs.FillMode = FillMode.Solid;
            rs.FogEnable = false;
            rs.RangeFogEnable = false;
        }

        public void Render(GraphicsDevice graphicsDevice, List<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Materials.ShadowMapShader;

            Matrix viewproj = XNAViewMatrix * XNAProjectionMatrix;
			
			shader.Begin();
			foreach (EffectPass pass in shader.CurrentTechnique.Passes)
			{
				pass.Begin();
                foreach (var item in renderItems)
                {
                    Matrix wvp = item.XNAMatrix * viewproj;
                    shader.SetData(ref wvp, item.Material.GetShadowTexture(item.RenderPrimitive));
                    shader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
				pass.End();
			}
			shader.End();
		}
	}
    #endregion

	#region Popup Window material
	public class PopupWindowMaterial : Material
	{
		public SpriteFont DefaultFont;

		public PopupWindowMaterial(RenderProcess renderProcess)
		{
			DefaultFont = renderProcess.Content.Load<SpriteFont>("Arial");
		}

		public void SetState(GraphicsDevice graphicsDevice, Texture2D screen)
		{
			var shader = Materials.PopupWindowShader;
			shader.CurrentTechnique = screen == null ? shader.Techniques["PopupWindow"] : shader.Techniques["PopupWindowGlass"];
			shader.Screen = screen;
			shader.GlassColor = Color.Black;

			var rs = graphicsDevice.RenderState;
			rs.AlphaBlendEnable = true;
			rs.AlphaFunction = CompareFunction.Greater;
			rs.AlphaTestEnable = true;
			rs.CullMode = CullMode.None;
			rs.DepthBufferEnable = false;
			rs.DepthBufferFunction = CompareFunction.Always;
			rs.DepthBufferWriteEnable = false;
		}

        public void Render(GraphicsDevice graphicsDevice, RenderPrimitive renderPrimitive, ref Matrix XNAWorldMatrix, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Materials.PopupWindowShader;

            Matrix wvp = XNAWorldMatrix * XNAViewMatrix * XNAProjectionMatrix;
            shader.SetMatrix(XNAWorldMatrix, ref wvp);

            shader.Begin();
            foreach (EffectPass pass in shader.CurrentTechnique.Passes)
            {
                pass.Begin();
                renderPrimitive.Draw(graphicsDevice);
                pass.End();
            }
            shader.End();
        }

		public override void ResetState(GraphicsDevice graphicsDevice)
		{
			var rs = graphicsDevice.RenderState;
			rs.AlphaBlendEnable = false;
			rs.AlphaFunction = CompareFunction.Always;
			rs.AlphaTestEnable = false;
			rs.CullMode = CullMode.CullCounterClockwiseFace;
			rs.DepthBufferEnable = true;
			rs.DepthBufferFunction = CompareFunction.LessEqual;
			rs.DepthBufferWriteEnable = true;
		}

		public override bool GetBlending(RenderPrimitive renderPrimitive)
		{
			return true;
		}
	}
	#endregion

	#region Yellow (testing) material
    /// <summary>
    /// This material is used for debug and testing.
    /// </summary>
    public class YellowMaterial : Material
    {
        static BasicEffect basicEffect = null;
        RenderProcess RenderProcess;

        public YellowMaterial(RenderProcess renderProcess)
        {
            RenderProcess = renderProcess;
            if( basicEffect == null )
            {
                basicEffect = new BasicEffect(renderProcess.GraphicsDevice, null);
                basicEffect.Alpha = 1.0f;
                basicEffect.DiffuseColor = new Vector3(197.0f/255.0f, 203.0f/255.0f, 37.0f/255.0f);
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
			RenderProcess.RenderStateChangesCount++;

			graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
			//Materials.SetupFog(graphicsDevice);

			graphicsDevice.VertexDeclaration = WaterTile.PatchVertexDeclaration;
		}

        public override void Render(GraphicsDevice graphicsDevice, List<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
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
    #endregion
}
