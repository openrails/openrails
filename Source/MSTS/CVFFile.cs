/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace MSTS
{

	// TODO - this is an incomplete parse of the cvf file.
	public class CVFFile
	{
        public List<Vector3> Locations = new List<Vector3>();   // Head locations for front, left and right views
        public List<Vector3> Directions = new List<Vector3>();  // Head directions for each view

        public CVFFile(string filePath)
		{
            STFReader inf = new STFReader( filePath );
            try
            {
                inf.MustMatch( "Tr_CabViewFile" );
                inf.VerifyStartOfBlock();
                while( !inf.EOF() )
                {
                    string token = inf.ReadToken();
                    if (0 == string.Compare(token, "Position", true))
                        Locations.Add(inf.ReadVector3Block());

                    else if (0 == string.Compare(token, "Direction", true))
                        Directions.Add(inf.ReadVector3Block());

                    else
                        inf.SkipBlock();  // TODO, complete parse

                }
            }
            finally
            {
                inf.Close();
            }
		}

	} // class CVFFile
} // namespace MSTS

