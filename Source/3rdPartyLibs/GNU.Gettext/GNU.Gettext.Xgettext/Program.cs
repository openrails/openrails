using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Diagnostics;

using GNU.Getopt;

namespace GNU.Gettext.Xgettext
{
	
	public class Options
	{
		public Options()
		{
			this.InputFiles = new List<string>();
			this.InputDirs = new List<string>();
			this.InputEncoding = new UTF8Encoding();
			this.SearchPatterns = new List<string>();
			this.OutFile = "messages.pot";
			this.DetectEncoding = false;
		}

        public string OutFile { get; set; }
		public bool Overwrite { get; set; }
		public bool Recursive { get; set; }
		public bool Verbose { get; set; }
		public bool ShowUsage { get; set; }
		public Encoding InputEncoding { get; set; }
		public bool DetectEncoding { get; set; }
		public List<string> InputFiles { get; private set; }
		public List<string> InputDirs { get; private set; }
		public List<string> SearchPatterns { get; private set; }
		
		public void SetEncoding(string encodingName)
		{
			InputEncoding = Encoding.GetEncoding(encodingName);
		}
	}
	
	class MainClass
	{
		public const String SOpts = "-:hjf:D:o:v";
		public static LongOpt[] LOpts
		{
			get
			{
				LongOpt[] lopts = new LongOpt[] 
				{
					new LongOpt("help", Argument.No, null, 'h'),
					new LongOpt("join-existing", Argument.No, null, 'j'),
					new LongOpt("recursive", Argument.No, null, 2),
					new LongOpt("directory", Argument.Required, null, 'D'),
					new LongOpt("search-pattern", Argument.Required, null, 3),
					new LongOpt("output", Argument.Required, null, 'o'),
					new LongOpt("from-code", Argument.Required, null, 4),
					new LongOpt("files-from", Argument.Required, null, 'f'),
					new LongOpt("detect-code", Argument.No, null, 5),
					new LongOpt("verbose", Argument.No, null, 'v')
				};
				return lopts;
			}
		}

		public static int Main(string[] args)
		{
			Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
			
			StringBuilder message;
			
            Options options = new Options();
			if (args.Length == 0)
			{
                PrintUsage();
				return 1;
			}
			else if (!GetOptions(args, SOpts, LOpts, options, out message))
			{
				Console.WriteLine(message.ToString());
				return 1;
			}
			else if (options.ShowUsage)
			{
                PrintUsage();
				return 0;
			}
			if (!AnalyseOptions(options, out message))
			{
				Console.WriteLine(message.ToString());
				return 1;
			}

            try
            {
				ExtractorCsharp extractor = new ExtractorCsharp(options);
				extractor.GetMessages();
				extractor.Save();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during execution: {0}", ex.Message);
                return 1;
            }
			
			Console.WriteLine("Template file '{0}' generated", options.OutFile);
			return 0;
			
		}
		
		public static bool GetOptions(string[] args, String sopts, LongOpt[] lopts, Options options, out StringBuilder message)
		{
			message = new StringBuilder();
            Getopt.Getopt getopt = new Getopt.Getopt(
                Assembly.GetExecutingAssembly().GetName().Name,
                args, sopts, lopts)
                {
                    Opterr = false
                };

			options.Verbose = false;
			options.ShowUsage = false;
			options.Recursive = false;
            options.Overwrite = true;

            int option;
            while ((option = getopt.getopt()) != -1)
            {
                switch (option)
                {
				case 1:
					options.InputFiles.Add(getopt.Optarg);
					break;
				case 2:
					options.Recursive = true;
					break;
				case 3:
					options.SearchPatterns.Add(getopt.Optarg);
					break;
				case 4:
					options.SetEncoding(getopt.Optarg);
					break;
				case 5:
					options.DetectEncoding = true;
					break;
				case ':':
					message.AppendFormat("Option '{0}' requires an argument", getopt.OptoptStr);
					return false;
				case '?':
					message.AppendFormat("Invalid option '{0}'", getopt.OptoptStr);
					return false;
				case 'f':
					string fileList = getopt.Optarg;
					Utils.FileUtils.ReadStrings(fileList, options.InputFiles);
					break;
				case 'j':
                    options.Overwrite = false;
                    break;
                case 'o':
                    options.OutFile = getopt.Optarg;
                    break;
                case 'D':
                    options.InputDirs.Add(getopt.Optarg);
                    break;
                case 'v':
                    options.Verbose = true;
                    break;
                case 'h':
					options.ShowUsage = true;
                    return true;
				default:
                    PrintUsage();
                    return false;
                }
            }

			if (getopt.Opterr)
			{
				message.AppendLine();
                message.Append("Error in command line options. Use -h to read options usage");
                return false;
			}
			return true;
		}

		public static bool AnalyseOptions(Options options, out StringBuilder message)
		{
			message = new StringBuilder();
            try
            {
                if (options.InputFiles.Count == 0)
                {
                    options.InputFiles.Add("*.cs");
                    options.InputFiles.Add("*.xaml");
                }
				if (options.InputDirs.Count == 0)
					options.InputDirs.Add(Environment.CurrentDirectory);

				foreach(string dir in options.InputDirs)
				{
	                if (!Directory.Exists(dir))
	                {
	                    message.AppendFormat("Input directory '{0}' not found", dir);
	                    return false;
	                }
				}
            }
            catch(Exception e)
            {
                message.Append(e.Message);
                return false;
            }

            return true;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Xgettext (Gettext.NET tools)");
            Console.WriteLine("Extract strings from C# source code files and then creates or updates PO template file");
            Console.WriteLine();
            Console.WriteLine("Usage:\n" +
            	"    {0}[.exe] [options] [inputfile | filemask] ...\n\n",
                Assembly.GetExecutingAssembly().GetName().Name);
            Console.WriteLine(
				"   -f, --files-from=file                  Read the names of the input files from file\n" +
				"                                          instead of getting them from the command line\n\n" +
            	"   -o file, --output=file                 Output PO template file name.\n" +
            	"                                          Using of '*.pot' file type is strongly recommended\n" +
            	"                                          \"{0}\" will be used if not specified\n\n" +
            	"   -D directory, --directory=directory    Input directory(ies) for C# source code files\n" +
            	"                                          Use multiples options to specify more directories\n\n" +
            	"   --recursive                            Process all subdirectories\n\n" +
				"   --search-pattern                       Custom regex pattern to find strings\n" +
				"                                          Macro {1} can be used for C# string\n" +
				"                                          Use multiples opions to specify more patterns\n\n" +
            	"   --from-code=name                       Specifies the encoding of the input files\n" +
            	"                                          Default is '{2}'\n\n" +
            	"   --detect-code                          Try detects the unicode encoding.\n" +
            	"                                          If not detected '--from-code' or default '{2}' will be used\n\n" +
				"   -j, --join-existing                    Join with existing file instead of overwrite\n\n" +
            	"   -v, --verbose                          Verbose output\n\n" +
            	"   -h, --help                             Display this help and exit",
				(new Options()).OutFile,
				ExtractorCsharp.CsharpStringPatternMacro,
				(new Options()).InputEncoding.BodyName
				);
        }
	}

}
