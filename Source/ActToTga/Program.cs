using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Formats.Msts;

namespace ActToTga
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string path;
            if (args.Length == 0)
            {
                Console.WriteLine("No arguments given. Put absolute path to ace file:");
                path = Console.ReadLine();
                Convert(path);
            }
            else if (args.Length == 1)
            {
                path = args[0];
                Convert(path);
            }
            else
            {
                foreach (string arg in args)
                {
                    Convert(arg);
                }
            }
        }
        static void Convert(string path)
        {
            Console.WriteLine("Initializing");
            // Details probably do not matter too much
            PresentationParameters parameters = new PresentationParameters()
            {
                BackBufferWidth = 100,
                BackBufferHeight = 100,
                BackBufferFormat = SurfaceFormat.Color,
                //DepthStencilFormat = DepthFormat.Depth24,
                PresentationInterval = PresentInterval.Immediate,
                IsFullScreen = false,
            };

            var graphicsDevice = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.HiDef, parameters);

            Console.WriteLine("Reading ace file");
            Texture2D af = AceFile.Texture2DFromFile(graphicsDevice, path);

            Console.WriteLine("Performing conversion");
            int width = af.Width;
            int height = af.Height;
            Color[] pixelData = new Color[width * height];
            af.GetData(pixelData);

            string exportPath = Path.ChangeExtension(path, "tga");
            Console.WriteLine("Saving TGA to: {0}", exportPath);
            // Create a TGA header (18 bytes)
            byte[] header = new byte[18];
            header[2] = 2; // Image type: Uncompressed True-color image
            header[12] = (byte)(width & 0xFF); // Width (low byte)
            header[13] = (byte)((width >> 8) & 0xFF); // Width (high byte)
            header[14] = (byte)(height & 0xFF); // Height (low byte)
            header[15] = (byte)((height >> 8) & 0xFF); // Height (high byte)
            header[16] = 32; // Bits per pixel
            header[17] = 0x20; // Image descriptor: top-left origin

            byte[] imageData = new byte[width * height * 4];
            for (int i = 0; i < pixelData.Length; i++)
            {
                Color color = pixelData[i];
                int index = i * 4;
                imageData[index] = color.B;
                imageData[index + 1] = color.G;
                imageData[index + 2] = color.R;
                imageData[index + 3] = color.A;
            }

            // Write the TGA file
            using (FileStream fileStream = new FileStream(exportPath, FileMode.Create, FileAccess.Write))
            {
                // Write the header
                fileStream.Write(header, 0, header.Length);

                // Write the pixel data
                fileStream.Write(imageData, 0, imageData.Length);
            }
        }
    }
}
