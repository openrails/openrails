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
using System.Drawing;
using System.Windows.Forms;
using Orts.Formats.Msts;
using Orts.Simulation.RollingStocks;
using ORTS.Common;

namespace Orts.Viewer3D.Debugging
{
    public partial class SoundDebugForm : Form
    {
        Viewer Viewer;

        private Timer UITimer;
        private double lastUpdateTime = 0;
        private int UpdateCounter = -1;

        private SoundSource selectedSoundSource;

        public SoundDebugForm(Viewer viewer)
        {
            InitializeComponent();

            // Windows 2000 and XP should use 8.25pt Tahoma, while Windows
            // Vista and later should use 9pt "Segoe UI". We'll use the
            // Message Box font to allow for user-customizations, though.
            Font = SystemFonts.MessageBoxFont;

            Viewer = viewer;

            //foreach (string eventName in Enum.GetNames(typeof(Event)))
            //    discreteTriggersList.Items.Add(eventName);
            //discreteTriggersList.SelectedIndex = 0;

            // initialise the timer used to handle user input
            UITimer = new Timer();
            UITimer.Interval = 100;
            UITimer.Tick += new EventHandler(UITimer_Tick);
            UITimer.Start();
        }

        void UITimer_Tick(object sender, EventArgs e)
        {
            Visible = Viewer.SoundDebugFormEnabled;
            if (!Visible)
            {
                // Reset the tree when closing the window
                if (activeSoundList.Nodes.Count > 0)
                    activeSoundList.Nodes.Clear();
                if (inactiveSoundList.Nodes.Count > 0)
                    inactiveSoundList.Nodes.Clear();
                UpdateCounter = -1;

                return;
            }
            if (Program.Viewer.RealTime - lastUpdateTime < 0.1)
                return;
            lastUpdateTime = Program.Viewer.RealTime;

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

            int sourceIndex = 0;

            foreach (var src in soundSources.Values)
            {
                foreach (var ssb in src)
                {
                    if (ssb is SoundSource ss)
                    {
                        // Lag reduction: Only update 1/10th of the sound streams each update,
                        // except on the first update, and except for the selected sound source
                        sourceIndex++;
                        if (UpdateCounter > -1 && (sourceIndex + UpdateCounter) % 10 != 0 && ss != selectedSoundSource)
                            continue;

                        TreeNode node;

                        string nodeString = String.Format("{0}: {1} ", ss.Car != null ? ss.Car.UiD.ToString() : "-", ss.SMSFileName);
                        string nodeKey = nodeString + ss.GetHashCode().ToString();

                        if (ss.SoundStreams.Count > 0)
                        {
                            if (ss.Active)
                            {
                                int index = activeSoundList.Nodes.IndexOfKey(nodeKey);
                                if (index == -1)
                                {
                                    node = activeSoundList.Nodes.Add(nodeKey, nodeString);
                                    node.Tag = ss;
                                }
                                else
                                    node = activeSoundList.Nodes[index];

                                inactiveSoundList.Nodes.RemoveByKey(nodeKey);
                            }
                            else
                            {
                                int index = inactiveSoundList.Nodes.IndexOfKey(nodeKey);
                                if (index == -1)
                                {
                                    node = inactiveSoundList.Nodes.Add(nodeKey, nodeString);
                                    node.Tag = ss;
                                }
                                else
                                    node = inactiveSoundList.Nodes[index];

                                activeSoundList.Nodes.RemoveByKey(nodeKey);
                            }

                            int activeSS = 0;
                            int streamIndex = 0;
                            foreach (var soundStream in ss.SoundStreams)
                            {
                                string[] playingData = soundStream.ALSoundSource.GetPlayingData();
                                if (playingData[0] != "-1")
                                    activeSS++;
                                if (node.IsExpanded || node.Nodes.Count < ss.SoundStreams.Count)
                                {
                                    // Only update sound stream nodes one time unless main node is expanded
                                    string streamString = String.Format("{0} {1} (cue: {2}) {3}", playingData);
                                    string streamKey = streamIndex.ToString() + soundStream.GetHashCode().ToString();

                                    TreeNode streamNode;

                                    int streamNodeIndex = node.Nodes.IndexOfKey(streamKey);
                                    if (streamNodeIndex == -1)
                                    {
                                        streamNode = node.Nodes.Add(streamKey, streamString);
                                        streamNode.Tag = soundStream;
                                    }
                                    else
                                    {
                                        streamNode = node.Nodes[streamNodeIndex];
                                        streamNode.Text = streamString;
                                    }
                                }
                                streamIndex++;
                            }
                            node.Text = string.Format("{0}({1}{2}{3})", node.Text.Split('(')[0], activeSS, @"@", ss.SoundStreams.Count);
                        }
                        else
                        {
                            // Don't list sound sources with no sound streams
                            activeSoundList.Nodes.RemoveByKey(nodeKey);
                            inactiveSoundList.Nodes.RemoveByKey(nodeKey);
                        }
                    }
                }
            }

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
                    double squareDistance = pos[0] * pos[0] + pos[1] * pos[1] + pos[2] * pos[2];

                    distance.Text = Math.Sqrt(squareDistance).ToString("0.0");
                    distanceSquared.Text = squareDistance.ToString("0");
                }
                else
                {
                    distance.Text = "-";
                    distanceSquared.Text = "-";
                }

                OpenAL.alGetSourcei(soundSourceID, OpenAL.AL_SOURCE_RELATIVE, out int relative);
                sound3D.Checked = relative == OpenAL.AL_FALSE;

                if (selectedSoundSource.Car != null)
                {
                    speed.Text = Math.Abs(selectedSoundSource.Car.SpeedMpS).ToString("0.0");
                    wheelRPM.Text = pS.TopM((float)(selectedSoundSource.Car.AbsSpeedMpS / (2 * Math.PI * selectedSoundSource.Car.WheelRadiusM))).ToString("0.0");

                    tractiveEffort.Text = (selectedSoundSource.Car.MotiveForceN / 1000.0f * Math.Sign(selectedSoundSource.Car.WheelSpeedMpS)).ToString("0.0"); // Convert to kN, ensure positive for traction, negative for dynamics
                    tractivePower.Text = (selectedSoundSource.Car.MotiveForceN * selectedSoundSource.Car.WheelSpeedMpS / 1000.0f).ToString("0"); // Convert to kW

                    float[] Variable1 = selectedSoundSource.Car.Variable1;
                    float Variable2 = selectedSoundSource.Car.Variable2;
                    float Variable2Booster = selectedSoundSource.Car.Variable2_Booster;
                    float Variable3 = selectedSoundSource.Car.Variable3;

                    if (selectedSoundSource.Car is MSTSSteamLocomotive)
                    {
                        for (int v1 = 0; v1 < Variable1.Length; v1++)
                            Variable1[v1] /= 100f;
                        Variable2 /= 100f;
                        Variable2Booster /= 100f;
                        Variable3 /= 100f;
                    }
                    if (selectedSoundSource.Car is MSTSElectricLocomotive)
                    {
                        for (int v1 = 0; v1 < Variable1.Length; v1++)
                            Variable1[v1] /= 100f;
                        Variable2 /= 100f;
                    }

                    string variable1Text = Variable1[0].ToString("0.000");
                    for (int v1 = 1; v1 < Variable1.Length; v1++)
                        variable1Text = variable1Text + ", " + Variable1[v1].ToString("0.000");
                    variable1.Text = variable1Text;
                    variable2.Text = Variable2.ToString("0.000");
                    variable2Booster.Text = Variable2.ToString("0.000");
                    variable3.Text = Variable3.ToString("0.000");

                    string engineRPMText = selectedSoundSource.Car.EnginesRPM[0].ToString("0.0");
                    for (int r = 1; r < selectedSoundSource.Car.EnginesRPM.Length; r++)
                        engineRPMText = engineRPMText + ", " + selectedSoundSource.Car.EnginesRPM[r].ToString("0.0");
                    engineRPM.Text = engineRPMText;
                    string enginePowerText = selectedSoundSource.Car.EnginesPower[0].ToString("0");
                    for (int p = 1; p < selectedSoundSource.Car.EnginesPower.Length; p++)
                        enginePowerText = enginePowerText + ", " + selectedSoundSource.Car.EnginesPower[p].ToString("0");
                    enginePower.Text = enginePowerText;
                    string engineTorqueText = selectedSoundSource.Car.EnginesTorque[0].ToString("0");
                    for (int t = 1; t < selectedSoundSource.Car.EnginesTorque.Length; t++)
                        engineTorqueText = engineTorqueText + ", " + selectedSoundSource.Car.EnginesTorque[t].ToString("0");
                    engineTorque.Text = engineTorqueText;

                    backPressure.Text = selectedSoundSource.Car.BackPressurePSIG.ToString("0.0");

                    brakeCyl.Text = selectedSoundSource.Car.BrakeSystem.GetCylPressurePSI().ToString("0.0");
                    carFriction.Text = selectedSoundSource.Car.Train.WagonCoefficientFriction.ToString("0.000");

                    curveForce.Text = selectedSoundSource.Car.CurveForceNFiltered.ToString("0");
                    angleOfAttack.Text = selectedSoundSource.Car.CurveSquealAoAmRadFiltered.ToString("0.0");

                    carTunnelDistance.Text = selectedSoundSource.Car.CarTunnelDistanceM.ToString("0.0");

                    concreteSleepers.Checked = SharedSMSFileManager.ConcreteSleepers == 1.0f;
                    carInTunnel.Checked = selectedSoundSource.Car.TrackSoundInTunnelTriggered == 1.0f;
                }
                else
                {
                    speed.Text = "0";
                    wheelRPM.Text = "-";
                    tractiveEffort.Text = "-";
                    tractivePower.Text = "-";

                    variable1.Text = "-";
                    variable2.Text = "-";
                    variable2Booster.Text = "-";
                    variable3.Text = "-";

                    backPressure.Text = "-";

                    engineRPM.Text = "-";
                    enginePower.Text = "-";
                    engineTorque.Text = "-";

                    brakeCyl.Text = "-";
                    carFriction.Text = "-";

                    curveForce.Text = "-";
                    angleOfAttack.Text = "-";

                    distanceSquared.Text = "-";
                    carTunnelDistance.Text = "-";

                    concreteSleepers.Checked = false;
                    carInTunnel.Checked = false;
                }

                OpenAL.alGetSourcef(soundSourceID, OpenAL.AL_GAIN, out float gain);
                smsVolume.Text = gain.ToString("0.#%");
            }

            waves.Text = SoundItem.AllPieces.Count.ToString();
            alSources.Text = ALSoundSource.ActiveCount.ToString();

            activeSoundList.EndUpdate();
            inactiveSoundList.EndUpdate();

            UpdateCounter = (UpdateCounter + 1) % 10;
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
