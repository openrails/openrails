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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Orts.Formats.Msts;
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

    public class VolumeCurve
    {
        public enum Controls { None, DistanceControlled, SpeedControlled, Variable1Controlled, Variable1_2Controlled, Variable1_3Controlled, Variable1_4Controlled, Variable2Controlled, Variable2BoosterControlled, Variable3Controlled, BrakeCylControlled, CurveForceControlled, AngleofAttackControlled, CarFrictionControlled, WheelRpMControlled, CarDistanceTrackControlled, CarTunnelDistanceControlled, BackPressureControlled };

        public Controls Control = Controls.None;
        public float Granularity = 1.0f;

        public CurvePoint[] CurvePoints;

        public VolumeCurve(STFReader stf)
        {
            stf.MustMatch("(");
            var type = stf.ReadString();
            switch (type.ToLower())
            {
                case "distancecontrolled": Control = Controls.DistanceControlled; break;
                case "speedcontrolled": Control = Controls.SpeedControlled; break;
                case "variable1controlled": Control = Controls.Variable1Controlled; break;
                case "variable1_2controlled": Control = Controls.Variable1_2Controlled; break;
                case "variable1_3controlled": Control = Controls.Variable1_3Controlled; break;
                case "variable1_4controlled": Control = Controls.Variable1_4Controlled; break;
                case "variable2controlled": Control = Controls.Variable2Controlled; break;
                case "variable2boostercontrolled": Control = Controls.Variable2BoosterControlled; break;
                case "variable3controlled": Control = Controls.Variable3Controlled; break;
                case "brakecylcontrolled": Control = Controls.BrakeCylControlled; break;
                case "curveforcecontrolled": Control = Controls.CurveForceControlled; break;
                case "angleofattackcontrolled": Control = Controls.AngleofAttackControlled; break;
                case "carfrictioncontrolled": Control = Controls.CarFrictionControlled; break;
                case "wheelrpmcontrolled": Control = Controls.WheelRpMControlled; break;
                case "cardistancetrackcontrolled": Control = Controls.CarDistanceTrackControlled; break;
                case "cartunneldistancecontrolled": Control = Controls.CarTunnelDistanceControlled; break;
                case "backpressurecontrolled": Control = Controls.BackPressureControlled; break;
                default: STFException.TraceWarning(stf, "Crash expected: Skipped unknown VolumeCurve/Frequencycurve type " + type); stf.SkipRestOfBlock(); return;
            }
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("granularity", ()=>{ Granularity = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("curvepoints", ()=>{
                    stf.MustMatch("(");
                    int count = stf.ReadInt(null);
                    CurvePoints = new CurvePoint[count];
                    for (int i = 0; i < count; ++i)
                    {
                        CurvePoints[i].X = stf.ReadFloat(STFReader.UNITS.None, null);
                        if (Control == Controls.DistanceControlled)
						{
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
        public enum Events { Speed_Inc_Past, Speed_Dec_Past, Distance_Inc_Past, Distance_Dec_Past,
            Variable1_Inc_Past, Variable1_2_Inc_Past, Variable1_3_Inc_Past, Variable1_4_Inc_Past, Variable1_Dec_Past, Variable1_2_Dec_Past, Variable1_3_Dec_Past, Variable1_4_Dec_Past, Variable2_Inc_Past, Variable2_Dec_Past, Variable3_Inc_Past, Variable3_Dec_Past, BrakeCyl_Inc_Past, BrakeCyl_Dec_Past, CurveForce_Inc_Past, CurveForce_Dec_Past, AngleofAttack_Inc_Past, AngleofAttack_Dec_Past, WheelRpM_Dec_Past, WheelRPM_Inc_Past, ConcreteSleepers_Inc_Past, ConcreteSleepers_Dec_Past, CarInTunnel_Inc_Past, CarInTunnel_Dec_Past
        };

        public Events Event;
        public float Threshold;

        public Variable_Trigger(STFReader f)
        {
            f.MustMatch("(");

            string eventString = f.ReadString();

            Threshold = f.ReadFloat(STFReader.UNITS.None, null);

            switch (eventString.ToLower())
            {
                case "speed_inc_past": Event = Events.Speed_Inc_Past; break;
                case "speed_dec_past": Event = Events.Speed_Dec_Past; break;
                case "distance_inc_past":
                    {
                        Event = Events.Distance_Inc_Past;
                        Threshold = Threshold * Threshold;
                        break;
                    }
                case "distance_dec_past":
                    {
                        Event = Events.Distance_Dec_Past;
                        Threshold = Threshold * Threshold;
                        break;
                    }
                case "variable1_inc_past": Event = Events.Variable1_Inc_Past; break;
                case "variable1_2_inc_past": Event = Events.Variable1_2_Inc_Past; break;
                case "variable1_3_inc_past": Event = Events.Variable1_3_Inc_Past; break;
                case "variable1_4_inc_past": Event = Events.Variable1_4_Inc_Past; break;
                case "variable1_dec_past": Event = Events.Variable1_Dec_Past; break;
                case "variable1_2_dec_past": Event = Events.Variable1_2_Dec_Past; break;
                case "variable1_3_dec_past": Event = Events.Variable1_3_Dec_Past; break;
                case "variable1_4_dec_past": Event = Events.Variable1_4_Dec_Past; break;
                case "variable2_inc_past": Event = Events.Variable2_Inc_Past; break;
                case "variable2_dec_past": Event = Events.Variable2_Dec_Past; break;
                case "variable3_inc_past": Event = Events.Variable3_Inc_Past; break;
                case "variable3_dec_past": Event = Events.Variable3_Dec_Past; break;
                case "brakecyl_inc_past": Event = Events.BrakeCyl_Inc_Past; break;
                case "brakecyl_dec_past": Event = Events.BrakeCyl_Dec_Past; break;
                case "curveforce_inc_past": Event = Events.CurveForce_Inc_Past; break;
                case "curveforce_dec_past": Event = Events.CurveForce_Dec_Past; break;
                case "angleofattack_inc_past": Event = Events.AngleofAttack_Inc_Past; break;
                case "angleofattack_dec_past": Event = Events.AngleofAttack_Dec_Past; break;
                case "wheelrpm_inc_past": Event = Events.WheelRPM_Inc_Past; break;
                case "wheelrpm_dec_past": Event = Events.WheelRpM_Dec_Past; break;
                case "concretesleepers_inc_past": Event = Events.ConcreteSleepers_Inc_Past; break;
                case "concretesleepers_dec_past": Event = Events.ConcreteSleepers_Dec_Past; break;
                case "carintunnel_inc_past": Event = Events.CarInTunnel_Inc_Past; break;
                case "carintunnel_dec_past": Event = Events.CarInTunnel_Dec_Past; break;
            }

            while (!f.EndOfBlock())
                ParsePlayCommand(f, f.ReadString().ToLower());
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
