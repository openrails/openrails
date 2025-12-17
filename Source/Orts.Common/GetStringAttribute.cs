// COPYRIGHT 2013, 2014 by the Open Rails project.
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
using System.Linq;

namespace ORTS.Common
{
    /// <summary>
    /// Localization attribute for decorating enums
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class GetStringAttribute : Attribute
    {
        public string Name { get; protected set; }
        public GetStringAttribute(string name) { Name = name; }

        public static string GetPrettyName(Enum value)
        {
            var type = value.GetType();
            return type.GetField(Enum.GetName(type, value))
                .GetCustomAttributes(false)
                .OfType<GetStringAttribute>()
                .FirstOrDefault()
                .Name;
        }
    }

    /// <summary>
    /// Localization attribute for decorating enums
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class GetParticularStringAttribute : GetStringAttribute
    {
        public string Context { get; protected set; }
        public GetParticularStringAttribute(string context, string name) : base(name) { Context = context; }

        public static string GetParticularPrettyName(string context, Enum value)
        {
            var type = value.GetType();
            return type.GetField(Enum.GetName(type, value))
                .GetCustomAttributes(false)
                .OfType<GetParticularStringAttribute>()
                .FirstOrDefault(attribute => attribute.Context == context)
                .Name;
        }
    }
}
