// COPYRIGHT 2014 by the Open Rails project.
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
using System.Linq;
using System.Text;

namespace Orts.Formats.Msts
{
    /// <summary>
    /// Describe the various states a block (roughly a region between two signals) can be in.
    /// </summary>
    public enum MstsBlockState
    {
        /// <summary>Block ahead is clear and accesible</summary>
        CLEAR,
        /// <summary>Block ahead is occupied by one or more wagons/locos not moving in opposite direction</summary>
        OCCUPIED,
        /// <summary>Block ahead is impassable due to the state of a switch or occupied by moving train or not accesible</summary>
        JN_OBSTRUCTED,
    }

    /// <summary>
    /// Describe the various aspects (or signal indication states) that MSTS signals can have.
    /// Within MSTS known as SIGASP_ values.  
    /// Note: They are in order from most restrictive to least restrictive.
    /// </summary>
    public enum MstsSignalAspect
    {
        /// <summary>Stop (absolute)</summary>
        STOP,
        /// <summary>Stop and proceed</summary>
        STOP_AND_PROCEED,
        /// <summary>Restricting</summary>
        RESTRICTING,
        /// <summary>Final caution before 'stop' or 'stop and proceed'</summary>
        APPROACH_1,
        /// <summary>Advanced caution</summary>
        APPROACH_2,
        /// <summary>Least restrictive advanced caution</summary>
        APPROACH_3,
        /// <summary>Clear to next signal</summary>
        CLEAR_1,
        /// <summary>Clear to next signal (least restrictive)</summary>
        CLEAR_2,
        /// <summary>Signal aspect is unknown (possibly not yet defined)</summary>
        UNKNOWN,
    }

    /// <summary>
    /// Describe the function of a particular signal head.
    /// Only SIGFN_NORMAL signal heads will require a train to take action (e.g. to stop).  
    /// The other values act only as categories for signal types to belong to.
    /// Within MSTS known as SIGFN_ values.  
    /// </summary>
    public enum MstsSignalFunction
    {
        /// <summary>Signal head showing primary indication</summary>
        NORMAL,
        /// <summary>Distance signal head</summary>
        DISTANCE,
        /// <summary>Repeater signal head</summary>
        REPEATER,
        /// <summary>Shunting signal head</summary>
        SHUNTING,
        /// <summary>Signal is informational only e.g. direction lights</summary>
        INFO,
        /// <summary>Speedpost signal (not part of MSTS SIGFN_)</summary>
        SPEED,
        /// <summary>Alerting function not part of MSTS SIGFN_)</summary>
        ALERT,
        /// <summary>Unknown (or undefined) signal type</summary>
        UNKNOWN, // needs to be last because some code depends this for looping. That should be changed of course.
    }
}
