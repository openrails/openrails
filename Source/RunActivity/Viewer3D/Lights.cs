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

    #region Light enums
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
    #endregion

    /// <summary>
    /// The Light class encapsulates the data for each Light object 
    /// in the Lights block of an ENG/WAG file. 
    /// </summary>
    public class Light
    {
        public int Index;
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

        public Light(int index, STFReader stf)
        {
            Index = index;
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
                new STFReader.TokenProcessor("light", ()=>{ Lights.Add(new Light(Lights.Count, stf)); }),
            });
            if (Lights.Count == 0)
                throw new InvalidDataException("lights with no lights");
        }
    }

    public class LightDrawer
    {
        readonly Viewer3D Viewer;
        readonly TrainCar Car;
        readonly Material LightGlowMaterial;
        readonly Material LightConeMaterial;

        public bool CarInService;
        public bool CarIsPlayer;
        public bool CarIsFirst;
        public bool CarIsLast;
        public bool CarCoupledFront;
        public bool CarCoupledRear;
        public int TrainHeadlight;
        public bool IsDay;
        public WeatherType Weather;
        List<LightMesh> LightMeshes = new List<LightMesh>();

        LightMesh ActiveLightCone;
        public bool HasLightCone;
        public float LightConeFadeIn;
        public float LightConeFadeOut;
        public Vector3 LightConePosition;
        public Vector3 LightConeDirection;
        public float LightConeDistance;
        public float LightConeMinDotProduct;

        public LightDrawer(Viewer3D viewer, TrainCar car)
        {
            Viewer = viewer;
            Car = car;
            LightGlowMaterial = Materials.Load(Viewer.RenderProcess, "LightGlowMaterial");
            LightConeMaterial = Materials.Load(Viewer.RenderProcess, "LightConeMaterial");

            UpdateState();
            if (Car.Lights != null)
            {
                foreach (var light in Car.Lights.Lights)
                {
                    switch (light.Type)
                    {
                        case LightType.Glow:
                            LightMeshes.Add(new LightGlowMesh(this, Viewer.RenderProcess, light));
                            break;
                        case LightType.Cone:
                            LightMeshes.Add(new LightConeMesh(this, Viewer.RenderProcess, light));
                            break;
                    }
                }
            }
            HasLightCone = LightMeshes.Any(lm => lm is LightConeMesh);
#if DEBUG_LIGHT_STATES
            Console.WriteLine();
#endif
            UpdateActiveLightCone();
        }

        void UpdateActiveLightCone()
        {
            var newLightCone = LightMeshes.FirstOrDefault(lm => lm is LightConeMesh && lm.Enabled);

            // Fade-in should be NEW headlight.
            if ((ActiveLightCone == null) && (newLightCone != null))
                LightConeFadeIn = newLightCone.Light.FadeIn;
            else
                LightConeFadeIn = 0;

            // Fade-out should be OLD headlight.
            if ((ActiveLightCone != null) && (newLightCone == null))
                LightConeFadeOut = ActiveLightCone.Light.FadeOut;
            else
                LightConeFadeOut = 0;

#if DEBUG_LIGHT_STATES
            if (Headlight != null)
                Console.WriteLine("Old headlight: index = {0}, fade-in = {1:F1}, fade-out = {2:F1}", Headlight.Light.Index, Headlight.Light.FadeIn, Headlight.Light.FadeOut);
            else
                Console.WriteLine("Old headlight: <none>");
            if (headlight != null)
                Console.WriteLine("New headlight: index = {0}, fade-in = {1:F1}, fade-out = {2:F1}", headlight.Light.Index, headlight.Light.FadeIn, headlight.Light.FadeOut);
            else
                Console.WriteLine("New headlight: <none>");
            if ((Headlight != null) || (headlight != null))
            {
                Console.WriteLine("Headlight changed from {0} to {1}, fade-in = {2:F1}, fade-out = {3:F1}", Headlight != null ? Headlight.Light.Index.ToString() : "<none>", headlight != null ? headlight.Light.Index.ToString() : "<none>", LightConeFadeIn, LightConeFadeOut);
                Console.WriteLine();
            }
#endif

            ActiveLightCone = newLightCone;
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (UpdateState())
            {
                foreach (var lightMesh in LightMeshes)
                    lightMesh.UpdateState(this);
#if DEBUG_LIGHT_STATES
                Console.WriteLine();
#endif
                UpdateActiveLightCone();
            }

            foreach (var lightMesh in LightMeshes)
                lightMesh.PrepareFrame(frame, elapsedTime);

            int dTileX = Car.WorldPosition.TileX - Viewer.Camera.TileX;
            int dTileZ = Car.WorldPosition.TileZ - Viewer.Camera.TileZ;
            Matrix xnaDTileTranslation = Matrix.CreateTranslation(dTileX * 2048, 0, -dTileZ * 2048);  // object is offset from camera this many tiles
            xnaDTileTranslation = Car.WorldPosition.XNAMatrix * xnaDTileTranslation;

            Vector3 mstsLocation = new Vector3(xnaDTileTranslation.Translation.X, xnaDTileTranslation.Translation.Y, -xnaDTileTranslation.Translation.Z);

            float objectRadius = 20; // Even more arbitrary.
            float viewingDistance = 1500; // Arbitrary.
            if (Viewer.Camera.InFOV(mstsLocation, objectRadius))
                if (Viewer.Camera.InRange(mstsLocation, viewingDistance + objectRadius))
                    foreach (var lightMesh in LightMeshes)
                        if (lightMesh.Enabled || lightMesh.FadeOut)
                            if (lightMesh is LightGlowMesh)
                                frame.AddPrimitive(LightGlowMaterial, lightMesh, RenderPrimitiveGroup.Lights, ref xnaDTileTranslation);
                            //else if (lightMesh is LightConeMesh)
                            //    frame.AddPrimitive(LightConeMaterial, lightMesh, RenderPrimitiveGroup.Lights, ref xnaDTileTranslation);

            // Set the active light cone info for the material code.
            if (HasLightCone && ActiveLightCone != null)
            {
                var angle = MathHelper.ToRadians(ActiveLightCone.Light.States[0].Angle);
                var position = ActiveLightCone.Light.States[0].Position;
                position.Z *= -1;
                LightConePosition = Vector3.Transform(position, xnaDTileTranslation);
                LightConeDirection = Vector3.Transform(-Vector3.UnitZ, Car.WorldPosition.XNAMatrix);
                LightConeDirection -= Car.WorldPosition.XNAMatrix.Translation;
                LightConeDirection.Normalize();
                LightConeDistance = (float)(ActiveLightCone.Light.States[0].Radius / Math.Sin(angle));
                LightConeMinDotProduct = (float)Math.Cos(angle);
            }
        }

#if DEBUG_LIGHT_STATES
        public const string MeshStateLabel = "Index       Enabled     Type        Headlight   Unit        Penalty     Control     Service     Time        Weather     Coupling  ";
        public const string MeshStateFormat = "{0,-10  }  {1,-10   }  {2,-10   }  {3,-10   }  {4,-10   }  {5,-10   }  {6,-10   }  {7,-10   }  {8,-10   }  {9,-10   }  {10,-10  }";
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
                Console.WriteLine("LightDrawer: {0} {1,9} {2}{3}{4}{5}{6}{7}{8}{9}{10}{11}{12}",
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
                    Console.WriteLine();
                    Console.WriteLine(MeshStateLabel);
                    Console.WriteLine(new String('=', MeshStateLabel.Length));
                }
#endif

                return true;
            }
            return false;
        }
    }

    public abstract class LightMesh : RenderPrimitive
    {
        public Light Light;
        public bool Enabled;
        public Vector2 Fade;
        public bool FadeIn;
        public bool FadeOut;
        protected float FadeTime;
        protected int State;
        protected int StateCount;
        protected float StateTime;

        public LightMesh(Light light)
        {
            Light = light;
        }

        internal void UpdateState(LightDrawer lightDrawer)
        {
            var oldEnabled = Enabled;
            Enabled = true;
            if (Light.Headlight != LightHeadlightCondition.Ignore)
            {
                if (Light.Headlight == LightHeadlightCondition.Off)
                    Enabled &= lightDrawer.TrainHeadlight == 0;
                else if (Light.Headlight == LightHeadlightCondition.Dim)
                    Enabled &= lightDrawer.TrainHeadlight == 1;
                else if (Light.Headlight == LightHeadlightCondition.Bright)
                    Enabled &= lightDrawer.TrainHeadlight == 2;
                else if (Light.Headlight == LightHeadlightCondition.DimBright)
                    Enabled &= lightDrawer.TrainHeadlight >= 1;
                else if (Light.Headlight == LightHeadlightCondition.OffDim)
                    Enabled &= lightDrawer.TrainHeadlight <= 1;
                else if (Light.Headlight == LightHeadlightCondition.OffBright)
                    Enabled &= lightDrawer.TrainHeadlight != 1;
            }
            if (Light.Unit != LightUnitCondition.Ignore)
            {
                if (Light.Unit == LightUnitCondition.First || Light.Unit == LightUnitCondition.LastFlip)
                    Enabled &= lightDrawer.CarIsFirst;
                else if (Light.Unit == LightUnitCondition.Last || Light.Unit == LightUnitCondition.FirstFlip)
                    Enabled &= lightDrawer.CarIsLast;
                else if (Light.Unit == LightUnitCondition.Middle)
                    Enabled &= !lightDrawer.CarIsFirst && !lightDrawer.CarIsLast;
            }
            // TODO: Check Penalty here.
            if (Light.Control != LightControlCondition.Ignore)
            {
                Enabled &= lightDrawer.CarIsPlayer == (Light.Control == LightControlCondition.Player);
            }
            if (Light.Service != LightServiceCondition.Ignore)
            {
                Enabled &= lightDrawer.CarInService == (Light.Service == LightServiceCondition.Yes);
            }
            if (Light.TimeOfDay != LightTimeOfDayCondition.Ignore)
            {
                Enabled &= lightDrawer.IsDay == (Light.TimeOfDay == LightTimeOfDayCondition.Day);
            }
            if (Light.Weather != LightWeatherCondition.Ignore)
            {
                if (lightDrawer.Weather == WeatherType.Clear)
                    Enabled &= Light.Weather == LightWeatherCondition.Clear;
                else if (lightDrawer.Weather == WeatherType.Rain)
                    Enabled &= Light.Weather == LightWeatherCondition.Rain;
                else if (lightDrawer.Weather == WeatherType.Snow)
                    Enabled &= Light.Weather == LightWeatherCondition.Snow;
            }
            if (Light.Coupling != LightCouplingCondition.Ignore)
            {
                Enabled &= lightDrawer.CarCoupledFront == (Light.Coupling == LightCouplingCondition.Front || Light.Coupling == LightCouplingCondition.Both);
                Enabled &= lightDrawer.CarCoupledRear == (Light.Coupling == LightCouplingCondition.Rear || Light.Coupling == LightCouplingCondition.Both);
            }

            if (oldEnabled != Enabled)
            {
                FadeIn = Enabled;
                FadeOut = !Enabled;
                FadeTime = 0;
            }

#if DEBUG_LIGHT_STATES
            Console.WriteLine(LightDrawer.MeshStateFormat, Light.Index, Enabled, Light.Type, Light.Headlight, Light.Unit, Light.Penalty, Light.Control, Light.Service, Light.TimeOfDay, Light.Weather, Light.Coupling);
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
    }

    public class LightGlowMesh : LightMesh
    {
        static VertexDeclaration LightVertexDeclaration;

        LightGlowVertex[] LightVertices;

        public LightGlowMesh(LightDrawer lightDrawer, RenderProcess renderProcess, Light light)
            : base(light)
        {
            Debug.Assert(light.Type == LightType.Glow, "LightGlowMesh is only for LightType.Glow lights.");

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

            UpdateState(lightDrawer);
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

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.VertexDeclaration = LightVertexDeclaration;
            graphicsDevice.DrawUserPrimitives<LightGlowVertex>(PrimitiveType.TriangleList, LightVertices, State * 6, 2);
        }
    }

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

    public class LightConeMesh : LightMesh
    {
        const int CircleSegments = 16;

        static VertexDeclaration VertexDeclaration;
        static VertexBuffer VertexBuffer;
        static IndexBuffer IndexBuffer;

        public LightConeMesh(LightDrawer lightDrawer, RenderProcess renderProcess, Light light)
            : base(light)
        {
            Debug.Assert(light.Type == LightType.Cone, "LightConeMesh is only for LightType.Cone lights.");
            Debug.Assert(light.States.Count == 1, "LightConeMesh only supports 1 state.");
            Debug.Assert(light.States[0].Azimuth.Y == 0, "LightConeMesh only supports Azimuth = 0.");
            Debug.Assert(light.States[0].Elevation.Y == 0, "LightConeMesh only supports Elevation = 0.");

            if (VertexDeclaration == null)
            {
                VertexDeclaration = new VertexDeclaration(renderProcess.GraphicsDevice, VertexPositionColor.VertexElements);
            }
            if (VertexBuffer == null)
            {
                var position = light.States[0].Position;
                position.Z *= -1;
                var radius = light.States[0].Radius;
                var distance = (float)(radius / Math.Sin(MathHelper.ToRadians(light.States[0].Angle)));
                var color = new Color(0.5f, 0.5f, 0.5f, 0.5f);

                var vertexData = new VertexPositionColor[CircleSegments + 2];
                for (var i = 0; i < CircleSegments; i++)
                {
                    var angle = MathHelper.TwoPi * i / CircleSegments;
                    vertexData[i] = new VertexPositionColor(new Vector3(position.X + (float)(radius * Math.Cos(angle)), position.Y + (float)(radius * Math.Sin(angle)), position.Z - distance), color);
                }
                vertexData[CircleSegments + 0] = new VertexPositionColor(position, color);
                vertexData[CircleSegments + 1] = new VertexPositionColor(new Vector3(position.X, position.Y, position.Z - distance), color);
                VertexBuffer = new VertexBuffer(renderProcess.GraphicsDevice, typeof(VertexPositionColor), vertexData.Length, BufferUsage.WriteOnly);
                VertexBuffer.SetData(vertexData);
            }
            if (IndexBuffer == null)
            {
                var indexData = new short[6 * CircleSegments];
                for (var i = 0; i < CircleSegments; i++)
                {
                    var i2 = (i + 1) % CircleSegments;
                    indexData[i * 6 + 0] = (short)(CircleSegments + 0);
                    indexData[i * 6 + 1] = (short)i2;
                    indexData[i * 6 + 2] = (short)i;
                    indexData[i * 6 + 3] = (short)i;
                    indexData[i * 6 + 4] = (short)i2;
                    indexData[i * 6 + 5] = (short)(CircleSegments + 1);
                }
                IndexBuffer = new IndexBuffer(renderProcess.GraphicsDevice, typeof(short), indexData.Length, BufferUsage.WriteOnly);
                IndexBuffer.SetData(indexData);
            }

            UpdateState(lightDrawer);
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.VertexDeclaration = VertexDeclaration;
            graphicsDevice.Vertices[0].SetSource(VertexBuffer, 0, VertexPositionColor.SizeInBytes);
            graphicsDevice.Indices = IndexBuffer;

            graphicsDevice.RenderState.CullMode = CullMode.CullClockwiseFace;
            graphicsDevice.RenderState.StencilFunction = CompareFunction.Always;
            graphicsDevice.RenderState.StencilPass = StencilOperation.Increment;
            graphicsDevice.RenderState.DepthBufferFunction = CompareFunction.Greater;
            graphicsDevice.RenderState.DestinationBlend = Blend.One;
            graphicsDevice.RenderState.SourceBlend = Blend.Zero;
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, CircleSegments + 2, 0, 2 * CircleSegments);

            graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
            graphicsDevice.RenderState.StencilFunction = CompareFunction.Less;
            graphicsDevice.RenderState.StencilPass = StencilOperation.Zero;
            graphicsDevice.RenderState.DepthBufferFunction = CompareFunction.LessEqual;
            graphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
            graphicsDevice.RenderState.SourceBlend = Blend.One;
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, CircleSegments + 2, 0, 2 * CircleSegments);
        }
    }
}
