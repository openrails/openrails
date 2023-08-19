// COPYRIGHT 2020 by the Open Rails project.
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

namespace Orts.Simulation.RollingStocks.SubSystems
{
    public interface ISubSystem<T> where T : ISubSystem<T>
    {
        void Copy(T other);
        void Initialize();
        void InitializeMoving();
        void Save(BinaryWriter outf);
        void Restore(BinaryReader inf);
        void Update(float elapsedClockSeconds);
    }

    public interface IParsable
    {
        void Parse(string lowercasetoken, STFReader stf);
    }
}
