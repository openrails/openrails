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

        }

        public abstract void HandleUserInput(ElapsedTime elapsedTime);

        public abstract void InitializeUserInputCommands();

        /// <summary>
        /// Called at the full frame rate
        /// elapsedTime is time since last frame
        /// Executes in the UpdaterThread
        /// </summary>
        public abstract void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime);
        public abstract void UpdateAnimations(ElapsedTime elapsedTime);

        [CallOnThread("Loader")]
        public virtual void Unload() { }

        [CallOnThread("Loader")]
        internal virtual void LoadForPlayer() { }

        [CallOnThread("Loader")]
        internal abstract void Mark();


        public float[] Velocity = new float[] { 0, 0, 0 };
        public WorldLocation SoundLocation;

        public void UpdateSoundPosition()
        {
            if (Viewer.Camera == null)
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

            SoundLocation = new WorldLocation(Car.WorldPosition.WorldLocation);
            SoundLocation.NormalizeTo(Camera.SoundBaseTile.X, Camera.SoundBaseTile.Y);
        }
    }
}
