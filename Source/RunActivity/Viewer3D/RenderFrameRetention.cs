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

namespace Orts.Viewer3D
{
    /// <summary>
    /// Represents a reusable CPU retention receipt released synchronously when its frame is cleared.
    /// </summary>
    /// <remarks>
    /// Registration and release belong to the frame's synchronous Updater traversal. Do not release receipts externally or use them as deferred release handles. The receipt does not own GPU resources.
    /// </remarks>
    internal abstract class RenderFrameRetention
    {
        private RenderFrame RegisteredFrame;
        private bool Releasing;
        internal RenderFrameRetention NextInFrame { get; private set; }
        /// <summary>
        /// Indicates that the receipt is neither registered nor being released.
        /// </summary>
        internal bool IsAvailable { get { return RegisteredFrame == null && !Releasing; } }

        /// <summary>
        /// Associates an available receipt with a frame and the next receipt in its intrusive list.
        /// </summary>
        /// <returns>False without changing the receipt when the frame is null or the receipt is unavailable.</returns>
        /// <remarks>
        /// Used by RenderFrame registration; associating the receipt alone does not insert it into a frame's list.
        /// </remarks>
        internal bool TryRegister(RenderFrame frame, RenderFrameRetention next)
        {
            if (frame == null || !IsAvailable) return false;
            RegisteredFrame = frame;
            NextInFrame = next;
            return true;
        }

        /// <summary>
        /// Returns the registered retention and makes the receipt available only after successful completion.
        /// </summary>
        /// <remarks>
        /// A callback exception propagates and leaves the receipt unavailable. Release is performed by the owning frame's retention traversal.
        /// </remarks>
        internal void Release(RenderFrame frame)
        {
            if (RegisteredFrame != frame || Releasing || RegisteredFrame == null)
                throw new InvalidOperationException("Invalid render frame retention release.");
            Releasing = true;
            NextInFrame = null;
            ReleaseCore(frame);
            RegisteredFrame = null;
            Releasing = false;
        }

        /// <summary>
        /// Returns the owner's CPU retention without disposing GPU resources.
        /// </summary>
        protected abstract void ReleaseCore(RenderFrame frame);
    }
}
