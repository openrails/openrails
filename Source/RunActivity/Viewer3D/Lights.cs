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
using Orts.Simulation.AIs;
using Orts.Viewer3D.Processes;
using ORTS.Common;
using ORTS.Scripting.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Orts.Viewer3D
{
    public class LightViewer
    {
        readonly Viewer Viewer;
        readonly TrainCar Car;
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
        public bool TrainPowerOn;
        public bool SpecialLightsSelected;

        public SpecialLightsCondition SpecialLights;
        public List<string> SpecialLightsSelection = new List<string>();


        public bool IsLightConeActive { get { return ActiveLightCone != null; } }
        List<LightPrimitive> LightPrimitives = new List<LightPrimitive>();

        LightConePrimitive ActiveLightCone;
        public bool HasLightCone;
        public float LightConeFadeIn;
        public float LightConeFadeOut;
        public Vector3 LightConePosition;
        public Vector3 LightConeDirection;
        public float LightConeDistance;
        public float LightConeMinDotProduct;
        public Vector4 LightConeColor;

        public LightViewer(Viewer viewer, TrainCar car)
        {
            Viewer = viewer;
            Car = car;
            LightGlowMaterial = viewer.MaterialManager.Load("LightGlow");
            LightConeMaterial = viewer.MaterialManager.Load("LightCone");

            UpdateState();
            if (Car.Lights != null)
            {
                foreach (var light in Car.Lights.Lights)
                {
                    switch (light.Type)
                    {
                        case LightType.Glow:
                            LightPrimitives.Add(new LightGlowPrimitive(this, Viewer.RenderProcess, light));
                            break;
                        case LightType.Cone:
                            LightPrimitives.Add(new LightConePrimitive(this, Viewer.RenderProcess, light));
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

            ActiveLightCone = newLightCone;
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (UpdateState())
            {
                foreach (var lightPrimitive in LightPrimitives)
                {
                    lightPrimitive.UpdateState(this);
                }
#if DEBUG_LIGHT_STATES
                Console.WriteLine();
#endif
                UpdateActiveLightCone();
            }

            foreach (var lightPrimitive in LightPrimitives)
                lightPrimitive.PrepareFrame(frame, elapsedTime);

            int dTileX = Car.WorldPosition.TileX - Viewer.Camera.TileX;
            int dTileZ = Car.WorldPosition.TileZ - Viewer.Camera.TileZ;
            Matrix xnaDTileTranslation = Matrix.CreateTranslation(dTileX * 2048, 0, -dTileZ * 2048);  // object is offset from camera this many tiles
            xnaDTileTranslation = Car.WorldPosition.XNAMatrix * xnaDTileTranslation;

            Vector3 mstsLocation = new Vector3(xnaDTileTranslation.Translation.X, xnaDTileTranslation.Translation.Y, -xnaDTileTranslation.Translation.Z);

            float objectRadius = 20; // Even more arbitrary.
            float objectViewingDistance = Viewer.Settings.ViewingDistance; // Arbitrary.
            if (Viewer.Camera.CanSee(mstsLocation, objectRadius, objectViewingDistance))
                foreach (var lightPrimitive in LightPrimitives)
                    if (lightPrimitive.Enabled || lightPrimitive.FadeOut)
                        if (lightPrimitive is LightGlowPrimitive)
                            frame.AddPrimitive(LightGlowMaterial, lightPrimitive, RenderPrimitiveGroup.Lights, ref xnaDTileTranslation);

#if DEBUG_LIGHT_CONE
            foreach (var lightPrimitive in LightPrimitives)
                if (lightPrimitive.Enabled || lightPrimitive.FadeOut)
                    if (lightPrimitive is LightConePrimitive)
                            frame.AddPrimitive(LightConeMaterial, lightPrimitive, RenderPrimitiveGroup.Lights, ref xnaDTileTranslation);
#endif

            // Set the active light cone info for the material code.
            if (HasLightCone && ActiveLightCone != null)
            {
                LightConePosition = Vector3.Transform(Vector3.Lerp(ActiveLightCone.Position1, ActiveLightCone.Position2, ActiveLightCone.Fade.Y), xnaDTileTranslation);
                LightConeDirection = Vector3.Transform(Vector3.Lerp(ActiveLightCone.Direction1, ActiveLightCone.Direction2, ActiveLightCone.Fade.Y), Car.WorldPosition.XNAMatrix);
                LightConeDirection -= Car.WorldPosition.XNAMatrix.Translation;
                LightConeDirection.Normalize();
                LightConeDistance = MathHelper.Lerp(ActiveLightCone.Distance1, ActiveLightCone.Distance2, ActiveLightCone.Fade.Y);
                LightConeMinDotProduct = (float)Math.Cos(MathHelper.Lerp(ActiveLightCone.Angle1, ActiveLightCone.Angle2, ActiveLightCone.Fade.Y));
                LightConeColor = Vector4.Lerp(ActiveLightCone.Color1, ActiveLightCone.Color2, ActiveLightCone.Fade.Y);
            }
        }

        [CallOnThread("Loader")]
        public void Mark()
        {
            LightGlowMaterial.Mark();
            LightConeMaterial.Mark();
        }

        public static void CalculateLightCone(LightState lightState, out Vector3 position, out Vector3 direction, out float angle, out float radius, out float distance, out Vector4 color)
        {
            position = lightState.Position;
            position.Z *= -1;
            direction = -Vector3.UnitZ;
            direction = Vector3.Transform(Vector3.Transform(-Vector3.UnitZ, Matrix.CreateRotationX(MathHelper.ToRadians(-lightState.Elevation.Y))), Matrix.CreateRotationY(MathHelper.ToRadians(-lightState.Azimuth.Y)));
            angle = MathHelper.ToRadians(lightState.Angle) / 2;
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
			Debug.Assert(Viewer.PlayerTrain.LeadLocomotive == Viewer.PlayerLocomotive ||Viewer.PlayerTrain.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING ||
                Viewer.PlayerTrain.TrainType == Train.TRAINTYPE.REMOTE || Viewer.PlayerTrain.TrainType == Train.TRAINTYPE.STATIC, "PlayerTrain.LeadLocomotive must be PlayerLocomotive.");
			var locomotive = Car.Train != null && Car.Train.IsActualPlayerTrain ? Viewer.PlayerLocomotive : null;
            if (locomotive == null && Car.Train != null && Car.Train.TrainType == Train.TRAINTYPE.REMOTE && Car is MSTSLocomotive && (Car as MSTSLocomotive) == Car.Train.LeadLocomotive)
                locomotive = Car.Train.LeadLocomotive;
			var mstsLocomotive = locomotive as MSTSLocomotive;

            // Headlight
			var newTrainHeadlight = locomotive != null ? locomotive.Headlight : Car.Train != null && Car.Train.TrainType != Train.TRAINTYPE.STATIC ? 2 : 0;
            // Unit
			var locomotiveFlipped = locomotive != null && locomotive.Flipped;
			var locomotiveReverseCab = mstsLocomotive != null && mstsLocomotive.UsingRearCab;
            var newCarIsReversed = Car.Flipped ^ locomotiveFlipped ^ locomotiveReverseCab;
			var newCarIsFirst = Car.Train == null || (locomotiveFlipped ^ locomotiveReverseCab ? Car.Train.LastCar : Car.Train.FirstCar) == Car;
			var newCarIsLast = Car.Train == null || (locomotiveFlipped ^ locomotiveReverseCab ? Car.Train.FirstCar : Car.Train.LastCar) == Car;
            // Penalty
			var newPenalty = mstsLocomotive != null && mstsLocomotive.TrainBrakeController.EmergencyBraking;
            // Control
            var newCarIsPlayer = (Car.Train != null && Car.Train == Viewer.PlayerTrain) || (Car.Train != null && Car.Train.TrainType == Train.TRAINTYPE.REMOTE);
            // Service - if a player or AI train, then will considered to be in servie, loose consists will not be considered to be in service.
            var newCarInService = (Car.Train != null && Car.Train == Viewer.PlayerTrain) || (Car.Train != null && Car.Train.TrainType == Train.TRAINTYPE.REMOTE) || (Car.Train != null && Car.Train.TrainType == Train.TRAINTYPE.AI);
            // Time of day
            bool newIsDay = false;
            if (Viewer.Settings.UseMSTSEnv == false)
                newIsDay = Viewer.World.Sky.SolarDirection.Y > 0;
            else
                newIsDay = Viewer.World.MSTSSky.mstsskysolarDirection.Y > 0;
            // Weather
            var newWeather = Viewer.Simulator.WeatherType;
            // Coupling
            var newCarCoupledFront = Car.Train != null && (Car.Train.Cars.Count > 1) && ((Car.Flipped ? Car.Train.LastCar : Car.Train.FirstCar) != Car);
            var newCarCoupledRear = Car.Train != null && (Car.Train.Cars.Count > 1) && ((Car.Flipped ? Car.Train.FirstCar : Car.Train.LastCar) != Car);
            // Battery
            var newCarBatteryOn = Car is MSTSWagon wagon ? wagon.PowerSupply?.BatteryState == PowerSupplyState.PowerOn : true;
            // Train power (for AI trains only, for player train PowerOn is always assumed)
            bool newPowerOn = Car.Train is AITrain AIt ? AIt.PowerState : true;

            // Special Lights
            var newSpecialLights = Car.SpecialLights;
            List<string> newSpecialLightsSelection = new List<string>(Car.SpecialLightSelection);

            // test change in special lights list

            var specialLightsChanged = false;
            if (newSpecialLights != SpecialLights)
            {
                specialLightsChanged = true;
            }
            else if (newSpecialLights == SpecialLightsCondition.Special_additional || newSpecialLights == SpecialLightsCondition.Special_only)
            {
                if (newSpecialLightsSelection.Count != SpecialLightsSelection.Count)
                {
                    specialLightsChanged = true;
                }
                else
                {
                    List<string> tempSpecialLightSelection = new List<string>(newSpecialLightsSelection);
                    for (int i = SpecialLightsSelection.Count - 1; i >= 0; i--)
                    {
                        if (tempSpecialLightSelection.Contains(SpecialLightsSelection[i]))
                        {
                            tempSpecialLightSelection.Remove(tempSpecialLightSelection[i]);
                            SpecialLightsSelection.RemoveAt(i);
                        }
                    }
                    specialLightsChanged = (tempSpecialLightSelection.Count > 0 || SpecialLightsSelection.Count > 0);
                }
            }

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
                (CarBatteryOn != newCarBatteryOn) ||
                (TrainPowerOn != newPowerOn) ||
                specialLightsChanged)
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
                TrainPowerOn = newPowerOn;
                SpecialLights = newSpecialLights;
                SpecialLightsSelection = newSpecialLightsSelection;

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
        public bool Enabled;
        public Vector2 Fade;
        public bool FadeIn;
        public bool FadeOut;
        protected float FadeTime;
        protected int State;
        protected int StateCount;
        protected float StateTime;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public LightPrimitive(Light light)
        {
            Light = light;
            StateCount = Light.Cycle ? 2 * Light.States.Count - 2 : Light.States.Count;
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
                    transitionHandler(Light.States.Count * 2 - 1 - i, i, i - 1);
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
            Enabled = lightViewer.SpecialLights != SpecialLightsCondition.Off;

            // check conditions which always apply
            if (Enabled && Light.Unit != LightUnitCondition.Ignore)
            {
                if (Light.Unit == LightUnitCondition.Middle)
                    Enabled &= !lightViewer.CarIsFirst && !lightViewer.CarIsLast;
                else if (Light.Unit == LightUnitCondition.First)
                    Enabled &= lightViewer.CarIsFirst && !lightViewer.CarIsReversed;
                else if (Light.Unit == LightUnitCondition.Last)
                    Enabled &= lightViewer.CarIsLast && !lightViewer.CarIsReversed;
                else if (Light.Unit == LightUnitCondition.LastRev)
                    Enabled &= lightViewer.CarIsLast && lightViewer.CarIsReversed;
                else if (Light.Unit == LightUnitCondition.FirstRev)
                    Enabled &= lightViewer.CarIsFirst && lightViewer.CarIsReversed;
                else
                    Enabled &= false;
            }
            if (Enabled && Light.TimeOfDay != LightTimeOfDayCondition.Ignore)
            {
                if (Light.TimeOfDay == LightTimeOfDayCondition.Day)
                    Enabled &= lightViewer.IsDay;
                else if (Light.TimeOfDay == LightTimeOfDayCondition.Night)
                    Enabled &= !lightViewer.IsDay;
                else
                    Enabled &= false;
            }
            if (Enabled && Light.Weather != LightWeatherCondition.Ignore)
            {
                if (Light.Weather == LightWeatherCondition.Clear)
                    Enabled &= lightViewer.Weather == WeatherType.Clear;
                else if (Light.Weather == LightWeatherCondition.Rain)
                    Enabled &= lightViewer.Weather == WeatherType.Rain;
                else if (Light.Weather == LightWeatherCondition.Snow)
                    Enabled &= lightViewer.Weather == WeatherType.Snow;
                else
                    Enabled &= false;
            }
            if (Enabled && Light.Coupling != LightCouplingCondition.Ignore)
            {
                if (Light.Coupling == LightCouplingCondition.Front)
                    Enabled &= lightViewer.CarCoupledFront && !lightViewer.CarCoupledRear;
                else if (Light.Coupling == LightCouplingCondition.Rear)
                    Enabled &= !lightViewer.CarCoupledFront && lightViewer.CarCoupledRear;
                else if (Light.Coupling == LightCouplingCondition.Both)
                    Enabled &= lightViewer.CarCoupledFront && lightViewer.CarCoupledRear;
                else
                    Enabled &= false;
            }
            if (Enabled && Light.TrainPower != LightPowerCondition.Ignore)
            {
                if (Light.TrainPower == LightPowerCondition.On)
                    Enabled &= lightViewer.TrainPowerOn;
                else if (Light.TrainPower == LightPowerCondition.Off)
                    Enabled &= !lightViewer.TrainPowerOn;
                else
                    Enabled &= false;
            }

            // check conditions which apply for normal and special_additional

            if (Enabled && lightViewer.SpecialLights == SpecialLightsCondition.Normal || lightViewer.SpecialLights == SpecialLightsCondition.Special_additional)
            {

                if (Enabled && Light.Headlight != LightHeadlightCondition.Ignore)
                {
                if (Light.Headlight == LightHeadlightCondition.Off)
                    Enabled &= lightViewer.TrainHeadlight == 0;
                else if (Light.Headlight == LightHeadlightCondition.Dim)
                    Enabled &= lightViewer.TrainHeadlight == 1;
                else if (Light.Headlight == LightHeadlightCondition.Bright)
                    Enabled &= lightViewer.TrainHeadlight == 2;
                else if (Light.Headlight == LightHeadlightCondition.DimBright)
                    Enabled &= lightViewer.TrainHeadlight >= 1;
                else if (Light.Headlight == LightHeadlightCondition.OffDim)
                    Enabled &= lightViewer.TrainHeadlight <= 1;
                else if (Light.Headlight == LightHeadlightCondition.OffBright)
                    Enabled &= lightViewer.TrainHeadlight != 1;
                else
                    Enabled &= false;
            }
                if (Enabled && Light.Unit != LightUnitCondition.Ignore)
            {
                if (Light.Unit == LightUnitCondition.Middle)
                    Enabled &= !lightViewer.CarIsFirst && !lightViewer.CarIsLast;
                else if (Light.Unit == LightUnitCondition.First)
                    Enabled &= lightViewer.CarIsFirst && !lightViewer.CarIsReversed;
                else if (Light.Unit == LightUnitCondition.Last)
                    Enabled &= lightViewer.CarIsLast && !lightViewer.CarIsReversed;
                else if (Light.Unit == LightUnitCondition.LastRev)
                    Enabled &= lightViewer.CarIsLast && lightViewer.CarIsReversed;
                else if (Light.Unit == LightUnitCondition.FirstRev)
                    Enabled &= lightViewer.CarIsFirst && lightViewer.CarIsReversed;
                else
                    Enabled &= false;
            }
                if (Enabled && Light.Penalty != LightPenaltyCondition.Ignore)
            {
                if (Light.Penalty == LightPenaltyCondition.No)
                    Enabled &= !lightViewer.Penalty;
                else if (Light.Penalty == LightPenaltyCondition.Yes)
                    Enabled &= lightViewer.Penalty;
                else
                    Enabled &= false;
            }
                if (Enabled && Light.Control != LightControlCondition.Ignore)
            {
                if (Light.Control == LightControlCondition.AI)
                    Enabled &= !lightViewer.CarIsPlayer;
                else if (Light.Control == LightControlCondition.Player)
                    Enabled &= lightViewer.CarIsPlayer;
                else
                    Enabled &= false;
            }
                if (Enabled && Light.Service != LightServiceCondition.Ignore)
            {
                if (Light.Service == LightServiceCondition.No)
                    Enabled &= !lightViewer.CarInService;
                else if (Light.Service == LightServiceCondition.Yes)
                    Enabled &= lightViewer.CarInService;
                else
                    Enabled &= false;
            }
                if (Enabled && Light.TimeOfDay != LightTimeOfDayCondition.Ignore)
            {
                if (Light.TimeOfDay == LightTimeOfDayCondition.Day)
                    Enabled &= lightViewer.IsDay;
                else if (Light.TimeOfDay == LightTimeOfDayCondition.Night)
                    Enabled &= !lightViewer.IsDay;
                else
                    Enabled &= false;
            }
                if (Enabled && Light.Weather != LightWeatherCondition.Ignore)
            {
                if (Light.Weather == LightWeatherCondition.Clear)
                    Enabled &= lightViewer.Weather == WeatherType.Clear;
                else if (Light.Weather == LightWeatherCondition.Rain)
                    Enabled &= lightViewer.Weather == WeatherType.Rain;
                else if (Light.Weather == LightWeatherCondition.Snow)
                    Enabled &= lightViewer.Weather == WeatherType.Snow;
                else
                    Enabled &= false;
            }
                if (Enabled && Light.Coupling != LightCouplingCondition.Ignore)
            {
                if (Light.Coupling == LightCouplingCondition.Front)
                    Enabled &= lightViewer.CarCoupledFront && !lightViewer.CarCoupledRear;
                else if (Light.Coupling == LightCouplingCondition.Rear)
                    Enabled &= !lightViewer.CarCoupledFront && lightViewer.CarCoupledRear;
                else if (Light.Coupling == LightCouplingCondition.Both)
                    Enabled &= lightViewer.CarCoupledFront && lightViewer.CarCoupledRear;
                else
                    Enabled &= false;
            }
                if (Enabled && Light.Battery != LightBatteryCondition.Ignore)
            {
                if (Light.Battery == LightBatteryCondition.On)
                    Enabled &= lightViewer.CarBatteryOn;
                else if (Light.Battery == LightBatteryCondition.Off)
                    Enabled &= !lightViewer.CarBatteryOn;
                else
                    Enabled &= false;
            }


                // if light setting is normal and this is special light, disable
                if (Enabled && lightViewer.SpecialLights == SpecialLightsCondition.Normal && !String.IsNullOrEmpty(Light.ORTSSpecialLight))
                {
                    Enabled &= false;
                }

                // check conditions for special
                if (Enabled && (lightViewer.SpecialLights == SpecialLightsCondition.Special_only || lightViewer.SpecialLights == SpecialLightsCondition.Special_additional))
                {
                    Enabled &= lightViewer.SpecialLightsSelection.Contains(Light.ORTSSpecialLight);
                }

            if (oldEnabled != Enabled)
            {
                FadeIn = Enabled;
                FadeOut = !Enabled;
                FadeTime = 0;
            }
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
                Fade.X = 1 - FadeTime / Light.FadeIn;
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

        public LightGlowPrimitive(LightViewer lightViewer, RenderProcess renderProcess, Light light)
            : base(light)
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

        public LightConePrimitive(LightViewer lightViewer, RenderProcess renderProcess, Light light)
            : base(light)
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

        public LightGlowMaterial(Viewer viewer)
            : base(viewer, null)
        {
            // TODO: This should happen on the loader thread.
            LightGlowTexture = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "Lightglow.png"));
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
