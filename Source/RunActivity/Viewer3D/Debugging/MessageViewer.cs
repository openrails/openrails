// COPYRIGHT 2012 by the Open Rails project.
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
using System.Linq;
using System.Windows.Forms;

namespace Orts.Viewer3D.Debugging
{
    public partial class MessageViewer : Form
    {
        public MessageViewer()
        {
            InitializeComponent();
        }

        private void ClearAllClick(object sender, EventArgs e)
        {
            messages.Items.Clear();
        }
        
        private void ComposeClick(object sender, EventArgs e)
        {
            MSG.Enabled = true;
            MultiPlayer.MPManager.Instance().ComposingText = true;
        }
        
        private void ReplySelectedClick(object sender, EventArgs e)
        {
            string msg = MSG.Text
                .Replace("\r", "")
                .Replace("\t", "");
            if (msg == "")
                return;
            if (messages.SelectedItems.Count > 0)
            {
                var users = messages.SelectedItems.Cast<string>()
                    .Where((string u) => u.Contains(":"))
                    .Distinct()
                    .Select((string u) => $"{u.Substring(0, u.IndexOf(':'))}\r");
                string user = string.Join("", users) + "0END";
                string msgText = new MultiPlayer.MSGText(MultiPlayer.MPManager.GetUserName(), user, msg).ToString();
                foreach (int _ in Enumerable.Range(0, 3))
                {
                    try
                    {
                        MultiPlayer.MPManager.Notify(msgText);
                    }
                    catch
                    {
                        continue;
                    }
                    break;
                }
                MSG.Text = "";
                //MSG.Enabled = false;
                MultiPlayer.MPManager.Instance().ComposingText = false;
            }
        }
        
        public bool AddNewMessage(double _, string msg)
        {
            if (messages.Items.Count > 10)
                messages.Items.RemoveAt(0);
            messages.Items.Add(msg);
            Show();
            Visible = true;
            BringToFront();
            return true;
        }
    }
}
