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

// This file is the responsibility of the 3D & Environment Team. 

using Microsoft.Xna.Framework;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Orts.Viewer3D.Popups
{
    public class MessagesWindow : LayeredWindow
    {
        const int HorizontalPadding = 160; // Wider than Track Monitor.
        const int VerticalPadding = 150; // Taller than Next Station.
        const int TextSize = 16;
        const double FadeTime = 2.0;

        // THREAD SAFETY:
        //   All accesses must be done in local variables. No modifications to the objects are allowed except by
        //   assignment of a new instance (possibly cloned and then modified).
        List<Message> Messages = new List<Message>();
        bool MessagesChanged;

		public List<string> GetTextMessages()
		{
			List<string> text = null;
			foreach (var m in Messages)
			{
				if (true/*m.Text.StartsWith("TEXT")*/)
				{
					if (text == null) text = new List<string>();
					text.Add(m.Text);
				}
			}
			return text;
		}
        public MessagesWindow(WindowManager owner)
            : base(owner, HorizontalPadding, VerticalPadding, "Messages")
        {
            Visible = true;
        }

        protected internal override void Save(BinaryWriter outf)
        {
            var messages = Messages;
            base.Save(outf);
            outf.Write(messages.Count);
            foreach (var message in messages)
                message.Save(outf);
        }

        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            var messages = new List<Message>(inf.ReadInt32());
            for( var i = 0; i < messages.Capacity; i++ )
            {
                messages.Add(new Message(inf));
                // Reset the EndTime so the message lasts as long on restore as it did orginally.
                // Without this reset, the band may last a very long time.
                var last = messages.Count - 1;
                var message = messages[last];
                message.EndTime = Owner.Viewer.Simulator.GameTime + (message.EndTime - message.StartTime); 
                MessagesChanged = true;
            }
            Messages = messages;
            //MessagesChanged = true;
        }

        protected override void LocationChanged()
        {
            // SizeTo does not clamp the size so we should do it first; MoveTo clamps position.
            SizeTo(Owner.ScreenSize.X - 2 * HorizontalPadding, Owner.ScreenSize.Y - 2 * VerticalPadding);
            MoveTo(HorizontalPadding, VerticalPadding);

            base.LocationChanged();
        }

        public override bool Interactive
        {
            get
            {
                return false;
            }
        }

        public override bool TopMost
        {
            get
            {
                return true;
            }
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            var vbox = base.Layout(layout).AddLayoutVertical();

            var maxLines = vbox.RemainingHeight / TextSize;
            var messages = Messages.Take(maxLines).Reverse().ToList();
            vbox.AddSpace(0, vbox.RemainingHeight - TextSize * messages.Count);
            foreach( var message in messages )
            {
                var hbox = vbox.AddLayoutHorizontal(TextSize);
                var width = Owner.Viewer.WindowManager.TextFontDefault.MeasureString(message.Text);
                hbox.Add(message.LabelShadow = new LabelShadow(width, hbox.RemainingHeight));
                hbox.Add(message.LabelText = new Label(-width, 0, width, TextSize, message.Text));
            }
            return vbox;
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            if (MessagesChanged)
            {
                MessagesChanged = false;
                Layout();
            }

            foreach (var message in Messages)
                if (message.LabelShadow != null && message.LabelText != null) // It seems LabelShadow and LabelText aren't guaranteed to be initialized, causing rare crashes
                    message.LabelShadow.Color.A = message.LabelText.Color.A = (byte)MathHelper.Lerp(255, 0, MathHelper.Clamp((float)((Owner.Viewer.RealTime - message.EndTime) / FadeTime), 0, 1));
        }

        class Message
        {
            public readonly string Key;
            public readonly string Text;
            public readonly double StartTime;
            public double EndTime;  // Not readonly so it can be reset by Restore().
            internal LabelShadow LabelShadow;
            internal Label LabelText;

            public Message(string key, string text, double startTime, double endTime)
            {
                Key = key;
                Text = text;
                StartTime = startTime;
                EndTime = endTime;
            }

            public Message(BinaryReader inf)
            {
                Key = inf.ReadString();
                Text = inf.ReadString();
                StartTime = inf.ReadDouble();
                EndTime = inf.ReadDouble();
            }

            public void Save(BinaryWriter outf)
            {
                outf.Write(Key);
                outf.Write(Text);
                outf.Write(StartTime);
                outf.Write(EndTime);
            }
        }

        public void AddMessage(string text, double duration)
        {
            AddMessage("", text, duration);
        }

        public void AddMessage(string key, string text, double duration)
        {
            var clockTime = Owner.Viewer.Simulator.ClockTime;
            var realTime = Owner.Viewer.RealTime;
            while (true)
            {
                // Store the original list and make a clone for replacing it thread-safely.
                var oldMessages = Messages;
                var newMessages = new List<Message>(oldMessages);

                // Find an existing message if there is one.
                var existingMessage = String.IsNullOrEmpty(key) ? null : newMessages.FirstOrDefault(m => m.Key == key);

                // Clean out any existing duplicate key and expired messages.
                newMessages = (from m in newMessages
                               where (String.IsNullOrEmpty(key) || m.Key != key) && m.EndTime + FadeTime > realTime
                               select m).ToList();

                // Add the new message.
                newMessages.Add(new Message(key, String.Format("{0} {1}", FormatStrings.FormatTime(clockTime), text), existingMessage != null ? existingMessage.StartTime : realTime, realTime + duration));

                // Sort the messages.
                newMessages = (from m in newMessages
                               orderby m.StartTime descending
                               select m).ToList();

                // Thread-safely switch from the old list to the new list; we've only succeeded if the previous (return) value is the old list.
                if (Interlocked.CompareExchange(ref Messages, newMessages, oldMessages) == oldMessages)
                    break;
            }
            MessagesChanged = true;
        }
    }
}
