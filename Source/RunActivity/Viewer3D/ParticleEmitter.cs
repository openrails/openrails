// COPYRIGHT 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MSTS;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace ORTS
{
    public struct ParticleEmitterData
    {
        public ParticleEmitterData(STFReader stf)
        {
            stf.MustMatch("(");
            Offset.X = stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            Offset.Y = stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            Offset.Z = stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            Direction.X = stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            Direction.Y = stf.ReadFloat(STFReader.UNITS.Distance, 1.0f);  // May as well go up by default.
            Direction.Z = stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            NozzleWidth = stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            stf.SkipRestOfBlock();

            MaxParticlesPerSecond = 60; // May come from the STF in the future.
            ParticleDuration = 5.0f;    // May come from the STF in the future.
            texturePath = string.Empty; // May come from the STF in the future.
        }

        public Vector3 Offset;
        public Vector3 Direction;
        public float NozzleWidth;
        public float MaxParticlesPerSecond;
        public float ParticleDuration;
        public string texturePath;
    }

    public class ParticleEmitterDrawer
    {
        Viewer3D Viewer;
        ParticleEmitterMaterial ParticleMaterial;

        // Classes reqiring instantiation
        public ParticleEmitter emitter;

        public WorldPosition WorldPosition
        {
            set { emitter.WorldPosition = value; }
        }

        public ParticleEmitterDrawer(Viewer3D viewer, ParticleEmitterData data)
        {
            Viewer = viewer;
            //string texturePath = viewer.Simulator.BasePath + @"\GLOBAL\TEXTURES\dieselsmoke.ace";
            ParticleMaterial = (ParticleEmitterMaterial)viewer.MaterialManager.Load("ParticleEmitter");
            emitter = new ParticleEmitter(Viewer.RenderProcess, data);
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            emitter.CameraTileXZ.X = Viewer.Camera.TileX;
            emitter.CameraTileXZ.Y = Viewer.Camera.TileZ;

            emitter.Update(Viewer.Simulator.GameTime, elapsedTime);
            //ParticleMaterial.particleEmitterShader.
            
            Matrix XNAPrecipWorldLocation = Matrix.Identity;// Matrix.CreateTranslation(ViewerXNAPosition);

            if(emitter.HasParticlesToRender())
                frame.AddPrimitive(ParticleMaterial, emitter, RenderPrimitiveGroup.Particles, ref XNAPrecipWorldLocation);
        }

        public void SetTexture(Texture2D texture)
        {
            ParticleMaterial.Texture = texture;
        }

        public void SetEmissionRate(float particlesPerSecond)
        {
            emitter.ParticlesPerSecond = particlesPerSecond;
        }

        public void SetEmissionColor(Color particleColor)
        {
            emitter.ParticleColor = particleColor;
        }

        public void Reset()
        {
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

            maxParticles = (int)(data.MaxParticlesPerSecond * data.ParticleDuration);

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
            RetireActiveParticles((float)currentTime);
            FreeRetiredParticles();

            var timeLastFrame = (float)currentTime - elapsedTime.ClockSeconds;
            var time = (float)currentTime;

            particlesToEmit += (elapsedTime.ClockSeconds * ParticlesPerSecond);

            var numParticlesAdded = 0;

            var numToBeEmitted = (int)particlesToEmit;
            var numCanBeEmitted = GetNumParticlesAvailableForEmission();
            var numToEmit = Math.Min(numToBeEmitted, numCanBeEmitted);

            var intervalPerParticle = (time - timeParticlesLastEmitted) / numToEmit;

            for (var i = 0; i < numToEmit; i++)
            {
                var nextFreeParticle = (firstFreeParticle + 1) % maxParticles;

                var newParticleVertexIndex = nextFreeParticle * VERTICES_PER_PARTICLE;

                var particleOffset = EmitterData.Offset;
                var rotation = WorldPosition.XNAMatrix;
                rotation.Translation = Vector3.Zero;
                particleOffset = Vector3.Transform(particleOffset, Matrix.Invert(rotation));

                var particlePosition = WorldPosition.Location + particleOffset;

                var timeOfEmission = timeParticlesLastEmitted + (i * intervalPerParticle);

                var positionTime = new Vector4(WorldPosition.Location + particleOffset, timeOfEmission);
                positionTime.Z *= -1;

                var randomTextureOffset = (float)rng.Next(16);

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
