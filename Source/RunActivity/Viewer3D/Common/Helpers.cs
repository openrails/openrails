// COPYRIGHT 2010, 2011, 2012 by the Open Rails project.
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Orts.Formats.Msts;
using Orts.Simulation;

namespace Orts.Viewer3D.Common
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
            Underground = 0x40000000,
        }

        public static string GetForestTextureFile(Simulator simulator, string textureName)
        {
            return GetRouteTextureFile(simulator, Helpers.TextureFlags.Spring | Helpers.TextureFlags.Autumn | Helpers.TextureFlags.Winter | Helpers.TextureFlags.SpringSnow | Helpers.TextureFlags.AutumnSnow | Helpers.TextureFlags.WinterSnow, textureName);
        }

        public static string GetNightTextureFile(Simulator simulator, string textureFilePath)
        {
            var texturePath = Path.GetDirectoryName(textureFilePath);
            var textureName = Path.GetFileName(textureFilePath);
            var nightTexturePath = !File.Exists(texturePath + @"\Night\" + textureName) &&
                !File.Exists(texturePath + @"\Night\" + Path.ChangeExtension(textureName, ".dds")) ? Path.GetDirectoryName(texturePath) + @"\Night\" : texturePath + @"\Night\";

            if (!String.IsNullOrEmpty(nightTexturePath + textureName) && Path.GetExtension(nightTexturePath + textureName) == ".dds" && File.Exists(nightTexturePath + textureName))
            {
                return nightTexturePath + textureName;
            }
            else if (!String.IsNullOrEmpty(nightTexturePath + textureName) && Path.GetExtension(nightTexturePath + textureName) == ".ace")
            {
                var alternativeTexture = Path.ChangeExtension(nightTexturePath + textureName, ".dds");
                if (!String.IsNullOrEmpty(alternativeTexture.ToLower()) && File.Exists(alternativeTexture))
                {
                    return alternativeTexture;
                }
                else if (File.Exists(nightTexturePath + textureName))
                {
                    return nightTexturePath + textureName;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public static string GetRouteTextureFile(Simulator simulator, TextureFlags textureFlags, string textureName)
        {
            return GetTextureFile(simulator, textureFlags, simulator.RoutePath + @"\Textures", textureName);
        }

        public static string GetTransferTextureFile(Simulator simulator, string textureName)
        {
            return GetTextureFile(simulator, Helpers.TextureFlags.Snow, simulator.RoutePath + @"\Textures", textureName);
        }

        public static string GetTerrainTextureFile(Simulator simulator, string textureName)
        {
            return GetTextureFile(simulator, Helpers.TextureFlags.Snow, simulator.RoutePath + @"\TerrTex", textureName);
        }

        public static string GetTextureFile(Simulator simulator, TextureFlags textureFlags, string texturePath, string textureName)
        {
            var alternativePath = @"\";
            if ((textureFlags & TextureFlags.Snow) != 0 || (textureFlags & TextureFlags.SnowTrack) != 0)
                if (IsSnow(simulator))
                    alternativePath = @"\Snow\";
                else
                    alternativePath = @"\";
            else if ((textureFlags & TextureFlags.Spring) != 0 && simulator.Season == SeasonType.Spring && simulator.WeatherType != WeatherType.Snow)
                alternativePath = @"\Spring\";
            else if ((textureFlags & TextureFlags.Autumn) != 0 && simulator.Season == SeasonType.Autumn && simulator.WeatherType != WeatherType.Snow)
                alternativePath = @"\Autumn\";
            else if ((textureFlags & TextureFlags.Winter) != 0 && simulator.Season == SeasonType.Winter && simulator.WeatherType != WeatherType.Snow)
                alternativePath = @"\Winter\";
            else if ((textureFlags & TextureFlags.SpringSnow) != 0 && simulator.Season == SeasonType.Spring && simulator.WeatherType == WeatherType.Snow)
                alternativePath = @"\SpringSnow\";
            else if ((textureFlags & TextureFlags.AutumnSnow) != 0 && simulator.Season == SeasonType.Autumn && simulator.WeatherType == WeatherType.Snow)
                alternativePath = @"\AutumnSnow\";
            else if ((textureFlags & TextureFlags.WinterSnow) != 0 && simulator.Season == SeasonType.Winter && simulator.WeatherType == WeatherType.Snow)
                alternativePath = @"\WinterSnow\";

            if (alternativePath.Length > 0) return texturePath + alternativePath + textureName;
            return texturePath + @"\" + textureName;
        }

        public static bool IsSnow(Simulator simulator)
        {
            // MSTS shows snow textures:
            //   - In winter, no matter what the weather is.
            //   - In spring and autumn, if the weather is snow.
            return (simulator.Season == SeasonType.Winter) || ((simulator.Season != SeasonType.Summer) && (simulator.WeatherType == WeatherType.Snow));
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

            if ((lod.ESD_Alternative_Texture & (int)TextureFlags.Night) != 0)
                options |= SceneryMaterialOptions.NightTexture;

            return options;
        } // end EncodeMaterialOptions
    } // end class Helpers
}
