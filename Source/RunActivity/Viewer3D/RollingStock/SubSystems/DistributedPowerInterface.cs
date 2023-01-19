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

// This file is the responsibility of the 3D & Environment Team. 

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Formats.Msts;
using Orts.Simulation.RollingStocks;
using Orts.Viewer3D.Common;
using Orts.Viewer3D.Popups;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static Orts.Viewer3D.RollingStock.SubSystems.DistributedPowerInterface;

namespace Orts.Viewer3D.RollingStock.SubSystems
{
    public class DistributedPowerInterface
    {
        public readonly MSTSLocomotive Locomotive;
        public readonly Viewer Viewer;
        public IList<DPIWindow> Windows = new List<DPIWindow>();
        float PrevScale = 1;
        public DPIStatus DPIStatus = new DPIStatus();

        bool Active;
        public float Scale { get; private set; }
        public float MipMapScale { get; private set; }
        readonly int Height = 240;
        readonly int Width = 640;

        public readonly DPDefaultWindow DPDefaultWindow;

        public readonly DriverMachineInterfaceShader Shader;

        // Color RGB values are from ETCS specification
        public static readonly Color ColorGrey = new Color(195, 195, 195);
        public static readonly Color ColorMediumGrey = new Color(150, 150, 150);
        public static readonly Color ColorDarkGrey = new Color(85, 85, 85);
        public static readonly Color ColorYellow = new Color(223, 223, 0);
        public static readonly Color ColorOrange = new Color(234, 145, 0);
        public static readonly Color ColorRed = new Color(191, 0, 2);
        public static readonly Color ColorBackground = new Color(0, 0, 0, 0); // transparent
        public static readonly Color ColorPASPlight = new Color(41, 74, 107);
        public static readonly Color ColorPASPdark = new Color(33, 49, 74);
        public static readonly Color ColorShadow = new Color(8, 24, 57);
        public static readonly Color ColorWhite = new Color(255, 255, 255);

        // Some DPIs use black for the background and white for borders, instead of blue scale
        public readonly bool BlackWhiteTheme = false;

        public Texture2D ColorTexture { get; private set; }

        public float CurrentTime => (float)Viewer.Simulator.ClockTime;

        /// <summary>
        /// True if the screen is sensitive
        /// </summary>
        public bool IsTouchScreen = true;
        /// <summary>
        /// Controls the layout of the DMI screen depending.
        /// Must be true if there are physical buttons to control the DMI, even if it is a touch screen.
        /// If false, the screen must be tactile.
        /// </summary>
        public bool IsSoftLayout;
        public DPIWindow ActiveWindow;
        public DistributedPowerInterface(float height, float width, MSTSLocomotive locomotive, Viewer viewer, CabViewControl control)
        {
            Viewer = viewer;
            Locomotive = locomotive;
            Scale = Math.Min(width / Width, height / Height);
            if (Scale < 0.5) MipMapScale = 2;
            else MipMapScale = 1;

            Shader = new DriverMachineInterfaceShader(Viewer.GraphicsDevice);
            DPDefaultWindow = new DPDefaultWindow(this, control);
            DPDefaultWindow.Visible = true;

            AddToLayout(DPDefaultWindow, Point.Zero);
            ActiveWindow = DPDefaultWindow;
        }

        public void AddToLayout(DPIWindow window, Point position)
        {
            window.Position = position;
            window.Parent = ActiveWindow;
            ActiveWindow = window;
            Windows.Add(window);
        }

        public void PrepareFrame(float elapsedSeconds)
        {
            Active = DPIStatus != null && DPIStatus.DPIActive;
            if (!Active) return;

            foreach (var area in Windows)
            {
                area.PrepareFrame(DPIStatus);
            }
        }
        public void SizeTo(float width, float height)
        {
            Scale = Math.Min(width / Width, height / Height);

            if (Math.Abs(1f - PrevScale / Scale) > 0.1f)
            {
                PrevScale = Scale;
                if (Scale < 0.5) MipMapScale = 2;
                else MipMapScale = 1;
                foreach (var area in Windows)
                {
                    area.ScaleChanged();
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, Point position)
        {
            if (ColorTexture == null)
            {
                ColorTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                ColorTexture.SetData(new[] { Color.White });
            }
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearWrap, null, null, null); // TODO: Handle brightness via DMI shader
            if (!Active) return;
            foreach (var area in Windows)
            {
                area.Draw(spriteBatch, new Point(position.X + (int)(area.Position.X * Scale), position.Y + (int)(area.Position.Y * Scale)));
            }
        }

        public void ExitWindow(DPIWindow window)
        {
            var windows = new List<DPIWindow>(Windows);
            windows.Remove(window);
            Windows = windows;
            if (window.Parent == null) ActiveWindow = DPDefaultWindow;
            else ActiveWindow = window.Parent;
        }
    }
    public class DPDefaultWindow : DPIWindow
    {
        public bool FullTable;
        public DPITable DPITable;
        public CABViewControlUnits LoadUnits = CABViewControlUnits.NONE;
        public DPDefaultWindow(DistributedPowerInterface dpi, CabViewControl control) : base(dpi, 640, 240)
        {
            var param = (control as CVCScreen).CustomParameters;
            if (param.ContainsKey("fulltable")) bool.TryParse(param["fulltable"], out FullTable);
            if (param.ContainsKey("loadunits"))
            {
                string sUnits = param["loadunits"].ToUpper();
                sUnits = sUnits.Replace('/', '_');
                CABViewControlUnits.TryParse(sUnits, out LoadUnits);
            }
            DPITable = new DPITable(FullTable, LoadUnits, fullScreen:true, dpi:dpi);
            AddToLayout(DPITable, new Point(0, 0));
        }
    }

    public class DPIArea
    {
        public Point Position;
        public readonly DistributedPowerInterface DPI;
        protected Texture2D ColorTexture => DPI.ColorTexture;
        public float Scale => DPI.Scale;
        public int Height;
        public int Width;
        protected List<RectanglePrimitive> Rectangles = new List<RectanglePrimitive>();
        protected List<TextPrimitive> Texts = new List<TextPrimitive>();
        protected List<TexturePrimitive> Textures = new List<TexturePrimitive>();
        public int Layer;
        protected bool FlashingFrame;
        public Color BackgroundColor = Color.Transparent;
        public bool Pressed;
        public bool Visible;
        public class TextPrimitive
        {
            public Point Position;
            public Color Color;
            public WindowTextFont Font;
            public string Text;

            public TextPrimitive(Point position, Color color, string text, WindowTextFont font)
            {
                Position = position;
                Color = color;
                Text = text;
                Font = font;
            }

            public void Draw(SpriteBatch spriteBatch, Point position)
            {
                Font.Draw(spriteBatch, position, Text, Color);
            }
        }
        public struct TexturePrimitive
        {
            public readonly Texture2D Texture;
            public readonly Vector2 Position;
            public TexturePrimitive(Texture2D texture, Vector2 position)
            {
                Texture = texture;
                Position = position;
            }
            public TexturePrimitive(Texture2D texture, float x, float y)
            {
                Texture = texture;
                Position = new Vector2(x, y);
            }
        }
        public struct RectanglePrimitive
        {
            public readonly float X;
            public readonly float Y;
            public readonly float Width;
            public readonly float Height;
            public readonly bool DrawAsInteger;
            public Color Color;
        }
        public DPIArea(DistributedPowerInterface dpi)
        {
            DPI = dpi;
        }
        public DPIArea(DistributedPowerInterface dpi, int width, int height)
        {
            DPI = dpi;
            Width = width;
            Height = height;
        }
        public virtual void Draw(SpriteBatch spriteBatch, Point drawPosition)
        {
            if (BackgroundColor != Color.Transparent) DrawRectangle(spriteBatch, drawPosition, 0, 0, Width, Height, BackgroundColor);

            foreach (var r in Rectangles)
            {
                if (r.DrawAsInteger) DrawIntRectangle(spriteBatch, drawPosition, r.X, r.Y, r.Width, r.Height, r.Color);
                else DrawRectangle(spriteBatch, drawPosition, r.X, r.Y, r.Width, r.Height, r.Color);
            }
            foreach(var text in Texts)
            {
                int x = drawPosition.X + (int)Math.Round(text.Position.X * Scale);
                int y = drawPosition.Y + (int)Math.Round(text.Position.Y * Scale);
                text.Draw(spriteBatch, new Point(x, y));
            }
            foreach(var tex in Textures)
            {
                DrawSymbol(spriteBatch, tex.Texture, drawPosition, tex.Position.Y, tex.Position.Y);
            }
            if (DPI.BlackWhiteTheme)
            {
                DrawIntRectangle(spriteBatch, drawPosition, 0, 0, 1, Height, Color.White);
                DrawIntRectangle(spriteBatch, drawPosition, Width - 1, 0, 1, Height, Color.White);
                DrawIntRectangle(spriteBatch, drawPosition, 0, 0, Width, 1, Color.White);
                DrawIntRectangle(spriteBatch, drawPosition, 0, Height - 1, Width, 1, Color.White);
            }
            else if (Layer < 0)
            {
                DrawIntRectangle(spriteBatch, drawPosition, 0, 0, 1, Height, Color.Black);
                DrawIntRectangle(spriteBatch, drawPosition, Width - 1, 0, 1, Height, ColorShadow);
                DrawIntRectangle(spriteBatch, drawPosition, 0, 0, Width, 1, Color.Black);
                DrawIntRectangle(spriteBatch, drawPosition, 0, Height - 1, Width, 1, ColorShadow);
            }
        }
        public virtual void PrepareFrame(DPIStatus status) { }

        public void DrawRectangle(SpriteBatch spriteBatch, Point drawPosition, float x, float y, float width, float height, Color color)
        {
            spriteBatch.Draw(ColorTexture, new Vector2(drawPosition.X + x * Scale, drawPosition.Y + y * Scale), null, color, 0f, Vector2.Zero, new Vector2(width * Scale, height * Scale), SpriteEffects.None, 0);
        }
        public void DrawIntRectangle(SpriteBatch spriteBatch, Point drawPosition, float x, float y, float width, float height, Color color)
        {
            spriteBatch.Draw(ColorTexture, new Rectangle(drawPosition.X + (int)(x * Scale), drawPosition.Y + (int)(y * Scale), Math.Max((int)(width * Scale), 1), Math.Max((int)(height * Scale), 1)), null, color);
        }
        public void DrawSymbol(SpriteBatch spriteBatch, Texture2D texture, Point origin, float x, float y)
        {
            spriteBatch.Draw(texture, new Vector2(origin.X + x * Scale, origin.Y + y * Scale), null, Color.White, 0, Vector2.Zero, Scale * DPI.MipMapScale, SpriteEffects.None, 0);
        }
        public WindowTextFont GetFont(float size, bool bold=false)
        {
            return DPI.Viewer.WindowManager.TextManager.GetExact("Arial", GetScaledFontSize(size), bold ? System.Drawing.FontStyle.Bold : System.Drawing.FontStyle.Regular);
        }
        /// <summary>
        /// Get scaled font size, increasing it if result is small
        /// </summary>
        /// <param name="requiredSize"></param>
        /// <returns></returns>
        public float GetScaledFontSize(float requiredSize)
        {
            float size = requiredSize * Scale;
            if (size < 5) return size * 1.2f;
            return size;
        }
        public virtual void ScaleChanged() { }
    }
    public class DPIWindow : DPIArea
    {
        public DPIWindow Parent;
        public List<DPIArea> SubAreas = new List<DPIArea>();
        public bool FullScreen;
        protected DPIWindow(DistributedPowerInterface dpi, int width, int height) : base(dpi, width, height)
        {
        }
        public override void PrepareFrame(DPIStatus status)
        {
            if (!Visible) return;
            base.PrepareFrame(status);
            foreach(var area in SubAreas)
            {
                area.PrepareFrame(status);
            }
        }
        public override void Draw(SpriteBatch spriteBatch, Point drawPosition)
        {
            if (!Visible) return;
            base.Draw(spriteBatch, drawPosition);
            foreach(var area in SubAreas)
            {
                if (area.Visible) area.Draw(spriteBatch, new Point((int)Math.Round(drawPosition.X + area.Position.X * Scale), (int)Math.Round(drawPosition.Y + area.Position.Y * Scale)));
            }
        }
        public void AddToLayout(DPIArea area, Point position)
        {
            area.Position = position;
            area.Visible = true;
            SubAreas.Add(area);
        }
        public override void ScaleChanged()
        {
            base.ScaleChanged();
            foreach (var area in SubAreas)
            {
                area.ScaleChanged();
            }
        }
    }

    public class DPITable : DPIWindow
    {
        public DistributedPowerInterface DPI;
        public const int NumberOfRowsFull = 9;
        public const int NumberOfRowsPartial = 6;
        private const int NumberOfColumns = 7;
        public const string Fence = "\u2590";
        public string[] TableRows = new string[NumberOfRowsFull];
        public TextPrimitive[,] TableText = new TextPrimitive[NumberOfRowsFull, NumberOfColumns];
        public TextPrimitive[,] TableSymbol = new TextPrimitive[NumberOfRowsFull, NumberOfColumns];
        WindowTextFont TableTextFont;
        WindowTextFont TableSymbolFont;
        readonly int FontHeightTableText = 16;
        readonly int FontHeightTableSymbol = 19;
        readonly int RowHeight = 34;
        readonly int ColLength = 88;
        public bool FullTable = true;
        public CABViewControlUnits LoadUnits;

        // Change text color
        readonly Dictionary<string, Color> ColorCodeCtrl = new Dictionary<string, Color>
        {
            { "!!!", Color.OrangeRed },
            { "!!?", Color.Orange },
            { "!??", Color.White },
            { "?!?", Color.Black },
            { "???", Color.Yellow },
            { "??!", Color.Green },
            { "?!!", Color.PaleGreen },
            { "$$$", Color.LightSkyBlue},
            { "%%%", Color.Cyan}
        };


        public readonly string[] FirstColumn = { "ID", "Throttle", "Load", "BP", "Flow", "Remote", "ER", "BC", "MR" };

        public DPITable(bool fullTable, CABViewControlUnits loadUnits, bool fullScreen, DistributedPowerInterface dpi) : base(dpi, 640,  fullTable? 230 : 162)
        {
            DPI = dpi;
            FullScreen = fullScreen;
            FullTable = fullTable;
            LoadUnits = loadUnits;
            BackgroundColor = DPI.BlackWhiteTheme ? Color.Black : ColorBackground;
            SetFont();
            string text = "";
            for (int iRow = 0; iRow < (fullTable ? NumberOfRowsFull : NumberOfRowsPartial); iRow++)
            {
                for (int iCol = 0; iCol < NumberOfColumns; iCol++)
                {
//                    text = iCol.ToString() + "--" + iRow.ToString();
                    TableText[iRow, iCol] = new TextPrimitive(new Point(20 + ColLength * iCol, (iRow) * (FontHeightTableText + 8)), Color.White, text, TableTextFont);
                    TableSymbol[iRow, iCol] = new TextPrimitive(new Point(10 + ColLength * iCol, (iRow) * (FontHeightTableText + 8)), Color.Green, text, TableSymbolFont);
                }
            }
        }

        public override void ScaleChanged()
        {
//            base.ScaleChanged();
            SetFont();
        }
        void SetFont()
        {
            TableTextFont = GetFont(FontHeightTableText);
            TableSymbolFont = GetFont(FontHeightTableSymbol);
        }
        public override void Draw(SpriteBatch spriteBatch, Point drawPosition)
        {
            if (!Visible) return;
            base.Draw(spriteBatch, drawPosition);
            var nRows = FullTable ? NumberOfRowsFull : NumberOfRowsPartial;
            for (int iRow = 0; iRow < nRows; iRow++)
                for (int iCol = 0; iCol < NumberOfColumns; iCol++)
                {
                    //            DrawRectangle(spriteBatch, drawPosition, 0, 0, FullScreen ? 334 : 306, 24, Color.Black);
                    int x = drawPosition.X + (int)Math.Round(TableText[iRow, iCol].Position.X * Scale);
                    int y = drawPosition.Y + (int)Math.Round(TableText[iRow, iCol].Position.Y * Scale);
                    TableText[iRow, iCol].Draw(spriteBatch, new Point(x, y));
                    x = drawPosition.X + (int)Math.Round(TableSymbol[iRow, iCol].Position.X * Scale);
                    y = drawPosition.Y + (int)Math.Round(TableSymbol[iRow, iCol].Position.Y * Scale);
                    TableSymbol[iRow, iCol].Draw(spriteBatch, new Point(x, y));
                }
        }

        public override void PrepareFrame(DPIStatus dpiStatus)
        {
            string[,] tempStatus;
            var locomotive = DPI.Locomotive;
            var train = locomotive.Train;
            var multipleUnitsConfiguration = locomotive.GetMultipleUnitsConfiguration();
            int dieselLocomotivesCount = 0;

            if (locomotive != null)
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
                    var dieselLoco = MSTSDieselLocomotive.GetDpuHeader(true, numberOfDieselLocomotives, maxNumberOfEngines).Replace("\t", "");
                    string[] dieselLocoHeader = dieselLoco.Split('\n');
                    tempStatus = new string[numberOfDieselLocomotives, dieselLocoHeader.Length];
                    var k = 0;
                    var dpUnitId = 0;
                    var dpUId = -1;
                    var i = 0;
                    for (i = 0; i < train.Cars.Count; i++)
                    {
                        if (train.Cars[i] is MSTSDieselLocomotive)
                        {
                            if (dpUId != (train.Cars[i] as MSTSLocomotive).DPUnitID)
                            {
                                var status = (train.Cars[i] as MSTSDieselLocomotive).GetDpuStatus(true, LoadUnits).Split('\t');
                                var fence = ((dpUnitId != (dpUnitId = train.Cars[i].RemoteControlGroup)) ? "| " : "  ");
                                tempStatus[k, 0] = fence + status[0].Split('(').First();
                                for (var j = 1; j < status.Length; j++)
                                {
                                    tempStatus[k, j] = fence + status[j].Split(' ').First();
                                    // move color code from after the Units to after the value
                                    if (ColorCodeCtrl.Keys.Any(status[j].EndsWith) && !ColorCodeCtrl.Keys.Any(tempStatus[k, j].EndsWith))
                                    {
                                        tempStatus[k, j] += status[j].Substring(status[j].Length - 3);
                                    }
                                }
                                dpUId = (train.Cars[i] as MSTSLocomotive).DPUnitID;
                                k++;
                            }
                        }
                    }

                    dieselLocomotivesCount = k;// only leaders loco group
                    var nRows = Math.Min (FullTable ? NumberOfRowsFull : NumberOfRowsPartial, dieselLocoHeader.Count());

                    for (i = 0; i < nRows; i++)
                    {

                        for (int j = 0; j < dieselLocomotivesCount; j++)
                        {
                            var text = tempStatus[j, i].Replace('|', ' ');
                            var colorFirstColEndsWith = ColorCodeCtrl.Keys.Any(text.EndsWith) ? ColorCodeCtrl[text.Substring(text.Length - 3)] : Color.White;
                            TableText[i, j + 1].Font = TableTextFont;
                            TableText[i, j + 1].Text = (colorFirstColEndsWith == Color.White) ? text : text.Substring(0, text.Length - 3); ;
                            TableText[i, j + 1].Color = colorFirstColEndsWith;
                            TableSymbol[i, j + 1].Font = TableSymbolFont; 
                            TableSymbol[i, j + 1].Text = (tempStatus[j, i] != null && tempStatus[j, i].Contains("|")) ? Fence : " ";
                        }
                        TableText[i, 0].Font = TableTextFont;
                        TableText[i, 0].Text = dieselLocoHeader[i];
                    }
                }
            }
        }
    }

    public class DPIStatus
    {
        // General status
        /// <summary>
        /// True if the DPI is active and will be shown
        /// </summary>
        public bool DPIActive = true;
    }

    public class DistributedPowerInterfaceRenderer : CabViewControlRenderer
    {
        public DistributedPowerInterface DPI;
        bool Zoomed = false;
        protected Rectangle DrawPosition;
        [CallOnThread("Loader")]
        public DistributedPowerInterfaceRenderer(Viewer viewer, MSTSLocomotive locomotive, CVCScreen control, CabShader shader)
            : base(viewer, locomotive, control, shader)
        {
            Position.X = (float)Control.PositionX;
            Position.Y = (float)Control.PositionY;
            DPI = new DistributedPowerInterface((int)Control.Height, (int)Control.Width, locomotive, viewer, control);
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (!IsPowered && Control.HideIfDisabled)
                return;

            base.PrepareFrame(frame, elapsedTime);
            var xScale = Viewer.CabWidthPixels / 640f;
            var yScale = Viewer.CabHeightPixels / 480f;
            DrawPosition.X = (int)(Position.X * xScale) - Viewer.CabXOffsetPixels + Viewer.CabXLetterboxPixels;
            DrawPosition.Y = (int)(Position.Y * yScale) + Viewer.CabYOffsetPixels + Viewer.CabYLetterboxPixels;
            DrawPosition.Width = (int)(Control.Width * xScale);
            DrawPosition.Height = (int)(Control.Height * yScale);
            if (Zoomed)
            {
                DrawPosition.Width = 640;
                DrawPosition.Height = 480;
                DPI.SizeTo(DrawPosition.Width, DrawPosition.Height);
                DrawPosition.X -= 320;
                DrawPosition.Y -= 240;
                DPI.DPDefaultWindow.BackgroundColor = ColorBackground;
            }
            else
            {
                DPI.SizeTo(DrawPosition.Width, DrawPosition.Height);
                DPI.DPDefaultWindow.BackgroundColor = Color.Transparent;
            }
            DPI.PrepareFrame(elapsedTime.ClockSeconds);
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            DPI.Draw(ControlView.SpriteBatch, new Point(DrawPosition.X, DrawPosition.Y));
            ControlView.SpriteBatch.End();
            ControlView.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, DepthStencilState.Default, null, Shader);
        }
    }

    //    public class ThreeDimCabDPI : ThreeDimCabDigit
    public class ThreeDimCabDPI
    {
        const int MaxDigits = 7;
        const int HeaderMaxDigits = 8;
        const int NumColumns = 6;
        PoseableShape TrainCarShape;
        VertexPositionNormalTexture[] VertexList;
        int NumVertices;
        int NumIndices;
        public short[] TriangleListIndices;// Array of indices to vertices for triangles
        Matrix XNAMatrix;
        Viewer Viewer;
        MutableShapePrimitive shapePrimitive;
        public DistributedPowerInterfaceRenderer CVFR;
        public DPITable DPITable;
        public DPIStatus DPIStatus;
        Material Material;
        Material AlertMaterial;
        float Size;
        string AceFile;
        public ThreeDimCabDPI(Viewer viewer, int iMatrix, string size, string aceFile, PoseableShape trainCarShape, CabViewControlRenderer c)
        //           : base(viewer, iMatrix, size, aceFile, trainCarShape, c)
        {
            Size = int.Parse(size) * 0.001f;//input size is in mm
            if (aceFile != "")
            {
                AceFile = aceFile.ToUpper();
                if (!AceFile.EndsWith(".ACE")) AceFile = AceFile + ".ACE"; //need to add ace into it
            }
            else { AceFile = ""; }

            CVFR = (DistributedPowerInterfaceRenderer)c;
            DPITable = CVFR.DPI.DPDefaultWindow.DPITable;
            DPIStatus = CVFR.DPI.DPIStatus;
            Viewer = viewer;
            TrainCarShape = trainCarShape;
            XNAMatrix = TrainCarShape.SharedShape.Matrices[iMatrix];
            // 9 rows, 5 columns plus first one; first one has a couple of triangles for the whole string,
            // the other ones have a couple of triangles for each char, and there are max 7 chars per string;
            // this leads to 1944 vertices
            var maxVertex = 2048;

            //Material = viewer.MaterialManager.Load("Scenery", Helpers.GetRouteTextureFile(viewer.Simulator, Helpers.TextureFlags.None, texture), (int)(SceneryMaterialOptions.None | SceneryMaterialOptions.AlphaBlendingBlend), 0);
            Material = FindMaterial(false);//determine normal material
                                           // Create and populate a new ShapePrimitive
            NumVertices = NumIndices = 0;

            VertexList = new VertexPositionNormalTexture[maxVertex];
            TriangleListIndices = new short[maxVertex / 2 * 3]; // as is NumIndices

            //start position is the center of the text
            var start = new Vector3(0, 0, 0);
            var rotation = 0;

            //find the left-most of text
            Vector3 offset;

            offset.X = 0;

            offset.Y = -Size;
            var param = new string(' ', MaxDigits);
            var color = ColorYellow;
            var headerIndex = 0;
            float tX, tY;
            Matrix rot;
            for (int iRow = 0; iRow < DPITable.NumberOfRowsFull; iRow++)
            {
                // fill with blanks at startup
                tX = 0.875f;
                tY = 0.125f;
                //the left-bottom vertex
                Vector3 v = new Vector3(offset.X, offset.Y, 0.01f);
                v += start; Vertex v1 = new Vertex(v.X, v.Y, v.Z, 0, 0, -1, tX, tY);

                //the right-bottom vertex
                v.X = offset.X + Size * 7 * 0.5f; v.Y = offset.Y;
                v += start; Vertex v2 = new Vertex(v.X, v.Y, v.Z, 0, 0, -1, tX + 0.125f, tY);

                //the right-top vertex
                v.X = offset.X + Size * 7 * 0.5f; v.Y = offset.Y + Size;
                v += start; Vertex v3 = new Vertex(v.X, v.Y, v.Z, 0, 0, -1, tX + 0.125f, tY - 0.0625f);

                //the left-top vertex
                v.X = offset.X; v.Y = offset.Y + Size;
                v += start; Vertex v4 = new Vertex(v.X, v.Y, v.Z, 0, 0, -1, tX, tY - 0.0625f);

                //create first triangle
                TriangleListIndices[NumIndices++] = (short)NumVertices;
                TriangleListIndices[NumIndices++] = (short)(NumVertices + 2);
                TriangleListIndices[NumIndices++] = (short)(NumVertices + 1);
                // Second triangle:
                TriangleListIndices[NumIndices++] = (short)NumVertices;
                TriangleListIndices[NumIndices++] = (short)(NumVertices + 3);
                TriangleListIndices[NumIndices++] = (short)(NumVertices + 2);

                //create vertex
                VertexList[NumVertices].Position = v1.Position; VertexList[NumVertices].Normal = v1.Normal; VertexList[NumVertices].TextureCoordinate = v1.TexCoord;
                VertexList[NumVertices + 1].Position = v2.Position; VertexList[NumVertices + 1].Normal = v2.Normal; VertexList[NumVertices + 1].TextureCoordinate = v2.TexCoord;
                VertexList[NumVertices + 2].Position = v3.Position; VertexList[NumVertices + 2].Normal = v3.Normal; VertexList[NumVertices + 2].TextureCoordinate = v3.TexCoord;
                VertexList[NumVertices + 3].Position = v4.Position; VertexList[NumVertices + 3].Normal = v4.Normal; VertexList[NumVertices + 3].TextureCoordinate = v4.TexCoord;
                NumVertices += 4;
                headerIndex++;
                offset.X = 0;

                for (int iCol = 1; iCol < NumColumns; iCol++)
                {
                    for (int iChar = 0; iChar < param.Length; iChar++)
                    {
                        tX = GetTextureCoordX(param, iChar);
                        tY = GetTextureCoordY(param, iChar, color);
                        var offX = offset.X + Size * (1 + HeaderMaxDigits + (MaxDigits) * (iCol - 1)) * 0.5f;
                        //the left-bottom vertex
                        Vector3 va = new Vector3(offX, offset.Y, 0.01f);
                        va += start; Vertex v5 = new Vertex(va.X, va.Y, va.Z, 0, 0, -1, tX, tY);

                        //the right-bottom vertex
                        va.X = offX + Size * 0.5f; va.Y = offset.Y;
                        va += start; Vertex v6 = new Vertex(va.X, va.Y, va.Z, 0, 0, -1, tX + 0.125f, tY);

                        //the right-top vertex
                        va.X = offX + Size * 0.5f; va.Y = offset.Y + Size;
                        va += start; Vertex v7 = new Vertex(va.X, va.Y, va.Z, 0, 0, -1, tX + 0.125f, tY - 0.0625f);

                        //the left-top vertex
                        va.X = offX; va.Y = offset.Y + Size;
                        va += start; Vertex v8 = new Vertex(va.X, va.Y, va.Z, 0, 0, -1, tX, tY - 0.0625f);

                        //create first triangle
                        TriangleListIndices[NumIndices++] = (short)NumVertices;
                        TriangleListIndices[NumIndices++] = (short)(NumVertices + 2);
                        TriangleListIndices[NumIndices++] = (short)(NumVertices + 1);
                        // Second triangle:
                        TriangleListIndices[NumIndices++] = (short)NumVertices;
                        TriangleListIndices[NumIndices++] = (short)(NumVertices + 3);
                        TriangleListIndices[NumIndices++] = (short)(NumVertices + 2);

                        //create vertex
                        VertexList[NumVertices].Position = v5.Position; VertexList[NumVertices].Normal = v5.Normal; VertexList[NumVertices].TextureCoordinate = v5.TexCoord;
                        VertexList[NumVertices + 1].Position = v6.Position; VertexList[NumVertices + 1].Normal = v6.Normal; VertexList[NumVertices + 1].TextureCoordinate = v6.TexCoord;
                        VertexList[NumVertices + 2].Position = v7.Position; VertexList[NumVertices + 2].Normal = v7.Normal; VertexList[NumVertices + 2].TextureCoordinate = v7.TexCoord;
                        VertexList[NumVertices + 3].Position = v8.Position; VertexList[NumVertices + 3].Normal = v8.Normal; VertexList[NumVertices + 3].TextureCoordinate = v8.TexCoord;
                        NumVertices += 4;
                        offset.X += Size * 0.5f; offset.Y += 0; //move to next digit
                    }
                    offset.X = 0;
                }
                offset.Y -= Size; //move to next digit
            }

            //create the shape primitive
            shapePrimitive = new MutableShapePrimitive(Material, NumVertices, NumIndices, new[] { -1 }, 0);
            UpdateShapePrimitive(Material);

        }
        Material FindMaterial(bool Alert)
        {
            string imageName = "";
            string globalText = Viewer.Simulator.BasePath + @"\GLOBAL\TEXTURES\";
            CABViewControlTypes controltype = CVFR.GetControlType().Type;
            Material material = null;

            if (AceFile != "")
            {
                imageName = AceFile;
            }
            else
            {
                imageName = "dpi.ace";
            }

            SceneryMaterialOptions options = SceneryMaterialOptions.ShaderFullBright | SceneryMaterialOptions.AlphaBlendingAdd | SceneryMaterialOptions.UndergroundTexture;

            if (String.IsNullOrEmpty(TrainCarShape.SharedShape.ReferencePath))
            {
                if (!File.Exists(globalText + imageName))
                {
                    Trace.TraceInformation("Ignored missing " + imageName + " using default. You can copy and unpack the " + imageName + " from OR\'s Documentation\\SampleFiles\\Manual folder to " + globalText +
                        ", or place it under " + TrainCarShape.SharedShape.ReferencePath);
                }
                material = Viewer.MaterialManager.Load("Scenery", Helpers.GetTextureFile(Viewer.Simulator, Helpers.TextureFlags.None, globalText, imageName), (int)(options), 0);
            }
            else
            {
                if (!File.Exists(TrainCarShape.SharedShape.ReferencePath + @"\" + imageName))
                {
                    Trace.TraceInformation("Ignored missing " + imageName + " using default. You can copy and unpack the " + imageName + " from OR\'s Documentation\\SampleFiles\\Manual folder to " + globalText +
                        ", or place it under " + TrainCarShape.SharedShape.ReferencePath);
                    material = Viewer.MaterialManager.Load("Scenery", Helpers.GetTextureFile(Viewer.Simulator, Helpers.TextureFlags.None, globalText, imageName), (int)(options), 0);
                }
                else material = Viewer.MaterialManager.Load("Scenery", Helpers.GetTextureFile(Viewer.Simulator, Helpers.TextureFlags.None, TrainCarShape.SharedShape.ReferencePath + @"\", imageName), (int)(options), 0);
            }

            return material;
        }

        //update the 3D cab DPI table
        public void Update3DDPITable()
        {

            Material UsedMaterial = Material; //use default material

            //update text string
            bool Alert = false;
            //            string speed = CVFR.Get3DDigits(out Alert);
            DPITable.PrepareFrame(DPIStatus);

            //           NumVertices = NumIndices = 0;

            if (Alert)//alert use alert meterial
            {
                if (AlertMaterial == null) AlertMaterial = FindMaterial(true);
                UsedMaterial = AlertMaterial;
            }
            //update vertex texture coordinate
            var numVertices = 0;
            var numIndices = 0;
            var headerIndex = 0;
            string param = "";
            Color color;
            float tX, tY;
            Matrix rot;
            for (int iRow = 0; iRow < DPITable.NumberOfRowsFull; iRow++)
            {
                numIndices = 6 * iRow * (1 + (NumColumns - 1) * MaxDigits);
                numVertices = 4 * iRow * (1 + (NumColumns - 1) * MaxDigits);
                if (DPITable.TableText[iRow, 0].Text == "" || headerIndex >= DPITable.FirstColumn.Length) break;
                // manage row title here
                while (DPITable.TableText[iRow, 0].Text != DPITable.FirstColumn[headerIndex] && headerIndex < DPITable.FirstColumn.Length - 1)
                    headerIndex++;
                tX = 0;
                tY = (headerIndex + 8) * 0.0625f;

                //create first triangle
                TriangleListIndices[numIndices++] = (short)numVertices;
                TriangleListIndices[numIndices++] = (short)(numVertices + 2);
                TriangleListIndices[numIndices++] = (short)(numVertices + 1);
                // Second triangle:
                TriangleListIndices[numIndices++] = (short)numVertices;
                TriangleListIndices[numIndices++] = (short)(numVertices + 3);
                TriangleListIndices[numIndices++] = (short)(numVertices + 2);

                //create vertex
                VertexList[numVertices].TextureCoordinate.X = tX; VertexList[numVertices].TextureCoordinate.Y = tY;
                VertexList[numVertices + 1].TextureCoordinate.X = tX + 0.875f; VertexList[numVertices + 1].TextureCoordinate.Y = tY;
                VertexList[numVertices + 2].TextureCoordinate.X = tX + 0.875f; VertexList[numVertices + 2].TextureCoordinate.Y = tY - 0.0625f;
                VertexList[numVertices + 3].TextureCoordinate.X = tX; VertexList[numVertices + 3].TextureCoordinate.Y = tY - 0.0625f;
                numVertices += 4;
                headerIndex++;

                for (int iCol = 1; iCol < NumColumns; iCol++)
                {
                    numIndices = 6 * (1 + iRow * (1 + (NumColumns - 1) * MaxDigits) + ((iCol - 1) * MaxDigits));
                    numVertices = 4 * (1 + iRow * (1 + (NumColumns - 1) * MaxDigits) + ((iCol - 1) * MaxDigits));
                    param = "";
                    if (DPITable.TableText[iRow, iCol].Text.Length >= 2) param = DPITable.TableText[iRow, iCol].Text.Substring(2);
                    Debug.Assert(param.Length - 1 <= MaxDigits);
                    color = DPITable.TableText[iRow, iCol].Color;
                    var leadingSpaces = 0;
                    for (int iChar = 0; iChar < MaxDigits; iChar++)
                    {
                        if (iChar == 0 && param.Length != 0)
                        {
                            tX = GetTextureCoordX(DPITable.TableSymbol[iRow, iCol].Text, 0);
                            tY = GetTextureCoordY(DPITable.TableSymbol[iRow, iCol].Text, 0, Color.White);
                        }
                        else if (iChar == 1 && param.Length < 5)
                        {
                            // Add a leading space
                            tX = 0.875f;
                            tY = 0.125f;
                            leadingSpaces++ ;
                        }
                        else if (iChar == 2 && param.Length < 3)
                        {
                            // Add a further leading space
                            tX = 0.875f;
                            tY = 0.125f;
                            leadingSpaces++ ;
                        }
                        else if (iChar < param.Length + 1 + leadingSpaces && param.Length != 0)
                        {
                            tX = GetTextureCoordX(param, iChar - 1 - leadingSpaces);
                            tY = GetTextureCoordY(param, iChar - 1 - leadingSpaces, color);
                        }
                        else // space
                        {
                            tX = 0.875f;
                            tY = 0.125f;
                        }
                        //create first triangle
                        TriangleListIndices[numIndices++] = (short)numVertices;
                        TriangleListIndices[numIndices++] = (short)(numVertices + 2);
                        TriangleListIndices[numIndices++] = (short)(numVertices + 1);
                        // Second triangle:
                        TriangleListIndices[numIndices++] = (short)numVertices;
                        TriangleListIndices[numIndices++] = (short)(numVertices + 3);
                        TriangleListIndices[numIndices++] = (short)(numVertices + 2);

                        VertexList[numVertices].TextureCoordinate.X = tX; VertexList[numVertices].TextureCoordinate.Y = tY;
                        VertexList[numVertices + 1].TextureCoordinate.X = tX + 0.125f; VertexList[numVertices + 1].TextureCoordinate.Y = tY;
                        VertexList[numVertices + 2].TextureCoordinate.X = tX + 0.125f; VertexList[numVertices + 2].TextureCoordinate.Y = tY - 0.0625f;
                        VertexList[numVertices + 3].TextureCoordinate.X = tX; VertexList[numVertices + 3].TextureCoordinate.Y = tY - 0.0625f;
                        numVertices += 4;
                    }
                }
            }
            //update the shape primitive
            UpdateShapePrimitive(UsedMaterial);

        }

        private void UpdateShapePrimitive(Material material)
        {
            var indexData = new short[NumIndices];
            Array.Copy(TriangleListIndices, indexData, NumIndices);
            shapePrimitive.SetIndexData(indexData);

            var vertexData = new VertexPositionNormalTexture[NumVertices];
            Array.Copy(VertexList, vertexData, NumVertices);
            shapePrimitive.SetVertexData(vertexData, 0, NumVertices, NumIndices / 3);

            shapePrimitive.SetMaterial(material);
        }

        //ACE MAP: 3rd and 4th rowe used when colour is yellow
        //First 7 rows displayed on a char basis ( 8 chars per row)
        //sugsequent rows are retrieved in a single step
        //Assumed form factor for chars is height = 2 * width

        //01234567
        //89N:.-| 
        //01234567
        //89B  - 
        //Idle
        //Sync
        //Async
        //ID
        //Throttle
        //Load
        //BP
        //Flow
        //Remote
        //ER
        //BC
        //MR
        static float GetTextureCoordX(string param, int iChar)
        {
            float x = 0;
            switch (param)
            {
                case "Idle":
                case "Sync":
                case "Async":
                    x = iChar * 0.125f;
                    break;
                default:
                    var c = param[iChar];
                    switch (c)
                    {
                        case 'N':
                        case 'B': x = 0.25f; break;
                        case ':': x = 0.375f; break;
                        case '.': x = 0.5f; break;
                        case '—':
                        case '-': x = 0.625f; break;
                        case ' ': x = 0.875f; break;
                        case '\u2590': x = 0.75f; break;
                        default:
                            x = (c - '0') % 8 * 0.125f;
                            break;
                    }
                    break;
            }
            if (x < 0) x = 0;
            if (x > 1) x = 1;
            return x;
        }

        static float GetTextureCoordY(string param, int iChar, Color color)
        {
            float y = 0f;
            switch (param)
            {
                case "Idle": return 0.3125f;
                case "Sync": return 0.375f;
                case "Async": return 0.4375f;
                default:
                    var c = param[iChar];
                    if (c == '0' || c == '1' || c == '2' || c == '3' || c == '4' || c == '5' || c == '6' || c == '7') y = 0.0625f;
                    if (c == '8' || c == '9' || c == 'B' || c == ':' || c == '.' || c == '-' || c == 'N' || c == ' ' || c == '—' || c == '\u2590') y = 0.125f;
                    if (color == Color.Yellow) y += 0.125f;
                    return y;
            }
            return 1.0f;
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (!CVFR.IsPowered && CVFR.Control.HideIfDisabled)
                return;

            Update3DDPITable();
            Matrix mx = TrainCarShape.Location.XNAMatrix;
            mx.M41 += (TrainCarShape.Location.TileX - Viewer.Camera.TileX) * 2048;
            mx.M43 += (-TrainCarShape.Location.TileZ + Viewer.Camera.TileZ) * 2048;
            Matrix m = XNAMatrix * mx;

            // TODO: Make this use AddAutoPrimitive instead.
            frame.AddPrimitive(this.shapePrimitive.Material, this.shapePrimitive, RenderPrimitiveGroup.Interior, ref m, ShapeFlags.None);
        }

        internal void Mark()
        {
            shapePrimitive.Mark();
        }
    } // class ThreeDimCabDPI

}
