// COPYRIGHT 2015 by the Open Rails project.
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

using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Xna.Framework.Graphics;

namespace ORTS.Common
{
    public static class SystemInfo
    {
        public static void WriteSystemDetails(TextWriter output)
        {
            output.WriteLine("Date/time  = {0} ({1:u})", DateTime.Now, DateTime.UtcNow);
            WriteEnvironment(output);
            WriteAvailableRuntimes(output);
            output.WriteLine("Runtime    = {0} ({1}bit)", Environment.Version, IntPtr.Size * 8);
            WriteGraphicsAdapter(output);
        }

        static void WriteEnvironment(TextWriter output)
        {
            var buffer = new NativeStructs.MemoryStatusExtended { Size = 64 };
            NativeMethods.GlobalMemoryStatusEx(buffer);
            try
            {
                foreach (ManagementObject bios in new ManagementClass("Win32_BIOS").GetInstances())
                {
                    output.WriteLine("BIOS       = {0} ({1})", (string)bios["Description"], (string)bios["Manufacturer"]);
                }
            }
            catch (Exception error)
            {
                Trace.WriteLine(error);
            }
            try
            {
                foreach (ManagementObject processor in new ManagementClass("Win32_Processor").GetInstances())
                {
                    output.Write("Processor  = {0} ({2} threads, {1} cores, {3:F1} GHz)", (string)processor["Name"], (uint)processor["NumberOfCores"], (uint)processor["NumberOfLogicalProcessors"], (float)(uint)processor["MaxClockSpeed"] / 1000);
                    foreach (ManagementObject cpuCache in processor.GetRelated("Win32_CacheMemory"))
                    {
                        output.Write(" ({0} {1:F0} KB)", (string)cpuCache["Purpose"], (float)(uint)cpuCache["InstalledSize"]);
                    }
                    output.WriteLine();
                }
            }
            catch (Exception error)
            {
                Trace.WriteLine(error);
            }
            output.WriteLine("Memory     = {0:F1} GB", (float)buffer.TotalPhysical / 1024 / 1024 / 1024);
            try
            {
                foreach (ManagementObject display in new ManagementClass("Win32_VideoController").GetInstances())
                {
                    // ? used as display["AdapterRAM"] may be null on a virtual machine (e.g. VMWare)
                    output.WriteLine("Video      = {0} ({1:F1} GB RAM){2}", (string)display["Description"], (float?)(uint?)display["AdapterRAM"] / 1024 / 1024 / 1024, GetPnPDeviceDrivers(display));
                }
            }
            catch (Exception error)
            {
                Trace.WriteLine(error);
            }
            try
            {
                foreach (var screen in Screen.AllScreens)
                {
                    output.WriteLine("Display    = {0} ({3} x {4}, {5}-bit{6}, {1} x {2})", screen.DeviceName, screen.Bounds.X, screen.Bounds.Y, screen.Bounds.Width, screen.Bounds.Height, screen.BitsPerPixel, screen.Primary ? ", primary" : "");
                }
            }
            catch (Exception error)
            {
                Trace.WriteLine(error);
            }
            try
            {
                foreach (ManagementObject sound in new ManagementClass("Win32_SoundDevice").GetInstances())
                {
                    Console.WriteLine("Sound      = {0}{1}", (string)sound["Description"], GetPnPDeviceDrivers(sound));
                }
            }
            catch (Exception error)
            {
                Trace.WriteLine(error);
            }
            try
            {
                foreach (ManagementObject disk in new ManagementClass("Win32_LogicalDisk").GetInstances())
                {
                    output.Write("Disk       = {0} ({1}, {2}", (string)disk["Name"], (string)disk["Description"], (string)disk["FileSystem"]);
                    if (disk["Size"] != null && disk["FreeSpace"] != null)
                        output.WriteLine(", {0:F1} GB, {1:F1} GB free)", (float)(ulong)disk["Size"] / 1024 / 1024 / 1024, (float)(ulong)disk["FreeSpace"] / 1024 / 1024 / 1024);
                    else
                        output.WriteLine(")");
                }
            }
            catch (Exception error)
            {
                Trace.WriteLine(error);
            }
            try
            {
                foreach (ManagementObject os in new ManagementClass("Win32_OperatingSystem").GetInstances())
                {
                    output.WriteLine("OS         = {0} {1} ({2})", (string)os["Caption"], (string)os["OSArchitecture"], (string)os["Version"]);
                }
            }
            catch (Exception error)
            {
                Trace.WriteLine(error);
            }
        }

        static string GetPnPDeviceDrivers(ManagementObject device)
        {
            var output = new StringBuilder();
            foreach (ManagementObject pnpDevice in device.GetRelated("Win32_PnPEntity"))
            {
                foreach (ManagementObject dataFile in pnpDevice.GetRelated("CIM_DataFile"))
                {
                    output.AppendFormat(" ({0} {1})", (string)dataFile["FileName"], (string)dataFile["Version"]);
                }
            }
            return output.ToString();
        }

        static void WriteAvailableRuntimes(TextWriter output)
        {
            output.Write("Runtimes   =");
            try
            {
                // This remote access is necessary to ensure we get the correct bitness view of the registry.
                using (var frameworksKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, "").OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP", false))
                {
                    foreach (var versionKeyName in frameworksKey.GetSubKeyNames())
                    {
                        if (!versionKeyName.StartsWith("v"))
                            continue;

                        using (var versionKey = frameworksKey.OpenSubKey(versionKeyName))
                        {
                            var fullVersion = WriteInstalledRuntimes(output, versionKeyName, versionKey);
                            if (fullVersion != "")
                                continue;

                            foreach (var skuKeyName in versionKey.GetSubKeyNames())
                            {
                                using (var skuKey = versionKey.OpenSubKey(skuKeyName))
                                {
                                    WriteInstalledRuntimes(output, versionKeyName + " " + skuKeyName, skuKey);
                                }
                            }
                        }
                    }
                }
                output.WriteLine();
            }
            catch (Exception error)
            {
                Trace.WriteLine(error);
            }
        }

        static string WriteInstalledRuntimes(TextWriter output, string versionKeyName, RegistryKey versionKey)
        {
            var installed = SafeReadKey(versionKey, "Install", -1);
            var fullVersion = SafeReadKey(versionKey, "Version", "");
            var servicePack = SafeReadKey(versionKey, "SP", -1);

            if (installed == 1 && servicePack != -1)
            {
                output.Write(" {0} SP{2} ", versionKeyName.Substring(1), fullVersion, servicePack);
            }
            else if (installed == 1)
            {
                output.Write(" {0} ", versionKeyName.Substring(1), fullVersion);
            }
            return fullVersion;
        }

        static void WriteGraphicsAdapter(TextWriter output)
        {
            try {
                foreach (var adapter in GraphicsAdapter.Adapters)
                {
                    try
                    {
                        output.WriteLine("{0} = {1}", adapter.DeviceName, adapter.Description);
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception error)
            {
                output.WriteLine(error);
            }
        }

        static T SafeReadKey<T>(RegistryKey key, string name, T defaultValue)
        {
            try
            {
                return (T)key.GetValue(name, defaultValue);
            }
            catch
            {
                return defaultValue;
            }
        }

        static class NativeStructs
        {
            [StructLayout(LayoutKind.Sequential, Size = 64)]
            public class MemoryStatusExtended
            {
                public uint Size;
                public uint MemoryLoad;
                public ulong TotalPhysical;
                public ulong AvailablePhysical;
                public ulong TotalPageFile;
                public ulong AvailablePageFile;
                public ulong TotalVirtual;
                public ulong AvailableVirtual;
                public ulong AvailableExtendedVirtual;
            }
        }

        static class NativeMethods
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool GlobalMemoryStatusEx([In, Out] NativeStructs.MemoryStatusExtended buffer);
        }
    }
}
