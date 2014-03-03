// COPYRIGHT 2014 by the Open Rails project.
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Ionic.Zip;
using Newtonsoft.Json;
using ORTS.Common;
using ORTS.Settings;

namespace ORTS.Updater
{
    public class UpdateManager
    {
        public readonly string BasePath;
        readonly SettingsStore Settings;

        public Update LatestUpdate { get; private set; }
        public Exception LatestUpdateError { get; private set; }

        public Exception UpdateError { get; private set; }

        public UpdateManager(string basePath)
        {
            if (!Directory.Exists(basePath)) throw new ArgumentException("The specified path must be valid and exist as a directory.", "basePath");
            BasePath = basePath;
            // We force this to be a local INI file by passing null for the
            // registry key. Also, since GetSettingStore throws if the INI
            // file doesn't exist, we check that first.
            var updateSettingsFile = Path.Combine(basePath, "Updater.ini");
            if (File.Exists(updateSettingsFile))
                Settings = SettingsStore.GetSettingStore(updateSettingsFile, null, "Settings");
        }

        public void Check()
        {
            if (Settings == null)
                return;
            try
            {
                var client = new WebClient()
                {
                    CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.BypassCache),
                    Encoding = Encoding.UTF8,
                };
                var updateUri = new Uri((string)Settings.GetUserValue("URL"));
                var updateData = client.DownloadString(updateUri);
                LatestUpdate = JsonConvert.DeserializeObject<Update>(updateData);
                LatestUpdateError = null;
            }
            catch (Exception error)
            {
                LatestUpdate = null;
                LatestUpdateError = error;
                Trace.WriteLine(error);
            }
        }

        public void Update()
        {
            if (LatestUpdate == null) throw new InvalidOperationException("Cannot get update when no LatestUpdate exists.");
            try
            {
                TestUpdateWrites();
                CleanDirectories();
                DownloadUpdate();
                ExtractUpdate();
                ApplyUpdate();
            }
            catch (Exception error)
            {
                UpdateError = error;
                return;
            }
        }

        public void Clean()
        {
            try
            {
                CleanDirectories();
            }
            catch (Exception error)
            {
                UpdateError = error;
                return;
            }
        }

        string PathUpdateTest { get { return Path.Combine(BasePath, "UpdateTest"); } }
        string PathUpdateDirty { get { return Path.Combine(BasePath, "UpdateDirty"); } }
        string PathUpdateStage { get { return Path.Combine(BasePath, "UpdateStage"); } }
        string FileUpdateStage { get { return Path.Combine(PathUpdateStage, "Update.zip"); } }

        void TestUpdateWrites()
        {
            Directory.CreateDirectory(PathUpdateTest);
            Directory.Delete(PathUpdateTest, true);
        }

        void CleanDirectories()
        {
            if (Directory.Exists(PathUpdateDirty))
                Directory.Delete(PathUpdateDirty, true);

            if (Directory.Exists(PathUpdateStage))
                Directory.Delete(PathUpdateStage, true);
        }

        void DownloadUpdate()
        {
            if (!Directory.Exists(PathUpdateStage))
                Directory.CreateDirectory(PathUpdateStage);

            var updateUri = new Uri("http://james-ross.co.uk/projects/or/update?format=json");
            var uri = new Uri(updateUri, LatestUpdate.Url);
            var client = new WebClient();
            client.DownloadFile(uri, FileUpdateStage);
        }

        void ExtractUpdate()
        {
            using (var zip = ZipFile.Read(FileUpdateStage))
                zip.ExtractAll(PathUpdateStage, ExtractExistingFileAction.OverwriteSilently);

            File.Delete(FileUpdateStage);
        }

        void ApplyUpdate()
        {
            if (!Directory.Exists(PathUpdateDirty))
                Directory.CreateDirectory(PathUpdateDirty);

            foreach (var file in Directory.GetFiles(BasePath))
                File.Move(file, Path.Combine(PathUpdateDirty, Path.GetFileName(file)));

            foreach (var directory in Directory.GetDirectories(BasePath))
                if (!directory.Equals(PathUpdateDirty, StringComparison.OrdinalIgnoreCase) && !directory.Equals(PathUpdateStage, StringComparison.OrdinalIgnoreCase))
                    Directory.Move(directory, Path.Combine(PathUpdateDirty, Path.GetFileName(directory)));

            foreach (var file in Directory.GetFiles(PathUpdateStage))
                File.Move(file, Path.Combine(BasePath, Path.GetFileName(file)));

            foreach (var directory in Directory.GetDirectories(PathUpdateStage))
                Directory.Move(directory, Path.Combine(BasePath, Path.GetFileName(directory)));
        }
    }

    public class Update
    {
        [JsonProperty]
        public DateTime Date { get; private set; }

        [JsonProperty]
        public string Url { get; private set; }

        [JsonProperty]
        public string Version { get; private set; }
    }
}
