// COPYRIGHT 2026 by the Open Rails project.
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

namespace Orts.Common
{
    /// <summary>
    /// Assorted small methods to help with manipulating strings
    /// </summary>
    public static class StringHelper
    {
        /// <summary>
        /// If possible, removes specified text from the beginning and end of a given string,
        /// returning the resulting string after removal of the beginning and end.
        /// If the beginning or end values are empty or are not found, nothing will be removed.
        /// </summary>
        /// <param name="main">The original string to be trimmed</param>
        /// <param name="beginning">The text that should be removed from the beginning of <paramref name="main"/></param>
        /// <param name="end">The text that should be removed from the end of <paramref name="main"/></param>
        /// <param name="stringComparer">(Optional) string comparison to be used, defaults to case-sensitive</param>
        /// <returns>The remainder of string <paramref name="main"/> after removing both <paramref name="beginning"/>
        /// from the beginning and <paramref name="end"/> from the end</returns>
        public static string TrimBeginningAndEnd(string main, string beginning, string end, StringComparison stringComparer = StringComparison.Ordinal)
        {
            string result = main;

            result = TrimBeginning(result, beginning, stringComparer);
            result = TrimEnd(result, end, stringComparer);

            return result;
        }

        /// <summary>
        /// If possible, removes specified text from the beginning of a given string,
        /// returning the resulting string after removal of the beginning.
        /// If the beginning value is empty or not found, nothing will be removed.
        /// </summary>
        /// <param name="main">The original string to be trimmed</param>
        /// <param name="beginning">The text that should be removed from the beginning of <paramref name="main"/></param>
        /// <param name="stringComparer">(Optional) string comparison to be used, defaults to case-sensitive</param>
        /// <returns>The remainder of string <paramref name="main"/> after removing <paramref name="beginning"/>
        /// from the beginning</returns>
        public static string TrimBeginning(string main, string beginning, StringComparison stringComparer = StringComparison.Ordinal)
        {
            string result = main;

            if (!string.IsNullOrEmpty(beginning))
                result = result.Replace(beginning, "", stringComparer);

            return result;
        }

        /// <summary>
        /// If possible, removes specified text from the end of a given string,
        /// returning the resulting string after removal of the end.
        /// If the end value is empty or not found, nothing will be removed.
        /// </summary>
        /// <param name="main">The original string to be trimmed</param>
        /// <param name="end">The text that should be removed from the end of <paramref name="main"/></param>
        /// <param name="stringComparer">(Optional) string comparison to be used, defaults to case-sensitive</param>
        /// <returns>The remainder of string <paramref name="main"/> after removing
        /// <paramref name="end"/> from the end</returns>
        public static string TrimEnd(string main, string end, StringComparison stringComparer = StringComparison.Ordinal)
        {
            string result = main;

            if (!string.IsNullOrEmpty(end))
                result = result.Replace(end, "", stringComparer);

            return result;
        }

        /// <summary>
        /// Extracts an integer value from a string by removing text, if possible, from the beginning and end of the
        /// original string, returning the integer value contained within (or null if no integer could be read).
        /// If the beginning or end values are empty or are not found, the integer will attempt to be read from
        /// the original string.
        /// </summary>
        /// <param name="main">The original string to find an integer inside</param>
        /// <param name="beginning">The text at the beginning of <paramref name="main"/> that precedes the integer to find</param>
        /// <param name="end">The text at the end of <paramref name="main"/> that follows the integer to find</param>
        /// <param name="stringComparer">(Optional) string comparison to be used, defaults to case-sensitive</param>
        /// <returns>The integer contained within <paramref name="main"/> after removing <paramref name="beginning"/>
        /// from the beginning and <paramref name="end"/> from the end</returns>
        public static int? ExtractInt(string main, string beginning, string end, StringComparison stringComparer = StringComparison.Ordinal)
        {
            string result = main;

            result = TrimBeginningAndEnd(result, beginning, end, stringComparer);

            if (!int.TryParse(result, out int output))
                return null;
            else
                return output;
        }

        /// <summary>
        /// Extracts a floating point value from a string by removing text, if possible, from the beginning and end of the
        /// original string, returning the float value contained within (or null if no float could be read).
        /// If the beginning or end values are empty or are not found, the float will attempt to be read from
        /// the original string.
        /// </summary>
        /// <param name="main">The original string to find a float inside</param>
        /// <param name="beginning">The text at the beginning of <paramref name="main"/> that precedes the float to find</param>
        /// <param name="end">The text at the end of <paramref name="main"/> that follows the float to find</param>
        /// <param name="stringComparer">(Optional) string comparison to be used, defaults to case-sensitive</param>
        /// <returns>The floating point value contained within <paramref name="main"/> after removing <paramref name="beginning"/>
        /// from the beginning and <paramref name="end"/> from the end</returns>
        public static float? ExtractFloat(string main, string beginning, string end, StringComparison stringComparer = StringComparison.Ordinal)
        {
            string result = main;

            result = TrimBeginningAndEnd(result, beginning, end, stringComparer);

            if (!float.TryParse(result, out float output))
                return null;
            else
                return output;
        }
    }
}
