using System;
using System.IO;
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
}
