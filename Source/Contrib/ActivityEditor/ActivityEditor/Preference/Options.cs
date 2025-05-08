using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LibAE.Formats;
using ActivityEditor.ActionProperties;
using Orts.Formats.OR;
using ORTS.Common;

namespace ActivityEditor.Preference
{
    public partial class Options : Form
    {
        List<string> routePaths;

        public Options()
        {
            InitializeComponent();
            Program.aePreference.UpdateConfig();
            this.checkBox1.Checked = Program.aePreference.ShowAllSignal;
            this.ShowSnap.Checked = Program.aePreference.ShowSnapCircle;
            this.ShowLabelPlat.Checked = Program.aePreference.ShowPlSiLabel;
            this.MSTSPath.Text = Program.aePreference.MSTSPath;
            this.AEPath.Text = Program.aePreference.AEPath;
            ListRoutePaths.DataSource = Program.aePreference.RoutePaths;
            routePaths = new List<string> ();
            this.showTiles.Checked = Program.aePreference.ShowTiles;
            this.snapTrack.Checked = Program.aePreference.ShowSnapLine;
            this.SnapInfo.Checked = Program.aePreference.ShowSnapInfo;
            this.showRuler.Checked = Program.aePreference.ShowRuler;
            this.snapLine.Checked = Program.aePreference.ShowSnapLine;
            this.trackInfo.Checked = Program.aePreference.ShowTrackInfo;
            this.ListAvailable.DataSource = Program.aePreference.AvailableActions;
            this.ListUsed.DataSource = Program.aePreference.UsedActions;
        }
        
        private void DrawOnTab(object sender, DrawItemEventArgs e)
        {
            Font font;
            Brush back_brush;
            Brush fore_brush;
            Rectangle bounds = e.Bounds;

            this.tabControl1.Controls[e.Index].BackColor = Color.Silver;
            if (e.Index == this.tabControl1.SelectedIndex)
            {
                font = new Font(e.Font, e.Font.Style);
                back_brush = new SolidBrush(Color.DimGray);
                fore_brush = new SolidBrush(Color.White);
                bounds = new Rectangle(bounds.X + (this.tabControl1.Padding.X / 2), 
                    bounds.Y + this.tabControl1.Padding.Y, 
                    bounds.Width - this.tabControl1.Padding.X, 
                    bounds.Height - (this.tabControl1.Padding.Y * 2));
            }
            else
            {
                font = new Font(e.Font, e.Font.Style & ~FontStyle.Bold);
                back_brush = new SolidBrush(this.tabControl1.TabPages[e.Index].BackColor);
                fore_brush = new SolidBrush(this.tabControl1.TabPages[e.Index].ForeColor);
            }
            string tab_name = this.tabControl1.TabPages[e.Index].Text;
            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Center;
            sf.LineAlignment = StringAlignment.Center;
            e.Graphics.FillRectangle(back_brush, bounds);
            e.Graphics.DrawString(tab_name, font, fore_brush, bounds, sf);
            /*
            Brush background_brush = new SolidBrush(Color.DodgerBlue);
            Rectangle LastTabRect = this.tabControl1.GetTabRect(this.tabControl1.TabPages.Count - 1);
            Rectangle rect = new Rectangle();
            rect.Location = new Point(LastTabRect.Right + this.Left, this.Top);
            rect.Size = new Size(this.Right - rect.Left, LastTabRect.Height);
            e.Graphics.FillRectangle(background_brush, rect);
            background_brush.Dispose();
            sf.Dispose();
            back_brush.Dispose();
            fore_brush.Dispose();
            font.Dispose();
             */
        }

        private void browseMSTSPath_Click(object sender, EventArgs e)
        {
            if (MSTSfolderBrowse.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string path = MSTSfolderBrowse.SelectedPath;
                MSTSPath.Text = path;
                Program.aePreference.MSTSPath = path;
                Vfs.Initialize(path, Path.GetDirectoryName(Application.ExecutablePath));
                string completeFileName = "/MSTS/ROUTES";
                if (Vfs.DirectoryExists(completeFileName))
                {
                    Program.aePreference.RoutePaths.Add(completeFileName);
                    ListRoutePaths.DataSource = null;
                    ListRoutePaths.DataSource = Program.aePreference.RoutePaths;
                    RemoveRoutePaths.Enabled = true;

                }
            }
        }

        private void browseFilePath_Click(object sender, EventArgs e)
        {
            if (VfsFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string path = VfsFileDialog.FileName;
                MSTSPath.Text = path;
                Program.aePreference.MSTSPath = path;
                Vfs.Initialize(path, Path.GetDirectoryName(Application.ExecutablePath));
                string completeFileName = "/MSTS/ROUTES";
                if (Vfs.DirectoryExists(completeFileName))
                {
                    Program.aePreference.RoutePaths.Add(completeFileName);
                    ListRoutePaths.DataSource = null;
                    ListRoutePaths.DataSource = Program.aePreference.RoutePaths;
                    RemoveRoutePaths.Enabled = true;

                }
            }
        }

        private void AddRoutePaths_Click(object sender, EventArgs e)
        {
            if (MSTSfolderBrowse.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string path = MSTSfolderBrowse.SelectedPath;
                Program.aePreference.RoutePaths.Add(path);
                ListRoutePaths.DataSource = null;
                ListRoutePaths.DataSource = Program.aePreference.RoutePaths;
                RemoveRoutePaths.Enabled = true;
            }
        }

        private void RemoveRoutePaths_Click(object sender, EventArgs e)
        {
            int selected = -1;
            //  Trouver le sélectionné
            selected = ListRoutePaths.SelectedIndex;
            if (selected >= 0)
            {
                Program.aePreference.RoutePaths.RemoveAt(selected);
                ListRoutePaths.DataSource = null;
                ListRoutePaths.DataSource = Program.aePreference.RoutePaths;
            }
            if (routePaths.Count < 1)
            {
                RemoveRoutePaths.Enabled = false;
                return;
            }
        }

        private void configureRoutePath ()
        {
            if (Program.aePreference.RoutePaths.Count <= 0)
            {
                this.ListRoutePaths.DataSource = null;
                RemoveRoutePaths.Enabled = false;
            }
            else
            {
                this.ListRoutePaths.DataSource = Program.aePreference.RoutePaths;
                RemoveRoutePaths.Enabled = true;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Program.aePreference.ShowAllSignal = checkBox1.Checked;
        }

        private void CheckedChanged(object sender, EventArgs e)
        {
            this.snapCircle.Enabled = ShowSnap.Checked;
            this.snapCircleLabel.Enabled = ShowSnap.Checked;
            this.snapCircle.Value = Program.aePreference.getSnapCircle()>0?Program.aePreference.getSnapCircle():2;
            Program.aePreference.ShowSnapCircle = ShowSnap.Checked;
        }

        private void PlSiShow(object sender, EventArgs e)
        {
            this.PlSiZoomLevel.Enabled = ShowLabelPlat.Checked;
            this.PlSiLabel.Enabled = ShowLabelPlat.Checked;
            this.PlSiZoomLevel.Value = (decimal)(Program.aePreference.getPlSiZoom() > 0 ? Program.aePreference.getPlSiZoom() : 1);
            Program.aePreference.ShowPlSiLabel = ShowLabelPlat.Checked;
            Program.aePreference.PlSiZoom = (float)this.PlSiZoomLevel.Value;
        }

        private void snapCircle_ValueChanged(object sender, EventArgs e)
        {
            Program.aePreference.setSnapCircle((int)((NumericUpDown)sender).Value);
        }

        private void PlSiValue(object sender, EventArgs e)
        {
            Program.aePreference.setPlSiZoom((float)((NumericUpDown)sender).Value);
            this.PlSiZoomLevel.Value = (decimal)(Program.aePreference.getPlSiZoom() > 0 ? Program.aePreference.getPlSiZoom() : 1);
        }

        private void showTiles_CheckedChanged(object sender, EventArgs e)
        {
            Program.aePreference.ShowTiles = showTiles.Checked;
        }

        private void snapTrack_CheckedChanged(object sender, EventArgs e)
        {
            Program.aePreference.ShowSnapLine = snapTrack.Checked;
        }

        private void SnapInfo_CheckedChanged(object sender, EventArgs e)
        {
            Program.aePreference.ShowSnapInfo = SnapInfo.Checked;
        }

        private void showRuler_CheckedChanged(object sender, EventArgs e)
        {
            Program.aePreference.ShowRuler = showRuler.Checked;
        }
        private void optionOK_click(object sender, EventArgs e)
        {
            Close();
            Program.aePreference.ShowAllSignal = this.checkBox1.Checked ;
            Program.aePreference.ShowSnapCircle = this.ShowSnap.Checked;
            Program.aePreference.ShowPlSiLabel = this.ShowLabelPlat.Checked;
            Program.aePreference.MSTSPath = this.MSTSPath.Text;
            //Program.aePreference.AEPath = this.AEPath.Text;
            Program.aePreference.ShowTiles = this.showTiles.Checked;
            Program.aePreference.ShowSnapLine = this.snapTrack.Checked;
            Program.aePreference.ShowSnapInfo = this.SnapInfo.Checked;
            Program.aePreference.ShowRuler = this.showRuler.Checked;
            Program.aePreference.ShowSnapLine = snapLine.Checked;
            Program.aePreference.ShowTrackInfo = this.trackInfo.Checked;
            Program.aePreference.saveXml();
        }

        private void snapLine_CheckedChanged(object sender, EventArgs e)
        {
            Program.aePreference.ShowSnapLine = snapLine.Checked;
        }

        private void trackInfo_changed(object sender, EventArgs e)
        {
            Program.aePreference.ShowTrackInfo = trackInfo.Checked;
        }

        private void AddToUsed(object sender, EventArgs e)
        {
            int selected = -1;
            //  Trouver le sélectionné
            selected = ListAvailable.SelectedIndex;
            if (selected >= 0)
            {
                Program.aePreference.AddGenAction(Program.aePreference.AvailableActions[selected]);
                ListUsed.DataSource = null;
                ListUsed.DataSource = Program.aePreference.UsedActions;

            }
            if (routePaths.Count < 1)
            {
                return;
            }
        }

        private void RemoveFromUsed(object sender, EventArgs e)
        {
            int selected = -1;
            //  Trouver le sélectionné
            selected = ListUsed.SelectedIndex;
            if (selected >= 0)
            {
                Program.aePreference.RemoveGenAction(selected);
                ListUsed.DataSource = null;
                ListUsed.DataSource = Program.aePreference.UsedActions;

            }
            if (routePaths.Count < 1)
            {
                return;
            }
        }

        public void EditProperties(object sender, EventArgs e)
        {
            int selected = -1;
            //  Trouver le sélectionné
            selected = ListUsed.SelectedIndex;
            if (selected >= 0)
            {
                AuxActionRef action = Program.aePreference.GetAction(selected);
                if (action != null)
                {
                    if (action.GetType() == typeof(AuxActionHorn))
                        EditHornProperties(action);
                    else if (action.GetType() == typeof(AuxControlStart))
                        EditControlStartProperties(action);
                }
            }
        }

        public void EditHornProperties(AuxActionRef action)
        {
            HornProperties hornProperties = new HornProperties(action);
            hornProperties.ShowDialog();
            ((AuxActionHorn)action).SaveProperties(hornProperties.Action);

        }

        public void EditControlStartProperties(AuxActionRef action)
        {
            ControlStartProperties controlStartProperties = new ControlStartProperties(action);
            controlStartProperties.ShowDialog();
            ((AuxControlStart)action).SaveProperties(controlStartProperties.Action);
        }

        public void ShowCommentUsed(object sender, EventArgs e)
        {
            int selected = -1;
            //  Trouver le sélectionné
            selected = ListUsed.SelectedIndex;
            if (selected >= 0)
            {
                CommentAction.Text = Program.aePreference.GetComment(Program.aePreference.AvailableActions[selected]);
            }
        }

        public void ShowCommentAvailable(object sender, EventArgs e)
        {
            int selected = -1;
            //  Trouver le sélectionné
            selected = ListAvailable.SelectedIndex;
            if (selected >= 0)
            {
                CommentAction.Text = Program.aePreference.GetComment(Program.aePreference.AvailableActions[selected]);
            }
        }

        private void MouseDownUsed(object sender, MouseEventArgs e)
        {
            ListUsed.SelectedIndex = ListUsed.IndexFromPoint(e.X, e.Y);
            int index = ListUsed.SelectedIndex;

            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                CommentAction.Text = Program.aePreference.GetComment(Program.aePreference.AvailableActions[index]);
                AuxActionRef action = Program.aePreference.GetAction(index);
                if (action != null)
                {
                    if (action.GetType() == typeof(AuxActionHorn))
                        EditHornProperties(action);
                    else if (action.GetType() == typeof(AuxControlStart))
                        EditControlStartProperties(action);
                }
            }
        }

    }
}
