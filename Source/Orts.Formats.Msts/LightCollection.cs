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

using Microsoft.Xna.Framework;
using Orts.Parsers.Msts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Orts.Formats.Msts
{
    /// <summary>
    /// A LightState object encapsulates the data for each State in the States subblock.
    /// </summary>
    public class LightState
    {
        public float Duration;
        public Color Color;
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
                new STFReader.TokenProcessor("lightcolour", ()=>{ Color = stf.ReadColorBlock(null); }),
                new STFReader.TokenProcessor("position", ()=>{ Position = stf.ReadVector3Block(STFReader.UNITS.None, Vector3.Zero); }),
                new STFReader.TokenProcessor("radius", ()=>{ Radius = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                new STFReader.TokenProcessor("azimuth", ()=>{ Azimuth = stf.ReadVector3Block(STFReader.UNITS.None, Vector3.Zero); }),
                new STFReader.TokenProcessor("elevation", ()=>{ Elevation = stf.ReadVector3Block(STFReader.UNITS.None, Vector3.Zero); }),
                new STFReader.TokenProcessor("transition", ()=>{ Transition = 1 <= stf.ReadFloatBlock(STFReader.UNITS.None, 0); }),
                new STFReader.TokenProcessor("angle", ()=>{ Angle = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
            });
        }

        public LightState(LightState state, bool reverse)
        {
            Duration = state.Duration;
            Color = state.Color;
            Position = state.Position;
            Radius = state.Radius;
            Azimuth = state.Azimuth;
            Elevation = state.Elevation;
            Transition = state.Transition;
            Angle = state.Angle;

            if (reverse)
            {
                Azimuth.X += 180;
                Azimuth.X %= 360;
                Azimuth.Y += 180;
                Azimuth.Y %= 360;
                Azimuth.Z += 180;
                Azimuth.Z %= 360;
                Position.X *= -1;
                Position.Z *= -1;
            }
        }
    }

    #region Light enums
    /// <summary>
    /// Specifies whether a wagon light is glow (simple light texture) or cone (projected light cone).
    /// </summary>
    public enum LightType
    {
        Glow,
        Cone,
    }

    /// <summary>
    /// Specifies in which headlight positions (off, dim, bright) the wagon light is illuminated.
    /// </summary>
    public enum LightHeadlightCondition
    {
        Ignore,
        Off,
        Dim,
        Bright,
        DimBright, // MSTSBin
        OffBright, // MSTSBin
        OffDim, // MSTSBin
        // TODO: DimBright?, // MSTSBin labels this the same as DimBright. Not sure what it means.
    }

    /// <summary>
    /// Specifies on which units of a consist (first, middle, last) the wagon light is illuminated.
    /// </summary>
    public enum LightUnitCondition
    {
        Ignore,
        Middle,
        First,
        Last,
        LastRev, // MSTSBin
        FirstRev, // MSTSBin
    }

    /// <summary>
    /// Specifies in which penalty states (no, yes) the wagon light is illuminated.
    /// </summary>
    public enum LightPenaltyCondition
    {
        Ignore,
        No,
        Yes,
    }

    /// <summary>
    /// Specifies on which types of trains (AI, player) the wagon light is illuminated.
    /// </summary>
    public enum LightControlCondition
    {
        Ignore,
        AI,
        Player,
    }

    /// <summary>
    /// Specifies in which in-service states (no, yes) the wagon light is illuminated.
    /// </summary>
    public enum LightServiceCondition
    {
        Ignore,
        No,
        Yes,
    }

    /// <summary>
    /// Specifies during which times of day (day, night) the wagon light is illuminated.
    /// </summary>
    public enum LightTimeOfDayCondition
    {
        Ignore,
        Day,
        Night,
    }

    /// <summary>
    /// Specifies in which weather conditions (clear, rain, snow) the wagon light is illuminated.
    /// </summary>
    public enum LightWeatherCondition
    {
        Ignore,
        Clear,
        Rain,
        Snow,
    }

    /// <summary>
    /// Specifies on which units of a consist by coupling (front, rear, both) the wagon light is illuminated.
    /// </summary>
    public enum LightCouplingCondition
    {
        Ignore,
        Front,
        Rear,
        Both,
    }

    /// <summary>
    /// Specifies if the light must be illuminated on if low voltage power supply is on or off.
    /// </summary>
    public enum LightBatteryCondition
    {
        Ignore,
        On,
        Off,
    }

    /// <summary>
    /// Specifies if the light must be illuminated on if train power supply is on or off.
    /// </summary>
    public enum LightPowerCondition
    {
        Ignore,
        On,
        Off,
    }

    /// <summary>
    /// Specifies use of special ORST lights (defined through timetable commands)
    /// Normal - normal lights are in use
    /// Off - no lights shown at all
    /// Special - as set through timetable commands, in variable SpecialLightsCommand in TrainCar class
    /// Special_additional - special lights shown additional to normal lights
    /// Special_ony - special lights only are shown, normal lights are off
    /// </summary>
    public enum SpecialLightsCondition
    {
        Normal,
        Off,
        Special_additional,
        Special_only,
    }

    /// <summary>
    /// Specifies train existance phase during which special lights settings are used
    /// PreStart : during AI_STATIC phase, when train is inactive (after create, forms or detach), before start time
    /// Running : after start time
    /// PostForms : after forms or detach, but only if static (otherwise light setting of new formed train is used)
    /// </summary>
    public enum SpecialLightsPhase
    {
        PreStart,
        Active,
        PostForms,
        LastEntry,          // keep this as last entry for array size
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
        public LightBatteryCondition Battery;
        public LightPowerCondition TrainPower;
        public string ORTSSpecialLight;
        public bool Cycle;
        public float FadeIn;
        public float FadeOut;
        public List<LightState> States = new List<LightState>();

        public Light(int index, STFReader stf)
        {
            Index = index;
            stf.MustMatch("(");
            stf.ParseBlock(new[] {
                new STFReader.TokenProcessor("type", ()=>{ Type = (LightType)stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("conditions", ()=>{ stf.MustMatch("("); stf.ParseBlock(new[] {
                    new STFReader.TokenProcessor("headlight", ()=>{ Headlight = (LightHeadlightCondition)stf.ReadIntBlock(null); }),
                    new STFReader.TokenProcessor("unit", ()=>{ Unit = (LightUnitCondition)stf.ReadIntBlock(null); }),
                    new STFReader.TokenProcessor("penalty", ()=>{ Penalty = (LightPenaltyCondition)stf.ReadIntBlock(null); }),
                    new STFReader.TokenProcessor("control", ()=>{ Control = (LightControlCondition)stf.ReadIntBlock(null); }),
                    new STFReader.TokenProcessor("service", ()=>{ Service = (LightServiceCondition)stf.ReadIntBlock(null); }),
                    new STFReader.TokenProcessor("timeofday", ()=>{ TimeOfDay = (LightTimeOfDayCondition)stf.ReadIntBlock(null); }),
                    new STFReader.TokenProcessor("weather", ()=>{ Weather = (LightWeatherCondition)stf.ReadIntBlock(null); }),
                    new STFReader.TokenProcessor("coupling", ()=>{ Coupling = (LightCouplingCondition)stf.ReadIntBlock(null); }),
                    new STFReader.TokenProcessor("ortsbattery", ()=>{ Battery = (LightBatteryCondition)stf.ReadIntBlock(null); }),
                    new STFReader.TokenProcessor("ortstrainpower", ()=>{ TrainPower = (LightPowerCondition)stf.ReadIntBlock(null); }),
                    new STFReader.TokenProcessor("ortsspeciallight", ()=>{ ORTSSpecialLight = stf.ReadStringBlock(String.Empty); }),
                });}),
                new STFReader.TokenProcessor("cycle", ()=>{ Cycle = 0 != stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("fadein", ()=>{ FadeIn = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("fadeout", ()=>{ FadeOut = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("states", ()=>{
                    stf.MustMatch("(");
                    var count = stf.ReadInt(null);
                    stf.ParseBlock(new[] {
                        new STFReader.TokenProcessor("state", ()=>{
                            if (States.Count >= count)
                                STFException.TraceWarning(stf, "Skipped extra State");
                            else
                                States.Add(new LightState(stf));
                        }),
                    });
                    if (States.Count < count)
                        STFException.TraceWarning(stf, (count - States.Count).ToString() + " missing State(s)");
                }),
            });
        }

        public Light(Light light, bool reverse)
        {
            Index = light.Index;
            Type = light.Type;
            Headlight = light.Headlight;
            Unit = light.Unit;
            Penalty = light.Penalty;
            Control = light.Control;
            Service = light.Service;
            TimeOfDay = light.TimeOfDay;
            Weather = light.Weather;
            Coupling = light.Coupling;
            Battery = light.Battery;
            TrainPower = light.TrainPower;
            ORTSSpecialLight = light.ORTSSpecialLight;
            Cycle = light.Cycle;
            FadeIn = light.FadeIn;
            FadeOut = light.FadeOut;
            foreach (var state in light.States)
                States.Add(new LightState(state, reverse));

            if (reverse)
            {
                if (Unit == LightUnitCondition.First)
                    Unit = LightUnitCondition.FirstRev;
                else if (Unit == LightUnitCondition.Last)
                    Unit = LightUnitCondition.LastRev;
            }
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
            stf.ReadInt(null); // count; ignore this because its not always correct
            stf.ParseBlock(new[] {
                new STFReader.TokenProcessor("light", ()=>{ Lights.Add(new Light(Lights.Count, stf)); }),
            });
            if (Lights.Count == 0)
                throw new InvalidDataException("lights with no lights");

            // MSTSBin created reverse headlight cones automatically, so we shall do so too.
            foreach (var light in Lights.ToArray())
                if (light.Type == LightType.Cone)
                    Lights.Add(new Light(light, true));
        }
    }
}
