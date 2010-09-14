using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ORTS
{
    class DataLogger
    {
        readonly int CacheSize;
        const int LargeCacheSize = 2048 * 1024;  // 2 Megs
        readonly StringBuilder Cache = new StringBuilder();
        int CacheCount = 0;
        bool FirstItem = true;

        public DataLogger(int cacheSize)
        {
            CacheSize = cacheSize;
            Cache.EnsureCapacity(LargeCacheSize);
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
            /*
			if (++CacheCount >= CacheSize)
				Flush();
             */
            if (Cache.Length > LargeCacheSize)
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
            CacheCount = 0;
            Cache.EnsureCapacity(LargeCacheSize);
        }

        ~DataLogger()
        {
            Flush();
        }
    }
}
