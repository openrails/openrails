using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Forms;

namespace MenuWPF
{
	/// <summary>
	/// Interaction logic for EngineInfoWindow.xaml
	/// </summary>
	public partial class ProgressionWindow : Window
	{
        public double MaxValue 
        {
            get
            {
                return progBar.Maximum;
            }
            set
            {
                progBar.Maximum = value;
            }
        }

        public ProgressionWindow()
		{
			this.InitializeComponent();
            progBar.Value = 0;
		}

        public void IncreaseBy(double value)
        {
            this.Dispatcher.Invoke(new IncreaseByDelegate(increase), value);
        }

        private void increase(double value)
        {
            progBar.Value += value;
        }

        public delegate void IncreaseByDelegate(double value);

        public void DoClose()
        {
            this.Dispatcher.Invoke(new DoCloseDelegate(doClose));
        }

        public delegate void DoCloseDelegate();

        private void doClose()
        {
            this.Close();
        }
	}
}