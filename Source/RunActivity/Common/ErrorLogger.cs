using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ORTS
{
    public class ErrorLogger : Logger
    {
        public ErrorLogger(string filename)
            : base(filename)
        {
        }

        public override void WriteLine(string value)
        {
            console.WriteLine();
            console.WriteLine("ERROR: " + value);
            console.WriteLine();
            Warn("ERROR: " + value);
        }
    }

    public class Logger : TextWriter
    {
        static string WarningLogFileName = null;
        static protected TextWriter console = null;

        public Logger(string filename)
            : base()
        {
            if (WarningLogFileName == null)
                WarningLogFileName = filename;
            if (console == null)
                console = Console.Out;
        }

        public override void WriteLine(string value)
        {
            console.WriteLine(value);
            Warn(value);
        }

        public override void Write(string value)
        {
            console.Write(value);
        }

        public override void WriteLine()
        {
            console.WriteLine();
        }

        public override System.Text.Encoding Encoding
        {
            get { return System.Text.Encoding.ASCII; }
        }

        public void Warn(string s)
        {
            StreamWriter f;
            if (!File.Exists(WarningLogFileName))
            {
                f = new StreamWriter(WarningLogFileName);
            }
            else
            {
                f = new StreamWriter(WarningLogFileName, true); // append
            }

            f.WriteLine(s);
            f.WriteLine();
            f.Close();
        }

    }
}
