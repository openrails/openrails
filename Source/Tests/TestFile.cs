// COPYRIGHT 2015 by the Open Rails project.
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
using System.IO;
using System.Linq;
using System.Text;
using ORTS.Common;

namespace Tests
{
    public class TestFile : IDisposable
    {
        bool Disposed;

        public readonly string FileName;

        public TestFile(string contents) : this(contents, "") { }
        public TestFile(string contents, string extension)
        {
            FileName = Path.GetTempFileName();
            if (!string.IsNullOrEmpty(extension))
            {
                var newFileName = Path.ChangeExtension(FileName, extension);
                File.Move(FileName, newFileName);
                FileName = newFileName;
            }
            using (var writer = new StreamWriter(FileName))
            {
                writer.Write(contents);
            }
        }

        public void Cleanup()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void IDisposable.Dispose()
        {
            Cleanup();
        }

        ~TestFile()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed)
                return;

            if (disposing)
            {
                Vfs.Cleanup();
                File.Delete(FileName);
            }
            Disposed = true;
        }
    }
}
