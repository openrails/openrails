// #define INCLUDE_TIMETABLE_INPUT

namespace ORTS
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.checkBoxWindowed = new System.Windows.Forms.CheckBox();
            this.buttonStart = new System.Windows.Forms.Button();
            this.labelLogo1 = new System.Windows.Forms.Label();
            this.labelLogo2 = new System.Windows.Forms.Label();
            this.buttonFolderAdd = new System.Windows.Forms.Button();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.buttonFolderRemove = new System.Windows.Forms.Button();
            this.checkBoxWarnings = new System.Windows.Forms.CheckBox();
            this.buttonOptions = new System.Windows.Forms.Button();
            this.buttonResume = new System.Windows.Forms.Button();
            this.buttonTesting = new System.Windows.Forms.Button();
            this.comboBoxFolder = new System.Windows.Forms.ComboBox();
            this.comboBoxRoute = new System.Windows.Forms.ComboBox();
            this.comboBoxActivity = new System.Windows.Forms.ComboBox();
            this.comboBoxLocomotive = new System.Windows.Forms.ComboBox();
            this.comboBoxConsist = new System.Windows.Forms.ComboBox();
            this.comboBoxStartAt = new System.Windows.Forms.ComboBox();
            this.comboBoxHeadTo = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.textBoxMPHost = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.buttonMPClient = new System.Windows.Forms.Button();
            this.buttonMPServer = new System.Windows.Forms.Button();
            this.comboBoxDuration = new System.Windows.Forms.ComboBox();
            this.comboBoxStartTime = new System.Windows.Forms.ComboBox();
            this.comboBoxStartSeason = new System.Windows.Forms.ComboBox();
            this.comboBoxDifficulty = new System.Windows.Forms.ComboBox();
            this.comboBoxStartWeather = new System.Windows.Forms.ComboBox();
            this.label12 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.textBoxMPUser = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.panelDetails = new System.Windows.Forms.Panel();
            this.pictureBoxLogo = new System.Windows.Forms.PictureBox();
            this.buttonFolderEdit = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.ActivityPage = new System.Windows.Forms.TabPage();
            this.TimetablePage = new System.Windows.Forms.TabPage();
            this.label24 = new System.Windows.Forms.Label();
            this.comboBoxPlayerTrain = new System.Windows.Forms.ComboBox();
            this.label23 = new System.Windows.Forms.Label();
            this.comboBoxTimetableDay = new System.Windows.Forms.ComboBox();
            this.label22 = new System.Windows.Forms.Label();
            this.comboBoxTimetableWeather = new System.Windows.Forms.ComboBox();
            this.label20 = new System.Windows.Forms.Label();
            this.comboBoxTimetableSeason = new System.Windows.Forms.ComboBox();
            this.label21 = new System.Windows.Forms.Label();
            this.groupBoxAITrains = new System.Windows.Forms.GroupBox();
            this.radioButtonAITimeRelative = new System.Windows.Forms.RadioButton();
            this.radioButtonAITimeAbsolute = new System.Windows.Forms.RadioButton();
            this.checkBoxAISameDirection = new System.Windows.Forms.CheckBox();
            this.label19 = new System.Windows.Forms.Label();
            this.numericUpDownAIMins = new System.Windows.Forms.NumericUpDown();
            this.label17 = new System.Windows.Forms.Label();
            this.numericUpDownAIHours = new System.Windows.Forms.NumericUpDown();
            this.label18 = new System.Windows.Forms.Label();
            this.comboBoxPlayerTimetable = new System.Windows.Forms.ComboBox();
            this.label16 = new System.Windows.Forms.Label();
            this.comboBoxTimetable = new System.Windows.Forms.ComboBox();
            this.label15 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).BeginInit();
            this.panel1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.ActivityPage.SuspendLayout();
            this.TimetablePage.SuspendLayout();
            this.groupBoxAITrains.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownAIMins)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownAIHours)).BeginInit();
            this.SuspendLayout();
            // 
            // checkBoxWindowed
            // 
            this.checkBoxWindowed.AutoSize = true;
            this.checkBoxWindowed.Checked = true;
            this.checkBoxWindowed.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxWindowed.Location = new System.Drawing.Point(3, 71);
            this.checkBoxWindowed.Name = "checkBoxWindowed";
            this.checkBoxWindowed.Size = new System.Drawing.Size(77, 17);
            this.checkBoxWindowed.TabIndex = 28;
            this.checkBoxWindowed.Text = "Windowed";
            this.checkBoxWindowed.UseVisualStyleBackColor = true;
            // 
            // buttonStart
            // 
            this.buttonStart.Enabled = false;
            this.buttonStart.Location = new System.Drawing.Point(6, 19);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(75, 34);
            this.buttonStart.TabIndex = 0;
            this.buttonStart.Text = "Start";
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            // 
            // labelLogo1
            // 
            this.labelLogo1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelLogo1.AutoSize = true;
            this.labelLogo1.Font = new System.Drawing.Font("Arial", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelLogo1.ForeColor = System.Drawing.Color.Gray;
            this.labelLogo1.Location = new System.Drawing.Point(146, 467);
            this.labelLogo1.Name = "labelLogo1";
            this.labelLogo1.Size = new System.Drawing.Size(71, 29);
            this.labelLogo1.TabIndex = 24;
            this.labelLogo1.Text = "open";
            // 
            // labelLogo2
            // 
            this.labelLogo2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelLogo2.AutoSize = true;
            this.labelLogo2.Font = new System.Drawing.Font("Arial", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelLogo2.ForeColor = System.Drawing.Color.Gray;
            this.labelLogo2.Location = new System.Drawing.Point(185, 492);
            this.labelLogo2.Name = "labelLogo2";
            this.labelLogo2.Size = new System.Drawing.Size(62, 29);
            this.labelLogo2.TabIndex = 25;
            this.labelLogo2.Text = "rails";
            // 
            // buttonFolderAdd
            // 
            this.buttonFolderAdd.Location = new System.Drawing.Point(299, 29);
            this.buttonFolderAdd.Name = "buttonFolderAdd";
            this.buttonFolderAdd.Size = new System.Drawing.Size(75, 23);
            this.buttonFolderAdd.TabIndex = 31;
            this.buttonFolderAdd.Text = "Add...";
            this.buttonFolderAdd.Click += new System.EventHandler(this.buttonFolderAdd_Click);
            // 
            // folderBrowserDialog
            // 
            this.folderBrowserDialog.Description = "Navigate to your alternate MSTS installation folder.";
            this.folderBrowserDialog.ShowNewFolderButton = false;
            // 
            // buttonFolderRemove
            // 
            this.buttonFolderRemove.Location = new System.Drawing.Point(461, 29);
            this.buttonFolderRemove.Name = "buttonFolderRemove";
            this.buttonFolderRemove.Size = new System.Drawing.Size(75, 23);
            this.buttonFolderRemove.TabIndex = 33;
            this.buttonFolderRemove.Text = "Remove";
            this.buttonFolderRemove.Click += new System.EventHandler(this.buttonFolderRemove_Click);
            // 
            // checkBoxWarnings
            // 
            this.checkBoxWarnings.AutoSize = true;
            this.checkBoxWarnings.Checked = true;
            this.checkBoxWarnings.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxWarnings.Location = new System.Drawing.Point(3, 48);
            this.checkBoxWarnings.Name = "checkBoxWarnings";
            this.checkBoxWarnings.Size = new System.Drawing.Size(64, 17);
            this.checkBoxWarnings.TabIndex = 27;
            this.checkBoxWarnings.Text = "Logging";
            this.checkBoxWarnings.UseVisualStyleBackColor = true;
            // 
            // buttonOptions
            // 
            this.buttonOptions.Location = new System.Drawing.Point(3, 19);
            this.buttonOptions.Name = "buttonOptions";
            this.buttonOptions.Size = new System.Drawing.Size(75, 23);
            this.buttonOptions.TabIndex = 26;
            this.buttonOptions.Text = "Options";
            this.buttonOptions.Click += new System.EventHandler(this.buttonOptions_Click);
            // 
            // buttonResume
            // 
            this.buttonResume.Enabled = false;
            this.buttonResume.Location = new System.Drawing.Point(6, 59);
            this.buttonResume.Name = "buttonResume";
            this.buttonResume.Size = new System.Drawing.Size(75, 35);
            this.buttonResume.TabIndex = 1;
            this.buttonResume.Text = "Resume/ Replay...";
            this.buttonResume.Click += new System.EventHandler(this.buttonResume_Click);
            // 
            // buttonTesting
            // 
            this.buttonTesting.Location = new System.Drawing.Point(542, 29);
            this.buttonTesting.Name = "buttonTesting";
            this.buttonTesting.Size = new System.Drawing.Size(75, 23);
            this.buttonTesting.TabIndex = 34;
            this.buttonTesting.Text = "Testing";
            this.buttonTesting.Click += new System.EventHandler(this.buttonTesting_Click);
            // 
            // comboBoxFolder
            // 
            this.comboBoxFolder.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.comboBoxFolder.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.comboBoxFolder.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxFolder.FormattingEnabled = true;
            this.comboBoxFolder.Location = new System.Drawing.Point(12, 31);
            this.comboBoxFolder.Name = "comboBoxFolder";
            this.comboBoxFolder.Size = new System.Drawing.Size(281, 21);
            this.comboBoxFolder.TabIndex = 1;
            this.comboBoxFolder.SelectedIndexChanged += new System.EventHandler(this.comboBoxFolder_SelectedIndexChanged);
            // 
            // comboBoxRoute
            // 
            this.comboBoxRoute.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.comboBoxRoute.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.comboBoxRoute.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxRoute.FormattingEnabled = true;
            this.comboBoxRoute.Location = new System.Drawing.Point(12, 77);
            this.comboBoxRoute.Name = "comboBoxRoute";
            this.comboBoxRoute.Size = new System.Drawing.Size(281, 21);
            this.comboBoxRoute.TabIndex = 3;
            this.comboBoxRoute.SelectedIndexChanged += new System.EventHandler(this.comboBoxRoute_SelectedIndexChanged);
            // 
            // comboBoxActivity
            // 
            this.comboBoxActivity.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.comboBoxActivity.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.comboBoxActivity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxActivity.FormattingEnabled = true;
            this.comboBoxActivity.Location = new System.Drawing.Point(0, 25);
            this.comboBoxActivity.Name = "comboBoxActivity";
            this.comboBoxActivity.Size = new System.Drawing.Size(281, 21);
            this.comboBoxActivity.TabIndex = 5;
            this.comboBoxActivity.SelectedIndexChanged += new System.EventHandler(this.comboBoxActivity_SelectedIndexChanged);
            // 
            // comboBoxLocomotive
            // 
            this.comboBoxLocomotive.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.comboBoxLocomotive.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.comboBoxLocomotive.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxLocomotive.Enabled = false;
            this.comboBoxLocomotive.FormattingEnabled = true;
            this.comboBoxLocomotive.Location = new System.Drawing.Point(0, 71);
            this.comboBoxLocomotive.Name = "comboBoxLocomotive";
            this.comboBoxLocomotive.Size = new System.Drawing.Size(281, 21);
            this.comboBoxLocomotive.TabIndex = 7;
            this.comboBoxLocomotive.SelectedIndexChanged += new System.EventHandler(this.comboBoxLocomotive_SelectedIndexChanged);
            // 
            // comboBoxConsist
            // 
            this.comboBoxConsist.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.comboBoxConsist.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.comboBoxConsist.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxConsist.Enabled = false;
            this.comboBoxConsist.FormattingEnabled = true;
            this.comboBoxConsist.Location = new System.Drawing.Point(0, 115);
            this.comboBoxConsist.Name = "comboBoxConsist";
            this.comboBoxConsist.Size = new System.Drawing.Size(281, 21);
            this.comboBoxConsist.TabIndex = 9;
            this.comboBoxConsist.SelectedIndexChanged += new System.EventHandler(this.comboBoxConsist_SelectedIndexChanged);
            // 
            // comboBoxStartAt
            // 
            this.comboBoxStartAt.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.comboBoxStartAt.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.comboBoxStartAt.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxStartAt.Enabled = false;
            this.comboBoxStartAt.FormattingEnabled = true;
            this.comboBoxStartAt.Location = new System.Drawing.Point(0, 161);
            this.comboBoxStartAt.Name = "comboBoxStartAt";
            this.comboBoxStartAt.Size = new System.Drawing.Size(281, 21);
            this.comboBoxStartAt.TabIndex = 11;
            this.comboBoxStartAt.SelectedIndexChanged += new System.EventHandler(this.comboBoxStartAt_SelectedIndexChanged);
            // 
            // comboBoxHeadTo
            // 
            this.comboBoxHeadTo.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.comboBoxHeadTo.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.comboBoxHeadTo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxHeadTo.Enabled = false;
            this.comboBoxHeadTo.FormattingEnabled = true;
            this.comboBoxHeadTo.Location = new System.Drawing.Point(0, 207);
            this.comboBoxHeadTo.Name = "comboBoxHeadTo";
            this.comboBoxHeadTo.Size = new System.Drawing.Size(281, 21);
            this.comboBoxHeadTo.TabIndex = 13;
            this.comboBoxHeadTo.SelectedIndexChanged += new System.EventHandler(this.comboBoxHeadTo_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 58);
            this.label2.Margin = new System.Windows.Forms.Padding(3);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(39, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Route:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(0, 6);
            this.label3.Margin = new System.Windows.Forms.Padding(3);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(44, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Activity:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(0, 52);
            this.label4.Margin = new System.Windows.Forms.Padding(3);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(65, 13);
            this.label4.TabIndex = 6;
            this.label4.Text = "Locomotive:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(0, 96);
            this.label5.Margin = new System.Windows.Forms.Padding(3);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(44, 13);
            this.label5.TabIndex = 8;
            this.label5.Text = "Consist:";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.label5.Click += new System.EventHandler(this.label5_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(0, 142);
            this.label6.Margin = new System.Windows.Forms.Padding(3);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(58, 13);
            this.label6.TabIndex = 10;
            this.label6.Text = "Starting at:";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(0, 188);
            this.label7.Margin = new System.Windows.Forms.Padding(3);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(62, 13);
            this.label7.TabIndex = 12;
            this.label7.Text = "Heading to:";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBoxMPHost
            // 
            this.textBoxMPHost.Location = new System.Drawing.Point(83, 45);
            this.textBoxMPHost.Name = "textBoxMPHost";
            this.textBoxMPHost.Size = new System.Drawing.Size(156, 20);
            this.textBoxMPHost.TabIndex = 3;
            this.textBoxMPHost.TextChanged += new System.EventHandler(this.textBoxMPUser_TextChanged);
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(6, 48);
            this.label14.Margin = new System.Windows.Forms.Padding(3);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(55, 13);
            this.label14.TabIndex = 2;
            this.label14.Text = "Host/port:";
            this.label14.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(6, 22);
            this.label13.Margin = new System.Windows.Forms.Padding(3);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(61, 13);
            this.label13.TabIndex = 0;
            this.label13.Text = "User name:";
            this.label13.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // buttonMPClient
            // 
            this.buttonMPClient.Enabled = false;
            this.buttonMPClient.Location = new System.Drawing.Point(164, 71);
            this.buttonMPClient.Name = "buttonMPClient";
            this.buttonMPClient.Size = new System.Drawing.Size(75, 23);
            this.buttonMPClient.TabIndex = 5;
            this.buttonMPClient.Text = "Client";
            this.buttonMPClient.Click += new System.EventHandler(this.buttonMPClient_Click);
            // 
            // buttonMPServer
            // 
            this.buttonMPServer.Enabled = false;
            this.buttonMPServer.Location = new System.Drawing.Point(83, 71);
            this.buttonMPServer.Name = "buttonMPServer";
            this.buttonMPServer.Size = new System.Drawing.Size(75, 23);
            this.buttonMPServer.TabIndex = 4;
            this.buttonMPServer.Text = "Server";
            this.buttonMPServer.Click += new System.EventHandler(this.buttonMPServer_Click);
            // 
            // comboBoxDuration
            // 
            this.comboBoxDuration.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.comboBoxDuration.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.comboBoxDuration.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxDuration.Enabled = false;
            this.comboBoxDuration.FormattingEnabled = true;
            this.comboBoxDuration.Location = new System.Drawing.Point(209, 240);
            this.comboBoxDuration.Name = "comboBoxDuration";
            this.comboBoxDuration.Size = new System.Drawing.Size(73, 21);
            this.comboBoxDuration.TabIndex = 21;
            // 
            // comboBoxStartTime
            // 
            this.comboBoxStartTime.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.comboBoxStartTime.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.comboBoxStartTime.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxStartTime.Enabled = false;
            this.comboBoxStartTime.FormattingEnabled = true;
            this.comboBoxStartTime.Location = new System.Drawing.Point(58, 240);
            this.comboBoxStartTime.Name = "comboBoxStartTime";
            this.comboBoxStartTime.Size = new System.Drawing.Size(73, 21);
            this.comboBoxStartTime.TabIndex = 15;
            this.comboBoxStartTime.TextChanged += new System.EventHandler(this.comboBoxStartTime_TextChanged);
            // 
            // comboBoxStartSeason
            // 
            this.comboBoxStartSeason.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.comboBoxStartSeason.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.comboBoxStartSeason.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxStartSeason.Enabled = false;
            this.comboBoxStartSeason.FormattingEnabled = true;
            this.comboBoxStartSeason.Location = new System.Drawing.Point(58, 268);
            this.comboBoxStartSeason.Name = "comboBoxStartSeason";
            this.comboBoxStartSeason.Size = new System.Drawing.Size(73, 21);
            this.comboBoxStartSeason.TabIndex = 17;
            this.comboBoxStartSeason.SelectedIndexChanged += new System.EventHandler(this.comboBoxStartSeason_SelectedIndexChanged);
            // 
            // comboBoxDifficulty
            // 
            this.comboBoxDifficulty.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.comboBoxDifficulty.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.comboBoxDifficulty.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxDifficulty.Enabled = false;
            this.comboBoxDifficulty.FormattingEnabled = true;
            this.comboBoxDifficulty.Location = new System.Drawing.Point(209, 268);
            this.comboBoxDifficulty.Name = "comboBoxDifficulty";
            this.comboBoxDifficulty.Size = new System.Drawing.Size(73, 21);
            this.comboBoxDifficulty.TabIndex = 23;
            // 
            // comboBoxStartWeather
            // 
            this.comboBoxStartWeather.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.comboBoxStartWeather.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.comboBoxStartWeather.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxStartWeather.Enabled = false;
            this.comboBoxStartWeather.FormattingEnabled = true;
            this.comboBoxStartWeather.Location = new System.Drawing.Point(58, 295);
            this.comboBoxStartWeather.Name = "comboBoxStartWeather";
            this.comboBoxStartWeather.Size = new System.Drawing.Size(73, 21);
            this.comboBoxStartWeather.TabIndex = 19;
            this.comboBoxStartWeather.SelectedIndexChanged += new System.EventHandler(this.comboBoxStartWeather_SelectedIndexChanged);
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(1, 298);
            this.label12.Margin = new System.Windows.Forms.Padding(3);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(51, 13);
            this.label12.TabIndex = 18;
            this.label12.Text = "Weather:";
            this.label12.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(141, 243);
            this.label11.Margin = new System.Windows.Forms.Padding(3);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(50, 13);
            this.label11.TabIndex = 20;
            this.label11.Text = "Duration:";
            this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(141, 271);
            this.label10.Margin = new System.Windows.Forms.Padding(3);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(50, 13);
            this.label10.TabIndex = 22;
            this.label10.Text = "Difficulty:";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(1, 243);
            this.label9.Margin = new System.Windows.Forms.Padding(3);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(33, 13);
            this.label9.TabIndex = 14;
            this.label9.Text = "Time:";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(1, 271);
            this.label8.Margin = new System.Windows.Forms.Padding(3);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(46, 13);
            this.label8.TabIndex = 16;
            this.label8.Text = "Season:";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBoxMPUser
            // 
            this.textBoxMPUser.Location = new System.Drawing.Point(83, 19);
            this.textBoxMPUser.Name = "textBoxMPUser";
            this.textBoxMPUser.Size = new System.Drawing.Size(156, 20);
            this.textBoxMPUser.TabIndex = 1;
            this.textBoxMPUser.TextChanged += new System.EventHandler(this.textBoxMPUser_TextChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.label13);
            this.groupBox1.Controls.Add(this.textBoxMPHost);
            this.groupBox1.Controls.Add(this.textBoxMPUser);
            this.groupBox1.Controls.Add(this.buttonMPClient);
            this.groupBox1.Controls.Add(this.label14);
            this.groupBox1.Controls.Add(this.buttonMPServer);
            this.groupBox1.Location = new System.Drawing.Point(597, 436);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(245, 100);
            this.groupBox1.TabIndex = 30;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Multiplayer";
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Controls.Add(this.buttonResume);
            this.groupBox3.Controls.Add(this.buttonStart);
            this.groupBox3.Location = new System.Drawing.Point(504, 436);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(87, 100);
            this.groupBox3.TabIndex = 29;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Singleplayer";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 12);
            this.label1.Margin = new System.Windows.Forms.Padding(3);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(91, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Installation profile:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panelDetails
            // 
            this.panelDetails.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.panelDetails.AutoScroll = true;
            this.panelDetails.BackColor = System.Drawing.SystemColors.Window;
            this.panelDetails.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelDetails.Location = new System.Drawing.Point(299, 58);
            this.panelDetails.Name = "panelDetails";
            this.panelDetails.Size = new System.Drawing.Size(543, 372);
            this.panelDetails.TabIndex = 35;
            // 
            // pictureBoxLogo
            // 
            this.pictureBoxLogo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.pictureBoxLogo.Image = ((System.Drawing.Image)(resources.GetObject("pictureBoxLogo.Image")));
            this.pictureBoxLogo.Location = new System.Drawing.Point(73, 462);
            this.pictureBoxLogo.Name = "pictureBoxLogo";
            this.pictureBoxLogo.Size = new System.Drawing.Size(67, 68);
            this.pictureBoxLogo.TabIndex = 5;
            this.pictureBoxLogo.TabStop = false;
            // 
            // buttonFolderEdit
            // 
            this.buttonFolderEdit.Location = new System.Drawing.Point(380, 29);
            this.buttonFolderEdit.Name = "buttonFolderEdit";
            this.buttonFolderEdit.Size = new System.Drawing.Size(75, 23);
            this.buttonFolderEdit.TabIndex = 32;
            this.buttonFolderEdit.Text = "Edit";
            this.buttonFolderEdit.Click += new System.EventHandler(this.buttonFolderEdit_Click);
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Controls.Add(this.buttonOptions);
            this.panel1.Controls.Add(this.checkBoxWarnings);
            this.panel1.Controls.Add(this.checkBoxWindowed);
            this.panel1.Location = new System.Drawing.Point(411, 436);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(87, 100);
            this.panel1.TabIndex = 36;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.ActivityPage);
            this.tabControl1.Controls.Add(this.TimetablePage);
            this.tabControl1.ItemSize = new System.Drawing.Size(50, 20);
            this.tabControl1.Location = new System.Drawing.Point(5, 104);
            this.tabControl1.Multiline = true;
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(288, 346);
            this.tabControl1.TabIndex = 37;
            // 
            // ActivityPage
            // 
            this.ActivityPage.Controls.Add(this.label3);
            this.ActivityPage.Controls.Add(this.comboBoxActivity);
            this.ActivityPage.Controls.Add(this.label4);
            this.ActivityPage.Controls.Add(this.comboBoxLocomotive);
            this.ActivityPage.Controls.Add(this.comboBoxConsist);
            this.ActivityPage.Controls.Add(this.label5);
            this.ActivityPage.Controls.Add(this.label6);
            this.ActivityPage.Controls.Add(this.comboBoxStartAt);
            this.ActivityPage.Controls.Add(this.label7);
            this.ActivityPage.Controls.Add(this.comboBoxHeadTo);
            this.ActivityPage.Controls.Add(this.label11);
            this.ActivityPage.Controls.Add(this.label9);
            this.ActivityPage.Controls.Add(this.comboBoxStartTime);
            this.ActivityPage.Controls.Add(this.comboBoxDuration);
            this.ActivityPage.Controls.Add(this.comboBoxStartWeather);
            this.ActivityPage.Controls.Add(this.label12);
            this.ActivityPage.Controls.Add(this.comboBoxStartSeason);
            this.ActivityPage.Controls.Add(this.label10);
            this.ActivityPage.Controls.Add(this.comboBoxDifficulty);
            this.ActivityPage.Controls.Add(this.label8);
            this.ActivityPage.Location = new System.Drawing.Point(4, 24);
            this.ActivityPage.Name = "ActivityPage";
            this.ActivityPage.Padding = new System.Windows.Forms.Padding(3);
            this.ActivityPage.Size = new System.Drawing.Size(280, 318);
            this.ActivityPage.TabIndex = 1;
            this.ActivityPage.Text = "Activity Details";
            this.ActivityPage.UseVisualStyleBackColor = true;
            // 
            // TimetablePage
            // 
            this.TimetablePage.Controls.Add(this.label24);
            this.TimetablePage.Controls.Add(this.comboBoxPlayerTrain);
            this.TimetablePage.Controls.Add(this.label23);
            this.TimetablePage.Controls.Add(this.comboBoxTimetableDay);
            this.TimetablePage.Controls.Add(this.label22);
            this.TimetablePage.Controls.Add(this.comboBoxTimetableWeather);
            this.TimetablePage.Controls.Add(this.label20);
            this.TimetablePage.Controls.Add(this.comboBoxTimetableSeason);
            this.TimetablePage.Controls.Add(this.label21);
            this.TimetablePage.Controls.Add(this.groupBoxAITrains);
            this.TimetablePage.Controls.Add(this.comboBoxPlayerTimetable);
            this.TimetablePage.Controls.Add(this.label16);
            this.TimetablePage.Controls.Add(this.comboBoxTimetable);
            this.TimetablePage.Controls.Add(this.label15);
            this.TimetablePage.Location = new System.Drawing.Point(4, 24);
            this.TimetablePage.Name = "TimetablePage";
            this.TimetablePage.Padding = new System.Windows.Forms.Padding(3);
            this.TimetablePage.Size = new System.Drawing.Size(280, 318);
            this.TimetablePage.TabIndex = 0;
            this.TimetablePage.Text = "Timetable";
            this.TimetablePage.UseVisualStyleBackColor = true;
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.Location = new System.Drawing.Point(13, 97);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(40, 13);
            this.label24.TabIndex = 28;
            this.label24.Text = "Train : ";
            // 
            // comboBoxPlayerTrain
            // 
            this.comboBoxPlayerTrain.FormattingEnabled = true;
            this.comboBoxPlayerTrain.Location = new System.Drawing.Point(81, 93);
            this.comboBoxPlayerTrain.Name = "comboBoxPlayerTrain";
            this.comboBoxPlayerTrain.Size = new System.Drawing.Size(193, 21);
            this.comboBoxPlayerTrain.TabIndex = 27;
            // 
            // label23
            // 
            this.label23.AutoSize = true;
            this.label23.Location = new System.Drawing.Point(13, 70);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(62, 13);
            this.label23.TabIndex = 26;
            this.label23.Text = "Timetable : ";
            // 
            // comboBoxTimetableDay
            // 
            this.comboBoxTimetableDay.FormattingEnabled = true;
            this.comboBoxTimetableDay.Location = new System.Drawing.Point(67, 251);
            this.comboBoxTimetableDay.Name = "comboBoxTimetableDay";
            this.comboBoxTimetableDay.Size = new System.Drawing.Size(73, 21);
            this.comboBoxTimetableDay.TabIndex = 25;
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.Location = new System.Drawing.Point(13, 255);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(26, 13);
            this.label22.TabIndex = 24;
            this.label22.Text = "Day";
            this.label22.Click += new System.EventHandler(this.label22_Click);
            // 
            // comboBoxTimetableWeather
            // 
            this.comboBoxTimetableWeather.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.comboBoxTimetableWeather.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.comboBoxTimetableWeather.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxTimetableWeather.FormattingEnabled = true;
            this.comboBoxTimetableWeather.Location = new System.Drawing.Point(199, 278);
            this.comboBoxTimetableWeather.Name = "comboBoxTimetableWeather";
            this.comboBoxTimetableWeather.Size = new System.Drawing.Size(73, 21);
            this.comboBoxTimetableWeather.TabIndex = 23;
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Location = new System.Drawing.Point(142, 281);
            this.label20.Margin = new System.Windows.Forms.Padding(3);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(51, 13);
            this.label20.TabIndex = 22;
            this.label20.Text = "Weather:";
            this.label20.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // comboBoxTimetableSeason
            // 
            this.comboBoxTimetableSeason.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.comboBoxTimetableSeason.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.comboBoxTimetableSeason.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxTimetableSeason.FormattingEnabled = true;
            this.comboBoxTimetableSeason.Location = new System.Drawing.Point(67, 278);
            this.comboBoxTimetableSeason.Name = "comboBoxTimetableSeason";
            this.comboBoxTimetableSeason.Size = new System.Drawing.Size(73, 21);
            this.comboBoxTimetableSeason.TabIndex = 21;
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(10, 281);
            this.label21.Margin = new System.Windows.Forms.Padding(3);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(46, 13);
            this.label21.TabIndex = 20;
            this.label21.Text = "Season:";
            this.label21.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // groupBoxAITrains
            // 
            this.groupBoxAITrains.Controls.Add(this.radioButtonAITimeRelative);
            this.groupBoxAITrains.Controls.Add(this.radioButtonAITimeAbsolute);
            this.groupBoxAITrains.Controls.Add(this.checkBoxAISameDirection);
            this.groupBoxAITrains.Controls.Add(this.label19);
            this.groupBoxAITrains.Controls.Add(this.numericUpDownAIMins);
            this.groupBoxAITrains.Controls.Add(this.label17);
            this.groupBoxAITrains.Controls.Add(this.numericUpDownAIHours);
            this.groupBoxAITrains.Controls.Add(this.label18);
            this.groupBoxAITrains.Location = new System.Drawing.Point(7, 122);
            this.groupBoxAITrains.Name = "groupBoxAITrains";
            this.groupBoxAITrains.Size = new System.Drawing.Size(266, 123);
            this.groupBoxAITrains.TabIndex = 6;
            this.groupBoxAITrains.TabStop = false;
            this.groupBoxAITrains.Text = "AI Train Selection";
            // 
            // radioButtonAITimeRelative
            // 
            this.radioButtonAITimeRelative.AutoSize = true;
            this.radioButtonAITimeRelative.Location = new System.Drawing.Point(6, 68);
            this.radioButtonAITimeRelative.Name = "radioButtonAITimeRelative";
            this.radioButtonAITimeRelative.Size = new System.Drawing.Size(196, 17);
            this.radioButtonAITimeRelative.TabIndex = 13;
            this.radioButtonAITimeRelative.TabStop = true;
            this.radioButtonAITimeRelative.Tag = "AITimeSelection";
            this.radioButtonAITimeRelative.Text = "Relative time before player train start";
            this.radioButtonAITimeRelative.UseVisualStyleBackColor = true;
            // 
            // radioButtonAITimeAbsolute
            // 
            this.radioButtonAITimeAbsolute.AutoSize = true;
            this.radioButtonAITimeAbsolute.Location = new System.Drawing.Point(6, 52);
            this.radioButtonAITimeAbsolute.Name = "radioButtonAITimeAbsolute";
            this.radioButtonAITimeAbsolute.Size = new System.Drawing.Size(92, 17);
            this.radioButtonAITimeAbsolute.TabIndex = 12;
            this.radioButtonAITimeAbsolute.TabStop = true;
            this.radioButtonAITimeAbsolute.Tag = "AITimeSelection";
            this.radioButtonAITimeAbsolute.Text = "Absolute Time";
            this.radioButtonAITimeAbsolute.UseVisualStyleBackColor = true;
            // 
            // checkBoxAISameDirection
            // 
            this.checkBoxAISameDirection.AutoSize = true;
            this.checkBoxAISameDirection.Location = new System.Drawing.Point(8, 95);
            this.checkBoxAISameDirection.Name = "checkBoxAISameDirection";
            this.checkBoxAISameDirection.Size = new System.Drawing.Size(194, 17);
            this.checkBoxAISameDirection.TabIndex = 11;
            this.checkBoxAISameDirection.Text = "Include AI in direction of player train";
            this.checkBoxAISameDirection.UseVisualStyleBackColor = true;
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(198, 22);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(28, 13);
            this.label19.TabIndex = 9;
            this.label19.Text = "mins";
            // 
            // numericUpDownAIMins
            // 
            this.numericUpDownAIMins.Location = new System.Drawing.Point(152, 20);
            this.numericUpDownAIMins.Maximum = new decimal(new int[] {
            59,
            0,
            0,
            0});
            this.numericUpDownAIMins.Name = "numericUpDownAIMins";
            this.numericUpDownAIMins.Size = new System.Drawing.Size(44, 20);
            this.numericUpDownAIMins.TabIndex = 8;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(126, 22);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(21, 13);
            this.label17.TabIndex = 7;
            this.label17.Text = "hrs";
            // 
            // numericUpDownAIHours
            // 
            this.numericUpDownAIHours.Location = new System.Drawing.Point(80, 20);
            this.numericUpDownAIHours.Maximum = new decimal(new int[] {
            24,
            0,
            0,
            0});
            this.numericUpDownAIHours.Name = "numericUpDownAIHours";
            this.numericUpDownAIHours.Size = new System.Drawing.Size(44, 20);
            this.numericUpDownAIHours.TabIndex = 6;
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(3, 22);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(79, 13);
            this.label18.TabIndex = 5;
            this.label18.Text = "Start AI trains : ";
            // 
            // comboBoxPlayerTimetable
            // 
            this.comboBoxPlayerTimetable.FormattingEnabled = true;
            this.comboBoxPlayerTimetable.Location = new System.Drawing.Point(81, 66);
            this.comboBoxPlayerTimetable.Name = "comboBoxPlayerTimetable";
            this.comboBoxPlayerTimetable.Size = new System.Drawing.Size(193, 21);
            this.comboBoxPlayerTimetable.TabIndex = 3;
            this.comboBoxPlayerTimetable.SelectedIndexChanged += new System.EventHandler(this.comboboxPlayerTimetable_selectedIndexChanged);
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(10, 49);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(63, 13);
            this.label16.TabIndex = 2;
            this.label16.Text = "Player Train";
            // 
            // comboBoxTimetable
            // 
            this.comboBoxTimetable.FormattingEnabled = true;
            this.comboBoxTimetable.Location = new System.Drawing.Point(10, 21);
            this.comboBoxTimetable.Name = "comboBoxTimetable";
            this.comboBoxTimetable.Size = new System.Drawing.Size(264, 21);
            this.comboBoxTimetable.TabIndex = 1;
            this.comboBoxTimetable.SelectedIndexChanged += new System.EventHandler(this.ComboBoxTimetable_SelectedIndexChanged);
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(7, 4);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(72, 13);
            this.label15.TabIndex = 0;
            this.label15.Text = "Timetable File";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(854, 548);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.buttonFolderEdit);
            this.Controls.Add(this.panelDetails);
            this.Controls.Add(this.comboBoxFolder);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.buttonFolderAdd);
            this.Controls.Add(this.buttonFolderRemove);
            this.Controls.Add(this.comboBoxRoute);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.pictureBoxLogo);
            this.Controls.Add(this.labelLogo2);
            this.Controls.Add(this.labelLogo1);
            this.Controls.Add(this.buttonTesting);
            this.Controls.Add(this.label2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Open Rails";
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.ActivityPage.ResumeLayout(false);
            this.ActivityPage.PerformLayout();
            this.TimetablePage.ResumeLayout(false);
            this.TimetablePage.PerformLayout();
            this.groupBoxAITrains.ResumeLayout(false);
            this.groupBoxAITrains.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownAIMins)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownAIHours)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox checkBoxWindowed;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.Label labelLogo1;
        private System.Windows.Forms.PictureBox pictureBoxLogo;
        private System.Windows.Forms.Label labelLogo2;
        private System.Windows.Forms.Button buttonFolderAdd;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
		private System.Windows.Forms.Button buttonFolderRemove;
        private System.Windows.Forms.CheckBox checkBoxWarnings;
        private System.Windows.Forms.Button buttonOptions;
        private System.Windows.Forms.Button buttonResume;
        private System.Windows.Forms.Button buttonTesting;
        private System.Windows.Forms.ComboBox comboBoxFolder;
        private System.Windows.Forms.ComboBox comboBoxRoute;
        private System.Windows.Forms.ComboBox comboBoxActivity;
        private System.Windows.Forms.ComboBox comboBoxLocomotive;
        private System.Windows.Forms.ComboBox comboBoxConsist;
        private System.Windows.Forms.ComboBox comboBoxStartAt;
        private System.Windows.Forms.ComboBox comboBoxHeadTo;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox comboBoxStartSeason;
        private System.Windows.Forms.ComboBox comboBoxDifficulty;
        private System.Windows.Forms.ComboBox comboBoxStartWeather;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox comboBoxStartTime;
        private System.Windows.Forms.ComboBox comboBoxDuration;
        private System.Windows.Forms.Button buttonMPClient;
        private System.Windows.Forms.Button buttonMPServer;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox textBoxMPHost;
        private System.Windows.Forms.TextBox textBoxMPUser;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panelDetails;
        private System.Windows.Forms.Button buttonFolderEdit;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage TimetablePage;
        private System.Windows.Forms.TabPage ActivityPage;
        private System.Windows.Forms.ComboBox comboBoxPlayerTimetable;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.ComboBox comboBoxTimetable;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.GroupBox groupBoxAITrains;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.NumericUpDown numericUpDownAIMins;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.NumericUpDown numericUpDownAIHours;
        private System.Windows.Forms.ComboBox comboBoxTimetableWeather;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.ComboBox comboBoxTimetableSeason;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.CheckBox checkBoxAISameDirection;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.ComboBox comboBoxTimetableDay;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.ComboBox comboBoxPlayerTrain;
        private System.Windows.Forms.RadioButton radioButtonAITimeRelative;
        private System.Windows.Forms.RadioButton radioButtonAITimeAbsolute;
    }
}
