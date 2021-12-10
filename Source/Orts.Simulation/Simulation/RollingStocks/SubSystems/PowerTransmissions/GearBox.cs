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

namespace Orts.Simulation.RollingStocks.SubSystems.PowerTransmissions
{
    public class MSTSGearBoxParams
    {
        public int GearBoxNumberOfGears = 1;
        public int GearBoxDirectDriveGear = 1;
        public bool FreeWheelFitted = false;

        public TypesGearBox  GearBoxType = TypesGearBox.A;
        // GearboxType ( A ) - power is continuous during gear changes (and throttle does not need to be adjusted) - this is the MSTS legacy operation so possibly needs to be the default.
        // GearboxType ( B ) - power is interrupted during gear changes - but the throttle does not need to be adjusted when changing gear
        // GearboxType ( C ) - power is interrupted and if GearboxOperation is Manual throttle must be closed when changing gear
        // GearboxType ( D ) - power is interrupted and if GearboxOperation is Manual throttle must be closed when changing gear, clutch will remain engaged, and can stall engine

        public TypesClutch ClutchType = TypesClutch.Friction;

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
        public float GearBoxUpGearProportion = 1.0f;
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
                case "engine(gearboxdirectdrivegear": GearBoxDirectDriveGear = stf.ReadIntBlock(1); break; // initLevel++; break;
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
                        GearBoxType = (TypesGearBox)Enum.Parse(typeof(TypesGearBox), gearType);
                    }
                    catch
                    {
                        STFException.TraceWarning(stf, "Assumed unknown gear type " + gearType);
                    }
                    break;
                case "engine(ortsmainclutchtype":
                    stf.MustMatch("(");
                    var clutchType = stf.ReadString();
                    try
                    {
                        ClutchType = (TypesClutch)Enum.Parse(typeof(TypesClutch), clutchType);
                    }
                    catch
                    {
                        STFException.TraceWarning(stf, "Assumed unknown gear type " + clutchType);
                    }
                    break;
                case "engine(gearboxoperation":
                    stf.MustMatch("(");
                    var gearOperation = stf.ReadString();
                    try
                    {
                        GearBoxOperation = (GearBoxOperation)Enum.Parse(typeof(GearBoxOperation), gearOperation);
                    }
                    catch
                    {
                        STFException.TraceWarning(stf, "Assumed unknown gear operation type " + gearOperation);
                    }
                    initLevel++;
                    break;
                case "engine(gearboxenginebraking":
                    stf.MustMatch("(");
                    var engineBraking = stf.ReadString();
                    try
                    {
                        GearBoxEngineBraking = (GearBoxEngineBraking)Enum.Parse(typeof(GearBoxEngineBraking), engineBraking);
                    }
                    catch
                    {
                        STFException.TraceWarning(stf, "Assumed unknown gear operation type " + engineBraking);
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
            GearBoxDirectDriveGear = copy.GearBoxDirectDriveGear;
            GearBoxType = copy.GearBoxType;
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

        public float previousThrottleSetting;
        public float previousRpM;

        public float ManualGearTimerResetS = 2;  // Allow gear change to take 2 seconds
        public float ManualGearTimerS; // Time for gears to change
        public bool ManualGearBoxChangeOn = false;
        public bool ManualGearUp = false;
        public bool ManualGearDown = false;
        
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
        }


        public Gear NextGear 
        {
            get
            {
                if ((nextGearIndex >= 0)&&(nextGearIndex < NumOfGears))
                    return Gears[nextGearIndex];
                else
                    return null;
            }
            set
            {
                switch(GearBoxOperation)
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

                    // Set clutch engaged when shaftrpm and engine rpm are equal
                    if ((DieselEngine.Locomotive.ThrottlePercent >= 0 || DieselEngine.Locomotive.SpeedMpS > 0) && CurrentGear != null)
                    {
                        var clutchEngagementBandwidthRPM = 10.0f;
                        if (ShaftRPM >= DieselEngine.RealRPM - clutchEngagementBandwidthRPM && ShaftRPM < DieselEngine.RealRPM + clutchEngagementBandwidthRPM && ShaftRPM < DieselEngine.MaxRPM && ShaftRPM > DieselEngine.IdleRPM)
                            clutchOn = true;
                        return clutchOn;
                    }
                    else if ((ClutchType == TypesClutch.Scoop || ClutchType == TypesClutch.Fluid) && CurrentGear == null )
                    {
                        clutchOn = false;
                        return clutchOn;
                    }

                    if (DieselEngine.Locomotive.ThrottlePercent == 0 && !clutchOn)
                    {
                        clutchOn = false;
                        return clutchOn;
                    }

                    if (ManualGearBoxChangeOn)
                    {
                        clutchOn = false;
                        return clutchOn;
                    }

                    // Set clutch disengaged (slip mode) if shaft rpm moves outside of acceptable bandwidth speed (on type A, B and C clutches), Type D will not slip unless put into neutral
                    var clutchSlipBandwidth = 0.1f * DieselEngine.ThrottleRPMTab[DieselEngine.demandedThrottlePercent]; // Bandwidth 10%
                    var speedVariationRpM = Math.Abs(DieselEngine.ThrottleRPMTab[DieselEngine.demandedThrottlePercent] - ShaftRPM);
                    if (GearBoxFreeWheelFitted && speedVariationRpM > clutchSlipBandwidth && ( GearBoxType != TypesGearBox.D || GearBoxType != TypesGearBox.A ))
                    {
                        clutchOn = false;
                        return clutchOn;
                    }
                    return clutchOn;
                }
            }
        }

        public int NumOfGears { get { return Gears.Count; } }

        public float CurrentSpeedMpS 
        {
            get
            {
                if(DieselEngine.Locomotive.Direction == Direction.Reverse)
                    return -(DieselEngine.Locomotive.SpeedMpS);
                else
                    return (DieselEngine.Locomotive.SpeedMpS);
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
                if (CurrentGear == null)
                    return DieselEngine.RealRPM;
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
                        var driveWheelRpm = Locomotive.AbsSpeedMpS * perSectoPerMin / driveWheelCircumferenceM;
                        var shaftRPM = driveWheelRpm * CurrentGear.Ratio;
                        return (float)(shaftRPM);
                    }
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

        public TypesClutch ClutchType = TypesClutch.Friction;
        public TypesGearBox GearBoxType = TypesGearBox.A;
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

                        if (GearBoxOperation == GearBoxOperation.Manual)
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

                            if (ShaftRPM != dieselRpM && !IsClutchOn && DieselEngine.ApparentThrottleSetting < DieselEngine.DemandedThrottlePercent)
                            {
                                // Use apparent throttle when accelerating, but use demanded throttle at other times????
                                throttleFraction = DieselEngine.ApparentThrottleSetting * 0.01f; // Convert from percentage to fraction, use the apparent throttle as this includes some delay for rpm increase
                            }
                            else
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
                                //    Trace.TraceInformation("Governor - throttle {0} Demand {1} Grad {2} Real {3} Max {4} GovRpM {5} Over {6}", throttleFraction, DieselEngine.DemandedThrottlePercent, decayGradient, DieselEngine.RealRPM, DieselEngine.MaxRPM, DieselEngine.GovernorRPM, rpmOverRun);
                                }

                                // If engine RpM drops below idle rpm
                                if (DieselEngine.GovernorEnabled && DieselEngine.DemandedThrottlePercent > 0 && DieselEngine.RealRPM < DieselEngine.IdleRPM)
                                {
                                    var decayGradient = 1.0f / (DieselEngine.IdleRPM - DieselEngine.StartingRPM);
                                    var rpmUnderRun = DieselEngine.IdleRPM - DieselEngine.RealRPM;
                                    throttleFraction = decayGradient * rpmUnderRun + throttleFraction; // Increases throttle over current setting up to a maximum of 100%
                                    throttleFraction = MathHelper.Clamp(throttleFraction, 0.0f, 1.0f);  // Clamp throttle setting within bounds, so it doesn't go negative
                                //    Trace.TraceInformation("Governor Up - throttle {0} Demand {1} Grad {2} Real {3} Start {4} GovRpM {5} Under {6}", throttleFraction, DieselEngine.DemandedThrottlePercent, decayGradient, DieselEngine.RealRPM, DieselEngine.StartingRPM, DieselEngine.GovernorRPM, rpmUnderRun);
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

//                            Trace.TraceInformation("Tractive Force {0}, Throttle {1} TCM {2} RpM {3} Throttle% {4} Gear {5} Torque {6} MaxTor {7} MaxTE {8}", tractiveForceN, throttleFraction, torqueCurveMultiplier, dieselRpM, DieselEngine.DemandedThrottlePercent, DieselEngine.GearBox.CurrentGearIndex + 1, DieselEngine.DieselTorqueTab[DieselEngine.RealRPM], DieselEngine.DieselTorqueTab.MaxY(), CurrentGear.MaxTractiveForceN);

                            Locomotive.HuDGearMaximumTractiveForce = CurrentGear.MaxTractiveForceN;
                                                        
//                            Trace.TraceInformation("Geared Tractive Effort #1 - Driving - TE: {0} lbf, RpM: {1}, Torque: {2} lb-ft, Throttle%: {3}, MaxTorque: {4} lb-ft, MaxTE {5} lbf, Speed: {6} mph, Clutch {7}, Gear: {8} IsClutchOn {9} Shaft RpM {10} DemandedRpM {11} rpmRatio {12} TCM {13}", tractiveForceN * 0.224809f, DieselEngine.RealRPM, DieselEngine.DieselTorqueTab[DieselEngine.RealRPM] * 0.737562f, DieselEngine.DemandedThrottlePercent, DieselEngine.DieselTorqueTab.MaxY(), CurrentGear.MaxTractiveForceN * 0.224809f, CurrentSpeedMpS * 2.23694f, ClutchPercent, Locomotive.GearBoxController.CurrentNotch, IsClutchOn, ShaftRPM, DieselEngine.DemandedRPM, rpmRatio, torqueCurveMultiplier);

                            if (CurrentSpeedMpS > 0)
                            {
                                if (tractiveForceN > (DieselEngine.RailPowerTab[DieselEngine.RealRPM] / CurrentSpeedMpS))
                                {
                                    tractiveForceN = DieselEngine.RailPowerTab[DieselEngine.RealRPM] / CurrentSpeedMpS;
                                }

                            }

                            // Set TE to zero if gear change happening && type B gear box
                            if (ManualGearBoxChangeOn && GearBoxType == TypesGearBox.B)
                            {
                                tractiveForceN = 0;
                            }

                            // Scoop couplings prevent TE "creep" at zero throttle
                            if (throttleFraction == 0 && ClutchType == TypesClutch.Scoop)
                            {
                                tractiveForceN = 0;
                            }
                            
                            return tractiveForceN;
                        }
                        else
                        {
                            return 0;
                        }
                    }
                    else
                        return 0;
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

                GearBoxType = GearBoxParams.GearBoxType;
                ClutchType = GearBoxParams.ClutchType;
                GearBoxFreeWheelFitted = GearBoxParams.FreeWheelFitted;

                for (int i = 0; i < GearBoxParams.GearBoxNumberOfGears; i++)
                {
                    Gears.Add(new Gear(this));
                    Gears[i].DownGearProportion = GearBoxParams.GearBoxDownGearProportion;
                    Gears[i].IsDirectDriveGear = (GearBoxParams.GearBoxDirectDriveGear == GearBoxParams.GearBoxNumberOfGears);
                    Gears[i].MaxSpeedMpS = GearBoxParams.GearBoxMaxSpeedForGearsMpS[i];

                    // Maximum torque (tractive effort) actually occurs at less then the maximum engine rpm, so this section uses either 
                    // the TE at gear maximum speed, or if the user has entered the maximum TE
                    if (!GearBoxParams.MaxTEFound)
                    {
                        // If user has entered this value then assume that they have already put the maximum torque value in
                        Gears[i].MaxTractiveForceN = GearBoxParams.GearBoxMaxTractiveForceForGearsN[i];
                    }
                    else
                    {
                        // if they entered the TE at maximum gear speed, then increase the value accordingly 
                        Gears[i].MaxTractiveForceN = GearBoxParams.GearBoxTractiveForceAtSpeedN[i] * 1.234f;
                    }
                    Gears[i].OverspeedPercentage = GearBoxParams.GearBoxOverspeedPercentageForFailure;
                    Gears[i].UpGearProportion = GearBoxParams.GearBoxUpGearProportion;

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

            if (DieselEngine.State == DieselEngineState.Running)
            {
                switch (GearBoxOperation)
                {
                    case GearBoxOperation.Manual:
                        if (DieselEngine.Locomotive.ThrottlePercent == 0 && Locomotive.AbsSpeedMpS == 0) // Manual gearboxes
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
                        clutchOn = false;
                        ClutchPercent = 0f;
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
        Friction,
        Fluid,
        Scoop
    }

    public enum TypesGearBox
    {
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
