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
using System.Windows.Forms;
using ORTS.Common;
using ORTS.Settings;

namespace ORTS.Processes
{
    /// <summary>
    /// Provides the foundation for running the game.
    /// </summary>
    [CallOnThread("Render")]
    public class Game : Microsoft.Xna.Framework.Game
    {
        /// <summary>
        /// Gets the <see cref="UserSettings"/> for the game.
        /// </summary>
        public UserSettings Settings { get; private set; }

        /// <summary>
        /// Gets the directory path containing game-specific content.
        /// </summary>
        public string ContentPath { get; private set; }

        /// <summary>
        /// Exposes access to the <see cref="RenderProcess"/> for the game.
        /// </summary>
        public RenderProcess RenderProcess { get; private set; }

        /// <summary>
        /// Exposes access to the <see cref="UpdaterProcess"/> for the game.
        /// </summary>
        public UpdaterProcess UpdaterProcess { get; private set; }

        /// <summary>
        /// Exposes access to the <see cref="LoaderProcess"/> for the game.
        /// </summary>
        public LoaderProcess LoaderProcess { get; private set; }

        /// <summary>
        /// Gets the current <see cref="GameState"/>, if there is one, or <c>null</c>.
        /// </summary>
        public GameState State { get { return States.Count > 0 ? States.Peek() : null; } }

        Stack<GameState> States;

        /// <summary>
        /// Initializes a new instance of the <see cref="Game"/> based on the specified <see cref="UserSettings"/>.
        /// </summary>
        /// <param name="settings">The <see cref="UserSettings"/> for the game to use.</param>
        public Game(UserSettings settings)
        {
            Settings = settings;
            LoadLanguage();
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

        public void LoadLanguage()
        {
            switch (Settings.Language)
            {
                case "System": System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InstalledUICulture; break;
                case "Danish": System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("da"); break;
                case "English": System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en"); break;
                case "French": System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("fr"); break;
                case "Hungarian": System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("hu"); break;
                case "Italian": System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("it"); break;
            }
        }

        /// <summary>
        /// Reports an <see cref="Exception"/> to the log file and/or user, exiting the game in the process.
        /// </summary>
        /// <param name="error">The <see cref="Exception"/> to report.</param>
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
                System.Windows.Forms.MessageBox.Show(error.ToString(), Application.ProductName + " " + VersionInfo.VersionOrBuild);
        }
    }
}
