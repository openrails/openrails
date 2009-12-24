using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace ORTS
{
    public class Materials
    {
        public static SceneryShader SceneryShader = null;
        private static SpriteBatchMaterial SpriteBatchMaterial = null;
        private static WaterMaterial WaterMaterial = null;
        private static SkyMaterial SkyMaterial = null;
        private static Dictionary<string, TerrainMaterial> TerrainMaterials = new Dictionary<string, TerrainMaterial>();
        private static Dictionary<string, SceneryMaterial> SceneryMaterials = new Dictionary<string, SceneryMaterial>();
        public static Texture2D MissingTexture = null;  // sub this when we are missing the required texture

        public static Material Load(RenderProcess renderProcess, string materialName)
        {
            return Load(renderProcess, materialName, null, 0);
        }
        public static Material Load(RenderProcess renderProcess, string materialName, string textureName)
        {
            return Load(renderProcess, materialName, textureName, 0);
        }

        public static Material Load(RenderProcess renderProcess, string materialName, string textureName, int options )
        {
            if( textureName != null )
                textureName = textureName.ToLower();

            if (MissingTexture == null)
                MissingTexture = renderProcess.Content.Load<Texture2D>("blank");

            if (SceneryShader == null)
            {
                SceneryShader = new SceneryShader(renderProcess.GraphicsDevice, renderProcess.Content);
                SceneryShader.BumpTexture = MSTS.ACEFile.Texture2DFromFile(renderProcess.GraphicsDevice, renderProcess.Viewer.Simulator.RoutePath + @"\TERRTEX\microtex.ace");

            }

            switch( materialName )
            {
                case "SpriteBatch":
                    if (SpriteBatchMaterial == null)
                        SpriteBatchMaterial = new SpriteBatchMaterial(renderProcess);
                    return SpriteBatchMaterial;
                case "Terrain":
                    if (!TerrainMaterials.ContainsKey(textureName))
                    {
                        TerrainMaterial terrainMaterial = new TerrainMaterial(renderProcess, textureName);
                        TerrainMaterials.Add(textureName, terrainMaterial);
                        return terrainMaterial;
                    }
                    else
                    {
                        return TerrainMaterials[textureName];
                    }
                case "SceneryMaterial":
                    string key;
                    if (textureName != null)
                        key = options.ToString() + ":" + textureName;
                    else
                        key = options.ToString() + ":";
                    if (!SceneryMaterials.ContainsKey(key))  
                    {
                        SceneryMaterial sceneryMaterial = new SceneryMaterial(renderProcess, textureName);
                        sceneryMaterial.Options = options;
                        SceneryMaterials.Add(key, sceneryMaterial);
                        return sceneryMaterial;
                    }
                    else
                    {
                        return SceneryMaterials[key];
                    }
                case "WaterMaterial":
                    if (WaterMaterial == null)
                        WaterMaterial = new WaterMaterial(renderProcess, textureName);
                    return WaterMaterial;
                case "SkyMaterial":
                    if (SkyMaterial == null)
                        SkyMaterial = new SkyMaterial(renderProcess);
                    return SkyMaterial; 
                default:
                    return Load(renderProcess, "ScenerMaterial");
            }
            
        }

        public static float ViewingDistance = 2000;  // TODO, this is awkward, viewer must set this to control fog

        /// <summary>
        /// Setup the renderstate for fog
        /// </summary>
        public static void SetupFog(GraphicsDevice graphicsDevice )
        {
            graphicsDevice.RenderState.FogEnable = true;
            graphicsDevice.RenderState.FogVertexMode = FogMode.None;  // vertex fog
            graphicsDevice.RenderState.FogTableMode = FogMode.Linear;     // pixel fog off
            graphicsDevice.RenderState.FogColor = new Color(189, 189, 189, 255); // new Color(128, 128, 128, 255);
            graphicsDevice.RenderState.FogDensity = 1.0f;                      // used for exponential fog only, not linear
            graphicsDevice.RenderState.FogEnd = ViewingDistance; // +300;
            graphicsDevice.RenderState.FogStart = ViewingDistance / 2;
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
                try { 
                    Texture2D texture = MSTS.ACEFile.Texture2DFromFile(device, path);
                    SharedTextures.Add(path, texture);
                    return texture;
                }
                catch (System.Exception error)
                {
                    Console.Error.WriteLine("While loading " + path + " " + error.Message);
                    return Materials.MissingTexture;
                }
            }
            else
            {
                return SharedTextures[path];
            }
        }
    }

    public interface Material
    {
        /// <summary>
        /// Have the material shader render the primitive.  Some shaders require multple passes
        /// Use previousMaterial to optimize the state change
        /// </summary>
        void Render(GraphicsDevice graphicsDevice, Material previousMaterial, RenderPrimitive renderPrimitive,
                            ref Matrix XNAWorldMatrix, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix);
                
        /// <summary>
        /// Use nextMaterial to optimize the state change
        /// </summary>
        void ResetState(GraphicsDevice graphicsDevice, Material nextMaterial);
    }

    public class EmptyMaterial : Material
    {
        public void Render(GraphicsDevice graphicsDevice, Material previousMaterial, RenderPrimitive renderPrimitive,
                            ref Matrix XNAWorldMatrix, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix ) 
        { 
        }
        public void ResetState(GraphicsDevice graphicsDevice, Material nextMaterial) { }
    }


    public class SpriteBatchMaterial:  Material
    {
        public SpriteBatch SpriteBatch;
        public SpriteFont DefaultFont;
        public RenderProcess RenderProcess;  // for diagnostics only

        public SpriteBatchMaterial( RenderProcess renderProcess )
        {
            RenderProcess = renderProcess;
            SpriteBatch = new SpriteBatch(renderProcess.GraphicsDevice);
            DefaultFont =  renderProcess.Content.Load<SpriteFont>("CourierNew");
        }

        public void Render(GraphicsDevice graphicsDevice, Material previousMaterial, RenderPrimitive renderPrimitive,
                           ref Matrix XNAWorldMatrix, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            if (previousMaterial != this)
            {
                RenderProcess.RenderStateChangesCount++;
                SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.SaveState);
            }
            RenderProcess.PrimitiveCount++;
            renderPrimitive.Draw(graphicsDevice);
        }              // TODO Note- using negative DepthBias anywhere results text being drawn underneath the scenery.
        

        public void ResetState(GraphicsDevice graphicsDevice, Material nextMaterial )
        {
            if( nextMaterial != this )
                SpriteBatch.End();
        }
    }

    public class SceneryMaterial : Material
    {
        public int Options = 0;
        SceneryShader SceneryShader;
        Texture2D Texture;
        public RenderProcess RenderProcess;  // for diagnostics only

        public SceneryMaterial(RenderProcess renderProcess, string texturePath)
        {
            RenderProcess = renderProcess;
            SceneryShader = Materials.SceneryShader;
            Texture = SharedTextureManager.Get(renderProcess.GraphicsDevice, texturePath);
        }

        public void Render(GraphicsDevice graphicsDevice, Material previousMaterial, RenderPrimitive renderPrimitive,
                           ref Matrix XNAWorldMatrix, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {

            SceneryShader.SetMatrix(XNAWorldMatrix, XNAViewMatrix, XNAProjectionMatrix);
                  
            if ( previousMaterial == null || this.GetType() != previousMaterial.GetType())
            {
                RenderProcess.RenderStateChangesCount++;
                graphicsDevice.RenderState.DepthBias = 0;
                graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
                graphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
                graphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
                graphicsDevice.VertexSamplerStates[0].AddressU = TextureAddressMode.Wrap;
                graphicsDevice.VertexSamplerStates[0].AddressV = TextureAddressMode.Wrap;
                graphicsDevice.RenderState.AlphaFunction = CompareFunction.Always;
                graphicsDevice.RenderState.AlphaTestEnable = true;
                graphicsDevice.RenderState.ReferenceAlpha = 200;  // setting this to 128, chain link fences become solid at distance, at 200, they become transparent
                graphicsDevice.RenderState.AlphaFunction = CompareFunction.GreaterEqual;        // if alpha > reference, then skip processing this pixel

                Materials.SetupFog(graphicsDevice);
            }

            if (this != previousMaterial)
            {
                RenderProcess.ImageChangesCount++;
                SceneryShader.Texture = Texture;
                
                if ( (Options & 1) == 1)
                {
                    SceneryShader.CurrentTechnique = SceneryShader.Techniques[1];
                }
                else
                {
                    SceneryShader.CurrentTechnique = SceneryShader.Techniques[0];
                }
                if ( (Options & 2) == 2)
                {
                    graphicsDevice.RenderState.AlphaTestEnable = true;
                }
                else
                {
                    graphicsDevice.RenderState.AlphaTestEnable = false;
                }
            }

            // With the GPU configured, now we can draw the primitive
            SceneryShader.Begin();
            EffectPass pass = SceneryShader.CurrentTechnique.Passes[0];  // we know this is a one pass shader
            pass.Begin();
            RenderProcess.PrimitiveCount++;
            renderPrimitive.Draw(graphicsDevice);
            pass.End();
            SceneryShader.End();
        }

        public void ResetState(GraphicsDevice graphicsDevice, Material nextMaterial)
        {
            if (this != nextMaterial)
            {
            }
        }
    }

    public class TerrainMaterial : Material
    {
        SceneryShader SceneryShader;
        Texture2D PatchTexture;
        public RenderProcess RenderProcess;  // for diagnostics only

        public TerrainMaterial(RenderProcess renderProcess, string terrainTexture )
        {
            RenderProcess = renderProcess;
            SceneryShader = Materials.SceneryShader;
            PatchTexture = SharedTextureManager.Get(renderProcess.GraphicsDevice, terrainTexture);
        }

        public void Render(GraphicsDevice graphicsDevice, Material previousMaterial, RenderPrimitive renderPrimitive,
                            ref Matrix XNAWorldMatrix, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            if ( previousMaterial == null || this.GetType() != previousMaterial.GetType())
            {
                RenderProcess.RenderStateChangesCount++;
                graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
                graphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
                graphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
                graphicsDevice.VertexDeclaration = TerrainPatch.PatchVertexDeclaration;
                graphicsDevice.RenderState.DepthBias = 0f;
                graphicsDevice.VertexSamplerStates[0].AddressU = TextureAddressMode.Wrap;
                graphicsDevice.VertexSamplerStates[0].AddressV = TextureAddressMode.Wrap;
                graphicsDevice.RenderState.AlphaFunction = CompareFunction.Always;
                SceneryShader.CurrentTechnique = SceneryShader.Techniques[2];
                graphicsDevice.RenderState.AlphaTestEnable = true;
                graphicsDevice.RenderState.ReferenceAlpha = 200;  // setting this to 128, chain link fences become solid at distance, at 200, they become transparent
                graphicsDevice.RenderState.AlphaFunction = CompareFunction.GreaterEqual;        // if alpha > reference, then skip processing this pixel
                Materials.SetupFog( graphicsDevice );

                graphicsDevice.VertexDeclaration = TerrainPatch.PatchVertexDeclaration;
                graphicsDevice.Indices = TerrainPatch.PatchIndexBuffer;
            }

            SceneryShader.SetMatrix(XNAWorldMatrix, XNAViewMatrix, XNAProjectionMatrix);

            if (this != previousMaterial)
            {
                RenderProcess.ImageChangesCount++;
                SceneryShader.Texture = PatchTexture;
            }

            SceneryShader.SetMatrix(XNAWorldMatrix, XNAViewMatrix, XNAProjectionMatrix);

            SceneryShader.Begin();
            foreach (EffectPass pass in SceneryShader.CurrentTechnique.Passes)
            {
                pass.Begin();
                RenderProcess.PrimitiveCount++;
                renderPrimitive.Draw(graphicsDevice);
                pass.End();
            }
            SceneryShader.End();
        }

        public void ResetState(GraphicsDevice graphicsDevice, Material nextMaterial)
        {
        }
    }

    public class SkyMaterial : Material
    {
        SceneryShader SceneryShader;
        Texture2D skyTexture;
        public RenderProcess RenderProcess;  // for diagnostics only

        public SkyMaterial(RenderProcess renderProcess)
        {
            RenderProcess = renderProcess;
            SceneryShader = Materials.SceneryShader;
            skyTexture = renderProcess.Content.Load<Texture2D>("sky");
        }

        public void Render(GraphicsDevice graphicsDevice, Material previousMaterial, RenderPrimitive renderPrimitive,
                            ref Matrix XNAWorldMatrix, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            SceneryShader.SetMatrix(XNAWorldMatrix, XNAViewMatrix, XNAProjectionMatrix);

            RenderProcess.RenderStateChangesCount++;
            RenderProcess.ImageChangesCount++;
            SceneryShader.CurrentTechnique = SceneryShader.Techniques[3];
            SceneryShader.Texture = skyTexture;
            // These parameter changes have no effect
            SceneryShader.Brightness = 1.0f;
            SceneryShader.Ambient = 1.0f;
            SceneryShader.Saturation = 1.0f;

            graphicsDevice.RenderState.CullMode = CullMode.None;
            graphicsDevice.RenderState.FillMode = FillMode.Solid;

            graphicsDevice.RenderState.FogVertexMode = FogMode.None;  // vertex fog
            graphicsDevice.RenderState.FogTableMode = FogMode.Linear;     // pixel fog off
            graphicsDevice.RenderState.FogColor = new Color(128, 128, 128, 255);
            graphicsDevice.RenderState.FogDensity = 1.0f;                      // used for exponential fog only, not linear
            graphicsDevice.RenderState.FogEnd = SkyConstants.skyRadius + 100;
            graphicsDevice.RenderState.FogStart = 1000f;
            graphicsDevice.RenderState.FogEnable = false;

            SceneryShader.Begin();
            foreach (EffectPass pass in SceneryShader.CurrentTechnique.Passes)
            {
                pass.Begin();
                RenderProcess.PrimitiveCount++;
                renderPrimitive.Draw(graphicsDevice);
                pass.End();
            }
            SceneryShader.End();

        }

        public void ResetState(GraphicsDevice graphicsDevice, Material nextMaterial)
        {
        }
    }


    public class WaterMaterial : Material
    {
        SceneryShader SceneryShader;
        static Texture2D WaterTexture = null;
        public RenderProcess RenderProcess;  // for diagnostics only

        public WaterMaterial(RenderProcess renderProcess, string waterTexturePath )
        {
            RenderProcess = renderProcess;
            SceneryShader = Materials.SceneryShader;
            if( WaterTexture == null )
                WaterTexture = SharedTextureManager.Get(renderProcess.GraphicsDevice, waterTexturePath);
        }

        public void Render(GraphicsDevice graphicsDevice, Material previousMaterial, RenderPrimitive renderPrimitive,
                            ref Matrix XNAWorldMatrix, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            SceneryShader.SetMatrix(XNAWorldMatrix, XNAViewMatrix, XNAProjectionMatrix);

            if (previousMaterial != this)
            {
                RenderProcess.RenderStateChangesCount++;

                graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
                graphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
                graphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
                graphicsDevice.RenderState.DepthBias = 0f;
                graphicsDevice.VertexSamplerStates[0].AddressU = TextureAddressMode.Wrap;
                graphicsDevice.VertexSamplerStates[0].AddressV = TextureAddressMode.Wrap;
                graphicsDevice.RenderState.AlphaFunction = CompareFunction.Always;
                SceneryShader.CurrentTechnique = SceneryShader.Techniques[2];
                graphicsDevice.RenderState.AlphaTestEnable = false;
                graphicsDevice.RenderState.ReferenceAlpha = 200;  
                graphicsDevice.RenderState.AlphaFunction = CompareFunction.GreaterEqual;        // if alpha > reference, then skip processing this pixel
                Materials.SetupFog(graphicsDevice);

                RenderProcess.ImageChangesCount++;
                SceneryShader.Texture = WaterTexture;

                graphicsDevice.VertexDeclaration = WaterTile.PatchVertexDeclaration;
            }


            SceneryShader.Begin();
            foreach (EffectPass pass in SceneryShader.CurrentTechnique.Passes)
            {
                pass.Begin();
                RenderProcess.PrimitiveCount++;
                renderPrimitive.Draw(graphicsDevice);
                pass.End();
            }
            SceneryShader.End();
        }

        public void ResetState(GraphicsDevice graphicsDevice, Material nextMaterial)
        {
        }
    }
}
