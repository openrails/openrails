using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSTS;
using System.IO;
using System.Diagnostics;

namespace ORTS
{
    public class MSTSGearBoxParams
    {
        public int GearBoxNumberOfGears = 1;
        public int GearBoxDirectDriveGear = 1;
        public GearBoxOperation GearBoxOperation = GearBoxOperation.Manual;
        public GearBoxEngineBraking GearBoxEngineBraking = GearBoxEngineBraking.None;
        public List<float> GearBoxMaxSpeedForGears = new List<float>();
        public List<float> GearBoxMaxTractiveForceForGears = new List<float>();
        public float GearBoxOverspeedPercentageForFailure = 150f;
        public float GearBoxBackLoadForce = 1000;
        public float GearBoxCoastingForce = 500;
        public float GearBoxUpGearProportion = 0.85f;
        public float GearBoxDownGearProportion = 0.35f;

        int initLevel = 0;

        public bool IsInitialized { get { return initLevel >= 7; } }
        public bool AtLeastOneParamFound { get { return initLevel >= 1; } }

        public MSTSGearBoxParams()
        {

        }

        public MSTSGearBoxParams(MSTSGearBoxParams copy)
        {
            GearBoxNumberOfGears = copy.GearBoxNumberOfGears;
            GearBoxDirectDriveGear = copy.GearBoxDirectDriveGear;
            GearBoxOperation = copy.GearBoxOperation;
            GearBoxEngineBraking = copy.GearBoxEngineBraking;
            GearBoxMaxSpeedForGears = new List<float>(copy.GearBoxMaxSpeedForGears);
            GearBoxMaxTractiveForceForGears = new List<float>(copy.GearBoxMaxTractiveForceForGears);
            GearBoxOverspeedPercentageForFailure = copy.GearBoxOverspeedPercentageForFailure;
            GearBoxBackLoadForce = copy.GearBoxBackLoadForce;
            GearBoxCoastingForce = copy.GearBoxCoastingForce;
            GearBoxUpGearProportion = copy.GearBoxUpGearProportion;
            GearBoxDownGearProportion = copy.GearBoxDownGearProportion;
            initLevel = copy.initLevel;
        }

        public void Parse(string lowercasetoken, STFReader stf)
        {
            string temp = "";
            switch (lowercasetoken)
            {
                case "engine(gearboxnumberofgears": GearBoxNumberOfGears = stf.ReadIntBlock(STFReader.UNITS.None, 1); initLevel++; break;
                case "engine(gearboxdirectdrivegear": GearBoxDirectDriveGear = stf.ReadIntBlock(STFReader.UNITS.None, 1); break; // initLevel++; break;
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
                        for (int i = 0; i < GearBoxNumberOfGears; i++)
                        {
                            GearBoxMaxSpeedForGears.Add(stf.ReadFloat(STFReader.UNITS.None, 10.0f));
                            GearBoxMaxSpeedForGears[i] = MpS.FromMpH(GearBoxMaxSpeedForGears[i]);
                        }
                        stf.SkipRestOfBlock();
                        initLevel++;
                    }
                    break;
                case "engine(gearboxmaxtractiveforceforgears":
                    temp = stf.ReadItem();
                    if (temp == ")")
                    {
                        stf.StepBackOneItem();
                    }
                    if (temp == "(")
                    {
                        for (int i = 0; i < GearBoxNumberOfGears; i++)
                            GearBoxMaxTractiveForceForGears.Add(stf.ReadFloat(STFReader.UNITS.Force, 10000.0f));
                        stf.SkipRestOfBlock();
                        initLevel++;
                    }
                    break;
                case "engine(gearboxoverspeedpercentageforfailure": GearBoxOverspeedPercentageForFailure = stf.ReadFloatBlock(STFReader.UNITS.None, 150f); break; // initLevel++; break;
                case "engine(gearboxbackloadforce": GearBoxBackLoadForce = stf.ReadFloatBlock(STFReader.UNITS.Force, 0f); initLevel++; break;
                case "engine(gearboxcoastingforce": GearBoxCoastingForce = stf.ReadFloatBlock(STFReader.UNITS.Force, 0f); initLevel++; break;
                case "engine(gearboxupgearproportion": GearBoxUpGearProportion = stf.ReadFloatBlock(STFReader.UNITS.None, 0.85f); break; // initLevel++; break;
                case "engine(gearboxdowngearproportion": GearBoxDownGearProportion = stf.ReadFloatBlock(STFReader.UNITS.None, 0.25f); break; // initLevel++; break;
                default: break;
            }
        }
    }

    public class GearBox
    {
        MSTSGearBoxParams mstsParams = new MSTSGearBoxParams();
        DieselEngine DieselEngine;
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

        private bool gearedUp = false;
        private bool gearedDown = false;
        public bool GearedUp { get { return gearedUp; } }
        public bool GearedDown { get { return gearedDown; } }

        public bool AutoGearUp()
        {
            if (shaft < 0.05f)
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
            if (shaft < 0.05f)
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

        public bool shaftOn = false;
        public bool IsShaftOn
        {
            get
            {
                if (ShaftRPM >= (CurrentGear.DownGearProportion * DieselEngine.MaxRPM))
                    shaftOn = true;
                if (ShaftRPM < DieselEngine.StartingRPM)
                    shaftOn = false;
                return shaftOn;
            }
        }

        public int NumOfGears { get { return Gears.Count; } }

        int currentGearIndex = -1;
        int nextGearIndex = -1;

        public float CurrentSpeedMpS 
        {
            get
            {
                if(DieselEngine.locomotive.Direction == Direction.Reverse)
                    return -(DieselEngine.locomotive.SpeedMpS);
                else
                    return (DieselEngine.locomotive.SpeedMpS);
            }
        }

        public float ShaftRPM 
        {
            get
            {
                if (CurrentGear == null)
                    return DieselEngine.RealRPM;
                else
                    return CurrentSpeedMpS / CurrentGear.Ratio; 
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

        float shaft = 0;
        public float ShaftPercent { set { shaft = (value > 100.0f ? 100f : (value < -100f ? -100f : value)) / 100f; } get { return shaft * 100f; } }

        public bool AutoShaft = true;

        public GearBoxOperation GearBoxOperation = GearBoxOperation.Manual;

        public float MotiveForceN
        {
            get
            {
                if (CurrentGear != null)
                {
                    if (ShaftPercent >= -20)
                    {
                        //float motiveForceN = DieselEngine.DemandedThrottlePercent / 100f * CurrentGear.MaxTractiveForceN;
                        //if (CurrentSpeedMpS > 0)
                        //{
                        //    if (motiveForceN > (DieselEngine.MaxOutputPowerW / CurrentSpeedMpS))
                        //        motiveForceN = DieselEngine.MaxOutputPowerW / CurrentSpeedMpS;
                        //}

                        float motiveForceN = DieselEngine.DieselTorqueTab[DieselEngine.RealRPM] * DieselEngine.DemandedThrottlePercent / 100f * CurrentGear.MaxTractiveForceN;
                        if (CurrentSpeedMpS > 0)
                        {
                            if (motiveForceN > (DieselEngine.MaxOutputPowerW/ CurrentSpeedMpS))
                                motiveForceN = DieselEngine.MaxOutputPowerW / CurrentSpeedMpS;
                        }
                        return motiveForceN;
                    }
                    else
                        return -CurrentGear.CoastingForceN * (100f + ShaftPercent) / 100f;
                }
                else
                    return 0;
            }
        }

        public GearBox() { }

        public GearBox(GearBox copy, DieselEngine de)
        {
            mstsParams = new MSTSGearBoxParams(copy.mstsParams);
            DieselEngine = de;

            if (mstsParams != null)
            {
                if ((!mstsParams.IsInitialized) && (mstsParams.AtLeastOneParamFound))
                    Trace.TraceWarning("Some of the gearbox parameters are missing! Default physics will be used.");
                for (int i = 0; i < mstsParams.GearBoxNumberOfGears; i++)
                {
                    Gears.Add(new Gear(this));
                    Gears[i].BackLoadForceN = mstsParams.GearBoxBackLoadForce;
                    Gears[i].CoastingForceN = mstsParams.GearBoxCoastingForce;
                    Gears[i].DownGearProportion = mstsParams.GearBoxDownGearProportion;
                    Gears[i].IsDirectDriveGear = (mstsParams.GearBoxDirectDriveGear == mstsParams.GearBoxNumberOfGears);
                    Gears[i].MaxSpeedMpS = mstsParams.GearBoxMaxSpeedForGears[i];
                    Gears[i].MaxTractiveForceN = mstsParams.GearBoxMaxTractiveForceForGears[i];
                    Gears[i].OverspeedPercentage = mstsParams.GearBoxOverspeedPercentageForFailure;
                    Gears[i].UpGearProportion = mstsParams.GearBoxUpGearProportion;
                    Gears[i].Ratio = mstsParams.GearBoxMaxSpeedForGears[i] / DieselEngine.MaxRPM;
                }
                GearBoxOperation = mstsParams.GearBoxOperation;
            }

        }      

        

        public void Parse(string lowercasetoken, STFReader stf)
        {
            mstsParams.Parse(lowercasetoken, stf);
        }

        public bool IsRestored = false;

        public void Restore(BinaryReader inf)
        {
            currentGearIndex = inf.ReadInt32();
            nextGearIndex = inf.ReadInt32();
            gearedUp = inf.ReadBoolean();
            gearedDown = inf.ReadBoolean();
            shaftOn = inf.ReadBoolean();
            IsRestored = true;
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write(currentGearIndex);
            outf.Write(nextGearIndex);
            outf.Write(gearedUp);
            outf.Write(gearedDown);
            outf.Write(shaftOn);
        }

        public bool IsInitialized { get { return mstsParams.IsInitialized; } }

        public void Update(float elapsedClockSeconds)
        {
            if ((shaft <= 0.05) || (shaft >= 1f))
            {
                if (currentGearIndex < nextGearIndex) DieselEngine.locomotive.SignalEvent(Event.GearUp);
                if (currentGearIndex > nextGearIndex) DieselEngine.locomotive.SignalEvent(Event.GearDown);
                currentGearIndex = nextGearIndex;
            }
            if (DieselEngine.EngineStatus == DieselEngine.Status.Running)
            {
                switch (GearBoxOperation)
                {
                    case GearBoxOperation.Manual:
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
                            if (DieselEngine.locomotive.ThrottlePercent == 0)
                            {
                                if ((CurrentGear != null) || (NextGear == null))
                                {
                                    nextGearIndex = -1;
                                    currentGearIndex = -1;
                                    shaftOn = false;
                                    gearedDown = false;
                                    gearedUp = false;
                                }

                            }
                        }
                        else
                        {
                            if ((DieselEngine.locomotive.ThrottlePercent > 0))
                                AutoGearUp();
                            else
                            {
                                nextGearIndex = -1;
                                currentGearIndex = -1;
                                shaftOn = false;
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
                shaftOn = false;
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
        public bool IsDirectDriveGear = false;
        public float MaxSpeedMpS = 0;
        public float MaxTractiveForceN = 0;
        public float OverspeedPercentage = 0;
        public float BackLoadForceN = 0;
        public float CoastingForceN = 0;
        public float UpGearProportion = 0;
        public float DownGearProportion = 0;

        public float Ratio = 1f;

        public GearBox GearBox = null;

        public Gear(GearBox gb) { GearBox = gb; }
    }
}
