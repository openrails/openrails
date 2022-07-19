// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Orts.Common
{
    public class NullLogger : TextWriter
    {
        public NullLogger()
        {
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
        }
    }

    public class FileTeeLogger : TextWriter
    {
        public readonly string FileName;
        public readonly TextWriter Console;

        public FileTeeLogger(string fileName, TextWriter console)
        {
            FileName = fileName;
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
            using (var writer = new StreamWriter(FileName, true, Encoding))
            {
                writer.Write(value);
            }
        }

        public override void Write(char[] buffer, int index, int count)
        {
            Write(new String(buffer, index, count));
        }
    }

    public class ORTraceListener : TraceListener
    {
        public readonly TextWriter Writer;
        public readonly bool OnlyErrors;
        public readonly int[] Counts = new int[5];
        bool LastWrittenFormatted;

        public ORTraceListener(TextWriter writer)
            : this(writer, false)
        {
        }

        public ORTraceListener(TextWriter writer, bool onlyErrors)
        {
            Writer = writer;
            OnlyErrors = onlyErrors;
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
        {
            if ((Filter == null) || Filter.ShouldTrace(eventCache, source, eventType, id, null, null, null, null))
                TraceEventInternal(eventCache, source, eventType, id, "", new object[0]);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            if ((Filter == null) || Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
                TraceEventInternal(eventCache, source, eventType, id, message, new object[0]);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            if ((Filter == null) || Filter.ShouldTrace(eventCache, source, eventType, id, format, args, null, null))
                TraceEventInternal(eventCache, source, eventType, id, format, args);
        }

        void TraceEventInternal(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, object[] args)
        {
            // Convert eventType (an enum) back to an index so we can count the different types of error separately.
            var errorLevel = (int)Math.Round(Math.Log((int)eventType) / Math.Log(2));
            if (errorLevel < Counts.Length)
                Counts[errorLevel]++;
            
            // Event is less important than error (and critical) and we're logging only errors... bail.
            if (eventType > TraceEventType.Error && OnlyErrors)
                return;

            var output = new StringBuilder();
            if (!LastWrittenFormatted)
            {
                output.AppendLine();
                output.AppendLine();
            }
            output.Append(eventType);
            output.Append(": ");
            if (args.Length == 0)
                output.Append(format);
            else
                output.AppendFormat(format, args);

            // Log exception details if it is an exception.
            if (eventCache.LogicalOperationStack.Contains(LogicalOperationWriteException))
            {
                // Attempt to clean up the stacks; the problem is that the exception stack only goes as far back as the call made inside the try block. We also have access to the
                // full stack to this trace call, which goes via the catch block at the same level as the try block. We'd prefer to have the whole stack, so we need to find the
                // join and stitch the stacks together.
                var error = args[0] as Exception;
                var errorStack = new StackTrace(args[0] as Exception);
                var errorStackLast = errorStack.GetFrame(errorStack.FrameCount - 1);
                var catchStack = new StackTrace();
                var catchStackIndex = 0;
                while (catchStackIndex < catchStack.FrameCount && errorStackLast != null && catchStack.GetFrame(catchStackIndex).GetMethod().Name != errorStackLast.GetMethod().Name)
                    catchStackIndex++;
                catchStack = new StackTrace(catchStackIndex < catchStack.FrameCount ? catchStackIndex + 1 : 0, true);

                output.AppendLine(error.ToString());
                output.AppendLine(catchStack.ToString());
            }
            else
            {
                output.AppendLine();

                // Only log a stack trace for critical and error levels.
                if ((eventType < TraceEventType.Warning) && (TraceOutputOptions & TraceOptions.Callstack) != 0)
                    output.AppendLine(new StackTrace(true).ToString());
            }

            output.AppendLine();
            Writer.Write(output);
            LastWrittenFormatted = true;
        }

        public override void Write(string message)
        {
            if (!OnlyErrors)
            {
                Writer.Write(message);
                LastWrittenFormatted = false;
            }
        }

        public override void WriteLine(string message)
        {
            if (!OnlyErrors)
            {
                Writer.WriteLine(message);
                LastWrittenFormatted = false;
            }
        }

        public override void WriteLine(object o)
        {
            if (o is Exception)
            {
                Trace.CorrelationManager.StartLogicalOperation(LogicalOperationWriteException);
                if (o is FatalException)
                    Trace.TraceError("", (o as FatalException).InnerException);
                else
                    Trace.TraceWarning("", o);
                Trace.CorrelationManager.StopLogicalOperation();
            }
            else if (!OnlyErrors)
            {
                base.WriteLine(o);
                LastWrittenFormatted = false;
            }
        }

        static readonly LogicalOperation LogicalOperationWriteException = new LogicalOperation();

        class LogicalOperation
        {
        }
    }

    public sealed class FatalException : Exception
    {
        public FatalException(Exception innerException)
            : base("A fatal error has occurred", innerException)
        {
            Debug.Assert(innerException != null, "The inner exception of a FatalException must not be null.");
        }
    }
}
