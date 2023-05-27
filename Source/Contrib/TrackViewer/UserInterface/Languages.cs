// COPYRIGHT 2014, 2018 by the Open Rails project.
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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ORTS.TrackViewer.UserInterface
{
    /// <summary>
    /// Class to store the combination of a language code ("de", "en", ...) with a human-readable name ("Deutsch", "English")
    /// </summary>
    public class Language
    {
        /// <summary>The standardized two-letter code for the language</summary>
        public string Code { get; set; }
        /// <summary>The name for the language to present to the user</summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// Class to manage the available languages. Find the supported languages. Set a language. Create lists of languages for the menus
    /// </summary>
    public class LanguageManager
    {
        /// <summary>Gives a list of the available languages</summary>
        public IEnumerable<Language> Languages { get; private set; }

        /// <summary>Returns the code for the current language</summary>
        public static string CurrentLanguageCode { get { return ORTS.TrackViewer.Properties.Settings.Default.language; } }

        /// <summary>
        /// Constructor. This will also search for available languages and store these
        /// </summary>
        public LanguageManager()
        {
            // Collect all the available language codes by searching for
            // localisation files, but always include English (base language).
            List<string> languageCodes = new List<string> { "en" };
            foreach (var path in Directory.GetDirectories(Path.GetDirectoryName(Application.ExecutablePath)))
                if (Directory.GetFiles(path, "*.Messages.resources.dll").Length > 0)
                    languageCodes.Add(Path.GetFileName(path));

            // Turn the list of codes in to a list of code + name pairs for
            // displaying in the dropdown list.
            Languages =
                new[] { new Language { Code = "", Name = "System" } }
                .Union(languageCodes
                    .SelectMany(lc =>
                        {
                            try
                            {
                                return new[] { new Language { Code = lc, Name = CultureInfo.GetCultureInfo(lc).NativeName } };
                            }
                            catch (ArgumentException)
                            {
                                return new Language[0];
                            }
                        })
                    .OrderBy(l => l.Name)
                )
                .ToList();
        }

        /// <summary>
        /// Change/select the language. Store the preference to be used after a restart
        /// Give message to use that TrackViewer needs to be restarted
        /// </summary>
        /// <param name="languageCode">Two-letter code for the new language</param>
        public void SelectLanguage(string languageCode)
        {
            if (languageCode != CurrentLanguageCode)
            {
                MessageBox.Show(TrackViewer.catalog.GetString("Please restart TrackViewer in order to load the new language."));
                ORTS.TrackViewer.Properties.Settings.Default.language = languageCode;
                ORTS.TrackViewer.Properties.Settings.Default.Save();
            }
        }

        /// <summary>
        /// Load the language (from stored preferences). This needs to be called before any menu steup is done.
        /// </summary>
        public static void LoadLanguage()
        {
            string preferenceLanguageCode = ORTS.TrackViewer.Properties.Settings.Default.language;
            if (preferenceLanguageCode.Length > 0)
            {
                try
                {
                    System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo(preferenceLanguageCode);
                }
                catch { }
            }
        }
    }
}
