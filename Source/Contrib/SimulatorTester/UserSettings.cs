// COPYRIGHT 2022 by the Open Rails project.
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
using ORTS.Common;

namespace SimulatorTester
{
    public class UserSettings : ORTS.Settings.UserSettings
    {
        [Default(false)]
        public bool Quiet { get; set; }

        [Default(false)]
        public bool Verbose { get; set; }

        [Default(10)]
        public int FPS { get; set; }

        public UserSettings(IEnumerable<string> options)
            : base(options)
        {
        }
    }
}
