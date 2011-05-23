// COPYRIGHT 2010, 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ORTS.Popups
{
	public class MessagesWindow : LayeredWindow
	{
		const int HorizontalPadding = 160; // Wider than Track Monitor.
		const int VerticalPadding = 150; // Taller than Next Station.
		const int TextSize = 16;
		const double FadeTime = 2.0;

		IList<Message> Messages = new List<Message>();

		public MessagesWindow(WindowManager owner)
			: base(owner, HorizontalPadding, VerticalPadding, "Messages")
		{
			Visible = true;
		}

        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            outf.Write(Messages.Count);
            foreach (var message in Messages)
                message.Save(outf);
        }

        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            var count = inf.ReadInt32();
            for (var i = 0; i < count; i++)
                Messages.Add(new Message(inf));
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
			var messages = Messages.Reverse().Take(maxLines).Reverse().ToList();
			vbox.AddSpace(0, vbox.RemainingHeight - TextSize * messages.Count);
			foreach (var message in messages)
			{
				var hbox = vbox.AddLayoutHorizontal(TextSize);
				var width = hbox.RemainingWidth;
				hbox.Add(message.LabelShadow = new LabelShadow(hbox.RemainingWidth, hbox.RemainingHeight));
				hbox.Add(message.LabelTime = new Label(-width, 0, TextSize * 4, TextSize, InfoDisplay.FormattedTime(message.ClockTime)));
                hbox.Add(message.LabelText = new Label(-width + TextSize * 4, 0, width - TextSize * 4, TextSize, message.Text));
            }
			return vbox;
		}

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            if (updateFull)
            {
                if (Messages.Any(m => Owner.Viewer.Simulator.ClockTime >= m.ExpiryTime + FadeTime))
                {
                    Messages = Messages.Where(m => Owner.Viewer.Simulator.ClockTime < m.ExpiryTime + FadeTime).ToList();
                    Layout();
                }
            }

            foreach (var message in Messages.Where(m => Owner.Viewer.Simulator.ClockTime >= m.ExpiryTime))
                message.LabelShadow.Color.A = message.LabelTime.Color.A = message.LabelText.Color.A = (byte)MathHelper.Lerp(255, 0, MathHelper.Clamp((float)((Owner.Viewer.Simulator.ClockTime - message.ExpiryTime) / FadeTime), 0, 1));
        }

		class Message
		{
			public readonly string Text;
			public readonly double ClockTime;
			public readonly double ExpiryTime;
			internal LabelShadow LabelShadow;
			internal Label LabelTime;
			internal Label LabelText;

			public Message(string text, double clockTime, double duration)
			{
				Text = text;
				ClockTime = clockTime;
				ExpiryTime = clockTime + duration;
			}

            public Message(BinaryReader inf)
            {
                Text = inf.ReadString();
                ClockTime = inf.ReadDouble();
                ExpiryTime = inf.ReadDouble();
            }

            public void Save(BinaryWriter outf)
            {
                outf.Write(Text);
                outf.Write(ClockTime);
                outf.Write(ExpiryTime);
            }
        }

		public void AddMessage(string text, double duration)
		{
			Messages.Add(new Message(text, Owner.Viewer.Simulator.ClockTime, duration));
			Layout();
		}
	}
}
