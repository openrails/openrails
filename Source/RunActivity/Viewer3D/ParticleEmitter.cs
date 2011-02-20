/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 
/// Principal Author:
///    Adam Miles
/// 
///     
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
            stf.MustMatch(")");

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
            ParticleMaterial = (ParticleEmitterMaterial)Materials.Load(Viewer.RenderProcess, "ParticleEmitterMaterial");
            emitter = new ParticleEmitter(Viewer.RenderProcess, data);
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            emitter.Update(Viewer.Simulator.GameTime, elapsedTime);
            //ParticleMaterial.particleEmitterShader.
            
            Matrix XNAPrecipWorldLocation = Matrix.Identity;// Matrix.CreateTranslation(ViewerXNAPosition);

            if(emitter.HasParticlesToRender())
                frame.AddPrimitive(ParticleMaterial, emitter, RenderPrimitiveGroup.Particles, ref XNAPrecipWorldLocation);
        }

        public void SetTexture(Texture2D texture)
        {
            ParticleMaterial.texture = texture;
        }

        public void SetEmissionRate(float particlesPerSecond)
        {
            emitter.ParticlesPerSecond = particlesPerSecond;
        }

        public void Reset()
        {
        }
    }

    public class ParticleEmitter : RenderPrimitive
    {
        struct ParticleVertex
        {
            public Vector4 position_time;
            public Short4 tileXY_ID;
            public NormalizedShort4 randomNumbers;

            public const int SizeInBytes = 32;

            public static readonly VertexElement[] VertexElements =
            {
                new VertexElement(0, 0, VertexElementFormat.Vector4, VertexElementMethod.Default, VertexElementUsage.Position, 0),
                new VertexElement(0, 16, VertexElementFormat.Short4, VertexElementMethod.Default, VertexElementUsage.Position, 1),
                new VertexElement(0, 24, VertexElementFormat.NormalizedShort4, VertexElementMethod.Default, VertexElementUsage.Position, 2)
            };
        }

        static int VERTICES_PER_PARTICLE = 4;
        static int PRIMITIVES_PER_PARTICLE = 2;
        static int INDICES_PER_PARTICLE = 6;

        public ParticleEmitterData EmitterData;
        int maxParticles;

        private float particlesPerSecond = 0;
        public float ParticlesPerSecond
        {
            set { particlesPerSecond = Math.Min(value, EmitterData.MaxParticlesPerSecond); }
            private get { return particlesPerSecond; }
        }

        public Color ColorTint { get; set; }

        float particlesToEmit = 0;

        Random rng = new Random();
        RenderProcess renderProcess;
        ParticleVertex[] vertices;

        VertexDeclaration vd;
        DynamicVertexBuffer vb;
        IndexBuffer ib;

        public WorldPosition WorldPosition { get;  set; }

        int firstActiveParticle;
        int firstNewParticle;
        int firstFreeParticle;
        int firstRetiredParticle;

        float timeParticlesLastEmitted;
        int drawCounter;

        public ParticleEmitter(RenderProcess renderProcess, ParticleEmitterData data)
        {
            ColorTint = Color.White;
            EmitterData = data;
            this.renderProcess = renderProcess;

            maxParticles = (int)(data.MaxParticlesPerSecond * data.ParticleDuration);

            vd = new VertexDeclaration(renderProcess.GraphicsDevice, ParticleVertex.VertexElements);
            InitVB(renderProcess.GraphicsDevice);
            InitIB(renderProcess.GraphicsDevice);
        }

        private void InitVB(GraphicsDevice device)
        {
            vb = new DynamicVertexBuffer(device, typeof(ParticleVertex), maxParticles * VERTICES_PER_PARTICLE, BufferUsage.WriteOnly);
            vertices = new ParticleVertex[maxParticles * VERTICES_PER_PARTICLE];
        }

        private void InitIB(GraphicsDevice device)
        {
            int numIndices = maxParticles * INDICES_PER_PARTICLE;
            ib = new IndexBuffer(device, sizeof(ushort) * numIndices, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
            ushort[] indices = new ushort[numIndices];

            ushort idx = 0;

            for (int i = 0; i < numIndices; i += INDICES_PER_PARTICLE)
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

        private void RetireActiveParticles(float currentTime)
        {
            float particleDuration = EmitterData.ParticleDuration;

            while (firstActiveParticle != firstNewParticle)
            {
                int firstVertexOfParticle = firstActiveParticle * VERTICES_PER_PARTICLE;
                float particleAge = currentTime - vertices[firstVertexOfParticle].position_time.W;

                if (particleAge < particleDuration)
                    break;

                vertices[firstVertexOfParticle].position_time.W = (float)drawCounter;

                firstActiveParticle = (firstActiveParticle + 1) % maxParticles;
            }
        }

        private void FreeRetiredParticles()
        {
            while (firstRetiredParticle != firstActiveParticle)
            {
                int firstVertexOfParticle = firstRetiredParticle * VERTICES_PER_PARTICLE;
                int age = drawCounter - (int)vertices[firstVertexOfParticle].position_time.W;

                if (age < 2)
                    break;

                firstRetiredParticle = (firstRetiredParticle + 1) % maxParticles;
            }
        }

        private int GetNumParticlesAvailableForEmission()
        {
            int nextFree = (firstFreeParticle + 1) % maxParticles;

            if (nextFree <= firstRetiredParticle)
                return firstRetiredParticle - nextFree;
            else
            {
                return (maxParticles - nextFree) + firstRetiredParticle;
            }
        }

        public void Update(double currentTime, ElapsedTime elapsedTime)
        {
            RetireActiveParticles((float)currentTime);
            FreeRetiredParticles();

            //Add a particle.
            float timeLastFrame = (float)currentTime - elapsedTime.ClockSeconds;
            float time = (float)currentTime;

            particlesToEmit += (elapsedTime.ClockSeconds * ParticlesPerSecond);

            int numParticlesAdded = 0;

            int numToBeEmitted = (int)particlesToEmit;
            int numCanBeEmitted = GetNumParticlesAvailableForEmission();
            int numToEmit = Math.Min(numToBeEmitted, numCanBeEmitted);
            
            float intervalPerParticle = (time - timeParticlesLastEmitted) / numToEmit;

            for(int i = 0; i < numToEmit; i++)
            {
                int nextFreeParticle = (firstFreeParticle + 1) % maxParticles;

                int newParticleVertexIndex = nextFreeParticle * VERTICES_PER_PARTICLE;

                Vector3 particleOffset = EmitterData.Offset;
                Matrix rotation = WorldPosition.XNAMatrix;
                rotation.Translation = Vector3.Zero;
                particleOffset = Vector3.Transform(particleOffset, Matrix.Invert(rotation));

                Vector3 particlePosition = WorldPosition.Location + particleOffset;

                float timeOfEmission = timeParticlesLastEmitted + (i * intervalPerParticle);

                Vector4 positionTime = new Vector4(WorldPosition.Location + particleOffset, timeOfEmission);
                positionTime.Z *= -1;

                float randomTextureOffset = (float)rng.Next(16);

                NormalizedShort4 randomNumbers = new NormalizedShort4((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble());

                for (int j = 0; j < VERTICES_PER_PARTICLE; j++)
                {
                    vertices[newParticleVertexIndex + j].position_time = positionTime;
                    vertices[newParticleVertexIndex + j].tileXY_ID = new Short4(WorldPosition.TileX, WorldPosition.TileZ, j, randomTextureOffset);
                    vertices[newParticleVertexIndex + j].randomNumbers = randomNumbers;
                }

                firstFreeParticle = nextFreeParticle;
                particlesToEmit--;
                numParticlesAdded++;
            }

            if (numParticlesAdded > 0)
                timeParticlesLastEmitted = time;

            particlesToEmit = particlesToEmit - (int)particlesToEmit;
        }


        private void AddNewParticlesToVertexBuffer()
        {
            int stride = ParticleVertex.SizeInBytes;

            if (firstNewParticle < firstFreeParticle)
            {
                int numParticlesToAdd = firstFreeParticle - firstNewParticle;

                vb.SetData( firstNewParticle * stride * VERTICES_PER_PARTICLE,
                            vertices,
                            firstNewParticle * VERTICES_PER_PARTICLE,
                            numParticlesToAdd * VERTICES_PER_PARTICLE,
                            stride, SetDataOptions.NoOverwrite);
            }
            else
            {
                int numParticlesToAddAtEnd = maxParticles - firstNewParticle;
                vb.SetData(firstNewParticle * stride * VERTICES_PER_PARTICLE,
                            vertices,
                            firstNewParticle * VERTICES_PER_PARTICLE,
                            numParticlesToAddAtEnd * VERTICES_PER_PARTICLE,
                            stride, SetDataOptions.NoOverwrite);

                if (firstFreeParticle > 0)
                {
                    vb.SetData(0, vertices, 0, firstFreeParticle * VERTICES_PER_PARTICLE, stride, SetDataOptions.NoOverwrite);
                }
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
            {
                AddNewParticlesToVertexBuffer();
            }

            if (HasParticlesToRender())
            {
                graphicsDevice.Indices = ib;
                graphicsDevice.VertexDeclaration = vd;
                graphicsDevice.Vertices[0].SetSource(vb, 0, ParticleVertex.SizeInBytes);

                if (firstActiveParticle < firstFreeParticle)
                {
                    int numParticles = firstFreeParticle - firstActiveParticle;
                    graphicsDevice.DrawIndexedPrimitives(   PrimitiveType.TriangleList, 
                                                            0,
                                                            0,
                                                            numParticles * VERTICES_PER_PARTICLE,
                                                            firstActiveParticle * INDICES_PER_PARTICLE,
                                                            numParticles * PRIMITIVES_PER_PARTICLE);//, 0, maxParticles * VERTICES_PER_PARTICLE, 0, maxParticles * PRIMITIVES_PER_PARTICLE);
                }
                else
                {
                    int numParticlesAtEnd = maxParticles - firstActiveParticle;
                    graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                                                            0, 0, numParticlesAtEnd * VERTICES_PER_PARTICLE, firstActiveParticle * INDICES_PER_PARTICLE, numParticlesAtEnd * PRIMITIVES_PER_PARTICLE);

                    if (firstFreeParticle > 0)
                    {
                        graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, firstFreeParticle * VERTICES_PER_PARTICLE, 0, firstFreeParticle * PRIMITIVES_PER_PARTICLE);
                    }
                }
            }

            drawCounter++;
        }
    }

}
