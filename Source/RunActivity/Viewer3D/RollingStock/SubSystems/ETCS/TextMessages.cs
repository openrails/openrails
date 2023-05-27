// COPYRIGHT 2014 by the Open Rails project.
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

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Viewer3D.Popups;
using Orts.Viewer3D.RollingStock.Subsystems.ETCS;
using ORTS.Scripting.Api.ETCS;

namespace Orts.Viewer3D.RollingStock.SubSystems.ETCS
{
    public class MessageArea : DMIButton
    {
        const float FontHeightMessage = 12;
        const float FontHeightTimestamp = 10;

        WindowTextFont FontTimestamp;
        WindowTextFont FontMessage;
        WindowTextFont FontMessageBold;

        readonly Texture2D[] ScrollUpTexture = new Texture2D[2];
        readonly Texture2D[] ScrollDownTexture = new Texture2D[2];
        int CurrentPage = 0;
        int NumPages = 1;

        public readonly DMIButton ButtonScrollUp;
        public readonly DMIButton ButtonScrollDown;

        readonly int MaxTextLines;

        const int RowHeight = 20;

        readonly TextPrimitive[] DisplayedTexts;
        readonly TextPrimitive[] DisplayedTimes;

        List<TextMessage> MessageList;
        TextMessage? AcknowledgingMessage;
        public MessageArea(DriverMachineInterface dmi) : base(Viewer.Catalog.GetString("Acknowledge"), true, null, 234, (dmi.IsSoftLayout ? 4 : 5) * RowHeight, dmi, false)
        {
            MaxTextLines = dmi.IsSoftLayout ? 4 : 5;

            DisplayedTexts = new TextPrimitive[MaxTextLines];
            DisplayedTimes = new TextPrimitive[MaxTextLines];

            ButtonScrollUp = new DMIIconButton("NA_13.bmp", "NA_15.bmp", Viewer.Catalog.GetString("Scroll Up"), true, () =>
            {
                if (CurrentPage < NumPages - 1)
                {
                    CurrentPage++;
                    SetMessages();
                }
            }, 46, Height / 2, dmi);
            ButtonScrollDown = new DMIIconButton("NA_14.bmp", "NA_16.bmp", Viewer.Catalog.GetString("Scroll Down"), true, () =>
            {
                if (CurrentPage > 0)
                {
                    CurrentPage--;
                    SetMessages();
                }
            }, 46, Height / 2, dmi);
            PressedAction = () =>
            {
                if (AcknowledgingMessage != null)
                {
                    var ackmsg = AcknowledgingMessage.Value;
                    ackmsg.Acknowledgeable = false;
                    ackmsg.Acknowledged = true;
                    int index = MessageList.IndexOf(ackmsg);
                    if (index != -1) MessageList[index] = ackmsg;
                    AcknowledgingMessage = null;
                }
            };
            ScaleChanged();
        }
        public override void Draw(SpriteBatch spriteBatch, Point position)
        {
            if (!Visible) return;
            base.Draw(spriteBatch, position);
            foreach (var text in DisplayedTexts)
            {
                if (text == null) continue;
                int x = position.X + (int)(text.Position.X * Scale);
                int y = position.Y + (int)(text.Position.Y * Scale);
                text.Draw(spriteBatch, new Point(x, y));
            }
            foreach (var text in DisplayedTimes)
            {
                if (text == null) continue;
                int x = position.X + (int)(text.Position.X * Scale);
                int y = position.Y + (int)(text.Position.Y * Scale);
                text.Draw(spriteBatch, new Point(x, y));
            }
            FlashingFrame = AcknowledgingMessage.HasValue;
        }

        int CompareMessages(TextMessage m1, TextMessage m2)
        {
            int ack = m2.Acknowledgeable.CompareTo(m1.Acknowledgeable);
            if (ack != 0) return ack;
            int date = m2.TimestampS.CompareTo(m1.TimestampS);
            if (m1.Acknowledgeable) return date;
            int prior = m2.FirstGroup.CompareTo(m1.FirstGroup);
            if (prior != 0) return prior;
            return date;
        }
        void SetDatePrimitive(float timestampS, int row)
        {
            int totalseconds = (int)timestampS;
            int hour = (totalseconds / 3600) % 24;
            int minute = (totalseconds / 60) % 60;
            var text = $"{(hour < 10 ? "0" : "")}{hour}:{(minute < 10 ? "0" : "")}{minute}";
            DisplayedTimes[row] = new TextPrimitive(new Point(3, (row + 1) * RowHeight - (int)FontHeightTimestamp), Color.White, text, FontTimestamp);
        }
        void SetTextPrimitive(string text, int row, bool isBold)
        {
            var font = isBold ? FontMessageBold : FontMessage;
            DisplayedTexts[row] = new TextPrimitive(new Point(48, (row + 1) * RowHeight - (int)FontHeightMessage), Color.White, text, font);
        }
        string[] GetRowSeparated(string text, bool isBold)
        {
            var font = isBold ? FontMessageBold : FontMessage;
            var size = font.MeasureString(text) / Scale;
            if (size > 234 - 48)
            {
                int split = text.LastIndexOf(' ', (int)((234 - 48) / size * text.Length));
                if (split == -1) split = (int)((234 - 48) / size * text.Length);
                var remaining = GetRowSeparated(text.Substring(split + 1), isBold);
                var arr = new string[remaining.Length + 1];
                arr[0] = text.Substring(0, split);
                remaining.CopyTo(arr, 1);
                return arr;
            }
            else
            {
                return new string[] { text };
            }
        }
        void SetMessages()
        {
            foreach (int i in Enumerable.Range(0, MaxTextLines))
            {
                DisplayedTexts[i] = null;
                DisplayedTimes[i] = null;
            }
            if (MessageList == null || MessageList.Count == 0) return;
            if (MessageList[0].Acknowledgeable)
            {
                var msg = MessageList[0];
                msg.Displayed = true;
                CurrentPage = 0;
                SetDatePrimitive(msg.TimestampS, 0);
                string[] text = GetRowSeparated(msg.Text, false);
                foreach (int j in Enumerable.Range(0, Math.Min(text.Length, MaxTextLines)))
                {
                    SetTextPrimitive(text[j], j, false);
                }
                AcknowledgingMessage = msg;
                MessageList[0] = msg;
                NumPages = 1;
            }
            else
            {
                if (!MessageList[0].Displayed) CurrentPage = 0;
                int row = 0;
                foreach (int i in Enumerable.Range(0, MessageList.Count))
                {
                    var msg = MessageList[i];
                    string[] text = GetRowSeparated(msg.Text, msg.FirstGroup);
                    if (CurrentPage * MaxTextLines <= row && row < (CurrentPage + 1) * MaxTextLines)
                    {
                        msg.Displayed = true;
                        SetDatePrimitive(msg.TimestampS, row % MaxTextLines);
                    }
                    foreach (string c in text)
                    {
                        if (CurrentPage * MaxTextLines <= row && row < (CurrentPage + 1) * MaxTextLines)
                        {
                            SetTextPrimitive(c, row % MaxTextLines, msg.FirstGroup);
                        }
                        row++;
                    }
                    MessageList[i] = msg;
                }
                NumPages = row / MaxTextLines + 1;
            }
        }
        public override void PrepareFrame(ETCSStatus status)
        {
            if (Visible != status.TextMessageAreaShown)
            {
                Visible = status.TextMessageAreaShown;
                ButtonScrollUp.Visible = Visible;
                ButtonScrollDown.Visible = Visible;
            }
            MessageList = status.TextMessages;
            if (!Visible) return;
            if (AcknowledgingMessage.HasValue)
            {
                if (MessageList.Contains(AcknowledgingMessage.Value)) return;
                AcknowledgingMessage = null;
            }
            MessageList.Sort(CompareMessages);
            SetMessages();

            ButtonScrollDown.Enabled = CurrentPage < NumPages - 1;
            ButtonScrollUp.Enabled = CurrentPage > 0;
            Enabled = AcknowledgingMessage != null;
        }
        public override void ScaleChanged()
        {
            ScrollUpTexture[0] = DMI.LoadTexture("NA_15.bmp");
            ScrollUpTexture[1] = DMI.LoadTexture("NA_13.bmp");
            ScrollDownTexture[0] = DMI.LoadTexture("NA_16.bmp");
            ScrollDownTexture[1] = DMI.LoadTexture("NA_14.bmp");

            SetFont();
        }
        void SetFont()
        {
            FontTimestamp = GetFont(FontHeightTimestamp);
            FontMessage = GetFont(FontHeightMessage);
            FontMessageBold = GetFont(FontHeightMessage, true);
            SetMessages();
        }
    }
}
