/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

/// Autor Laurie Heath

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using SD = System.Drawing;
using SDI = System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ORTS
{
    public class PopupWindows
    {
        private List<PopupWindow> WindwsList = new List<PopupWindow>();
        private PopupWindow selectedWindow = null;

        public void Draw(GraphicsDevice device)
        {
            if (WindwsList.Count > 0)
            {
                SpriteBatch spritebatch = new SpriteBatch(device);
                spritebatch.Begin(SpriteBlendMode.None, SpriteSortMode.Deferred, SaveStateMode.SaveState);
                foreach (PopupWindow window in WindwsList)
                {
                    window.Draw(spritebatch);
                }
                spritebatch.End();
            }
        }

        // 
        //  Adds a window to the list to be rendered.
        //
        public void Add(PopupWindow window)
        {
            WindwsList.Add(window);
        }

        //
        //  Selects the window if mouse is clicked within its bounds.
        //
        public void SelectWindow(int x, int y)
        {
            foreach (PopupWindow window in WindwsList)
            {
                if (window.visible)
                {
                    if ((x >= window.X) && (x < (window.X + window.Width)))
                    {
                        if ((y >= window.Y) && (y < (window.Y + window.Height)))
                        {
                            if (!window.isCloseClicked(x, y))
                            {
                                window.Selected = true;
                                selectedWindow = window;
                            }
                            return;
                        }
                    }
                }
            }
        }

        public void DelselectWindow()
        {
            if (selectedWindow != null) selectedWindow.Selected = false;
        }

        //
        //  Move the selected window by specified amount
        //
        public void MoveWindow(int dx, int dy)
        {
            if (selectedWindow != null)
            {
                if (selectedWindow.Selected) selectedWindow.MoveBy(dx, dy);
            }
        }

        //
        //  Adds an arbitory message box
        //
        public void PopupMessage(string text,GraphicsDevice device,SpriteFont f)
        {
            PopupMessage popMsgbox;

            popMsgbox = new PopupMessage(device, text, f, 5.0);
           // Add(popMsgbox);
        }

        //
        //  Returns true if at least one popup window is visible.
        //
        public bool isVisble()
        {
            foreach (PopupWindow popup in WindwsList)
            {
                if (popup.visible) return true;
            }
            return false;
        }
    }

    public class PopupWindow
    {
        public static GraphicsDevice device;
        private Texture2D backgroundTexture;
        private Texture2D closeTexture;
        private Color[] backgroundColours;
        private bool isVisible = false;
        private bool isDown = false;
        private bool isGraphics = false;
        private bool isSelected = false;
        private SD.Graphics GR;
        private SD.Bitmap bmpBackground;
        private List<TextBox> textBoxes = new List<TextBox>();  // List of text fields

        private int spriteX, spriteY, spriteW, spriteH;   //  coordinates relative to main display.

        public PopupWindow( int x, int y, int width, int height)
        {
            spriteX = x;
            spriteY = y;
            spriteH = height;
            spriteW = width;

            InitSprite(width, height);
        }

        public PopupWindow()
        {
        }

        protected void InitSprite(int width, int height)
        {
            backgroundColours = new Color[width * height];
            backgroundTexture = new Texture2D(device, width, height, 1, TextureUsage.None, SurfaceFormat.Color);

            for (int i = 0; i < backgroundColours.Length; i++)
            {
                backgroundColours[i] = Color.TransparentBlack;
            }
            backgroundTexture.SetData(backgroundColours);
            CreateCloseIcon(device);
        }

        //
        //      Creates a close icon for the window in the top right hand corner
        //
        public void CreateCloseIcon(GraphicsDevice device)
        {
            int w = 12;
            int h = 12;
            Color[] data = new Color[w * h];
            int[,] icondata = new int[,]
            {
                 {1,1,1,1,1,1,1,1,1,1,1,1},
                 {1,0,0,0,0,0,0,0,0,0,0,1},
                 {1,0,2,0,0,0,0,0,0,2,0,1},
                 {1,0,0,2,0,0,0,0,2,0,0,1},
                 {1,0,0,0,2,0,0,2,0,0,0,1},
                 {1,0,0,0,0,2,2,0,0,0,0,1},
                 {1,0,0,0,0,2,2,0,0,0,0,1},
                 {1,0,0,0,2,0,0,2,0,0,0,1},
                 {1,0,0,2,0,0,0,0,2,0,0,1},
                 {1,0,2,0,0,0,0,0,0,2,0,1},
                 {1,0,0,0,0,0,0,0,0,0,0,1},
                 {1,1,1,1,1,1,1,1,1,1,1,1},
            };

            int i = 0;
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    switch (icondata[x, y])
                    {
                        case 1:
                            data[i] = Color.Gray;
                            break;
                        case 2:
                            data[i] = Color.Red;
                            break;

                        default:
                            data[i] = Color.TransparentBlack;
                            break;
                    }
                    i++;
                }
            }

            closeTexture = new Texture2D(device, w, h, 1, TextureUsage.None, SurfaceFormat.Color);
            closeTexture.SetData(data);

        }

        //
        //  This method is invoked if bitmap is used to display the information
        //
        private void SetupGrahics()
        {
            bmpBackground = new SD.Bitmap(spriteW, spriteH);
            GR = SD.Graphics.FromImage(bmpBackground);
            GR.Clear(SD.Color.FromArgb(0, 0, 0, 0));
        }

        //
        //  This method copies the bitmap to the texture
        //  Indebted to Florian Block for this code snippet.
        //
        public void UpdateGraphics()
        {
            SDI.BitmapData bmpData = bmpBackground.LockBits(new System.Drawing.Rectangle(0, 0, bmpBackground.Width, bmpBackground.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bmpBackground.PixelFormat);
            int bufferSize = bmpData.Height * bmpData.Stride;
            byte[] texBytes = new byte[bufferSize];
            Marshal.Copy(bmpData.Scan0, texBytes, 0, texBytes.Length);
            backgroundTexture.SetData<Byte>(texBytes);
            bmpBackground.UnlockBits(bmpData);
        }

        public void UseGraphics()
        {
            SetupGrahics();
            UpdateGraphics();
            isGraphics = true;
        }

        //
        //  Make winow invisible if close icon clicked
        //
        public bool isCloseClicked(int x, int y)
        {
            if ((x > (spriteX + spriteW - closeTexture.Width)) && (x < (spriteX + spriteW)))
            {
                if ((y > spriteY) && (y < (spriteY + closeTexture.Height)))
                {
                    isVisible = false;
                    return true;
                }
            }
            return false;
        }

        public void Draw(SpriteBatch spritebatch)
        {
            if (isVisible)
            {
                Rectangle rect = new Rectangle(spriteX, spriteY, spriteW, spriteH);
                spritebatch.Draw(backgroundTexture, rect, Color.White);
                spritebatch.Draw(closeTexture, new Rectangle(spriteX + spriteW - closeTexture.Width, spriteY, closeTexture.Width, closeTexture.Height), Color.White);
                if (textBoxes.Count > 0)
                {
                    foreach (TextBox tb in textBoxes)
                    {
                        tb.Draw(spritebatch, spriteX, spriteY);
                    }
                }
            }
        }

        //
        //  Move widow by specified ampount (within bounds of screen,)
        //
        public void MoveBy(int dx, int dy)
        {
            if(spriteX+dx >=0 && (spriteX +dx + spriteW) <= device.Viewport.Width) spriteX += dx;
            if (spriteY+dy >= 0 && (spriteY +dy + spriteH) <= device.Viewport.Height) spriteY += dy;
        }

        public void AddTextbox(TextBox tb)
        {
            textBoxes.Add(tb);
        }

        //
        //      Sets the background clour for the window
        //
        public Color backgroundColour
        {
            set
            {
                Color[] colData = new Color[spriteW * spriteH];
                backgroundTexture.GetData<Color>(colData);
                for (int i = 0; i < colData.Length; i++)
                {
                    colData[i] = value;
                }
                backgroundTexture.SetData<Color>(colData);
            }
        }

        //
        //  A selection of allignment methods
        //
        public void AlignTop()
        {
            spriteY = 0;
        }

        public void AlignBottom()
        {
            spriteY = device.Viewport.Height - spriteH;
        }

        public void AlignLeft()
        {
            spriteX = 0;
        }

        public void AlignRignt()
        {
            spriteX = device.Viewport.Width - spriteW;
        }

        // Centre vertically
        public void Vcentre()
        {
            spriteY = (device.Viewport.Height - spriteH) / 2;
        }

        // Center Horizontally
        public void Hcentre()
        {
            spriteX=(device.Viewport.Width-spriteW)/2;
        }

        public void Centre()
        {
            Vcentre();
            Hcentre();
        }

        public bool visible
        {
            get
            {
                return isVisible;
            }
            set
            {
                isVisible = value;
            }
        }

        public SD.Graphics puGraphics
        {
            get
            {
                return GR;
            }
        }

        public int X
        {
            get
            {
                return spriteX;
            }
            set
            {
                spriteX = value;
            }
        }

        public int Y
        {
            get
            {
                return spriteY;
            }
            set
            {
                spriteY = value;
            }
        }

        public int Width
        {
            get
            {
                return spriteW;
            }
            set
            {
                spriteW = value;
            }
        }
        public int Height
        {
            get
            {
                return spriteH;
            }
            set
            {
                spriteH = value;
            }
        }

        public bool Selected
        {
            get
            {
                return isSelected;
            }
            set
            {
                isSelected = value;
            }
        }
    }

    //
    //  Creates a text field within a window 
    //
    public class TextBox
    {
        SpriteFont font;
        int spriteX, spriteY;         // Coordinates relative to the main window
        String spriteText = "";
        Color textColour = Color.White;

        public TextBox(int x, int y, SpriteFont f)
        {
            font = f;
            spriteX = x;
            spriteY = y;
        }

        //
        //  Renders the text field (ox & oy are coordinates of main window.
        //
        public void Draw(SpriteBatch spritebatch, int ox, int oy)
        {
            spritebatch.DrawString(font, spriteText, new Vector2((float)(ox + spriteX), (float)(oy + spriteY)), textColour);
        }

        public string text
        {
            get
            {
                return spriteText;
            }
            set
            {
                spriteText = value;
            }
        }
    }

    //
    //  Creates a message boz (not complete)
    //
    public class PopupMessage: PopupWindow
    {
        TextBox tbText;

        public PopupMessage(GraphicsDevice device, string text, SpriteFont f, double displayTime)
            : base()
        {
            this.Width = text.Length * 10 + 50;
            this.Height = 150;
            this.Centre();
            tbText = new TextBox(25, 100, f);
            tbText.text = text;
            this.InitSprite( this.Width, this.Height);
            this.AddTextbox(tbText);           
        }
    }
}
