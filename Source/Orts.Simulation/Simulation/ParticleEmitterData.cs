// COPYRIGHT 2026 by the Open Rails project.
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
using Orts.Common;
using Orts.Parsers.Msts;

namespace Orts.Simulation
{
    /// <summary>
    /// Class for storing and parsing the settings for a particle emitter.
    /// Actual behavior of particle emitters is controlled by ParticleEmitter.cs
    /// </summary>
    public class ParticleEmitterData
    {
        // NOTE: To anyone intending to add additional particle emitter types,
        // make sure the particle emitter type is added to this enum,
        // AND make sure the new particle emitter type is parsed in ParticleEmitterData.NewParticleEmitterData
        // AND make sure there is proper handling for controlling the rate of particles emitted by
        // the new particle emitter type in ParticleEmitterViewer.UpdateOutput (or one of its overrides)
        // Each particle emitter type in this enum should have AT LEAST 2 references
        public enum ParticleEmitterTypes
        {
            Unknown,
            // Wagon type agnostic emitter types
            HeatingSteamBoiler, // dieselsmoke
            HeatingHose,
            HeatingMainPipeSteamTrap,
            HeatingCompartmentSteamTrap,
            WagonGenerator, // dieselsmoke
            WagonSmoke,
            BearingHotBox,
            WaterScoopForward,
            WaterScoopReverse,
            TenderWaterOverflow,
            SteamBrake,
            // Steam locomotive specific emitter types
            CylinderCocks,
            FrontCylinderCock,
            RearCylinderCock,
            CylinderSteamExhaust,
            BoosterCylinderCocks,
            FrontBoosterCylinderCock,
            RearBoosterCylinderCock,
            BoosterCylinderSteamExhaust,
            SanderSteamExhaustForward,
            SanderSteamExhaustReverse,
            Blowdown,
            Drainpipe,
            Injector,
            SmallEjector,
            LargeEjector,
            CounterPressureBrake,
            Compressor,
            Generator,
            SafetyValve,
            Stack,
            Whistle,
            // Diesel locomotive specific emitter types
            DieselExhaust // dieselsmoke
        }

        public ParticleEmitterTypes ParticleEmitterType = ParticleEmitterTypes.Unknown;

        public Vector3 PositionM;
        public Vector3 PositionVariationM = Vector3.Zero;

        public Vector3 InitialVelocityFactor; // Note: Not a measure of the initial velocity, actually just a multiplication factor
        public Vector3 InitialVelocityVariationFactor = new Vector3(0.1f); // Randomization of initial velocity is relative to initial speed

        public Vector3 FinalVelocityMpS = Vector3.Zero;
        public Vector3 FinalVelocityVariationMpS = new Vector3(0.75f);

        public float NozzleDiameterM = 0.1f;
        public float NozzleAreaM2 = -1; // If left at -1, will be initialized later

        public float RateFactor = 1.0f;
        public float LifetimeFactor = 1.0f;
        public float LifetimeVariationFactor = 0.5f;
        public float SettlingFactor = 1.0f;
        public float SettlingVariationFactor = 0.1f;
        public float ExpansionSpeed = 4.0f;
        public float InitialExpansionFactor = 1.0f;

        public float RotationVariation = 0.25f;

        public float WindEffect = 1.0f;
        public int MaxParticles = 2048;

        public float Opacity = 1.0f;
        public string Graphic;
        public int AtlasWidth = 4;
        public int AtlasHeight = 4;
        public Color BaseColor = Color.White;

        public ParticleEmitterData(ParticleEmitterTypes type)
        {
            ParticleEmitterType = type;
        }

        /// <summary>
        /// Creates a new particle emitter data object, the type of emitter being specified by the
        /// emitter name.
        /// </summary>
        /// <param name="stf">Reference to the stf reader that is parsing the particle emitter settings.</param>
        /// <param name="name">The name of the particle emitter as entered in the source file, such as "WhistleFX".</param>
        /// <returns>A new ParticleEmitterData object, may be a derived type of ParticleEmitterData.</returns>
        public static ParticleEmitterData NewParticleEmitterData(STFReader stf, string name)
        {
            name = name.ToLowerInvariant();

            switch (name)
            {
                // Wagon type agnostic emitter types
                case "heatingsteamboilerfx": return new DieselParticleEmitterData(ParticleEmitterTypes.HeatingSteamBoiler);
                case "heatinghosefx": return new SteamParticleEmitterData(ParticleEmitterTypes.HeatingHose);
                case "heatingmainpipesteamtrapfx": return new SteamParticleEmitterData(ParticleEmitterTypes.HeatingMainPipeSteamTrap);
                case "heatingcompartmentsteamtrapfx": return new SteamParticleEmitterData(ParticleEmitterTypes.HeatingCompartmentSteamTrap);
                case "wagongeneratorfx": return new DieselParticleEmitterData(ParticleEmitterTypes.WagonGenerator);
                case "wagonsmokefx": return new SteamParticleEmitterData(ParticleEmitterTypes.WagonSmoke);
                case "bearinghotboxfx": return new SteamParticleEmitterData(ParticleEmitterTypes.BearingHotBox);
                case "waterscoopfx": return new WaterParticleEmitterData(ParticleEmitterTypes.WaterScoopForward);
                case "waterscoopreversefx": return new WaterParticleEmitterData(ParticleEmitterTypes.WaterScoopReverse);
                case "tenderwateroverflowfx": return new WaterParticleEmitterData(ParticleEmitterTypes.TenderWaterOverflow);
                case "steambrakefx": return new SteamParticleEmitterData(ParticleEmitterTypes.SteamBrake);

                case string s when s.StartsWith("cylindersteamexhaust") && s.EndsWith("fx"): // Values in the format "CylinderSteamExhaustY_ZFX" are accepted, where Y (optional) is the engine number, and Z is the cylinder number
                    // Determine which engine and which cylinder this emitter is using based on structure of the text
                    int cylinderExhaustEngineIndex = GetSteamEngineNumber(name, "cylindersteamexhaust", "fx", out string cylinderExhaustCylinderNumbers);

                    int cylinderExhaustCylinderIndex;
                    if (!int.TryParse(cylinderExhaustCylinderNumbers, out cylinderExhaustCylinderIndex))
                        cylinderExhaustCylinderIndex = 1; // For some reason, the text couldn't be read as an integer, assume cylinder number is 1

                    // Note: User input will be 1-indexed, but program requires these values to be 0-indexed
                    cylinderExhaustEngineIndex--;
                    cylinderExhaustCylinderIndex--;

                    return new SteamCylinderParticleEmitterData(ParticleEmitterTypes.CylinderSteamExhaust, cylinderExhaustEngineIndex, cylinderExhaustCylinderIndex);

                // Steam locomotive specific emitter types
                case string s when s.StartsWith("cylinders") && s.EndsWith("fx"): // Values in the format "CylindersW_YZfx" are accepted, where W (optional) is the engine number, Y is the cylinder number, and Z (optional) is 1 (front) or 2 (rear)
                    // Determine which engine, which cylinder, and which valve this emitter is using based on structure of the text
                    int cylinderCockEngineIndex = GetSteamEngineNumber(name, "cylinders", "fx", out string cylinderCockCylinderNumbers);

                    (int cylinderCockCylinderIndex, int cylinderValveIndex) = GetSteamCylinderNumber(cylinderCockCylinderNumbers);

                    // Note: User input will be 1-indexed, but program requires these values to be 0-indexed
                    cylinderCockEngineIndex--;
                    cylinderCockCylinderIndex--;

                    if (cylinderValveIndex == 0) // Both cylinder cocks
                        return new SteamCylinderParticleEmitterData(ParticleEmitterTypes.CylinderCocks, cylinderCockEngineIndex, cylinderCockCylinderIndex);
                    else if (cylinderValveIndex == 1) // Front cylinder cock only
                        return new SteamCylinderParticleEmitterData(ParticleEmitterTypes.FrontCylinderCock, cylinderCockEngineIndex, cylinderCockCylinderIndex);
                    else // Rear cylinder cock only
                        return new SteamCylinderParticleEmitterData(ParticleEmitterTypes.RearCylinderCock, cylinderCockEngineIndex, cylinderCockCylinderIndex);

                case string s when s.StartsWith("boostercylindersteamexhaust") && s.EndsWith("fx"): // Values in the format "BoosterCylinderSteamExhaustZFX" are accepted, where Z is the cylinder number
                    // Determine which cylinder this emitter is using based on structure of the text
                    int boosterExhaustCylinderIndex = StringHelper.ExtractInt(name, "boostercylindersteamexhaust", "fx", System.StringComparison.OrdinalIgnoreCase) ?? 1;

                    // Note: User input will be 1-indexed, but program requires this value to be 0-indexed
                    boosterExhaustCylinderIndex--;

                    // Note: An engine index of -1 is passed to the emitter data, this will be set to the index of the booster engine during initialization
                    return new SteamCylinderParticleEmitterData(ParticleEmitterTypes.BoosterCylinderSteamExhaust, -1, boosterExhaustCylinderIndex);

                case string s when s.StartsWith("boostercylinders") && s.EndsWith("fx"): // Values in the format "BoosterCylindersYZFX" are accepted, where Y is the cylinder number and Z is 1 or 2
                    // Determine which cylinder and which valve this emitter is using based on structure of the text
                    (int boosterCylinderCockCylinderIndex, int boosterValveIndex) = GetSteamCylinderNumber(name, "boostercylinders", "fx");

                    // Note: User input will be 1-indexed, but program requires this value to be 0-indexed
                    boosterCylinderCockCylinderIndex--;

                    // Note: An engine index of -1 is passed to the emitter data, this will be set to the index of the booster engine during initialization
                    if (boosterValveIndex == 0) // Both cylinder cocks
                        return new SteamCylinderParticleEmitterData(ParticleEmitterTypes.BoosterCylinderCocks, -1, boosterCylinderCockCylinderIndex);
                    else if (boosterValveIndex == 1) // Front cylinder cock only
                        return new SteamCylinderParticleEmitterData(ParticleEmitterTypes.FrontBoosterCylinderCock, -1, boosterCylinderCockCylinderIndex);
                    else // Rear cylinder cock only
                        return new SteamCylinderParticleEmitterData(ParticleEmitterTypes.RearBoosterCylinderCock, -1, boosterCylinderCockCylinderIndex);

                case "sandersteamexhaustforwardfx": return new SteamParticleEmitterData(ParticleEmitterTypes.SanderSteamExhaustForward);
                case "sandersteamexhaustreversefx": return new SteamParticleEmitterData(ParticleEmitterTypes.SanderSteamExhaustReverse);
                case "blowdownfx": return new SteamParticleEmitterData(ParticleEmitterTypes.Blowdown);
                case "drainpipefx": return new SteamParticleEmitterData(ParticleEmitterTypes.Drainpipe);

                case string s when s.StartsWith("injectors") && s.EndsWith("fx"): // Values in the format "InjectorsZFX" are accepted, where Z is the injector number
                    // Determine which injector this emitter is using based on structure of the text
                    // First injector will be used if no index could be found in the name
                    int injectorIndex = StringHelper.ExtractInt(name, "injectors", "fx", System.StringComparison.OrdinalIgnoreCase) ?? 1;
                    // Note: User input will be 1-indexed, but program requires this value to be 0-indexed
                    injectorIndex--;

                    return new SteamIndexableParticleEmitterData(ParticleEmitterTypes.Injector, injectorIndex);

                case "smallejectorfx": return new SteamParticleEmitterData(ParticleEmitterTypes.SmallEjector);
                case "largeejectorfx": return new SteamParticleEmitterData(ParticleEmitterTypes.LargeEjector);

                case string s when s.StartsWith("counterpressurebrake") && s.EndsWith("fx"):
                    // For some reason, counterpressurebrake must accept variable indices even though there is no difference between "CounterPressureBrake1FX" and "CounterPressureBrake2FX"
                    return new SteamParticleEmitterData(ParticleEmitterTypes.CounterPressureBrake);

                case "compressorfx": return new SteamParticleEmitterData(ParticleEmitterTypes.Compressor);
                case "generatorfx": return new SteamParticleEmitterData(ParticleEmitterTypes.Generator);

                case string s when s.StartsWith("safetyvalves") && s.EndsWith("fx"): // Values in the format "SafetyValvesZFX" are accepted, where Z is the safety valve number
                    // Determine which safety valve this emitter is using based on structure of the text
                    // First safety valve will be used if no index could be found in the name
                    int safetyIndex = StringHelper.ExtractInt(name, "safetyvalves", "fx", System.StringComparison.OrdinalIgnoreCase) ?? 1;
                    // Note: User input will be 1-indexed, but program requires this value to be 0-indexed
                    safetyIndex--;

                    return new SteamIndexableParticleEmitterData(ParticleEmitterTypes.SafetyValve, safetyIndex);

                case "stackfx": return new SteamCylinderParticleEmitterData(ParticleEmitterTypes.Stack, -1, -1); // Stack needs to use the same default settings as cylinder exhaust, but index is irrelevant
                case "whistlefx": return new SteamParticleEmitterData(ParticleEmitterTypes.Whistle);

                // Diesel locomotive specific emitter types
                case string s when s.StartsWith("exhaust"): // Values in the format "ExhaustZ" are accepted, where Z is the engine number
                    // Determine which diesel engine this emitter is using based on structure of the text
                    // First diesel engine will be used if no index could be found in the name
                    int dieselEngineIndex = StringHelper.ExtractInt(name, "exhaust", "", System.StringComparison.OrdinalIgnoreCase) ?? 1;
                    // Note: User input will be 1-indexed, but program requires this value to be 0-indexed
                    dieselEngineIndex--;

                    return new DieselEngineParticleEmitterData(ParticleEmitterTypes.DieselExhaust, dieselEngineIndex);
                default:
                    // Couldn't match the particle emitter name to a known particle emitter type
                    STFException.TraceWarning(stf, "Encountered unrecognized special effect type " + name);
                    return new ParticleEmitterData(ParticleEmitterTypes.Unknown);
            }
        }

        /// <summary>
        /// Particle emitters for steam locomotives may specify a steam engine number, this extracts the
        /// steam engine number from the name of the particle emitter (or returns 1 if the steam engine
        /// number isn't specified). Also outputs the unparsed part of the particle emitter name, which
        /// should just be the steam cylinder number.
        /// </summary>
        /// <param name="name">The name of the particle emitter</param>
        /// <param name="beginning">The text in <paramref name="name"/> that precedes the steam engine numbers</param>
        /// <param name="end">The text in <paramref name="name"/> that follows the steam engine numbers</param>
        /// <param name="remainder">Output parameter with the leftover text that was not parsed</param>
        /// <returns>The number of the steam engine as defined in <paramref name="name"/>, or 1 if no
        /// steam engine number is explicitly defined.</returns>
        public static int GetSteamEngineNumber(string name, string beginning, string end, out string remainder)
        {
            int steamEngineNumber;

            // Isolate the text that should contain the steam engine numbers to parse
            string trimmedName = StringHelper.TrimBeginningAndEnd(name, beginning, end, System.StringComparison.OrdinalIgnoreCase);

            // Steam engine definition should be in the format "X_YZ" where X is the engine number, Y is the cylinder number, and Z is 1 or 2
            // In this method, the goal is to determine the value "X"
            string[] steamNumbers = trimmedName.Split("_");

            // We should now have two strings, the first being just "X" (the value we want), the other being "YZ" (the remainder)
            // If there is only one string, the steam engine has not been defined explicitly
            if (steamNumbers.Length > 1)
            {
                if (!int.TryParse(steamNumbers[0], out steamEngineNumber))
                    steamEngineNumber = 1; // For some reason, the text couldn't be read as an integer, assume engine number is 1

                // The final element of the split is the remainder, this should be parsed separately to determine the steam cylinder number
                remainder = steamNumbers[steamNumbers.Length - 1];
            }
            else if (steamNumbers.Length > 0)
            {
                // Steam engine number wasn't defined, assume 1
                steamEngineNumber = 1;
                remainder = steamNumbers[0];
            }
            else
            {
                // Malformed particle emitter name, assume steam engine number is 1
                steamEngineNumber = 1;
                remainder = "";
            }

            return steamEngineNumber;
        }

        /// <summary>
        /// Particle emitters for steam locomotives may specify a steam cylinder number and a number to indicate
        /// if the front or rear valve of the steam cylinder is used. This determines which cylinder
        /// (or cylinder 1 if none could be determined) and whether the front or rear cylinder valve (or both if neither
        /// was specified) are being referenced.
        /// </summary>
        /// <param name="cylinderNumbers">The string containing the cylinder number and valve number</param>
        /// <returns>Two integers, the first being the steam cylinder number, the second
        /// being 0 (neither front nor rear valve), 1 (front valve), or 2 (rear valve)</returns>
        public static (int, int) GetSteamCylinderNumber(string cylinderNumbers)
        {
            int cylinderNumber;
            int valveNumber;

            // Steam cylinder and valve number should be in the format "YZ" where Y is the cylinder number, and Z is 1 (front valve) or 2 (rear valve)
            if (cylinderNumbers.Length > 1)
            {
                // At least 2 characters, assume the last character is the valve number and other characters are the cylinder number
                string valveText = cylinderNumbers[(cylinderNumbers.Length - 1)..]; // All other characters
                string cylinderText = cylinderNumbers[..(cylinderNumbers.Length - 1)]; // Final character

                if (!int.TryParse(cylinderText, out cylinderNumber))
                    cylinderNumber = 1; // For some reason, the text couldn't be read as an integer, assume cylinder number is 1
                if (!int.TryParse(valveText, out valveNumber))
                    valveNumber = 0; // For some reason, the text couldn't be read as an integer, assume neither valve has been specified
            }
            else if (cylinderNumbers.Length > 0)
            {
                // Only 1 character, assume this character is the cylinder number (neither front nor rear valve has been specified)
                if (!int.TryParse(cylinderNumbers, out cylinderNumber))
                    cylinderNumber = 1; // For some reason, the text couldn't be read as an integer, assume cylinder number is 1

                valveNumber = 0;
            }
            else
            {
                // Malformed particle emitter name, assume steam cylinder number is 1, assume neither valve has been specified
                cylinderNumber = 1;
                valveNumber = 0;
            }

            return (cylinderNumber, valveNumber);
        }

        /// <summary>
        /// Particle emitters for steam locomotives may specify a steam cylinder number and a number to indicate
        /// if the front or rear valve of the steam cylinder is used. This determines which cylinder
        /// (or cylinder 1 if none could be determined) and whether the front or rear cylinder valve (or both if neither
        /// was specified) are being referenced.
        /// </summary>
        /// <param name="name">The name of the particle emitter</param>
        /// <param name="beginning">The text in <paramref name="name"/> that precedes the steam cylinder numbers</param>
        /// <param name="end">The text in <paramref name="name"/> that follows the steam cylinder numbers</param>
        /// <returns>Two integers, the first being the steam cylinder number, the second
        /// being 0 (neither front nor rear valve), 1 (front valve), or 2 (rear valve)</returns>
        public static (int, int) GetSteamCylinderNumber(string name, string beginning, string end)
        {
            string cylinderNumbers = StringHelper.TrimBeginningAndEnd(name, beginning, end, System.StringComparison.OrdinalIgnoreCase);

            return GetSteamCylinderNumber(cylinderNumbers);
        }

        /// <summary>
        /// Read user input for particle emitter and set particle emitter data accordingly.
        /// </summary>
        /// <param name="stf">Reference to the stf reader that is parsing the particle emitter settings.</param>
        public void Parse(STFReader stf)
        {
            stf.MustMatch("(");
            PositionM = stf.ReadVector3(STFReader.UNITS.Distance, Vector3.Zero);
            PositionM.Z *= -1; // Convert to MSTS coordinate system
            InitialVelocityFactor = stf.ReadVector3(STFReader.UNITS.None, Vector3.Zero);
            InitialVelocityFactor.Z *= -1; // Convert to MSTS coordinate system
            NozzleDiameterM = stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            stf.SkipRestOfBlock();
        }
    }

    /// <summary>
    /// ParticleEmitterData with default values modified as appropriate for
    /// particles that represent steam
    /// </summary>
    public class SteamParticleEmitterData : ParticleEmitterData
    {
        public SteamParticleEmitterData(ParticleEmitterTypes type) : base(type)
        {
            Graphic = "smokemain.ace";
        }
    }

    /// <summary>
    /// ParticleEmitterData with additional data and default values modified as
    /// appropriate for particles that represent exhaust from various steam locomotive components
    /// </summary>
    public class SteamIndexableParticleEmitterData : SteamParticleEmitterData
    {
        public int SourceIndex = 0;
        public SteamIndexableParticleEmitterData(ParticleEmitterTypes type, int sourceIndex) : base(type)
        {
            SourceIndex = sourceIndex;
        }
    }

    /// <summary>
    /// ParticleEmitterData with additional data and default values modified as
    /// appropriate for particles that represent steam cylinder exhaust
    /// </summary>
    public class SteamCylinderParticleEmitterData : SteamParticleEmitterData
    {
        public int SourceEngineIndex = 0;
        public int SourceCylinderIndex = 0;
        public SteamCylinderParticleEmitterData(ParticleEmitterTypes type, int engineIndex, int cylinderIndex) : base(type)
        {
            // Steam from cylinders comes out in huge volumes, so only spawn 1/10th the number of particles by default
            RateFactor = 0.1f;
            SourceEngineIndex = engineIndex;
            SourceCylinderIndex = cylinderIndex;
        }
    }

    /// <summary>
    /// ParticleEmitterData with default values modified as appropriate for
    /// particles that represent water
    /// </summary>
    public class WaterParticleEmitterData : ParticleEmitterData
    {
        public WaterParticleEmitterData(ParticleEmitterTypes type) : base(type)
        {
            // Unlike particles for gasses, water particles should fall downward
            FinalVelocityMpS = Vector3.Down;
            Graphic = "smokemain.ace";
        }
    }

    /// <summary>
    /// ParticleEmitterData with default values modified as appropriate for
    /// particles that represent burning diesel
    /// </summary>
    public class DieselParticleEmitterData : ParticleEmitterData
    {
        public DieselParticleEmitterData(ParticleEmitterTypes type) : base(type)
        {
            Graphic = "dieselsmoke.ace";
        }
    }

    /// <summary>
    /// ParticleEmitterData with additional data and default values modified as
    /// appropriate for particles that represent combustion engine exhaust
    /// </summary>
    public class DieselEngineParticleEmitterData : DieselParticleEmitterData
    {
        public int SourceEngineIndex = 0;

        public DieselEngineParticleEmitterData(ParticleEmitterTypes type, int index) : base(type)
        {
            SourceEngineIndex = index;
        }
    }
}
