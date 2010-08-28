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
        public List<LightState> StateList;

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

        public Light light;
        LightState lightState;

        public Lights(STFReader f, TrainCar railcar)
        {
            ReadWagLights(f);
            if (LightList.Count == 0)
				throw new InvalidDataException("lights with no lights");
        }

        #region ReadWagLights
        /// <summary>
        /// Reads the Lights block of an ENG/WAG file
        /// </summary>
        public bool ReadWagLights(STFReader f)
        {
            int numStates = 0;

            try
            {
                string token = f.ReadTokenNoComment();
                while (token != "") // EOF
                {
                    if (token == ")") break; // throw ( new STFError( f, "Unexpected )" ) );  we should really throw an exception
                    // but MSTS just ignores the rest of the file, and we will also
                    else
                    {
                        int numLights = f.ReadInt();// ignore this because its not always correct
                        for (; ; )
                        {
                            token = f.ReadTokenNoComment();
                            if (token == ")") break;
                            if (token == "") throw (new STFException(f, "Missing )"));
                            if (0 != String.Compare(token, "Light", true))// Weed out extraneous comments etc.
                            {
                                f.SkipBlock();
                                token = f.ReadTokenNoComment();
                            }
                            if (0 == String.Compare(token, "Light", true))
                            {
                                light = new Light();
                                LightList.Add(light);
                                f.VerifyStartOfBlock();
                                token = f.ReadTokenNoComment();
                                while (token != ")")
                                {
                                    if (token == "") throw (new STFException(f, "Missing )"));
                                    else if (0 == String.Compare(token, "comment", true))
                                    {
                                        f.SkipBlock(); // Ignore the comment
                                    }
                                    else if (0 == String.Compare(token, "Type", true))
                                    {
                                        f.VerifyStartOfBlock();
                                        light.type = f.ReadInt();
                                        f.VerifyEndOfBlock();
                                    }
                                    else if (0 == String.Compare(token, "Conditions", true))
                                    {
                                        f.VerifyStartOfBlock();
                                        token = f.ReadTokenNoComment();
                                        while (token != ")")
                                        {
                                            if (0 == String.Compare(token, "Headlight", true))
                                            {
                                                f.VerifyStartOfBlock();
                                                light.headlight = f.ReadInt();
                                                f.VerifyEndOfBlock();
                                            }
                                            else if (0 == String.Compare(token, "Unit", true))
                                            {
                                                f.VerifyStartOfBlock();
                                                light.unit = f.ReadInt();
                                                f.VerifyEndOfBlock();
                                            }
                                            else if (0 == String.Compare(token, "Penalty", true))
                                            {
                                                f.VerifyStartOfBlock();
                                                light.penalty = f.ReadInt();
                                                f.VerifyEndOfBlock();
                                            }
                                            else if (0 == String.Compare(token, "Control", true))
                                            {
                                                f.VerifyStartOfBlock();
                                                light.control = f.ReadInt();
                                                f.VerifyEndOfBlock();
                                            }
                                            else if (0 == String.Compare(token, "Service", true))
                                            {
                                                f.VerifyStartOfBlock();
                                                light.service = f.ReadInt();
                                                f.VerifyEndOfBlock();
                                            }
                                            else if (0 == String.Compare(token, "TimeOfDay", true))
                                            {
                                                f.VerifyStartOfBlock();
                                                light.timeofday = f.ReadInt();
                                                f.VerifyEndOfBlock();
                                            }
                                            else if (0 == String.Compare(token, "Weather", true))
                                            {
                                                f.VerifyStartOfBlock();
                                                light.weather = f.ReadInt();
                                                f.VerifyEndOfBlock();
                                            }
                                            else if (0 == String.Compare(token, "Coupling", true))
                                            {
                                                f.VerifyStartOfBlock();
                                                light.coupling = f.ReadInt();
                                                f.VerifyEndOfBlock();
                                            }
                                            token = f.ReadTokenNoComment();
                                        }
                                    }// else if (0 == String.Compare(token, "Conditions", true))
                                    else if (0 == String.Compare(token, "Cycle", true))
                                    {
                                        f.VerifyStartOfBlock();
                                        light.cycle = f.ReadInt();
                                        f.VerifyEndOfBlock();
                                    }
                                    else if (0 == String.Compare(token, "FadeIn", true))
                                    {
                                        f.VerifyStartOfBlock();
                                        light.fadein = f.ReadFloat();
                                        f.VerifyEndOfBlock();
                                    }
                                    else if (0 == String.Compare(token, "FadeOut", true))
                                    {
                                        f.VerifyStartOfBlock();
                                        light.fadeout = f.ReadFloat();
                                        f.VerifyEndOfBlock();
                                    }
                                    else if (0 == String.Compare(token, "States", true))
                                    {
                                        f.VerifyStartOfBlock();
                                        numStates = f.ReadInt();
                                        for (int j = 0; j < numStates; j++)
                                        {
                                            light.StateList = new List<LightState>();
                                            lightState = new LightState();
                                            lightState.ReadLightState(f);
                                            light.StateList.Add(lightState);
                                        }
                                    }// else if (0 == String.Compare(token, "States", true))
                                    token = f.ReadTokenNoComment();
                                }// while (token != ")")
                                token = f.ReadTokenNoComment();
                            }// if (0 == String.Compare(token, "Light", true))
                        }// for (int i = 0; i < numLights; i++)
                    }// else file is readable
                }// while !EOF

            }
            catch (Exception error)
            {
				Trace.WriteLine(error);
                return false;
            }
            return true;
        }// ReadWagLights
        #endregion
    }// Lights
    #endregion

    #region LightState
	/// <summary>
    /// A LightState object encapsulates the data for each State 
    /// in the States subblock.
    /// </summary>
    public class LightState
    {
        public float duration;
        public float transition;
        public float radius;
        public float angle;
        public Vector3 position;
        public Vector3 azimuth;
        public Vector3 elevation;
        public int color;

        public LightState()
        {
        }

        /// <summary>
        /// Reads the State data from the current States subblock.
        /// </summary>
        public void ReadLightState(STFReader f)
        {
            string token = f.ReadToken();
            if (0 == String.Compare(token, "State", true))
            {
                f.VerifyStartOfBlock();
                token = f.ReadToken();
                while (token != ")")
                {
                    if (token == "") throw (new STFException(f, "Missing )"));
                    else if (0 == String.Compare(token, "Duration", true))
                    {
                        f.VerifyStartOfBlock();
                        duration = f.ReadFloat();
                        f.VerifyEndOfBlock();
                    }
                    else if (0 == String.Compare(token, "Transition", true))
                    {
                        f.VerifyStartOfBlock();
                        transition = f.ReadFloat();
                        f.VerifyEndOfBlock();
                    }
                    else if (0 == String.Compare(token, "Radius", true))
                    {
                        f.VerifyStartOfBlock();
                        radius = f.ReadFloat();
                        f.VerifyEndOfBlock();
                    }
                    else if (0 == String.Compare(token, "Angle", true))
                    {
                        f.VerifyStartOfBlock();
                        angle = f.ReadFloat();
                        f.VerifyEndOfBlock();
                    }
                    else if (0 == String.Compare(token, "Position", true))
                    {
                        f.VerifyStartOfBlock();
                        position.X = f.ReadFloat();
                        position.Y = f.ReadFloat();
                        position.Z = f.ReadFloat();
                        f.VerifyEndOfBlock();
                    }
                    else if (0 == String.Compare(token, "Azimuth", true))
                    {
                        f.VerifyStartOfBlock();
                        azimuth.X = f.ReadFloat();
                        azimuth.Y = f.ReadFloat();
                        azimuth.Z = f.ReadFloat();
                        f.VerifyEndOfBlock();
                    }
                    else if (0 == String.Compare(token, "Elevation", true))
                    {
                        f.VerifyStartOfBlock();
                        elevation.X = f.ReadFloat();
                        elevation.Y = f.ReadFloat();
                        elevation.Z = f.ReadFloat();
                        f.VerifyEndOfBlock();
                    }
                    else if (0 == String.Compare(token, "LightColour", true))
                    {
                        f.VerifyStartOfBlock();
                        color = f.ReadHex();
                        f.VerifyEndOfBlock();
                    }
                    token = f.ReadToken();
                }// while (token != ")")
            }// if (0 == String.Compare(token, "State", true))
        }// ReadLightStates
    }// LightStates
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
        int numStates;
        public bool isFrontCar = false;
        public bool hasHeadlight = false;
        bool isFirstHeadlight = true;
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
        public int[,] color;

        /// <summary>
        /// Constructor.
        /// </summary>
        public LightGlowMesh(RenderProcess renderProcess, TrainCar car, bool frontCar)
        {
            isFrontCar = frontCar;
            int i = 0;
            int j;
            numLights = car.Lights.LightList.Count;
            numStates = car.Lights.light.StateList.Count;
            // Create and fill arrays with the light variables
            type =          new int[numLights];
            headlight =     new int[numLights];
            unit =          new int[numLights];
            fadein =        new float[numLights];
            fadeout =       new float[numLights];
            duration =      new float[numLights, numStates];
            transition =    new float[numLights, numStates];
            radius =        new float[numLights, numStates];
            position =      new Vector3[numLights, numStates];
            azimuth =       new Vector3[numLights, numStates];
            color =         new int[numLights, numStates];
            foreach (Light light in car.Lights.LightList)
            {
                if (light.type == 1 && light.unit == 2 && light.penalty <= 1 
                    && isFirstHeadlight && isFrontCar) // Find the first non-penalty light cone on the player locomotive
                {
                    hasHeadlight = true;
                    lightconeLoc = light.StateList.ElementAt<LightState>(0).position;
                    lightconeFadein = light.fadein;
                    lightconeFadeout = light.fadeout;
                    isFirstHeadlight = false;
                }

                
                if (light.type == 0 && light.penalty <= 1 && ((isFrontCar && light.unit == 2) 
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
                uint u = (uint)unchecked(color[i / 6, 0]);
                colorA = (float)((u & 0xff000000) >> 24) / 255;
                colorR = (float)((u & 0x00ff0000) >> 16) / 255;
                colorG = (float)((u & 0x0000ff00) >> 8) / 255;
                colorB = (float) (u & 0x000000ff) / 255;
                // Convert "azimuth" to a normal
                lightAzimuth = MathHelper.ToRadians(360 + azimuth[i / 6, 0].X);
                normal = new Vector3((float)Math.Sin(lightAzimuth), 0.0f, -(float)Math.Cos(lightAzimuth));
                // Convert position to XNA
                // Note: MSTS cars are offset 0.275 m above tracks
                lightPosition = new Vector3(position[i / 6, 0].X, position[i / 6, 0].Y + 0.275f, -position[i / 6, 0].Z);
                //radius[i / 6, 0] *= 1.0f; // Adjust scale if necessary
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

