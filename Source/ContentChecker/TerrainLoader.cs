// COPYRIGHT 2018 by the Open Rails project.
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;

using Orts.Formats.Msts;

namespace ContentChecker
{
    /// <summary>
    /// Loader class for .s files
    /// </summary>
    class TerrainLoader : Loader
    {
        TerrainFile tFile;

        /// <summary>
        /// Try to load the file.
        /// Possibly this might raise an exception. That exception is not caught here
        /// </summary>
        /// <param name="file">The file that needs to be loaded</param>
        public override void TryLoading(string file)
        {
            loadedFile = file;
            tFile = new TerrainFile(file);
        }

        protected override void AddDependentFiles()
        {
            var baseFileWithDir = Path.Combine(Path.GetDirectoryName(loadedFile), Path.GetFileNameWithoutExtension(loadedFile));
            var sampleCount = tFile.terrain.terrain_samples.terrain_nsamples;
            AddAdditionalFileAction.Invoke(baseFileWithDir + "_y.raw", new TerrainAltitudeLoader(sampleCount));

            var f_raw_file = baseFileWithDir + "_f.raw";
            if (File.Exists(f_raw_file))
            {   // we need to check here if it exists, because we do not want an error popping up later
                AddAdditionalFileAction.Invoke(f_raw_file, new TerrainFlagsLoader(sampleCount));
            }
        }
    }
}
