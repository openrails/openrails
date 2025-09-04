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
using System;
using System.Collections.Generic;
using System.Linq;

namespace Orts.Viewer3D.Popups
{
    public abstract class Control
    {
        public Rectangle Position;
        public object Tag;
        public event Action<Control, Point> Click;

        protected Control(int x, int y, int width, int height)
        {
            Position = new Rectangle(x, y, width, height);
        }

        protected void OnClick(Point mouseControlLocation)
        {
            var click = Click;
            if (click != null)
                click(this, mouseControlLocation);
        }

        public virtual void Initialize(WindowManager windowManager)
        {
        }

        internal abstract void Draw(SpriteBatch spriteBatch, Point offset);

        internal virtual bool HandleMouseDown(WindowMouseEvent e)
        {
            return false;
        }

        internal virtual bool HandleMouseUp(WindowMouseEvent e)
        {
            MouseClick(e);
            return false;
        }

        internal virtual bool HandleMouseMove(WindowMouseEvent e)
        {
            return false;
        }

        internal virtual bool HandleUserInput(WindowMouseEvent e)
        {
            return false;
        }

        internal virtual void MoveBy(int x, int y)
        {
            Position.X += x;
            Position.Y += y;
        }

        internal virtual void MouseClick(WindowMouseEvent e)
        {
            OnClick(new Point(e.MouseDownPosition.X - Position.X, e.MouseDownPosition.Y - Position.Y));
        }
    }

    public class Spacer : Control
    {
        public Spacer(int width, int height)
            : base(0, 0, width, height)
        {
        }

        internal override void Draw(SpriteBatch spriteBatch, Point offset)
        {
        }
    }

    public class Separator : Control
    {
        public int Padding;

        public Separator(int width, int height, int padding)
            : base(0, 0, width, height)
        {
            Padding = padding;
        }

        internal override void Draw(SpriteBatch spriteBatch, Point offset)
        {
            spriteBatch.Draw(WindowManager.WhiteTexture, new Rectangle(offset.X + Position.X + Padding, offset.Y + Position.Y + Padding, Position.Width - 2 * Padding, Position.Height - 2 * Padding), Color.White);
        }
    }

    public enum LabelAlignment
    {
        Left,
        Center,
        Right,
    }

    public class Label : Control
    {
        public string Text;
        public LabelAlignment Align;
        public Color Color;
        protected WindowTextFont Font;

        public Label(int x, int y, int width, int height, string text, LabelAlignment align)
            : base(x, y, width, height)
        {
            Text = text;
            Align = align;
            Color = Color.White;
        }

        public Label(int x, int y, int width, int height, string text)
            : this(x, y, width, height, text, LabelAlignment.Left)
        {
        }

        public Label(int width, int height, string text, LabelAlignment align)
            : this(0, 0, width, height, text, align)
        {
        }

        public Label(int width, int height, string text)
            : this(0, 0, width, height, text, LabelAlignment.Left)
        {
        }

        public override void Initialize(WindowManager windowManager)
        {
            base.Initialize(windowManager);
            Font = windowManager.TextFontDefault;
            if (TrainDrivingWindow.FontToBold || MultiPlayerWindow.FontToBold || TrainDpuWindow.FontToBold || TrainCarOperationsWindow.FontToBold)
            {
                Font = windowManager.TextFontDefaultBold;
            }
        }

        internal override void Draw(SpriteBatch spriteBatch, Point offset)
        {
            Font.Draw(spriteBatch, Position, offset, Text, Align, Color);
        }
    }

    public class LabelMono : Control
    {
        public string Text;
        public LabelAlignment Align;
        public Color Color;
        protected WindowTextFont Font;

        public LabelMono(int x, int y, int width, int height, string text, LabelAlignment align)
            : base(x, y, width, height)
        {
            Text = text;
            Align = align;
            Color = Color.White;
        }

        public LabelMono(int x, int y, int width, int height, string text)
            : this(x, y, width, height, text, LabelAlignment.Left)
        {
        }

        public LabelMono(int width, int height, string text, LabelAlignment align)
            : this(0, 0, width, height, text, align)
        {
        }

        public LabelMono(int width, int height, string text)
            : this(0, 0, width, height, text, LabelAlignment.Left)
        {
        }

        public override void Initialize(WindowManager windowManager)
        {
            base.Initialize(windowManager);
            Font = windowManager.TextFontMonoSpacedBold;
        }

        internal override void Draw(SpriteBatch spriteBatch, Point offset)
        {
            Font.Draw(spriteBatch, Position, offset, Text, Align, Color);
        }
    }

    public class LabelShadow : Control
    {
        public const int ShadowSize = 8;
        public const int ShadowExtraSizeX = 4;
        public const int ShadowExtraSizeY = 0;

        public Color Color;

        public LabelShadow(int x, int y, int width, int height)
            : base(x, y, width, height)
        {
            Color = Color.White;
        }

        public LabelShadow(int width, int height)
            : this(0, 0, width, height)
        {
        }

        internal override void Draw(SpriteBatch spriteBatch, Point offset)
        {
            spriteBatch.Draw(WindowManager.LabelShadowTexture, new Rectangle(offset.X + Position.X - ShadowExtraSizeX, offset.Y + Position.Y - ShadowExtraSizeY, ShadowSize + ShadowExtraSizeY, Position.Height + 2 * ShadowExtraSizeY), new Rectangle(0, 0, ShadowSize, 2 * ShadowSize), Color);
            spriteBatch.Draw(WindowManager.LabelShadowTexture, new Rectangle(offset.X + Position.X - ShadowExtraSizeX + ShadowSize + ShadowExtraSizeY, offset.Y + Position.Y - ShadowExtraSizeY, Position.Width + 2 * ShadowExtraSizeX - 2 * ShadowSize - 2 * ShadowExtraSizeY, Position.Height + 2 * ShadowExtraSizeY), new Rectangle(ShadowSize, 0, ShadowSize, 2 * ShadowSize), Color);
            spriteBatch.Draw(WindowManager.LabelShadowTexture, new Rectangle(offset.X + Position.X + ShadowExtraSizeX - ShadowSize - ShadowExtraSizeY + Position.Width, offset.Y + Position.Y - ShadowExtraSizeY, ShadowSize + ShadowExtraSizeY, Position.Height + 2 * ShadowExtraSizeY), new Rectangle(2 * ShadowSize, 0, ShadowSize, 2 * ShadowSize), Color);
        }
    }

    public class TextFlow : Control
    {
        static readonly char[] Whitespace = new[] { ' ', '\t', '\r', '\n' };

        // Text acts like a member variable but changing it then calls the Reflow method so we can see the changed text.
        public string Text { get { return _text; } set { _text = value; Reflow(); } }
        private string _text;
        public Color Color;
        protected WindowTextFont Font;
        /// <summary>
        /// Lines of text to draw prepared by Updater process
        /// </summary>
        List<string> Lines;
        /// <summary>
        /// Copy of Lines to iterate through by Render process
        /// </summary>
        string[] DrawnLines;

        public TextFlow(int x, int y, int width, string text)
            : base(x, y, width, 0)
        {
            _text = text == null ? "" : text.Replace('\t', ' ');
            Color = Color.White;
        }

        public TextFlow(int width, string text)
            : this(0, 0, width, text)
        {
        }

        public override void Initialize(WindowManager windowManager)
        {
            base.Initialize(windowManager);
            Font = windowManager.TextFontDefault;
            Reflow();
        }

        void Reflow()
        {
            Lines = new List<string>();
            var position = 0;
            while (position < Text.Length)
            {
                var wrap = position;
                var search = position;
                while (search != -1 && Text[search] != '\n' && Font.MeasureString(Text.Substring(position, search - position)) < Position.Width)
                {
                    wrap = search;
                    search = Text.IndexOfAny(Whitespace, search + 1);
                }
                // Possible cases here:
                //   SEARCH    NEWLINE   FITS      WRAP=POS  WRAP AT?
                //   no        no        no        no        wrap
                //   no        no        no        yes       text.length
                //   no        no        yes       no        text.length
                //   no        no        yes       yes       text.length
                //   yes       no        no        no        wrap
                //   yes       no        no        yes       search
                //   yes       yes       no        no        wrap
                //   yes       yes       no        yes       search
                //   yes       yes       yes       no        search
                //   yes       yes       yes       yes       search
                var width = Font.MeasureString(search == -1 ? Text.Substring(position) : Text.Substring(position, search - position));
                if (width < Position.Width || wrap == position)
                    wrap = search == -1 ? Text.Length : search;
                Lines.Add(Text.Substring(position, wrap - position));
                position = wrap + 1;
            }
            Position.Height = Lines.Count * Font.Height;
        }

        internal override void Draw(SpriteBatch spriteBatch, Point offset)
        {
            DrawnLines = Lines.ToArray();
            foreach (var line in DrawnLines)
            {
                Font.Draw(spriteBatch, Position, offset, line, LabelAlignment.Left, Color);
                offset.Y += Font.Height;
            }
        }
    }

    public class Image : Control
    {
        public Texture2D Texture;
        public Rectangle Source;

        public Image(int x, int y, int width, int height)
            : base(x, y, width, height)
        {
            Source = Rectangle.Empty;
        }

        public Image(int width, int height)
            : this(0, 0, width, height)
        {
        }

        internal override void Draw(SpriteBatch spriteBatch, Point offset)
        {
            var destinationRectangle = new Rectangle(offset.X + Position.X, offset.Y + Position.Y, Position.Width, Position.Height);
            if (Texture == null)
                spriteBatch.Draw(WindowManager.WhiteTexture, destinationRectangle, Color.White);
            else
                spriteBatch.Draw(Texture, destinationRectangle, Source, Color.White);
        }
    }

    public abstract class ControlLayout : Control
    {
        public const int SeparatorSize = 5;
        public const int SeparatorPadding = 2;

        protected readonly List<Control> controls = new List<Control>();
        public IEnumerable<Control> Controls { get { return controls; } }
        public int TextHeight { get; internal set; }

        public ControlLayout(int x, int y, int width, int height)
            : base(x, y, width, height)
        {
        }

        public virtual int RemainingWidth
        {
            get
            {
                return Position.Width;
            }
        }

        public virtual int RemainingHeight
        {
            get
            {
                return Position.Height;
            }
        }

        public virtual int CurrentLeft
        {
            get
            {
                return 0;
            }
        }

        public virtual int CurrentTop
        {
            get
            {
                return 0;
            }
        }

        protected T InternalAdd<T>(T control) where T : Control
        {
            var controlLayout = control as ControlLayout;
            if (controlLayout != null)
            {
                controlLayout.TextHeight = TextHeight;
            }
            // Offset control by our position and current values. Don't touch its size!
            control.Position.X += Position.Left + CurrentLeft;
            control.Position.Y += Position.Top + CurrentTop;
            controls.Add(control);
            return control;
        }

        public void Add(Control control)
        {
            InternalAdd(control);
        }

        public void AddSpace(int width, int height)
        {
            Add(new Spacer(width, height));
        }

        public void AddHorizontalSeparator()
        {
            Add(new Separator(RemainingWidth, SeparatorSize, SeparatorPadding));
        }

        public void AddVerticalSeparator()
        {
            Add(new Separator(SeparatorSize, RemainingHeight, SeparatorPadding));
        }

        public ControlLayoutOffset AddLayoutOffset(int left, int top, int right, int bottom)
        {
            return InternalAdd(new ControlLayoutOffset(RemainingWidth, RemainingHeight, left, top, right, bottom));
        }

        public ControlLayoutHorizontal AddLayoutHorizontal()
        {
            return AddLayoutHorizontal(RemainingHeight);
        }

        public ControlLayoutHorizontal AddLayoutHorizontalLineOfText()
        {
            return AddLayoutHorizontal(TextHeight);
        }

        public ControlLayoutHorizontal AddLayoutHorizontal(int height)
        {
            return InternalAdd(new ControlLayoutHorizontal(RemainingWidth, height));
        }

        public ControlLayoutVertical AddLayoutVertical()
        {
            return AddLayoutVertical(RemainingWidth);
        }

        public ControlLayoutVertical AddLayoutVertical(int width)
        {
            return InternalAdd(new ControlLayoutVertical(width, RemainingHeight));
        }

        public ControlLayout AddLayoutScrollboxHorizontal(int height)
        {
            var sb = InternalAdd(new ControlLayoutScrollboxHorizontal(RemainingWidth, height));
            sb.Initialize();
            return sb.Client;
        }

        public ControlLayout AddLayoutScrollboxVertical(int width)
        {
            var sb = InternalAdd(new ControlLayoutScrollboxVertical(width, RemainingHeight));
            sb.Initialize();
            return sb.Client;
        }

        public override void Initialize(WindowManager windowManager)
        {
            base.Initialize(windowManager);
            foreach (var control in Controls)
                control.Initialize(windowManager);
        }

        internal override void Draw(SpriteBatch spriteBatch, Point offset)
        {
            foreach (var control in controls)
                control.Draw(spriteBatch, offset);
        }

        internal override bool HandleMouseDown(WindowMouseEvent e)
        {
            foreach (var control in controls.Where(c => c.Position.Contains(e.MouseDownPosition)))
                if (control.HandleMouseDown(e))
                    return true;
            return base.HandleMouseDown(e);
        }

        internal override bool HandleMouseUp(WindowMouseEvent e)
        {
            foreach (var control in controls.Where(c => c.Position.Contains(e.MouseDownPosition)))
                if (control.HandleMouseUp(e))
                    return true;
            return base.HandleMouseUp(e);
        }

        internal override bool HandleMouseMove(WindowMouseEvent e)
        {
            foreach (var control in controls.Where(c => c.Position.Contains(e.MouseDownPosition)))
                if (control.HandleMouseMove(e))
                    return true;
            return base.HandleMouseMove(e);
        }

        internal override bool HandleUserInput(WindowMouseEvent e)
        {
            foreach (var control in controls.Where(c => c.Position.Contains(e.MouseDownPosition)))
                if (control.HandleUserInput(e))
                    return true;
            return base.HandleUserInput(e);
        }

        internal override void MoveBy(int x, int y)
        {
            foreach (var control in controls)
                control.MoveBy(x, y);
            base.MoveBy(x, y);
        }
    }

    public class ControlLayoutOffset : ControlLayout
    {
        internal ControlLayoutOffset(int width, int height, int left, int top, int right, int bottom)
            : base(left, top, width - left - right, height - top - bottom)
        {
        }
    }

    public class ControlLayoutHorizontal : ControlLayout
    {
        internal ControlLayoutHorizontal(int width, int height)
            : base(0, 0, width, height)
        {
        }

        public override int RemainingWidth
        {
            get
            {
                return base.RemainingWidth - CurrentLeft;
            }
        }

        public override int CurrentLeft
        {
            get
            {
                return controls.Count > 0 ? controls.Max(c => c.Position.Right) - Position.Left : 0;
            }
        }
    }

    public class ControlLayoutVertical : ControlLayout
    {
        internal ControlLayoutVertical(int width, int height)
            : base(0, 0, width, height)
        {
        }

        public override int RemainingHeight
        {
            get
            {
                return base.RemainingHeight - CurrentTop;
            }
        }

        public override int CurrentTop
        {
            get
            {
                return controls.Count > 0 ? controls.Max(c => c.Position.Bottom) - Position.Top : 0;
            }
        }
    }

    public abstract class ControlLayoutScrollbox : ControlLayout
    {
        public ControlLayout Client;
        protected int ScrollPosition;

        protected ControlLayoutScrollbox(int width, int height)
            : base(0, 0, width, height)
        {
        }

        internal abstract void Initialize();

        public abstract int ScrollSize { get; }

        public abstract int GetScrollPosition();
        public abstract void SetScrollPosition(int position);

        internal RasterizerState ScissorTestEnable = new RasterizerState { ScissorTestEnable = true };
    }

    public class ControlLayoutScrollboxHorizontal : ControlLayoutScrollbox
    {
        internal ControlLayoutScrollboxHorizontal(int width, int height)
            : base(width, height)
        {
        }

        bool Dragging;
        float DragThumbOffset;

        internal override void Initialize()
        {
            Client = InternalAdd(new ControlLayoutHorizontal(RemainingWidth, RemainingHeight));
        }

        internal override void Draw(SpriteBatch spriteBatch, Point offset)
        {
            var thumbOffset = (int)((float)(Position.Width - 3 * TextHeight) * (float)ScrollPosition / (float)ScrollSize);

            // Left button
            spriteBatch.Draw(WindowManager.ScrollbarTexture, new Rectangle(offset.X + Position.X, offset.Y + Position.Y + Position.Height - TextHeight, TextHeight, TextHeight), new Rectangle(0, 0, 16, 16), Color.White);
            // Left gutter
            spriteBatch.Draw(WindowManager.ScrollbarTexture, new Rectangle(offset.X + Position.X + TextHeight, offset.Y + Position.Y + Position.Height - TextHeight, thumbOffset, TextHeight), new Rectangle(2 * 16, 0, 16, 16), Color.White);
            // Thumb
            spriteBatch.Draw(WindowManager.ScrollbarTexture, new Rectangle(offset.X + Position.X + TextHeight + thumbOffset, offset.Y + Position.Y + Position.Height - TextHeight, TextHeight, TextHeight), new Rectangle(ScrollSize > 0 ? 16 : 2 * 16, 0, 16, 16), Color.White);
            // Right gutter
            spriteBatch.Draw(WindowManager.ScrollbarTexture, new Rectangle(offset.X + Position.X + 2 * TextHeight + thumbOffset, offset.Y + Position.Y + Position.Height - TextHeight, Position.Width - 3 * TextHeight - thumbOffset, TextHeight), new Rectangle(2 * 16, 0, 16, 16), Color.White);
            // Right button
            spriteBatch.Draw(WindowManager.ScrollbarTexture, new Rectangle(offset.X + Position.X + Position.Width - TextHeight, offset.Y + Position.Y + Position.Height - TextHeight, TextHeight, TextHeight), new Rectangle(3 * 16, 0, 16, 16), Color.White);

            // Draw contents inside a scissor rectangle (so they're clipped to the client area).
            WindowManager.Flush(spriteBatch);
            var oldScissorRectangle = spriteBatch.GraphicsDevice.ScissorRectangle;
            spriteBatch.GraphicsDevice.ScissorRectangle = new Rectangle(offset.X + Position.X, offset.Y + Position.Y, Position.Width, Position.Height - TextHeight);
            spriteBatch.GraphicsDevice.RasterizerState = ScissorTestEnable;
            base.Draw(spriteBatch, offset);
            WindowManager.Flush(spriteBatch);
            spriteBatch.GraphicsDevice.ScissorRectangle = oldScissorRectangle;
            spriteBatch.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
        }

        internal override bool HandleUserInput(WindowMouseEvent e)
        {
            if (UserInput.IsMouseLeftButtonDown)
            {
                Client.Position.Width = Client.CurrentLeft;
                if (e.MouseDownPosition.Y > Position.Bottom - TextHeight)
                {
                    var thumbOffset = (int)((float)(Position.Width - 3 * TextHeight) * (float)ScrollPosition / (float)ScrollSize);

                    // Mouse down occured within the scrollbar.
                    if (!Dragging && e.MouseDownPosition.X < Position.Left + TextHeight)
                        // Mouse down occured on left button.
                        SetScrollPosition(ScrollPosition - 10);
                    else if (!Dragging && e.MouseDownPosition.X < Position.Left + TextHeight + thumbOffset)
                        // Mouse down occured on left gutter.
                        SetScrollPosition(ScrollPosition - 100);
                    else if (!Dragging && e.MouseDownPosition.X > Position.Right - TextHeight)
                        // Mouse down occured on right button.
                        SetScrollPosition(ScrollPosition + 10);
                    else if (!Dragging && e.MouseDownPosition.X > Position.Left + 2 * TextHeight + thumbOffset)
                        // Mouse down occured on right gutter.
                        SetScrollPosition(ScrollPosition + 100);
                    else if (e.MouseDownPosition.X > Position.Left + TextHeight + thumbOffset && e.MouseDownPosition.X < Position.Left + 2 * TextHeight + thumbOffset && !Dragging)
                    {
                        // Mouse down occured on the thumb
                        Dragging = true;
                        DragThumbOffset = e.MousePosition.X - (Position.Left + TextHeight + thumbOffset);
                    }

                    return true;
                }
            }
            return base.HandleUserInput(e);
        }

        internal override bool HandleMouseMove(WindowMouseEvent e)
        {
            if (UserInput.IsMouseLeftButtonDown && Dragging && UserInput.MouseMovedLeft)
            {
                SetScrollPosition(ScrollPosition - (int)(DragThumbOffset * UserInput.MouseSpeedX));
            }

            if (UserInput.IsMouseLeftButtonDown && Dragging && UserInput.MouseMovedRight)
            {
                SetScrollPosition(ScrollPosition + (int)(DragThumbOffset * UserInput.MouseSpeedX));
            }

            return base.HandleMouseMove(e);
        }

        internal override bool HandleMouseUp(WindowMouseEvent e)
        {
            if (Dragging)
                Dragging = false;

            return base.HandleMouseUp(e);
        }

        public override int RemainingHeight
        {
            get
            {
                return base.RemainingHeight - TextHeight;
            }
        }

        public override int ScrollSize
        {
            get
            {
                return Client.CurrentLeft - Position.Width;
            }
        }

        public override int GetScrollPosition()
        {
            return ScrollPosition;
        }

        public override void SetScrollPosition(int position)
        {
            position = Math.Max(0, Math.Min(Math.Max(0, ScrollSize), position));
            Client.MoveBy(ScrollPosition - position, 0);
            ScrollPosition = position;
        }
    }

    public class ControlLayoutScrollboxVertical : ControlLayoutScrollbox
    {
        public static ControlLayout NewClient
        {
            get;
            set;
        }
        internal ControlLayoutScrollboxVertical(int width, int height)
            : base(width, height)
        {
        }

        float DragThumbOffset;
        bool Dragging;

        internal override void Initialize()
        {
            Client = InternalAdd(new ControlLayoutVertical(RemainingWidth, RemainingHeight));
            NewClient = Client;
        }

        internal override void Draw(SpriteBatch spriteBatch, Point offset)
        {
            var thumbOffset = (int)((float)(Position.Height - 3 * TextHeight) * (float)ScrollPosition / (float)ScrollSize);
            var rotateOrigin = new Vector2(0, 16);

            // Top button
            spriteBatch.Draw(WindowManager.ScrollbarTexture, new Rectangle(offset.X + Position.X + Position.Width - TextHeight, offset.Y + Position.Y, TextHeight, TextHeight), new Rectangle(0, 0, 16, 16), Color.White, (float)Math.PI / 2, rotateOrigin, SpriteEffects.None, 0);
            // Top gutter
            spriteBatch.Draw(WindowManager.ScrollbarTexture, new Rectangle(offset.X + Position.X + Position.Width - TextHeight, offset.Y + Position.Y + TextHeight, thumbOffset, TextHeight), new Rectangle(2 * 16, 0, 16, 16), Color.White, (float)Math.PI / 2, rotateOrigin, SpriteEffects.None, 0);
            // Thumb
            spriteBatch.Draw(WindowManager.ScrollbarTexture, new Rectangle(offset.X + Position.X + Position.Width - TextHeight, offset.Y + Position.Y + TextHeight + thumbOffset, TextHeight, TextHeight), new Rectangle(ScrollSize > 0 ? 16 : 2 * 16, 0, 16, 16), Color.White, (float)Math.PI / 2, rotateOrigin, SpriteEffects.None, 0);
            // Bottom gutter
            spriteBatch.Draw(WindowManager.ScrollbarTexture, new Rectangle(offset.X + Position.X + Position.Width - TextHeight, offset.Y + Position.Y + 2 * TextHeight + thumbOffset, Position.Height - 3 * TextHeight - thumbOffset, TextHeight), new Rectangle(2 * 16, 0, 16, 16), Color.White, (float)Math.PI / 2, rotateOrigin, SpriteEffects.None, 0);
            // Bottom button
            spriteBatch.Draw(WindowManager.ScrollbarTexture, new Rectangle(offset.X + Position.X + Position.Width - TextHeight, offset.Y + Position.Y + Position.Height - TextHeight, TextHeight, TextHeight), new Rectangle(3 * 16, 0, 16, 16), Color.White, (float)Math.PI / 2, rotateOrigin, SpriteEffects.None, 0);

            // Draw contents inside a scissor rectangle (so they're clipped to the client area).
            WindowManager.Flush(spriteBatch);
            var oldScissorRectangle = spriteBatch.GraphicsDevice.ScissorRectangle;
            spriteBatch.GraphicsDevice.ScissorRectangle = new Rectangle(offset.X + Position.X, offset.Y + Position.Y, Position.Width - TextHeight, Position.Height);
            spriteBatch.GraphicsDevice.RasterizerState = ScissorTestEnable;
            base.Draw(spriteBatch, offset);
            WindowManager.Flush(spriteBatch);
            spriteBatch.GraphicsDevice.ScissorRectangle = oldScissorRectangle;
            spriteBatch.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
        }

        internal override bool HandleUserInput(WindowMouseEvent e)
        {
            if (UserInput.IsMouseLeftButtonDown)
            {
                Client.Position.Height = Client.CurrentTop;
                
                if (e.MouseDownPosition.X > Position.Right - TextHeight)
                {
                    var thumbOffset = (int)((float)(Position.Height - 3 * TextHeight) * (float)ScrollPosition / (float)ScrollSize);

                    // Mouse down occured within the scrollbar.
                    if (!Dragging && e.MouseDownPosition.Y < Position.Top + TextHeight)
                        // Mouse down occured on top button.
                        SetScrollPosition(ScrollPosition - 10);
                    else if (!Dragging && e.MouseDownPosition.Y < Position.Top + TextHeight + thumbOffset)
                        // Mouse down occured on top gutter.
                        SetScrollPosition(ScrollPosition - 100);
                    else if (!Dragging && e.MouseDownPosition.Y > Position.Bottom - TextHeight)
                        // Mouse down occured on bottom button.
                        SetScrollPosition(ScrollPosition + 10);
                    else if (!Dragging && e.MouseDownPosition.Y > Position.Top + 2 * TextHeight + thumbOffset)
                        // Mouse down occured on bottom gutter.
                        SetScrollPosition(ScrollPosition + 100);
                    else if (e.MouseDownPosition.Y > Position.Top + TextHeight + thumbOffset && e.MouseDownPosition.Y < Position.Top + 2 * TextHeight + thumbOffset && !Dragging)
                    {
                        Dragging = true;
                        DragThumbOffset = e.MousePosition.Y - (Position.Top + TextHeight + thumbOffset);
                    }

                    return true;
                }
            }

            if (UserInput.IsMouseWheelChanged)
                SetScrollPosition(ScrollPosition - UserInput.MouseWheelChange);

            return base.HandleUserInput(e);
        }

        internal override bool HandleMouseMove(WindowMouseEvent e)
        {
            if (UserInput.IsMouseLeftButtonDown && Dragging && UserInput.MouseMovedUp)
            {
                SetScrollPosition(ScrollPosition - (int)(DragThumbOffset * UserInput.MouseSpeedY));
            }

            if (UserInput.IsMouseLeftButtonDown && Dragging && UserInput.MouseMovedDown)
            {
                SetScrollPosition(ScrollPosition + (int)(DragThumbOffset * UserInput.MouseSpeedY));
            }

            return base.HandleMouseMove(e);
        }

        internal override bool HandleMouseUp(WindowMouseEvent e)
        {
            if (Dragging)
                Dragging = false;               

            return base.HandleMouseUp(e);
        }

        public override int RemainingWidth
        {
            get
            {
                return base.RemainingWidth - TextHeight;
            }
        }

        public override int ScrollSize
        {
            get
            {
                return Client.CurrentTop - Position.Height;
            }
        }

        public override int GetScrollPosition()
        {
            return(ScrollPosition);
        }

        public override void SetScrollPosition(int position)
        {
            position = Math.Max(0, Math.Min(Math.Max(0, ScrollSize), position));
            Client.MoveBy(0, ScrollPosition - position);
            ScrollPosition = position;
        }
    }
}
