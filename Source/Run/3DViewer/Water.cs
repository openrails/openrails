/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
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
using System.Threading;
using MSTS;

namespace ORTS
{
    public class WaterTile : DrawableGameComponent
    {

        public int TileX, TileZ;
        public Viewer Viewer;

        // these can be shared since they are the same for all patches
        static VertexDeclaration PatchVertexDeclaration = null;
        static int PatchVertexStride;  // in bytes
        static Texture2D WaterTexture = null;

        private IndexBuffer TileIndexBuffer = null;
        private VertexBuffer TileVertexBuffer;
        private WorldLocation TileWorldLocation;
        private int TriangleCount = 0;


        public WaterTile(Viewer viewer, int tileX, int tileZ): base(viewer)
        {
            Viewer = viewer;
            TileX = tileX;
            TileZ = tileZ;

            if( viewer.Tiles.GetTile( tileX, tileZ ) == null )
                return;

            viewer.Components.Add(this);
        }

        public void Dispose()
        {
            Viewer.Components.Remove(this);
            base.Dispose();
        }

        protected override void LoadContent()
        {
            if (WaterTexture == null)
            {
                try
                {
                    // TODO, for now, top layer only
                    if (Viewer.ENVFile.WaterTextureNames.Count > 0)
                    {
                        string waterTextureName = Viewer.ENVFile.WaterTextureNames[Viewer.ENVFile.WaterTextureNames.Count - 1];  // TODO, render lower water layers
                        WaterTexture = SharedTextureManager.Get(GraphicsDevice, Viewer.Simulator.RoutePath + @"\envfiles\textures\" + waterTextureName);
                    }
                }
                catch (System.Exception error)
                {
                    Console.Error.WriteLine("Problem creating water: " + error.Message);
                    WaterTexture = null;
                    return;
                }
            }

            if (WaterTexture != null)

            // vertex type declaration to be shared by all tiles
            if (PatchVertexDeclaration == null)
            {
                PatchVertexDeclaration = new VertexDeclaration(this.GraphicsDevice, VertexPositionNormalTexture.VertexElements);
                PatchVertexStride = VertexPositionNormalTexture.SizeInBytes;
            }


            Tile tile = Viewer.Tiles.GetTile(TileX, TileZ);
            TFile tFile = tile.TFile;
            TileWorldLocation = new WorldLocation(TileX, TileZ, 0, 0, 0);

            ORTSMath.Matrix2x2 waterLevels = new ORTSMath.Matrix2x2(tFile.WaterSW, tFile.WaterSE, tFile.WaterNW, tFile.WaterNE);

            // Set up one indexBuffer 
            {
                int indexCount = 16 * 16 * 2 * 3;  // 16 x 16 squares * 2 triangles per square * 3 indices per triangle
                short[] indexData = new short[indexCount];

                int iIndex = 0;
                // for each 128 meter patch
                for (int iz = 0; iz < 16; ++iz)
                    for (int ix = 0; ix < 16; ++ix)
                    {
                        terrain_patchset_patch patch = tFile.terrain.terrain_patchsets[0].GetPatch(ix,iz);

                        if (patch.WaterEnabled)
                        {
                            short nw = (short)(ix + iz * 17);  // vertice index in the north west corner
                            short ne = (short)(nw + 1);
                            short sw = (short)(nw + 17);
                            short se = (short)(sw + 1);

                            TriangleCount += 2;

                            if (((ix & 1) == (iz & 1)))  // triangles alternate
                            {
                                indexData[iIndex++] = nw;
                                indexData[iIndex++] = se;
                                indexData[iIndex++] = sw;
                                indexData[iIndex++] = se;
                                indexData[iIndex++] = nw;
                                indexData[iIndex++] = ne;
                            }
                            else
                            {
                                indexData[iIndex++] = se;
                                indexData[iIndex++] = sw;
                                indexData[iIndex++] = ne;
                                indexData[iIndex++] = nw;
                                indexData[iIndex++] = ne;
                                indexData[iIndex++] = sw;
                            }
                        }
                    }
                TileIndexBuffer = new IndexBuffer(GraphicsDevice, sizeof(short) * iIndex, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
                TileIndexBuffer.SetData<short>(indexData, 0, iIndex);
            }



            // Set up the vertex buffer
            VertexPositionNormalTexture[] vertexData = new VertexPositionNormalTexture[17 * 17];
            int iV = 0;
            // for each vertex - starting in SW corner
            for (int iz = 16; iz >= 0; --iz)
                for (int ix = 0; ix < 17; ++ix)
                {
                    float xp = (float)ix / 16.0f;  // make it 0-1
                    float zp = (float)iz / 16.0f;  // make it 0-1
                    float y = ORTSMath.Interpolate2D(xp, zp, waterLevels);

                    float x = -1024 + xp * 2048f;  // make it -1024 to +1024
                    float z = -1024 + zp * 2048f;

                    vertexData[iV].Position = new Vector3(x, y, -z);
                    vertexData[iV].TextureCoordinate = new Vector2(xp, zp);
                    vertexData[iV].Normal = new Vector3(0, 1, 0);
                    iV++;

                    // TODO deal with disabled patches
                }
            TileVertexBuffer = new VertexBuffer(GraphicsDevice, VertexPositionNormalTexture.SizeInBytes * vertexData.Length, BufferUsage.WriteOnly);
            TileVertexBuffer.SetData(vertexData);

            base.LoadContent();
        }



        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Draw(GameTime gameTime)
        {
            if (WaterTexture == null)  // if there was a problem loading the water texture
                return;

            int dTileX = TileX - Viewer.Camera.TileX;
            int dTileZ = TileZ - Viewer.Camera.TileZ;

            Vector3 xnaTileLocation = Viewer.Camera.XNALocation(TileWorldLocation);

            // Distance cull
            if (Viewer.Camera.CanSee(xnaTileLocation, 3500f, 2000f))
            {
                Matrix xnaTilePosition = Matrix.Identity;
                xnaTilePosition.Translation = xnaTileLocation;
                Viewer.SceneryShader.SetMatrix(xnaTilePosition, Viewer.Camera.XNAView, Viewer.Camera.XNAProjection);

                // These settings are constant for all patches so we set up only when needed
                // TODO, there is something wrong here- StaticShape must be called before
                // this works.
                if (Viewer.RenderState != 5)
                {

                    Viewer.RenderState = 5;
                    GraphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
                    GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
                    GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
                    GraphicsDevice.VertexDeclaration = PatchVertexDeclaration;
                    GraphicsDevice.RenderState.DepthBias = 0f;

                    GraphicsDevice.VertexSamplerStates[0].AddressU = TextureAddressMode.Wrap;
                    GraphicsDevice.VertexSamplerStates[0].AddressV = TextureAddressMode.Wrap;
                    GraphicsDevice.RenderState.AlphaFunction = CompareFunction.Always;


                    GraphicsDevice.VertexDeclaration = PatchVertexDeclaration;

                    Viewer.SceneryShader.CurrentTechnique = Viewer.SceneryShader.Techniques[0];

                    GraphicsDevice.RenderState.AlphaTestEnable = false;
                    GraphicsDevice.RenderState.ReferenceAlpha = 200;  // setting this to 128, chain link fences become solid at distance, at 200, they become transparent
                    GraphicsDevice.RenderState.AlphaFunction = CompareFunction.GreaterEqual;        // if alpha > reference, then skip processing this pixel

                    Viewer.SetupFog();
                }

                // These items change per Draw call
                Viewer.SceneryShader.Texture = WaterTexture;

                GraphicsDevice.Indices = TileIndexBuffer;
                GraphicsDevice.Vertices[0].SetSource(this.TileVertexBuffer, 0, PatchVertexStride);

                Viewer.SceneryShader.Begin();
                foreach (EffectPass pass in Viewer.SceneryShader.CurrentTechnique.Passes)
                {
                    pass.Begin();
                    GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 17 * 17, 0, TriangleCount);
                    pass.End();
                }
                Viewer.SceneryShader.End();

            }
            base.Draw(gameTime);
        }

    } // class watertile


}
