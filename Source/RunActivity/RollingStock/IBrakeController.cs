using MSTS;
using System;

namespace ORTS
{
    /**
     * This is the interface for a controller that can be used as a BrakeController.
     * 
     */
    public interface IBrakeController: IController
    {
        void ParseBrakeValue(string lowercasetoken, STFReader stf);

        void UpdatePressure(ref float pressurePSI, float elapsedClockSeconds, ref float epPressurePSI);
        void UpdateEngineBrakePressure(ref float pressurePSI, float elapsedClockSeconds);

        void SetEmergency();
        bool GetIsEmergency();

        float GetFullServReductionPSI();
        float GetMaxPressurePSI();
    }
}
