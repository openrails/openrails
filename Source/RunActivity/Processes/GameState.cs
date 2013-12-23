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


namespace ORTS.Processes
{
    public abstract class GameState
    {
        internal Game Game { get; set; }

        [CallOnThread("Render")]
        internal virtual void BeginRender(RenderFrame frame)
        {
        }

        [CallOnThread("Render")]
        internal virtual void EndRender(RenderFrame frame)
        {
        }

        [CallOnThread("Updater")]
        internal virtual void Update(RenderFrame frame, double totalRealSeconds)
        {
            // By default, every update tries to trigger a load.
            if (Game.LoaderProcess.Finished)
                Game.LoaderProcess.StartLoad();
        }

        [CallOnThread("Loader")]
        internal virtual void Load()
        {
        }

        [CallOnThread("Loader")]
        internal virtual void Dispose()
        {
        }
    }
}
