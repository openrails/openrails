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

using Orts.Formats.Msts;

namespace ContentChecker
{
    /// <summary>
    /// Loader class for .s files
    /// </summary>
    class WorldSoundLoader : Loader
    {
        /// <summary> The track database needed for loading a world-sound file </summary>
        private TrackDB _TDB;

        /// <summary>
        /// default constructor when not enough information is available
        /// </summary>
        public WorldSoundLoader() : base()
        {
            IsDependent = true;
        }

        /// <summary>
        /// Constructor giving the information this loaded depends on
        /// </summary>
        /// <param name="tdb">The Signal Configuration object</param>
        public WorldSoundLoader(TrackDB tdb) : this()
        {
            _TDB = tdb;
        }

        /// <summary>
        /// Try to load the file.
        /// Possibly this might raise an exception. That exception is not caught here
        /// </summary>
        /// <param name="file">The file that needs to be loaded</param>
        public override void TryLoading(string file)
        {
            TrItem[] items;
            if (_TDB == null)
            {
                //It is not clear that 10000 items is enough, which would give wrong warnings
                Console.WriteLine("");
                Console.Write("  Not using the proper .tdb, but just an empty array of TrItems for loading the .ws file: ");
                items = new TrItem[100000];
            }
            else
            {
                items = _TDB.TrItemTable;
            }
            var wf = new WorldSoundFile(file, items);
        }
    }
}
