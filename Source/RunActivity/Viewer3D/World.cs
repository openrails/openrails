// COPYRIGHT 2012 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 


namespace ORTS
{
    public class World
    {
        readonly Viewer3D Viewer;
        public readonly WeatherControl WeatherControl;
        public readonly SkyDrawer Sky;
        public readonly PrecipDrawer Precipitation;
        public readonly TerrainDrawer Terrain;
        public readonly SceneryDrawer Scenery;
        public readonly TrainDrawer Trains;
        public readonly RoadCarDrawer RoadCars;
        public readonly SoundSource GameSounds;
        public readonly WorldSounds Sounds;

        int TileX;
        int TileZ;
        int VisibleTileX;
        int VisibleTileZ;

        [CallOnThread("Render")]
        public World(Viewer3D viewer)
        {
            Viewer = viewer;
            // Control stuff first.
            WeatherControl = new WeatherControl(viewer);
            // Then drawers.
            Sky = new SkyDrawer(viewer);
            if (viewer.Settings.Precipitation)
                Precipitation = new PrecipDrawer(viewer);
            Terrain = new TerrainDrawer(viewer);
            Scenery = new SceneryDrawer(viewer);
            Trains = new TrainDrawer(viewer);
            RoadCars = new RoadCarDrawer(viewer);
            // Then sound.
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
            RoadCars.Load();
            if (TileX != VisibleTileX || TileZ != VisibleTileZ)
            {
                TileX = VisibleTileX;
                TileZ = VisibleTileZ;
                Viewer.ShapeManager.Mark();
                Viewer.MaterialManager.Mark();
                Viewer.TextureManager.Mark();
                Sky.Mark();
                if (Precipitation != null) Precipitation.Mark();
                Terrain.Mark();
                Scenery.Mark();
                Trains.Mark();
                RoadCars.Mark();
                Viewer.Mark();
                Viewer.ShapeManager.Sweep();
                Viewer.MaterialManager.Sweep();
                Viewer.TextureManager.Sweep();
            }
        }

        [CallOnThread("Updater")]
        public void Update(ElapsedTime elapsedTime)
        {
            Scenery.Update(elapsedTime);
        }

        [CallOnThread("Updater")]
        public void LoadPrep()
        {
            Terrain.LoadPrep();
            Scenery.LoadPrep();
            Trains.LoadPrep();
            RoadCars.LoadPrep();
            VisibleTileX = Viewer.Camera.TileX;
            VisibleTileZ = Viewer.Camera.TileZ;
        }

        [CallOnThread("Updater")]
        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            Sky.PrepareFrame(frame, elapsedTime);
            if (Precipitation != null) Precipitation.PrepareFrame(frame, elapsedTime);
            Terrain.PrepareFrame(frame, elapsedTime);
            Scenery.PrepareFrame(frame, elapsedTime);
            Trains.PrepareFrame(frame, elapsedTime);
            RoadCars.PrepareFrame(frame, elapsedTime);
        }
    }
}
