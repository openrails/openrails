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
using ORTS.Orge.Drawing;

namespace ORTS.Orge.UserInterface
{
    /// <summary>
    /// Interaction logic for OtherPathsWindow.xaml
    /// The window allows to select an arbitrary number of paths that will be drawn on the tracks.
    /// </summary>
    public sealed partial class OtherPathsWindow : Window
    {
        /// <summary>The DrawMultiplePaths that contains the multiple paths from which a user can make a selection</summary>
        private DrawMultiplePaths multiPaths;
        /// <summary>While setting IsChecked programmatically, prevent callbacks on changed to do something</summary>
        private bool NoClickAction;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="drawMultiPaths">The object containing the information on the available and selected paths</param>
        public OtherPathsWindow(DrawMultiplePaths drawMultiPaths)
        {
            InitializeComponent();
            this.multiPaths = drawMultiPaths;
            this.listOfPaths.Items.Clear();

            string[] pathNames = multiPaths.PathNames();
            foreach (string pathName in pathNames)
            {
                CheckBox checkBox = new CheckBox
                {
                    Content = pathName
                };
                checkBox.Click += new RoutedEventHandler(CheckBox_Click);
                listOfPaths.Items.Add(checkBox);
            }

            RecolorAll();
        }

        private static Color? ConvertXnaColorToMediaColor(Microsoft.Xna.Framework.Color? originalColor)
        {
            if (originalColor == null)
            {
                return null;
            }
            return Color.FromArgb(originalColor.Value.A, originalColor.Value.R, originalColor.Value.G, originalColor.Value.B);
        }

        /// <summary>
        /// User has selected to clear all paths.
        /// </summary>
        private void ClearOtherPathsButton_Click(object sender, RoutedEventArgs e)
        {
            multiPaths.ClearAll();
            RecolorAll();
        }

        /// <summary>
        /// For all available paths, set the color as well as the checkmark, based on the 
        /// state in the DrawMultiPaths. Basically should be called whenever the state changes
        /// </summary>
        private void RecolorAll()
        {
            NoClickAction = true;
            foreach (var item in listOfPaths.Items)
            {
                CheckBox checkBox = item as CheckBox;
                if (checkBox != null)
                {
                    string pathName = (string)checkBox.Content;
                    System.Windows.Media.Color? backgroundColor = ConvertXnaColorToMediaColor(multiPaths.ColorOf(pathName));
                    if (backgroundColor == null)
                    {
                        checkBox.IsChecked = false;
                        checkBox.Foreground = new SolidColorBrush(Color.FromArgb(255,0,0,0));
                    }
                    else
                    {
                        checkBox.IsChecked = true;
                        checkBox.Foreground = new SolidColorBrush(backgroundColor.Value);
                    }
                }
            }
            NoClickAction = false;
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (NoClickAction) return;
            CheckBox selectedCheckBox = (CheckBox)sender;
            string value = (string)selectedCheckBox.Content;
            multiPaths.SetSelection(value, selectedCheckBox.IsChecked.Value);
            RecolorAll();
        }

        private void AlwaysOnTop_Click(object sender, RoutedEventArgs e)
        {
            CheckBox alwaysOnTop = (CheckBox)sender;
            this.Topmost = alwaysOnTop.IsChecked.Value;
        }
    }
}
