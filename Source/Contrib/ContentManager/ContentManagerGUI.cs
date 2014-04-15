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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ORTS.Settings;

namespace ORTS.ContentManager
{
    public partial class ContentManagerGUI : Form
    {
        readonly UserSettings Settings;
        readonly ContentManager ContentManager;

        public ContentManagerGUI()
        {
            InitializeComponent();

            // Windows 2000 and XP should use 8.25pt Tahoma, while Windows
            // Vista and later should use 9pt "Segoe UI". We'll use the
            // Message Box font to allow for user-customizations, though.
            Font = SystemFonts.MessageBoxFont;

            Settings = new UserSettings(new string[0]);
            ContentManager = new ContentManager(Settings.Folders);

            treeViewContent.Nodes.Add(CreateContentNode(ContentManager));
            treeViewContent.Nodes[0].Expand();

            var width = richTextBoxContent.Font.Height;
            richTextBoxContent.SelectionTabs = new[] { width * 5, width * 15, width * 25, width * 35 };
        }

        void treeViewContent_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.Tag != null && e.Node.Tag is Content && e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Text == "" && e.Node.Nodes[0].Tag == null)
            {
                e.Node.Nodes[0].Text = "Loading...";
                var content = e.Node.Tag as Content;
                var worker = new BackgroundWorker();
                worker.DoWork += (object sender1, DoWorkEventArgs e1) =>
                {
                    e1.Result = ((ContentType[])Enum.GetValues(typeof(ContentType))).SelectMany(ct => content.Get(ct).Select(c => CreateContentNode(c))).ToArray();

                    if (worker.CancellationPending)
                        e1.Cancel = true;
                };
                worker.RunWorkerCompleted += (object sender2, RunWorkerCompletedEventArgs e2) =>
                {
                    // TODO: Cope with nodes being added whilst we worked.
                    if (e2.Cancelled || e.Node.Nodes.Count != 1 || e.Node.Nodes[0].Text != "Loading..." || e.Node.Nodes[0].Tag != null)
                        return;

                    e.Node.Collapse();
                    e.Node.Nodes.Clear();

                    if (e2.Error != null)
                        e.Node.Nodes.Add(new TreeNode(e2.Error.Message));
                    else
                        e.Node.Nodes.AddRange((TreeNode[])e2.Result);

                    if (e.Node.Nodes.Count > 0)
                        e.Node.Expand();
                };
                worker.RunWorkerAsync(e.Node.Tag);
            }
        }

        static TreeNode CreateContentNode(Content c)
        {
            return new TreeNode(String.Format("{0} ({1})", c.Name, c.Type), new[] { new TreeNode() }) { Tag = c };
        }

        void treeViewContent_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag == null || !(e.Node.Tag is Content))
                return;

            var link = new Native.CharFormat2
            {
                Size = Marshal.SizeOf(typeof(Native.CharFormat2)),
                Mask = Native.CfmLink,
                Effects = Native.CfmLink,
            };

            richTextBoxContent.Clear();
            richTextBoxContent.Text = ContentInfo.GetText(e.Node.Tag as Content);
            var boldFont = new Font(richTextBoxContent.Font, FontStyle.Bold);
            var start = 0;
            while (richTextBoxContent.Find(":\t", start, RichTextBoxFinds.None) >= 0)
            {
                var endPos = richTextBoxContent.Find(":\t", start, RichTextBoxFinds.None);
                var line = richTextBoxContent.GetLineFromCharIndex(endPos);
                var startPos = richTextBoxContent.GetFirstCharIndexFromLine(line);
                if (startPos <= start && start > 0)
                    startPos = start + 1;
                richTextBoxContent.Select(startPos, endPos - startPos + 1);
                if (richTextBoxContent.SelectedText.IndexOf("\t") == -1)
                    richTextBoxContent.SelectionFont = boldFont;
                start = endPos + 1;
            }
            var linkRe = new Regex("\u0001(.*?)\u0002(.*?)\u0001");
            var linkMatch = linkRe.Match(richTextBoxContent.Text);
            while (linkMatch.Success)
            {
                richTextBoxContent.Select(linkMatch.Index, linkMatch.Length);
                richTextBoxContent.SelectedRtf = String.Format(@"{{\rtf{{{0}{{\v{{\u1.{0}\u1.{1}}}\v0}}}}}}", linkMatch.Groups[1].Value, linkMatch.Groups[2].Value);
                richTextBoxContent.Select(linkMatch.Index, linkMatch.Groups[1].Value.Length * 2 + linkMatch.Groups[2].Value.Length + 2);
                Native.SendMessage(richTextBoxContent.Handle, Native.EmSetCharFormat, Native.ScfSelection, ref link);
                linkMatch = linkRe.Match(richTextBoxContent.Text);
            }
            richTextBoxContent.Select(0, 0);
            richTextBoxContent.SelectionFont = richTextBoxContent.Font;
        }

        void richTextBoxContent_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            var content = treeViewContent.SelectedNode.Tag as Content;
            var link = e.LinkText.Split('\u0001');
            if (content != null && link.Length == 3)
            {
                var newContent = content.Get(link[1], (ContentType)Enum.Parse(typeof(ContentType), link[2]));
                if (newContent != null)
                {
                    treeViewContent.SelectedNode.Expand();
                    treeViewContent.SelectedNode.Nodes.Insert(0, CreateContentNode(newContent));
                    treeViewContent.SelectedNode = treeViewContent.SelectedNode.Nodes[0];
                    treeViewContent.Focus();
                }
                else
                {
                    MessageBox.Show(String.Format("No content was returned for the query:\n\nName: {0}\nType: {1}", link[1], link[2]), Application.ProductName + " " + Application.ProductVersion);
                }
            }
        }
    }

    sealed class Native
    {
        public const int WmUser = 0x0400;
        public const int EmSetCharFormat = WmUser + 68;
        public const int ScfSelection = 0x0001;
        public const int CfmLink = 0x00000020;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr window, int msg, int wParam, ref CharFormat2 lParam);

        public struct CharFormat2
        {
            public int Size;
            public int Mask;
            public int Effects;
            public int Height;
            public int Offset;
            public int TextColor;
            public byte CharSet;
            public byte PitchAndFamily;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string FaceName;
            public short Weight;
            public short Spacing;
            public int BackColor;
            public int Lcid;
            public int Reserved;
            public short Style;
            public short Kerning;
            public byte UnderlineType;
            public byte Animation;
            public byte RevAuthor;
            public byte Reserved1;
        }
    }
}
