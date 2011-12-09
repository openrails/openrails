/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MSTS;

namespace ORTS
{
    public static class Helpers
    {
        [Flags]
        public enum TextureFlags
        {
            None = 0x0,
            Snow = 0x1,
            SnowTrack = 0x2,
            Spring = 0x4,
            Autumn = 0x8,
            Winter = 0x10,
            SpringSnow = 0x20,
            AutumnSnow = 0x40,
            WinterSnow = 0x80,
            Night = 0x100,
            // SPECIAL VALUE FOR OPEN RAILS!
            TrainSet = 0x10000,
        }

        public static string GetForestTextureFile(Simulator simulator, string textureName)
        {
            return GetTextureFile(simulator, Helpers.TextureFlags.Spring | Helpers.TextureFlags.Autumn | Helpers.TextureFlags.Winter | Helpers.TextureFlags.SpringSnow | Helpers.TextureFlags.AutumnSnow | Helpers.TextureFlags.WinterSnow, simulator.RoutePath + @"\Textures", textureName);
        }

        public static string GetNightTextureFile(Simulator simulator, string textureFilePath)
        {
            var texturePath = Path.GetDirectoryName(textureFilePath);
            var textureName = Path.GetFileName(textureFilePath);
            if (File.Exists(texturePath + @"\Night\" + textureName)) return texturePath + @"\Night\" + textureName;
            texturePath = Path.GetDirectoryName(texturePath);
            if (File.Exists(texturePath + @"\Night\" + textureName)) return texturePath + @"\Night\" + textureName;
            return null;
        }

        public static string GetRouteTextureFile(Simulator simulator, TextureFlags textureFlags, string textureName)
        {
            return GetTextureFile(simulator, textureFlags, simulator.RoutePath + @"\Textures", textureName);
        }

        public static string GetShapeTextureFile(Simulator simulator, TextureFlags textureFlags, string shapeFile, string textureName)
        {
            if ((textureFlags & TextureFlags.TrainSet) != 0)
                return GetTextureFile(simulator, textureFlags, Path.GetDirectoryName(shapeFile), textureName);
            return GetTextureFile(simulator, textureFlags, simulator.RoutePath + @"\Textures", textureName);
        }

        public static string GetTerrainTextureFile(Simulator simulator, string textureName)
        {
            return GetTextureFile(simulator, Helpers.TextureFlags.Snow, simulator.RoutePath + @"\TerrTex", textureName);
        }

        static string GetTextureFile(Simulator simulator, TextureFlags textureFlags, string texturePath, string textureName)
        {
            var alternativePath = "";
            if ((textureFlags & TextureFlags.Snow) != 0 || (textureFlags & TextureFlags.SnowTrack) != 0)
                if (IsSnow(simulator))
                    alternativePath = @"\Snow\";
                else
                    alternativePath = "";
            else if ((textureFlags & TextureFlags.Spring) != 0 && simulator.Season == SeasonType.Spring && simulator.Weather != WeatherType.Snow)
                alternativePath = @"\Spring\";
            else if ((textureFlags & TextureFlags.Autumn) != 0 && simulator.Season == SeasonType.Autumn && simulator.Weather != WeatherType.Snow)
                alternativePath = @"\Autumn\";
            else if ((textureFlags & TextureFlags.Winter) != 0 && simulator.Season == SeasonType.Winter && simulator.Weather != WeatherType.Snow)
                alternativePath = @"\Winter\";
            else if ((textureFlags & TextureFlags.SpringSnow) != 0 && simulator.Season == SeasonType.Spring && simulator.Weather == WeatherType.Snow)
                alternativePath = @"\SpringSnow\";
            else if ((textureFlags & TextureFlags.AutumnSnow) != 0 && simulator.Season == SeasonType.Autumn && simulator.Weather == WeatherType.Snow)
                alternativePath = @"\AutumnSnow\";
            else if ((textureFlags & TextureFlags.WinterSnow) != 0 && simulator.Season == SeasonType.Winter && simulator.Weather == WeatherType.Snow)
                alternativePath = @"\WinterSnow\";

            if (alternativePath.Length > 0 && File.Exists(texturePath + alternativePath + textureName)) return texturePath + alternativePath + textureName;
            if (File.Exists(texturePath + @"\" + textureName)) return texturePath + @"\" + textureName;
			//if (File.Exists(textureName)) return textureName; //some may use \program\content\*.ace
            return null;
        }

        static bool IsSnow(Simulator simulator)
        {
            // MSTS shows snow textures:
            //   - In winter, no matter what the weather is.
            //   - In spring and autumn, if the weather is snow.
            return (simulator.Season == SeasonType.Winter) || ((simulator.Season != SeasonType.Summer) && (simulator.Weather == WeatherType.Snow));
        }

        /// <summary>
        /// Encodes material options code from parameterized options.
        /// Material options encoding is documented in SharedShape.SubObject() (Shapes.cs)
        /// or SceneryMaterial.SetState() (Materials.cs).
        /// </summary>
        /// <param name="lod">LODItem instance.</param>
        /// <returns>Options code.</returns>
        public static int EncodeMaterialOptions(LODItem lod)
        {
            int options = 0; // Material options code to be returned

            // Named shaders
            int namedShader;
            switch (lod.ShaderName)
            {
                case "Diffuse":
                    namedShader = 1;
                    break;
                case "Tex":
                    namedShader = 2;
                    break;
                case "TexDiff":
                    namedShader = 3;
                    break;
                case "BlendATex":
                    namedShader = 4;
                    break;
                case "AddATex":
                    namedShader = 5;
                    break;
                case "BlendATexDiff":
                    namedShader = 6;
                    break;
                case "AddATexDiff":
                    namedShader = 7;
                    break;
                default:
                    namedShader = 3; // Default is TexDiff
                    break;
            }
            options |= namedShader;

            int namedLightingMode;
            switch (lod.LightModelName)
            {
                case "DarkShade":
                    namedLightingMode = 1;
                    break;
                case "OptHalfBright":
                    namedLightingMode = 2;
                    break;
                case "CruciformLong":
                    namedLightingMode = 3;
                    break;
                case "Cruciform":
                    namedLightingMode = 4;
                    break;
                case "OptFullBright":
                    namedLightingMode = 5;
                    break;
                case "OptSpecular750":
                    namedLightingMode = 6;
                    break;
                case "OptSpecular25":
                    namedLightingMode = 7;
                    break;
                case "OptSpecular0":
                    namedLightingMode = 8;
                    break;
                default:
                    namedLightingMode = 8; // Default is OptSpecular0
                    break;
            }
            options |= namedLightingMode << 4;

            options |= lod.AlphaTestMode << 8;

            int namedTexAddrMode;
            switch (lod.TexAddrModeName)
            {
                case "Wrap":
                    namedTexAddrMode = 0;
                    break;
                case "Mirror":
                    namedTexAddrMode = 1;
                    break;
                case "Clamp":
                    namedTexAddrMode = 2;
                    break;
                case "Border":
                    namedTexAddrMode = 3;
                    break;
                default:
                    namedTexAddrMode = 0; // Default is Wrap
                    break;
            }
            options |= namedTexAddrMode << 11;

            options |= lod.ESD_Alternative_Texture << 13;

            return options;
        } // end EncodeMaterialOptions
    } // end class Helpers
}
