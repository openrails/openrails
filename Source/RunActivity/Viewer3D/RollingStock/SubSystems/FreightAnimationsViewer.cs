// COPYRIGHT 2015 by the Open Rails project.
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

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems;
using ORTS.Common;

namespace Orts.Viewer3D.RollingStock.SubSystems
{
    public class FreightAnimationsViewer
    {
        public List<FreightAnimationViewer> Animations = new List<FreightAnimationViewer>();

        public FreightAnimationsViewer(Viewer viewer, MSTSWagon wagon, string wagonFolderSlash)
        {
            foreach (var animation in wagon.FreightAnimations.Animations)
            {
                if (animation.ShapeFileName != null)
                    Animations.Add(new FreightAnimationViewer(viewer, wagon, wagonFolderSlash, animation));
            }
        }

        public void Mark()
        {
            foreach (var animation in Animations)
            {
                animation.Mark();
            }
        }
    }

    public class FreightAnimationViewer
    {
        public FreightAnimation Animation;
        public AnimatedShape FreightShape;

        public FreightAnimationViewer(Viewer viewer, MSTSWagon wagon, string wagonFolderSlash, FreightAnimation animation)
        {
            Animation = animation;
            FreightShape = new AnimatedShape(viewer, wagonFolderSlash + animation.ShapeFileName + '\0' + wagonFolderSlash, new WorldPosition(wagon.WorldPosition), ShapeFlags.ShadowCaster);
            if (FreightShape.SharedShape.LodControls.Length > 0)
            {
                foreach (var lodControl in FreightShape.SharedShape.LodControls)
                {
                    if (lodControl.DistanceLevels.Length > 0)
                    {
                        foreach (var distanceLevel in lodControl.DistanceLevels)
                        {
                            if (distanceLevel.SubObjects.Length > 0
                                && distanceLevel.SubObjects[0].ShapePrimitives.Length > 0
                                && distanceLevel.SubObjects[0].ShapePrimitives[0].Hierarchy.Length > 0)
                            {
                                distanceLevel.SubObjects[0].ShapePrimitives[0].Hierarchy[0] = distanceLevel.SubObjects[0].ShapePrimitives[0].Hierarchy.Length;
                            }
                        }
                    }
                }
            }
            if (FreightShape.XNAMatrices.Length > 0 && animation is FreightAnimationStatic && (animation as FreightAnimationStatic).Flipped)
            {
                var flipper = Matrix.Identity;
                flipper.M11 = -1;
                flipper.M33 = -1;
                FreightShape.XNAMatrices[0] *= flipper;
            }
        }

        public void Mark()
        {
            FreightShape.Mark();
        }
    }
}
