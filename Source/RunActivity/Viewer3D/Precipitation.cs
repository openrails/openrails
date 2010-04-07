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
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
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
        public int weatherType;
        public double startTime;
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
            // Get data for current activity
            startTime = Viewer.Simulator.ClockTime;
            weatherType = (int)Viewer.Simulator.Weather;

            WeatherControl weather = new WeatherControl(Viewer);
            precipMesh.intensity = weather.intensity;
            precipMesh.InitPrecipParticles(0);
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

            if (UserInput.IsAltPressed(Keys.P))
            {
                switch (weatherType)
                {
                    case (int)WeatherType.Clear:
                        weatherType = (int)WeatherType.Rain;
                        break;
                    case (int)WeatherType.Rain:
                        weatherType = (int)WeatherType.Snow;
                        break;
                    case (int)WeatherType.Snow:
                        weatherType = (int)WeatherType.Clear;
                        break;
                    default:
                        weatherType = (int)WeatherType.Clear;
                        break;
                }
            }

////////////////////////////////////////////////////////////////////
            precipMesh.Reinitialize(Viewer.Simulator.ClockTime - startTime);

            frame.AddPrimitive(precipMaterial, precipMesh, ref XNAPrecipWorldLocation);
        }

        /// <summary>
        /// Reset the particle array upon any event that inerrupts or alters the time clock.
        /// </summary>
        public void Reset()
        {
            startTime = Viewer.Simulator.ClockTime;
            precipMesh.Reset( );
        }
    }
    #endregion

    #region PrecipMesh
    public class PrecipMesh: RenderPrimitive 
    {
        // Vertex declaration
        private VertexDeclaration pointSpriteVertexDeclaration;
        private VertexPointSprite[] drops;

        private float width; // Width (and depth) of precipitation box surrounding the viewer
        private float height; // Maximum particl age. In effect, this is the height of the precipitation box
        // Intensity factor: 1000 light; 3500 average; 7000 heavy
        public float intensity; // Particles per second
        private float particleSize;
        public Vector2 windDir;
        public float windStrength;
        int lastActiveParticle;
        Random random;

        /// <summary>
        /// Constructor.
        /// </summary>
        public PrecipMesh(RenderProcess renderProcess)
        {
            // Instantiate classes
            random = new Random();
            pointSpriteVertexDeclaration = new VertexDeclaration(renderProcess.GraphicsDevice, VertexPointSprite.VertexElements);

            // Set default values
            width = 150;
            height = 10;
            intensity = 6000;
            particleSize = 0.35f;
            lastActiveParticle = -1;
        }

        /// <summary>
        /// Reset the particle array upon any event that interrupts or alters the time clock. 
        /// </summary>
        public void Reset( )
        {
            lastActiveParticle = -1;
            Reinitialize(0);
        }

        /// <summary>
        /// Precipitation particle intialization. 
        /// </summary>
        public void InitPrecipParticles(double currentTime)
        {
            // Create the precipitation particles
            drops = new VertexPointSprite[(int)(height * intensity)];
            float timeStep = height / drops.Length;
            
            // Initialize particles
            for (int i = 0; i < drops.Length; i++)
            {
                drops[i] = new VertexPointSprite(new Vector3(
                        random.Next((int)width * 1000) / 1000f - width / 2f,
                        width / 2,
                        random.Next((int)width * 1000) / 1000f - width / 2f),
                    particleSize,
                    // Particles are uniformly diffused in time
                    (float)currentTime - (drops.Length - i)*timeStep,
                    windStrength * windDir);
            }
        }

        public void Reinitialize(double currentTime)
        {
            // If particles haven't been initialized...
            if (lastActiveParticle == -1)
            {
                InitPrecipParticles(currentTime);
                lastActiveParticle = 0;
            }
            else
            {
               // Reinitialize old particles
                while (currentTime - drops[lastActiveParticle].time >= height)
                {
                    drops[lastActiveParticle].position = new Vector3(
                        random.Next((int)width * 1000) / 1000f - width / 2f,
                        width / 2,
                        random.Next((int)width * 1000) / 1000f - width / 2f);
                    drops[lastActiveParticle].time = (float)currentTime;
                    lastActiveParticle++;
                    lastActiveParticle %= drops.Length;
                }
            }
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            // Place the vertex declaration on the graphics device
            graphicsDevice.VertexDeclaration = pointSpriteVertexDeclaration;

            graphicsDevice.DrawUserPrimitives(PrimitiveType.PointList, drops, 0, drops.Length);
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
