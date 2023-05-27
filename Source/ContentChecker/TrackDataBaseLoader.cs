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

namespace ContentChecker
{
    /// <summary>
    /// Loader class for .tdb files
    /// </summary>
    class TrackDataBaseLoader : Loader
    {
        TrackDatabaseFile TDBfile;
        /// <summary>
        /// Try to load the file.
        /// Possibly this might raise an exception. That exception is not caught here
        /// </summary>
        /// <param name="file">The file that needs to be loaded</param>
        public override void TryLoading(string file)
        {
            loadedFile = file;
            TDBfile = new TrackDatabaseFile(file);
        }

        protected override void AddDependentFiles()
        {
            string worldDirectory = Path.Combine(Path.GetDirectoryName(loadedFile), "WORLD");
            if (!Directory.Exists(worldDirectory)) { return; }

            var worldSoundFiles = Directory.GetFiles(worldDirectory, "*.ws", SearchOption.TopDirectoryOnly);
            foreach (string soundFile in worldSoundFiles)
            {
                AddAdditionalFileAction.Invoke(soundFile, new WorldSoundLoader(TDBfile.TrackDB));
            }
        }
    }
}
