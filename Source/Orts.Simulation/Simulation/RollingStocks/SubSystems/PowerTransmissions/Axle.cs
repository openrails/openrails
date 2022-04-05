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
        /// <summary>
        /// Integrator used for axle dynamic solving
        /// </summary>
        public Integrator AxleRevolutionsInt = new Integrator(0.0f, IntegratorMethods.RungeKutta4);
        public int NumOfSubstepsPS { get { return AxleRevolutionsInt.NumOfSubstepsPS; } }

        /// <summary>
        /// Brake force covered by BrakeForceN interface
        /// </summary>
        protected float brakeRetardForceN;
        /// <summary>
        /// Read/Write positive only brake force to the axle, in Newtons
        /// </summary>
        public float BrakeRetardForceN
        {
            set { brakeRetardForceN = value; }
            get { return brakeRetardForceN; }
        }

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
        protected AxleDriveType driveType;
        /// <summary>
        /// Read/Write Axle drive type flag
        /// </summary>
        public AxleDriveType DriveType { set { driveType = value; } get { return driveType; } }

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
                switch(driveType)
                {
                    case AxleDriveType.NotDriven:
                        break;
                    case AxleDriveType.MotorDriven:
                        //Total inertia considering gearbox
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
                switch (driveType)
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
        /// Axle diameter value, covered by AxleDiameterM interface, in metric meters
        /// </summary>
        float axleDiameterM;
        /// <summary>
        /// Read/Write nonzero positive axle diameter parameter, in metric meters
        /// Throws exception when zero or negative value is passed
        /// </summary>
        public float AxleDiameterM
        {
            set
            {
                if (value <= 0.0f)
                    throw new NotSupportedException("Axle diameter must be greater than zero");
                axleDiameterM = value;
            }
            get
            {
                return axleDiameterM;
            }
        }

        /// <summary>
        /// Read/Write adhesion conditions parameter
        /// Should be set within the range of 0.3 to 1.2 but there is no restriction
        /// - Set 1.0 for dry weather (standard)
        /// - Set 0.7 for wet, rainy weather
        /// </summary>
        public float AdhesionConditions { set; get; }

        /// <summary>
        /// Curtius-Kniffler equation A parameter
        /// </summary>
        public float CurtiusKnifflerA { set; get; }
        /// <summary>
        /// Curtius-Kniffler equation B parameter
        /// </summary>
        public float CurtiusKnifflerB { set; get; }
        /// <summary>
        /// Curtius-Kniffler equation C parameter
        /// </summary>
        public float CurtiusKnifflerC { set; get; }

        /// <summary>
        /// Read/Write correction parameter of adhesion, it has proportional impact on adhesion limit
        /// Should be set to 1.0 for most cases
        /// </summary>
        public float AdhesionK
        {
            set
            {
                adhesionK_orig = adhesionK = value;
            }
            get
            {
                return adhesionK;
            }

        }
        private float adhesionK;
        private float adhesionK_orig;

        /// <summary>
        /// Read/Write Adhesion2 parameter from the ENG/WAG file, used to correct the adhesion
        /// Should not be zero
        /// </summary>
        public float Adhesion2 { set; get; }
        /// <summary>
        /// Axle speed value, covered by AxleSpeedMpS interface, in metric meters per second
        /// </summary>
        float axleSpeedMpS;
        /// <summary>
        /// Axle speed value, in metric meters per second
        /// </summary>
        public float AxleSpeedMpS
        {
            set // used in initialisation at speed > = 0
            {
                axleSpeedMpS = value;
            }
            get
            {
                return axleSpeedMpS;
            }
        }

        /// <summary>
        /// Axle force value, covered by AxleForceN interface, in Newtons
        /// </summary>
        float axleForceN;
        /// <summary>
        /// Read only axle force value, in Newtons
        /// </summary>
        public float AxleForceN
        {
            get
            {
                return axleForceN;
            }
        }

        /// <summary>
        /// Compensated Axle force value, this provided the motive force equivalent excluding brake force, in Newtons
        /// </summary>
        public float CompensatedAxleForceN { get; protected set; }

        /// <summary>
        /// Read/Write axle weight parameter in Newtons
        /// </summary>
        public float AxleWeightN { set; get; }

        /// <summary>
        /// Read/Write train speed parameter in metric meters per second
        /// </summary>
        public float TrainSpeedMpS { set; get; }

        /// <summary>
        /// Read only wheel slip indicator
        /// - is true when absolute value of SlipSpeedMpS is greater than WheelSlipThresholdMpS, otherwise is false
        /// </summary>
        public bool IsWheelSlip
        {
            get
            {
                if (Math.Abs(SlipSpeedMpS) > WheelSlipThresholdMpS) 
                    return true;
                else
                    return false;
            }
        }

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
                if (AdhesionK == 0.0f)
                    AdhesionK = 1.0f;
                float umax = (CurtiusKnifflerA / (MpS.ToKpH(Math.Abs(TrainSpeedMpS)) + CurtiusKnifflerB) + CurtiusKnifflerC); // Curtius - Kniffler equation
                umax *= AdhesionConditions;
                return MpS.FromKpH(AdhesionK / umax);
            }
        }

        /// <summary>
        /// Read only wheelslip warning indication
        /// - is true when SlipSpeedMpS is greater than zero and 
        ///   SlipSpeedPercent is greater than SlipWarningThresholdPercent in both directions,
        ///   otherwise is false
        /// </summary>
        public bool IsWheelSlipWarning
        {
            get
            {
                return Math.Abs(SlipSpeedPercent) > SlipWarningTresholdPercent;
            }
        }

        /// <summary>
        /// Read only slip speed value in metric meters per second
        /// - computed as a substraction of axle speed and train speed
        /// </summary>
        public float SlipSpeedMpS
        {
            get
            {
                return (axleSpeedMpS - TrainSpeedMpS);
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
            driveType = AxleDriveType.ForceDriven;
            AxleRevolutionsInt.IsLimited = true;
            AxleRevolutionsInt.MaxSubsteps = 50;
            Adhesion2 = 0.331455f;

            switch (driveType)
            {
                case AxleDriveType.NotDriven:
                    break;
                case AxleDriveType.MotorDriven:
                    AxleRevolutionsInt.Max = 5000.0f;
                    AxleRevolutionsInt.Min = -5000.0f;
                    totalInertiaKgm2 = inertiaKgm2 + transmissionRatio * transmissionRatio * motor.InertiaKgm2;
                    break;
                case AxleDriveType.ForceDriven:
                    AxleRevolutionsInt.Max = 1000.0f;
                    AxleRevolutionsInt.Min = -1000.0f;
                    totalInertiaKgm2 = inertiaKgm2;
                    break;
                default:
                    totalInertiaKgm2 = inertiaKgm2;
                    break;
            }
        }

        /// <summary>
        /// Creates motor driven axle class instance
        /// - sets TransmissionEfficiency to 0.99 (99%)
        /// - sets SlipWarningThresholdPercent to 70%
        /// - sets axle DriveType to MotorDriven
        /// - updates totalInertiaKgm2 parameter
        /// </summary>
        /// <param name="electricMotor">Electric motor connected with the axle</param>
        public Axle(ElectricMotor electricMotor)
        {
            motor = electricMotor;
            motor.AxleConnected = this;
            transmissionEfficiency = 0.99f;
            driveType = AxleDriveType.MotorDriven;
            AxleRevolutionsInt.IsLimited = true;
            Adhesion2 = 0.331455f;

            switch (driveType)
            {
                case AxleDriveType.NotDriven:
                    totalInertiaKgm2 = inertiaKgm2;
                    break;
                case AxleDriveType.MotorDriven:
                    AxleRevolutionsInt.Max = 5000.0f;
                    AxleRevolutionsInt.Min = -5000.0f;
                    totalInertiaKgm2 = inertiaKgm2 + transmissionRatio * transmissionRatio * motor.InertiaKgm2;
                    break;
                case AxleDriveType.ForceDriven:
                    AxleRevolutionsInt.Max = 100.0f;
                    AxleRevolutionsInt.Min = -100.0f;
                    totalInertiaKgm2 = inertiaKgm2;
                    break;
                default:
                    totalInertiaKgm2 = inertiaKgm2;
                    break;
            }
        }

        /// <summary>
        /// A constructor that restores the game state.
        /// </summary>
        /// <param name="inf">The save stream to read from.</param>
        public Axle(BinaryReader inf) : this()
        {
            previousSlipPercent = inf.ReadSingle();
            previousSlipSpeedMpS = inf.ReadSingle();
            axleForceN = inf.ReadSingle();
            adhesionK = inf.ReadSingle();
            AdhesionConditions = inf.ReadSingle();
            frictionN = inf.ReadSingle();
            dampingNs = inf.ReadSingle();
        }

        /// <summary>
        /// Save the game state.
        /// </summary>
        /// <param name="outf">The save stream to write to.</param>
        public void Save(BinaryWriter outf)
        {
            outf.Write(previousSlipPercent);
            outf.Write(previousSlipSpeedMpS);
            outf.Write(axleForceN);
            outf.Write(adhesionK);
            outf.Write(AdhesionConditions);
            outf.Write(frictionN);
            outf.Write(dampingNs);
        }

        float avgAxleForce;
        int times;

        public float GetSpeedVariation(float axleSpeedMpS)
        {
            float axleForceN = AxleWeightN * SlipCharacteristics(axleSpeedMpS - TrainSpeedMpS, TrainSpeedMpS, AdhesionK, AdhesionConditions, Adhesion2);

            float motiveAxleForceN = -axleForceN - frictionN * (axleSpeedMpS - TrainSpeedMpS); // Force transmitted to rail + heat losses
            if (driveType == AxleDriveType.ForceDriven)
                motiveAxleForceN += driveForceN * transmissionEfficiency;
            else if (driveType == AxleDriveType.MotorDriven)
                motiveAxleForceN += motor.DevelopedTorqueNm * transmissionEfficiency * AxleDiameterM / 2;

            // Dissipative forces: they will never increase wheel speed
            float frictionalForceN = brakeRetardForceN;
                // + slipDerivationMpSS * dampingNs TODO: Integrator does not allow derivatives of integration variable, is damping required?
            
            float totalAxleForceN = motiveAxleForceN - Math.Sign(axleSpeedMpS) * frictionalForceN;
            if (Math.Abs(axleSpeedMpS) < 0.01f)
            {
                if (Math.Abs(TrainSpeedMpS) < 0.01f) frictionalForceN += frictionN; // Set a starting friction to avoid oscillations at standstill. Maybe Davis A should go here?
                if (motiveAxleForceN > frictionalForceN) totalAxleForceN = motiveAxleForceN - frictionalForceN;
                else if (motiveAxleForceN < -frictionalForceN) totalAxleForceN = motiveAxleForceN + frictionalForceN;
                else
                {
                    totalAxleForceN = 0;
                    axleForceN = 0;
                    frictionalForceN -= Math.Abs(motiveAxleForceN);
                }
            }
            avgAxleForce += axleForceN;
            times++;
            return totalAxleForceN * axleDiameterM * axleDiameterM / 4 / totalInertiaKgm2;
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
            float prevSpeedMpS = axleSpeedMpS;
            times = 0;
            avgAxleForce = 0;
            axleSpeedMpS = AxleRevolutionsInt.Integrate(timeSpan, GetSpeedVariation);
            if (times > 0) axleForceN = avgAxleForce / times;
            // TODO: around zero wheel speed calculations become unstable
            // Near-zero regime will probably need further corrections
            if ((prevSpeedMpS > 0 && axleSpeedMpS <= 0) || (prevSpeedMpS < 0 && axleSpeedMpS >= 0))
            {
                Reset();
            }
            // TODO: We should calculate brake force here
            // Adding and substracting the brake force is correct for normal operation,
            // but during wheelslip this will produce wrong results.
            // The Axle module subtracts brake force from the motive force for calculation purposes. However brake force is already taken into account in the braking module.
            // And thus there is a duplication of the braking effect in OR. To compensate for this, after the slip characteristics have been calculated, the output of the axle module
            // has the brake force "added" back in to give the appropriate motive force output for the locomotive. Braking force is handled separately.
            // Hence CompensatedAxleForce is the actual output force on the axle. 
            if (TrainSpeedMpS > 0.01f) CompensatedAxleForceN = axleForceN + brakeRetardForceN;
            else if (TrainSpeedMpS < -0.01f) CompensatedAxleForceN = axleForceN - brakeRetardForceN;
            else CompensatedAxleForceN = axleForceN;

            if (driveType == AxleDriveType.MotorDriven)
            {
                motor.RevolutionsRad = axleSpeedMpS * 2.0f * transmissionRatio / (axleDiameterM);
                motor.Update(timeSpan);
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
            axleSpeedMpS = 0;
            AxleRevolutionsInt.Reset();
            adhesionK = adhesionK_orig;
            if (motor != null)
                motor.Reset();

        }

        /// <summary>
        /// Resets all integral values to given initial condition
        /// </summary>
        /// <param name="initValue">Initial condition</param>
        public void Reset(double resetTime, float initValue)
        {
            axleSpeedMpS = initValue;
            AxleRevolutionsInt.InitialCondition = initValue;
            AxleRevolutionsInt.Reset();
            AxleRevolutionsInt.InitialCondition = 0;
            ResetTime = resetTime;
            if (motor != null)
                motor.Reset();
        }

        /// <summary>
        /// Slip characteristics computation
        /// - Computes adhesion limit using Curtius-Kniffler formula:
        ///                 7.5
        ///     umax = ---------------------  + 0.161
        ///             speed * 3.6 + 44.0
        /// - Computes slip speed
        /// - Computes relative adhesion force as a result of slip characteristics:
        ///             2*K*umax^2*dV
        ///     u = ---------------------
        ///           umax^2*dv^2 + K^2
        /// For high slip speeds (after the inflexion point of u), the formula is
        /// replaced with an exponentially decaying function (with smooth coupling)
        /// reaching a 40% of maximum adhesion at infinity. Quick fix until
        /// further investigation is done to get a single formula that provides
        /// non zero adhesion at infinity.
        /// </summary>
        /// <param name="slipSpeed">Difference between train speed and wheel speed MpS</param>
        /// <param name="speed">Current speed MpS</param>
        /// <param name="K">Slip speed correction. If is set K = 0 then K = 0.7 is used</param>
        /// <param name="conditions">Relative weather conditions, usually from 0.2 to 1.0</param>
        /// <returns>Relative force transmitted to the rail</returns>
        public float SlipCharacteristics(float slipSpeedMpS, float speedMpS, float K, float conditions, float Adhesion2)
        {
            var speedKpH = Math.Abs(MpS.ToKpH(speedMpS));
            float umax = (CurtiusKnifflerA / (speedKpH + CurtiusKnifflerB) + CurtiusKnifflerC);// *Adhesion2 / 0.331455f; // Curtius - Kniffler equation
            umax *= conditions;
            if (K == 0.0)
                K = 1;
            var slipSpeedKpH = MpS.ToKpH(slipSpeedMpS);
            float x = Math.Abs(slipSpeedKpH * umax / K);
            float sqrt3 = (float)Math.Sqrt(3);
            if (x > sqrt3)
            { 
                // At infinity, adhesion is 40% of maximum (Polach, 2005)
                // The value must be lower than 85% for the formula to work
                float inftyFactor = 0.4f;
                return Math.Sign(slipSpeedKpH) * umax * ((sqrt3 / 2 - inftyFactor) * (float)Math.Exp((sqrt3 - x) / (2 * sqrt3 - 4 * inftyFactor)) + inftyFactor);
            }
            return 2.0f * K * umax * umax * (slipSpeedKpH / (umax * umax * slipSpeedKpH * slipSpeedKpH + K * K));
        }
    }
}
