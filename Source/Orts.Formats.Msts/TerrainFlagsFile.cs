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

namespace Orts.Formats.Msts
{
    public class TerrainFlagsFile
    {
        readonly byte[,] Flags;

        public TerrainFlagsFile(string fileName, int sampleCount)
        {
            Flags = new byte[sampleCount, sampleCount];
            try
            {
                using (var reader = new BinaryReader(File.OpenRead(fileName)))
                    for (var z = 0; z < sampleCount; z++)
                        for (var x = 0; x < sampleCount; x++)
                            Flags[x, z] = reader.ReadByte();
            }
            catch (Exception error)
            {
                Trace.WriteLine(new FileLoadException(fileName, error));
            }
        }

        /// <summary>
        /// Returns the vertex-hidden flag at a specific sample point.
        /// </summary>
        /// <param name="x">X coordinate; starts at west side, increases easterly.</param>
        /// <param name="z">Z coordinate; starts at north side, increases southerly.</param>
        /// <returns>Vertex-hidden flag.</returns>
        public bool IsVertexHidden(int x, int z)
        {
            return (Flags[x, z] & 0x04) == 0x04;
        }
    }
}
