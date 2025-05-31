// COPYRIGHT 2013, 2014 by the Open Rails project.
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

using Microsoft.Xna.Framework;
using Orts.Common;
using Orts.Parsers.Msts;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using ORTS.Common;
using ORTS.Scripting.Api;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System;
using System.Linq;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerTransmissions
{
    public class MSTSGearBoxParams
    {
        public int GearBoxNumberOfGears = 1;
        public bool ReverseGearBoxIndication;
        public int GearBoxDirectDriveGear = 1;
        public bool FreeWheelFitted = false;
        public TypesGearBox GearBoxType = TypesGearBox.Unknown;
        // GearboxType ( A ) - power is continuous during gear changes (and throttle does not need to be adjusted)
        // GearboxType ( B ) - power is interrupted during gear changes - but the throttle does not need to be adjusted when changing gear
        // GearboxType ( C ) - power is interrupted and if GearboxOperation is Manual throttle must be closed when changing gear
        // GearboxType ( D ) - power is interrupted and if GearboxOperation is Manual throttle must be closed when changing gear, clutch will remain engaged, and can stall engine

        public TypesClutch ClutchType = TypesClutch.Unknown;


        public GearBoxOperation GearBoxOperation = GearBoxOperation.Manual;
        public GearBoxEngineBraking GearBoxEngineBraking = GearBoxEngineBraking.None;
        public List<float> GearBoxMaxSpeedForGearsMpS = new List<float>();
        public List<float> GearBoxChangeUpSpeedRpM = new List<float>();
        public List<float> GearBoxChangeDownSpeedRpM = new List<float>();
        public List<float> GearBoxMaxTractiveForceForGearsN = new List<float>();
        public List<float> GearBoxTractiveForceAtSpeedN = new List<float>();
        public float GearBoxOverspeedPercentageForFailure = 150f;
        public float GearBoxBackLoadForceN = 1000;
        public float GearBoxCoastingForceN = 500;
        public float GearBoxUpGearProportion = 0.85f;
        public float GearBoxDownGearProportion = 0.35f;

        int initLevel;

        public bool MaxTEFound = false;

        public bool IsInitialized { get { return initLevel >= 3; } }
        public bool AtLeastOneParamFound { get { return initLevel >= 1; } }

        public void Parse(string lowercasetoken, STFReader stf)
        {
            string temp = "";
            switch (lowercasetoken)
            {
                case "engine(gearboxnumberofgears": GearBoxNumberOfGears = stf.ReadIntBlock(1); initLevel++; break;
                case "engine(ortsreversegearboxindication": int tempIndication = stf.ReadIntBlock(1); if (tempIndication == 1) { ReverseGearBoxIndication = true; }; break;
                case "engine(gearboxdirectdrivegear": GearBoxDirectDriveGear = stf.ReadIntBlock(1); break;
                case "engine(ortsgearboxfreewheel":
                    var freeWheel = stf.ReadIntBlock(null);
                    if (freeWheel == 1)
                    {
                        FreeWheelFitted = true;
                    }
                    break;
                case "engine(ortsgearboxtype":
                    stf.MustMatch("(");
                    var gearType = stf.ReadString();
                    try
                    {
                        GearBoxType = (TypesGearBox)Enum.Parse(typeof(TypesGearBox), gearType.First().ToString().ToUpper() + gearType.Substring(1));
                    }
                    catch
                    {
                        STFException.TraceWarning(stf, "Assumed unknown gear box type " + gearType);
                    }
                    break;
                case "engine(ortsmainclutchtype":
                    stf.MustMatch("(");
                    var clutchType = stf.ReadString();
                    try
                    {
                        ClutchType = (TypesClutch)Enum.Parse(typeof(TypesClutch), clutchType.First().ToString().ToUpper() + clutchType.Substring(1));
                    }
                    catch
                    {
                        STFException.TraceWarning(stf, "Assumed unknown main clutch type " + clutchType);
                    }
                    break;
                case "engine(gearboxoperation":
                    stf.MustMatch("(");
                    var gearOperation = stf.ReadString();
                    try
                    {
                        GearBoxOperation = (GearBoxOperation)Enum.Parse(typeof(GearBoxOperation), gearOperation.First().ToString().ToUpper() + gearOperation.Substring(1));
                    }
                    catch
                    {
                        STFException.TraceWarning(stf, "Assumed unknown gear box operation type " + gearOperation);
                    }
                    initLevel++;
                    break;
                case "engine(gearboxenginebraking":
                    stf.MustMatch("(");
                    var engineBraking = stf.ReadString();
                    try
                    {
                        GearBoxEngineBraking = (GearBoxEngineBraking)Enum.Parse(typeof(GearBoxEngineBraking), engineBraking.First().ToString().ToUpper() + engineBraking.Substring(1));
                    }
                    catch
                    {
                        STFException.TraceWarning(stf, "Assumed unknown gear box engine braking type " + engineBraking);
                    }
                    break;
                case "engine(gearboxmaxspeedforgears":
                    temp = stf.ReadItem();
                    if (temp == ")")
                    {
                        stf.StepBackOneItem();
                    }
                    if (temp == "(")
                    {
                        GearBoxMaxSpeedForGearsMpS.Clear();
                        for (int i = 0; i < GearBoxNumberOfGears; i++)
                        {
                            GearBoxMaxSpeedForGearsMpS.Add(stf.ReadFloat(STFReader.UNITS.SpeedDefaultMPH, 10.0f));
                        }
                        stf.SkipRestOfBlock();
                        initLevel++;
                    }
                    break;
                // gearboxmaxtractiveforceforgears purely retained for legacy reasons
                case "engine(gearboxmaxtractiveforceforgears":
                    temp = stf.ReadItem();
                    if (temp == ")")
                    {
                        stf.StepBackOneItem();
                    }
                    if (temp == "(")
                    {
                        GearBoxMaxTractiveForceForGearsN.Clear();
                        for (int i = 0; i < GearBoxNumberOfGears; i++)
                            GearBoxMaxTractiveForceForGearsN.Add(stf.ReadFloat(STFReader.UNITS.Force, 10000.0f));
                        stf.SkipRestOfBlock();
                        initLevel++;
                    }
                    break;
                case "engine(ortsgearboxtractiveforceatspeed":
                    MaxTEFound = true;
                    temp = stf.ReadItem();
                    if (temp == ")")
                    {
                        stf.StepBackOneItem();
                    }
                    if (temp == "(")
                    {
                        GearBoxTractiveForceAtSpeedN.Clear();
                        for (int i = 0; i < GearBoxNumberOfGears; i++)
                        {
                            GearBoxTractiveForceAtSpeedN.Add(stf.ReadFloat(STFReader.UNITS.Force, 0f));
                        }
                        stf.SkipRestOfBlock();
                        initLevel++;
                    }
                    break;
                case "engine(gearboxoverspeedpercentageforfailure": GearBoxOverspeedPercentageForFailure = stf.ReadFloatBlock(STFReader.UNITS.None, 150f); break; // initLevel++; break;
                case "engine(gearboxbackloadforce": GearBoxBackLoadForceN = stf.ReadFloatBlock(STFReader.UNITS.Force, 0f); break;
                case "engine(gearboxcoastingforce": GearBoxCoastingForceN = stf.ReadFloatBlock(STFReader.UNITS.Force, 0f); break;
                case "engine(gearboxupgearproportion": GearBoxUpGearProportion = stf.ReadFloatBlock(STFReader.UNITS.None, 0.85f); break; // initLevel++; break;
                case "engine(gearboxdowngearproportion": GearBoxDownGearProportion = stf.ReadFloatBlock(STFReader.UNITS.None, 0.25f); break; // initLevel++; break;
                default: break;
            }
        }

        public void Copy(MSTSGearBoxParams copy)
        {
            GearBoxNumberOfGears = copy.GearBoxNumberOfGears;
            ReverseGearBoxIndication = copy.ReverseGearBoxIndication;
            GearBoxDirectDriveGear = copy.GearBoxDirectDriveGear;
            GearBoxType = copy.GearBoxType;
            MaxTEFound = copy.MaxTEFound;
            ClutchType = copy.ClutchType;
            GearBoxOperation = copy.GearBoxOperation;
            GearBoxEngineBraking = copy.GearBoxEngineBraking;
            GearBoxMaxSpeedForGearsMpS = new List<float>(copy.GearBoxMaxSpeedForGearsMpS);
            GearBoxChangeUpSpeedRpM = new List<float>(copy.GearBoxChangeUpSpeedRpM);
            GearBoxChangeDownSpeedRpM = new List<float>(copy.GearBoxChangeDownSpeedRpM);
            GearBoxMaxTractiveForceForGearsN = new List<float>(copy.GearBoxMaxTractiveForceForGearsN);
            GearBoxTractiveForceAtSpeedN = new List<float>(copy.GearBoxTractiveForceAtSpeedN);
            GearBoxOverspeedPercentageForFailure = copy.GearBoxOverspeedPercentageForFailure;
            GearBoxBackLoadForceN = copy.GearBoxBackLoadForceN;
            GearBoxCoastingForceN = copy.GearBoxCoastingForceN;
            GearBoxUpGearProportion = copy.GearBoxUpGearProportion;
            GearBoxDownGearProportion = copy.GearBoxDownGearProportion;
            FreeWheelFitted = copy.FreeWheelFitted;
            initLevel = copy.initLevel;
        }
    }

    public class GearBox : ISubSystem<GearBox>
    {
        protected readonly DieselEngine DieselEngine;
        protected readonly MSTSDieselLocomotive Locomotive;
        protected MSTSGearBoxParams GearBoxParams => Locomotive.DieselEngines.MSTSGearBoxParams;
        public List<Gear> Gears = new List<Gear>();

        public bool GearBoxFreeWheelFitted;
        public bool GearBoxFreeWheelEnabled = false;

        public bool GearedThrottleDecrease = false;
        public float previousGearThrottleSetting;
        public float previousRpM;

        public float ManualGearTimerResetS = 2;  // Allow gear change to take 2 seconds
        public float ManualGearTimerS; // Time for gears to change
        public bool ManualGearBoxChangeOn = false;
        public bool ManualGearUp = false;
        public bool ManualGearDown = false;
        public bool clutchLockOut = false;
        
        public int currentGearIndex = -1;
        public int nextGearIndex = -1;

        public Gear CurrentGear
        {
            get
            {
                if ((currentGearIndex >= 0)&&(currentGearIndex < NumOfGears))
                    return Gears[currentGearIndex];
                else
                    return null;
            }
        }

        public int CurrentGearIndex
        {
            get
            {
                return currentGearIndex;
            }
            set
            {
                currentGearIndex = value;
            }
        }

public Gear NextGear 
        {
            get
            {
                if ((nextGearIndex >= 0) && (nextGearIndex < NumOfGears))
                    return Gears[nextGearIndex];
                else
                    return null;
            }
            set
            {
                switch (GearBoxOperation)
                {
                    case GearBoxOperation.Manual:
                    case GearBoxOperation.Semiautomatic:
                        int temp = 0;
                        if(value == null)
                            nextGearIndex = -1;
                        else
                        {
                            foreach (Gear gear in Gears)
                            {
                                temp++;
                                if (gear == value)
                                {
                                    break;
                                }
                            }
                            nextGearIndex = temp - 1;
                        }
                        break;
                    case GearBoxOperation.Automatic:
                        break;
                }
            }
        }

        public int NextGearIndex { get { return nextGearIndex; } }

        private bool gearedUp;
        private bool gearedDown;
        public bool GearedUp { get { return gearedUp; } }
        public bool GearedDown { get { return gearedDown; } }

        public bool AutoGearUp()
        {
            if (clutch < 0.05f)
            {
                if (!gearedUp)
                {
                    if(++nextGearIndex >= Gears.Count)
                        nextGearIndex =  (Gears.Count - 1);
                    else
                        gearedUp = true;
                }
                else
                    gearedUp = false;
            }
            return gearedUp;
        }

        public bool AutoGearDown()
        {
            if (clutch < 0.05f)
            {
                if (!gearedDown)
                {
                    if(--nextGearIndex <= 0)
                        nextGearIndex =  0;
                    else
                        gearedDown = true;
                }
                else
                    gearedDown = false;
            }
            return gearedDown;
        }

        public void AutoAtGear()
        {
            gearedUp = false;
            gearedDown = false;
        }

        /// <summary>
        /// Indicates when a manual gear change has been initiated
        /// </summary>
        public bool ManualGearChange;

        /// <summary>
        /// ClutchOn is true when clutch is fully engaged, and false when slipping
        /// </summary>
        public bool clutchOn;
        public bool IsClutchOn
        {
            get
            {
                if (Locomotive != null && Locomotive.DieselTransmissionType == MSTSDieselLocomotive.DieselTransmissionTypes.Mechanic)
                {
                    if (GearBoxOperation == GearBoxOperation.Automatic)
                    {

                        if (DieselEngine.Locomotive.ThrottlePercent > 0)
                        {
                            if (ShaftRPM >= (CurrentGear.DownGearProportion * DieselEngine.MaxRPM))
                                clutchOn = true;
                        }
                        if (ShaftRPM < DieselEngine.StartingRPM)
                            clutchOn = false;
                        return clutchOn;
                    }
                    else  // Manual clutch operation
                    {

                        if (DieselEngine.Locomotive.ThrottlePercent == 0 && !clutchOn && Locomotive.SpeedMpS < 0.05f && ClutchType == TypesClutch.Friction)
                        {
                            clutchOn = false;
                            return clutchOn;
                        }
                        else if (!GearBoxFreeWheelEnabled && DieselEngine.Locomotive.ThrottlePercent == 0 && !clutchLockOut && ManualGearBoxChangeOn && ClutchType != TypesClutch.Friction) // Fluid and Scoop clutches disengage if throttle is closed
                        {
                            clutchLockOut = true;
                            clutchOn = false;
                            return clutchOn;
                        }
                        else if (ClutchType != TypesClutch.Friction && DieselEngine.Locomotive.ThrottlePercent > 0)
                        {
                            clutchLockOut = false;
                        }

                        // Set clutch status to false when gear change is initiated.
                        if (ManualGearBoxChangeOn)
                        {
                            clutchOn = false;
                            return clutchOn;
                        }

                        // Set clutch engaged when shaftrpm and engine rpm are equal
                        if ((DieselEngine.Locomotive.ThrottlePercent >= 0 || DieselEngine.Locomotive.SpeedMpS > 0) && CurrentGear != null && !GearBoxFreeWheelEnabled)
                        {
                            var clutchEngagementBandwidthRPM = 10.0f;
                            if (ShaftRPM >= DieselEngine.RealRPM - clutchEngagementBandwidthRPM && ShaftRPM < DieselEngine.RealRPM + clutchEngagementBandwidthRPM && ShaftRPM < DieselEngine.MaxRPM && ShaftRPM > DieselEngine.IdleRPM)
                                clutchOn = true;
                            return clutchOn;
                        }
                        else if ((ClutchType == TypesClutch.Scoop || ClutchType == TypesClutch.Fluid) && CurrentGear == null)
                        {
                            clutchOn = false;
                            return clutchOn;
                        }

                        // Set clutch disengaged (slip mode) if shaft rpm moves outside of acceptable bandwidth speed (on type A, B and C clutches), Type D will not slip unless put into neutral
                        var clutchSlipBandwidth = 0.1f * DieselEngine.ThrottleRPMTab[DieselEngine.DemandedThrottlePercent]; // Bandwidth 10%
                        var speedVariationRpM = Math.Abs(DieselEngine.ThrottleRPMTab[DieselEngine.DemandedThrottlePercent] - ShaftRPM);
                        if (GearBoxFreeWheelFitted && speedVariationRpM > clutchSlipBandwidth && (GearBoxType != TypesGearBox.D || GearBoxType != TypesGearBox.A))
                        {
                            clutchOn = false;
                            return clutchOn;
                        }
                        return clutchOn;
                    }
                }
                else // default (legacy) units
                {
                    if (DieselEngine.Locomotive.ThrottlePercent > 0)
                    {
                        if (ShaftRPM >= (CurrentGear.DownGearProportion * DieselEngine.MaxRPM))
                            clutchOn = true;
                    }
                    if (ShaftRPM < DieselEngine.StartingRPM)
                        clutchOn = false;
                    return clutchOn;
                }
            }
        }

        public int NumOfGears { get { return Gears.Count; } }

        // The default gear configuration is N-1-2-3-4, etc. However some locomotives have a N-4-3-2-1 configuration. So the display indication is reversed to 
        // give the impression that this gear system is set.
        public int GearIndication
        {
            get
            {
                if (ReverseGearBoxIndication )
                {
                    int tempgear = NumOfGears - CurrentGearIndex;
                    tempgear = MathHelper.Clamp(tempgear, 0, NumOfGears);
                    return tempgear;
                }
                else
                {
                    return CurrentGearIndex + 1;
                }
            }
        }

        public float CurrentSpeedMpS 
        {
            get
            {
                return DieselEngine.Locomotive.AbsTractionSpeedMpS;
            }
        }

        /// <summary>
        /// The HuD display value for ShaftRpM
        /// </summary>
        public float HuDShaftRPM
        {
            get
            {
                if (CurrentGear == null)
                {
                    return 0;
                }
                else
                {
                    var temp = ShaftRPM;

                    return temp;
                }
            }
        }


        /// <summary>
        /// ShaftRpM is the speed of the input shaft to the gearbox due to the speed of the wheel rotation
        /// </summary>
        public float ShaftRPM 
        {
            get
            {
                if (Locomotive != null && Locomotive.DieselTransmissionType == MSTSDieselLocomotive.DieselTransmissionTypes.Mechanic)
                {
                    if (CurrentGear == null)
                    {
                        return DieselEngine.RealRPM;
                    }
                    else
                    {
                        if (GearBoxOperation == GearBoxOperation.Automatic)
                        {
                            return CurrentSpeedMpS / CurrentGear.Ratio;
                        }
                        else
                        {
                            const float perSectoPerMin = 60;
                            var driveWheelCircumferenceM = 2 * Math.PI * Locomotive.DriverWheelRadiusM;
                            var driveWheelRpm = Locomotive.AbsTractionSpeedMpS * perSectoPerMin / driveWheelCircumferenceM;
                            var shaftRPM = driveWheelRpm * CurrentGear.Ratio;
                            return (float)(shaftRPM);
                        }

                    }

                }
                else // Legacy operation
                {

                    if (CurrentGear == null)
                        return DieselEngine.RealRPM;
                    else
                        return CurrentSpeedMpS / CurrentGear.Ratio;
                }
            }
        }

        public bool IsOverspeedError
        {
            get
            {
                if (CurrentGear == null)
                    return false;
                else
                    return ((DieselEngine.RealRPM / DieselEngine.MaxRPM * 100f) > CurrentGear.OverspeedPercentage); 
            } 
        }

        public bool IsOverspeedWarning 
        {
            get
            {
                if (CurrentGear == null)
                    return false;
                else
                    return ((DieselEngine.RealRPM / DieselEngine.MaxRPM * 100f) > 100f); 
            }
        }

        float clutch;
        public float ClutchPercent { set { clutch = (value > 100.0f ? 100f : (value < -100f ? -100f : value)) / 100f; }
            get { return clutch * 100f; } }

        public bool AutoClutch = true;

        public bool ReverseGearBoxIndication = false;
        public TypesClutch ClutchType = TypesClutch.Unknown;
        public TypesGearBox GearBoxType = TypesGearBox.Unknown;
        public GearBoxOperation GearBoxOperation = GearBoxOperation.Manual;
        public GearBoxOperation OriginalGearBoxOperation = GearBoxOperation.Manual;
 
        public float rpmRatio;
        public float torqueCurveMultiplier;
        public float throttleFraction;

        public float tractiveForceN;
        public float TractiveForceN
        {
            get
            {
                if (CurrentGear != null)
                {

                    if (Locomotive != null && Locomotive.DieselTransmissionType == MSTSDieselLocomotive.DieselTransmissionTypes.Mechanic)
                    {

                        if (GearBoxOperation == GearBoxOperation.Automatic)
                        {
                            if (ClutchPercent >= -20)
                            {
                                tractiveForceN = DieselEngine.DieselTorqueTab[DieselEngine.RealRPM] * DieselEngine.DemandedThrottlePercent / DieselEngine.DieselTorqueTab.MaxY() * 0.01f * CurrentGear.MaxTractiveForceN;
                                if (CurrentSpeedMpS > 0)
                                {
                                    if (tractiveForceN > (DieselEngine.CurrentDieselOutputPowerW / CurrentSpeedMpS))
                                        tractiveForceN = DieselEngine.CurrentDieselOutputPowerW / CurrentSpeedMpS;
                                }
                                return tractiveForceN;
                            }
                            else
                                return -CurrentGear.CoastingForceN * (100f + ClutchPercent) / 100f;
                        }
                        else if (GearBoxOperation == GearBoxOperation.Manual)
                        {
                            // Allow rpm to go below idle for display purposes, but not for calculation - creates -ve te
                            float dieselRpM = 0;
                            if (DieselEngine.RealRPM < DieselEngine.IdleRPM)
                            {
                                dieselRpM = DieselEngine.IdleRPM;
                            }
                            else
                            {
                                dieselRpM = DieselEngine.RealRPM;
                            }

                            throttleFraction = 0;

                            if (DieselEngine.ApparentThrottleSetting < DieselEngine.DemandedThrottlePercent)
                            {
                                // Use apparent throttle when accelerating so that time delays in rpm rise and fall are used, but use demanded throttle at other times
                                //  throttleFraction = DieselEngine.ApparentThrottleSetting * 0.01f; // Convert from percentage to fraction, use the apparent throttle as this includes some delay for rpm increase

                                throttleFraction = DieselEngine.DemandedThrottlePercent * 0.01f;

                            }
                            else // As apparent throttle is related to current rpm, limit throttle to the actual demanded throttle. 
                            {
                                throttleFraction = DieselEngine.DemandedThrottlePercent * 0.01f;
                            }

                            // Limit tractive force if engine is governed, ie speed cannot exceed the governed speed or the throttled speed
                            // Diesel mechanical transmission are not "governed" at all engine speed settings, rather only at Idle and Max RpM. 
                            // (See above where DM units TE held at constant value, unless overwritten by the following)
                            if (Locomotive.DieselTransmissionType == MSTSDieselLocomotive.DieselTransmissionTypes.Mechanic)
                            {
                                // If engine RpM exceeds maximum rpm
                                if (DieselEngine.GovernorEnabled && DieselEngine.DemandedThrottlePercent > 0 && DieselEngine.RealRPM > DieselEngine.MaxRPM)
                                {
                                    var decayGradient = 1.0f / (DieselEngine.GovernorRPM - DieselEngine.MaxRPM);
                                    var rpmOverRun = (DieselEngine.RealRPM - DieselEngine.MaxRPM);
                                    throttleFraction = (1.0f - (decayGradient * rpmOverRun)) * throttleFraction;
                                    throttleFraction = MathHelper.Clamp(throttleFraction, 0.0f, 1.0f);  // Clamp throttle setting within bounds, so it doesn't go negative                           
                                }

                                // If engine RpM drops below idle rpm
                                if (DieselEngine.GovernorEnabled && DieselEngine.DemandedThrottlePercent > 0 && DieselEngine.RealRPM < DieselEngine.IdleRPM)
                                {
                                    var decayGradient = 1.0f / (DieselEngine.IdleRPM - DieselEngine.StartingRPM);
                                    var rpmUnderRun = DieselEngine.IdleRPM - DieselEngine.RealRPM;
                                    throttleFraction = decayGradient * rpmUnderRun + throttleFraction; // Increases throttle over current setting up to a maximum of 100%
                                    throttleFraction = MathHelper.Clamp(throttleFraction, 0.0f, 1.0f);  // Clamp throttle setting within bounds, so it doesn't go negative
                                }
                            }

                            // A torque vs rpm family of curves has been built based on the information on this page
                            // https://www.cm-labs.com/vortexstudiodocumentation/Vortex_User_Documentation/Content/Editor/editor_vs_configure_engine.html
                            //
                            // Calculate torque curve for throttle position and RpM
                            rpmRatio = (dieselRpM - DieselEngine.IdleRPM) / (DieselEngine.MaxRPM - DieselEngine.IdleRPM);
                            torqueCurveMultiplier = (0.824f * throttleFraction + 0.176f) + (0.785f * throttleFraction - 0.785f) * rpmRatio;

                            // During normal operation fuel admission is fixed, and therefore TE follows curve as RpM varies
                            tractiveForceN = torqueCurveMultiplier * DieselEngine.DieselTorqueTab[DieselEngine.RealRPM] / DieselEngine.DieselTorqueTab.MaxY() * CurrentGear.MaxTractiveForceN;

                            Locomotive.HuDGearMaximumTractiveForce = CurrentGear.MaxTractiveForceN;

                            if (CurrentSpeedMpS > 0)
                            {
                                var tractiveEffortLimitN = (DieselEngine.DieselPowerTab[DieselEngine.RealRPM] * (DieselEngine.LoadPercent / 100f)) / CurrentSpeedMpS;

                                if (tractiveForceN > tractiveEffortLimitN )
                                {
                                    tractiveForceN = tractiveEffortLimitN;
                                }
                            }

                            // Set TE to zero if gear change happening && type B gear box
                            if (ManualGearBoxChangeOn && GearBoxType == TypesGearBox.B)
                            {
                                tractiveForceN = 0;
                            }

                            // Scoop couplings prevent TE "creep" at zero throttle
                            if (throttleFraction == 0 && DieselEngine.RealRPM < 1.05f * DieselEngine.IdleRPM && ClutchType == TypesClutch.Scoop)
                            {
                                tractiveForceN = 0;
                            }

                            // if freewheeling set TE to zero
                            if (GearBoxFreeWheelEnabled)
                            {
                                tractiveForceN = 0;
                            }

                            // Calculate tractive force if engine shuts down due to under or overspeed
                            // For engines when clutch is on calculate drag of "stalled" engine on locomotive. This will be maximum zero throttle @ GovernorRpM scaled back to 0 TE at 0 rpm.
                            if (DieselEngine.GearOverspeedShutdownEnabled || DieselEngine.GearUnderspeedShutdownEnabled)
                            {
                                if (IsClutchOn)
                                {
                                    var tempRpmRatio = (dieselRpM - DieselEngine.IdleRPM) / (DieselEngine.MaxRPM - DieselEngine.IdleRPM);
                                    var stallThrottleFraction = 0; // when stalled throttle fraction will be zero
                                    var stallTorqueCurveMultiplier = (0.824f * stallThrottleFraction + 0.176f) + (0.785f * stallThrottleFraction - 0.785f) * rpmRatio;

                                    // During normal operation fuel admission is fixed, and therefore TE follows curve as RpM varies
                                    var maxStallEngineTE = stallTorqueCurveMultiplier * DieselEngine.DieselTorqueTab[DieselEngine.RealRPM] / DieselEngine.DieselTorqueTab.MaxY() * CurrentGear.MaxTractiveForceN;

                                    var stallGradient = maxStallEngineTE / DieselEngine.GovernorRPM;

                                    tractiveForceN = stallGradient * dieselRpM;

                                }
                                else
                                {
                                    tractiveForceN = CurrentGear.CoastingForceN;
                                }

                            }
                            return tractiveForceN;
                        }
                        else
                            return 0;
                    }
                    else
                    {                                               
                        if (ClutchPercent >= -20)
                        {
                            float tractiveForceN = DieselEngine.DieselTorqueTab[DieselEngine.RealRPM] * DieselEngine.DemandedThrottlePercent / DieselEngine.DieselTorqueTab.MaxY() * 0.01f * CurrentGear.MaxTractiveForceN;
                            if (CurrentSpeedMpS > 0)
                            {
                                if (tractiveForceN > (DieselEngine.CurrentDieselOutputPowerW / CurrentSpeedMpS))
                                    tractiveForceN = DieselEngine.CurrentDieselOutputPowerW / CurrentSpeedMpS;
                            }
                            return tractiveForceN;
                        }
                        else
                            return -CurrentGear.CoastingForceN * (100f + ClutchPercent) / 100f;
                    }
                }
                else
                    return 0;
            }
        }

        public GearBox(DieselEngine de)
        {
            DieselEngine = de;
            Locomotive = de.Locomotive;
        }

        public void Copy(GearBox copy)
        {
            // Nothing to copy, all parameters will be copied from MSTSGearBoxParams at initialization
        }

        public void Restore(BinaryReader inf)
        {
            currentGearIndex = inf.ReadInt32();
            nextGearIndex = inf.ReadInt32();
            gearedUp = inf.ReadBoolean();
            gearedDown = inf.ReadBoolean();
            clutchOn = inf.ReadBoolean();
            clutch = inf.ReadSingle();
            ManualGearDown = inf.ReadBoolean();
            ManualGearUp = inf.ReadBoolean();
            ManualGearChange = inf.ReadBoolean();
            ManualGearTimerS = inf.ReadSingle();
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write(currentGearIndex);
            outf.Write(nextGearIndex);
            outf.Write(gearedUp);
            outf.Write(gearedDown);
            outf.Write(clutchOn);
            outf.Write(clutch);
            outf.Write(ManualGearDown);
            outf.Write(ManualGearUp);
            outf.Write(ManualGearChange);
            outf.Write(ManualGearTimerS);
        }

        public void Initialize()
        {
            if (GearBoxParams != null)
            {
                if ((!GearBoxParams.IsInitialized) && (GearBoxParams.AtLeastOneParamFound))
                    Trace.TraceWarning("Some of the gearbox parameters are missing! Default physics will be used.");

                ReverseGearBoxIndication = GearBoxParams.ReverseGearBoxIndication;
                GearBoxType = GearBoxParams.GearBoxType;
                ClutchType = GearBoxParams.ClutchType;
                GearBoxFreeWheelFitted = GearBoxParams.FreeWheelFitted;

                for (int i = 0; i < GearBoxParams.GearBoxNumberOfGears; i++)
                {
                    Gears.Add(new Gear(this));
                    Gears[i].BackLoadForceN = GearBoxParams.GearBoxBackLoadForceN;
                    Gears[i].CoastingForceN = GearBoxParams.GearBoxCoastingForceN;
                    Gears[i].DownGearProportion = GearBoxParams.GearBoxDownGearProportion;
                    Gears[i].IsDirectDriveGear = (GearBoxParams.GearBoxDirectDriveGear == GearBoxParams.GearBoxNumberOfGears);
                    Gears[i].MaxSpeedMpS = GearBoxParams.GearBoxMaxSpeedForGearsMpS[i];
                    // Maximum torque (tractive effort) actually occurs at less then the maximum engine rpm, so this section uses either 
                    // the TE at gear maximum speed, or if the user has entered the maximum TE
                    if (!GearBoxParams.MaxTEFound)
                    {
                        // If user has entered this value then assume that they have already put the maximum torque value in
                        Gears[i].MaxTractiveForceN = GearBoxParams.GearBoxMaxTractiveForceForGearsN[i] / Locomotive.DieselEngines.Count;

                        // For purposes of calculating engine efficiency the tractive force at maximum gear speed needs to be used.
                        Gears[i].TractiveForceatMaxSpeedN = (GearBoxParams.GearBoxMaxTractiveForceForGearsN[i] / (DieselEngine.DieselTorqueTab.MaxY() / DieselEngine.DieselTorqueTab[DieselEngine.MaxRPM])) / Locomotive.DieselEngines.Count;
                    }
                    else
                    {
                        // if they entered the TE at maximum gear speed, then increase the value accordingly 
                        Gears[i].MaxTractiveForceN = (GearBoxParams.GearBoxTractiveForceAtSpeedN[i] * DieselEngine.DieselTorqueTab.MaxY() / DieselEngine.DieselTorqueTab[DieselEngine.MaxRPM]) / Locomotive.DieselEngines.Count;

                        // For purposes of calculating engine efficiency the tractive force at maximum gear speed needs to be used.
                        Gears[i].TractiveForceatMaxSpeedN = GearBoxParams.GearBoxTractiveForceAtSpeedN[i] / Locomotive.DieselEngines.Count;
                    }                        

                    Gears[i].OverspeedPercentage = GearBoxParams.GearBoxOverspeedPercentageForFailure;
                    Gears[i].UpGearProportion = GearBoxParams.GearBoxUpGearProportion;
                    if (Locomotive != null && Locomotive.DieselTransmissionType == MSTSDieselLocomotive.DieselTransmissionTypes.Mechanic)
                    {
                        // Calculate gear ratio, based on premise that drive wheel rpm @ max speed will be when engine is operating at max rpm
                        var driveWheelCircumferenceM = 2 * Math.PI * Locomotive.DriverWheelRadiusM;
                        var driveWheelRpm = pS.TopM(Gears[i].MaxSpeedMpS) / driveWheelCircumferenceM;
                        float apparentGear = (float)(DieselEngine.MaxRPM / driveWheelRpm);

                        Gears[i].Ratio = apparentGear;

                        Gears[i].BackLoadForceN = Gears[i].Ratio * GearBoxParams.GearBoxBackLoadForceN;
                        Gears[i].CoastingForceN = Gears[i].Ratio * GearBoxParams.GearBoxCoastingForceN;

                        Gears[i].ChangeUpSpeedRpM = DieselEngine.MaxRPM;

                        Gears[0].ChangeDownSpeedRpM = DieselEngine.IdleRPM;

                        if (i > 0)
                        {
                            driveWheelRpm = pS.TopM(Gears[i - 1].MaxSpeedMpS) / driveWheelCircumferenceM;
                            Gears[i].ChangeDownSpeedRpM = (float)driveWheelRpm * Gears[i].Ratio;
                        }
                    }
                    else
                    {
                        Gears[i].Ratio = GearBoxParams.GearBoxMaxSpeedForGearsMpS[i] / DieselEngine.MaxRPM;
                    }
                }
                GearBoxOperation = GearBoxParams.GearBoxOperation;
                OriginalGearBoxOperation = GearBoxParams.GearBoxOperation;
            }
        }

        public void InitializeMoving()
        {
            for (int iGear = 0; iGear < Gears.Count; iGear++)
            {
                if (Gears[iGear].MaxSpeedMpS < CurrentSpeedMpS) continue;
                else currentGearIndex = nextGearIndex = iGear;
                break;
             } 

            gearedUp = false;
            gearedDown = false;
            clutchOn = true;
            clutch = 0.4f;
            DieselEngine.RealRPM = ShaftRPM;
        }

        public void Update(float elapsedClockSeconds)
        {
            if (Locomotive != null && Locomotive.DieselTransmissionType == MSTSDieselLocomotive.DieselTransmissionTypes.Mechanic)
            {
                if (GearBoxOperation == GearBoxOperation.Automatic || GearBoxOperation == GearBoxOperation.Semiautomatic)
                {

                    if ((clutch <= 0.05) || (clutch >= 1f))
                    {

                        if (currentGearIndex < nextGearIndex)
                        {
                            DieselEngine.Locomotive.SignalEvent(Event.GearUp);
                            currentGearIndex = nextGearIndex;
                        }
                    }
                    if ((clutch <= 0.05) || (clutch >= 0.5f))
                    {
                        if (currentGearIndex > nextGearIndex)
                        {
                            DieselEngine.Locomotive.SignalEvent(Event.GearDown);
                            currentGearIndex = nextGearIndex;
                        }
                    }
                }
                else if (GearBoxOperation == GearBoxOperation.Manual)
                {

                    if (ManualGearUp)
                    {

                        if (currentGearIndex < nextGearIndex)
                        {
                            DieselEngine.Locomotive.SignalEvent(Event.GearUp);
                            currentGearIndex = nextGearIndex;
                            ManualGearUp = false;
                        }
                    }

                    if (ManualGearDown)
                    {
                        if (currentGearIndex > nextGearIndex)
                        {
                            DieselEngine.Locomotive.SignalEvent(Event.GearDown);
                            currentGearIndex = nextGearIndex;
                            ManualGearDown = false;
                        }
                    }
                }

            }
            else // Legacy operation
            {
                if ((clutch <= 0.05) || (clutch >= 1f))
                {
                    if (currentGearIndex < nextGearIndex)
                    {
                        DieselEngine.Locomotive.SignalEvent(Event.GearUp);
                        currentGearIndex = nextGearIndex;
                    }
                }
                if ((clutch <= 0.05) || (clutch >= 0.5f))
                {
                    if (currentGearIndex > nextGearIndex)
                    {
                        DieselEngine.Locomotive.SignalEvent(Event.GearDown);
                        currentGearIndex = nextGearIndex;
                    }
                }
            }

            if (DieselEngine.State == DieselEngineState.Running)
            {
                switch (GearBoxOperation)
                {
                    case GearBoxOperation.Manual:
                        if (DieselEngine.Locomotive.ThrottlePercent == 0 && Locomotive.AbsTractionSpeedMpS == 0)
                        {
                            clutchOn = false;
                            ClutchPercent = 0f;
                        }
                        break;
                    case GearBoxOperation.Automatic:
                    case GearBoxOperation.Semiautomatic:
                        if ((CurrentGear != null))
                        {
                            if ((CurrentSpeedMpS > (DieselEngine.MaxRPM * CurrentGear.UpGearProportion * CurrentGear.Ratio)))// && (!GearedUp) && (!GearedDown))
                                AutoGearUp();
                            else
                            {
                                if ((CurrentSpeedMpS < (DieselEngine.MaxRPM * CurrentGear.DownGearProportion * CurrentGear.Ratio)))// && (!GearedUp) && (!GearedDown))
                                    AutoGearDown();
                                else
                                    AutoAtGear();
                            }
                            if (DieselEngine.Locomotive.ThrottlePercent == 0)
                            {
                                if ((CurrentGear != null) || (NextGear == null))
                                {
                                    nextGearIndex = -1;
                                    currentGearIndex = -1;
                                    clutchOn = false;
                                    gearedDown = false;
                                    gearedUp = false;
                                }

                            }
                        }
                        else
                        {
                            if ((DieselEngine.Locomotive.ThrottlePercent > 0))
                                AutoGearUp();
                            else
                            {
                                nextGearIndex = -1;
                                currentGearIndex = -1;
                                clutchOn = false;
                                gearedDown = false;
                                gearedUp = false;
                            }
                        }
                        break;
                }
            }
            // If diesel engine is stopped (potentially after a stall) on a manual gearbox then allow gears to be changed
            else if (DieselEngine.State == DieselEngineState.Stopped)
            {
                switch (GearBoxOperation)
                {
                    case GearBoxOperation.Manual:
                        if (Locomotive.AbsSpeedMpS < 0.05)
                        {
                            clutchOn = false;
                            ClutchPercent = 0f;
                        }
                        break;
                }
            }
            else
            {
                nextGearIndex = -1;
                currentGearIndex = -1;
                clutchOn = false;
                gearedDown = false;
                gearedUp = false;
            }
        }
    }

    public enum TypesClutch
    {
        Unknown,
        Friction,
        Fluid,
        Scoop
    }

    public enum TypesGearBox
    {
        Unknown,
        A,
        B,
        C,
        D
    }

    public enum GearBoxOperation
    {
        Manual,
        Automatic,
        Semiautomatic
    }

    public enum GearBoxEngineBraking
    {
        None,
        DirectDrive,
        AllGears
    }

    public class Gear
    {
        public bool IsDirectDriveGear;
        public float MaxSpeedMpS;
        public float ChangeUpSpeedRpM;
        public float ChangeDownSpeedRpM;
        public float MaxTractiveForceN;
        public float TractiveForceatMaxSpeedN;
        public float OverspeedPercentage;
        public float BackLoadForceN;
        public float CoastingForceN;
        public float UpGearProportion;
        public float DownGearProportion;

        public float Ratio = 1f;

        protected readonly GearBox GearBox;

        public Gear(GearBox gb) { GearBox = gb; }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
