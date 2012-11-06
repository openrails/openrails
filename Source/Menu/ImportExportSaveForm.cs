using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;            // for Path
using System.IO.Packaging;  // Needs Project > Add reference > .NET > Component name = WindowsBase
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ORTS;    // needed for ActivitySave

namespace ORTS {
    public partial class ImportExportSaveForm : Form {
        readonly ResumeForm ResumeForm;
        readonly ResumeForm.Save Save;
        string savePackPath;
        const long BUFFER_SIZE = 4096;
        const string extension = "ORSavePack";  // Includes "OR" in the extension as this may be emailed, downloaded and mixed in with non-OR files.

        public ImportExportSaveForm( ResumeForm parent, ResumeForm.Save save) {
            ResumeForm = parent;
            Save = save;
            InitializeComponent();  // Needed so that setting StartPosition = CenterParent is respected.
            savePackPath = Path.Combine( Program.UserDataFolder, ResumeForm.MainForm.SPFolder );
            if( !Directory.Exists( savePackPath ) ) { Directory.CreateDirectory( savePackPath ); }
            cbEmptySavePacks.Checked = ResumeForm.MainForm.EmptySavePacksOnExit;
            UpdateFileList( savePackPath );
            bExport.Enabled = !(Save == null);
        }

        void UpdateFileList( string savePackPath ) {
            var files = GetSavePacks( savePackPath );
            textBoxSavePacks.Text = String.Format( "Folder {0} contains {1} save pack(s)", ResumeForm.MainForm.SPFolder, files.Length );
            foreach( var s in GetSavePacks( savePackPath ) ) {
                textBoxSavePacks.Text += "\r\n" + Path.GetFileNameWithoutExtension( s );
            }
        }

        string[] GetSavePacks(string savePackPath) {
            return Directory.GetFiles( savePackPath, "*." + extension );
        }

        private void bExport_Click( object sender, EventArgs e ) {
            // Create a Zip-compatible file/compressed folder containing:
            // all files with the same stem (i.e. *.save, *.png, *.replay, *.txt)

            // For Zip, see http://weblogs.asp.net/jgalloway/archive/2007/10/25/creating-zip-archives-in-net-without-an-external-library-like-sharpziplib.aspx

            // Copy files to new package in folder save_packs
            string fullFilePath = Path.Combine( Program.UserDataFolder, Save.File );
            var toFile = Path.GetFileNameWithoutExtension( Save.File ) + "." + extension;
            string fullZipFilePath = Path.Combine( savePackPath, toFile ); 
            foreach( var fileName in new[] { 
                    fullFilePath, 
                    Path.ChangeExtension( fullFilePath, "png" ),
                    Path.ChangeExtension( fullFilePath, "replay" ),
                    Path.ChangeExtension( fullFilePath, "txt" )
                } ) {
                AddFileToZip( fullZipFilePath, fileName );
            }
            UpdateFileList( savePackPath );
        }

        private void AddFileToZip( string zipFilename, string fileToAdd ) {
            using( Package zip = System.IO.Packaging.Package.Open( zipFilename, FileMode.OpenOrCreate ) ) {
                string destFilename = @".\" + Path.GetFileName( fileToAdd );
                Uri uri = PackUriHelper.CreatePartUri( new Uri( destFilename, UriKind.Relative ) );
                if( zip.PartExists( uri ) ) {
                    zip.DeletePart( uri );
                }
                PackagePart part = zip.CreatePart( uri, "", CompressionOption.Normal );
                using( FileStream fileStream = new FileStream( fileToAdd, FileMode.Open, FileAccess.Read ) ) {
                    using( Stream dest = part.GetStream() ) {
                        CopyStream( fileStream, dest );
                    }
                }
            }
        }

        private void bViewSavePacksFolder_Click( object sender, EventArgs e ) {
            System.Diagnostics.ProcessStartInfo objPSI = new System.Diagnostics.ProcessStartInfo();
            string winDir = Environment.GetEnvironmentVariable( "windir" );
            objPSI.FileName = winDir + @"\explorer.exe";
            objPSI.Arguments = "/select, \"" + savePackPath; // Opens the Open Rails folder and selects the SavePacks folder
            if( Save != null ) {
                var toFile = Path.GetFileNameWithoutExtension( Save.File ) + "." + extension;
                string fullZipFilePath = Path.Combine( savePackPath, toFile );
                if( File.Exists( fullZipFilePath ) ) {
                    objPSI.Arguments = "/select, \"" + fullZipFilePath; // Opens the SavePacks folder and selects the exported SavePack
                }
            }
            objPSI.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            objPSI.WorkingDirectory = savePackPath;
            System.Diagnostics.Process objProcess = System.Diagnostics.Process.Start( objPSI );
        }

        private void bImportSave_Click_1( object sender, EventArgs e ) {
            // Show the dialog and get result.
            ofdImportSave.InitialDirectory = savePackPath;
            ofdImportSave.Title = "Extract Save from an OR Save Package";
            DialogResult result = ofdImportSave.ShowDialog();
            if( result == DialogResult.OK ) {
                ExtractFilesFromZip( ofdImportSave.FileName, Program.UserDataFolder );
                textBoxSavePacks.Text = String.Format( "SavePack {0} imported successfully.", Path.GetFileNameWithoutExtension( ofdImportSave.FileName ) );
            }
        }

        private void ExtractFilesFromZip( string zipFilename, string toPath ) {
            using( Package zip = System.IO.Packaging.Package.Open( zipFilename, FileMode.Open, FileAccess.Read ) ) {
                foreach( var part in zip.GetParts() ) {
                    string toFile = Path.Combine( toPath, part.Uri.ToString().TrimStart( '/' ).Replace( "%20", " " ) );
                    try {
                        FileStream toStream = new FileStream( toFile, FileMode.Create );
                        CopyStream( part.GetStream(), toStream );
                    } catch {} // Ignore attempts to copy to a destination file locked by another process as
                                  // ResumeForm locks PNG of selected save.
                }
            }
        }

        private static void CopyStream( Stream source, Stream target ) {
            const int bufSize = 0x1000;
            byte[] buf = new byte[bufSize];
            int bytesRead = 0;
            while( (bytesRead = source.Read( buf, 0, bufSize )) > 0 )
                target.Write( buf, 0, bytesRead );
        }

        private void cbEmptySavePacks_CheckedChanged( object sender, EventArgs e ) {
            ResumeForm.MainForm.EmptySavePacksOnExit = cbEmptySavePacks.Checked;
        }
    }
}
