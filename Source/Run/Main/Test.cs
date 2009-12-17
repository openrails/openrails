/// These classes are used to test and debug the various classes in the program.
/// They are not meant to be used in the production code.

/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MSTS;

namespace ORTS
{
    /// <summary>
    /// This class contains various debug and diagnostic code.
    /// </summary>
    class Test
    {
        public static string[] AllBaseFolders = new string[] { @"c:\program files\microsoft games\train simulator", @"c:\personal\mststest", @"c:\personal\msts" };
        /// <summary>
        /// Test load of all static consists in all routes.
        /// </summary>
        public static void SimulatorInitialization()
        {
            foreach (string routeFolder in Directory.GetDirectories(MSTSPath.Base() + @"\ROUTES"))
                foreach (string activityFile in Directory.GetFiles(routeFolder + @"\ACTIVITIES", "*.act"))
                {
                    Console.WriteLine(activityFile);
                    new Simulator(activityFile); 
                }
            Console.WriteLine("DONE");
            Console.ReadLine();
        }
        /// <summary>
        /// Test read of all activity files in all routes
        /// </summary>
        public static void ActivityFiles()
        {
            foreach (string routeFolder in Directory.GetDirectories(MSTSPath.Base() + @"\ROUTES"))
                foreach (string activityFile in Directory.GetFiles(routeFolder + @"\ACTIVITIES", "*.act"))
                {
                    Console.WriteLine(activityFile);
                    ACTFile actFile = new ACTFile(activityFile);
                }
            Console.ReadLine();
        }
        /// <summary>
        /// Test 
        /// </summary>
        public static void TileToFileConverter()
        {
            foreach (string routeFolder in Directory.GetDirectories(MSTSPath.Base() + @"\ROUTES"))
                foreach (string worldFile in Directory.GetFiles(routeFolder + @"\WORLD", "*.w"))
                {
                    Console.Write(worldFile.Replace( @"c:\personal\msts\ROUTES","...") );
                     WFile wFile = new WFile(worldFile);
                     string result1 = TileNameConversion.GetTileNameFromTileXZ(wFile.TileX, wFile.TileZ);
                     if (result1 != null)
                         Console.Write(result1);
                     else
                         Console.Write(" MISSING");
                     string result2 = null;// TileNameConversion.GetTileNameFromTileXZ2(wFile.TileX, wFile.TileZ);
                     Console.Write(result2);
                     if (result1 != result2)
                     {
                         if (result1 != null)
                         {
                             Console.WriteLine("  BAD");
                             Console.ReadLine();
                         }
                         else
                         {
                             Console.WriteLine();
                         }
                     }
                     else
                     {
                         Console.WriteLine("  OK");
                     }
                }
            Console.WriteLine("DONE");
            Console.ReadLine();
        }

        /// <summary>
        /// Test read of all world files in all routes
        /// </summary>
        public static void WorldFiles()
        {
            foreach( string baseFolder in AllBaseFolders )
                foreach (string routeFolder in Directory.GetDirectories(baseFolder + @"\ROUTES"))
                    foreach (string worldFile in Directory.GetFiles(routeFolder + @"\WORLD", "*.w"))
                    {
                        Console.WriteLine(worldFile);
                        WFile wFile = new WFile(worldFile);
                    }

        }
        /// <summary>
        /// Test read of all world files in all routes
        /// </summary>
        public static void TileFiles()
        {
            foreach (string routeFolder in Directory.GetDirectories(MSTSPath.Base() + @"\ROUTES"))
                foreach (string tileFile in Directory.GetFiles(routeFolder + @"\TILES", "*.t"))
                {
                    Console.WriteLine(tileFile);
                    try
                    {
                        TFile tFile = new TFile(tileFile);
                    }
                    catch (SystemException error)
                    {
                        Console.WriteLine(error.Message);
                        Console.ReadLine();
                    }
                }

        }
        /// <summary>
        /// Test read of all files in all routes and global folder
        /// </summary>
        public static void AllFiles()
        {
            foreach (string directoryPath in AllBaseFolders)
                TestAllFilesInFolder(directoryPath);

            Console.WriteLine("DONE");
            Console.ReadLine();
        }



        private static void TestAllFilesInFolder(string directoryPath)
        {
            foreach (string filePath in Directory.GetFiles(directoryPath, "*.ACT"))
                TestFile(filePath);

            foreach (string subDirectoryPath in Directory.GetDirectories(directoryPath))
                TestAllFilesInFolder(subDirectoryPath);
        }

        private static void TestFile( string filePath )
        {
            try
            {
                Console.WriteLine(filePath);
                ACTFile file = new ACTFile(filePath);
                //Console.WriteLine(envFile.WaterTextureNames.Count.ToString());
            }
            catch (System.Exception error)
            {
                Console.Write("**** ERROR ****");
                Console.Error.WriteLine(error.Message);
            }
        }
    }
}
