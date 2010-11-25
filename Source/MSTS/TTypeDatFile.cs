/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 



using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;


namespace MSTS
{

	// TODO - this is an incomplete parse of the cvf file.
	public class TTypeDatFile: List<TTypeDatFile.TrackType>
	{
        
        public TTypeDatFile(string filePath)
		{
            using (STFReader stf = new STFReader(filePath, false))
            {
                int count = stf.ReadInt(STFReader.UNITS.None, null);
                stf.ParseBlock(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("tracktype", ()=>{ Add(new TrackType(stf)); }),
                });
                if (count != Count)
                    STFException.TraceError(stf, "Count mismatch.");
            }
		}

        public class TrackType
        {
            public string Label;
            public string InsideSound;
            public string OutsideSound;

            public TrackType(STFReader stf)
            {
                stf.MustMatch("(");
                Label = stf.ReadString();
                InsideSound = stf.ReadString();
                OutsideSound = stf.ReadString();
                stf.SkipRestOfBlock();
            }
        } // TrackType

	} // class CVFFile
} // namespace MSTS

