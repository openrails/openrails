// COPYRIGHT 2018 by the Open Rails project.
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

using System.Collections.Generic;
using System.Diagnostics;
using Orts.Formats.Msts;
using Path = System.IO.Path;

namespace ContentChecker
{
    /// <summary>
    /// Loader class for .sms files
    /// </summary>
    class SmsLoader : Loader
    {
        SoundManagmentFile sms;
        /// <summary>
        /// Try to load the file.
        /// Possibly this might raise an exception. That exception is not caught here
        /// </summary>
        /// <param name="file">The file that needs to be loaded</param>
        public override void TryLoading(string file)
        {
            loadedFile = file;
            sms = SharedSMSFileManager.Get(file);
        }

        protected override void AddReferencedFiles()
        {
            string routePath;
            string basePath;
            GetRouteAndBasePath(loadedFile, out routePath, out basePath);

            List<string> possiblePaths = new List<string>
            {
                Path.GetDirectoryName(loadedFile)
            };
            if (routePath != null)
            {
                possiblePaths.Add(Path.Combine(routePath, "SOUND"));
            }
            if (basePath != null)
            {
                possiblePaths.Add(Path.Combine(basePath, "SOUND"));
            }

            // Try to also load all sound files. This is tricky, beucase quit deep into the structure of a sms
            foreach (var group in sms.Tr_SMS.ScalabiltyGroups)
            {
                if (group.Streams == null) { continue; }
                foreach (var stream in group.Streams)
                {
                    foreach (var trigger in stream.Triggers)
                    {
                        SoundPlayCommand playCommand = trigger.SoundCommand as SoundPlayCommand;
                        if (playCommand == null) { continue; }
                        foreach (string file in playCommand.Files)
                        {
                            if (file == null)
                            {
                                Trace.TraceWarning("Missing well-defined file name in {0}\n", loadedFile);
                                continue;
                            }

                            //The file can be in multiple places
                            //Assume .wav file for now
                            var fullPath = Orts.Common.ORTSPaths.GetFileFromFolders(possiblePaths.ToArray(), file);
                            if (fullPath == null)
                            {
                                //apparently the file does not exist, but we want to make that known to the user, so we make a path anyway
                                fullPath = Path.Combine(possiblePaths[0], file);
                            }
                            AddAdditionalFileAction.Invoke(fullPath, new WavLoader());
                        }
                    }
                }

            }
        }

        void GetRouteAndBasePath(string file, out string routePath, out string basePath)
        {
            routePath = null;
            basePath = null;
            Stack<string> subDirectories = new Stack<string>();
            string directory = Path.GetDirectoryName(file);
            var root = Path.GetPathRoot(file);
            while (directory.Length > root.Length)
            {
                string subdDirectoryName = Path.GetFileName(directory);
                if (subdDirectoryName.ToLowerInvariant().Equals("routes"))
                {
                    routePath = Path.Combine(directory, subDirectories.Pop());
                    basePath = Path.GetDirectoryName(Path.GetDirectoryName(routePath));
                    return;
                }
                if (subdDirectoryName.ToLowerInvariant().Equals("trains"))
                {
                    basePath = Path.GetDirectoryName(directory);
                    return;
                }
                if (subdDirectoryName.ToLowerInvariant().Equals("trains"))
                {
                    basePath = Path.GetDirectoryName(directory);
                }
                if (subdDirectoryName.ToLowerInvariant().Equals("sound"))
                {
                    basePath = Path.GetDirectoryName(directory);
                }
                subDirectories.Push(Path.GetFileName(directory));
                directory = Path.GetDirectoryName(directory);
            }
        }
    }
}
