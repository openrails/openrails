// COPYRIGHT 2014, 2015 by the Open Rails project.
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


namespace ORTS.Common
{
    /// <summary>
    /// List of items that can be shown through the Editor.  These items can be searchable, modifiable, movable, ...
    /// </summary>
    public enum TypeItem
    {
        GLOBAL_ITEM = 0,
        SIGNAL_ITEM = 1,
        SWITCH_ITEM = 2,
        TAG_ITEM = 3,
        BUFFER_ITEM = 4,
        SIDING_START = 5,
        STATION_ITEM = 6,
        STATION_AREA_ITEM = 7,
        STATION_CONNECTOR = 8,
        ACTIVITY_ITEM = 9,
        SIDING_END = 10,
        CROSS_OVER = 11
    };

    public enum TypeSiding
    {
        SIDING_START = 0,
        SIDING_END = 1,
        PLATFORM_START = 2,
        PLATFORM_END = 3
    };
    public enum TypeEditor
    {
        ACTIVITY = 1,
        ROUTECONFIG = 2,
        TRAFFIC = 3,
        NONE = 0
    };
}
