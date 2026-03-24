// COPYRIGHT 2011 by the Open Rails project.
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
//
// Debug for Adhesion
//#define DEBUG_ADHESION

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using ORTS.Common;
using Orts.Parsers.Msts;
using Orts.Simulation.RollingStocks.SubSystems.PowerTransmissions;
using SharpDX.Direct2D1;
using SharpDX.Direct3D9;
using Orts.Formats.OR;
using static Orts.Simulation.RollingStocks.SubSystems.PowerTransmissions.Axle;
using MonoGame.Framework.Utilities.Deflate;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerTransmissions
{
    /// <summary>
    /// Axle drive type to determine an input and solving method for axles
    /// </summary>
    public enum AxleDriveType
    {
        /// <summary>
        /// Without any drive
        /// </summary>
        NotDriven = 0,
        /// <summary>
        /// Traction motor connected through gearbox to axle
        /// </summary>
        MotorDriven = 1,
        /// <summary>
        /// Simple force driven axle
        /// </summary>
        ForceDriven = 2
    }

    /// <summary>
    /// Sums individual axle values to create a total value
    /// </summary>

    public class Axles : ISubSystem<Axles>
    {
        /// <summary>
        /// List of axles
        /// </summary>
        public List<Axle> AxleList = new List<Axle>();
        /// <summary>
        /// Number of axles
        /// </summary>
        public int Count { get { return AxleList.Count; } }
        /// <summary>
        /// Reference to the car
        /// </summary>
        protected readonly TrainCar Car;

        /// <summary>
        /// Get total axle out force with brake and friction force substracted
        /// </summary>
        public float AxleMotiveForceN
        {
            get
            {
                float forceN = 0;
                foreach (var axle in AxleList)
                {
                    forceN += axle.AxleMotiveForceN;
                }
                return forceN;
            }
        }
        /// <summary>
        /// Get total axle out power with brake and friction substracted
        /// </summary>
        public float AxleMotivePowerW
        {
            get
            {
                float powerW = 0;
                foreach (var axle in AxleList)
                {
                    powerW += axle.AxleMotivePowerW;
                }
                return powerW;
            }
        }
        /// <summary>
        /// Get total axle out force due to braking
        /// </summary>
        public float AxleBrakeForceN
        {
            get
            {
                float forceN = 0;
                foreach (var axle in AxleList)
                {
                    forceN += axle.AxleBrakeForceN;
                }
                return forceN;
            }
        }
        /// <summary>
        /// Get total axle out force due to friction
        /// </summary>
        public float AxleFrictionForceN
        {
            get
            {
                float forceN = 0;
                foreach (var axle in AxleList)
                {
                    forceN += axle.AxleFrictionForceN;
                }
                return forceN;
            }
        }
        /// <summary>
        /// Get total axle out force
        /// </summary>
        public float AxleForceN
        {
            get
            {
                float forceN = 0;
                foreach (var axle in AxleList)
                {
                    forceN += axle.AxleForceN;
                }
                return forceN;
            }
        }
        /// <summary>
        /// Get total axle out power
        /// </summary>
        public float AxlePowerW
        {
            get
            {
                float powerW = 0;
                foreach (var axle in AxleList)
                {
                    powerW += axle.AxlePowerW;
                }
                return powerW;
            }
        }
        /// <summary>
        /// Get total axle in force
        /// </summary>
        public float DriveForceN
        {
            get
            {
                float forceN = 0;
                foreach (var axle in AxleList)
                {
                    forceN += axle.DriveForceN;
                }
                return forceN;
            }
        }
        /// <summary>
        /// Get total axle in power
        /// </summary>
        public float DrivePowerW
        {
            get
            {
                float powerW = 0;
                foreach (var axle in AxleList)
                {
                    powerW += axle.DrivePowerW;
                }
                return powerW;
            }
        }
        public bool IsWheelSlip
        {
            get
            {
                foreach (var axle in AxleList)
                {
                    if (axle.IsWheelSlip) return true;
                }
                return false;
            }
        }
        public bool IsWheelSlipWarning
        {
            get
            {
                foreach (var axle in AxleList)
                {
                    if (axle.IsWheelSlipWarning) return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Get wheel slip status for the whole engine, ie whenever one axle is in slip 
        /// </summary>
        public bool HuDIsWheelSlip
        {
            get
            {
                foreach (var axle in AxleList)
                {
                    if (axle.HuDIsWheelSlip) return true;
                }
                return false;
            }
        }
        public bool HuDIsWheelSlipWarning
        {
            get
            {
                foreach (var axle in AxleList)
                {
                    if (axle.HuDIsWheelSlipWarning) return true;
                }
                return false;
            }
        }

        public int NumOfSubstepsPS
        {
            get
            {
                int sub = 0;
                foreach (var axle in AxleList)
                {
                    if (axle.NumOfSubstepsPS > sub) sub = axle.NumOfSubstepsPS;
                }
                return sub;
            }
        }
        public float SlipSpeedMpS
        {
            get
            {
                float speed = 0;
                foreach (var axle in AxleList)
                {
                    if (Math.Abs(axle.SlipSpeedMpS) > speed) speed = axle.SlipSpeedMpS;
                }
                return speed;
            }
        }
        public float SlipSpeedPercent
        {
            get
            {
                float slip = 0;
                foreach (var axle in AxleList)
                {
                    if (Math.Abs(axle.SlipSpeedPercent) > slip) slip = axle.SlipSpeedPercent;
                }
                return slip;
            }
        }
        public float SlipDerivationPercentpS
        {
            get
            {
                float slip = 0;
                foreach (var axle in AxleList)
                {
                    if (Math.Abs(axle.SlipDerivationPercentpS) > slip) slip = axle.SlipDerivationPercentpS;
                }
                return slip;
            }
        }
        public double ResetTime;
        public Axles(TrainCar car)
        {
            Car = car;
        }
        public Axle this[int i]
        {
            get { return AxleList[i]; }
            set { AxleList[i] = value; }
        }
        public void Add(Axle axle)
        {
            AxleList.Add(axle);
        }

        /// <summary>
        /// Parses all the parameters within the ENG file
        /// </summary>
        /// <param name="stf">reference to the ENG file reader</param>
        public void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "wagon(ortsadhesion(wheelset":
                    AxleList.Clear();
                    stf.MustMatch("(");
                    stf.ParseBlock(
                        new[] {
                            new STFReader.TokenProcessor(
                                "axle",
                                () => {
                                    var axle = new Axle(Car);
                                    AxleList.Add(axle);
                                    axle.Parse(stf);
                                }
                            )
                        });
                    if (AxleList.Count == 0)
                        throw new InvalidDataException("Wheelset block with no axles");
                    break;
            }
        }
        public void Copy(Axles other)
        {
            AxleList = new List<Axle>();
            foreach (var ax in other.AxleList)
            {
                var axle = new Axle(Car);
                axle.Copy(ax);
                AxleList.Add(axle);
            }
        }

        public void Initialize()
        {
            ResetTime = Car.Simulator.GameTime;
            int numForce = 0;
            int numMotor = 0;
            foreach (var axle in AxleList)
            {
                if (!(Car is MSTSLocomotive)) axle.DriveType = AxleDriveType.NotDriven;
                else if (axle.DriveType == AxleDriveType.ForceDriven) numForce++;
                else if (axle.DriveType == AxleDriveType.MotorDriven) numMotor++;
            }
            float totalDriveWheelWeightKg = 0;
            float totalWheelWeightKg = 0;
            foreach (var axle in AxleList)
            {
                if (numMotor > 0 && axle.DriveType == AxleDriveType.ForceDriven) axle.DriveType = AxleDriveType.NotDriven;
                else if (axle.DriveType == AxleDriveType.ForceDriven) axle.TractiveForceFraction = 1.0f / numForce;
                else if (axle.DriveType == AxleDriveType.MotorDriven) axle.TractiveForceFraction = 1.0f / numMotor;
                int numDriven = numMotor > 0 ? numMotor : numForce;
                int numNotDriven = AxleList.Count - numDriven;
                var locomotive = Car as MSTSLocomotive;
                if (locomotive != null)
                {
                    if (axle.DriveType != AxleDriveType.NotDriven)
                    {
                        if (axle.WheelWeightKg <= 0) axle.WheelWeightKg = locomotive.DrvWheelWeightKg / numDriven;
                        if (axle.NumWheelsetAxles <= 0) axle.NumWheelsetAxles = locomotive.LocoNumDrvAxles / numDriven;
                        if (axle.WheelRadiusM <= 0) axle.WheelRadiusM = locomotive.DriverWheelRadiusM;
                    }

                    // Set default type of axle rail traction selected
                    if (axle.AxleRailTractionType == AxleRailTractionTypes.Unknown)
                    {
                        axle.AxleRailTractionType = AxleRailTractionTypes.Adhesion;

                        if ( locomotive.Simulator.Settings.VerboseConfigurationMessages)
                            Trace.TraceInformation("LocomotiveAxleRailDriveType set to Default value of {0}", axle.AxleRailTractionType);
                    }

                    // set the wheel slip threshold times for different types of locomotives
                    // Because of the irregular force around the wheel for a steam engine during a revolution, "response" time for warnings needs to be lower
                    if (locomotive.EngineType == TrainCar.EngineTypes.Steam)
                    {
                        axle.WheelSlipThresholdTimeS = 1;
                        axle.WheelSlipWarningThresholdTimeS = axle.WheelSlipThresholdTimeS * 0.75f;
                    }
                    else // diesel and electric locomotives
                    {
                        axle.WheelSlipThresholdTimeS = 1;
                        axle.WheelSlipWarningThresholdTimeS = 1;
                    }
                }
                if (axle.DriveType == AxleDriveType.NotDriven)
                {
                    var wagon = Car as MSTSWagon;
                    if (axle.WheelWeightKg <= 0)
                    {
                        if (locomotive != null) axle.WheelWeightKg = Math.Max((Car.MassKG - locomotive.DrvWheelWeightKg) / numNotDriven, 500);
                        else axle.WheelWeightKg = Car.MassKG / numNotDriven;
                    }
                    if (axle.NumWheelsetAxles <= 0) axle.NumWheelsetAxles = Math.Max(Car.GetWagonNumAxles() / numNotDriven, 1);
                    if (axle.WheelRadiusM <= 0) axle.WheelRadiusM = wagon.WheelRadiusM;
                }
                if (axle.InertiaKgm2 <= 0) axle.InertiaKgm2 = (Car as MSTSWagon).AxleInertiaKgm2 / AxleList.Count;
                if (axle.WheelFlangeAngleRad <= 0) axle.WheelFlangeAngleRad = Car.MaximumWheelFlangeAngleRad;
                if (axle.AxleWeightN <= 0) axle.AxleWeightN = 9.81f * axle.WheelWeightKg;  //remains fixed for diesel/electric locomotives, but varies for steam locomotives
                if (axle.DampingNs <= 0) axle.DampingNs = axle.WheelWeightKg / 1000.0f;
                if (axle.FrictionN <= 0) axle.FrictionN = axle.WheelWeightKg / 1000.0f;

                if (axle.DriveType != AxleDriveType.NotDriven) totalDriveWheelWeightKg += axle.WheelWeightKg;
                totalWheelWeightKg += axle.WheelWeightKg;

                // if values have not been configured in the ENG axle block, read from the locomotive/wagon file
                if (axle.brakingCogRead == false && (axle.AxleRailTractionType == AxleRailTractionTypes.Rack || axle.AxleRailTractionType == AxleRailTractionTypes.Rack ))
                {
                    axle.BrakingCogWheelFitted = Car.BrakeCogWheelFitted;
                }
            }
            foreach (var axle in AxleList)
            {
                var locomotive = Car as MSTSLocomotive;
                if (axle.DriveType == AxleDriveType.NotDriven)
                {
                    axle.BrakeForceFraction = locomotive == null || !locomotive.DriveWheelOnlyBrakes ? axle.WheelWeightKg / totalWheelWeightKg : 0;
                }
                else
                {
                    axle.BrakeForceFraction = axle.WheelWeightKg / (locomotive != null && locomotive.DriveWheelOnlyBrakes ? totalDriveWheelWeightKg : totalWheelWeightKg);
                }
                axle.Initialize();
            }
        }

        public void InitializeMoving()
        {
            ResetTime = Car.Simulator.GameTime;
            foreach (var axle in AxleList)
            {
                axle.TrainSpeedMpS = Car.SpeedMpS;
                axle.InitializeMoving();
            }
        }
        /// <summary>
        /// Saves status of each axle on the list
        /// </summary>
        /// <param name="outf"></param>
        public void Save(BinaryWriter outf)
        {
            outf.Write(AxleList.Count);
            foreach (var axle in AxleList)
                axle.Save(outf);
        }

        /// <summary>
        /// Restores status of each axle on the list
        /// </summary>
        /// <param name="inf"></param>
        public void Restore(BinaryReader inf)
        {
            int count = inf.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                if (i >= AxleList.Count)
                {
                    AxleList.Add(new Axle(Car));
                    AxleList[i].Initialize();
                }
                AxleList[i].Restore(inf);
            }
        }

        /// <summary>
        /// switch between Polach and Pacha adhesion calculation
        /// </summary>
        public static bool UsePolachAdhesion = false; // "static" so there's only one value in the program.
        public bool PreviousUsePolachAdhesion = false; // Keep a note for each Axles so that we can tell if it changed.

        /// <summary>
        /// Updates each axle on the list
        /// </summary>
        /// <param name="elapsedSeconds">Time span within the simulation cycle</param>
        public void Update(float elapsedSeconds)
        {
            UsePolachAdhesion = AdhesionPrecision.IsPrecisionHigh(this, elapsedSeconds, Car.Simulator.GameTime);
            foreach (var axle in AxleList)
            {
                if (UsePolachAdhesion != PreviousUsePolachAdhesion) // There's been a transition
                {
                    axle.AxleSpeedMpS = axle.TrainSpeedMpS; // So the transition doesn't cause a wheelslip
                }
                axle.Update(elapsedSeconds);
            }
            PreviousUsePolachAdhesion = UsePolachAdhesion;
        }
        public List<Axle>.Enumerator GetEnumerator()
        {
            return AxleList.GetEnumerator();
        }

        static class AdhesionPrecision  // "static" so all "Axles" share the same level of precision
        {
            enum AdhesionPrecisionLevel
            {
                /// <summary>
                /// Initial level uses Polach algorithm
                /// </summary>
                High = 0,
                /// <summary>
                /// Low-performance PCs use Pacha's algorithm
                /// </summary>
                Low = 1,
                /// <summary>
                /// After frequent transitions, low-performance PCs are locked to Pacha's algorithm
                /// </summary>
                LowLocked = 2
            }

            // Adjustable limits
            const float LowerLimitS = 0.025f;   // timespan 0.025 = 40 fps screen rate, low timeSpan and high FPS
            const float UpperLimitS = 0.033f;   // timespan 0.033 = 30 fps screen rate, high timeSpan and low FPS
            const double IntervalBetweenDowngradesLimitS = 5 * 60; // Locks in low precision if < 5 mins between downgrades

            static AdhesionPrecisionLevel PrecisionLevel = AdhesionPrecisionLevel.High;
            static double TimeOfLatestDowngrade = 0 - IntervalBetweenDowngradesLimitS; // Starts at -5 mins

            // Tested by dropping the framerate below 30 fps interactively. Did this by opening and closing the HelpWindow after inserting
            //   Threading.Thread.Sleep(40);
            // into HelpWindow.PrepareFrame() temporarily.
            public static bool IsPrecisionHigh(Axles axles, float elapsedSeconds, double gameTime)
            {
                // Switches between Polach (high precision) adhesion model and Pacha (low precision) adhesion model depending upon the PC performance
                switch (PrecisionLevel)
                {
                    case AdhesionPrecisionLevel.High:
                        if (elapsedSeconds > UpperLimitS)
                        {
                            var screenFrameRate = 1 / elapsedSeconds;
                            var timeSincePreviousDowngradeS = gameTime - TimeOfLatestDowngrade;
                            if (timeSincePreviousDowngradeS < IntervalBetweenDowngradesLimitS)
                            {
                                Trace.TraceInformation($"At {gameTime:F0} secs, advanced adhesion model switched to low precision permanently after {timeSincePreviousDowngradeS:F0} secs since previous switch (less than limit of {IntervalBetweenDowngradesLimitS})");
                                PrecisionLevel = AdhesionPrecisionLevel.LowLocked;
                            }
                            else
                            {
                                TimeOfLatestDowngrade = gameTime;
                                Trace.TraceInformation($"At {gameTime:F0} secs, advanced adhesion model switched to low precision after low frame rate {screenFrameRate:F1} below limit {1 / UpperLimitS:F0}");
                                PrecisionLevel = AdhesionPrecisionLevel.Low;
                            }
                        }
                        break;

                    case AdhesionPrecisionLevel.Low:
                        if (elapsedSeconds > 0 // When debugging step by step, elapsedSeconds == 0, so test for that
                            && elapsedSeconds < LowerLimitS)
                        {
                            PrecisionLevel = AdhesionPrecisionLevel.High;
                            var ScreenFrameRate = 1 / elapsedSeconds;
                            Trace.TraceInformation($"At {gameTime:F0} secs, advanced adhesion model switched to high precision after high frame rate {ScreenFrameRate:F1} above limit {1 / LowerLimitS:F0}");
                        }
                        break;

                    case AdhesionPrecisionLevel.LowLocked:
                        break;
                }
                return (PrecisionLevel == AdhesionPrecisionLevel.High);
            }
        }
    }


    /// <summary>
    /// Axle class by Matej Pacha (c)2011, University of Zilina, Slovakia (matej.pacha@kves.uniza.sk)
    /// The class is used to manage and simulate axle forces considering adhesion problems.
    /// Basic configuration:
    ///  - Motor generates motive torque what is converted into a motive force (through gearbox)
    ///    or the motive force is passed directly to the DriveForce property
    ///  - With known TrainSpeed the Update(timeSpan) method computes a dynamic model of the axle
    ///     - additional (optional) parameters are weather conditions and correction parameter
    ///  - Finally an output motive force is stored into the AxleForce
    ///  
    /// Every computation within Axle class uses SI-units system with xxxxxUUU unit notation
    /// </summary>
    public class Axle : ISubSystem<Axle>
    {
        public int NumOfSubstepsPS { get; set; }

        /// <summary>
        /// Contribution of this axle to the total tractive force
        /// </summary>
        public float TractiveForceFraction;
        /// <summary>
        /// Contribution of this axle to the total brake force
        /// </summary>
        public float BrakeForceFraction;

        /// <summary>
        /// Positive only brake force to the individual axle, in Newtons
        /// </summary>
        public float BrakeRetardForceN;

        /// <summary>
        /// Positive only damping force to the axle, in Newton-second
        /// </summary>
        public float DampingNs;

        int count;

        /// <summary>
        /// Positive only friction force to the axle, in Newtons
        /// </summary>
        public float FrictionN;

        /// <summary>
        /// Axle drive type covered by DriveType interface
        /// </summary>
        public AxleDriveType DriveType;

        /// <summary>
        /// Axle drive represented by a motor, covered by ElectricMotor interface
        /// </summary>
        ElectricMotor motor;
        /// <summary>
        /// Read/Write Motor drive parameter.
        /// With setting a value the totalInertiaKgm2 is updated
        /// </summary>
        public ElectricMotor Motor
        {
            set
            {
                motor = value;
                DriveType = motor != null ? AxleDriveType.MotorDriven : AxleDriveType.ForceDriven;
                switch(DriveType)
                {
                    case AxleDriveType.MotorDriven:
                        totalInertiaKgm2 = inertiaKgm2 + transmissionRatio * transmissionRatio * motor.InertiaKgm2;
                        break;
                    default:
                        totalInertiaKgm2 = inertiaKgm2;
                        break;
                }
            }
            get
            {
                return motor;
            }

        }
        /// <summary>
        /// Drive force used to pass the force directly to the axle without gearbox, in Newtons
        /// </summary>
        public float DriveForceN;
        public float DrivePowerW => DriveForceN * (float)AxleSpeedMpS;

        /// <summary>
        /// Sum of inertia over all axle conected rotating mass, in kg.m^2
        /// </summary>
        float totalInertiaKgm2;

        /// <summary>
        /// Axle inertia covered by InertiaKgm2 interface, in kg.m^2
        /// </summary>
        float inertiaKgm2;
        /// <summary>
        /// Read/Write positive non zero only axle inertia, in kg.m^2
        /// By setting this parameter the totalInertiaKgm2 is updated
        /// Throws exception when zero or negative value is passed
        /// </summary>
        public float InertiaKgm2
        {
            set
            {
                if (value <= 0.0)
                    throw new NotSupportedException("Inertia must be greater than zero");
                
                inertiaKgm2 = value;
                switch (DriveType)
                {
                    case AxleDriveType.NotDriven:
                        break;
                    case AxleDriveType.MotorDriven:
                        totalInertiaKgm2 = inertiaKgm2 + transmissionRatio * transmissionRatio * motor.InertiaKgm2;
                        break;
                    case AxleDriveType.ForceDriven:
                        totalInertiaKgm2 = inertiaKgm2;
                        break;
                    default:
                        totalInertiaKgm2 = inertiaKgm2;
                        break;
                }
            }
            get 
            {
                return inertiaKgm2;
            }
        }

        /// <summary>
        /// Pre-calculation of r^2/I
        /// </summary>
        float forceToAccelerationFactor;

        /// <summary>
        /// Pre-calculation of slip characteristics at 0 slip speed
        /// </summary>
        double axleStaticForceN;

        /// <summary>
        /// Transmission ratio on gearbox covered by TransmissionRatio interface
        /// </summary>
        float transmissionRatio;
        /// <summary>
        /// Read/Write positive nonzero transmission ratio, given by n1:n2 ratio
        /// Throws an exception when negative or zero value is passed
        /// </summary>
        public float TransmissionRatio
        {
            set
            {
                if (value <= 0.0)
                    throw new NotSupportedException("Transmission ratio must be greater than zero");
                transmissionRatio = value;
            }
            get
            {
                return transmissionRatio;
            }
        }

        /// <summary>
        /// Transmission efficiency, relative to 1.0, covered by TransmissionEfficiency interface
        /// </summary>
        float transmissionEfficiency;
        /// <summary>
        /// Read/Write transmission efficiency, relative to 1.0, within range of 0.0 to 1.0 (1.0 means 100%, 0.5 means 50%)
        /// Throws an exception when out of range value is passed
        /// When 0.0 is set the value of 0.99 is used instead
        /// </summary>
        public float TransmissionEfficiency
        {
            set
            {
                if (value > 1.0f)
                    throw new NotSupportedException("Value must be within the range of 0.0 and 1.0");
                if (value <= 0.0f)
                    transmissionEfficiency = 1.0f;
                else
                    transmissionEfficiency = value;
            }
            get
            {
                return transmissionEfficiency;
            }
        }

        /// <summary>
        /// Radius of wheels connected to axle
        /// </summary>
        public float WheelRadiusM;

        /// <summary>
        /// Wheel number
        /// </summary>
  //      public int NumberWheelAxles;

        /// <summary>
        /// Wheel mass parameter in kilograms
        /// </summary>
        public float WheelWeightKg;

        /// <summary>
        /// Initial Wheel mass parameter in kilograms, is the reference against which the dynamic wheel weight is calculated.
        /// </summary>
        public float InitialDrvWheelWeightKg;
        
        /// <summary>
        /// Flange angle wheels connected to axle
        /// </summary>
        public float WheelFlangeAngleRad;

        /// <summary>
        /// Gauge of Track
        /// </summary>
        public float WheelDistanceGaugeM;

        /// <summary>
        /// Radius of Track Curve
        /// </summary>
        public float CurrentCurveRadiusM;

        /// <summary>
        /// Gradient of Track
        /// </summary>
        public float CurrentElevationPercent;

        /// <summary>
        /// Force on axle due to gradient of track
        /// </summary>
        public float AxleGradientForceN;

        /// <summary>
        /// Bogie Rigid Wheel Base - distance between wheel in the bogie
        /// </summary>
        public float BogieRigidWheelBaseM;

        /// <summary>
        /// Number of axles in group of wheels, in some instance this might be a mix of drive and non-drive axles
        /// </summary>
        public float NumWheelsetAxles;

        /// <summary>
        /// Static adhesion coefficient, as given by Curtius-Kniffler formula
        /// </summary>
        public float AdhesionLimit;

        /// <summary>
        /// Indicates whether the axle is running on a rack railway
        /// </summary>
        public bool IsRackRailway;

        /// <summary>
        /// Indicates whether the axle is operating on a rack railway
        /// </summary>
        public bool IsRackRailwayOperational;

        /// <summary>
        /// Gear factor to increase drive force if rack locomotive
        /// </summary>
        public float CogWheelGearFactor;

        /// <summary>
        /// Indicates that a Cog wheel is fitted to the axle set to provide better braking on rack railways
        /// </summary>
        public bool BrakingCogWheelFitted;
        public bool brakingCogRead = false;

        /// <summary>
        /// Indicates the type of traction that the axle has with the track, which determines the method of calculating 
        /// the drive force and adhesion limit for the axle
        /// </summary>
        public enum AxleRailTractionTypes
        {
            Unknown,
            Rack,           // axle has may have a cog wheel, but is not necessarily running on a rack railway
            Rack_Adhesion,  // axles has a cog wheel and adhesion wheels on same axle
            Adhesion       // defaults to adhesion
        }

        public AxleRailTractionTypes AxleRailTractionType;


        /// <summary>
        /// Static adhesion coefficient, as given by Curtius-Kniffler formula, at zero speed, ie UMax
        /// </summary>
        public float CurtiusKnifflerZeroSpeed;

        /// <summary>
        /// Wheel adhesion as calculated by Polach
        /// </summary>
        public float WheelAdhesion;

        /// <summary>
        /// Maximum wheel adhesion as calculated by Polach at the slip threshold speed
        /// </summary>
        public float MaximumWheelAdhesion;

        /// <summary>
        /// Correction parameter of adhesion, it has proportional impact on adhesion limit
        /// Should be set to 1.0 for most cases
        /// </summary>
        public float AdhesionK = 0.7f;

        /// <summary>
        /// Axle speed value, in metric meters per second
        /// </summary>
        public double AxleSpeedMpS { get; set; }

        /// <summary>
        /// Axle angular position in radians
        /// </summary>
        public double AxlePositionRad { get; private set; }

        /// <summary>
        /// Read only axle force value, in Newtons
        /// </summary>
        public float AxleForceN { get; private set; }
        public float AxlePowerW => AxleForceN * (float)AxleSpeedMpS;
        /// <summary>
        /// Component of the total axle out force caused by motional forces (drive force and inertia)
        /// </summary>
        public float AxleMotiveForceN { get; protected set; }
        public float AxleMotivePowerW => AxleMotiveForceN * (float)AxleSpeedMpS;
        /// <summary>
        /// Component of the total axle out force caused by brake force
        /// </summary>
        public float AxleBrakeForceN { get; protected set; }
        public float AxleBrakePowerW => AxleBrakeForceN * (float)AxleSpeedMpS;
        /// <summary>
        /// Component of the total axle out force caused by rolling friction and heat dissipated at the wheel-rail interface
        /// </summary>
        public float AxleFrictionForceN { get; protected set; }
        public float AxleFrictionPowerW => AxleFrictionForceN * (float)AxleSpeedMpS;

        /// <summary>
        /// Read/Write axle weight parameter in Newtons
        /// </summary>
        public float AxleWeightN;

        /// <summary>
        /// Read/Write train speed parameter in metric meters per second
        /// </summary>
        public float TrainSpeedMpS;

        /// <summary>
        /// Wheel slip indicator
        /// - is true when absolute value of SlipSpeedMpS is greater than WheelSlipThresholdMpS, otherwise is false
        /// </summary>
        public bool HuDIsWheelSlip { get; private set; }
        public bool IsWheelSlip { get; private set; }
        float WheelSlipTimeS;
        public float WheelSlipThresholdTimeS = 1;

        /// <summary>
        /// Wheelslip threshold value used to indicate maximal effective slip
        /// - its value is computed as a maximum of slip function
        ///   maximum can be found as a derivation f'(dV) = 0
        /// </summary>
        public float WheelSlipThresholdMpS;
        public void ComputeWheelSlipThresholdMpS()
        {
            // Bisection algorithm. We assume adhesion maximum is between 0 (0.005) and 4 m/s
            double a = 0.005f;
            double b = 4;
            // We have to find the zero of the derivative of adhesion curve
            // i.e. the point where slope changes from positive (adhesion region)
            // to negative (slip region)
            double dx = 0.001;
            double fa = SlipCharacteristicsPolach(a + dx) - SlipCharacteristicsPolach(a);
            double fb = SlipCharacteristicsPolach(b + dx) - SlipCharacteristicsPolach(b);

            double SlipSpeedMpS = AxleSpeedMpS - TrainSpeedMpS;

            if (SlipSpeedMpS == 0)
            {
                // For display purposes threshold = 0 when no slip speed
                WheelSlipThresholdMpS = 0;
                return;
            }

            if (fa * fb > 0)
            {
                // If sign does not change, bisection fails
                WheelSlipThresholdMpS = MpS.FromKpH(0.1f);
                return;
            }
            while (Math.Abs(b - a) > MpS.FromKpH(0.1f))
            {
                double c = (a + b) / 2;
                double fc = SlipCharacteristicsPolach(c + dx) - SlipCharacteristicsPolach(c);
                if (fa * fc > 0)
                {
                    a = c;
                    fa = fc;
                }
                else
                {
                    b = c;
                }
            }
            WheelSlipThresholdMpS = (float)Math.Max((a + b) / 2, MpS.FromKpH(0.1f));
        }

        /// <summary>
        /// Wheelslip warning indication
        /// - is true when SlipSpeedMpS is greater than zero and 
        ///   SlipSpeedPercent is greater than SlipWarningThresholdPercent in both directions,
        ///   otherwise is false
        /// </summary>
        public bool HuDIsWheelSlipWarning { get; private set; }
        public bool IsWheelSlipWarning { get; private set; }
        float WheelSlipWarningTimeS;
        public float WheelSlipWarningThresholdTimeS = 1;

        /// <summary>
        /// Read only slip speed value in metric meters per second
        /// - computed as a substraction of axle speed and train speed
        /// </summary>
        public float SlipSpeedMpS
        {
            get
            {
                return Math.Abs((float)(AxleSpeedMpS - TrainSpeedMpS));
            }
        }

        /// <summary>
        /// Read only relative slip speed value, in percent
        /// - the value is relative to WheelSlipThreshold value
        /// </summary>
        public float SlipSpeedPercent
        {
            get
            {
                var temp = (SlipSpeedMpS / WheelSlipThresholdMpS) * 100.0f;
                if (float.IsNaN(temp)) temp = 0;//avoid NaN on HuD display when first starting OR
                return Math.Abs(temp);
            }
        }

        /// <summary>
        /// Percentage of wheelslip (different models use different observables to calculate it)
        /// </summary>
        public float SlipPercent;

        /// <summary>
        /// Slip speed rate of change value, in metric (meters per second) per second
        /// </summary>
        protected float slipDerivationMpSS;
        /// <summary>
        /// Slip speed memorized from previous iteration
        /// </summary>
        protected float previousSlipSpeedMpS;
        /// <summary>
        /// Read only slip speed rate of change, in metric (meters per second) per second
        /// </summary>
        public float SlipDerivationMpSS
        {
            get
            {
                return slipDerivationMpSS;
            }
        }

        /// <summary>
        /// Relative slip rate of change
        /// </summary>
        protected float slipDerivationPercentpS;
        /// <summary>
        /// Relativ slip speed from previous iteration
        /// </summary>
        protected float previousSlipPercent;
        /// <summary>
        /// Read only relative slip speed rate of change, in percent per second
        /// </summary>
        public float SlipDerivationPercentpS
        {
            get
            {
                return slipDerivationPercentpS;
            }
        }

        double integratorError;
        int waitBeforeSpeedingUp;
        int waitBeforeChangingRate;

        /// <summary>
        /// Read/Write relative slip speed warning threshold value, in percent of maximal effective slip
        /// </summary>
        public float SlipWarningTresholdPercent { set; get; }

        PolachCalculator Polach;

        public List<string> AnimatedParts = new List<string>();

        public readonly TrainCar Car;

        /// <summary>
        /// Nonparametric constructor of Axle class instance
        /// - sets motor parameter to null
        /// - sets TtransmissionEfficiency to 1.0 (100%)
        /// - sets SlipWarningThresholdPercent to 70%
        /// - sets axle DriveType to ForceDriven
        /// - updates totalInertiaKgm2 parameter
        /// </summary>
        public Axle(TrainCar car)
        {
            Car = car;
            transmissionEfficiency = 1.0f;
            SlipWarningTresholdPercent = 70.0f;
            DriveType = AxleDriveType.ForceDriven;
            totalInertiaKgm2 = inertiaKgm2;
            Polach = new PolachCalculator(this);
        }
        public void Initialize()
        {
            motor?.Initialize();
        }
        public void InitializeMoving()
        {
            AxleSpeedMpS = TrainSpeedMpS;
            motor?.InitializeMoving();
        }
        public void Parse(STFReader stf)
        {
            stf.MustMatch("(");
            while (!stf.EndOfBlock())
            {
                switch (stf.ReadItem().ToLower())
                {
                    case "brakingcogwheelfitted":
                        BrakingCogWheelFitted = stf.ReadBoolBlock(false);
                        brakingCogRead = true;
                        break;
                    case "ortsradius":
                        WheelRadiusM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null);
                        break;
                    case "ortsflangeangle":
                        WheelFlangeAngleRad = stf.ReadFloatBlock(STFReader.UNITS.Angle, null);
                        break;
                    case "numberwheelsetaxles":
                        NumWheelsetAxles = stf.ReadIntBlock(null);
                        break;
                    case "ortsinertia":
                        InertiaKgm2 = stf.ReadFloatBlock(STFReader.UNITS.RotationalInertia, null);
                        break;
                    case "weight":
                        WheelWeightKg = stf.ReadFloatBlock(STFReader.UNITS.Mass, null);
                        AxleWeightN = 9.81f * WheelWeightKg;
                        break;
                    case "animatedparts":
                        foreach (var part in stf.ReadStringBlock("").ToUpper().Replace(" ", "").Split(','))
                        {
                            if (part != "") AnimatedParts.Add(part);
                        }
                        break;
                    case "axlerailtractiontype":
                     //   stf.MustMatch("(");
                        var locomotiveTractionType = stf.ReadStringBlock("");
                        try
                        {
                            AxleRailTractionType = (AxleRailTractionTypes)Enum.Parse(typeof(AxleRailTractionTypes), locomotiveTractionType);
                        }
                        catch
                        {
                                STFException.TraceWarning(stf, "Assumed unknown Locomotive drive type " + locomotiveTractionType);
                        }
                        break;
                    case "(":
                        stf.SkipRestOfBlock();
                        break;
                }
            }
        }
        public void Copy(Axle other)
        {
            BrakingCogWheelFitted = other.BrakingCogWheelFitted;
            brakingCogRead = other.brakingCogRead;
            WheelRadiusM = other.WheelRadiusM;
            WheelFlangeAngleRad = other.WheelFlangeAngleRad;
            NumWheelsetAxles = other.NumWheelsetAxles;
            InertiaKgm2 = other.InertiaKgm2;
            WheelWeightKg = other.WheelWeightKg;
            AxleWeightN = other.AxleWeightN;
            AxleRailTractionType = other.AxleRailTractionType;
            AnimatedParts.Clear();
            AnimatedParts.AddRange(other.AnimatedParts);
        }

        /// <summary>
        /// Restores the game state.
        /// </summary>
        /// <param name="inf">The save stream to read from.</param>
        public void Restore(BinaryReader inf)
        {
            previousSlipPercent = inf.ReadSingle();
            previousSlipSpeedMpS = inf.ReadSingle();
            AxleForceN = inf.ReadSingle();
            AxleSpeedMpS = inf.ReadDouble();
            NumOfSubstepsPS = inf.ReadInt32();
            integratorError = inf.ReadDouble();
        }

        /// <summary>
        /// Save the game state.
        /// </summary>
        /// <param name="outf">The save stream to write to.</param>
        public void Save(BinaryWriter outf)
        {
            outf.Write(previousSlipPercent);
            outf.Write(previousSlipSpeedMpS);
            outf.Write(AxleForceN);
            outf.Write(AxleSpeedMpS);
            outf.Write(NumOfSubstepsPS);
            outf.Write(integratorError);
        }

        /// <summary>
        /// Compute variation in axle dynamics. Calculates axle speed, axle angular position and in/out forces.
        /// </summary>
        public (double accelMpSS, double angSpeedRadpS, double driveForceN, double axleMotiveForceN, double axleBrakeForceN, double axleFrictionForceN) GetAxleMotionVariation(double axleSpeedMpS, double elapsedClockSeconds)
        {
            if (double.IsNaN(axleSpeedMpS)) axleSpeedMpS = 0; // TODO: axleSpeedMpS should always be a number, find the cause of the NaN
            double slipSpeedMpS = axleSpeedMpS - TrainSpeedMpS;
            // Compute force transmitted to rail according to adhesion curves
            double axleOutForceN;

            if (Axles.UsePolachAdhesion)
            {
                axleOutForceN = Math.Sign(slipSpeedMpS) * AxleGradientForceN * SlipCharacteristicsPolach(slipSpeedMpS);
            }
            else
            {
                axleOutForceN = AxleGradientForceN * SlipCharacteristicsPacha((float)axleSpeedMpS - TrainSpeedMpS, TrainSpeedMpS, AdhesionK, AdhesionLimit);
            }

            // Compute force produced by the engine
            double axleInForceN = 0;
            if (DriveType == AxleDriveType.ForceDriven)
                axleInForceN = DriveForceN * transmissionEfficiency;
            else if (DriveType == AxleDriveType.MotorDriven)
                axleInForceN = motor.GetDevelopedTorqueNm(axleSpeedMpS * transmissionRatio / WheelRadiusM) * transmissionEfficiency / WheelRadiusM;

            double frictionForceN = FrictionN + DampingNs * Math.Abs(slipSpeedMpS); // Rolling friction
            double totalFrictionForceN = BrakeRetardForceN + frictionForceN; // Dissipative forces: they will never increase wheel speed
            double internalForceN = axleInForceN - Math.Sign(axleSpeedMpS) * totalFrictionForceN; // All forces except rail
            double accelerationMpSS = (internalForceN - axleOutForceN) * forceToAccelerationFactor; // Resulting acceleration from the sum of all forces (including rail)
            // Rail force is divided in 3 components: due to engine (motive), brake and axle friction
            // Usually only the total rail force is relevant for train physics, splitted in 3 for debug (HUD) purposes
            // However, distinguishing between motive and frictional forces is required to start from 0 speed, as motion force must exceed friction losses, otherwise speed stays at 0
            double axleMotiveForceN = axleInForceN;
            double axleBrakeForceN = BrakeRetardForceN;
            // Case when axle speed == 0, drive+rail forces must overcome friction to start moving the wheel
            // For Polach, axleOutForceN is bad-behaved for low train speeds. Thus, for low speeds, since axle speed == 0 in this clause, consider out force to be zero. Otherwise use the real out force.
            if (Math.Abs(axleInForceN - (axleStaticForceN > 0 && Math.Abs(slipSpeedMpS) < 0.1f ? 0 : axleOutForceN)) < totalFrictionForceN && (Math.Abs(axleSpeedMpS) < 0.001f || Math.Sign(axleSpeedMpS + accelerationMpSS * elapsedClockSeconds) != Math.Sign(axleSpeedMpS)))
            {
                // Require that wheel speed stays at 0
                accelerationMpSS = -axleSpeedMpS / elapsedClockSeconds;
                // The car can also start moving when braked if external forces (e.g. gravity, coupler) overcome the static adhesion coefficient
                // Ensure that axle static friction never exceeds the adhesion coefficient, so the car will start moving even when wheels are stuck
                if (Math.Abs(slipSpeedMpS) < 0.1f)
                {
                    axleBrakeForceN = Math.Min(BrakeRetardForceN, Math.Max(MaximumWheelAdhesion * AxleGradientForceN - frictionForceN + Math.Abs(axleInForceN), 0));
                    return (accelerationMpSS, axleSpeedMpS / WheelRadiusM, axleInForceN / transmissionEfficiency, axleMotiveForceN, axleBrakeForceN, frictionForceN);
                }
            }
            // In the static adhesion regime (low speeds for Polach formula), unless the static adhesion coefficient is exceeded,
            // all forces (including rail output force) are balanced, so slip speed stays at 0
            else if (Math.Abs(internalForceN) < axleStaticForceN
                    && (Math.Abs(slipSpeedMpS) < 0.001f || Math.Sign(slipSpeedMpS) != Math.Sign(slipSpeedMpS + accelerationMpSS * elapsedClockSeconds)))
            {
                accelerationMpSS = -slipSpeedMpS / elapsedClockSeconds;
                axleOutForceN = internalForceN - accelerationMpSS / forceToAccelerationFactor;
            }
            // By default, differences between axle in and out forces (caused by axle inertia and slip), are accounted as part of the motive component
            // If the car is braking, they are accounted in the brake force component (e.g. during wheel skid, AxleBrakeForceN will be reduced with respect to BrakeRetardForceN)
            if (Math.Abs(axleInForceN) < BrakeRetardForceN && TrainSpeedMpS != 0)
            {
                axleBrakeForceN = Math.Sign(TrainSpeedMpS) * (-axleOutForceN + axleInForceN) - frictionForceN;
            }
            else
            {
                axleMotiveForceN = axleOutForceN + Math.Sign(TrainSpeedMpS) * totalFrictionForceN;
            }
            return (accelerationMpSS, axleSpeedMpS / WheelRadiusM, axleInForceN / transmissionEfficiency, axleMotiveForceN, axleBrakeForceN, frictionForceN);
        }

        /// <summary>
        /// Integrates the wheel rotation movement using a RK4 method,
        /// calculating the required number of substeps
        /// To maintain the accuracy of the integration method, the number of substeps needs to increase when slip speed approaches the slip threshold speed.
        /// The following section attempts to calculate the optimal substep limit. This is a trade off between the accuracy of the slips calculations and the CPU load which impacts the screen FPS
        /// Outputs: wheel speed, wheel angular position and motive force
        /// </summary>
        void Integrate(float elapsedClockSeconds)
        {
            if (elapsedClockSeconds <= 0) return;
            double prevSpeedMpS = AxleSpeedMpS;

            if (Axles.UsePolachAdhesion)
            {

                float upperSubStepLimit = 100;
                float lowerSubStepLimit = 1;

                // use straight line graph approximation to increase substeps as slipspeed increases towards the threshold speed point
                // Points are 1 = (0, upperLimit) and 2 = (threshold, lowerLimit)           
                var AdhesGrad = ((upperSubStepLimit - lowerSubStepLimit) / (WheelSlipThresholdMpS - 0));
                var targetNumOfSubstepsPS = Math.Abs((AdhesGrad * SlipSpeedMpS) + lowerSubStepLimit);
                if (float.IsNaN((float)targetNumOfSubstepsPS)) targetNumOfSubstepsPS = 1;

                if (SlipSpeedPercent > 100) // if in wheel slip then maximise the substeps
                {
                    targetNumOfSubstepsPS = upperSubStepLimit;
                }

                if (Math.Abs(integratorError) < 0.000277 && SlipSpeedPercent < 25 && Math.Abs(SlipSpeedMpS) < Math.Abs(previousSlipSpeedMpS))
                {
                    if (--waitBeforeChangingRate <= 0) //wait for a while before changing the integration rate
                    {
                        NumOfSubstepsPS -= 2; // decrease substeps when under low slip conditions
                        waitBeforeChangingRate = 30;
                    }
                }
                else if (targetNumOfSubstepsPS > NumOfSubstepsPS) // increase substeps
                {
                    if (--waitBeforeChangingRate <= 0) //wait for a while before changing the integration rate
                    {

                        if (SlipSpeedPercent > 70 || Math.Abs(SlipSpeedMpS) > Math.Abs(previousSlipSpeedMpS))
                        {
                            // this speeds up the substep increase if the slip speed approaches the threshold or has exceeded it, ie "critical conditions".
                            NumOfSubstepsPS += 10;
                            waitBeforeChangingRate = 5;
                        }
                        else
                        {
                            // this speeds ups the substeps under "non critical" conditions
                            NumOfSubstepsPS += 3;
                            waitBeforeChangingRate = 30;
                        }

                    }
                }
                else if (targetNumOfSubstepsPS < NumOfSubstepsPS) // decrease sub steps
                {
                    if (--waitBeforeChangingRate <= 0) //wait for a while before changing the integration rate
                    {
                        NumOfSubstepsPS -= 3;
                        waitBeforeChangingRate = 30;
                    }
                }

                // keeps the substeps to a relevant upper and lower limits
                if (NumOfSubstepsPS < lowerSubStepLimit)
                    NumOfSubstepsPS = (int)lowerSubStepLimit;

                if (NumOfSubstepsPS > upperSubStepLimit)
                    NumOfSubstepsPS = (int)upperSubStepLimit;

            }
            else
            {
                if (Math.Abs(integratorError) > Math.Max((Math.Abs(SlipSpeedMpS) - 1) * 0.01f, 0.001f))
                {
                    ++NumOfSubstepsPS;
                    waitBeforeSpeedingUp = 100;
                }
                else
                {
                    if (--waitBeforeSpeedingUp <= 0)    //wait for a while before speeding up the integration
                    {
                        --NumOfSubstepsPS;
                        waitBeforeSpeedingUp = 10;      //not so fast ;)
                    }
                }

                NumOfSubstepsPS = Math.Max(Math.Min(NumOfSubstepsPS, 50), 1);
            }

            double dt = elapsedClockSeconds / NumOfSubstepsPS;
            double hdt = dt / 2;
            double driveForceSumN = 0;
            double axleMotiveForceSumN = 0;
            double axleBrakeForceSumN = 0;
            double axleFrictionForceSumN = 0;
            for (int i = 0; i < NumOfSubstepsPS; i++)
            {
                var k1 = GetAxleMotionVariation(AxleSpeedMpS, dt);

                if (i == 0 && !Axles.UsePolachAdhesion)
                {
                    if (k1.Item1 * dt > Math.Max((Math.Abs(SlipSpeedMpS) - 1) * 10, 1) / 100)
                    {
                        NumOfSubstepsPS = Math.Min(NumOfSubstepsPS + 5, 50);
                        dt = elapsedClockSeconds / NumOfSubstepsPS;
                        hdt = dt / 2;
                    }
                }

                var k2 = GetAxleMotionVariation(AxleSpeedMpS + k1.accelMpSS * hdt, hdt);
                var k3 = GetAxleMotionVariation(AxleSpeedMpS + k2.accelMpSS * hdt, hdt);
                var k4 = GetAxleMotionVariation(AxleSpeedMpS + k3.accelMpSS * dt, dt);

                AxleSpeedMpS += (integratorError = (k1.accelMpSS + 2 * (k2.accelMpSS + k3.accelMpSS) + k4.accelMpSS) * dt / 6);
                AxlePositionRad += (k1.angSpeedRadpS + 2 * (k2.angSpeedRadpS + k3.angSpeedRadpS) + k4.angSpeedRadpS) * dt / 6;
                driveForceSumN += (k1.driveForceN + 2 * (k2.driveForceN + k3.driveForceN) + k4.driveForceN);
                axleMotiveForceSumN += (k1.axleMotiveForceN + 2 * (k2.axleMotiveForceN + k3.axleMotiveForceN) + k4.axleMotiveForceN);
                axleBrakeForceSumN += (k1.axleBrakeForceN + 2 * (k2.axleBrakeForceN + k3.axleBrakeForceN) + k4.axleBrakeForceN);
                axleFrictionForceSumN += (k1.axleFrictionForceN + 2 * (k2.axleFrictionForceN + k3.axleFrictionForceN) + k4.axleFrictionForceN);
            }
            DriveForceN = (float)(driveForceSumN / (NumOfSubstepsPS * 6));
            AxleMotiveForceN = (float)(axleMotiveForceSumN / (NumOfSubstepsPS * 6));
            AxleBrakeForceN = (float)(axleBrakeForceSumN / (NumOfSubstepsPS * 6));
            AxleFrictionForceN = (float)(axleFrictionForceSumN / (NumOfSubstepsPS * 6));
            if (Math.Abs(TrainSpeedMpS) < 0.001f && Math.Abs(AxleMotiveForceN) < AxleBrakeForceN + AxleFrictionForceN) AxleForceN = 0;
            else AxleForceN = (float)(AxleMotiveForceN - Math.Sign(TrainSpeedMpS) * (AxleBrakeForceN + AxleFrictionForceN));
            AxlePositionRad = MathHelper.WrapAngle((float)AxlePositionRad);
        }

        /// <summary>
        /// Main Update method
        /// - computes slip characteristics to get new axle force
        /// - computes axle dynamic model according to its driveType
        /// - computes wheelslip indicators
        /// </summary>
        /// <param name="elapsedSeconds"></param>
        public virtual void Update(float elapsedSeconds)
        {
            if (float.IsNaN(TrainSpeedMpS)) TrainSpeedMpS = 0; // TODO: TrainSpeedMpS should always be a number, find the cause of the NaN
            if (double.IsNaN(AxleSpeedMpS)) AxleSpeedMpS = 0; // TODO: AxleSpeedMpS should always be a number, find the cause of the NaN

            // Calculate factor to reduce adhesion due to track gradient
            float gradeAngle = (float)Math.Atan(Math.Abs(CurrentElevationPercent / 100.0f));
            AxleGradientForceN = AxleWeightN * (float)Math.Cos(gradeAngle);
            AxleGradientForceN = MathHelper.Clamp(AxleGradientForceN, 0, AxleWeightN);

            bool advancedAdhesion = Car is MSTSLocomotive locomotive && locomotive.AdvancedAdhesionModel;
            advancedAdhesion &= DriveType != AxleDriveType.NotDriven; // Skip integrator for undriven axles to save CPU
            forceToAccelerationFactor = WheelRadiusM * WheelRadiusM / totalInertiaKgm2;
            if (!advancedAdhesion)
            {
                MaximumWheelAdhesion = AdhesionLimit;
                axleStaticForceN = 0;
                WheelSlipThresholdMpS = 0;
            }
            else if (Axles.UsePolachAdhesion)
            {
                Polach.Update();
                axleStaticForceN = AxleGradientForceN * SlipCharacteristicsPolach(0);
                ComputeWheelSlipThresholdMpS();
                WheelAdhesion = (float)SlipCharacteristicsPolach(SlipSpeedMpS);
                MaximumWheelAdhesion = (float)SlipCharacteristicsPolach(WheelSlipThresholdMpS);

#if DEBUG_ADHESION
                if (count < 6 && count++ == 5)
                {
                    TrainSpeedMpS = 10 / 3.6f;
                    Polach.Update();
                    axleStaticForceN = AxleWeightN * SlipCharacteristicsPolach(0);
                }
#endif
            }
            else
            {
                // Set values for Pacha adhesion
                WheelSlipThresholdMpS = MpS.FromKpH(AdhesionK / AdhesionLimit);
                WheelAdhesion = Math.Abs(SlipCharacteristicsPacha(SlipSpeedMpS, TrainSpeedMpS, AdhesionK, AdhesionLimit));
                MaximumWheelAdhesion = AdhesionLimit;
                axleStaticForceN = 0;
            }

#if DEBUG_ADHESION
            double[] spd = new double[50];
            double[] adh = new double[50];
            for (int i = 0; i < spd.Length; i++)
            {
                spd[i] = i / (float)spd.Length;
                adh[i] = SlipCharacteristics(spd[i]);
            }
            for (int i = 0; i < spd.Length; i++)
            {
                Console.Write(spd[i]);
                Console.Write(" ");
            }
            Console.WriteLine("");
            Console.WriteLine("");
            for (int i = 0; i < spd.Length; i++)
            {
                Console.Write(adh[i]);
                Console.Write(" ");
            }
            Console.WriteLine("");
#endif

            motor?.Update(elapsedSeconds);

            if (advancedAdhesion && !IsRackRailwayOperational)
            {
                Integrate(elapsedSeconds);

                SlipPercent = SlipSpeedPercent;

                if (elapsedSeconds > 0.0f)
                {
                    slipDerivationMpSS = (SlipSpeedMpS - previousSlipSpeedMpS) / elapsedSeconds;
                    previousSlipSpeedMpS = SlipSpeedMpS;

                    slipDerivationPercentpS = (SlipSpeedPercent - previousSlipPercent) / elapsedSeconds;
                    previousSlipPercent = SlipSpeedPercent;
                }
            }
            else
            {
                UpdateSimpleAdhesion(elapsedSeconds);
            }
            if ((SlipPercent > (Car is MSTSLocomotive loco && loco.SlipControlSystem == MSTSLocomotive.SlipControlType.Full && Math.Abs(DriveForceN) > BrakeRetardForceN ? (200 - SlipWarningTresholdPercent) : 100)))
            {
                // Wheel slip internally happens instantaneously, but may correct itself in a short period, so HuD indication has a small time delay to eliminate "false" indications
                IsWheelSlip = IsWheelSlipWarning = true;

                // Wait some time before indicating the HuD wheelslip to avoid false triggers
                if (WheelSlipTimeS > WheelSlipThresholdTimeS)
                {
                    HuDIsWheelSlip = HuDIsWheelSlipWarning = true;
                }
                WheelSlipTimeS += elapsedSeconds;
            }
            else if (SlipPercent > SlipWarningTresholdPercent)
            {
                // Wheel slip internally happens instantaneously, but may correct itself in a short period, so HuD indication has a small time delay to eliminate "false" indications
                IsWheelSlipWarning = true;
                IsWheelSlip = false;

                // Wait some time before indicating wheelslip to avoid false triggers
                if (WheelSlipWarningTimeS > WheelSlipWarningThresholdTimeS) HuDIsWheelSlipWarning = true;
                HuDIsWheelSlip = false;
                WheelSlipWarningTimeS += elapsedSeconds;
            }
            else
            {
                HuDIsWheelSlipWarning = false;
                HuDIsWheelSlip = false;
                IsWheelSlipWarning = false;
                IsWheelSlip = false;
                WheelSlipWarningTimeS = WheelSlipTimeS = 0;
            }

        }

        public void UpdateSimpleAdhesion(float elapsedClockSeconds)
        {
            float axleInForceN = 0;        
            if (DriveType == AxleDriveType.ForceDriven)
                axleInForceN = DriveForceN * transmissionEfficiency;
            else if (DriveType == AxleDriveType.MotorDriven)
                axleInForceN = (float)motor.GetDevelopedTorqueNm(TrainSpeedMpS * transmissionRatio / WheelRadiusM) * transmissionEfficiency / WheelRadiusM;
            DriveForceN = axleInForceN / transmissionEfficiency;

            float frictionForceN = FrictionN;
            float totalFrictionForceN = BrakeRetardForceN + frictionForceN; // Dissipative forces: they will never increase wheel speed
            float totalAxleForceN = axleInForceN - Math.Sign(TrainSpeedMpS) * totalFrictionForceN;
            float axleOutForceN = totalAxleForceN;
            AxleMotiveForceN = axleInForceN;
            AxleBrakeForceN = BrakeRetardForceN;
            AxleFrictionForceN = frictionForceN;
            AxleSpeedMpS = TrainSpeedMpS;

            // In a slip possible model wheel axle force is limited to adhesion force so that wheel slip will not occur
            float adhesionForceN = AxleGradientForceN * AdhesionLimit;
            SlipPercent = Math.Abs(axleOutForceN) / adhesionForceN * 100;

            if ((Car is MSTSSteamLocomotive steam && !steam.AdvancedAdhesionModel) || (IsRackRailway & ( AxleRailTractionType == AxleRailTractionTypes.Rack || AxleRailTractionType == AxleRailTractionTypes.Rack_Adhesion))) 
            {
                // Do not allow wheelslip on steam locomotives if simple adhesion is selected, or if it is a rack axle
                SlipPercent = 0;
            }
            else if (SlipPercent > 100)
            {
                axleOutForceN = MathHelper.Clamp(axleOutForceN, -adhesionForceN, adhesionForceN);
                // Simple adhesion, simple wheelslip conditions
                if (Car is MSTSLocomotive locomotive && !locomotive.AdvancedAdhesionModel)
                {
                    if (!locomotive.AntiSlip && locomotive.SlipControlSystem != MSTSLocomotive.SlipControlType.Full) axleOutForceN *= locomotive.Adhesion1;
                    else SlipPercent = 100;
                }
                else if (!Car.Simulator.UseAdvancedAdhesion || Car.Simulator.Settings.SimpleControlPhysics || !Car.Train.IsPlayerDriven)
                {
                    // No wagon skid in simple adhesion
                    SlipPercent = 100;
                }
                // Semi-advanced adhesion. Used in non-driven axles when advanced adhesion is enabled, to avoid running the integrator
                else
                {
                    // For non-driven axles, only brake skid is possible (no wheel slip). Consider wheels to be fully locked
                    AxleSpeedMpS = 0;
                    // Use the advanced adhesion coefficient
                    adhesionForceN = AxleGradientForceN * SlipCharacteristicsPacha(SlipSpeedMpS, TrainSpeedMpS, AdhesionK, AdhesionLimit);
                    axleOutForceN = MathHelper.Clamp(axleOutForceN, -adhesionForceN, adhesionForceN);
                }
                // In case of wheel skid, reduce indicated brake force
                if (((TrainSpeedMpS > 0 && axleOutForceN < 0) || (TrainSpeedMpS < 0 && axleOutForceN > 0)) && Math.Abs(axleInForceN) < BrakeRetardForceN && Math.Sign(TrainSpeedMpS) * (-axleOutForceN + axleInForceN) - frictionForceN > 0)
                {
                    AxleBrakeForceN = Math.Sign(TrainSpeedMpS) * (-axleOutForceN + axleInForceN) - frictionForceN;
                }
                // Otherwise, indicate that slip is reducing motive force
                else
                {
                    AxleMotiveForceN = axleOutForceN + Math.Sign(TrainSpeedMpS) * totalFrictionForceN;
                }
            }
            AxlePositionRad += AxleSpeedMpS / WheelRadiusM * elapsedClockSeconds;

            if (Math.Abs(TrainSpeedMpS) < 0.001f && Math.Abs(axleInForceN) < totalFrictionForceN)
            {
                axleOutForceN = 0;
            }
            AxleForceN = axleOutForceN;
        }

        class PolachCalculator
        {
            Axle Axle;
            double StiffnessPreFactors1;
            double StiffnessPreFactors2;
            double polach_A;
            double wheelLoadN;
            double polach_B;
            double polach_Ka;
            double polach_Ks;
            double umax;
            double trainSpeedMpS;
            double spinM1;
            double Sy = 0;
            double Sy2 = 0;
            double Syc = 0;
            double Syc2 = 0;
            double kalker_C11;
            double kalker_C22;
            double zeroSpeedAdhesion;
            double a_HertzianMM;

            public PolachCalculator(Axle axle)
            {
                Axle = axle;
            }
            public void Update()
            {
                umax = Axle.AdhesionLimit;
                trainSpeedMpS = Math.Abs(Axle.TrainSpeedMpS);
                zeroSpeedAdhesion = Axle.CurtiusKnifflerZeroSpeed;

                var wheelRadiusMM = Axle.WheelRadiusM * 1000;
                var wheelDistanceGaugeMM = Axle.WheelDistanceGaugeM * 1000;
                var GNm2 = 8.40E+10;
                // Prevent wheel load from going negative, negative wheel load would indicate wheel has lifted off rail which otherwise leads to NaN errors
                wheelLoadN = Math.Max(Axle.AxleGradientForceN / (Axle.NumWheelsetAxles * 2), 0.1); // Assume two wheels per axle, and thus wheel weight will be half the value - multiple axles????
                var wheelLoadkN = wheelLoadN / 1000;
                var Young_ModulusMPa = 207000;

                // Calculate Hertzian values - assume 2b = 12mm.
                var b_HertzianMM = 6.0;
                a_HertzianMM = (3.04f / 2) * Math.Sqrt(((wheelLoadkN * wheelRadiusMM * 1000) / (2 * b_HertzianMM * Young_ModulusMPa)));
                var a_HertzianM = a_HertzianMM / 1000;

                var hertzianMM = a_HertzianMM / b_HertzianMM;
                var hertzianMM2 = hertzianMM * hertzianMM;
                // Calculate Kalker values
                kalker_C11 = 0.32955 * hertzianMM2 + 0.48538 * hertzianMM + 3.4992;
                kalker_C22 = 0.90909 * hertzianMM2 + 1.2594 * hertzianMM + 2.3853;

                StiffnessPreFactors1 = (GNm2 * Math.PI * a_HertzianMM * b_HertzianMM) / (4.0 * wheelLoadN);

                // Calculate slip and creep values
                var wheelProfileConicityRad = 0.5f;
                var wheelContactAngleRad = MathHelper.ToRadians(3); // Assume that on straight track wheel runs on the tread which has a 1 in 20 slope = 3 deg 
                // At a later date this could be changed when in a curve to run on the flange at a steeper angle.
                var wheelCentreDeviationMM = 3.0f;

                spinM1 = Math.Sin(wheelContactAngleRad) / Axle.WheelRadiusM; // set spin assuming wheel running on tread

                double YawAngleRad = 0;
                if (Axle.CurrentCurveRadiusM > 0)
                {
                    YawAngleRad = Math.Asin(2.0f * Axle.BogieRigidWheelBaseM / Axle.CurrentCurveRadiusM);
                    spinM1 = Math.Sin(Axle.WheelFlangeAngleRad) / Axle.WheelRadiusM; // Overwrite spin if locomotive is on a curve. Assume wheel running on flange
                }

                if (float.IsNaN((float)spinM1)) spinM1 = 0;//avoid NaN when first starting OR

                var supplenessFactor = (wheelDistanceGaugeMM * wheelRadiusMM) / (wheelProfileConicityRad * wheelCentreDeviationMM);
                var lateralSlipVelocityMpS = Math.Abs(((-1 * Axle.CurrentCurveRadiusM * YawAngleRad) / supplenessFactor) * trainSpeedMpS);
                Sy = lateralSlipVelocityMpS / trainSpeedMpS;
                if (float.IsNaN((float)Sy)) Sy = 0;//avoid NaN when first starting OR
                Sy2 = Sy * Sy;

                Syc = Sy + (spinM1 * a_HertzianM);
                Syc2 = Syc * Syc;

                // calculate "standard" Polach adhesion parameters as straight line approximations as u varies - these values are capped at the moment at the u=0.3 level
                // Taking them lower may reduce the stability of the calculations
                polach_A = 0.4;
                polach_B = (1.6 * zeroSpeedAdhesion) - 0.28;
                if (polach_B < 0.2) polach_B = 0.2f;

                polach_Ka = (2.8 * zeroSpeedAdhesion) - 0.54;
                if (polach_Ka < 0.3) polach_Ka = 0.3f;

                polach_Ks = (1.2 * zeroSpeedAdhesion) - 0.26;
                if (polach_Ks < 0.1) polach_Ks = 0.1f;
            }

            public double SlipCharacteristics(double slipSpeedMpS)
            {
                var polach_uadhesion = zeroSpeedAdhesion * (((1 - polach_A) * Math.Exp(-polach_B * slipSpeedMpS)) + polach_A);

                if (trainSpeedMpS < 0.05f)
                    return polach_uadhesion;

                var Sx = slipSpeedMpS / trainSpeedMpS;
                var Sx2 = Sx * Sx;
                var S = Math.Sqrt(Sx2 + Sy2);
                var Sc = Math.Sqrt(Sx2 + Syc2);

                var kalker_Cjj = 1.0;
                if (S != 0) // prevents NaN calculation if slip values are zero
                {
                    var coef1 = kalker_C11 * Sx / S;
                    var coef2 = kalker_C22 * Sy / S;
                    kalker_Cjj = Math.Sqrt(coef1 * coef1 + coef2 * coef2);
                }

                // Calculate adhesion forces
                var Stiffness = StiffnessPreFactors1 * kalker_Cjj * Sc / polach_uadhesion;
                StiffnessPreFactors2 = (2.0f * wheelLoadN) / Math.PI;
                var Stiffness2 = StiffnessPreFactors2 * polach_uadhesion;
                var KaStiffness = polach_Ka * Stiffness;
                var adhesionComponent = KaStiffness / (1 + (polach_Ka * Stiffness * Stiffness));
                var slipComponent = Math.Atan(polach_Ks * Stiffness);
                var f = Stiffness2 * (adhesionComponent + slipComponent);

                var fx = (f * Sx / Sc) / wheelLoadN;

                return fx;
            }
        }

        /// <summary>
        /// ***** Polach Adhesion *****
        /// Uses the Polach creep force curves calculation described in the following document
        ///	"Creep forces in simulations of traction vehicles running on adhesion limit" by O. Polach 2005 Wear - http://www.sze.hu/~szenasy/VILLVONT/polachslipvizsg.pdf
        /// 
        /// To calculate the Polach curves the Hertzian, and Kalker values also need to be calculated as inputs into the Polach formula.
        /// The following paper describes a methodology for calculating these values -
        /// 
        /// "Determination of Wheel-Rail Contact Characteristics by Creating a Special Program for Calculation" by Ioan Sebesan and Yahia Zakaria 
        ///  - https://www.researchgate.net/publication/273303594_Determination_of_Wheel-Rail_Contact_Characteristics_by_Creating_a_Special_Program_for_Calculation
        /// 
        /// </summary>
        /// <param name="slipSpeedMpS">Difference between train speed and wheel speed</param>
        /// <param name="speedMpS">Current speed</param>
        /// <returns>Relative force transmitted to the rail</returns>
        public double SlipCharacteristicsPolach(double slipSpeedMpS)
        {
            slipSpeedMpS = Math.Abs(slipSpeedMpS);
            double fx = Polach.SlipCharacteristics(slipSpeedMpS);
            return fx;
        }


        /// <summary>
        /// ***** Pacha Adhesion *****
        /// Slip characteristics computation
        /// - Uses adhesion limit calculated by Curtius-Kniffler formula:
        ///                 7.5
        ///     umax = ---------------------  + 0.161
        ///             speed * 3.6 + 44.0
        /// - Computes slip speed
        /// - Computes relative adhesion force as a result of slip characteristics:
        ///             2*K*umax^2*dV
        ///     u = ---------------------
        ///           umax^2*dv^2 + K^2
        /// 
        /// For high slip speeds the formula is replaced with an exponentially 
        /// decaying function (with smooth coupling) which reaches 40% of
        /// maximum adhesion at infinity. The transition point between equations
        /// is at dV = sqrt(3)*K/umax (inflection point)
        /// 
        /// </summary>
        /// <param name="slipSpeedMpS">Difference between train speed and wheel speed</param>
        /// <param name="speedMpS">Current speed</param>
        /// <param name="K">Slip speed correction</param>
        /// <param name="umax">Relative weather conditions, usually from 0.2 to 1.0</param>
        /// <returns>Relative force transmitted to the rail</returns>
        public float SlipCharacteristicsPacha(float slipSpeedMpS, float speedMpS, float K, float umax)
        {
            var slipSpeedKpH = MpS.ToKpH(slipSpeedMpS);
            float x = slipSpeedKpH * umax / K; // Slip percentage
            float absx = Math.Abs(x);
            float sqrt3 = (float)Math.Sqrt(3);
            if (absx > sqrt3)
            {
                // At infinity, adhesion is 40% of maximum (Polach, 2005)
                // The value must be lower than 85% for the formula to work
                float inftyFactor = 0.4f;
                return Math.Sign(slipSpeedKpH) * umax * ((sqrt3 / 2 - inftyFactor) * (float)Math.Exp((sqrt3 - absx) / (2 * sqrt3 - 4 * inftyFactor)) + inftyFactor);
            }
            return 2.0f * umax * x / (1 + x * x);
        }


    }
}
