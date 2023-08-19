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
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace ORTS.TrackViewer.Editing
{
    /// <summary>
    /// Translate an internally stored path (using linked nodes etc) to the MSTS .pat format, 
    /// and write the result to a file. 
    /// 
    /// This has only one public method : WritePatFile
    /// </summary>
    public static class SavePatFile
    {
        static List<string> trackPDPs;
        static List<string> trpathnodes;
        const uint nonext = 4294967295;
        static Dictionary<int, int> pdpOfJunction;

        /// <summary>
        /// Write the path to file. This will need to confirm to the MSTS definition for .pat files.
        /// The user will be prompted for the path and possibly other things just to make sure
        /// </summary>
        /// <param name="trainpath">The path itself, that needs to be written</param>
        public static void WritePatFile(Trainpath trainpath)
        {
            if (trainpath == null) return;

            if (trainpath.IsBroken)
            {
                InformUserPathIsBroken();
                //return;
            }

            if (trainpath.FirstNode == null)
            {
                DisplayNoPathAvailable();
                return;
            }

            if (UserCancelledBecauseOfUnfinishedPath(trainpath)) return;

            if (GetFileName(trainpath))
            {
                WritePatFileDirect(trainpath);
            }
        }

        /// <summary>
        /// Write the path to file. This will need to confirm to the MSTS definition for .pat files.
        /// No additional user checks on filename, etc.
        /// </summary>
        /// <param name="trainpath">The path itself, that needs to be written</param>
        public static void WritePatFileDirect(Trainpath trainpath)
        {
            CreatePDPsAndTrpathNodes(trainpath);
            WriteToFile(trainpath);
            trainpath.IsModified = false;

        }

        static void DisplayNoPathAvailable()
        {
            MessageBox.Show(TrackViewer.catalog.GetString("Path does not even have a start node.\nSaving is not possible."),
                        TrackViewer.catalog.GetString("Trackviewer Path Editor"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        static bool UserCancelledBecauseOfUnfinishedPath(Trainpath trainpath)
        {
            List<string> cancelReasons = new List<string>();
            if (!trainpath.HasEnd) cancelReasons.Add(TrackViewer.catalog.GetString("* The path does not have a well-defined end-point"));
            //if (trainpath.IsBroken) cancelReasons.Add(TrackViewer.catalog.GetString("* The path has broken nodes or links"));
            if (trainpath.FirstNodeOfTail!=null) cancelReasons.Add(TrackViewer.catalog.GetString("* The path has a stored and not-yet reconnected tail"));
            
            if (cancelReasons.Count == 0) return false;

            string message = TrackViewer.catalog.GetString("The current path is not finished:") + "\n";
            message += String.Join("\n", cancelReasons.ToArray());
            message += "\n" + TrackViewer.catalog.GetString("Do you want to continue?");

            DialogResult dialogResult = MessageBox.Show(message, 
                        TrackViewer.catalog.GetString("Trackviewer Path Editor"), MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            return (dialogResult == DialogResult.Cancel);

        }

        static void InformUserPathIsBroken()
        {
            MessageBox.Show(
                TrackViewer.catalog.GetString("Saving broken paths might not always work correctly.") + "\n" +
                TrackViewer.catalog.GetString("The MSTS format for defining broken paths is not understood very well.") + "\n" +
                TrackViewer.catalog.GetString("Reopening it in TrackViewer, however, is likely to work."));
        }

        /// <summary>
        /// Get the filename to save the path file from the user.
        /// </summary>
        /// <param name="trainpath">The current path to get the default file path and name</param>
        /// <returns>Boolean describing whether user wants to write to file or not.</returns>
        static bool GetFileName(Trainpath trainpath)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
            {
                OverwritePrompt = true,
                InitialDirectory = Path.GetDirectoryName(trainpath.FilePath),
                FileName = trainpath.PathId,
                DefaultExt = ".pat",
                Filter = "PAT Files (.pat)|*.pat"
            };
            if (dlg.ShowDialog() == true)
            {
                trainpath.FilePath = dlg.FileName;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Run through all path nodes and create the trackPDPs and the trPathNodes
        /// </summary>
        /// <param name="trainpath">The path itself, that needs to be written</param>
        static void CreatePDPsAndTrpathNodes(Trainpath trainpath)
        {
            trackPDPs = new List<string>();
            trpathnodes = new List<string>();
            pdpOfJunction = new Dictionary<int,int>();

            uint nextMainIndex = 1;
            TrainpathNode currentMainNode = trainpath.FirstNode;
            while (currentMainNode.NextMainNode != null)
            {
                TrainpathNode nextMainNode = currentMainNode.NextMainNode;

                if (currentMainNode.NextSidingNode == null)
                {
                    AddNode(currentMainNode, nextMainIndex, nonext);
                }              
                else
                {
                    // This is a siding start.
                    // We print, in this else statement, the siding start, and all extra siding nodes.
                    // But we also need to know, for the last siding node, the siding end index, which is on main track.

                    uint nextSidingIndex = nextMainIndex;

                    // Find the amount of extra nodes in the siding path. 
                    // This can be 0 or more.
                    uint extraSidingNodes = 0;
                    TrainpathNode currentSidingNode = currentMainNode.NextSidingNode;
                    while (currentSidingNode.NextSidingNode != null)
                    {
                        extraSidingNodes++;
                        currentSidingNode = currentSidingNode.NextSidingNode;
                    }
                    nextMainIndex += extraSidingNodes;

                    // Find the number of main Nodes 
                    TrainpathNode sidingEndNode = currentSidingNode;
                    TrainpathNode tempMainNode = currentMainNode.NextMainNode;
                    uint extraMainNodes = 0;
                    while (tempMainNode != sidingEndNode)
                    {
                        extraMainNodes++;
                        tempMainNode = tempMainNode.NextMainNode; // It should exist, if not path editor itself is broken.
                    }
                    

                    if (extraSidingNodes == 0)
                    {
                        // Apparently there are no extra nodes in the siding, it is a direct path
                        AddNode(currentMainNode, nextMainIndex, nextMainIndex + extraMainNodes);
                    }
                    else
                    {
                        // Write the siding start node
                        AddNode(currentMainNode, nextMainIndex, nextSidingIndex);

                        // Write the intermediate siding nodes, so neither the first nor the last
                        // For simple passing paths, there are no intermeidate siding nodes
                        currentSidingNode = currentMainNode.NextSidingNode;
                        while (currentSidingNode.NextSidingNode.NextSidingNode != null)
                        {
                            nextSidingIndex++; 
                            AddNode(currentSidingNode, nonext, nextSidingIndex);
                            currentSidingNode = currentSidingNode.NextSidingNode;
                        }

                        // Write the final siding node, linking to main path again.
                        AddNode(currentSidingNode, nonext, nextMainIndex + extraMainNodes);
                        
                    }
                }
                nextMainIndex++;
                currentMainNode = nextMainNode;
            }

            // final node
            AddNode(currentMainNode, nonext, nonext);
        }

        /// <summary>
        /// Add a single TrPathNode. Make sure the pdp's are updated as needed.
        /// </summary>
        /// <param name="node">path node, needed for location, and various flags</param>
        /// <param name="nextMainIndex">Index of the next main node</param>
        /// <param name="nextSidingIndex">Index of the next siding node</param>
        private static void AddNode(TrainpathNode node, uint nextMainIndex, uint nextSidingIndex)
        {
            int pdpIndex;
            string trackPDPstart = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                "\tTrackPDP ( {0,6:D} {1,6:D} {2,9} {3,9:F3} {4,9:F3}",
                node.Location.TileX, node.Location.TileZ, 
                node.Location.Location.X.ToString("F3", System.Globalization.CultureInfo.CreateSpecificCulture("en-US")),
                node.Location.Location.Y.ToString("F3", System.Globalization.CultureInfo.CreateSpecificCulture("en-US")),
                node.Location.Location.Z.ToString("F3", System.Globalization.CultureInfo.CreateSpecificCulture("en-US")));

            pdpIndex = trackPDPs.Count(); // default PDP index
            TrainpathJunctionNode nodeAsJunction = node as TrainpathJunctionNode;
            if (nodeAsJunction != null)
            {
                int junctionIndex = nodeAsJunction.JunctionIndex;
                if (!node.IsBroken && pdpOfJunction.ContainsKey(junctionIndex))
                {
                    //this junction is already in the list of PDPs, so use another PDP index;
                    pdpIndex = pdpOfJunction[junctionIndex];
                }
                else
                {
                    trackPDPs.Add(String.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "{0} {1} {2} )", trackPDPstart, 2, 0));
                    pdpOfJunction[junctionIndex] = pdpIndex;
                }
            }
            else
            {   // TrainpathVectorNode
                if (node.NodeType == TrainpathNodeType.Start || node.NodeType == TrainpathNodeType.End)
                {
                    trackPDPs.Add(String.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "{0} {1} {2} )", trackPDPstart, 1, 0));
                }
                else
                {
                    trackPDPs.Add(String.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "{0} {1} {2} )", trackPDPstart, 1, 1));
                }
            }
            
            trpathnodes.Add(String.Format(System.Globalization.CultureInfo.InvariantCulture,
                "\t\tTrPathNode ( {0} {1} {2} {3} )",
                    node.FlagsToString(), nextMainIndex, nextSidingIndex, pdpIndex));
        }


        /// <summary>
        /// Actual output routine. Here also path properties like Name, Start, and End are added.
        /// </summary>
        /// <param name="trainpath">The path, needed for some properties</param>
        /// <remarks>Output will be in unicode, and also in US-EN</remarks>
        private static void WriteToFile(Trainpath trainpath)
        {
            System.IO.StreamWriter file = new System.IO.StreamWriter(trainpath.FilePath, false, System.Text.Encoding.Unicode);
            file.WriteLine("SIMISA@@@@@@@@@@JINX0P0t______");
            file.WriteLine("");
            file.WriteLine("Serial ( 1 )");
            file.WriteLine("TrackPDPs (");
            foreach (string line in trackPDPs)
            {
                file.WriteLine(line);
            }
            file.WriteLine(")");
            file.WriteLine("TrackPath (");
            file.WriteLine("\tTrPathName ( \"" + trainpath.PathId + "\" )");
         
            // How to format hex? "{0:X8}" is not working for me. Neither is {0:X08}.
            // Fortunately, {0:X} will use 8 characters anyway. Maybe because it knows the length from the number of bits in the number
            string flagsString = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:X}", trainpath.PathFlags);
            file.WriteLine("\tTrPathFlags ( " + flagsString + " )"); 
            
            file.WriteLine("\tName ( \"" + trainpath.PathName + "\" )");
            file.WriteLine("\tTrPathStart ( \""  + trainpath.PathStart + "\" )");
            file.WriteLine("\tTrPathEnd ( \"" + trainpath.PathEnd + "\" )");
            file.Write    ("\tTrPathNodes ( ");
            file.WriteLine(trpathnodes.Count());
            foreach (string line in trpathnodes) {
                file.WriteLine(line);
            }
            file.WriteLine("\t)");
            file.WriteLine(")");
            file.Close();
        }

    }

    /// <summary>
    /// Class to simply save a list of stationNames to a unicode text file.
    /// </summary>
    class SaveStationNames
    {
        /// <summary>
        /// Saves the station names to a file
        /// </summary>
        /// <param name="stationNames">String array containing the names of the stations.</param>
        public void SaveToFile(string[] stationNames)
        {
            string fullFilePath = GetFileName();
            if (String.IsNullOrEmpty(fullFilePath)) return;

            System.IO.StreamWriter file = new System.IO.StreamWriter(fullFilePath, false, System.Text.Encoding.Unicode);
            foreach (string stationName in stationNames)
            {
                file.WriteLine(stationName);
            }
            file.Close();
        }

        static string GetFileName()
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
            {
                OverwritePrompt = true,
                //dlg.InitialDirectory = Path.GetDirectoryName(trainpath.FilePath);
                FileName = "stations.txt",
                DefaultExt = ".txt",
                Filter = "Text documents (.txt)|*.txt"
            };
            if (dlg.ShowDialog() == true)
            {
                return dlg.FileName;
            }
            return String.Empty;
        }
    }
}
