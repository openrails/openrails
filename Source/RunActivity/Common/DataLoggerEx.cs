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

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ORTS
{
    class DataLoggerEx
    {

        const int m_CacheSize = 2048 * 1024;  // 2 Megs
        readonly StringBuilder m_Cache = new StringBuilder(m_CacheSize);
        readonly string m_fileName;
        bool m_isActive;

        bool FirstItem = true;

        public DataLoggerEx()
        {
            m_fileName = "dump.txt";
            m_isActive = true;
        }
        
        public DataLoggerEx(string fileName)
        {
            m_fileName = fileName;
            m_isActive = true;
        }

        ~DataLoggerEx()
        {
            Flush();
        }

        public void Start()
        {
            m_isActive = true;
        }

        public void Stop()
        {
            Flush();
            m_isActive = false;
        }

        public bool IsActive
        {
            get
            {
                return m_isActive;
            }
        }

        public void Data(string data)
        {
            if (m_isActive)
            {
                if (!FirstItem)
                    m_Cache.Append(',');
                m_Cache.Append(data);
                FirstItem = false;
            }
        }

        // Synonym for End() with a more descriptive name
        public void EndLine()
        {
            End();
        }

        public void End()
        {
            if (m_isActive)
            {
                m_Cache.AppendLine();
			    if (m_Cache.Length >= m_CacheSize)
				    Flush();
                FirstItem = true;
            }
        }

        public void Flush()
        {
            //TODO: this whole function should be in a thread maybe
            using (StreamWriter file = File.AppendText(m_fileName))
            {
                file.Write(m_Cache);
                file.Close();
            }
            m_Cache.Length = 0;
        }
    }
}
