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
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Simulation.RollingStocks;
using Orts.Viewer3D.RollingStock;
using ORTS.Common;

namespace Orts.Viewer3D
{
    public class ParticleEmitterViewer
    {
        public ParticleEmitterData EmitterData;

        public readonly Viewer Viewer;
        public readonly float EmissionHoleM2 = 0.01f;
        public readonly float ParticleVolumeM3 = 0.001f;
        public readonly ParticleEmitterPrimitive Emitter;

        public string TexturePath;
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
            EmitterData = data;

            if (EmitterData.NozzleAreaM2 <= 0)
            {
                // If area is undefined, assume emitter is circular, A = pi * (d/2)^2
                // If position randomization is used, this calculation will not be accurate, in which case the user should adjust velocities accordingly
                EmissionHoleM2 = MathHelper.Pi * ((EmitterData.NozzleDiameterM * EmitterData.NozzleDiameterM) / 4.0f);
            }
            else
                EmissionHoleM2 = EmitterData.NozzleAreaM2;

            // Assume particles are spheres, V = (4/3) * pi * (d/2)^3
            // Particles expand over time, this is just the initial volume, useful for calculating initial velocity
            ParticleVolumeM3 = 4.0f / 3.0f * MathHelper.Pi * ((EmitterData.NozzleDiameterM * EmitterData.NozzleDiameterM * EmitterData.NozzleDiameterM) / 8.0f);
            Emitter = new ParticleEmitterPrimitive(this, data, car, worldPosition);

            if (!String.IsNullOrEmpty(EmitterData.Graphic))
                TexturePath = EmitterData.Graphic;
#if DEBUG_EMITTER_INPUT
            EmitterID = ++EmitterIDIndex;
            InputCycle = Viewer.Random.Next(InputCycleLimit);
#endif
            }

        public void Initialize(string defaultTextureName)
        {
            bool customTexture = false;

            if (!String.IsNullOrEmpty(TexturePath))
                customTexture = true;
            else
                TexturePath = defaultTextureName;

            string noExtension = Path.ChangeExtension(TexturePath, null);

            string wagPath = Path.Combine(Path.GetDirectoryName(Emitter.CarViewer.Car.WagFilePath), noExtension);
            string globalPath = Path.Combine(Viewer.Simulator.BasePath + @"\GLOBAL\TEXTURES\", noExtension);
            string contentPath = Path.Combine(Viewer.ContentPath, noExtension);

            // Texture location preference is eng/wag folder -> MSTS GLOBAL\TEXTURES folder -> OR CONTENT folder
            // File type agnostic: We should detect a match if a .ace OR .dds is present, regardless of the specific file type requested
            if (File.Exists(wagPath + ".dds"))
                TexturePath = wagPath + ".dds";
            else if (File.Exists(wagPath + ".ace"))
                TexturePath = wagPath + ".ace";
            else if (File.Exists(globalPath + ".dds"))
                TexturePath = globalPath + ".dds";
            else if (File.Exists(globalPath + ".ace"))
                TexturePath = globalPath + ".ace";
            else if (File.Exists(contentPath + ".dds"))
                TexturePath = contentPath + ".dds";
            else if (File.Exists(contentPath + ".ace"))
                TexturePath = contentPath + ".ace";
            else // Fall back to default texture in CONTENT folder
            {
                TexturePath = Path.Combine(Viewer.ContentPath, defaultTextureName);

                if (customTexture)
                    Trace.TraceWarning("Could not find particle graphic {0} at {1}", TexturePath, Path.Combine(Path.GetDirectoryName(Emitter.CarViewer.Car.WagFilePath), TexturePath));
            }

            Material = (ParticleEmitterMaterial)Viewer.MaterialManager.Load("ParticleEmitter", TexturePath);
        }

        /// <summary>
        /// Sets the particle and velocity output of this particle emitter based on a given rate of particles per second.
        /// The velocity of particles will be calculated from the particle rate and particle size.
        /// </summary>
        /// <param name="particlespS">The number of particles to emit every second.</param>
        public void SetOutputRate(float particlespS)
        {
            Emitter.ParticlesPerSecond = EmitterData.RateFactor * particlespS;
            Emitter.XNAInitialVelocity = Emitter.EmitterData.InitialVelocityFactor * (ParticleVolumeM3 * particlespS / EmissionHoleM2);

#if DEBUG_EMITTER_INPUT
            if (InputCycle == 0)
                Trace.TraceInformation("Emitter{0}({1:F6}m^2) V={2,7:F3}m^3/s IV={3,7:F3}m/s P={4,7:F3}p/s (V)", EmitterID, EmissionHoleM2, volumeM3pS, Emitter.XNAInitialVelocity.Length(), Emitter.ParticlesPerSecond);
#endif
        }

        /// <summary>
        /// Sets the particle and velocity output of this particle emitter based on a given output flow rate.
        /// The rate of particle emission will be calculated from the volumetric rate and particle size.
        /// </summary>
        /// <param name="volumeM3pS">The cubic meter volume of particles to emit every second.</param>
        public void SetOutputVolumetric(float volumeM3pS)
        {
            Emitter.XNAInitialVelocity = Emitter.EmitterData.InitialVelocityFactor * volumeM3pS / EmissionHoleM2;
            Emitter.ParticlesPerSecond = EmitterData.RateFactor * volumeM3pS / ParticleVolumeM3;

#if DEBUG_EMITTER_INPUT
            if (InputCycle == 0)
                Trace.TraceInformation("Emitter{0}({1:F6}m^2) V={2,7:F3}m^3/s IV={3,7:F3}m/s P={4,7:F3}p/s (V)", EmitterID, EmissionHoleM2, volumeM3pS, Emitter.XNAInitialVelocity.Length(), Emitter.ParticlesPerSecond);
#endif
        }

        /// <summary>
        /// Sets the particle and velocity output of this particle emitter based on a given speed.
        /// The rate of particle emission will be calculated from the speed and particle size.
        /// </summary>
        /// <param name="initialVelocityMpS">The meter per second speed particles should emit with.</param>
        public void SetOutputVelocity(float initialVelocityMpS)
        {
            Emitter.XNAInitialVelocity = Emitter.EmitterData.InitialVelocityFactor * initialVelocityMpS;
            Emitter.ParticlesPerSecond = EmitterData.RateFactor * initialVelocityMpS * EmissionHoleM2 / ParticleVolumeM3;
        }

        /// <summary>
        /// Sets the particle and velocity output of this particle emitter based on a given rate of particles per second,
        /// including the ability to change particle duration and color.
        /// The velocity of particles will be calculated from the particle rate and particle size.
        /// </summary>
        /// <param name="particlespS">The number of particles to emit every second.</param>
        /// <param name="durationS">The lifespan of each particle in seconds.</param>
        /// <param name="color">A color struct giving the color of particles to emit.</param>
        public void SetOutputRate(float particlespS, float durationS, Color color)
        {
            SetOutputRate(particlespS);
            Emitter.ParticleDuration = durationS * EmitterData.LifetimeFactor;
            Emitter.ParticleColor = color;
            Emitter.ParticleColor.A = (byte)(Emitter.ParticleColor.A * EmitterData.Opacity);
        }

        /// <summary>
        /// Sets the particle and velocity output of this particle emitter based on a given output flow rate,
        /// including the ability to change particle duration and color.
        /// The rate of particle emission will be calculated from the volumetric rate and particle size.
        /// </summary>
        /// <param name="volumeM3pS">The cubic meter volume of particles to emit every second.</param>
        /// <param name="durationS">The lifespan of each particle in seconds.</param>
        /// <param name="color">A color struct giving the color of particles to emit.</param>
        public void SetOutputVolumetric(float volumeM3pS, float durationS, Color color)
        {
            SetOutputVolumetric(volumeM3pS);
            Emitter.ParticleDuration = durationS * EmitterData.LifetimeFactor;
            Emitter.ParticleColor = color;
            Emitter.ParticleColor.A = (byte)(Emitter.ParticleColor.A * EmitterData.Opacity);

#if DEBUG_EMITTER_INPUT
            if (InputCycle == 0)
                Trace.TraceInformation("Emitter{0}({1:F6}m^3) D={2,3}s C={3} (V, D, C)", EmitterID, EmissionHoleM2, durationS, color);
#endif
        }

        /// <summary>
        /// Sets the particle and velocity output of this particle emitter based on a given output flow rate,
        /// including the ability to change particle duration.
        /// The rate of particle emission will be calculated from the volumetric rate and particle size.
        /// </summary>
        /// <param name="volumeM3pS">The cubic meter volume of particles to emit every second.</param>
        /// <param name="durationS">The lifespan of each particle in seconds.</param>
        public void SetOutputVolumetric(float volumeM3pS, float durationS)
        {
            SetOutputVolumetric(volumeM3pS);
            Emitter.ParticleDuration = durationS * EmitterData.LifetimeFactor;

#if DEBUG_EMITTER_INPUT
            if (InputCycle == 0)
                Trace.TraceInformation("Emitter{0}({1:F6}m^3) D={2,3}s C={3} (V, D, C)", EmitterID, EmissionHoleM2, durationS, color);
#endif
        }

        /// <summary>
        /// Sets the particle and velocity output of this particle emitter based on a given speed,
        /// including the ability to change particle duration.
        /// The rate of particle emission will be calculated from the speed and particle size.
        /// </summary>
        /// <param name="initialVelocityMpS">The meter per second speed particles should emit with.</param>
        /// <param name="durationS">The lifespan of each particle in seconds.</param>
        public void SetOutputVelocity(float initialVelocityMpS, float durationS)
        {
            SetOutputVelocity(initialVelocityMpS);
            Emitter.ParticleDuration = EmitterData.LifetimeFactor * durationS;

#if DEBUG_EMITTER_INPUT
            if (InputCycle == 0)
                Trace.TraceInformation("Emitter{0}({1:F6}m^2) IV={2,7:F3}m/s V={3,7:F3}m^3/s P={4,7:F3}p/s (IV, V)", EmitterID, EmissionHoleM2, initialVelocityMpS, volumeM3pS, Emitter.ParticlesPerSecond);
#endif
        }

        /// <summary>
        /// Sets the particle and velocity output of this particle emitter based on a given speed,
        /// including the ability to change particle duration and color.
        /// The rate of particle emission will be calculated from the speed and particle size.
        /// </summary>
        /// <param name="initialVelocityMpS">The meter per second speed particles should emit with.</param>
        /// <param name="durationS">The lifespan of each particle in seconds.</param>
        /// <param name="color">A color struct giving the color of particles to emit.</param>
        public void SetOutputVelocity(float initialVelocityMpS, float durationS, Color color)
        {
            SetOutputVelocity(initialVelocityMpS);
            Emitter.ParticleDuration = EmitterData.LifetimeFactor * durationS;
            Emitter.ParticleColor = color;
            Emitter.ParticleColor.A = (byte)(Emitter.ParticleColor.A * EmitterData.Opacity);

#if DEBUG_EMITTER_INPUT
            if (InputCycle == 0)
                Trace.TraceInformation("Emitter{0}({1:F6}m^2) IV={2,7:F3}m/s V={3,7:F3}m^3/s P={4,7:F3}p/s D={5,3}s C={6} (IV, V, D, C)", EmitterID, EmissionHoleM2, initialVelocityMpS, volumeM3pS, Emitter.ParticlesPerSecond, durationS, color);
#endif
        }

        /// <summary>
        /// Sets the particle and velocity output of this particle emitter based on a given speed and emission rate,
        /// including the ability to change particle duration.
        /// Neither the speed nor emission rate will be automatically calculated, which may lead to displeasing results.
        /// In most cases, other SetOutput methods will do better at matching particle speed and the number of particles.
        /// </summary>
        /// <param name="initialVelocityMpS">The meter per second speed particles should emit with.</param>
        /// <param name="volumeM3pS">The cubic meter volume of particles to emit every second.</param>
        /// <param name="durationS">The lifespan of each particle in seconds.</param>
        public void SetOutputVelocity(float initialVelocityMpS, float volumeM3pS, float durationS)
        {
            Emitter.XNAInitialVelocity = Emitter.EmitterData.InitialVelocityFactor * initialVelocityMpS;
            Emitter.ParticlesPerSecond = EmitterData.RateFactor * volumeM3pS / ParticleVolumeM3;
            Emitter.ParticleDuration = EmitterData.LifetimeFactor * durationS;

#if DEBUG_EMITTER_INPUT
            if (InputCycle == 0)
                Trace.TraceInformation("Emitter{0}({1:F6}m^2) IV={2,7:F3}m/s V={3,7:F3}m^3/s P={4,7:F3}p/s (IV, V)", EmitterID, EmissionHoleM2, initialVelocityMpS, volumeM3pS, Emitter.ParticlesPerSecond);
#endif
        }

        /// <summary>
        /// Sets the particle and velocity output of this particle emitter based on a given speed and emission rate,
        /// including the ability to change particle duration and color.
        /// Neither the speed nor emission rate will be automatically calculated, which may lead to displeasing results.
        /// In most cases, other SetOutput methods will do better at matching particle speed and the number of particles.
        /// </summary>
        /// <param name="initialVelocityMpS">The meter per second speed particles should emit with.</param>
        /// <param name="volumeM3pS">The cubic meter volume of particles to emit every second.</param>
        /// <param name="durationS">The lifespan of each particle in seconds.</param>
        /// <param name="color">A color struct giving the color of particles to emit.</param>
        public void SetOutputVelocity(float initialVelocityMpS, float volumeM3pS, float durationS, Color color)
        {
            Emitter.XNAInitialVelocity = Emitter.EmitterData.InitialVelocityFactor * initialVelocityMpS;
            Emitter.ParticlesPerSecond = EmitterData.RateFactor * volumeM3pS / ParticleVolumeM3;
            Emitter.ParticleDuration = EmitterData.LifetimeFactor * durationS;
            Emitter.ParticleColor = color;
            Emitter.ParticleColor.A = (byte)(Emitter.ParticleColor.A * EmitterData.Opacity);

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

        readonly ParticleVertex[] Vertices;
        readonly VertexDeclaration VertexDeclaration;
        readonly DynamicVertexBuffer VertexBuffer;
        readonly IndexBuffer IndexBuffer;

        readonly float[] PerlinOffset;
        readonly float[] PerlinFrequency;

        struct ParticleVertex
        {
            public Vector4 StartPosition_StartTime;
            public Vector4 InitialVelocity_EndTime;
            public Vector4 TargetVelocity_TargetTime;
            public Vector4 TileXY_Vertex_ID;
            public Vector4 Size_Rotation;
            public Color Color;

            public static readonly VertexElement[] VertexElements =
            {
                new VertexElement(0 * 16, VertexElementFormat.Vector4, VertexElementUsage.Position, 0),
                new VertexElement(1 * 16, VertexElementFormat.Vector4, VertexElementUsage.Position, 1),
                new VertexElement(2 * 16, VertexElementFormat.Vector4, VertexElementUsage.Position, 2),
                new VertexElement(3 * 16, VertexElementFormat.Vector4, VertexElementUsage.Position, 3),
                new VertexElement(4 * 16, VertexElementFormat.Vector4, VertexElementUsage.Position, 4),
                new VertexElement(5 * 16, VertexElementFormat.Color, VertexElementUsage.Position, 5)
            };

            // Vector4 is 4 floats XYZW, Color is 4 bytes RGBA (bit-packed into a single int)
            public static int VertexStride = sizeof(float) * 4 * 5 + sizeof(byte) * 4;
        }

        internal ParticleEmitterData EmitterData;
        internal Vector3 XNAInitialVelocity;
        internal Vector3 XNAFinalVelocity;
        internal float ParticlesPerSecond;
        internal float ParticleDuration;
        internal Color ParticleColor;

        internal int SpriteCount;

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

        ParticleEmitterViewer ParticleViewer;
        GraphicsDevice graphicsDevice;

        public ParticleEmitterPrimitive(ParticleEmitterViewer particleViewer, ParticleEmitterData data, MSTSWagonViewer car, WorldPosition worldPosition)
        {
            ParticleViewer = particleViewer;
            EmitterData = data;
            graphicsDevice = ParticleViewer.Viewer.GraphicsDevice;
            Vertices = new ParticleVertex[EmitterData.MaxParticles * VerticesPerParticle];
            VertexDeclaration = new VertexDeclaration(ParticleVertex.VertexStride, ParticleVertex.VertexElements);
            VertexBuffer = new DynamicVertexBuffer(graphicsDevice, VertexDeclaration, EmitterData.MaxParticles * VerticesPerParticle, BufferUsage.WriteOnly);
            IndexBuffer = InitIndexBuffer(graphicsDevice, EmitterData.MaxParticles * IndiciesPerParticle);

            XNAInitialVelocity = data.InitialVelocityFactor;
            XNAFinalVelocity = data.FinalVelocityMpS;
            ParticlesPerSecond = 0;
            ParticleDuration = 3;
            ParticleColor = Color.White;

            SpriteCount = EmitterData.AtlasWidth * EmitterData.AtlasHeight;

            CarViewer = car;
            WorldPosition = worldPosition;

            // Initialize the particle accumulator to a random value to de-sync particle emitters from eachother
            AccumulatedParticles = -(float)Viewer.Random.NextDouble() * 5.0f;

            // Pre-compute some randomization for the "smooth" randomness method
            if (!EmitterData.ChaoticRandomization)
            {
                // Determine random, but fixed, time offsets for perlin noise generation
                // Range from 0 to 30,000 seconds
                PerlinOffset = new float[] {
                    (float)Viewer.Random.NextDouble() * 30000f,
                    (float)Viewer.Random.NextDouble() * 30000f,
                    (float)Viewer.Random.NextDouble() * 30000f,
                    (float)Viewer.Random.NextDouble() * 30000f,
                    (float)Viewer.Random.NextDouble() * 30000f,
                    (float)Viewer.Random.NextDouble() * 30000f,
                    (float)Viewer.Random.NextDouble() * 30000f,
                    (float)Viewer.Random.NextDouble() * 30000f,
                    (float)Viewer.Random.NextDouble() * 30000f,
                };

                // Determine random, but fixed, speed multiplier for perlin noise generation
                // This should make the noise feel less regular/repeatable
                // Range from 0.90 to 1.1
                PerlinFrequency = new float[] {
                    0.9f + (float)Viewer.Random.NextDouble() * 0.2f,
                    0.9f + (float)Viewer.Random.NextDouble() * 0.2f,
                    0.9f + (float)Viewer.Random.NextDouble() * 0.2f,
                    0.9f + (float)Viewer.Random.NextDouble() * 0.2f,
                    0.9f + (float)Viewer.Random.NextDouble() * 0.2f,
                    0.9f + (float)Viewer.Random.NextDouble() * 0.2f,
                    0.9f + (float)Viewer.Random.NextDouble() * 0.2f,
                    0.9f + (float)Viewer.Random.NextDouble() * 0.2f,
                    0.9f + (float)Viewer.Random.NextDouble() * 0.2f,
                };
            }
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
            get { return EmitterData.NozzleDiameterM; }
        }

        void RetireActiveParticles(float currentTime)
        {
            while (FirstActiveParticle != FirstNewParticle)
            {
                int vertex = FirstActiveParticle * VerticesPerParticle;
                float expiry = Vertices[vertex].InitialVelocity_EndTime.W;

                // Stop as soon as we find the first particle which hasn't expired.
                if (expiry > currentTime)
                    break;

                // Expire particle.
                Vertices[vertex].StartPosition_StartTime.W = (float)DrawCounter;
                FirstActiveParticle = (FirstActiveParticle + 1) % EmitterData.MaxParticles;
            }
        }

        void ForceRetireParticles(int count)
        {
            for (int i = 0; i < count; i++)
            {
                int NextActiveParticle = (FirstActiveParticle + 1) % EmitterData.MaxParticles;

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

                FirstRetiredParticle = (FirstRetiredParticle + 1) % EmitterData.MaxParticles;
            }
        }

        int GetCountFreeParticles()
        {
            var nextFree = (FirstFreeParticle + 1) % EmitterData.MaxParticles;

            if (nextFree <= FirstRetiredParticle)
                return FirstRetiredParticle - nextFree;

            return (EmitterData.MaxParticles - nextFree) + FirstRetiredParticle;
        }

        public void Update(float currentTime, ElapsedTime elapsedTime)
        {
            if (ParticlesPerSecond > 0)
            {
                // Limit particle spawn rate to try and prevent overfilling the particle buffer
                // This should only be needed when the particle spawn rate is visually excessive
                float effectiveParticlesPerSecond = Math.Min(ParticlesPerSecond, (EmitterData.MaxParticles * 0.9f) / (ParticleDuration * (1.0f + EmitterData.LifetimeVariationFactor)));

                AccumulatedParticles += elapsedTime.ClockSeconds * effectiveParticlesPerSecond;

                int maxNewParticles = GetCountFreeParticles() - (int)(EmitterData.MaxParticles * 0.025f);

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

                    float initialSpeed = XNAInitialVelocity.Length();
                    Vector3 carVelocity = new Vector3(CarViewer.Velocity[0], CarViewer.Velocity[1], -CarViewer.Velocity[2]);

                    float emitTime = currentTime - elapsedTime.ClockSeconds;

                    float deltaTime = elapsedTime.ClockSeconds / AccumulatedParticles;

                    for (int i = 0; i < numToBeEmitted; i++)
                    {
                        emitTime += deltaTime;

                        int nextFreeParticle = (FirstFreeParticle + 1) % EmitterData.MaxParticles;
                        int vertex = FirstFreeParticle * VerticesPerParticle;
                        int texture = Viewer.Random.Next(SpriteCount); // Randomizes particle texture to any texture on the sheet.

                        Vector3 position = EmitterData.PositionM;

                        Vector3 initialVelocity = XNAInitialVelocity;
                        Vector3 finalVelocity = XNAFinalVelocity;

                        float initialRot = 2.0f * (float)(Viewer.Random.NextDouble() * Math.PI);
                        float rotSpeed = EmitterData.RotationVariation;

                        float duration = ParticleDuration;

                        float settling = EmitterData.SettlingFactor;

                        position.X += ((float)Viewer.Random.NextDouble() - 0.5f) * 2.0f * EmitterData.PositionVariationM.X;
                        position.Y += ((float)Viewer.Random.NextDouble() - 0.5f) * 2.0f * EmitterData.PositionVariationM.Y;
                        position.Z += ((float)Viewer.Random.NextDouble() - 0.5f) * 2.0f * EmitterData.PositionVariationM.Z;

                        if (EmitterData.ChaoticRandomization)
                        {
                            // "Chaotic" randomization: Uses random output directly, making for sudden changes
                            initialVelocity.X += ((float)Viewer.Random.NextDouble() - 0.5f) * 2.0f * EmitterData.InitialVelocityVariationFactor.X * initialSpeed;
                            initialVelocity.Y += ((float)Viewer.Random.NextDouble() - 0.5f) * 2.0f * EmitterData.InitialVelocityVariationFactor.Y * initialSpeed;
                            initialVelocity.Z += ((float)Viewer.Random.NextDouble() - 0.5f) * 2.0f * EmitterData.InitialVelocityVariationFactor.Z * initialSpeed;

                            finalVelocity.X += ((float)Viewer.Random.NextDouble() - 0.5f) * 2.0f * EmitterData.FinalVelocityVariationMpS.X;
                            finalVelocity.Y += ((float)Viewer.Random.NextDouble() - 0.5f) * 2.0f * EmitterData.FinalVelocityVariationMpS.Y;
                            finalVelocity.Z += ((float)Viewer.Random.NextDouble() - 0.5f) * 2.0f * EmitterData.FinalVelocityVariationMpS.Z;

                            rotSpeed *= ((float)Viewer.Random.NextDouble() - 0.5f) * 2.0f;

                            duration *= 1 + ((float)Viewer.Random.NextDouble() - 0.5f) * 2.0f * EmitterData.LifetimeVariationFactor;

                            settling += ((float)Viewer.Random.NextDouble() - 0.5f) * 2.0f * EmitterData.SettlingVariationFactor;
                        }
                        else
                        {
                            // "Smoothed" randomization: Uses perlin noise for smooth changes in the random value
                            initialVelocity.X += Noise.Generate(emitTime * PerlinFrequency[0] + PerlinOffset[0]) * EmitterData.InitialVelocityVariationFactor.X * initialSpeed;
                            initialVelocity.Y += Noise.Generate(emitTime * PerlinFrequency[1] + PerlinOffset[1]) * EmitterData.InitialVelocityVariationFactor.Y * initialSpeed;
                            initialVelocity.Z += Noise.Generate(emitTime * PerlinFrequency[2] + PerlinOffset[2]) * EmitterData.InitialVelocityVariationFactor.Z * initialSpeed;

                            finalVelocity.X += Noise.Generate(emitTime * PerlinFrequency[3] + PerlinOffset[3]) * EmitterData.FinalVelocityVariationMpS.X;
                            finalVelocity.Y += Noise.Generate(emitTime * PerlinFrequency[4] + PerlinOffset[4]) * EmitterData.FinalVelocityVariationMpS.Y;
                            finalVelocity.Z += Noise.Generate(emitTime * PerlinFrequency[5] + PerlinOffset[5]) * EmitterData.FinalVelocityVariationMpS.Z;

                            rotSpeed *= Noise.Generate(emitTime * PerlinFrequency[6] + PerlinOffset[6]);

                            duration *= 1 + Noise.Generate(emitTime * PerlinFrequency[7] + PerlinOffset[7]) * EmitterData.LifetimeVariationFactor;

                            settling += Noise.Generate(emitTime * PerlinFrequency[8] + PerlinOffset[8]) * EmitterData.SettlingVariationFactor;
                        }

                        position = Vector3.Transform(position, transform) + WorldPosition.XNAMatrix.Translation;

                        // Interpolate the position of the particle in-between frames
                        position -= carVelocity * (currentTime - emitTime);

                        initialVelocity = Vector3.Transform(initialVelocity, rotation);
                        finalVelocity = Vector3.Transform(finalVelocity, rotY);

                        // Add on velocity of attached train car
                        initialVelocity += carVelocity;

                        // Add wind speed (not randomized here)
                        finalVelocity.X += ParticleViewer.Viewer.Simulator.Weather.WindInstantaneousSpeedMpS * ParticleViewer.Viewer.Simulator.Weather.WindInstantaneousDirection.X * EmitterData.WindEffect;
                        finalVelocity.Z += ParticleViewer.Viewer.Simulator.Weather.WindInstantaneousSpeedMpS * ParticleViewer.Viewer.Simulator.Weather.WindInstantaneousDirection.Y * EmitterData.WindEffect;

                        // Amount by which particles initially expand depends on particle speed; faster particles expand more due to 'high pressure' at exhaust
                        float speedIntensity = (float)Math.Sqrt(initialSpeed);
                        float initialExpansion = speedIntensity * EmitterData.InitialExpansionFactor;
                        // Speed at which particles slow down depends on change in particle speed; faster particles slow down faster due to 'drag' from speed difference
                        settling /= speedIntensity / 5.0f + 1.0f; // Note: The / 5 is largely arbitrary, chosen to give results that look good

                        for (var j = 0; j < VerticesPerParticle; j++)
                        {
                            Vertices[vertex + j].StartPosition_StartTime = new Vector4(position, emitTime);
                            Vertices[vertex + j].InitialVelocity_EndTime = new Vector4(initialVelocity, emitTime + duration);
                            Vertices[vertex + j].TargetVelocity_TargetTime = new Vector4(finalVelocity, settling);
                            Vertices[vertex + j].TileXY_Vertex_ID = new Vector4(WorldPosition.TileX, WorldPosition.TileZ, j, texture);
                            Vertices[vertex + j].Size_Rotation = new Vector4(initialExpansion, EmitterData.ExpansionSpeed, initialRot, rotSpeed);
                            Vertices[vertex + j].Color = ParticleColor;
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
                var numParticlesToAddAtEnd = EmitterData.MaxParticles - FirstNewParticle;
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
                    var numParticlesAtEnd = EmitterData.MaxParticles - FirstActiveParticle;
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
                    shader.TextureAtlasSizeXY = new Vector2(emitter.EmitterData.AtlasWidth, emitter.EmitterData.AtlasHeight);
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
