/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MSTS;

namespace ORTS
{
    public class WSFile
    {
        public TR_WorldSoundFile TR_WorldSoundFile = null;

        public WSFile(string wsfilename)
        {
            Read(wsfilename);
        }

        public void Read(string wsfilename)
        {
            if (File.Exists(wsfilename))
            {
				Trace.Write("$");
                using (STFReader reader = new STFReader(wsfilename))
                {
                    while (!reader.EndOfBlock())
                        switch (reader.ReadItem().ToLower())
                        {
                            case "tr_worldsoundfile": TR_WorldSoundFile = new TR_WorldSoundFile(reader); break;
                            case "(": reader.SkipRestOfBlock(); break;
                        }
                    if (TR_WorldSoundFile == null)
                        throw new STFException(reader, "Missing TR_WorldSoundFile statement");
                }
            }
        }
    }

    public class TR_WorldSoundFile
    {
        public List<WorldSoundSource> SoundSources = new List<WorldSoundSource>();

        public TR_WorldSoundFile(STFReader reader)
        {
            reader.MustMatch("(");
            while (!reader.EndOfBlock())
                switch (reader.ReadItem().ToLower())
                {
                    case "soundsource": SoundSources.Add(new WorldSoundSource(reader)); break;
                    case "(": reader.SkipRestOfBlock(); break;
                }
        }
    }

    public class WorldSoundSource
    {
        public float X;
        public float Y;
        public float Z;
        public string SoundSourceFileName;

        public WorldSoundSource(STFReader reader)
        {
            reader.MustMatch("(");
            while (!reader.EndOfBlock())
                switch (reader.ReadItem().ToLower())
                {
                    case "position":
                        reader.MustMatch("(");
                        X = reader.ReadFloat(STFReader.UNITS.None, null);
                        Y = reader.ReadFloat(STFReader.UNITS.None, null);
                        Z = reader.ReadFloat(STFReader.UNITS.None, null);
                        reader.SkipRestOfBlock();
                        break;
                    case "filename": SoundSourceFileName = reader.ReadItemBlock(null); break;
                    case "(": reader.SkipRestOfBlock(); break;
                }
        }
    }
}