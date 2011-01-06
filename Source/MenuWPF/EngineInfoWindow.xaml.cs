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

namespace MenuWPF
{
	/// <summary>
	/// Interaction logic for EngineInfoWindow.xaml
	/// </summary>
	public partial class EngineInfoWindow : Window
	{
		public EngineInfoWindow(FlowDocument doc)
		{
			this.InitializeComponent();
            docEngineInfo.Document = doc;
			// Insert code required on object creation below this point.
		}
	}
}