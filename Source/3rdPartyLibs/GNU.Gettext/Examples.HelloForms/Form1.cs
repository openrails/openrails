using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;

using GNU.Gettext.WinForms;

namespace GNU.Gettext.Examples
{
    public partial class Form1 : Form
    {
		private ObjectPropertiesStore store = new ObjectPropertiesStore();
        public Form1()
        {
            InitializeComponent();
			rbEnUs.PerformClick();
        }

		private void SetTexts()
		{
            GettextResourceManager catalog = new GettextResourceManager();
            // If satellite assemblies have another base name use GettextResourceManager("Examples.HelloForms.Messages") constructor
			// If you call from another assembly, use GettextResourceManager(anotherAssembly) constructor
			Localizer.Localize(this, catalog, store);
			// We need pass 'store' argument only to be able revert original text and switch languages on fly
			// Common use case doesn't required it: Localizer.Localize(this, catalog);

			// Manually formatted strings
			label2.Text = catalog.GetStringFmt("This program is running as process number \"{0}\".",
			                                   System.Diagnostics.Process.GetCurrentProcess().Id);
            label3.Text = String.Format(
				catalog.GetPluralString("found {0} similar word", "found {0} similar words", 1),
				1);
            label4.Text = String.Format(
				catalog.GetPluralString("found {0} similar word", "found {0} similar words", 2),
				2);
            label5.Text = String.Format(
				catalog.GetPluralString("found {0} similar word", "found {0} similar words", 5),
				5);
            label6.Text = String.Format("{0} ('computers')",  catalog.GetParticularString("Computers", "Text encoding"));
            label7.Text = String.Format("{0} ('military')",  catalog.GetParticularString("Military", "Text encoding"));
            label8.Text = String.Format("{0} (non contextual)",  catalog.GetString("Text encoding"));
		}

		private void OnLocaleChanged(object sender, EventArgs e)
		{
			string locale = "en-US";
			if (sender == rbFrFr)
			{
				locale = "fr-FR";
			}
			else if (sender == rbRuRu)
			{
				locale = "ru-RU";
			}
            System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo(locale);
			GNU.Gettext.WinForms.Localizer.Revert(this, store);
			SetTexts();
		}

        #region Windows Form Designer code
        private System.ComponentModel.IContainer components = null;
		
		private ToolTip toolTip1;
		private RadioButton rbEnUs;
		private RadioButton rbFrFr;
		private RadioButton rbRuRu;
		private GroupBox gbSwitch;
		private GroupBox gbForms;
		private Label label1;
		private Label label2;
		private Label label3;
		private Label label4;
		private Label label5;
		private Label label6;
		private Label label7;
		private Label label8;
		private TextBox textBox1;

		protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Text = "Hello, world!";
			this.Width = 440;
			this.Height = 400;
			
			gbSwitch = new GroupBox();
			gbSwitch.Location = new Point(5, 5);
			gbSwitch.Width = this.Width - 25;
			gbSwitch.Height = 35;
			Controls.Add(gbSwitch);
			
			rbEnUs = new RadioButton();
			rbEnUs.Text = "en-US";
			rbEnUs.Location = new Point(15, 5);
			rbEnUs.AutoSize = true;
			rbEnUs.Click += OnLocaleChanged;
			gbSwitch.Controls.Add(rbEnUs);

			rbFrFr = new RadioButton();
			rbFrFr.Text = "fr-FR";
			rbFrFr.Location = new Point(150, 5);
			rbFrFr.AutoSize = true;
			rbFrFr.Click += OnLocaleChanged;
			gbSwitch.Controls.Add(rbFrFr);

			rbRuRu = new RadioButton();
			rbRuRu.Text = "ru-RU";
			rbRuRu.Location = new Point(280, 5);
			rbRuRu.AutoSize = true;
			rbRuRu.Click += OnLocaleChanged;
			gbSwitch.Controls.Add(rbRuRu);
			
			toolTip1 = new ToolTip(this.components);
			toolTip1.SetToolTip(rbEnUs, "Switch to English");
			toolTip1.SetToolTip(rbFrFr, "Switch to French");
			toolTip1.SetToolTip(rbRuRu, "Switch to Russian");

			label1 = new Label();
			label1.Name = "label1";
			label1.Location = new Point(10, 50);
			label1.Text = "Hello, world!";
			label1.AutoSize = true;
			Controls.Add(label1);

			label2 = new Label();
			label2.Name = "label2";
			label2.Location = new Point(10, 70);
			label2.AutoSize = true;
			Controls.Add(label2);

			gbForms = new GroupBox();
			gbForms.Location = new Point(10, 105);
			gbForms.Width = this.Width - 25;
			gbForms.Height = 70;
			Controls.Add(gbForms);

			label3 = new Label();
			label3.Name = "label3";
			label3.Location = new Point(5, 5);
			label3.AutoSize = true;
			gbForms.Controls.Add(label3);

			label4 = new Label();
			label4.Name = "label4";
			label4.Location = new Point(5, 25);
			label4.AutoSize = true;
			gbForms.Controls.Add(label4);

			label5 = new Label();
			label5.Name = "label5";
			label5.Location = new Point(5, 45);
			label5.AutoSize = true;
			gbForms.Controls.Add(label5);

			label6 = new Label();
			label6.Name = "label6";
			label6.Location = new Point(10, 180);
			label6.AutoSize = true;
			Controls.Add(label6);

			label7 = new Label();
			label7.Name = "label7";
			label7.Location = new Point(10, 200);
			label7.AutoSize = true;
			Controls.Add(label7);

			label8 = new Label();
			label8.Name = "label8";
			label8.Location = new Point(10, 220);
			label8.AutoSize = true;
			Controls.Add(label8);

			textBox1 = new TextBox();
			textBox1.Name = "textBox1";
			textBox1.Location = new Point(10, 250);
			textBox1.Multiline = true;
			textBox1.AutoSize = false;
			textBox1.ReadOnly = true;
			textBox1.Width = 420;
			textBox1.Height = 80;
			textBox1.Text = "Here is an example of how one might continue a very long string\nfor the common case the string represents multi-line output.\n";
			Controls.Add(textBox1);
		}
		#endregion
    }
}
