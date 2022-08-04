using System;
using System.Windows.Forms;
using LibAE;


namespace AEWizard
{
    public class SinglePage : UserControl
    {
        public SinglePage()
        {
        }
        protected WizardForm Wizard
        {
            get
            {
                // Return the parent WizardForm
                return (WizardForm)(Parent);
            }
        }


        protected internal virtual bool OnKillActive()
        {
            // Deactivate if validation successful
            return Validate();
        }
        protected internal virtual bool OnSetActive()
        {
            // Activate the page
            return true;
        }


        protected internal virtual string OnWizardBack()
        {
            // Move to the default previous page in the wizard

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
        protected internal virtual bool OnWizardFinish()
        {
            // Finish the wizard
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
        protected internal virtual string OnWizardNext()
        {
            // Move to the default next page in the wizard
            return WizardForm.NextPage;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // SinglePage
            // 
            this.Name = "SinglePage";
            this.Size = new System.Drawing.Size(436, 195);
            this.ResumeLayout(false);

        }

    }
}
