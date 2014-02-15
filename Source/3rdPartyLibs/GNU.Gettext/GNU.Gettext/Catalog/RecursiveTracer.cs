using System;
using System.Text;
using System.IO;

namespace GNU.Gettext
{
	public class RecursiveTracer
	{
		public int Level { get; set; }
		public StringBuilder Text { get; private set; }
		
		public RecursiveTracer()
		{
			this.Text = new StringBuilder();
			this.Level = 0;
		}
		
		public void SaveToFile(string fileName)
		{
			using (StreamWriter outfile = new StreamWriter(fileName))
			{
				outfile.Write(Text.ToString());
			}
		}
		
		public void Indent()
		{
			for (int i = 0; i < Level; i++)
				Text.Append("\t");
		}
	}
}

