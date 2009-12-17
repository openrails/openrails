/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 
/// NOTE: THIS IS THE REFERENCE PROTOTYPE FOR PARSING STF FILES

using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;

namespace MSTS
{

    /// <summary>
    /// Utility class to avoid loading multiple copies of the same file.
    /// </summary>
    public class SharedSMSFileManager
    {
        private static Dictionary<string, SMSFile> SharedSMSFiles = new Dictionary<string, SMSFile>();

        public static SMSFile Get(string path)
        {
            if (!SharedSMSFiles.ContainsKey(path))
            {
                SMSFile smsFile = new SMSFile(path);
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
	public class SMSFile
	{
		public Tr_SMS Tr_SMS;

		public SMSFile( string filePath )
		{
            ReadFile(filePath);  
        }

        private void ReadFile(string filePath)
        {
            STFReader f = new STFReader(filePath);
            while (!f.EndOfBlock())
            {
                string token = f.ReadToken();
                switch( token.ToLower() )
                {
                    case "tr_sms": Tr_SMS = new Tr_SMS(f); break;
                    default: f.SkipUnknownBlock(token); break;
                }
            }
            f.Close();
        }

	} // class SMSFile

    public class Tr_SMS
    {
        public List<ScalabiltyGroup> ScalabiltyGroups = new List<ScalabiltyGroup>();
        
        public Tr_SMS(STFReader f)
        {
            f.VerifyStartOfBlock();
            while (!f.EndOfBlock())
            {
                string token = f.ReadToken();
                switch (token.ToLower())
                {
                    case "scalabiltygroup": ScalabiltyGroups.Add(new ScalabiltyGroup(f)); break;
                    default: f.SkipUnknownBlock( token ); break;
                }
            }
        }
    } // class Tr_SMS

    public partial class ScalabiltyGroup
    {
        public int DetailLevel;
        public SMSStreams Streams = null;
        public float Volume = 1.0f;
        public bool Stereo = false;
        public bool Ignore3D = false;
        public Activation Activation;
        public Deactivation Deactivation;

        public ScalabiltyGroup(STFReader f)
        {
            f.VerifyStartOfBlock();
            DetailLevel = f.ReadInt();
            while (!f.EndOfBlock())
            {
                string token = f.ReadToken();
                switch( token.ToLower() )
                {
                    case "activation": Activation = new Activation(f); break;
                    case "deactivation": Deactivation = new Deactivation(f); break; 
                    case "streams": Streams = new SMSStreams( f ); break;
                    case "volume": Volume = f.ReadFloatBlock(); break;
                    case "stereo": Stereo = f.ReadBoolBlock(); break;
                    case "ignore3d": Ignore3D = f.ReadBoolBlock(); break;
                    default: f.SkipUnknownBlock( token); break;
                }
            }
        }
    } // class ScalabiltyGroup

    public class Activation
    {
        public bool ExternalCam = false;
        public bool CabCam = false;
        public bool PassengerCam = false;
        public float Distance = 10000;  // by default we are 'in range' to hear this
        public int TrackType = -1;

        public Activation(STFReader f)
        {
            f.VerifyStartOfBlock();
            while (!f.EndOfBlock())
            {
                string token = f.ReadToken();
                switch (token.ToLower())
                {
                    case "externalcam": ExternalCam = f.ReadBoolBlock(); break;
                    case "cabcam": CabCam = f.ReadBoolBlock(); break;
                    case "passengercam": PassengerCam = f.ReadBoolBlock(); break;
                    case "distance": Distance = f.ReadFloatBlock(); break;
                    case "tracktype": TrackType = f.ReadIntBlock(); break;
                    default: f.SkipUnknownBlock(token); break;
                }
            }
        }
    }

    public class Deactivation: Activation
    {
        public Deactivation(STFReader f): base( f )
        {
        }
    }

    public class SMSStreams : List<SMSStream>
    {
        public SMSStreams(STFReader f)
        {
            f.VerifyStartOfBlock();

            int count = f.ReadInt();

            while( !f.EndOfBlock() )
            {
                string token = f.ReadToken();
                switch (token.ToLower())
                {
                    case "stream": Add(new SMSStream(f)); break;
                    default: f.SkipUnknownBlock(token); break;
                }
            }

            if (count != this.Count)
                STFError.Report(f,"Stream count mismatch");  
        }
    }

    public class SMSStream
    {
        public int Priority = 0;
        public Triggers Triggers;
        public float Volume = 1.0f;
        public VolumeCurve VolumeCurve = null;
        public FrequencyCurve FrequencyCurve = null;

        public SMSStream( STFReader f)
        {
            f.VerifyStartOfBlock();

            while (!f.EndOfBlock())
            {
                string token = f.ReadToken();
                switch (token.ToLower())
                {
                    case "priority": Priority = f.ReadIntBlock(); break;
                    case "triggers":  Triggers = new Triggers(f); break;
                    case "volumecurve": VolumeCurve = new VolumeCurve(f); break; 
                    case "frequencycurve": FrequencyCurve = new FrequencyCurve(f); break;
                    case "volume": Volume = f.ReadFloatBlock(); break;
                    default: f.SkipUnknownBlock(token); break;
                }
            }
        }
    }

    public struct CurvePoint
    {
        public float X, Y;
    }

    public class VolumeCurve
    {
        public enum Controls { None, DistanceControlled, SpeedControlled, Variable1Controlled, Variable2Controlled, Variable3Controlled };

        public Controls Control = Controls.None;
        public float Granularity = 1.0f;

        public CurvePoint[] CurvePoints;

        public VolumeCurve(STFReader f)
        {
            f.VerifyStartOfBlock();
            string controlString = f.ReadToken();
            switch (controlString.ToLower())
            {
                case "distancecontrolled": Control = Controls.DistanceControlled; break;
                case "speedcontrolled": Control = Controls.SpeedControlled; break;
                case "variable1controlled": Control = Controls.Variable1Controlled; break;
                case "variable2controlled": Control = Controls.Variable2Controlled; break;
                case "variable3controlled": Control = Controls.Variable3Controlled; break;
                default: STFError.Report(f, "Unexpected " + controlString); break; 
            }
            while (!f.EndOfBlock())
            {
                string token = f.ReadToken();
                switch (token.ToLower())
                {
                    case "curvepoints":
                        f.VerifyStartOfBlock();
                        int count = f.ReadInt();
                        CurvePoints = new CurvePoint[count];
                        for (int i = 0; i < count; ++i)
                        {
                            CurvePoints[i].X = f.ReadFloat();
                            CurvePoints[i].Y = f.ReadFloat();
                        }
                        f.VerifyEndOfBlock();
                        break;
                    case "granularity": Granularity = f.ReadFloatBlock(); break;
                    default: f.SkipUnknownBlock(token); break;
                }
            }
        }
    }

    public class FrequencyCurve: VolumeCurve
    {
        public FrequencyCurve(STFReader f)
            : base(f)
        {
        }
    }


    public class Triggers : List<Trigger>
    {
        public Triggers(STFReader f)
        {
            f.VerifyStartOfBlock();
            int count = f.ReadInt();

            while( !f.EndOfBlock() )
            {
                string token = f.ReadToken();

                switch (token.ToLower())
                {
                    case "dist_travelled_trigger": Add( new Dist_Travelled_Trigger( f )); break;   
                    case "discrete_trigger": Add( new Discrete_Trigger( f) ); break;       
                    case "random_trigger": Add( new Random_Trigger( f) ); break; 
                    case "variable_trigger": Add( new Variable_Trigger( f )); break; 
                    case "initial_trigger": Add( new Initial_Trigger( f )); break;
                    default: f.SkipUnknownBlock(token); break;
                }
            }

            foreach (Trigger trigger in this)
                if (trigger.SoundCommand == null)
                    STFError.Report( f, "Trigger lacks a sound command");
        }
    }

    public class Trigger
    {
        public SoundCommand SoundCommand = null;

        int playcommandcount = 0;

        protected void ParsePlayCommand( STFReader f, string token )
        {
            switch (token.ToLower())
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
                        STFError.Report( f, "MultiplePlayCommands");
                    break;
                default:
                    break;
            }

            switch (token.ToLower())
            {
                case "playoneshot": SoundCommand = new PlayOneShot(f); break;
                case "startloop": SoundCommand = new StartLoop(f); break;
                case "releaselooprelease":  SoundCommand = new ReleaseLoopRelease(f); break; 
                case "startlooprelease":  SoundCommand = new StartLoopRelease( f ); break; 
                case "releaseloopreleasewithjump": SoundCommand = new ReleaseLoopReleaseWithJump( f ); break; 
                case "disabletrigger": SoundCommand = new DisableTrigger( f); break; 
                case "enabletrigger": SoundCommand = new EnableTrigger( f); break;
                case "setstreamvolume": SoundCommand = new SetStreamVolume(f); break;
                default: f.SkipUnknownBlock(token); break;
            }
        }
    }

    public class Initial_Trigger : Trigger
    {

        public Initial_Trigger(STFReader f)
        {
            f.VerifyStartOfBlock();
            while (!f.EndOfBlock())
            {
                string token = f.ReadToken();
                ParsePlayCommand(f, token);
            }
        }
    }

    public class Discrete_Trigger : Trigger
    {

        public int TriggerID;

        public Discrete_Trigger(STFReader f)
        {
            f.VerifyStartOfBlock();

            TriggerID = f.ReadInt();

            while (!f.EndOfBlock())
            {
                string token = f.ReadToken();
                ParsePlayCommand(f, token);
            }
        }
    }

    public class Variable_Trigger : Trigger
    {
        public enum Events { Speed_Inc_Past, Speed_Dec_Past, Distance_Inc_Past, Distance_Dec_Past,
        Variable1_Inc_Past, Variable1_Dec_Past, Variable2_Inc_Past, Variable2_Dec_Past, Variable3_Inc_Past, Variable3_Dec_Past   };

        public Events Event;
        public float Threshold;

        public Variable_Trigger(STFReader f)
        {
            f.VerifyStartOfBlock();

            string eventString = f.ReadToken();

            switch (eventString.ToLower())
            {
                case "speed_inc_past": Event = Events.Speed_Inc_Past; break;
                case "speed_dec_past": Event = Events.Speed_Dec_Past; break;
                case "distance_inc_past": Event = Events.Distance_Inc_Past; break;
                case "distance_dec_past": Event = Events.Distance_Dec_Past; break;
                case "variable1_inc_past": Event = Events.Variable1_Inc_Past; break;
                case "variable1_dec_past": Event = Events.Variable1_Dec_Past; break;
                case "variable2_inc_past": Event = Events.Variable2_Inc_Past; break;
                case "variable2_dec_past": Event = Events.Variable2_Dec_Past; break;
                case "variable3_inc_past": Event = Events.Variable3_Inc_Past; break;
                case "variable3_dec_past": Event = Events.Variable3_Dec_Past; break;
                default:
                    STFError.Report(f, "Unexpected " + eventString);
                    break;  // MSTS ignores unrecognized tokens
            }

            Threshold = f.ReadFloat();

            while (!f.EndOfBlock())
            {
                string token = f.ReadToken();
                ParsePlayCommand(f, token);
            }
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
            f.VerifyStartOfBlock();
            while (!f.EndOfBlock())
            {
                string token = f.ReadToken();
                switch (token.ToLower())
                {
                    case "dist_min_max": f.VerifyStartOfBlock();  Dist_Min = f.ReadFloat(); Dist_Max = f.ReadFloat(); f.VerifyEndOfBlock(); break;
                    case "volume_min_max": f.VerifyStartOfBlock();  Volume_Min = f.ReadFloat(); Volume_Max = f.ReadFloat(); f.VerifyEndOfBlock(); break;
                    default: ParsePlayCommand(f, token); break;
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
            f.VerifyStartOfBlock();
            while (!f.EndOfBlock())
            {
                string token = f.ReadToken();
                switch (token.ToLower())
                {
                    case "delay_min_max": f.VerifyStartOfBlock(); Delay_Min = f.ReadFloat(); Delay_Max = f.ReadFloat(); f.VerifyEndOfBlock(); break;
                    case "volume_min_max": f.VerifyStartOfBlock(); Volume_Min = f.ReadFloat(); Volume_Max = f.ReadFloat(); f.VerifyEndOfBlock(); break;
                    default: ParsePlayCommand(f, token); break;
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
            f.VerifyStartOfBlock();
            Volume = f.ReadFloat();
            f.VerifyEndOfBlock();
        }
    }

    public class DisableTrigger : SoundCommand
    {
        public int TriggerID;

        public DisableTrigger(STFReader f)
        {
            f.VerifyStartOfBlock();
            TriggerID = f.ReadInt();
            f.VerifyEndOfBlock();
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
            f.VerifyStartOfBlock();
            f.VerifyEndOfBlock();
        }
    }

    public class ReleaseLoopReleaseWithJump : SoundCommand
    {
        public ReleaseLoopReleaseWithJump(STFReader f)
        {
            f.VerifyStartOfBlock();
            f.VerifyEndOfBlock();
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
            f.VerifyStartOfBlock();
            int count = f.ReadInt();
            Files = new string[count];
            int iFile = 0;
            while( !f.EndOfBlock() )
            {
                string token = f.ReadToken();
                switch (token.ToLower())
                {
                    case "file":
                        if (iFile < count)
                        {
                            f.VerifyStartOfBlock();
                            Files[iFile++] = f.ReadToken();
                            f.ReadInt();
                            f.VerifyEndOfBlock();
                        }
                        else  // MSTS skips extra files
                        {
                            STFError.Report(f, "File count mismatch");
                            f.SkipBlock();
                        }
                        break;
                    case "selectionmethod":
                        f.VerifyStartOfBlock();
                        string s = f.ReadToken();
                        switch (s.ToLower())
                        {
                            case "randomselection": SelectionMethod = SelectionMethods.RandomSelection; break;
                            case "sequentialselection": SelectionMethod = SelectionMethods.SequentialSelection; break;
                            default: STFError.Report(f, "Unknown selection method " + s); break;
                        }
                        f.VerifyEndOfBlock(); 
                        break;
                    default: f.SkipUnknownBlock( token ); break;
                }
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
