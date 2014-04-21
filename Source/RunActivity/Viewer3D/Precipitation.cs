// COPYRIGHT 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
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
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using ORTS.Common;

namespace ORTS.Viewer3D
{
    public class PrecipitationViewer
    {
        public const float MinIntensity = 0;
        public const float MaxIntensity = 15000;

        readonly Viewer Viewer;
        readonly WeatherControl Weather;

        readonly Material Material;
        readonly PrecipitationPrimitive Pricipitation;

        Vector3 Wind;

        public PrecipitationViewer(Viewer viewer, WeatherControl weather)
        {
            Viewer = viewer;
            Weather = weather;

            Material = viewer.MaterialManager.Load("Precipitation");
            Pricipitation = new PrecipitationPrimitive(Viewer.GraphicsDevice);

            Wind = new Vector3(0, 0, 0);
            Reset();
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            var gameTime = (float)Viewer.Simulator.GameTime;
            Pricipitation.Update(gameTime, elapsedTime, Weather.pricipitationIntensity, Viewer.Tiles, Viewer.Camera.CameraWorldLocation);

            // Note: This is quite a hack. We ideally should be able to pass this through RenderItem somehow.
            var XNAWorldLocation = Matrix.Identity;
            XNAWorldLocation.M11 = gameTime;
            XNAWorldLocation.M21 = Viewer.Camera.TileX;
            XNAWorldLocation.M22 = Viewer.Camera.TileZ;

            frame.AddPrimitive(Material, Pricipitation, RenderPrimitiveGroup.Precipitation, ref XNAWorldLocation);
        }

        public void Reset()
        {
            Wind.X = Viewer.Simulator.Weather == MSTS.Formats.WeatherType.Snow ? 2 : 20;

            var gameTime = (float)Viewer.Simulator.GameTime;
            Pricipitation.Initialize(Viewer.Simulator.Weather, Wind);
            // Camera is null during first initialisation.
            if (Viewer.Camera != null) Pricipitation.Update(gameTime, null, Weather.pricipitationIntensity, Viewer.Tiles, Viewer.Camera.CameraWorldLocation);
        }

        [CallOnThread("Loader")]
        internal void Mark()
        {
            Material.Mark();
        }
    }

    public class PrecipitationPrimitive : RenderPrimitive
    {
        // http://www-das.uwyo.edu/~geerts/cwx/notes/chap09/hydrometeor.html
        // "Rain  1.8 - 2.2mm  6.1 - 6.9m/s"
        const float RainVelocityMpS = 6.5f;
        // "Snow flakes of any size falls at about 1 m/s"
        const float SnowVelocityMpS = 1.0f;
        // This is a fiddle factor because the above values feel too slow. Alternative suggestions welcome.
        const float ParticleVelocityFactor = 10.0f;

        // The width/depth of the box containing pricipitation. It is centered around the camera usually.
        const float ParticleBoxSizeM = 1024;
        // The height of the box containing pricipitation.
        const float ParticleBoxHeightM = 100;

        const int IndiciesPerParticle = 6;
        const int VerticiesPerParticle = 4;
        const int PrimitivesPerParticle = 2;

        readonly int MaxParticles;
        readonly ParticleVertex[] Vertices;
        readonly VertexDeclaration VertexDeclaration;
        readonly int VertexStride;
        readonly DynamicVertexBuffer VertexBuffer;
        readonly IndexBuffer IndexBuffer;

        struct ParticleVertex
        {
            public Vector4 StartPosition_StartTime;
            public Vector4 EndPosition_EndTime;
            public Short4 TileXZ_Vertex;

            public static readonly VertexElement[] VertexElements =
            {
                new VertexElement(0, 0, VertexElementFormat.Vector4, VertexElementMethod.Default, VertexElementUsage.Position, 0),
                new VertexElement(0, 16, VertexElementFormat.Vector4, VertexElementMethod.Default, VertexElementUsage.Position, 1),
                new VertexElement(0, 16 + 16, VertexElementFormat.Short4, VertexElementMethod.Default, VertexElementUsage.Position, 2),
            };
        }

        float ParticleDuration;
        Vector3 ParticleDirection;

        // Particle buffer goes like this:
        //   +--active>-----new>--+
        //   |                    |
        //   +--<retired---<free--+

        int FirstActiveParticle;
        int FirstNewParticle;
        int FirstFreeParticle;
        int FirstRetiredParticle;

        float ParticlesToEmit;
        float TimeParticlesLastEmitted;
        int DrawCounter;

        public PrecipitationPrimitive(GraphicsDevice graphicsDevice)
        {
            // Snow is the slower particle, hence longer duration, hence more particles in total.
            MaxParticles = (int)(PrecipitationViewer.MaxIntensity * (ParticleBoxHeightM / SnowVelocityMpS / ParticleVelocityFactor));
            Vertices = new ParticleVertex[MaxParticles * VerticiesPerParticle];
            VertexDeclaration = new VertexDeclaration(graphicsDevice, ParticleVertex.VertexElements);
            VertexStride = Marshal.SizeOf(typeof(ParticleVertex));
            VertexBuffer = new DynamicVertexBuffer(graphicsDevice, typeof(ParticleVertex), MaxParticles * VerticiesPerParticle, BufferUsage.WriteOnly);
            VertexBuffer.ContentLost += new System.EventHandler(VertexBuffer_ContentLost);
            IndexBuffer = InitIndexBuffer(graphicsDevice, MaxParticles * IndiciesPerParticle);
        }

        void VertexBuffer_ContentLost(object sender, EventArgs e)
        {
            VertexBuffer.SetData(0, Vertices, 0, Vertices.Length, VertexStride, SetDataOptions.NoOverwrite);
        }

        static IndexBuffer InitIndexBuffer(GraphicsDevice graphicsDevice, int numIndicies)
        {
            var indices = new uint[numIndicies];
            var index = 0;
            for (var i = 0; i < numIndicies; i += IndiciesPerParticle)
            {
                indices[i] = (uint)index;
                indices[i + 1] = (uint)(index + 1);
                indices[i + 2] = (uint)(index + 2);

                indices[i + 3] = (uint)(index + 2);
                indices[i + 4] = (uint)(index + 3);
                indices[i + 5] = (uint)(index);

                index += VerticiesPerParticle;
            }
            var indexBuffer = new IndexBuffer(graphicsDevice, sizeof(uint) * numIndicies, BufferUsage.WriteOnly, IndexElementSize.ThirtyTwoBits);
            indexBuffer.SetData(indices);
            return indexBuffer;
        }

        void RetireActiveParticles(float currentTime)
        {
            while (FirstActiveParticle != FirstNewParticle)
            {
                var vertex = FirstActiveParticle * VerticiesPerParticle;
                var expiry = Vertices[vertex].EndPosition_EndTime.W;

                // Stop as soon as we find the first particle which hasn't expired.
                if (expiry > currentTime)
                    break;

                // Expire particle.
                Vertices[vertex].StartPosition_StartTime.W = (float)DrawCounter;
                FirstActiveParticle = (FirstActiveParticle + 1) % MaxParticles;
            }
        }

        void FreeRetiredParticles()
        {
            while (FirstRetiredParticle != FirstActiveParticle)
            {
                var vertex = FirstRetiredParticle * VerticiesPerParticle;
                var age = DrawCounter - (int)Vertices[vertex].StartPosition_StartTime.W;

                // Stop as soon as we find the first expired particle which hasn't been expired for at least 2 'ticks'.
                if (age < 2)
                    break;

                FirstRetiredParticle = (FirstRetiredParticle + 1) % MaxParticles;
            }
        }

        int GetCountFreeParticles()
        {
            var nextFree = (FirstFreeParticle + 1) % MaxParticles;

            if (nextFree <= FirstRetiredParticle)
                return FirstRetiredParticle - nextFree;

            return (MaxParticles - nextFree) + FirstRetiredParticle;
        }

        public void Initialize(MSTS.Formats.WeatherType weather, Vector3 wind)
        {
            ParticleDuration = ParticleBoxHeightM / (weather == MSTS.Formats.WeatherType.Snow ? SnowVelocityMpS : RainVelocityMpS) / ParticleVelocityFactor;
            ParticleDirection = wind;
            FirstActiveParticle = FirstNewParticle = FirstFreeParticle = FirstRetiredParticle = 0;
            ParticlesToEmit = TimeParticlesLastEmitted = 0;
            DrawCounter = 0;
        }

        public void Update(float currentTime, ElapsedTime elapsedTime, float particlesPerSecond, TileManager tiles, WorldLocation worldLocation)
        {
            if (TimeParticlesLastEmitted == 0)
            {
                TimeParticlesLastEmitted = currentTime - ParticleDuration;
                ParticlesToEmit += ParticleDuration * particlesPerSecond;
            }
            else
            {
                RetireActiveParticles(currentTime);
                FreeRetiredParticles();

                ParticlesToEmit += elapsedTime.ClockSeconds * particlesPerSecond;
            }

            var numParticlesAdded = 0;
            var numToBeEmitted = (int)ParticlesToEmit;
            var numCanBeEmitted = GetCountFreeParticles();
            var numToEmit = Math.Min(numToBeEmitted, numCanBeEmitted);

            for (var i = 0; i < numToEmit; i++)
            {
                var temp = new WorldLocation(worldLocation.TileX, worldLocation.TileZ, worldLocation.Location.X + (float)((Program.Random.NextDouble() - 0.5) * ParticleBoxSizeM), 0, worldLocation.Location.Z + (float)((Program.Random.NextDouble() - 0.5) * ParticleBoxSizeM));
                temp.Location.Y = tiles.GetElevation(temp);
                var position = new WorldPosition(temp);

                var time = MathHelper.Lerp(TimeParticlesLastEmitted, currentTime, (float)Program.Random.NextDouble());
                var particle = (FirstFreeParticle + 1) % MaxParticles;
                var vertex = particle * VerticiesPerParticle;

                for (var j = 0; j < VerticiesPerParticle; j++)
                {
                    Vertices[vertex + j].StartPosition_StartTime = new Vector4(position.XNAMatrix.Translation - ParticleDirection * ParticleDuration, time);
                    Vertices[vertex + j].StartPosition_StartTime.Y += ParticleBoxHeightM;
                    Vertices[vertex + j].EndPosition_EndTime = new Vector4(position.XNAMatrix.Translation, time + ParticleDuration);
                    Vertices[vertex + j].TileXZ_Vertex = new Short4(position.TileX, position.TileZ, j, 0);
                }

                FirstFreeParticle = particle;
                ParticlesToEmit--;
                numParticlesAdded++;
            }

            if (numParticlesAdded > 0)
                TimeParticlesLastEmitted = currentTime;

            ParticlesToEmit = ParticlesToEmit - (int)ParticlesToEmit;
        }

        void AddNewParticlesToVertexBuffer()
        {
            if (FirstNewParticle < FirstFreeParticle)
            {
                var numParticlesToAdd = FirstFreeParticle - FirstNewParticle;
                VertexBuffer.SetData(FirstNewParticle * VertexStride * VerticiesPerParticle, Vertices, FirstNewParticle * VerticiesPerParticle, numParticlesToAdd * VerticiesPerParticle, VertexStride, SetDataOptions.NoOverwrite);
            }
            else
            {
                var numParticlesToAddAtEnd = MaxParticles - FirstNewParticle;
                VertexBuffer.SetData(FirstNewParticle * VertexStride * VerticiesPerParticle, Vertices, FirstNewParticle * VerticiesPerParticle, numParticlesToAddAtEnd * VerticiesPerParticle, VertexStride, SetDataOptions.NoOverwrite);
                if (FirstFreeParticle > 0)
                    VertexBuffer.SetData(0, Vertices, 0, FirstFreeParticle * VerticiesPerParticle, VertexStride, SetDataOptions.NoOverwrite);
            }

            FirstNewParticle = FirstFreeParticle;
        }

        public bool HasParticlesToRender()
        {
            return FirstActiveParticle != FirstFreeParticle;
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            if (FirstNewParticle != FirstFreeParticle)
                AddNewParticlesToVertexBuffer();

            if (HasParticlesToRender())
            {
                graphicsDevice.Indices = IndexBuffer;
                graphicsDevice.VertexDeclaration = VertexDeclaration;
                graphicsDevice.Vertices[0].SetSource(VertexBuffer, 0, VertexStride);

                if (FirstActiveParticle < FirstFreeParticle)
                {
                    var numParticles = FirstFreeParticle - FirstActiveParticle;
                    graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, FirstActiveParticle * VerticiesPerParticle, numParticles * VerticiesPerParticle, FirstActiveParticle * IndiciesPerParticle, numParticles * PrimitivesPerParticle);
                }
                else
                {
                    var numParticlesAtEnd = MaxParticles - FirstActiveParticle;
                    if (numParticlesAtEnd > 0)
                        graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, FirstActiveParticle * VerticiesPerParticle, numParticlesAtEnd * VerticiesPerParticle, FirstActiveParticle * IndiciesPerParticle, numParticlesAtEnd * PrimitivesPerParticle);
                    if (FirstFreeParticle > 0)
                        graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, FirstFreeParticle * VerticiesPerParticle, 0, FirstFreeParticle * PrimitivesPerParticle);
                }
            }

            DrawCounter++;
        }
    }

    public class PrecipitationMaterial : Material
    {
        Texture2D RainTexture;
        Texture2D SnowTexture;
        IEnumerator<EffectPass> ShaderPasses;

        public PrecipitationMaterial(Viewer viewer)
            : base(viewer, null)
        {
            // TODO: This should happen on the loader thread.
            RainTexture = Texture2D.FromFile(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "Raindrop.png"));
            SnowTexture = Texture2D.FromFile(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "Snowflake.png"));
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            var shader = Viewer.MaterialManager.PrecipitationShader;
            shader.CurrentTechnique = shader.Techniques["Pricipitation"];
            if (ShaderPasses == null) ShaderPasses = shader.Techniques["Pricipitation"].Passes.GetEnumerator();

            shader.LightVector.SetValue(Viewer.Settings.UseMSTSEnv ? Viewer.World.MSTSSky.mstsskysolarDirection : Viewer.World.Sky.solarDirection);
            shader.particleSize.SetValue(1);
            shader.precipitation_Tex.SetValue(Viewer.Simulator.Weather == MSTS.Formats.WeatherType.Snow ? SnowTexture : RainTexture);

            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = true;
            rs.DepthBufferWriteEnable = false;
            rs.DestinationBlend = Blend.InverseSourceAlpha;
            rs.SourceBlend = Blend.SourceAlpha;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.PrecipitationShader;

            shader.Begin();
            ShaderPasses.Reset();
            while (ShaderPasses.MoveNext())
            {
                ShaderPasses.Current.Begin();
                foreach (var item in renderItems)
                {
                    // Note: This is quite a hack. We ideally should be able to pass this through RenderItem somehow.
                    shader.cameraTileXZ.SetValue(new Vector2(item.XNAMatrix.M21, item.XNAMatrix.M22));
                    shader.currentTime.SetValue(item.XNAMatrix.M11);
                    item.XNAMatrix = Matrix.Identity;

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
            Viewer.TextureManager.Mark(RainTexture);
            Viewer.TextureManager.Mark(SnowTexture);
            base.Mark();
        }
    }

    [CallOnThread("Render")]
    public class PrecipitationShader : Shader
    {
        internal readonly EffectParameter worldViewProjection;
        internal readonly EffectParameter invView;
        internal readonly EffectParameter LightVector;
        internal readonly EffectParameter particleSize;
        internal readonly EffectParameter cameraTileXZ;
        internal readonly EffectParameter currentTime;
        internal readonly EffectParameter precipitation_Tex;

        public PrecipitationShader(GraphicsDevice graphicsDevice)
            : base(graphicsDevice, "PrecipitationShader")
        {
            worldViewProjection = Parameters["worldViewProjection"];
            invView = Parameters["invView"];
            LightVector = Parameters["LightVector"];
            particleSize = Parameters["particleSize"];
            cameraTileXZ = Parameters["cameraTileXZ"];
            currentTime = Parameters["currentTime"];
            precipitation_Tex = Parameters["precipitation_Tex"];
        }

        public void SetMatrix(Matrix world, ref Matrix view, ref Matrix projection)
        {
            worldViewProjection.SetValue(world * view * projection);
            invView.SetValue(Matrix.Invert(view));
        }
    }
}
