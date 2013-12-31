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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ORTS.Processes
{
    [CallOnThread("Render")]
    public class Game : Microsoft.Xna.Framework.Game
    {
        public readonly UserSettings Settings;
        public readonly string ContentPath;
        public readonly RenderProcess RenderProcess;
        public readonly UpdaterProcess UpdaterProcess;
        public readonly LoaderProcess LoaderProcess;

        public GameState State { get { return States.Count > 0 ? States.Peek() : null; } }

        Stack<GameState> States;

        public Game(UserSettings settings)
        {
            Settings = settings;
            ContentPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath), "Content");
            Exiting += new System.EventHandler(Game_Exiting);
            RenderProcess = new RenderProcess(this);
            UpdaterProcess = new UpdaterProcess(this);
            LoaderProcess = new LoaderProcess(this);
            States = new Stack<GameState>();
        }

        [ThreadName("Render")]
        protected override void BeginRun()
        {
            // At this point, GraphicsDevice is initialized and set up.
            LoaderProcess.Start();
            UpdaterProcess.Start();
            RenderProcess.Start();
            base.BeginRun();
        }

        [ThreadName("Render")]
        protected override void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            // The first Update() is called before the window is displayed, with a gameTime == 0. The second is called
            // after the window is displayed.
            if (State == null)
                Exit();
            else
                RenderProcess.Update(gameTime);
            base.Update(gameTime);
        }

        [ThreadName("Render")]
        protected override bool BeginDraw()
        {
            if (!base.BeginDraw())
                return false;
            RenderProcess.BeginDraw();
            return true;
        }

        [ThreadName("Render")]
        protected override void Draw(Microsoft.Xna.Framework.GameTime gameTime)
        {
            RenderProcess.Draw();
            base.Draw(gameTime);
        }

        [ThreadName("Render")]
        protected override void EndDraw()
        {
            RenderProcess.EndDraw();
            base.EndDraw();
        }

        [ThreadName("Render")]
        protected override void EndRun()
        {
            base.EndRun();
            RenderProcess.Stop();
            UpdaterProcess.Stop();
            LoaderProcess.Stop();
        }

        [ThreadName("Render")]
        void Game_Exiting(object sender, EventArgs e)
        {
            while (State != null)
                PopState();
        }

        [CallOnThread("Loader")]
        internal void PushState(GameState state)
        {
            state.Game = this;
            States.Push(state);
            Trace.TraceInformation("Game.PushState({0})  {1}", state.GetType().Name, String.Join(" | ", States.Select(s => s.GetType().Name).ToArray()));
        }

        [CallOnThread("Loader")]
        internal void PopState()
        {
            State.Dispose();
            States.Pop();
            Trace.TraceInformation("Game.PopState()  {0}", String.Join(" | ", States.Select(s => s.GetType().Name).ToArray()));
        }

        [CallOnThread("Loader")]
        internal void ReplaceState(GameState state)
        {
            if (State != null)
            {
                State.Dispose();
                States.Pop();
            }
            state.Game = this;
            States.Push(state);
            Trace.TraceInformation("Game.ReplaceState({0})  {1}", state.GetType().Name, String.Join(" | ", States.Select(s => s.GetType().Name).ToArray()));
        }

        [CallOnThread("Render")]
        [CallOnThread("Updater")]
        [CallOnThread("Loader")]
        public void ProcessReportError(Exception error)
        {
            // Log the error first in case we're burning.
            Trace.WriteLine(new FatalException(error));
            // Stop the world!
            Exit();
            // Show the user that it's all gone horribly wrong.
            if (Settings.ShowErrorDialogs)
                System.Windows.Forms.MessageBox.Show(error.ToString());
        }
    }
}
