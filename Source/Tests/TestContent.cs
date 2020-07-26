// COPYRIGHT 2020 by the Open Rails project.
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
using System.IO;
using System.Linq;
using static System.IO.Path;

namespace Tests
{
    /// <summary>
    /// Mocks an MSTS/ORTS content directory for integration testing.
    /// </summary>
    public class TestContent : IDisposable
    {
        /// <summary>
        /// The base path.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// The consist file directory.
        /// </summary>
        public string ConsistsPath => Mkdir("TRAINS", "CONSISTS");

        /// <summary>
        /// The rolling stock directory.
        /// </summary>
        public string TrainsetPath => Mkdir("TRAINS", "TRAINSET");

        /// <summary>
        /// The routes directory.
        /// </summary>
        public string RoutesPath => Mkdir("ROUTES");

        /// <summary>
        /// The global data directory.
        /// </summary>
        public string GlobalPath => Mkdir("GLOBAL");

        /// <summary>
        /// The global sound directory.
        /// </summary>
        public string SoundPath  => Mkdir("SOUND");

        /// <summary>
        /// Create a mock using a temporary folder.
        /// </summary>
        public TestContent()
        {
            Path = Combine(GetTempPath(), GetRandomFileName());
            Mkdir();
        }

        private string Mkdir(params string[] subPath)
        {
            string path = Combine(subPath.Prepend(Path).ToArray());
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// Delete the temporary folder and all of its contents.
        /// </summary>
        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, true);
        }
    }
}
