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
                STFReader reader = new STFReader(wsfilename);
                try
                {
                    while (!reader.EndOfBlock()) // EOF
                    {
                        string token = reader.ReadToken();
                        if (string.Compare(token, "Tr_Worldsoundfile", true) == 0) TR_WorldSoundFile = new TR_WorldSoundFile(reader);
                        else reader.SkipBlock();
                    }
                    if (TR_WorldSoundFile == null)
                        throw (new STFException(reader, "Missing TR_WorldSoundFile statement"));
                }
                finally
                {
                    reader.Close();
                }
            }
        }
    }

    public class TR_WorldSoundFile
    {
        public List<WorldSoundSource> SoundSources = new List<WorldSoundSource>();

        public TR_WorldSoundFile(STFReader reader)
        {
            reader.VerifyStartOfBlock();
            while (!reader.EndOfBlock())
            {
                string token = reader.ReadToken();
                if (string.Compare(token, "Soundsource", true) == 0) SoundSources.Add (new WorldSoundSource (reader));
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
            reader.VerifyStartOfBlock();
            while (!reader.EndOfBlock())
            {
                string token = reader.ReadToken();
                if (string.Compare(token, "position", true) == 0)
                {
                    reader.VerifyStartOfBlock();
                    X = reader.ReadFloat();
                    Y = reader.ReadFloat();
                    Z = reader.ReadFloat();
                    reader.VerifyEndOfBlock();
                }
                else if (string.Compare(token, "filename", true) == 0) SoundSourceFileName = reader.ReadStringBlock();
                else reader.SkipBlock();
            }
        }
    }
}