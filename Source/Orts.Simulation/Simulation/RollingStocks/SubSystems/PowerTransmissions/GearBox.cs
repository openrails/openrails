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

        public TypesGearBox  GearBoxType = TypesGearBox.A;
        // GearboxType ( A ) - power is continuous during gear changes (and throttle does not need to be adjusted) - this is the MSTS legacy operation so possibly needs to be the default.
        // GearboxType ( B ) - power is interrupted during gear changes - but the throttle does not need to be adjusted when changing gear
        // GearboxType ( C ) - power is interrupted and if GearboxOperation is Manual throttle must be closed when changing gear
        // GearboxType ( D ) - power is interrupted and if GearboxOperation is Manual throttle must be closed when changing gear, clutch will remain engaged, and can stall engine

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
            initLevel = copy.initLevel;
        }
    }

    public class GearBox : ISubSystem<GearBox>
    {
        protected readonly DieselEngine DieselEngine;
        protected readonly MSTSDieselLocomotive Locomotive;
        protected MSTSGearBoxParams GearBoxParams => Locomotive.DieselEngines.MSTSGearBoxParams;
        public List<Gear> Gears = new List<Gear>();

        public float ManualGearTimerResetS = 2;  // Allow gear change to take 2 seconds
        public float ManualGearTimerS; // Time for gears to change
        public bool ManualGearBoxChangeOn = false;
        public bool ManualGearUp = false;
        public bool ManualGearDown = false;
        public bool gearRestore = false;
        public int restoreCurrentGearIndex;
        
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

        public bool gearedUp;
        public bool gearedDown;
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
                    if (DieselEngine.Locomotive.ThrottlePercent == 0 && !clutchOn)
                    {
                        return clutchOn;
                    }

                    // Set clutch engaged when shaftrpm and engine rpm are equal
                    if (DieselEngine.Locomotive.ThrottlePercent > 0)
                    {
                        if (ShaftRPM >= DieselEngine.RealRPM)
                            clutchOn = true;
                    }

                    // Set clutch disengaged (slip mode) if shaft rpm drops below idle speed (on type A, B and C clutches), Type D will not slip unless put into neutral
                    if (ShaftRPM <= DieselEngine.IdleRPM && GearBoxType != TypesGearBox.D)
                        clutchOn = false;

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
                    if (ShaftRPM > DieselEngine.GovenorRPM + 2)
                    {
                        temp = DieselEngine.GovenorRPM + 2;
                    }
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

        public float clutch;
        public float ClutchPercent { set { clutch = (value > 100.0f ? 100f : (value < -100f ? -100f : value)) / 100f; }
            get { return clutch * 100f; } }

        public bool AutoClutch = true;

        public TypesGearBox GearBoxType = TypesGearBox.A;
        public GearBoxOperation GearBoxOperation = GearBoxOperation.Manual;
        public GearBoxOperation OriginalGearBoxOperation = GearBoxOperation.Manual;

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
                    else if (GearBoxOperation == GearBoxOperation.Manual)
                    {

                        if (GearBoxOperation == GearBoxOperation.Manual || (GearBoxOperation != GearBoxOperation.Manual && ClutchPercent >= -20))
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
                            float tractiveForceN = DieselEngine.DieselTorqueTab[dieselRpM] / DieselEngine.DieselTorqueTab.MaxY() * CurrentGear.MaxTractiveForceN;

                            Locomotive.HuDGearMaximumTractiveForce = CurrentGear.MaxTractiveForceN;

                            // Limit tractive force if engine is governed, ie speed cannot exceed the governed speed or the throttled speed
                           
                            if ((DieselEngine.RealRPM >= DieselEngine.GovenorRPM && ShaftRPM > DieselEngine.GovenorRPM) || (DieselEngine.RealRPM < DieselEngine.GovenorRPM && DieselEngine.RealRPM > DieselEngine.ThrottleRPMTab[DieselEngine.DemandedThrottlePercent]))
                            {
                                // use decay function to decrease tractive effort if RpM exceeds governed RpM value.
                                // y = original amount ( 1 - decay rate)^length of prediction
                                float decayRpM = 0;

                                if (DieselEngine.RealRPM < DieselEngine.GovenorRPM && DieselEngine.RealRPM > DieselEngine.ThrottleRPMTab[DieselEngine.DemandedThrottlePercent])
                                {
                                    decayRpM = ShaftRPM - DieselEngine.ThrottleRPMTab[DieselEngine.DemandedThrottlePercent];
                                }
                                else
                                {
                                    decayRpM = ShaftRPM - DieselEngine.GovenorRPM;
                                }
                                
                                var teDecline = Math.Pow((1.0f - 0.05f), decayRpM);

                                tractiveForceN = (float)Math.Abs(CurrentGear.MaxTractiveForceN * teDecline);
                                tractiveForceN = MathHelper.Clamp(tractiveForceN, 0.0f, CurrentGear.MaxTractiveForceN);  // Clamp tractive effort so that it doesn't go below zero
                            }

                        // Trace.TraceInformation("Geared Tractive Effort #1 - Driving - TE: {0} lbf, RpM: {1}, Torque: {2} lb-ft, Throttle%: {3}, MaxTorque: {4} lb-ft, MaxTE {5} lbf, Speed: {6} mph, Clutch {7}, Gear: {8} IsClutchOn {9} Shaft RpM {10} DemandedRpM {11}", tractiveForceN * 0.224809f, DieselEngine.RealRPM, DieselEngine.DieselTorqueTab[DieselEngine.RealRPM] * 0.737562f, DieselEngine.DemandedThrottlePercent, DieselEngine.DieselTorqueTab.MaxY(), CurrentGear.MaxTractiveForceN * 0.224809f, CurrentSpeedMpS * 2.23694f, ClutchPercent, Locomotive.GearBoxController.CurrentNotch, IsClutchOn, ShaftRPM, DieselEngine.DemandedRPM);

                            if (CurrentSpeedMpS > 0)
                            {
                                if (tractiveForceN > (DieselEngine.RailPowerTab[DieselEngine.RealRPM] / CurrentSpeedMpS))
                                {
                                    tractiveForceN = DieselEngine.RailPowerTab[DieselEngine.RealRPM] / CurrentSpeedMpS;
                               //     Trace.TraceInformation("Power Reduction - RailPower {0} Speed {1} tractiveForce {2} Calculated {3} rpM {4}", DieselEngine.RailPowerTab[DieselEngine.RealRPM], CurrentSpeedMpS, tractiveForceN, DieselEngine.RailPowerTab[DieselEngine.RealRPM] / CurrentSpeedMpS, DieselEngine.RealRPM);
                                }

                            }

                            // Set TE to zero if gear change happening && type B gear box
                            if (ManualGearBoxChangeOn && GearBoxType == TypesGearBox.B)
                            {
                                tractiveForceN = 0;
                            }

                            return tractiveForceN;

                        }
                        else
                        {
                            if (CurrentSpeedMpS > 0)
                                return -CurrentGear.CoastingForceN * (100f + ClutchPercent) / 100f;
                            else if (CurrentSpeedMpS < 0)
                                return CurrentGear.CoastingForceN * (100f + ClutchPercent) / 100f;
                            else
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
            // Restored in diesel engines
        }

        public void Save(BinaryWriter outf)
        {
            // Saved in diesel engine
        }

        public void Initialize()
        {
            if (GearBoxParams != null)
            {
                if ((!GearBoxParams.IsInitialized) && (GearBoxParams.AtLeastOneParamFound))
                    Trace.TraceWarning("Some of the gearbox parameters are missing! Default physics will be used.");

                GearBoxType = GearBoxParams.GearBoxType;

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
                        if (DieselEngine.Locomotive.ThrottlePercent == 0) // Manual gearboxes
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
