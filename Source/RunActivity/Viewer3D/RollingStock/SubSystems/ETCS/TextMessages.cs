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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Viewer3D.Popups;
using Orts.Viewer3D.RollingStock.Subsystems.ETCS;
using ORTS.Scripting.Api.ETCS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Orts.Viewer3D.RollingStock.Subsystems.ETCS.DriverMachineInterface;

namespace Orts.Viewer3D.RollingStock.SubSystems.ETCS
{
    public class MessageArea : DMIWindow
    {
        readonly Viewer Viewer;

        const float FontHeightMessage = 12;
        const float FontHeightTimestamp = 10;

        WindowTextFont FontTimestamp;
        WindowTextFont FontMessage;
        WindowTextFont FontMessageBold;

        readonly Texture2D[] ScrollUpTexture = new Texture2D[2];
        readonly Texture2D[] ScrollDownTexture = new Texture2D[2];
        int CurrentPage = 0;
        int NumPages = 1;

        readonly Button ButtonScrollUp;
        readonly Button ButtonScrollDown;
        readonly Button ButtonAcknowledgeMessage;

        readonly Point AreaOrigin;

        readonly int MaxTextLines;

        readonly int AreaHeight;

        readonly int RowHeight = 20;

        readonly TextPrimitive[] DisplayedTexts;
        readonly TextPrimitive[] DisplayedTimes;
        bool FlashingFrame;

        readonly List<TextMessage> MessageList = new List<TextMessage>();
        TextMessage? AcknowledgingMessage;

        public MessageArea(DriverMachineInterface dmi, Viewer viewer, Point position) : base(dmi)
        {
            Viewer = viewer;
            MaxTextLines = dmi.IsSoftLayout ? 4 : 5;
            AreaHeight = MaxTextLines * RowHeight;
            AreaOrigin = position;

            DisplayedTexts = new TextPrimitive[MaxTextLines];
            DisplayedTimes = new TextPrimitive[MaxTextLines];

            ScrollUpTexture[0] = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "ETCS", "symbols", "Navigation", "NA_15.bmp"));
            ScrollUpTexture[1] = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "ETCS", "symbols", "Navigation", "NA_13.bmp"));
            ScrollDownTexture[0] = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "ETCS", "symbols", "Navigation", "NA_16.bmp"));
            ScrollDownTexture[1] = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "ETCS", "symbols", "Navigation", "NA_14.bmp"));

            ButtonScrollUp = new Button(Viewer.Catalog.GetString("Scroll Up"), true, new Rectangle(position.X + 234, position.Y, 46, AreaHeight / 2));
            ButtonScrollDown = new Button(Viewer.Catalog.GetString("Scroll Down"), true, new Rectangle(position.X + 234, position.Y + AreaHeight / 2, 46, AreaHeight / 2));
            DMI.SensitiveButtons.Add(ButtonScrollUp);
            DMI.SensitiveButtons.Add(ButtonScrollDown);

            SetFont();
        }
        public override void Draw(SpriteBatch spriteBatch, Point position)
        {
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
            DrawSymbol(spriteBatch, ScrollUpTexture[ButtonScrollUp.Enabled ? 1 : 0], position, 234, 0);
            DrawSymbol(spriteBatch, ScrollDownTexture[ButtonScrollDown.Enabled ? 1 : 0], position, 234, AreaHeight/2);

            if (FlashingFrame)
            {
                spriteBatch.Draw(ColorTexture, ScaledRectangle(position, 0, 0, 234, 1), ColorYellow);
                spriteBatch.Draw(ColorTexture, ScaledRectangle(position, 0, AreaHeight, 234, 1), ColorYellow);
                spriteBatch.Draw(ColorTexture, ScaledRectangle(position, 0, 0, 1, AreaHeight), ColorYellow);
                spriteBatch.Draw(ColorTexture, ScaledRectangle(position, 234, 0, 1, AreaHeight), ColorYellow);
            }
        }

        int CompareMessages(TextMessage m1, TextMessage m2)
        {
            int ack = m1.Acknowledgeable.CompareTo(m2.Acknowledgeable);
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
            DisplayedTimes[row] = new TextPrimitive(new Point(3, 3 + row * RowHeight), Color.White, hour.ToString() + ":" + (minute < 10 ? "0" : "") + minute.ToString(), FontTimestamp);
        }
        void SetTextPrimitive(string text, int row, bool isBold)
        {
            var font = isBold ? FontMessageBold : FontMessage;
            DisplayedTexts[row] = new TextPrimitive(new Point(48, 3 + row * RowHeight), Color.White, text, font);
        }
        string[] GetRowSeparated(string text, bool isBold)
        {
            var font = isBold ? FontMessageBold : FontMessage;
            var size = font.MeasureString(text) / Scale;
            if (size > 234 - 48)
            {
                int split = text.LastIndexOf(' ', 20);
                return new string[] { text.Substring(0, split), text.Substring(split + 1) };
            }
            else
            {
                return new string[] { text };
            }
        }
        void SetMessages()
        {
            for (int i = 0; i < MaxTextLines; i++)
            {
                DisplayedTexts[i] = null;
                DisplayedTimes[i] = null;
            }
            if (MessageList.Count == 0) return;
            if (MessageList[0].Acknowledgeable)
            {
                CurrentPage = 0;
                SetDatePrimitive(MessageList[0].TimestampS, 0);
                string[] text = GetRowSeparated(MessageList[0].Text, false);
                for (int j = 0; j < text.Length && j < MaxTextLines; j++)
                {
                    SetTextPrimitive(text[j], j, false);
                }
                AcknowledgingMessage = MessageList[0];
                NumPages = 1;
            }
            else
            {
                //if (!MessageList[0].Displayed) CurrentPage = 0;
                int row = 0;
                foreach (var msg in MessageList)
                {
                    string[] text = GetRowSeparated(msg.Text, msg.FirstGroup);
                    if (CurrentPage * MaxTextLines <= row && row < (CurrentPage + 1) * MaxTextLines)
                    {
                        SetDatePrimitive(msg.TimestampS, row % MaxTextLines);
                    }
                    for (int j = 0; j < text.Length; j++)
                    {
                        if (CurrentPage * MaxTextLines <= row && row < (CurrentPage + 1) * MaxTextLines)
                        {
                            SetTextPrimitive(text[j], row % MaxTextLines, msg.FirstGroup);
                        }
                        row++;
                    }
                }
                NumPages = row / MaxTextLines + 1;
            }
        }
        public override void PrepareFrame(ETCSStatus status)
        {
            if (AcknowledgingMessage.HasValue)
            {
                if (status.TextMessages.Contains(AcknowledgingMessage.Value)) return;
                AcknowledgingMessage = null;
            }

            MessageList.RemoveAll(x => !status.TextMessages.Contains(x));
            foreach (var msg in status.TextMessages)
            {
                if (!MessageList.Contains(msg)) MessageList.Add(msg);
            }
            MessageList.Sort(CompareMessages);
            SetMessages();

            ButtonScrollDown.Enabled = CurrentPage < NumPages - 1;
            ButtonScrollUp.Enabled = CurrentPage > 0;
        }
        public void HandleInput()
        {
            if (DMI.PressedButton == ButtonScrollDown)
            {
                if (CurrentPage < NumPages - 1)
                {
                    CurrentPage++;
                    SetMessages();
                }
            }
            else if (DMI.PressedButton == ButtonScrollUp)
            {
                if (CurrentPage > 0)
                {
                    CurrentPage--;
                    SetMessages();
                }
            }
            else if (DMI.PressedButton == ButtonAcknowledgeMessage)
            {
                AcknowledgingMessage = null;   
            }
        }
        public void SetFont()
        {
            FontTimestamp = Viewer.WindowManager.TextManager.GetExact("Arial", GetScaledFontSize(FontHeightTimestamp), System.Drawing.FontStyle.Regular);
            FontMessage = Viewer.WindowManager.TextManager.GetExact("Arial", GetScaledFontSize(FontHeightMessage), System.Drawing.FontStyle.Regular);
            FontMessageBold = Viewer.WindowManager.TextManager.GetExact("Arial", GetScaledFontSize(FontHeightMessage), System.Drawing.FontStyle.Bold);
        }
    }
}
