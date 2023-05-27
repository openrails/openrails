// COPYRIGHT 2013 by the Open Rails project.
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
using System.Drawing;
using System.Windows.Forms;
using Orts.Simulation.RollingStocks;
using ORTS.Common;

namespace Orts.Viewer3D.Debugging
{
    public partial class SoundDebugForm : Form
    {
        Viewer Viewer;

        private Timer UITimer;
        private double lastUpdateTime = 0;

        private List<SoundSource> ActiveSoundSources;
        private List<SoundSource> InactiveSoundSources;
        private SoundSource selectedSoundSource;

        public SoundDebugForm(Viewer viewer)
        {
            InitializeComponent();

            // Windows 2000 and XP should use 8.25pt Tahoma, while Windows
            // Vista and later should use 9pt "Segoe UI". We'll use the
            // Message Box font to allow for user-customizations, though.
            Font = SystemFonts.MessageBoxFont;

            Viewer = viewer;

            ActiveSoundSources = new List<SoundSource>();
            InactiveSoundSources = new List<SoundSource>();

            //foreach (string eventName in Enum.GetNames(typeof(Event)))
            //    discreteTriggersList.Items.Add(eventName);
            //discreteTriggersList.SelectedIndex = 0;

            // initialise the timer used to handle user input
            UITimer = new Timer();
            UITimer.Interval = 100;
            UITimer.Tick += new System.EventHandler(UITimer_Tick);
            UITimer.Start();
        }

        void UITimer_Tick(object sender, EventArgs e)
        {
            Visible = Viewer.SoundDebugFormEnabled;
            if (!Visible || Program.Simulator.GameTime - lastUpdateTime < 0.1) return;
            lastUpdateTime = Program.Simulator.GameTime;

            UpdateContent();
        }

        /// <summary>
        /// Updates the form content. Warning: Creates garbage
        /// </summary>
        void UpdateContent()
        {
            var soundSources = Viewer.SoundProcess.GetSoundSources();
            activeSoundList.BeginUpdate();
            inactiveSoundList.BeginUpdate();

            for (int i = 0; i < activeSoundList.Nodes.Count; i++)
                activeSoundList.Nodes[i].Nodes.Clear();
            for (int i = 0; i < inactiveSoundList.Nodes.Count; i++)
                inactiveSoundList.Nodes[i].Nodes.Clear();

            foreach (var src in soundSources.Values)
                foreach (var ssb in src)
                {
                    if (ssb is SoundSource)
                    {
                        var ss = (SoundSource)ssb;
                        TreeNode node;

                        var nodeString = String.Format("{0}: {1} ", ss.Car != null ? ss.Car.UiD.ToString() : "-", ss.SMSFileName);
                        var nodeKey = nodeString + ss.GetHashCode().ToString();

                        if (ss.Active)
                        {
                            int index = activeSoundList.Nodes.IndexOfKey(nodeKey);
                            if (index == -1)
                            {
                                activeSoundList.Nodes.Add(nodeKey, nodeString);
                                index = activeSoundList.Nodes.IndexOfKey(nodeKey);
                            }
                            node = activeSoundList.Nodes[index];
                        }
                        else
                        {
                            int index = inactiveSoundList.Nodes.IndexOfKey(nodeKey);
                            if (index == -1)
                            {
                                inactiveSoundList.Nodes.Add(nodeKey, nodeString);
                                index = inactiveSoundList.Nodes.IndexOfKey(nodeKey);
                            }
                            node = inactiveSoundList.Nodes[index];
                        }
                        node.Tag = ss;

                        var activeSS = 0;
                        foreach (var soundStream in ss.SoundStreams)
                        {
                            var playingData = soundStream.ALSoundSource.GetPlayingData();
                            if (playingData[0] != "-1")
                                activeSS++;
                            var streamString = String.Format("{0} {1} (cue: {2}) {3}", playingData);
                            var streamKey = streamString + soundStream.GetHashCode().ToString();
                            node.Nodes.Add(streamKey, streamString);
                            node.Nodes[streamKey].Tag = soundStream;
                        }
                        node.Text = string.Format("{0}({1}{2}", node.Text.Split('(')[0], activeSS, @"@");
                    }
                    else
                    {
                    }
                }

            CleanUp(activeSoundList.Nodes);
            CleanUp(inactiveSoundList.Nodes);

            // Fill selected node's data
            var selectedNode = activeSoundList.SelectedNode;
            if (selectedNode == null)
                selectedNode = inactiveSoundList.SelectedNode;

            if (selectedNode != null && selectedNode.Tag is SoundSource)
                selectedSoundSource = (SoundSource)selectedNode.Tag;
            else
                selectedSoundSource = null;

            if (selectedSoundSource != null)
            {
                int soundSourceID = -1;
                int i = -1;
                if (selectedSoundSource.SoundStreams.Count > 0)
                    while (++i < selectedSoundSource.SoundStreams.Count)
                    {
                        soundSourceID = selectedSoundSource.SoundStreams[i].ALSoundSource.SoundSourceID;
                        if (soundSourceID != -1)
                            break;
                    }

                if (selectedSoundSource.WorldLocation != WorldLocation.None && selectedSoundSource.SoundStreams.Count > 0)
                {
                    //Source distance:
                    //distance.Text = Math.Sqrt(selectedSoundSource.DistanceSquared).ToString("F1");

                    //Stream distance:
                    float[] pos = new float[3];
                    OpenAL.alGetSource3f(soundSourceID, OpenAL.AL_POSITION, out pos[0], out pos[1], out pos[2]);
                    float[] lpos = new float[3];
                    OpenAL.alGetListener3f(OpenAL.AL_POSITION, out lpos[0], out lpos[1], out lpos[2]);
                    for (var j = 0; j < 3; j++)
                        pos[j] -= lpos[j];
                    distance.Text = Math.Sqrt(pos[0] * pos[0] + pos[1] * pos[1] + pos[2] * pos[2]).ToString("F1");
                }
                else
                {
                    distance.Text = "-";
                }

                int relative;
                OpenAL.alGetSourcei(soundSourceID, OpenAL.AL_SOURCE_RELATIVE, out relative);
                sound3D.Checked = relative == OpenAL.AL_FALSE;

                if (selectedSoundSource.Car != null)
                {
                    speed.Text = Math.Abs(selectedSoundSource.Car.SpeedMpS).ToString("F1");
                    var Variable1 = selectedSoundSource.Car.Variable1;
                    var Variable2 = selectedSoundSource.Car.Variable2;
                    var Variable3 = selectedSoundSource.Car.Variable3;

                    if (selectedSoundSource.Car is MSTSSteamLocomotive)
                    {
                        Variable1 /= 100f;
                        Variable2 /= 100f;
                        Variable3 /= 100f;
                    }
                    if (selectedSoundSource.Car is MSTSElectricLocomotive)
                    {
                        Variable1 /= 100f;
                        Variable2 /= 100f;
                    }

                    variable1.Text = Variable1.ToString("0.#%");
                    variable2.Text = Variable2.ToString("0.#%");
                    variable3.Text = Variable3.ToString("0.#%");
                }
                else
                {
                    speed.Text = "0";
                    variable1.Text = "-";
                    variable2.Text = "-";
                    variable3.Text = "-";
                }

                float gain;
                OpenAL.alGetSourcef(soundSourceID, OpenAL.AL_GAIN, out gain);
                smsVolume.Text = gain.ToString("0.#%");
            }

            activeSoundList.EndUpdate();
            inactiveSoundList.EndUpdate();

            waves.Text = SoundItem.AllPieces.Count.ToString();
            alSources.Text = ALSoundSource.ActiveCount.ToString();
        }

        private void CleanUp(TreeNodeCollection nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Nodes.Count == 0)
                    nodes[i].Remove();
                else
                    nodes[i].Text = String.Format("{0}{1})", nodes[i].Text.Split('/')[0], nodes[i].Nodes.Count);
            }
        }

        private void SoundDebugForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Viewer.SoundDebugFormEnabled = false;
        }

        private void activeSoundList_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }

        private void activeSoundList_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        private void inactiveSoundList_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }

        private void inactiveSoundList_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }
    }
}
