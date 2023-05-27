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

using System.Collections.Generic;
using Orts.Viewer3D.RollingStock;

namespace Orts.Viewer3D.WebServices
{
    /// <summary>
    /// Contains information about a single cab control.
    /// </summary>
    public struct ControlValue
    {
        public string TypeName;
        public double MinValue;
        public double MaxValue;
        public double RangeFraction;
    }


    /// <summary>
    /// Contains a posted value for a single cab control.
    /// </summary>
    public struct ControlValuePost
    {
        public string TypeName;
        public int ControlIndex;
        public double Value;
    }

    public static class LocomotiveViewerExtensions
    {
        /// <summary>
        /// Get a list of all cab controls.
        /// </summary>
        /// <param name="viewer">The locomotive viewer instance.</param>
        /// <returns></returns>
        public static IList<ControlValue> GetWebControlValueList(this MSTSLocomotiveViewer viewer)
        {
            var controlValueList = new List<ControlValue>();
            foreach (var controlRenderer in viewer._CabRenderer.ControlMap.Values)
            {
                controlValueList.Add(new ControlValue
                {
                    TypeName = controlRenderer.GetControlType().ToString()
                    ,
                    MinValue = controlRenderer.Control.MinValue
                    ,
                    MaxValue = controlRenderer.Control.MaxValue
                    ,
                    RangeFraction = controlRenderer.GetRangeFraction()
                });
            }
            return controlValueList;
        }
    }
}
