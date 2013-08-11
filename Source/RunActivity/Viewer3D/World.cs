// COPYRIGHT 2012, 2013 by the Open Rails project.
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

// This file is the responsibility of the 3D & Environment Team. 

using Microsoft.Xna.Framework;

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
        bool PerformanceTune;

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
                GameSounds = new SoundSource(viewer, Events.Source.MSTSInGame, viewer.Simulator.RoutePath + "\\Sound\\ingame.sms");
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
            if (PerformanceTune)
            {
                // Work out how far we need to change the actual FPS to get to the target.
                var target = Viewer.Settings.PerformanceTunerTarget - Viewer.RenderProcess.FrameRate.SmoothedValue;

                // If vertical sync is on, we're capped to 60 FPS. This means we need to shift a target of 60FPS down to 57FPS.
                if (Viewer.Settings.VerticalSync && Viewer.Settings.PerformanceTunerTarget > 55)
                    target -= 3;

                // Now we adjust the viewing distance to try and balance out the FPS.
                var oldViewingDistance = Viewer.Settings.ViewingDistance;
                if (target > 2.5)
                {
                    if (Viewer.Settings.ViewingDistance > 250)
                        Viewer.Settings.ViewingDistance -= (int)(target - 1.5);
                }
                else if (target < -2.5)
                {
                    if (Viewer.Settings.ViewingDistance < 5000)
                        Viewer.Settings.ViewingDistance += (int)(-target - 1.5);
                }
                Viewer.Settings.ViewingDistance = (int)MathHelper.Clamp(Viewer.Settings.ViewingDistance, 250, 5000);

                // If we've changed the viewing distance, we need to update the camera matricies.
                if (oldViewingDistance != Viewer.Settings.ViewingDistance)
                    Viewer.Camera.ScreenChanged();

                // Flag as done, so the next load prep (every 250ms) can trigger us again.
                PerformanceTune = false;
            }
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
            PerformanceTune = Viewer.Settings.PerformanceTuner;
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
