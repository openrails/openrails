// COPYRIGHT 2023 by the Open Rails project.
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
//

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Orts.Viewer3D.WebServices.SwitchPanel
{
    public class SwitchPanelEvent
    {
        public SwitchPanelEvent(string type, object data)
        {
            Type = type;
            Data = data;
        }

        [JsonProperty("type")]
        public string Type { get; private set; }

        [JsonProperty("data")]
        public object Data { get; set; }
    }
}
