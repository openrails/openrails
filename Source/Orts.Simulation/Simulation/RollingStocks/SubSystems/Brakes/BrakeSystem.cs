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

using Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS;
using ORTS.Common;
using System.Collections.Generic;
using System.IO;

namespace Orts.Simulation.RollingStocks.SubSystems.Brakes
{
    public enum BrakeSystemComponent
    {
        MainReservoir,
        EqualizingReservoir,
        AuxiliaryReservoir,
        EmergencyReservoir,
        MainPipe,
        BrakePipe,
        BrakeCylinder,
        SupplyReservoir
    }

    public abstract class BrakeSystem
    {
        public float BrakeLine1PressurePSI = 90;    // main trainline pressure at this car
        public float BrakeLine2PressurePSI;         // main reservoir equalization pipe pressure
        public float BrakeLine3PressurePSI;         // engine brake cylinder equalization pipe pressure
        public float BrakePipeVolumeM3 = 1.4e-2f;      // volume of a single brake line
        public bool ControllerRunningLock = false;  // Stops Running controller from becoming active until BP = EQ Res, used in EQ vacuum brakes
        public float BrakeCylFraction;
        public float AngleCockOpeningTime = 30.0f;  // Time taken to fully open a closed anglecock

        /// <summary>
        /// Front brake hoses connection status
        /// </summary>
        public bool FrontBrakeHoseConnected;

        /// <summary>
        /// Rear brake hoses connection status
        /// </summary>
        public bool RearBrakeHoseConnected;

        /// <summary>
        /// Front angle cock opened/closed status
        /// </summary>
        public bool AngleCockAOpen = true;
        public float AngleCockAOpenAmount = 1.0f; // 0 - anglecock fully closed, 1 - anglecock fully open, allows for partial opening
        public float? AngleCockAOpenTime = null;   // Time elapsed since anglecock open command was sent
        /// <summary>
        /// Rear angle cock opened/closed status
        /// </summary>
        public bool AngleCockBOpen = true;
        public float AngleCockBOpenAmount = 1.0f; // 0 - anglecock fully closed, 1 - anglecock fully open, allows for partial opening
        public float? AngleCockBOpenTime = null;   // Time elapsed since anglecock open command was sent
        /// <summary>
        /// Auxiliary brake reservoir vent valve open/closed status
        /// </summary>
        public bool BleedOffValveOpen;
        /// <summary>
        /// Indicates whether the main reservoir pipe is available
        /// </summary>
        public bool TwoPipes;

        public float MaxBrakeShoeForceN; // This is the force applied to the brake shoe, hence it will be decreased by CoF to give force applied to the wheel
        public float InitialMaxHandbrakeForceN;  // Initial force when the wagon initialised
        public float InitialMaxBrakeForceN;   // Initial force when the wagon initialised, this is the force on the wheel, ie after the brake shoe.

        public BrakeModes BrakeMode;

        protected TrainCar Car;

        public abstract void AISetPercent(float percent);

        public abstract string GetStatus(Dictionary<BrakeSystemComponent, PressureUnit> units);
        public abstract string GetFullStatus(BrakeSystem lastCarBrakeSystem, Dictionary<BrakeSystemComponent, PressureUnit> units);
        public abstract string[] GetDebugStatus(Dictionary<BrakeSystemComponent, PressureUnit> units);
        public abstract float GetCylPressurePSI();
        public abstract float GetCylVolumeM3();
        public abstract float GetTotalCylVolumeM3();
        public abstract float GetNormalizedCylTravel();
        public abstract float GetVacResPressurePSI();
        public abstract float GetVacResVolume();
        public abstract float GetVacBrakeCylNumber();
        public bool CarBPIntact;

        public abstract void Save(BinaryWriter outf);

        public abstract void Restore(BinaryReader inf);

        public abstract void PropagateBrakePressure(float elapsedClockSeconds);

        public abstract void Update(float elapsedClockSeconds);

        public abstract void InitializeFromCopy(BrakeSystem copy, bool diff);

        public virtual BrakeSystem InitializeDefault() { return this; }

        /// <summary>
        /// Convert real pressure to a system specific internal pressure.
        /// For pressured brakes it is a straight 1:1 noop conversion,
        /// but for vacuum brakes it is a conversion to an internally used equivalent pressure.
        /// </summary>
        public abstract float InternalPressure(float realPressure);
        public abstract void Initialize(bool handbrakeOn, float maxPressurePSI, float fullServPressurePSI, bool immediateRelease);
        public abstract void SetHandbrakePercent(float percent);
        public abstract bool GetHandbrakeStatus();
        public abstract void SetRetainer(RetainerSetting setting);
        public virtual void Initialize() {}
        public abstract void InitializeMoving(); // starting conditions when starting speed > 0
        public abstract void LocoInitializeMoving(); // starting conditions when starting speed > 0
        public abstract bool IsBraking(); // return true if the wagon is braking above a certain threshold
        public abstract void CorrectMaxCylPressurePSI(MSTSLocomotive loco); // corrects max cyl pressure when too high

        public static BrakeSystem CreateNewLike(BrakeSystem brakeSystem, TrainCar car)
        {
            if (brakeSystem == null)
                return null;
            else if (brakeSystem is ManualBraking)
                return new ManualBraking(car);
            else if (brakeSystem is StraightVacuumSinglePipe)
                return new StraightVacuumSinglePipe(car);
            else if (brakeSystem is VacuumSinglePipe)
                return new VacuumSinglePipe(car);
            else if (brakeSystem is SingleTransferPipe)
                return new SingleTransferPipe(car);
            else if (brakeSystem is EPBrakeSystem)
                return new EPBrakeSystem(car);
            else if (brakeSystem is SMEBrakeSystem)
                return new SMEBrakeSystem(car);
            else if (brakeSystem is AirTwinPipe)
                return new AirTwinPipe(car);
            else if (brakeSystem is AirSinglePipe)
                return new AirSinglePipe(car);
            else
                return new SingleTransferPipe(car);
        }

        public virtual (float maxPressurePSI, float fullServPressurePSI) GetDefaultPressures() => (90, 64);
    }

    public enum RetainerSetting
    {
        [GetString("Exhaust")] Exhaust,
        [GetString("High Pressure")] HighPressure,
        [GetString("Low Pressure")] LowPressure,
        [GetString("Slow Direct")] SlowDirect
    };

    public enum BrakeModes
    {
        Undefined,
        G, // Goods
        P, // Passanger
        R, // Rapid
        RR, // Rapid with non-working accelerator, <R>
        R_MG, // Rapid with Magnetic Track Brakes, R+Mg

        D, // Passanger Long train (ru)
        K, // Passanger Short train (ru)

        LE, // Light Engine
        
        AG, // Same as G
        AP, // Same as P
        AU, // Air Unfitted/unbraked

        VB, // Vacuum Goods
        VP, // Vacuum Passanger
        VU, // Vacuum Unfitted/unbraked
    }
}
