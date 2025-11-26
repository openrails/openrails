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

using ORTS.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ORTS.ContentManager
{
    public partial class ContentManagerGUI : Form
    {
        readonly UserSettings Settings;
        readonly ContentManager ContentManager;

        readonly Regex ContentBold = new Regex("(?:^|\t)([\\w ]+:)\t", RegexOptions.Multiline);
        readonly Regex ContentLink = new Regex("\u0001(.*?)\u0002(.*?)\u0001");

        Content PendingSelection;
        List<string> PendingSelections = new List<string>();

        BackgroundWorker SearchWorker = null;
        const string InitialSearchPendingText = "Searching...";
        const int InitialSearchPendingLength = 9;
        int SearchPendingLength = InitialSearchPendingLength;
        DateTimeOffset SearchPendingTime;

        class SearchResult
        {
            public string Name;
            public string[] Path;
            public SearchResult(Content content, string path)
            {
                var placeEnd = Math.Max(path.LastIndexOf(" / "), path.LastIndexOf(" -> "));
                var place = path.Substring(0, placeEnd);
                Name = string.Format("{0} ({1}) in {2}", content.Name, content.Type, place);
                Path = path.Split(new[] { " / ", " -> " }, StringSplitOptions.None);
            }

            public override string ToString()
            {
                return Name;
            }
        }

        public ContentManagerGUI()
        {
            InitializeComponent();

            // Windows 2000 and XP should use 8.25pt Tahoma, while Windows
            // Vista and later should use 9pt "Segoe UI". We'll use the
            // Message Box font to allow for user-customizations, though.
            Font = SystemFonts.MessageBoxFont;

            Settings = new UserSettings(new string[0]);
            ContentManager = new ContentManager(Settings.Folders);

            // add unit settings to ContentInfo static class; only considering explict setting or system setting
            if (Settings.Units == "Metric") { ContentInfo.IsMetric = true; }
            else if (Settings.Units == "UK") { ContentInfo.IsUK = true; }
            else // setting is Automatic or Route
            {
                ContentInfo.IsMetric = System.Globalization.RegionInfo.CurrentRegion.IsMetric;
                // special cases
                if (System.Globalization.RegionInfo.CurrentRegion.Name == "UK") { ContentInfo.IsUK = true; }
                else if (System.Globalization.RegionInfo.CurrentRegion.Name == "CA") { ContentInfo.IsMetric = false; } // Canada is metric, but RRs use imperial US
            }

            // Start off the tree with the Content Manager itself at the root and expand to show packages.
            treeViewContent.Nodes.Add(CreateContentNode(ContentManager));
            treeViewContent.Nodes[0].Expand();

            var width = richTextBoxContent.Font.Height;
            richTextBoxContent.SelectionTabs = new[] { width * 5, width * 15, width * 25, width * 35 };
        }

        void treeViewContent_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            // Are we about to expand a not-yet-loaded node? This is identified by a single child with no text or tag.
            if (e.Node.Tag != null && e.Node.Tag is Content && e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Text == "" && e.Node.Nodes[0].Tag == null)
            {
                // Make use of the single child to show a loading message.
                e.Node.Nodes[0].Text = "Loading...";
                var content = e.Node.Tag as Content;
                var worker = new BackgroundWorker();
                worker.WorkerSupportsCancellation = true;
                worker.DoWork += (object sender1, DoWorkEventArgs e1) =>
                {
                    // Get all the different possible types of content from this content item.
                    var getChildren = ((ContentType[])Enum.GetValues(typeof(ContentType))).SelectMany(ct => content.Get(ct).Select(c => CreateContentNode(c)));
                    var linkChildren = ContentLink.Matches(ContentInfo.GetText(e.Node.Tag as Content)).Cast<Match>().Select(linkMatch => CreateContentNode(content, linkMatch.Groups[1].Value, (ContentType)Enum.Parse(typeof(ContentType), linkMatch.Groups[2].Value)));
                    Debug.Assert(!getChildren.Any() || !linkChildren.Any(), "Content item should not return items from Get(ContentType) and Get(string, ContentType)");
                    e1.Result = getChildren.Concat(linkChildren).ToArray();

                    if (worker.CancellationPending)
                        e1.Cancel = true;
                };
                worker.RunWorkerCompleted += (object sender2, RunWorkerCompletedEventArgs e2) =>
                {
                    // If we got cancelled, or the loading node is missing, we should abort here to avoid issues caused by double-loading.
                    if (e2.Cancelled || e.Node.Nodes.Count < 1 || e.Node.Nodes[0].Text != "Loading..." || e.Node.Nodes[0].Tag != null)
                        return;

                    // Get results from worker.
                    var nodes = e2.Result as TreeNode[];

                    // Collapse node if we're going to end up with no child nodes.
                    if (e2.Error == null && nodes.Length == 0)
                        e.Node.Collapse();

                    // Remove the loading node.
                    e.Node.Nodes.RemoveAt(0);

                    // Add either the error we encountered or the resulting content items.
                    if (e2.Error != null)
                        e.Node.Nodes.Add(new TreeNode(e2.Error.Message));
                    else
                        e.Node.Nodes.AddRange(nodes);

                    CheckForPendingActions(e.Node);
                };
                worker.RunWorkerAsync(e.Node.Tag);
            }
            else
            {
                var worker = new BackgroundWorker();
                worker.RunWorkerCompleted += (object sender2, RunWorkerCompletedEventArgs e2) =>
                {
                    CheckForPendingActions(e.Node);
                };
                worker.RunWorkerAsync(e.Node.Tag);
            }
        }

        void CheckForPendingActions(TreeNode startNode)
        {
            if (PendingSelection != null)
            {
                var pendingSelectionNode = startNode.Nodes.OfType<TreeNode>().FirstOrDefault(node => (Content)node.Tag == PendingSelection);
                if (pendingSelectionNode != null)
                {
                    treeViewContent.SelectedNode = pendingSelectionNode;
                    treeViewContent.Focus();
                }
                PendingSelection = null;
            }
            if (PendingSelections.Count > 0)
            {
                var pendingSelectionNode = startNode.Nodes.OfType<TreeNode>().FirstOrDefault(node => ((Content)node.Tag).Name == PendingSelections[0]);
                if (pendingSelectionNode != null)
                {
                    treeViewContent.SelectedNode = pendingSelectionNode;
                    treeViewContent.SelectedNode.Expand();
                }
                PendingSelections.RemoveAt(0);
            }
        }

        static TreeNode CreateContentNode(Content content, string name, ContentType type)
        {
            var c = content.Get(name, type);
            if (c != null)
                return CreateContentNode(c);
            return new TreeNode(String.Format("Missing: {0} ({1})", name, type));
        }

        static TreeNode CreateContentNode(Content c)
        {
            return new TreeNode(String.Format("{0} ({1})", c.Name, c.Type), new[] { new TreeNode() }) { Tag = c };
        }

        void treeViewContent_AfterSelect(object sender, TreeViewEventArgs e)
        {
            richTextBoxContent.Clear();

            if (e.Node.Tag == null || !(e.Node.Tag is Content))
                return;

            var link = new Native.CharFormat2
            {
                Size = Marshal.SizeOf(typeof(Native.CharFormat2)),
                Mask = Native.CfmLink,
                Effects = Native.CfmLink,
            };

            Trace.TraceInformation("Updating richTextBoxContent with content {0}", e.Node.Tag as Content);
            richTextBoxContent.Text = ContentInfo.GetText(e.Node.Tag as Content);
            var boldFont = new Font(richTextBoxContent.Font, FontStyle.Bold);
            var boldMatch = ContentBold.Match(richTextBoxContent.Text);
            while (boldMatch.Success)
            {
                richTextBoxContent.Select(boldMatch.Groups[1].Index, boldMatch.Groups[1].Length);
                richTextBoxContent.SelectionFont = boldFont;
                boldMatch = ContentBold.Match(richTextBoxContent.Text, boldMatch.Groups[1].Index + boldMatch.Groups[1].Length);
            }
            var linkMatch = ContentLink.Match(richTextBoxContent.Text);
            while (linkMatch.Success)
            {
                richTextBoxContent.Select(linkMatch.Index, linkMatch.Length);
                richTextBoxContent.SelectedRtf = String.Format(@"{{\rtf{{{0}{{\v{{\u1.{0}\u1.{1}}}\v0}}}}}}", linkMatch.Groups[1].Value, linkMatch.Groups[2].Value);
                richTextBoxContent.Select(linkMatch.Index, linkMatch.Groups[1].Value.Length * 2 + linkMatch.Groups[2].Value.Length + 2);
                Native.SendMessage(richTextBoxContent.Handle, Native.EmSetCharFormat, Native.ScfSelection, ref link);
                linkMatch = ContentLink.Match(richTextBoxContent.Text);
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
                PendingSelection = content.Get(link[1], (ContentType)Enum.Parse(typeof(ContentType), link[2]));
                if (treeViewContent.SelectedNode.IsExpanded)
                {
                    var pendingSelectionNode = treeViewContent.SelectedNode.Nodes.Cast<TreeNode>().FirstOrDefault(node => (Content)node.Tag == PendingSelection);
                    if (pendingSelectionNode != null)
                    {
                        treeViewContent.SelectedNode = pendingSelectionNode;
                        treeViewContent.Focus();
                    }
                    PendingSelection = null;
                }
                else
                {
                    treeViewContent.SelectedNode.Expand();
                }
            }
        }

        void searchBox_TextChanged(object sender, EventArgs e)
        {
            if (SearchWorker != null)
            {
                SearchWorker.CancelAsync();
                SearchWorker = null;
            }

            searchResults.Items.Clear();
            searchResults.Visible = searchBox.Text.Length > 0;
            if (!searchResults.Visible)
                return;

            searchResults.Items.Add("");
            SearchPendingTime = DateTimeOffset.Now;

            var worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += (object sender1, DoWorkEventArgs e1) =>
            {
                var search = (e1.Argument as string).ToLowerInvariant();
                var finds = new List<SearchResult>();
                var pending = new Dictionary<string, Content>();
                pending.Add(ContentManager.Name, ContentManager);

                while (pending.Count > 0 && !worker.CancellationPending)
                {
                    var path = pending.Keys.First();
                    var content = pending[path];
                    pending.Remove(path);

                    // do not add root element to search results, see bug 1944070
                    if (content.Type != ContentType.Root && content.Name.ToLowerInvariant().Contains(search))
                        finds.Add(new SearchResult(content, path));

                    foreach (var child in ((ContentType[])Enum.GetValues(typeof(ContentType))).SelectMany(ct => content.Get(ct)))
                        if (!pending.ContainsKey(path + " / " + child.Name))
                            pending.Add(path + " / " + child.Name, child);

                    foreach (var child in ContentLink.Matches(ContentInfo.GetText(content)).Cast<Match>().Select(linkMatch => content.Get(linkMatch.Groups[1].Value, (ContentType)Enum.Parse(typeof(ContentType), linkMatch.Groups[2].Value))).Where(linkContent => linkContent != null))
                        if (!pending.ContainsKey(path + " -> " + child.Name))
                            pending.Add(path + " -> " + child.Name, child);

                    searchResults.Invoke((Action<List<SearchResult>, int, bool>)UpdateSearchResults, finds, pending.Count, false);
                }

                e1.Result = finds;

                if (worker.CancellationPending)
                    e1.Cancel = true;
            };
            worker.RunWorkerCompleted += (object sender2, RunWorkerCompletedEventArgs e2) =>
            {
                if (e2.Cancelled)
                    return;

                var finds = e2.Result as List<SearchResult>;

                if (e2.Error != null)
                    MessageBox.Show(e2.Error.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);

                searchResults.Invoke((Action<List<SearchResult>, int, bool>)UpdateSearchResults, finds, 0, true);
            };
            SearchWorker = worker;
            SearchWorker.RunWorkerAsync(searchBox.Text);
        }

        void UpdateSearchResults(List<SearchResult> results, int pending, bool done)
        {
            if (done || (DateTimeOffset.Now - SearchPendingTime).TotalSeconds >= 0.5)
            {
                searchResults.Items.AddRange(results.Skip(searchResults.Items.Count - 1).ToArray());
                SearchPendingLength++;
                if (SearchPendingLength > InitialSearchPendingText.Length) SearchPendingLength = InitialSearchPendingLength;
                searchResults.Items[0] = "[" + pending + "/" + results.Count + "] " + InitialSearchPendingText.Substring(0, SearchPendingLength);
                SearchPendingTime = DateTimeOffset.Now;
            }
            if (done)
            {
                searchResults.Items.RemoveAt(0);
            }
        }

        void searchResults_DoubleClick(object sender, EventArgs e)
        {
            var result = searchResults.SelectedItem as SearchResult;
            if (result == null)
                return;

            PendingSelections.Clear();
            PendingSelections.AddRange(result.Path.Skip(1).ToArray());
            treeViewContent.Focus();
            treeViewContent.CollapseAll();
            treeViewContent.SelectedNode = treeViewContent.Nodes[0];
            treeViewContent.SelectedNode.Expand();
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
