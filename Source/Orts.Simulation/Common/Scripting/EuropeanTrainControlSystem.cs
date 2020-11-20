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
        public Mode CurrentMode;
        public string NTCMode;

        // Speed and distance monitoring
        public bool SpeedAreaShown;
        public float EstimatedSpeedMpS;
        public float AllowedSpeedMpS;
        public float? TargetSpeedMpS;
        public float? TargetDistanceM;
        public float? ReleaseSpeedMpS;
        public float? TimeToIndication;
        MonitoringStatus CurrentMonitor;
        SupervisionStatus CurrentSupervisionStatus;

        // Planning information
        public bool PlanningAreaShown = true;
        public List<PlanningTarget> SpeedTargets = new List<PlanningTarget>();
        public PlanningTarget? IndicationMarkerTarget;
        public float? IndicationMarkerDistanceM;
        public List<GradientProfileElement> GradientProfile = new List<GradientProfileElement>();

        public ETCSStatus Clone()
        {
            ETCSStatus other = (ETCSStatus)MemberwiseClone();
            other.NTCMode = (string)NTCMode?.Clone();
            other.SpeedTargets = new List<PlanningTarget>(SpeedTargets);
            other.GradientProfile = new List<GradientProfileElement>(GradientProfile);
            return other;
        }
    }
}
namespace ORTS.Scripting.Api.ETCS
{
    public enum MonitoringStatus
    {
        CSM,
        TSM,
        RSM,
    }
    public enum SupervisionStatus
    {
        NoS,
        IndS,
        OvS,
        WaS,
        IntS
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
        TractionSystemChange
    }

    public enum TractionSystem
    {
        NonFitted,
        AC25kV
    }

    public struct TrackCondition
    {
        public readonly TrackConditionType Type;
        public float DistanceToTrainM;
        public readonly bool YellowColour;
        public TractionSystem? TractionSystem;

        public TrackCondition(TrackConditionType type, bool isYellowColour, float distanceToTrainM)
        {
            DistanceToTrainM = distanceToTrainM;
            Type = type;
            YellowColour = isYellowColour;
            TractionSystem = null;
        }

        public TrackCondition(TractionSystem tractionSystem, bool isYellowColour, float distanceToTrainM)
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