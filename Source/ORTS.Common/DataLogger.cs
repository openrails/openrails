// COPYRIGHT 2010 by the Open Rails project.
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
using System.Text;

namespace ORTS.Common
{
    public class DataLogger
    {
        const int CacheSize = 2048 * 1024;  // 2 Megs
        readonly string FilePath;
        readonly StringBuilder Cache = new StringBuilder(CacheSize);
        bool FirstItem = true;

        public enum Separators
        {
            comma = ',',
            semicolon = ';',
            tab = '\t',
            space = ' '
        };

        public Separators Separator = Separators.comma;

        public DataLogger(string filePath)
        {
            FilePath = filePath;
        }

        public void Data(string data)
        {
            if (!FirstItem)
                Cache.Append((char)Separator);
            Cache.Append(data);
            FirstItem = false;
        }

        public void End()
        {
            Cache.AppendLine();
            if (Cache.Length >= CacheSize)
                Flush();
            FirstItem = true;
        }

        public void Flush()
        {
            //TODO: this whole function should be in a thread maybe
            using (StreamWriter file = File.AppendText(FilePath))
            {
                file.Write(Cache);
            }
            Cache.Length = 0;
        }
    }
}
