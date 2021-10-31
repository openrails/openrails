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

        // GearboxType ( 1 ) - power is continuous during gear changes (and throttle does not need to be adjusted) - this is the MSTS legacy operation so possibly needs to be the default.
        // GearboxType ( 2 ) - power is interrupted during gear changes - but the throttle does not need to be adjusted when changing gear
        // GearboxType ( 3 ) - power is interrupted and if GearboxOperation is Manual throttle must be closed when changing gear - see note on Wilson gearbox question below
        public int GearBoxType = 1;  
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

        public bool IsInitialized { get { return initLevel >= 5; } }
        public bool AtLeastOneParamFound { get { return initLevel >= 1; } }

        public void Parse(string lowercasetoken, STFReader stf)
        {
            string temp = "";
            switch (lowercasetoken)
            {
                case "engine(gearboxnumberofgears": GearBoxNumberOfGears = stf.ReadIntBlock(1); initLevel++; break;
                case "engine(gearboxdirectdrivegear": GearBoxDirectDriveGear = stf.ReadIntBlock(1); break; // initLevel++; break;
                case "engine(ortsgearboxtype": GearBoxType = stf.ReadIntBlock(1); Trace.TraceInformation("Read - {0}", GearBoxType); break; // default = 1
                case "engine(gearboxoperation":
                    temp = stf.ReadStringBlock("manual");
                    switch (temp)
                    {
                        case "manual": GearBoxOperation = GearBoxOperation.Manual; break;
                        case "automatic": GearBoxOperation = GearBoxOperation.Automatic; break;
                        case "semiautomatic": GearBoxOperation = GearBoxOperation.Semiautomatic; break;
                    }
                    initLevel++;
                    break;
                case "engine(gearboxenginebraking":
                    temp = stf.ReadStringBlock("none");
                    switch (temp)
                    {
                        case "none": GearBoxEngineBraking = GearBoxEngineBraking.None; break;
                        case "all_gears": GearBoxEngineBraking = GearBoxEngineBraking.AllGears; break;
                        case "direct_drive": GearBoxEngineBraking = GearBoxEngineBraking.DirectDrive; break;
                    }
                    initLevel++;
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

        public int CurrentGearIndex { get { return currentGearIndex; } }
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
        /// ClutchOn is true when clutch is fully engaged, and false when slipping
        /// </summary>
        public bool clutchOn;
        public bool IsClutchOn
        {
            get
            {
                if (Gears[0].TypeGearBox == 1)
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
                else
                {
                    if (DieselEngine.Locomotive.ThrottlePercent == 0 && !clutchOn)
                    {
                        return clutchOn;
                    }

                    if (DieselEngine.Locomotive.ThrottlePercent > 0)
                    {
                        if (ShaftRPM >= DieselEngine.RealRPM)
                            clutchOn = true;
                    }
                    if (ShaftRPM < DieselEngine.StartingRPM)
                        clutchOn = false;
                    return clutchOn;
                }
            }
        }

        public int NumOfGears { get { return Gears.Count; } }

        int currentGearIndex = -1;
        int nextGearIndex = -1;

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
                    if (Gears[0].TypeGearBox == 1)
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

        public GearBoxOperation GearBoxOperation = GearBoxOperation.Manual;
        public GearBoxOperation OriginalGearBoxOperation = GearBoxOperation.Manual;

        public float TractiveForceN
        {
            get
            {
                if (CurrentGear != null)
                {

                    if (Gears[0].TypeGearBox == 1)
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
                    else if (Gears[0].TypeGearBox == 2 || Gears[0].TypeGearBox == 3)
                    {

                        if (GearBoxOperation == GearBoxOperation.Manual || (GearBoxOperation != GearBoxOperation.Manual && ClutchPercent >= -20))
                        {
                            float tractiveForceN = DieselEngine.DieselTorqueTab[DieselEngine.RealRPM] / DieselEngine.DieselTorqueTab.MaxY() * CurrentGear.MaxTractiveForceN;


                            // Limit tractive force if engine is governed, ie speed cannot exceed the governed speed
                            if (DieselEngine.RealRPM >= DieselEngine.GovenorRPM && ShaftRPM > DieselEngine.GovenorRPM)
                            {
                                // use decay function to decrease tractive effort if RpM exceeds governed RpM value.
                                // y = original amount ( 1 - decay rate)^length of prediction

                                var decayRpM = ShaftRPM - DieselEngine.GovenorRPM;
                                var teDecline = Math.Pow((1.0f - 0.05f), decayRpM);

                                tractiveForceN = (float)Math.Abs(CurrentGear.MaxTractiveForceN * teDecline);
                                tractiveForceN = MathHelper.Clamp(tractiveForceN, 0.0f, CurrentGear.MaxTractiveForceN);  // Clamp tractive effort so that it doesn't go below zero
                            }

//                            Trace.TraceInformation("Geared Tractive Effort #1 - Driving - TE: {0} lbf, RpM: {1}, Torque: {2} lb-ft, Throttle%: {3}, MaxTorque: {4} lb-ft, MaxTE {5} lbf, Speed: {6} mph, Clutch {7}, Gear: {8} IsClutchOn {9} Shaft RpM {10}", tractiveForceN * 0.224809f, DieselEngine.RealRPM, DieselEngine.DieselTorqueTab[DieselEngine.RealRPM] * 0.737562f, DieselEngine.DemandedThrottlePercent, DieselEngine.DieselTorqueTab.MaxY(), CurrentGear.MaxTractiveForceN * 0.224809f, CurrentSpeedMpS * 2.23694f, ClutchPercent, Locomotive.GearBoxController.CurrentNotch, IsClutchOn, ShaftRPM);

                            if (CurrentSpeedMpS > 0)
                            {
                                if (tractiveForceN > (DieselEngine.RailPowerTab[DieselEngine.RealRPM] / CurrentSpeedMpS))
                                {
                                    tractiveForceN = DieselEngine.RailPowerTab[DieselEngine.RealRPM] / CurrentSpeedMpS;
                                }

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
            currentGearIndex = inf.ReadInt32();
            nextGearIndex = inf.ReadInt32();
            gearedUp = inf.ReadBoolean();
            gearedDown = inf.ReadBoolean();
            clutchOn = inf.ReadBoolean();
            clutch = inf.ReadSingle();
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write(currentGearIndex);
            outf.Write(nextGearIndex);
            outf.Write(gearedUp);
            outf.Write(gearedDown);
            outf.Write(clutchOn);
            outf.Write(clutch);
        }

        public void Initialize()
        {
            if (GearBoxParams != null)
            {
                if ((!GearBoxParams.IsInitialized) && (GearBoxParams.AtLeastOneParamFound))
                    Trace.TraceWarning("Some of the gearbox parameters are missing! Default physics will be used.");
                for (int i = 0; i < GearBoxParams.GearBoxNumberOfGears; i++)
                {
                    Gears.Add(new Gear(this));
                    Gears[i].DownGearProportion = GearBoxParams.GearBoxDownGearProportion;
                    Gears[i].IsDirectDriveGear = (GearBoxParams.GearBoxDirectDriveGear == GearBoxParams.GearBoxNumberOfGears);
                    Gears[i].TypeGearBox = GearBoxParams.GearBoxType;
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

                    if (Gears[0].TypeGearBox == 1) // Legacy default operation
                    {
                        Gears[i].Ratio = GearBoxParams.GearBoxMaxSpeedForGearsMpS[i] / DieselEngine.MaxRPM;
                    }
                    else
                    {
                        Gears[i].Ratio = apparentGear;
                    }

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

            if (DieselEngine.State == DieselEngineState.Running)
            {
                switch (GearBoxOperation)
                {
                    case GearBoxOperation.Manual:
                        if (DieselEngine.GearBox.Gears[1].TypeGearBox == 1 &&  DieselEngine.Locomotive.ThrottlePercent == 0)
                        {
                            clutchOn = false;
                            ClutchPercent = 0f;
                        }
                        else if (DieselEngine.Locomotive.ThrottlePercent == 0)
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
        public int TypeGearBox;
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
