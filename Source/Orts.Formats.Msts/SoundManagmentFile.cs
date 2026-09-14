// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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

using System.Collections.Generic;
using Orts.Parsers.Msts;

namespace Orts.Formats.Msts
{

    /// <summary>
    /// Utility class to avoid loading multiple copies of the same file.
    /// </summary>
    public class SharedSMSFileManager
    {
        private static Dictionary<string, SoundManagmentFile> SharedSMSFiles = new Dictionary<string, SoundManagmentFile>();

        public static int SwitchSMSNumber;
        public static int CurveSMSNumber;
        public static int CurveSwitchSMSNumber;
        public static bool AutoTrackSound = false;

        public static bool PlayDefaultTrackSoundsContinuous = false;
        public static float ConcreteSleepers;

        public static SoundManagmentFile Get(string path)
        {
            if (!SharedSMSFiles.ContainsKey(path))
            {
                SoundManagmentFile smsFile = new SoundManagmentFile(path);
                SharedSMSFiles.Add(path, smsFile);
                return smsFile;
            }
            else
            {
                return SharedSMSFiles[path];
            }
        }
    }

	/// <summary>
	/// Represents the hiearchical structure of the SMS File
	/// </summary>
	public class SoundManagmentFile
	{
		public Tr_SMS Tr_SMS;

		public SoundManagmentFile( string filePath )
		{
            ReadFile(filePath);  
        }

        private void ReadFile(string filePath)
        {
            using (STFReader stf = new STFReader(filePath, false))
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("tr_sms", ()=>{ Tr_SMS = new Tr_SMS(stf); }),
                });
        }
    } // class SMSFile

    public class Tr_SMS
    {
        public List<ScalabiltyGroup> ScalabiltyGroups = new List<ScalabiltyGroup>();
        
        public Tr_SMS(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("scalabiltygroup", ()=>{ ScalabiltyGroups.Add(new ScalabiltyGroup(stf)); }),
            });
        }
    } // class Tr_SMS

    public partial class ScalabiltyGroup
    {
        public int DetailLevel;
        public SMSStreams Streams;
        public float Volume = 1.0f;
        public bool Stereo;
        public bool Ignore3D;
        public Activation Activation;
        public Deactivation Deactivation;

        public ScalabiltyGroup(STFReader stf)
        {
            stf.MustMatch("(");
            DetailLevel = stf.ReadInt(null);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("activation", ()=>{ Activation = new Activation(stf); }),
                new STFReader.TokenProcessor("deactivation", ()=>{ Deactivation = new Deactivation(stf); }),
                new STFReader.TokenProcessor("streams", ()=>{ Streams = new SMSStreams(stf); }),
                new STFReader.TokenProcessor("volume", ()=>{ Volume = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("stereo", ()=>{ Stereo = stf.ReadBoolBlock(true); }),
                new STFReader.TokenProcessor("ignore3d", ()=>{ Ignore3D = stf.ReadBoolBlock(true); }),
            });
        }
    } // class ScalabiltyGroup

    public class Activation
    {
        public bool ExternalCam;
        public bool CabCam;
        public bool PassengerCam;
        public float Distance = 1000;  // by default we are 'in range' to hear this
        public int TrackType = -1;

        public Activation(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("externalcam", ()=>{ ExternalCam = stf.ReadBoolBlock(true); }),
                new STFReader.TokenProcessor("cabcam", ()=>{ CabCam = stf.ReadBoolBlock(true); }),
                new STFReader.TokenProcessor("passengercam", ()=>{ PassengerCam = stf.ReadBoolBlock(true); }),
                new STFReader.TokenProcessor("distance", ()=>{ Distance = stf.ReadFloatBlock(STFReader.UNITS.Distance, Distance); }),
                new STFReader.TokenProcessor("tracktype", ()=>{ TrackType = stf.ReadIntBlock(null); }),
            });
        }

        // for precompiled sound sources for activity sound
        public Activation()
        { }

    }

    public class Deactivation: Activation
    {
        public Deactivation(STFReader stf): base(stf)
        {
        }

        // for precompiled sound sources for activity sound
        public Deactivation(): base()
        { }
    }

    public class SMSStreams : List<SMSStream>
    {
        public SMSStreams(STFReader stf)
        {
            stf.MustMatch("(");
            var count = stf.ReadInt(null);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("stream", ()=>{
                    if (--count < 0)
                        STFException.TraceWarning(stf, "Skipped extra Stream");
                    else
                        Add(new SMSStream(stf));
                }),
            });
            if (count > 0)
                STFException.TraceWarning(stf, count + " missing Stream(s)");
        }
    }

    public class SMSStream
    {
        public int Priority;
        public Triggers Triggers;
        public float Volume = 1.0f;
        public List<VolumeCurve> VolumeCurves = new List<VolumeCurve>();
        public FrequencyCurve FrequencyCurve;
        public bool[] Season;
        public bool[] Weather;
        public int[] TimeInterval;
        public List<int[]> TimeIntervals;



        public SMSStream(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("priority", ()=>{ Priority = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("triggers", ()=>{ Triggers = new Triggers(stf); }),
                new STFReader.TokenProcessor("volumecurve", ()=>{ VolumeCurves.Add(new VolumeCurve(stf)); }),
                new STFReader.TokenProcessor("frequencycurve", ()=>{ FrequencyCurve = new FrequencyCurve(stf); }),
                new STFReader.TokenProcessor("volume", ()=>{ Volume = stf.ReadFloatBlock(STFReader.UNITS.None, Volume); }),
                new STFReader.TokenProcessor("ortstimeofday", ()=>{
                    if (TimeIntervals == null)
                        TimeIntervals = new List<int[]>();
                    var timeInterval = new int[2];
                    stf.MustMatch("(");
                    timeInterval[0] = stf.ReadInt(null);
                    timeInterval[1] = stf.ReadInt(null);
                    TimeIntervals.Add(timeInterval);
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("ortsseason", ()=>{ 
                    Season = new bool[4];
                    stf.MustMatch("(");
                    stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("spring", ()=>{ if(stf.ReadBoolBlock(true))
                                Season[(int)SeasonType.Spring] = true; }),
                        new STFReader.TokenProcessor("summer", ()=>{ if(stf.ReadBoolBlock(true))
                                Season[(int)SeasonType.Summer] = true; }),
                        new STFReader.TokenProcessor("autumn", ()=>{ if(stf.ReadBoolBlock(true))
                                Season[(int)SeasonType.Autumn] = true; }),
                        new STFReader.TokenProcessor("winter", ()=>{ if(stf.ReadBoolBlock(true))
                                Season[(int)SeasonType.Winter] = true; }),
                    });
                }),
                new STFReader.TokenProcessor("ortsweather", ()=>{
                    Weather = new bool[3];
                    stf.MustMatch("(");
                    stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("clear", ()=>{ if(stf.ReadBoolBlock(true))
                                Weather[(int)WeatherType.Clear] = true; }),
                        new STFReader.TokenProcessor("snow", ()=>{ if(stf.ReadBoolBlock(true))
                                Weather[(int)WeatherType.Snow] = true; }),
                        new STFReader.TokenProcessor("rain", ()=>{ if(stf.ReadBoolBlock(true))
                                Weather[(int)WeatherType.Rain] = true; }),
                    });
                }),
            });
            //if (Volume > 1)  Volume /= 100f;
        }
    }

    public struct CurvePoint
    {
        public float X, Y;
    }

    public class SoundVariable
    {
        public enum ControlType
        {
            None,
            Distance,
            Speed,
            Variable1,
            Variable2,
            Variable2Booster,
            Variable3,
            BrakeCyl,
            CurveForce,
            AngleofAttack,
            CarFriction,
            WheelRPM,
            ConcreteSleepers,
            CarInTunnel,
            CarDistanceTrack,
            CarTunnelDistance,
            BackPressure,
            TractiveEffort,
            TractivePower,
            EngineRPM,
            EnginePower,
            EngineTorque
        };

        public ControlType Control; // What controls this SoundVariable
        public int SourceID = -1; // Disambiguation for cases where there are multiple data points possible

        /// <summary>
        /// Creates an empty SoundVariable object with the ControlType set to none. This variable will do nothing if used.
        /// </summary>
        public SoundVariable()
        {
            Control = ControlType.None;
        }

        /// <summary>
        /// Creates a new SoundVariable object using the string name of the sound variable to determine the data source of the sound.
        /// </summary>
        /// <param name="controlName">The string name of the control, less any prexfixes or suffixes (such as "_Inc_Past")</param>
        public SoundVariable(string controlName)
        {
            switch (controlName.ToLower())
            {
                case "distance": Control = ControlType.Distance; break;
                case "speed": Control = ControlType.Speed; break;
                case "variable1": Control = ControlType.Variable1; SourceID = 0; break;
                case "variable2": Control = ControlType.Variable2; break;
                case "variable2booster": Control = ControlType.Variable2Booster; break;
                case "variable3": Control = ControlType.Variable3; break;
                case "brakecyl": Control = ControlType.BrakeCyl; break;
                case "curveforce": Control = ControlType.CurveForce; break;
                case "angleofattack": Control = ControlType.AngleofAttack; break;
                case "carfriction": Control = ControlType.CarFriction; break;
                case "wheelrpm": Control = ControlType.WheelRPM; break;
                case "concretesleepers": Control = ControlType.ConcreteSleepers; break;
                case "carintunnel": Control = ControlType.CarInTunnel; break;
                case "cardistancetrack": Control = ControlType.CarDistanceTrack; break;
                case "cartunneldistance": Control = ControlType.CarTunnelDistance; break;
                case "backpressure": Control = ControlType.BackPressure; break;
                case "tractiveeffort": Control = ControlType.TractiveEffort; break;
                case "tractivepower": Control = ControlType.TractivePower; break;
                // Below are special cases for sound variables that can accept arbitrary syntax
                case string s when s.StartsWith("variable1_"): // Variable1_[X], eg: Variable1_5
                    Control = ControlType.Variable1;
                    SourceID = GetIndexInString(controlName, "variable1_", "");
                    break;
                case string s when s.StartsWith("engine") && s.EndsWith("rpm"): // Engine[X]RPM, eg: Engine5RPM
                    Control = ControlType.EngineRPM;
                    SourceID = GetIndexInString(controlName, "engine", "rpm");
                    break;
                case string s when s.StartsWith("engine") && s.EndsWith("power"): // Engine[X]Power, eg: Engine5Power
                    Control = ControlType.EnginePower;
                    SourceID = GetIndexInString(controlName, "engine", "power");
                    break;
                case string s when s.StartsWith("engine") && s.EndsWith("torque"): // Engine[X]Torque, eg: Engine5Torque
                    Control = ControlType.EngineTorque;
                    SourceID = GetIndexInString(controlName, "engine", "torque");
                    break;
                default: Control = ControlType.None; break;
            }
        }

        /// <summary>
        /// Determines the type of units of measure that can be used to measure this SoundVariable
        /// </summary>
        /// <returns>STFReader.UNITS value representing the type of units to parse for this SoundVariable</returns>
        public STFReader.UNITS GetUnits()
        {
            switch (Control)
            {
                case ControlType.Distance:
                case ControlType.CarDistanceTrack:
                case ControlType.CarTunnelDistance:
                    // Distance, track distance, and tunnel distance are all meters
                    return STFReader.UNITS.Distance;
                case ControlType.Speed:
                    // Speed is meters per second
                    return STFReader.UNITS.Speed;
                case ControlType.BrakeCyl:
                case ControlType.BackPressure:
                    // Brake cylinder and back pressure are PSI
                    return STFReader.UNITS.PressureDefaultPSI;
                case ControlType.TractiveEffort:
                case ControlType.CurveForce:
                    // Tractive effort and curve force are newtons
                    return STFReader.UNITS.Force;
                case ControlType.TractivePower:
                case ControlType.EnginePower:
                    // Tractive power and engine power are watts
                    return STFReader.UNITS.Power;
                case ControlType.EngineRPM:
                case ControlType.WheelRPM:
                    // TODO: Currently there is no STF unit type for rotation speed
                    return STFReader.UNITS.None;
                case ControlType.EngineTorque:
                    // TODO: Currently there is no STF unit type for torque
                    return STFReader.UNITS.None;
                case ControlType.AngleofAttack:
                    // TODO: Currently there is no STF unit type for milliradians
                    return STFReader.UNITS.None;
                default: // All other quanities are unitless/dimensionless
                    return STFReader.UNITS.None;
            }
        }

        /// <summary>
        /// Attemps to extract an integer index contained in the middle of a given string by
        /// removing specified text from the beginning and end of the string to isolate the
        /// index in the middle. Returns 0 if the value in the middle could not be interpreted
        /// as a number.
        /// </summary>
        /// <param name="main">The complete string to return an integer from.</param>
        /// <param name="beginning">The (case-insensitive) part of the string that occurs before the index.</param>
        /// <param name="end">The (case-insensitive) part of the string that occurs after the index.</param>
        /// <returns>The integer value contained within the middle of the string,
        /// or 0 if no such value could be found.</returns>
        public static int GetIndexInString(string main, string beginning, string end)
        {
            // Replace the specified beginning and end strings inside the main string with empty space
            // Resulting string should contain only the desired index, nothing else
            string indexStr = main.ToLower().Replace(beginning.ToLower(), "").Replace(end.ToLower(), "");

            // If index can't be determined, assume the user wanted the first thing
            if (!int.TryParse(indexStr, out int index))
                index = 1;

            index--; // User input will be 1-indexed, but code needs this value as a 0-index

            return index;
        }
    }

    public class VolumeCurve
    {
        public SoundVariable CurveVariable;
        public float Granularity = 1.0f;

        public CurvePoint[] CurvePoints;

        public VolumeCurve(STFReader stf)
        {
            stf.MustMatch("(");
            string curveType = stf.ReadString();
            string variableType = curveType.ToLower();
            if (variableType.EndsWith("controlled")) // All curve variables end with "controlled"
            {
                // Remove "controlled" from the end to get the control type
                variableType = variableType.Substring(0, variableType.LastIndexOf("controlled"));
                CurveVariable = new SoundVariable(variableType);
            }
            if (CurveVariable == null || CurveVariable.Control == SoundVariable.ControlType.None)
            {
                STFException.TraceWarning(stf, "Crash expected: Skipped unknown VolumeCurve/Frequencycurve type " + curveType);
                CurveVariable = new SoundVariable();
                stf.SkipRestOfBlock();
                return;
            }
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("granularity", ()=>{ Granularity = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("curvepoints", ()=>{
                    stf.MustMatch("(");
                    int count = stf.ReadInt(null);
                    CurvePoints = new CurvePoint[count];
                    STFReader.UNITS units = CurveVariable.GetUnits();
                    for (int i = 0; i < count; ++i)
                    {
                        CurvePoints[i].X = stf.ReadFloat(units, null);
                        if (CurveVariable.Control == SoundVariable.ControlType.Distance)
                        {
                            // Convert distance into square distance
							if (CurvePoints[i].X >= 0) CurvePoints[i].X *= CurvePoints[i].X;
							else CurvePoints[i].X *= -CurvePoints[i].X;
						}
                        CurvePoints[i].Y = stf.ReadFloat(STFReader.UNITS.None, null);
                    }
                    stf.SkipRestOfBlock();
                }),
            });
        }
    }

    public class FrequencyCurve: VolumeCurve
    {
        public FrequencyCurve(STFReader stf)
            : base(stf)
        {
        }
    }


    public class Triggers : List<Trigger>
    {
        public Triggers(STFReader stf)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(null);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("dist_travelled_trigger", ()=>{ Add(new Dist_Travelled_Trigger(stf)); }),
                new STFReader.TokenProcessor("joint_2axle_trigger", ()=>{ Add(new Joint_Trigger_2(stf)); }),
                new STFReader.TokenProcessor("joint_3axle_trigger", ()=>{ Add(new Joint_Trigger_3(stf)); }),
                new STFReader.TokenProcessor("joint_4axle_trigger", ()=>{ Add(new Joint_Trigger_4(stf)); }),
                new STFReader.TokenProcessor("joint_6axle_trigger", ()=>{ Add(new Joint_Trigger_6(stf)); }),
                new STFReader.TokenProcessor("joint_8axle_trigger", ()=>{ Add(new Joint_Trigger_8(stf)); }),
                new STFReader.TokenProcessor("switch_2axle_trigger", ()=>{ Add(new Switch_Trigger_2(stf)); }),
                new STFReader.TokenProcessor("switch_3axle_trigger", ()=>{ Add(new Switch_Trigger_3(stf)); }),
                new STFReader.TokenProcessor("switch_4axle_trigger", ()=>{ Add(new Switch_Trigger_4(stf)); }),
                new STFReader.TokenProcessor("switch_6axle_trigger", ()=>{ Add(new Switch_Trigger_6(stf)); }),
                new STFReader.TokenProcessor("switch_8axle_trigger", ()=>{ Add(new Switch_Trigger_8(stf)); }),
                new STFReader.TokenProcessor("xover_2axle_trigger", ()=>{ Add(new Xover_Trigger_2(stf)); }),
                new STFReader.TokenProcessor("xover_3axle_trigger", ()=>{ Add(new Xover_Trigger_3(stf)); }),
                new STFReader.TokenProcessor("xover_4axle_trigger", ()=>{ Add(new Xover_Trigger_4(stf)); }),
                new STFReader.TokenProcessor("xover_6axle_trigger", ()=>{ Add(new Xover_Trigger_6(stf)); }),
                new STFReader.TokenProcessor("xover_8axle_trigger", ()=>{ Add(new Xover_Trigger_8(stf)); }),
                new STFReader.TokenProcessor("discrete_trigger", ()=>{ Add(new Discrete_Trigger(stf)); }),
                new STFReader.TokenProcessor("random_trigger", ()=>{ Add(new Random_Trigger(stf)); }),
                new STFReader.TokenProcessor("variable_trigger", ()=>{ Add(new Variable_Trigger(stf)); }),
                new STFReader.TokenProcessor("initial_trigger", ()=>{ Add(new Initial_Trigger(stf)); }),
            });
            foreach (Trigger trigger in this)
                if (trigger.SoundCommand == null)
                    STFException.TraceWarning(stf, "Trigger lacks a sound command");
        }
    }

    public class Trigger
    {
        public SoundCommand SoundCommand;

        int playcommandcount;

        protected void ParsePlayCommand(STFReader f, string lowertoken)
        {
            switch (lowertoken)
            {
                case "playoneshot": 
                case "startloop":
                case "releaselooprelease": 
                case "startlooprelease":
                case "releaseloopreleasewithjump": 
                case "disabletrigger": 
                case "enabletrigger": 
                case "setstreamvolume":
                    ++playcommandcount;
                    if (playcommandcount > 1)
                        STFException.TraceWarning(f, "Replaced play command");
                    break;
                default:
                    break;
            }

            switch (lowertoken)
            {
                case "playoneshot": SoundCommand = new PlayOneShot(f); break;
                case "startloop": SoundCommand = new StartLoop(f); break;
                case "releaselooprelease":  SoundCommand = new ReleaseLoopRelease(f); break; 
                case "startlooprelease":  SoundCommand = new StartLoopRelease( f ); break; 
                case "releaseloopreleasewithjump": SoundCommand = new ReleaseLoopReleaseWithJump( f ); break; 
                case "disabletrigger": SoundCommand = new DisableTrigger( f); break; 
                case "enabletrigger": SoundCommand = new EnableTrigger( f); break;
                case "setstreamvolume": SoundCommand = new SetStreamVolume(f); break;
                case "(": f.SkipRestOfBlock(); break;
            }
        }
    }

    public class Initial_Trigger : Trigger
    {

        public Initial_Trigger(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
                ParsePlayCommand(f, f.ReadString().ToLower());
        }
    }

    public class Discrete_Trigger : Trigger
    {

        public int TriggerID;

        public Discrete_Trigger(STFReader f)
        {
            f.MustMatch("(");
            TriggerID = f.ReadInt(null);
            while (!f.EndOfBlock())
                ParsePlayCommand(f, f.ReadString().ToLower());
        }
    }

    public class Variable_Trigger : Trigger
    {
        public enum TriggerType
        {
            None,
            Inc_Past,
            Dec_Past
        }

        public SoundVariable TriggerVariable;
        public TriggerType Type = TriggerType.None;
        public float Threshold;

        public Variable_Trigger(STFReader stf)
        {
            stf.MustMatch("(");

            string triggerType = stf.ReadString();
            string variableType = triggerType.ToLower();
            // All variable triggers end with "_Inc_Past" or "_Dec_Past"
            if (variableType.EndsWith("_inc_past"))
            {
                // Remove "_Inc_Past" from the end to get the control type
                Type = TriggerType.Inc_Past;
                variableType = variableType.Substring(0, variableType.LastIndexOf("_inc_past"));
                TriggerVariable = new SoundVariable(variableType);
            }
            else if (variableType.EndsWith("_dec_past"))
            {
                // Remove "_Dec_Past" from the end to get the control type
                Type = TriggerType.Dec_Past;
                variableType = variableType.Substring(0, variableType.LastIndexOf("_dec_past"));
                TriggerVariable = new SoundVariable(variableType);
            }
            if (TriggerVariable == null || TriggerVariable.Control == SoundVariable.ControlType.None)
            {
                STFException.TraceWarning(stf, "Crash expected: Skipped unknown variable trigger type " + triggerType);
                TriggerVariable = new SoundVariable();
                stf.SkipRestOfBlock();
                return;
            }

            STFReader.UNITS units = TriggerVariable.GetUnits();
            Threshold = stf.ReadFloat(units, null);

            if (TriggerVariable.Control == SoundVariable.ControlType.Distance)
            {
                // Convert distance into square distance
                Threshold = Threshold * Threshold;
            }

            while (!stf.EndOfBlock())
                ParsePlayCommand(stf, stf.ReadString().ToLower());
        }
    }

    public class Dist_Travelled_Trigger : Trigger
    {
        public float Dist_Min = 80;
        public float Dist_Max = 100;
        public float Volume_Min = 0.9f;
        public float Volume_Max = 1.0f;

        public Dist_Travelled_Trigger(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
            {
                string lowtok = f.ReadString().ToLower();
                switch (lowtok)
                {
                    case "dist_min_max": f.MustMatch("("); Dist_Min = f.ReadFloat(STFReader.UNITS.Distance, null); Dist_Max = f.ReadFloat(STFReader.UNITS.Distance, null); f.SkipRestOfBlock(); break;
                    case "volume_min_max": f.MustMatch("("); Volume_Min = f.ReadFloat(STFReader.UNITS.None, null); Volume_Max = f.ReadFloat(STFReader.UNITS.None, null); f.SkipRestOfBlock(); break;
                    default: ParsePlayCommand(f, lowtok); break;
                }
            }
        }
    }

    public class Joint_Trigger_2 : Trigger
    {
        public float Car_Joint_Camera_DistM = 80;
        public float Volume_Min = 0.9f;
        public float Volume_Max = 1.0f;

        public Joint_Trigger_2(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
            {
                string lowtok = f.ReadString().ToLower();
                switch (lowtok)
                {
                    case "car_camera_dist": f.MustMatch("("); Car_Joint_Camera_DistM = f.ReadFloat(STFReader.UNITS.Distance, null); f.SkipRestOfBlock(); break;
                    //      case "volume_min_max": f.MustMatch("("); Volume_Min = f.ReadFloat(STFReader.UNITS.None, null); Volume_Max = f.ReadFloatSTFReader.UNITS.None, null); f.SkipRestOfBlock(); break;
                    default: ParsePlayCommand(f, lowtok); break;
                }
            }
        }
    }

    public class Joint_Trigger_3 : Trigger
    {
        public float Car_Joint_Camera_DistM = 80;
        public float Volume_Min = 0.9f;
        public float Volume_Max = 1.0f;

        public Joint_Trigger_3(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
            {
                string lowtok = f.ReadString().ToLower();
                switch (lowtok)
                {
                    case "car_camera_dist": f.MustMatch("("); Car_Joint_Camera_DistM = f.ReadFloat(STFReader.UNITS.Distance, null); f.SkipRestOfBlock(); break;
                    //      case "volume_min_max": f.MustMatch("("); Volume_Min = f.ReadFloat(STFReader.UNITS.None, null); Volume_Max = f.ReadFloat(STFReader.UNITS.None, null); f.SkipRestOfBlock(); break;
                    default: ParsePlayCommand(f, lowtok); break;
                }
            }
        }
    }
    public class Joint_Trigger_4 : Trigger
    {
        public float Car_Joint_Camera_DistM = 80;
        public float Volume_Min = 0.9f;
        public float Volume_Max = 1.0f;

        public Joint_Trigger_4(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
            {
                string lowtok = f.ReadString().ToLower();
                switch (lowtok)
                {
                    case "car_camera_dist": f.MustMatch("("); Car_Joint_Camera_DistM = f.ReadFloat(STFReader.UNITS.Distance, null); f.SkipRestOfBlock(); break;
              //      case "volume_min_max": f.MustMatch("("); Volume_Min = f.ReadFloat(STFReader.UNITS.None, null); Volume_Max = f.ReadFloat(STFReader.UNITS.None, null); f.SkipRestOfBlock(); break;
                    default: ParsePlayCommand(f, lowtok); break;
                }
            }
        }
    }

    public class Joint_Trigger_6 : Trigger
    {
        public float Car_Joint_Camera_DistM = 80;
        public float Volume_Min = 0.9f;
        public float Volume_Max = 1.0f;

        public Joint_Trigger_6(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
            {
                string lowtok = f.ReadString().ToLower();
                switch (lowtok)
                {
                    case "car_camera_dist": f.MustMatch("("); Car_Joint_Camera_DistM = f.ReadFloat(STFReader.UNITS.Distance, null); f.SkipRestOfBlock(); break;
                    //      case "volume_min_max": f.MustMatch("("); Volume_Min = f.ReadFloat(STFReader.UNITS.None, null); Volume_Max = f.ReadFloat(STFReader.UNITS.None, null); f.SkipRestOfBlock(); break;
                    default: ParsePlayCommand(f, lowtok); break;
                }
            }
        }
    }

    public class Joint_Trigger_8 : Trigger
    {
        public float Car_Joint_Camera_DistM = 80;
        public float Volume_Min = 0.9f;
        public float Volume_Max = 1.0f;

        public Joint_Trigger_8(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
            {
                string lowtok = f.ReadString().ToLower();
                switch (lowtok)
                {
                    case "car_camera_dist": f.MustMatch("("); Car_Joint_Camera_DistM = f.ReadFloat(STFReader.UNITS.Distance, null); f.SkipRestOfBlock(); break;
                    //      case "volume_min_max": f.MustMatch("("); Volume_Min = f.ReadFloat(STFReader.UNITS.None, null); Volume_Max = f.ReadFloatSTFReader.UNITS.None, null); f.SkipRestOfBlock(); break;
                    default: ParsePlayCommand(f, lowtok); break;
                }
            }
        }
    }

    public class Switch_Trigger_2 : Trigger
    {
        public float Car_Switch_Camera_DistM = 80;
        public float Volume_Min = 0.9f;
        public float Volume_Max = 1.0f;

        public Switch_Trigger_2(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
            {
                string lowtok = f.ReadString().ToLower();
                switch (lowtok)
                {
                    case "car_camera_dist": f.MustMatch("("); Car_Switch_Camera_DistM = f.ReadFloat(STFReader.UNITS.Distance, null); f.SkipRestOfBlock(); break;
                    default: ParsePlayCommand(f, lowtok); break;
                }
            }
        }
    }

    public class Switch_Trigger_3 : Trigger
    {
        public float Car_Switch_Camera_DistM = 80;
        public float Volume_Min = 0.9f;
        public float Volume_Max = 1.0f;

        public Switch_Trigger_3(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
            {
                string lowtok = f.ReadString().ToLower();
                switch (lowtok)
                {
                    case "car_camera_dist": f.MustMatch("("); Car_Switch_Camera_DistM = f.ReadFloat(STFReader.UNITS.Distance, null); f.SkipRestOfBlock(); break;
                    default: ParsePlayCommand(f, lowtok); break;
                }
            }
        }
    }

    public class Switch_Trigger_4 : Trigger
    {
        public float Car_Switch_Camera_DistM = 80;
        public float Volume_Min = 0.9f;
        public float Volume_Max = 1.0f;

        public Switch_Trigger_4(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
            {
                string lowtok = f.ReadString().ToLower();
                switch (lowtok)
                {
                    case "car_camera_dist": f.MustMatch("("); Car_Switch_Camera_DistM = f.ReadFloat(STFReader.UNITS.Distance, null); f.SkipRestOfBlock(); break;
                    default: ParsePlayCommand(f, lowtok); break;
                }
            }
        }
    }

    public class Switch_Trigger_6 : Trigger
    {
        public float Car_Switch_Camera_DistM = 80;
        public float Volume_Min = 0.9f;
        public float Volume_Max = 1.0f;

        public Switch_Trigger_6(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
            {
                string lowtok = f.ReadString().ToLower();
                switch (lowtok)
                {
                    case "car_camera_dist": f.MustMatch("("); Car_Switch_Camera_DistM = f.ReadFloat(STFReader.UNITS.Distance, null); f.SkipRestOfBlock(); break;
                    default: ParsePlayCommand(f, lowtok); break;
                }
            }
        }
    }

    public class Switch_Trigger_8 : Trigger
    {
        public float Car_Switch_Camera_DistM = 80;
        public float Volume_Min = 0.9f;
        public float Volume_Max = 1.0f;

        public Switch_Trigger_8(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
            {
                string lowtok = f.ReadString().ToLower();
                switch (lowtok)
                {
                    case "car_camera_dist": f.MustMatch("("); Car_Switch_Camera_DistM = f.ReadFloat(STFReader.UNITS.Distance, null); f.SkipRestOfBlock(); break;
                    default: ParsePlayCommand(f, lowtok); break;
                }
            }
        }
    }

    public class Xover_Trigger_2 : Trigger
    {
        public float Car_Xover_Camera_DistM = 80;
        public float Volume_Min = 0.9f;
        public float Volume_Max = 1.0f;

        public Xover_Trigger_2(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
            {
                string lowtok = f.ReadString().ToLower();
                switch (lowtok)
                {
                    case "car_camera_dist": f.MustMatch("("); Car_Xover_Camera_DistM = f.ReadFloat(STFReader.UNITS.Distance, null); f.SkipRestOfBlock(); break;
                    default: ParsePlayCommand(f, lowtok); break;
                }
            }
        }
    }

    public class Xover_Trigger_3 : Trigger
    {
        public float Car_Xover_Camera_DistM = 80;
        public float Volume_Min = 0.9f;
        public float Volume_Max = 1.0f;

        public Xover_Trigger_3(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
            {
                string lowtok = f.ReadString().ToLower();
                switch (lowtok)
                {
                    case "car_camera_dist": f.MustMatch("("); Car_Xover_Camera_DistM = f.ReadFloat(STFReader.UNITS.Distance, null); f.SkipRestOfBlock(); break;
                    default: ParsePlayCommand(f, lowtok); break;
                }
            }
        }
    }

    public class Xover_Trigger_4 : Trigger
    {
        public float Car_Xover_Camera_DistM = 80;
        public float Volume_Min = 0.9f;
        public float Volume_Max = 1.0f;

        public Xover_Trigger_4(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
            {
                string lowtok = f.ReadString().ToLower();
                switch (lowtok)
                {
                    case "car_camera_dist": f.MustMatch("("); Car_Xover_Camera_DistM = f.ReadFloat(STFReader.UNITS.Distance, null); f.SkipRestOfBlock(); break;
                    default: ParsePlayCommand(f, lowtok); break;
                }
            }
        }
    }

    public class Xover_Trigger_6 : Trigger
    {
        public float Car_Xover_Camera_DistM = 80;
        public float Volume_Min = 0.9f;
        public float Volume_Max = 1.0f;

        public Xover_Trigger_6(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
            {
                string lowtok = f.ReadString().ToLower();
                switch (lowtok)
                {
                    case "car_camera_dist": f.MustMatch("("); Car_Xover_Camera_DistM = f.ReadFloat(STFReader.UNITS.Distance, null); f.SkipRestOfBlock(); break;
                    default: ParsePlayCommand(f, lowtok); break;
                }
            }
        }
    }

    public class Xover_Trigger_8 : Trigger
    {
        public float Car_Xover_Camera_DistM = 80;
        public float Volume_Min = 0.9f;
        public float Volume_Max = 1.0f;

        public Xover_Trigger_8(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
            {
                string lowtok = f.ReadString().ToLower();
                switch (lowtok)
                {
                    case "car_camera_dist": f.MustMatch("("); Car_Xover_Camera_DistM = f.ReadFloat(STFReader.UNITS.Distance, null); f.SkipRestOfBlock(); break;
                    default: ParsePlayCommand(f, lowtok); break;
                }
            }
        }
    }

    public class Random_Trigger : Trigger
    {
        public float Delay_Min = 80;
        public float Delay_Max = 100;
        public float Volume_Min = 0.9f;
        public float Volume_Max = 1.0f;

        public Random_Trigger(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
            {
                string lowtok = f.ReadString().ToLower();
                switch (lowtok)
                {
                    case "delay_min_max": f.MustMatch("("); Delay_Min = f.ReadFloat(STFReader.UNITS.None, null); Delay_Max = f.ReadFloat(STFReader.UNITS.None, null); f.SkipRestOfBlock(); break;
                    case "volume_min_max": f.MustMatch("("); Volume_Min = f.ReadFloat(STFReader.UNITS.None, null); Volume_Max = f.ReadFloat(STFReader.UNITS.None, null); f.SkipRestOfBlock(); break;
                    default: ParsePlayCommand(f, lowtok); break;
                }
            }
        }
    }
    public class SoundCommand
    {
        public enum SelectionMethods { RandomSelection, SequentialSelection };
    }

    public class SetStreamVolume : SoundCommand
    {
        public float Volume;

        public SetStreamVolume(STFReader f)
        {
            f.MustMatch("(");
            Volume = f.ReadFloat(STFReader.UNITS.None, null);
            f.SkipRestOfBlock();
        }
    }

    public class DisableTrigger : SoundCommand
    {
        public int TriggerID;

        public DisableTrigger(STFReader f)
        {
            f.MustMatch("(");
            TriggerID = f.ReadInt(null);
            f.SkipRestOfBlock();
        }
    }

    public class EnableTrigger : DisableTrigger
    {
        public EnableTrigger(STFReader f)
            : base(f)
        {
        }
    }

    public class ReleaseLoopRelease : SoundCommand
    {
        public ReleaseLoopRelease(STFReader f)
        {
            f.MustMatch("(");
            f.SkipRestOfBlock();
        }
    }

    public class ReleaseLoopReleaseWithJump : SoundCommand
    {
        public ReleaseLoopReleaseWithJump(STFReader f)
        {
            f.MustMatch("(");
            f.SkipRestOfBlock();
        }
    }

    public class SoundPlayCommand: SoundCommand
    {
        public string[] Files;
        public SelectionMethods SelectionMethod = SelectionMethods.SequentialSelection;
    }

    public class PlayOneShot : SoundPlayCommand
    {
        
        public PlayOneShot(STFReader f)
        {
            f.MustMatch("(");
            int count = f.ReadInt(null);
            Files = new string[count];
            int iFile = 0;
            while (!f.EndOfBlock())
                switch (f.ReadString().ToLower())
                {
                    case "file":
                        if (iFile < count)
                        {
                            f.MustMatch("(");
                            Files[iFile++] = f.ReadString();
                            f.ReadInt(null);
                            f.SkipRestOfBlock();
                        }
                        else  // MSTS skips extra files
                        {
                            STFException.TraceWarning(f, "Skipped extra File");
                            f.SkipBlock();
                        }
                        break;
                    case "selectionmethod":
                        f.MustMatch("(");
                        string s = f.ReadString();
                        switch (s.ToLower())
                        {
                            case "randomselection": SelectionMethod = SelectionMethods.RandomSelection; break;
                            case "sequentialselection": SelectionMethod = SelectionMethods.SequentialSelection; break;
                            default: STFException.TraceWarning(f, "Skipped unknown selection method " + s); break;
                        }
                        f.SkipRestOfBlock();
                        break;
                    case "(": f.SkipRestOfBlock(); break;
                }
        }
    }// PlayOneShot

    public class StartLoop : PlayOneShot
    {
        public StartLoop( STFReader f ): base(f)
        {
        }
    }

    public class StartLoopRelease : PlayOneShot
    {
        public StartLoopRelease(STFReader f)
            : base(f)
        {
        }
    }


} // namespace
