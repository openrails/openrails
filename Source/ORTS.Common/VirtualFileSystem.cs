// COPYRIGHT 2021 by the Open Rails project.
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
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;
using SharpCompress.Archives;
using SharpCompress.Readers;

namespace ORTS.Common
{
    /// <summary>
    /// Virtual File System. Can be <see cref="Initialize(string)"/>'d from a config file with lines e.g. C:\dir\archive.zip\subdir\ /MSTS/mountpoint/,
    /// or from a directory. Found subsequent archives are auto-mounted to their respective locations.
    /// <br><see cref="Path.Combine"/> like functions continue to work on virtual paths, as well as both the "\" and "/" separators.
    /// But <see cref="Path.GetFullPath"/> must never be used on them, because that adds an unwanted drive letter.</br>
    /// <br><see cref="File.Exists"/> becomes <see cref="Vfs.FileExists"/>, <see cref="Directory.Exists"/> becomes <see cref="Vfs.DirectoryExists"/>,
    /// <see cref="Directory.GetFiles(string, string)"/> becomes <see cref="Vfs.GetFiles"/>.</br>
    /// </summary>
    public static class Vfs
    {
        static VfsNode VfsRoot;
        static readonly Stack<(string, VfsNode)> Stack = new Stack<(string, VfsNode)>();
        static readonly Dictionary<string, IArchive> OpenArchives = new Dictionary<string, IArchive>();
        static readonly List<string> SupportedArchiveExtensions = new List<string> { ".zip", ".rar", ".7z" };

        ///////////////////////////////////////////////////////////////////////////////////////
        ///
        /// The configuration file consits of lines of format:
        /// Drive:\SystemPath\ /MountPoint/
        ///
        /// SystemPath - may use "/" or "\", but MountPoint may use "/" only as a directory separator.
        ///            - with spaces in it must be quoted, e.g. "C:\My Routes\USA85\" /MSTS/ROUTES/USA85/
        ///            - referring to a directory must end with "/" or "\" to assure that only _below_ dirs and files will be visible _below_ the mount point.
        ///            - referring to an archive must not end with "/" or "\", e.g. C:\TEMP\MSTS1.2.zip /MSTS/
        ///            - may refer to an archive subdirectory, e.g.: C:\routes.zip\USA3\ /MSTS/ROUTES/USA3/
        ///            - may _not_ refer to a single non-archive file, neither a non-archive file within an archive.
        ///            - is case-insenditive.
        /// MountPoint - must end with "/" to avoid confusion and to assure that mounting will be done _below_ the given path.
        ///            - may not contain spaces, may not be quoted.
        ///            - is case-insensitive, will be converted to all-uppercase internally.
        ///
        /// Found subsequent archives are auto-mounted to their respective VFS locations, but nested archives are unsupported.
        ///
        /// System.IO.Path.*() functions can be used on virtual paths, but make sure that
        /// NormalizeVirtualPath() is used on them _afterwards_ in the processing pipeline somewhere.
        ///
        ///////////////////////////////////////////////////////////////////////////////////////
        ///
        /// Public interfaces
        /// 

        public const string MstsBasePath = "/MSTS/";
        public const string ExecutablePath = "/EXECUTABLE/";

        public static bool AccessLoggingEnabled { get; set; }

        public static void Initialize(string initPath, string executablePath)
        {
            if (File.Exists(initPath))
                Initialize(File.ReadAllLines(initPath), executablePath);
            else if (Directory.Exists(initPath))
                Initialize(new[] { $"\"{initPath}/\" {MstsBasePath}" }, executablePath);
            else
                Trace.TraceError($"VFS: Could not initialize from {initPath}, aborting.");
        }

        public static StreamReader StreamReader(string vfsPath, bool detectEncodingFromByteOrderMarks)
        {
            switch (PrepareForRead(vfsPath))
            {
                case Stream stream: return new StreamReader(stream, detectEncodingFromByteOrderMarks);
                case string path: return new StreamReader(path, detectEncodingFromByteOrderMarks);
                default: throw new FileNotFoundException($"VFS reading failed: {vfsPath}");
            }
            // Looks like the rest of the OR code expects to throw an exception at these places instead of just returning null.
        }

        public static StreamReader StreamReader(string vfsPath, Encoding encoding)
        {
            switch (PrepareForRead(vfsPath))
            {
                case Stream stream: return new StreamReader(stream, encoding);
                case string path: return new StreamReader(path, encoding);
                default: throw new FileNotFoundException($"VFS reading failed: {vfsPath}");
            }
        }

        public static StreamReader OpenText(string vfsPath)
        {
            switch (PrepareForRead(vfsPath))
            {
                case Stream stream: return new StreamReader(stream, Encoding.UTF8);
                case string path: return File.OpenText(path);
                default: throw new FileNotFoundException($"VFS reading failed: {vfsPath}");
            }
        }

        public static Stream OpenRead(string vfsPath)
        {
            switch (PrepareForRead(vfsPath))
            {
                case Stream stream: return stream;
                case string path: return File.OpenRead(path);
                default: throw new FileNotFoundException($"VFS reading failed: {vfsPath}");
            }
        }

        public static Stream OpenCreate(string vfsPath)
        {
            switch (PrepareForWrite(vfsPath))
            {
                case Stream stream: return stream;
                case string path: return new FileStream(path, FileMode.Create);
                default: throw new FileNotFoundException($"VFS writing failed: {vfsPath}");
            }
        }

        public static string[] GetDirectories(string vfsPath) => GetDirectoriesBase(vfsPath) ?? Array.Empty<string>();
        public static bool TryGetDirectories(string vfsPath, out string[] directories) => (directories = GetDirectoriesBase(vfsPath)) != null;

        public static string[] GetFiles(string vfsPath, string searchPattern) => GetFilesBase(vfsPath, searchPattern) ?? Array.Empty<string>();
        public static bool TryGetFiles(string vfsPath, string searchPattern, out string[] files) => (files = GetFilesBase(vfsPath, searchPattern)) != null;

        public static bool DirectoryExists(string vfsPath) => VfsRoot.ChangeDirectory(NormalizeVirtualPath(vfsPath), false) != null;
        public static bool FileExists(string vfsPath) => !VfsRoot.ChangeDirectory(NormalizeVirtualPath(Path.GetDirectoryName(vfsPath)), false)
                                                            ?.GetNode(NormalizeVirtualPath(Path.GetFileName(vfsPath)))?.IsDirectory ?? false;

        public static void DebugDump()
        {
            Trace.TraceInformation($"VFS: Start of hierarchy dump");
            Stack.Clear();
            Stack.Push(("/", VfsRoot));
            while (Stack.Count > 0)
            {
                var (dirpath, vfsNode) = Stack.Pop();
                Trace.TraceInformation($"VFS: {dirpath}" + (vfsNode.IsDirectoryWritable() ? $" <= {vfsNode.AbsolutePath}" : ""));
                foreach (var subdir in vfsNode.GetDirectories())
                    Stack.Push((subdir, vfsNode.ChangeDirectory(NormalizeVirtualPath(Path.GetFileName(subdir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))), false)));
                foreach (var file in vfsNode.GetFiles())
                {
                    var node = vfsNode.GetNode(Path.GetFileName(file));
                    Trace.TraceInformation($"VFS: {file} <= " + (node.IsArchiveFile() ? $"[{node.AbsolutePath}]/{node.SubPath}" : node.AbsolutePath));
                }
            }
            Trace.TraceInformation($"VFS: End of hierarchy dump");
        }
        ///
        /// End of interfaces.
        /// 
        ///////////////////////////////////////////////////////////////////////////////////////

        static void Initialize(string[] settings, string executablePath)
        {
            VfsRoot =  new VfsNode("", true);

            OpenArchives.Clear();
            Stack.Clear();

            Match match;
            foreach (var line in settings)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                if (!(match = Regex.Match(line, @"^("".+""|\S+) +(/MSTS/|/OR/)([\S]+/)*")).Success)
                {
                    Trace.TraceWarning($"VFS mount: Cannot parse configuration line, skipping: {line}");
                    continue;
                }

                // Mountpoint validation:
                var mountpoint = match.Groups[2].Value + match.Groups[3].Value;
                if (!mountpoint.EndsWith("/"))
                {
                    Trace.TraceWarning($"VFS mount: Mount point doesn't end with slash (/), skipping: {line}");
                    continue;
                }
                mountpoint = NormalizeVirtualPath(mountpoint);

                //Source path validation:
                var sourcePath = NormalizeSystemPath(match.Groups[1].Value.Trim('"'));
                var sourcePieces = sourcePath.Split('/');
                if (sourcePieces.Length > 0 && sourcePieces.Last() == "" && Directory.Exists(sourcePath))
                {
                    // A bare mountable directory must end with "/"
                    MountDirectory(sourcePath, mountpoint);
                }
                else if (sourcePieces.Length > 1 && sourcePieces.Last() != "" && File.Exists(sourcePath))
                {
                    // An archive file reference should not end with "/"
                    if (IsArchiveSupported(sourcePath))
                        MountArchive(sourcePath, null, mountpoint);
                    else
                        Trace.TraceWarning($"VFS mount: Source archive format is not supported, skipping: {line}");
                }
                else if (sourcePieces.Length > 0)
                {
                    // Trying to find the base archive in a directory-within-archive or archive-within-archive case.
                    // Start from 1 ignoring the drive letter checking.
                    for (var i = 1; i < sourcePieces.Length - 1; i++)
                    {
                        var archivePath = string.Join("/", sourcePieces, 0, i + 1);
                        if (File.Exists(archivePath))
                        {
                            if (IsArchiveSupported(archivePath))
                                MountArchive(archivePath, string.Join("/", sourcePieces, i + 1, sourcePieces.Length - 1 - i), mountpoint);
                            else
                                Trace.TraceWarning($"VFS mount: Source archive format is not supported, skipping: {line}");
                            break;
                        }
                        if (!Directory.Exists(archivePath))
                        {
                            Trace.TraceWarning($"VFS mount: Source file not found, skipping: {line}");
                            break;
                        }
                    }
                }
                else
                {
                    Trace.TraceWarning($"VFS mount: Cannot parse configuration line, skipping: {line}");
                }
            }

            if (executablePath != null)
                MountDirectory(NormalizeSystemPath(executablePath), ExecutablePath);

            //DebugDump();
        }

        static void MountDirectory(string directory, string mountpoint)
        {
            Debug.Assert(VfsRoot != null, "VFS is uninitialized");

            Trace.TraceInformation($"VFS mount system directory: {directory} => {mountpoint}");
            Stack.Clear();
            Stack.Push((directory, VfsRoot.ChangeDirectory(mountpoint, true)));
            while (Stack.Count > 0)
            {
                var (dirpath, vfsNode) = Stack.Pop();
                foreach (var subdir in Directory.GetDirectories(dirpath))
                {
                    var name = NormalizeVirtualPath(Path.GetFileName(subdir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));
                    Stack.Push((subdir, vfsNode.CreateDirectory(name, subdir)));
                }
                foreach (var file in Directory.GetFiles(dirpath))
                {
                    if (IsArchiveSupported(file))
                        MountArchive(file, null, vfsNode.GetVfsPath());
                    else
                        vfsNode.CreateFile(NormalizeVirtualPath(Path.GetFileName(file)), file);
                }
            }
        }

        static void MountArchive(string archivePath, string subPath, string mountpoint)
        {
            Debug.Assert(VfsRoot != null, "VFS is uninitialized");

            var subPathIsDirectory = subPath?.EndsWith("/") ?? false;
            try
            {
                using (var archive = ArchiveFactory.Open(new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    Trace.TraceInformation($"VFS mount archive: [{archivePath}]/{subPath ?? ""} => {mountpoint}");
                    var mountNode = VfsRoot.ChangeDirectory(mountpoint, true);
                    foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                    {
                        var key = NormalizeVirtualPath(entry.Key);
                        if (subPath == null || subPathIsDirectory && key.StartsWith(subPath, StringComparison.OrdinalIgnoreCase))
                        {
                            var relativePath = key.Substring(subPath?.Length ?? 0);

                            if (IsArchiveSupported(key))
                            {
                                // TODO: archive-in-archive
                            }

                            mountNode.ChangeDirectory(NormalizeVirtualPath(Path.GetDirectoryName(relativePath)), true)
                                .CreateFile(Path.GetFileName(relativePath), archivePath, entry.Key, false);
                        }
                    }
                }
            }
            catch
            {
                Trace.TraceWarning($"VFS mount: Could not open archive {archivePath}, skipping {subPath ?? string.Empty} => {mountpoint}");
            }
        }

        static string[] GetDirectoriesBase(string vfsPath) => VfsRoot.ChangeDirectory(NormalizeVirtualPath(vfsPath), false)?.GetDirectories()?.ToArray();
        static string[] GetFilesBase(string vfsPath, string searchPattern) => VfsRoot.ChangeDirectory(NormalizeVirtualPath(vfsPath), false)?.GetFiles()
                .Where(f => Regex.IsMatch(f.Split('/').Last(), NormalizeVirtualPath(searchPattern).Replace(".", @"\.").Replace("*", ".*")))?.ToArray();

        static object PrepareForRead(string vfsPath)
        {
            Debug.Assert(VfsRoot != null, "VFS is uninitialized");

            vfsPath = NormalizeVirtualPath(vfsPath);
            var foundNode = VfsRoot.GetNode(vfsPath);
            if (foundNode != null && foundNode.IsArchiveFile() && File.Exists(foundNode.AbsolutePath))
            {
                // Reading from an archive is not thread safe, because only one internal stream exists there.
                // Thus an archive must be opened as many times as the number of the threads wants to read from it.
                var threadId = Thread.CurrentThread.Name ?? $"#{Thread.CurrentThread.ManagedThreadId}";
                var archiveKey = $"{threadId}@{foundNode.AbsolutePath}";
                if (!OpenArchives.TryGetValue(archiveKey, out var archive))
                {
                    archive = ArchiveFactory.Open(File.OpenRead(foundNode.AbsolutePath), new ReaderOptions() { LeaveStreamOpen = true });
                    if (AccessLoggingEnabled)
                        Trace.TraceInformation($"VFS opening archive file for thread {archiveKey}");
                    OpenArchives.Add(archiveKey, archive);
                }

                var archiveEntry = archive.Entries.Where(entry => entry.Key == foundNode.SubPath).FirstOrDefault();
                if (archiveEntry != null)
                {
                    if (AccessLoggingEnabled)
                        Trace.TraceInformation($"VFS reading archive node: [{foundNode.AbsolutePath}]/{archiveEntry.Key} => {vfsPath}");
                    return archiveEntry.OpenEntryStream();
                }
            }
            else if (foundNode != null && foundNode.IsRegularFile() && File.Exists(foundNode.AbsolutePath))
            {
                if (AccessLoggingEnabled)
                    Trace.TraceInformation($"VFS reading system file: {foundNode.AbsolutePath} => {vfsPath}");
                return foundNode.AbsolutePath;
            }
            if (AccessLoggingEnabled)
                Trace.TraceInformation($"VFS reading failed: null => {vfsPath}");
            return null;
        }

        static object PrepareForWrite(string vfsPath)
        {
            Debug.Assert(VfsRoot != null, "VFS is uninitialized");

            vfsPath = NormalizeVirtualPath(vfsPath);
            var foundNode = VfsRoot.GetNode(vfsPath);
            if (foundNode != null && foundNode.IsRegularFile() && File.Exists(foundNode.AbsolutePath))
            {
                if (AccessLoggingEnabled)
                    Trace.TraceInformation($"VFS writing system file: {foundNode.AbsolutePath} => {vfsPath}");
                return foundNode.AbsolutePath;
            }
            else
            {
                var parent = VfsRoot.GetNode(NormalizeVirtualPath(Path.GetDirectoryName(vfsPath)));
                if (parent?.IsDirectoryWritable() ?? false)
                {
                    var filename = Path.GetFileName(vfsPath);
                    var fullName = Path.Combine(parent.AbsolutePath, Path.GetFileName(vfsPath));
                    FileStream stream = null;
                    try
                    {
                        // Create the real file first, the virtual node second. This is to retain consistency.
                        // Would be good to move the new FileStream() out of here, but I am not dare enough. - PG
                        stream = new FileStream(fullName, FileMode.Create);
                        parent.CreateFile(filename, fullName);
                        if (AccessLoggingEnabled)
                            Trace.TraceInformation($"VFS writing system file: {fullName} => {vfsPath}");
                        return stream;
                    }
                    catch
                    {
                        stream.Close();
                    }
                }
            }
            if (AccessLoggingEnabled)
                Trace.TraceInformation($"VFS writing failed: null => {vfsPath}");
            return null;
        }

        static bool IsArchiveSupported(string filename) => SupportedArchiveExtensions.Contains(Path.GetExtension(filename).ToLower());

        static string NormalizeSystemPath(string path) => Path.GetFullPath(new Uri(path).LocalPath).Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/').Trim('"');
        static string NormalizeVirtualPath(string path) => path.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/').Trim('"').ToUpperInvariant();
    }

    internal class VfsNode
    {
        readonly Dictionary<string, VfsNode> Children = new Dictionary<string, VfsNode>();
        readonly VfsNode Parent;
        readonly string Name;
        
        public string AbsolutePath { get; }
        public string SubPath { get; }
        public bool IsDirectory { get; }

        public VfsNode(string name, bool isDirecotry) : this(name) => IsDirectory = isDirecotry;
        public VfsNode(string name) => Name = name;
        public VfsNode(VfsNode parent, string name) : this(name) => Parent = parent;
        public VfsNode(VfsNode parent, string name, string absolutePath) : this(parent, name) => AbsolutePath = absolutePath;
        public VfsNode(VfsNode parent, string name, string absolutePath, string subPath) : this(parent, name, absolutePath) => SubPath = subPath;
        public VfsNode(VfsNode parent, string name, string absolutePath, string subPath, bool isDirectory) : this(parent, name, absolutePath, subPath) => IsDirectory = isDirectory;

        public bool IsDirectoryWritable() => IsDirectory && AbsolutePath != null;
        public bool IsRegularFile() => AbsolutePath != null && SubPath == null;
        public bool IsArchiveFile() => AbsolutePath != null && SubPath != null;

        public VfsNode CreateDirectory(string name) => CreateFile(name, null, null, true);
        public VfsNode CreateDirectory(string name, string absolutePath) => CreateFile(name, absolutePath, null, true);
        public VfsNode CreateFile(string name, string absolutePath) => CreateFile(name, absolutePath, null, false);
        public VfsNode CreateFile(string name, string absolutePath, string subPath, bool isDirectory)
        {
            if (!Children.TryGetValue(name, out var child))
                Children.Add(name, child = new VfsNode(this, name, absolutePath, subPath, isDirectory));
            else if (!child.IsDirectory)
                // A directory overrides a file, but a file cannot override a directory.
                Children[name] = child = new VfsNode(this, name, absolutePath, subPath, isDirectory);
            else if (child.IsDirectory && isDirectory && absolutePath != null &&  absolutePath != child.AbsolutePath)
                // A directory can be made writable, and a write path can override an old write path.
                child.SetDirectoryWritable(absolutePath);
            return child;
        }

        public VfsNode ChangeDirectory(string path, bool createIfMissing) => ChangeDirectory(path.Split('/'), createIfMissing);
        public VfsNode ChangeDirectory(IEnumerable<string> names, bool createIfMissing)
        {
            var node = this;
            foreach (var name in names)
            {
                switch (name)
                {
                    case "":
                    case ".": break;
                    case "..": node = node.Parent ?? node; break;
                    default: if (createIfMissing) node = node.CreateDirectory(name);
                             else node.Children.TryGetValue(name, out node);
                             break;
                }
                if (!node?.IsDirectory ?? true)
                    return null;
            }
            return node;
        }

        public bool DeleteChild(string name)
        {
            var success = Children.ContainsKey(name);
            if (success) Children.Remove(name);
            return success;
        }

        public VfsNode GetNode(string path)
        {
            VfsNode node = this;
            var names = path.Split('/');
            if (names.Length > 1)
                node = ChangeDirectory(names.Take(names.Length - 1), false);
            node?.Children.TryGetValue(names.Last(), out node);
            return node;
        }

        public string GetVfsPath()
        {
            var name = Name;// + (IsDirectory() ? "/" : "");
            var node = this;
            while ((node = node.Parent) != null)
                name = $"{node.Name}/{name}";
            return name;
        }

        public VfsNode SetDirectoryWritable(string absolutePath)
        {
            if (IsDirectory)
            {
                var writableNode = new VfsNode(Parent, Name, absolutePath, null, true);
                foreach (var key in Children.Keys)
                    writableNode.Children.Add(key, Children[key]);
                Parent.Children[Name] = writableNode;
                return writableNode;
            }
            return this;
        }

        public IEnumerable<string> GetDirectories() => Children.Where(kvp => kvp.Value.IsDirectory).Select(kvp => kvp.Value.GetVfsPath());
        public IEnumerable<string> GetFiles() => Children.Where(kvp => !kvp.Value.IsDirectory).Select(kvp => kvp.Value.GetVfsPath());
    }
}
