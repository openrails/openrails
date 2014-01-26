// COPYRIGHT 2011, 2012, 2013, 2014 by the Open Rails project.
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
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using MSTS;

namespace ORTS
{
    public struct ParticleEmitterData
    {
        public readonly Vector3 XNALocation;
        public readonly Vector3 XNADirection;
        public readonly float NozzleWidth;
        public readonly float MaxParticlesPerSecond;
        public readonly float MaxParticleDuration;

        public ParticleEmitterData(STFReader stf)
        {
            stf.MustMatch("(");
            XNALocation.X = stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            XNALocation.Y = stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            XNALocation.Z = -stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            XNADirection.X = stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            XNADirection.Y = stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            XNADirection.Z = -stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            XNADirection.Normalize();
            NozzleWidth = stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            MaxParticlesPerSecond = 100f;
            MaxParticleDuration = 100f;
            stf.SkipRestOfBlock();
        }
    }

    public class ParticleEmitterDrawer
    {
        public const float VolumeScale = 1f / 100;
        public const float Rate = 10;
        public const float DecelerationTime = 0.5f;
        public const float InitialSpreadRate = 1;
        public const float SpreadRate = 1.5f;
        public const float DurationVariation = 0.5f; // Duration varies +/-50%

        readonly Viewer3D Viewer;
        readonly ParticleEmitterMaterial Material;
        readonly float EmissionHoleM2 = 1;
        readonly ParticleEmitter Emitter;

#if DEBUG_EMITTER_INPUT
        const int InputCycleLimit = 600;
        static int EmitterIDIndex = 0;
        int EmitterID;
        int InputCycle;
#endif

        public ParticleEmitterDrawer(Viewer3D viewer, ParticleEmitterData data, WorldPosition worldPosition)
        {
            Viewer = viewer;
            Material = (ParticleEmitterMaterial)viewer.MaterialManager.Load("ParticleEmitter");
            EmissionHoleM2 = (MathHelper.Pi * ((data.NozzleWidth / 2f) * (data.NozzleWidth / 2f)));
            Emitter = new ParticleEmitter(viewer.GraphicsDevice, data, worldPosition);
#if DEBUG_EMITTER_INPUT
            EmitterID = ++EmitterIDIndex;
            InputCycle = Program.Random.Next(InputCycleLimit);
#endif
        }

        public void Initialize(Texture2D texture)
        {
            Material.Texture = texture;
        }

        public void SetOutput(float volumeM3pS)
        {
            // TODO: The values here are out by a factor of 100 here it seems. The XNAInitialVelocity should need no multiplication or division factors.
            Emitter.ParticlesPerSecond = volumeM3pS / EmissionHoleM2 * VolumeScale * Rate;
            Emitter.XNAInitialVelocity = Emitter.EmitterData.XNADirection * volumeM3pS / EmissionHoleM2 * VolumeScale;
#if DEBUG_EMITTER_INPUT
            if (InputCycle == 0)
                Trace.TraceInformation("Emitter{0}({1:F6}m^2) V={2,7:F3}m^3/s P={3,7:F3}p/s IV={4,7:F3}m/s", EmitterID, EmissionHoleM2, volumeM3pS, Emitter.ParticlesPerSecond, Emitter.XNAInitialVelocity.Length());
#endif
        }

        public void SetOutput(float volumeM3pS, float durationS)
        {
            SetOutput(volumeM3pS);
            Emitter.ParticleDuration = durationS;
#if DEBUG_EMITTER_INPUT
            if (InputCycle == 0)
                Trace.TraceInformation("Emitter{0}({1:F6}m^3) D={2,3}s", EmitterID, EmissionHoleM2, durationS);
#endif
        }

        public void SetColor(Color particleColor)
        {
            Emitter.ParticleColor = particleColor;
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
            Material.Mark();
        }
    }

    public class ParticleEmitter : RenderPrimitive
    {
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
            public Vector4 InitialVelocity_EndTime;
            public Vector4 TargetVelocity_TargetTime;
            public Short4 TileXY_Vertex_ID;
            public Color Color_Random;

            public static readonly VertexElement[] VertexElements =
            {
                new VertexElement(0, 0, VertexElementFormat.Vector4, VertexElementMethod.Default, VertexElementUsage.Position, 0),
                new VertexElement(0, 16, VertexElementFormat.Vector4, VertexElementMethod.Default, VertexElementUsage.Position, 1),
                new VertexElement(0, 16 + 16, VertexElementFormat.Vector4, VertexElementMethod.Default, VertexElementUsage.Position, 2),
                new VertexElement(0, 16 + 16 + 16, VertexElementFormat.Short4, VertexElementMethod.Default, VertexElementUsage.Position, 3),
                new VertexElement(0, 16 + 16 + 16 + 8, VertexElementFormat.Color, VertexElementMethod.Default, VertexElementUsage.Position, 4)
            };
        }

        internal ParticleEmitterData EmitterData;
        internal Vector3 XNAInitialVelocity;
        internal Vector3 XNATargetVelocity;
        internal float ParticlesPerSecond;
        internal float ParticleDuration;
        internal Color ParticleColor;

        internal WorldPosition WorldPosition;
        internal WorldPosition LastWorldPosition;

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

        public ParticleEmitter(GraphicsDevice graphicsDevice, ParticleEmitterData data, WorldPosition worldPosition)
        {
            MaxParticles = (int)(data.MaxParticlesPerSecond * data.MaxParticleDuration);
            Vertices = new ParticleVertex[MaxParticles * VerticiesPerParticle];
            VertexDeclaration = new VertexDeclaration(graphicsDevice, ParticleVertex.VertexElements);
            VertexStride = Marshal.SizeOf(typeof(ParticleVertex));
            VertexBuffer = new DynamicVertexBuffer(graphicsDevice, typeof(ParticleVertex), MaxParticles * VerticiesPerParticle, BufferUsage.WriteOnly);
            IndexBuffer = InitIndexBuffer(graphicsDevice, MaxParticles * IndiciesPerParticle);

            EmitterData = data;
            XNAInitialVelocity = data.XNADirection;
            XNATargetVelocity = Vector3.Up;
            ParticlesPerSecond = 0;
            ParticleDuration = 3;
            ParticleColor = Color.White;

            WorldPosition = worldPosition;
            LastWorldPosition = new WorldPosition(worldPosition);
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

                index += VerticiesPerParticle;
            }
            var indexBuffer = new IndexBuffer(graphicsDevice, sizeof(ushort) * numIndicies, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
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
                var vertex = FirstActiveParticle * VerticiesPerParticle;
                var expiry = Vertices[vertex].InitialVelocity_EndTime.W;

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

        public void Update(float currentTime, ElapsedTime elapsedTime)
        {
            var velocity = WorldPosition.Location - LastWorldPosition.Location;
            velocity.X += (WorldPosition.TileX - LastWorldPosition.TileX) * 2048;
            velocity.Z += (WorldPosition.TileZ - LastWorldPosition.TileZ) * 2048;
            velocity.Z *= -1;
            velocity /= elapsedTime.ClockSeconds;
            LastWorldPosition.Location = WorldPosition.Location;
            LastWorldPosition.TileX = WorldPosition.TileX;
            LastWorldPosition.TileZ = WorldPosition.TileZ;

            RetireActiveParticles(currentTime);
            FreeRetiredParticles();

            ParticlesToEmit += elapsedTime.ClockSeconds * ParticlesPerSecond;

            var numParticlesAdded = 0;
            var numToBeEmitted = (int)ParticlesToEmit;
            var numCanBeEmitted = GetCountFreeParticles();
            var numToEmit = Math.Min(numToBeEmitted, numCanBeEmitted);

            var rotation = WorldPosition.XNAMatrix;
            rotation.Translation = Vector3.Zero;

            var position = Vector3.Transform(EmitterData.XNALocation, rotation) + WorldPosition.XNAMatrix.Translation;
            var globalInitialVelocity = Vector3.Transform(XNAInitialVelocity, rotation) + velocity;
            // TODO: This should only be rotated about the Y axis and not get fully rotated.
            var globalTargetVelocity = Vector3.Transform(XNATargetVelocity, rotation);

            for (var i = 0; i < numToEmit; i++)
            {
                var particle = (FirstFreeParticle + 1) % MaxParticles;
                var vertex = particle * VerticiesPerParticle;
                var texture = Program.Random.Next(16); // Randomizes emissions.
                var color_Random = new Color(ParticleColor, (float)Program.Random.NextDouble());

                // Initial velocity varies in X and Z only.
                var initialVelocity = globalInitialVelocity;
                initialVelocity.X += (float)(Program.Random.NextDouble() - 0.5f) * ParticleEmitterDrawer.InitialSpreadRate;
                initialVelocity.Z += (float)(Program.Random.NextDouble() - 0.5f) * ParticleEmitterDrawer.InitialSpreadRate;

                // Target/final velocity vaies in X, Y and Z.
                var targetVelocity = globalTargetVelocity;
                targetVelocity.X += (float)(Program.Random.NextDouble() - 0.5f) * ParticleEmitterDrawer.SpreadRate;
                targetVelocity.Y += (float)(Program.Random.NextDouble() - 0.5f) * ParticleEmitterDrawer.SpreadRate;
                targetVelocity.Z += (float)(Program.Random.NextDouble() - 0.5f) * ParticleEmitterDrawer.SpreadRate;

                // Duration is variable too.
                var duration = ParticleDuration * (1 + (float)(Program.Random.NextDouble() - 0.5f) * 2 * ParticleEmitterDrawer.DurationVariation);

                for (var j = 0; j < VerticiesPerParticle; j++)
                {
                    Vertices[vertex + j].StartPosition_StartTime = new Vector4(position, currentTime);
                    Vertices[vertex + j].InitialVelocity_EndTime = new Vector4(initialVelocity, currentTime + duration);
                    Vertices[vertex + j].TargetVelocity_TargetTime = new Vector4(targetVelocity, ParticleEmitterDrawer.DecelerationTime);
                    Vertices[vertex + j].TileXY_Vertex_ID = new Short4(WorldPosition.TileX, WorldPosition.TileZ, j, texture);
                    Vertices[vertex + j].Color_Random = color_Random;
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
}
