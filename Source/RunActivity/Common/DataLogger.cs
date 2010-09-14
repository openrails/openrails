using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ORTS
{
    class DataLogger
    {
        const int CacheSize = 2048 * 1024;  // 2 Megs
        readonly StringBuilder Cache = new StringBuilder(CacheSize);
        bool FirstItem = true;

        public DataLogger()
        {
        }

        public void Data(string data)
        {
            if (!FirstItem)
                Cache.Append(',');
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
            using (StreamWriter file = File.AppendText("dump.csv"))
            {
                file.Write(Cache);
                file.Close();
            }
            Cache.Length = 0;
        }
    }
}
