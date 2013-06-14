// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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

// AUXILIARIES class
// Auxiliaries class is designed to carry a list of auxiliaries hosted on locomotive
// Auxiliary class represents an auxiliary device such as compressor, fan, charger, etc.
//
// Created by Matej Pacha, PhD.
// For OpenRails Train Simulator, May 27th, 2013
// Contact information:
// e-mail: matej.pacha@fel.uniza.sk, matej.pacha@ieee.org
// University of Zilina, Slovakia
//
// For the ENG file structure example see the bottom of this file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MSTS;

namespace ORTS
{
    
    public enum AuxiliaryDriveTypes
    {
        /// <summary>
        /// Not dependent on any value
        /// </summary>
        General,
        /// <summary>
        /// MotiveForceN dependent (in absolut value)
        /// </summary>
        TractionForce,
        /// <summary>
        /// Throttle dependent
        /// </summary>
        TractionPower,
        /// <summary>
        /// Traction current dependent (implemented as MotiveForceN dependent)
        /// </summary>
        TractionCurrent,
        /// <summary>
        /// DieselRPM dependent
        /// </summary>
        DieselRPM,
        /// <summary>
        /// Dynamic brake current dependent
        /// </summary>
        DynamicBrakeCurrent
    }

    /// <summary>
    /// Class contains and maintains all the auxiliaries in the locomotive
    /// </summary>
    public class Auxiliaries
    {
        /// <summary>
        /// A list of auxiliaries
        /// </summary>
        public List<Auxiliary> AuxList = new List<Auxiliary>();

        /// <summary>
        /// Number of Auxiliaries on the list
        /// </summary>
        public int Count { get { return AuxList.Count; } }

        /// <summary>
        /// Reference to the locomotive carrying the auxiliaries
        /// </summary>
        public readonly MSTSLocomotive Locomotive;

        /// <summary>
        /// not applicable, but still can be used
        /// </summary>
        public Auxiliaries()
        {

        }

        /// <summary>
        /// Creates a set of auxiliaries connected to the locomotive
        /// </summary>
        /// <param name="loco">Host locomotive</param>
        public Auxiliaries(MSTSLocomotive loco)
        {
            Locomotive = loco;
        }

        /// <summary>
        /// Creates a set of auxiliaries connected to the locomotive, based on stf reader parameters 
        /// </summary>
        /// <param name="loco">Host locomotive</param>
        /// <param name="stf">Reference to the ENG file reader</param>
        public Auxiliaries(MSTSLocomotive loco, STFReader stf)
        {
            Locomotive = loco;
            Parse(stf);
        }

        /// <summary>
        /// Parses all the parameters within the ENG file
        /// </summary>
        /// <param name="stf">eference to the ENG file reader</param>
        public void Parse(STFReader stf)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(STFReader.UNITS.None, 0);
            for(int i = 0; i < count; i++)
            {
                string setting = stf.ReadString().ToLower();
                switch (setting)
                {
                    case "compressor":
                        AuxList.Add(new Compressor(Locomotive));
                        break;
                    case "fan":
                        AuxList.Add(new Fan(Locomotive));
                        break;
                    case "miscelanous":
                        AuxList.Add(new Auxiliary(Locomotive));
                        break;
                    default:
                        AuxList.Add(new Auxiliary(Locomotive));
                        break;
                }
                
                AuxList[i].Parse(stf);
            }
        }

        /// <summary>
        /// Saves status of each auxiliary on the list
        /// </summary>
        /// <param name="outf"></param>
        public void Save(BinaryWriter outf)
        {
            foreach (Auxiliary aux in AuxList)
                aux.Save(outf);
        }

        /// <summary>
        /// Restores status of each auxiliary on the list
        /// </summary>
        /// <param name="inf"></param>
        public void Restore(BinaryReader inf)
        {
            foreach (Auxiliary aux in AuxList)
                aux.Restore(inf);
        }

        /// <summary>
        /// A summary of power of all the auxiliaries
        /// </summary>
        public float AuxPowerW
        {
            get
            {
                float temp = 0f;
                foreach (Auxiliary aux in AuxList)
                {
                    temp += aux.ActualPowerW;
                }
                return temp;
            }
        }

        /// <summary>
        /// Updates each auxiliary on the list
        /// </summary>
        /// <param name="elapsedClockSeconds">Time span within the simulation cycle</param>
        public void Update(float elapsedClockSeconds)
        {
            foreach (Auxiliary aux in AuxList)
            {
                aux.Update(elapsedClockSeconds);
            }
        }

        /// <summary>
        /// Gets status of each auxiliary on the list
        /// </summary>
        /// <returns>string formated as one line for one auxiliary</returns>
        public string GetStatus()
        {
            var result = new StringBuilder();
            result.AppendLine("Auxiliaries:");
            foreach (Auxiliary aux in AuxList)
            {
                result.AppendLine(aux.GetStatus());
            }
            return result.ToString();
        }
    }

    /// <summary>
    /// General Auxiliary class, basic definition.
    /// The main principal is to get the auxiliary power based on locomotive status.
    /// User can choose the locomotive variable the auxiliary depends on.
    /// There is a lookup table to consider nonlinear characteristics of the auxiliary drive (such as fans)
    /// </summary>
    public class Auxiliary
    {
        /// <summary>
        /// Reference to the host locomotive
        /// </summary>
        public readonly MSTSLocomotive Locomotive;

        /// <summary>
        /// Internal value for ramps computing
        /// </summary>
        protected float demandedLoadPercent;
        /// <summary>
        /// Gets the actual power consumend by the auxiliary
        /// </summary>
        public float ActualPowerW { get { return LoadPercent * (MaxPowerW / 100f); } }
        /// <summary>
        /// Maximal power in Watts (at 100% of load)
        /// </summary>
        public float MaxPowerW;
        /// <summary>
        /// Load data lookup table - output power in % depends on input command in %
        /// </summary>
        public Interpolator LoadCharacteristics = null;
        /// <summary>
        /// Is always enabled - no matter on Locomotive.PowerOn status
        /// </summary>
        public bool AlwaysEnabled = false;
        /// <summary>
        /// Fault status flag - not used
        /// </summary>
        public bool Fault = false;
        /// <summary>
        /// Name used for a description
        /// </summary>
        public string Name;
        /// <summary>
        /// Running indicator
        /// </summary>
        public bool Running
        {
            get
            {
                return LoadPercent > 0;
            }
        }
        /// <summary>
        /// Current Load in %
        /// </summary>
        public float LoadPercent;
        /// <summary>
        /// Ramp slope when load increases, in percent per second
        /// </summary>
        public float RampUpPercentPerSec = 0;
        /// <summary>
        /// Ramp slope when load decreases, in percent per second
        /// </summary>
        public float RampDownPercentPerSec = 0;

        /// <summary>
        /// Auxiliary drive type is used as an input (InputValuePercent) from which the Load is computed
        /// </summary>
        AuxiliaryDriveTypes DrivenBy = AuxiliaryDriveTypes.General;

        /// <summary>
        /// Input value in % from which the LoadPercent is computed
        /// </summary>
        public float InputValuePercent;

        /// <summary>
        /// Not applicable
        /// </summary>
        public Auxiliary()
        {

        }
        
        /// <summary>
        /// Creates the Auxiliary with a link to the host locomotive
        /// </summary>
        /// <param name="loco">Reference to the host locomotive</param>
        public Auxiliary(MSTSLocomotive loco)
        {
            Locomotive = loco;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="copy">A copy</param>
        public Auxiliary(Auxiliary copy)
        {
            Locomotive = copy.Locomotive;
            MaxPowerW = copy.MaxPowerW;
            LoadCharacteristics = new Interpolator(copy.LoadCharacteristics);
            AlwaysEnabled = copy.AlwaysEnabled;
            Fault = copy.Fault;
            LoadPercent = copy.LoadPercent;
            RampDownPercentPerSec = copy.RampDownPercentPerSec;
            RampUpPercentPerSec = copy.RampUpPercentPerSec;
            Name = copy.Name;
        }

        /// <summary>
        /// Parses parameters from the stf reader
        /// </summary>
        /// <param name="stf">Reference to the stf reader</param>
        public virtual void Parse(STFReader stf)
        {
            stf.MustMatch("(");
            bool end = false;
            while (!end)
            {
                string lowercasetoken = stf.ReadItem().ToLower();
                switch (lowercasetoken)
                {
                    case "maxpower":  MaxPowerW = stf.ReadFloatBlock(STFReader.UNITS.Power, 0); break;
                    case "rampuppercentpersec": RampUpPercentPerSec = stf.ReadFloatBlock(STFReader.UNITS.None, 0); break;
                    case "rampdownpercentpersec": RampDownPercentPerSec = stf.ReadFloatBlock(STFReader.UNITS.None, 0); break;
                    case "alwaysenabled": AlwaysEnabled = stf.ReadBoolBlock(false); break;
                    case "loadcurve": LoadCharacteristics = new Interpolator(stf); break;
                    case "name": Name = stf.ReadStringBlock("general"); break;
                    case "drivenby":
                        string drivenBy = stf.ReadStringBlock("general").ToLower();
                        switch (drivenBy)
                        {
                            case "dieselrpm":
                                DrivenBy = AuxiliaryDriveTypes.DieselRPM;
                                break;
                            case "tractioncurrent":
                                DrivenBy = AuxiliaryDriveTypes.TractionCurrent;
                                break;
                            case "tractionforce":
                                DrivenBy = AuxiliaryDriveTypes.TractionForce;
                                break;
                            case "tractionpower":
                                DrivenBy = AuxiliaryDriveTypes.TractionPower;
                                break;
                            case "general":
                                DrivenBy = AuxiliaryDriveTypes.General;
                                break;
                            case "dynamicbrakecurrent":
                                DrivenBy = AuxiliaryDriveTypes.DynamicBrakeCurrent;
                                break;
                            default:
                                DrivenBy = AuxiliaryDriveTypes.General;
                                break;
                        }
                        break;
                    default:
                        end = true;
                        break;
                }
            }
        }

        /// <summary>
        /// Saves the status of the auxiliary to file
        /// </summary>
        /// <param name="outf">Reference to the binary writer</param>
        public virtual void Save(BinaryWriter outf)
        {
            outf.Write(demandedLoadPercent);
            outf.Write(AlwaysEnabled);
            outf.Write(Fault);
            outf.Write(LoadPercent);
        }

        /// <summary>
        /// Restores the status of the auxiliary from file
        /// </summary>
        /// <param name="inf">Reference to the binary reader</param>
        public virtual void Restore(BinaryReader inf)
        {
            demandedLoadPercent = inf.ReadSingle();
            AlwaysEnabled = inf.ReadBoolean();
            Fault = inf.ReadBoolean();
            LoadPercent = inf.ReadSingle();
        }

        /// <summary>
        /// Updates the status of the auxiliary
        /// </summary>
        /// <param name="elapsedClockSeconds">Time span</param>
        public virtual void Update(float elapsedClockSeconds)
        {
            ProcessDriveType();

            if ((AlwaysEnabled) || (Locomotive != null ? Locomotive.PowerOn : true))
            {
                if (LoadCharacteristics != null)
                    demandedLoadPercent = LoadCharacteristics[InputValuePercent];
                else
                    demandedLoadPercent = InputValuePercent;
            }
            else
                demandedLoadPercent = 0;

            ProcessRamps(elapsedClockSeconds);
        }

        /// <summary>
        /// Processes the DriveType and computes the InputValuePercent based on the Locomotive status
        /// </summary>
        protected void ProcessDriveType()
        {
            switch (DrivenBy)
            {
                case AuxiliaryDriveTypes.General:
                    InputValuePercent = 100f;
                    break;
                case AuxiliaryDriveTypes.DieselRPM:
                    if (Locomotive.GetType() == typeof(MSTSDieselLocomotive))
                    {
                        if (((MSTSDieselLocomotive)Locomotive).DieselEngines != null)
                        {
                            InputValuePercent =
                                (
                                (((MSTSDieselLocomotive)Locomotive).DieselEngines[0].RealRPM - ((MSTSDieselLocomotive)Locomotive).DieselEngines[0].IdleRPM))
                              / (((MSTSDieselLocomotive)Locomotive).DieselEngines[0].MaxRPM - ((MSTSDieselLocomotive)Locomotive).DieselEngines[0].IdleRPM) * 100f;
                        }
                        else
                            InputValuePercent = Locomotive.Throttle * 100f;
                    }
                    else
                        InputValuePercent = Locomotive.Throttle * 100f;
                    break;
                case AuxiliaryDriveTypes.TractionCurrent:
                case AuxiliaryDriveTypes.TractionForce:
                    InputValuePercent = Locomotive.MotiveForceN / Locomotive.MaxForceN * 100f;
                    break;
                case AuxiliaryDriveTypes.TractionPower:
                    InputValuePercent = Locomotive.Throttle * 100f;
                    break;
                case AuxiliaryDriveTypes.DynamicBrakeCurrent:
                    if (Locomotive.DynamicBrakePercent > 0)
                        InputValuePercent = -Locomotive.MotiveForceN / Locomotive.MaxDynamicBrakeForceN * 100f;
                    else
                        InputValuePercent = 0f;
                    break;
                default:
                    InputValuePercent = 100f;
                    break;
            }
            if (InputValuePercent < 0)
                InputValuePercent *= -1f;
        }

        /// <summary>
        /// Processes the demand and computes the output (LoadPercent) based on the ramp parameters
        /// </summary>
        /// <param name="elapsedClockSeconds"></param>
        protected void ProcessRamps(float elapsedClockSeconds)
        {

            if ((RampUpPercentPerSec > 0f) && (LoadPercent < demandedLoadPercent))
            {
                LoadPercent += RampUpPercentPerSec * elapsedClockSeconds;
                if (LoadPercent > 100.0f)
                    LoadPercent = 100.0f;
            }
            else
            {

                if ((RampDownPercentPerSec > 0f) && (LoadPercent > demandedLoadPercent))
                {
                    LoadPercent -= RampDownPercentPerSec * elapsedClockSeconds;
                    if (LoadPercent < 0f)
                        LoadPercent = 0f;
                }
                else
                    LoadPercent = demandedLoadPercent;
            }
        }


        /// <summary>
        /// Gets a string containing basic status informations
        /// </summary>
        /// <returns>Status information</returns>
        public virtual string GetStatus()
        {
            var result = new StringBuilder();
            result.AppendFormat("{0}: Load: {1:F0}%, Power: {2:F0} kW, Status: {3}", Name, LoadPercent, ActualPowerW / 1000f, Fault ? "Fault" : "OK");
            return result.ToString();
        }
    }

    /// <summary>
    /// Auxiliary Fan specialization
    /// </summary>
    public class Fan : Auxiliary
    {
        public Fan(MSTSLocomotive loco) : base(loco)
        {
        }

        public override void Parse(STFReader stf)
        {
            base.Parse(stf);
        }
    }

    /// <summary>
    /// Auxiliary Compressor specialization
    /// </summary>
    public class Compressor : Auxiliary
    {
        public float StartPressurePSI = 110;
        public float StopPressurePSI = 130;
        public float MainResChargingPSIpS = 0.4f;

        public float PressurePSI;

        public Compressor(MSTSLocomotive loco) : base(loco)
        {
        }

        public override void Parse(STFReader stf)
        {
            base.Parse(stf);
        }

        /// <summary>
        /// Updates the status of the compressor, similar to the general auxiliary, but considering the Locomotive.CompressorOn status
        /// </summary>
        /// <param name="elapsedClockSeconds"></param>
        public override void Update(float elapsedClockSeconds)
        {
            ProcessDriveType();

            if ((AlwaysEnabled) || (Locomotive != null ? Locomotive.PowerOn : true))
            {
                if (LoadCharacteristics != null)
                    demandedLoadPercent = LoadCharacteristics[InputValuePercent];
                else
                    demandedLoadPercent = InputValuePercent;
            }
            else
                demandedLoadPercent = 0;

            if (!Locomotive.CompressorOn)
                demandedLoadPercent = 0;

            ProcessRamps(elapsedClockSeconds);
        }
    }

}


//Engine (
//  ORTS (
//      Auxiliaries ( 5
//            Compressor (
//                MaxPower ( 30kW )
//                RampUpPercentPerSec ( 10 )
//                RampDownPercentPerSec ( 20 )
//                AlwaysEnabled ( 0 )
//                Name ( "Compressor" )
//                DrivenBy ( "General" )
//            )
//            Fan (
//                MaxPower ( 30kW )
//                AlwaysEnabled ( 0 )
//                LoadCurve (
//                    0	10
//                    10	12
//                    20	15
//                    30	20
//                    40	30
//                    50	50
//                    60	80
//                    70	100
//                    80	100
//                    100	100		
//                )
//                Name ( "DieselFan" )
//                DrivenBy ( "DieselRPM" )
//            )
//            Fan (
//                MaxPower ( 25kW )
//                AlwaysEnabled ( 0 )
//                LoadCurve (
//                    0	5
//                    10	5
//                    20	5
//                    30	10
//                    40	20
//                    50	40
//                    60	70
//                    70	100
//                    80	100
//                    100	100		
//                )
//                Name ( "TractionMotorFan" )
//                DrivenBy ( "TractionForce" )
//            )
//            Fan (
//                MaxPower ( 25kW )
//                AlwaysEnabled ( 0 )
//                LoadCurve (
//                    0	5
//                    10	10
//                    20	15
//                    30	20
//                    40	25
//                    50	40
//                    60	70
//                    70	100
//                    80	100
//                    100	100		
//                )
//                Name ( "EDBFan" )
//                DrivenBy ( "DynamicBrakeCurrent" )
//            )
//            Miscelanous (
//                MaxPower ( 20kW )
//                Name ( "Misc" )
//                DrivenBy ( "General" )
//            )
//        )
//  )
//)