// COPYRIGHT 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

#define WINDOWTEXT_SPRITEBATCH

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Font = System.Drawing.Font;
using FontStyle = System.Drawing.FontStyle;
using GraphicsUnit = System.Drawing.GraphicsUnit;

namespace ORTS.Popups
{
    public sealed class WindowTextManager
    {
        // THREAD SAFETY:
        //   All accesses must be done in local variables. No modifications to the objects are allowed except by
        //   assignment of a new instance (possibly cloned and then modified).
        Dictionary<string, WindowTextFont> Fonts = new Dictionary<string, WindowTextFont>();

        /// <summary>
        /// Provides a <see cref="WindowTextFont"/> for the specified <paramref name="fontFamily"/>,
        /// <paramref name="sizeInPt"/> and <paramref name="style"/>.
        /// </summary>
        /// <remarks>
        /// All <see cref="WindowTextFont"/> instances are cached by the <see cref="WindowTextManager"/>.
        /// </remarks>
        /// <param name="fontFamily">Font family name (e.g. "Arial")</param>
        /// <param name="sizeInPt">Size of the font, in points (e.g. 9)</param>
        /// <param name="style">Style of the font (e.g. <c>FontStyle.Normal</c>)</param>
        /// <returns>A <see cref="WindowTextFont"/> that can be used to draw text of the given font family,
        /// size and style.</returns>
        public WindowTextFont Get(string fontFamily, float sizeInPt, FontStyle style)
        {
            return Get(fontFamily, sizeInPt, style, 0);
        }

        /// <summary>
        /// Provides a <see cref="WindowTextFont"/> for the specified <paramref name="fontFamily"/>,
        /// <paramref name="sizeInPt"/> and <paramref name="style"/> with the specified <paramref name="outlineSize"/>.
        /// </summary>
        /// <remarks>
        /// All <see cref="WindowTextFont"/> instances are cached by the <see cref="WindowTextManager"/>.
        /// </remarks>
        /// <param name="fontFamily">Font family name (e.g. "Arial")</param>
        /// <param name="sizeInPt">Size of the font, in points (e.g. 9)</param>
        /// <param name="style">Style of the font (e.g. <c>FontStyle.Normal</c>)</param>
        /// <param name="outlineSize">Size of the outline, in pixels (e.g. 2)</param>
        /// <returns>A <see cref="WindowTextFont"/> that can be used to draw text of the given font family,
        /// size and style with the given outline size.</returns>
        public WindowTextFont Get(string fontFamily, float sizeInPt, FontStyle style, int outlineSize)
        {
            var fonts = Fonts;
            var key = String.Format("{0}:{1:F}:{2}:{3}", fontFamily, sizeInPt, style, outlineSize);
            if (!fonts.ContainsKey(key))
            {
                fonts = new Dictionary<string, WindowTextFont>(fonts);
                fonts.Add(key, new WindowTextFont(fontFamily, sizeInPt, style, outlineSize));
                Fonts = fonts;
            }
            return fonts[key];
        }

        [CallOnThread("Loader")]
        public void Load(GraphicsDevice graphicsDevice)
        {
            var fonts = Fonts;
            foreach (var font in fonts.Values)
                font.Load(graphicsDevice);
        }
    }

    public sealed class WindowTextFont
    {
        readonly Font Font;
        readonly int FontHeight;
        readonly int OutlineSize;

        // THREAD SAFETY:
        //   All accesses must be done in local variables. No modifications to the objects are allowed except by
        //   assignment of a new instance (possibly cloned and then modified).
        CharacterGroup Characters;

        internal WindowTextFont(string fontFamily, float sizeInPt, FontStyle style, int outlineSize)
        {
            var font = new Font(fontFamily, sizeInPt, style);
            Font = new Font(fontFamily, (int)font.GetHeight(), style, GraphicsUnit.Pixel);
            Debug.Assert(Font.Height == (int)Math.Ceiling(Font.GetHeight()), "Font.Height is not expected value.");
            FontHeight = Font.Height;
            OutlineSize = outlineSize;
            Characters = new CharacterGroup(Font, OutlineSize);
        }

        /// <summary>
        /// Gets the line height of the font.
        /// </summary>
        public int Height
        {
            get
            {
                return FontHeight;
            }
        }

        /// <summary>
        /// Measures the width of a given string.
        /// </summary>
        /// <param name="text">Text to measure</param>
        /// <returns>The length of the text in pixels.</returns>
        public int MeasureString(string text)
        {
            EnsureCharacterData(text);
            var characters = Characters;

            var chIndexes = new int[text.Length];
            for (var i = 0; i < text.Length; i++)
                chIndexes[i] = characters.IndexOfCharacter(text[i]);

            var x = 0f;
            for (var i = 0; i < text.Length; i++)
            {
                x += characters.AbcWidths[chIndexes[i]].X;
                x += characters.AbcWidths[chIndexes[i]].Y;
                x += characters.AbcWidths[chIndexes[i]].Z;
            }
            return (int)x;
        }

#if WINDOWTEXT_SPRITEBATCH
        [CallOnThread("Render")]
        public void Draw(SpriteBatch spriteBatch, Point offset, string text, Color color)
        {
            Draw(spriteBatch, offset, 0, text, LabelAlignment.Left, color, Color.Black);
        }

        [CallOnThread("Render")]
        public void Draw(SpriteBatch spriteBatch, Point offset, string text, Color color, Color outline)
        {
            Draw(spriteBatch, offset, 0, text, LabelAlignment.Left, color, outline);
        }

        [CallOnThread("Render")]
        public void Draw(SpriteBatch spriteBatch, Rectangle position, Point offset, string text, LabelAlignment align, Color color)
        {
            Draw(spriteBatch, position, offset, text, align, color, Color.Black);
        }

        [CallOnThread("Render")]
        public void Draw(SpriteBatch spriteBatch, Rectangle position, Point offset, string text, LabelAlignment align, Color color, Color outline)
        {
            offset.X += position.Location.X;
            offset.Y += position.Location.Y;
            Draw(spriteBatch, offset, position.Width, text, align, color, outline);
        }

        [CallOnThread("Render")]
        void Draw(SpriteBatch spriteBatch, Point position, int width, string text, LabelAlignment align, Color color, Color outline)
        {
            EnsureCharacterData(text);
            var characters = Characters;

            var chIndexes = new int[text.Length];
            for (var i = 0; i < text.Length; i++)
                chIndexes[i] = characters.IndexOfCharacter(text[i]);

            var current = Vector2.Zero;
            if (align != LabelAlignment.Left)
            {
                for (var i = 0; i < text.Length; i++)
                {
                    current.X += characters.AbcWidths[chIndexes[i]].X;
                    current.X += characters.AbcWidths[chIndexes[i]].Y;
                    current.X += characters.AbcWidths[chIndexes[i]].Z;
                }
                if (align == LabelAlignment.Center)
                    current.X = (int)((width - current.X) / 2);
                else
                    current.X = width - current.X;
            }

            current.X += position.X - OutlineSize;
            current.Y += position.Y - OutlineSize;

            var start = current;
            var texture = characters.Texture;
            if (texture == null)
                return;

            if (OutlineSize > 0)
            {
                var outlineOffset = characters.BoxesMaxBottom;
                current = start;
                for (var i = 0; i < text.Length; i++)
                {
                    var box = characters.Boxes[chIndexes[i]];
                    box.Y += outlineOffset;
                    if (text[i] > ' ')
                        spriteBatch.Draw(texture, current, box, outline, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, 0);
                    current.X += characters.AbcWidths[chIndexes[i]].X;
                    current.X += characters.AbcWidths[chIndexes[i]].Y;
                    current.X += characters.AbcWidths[chIndexes[i]].Z;
                    if (text[i] == '\n')
                    {
                        current.X = start.X;
                        current.Y += Height;
                    }
                }
            }
            current = start;
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] > ' ')
                    spriteBatch.Draw(texture, current, characters.Boxes[chIndexes[i]], color, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, 0);
                current.X += characters.AbcWidths[chIndexes[i]].X;
                current.X += characters.AbcWidths[chIndexes[i]].Y;
                current.X += characters.AbcWidths[chIndexes[i]].Z;
                if (text[i] == '\n')
                {
                    current.X = start.X;
                    current.Y += Height;
                }
            }
        }
#else
        [CallOnThread("Updater")]
        public DrawData PrepareFrame(GraphicsDevice graphicsDevice, float width, float height, string text, LabelAlignment align)
        {
            // We'll crash creating 0-byte buffers below and there's nothing to be done with an empty string anyway.
            if (String.IsNullOrEmpty(text))
                return null;

            EnsureCharacterData(text);

            var chIndexes = new int[text.Length];
            for (var i = 0; i < text.Length; i++)
                chIndexes[i] = Characters.IndexOfCharacter(text[i]);

            var x = 0f;
            var y = 0f;
            if (align != LabelAlignment.Left)
            {
                for (var i = 0; i < text.Length; i++)
                {
                    x += Characters.AbcWidths[chIndexes[i]].X;
                    x += Characters.AbcWidths[chIndexes[i]].Y;
                    x += Characters.AbcWidths[chIndexes[i]].Z;
                }
                if (align == LabelAlignment.Center)
                    x = (width - x) / 2;
                else
                    x = width - x;
            }

            var vertexData = new VertexPositionTexture[text.Length * 4];
            var indexData = new short[text.Length * 6];
            for (var i = 0; i < text.Length; i++)
            {
                vertexData[i * 4 + 0] = new VertexPositionTexture(new Vector3(x + 0, y + 0, 0), new Vector2((float)Characters.Boxes[chIndexes[i]].Left / Characters.Texture.Width, (float)Characters.Boxes[chIndexes[i]].Top / Characters.Texture.Height));
                vertexData[i * 4 + 1] = new VertexPositionTexture(new Vector3(x + Characters.Boxes[chIndexes[i]].Width, y + 0, 0), new Vector2((float)Characters.Boxes[chIndexes[i]].Right / Characters.Texture.Width, (float)Characters.Boxes[chIndexes[i]].Top / Characters.Texture.Height));
                vertexData[i * 4 + 2] = new VertexPositionTexture(new Vector3(x + 0, y + Characters.Boxes[chIndexes[i]].Height, 0), new Vector2((float)Characters.Boxes[chIndexes[i]].Left / Characters.Texture.Width, (float)Characters.Boxes[chIndexes[i]].Bottom / Characters.Texture.Height));
                vertexData[i * 4 + 3] = new VertexPositionTexture(new Vector3(x + Characters.Boxes[chIndexes[i]].Width, y + Characters.Boxes[chIndexes[i]].Height, 0), new Vector2((float)Characters.Boxes[chIndexes[i]].Right / Characters.Texture.Width, (float)Characters.Boxes[chIndexes[i]].Bottom / Characters.Texture.Height));
                x += Characters.AbcWidths[chIndexes[i]].X;
                x += Characters.AbcWidths[chIndexes[i]].Y;
                x += Characters.AbcWidths[chIndexes[i]].Z;
                indexData[i * 6 + 0] = (short)(i * 4 + 0);
                indexData[i * 6 + 1] = (short)(i * 4 + 1);
                indexData[i * 6 + 2] = (short)(i * 4 + 2);
                indexData[i * 6 + 3] = (short)(i * 4 + 1);
                indexData[i * 6 + 4] = (short)(i * 4 + 3);
                indexData[i * 6 + 5] = (short)(i * 4 + 2);
            }
            var vertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionTexture), vertexData.Length, BufferUsage.WriteOnly);
            vertexBuffer.SetData(vertexData);
            var indexBuffer = new IndexBuffer(graphicsDevice, typeof(short), indexData.Length, BufferUsage.WriteOnly);
            indexBuffer.SetData(indexData);
            return new DrawData(Characters, vertexBuffer, vertexData.Length, indexBuffer, text.Length * 2);
        }

        [CallOnThread("Render")]
        public void Draw(GraphicsDevice graphicsDevice, DrawData data)
        {
            if (data != null)
            {
                graphicsDevice.VertexDeclaration = new VertexDeclaration(graphicsDevice, VertexPositionTexture.VertexElements);
                graphicsDevice.Vertices[0].SetSource(data.VertexBuffer, 0, VertexPositionTexture.SizeInBytes);
                graphicsDevice.Indices = data.IndexBuffer;
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, data.VertexCount, 0, data.PrimitiveCount);
            }
        }

        public bool IsValid(DrawData LastDrawData)
        {
            return (LastDrawData != null) && (LastDrawData.Characters == Characters);
        }
#endif

        [CallOnThread("Loader")]
        public void Load(GraphicsDevice graphicsDevice)
        {
            Characters.Load(graphicsDevice);
        }

        void EnsureCharacterData(string text)
        {
            var characters = Characters;

            foreach (var ch in text)
            {
                if (!characters.ContainsCharacter(ch))
                {
                    Characters = new CharacterGroup(text.ToCharArray(), characters);
                    break;
                }
            }
        }

#if !WINDOWTEXT_SPRITEBATCH
        public sealed class DrawData
        {
            internal readonly CharacterGroup Characters;
            internal readonly VertexBuffer VertexBuffer;
            internal readonly int VertexCount;
            internal readonly IndexBuffer IndexBuffer;
            internal readonly int PrimitiveCount;
            internal DrawData(CharacterGroup characters, VertexBuffer vertexBuffer, int vertexCount, IndexBuffer indexBuffer, int primitiveCount)
            {
                Characters = characters;
                VertexBuffer = vertexBuffer;
                VertexCount = vertexCount;
                IndexBuffer = indexBuffer;
                PrimitiveCount = primitiveCount;
            }
        }
#endif

        sealed internal class CharacterGroup
        {
            const int BoxSpacing = 1;
            const System.Windows.Forms.TextFormatFlags Flags = System.Windows.Forms.TextFormatFlags.NoPadding | System.Windows.Forms.TextFormatFlags.NoPrefix | System.Windows.Forms.TextFormatFlags.SingleLine | System.Windows.Forms.TextFormatFlags.Top;

            readonly Font Font;
            readonly int OutlineSize;
            readonly char[] Characters;
            public readonly Rectangle[] Boxes;
            public readonly int BoxesMaxRight;
            public readonly int BoxesMaxBottom;
            public readonly Vector3[] AbcWidths;
            public Texture2D Texture { get; private set; }

            public CharacterGroup(Font font, int outlineSize)
            {
                Font = font;
                OutlineSize = outlineSize;
                Characters = new char[0];
                Boxes = new Rectangle[0];
                BoxesMaxRight = 0;
                BoxesMaxBottom = 0;
                AbcWidths = new Vector3[0];
            }

            public CharacterGroup(char[] characters, CharacterGroup mergeGroup)
                : this(characters, mergeGroup.Font, mergeGroup.OutlineSize, mergeGroup.Characters, mergeGroup.Boxes, mergeGroup.AbcWidths)
            {
            }

            CharacterGroup(char[] characters, Font mergeFont, int mergeOutlineSize, char[] mergeCharacters, Rectangle[] mergeBoxes, Vector3[] mergeAbcWidths)
            {
                Font = mergeFont;
                OutlineSize = mergeOutlineSize;
                Characters = characters.Union(mergeCharacters).OrderBy(c => c).ToArray();
                Boxes = new Rectangle[Characters.Length];
                AbcWidths = new Vector3[Characters.Length];

                // Boring device context for APIs.
                var hdc = NativeMethods.CreateCompatibleDC(IntPtr.Zero);
                NativeMethods.SelectObject(hdc, Font.ToHfont());
                try
                {
                    // Get character glyph indices to identify those not supported by this font.
                    var charactersGlyphs = new short[Characters.Length];
                    if (NativeMethods.GetGlyphIndices(hdc, new String(Characters), Characters.Length, charactersGlyphs, NativeMethods.GgiFlags.MarkNonexistingGlyphs) != Characters.Length) throw new Exception();

                    var mergeIndex = 0;
                    var spacing = BoxSpacing + OutlineSize;
                    var x = spacing;
                    var y = spacing;
                    var height = (int)Math.Ceiling(Font.GetHeight()) + 1;
                    for (var i = 0; i < Characters.Length; i++)
                    {
                        // Copy ABC widths from merge data or calculate ourselves.
                        if ((mergeIndex < mergeCharacters.Length) && (mergeCharacters[mergeIndex] == Characters[i]))
                        {
                            AbcWidths[i] = mergeAbcWidths[mergeIndex];
                            mergeIndex++;
                        }
                        else if (charactersGlyphs[i] != -1)
                        {
                            NativeStructs.AbcFloatWidth characterAbcWidth;
                            if (!NativeMethods.GetCharABCWidthsFloat(hdc, Characters[i], Characters[i], out characterAbcWidth)) throw new Exception();
                            AbcWidths[i] = new Vector3(characterAbcWidth.A, characterAbcWidth.B, characterAbcWidth.C);
                        }
                        else
                        {
                            // This is a bit of a cheat, but is used when the chosen font does not have the character itself but it will render anyway (e.g. through font fallback).
                            AbcWidths[i] = new Vector3(0, System.Windows.Forms.TextRenderer.MeasureText(String.Format(" {0} ", Characters[i]), Font, System.Drawing.Size.Empty, Flags).Width - System.Windows.Forms.TextRenderer.MeasureText("  ", Font, System.Drawing.Size.Empty, Flags).Width, 0);
                        }
                        Boxes[i] = new Rectangle(x, y, (int)(Math.Max(0, AbcWidths[i].X) + AbcWidths[i].Y + Math.Max(0, AbcWidths[i].Z) + 2 * OutlineSize), height + 2 * OutlineSize);
                        x += Boxes[i].Width + BoxSpacing;
                        if (x >= 256)
                        {
                            x = BoxSpacing;
                            y += Boxes[i].Height + BoxSpacing;
                        }
                    }

                    // TODO: Copy boxes from the merge data.
                }
                finally
                {
                    // Cleanup.
                    NativeMethods.DeleteDC(hdc);
                }
                BoxesMaxRight = Boxes.Max(b => b.Right);
                BoxesMaxBottom = Boxes.Max(b => b.Bottom);
            }

            public bool ContainsCharacter(char character)
            {
                return Array.BinarySearch(Characters, character) >= 0;
            }

            public int IndexOfCharacter(char character)
            {
                return Array.BinarySearch(Characters, character);
            }

            byte[] GetBitmapData(System.Drawing.Rectangle rectangle)
            {
                var bitmap = new System.Drawing.Bitmap(rectangle.Width, rectangle.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                var buffer = new byte[4 * rectangle.Width * rectangle.Height];
                using (var g = System.Drawing.Graphics.FromImage(bitmap))
                {
                    // Clear to black.
                    g.FillRectangle(new System.Drawing.SolidBrush(System.Drawing.Color.Black), rectangle);

                    // Draw the text using system text drawing (yay, ClearType).
                    for (var i = 0; i < Characters.Length; i++)
                        System.Windows.Forms.TextRenderer.DrawText(g, Characters[i].ToString(), Font, new System.Drawing.Point(Boxes[i].X + OutlineSize, Boxes[i].Y + OutlineSize), System.Drawing.Color.White, Flags);
                }
                var bits = bitmap.LockBits(rectangle, System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
                Marshal.Copy(bits.Scan0, buffer, 0, buffer.Length);
                bitmap.UnlockBits(bits);
                return buffer;
            }

            [CallOnThread("Loader")]
            public void Load(GraphicsDevice graphicsDevice)
            {
                if (Texture != null || Characters.Length == 0)
                    return;

                var rectangle = new System.Drawing.Rectangle(0, 0, BoxesMaxRight, BoxesMaxBottom);
                var buffer = GetBitmapData(rectangle);

                for (var y = 0; y < rectangle.Height; y++)
                {
                    for (var x = 0; x < rectangle.Width; x++)
                    {
                        var offset = y * rectangle.Width * 4 + x * 4;
                        // alpha = (red + green + blue) / 3.
                        buffer[offset + 3] = (byte)((buffer[offset + 0] + buffer[offset + 1] + buffer[offset + 2]) / 3);
                        // red|green|blue = Color.White;
                        buffer[offset + 2] = 255;
                        buffer[offset + 1] = 255;
                        buffer[offset + 0] = 255;
                    }
                }

                if (OutlineSize > 0)
                {
                    var outlineBuffer = new byte[buffer.Length];
                    Array.Copy(buffer, outlineBuffer, buffer.Length);
                    for (var offsetX = -OutlineSize; offsetX <= OutlineSize; offsetX++)
                    {
                        for (var offsetY = -OutlineSize; offsetY <= OutlineSize; offsetY++)
                        {
                            if (Math.Sqrt(offsetX * offsetX + offsetY * offsetY) <= OutlineSize)
                            {
                                for (var x = OutlineSize; x < rectangle.Width - OutlineSize; x++)
                                {
                                    for (var y = OutlineSize; y < rectangle.Height - OutlineSize; y++)
                                    {
                                        var offset = y * rectangle.Width * 4 + x * 4;
                                        var outlineOffset = offset + offsetY * rectangle.Width * 4 + offsetX * 4;
                                        outlineBuffer[outlineOffset + 3] = (byte)Math.Min(255, outlineBuffer[outlineOffset + 3] + buffer[offset + 3]);
                                    }
                                }
                            }
                        }
                    }
                    var combinedBuffer = new byte[buffer.Length * 2];
                    Array.Copy(buffer, 0, combinedBuffer, 0, buffer.Length);
                    Array.Copy(outlineBuffer, 0, combinedBuffer, buffer.Length, buffer.Length);
                    buffer = combinedBuffer;
                    rectangle.Height *= 2;
                }

                var texture = new Texture2D(graphicsDevice, rectangle.Width, rectangle.Height, 1, TextureUsage.None, SurfaceFormat.Color); // Color = 32bppRgb
                texture.SetData(buffer);
                Texture = texture;
            }
        }
    }

    class NativeStructs
    {
        [DebuggerDisplay("{First} + {Second} = {Amount}")]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct KerningPair
        {
            public char First;
            public char Second;
            public int Amount;
        }

        [DebuggerDisplay("{A} + {B} + {C}")]
        [StructLayout(LayoutKind.Sequential)]
        public struct AbcFloatWidth
        {
            public float A;
            public float B;
            public float C;
        }
    }

    class NativeMethods
    {
        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hObject);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern bool DeleteDC(IntPtr hdc);

        [Flags]
        public enum GgiFlags : uint
        {
            None = 0,
            MarkNonexistingGlyphs = 1,
        }

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern uint GetGlyphIndices(IntPtr hdc, string text, int textLength, [Out] short[] indices, GgiFlags flags);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern uint GetKerningPairs(IntPtr hdc, int kerningPairsLength, [Out] NativeStructs.KerningPair[] kerningPairs);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern uint GetKerningPairs(IntPtr hdc, int kerningPairsLength, IntPtr kerningPairs);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool GetCharABCWidthsFloat(IntPtr hdc, char firstChar, char lastChar, out NativeStructs.AbcFloatWidth abcFloatWidths);
    }
}
