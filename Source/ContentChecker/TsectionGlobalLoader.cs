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

using System.IO;
using Orts.Formats.Msts;
using Path = System.IO.Path;

namespace ContentChecker
{
    /// <summary>
    /// Loader class for the tsection.dat in the global directory files
    /// </summary>
    class TsectionGlobalLoader : Loader
    {
        TrackSectionsFile TSectionDat;
        string _routePath;

        /// <summary>
        /// default constructor for when this file is checked directly
        /// </summary>
        public TsectionGlobalLoader() : base()
        {
            IsDependent = true;
        }

        /// <summary>
        /// Constructor that is called from a specific route. This allows to load the dependent route-specific Tsection.dat
        /// </summary>
        /// <param name="routePath">The path (directory) of a route</param>
        public TsectionGlobalLoader(string routePath) : this()
        {
            _routePath = routePath;
        }
        /// <summary>
        /// Try to load the file.
        /// Possibly this might raise an exception. That exception is not caught here
        /// </summary>
        /// <param name="file">The file that needs to be loaded</param>
        public override void TryLoading(string file)
        {
            TSectionDat = new TrackSectionsFile(file);
        }

        protected override void AddDependentFiles()
        {
            if (_routePath == null)
            { // we do not know which route needs to be loaded additionally
                return;
            }

            string routeTsectionDat = Path.Combine(_routePath, "tsection.dat");
            if (File.Exists(routeTsectionDat))
            {
                var tsectionLoader = new TsectionLoader(TSectionDat);
                AddAdditionalFileAction.Invoke(routeTsectionDat, tsectionLoader);
            }

        }
    }
}
