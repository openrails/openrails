// COPYRIGHT 2018 by the Open Rails project.
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
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ORTS.ContentManager
{
    public class CueTextBox : TextBox
    {
        [DllImport("user32.dll", EntryPoint = "SendMessageW")]
        static extern int SendMessageForEMSCB(IntPtr handle, int em_setcuebanner, bool showWhenFocused, IntPtr cueText);
        const int EM_SETCUEBANNER = 0x1501;

        string _cue;

        public string Cue
        {
            get { return _cue; }
            set
            {
                _cue = value;
                UpdateCueText();
            }
        }

        public bool ShowCueWhenFocused { get; set; }

        void UpdateCueText()
        {
            if (!this.IsHandleCreated || string.IsNullOrEmpty(_cue)) return;
            var hglobal = Marshal.StringToHGlobalUni(_cue);
            SendMessageForEMSCB(this.Handle, EM_SETCUEBANNER, ShowCueWhenFocused, hglobal);
            Marshal.FreeHGlobal(hglobal);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateCueText();
        }
    }
}