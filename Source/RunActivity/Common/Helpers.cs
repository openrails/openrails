/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        static readonly Dictionary<string, SceneryMaterialOptions> TextureAddressingModeNames = new Dictionary<string, SceneryMaterialOptions> {
            { "Wrap", SceneryMaterialOptions.TextureAddressModeWrap },
            { "Mirror", SceneryMaterialOptions.TextureAddressModeMirror },
            { "Clamp", SceneryMaterialOptions.TextureAddressModeClamp },
            { "Border", SceneryMaterialOptions.TextureAddressModeBorder },
        };

        static readonly Dictionary<string, SceneryMaterialOptions> ShaderNames = new Dictionary<string, SceneryMaterialOptions> {
            { "Tex", SceneryMaterialOptions.None },
            { "TexDiff", SceneryMaterialOptions.Diffuse },
            { "BlendATex", SceneryMaterialOptions.AlphaBlendingBlend },
            { "BlendATexDiff", SceneryMaterialOptions.AlphaBlendingBlend | SceneryMaterialOptions.Diffuse },
            { "AddATex", SceneryMaterialOptions.AlphaBlendingAdd },
            { "AddATexDiff", SceneryMaterialOptions.AlphaBlendingAdd | SceneryMaterialOptions.Diffuse },
        };

        static readonly Dictionary<string, SceneryMaterialOptions> LightingModelNames = new Dictionary<string, SceneryMaterialOptions> {
            { "DarkShade", SceneryMaterialOptions.ShaderDarkShade },
            { "OptHalfBright", SceneryMaterialOptions.ShaderHalfBright },
            { "Cruciform", SceneryMaterialOptions.ShaderVegetation },
            { "OptFullBright", SceneryMaterialOptions.ShaderFullBright },
            { "OptSpecular750", SceneryMaterialOptions.None | SceneryMaterialOptions.Specular750 },
            { "OptSpecular25", SceneryMaterialOptions.None | SceneryMaterialOptions.Specular25 },
            { "OptSpecular0", SceneryMaterialOptions.None | SceneryMaterialOptions.None },
        };

        /// <summary>
        /// Encodes material options code from parameterized options.
        /// Material options encoding is documented in SharedShape.SubObject() (Shapes.cs)
        /// or SceneryMaterial.SetState() (Materials.cs).
        /// </summary>
        /// <param name="lod">LODItem instance.</param>
        /// <returns>Options code.</returns>
        public static SceneryMaterialOptions EncodeMaterialOptions(LODItem lod)
        {
            var options = SceneryMaterialOptions.None;

            if (TextureAddressingModeNames.ContainsKey(lod.TexAddrModeName))
                options |= TextureAddressingModeNames[lod.TexAddrModeName];
            else
                Trace.TraceWarning("Skipped unknown texture addressing mode {1} in shape {0}", lod.Name, lod.TexAddrModeName);

            if (lod.AlphaTestMode == 1)
                options |= SceneryMaterialOptions.AlphaTest;

            if (ShaderNames.ContainsKey(lod.ShaderName))
                options |= ShaderNames[lod.ShaderName];
            else
                Trace.TraceWarning("Skipped unknown shader name {1} in shape {0}", lod.Name, lod.ShaderName);

            if (LightingModelNames.ContainsKey(lod.LightModelName))
                options |= LightingModelNames[lod.LightModelName];
            else
                Trace.TraceWarning("Skipped unknown lighting model index {1} in shape {0}", lod.Name, lod.LightModelName);

            if ((lod.ESD_Alternative_Texture & 0x1) != 0)
                options |= SceneryMaterialOptions.NightTexture;

            return options;
        } // end EncodeMaterialOptions
    } // end class Helpers
}
