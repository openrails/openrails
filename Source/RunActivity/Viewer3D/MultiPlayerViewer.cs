// COPYRIGHT 2012, 2013, 2014, 2015 by the Open Rails project.
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

using Orts.MultiPlayer;
using ORTS.Common.Input;

namespace Orts.Viewer3D
{
    public static class MultiPlayerViewer
    {
        //count how many times a key has been stroked, thus know if the panto should be up or down, etc. for example, stroke 11 times means up, thus send event with id 1
        static int PantoSecondCount;
        static int PantoFirstCount;
        static int PantoFourthCount;
        static int PantoThirdCount;
        static int WiperCount;
        static int HeadLightCount;
        static int MirrorsCount;

        public static void HandleUserInput()
        {
            //In Multiplayer, I maybe the helper, but I can request to be the controller
            // Horn and bell are managed by UpdateHornAndBell in MSTSLocomotive.cs
            if (UserInput.IsPressed(UserCommand.GameRequestControl))
            {
                MPManager.RequestControl();
            }

            if (UserInput.IsPressed(UserCommand.ControlPantograph2)) MPManager.Notify((new MSGEvent(MPManager.GetUserName(), "PANTO2", (++PantoSecondCount) % 2)).ToString());

            if (UserInput.IsPressed(UserCommand.ControlPantograph1)) MPManager.Notify((new MSGEvent(MPManager.GetUserName(), "PANTO1", (++PantoFirstCount) % 2)).ToString());

            if (UserInput.IsPressed(UserCommand.ControlPantograph4)) MPManager.Notify((new MSGEvent(MPManager.GetUserName(), "PANTO4", (++PantoFourthCount) % 2)).ToString());

            if (UserInput.IsPressed(UserCommand.ControlPantograph3)) MPManager.Notify((new MSGEvent(MPManager.GetUserName(), "PANTO3", (++PantoThirdCount) % 2)).ToString());

            if (UserInput.IsPressed(UserCommand.ControlWiper)) MPManager.Notify((new MSGEvent(MPManager.GetUserName(), "WIPER", (++WiperCount) % 2)).ToString());

            if (UserInput.IsPressed(UserCommand.ControlMirror)) MPManager.Notify((new MSGEvent(MPManager.GetUserName(), "MIRRORS", (++MirrorsCount) % 2)).ToString());

            if (UserInput.IsPressed(UserCommand.ControlHeadlightIncrease))
            {
                HeadLightCount++; if (HeadLightCount >= 3) HeadLightCount = 2;
                MPManager.Notify((new MSGEvent(MPManager.GetUserName(), "HEADLIGHT", HeadLightCount)).ToString());
            }

            if (UserInput.IsPressed(UserCommand.ControlHeadlightDecrease))
            {
                HeadLightCount--; if (HeadLightCount < 0) HeadLightCount = 0;
                MPManager.Notify((new MSGEvent(MPManager.GetUserName(), "HEADLIGHT", HeadLightCount)).ToString());
            }

        }
    }
}
