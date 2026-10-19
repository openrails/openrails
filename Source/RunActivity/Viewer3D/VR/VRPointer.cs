// COPYRIGHT 2026 by the Open Rails project.
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

// The bits of 3D needed to point a controller at things: a ray from a hand pose, a laser line and
// a flat textured panel to draw, and a material that draws them with the loading screen's shader.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Orts.Viewer3D.VR
{
    public static class VRPointer
    {
        /// <summary>World-from-hand for a hand pose in the seat frame and the seat view (world-to-seat) of the camera.</summary>
        public static Matrix HandToWorld(Matrix handPose, Matrix seatView)
        {
            return handPose * Matrix.Invert(seatView);
        }

        /// <summary>The ray a controller points along: from its aim pose, out along its -Z.</summary>
        public static Ray RayFrom(Matrix handWorld)
        {
            return new Ray(handWorld.Translation, Vector3.Normalize(Vector3.TransformNormal(Vector3.Forward, handWorld)));
        }

        /// <summary>
        /// Where a ray meets a rectangle lying in the plane z = centre.Z (facing +Z, i.e. the viewer at the
        /// origin), as (u, v) in 0..1 from the top left, or null when it misses or points away.
        /// </summary>
        public static Vector2? HitPanel(Ray ray, Vector3 centre, float width, float height, out Vector3 hit)
        {
            hit = Vector3.Zero;
            if (Math.Abs(ray.Direction.Z) < 1e-5f)
                return null;
            var t = (centre.Z - ray.Position.Z) / ray.Direction.Z;
            if (t <= 0)
                return null;
            hit = ray.Position + ray.Direction * t;
            var u = (hit.X - centre.X) / width + 0.5f;
            var v = 0.5f - (hit.Y - centre.Y) / height;
            if (u < 0 || u > 1 || v < 0 || v > 1)
                return null;
            return new Vector2(u, v);
        }

        /// <summary>The controller body drawn at each hand: a block the size of a grip, behind and below the aim point.</summary>
        public static readonly Vector3 ControllerSize = new Vector3(0.035f, 0.045f, 0.10f);
        public static readonly Vector3 ControllerOffset = new Vector3(0, -0.02f, 0.06f);

        static readonly Dictionary<Color, Texture2D> SolidTextures = new Dictionary<Color, Texture2D>();

        /// <summary>A 1x1 texture of one colour, for drawing solid things with a textured shader.</summary>
        public static Texture2D SolidTexture(GraphicsDevice graphicsDevice, Color color)
        {
            lock (SolidTextures)
            {
                Texture2D texture;
                if (!SolidTextures.TryGetValue(color, out texture) || texture.IsDisposed || texture.GraphicsDevice != graphicsDevice)
                {
                    texture = new Texture2D(graphicsDevice, 1, 1, false, SurfaceFormat.Color);
                    texture.SetData(new[] { color });
                    SolidTextures[color] = texture;
                }
                return texture;
            }
        }
    }

    /// <summary>A rectangle of the given size centred on the origin in the XY plane, facing +Z, showing a texture; place it with the render item's matrix.</summary>
    public class VRQuadPrimitive : RenderPrimitive
    {
        readonly VertexBuffer VertexBuffer;

        public VRQuadPrimitive(GraphicsDevice graphicsDevice, float width, float height)
            : this(graphicsDevice, width, height, 0, 0, 1, 1)
        {
        }

        /// <summary>A quad showing only the part of the texture between (u0, v0) and (u1, v1).</summary>
        public VRQuadPrimitive(GraphicsDevice graphicsDevice, float width, float height, float u0, float v0, float u1, float v1)
        {
            var w = width / 2;
            var h = height / 2;
            var vertices = new[] {
                new VertexPositionTexture(new Vector3(-w, +h, 0), new Vector2(u0, v0)),
                new VertexPositionTexture(new Vector3(+w, +h, 0), new Vector2(u1, v0)),
                new VertexPositionTexture(new Vector3(-w, -h, 0), new Vector2(u0, v1)),
                new VertexPositionTexture(new Vector3(+w, -h, 0), new Vector2(u1, v1)),
            };
            VertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionTexture), vertices.Length, BufferUsage.WriteOnly);
            VertexBuffer.SetData(vertices);
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.SetVertexBuffer(VertexBuffer);
            graphicsDevice.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
        }
    }

    /// <summary>A box of the given size centred on the origin, to stand in for a controller; place it with the render item's matrix.</summary>
    public class VRBoxPrimitive : RenderPrimitive
    {
        readonly VertexBuffer VertexBuffer;
        readonly IndexBuffer IndexBuffer;

        public VRBoxPrimitive(GraphicsDevice graphicsDevice, Vector3 size, Vector3 centre)
        {
            var h = size / 2;
            var vertices = new VertexPositionTexture[8];
            for (var i = 0; i < 8; i++)
                vertices[i] = new VertexPositionTexture(centre + new Vector3((i & 1) == 0 ? -h.X : h.X, (i & 2) == 0 ? -h.Y : h.Y, (i & 4) == 0 ? -h.Z : h.Z), new Vector2((i & 1), (i & 2) >> 1));
            var indices = new short[] {
                0, 2, 1, 1, 2, 3,  // -Z
                4, 5, 6, 5, 7, 6,  // +Z
                0, 1, 4, 1, 5, 4,  // -Y
                2, 6, 3, 3, 6, 7,  // +Y
                0, 4, 2, 2, 4, 6,  // -X
                1, 3, 5, 3, 7, 5,  // +X
            };
            VertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionTexture), vertices.Length, BufferUsage.WriteOnly);
            VertexBuffer.SetData(vertices);
            IndexBuffer = new IndexBuffer(graphicsDevice, typeof(short), indices.Length, BufferUsage.WriteOnly);
            IndexBuffer.SetData(indices);
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.SetVertexBuffer(VertexBuffer);
            graphicsDevice.Indices = IndexBuffer;
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 12);
        }
    }

    /// <summary>
    /// A thin line from one point to another, as two quads crossed at right angles so it is visible
    /// from any direction. It is built for one frame; make a new one each frame (the previous frame may
    /// still be drawing the old one).
    /// </summary>
    public class VRLinePrimitive : RenderPrimitive
    {
        readonly VertexPositionTexture[] Vertices = new VertexPositionTexture[8];

        public VRLinePrimitive(Vector3 from, Vector3 to, float width)
        {
            var axis = to - from;
            var length = axis.Length();
            if (length < 1e-6f)
                axis = Vector3.Forward;
            else
                axis /= length;
            var side = Vector3.Cross(axis, Math.Abs(axis.Y) < 0.9f ? Vector3.Up : Vector3.Right);
            side.Normalize();
            var up = Vector3.Cross(axis, side);
            side *= width / 2;
            up *= width / 2;
            Quad(0, from, to, side);
            Quad(4, from, to, up);
        }

        void Quad(int index, Vector3 from, Vector3 to, Vector3 half)
        {
            Vertices[index + 0] = new VertexPositionTexture(from - half, new Vector2(0, 0));
            Vertices[index + 1] = new VertexPositionTexture(from + half, new Vector2(1, 0));
            Vertices[index + 2] = new VertexPositionTexture(to - half, new Vector2(0, 1));
            Vertices[index + 3] = new VertexPositionTexture(to + half, new Vector2(1, 1));
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, Vertices, 0, 2);
            graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, Vertices, 4, 2);
        }
    }

    /// <summary>Draws primitives with one texture through the loading screen's shader (a texture through a world-view-projection, nothing else), unlit and double-sided.</summary>
    public class VRTexturedMaterial : Material
    {
        readonly VRPanelShader Shader;
        public Texture2D Texture { get; set; }
        readonly bool Blended;
        /// <summary>Draw over everything (no depth test), for panels that must not sink into the cab.</summary>
        public bool AlwaysOnTop;

        public VRTexturedMaterial(GraphicsDevice graphicsDevice, Texture2D texture, bool blended)
            : base(null, null)
        {
            Shader = VRPanelShader.Get(graphicsDevice);
            Texture = texture;
            Blended = blended;
        }

        public override bool GetBlending()
        {
            return Blended;
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            Shader.CurrentTechnique = Shader.Techniques["Loading"];
            Shader.Texture = Texture;
            graphicsDevice.BlendState = Blended ? BlendState.NonPremultiplied : BlendState.Opaque;
            graphicsDevice.RasterizerState = RasterizerState.CullNone;
            graphicsDevice.DepthStencilState = AlwaysOnTop ? DepthStencilState.None : DepthStencilState.Default;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            foreach (var item in renderItems)
            {
                Shader.WorldViewProjection = item.XNAMatrix * XNAViewMatrix * XNAProjectionMatrix;
                Shader.CurrentTechnique.Passes[0].Apply();
                item.RenderPrimitive.Draw(graphicsDevice);
            }
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
        }
    }

    /// <summary>The loading screen's shader, shared by all the VR materials on a device.</summary>
    public class VRPanelShader : Shader
    {
        static VRPanelShader Instance;

        readonly EffectParameter worldViewProjection;
        readonly EffectParameter loadingTexture;

        public Matrix WorldViewProjection { set { worldViewProjection.SetValue(value); } }
        public Texture2D Texture { set { loadingTexture.SetValue(value); } }

        public static VRPanelShader Get(GraphicsDevice graphicsDevice)
        {
            if (Instance == null || Instance.GraphicsDevice != graphicsDevice || Instance.IsDisposed)
                Instance = new VRPanelShader(graphicsDevice);
            return Instance;
        }

        VRPanelShader(GraphicsDevice graphicsDevice)
            : base(graphicsDevice, "Loading")
        {
            worldViewProjection = Parameters["WorldViewProjection"];
            loadingTexture = Parameters["LoadingTexture"];
        }
    }
}
