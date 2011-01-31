/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 
/// Principal Author:
///    Rick Grout
/// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MSTS;

namespace ORTS
{
    #region Light properties
    /// <summary>
    /// The Light class encapsulates the data for each Light object 
    /// in the Lights block of an ENG/WAG file. 
    /// </summary>
    public class Light
    {
        public int type;
        public int headlight;
        public int unit;
        public int penalty;
        public int control;
        public int service;
        public int timeofday;
        public int weather;
        public int coupling;
        public int cycle;
        public float fadein;
        public float fadeout;
        public List<LightState> StateList = new List<LightState>();

        public Light()
        {
        }
    }
    #endregion

    #region Lights
    /// <summary>
    /// A Lights object is created for any engine or wagon having a 
    /// Lights block in its ENG/WAG file. It contains a collection of
    /// Light objects.
    /// Called from within the MSTSWagon class.
    /// </summary>
    public class Lights
    {
        public List<Light> LightList = new List<Light>();

        public Lights(STFReader stf, TrainCar railcar)
        {
            ReadWagLights(stf);
            if (LightList.Count == 0)
				throw new InvalidDataException("lights with no lights");
        }

        #region ReadWagLights
        /// <summary>
        /// Reads the Lights block of an ENG/WAG file
        /// </summary>
        public bool ReadWagLights(STFReader stf)
        {
            Light light;
            int numStates;
            stf.MustMatch("(");
            stf.ReadInt(STFReader.UNITS.None, null);// ignore this because its not always correct
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("light", ()=>{
                    stf.MustMatch("(");
                    LightList.Add(light = new Light());
                    stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("type", ()=>{ light.type = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                        new STFReader.TokenProcessor("conditions", ()=>{ stf.MustMatch("("); stf.ParseBlock( new STFReader.TokenProcessor[] {
                            new STFReader.TokenProcessor("headlight", ()=>{ light.headlight = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                            new STFReader.TokenProcessor("unit", ()=>{ light.unit = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                            new STFReader.TokenProcessor("penalty", ()=>{ light.penalty = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                            new STFReader.TokenProcessor("control", ()=>{ light.control = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                            new STFReader.TokenProcessor("service", ()=>{ light.service = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                            new STFReader.TokenProcessor("timeofday", ()=>{ light.timeofday = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                            new STFReader.TokenProcessor("weather", ()=>{ light.weather = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                            new STFReader.TokenProcessor("coupling", ()=>{ light.coupling = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                        });}),
                        new STFReader.TokenProcessor("cycle", ()=>{ light.cycle = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                        new STFReader.TokenProcessor("fadein", ()=>{ light.fadein = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                        new STFReader.TokenProcessor("fadeout", ()=>{ light.fadeout = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                        new STFReader.TokenProcessor("states", ()=>{ stf.MustMatch("("); numStates = stf.ReadInt(STFReader.UNITS.None, null); stf.ParseBlock( new STFReader.TokenProcessor[] {
                            new STFReader.TokenProcessor("state", ()=>{
                                if(light.StateList.Count < numStates)
                                    light.StateList.Add(new LightState(stf));
                                else
                                    STFException.TraceWarning(stf, "Additional State ignored");
                            }),});
                            if(light.StateList.Count != numStates)
                                STFException.TraceWarning(stf, "Missing State block");
                        }),
                });}),
            });
            return true;
        }// ReadWagLights
        #endregion
    }// Lights
    #endregion

    #region LightState
	/// <summary>A LightState object encapsulates the data for each State in the States subblock.
    /// </summary>
    public class LightState
    {
        public float duration;
        public float transition;
        public float radius;
        public float angle;
        public uint color;
        public Vector3 position;
        public Vector3 azimuth;
        public Vector3 elevation;

        public LightState(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("duration", ()=>{ duration = stf.ReadFloatBlock(STFReader.UNITS.None, 0f); }),
                new STFReader.TokenProcessor("transition", ()=>{ transition = stf.ReadFloatBlock(STFReader.UNITS.None, 0f); }),
                new STFReader.TokenProcessor("radius", ()=>{ radius = stf.ReadFloatBlock(STFReader.UNITS.Distance, 0f); }),
                new STFReader.TokenProcessor("angle", ()=>{ angle = stf.ReadFloatBlock(STFReader.UNITS.None, 0f); }),
                new STFReader.TokenProcessor("lightcolour", ()=>{ stf.MustMatch("("); color = stf.ReadHex(0); stf.SkipRestOfBlock(); }),
                new STFReader.TokenProcessor("position", ()=>{ position = stf.ReadVector3Block(STFReader.UNITS.None, new Vector3()); }),
                new STFReader.TokenProcessor("azimuth", ()=>{ azimuth = stf.ReadVector3Block(STFReader.UNITS.None, new Vector3()); }),
                new STFReader.TokenProcessor("elevation", ()=>{ elevation = stf.ReadVector3Block(STFReader.UNITS.None, new Vector3()); }),
            });
        }
    }
	#endregion

    #region LightGlowDrawer
    public class LightGlowDrawer
    {
        Material lightMaterial;
        public TrainCar Railcar;
        Viewer3D Viewer;
        public Vector3 xnaLightconeDir;
        public Vector3 xnaLightconeLoc;

        // Classes reqiring instantiation
        public LightGlowMesh lightMesh;

        #region Class variables
        public WorldPosition worldPosition;
        public bool isLightOn;
        public bool isLightDim;
        public bool isFrontCar;
        public Vector3 lightconeLoc;
        public float lightconeFadein;
        public float lightconeFadeout;
        #endregion

        #region Constructor
        /// <summary>
        /// LightGlowDrawer constructor
        /// </summary>
        public LightGlowDrawer(Viewer3D viewer, TrainCar railcar, bool frontCar)
        {
            Railcar = railcar;
            Viewer = viewer;
            isFrontCar = frontCar;

            lightMaterial = Materials.Load(Viewer.RenderProcess, "LightGlowMaterial");

            // Instantiate LightGlowMesh class for this drawer
            lightMesh = new LightGlowMesh(Viewer.RenderProcess, railcar, frontCar);
            lightconeLoc = lightMesh.lightconeLoc;
            lightconeLoc.Z *= -1;
            lightconeFadein = lightMesh.lightconeFadein;
            lightconeFadeout = lightMesh.lightconeFadeout;
        }
        #endregion

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            int dTileX = Railcar.WorldPosition.TileX - Viewer.Camera.TileX;
            int dTileZ = Railcar.WorldPosition.TileZ - Viewer.Camera.TileZ;
            Matrix xnaDTileTranslation = Matrix.CreateTranslation(dTileX * 2048, 0, -dTileZ * 2048);  // object is offset from camera this many tiles
            xnaDTileTranslation = Railcar.WorldPosition.XNAMatrix * xnaDTileTranslation;

            Vector3 mstsLocation = new Vector3(xnaDTileTranslation.Translation.X, xnaDTileTranslation.Translation.Y, -xnaDTileTranslation.Translation.Z);

            float objectRadius = lightMesh.objectRadius;
            float viewingDistance = 1500; // Arbitrary.
            if (Viewer.Camera.InFOV(mstsLocation, objectRadius))
                if (Viewer.Camera.InRange(mstsLocation, viewingDistance + objectRadius))
                    frame.AddPrimitive(lightMaterial, lightMesh, RenderPrimitiveGroup.Lights, ref xnaDTileTranslation);

            // Set the headlight cone location and direction vectors
            if (lightMesh.hasHeadlight)
            {
                xnaLightconeLoc = Vector3.Transform(lightconeLoc, xnaDTileTranslation);
                xnaLightconeDir = xnaLightconeLoc - xnaDTileTranslation.Translation;
                xnaLightconeDir.Normalize();
                // Tilt the light cone downward at a constant angle
                xnaLightconeDir.Y = -0.5f;
            }
        }
    }
	#endregion

    #region LightGlowMesh
    public class LightGlowMesh : RenderPrimitive
    {
        // Vertex declaration
        public VertexDeclaration lightVertexDeclaration;
        private LightGlowVertex[] lights;

        // LightGlow variables
        public int objectRadius = 20;
        int numLights;
        int maxStates;
        public bool isFrontCar = false;
        public bool hasHeadlight = false;
        public Vector3 lightconeLoc;
        public float lightconeFadein;
        public float lightconeFadeout;

        // Basic light parameters from Lights block of eng/wag file.
        public int[] type;
        public int[] headlight;
        public int[] unit;
        public float[] fadein;
        public float[] fadeout;
        public float[,] duration;
        public float[,] transition;
        public float[,] radius;
        public Vector3[,] position;
        public Vector3[,] azimuth;
        public uint[,] color;

        /// <summary>
        /// Constructor.
        /// </summary>
        public LightGlowMesh(RenderProcess renderProcess, TrainCar car, bool frontCar)
        {
            isFrontCar = frontCar;
            int i = 0;
            int j;
            numLights = car.Lights.LightList.Count;
			maxStates = 1;
            // Create and fill arrays with the light variables
            type =          new int[numLights];
            headlight =     new int[numLights];
            unit =          new int[numLights];
            fadein =        new float[numLights];
            fadeout =       new float[numLights];
            duration =      new float[numLights, maxStates];
            transition =    new float[numLights, maxStates];
            radius =        new float[numLights, maxStates];
            position =      new Vector3[numLights, maxStates];
            azimuth =       new Vector3[numLights, maxStates];
            color =         new uint[numLights, maxStates];
            bool findFirstHeadlight = true;
            foreach (Light light in car.Lights.LightList)
            {
                if (light.type == 1 && light.unit == 2 && light.penalty <= 1
                    && findFirstHeadlight && isFrontCar) // Find the first non-penalty light cone on the player locomotive
                {
                    hasHeadlight = true;
                    lightconeLoc = light.StateList.ElementAt<LightState>(0).position;
                    lightconeFadein = light.fadein;
                    lightconeFadeout = light.fadeout;
                    findFirstHeadlight = false;
                }

                
                if ((light.StateList.Count > 0) && light.type == 0 && light.penalty <= 1 && ((isFrontCar && light.unit == 2) 
                    || !isFrontCar && light.unit == 3 || light.unit <= 1)) // Not a light cone, not penalty; unit: 2 = front, 3 = rear
                {
                    type[i] = light.type;
                    headlight[i] = light.headlight;
                    unit[i] = light.unit;
                    fadein[i] = light.fadein;
                    fadeout[i] = light.fadein;
                    j = 0;
                    foreach (LightState state in light.StateList)
                    {
                        if (j == maxStates) continue;
                        duration[i, j] = state.duration;
                        transition[i, j] = state.transition;
                        radius[i, j] = state.radius;
                        position[i, j] = state.position;
                        azimuth[i, j] = state.azimuth;
                        color[i, j] = state.color;
                        j++;
                    }
                    i++;
                }
                else
                    numLights--; // Don't include special versions (light cone, penalty) in count.
            }
            //if (numLights <= 0)
                //return; // Not the player train or not the first/last car of the player train

            // Instantiate classes
            lights = new LightGlowVertex[numLights * 6];
            lightVertexDeclaration = new VertexDeclaration(renderProcess.GraphicsDevice, LightGlowVertex.VertexElements);

            InitVertices();
        }

        /// <summary>
        /// LightGlow light array intialization. 
        /// </summary>
        private void InitVertices()
        {
            float colorA = 1.0f, colorR = 1.0f, colorG = 1.0f, colorB = 1.0f;
            float lightAzimuth, headlightStatus, locationInTrain, fadeIn, fadeOut;
            Vector3 normal;
            Vector3 lightPosition;
                        
            // Create the light glow vertex array.
            for (int i = 0; i < numLights * 6; i++)
            {
                // Parse the MSTS color variable
                uint u = color[i / 6, 0];
                colorA = (float)((u & 0xff000000) >> 24) / 255;
                colorR = (float)((u & 0x00ff0000) >> 16) / 255;
                colorG = (float)((u & 0x0000ff00) >> 8) / 255;
                colorB = (float) (u & 0x000000ff) / 255;
                // Convert "azimuth" to a normal
                lightAzimuth = MathHelper.ToRadians(360 + azimuth[i / 6, 0].X);
                normal = new Vector3((float)Math.Sin(lightAzimuth), 0.0f, -(float)Math.Cos(lightAzimuth));
                // Convert position to XNA
                lightPosition = new Vector3(position[i / 6, 0].X, position[i / 6, 0].Y, -position[i / 6, 0].Z);
                headlightStatus = headlight[i / 6];
                locationInTrain = unit[i / 6];
                fadeIn = fadein[i / 6];
                fadeOut = fadeout[i / 6];

                lights[i++] = new LightGlowVertex(lightPosition, normal,
                    new Vector3(colorR, colorG, colorB),
                    new Vector4(colorA, radius[i / 6, 0], 1, 1),
                    new Vector4(headlightStatus, locationInTrain, fadeIn, fadeOut));
                lights[i++] = new LightGlowVertex(lightPosition, normal,
                    new Vector3(colorR, colorG, colorB),
                    new Vector4(colorA, radius[i / 6, 0], 0, 0),
                    new Vector4(headlightStatus, locationInTrain, fadeIn, fadeOut));
                lights[i++] = new LightGlowVertex(lightPosition, normal,
                    new Vector3(colorR, colorG, colorB),
                    new Vector4(colorA, radius[i / 6, 0], 1, 0),
                    new Vector4(headlightStatus, locationInTrain, fadeIn, fadeOut));

                lights[i++] = new LightGlowVertex(lightPosition, normal,
                    new Vector3(colorR, colorG, colorB),
                    new Vector4(colorA, radius[i / 6, 0], 1, 1),
                    new Vector4(headlightStatus, locationInTrain, fadeIn, fadeOut));
                lights[i++] = new LightGlowVertex(lightPosition, normal,
                    new Vector3(colorR, colorG, colorB),
                    new Vector4(colorA, radius[i / 6, 0], 0, 1),
                    new Vector4(headlightStatus, locationInTrain, fadeIn, fadeOut));
                lights[i] = new LightGlowVertex(lightPosition, normal,
                    new Vector3(colorR, colorG, colorB),
                    new Vector4(colorA, radius[i / 6, 0], 0, 0),
                    new Vector4(headlightStatus, locationInTrain, fadeIn, fadeOut));
            }
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            if (lights.Length == 0)
                return;
            // Place the vertex declaration on the graphics device
            graphicsDevice.VertexDeclaration = lightVertexDeclaration;
            graphicsDevice.DrawUserPrimitives<LightGlowVertex>(PrimitiveType.TriangleList, lights, 0, lights.Length / 3);
        }
    }
    #endregion

    #region LightGlowVertex definition
    /// <summary>
    /// Custom light glow vertex format.
    /// </summary>
    struct LightGlowVertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector3 rgbcolor;
        public Vector4 alphascaletex;
        public Vector4 flags;

        /// <summary>
        /// Light glow vertex constructor.
        /// </summary>
        /// <param name="position">quad position</param>
        /// <param name="normal">quad normals</param>
        /// <param name="rgbcolor">rgb color</param>
        /// <param name="alphascaletex">color alpha, quad scale, texture coords</param>
        /// <param name="flags">headlight, unit, fadein, fadeout</param>
        /// TODO: Expand as needed for increased functionality
        public LightGlowVertex(Vector3 position, Vector3 normal, Vector3 rgbcolor, Vector4 alphascaletex, Vector4 flags)
        {
            this.position = position;
            this.normal = normal;
            this.rgbcolor = rgbcolor;
            this.alphascaletex = alphascaletex;
            this.flags = flags;
        }

        // Vertex elements definition
        public static readonly VertexElement[] VertexElements = 
            {
                new VertexElement(0, 0, 
                    VertexElementFormat.Vector3, 
                    VertexElementMethod.Default, 
                    VertexElementUsage.Position, 0),
                new VertexElement(0, sizeof(float) * (3), 
                    VertexElementFormat.Vector3, 
                    VertexElementMethod.Default, 
                    VertexElementUsage.Normal, 0),
                new VertexElement(0, sizeof(float) * (3 + 3), 
                    VertexElementFormat.Vector3, 
                    VertexElementMethod.Default, 
                    VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(0, sizeof(float) * (3 + 3 + 3), 
                    VertexElementFormat.Vector4, 
                    VertexElementMethod.Default, 
                    VertexElementUsage.Position, 1),
                new VertexElement(0, sizeof(float) * (3 + 3 + 3 + 4), 
                    VertexElementFormat.Vector4, 
                    VertexElementMethod.Default, 
                    VertexElementUsage.Color, 0)
           };

        // Size of one vertex in bytes
        public static int SizeInBytes = sizeof(float) * (3 + 3 + 3 + 4 + 4);
    }
    #endregion

}

