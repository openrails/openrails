/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Diagnostics;
using System.IO;


namespace MSTS
{

	public class YFile
	{

		public ushort GetElevationIndex( int X, int Z )
		{
			// What is the elevation at the tile coordinates x, z
			// 0,0 is the north west corner of the tile
			// 255,255 is the south east corner
			
			return A[Z,X];
		}

		public YFile( string filename )
		{
			Filename = filename;

			// Open the file
			BinaryReader reader = new BinaryReader( new FileStream( Filename, FileMode.Open, FileAccess.Read ) );
            try
            {
                // read it in
                for (int y = 0; y < 256; ++y)
                    for (int x = 0; x < 256; ++x)
                        A[y, x] = reader.ReadUInt16();
            }
            catch (Exception error)
            {
				Trace.TraceInformation(filename);
				Trace.WriteLine(error);
            }
			finally
			{
				reader.Close( );
			}
		}

		private string Filename;
		private ushort[,] A = new ushort[256,256];
	}

}