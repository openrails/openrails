// COPYRIGHT 2009 - 2023 by the Open Rails project.
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

// Enable this define to debug the inputs to the particle emitters from other parts of the program.
//#define DEBUG_EMITTER_INPUT

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ORTS.Common;
using Orts.Simulation.RollingStocks;
using Orts.Viewer3D.RollingStock;

namespace Orts.Viewer3D
{
    public class ParticleEmitterViewer
    {
        public const float VolumeScale = 1f / 100;
        public const float Rate = 0.1f;
        public const float DecelerationTime = 0.2f;
        public const float InitialSpreadRate = 1;
        public const float SpreadRate = 0.75f;
        public const float DurationVariation = 0.5f; // ActionDuration varies +/-50%

        public const float MaxParticlesPerSecond = 50f;
        public const float MaxParticleDuration = 50f;

        readonly Viewer Viewer;
        readonly float EmissionHoleM2 = 1;
        readonly ParticleEmitterPrimitive Emitter;

        ParticleEmitterMaterial Material;

#if DEBUG_EMITTER_INPUT
        const int InputCycleLimit = 600;
        static int EmitterIDIndex = 0;
        int EmitterID;
        int InputCycle;
#endif

        public ParticleEmitterViewer(Viewer viewer, ParticleEmitterData data, MSTSWagonViewer car, WorldPosition worldPosition)
        {
            Viewer = viewer;
            EmissionHoleM2 = (MathHelper.Pi * ((data.NozzleWidth / 2f) * (data.NozzleWidth / 2f)));
            Emitter = new ParticleEmitterPrimitive(viewer, data, car, worldPosition);
#if DEBUG_EMITTER_INPUT
            EmitterID = ++EmitterIDIndex;
            InputCycle = Viewer.Random.Next(InputCycleLimit);
#endif
        }

        public void Initialize(string textureName)
        {
            Material = (ParticleEmitterMaterial)Viewer.MaterialManager.Load("ParticleEmitter", textureName);
        }

        public void SetOutput(float volumeM3pS)
        {
            // TODO: The values here are out by a factor of 100 here it seems. The XNAInitialVelocity should need no multiplication or division factors.
            Emitter.XNAInitialVelocity = Emitter.EmitterData.XNADirection * volumeM3pS / EmissionHoleM2 * VolumeScale;
            Emitter.ParticlesPerSecond = volumeM3pS / EmissionHoleM2 * Rate;

#if DEBUG_EMITTER_INPUT
            if (InputCycle == 0)
                Trace.TraceInformation("Emitter{0}({1:F6}m^2) V={2,7:F3}m^3/s IV={3,7:F3}m/s P={4,7:F3}p/s (V)", EmitterID, EmissionHoleM2, volumeM3pS, Emitter.XNAInitialVelocity.Length(), Emitter.ParticlesPerSecond);
#endif
        }

        // Called for diesel locomotive emissions
        public void SetOutput(float volumeM3pS, float durationS, Color color)
        {
            SetOutput(volumeM3pS);
            Emitter.ParticleDuration = durationS;
            Emitter.ParticleColor = color;

#if DEBUG_EMITTER_INPUT
            if (InputCycle == 0)
                Trace.TraceInformation("Emitter{0}({1:F6}m^3) D={2,3}s C={3} (V, D, C)", EmitterID, EmissionHoleM2, durationS, color);
#endif
        }

        // Called for steam locomotive emissions (non-main stack)
        public void SetOutput(float initialVelocityMpS, float volumeM3pS, float durationS)
        {
            Emitter.XNAInitialVelocity = Emitter.EmitterData.XNADirection * initialVelocityMpS / 10; // FIXME: Temporary hack until we can improve the particle emitter's ability to cope with high-velocity, quick-deceleration emissions.
            Emitter.ParticlesPerSecond = volumeM3pS / Rate * 0.2f;
            Emitter.ParticleDuration = durationS;

#if DEBUG_EMITTER_INPUT
            if (InputCycle == 0)
                Trace.TraceInformation("Emitter{0}({1:F6}m^2) IV={2,7:F3}m/s V={3,7:F3}m^3/s P={4,7:F3}p/s (IV, V)", EmitterID, EmissionHoleM2, initialVelocityMpS, volumeM3pS, Emitter.ParticlesPerSecond);
#endif
        }

        // Called for steam locomotive emissions (main stack)
        public void SetOutput(float initialVelocityMpS, float volumeM3pS, float durationS, Color color)
        {
            Emitter.XNAInitialVelocity = Emitter.EmitterData.XNADirection * initialVelocityMpS / 10; // FIXME: Temporary hack until we can improve the particle emitter's ability to cope with high-velocity, quick-deceleration emissions.
            Emitter.ParticlesPerSecond = volumeM3pS / Rate * 0.2f;
            Emitter.ParticleDuration = durationS;
            Emitter.ParticleColor = color;

#if DEBUG_EMITTER_INPUT
            if (InputCycle == 0)
                Trace.TraceInformation("Emitter{0}({1:F6}m^2) IV={2,7:F3}m/s V={3,7:F3}m^3/s P={4,7:F3}p/s D={5,3}s C={6} (IV, V, D, C)", EmitterID, EmissionHoleM2, initialVelocityMpS, volumeM3pS, Emitter.ParticlesPerSecond, durationS, color);
#endif
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            var gameTime = (float)Viewer.Simulator.GameTime;
            Emitter.Update(gameTime, elapsedTime);

            // Note: This is quite a hack. We ideally should be able to pass this through RenderItem somehow.
            var XNAWorldLocation = Matrix.Identity;
            XNAWorldLocation.M11 = gameTime;
            XNAWorldLocation.M21 = Viewer.Camera.TileX;
            XNAWorldLocation.M22 = Viewer.Camera.TileZ;

            if (Emitter.HasParticlesToRender())
                frame.AddPrimitive(Material, Emitter, RenderPrimitiveGroup.Particles, ref XNAWorldLocation);

#if DEBUG_EMITTER_INPUT
            InputCycle++;
            InputCycle %= InputCycleLimit;
#endif
        }

        [CallOnThread("Loader")]
        internal void Mark()
        {
            if (Material != null) // stops error messages if a special effect entry is not a defined OR parameter
                Material.Mark();
        }
    }

    public class ParticleEmitterPrimitive : RenderPrimitive
    {
        const int IndiciesPerParticle = 6;
        const int VerticesPerParticle = 4;
        const int PrimitivesPerParticle = 2;

        readonly int MaxParticles;
        readonly ParticleVertex[] Vertices;
        readonly VertexDeclaration VertexDeclaration;
        readonly DynamicVertexBuffer VertexBuffer;
        readonly IndexBuffer IndexBuffer;

        readonly float[] PerlinStart;

        struct ParticleVertex
        {
            public Vector4 StartPosition_StartTime;
            public Vector4 InitialVelocity_EndTime;
            public Vector4 TargetVelocity_TargetTime;
            public Vector4 TileXY_Vertex_ID;
            public Color Color_Random;

            public static readonly VertexElement[] VertexElements =
            {
                new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.Position, 0),
                new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.Position, 1),
                new VertexElement(16 + 16, VertexElementFormat.Vector4, VertexElementUsage.Position, 2),
                new VertexElement(16 + 16 + 16, VertexElementFormat.Vector4, VertexElementUsage.Position, 3),
                new VertexElement(16 + 16 + 16 + 16, VertexElementFormat.Color, VertexElementUsage.Position, 4)
            };

            public static int VertexStride = sizeof(float) * 12 + sizeof(float) * 4 + sizeof(float) * 4;
        }

        internal ParticleEmitterData EmitterData;
        internal Vector3 XNAInitialVelocity;
        internal Vector3 XNAFinalVelocity;
        internal float ParticlesPerSecond;
        internal float ParticleDuration;
        internal Color ParticleColor;

        internal WorldPosition WorldPosition;
        internal MSTSWagonViewer CarViewer;

        // Particle buffer goes like this:
        //   +--active>-----new>--+
        //   |                    |
        //   +--<retired---<free--+

        int FirstActiveParticle;
        int FirstNewParticle;
        int FirstFreeParticle;
        int FirstRetiredParticle;

        float AccumulatedParticles;
        int DrawCounter;

        Viewer Viewer;
        GraphicsDevice GraphicsDevice;

        public ParticleEmitterPrimitive(Viewer viewer, ParticleEmitterData data, MSTSWagonViewer car, WorldPosition worldPosition)
        {
            this.Viewer = viewer;
            this.GraphicsDevice = viewer.GraphicsDevice;

            MaxParticles = (int)(ParticleEmitterViewer.MaxParticlesPerSecond * ParticleEmitterViewer.MaxParticleDuration);
            Vertices = new ParticleVertex[MaxParticles * VerticesPerParticle];
            VertexDeclaration = new VertexDeclaration(ParticleVertex.VertexStride, ParticleVertex.VertexElements);
            VertexBuffer = new DynamicVertexBuffer(GraphicsDevice, VertexDeclaration, MaxParticles * VerticesPerParticle, BufferUsage.WriteOnly);
            IndexBuffer = InitIndexBuffer(GraphicsDevice, MaxParticles * IndiciesPerParticle);

            EmitterData = data;
            XNAInitialVelocity = data.XNADirection;
            XNAFinalVelocity = Vector3.Up;
            ParticlesPerSecond = 0;
            ParticleDuration = 3;
            ParticleColor = Color.White;

            CarViewer = car;

            WorldPosition = worldPosition;

            // Initialize the particle accumulator to a random value to de-sync particle emitters from eachother
            AccumulatedParticles = -(float)Viewer.Random.NextDouble() * 5.0f;

            PerlinStart = new float[] {
                (float)Viewer.Random.NextDouble() * 30000f,
                (float)Viewer.Random.NextDouble() * 30000f,
                (float)Viewer.Random.NextDouble() * 30000f,
                (float)Viewer.Random.NextDouble() * 30000f,
            };
        }

        void VertexBuffer_ContentLost()
        {
            VertexBuffer.SetData(0, Vertices, 0, Vertices.Length, ParticleVertex.VertexStride, SetDataOptions.NoOverwrite);
        }

        static IndexBuffer InitIndexBuffer(GraphicsDevice graphicsDevice, int numIndicies)
        {
            var indices = new ushort[numIndicies];
            var index = 0;
            for (var i = 0; i < numIndicies; i += IndiciesPerParticle)
            {
                indices[i] = (ushort)index;
                indices[i + 1] = (ushort)(index + 1);
                indices[i + 2] = (ushort)(index + 2);

                indices[i + 3] = (ushort)(index + 2);
                indices[i + 4] = (ushort)(index + 3);
                indices[i + 5] = (ushort)(index);

                index += VerticesPerParticle;
            }
            var indexBuffer = new IndexBuffer(graphicsDevice, typeof(ushort), numIndicies, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices);
            return indexBuffer;
        }

        public float EmitSize
        {
            get { return EmitterData.NozzleWidth; }
        }

        void RetireActiveParticles(float currentTime)
        {
            while (FirstActiveParticle != FirstNewParticle)
            {
                var vertex = FirstActiveParticle * VerticesPerParticle;
                var expiry = Vertices[vertex].InitialVelocity_EndTime.W;

                // Stop as soon as we find the first particle which hasn't expired.
                if (expiry > currentTime)
                    break;

                // Expire particle.
                Vertices[vertex].StartPosition_StartTime.W = (float)DrawCounter;
                FirstActiveParticle = (FirstActiveParticle + 1) % MaxParticles;
            }
        }

        void ForceRetireParticles(int count)
        {
            for (int i = 0; i < count; i++)
            {
                int NextActiveParticle = (FirstActiveParticle + 1) % MaxParticles;

                // Don't try to clear so many particles that we start clearing the newest ones
                if (NextActiveParticle == FirstNewParticle)
                    break;

                int vertex = FirstActiveParticle * VerticesPerParticle;

                Vertices[vertex].StartPosition_StartTime.W = (float)DrawCounter;
                FirstActiveParticle = NextActiveParticle;
            }
        }

        void FreeRetiredParticles()
        {
            while (FirstRetiredParticle != FirstActiveParticle)
            {
                var vertex = FirstRetiredParticle * VerticesPerParticle;
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

        public void Update(float currentTime, ElapsedTime elapsedTime)
        {
            if (ParticlesPerSecond > 0)
            {
                // Limit particle spawn rate to try and prevent overfilling the particle buffer
                // This should only be needed when the particle spawn rate is visually excessive
                float effectiveParticlesPerSecond = Math.Min(ParticlesPerSecond, (MaxParticles * 0.9f) / (ParticleDuration * (1.0f + ParticleEmitterViewer.DurationVariation)));

                // Momentary Infinity/NaN values can occur, treat these as 0
                if (!float.IsFinite(effectiveParticlesPerSecond))
                    effectiveParticlesPerSecond = 0;

                AccumulatedParticles += elapsedTime.ClockSeconds * effectiveParticlesPerSecond;

                int maxNewParticles = GetCountFreeParticles() - (int)(MaxParticles * 0.025f);

                // We are low on free particles, always try to leave a free buffer of about 2.5% of the total
                if (AccumulatedParticles > maxNewParticles)
                    ForceRetireParticles((int)AccumulatedParticles);
                else // Otherwise, only clear out expired particles
                    RetireActiveParticles(currentTime);
                FreeRetiredParticles();

                AccumulatedParticles = Math.Min(AccumulatedParticles, GetCountFreeParticles());

                int numToBeEmitted = (int)Math.Floor(AccumulatedParticles);

                if (numToBeEmitted > 0)
                {
                    Matrix transform = WorldPosition.XNAMatrix;
                    transform.Translation = Vector3.Zero; // Only want rotation data for this step
                    // rotation = CarViewer.TrainCarShape.ResultMatrices[EmitterData.ShapeIndex] * rotation; // Future: ShapeHierarchy goes here

                    Matrix rotation = transform;
                    rotation.Translation = Vector3.Zero; // Last step needed translational effects, next step does not

                    // Final velocity should rotate with the attached train car, but only about the Y axis
                    rotation.Decompose(out _, out Quaternion rotY, out _);
                    rotY.X = 0;
                    rotY.Z = 0;
                    rotY.Normalize();

                    Vector3 carVelocity = new Vector3(CarViewer.Velocity[0], CarViewer.Velocity[1], -CarViewer.Velocity[2]);

                    float emitTime = currentTime - elapsedTime.ClockSeconds;

                    float deltaTime = elapsedTime.ClockSeconds / AccumulatedParticles;

                    for (int i = 0; i < numToBeEmitted; i++)
                    {
                        emitTime += deltaTime;

                        int nextFreeParticle = (FirstFreeParticle + 1) % MaxParticles;
                        int vertex = FirstFreeParticle * VerticesPerParticle;
                        int texture = Viewer.Random.Next(16); // Randomizes emissions.
                        Color color_Random = new Color(ParticleColor.R / 255f, ParticleColor.G / 255f, ParticleColor.B / 255f, (float)Viewer.Random.NextDouble());

                        Vector3 position = EmitterData.XNALocation;

                        Vector3 initialVelocity = XNAInitialVelocity;
                        Vector3 finalVelocity = XNAFinalVelocity;

                        initialVelocity.X += (float)(Viewer.Random.NextDouble() - 0.5f) * ParticleEmitterViewer.InitialSpreadRate;
                        initialVelocity.Z += (float)(Viewer.Random.NextDouble() - 0.5f) * ParticleEmitterViewer.InitialSpreadRate;

                        finalVelocity.X += Noise.Generate(emitTime + PerlinStart[0]) * ParticleEmitterViewer.SpreadRate;
                        finalVelocity.Y += Noise.Generate(emitTime + PerlinStart[1]) * ParticleEmitterViewer.SpreadRate;
                        finalVelocity.Z += Noise.Generate(emitTime + PerlinStart[2]) * ParticleEmitterViewer.SpreadRate;

                        // Duration is variable too.
                        float duration = ParticleDuration * (1 + Noise.Generate(emitTime + PerlinStart[3]) * ParticleEmitterViewer.DurationVariation);

                        position = Vector3.Transform(position, transform) + WorldPosition.XNAMatrix.Translation;

                        // Interpolate the position of the particle in-between frames
                        position -= carVelocity * (currentTime - emitTime);

                        initialVelocity = Vector3.Transform(initialVelocity, rotation);
                        finalVelocity = Vector3.Transform(finalVelocity, rotY);

                        // Add on velocity of attached train car
                        initialVelocity += carVelocity;

                        // Add wind speed (not randomized here)
                        finalVelocity.X += Viewer.Simulator.Weather.WindInstantaneousSpeedMpS * Viewer.Simulator.Weather.WindInstantaneousDirection.X;
                        finalVelocity.Z += Viewer.Simulator.Weather.WindInstantaneousSpeedMpS * Viewer.Simulator.Weather.WindInstantaneousDirection.Y;

                        for (int j = 0; j < VerticesPerParticle; j++)
                        {
                            Vertices[vertex + j].StartPosition_StartTime = new Vector4(position, emitTime);
                            Vertices[vertex + j].InitialVelocity_EndTime = new Vector4(initialVelocity, emitTime + duration);
                            Vertices[vertex + j].TargetVelocity_TargetTime = new Vector4(finalVelocity, ParticleEmitterViewer.DecelerationTime);
                            Vertices[vertex + j].TileXY_Vertex_ID = new Vector4(WorldPosition.TileX, WorldPosition.TileZ, j, texture);
                            Vertices[vertex + j].Color_Random = color_Random;
                        }

                        FirstFreeParticle = nextFreeParticle;
                    }

                    // Remove emitted particles from the accumulator, with some randomness to keep emitters out of sync
                    AccumulatedParticles -= numToBeEmitted + ((float)Viewer.Random.NextDouble() * 0.05f);
                }
            }
            else // Skip most processing if emitter is currently inactive
            {
                RetireActiveParticles(currentTime);
                FreeRetiredParticles();
            }
        }

        void AddNewParticlesToVertexBuffer()
        {
            if (FirstNewParticle < FirstFreeParticle)
            {
                var numParticlesToAdd = FirstFreeParticle - FirstNewParticle;
                VertexBuffer.SetData(FirstNewParticle * ParticleVertex.VertexStride * VerticesPerParticle, Vertices, FirstNewParticle * VerticesPerParticle, numParticlesToAdd * VerticesPerParticle, ParticleVertex.VertexStride, SetDataOptions.NoOverwrite);
            }
            else
            {
                var numParticlesToAddAtEnd = MaxParticles - FirstNewParticle;
                VertexBuffer.SetData(FirstNewParticle * ParticleVertex.VertexStride * VerticesPerParticle, Vertices, FirstNewParticle * VerticesPerParticle, numParticlesToAddAtEnd * VerticesPerParticle, ParticleVertex.VertexStride, SetDataOptions.NoOverwrite);
                if (FirstFreeParticle > 0)
                    VertexBuffer.SetData(0, Vertices, 0, FirstFreeParticle * VerticesPerParticle, ParticleVertex.VertexStride, SetDataOptions.NoOverwrite);
            }

            FirstNewParticle = FirstFreeParticle;
        }

        public bool HasParticlesToRender()
        {
            return FirstActiveParticle != FirstFreeParticle;
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            if (VertexBuffer.IsContentLost)
                VertexBuffer_ContentLost();

            if (FirstNewParticle != FirstFreeParticle)
                AddNewParticlesToVertexBuffer();

            if (HasParticlesToRender())
            {
                graphicsDevice.Indices = IndexBuffer;
                graphicsDevice.SetVertexBuffer(VertexBuffer);

                if (FirstActiveParticle < FirstFreeParticle)
                {
                    var numParticles = FirstFreeParticle - FirstActiveParticle;
                    // thread safe clause
                    if (numParticles > 0)
                        graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, baseVertex: 0, startIndex: FirstActiveParticle * IndiciesPerParticle, primitiveCount: numParticles * PrimitivesPerParticle);
                }
                else
                {
                    var numParticlesAtEnd = MaxParticles - FirstActiveParticle;
                    if (numParticlesAtEnd > 0)
                        graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, baseVertex: 0, startIndex: FirstActiveParticle * IndiciesPerParticle, primitiveCount: numParticlesAtEnd * PrimitivesPerParticle);
                    if (FirstFreeParticle > 0)
                        graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, baseVertex: 0, startIndex: 0, primitiveCount: FirstFreeParticle * PrimitivesPerParticle);
                }
            }

            DrawCounter++;
        }
    }

    public class ParticleEmitterMaterial : Material
    {
        public Texture2D Texture;

        IEnumerator<EffectPass> ShaderPasses;

        public ParticleEmitterMaterial(Viewer viewer, string textureName)
            : base(viewer, null)
        {
            Texture = viewer.TextureManager.Get(textureName, true);
            ShaderPasses = Viewer.MaterialManager.ParticleEmitterShader.Techniques["ParticleEmitterTechnique"].Passes.GetEnumerator();
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            var shader = Viewer.MaterialManager.ParticleEmitterShader;
            if (Viewer.Settings.UseMSTSEnv == false)
                shader.LightVector = Viewer.World.Sky.SolarDirection;
            else
                shader.LightVector = Viewer.World.MSTSSky.mstsskysolarDirection; 

            graphicsDevice.BlendState = BlendState.NonPremultiplied;
            graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.ParticleEmitterShader;

            ShaderPasses.Reset();
            while (ShaderPasses.MoveNext())
            {
                foreach (var item in renderItems)
                {
                    // Note: This is quite a hack. We ideally should be able to pass this through RenderItem somehow.
                    shader.CameraTileXY = new Vector2(item.XNAMatrix.M21, item.XNAMatrix.M22);
                    shader.CurrentTime = item.XNAMatrix.M11;

                    var emitter = (ParticleEmitterPrimitive)item.RenderPrimitive;
                    shader.EmitSize = emitter.EmitSize;
                    shader.Texture = Texture;
                    shader.SetMatrix(Matrix.Identity, ref XNAViewMatrix, ref XNAProjectionMatrix);
                    ShaderPasses.Current.Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
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
