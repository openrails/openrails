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
				//msg = ""; //no ':', clear the messages
				throw new Exception("Parsing error, no : found");
			}
			try
			{
				string tmp = msg.Substring(0, index);
				int len = int.Parse(tmp);
				tmp = msg.Substring(index+2, len); //not taking ": "
				msg = msg.Remove(0, index+2+len); //remove :
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
