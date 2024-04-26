// COPYRIGHT 2014, 2015 by the Open Rails project.
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

// Uncomment this define to show a textual representation of the serialised Content items for debugging.
//#define DEBUG_CONTENT_SERIALIZATION

using Orts.Formats.Msts;
using ORTS.ContentManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Path = ORTS.ContentManager.Models.Path;
using ORTS.Common;

namespace ORTS.ContentManager
{
    public static class ContentInfo
    {
        // will be set from ConntentManagerGUI constructor
        public static bool IsMetric = false;
        public static bool IsUK = false;
        public static bool IsImperialBHP = false; // using default; seems only used for steam locos
        public static bool IsImperialBTUpS = false;  // using default; seems only be used for steam locos

        public static string GetText(Content content)
        {
            var details = new StringBuilder();
            details.AppendFormat("Type:\t{1}{0}", Environment.NewLine, content.Type);
            details.AppendFormat("Name:\t{1}{0}", Environment.NewLine, content.Name);
            details.AppendFormat("Path:\t{1}{0}", Environment.NewLine, content.PathName);

            try {
                var stream = new MemoryStream();
                var serializer = new BinaryFormatter();
                serializer.Serialize(stream, content);
#if DEBUG_CONTENT_SERIALIZATION
                var serializedText = new StringBuilder((int)stream.Length);
                stream.Position = 0;
                while (stream.Position < stream.Length)
                {
                    var streamByte = stream.ReadByte();
                    serializedText.Append(streamByte >= 32 ? new String((char)streamByte, 1) : ".");
                }
                details.AppendFormat("Serialization:\t{1} bytes\t{2}{0}", Environment.NewLine, stream.Length, serializedText.ToString());
#else
                details.AppendFormat("Serialization:\t{1} bytes{0}", Environment.NewLine, stream.Length);
#endif
                details.Append(Environment.NewLine);
            }
            catch (Exception error)
            {
                details.Append(Environment.NewLine);
                details.Append(error);
                details.Append(Environment.NewLine);
                details.Append(Environment.NewLine);
            }

            try
            {

                if (content.Type == ContentType.Route)
                {
                    var data = new Route(content);
                    details.AppendFormat("Name:\t{1}{0}", Environment.NewLine, data.Name);
                    details.AppendFormat("Description:\t{0}{0}{1}{0}{0}", Environment.NewLine, data.Description);

                    if (content is ContentMSTSRoute)
                    {
                        var file = new RouteFile(System.IO.Path.Combine(content.PathName, content.Name + ".trk"));
                        details.AppendFormat("Route ID:\t{1}{0}", Environment.NewLine, file.Tr_RouteFile.RouteID);
                        details.AppendFormat("Route Key:\t{1}{0}", Environment.NewLine, file.Tr_RouteFile.FileName);
                    }
                }
                else if (content.Type == ContentType.Activity)
                {
                    var data = new Activity(content);
                    details.AppendFormat("Name:\t{1}{0}", Environment.NewLine, data.Name);
                    foreach (var service in data.PlayerServices)
                        details.AppendFormat("Player:\t\u0001{1}\u0002Service\u0001{0}", Environment.NewLine, service);
                    foreach (var service in data.Services)
                        details.AppendFormat("Traffic:\t\u0001{1}\u0002Service\u0001{0}", Environment.NewLine, service);
                    details.AppendLine();
                    details.AppendFormat("Description:\t{0}{0}{1}{0}{0}", Environment.NewLine, data.Description);
                    details.AppendFormat("Briefing:\t{0}{0}{1}{0}{0}", Environment.NewLine, data.Briefing);
                }
                else if (content.Type == ContentType.Service)
                {
                    var data = new Service(content);
                    details.AppendFormat("Name:\t{1}{0}", Environment.NewLine, data.Name);
                    details.AppendFormat("ID:\t{1}{0}", Environment.NewLine, data.ID);
                    details.AppendFormat("Start time:\t{1}{0}", Environment.NewLine, FormatDateTime(data.StartTime));
                    details.AppendFormat("Consist:\t\u0001{1}\u0002Consist\u0001{2}{0}", Environment.NewLine, data.Consist, data.Reversed ? " (reversed)" : "");
                    details.AppendFormat("Path:\t\u0001{1}\u0002Path\u0001{0}", Environment.NewLine, data.Path);
                    details.AppendLine();
                    details.AppendFormat("Arrival:\tDeparture:\tStation:\tDistance:\t{0}", Environment.NewLine);
                    foreach (var item in data.Stops)
                        if (String.IsNullOrEmpty(item.Station))
                            details.AppendFormat("{3}\t{4}\t{1}\t{2} m{0}", Environment.NewLine, item.PlatformID, item.Distance, FormatDateTime(item.ArrivalTime), FormatDateTime(item.DepartureTime));
                        else
                            details.AppendFormat("{2}\t{3}\t{1}{0}", Environment.NewLine, item.Station, FormatDateTime(item.ArrivalTime), FormatDateTime(item.DepartureTime));
                }
                else if (content.Type == ContentType.Path)
                {
                    var data = new Path(content);
                    details.AppendFormat("Name:\t{1}{0}", Environment.NewLine, data.Name);
                    details.AppendFormat("Start:\t{1}{0}", Environment.NewLine, data.StartName);
                    details.AppendFormat("End:\t{1}{0}", Environment.NewLine, data.EndName);
                    details.AppendLine();
                    details.AppendFormat("Path:\tLocation:\tFlags:\t{0}", Environment.NewLine);
                    var visitedNodes = new HashSet<Path.Node>();
                    var rejoinNodes = new HashSet<Path.Node>();
                    foreach (var node in data.Nodes)
                    {
                        foreach (var nextNode in node.Next)
                        {
                            if (!visitedNodes.Contains(nextNode))
                                visitedNodes.Add(nextNode);
                            else if (!rejoinNodes.Contains(nextNode))
                                rejoinNodes.Add(nextNode);
                        }
                    }
                    var tracks = new List<Path.Node>() { data.Nodes.First() };
                    var activeTrack = 0;
                    while (tracks.Count > 0)
                    {
                        var node = tracks[activeTrack];
                        var line = new StringBuilder();
                        line.Append(" ");
                        for (var i = 0; i < tracks.Count; i++)
                            line.Append(i == activeTrack ? " |" : " .");
                        if ((node.Flags & Path.Flags.Wait) != 0)
                            line.AppendFormat("\t{1}\t{2} (wait for {3} seconds){0}", Environment.NewLine, node.Location, node.Flags, node.WaitTime);
                        else
                            line.AppendFormat("\t{1}\t{2}{0}", Environment.NewLine, node.Location, node.Flags);
                        if (node.Next.Count() == 0)
                        {
                            line.Append(" ");
                            for (var i = 0; i < tracks.Count; i++)
                                line.Append(i == activeTrack ? @"  " : @" .");
                            line.Append(Environment.NewLine);
                        }
                        else if (node.Next.Count() == 2)
                        {
                            line.Append(" ");
                            for (var i = 0; i < tracks.Count; i++)
                                line.Append(i == activeTrack ? @" |\" : @" .");
                            line.Append(Environment.NewLine);
                        }
                        tracks.RemoveAt(activeTrack);
                        tracks.InsertRange(activeTrack, node.Next);
                        if (node.Next.Count() >= 1 && rejoinNodes.Contains(tracks[activeTrack]))
                        {
                            activeTrack++;
                            activeTrack %= tracks.Count;
                            if (rejoinNodes.Contains(tracks[activeTrack]))
                            {
                                activeTrack = tracks.IndexOf(tracks[activeTrack]);
                                tracks.RemoveAt(tracks.LastIndexOf(tracks[activeTrack]));
                                line.Append(" ");
                                for (var i = 0; i < tracks.Count; i++)
                                    line.Append(i == activeTrack ? @" |/" : @" .");
                                line.Append(Environment.NewLine);
                            }
                        }
                        details.Append(line);
                    }
                }
                else if (content.Type == ContentType.Consist)
                {
                    var data = new Consist(content);
                    details.AppendFormat("Name:\t{1}{0}", Environment.NewLine, data.Name);
                    details.AppendFormat("NumEngines:\t{1}{0}", Environment.NewLine, data.NumEngines);
                    details.AppendFormat("NumCars:\t{1}{0}", Environment.NewLine, data.NumCars);
                    details.AppendFormat("MaxSpeed:\t{1}{0}", Environment.NewLine, FormatStrings.FormatSpeedLimit(data.MaxSpeedMps, IsMetric));
                    details.AppendFormat("Weight:\t{1}{0}", Environment.NewLine, FormatStrings.FormatLargeMass(data.MassKG, IsMetric, IsUK));
                    details.AppendFormat("Length:\t{1}{0}", Environment.NewLine, FormatStrings.FormatShortDistanceDisplay(data.LengthM, IsMetric));
                    details.AppendFormat("Power:\t{1}{0}", Environment.NewLine, FormatStrings.FormatPower(data.MaxPowerW, IsMetric, IsImperialBHP, IsImperialBTUpS));
                    details.AppendFormat("MaxTE:\t{1}{0}", Environment.NewLine, FormatStrings.FormatForce(data.MaxTractiveForceN, IsMetric));
                    if (!IsMetric && !IsUK) details.AppendFormat("HPT:\t{1}{0}", Environment.NewLine, FormatHPT(data.MaxPowerW, data.MassKG));
                    if (!IsMetric && !IsUK) details.AppendFormat("TPOB:\t{1}{0}", Environment.NewLine, FormatTPOB(data.MassKG, data.NumOperativeBrakes));
                    details.AppendLine();
                    details.AppendFormat("Car ID:\tDirection:\tWeight:\tName:\t{0}", Environment.NewLine);
                    foreach (var car in data.Cars)
                        details.AppendFormat("{1}\t{2}\t{3}\t\u0001{4}\u0002Car\u0001{0}", Environment.NewLine, car.ID, car.Direction, car.IsEngine ? "Engine" : FormatMassBar(car.MassKG), car.Name);
                    details.AppendFormat("{0}", Environment.NewLine);
                }
                else if (content.Type == ContentType.Car)
                {
                    var data = new Car(content);
                    details.AppendFormat("Type:\t{1}{0}", Environment.NewLine, data.Type);
                    details.AppendFormat("SubType:\t{1}{0}", Environment.NewLine, data.SubType);
                    details.AppendFormat("Name:\t{1}{0}", Environment.NewLine, data.Name);
                    details.AppendFormat("Weight:\t{1} ({2}){0}", Environment.NewLine, FormatStrings.FormatMass(data.MassKG, IsMetric), FormatStrings.FormatLargeMass(data.MassKG, IsMetric, IsUK));
                    details.AppendFormat("Length:\t{1}{0}", Environment.NewLine, FormatStrings.FormatShortDistanceDisplay(data.LengthM, IsMetric));
                    if (data.Type == CarType.Engine)
                    {
                        details.AppendFormat("MaxPowerW:\t{1}{0}", Environment.NewLine, FormatStrings.FormatPower(data.MaxPowerW, IsMetric, IsImperialBHP, IsImperialBTUpS));
                        details.AppendFormat("MaxForce:\t{1}{0}", Environment.NewLine, FormatStrings.FormatForce(data.MaxForceN, IsMetric));
                        details.AppendFormat("MaxSpeed:\t{1}{0}", Environment.NewLine, FormatStrings.FormatSpeedLimit(data.MaxSpeedMps, IsMetric));
                    }
                    details.AppendFormat("MaxBrakeF:\t{1}{0}", Environment.NewLine, FormatStrings.FormatForce(data.MaxBarkeForceN, IsMetric));
                    details.AppendLine();
                    details.AppendFormat("Description:\t{0}{0}{1}{0}{0}", Environment.NewLine, data.Description);
                }
                else if (content is ContentMSTSCab)
                {
                    var file = new CabViewFile(content.PathName, System.IO.Path.GetDirectoryName(content.PathName));
                    details.AppendFormat("Position:\tDimensions:\tStyle:\tType:\t{0}", Environment.NewLine);
                    foreach (var control in file.CabViewControls)
                        details.AppendFormat("{1},{2}\t{3}x{4}\t{5}\t{6}{0}", Environment.NewLine, control.PositionX, control.PositionY, control.Width, control.Height, control.ControlStyle, control.ControlType);
                    details.AppendFormat("{0}", Environment.NewLine);
                }
            }
            catch (Exception error)
            {
                details.Append(Environment.NewLine);
                details.Append(error);
                details.Append(Environment.NewLine);
                details.Append(Environment.NewLine);
            }

            return details.ToString();
        }

        static string FormatDateTime(DateTime dateTime)
        {
            return String.Format("{0} {1}", dateTime.Day - 1, dateTime.ToLongTimeString());
        }

        /// <summary>
        /// Create a simple bar graph (string of asterisk) for the car mass.
        /// </summary>
        /// <param name="massKg"></param>
        /// <returns>String of asterisk representing the mass.</returns>
        static string FormatMassBar(float massKg)
        {
            string massBar;
            var range = (int)Math.Ceiling(massKg / 20000);
            if (massKg < 1.0) { massBar = "?";  }
            else if (range > 8) { massBar = new string('*', 8) + '>'; }
            else { massBar = new string('*', range); }
            return massBar;
        }

        /// <summary>
        /// Calculate and format horsepower per ton for consist, rounded to one decimal.
        /// </summary>
        /// <param name="consistPowerW"></param>
        /// <param name="consistMassKG"></param>
        /// <returns>horsepower-per-ton formated to one decimal.</returns>
        //TODO: implement UK and metric version
        static string FormatHPT(float consistPowerW, float consistMassKG)
        {
            var hpt = consistMassKG > 0 ? W.ToHp(consistPowerW) / Kg.ToTUS(consistMassKG) : 0;
            return string.Format("{0:0.0}", hpt);
        }

        /// <summary>
        /// Calculate and format tons per operative brake for consist, rounded to an integer.
        /// </summary>
        /// <param name="consistMassKG"></param>
        /// <param name="consistNumOpBrakes"></param>
        /// <returns>tons-per-operative-brake formated to an integer.</returns>
        //TODO: implement UK and metric version
        static string FormatTPOB(float consistMassKG, float consistNumOpBrakes)
        {
            var tpob = consistNumOpBrakes > 0 ? Kg.ToTUS(consistMassKG) / consistNumOpBrakes : 0;
            return string.Format("{0:0}", tpob);
        }
    }
}
