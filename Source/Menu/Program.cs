// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace Menu
{
    static class Program
    {
        [STAThread]  // required for use of the DirectoryBrowserDialog in the main form.
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();

            if (Debugger.IsAttached)
            {
                MainForm();
            }
            else
            {
                try
                {
                    MainForm();
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.ToString(), Application.ProductName + " " + VersionInfo.VersionOrBuild);
                }
            }
        }

        static void MainForm()
        {
            using (var MainForm = new MainForm())
            {
                while (MainForm.ShowDialog() == DialogResult.OK)
                {
                    var joinedParameters = MenuLauncher.JoinArguments(MenuLauncher.GetRunActivityArguments(MainForm));
                    if ((Control.ModifierKeys & Keys.Alt) == Keys.Alt)
                    {
                        Clipboard.SetText(joinedParameters);
                        MessageBox.Show(
                            "RunActivity.exe arguments have been copied to the clipboard:" +
                            $"\n\n{joinedParameters}\n\n" +
                            "This is a debugging aid. If you wanted to start the simulator instead, select Start without holding down the Alt key.");
                        continue;
                    }

                    var processStartInfo = new ProcessStartInfo()
                    {
                        FileName = MainForm.RunActivityProgram,
                        Arguments = joinedParameters,
                        WindowStyle = ProcessWindowStyle.Normal,
                        WorkingDirectory = ApplicationInfo.ProcessDirectory,
                    };
                    var process = Process.Start(processStartInfo);
                    process.WaitForExit();
                }
            }
        }
    }
}
