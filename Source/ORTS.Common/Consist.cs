// COPYRIGHT 2020 by the Open Rails project.
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

namespace ORTS.Common
{
    /// <summary>
    /// An engine or wagon reference that has been prepared for loading by the simulator.
    /// </summary>
    public class WagonSpecification
    {
        public string FilePath { get; }
        public bool Flipped { get; }
        public int UiD { get; }

        public WagonSpecification(string filePath, bool flipped, int uid)
        {
            FilePath = filePath;
            Flipped = flipped;
            UiD = uid;
        }
    }

    public interface IConsist
    {
        string Name { get; }
        float? MaxVelocityMpS { get; }
        float Durability { get; }
        bool PlayerDrivable { get; }
        IEnumerable<WagonSpecification> GetWagonList(string basePath, IDictionary<string, string> folders, string preferredLocomotivePath = null);
        ICollection<string> GetLeadLocomotiveChoices(string basePath, IDictionary<string, string> folders);
        ICollection<string> GetReverseLocomotiveChoices(string basePath, IDictionary<string, string> folders);
    }
}
