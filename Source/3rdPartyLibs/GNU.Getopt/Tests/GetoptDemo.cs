/*
 * This sample code was written by Aaron M. Renn and is a demonstration
 * of how to utilize some of the features of the GNU getopt package. This
 * sample code is hereby placed into the public domain by the author and
 * may be used without restriction.
 * 
 * C#.NET Port by Klaus Prückl (klaus.prueckl@aon.at)
 */

using System;
using System.Text;

using Gnu.Getopt;

namespace Gnu.Getopt.Tests
{
	/// <summary>
	/// Summary description for TestApp.
	/// </summary>
	class GetoptDemo
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
#if true	// Getopt sample
			Getopt g = new Getopt("testprog", args, "ab:c::d");
			 
			int c;
			string arg;
			while ((c = g.getopt()) != -1)
			{
				switch(c)
				{
					case 'a':
					case 'd':
						Console.WriteLine("You picked " + (char)c );
						break;
				
					case 'b':
					case 'c':
						arg = g.Optarg;
						Console.WriteLine("You picked " + (char)c + 
							" with an argument of " +
							((arg != null) ? arg : "null") );
						break;
				
					case '?':
						break; // getopt() already printed an error
				
					default:
						Console.WriteLine("getopt() returned " + c);
						break;
				}
			}

#else		// Getopt/LongOpt docu sample
			int c;
			String arg;
			LongOpt[] longopts = new LongOpt[3];
			
			StringBuilder sb = new StringBuilder();
			longopts[0] = new LongOpt("help", Argument.No, null, 'h');
			longopts[1] = new LongOpt("outputdir", Argument.Required, sb, 'o'); 
			longopts[2] = new LongOpt("maximum", Argument.Optional, null, 2);
			
			Getopt g = new Getopt("testprog", args, "-:bc::d:hW;", longopts);
			g.Opterr = false; // We'll do our own error handling
			
			while ((c = g.getopt()) != -1)
				switch (c)
				{
					case 0:
						arg = g.Optarg;
						Console.WriteLine("Got long option with value '" +
							(char)int.Parse(sb.ToString())
							+ "' with argument " +
							((arg != null) ? arg : "null"));
						break;
						
					case 1:
						Console.WriteLine("I see you have return in order set and that " +
							"a non-option argv element was just found " +
							"with the value '" + g.Optarg + "'");
						break;
						
					case 2:
						arg = g.Optarg;
						Console.WriteLine("I know this, but pretend I didn't");
						Console.WriteLine("We picked option " +
							longopts[g.Longind].Name +
							" with value " + 
							((arg != null) ? arg : "null"));
						break;
						
					case 'b':
						Console.WriteLine("You picked plain old option " + (char)c);
						break;
						
					case 'c':
					case 'd':
						arg = g.Optarg;
						Console.WriteLine("You picked option '" + (char)c + 
							"' with argument " +
							((arg != null) ? arg : "null"));
						break;
						
					case 'h':
						Console.WriteLine("I see you asked for help");
						break;
						
					case 'W':
						Console.WriteLine("Hmmm. You tried a -W with an incorrect long " +
							"option name");
						break;
						
					case ':':
						Console.WriteLine("Doh! You need an argument for option " +
							(char)g.Optopt);
						break;
						
					case '?':
						Console.WriteLine("The option '" + (char)g.Optopt + 
							"' is not valid");
						break;
						
					default:
						Console.WriteLine("getopt() returned " + c);
						break;
				}
			
			for (int i = g.Optind; i < args.Length ; i++)
				Console.WriteLine("Non option argv element: " + args[i] + "\n");
#endif
		}
	}
}
