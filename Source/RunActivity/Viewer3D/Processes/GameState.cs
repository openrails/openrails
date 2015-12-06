// COPYRIGHT 2013 by the Open Rails project.
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

using ORTS.Common;

namespace Orts.Viewer3D.Processes
{
    /// <summary>
    /// Represents a single state for the game to be in (e.g. loading, running, in menu).
    /// </summary>
    public abstract class GameState
    {
        internal Game Game { get; set; }

        /// <summary>
        /// Called just before a frame is drawn.
        /// </summary>
        /// <param name="frame">The <see cref="RenderFrame"/> containing everything to be drawn.</param>
        [CallOnThread("Render")]
        internal virtual void BeginRender(RenderFrame frame)
        {
        }

        /// <summary>
        /// Called just after a frame is drawn.
        /// </summary>
        /// <param name="frame">The <see cref="RenderFrame"/> containing everything that was drawn.</param>
        [CallOnThread("Render")]
        internal virtual void EndRender(RenderFrame frame)
        {
        }

        /// <summary>
        /// Called to update the game and populate a new <see cref="RenderFrame"/>.
        /// </summary>
        /// <param name="frame">The new <see cref="RenderFrame"/> that needs populating.</param>
        /// <param name="totalRealSeconds">The total number of real-world seconds which have elapsed since the game was started.</param>
        [CallOnThread("Updater")]
        internal virtual void Update(RenderFrame frame, double totalRealSeconds)
        {
            // By default, every update tries to trigger a load.
            if (Game.LoaderProcess.Finished)
                Game.LoaderProcess.StartLoad();
        }

        /// <summary>
        /// Called to load new content as and when necessary.
        /// </summary>
        [CallOnThread("Loader")]
        internal virtual void Load()
        {
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="GameState"/> class.
        /// </summary>
        [CallOnThread("Loader")]
        internal virtual void Dispose()
        {
        }
    }
}
