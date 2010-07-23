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
                    Dump();
            }
        }

        public void Dump()
        {
            //TODO: this whole function should be in a thread maybe
            using (StreamWriter fileout = File.AppendText("dumplog.txt"))
            {
                for (int a = 0; a < frameIter; a++)
                {
                    for (int b = 0; b < ARRAYSIZE; b++)
                    {
                        fileout.Write(tempLog[a, b]);
                        if (b == (ARRAYSIZE - 1))
                            fileout.Write('\n');
                        else
                            fileout.Write(", ");
                    }
                }
                fileout.Close();
                frameIter = 0;
            }
        }
    }
}
