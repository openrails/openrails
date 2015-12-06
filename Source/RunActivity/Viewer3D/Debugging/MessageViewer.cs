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
using System.Windows.Forms;

namespace Orts.Viewer3D.Debugging
{
    public partial class MessageViewer : Form
   {
	   private void clearAllClick(object sender, EventArgs e)
	   {
		   messages.Items.Clear();
	   }

	   private void composeClick(object sender, EventArgs e)
	   {
		   MSG.Enabled = true;
		   MultiPlayer.MPManager.Instance().ComposingText = true;
	   }
	   
	   private void replySelectedClick(object sender, EventArgs e)
	   {
		   var count = 0;
		   while (count < 3)
		   {
			   try
			   {
				   var chosen = messages.SelectedItems;
				   var msg = MSG.Text;
				   msg = msg.Replace("\r", "");
				   msg = msg.Replace("\t", "");
				   if (msg == "") return;
				   if (chosen.Count > 0)
				   {
					   var user = "";

					   for (var i = 0; i < chosen.Count; i++)
					   {
						   var tmp = (string)chosen[i];
						   var index = tmp.IndexOf(':');
						   if (index < 0) continue;
						   tmp = tmp.Substring(0, index)+"\r";
						   if (user.Contains(tmp)) continue;
						   user += tmp;
					   }
					   user += "0END";

					   MultiPlayer.MPManager.Notify((new MultiPlayer.MSGText(MultiPlayer.MPManager.GetUserName(), user, msg)).ToString());
					   MSG.Text = "";
					   //MSG.Enabled = false;
					   MultiPlayer.MPManager.Instance().ComposingText = false;

				   }
				   return;
			   }
			   catch { count++; }
		   }
	   }
	   public MessageViewer()
	   {
		   InitializeComponent();
	   }

	   public bool addNewMessage(double time, string msg)
	   {
		   var count = 0;
		   while (count < 3)
		   {
			   try
			   {
				   if (messages.Items.Count > 10)
				   {
					   messages.Items.RemoveAt(0);
				   }
				   messages.Items.Add(msg);
				   Show();
				   Visible = true;
				   BringToFront();
				   break;
			   }
			   catch { count++; }
		   }
		   return true;
	   }

   }
}
