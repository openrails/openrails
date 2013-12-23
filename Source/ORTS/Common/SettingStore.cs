// COPYRIGHT 2013 by the Open Rails project.
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ORTS.Common
{
	/// <summary>
	/// Base class for all means of persisting settings from the user/game.
	/// </summary>
	public abstract class SettingStore
	{
		protected readonly string Section;

		protected SettingStore(string section)
		{
			Section = section;
		}

		public abstract object GetUserValue(string name);

		public abstract void SetUserValue(string name, string value);

		public abstract void SetUserValue(string name, int value);

		public abstract void SetUserValue(string name, bool value);

		public abstract void SetUserValue(string name, string[] value);

		public abstract void SetUserValue(string name, int[] value);

		public abstract void DeleteUserValue(string name);

		public static SettingStore GetSettingStore(string filePath, string registryKey, string section)
		{
			if (!String.IsNullOrEmpty(filePath) && File.Exists(filePath))
				return new SettingStoreLocalIni(filePath, section);
			if (!String.IsNullOrEmpty(registryKey))
				return new SettingStoreRegistry(registryKey, section);
			throw new ArgumentException("Neither 'filePath' nor 'registryKey' arguments are valid.");
		}
	}

	/// <summary>
	/// Registry implementation of <c ref="SettingStore"/>.
	/// </summary>
	public sealed class SettingStoreRegistry : SettingStore
	{
		readonly string RegistryKey;
		readonly RegistryKey Key;

		internal SettingStoreRegistry(string registryKey, string section)
			: base(section)
		{
			RegistryKey = String.IsNullOrEmpty(section) ? registryKey : registryKey + @"\" + section;
			Key = Registry.CurrentUser.CreateSubKey(RegistryKey);
		}

		public override object GetUserValue(string name)
		{
			return Key.GetValue(name);
		}

		public override void SetUserValue(string name, string value)
		{
			Key.SetValue(name, value, RegistryValueKind.String);
		}

		public override void SetUserValue(string name, int value)
		{
			Key.SetValue(name, value, RegistryValueKind.DWord);
		}

		public override void SetUserValue(string name, bool value)
		{
			Key.SetValue(name, value ? 1 : 0, RegistryValueKind.DWord);
		}

		public override void SetUserValue(string name, string[] value)
		{
			Key.SetValue(name, value, RegistryValueKind.MultiString);
		}

		public override void SetUserValue(string name, int[] value)
		{
			Key.SetValue(name, String.Join(",", ((int[])value).Select(v => v.ToString()).ToArray()), RegistryValueKind.String);
		}

		public override void DeleteUserValue(string name)
		{
			Key.DeleteValue(name, false);
		}
	}

	/// <summary>
	/// INI file implementation of <c ref="SettingStore"/>.
	/// </summary>
	public sealed class SettingStoreLocalIni : SettingStore
	{
		const string DefaultSection = "ORTS";

		readonly string FilePath;

		internal SettingStoreLocalIni(string filePath, string section)
			: base(String.IsNullOrEmpty(section) ? DefaultSection : section)
		{
			FilePath = filePath;
		}

		public override object GetUserValue(string name)
		{
			var buffer = new String('\0', 256);
			var length = NativeMethods.GetPrivateProfileString(Section, name, null, buffer, buffer.Length, FilePath);
			if (length == 0)
				return null;
			buffer = buffer.Substring(0, length);
			var value = buffer.Split(':');
			if (value.Length < 2)
			{
				Trace.TraceWarning("Setting {0} contains invalid value {1}", name, buffer);
				return null;
			}
			var valueFull = String.Join(":", value.Skip(1).ToArray());
			switch (value[0])
			{
				case "string":
					return valueFull;
				case "int":
					return int.Parse(valueFull, CultureInfo.InvariantCulture);
				case "bool":
					return valueFull.Equals("true", StringComparison.InvariantCultureIgnoreCase);
				case "string[]":
					return valueFull.Split(',');
				case "int[]":
					return valueFull.Split(',').Select(v => int.Parse(v, CultureInfo.InvariantCulture)).ToArray();
				default:
					Trace.TraceWarning("Setting {0} contains invalid value {1}", name, buffer);
					return null;
			}
		}

		public override void SetUserValue(string name, string value)
		{
			NativeMethods.WritePrivateProfileString(Section, name, "string:" + value, FilePath);
		}

		public override void SetUserValue(string name, int value)
		{
			NativeMethods.WritePrivateProfileString(Section, name, "int:" + value.ToString(CultureInfo.InvariantCulture), FilePath);
		}

		public override void SetUserValue(string name, bool value)
		{
			NativeMethods.WritePrivateProfileString(Section, name, "bool:" + (value ? "true" : "false"), FilePath);
		}

		public override void SetUserValue(string name, string[] value)
		{
			NativeMethods.WritePrivateProfileString(Section, name, "string[]:" + String.Join(",", value), FilePath);
		}

		public override void SetUserValue(string name, int[] value)
		{
			NativeMethods.WritePrivateProfileString(Section, name, "int[]:" + String.Join(",", ((int[])value).Select(v => v.ToString(CultureInfo.InvariantCulture)).ToArray()), FilePath);
		}

		public override void DeleteUserValue(string name)
		{
			NativeMethods.WritePrivateProfileString(Section, name, null, FilePath);
		}
	}

	internal class NativeMethods
	{
		[DllImport("KERNEL32.DLL", EntryPoint = "GetPrivateProfileStringW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern int GetPrivateProfileString(string sectionName, string keyName, string defaultValue, string value, int size, string fileName);

		[DllImport("KERNEL32.DLL", EntryPoint = "WritePrivateProfileStringW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern int WritePrivateProfileString(string sectionName, string keyName, string value, string fileName);
	}
}
