// COPYRIGHT 2010, 2011, 2012, 2013 by the Open Rails project.
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
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MSTS;
using ORTS.Processes;

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
            precipMaterial = viewer.MaterialManager.Load("Precip");
            // Instantiate classes
            precipMesh = new PrecipMesh( Viewer.RenderProcess);

            // Set default values and pass to PrecipMesh as applicable
            // TODO: Obtain from route files (wind params are future)
            // Sync with Sky.cs wind direction
            precipMesh.windDir = new Vector2(1, 0); // Westerly.
            windStrength = 2.0f;
            precipMesh.windStrength = windStrength;

            WeatherControl weather = new WeatherControl(Viewer);
            precipMesh.intensity = weather.pricipitationIntensity;
            precipMesh.Initialize(Viewer.Simulator);
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

			if (MultiPlayer.MPManager.IsClient())
			{
				//received message about weather change
				if (MultiPlayer.MPManager.Instance().weatherChanged && MultiPlayer.MPManager.Instance().newWeather >=0 &&
                    MultiPlayer.MPManager.Instance().newWeather != (int)Viewer.Simulator.Weather)
				{
					Viewer.Simulator.Weather = (WeatherType)MultiPlayer.MPManager.Instance().newWeather;
                    Viewer.World.WeatherControl.SetWeather(Viewer.Simulator.Weather);
					try
					{
                        if ( MultiPlayer.MPManager.Instance().newWeather >=0 )
						{
                            MultiPlayer.MPManager.Instance().weatherChanged = false;
                            MultiPlayer.MPManager.Instance().newWeather = -1;
                        }
					}
					catch { }
				}
                if (MultiPlayer.MPManager.Instance().weatherChanged && MultiPlayer.MPManager.Instance().precipIntensity >= 0)
                {
                    precipMesh.intensity = MultiPlayer.MPManager.Instance().precipIntensity;
                    try
                    {
                        if (MultiPlayer.MPManager.Instance().precipIntensity >= 0)
                        {
                            MultiPlayer.MPManager.Instance().weatherChanged = false;
                            MultiPlayer.MPManager.Instance().precipIntensity = -1;
                        }
                    }
                    catch { }
                }
				precipMesh.Initialize(Viewer.Simulator);
			}
            if (UserInput.IsPressed(UserCommands.DebugWeatherChange) && !MultiPlayer.MPManager.IsClient())
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
                Viewer.World.WeatherControl.SetWeather(Viewer.Simulator.Weather);
                precipMesh.Initialize(Viewer.Simulator);
				if (MultiPlayer.MPManager.IsServer())
				{
					MultiPlayer.MPManager.Notify((new MultiPlayer.MSGWeather((int)Viewer.Simulator.Weather, 
                        -1, -1, -1)).ToString());//server notify others the weather has changed
				}
            }
            if (UserInput.IsDown(UserCommands.DebugPrecipitationIncrease) && !MultiPlayer.MPManager.IsClient())
            {
                precipMesh.intensity = MathHelper.Clamp(precipMesh.intensity * 1.05f, PrecipMesh.MinPrecipIntensity, PrecipMesh.MaxPrecipIntensity);
                Viewer.World.WeatherControl.SetPricipitationVolume(precipMesh.intensity / PrecipMesh.MaxPrecipIntensity);
            }
            if (UserInput.IsDown(UserCommands.DebugPrecipitationDecrease) && !MultiPlayer.MPManager.IsClient())
            {
                precipMesh.intensity = MathHelper.Clamp(precipMesh.intensity / 1.05f, PrecipMesh.MinPrecipIntensity, PrecipMesh.MaxPrecipIntensity);
                Viewer.World.WeatherControl.SetPricipitationVolume(precipMesh.intensity / PrecipMesh.MaxPrecipIntensity);
            }
            if (MultiPlayer.MPManager.IsServer() && 
                (UserInput.IsReleased(UserCommands.DebugPrecipitationDecrease) || UserInput.IsReleased(UserCommands.DebugPrecipitationIncrease)))
                  MultiPlayer.MPManager.Notify((new MultiPlayer.MSGWeather(-1,
                        -1, -1, (int)precipMesh.intensity)).ToString());//server notifies others precipitation intensity has changed

////////////////////////////////////////////////////////////////////

            precipMesh.Update(Viewer.Simulator);

            frame.AddPrimitive(precipMaterial, precipMesh, RenderPrimitiveGroup.Precipitation, ref XNAPrecipWorldLocation);
        }

        /// <summary>
        /// Reset the particle array upon any event that inerrupts or alters the time clock.
        /// </summary>
        public void Reset()
        {
            precipMesh.Initialize(Viewer.Simulator);
        }

        [CallOnThread("Loader")]
        internal void Mark()
        {
            precipMaterial.Mark();
        }
    }
    #endregion

    #region PrecipMesh
    public class PrecipMesh: RenderPrimitive 
    {
		const int MaxParticleCount = 250000;
        public const float MinPrecipIntensity = 100;
        public const float MaxPrecipIntensity = 15000;

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

		public void Initialize(Simulator simulator)
		{
			var particleCount = (int)(intensity * height);
			Debug.Assert(particleCount < MaxParticleCount, "PrecipMesh MaxParticleCount exceeded.");
			ParticleStartIndex = 0;
			ParticleEndIndex = particleCount;
			for (var i = 0; i < particleCount; i++)
				InitializeParticle(ref Particles[i], simulator.GameTime - height * (particleCount - i) / particleCount);
			VertexBuffer.SetData(Particles, 0, MaxParticleCount, SetDataOptions.NoOverwrite);
            LastNewParticleTime = simulator.GameTime;
		}

        public void Update(Simulator simulator)
		{
            while (((ParticleEndIndex - ParticleStartIndex + MaxParticleCount) % MaxParticleCount != 1) && (simulator.GameTime >= Particles[ParticleStartIndex].time + height))
			{
				ParticleStartIndex++;
				ParticleStartIndex %= MaxParticleCount;
			}

            var newParticles = (int)Math.Min((simulator.GameTime - LastNewParticleTime) * intensity, (ParticleStartIndex - ParticleEndIndex + MaxParticleCount) % MaxParticleCount);
			if (newParticles > 0)
			{
				for (var i = 0; i < newParticles; i++)
				{
                    InitializeParticle(ref Particles[ParticleEndIndex], simulator.GameTime - (simulator.GameTime - LastNewParticleTime) * (newParticles - i) / newParticles);
					VertexBuffer.SetData(ParticleEndIndex * VertexPointSprite.SizeInBytes, Particles, ParticleEndIndex, 1, VertexPointSprite.SizeInBytes, SetDataOptions.NoOverwrite);
					ParticleEndIndex++;
					ParticleEndIndex %= MaxParticleCount;
				}
                LastNewParticleTime = simulator.GameTime;
			}
		}

		void InitializeParticle(ref VertexPointSprite particle, double time)
		{
			particle.position.X = (float)Random.NextDouble() * width - width / 2;
			particle.position.Y = width / 2;
			particle.position.Z = (float)Random.NextDouble() * width - width / 2;
			particle.pointSize = particleSize;
			particle.time = (float)time;
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
