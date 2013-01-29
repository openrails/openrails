// COPYRIGHT 2010, 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

using System;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using Microsoft.Xna.Framework.Input;
using System.Linq;
namespace ORTS.Popups
{
	public class ComposeMessage : Window
	{
		Label Message;

		int index;
		public ComposeMessage(WindowManager owner)
			: base(owner, 600, 72, "Compose Message (e.g.   receiver1, receiver2: message body)")
		{
		}

		bool EnterReceived = false;

		public bool InitMessage()
		{
			this.Visible = true; UserInput.ComposingMessage = true;
			if ( MultiPlayer.MPManager.Instance().lastSender != "") Message.Text = MultiPlayer.MPManager.Instance().lastSender + ":";
			return true;
		}
		public void AppendMessage(Keys[] newKeys, Keys[] oldKeys)
		{
#if false
			foreach (var k in keys)
			{
				if (k == Keys.Enter)
				{
					this.Visible = false;
					UserInput.ComposingMessage = false;
				}
				if (k == Keys.Back)
				{
					if (Message.Text.Length <= 1) Message.Text = "";
					else Message.Text = Message.Text.Remove(Message.Text.Length - 1);
				}
				else if (k == Keys.OemComma) Message.Text += ",";
				else if (k == Keys.OemPeriod) Message.Text += ".";
				else if (k == Keys.OemMinus) Message.Text += "-";
				else if (k == Keys.OemQuestion) Message.Text += "?";
				else if (k == Keys.OemQuotes) Message.Text += "\"";
				else if (k == Keys.OemSemicolon) Message.Text += ";";
				else if (k == Keys.OemPlus) Message.Text += "+";
				else
				{
					char c = (char)k;
					if (char.IsLetterOrDigit(c))
					{
						Message.Text += char.ToLower(c);
					}
					if (c == ' ' || char.IsPunctuation(c)) { Message.Text += c; }
				}
			}
#endif
			EnterReceived = false;

			string input = Convert(newKeys);
			foreach (char x in input)
			{
				//process backspace
				if (x == '\b')
				{
					if (Message.Text.Length >= 1)
					{
						Message.Text = Message.Text.Remove(Message.Text.Length - 1, 1);
					}
				}
				else
					Message.Text += x;
			}

			//we need to send message out
			if (EnterReceived == true)
			{
				try
				{
					var user = "";
					if (MultiPlayer.MPManager.Instance().lastSender == "")
					{
						//server will broadcast the message to everyone
						if (MultiPlayer.MPManager.IsServer())
						{
							foreach (var p in MultiPlayer.MPManager.OnlineTrains.Players)
							{
								user += p.Key + "\r";
							}
							user += "0END";

						}
						else user = "0Server\r0END";
					}
					var index = Message.Text.IndexOf(':');
					var msg = Message.Text;
					if (index > 0) 
					{
						msg = Message.Text.Remove(0, index+1);
						var str = Message.Text.Substring(0, index);
						var names = str.Split(',');
						var first = true;
						foreach (var n in names)
						{
							if (MultiPlayer.MPManager.OnlineTrains.Players.ContainsKey(n.Trim()))
							{
								if (first) { user = ""; first = false; }
								user += n.Trim() + "\r";
							}
						}
						user += "0END";
					}
					MultiPlayer.MPManager.Notify((new MultiPlayer.MSGText(MultiPlayer.MPManager.GetUserName(), user, msg)).ToString());
					this.Visible = false;
					UserInput.ComposingMessage = false; Message.Text = "";
				}
				catch { }
			}
		}
		protected override ControlLayout Layout(ControlLayout layout)
		{
			var vbox = base.Layout(layout).AddLayoutVertical();
			var boxWidth = vbox.RemainingWidth;
			{
				var hbox = vbox.AddLayoutHorizontal(128);
				hbox.Add(Message = new Label(boxWidth, hbox.RemainingHeight, ""));
			}
			return vbox;
		}

		public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
		{
			base.PrepareFrame(elapsedTime, updateFull);

			if (updateFull)
			{
			}
		}

		public string Convert(Keys[] keys)
		{
			string output = "";
			bool usesShift = (keys.Contains(Keys.LeftShift) || keys.Contains(Keys.RightShift));

			foreach (Keys key in keys)
			{
				if (key == Keys.Enter)
				{
					EnterReceived = true; break;
				}
				if (key >= Keys.A && key <= Keys.Z)
					output += key.ToString();
				else if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
					output += ((int)(key - Keys.NumPad0)).ToString();
				else if (key >= Keys.D0 && key <= Keys.D9)
				{
					string num = ((int)(key - Keys.D0)).ToString();
					#region special num chars
					if (usesShift)
					{
						switch (num)
						{
							case "1":
								{
									num = "!";
								}
								break;
							case "2":
								{
									num = "@";
								}
								break;
							case "3":
								{
									num = "#";
								}
								break;
							case "4":
								{
									num = "$";
								}
								break;
							case "5":
								{
									num = "%";
								}
								break;
							case "6":
								{
									num = "^";
								}
								break;
							case "7":
								{
									num = "&";
								}
								break;
							case "8":
								{
									num = "*";
								}
								break;
							case "9":
								{
									num = "(";
								}
								break;
							case "0":
								{
									num = ")";
								}
								break;
							default:
								//wtf?
								break;
						}
					}
					#endregion
					output += num;
				}
				else if (key == Keys.OemPeriod && !usesShift)
					output += ".";
				else if (key == Keys.OemPeriod && usesShift)
					output += ">";
				else if (key == Keys.OemComma && !usesShift)
					output += ",";
				else if (key == Keys.OemComma && usesShift)
					output += "<";
				else if (key == Keys.OemTilde)
					output += "'";
				else if (key == Keys.Space)
					output += " ";
				else if (key == Keys.OemMinus && !usesShift)
					output += "-";
				else if (key == Keys.OemMinus && usesShift)
					output += "_";
				else if (key == Keys.OemPlus && !usesShift)
					output += "=";
				else if (key == Keys.OemPlus && usesShift)
					output += "+";
				else if (key == Keys.OemQuestion && usesShift)
					output += "?";
				else if (key == Keys.OemQuestion && !usesShift)
					output += "/";
				else if (key == Keys.OemSemicolon && !usesShift)
					output += ";";
				else if (key == Keys.OemSemicolon && usesShift)
					output += ":";
				else if (key == Keys.OemQuotes && !usesShift)
					output += "\'";
				else if (key == Keys.OemQuotes && usesShift)
					output += "\"";
				else if (key == Keys.OemOpenBrackets && !usesShift)
					output += "[";
				else if (key == Keys.OemOpenBrackets && usesShift)
					output += "{";
				else if (key == Keys.OemCloseBrackets && !usesShift)
					output += "]";
				else if (key == Keys.OemCloseBrackets && usesShift)
					output += "}";
				else if (key == Keys.Back) //backspace
					output += "\b";

				if (!usesShift) //shouldn't need to upper because it's automagically in upper case
					output = output.ToLower();
			}
			return output;
		}
	}

}
