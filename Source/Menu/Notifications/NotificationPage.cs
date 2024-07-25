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
using Button = System.Windows.Forms.Button;
using Image = System.Drawing.Image;

namespace ORTS
{
    public class NotificationPage
    {
        public List<NDetail> NDetailList = new List<NDetail>();
        public Dictionary<int, NButtonControl> ButtonDictionary = new Dictionary<int, NButtonControl>();
        public Panel Panel;
        private MainForm MainForm; // Needed so the Retry button can raise an event which the form can catch.
        public List<Control> ControlList = new List<Control>();

        public NotificationPage(MainForm mainForm, Panel panel, Image nextImage, Image previousImage, Image firstImage, Image lastImage, string pageCount,
            bool previousVisible, bool firstVisible, bool nextVisible, bool lastVisible)
        {
            MainForm = mainForm;
            Panel = panel;
            NButtonControl.ButtonCount = 0;

            var nextPageControl = new Arrow(panel, nextImage, nextVisible, true, 25);
            nextPageControl.Click += new EventHandler(MainForm.Next_Click);
            Panel.Controls.Add(nextPageControl);

            var previousPageControl = new Arrow(panel, previousImage, previousVisible, true, 90);
            previousPageControl.Click += new EventHandler(MainForm.Previous_Click);
            Panel.Controls.Add(previousPageControl);

            Panel.Controls.Add( new Arrow(panel, lastImage, lastVisible, false, 25));
            Panel.Controls.Add( new Arrow(panel, firstImage, firstVisible, false, 90));

            var pageCountControl = new Label
            {
                Text = pageCount,
                UseMnemonic = false,
                Font = new Font(panel.Font, FontStyle.Bold),
                Height = 15,
                Left = panel.ClientSize.Width - 85 + (pageCount.Length * 9), // Keep the text centred between the < > arrows
                Top = 3
            };
            //ControlList.Add(pageCountControl);
            Panel.Controls.Add(pageCountControl);
        }

        public class Arrow : Button {
            public Arrow(Panel panel, Image image, bool visible, bool enabled, int indentRight)
            {
                Margin = new Padding(0);
                Text = "";
                FlatStyle = FlatStyle.Flat;
                Left = panel.ClientSize.Width - indentRight;
                Top = 0;
                Width = 20;
                Anchor = AnchorStyles.Top | AnchorStyles.Left;
                FlatAppearance.BorderSize = 0;
                BackgroundImageLayout = ImageLayout.Center;
                BackgroundImage = image;
                Visible = visible;
                Enabled = enabled;
            }
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

            public NotificationPage Page { get; private set; }
            public Label Control;

            public NDetail(NotificationPage page)
            {
                Page = page;
            }

            /// <summary>
            /// Use as "new NTextControl(NotificationPage, "text").Add();"
            /// </summary>
            public void Add()
            {
                Page.NDetailList.Add(this);
            }
        }

        /// <summary>
        /// Title for the notification
        /// </summary>
        public class NTitleControl : NDetail
        {
            public NTitleControl(NotificationPage page, int current, int total, string date, string text) : base(page)
            {
                var title = $"Notification {current}/{total}: {date} - {text}";
                var left = LeftPadding;
                Control = new Label
                {
                    Text = title,
                    UseMnemonic = false,
                    Font = new Font(Page.Panel.Font, FontStyle.Bold),
                    TextAlign = ContentAlignment.BottomLeft,
                    Height = TitleHeight,
                    Width = page.Panel.Width - ScrollBarWidth - left,
                    Left = LeftPadding
                };
                //page.ControlList.Add(Control);
                page.Panel.Controls.Add(Control);
            }
        }
        public class NHeadingControl : NDetail
        {
            public NHeadingControl(NotificationPage page, string text, string colorName = "blue") : base(page)
            {
                var color = Color.FromName(colorName);
                var left = LeftPadding;
                Control = new Label
                {
                    ForeColor = color,
                    Text = text,
                    UseMnemonic = false,
                    Font = new Font(Page.Panel.Font, FontStyle.Bold),
                    TextAlign = ContentAlignment.BottomLeft,
                    Height = HeadingHeight,
                    Width = page.Panel.Width - ScrollBarWidth - left,
                    Left = left,
                    Top = TopPadding,
                };
                //page.ControlList.Add(Control);
                page.Panel.Controls.Add(Control);
            }
        }
        public class NTextControl : NDetail
        {
            public NTextControl(NotificationPage page, string text, string colorName = "black") : base(page)
            {
                var color = Color.FromName(colorName);
                var left = LeftPaddingIndented;
                Control = new Label
                {
                    ForeColor = color,
                    Text = text,
                    UseMnemonic = false,
                    Font = new Font(Page.Panel.Font, FontStyle.Regular),
                    TextAlign = ContentAlignment.BottomLeft,
                    Height = TextHeight,
                    Width = page.Panel.Width - ScrollBarWidth - left,
                    Left = left,
                };
                //page.ControlList.Add(Control);
                page.Panel.Controls.Add(Control);
            }
        }
        public class NButtonControl : NDetail
        {
            public static int ButtonCount = 0;
            public Button Button;
            public NButtonControl(NotificationPage page, string legend, int width, string description, MainForm mainForm) : base(page)
            {
                var buttonLeft = LeftPaddingIndented;
                Button = new Button
                {
                    Margin = new Padding(20),
                    Text = legend,
                    UseMnemonic = false,
                    Font = new Font(Page.Panel.Font, FontStyle.Regular),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Height = ButtonHeight,
                    Width = width,
                    Left = buttonLeft,
                    Top = TopPadding,
                    BackColor = SystemColors.ButtonFace
                };
                //page.ControlList.Add(Button);
                page.Panel.Controls.Add(Button);

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
                    Font = new Font(Page.Panel.Font, FontStyle.Regular),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Height = ButtonHeight,
                    Width = page.Panel.Width - ScrollBarWidth - labelLeft,
                    Top = TopPadding,
                    Left = labelLeft
                };
                //page.ControlList.Add(Control);
                page.Panel.Controls.Add(Control);
            }
        }
        public class NLinkControl : NButtonControl
        {
            public string Url;
            public NLinkControl(NotificationPage page, string legend, int width, string description, MainForm mainForm, string url)
            : base(page, legend, width, description, mainForm)
            {
                Url = url;
                page.ButtonDictionary.Add(ButtonCount, this);
                ButtonCount++;
            }
        }
        public class NUpdateControl : NButtonControl
        {
            public NUpdateControl(NotificationPage page, string legend, int width, string description, MainForm mainForm)
            : base(page, legend, width, description, mainForm)
            {
                page.ButtonDictionary.Add(ButtonCount, this);
                ButtonCount++;
            }
        }
        public class NRetryControl : NButtonControl
        {
            public NRetryControl(NotificationPage page, string legend, int width, string description, MainForm mainForm)
            : base(page, legend, width, description, mainForm)
            {
                page.ButtonDictionary.Add(ButtonCount, this);
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
            else if (button is NRetryControl)
            {
                MainForm.OnCheckUpdatesAgain(EventArgs.Empty);
            }
        }

        public void DoNext(int key)
        {

        }

        public class NRecordControl : NDetail
        {
            public Label Field;

            public NRecordControl(NotificationPage page, string label, int width, string field) : base(page)
            {
                Control = new Label
                {
                    Text = label + ":",
                    UseMnemonic = false,
                    Font = new Font(page.Panel.Font, FontStyle.Bold),
                    TextAlign = ContentAlignment.BottomRight,
                    Width = width,
                    Height = RecordHeight,
                    Left = LeftPadding,
                    Top = TopPadding
                };
                //page.ControlList.Add(Control);
                Page.Panel.Controls.Add(Control);

                var left = width + LeftPadding;
                Field = new Label
                {
                    Text = field,
                    UseMnemonic = false,
                    Font = new Font(page.Panel.Font, FontStyle.Regular),
                    TextAlign = ContentAlignment.BottomLeft,
                    Width = page.Panel.Width - ScrollBarWidth - left,
                    Height = RecordHeight,
                    Left = left,
                    Top = TopPadding
                };
                //page.ControlList.Add(Field);
                Page.Panel.Controls.Add(Field);
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

                this.Panel.Controls.Add(nDetail.Control);
                top += nDetail.Control.Height + NDetail.VerticalSpacing;
            }
        }
    }
}
