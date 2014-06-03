using System;
using System.Windows.Forms;
//using LibAE;


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
        protected internal virtual bool OnWizardFinish()
        {
            return true;
        }

        protected internal virtual string OnWizardNext()
        {
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
