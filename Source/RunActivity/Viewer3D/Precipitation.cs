/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 
/// Principal Author:
///    Rick Grout
/// 
///     
using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MSTS;

namespace ORTS
{
    #region PrecipDrawer
    /// <summary>
    /// Precipitation render primitive
    /// Adapted from code by Jan Vytlačil.
    /// </summary>
    public class PrecipDrawer
    {
        Viewer3D Viewer;
        Material precipMaterial;

        // Classes reqiring instantiation
        public PrecipMesh precipMesh;

        #region Class variables
        // Precipitation parameters
        // Some of these are candidates for external access in the future
        private Vector2 windDir;
        private float windStrength;
        public float intensity; // Particles per second
        #endregion

        #region Constructor
        /// <summary>
        /// PrecipDrawer constructor
        /// </summary>
        public PrecipDrawer(Viewer3D viewer)
        {
            Viewer = viewer;
            precipMaterial = Materials.Load(Viewer.RenderProcess, "PrecipMaterial");
            // Instantiate classes
            precipMesh = new PrecipMesh( Viewer.RenderProcess);

            // Set default values and pass to PrecipMesh as applicable
            // TODO: Obtain from route files (wind params are future)
            // Sync with Sky.cs wind direction
            precipMesh.windDir = windDir = new Vector2(1, 0); // Westerly.
            windStrength = 2.0f;
            precipMesh.windStrength = windStrength;

            WeatherControl weather = new WeatherControl(Viewer);
            precipMesh.intensity = weather.intensity;
			precipMesh.Initialize(Viewer.Simulator.ClockTime);
        }
        #endregion

        /// <summary>
        /// Used to update information affecting the precipitation particles
        /// </summary>
        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            // Set up the positioning matrices
            Vector3 ViewerXNAPosition = new Vector3(Viewer.Camera.Location.X, Viewer.Camera.Location.Y, -Viewer.Camera.Location.Z);
            Matrix XNAPrecipWorldLocation = Matrix.CreateTranslation(ViewerXNAPosition);

////////////////////// T E M P O R A R Y ///////////////////////////

            // The following keyboard commands are used for viewing sky and weather effects in "demo" mode
            // Use Alt+P to toggle precipitation through Clear, Rain and Snow states

            if (UserInput.IsPressed(UserCommands.GameDebugWeatherChange))
            {
                switch (Viewer.Simulator.Weather)
                {
                    case WeatherType.Clear:
                        Viewer.Simulator.Weather = WeatherType.Rain;
                        break;
                    case WeatherType.Rain:
                        Viewer.Simulator.Weather = WeatherType.Snow;
                        break;
                    case WeatherType.Snow:
                        Viewer.Simulator.Weather = WeatherType.Clear;
                        break;
                    default:
                        Viewer.Simulator.Weather = WeatherType.Clear;
                        break;
                }
				precipMesh.Initialize(Viewer.Simulator.ClockTime);
            }

////////////////////////////////////////////////////////////////////

            precipMesh.Update(Viewer.Simulator.ClockTime);

            frame.AddPrimitive(precipMaterial, precipMesh, RenderPrimitiveGroup.Precipitation, ref XNAPrecipWorldLocation);
        }

        /// <summary>
        /// Reset the particle array upon any event that inerrupts or alters the time clock.
        /// </summary>
        public void Reset()
        {
			precipMesh.Initialize(Viewer.Simulator.ClockTime);
        }
    }
    #endregion

    #region PrecipMesh
    public class PrecipMesh: RenderPrimitive 
    {
		const int MaxParticleCount = 100000;

        Random Random;
		VertexDeclaration VertexDeclaration;
		DynamicVertexBuffer VertexBuffer;
		VertexPointSprite[] Particles;
		int ParticleStartIndex; // INCLUSIVE
		int ParticleEndIndex; // EXCLUSIVE
		double LastNewParticleTime;

		private float width; // Width (and depth) of precipitation box surrounding the viewer
		private float height; // Maximum particl age. In effect, this is the height of the precipitation box
		// Intensity factor: 1000 light; 3500 average; 7000 heavy
		public float intensity; // Particles per second
		private float particleSize;
		public Vector2 windDir;
		public float windStrength;

        /// <summary>
        /// Constructor.
        /// </summary>
        public PrecipMesh(RenderProcess renderProcess)
        {
            Random = new Random();
			VertexDeclaration = new VertexDeclaration(renderProcess.GraphicsDevice, VertexPointSprite.VertexElements);
			VertexBuffer = new DynamicVertexBuffer(renderProcess.GraphicsDevice, MaxParticleCount * VertexPointSprite.SizeInBytes, BufferUsage.Points | BufferUsage.WriteOnly);
			Particles = new VertexPointSprite[MaxParticleCount];

			// Set default values
			width = 150;
			height = 10;
			intensity = 6000;
			particleSize = 0.35f;
        }

		public void Initialize(double currentTime)
		{
			var particleCount = (int)(intensity * height);
			Debug.Assert(particleCount < MaxParticleCount, "PrecipMesh MaxParticleCount exceeded.");
			ParticleStartIndex = 0;
			ParticleEndIndex = particleCount;
			for (var i = 0; i < particleCount; i++)
				InitializeParticle(ref Particles[i], currentTime - height * (particleCount - i) / particleCount);
			VertexBuffer.SetData(Particles, 0, MaxParticleCount, SetDataOptions.NoOverwrite);
			LastNewParticleTime = currentTime;
		}

		public void Update(double currentTime)
		{
			while (((ParticleEndIndex - ParticleStartIndex + MaxParticleCount) % MaxParticleCount != 1) && (currentTime >= Particles[ParticleStartIndex].time + height))
			{
				ParticleStartIndex++;
				ParticleStartIndex %= MaxParticleCount;
			}

			var newParticles = (int)Math.Min((currentTime - LastNewParticleTime) * intensity, (ParticleStartIndex - ParticleEndIndex + MaxParticleCount) % MaxParticleCount);
			if (newParticles > 0)
			{
				for (var i = 0; i < newParticles; i++)
				{
					InitializeParticle(ref Particles[ParticleEndIndex], currentTime - (currentTime - LastNewParticleTime) * (newParticles - i) / newParticles);
					VertexBuffer.SetData(ParticleEndIndex * VertexPointSprite.SizeInBytes, Particles, ParticleEndIndex, 1, VertexPointSprite.SizeInBytes, SetDataOptions.NoOverwrite);
					ParticleEndIndex++;
					ParticleEndIndex %= MaxParticleCount;
				}
				LastNewParticleTime = currentTime;
			}
		}

		void InitializeParticle(ref VertexPointSprite particle, double currentTime)
		{
			particle.position.X = (float)Random.NextDouble() * width - width / 2;
			particle.position.Y = width / 2;
			particle.position.Z = (float)Random.NextDouble() * width - width / 2;
			particle.pointSize = particleSize;
			particle.time = (float)currentTime;
			particle.wind = windStrength * windDir;
		}

        public override void Draw(GraphicsDevice graphicsDevice)
        {
			graphicsDevice.VertexDeclaration = VertexDeclaration;
			graphicsDevice.Vertices[0].SetSource(VertexBuffer, 0, VertexPointSprite.SizeInBytes);
			if (ParticleStartIndex > ParticleEndIndex)
			{
				graphicsDevice.DrawPrimitives(PrimitiveType.PointList, ParticleStartIndex, MaxParticleCount - ParticleStartIndex);
				if (ParticleEndIndex > 0)
					graphicsDevice.DrawPrimitives(PrimitiveType.PointList, 0, ParticleEndIndex);
			}
			else if (ParticleStartIndex < ParticleEndIndex)
			{
				graphicsDevice.DrawPrimitives(PrimitiveType.PointList, ParticleStartIndex, ParticleEndIndex - ParticleStartIndex);
			}
        }

        #region VertexPointSprite definition
        /// <summary>
        /// Custom precipitation sprite vertex format.
        /// </summary>
        private struct VertexPointSprite
        {
            public Vector3 position;
            public float pointSize;
            public float time;
            public Vector2 wind;

            /// <summary>
            /// Precipitaiton vertex constructor.
            /// </summary>
            /// <param name="position">particle position</param>
            /// <param name="pointSize">particle size</param>
            /// <param name="time">time of particle initialization</param>
            /// <param name="wind">wind direction</param>
            //public VertexPointSprite(Vector3 position, float pointSize, float time, Vector3 random, Vector2 wind)
            public VertexPointSprite(Vector3 position, float pointSize, float time, Vector2 wind)
            {
                this.position = position;
                this.pointSize = pointSize;
                this.time = time;
                this.wind = wind;
            }

            // Vertex elements definition
            public static readonly VertexElement[] VertexElements = 
            {
                new VertexElement(0, 0, 
                    VertexElementFormat.Vector3, 
                    VertexElementMethod.Default, 
                    VertexElementUsage.Position, 0),
                new VertexElement(0, sizeof(float) * 3, 
                    VertexElementFormat.Single, 
                    VertexElementMethod.Default, 
                    VertexElementUsage.PointSize, 0),
                new VertexElement(0, sizeof(float) * (3 + 1), 
                    VertexElementFormat.Single, 
                    VertexElementMethod.Default, 
                    VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(0, sizeof(float) * (3 + 1 + 1), 
                    VertexElementFormat.Vector2, 
                    VertexElementMethod.Default, 
                    VertexElementUsage.TextureCoordinate, 1),
           };

            // Size of one vertex in bytes
            public static int SizeInBytes = sizeof(float) * (3 + 1 + 1 + 2);
        }
        #endregion
    }
    #endregion
}
