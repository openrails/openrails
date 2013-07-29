// COPYRIGHT 2011, 2012, 2013 by the Open Rails project.
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

// This file is the responsibility of the 3D & Environment Team. 
#define SHOW_PHYSICS_GRAPHS     //Matej Pacha - if commented, the physics graphs are not ready for public release

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using ORTS.Common;

namespace ORTS.Popups
{
    public class HUDWindow : LayeredWindow
    {
        // Set this to the maximum number of columns that'll be used.
        const int ColumnCount = 8;

        // Set this to the width of each column.
        const int ColumnWidth = 60;

        // Set to distance from top-left corner to place text.
        const int TextOffset = 10;

        readonly int ProcessorCount = System.Environment.ProcessorCount;

        readonly Viewer3D Viewer;
        readonly Action<TableData>[] TextPages;
        readonly WindowTextFont TextFont;

        Matrix Identity = Matrix.Identity;
        int TextPage = 0;
        TableData TextTable = new TableData() { Cells = new string[0, 0] };

        HUDDebugMaterial DebugMaterial;
        HUDDebugGraphMesh DebugGraphFT;
        HUDDebugGraphMesh DebugGraphProcessRender;
        HUDDebugGraphMesh DebugGraphProcessUpdater;
        HUDDebugGraphMesh DebugGraphProcessLoader;
        HUDDebugGraphMesh DebugGraphProcessSound;

        HUDDebugGraphMesh DebugGraphMotiveForce;
        HUDDebugGraphMesh DebugGraphDynamicForce;
        HUDDebugGraphMesh DebugGraphNumOfSubsteps;

        public HUDWindow(WindowManager owner)
            : base(owner, TextOffset, TextOffset, "HUD")
        {
            Viewer = owner.Viewer;
            Visible = true;

            ProcessHandle = OpenProcess(0x410 /* PROCESS_QUERY_INFORMATION | PROCESS_VM_READ */, false, Process.GetCurrentProcess().Id);
            ProcessMemoryCounters = new PROCESS_MEMORY_COUNTERS() { cb = 40 };

            Debug.Assert(GC.MaxGeneration == 2, "Runtime is expected to have a MaxGeneration of 2.");

            TextPages = new Action<TableData>[] {
                TextPageCommon,
                TextPageBrakeInfo,
				TextPageForceInfo,
                TextPageLocoInfo,
                TextPageDispatcherInfo,
				TextPageDebugInfo,
            };

            TextFont = owner.TextFontDefaultOutlined;

            DebugMaterial = (HUDDebugMaterial)Viewer.MaterialManager.Load("Debug");
            DebugGraphFT = new HUDDebugGraphMesh(Viewer, Color.LightGreen, 1000, 100);
            DebugGraphProcessRender = new HUDDebugGraphMesh(Viewer, Color.Red, 1000, 25);
            DebugGraphProcessUpdater = new HUDDebugGraphMesh(Viewer, Color.Yellow, 1000, 25);
            DebugGraphProcessLoader = new HUDDebugGraphMesh(Viewer, Color.Magenta, 1000, 25);
            DebugGraphProcessSound = new HUDDebugGraphMesh(Viewer, Color.Cyan, 1000, 25);
            DebugGraphFT.GraphPos.Y = 10 * 5 + 25 * 4;
            DebugGraphProcessRender.GraphPos.Y = 10 * 4 + 25 * 3;
            DebugGraphProcessUpdater.GraphPos.Y = 10 * 3 + 25 * 2;
            DebugGraphProcessLoader.GraphPos.Y = 10 * 2 + 25;
            DebugGraphProcessSound.GraphPos.Y = 10;

            DebugGraphMotiveForce = new HUDDebugGraphMesh(Viewer, Color.Green, 500, 100);
            DebugGraphDynamicForce = new HUDDebugGraphMesh(Viewer, Color.Red, 500, 100);
            DebugGraphNumOfSubsteps = new HUDDebugGraphMesh(Viewer, Color.Blue, 500, 25);
            DebugGraphMotiveForce.GraphPos.Y = 10 * 5 + 25 * 4;
            DebugGraphDynamicForce.GraphPos.Y = 10 * 5 + 25 * 4;    //Overlapped - showing negative values of the same parameter
            DebugGraphNumOfSubsteps.GraphPos.Y = 10 * 4 + 25 * 3;
        }

        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            outf.Write(TextPage);
        }

        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            var page = inf.ReadInt32();
            if (page >= 0 && page <= TextPages.Length)
                TextPage = page;
        }

        public override bool Interactive
        {
            get
            {
                return false;
            }
        }

        public override void TabAction()
        {
            TextPage = (TextPage + 1) % TextPages.Length;
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(frame, elapsedTime, updateFull);
            if (Visible && TextPages[TextPage] == TextPageDebugInfo)
            {
                DebugGraphFT.GraphPos.X = DebugGraphProcessRender.GraphPos.X = DebugGraphProcessUpdater.GraphPos.X = DebugGraphProcessLoader.GraphPos.X = DebugGraphProcessSound.GraphPos.X = Viewer.DisplaySize.X - DebugGraphFT.GraphPos.Z - 10;
                DebugGraphFT.AddSample(Viewer.RenderProcess.FrameTime.Value * 10);
                DebugGraphProcessRender.AddSample(Viewer.RenderProcess.Profiler.Wall.Value / 100);
                DebugGraphProcessUpdater.AddSample(Viewer.UpdaterProcess.Profiler.Wall.Value / 100);
                DebugGraphProcessLoader.AddSample(Viewer.LoaderProcess.Profiler.Wall.Value / 100);
                DebugGraphProcessSound.AddSample(Viewer.SoundProcess.Profiler.Wall.Value / 100);
                var matrix = Matrix.Identity;
                frame.AddPrimitive(DebugMaterial, DebugGraphFT, RenderPrimitiveGroup.Overlay, ref matrix);
                frame.AddPrimitive(DebugMaterial, DebugGraphProcessRender, RenderPrimitiveGroup.Overlay, ref matrix);
                frame.AddPrimitive(DebugMaterial, DebugGraphProcessUpdater, RenderPrimitiveGroup.Overlay, ref matrix);
                frame.AddPrimitive(DebugMaterial, DebugGraphProcessLoader, RenderPrimitiveGroup.Overlay, ref matrix);
                frame.AddPrimitive(DebugMaterial, DebugGraphProcessSound, RenderPrimitiveGroup.Overlay, ref matrix);
            }
#if SHOW_PHYSICS_GRAPHS
            if (Visible && TextPages[TextPage] == TextPageForceInfo)
            {
                DebugGraphMotiveForce.GraphPos.X = Viewer.DisplaySize.X - DebugGraphMotiveForce.GraphPos.Z - 10;
                DebugGraphDynamicForce.GraphPos.X = Viewer.DisplaySize.X - DebugGraphMotiveForce.GraphPos.Z - 10;
                DebugGraphNumOfSubsteps.GraphPos.X = Viewer.DisplaySize.X - DebugGraphMotiveForce.GraphPos.Z - 10;
                var loco = Viewer.PlayerLocomotive as MSTSLocomotive;
                DebugGraphMotiveForce.AddSample(loco.MotiveForceN / (loco.MaxForceN));
                DebugGraphDynamicForce.AddSample(-loco.MotiveForceN / (loco.MaxForceN));
                DebugGraphNumOfSubsteps.AddSample((float)loco.LocomotiveAxle.AxleRevolutionsInt.NumOfSubstepsPS / (float)loco.LocomotiveAxle.AxleRevolutionsInt.MaxSubsteps);

                var matrix = Matrix.Identity;
                frame.AddPrimitive(DebugMaterial, DebugGraphMotiveForce, RenderPrimitiveGroup.Overlay, ref matrix);
                frame.AddPrimitive(DebugMaterial, DebugGraphDynamicForce, RenderPrimitiveGroup.Overlay, ref matrix);
                frame.AddPrimitive(DebugMaterial, DebugGraphNumOfSubsteps, RenderPrimitiveGroup.Overlay, ref matrix); 
            }
#endif
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            if (updateFull)
            {
                var table = new TableData() { Cells = new string[TextTable.Cells.GetLength(0), TextTable.Cells.GetLength(1)] };
                TextPages[0](table);
                if (TextPage > 0)
                    TextPages[TextPage](table);
                TextTable = table;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // Completely customise the rendering of the HUD - don't call base.Draw(spriteBatch).
            for (var row = 0; row < TextTable.Cells.GetLength(0); row++)
                for (var column = 0; column < TextTable.Cells.GetLength(1); column++)
                    if (TextTable.Cells[row, column] != null)
                        TextFont.Draw(spriteBatch, Rectangle.Empty, new Point(TextOffset + column * ColumnWidth, TextOffset + row * TextFont.Height), TextTable.Cells[row, column], LabelAlignment.Left, Color.White);
        }

        #region Table handling
        sealed class TableData
        {
            public string[,] Cells;
            public int CurrentRow;
            public int CurrentLabelColumn;
            public int CurrentValueColumn;
        }

        void TableSetCell(TableData table, int cellColumn, string format, params object[] args)
        {
            TableSetCell(table, table.CurrentRow, cellColumn, format, args);
        }

        void TableSetCell(TableData table, int cellRow, int cellColumn, string format, params object[] args)
        {
            if (cellRow > table.Cells.GetUpperBound(0) || cellColumn > table.Cells.GetUpperBound(1))
            {
                var newCells = new string[Math.Max(cellRow + 1, table.Cells.GetLength(0)), Math.Max(cellColumn + 1, table.Cells.GetLength(1))];
                for (var row = 0; row < table.Cells.GetLength(0); row++)
                    for (var column = 0; column < table.Cells.GetLength(1); column++)
                        newCells[row, column] = table.Cells[row, column];
                table.Cells = newCells;
            }
            Debug.Assert(!format.Contains('\n'), "HUD table cells must not contain newlines. Use the table positioning instead.");
            table.Cells[cellRow, cellColumn] = args.Length > 0 ? String.Format(format, args) : format;
        }

        void TableSetCells(TableData table, int startColumn, params string[] columns)
        {
            for (var i = 0; i < columns.Length; i++)
                TableSetCell(table, startColumn + i, columns[i]);
        }

        void TableAddLine(TableData table)
        {
            table.CurrentRow++;
        }

        void TableAddLine(TableData table, string format, params object[] args)
        {
            TableSetCell(table, table.CurrentRow, 0, format, args);
            table.CurrentRow++;
        }

        void TableSetLabelValueColumns(TableData table, int labelColumn, int valueColumn)
        {
            table.CurrentLabelColumn = labelColumn;
            table.CurrentValueColumn = valueColumn;
        }

        void TableAddLabelValue(TableData table, string label, string format, params object[] args)
        {
            TableSetCell(table, table.CurrentRow, table.CurrentLabelColumn, label);
            TableSetCell(table, table.CurrentRow, table.CurrentValueColumn, format, args);
            table.CurrentRow++;
        }
        #endregion

        void TextPageCommon(TableData table)
        {
            var mstsLocomotive = Viewer.PlayerLocomotive as MSTSLocomotive;
            var playerTrain = Viewer.PlayerLocomotive.Train;
            var showMUReverser = Math.Abs(playerTrain.MUReverserPercent) != 100;
            var showRetainers = playerTrain.RetainerSetting != RetainerSetting.Exhaust;
            var engineBrakeStatus = Viewer.PlayerLocomotive.GetEngineBrakeStatus();
            var dynamicBrakeStatus = Viewer.PlayerLocomotive.GetDynamicBrakeStatus();
            var locomotiveStatus = Viewer.PlayerLocomotive.GetStatus();
            var stretched = playerTrain.Cars.Count > 1 && playerTrain.NPull == playerTrain.Cars.Count - 1;
            var bunched = !stretched && playerTrain.Cars.Count > 1 && playerTrain.NPush == playerTrain.Cars.Count - 1;

            TableSetLabelValueColumns(table, 0, 2);
            TableAddLabelValue(table, "Version", VersionInfo.Version.Length > 0 ? VersionInfo.Version : VersionInfo.Build);
            TableAddLabelValue(table, "Time", InfoDisplay.FormattedTime(Viewer.Simulator.ClockTime));
            if (Viewer.IsReplaying)
            {
                TableAddLabelValue(table, "Replay", InfoDisplay.FormattedTime(Viewer.Log.ReplayEndsAt - Viewer.Simulator.ClockTime));
            }
#if !NEW_SIGNALLING
            TableAddLabelValue(table, "Speed", TrackMonitorWindow.FormatSpeed(Viewer.PlayerLocomotive.SpeedMpS, Viewer.MilepostUnitsMetric));
#else
            TableAddLabelValue(table, "Speed", FormatStrings.FormatSpeed(Viewer.PlayerLocomotive.SpeedMpS, Viewer.MilepostUnitsMetric));
#endif
            TableAddLabelValue(table, "Direction", showMUReverser ? "{1:F0} {0}" : "{0}", Viewer.PlayerLocomotive.Direction, Math.Abs(playerTrain.MUReverserPercent));
            TableAddLabelValue(table, "Throttle", "{0:F0}%", Viewer.PlayerLocomotive.ThrottlePercent);
            TableAddLabelValue(table, "Train brake", "{0}", Viewer.PlayerLocomotive.GetTrainBrakeStatus());
            if (showRetainers)
            {
                TableAddLabelValue(table, "Retainers", "{0}% {1}", playerTrain.RetainerPercent, playerTrain.RetainerSetting);
            }
            if (engineBrakeStatus != null)
            {
                TableAddLabelValue(table, "Engine brake", "{0}", engineBrakeStatus);
            }
            if (dynamicBrakeStatus != null)
            {
                TableAddLabelValue(table, "Dynamic brake", "{0}", dynamicBrakeStatus);
            }
            if (locomotiveStatus != null)
            {
                var lines = locomotiveStatus.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Length > 0)
                    {
                        var parts = line.Split(new[] { " = " }, 2, StringSplitOptions.None);
                        TableAddLabelValue(table, parts[0], parts.Length > 1 ? parts[1] : "");
                    }
                }
            }
            TableAddLabelValue(table, "Coupler slack", "{0:F2} m ({1} pulling, {2} pushing) {3}", playerTrain.TotalCouplerSlackM, playerTrain.NPull, playerTrain.NPush, stretched ? "Stretched" : bunched ? "Bunched" : "");
            TableAddLabelValue(table, "Coupler force", "{0:F0} N ({1:F0} kW)", playerTrain.MaximumCouplerForceN, playerTrain.MaximumCouplerForceN * playerTrain.SpeedMpS / 1000.0f);
            TableAddLine(table);
            TableAddLabelValue(table, "FPS", "{0:F0}", Viewer.RenderProcess.FrameRate.SmoothedValue);
            TableAddLine(table);
#if !NEW_SIGNALLING
            locomotiveStatus = Viewer.Simulator.AI.GetStatus();
            if (locomotiveStatus != null)
            {
                var lines = locomotiveStatus.Split('\n');
                foreach (var line in lines.Where(s => s.Length > 0))
                    if (line.Length > 0)
                        TableAddLine(table, line);
            }
#endif
            if (Viewer.PlayerLocomotive.WheelSlip)
                TableAddLine(table, "Wheel slip");
            else if ((mstsLocomotive != null) && mstsLocomotive.LocomotiveAxle.IsWheelSlipWarning)
                TableAddLine(table, "Wheel slip warning");
            if (Viewer.PlayerLocomotive.GetSanderOn())
                TableAddLine(table, "Sander on");

            if (MultiPlayer.MPManager.IsMultiPlayer())
            {
                TableAddLine(table, "MultiPlayer Status");
                var text = MultiPlayer.MPManager.Instance().GetOnlineUsersInfo();
                var temp = text.Split('\t');
                foreach (var t in temp) TableAddLabelValue(table, "", "{0}", t);
            }
        }

        void TextPageBrakeInfo(TableData table)
        {
            TextPageHeading(table, "BRAKE INFORMATION");

            var train = Viewer.PlayerLocomotive.Train;
            TableAddLabelValue(table, "Main reservoir", "{0:F0} psi", train.BrakeLine2PressurePSI);

            var n = Math.Min(10, train.Cars.Count);
            for (var i = 0; i < n; i++)
            {
                var j = i == 0 ? 0 : i * (train.Cars.Count - 1) / (n - 1);
                var car = train.Cars[j];
                TableSetCell(table, 0, "{0}", j + 1);
                TableSetCells(table, 1, car.BrakeSystem.GetDebugStatus());
                TableAddLine(table);
            }
        }

        void TextPageForceInfo(TableData table)
        {
            TextPageHeading(table, "FORCE INFORMATION");

            var train = Viewer.PlayerLocomotive.Train;
            var mstsLocomotive = Viewer.PlayerLocomotive as MSTSLocomotive;
            if (mstsLocomotive != null)
            {
                if ((mstsLocomotive.Simulator.UseAdvancedAdhesion) && (!mstsLocomotive.AntiSlip))
                {
                    TableAddLabelValue(table, "Wheel slip", "{0:F0}% ({1:F0}%/s)", mstsLocomotive.LocomotiveAxle.SlipSpeedPercent, mstsLocomotive.LocomotiveAxle.SlipDerivationPercentpS);
                    TableAddLabelValue(table, "Axle drive force", "{0:F0} N", mstsLocomotive.LocomotiveAxle.DriveForceN);
                    TableAddLabelValue(table, "Axle brake force", "{0:F0} N", mstsLocomotive.LocomotiveAxle.BrakeForceN);
                    TableAddLabelValue(table, "Num of substeps", "{0:F0}", mstsLocomotive.LocomotiveAxle.AxleRevolutionsInt.NumOfSubstepsPS);
                    TableAddLabelValue(table, "Solver", "{0}", mstsLocomotive.LocomotiveAxle.AxleRevolutionsInt.Method.ToString());
                    TableAddLabelValue(table, "Stability correction", "{0:F0}", mstsLocomotive.LocomotiveAxle.AdhesionK);
                    TableAddLabelValue(table, "Axle out force", "{0:F0} N ({1:F0} kW)", mstsLocomotive.LocomotiveAxle.AxleForceN, mstsLocomotive.LocomotiveAxle.AxleForceN * mstsLocomotive.WheelSpeedMpS / 1000.0f);
                }
                else
                {
                    TableAddLine(table, "(Advanced adhesion model disabled)");
                    TableAddLabelValue(table, "Axle out force", "{0:F0} N ({1:F0} kW)", mstsLocomotive.MotiveForceN, mstsLocomotive.MotiveForceN * mstsLocomotive.SpeedMpS / 1000.0f);
                }
                TableAddLine(table);
            }

            //TableAddLine(table,"Coupler breaks: {0:F0}", train.NumOfCouplerBreaks);

            TableSetCells(table, 0, "Car", "Total", "Motive", "Friction", "Gravity", "Coupler", "Mass", "Notes");
            TableAddLine(table);

            var n = Math.Min(10, train.Cars.Count);
            for (var i = 0; i < n; i++)
            {
                var j = i == 0 ? 0 : i * (train.Cars.Count - 1) / (n - 1);
                var car = train.Cars[j];
                TableSetCell(table, 0, "{0}", j + 1);
                TableSetCell(table, 1, "{0:F0}", car.TotalForceN);
                TableSetCell(table, 2, "{0:F0}", car.MotiveForceN);
                TableSetCell(table, 3, "{0:F0}", car.FrictionForceN);
                TableSetCell(table, 4, "{0:F0}", car.GravityForceN);
                TableSetCell(table, 5, "{0:F0}", car.CouplerForceU);
                TableSetCell(table, 6, "{0:F0}", car.MassKG);
                TableSetCell(table, 7, car.Flipped ? "Flipped" : "");
                TableSetCell(table, 8, car.CouplerOverloaded ? "Coupler overloaded" : "");
                TableAddLine(table);
            }
        }

        void TextPageLocoInfo(TableData table)
        {
            TextPageHeading(table, "LOCOMOTIVE INFORMATION");

            var train = Viewer.PlayerLocomotive.Train;
            var mstsLocomotive = Viewer.PlayerLocomotive as MSTSLocomotive;

            if (mstsLocomotive != null)
            {
                if (mstsLocomotive.GetType() == typeof(MSTSDieselLocomotive))
                {
                    var locomotiveStatus = ((MSTSDieselLocomotive)mstsLocomotive).GetSpecialInfoStatus();
                    if (locomotiveStatus != null)
                    {
                        var lines = locomotiveStatus.Split('\n');
                        foreach (var line in lines)
                        {
                            if (line.Length > 0)
                            {
                                var parts = line.Split(new[] { " = " }, 2, StringSplitOptions.None);
                                TableAddLabelValue(table, parts[0], parts.Length > 1 ? parts[1] : "");
                            }
                        }
                    }
                }
                TableAddLine(table);
            }


            //TableAddLine(table,"Coupler breaks: {0:F0}", train.NumOfCouplerBreaks);
            TableAddLine(table, "Electric Locomotives:");
            TableSetCells(table, 0, "Car", "PowerOn", "Pantos", "Throttle", "Power", "Ft[N]", "WhlSlip", "Flipped", "AuxPwr", "Notes");
            TableAddLine(table);

            int numDispCars = 0;
            foreach(TrainCar car in train.Cars)
            {
                if (car.GetType() == typeof(MSTSElectricLocomotive))
                {
                    TableSetCell(table, 0, "{0}", numDispCars);
                    TableSetCell(table, 1, "{0}", ((MSTSElectricLocomotive)car).PowerOn ? "On" : "Off");
                    TableSetCell(table, 1, "{0} {1}", ((MSTSElectricLocomotive)car).Pan1Up ? "Up" : "Dn", ((MSTSElectricLocomotive)car).Pan2Up ? "Up" : "Dn");
                    TableSetCell(table, 2, "{0:F0}", ((MSTSElectricLocomotive)car).ThrottlePercent);
                    TableSetCell(table, 3, "{0:F0}", ((MSTSElectricLocomotive)car).MotiveForceN * car.SpeedMpS);
                    TableSetCell(table, 4, "{0:F0}", ((MSTSElectricLocomotive)car).MotiveForceN);
                    if ((car.Simulator.UseAdvancedAdhesion) && (!((MSTSLocomotive)car).AntiSlip))
                        TableSetCell(table, 5, "{0:F0}", ((MSTSLocomotive)car).LocomotiveAxle.SlipSpeedPercent);
                    else
                        TableSetCell(table, 5, "{0}", car.WheelSlip ? "WhlSlp!" : "-");
                    TableSetCell(table, 6, "{0:F0}", car.Flipped ? "Flipped" : "");
                    TableSetCell(table, 7, "{0:F0}", "");
                    TableSetCell(table, 8, car.CouplerOverloaded ? "Coupler overloaded" : "");
                    TableAddLine(table);
                    if (++numDispCars > 10)
                        break;
                }
                
            }

            TableAddLine(table);
            TableAddLine(table, "Diesel Locomotives:");
            TableSetCells(table, 0, "Car", "Status", "RPM", "Fuel/h", "Power", "Ft[N]", "WhlSlip", "Flipped", "AuxPwr", "Notes");
            TableAddLine(table);
            foreach (TrainCar car in train.Cars)
            {
                if (car.GetType() == typeof(MSTSDieselLocomotive))
                {
                    TableSetCell(table, 0, "{0}", numDispCars);
                    TableSetCell(table, 1, "{0}", ((MSTSDieselLocomotive)car).DieselEngines[0].EngineStatus.ToString());
                    if (((MSTSDieselLocomotive)car).DieselEngines.HasGearBox)
                        TableSetCell(table, 2, "{0:F0}({1})", ((MSTSDieselLocomotive)car).DieselEngines[0].RealRPM, ((MSTSDieselLocomotive)car).DieselEngines[0].GearBox.CurrentGearIndex < 0 ? "N" : (((MSTSDieselLocomotive)car).DieselEngines[0].GearBox.CurrentGearIndex + 1).ToString()); 
                    else
                        TableSetCell(table, 2, "{0:F0}", ((MSTSDieselLocomotive)car).DieselEngines[0].RealRPM);
                    TableSetCell(table, 3, "{0:F0}", ((MSTSDieselLocomotive)car).DieselEngines.DieselFlowLps * 3600.0f);
                    TableSetCell(table, 4, "{0:F0}", ((MSTSDieselLocomotive)car).MotiveForceN * car.SpeedMpS);
                    TableSetCell(table, 5, "{0:F0}", ((MSTSDieselLocomotive)car).MotiveForceN);
                    if((car.Simulator.UseAdvancedAdhesion)&&(!((MSTSLocomotive)car).AntiSlip))
                        TableSetCell(table, 6, "{0:F0}", ((MSTSDieselLocomotive)car).LocomotiveAxle.SlipSpeedPercent);
                    else
                        TableSetCell(table, 6, "{0}", car.WheelSlip ? "WhlSlp!" : "-");
                    TableSetCell(table, 7, "{0:F0}", car.Flipped ? "Flipped" : "");
                    TableSetCell(table, 8, "{0:F0}", "");
                    TableSetCell(table, 9, car.CouplerOverloaded ? "Coupler overloaded" : "");
                    TableAddLine(table);
                    if (++numDispCars > 10)
                        break;
                }
            }
        }

        void TextPageDispatcherInfo(TableData table)
        {
            TextPageHeading(table, "DISPATCHER INFORMATION");

#if NEW_SIGNALLING
            TableSetCells(table, 0, "Train", "Travelled", "Speed", "Max", "AI mode", "AI data", "Mode", "Auth", "Distance", "Signal", "Distance", "Consist", "Path");
#else	    
            TableSetCells(table, 0, "Train", "Speed", "Signal aspect", "", "Distance", "Path");
#endif
            TableAddLine(table);

#if !NEW_SIGNALLING
            foreach (var auth in Viewer.Simulator.AI.Dispatcher.TrackAuthorities)
            {
                var status = auth.GetStatus();
                TableSetCells(table, 0, status.TrainID.ToString(), TrackMonitorWindow.FormatSpeed(status.Train.SpeedMpS, Viewer.MilepostUnitsMetric), status.Train.GetNextSignalAspect().ToString(), "", TrackMonitorWindow.FormatDistance(status.Train.distanceToSignal, Viewer.MilepostUnitsMetric), status.Path);
                TableAddLine(table);
            }
#else
            foreach (var thisTrain in Viewer.Simulator.Trains)
            {
                if (thisTrain.TrainType == Train.TRAINTYPE.PLAYER)
                {
                    var status = thisTrain.GetStatus(Viewer.MilepostUnitsMetric);
                    for (var iCell = 0; iCell < status.Length; iCell++)
                        TableSetCell(table, table.CurrentRow, iCell, status[iCell]);
                    TableAddLine(table);
                }
            }

            foreach (var thisTrain in Viewer.Simulator.AI.AITrains)
            {
                var status = thisTrain.GetStatus(Viewer.MilepostUnitsMetric);
                status = thisTrain.AddMovementState(status, Viewer.MilepostUnitsMetric);
                for (var iCell = 0; iCell < status.Length; iCell++)
                    TableSetCell(table, table.CurrentRow, iCell, status[iCell]);
                TableAddLine(table);
            }
#endif
        }

        void TextPageDebugInfo(TableData table)
        {
            TextPageHeading(table, "DEBUG INFORMATION");

            TableAddLabelValue(table, "Logging enabled", "{0}", Viewer.Settings.DataLogger);
            TableAddLabelValue(table, "Build", "{0}", VersionInfo.Build);
            TableAddLabelValue(table, "Memory", "{0:F0} MB ({5}, {6}, {7}, {8}, {1:F0} MB managed, {2:F0}/{3:F0}/{4:F0} GCs)", GetWorkingSetSize() / 1024 / 1024, GC.GetTotalMemory(false) / 1024 / 1024, GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2), Viewer.TextureManager.GetStatus(), Viewer.MaterialManager.GetStatus(), Viewer.ShapeManager.GetStatus(), Viewer.World.Terrain.GetStatus());
            TableAddLabelValue(table, "CPU", "{0:F0}% ({1} logical processors)", (Viewer.RenderProcess.Profiler.CPU.SmoothedValue + Viewer.UpdaterProcess.Profiler.CPU.SmoothedValue + Viewer.LoaderProcess.Profiler.CPU.SmoothedValue + Viewer.SoundProcess.Profiler.CPU.SmoothedValue) / ProcessorCount, ProcessorCount);
            TableAddLabelValue(table, "GPU", "{0:F0} FPS ({1:F1} ms, P95 {2:F0} %, P99 {3:F0} %, shader model {4})", Viewer.RenderProcess.FrameRate.SmoothedValue, Viewer.RenderProcess.FrameTime.SmoothedP50 * 1000, Viewer.RenderProcess.FrameTime.SmoothedP95PCFromP50, Viewer.RenderProcess.FrameTime.SmoothedP99PCFromP50, Viewer.Settings.ShaderModel);
            TableAddLabelValue(table, "Adapter", "{0} ({1:F0} MB)", Viewer.AdapterDescription, Viewer.AdapterMemory / 1024 / 1024);
            if (Viewer.Settings.DynamicShadows)
            {
                TableSetCells(table, 3, Enumerable.Range(0, RenderProcess.ShadowMapCount).Select(i => String.Format("{0}/{1}", RenderProcess.ShadowMapDistance[i], RenderProcess.ShadowMapDiameter[i])).ToArray());
                TableSetCell(table, 3 + RenderProcess.ShadowMapCount, "({0}x{0})", Viewer.Settings.ShadowMapResolution);
                TableAddLine(table, "Shadow maps");
                TableSetCells(table, 3, Viewer.RenderProcess.ShadowPrimitivePerFrame.Select(p => p.ToString("F0")).ToArray());
                TableAddLabelValue(table, "Shadow primitives", "{0:F0}", Viewer.RenderProcess.ShadowPrimitivePerFrame.Sum());
            }
            TableSetCells(table, 3, Viewer.RenderProcess.PrimitivePerFrame.Select(p => p.ToString("F0")).ToArray());
            TableAddLabelValue(table, "Render primitives", "{0:F0}", Viewer.RenderProcess.PrimitivePerFrame.Sum());
            TableAddLabelValue(table, "Render process", "{0:F0}% ({1:F0}% wait)", Viewer.RenderProcess.Profiler.Wall.SmoothedValue, Viewer.RenderProcess.Profiler.Wait.SmoothedValue);
            TableAddLabelValue(table, "Updater process", "{0:F0}% ({1:F0}% wait)", Viewer.UpdaterProcess.Profiler.Wall.SmoothedValue, Viewer.UpdaterProcess.Profiler.Wait.SmoothedValue);
            TableAddLabelValue(table, "Loader process", "{0:F0}% ({1:F0}% wait)", Viewer.LoaderProcess.Profiler.Wall.SmoothedValue, Viewer.LoaderProcess.Profiler.Wait.SmoothedValue);
            TableAddLabelValue(table, "Sound process", "{0:F0}% ({1:F0}% wait)", Viewer.SoundProcess.Profiler.Wall.SmoothedValue, Viewer.SoundProcess.Profiler.Wait.SmoothedValue);
            TableAddLabelValue(table, "Total process", "{0:F0}% ({1:F0}% wait)", Viewer.RenderProcess.Profiler.Wall.SmoothedValue + Viewer.UpdaterProcess.Profiler.Wall.SmoothedValue + Viewer.LoaderProcess.Profiler.Wall.SmoothedValue + Viewer.SoundProcess.Profiler.Wall.SmoothedValue, Viewer.RenderProcess.Profiler.Wait.SmoothedValue + Viewer.UpdaterProcess.Profiler.Wait.SmoothedValue + Viewer.LoaderProcess.Profiler.Wait.SmoothedValue + Viewer.SoundProcess.Profiler.Wait.SmoothedValue);
            TableSetCells(table, 0, "Camera", "", Viewer.Camera.TileX.ToString("F0"), Viewer.Camera.TileZ.ToString("F0"), Viewer.Camera.Location.X.ToString("F3"), Viewer.Camera.Location.Y.ToString("F3"), Viewer.Camera.Location.Z.ToString("F3"));
            TableAddLine(table);
        }

        void TextPageHeading(TableData table, string name)
        {
            TableAddLine(table);
            TableAddLine(table, name);
        }

        #region Native code
        [StructLayout(LayoutKind.Sequential, Size = 40)]
        struct PROCESS_MEMORY_COUNTERS
        {
            public int cb;
            public int PageFaultCount;
            public int PeakWorkingSetSize;
            public int WorkingSetSize;
            public int QuotaPeakPagedPoolUsage;
            public int QuotaPagedPoolUsage;
            public int QuotaPeakNonPagedPoolUsage;
            public int QuotaNonPagedPoolUsage;
            public int PagefileUsage;
            public int PeakPagefileUsage;
        }

        [DllImport("psapi.dll", SetLastError = true)]
        static extern bool GetProcessMemoryInfo(IntPtr hProcess, out PROCESS_MEMORY_COUNTERS counters, int size);

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(int dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

        readonly IntPtr ProcessHandle;
        PROCESS_MEMORY_COUNTERS ProcessMemoryCounters;
        #endregion

        int GetWorkingSetSize()
        {
            // Get memory usage (working set).
            GetProcessMemoryInfo(ProcessHandle, out ProcessMemoryCounters, ProcessMemoryCounters.cb);
            var memory = ProcessMemoryCounters.WorkingSetSize;
            return memory;
        }
    }

    public class HUDDebugGraphMesh : RenderPrimitive
    {
        const int SampleCount = 1000;
        const int VerticiesPerSample = 6;
        const int PrimitivesPerSample = 2;
        const int VertexCount = VerticiesPerSample * SampleCount;

        readonly VertexDeclaration VertexDeclaration;
        readonly DynamicVertexBuffer VertexBuffer;
        readonly Color Color;

        public Vector4 GraphPos;
        public Vector2 Sample;

        public HUDDebugGraphMesh(Viewer3D viewer, Color color, int width, int height)
        {
            VertexDeclaration = new VertexDeclaration(viewer.GraphicsDevice, VertexPositionColor.VertexElements);
            VertexBuffer = new DynamicVertexBuffer(viewer.GraphicsDevice, VertexCount * VertexPositionColor.SizeInBytes, BufferUsage.WriteOnly);
            Color = color;
            GraphPos.Z = width;
            GraphPos.W = height;
            Sample.Y = SampleCount;
        }

        public void AddSample(float value)
        {
            value = MathHelper.Clamp(value, 0, 1);
            var x0 = Sample.X / Sample.Y;
            var x1 = (Sample.X + 1) / Sample.Y;

            VertexBuffer.SetData((int)Sample.X * VerticiesPerSample * VertexPositionColor.SizeInBytes, new [] {
                new VertexPositionColor(new Vector3(x0, value, 0), Color),
                new VertexPositionColor(new Vector3(x1, value, 0), Color),
                new VertexPositionColor(new Vector3(x1, 0, 0), Color),
                new VertexPositionColor(new Vector3(x1, 0, 0), Color),
                new VertexPositionColor(new Vector3(x0, value, 0), Color),
                new VertexPositionColor(new Vector3(x0, 0, 0), Color),
            }, 0, VerticiesPerSample, VertexPositionColor.SizeInBytes, SetDataOptions.NoOverwrite);

            Sample.X = (Sample.X + 1) % SampleCount;
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.VertexDeclaration = VertexDeclaration;
            graphicsDevice.Vertices[0].SetSource(VertexBuffer, 0, VertexPositionColor.SizeInBytes);
            if (Sample.X > 1)
                graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, ((int)Sample.X - 1) * PrimitivesPerSample);
            if (Sample.X + 1 < SampleCount)
                graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, ((int)Sample.X + 1) * VerticiesPerSample, (SampleCount - (int)Sample.X - 1) * PrimitivesPerSample);
        }
    }

    public class HUDDebugMaterial : Material
    {
        IEnumerator<EffectPass> ShaderPassesGraph;

        public HUDDebugMaterial(Viewer3D viewer)
            : base(viewer, null)
        {
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            var shader = Viewer.MaterialManager.DebugShader;
            shader.CurrentTechnique = shader.Techniques["Graph"];
            if (ShaderPassesGraph == null) ShaderPassesGraph = shader.Techniques["Graph"].Passes.GetEnumerator();
            shader.ScreenSize = new Vector2(Viewer.DisplaySize.X, Viewer.DisplaySize.Y);

            var rs = graphicsDevice.RenderState;
            rs.CullMode = CullMode.None;
            rs.DepthBufferEnable = false;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.DebugShader;

            shader.Begin();
            ShaderPassesGraph.Reset();
            while (ShaderPassesGraph.MoveNext())
            {
                ShaderPassesGraph.Current.Begin();
                foreach (var item in renderItems)
                {
                    var graphMesh = item.RenderPrimitive as HUDDebugGraphMesh;
                    if (graphMesh != null)
                    {
                        shader.GraphPos = graphMesh.GraphPos;
                        shader.GraphSample = graphMesh.Sample;
                        shader.CommitChanges();
                    }
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                ShaderPassesGraph.Current.End();
            }
            shader.End();
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            var rs = graphicsDevice.RenderState;
            rs.CullMode = CullMode.CullCounterClockwiseFace;
            rs.DepthBufferEnable = true;
        }
    }

    [CallOnThread("Render")]
    public class HUDDebugShader : Effect
    {
        readonly EffectParameter screenSize;
        readonly EffectParameter graphPos;
        readonly EffectParameter graphSample;

        public Vector2 ScreenSize { set { screenSize.SetValue(value); } }

        public Vector4 GraphPos { set { graphPos.SetValue(value); } }

        public Vector2 GraphSample { set { graphSample.SetValue(value); } }

        public HUDDebugShader(GraphicsDevice graphicsDevice, ContentManager content)
            : base(graphicsDevice, content.Load<Effect>("DebugShader"))
        {
            screenSize = Parameters["ScreenSize"];
            graphPos = Parameters["GraphPos"];
            graphSample = Parameters["GraphSample"];
        }
    }
}
