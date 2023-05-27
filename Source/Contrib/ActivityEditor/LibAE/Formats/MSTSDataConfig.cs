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

/// This module ...
/// 
/// Author: Stéfan Paitoni
/// Updates : 
/// 

using System.IO;
using Orts.Formats.OR;
using ORTS.Common;

namespace LibAE
{
    public class MSTSDataConfig : MSTSData
    {
        public MSTSBase TileBase { get; protected set; }

        public MSTSDataConfig(string mstsPath, string Route, TypeEditor interfaceType) : base(mstsPath, Route)
        {
            string routePath = Path.Combine(Route, TRK.Tr_RouteFile.FileName);
            TileBase = new MSTSBase(TDB);
            TileBase.reduce(TDB);
        }
    }
}
