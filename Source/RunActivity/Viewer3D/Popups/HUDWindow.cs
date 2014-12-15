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
using Microsoft.Xna.Framework.Graphics;
using ORTS.Common;
using ORTS.Processes;
using ORTS.Viewer3D;

namespace ORTS.Viewer3D.Popups
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

        readonly PerformanceCounter AllocatedBytesPerSecCounter; // \.NET CLR Memory(*)\Allocated Bytes/sec
        float AllocatedBytesPerSecLastValue;

        readonly Viewer Viewer;
        readonly Action<TableData>[] TextPages;
        readonly WindowTextFont TextFont;
		readonly HUDGraphMaterial HUDGraphMaterial;

        int TextPage;
        TableData TextTable = new TableData() { Cells = new string[0, 0] };

        HUDGraphSet ForceGraphs;
        HUDGraphMesh ForceGraphMotiveForce;
        HUDGraphMesh ForceGraphDynamicForce;
        HUDGraphMesh ForceGraphNumOfSubsteps;

        HUDGraphSet LocomotiveGraphs;
        HUDGraphMesh LocomotiveGraphsThrottle;
        HUDGraphMesh LocomotiveGraphsInputPower;
        HUDGraphMesh LocomotiveGraphsOutputPower;

        HUDGraphSet DebugGraphs;
        HUDGraphMesh DebugGraphMemory;
        HUDGraphMesh DebugGraphGCs;
        HUDGraphMesh DebugGraphFrameTime;
        HUDGraphMesh DebugGraphProcessRender;
        HUDGraphMesh DebugGraphProcessUpdater;
        HUDGraphMesh DebugGraphProcessLoader;
        HUDGraphMesh DebugGraphProcessSound;

        public HUDWindow(WindowManager owner)
            : base(owner, TextOffset, TextOffset, "HUD")
        {
            Viewer = owner.Viewer;
            Visible = true;

            ProcessHandle = OpenProcess(0x410 /* PROCESS_QUERY_INFORMATION | PROCESS_VM_READ */, false, Process.GetCurrentProcess().Id);
            ProcessMemoryCounters = new PROCESS_MEMORY_COUNTERS() { Size = 40 };
            ProcessVirtualAddressLimit = GetVirtualAddressLimit();

            try
            {
                var counterDotNetClrMemory = new PerformanceCounterCategory(".NET CLR Memory");
                foreach (var process in counterDotNetClrMemory.GetInstanceNames())
                {
                    var processId = new PerformanceCounter(".NET CLR Memory", "Process ID", process);
                    if (processId.NextValue() == Process.GetCurrentProcess().Id)
                    {
                        AllocatedBytesPerSecCounter = new PerformanceCounter(".NET CLR Memory", "Allocated Bytes/sec", process);
                        break;
                    }
                }
            }
            catch (Exception error)
            {
                Trace.WriteLine(error);
                Trace.TraceWarning("Unable to access Microsoft .NET Framework performance counters. This may be resolved by following the instructions at http://support.microsoft.com/kb/300956");
            }

            Debug.Assert(GC.MaxGeneration == 2, "Runtime is expected to have a MaxGeneration of 2.");

            TextPages = new Action<TableData>[] {
                TextPageCommon,
                TextPageConsistInfo,
                TextPageLocomotiveInfo,
                TextPageBrakeInfo,
				TextPageForceInfo,
                TextPageDispatcherInfo,
				TextPageDebugInfo,
            };

            TextFont = owner.TextFontDefaultOutlined;

			HUDGraphMaterial = (HUDGraphMaterial)Viewer.MaterialManager.Load("Debug");

			LocomotiveGraphs = new HUDGraphSet(Viewer, HUDGraphMaterial);
            LocomotiveGraphsThrottle = LocomotiveGraphs.Add("Throttle", "0", "100%", Color.Blue, 50);
            LocomotiveGraphsInputPower = LocomotiveGraphs.Add("Power In/Out", "0", "100%", Color.Yellow, 50);
            LocomotiveGraphsOutputPower = LocomotiveGraphs.AddOverlapped(Color.Green, 50);

			ForceGraphs = new HUDGraphSet(Viewer, HUDGraphMaterial);
            ForceGraphMotiveForce = ForceGraphs.Add("Motive force", "0%", "100%", Color.Green, 75);
            ForceGraphDynamicForce = ForceGraphs.AddOverlapped(Color.Red, 75);
            ForceGraphNumOfSubsteps = ForceGraphs.Add("Num of substeps", "0", "300", Color.Blue, 25);

			DebugGraphs = new HUDGraphSet(Viewer, HUDGraphMaterial);
            DebugGraphMemory = DebugGraphs.Add("Memory", "0GB", String.Format("{0:F0}GB", (float)ProcessVirtualAddressLimit / 1024 / 1024 / 1024), Color.Orange, 50);
            DebugGraphGCs = DebugGraphs.Add("GCs", "0", "2", Color.Magenta, 20); // Multiple of 4
            DebugGraphFrameTime = DebugGraphs.Add("Frame time", "0.0s", "0.1s", Color.LightGreen, 50);
            DebugGraphProcessRender = DebugGraphs.Add("Render process", "0%", "100%", Color.Red, 20);
            DebugGraphProcessUpdater = DebugGraphs.Add("Updater process", "0%", "100%", Color.Yellow, 20);
            DebugGraphProcessLoader = DebugGraphs.Add("Loader process", "0%", "100%", Color.Magenta, 20);
            DebugGraphProcessSound = DebugGraphs.Add("Sound process", "0%", "100%", Color.Cyan, 20);
#if WITH_PATH_DEBUG
            TextPage = 5;
#endif
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

		public override void Mark()
		{
			base.Mark();
			HUDGraphMaterial.Mark();
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

        int[] lastGCCounts = new int[3];

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(frame, elapsedTime, updateFull);
#if SHOW_PHYSICS_GRAPHS
            if (Visible && TextPages[TextPage] == TextPageForceInfo)
            {
                var loco = Viewer.PlayerLocomotive as MSTSLocomotive;
                ForceGraphMotiveForce.AddSample(loco.MotiveForceN / loco.MaxForceN);
                ForceGraphDynamicForce.AddSample(-loco.MotiveForceN / loco.MaxForceN);
                ForceGraphNumOfSubsteps.AddSample((float)loco.LocomotiveAxle.AxleRevolutionsInt.NumOfSubstepsPS / (float)loco.LocomotiveAxle.AxleRevolutionsInt.MaxSubsteps);

                ForceGraphs.PrepareFrame(frame);
            }

            if (Visible && TextPages[TextPage] == TextPageLocomotiveInfo)
            {
                var loco = Viewer.PlayerLocomotive as MSTSLocomotive;
                var locoD = Viewer.PlayerLocomotive as MSTSDieselLocomotive;
                var locoE = Viewer.PlayerLocomotive as MSTSElectricLocomotive;
                var locoS = Viewer.PlayerLocomotive as MSTSSteamLocomotive;
                LocomotiveGraphsThrottle.AddSample(loco.ThrottlePercent * 0.01f);
                if (locoD != null)
                {
                    LocomotiveGraphsInputPower.AddSample(locoD.DieselEngines.MaxOutputPowerW / locoD.DieselEngines.MaxPowerW);
                    LocomotiveGraphsOutputPower.AddSample(locoD.DieselEngines.PowerW / locoD.DieselEngines.MaxPowerW);
                }
                if (locoE != null)
                {
                    LocomotiveGraphsInputPower.AddSample(loco.ThrottlePercent * 0.01f);
                    LocomotiveGraphsOutputPower.AddSample((loco.MotiveForceN / loco.MaxPowerW) * loco.SpeedMpS);
                }
                //TODO: plot correct values
                if (locoS != null)
                {
                    LocomotiveGraphsInputPower.AddSample(loco.ThrottlePercent * 0.01f);
                    LocomotiveGraphsOutputPower.AddSample((loco.MotiveForceN / loco.MaxPowerW) * loco.SpeedMpS);
                }

                LocomotiveGraphs.PrepareFrame(frame);
            }
#endif
            if (Visible && TextPages[TextPage] == TextPageDebugInfo)
            {
                var gcCounts = new[] { GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2) };
                DebugGraphMemory.AddSample((float)GetWorkingSetSize() / ProcessVirtualAddressLimit);
                DebugGraphGCs.AddSample(gcCounts[2] > lastGCCounts[2] ? 1.0f : gcCounts[1] > lastGCCounts[1] ? 0.5f : gcCounts[0] > lastGCCounts[0] ? 0.25f : 0);
                DebugGraphFrameTime.AddSample(Viewer.RenderProcess.FrameTime.Value * 10);
                DebugGraphProcessRender.AddSample(Viewer.RenderProcess.Profiler.Wall.Value / 100);
                DebugGraphProcessUpdater.AddSample(Viewer.UpdaterProcess.Profiler.Wall.Value / 100);
                DebugGraphProcessLoader.AddSample(Viewer.LoaderProcess.Profiler.Wall.Value / 100);
                DebugGraphProcessSound.AddSample(Viewer.SoundProcess.Profiler.Wall.Value / 100);
                lastGCCounts = gcCounts;
                DebugGraphs.PrepareFrame(frame);
            }
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
                        TextFont.Draw(spriteBatch, new Rectangle(TextOffset + column * ColumnWidth, TextOffset + row * TextFont.Height, ColumnWidth, TextFont.Height), Point.Zero, TextTable.Cells[row, column], TextTable.Cells[row, column].StartsWith(" ") ? LabelAlignment.Right : LabelAlignment.Left, Color.White);

#if SHOW_PHYSICS_GRAPHS
            if (Visible && TextPages[TextPage] == TextPageForceInfo)
                ForceGraphs.Draw(spriteBatch);
            if (Visible && TextPages[TextPage] == TextPageLocomotiveInfo)
                LocomotiveGraphs.Draw(spriteBatch);
#endif
            if (Visible && TextPages[TextPage] == TextPageDebugInfo)
                DebugGraphs.Draw(spriteBatch);
        }

        #region Table handling
        sealed class TableData
        {
            public string[,] Cells;
            public int CurrentRow;
            public int CurrentLabelColumn;
            public int CurrentValueColumn;
        }

        static void TableSetCell(TableData table, int cellColumn, string format, params object[] args)
        {
            TableSetCell(table, table.CurrentRow, cellColumn, format, args);
        }

        static void TableSetCell(TableData table, int cellRow, int cellColumn, string format, params object[] args)
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

        static void TableSetCells(TableData table, int startColumn, params string[] columns)
        {
            for (var i = 0; i < columns.Length; i++)
                TableSetCell(table, startColumn + i, columns[i]);
        }

        static void TableAddLine(TableData table)
        {
            table.CurrentRow++;
        }
        
        static void TableAddLine(TableData table, string format, params object[] args)
        {
            TableSetCell(table, table.CurrentRow, 0, format, args);
            table.CurrentRow++;
        }

        static void TableAddLines(TableData table, string lines)
        {
            if (lines == null)
                return;

            foreach (var line in lines.Split('\n'))
            {
                var column = 0;
                foreach (var cell in line.Split('\t'))
                    TableSetCell(table, column++, "{0}", cell);
                table.CurrentRow++;
            }
        }

        static void TableSetLabelValueColumns(TableData table, int labelColumn, int valueColumn)
        {
            table.CurrentLabelColumn = labelColumn;
            table.CurrentValueColumn = valueColumn;
        }

        static void TableAddLabelValue(TableData table, string label, string format, params object[] args)
        {
            TableSetCell(table, table.CurrentRow, table.CurrentLabelColumn, label);
            TableSetCell(table, table.CurrentRow, table.CurrentValueColumn, format, args);
            table.CurrentRow++;
        }
        #endregion

        void TextPageCommon(TableData table)
        {
            var playerTrain = Viewer.PlayerLocomotive.Train;
            var showMUReverser = Math.Abs(playerTrain.MUReverserPercent) != 100;
            var showRetainers = playerTrain.RetainerSetting != RetainerSetting.Exhaust;
            var engineBrakeStatus = Viewer.PlayerLocomotive.GetEngineBrakeStatus((Viewer.PlayerLocomotive as MSTSLocomotive).PressureUnit);
            var dynamicBrakeStatus = Viewer.PlayerLocomotive.GetDynamicBrakeStatus();
            var locomotiveStatus = Viewer.PlayerLocomotive.GetStatus();
            var stretched = playerTrain.Cars.Count > 1 && playerTrain.NPull == playerTrain.Cars.Count - 1;
            var bunched = !stretched && playerTrain.Cars.Count > 1 && playerTrain.NPush == playerTrain.Cars.Count - 1;

            TableSetLabelValueColumns(table, 0, 2);
            TableAddLabelValue(table, "Version", VersionInfo.VersionOrBuild);

            if (MultiPlayer.MPManager.IsClient()) //client and server may have time difference
                TableAddLabelValue(table, "Time", InfoDisplay.FormattedTime(Viewer.Simulator.ClockTime + MultiPlayer.MPManager.Instance().serverTimeDifference));
            else TableAddLabelValue(table, "Time", InfoDisplay.FormattedTime(Viewer.Simulator.ClockTime));

            if (Viewer.IsReplaying)
            {
                TableAddLabelValue(table, "Replay", InfoDisplay.FormattedTime(Viewer.Log.ReplayEndsAt - Viewer.Simulator.ClockTime));
            }

            TableAddLabelValue(table, "Speed", FormatStrings.FormatSpeed(Viewer.PlayerLocomotive.SpeedMpS, Viewer.MilepostUnitsMetric));
            TableAddLabelValue(table, "Direction", showMUReverser ? "{1:F0} {0}" : "{0}", Viewer.PlayerLocomotive.Direction, Math.Abs(playerTrain.MUReverserPercent));
            TableAddLabelValue(table, "Throttle", "{0:F0}%", Viewer.PlayerLocomotive.ThrottlePercent);
            TableAddLabelValue(table, "Train brake", "{0}", Viewer.PlayerLocomotive.GetTrainBrakeStatus((Viewer.PlayerLocomotive as MSTSLocomotive).PressureUnit));
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
            if (Viewer.PlayerLocomotive is MSTSElectricLocomotive)
            {
                MSTSElectricLocomotive loco = Viewer.PlayerLocomotive as MSTSElectricLocomotive;

                StringBuilder pantographStatus = new StringBuilder();
                foreach (Pantograph pantograph in loco.Pantographs.List)
                {
                    pantographStatus.AppendFormat("{0} {1}", pantograph.Id, pantograph.State.ToString());
                    if (pantograph != loco.Pantographs.List.Last())
                    {
                        pantographStatus.Append(" ");
                    }
                }

                StringBuilder cbAuthorization = new StringBuilder();
                cbAuthorization.Append("TCS ");
                if (loco.TrainControlSystem.PowerAuthorization)
                    cbAuthorization.Append("OK");
                else
                    cbAuthorization.Append("NOTOK");
                cbAuthorization.Append(", driver ");
                if (loco.PowerSupply.CircuitBreaker.DriverCloseAuthorization)
                    cbAuthorization.Append("OK");
                else
                    cbAuthorization.Append("NOTOK");

                TableAddLabelValue(table, "Pantographs", pantographStatus.ToString());
                TableAddLabelValue(table, "CB authorization", cbAuthorization.ToString());
                TableAddLabelValue(table, "Circuit breaker", loco.PowerSupply.CircuitBreaker.State.ToString());
                TableAddLabelValue(table, "Electric power", loco.PowerSupply.State.ToString());
                TableAddLabelValue(table, "Auxiliary power", loco.PowerSupply.AuxiliaryState.ToString());
            }
            else if (locomotiveStatus != null)
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
            if (Viewer.PlayerLocomotive.Train.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING) TableAddLine(table, "Autopilot");
            TableAddLine(table);
            TableAddLabelValue(table, "FPS", "{0:F0}", Viewer.RenderProcess.FrameRate.SmoothedValue);
            TableAddLine(table);

            if (Viewer.PlayerTrain.IsWheelSlip)
                TableAddLine(table, "Wheel slip");
            else if (Viewer.PlayerTrain.IsWheelSlipWarninq)
                TableAddLine(table, "Wheel slip warning");
            if (Viewer.PlayerLocomotive.GetSanderOn())
            {
                if (Math.Abs(playerTrain.SpeedMpS) < ((MSTSLocomotive)Viewer.PlayerLocomotive).SanderSpeedOfMpS)
                    TableAddLine(table, "Sander on");
                else
                    TableAddLine(table, "Sander blocked");
            }

            if (MultiPlayer.MPManager.IsMultiPlayer())
            {
                var status = "MultiPlayerStatus: ";
                if (MultiPlayer.MPManager.IsServer()) status += "dispatcher";
                else if (MultiPlayer.MPManager.Instance().AmAider) status += "helper";
                else if (MultiPlayer.MPManager.IsClient()) status += "client";
                TableAddLine(table, status);
                var text = MultiPlayer.MPManager.Instance().GetOnlineUsersInfo();
                var temp = text.Split('\t');
                foreach (var t in temp) TableAddLabelValue(table, "", "{0}", t);
            }
        }

        void TextPageConsistInfo(TableData table)
        {
            TextPageHeading(table, "CONSIST INFORMATION");

            var locomotive = Viewer.PlayerLocomotive;
            var mstsLocomotive = locomotive as MSTSLocomotive;
            var train = locomotive.Train;

            TableSetCells(table, 0, "Player", "Tilted", "Type", "Length", "Weight", "Control Mode", "", "Out of Control", "", "Cab Aspect");
            TableAddLine(table);
            TableSetCells(table, 0, locomotive.UiD + " " + (mstsLocomotive == null ? "" : mstsLocomotive.UsingRearCab ? "R" : "F"), train.tilted.ToString(), train.IsFreight ? "Freight" : "Pass", FormatStrings.FormatDistance(train.Length, true), FormatStrings.FormatMass(train.MassKg, true) , train.ControlMode.ToString(), "", train.OutOfControlReason.ToString(), "", mstsLocomotive.TrainControlSystem.CabSignalAspect.ToString());
            TableAddLine(table);
            TableAddLine(table);
            TableSetCells(table, 0, "Car", "Flipped", "Type", "Length", "Weight", "Drv/Cabs", "Wheels");
            TableAddLine(table);
            foreach (var car in train.Cars.Take(20))
            {
                TableSetCells(table, 0, car.UiD.ToString(), car.Flipped.ToString(), train.IsFreight ? "Freight" : "Pass", FormatStrings.FormatDistance(car.LengthM, true), FormatStrings.FormatMass(car.MassKG, true), (car.IsDriveable ? "D" : "") + (car.HasFrontCab ? "F" : "") + (car.HasRearCab ? "R" : ""), GetCarWhyteLikeNotation(car));
                TableAddLine(table);
            }
        }

        static string GetCarWhyteLikeNotation(TrainCar car)
        {
            if (car.WheelAxles.Count == 0)
                return "";

            var whyte = new List<string>();
            var currentCount = 0;
            var currentBogie = car.WheelAxles[0].BogieIndex;
            foreach (var axle in car.WheelAxles)
            {
                if (currentBogie != axle.BogieIndex)
                {
                    whyte.Add(currentCount.ToString());
                    currentBogie = axle.BogieIndex;
                    currentCount = 0;
                }
                currentCount += 2;
            }
            whyte.Add(currentCount.ToString());
            return String.Join("-", whyte.ToArray());
        }

        void TextPageLocomotiveInfo(TableData table)
        {
            TextPageHeading(table, "LOCOMOTIVE INFORMATION");

            var locomotive = Viewer.PlayerLocomotive;
            var train = locomotive.Train;

            TableAddLines(table, String.Format("Direction\t{0}\tReverser\t{1:F0}%\tThrottle\t{2:F0}%\tD-brake\t{3:F0}%", train.MUDirection, train.MUReverserPercent, train.MUThrottlePercent, train.MUDynamicBrakePercent));
            TableAddLine(table);
            foreach (var car in train.Cars)
                if (car is MSTSLocomotive)
                    TableAddLines(table, car.GetDebugStatus());
        }

        void TextPageBrakeInfo(TableData table)
        {
            TextPageHeading(table, "BRAKE INFORMATION");

            var train = Viewer.PlayerLocomotive.Train;
            TableAddLabelValue(table, "Main reservoir", "{0}", FormatStrings.FormatPressure(train.BrakeLine2PressurePSI, PressureUnit.PSI, (Viewer.PlayerLocomotive as MSTSLocomotive).PressureUnit, true));

            var n = train.Cars.Count; // Number of lines to show
            for (var i = 0; i < n; i++)
            {
                var j = i < 2 ? i : i * (train.Cars.Count - 1) / (n - 1);
                var car = train.Cars[j];
                TableSetCell(table, 0, "{0}", train.Cars[j].CarID);
                TableSetCells(table, 1, car.BrakeSystem.GetDebugStatus((Viewer.PlayerLocomotive as MSTSLocomotive).PressureUnit));
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
                    TableAddLabelValue(table, "Conditions", "{0:F0}%", mstsLocomotive.LocomotiveAxle.AdhesionConditions * 10f);
                    TableAddLabelValue(table, "Axle drive force", "{0:F0} N", mstsLocomotive.LocomotiveAxle.DriveForceN);
                    TableAddLabelValue(table, "Axle brake force", "{0:F0} N", mstsLocomotive.LocomotiveAxle.BrakeForceN);
                    TableAddLabelValue(table, "Num of substeps", "{0:F0} (filtered by {1:F0})", mstsLocomotive.LocomotiveAxle.AxleRevolutionsInt.NumOfSubstepsPS,
                                                                                               mstsLocomotive.LocomotiveAxle.FilterMovingAverage.Size);
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

            TableSetCells(table, 0, "Car", "Total", "Motive", "Brake", "Friction", "Gravity", "Curve", "Coupler", "Mass", "Elev", "Notes");
            TableAddLine(table);

            var n = Math.Min(10, train.Cars.Count);
            for (var i = 0; i < n; i++)
            {
                var j = i == 0 ? 0 : i * (train.Cars.Count - 1) / (n - 1);
                var car = train.Cars[j];
                TableSetCell(table, 0, "{0}", j + 1);
                TableSetCell(table, 1, "{0:F0}", car.TotalForceN);
                TableSetCell(table, 2, "{0:F0}", car.MotiveForceN);
                TableSetCell(table, 3, "{0:F0}", car.BrakeForceN);
                TableSetCell(table, 4, "{0:F0}", car.FrictionForceN);
                TableSetCell(table, 5, "{0:F0}", car.GravityForceN);
                TableSetCell(table, 6, "{0:F2}", car.CurveForceN);
                TableSetCell(table, 7, "{0:F0}", car.CouplerForceU);
                TableSetCell(table, 8, "{0:F0}", car.MassKG);
                TableSetCell(table, 9, "{0:F2}", -car.CurrentElevationPercent);
                TableSetCell(table, 10, car.Flipped ? "Flipped" : "");
                TableSetCell(table, 11, car.CouplerOverloaded ? "Coupler overloaded" : "");
                TableAddLine(table);
            }
        }

        void TextPageDispatcherInfo(TableData table)
        {
            TextPageHeading(table, "DISPATCHER INFORMATION");
            TableSetCells(table, 0, "Train", "Travelled", "Speed", "Max", "AI mode", "AI data", "Mode", "Auth", "Distance", "Signal", "Distance", "Consist", "Path");
            TableAddLine(table);

            // first is player train
            foreach (var thisTrain in Viewer.Simulator.Trains)
            {
                if (thisTrain.TrainType == Train.TRAINTYPE.PLAYER || (thisTrain.TrainType == Train.TRAINTYPE.REMOTE && MultiPlayer.MPManager.IsServer())
                    ||(thisTrain.Number == 0 && thisTrain.IsActualPlayerTrain))
                {
                    var status = thisTrain.GetStatus(Viewer.MilepostUnitsMetric);
                    if (thisTrain.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING) status = ((AITrain)thisTrain).AddMovementState(status, Viewer.MilepostUnitsMetric);
                    for (var iCell = 0; iCell < status.Length; iCell++)
                        TableSetCell(table, table.CurrentRow, iCell, status[iCell]);
                    TableAddLine(table);
                }
            }

            // next is active AI trains
            foreach (var thisTrain in Viewer.Simulator.AI.AITrains)
            {
                if (thisTrain.MovementState != AITrain.AI_MOVEMENT_STATE.AI_STATIC)
                {
                    var status = thisTrain.GetStatus(Viewer.MilepostUnitsMetric);
                    status = thisTrain.AddMovementState(status, Viewer.MilepostUnitsMetric);
                    for (var iCell = 0; iCell < status.Length; iCell++)
                        TableSetCell(table, table.CurrentRow, iCell, status[iCell]);
                    TableAddLine(table);
                }
            }

            // finally is static AI trains
            foreach (var thisTrain in Viewer.Simulator.AI.AITrains)
            {
                if (thisTrain.MovementState == AITrain.AI_MOVEMENT_STATE.AI_STATIC)
                {
                    var status = thisTrain.GetStatus(Viewer.MilepostUnitsMetric);
                    status = thisTrain.AddMovementState(status, Viewer.MilepostUnitsMetric);
                    for (var iCell = 0; iCell < status.Length; iCell++)
                        TableSetCell(table, table.CurrentRow, iCell, status[iCell]);
                    TableAddLine(table);
                }
            }
#if WITH_PATH_DEBUG
            TextPageHeading(table, "PATH info");

            TableSetCells(table, 0, "Train", "Path ");
            TableSetCells(table, 8, "Type", "Info");
            TableAddLine(table);

            foreach (var thisTrain in Viewer.Simulator.AI.AITrains)
            {
                if (thisTrain.MovementState != AITrain.AI_MOVEMENT_STATE.AI_STATIC)
                {
                    TextPagePathInfo(thisTrain, table);
                }
            }
            TextPageHeading(table, "ACTIONs info");

            TableSetCells(table, 0, "Train", "Actions ");
            TableAddLine(table);

            foreach (var thisTrain in Viewer.Simulator.AI.AITrains)
            {
                if (thisTrain.MovementState != AITrain.AI_MOVEMENT_STATE.AI_STATIC)
                {
                    TextPageActionsInfo(thisTrain, table);
                }
            }
#endif

        }
#if WITH_PATH_DEBUG
        void TextPagePathInfo(AITrain thisTrain, TableData table)
        {
            // next is active AI trains
            if (thisTrain.MovementState != AITrain.AI_MOVEMENT_STATE.AI_STATIC)
            {
                var status = thisTrain.GetPathStatus(Viewer.MilepostUnitsMetric);
                status = thisTrain.AddPathInfo(status, Viewer.MilepostUnitsMetric);
                for (var iCell = 0; iCell < status.Length; iCell++)
                    TableSetCell(table, table.CurrentRow, iCell, status[iCell]);
                TableAddLine(table);
            }
        }

        void TextPageActionsInfo(AITrain thisTrain, TableData table)
        {
            // next is active AI trains
            if (thisTrain.MovementState != AITrain.AI_MOVEMENT_STATE.AI_STATIC)
            {
                var status = thisTrain.GetActionStatus(Viewer.MilepostUnitsMetric);
                for (var iCell = 0; iCell < status.Length; iCell++)
                    TableSetCell(table, table.CurrentRow, iCell, status[iCell]);
                TableAddLine(table);
            }
        }
#endif

        void TextPageDebugInfo(TableData table)
        {
            TextPageHeading(table, "DEBUG INFORMATION");

            var allocatedBytesPerSecond = AllocatedBytesPerSecCounter.NextValue();
            if (allocatedBytesPerSecond >= 1 && AllocatedBytesPerSecLastValue != allocatedBytesPerSecond)
                AllocatedBytesPerSecLastValue = allocatedBytesPerSecond;

            TableAddLabelValue(table, "Logging enabled", "{0}", Viewer.Settings.DataLogger);
            TableAddLabelValue(table, "Build", "{0}", VersionInfo.Build);
            TableAddLabelValue(table, "Memory", "{0:F0} MB ({5}, {6}, {7}, {8}, {1:F0} MB managed, {9:F0} kB/frame allocated, {2:F0}/{3:F0}/{4:F0} GCs)", GetWorkingSetSize() / 1024 / 1024, GC.GetTotalMemory(false) / 1024 / 1024, GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2), Viewer.TextureManager.GetStatus(), Viewer.MaterialManager.GetStatus(), Viewer.ShapeManager.GetStatus(), Viewer.World.Terrain.GetStatus(), AllocatedBytesPerSecLastValue / Viewer.RenderProcess.FrameRate.SmoothedValue / 1024);
            TableAddLabelValue(table, "CPU", "{0:F0}% ({1} logical processors)", (Viewer.RenderProcess.Profiler.CPU.SmoothedValue + Viewer.UpdaterProcess.Profiler.CPU.SmoothedValue + Viewer.LoaderProcess.Profiler.CPU.SmoothedValue + Viewer.SoundProcess.Profiler.CPU.SmoothedValue) / ProcessorCount, ProcessorCount);
            TableAddLabelValue(table, "GPU", "{0:F0} FPS (50th/95th/99th percentiles {1:F1} / {2:F1} / {3:F1} ms, shader model {4})", Viewer.RenderProcess.FrameRate.SmoothedValue, Viewer.RenderProcess.FrameTime.SmoothedP50 * 1000, Viewer.RenderProcess.FrameTime.SmoothedP95 * 1000, Viewer.RenderProcess.FrameTime.SmoothedP99 * 1000, Viewer.Settings.ShaderModel);
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
            TableSetCells(table, 0, "Camera", "", Viewer.Camera.TileX.ToString("F0"), Viewer.Camera.TileZ.ToString("F0"), Viewer.Camera.Location.X.ToString("F2"), Viewer.Camera.Location.Y.ToString("F2"), Viewer.Camera.Location.Z.ToString("F2"), Viewer.Tiles.GetElevation(Viewer.Camera.CameraWorldLocation).ToString("F1") + " m", Viewer.Settings.ViewingDistance + " m", Viewer.Settings.DistantMountains ? ((float)Viewer.Settings.DistantMountainsViewingDistance / 1000).ToString("F0") + " km" : "");
            TableAddLine(table);
        }

        static void TextPageHeading(TableData table, string name)
        {
            TableAddLine(table);
            TableAddLine(table, name);
        }

        #region Native code
        [StructLayout(LayoutKind.Sequential, Size = 64)]
        public class MEMORYSTATUSEX
        {
            public uint Size;
            public uint MemoryLoad;
            public ulong TotalPhysical;
            public ulong AvailablePhysical;
            public ulong TotalPageFile;
            public ulong AvailablePageFile;
            public ulong TotalVirtual;
            public ulong AvailableVirtual;
            public ulong AvailableExtendedVirtual;
        }

        [StructLayout(LayoutKind.Sequential, Size = 40)]
        struct PROCESS_MEMORY_COUNTERS
        {
            public int Size;
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

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX buffer);

        [DllImport("psapi.dll", SetLastError = true)]
        static extern bool GetProcessMemoryInfo(IntPtr hProcess, out PROCESS_MEMORY_COUNTERS counters, int size);

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(int dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

        readonly IntPtr ProcessHandle;
        PROCESS_MEMORY_COUNTERS ProcessMemoryCounters;
        readonly ulong ProcessVirtualAddressLimit;
        #endregion

        public uint GetWorkingSetSize()
        {
            // Get memory usage (working set).
            GetProcessMemoryInfo(ProcessHandle, out ProcessMemoryCounters, ProcessMemoryCounters.Size);
            return (uint)ProcessMemoryCounters.WorkingSetSize;
        }

        public ulong GetVirtualAddressLimit()
        {
            var buffer = new MEMORYSTATUSEX { Size = 64 };
            GlobalMemoryStatusEx(buffer);
            return Math.Min(buffer.TotalVirtual, buffer.TotalPhysical);
        }
    }

    public class HUDGraphSet
    {
        readonly Viewer Viewer;
        readonly Material Material;
        readonly Vector2 Margin = new Vector2(40, 10);
        readonly int Spacing;
        readonly List<Graph> Graphs = new List<Graph>();

        public HUDGraphSet(Viewer viewer, Material material)
        {
            Viewer = viewer;
            Material = material;
            Spacing = Viewer.WindowManager.TextFontSmallOutlined.Height + 2;
        }

        public HUDGraphMesh AddOverlapped(Color color, int height)
        {
            return Add("", "", "", color, height, true);
        }

        public HUDGraphMesh Add(string labelName, string labelMin, string labelMax, Color color, int height)
        {
            return Add(labelName, labelMin, labelMax, color, height, false);
        }

        HUDGraphMesh Add(string labelName, string labelMin, string labelMax, Color color, int height, bool overlapped)
        {
            HUDGraphMesh mesh;
            Graphs.Add(new Graph()
            {
                Mesh = mesh = new HUDGraphMesh(Viewer, color, height),
                LabelName = labelName,
                LabelMin = labelMin,
                LabelMax = labelMax,
                Overlapped = overlapped,
            });
            for (var i = Graphs.Count - 1; i >= 0; i--)
            {
                var previousGraphs = Graphs.Skip(i + 1).Where(g => !g.Overlapped);
                Graphs[i].YOffset = (int)previousGraphs.Sum(g => g.Mesh.GraphPos.W) + Spacing * previousGraphs.Count();
            }
            return mesh;
        }

        public void PrepareFrame(RenderFrame frame)
        {
            var matrix = Matrix.Identity;
            for (var i = 0; i < Graphs.Count ; i++)
            {
                Graphs[i].Mesh.GraphPos.X = Viewer.DisplaySize.X - Margin.X - Graphs[i].Mesh.GraphPos.Z;
                Graphs[i].Mesh.GraphPos.Y = Margin.Y + Graphs[i].YOffset;
                frame.AddPrimitive(Material, Graphs[i].Mesh, RenderPrimitiveGroup.Overlay, ref matrix);
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            var box = new Rectangle();
            for (var i = 0; i < Graphs.Count; i++)
            {
                if (!string.IsNullOrEmpty(Graphs[i].LabelName))
                {
                    box.X = (int)Graphs[i].Mesh.GraphPos.X;
                    box.Y = Viewer.DisplaySize.Y - (int)Graphs[i].Mesh.GraphPos.Y - (int)Graphs[i].Mesh.GraphPos.W - Spacing;
                    box.Width = (int)Graphs[i].Mesh.GraphPos.Z;
                    box.Height = Spacing;
                    Viewer.WindowManager.TextFontSmallOutlined.Draw(spriteBatch, box, Point.Zero, Graphs[i].LabelName, LabelAlignment.Right, Color.White);
                    box.X = box.Right + 3;
                    box.Y += Spacing - 3;
                    Viewer.WindowManager.TextFontSmallOutlined.Draw(spriteBatch, box.Location, Graphs[i].LabelMax, Color.White);
                    box.Y += (int)Graphs[i].Mesh.GraphPos.W - Spacing + 7;
                    Viewer.WindowManager.TextFontSmallOutlined.Draw(spriteBatch, box.Location, Graphs[i].LabelMin, Color.White);
                }
            }
        }

        class Graph
        {
            public HUDGraphMesh Mesh;
            public string LabelName;
            public string LabelMin;
            public string LabelMax;
            public int YOffset;
            public bool Overlapped;
        }
    }

    public class HUDGraphMesh : RenderPrimitive
    {
        const int SampleCount = 1024 - 10 - 40; // Widest graphs we can fit in 1024x768.
        const int VerticiesPerSample = 6;
        const int PrimitivesPerSample = 2;
        const int VertexCount = VerticiesPerSample * SampleCount;

        readonly VertexDeclaration VertexDeclaration;
        readonly DynamicVertexBuffer VertexBuffer;
        readonly VertexBuffer BorderVertexBuffer;
        readonly Color Color;

        int SampleIndex;
        VertexPositionColor[] Samples = new VertexPositionColor[VertexCount];

        public Vector4 GraphPos; // xy = xy position, zw = width/height
        public Vector2 Sample; // x = index, y = count

        public HUDGraphMesh(Viewer viewer, Color color, int height)
        {
            VertexDeclaration = new VertexDeclaration(viewer.GraphicsDevice, VertexPositionColor.VertexElements);
            VertexBuffer = new DynamicVertexBuffer(viewer.GraphicsDevice, VertexCount * VertexPositionColor.SizeInBytes, BufferUsage.WriteOnly);
            VertexBuffer.ContentLost += VertexBuffer_ContentLost;
            BorderVertexBuffer = new VertexBuffer(viewer.GraphicsDevice, 10 * VertexPositionColor.SizeInBytes, BufferUsage.WriteOnly);
            var borderOffset = new Vector2(1f / SampleCount, 1f / height);
            var borderColor = new Color(Color.White, 0);
            BorderVertexBuffer.SetData(new[] {
                // Bottom left
                new VertexPositionColor(new Vector3(0 - borderOffset.X, 0 - borderOffset.Y, 1), borderColor),
                new VertexPositionColor(new Vector3(0, 0, 1), borderColor),
                // Bottom right
                new VertexPositionColor(new Vector3(1 + borderOffset.X, 0 - borderOffset.Y, 0), borderColor),
                new VertexPositionColor(new Vector3(1, 0, 0), borderColor),
                // Top right
                new VertexPositionColor(new Vector3(1 + borderOffset.X, 1 + borderOffset.Y, 0), borderColor),
                new VertexPositionColor(new Vector3(1, 1, 0), borderColor),
                // Top left
                new VertexPositionColor(new Vector3(0 - borderOffset.X, 1 + borderOffset.Y, 1), borderColor),
                new VertexPositionColor(new Vector3(0, 1, 1), borderColor),
                // Bottom left
                new VertexPositionColor(new Vector3(0 - borderOffset.X, 0 - borderOffset.Y, 1), borderColor),
                new VertexPositionColor(new Vector3(0, 0, 1), borderColor),
            });
            Color = color;
            Color.A = 255;
            GraphPos.Z = SampleCount;
            GraphPos.W = height;
            Sample.Y = SampleCount;
        }

        void VertexBuffer_ContentLost(object sender, EventArgs e)
        {
            VertexBuffer.SetData(0, Samples, 0, Samples.Length, VertexPositionColor.SizeInBytes, SetDataOptions.NoOverwrite);
        }

        public void AddSample(float value)
        {
            value = MathHelper.Clamp(value, 0, 1);
            var x = Sample.X / Sample.Y;

            Samples[(int)Sample.X * VerticiesPerSample + 0] = new VertexPositionColor(new Vector3(x, value, 0), Color);
            Samples[(int)Sample.X * VerticiesPerSample + 1] = new VertexPositionColor(new Vector3(x, value, 1), Color);
            Samples[(int)Sample.X * VerticiesPerSample + 2] = new VertexPositionColor(new Vector3(x, 0, 1), Color);
            Samples[(int)Sample.X * VerticiesPerSample + 3] = new VertexPositionColor(new Vector3(x, 0, 1), Color);
            Samples[(int)Sample.X * VerticiesPerSample + 4] = new VertexPositionColor(new Vector3(x, value, 0), Color);
            Samples[(int)Sample.X * VerticiesPerSample + 5] = new VertexPositionColor(new Vector3(x, 0, 0), Color);
            VertexBuffer.SetData((int)Sample.X * VerticiesPerSample * VertexPositionColor.SizeInBytes, Samples, (int)Sample.X * VerticiesPerSample, VerticiesPerSample, VertexPositionColor.SizeInBytes, SetDataOptions.NoOverwrite);

            SampleIndex = (SampleIndex + 1) % SampleCount;
            Sample.X = SampleIndex;
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.VertexDeclaration = VertexDeclaration;

            // Draw border
            graphicsDevice.Vertices[0].SetSource(BorderVertexBuffer, 0, VertexPositionColor.SizeInBytes);
            graphicsDevice.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 8);

            // Draw graph area (skipping the next value to be written)
            graphicsDevice.Vertices[0].SetSource(VertexBuffer, 0, VertexPositionColor.SizeInBytes);
            if (SampleIndex > 0)
                graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, SampleIndex * PrimitivesPerSample);
            if (SampleIndex + 1 < SampleCount)
                graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, (SampleIndex + 1) * VerticiesPerSample, (SampleCount - SampleIndex - 1) * PrimitivesPerSample);
        }
    }

    public class HUDGraphMaterial : Material
    {
        IEnumerator<EffectPass> ShaderPassesGraph;

        public HUDGraphMaterial(Viewer viewer)
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
                    var graphMesh = item.RenderPrimitive as HUDGraphMesh;
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
}
