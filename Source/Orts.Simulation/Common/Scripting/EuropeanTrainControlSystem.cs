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
using ORTS.Scripting.Api.ETCS;

namespace ORTS.Scripting.Api
{
    /// <summary>
    /// Current status of ETCS, to be shown in DMI
    /// </summary>
    public class ETCSStatus
    {
        // General status
        public bool DMIActive = true;
        public Level CurrentLevel;
        public Mode CurrentMode = Mode.FS;
        public string NTCMode;

        // Speed and distance monitoring
        public bool SpeedAreaShown = true;
        public float EstimatedSpeedMpS;
        public float AllowedSpeedMpS;
        public float InterventionSpeedMpS;
        public float? TargetSpeedMpS;
        public float? TargetDistanceM;
        public float? ReleaseSpeedMpS;
        public float? TimeToIndication;
        public Monitor CurrentMonitor;
        public SupervisionStatus CurrentSupervisionStatus;

        // Planning information
        public bool PlanningAreaShown = true;
        public List<PlanningTarget> SpeedTargets = new List<PlanningTarget>();
        public PlanningTarget? IndicationMarkerTarget;
        public float? IndicationMarkerDistanceM;
        public List<GradientProfileElement> GradientProfile = new List<GradientProfileElement>();
        public List<PlanningTrackCondition> PlanningTrackConditions = new List<PlanningTrackCondition>();

        public ETCSStatus Clone()
        {
            ETCSStatus other = (ETCSStatus)MemberwiseClone();
            other.NTCMode = (string)NTCMode?.Clone();
            other.SpeedTargets = new List<PlanningTarget>(SpeedTargets);
            other.GradientProfile = new List<GradientProfileElement>(GradientProfile);
            other.PlanningTrackConditions = new List<PlanningTrackCondition>();
            return other;
        }
    }
}
namespace ORTS.Scripting.Api.ETCS
{
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
        /// A target speed is being supervised.
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
        AC25kV
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

        // TODO: Allow custom symbols
        /*public TrackCondition(string customImage, float distanceToTrainM)
        {
            Type = TrackConditionType.Custom;
            DistanceToTrainM = distanceToTrainM;
            YellowColour = false;
            TractionSystem = null;
        }*/
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
}