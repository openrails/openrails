// COPYRIGHT 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
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

// Uncomment either or both of these for debugging information about lights.
//#define DEBUG_LIGHT_STATES
//#define DEBUG_LIGHT_TRANSITIONS
//#define DEBUG_LIGHT_CONE
//#define DEBUG_LIGHT_CONE_FULL

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Formats.Msts;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems;
using Orts.Viewer3D.Processes;
using Orts.Viewer3D.RollingStock;
using ORTS.Common;
using ORTS.Scripting.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Orts.Viewer3D
{
    public class LightViewer
    {
        readonly Viewer Viewer;
        readonly TrainCar Car;
        readonly TrainCarViewer CarViewer;
        readonly Material LightGlowMaterial;
        readonly Material LightConeMaterial;

        public int TrainHeadlight;
        public bool CarIsReversed;
        public bool CarIsFirst;
        public bool CarIsLast;
        public bool Penalty;
        public bool CarIsPlayer;
        public bool CarInService;
        public bool IsDay;
        public WeatherType Weather;
        public bool CarCoupledFront;
        public bool CarCoupledRear;
        public bool CarBatteryOn;
        public bool BrakeOn;
        public int ReverserState;
        public bool LeftDoorOpen;
        public bool RightDoorOpen;
        public bool HornOn;
        public bool BellOn;
        public int MU;

        // Caching for shape object world coordinate matricies
        public Dictionary<int, Matrix> ShapeXNATranslations = new Dictionary<int, Matrix>();

        public bool IsLightConeActive { get { return ActiveLightCone != null; } }
        List<LightPrimitive> LightPrimitives = new List<LightPrimitive>();

        LightConePrimitive ActiveLightCone;
        public bool HasLightCone;
        public float LightConeFadeIn;
        public float LightConeFadeOut;
        public Vector3 LightConePosition;
        public Vector3 LightConeDirection;
        public float LightConeDistance;
        public float LightConeOuterAngle;
        public Vector3 LightConeColor;

        public LightViewer(Viewer viewer, TrainCar car, TrainCarViewer carViewer)
        {
            Viewer = viewer;
            Car = car;
            CarViewer = carViewer;
            
            LightGlowMaterial = viewer.MaterialManager.Load("LightGlow", DefineFullTexturePath(Car.Lights.GeneralLightGlowGraphic));
            LightConeMaterial = viewer.MaterialManager.Load("LightCone");

            UpdateState();
            if (Car.Lights != null)
            {
                foreach (var light in Car.Lights.Lights)
                {
                    StaticLight staticLight = null;

                    // Initialization step for light shape attachment, can't do this step in LightCollection
                    if (light.ShapeIndex != -1)
                    {
                        if (light.ShapeIndex < 0 || light.ShapeIndex >= (CarViewer as MSTSWagonViewer).TrainCarShape.XNAMatrices.Count())
                        {
                            Trace.TraceWarning("Light in car {0} has invalid shape index defined, shape index {1} does not exist",
                                (Car as MSTSWagon).WagFilePath, light.ShapeIndex);
                            light.ShapeIndex = 0;
                        }
                    }
                    else
                    {
                        if (light.ShapeHierarchy != null)
                        {
                            if ((CarViewer as MSTSWagonViewer).TrainCarShape.SharedShape.LodControls
                                .SelectMany(l => l.DistanceLevels)
                                .SelectMany(d => d.SubObjects)
                                .SelectMany(s => s.ShapePrimitives)
                                .FirstOrDefault(p => light.ShapeHierarchy.Equals(p.AttachedLight?.ManagedName, StringComparison.OrdinalIgnoreCase)) is var primitive && primitive != null)
                            {
                                light.ShapeIndex = primitive.HierarchyIndex;
                                staticLight = primitive.AttachedLight;
                                staticLight.IntensityX = 0; // Off by default if managed from here
                            }
                            else if ((CarViewer as MSTSWagonViewer).TrainCarShape.SharedShape.MatrixNames.IndexOf(light.ShapeHierarchy) is var index && index >= 0)
                            {
                                light.ShapeIndex = index;
                            }
                            else
                            {
                                Trace.TraceWarning("Light in car {0} has invalid shape index defined, shape name {1} does not exist",
                                    (Car as MSTSWagon).WagFilePath, light.ShapeHierarchy);
                                light.ShapeIndex = 0;
                            }
                        }
                        else
                            light.ShapeIndex = 0;
                    }

                    if (!ShapeXNATranslations.ContainsKey(light.ShapeIndex))
                        ShapeXNATranslations.Add(light.ShapeIndex, Matrix.Identity);
                    
                    switch (light.Type)
                    {
                        case LightType.Glow:
                            LightPrimitives.Add(new LightGlowPrimitive(this, Viewer.RenderProcess, light, staticLight));
                            if (light.Graphic != null)
                                (LightPrimitives.Last() as LightGlowPrimitive).SpecificGlowMaterial = viewer.MaterialManager.Load("LightGlow", DefineFullTexturePath(light.Graphic, true));
                            else
                                (LightPrimitives.Last() as LightGlowPrimitive).SpecificGlowMaterial = LightGlowMaterial;
                            break;
                        case LightType.Cone:
                            LightPrimitives.Add(new LightConePrimitive(this, Viewer.RenderProcess, light, staticLight));
                            break;
                    }

                }
            }
            HasLightCone = LightPrimitives.Any(lm => lm is LightConePrimitive);
#if DEBUG_LIGHT_STATES
            Console.WriteLine();
#endif

            UpdateActiveLightCone();
        }

        string DefineFullTexturePath(string textureName, bool searchSpecificTexture = false)
        {
            if (File.Exists(Path.Combine(Path.GetDirectoryName(Car.WagFilePath), textureName)))
                return Path.Combine(Path.GetDirectoryName(Car.WagFilePath), textureName);
            if (searchSpecificTexture)
                Trace.TraceWarning("Could not find light graphic {0} at {1}", textureName, Path.Combine(Path.GetDirectoryName(Car.WagFilePath), textureName));
            if (File.Exists(Path.Combine(Viewer.ContentPath, textureName)))
                return Path.Combine(Viewer.ContentPath, textureName);
            return Path.Combine(Viewer.ContentPath, "LightGlow.png");
        }

        void UpdateActiveLightCone()
        {
            var newLightCone = (LightConePrimitive)LightPrimitives.FirstOrDefault(lm => lm is LightConePrimitive && lm.Enabled);

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
            if (ActiveLightCone != null)
                Console.WriteLine("Old headlight: index = {0}, fade-in = {1:F1}, fade-out = {2:F1}, position = {3}, angle = {4:F1}, radius = {5:F1}", ActiveLightCone.Light.Index, ActiveLightCone.Light.FadeIn, ActiveLightCone.Light.FadeOut, ActiveLightCone.Light.States[0].Position, ActiveLightCone.Light.States[0].Angle, ActiveLightCone.Light.States[0].Radius);
            else
                Console.WriteLine("Old headlight: <none>");
            if (newLightCone != null)
                Console.WriteLine("New headlight: index = {0}, fade-in = {1:F1}, fade-out = {2:F1}, position = {3}, angle = {4:F1}, radius = {5:F1}", newLightCone.Light.Index, newLightCone.Light.FadeIn, newLightCone.Light.FadeOut, newLightCone.Light.States[0].Position, newLightCone.Light.States[0].Angle, newLightCone.Light.States[0].Radius);
            else
                Console.WriteLine("New headlight: <none>");
            if ((ActiveLightCone != null) || (newLightCone != null))
            {
                Console.WriteLine("Headlight changed from {0} to {1}, fade-in = {2:F1}, fade-out = {3:F1}", ActiveLightCone != null ? ActiveLightCone.Light.Index.ToString() : "<none>", newLightCone != null ? newLightCone.Light.Index.ToString() : "<none>", LightConeFadeIn, LightConeFadeOut);
                Console.WriteLine();
            }
#endif
            // Keep the light cone active while fading out
            if (newLightCone != null || LightConeFadeOut == 0)
                ActiveLightCone = newLightCone;
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (UpdateState())
            {
                foreach (var lightPrimitive in LightPrimitives)
                    lightPrimitive.UpdateState(this);
#if DEBUG_LIGHT_STATES
                Console.WriteLine();
#endif
                UpdateActiveLightCone();
            }

            foreach (var lightPrimitive in LightPrimitives)
                lightPrimitive.PrepareFrame(frame, elapsedTime);

            var trainCarShape = (CarViewer as MSTSWagonViewer).TrainCarShape;

            int dTileX = trainCarShape.Location.TileX - Viewer.Camera.TileX;
            int dTileZ = trainCarShape.Location.TileZ - Viewer.Camera.TileZ;
            Matrix xnaDTileTranslation = Matrix.CreateTranslation(dTileX * 2048, 0, -dTileZ * 2048);  // object is offset from camera this many tiles
            xnaDTileTranslation = trainCarShape.Location.XNAMatrix * xnaDTileTranslation;

            Vector3 mstsLocation = new Vector3(xnaDTileTranslation.Translation.X, xnaDTileTranslation.Translation.Y, -xnaDTileTranslation.Translation.Z);

            if (trainCarShape.Hierarchy.Length == 0)
                return;

            // Calculate XNA matrix for shape file objects by offsetting from car's location
            // The new List<int> is intentional, this allows the dictionary to be changed while iterating
            int maxDepth = trainCarShape.Hierarchy.Max();
            foreach (int index in new List<int>(ShapeXNATranslations.Keys))
            {
                Matrix res = trainCarShape.XNAMatrices[index];
                int hIndex = trainCarShape.Hierarchy[index];

                int i = 0;

                // Transform the matrix repeatedly for all of its parents
                while (hIndex > -1 && hIndex < trainCarShape.Hierarchy.Length && i < maxDepth)
                {
                    res = res * trainCarShape.XNAMatrices[hIndex];
                    // Prevent potential infinite loop due to faulty hierarchy definition
                    if (hIndex != trainCarShape.Hierarchy[hIndex])
                        hIndex = trainCarShape.Hierarchy[hIndex];
                    else
                        break;

                    i++;
                }

                ShapeXNATranslations[index] = res * xnaDTileTranslation;
            }

            float objectRadius = 20; // Even more arbitrary.
            float objectViewingDistance = Viewer.Settings.ViewingDistance; // Arbitrary.
            if (Viewer.Camera.CanSee(mstsLocation, objectRadius, objectViewingDistance))
                foreach (var lightPrimitive in LightPrimitives)
                    if ((lightPrimitive.Enabled || lightPrimitive.FadeOut) && lightPrimitive is LightGlowPrimitive)
                    {
                        if (ShapeXNATranslations.TryGetValue(lightPrimitive.Light.ShapeIndex, out Matrix lightMatrix))
                            frame.AddPrimitive((lightPrimitive as LightGlowPrimitive).SpecificGlowMaterial, lightPrimitive, RenderPrimitiveGroup.Lights, ref lightMatrix);
                        else
                            frame.AddPrimitive((lightPrimitive as LightGlowPrimitive).SpecificGlowMaterial, lightPrimitive, RenderPrimitiveGroup.Lights, ref xnaDTileTranslation);
                    }

#if DEBUG_LIGHT_CONE
            foreach (var lightPrimitive in LightPrimitives)
                if (lightPrimitive.Enabled || lightPrimitive.FadeOut)
                    if (lightPrimitive is LightConePrimitive)
                            frame.AddPrimitive(LightConeMaterial, lightPrimitive, RenderPrimitiveGroup.Lights, ref xnaDTileTranslation);
#endif

            if (HasLightCone && ActiveLightCone != null)
            {
                if (ActiveLightCone.StaticLight == null)
                {
                    int coneIndex = ActiveLightCone.Light.ShapeIndex;
                    
                    LightConePosition = Vector3.Transform(Vector3.Lerp(ActiveLightCone.Position1, ActiveLightCone.Position2, ActiveLightCone.Fade.Y), ShapeXNATranslations[coneIndex]);
                    LightConeDirection = Vector3.Transform(Vector3.Lerp(ActiveLightCone.Direction1, ActiveLightCone.Direction2, ActiveLightCone.Fade.Y), ShapeXNATranslations[coneIndex]);
                    LightConeDirection -= ShapeXNATranslations[coneIndex].Translation;
                    LightConeDirection.Normalize();
                    LightConeDistance = 4 * MathHelper.Lerp(ActiveLightCone.Distance1, ActiveLightCone.Distance2, ActiveLightCone.Fade.Y);
                    LightConeOuterAngle = MathHelper.Lerp(ActiveLightCone.Angle1, ActiveLightCone.Angle2, ActiveLightCone.Fade.Y);
                    var lightConeColor = Vector4.Lerp(ActiveLightCone.Color1, ActiveLightCone.Color2, ActiveLightCone.Fade.Y);
                    LightConeColor = new Vector3(lightConeColor.X, lightConeColor.Y, lightConeColor.Z) * lightConeColor.W;

                    frame.AddLight(LightMode.Headlight, LightConePosition, LightConeDirection, LightConeColor, RenderFrame.HeadLightIntensity, LightConeDistance, 0, LightConeOuterAngle, ActiveLightCone.Fade.X, false);
                }
                else
                {
                    // Only set the properties, the light is added in frame.AddAutoPrimitive()
                    ActiveLightCone.StaticLight.IntensityX = ActiveLightCone.Fade.X;
                }
            }
        }

        [CallOnThread("Loader")]
        public void Mark()
        {
            LightGlowMaterial.Mark();
            LightConeMaterial.Mark();
            foreach (var lightPrimitive in LightPrimitives)
                if (lightPrimitive is LightGlowPrimitive && lightPrimitive.Light.Graphic != null)
                {
                    (lightPrimitive as LightGlowPrimitive).SpecificGlowMaterial.Mark();
                 }
        }

        public static void CalculateLightCone(LightState lightState, out Vector3 position, out Vector3 direction, out float angle, out float radius, out float distance, out Vector4 color)
        {
            position = lightState.Position;
            position.Z *= -1;
            direction = Vector3.Transform(Vector3.Transform(-Vector3.UnitZ, Matrix.CreateRotationX(MathHelper.ToRadians(-lightState.Elevation.Y))), Matrix.CreateRotationY(MathHelper.ToRadians(-lightState.Azimuth.Y)));
            angle = MathHelper.ToRadians(lightState.Angle);
            radius = lightState.Radius / 2;
            distance = (float)(radius / Math.Sin(angle));
            color = lightState.Color.ToVector4();
        }

#if DEBUG_LIGHT_STATES
        public const string PrimitiveStateLabel = "Index       Enabled     Type        Headlight   Unit        Penalty     Control     Service     Time        Weather     Coupling  ";
        public const string PrimitiveStateFormat = "{0,-10  }  {1,-10   }  {2,-10   }  {3,-10   }  {4,-10   }  {5,-10   }  {6,-10   }  {7,-10   }  {8,-10   }  {9,-10   }  {10,-10  }";
#endif

        bool UpdateState()
        {
            // No need to update lights if there are none
            if (Car.Lights == null)
                return false;

			Debug.Assert(Viewer.PlayerTrain.LeadLocomotive == Viewer.PlayerLocomotive ||Viewer.PlayerTrain.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING || Viewer.PlayerTrain.Autopilot ||
                Viewer.PlayerTrain.TrainType == Train.TRAINTYPE.REMOTE || Viewer.PlayerTrain.TrainType == Train.TRAINTYPE.STATIC, "PlayerTrain.LeadLocomotive must be PlayerLocomotive.");
			var leadLocomotiveCar = Car.Train?.LeadLocomotive; // Note: Will return null for AI trains, this is intended behavior
            var leadLocomotive = leadLocomotiveCar as MSTSLocomotive;

            // There are a lot of conditions now! IgnoredConditions[] stores which conditions are ignored, allowing shortcutting of many of these calculations
            // Should prevent some unneeded computation, but is a little messy. May revise in the future
            
            // Headlight
			int newTrainHeadlight = !Car.Lights.IgnoredConditions[0] ? (Car.Train?.TrainType != Train.TRAINTYPE.STATIC ? (leadLocomotiveCar != null ? leadLocomotiveCar.Headlight : 2) : 0) : 0;
            // Unit
            bool locomotiveFlipped = leadLocomotiveCar != null && leadLocomotiveCar.Flipped;
            bool locomotiveReverseCab = leadLocomotive != null && leadLocomotive.UsingRearCab;
            bool newCarIsReversed = Car.Flipped ^ locomotiveFlipped ^ locomotiveReverseCab;
            bool newCarIsFirst = !Car.Lights.IgnoredConditions[1] && (locomotiveFlipped ^ locomotiveReverseCab ? Car.Train?.LastCar : Car.Train?.FirstCar) == Car;
            bool newCarIsLast = !Car.Lights.IgnoredConditions[1] && (locomotiveFlipped ^ locomotiveReverseCab ? Car.Train?.FirstCar : Car.Train?.LastCar) == Car;
            // Penalty
			bool newPenalty = !Car.Lights.IgnoredConditions[2] && Car.Train != null && Car.Train.TrainType != Train.TRAINTYPE.AI
                && leadLocomotive != null && leadLocomotive.TrainBrakeController.EmergencyBraking;
            // Control
            bool newCarIsPlayer = !Car.Lights.IgnoredConditions[3] && Car.Train != null && (Car.Train == Viewer.PlayerTrain || Car.Train.TrainType == Train.TRAINTYPE.REMOTE);
            // Service - if a player or AI train, then will considered to be in service, loose consists will not be considered to be in service.
            bool newCarInService = !Car.Lights.IgnoredConditions[4] && Car.Train != null
                && (Car.Train == Viewer.PlayerTrain || Car.Train.TrainType == Train.TRAINTYPE.REMOTE || Car.Train.TrainType == Train.TRAINTYPE.AI);
            // Time of day
            bool newIsDay = false;
            if (!Car.Lights.IgnoredConditions[5])
            {
                if (Viewer.Settings.UseMSTSEnv == false)
                    newIsDay = Viewer.World.Sky.SolarDirection.Y > 0;
                else
                    newIsDay = Viewer.World.MSTSSky.mstsskysolarDirection.Y > 0;

            }
            // Weather
            WeatherType newWeather = !Car.Lights.IgnoredConditions[6] ? Viewer.Simulator.WeatherType : WeatherType.Clear;
            // Coupling
            bool newCarCoupledFront = !Car.Lights.IgnoredConditions[7] && Car.Train != null && (Car.Train.Cars.Count > 1) && ((Car.Flipped ? Car.Train.LastCar : Car.Train.FirstCar) != Car);
            bool newCarCoupledRear = !Car.Lights.IgnoredConditions[7] && Car.Train != null && (Car.Train.Cars.Count > 1) && ((Car.Flipped ? Car.Train.FirstCar : Car.Train.LastCar) != Car);
            // Battery
            bool newCarBatteryOn = !Car.Lights.IgnoredConditions[8] && Car is MSTSWagon wagon ? wagon.PowerSupply?.BatteryState == PowerSupplyState.PowerOn : true;
            // Friction brakes, activation force is arbitrary
            bool newBrakeOn = !Car.Lights.IgnoredConditions[9] && Car.BrakeForceN > 250.0f;
            // Reverser: -1: reverse, 0: within 10% of neutral, 1: forwards. Automatically swaps if this car is reversed
            int newReverserState = (!Car.Lights.IgnoredConditions[10] && Car.Train != null) ? ((Car.Train.MUDirection == Direction.N || Math.Abs(Car.Train.MUReverserPercent) < 10.0f) ? 0 : 
                Car.Train.MUDirection == Direction.Forward ? 1 : -1) * (Car.Flipped ? -1 : 1) : 0;
            // Passenger doors
            bool newLeftDoorOpen = !Car.Lights.IgnoredConditions[11] && Car.Train?.DoorState(DoorSide.Left) != DoorState.Closed;
            bool newRightDoorOpen = !Car.Lights.IgnoredConditions[11] && Car.Train?.DoorState(DoorSide.Right) != DoorState.Closed;
            // AI trains don't have a lead locomotive, but the upcoming lighting calculations want a lead locomotive, try to determine a lead locomotive to use
            if (leadLocomotive == null && Car.Train != null)
            {
                // If first car is flipped, the 'lead' vehicle is actually at the rear
                if (Car.Train.FirstCar.Flipped && Car.Train.LastCar is MSTSLocomotive)
                    leadLocomotive = Car.Train.LastCar as MSTSLocomotive;
                else if (Car.Train.FirstCar is MSTSLocomotive)
                    leadLocomotive = Car.Train.FirstCar as MSTSLocomotive;
            }
            // Horn and bell (for flashing ditch lights)
            bool newHornOn = !Car.Lights.IgnoredConditions[12] && leadLocomotive != null && leadLocomotive.HornRecent;
            bool newBellOn = !Car.Lights.IgnoredConditions[13] && leadLocomotive != null && leadLocomotive.BellRecent;
            // Multiple unit configuration, -1: this loco is the lead loco, 0: this loco is not directly connected to the lead loco (distributed power), 1: this loco is directly connected to the lead loco
            int newMU = !Car.Lights.IgnoredConditions[14] ? (Car is MSTSLocomotive loco && leadLocomotive != null && loco.DPUnitID == leadLocomotive.DPUnitID ? loco == leadLocomotive ? -1 : 1 : 0) : 0;

            if (
                (TrainHeadlight != newTrainHeadlight) ||
                (CarIsReversed != newCarIsReversed) ||
                (CarIsFirst != newCarIsFirst) ||
                (CarIsLast != newCarIsLast) ||
                (Penalty != newPenalty) ||
                (CarIsPlayer != newCarIsPlayer) ||
                (CarInService != newCarInService) ||
                (IsDay != newIsDay) ||
                (Weather != newWeather) ||
                (CarCoupledFront != newCarCoupledFront) ||
                (CarCoupledRear != newCarCoupledRear) ||
                (CarBatteryOn != newCarBatteryOn) ||
                (BrakeOn != newBrakeOn) ||
                (ReverserState != newReverserState) ||
                (LeftDoorOpen != newLeftDoorOpen) ||
                (RightDoorOpen != newRightDoorOpen) ||
                (HornOn != newHornOn) ||
                (BellOn != newBellOn) ||
                (MU != newMU)
                )
            {
                TrainHeadlight = newTrainHeadlight;
                CarIsReversed = newCarIsReversed;
                CarIsFirst = newCarIsFirst;
                CarIsLast = newCarIsLast;
                Penalty = newPenalty;
                CarIsPlayer = newCarIsPlayer;
                CarInService = newCarInService;
                IsDay = newIsDay;
                Weather = newWeather;
                CarCoupledFront = newCarCoupledFront;
                CarCoupledRear = newCarCoupledRear;
                CarBatteryOn = newCarBatteryOn;
                BrakeOn = newBrakeOn;
                ReverserState = newReverserState;
                LeftDoorOpen = newLeftDoorOpen;
                RightDoorOpen = newRightDoorOpen;
                HornOn = newHornOn;
                BellOn = newBellOn;
                MU = newMU;

#if DEBUG_LIGHT_STATES
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("LightViewer: {0} {1} {2:D}{3}:{4}{5}{6}{7}{8}{9}{10}{11}{12}{13}{14}",
                    Car.Train != null ? Car.Train.FrontTDBTraveller.WorldLocation : Car.WorldPosition.WorldLocation, Car.Train != null ? "train car" : "car", Car.Train != null ? Car.Train.Cars.IndexOf(Car) : 0, Car.Flipped ? " (flipped)" : "",
                    TrainHeadlight == 2 ? " HL=Bright" : TrainHeadlight == 1 ? " HL=Dim" : "",
                    CarIsReversed ? " Reversed" : "",
                    CarIsFirst ? " First" : "",
                    CarIsLast ? " Last" : "",
                    Penalty ? " Penalty" : "",
                    CarIsPlayer ? " Player" : " AI",
                    CarInService ? " Service" : "",
                    IsDay ? "" : " Night",
                    Weather == WeatherType.Snow ? " Snow" : Weather == WeatherType.Rain ? " Rain" : "",
                    CarCoupledFront ? " CoupledFront" : "",
                    CarCoupledRear ? " CoupledRear" : "",
                    CarLowVoltagePowerSupplyOn ? " LowVoltageOn" : "");
                if (Car.Lights != null)
                {
                    Console.WriteLine();
                    Console.WriteLine(PrimitiveStateLabel);
                    Console.WriteLine(new String('=', PrimitiveStateLabel.Length));
                }
#endif

                return true;
            }
            return false;
        }
    }

    public abstract class LightPrimitive : RenderPrimitive
    {
        public Light Light;
        public StaticLight StaticLight;
        public bool Enabled;
        public Vector2 Fade;
        public bool FadeIn;
        public bool FadeOut;
        protected float FadeTime;
        protected int State;
        protected int StateCount;
        protected float StateTime;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public LightPrimitive(Light light, StaticLight staticLight)
        {
            Light = light;
            StaticLight = staticLight;
            StateCount = Math.Max(Light.Cycle ? 2 * Light.States.Count - 2 : Light.States.Count, 1);
            UpdateStates(State, (State + 1) % StateCount);
        }

        protected void SetUpTransitions(Action<int, int, int> transitionHandler)
        {
#if DEBUG_LIGHT_TRANSITIONS
            Console.WriteLine();
            Console.WriteLine("LightPrimitive transitions:");
#endif
            if (Light.Cycle)
            {
                for (var i = 0; i < Light.States.Count - 1; i++)
                    transitionHandler(i, i, i + 1);
                for (var i = Light.States.Count - 1; i > 0; i--)
                    transitionHandler((Light.States.Count * 2 - 2) - i, i, i - 1);
            }
            else
            {
                for (var i = 0; i < Light.States.Count; i++)
                    transitionHandler(i, i, (i + 1) % Light.States.Count);
            }
#if DEBUG_LIGHT_TRANSITIONS
            Console.WriteLine();
#endif
        }

        internal void UpdateState(LightViewer lightViewer)
        {
            var oldEnabled = Enabled;
            Enabled = false;

            // Assume light is unconditionally turned on if there are no conditions
            if (Light.Conditions.Count == 0)
                Enabled = true;

            foreach (var condition in Light.Conditions)
            {
                bool thisEnabled = true;

                if (thisEnabled && condition.Headlight != LightHeadlightCondition.Ignore)
                {
                    switch (condition.Headlight)
                    {
                        case LightHeadlightCondition.Off:       thisEnabled &= lightViewer.TrainHeadlight == 0; break;
                        case LightHeadlightCondition.Dim:       thisEnabled &= lightViewer.TrainHeadlight == 1; break;
                        case LightHeadlightCondition.Bright:    thisEnabled &= lightViewer.TrainHeadlight == 2; break;
                        case LightHeadlightCondition.DimBright: thisEnabled &= lightViewer.TrainHeadlight >= 1; break;
                        case LightHeadlightCondition.OffDim:    thisEnabled &= lightViewer.TrainHeadlight <= 1; break;
                        case LightHeadlightCondition.OffBright: thisEnabled &= lightViewer.TrainHeadlight != 1; break;
                        default: thisEnabled = false; break;
                    }
                }
                if (thisEnabled && condition.Unit != LightUnitCondition.Ignore)
                {
                    switch (condition.Unit)
                    {
                        case LightUnitCondition.Middle:         thisEnabled &= !lightViewer.CarIsFirst && !lightViewer.CarIsLast; break;
                        case LightUnitCondition.First:          thisEnabled &= lightViewer.CarIsFirst && !lightViewer.CarIsReversed; break;
                        case LightUnitCondition.Last:           thisEnabled &= lightViewer.CarIsLast && !lightViewer.CarIsReversed; break;
                        case LightUnitCondition.LastRev:        thisEnabled &= lightViewer.CarIsLast && lightViewer.CarIsReversed; break;
                        case LightUnitCondition.FirstRev:       thisEnabled &= lightViewer.CarIsFirst && lightViewer.CarIsReversed; break;
                        default: thisEnabled = false; break;
                    }
                }
                if (thisEnabled && condition.Penalty != LightPenaltyCondition.Ignore)
                {
                    switch (condition.Penalty)
                    {
                        case LightPenaltyCondition.No:          thisEnabled &= !lightViewer.Penalty; break;
                        case LightPenaltyCondition.Yes:         thisEnabled &= lightViewer.Penalty; break;
                        default: thisEnabled = false; break;
                    }
                }
                if (thisEnabled && condition.Control != LightControlCondition.Ignore)
                {
                    switch (condition.Control)
                    {
                        case LightControlCondition.AI:          thisEnabled &= !lightViewer.CarIsPlayer; break;
                        case LightControlCondition.Player:      thisEnabled &= lightViewer.CarIsPlayer; break;
                        default: thisEnabled = false; break;
                    }
                }
                if (thisEnabled && condition.Service != LightServiceCondition.Ignore)
                {
                    switch (condition.Service)
                    {
                        case LightServiceCondition.No:          thisEnabled &= !lightViewer.CarInService; break;
                        case LightServiceCondition.Yes:         thisEnabled &= lightViewer.CarInService; break;
                        default: thisEnabled = false; break;
                    }
                }
                if (thisEnabled && condition.TimeOfDay != LightTimeOfDayCondition.Ignore)
                {
                    switch (condition.TimeOfDay)
                    {
                        case LightTimeOfDayCondition.Day:       thisEnabled &= lightViewer.IsDay; break;
                        case LightTimeOfDayCondition.Night:     thisEnabled &= !lightViewer.IsDay; break;
                        default: thisEnabled = false; break;
                    }
                }
                if (thisEnabled && condition.Weather != LightWeatherCondition.Ignore)
                {
                    switch (condition.Weather)
                    {
                        case LightWeatherCondition.Clear:       thisEnabled &= lightViewer.Weather == WeatherType.Clear; break;
                        case LightWeatherCondition.Rain:        thisEnabled &= lightViewer.Weather == WeatherType.Rain; break;
                        case LightWeatherCondition.Snow:        thisEnabled &= lightViewer.Weather == WeatherType.Snow; break;
                        default: thisEnabled = false; break;
                    }
                }
                if (thisEnabled && condition.Coupling != LightCouplingCondition.Ignore)
                {
                    switch (condition.Coupling)
                    {
                        case LightCouplingCondition.Front:      thisEnabled &= lightViewer.CarCoupledFront && !lightViewer.CarCoupledRear; break;
                        case LightCouplingCondition.Rear:       thisEnabled &= !lightViewer.CarCoupledFront && lightViewer.CarCoupledRear; break;
                        case LightCouplingCondition.Both:       thisEnabled &= lightViewer.CarCoupledFront && lightViewer.CarCoupledRear; break;
                        default: thisEnabled = false; break;
                    }
                }
                if (thisEnabled && condition.Battery != LightBatteryCondition.Ignore)
                {
                    switch (condition.Battery)
                    {
                        case LightBatteryCondition.On:          thisEnabled &= lightViewer.CarBatteryOn; break;
                        case LightBatteryCondition.Off:         thisEnabled &= !lightViewer.CarBatteryOn; break;
                        default: thisEnabled = false; break;
                    }
                }
                if (thisEnabled && condition.Brake != LightBrakeCondition.Ignore)
                {
                    switch (condition.Brake)
                    {
                        case LightBrakeCondition.Released:      thisEnabled &= !lightViewer.BrakeOn; break;
                        case LightBrakeCondition.Applied:       thisEnabled &= lightViewer.BrakeOn; break;
                        default: thisEnabled = false; break;
                    }
                }
                if (thisEnabled && condition.Reverser != LightReverserCondition.Ignore)
                {
                    switch (condition.Reverser)
                    {
                        case LightReverserCondition.Forward:    thisEnabled &= lightViewer.ReverserState == 1; break;
                        case LightReverserCondition.Reverse:    thisEnabled &= lightViewer.ReverserState == -1; break;
                        case LightReverserCondition.Neutral:    thisEnabled &= lightViewer.ReverserState == 0; break;
                        case LightReverserCondition.ForwardReverse: thisEnabled &= lightViewer.ReverserState != 0; break;
                        case LightReverserCondition.ForwardNeutral: thisEnabled &= lightViewer.ReverserState >= 0; break;
                        case LightReverserCondition.ReverseNeutral: thisEnabled &= lightViewer.ReverserState <= 0; break;
                        default: thisEnabled = false; break;
                    }
                }
                if (thisEnabled && condition.Doors != LightDoorsCondition.Ignore)
                {
                    switch (condition.Doors)
                    {
                        case LightDoorsCondition.Closed:        thisEnabled &= !lightViewer.RightDoorOpen && !lightViewer.LeftDoorOpen; break;
                        case LightDoorsCondition.Left:          thisEnabled &= lightViewer.LeftDoorOpen; break;
                        case LightDoorsCondition.Right:         thisEnabled &= lightViewer.RightDoorOpen; break;
                        case LightDoorsCondition.Both:          thisEnabled &= lightViewer.RightDoorOpen && lightViewer.LeftDoorOpen; break;
                        case LightDoorsCondition.LeftRight:     thisEnabled &= lightViewer.RightDoorOpen || lightViewer.LeftDoorOpen; break;
                        default: thisEnabled = false; break;
                    }
                }
                if (thisEnabled && condition.Horn != LightHornCondition.Ignore)
                {
                    switch (condition.Horn)
                    {
                        case LightHornCondition.Off:            thisEnabled &= !lightViewer.HornOn; break;
                        case LightHornCondition.Sounding:       thisEnabled &= lightViewer.HornOn; break;
                        default: thisEnabled = false; break;
                    }
                }
                if (thisEnabled && condition.Bell != LightBellCondition.Ignore)
                {
                    switch (condition.Bell)
                    {
                        case LightBellCondition.Off:            thisEnabled &= !lightViewer.BellOn; break;
                        case LightBellCondition.Ringing:        thisEnabled &= lightViewer.BellOn; break;
                        default: thisEnabled = false; break;
                    }
                }
                if (thisEnabled && condition.MU != LightMUCondition.Ignore)
                {
                    switch (condition.MU)
                    {
                        case LightMUCondition.Lead:             thisEnabled &= lightViewer.MU == -1; break;
                        case LightMUCondition.Local:            thisEnabled &= lightViewer.MU != 0; break;
                        case LightMUCondition.Remote:           thisEnabled &= lightViewer.MU == 0; break;
                        default: thisEnabled = false; break;
                    }
                }

                // If ANY set of conditions are enabled, the entire thing is enabled
                Enabled |= thisEnabled;

                // No need to waste time checking other conditions once one is enabled
                if (Enabled)
                    break;
            }

            if (oldEnabled != Enabled)
            {
                FadeIn = Enabled;
                FadeOut = !Enabled;
                FadeTime = 0;
            }

#if DEBUG_LIGHT_STATES
            Console.WriteLine(LightViewer.PrimitiveStateFormat, Light.Index, Enabled, Light.Type, Light.Headlight, Light.Unit, Light.Penalty, Light.Control, Light.Service, Light.TimeOfDay, Light.Weather, Light.Coupling);
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
                    UpdateStates(State, (State + 1) % StateCount);
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
                Fade.X = 1 - FadeTime / Light.FadeOut;
                if (Fade.X < 0)
                {
                    FadeOut = false;
                    Fade.X = 0;
                }
            }
        }

        protected virtual void UpdateStates(int stateIndex1, int stateIndex2)
        {
        }
    }

    public class LightGlowPrimitive : LightPrimitive
    {
        static VertexDeclaration VertexDeclaration;
        VertexBuffer VertexBuffer;
        static IndexBuffer IndexBuffer;
        public Material SpecificGlowMaterial;

        public LightGlowPrimitive(LightViewer lightViewer, RenderProcess renderProcess, Light light, StaticLight staticLight)
            : base(light, staticLight)
        {
            Debug.Assert(light.Type == LightType.Glow, "LightGlowPrimitive is only for LightType.Glow lights.");

            if (VertexDeclaration == null)
                VertexDeclaration = new VertexDeclaration(LightGlowVertex.SizeInBytes, LightGlowVertex.VertexElements);
            if (VertexBuffer == null)
            {
                var vertexData = new LightGlowVertex[6 * StateCount];
                SetUpTransitions((state, stateIndex1, stateIndex2) =>
                {
                    var state1 = Light.States[stateIndex1];
                    var state2 = Light.States[stateIndex2];

#if DEBUG_LIGHT_TRANSITIONS
                    Console.WriteLine("    Transition {0} is from state {1} to state {2} over {3:F1}s", state, stateIndex1, stateIndex2, state1.Duration);
#endif

                    // FIXME: Is conversion of "azimuth" to a normal right?

                    var position1 = state1.Position; position1.Z *= -1;
                    var normal1 = Vector3.Transform(Vector3.Transform(-Vector3.UnitZ, Matrix.CreateRotationX(MathHelper.ToRadians(-state1.Elevation.Y))), Matrix.CreateRotationY(MathHelper.ToRadians(-state1.Azimuth.Y)));
                    var color1 = state1.Color.ToVector4();

                    var position2 = state2.Position; position2.Z *= -1;
                    var normal2 = Vector3.Transform(Vector3.Transform(-Vector3.UnitZ, Matrix.CreateRotationX(MathHelper.ToRadians(-state2.Elevation.Y))), Matrix.CreateRotationY(MathHelper.ToRadians(-state2.Azimuth.Y)));
                    var color2 = state2.Color.ToVector4();

                    vertexData[6 * state + 0] = new LightGlowVertex(new Vector2(1, 1), position1, position2, normal1, normal2, color1, color2, state1.Radius, state2.Radius);
                    vertexData[6 * state + 1] = new LightGlowVertex(new Vector2(0, 0), position1, position2, normal1, normal2, color1, color2, state1.Radius, state2.Radius);
                    vertexData[6 * state + 2] = new LightGlowVertex(new Vector2(1, 0), position1, position2, normal1, normal2, color1, color2, state1.Radius, state2.Radius);
                    vertexData[6 * state + 3] = new LightGlowVertex(new Vector2(1, 1), position1, position2, normal1, normal2, color1, color2, state1.Radius, state2.Radius);
                    vertexData[6 * state + 4] = new LightGlowVertex(new Vector2(0, 1), position1, position2, normal1, normal2, color1, color2, state1.Radius, state2.Radius);
                    vertexData[6 * state + 5] = new LightGlowVertex(new Vector2(0, 0), position1, position2, normal1, normal2, color1, color2, state1.Radius, state2.Radius);
                });
                VertexBuffer = new VertexBuffer(renderProcess.GraphicsDevice, VertexDeclaration, vertexData.Length, BufferUsage.WriteOnly);
                VertexBuffer.SetData(vertexData);
            }
            if (IndexBuffer == null)
            {
                var indexData = new short[] {
                    0, 1, 2, 3, 4, 5
                };
                IndexBuffer = new IndexBuffer(renderProcess.GraphicsDevice, typeof(short), indexData.Length, BufferUsage.WriteOnly);
                IndexBuffer.SetData(indexData);
            }

            UpdateState(lightViewer);
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.SetVertexBuffer(VertexBuffer);
            graphicsDevice.Indices = IndexBuffer;
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, baseVertex: 6 * State, startIndex: 0, primitiveCount: 2);
        }
    }

    public class StaticLight
    {
        public string Name;
        public LightMode Type;
        public Vector3 Color;
        public float Intensity;
        public float Range;
        public float InnerConeAngle;
        public float OuterConeAngle;

        public Vector3 ColorX = Vector3.One;
        public float IntensityX = 1;
        public float RangeX = 1;
        public float InnerConeAngleX = 1;
        public float OuterConeAngleX = 1;

        public string ManagedName;

        public Matrix WorldMatrix;
    }
    
    struct LightGlowVertex
    {
        public Vector3 PositionO;
        public Vector3 PositionT;
        public Vector3 NormalO;
        public Vector3 NormalT;
        public Vector4 ColorO;
        public Vector4 ColorT;
        public Vector2 TexCoords;
        public float RadiusO;
        public float RadiusT;

        public LightGlowVertex(Vector2 texCoords, Vector3 position1, Vector3 position2, Vector3 normal1, Vector3 normal2, Vector4 color1, Vector4 color2, float radius1, float radius2)
        {
            PositionO = position1;
            PositionT = position2;
            NormalO = normal1;
            NormalT = normal2;
            ColorO = color1;
            ColorT = color2;
            TexCoords = texCoords;
            RadiusO = radius1;
            RadiusT = radius2;
        }

        public static readonly VertexElement[] VertexElements = {
            new VertexElement(sizeof(float) * 0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(sizeof(float) * (3), VertexElementFormat.Vector3, VertexElementUsage.Position, 1),
            new VertexElement(sizeof(float) * (3 + 3), VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
            new VertexElement(sizeof(float) * (3 + 3 + 3), VertexElementFormat.Vector3, VertexElementUsage.Normal, 1),
            new VertexElement(sizeof(float) * (3 + 3 + 3 + 3), VertexElementFormat.Vector4, VertexElementUsage.Color, 0),
            new VertexElement(sizeof(float) * (3 + 3 + 3 + 3 + 4), VertexElementFormat.Vector4, VertexElementUsage.Color, 1),
            new VertexElement(sizeof(float) * (3 + 3 + 3 + 3 + 4 + 4), VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 0),
        };

        public static int SizeInBytes = sizeof(float) * (3 + 3 + 3 + 3 + 4 + 4 + 4);
    }

    public class LightConePrimitive : LightPrimitive
    {
        const int CircleSegments = 16;

        static VertexDeclaration VertexDeclaration;
        VertexBuffer VertexBuffer;
        static IndexBuffer IndexBuffer;
        static BlendState BlendState_SourceZeroDestOne;

        public LightConePrimitive(LightViewer lightViewer, RenderProcess renderProcess, Light light, StaticLight staticLight)
            : base(light, staticLight)
        {
            Debug.Assert(light.Type == LightType.Cone, "LightConePrimitive is only for LightType.Cone lights.");

            if (VertexDeclaration == null)
                VertexDeclaration = new VertexDeclaration(LightConeVertex.SizeInBytes, LightConeVertex.VertexElements);
            if (VertexBuffer == null)
            {
                var vertexData = new LightConeVertex[(CircleSegments + 2) * StateCount];
                SetUpTransitions((state, stateIndex1, stateIndex2) =>
                {
                    var state1 = Light.States[stateIndex1];
                    var state2 = Light.States[stateIndex2];

#if DEBUG_LIGHT_TRANSITIONS
                    Console.WriteLine("    Transition {0} is from state {1} to state {2} over {3:F1}s", state, stateIndex1, stateIndex2, state1.Duration);
#endif

                    Vector3 position1, position2, direction1, direction2;
                    float angle1, angle2, radius1, radius2, distance1, distance2;
                    Vector4 color1, color2;
                    LightViewer.CalculateLightCone(state1, out position1, out direction1, out angle1, out radius1, out distance1, out color1);
                    LightViewer.CalculateLightCone(state2, out position2, out direction2, out angle2, out radius2, out distance2, out color2);
                    var direction1Right = Vector3.Cross(direction1, Vector3.UnitY);
                    var direction1Up = Vector3.Cross(direction1Right, direction1);
                    var direction2Right = Vector3.Cross(direction2, Vector3.UnitY);
                    var direction2Up = Vector3.Cross(direction2Right, direction2);

                    for (var i = 0; i < CircleSegments; i++)
                    {
                        var a1 = MathHelper.TwoPi * i / CircleSegments;
                        var a2 = MathHelper.TwoPi * (i + 1) / CircleSegments;
                        var v1 = position1 + direction1 * distance1 + direction1Right * (float)(radius1 * Math.Cos(a1)) + direction1Up * (float)(radius1 * Math.Sin(a1));
                        var v2 = position2 + direction2 * distance2 + direction2Right * (float)(radius2 * Math.Cos(a2)) + direction2Up * (float)(radius2 * Math.Sin(a2));
                        vertexData[(CircleSegments + 2) * state + i] = new LightConeVertex(v1, v2, color1, color2);
                    }
                    vertexData[(CircleSegments + 2) * state + CircleSegments + 0] = new LightConeVertex(position1, position2, color1, color2);
                    vertexData[(CircleSegments + 2) * state + CircleSegments + 1] = new LightConeVertex(new Vector3(position1.X, position1.Y, position1.Z - distance1), new Vector3(position2.X, position2.Y, position2.Z - distance2), color1, color2);
                });
                VertexBuffer = new VertexBuffer(renderProcess.GraphicsDevice, VertexDeclaration, vertexData.Length, BufferUsage.WriteOnly);
                VertexBuffer.SetData(vertexData);
            }
            if (IndexBuffer == null)
            {
                var indexData = new short[6 * CircleSegments];
                for (var i = 0; i < CircleSegments; i++)
                {
                    var i2 = (i + 1) % CircleSegments;
                    indexData[6 * i + 0] = (short)(CircleSegments + 0);
                    indexData[6 * i + 1] = (short)i2;
                    indexData[6 * i + 2] = (short)i;
                    indexData[6 * i + 3] = (short)i;
                    indexData[6 * i + 4] = (short)i2;
                    indexData[6 * i + 5] = (short)(CircleSegments + 1);
                }
                IndexBuffer = new IndexBuffer(renderProcess.GraphicsDevice, typeof(short), indexData.Length, BufferUsage.WriteOnly);
                IndexBuffer.SetData(indexData);
            }
            if (BlendState_SourceZeroDestOne == null)
                BlendState_SourceZeroDestOne = new BlendState 
                {
                    ColorSourceBlend = Blend.Zero,
                    ColorDestinationBlend = Blend.One,
                    AlphaSourceBlend = Blend.Zero,
                    AlphaDestinationBlend = Blend.One
                };

            UpdateState(lightViewer);
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.SetVertexBuffer(VertexBuffer);
            graphicsDevice.Indices = IndexBuffer;

#if DEBUG_LIGHT_CONE_FULL
            graphicsDevice.BlendState = Blendstate.AlphaBlend;
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, (CircleSegments + 2) * State, 0, CircleSegments + 2, 0, 2 * CircleSegments);
#else
            graphicsDevice.RasterizerState = RasterizerState.CullClockwise;
            graphicsDevice.DepthStencilState.StencilFunction = CompareFunction.Always;
            graphicsDevice.DepthStencilState.StencilPass = StencilOperation.Increment;
            graphicsDevice.DepthStencilState.DepthBufferFunction = CompareFunction.Greater;
            graphicsDevice.BlendState = BlendState_SourceZeroDestOne;
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, baseVertex: (CircleSegments + 2) * State, startIndex: 0, primitiveCount: 2 * CircleSegments);

            graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            graphicsDevice.DepthStencilState.StencilFunction = CompareFunction.Less;
            graphicsDevice.DepthStencilState.StencilPass = StencilOperation.Zero;
            graphicsDevice.DepthStencilState.DepthBufferFunction = CompareFunction.LessEqual;
            graphicsDevice.BlendState = BlendState.AlphaBlend;
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, baseVertex: (CircleSegments + 2) * State, startIndex: 0, primitiveCount: 2 * CircleSegments);
#endif
        }

        public Vector3 Position1, Position2, Direction1, Direction2;
        public float Angle1, Angle2, Radius1, Radius2, Distance1, Distance2;
        public Vector4 Color1, Color2;

        protected override void UpdateStates(int stateIndex1, int stateIndex2)
        {
            // Cycling light: state index will be set above actual number of states
            if (stateIndex1 >= Light.States.Count)
                stateIndex1 = StateCount - stateIndex1;
            if (stateIndex2 >= Light.States.Count)
                stateIndex2 = StateCount - stateIndex2;

            var state1 = Light.States[stateIndex1];
            var state2 = Light.States[stateIndex2];

            LightViewer.CalculateLightCone(state1, out Position1, out Direction1, out Angle1, out Radius1, out Distance1, out Color1);
            LightViewer.CalculateLightCone(state2, out Position2, out Direction2, out Angle2, out Radius2, out Distance2, out Color2);
        }
    }

    struct LightConeVertex
    {
        public Vector3 PositionO;
        public Vector3 PositionT;
        public Vector4 ColorO;
        public Vector4 ColorT;

        public LightConeVertex(Vector3 position1, Vector3 position2, Vector4 color1, Vector4 color2)
        {
            PositionO = position1;
            PositionT = position2;
            ColorO = color1;
            ColorT = color2;
        }

        public static readonly VertexElement[] VertexElements = {
            new VertexElement(sizeof(float) * 0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(sizeof(float) * (3), VertexElementFormat.Vector3, VertexElementUsage.Position, 1),
            new VertexElement(sizeof(float) * (3 + 3), VertexElementFormat.Vector4, VertexElementUsage.Color, 0),
            new VertexElement(sizeof(float) * (3 + 3 + 4), VertexElementFormat.Vector4, VertexElementUsage.Color, 1),
        };

        public static int SizeInBytes = sizeof(float) * (3 + 3 + 4 + 4);
    }

    public class LightGlowMaterial : Material
    {
        readonly Texture2D LightGlowTexture;

        public LightGlowMaterial(Viewer viewer, string textureName)
            : base(viewer, textureName)
        {
            // TODO: This should happen on the loader thread.
            LightGlowTexture = textureName.StartsWith(Viewer.ContentPath, StringComparison.OrdinalIgnoreCase) ? SharedTextureManager.LoadInternal(Viewer.RenderProcess.GraphicsDevice, textureName) : Viewer.TextureManager.Get(textureName);
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            var shader = Viewer.MaterialManager.LightGlowShader;
            shader.CurrentTechnique = shader.Techniques["LightGlow"];
            shader.LightGlowTexture = LightGlowTexture;

            graphicsDevice.BlendState = BlendState.NonPremultiplied;
            graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.LightGlowShader;

            foreach (var pass in shader.CurrentTechnique.Passes)
            {
                foreach (var item in renderItems)
                {
                    // Glow lights were not working properly because farPlaneDistance used by XNASkyProjection is hardcoded at 6100.  So when view distance was greater than 6100, the 
                    // glow lights were unable to render properly.
                    Matrix wvp = item.XNAMatrix * XNAViewMatrix * Viewer.Camera.XnaProjection;
                    shader.SetMatrix(ref wvp);
                    shader.SetFade(((LightPrimitive)item.RenderPrimitive).Fade);
                    pass.Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
        }

        public override bool GetBlending()
        {
            return true;
        }

        public override void Mark()
        {
            Viewer.TextureManager.Mark(LightGlowTexture);
            base.Mark();
        }
    }

    public class LightConeMaterial : Material
    {
        public LightConeMaterial(Viewer viewer)
            : base(viewer, null)
        {
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            var shader = Viewer.MaterialManager.LightConeShader;
            shader.CurrentTechnique = shader.Techniques["LightCone"];

            graphicsDevice.BlendState = BlendState.NonPremultiplied;
            graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
            graphicsDevice.DepthStencilState.StencilEnable = true;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.LightConeShader;

            foreach (var pass in shader.CurrentTechnique.Passes)
            {
                foreach (var item in renderItems)
                {
                    // Light cone was originally using XNASkyProjection, but with no problems.
                    // Switched to Viewer.Camera.XnaProjection to keep the standard since farPlaneDistance used by XNASkyProjection is limited to 6100.
                    Matrix wvp = item.XNAMatrix * XNAViewMatrix * Viewer.Camera.XnaProjection;
                    shader.SetMatrix(ref wvp);
                    shader.SetFade(((LightPrimitive)item.RenderPrimitive).Fade);
                    pass.Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
            graphicsDevice.DepthStencilState.StencilEnable = false;
        }
    }
}
