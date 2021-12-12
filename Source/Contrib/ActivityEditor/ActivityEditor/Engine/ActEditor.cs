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

using ActivityEditor.Engine;
using AEWizard;
using LibAE.Formats;
using Orts.Formats.OR;
using ORTS.Common;
/// This module ...
/// 
/// Author: Stéfan Paitoni
/// Updates : 
/// 

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ActivityEditor
{
    public enum ToolClicks // Changed from ToolClicked as led to compiler warnings
    {
        NO_TOOL = 0,
        TAG = 1,
        STATION = 2,
        MOVE = 3,
        ROTATE = 4,
        START = 5,
        STOP = 6,
        WAIT = 7,
        ACTION = 8,
        CHECK = 9,
        AREA = 10,
        AREA_ADD = 11,
        DRAG = 12,
        ZOOM = 13,
        METASEGMENT = 14
    };

    public partial class ActEditor : Form
    {
        private bool saveEnabled = false;
        private bool saveAsEnabled = false;
        private bool loadEnabled = false;
        public List<Viewer2D> viewer2ds;
        public Viewer2D selectedViewer;
        
        private bool focusOnViewer = false;

        // Commented out as never used
        //public List<AEActivity> aeActivity;
        //public AEActivity selectedActivity;
        //private ToolClicks ToolClicked = ToolClicks.NO_TOOL;
        // private WorldPosition worldPos;

        public Image ToolToUse = null;
        public Cursor CursorToUse = Cursors.Default;
        public ActEditor()
        {
            InitializeComponent();
            SelectTools(TypeEditor.NONE);
            viewer2ds = new List<Viewer2D>();
            selectedViewer = null;
            CheckCurrentStatus();
            if (selectedViewer != null)
            {
                using (MemoryStream ms = new MemoryStream(Properties.Resources.point))
                {
                    selectedViewer.reduit = new Cursor(ms);
                }
            }
        }

        private void StatusEditor_Click(object sender, EventArgs e)
        {
            Form activeChild = this.ActiveMdiChild;
        }

        public void DisplayStatusMessage(string info)
        {
            this.StatusEditor.Text = info;
            this.Refresh();
        }

        private void CheckCurrentStatus()
        {
            if (!Program.aePreference.CheckPrefValidity())
            {
                DisplayStatusMessage("Please, Configure your Path!");
                return;
            }
            Vfs.Initialize(Program.aePreference.MSTSPath, Path.GetDirectoryName(Application.ExecutablePath));

            DisplayStatusMessage("Create New Activity");
        }
        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            updateNewMenuItems();
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Really Quit?", "Exit", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                Application.Exit();
            }
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenActivity.InitialDirectory = Program.aePreference.AEPath;
            if (OpenActivity.ShowDialog() == DialogResult.OK)
            {
                ActivityInfo activityInfo = ActivityInfo.loadActivity(OpenActivity.FileName);
                if (activityInfo == null)
                    return;
                this.Cursor = Cursors.AppStarting;
                this.Refresh();

                Viewer2D newViewer = new Viewer2D(this, activityInfo);
                viewer2ds.Add(newViewer);
                focusOnViewer = true;
                setFocus(newViewer);
                this.Cursor = Cursors.Default;
                this.Refresh();

                DisplayStatusMessage("Load Succeed");
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.selectedViewer.Save();
            //this.selectedActivity.activityInfo.saveXml();
            DisplayStatusMessage("Save Activity Info Status");
        }

        private void activityToolStripMenuItem_Click(object sender, EventArgs e)
        {   //  Try to create a new activity Object with wizard
            ActivityInfo activityInfo = new ActivityInfo();
            activityInfo.config(Program.aePreference.RoutePaths);
            WizardForm wiz = new WizardForm(activityInfo);
            if (wiz.ShowDialog() == DialogResult.OK)
            {
                Viewer2D newViewer = new Viewer2D(this, activityInfo);
                viewer2ds.Add(newViewer);
                focusOnViewer = true;
                setFocus(newViewer);
                this.Cursor = Cursors.AppStarting;
                this.Refresh();

                this.Cursor = Cursors.Default;
                this.Refresh();
                DisplayStatusMessage("Load Succeed");
            }
        }

        private void trafficToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //  For Generic Traffic definition
        }

        private void aboutActivityEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutActEdit aae = new AboutActEdit();
            aae.ShowDialog();
        }

        private void preferenceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Preference.Options optionWindow = new Preference.Options();
            optionWindow.ShowDialog();
        }
        private void updateNewMenuItems()
        {
            if (Program.aePreference.classFilled == true)
            {
                this.activityToolStripMenuItem.Enabled = true;
                this.trafficToolStripMenuItem.Enabled = true;
                this.loadMetada.Enabled = true;

            }
            else
            {
                this.activityToolStripMenuItem.Enabled = false;
                this.trafficToolStripMenuItem.Enabled = false;
                this.loadMetada.Enabled = false;
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (selectedViewer == null)
            {
                DisplayStatusMessage("No Activity Selected");
                return;
            }
            DisplayStatusMessage("Type In Activity Descr");
            SimpleTextEd simpleTextEd = new SimpleTextEd();
            simpleTextEd.aEditer.Text = selectedViewer.getDescr();
            simpleTextEd.ShowDialog();
            if (simpleTextEd.aEditer.TextLength > 0)
            {
                selectedViewer.setDescr(simpleTextEd.aEditer.Text);
                DisplayStatusMessage("Act Description Updated");
            }
        }

        private void ActivityAECB_TextChanged(object sender, EventArgs e)
        {
#if WITH_DEBUG
            File.AppendAllText(@"C:\temp\AE.txt",
                "TagName_TextChanged: " + sender.ToString() + "\n");
#endif
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (selectedViewer == null)
                return;
            /* if (selectedActivity.GetTagVisibility())
            {
                //selectedActivity.TagPanel.Visible = false;
                //selectedActivity.SetTagVisibility(false);
            }
            else
            {
                //selectedActivity.TagPanel.Visible = true;
                //selectedActivity.SetTagVisibility(true);
            } */
        }

        private void AddTag_Click(object sender, EventArgs e)
        {
            DisplayStatusMessage("Please, Place Tag");
            selectedViewer.SetToolClicked(ToolClicks.TAG);
        }

        private void AddStation_Click(object sender, EventArgs e)
        {
            DisplayStatusMessage("Please, Place Station");
            selectedViewer.SetToolClicked(ToolClicks.STATION);
        }

        #region Activity    
        //  Not used actuially
        private void AddActivityStart_Click(object sender, EventArgs e)
        {
            DisplayStatusMessage("Please, Place Start Activity");
            //ToolClicked = ToolClicks.START;
            ToolToUse = global::ActivityEditor.Properties.Resources.Activity_start;

        }

        private void AddActivityStop_Click(object sender, EventArgs e)
        {
            DisplayStatusMessage("Please, Place Stop Activity");
            //ToolClicked = ToolClicks.STOP;
            ToolToUse = global::ActivityEditor.Properties.Resources.Activity_stop;

        }

        private void AddActivityWait_Click(object sender, EventArgs e)
        {
            DisplayStatusMessage("Please, Place Wait Point Activity");
            //ToolClicked = ToolClicks.WAIT;
            ToolToUse = global::ActivityEditor.Properties.Resources.Activity_wait;

        }

        private void AddActivityAction_Click(object sender, EventArgs e)
        {
            DisplayStatusMessage("Please, Place Action Point Activity");
            //ToolClicked = ToolClicks.ACTION;
            ToolToUse = global::ActivityEditor.Properties.Resources.Action;

        }

        private void AddActivityEval_Click(object sender, EventArgs e)
        {
            DisplayStatusMessage("Please, Place Evaluation Point Activity");
            //ToolClicked = ToolClicks.CHECK;
            ToolToUse = global::ActivityEditor.Properties.Resources.Activity_check;

        }
        #endregion
        
        private void MoveSelected_Click(object sender, EventArgs e)
        {
            DisplayStatusMessage("Please, Place Move Tool");
            selectedViewer.SetToolClicked(ToolClicks.MOVE);
        }

        private void RotateSelected_Click(object sender, EventArgs e)
        {
            DisplayStatusMessage("Place Rotate Tool");
            selectedViewer.SetToolClicked(ToolClicks.ROTATE);
        }

        public void UnsetToolClick()
        {
            DisplayStatusMessage("Wait Action Form");
            //ToolClicked = ToolClicks.NO_TOOL;
            ToolToUse = null;
            this.Cursor = Cursors.Default;
            CursorToUse = Cursors.Default;
        }

        private void AddArea_Click(object sender, EventArgs e)
        {
            DisplayStatusMessage("Add Station Area");
            selectedViewer.SetToolClicked(ToolClicks.AREA);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.selectedViewer.Save();
        }

        public void setFocus(Viewer2D viewer)
        {
            if (viewer == null || !focusOnViewer)
                return;
            if (selectedViewer != viewer)
            {
                this.SuspendLayout();
                if (selectedViewer != null)
                {
                    selectedViewer.UnsetFocus();
                }
                viewer.SetFocus();
                selectedViewer = viewer;
                if (viewer.ViewerMode == TypeEditor.ACTIVITY)
                {
                    activityOverview.Visible = true;
                    ActivityAECB.Text = viewer.aeConfig.aeActivity.activityInfo.ActivityName;
                }
                this.loadToolStripMenuItem.Enabled = this.loadEnabled;
                this.saveAsToolStripMenuItem.Enabled = this.saveAsEnabled;
                this.saveToolStripMenuItem.Enabled = this.saveEnabled;
                viewer.Show();
                viewer.Focus();
                viewer.Select();
                this.ResumeLayout(true);
                this.PerformLayout();
            }
        }

        private void routeData_Enter(object sender, EventArgs e)
        {
            focusOnViewer = false;
            selectedViewer = null;
            this.Focus();
        }

        public bool askFocus()
        {
            focusOnViewer = true;
            return focusOnViewer;
        }
        private void UpdateRouteCfg(object sender, EventArgs e)
        {
            RouteInfo routeInfo = new RouteInfo();
            routeInfo.config(Program.aePreference.RoutePaths);
            WizardForm wiz = new WizardForm(routeInfo);
            if (wiz.ShowDialog() == DialogResult.OK)
            {
                this.Cursor = Cursors.AppStarting;
                this.Refresh();

                Viewer2D newViewer = new Viewer2D(this, routeInfo.route);
                viewer2ds.Add(newViewer);
                focusOnViewer = true;
                setFocus(newViewer);

                this.Cursor = Cursors.Default;
                this.Refresh();
                DisplayStatusMessage("Load Succeed");
            }
        }

        static int cntAct = 0;
        static int cntRoute = 0;
        public void SelectTools(TypeEditor viewerMode)
        {
            if (viewerMode == TypeEditor.ACTIVITY)
            {
                cntAct++;
            }
            else if (viewerMode == TypeEditor.ROUTECONFIG)
            {
                cntRoute++;
            }
            else if (viewerMode == TypeEditor.TRAFFIC)
            {
            }
            else
            {
            }
            if (cntAct > 0)
            {
                this.activityCFG.Visible = true;
            }
            else
            {
                this.activityCFG.Visible = false;
            }
            if (cntRoute > 0)
            {
                this.routeCFG.Visible = true;
            }
            else
            {
                this.routeCFG.Visible = false;
            }
            if (cntRoute == 0 && cntAct == 0)
            {
                this.toolStrip3.Visible = false;
            }
            else
            {
                this.toolStrip3.Visible = true;
            }
        }

        public void UnselectTools(TypeEditor viewerMode)
        {
            if (viewerMode == TypeEditor.ACTIVITY)
            {
                cntAct--;
            }
            else if (viewerMode == TypeEditor.ROUTECONFIG)
            {
                cntRoute--;
            }
            else if (viewerMode == TypeEditor.TRAFFIC)
            {
            }
            else
            {
            }
            if (cntAct > 0)
                this.activityCFG.Visible = true;
            else
            {
                cntAct = 0;
                this.activityCFG.Visible = false;
            }
            if (cntRoute > 0)
                this.routeCFG.Visible = true;
            else
            {
                cntRoute = 0;
                this.routeCFG.Visible = false;
            }
            if (cntRoute == 0 && cntAct == 0)
            {
                this.toolStrip3.Visible = false;
            }
            else
            {
                this.toolStrip3.Visible = true;
            }
        }

        private void ActEditorKeyDown(object sender, KeyEventArgs e)
        {
            if (selectedViewer != null)
            {
                selectedViewer.Viewer2D_KeyDown(sender, e);
            }
        }

        private void editMetaSegment(object sender, EventArgs e)
        {
            DisplayStatusMessage("Edit Metadata for Segment");
            selectedViewer.SetToolClicked(ToolClicks.METASEGMENT);
        }

        private void routeData_Leave(object sender, EventArgs e)
        {
            DisplayStatusMessage("lose");
        }

        private void SaveRouteCfg(object sender, EventArgs e)
        {

        }
#if zorro
        public void CloseActivity(AEActivity activity)
        {
            this.SuspendLayout();
            AEActivity toClose;
            //unsetFocus(activity);
            int item = aeActivity.FindIndex(0, place => place.activityInfo.ActivityName == activity.activityInfo.ActivityName);
            toClose = aeActivity[item];
            aeActivity.RemoveAt(item);
            if (aeActivity.Count > 0)
            {
                selectedActivity = aeActivity[0];
                setFocus(aeActivity[0]);
                //selectedActivity.TagPanel.Visible = true;
                //selectedActivity.StationPanel.Visible = true;
                selectedActivity.ActivityPanel.Visible = true;
                selectedActivity.SetTagVisibility(true);
            }
            else
            {
                activity.ActivityPanel.Visible = false;
                activity.ActivityPanel.Refresh();
                this.ActivityAECB.ResetText();
                selectedActivity = null;
                DisplayStatusMessage(Program.intlMngr.GetString("NoMoreAct"));
            }
            this.ResumeLayout(true);
        }

        public void unsetFocus(AEActivity activity)
        {
            this.loadToolStripMenuItem.Enabled = false;
            this.saveAsToolStripMenuItem.Enabled = false;
            this.saveToolStripMenuItem.Enabled = false;
        }

#endif
    }
}
