// COPYRIGHT 2011, 2012, 2013 by the Open Rails project.
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using MSTS;

namespace ORTS
{
    public struct ParticleEmitterData
    {
        public ParticleEmitterData(STFReader stf)
        {
            stf.MustMatch("(");
            XNAOffset.X = stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            XNAOffset.Y = stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            XNAOffset.Z = -stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            XNADirection.X = stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            XNADirection.Y = stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);  // May as well go up by default.
            XNADirection.Z = -stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            NozzleWidth = stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            stf.SkipRestOfBlock();

            MaxParticlesPerSecond = 60f;
            ParticlesPerSecond = 60f;
            ParticleDuration = 60f;    // May come from the STF in the future.
            texturePath = string.Empty; // May come from the STF in the future.
            ParticleDurationRandomness = 1;
            ParticleVelocitySensitivity = 0;
            MinHorizontalVelocity = 0;
            MaxHorizontalVelocity = 50;
            MinVerticalVelocity = -10;
            MaxVerticalVelocity = 50;
            EndVelocity = 0;
            MinRotateSpeed = -2;
            MaxRotateSpeed = 2;
            MinStartSize = 10;
            MaxStartSize = 10;
            MinStartSize = 100;
            MinEndSize = 100;
            MaxEndSize = 200;
        }

        public Vector3 XNAOffset;
        public Vector3 XNADirection;
        public float NozzleWidth;
        public float MaxParticlesPerSecond;
        public float ParticlesPerSecond;
        public float ParticleDuration;
        public float ParticleDurationRandomness;
        public float ParticleVelocitySensitivity;
        public float MinHorizontalVelocity;
        public float MaxHorizontalVelocity;
        public float MinVerticalVelocity;
        public float MaxVerticalVelocity;
        public float EndVelocity;
        public float MinRotateSpeed;
        public float MaxRotateSpeed;
        public float MinStartSize;
        public float MaxStartSize;
        public float MinEndSize;
        public float MaxEndSize;
        public string texturePath;
    }

    public class ParticleEmitterDrawer
    {
        Viewer3D Viewer;
        ParticleEmitterMaterial ParticleMaterial;
        float ParticleEmissionHoleM3 = 1;

        // Classes reqiring instantiation
        public ParticleEmitter emitter;
                

        public WorldPosition WorldPosition
        {
            set { emitter.WorldPosition = value; }
        }
        public ParticleEmitterDrawer(Viewer3D viewer, ParticleEmitterData data)
        {
            Viewer = viewer;
            ParticleMaterial = (ParticleEmitterMaterial)viewer.MaterialManager.Load("ParticleEmitter");
            ParticleEmissionHoleM3 = (MathHelper.Pi * ((data.NozzleWidth / 2f) * (data.NozzleWidth / 2f)));
            emitter = new ParticleEmitter(Viewer.RenderProcess, data);
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            emitter.CameraTileXZ.X = Viewer.Camera.TileX;
            emitter.CameraTileXZ.Y = Viewer.Camera.TileZ;
            emitter.Update(Viewer.Simulator.GameTime, elapsedTime);
            var XNAPrecipWorldLocation = Matrix.Identity;

            if(emitter.HasParticlesToRender())
                frame.AddPrimitive(ParticleMaterial, emitter, RenderPrimitiveGroup.Particles, ref XNAPrecipWorldLocation);
        }
        public void SetTexture(Texture2D texture)
        {
            ParticleMaterial.Texture = texture;
        }
        public void SetEmissionRate(float particleVolumeM3)
        {
            emitter.EmitterData.ParticlesPerSecond = (particleVolumeM3 / ParticleEmissionHoleM3) * .10f;
            emitter.ParticlesPerSecond = emitter.EmitterData.ParticlesPerSecond;
        }
        public void SetParticleDuration(float particleDuration)
        {
            emitter.EmitterData.ParticleDuration = particleDuration;
        }
        public void SetEmissionColor(Color particleColor)
        {
            emitter.ParticleColor = particleColor;
        }

        [CallOnThread("Loader")]
        internal void Mark()
        {
            ParticleMaterial.Mark();
        }
    }

    public class ParticleEmitter : RenderPrimitive
    {
        struct ParticleVertex
        {
            public Vector4 position_time;
            public Short4 tileXY_ID;
            public Color color_random;

            public const int SizeInBytes = 32;

            public static readonly VertexElement[] VertexElements =
            {
                new VertexElement(0, 0, VertexElementFormat.Vector4, VertexElementMethod.Default, VertexElementUsage.Position, 0),
                new VertexElement(0, 16, VertexElementFormat.Short4, VertexElementMethod.Default, VertexElementUsage.Position, 1),
                new VertexElement(0, 24, VertexElementFormat.Color, VertexElementMethod.Default, VertexElementUsage.Position, 2)
            };
        }

        static int VERTICES_PER_PARTICLE = 4;
        static int PRIMITIVES_PER_PARTICLE = 2;
        static int INDICES_PER_PARTICLE = 6;

        public Vector2 CameraTileXZ = Vector2.Zero;

        public Vector3 XNADirection { get; private set; }

        public ParticleEmitterData EmitterData;
        int maxParticles;

        float particlesPerSecond;
        public float ParticlesPerSecond
        {
            set { particlesPerSecond = Math.Min(value, EmitterData.MaxParticlesPerSecond); }
            private get { return particlesPerSecond; }
        }

        public Color ParticleColor { get; set; }

        float particlesToEmit;

        Random rng = new Random();
        RenderProcess renderProcess;
        ParticleVertex[] vertices;

        VertexDeclaration vd;
        DynamicVertexBuffer vb;
        IndexBuffer ib;

        public WorldPosition WorldPosition { get; set; }

        int firstActiveParticle;
        int firstNewParticle;
        int firstFreeParticle;
        int firstRetiredParticle;

        float timeParticlesLastEmitted;
        int drawCounter;

        public ParticleEmitter(RenderProcess renderProcess, ParticleEmitterData data)
        {
            ParticleColor = Color.White;
            EmitterData = data;
            this.renderProcess = renderProcess;
            maxParticles = (int)(EmitterData.MaxParticlesPerSecond * data.ParticleDuration);
            vd = new VertexDeclaration(renderProcess.GraphicsDevice, ParticleVertex.VertexElements);
            InitVB(renderProcess.GraphicsDevice);
            InitIB(renderProcess.GraphicsDevice);
        }

        void InitVB(GraphicsDevice device)
        {
            vb = new DynamicVertexBuffer(device, typeof(ParticleVertex), maxParticles * VERTICES_PER_PARTICLE, BufferUsage.WriteOnly);
            vertices = new ParticleVertex[maxParticles * VERTICES_PER_PARTICLE];
        }

        void InitIB(GraphicsDevice device)
        {
            var numIndices = maxParticles * INDICES_PER_PARTICLE;
            ib = new IndexBuffer(device, sizeof(ushort) * numIndices, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
            var indices = new ushort[numIndices];

            var idx = 0;

            for (var i = 0; i < numIndices; i += INDICES_PER_PARTICLE)
            {
                indices[i] = (ushort)idx;
                indices[i + 1] = (ushort)(idx + 1);
                indices[i + 2] = (ushort)(idx + 2);

                indices[i + 3] = (ushort)(idx + 2);
                indices[i + 4] = (ushort)(idx + 3);
                indices[i + 5] = (ushort)(idx);

                idx += (ushort)VERTICES_PER_PARTICLE;
            }
            ib.SetData<ushort>(indices);
        }

        void RetireActiveParticles(float currentTime)
        {
            var particleDuration = EmitterData.ParticleDuration;

            while (firstActiveParticle != firstNewParticle)
            {
                var firstVertexOfParticle = firstActiveParticle * VERTICES_PER_PARTICLE;
                var particleAge = currentTime - vertices[firstVertexOfParticle].position_time.W;

                if (particleAge < particleDuration)
                    break;

                vertices[firstVertexOfParticle].position_time.W = (float)drawCounter;

                firstActiveParticle = (firstActiveParticle + 1) % maxParticles;
            }
        }

        void FreeRetiredParticles()
        {
            while (firstRetiredParticle != firstActiveParticle)
            {
                var firstVertexOfParticle = firstRetiredParticle * VERTICES_PER_PARTICLE;
                var age = drawCounter - (int)vertices[firstVertexOfParticle].position_time.W;

                if (age < 2)
                    break;

                firstRetiredParticle = (firstRetiredParticle + 1) % maxParticles;
            }
        }

        int GetNumParticlesAvailableForEmission()
        {
            var nextFree = (firstFreeParticle + 1) % maxParticles;

            if (nextFree <= firstRetiredParticle)
                return firstRetiredParticle - nextFree;

            return (maxParticles - nextFree) + firstRetiredParticle;
        }

        public void Update(double currentTime, ElapsedTime elapsedTime)
        {
            var rotation = WorldPosition.XNAMatrix;
            rotation.Translation = Vector3.Zero;
            XNADirection = Vector3.Transform(EmitterData.XNADirection, rotation);

            RetireActiveParticles((float)currentTime);
            FreeRetiredParticles();

            var timeLastFrame = (float)currentTime - elapsedTime.ClockSeconds;
            var time = (float)currentTime;

            particlesToEmit += (elapsedTime.ClockSeconds * ParticlesPerSecond);

            var numParticlesAdded = 0;

            var numToBeEmitted = (int)particlesToEmit;
            var numCanBeEmitted = GetNumParticlesAvailableForEmission();
            var numToEmit = Math.Min(numToBeEmitted, numCanBeEmitted);

            var intervalPerParticle = (time - timeParticlesLastEmitted) / numToEmit; //Change to randomize time of emission.

            for (var i = 0; i < numToEmit; i++)
            {
                var nextFreeParticle = (firstFreeParticle + 1) % maxParticles;
                var newParticleVertexIndex = nextFreeParticle * VERTICES_PER_PARTICLE;
                var particleOffset = Vector3.Transform(EmitterData.XNAOffset, rotation);
                var particlePosition = WorldPosition.Location + particleOffset;
                var timeOfEmission = timeParticlesLastEmitted + intervalPerParticle;
                var positionTime = new Vector4(WorldPosition.XNAMatrix.Translation + particleOffset, timeOfEmission);
                var randomTextureOffset = (float)rng.Next(16); //Randomizes emissions.
                var color_random = new Color(ParticleColor, (float)rng.NextDouble());

                for (var j = 0; j < VERTICES_PER_PARTICLE; j++)
                {
                    vertices[newParticleVertexIndex + j].position_time = positionTime;
                    vertices[newParticleVertexIndex + j].tileXY_ID = new Short4(WorldPosition.TileX, WorldPosition.TileZ, j, randomTextureOffset);
                    vertices[newParticleVertexIndex + j].color_random = color_random;
                }

                firstFreeParticle = nextFreeParticle;
                particlesToEmit--;
                numParticlesAdded++;
            }

            if (numParticlesAdded > 0)
                timeParticlesLastEmitted = time;

            particlesToEmit = particlesToEmit - (int)particlesToEmit;
        }


        void AddNewParticlesToVertexBuffer()
        {
            var stride = ParticleVertex.SizeInBytes;

            if (firstNewParticle < firstFreeParticle)
            {
                var numParticlesToAdd = firstFreeParticle - firstNewParticle;
                vb.SetData(firstNewParticle * stride * VERTICES_PER_PARTICLE, vertices, firstNewParticle * VERTICES_PER_PARTICLE, numParticlesToAdd * VERTICES_PER_PARTICLE, stride, SetDataOptions.NoOverwrite);
            }
            else
            {
                var numParticlesToAddAtEnd = maxParticles - firstNewParticle;
                vb.SetData(firstNewParticle * stride * VERTICES_PER_PARTICLE, vertices, firstNewParticle * VERTICES_PER_PARTICLE, numParticlesToAddAtEnd * VERTICES_PER_PARTICLE, stride, SetDataOptions.NoOverwrite);
                if (firstFreeParticle > 0)
                    vb.SetData(0, vertices, 0, firstFreeParticle * VERTICES_PER_PARTICLE, stride, SetDataOptions.NoOverwrite);
            }

            firstNewParticle = firstFreeParticle;
        }

        public bool HasParticlesToRender()
        {
            return firstActiveParticle != firstFreeParticle;
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            if (firstNewParticle != firstFreeParticle)
                AddNewParticlesToVertexBuffer();

            if (HasParticlesToRender())
            {
                graphicsDevice.Indices = ib;
                graphicsDevice.VertexDeclaration = vd;
                graphicsDevice.Vertices[0].SetSource(vb, 0, ParticleVertex.SizeInBytes);

                if (firstActiveParticle < firstFreeParticle)
                {
                    var numParticles = firstFreeParticle - firstActiveParticle;
                    graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, firstActiveParticle * VERTICES_PER_PARTICLE, numParticles * VERTICES_PER_PARTICLE, firstActiveParticle * INDICES_PER_PARTICLE, numParticles * PRIMITIVES_PER_PARTICLE);
                }
                else
                {
                    var numParticlesAtEnd = maxParticles - firstActiveParticle;
                    graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, firstActiveParticle * VERTICES_PER_PARTICLE, numParticlesAtEnd * VERTICES_PER_PARTICLE, firstActiveParticle * INDICES_PER_PARTICLE, numParticlesAtEnd * PRIMITIVES_PER_PARTICLE);
                    if (firstFreeParticle > 0)
                        graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, firstFreeParticle * VERTICES_PER_PARTICLE, 0, firstFreeParticle * PRIMITIVES_PER_PARTICLE);
                }
            }

            drawCounter++;
        }
    }
}
