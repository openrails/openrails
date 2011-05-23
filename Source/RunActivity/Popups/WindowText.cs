// COPYRIGHT 2011 by the Open Rails project.
// This code is provided to enable you to contribute improvements to the open rails program.  
// Use of the code for any other purpose or distribution of the code to anyone else
// is prohibited without specific written permission from admin@openrails.org.

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
        Dictionary<string, WindowTextFont> Fonts = new Dictionary<string, WindowTextFont>();

        [CallOnThread("Loader")]
        public WindowTextFont Get(string fontFamily, float sizeInPt, FontStyle style)
        {
            var key = String.Format("{0}{1:F}{2}", fontFamily, sizeInPt, style);
            WindowTextFont font;
            if (Fonts.TryGetValue(key, out font))
                return font;
            return Fonts[key] = font = new WindowTextFont(fontFamily, sizeInPt, style);
        }
    }

    public sealed class WindowTextFont
    {
        readonly Font Font;
        CharacterGroup Characters;

        [CallOnThread("Loader")]
        public WindowTextFont(string fontFamily, float sizeInPt, FontStyle style)
        {
            var font = new Font(fontFamily, sizeInPt, style);
            Font = new Font(fontFamily, (int)font.GetHeight(), style, GraphicsUnit.Pixel);
            Characters = new CharacterGroup();
        }

        public Texture2D Texture
        {
            get
            {
                return Characters.Texture;
            }
        }

        public int Height
        {
            get
            {
                if (Characters.Characters.Length > 0)
                    return Characters.Boxes[0].Height;

                return (int)Math.Ceiling(Font.GetHeight());
            }
        }

        public int MeasureString(GraphicsDevice graphicsDevice, string text)
        {
            // We'll crash creating 0-byte buffers below and there's nothing to be done with an empty string anyway.
            if (text.Length == 0)
                return 0;

            foreach (var ch in text)
            {
                if (!Characters.ContainsCharacter(ch))
                {
                    Characters = new CharacterGroup(graphicsDevice, Font, text.ToCharArray(), Characters);
                    break;
                }
            }

            var chIndexes = new int[text.Length];
            for (var i = 0; i < text.Length; i++)
                chIndexes[i] = Characters.IndexOfCharacter(text[i]);

            var x = 0f;
            for (var i = 0; i < text.Length; i++)
            {
                x += Characters.AbcWidths[chIndexes[i]].X;
                x += Characters.AbcWidths[chIndexes[i]].Y;
                x += Characters.AbcWidths[chIndexes[i]].Z;
            }
            return (int)x;
        }

#if WINDOWTEXT_SPRITEBATCH
        public void Draw(SpriteBatch spriteBatch, Rectangle position, Point offset, string text, LabelAlignment align, Color color)
        {
            // We'll crash creating 0-byte buffers below and there's nothing to be done with an empty string anyway.
            if (text.Length == 0)
                return;

            foreach (var ch in text)
            {
                if (!Characters.ContainsCharacter(ch))
                {
                    Characters = new CharacterGroup(spriteBatch.GraphicsDevice, Font, text.ToCharArray(), Characters);
                    break;
                }
            }

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
                    x = (int)((position.Width - x) / 2);
                else
                    x = position.Width - x;
            }

            x += position.X + offset.X;
            y += position.Y + offset.Y;

            color = Color.Lerp(Color.Black, color, (float)color.A / 255);
            WindowManager.Flush(spriteBatch);
            spriteBatch.GraphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceColor;
            for (var i = 0; i < text.Length; i++)
            {
                spriteBatch.Draw(Characters.Texture, new Vector2(x, y), Characters.Boxes[chIndexes[i]], color, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
                x += Characters.AbcWidths[chIndexes[i]].X;
                x += Characters.AbcWidths[chIndexes[i]].Y;
                x += Characters.AbcWidths[chIndexes[i]].Z;
            }
            WindowManager.Flush(spriteBatch);
            spriteBatch.GraphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
        }
#else
        [CallOnThread("Updater")]
        public DrawData PrepareFrame(GraphicsDevice graphicsDevice, float width, float height, string text, LabelAlignment align)
        {
            // We'll crash creating 0-byte buffers below and there's nothing to be done with an empty string anyway.
            if (text.Length == 0)
                return null;

            foreach (var ch in text)
                if (!Characters.ContainsCharacter(ch))
                    Characters = new CharacterGroup(graphicsDevice, Font, (char)(ch - (ch % 32)), (char)(ch - (ch % 32) + 31), Characters);

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

            public readonly Texture2D Texture;
            public readonly char[] Characters;
            public readonly Rectangle[] Boxes;
            public readonly Vector3[] AbcWidths;

            public CharacterGroup()
            {
                Characters = new char[0];
                Boxes = new Rectangle[0];
                AbcWidths = new Vector3[0];
            }

            public CharacterGroup(GraphicsDevice graphicsDevice, Font font, char[] characters, CharacterGroup mergeGroup)
                : this(graphicsDevice, font, characters, mergeGroup.Characters, mergeGroup.Boxes, mergeGroup.AbcWidths)
            {
            }

            CharacterGroup(GraphicsDevice graphicsDevice, Font font, char[] characters, char[] mergeCharacters, Rectangle[] mergeBoxes, Vector3[] mergeAbcWidths)
            {
                Characters = characters.Union(mergeCharacters).OrderBy(c => c).ToArray();
                Boxes = new Rectangle[Characters.Length];
                AbcWidths = new Vector3[Characters.Length];

                // Set up flags for drawing text.
                var flags = System.Windows.Forms.TextFormatFlags.NoPadding | System.Windows.Forms.TextFormatFlags.NoPrefix | System.Windows.Forms.TextFormatFlags.SingleLine | System.Windows.Forms.TextFormatFlags.Top;

                // Boring device context for APIs.
                var hdc = NativeMethods.CreateCompatibleDC(IntPtr.Zero);
                NativeMethods.SelectObject(hdc, font.ToHfont());
                try
                {
                    // Get character glyph indices to identify those not supported by this font.
                    var charactersGlyphs = new short[Characters.Length];
                    if (NativeMethods.GetGlyphIndices(hdc, new String(Characters), Characters.Length, charactersGlyphs, NativeMethods.GgiFlags.MarkNonexistingGlyphs) != Characters.Length) throw new Exception();

                    // Copy what data we can from the merge CharacterGroup data. Load anything we don't have.
                    var mergeIndex = 0;
                    var x = BoxSpacing;
                    var y = BoxSpacing;
                    var height = (int)Math.Ceiling(font.GetHeight()) + 1;
                    for (var i = 0; i < Characters.Length; i++)
                    {
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
                            AbcWidths[i] = new Vector3(0, System.Windows.Forms.TextRenderer.MeasureText(String.Format(" {0} ", Characters[i]), font, System.Drawing.Size.Empty, flags).Width - System.Windows.Forms.TextRenderer.MeasureText("  ", font, System.Drawing.Size.Empty, flags).Width, 0);
                        }
                        Boxes[i] = new Rectangle(x, y, (int)(Math.Max(0, AbcWidths[i].X) + AbcWidths[i].Y + Math.Max(0, AbcWidths[i].Z)), height);
                        x += Boxes[i].Width + BoxSpacing;
                        if (x >= 256)
                        {
                            x = BoxSpacing;
                            y += height + BoxSpacing;
                        }
                    }
                }
                finally
                {
                    // Cleanup.
                    NativeMethods.DeleteDC(hdc);
                }

                // TODO: Copy texture/bitmap and boxes data from merge source.
                var rectangle = new System.Drawing.Rectangle(0, 0, Boxes.Max(r => r.Right), Boxes.Max(r => r.Bottom));
                var bitmap = new System.Drawing.Bitmap(rectangle.Width, rectangle.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                var buffer = new int[rectangle.Width * rectangle.Height];
                using (var g = System.Drawing.Graphics.FromImage(bitmap))
                {
                    // Clear to black.
                    g.FillRectangle(new System.Drawing.SolidBrush(System.Drawing.Color.Black), rectangle);

                    // Draw the text using system text drawing (yay, ClearType).
                    for (var i = 0; i < Characters.Length; i++)
                        System.Windows.Forms.TextRenderer.DrawText(g, Characters[i].ToString(), font, new System.Drawing.Point(Boxes[i].X, Boxes[i].Y), System.Drawing.Color.White, flags);
                }
                var bits = bitmap.LockBits(rectangle, System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
                System.Runtime.InteropServices.Marshal.Copy(bits.Scan0, buffer, 0, buffer.Length);
                Texture = new Texture2D(graphicsDevice, rectangle.Width, rectangle.Height, 1, TextureUsage.None, SurfaceFormat.Color); // Color = 32bppRgb
                Texture.SetData(buffer);
                bitmap.UnlockBits(bits);
            }

            public bool ContainsCharacter(char character)
            {
                return Array.BinarySearch(Characters, character) >= 0;
            }

            public int IndexOfCharacter(char character)
            {
                return Array.BinarySearch(Characters, character);
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
