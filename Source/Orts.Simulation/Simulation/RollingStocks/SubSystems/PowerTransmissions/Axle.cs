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

using System;
using System.IO;
using Microsoft.Xna.Framework;
using ORTS.Common;

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
    public class Axle
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
        /// Static adhesion coefficient, as given by Curtius-Kniffler formula
        /// </summary>
        public float AdhesionLimit;

        /// <summary>
        /// Correction parameter of adhesion, it has proportional impact on adhesion limit
        /// Should be set to 1.0 for most cases
        /// </summary>
        public float AdhesionK = 0.7f;

        /// <summary>
        /// Axle speed value, in metric meters per second
        /// </summary>
        public float AxleSpeedMpS { get; private set; }
        /// <summary>
        /// Axle angular position in radians
        /// </summary>
        public float AxlePositionRad { get; private set; }
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
        /// Read only wheelslip threshold value used to indicate maximal effective slip
        /// - its value is computed as a maximum of slip function:
        ///                 2*K*umax^2 * dV
        ///   f(dV) = u = ---------------------
        ///                umax^2*dV^2 + K^2
        ///   maximum can be found as a derivation f'(dV) = 0
        /// </summary>
        public float WheelSlipThresholdMpS
        {
            get
            {
                return MpS.FromKpH(AdhesionK / AdhesionLimit);
            }
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
                return (AxleSpeedMpS - TrainSpeedMpS);
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
                var temp = SlipSpeedMpS / WheelSlipThresholdMpS * 100.0f;
                if (float.IsNaN(temp)) temp = 0;//avoid NaN on HuD display when first starting OR
                return temp;
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

        float integratorError;
        int waitBeforeSpeedingUp;

        /// <summary>
        /// Read/Write relative slip speed warning threshold value, in percent of maximal effective slip
        /// </summary>
        public float SlipWarningTresholdPercent { set; get; }

        public double ResetTime = 0;

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
        }

        /// <summary>
        /// A constructor that restores the game state.
        /// </summary>
        /// <param name="inf">The save stream to read from.</param>
        public Axle(BinaryReader inf) : this()
        {
            previousSlipPercent = inf.ReadSingle();
            previousSlipSpeedMpS = inf.ReadSingle();
            AxleForceN = inf.ReadSingle();
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
        }

        /// <summary>
        /// Compute variation in axle dynamics. Calculates axle speed, axle angular position and in/out forces.
        /// </summary>
        public (float, float, float, float) GetAxleMotionVariation(float axleSpeedMpS)
        {
            float axleOutForceN = AxleWeightN * SlipCharacteristics(axleSpeedMpS - TrainSpeedMpS, TrainSpeedMpS, AdhesionK, AdhesionLimit);

            float axleInForceN = 0;
            if (DriveType == AxleDriveType.ForceDriven)
                axleInForceN = DriveForceN * transmissionEfficiency;
            else if (DriveType == AxleDriveType.MotorDriven)
                axleInForceN = motor.GetDevelopedTorqueNm(axleSpeedMpS * transmissionRatio / WheelRadiusM) * transmissionEfficiency / WheelRadiusM;

            float totalAxleForceN = axleInForceN - axleOutForceN - dampingNs * (axleSpeedMpS - TrainSpeedMpS); // Force transmitted to rail + heat losses

            totalAxleForceN -= Math.Sign(axleSpeedMpS) * (BrakeRetardForceN + frictionN); // Dissipative forces: they will never increase wheel speed

            return (totalAxleForceN * forceToAccelerationFactor, axleSpeedMpS / WheelRadiusM, axleOutForceN, axleInForceN);
        }

        /// <summary>
        /// Integrates the wheel rotation movement using a RK4 method,
        /// calculating the required number of substeps
        /// Outputs: wheel speed, wheel angular position and motive force
        /// </summary>
        void Integrate(float elapsedClockSeconds)
        {
            if (elapsedClockSeconds <= 0) return;
            float prevSpeedMpS = AxleSpeedMpS;

            if (Math.Abs(integratorError) > Math.Max((Math.Abs(SlipSpeedMpS)-1)*0.01f, 0.001f))
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

            float dt = elapsedClockSeconds / NumOfSubstepsPS;
            float hdt = dt / 2.0f;
            float axleInForceSumN = 0;
            float axleOutForceSumN = 0;
            for (int i=0; i<NumOfSubstepsPS; i++)
            {
                var k1 = GetAxleMotionVariation(AxleSpeedMpS);
                if (i == 0)
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
                        AxlePositionRad = MathHelper.WrapAngle(AxlePositionRad);
                        AxleSpeedMpS = 0;
                        AxleForceN = 0;
                        DriveForceN = k1.Item4;
                        return;
                    }
                }
                var k2 = GetAxleMotionVariation(AxleSpeedMpS + k1.Item1 * hdt);
                var k3 = GetAxleMotionVariation(AxleSpeedMpS + k2.Item1 * hdt);
                var k4 = GetAxleMotionVariation(AxleSpeedMpS + k3.Item1 * dt);
                AxleSpeedMpS += (integratorError = (k1.Item1 + 2 * (k2.Item1 + k3.Item1) + k4.Item1) * dt / 6.0f);
                AxlePositionRad += (k1.Item2 + 2 * (k2.Item2 + k3.Item2) + k4.Item2) * dt / 6.0f;
                axleOutForceSumN += (k1.Item3 + 2 * (k2.Item3 + k3.Item3) + k4.Item3);
                axleInForceSumN += (k1.Item4 + 2 * (k2.Item4 + k3.Item4) + k4.Item4);
            }
            AxleForceN = axleOutForceSumN / (NumOfSubstepsPS * 6);
            DriveForceN = axleInForceSumN / (NumOfSubstepsPS * 6);
            AxlePositionRad = MathHelper.WrapAngle(AxlePositionRad);
        }

        /// <summary>
        /// Work in progress. Calculates wheel creep assuming that wheel inertia
        /// is low, removing the need of an integrator
        /// Useful for slow CPUs
        /// </summary>
        void StationaryCalculation(float elapsedClockSeconds)
        {
            var res = GetAxleMotionVariation(AxleSpeedMpS);
            float force = res.Item1 / forceToAccelerationFactor + res.Item3;
            float maxAdhesiveForce = AxleWeightN * AdhesionLimit;
            if (maxAdhesiveForce == 0) return;
            float forceRatio = force / maxAdhesiveForce;
            float absForceRatio = Math.Abs(forceRatio);
            float characteristicTime = WheelSlipThresholdMpS / (maxAdhesiveForce * forceToAccelerationFactor);
            if (absForceRatio > 1 || IsWheelSlip || Math.Abs(res.Item1 * elapsedClockSeconds) < WheelSlipThresholdMpS)
            {
                Integrate(elapsedClockSeconds);
                return;
            }
            NumOfSubstepsPS = 1;
            if (absForceRatio < 0.001f)
            {
                AxleSpeedMpS = TrainSpeedMpS;
                AxleForceN = 0;
                return;
            }
            float x = (float)((1 - Math.Sqrt(1 - forceRatio * forceRatio)) / forceRatio);
            AxleSpeedMpS = TrainSpeedMpS + MpS.FromKpH(AdhesionK * x / AdhesionLimit);
            AxleForceN = (force + res.Item3)/2;
        }

        /// <summary>
        /// Main Update method
        /// - computes slip characteristics to get new axle force
        /// - computes axle dynamic model according to its driveType
        /// - computes wheelslip indicators
        /// </summary>
        /// <param name="timeSpan"></param>
        public virtual void Update(float timeSpan)
        {
            forceToAccelerationFactor = WheelRadiusM * WheelRadiusM / totalInertiaKgm2;

            motor?.Update(timeSpan);

            Integrate(timeSpan);
            // TODO: We should calculate brake force here
            // Adding and substracting the brake force is correct for normal operation,
            // but during wheelslip this will produce wrong results.
            // The Axle module subtracts brake force from the motive force for calculation purposes. However brake force is already taken into account in the braking module.
            // And thus there is a duplication of the braking effect in OR. To compensate for this, after the slip characteristics have been calculated, the output of the axle module
            // has the brake force "added" back in to give the appropriate motive force output for the locomotive. Braking force is handled separately.
            // Hence CompensatedAxleForce is the actual output force on the axle. 
            CompensatedAxleForceN = AxleForceN + Math.Sign(TrainSpeedMpS) * BrakeRetardForceN;
            if (AxleForceN == 0) CompensatedAxleForceN = 0;

            if (Math.Abs(SlipSpeedMpS) > WheelSlipThresholdMpS)
            {
                // Wait some time before indicating wheelslip to avoid false triggers
                if (WheelSlipTimeS > 0.1f)
                {
                    IsWheelSlip = IsWheelSlipWarning = true;
                }
                WheelSlipTimeS += timeSpan;
            }
            else if (Math.Abs(SlipSpeedPercent) > SlipWarningTresholdPercent)
            {
                // Wait some time before indicating wheelslip to avoid false triggers
                if (WheelSlipWarningTimeS > 0.1f) IsWheelSlipWarning = true;
                IsWheelSlip = false;
                WheelSlipWarningTimeS += timeSpan;
            }
            else
            {
                IsWheelSlipWarning = false;
                IsWheelSlip = false;
                WheelSlipWarningTimeS = WheelSlipTimeS = 0;
            }

            if (timeSpan > 0.0f)
            {
                slipDerivationMpSS = (SlipSpeedMpS - previousSlipSpeedMpS) / timeSpan;
                previousSlipSpeedMpS = SlipSpeedMpS;

                slipDerivationPercentpS = (SlipSpeedPercent - previousSlipPercent) / timeSpan;
                previousSlipPercent = SlipSpeedPercent;
            }
        }

        /// <summary>
        /// Resets all integral values (set to zero)
        /// </summary>
        public void Reset()
        {
            AxleSpeedMpS = 0;
            motor?.Reset();

        }

        /// <summary>
        /// Resets all integral values to given initial condition
        /// </summary>
        /// <param name="initValue">Initial condition</param>
        public void Reset(double resetTime, float initValue)
        {
            AxleSpeedMpS = initValue;
            ResetTime = resetTime;
            motor?.Reset();
        }

        /// <summary>
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
        public float SlipCharacteristics(float slipSpeedMpS, float speedMpS, float K, float umax)
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
