// COPYRIGHT 2026 by the Open Rails project.
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

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using ORTS.Common;

namespace Orts.Viewer3D.Processes
{
    /// <summary>
    /// Provides Render-thread resource service and terminal cleanup for a Game-lifetime participant.
    /// </summary>
    internal interface IRenderResourceParticipant
    {
        /// <summary>
        /// Services deferred resource work on Render before drawing, including when no game state is active.
        /// </summary>
        void ProcessPending(GraphicsDevice graphicsDevice);
        /// <summary>
        /// Performs terminal Render-thread cleanup; no subsequent drawing or resource service is expected.
        /// </summary>
        void Close();
    }

    /// <summary>
    /// Services a startup-sealed set of resource participants on the Render thread.
    /// </summary>
    /// <remarks>
    /// Registration is limited to initialization. All calls are non-concurrent. Exceptions propagate without recovery or automatic retries.
    /// </remarks>
    [CallOnThread("Render")]
    internal sealed class RenderResourceCoordinator
    {
        private enum CoordinatorState { Configuring, Ready, Closing, Closed }

        private readonly List<IRenderResourceParticipant> Registrations = new List<IRenderResourceParticipant>();
        private IRenderResourceParticipant[] Participants;
        private CoordinatorState State;
        private bool Servicing;

        /// <summary>
        /// Registers a non-null participant once, before registration is sealed.
        /// </summary>
        internal void Register(IRenderResourceParticipant participant)
        {
            if (State != CoordinatorState.Configuring)
                throw new InvalidOperationException("Render resource registration is closed.");
            if (participant == null)
                throw new ArgumentNullException(nameof(participant));
            foreach (var registered in Registrations)
                if (ReferenceEquals(registered, participant))
                    throw new InvalidOperationException("Render resource participant is already registered.");
            Registrations.Add(participant);
        }

        /// <summary>
        /// Freezes the participant list before worker processes start.
        /// </summary>
        internal void SealRegistrations()
        {
            if (State != CoordinatorState.Configuring)
                throw new InvalidOperationException("Render resource registration is already sealed.");
            Participants = Registrations.ToArray();
            State = CoordinatorState.Ready;
        }

        /// <summary>
        /// Services the sealed participant list without concurrent or reentrant execution.
        /// </summary>
        internal void ProcessPending(GraphicsDevice graphicsDevice)
        {
            if (State != CoordinatorState.Ready || Servicing)
                throw new InvalidOperationException("Render resource service is unavailable or reentrant.");
            Servicing = true;
            try
            {
                foreach (var participant in Participants)
                    participant.ProcessPending(graphicsDevice);
            }
            finally
            {
                Servicing = false;
            }
        }

        /// <summary>
        /// Attempts terminal participant cleanup once and rejects further registration.
        /// </summary>
        /// <remarks>
        /// A participant exception stops the traversal. The coordinator becomes closed even if later participants have not been called.
        /// </remarks>
        internal void Close()
        {
            if (State == CoordinatorState.Closing || State == CoordinatorState.Closed)
                return;
            if (State != CoordinatorState.Ready || Servicing)
                throw new InvalidOperationException("Render resource closure requires a sealed, idle coordinator.");
            State = CoordinatorState.Closing;
            try
            {
                foreach (var participant in Participants)
                    participant.Close();
            }
            finally
            {
                // Terminal means closure was attempted, not that all resources were released successfully.
                State = CoordinatorState.Closed;
            }
        }
    }
}
