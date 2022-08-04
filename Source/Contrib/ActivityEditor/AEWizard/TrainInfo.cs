using System;
using System.Windows.Forms;
using System.Linq;
using LibAE;
using LibAE.Formats;

namespace AEWizard
{
    public partial class TrainInfo : SinglePage
    {
        private Panel panel1;
        private ComboBox trainConsistCB;
        private Label label1;
        private TextBox trainNameCB;
        private Label label2;
        private TextBox TrainInfoTB1;

        public ActivityInfo activityInfo { get; set; }

        public TrainInfo()
        {
            InitializeComponent();
        }

        public void completePage()
        {
            this.trainConsistCB.DataSource = activityInfo.trainConsists.Select(o => o.consistName).ToList();
        }
                // ==================================================================
        // Protected Properties
        // ==================================================================
        
        /// <summary>
        /// Gets the <see cref="SMS.Windows.Forms.WizardForm">WizardForm</see>
        /// to which this <see cref="SMS.Windows.Forms.WizardPage">WizardPage</see>
        /// belongs.
        /// </summary>
        
        // ==================================================================
        // Private Methods
        // ==================================================================


        // ==================================================================
        // Protected Internal Methods
        // ==================================================================

        /// <summary>
        /// Called when the page is no longer the active page.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the page was successfully deactivated; otherwise
        /// <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Override this method to perform special data validation tasks.
        /// </remarks>
        protected internal override bool OnKillActive()
        {
            // Deactivate if validation successful
            return Wizard.Validate();
        }

        /// <summary>
        /// Called when the page becomes the active page.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the page was successfully set active; otherwise
        /// <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Override this method to performs tasks when a page is activated.
        /// Your override of this method should call the default version
        /// before any other processing is done.
        /// </remarks>
        protected internal override bool OnSetActive()
        {
            // Activate the page
            Wizard.SetWizardButtons(WizardButton.Finish);
            return true;
        }

        /// <summary>
        /// Called when the user clicks the Back button in a wizard.
        /// </summary>
        /// <returns>
        /// <c>WizardForm.DefaultPage</c> to automatically advance to the
        /// next page; <c>WizardForm.NoPageChange</c> to prevent the page
        /// changing.  To jump to a page other than the next one, return
        /// the <c>Name</c> of the page to be displayed.
        /// </returns>
        /// <remarks>
        /// Override this method to specify some action the user must take
        /// when the Back button is pressed.
        /// </remarks>
        protected internal override string OnWizardBack()
        {
            // Move to the default previous page in the wizard
            Wizard.SetWizardButtons(WizardButton.Next);
            return WizardForm.NextPage;
        }
        
        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.panel1 = new System.Windows.Forms.Panel();
            this.TrainInfoTB1 = new System.Windows.Forms.TextBox();
            this.trainConsistCB = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.trainNameCB = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.trainNameCB);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.trainConsistCB);
            this.panel1.Location = new System.Drawing.Point(0, 39);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(497, 310);
            this.panel1.TabIndex = 0;
            // 
            // TrainInfoTB1
            // 
            this.TrainInfoTB1.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TrainInfoTB1.Location = new System.Drawing.Point(5, 2);
            this.TrainInfoTB1.Name = "TrainInfoTB1";
            this.TrainInfoTB1.ReadOnly = true;
            this.TrainInfoTB1.ShortcutsEnabled = false;
            this.TrainInfoTB1.Size = new System.Drawing.Size(492, 31);
            this.TrainInfoTB1.TabIndex = 1;
            this.TrainInfoTB1.Text = "Activity Editor: Specify Train Info";
            this.TrainInfoTB1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // trainConsistCB
            // 
            this.trainConsistCB.FormattingEnabled = true;
            this.trainConsistCB.Location = new System.Drawing.Point(5, 12);
            this.trainConsistCB.Name = "trainConsistCB";
            this.trainConsistCB.Size = new System.Drawing.Size(301, 21);
            this.trainConsistCB.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(328, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(134, 17);
            this.label1.TabIndex = 3;
            this.label1.Text = "Select Train Consist";
            // 
            // trainNameCB
            // 
            this.trainNameCB.Location = new System.Drawing.Point(5, 39);
            this.trainNameCB.Name = "trainNameCB";
            this.trainNameCB.Size = new System.Drawing.Size(301, 20);
            this.trainNameCB.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(328, 39);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(123, 17);
            this.label2.TabIndex = 5;
            this.label2.Text = "Train Name (Opt.)";
            // 
            // TrainInfo
            // 
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.TrainInfoTB1);
            this.Name = "TrainInfo";
            this.Size = new System.Drawing.Size(497, 350);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

    }
}
