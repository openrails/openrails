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

namespace ORTS
{
    /// <summary>
    /// A data converter converts from legacy STF formats to newer Open Rails formats. Data converters may be run before starting RunActivity.exe.
    /// </summary>
    public interface IDataConverter
    {
        /// <summary>
        /// If a source file is available and more recent than any destination file, then conversion is appropriate.
        /// </summary>
        bool IsAppropriate(MainForm form);
        /// <summary>
        /// Prompts to carry out conversion, carries it out using Contrib.DataConverter.exe and returns flag to continue.
        /// </summary>
        bool IsDone(MainForm form);
    }
}
