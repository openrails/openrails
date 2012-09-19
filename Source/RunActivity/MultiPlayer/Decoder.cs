// COPYRIGHT 2012 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.

/// 
/// Additional Contributions
/// Copyright (c) Jijun Tang
/// Can only be used by the Open Rails Project.
/// This file cannot be copied, modified or included in any software which is not distributed directly by the Open Rails project.
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORTS.MultiPlayer
{
	public class Decoder
	{
		string msg = "";
		/*
		static Decoder decoder= null;
		private Decoder()
		{
		}

		public static Decoder Instance()
		{
			if (decoder == null) decoder = new Decoder();
			return decoder;
		}
		*/
		public void PushMsg(string s)
		{
			msg += s; //add to existing string of msgs
		}
		public string GetMsg()
		{
//			System.Console.WriteLine(msg);
			if (msg.Length < 1) return null;
			int index = msg.IndexOf(':');
			if (index < 0)
			{
				msg = ""; //no ':', clear the messages, no way to recover anyway
				throw new Exception("Parsing error, no : found");
			}
			try
			{
				int last = index-1;
				while (last >= 0 && char.IsDigit(msg[last--])) ; //shift back to get all digits
				if (last < 0) last = 0;
				string tmp = msg.Substring(last, index);
				int len;
				if (!int.TryParse(tmp, out len)) len = 0;
				if (index + 2 + len > msg.Length) return null;//not enough characters
				tmp = msg.Substring(index+2, len); //not taking ": "
				msg = msg.Remove(last, index+2+len); //remove :
				return tmp;
			}
			catch (Exception)
			{
				//System.Console.WriteLine(msg);
				//msg = ""; //clear the messages
				return null;
			}
			
		}
	}

	public class MultiPlayerError : Exception
	{

	}
}
