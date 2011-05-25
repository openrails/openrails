// COPYRIGHT 2010, 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ORTS.Popups
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

		public string Text;
		public Color Color;
        protected WindowTextFont Font;
        List<string> Lines;

		public TextFlow(int x, int y, int width, string text)
			: base(x, y, width, 0)
		{
			Text = text.Replace('\t', ' ');
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
                if (search == -1)
                    wrap = Text.Length;
                else if (Text[search] == '\n')
                    wrap = search;
                else if (wrap == position)
                    wrap = search;
                Lines.Add(Text.Substring(position, wrap - position));
                position = wrap + 1;
            }
            Position.Height = Lines.Count * Font.Height;
        }

        internal override void Draw(SpriteBatch spriteBatch, Point offset)
        {
            foreach (var line in Lines)
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
		protected readonly List<Control> controls = new List<Control>();
		public IEnumerable<Control> Controls { get { return controls; } }

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
			Add(new Separator(RemainingWidth, 5, 2));
		}

		public void AddVerticalSeparator()
		{
			Add(new Separator(5, RemainingHeight, 2));
		}

		public ControlLayoutOffset AddLayoutOffset(int left, int top, int right, int bottom)
		{
			return InternalAdd(new ControlLayoutOffset(RemainingWidth, RemainingHeight, left, top, right, bottom));
		}

		public ControlLayoutHorizontal AddLayoutHorizontal()
		{
			return AddLayoutHorizontal(RemainingHeight);
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
		readonly int PadLeft;
		readonly int PadTop;
		readonly int PadRight;
		readonly int PadBottom;

		internal ControlLayoutOffset(int width, int height, int left, int top, int right, int bottom)
			: base(left, top, width - left - right, height - top - bottom)
		{
			PadLeft = left;
			PadTop = top;
			PadRight = right;
			PadBottom = bottom;
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
		public const int ScrollbarSize = 16;
		public ControlLayout Client;
		protected int ScrollPosition;

		protected ControlLayoutScrollbox(int width, int height)
			: base(0, 0, width, height)
		{
		}

		internal abstract void Initialize();

		public abstract int ScrollSize { get; }
	}

	public class ControlLayoutScrollboxHorizontal : ControlLayoutScrollbox
	{
		internal ControlLayoutScrollboxHorizontal(int width, int height)
			: base(width, height)
		{
		}

		internal override void Initialize()
		{
			Client = InternalAdd(new ControlLayoutHorizontal(RemainingWidth, RemainingHeight));
		}

		internal override void Draw(SpriteBatch spriteBatch, Point offset)
		{
			var thumbOffset = (int)((float)(Position.Width - 3 * ScrollbarSize) * (float)ScrollPosition / (float)ScrollSize);

			// Left button
			spriteBatch.Draw(WindowManager.ScrollbarTexture, new Rectangle(offset.X + Position.X, offset.Y + Position.Y + Position.Height - ScrollbarSize, ScrollbarSize, ScrollbarSize), new Rectangle(0, 0, ScrollbarSize, ScrollbarSize), Color.White);
			// Left gutter
			spriteBatch.Draw(WindowManager.ScrollbarTexture, new Rectangle(offset.X + Position.X + ScrollbarSize, offset.Y + Position.Y + Position.Height - ScrollbarSize, thumbOffset, ScrollbarSize), new Rectangle(2 * ScrollbarSize, 0, ScrollbarSize, ScrollbarSize), Color.White);
			// Thumb
            spriteBatch.Draw(WindowManager.ScrollbarTexture, new Rectangle(offset.X + Position.X + ScrollbarSize + thumbOffset, offset.Y + Position.Y + Position.Height - ScrollbarSize, ScrollbarSize, ScrollbarSize), new Rectangle(ScrollSize > 0 ? ScrollbarSize : 2 * ScrollbarSize, 0, ScrollbarSize, ScrollbarSize), Color.White);
			// Right gutter
			spriteBatch.Draw(WindowManager.ScrollbarTexture, new Rectangle(offset.X + Position.X + 2 * ScrollbarSize + thumbOffset, offset.Y + Position.Y + Position.Height - ScrollbarSize, Position.Width - 3 * ScrollbarSize - thumbOffset, ScrollbarSize), new Rectangle(2 * ScrollbarSize, 0, ScrollbarSize, ScrollbarSize), Color.White);
			// Right button
			spriteBatch.Draw(WindowManager.ScrollbarTexture, new Rectangle(offset.X + Position.X + Position.Width - ScrollbarSize, offset.Y + Position.Y + Position.Height - ScrollbarSize, ScrollbarSize, ScrollbarSize), new Rectangle(3 * ScrollbarSize, 0, ScrollbarSize, ScrollbarSize), Color.White);

			// Draw contents inside a scissor rectangle (so they're clipped to the client area).
            WindowManager.Flush(spriteBatch);
            var oldScissorRectangle = spriteBatch.GraphicsDevice.ScissorRectangle;
			spriteBatch.GraphicsDevice.ScissorRectangle = new Rectangle(offset.X + Position.X, offset.Y + Position.Y, Position.Width, Position.Height - ScrollbarSize);
			spriteBatch.GraphicsDevice.RenderState.ScissorTestEnable = true;
            base.Draw(spriteBatch, offset);
            WindowManager.Flush(spriteBatch);
            spriteBatch.GraphicsDevice.ScissorRectangle = oldScissorRectangle;
			spriteBatch.GraphicsDevice.RenderState.ScissorTestEnable = false;
        }

		internal override bool HandleUserInput(WindowMouseEvent e)
		{
			if (UserInput.IsMouseLeftButtonDown())
			{
				Client.Position.Width = Client.CurrentLeft;
				if (e.MouseDownPosition.Y > Position.Bottom - ScrollbarSize)
				{
					var thumbOffset = (int)((float)(Position.Width - 3 * ScrollbarSize) * (float)ScrollPosition / (float)ScrollSize);

					// Mouse down occured within the scrollbar.
					if (e.MouseDownPosition.X < Position.Left + ScrollbarSize)
					{
						// Mouse down occured on left button.
						var newScrollPosition = Math.Max(0, ScrollPosition - 10);
						Client.MoveBy(ScrollPosition - newScrollPosition, 0);
						ScrollPosition = newScrollPosition;
					}
					else if (e.MouseDownPosition.X < Position.Left + ScrollbarSize + thumbOffset)
					{
						// Mouse down occured on left gutter.
						var newScrollPosition = Math.Max(0, ScrollPosition - 100);
						Client.MoveBy(ScrollPosition - newScrollPosition, 0);
						ScrollPosition = newScrollPosition;
					}
					else if (e.MouseDownPosition.X > Position.Right - ScrollbarSize)
					{
						// Mouse down occured on right button.
						var newScrollPosition = Math.Min(ScrollPosition + 10, Math.Max(0, ScrollSize));
						Client.MoveBy(ScrollPosition - newScrollPosition, 0);
						ScrollPosition = newScrollPosition;
					}
					else if (e.MouseDownPosition.X > Position.Left + 2 * ScrollbarSize + thumbOffset)
					{
						// Mouse down occured on right gutter.
                        var newScrollPosition = Math.Min(ScrollPosition + 100, Math.Max(0, ScrollSize));
						Client.MoveBy(ScrollPosition - newScrollPosition, 0);
						ScrollPosition = newScrollPosition;
					}
					return true;
				}
			}
			return base.HandleUserInput(e);
		}

		public override int RemainingHeight
		{
			get
			{
				return base.RemainingHeight - ScrollbarSize;
			}
		}

		public override int ScrollSize
		{
			get {
				return Client.CurrentLeft - Position.Width;
			}
		}
	}

	public class ControlLayoutScrollboxVertical : ControlLayoutScrollbox
	{
		internal ControlLayoutScrollboxVertical(int width, int height)
			: base(width, height)
		{
		}

		internal override void Initialize()
		{
			Client = InternalAdd(new ControlLayoutVertical(RemainingWidth, RemainingHeight));
		}

        internal override void Draw(SpriteBatch spriteBatch, Point offset)
        {
            var thumbOffset = (int)((float)(Position.Height - 3 * ScrollbarSize) * (float)ScrollPosition / (float)ScrollSize);
            var rotateOrigin = new Vector2(0, ScrollbarSize);

            // Top button
            spriteBatch.Draw(WindowManager.ScrollbarTexture, new Rectangle(offset.X + Position.X + Position.Width - ScrollbarSize, offset.Y + Position.Y, ScrollbarSize, ScrollbarSize), new Rectangle(0, 0, ScrollbarSize, ScrollbarSize), Color.White, (float)Math.PI / 2, rotateOrigin, SpriteEffects.None, 0);
            // Top gutter
            spriteBatch.Draw(WindowManager.ScrollbarTexture, new Rectangle(offset.X + Position.X + Position.Width - ScrollbarSize, offset.Y + Position.Y + ScrollbarSize, thumbOffset, ScrollbarSize), new Rectangle(2 * ScrollbarSize, 0, ScrollbarSize, ScrollbarSize), Color.White, (float)Math.PI / 2, rotateOrigin, SpriteEffects.None, 0);
            // Thumb
            spriteBatch.Draw(WindowManager.ScrollbarTexture, new Rectangle(offset.X + Position.X + Position.Width - ScrollbarSize, offset.Y + Position.Y + ScrollbarSize + thumbOffset, ScrollbarSize, ScrollbarSize), new Rectangle(ScrollSize > 0 ? ScrollbarSize : 2 * ScrollbarSize, 0, ScrollbarSize, ScrollbarSize), Color.White, (float)Math.PI / 2, rotateOrigin, SpriteEffects.None, 0);
            // Bottom gutter
            spriteBatch.Draw(WindowManager.ScrollbarTexture, new Rectangle(offset.X + Position.X + Position.Width - ScrollbarSize, offset.Y + Position.Y + 2 * ScrollbarSize + thumbOffset, Position.Height - 3 * ScrollbarSize - thumbOffset, ScrollbarSize), new Rectangle(2 * ScrollbarSize, 0, ScrollbarSize, ScrollbarSize), Color.White, (float)Math.PI / 2, rotateOrigin, SpriteEffects.None, 0);
            // Bottom button
            spriteBatch.Draw(WindowManager.ScrollbarTexture, new Rectangle(offset.X + Position.X + Position.Width - ScrollbarSize, offset.Y + Position.Y + Position.Height - ScrollbarSize, ScrollbarSize, ScrollbarSize), new Rectangle(3 * ScrollbarSize, 0, ScrollbarSize, ScrollbarSize), Color.White, (float)Math.PI / 2, rotateOrigin, SpriteEffects.None, 0);

            // Draw contents inside a scissor rectangle (so they're clipped to the client area).
            WindowManager.Flush(spriteBatch);
            var oldScissorRectangle = spriteBatch.GraphicsDevice.ScissorRectangle;
            spriteBatch.GraphicsDevice.ScissorRectangle = new Rectangle(offset.X + Position.X, offset.Y + Position.Y, Position.Width - ScrollbarSize, Position.Height);
            spriteBatch.GraphicsDevice.RenderState.ScissorTestEnable = true;
            base.Draw(spriteBatch, offset);
            WindowManager.Flush(spriteBatch);
            spriteBatch.GraphicsDevice.ScissorRectangle = oldScissorRectangle;
            spriteBatch.GraphicsDevice.RenderState.ScissorTestEnable = false;
        }

		internal override bool HandleUserInput(WindowMouseEvent e)
		{
			if (UserInput.IsMouseLeftButtonDown())
			{
				Client.Position.Height = Client.CurrentTop;
				if (e.MouseDownPosition.X > Position.Right - ScrollbarSize)
				{
					var thumbOffset = (int)((float)(Position.Height - 3 * ScrollbarSize) * (float)ScrollPosition / (float)ScrollSize);

					// Mouse down occured within the scrollbar.
					if (e.MouseDownPosition.Y < Position.Top + ScrollbarSize)
					{
						// Mouse down occured on top button.
						var newScrollPosition = Math.Max(0, ScrollPosition - 10);
						Client.MoveBy(0, ScrollPosition - newScrollPosition);
						ScrollPosition = newScrollPosition;
					}
					else if (e.MouseDownPosition.Y < Position.Top + ScrollbarSize + thumbOffset)
					{
						// Mouse down occured on top gutter.
						var newScrollPosition = Math.Max(0, ScrollPosition - 100);
						Client.MoveBy(0, ScrollPosition - newScrollPosition);
						ScrollPosition = newScrollPosition;
					}
					else if (e.MouseDownPosition.Y > Position.Bottom - ScrollbarSize)
					{
						// Mouse down occured on bottom button.
                        var newScrollPosition = Math.Min(ScrollPosition + 10, Math.Max(0, ScrollSize));
						Client.MoveBy(0, ScrollPosition - newScrollPosition);
						ScrollPosition = newScrollPosition;
					}
					else if (e.MouseDownPosition.Y > Position.Top + 2 * ScrollbarSize + thumbOffset)
					{
						// Mouse down occured on bottom gutter.
                        var newScrollPosition = Math.Min(ScrollPosition + 100, Math.Max(0, ScrollSize));
						Client.MoveBy(0, ScrollPosition - newScrollPosition);
						ScrollPosition = newScrollPosition;
					}
					return true;
				}
			}
			return base.HandleUserInput(e);
		}

		public override int RemainingWidth
		{
			get
			{
				return base.RemainingWidth - ScrollbarSize;
			}
		}

		public override int ScrollSize
		{
			get
			{
				return Client.CurrentTop - Position.Height;
			}
		}
	}
}
