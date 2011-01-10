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

namespace MenuWPF
{
	/// <summary>
	/// Interaction logic for OptionsWindow.xaml
	/// </summary>
	public partial class OptionsWindow : Window
	{
        string regKey;

		public OptionsWindow(string registryKey)
		{
			this.InitializeComponent();

            this.sliderWOD.Value = 10;
            this.sliderSound.Value = 5;
            this.cboResolution.Text = "1024x768";
            this.sliderBrakePipe.Value = 21;
            regKey = registryKey;


            // Restore retained settings
            RegistryKey RK = Registry.CurrentUser.OpenSubKey(registryKey);
            if (RK != null)
            {
                this.sliderWOD.Value = (int)RK.GetValue("WorldObjectDensity", (int)sliderWOD.Value);
                this.sliderSound.Value = (int)RK.GetValue("SoundDetailLevel", (int)sliderSound.Value);
                this.cboResolution.Text = (string)RK.GetValue("WindowSize", (string)cboResolution.Text);
                this.chkTrainLights.IsChecked = (1 == (int)RK.GetValue("TrainLights", 0));
                this.chkPrecipitation.IsChecked = (1 == (int)RK.GetValue("Precipitation", 0));
                this.chkOverheadWire.IsChecked = (1 == (int)RK.GetValue("Wire", 0));
                this.sliderBrakePipe.Value = (int)RK.GetValue("BrakePipeChargingRate", (int)sliderBrakePipe.Value);
                this.chkGraduated.IsChecked = (1 == (int)RK.GetValue("GraduatedRelease", 0));
                this.chkDinamicShadows.IsChecked = (1 == (int)RK.GetValue("DynamicShadows", 0));
                this.chkUseGlass.IsChecked = (1 == (int)RK.GetValue("WindowGlass", 0));
                this.chkUseMSTSbin.IsChecked = (1 == (int)RK.GetValue("MSTSBINSound", 0));
                this.chkFullScreen.IsChecked = (int)RK.GetValue("Fullscreen", 0) == 1 ? true : false;
                this.chkWarningLog.IsChecked = (int)RK.GetValue("Warnings", 1) == 1 ? true : false;
            }
		}

		private void buttonCancel_Click(object sender, System.Windows.RoutedEventArgs e)
		{
            Close();
		}

		private void buttonOK_Click(object sender, System.Windows.RoutedEventArgs e)
		{
            // Retain settings for convenience
            RegistryKey RK = Registry.CurrentUser.CreateSubKey(regKey);
            if (RK != null)
            {
                RK.SetValue("WorldObjectDensity", (int)this.sliderWOD.Value);
                RK.SetValue("SoundDetailLevel", (int)this.sliderSound.Value);
                RK.SetValue("WindowSize", (string)this.cboResolution.Text);
                RK.SetValue("TrainLights", this.chkTrainLights.IsChecked.Value ? 1 : 0);
                RK.SetValue("Precipitation", this.chkPrecipitation.IsChecked.Value ? 1 : 0);
                RK.SetValue("Wire", this.chkOverheadWire.IsChecked.Value ? 1 : 0);
                RK.SetValue("BrakePipeChargingRate", (int)this.sliderBrakePipe.Value);
                RK.SetValue("GraduatedRelease", this.chkGraduated.IsChecked.Value ? 1 : 0);
                RK.SetValue("DynamicShadows", this.chkDinamicShadows.IsChecked.Value ? 1 : 0);
                RK.SetValue("WindowGlass", this.chkUseGlass.IsChecked.Value ? 1 : 0);
                RK.SetValue("MSTSBINSound", this.chkUseMSTSbin.IsChecked.Value ? 1 : 0);
                RK.SetValue("Fullscreen", this.chkFullScreen.IsChecked.Value ? 1 : 0);
                RK.SetValue("Warnings", this.chkWarningLog.IsChecked.Value ? 1 : 0);
            }
            Close();
		}

        private void sliderBrakePipe_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Slider)sender).ToolTip = (int)((Slider)sender).Value;
        }

        private void imgLogo2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btnPrevious_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            tabOptions.SelectedIndex -= tabOptions.SelectedIndex == 0 ? 0 : 1; 
        }

        private void btnNext_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            tabOptions.SelectedIndex += tabOptions.SelectedIndex == tabOptions.Items.Count - 1 ? 0 : 1; 
        }
	}
}