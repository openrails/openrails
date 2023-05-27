// COPYRIGHT 2017 by the Open Rails project.
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
using Orts.Common;
using ORTS.Common;

namespace DataValidator
{
    class Program
    {
        static void Main(string[] args)
        {
            var verbose = args.Contains("/verbose", StringComparer.OrdinalIgnoreCase);
            var files = args.Where(arg => !arg.StartsWith("/"));
            if (files.Any())
                Validate(verbose, files);
            else
                ShowHelp();
        }

        static void ShowHelp()
        {
            var version = FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location);
            Console.WriteLine("{0} {1}", version.FileDescription, VersionInfo.VersionOrBuild);
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  {0} [options] <FILE> [...]", Path.GetFileNameWithoutExtension(version.FileName));
            Console.WriteLine();
            Console.WriteLine("Arguments:");
            Console.WriteLine("  <FILE>    Data files to validate; may contain wildcards");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  /verbose  Displays all expected/valid values in addition to any errors");
            Console.WriteLine("  /help     Show help and usage information");
            Console.WriteLine();
        }

        static void Validate(bool verbose, IEnumerable<string> files)
        {
            var traceListener = SetUpTracing(verbose);

            foreach (var file in files)
                Validate(file);

            ShowTracingReport(traceListener);
        }

        static ORTraceListener SetUpTracing(bool verbose)
        {
            // Captures Trace.Trace* calls and others and formats.
            var traceListener = new ORTraceListener(Console.Out);
            traceListener.TraceOutputOptions = TraceOptions.Callstack;
            if (verbose)
                traceListener.Filter = new EventTypeFilter(SourceLevels.Critical | SourceLevels.Error | SourceLevels.Warning | SourceLevels.Information);
            else
                traceListener.Filter = new EventTypeFilter(SourceLevels.Critical | SourceLevels.Error | SourceLevels.Warning);

            // Trace.Listeners and Debug.Listeners are the same list.
            Trace.Listeners.Add(traceListener);

            return traceListener;
        }

        static void ShowTracingReport(ORTraceListener traceListener)
        {
            Console.WriteLine();
            Console.WriteLine("Validator summary");
            Console.WriteLine("  Errors:        {0}", traceListener.Counts[0] + traceListener.Counts[1]);
            Console.WriteLine("  Warnings:      {0}", traceListener.Counts[2]);
            Console.WriteLine("  Informations:  {0}", traceListener.Counts[3]);
            Console.WriteLine();
        }

        static void Validate(string file)
        {
            Console.WriteLine("{0}: Begin", file);

            if (file.Contains("*"))
            {
                var path = Path.GetDirectoryName(file);
                var searchPattern = Path.GetFileName(file);
                foreach (var foundFile in Directory.GetFiles(path, searchPattern, SearchOption.AllDirectories))
                    Validate(foundFile);
            }
            else
            {

                if (!File.Exists(file))
                {
                    Trace.TraceError("Error: File does not exist");
                    return;
                }

                switch (Path.GetExtension(file).ToLowerInvariant())
                {
                    case ".t":
                        new TerrainValidator(file);
                        break;
                }
            }

            Console.WriteLine("{0}: End", file);
        }
    }
}
