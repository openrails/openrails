// COPYRIGHT 2012 by the Open Rails project.
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

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Packaging;  // Needs Project > Add reference > .NET > Component name = WindowsBase
using System.Windows.Forms;
using GNU.Gettext;
using GNU.Gettext.WinForms;
using ORTS.Settings;

namespace ORTS
{
    public partial class ImportExportSaveForm : Form
    {
        readonly ResumeForm.Save Save;
        const string SavePackFileExtension = "ORSavePack";  // Includes "OR" in the extension as this may be emailed, downloaded and mixed in with non-OR files.

        GettextResourceManager catalog = new GettextResourceManager("Menu");

        public ImportExportSaveForm(ResumeForm.Save save)
        {
            InitializeComponent();  // Needed so that setting StartPosition = CenterParent is respected.

            Localizer.Localize(this, catalog);

            // Windows 2000 and XP should use 8.25pt Tahoma, while Windows
            // Vista and later should use 9pt "Segoe UI". We'll use the
            // Message Box font to allow for user-customizations, though.
            Font = SystemFonts.MessageBoxFont;

            Save = save;
            if (!Directory.Exists(UserSettings.SavePackFolder)) Directory.CreateDirectory(UserSettings.SavePackFolder);
            UpdateFileList(null);
            bExport.Enabled = !(Save == null);
            ofdImportSave.Filter = Application.ProductName + catalog.GetString("Save Packs") + " (*." + SavePackFileExtension + ")|*." + SavePackFileExtension + "|" + catalog.GetString("All files") + " (*.*)|*";
        }

        #region Event handlers
        private void bImportSave_Click_1(object sender, EventArgs e)
        {
            // Show the dialog and get result.
            ofdImportSave.InitialDirectory = UserSettings.SavePackFolder;
            if (ofdImportSave.ShowDialog() == DialogResult.OK)
            {
                ExtractFilesFromZip(ofdImportSave.FileName, UserSettings.UserDataFolder);
                UpdateFileList(catalog.GetStringFmt("Save Pack '{0}' imported successfully.", Path.GetFileNameWithoutExtension(ofdImportSave.FileName)));
            }
        }

        private void bExport_Click(object sender, EventArgs e)
        {
            // Create a Zip-compatible file/compressed folder containing:
            // all files with the same stem (i.e. *.save, *.png, *.replay, *.txt)

            // For Zip, see http://weblogs.asp.net/jgalloway/archive/2007/10/25/creating-zip-archives-in-net-without-an-external-library-like-sharpziplib.aspx

            // Copy files to new package in folder save_packs
            var fullFilePath = Path.Combine(UserSettings.UserDataFolder, Save.File);
            var toFile = Path.GetFileNameWithoutExtension(Save.File) + "." + SavePackFileExtension;
            var fullZipFilePath = Path.Combine(UserSettings.SavePackFolder, toFile);
            foreach (var fileName in new[] {
                fullFilePath,
                Path.ChangeExtension(fullFilePath, "png"),
                Path.ChangeExtension(fullFilePath, "replay"),
                Path.ChangeExtension(fullFilePath, "txt"),
            })
            {
                AddFileToZip(fullZipFilePath, fileName);
            }
            UpdateFileList(catalog.GetStringFmt("Save Pack '{0}' exported successfully.", Path.GetFileNameWithoutExtension(Save.File)));
        }

        private void bViewSavePacksFolder_Click(object sender, EventArgs e)
        {
            var objPSI = new System.Diagnostics.ProcessStartInfo();
            var winDir = Environment.GetEnvironmentVariable("windir");
            objPSI.FileName = winDir + @"\explorer.exe";
            objPSI.Arguments = "\"" + UserSettings.SavePackFolder + "\""; // Opens the Save Packs folder
            if (Save != null)
            {
                var toFile = Path.GetFileNameWithoutExtension(Save.File) + "." + SavePackFileExtension;
                var fullZipFilePath = Path.Combine(UserSettings.SavePackFolder, toFile);
                if (File.Exists(fullZipFilePath))
                {
                    objPSI.Arguments = "/select,\"" + fullZipFilePath + "\""; // Opens the Save Packs folder and selects the exported SavePack
                }
            }
            Process.Start(objPSI);
        }
        #endregion

        void UpdateFileList(string message)
        {
            var files = Directory.GetFiles(UserSettings.SavePackFolder, "*." + SavePackFileExtension);
            textBoxSavePacks.Text = String.IsNullOrEmpty(message) ? "" : message + "\r\n";
            textBoxSavePacks.Text += catalog.GetPluralStringFmt("Save Pack folder contains {0} save pack:", "Save Pack folder contains {0} save packs:", files.Length);
            foreach (var s in files)
                textBoxSavePacks.Text += "\r\n    " + Path.GetFileNameWithoutExtension(s);
        }

        static void AddFileToZip(string zipFilename, string fileToAdd)
        {
            using (var zip = Package.Open(zipFilename, FileMode.OpenOrCreate))
            {
                var destFilename = @".\" + Path.GetFileName(fileToAdd);
                var uri = PackUriHelper.CreatePartUri(new Uri(destFilename, UriKind.Relative));
                if (zip.PartExists(uri))
                {
                    zip.DeletePart(uri);
                }
                var part = zip.CreatePart(uri, "", CompressionOption.Normal);
                using (var source = new FileStream(fileToAdd, FileMode.Open, FileAccess.Read))
                {
                    using (var destination = part.GetStream())
                    {
                        CopyStream(source, destination);
                    }
                }
            }
        }


        static void ExtractFilesFromZip(string zipFilename, string path)
        {
            using (var zip = Package.Open(zipFilename, FileMode.Open, FileAccess.Read))
            {
                foreach (var part in zip.GetParts())
                {
                    var fileName = Path.Combine(path, part.Uri.ToString().TrimStart('/').Replace("%20", " "));
                    try
                    {
                        using (var destination = new FileStream(fileName, FileMode.Create))
                        {
                            using (var source = part.GetStream())
                            {
                                CopyStream(source, destination);
                            }
                        }
                    }
                    catch { } // Ignore attempts to copy to a destination file locked by another process as
                    // ResumeForm locks PNG of selected save.
                }
            }
        }

        static void CopyStream(Stream source, Stream target)
        {
            const int bufferSize = 0x1000;
            var buffer = new byte[bufferSize];
            var bytesRead = 0;
            while ((bytesRead = source.Read(buffer, 0, bufferSize)) > 0)
                target.Write(buffer, 0, bytesRead);
        }
    }
}
