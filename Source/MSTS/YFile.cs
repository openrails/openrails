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
        int xdim = 256, zdim = 256;
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
            xdim = zdim = 256;
            A =  new ushort[zdim,xdim];
			// Open the file
			BinaryReader reader = new BinaryReader( new FileStream( Filename, FileMode.Open, FileAccess.Read ) );
            try
            {
                // read it in
                for (int y = 0; y < xdim; ++y)
                    for (int x = 0; x < zdim; ++x)
                        A[y, x] = reader.ReadUInt16();
            }
            catch (Exception error)
            {
                Trace.WriteLine(new FileLoadException(filename, error));
            }
			finally
			{
				reader.Close( );
			}
		}

        public YFile(string filename, int dim)
        {
            Filename = filename;
            xdim = zdim = dim;
            A = new ushort[zdim, xdim];
            // Open the file
            BinaryReader reader = new BinaryReader(new FileStream(Filename, FileMode.Open, FileAccess.Read));
            try
            {
                // read it in
                for (int y = 0; y < zdim; ++y)
                    for (int x = 0; x < xdim; ++x)
                        A[y, x] = reader.ReadUInt16();
            }
            catch (Exception error)
            {
                Trace.WriteLine(new FileLoadException(filename, error));
            }
            finally
            {
                reader.Close();
            }
        }
        
        private string Filename;
		private ushort[,] A;
	}

}