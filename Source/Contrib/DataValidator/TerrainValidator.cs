// COPYRIGHT 2017 by the Open Rails project.
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

using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace DataValidator
{
    class TerrainValidator : Validator
    {
        public TerrainValidator(string file)
            : base(file)
        {
            try
            {
                var parsed = new TerrainFile(File);
                if (File.Contains("\\lo_tiles\\"))
                {
                    Equal(TraceEventType.Warning, 64, parsed.terrain.terrain_samples.terrain_nsamples, "terrain_nsamples");
                    Equal(TraceEventType.Warning, 256, parsed.terrain.terrain_samples.terrain_sample_size, "terrain_sample_size");
                }
                else
                {
                    Equal(TraceEventType.Warning, 256, parsed.terrain.terrain_samples.terrain_nsamples, "terrain_nsamples");
                    Equal(TraceEventType.Warning, 8, parsed.terrain.terrain_samples.terrain_sample_size, "terrain_sample_size");
                }
                Equal(TraceEventType.Warning, 0, parsed.terrain.terrain_samples.terrain_sample_rotation, "terrain_sample_rotation");
                ValidFileRef(TraceEventType.Error, parsed.terrain.terrain_samples.terrain_sample_ebuffer, "terrain_sample_ebuffer");
                ValidFileRef(TraceEventType.Error, parsed.terrain.terrain_samples.terrain_sample_nbuffer, "terrain_sample_nbuffer");
                ValidFileRef(TraceEventType.Error, parsed.terrain.terrain_samples.terrain_sample_ybuffer, "terrain_sample_ybuffer");
            }
            catch (Exception error)
            {
                Trace.WriteLine(error);
            }
        }
    }
}
