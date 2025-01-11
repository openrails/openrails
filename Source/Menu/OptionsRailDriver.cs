using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ORTS.Common;
using ORTS.Common.Input;
using ORTS.Settings;

namespace Menu
{
    public partial class OptionsForm : Form
    {
        private RailDriverDevice instance;
        private Form railDriverLegend;
        private RailDriverCalibrationSetting currentCalibrationStep = RailDriverCalibrationSetting.CutOffDelta;

        private byte[] calibrationSettings = new byte[Enum.GetNames(typeof(UserCommand)).Length];
        private bool isCalibrationSet;

        private static int[,] startingPoints = { 
            { 170, 110 }, { 170, 150 }, { 170, 60 }, //Reverser
            { 230, 120 },  { 230, 150 }, { 230, 60 }, { 230, 90 }, //Throttle
            { 340, 150 }, { 340, 90 }, { 340, 60 }, // Auto Brake
            { 440, 150 }, { 440, 60 }, { 440, 150 }, { 470, 150 }, { 440, 60 }, { 470, 60 }, // Independent Brake
            { 520, 80 }, { 540, 80 }, { 560, 80 }, // Rotary Switch 1
            { 520, 150 }, { 535, 150 }, { 550, 150 }, // Rotary Switch 2
        };
        
        private Form GetRailDriverLegend()
        {
            const int WM_NCLBUTTONDOWN = 0xA1;
            const int HT_CAPTION = 0x2;

            if (null == railDriverLegend)
            {
                Size clientSize = new Size(Properties.Resources.RailDriverLegend.Width, Properties.Resources.RailDriverLegend.Height);
                PictureBox legend = new PictureBox() { Image = Properties.Resources.RailDriverLegend, Size = clientSize };
                legend.MouseDown += (object sender, MouseEventArgs e) =>
                {
                    (sender as Control).Capture = false;
                    Message msg = Message.Create(railDriverLegend.Handle, WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero);
                    base.WndProc(ref msg);
                };

                railDriverLegend = new Form()
                {
                    ShowIcon = false,
                    ShowInTaskbar = false,
                    ControlBox = false,
                    Text = string.Empty,
                    FormBorderStyle = FormBorderStyle.FixedSingle,
                    ClientSize = clientSize
                };
                railDriverLegend.Controls.Add(legend);
                railDriverLegend.FormClosed += (object sender, FormClosedEventArgs e) =>
                {
                    railDriverLegend = null;
                };
                railDriverLegend.KeyDown += (object sender, KeyEventArgs e) =>
                {
                    if (e.KeyValue == 0x1b)
                        railDriverLegend.Close();
                };
                legend.Paint += (object sender, PaintEventArgs e) =>
                {
                    Pen penLine = new Pen(Color.Red, 4f);
                    Pen penArrow = new Pen(Color.Red, 4f) { EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor };

                    if ((int)currentCalibrationStep < (startingPoints.Length / 2))
                    {
                        e.Graphics.DrawRectangle(penLine, startingPoints[(int)currentCalibrationStep, 0], startingPoints[(int)currentCalibrationStep, 1], 60, 40);
                        e.Graphics.DrawLine(penArrow, 10, 10, startingPoints[(int)currentCalibrationStep, 0] - 5, startingPoints[(int)currentCalibrationStep, 1] + 20);
                        e.Graphics.DrawString(catalog.GetString(GetStringAttribute.GetPrettyName(currentCalibrationStep)), new Font("Arial", 14), new SolidBrush(Color.Red), 80, 225);
                    }
                };
            }
            else
            {
                railDriverLegend.Hide();
            }
            return railDriverLegend;
        }

        private Panel InitializeRailDriverInputControls()
        {
            Panel panel = new Panel() { AutoScroll = true, Width = panelRDSettings.Width / 2 };
            panel.SuspendLayout();

            var columnWidth = (panel.ClientSize.Width - 20) / 3;

            Label tempLabel = new Label();
            RDButtonInputControl tempControl = new RDButtonInputControl(Settings.RailDriver.UserCommands[(int)UserCommand.GameQuit], RailDriverSettings.GetDefaultValue(UserCommand.GameQuit), instance);
            int rowTop = Math.Max(tempLabel.Margin.Top, tempControl.Margin.Top);
            int rowHeight = tempControl.Height;
            int rowSpacing = rowHeight + tempControl.Margin.Vertical;
            Size categorySize = new Size(columnWidth * 2 - tempLabel.Margin.Horizontal, rowHeight);
            Size inputControlSize = new Size(columnWidth - tempControl.Margin.Horizontal, rowHeight);
            Size labelSize = new Size(columnWidth * 2 - tempLabel.Margin.Horizontal, rowHeight);

            string previousCategory = "";
            var i = 0;

            foreach (UserCommand command in Enum.GetValues(typeof(UserCommand)))
            {
                string name = InputSettings.GetPrettyLocalizedName(command);
                string category, description;
                int index = name.IndexOf(' ');
                if (index == -1)
                {
                    category = string.Empty;
                    description = name;
                }
                else
                {
                    category = name.Substring(0, index);
                    description = name.Substring(index + 1);
                }

                if (category != previousCategory)
                {
                    Label categorylabel = new Label
                    {
                        Location = new Point(tempLabel.Margin.Left, rowTop + rowSpacing * i),
                        Size = categorySize,
                        Text = category,
                        TextAlign = ContentAlignment.MiddleLeft
                    };
                    categorylabel.Font = new Font(categorylabel.Font, FontStyle.Bold);
                    panel.Controls.Add(categorylabel);

                    previousCategory = category;
                    ++i;
                }

                var label = new Label
                {
                    Location = new Point(tempLabel.Margin.Left, rowTop + rowSpacing * i),
                    Size = labelSize,
                    Text = description,
                    TextAlign = ContentAlignment.MiddleRight
                };
                panel.Controls.Add(label);

                RDButtonInputControl rdButtonControl = new RDButtonInputControl(Settings.RailDriver.UserCommands[(int)command], RailDriverSettings.GetDefaultValue(command), instance)
                {
                    Location = new Point(columnWidth * 2 + tempControl.Margin.Left, rowTop + rowSpacing * i),
                    Size = inputControlSize,
                    Tag = command
                };
                panel.Controls.Add(rdButtonControl);

                ++i;
            }
            panel.ResumeLayout(true);
            return panel;
        }

        private void InitializeRailDriverSettings()
        {
                instance = RailDriverDevice.Instance;
            //#if !DEBUG
            if (!instance.Enabled)
                {
                    tabOptions.TabPages.Remove(tabPageRailDriver);
                    return;
                }
            //#endif
            panelRDButtons.Width = panelRDSettings.Width / 2;
            panelRDButtons.Controls.Clear();

            checkReverseReverser.Checked = Settings.RailDriver.CalibrationSettings[(int)RailDriverCalibrationSetting.ReverseReverser] != 0;
            checkReverseThrottle.Checked = Settings.RailDriver.CalibrationSettings[(int)RailDriverCalibrationSetting.ReverseThrottle] != 0;
            checkReverseAutoBrake.Checked = Settings.RailDriver.CalibrationSettings[(int)RailDriverCalibrationSetting.ReverseAutoBrake] != 0;
            checkReverseIndependentBrake.Checked = Settings.RailDriver.CalibrationSettings[(int)RailDriverCalibrationSetting.ReverseIndependentBrake] != 0;
            checkFullRangeThrottle.Checked = Settings.RailDriver.CalibrationSettings[(int)RailDriverCalibrationSetting.FullRangeThrottle] != 0;
            Panel controls = InitializeRailDriverInputControls();
            controls.Dock = DockStyle.Fill;
            panelRDButtons.Controls.Add(controls);
            foreach(Control control in controls.Controls)
                if (control is RDButtonInputControl)
                    toolTip1.SetToolTip(control, catalog.GetString("Click to change this button"));
        }

        private void RunCalibration()
        {
            byte[] readData = instance.GetReadBuffer();
            instance.SetLeds(RailDriverDisplaySign.Char_C, RailDriverDisplaySign.Char_A, RailDriverDisplaySign.Char_L);
            RailDriverCalibrationSetting nextStep = RailDriverCalibrationSetting.ReverserNeutral;
            DialogResult result = DialogResult.OK;
            while (result == DialogResult.OK && nextStep < RailDriverCalibrationSetting.ReverseReverser)
            {
                currentCalibrationStep = nextStep;
                railDriverLegend.Invalidate(true);  //enforce redraw legend to show guidance
                result = MessageBox.Show(railDriverLegend, $"Now calibrating \"{catalog.GetString(GetStringAttribute.GetPrettyName(currentCalibrationStep))}\". Move the Lever as indicated through guidance. \r\n\r\nClick OK to read the position and continue. Click Cancel anytime to abort the calibration process.", "RailDriver Calibration", MessageBoxButtons.OKCancel);
                // Read Setting
                if (result == DialogResult.OK)
                {
                    if (0 == instance.ReadCurrentData(ref readData))
                    {
                        int index = 0;
                        if ((int)currentCalibrationStep < 3)        //Reverser
                            index = 1;
                        else if ((int)currentCalibrationStep < 7)   //Throttle/Dynamic Brake
                            index = 2;  
                        else if ((int)currentCalibrationStep < 10)  //Auto Brake
                            index = 3;
                        else if ((int)currentCalibrationStep < 12)  //Independent Brake
                            index = 4;
                        else if ((int)currentCalibrationStep < 16)  //Independent Brake
                            index = 5;
                        else if ((int)currentCalibrationStep < 19)  //Rotary 1
                            index = 6;
                        else if ((int)currentCalibrationStep < 22)  //Rotary 2
                            index = 7;

                        calibrationSettings[(int)currentCalibrationStep] = readData[index];
                        instance.SetLedsNumeric(readData[index]);
                    }
                }
                nextStep++;
            }
            currentCalibrationStep = RailDriverCalibrationSetting.CutOffDelta;
            railDriverLegend.Invalidate(true);
            if (nextStep == RailDriverCalibrationSetting.ReverseReverser)
            {
                isCalibrationSet = (MessageBox.Show(railDriverLegend, "Calibration Completed. Do you want to keep the results?", "Calibration Done", MessageBoxButtons.YesNo) == DialogResult.Yes);
            }
            instance.SetLeds(RailDriverDisplaySign.Blank, RailDriverDisplaySign.Blank, RailDriverDisplaySign.Blank);
        }

        private void StartRDCalibration_Click(object sender, EventArgs e)
        {
            GetRailDriverLegend().Show(this);
            RunCalibration();
        }

        private void BtnRDReset_Click(object sender, EventArgs e)
        {
            if (DialogResult.Yes == MessageBox.Show(catalog.GetString("Remove all custom button assignments?"), Application.ProductName, MessageBoxButtons.YesNo))
            {
                Settings.RailDriver.Reset();
                InitializeRailDriverSettings();
            }
        }

        private void BtnShowRDLegend_Click(object sender, EventArgs e)
        {
            GetRailDriverLegend().Show(this);
        }

        private void BtnCheck_Click(object sender, EventArgs e)
        {
            string result = CheckButtonAssignments();
            if (!string.IsNullOrEmpty(result))
                MessageBox.Show(result, Application.ProductName);
            else
                MessageBox.Show(catalog.GetString("No errors found."), Application.ProductName);
        }

        private void BtnRDSettingsExport_Click(object sender, EventArgs e)
        {
            string outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Open Rails RailDriver.txt");
            Settings.RailDriver.DumpToText(outputPath);
            MessageBox.Show(catalog.GetString("A listing of all Raildriver button assignments has been placed here:\n\n") + outputPath, Application.ProductName);
        }

        private string CheckButtonAssignments()
        {
           if (!instance.Enabled)
                return string.Empty;
            byte[] buttons = new byte[Enum.GetNames(typeof(UserCommand)).Length];
            foreach (Control control in panelRDButtons.Controls)
            {
                if (control is Panel)
                {
                    foreach (Control child in control.Controls)
                        if (child is RDButtonInputControl)
                            buttons[(int)child.Tag] = (child as RDButtonInputControl).UserButton;
                    break;
                }
            }
            return Settings.RailDriver.CheckForErrors(buttons);
        }

        private void SaveRailDriverSettings()
        {
            foreach (Control control in panelRDButtons.Controls)
            {
                if (control is Panel)
                {
                    foreach (Control child in control.Controls)
                        if (child is RDButtonInputControl)
                            Settings.RailDriver.UserCommands[(int)child.Tag] = (child as RDButtonInputControl).UserButton;
                    break;
                }
            }

            if (isCalibrationSet)
            {
                currentCalibrationStep = RailDriverCalibrationSetting.ReverserNeutral;
                while (currentCalibrationStep < RailDriverCalibrationSetting.ReverseReverser)
                {

                    Settings.RailDriver.CalibrationSettings[(int)currentCalibrationStep] = calibrationSettings[(int)currentCalibrationStep];
                    currentCalibrationStep++;
                }
            }

            Settings.RailDriver.CalibrationSettings[(int)RailDriverCalibrationSetting.ReverseReverser] = Convert.ToByte(checkReverseReverser.Checked);
            Settings.RailDriver.CalibrationSettings[(int)RailDriverCalibrationSetting.ReverseThrottle] = Convert.ToByte(checkReverseThrottle.Checked);
            Settings.RailDriver.CalibrationSettings[(int)RailDriverCalibrationSetting.ReverseAutoBrake] = Convert.ToByte(checkReverseAutoBrake.Checked);
            Settings.RailDriver.CalibrationSettings[(int)RailDriverCalibrationSetting.ReverseIndependentBrake] = Convert.ToByte(checkReverseIndependentBrake.Checked);
            Settings.RailDriver.CalibrationSettings[(int)RailDriverCalibrationSetting.FullRangeThrottle] = Convert.ToByte(checkFullRangeThrottle.Checked);

            currentCalibrationStep = RailDriverCalibrationSetting.CutOffDelta;
        }

    }
}
