/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Diagnostics;
using System.IO;

namespace MSTS
{
	public class FFile
	{
		readonly string FileName;
		readonly byte[,] Data = new byte[256, 256];

		public FFile(string fileName)
		{
			FileName = fileName;

			if (File.Exists(FileName))
			{
				try
				{
					using (BinaryReader f = new BinaryReader(new FileStream(FileName, FileMode.Open, FileAccess.Read)))
					{
						for (int z = 0; z < 256; ++z)
							for (int x = 0; x < 256; ++x)
								Data[z, x] = f.ReadByte();
					}
				}
				catch (Exception error)
				{
					Trace.TraceInformation(fileName);
					Trace.WriteLine(error);
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
			return (GetFloorData(x, z) & 0x04) == 0x04;
		}
	}
}
