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
                using (STFReader stf = new STFReader(wsfilename, false))
                {
                    stf.ParseFile(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("tr_worldsoundfile", ()=>{ TR_WorldSoundFile = new TR_WorldSoundFile(stf); }),
                    });
                    //TODO This should be changed to STFException.TraceError() with defaults values created
                    if (TR_WorldSoundFile == null)
                        throw new STFException(stf, "Missing TR_WorldSoundFile statement");
                }
            }
        }
    }

    public class TR_WorldSoundFile
    {
        public List<WorldSoundSource> SoundSources = new List<WorldSoundSource>();
        public List<WorldSoundRegion> SoundRegions = new List<WorldSoundRegion>();

        public TR_WorldSoundFile(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("soundsource", ()=>{ SoundSources.Add(new WorldSoundSource(stf)); }),
                new STFReader.TokenProcessor("soundregion", ()=>{ SoundRegions.Add(new WorldSoundRegion(stf)); }),
            });
        }
    }

    public class WorldSoundSource
    {
        public float X;
        public float Y;
        public float Z;
        public string SoundSourceFileName;

        public WorldSoundSource(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("filename", ()=>{ SoundSourceFileName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("position", ()=>{
                    stf.MustMatch("(");
                    X = stf.ReadFloat(STFReader.UNITS.None, null);
                    Y = stf.ReadFloat(STFReader.UNITS.None, null);
                    Z = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                }),
            });
        }
    }

    public class WorldSoundRegion
    {
        public int SoundRegionTrackType = -1;
        public float ROTy;
        public List<int> TrackNodes;

        public WorldSoundRegion(STFReader stf)
        {
            TrackNodes = new List<int>();
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("soundregiontracktype", ()=>{ SoundRegionTrackType = stf.ReadIntBlock(STFReader.UNITS.None, -1); }),
                new STFReader.TokenProcessor("soundregionroty", ()=>{ ROTy = stf.ReadFloatBlock(STFReader.UNITS.None, float.MaxValue); }),
                new STFReader.TokenProcessor("tritemid", ()=>{
                    stf.MustMatch("(");
                    int dummy = stf.ReadInt(STFReader.UNITS.None, 0);
                    dummy = stf.ReadInt(STFReader.UNITS.None, -1);
                    if (dummy != -1)
                        TrackNodes.Add(dummy);
                    stf.SkipRestOfBlock();
                }),
            });
        }
    }
}