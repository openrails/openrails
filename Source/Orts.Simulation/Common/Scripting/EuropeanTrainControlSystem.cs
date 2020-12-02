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

using System;
using System.Collections.Generic;

namespace ORTS.Scripting.Api.ETCS
{
    /// <summary>
    /// Current status of ETCS, to be shown in DMI
    /// </summary>
    public class ETCSStatus
    {
        // General status
        /// <summary>
        /// True if the DMI is active and will be shown
        /// </summary>
        public bool DMIActive = true;
        /// <summary>
        /// Current operating level
        /// </summary>
        public Level CurrentLevel;
        /// <summary>
        /// Current ETCS supervision mode
        /// </summary>
        public Mode CurrentMode = Mode.FS;
        /// <summary>
        /// If mode is NTC, specific name of the NTC. Not used yet
        /// </summary>
        public string NTCMode;

        // Speed and distance monitoring
        /// <summary>
        /// True if the speedometer (needle and gauges) are to be shown
        /// </summary>
        public bool SpeedAreaShown = true;
        /// <summary>
        /// Permitted speed to be shown in the circular speed gauge
        /// </summary>
        public float AllowedSpeedMpS;
        /// <summary>
        /// Intervention speed at which brakes will be applied
        /// </summary>
        public float InterventionSpeedMpS;
        /// <summary>
        /// Target speed limit to be displayed, if any
        /// </summary>
        public float? TargetSpeedMpS;
        /// <summary>
        /// Distance to the speed reduction being displayed, if any
        /// </summary>
        public float? TargetDistanceM;
        /// <summary>
        /// Speed at which the train is allowed to approach to a zero target
        /// </summary>
        public float? ReleaseSpeedMpS;
        /// <summary>
        /// Visual indication for the driver to help him follow the braking curve with reduced adheasion conditions.
        /// It is shown as a grey/white square at the top left corner of the DMI while in CSM.
        /// </summary>
        public float? TimeToIndicationS;
        /// <summary>
        /// Visual indication for the driver to help him follow the permitted speed curve.
        /// It is shown as a yellow, orange or red square at the top left corner of the DMI while in TSM.
        /// </summary>
        public float? TimeToPermittedS;
        /// <summary>
        /// Current speed monitoring status, either ceiling speed, target speed or release speed
        /// </summary>
        public Monitor CurrentMonitor;
        /// <summary>
        /// Determines the color of the needle and circular speed gauge, depending on train speed
        /// </summary>
        public SupervisionStatus CurrentSupervisionStatus;

        // Planning information
        /// <summary>
        /// Set to true to display planning area
        /// </summary>
        public bool PlanningAreaShown = true;
        /// <summary>
        /// List of targets to be shown in the planning area.
        /// First target must be current speed limit, with distance = 0
        /// It will also be used to draw the planning area speed profile (PASP)
        /// </summary>
        public readonly List<PlanningTarget> SpeedTargets = new List<PlanningTarget>();
        /// <summary>
        /// Target with the closest distance to indication.
        /// Its speed limit will be shown in yellow in the planning area
        /// </summary>
        public PlanningTarget? IndicationMarkerTarget;
        /// <summary>
        /// Distance to the point where the monitoring status will change to either TSM or RSM from CSM
        /// Will be shown as a yellow horizontal line in the planning area
        /// </summary>
        public float? IndicationMarkerDistanceM;
        /// <summary>
        /// Gradient of the track ahead of the train, to be shown in planning area
        /// First gradient must be with distance = 0
        /// At the point where the gradient profile ends a target must be inserted with any value, to mark the end of the profile
        /// </summary>
        public readonly List<GradientProfileElement> GradientProfile = new List<GradientProfileElement>();
        /// <summary>
        /// Orders and announcements ahead to be displayed in the planning area
        /// </summary>
        public readonly List<PlanningTrackCondition> PlanningTrackConditions = new List<PlanningTrackCondition>();
        /// <summary>
        /// True if the text message area shall be displayed
        /// </summary>
        public bool ShowTextMessageArea;
        /// <summary>
        /// List of text messages to be displayed in text area
        /// </summary>
        public readonly List<TextMessage> TextMessages = new List<TextMessage>();
    }

    /// <summary>
    /// Monitoring status of ETCS
    /// </summary>
    public enum Monitor
    {
        /// <summary>
        /// No speed restriction ahead, a fixed speed is supervised.
        /// </summary>
        CeilingSpeed,
        /// <summary>
        /// A braking curve is being supervised while approaching a speed target
        /// </summary>
        TargetSpeed,
        /// <summary>
        /// A release speed is supervised while approaching a zero speed target
        /// </summary>
        ReleaseSpeed
    }

    /// <summary>
    /// Controls supervision status of ETCS
    /// </summary>
    public enum SupervisionStatus
    {
        /// <summary>
        /// Grey color. No speed restriction is ahead.
        /// </summary>
        Normal,
        /// <summary>
        /// Yellow color. Next signal is restricted, driver should start decreasing speed.
        /// </summary>
        Indication,
        /// <summary>
        /// Orange color. The locomotive is over the permitted supervision limit.
        /// </summary>
        Overspeed,
        /// <summary>
        /// Orange color. Computer is close to apply brakes, audible warning is played.
        /// </summary>
        Warning,
        /// <summary>
        /// Red color. Train control system intervention speed. Computer has to apply full service or emergency brake to maintain speed restriction.
        /// </summary>
        Intervention
    }

    public enum Level
    {
        N0,
        N1,
        N2,
        N3,
        NTC
    }
    public enum Mode
    {
        FS,
        LS,
        OS,
        SR,
        SH,
        UN,
        PS,
        SL,
        SB,
        TR,
        PT,
        SF,
        IS,
        NP,
        NL,
        SN,
        RV
    }

    public enum TrackConditionType
    {
        Custom,
        LowerPantograph,
        RaisePantograph,
        NeutralSectionAnnouncement,
        EndOfNeutralSection,
        NonStoppingArea,
        RadioHole,
        MagneticShoeInhibition,
        EddyCurrentBrakeInhibition,
        RegenerativeBrakeInhibition,
        OpenAirIntake,
        CloseAirIntake,
        SoundHorn,
        TractionSystemChange
    }

    public enum TractionSystem
    {
        NonFitted,
        AC25kV,
        AC15kV,
        DC3000V,
        DC1500V,
        DC750V
    }

    public struct PlanningTrackCondition
    {
        public readonly TrackConditionType Type;
        public float DistanceToTrainM;
        public readonly bool YellowColour;
        public TractionSystem? TractionSystem;

        public PlanningTrackCondition(TrackConditionType type, bool isYellowColour, float distanceToTrainM)
        {
            DistanceToTrainM = distanceToTrainM;
            Type = type;
            YellowColour = isYellowColour;
            TractionSystem = null;
        }

        public PlanningTrackCondition(TractionSystem tractionSystem, bool isYellowColour, float distanceToTrainM)
        {
            DistanceToTrainM = distanceToTrainM;
            Type = TrackConditionType.TractionSystemChange;
            YellowColour = isYellowColour;
            TractionSystem = tractionSystem;
        }
    }

    public struct PlanningTarget
    {
        public float DistanceToTrainM;
        public readonly float TargetSpeedMpS;
        public PlanningTarget(float distanceToTrainM, float targetSpeedMpS)
        {
            DistanceToTrainM = distanceToTrainM;
            TargetSpeedMpS = targetSpeedMpS;
        }
    }
    public struct GradientProfileElement
    {
        public float DistanceToTrainM;
        public int GradientPerMille;
        public GradientProfileElement(float distanceToTrainM, int gradientPerMille)
        {
            DistanceToTrainM = distanceToTrainM;
            GradientPerMille = gradientPerMille;
        }
    }
    /// <summary>
    /// Defines a text message to be shown in DMI
    /// </summary>
    public struct TextMessage : IEquatable<TextMessage>
    {
        /// <summary>
        /// Text to show
        /// </summary>
        public readonly string Text;
        /// <summary>
        /// Defines the priority of the message
        /// </summary>
        public readonly bool FirstGroup;
        /// <summary>
        /// Timestamp in seconds of the message
        /// </summary>
        public float TimestampS;
        /// <summary>
        /// True if it needs acknowledgement from the driver
        /// </summary>
        public bool Acknowledgeable;
        /// <summary>
        /// Will be set by the DMI if the driver acknowledges the message
        /// </summary>
        public bool Acknowledged;
        /// <summary>
        /// Internally used by the DMI to determine if it shall inform the driver about the reception
        /// </summary>
        public bool Displayed;
        public TextMessage(string text, float timestampS, bool firstGroup = false, bool acknowledgeable = false)
        {
            Text = text;
            TimestampS = timestampS;
            FirstGroup = firstGroup;
            Acknowledgeable = acknowledgeable;
            Acknowledged = false;
            Displayed = false;
        }
        public bool Equals(TextMessage o)
        {
            return o.Text == Text && o.TimestampS == TimestampS;
        }
    }
}