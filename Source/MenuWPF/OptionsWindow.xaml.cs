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
using Microsoft.Win32;
using Menu;

namespace MenuWPF
{
	/// <summary>
	/// Interaction logic for OptionsWindow.xaml
	/// </summary>
	public partial class OptionsWindow : Window
	{
		public OptionsWindow(string registryKey)
		{
			this.InitializeComponent();

            //this.numericWorldObjectDensity.Value = 10;
            //this.numericSoundDetailLevel.Value = 5;
            //this.comboBoxWindowSize.Items.AddRange(strContents);
            //this.comboBoxWindowSize.Text = "1024x768";
            //this.numericBrakePipeChargingRatePSIpS.Value = 21;



            // Restore retained settings
            RegistryKey RK = Registry.CurrentUser.OpenSubKey(registryKey);
            if (RK != null)
            {
                //this.numericWorldObjectDensity.Value = (int)RK.GetValue("WorldObjectDensity", (int)numericWorldObjectDensity.Value);
                //this.numericSoundDetailLevel.Value = (int)RK.GetValue("SoundDetailLevel", (int)numericSoundDetailLevel.Value);
                //this.comboBoxWindowSize.Text = (string)RK.GetValue("WindowSize", (string)comboBoxWindowSize.Text);
                //this.checkBoxTrainLights.Checked = (1 == (int)RK.GetValue("TrainLights", 0));
                //this.checkBoxPrecipitation.Checked = (1 == (int)RK.GetValue("Precipitation", 0));
                //this.checkBoxWire.Checked = (1 == (int)RK.GetValue("Wire", 0));
                //this.numericBrakePipeChargingRatePSIpS.Value = (int)RK.GetValue("BrakePipeChargingRate", (int)numericBrakePipeChargingRatePSIpS.Value);
                //this.checkBoxGraduatedRelease.Checked = (1 == (int)RK.GetValue("GraduatedRelease", 0));
                //this.checkBoxShadows.Checked = (1 == (int)RK.GetValue("DynamicShadows", 0));
                //this.checkBoxWindowGlass.Checked = (1 == (int)RK.GetValue("WindowGlass", 0));
                //this.checkBoxBINSound.Checked = (1 == (int)RK.GetValue("MSTSBINSound", 0));
            }
		}

		private void buttonCancel_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			
		}

		private void buttonOK_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			
		}
	}
}