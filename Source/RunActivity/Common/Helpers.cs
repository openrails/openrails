/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSTS;

namespace ORTS
{
    static class Helpers
    {
        public static string GetTextureFolder(Viewer3D viewer, int altTex)
        {
            string textureFolder;
            int season = (int)viewer.Simulator.Season;
            int weather = (int)viewer.Simulator.Weather;

            switch (altTex)
            {
                case 0:
                default:
                    textureFolder = viewer.Simulator.RoutePath + @"\textures";
                    break;
                case 1:
                case 2: // Track
                    if (season == (int)SeasonType.Winter || weather == (int)WeatherType.Snow)
                        textureFolder = viewer.Simulator.RoutePath + @"\textures\snow";
                    else
                        textureFolder = viewer.Simulator.RoutePath + @"\textures";
                    break;
                case 252: // Vegetation
                    if (season == (int)SeasonType.Spring && weather != (int)WeatherType.Snow)
                        textureFolder = viewer.Simulator.RoutePath + @"\textures\spring";
                    else if (season == (int)SeasonType.Spring && weather == (int)WeatherType.Snow)
                        textureFolder = viewer.Simulator.RoutePath + @"\textures\springsnow";
                    else if (season == (int)SeasonType.Autumn && weather != (int)WeatherType.Snow)
                        textureFolder = viewer.Simulator.RoutePath + @"\textures\autumn";
                    else if (season == (int)SeasonType.Autumn && weather == (int)WeatherType.Snow)
                        textureFolder = viewer.Simulator.RoutePath + @"\textures\autumnsnow";
                    else if (season == (int)SeasonType.Winter && weather != (int)WeatherType.Snow)
                        textureFolder = viewer.Simulator.RoutePath + @"\textures\winter";
                    else if (season == (int)SeasonType.Winter && weather == (int)WeatherType.Snow)
                        textureFolder = viewer.Simulator.RoutePath + @"\textures\wintersnow";
                    else
                        textureFolder = viewer.Simulator.RoutePath + @"\textures";
                    break;
                case 256: // Incorrect param in MSTS. In OR we default to 257.
                case 257:
                    if (season == (int)SeasonType.Winter || weather == (int)WeatherType.Snow)
                        textureFolder = viewer.Simulator.RoutePath + @"\textures\snow";
                    else
                        textureFolder = viewer.Simulator.RoutePath + @"\textures";
                    break;
            }
            return textureFolder;
        } // end GetTextureFolder

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
