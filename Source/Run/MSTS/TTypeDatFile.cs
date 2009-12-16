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
        
        public TTypeDatFile(string filePath): base()
		{
            STFReader f = new STFReader(filePath); 
            int count = f.ReadInt();
            while( !f.EndOfBlock() )
            {
                string token = f.ReadToken();
                switch (token.ToLower())
                {
                    case "tracktype": this.Add(new TrackType(f)); break;
                    default: f.SkipUnknownBlock( token ); break;
                }
            }
            if (count != this.Count)
                STFError.Report( f,"Count mismatch." );
            f.Close();
		}

        public class TrackType
        {
            public string Label;
            public string InsideSound;
            public string OutsideSound;

            public TrackType(STFReader f)
            {
                f.VerifyStartOfBlock();
                Label = f.ReadToken();
                InsideSound = f.ReadToken();
                OutsideSound = f.ReadToken();
                f.VerifyEndOfBlock();
            }
        } // TrackType

	} // class CVFFile
} // namespace MSTS

