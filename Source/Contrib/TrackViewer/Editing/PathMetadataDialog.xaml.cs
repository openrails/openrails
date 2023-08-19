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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ORTS.TrackViewer.Editing
{
    /// <summary>
    /// Interaction logic for PathMetadataDialog.xaml
    /// </summary>
    public sealed partial class PathMetadataDialog : Window
    {
        /// <summary>
        /// Create the dialog to edit the path metadata, and fill it with current values
        /// </summary>
        /// <param name="metadata">Array of string, containing ID, name, start and end</param>
        /// <param name="isPlayerPath">Is the path currently a player path?</param>
        public PathMetadataDialog(string[] metadata, bool isPlayerPath)
        {
            InitializeComponent();
            this.Left = 100;
            this.Top = 10;
            pathID.Text    = metadata[0];
            pathName.Text  = metadata[1];
            pathStart.Text = metadata[2];
            pathEnd.Text   = metadata[3];
            pathIsPlayerPath.IsChecked = isPlayerPath;
            pathID.Focus();
            pathID.SelectAll();
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        /// <summary>
        /// Return the meta data of the path (ID, name, start and end) as edited by the user.
        /// </summary>
        /// <returns>string array containing the meta data</returns>
        public string[] GetMetadata()
        {
            string[] metadata = { pathID.Text, pathName.Text, pathStart.Text, pathEnd.Text, pathIsPlayerPath.IsChecked.ToString() };
            return metadata;
        }

        private void PathX_GotFocus(object sender, RoutedEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }
    }
}
