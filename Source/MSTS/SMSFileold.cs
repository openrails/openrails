/// COPYWRITE 2009 by Wayne Campbell of the Open Rails Transport Simulator project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from Wayne Campbell.
/// 
/// WARNING - This file needs to be rewritten to use STF class 

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

		public SMSFile( string filenamewithpath )
		{
            ReadFile(filenamewithpath);  
        }

        private void ReadFile(string filenamewithpath)
        {
            SBR f = SBR.Open(filenamewithpath);
            Tr_SMS = new Tr_SMS(f.ReadSubBlock());
            f.VerifyEndOfBlock(); // msts seems to allow extra stuff after the end
        }

	} // class SMSFile

    public class Tr_SMS
    {
        public List<ScalabiltyGroup> ScalabiltyGroups = new List<ScalabiltyGroup>();
        
        public Tr_SMS(SBR f)
        {
            while (!f.EndOfBlock())
            {
                SBR block = f.ReadSubBlock();
                switch (block.ID)
                {
                    case TokenID.ScalabiltyGroup: ScalabiltyGroups.Add(new ScalabiltyGroup(block)); break;
                    default: block.ExpectComment(); break;  
                }
            }
            f.VerifyEndOfBlock();
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

        public ScalabiltyGroup(SBR f)
        {
            DetailLevel = f.ReadInt();
            while (!f.EndOfBlock())
            {
                SBR block = f.ReadSubBlock();
                switch (block.ID)
                {
                    case TokenID.Activation: Activation = new Activation(block); break;
                    case TokenID.Deactivation: Deactivation = new Deactivation(block); break; 
                    case TokenID.Streams: Streams = new SMSStreams( block ); break;
                    case TokenID.Volume: Volume = block.ReadFloat(); block.VerifyEndOfBlock(); break;
                    case TokenID.Stereo: Stereo = true; block.VerifyEndOfBlock(); break;
                    case TokenID.Ignore3d: Ignore3D = true; block.VerifyEndOfBlock(); break;
                    default: block.ExpectComment(); break;
                }
            }
            f.VerifyEndOfBlock();
        }
    } // class ScalabiltyGroup

    public class Activation
    {
        public bool ExternalCam = false;
        public bool CabCam = false;
        public bool PassengerCam = false;
        public float Distance = 10000;  // by default we are 'in range' to hear this
        public int TrackType = -1;

        public Activation(SBR f)
        {
            while (!f.EndOfBlock())
            {
                SBR block = f.ReadSubBlock();
                switch (block.ID)
                {
                    case TokenID.ExternalCam: ExternalCam = true; block.VerifyEndOfBlock(); break;
                    case TokenID.CabCam: CabCam = true; block.VerifyEndOfBlock(); break;
                    case TokenID.PassengerCam: PassengerCam = true; block.VerifyEndOfBlock(); break;
                    case TokenID.Distance: Distance = block.ReadFloat(); block.VerifyEndOfBlock(); break;
                    case TokenID.TrackType: TrackType = block.ReadInt(); block.VerifyEndOfBlock(); break;
                    default: block.ExpectComment(); break;
                }
            }
            f.VerifyEndOfBlock();
        }
    }

    public class Deactivation: Activation
    {
        public Deactivation(SBR f): base( f )
        {
        }
    }

    public class SMSStreams : List<SMSStream>
    {
        public SMSStreams(SBR f)
        {
            int count = f.ReadInt();

            while( !f.EndOfBlock() )
            {
                SBR block = f.ReadSubBlock();
                switch (block.ID)
                {
                    case TokenID.Stream: Add(new SMSStream(block)); break;
                    default: block.ExpectComment(); break;
                }
            }
            f.VerifyEndOfBlock();

            if (count != this.Count)
                Console.Error.WriteLine(f.ErrorMessage("Stream count mismatch"));  // it seems MSTS allows a mismatch
        }
    }

    public class SMSStream
    {
        public int Priority = 0;
        public Triggers Triggers;
        public float Volume = 1.0f;
        public VolumeCurve VolumeCurve = null;
        public FrequencyCurve FrequencyCurve = null;

        public SMSStream(SBR f)
        {
            while (!f.EndOfBlock())
            {
                SBR block = f.ReadSubBlock();
                switch (block.ID)
                {
                    case TokenID.Priority: Priority = block.ReadInt(); block.VerifyEndOfBlock(); break;
                    case TokenID.Triggers: Triggers = new Triggers(block); break;
                    case TokenID.VolumeCurve: VolumeCurve = new VolumeCurve(block); break; 
                    case TokenID.FrequencyCurve: FrequencyCurve = new FrequencyCurve(block); break; 
                    case TokenID.Volume: Volume = block.ReadFloat(); block.VerifyEndOfBlock(); break;
                    default: block.ExpectComment(); break;
                }
            }
            f.VerifyEndOfBlock();
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

        public VolumeCurve(SBR f)
        {
            string controlString = f.ReadString();
            switch (controlString)
            {
                case "DistanceControlled": Control = Controls.DistanceControlled; break;
                case "SpeedControlled": Control = Controls.SpeedControlled; break;
                case "Variable1Controlled": Control = Controls.Variable1Controlled; break;
                case "Variable2Controlled": Control = Controls.Variable2Controlled; break;
                case "Variable3Controlled": Control = Controls.Variable3Controlled; break;
                default: Console.Error.WriteLine(f.ErrorMessage("Unexpected " + controlString)); break; 
            }
            while (!f.EndOfBlock())
            {
                SBR block = f.ReadSubBlock();
                switch (block.ID)
                {
                    case TokenID.CurvePoints:
                        int count = block.ReadInt();
                        CurvePoints = new CurvePoint[count];
                        for (int i = 0; i < count; ++i)
                        {
                            CurvePoints[i].X = f.ReadFloat();
                            CurvePoints[i].Y = f.ReadFloat();
                        }
                        block.VerifyEndOfBlock(); 
                        break; 
                    case TokenID.Granularity: Granularity = block.ReadFloat(); block.VerifyEndOfBlock(); break;
                    default: block.ExpectComment(); break;
                }
            }
            f.VerifyEndOfBlock();
        }
    }

    public class FrequencyCurve: VolumeCurve
    {
        public FrequencyCurve(SBR f)
            : base(f)
        {
        }
    }


    public class Triggers : List<Trigger>
    {
        public Triggers(SBR f)
        {
            int count = f.ReadInt();

            while( !f.EndOfBlock() )
            {
                SBR block = f.ReadSubBlock();

                switch (block.ID)
                {
                    case TokenID.Dist_Travelled_Trigger: Add( new Dist_Travelled_Trigger( block )); break;   
                    case TokenID.Discrete_Trigger: Add( new Discrete_Trigger( block) ); break;       
                    case TokenID.Random_Trigger: Add( new Random_Trigger( block) ); break; 
                    case TokenID.Variable_Trigger: Add( new Variable_Trigger( block )); break; 
                    case TokenID.Initial_Trigger: Add( new Initial_Trigger( block )); break;
                    default: block.ExpectComment(); break;
                }
            }
            f.VerifyEndOfBlock();
            //if (count != this.Count)  - there are so many of these errors, I'm not going to report them
            //    Warnings.Log( f.ErrorMessage("Trigger count mismatch"));

            foreach (Trigger trigger in this)
                if (trigger.SoundCommand == null)
                    Console.Error.WriteLine(f.ErrorMessage("Trigger lacks a sound command"));
        }
    }

    public class Trigger
    {
        public SoundCommand SoundCommand = null;

        int playcommandcount = 0;

        protected void ParsePlayCommand(SBR block)
        {
            switch (block.ID)
            {
                case TokenID.PlayOneShot: 
                case TokenID.StartLoop:
                case TokenID.ReleaseLoopRelease: 
                case TokenID.StartLoopRelease:
                case TokenID.ReleaseLoopReleaseWithJump: 
                case TokenID.DisableTrigger: 
                case TokenID.EnableTrigger: 
                case TokenID.SetStreamVolume:
                    ++playcommandcount;
                    if (playcommandcount > 1)
                        Console.Error.WriteLine(block.ErrorMessage("MultiplePlayCommands"));
                    break;
                default:
                    break;
            }

            switch (block.ID)
            {
                case TokenID.PlayOneShot: SoundCommand = new PlayOneShot(block); break;
                case TokenID.StartLoop: SoundCommand = new StartLoop(block); break;
                case TokenID.ReleaseLoopRelease:  SoundCommand = new ReleaseLoopRelease(block); break; 
                case TokenID.StartLoopRelease:  SoundCommand = new StartLoopRelease( block ); break; 
                case TokenID.ReleaseLoopReleaseWithJump: SoundCommand = new ReleaseLoopReleaseWithJump( block ); break; 
                case TokenID.DisableTrigger: SoundCommand = new DisableTrigger( block); break; 
                case TokenID.EnableTrigger: SoundCommand = new EnableTrigger( block); break;
                case TokenID.SetStreamVolume: SoundCommand = new SetStreamVolume(block); break;
                default: block.ExpectComment(); break;
            }


        }
    }

    public class Initial_Trigger : Trigger
    {

        public Initial_Trigger(SBR f)
        {
            while (!f.EndOfBlock())
            {
                SBR block = f.ReadSubBlock();
                ParsePlayCommand(block);
            }
            f.VerifyEndOfBlock();

        }
    }

    public class Discrete_Trigger : Trigger
    {

        public int TriggerID;

        public Discrete_Trigger(SBR f)
        {
            TriggerID = f.ReadInt();

            while (!f.EndOfBlock())
            {
                SBR block = f.ReadSubBlock();
                ParsePlayCommand(block);
            }
            f.VerifyEndOfBlock();
        }
    }

    public class Variable_Trigger : Trigger
    {
        public enum Events { Speed_Inc_Past, Speed_Dec_Past, Distance_Inc_Past, Distance_Dec_Past,
        Variable1_Inc_Past, Variable1_Dec_Past, Variable2_Inc_Past, Variable2_Dec_Past, Variable3_Inc_Past, Variable3_Dec_Past   };

        public Events Event;
        public float Threshold;

        public Variable_Trigger(SBR f)
        {
            string eventString = f.ReadString();

            switch (eventString)
            {
                case "Speed_Inc_Past": Event = Events.Speed_Inc_Past; break;
                case "Speed_Dec_Past": Event = Events.Speed_Dec_Past; break;
                case "Distance_Inc_Past": Event = Events.Distance_Inc_Past; break;
                case "Distance_Dec_Past": Event = Events.Distance_Dec_Past; break;
                case "Variable1_Inc_Past": Event = Events.Variable1_Inc_Past; break;
                case "Variable1_Dec_Past": Event = Events.Variable1_Dec_Past; break;
                case "Variable2_Inc_Past": Event = Events.Variable2_Inc_Past; break;
                case "Variable2_Dec_Past": Event = Events.Variable2_Dec_Past; break;
                case "Variable3_Inc_Past": Event = Events.Variable3_Inc_Past; break;
                case "Variable3_Dec_Past": Event = Events.Variable3_Dec_Past; break;
                default:
                    Console.Error.WriteLine(f.ErrorMessage("Unexpected " + eventString));
                    break;  // MSTS ignores unrecognized tokens
            }

            Threshold = f.ReadFloat();

            while (!f.EndOfBlock())
            {
                SBR block = f.ReadSubBlock();
                ParsePlayCommand(block);
            }
            f.VerifyEndOfBlock();
        }
    }

    public class Dist_Travelled_Trigger : Trigger
    {
        public float Dist_Min = 80;
        public float Dist_Max = 100;
        public float Volume_Min = 0.9f;
        public float Volume_Max = 1.0f;

        public Dist_Travelled_Trigger(SBR f)
        {
            while (!f.EndOfBlock())
            {
                SBR block = f.ReadSubBlock();
                switch (block.ID)
                {
                    case TokenID.Dist_Min_Max: Dist_Min = block.ReadFloat(); Dist_Max = block.ReadFloat(); block.VerifyEndOfBlock(); break;
                    case TokenID.Volume_Min_Max: Volume_Min = block.ReadFloat(); Volume_Max = block.ReadFloat(); block.VerifyEndOfBlock(); break;
                    default: ParsePlayCommand(block); break;
                }
            }
            f.VerifyEndOfBlock();
        }
    }

    public class Random_Trigger : Trigger
    {
        public float Delay_Min = 80;
        public float Delay_Max = 100;
        public float Volume_Min = 0.9f;
        public float Volume_Max = 1.0f;

        public Random_Trigger(SBR f)
        {
            while (!f.EndOfBlock())
            {
                SBR block = f.ReadSubBlock();
                switch (block.ID)
                {
                    case TokenID.Delay_Min_Max: Delay_Min = block.ReadFloat(); Delay_Max = block.ReadFloat(); block.VerifyEndOfBlock(); break;
                    case TokenID.Volume_Min_Max: Volume_Min = block.ReadFloat(); Volume_Max = block.ReadFloat(); block.VerifyEndOfBlock(); break;
                    case TokenID.comment: block.Skip(); break;
                    default: ParsePlayCommand(block); break;
                }
            }
            f.VerifyEndOfBlock();
        }
    }
    public class SoundCommand
    {
        public enum SelectionMethods { RandomSelection, SequentialSelection };
    }

    public class SetStreamVolume : SoundCommand
    {
        public float Volume;

        public SetStreamVolume(SBR f)
        {
            Volume = f.ReadFloat();
            f.VerifyEndOfBlock();
        }
    }

    public class DisableTrigger : SoundCommand
    {
        public int TriggerID;

        public DisableTrigger(SBR f)
        {
            TriggerID = f.ReadInt();
            f.VerifyEndOfBlock();
        }
    }

    public class EnableTrigger : DisableTrigger
    {
        public EnableTrigger(SBR f)
            : base(f)
        {
        }
    }

    public class ReleaseLoopRelease : SoundCommand
    {
        public ReleaseLoopRelease(SBR f)
        {
            f.VerifyEndOfBlock();
        }
    }

    public class ReleaseLoopReleaseWithJump : SoundCommand
    {
        public ReleaseLoopReleaseWithJump(SBR f)
        {
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
        
        public PlayOneShot(SBR f)
        {
            int count = f.ReadInt();
            Files = new string[count];
            int iFile = 0;
            while( !f.EndOfBlock() )
            {
                SBR block = f.ReadSubBlock();
                switch (block.ID)
                {
                    case TokenID.File:
                        if (iFile < count)
                        {
                            Files[iFile++] = block.ReadString();
                            block.ReadInt();
                            block.VerifyEndOfBlock();
                        }
                        else  // MSTS skips extra files
                        {
                            Console.Error.WriteLine(block.ErrorMessage("File count mismatch"));
                            block.Skip();
                        }
                        break;
                    case TokenID.SelectionMethod: 
                        string s = block.ReadString();
                        switch (s)
                        {
                            case "RandomSelection": SelectionMethod = SelectionMethods.RandomSelection; break;
                            case "SequentialSelection": SelectionMethod = SelectionMethods.SequentialSelection; break;
                        }
                        block.VerifyEndOfBlock(); 
                        break;
                    default: block.ExpectComment(); break;
                }
            }
            f.VerifyEndOfBlock();
        }
    }// PlayOneShot

    public class StartLoop : PlayOneShot
    {
        public StartLoop( SBR f ): base(f)
        {
        }
    }

    public class StartLoopRelease : PlayOneShot
    {
        public StartLoopRelease(SBR f)
            : base(f)
        {
        }
    }


} // namespace
