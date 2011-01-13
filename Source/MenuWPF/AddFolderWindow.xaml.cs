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
	public partial class AddFolderWindow : Window
	{
        public string FolderPath { get; set; }
        public string FolderName { get; set; }

		public AddFolderWindow()
		{
			this.InitializeComponent();
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "Add MSTS content folder";
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtPath.Text = fbd.SelectedPath;
            }
		}

		private void btnBrowse_Click(object sender, System.Windows.RoutedEventArgs e)
		{
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "Add MSTS content folder";
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtPath.Text = fbd.SelectedPath;
            }
		}

		private void btnOK_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			if(String.IsNullOrEmpty(txtPath.Text) || String.IsNullOrEmpty(txtName.Text))
            {
                System.Windows.MessageBox.Show("Folder path or folder name cannot be empty!", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else
            {
                FolderPath = txtPath.Text;
                FolderName = txtName.Text;
                DialogResult = true;
            }
		}

		private void btnCancel_Click(object sender, System.Windows.RoutedEventArgs e)
		{
            DialogResult = false;
            Close();
		}


	}
}