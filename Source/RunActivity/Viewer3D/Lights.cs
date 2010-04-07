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
using System.Collections;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
        int numLights = 0;

        public Lights(STFReader f, TrainCar railcar)
        {
            ReadWagLights(f);
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
                string token = f.ReadToken();
                while (token != "") // EOF
                {
                    if (token == ")") break; // throw ( new STFError( f, "Unexpected )" ) );  we should really throw an exception
                    // but MSTS just ignores the rest of the file, and will also
                    else
                    {
                        numLights = f.ReadInt();
                        for (int i = 0; i < numLights; i++)
                        {
                            token = f.ReadToken();
                            if (0 == String.Compare(token, "Light", true))
                            {
                                light = new Light();
                                f.MustMatch("(");
                                token = f.ReadToken();
                                while (token != ")")
                                {
                                    if (token == "") throw (new STFError(f, "Missing )"));
                                    else if (0 == String.Compare(token, "comment", true))
                                    {
                                        f.ReadDelimitedItem(); // Ignore the comment
                                    }
                                    else if (0 == String.Compare(token, "Type", true))
                                    {
                                        f.MustMatch("(");
                                        light.type = f.ReadInt();
                                        if (f.PeekPastWhitespace() == '#') // comment
                                            do
                                            {
                                                token = f.ReadToken(); // Ignore the comment
                                            } while (token != ")");
                                        else
                                            f.MustMatch(")");
                                    }
                                    else if (0 == String.Compare(token, "Conditions", true))
                                    {
                                        f.MustMatch("(");
                                        token = f.ReadToken();
                                        while (token != ")")
                                        {
                                            if (0 == String.Compare(token, "Headlight", true))
                                            {
                                                f.MustMatch("(");
                                                light.headlight = f.ReadInt();
                                                f.MustMatch(")");
                                            }
                                            else if (0 == String.Compare(token, "Unit", true))
                                            {
                                                f.MustMatch("(");
                                                light.unit = f.ReadInt();
                                                f.MustMatch(")");
                                            }
                                            else if (0 == String.Compare(token, "Penalty", true))
                                            {
                                                f.MustMatch("(");
                                                light.penalty = f.ReadInt();
                                                f.MustMatch(")");
                                            }
                                            else if (0 == String.Compare(token, "Control", true))
                                            {
                                                f.MustMatch("(");
                                                light.control = f.ReadInt();
                                                f.MustMatch(")");
                                            }
                                            else if (0 == String.Compare(token, "Service", true))
                                            {
                                                f.MustMatch("(");
                                                light.service = f.ReadInt();
                                                f.MustMatch(")");
                                            }
                                            else if (0 == String.Compare(token, "TimeOfDay", true))
                                            {
                                                f.MustMatch("(");
                                                light.timeofday = f.ReadInt();
                                                f.MustMatch(")");
                                            }
                                            else if (0 == String.Compare(token, "Weather", true))
                                            {
                                                f.MustMatch("(");
                                                light.weather = f.ReadInt();
                                                f.MustMatch(")");
                                            }
                                            else if (0 == String.Compare(token, "Coupling", true))
                                            {
                                                f.MustMatch("(");
                                                light.coupling = f.ReadInt();
                                                f.MustMatch(")");
                                            }
                                            token = f.ReadToken();
                                        }
                                    }// else if (0 == String.Compare(token, "Conditions", true))
                                    else if (0 == String.Compare(token, "Cycle", true))
                                    {
                                        f.MustMatch("(");
                                        light.cycle = f.ReadInt();
                                        f.MustMatch(")");
                                    }
                                    else if (0 == String.Compare(token, "FadeIn", true))
                                    {
                                        f.MustMatch("(");
                                        light.fadein = f.ReadFloat();
                                        f.MustMatch(")");
                                    }
                                    else if (0 == String.Compare(token, "FadeOut", true))
                                    {
                                        f.MustMatch("(");
                                        light.fadeout = f.ReadFloat();
                                        f.MustMatch(")");
                                    }
                                    else if (0 == String.Compare(token, "States", true))
                                    {
                                        f.MustMatch("(");
                                        numStates = f.ReadInt();
                                        for (int j = 0; j < numStates; j++)
                                        {
                                            light.StateList = new List<LightState>();
                                            lightState = new LightState();
                                            lightState.ReadLightState(f);
                                            light.StateList.Add(lightState);
                                        }
                                    }// else if (0 == String.Compare(token, "States", true))
                                    token = f.ReadToken();
                                }// while (token != ")")
                                token = f.ReadToken();
                            }// if (0 == String.Compare(token, "Light", true))
                            LightList.Add(light);
                        }// for (int i = 0; i < numLights; i++)
                    }// else file is readable
                }// while !EOF

            }
            catch
            {
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
                f.MustMatch("(");
                token = f.ReadToken();
                while (token != ")")
                {
                    if (token == "") throw (new STFError(f, "Missing )"));
                    else if (0 == String.Compare(token, "Duration", true))
                    {
                        f.MustMatch("(");
                        duration = f.ReadFloat();
                        f.MustMatch(")");
                    }
                    else if (0 == String.Compare(token, "Transition", true))
                    {
                        f.MustMatch("(");
                        transition = f.ReadFloat();
                        f.MustMatch(")");
                    }
                    else if (0 == String.Compare(token, "Radius", true))
                    {
                        f.MustMatch("(");
                        radius = f.ReadFloat();
                        f.MustMatch(")");
                    }
                    else if (0 == String.Compare(token, "Angle", true))
                    {
                        f.MustMatch("(");
                        angle = f.ReadFloat();
                        f.MustMatch(")");
                    }
                    else if (0 == String.Compare(token, "Position", true))
                    {
                        f.MustMatch("(");
                        position.X = f.ReadFloat();
                        position.Y = f.ReadFloat();
                        position.Z = f.ReadFloat();
                        f.MustMatch(")");
                    }
                    else if (0 == String.Compare(token, "Azimuth", true))
                    {
                        f.MustMatch("(");
                        azimuth.X = f.ReadFloat();
                        azimuth.Y = f.ReadFloat();
                        azimuth.Z = f.ReadFloat();
                        f.MustMatch(")");
                    }
                    else if (0 == String.Compare(token, "Elevation", true))
                    {
                        f.MustMatch("(");
                        elevation.X = f.ReadFloat();
                        elevation.Y = f.ReadFloat();
                        elevation.Z = f.ReadFloat();
                        f.MustMatch(")");
                    }
                    else if (0 == String.Compare(token, "LightColour", true))
                    {
                        f.MustMatch("(");
                        color = f.ReadHex();
                        f.MustMatch(")");
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
        #endregion

        #region Constructor
        /// <summary>
        /// LightGlowDrawer constructor
        /// </summary>
        public LightGlowDrawer(Viewer3D viewer, TrainCar railcar)
        {
            Railcar = railcar;
            Viewer = viewer;

            lightMaterial = Materials.Load(Viewer.RenderProcess, "LightGlowMaterial");

            // Instantiate classes
            lightMesh = new LightGlowMesh(Viewer.RenderProcess, railcar);
        }
        #endregion

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            /*
            int dTileX = Railcar.WorldPosition.TileX - Viewer.Camera.TileX;
            int dTileZ = Railcar.WorldPosition.TileZ - Viewer.Camera.TileZ;
            Matrix xnaDTileTranslation = Matrix.CreateTranslation(dTileX * 2048, 0, -dTileZ * 2048);  // object is offset from camera this many tiles
            xnaDTileTranslation = Railcar.WorldPosition.XNAMatrix * xnaDTileTranslation;

            Vector3 mstsLocation = new Vector3(xnaDTileTranslation.Translation.X, xnaDTileTranslation.Translation.Y, -xnaDTileTranslation.Translation.Z);

            float objectRadius = lightMesh.objectRadius;
            float viewingDistance = 1500; // Arbitrary.
            if (Viewer.Camera.InFOV(mstsLocation, objectRadius))
                if (Viewer.Camera.InRange(mstsLocation, viewingDistance + objectRadius))
                    frame.AddPrimitive(lightMaterial, lightMesh, ref xnaDTileTranslation);

            if (lightMesh.hasHeadlight)
            {
                Vector3 lightconeLoc = lightMesh.lightconeLoc;
                lightconeLoc.Z *= -1;
                xnaLightconeLoc = Vector3.Transform(lightconeLoc, xnaDTileTranslation);
                xnaLightconeDir = xnaLightconeLoc - xnaDTileTranslation.Translation;
                xnaLightconeDir.Normalize();
                // Tilt the light cone downward
                xnaLightconeDir.Y = -0.7f;
            }
            */
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
        public bool hasHeadlight = false;
        public Vector3 lightconeLoc;
        bool isFirst = true;

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
        public LightGlowMesh(RenderProcess renderProcess, TrainCar car)
        {
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
                if (light.type == 1 && isFirst) // Find the first light cone
                {
                    if (light.unit == 2)
                    {
                        hasHeadlight = true;
                        lightconeLoc = light.StateList.ElementAt<LightState>(i).position;
                        isFirst = false;
                    }
                }
               
                if (light.type == 0 && light.penalty < 2 && light.headlight != 2) // Not a light cone, not penalty, not dim version
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
                    numLights--; // Don't include special versions (light cone, penalty, dim) in count.
            }

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
            float lightAzimuth;
            Vector3 normal = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 lightPosition = new Vector3(0.0f, 0.0f, 0.0f);
                        
            // Create the light glow vertex array.
            for (int i = 0; i < numLights * 6; i++)
            {
                // Parse the MSTS color variable
                uint u = (uint)unchecked(color[i / 6, 0]);
                colorA = (float)((u & 0xff000000) >> 24) / 255;
                colorR = (float)((u & 0x00ff0000) >> 16) / 255;
                colorG = (float)((u & 0x0000ff00) >> 8) / 255;
                colorB = (float)(u & 0x000000ff) / 255;
                // Convert "azimuth" to a normal
                lightAzimuth = MathHelper.ToRadians(360 + azimuth[i / 6, 0].X);
                normal = new Vector3((float)Math.Sin(lightAzimuth), 0.0f, -(float)Math.Cos(lightAzimuth));
                // Convert position to XNA
                // Note: MSTS cars are offset 0.275 m above tracks
                lightPosition = new Vector3(position[i / 6, 0].X, position[i / 6, 0].Y*1.12f + 0.275f, -position[i / 6, 0].Z);
                radius[i / 6, 0] *= 0.7f;

                lights[i++] = new LightGlowVertex(lightPosition, normal,
                    new Vector3(colorR, colorG, colorB),
                    new Vector4(colorA, radius[i / 6, 0], 1, 1));
                lights[i++] = new LightGlowVertex(lightPosition, normal,
                    new Vector3(colorR, colorG, colorB),
                    new Vector4(colorA, radius[i / 6, 0], 0, 0));
                lights[i++] = new LightGlowVertex(lightPosition, normal,
                    new Vector3(colorR, colorG, colorB),
                    new Vector4(colorA, radius[i / 6, 0], 1, 0));

                lights[i++] = new LightGlowVertex(lightPosition, normal,
                    new Vector3(colorR, colorG, colorB),
                    new Vector4(colorA, radius[i / 6, 0], 1, 1));
                lights[i++] = new LightGlowVertex(lightPosition, normal,
                    new Vector3(colorR, colorG, colorB),
                    new Vector4(colorA, radius[i / 6, 0], 0, 1));
                lights[i] = new LightGlowVertex(lightPosition, normal,
                    new Vector3(colorR, colorG, colorB),
                    new Vector4(colorA, radius[i / 6, 0], 0, 0));
            }
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
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

        /// <summary>
        /// Light glow vertex constructor.
        /// </summary>
        /// <param name="position">quad position</param>
        /// <param name="normal">quad normals</param>
        /// <param name="rgbcolor">rgb color</param>
        /// <param name="alphascaletex">color alpha, quad scale, texture coords</param>
        /// TODO: Expand as needed for increased functionality
        public LightGlowVertex(Vector3 position, Vector3 normal, Vector3 rgbcolor, Vector4 alphascaletex)
        {
            this.position = position;
            this.normal = normal;
            this.rgbcolor = rgbcolor;
            this.alphascaletex = alphascaletex;
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
                    VertexElementUsage.Position, 1)
           };

        // Size of one vertex in bytes
        public static int SizeInBytes = sizeof(float) * (3 + 3 + 3 + 4);
    }
    #endregion

}

