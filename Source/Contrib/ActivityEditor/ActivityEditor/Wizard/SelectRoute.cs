using System;
using System.Windows.Forms;
using System.Collections.Generic;

using LibAE;
using Orts.Formats.OR;

namespace AEWizard
{
    public class SelectRoute : SinglePage
    {
        private Panel panel1;
        private Label label4;
        private ComboBox routePathCB;
        private TextBox textBox1;

        public String RoutePath;
        public RouteInfo routeInfo { get; set; }

        public SelectRoute()
        {
            InitializeComponent();
        }

        public void completePage()
        {
            this.routePathCB.DataSource = routeInfo.routePaths;
        }

        private void InitializeComponent()
        {
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label4 = new System.Windows.Forms.Label();
            this.routePathCB = new System.Windows.Forms.ComboBox();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F);
            this.textBox1.Location = new System.Drawing.Point(3, 3);
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.ShortcutsEnabled = false;
            this.textBox1.Size = new System.Drawing.Size(492, 31);
            this.textBox1.TabIndex = 2;
            this.textBox1.Text = "Route Configuration: Select Route";
            this.textBox1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.routePathCB);
            this.panel1.Location = new System.Drawing.Point(3, 37);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(491, 310);
            this.panel1.TabIndex = 5;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label4.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label4.Location = new System.Drawing.Point(328, 12);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(79, 17);
            this.label4.TabIndex = 2;
            this.label4.Text = "Route Path";
            // 
            // comboBox1
            // 
            this.routePathCB.FormattingEnabled = true;
            this.routePathCB.Location = new System.Drawing.Point(5, 12);
            this.routePathCB.Name = "comboBox1";
            this.routePathCB.Size = new System.Drawing.Size(301, 21);
            this.routePathCB.TabIndex = 1;
            // 
            // SelectRoute
            // 
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.textBox1);
            this.Name = "SelectRoute";
            this.Size = new System.Drawing.Size(497, 350);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
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
            routeInfo.route = routePathCB.Text;
            return true;
        }
        

    }
}
