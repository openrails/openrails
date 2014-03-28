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
using System.Linq;
using System.Text;
using MSTS.Formats;
using System.IO;

namespace ORTS.ContentManager
{
    public static class ContentInfo
    {
        public static string GetText(Content content)
        {
            var details = new StringBuilder();
            details.AppendFormat("Type:\t{1}{0}", Environment.NewLine, content.Type);
            details.AppendFormat("Name:\t{1}{0}", Environment.NewLine, content.Name);
            details.AppendFormat("Path:\t{1}{0}{0}", Environment.NewLine, content.PathName);

            try
            {
                if (content is ContentMSTSRoute)
                {
                    var file = new TRKFile(Path.Combine(content.PathName, content.Name + ".trk"));
                    details.AppendFormat("Route ID:\t{1}{0}", Environment.NewLine, file.Tr_RouteFile.RouteID);
                    details.AppendFormat("Name:\t{1}{0}", Environment.NewLine, file.Tr_RouteFile.Name);
                    details.AppendFormat("Description:\t{0}{0}{1}{0}{0}", Environment.NewLine, file.Tr_RouteFile.Description);
                }
                else if (content is ContentMSTSActivity)
                {
                    var file = new ACTFile(content.PathName);
                    details.AppendFormat("Name:\t{1}{0}", Environment.NewLine, file.Tr_Activity.Tr_Activity_Header.Name);
                    details.AppendFormat("Route ID:\t{1}{0}", Environment.NewLine, file.Tr_Activity.Tr_Activity_Header.RouteID);
                    details.AppendFormat("Path ID:\t{1}{0}", Environment.NewLine, file.Tr_Activity.Tr_Activity_Header.PathID);
                    details.AppendFormat("Description:\t{0}{0}{1}{0}{0}", Environment.NewLine, file.Tr_Activity.Tr_Activity_Header.Description);
                    details.AppendFormat("Briefing:\t{0}{0}{1}{0}{0}", Environment.NewLine, file.Tr_Activity.Tr_Activity_Header.Briefing);
                }
                else if (content is ContentMSTSPath)
                {
                    var file = new PATFile(content.PathName);
                    details.AppendFormat("Path ID:\t{1}{0}", Environment.NewLine, file.PathID);
                    details.AppendFormat("Name:\t{1}{0}", Environment.NewLine, file.Name);
                    details.AppendFormat("Start:\t{1}{0}", Environment.NewLine, file.Start);
                    details.AppendFormat("End:\t{1}{0}", Environment.NewLine, file.End);
                    details.AppendFormat("Player:\t{1}{0}", Environment.NewLine, file.IsPlayerPath ? "Yes" : "No");
                    details.AppendFormat("Index:\tTile:\tLocation:\tNotes:\t{0}", Environment.NewLine);
                    var mainlineIndex = 0;
                    // Find all mainline nodes first.
                    var mainlineIndexes = new HashSet<int>();
                    while (mainlineIndex >= 0 && mainlineIndex < file.TrPathNodes.Count)
                    {
                        mainlineIndexes.Add(mainlineIndex);
                        mainlineIndex = (int)file.TrPathNodes[mainlineIndex].nextMainNode;
                    }
                    // Now work alone the mainline.
                    mainlineIndex = 0;
                    while (mainlineIndex >= 0 && mainlineIndex < file.TrPathNodes.Count)
                    {
                        var mainline = file.TrPathNodes[mainlineIndex];
                        var pdp = file.TrackPDPs[(int)mainline.fromPDP];
                        details.AppendFormat("{8}{1}\t{2},{3}\t{4},{5},{6}\t{7}{0}", Environment.NewLine, mainlineIndex, pdp.TileX, pdp.TileZ, pdp.X, pdp.Y, pdp.Z, mainline.HasNextSidingNode ? "Alternate path start" : !mainline.HasNextMainNode ? "End of path" : "", "");
                        if (mainline.HasNextSidingNode)
                        {
                            // Work along a siding...
                            var sidingIndex = (int)file.TrPathNodes[mainlineIndex].nextSidingNode;
                            while (sidingIndex >= 0 && sidingIndex < file.TrPathNodes.Count)
                            {
                                var siding = file.TrPathNodes[sidingIndex];
                                var pdp2 = file.TrackPDPs[(int)siding.fromPDP];
                                details.AppendFormat("{8}{1}\t{2},{3}\t{4},{5},{6}\t{7}{0}", Environment.NewLine, sidingIndex, pdp2.TileX, pdp2.TileZ, pdp2.X, pdp2.Y, pdp2.Z, mainlineIndexes.Contains(sidingIndex) ? "Alternate path end" : "", "> ");
                                // Stop if we're back on the mainline.
                                if (mainlineIndexes.Contains(sidingIndex))
                                    break;
                                sidingIndex = (int)file.TrPathNodes[sidingIndex].nextSidingNode;
                            }
                        }
                        mainlineIndex = (int)file.TrPathNodes[mainlineIndex].nextMainNode;
                    }
                    details.AppendFormat("{0}", Environment.NewLine);
                }
                else if (content is ContentMSTSConsist)
                {
                    var file = new CONFile(content.PathName);
                    details.AppendFormat("Name:\t{1}{0}", Environment.NewLine, file.Name);
                    // Always the same as file.Name?  details.AppendFormat("Train:\t{1}{0}", Environment.NewLine, file.Train.TrainCfg.Name);
                    details.AppendFormat("UID:\tType/Flipped:\tTrainset:\tCar:\t{0}", Environment.NewLine);
                    foreach (var car in file.Train.TrainCfg.WagonList)
                        details.AppendFormat("{1}\t{2} {3}\t{4}\t{5}{0}", Environment.NewLine, car.UiD, car.IsEngine ? "Engine" : "Wagon", car.Flip ? "(Flipped)" : "", car.Folder, car.Name);
                    details.AppendFormat("{0}", Environment.NewLine);
                }
                else if (content is ContentMSTSCar)
                {
                    var file = new ENGFile(content.PathName);
                    details.AppendFormat("Name:\t{1}{0}", Environment.NewLine, file.Name);
                    details.AppendFormat("Description:\t{0}{0}{1}{0}{0}", Environment.NewLine, file.Description);
                }
                else if (content is ContentMSTSCab)
                {
                    var file = new CVFFile(content.PathName, Path.GetDirectoryName(content.PathName));
                    details.AppendFormat("Position:\tDimensions:\tStyle:\tType:\t{0}", Environment.NewLine);
                    foreach (var control in file.CabViewControls)
                        details.AppendFormat("{1},{2}\t{3}x{4}\t{5}\t{6}{0}", Environment.NewLine, control.PositionX, control.PositionY, control.Width, control.Height, control.ControlStyle, control.ControlType);
                    details.AppendFormat("{0}", Environment.NewLine);
                }
            }
            catch { }

            return details.ToString();
        }
    }
}
