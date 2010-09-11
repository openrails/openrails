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
    class Helpers
    {
        public string GetTextureFolder(Viewer3D viewer, int altTex)
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
        }
    }
}
