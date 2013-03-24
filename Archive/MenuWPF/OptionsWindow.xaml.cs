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
using System.Text.RegularExpressions;
using System.IO;
using MSTS;

namespace MenuWPF
{
	/// <summary>
	/// Interaction logic for OptionsWindow.xaml
	/// </summary>
	public partial class OptionsWindow : Window
    {
        #region Members & Constructor

        private string regKey;
        private string foldersFile;
        private List<MenuWPF.MainWindow.Folder> Folders;
        private bool folderschanged = false;

        public bool FoldersChanged
        {
            get
            {
                return folderschanged;
            }
        }


		public OptionsWindow(string registryKey, string foldersFile, int indexTab)
		{
			this.InitializeComponent();

            this.sliderWOD.Value = 10;
            this.sliderSound.Value = 5;
            this.cboResolution.Text = "1024x768";
			this.txtBrakePipe.Text = "21";
			this.avatarURL.Text = "http://www.openrails.org/images/ICONmediumOD.jpg";
			this.regKey = registryKey;
            this.foldersFile = foldersFile;

            // Restore retained settings
            using (var RK = Registry.CurrentUser.OpenSubKey(registryKey))
            {
                if (RK != null)
                {
                    this.sliderWOD.Value = (int)RK.GetValue("WorldObjectDensity", (int)sliderWOD.Value);
                    this.sliderSound.Value = (int)RK.GetValue("SoundDetailLevel", (int)sliderSound.Value);
                    this.cboResolution.Text = (string)RK.GetValue("WindowSize", (string)cboResolution.Text);
                    this.chkAlerter.IsChecked = (1 == (int)RK.GetValue("Alerter", 0));
                    this.chkTrainLights.IsChecked = (1 == (int)RK.GetValue("TrainLights", 0));
                    this.chkPrecipitation.IsChecked = (1 == (int)RK.GetValue("Precipitation", 0));
                    this.chkOverheadWire.IsChecked = (1 == (int)RK.GetValue("Wire", 0));
                    this.txtBrakePipe.Text = RK.GetValue("BrakePipeChargingRate", txtBrakePipe.Text).ToString();
                    this.chkGraduated.IsChecked = (1 == (int)RK.GetValue("GraduatedRelease", 0));
                    this.chkDinamicShadows.IsChecked = (1 == (int)RK.GetValue("DynamicShadows", 0));
                    this.chkUseGlass.IsChecked = (1 == (int)RK.GetValue("WindowGlass", 0));
                    this.chkUseMSTSbin.IsChecked = (1 == (int)RK.GetValue("MSTSBINSound", 0));
                    this.chkFullScreen.IsChecked = (int)RK.GetValue("Fullscreen", 0) == 1 ? true : false;
                    this.chkWarningLog.IsChecked = (int)RK.GetValue("Logging", 1) == 1 ? true : false;
                    this.txtBgImage.Text = RK.GetValue("BackgroundImage", txtBgImage.Text).ToString();
					this.chkDispatcher.IsChecked = (1 == (int)RK.GetValue("ViewDispatcher", 0));
					this.textMPUpdate.Text = RK.GetValue("MPUpdateInterval", textMPUpdate.Text).ToString();
					this.showAvatar.IsChecked = (1 == (int)RK.GetValue("ShowAvatar", 0));
					this.avatarURL.Text = RK.GetValue("AvatarURL", this.avatarURL.Text).ToString();

				}

            }
            if (System.IO.File.Exists(txtBgImage.Text))
            {
                ((ImageBrush)this.Background).ImageSource = new BitmapImage(new Uri(txtBgImage.Text, UriKind.Absolute));
            }
            LoadFolders();
            tabOptions.SelectedIndex = indexTab;
        }
        #endregion

        #region Global Functions
        private void buttonCancel_Click(object sender, System.Windows.RoutedEventArgs e)
		{
            Close();
		}

		private void buttonOK_Click(object sender, System.Windows.RoutedEventArgs e)
		{
            if (MessageBox.Show("Are you sure you want to save the changes made?", "Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                //Check the values for resolution
                Regex reg = new Regex("^([0-1][0-9][0-9][0-9]|[0-9][0-9][0-9])x([0-1][0-9][0-9][0-9]|[0-9][0-9][0-9])$"); //Match a string format of WWWWxHHHH
                if (!reg.IsMatch(cboResolution.Text))
                {
                    MessageBox.Show("The resolution is not valid!\nPlease use the following format WidthxHeight", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                else
                {
                    int width = int.Parse(cboResolution.Text.Substring(0, cboResolution.Text.IndexOf('x')));
                    int height = int.Parse(cboResolution.Text.Substring(cboResolution.Text.IndexOf('x') + 1));
                    if ((width / 16) != (int)(width / 16) || (height / 16) != (int)(height / 16) || width < height)
                    {
                        MessageBox.Show("The resolution is not valid!\nThe values entered are not multiples of 16 or the width is lower than the height.", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                }
                //Check the values for the brake pipe
                int result = -1;
                int.TryParse(txtBrakePipe.Text, out result);
                if (result < 0)
                {
                    MessageBox.Show("The value for the brake pipe charging rate is not valid!\nMust be an integer greater than 0.", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

				//Check the values for the MP Update Frequency
				result = -1;
				int.TryParse(textMPUpdate.Text, out result);
				if (result < 0)
				{
					MessageBox.Show("The Multiplayer Update Interval should be numbers in seconds.", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
					return;
				}

                // Retain settings for convenience
                using (var RK = Registry.CurrentUser.CreateSubKey(regKey))
                {
                    RK.SetValue("WorldObjectDensity", (int)this.sliderWOD.Value);
                    RK.SetValue("SoundDetailLevel", (int)this.sliderSound.Value);
                    RK.SetValue("WindowSize", (string)this.cboResolution.Text);
                    RK.SetValue("Alerter", this.chkAlerter.IsChecked.Value ? 1 : 0);
                    RK.SetValue("TrainLights", this.chkTrainLights.IsChecked.Value ? 1 : 0);
                    RK.SetValue("Precipitation", this.chkPrecipitation.IsChecked.Value ? 1 : 0);
                    RK.SetValue("Wire", this.chkOverheadWire.IsChecked.Value ? 1 : 0);
                    RK.SetValue("BrakePipeChargingRate", (int)double.Parse(txtBrakePipe.Text));
                    RK.SetValue("GraduatedRelease", this.chkGraduated.IsChecked.Value ? 1 : 0);
                    RK.SetValue("DynamicShadows", this.chkDinamicShadows.IsChecked.Value ? 1 : 0);
                    RK.SetValue("WindowGlass", this.chkUseGlass.IsChecked.Value ? 1 : 0);
                    RK.SetValue("MSTSBINSound", this.chkUseMSTSbin.IsChecked.Value ? 1 : 0);
                    RK.SetValue("Fullscreen", this.chkFullScreen.IsChecked.Value ? 1 : 0);
                    RK.SetValue("Logging", this.chkWarningLog.IsChecked.Value ? 1 : 0);
                    RK.SetValue("BackgroundImage", this.txtBgImage.Text);
					RK.SetValue("ViewDispatcher", this.chkDispatcher.IsChecked.Value ? 1 : 0);
					RK.SetValue("MPUpdateInterval", (int)double.Parse(textMPUpdate.Text));
					RK.SetValue("ShowAvatar", this.showAvatar.IsChecked.Value ? 1 : 0);
					RK.SetValue("AvatarURL", this.avatarURL.Text);

                }
                SaveFolders();
                Close();
            }
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
        #endregion

        #region Simulation

        
        private void sliderWOD_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            lblWDOValue.Content = ((int)sliderWOD.Value).ToString();
        }

        #endregion

        #region Sounds

        private void sliderSound1_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            lblValue.Content = ((int)sliderSound.Value).ToString();
        }

        #endregion

        #region Video

        private void btnBrowseImage_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image Files (*.bmp, *.jpg, *.png, *.gif)|*.bmp;*.jpg;*.png;*.gif";
            ofd.Title = "Select background image";
            if (ofd.ShowDialog() == true)
            {
                txtBgImage.Text = ofd.FileName;
            }
        }

        private void btnResetImage_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            txtBgImage.Text = "";
        }

        #endregion

        #region Folders

        private void btnAddFolder_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            AddFolderWindow winAddFolder = new AddFolderWindow();

            
            winAddFolder.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            if (winAddFolder.ShowDialog() == true)
            {
                var fold = from f in Folders
                           where f.Name == winAddFolder.FolderName
                           select f;
                if (fold.Count() == 0)
                {
                    Folders.Add(new MainWindow.Folder(winAddFolder.FolderName, winAddFolder.FolderPath));
                    listBoxFolders.Items.Add(winAddFolder.FolderName);
                    folderschanged = true;
                }
                else
                {
                    MessageBox.Show("A folder with that name already exists!", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }

        private void btnRemoveFolder_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (listBoxFolders.SelectedItem != null)
            {
                Folders.RemoveAt(listBoxFolders.SelectedIndex);
                listBoxFolders.Items.RemoveAt(listBoxFolders.SelectedIndex);
                folderschanged = true;
            }
        }

        private void LoadFolders()
        {
            Folders = new List<MenuWPF.MainWindow.Folder>();


            if (File.Exists(foldersFile))
            {
                try
                {
                    using (var inf = new BinaryReader(File.Open(foldersFile, FileMode.Open)))
                    {
                        var count = inf.ReadInt32();
                        for (var i = 0; i < count; ++i)
                        {
                            var path = inf.ReadString();
                            var name = inf.ReadString();
                            Folders.Add(new MenuWPF.MainWindow.Folder(name, path));
                            listBoxFolders.Items.Add(name);
                        }
                    }
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            if (Folders.Count == 0)
            {
                try
                {
                    Folders.Add(new MenuWPF.MainWindow.Folder("- Default -", MSTSPath.Base()));
                    listBoxFolders.Items.Add("- Default -");
                }
                catch (Exception)
                {
                    MessageBox.Show("Microsoft Train Simulator doesn't appear to be installed.\nClick on 'Add...' to point Open Rails at your Microsoft Train Simulator folder.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            Folders = Folders.OrderBy(f => f.Name).ToList();


        }

        //================================================================================
        private void SaveFolders()
        {
            using (BinaryWriter outf = new BinaryWriter(File.Open(foldersFile, FileMode.Create)))
            {
                outf.Write(listBoxFolders.Items.Count);
                foreach (var folder in Folders)
                {
                    outf.Write(folder.Path);
                    outf.Write(folder.Name);
                }
            }
        }

        

        #endregion

    }
}