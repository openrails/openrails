using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ORTS
{
    class DataLogger
    {
        private int MAXFRAMES;
        private int ARRAYSIZE;
        private String[,] tempLog;
        private int frameIter;
        private int fieldIter;

        public DataLogger(int MaxFrames, int ArraySize)
        {
            MAXFRAMES = MaxFrames;
            ARRAYSIZE = ArraySize;
            tempLog = new String[MAXFRAMES, ARRAYSIZE];
            frameIter = 0;
            fieldIter = 0;
        }

        public void Store(String data)
        {
            tempLog[frameIter, fieldIter] = data;
            fieldIter++;
            if (fieldIter == ARRAYSIZE)
            {
                fieldIter = 0;
                frameIter++;
                if (frameIter == MAXFRAMES)
                {
                    frameIter = 0;
                    //The next two lines are for thread safety, in case it tries to store more data into the log while it's dumping.
                    String[,] dumpLog = tempLog;
                    tempLog = new String[MAXFRAMES, ARRAYSIZE];
                    Dump(dumpLog);
                }
            }
        }

        private void Dump(String[,] dumpLog)
        {
            //TODO: this whole function should be in a thread maybe
            using (StreamWriter fileout = File.AppendText("dumplog.txt"))
            {
                for (int a = 0; a < MAXFRAMES; a++)
                {
                    for (int b = 0; b < ARRAYSIZE; b++)
                    {
                        fileout.Write(dumpLog[a, b]);
                        if (b == (ARRAYSIZE - 1))
                            fileout.Write('\n');
                        else
                            fileout.Write(", ");
                    }
                }
                fileout.Close();
            }
            //TODO: delete dumpLog here for memory conservation
        }
    }
}
