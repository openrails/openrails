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
        /// Get total axle out force with brake force substracted
        /// </summary>
        public float CompensatedForceN
        {
            get
            {
                float forceN = 0;
                foreach (var axle in AxleList)
                {
                    forceN += axle.CompensatedAxleForceN;
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
                                    var axle = new Axle();
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
                var axle = new Axle();
                axle.Copy(ax);
                AxleList.Add(axle);
            }
        }

        public void Initialize()
        {
            ResetTime = Car.Simulator.GameTime;
            foreach (var axle in AxleList)
            {
                if (Car is MSTSLocomotive locomotive)
                {
                    if (axle.InertiaKgm2 <= 0) axle.InertiaKgm2 = locomotive.AxleInertiaKgm2 / AxleList.Count;
                    if (axle.AxleWeightN <= 0) axle.AxleWeightN = 9.81f * locomotive.DrvWheelWeightKg / AxleList.Count;  //remains fixed for diesel/electric locomotives, but varies for steam locomotives
                    if (axle.NumAxles <= 0) axle.NumAxles = locomotive.LocoNumDrvAxles;
                    if (axle.WheelRadiusM <= 0) axle.WheelRadiusM = locomotive.DriverWheelRadiusM;
                    if (axle.WheelFlangeAngleRad <= 0) axle.WheelFlangeAngleRad = locomotive.MaximumWheelFlangeAngleRad;
                    if (axle.DampingNs <= 0) axle.DampingNs = locomotive.MassKG / 1000.0f / AxleList.Count;
                    if (axle.FrictionN <= 0) axle.FrictionN = locomotive.MassKG / 1000.0f / AxleList.Count;
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
                    AxleList.Add(new Axle());
                    AxleList[i].Initialize();
                }
                AxleList[i].Restore(inf);
            }
        }
        /// <summary>
        /// Updates each axle on the list
        /// </summary>
        /// <param name="elapsedClockSeconds">Time span within the simulation cycle</param>
        public void Update(float elapsedClockSeconds)
        {
            foreach (var axle in AxleList)
            {
                axle.Update(elapsedClockSeconds);
            }
        }
        public List<Axle>.Enumerator GetEnumerator()
        {
            return AxleList.GetEnumerator();
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
        /// Positive only brake force to the axle, in Newtons
        /// </summary>
        public float BrakeRetardForceN;

        /// <summary>
        /// Damping force covered by DampingForceN interface
        /// </summary>
        protected float dampingNs;
        /// <summary>
        /// Read/Write positive only damping force to the axle, in Newton-second
        /// </summary>
        public float DampingNs { set { dampingNs = Math.Abs(value); } get { return dampingNs; } }

        int count;

        protected float frictionN;

        public float FrictionN { set { frictionN = Math.Abs(value); } get { return frictionN; } }

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
        /// Drive force covered by DriveForceN interface, in Newtons
        /// </summary>
        protected float driveForceN;
        /// <summary>
        /// Read/Write drive force used to pass the force directly to the axle without gearbox, in Newtons
        /// </summary>
        public float DriveForceN { set { driveForceN = value; } get { return driveForceN; } }

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
        /// switch between Polach and Pacha adhesion calculation
        /// </summary>
        public static bool UsePolachAdhesion = false; // "static" so it's shared by all axles of the Player's loco

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
                    transmissionEfficiency = 0.99f;
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
        /// Bogie Rigid Wheel Base - distance between wheel in the bogie
        /// </summary>
        public float BogieRigidWheelBaseM;

        /// <summary>
        /// Axles in group of wheels
        /// </summary>
        public float NumAxles;

        /// <summary>
        /// Static adhesion coefficient, as given by Curtius-Kniffler formula
        /// </summary>
        public float AdhesionLimit;

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
        public float MaximumPolachWheelAdhesion;

        /// <summary>
        /// Correction parameter of adhesion, it has proportional impact on adhesion limit
        /// Should be set to 1.0 for most cases
        /// </summary>
        public float AdhesionK = 0.7f;

        /// <summary>
        /// Axle speed value, in metric meters per second
        /// </summary>
        public double AxleSpeedMpS { get; private set; }

        /// <summary>
        /// Axle angular position in radians
        /// </summary>
        public double AxlePositionRad { get; private set; }

        /// <summary>
        /// Read only axle force value, in Newtons
        /// </summary>
        public float AxleForceN { get; private set; }

        /// <summary>
        /// Compensated Axle force value, this provided the motive force equivalent excluding brake force, in Newtons
        /// </summary>
        public float CompensatedAxleForceN { get; protected set; }

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
        public bool IsWheelSlip { get; private set; }
        float WheelSlipTimeS;

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

            MaximumPolachWheelAdhesion = (float)SlipCharacteristicsPolach(WheelSlipThresholdMpS);

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
        public bool IsWheelSlipWarning { get; private set; }
        float WheelSlipWarningTimeS;

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

        /// <summary>
        /// Nonparametric constructor of Axle class instance
        /// - sets motor parameter to null
        /// - sets TtransmissionEfficiency to 0.99 (99%)
        /// - sets SlipWarningThresholdPercent to 70%
        /// - sets axle DriveType to ForceDriven
        /// - updates totalInertiaKgm2 parameter
        /// </summary>
        public Axle()
        {
            transmissionEfficiency = 0.99f;
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
                    case "ortsradius":
                        WheelRadiusM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null);
                        break;
                    case "ortsflangeangle":
                        WheelFlangeAngleRad = stf.ReadFloatBlock(STFReader.UNITS.Angle, null);
                        break;
                    case "ortsnumberwheelaxles":
                        NumAxles = stf.ReadFloatBlock(STFReader.UNITS.Distance, null);
                        break;
                    case "ortsinertia":
                        InertiaKgm2 = stf.ReadFloatBlock(STFReader.UNITS.RotationalInertia, null);
                        break;
                    case "weight":
                        AxleWeightN = 9.81f * stf.ReadFloatBlock(STFReader.UNITS.Mass, null);
                        break;
                    case "animatedparts":
                        foreach (var part in stf.ReadStringBlock("").ToUpper().Replace(" ", "").Split(','))
                        {
                            if (part != "") AnimatedParts.Add(part);
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
            WheelRadiusM = other.WheelRadiusM;
            WheelFlangeAngleRad = other.WheelFlangeAngleRad;
            NumAxles = other.NumAxles;
            InertiaKgm2 = other.InertiaKgm2;
            AxleWeightN = other.AxleWeightN;
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
        public (double, double, double, double) GetAxleMotionVariation(double axleSpeedMpS, double elapsedClockSeconds)
        {
            double slipSpeedMpS = axleSpeedMpS - TrainSpeedMpS;
            double axleOutForceN = 0;

            if (UsePolachAdhesion)
            {
                axleOutForceN = Math.Sign(slipSpeedMpS) * AxleWeightN * SlipCharacteristicsPolach(slipSpeedMpS);
            }
            else
            {
                axleOutForceN = AxleWeightN * SlipCharacteristicsPacha((float)axleSpeedMpS - TrainSpeedMpS, TrainSpeedMpS, AdhesionK, AdhesionLimit);
            }
            
            double axleInForceN = 0;
            if (DriveType == AxleDriveType.ForceDriven)
                axleInForceN = DriveForceN * transmissionEfficiency;
            else if (DriveType == AxleDriveType.MotorDriven)
                axleInForceN = motor.GetDevelopedTorqueNm(axleSpeedMpS * transmissionRatio / WheelRadiusM) * transmissionEfficiency / WheelRadiusM;

            double motionForceN = axleInForceN - dampingNs * (axleSpeedMpS - TrainSpeedMpS); // Drive force + heat losses
            double frictionForceN = BrakeRetardForceN + frictionN; // Dissipative forces: they will never increase wheel speed
            double totalAxleForceN = motionForceN - Math.Sign(axleSpeedMpS) * frictionForceN;
            if (Math.Abs(TrainSpeedMpS) < 0.001f && Math.Abs(slipSpeedMpS) < 0.001f && Math.Abs(motionForceN) < frictionForceN)
            {
                return (-slipSpeedMpS / elapsedClockSeconds, axleSpeedMpS / WheelRadiusM, 0, axleInForceN);
            }
            if (Math.Abs(totalAxleForceN) < axleStaticForceN)
            {
                if (Math.Abs(slipSpeedMpS) < 0.001f || Math.Sign(slipSpeedMpS) != Math.Sign(slipSpeedMpS + (totalAxleForceN - axleOutForceN) * forceToAccelerationFactor * elapsedClockSeconds))
                {
                    axleOutForceN = slipSpeedMpS / elapsedClockSeconds / forceToAccelerationFactor + totalAxleForceN;
                }
            }
            totalAxleForceN -= axleOutForceN;

            return (totalAxleForceN * forceToAccelerationFactor, axleSpeedMpS / WheelRadiusM, axleOutForceN, axleInForceN);
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

            if (UsePolachAdhesion)
            {

                float upperSubStepLimit = 100;
                float lowerSubStepLimit = 1;

                // use straight line graph approximation to increase substeps as slipspeed increases towards the threshold speed point
                // Points are 1 = (0, upperLimit) and 2 = (threshold, lowerLimit)           
                var AdhesGrad = ((upperSubStepLimit - lowerSubStepLimit) / (WheelSlipThresholdMpS - 0));
                var targetNumOfSubstepsPS = Math.Abs((AdhesGrad * SlipSpeedMpS) + lowerSubStepLimit);
                if (float.IsNaN((float)targetNumOfSubstepsPS)) targetNumOfSubstepsPS = 1;

                if (SlipSpeedMpS > WheelSlipThresholdMpS) // if in wheel slip then maximise the substeps
                {
                    targetNumOfSubstepsPS = upperSubStepLimit;
                }

                if (Math.Abs(integratorError) < 0.000277 && !IsWheelSlip && !IsWheelSlipWarning && SlipSpeedMpS < 0.25 * WheelSlipThresholdMpS && SlipSpeedMpS < previousSlipSpeedMpS)
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

                        if (IsWheelSlip || IsWheelSlipWarning || SlipSpeedMpS > previousSlipSpeedMpS)
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
            double axleInForceSumN = 0;
            double axleOutForceSumN = 0;
            for (int i = 0; i < NumOfSubstepsPS; i++)
            {
                var k1 = GetAxleMotionVariation(AxleSpeedMpS, dt);

                if (i == 0 && !UsePolachAdhesion)
                {
                    if (k1.Item1 * dt > Math.Max((Math.Abs(SlipSpeedMpS) - 1) * 10, 1) / 100)
                    {
                        NumOfSubstepsPS = Math.Min(NumOfSubstepsPS + 5, 50);
                        dt = elapsedClockSeconds / NumOfSubstepsPS;
                        hdt = dt / 2;
                    }

                    if (Math.Sign(AxleSpeedMpS + k1.Item1 * dt) != Math.Sign(AxleSpeedMpS) && BrakeRetardForceN + frictionN > Math.Abs(driveForceN - k1.Item3))
                    {
                        AxlePositionRad += AxleSpeedMpS * hdt;
                        AxlePositionRad = MathHelper.WrapAngle((float)AxlePositionRad);
                        AxleSpeedMpS = 0;
                        AxleForceN = 0;
                        DriveForceN = (float)k1.Item4;
                        return;
                    }
                }

                var k2 = GetAxleMotionVariation(AxleSpeedMpS + k1.Item1 * hdt, hdt);
                var k3 = GetAxleMotionVariation(AxleSpeedMpS + k2.Item1 * hdt, hdt);
                var k4 = GetAxleMotionVariation(AxleSpeedMpS + k3.Item1 * dt, dt);

                AxleSpeedMpS += (integratorError = (k1.Item1 + 2 * (k2.Item1 + k3.Item1) + k4.Item1) * dt / 6);
                AxlePositionRad += (k1.Item2 + 2 * (k2.Item2 + k3.Item2) + k4.Item2) * dt / 6;
                axleOutForceSumN += (k1.Item3 + 2 * (k2.Item3 + k3.Item3) + k4.Item3);
                axleInForceSumN += (k1.Item4 + 2 * (k2.Item4 + k3.Item4) + k4.Item4);
            }
            AxleForceN = (float)(axleOutForceSumN / (NumOfSubstepsPS * 6));
            DriveForceN = (float)(axleInForceSumN / (NumOfSubstepsPS * 6));
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
            UsePolachAdhesion = AdhesionPrecision.IsPrecisionHigh(elapsedSeconds);
            if (UsePolachAdhesion)
            {
                forceToAccelerationFactor = WheelRadiusM * WheelRadiusM / totalInertiaKgm2;

                Polach.Update();
                axleStaticForceN = AxleWeightN * SlipCharacteristicsPolach(0);
                ComputeWheelSlipThresholdMpS();

                if (count < 6 && count++ == 5)
                {
                    TrainSpeedMpS = 10 / 3.6f;
                    Polach.Update();
                    axleStaticForceN = AxleWeightN * SlipCharacteristicsPolach(0);
                }
            }
            else
            {
                // Set values for Pacha adhesion
                WheelSlipThresholdMpS = MpS.FromKpH(AdhesionK / AdhesionLimit);
                WheelAdhesion = 0.99f;
                MaximumPolachWheelAdhesion = 0.99f;

                forceToAccelerationFactor = WheelRadiusM * WheelRadiusM / totalInertiaKgm2;
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

            Integrate(elapsedSeconds);
            // TODO: We should calculate brake force here
            // Adding and substracting the brake force is correct for normal operation,
            // but during wheelslip this will produce wrong results.
            // The Axle module subtracts brake force from the motive force for calculation purposes. However brake force is already taken into account in the braking module.
            // And thus there is a duplication of the braking effect in OR. To compensate for this, after the slip characteristics have been calculated, the output of the axle
            // module has the brake force "added" back in to give the appropriate motive force output for the locomotive. Braking force is handled separately.
            // Hence CompensatedAxleForce is the actual output force on the axle. Similarly friction is also handled separately so it is also discounted from the CompensatedForce.
            if (Math.Abs(TrainSpeedMpS) < 0.001f && AxleForceN == 0) CompensatedAxleForceN = DriveForceN;
            else if (TrainSpeedMpS < 0) CompensatedAxleForceN = AxleForceN - BrakeRetardForceN - FrictionN;
            else CompensatedAxleForceN = AxleForceN + BrakeRetardForceN + FrictionN;

            if (Math.Abs(SlipSpeedMpS) > WheelSlipThresholdMpS)
            {
                // Wait some time before indicating wheelslip to avoid false triggers
                if (WheelSlipTimeS > 1)
                {
                    IsWheelSlip = IsWheelSlipWarning = true;
                }
                WheelSlipTimeS += elapsedSeconds;
            }
            else if (Math.Abs(SlipSpeedPercent) > SlipWarningTresholdPercent)
            {
                // Wait some time before indicating wheelslip to avoid false triggers
                if (WheelSlipWarningTimeS > 1) IsWheelSlipWarning = true;
                IsWheelSlip = false;
                WheelSlipWarningTimeS += elapsedSeconds;
            }
            else
            {
                IsWheelSlipWarning = false;
                IsWheelSlip = false;
                WheelSlipWarningTimeS = WheelSlipTimeS = 0;
            }

            if (elapsedSeconds > 0.0f)
            {
                slipDerivationMpSS = (SlipSpeedMpS - previousSlipSpeedMpS) / elapsedSeconds;
                previousSlipSpeedMpS = SlipSpeedMpS;

                slipDerivationPercentpS = (SlipSpeedPercent - previousSlipPercent) / elapsedSeconds;
                previousSlipPercent = SlipSpeedPercent;
            }
        }

        static class AdhesionPrecision  // "static" so all "Axle"s share the same level of precision
        {
            enum AdhesionPrecisionLevel
            {
                /// <summary>
                /// Initial level uses Polach algorithm
                /// </summary>
                High,
                /// <summary>
                /// Low-performance PCs use Pacha's algorithm
                /// </summary>
                Low
            }

            static AdhesionPrecisionLevel PrecisionLevel = AdhesionPrecisionLevel.High;
            static double TimeOfLatestDowngrade = 0;

            // Adjustable limits
            const float UpperLimitS = 0.033f;   // timespan 0.033 = 30 fps screen rate, high timeSpan and low FPS

            // Tested by varying the framerate interactively. Did this by opening and closing the HelpWindow after inserting
            //   Threading.Thread.Sleep(40);
            // into HelpWindow.PrepareFrame() temporarily.
            public static bool IsPrecisionHigh(float elapsedSeconds)
            {
                if (elapsedSeconds > 0) // Ignore period with elapsedSeconds == 0 until user starts game.
                {
                    // Switches between Polach (high precision) adhesion model and Pacha (low precision) adhesion model depending upon the PC performance
                    switch (PrecisionLevel)
                    {
                        case AdhesionPrecisionLevel.High:
                            if (elapsedSeconds > UpperLimitS)
                            {
                                var screenFrameRate = 1 / elapsedSeconds;
                                {
                                    Trace.TraceInformation($"Advanced adhesion model switched to low precision permanently after low frame rate {screenFrameRate:F1} below limit {1 / UpperLimitS:F0}");
                                    PrecisionLevel = AdhesionPrecisionLevel.Low;
                                }
                            }
                            break;

                        case AdhesionPrecisionLevel.Low:
                            break;
                    }
                }
                return (PrecisionLevel == AdhesionPrecisionLevel.High);
            }
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
                wheelLoadN = Axle.AxleWeightN / (Axle.NumAxles * 2); // Assume two wheels per axle, and thus wheel weight will be have the value - multiple axles????
                var wheelLoadkN = Axle.AxleWeightN / (Axle.NumAxles * 2 * 1000); // Assume two wheels per axle, and thus wheel weight will be have the value - multiple axles????
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
            WheelAdhesion = (float)fx;
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
