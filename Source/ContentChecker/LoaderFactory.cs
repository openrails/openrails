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

// This class has the responsibility to determine the right Loader subclass from a filename
// This means that from a filename, and possibly the SIMIS header of that file this class needs
// to determine which kind of file it is.
// The determination is based on:
// 1. The extension
// 2. The SIMIS header in case the extension is not unique
// 3. The filename in case also the SIMIS header is not unique, or the file does not have a SIMIS header

using System;
using System.IO;

namespace ContentChecker
{
    static class LoaderFactory
    {
        /// <summary>
        /// Get the default Loader object for a file, based on its extension or in some other way.
        /// Possible it is a not-supported loader
        /// </summary>
        /// <param name="file">The name of the file that needs to be loaded</param>
        /// <returns>The most appropriate subclass to load this file</returns>
        public static Loader GetLoader(string file)
        {
            String extension = Path.GetExtension(file).ToLowerInvariant();

            switch (extension)
            {
                case ".ace": return new AceLoader();
                case ".act": return new ActivityLoader();
                case ".asv": return new NotUsedLoader();  // activity save file, binary format
                case ".con": return new ConsistLoader();
                case ".cvf": return new CabviewLoader();
                case ".eng": return new EngineLoader();
                case ".env": return new EnvironmentFileLoader();
                case ".haz": return new HazardLoader();
                case ".pat": return new PathLoader();
                case ".rdb": return new RoadDataBaseLoader();
                case ".s": return new ShapeLoader();
                case ".sd": return new ShapeDescriptorLoader();
                case ".sms": return new SmsLoader();
                case ".srv": return new ServiceLoader();
                case ".t": return new TerrainLoader();
                case ".tdb": return new TrackDataBaseLoader();
                case ".trf": return new TrafficLoader();
                case ".trk": return new TrackFileLoader();
                case ".w": return new WorldFileLoader();
                case ".wag": return new WagonLoader();
                case ".wav": return new WavLoader();
                case ".ws": return new WorldSoundLoader();
                case ".timetable_or": return new TimetableLoader();
                case ".timetable-or": return new TimetableLoader();
                case ".timetablelist_or": return new TimetableLoader();
                case ".timetablelist-or": return new TimetableLoader();
            }

            if (extension == ".dat")
            {
                return GetDatLoader(file);
            }

            if (extension == ".raw")
            {
                return GetRawLoader(file);
            }

            return new NotRecognizedLoader();
        }

        /// <summary>
        /// Get the default Loader object for a .dat file. This can be many files
        /// </summary>
        /// <param name="file">The name of the file that needs to be loaded</param>
        /// <returns>The most appropriate subclass to load this file</returns>
        static Loader GetDatLoader(string file)
        {
            string filename = Path.GetFileName(file).ToLowerInvariant();
            // First try to determine the file from the SIMIS header
            using (var stf = new StreamReader(file, true))
            {
                string SimisSignature = stf.ReadLine();
                switch (SimisSignature)
                {
                    case "SIMISA@@@@@@@@@@JINX0C0t______":
                        return new NotUsedLoader(); // <install_directory>/camcfg.dat

                    case "SIMISA@@@@@@@@@@JINX0D0t______":
                        return new NotUsedLoader(); // in TD/td_idx.dat and TD/lo_td_idx.dat

                    case "SIMISA@@@@@@@@@@JINX0F0t______":
                        switch (filename)
                        {
                            case "forests.dat":
                                return new NotUsedLoader(); // forests.dat
                            default:
                                return new TsectionGlobalLoader();
                        }


                    case "SIMISA@@@@@@@@@@JINX0f1t______":
                        return new NotUsedLoader(); // GUI/gui_fnts.dat

                    case "SIMISA@@@@@@@@@@JINX0G0t______":
                        return new SignalConfigLoader();

                    case "SIMISA@@@@@@@@@@JINX0g0t______":
                        return new NotUsedLoader(); // gantry.dat

                    case "SIMISA@@@@@@@@@@JINX0p1t______":
                        return new NotUsedLoader(); // mdrivers.dat

                    case "SIMISA@@@@@@@@@@JINX0r1t______":
                        return new NotUsedLoader(); // telepole.dat

                    case "SIMISA@@@@@@@@@@JINX0S0t______":
                        return new NotUsedLoader(); // Global/soundcfg.dat

                    case "SIMISA@@@@@@@@@@JINX0T0t______":
                        return new TsectionLoader();

                    case "SIMISA@@@@@@@@@@JINX0t1t______":
                        switch (filename)
                        {
                            case "ttype.dat":
                                return new TrackTypeLoader();
                            case "speedpost.dat":
                                return new NotUsedLoader(); // speedpost.dat
                            default:
                                return new NotRecognizedLoader();
                        }

                    case "SIMISA@@@@@@@@@@JINX0v1t______":
                        return new CarSpawnLoader();

                    case "SIMISA@@@@@@@@@@JINX0w1t______":
                        return new NotUsedLoader(); // GUI/*

                    case "SIMISA@@@@@@@@@@JINX0Z1t______":
                        return new NotUsedLoader(); // ssource.dat

                    default:
                        if (SimisSignature.Contains("SIMIS"))
                        {
                            string fileName = Path.GetFileName(file);
                            Console.WriteLine();
                            Console.WriteLine($"  Simis header {SimisSignature} in {fileName} is not recognized. This probably is a bug. Please report");
                        }
                        break;
                }
            }

            // otherwise, try falling back on hard-coded names
            // Obviously this will fail probably quite a lot. Using one of the other script options is better
            switch (filename)
            {
                case "sigscr.dat":
                    return new SignalScriptLoader();
            }

            return new NotRecognizedLoader();
        }

        /// <summary>
        /// Get the default Loader object for a .raw file. This can be many files
        /// </summary>
        /// <param name="file">The name of the file that needs to be loaded</param>
        /// <returns>The most appropriate subclass to load this file</returns>
        static Loader GetRawLoader(string file)
        {
            string fileName = Path.GetFileName(file).ToLowerInvariant();
            string endswith = fileName.Substring(fileName.Length - 6);

            switch (endswith)
            {
                case "_y.raw":
                    return new TerrainAltitudeLoader();
                case "_f.raw":
                    return new TerrainFlagsLoader();
                case "_n.raw":
                    return new NotUsedLoader();
                case "_e.raw":
                    return new NotUsedLoader();
                default:
                    return new NotRecognizedLoader();
            }
        }

    }
}
