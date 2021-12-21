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

using Orts.Simulation;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Orts.Common.Scripting
{
    [CallOnThread("Loader")]
    public class ScriptManager
    {
        readonly Simulator Simulator;
        readonly IDictionary<string, Assembly> Scripts = new Dictionary<string, Assembly>();
        static readonly string[] ReferenceAssemblies = new[]
        {
            typeof(System.Object).GetTypeInfo().Assembly.Location,
            typeof(System.Diagnostics.Debug).GetTypeInfo().Assembly.Location,
            typeof(ORTS.Common.ElapsedTime).GetTypeInfo().Assembly.Location,
            typeof(ORTS.Scripting.Api.Timer).GetTypeInfo().Assembly.Location,
        };
        static MetadataReference[] References = ReferenceAssemblies.Select(r => MetadataReference.CreateFromFile(r)).ToArray();
        static CSharpCompilationOptions CompilationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            optimizationLevel: Debugger.IsAttached ? OptimizationLevel.Debug : OptimizationLevel.Release);

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
                var scriptName = Path.GetFileName(path);
                var scriptCode = File.ReadAllText(path);
                var syntaxTree = CSharpSyntaxTree.ParseText(scriptCode);
                var compilation = CSharpCompilation.Create(
                    scriptName,
                    new[] { syntaxTree },
                    References,
                    CompilationOptions);
                var ms = new MemoryStream();
                var result = compilation.Emit(ms);
                if (result.Success)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    var script = Assembly.Load(ms.ToArray());
                    // in netcore:
                    //var script = AssemblyLoadContext.Default.LoadFromStream(ms);
                    if (script == null)
                        Trace.TraceWarning($"Script file {path} could not be loaded into the process.");
                    return script;
                }
                else
                {
                    var errors = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

                    var errorString = new StringBuilder();
                    errorString.AppendFormat("Skipped script {0} with error:", path);
                    errorString.Append(Environment.NewLine);
                    foreach (var error in errors)
                    {
                        var textSpan = error.Location.SourceSpan;
                        var lineSpan = error.Location.SourceTree.GetLineSpan(textSpan);
                        var line = lineSpan.StartLinePosition.Line + 1;
                        var column = lineSpan.StartLinePosition.Character;
                        errorString.AppendFormat("\t{0}: {1}, line: {2}, column: {3}", error.Id, error.GetMessage(), line, column);
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
            return null;
            /*
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
                        errorString.AppendFormat("   {0}, file: {1}, line: {2}, column: {3}", error.ErrorText, error.FileName, error.Line, error.Column);
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
            */
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
