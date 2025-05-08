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
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;
using System.IO.Compression;
using System.ComponentModel;

namespace ORTS.Common
{
    /// <summary>
    /// Virtual File System. Can be <see cref="Initialize(string)"/>'d from a json config file
    /// or from a directory. Found subsequent archives are auto-mounted to their respective locations.
    /// <br><see cref="Path.Combine"/> like functions continue to work on virtual paths, as well as both the "\" and "/" separators.
    /// But <see cref="Path.GetFullPath"/> must never be used on them, because that adds an unwanted drive letter.</br>
    /// <br><see cref="File.Exists"/> becomes <see cref="Vfs.FileExists"/>, <see cref="Directory.Exists"/> becomes <see cref="Vfs.DirectoryExists"/>,
    /// <see cref="Directory.GetFiles(string, string)"/> becomes <see cref="Vfs.GetFiles"/>.</br>
    /// </summary>
    public static class Vfs
    {
        static VfsNode VfsRoot;
        static readonly ConcurrentDictionary<string, object> OpenArchives = new ConcurrentDictionary<string, object>();
        static readonly List<string> SupportedArchiveExtensions = new List<string> { ".zip", ".rar" };
        internal static readonly ConcurrentQueue<string> InitLog = new ConcurrentQueue<string>();

        ///////////////////////////////////////////////////////////////////////////////////////
        ///
        /// The configuration file is in the following format:
        /// 
        /*
{
    "vfsEntries": [
        {
            "source": "C:\\MSTS_Packages\\MSTS_1.1.zip\\Train Simulator\\",
            "mountPoint": "/MSTS/"
        },
        {
            "source": "C:/MSTS_Packages/DemoModel1.zip/Demo Model 1/",
            "mountPoint": "/MSTS/"
        },
        {
            "source": "C:/My Routes/USA85/",
            "mountPoint": "/MSTS/ROUTES/USA85/"
        }
    ]
}
        */
        ///
        /// Source     - may use "/" or "\\", but MountPoint may use "/" only as a directory separator. Recommendation: use forward slashes "/" everywhere for the sake of simplicity.
        ///            - if using backslash as a separator, it must be doubled, e.g. "C:\\My Routes\\USA85\\"
        ///            - a directory must end with "/" or "\\".
        ///            - an archive must not end with "/" or "\\", e.g. "C:\\TEMP\\MSTS1.2.zip"
        ///            - may refer to an archive subdirectory, e.g.: "C:/routes.zip/USA3/"
        ///            - may _not_ refer to a single non-archive file, neither to a non-archive file within an archive.
        ///            - is case-insensitive.
        /// MountPoint - must end with "/" to avoid the confusion.
        ///            - is case-insensitive, will be converted to all-uppercase internally.
        ///
        /// If enabled, subsequent archives are auto-mounted to their respective VFS locations, but nested archives are unsupported.
        ///
        /// For programmers:
        /// 
        /// System.IO.Path.*() functions can be used on virtual paths, but make sure that
        /// NormalizeVirtualPath() is used on them _afterwards_ in the processing pipeline somewhere,
        /// so the rest of the code doesn't need to normalize before calling the public functions in this class.
        /// The exception function that must never be used on virtual paths is the Path.GetFullPath(),
        /// because it adds an unwanted drive letter to the beginning of the resulted path.
        ///
        ///////////////////////////////////////////////////////////////////////////////////////
        ///
        /// Public interfaces
        /// 

        public const string MstsBasePath = "/MSTS/";
        public const string ExecutablePath = "/EXECUTABLE/";

        /// <summary>
        /// Log level 1 is to log the mounting information and all the warnings,
        /// 2 is to log mount-time file operations such as virtual overwrites too,
        /// 3 is to log all runtime file accesses as well.
        /// </summary>
        public static int LogLevel { get; set; } = 1;
        public static bool AutoMount { get; set; }
        public static bool IsInitialized => VfsRoot != null;

        /// <summary>
        /// Initialize a new Vfs.
        /// </summary>
        /// <param name="initPath">Can be a system directory or a vfs config file.</param>
        /// <param name="executablePath">The path to the executable, where the Contents folder is found.</param>
        /// <returns></returns>
        public static bool Initialize(string initPath, string executablePath = null)
        {
            VfsRoot = new VfsNode("", true);
            Cleanup();
            if (Directory.Exists(executablePath))
                MountDirectory(executablePath, ExecutablePath);
            return TryAttach(initPath, MstsBasePath);
        }

        /// <summary>
        /// Attach new hierarchy levels to the existing Vfs.
        /// </summary>
        /// <param name="initPath">Can be a system directory or a vfs config file.</param>
        /// <param name="basePath">The base hierarchy level the directory will be attached to, in case initPath is a directory.</param>
        /// <returns></returns>
        public static bool TryAttach(string initPath, string basePath = MstsBasePath)
        {
            Debug.Assert(VfsRoot != null, "VFS is uninitialized");
            if (File.Exists(initPath))
            {
                var success = true;
                var entries = Newtonsoft.Json.JsonConvert.DeserializeObject<VfsTableFile>(File.ReadAllText(initPath))?.VfsEntries;
                if (entries != null)
                    foreach (var entry in entries)
                        success &= Attach(entry.Source, entry.MountPoint);
                return success;
            }
            if (Directory.Exists(initPath))
                return Attach(initPath + "/", basePath);
            var message = $"VFS: Could not attach {initPath}, aborting.";
            Trace.TraceError(message);
            InitLog.Enqueue(message);
            return false;
        }

        public static StreamReader StreamReader(string vfsPath, bool detectEncodingFromByteOrderMarks)
        {
            switch (PrepareForRead(vfsPath))
            {
                case ZipArchiveEntry entry: return new StreamReader(entry.Open(), detectEncodingFromByteOrderMarks);
                case string path: return new StreamReader(path, detectEncodingFromByteOrderMarks);
                default: throw new FileNotFoundException($"VFS reading failed: {vfsPath}");
            }
            // Looks like the rest of the OR code expects to throw an exception at these places instead of just returning null.
        }

        public static StreamReader StreamReader(string vfsPath, Encoding encoding)
        {
            switch (PrepareForRead(vfsPath))
            {
                case ZipArchiveEntry entry: return new StreamReader(entry.Open(), encoding);
                case string path: return new StreamReader(path, encoding);
                default: throw new FileNotFoundException($"VFS reading failed: {vfsPath}");
            }
        }

        public static StreamReader OpenText(string vfsPath)
        {
            switch (PrepareForRead(vfsPath))
            {
                case ZipArchiveEntry entry: return new StreamReader(entry.Open(), Encoding.UTF8);
                case string path: return File.OpenText(path);
                default: throw new FileNotFoundException($"VFS reading failed: {vfsPath}");
            }
        }

        public static Stream OpenRead(string vfsPath)
        {
            switch (PrepareForRead(vfsPath))
            {
                case ZipArchiveEntry entry: return entry.Open();
                case string path: return File.OpenRead(path);
                default: throw new FileNotFoundException($"VFS reading failed: {vfsPath}");
            }
        }

        /// SharpCompress has the <see cref="Stream.Seek(long, SeekOrigin)"/> unimplemented,
        /// so for reading a wav or dds within a VFS archive it needs to be loaded into the memory first.
        public static Stream OpenReadWithSeek(string vfsPath)
        {
            var stream = OpenRead(vfsPath);
            if (!stream.CanSeek)
            {
                var newStream = new MemoryStream();
                stream.CopyTo(newStream);
                stream.Close();
                stream = newStream;
                stream.Position = 0;
            }
            return stream;
        }

        public static DateTime GetLastWriteTime(string vfsPath)
        {
            switch (PrepareForRead(vfsPath))
            {
                case ZipArchiveEntry entry: return entry.LastWriteTime.DateTime;
                case string path: return File.GetLastWriteTime(path);
                default: throw new FileNotFoundException($"VFS reading failed: {vfsPath}");
            }
        }

        public static Stream OpenCreate(string vfsPath)
        {
            switch (PrepareForWrite(vfsPath))
            {
                case Stream stream: return stream;
                case string path: return new FileStream(path, FileMode.Create);
                default: throw new DirectoryNotFoundException($"VFS writing failed: {vfsPath}");
            }
        }

        public static void FileDelete(string vfsPath)
        {
            if (FileExists(vfsPath))
            {
                var node = VfsRoot.GetNode(NormalizeVirtualPath(vfsPath));
                if (node != null && node != VfsRoot && node.IsRegularFile())
                {
                    node.DeleteNode();
                    if (File.Exists(node.AbsolutePath))
                        File.Delete(node.AbsolutePath);
                    if (LogLevel > 2)
                        Trace.TraceInformation($"VFS deleting archive node: {node.GetVerbosePath()} => {vfsPath}");
                    return;
                }
            }
            throw new FileNotFoundException($"VFS deleting failed: {vfsPath}");
        }

        public static string[] GetDirectories(string vfsPath, SearchOption searchOption = SearchOption.TopDirectoryOnly)
            => GetDirectoriesBase(vfsPath, searchOption) ?? Array.Empty<string>();
        public static string[] GetFiles(string vfsPath, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly)
            => GetFilesBase(vfsPath, searchPattern, searchOption) ?? Array.Empty<string>();

        public static bool DirectoryExists(string vfsPath)
        {
            vfsPath = NormalizeVirtualPath(vfsPath);
            return vfsPath.StartsWith("/") && VfsRoot.ChangeDirectory(vfsPath, false) != null;
        }

        public static bool FileExists(string vfsPath)
        {
            vfsPath = NormalizeVirtualPath(vfsPath); // To remove duplicate //-s and \\-s in advance, so that Path.Get... would work.
            var directoryName = vfsPath == "" ? "" : NormalizeVirtualPath(Path.GetDirectoryName(vfsPath));
            var fileName = Path.GetFileName(vfsPath);
            return directoryName.StartsWith("/") && (!VfsRoot.ChangeDirectory(directoryName, false)?.GetNode(fileName)?.IsDirectory ?? false);
        }

        public static void Log()
        {
            while (InitLog.TryDequeue(out var message))
                Console.WriteLine(message);
        }

        public static void DebugDump()
        {
            Trace.TraceInformation($"VFS: Start of hierarchy dump");
            var stack = new Stack<(string, VfsNode)>();
            stack.Push(("/", VfsRoot));
            while (stack.Count > 0)
            {
                var (dirpath, vfsNode) = stack.Pop();
                Trace.TraceInformation($"VFS: {dirpath}" + (vfsNode.IsDirectoryWritable() ? $" <= {vfsNode.AbsolutePath}" : ""));
                foreach (var subdir in vfsNode.GetEntries(SearchOption.TopDirectoryOnly, false))
                    stack.Push((subdir, vfsNode.ChangeDirectory(NormalizeVirtualPath(Path.GetFileName(subdir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))), false)));
                foreach (var file in vfsNode.GetEntries(SearchOption.TopDirectoryOnly, true))
                {
                    var node = vfsNode.GetNode(Path.GetFileName(file));
                    Trace.TraceInformation($"VFS: {file} <= " + node.GetVerbosePath());
                }
            }
            Trace.TraceInformation($"VFS: End of hierarchy dump");
        }

        public static void Cleanup()
        {
            foreach (var key in OpenArchives.Keys)
            {
                OpenArchives.TryRemove(key, out var archive); 
                switch (archive)
                {
                    case ZipArchive zipArchive: zipArchive.Dispose(); break;
                }
            }
            OpenArchives.Clear();
        }

        /// <summary>
        /// Don't use this function, only for reading from a file by a native windows dll, that cannot use streams,
        /// e.g. the .ini reading method called from the ScriptedTrainControlSystem.LoadParameter method.
        /// This funciton will be removed after the migration to NET 6.
        /// </summary>
        public static string GetSystemPathForNativeWindowsMethodsIfRealFile_DontUse(string vfsPath)
        {
            switch (PrepareForRead(vfsPath))
            {
                case ZipArchiveEntry _: return null;
                case string path: return path;
                default: throw new FileNotFoundException($"VFS reading failed: {vfsPath}");
            }
        }
        ///
        /// End of interfaces.
        /// 
        ///////////////////////////////////////////////////////////////////////////////////////

        static bool Attach(string source, string mountpoint)
        {
            string message;

            // Mountpoint validation:
            if (!mountpoint.EndsWith("/"))
            {
                message = $"VFS mount: Mount point doesn't end with slash (/), skipping: {source} => {mountpoint}";
                Trace.TraceWarning(message);
                InitLog.Enqueue(message);
                return false;
            }
            mountpoint = NormalizeVirtualPath(mountpoint);

            //Source path validation:
            var sourcePath = NormalizeSystemPath(source);
            var sourcePieces = sourcePath.Split('/');
            var isDirectory = sourcePieces.Last() == "";
            if (sourcePieces.Length > 0 && isDirectory && Directory.Exists(sourcePath))
            {
                // A bare mountable directory must end with "/"
                MountDirectory(sourcePath, mountpoint);
                return true;
            }
            else if (sourcePieces.Length > 1 && !isDirectory && File.Exists(sourcePath))
            {
                // An archive file reference should not end with "/"
                if (IsArchiveSupported(sourcePath))
                {
                    MountArchive(sourcePath, null, mountpoint);
                    return true;
                }
                message = $"VFS mount: Source archive format is not supported, skipping: {source} => {mountpoint}";
                Trace.TraceWarning(message);
                InitLog.Enqueue(message);
                return false;
            }
            else if (sourcePieces.Length > 0)
            {
                if (!isDirectory)
                {
                    message = $"VFS mount: Source path is not an archive file and doesn't end with slash (/) or backslash (\\), skipping: {source} => {mountpoint}";
                    Trace.TraceWarning(message);
                    InitLog.Enqueue(message);
                    return false;
                }
                // Trying to find the base archive in a directory-within-archive case.
                // Start from 1, ignoring the drive letter at position 0.
                for (var i = 1; i < sourcePieces.Length - 1; i++)
                {
                    var archivePath = string.Join("/", sourcePieces, 0, i + 1);
                    if (File.Exists(archivePath))
                    {
                        if (IsArchiveSupported(archivePath))
                        {
                            var subPath = string.Join("/", sourcePieces, i + 1, sourcePieces.Length - 1 - i);
                            MountArchive(archivePath, subPath, mountpoint);
                            return true;
                        }
                        else
                        {
                            message = $"VFS mount: Source archive format is not supported, skipping: {source} => {mountpoint}";
                            Trace.TraceWarning(message);
                            InitLog.Enqueue(message);
                            return false;
                        }
                    }
                    if (!Directory.Exists(archivePath) || i == sourcePieces.Length - 2)
                    {
                        // During the search we diverged already to a non-existent directory, we can safely abort.
                        // Or we reached the last searchable part, and still not found the base archive.
                        message = $"VFS mount: Source file not found, skipping: {source} => {mountpoint}";
                        Trace.TraceWarning(message);
                        InitLog.Enqueue(message);
                        return false;
                    }
                }
            }
            message = $"VFS mount: Cannot parse configuration line, skipping: {source} => {mountpoint}";
            Trace.TraceWarning(message);
            InitLog.Enqueue(message);
            return false;
        }

        static void MountDirectory(string directory, string mountpoint)
        {
            Debug.Assert(VfsRoot != null, "VFS is uninitialized");

            var message = $"VFS mounting directory: {directory} => {mountpoint}";
            Trace.TraceInformation(message);
            InitLog.Enqueue(message);

            var mountNode = VfsRoot.ChangeDirectory(mountpoint, true);
            mountNode.SetDirectoryWritable(directory);

            var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var relativePath = file.Substring(directory.Length);
                var vfsNode = mountNode.ChangeDirectory(NormalizeVirtualPath(Path.GetDirectoryName(relativePath)), true);
                if (!vfsNode.IsDirectoryWritable())
                    vfsNode.SetDirectoryWritable(Path.GetDirectoryName(file));

                if (IsArchiveSupported(file))
                {
                    if (!AutoMount)
                    {
                        message = $"VFS skipped auto-mounting archive by user settings: {file}";
                        Trace.TraceInformation(message);
                        InitLog.Enqueue(message);
                    }
                    else
                        MountArchive(file, null, vfsNode.GetVfsPath());
                }
                else
                    vfsNode.CreateFile(NormalizeVirtualPath(Path.GetFileName(relativePath)), file);
            }

            // This is for enumerating the remaining empty directories only.
            // May be necessary only for the *.or-binpat like writes, otherwise it could be omitted.
            var dirs = Directory.GetDirectories(directory, "*", SearchOption.AllDirectories);
            foreach (var dir in dirs)
            {
                var relativePath = dir.Substring(directory.Length);
                var vfsNode = mountNode.ChangeDirectory(NormalizeVirtualPath(relativePath), true);
                if (!vfsNode.IsDirectoryWritable())
                    vfsNode.SetDirectoryWritable(dir);
            }
        }

        static void MountArchive(string archivePath, string subPath, string mountpoint)
        {
            Debug.Assert(VfsRoot != null, "VFS is uninitialized");

            var message = $"VFS mounting archive: [{archivePath}]/{subPath ?? ""} => {mountpoint}";
            if (!message.EndsWith("/"))
                message += "/"; // This is to keep the consistency of directories ending with '/'.
            Trace.TraceInformation(message);
            InitLog.Enqueue(message);
            var mountNode = VfsRoot.ChangeDirectory(mountpoint, true);
            try
            {
                if (Path.GetExtension(archivePath).ToLowerInvariant() == ".zip")
                {
                    using (var archive = new ZipArchive(new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
                        foreach (var entry in archive.Entries.Where(entry => !string.IsNullOrEmpty(entry.Name) && !entry.FullName.EndsWith("/") && !entry.FullName.EndsWith(@"\"))) // && (entry.ExternalAttributes & (int)FileAttributes.Directory) == 0))
                            createVirtualArchiveFile(entry.FullName);
                }
            }
            catch
            {
                message = $"VFS mount: Could not open archive {archivePath}, skipping {subPath ?? string.Empty} => {mountpoint}";
                Trace.TraceWarning(message);
                InitLog.Enqueue(message);
            }

            void createVirtualArchiveFile(string fullName)
            {
                var normalKey = NormalizeVirtualPath(fullName);
                var subPathIsDirectory = subPath?.EndsWith("/") ?? false;
                if (subPath == null || subPathIsDirectory && normalKey.StartsWith(subPath, StringComparison.OrdinalIgnoreCase))
                {
                    var relativePath = normalKey.Substring(subPath?.Length ?? 0);
                    mountNode.ChangeDirectory(NormalizeVirtualPath(Path.GetDirectoryName(relativePath)), true)
                        .CreateFile(Path.GetFileName(relativePath), archivePath, fullName, false);
                }
            }
        }

        static string[] GetDirectoriesBase(string vfsPath, SearchOption searchOption) => VfsRoot.ChangeDirectory(NormalizeVirtualPath(vfsPath), false)?.GetEntries(searchOption, false)?.ToArray();
        static string[] GetFilesBase(string vfsPath, string searchPattern, SearchOption searchOption) => VfsRoot.ChangeDirectory(NormalizeVirtualPath(vfsPath), false)?.GetEntries(searchOption, true)
                .Where(f => Regex.IsMatch(f.Split('/').Last(), NormalizeVirtualPath(searchPattern).Replace(".", @"\.").Replace("*", ".*").Replace("-", @"\-")))?.ToArray();

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
                    if (Path.GetExtension(foundNode.AbsolutePath.ToLowerInvariant()) == ".zip")
                        archive = new ZipArchive(File.OpenRead(foundNode.AbsolutePath), ZipArchiveMode.Read);

                    OpenArchives.TryAdd(archiveKey, archive);
                    if (LogLevel > 2)
                        Trace.TraceInformation($"VFS opening archive file for thread {archiveKey}");
                }

                object archiveEntry = null;
                string fullName = "";
                switch (archive)
                {
                    case ZipArchive zipArchive:
                        archiveEntry = zipArchive.Entries.Where(entry => entry.FullName == foundNode.SubPath).FirstOrDefault();
                        fullName = ((ZipArchiveEntry)archiveEntry).FullName;
                        break;
                }
                if (archiveEntry != null)
                {
                    if (LogLevel > 2)
                        Trace.TraceInformation($"VFS reading archive node: [{foundNode.AbsolutePath}]/{fullName} => {vfsPath}");
                    return archiveEntry;
                }
            }
            else if (foundNode != null && foundNode.IsRegularFile() && File.Exists(foundNode.AbsolutePath))
            {
                if (LogLevel > 2)
                    Trace.TraceInformation($"VFS reading system file: {foundNode.AbsolutePath} => {vfsPath}");
                return foundNode.AbsolutePath;
            }
            if (LogLevel > 2)
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
                if (LogLevel > 2)
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
                        if (LogLevel > 2)
                            Trace.TraceInformation($"VFS writing system file: {fullName} => {vfsPath}");
                        return stream;
                    }
                    catch
                    {
                        stream.Close();
                    }
                }
            }
            if (LogLevel > 2)
                Trace.TraceInformation($"VFS writing failed: null => {vfsPath}");
            return null;
        }

        static bool IsArchiveSupported(string filename) => SupportedArchiveExtensions.Contains(Path.GetExtension(filename).ToLowerInvariant());

        static string NormalizeSystemPath(string path) => Path.GetFullPath(new Uri(path).LocalPath).Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/').Trim('"');
        static string NormalizeVirtualPath(string path)
        {
            path = path?.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/').Trim('"').ToUpperInvariant() ?? "";
            while (path.Contains("//"))
                path = path.Replace("//", "/");
            // Need to do something similar to Path.GetFullPath() with flattening the dir/.. pairs.
            // They cause problems e.g. at Scripts/../etc. sequences, where browsing through imaginary directories.
            return string.Join("/", Flatten(path.Split('/').ToList()));
        }

        static List<string> Flatten(List<string> names)
        {
            for (var i = 0; i < names.Count; i++)
            {
                switch (names[i])
                {
                    case ".": names.RemoveAt(i); return Flatten(names);
                    case "..":
                        if (i == 0) names.RemoveAt(0);
                        else names.RemoveRange(i - 1, 2);
                        return Flatten(names);
                    default: continue;
                }
            }
            return names;
        }
    }

    public class VfsTableFile
    {
        public VfsTableEntry[] VfsEntries { get; set; }
    }

    public class VfsTableEntry
    {
        [DefaultValue("")]
        public string Source { get; set; }
        [DefaultValue("")]
        public string MountPoint { get; set; }
    }

    internal class VfsNode
    {
        readonly ConcurrentDictionary<string, VfsNode> Children = new ConcurrentDictionary<string, VfsNode>();
        readonly VfsNode Parent;

        /// <summary>
        /// The node's own name without the path.
        /// </summary>
        readonly string Name;

        /// <summary>
        /// Either the real operating system path of the node's referenced file or directory,
        /// or the path to the archive containing the node's referenced file.
        /// </summary>
        public string AbsolutePath { get; private set; }

        /// <summary>
        /// In case the node's referenced file is inside of an archive, then the in-archive part of the path, else null.
        /// </summary>
        public string SubPath { get; }
        
        public bool IsDirectory { get; }

        public VfsNode(string name, bool isDirecotry) : this(name) => IsDirectory = isDirecotry;
        public VfsNode(string name) => Name = name;
        public VfsNode(VfsNode parent, string name) : this(name) => Parent = parent;
        public VfsNode(VfsNode parent, string name, string absolutePath) : this(parent, name) => AbsolutePath = absolutePath;
        public VfsNode(VfsNode parent, string name, string absolutePath, string subPath) : this(parent, name, absolutePath) => SubPath = subPath;
        public VfsNode(VfsNode parent, string name, string absolutePath, string subPath, bool isDirectory) : this(parent, name, absolutePath, subPath) => IsDirectory = isDirectory;

        public bool IsDirectoryWritable() => IsDirectory && AbsolutePath != null;
        public bool IsRegularFile() => !IsDirectory && AbsolutePath != null && SubPath == null;
        public bool IsArchiveFile() => !IsDirectory && AbsolutePath != null && SubPath != null;

        public VfsNode CreateDirectory(string name) => CreateFile(name, null, null, true);
        public VfsNode CreateDirectory(string name, string absolutePath) => CreateFile(name, absolutePath, null, true);
        public VfsNode CreateFile(string name, string absolutePath) => CreateFile(name, absolutePath, null, false);
        public VfsNode CreateFile(string name, string absolutePath, string subPath, bool isDirectory)
        {
            if (!Children.TryGetValue(name, out var child))
                Children.TryAdd(name, child = new VfsNode(this, name, absolutePath, subPath, isDirectory));
            else if (!child.IsDirectory)
            {
                // A directory overrides a file, but a file cannot override a directory.
                child = new VfsNode(this, name, absolutePath, subPath, isDirectory);
                if (Vfs.LogLevel == 2)
                {
                    var message = $"VFS virtual overwrite: {Children[name].GetVerbosePath()} with {child.GetVerbosePath()} => {child.GetVfsPath()}";
                    Trace.TraceInformation(message);
                    Vfs.InitLog.Enqueue(message);
                }
                Children[name] = child;
            }
            else if (child.IsDirectory && isDirectory && absolutePath != null && absolutePath != child.AbsolutePath)
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

        public bool DeleteNode() => Parent.DeleteChild(Name);
        public bool DeleteChild(string name) => Children.TryRemove(name, out _);

        public VfsNode GetNode(string path)
        {
            VfsNode node = this;
            var names = path.Split('/');
            if (names.Length > 1)
                node = ChangeDirectory(names.Take(names.Length - 1), false);
            node?.Children.TryGetValue(names.Last(), out node);
            return node;
        }

        public string GetVerbosePath() => IsArchiveFile() ? $"[{AbsolutePath}]/{SubPath}" : AbsolutePath ?? "null";

        public string GetVfsPath()
        {
            // Not adding a trailing '/' to the directory paths, like: var name = Name + (IsDirectory ? "/" : "");
            // This is to keep compatibility with Path.GetFileName(directoryPath) statements.
            var name = Name;
            var node = this;
            while ((node = node.Parent) != null)
                name = $"{node.Name}/{name}";
            return name;
        }

        public void SetDirectoryWritable(string absolutePath) { if (IsDirectory) AbsolutePath = absolutePath; }

        public IEnumerable<string> GetEntries(SearchOption searchOption, bool files)
        {
            var result = Enumerable.Empty<string>()
                .Concat(Children.Where(kvp => files ^ kvp.Value.IsDirectory).Select(kvp => kvp.Value.GetVfsPath()));
            if (searchOption == SearchOption.AllDirectories)
                foreach (var subdir in Children.Where(kvp => kvp.Value.IsDirectory).Select(kvp => kvp.Value))
                    result = result.Concat(subdir.GetEntries(searchOption, files));
            return result;
        }
    }
}
