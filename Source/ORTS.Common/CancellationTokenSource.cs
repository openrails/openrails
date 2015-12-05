// COPYRIGHT 2015 by the Open Rails project.
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

namespace ORTS.Common
{
    public class CancellationTokenSource
    {
        public CancellationToken Token { get; private set; }

        readonly Action Ping;
        bool Cancelled;

        public CancellationTokenSource(Action ping)
        {
            Ping = ping;
            Token = new CancellationToken(this);
        }

        public bool IsCancellationRequested
        {
            get
            {
                DoPing();
                return Cancelled;
            }
        }

        public void Cancel()
        {
            Cancelled = true;
        }

        internal void DoPing()
        {
            var ping = Ping;
            if (ping != null)
                ping();
        }
    }
}
