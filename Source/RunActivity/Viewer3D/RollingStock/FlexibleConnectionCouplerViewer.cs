// ORTS Flexible Connections
//
// Purpose: Publish a complete rigid screw coupling with publisher-up orientation.
// Responsibilities: Apply Coupler admission and fixed-length joint construction, place ordered
//   assets, and use local publication state to control point-local NotCoupledShape visuals.
// Key components: FlexibleConnectionCouplerViewer.PrepareConnected publishes rigid pieces;
//   PrepareDisconnected handles the local disconnected asset.
// Related files: FlexibleConnectionCoupler.cs provides admission; FlexibleConnectionHook.cs
//   provides BuildRigidChain; FlexibleConnectionCatenary.cs supplies the geometric guide.
//   FlexibleConnectionHookViewer.cs provides shared placement and shape-retention services.
// Lifecycle: FlexibleConnectionViewer.cs delegates preparation; shared Loader/frame references
//   retain native shapes until retirement is safe. No coupling physics or slack is changed.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Orts.Simulation.RollingStocks;
using ORTS.Common;

namespace Orts.Viewer3D.RollingStock
{
    [Flags]
    internal enum FlexibleConnectionCouplerWarning
    {
        MissingPoint = 1, MixedFamilies = 2, MissingPublisherData = 4, Degenerate = 8,
        Slope = 16, Reach = 32, Guide = 64, Solver = 128, Orientation = 256,
        Placement = 512, Resource = 1024
    }

    internal sealed class FlexibleConnectionCouplerBinding
    {
        internal readonly FlexibleConnectionMapping Mapping;
        internal readonly MSTSWagon Remote;
        internal readonly FlexibleConnectionEnd End;
        internal readonly FlexibleConnectionPoint Point;
        internal readonly FlexibleConnectionCouplerBinding Next;
        private FlexibleConnectionCouplerWarning Warnings;

        internal FlexibleConnectionCouplerBinding(FlexibleConnectionMapping mapping, MSTSWagon remote,
            FlexibleConnectionEnd end, FlexibleConnectionPoint point, FlexibleConnectionCouplerBinding next)
        { Mapping = mapping; Remote = remote; End = end; Point = point; Next = next; }

        internal void Warn(MSTSWagon local, FlexibleConnectionCouplerWarning cause, string detail)
        {
            if ((Warnings & cause) != 0) return;
            Warnings |= cause;
            Trace.TraceWarning("Flexible connection Coupler {0} ({1}) {2}:{3}, Profile '{4}' -> {5} ({6}) {7}:{8}: {9}; Connected not published.",
                local.CarID, local.WagFilePath, Mapping.Point.End, Mapping.LocalPoint, Mapping.Point.Profile.Name,
                Remote.CarID, Remote.WagFilePath, End, Mapping.RemotePoint, detail);
        }
    }

    internal sealed class FlexibleConnectionCouplerViewer
    {
        private readonly Viewer Viewer;
        private readonly FlexibleConnectionHookResourceManager Manager;
        private readonly FlexibleConnectionHookOwner Owner;
        private readonly FlexibleConnectionPoint[] Points;
        private readonly FlexibleConnectionHookResource[] DisconnectedResources;
        private readonly bool[] DisconnectedWarnings;
        private readonly bool[] PublishedLocally;
        private readonly Dictionary<FlexibleConnectionLocalProfile, FlexibleConnectionHookResource[]> ProfileResources =
            new Dictionary<FlexibleConnectionLocalProfile, FlexibleConnectionHookResource[]>();
        private readonly Dictionary<FlexibleConnectionMapping, FlexibleConnectionCouplerBinding> Bindings =
            new Dictionary<FlexibleConnectionMapping, FlexibleConnectionCouplerBinding>();
        private readonly WorldPosition[] Placements = new WorldPosition[FlexibleConnectionCouplerConstants.MaximumCouplerLinks];
        private readonly WorldPosition DisconnectedPlacement = new WorldPosition();
        private readonly double[] Lengths = new double[FlexibleConnectionCouplerConstants.MaximumCouplerLinks];
        private readonly double[] SuffixMin = new double[FlexibleConnectionCouplerConstants.MaximumCouplerLinks + 1];
        private readonly double[] SuffixMax = new double[FlexibleConnectionCouplerConstants.MaximumCouplerLinks + 1];
        private readonly FlexibleConnectionHookVector[] Guide = new FlexibleConnectionHookVector[FlexibleConnectionCouplerConstants.MaximumCouplerLinks + 1];
        private readonly FlexibleConnectionHookVector[] Joints = new FlexibleConnectionHookVector[FlexibleConnectionCouplerConstants.MaximumCouplerLinks + 1];
        private readonly FlexibleConnectionCatenaryResult Catenary = new FlexibleConnectionCatenaryResult();
        private readonly Vector3[] CatenaryPoints = new Vector3[FlexibleConnectionCouplerConstants.MaximumCouplerLinks + 1];

        internal FlexibleConnectionCouplerViewer(Viewer viewer, FlexibleConnectionConfig config, string folder)
        {
            Viewer = viewer;
            Manager = viewer.RenderProcess.FlexibleConnections.Hooks;
            Owner = Manager.Register(viewer, folder, "Coupler");
            var points = new List<FlexibleConnectionPoint>();
            foreach (var point in config.FrontConnectionPoints.Values)
                if (point.Profile.Family == FlexibleConnectionFamily.Coupler) points.Add(point);
            foreach (var point in config.RearConnectionPoints.Values)
                if (point.Profile.Family == FlexibleConnectionFamily.Coupler) points.Add(point);
            Points = points.ToArray();
            DisconnectedResources = new FlexibleConnectionHookResource[Points.Length];
            DisconnectedWarnings = new bool[Points.Length];
            PublishedLocally = new bool[Points.Length];
            for (int i = 0; i < Placements.Length; i++) Placements[i] = new WorldPosition();
        }

        internal void ResetPublication() { Array.Clear(PublishedLocally, 0, PublishedLocally.Length); }
        internal void RequestRetirement() { Manager.Retire(Owner); }

        internal static FlexibleConnectionCouplerBinding GetBinding(
            Dictionary<FlexibleConnectionMapping, FlexibleConnectionCouplerBinding> bindings,
            FlexibleConnectionMapping mapping, MSTSWagon remote, FlexibleConnectionEnd end, FlexibleConnectionPoint point)
        {
            FlexibleConnectionCouplerBinding head;
            bindings.TryGetValue(mapping, out head);
            for (var binding = head; binding != null; binding = binding.Next)
                if (ReferenceEquals(binding.Remote, remote) && binding.End == end && ReferenceEquals(binding.Point, point)) return binding;
            var created = new FlexibleConnectionCouplerBinding(mapping, remote, end, point, head);
            bindings[mapping] = created;
            return created;
        }

        internal bool PrepareConnected(RenderFrame frame, FlexibleConnectionMapping mapping, MSTSWagon local,
            MSTSWagon remote, FlexibleConnectionEnd end, FlexibleConnectionPoint remotePoint)
        {
            var binding = GetBinding(Bindings, mapping, remote, end, remotePoint);
            if (remotePoint == null)
            { binding.Warn(local, FlexibleConnectionCouplerWarning.MissingPoint, "remote point/profile is undefined or invalid"); return false; }
            if (mapping.Point.Profile.Family != FlexibleConnectionFamily.Coupler || remotePoint.Profile.Family != FlexibleConnectionFamily.Coupler)
            { binding.Warn(local, FlexibleConnectionCouplerWarning.MixedFamilies, "Coupler requires Coupler at both ends"); return false; }
            var config = mapping.Point.Profile.Coupler;
            if (!config.CanPublish)
            { binding.Warn(local, FlexibleConnectionCouplerWarning.MissingPublisherData, "publisher needs Links and complete CouplerShapes; receiver-only Profile cannot publish"); return false; }

            Vector3 a = mapping.Point.Position, b = remotePoint.Position;
            a.Z = -a.Z; b.Z = -b.Z;
            a = Vector3.Transform(a, local.WorldPosition.XNAMatrix);
            b = Vector3.Transform(b, remote.WorldPosition.XNAMatrix);
            double dx = (double)b.X - a.X + ((double)remote.WorldPosition.TileX - local.WorldPosition.TileX) * WorldPosition.TileSize;
            double dz = (double)b.Z - a.Z - ((double)remote.WorldPosition.TileZ - local.WorldPosition.TileZ) * WorldPosition.TileSize;
            double dy = (double)b.Y - a.Y;
            double horizontal = Math.Sqrt(dx * dx + dz * dz);
            var status = FlexibleConnectionCouplerGeometry.Prepare(horizontal, dy, config, Lengths, SuffixMin, SuffixMax);
            if (status != FlexibleConnectionHookGeometryStatus.Success)
            { WarnGeometry(binding, local, status); return false; }

            int count = config.Links.Value;
            FlexibleConnectionHookResource[] resources;
            if (!ProfileResources.TryGetValue(mapping.Point.Profile, out resources))
            {
                resources = new FlexibleConnectionHookResource[count];
                for (int i = 0; i < count; i++) resources[i] = Manager.Request(Owner, config.Shapes[i].FileName);
                ProfileResources.Add(mapping.Point.Profile, resources);
            }
            bool ready = true;
            for (int i = 0; i < count; i++)
            {
                SharedShape shape;
                if (!Manager.TryGet(resources[i], out shape))
                {
                    ready = false;
                    if (Manager.Failed(resources[i]))
                        binding.Warn(local, FlexibleConnectionCouplerWarning.Resource, "required publisher shape '" + config.Shapes[i].FileName + "' is unusable");
                }
            }
            if (!ready) return false; // Pending loads are normal, and never partially publish.

            if (!PrepareGuide(horizontal, dy, count, SuffixMax[0]))
            { binding.Warn(local, FlexibleConnectionCouplerWarning.Guide, "catenary guide failed: " + Catenary.Cause); return false; }
            status = FlexibleConnectionHookGeometry.BuildRigidChain(horizontal, dy, count, Lengths, SuffixMin, SuffixMax, Guide, Joints);
            if (status != FlexibleConnectionHookGeometryStatus.Success)
            { WarnGeometry(binding, local, status); return false; }

            var publisherUp = local.WorldPosition.XNAMatrix.Up;
            if (!FlexibleConnectionHookViewer.Normalize(ref publisherUp))
            { binding.Warn(local, FlexibleConnectionCouplerWarning.Orientation, "publisher-up is invalid"); return false; }
            double hx = dx / horizontal, hz = dz / horizontal;
            Vector3 previousEnd = a;
            for (int i = 0; i < count; i++)
            {
                var shapeConfig = config.Shapes[i];
                Vector3 start = FlexibleConnectionHookViewer.WorldJoint(a, hx, hz, Joints[i]);
                Vector3 finish = FlexibleConnectionHookViewer.WorldJoint(a, hx, hz, Joints[i + 1]);
                double sx = Joints[i + 1].X - Joints[i].X, sy = Joints[i + 1].Y - Joints[i].Y;
                var direction = new Vector3((float)(hx * sx), (float)sy, (float)(hz * sx));
                Vector3 transverse;
                if (!TryTransverse(publisherUp, ref direction, out transverse))
                { binding.Warn(local, FlexibleConnectionCouplerWarning.Orientation, "segment is parallel or nearly parallel to publisher-up, or non-finite"); return false; }
                Matrix placement;
                if (!FlexibleConnectionHookViewer.TryRigidPlacement(shapeConfig.P1, shapeConfig.P2, start, finish,
                    direction, transverse, false, FlexibleConnectionCouplerConstants.CouplerGeometryTolerance, out placement))
                { binding.Warn(local, FlexibleConnectionCouplerWarning.Placement, "rigid placement or endpoint closure is invalid"); return false; }
                Vector3 p1 = shapeConfig.P1, p2 = shapeConfig.P2;
                p1.Z = -p1.Z; p2.Z = -p2.Z;
                if (!FlexibleConnectionHookViewer.Near(previousEnd, Vector3.Transform(p1, placement), FlexibleConnectionCouplerConstants.CouplerGeometryTolerance))
                { binding.Warn(local, FlexibleConnectionCouplerWarning.Placement, "represented joints do not coincide"); return false; }
                previousEnd = Vector3.Transform(p2, placement);
                var location = Placements[i];
                location.TileX = local.WorldPosition.TileX;
                location.TileZ = local.WorldPosition.TileZ;
                location.XNAMatrix = placement;
                if (!FlexibleConnectionHookViewer.ValidRenderPlacement(Viewer, resources[i], location))
                { binding.Warn(local, FlexibleConnectionCouplerWarning.Placement, "shape hierarchy placement is non-finite"); return false; }
            }
            if (!FlexibleConnectionHookViewer.Near(previousEnd, new Vector3((float)(a.X + dx), (float)(a.Y + dy), (float)(a.Z + dz)),
                FlexibleConnectionCouplerConstants.CouplerGeometryTolerance))
            { binding.Warn(local, FlexibleConnectionCouplerWarning.Placement, "represented chain does not close at the remote anchor"); return false; }

            // All preflight checks precede admission and the first native publication.
            if (!Manager.Retain(Owner, frame)) return false;
            for (int i = 0; i < count; i++) resources[i].Shape.PrepareFrame(frame, Placements[i], ShapeFlags.None);
            // Only this publisher's disconnected screw coupling is suppressed. Never use Connected here.
            for (int i = 0; i < Points.Length; i++)
                if (ReferenceEquals(Points[i], mapping.Point)) PublishedLocally[i] = true;
            return true;
        }

        private bool PrepareGuide(double horizontal, double height, int count, double total)
        {
            Guide[0] = new FlexibleConnectionHookVector(0, 0);
            bool straight = Math.Sqrt(horizontal * horizontal + height * height) == total;
            if (!straight)
            {
                FlexibleConnectionCatenary.Calculate(Vector3.Zero, new Vector3((float)horizontal, (float)height, 0),
                    total, count, Catenary, CatenaryPoints);
                // Float endpoints can consume the last representable slack. Rigid reach was already checked in double.
                straight = Catenary.Status == FlexibleConnectionCatenaryStatus.NoSlack;
                if (!straight && Catenary.Status != FlexibleConnectionCatenaryStatus.Success) return false;
            }
            var context = Catenary.ArcContext;
            double arc = 0;
            for (int i = 0; i < count; i++)
            {
                arc += Lengths[i];
                if (straight)
                {
                    double fraction = i == count - 1 ? 1 : Math.Min(arc, total) / total;
                    Guide[i + 1] = new FlexibleConnectionHookVector(horizontal * fraction, height * fraction);
                }
                else
                {
                    Vector3 sample;
                    if (!FlexibleConnectionCatenary.TryEvaluateAtArcLength(ref context, i == count - 1 ? total : Math.Min(arc, total), out sample)) return false;
                    Guide[i + 1] = new FlexibleConnectionHookVector(sample.X, sample.Y);
                }
            }
            return true;
        }

        private static bool TryTransverse(Vector3 up, ref Vector3 direction, out Vector3 transverse)
        {
            transverse = Vector3.Zero;
            if (!FlexibleConnectionHookViewer.Normalize(ref direction)) return false;
            transverse = Vector3.Cross(up, direction);
            double magnitudeSquared = (double)transverse.X * transverse.X + (double)transverse.Y * transverse.Y + (double)transverse.Z * transverse.Z;
            double minimum = FlexibleConnectionCouplerConstants.MinimumCouplerOrientationCrossMagnitude;
            return FlexibleConnectionHookGeometry.Finite(magnitudeSquared) && magnitudeSquared > minimum * minimum &&
                FlexibleConnectionHookViewer.Normalize(ref transverse);
        }

        private static void WarnGeometry(FlexibleConnectionCouplerBinding binding, MSTSWagon car, FlexibleConnectionHookGeometryStatus status)
        {
            if (status == FlexibleConnectionHookGeometryStatus.OutOfReach)
                binding.Warn(car, FlexibleConnectionCouplerWarning.Reach, "anchor distance is outside strict rigid reach; check asset lengths and native slack");
            else if (status == FlexibleConnectionHookGeometryStatus.ExcessiveSlope)
                binding.Warn(car, FlexibleConnectionCouplerWarning.Slope, "anchor slope exceeds MaximumCouplerSlopeAngle");
            else if (status == FlexibleConnectionHookGeometryStatus.Degenerate)
                binding.Warn(car, FlexibleConnectionCouplerWarning.Degenerate, "anchor separation is degenerate");
            else binding.Warn(car, FlexibleConnectionCouplerWarning.Solver, "non-finite input or rigid-chain numerical failure");
        }

        internal void PrepareDisconnected(RenderFrame frame, WorldPosition vehicle, FlexibleConnectionEnd end)
        {
            for (int i = 0; i < Points.Length; i++)
            {
                var point = Points[i];
                if (point.End != end || PublishedLocally[i] || point.NotCoupledShapeName == null) continue;
                var resource = DisconnectedResources[i];
                if (resource == null) DisconnectedResources[i] = resource = Manager.Request(Owner, point.NotCoupledShapeName);
                SharedShape shape;
                if (!Manager.TryGet(resource, out shape))
                {
                    if (Manager.Failed(resource)) WarnDisconnected(i, "shape is unusable");
                    continue;
                }
                // Full vehicle coordinates: no ConnectionPoint offset or automatic Front rotation.
                DisconnectedPlacement.TileX = vehicle.TileX;
                DisconnectedPlacement.TileZ = vehicle.TileZ;
                DisconnectedPlacement.XNAMatrix = vehicle.XNAMatrix;
                if (!FlexibleConnectionHookViewer.ValidRenderPlacement(Viewer, resource, DisconnectedPlacement))
                { WarnDisconnected(i, "placement is non-finite"); continue; }
                if (Manager.Retain(Owner, frame)) shape.PrepareFrame(frame, DisconnectedPlacement, ShapeFlags.None);
            }
        }

        private void WarnDisconnected(int index, string detail)
        {
            if (DisconnectedWarnings[index]) return;
            DisconnectedWarnings[index] = true;
            var point = Points[index];
            Trace.TraceWarning("Flexible connection Coupler vehicle folder '{0}', viewer {1:X8}, {2}:{3}, Profile '{4}': NotCoupledShape '{5}' {6}; not rendered.",
                Owner.Folder, System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this), point.End, point.ID,
                point.Profile.Name, point.NotCoupledShapeName, detail);
        }
    }
}
