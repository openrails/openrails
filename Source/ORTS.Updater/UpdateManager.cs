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

using Ionic.Zip;
using Newtonsoft.Json;
using ORTS.Settings;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace ORTS.Updater
{
    public class UpdateManager
    {
        // The date on this is fairly arbitrary - it's only used in a calculation to round the DateTime up to the next TimeSpan period.
        readonly DateTime BaseDateTimeMidnightLocal = new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Local);

        public event EventHandler<ProgressChangedEventArgs> ApplyProgressChanged;

        readonly string BasePath;
        readonly string ProductName;
        readonly string ProductVersion;
        readonly UpdateSettings Settings;
        readonly UpdateState State;
        UpdateSettings Channel;
        bool Force;

        public string ChannelName { get; set; }
        public string ChangeLogLink { get { return Channel != null ? Channel.ChangeLogLink : null; } }
        public Update LastUpdate { get; private set; }
        public Exception LastCheckError { get; private set; }
        public Exception LastUpdateError { get; private set; }
        public bool UpdaterNeedsElevation { get; private set; }

        public UpdateManager(string basePath, string productName, string productVersion)
        {
            if (!Directory.Exists(basePath)) throw new ArgumentException("The specified path must be valid and exist as a directory.", "basePath");
            BasePath = basePath;
            ProductName = productName;
            ProductVersion = productVersion;
            try
            {
                Settings = new UpdateSettings();
                State = new UpdateState();
                Channel = new UpdateSettings(ChannelName = Settings.Channel);
            }
            catch (ArgumentException)
            {
                // Updater.ini doesn't exist. That's cool, we'll just disable updating.
            }

            // Check for elevation to update; elevation is needed if the update writes failed and the user is NOT an
            // Administrator. Weird cases (like no permissions on the directory for anyone) are not handled.
            try
            {
                TestUpdateWrites();
            }
            catch
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                UpdaterNeedsElevation = !principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public string[] GetChannels()
        {
            if (Channel == null)
                return new string[0];

            return Settings.GetChannels();
        }

        public void SetChannel(string channel)
        {
            if (Channel == null)
                throw new InvalidOperationException();

            // Switch channel and save the change.
            Settings.Channel = channel;
            Settings.Save();
            Channel = new UpdateSettings(ChannelName = Settings.Channel);

            // Do a forced update check because the cached update data is likely to only be valid for the old channel.
            Force = true;
        }

        public void Check()
        {
            // If there's no updater file or the update channel is not correctly configured, exit without error.
            if (Channel == null)
                return;

            try
            {
                // If we're not at the appropriate time for the next check (and we're not forced), we reconstruct the cached update/error and exit.
                if (DateTime.Now < State.NextCheck && !Force)
                {
                    LastUpdate = State.Update.Length > 0 ? JsonConvert.DeserializeObject<Update>(State.Update) : null;
                    LastCheckError = State.Update.Length > 0 || string.IsNullOrEmpty(Channel.URL) ? null : new InvalidDataException("Last update check failed.");
                    return;
                }

                // This updates the NextCheck time and clears the cached update/error.
                ResetCachedUpdate();

                if (string.IsNullOrEmpty(Channel.URL))
                {
                    // If there's no update URL, reset cached update/error.
                    LastUpdate = null;
                    LastCheckError = null;
                    return;
                }

                // Fetch the update URL (adding ?force=true if forced) and cache the update/error.
                var client = new WebClient()
                {
                    CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.BypassCache),
                    Encoding = Encoding.UTF8,
                };
                client.Headers[HttpRequestHeader.UserAgent] = GetUserAgent();
                var updateUri = new Uri(!Force ? Channel.URL : Channel.URL.Contains('?') ? Channel.URL + "&force=true" : Channel.URL + "?force=true");
                var updateData = client.DownloadString(updateUri);
                LastUpdate = JsonConvert.DeserializeObject<Update>(updateData);
                LastCheckError = null;

                CacheUpdate(updateData);
            }
            catch (Exception error)
            {
                // This could be a problem deserializing the LastUpdate or fetching/deserializing the new update. It doesn't really matter, we record an error.
                LastUpdate = null;
                LastCheckError = error;
                Trace.WriteLine(error);

                ResetCachedUpdate();
            }
        }

        public void Update(IntPtr parentWindowHandle)
        {
            if (LastUpdate == null) throw new InvalidOperationException("Cannot get update when no LatestUpdate exists.");
            try
            {
                var processInfo = new ProcessStartInfo(FileUpdater, String.Format("/WAITPID={0}", Process.GetCurrentProcess().Id));
                if (UpdaterNeedsElevation)
                    processInfo.Verb = "runas";
                var process = Process.Start(processInfo);
                if (UpdaterNeedsElevation)
                    Thread.Sleep(1000);
                else
                    process.WaitForInputIdle();
                Environment.Exit(0);
            }
            catch (Exception error)
            {
                LastUpdateError = error;
                return;
            }
        }

        public void Apply()
        {
            TriggerApplyProgressChanged(0);
            try
            {
                TestUpdateWrites();
                TriggerApplyProgressChanged(1);

                CleanDirectories();
                TriggerApplyProgressChanged(2);

                DownloadUpdate(2, 65);
                TriggerApplyProgressChanged(67);

                ExtractUpdate(67, 30);
                TriggerApplyProgressChanged(97);

                if (UpdateIsReady())
                {
                    TriggerApplyProgressChanged(98);

                    VerifyUpdate();
                    TriggerApplyProgressChanged(99);

                    ApplyUpdate();
                    TriggerApplyProgressChanged(100);
                }

                LastUpdateError = null;
            }
            catch (Exception error)
            {
                LastUpdateError = error;
            }
            finally
            {
                try
                {
                    CleanDirectories();
                }
                catch { }
            }
        }

        void TriggerApplyProgressChanged(int progressPercentage)
        {
            var progressEvent = ApplyProgressChanged;
            if (progressEvent != null)
                progressEvent(this, new ProgressChangedEventArgs(progressPercentage, null));
        }

        string GetUserAgent()
        {
            return String.Format("{0}/{1}", ProductName, ProductVersion);
        }

        void ResetCachedUpdate()
        {
            State.LastCheck = DateTime.Now;
            // So what we're doing here is rounding up the DateTime (LastCheck) to the next TimeSpan (TTL) period. For
            // example, if the TTL was 1 hour, we'd round up the the start of the next hour. Similarly, if the TTL was
            // 1 day, we'd round up to midnight (the start of the next day). The purpose of this is to avoid 2 * TTL 
            // checking which might well occur if you always launch Open Rails around the same time of day each day -
            // if they launch it at 6:00PM on Monday, then 5:30PM on Tuesday, they won't get an update chech on
            // Tuesday. With the time rounding, they should get one check/day if the TTL is 1 day and they open it
            // every day. (This is why BaseDateTimeMidnightLocal uses the local midnight!)
            State.NextCheck = Channel.TTL.TotalMinutes > 1 ? BaseDateTimeMidnightLocal.AddSeconds(Math.Ceiling((State.LastCheck - BaseDateTimeMidnightLocal).TotalSeconds / Channel.TTL.TotalSeconds) * Channel.TTL.TotalSeconds) : State.LastCheck + TimeSpan.FromMinutes(1);
            State.Update = "";
            State.Save();
        }

        void CacheUpdate(string updateData)
        {
            Force = false;
            State.Update = updateData;
            State.Save();
        }

        string PathUpdateTest { get { return Path.Combine(BasePath, "UpdateTest"); } }
        string PathUpdateTemp { get { return Path.Combine(Path.GetTempPath(), "Open Rails"); } }
        string PathUpdateDirty { get { return Path.Combine(PathUpdateTemp, "UpdateDirty"); } }
        string PathUpdateStage { get { return Path.Combine(PathUpdateTemp, "UpdateStage"); } }
        string FileUpdateStage { get { return Path.Combine(PathUpdateStage, "Update.zip"); } }
        string FileUpdateStageIsReady { get { return Path.Combine(PathUpdateStage, "OpenRails.exe"); } }
        string FileSettings { get { return Path.Combine(BasePath, "OpenRails.ini"); } }
        string FileUpdater { get { return Path.Combine(BasePath, "Updater.exe"); } }

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

        void DownloadUpdate(int progressMin, int progressLength)
        {
            if (!Directory.Exists(PathUpdateStage))
                Directory.CreateDirectory(PathUpdateStage);

            var updateUri = new Uri(Channel.URL);
            var uri = new Uri(updateUri, LastUpdate.Url);
            var client = new WebClient();
            AsyncCompletedEventArgs done = null;
            client.DownloadProgressChanged += (object sender, DownloadProgressChangedEventArgs e) =>
            {
                try
                {
                    TriggerApplyProgressChanged(progressMin + progressLength * e.ProgressPercentage / 100);
                }
                catch (Exception error)
                {
                    done = new AsyncCompletedEventArgs(error, false, e.UserState);
                }
            };
            client.DownloadFileCompleted += (object sender, AsyncCompletedEventArgs e) =>
            {
                done = e;
            };
            client.Headers[HttpRequestHeader.UserAgent] = GetUserAgent();

            client.DownloadFileAsync(uri, FileUpdateStage);
            while (done == null)
            {
                Thread.Sleep(100);
            }
            if (done.Error != null)
                throw done.Error;

            TriggerApplyProgressChanged(progressMin + progressLength);
        }

        void ExtractUpdate(int progressMin, int progressLength)
        {
            using (var zip = ZipFile.Read(FileUpdateStage))
            {
                zip.ExtractProgress += (object sender, ExtractProgressEventArgs e) =>
                {
                    if (e.EventType == ZipProgressEventType.Extracting_BeforeExtractEntry)
                        TriggerApplyProgressChanged(progressMin + progressLength * e.EntriesExtracted / e.EntriesTotal);
                };
                zip.ExtractAll(PathUpdateStage, ExtractExistingFileAction.OverwriteSilently);
            }

            File.Delete(FileUpdateStage);

            TriggerApplyProgressChanged(progressMin + progressLength);
        }

        bool UpdateIsReady()
        {
            // The staging directory must exist, contain OpenRails.exe (be ready) and NOT contain the update zip.
            return Directory.Exists(PathUpdateStage)
                && File.Exists(FileUpdateStageIsReady)
                && !File.Exists(FileUpdateStage);
        }

        void VerifyUpdate()
        {
            var files = Directory.GetFiles(PathUpdateStage, "*", SearchOption.AllDirectories);

            var expectedSubject = "";
            try
            {
                var currentCertificate = new X509Certificate2(FileUpdater);
                expectedSubject = currentCertificate.Subject;
            }
            catch (CryptographicException)
            {
                // No signature on the updater, so we can't verify the update. :(
                return;
            }

            for (var i = 0; i < files.Length; i++)
            {
                if (files[i].EndsWith(".cpl", StringComparison.OrdinalIgnoreCase) ||
                    files[i].EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                    files[i].EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                    files[i].EndsWith(".ocx", StringComparison.OrdinalIgnoreCase) ||
                    files[i].EndsWith(".sys", StringComparison.OrdinalIgnoreCase))
                {
                    var certificate = new X509Certificate2(files[i]);
                    if (certificate.Subject != expectedSubject)
                        throw new InvalidDataException("Cryptographic signatures don't match. Expected " + expectedSubject + "; got " + certificate.Subject);
                }
            }

            return;
        }

        void ApplyUpdate()
        {
            var basePathFiles = Directory.GetFiles(BasePath).Where(file => !file.Equals(FileSettings, StringComparison.OrdinalIgnoreCase)).ToArray();
            var basePathDirectories = Directory.GetDirectories(BasePath).Where(directory => !directory.Equals(PathUpdateDirty, StringComparison.OrdinalIgnoreCase) && !directory.Equals(PathUpdateStage, StringComparison.OrdinalIgnoreCase)).ToArray();
            var updateStageFiles = Directory.GetFiles(PathUpdateStage);
            var updateStageDirectories = Directory.GetDirectories(PathUpdateStage);

            // Create dirty directory for old version.
            if (!Directory.Exists(PathUpdateDirty))
                Directory.CreateDirectory(PathUpdateDirty);

            // Copy (almost) all files from current version to dirty.
            foreach (var file in basePathFiles)
                File.Move(file, Path.Combine(PathUpdateDirty, Path.GetFileName(file)));

            // Copy (almost) all directories from current version to dirty.
            foreach (var directory in basePathDirectories)
                Directory.Move(directory, Path.Combine(PathUpdateDirty, Path.GetFileName(directory)));

            // Copy all files from new version to base path.
            foreach (var file in updateStageFiles)
                File.Move(file, Path.Combine(BasePath, Path.GetFileName(file)));

            // Copy all directories from new version to base path.
            foreach (var directory in updateStageDirectories)
                Directory.Move(directory, Path.Combine(BasePath, Path.GetFileName(directory)));

            // Forcing a save of the state adds back this information to the new "Updater.ini" file, without overwriting the new updater settings.
            State.Save();
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
