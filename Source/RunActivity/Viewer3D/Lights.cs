// COPYRIGHT 2010, 2011 by the Open Rails project.
// This code is provided to enable you to contribute improvements to the open rails program.  
// Use of the code for any other purpose or distribution of the code to anyone else
// is prohibited without specific written permission from admin@openrails.org.

// Uncomment either or both of these for debugging information about lights.
//#define DEBUG_LIGHT_STATES
//#define DEBUG_LIGHT_TRANSITIONS

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
    #region LightState
    /// <summary>
    /// A LightState object encapsulates the data for each State in the States subblock.
    /// </summary>
    public class LightState
    {
        public float Duration;
        public uint Color;
        public Vector3 Position;
        public float Radius;
        public Vector3 Azimuth;
        public Vector3 Elevation;
        public bool Transition;
        public float Angle;

        public LightState(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new[] {
                new STFReader.TokenProcessor("duration", ()=>{ Duration = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("lightcolour", ()=>{ Color = stf.ReadHexBlock(null); }),
                new STFReader.TokenProcessor("position", ()=>{ Position = stf.ReadVector3Block(STFReader.UNITS.None, Vector3.Zero); }),
                new STFReader.TokenProcessor("radius", ()=>{ Radius = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                new STFReader.TokenProcessor("azimuth", ()=>{ Azimuth = stf.ReadVector3Block(STFReader.UNITS.None, Vector3.Zero); }),
                new STFReader.TokenProcessor("elevation", ()=>{ Elevation = stf.ReadVector3Block(STFReader.UNITS.None, Vector3.Zero); }),
                new STFReader.TokenProcessor("transition", ()=>{ Transition = 0 != stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("angle", ()=>{ Angle = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
            });
        }
    }
    #endregion

    #region Light
    public enum LightType
    {
        Glow,
        Cone,
    }

    public enum LightHeadlightCondition
    {
        Ignore,
        Off,
        Dim,
        Bright,
        DimBright, // MSTSBin
        OffDim, // MSTSBin
        OffBright, // MSTSBin
        MSTSBinXXX, // MSTSBin // TODO: MSTSBin labels this the same as DimBright. Not sure what it means.
    }

    public enum LightUnitCondition
    {
        Ignore,
        Middle,
        First,
        Last,
        LastFlip, // MSTSBin // TODO: MSTSBin cab switched?
        FirstFlip, // MSTSBin // TODO: MSTSBin cab switched?
    }

    public enum LightPenaltyCondition
    {
        Ignore,
        No,
        Yes,
    }

    public enum LightControlCondition
    {
        Ignore,
        AI,
        Player,
    }

    public enum LightServiceCondition
    {
        Ignore,
        No,
        Yes,
    }

    public enum LightTimeOfDayCondition
    {
        Ignore,
        Day,
        Night,
    }

    public enum LightWeatherCondition
    {
        Ignore,
        Clear,
        Rain,
        Snow,
    }

    public enum LightCouplingCondition
    {
        Ignore,
        Front,
        Rear,
        Both,
    }

    /// <summary>
    /// The Light class encapsulates the data for each Light object 
    /// in the Lights block of an ENG/WAG file. 
    /// </summary>
    public class Light
    {
        public LightType Type;
        public LightHeadlightCondition Headlight;
        public LightUnitCondition Unit;
        public LightPenaltyCondition Penalty;
        public LightControlCondition Control;
        public LightServiceCondition Service;
        public LightTimeOfDayCondition TimeOfDay;
        public LightWeatherCondition Weather;
        public LightCouplingCondition Coupling;
        public bool Cycle;
        public float FadeIn;
        public float FadeOut;
        public List<LightState> States = new List<LightState>();

        public Light(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new[] {
                new STFReader.TokenProcessor("type", ()=>{ Type = (LightType)stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("conditions", ()=>{ stf.MustMatch("("); stf.ParseBlock(new[] {
                    new STFReader.TokenProcessor("headlight", ()=>{ Headlight = (LightHeadlightCondition)stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                    new STFReader.TokenProcessor("unit", ()=>{ Unit = (LightUnitCondition)stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                    new STFReader.TokenProcessor("penalty", ()=>{ Penalty = (LightPenaltyCondition)stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                    new STFReader.TokenProcessor("control", ()=>{ Control = (LightControlCondition)stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                    new STFReader.TokenProcessor("service", ()=>{ Service = (LightServiceCondition)stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                    new STFReader.TokenProcessor("timeofday", ()=>{ TimeOfDay = (LightTimeOfDayCondition)stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                    new STFReader.TokenProcessor("weather", ()=>{ Weather = (LightWeatherCondition)stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                    new STFReader.TokenProcessor("coupling", ()=>{ Coupling = (LightCouplingCondition)stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                });}),
                new STFReader.TokenProcessor("cycle", ()=>{ Cycle = 0 != stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("fadein", ()=>{ FadeIn = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("fadeout", ()=>{ FadeOut = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("states", ()=>{
                    stf.MustMatch("(");
                    var numStates = stf.ReadInt(STFReader.UNITS.None, null);
                    stf.ParseBlock(new[] {
                        new STFReader.TokenProcessor("state", ()=>{
                            if (States.Count < numStates)
                                States.Add(new LightState(stf));
                            else
                                STFException.TraceWarning(stf, "Additional State ignored");
                        }),
                    });
                    if (States.Count != numStates)
                        STFException.TraceWarning(stf, "Missing State block");
                }),
            });
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
    public class LightCollection
    {
        public List<Light> Lights = new List<Light>();

        public LightCollection(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ReadInt(STFReader.UNITS.None, null); // count; ignore this because its not always correct
            stf.ParseBlock(new[] {
                new STFReader.TokenProcessor("light", ()=>{ Lights.Add(new Light(stf)); }),
            });
            if (Lights.Count == 0)
                throw new InvalidDataException("lights with no lights");
        }
    }
    #endregion

    #region LightGlowDrawer
    public class LightGlowDrawer
    {
        readonly Viewer3D Viewer;
        readonly TrainCar Car;
        readonly Material LightMaterial;

        public bool CarInService;
        public bool CarIsPlayer;
        public bool CarIsFirst;
        public bool CarIsLast;
        public bool CarCoupledFront;
        public bool CarCoupledRear;
        public int TrainHeadlight;
        public bool IsDay;
        public WeatherType Weather;
        List<LightGlowMesh> LightGlowMeshes = new List<LightGlowMesh>();

        Vector3 LightConeLoc;
        public bool HasHeadlight;
        public Vector3 XNALightConeLoc;
        public Vector3 XNALightConeDir;
        public float LightConeFadeIn;
        public float LightConeFadeOut;

        public LightGlowDrawer(Viewer3D viewer, TrainCar car)
        {
            Viewer = viewer;
            Car = car;
            LightMaterial = Materials.Load(Viewer.RenderProcess, "LightGlowMaterial");

            UpdateState();
            if (Car.Lights != null)
            {
                foreach (var light in Car.Lights.Lights)
                {
                    if (light.Type == LightType.Cone)
                    {
                        if (HasHeadlight)
                        {
                            Trace.WriteLine("Ignored extra 'cone' light.");
                            continue;
                        }
                        HasHeadlight = true;
                        LightConeLoc = light.States[0].Position;
                        LightConeLoc.Z *= -1;
                        LightConeFadeIn = light.FadeIn;
                        LightConeFadeOut = light.FadeOut;
                    }
                    else
                    {
                        LightGlowMeshes.Add(new LightGlowMesh(this, Viewer.RenderProcess, light));
                    }
                }
            }
#if DEBUG_LIGHT_STATES
            Console.WriteLine();
#endif
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (UpdateState())
            {
                foreach (var lightGlowMesh in LightGlowMeshes)
                    lightGlowMesh.UpdateState(this);
#if DEBUG_LIGHT_STATES
                Console.WriteLine();
#endif
            }

            foreach (var lightGlowMesh in LightGlowMeshes)
                lightGlowMesh.PrepareFrame(frame, elapsedTime);

            int dTileX = Car.WorldPosition.TileX - Viewer.Camera.TileX;
            int dTileZ = Car.WorldPosition.TileZ - Viewer.Camera.TileZ;
            Matrix xnaDTileTranslation = Matrix.CreateTranslation(dTileX * 2048, 0, -dTileZ * 2048);  // object is offset from camera this many tiles
            xnaDTileTranslation = Car.WorldPosition.XNAMatrix * xnaDTileTranslation;

            Vector3 mstsLocation = new Vector3(xnaDTileTranslation.Translation.X, xnaDTileTranslation.Translation.Y, -xnaDTileTranslation.Translation.Z);

            float objectRadius = 20; // Even more arbitrary.
            float viewingDistance = 1500; // Arbitrary.
            if (Viewer.Camera.InFOV(mstsLocation, objectRadius))
                if (Viewer.Camera.InRange(mstsLocation, viewingDistance + objectRadius))
                    foreach (var lightGlowMesh in LightGlowMeshes)
                        if (lightGlowMesh.Enabled || lightGlowMesh.FadeOut)
                            frame.AddPrimitive(LightMaterial, lightGlowMesh, RenderPrimitiveGroup.Lights, ref xnaDTileTranslation);

            // Set the headlight cone location and direction vectors
            if (HasHeadlight)
            {
                XNALightConeLoc = Vector3.Transform(LightConeLoc, xnaDTileTranslation);
                XNALightConeDir = XNALightConeLoc - xnaDTileTranslation.Translation;
                XNALightConeDir.Normalize();
                // TODO: Tilt the light cone downward at the correct angle.
                XNALightConeDir.Y = -0.5f;
            }
        }

#if DEBUG_LIGHT_STATES
        public const string MeshStateLabel = "    Enabled     Type        Headlight   Unit        Penalty     Control     Service     Time        Weather     Coupling  ";
        public const string MeshStateFormat = "    {0,-10  }  {1,-10   }  {2,-10   }  {3,-10   }  {4,-10   }  {5,-10   }  {6,-10   }  {7,-10   }  {8,-10   }  {9,-10   }";
#endif

        bool UpdateState()
        {
            var newCarInService = Car.Train != null;
            var newCarIsPlayer = Car == Viewer.PlayerLocomotive;
            var newCarIsFirst = Car.Train == null || Car.Train.FirstCar == Car;
            var newCarIsLast = Car.Train == null || Car.Train.LastCar == Car;
            var newCarCoupledFront = Car.Train != null && (Car.Train.Cars.Count > 1) && !(Car.Flipped ? newCarIsLast : newCarIsFirst);
            var newCarCoupledRear = Car.Train != null && (Car.Train.Cars.Count > 1) && !(Car.Flipped ? newCarIsFirst : newCarIsLast);
            var newTrainHeadlight = Car.Train != null && Car.Train == Viewer.PlayerTrain ? Viewer.PlayerLocomotive.Headlight : 2;
            var newIsDay = Viewer.SkyDrawer.solarDirection.Y > 0;
            var newWeather = Viewer.Simulator.Weather;
            // TODO: Check for relevant Penalty changes.

            if (
                (CarInService != newCarInService) ||
                (CarIsPlayer != newCarIsPlayer) ||
                (CarIsFirst != newCarIsFirst) ||
                (CarIsLast != newCarIsLast) ||
                (CarCoupledFront != newCarCoupledFront) ||
                (CarCoupledRear != newCarCoupledRear) ||
                (TrainHeadlight != newTrainHeadlight) ||
                (IsDay != newIsDay) ||
                (Weather != newWeather))
            {
                CarInService = newCarInService;
                CarIsPlayer = newCarIsPlayer;
                CarIsFirst = newCarIsFirst;
                CarIsLast = newCarIsLast;
                CarCoupledFront = newCarCoupledFront;
                CarCoupledRear = newCarCoupledRear;
                TrainHeadlight = newTrainHeadlight;
                IsDay = newIsDay;
                Weather = newWeather;

#if DEBUG_LIGHT_STATES
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("LightGlowDrawer: {0} {1,9} {2}{3}{4}{5}{6}{7}{8}{9}{10}{11}{12}",
                    Car.Train != null ? Car.Train.FrontTDBTraveller.WorldLocation : Car.WorldPosition.WorldLocation, Car.Train != null ? "train car" : "car", Car.Train != null ? Car.Train.Cars.IndexOf(Car) : 0, Car.Flipped ? " Flipped" : "",
                    CarInService ? " Service" : "",
                    CarIsPlayer ? " Player" : " AI",
                    CarIsFirst ? " First" : "",
                    CarIsLast ? " Last" : "",
                    CarCoupledFront ? " CoupledFront" : "",
                    CarCoupledRear ? " CoupledRear" : "",
                    TrainHeadlight == 2 ? " HL=Bright" : TrainHeadlight == 1 ? " HL=Dim" : "",
                    IsDay ? "" : " Night",
                    Weather == WeatherType.Snow ? " Snow" : Weather == WeatherType.Rain ? " Rain" : "");
                if (Car.Lights != null)
                {
                    Console.WriteLine(MeshStateLabel);
                    Console.WriteLine(new String('=', MeshStateLabel.Length));
                }
#endif

                return true;
            }
            return false;
        }
    }
    #endregion

    #region LightGlowMesh
    public class LightGlowMesh : RenderPrimitive
    {
        static VertexDeclaration LightVertexDeclaration;

        public bool Enabled;
        public Vector2 Fade;
        public bool FadeIn;
        public bool FadeOut;
        float FadeTime;
        int State;
        int StateCount;
        float StateTime;
        Light Light;
        LightGlowVertex[] LightVertices;

        public LightGlowMesh(LightGlowDrawer lightGlowDrawer, RenderProcess renderProcess, Light light)
        {
            Debug.Assert(light.Type == LightType.Glow, "LightGlowMesh is only for LightType.Glow lights.");
            Light = light;

            if (LightVertexDeclaration == null)
                LightVertexDeclaration = new VertexDeclaration(renderProcess.GraphicsDevice, LightGlowVertex.VertexElements);

#if DEBUG_LIGHT_TRANSITIONS
            Console.WriteLine();
            Console.WriteLine("LightGlowMesh transitions:");
#endif
            if (light.Cycle)
            {
                StateCount = 2 * light.States.Count - 2;
                LightVertices = new LightGlowVertex[6 * StateCount];
                for (var i = 0; i < light.States.Count - 1; i++)
                    SetUpTransition(i, light, i, i + 1);
                for (var i = light.States.Count - 1; i > 0; i--)
                    SetUpTransition(light.States.Count * 2 - 1 - i, light, i, i - 1);
            }
            else
            {
                StateCount = light.States.Count;
                LightVertices = new LightGlowVertex[6 * StateCount];
                for (var i = 0; i < light.States.Count; i++)
                    SetUpTransition(i, light, i, (i + 1) % light.States.Count);
            }
#if DEBUG_LIGHT_TRANSITIONS
            Console.WriteLine();
#endif

            UpdateState(lightGlowDrawer);
        }

        void SetUpTransition(int state, Light light, int stateIndex1, int stateIndex2)
        {
            var state1 = light.States[stateIndex1];
            var state2 = light.States[stateIndex2];

#if DEBUG_LIGHT_TRANSITIONS
            Console.WriteLine("    Transition {0} is from state {1} to state {2} over {3:F1}s", state, stateIndex1, stateIndex2, state1.Duration);
#endif

            // FIXME: Is conversion of "azimuth" to a normal right?

            var position1 = state1.Position; position1.Z *= -1;
            var normal1 = Vector3.Transform(Vector3.Transform(-Vector3.UnitZ, Matrix.CreateRotationX(MathHelper.ToRadians(-state1.Elevation.Y))), Matrix.CreateRotationY(MathHelper.ToRadians(-state1.Azimuth.Y)));
            var color1 = ColorToVector(state1.Color);

            var position2 = state2.Position; position2.Z *= -1;
            var normal2 = Vector3.Transform(Vector3.Transform(-Vector3.UnitZ, Matrix.CreateRotationX(MathHelper.ToRadians(-state2.Elevation.Y))), Matrix.CreateRotationY(MathHelper.ToRadians(-state2.Azimuth.Y)));
            var color2 = ColorToVector(state2.Color);

            LightVertices[state * 6 + 0] = new LightGlowVertex(new Vector2(1, 1), 0, position1, position2, normal1, normal2, color1, color2, state1.Radius, state2.Radius);
            LightVertices[state * 6 + 1] = new LightGlowVertex(new Vector2(0, 0), 0, position1, position2, normal1, normal2, color1, color2, state1.Radius, state2.Radius);
            LightVertices[state * 6 + 2] = new LightGlowVertex(new Vector2(1, 0), 0, position1, position2, normal1, normal2, color1, color2, state1.Radius, state2.Radius);
            LightVertices[state * 6 + 3] = new LightGlowVertex(new Vector2(1, 1), 0, position1, position2, normal1, normal2, color1, color2, state1.Radius, state2.Radius);
            LightVertices[state * 6 + 4] = new LightGlowVertex(new Vector2(0, 1), 0, position1, position2, normal1, normal2, color1, color2, state1.Radius, state2.Radius);
            LightVertices[state * 6 + 5] = new LightGlowVertex(new Vector2(0, 0), 0, position1, position2, normal1, normal2, color1, color2, state1.Radius, state2.Radius);
        }

        static Vector4 ColorToVector(uint color)
        {
            return new Vector4((float)((color & 0x00ff0000) >> 16) / 255, (float)((color & 0x0000ff00) >> 8) / 255, (float)(color & 0x000000ff) / 255, (float)((color & 0xff000000) >> 24) / 255);
        }

        internal void UpdateState(LightGlowDrawer lightGlowDrawer)
        {
            var oldEnabled = Enabled;
            Enabled = true;
            if (Light.Headlight != LightHeadlightCondition.Ignore)
            {
                if (Light.Headlight == LightHeadlightCondition.Off)
                    Enabled &= lightGlowDrawer.TrainHeadlight == 0;
                else if (Light.Headlight == LightHeadlightCondition.Dim)
                    Enabled &= lightGlowDrawer.TrainHeadlight == 1;
                else if (Light.Headlight == LightHeadlightCondition.Bright)
                    Enabled &= lightGlowDrawer.TrainHeadlight == 2;
                else if (Light.Headlight == LightHeadlightCondition.DimBright)
                    Enabled &= lightGlowDrawer.TrainHeadlight >= 1;
                else if (Light.Headlight == LightHeadlightCondition.OffDim)
                    Enabled &= lightGlowDrawer.TrainHeadlight <= 1;
                else if (Light.Headlight == LightHeadlightCondition.OffBright)
                    Enabled &= lightGlowDrawer.TrainHeadlight != 1;
            }
            if (Light.Unit != LightUnitCondition.Ignore)
            {
                if (Light.Unit == LightUnitCondition.First || Light.Unit == LightUnitCondition.LastFlip)
                    Enabled &= lightGlowDrawer.CarIsFirst;
                else if (Light.Unit == LightUnitCondition.Last || Light.Unit == LightUnitCondition.FirstFlip)
                    Enabled &= lightGlowDrawer.CarIsLast;
                else if (Light.Unit == LightUnitCondition.Middle)
                    Enabled &= !lightGlowDrawer.CarIsFirst && !lightGlowDrawer.CarIsLast;
            }
            // TODO: Check Penalty here.
            if (Light.Control != LightControlCondition.Ignore)
            {
                Enabled &= lightGlowDrawer.CarIsPlayer == (Light.Control == LightControlCondition.Player);
            }
            if (Light.Service != LightServiceCondition.Ignore)
            {
                Enabled &= lightGlowDrawer.CarInService == (Light.Service == LightServiceCondition.Yes);
            }
            if (Light.TimeOfDay != LightTimeOfDayCondition.Ignore)
            {
                Enabled &= lightGlowDrawer.IsDay == (Light.TimeOfDay == LightTimeOfDayCondition.Day);
            }
            if (Light.Weather != LightWeatherCondition.Ignore)
            {
                if (lightGlowDrawer.Weather == WeatherType.Clear)
                    Enabled &= Light.Weather == LightWeatherCondition.Clear;
                else if (lightGlowDrawer.Weather == WeatherType.Rain)
                    Enabled &= Light.Weather == LightWeatherCondition.Rain;
                else if (lightGlowDrawer.Weather == WeatherType.Snow)
                    Enabled &= Light.Weather == LightWeatherCondition.Snow;
            }
            if (Light.Coupling != LightCouplingCondition.Ignore)
            {
                Enabled &= lightGlowDrawer.CarCoupledFront == (Light.Coupling == LightCouplingCondition.Front || Light.Coupling == LightCouplingCondition.Both);
                Enabled &= lightGlowDrawer.CarCoupledRear == (Light.Coupling == LightCouplingCondition.Rear || Light.Coupling == LightCouplingCondition.Both);
            }

            if (oldEnabled != Enabled)
            {
                FadeIn = Enabled;
                FadeOut = !Enabled;
                FadeTime = 0;
            }

#if DEBUG_LIGHT_STATES
            Console.WriteLine(LightGlowDrawer.MeshStateFormat, Enabled, Light.Type, Light.Headlight, Light.Unit, Light.Penalty, Light.Control, Light.Service, Light.TimeOfDay, Light.Weather, Light.Coupling);
#endif
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (StateCount > 1)
            {
                StateTime += elapsedTime.ClockSeconds;
                if (StateTime >= Light.States[State % Light.States.Count].Duration)
                {
                    StateTime -= Light.States[State % Light.States.Count].Duration;
                    State = (State + 1) % StateCount;
                    Fade.Y = 0;
                }
                if (Light.States[State % Light.States.Count].Transition)
                    Fade.Y = StateTime / Light.States[State % Light.States.Count].Duration;
            }
            if (FadeIn)
            {
                FadeTime += elapsedTime.ClockSeconds;
                Fade.X = FadeTime / Light.FadeIn;
                if (Fade.X > 1)
                {
                    FadeIn = false;
                    Fade.X = 1;
                }
            }
            else if (FadeOut)
            {
                FadeTime += elapsedTime.ClockSeconds;
                Fade.X = 1 - FadeTime / Light.FadeIn;
                if (Fade.X < 0)
                {
                    FadeOut = false;
                    Fade.X = 0;
                }
            }
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.VertexDeclaration = LightVertexDeclaration;
            graphicsDevice.DrawUserPrimitives<LightGlowVertex>(PrimitiveType.TriangleList, LightVertices, State * 6, 2);
        }
    }
    #endregion

    #region LightGlowVertex definition
    struct LightGlowVertex
    {
        public Vector3 PositionO;
        public Vector3 PositionT;
        public Vector3 NormalO;
        public float Duration;
        public Vector3 NormalT;
        public Vector4 ColorO;
        public Vector4 ColorT;
        public Vector2 TexCoords;
        public float RadiusO;
        public float RadiusT;

        public LightGlowVertex(Vector2 texCoords, float duration, Vector3 position1, Vector3 position2, Vector3 normal1, Vector3 normal2, Vector4 color1, Vector4 color2, float radius1, float radius2)
        {
            PositionO = position1;
            PositionT = position2;
            NormalO = normal1;
            NormalT = normal2;
            ColorO = color1;
            ColorT = color2;
            TexCoords = texCoords;
            Duration = duration;
            RadiusO = radius1;
            RadiusT = radius2;
        }

        // Vertex elements definition
        public static readonly VertexElement[] VertexElements = {
            new VertexElement(0, sizeof(float) * (0), VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0),
            new VertexElement(0, sizeof(float) * (3), VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 1),
            new VertexElement(0, sizeof(float) * (3 + 3), VertexElementFormat.Vector4, VertexElementMethod.Default, VertexElementUsage.Normal, 0),
            new VertexElement(0, sizeof(float) * (3 + 3 + 3), VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Normal, 1),
            new VertexElement(0, sizeof(float) * (3 + 3 + 3 + 4), VertexElementFormat.Vector4, VertexElementMethod.Default, VertexElementUsage.Color, 0),
            new VertexElement(0, sizeof(float) * (3 + 3 + 3 + 4 + 4), VertexElementFormat.Vector4, VertexElementMethod.Default, VertexElementUsage.Color, 1),
            new VertexElement(0, sizeof(float) * (3 + 3 + 3 + 4 + 4 + 4), VertexElementFormat.Vector4, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0)
       };

        // Size of one vertex in bytes
        public static int SizeInBytes = sizeof(float) * (3 + 3 + 3 + 4 + 4 + 4 + 4);
    }
    #endregion
}
