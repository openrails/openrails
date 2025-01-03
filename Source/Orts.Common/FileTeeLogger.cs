// COPYRIGHT 2009 - 2024 by the Open Rails project.
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
using System.Text;

namespace ORTS.Common
{
    public class FileTeeLogger : TextWriter
    {
        public readonly TextWriter LogFile;
        public readonly TextWriter Console;

        public FileTeeLogger(string fileName, TextWriter console)
        {
            LogFile = new StreamWriter(fileName, true, Encoding);
            Console = console;
        }

        public override Encoding Encoding
        {
            get
            {
                return Encoding.UTF8;
            }
        }

        public override void Write(char value)
        {
            // Everything in TextWriter boils down to Write(char), but
            // actually implementing just this would be horribly inefficient
            // since we open and close the file every time. Instead, we
            // implement Write(string) and Write(char[], int, int) which
            // should mean we only end up here if called directly by user
            // code. Which we won't support unless necessary.
            throw new NotImplementedException();
        }

        public override void Write(string value)
        {
            Console.Write(value);
            LogFile.Write(value);
            LogFile.Flush();
        }

        public override void Write(char[] buffer, int index, int count)
        {
            Write(new string(buffer, index, count));
        }
    }
}
