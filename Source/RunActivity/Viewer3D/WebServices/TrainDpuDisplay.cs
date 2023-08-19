// COPYRIGHT 2019, 2020 by the Open Rails project.
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

using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems.Brakes;
using ORTS.Common;
using ORTS.Common.Input;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Orts.Viewer3D.WebServices
{
    public static class TrainDpuDisplay
    {
        /// <summary>
        /// A Train Dpu row with data fields.
        /// </summary>
        public struct ListLabel
        {
            public string FirstCol;
            public List<string> LastCol;
            public List<string> SymbolCol;
        }

        private static bool normalVerticalMode = true;// vertical window size
        private static int dieselLocomotivesCount = 0;

        /// <summary>
        /// Table of Colors to client-side color codes.
        /// </summary>
        /// <remarks>
        /// Compare codes with index.css.
        /// </remarks>
        private static readonly Dictionary<Color, string> ColorCode = new Dictionary<Color, string>
        {
            { Color.Yellow, "???" },
            { Color.Green, "??!" },
            { Color.Black, "?!?" },
            { Color.PaleGreen, "?!!" },
            { Color.White, "!??" },
            { Color.Orange, "!!?" },
            { Color.OrangeRed, "!!!" },
            { Color.Cyan, "%%%" },
            { Color.Brown, "%$$" },
            { Color.LightGreen, "%%$" },
            { Color.Blue, "$%$" },
            { Color.LightSkyBlue, "$$$" },
        };

        private static class Symbols
        {
            public const string Fence = "\u2590";
        }

        private static readonly Dictionary<string, string> FirstColToAbbreviated = new Dictionary<string, string>()
        {
            [Viewer.Catalog.GetString("Flow")] = Viewer.Catalog.GetString("FLOW"),//
            [Viewer.Catalog.GetString("Fuel")] = Viewer.Catalog.GetString("FUEL"),//
            [Viewer.Catalog.GetString("Load")] = Viewer.Catalog.GetString("LOAD"),//
            [Viewer.Catalog.GetString("Loco Groups")] = Viewer.Catalog.GetString("GRUP"),
            [Viewer.Catalog.GetString("Oil Pressure")] = Viewer.Catalog.GetString("OIL"),//
            [Viewer.Catalog.GetString("Power")] = Viewer.Catalog.GetString("POWR"),//
            [Viewer.Catalog.GetString("Remote")] = Viewer.Catalog.GetString("RMT"),//
            [Viewer.Catalog.GetString("RPM")] = Viewer.Catalog.GetString("RPM"),//
            [Viewer.Catalog.GetString("Reverser")] = Viewer.Catalog.GetString("REVR"),//
            [Viewer.Catalog.GetString("Status")] = Viewer.Catalog.GetString("STAT"),//
            [Viewer.Catalog.GetString("Temperature")] = Viewer.Catalog.GetString("TEMP"),//
            [Viewer.Catalog.GetString("Throttle")] = Viewer.Catalog.GetString("THRO"),//
            [Viewer.Catalog.GetString("Time")] = Viewer.Catalog.GetString("TIME"),//
            [Viewer.Catalog.GetString("Tractive Effort")] = Viewer.Catalog.GetString("TRACT")//
        };

        private static readonly Dictionary<string, string> LastColToAbbreviated = new Dictionary<string, string>()
        {
            [Viewer.Catalog.GetString("Forward")] = Viewer.Catalog.GetString("Forw."),
            [Viewer.Catalog.GetString("Idle")] = Viewer.Catalog.GetString("Idle"),
            [Viewer.Catalog.GetString("Running")] = Viewer.Catalog.GetString("Runn")
        };


        /// <summary>
        /// Sanitize the fields of a <see cref="ListLabel"/> in-place.
        /// </summary>
        /// <param name="label">A reference to the <see cref="ListLabel"/> to check.</param>
        private static void CheckLabel(ref ListLabel label, bool normalMode)
        {
            void CheckString(ref string s) => s = s ?? "";
            CheckString(ref label.FirstCol);

            if (label.LastCol != null)
            {
                for (int i = 0; i < label.LastCol.Count; i++)
                {
                    var LastCol = label.LastCol[i];
                    CheckString(ref LastCol);
                    label.LastCol[i] = LastCol;
                }
            }

            if (label.SymbolCol != null)
            {
                for (int i = 0; i < label.SymbolCol.Count; i++)
                {
                    var symbolCol = label.SymbolCol[i];
                    CheckString(ref symbolCol);
                    label.SymbolCol[i] = symbolCol;
                }
            }

            if (!normalMode)
            {
                foreach (KeyValuePair<string, string> mapping in FirstColToAbbreviated)
                    label.FirstCol = label.FirstCol.Replace(mapping.Key, mapping.Value);
                foreach (KeyValuePair<string, string> mapping in LastColToAbbreviated)
                {
                    if (label.LastCol != null)
                    {
                        for (int i = 0; i < label.LastCol.Count; i++)
                        {
                            label.LastCol[i] = label.LastCol[i].Replace(mapping.Key, mapping.Value);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Retrieve a formatted list <see cref="ListLabel"/>s to be displayed as an in-browser Track Monitor.
        /// </summary>
        /// <param name="viewer">The Viewer to read train data from.</param>
        /// <returns>A list of <see cref="ListLabel"/>s, one per row of the popup.</returns>
        public static IEnumerable<ListLabel> TrainDpuDisplayList(this Viewer viewer, bool normalTextMode = true)
        {
            bool useMetric = viewer.MilepostUnitsMetric;
            var labels = new List<ListLabel>();

            void AddLabel(ListLabel label)
            {
                CheckLabel(ref label, normalTextMode);
                labels.Add(label);
            }
            void AddSeparator() => AddLabel(new ListLabel
            {
                FirstCol = "Sprtr",
            });

            TrainCar trainCar = viewer.PlayerLocomotive;
            Train train = trainCar.Train;
            MSTSLocomotive locomotive = (MSTSLocomotive)trainCar;
            var multipleUnitsConfiguration = locomotive.GetMultipleUnitsConfiguration();
            List<string> lastCol;
            List<string> symbolCol;
            var notDpuTrain = false;

            // Distributed Power
            if (multipleUnitsConfiguration != null)
            {
                lastCol = new List<string>();
                symbolCol = new List<string>();
                char[] multipleUnits = multipleUnitsConfiguration.Replace(" ", "").ToCharArray();
                symbolCol.Add("");//first symbol empty
                foreach (char ch in multipleUnits)
                {
                    if (ch.ToString() != " ")
                    {
                        if (Char.IsDigit(ch))
                        {
                            lastCol.Add(ch.ToString()); continue;
                        }
                        else
                            symbolCol.Add(ch == '|' ? Symbols.Fence + ColorCode[Color.Green] : ch == '–' ? ch.ToString() : "");
                    }
                }

                // allows to draw the second fence
                lastCol.Add("");
                symbolCol.Add("");
                AddLabel(new ListLabel
                {
                    FirstCol = Viewer.Catalog.GetString("Loco Groups"),
                    SymbolCol = symbolCol,
                    LastCol = lastCol
                });
                AddSeparator();
            }
            else
            {
                lastCol = new List<string>();
                symbolCol = new List<string>();
                lastCol.Add("");
                symbolCol.Add("");
                AddLabel(new ListLabel
                {
                    FirstCol = Viewer.Catalog.GetString(" Distributed power management not available with this player train. "),
                    SymbolCol = symbolCol,
                    LastCol = lastCol
                });
                notDpuTrain = true;
            }

            if (locomotive != null && !notDpuTrain)
            {
                int numberOfDieselLocomotives = 0;
                int maxNumberOfEngines = 0;
                for (var i = 0; i < train.Cars.Count; i++)
                {
                    if (train.Cars[i] is MSTSDieselLocomotive)
                    {
                        numberOfDieselLocomotives++;
                        maxNumberOfEngines = Math.Max(maxNumberOfEngines, (train.Cars[i] as MSTSDieselLocomotive).DieselEngines.Count);
                    }
                }
                if (numberOfDieselLocomotives > 0)
                {
                    var dieselLoco = MSTSDieselLocomotive.GetDpuHeader(normalVerticalMode, numberOfDieselLocomotives, maxNumberOfEngines).Replace("\t", "");
                    string[] dieselLocoHeader = dieselLoco.Split('\n');
                    string[,] tempStatus = new string[numberOfDieselLocomotives, dieselLocoHeader.Length];
                    var k = 0;
                    var dpUnitId = 0;
                    var dpUId = -1;
                    for (var i = 0; i < train.Cars.Count; i++)
                    {
                        if (train.Cars[i] is MSTSDieselLocomotive)
                        {
                            if (dpUId != (train.Cars[i] as MSTSLocomotive).DPUnitID)
                            {
                                var status = (train.Cars[i] as MSTSDieselLocomotive).GetDpuStatus(normalVerticalMode).Split('\t');
                                var fence = ((dpUnitId != (dpUnitId = train.Cars[i].RemoteControlGroup)) ? "|" : " ");
                                for (var j = 0; j < status.Length; j++)
                                {
                                    // fence
                                    tempStatus[k, j] = fence + status[j];
                                }
                                dpUId = (train.Cars[i] as MSTSLocomotive).DPUnitID;
                                k++;
                            }
                        }
                    }

                    dieselLocomotivesCount = k;// only leaders loco group
                    for (var j = 0; j < dieselLocoHeader.Count(); j++)
                    {
                        lastCol = new List<string>();
                        symbolCol = new List<string>();

                        for (int i = 0; i < dieselLocomotivesCount; i++)
                        {
                            symbolCol.Add(tempStatus[i, j] != null && tempStatus[i, j].Contains("|") ? Symbols.Fence + ColorCode[Color.Green] : " ");
                            lastCol.Add(tempStatus[i, j]);
                        }

                        // allows to draw the second fence
                        lastCol.Add("");
                        symbolCol.Add(" ");

                        AddLabel(new ListLabel
                        {
                            FirstCol = dieselLocoHeader[j],
                            SymbolCol = symbolCol,
                            LastCol = lastCol
                        });
                    }
                }
                AddLabel(new ListLabel());
            }

            AddLabel(new ListLabel());
            return labels;
        }
    }
}
