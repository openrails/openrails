// COPYRIGHT 2012 by the Open Rails project.
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
using System.Text;

namespace ORTS
{
    public class World
    {
        public readonly WeatherControl WeatherControl;
        public readonly SkyDrawer Sky;
        public readonly PrecipDrawer Precipitation;
        public readonly TerrainDrawer Terrain;
        public readonly SceneryDrawer Scenery;
        public readonly TrainDrawer Trains;
        public readonly RoadCarHandler Cars;
        public readonly SoundSource GameSounds;
        public readonly WorldSounds Sounds;

        [CallOnThread("Render")]
        public World(Viewer3D viewer)
        {
            WeatherControl = new WeatherControl(viewer);
            Sky = new SkyDrawer(viewer);
            Precipitation = new PrecipDrawer(viewer);
            Terrain = new TerrainDrawer(viewer);
            Scenery = new SceneryDrawer(viewer);
            Trains = new TrainDrawer(viewer);
            Cars = new RoadCarHandler(viewer);
            if (viewer.Settings.SoundDetailLevel > 0)
            {
                // Keep it silent while loading.
                ALSoundSource.MuteAll();
                // TODO: This looks kinda evil; do something about it.
                GameSounds = new SoundSource(viewer, viewer.Simulator.RoutePath + "\\Sound\\ingame.sms");
                Sounds = new WorldSounds(viewer);
            }
        }

        [CallOnThread("Loader")]
        public void Load()
        {
            Terrain.Load();
            Scenery.Load();
            Trains.Load();
        }

        [CallOnThread("Updater")]
        public void Update(ElapsedTime elapsedTime)
        {
        }

        [CallOnThread("Updater")]
        public void LoadPrep()
        {
            Terrain.LoadPrep();
            Scenery.LoadPrep();
            Trains.LoadPrep();
        }

        [CallOnThread("Updater")]
        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            Sky.PrepareFrame(frame, elapsedTime);
            Precipitation.PrepareFrame(frame, elapsedTime);
            Terrain.PrepareFrame(frame, elapsedTime);
            Scenery.PrepareFrame(frame, elapsedTime);
            Trains.PrepareFrame(frame, elapsedTime);
            Cars.PrepareFrame(frame, elapsedTime);
        }
    }
}
