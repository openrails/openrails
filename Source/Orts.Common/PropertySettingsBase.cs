// COPYRIGHT 2009 - 2024 by the Open Rails project.
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
using System.IO;
using System.Linq;
using System.Reflection;

namespace ORTS.Common
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DefaultAttribute : Attribute
    {
        public readonly object Value;
        public DefaultAttribute(object value)
        {
            Value = value;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DoNotSaveAttribute : Attribute
    {
    }

    public abstract class PropertySettingsBase : SettingsBase
    {
        public PropertySettingsBase(SettingsStore settingsStore)
            : base(settingsStore)
        {
        }

        /// <summary>
        /// Get a saving property from this instance by name.
        /// </summary>
        public SavingProperty<T> GetSavingProperty<T>(string name)
        {
            var property = GetProperty(name);
            if (property == null)
                return null;
            else
                return new SavingProperty<T>(this, property, AllowUserSettings);
        }

        public override object GetDefaultValue(string name)
        {
            var property = GetType().GetProperty(name);

            if (property.GetCustomAttributes(typeof(DefaultAttribute), false).Length > 0)
                return (property.GetCustomAttributes(typeof(DefaultAttribute), false)[0] as DefaultAttribute).Value;

            throw new InvalidDataException(String.Format("UserSetting {0} has no default value.", property.Name));
        }

        public PropertyInfo GetProperty(string name)
        {
            return GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        }

        PropertyInfo[] GetProperties()
        {
            // Skip any properties that implement SettingsBase; they are child setting classes and not individual user settings.
            return GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy).Where(pi => !typeof(SettingsBase).IsAssignableFrom(pi.PropertyType)).ToArray();
        }

        protected override object GetValue(string name)
        {
            return GetProperty(name).GetValue(this, null);
        }

        protected override void SetValue(string name, object value)
        {
            GetProperty(name).SetValue(this, value, null);
        }

        protected override void Load(Dictionary<string, string> optionsDictionary)
        {
            foreach (var property in GetProperties())
                Load(optionsDictionary, property.Name, property.PropertyType);
        }

        public override void Save()
        {
            foreach (var property in GetProperties())
            {
                if (property.GetCustomAttributes(typeof(DoNotSaveAttribute), false).Length == 0)
                {
                    Console.WriteLine(property.Name, property.PropertyType);
                    Save(property.Name, property.PropertyType);
                }
            }
        }

        public override void Save(string name)
        {
            var property = GetProperty(name);
            if (property.GetCustomAttributes(typeof(DoNotSaveAttribute), false).Length == 0)
                Save(property.Name, property.PropertyType);
        }

        public override void Reset()
        {
            foreach (var property in GetProperties())
                Reset(property.Name);
        }

        public void Log()
        {
            foreach (var property in GetProperties().OrderBy(p => p.Name))
            {
                var value = property.GetValue(this, null);
                var source = Sources[property.Name] == Source.CommandLine ? "(command-line)" : Sources[property.Name] == Source.User ? "(user set)" : "";
                if (property.PropertyType == typeof(string[]))
                    Console.WriteLine("{0,-30} = {2,-14} {1}", property.Name, String.Join(", ", ((string[])value).Select(v => v.ToString()).ToArray()), source);
                else if (property.PropertyType == typeof(int[]))
                    Console.WriteLine("{0,-30} = {2,-14} {1}", property.Name, String.Join(", ", ((int[])value).Select(v => v.ToString()).ToArray()), source);
                else
                    Console.WriteLine("{0,-30} = {2,-14} {1}", property.Name, value, source);
            }
        }
    }

    /// <summary>
    /// A wrapper for a PropertySettingsBase property that saves any new values immediately.
    /// </summary>
    /// <typeparam name="T">Cast values to this type.</typeparam>
    public class SavingProperty<T>
    {
        private readonly PropertySettingsBase Settings;
        private readonly PropertyInfo Property;
        private readonly bool DoSave;

        internal SavingProperty(PropertySettingsBase settings, PropertyInfo property, bool allowSave = true)
        {
            Settings = settings;
            Property = property;
            DoSave = allowSave;
        }

        /// <summary>
        /// Get or set the current value of this property.
        /// </summary>
        public T Value
        {
            get => GetValue();
            set => SetValue(value);
        }

        /// <summary>
        /// Get the current value of this property.
        /// </summary>
        public T GetValue()
            => Property.GetValue(Settings) is T cast ? cast : default;

        /// <summary>
        /// Set the current value of this property.
        /// </summary>
        public void SetValue(T value)
        {
            if (!GetValue().Equals(value))
            {
                Property.SetValue(Settings, value);
                if (DoSave)
                    Settings.Save(Property.Name);
            }
        }
    }
}
