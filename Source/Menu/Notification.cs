// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using ORTS.Updater;

namespace ORTS
{
    public class Notification
    {
        public List<NDetail> NDetailList = new List<NDetail>();
        public Dictionary<int, NButtonControl> ButtonDictionary = new Dictionary<int, NButtonControl>();
        public Panel Panel;

        public Notification(Panel panel)
        {
            Panel = panel;
            NButtonControl.ButtonCount = 0;
        }

        public class NDetail
        {
            public static readonly int TopPadding = 20;
            public static readonly int VerticalSpacing = 10;
            public static readonly int LeftPadding = 10;
            public static readonly int LeftPaddingIndented = 20;
            public static readonly int TitleHeight = 20;
            public static readonly int HeadingHeight = 30;
            public static readonly int TextHeight = 18;
            public static readonly int ButtonHeight = 30;
            public static readonly int RecordHeight = 15;
            public const int ScrollBarWidth = 20;

            public Notification Notification { get; private set; }
            public Label Control;

            public NDetail(Notification notification)
            {
                Notification = notification;
            }
            public void Add()
            {                
                Notification.NDetailList.Add(this);
            }
        }

        /// <summary>
        /// Title for the notification
        /// </summary>
        public class NTitleControl : NDetail
        {
            public NTitleControl(Notification notification, DateTime date, string text) : base(notification)
            {
                var title = $"Notification 1/1: {date:dd-MMM-yyyy} - {text}";
                var left = LeftPadding;
                Control = new Label
                {
                    Text = title,
                    UseMnemonic = false,
                    Font = new Font(Notification.Panel.Font, FontStyle.Bold),
                    TextAlign = ContentAlignment.BottomLeft,
                    Height = TitleHeight,
                    Width = Notification.Panel.Width - ScrollBarWidth - left,
                    Left = LeftPadding
                };
                Notification.Panel.Controls.Add(Control);
            }
        }
        public class NHeadingControl : NDetail
        {
            public NHeadingControl(Notification notification, string text, Color color = default) : base(notification)
            {
                var left = LeftPadding;
                Control = new Label
                {
                    ForeColor = color,
                    Text = text,
                    UseMnemonic = false,
                    Font = new Font(Notification.Panel.Font, FontStyle.Bold),
                    TextAlign = ContentAlignment.BottomLeft,
                    Height = HeadingHeight,
                    Width = Notification.Panel.Width - ScrollBarWidth - left,
                    Left = left,
                    Top = TopPadding,
                };
                Notification.Panel.Controls.Add(Control);
            }
        }
        public class NTextControl : NDetail
        {
            public NTextControl(Notification notification, string text) : base(notification)
            {
                var left = LeftPaddingIndented;
                Control = new Label
                {
                    Text = text,
                    UseMnemonic = false,
                    Font = new Font(Notification.Panel.Font, FontStyle.Regular),
                    TextAlign = ContentAlignment.BottomLeft,
                    Height = TextHeight,
                    Width = Notification.Panel.Width - ScrollBarWidth - left,
                    Left = left,
                };
                Notification.Panel.Controls.Add(Control);
            }
        }
        public class NButtonControl : NDetail
        {
            public static int ButtonCount = 0;
            public Button Button;
            public NButtonControl(Notification notification, string legend, int width, string description, MainForm mainForm) : base(notification)
    {
                var buttonLeft = LeftPaddingIndented;
                Button = new Button
                {
                    Margin = new Padding(20),
                    Text = legend,
                    UseMnemonic = false,
                    Font = new Font(Notification.Panel.Font, FontStyle.Regular),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Height = ButtonHeight,
                    Width = width,
                    Left = buttonLeft,
                    Top = TopPadding,
                    BackColor = SystemColors.ButtonFace
                };
                Notification.Panel.Controls.Add(Button);

                // 3 should be enough, but is there a way to get unlimited buttons?
                switch (ButtonCount)
                {
                    case 0:
                        Button.Click += new EventHandler(mainForm.Button0_Click);
                        break;
                    case 1:
                        Button.Click += new EventHandler(mainForm.Button1_Click);
                        break;
                    case 2:
                        Button.Click += new EventHandler(mainForm.Button2_Click);
                        break;
                    default:
                        MessageBox.Show("Cannot add 4th button; only 3 buttons are supported.", "Add Notification Button");
                        break;
                }

                var labelLeft = Button.Left + Button.Width + LeftPaddingIndented;
                Control = new Label
                {
                    Margin = new Padding(20),
                    Text = description,
                    UseMnemonic = false,
                    Font = new Font(Notification.Panel.Font, FontStyle.Regular),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Height = ButtonHeight,
                    Width = Notification.Panel.Width - ScrollBarWidth - labelLeft,
                    Top = TopPadding,
                    Left = labelLeft
                };
                Notification.Panel.Controls.Add(Control);
            }
        }
        public class NLinkControl : NButtonControl
        {
            public string Url;
            public NLinkControl(Notification notification, string legend, int width, string description, MainForm mainForm, string url) 
            : base(notification, legend, width, description, mainForm)
            { 
                Url = url;
                Notification.ButtonDictionary.Add(ButtonCount, this);
                ButtonCount++;
            }
        }
        public class NUpdateControl : NButtonControl
        {
            public NUpdateControl(Notification notification, string legend, int width, string description, MainForm mainForm)
            : base(notification, legend, width, description, mainForm)
            {
                Notification.ButtonDictionary.Add(ButtonCount, this);
                ButtonCount++;
            }
        }
        public void DoButton(UpdateManager updateManager, int key)
        {
            var button = ButtonDictionary[key];
            if (button is NLinkControl)
                Process.Start(((NLinkControl)button).Url);
            else if (button is NUpdateControl)
                updateManager.Update();
        }

        public class NRecordControl : NDetail
        {
            public Label Field;
            public NRecordControl(Notification notification, string label, int width, string field) : base(notification)
        {
                Control = new Label
                {
                    Text = label + ":",
                    UseMnemonic = false,
                    Font = new Font(Notification.Panel.Font, FontStyle.Bold),
                    TextAlign = ContentAlignment.BottomRight,
                    Width = width,
                    Height = RecordHeight,
                    Left = LeftPadding,
                    Top = TopPadding
                };
                Notification.Panel.Controls.Add(Control);

                var left = width + LeftPadding;
                Field = new Label
                {
                    Text = field,
                    UseMnemonic = false,
                    Font = new Font(Notification.Panel.Font, FontStyle.Regular),
                    TextAlign = ContentAlignment.BottomLeft,
                    Width = Notification.Panel.Width - ScrollBarWidth - left,
                    Height = RecordHeight,
                    Left = left,
                    Top = TopPadding
                };
                Notification.Panel.Controls.Add(Field);
            }
        }

        public void FlowNDetails()
        {
            var top = 2;
            foreach (var nDetail in NDetailList)
            {
                nDetail.Control.Top = top;

                // Adjust details that have a second part.
                if (nDetail is NButtonControl)
                {
                    ((NButtonControl)nDetail).Button.Top = top;
                }
                if (nDetail is NRecordControl)
                {
                    ((NRecordControl)nDetail).Field.Top = top;
                }

                top += nDetail.Control.Height + NDetail.VerticalSpacing;
            }
        }
    }
}
