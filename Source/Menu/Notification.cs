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
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ORTS
{
    public class Notification
    {
        public List<NDetail> NDetailList = new List<NDetail>();

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

            public Label Control;

            public void Add(Notification notification)
            {
                notification.NDetailList.Add(this);
            }
        }

        /// <summary>
        /// Title for the notification
        /// </summary>
        public class NTitleControl : NDetail
        {
            public NTitleControl(Panel panelDetails, DateTime date, string text)
            {
                var title = $"Notification 1/1: {date:dd-MMM-yyyy} - {text}";
                var left = LeftPadding;
                Control = new Label
                {
                    Text = title,
                    UseMnemonic = false,
                    Font = new Font(panelDetails.Font, FontStyle.Bold),
                    TextAlign = ContentAlignment.BottomLeft,
                    Height = TitleHeight,
                    Width = panelDetails.Width - ScrollBarWidth - left,
                    Left = LeftPadding
                };
                panelDetails.Controls.Add(Control);
            }
        }
        public class NHeadingControl : NDetail
        {
            public NHeadingControl(Panel panelDetails, string text, Color color = default)
            {
                var left = LeftPadding;
                Control = new Label
                {
                    ForeColor = color,
                    Text = text,
                    UseMnemonic = false,
                    Font = new Font(panelDetails.Font, FontStyle.Bold),
                    TextAlign = ContentAlignment.BottomLeft,
                    Height = HeadingHeight,
                    Width = panelDetails.Width - ScrollBarWidth - left,
                    Left = left,
                    Top = TopPadding,
                };
                panelDetails.Controls.Add(Control);
            }
        }
        public class NTextControl : NDetail
        {
            public NTextControl(Panel panelDetails, string text)
            {
                var left = LeftPaddingIndented;
                Control = new Label
                {
                    Text = text,
                    UseMnemonic = false,
                    Font = new Font(panelDetails.Font, FontStyle.Regular),
                    TextAlign = ContentAlignment.BottomLeft,
                    Height = TextHeight,
                    Width = panelDetails.Width - ScrollBarWidth - left,
                    Left = left,
                };
                panelDetails.Controls.Add(Control);
            }
        }
        public class NButtonControl : NDetail
        {
            public Button Button;
            public NButtonControl(Panel panelDetails, string legend, int width, string description)
            {
                var buttonLeft = LeftPaddingIndented;
                Button = new Button
                {
                    Margin = new Padding(20),
                    Text = legend,
                    UseMnemonic = false,
                    Font = new Font(panelDetails.Font, FontStyle.Regular),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Height = ButtonHeight,
                    Width = width,
                    Left = buttonLeft,
                    Top = TopPadding,
                    BackColor = SystemColors.ButtonFace
                };
                panelDetails.Controls.Add(Button);

                var labelLeft = Button.Left + Button.Width + LeftPaddingIndented;
                Control = new Label
                {
                    Margin = new Padding(20),
                    Text = description,
                    UseMnemonic = false,
                    Font = new Font(panelDetails.Font, FontStyle.Regular),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Height = ButtonHeight,
                    Width = panelDetails.Width - ScrollBarWidth - labelLeft,
                    Top = TopPadding,
                    Left = labelLeft
                };
                panelDetails.Controls.Add(Control);
            }
        }
        public class NRecordControl : NDetail
        {
            public Label Field;
            public NRecordControl(Panel panelDetails, string label, int width, string field)
            {
                Control = new Label
                {
                    Text = label + ":",
                    UseMnemonic = false,
                    Font = new Font(panelDetails.Font, FontStyle.Bold),
                    TextAlign = ContentAlignment.BottomRight,
                    Width = width,
                    Height = RecordHeight,
                    Left = LeftPadding,
                    Top = TopPadding
                };
                panelDetails.Controls.Add(Control);

                var left = width + LeftPadding;
                Field = new Label
                {
                    Text = field,
                    UseMnemonic = false,
                    Font = new Font(panelDetails.Font, FontStyle.Regular),
                    TextAlign = ContentAlignment.BottomLeft,
                    Width = panelDetails.Width - ScrollBarWidth - left,
                    Height = RecordHeight,
                    Left = left,
                    Top = TopPadding
                };
                panelDetails.Controls.Add(Field);
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
