using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ORTS.Debugging
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
