// COPYRIGHT 2010, 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;

namespace ORTS.Popups
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
            for (var i = 0; i < messages.Capacity; i++)
                messages.Add(new Message(inf));
            Messages = messages;
            MessagesChanged = true;
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
            vbox.AddSpace(0, vbox.RemainingHeight - TextSize * messages.Count());
            foreach (var message in messages)
            {
                var hbox = vbox.AddLayoutHorizontal(TextSize);
                var width = hbox.RemainingWidth;
                hbox.Add(message.LabelShadow = new LabelShadow(hbox.RemainingWidth, hbox.RemainingHeight));
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
                message.LabelShadow.Color.A = message.LabelText.Color.A = (byte)MathHelper.Lerp(255, 0, MathHelper.Clamp((float)((Owner.Viewer.Simulator.GameTime - message.EndTime) / FadeTime), 0, 1));
        }

        class Message
        {
            public readonly string Key;
            public readonly string Text;
            public readonly double StartTime;
            public readonly double EndTime;
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
            var gameTime = Owner.Viewer.Simulator.GameTime;
            while (true)
            {
                // Store the original list and make a clone for replacing it thread-safely.
                var oldMessages = Messages;
                var newMessages = new List<Message>(oldMessages);

                // Find an existing message if there is one.
                var existingMessage = String.IsNullOrEmpty(key) ? null : newMessages.FirstOrDefault(m => m.Key == key);

                // Clean out any existing duplicate key and expired messages.
                newMessages = (from m in newMessages
                               where (String.IsNullOrEmpty(key) || m.Key != key) && m.EndTime + FadeTime > Owner.Viewer.Simulator.GameTime
                               select m).ToList();

                // Add the new message.
                newMessages.Add(new Message(key, String.Format("{0} {1}", InfoDisplay.FormattedTime(clockTime), text), existingMessage != null ? existingMessage.StartTime : gameTime, gameTime + duration));

                // Sort the messages.
                newMessages = (from m in newMessages
                               orderby m.StartTime descending
                               select m).ToList();

                // Thread-safely switch from the old list to the new list; we've only suceeded if the previous (return) value is the old list.
                if (Interlocked.CompareExchange(ref Messages, newMessages, oldMessages) == oldMessages)
                    break;
            }
            MessagesChanged = true;
        }
    }
}