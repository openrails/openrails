// COPYRIGHT 2016 by the Open Rails project.
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using ORTS.Common;

namespace Orts.DataConverter
{
    class Program
    {
        //TODO Add a return code for success or not
        static void Main(string[] args)
        {
            var converters = new List<IDataConverter>()
                { new TerrainConverter()
                , new ClockConverter()
                };

            try
            {
                var conversions = GetConversions(args);
                if (conversions.Count == 0)
                {
                    ShowHelp(converters);
                    return;
                }

                foreach (var conversion in conversions)
                {
                    var valid = false;
                    // Apply each converter in turn to the conversion
                    foreach (var converter in converters)
                    {
                        if (converter.DoConversion(conversion))
                        {
                            valid = true;
                            Console.WriteLine(conversion.Input);
                            foreach (var output in conversion.Output)
                            {
                                Console.WriteLine("--> {0}", output);
                            }
                            // After a successful conversion, do not apply any more converters.
                            break;
                        }
                    }

                    if (!valid)
                    {
                        throw new InvalidCommandLineException("No conversion available for " + conversion.Input);
                    }
                }
            }
            catch (FileNotFoundException error)
            {
                Console.WriteLine("Error: File not found: " + error.FileName);
            }
            catch (InvalidCommandLineException error)
            {
                Console.WriteLine("Error: " + error.Message);
                Console.WriteLine();
                ShowHelp(converters);
            }
            catch (Orts.Parsers.Msts.STFException)
            {
                // End with failure
            }
        }

        static void ShowHelp(List<IDataConverter> converters)
        {
            var version = FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location);
            Console.WriteLine("{0} {1}", version.FileDescription, VersionInfo.VersionOrBuild);
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  {0} /input <INPUT> [/output] [<OUTPUT> [...]]", Path.GetFileNameWithoutExtension(version.FileName));
            Console.WriteLine();
            Console.WriteLine("Arguments:");
            Console.WriteLine("  <INPUT>   Specifies the file to read");
            Console.WriteLine("  <OUTPUT>  Specifies the file to generate");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  /input   Indicate that next argument is input");
            Console.WriteLine("  /output  Indicate that following arguments are outputs");
            Console.WriteLine("  /help    Show help and usage information");
            Console.WriteLine();
            Console.WriteLine("Multiple outputs may be specified for each input");
            Console.WriteLine();
            Console.WriteLine("Available file format conversions:");
            Console.WriteLine("               Input  Output              Description");
            foreach (var converter in converters)
            {
                converter.ShowConversions();
            }
            Console.WriteLine();
        }

        static List<DataConversion> GetConversions(string[] args)
        {
            var conversions = new List<DataConversion>();
            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "/input":
                        i++;
                        if (i >= args.Length)
                        {
                            throw new InvalidCommandLineException("Expected input filename; got end of input");
                        }
                        if (args[i].StartsWith("/"))
                        {
                            throw new InvalidCommandLineException("Expected input filename; got option " + args[i]);
                        }
                        conversions.Add(new DataConversion(args[i].ToLowerInvariant()));
                        break;
                    case "/output":
                        if (conversions.Count == 0)
                        {
                            throw new InvalidCommandLineException("Expected /input; got /output");
                        }
                        if (conversions.Last().Output.Count > 0)
                        {
                            throw new InvalidCommandLineException("Expected output filename or /input; got /output");
                        }
                        i++;
                        if (i >= args.Length)
                        {
                            throw new InvalidCommandLineException("Expected output filename; got end of input");
                        }
                        goto default;
                    default:
                        if (conversions.Count == 0)
                        {
                            throw new InvalidCommandLineException("Expected /input; got " + args[i]);
                        }
                        if (args[i].StartsWith("/"))
                        {
                            throw new InvalidCommandLineException("Expected output filename; got option " + args[i]);
                        }
                        conversions.Last().Output.Add(args[i].ToLowerInvariant());
                        break;
                }
            }
            return conversions;
        }
    }

    [Serializable]
    internal class InvalidCommandLineException : Exception
    {
        public InvalidCommandLineException()
        {
        }

        public InvalidCommandLineException(string message) : base(message)
        {
        }

        public InvalidCommandLineException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidCommandLineException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    public class DataConversion
    {
        public string Input { get; private set; }
        public List<string> Output { get; private set; }
        public DataConversion(string input)
        {
            Input = input;
            Output = new List<string>();
        }
        public void SetInput(string input)
        {
            Input = input;
        }
    }

    public interface IDataConverter
    {
        void ShowConversions();
        bool DoConversion(DataConversion conversion);
    }
}
