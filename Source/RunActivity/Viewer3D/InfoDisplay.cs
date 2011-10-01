// COPYRIGHT 2009, 2010, 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ORTS.Popups;

namespace ORTS
{
    /// <summary>
    /// Displays Viewer frame rate and Viewer.Text debug messages in the upper left corner of the screen.
    /// </summary>
    public class InfoDisplay
    {
        readonly Viewer3D Viewer;
        readonly DataLogger Logger = new DataLogger();
        readonly int ProcessorCount = System.Environment.ProcessorCount;

        bool DrawCarNumber = false;
        // F6 reveals labels for both sidings and platforms.
        // Booleans for both so they can also be used independently.
        bool DrawSiding = false;
        bool DrawPlatform = false;

        SpriteBatchMaterial TextMaterial;
        ActivityInforMaterial DrawInforMaterial;

        Matrix Identity = Matrix.Identity;
        int FrameNumber = 0;
        double LastUpdateRealTime = 0;   // update text message only 10 times per second

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

        public InfoDisplay(Viewer3D viewer)
        {
            Viewer = viewer;
            TextMaterial = (SpriteBatchMaterial)Materials.Load(Viewer.RenderProcess, "SpriteBatch");
            DrawInforMaterial = (ActivityInforMaterial)Materials.Load(Viewer.RenderProcess, "DrawInforMaterial");

            ProcessHandle = OpenProcess(0x410 /* PROCESS_QUERY_INFORMATION | PROCESS_VM_READ */, false, Process.GetCurrentProcess().Id);
            ProcessMemoryCounters = new PROCESS_MEMORY_COUNTERS() { cb = 40 };

            if (Viewer.Settings.DataLogger)
                DataLoggerStart();
        }

        public void Stop()
        {
            if (Viewer.Settings.DataLogger)
                DataLoggerStop();
        }

        public void HandleUserInput(ElapsedTime elapsedTime)
        {
            if (UserInput.IsPressed(UserCommands.DebugLogger))
            {
                Viewer.Settings.DataLogger = !Viewer.Settings.DataLogger;
                if (Viewer.Settings.DataLogger)
                    DataLoggerStart();
                else
                    DataLoggerStop();
            }
            if (UserInput.IsPressed(UserCommands.DisplayCarLabels))
                DrawCarNumber = !DrawCarNumber;
            if (UserInput.IsPressed(UserCommands.DisplayStationLabels))
            {
                // Cycles round 4 states
                // none > both > sidings only > platforms only > none
                // MSTS users will first see the 2 states they expect and then discover the extra two. 
                if (DrawSiding == false && DrawPlatform == false)
                {
                    DrawSiding = true;
                    DrawPlatform = true;
                }
                else
                {
                    if (DrawSiding == true && DrawPlatform == true)
                    {
                        DrawSiding = false;
                        DrawPlatform = true;
                    }
                    else
                    {
                        if (DrawSiding == false && DrawPlatform == true)
                        {
                            DrawSiding = true;
                            DrawPlatform = false;
                        }
                        else
                        {
                            DrawSiding = false;
                            DrawPlatform = false;
                        }
                    }
                }
            }
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            FrameNumber++;

            if (Viewer.RealTime - LastUpdateRealTime >= 0.25)
            {
                double elapsedRealSeconds = Viewer.RealTime - LastUpdateRealTime;
                LastUpdateRealTime = Viewer.RealTime;
                Profile(elapsedRealSeconds);
            }

            //Here's where the logger stores the data from each frame
            if (Viewer.Settings.DataLogger)
            {
                Logger.Data(Program.Version);
                Logger.Data(FrameNumber.ToString("F0"));
                Logger.Data(GetWorkingSetSize().ToString("F0"));
                Logger.Data(GC.GetTotalMemory(false).ToString("F0"));
                Logger.Data(GC.CollectionCount(0).ToString("F0"));
                Logger.Data(GC.CollectionCount(1).ToString("F0"));
                Logger.Data(GC.CollectionCount(2).ToString("F0"));
                Logger.Data(ProcessorCount.ToString("F0"));
                Logger.Data(Viewer.RenderProcess.FrameRate.Value.ToString("F0"));
                Logger.Data(Viewer.RenderProcess.FrameTime.Value.ToString("F4"));
                Logger.Data(Viewer.RenderProcess.FrameJitter.Value.ToString("F4"));
                Logger.Data(Viewer.RenderProcess.ShadowPrimitivePerFrame.Sum().ToString("F0"));
                Logger.Data(Viewer.RenderProcess.PrimitivePerFrame.Sum().ToString("F0"));
                Logger.Data(Viewer.RenderProcess.Profiler.Wall.Value.ToString("F0"));
                Logger.Data(Viewer.UpdaterProcess.Profiler.Wall.Value.ToString("F0"));
                Logger.Data(Viewer.LoaderProcess.Profiler.Wall.Value.ToString("F0"));
                Logger.Data(Viewer.SoundProcess.Profiler.Wall.Value.ToString("F0"));
                Logger.Data(FormattedTime(Viewer.Simulator.ClockTime));
                Logger.Data(Viewer.PlayerLocomotive.Direction.ToString());
                Logger.Data(Viewer.PlayerTrain.MUReverserPercent.ToString("F0"));
                Logger.Data(Viewer.PlayerLocomotive.ThrottlePercent.ToString("F0"));
                Logger.Data(Viewer.PlayerLocomotive.MotiveForceN.ToString("F0"));
                Logger.Data(TrackMonitorWindow.FormatSpeed(Viewer.PlayerLocomotive.SpeedMpS, Viewer.MilepostUnitsMetric));
                Logger.Data((Viewer.PlayerLocomotive.GravityForceN / (Viewer.PlayerLocomotive.MassKG * 9.81f)).ToString("F0"));
                Logger.Data((Viewer.PlayerLocomotive as MSTSLocomotive).LocomotiveAxle.SlipSpeedPercent.ToString("F0"));
                Logger.End();
            }
            if (DrawCarNumber == true)
            {
                foreach (TrainCar tcar in Viewer.TrainDrawer.ViewableCars)
                {
                    frame.AddPrimitive(DrawInforMaterial,
                        new ActivityInforPrimitive(DrawInforMaterial, tcar),
                            RenderPrimitiveGroup.World, ref Identity);
                }

                //	UpdateCarNumberText(frame, elapsedTime);
            }
            if (DrawSiding == true || DrawPlatform == true)
            {
                foreach (WorldFile w in Viewer.SceneryDrawer.WorldFiles)
                {
                    if (DrawSiding == true && w != null && w.sidings != null)
                    {
                        foreach (SidingLabel sd in w.sidings)
                        {
                            if (sd != null) frame.AddPrimitive(DrawInforMaterial,
                                new ActivityInforPrimitive(DrawInforMaterial, sd, Color.Coral),
                                RenderPrimitiveGroup.World, ref Identity);
                        }
                    }
                    if (DrawPlatform == true && w != null && w.platforms != null)
                    {
                        foreach (PlatformLabel pd in w.platforms)
                        {
                            if (pd != null) frame.AddPrimitive(DrawInforMaterial,
                                new ActivityInforPrimitive(DrawInforMaterial, pd, Color.CornflowerBlue),
                                RenderPrimitiveGroup.World, ref Identity);
                        }
                    }
                }
            }
        }

        int GetWorkingSetSize()
        {
            // Get memory usage (working set).
            GetProcessMemoryInfo(ProcessHandle, out ProcessMemoryCounters, ProcessMemoryCounters.cb);
            var memory = ProcessMemoryCounters.WorkingSetSize;
            return memory;
        }

        public static string FormattedTime(double clockTimeSeconds) //some measure of time so it can be sorted.  Good enuf for now. Might add more later. Okay
        {
            int hour = (int)(clockTimeSeconds / (60.0 * 60.0));
            clockTimeSeconds -= hour * 60.0 * 60.0;
            int minute = (int)(clockTimeSeconds / 60.0);
            clockTimeSeconds -= minute * 60.0;
            int seconds = (int)clockTimeSeconds;
            // Reset clock before and after midnight
            if (hour >= 24)
                hour %= 24;
            if (hour < 0)
                hour += 24;
            if (minute < 0)
                minute += 60;
            if (seconds < 0)
                seconds += 60;

            return string.Format("{0:D2}:{1:D2}:{2:D2}", hour, minute, seconds);
        }

        static void DataLoggerStart()
        {
            using (StreamWriter file = File.AppendText("dump.csv"))
            {
                file.WriteLine(String.Join(",", new[] {
							"SVN",
							"Frame",
							"Memory",
							"Memory (Managed)",
							"Gen 0 GC",
							"Gen 1 GC",
							"Gen 2 GC",
							"Processors",
							"Frame Rate",
							"Frame Time",
							"Frame Jitter",
							"Shadow Primitives",
							"Render Primitives",
							"Render Process",
							"Updater Process",
							"Loader Process",
							"Sound Process",
                            "Time",
                            "Player Direction",
                            "Player Reverser",
                            "Player Throttle",
                            "Player Motive Force [N]",
                            "Player Speed [Mps]",
                            "Player Elevation [N/kN]",
                            "Player Wheelslip",
						}));
                file.Close();
            }
        }

        void DataLoggerStop()
        {
            Logger.Flush();
        }

        public void Profile(double elapsedRealSeconds) // should be called every 100mS
        {
            if (elapsedRealSeconds < 0.01)  // just in case
                return;

            Viewer.RenderProcess.Profiler.Mark();
            Viewer.UpdaterProcess.Profiler.Mark();
            Viewer.LoaderProcess.Profiler.Mark();
            Viewer.SoundProcess.Profiler.Mark();
        }
    }

    public class TextPrimitive : RenderPrimitive
    {
        public readonly SpriteBatchMaterial Material;
        public Point Position;
        public readonly Color Color;
        public readonly WindowTextFont Font;
        public string Text;

        public TextPrimitive(SpriteBatchMaterial material, Point position, Color color, WindowTextFont font)
        {
            Material = material;
            Position = position;
            Color = color;
            Font = font;
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            Font.Draw(Material.SpriteBatch, Position, Text, Color);
        }
    }

    //2D straight lines
    public class LinePrimitive : RenderPrimitive
    {
        public readonly SpriteBatchLineMaterial Material;
        public Vector2 PositionStart;
        public Vector2 PositionEnd;
        public readonly Color Color;
        public float Depth; //z buffer value: 0 always show, 1 always not show, in between, depends
        public int Width; //line width

        //constructor: startX, startY: X,Y of the start point, endXY the end point, depth: z buffer value (between 0, 1)
        public LinePrimitive(SpriteBatchLineMaterial material, float startX, float startY, float endX, float endY, Color color, float depth, int width)
        {
            Material = material;
            Color = color;
            Depth = depth;
            Width = width;
            PositionEnd = new Vector2(endX, endY);
            PositionStart = new Vector2(startX, startY);
        }
        public void UpdateLocation(float startX, float startY, float endX, float endY)
        {
            PositionEnd.X = endX;
            PositionEnd.Y = endY;
            PositionStart.X = startX;
            PositionStart.Y = startY;
        }

        //draw 2D straight lines
        public void DrawLine(SpriteBatch batch, Color color, Vector2 point1,
                                    Vector2 point2, float Layer)
        {
            float angle = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            float length = (point2 - point1).Length();

            batch.Draw(Material.Texture, point1, null, color,
                       angle, Vector2.Zero, new Vector2(length, Width),
                       SpriteEffects.None, Layer);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        public override void Draw(GraphicsDevice graphicsDevice)
        {
            DrawLine(Material.SpriteBatch, Color, PositionStart, PositionEnd, Depth);
        }

    }

    //2D straight lines
    public class ActivityInforPrimitive : RenderPrimitive
    {
        public readonly ActivityInforMaterial Material;
        public SpriteFont Font;
        public Viewer3D Viewer;
        TrainCar TrainCar = null;
        TrItemLabel TrItemLabel = null;
        Color LabelColor;
        float LineSpacing;

        //constructor: create one that draw car numbers
        public ActivityInforPrimitive(ActivityInforMaterial material, TrainCar tcar)
        {
            Material = material;
            Font = material.Font;
            Viewer = material.RenderProcess.Viewer;
            TrainCar = tcar;
            LineSpacing = Material.LineSpacing;
        }

        /// <summary>
        /// Information for showing labels of track items such as sidings and platforms
        /// </summary>
        public ActivityInforPrimitive(ActivityInforMaterial material, TrItemLabel pd, Color labelColor)
        {
            Material = material;
            Font = material.Font;
            Viewer = material.RenderProcess.Viewer;
            TrItemLabel = pd;
            LineSpacing = Material.LineSpacing;
            LabelColor = labelColor;
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        public override void Draw(GraphicsDevice graphicsDevice)
        {
            if (TrainCar != null) UpdateCarNumberText();
            if (TrItemLabel != null) UpdateTrItemNameText();
        }

        //draw car numbers above train cars when F7 is hit
        void UpdateCarNumberText()
        {
            float X, BottomY, TopY;

            //find car location vs. camera
            Vector3 Location = TrainCar.WorldPosition.XNAMatrix.Translation +
                    new Vector3((TrainCar.WorldPosition.TileX - Viewer.Camera.TileX) * 2048, 0, (-TrainCar.WorldPosition.TileZ + Viewer.Camera.TileZ) * 2048);

            //project 3D space to 2D (for the top of the line)
            Vector3 cameraVector = Viewer.GraphicsDevice.Viewport.Project(
                Location + new Vector3(0, TrainCar.Height, 0),
                Viewer.Camera.XNAProjection, Viewer.Camera.XNAView, Matrix.Identity);
            if (cameraVector.Z > 1 || cameraVector.Z < 0) return; //out of range or behind the camera
            X = cameraVector.X;
            BottomY = cameraVector.Y;//remember them

            ////project for the top of the line
            cameraVector = Viewer.GraphicsDevice.Viewport.Project(
                Location + new Vector3(0, 10, 0),
                Viewer.Camera.XNAProjection, Viewer.Camera.XNAView, Matrix.Identity);

            //want to draw the train car name at cameraVector.Y, but need to check if it overlap other texts in Material.AlignedTextB
            //and determine the new location if conflict occurs
            TopY = AlignVertical(cameraVector.Y, X, X + Font.MeasureString(TrainCar.CarID).X, LineSpacing, Material.AlignedTextA);

            //draw the car number with blue and white color 
            Material.SpriteBatch.DrawString(Font, TrainCar.CarID, new Vector2(X - 1, TopY - 1), Color.White);
            Material.SpriteBatch.DrawString(Font, TrainCar.CarID, new Vector2(X + 0, TopY - 1), Color.White);
            Material.SpriteBatch.DrawString(Font, TrainCar.CarID, new Vector2(X + 1, TopY - 1), Color.White);
            Material.SpriteBatch.DrawString(Font, TrainCar.CarID, new Vector2(X - 1, TopY + 0), Color.White);
            Material.SpriteBatch.DrawString(Font, TrainCar.CarID, new Vector2(X + 1, TopY + 0), Color.White);
            Material.SpriteBatch.DrawString(Font, TrainCar.CarID, new Vector2(X - 1, TopY + 1), Color.White);
            Material.SpriteBatch.DrawString(Font, TrainCar.CarID, new Vector2(X + 0, TopY + 1), Color.White);
            Material.SpriteBatch.DrawString(Font, TrainCar.CarID, new Vector2(X + 1, TopY + 1), Color.White);
            Material.SpriteBatch.DrawString(Font, TrainCar.CarID, new Vector2(X, TopY), Color.Blue);

            //draw the vertical line with length Math.Abs(cameraVector.Y + LineSpacing - BottomY)
            //the term LineSpacing is used so that the text is above the line head
            Material.SpriteBatch.Draw(Material.Texture, new Vector2(X, BottomY), null, Color.Blue,
                       (float)-Math.PI / 2, Vector2.Zero, new Vector2(Math.Abs(cameraVector.Y + LineSpacing - BottomY), 2),
                       SpriteEffects.None, cameraVector.Z);
        }

        /// <summary>
        /// When F6 is pressed, draws names above track items such as sidings and platforms.
        /// </summary>
        void UpdateTrItemNameText()
        {
            float X, BottomY, TopY;

            //loop through all wfile and each platform to draw platform names and lines

            //the location w.r.t. the camera
            Vector3 locationWRTCamera = TrItemLabel.Location.WorldLocation.Location + new Vector3((TrItemLabel.Location.TileX - Viewer.Camera.TileX) * 2048, 0, (TrItemLabel.Location.TileZ - Viewer.Camera.TileZ) * 2048);

            //if the platform is out of viewing range
            if (!Viewer.Camera.InFOV(locationWRTCamera, 10)) return;

            //project 3D space to 2D (for the bottom of the line)
            Vector3 cameraVector = Viewer.GraphicsDevice.Viewport.Project(
            TrItemLabel.Location.XNAMatrix.Translation + new Vector3((TrItemLabel.Location.TileX - Viewer.Camera.TileX) * 2048, 0, (-TrItemLabel.Location.TileZ + Viewer.Camera.TileZ) * 2048),
                Viewer.Camera.XNAProjection, Viewer.Camera.XNAView, Matrix.Identity);
            if (cameraVector.Z > 1 || cameraVector.Z < 0) return; //out of range or behind the camera
            X = cameraVector.X;
            BottomY = cameraVector.Y;//remember them

            ////project for the top of the line
            cameraVector = Viewer.GraphicsDevice.Viewport.Project(
            TrItemLabel.Location.XNAMatrix.Translation + new Vector3((TrItemLabel.Location.TileX - Viewer.Camera.TileX) * 2048, 20, (-TrItemLabel.Location.TileZ + Viewer.Camera.TileZ) * 2048),
                Viewer.Camera.XNAProjection, Viewer.Camera.XNAView, Matrix.Identity);

            //want to draw the text at cameraVector.Y, but need to check if it overlap other texts in Material.AlignedTextB
            //and determine the new location if conflict occurs
            TopY = AlignVertical(cameraVector.Y, X, X + Font.MeasureString(TrItemLabel.ItemName).X, LineSpacing, Material.AlignedTextB);

            //outline the siding/platform name in white by pre-drawing all 8 points of compass
            //Isn't this a clumsy way to do it?
            Material.SpriteBatch.DrawString(Font, TrItemLabel.ItemName, new Vector2(X + 0, TopY + 1), Color.White);
            Material.SpriteBatch.DrawString(Font, TrItemLabel.ItemName, new Vector2(X + 1, TopY + 1), Color.White);
            Material.SpriteBatch.DrawString(Font, TrItemLabel.ItemName, new Vector2(X + 1, TopY + 0), Color.White);
            Material.SpriteBatch.DrawString(Font, TrItemLabel.ItemName, new Vector2(X + 1, TopY - 1), Color.White);
            Material.SpriteBatch.DrawString(Font, TrItemLabel.ItemName, new Vector2(X + 0, TopY - 1), Color.White);
            Material.SpriteBatch.DrawString(Font, TrItemLabel.ItemName, new Vector2(X - 1, TopY - 1), Color.White);
            Material.SpriteBatch.DrawString(Font, TrItemLabel.ItemName, new Vector2(X - 1, TopY - 0), Color.White);
            Material.SpriteBatch.DrawString(Font, TrItemLabel.ItemName, new Vector2(X - 1, TopY + 1), Color.White);
            //draw the siding/platform name in colour
            Material.SpriteBatch.DrawString(Font, TrItemLabel.ItemName, new Vector2(X, TopY), LabelColor);

            //draw a vertical line with length TopY + LineSpacing - BottomY
            //the term LineSpacing is used so that the text is above the line head
            Material.SpriteBatch.Draw(Material.Texture, new Vector2(X, BottomY), null, LabelColor,
                           -(float)Math.PI / 2, Vector2.Zero, new Vector2(Math.Abs(TopY + LineSpacing - BottomY), 2),
                           SpriteEffects.None, cameraVector.Z);

        }

        //helper function to make the train car and siding name align nicely on the screen
        //the basic idea is to space the screen vertically as table cell, each cell holds a list of text assigned.
        //new text in will check its destinated cell, if it overlap with a text in the cell, it will move up a cell and continue
        //once it is determined in a cell, it will be pushed in the list of text of that cell, and the new Y will be returned.
        float AlignVertical(float wantY, float startX, float endX, float spacing, List<Vector2>[] alignedTextY)
        {
            if (alignedTextY == null || wantY < 0) return wantY; //data checking
            int position = (int)(wantY / spacing);//the cell of the text it wants in
            if (position > alignedTextY.Length) return wantY;//position is larger than the number of cells
            int desiredPosition = position;
            while (position < alignedTextY.Length && position >= 0)
            {
                if (alignedTextY[position].Count == 0)
                {
                    alignedTextY[position].Add(new Vector2(startX, endX));//add info for the text (i.e. start and end location)
                    if (position == desiredPosition) return wantY; //if it can be in the desired cell, use the desired Y instead of the cell Y, so the text won't jump up-down
                    else return position * spacing;//the cell location is the new Y
                }
                bool conflict = false;
                //check if it is intersect any one in the cell
                foreach (Vector2 v in alignedTextY[position])
                {
                    //check conflict with a text, v.x is the start of the text, v.y is the end of the text
                    if ((startX > v.X && startX < v.Y) || (endX > v.X && endX < v.Y) || (v.X > startX && v.X < endX) || (v.Y > startX && v.Y < endX))
                    {
                        conflict = true;
                        break;
                    }
                }
                if (conflict == false) //no conflict
                {
                    alignedTextY[position].Add(new Vector2(startX, endX));//add info for the text (i.e. start and end location)
                    if (position == desiredPosition) return wantY;
                    else return position * spacing;//the cell location is the new Y
                }
                position--;
            }
            if (position == desiredPosition) return wantY;
            else return position * spacing;//the cell location is the new Y
        }

    }
}
