using System;
using System.Windows.Forms;
using LibAE.Formats;

namespace AEWizard
{
	/// <summary>
	/// Represents a single page within a wizard dialog.
	/// </summary>
	public class ActivityDescr : SinglePage
    {
        private FlowLayoutPanel panel1;
        private TextBox textBox1;
        private Label label3;
        private TextBox ActivityDescrCB;
        private Label label2;
        private TextBox activityNameCB;
        private Label label1;
        private ComboBox routePathCB;

        public String RoutePath;
        public ActivityInfo activityInfo { get; set; }
       // ==================================================================
        // Public Constructors
        // ==================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="SMS.Windows.Forms.WizardPage">WizardPage</see>
        /// class.
        /// </summary>
        public ActivityDescr()
		{
            // Required for Windows Form Designer support
            InitializeComponent();
            
		}

        public void completePage()
        {
            this.routePathCB.DataSource = activityInfo.routePaths;
        }

        // ==================================================================
        // Protected Properties
        // ==================================================================
        
        
        
        // ==================================================================
        // Private Methods
        // ==================================================================
        
        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.panel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.routePathCB = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.activityNameCB = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.ActivityDescrCB = new System.Windows.Forms.TextBox();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.textBox1);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.routePathCB);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.activityNameCB);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.ActivityDescrCB);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(497, 350);
            this.panel1.TabIndex = 0;
            // 
            // textBox1
            // 
            this.textBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F);
            this.textBox1.Location = new System.Drawing.Point(3, 3);
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.ShortcutsEnabled = false;
            this.textBox1.Size = new System.Drawing.Size(491, 31);
            this.textBox1.TabIndex = 1;
            this.textBox1.Text = "Activity Editor: Create new activity";
            this.textBox1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label1.Location = new System.Drawing.Point(3, 37);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(79, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "Route Path";
            // 
            // routePathCB
            // 
            this.routePathCB.FormattingEnabled = true;
            this.routePathCB.Location = new System.Drawing.Point(3, 57);
            this.routePathCB.Name = "routePathCB";
            this.routePathCB.Size = new System.Drawing.Size(491, 21);
            this.routePathCB.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label2.Location = new System.Drawing.Point(3, 81);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(93, 17);
            this.label2.TabIndex = 4;
            this.label2.Text = "Activity Name";
            // 
            // activityNameCB
            // 
            this.activityNameCB.Location = new System.Drawing.Point(3, 101);
            this.activityNameCB.Name = "activityNameCB";
            this.activityNameCB.Size = new System.Drawing.Size(491, 20);
            this.activityNameCB.TabIndex = 3;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label3.Location = new System.Drawing.Point(3, 124);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(127, 17);
            this.label3.TabIndex = 6;
            this.label3.Text = "Activity Description";
            // 
            // ActivityDescrCB
            // 
            this.ActivityDescrCB.Location = new System.Drawing.Point(3, 144);
            this.ActivityDescrCB.Multiline = true;
            this.ActivityDescrCB.Name = "ActivityDescrCB";
            this.ActivityDescrCB.Size = new System.Drawing.Size(491, 150);
            this.ActivityDescrCB.TabIndex = 3;
            // 
            // ActivityDescr
            // 
            this.Controls.Add(this.panel1);
            this.Name = "ActivityDescr";
            this.Size = new System.Drawing.Size(497, 350);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }
		#endregion


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
            return Validate();
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
            if (routePathCB.Text.Length <= 0)
                return WizardForm.NoPageChange;

            return WizardForm.NextPage;
        }
        
        /// <summary>
        /// Called when the user clicks the Finish button in a wizard.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the wizard finishes successfully; otherwise
        /// <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Override this method to specify some action the user must take
        /// when the Finish button is pressed.  Return <c>false</c> to
        /// prevent the wizard from finishing.
        /// </remarks>
        protected internal override bool OnWizardFinish()
        {
            // Finish the wizard
            if (routePathCB.Text.Length <= 0)
                return false;
            return true;
        }
        
        /// <summary>
        /// Called when the user clicks the Next button in a wizard.
        /// </summary>
        /// <returns>
        /// <c>WizardForm.DefaultPage</c> to automatically advance to the
        /// next page; <c>WizardForm.NoPageChange</c> to prevent the page
        /// changing.  To jump to a page other than the next one, return
        /// the <c>Name</c> of the page to be displayed.
        /// </returns>
        /// <remarks>
        /// Override this method to specify some action the user must take
        /// when the Next button is pressed.
        /// </remarks>
        protected internal override string OnWizardNext()
        {
            // Move to the default next page in the wizard
            if (routePathCB.Text.Length <= 0)
                return WizardForm.NoPageChange;
            activityInfo.RoutePath = routePathCB.Text;
            activityInfo.ActivityName = activityNameCB.Text;
            activityInfo.ActivityDescr = ActivityDescrCB.Text;

            return WizardForm.NextPage;
        }
    }
}
