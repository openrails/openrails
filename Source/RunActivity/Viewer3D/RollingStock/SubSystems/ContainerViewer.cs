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

using Microsoft.Xna.Framework;
using Orts.Simulation;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems;
using ORTS.Common;
using System.Collections.Generic;
using System.Linq;

namespace Orts.Viewer3D.RollingStock.SubSystems
{
    public class ContainersViewer
    {
        public Dictionary<Container, ContainerViewer> Containers = new Dictionary<Container, ContainerViewer>();
        List<Container> VisibleContainers = new List<Container>();
        readonly Viewer Viewer;

        public ContainersViewer(Viewer viewer, ContainerHandlingItem containerHandlingItem, string wagonFolderSlash)
        {
            foreach (var container in containerHandlingItem.Containers)
            {
                if (container.ShapeFileName != null)
                    Containers.Add(container, new ContainerViewer(viewer, containerHandlingItem, wagonFolderSlash, container));
            }
        }

        public ContainersViewer(Viewer viewer)
        {
            Viewer = viewer;
        }


        [CallOnThread("Updater")]
        public void LoadPrep()
        {
            var visibleContainers = new List<Container>();
            var removeDistance = Viewer.Settings.ViewingDistance * 1.5f;
            foreach (var container in Viewer.Simulator.ContainerManager.Containers)
                if (WorldLocation.ApproximateDistance(Viewer.Camera.CameraWorldLocation, container.WorldPosition.WorldLocation) < removeDistance)
                    visibleContainers.Add(container);
            VisibleContainers = visibleContainers;
        }

        public void Mark()
        {
            foreach (var container in Containers.Values)
            {
                container.Mark();
            }
        }

        [CallOnThread("Loader")]
        public void Load()
        {
            var cancellation = Viewer.LoaderProcess.CancellationToken;
            var visibleContainers = VisibleContainers;
            var containers = Containers;
            if (visibleContainers.Any(c => !containers.ContainsKey(c)) || containers.Keys.Any(c => !visibleContainers.Contains(c)))
            {
                var newContainers = new Dictionary<Container, ContainerViewer>();
                foreach (var container in visibleContainers)
                {
                    if (cancellation.IsCancellationRequested)
                        break;
                    if (containers.ContainsKey(container))
                        newContainers.Add(container, containers[container]);
                    else
                        newContainers.Add(container, LoadContainer(container));
                }
                Containers = newContainers;
            }
        }

        [CallOnThread("Loader")]
        ContainerViewer LoadContainer(Container container)
        {
            return new ContainerViewer(Viewer, container);
        }

        [CallOnThread("Updater")]
        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            var containers = Containers;
            foreach (var container in containers.Values)
                container.PrepareFrame(frame, elapsedTime);
        }
    }

    public class ContainerViewer
    {
        public Container Container;
        public AnimatedShape ContainerShape;
        public Viewer Viewer;

        public ContainerViewer(Viewer viewer, ContainerHandlingItem containerHandlingItem, string wagonFolderSlash, Container container)
        {
            Container = container;
            Viewer = viewer;
            ContainerShape = new AnimatedShape(viewer, container.BaseShapeFileFolderSlash + container.ShapeFileName + '\0' + container.BaseShapeFileFolderSlash, new WorldPosition(containerHandlingItem.ShapePosition), ShapeFlags.ShadowCaster);
            if (ContainerShape.SharedShape.LodControls.Length > 0)
            {
                foreach (var lodControl in ContainerShape.SharedShape.LodControls)
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
        }

        public ContainerViewer(Viewer viewer, Container container)
        {
            Container = container;
            Viewer = viewer;
            ContainerShape = new AnimatedShape(viewer, container.BaseShapeFileFolderSlash + container.ShapeFileName + '\0' + container.BaseShapeFileFolderSlash, new WorldPosition(container.WorldPosition), ShapeFlags.ShadowCaster);
            if (ContainerShape.SharedShape.LodControls.Length > 0)
            {
                foreach (var lodControl in ContainerShape.SharedShape.LodControls)
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
/*            if (ContainerShape.XNAMatrices.Length > 0 && animation is FreightAnimationDiscrete && (animation as FreightAnimationDiscrete).Flipped)
            {
                var flipper = Matrix.Identity;
                flipper.M11 = -1;
                flipper.M33 = -1;
                ContainerShape.XNAMatrices[0] *= flipper;
            }*/
        }

        public void Mark()
        {
            ContainerShape.Mark();
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            ContainerShape.Location.TileX = Container.WorldPosition.TileX;
            ContainerShape.Location.TileZ = Container.WorldPosition.TileZ;
            ContainerShape.Location.XNAMatrix = Container.WorldPosition.XNAMatrix;
            ContainerShape.PrepareFrame(frame, elapsedTime);
        }
    }
}
