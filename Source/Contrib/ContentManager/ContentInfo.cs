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

using MSTS.Formats;
using ORTS.ContentManager.Formats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
                if (content.Type == ContentType.Route)
                {
                    var file = new Route(content);
                    details.AppendFormat("Name:\t{1}{0}", Environment.NewLine, file.Name);
                    details.AppendFormat("Description:\t{0}{0}{1}{0}{0}", Environment.NewLine, file.Description);
                }
                else if (content is ContentMSTSRoute)
                {
                    var file = new TRKFile(Path.Combine(content.PathName, content.Name + ".trk"));
                    details.AppendFormat("Package:\t\u0001{1}\u0002Package\u0001{0}", Environment.NewLine, content.Get(ContentType.Package).Select(p => p.Name).FirstOrDefault());
                    details.AppendFormat("Route ID:\t{1}{0}", Environment.NewLine, file.Tr_RouteFile.RouteID);
                    details.AppendFormat("Route Key:\t{1}{0}", Environment.NewLine, file.Tr_RouteFile.FileName);
                    details.AppendFormat("Name:\t{1}{0}", Environment.NewLine, file.Tr_RouteFile.Name);
                    details.AppendFormat("Description:\t{0}{0}{1}{0}{0}", Environment.NewLine, file.Tr_RouteFile.Description);
                }
                else if (content.Type == ContentType.Activity)
                {
                    var file = new Activity(content);
                    details.AppendFormat("Name:\t{1}{0}", Environment.NewLine, file.Name);
                    if (file.PlayerService != null)
                    {
                        details.AppendFormat("Player:\t\u0001{1}\u0002Service\u0001{0}", Environment.NewLine, file.PlayerService);
                    }
                    // TODO: Traffic link
                    foreach (var service in file.Services)
                    {
                        details.AppendFormat("Traffic:\t\u0001{1}\u0002Service\u0001{0}", Environment.NewLine, service);
                    }
                    details.AppendLine();
                    details.AppendFormat("Description:\t{0}{0}{1}{0}{0}", Environment.NewLine, file.Description);
                    details.AppendFormat("Briefing:\t{0}{0}{1}{0}{0}", Environment.NewLine, file.Briefing);
                }
                else if (content is ContentMSTSActivity)
                {
                    var file = new ACTFile(content.PathName);
                    details.AppendFormat("Name:\t{1}{0}", Environment.NewLine, file.Tr_Activity.Tr_Activity_Header.Name);
                    details.AppendFormat("Route ID:\t\u0001{1}\u0002Route\u0001{0}", Environment.NewLine, file.Tr_Activity.Tr_Activity_Header.RouteID);
                    details.AppendFormat("Path ID:\t\u0001{1}\u0002Path\u0001{0}", Environment.NewLine, file.Tr_Activity.Tr_Activity_Header.PathID);
                    details.AppendLine();
                    details.AppendLine("Player:\t");
                    details.AppendFormat("  Service ID:\t\u0001{1}\u0002Service\u0001{0}", Environment.NewLine, file.Tr_Activity.Tr_Activity_File.Player_Service_Definition.Name);
                    details.AppendFormat("  Start time:\t{1}{0}", Environment.NewLine, file.Tr_Activity.Tr_Activity_File.Player_Service_Definition.Player_Traffic_Definition.Time);
                    details.AppendFormat("  Platform ID:\tDistance down path:\tArrival time:\tDeparture time:\t{0}", Environment.NewLine);
                    foreach (var item in file.Tr_Activity.Tr_Activity_File.Player_Service_Definition.Player_Traffic_Definition.Player_Traffic_List)
                        details.AppendFormat("  {1}\t{2}\t{3}\t{4}{0}", Environment.NewLine, item.PlatformStartID, item.DistanceDownPath, item.ArrivalTime, item.DepartTime);
                    details.AppendLine();
                    details.AppendLine("Open Rails does not support loading the player's service data (effeciency per-station stop).");
                    details.AppendLine();
                    details.AppendFormat("Traffic ID:\t\u0001{1}\u0002Traffic\u0001{0}", Environment.NewLine, file.Tr_Activity.Tr_Activity_File.Traffic_Definition.Name);
                    foreach (var traffic in file.Tr_Activity.Tr_Activity_File.Traffic_Definition.ServiceDefinitionList)
                    {
                        details.AppendLine();
                        details.AppendLine("Traffic:\t");
                        details.AppendFormat("  Service ID:\t\u0001{1}\u0002Service\u0001{0}", Environment.NewLine, traffic.Name);
                        details.AppendFormat("  UID:\t{1}{0}", Environment.NewLine, traffic.UiD);
                        details.AppendFormat("  Start time:\t{1}{0}", Environment.NewLine, traffic.Time);
                        details.AppendFormat("  Platform ID:\tDistance down path:\tSkip count:\tEfficiency:\t{0}", Environment.NewLine);
                        foreach (var item in traffic.ServiceList)
                            details.AppendFormat("  {1}\t{2}\t{3}\t{4}{0}", Environment.NewLine, item.PlatformStartID, item.DistanceDownPath, item.SkipCount, item.Efficiency);
                    }
                    details.AppendLine();
                    details.AppendFormat("Description:\t{0}{0}{1}{0}{0}", Environment.NewLine, file.Tr_Activity.Tr_Activity_Header.Description);
                    details.AppendFormat("Briefing:\t{0}{0}{1}{0}{0}", Environment.NewLine, file.Tr_Activity.Tr_Activity_Header.Briefing);
                }
                else if (content is ContentMSTSService)
                {
                    var file = new SRVFile(content.PathName);
                    details.AppendFormat("Name:\t{1}{0}", Environment.NewLine, file.Name);
                    details.AppendFormat("Consist ID:\t\u0001{1}\u0002Consist\u0001{0}", Environment.NewLine, file.Train_Config);
                    details.AppendFormat("Path ID:\t\u0001{1}\u0002Path\u0001{0}", Environment.NewLine, file.PathID);
                    details.AppendFormat("Efficiency:\t{1}{0}{0}", Environment.NewLine, file.Efficiency);
                    details.AppendFormat("This format is not supported by Open Rails.{0}{0}", Environment.NewLine);
                }
                else if (content is ContentMSTSTraffic)
                {
                    var file = new TRFFile(content.PathName);
                    details.AppendFormat("Name:\t{1}{0}", Environment.NewLine, file.TrafficDefinition.Name);
                    foreach (var service in file.TrafficDefinition.TrafficItems)
                    {
                        details.AppendLine();
                        details.AppendLine("Service:\t");
                        details.AppendFormat("  Service ID:\t\u0001{1}\u0002Service\u0001{0}", Environment.NewLine, service.Service_Definition);
                        details.AppendFormat("  Start time:\t{1}{0}", Environment.NewLine, service.Time);
                        details.AppendFormat("  Platform ID:\tDistance down path:\tArrival time:\tDeparture time:\t{0}", Environment.NewLine);
                        foreach (var item in service.TrafficDetails)
                            details.AppendFormat("  {1}\t{2}\t{3}\t{4}{0}", Environment.NewLine, item.PlatformStartID, item.DistanceDownPath, item.ArrivalTime, item.DepartTime);
                    }
                    details.AppendLine();
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
                    details.AppendFormat("Consist ID:\t{1}{0}", Environment.NewLine, content.Name);
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
