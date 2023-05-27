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

using System.Collections.Generic;
using System.IO;

using Orts.Formats.Msts;
using Orts.Formats.OR;

namespace ContentChecker
{
    /// <summary>
    /// Loader class for .pat files
    /// </summary>
    class CarSpawnLoader : Loader
    {
        /// <summary>
        /// Try to load the file.
        /// Possibly this might raise an exception. That exception is not caught here
        /// </summary>
        /// <param name="file">The file that needs to be loaded</param>
        public override void TryLoading(string file)
        {
            var CarSpawnerLists = new List<CarSpawnerList>();
            var subdirname = Path.GetFileName(Path.GetDirectoryName(file)).ToLowerInvariant();
            if (subdirname == "openrails")
            {
                string RoutePath = Path.GetDirectoryName(Path.GetDirectoryName(file));
                var ExtCarSpawnerFile = new ExtCarSpawnerFile(file, RoutePath + @"\shapes\", CarSpawnerLists);
            }
            else
            {
                string RoutePath = Path.GetDirectoryName(file);
                var CarSpawnerFile = new CarSpawnerFile(file, RoutePath + @"\shapes\", CarSpawnerLists);
            }
        }
    }
}
