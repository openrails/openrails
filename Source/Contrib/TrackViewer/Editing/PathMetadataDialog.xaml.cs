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
    public partial class PathMetadataDialog : Window
    {
        public PathMetadataDialog(string[] metadata)
        {
            InitializeComponent();
            this.Left = 100;
            this.Top = 10;
            pathID.Text    = metadata[0];
            pathName.Text  = metadata[1];
            pathStart.Text = metadata[2];
            pathEnd.Text   = metadata[3];
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        public string[] GetMetadata()
        {
            string[] metadata = { pathID.Text, pathName.Text, pathStart.Text, pathEnd.Text };
            return metadata;
        }
 
    }
}
