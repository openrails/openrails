using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ORTS
{
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

		public ORTraceListener(TextWriter writer)
		{
			Writer = writer;
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
			var output = new StringBuilder();
			output.AppendLine();
			output.AppendLine();
			output.AppendFormat("{0} : {1} : {2} : ", source, eventType, id);
			output.AppendFormat(format, args);
			if (eventCache.LogicalOperationStack.Contains(LogicalOperationWriteException))
			{
				var error = (Exception)args[0];
				output.AppendLine(error.ToString());
			}
            else //if (eventType != TraceEventType.Warning && eventType != TraceEventType.Information )
            {
                output.AppendLine();
                if ((TraceOutputOptions & TraceOptions.Callstack) != 0)
                    output.AppendLine(new StackTrace(true).ToString());
            }
            output.AppendLine();
 			Write(output);
		}

		public override void Write(string message)
		{
			Writer.Write(message);
		}

		public override void WriteLine(string message)
		{
			Writer.WriteLine(message);
		}

		public override void WriteLine(object o)
		{
			if (o is Exception)
			{
				Trace.CorrelationManager.StartLogicalOperation(LogicalOperationWriteException);
				Trace.TraceError("", o);
				Trace.CorrelationManager.StopLogicalOperation();
			}
			else
			{
				base.WriteLine(o);
			}
		}

		static readonly LogicalOperation LogicalOperationWriteException = new LogicalOperation();

		class LogicalOperation
		{
		}
	}
}
