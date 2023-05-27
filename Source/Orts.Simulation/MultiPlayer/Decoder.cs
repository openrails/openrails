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

namespace Orts.MultiPlayer
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
            //if (msg.Length > 10 && !msg.Contains(":") && s.Contains(":")) msg = "";
            msg += s; //add to existing string of msgs
        }
        public string GetMsg()
        {
            //			System.Console.WriteLine(msg);
            if (msg.Length < 1) return null;
            int index = msg.IndexOf(':');
            if (index < 0)
            {
                if (msg.Length > 10) msg = msg.Remove(0); //no ':', clear the messages, no way to recover anyway, except the first few digits
                throw new Exception("Parsing error, no : found");
            }
            try
            {
                int last = index - 1;
                while (last >= 0)
                {
                    if (!char.IsDigit(msg[last])) break;
                    last--;
                } //shift back to get all digits
                last += 1;
                if (last < 0) last = 0;
                string tmp = msg.Substring(last, index - last);
                int len;
                if (!int.TryParse(tmp, out len)) { msg = msg.Remove(0); return null; }
                if (len < 0) return null;
                if (index + 2 + len > msg.Length)
                {
                    //if (msg.LastIndexOf(":") > 64) { msg = msg.Remove(0, index + 1); }//if there is a : further down, means the length is wrong, needs to remove until next :
                    return null;
                }
                tmp = msg.Substring(index + 2, len); //not taking ": "
                msg = msg.Remove(0, index + 2 + len); //remove :
                if (len > 1000000) return null;//a long message, will ignore it
#if false
				int last = index-1;
				while (last >= 0 && char.IsDigit(msg[last--])) ; //shift back to get all digits
				if (last < 0) last = 0;
				string tmp = msg.Substring(last, index);
				int len;
				if (!int.TryParse(tmp, out len)) len = 0;
				if (index + 2 + len > msg.Length) return null;//not enough characters
				tmp = msg.Substring(index+2, len); //not taking ": "
				msg = msg.Remove(last, index+2+len); //remove :
#endif
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
    public class SameNameError : Exception
    {

    }
}
