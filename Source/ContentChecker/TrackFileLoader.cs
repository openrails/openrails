// COPYRIGHT 2018 by the Open Rails project.
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
using System.IO;
using Orts.Formats.Msts;
using Path = System.IO.Path;

namespace ContentChecker
{
    /// <summary>
    /// Loader class for .trk files
    /// </summary>
    class TrackFileLoader : Loader
    {
        RouteFile TRK;
        string routePath;
        string basePath;

        /// <summary>
        /// Try to load the file.
        /// Possibly this might raise an exception. That exception is not caught here
        /// </summary>
        /// <param name="file">The file that needs to be loaded</param>
        public override void TryLoading(string file)
        {
            loadedFile = file;
            TRK = new RouteFile(file);
        }

        protected override void AddReferencedFiles()
        {
            routePath = Path.GetDirectoryName(loadedFile);
            basePath = Path.GetDirectoryName(Path.GetDirectoryName(routePath));
            string loadingScreen = Path.Combine(routePath, TRK.Tr_RouteFile.LoadingScreen);
            AddAdditionalFileAction.Invoke(loadingScreen, new AceLoader());
            string graphic = Path.Combine(routePath, TRK.Tr_RouteFile.Thumbnail);
            AddAdditionalFileAction.Invoke(graphic, new AceLoader());

            AddAdditionalSMS(TRK.Tr_RouteFile.DefaultCrossingSMS);
            AddAdditionalSMS(TRK.Tr_RouteFile.DefaultCoalTowerSMS);
            AddAdditionalSMS(TRK.Tr_RouteFile.DefaultDieselTowerSMS);
            AddAdditionalSMS(TRK.Tr_RouteFile.DefaultSignalSMS);
            AddAdditionalSMS(TRK.Tr_RouteFile.DefaultTurntableSMS);
            AddAdditionalSMS(TRK.Tr_RouteFile.DefaultWaterTowerSMS);

            foreach (SeasonType season in Enum.GetValues(typeof(SeasonType)))
            {
                foreach (WeatherType weather in Enum.GetValues(typeof(WeatherType)))
                {
                    string environmentFile = TRK.Tr_RouteFile.Environment.ENVFileName(season, weather);
                    string envFileFull = Path.Combine(Path.Combine(routePath, "ENVFILES"), environmentFile);
                    AddAdditionalFileAction.Invoke(envFileFull, new EnvironmentFileLoader());
                }
            }
        }

        void AddAdditionalSMS(string smsFileName)
        {
            if (smsFileName == null) { return; }
            string smsInRoute = Path.Combine(Path.Combine(routePath, "SOUND"), smsFileName);
            if (File.Exists(smsInRoute))
            {
                AddAdditionalFileAction.Invoke(smsInRoute, new SmsLoader());
            }
            else
            {
                string smsInBase = Path.Combine(Path.Combine(basePath, "SOUND"), smsFileName);
                AddAdditionalFileAction.Invoke(smsInBase, new SmsLoader());
            }
        }

        protected override void AddAllFiles()
        {
            AddMainFiles();
            AddAllActivities();
            AddAllTiles();
            AddAllPaths();
            AddAllWorldFiles();
        }

        void AddMainFiles()
        {

            string globalTsectionDat = Path.Combine(Path.Combine(basePath, "Global"), "tsection.dat");
            AddAdditionalFileAction.Invoke(globalTsectionDat, new TsectionGlobalLoader(routePath));

            AddAdditionalFileAction.Invoke(Path.Combine(routePath, TRK.Tr_RouteFile.FileName + ".tdb"), new TrackDataBaseLoader());
            AddAdditionalFileAction.Invoke(Path.Combine(routePath, TRK.Tr_RouteFile.FileName + ".rdb"), new RoadDataBaseLoader());
            AddAdditionalFileAction.Invoke(Path.Combine(routePath, "carspawn.dat"), new CarSpawnLoader());
            string ORfilepath = System.IO.Path.Combine(routePath, "OpenRails");
            if (File.Exists(ORfilepath + @"\sigcfg.dat"))
            {
                AddAdditionalFileAction.Invoke(Path.Combine(ORfilepath, "sigcfg.dat"), new SignalConfigLoader());
            }
            else
            {
                AddAdditionalFileAction.Invoke(Path.Combine(routePath, "sigcfg.dat"), new SignalConfigLoader());
            }
        }

        void AddAllActivities()
        {
            var activityPath = Path.Combine(routePath, "Activities");
            if (Directory.Exists(activityPath))
            {
                foreach (string activity in Directory.GetFiles(activityPath, "*.act"))
                {
                    AddAdditionalFileAction.Invoke(activity, new ActivityLoader());
                }
            }

            var activityPathOr = Path.Combine(Path.Combine(routePath, "Activities"), "OpenRails");
            if (Directory.Exists(activityPathOr))
            {
                List<string> timetableFiles = new List<string>();
                timetableFiles.AddRange(Directory.GetFiles(activityPathOr, "*.timetable_or"));
                timetableFiles.AddRange(Directory.GetFiles(activityPathOr, "*.timetable-or"));
                timetableFiles.AddRange(Directory.GetFiles(activityPathOr, "*.timetablelist_or"));
                timetableFiles.AddRange(Directory.GetFiles(activityPathOr, "*.timetablelist-or"));
                foreach (string timetableFile in timetableFiles)
                {
                    AddAdditionalFileAction.Invoke(timetableFile, new TimetableLoader());
                }
            }
        }

        void AddAllTiles()
        {
            var tiles = Path.Combine(routePath, "Tiles");
            if (Directory.Exists(tiles))
            {
                foreach (string tile in Directory.GetFiles(tiles, "*.t"))
                {
                    AddAdditionalFileAction.Invoke(tile, new TerrainLoader());
                }
            }

            var loTiles = Path.Combine(routePath, "Lo_Tiles");
            if (Directory.Exists(loTiles))
            {
                foreach (string tile in Directory.GetFiles(loTiles, "*.t"))
                {
                    AddAdditionalFileAction.Invoke(tile, new TerrainLoader());
                }
            }
        }

        void AddAllPaths()
        {
            var paths = Path.Combine(routePath, "Paths");
            if (Directory.Exists(paths))
            {
                foreach (string pat in Directory.GetFiles(paths, "*.pat"))
                {
                    AddAdditionalFileAction.Invoke(pat, new PathLoader());
                }
            }
        }

        void AddAllWorldFiles()
        {
            var world = Path.Combine(routePath, "World");
            if (Directory.Exists(world))
            {
                foreach (string pat in Directory.GetFiles(world, "*.w"))
                {
                    AddAdditionalFileAction.Invoke(pat, new WorldFileLoader());
                }
            }
        }
    }
}
