// COPYRIGHT 2010 by the Open Rails project.
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

using Orts.Parsers.Msts;
using System.IO;

namespace Orts.Simulation.RollingStocks.SubSystems.Controllers
{
    /**
     * This interface is used to specify how controls will work.
     * 
     * We have a class for implementing each type of controller that locomotives use, being the commons
     * the Notched and not Notched controller.          
     * 
     */
    public interface IController
    {
        float Update(float elapsedSeconds);

        void StartIncrease();
        void StopIncrease();
        void StartDecrease();
        void StopDecrease();
        void StartIncrease(float? target);
        void StartDecrease(float? target, bool toZero = false);
        float SetPercent(float percent);

        float UpdateValue { get; set; }
        float InitialValue { get; set; }
        float CurrentValue { get; set; }
        int CurrentNotch { get; set; }
        double CommandStartTime { get; set; }
        int SetValue(float value);

        //Loads the controller from a stream
        void Parse(STFReader stf);

        //returns true if this controller was loaded and can be used
        //Some notched controllers will have stepSize == 0, those are invalid
        bool IsValid();

        string GetStatus();

        void Save(BinaryWriter outf);
    }
}
