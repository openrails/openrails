// COPYRIGHT 2011, 2012, 2013 by the Open Rails project.
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
	public class FFile
	{
		readonly string FileName;
        int xdim = 256, zdim = 256;
		readonly byte[,] Data;

		public FFile(string fileName)
		{
			FileName = fileName;

			if (File.Exists(FileName))
			{
				try
				{
					using (BinaryReader f = new BinaryReader(new FileStream(FileName, FileMode.Open, FileAccess.Read)))
					{
                        Data = new byte[256, 256];
                        for (int z = 0; z < 256; ++z)
							for (int x = 0; x < 256; ++x)
								Data[z, x] = f.ReadByte();
					}
				}
				catch (Exception error)
				{
                    Trace.WriteLine(new FileLoadException(fileName, error));
                }
			}
		}

        public FFile(string fileName, int dim)
        {
            xdim = zdim = dim;
            FileName = fileName;

            if (File.Exists(FileName))
            {
                try
                {
                    using (BinaryReader f = new BinaryReader(new FileStream(FileName, FileMode.Open, FileAccess.Read)))
                    {
                        Data = new byte[xdim, zdim];
                        for (int z = 0; z < zdim; ++z)
                            for (int x = 0; x < xdim; ++x)
                                Data[z, x] = f.ReadByte();
                    }
                }
                catch (Exception error)
                {
                    Trace.WriteLine(new FileLoadException(fileName, error));
                }
            }
        }
        
        byte GetFloorData(int x, int z)
		{
			// Gets floor properties at the tile coordinates x, z
			// 0,0 is the north west corner of the tile
			// 255,255 is the south east corner
			return Data[z, x];
		}

		public bool IsVertexHidden(int x, int z)
		{
            if (Data == null) return false;
            return (GetFloorData(x, z) & 0x04) == 0x04;
		}
	}
}
