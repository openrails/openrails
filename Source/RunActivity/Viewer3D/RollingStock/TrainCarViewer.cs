// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
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

using Microsoft.Xna.Framework;
using Orts.Simulation.RollingStocks;
using ORTS.Common;
using System.Diagnostics;

namespace Orts.Viewer3D.RollingStock
{
    public abstract class TrainCarViewer
    {
        // TODO add view location and limits
        public TrainCar Car;
        public LightViewer lightDrawer;

        protected Viewer Viewer;

        public TrainCarViewer(Viewer viewer, TrainCar car)
        {
            Car = car;
            Viewer = viewer;

            Car.StaleViewer = false;
        }

        public abstract void HandleUserInput(ElapsedTime elapsedTime);

        public abstract void InitializeUserInputCommands();

        /// <summary>
        /// Called at the full frame rate
        /// elapsedTime is time since last frame
        /// Executes in the UpdaterThread
        /// </summary>
        public abstract void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime);

        [CallOnThread("Loader")]
        public virtual void Unload() { }

        [CallOnThread("Loader")]
        internal virtual void LoadForPlayer() { }

        [CallOnThread("Loader")]
        internal abstract void Mark();

        /// <summary>
        /// Checks this car viewer for stale directly-referenced textures and sets the stale data flag if any textures are stale
        /// </summary>
        /// <returns>bool indicating if this viewer changed from fresh to stale</returns>
        public virtual bool CheckStaleTextures()
        {
            if (!Car.StaleViewer)
            {
                if (lightDrawer != null && lightDrawer.CheckStale())
                    Car.StaleViewer = true;

                return Car.StaleViewer;
            }
            return false;
        }
        public virtual bool CheckStaleShapes() { return false; }
        public virtual bool CheckStaleSounds() { return false; }


        public float[] Velocity = new float[] { 0, 0, 0 };
        WorldLocation SoundLocation;

        public void UpdateSoundPosition()
        {
            if (Car.SoundSourceIDs.Count == 0 || Viewer.Camera == null)
                return;

            if (Car.Train != null)
            {
                var realSpeedMpS = Car.SpeedMpS;
                //TODO Following if block is needed due to physics flipping when using rear cab
                // If such physics flipping is removed next block has to be removed.
                if (Car is MSTSLocomotive)
                {
                    var loco = Car as MSTSLocomotive;
                    if (loco.UsingRearCab) realSpeedMpS = -realSpeedMpS;
                }
                Vector3 directionVector = Vector3.Multiply(Car.WorldPosition.XNAMatrix.Forward, realSpeedMpS);
                Velocity = new float[] { directionVector.X, directionVector.Y, -directionVector.Z };
            }
            else
                Velocity = new float[] { 0, 0, 0 };

            // TODO This entire block of code (down to TODO END) should be inside the SoundProcess, not here.
            SoundLocation = new WorldLocation(Car.WorldPosition.WorldLocation);
            SoundLocation.NormalizeTo(Camera.SoundBaseTile.X, Camera.SoundBaseTile.Y);
            float[] position = new float[] {
                SoundLocation.Location.X,
                SoundLocation.Location.Y,
                SoundLocation.Location.Z};

            // make a copy of SoundSourceIDs, but check that it didn't change during the copy; if it changed, try again up to 5 times.
            var sSIDsFinalCount = -1;
            var sSIDsInitCount = -2;
            int[] soundSourceIDs = { 0 };
            int trialCount = 0;
            try
            {
                while (sSIDsInitCount != sSIDsFinalCount && trialCount < 5)
                {
                    sSIDsInitCount = Car.SoundSourceIDs.Count;
                    soundSourceIDs = Car.SoundSourceIDs.ToArray();
                    sSIDsFinalCount = Car.SoundSourceIDs.Count;
                    trialCount++;
                }
            }
            catch
            {
                Trace.TraceInformation("Skipped update of position and speed of sound sources");
                return;
            }
            if (trialCount >= 5)
                return;
            foreach (var soundSourceID in soundSourceIDs)
            {
                Viewer.Simulator.updaterWorking = true;
                if (OpenAL.alIsSource(soundSourceID))
                {
                    OpenAL.alSourcefv(soundSourceID, OpenAL.AL_POSITION, position);
                    OpenAL.alSourcefv(soundSourceID, OpenAL.AL_VELOCITY, Velocity);
                }
                Viewer.Simulator.updaterWorking = false;
            }
            // TODO END
        }
    }
}
