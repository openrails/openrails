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
    public class TestContent : IDisposable
    {
        private bool Disposed = false;

        public string Path { get; }

        public string ConsistsPath { get => Mkdir("trains", "consists"); }

        public string TrainsetPath { get => Mkdir("trains", "trainset"); }

        public string RoutesPath { get => Mkdir("routes"); }

        public string SoundPath { get => Mkdir("sound"); }

        public TestContent() => Path = Combine(GetTempPath(), GetRandomFileName());

        private string Mkdir(params string[] subPath)
        {
            string path = Combine(subPath.Prepend(Path).ToArray());
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        public void Dispose()
        {
            if (!Disposed)
                Directory.Delete(Path, true);
            Disposed = true;
        }
    }
}
