// COPYRIGHT 2014 by the Open Rails project.
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

using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using Orts.Simulation;
using ORTS.Common;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Orts.Common.Scripting
{
    [CallOnThread("Loader")]
    public class ScriptManager
    {
        readonly Simulator Simulator;
        readonly IDictionary<string, Assembly> Scripts = new Dictionary<string, Assembly>();
        static readonly ProviderOptions ProviderOptions = new ProviderOptions(Path.Combine(new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase)).LocalPath, "roslyn", "csc.exe"), 10);
        static readonly CSharpCodeProvider Compiler = new CSharpCodeProvider(ProviderOptions);

        static CompilerParameters GetCompilerParameters()
        {
            var cp = new CompilerParameters()
            {
                GenerateInMemory = true,
                IncludeDebugInformation = Debugger.IsAttached,
            };
            cp.ReferencedAssemblies.Add("System.dll");
            cp.ReferencedAssemblies.Add("System.Core.dll");
            cp.ReferencedAssemblies.Add("ORTS.Common.dll");
            cp.ReferencedAssemblies.Add("Orts.Simulation.dll");
            return cp;
        }

        [CallOnThread("Loader")]
        internal ScriptManager(Simulator simulator)
        {
            Simulator = simulator;
        }

        public object Load(string[] pathArray, string name, string nameSpace = "ORTS.Scripting.Script")
        {
            if (Thread.CurrentThread.Name != "Loader Process")
                Trace.TraceError("ScriptManager.Load incorrectly called by {0}; must be Loader Process or crashes will occur.", Thread.CurrentThread.Name);

            if (pathArray == null || pathArray.Length == 0 || name == null || name == "")
                return null;

            if (Path.GetExtension(name) != ".cs")
                name += ".cs";

            var path = ORTSPaths.GetFileFromFolders(pathArray, name);

            if (path == null || path == "")
                return null;
            
            path = path.ToLowerInvariant();

            var type = String.Format("{0}.{1}", nameSpace, Path.GetFileNameWithoutExtension(path).Replace('-', '_'));

            if (!Scripts.ContainsKey(path))
                Scripts[path] = CompileScript(path);
            return Scripts[path]?.CreateInstance(type, true);
        }

        private static Assembly CompileScript(string path)
        {
            try
            {
                var compilerResults = Compiler.CompileAssemblyFromFile(GetCompilerParameters(), path);
                if (!compilerResults.Errors.HasErrors)
                {
                    var script = compilerResults.CompiledAssembly;
                    if (script == null)
                        Trace.TraceWarning($"Script file {path} could not be loaded into the process.");
                    return script;
                }
                else
                {
                    var errorString = new StringBuilder();
                    errorString.AppendFormat("Skipped script {0} with error:", path);
                    errorString.Append(Environment.NewLine);
                    foreach (CompilerError error in compilerResults.Errors)
                    {
                        errorString.AppendFormat("   {0}, line: {1}, column: {2}", error.ErrorText, error.Line /*- prefixLines*/, error.Column);
                        errorString.Append(Environment.NewLine);
                    }

                    Trace.TraceWarning(errorString.ToString());
                    return null;
                }
            }
            catch (InvalidDataException error)
            {
                Trace.TraceWarning("Skipped script {0} with error: {1}", path, error.Message);
                return null;
            }
            catch (Exception error)
            {
                if (File.Exists(path))
                    Trace.WriteLine(new FileLoadException(path, error));
                else
                    Trace.TraceWarning("Ignored missing script file {0}", path);
                return null;
            }
        }

        public Assembly LoadFolder(string path)
        {
            if (Thread.CurrentThread.Name != "Loader Process")
                Trace.TraceError("ScriptManager.Load incorrectly called by {0}; must be Loader Process or crashes will occur.", Thread.CurrentThread.Name);

            if (path == null || path == "")
                return null;

            if (!Directory.Exists(path)) return null;

            var files = Directory.GetFiles(path, "*.cs");

            if (files == null || files.Length == 0) return null;

            try
            {
                var compilerResults = Compiler.CompileAssemblyFromFile(GetCompilerParameters(), files);
                if (!compilerResults.Errors.HasErrors)
                {
                    return compilerResults.CompiledAssembly;
                }
                else
                {
                    var errorString = new StringBuilder();
                    errorString.AppendFormat("Skipped script folder {0} with error:", path);
                    errorString.Append(Environment.NewLine);
                    foreach (CompilerError error in compilerResults.Errors)
                    {
                        errorString.AppendFormat("   {0}, file: {1}, line: {2}, column: {3}", error.ErrorText, error.FileName, error.Line /*- prefixLines*/, error.Column);
                        errorString.Append(Environment.NewLine);
                    }

                    Trace.TraceWarning(errorString.ToString());
                    return null;
                }
            }
            catch (InvalidDataException error)
            {
                Trace.TraceWarning("Skipped script folder {0} with error: {1}", path, error.Message);
                return null;
            }
            catch (Exception error)
            {
                Trace.WriteLine(new FileLoadException(path, error));
                return null;
            }
        }

        /*
        static ClassType CreateInstance<ClassType>(Assembly assembly) where ClassType : class
        {
            foreach (var type in assembly.GetTypes())
                if (typeof(ClassType).IsAssignableFrom(type))
                    return Activator.CreateInstance(type) as ClassType;

            return default(ClassType);
        }
        */

        [CallOnThread("Updater")]
        public string GetStatus()
        {
            return String.Format("{0:F0} scripts", Scripts.Keys.Count);
        }
    }
}
