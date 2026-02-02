// COPYRIGHT 2012, 2013, 2014 by the Open Rails project.
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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Formats.Msts;
using Orts.Viewer3D.Common;
using ORTS.Common;
using System;
using System.Collections.Generic;

namespace Orts.Viewer3D
{
    public class TransferShape : StaticShape
    {
        readonly Material Material;
        readonly TransferPrimitive Primitive;
        readonly float Radius;

        public TransferShape(Viewer viewer, TransferObj transfer, WorldPosition position)
            : base(viewer, null, RemoveRotation(position), ShapeFlags.AutoZBias)
        {
            Material = viewer.MaterialManager.Load("Transfer", Helpers.GetTransferTextureFile(viewer.Simulator, transfer.FileName));
            Primitive = new TransferPrimitive(viewer, transfer.Width, transfer.Height, position);
            Radius = (float)Math.Sqrt(transfer.Width * transfer.Width + transfer.Height * transfer.Height) / 2;
        }

        static WorldPosition RemoveRotation(WorldPosition position)
        {
            var rv = new WorldPosition(position);
            var translation = rv.XNAMatrix.Translation;
            rv.XNAMatrix = Matrix.Identity;
            rv.XNAMatrix.Translation = translation;
            return rv;
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            var dTileX = Location.TileX - Viewer.Camera.TileX;
            var dTileZ = Location.TileZ - Viewer.Camera.TileZ;
            var mstsLocation = Location.Location + new Vector3(dTileX * 2048, 0, dTileZ * 2048);
            var xnaMatrix = Matrix.CreateTranslation(mstsLocation.X, mstsLocation.Y, -mstsLocation.Z);
            frame.AddAutoPrimitive(mstsLocation, Radius, float.MaxValue, Material, Primitive, RenderPrimitiveGroup.World, ref xnaMatrix, Flags);
        }

        internal override void Mark()
        {
            Material.Mark();
            base.Mark();
        }
    }

    public class TransferPrimitive : RenderPrimitive
    {
        readonly VertexBuffer VertexBuffer;
        readonly IndexBuffer IndexBuffer;
        readonly int PrimitiveCount;

        public TransferPrimitive(Viewer viewer, float width, float height, WorldPosition position)
        {
            var center = position.Location;
            var radius = (float)Math.Sqrt(width * width + height * height) / 2;
            var minX = (int)Math.Floor((center.X - radius) / 8);
            var maxX = (int)Math.Ceiling((center.X + radius) / 8);
            var minZ = (int)Math.Floor((center.Z - radius) / 8);
            var maxZ = (int)Math.Ceiling((center.Z + radius) / 8);
            var xnaRotation = position.XNAMatrix;
            xnaRotation.Translation = Vector3.Zero;
            Matrix.Invert(ref xnaRotation, out xnaRotation);

            var verticies = new VertexPositionTexture[(maxX - minX + 1) * (maxZ - minZ + 1)];
            for (var x = 0; x <= maxX - minX; x++)
            {
                for (var z = 0; z <= maxZ - minZ; z++)
                {
                    var i = x * (maxZ - minZ + 1) + z;
                    verticies[i].Position.X = (x + minX) * 8 - center.X;
                    verticies[i].Position.Y = viewer.Tiles.LoadAndGetElevation(position.TileX, position.TileZ, (x + minX) * 8, (z + minZ) * 8, false) - center.Y;
                    verticies[i].Position.Z = -(z + minZ) * 8 + center.Z;

                    var tc = new Vector3(verticies[i].Position.X, 0, verticies[i].Position.Z);
                    tc = Vector3.Transform(tc, xnaRotation);
                    verticies[i].TextureCoordinate.X = tc.X / width + 0.5f;
                    verticies[i].TextureCoordinate.Y = tc.Z / height + 0.5f;
                }
            }

            var indicies = new short[(maxX - minX) * (maxZ - minZ) * 6];
            for (var x = 0; x < maxX - minX; x++)
            {
                for (var z = 0; z < maxZ - minZ; z++)
                {
                    // Condition must match TerrainPatch.GetIndexBuffer's condition.
                    if (((x + minX) & 1) == ((z + minZ) & 1))
                    {
                        indicies[(x * (maxZ - minZ) + z) * 6 + 0] = (short)((x + 0) * (maxZ - minZ + 1) + (z + 0));
                        indicies[(x * (maxZ - minZ) + z) * 6 + 1] = (short)((x + 1) * (maxZ - minZ + 1) + (z + 1));
                        indicies[(x * (maxZ - minZ) + z) * 6 + 2] = (short)((x + 1) * (maxZ - minZ + 1) + (z + 0));
                        indicies[(x * (maxZ - minZ) + z) * 6 + 3] = (short)((x + 0) * (maxZ - minZ + 1) + (z + 0));
                        indicies[(x * (maxZ - minZ) + z) * 6 + 4] = (short)((x + 0) * (maxZ - minZ + 1) + (z + 1));
                        indicies[(x * (maxZ - minZ) + z) * 6 + 5] = (short)((x + 1) * (maxZ - minZ + 1) + (z + 1));
                    }
                    else
                    {
                        indicies[(x * (maxZ - minZ) + z) * 6 + 0] = (short)((x + 0) * (maxZ - minZ + 1) + (z + 1));
                        indicies[(x * (maxZ - minZ) + z) * 6 + 1] = (short)((x + 1) * (maxZ - minZ + 1) + (z + 1));
                        indicies[(x * (maxZ - minZ) + z) * 6 + 2] = (short)((x + 1) * (maxZ - minZ + 1) + (z + 0));
                        indicies[(x * (maxZ - minZ) + z) * 6 + 3] = (short)((x + 0) * (maxZ - minZ + 1) + (z + 0));
                        indicies[(x * (maxZ - minZ) + z) * 6 + 4] = (short)((x + 0) * (maxZ - minZ + 1) + (z + 1));
                        indicies[(x * (maxZ - minZ) + z) * 6 + 5] = (short)((x + 1) * (maxZ - minZ + 1) + (z + 0));
                    }
                }
            }

            VertexBuffer = new VertexBuffer(viewer.GraphicsDevice, typeof(VertexPositionTexture), verticies.Length, BufferUsage.WriteOnly);
            VertexBuffer.SetData(verticies);

            IndexBuffer = new IndexBuffer(viewer.GraphicsDevice, typeof(short), indicies.Length, BufferUsage.WriteOnly);
            IndexBuffer.SetData(indicies);
            PrimitiveCount = indicies.Length / 3;
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.SetVertexBuffer(VertexBuffer);
            graphicsDevice.Indices = IndexBuffer;
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, baseVertex: 0, startIndex: 0, PrimitiveCount);
        }
    }

    public class TransferMaterial : Material
    {
        readonly Texture2D Texture;
        IEnumerator<EffectPass> ShaderPasses;
        readonly SamplerState TransferSamplerState;

        public TransferMaterial(Viewer viewer, string textureName)
            : base(viewer, textureName)
        {
            Texture = Viewer.TextureManager.Get(textureName, true);
            TransferSamplerState = new SamplerState {
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                Filter = TextureFilter.Anisotropic,
                MaxAnisotropy = 16,
            };
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            var shader = Viewer.MaterialManager.SceneryShader;
            shader.CurrentTechnique = shader.Techniques["Transfer"];
            if (ShaderPasses == null) ShaderPasses = shader.CurrentTechnique.Passes.GetEnumerator();
            shader.ImageTexture = Texture;
            shader.ReferenceAlpha = 10;

            graphicsDevice.BlendState = BlendState.NonPremultiplied;
            graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.SceneryShader;

            shader.SetViewMatrix(ref XNAViewMatrix);
            ShaderPasses.Reset();
            while (ShaderPasses.MoveNext())
            {
                foreach (var item in renderItems)
                {
                    shader.SetMatrix(item.XNAMatrix, ref XNAViewMatrix, ref XNAProjectionMatrix);
                    shader.ZBias = item.RenderPrimitive.ZBias;
                    ShaderPasses.Current.Apply();
                    // SamplerStates can only be set after the ShaderPasses.Current.Apply().
                    graphicsDevice.SamplerStates[0] = TransferSamplerState;
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            var shader = Viewer.MaterialManager.SceneryShader;
            shader.ReferenceAlpha = 0;

            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
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
}
