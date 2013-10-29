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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ORTS.Debugging
{
    public partial class SoundDebugForm : Form
    {
        private readonly Simulator Simulator;
        Viewer3D Viewer;

        private Timer UITimer;
        private double lastUpdateTime = 0;

        private List<SoundSource> ActiveSoundSources;
        private List<SoundSource> InactiveSoundSources;
        private Dictionary<object, List<SoundSourceBase>> SoundSources;
        private SoundSource selectedSoundSource;

        public SoundDebugForm(Simulator simulator, Viewer3D viewer)
        {
            InitializeComponent();

            if (simulator == null)
            {
                throw new ArgumentNullException("simulator", "Simulator object cannot be null.");
            }
            Simulator = simulator;
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

        void UpdateContent()
        {
            Viewer.SoundProcess.GetSoundSources(ref SoundSources);
            activeSoundList.BeginUpdate();
            inactiveSoundList.BeginUpdate();

            for (int i = 0; i < activeSoundList.Nodes.Count; i++)
                activeSoundList.Nodes[i].Nodes.Clear();
            for (int i = 0; i < inactiveSoundList.Nodes.Count; i++)
                inactiveSoundList.Nodes[i].Nodes.Clear();

            foreach (List<SoundSourceBase> src in SoundSources.Values)
                foreach (SoundSourceBase ssb in src)
                {
                    if (ssb is SoundSource)
                    {
                        SoundSource ss = (SoundSource)ssb;
                        TreeNode node = null;

                        string nodeString;
                        if (ss.Car != null)
                            nodeString = ss.Car.UiD.ToString();
                        else
                            nodeString = "-";
                        nodeString += ": " + ss.SMSFileName + " ";
                        string nodeKey = nodeString + ss.GetHashCode().ToString();

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

                        foreach (SoundStream soundStream in ss.SoundStreams)
                        {
                            string[] soundData = soundStream.ALSoundSource.GetPlayingData();
                            string streamString = soundData[0] + " " + soundData[1] + " (cue: " + soundData[2] + ")" + " " + soundData[3];
                            node.Nodes.Add(streamString, streamString);
                            int index = node.Nodes.IndexOfKey(streamString);
                        }
                    }
                    else
                    {
                    }
                }

            // Clean up
            for (int i = 0; i < activeSoundList.Nodes.Count; i++)
            {
                if (activeSoundList.Nodes[i].Nodes.Count == 0)
                    activeSoundList.Nodes[i].Remove();
                else
                    activeSoundList.Nodes[i].Text = activeSoundList.Nodes[i].Text.Split('(')[0] + "(" + activeSoundList.Nodes[i].Nodes.Count.ToString() + ")";
            }
            for (int i = 0; i < inactiveSoundList.Nodes.Count; i++)
            {
                if (inactiveSoundList.Nodes[i].Nodes.Count == 0)
                    inactiveSoundList.Nodes[i].Remove();
                else
                    inactiveSoundList.Nodes[i].Text = inactiveSoundList.Nodes[i].Text.Split('(')[0] + "(" + inactiveSoundList.Nodes[i].Nodes.Count.ToString() + ")";
            }

            // Fill selected node's data
            TreeNode selectedNode = activeSoundList.SelectedNode;
            if (selectedNode == null)
                selectedNode = inactiveSoundList.SelectedNode;

            if (selectedNode != null)
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
                        if (!selectedSoundSource.SoundStreams[i].ALSoundSource.GetPlayingData().Contains("Stopped"))
                            break;
                    }

                if (selectedSoundSource.WorldLocation != null && selectedSoundSource.SoundStreams.Count > 0)
                {
                    distance.Text = Math.Sqrt(selectedSoundSource.DistanceSquared).ToString("F1");
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
                        Variable2 /= 100f;

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

            cache.Text = SoundItem.AllPieces.Count.ToString();
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
