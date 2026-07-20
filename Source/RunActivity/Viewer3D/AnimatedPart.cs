// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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

using Orts.Formats.Msts;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Orts.Viewer3D
{
    /// <summary>
    /// Support for animating any sub-part of a wagon or locomotive. Supports both on/off toggled animations and continuous-running ones.
    /// </summary>
    public class AnimatedPart
    {
        // Shape that we're animating.
        readonly PoseableShape PoseableShape;

        /// <summary>
        /// shape format: maximum animation key-frame value used by this part. This is calculated from the matrices provided.
        /// glTF format: the frames are measured in seconds, so the frame count is actually the total length of the animation clip in seconds.
        /// </summary>
        public float MaxFrame;

        /// <summary>
        /// shape format: Current frame of the animation.
        /// glTF format: The actual time in seconds within the animation clip.
        /// </summary>
        float AnimationKey;

        /// <summary>
        /// Controls the speed of the animation.
        /// </summary>
        float Speed = 1.0f;

        /// <summary>
        /// shape format: List of the matrices we're animating for this part.
        /// glTF format: The animation clip's numbers we are playing for this part.
        /// </summary>
        public List<int> MatrixIndexes = new List<int>();

        /// <summary>
        /// Construct with a link to the shape that contains the animated parts 
        /// </summary>
        public AnimatedPart(PoseableShape poseableShape)
        {
            PoseableShape = poseableShape;
        }

        /// <summary>
        /// All the matrices associated with this part are added during initialization by the MSTSWagon constructor
        /// </summary>
        public void AddMatrix(int matrix)
        {
            if (matrix < 0 || MatrixIndexes.Contains(matrix))
                return;
            MatrixIndexes.Add(matrix);
            UpdateMaxFrame(matrix);
        }

        void UpdateMaxFrame(int matrix)
        {
            MaxFrame = PoseableShape.SharedShape.GetAnimationLength(matrix);

            if (!(PoseableShape.SharedShape is GltfShape))
            {
                for (var i = 0; i < PoseableShape.Hierarchy.Length; i++)
                    if (PoseableShape.Hierarchy[i] == matrix)
                        UpdateMaxFrame(i);
            }
        }

        public void AddAllMatrices()
        {
            var animationsCount = PoseableShape.SharedShape.GetAnimationNamesCount();
            for (var i = 0; i < animationsCount; i++)
                if (PoseableShape.SharedShape.HasAnimation(i))
                    AddMatrix(i);
        }

        /// <summary>
        /// Sets the speed only in case the of shape format.
        /// </summary>
        public void SetMstsSpeed(float mstsSpeed, bool useShapeFrameRate)
        {
            if (PoseableShape?.SharedShape?.Animations?.Count > 0)
            {
                Speed = mstsSpeed;

                if (useShapeFrameRate)
                    Speed *= (PoseableShape?.SharedShape?.Animations?.ElementAtOrDefault(0)?.FrameRate ?? 30f) / 30f;
            }
        }

        public void SetSpeedPositive()
        {
            Speed = Math.Abs(Speed);
        }

        public void SetSpeedNegative()
        {
            Speed = -Math.Abs(Speed);
        }

        /// <summary>
        /// Sets the speed only in case the of glTF format.
        /// </summary>
        public void SetGltfSpeed(float speed)
        {
            if (PoseableShape?.SharedShape is GltfShape)
                Speed = speed;
        }

        /// <summary>
        /// Ensure the shape file contained parts of this type 
        /// and those parts have an animation section.
        /// </summary>
        public bool IsAnimated()
        {
            return MatrixIndexes.Count > 0;
        }

        void SetFrame(float frame)
        {
            AnimationKey = frame;
            foreach (var matrix in MatrixIndexes)
                PoseableShape.AnimateMatrix(matrix, AnimationKey);
        }

        /// <summary>
        /// Sets the animation to a particular frame whilst clamping it to the frame count range.
        /// </summary>
        public void SetFrameClamp(float frame)
        {
            SetFrame(MathHelper.Clamp(frame, 0, MaxFrame));
        }

        /// <summary>
        /// Sets the animation to a particular frame whilst cycling back to the start as input goes beyond the last frame.
        /// Animates from 0-MaxFrame then MaxFrame-0 for values within [0 .. 2*MaxFrame].
        /// </summary>
        public void SetFrameCycle(float frame)
        {
            SetFrameClamp(MaxFrame - Math.Abs(frame - MaxFrame));
        }

        /// <summary>
        /// Sets the animation to a particular frame whilst wrapping it around the frame count range.
        /// </summary>
        public void SetFrameWrap(float frame)
        {
            // Wrap the frame around 0-MaxFrame without hanging when MaxFrame=0.
            if (MaxFrame > 0)
            {
                frame %= MaxFrame;
                // If frame was negative (eg: animation run in reverse), it will still be negative
                // and needs one additional offset by MaxFrame to be in the correct range
                if (frame < 0)
                    frame += MaxFrame;
            }
            else if (frame < 0)
                frame = 0;
            SetFrame(frame);
        }

        /// <summary>
        /// Bypass the normal slow transition and jump the part immediately to this new state
        /// </summary>
        public void SetState(bool state)
        {
            SetFrame(state ? MaxFrame : 0);
        }

        /// <summary>
        /// Bypass the normal slow transition and jump the part immediately to this new state
        /// </summary>
        public void SetState(float state)
        {
            SetFrame(MathHelper.Clamp(state, 0f, 1f) * MaxFrame);
        }

        /// <summary>
        /// Smoothly changes the animation to a particular state between 0 and 1.
        /// </summary>
        public void UpdateState(float state, ElapsedTime elapsedTime)
        {
            var desiredKey = state * MaxFrame;

            if (Math.Abs(desiredKey - AnimationKey) > elapsedTime.ClockSeconds * Speed)
                SetFrameClamp(AnimationKey + Math.Sign(desiredKey - AnimationKey) * elapsedTime.ClockSeconds * Speed);
            else
                SetFrameClamp(desiredKey);
        }

        /// <summary>
        /// Updates an animated part that toggles between two states.
        /// </summary>
        public void UpdateState(bool state, ElapsedTime elapsedTime)
        {
            UpdateState(state ? 1f : 0f, elapsedTime);
        }

        /// <summary>
        /// Returns the animation key fraction (between 0 and 1)
        /// </summary>
        public float AnimationKeyFraction()
        {
            return AnimationKey / MaxFrame;
        }

        /// <summary>
        /// Updates an animated part that loops (e.g. running gear), changing by the given amount.
        /// </summary>
        public void UpdateLoop(float change)
        {
            SetFrameWrap(AnimationKey + change * Speed);
        }

        /// <summary>
        /// Updates an animated part that loops only when enabled (e.g. wipers).
        /// </summary>
        public void UpdateLoop(bool running, ElapsedTime elapsedTime)
        {
            if (running || (0 < AnimationKey && AnimationKey < MaxFrame))
            {
                var resultKey = AnimationKey + elapsedTime.ClockSeconds * Speed;
                if (!running && (resultKey < 0 || MaxFrame <= resultKey))
                    SetFrame(0);
                else
                    SetFrameWrap(resultKey);
            }
        }
    }
}
