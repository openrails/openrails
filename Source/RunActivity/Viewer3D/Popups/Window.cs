// COPYRIGHT 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
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
using ORTS.Common;
using ORTS.Settings;
using System;
using System.IO;

namespace Orts.Viewer3D.Popups
{
    public abstract class Window : RenderPrimitive
    {
        const int BaseFontSize = 16; // DO NOT CHANGE without also changing the graphics for the windows.

        public static readonly Point DecorationOffset = new Point(4, 4 + BaseFontSize + 5);
        public static readonly Point DecorationSize = new Point(4 + 4, 4 + BaseFontSize + 5 + 4);
        public Matrix XNAWorld;

        protected WindowManager Owner;

        bool visible;
        Rectangle location;

        readonly string Caption;
        readonly SavingProperty<int[]> SettingsProperty;
        ControlLayout WindowLayout;
        VertexBuffer WindowVertexBuffer;
        IndexBuffer WindowIndexBuffer;

        public Window(WindowManager owner, int width, int height, string caption)
        {
            Owner = owner;
            // We need to correct the window height for the ACTUAL font size, so that the title bar is shown correctly.
            location = new Rectangle(0, 0, width, height - BaseFontSize + owner.TextFontDefault.Height);

            SettingsProperty = Owner.Viewer.Settings.GetSavingProperty<int[]>("WindowPosition_" + GetType().Name.Replace("Window", ""));
            if (SettingsProperty != null)
            {
                var value = SettingsProperty.Value;
                if ((value != null) && (value.Length >= 2))
                {
                    location.X = (int)Math.Round((float)value[0] * (Owner.ScreenSize.X - location.Width) / 100);
                    location.Y = (int)Math.Round((float)value[1] * (Owner.ScreenSize.Y - location.Height) / 100);
                }
            }

            Caption = caption;
            Owner.Add(this);
        }

        protected internal virtual void Initialize()
        {
            VisibilityChanged();
            LocationChanged();
            SizeChanged();
        }

        protected internal virtual void Save(BinaryWriter outf)
        {
            outf.Write(visible);
            outf.Write((float)location.X / (Owner.ScreenSize.X - location.Width));
            outf.Write((float)location.Y / (Owner.ScreenSize.Y - location.Height));
        }

        protected internal virtual void Restore(BinaryReader inf)
        {
            visible = inf.ReadBoolean();
            var x = location.X;
            var y = location.Y;
            location.X = (int)(inf.ReadSingle() * (Owner.ScreenSize.X - location.Width));
            location.Y = (int)(inf.ReadSingle() * (Owner.ScreenSize.Y - location.Height));
            // This is needed to move the window background to the correct position
            if ((location.X != x) || (location.Y != y))
                LocationChanged();
        }

        protected virtual void VisibilityChanged()
        {
            if (Visible)
                Owner.BringWindowToTop(this);
            else
                Owner.WriteWindowZOrder();
            if (Visible && (WindowLayout != null))
                PrepareFrame(ElapsedTime.Zero, true);
        }

        protected virtual void LocationChanged()
        {
            SettingsProperty?.SetValue(new[] { (int)Math.Round(100f * location.X / (Owner.ScreenSize.X - location.Width)), (int)Math.Round(100f * location.Y / (Owner.ScreenSize.Y - location.Height)) });

            XNAWorld = Matrix.CreateWorld(new Vector3(location.X, location.Y, 0), -Vector3.UnitZ, Vector3.UnitY);
        }

        protected virtual void SizeChanged()
        {
            Layout();
            WindowVertexBuffer = null;
        }

        internal virtual void ActiveChanged()
        {
        }

        internal virtual void ScreenChanged()
        {
        }

        public bool Visible
        {
            get
            {
                return visible;
            }
            set
            {
                if (visible != value)
                {
                    visible = value;
                    VisibilityChanged();
                }
            }
        }

        public virtual bool Interactive
        {
            get
            {
                return true;
            }
        }

        public virtual bool TopMost
        {
            get
            {
                return false;
            }
        }

        public Rectangle Location
        {
            get
            {
                return location;
            }
        }

        public virtual void TabAction()
        {
        }

        public void MoveTo(int x, int y)
        {
            x = (int)MathHelper.Clamp(x, 0, Owner.ScreenSize.X - location.Width);
            y = (int)MathHelper.Clamp(y, 0, Owner.ScreenSize.Y - location.Height);

            if ((location.X != x) || (location.Y != y))
            {
                location.X = x;
                location.Y = y;
                LocationChanged();
            }
        }

        public void MoveBy(int dx, int dy)
        {
            MoveTo(location.X + dx, location.Y + dy);
        }

        public void SizeTo(int width, int height)
        {
            if ((location.Width != width) || (location.Height != height))
            {
                location.Width = width;
                location.Height = height;
                MoveTo(location.X, location.Y);
                SizeChanged();
            }
        }

        protected internal void Layout()
        {
            var windowLayout = new WindowControlLayout(this, location.Width, location.Height);
            windowLayout.TextHeight = Owner.TextFontDefault.Height;
            if (Owner.ScreenSize != Point.Zero)
                Layout(windowLayout);
            windowLayout.Initialize(Owner);
            WindowLayout = windowLayout;
        }

        protected virtual ControlLayout Layout(ControlLayout layout)
        {
            // Pad window by 4px, add caption and space between to content area.
            var content = layout.AddLayoutOffset(4, 4, 4, 4).AddLayoutVertical();
            content.Add(new Label(content.RemainingWidth, Owner.TextFontDefault.Height, Caption, LabelAlignment.Center));
            content.AddSpace(0, 5);
            return content;
        }

        [CallOnThread("Updater")]
        public virtual void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime, bool updateFull)
        {
            if (Visible)
                PrepareFrame(elapsedTime, updateFull);
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            if (WindowVertexBuffer == null)
            {
                // Edges/corners are 32px (1/4th image size).
                var gp = 32 - BaseFontSize + Owner.TextFontDefault.Height;
                var vertexData = new[] {
					//  0  1  2  3
					new VertexPositionTexture(new Vector3(0 * location.Width + 00, 0 * location.Height + 00, 0), new Vector2(0.00f / 2, 0.00f)),
					new VertexPositionTexture(new Vector3(0 * location.Width + gp, 0 * location.Height + 00, 0), new Vector2(0.25f / 2, 0.00f)),
					new VertexPositionTexture(new Vector3(1 * location.Width - gp, 0 * location.Height + 00, 0), new Vector2(0.75f / 2, 0.00f)),
					new VertexPositionTexture(new Vector3(1 * location.Width - 00, 0 * location.Height + 00, 0), new Vector2(1.00f / 2, 0.00f)),
					//  4  5  6  7
					new VertexPositionTexture(new Vector3(0 * location.Width + 00, 0 * location.Height + gp, 0), new Vector2(0.00f / 2, 0.25f)),
					new VertexPositionTexture(new Vector3(0 * location.Width + gp, 0 * location.Height + gp, 0), new Vector2(0.25f / 2, 0.25f)),
					new VertexPositionTexture(new Vector3(1 * location.Width - gp, 0 * location.Height + gp, 0), new Vector2(0.75f / 2, 0.25f)),
					new VertexPositionTexture(new Vector3(1 * location.Width - 00, 0 * location.Height + gp, 0), new Vector2(1.00f / 2, 0.25f)),
					//  8  9 10 11
					new VertexPositionTexture(new Vector3(0 * location.Width + 00, 1 * location.Height - gp, 0), new Vector2(0.00f / 2, 0.75f)),
					new VertexPositionTexture(new Vector3(0 * location.Width + gp, 1 * location.Height - gp, 0), new Vector2(0.25f / 2, 0.75f)),
					new VertexPositionTexture(new Vector3(1 * location.Width - gp, 1 * location.Height - gp, 0), new Vector2(0.75f / 2, 0.75f)),
					new VertexPositionTexture(new Vector3(1 * location.Width - 00, 1 * location.Height - gp, 0), new Vector2(1.00f / 2, 0.75f)),
					// 12 13 14 15
					new VertexPositionTexture(new Vector3(0 * location.Width + 00, 1 * location.Height - 00, 0), new Vector2(0.00f / 2, 1.00f)),
					new VertexPositionTexture(new Vector3(0 * location.Width + gp, 1 * location.Height - 00, 0), new Vector2(0.25f / 2, 1.00f)),
					new VertexPositionTexture(new Vector3(1 * location.Width - gp, 1 * location.Height - 00, 0), new Vector2(0.75f / 2, 1.00f)),
					new VertexPositionTexture(new Vector3(1 * location.Width - 00, 1 * location.Height - 00, 0), new Vector2(1.00f / 2, 1.00f)),
				};
                WindowVertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionTexture), vertexData.Length, BufferUsage.WriteOnly);
                WindowVertexBuffer.SetData(vertexData);
            }
            if (WindowIndexBuffer == null)
            {
                var indexData = new short[] {
					0, 4, 1, 5, 2, 6, 3, 7,
					11, 6, 10, 5, 9, 4, 8,
					12, 9, 13, 10, 14, 11, 15,
				};
                WindowIndexBuffer = new IndexBuffer(graphicsDevice, typeof(short), indexData.Length, BufferUsage.WriteOnly);
                WindowIndexBuffer.SetData(indexData);
            }

            graphicsDevice.SetVertexBuffer(WindowVertexBuffer);
			graphicsDevice.Indices = WindowIndexBuffer;
			graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleStrip, baseVertex: 0, startIndex: 0, primitiveCount: 20);
		}

        [CallOnThread("Updater")]
        public virtual void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
        }

        [CallOnThread("Render")]
        public virtual void Draw(SpriteBatch spriteBatch)
        {
            WindowLayout.Draw(spriteBatch, Location.Location);
        }

        public void MouseDown()
        {
            WindowLayout.HandleMouseDown(new WindowMouseEvent(Owner, this));
        }

        public void MouseUp()
        {
            WindowLayout.HandleMouseUp(new WindowMouseEvent(Owner, this));
        }

        public void MouseMove()
        {
            WindowLayout.HandleMouseMove(new WindowMouseEvent(Owner, this));
        }

        public void HandleUserInput()
        {
            WindowLayout.HandleUserInput(new WindowMouseEvent(Owner, this));
        }

        public virtual void Mark()
        {
        }
    }

    public class WindowMouseEvent
    {
        public readonly Point MousePosition;
        public readonly Point MouseDownPosition;
        public readonly Point MouseScreenPosition;
        public readonly Point MouseDownScreenPosition;

        public WindowMouseEvent(WindowManager windowManager, Window window)
        {
            MousePosition = new Point(UserInput.MouseX - window.Location.X, UserInput.MouseY - window.Location.Y);
            MouseDownPosition = new Point(windowManager.MouseDownPosition.X - window.Location.X, windowManager.MouseDownPosition.Y - window.Location.Y);
            MouseScreenPosition = new Point(UserInput.MouseX, UserInput.MouseY);
            MouseDownScreenPosition = windowManager.MouseDownPosition;
        }
    }

    class WindowControlLayout : ControlLayout
    {
        public readonly Window Window;

        public WindowControlLayout(Window window, int width, int height)
            : base(0, 0, width, height)
        {
            Window = window;
        }

        static readonly Point DragInvalid = new Point(-1, -1);
        Point DragWindowOffset;
        bool Dragging;

        internal override bool HandleMouseDown(WindowMouseEvent e)
        {
            DragWindowOffset = DragInvalid;
         
            if (base.HandleMouseDown(e))
                return true;

            // prevent from dragging when clicking on vertical scrollbar
            if (MathHelper.Distance(base.RemainingWidth, e.MousePosition.X) < 20)
                return false;

            // prevent from dragging when clicking on horizontal scrollbar
            if (MathHelper.Distance(base.RemainingHeight, e.MousePosition.Y) < 20)
                return false;

            DragWindowOffset = new Point(e.MouseDownScreenPosition.X - Window.Location.X, e.MouseDownScreenPosition.Y - Window.Location.Y);
            return true;
        }

        internal override bool HandleMouseUp(WindowMouseEvent e)
        {
            if (base.HandleMouseUp(e))
                return true;
            if (Dragging)
                Dragging = false;
            return true;
        }

        internal override bool HandleMouseMove(WindowMouseEvent e)
        {
            if (base.HandleMouseMove(e))
                return true;
            if (UserInput.IsMouseLeftButtonDown && !Dragging && (DragWindowOffset != DragInvalid) && ((MathHelper.Distance(e.MouseScreenPosition.X, e.MouseDownScreenPosition.X) > WindowManager.DragMinimumDistance) || (MathHelper.Distance(e.MouseScreenPosition.Y, e.MouseDownScreenPosition.Y) > WindowManager.DragMinimumDistance)))
                Dragging = true;
            else if (UserInput.IsMouseLeftButtonDown && Dragging)
                Window.MoveTo(e.MouseScreenPosition.X - DragWindowOffset.X, e.MouseScreenPosition.Y - DragWindowOffset.Y);
            return true;
        }

        internal override bool HandleUserInput(WindowMouseEvent e)
        {
            if (base.HandleUserInput(e))
                return true;
            return true;
        }
    }
}
