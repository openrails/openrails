// COPYRIGHT 2009, 2010, 2013 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

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
