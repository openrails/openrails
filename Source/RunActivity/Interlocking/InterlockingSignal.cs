// COPYRIGHT 2010, 2011, 2012 by the Open Rails project.
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

/// Principal Author:
///     Author: Charlie Salts / Signalsoft Rail Consultancy Ltd.
/// Contributor:
///    Richard Plokhaar / Signalsoft Rail Consultancy Ltd.
/// 


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORTS.Interlocking
{
   /// <summary>
   /// Provides an abstraction atop an underlying signal
   /// object.
   /// </summary>
   public class InterlockingSignal : InterlockingTerminator
   {
     
      /// <summary>
      /// Gets the underlying SignalObject.
      /// </summary>
      public SignalObject SignalObject { get; private set; }


      /// <summary>
      /// Creates a new InterlockingSignal object.
      /// </summary>
      /// <param name="simulator"></param>
      /// <param name="signalObject"></param>
      public InterlockingSignal(Simulator simulator, SignalObject signalObject) : base(simulator)
      {
         
         this.SignalObject = signalObject;
      }





      /// <summary>
      /// Returns true when any of the signal heads show any "proceed" aspect.
      /// </summary>
      public bool IsShowingAnyProceed
      {
         get
         {
            bool returnValue = false;

            foreach (var signalHead in SignalObject.SignalHeads)
            {
               if (signalHead.state == SignalHead.SIGASP.CLEAR_1 ||
                   signalHead.state == SignalHead.SIGASP.CLEAR_2 )
               {
                  returnValue = true;

                  // we have found *one* signal head showing *some* clear aspect - we're done
                  break;
               }
                
            }

            return returnValue;
         }
      }
   }
}
