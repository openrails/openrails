// ORTS Flexible Connections
//
// Purpose: Publish Hook rigid-chain visuals and retain native shape resources safely.
// Responsibilities: Resolve Hook assets, use a catenary guide to place fixed-length pieces,
//   apply ChainParity, and prepare point-local NotHookedShape visuals when disconnected.
// Key components: FlexibleConnectionHookViewer prepares visuals;
//   FlexibleConnectionHookResourceManager serves both Hook and Coupler owners.
// Related files: FlexibleConnectionViewer.cs delegates publication and services the manager;
//   FlexibleConnectionHook.cs constructs joints; FlexibleConnectionCatenary.cs supplies the guide;
//   RenderFrame.cs retains FlexibleConnectionHookFrameReference tokens for both rigid families.
// Lifecycle: Loader loads SharedShape assets; Updater submits placements; Draw consumes them.
//   Retired owners remain marked while frames or pending loads still reference their resources.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Orts.Simulation.RollingStocks;
using ORTS.Common;

namespace Orts.Viewer3D.RollingStock
{
    // ---------------------------------------------------------------------
    // Stable runtime warning contexts; never format successful-frame messages.
    // ---------------------------------------------------------------------
    [Flags]
    internal enum FlexibleConnectionHookWarning
    {
        None = 0, MissingPoint = 1, MixedFamilies = 2, MissingPublisherData = 4,
        Degenerate = 8, Slope = 16, Length = 32, Reach = 64,
        Guide = 128, Solver = 256, Placement = 512, Resource = 1024
    }

    internal sealed class FlexibleConnectionHookBinding
    {
        internal readonly FlexibleConnectionMapping Mapping;
        internal readonly MSTSWagon Remote;
        internal readonly FlexibleConnectionEnd End;
        internal readonly FlexibleConnectionPoint Point;
        internal FlexibleConnectionHookBinding Next;
        internal FlexibleConnectionHookWarning Warnings;

        internal FlexibleConnectionHookBinding(FlexibleConnectionMapping mapping, MSTSWagon remote,
            FlexibleConnectionEnd end, FlexibleConnectionPoint point, FlexibleConnectionHookBinding next)
        { Mapping = mapping; Remote = remote; End = end; Point = point; Next = next; }

        internal void Warn(MSTSWagon local, FlexibleConnectionHookWarning cause, string detail)
        {
            if ((Warnings & cause) != 0) return;
            Warnings |= cause;
            Trace.TraceWarning("Flexible connection Hook {0} ({1}) {2}:{3} -> {4} ({5}) {6}:{7}: {8}; Connected not published.",
                local.CarID, local.WagFilePath, Mapping.Point.End, Mapping.LocalPoint,
                Remote.CarID, Remote.WagFilePath, End, Mapping.RemotePoint, detail);
        }
    }

    // ---------------------------------------------------------------------
    // Per-viewer reusable geometry and render plan. No arrays reach Draw.
    // ---------------------------------------------------------------------
    internal sealed class FlexibleConnectionHookRenderPlan
    {
        internal readonly WorldPosition[] Placements = new WorldPosition[FlexibleConnectionHookConstants.MaximumLinks];
        internal readonly FlexibleConnectionHookResource[] Resources = new FlexibleConnectionHookResource[FlexibleConnectionHookConstants.MaximumLinks];
        internal int Count;

        internal FlexibleConnectionHookRenderPlan()
        {
            for (int i = 0; i < Placements.Length; i++) Placements[i] = new WorldPosition();
        }
    }

    internal sealed class FlexibleConnectionHookViewer
    {
        private readonly Viewer Viewer;
        private readonly FlexibleConnectionHookResourceManager Manager;
        private readonly FlexibleConnectionHookOwner Owner;
        private readonly FlexibleConnectionPoint[] Points;
        private readonly FlexibleConnectionHookResource[] DisconnectedResources;
        private readonly bool[] DisconnectedWarnings;
        private readonly Dictionary<FlexibleConnectionLocalProfile, FlexibleConnectionHookResource[]> ProfileResources =
            new Dictionary<FlexibleConnectionLocalProfile, FlexibleConnectionHookResource[]>();
        private readonly Dictionary<FlexibleConnectionMapping, FlexibleConnectionHookBinding> Bindings =
            new Dictionary<FlexibleConnectionMapping, FlexibleConnectionHookBinding>();
        private readonly FlexibleConnectionHookRenderPlan Plan = new FlexibleConnectionHookRenderPlan();
        private readonly WorldPosition DisconnectedPlacement = new WorldPosition();
        private readonly double[] Lengths = new double[FlexibleConnectionHookConstants.MaximumLinks];
        private readonly double[] SuffixMin = new double[FlexibleConnectionHookConstants.MaximumLinks + 1];
        private readonly double[] SuffixMax = new double[FlexibleConnectionHookConstants.MaximumLinks + 1];
        private readonly FlexibleConnectionHookVector[] Guide = new FlexibleConnectionHookVector[FlexibleConnectionHookConstants.MaximumLinks + 1];
        private readonly FlexibleConnectionHookVector[] Joints = new FlexibleConnectionHookVector[FlexibleConnectionHookConstants.MaximumLinks + 1];
        private readonly FlexibleConnectionCatenaryResult Catenary = new FlexibleConnectionCatenaryResult();
        private readonly Vector3[] CatenaryPoints = new Vector3[FlexibleConnectionHookConstants.MaximumLinks + 1];

        internal FlexibleConnectionHookViewer(Viewer viewer, FlexibleConnectionConfig config, string folder)
        {
            Viewer = viewer;
            Manager = viewer.RenderProcess.FlexibleConnections.Hooks;
            Owner = Manager.Register(viewer, folder);
            var points = new List<FlexibleConnectionPoint>();
            foreach (var point in config.FrontConnectionPoints.Values) if (point.Profile.Hook != null) points.Add(point);
            foreach (var point in config.RearConnectionPoints.Values) if (point.Profile.Hook != null) points.Add(point);
            Points = points.ToArray();
            DisconnectedResources = new FlexibleConnectionHookResource[Points.Length];
            DisconnectedWarnings = new bool[Points.Length];
        }

        internal static FlexibleConnectionHookBinding GetBinding(
            Dictionary<FlexibleConnectionMapping, FlexibleConnectionHookBinding> bindings,
            FlexibleConnectionMapping mapping, MSTSWagon remote, FlexibleConnectionEnd end, FlexibleConnectionPoint point)
        {
            FlexibleConnectionHookBinding head;
            bindings.TryGetValue(mapping, out head);
            for (var binding = head; binding != null; binding = binding.Next)
                if (ReferenceEquals(binding.Remote, remote) && binding.End == end && ReferenceEquals(binding.Point, point)) return binding;
            var created = new FlexibleConnectionHookBinding(mapping, remote, end, point, head);
            bindings[mapping] = created;
            return created;
        }

        internal bool PrepareConnected(RenderFrame frame, FlexibleConnectionMapping mapping, MSTSWagon local,
            MSTSWagon remote, FlexibleConnectionEnd end, FlexibleConnectionPoint remotePoint)
        {
            Plan.Count = 0;
            var binding = GetBinding(Bindings, mapping, remote, end, remotePoint);
            if (remotePoint == null)
            { binding.Warn(local, FlexibleConnectionHookWarning.MissingPoint, "remote point/profile is undefined or invalid"); return false; }
            if (mapping.Point.Profile.Hook == null || remotePoint.Profile.Hook == null)
            { binding.Warn(local, FlexibleConnectionHookWarning.MixedFamilies, "Hook/Hose families cannot be connected"); return false; }
            var config = mapping.Point.Profile.Hook;
            if (!config.CanPublish)
            { binding.Warn(local, FlexibleConnectionHookWarning.MissingPublisherData, "publisher needs Links, LinkShape and HookShape; receiver-only Profile cannot publish"); return false; }

            // Same tile convention as the frozen Hose path. SharedShape applies
            // the camera offset later; it must not be applied twice here.
            Vector3 a = mapping.Point.Position, b = remotePoint.Position;
            a.Z = -a.Z; b.Z = -b.Z;
            a = Vector3.Transform(a, local.WorldPosition.XNAMatrix);
            b = Vector3.Transform(b, remote.WorldPosition.XNAMatrix);
            double dx = (double)b.X - a.X + ((double)remote.WorldPosition.TileX - local.WorldPosition.TileX) * WorldPosition.TileSize;
            double dz = (double)b.Z - a.Z - ((double)remote.WorldPosition.TileZ - local.WorldPosition.TileZ) * WorldPosition.TileSize;
            double dy = (double)b.Y - a.Y;
            double horizontal = Math.Sqrt(dx * dx + dz * dz);
            var status = FlexibleConnectionHookGeometry.Prepare(horizontal, dy, config, Lengths, SuffixMin, SuffixMax);
            if (status != FlexibleConnectionHookGeometryStatus.Success)
            { WarnGeometry(binding, local, status); return false; }

            FlexibleConnectionHookResource[] resources;
            if (!ProfileResources.TryGetValue(mapping.Point.Profile, out resources))
            {
                resources = new[] { Manager.Request(Owner, config.LinkShape.FileName), Manager.Request(Owner, config.HookShape.FileName) };
                ProfileResources.Add(mapping.Point.Profile, resources);
            }
            SharedShape link, hook;
            if (!Manager.TryGet(resources[0], out link) || !Manager.TryGet(resources[1], out hook))
            {
                if (Manager.Failed(resources[0]) || Manager.Failed(resources[1]))
                    binding.Warn(local, FlexibleConnectionHookWarning.Resource, "required publisher shape is unusable");
                return false;
            }

            int count = config.Links.Value;
            double total = SuffixMax[0];
            // Evaluate the guide close to zero. Float guide samples express only
            // a preference; the double rigid construction enforces final lengths.
            FlexibleConnectionCatenary.Calculate(Vector3.Zero, new Vector3((float)horizontal, (float)dy, 0),
                total, count, Catenary, CatenaryPoints);
            if (Catenary.Status != FlexibleConnectionCatenaryStatus.Success)
            { binding.Warn(local, FlexibleConnectionHookWarning.Guide, Catenary.Cause); return false; }
            var context = Catenary.ArcContext;
            double arc = 0;
            Guide[0] = new FlexibleConnectionHookVector(0, 0);
            for (int i = 0; i < count; i++)
            {
                arc += Lengths[i];
                Vector3 sample;
                // The endpoint uses the exact context length, avoiding summation-order overshoot.
                if (!FlexibleConnectionCatenary.TryEvaluateAtArcLength(ref context, i == count - 1 ? total : Math.Min(arc, total), out sample))
                { binding.Warn(local, FlexibleConnectionHookWarning.Guide, "arc guide evaluation failed"); return false; }
                Guide[i + 1] = new FlexibleConnectionHookVector(sample.X, sample.Y);
            }
            status = FlexibleConnectionHookGeometry.Build(horizontal, dy, count, Lengths, SuffixMin, SuffixMax, Guide, Joints);
            if (status != FlexibleConnectionHookGeometryStatus.Success)
            { WarnGeometry(binding, local, status); return false; }

            double hx = dx / horizontal, hz = dz / horizontal;
            var normal = new Vector3((float)-hz, 0, (float)hx);
            Vector3 previousEnd = a;
            for (int i = 0; i < count; i++)
            {
                var shapeConfig = i == count / 2 ? config.HookShape : config.LinkShape;
                var resource = resources[i == count / 2 ? 1 : 0];
                Vector3 start = WorldJoint(a, hx, hz, Joints[i]);
                Vector3 finish = WorldJoint(a, hx, hz, Joints[i + 1]);
                // Direction is computed from relative joints, not a subtraction
                // of rounded world coordinates of a short link.
                double sx = Joints[i + 1].X - Joints[i].X, sy = Joints[i + 1].Y - Joints[i].Y;
                var direction = new Vector3((float)(hx * sx), (float)sy, (float)(hz * sx));
                Matrix placement;
                // ORTS Flexible Connections
                // Purpose: Apply ChainParity to every Hook piece, including the central HookShape.
                if (!TryPlacement(shapeConfig, start, finish, direction, normal, ((i + config.ChainParity) & 1) != 0, out placement))
                { binding.Warn(local, FlexibleConnectionHookWarning.Placement, "rigid placement or endpoint closure is invalid"); return false; }
                Vector3 p1 = shapeConfig.P1, p2 = shapeConfig.P2;
                p1.Z = -p1.Z; p2.Z = -p2.Z;
                // Check adjacent represented endpoints directly, not just each
                // endpoint against its ideal joint (which could double the gap).
                if (!Near(previousEnd, Vector3.Transform(p1, placement)))
                { binding.Warn(local, FlexibleConnectionHookWarning.Placement, "represented joints do not coincide"); return false; }
                previousEnd = Vector3.Transform(p2, placement);
                var location = Plan.Placements[i];
                location.TileX = local.WorldPosition.TileX;
                location.TileZ = local.WorldPosition.TileZ;
                location.XNAMatrix = placement;
                if (!ValidRenderPlacement(resource, location))
                { binding.Warn(local, FlexibleConnectionHookWarning.Placement, "shape hierarchy placement is non-finite"); return false; }
                Plan.Resources[i] = resource;
            }
            if (!Near(previousEnd, new Vector3((float)(a.X + dx), (float)(a.Y + dy), (float)(a.Z + dz))))
            { binding.Warn(local, FlexibleConnectionHookWarning.Placement, "represented chain does not close at the remote anchor"); return false; }
            Plan.Count = count;
            // Commit admission and retain assets before the first primitive. A
            // later retirement must serve this already accepted frame.
            if (!Manager.Retain(Owner, frame)) return false;
            for (int i = 0; i < Plan.Count; i++)
                Plan.Resources[i].Shape.PrepareFrame(frame, Plan.Placements[i], ShapeFlags.None);
            return true;
        }

        internal void PrepareDisconnected(RenderFrame frame, WorldPosition vehicle, FlexibleConnectionEnd end, HashSet<string> connected)
        {
            for (int i = 0; i < Points.Length; i++)
            {
                var point = Points[i];
                if (point.End != end || connected.Contains(point.ID) || point.NotHookedShapeName == null) continue;
                var resource = DisconnectedResources[i];
                if (resource == null) DisconnectedResources[i] = resource = Manager.Request(Owner, point.NotHookedShapeName);
                SharedShape shape;
                if (!Manager.TryGet(resource, out shape))
                {
                    if (Manager.Failed(resource)) WarnDisconnected(i, "shape is unusable");
                    continue;
                }
                // Authored in full vehicle-local coordinates: no point offset,
                // no automatic Front rotation, and no private camera offset.
                DisconnectedPlacement.TileX = vehicle.TileX;
                DisconnectedPlacement.TileZ = vehicle.TileZ;
                DisconnectedPlacement.XNAMatrix = vehicle.XNAMatrix;
                if (!ValidRenderPlacement(resource, DisconnectedPlacement))
                { WarnDisconnected(i, "placement is non-finite"); continue; }
                if (Manager.Retain(Owner, frame)) shape.PrepareFrame(frame, DisconnectedPlacement, ShapeFlags.None);
            }
        }

        private void WarnDisconnected(int index, string detail)
        {
            if (DisconnectedWarnings[index]) return;
            DisconnectedWarnings[index] = true;
            var point = Points[index];
            Trace.TraceWarning("Flexible connection Hook vehicle folder '{0}', viewer {1:X8}, {2}:{3}: NotHookedShape '{4}' {5}; not rendered.",
                Owner.Folder, System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this), point.End, point.ID, point.NotHookedShapeName, detail);
        }

        internal void RequestRetirement()
        {
            Manager.Retire(Owner);
            // Updater may still finish its current frame. Its private dictionaries
            // are not cleared by Loader; they die with this retired viewer.
        }

        private static void WarnGeometry(FlexibleConnectionHookBinding binding, MSTSWagon car, FlexibleConnectionHookGeometryStatus status)
        {
            switch (status)
            {
                case FlexibleConnectionHookGeometryStatus.ExcessiveSlope:
                    binding.Warn(car, FlexibleConnectionHookWarning.Slope, "anchor slope exceeds MaximumSlopeAngle"); break;
                case FlexibleConnectionHookGeometryStatus.InsufficientLength:
                    binding.Warn(car, FlexibleConnectionHookWarning.Length, "chain lacks the required length margin; increase Links"); break;
                case FlexibleConnectionHookGeometryStatus.OutOfReach:
                    binding.Warn(car, FlexibleConnectionHookWarning.Reach, "anchor distance is outside rigid-chain reach; check Links and asset lengths"); break;
                case FlexibleConnectionHookGeometryStatus.Degenerate:
                    binding.Warn(car, FlexibleConnectionHookWarning.Degenerate, "anchor separation is degenerate"); break;
                default:
                    binding.Warn(car, FlexibleConnectionHookWarning.Solver, "non-finite input or rigid-chain numerical failure"); break;
            }
        }

        // ORTS Flexible Connections
        // Purpose: Share local-plane to world conversion without changing Hook arithmetic.
        internal static Vector3 WorldJoint(Vector3 origin, double hx, double hz, FlexibleConnectionHookVector joint)
        { return new Vector3((float)(origin.X + hx * joint.X), (float)(origin.Y + joint.Y), (float)(origin.Z + hz * joint.X)); }

        private static Matrix Basis(Vector3 x, Vector3 y, Vector3 z)
        { return new Matrix(x.X, x.Y, x.Z, 0, y.X, y.Y, y.Z, 0, z.X, z.Y, z.Z, 0, 0, 0, 0, 1); }

        private static bool TryPlacement(FlexibleConnectionHookShapeConfig config, Vector3 start, Vector3 finish,
            Vector3 direction, Vector3 normal, bool quarterTurn, out Matrix placement)
        {
            // ORTS Flexible Connections
            // Purpose: Hook retains its metadata, tolerance, normal and quarter-turn policy.
            return TryRigidPlacement(config.P1, config.P2, start, finish, direction, normal, quarterTurn,
                FlexibleConnectionHookConstants.GeometryTolerance, out placement);
        }

        // ORTS Flexible Connections
        // Purpose: Share rigid placement only; callers supply their own transverse axis and tolerance.
        internal static bool TryRigidPlacement(Vector3 p1, Vector3 p2, Vector3 start, Vector3 finish,
            Vector3 direction, Vector3 normal, bool quarterTurn, double tolerance, out Matrix placement)
        {
            placement = Matrix.Identity;
            p1.Z = -p1.Z; p2.Z = -p2.Z;
            var sourceZ = p2 - p1;
            if (!Normalize(ref sourceZ) || !Normalize(ref direction) || !Normalize(ref normal)) return false;
            var sourceX = Vector3.UnitX - sourceZ * Vector3.Dot(Vector3.UnitX, sourceZ);
            if (!Normalize(ref sourceX)) return false;
            var sourceY = Vector3.Cross(sourceZ, sourceX);
            var targetX = normal;
            var targetY = Vector3.Cross(direction, targetX);
            if (!Normalize(ref targetY)) return false;
            if (quarterTurn) { targetX = targetY; targetY = -normal; }
            placement = Matrix.Transpose(Basis(sourceX, sourceY, sourceZ)) * Basis(targetX, targetY, direction);
            placement.Translation = start - Vector3.TransformNormal(p1, placement);
            if (!Finite(placement)) return false;
            // Float matrix orthogonality uses a dimensionless roundoff allowance,
            // not the content tolerance in metres.
            const double rotationError = 128 * 1.1920928955078125e-7;
            var x = new Vector3(placement.M11, placement.M12, placement.M13);
            var y = new Vector3(placement.M21, placement.M22, placement.M23);
            var z = new Vector3(placement.M31, placement.M32, placement.M33);
            if (Math.Abs(x.LengthSquared() - 1) > rotationError || Math.Abs(y.LengthSquared() - 1) > rotationError ||
                Math.Abs(z.LengthSquared() - 1) > rotationError || Math.Abs(Vector3.Dot(x, y)) > rotationError ||
                Math.Abs(Vector3.Dot(x, z)) > rotationError || Math.Abs(Vector3.Dot(y, z)) > rotationError ||
                Vector3.Dot(Vector3.Cross(x, y), z) <= 0) return false;
            return Near(Vector3.Transform(p1, placement), start, tolerance) && Near(Vector3.Transform(p2, placement), finish, tolerance);
        }

        private bool ValidRenderPlacement(FlexibleConnectionHookResource resource, WorldPosition location)
        { return ValidRenderPlacement(Viewer, resource, location); }

        // ORTS Flexible Connections
        // Purpose: Share native matrix preflight; no change to Hook camera/tile handling.
        internal static bool ValidRenderPlacement(Viewer viewer, FlexibleConnectionHookResource resource, WorldPosition location)
        {
            var placement = location.XNAMatrix;
            if (!Finite(placement)) return false;
            // Preflight the same camera-relative translation that native Shapes
            // will apply; this copy is not supplied as a second offset.
            placement.M41 += (location.TileX - viewer.Camera.TileX) * (float)WorldPosition.TileSize;
            placement.M43 -= (location.TileZ - viewer.Camera.TileZ) * (float)WorldPosition.TileSize;
            if (!Finite(placement)) return false;
            foreach (var product in resource.StaticProducts)
                if (!Finite(product * placement)) return false;
            return true;
        }

        private static bool Near(Vector3 a, Vector3 b)
        { return Near(a, b, FlexibleConnectionHookConstants.GeometryTolerance); }

        // ORTS Flexible Connections
        // Purpose: Endpoint acceptance is shared; each family supplies its own tolerance in metres.
        internal static bool Near(Vector3 a, Vector3 b, double tolerance)
        {
            double x = (double)a.X - b.X, y = (double)a.Y - b.Y, z = (double)a.Z - b.Z;
            return FlexibleConnectionHookGeometry.Finite(x) && FlexibleConnectionHookGeometry.Finite(y) && FlexibleConnectionHookGeometry.Finite(z) &&
                x * x + y * y + z * z <= tolerance * tolerance;
        }

        // ORTS Flexible Connections
        // Purpose: Reuse finite normalization after Coupler has checked its angular degeneracy threshold.
        internal static bool Normalize(ref Vector3 value)
        {
            double length = Math.Sqrt((double)value.X * value.X + (double)value.Y * value.Y + (double)value.Z * value.Z);
            if (!FlexibleConnectionHookGeometry.Finite(length) || length <= 0) return false;
            value = new Vector3((float)(value.X / length), (float)(value.Y / length), (float)(value.Z / length));
            return true;
        }

        internal static bool Finite(Matrix m)
        {
            return FlexibleConnectionHookGeometry.Finite(m.M11) && FlexibleConnectionHookGeometry.Finite(m.M12) && FlexibleConnectionHookGeometry.Finite(m.M13) && FlexibleConnectionHookGeometry.Finite(m.M14) &&
                FlexibleConnectionHookGeometry.Finite(m.M21) && FlexibleConnectionHookGeometry.Finite(m.M22) && FlexibleConnectionHookGeometry.Finite(m.M23) && FlexibleConnectionHookGeometry.Finite(m.M24) &&
                FlexibleConnectionHookGeometry.Finite(m.M31) && FlexibleConnectionHookGeometry.Finite(m.M32) && FlexibleConnectionHookGeometry.Finite(m.M33) && FlexibleConnectionHookGeometry.Finite(m.M34) &&
                FlexibleConnectionHookGeometry.Finite(m.M41) && FlexibleConnectionHookGeometry.Finite(m.M42) && FlexibleConnectionHookGeometry.Finite(m.M43) && FlexibleConnectionHookGeometry.Finite(m.M44);
        }
    }

    // ---------------------------------------------------------------------
    // Native SharedShape resource records and frame references.
    // These records never own/dispose the shared GPU buffers.
    // ---------------------------------------------------------------------
    internal enum FlexibleConnectionHookResourceState { Pending, Loading, Ready, Failed }

    internal sealed class FlexibleConnectionHookResource
    {
        internal readonly string Path;
        internal FlexibleConnectionHookResourceState State;
        internal SharedShape Shape;
        internal Matrix[] StaticProducts;
        internal FlexibleConnectionHookResource(string path) { Path = path; }
    }

    internal sealed class FlexibleConnectionHookOwner
    {
        internal readonly Viewer Viewer;
        internal readonly string Folder;
        internal readonly List<FlexibleConnectionHookResource> Resources = new List<FlexibleConnectionHookResource>();
        internal readonly List<FlexibleConnectionHookFrameReference> References = new List<FlexibleConnectionHookFrameReference>();
        internal bool Retired;
        internal int FrameReferences, Loading;
        // ORTS Flexible Connections
        // Purpose: Share the existing frame-safe owner; preserve Hook's diagnostic label by default.
        internal readonly string Family;
        internal FlexibleConnectionHookOwner(Viewer viewer, string folder, string family)
        { Viewer = viewer; Folder = folder; Family = family; }
    }

    internal sealed class FlexibleConnectionHookFrameReference
    {
        private readonly FlexibleConnectionHookResourceManager Manager;
        internal readonly FlexibleConnectionHookOwner Owner;
        internal RenderFrame Frame;
        internal FlexibleConnectionHookFrameReference NextInFrame;
        internal FlexibleConnectionHookFrameReference(FlexibleConnectionHookResourceManager manager, FlexibleConnectionHookOwner owner)
        { Manager = manager; Owner = owner; }
        internal void Release(RenderFrame frame) { Manager.Release(this, frame); }
    }

    // ---------------------------------------------------------------------
    // Resource lifecycle. No nested Hose lock, no IO under Gate, no Draw load.
    // Ready fields are immutable; Gate publishes and observes them together.
    // ---------------------------------------------------------------------
    internal sealed class FlexibleConnectionHookResourceManager
    {
        private readonly object Gate = new object();
        private readonly List<FlexibleConnectionHookOwner> Owners = new List<FlexibleConnectionHookOwner>();
        private bool Closed;

        // ORTS Flexible Connections
        // Purpose: Coupler uses the same Loader/frame references; only the warning label differs.
        internal FlexibleConnectionHookOwner Register(Viewer viewer, string folder, string family = "Hook")
        {
            var owner = new FlexibleConnectionHookOwner(viewer, folder, family);
            lock (Gate)
            {
                owner.Retired = Closed;
                if (!Closed) Owners.Add(owner);
            }
            return owner;
        }

        internal FlexibleConnectionHookResource Request(FlexibleConnectionHookOwner owner, string fileName)
        {
            lock (Gate)
            {
                if (Closed || owner.Retired) return null;
                string path = System.IO.Path.Combine(owner.Folder, fileName);
                foreach (var existing in owner.Resources)
                    if (string.Equals(existing.Path, path, StringComparison.OrdinalIgnoreCase)) return existing;
                var resource = new FlexibleConnectionHookResource(path);
                owner.Resources.Add(resource);
                return resource;
            }
        }

        internal bool TryGet(FlexibleConnectionHookResource resource, out SharedShape shape)
        {
            lock (Gate)
            {
                shape = resource != null && resource.State == FlexibleConnectionHookResourceState.Ready ? resource.Shape : null;
                return shape != null;
            }
        }

        internal bool Failed(FlexibleConnectionHookResource resource)
        { lock (Gate) return resource != null && resource.State == FlexibleConnectionHookResourceState.Failed; }

        internal void ProcessPending()
        {
            while (true)
            {
                FlexibleConnectionHookOwner pendingOwner = null;
                FlexibleConnectionHookResource pending = null;
                lock (Gate)
                {
                    if (Closed) return;
                    foreach (var owner in Owners)
                    {
                        if (owner.Retired) continue;
                        foreach (var resource in owner.Resources)
                            if (resource.State == FlexibleConnectionHookResourceState.Pending) { pendingOwner = owner; pending = resource; break; }
                        if (pending != null) break;
                    }
                    if (pending == null) return;
                    pending.State = FlexibleConnectionHookResourceState.Loading;
                    pendingOwner.Loading++;
                }
                SharedShape shape = null;
                Matrix[] products = null;
                string failure = null;
                try
                {
                    // ORTS Flexible Connections
                    // Purpose: Supply the native rolling-stock texture context while retaining the physical shape path for diagnostics.
                    string referencePath = pendingOwner.Folder + @"\";
                    shape = pendingOwner.Viewer.ShapeManager.Get(pending.Path + '\0' + referencePath);
                    if (!ValidateShape(shape, out products)) failure = "empty or unusable static shape structure";
                }
                catch (Exception error)
                {
                    // Failure is memoized once; no automatic retry on subsequent frames.
                    failure = error.Message;
                }
                bool report;
                lock (Gate)
                {
                    pendingOwner.Loading--;
                    report = !Closed && !pendingOwner.Retired && failure != null;
                    if (!Closed && !pendingOwner.Retired && failure == null)
                    {
                        pending.Shape = shape;
                        pending.StaticProducts = products;
                        pending.State = FlexibleConnectionHookResourceState.Ready;
                    }
                    else pending.State = FlexibleConnectionHookResourceState.Failed;
                    RemoveRetired(pendingOwner);
                }
                // ORTS Flexible Connections
                // Purpose: Identify Coupler load failures while retaining the existing Hook warning text.
                if (report) Trace.TraceWarning("Flexible connection {2} shape '{0}' is unusable: {1}. No automatic retry.", pending.Path, failure, pendingOwner.Family);
            }
        }

        private static bool ValidateShape(SharedShape shape, out Matrix[] products)
        {
            products = null;
            if (shape == null || shape.LodControls == null || shape.LodControls.Length == 0 || shape.Matrices == null) return false;
            var transforms = new List<Matrix>();
            foreach (var matrix in shape.Matrices) if (!FlexibleConnectionHookViewer.Finite(matrix)) return false;
            foreach (var lod in shape.LodControls)
            {
                if (lod == null || lod.DistanceLevels == null || lod.DistanceLevels.Length == 0) return false;
                foreach (var level in lod.DistanceLevels)
                {
                    if (level == null || level.SubObjects == null || level.SubObjects.Length == 0 ||
                        !FlexibleConnectionHookGeometry.Finite(level.ViewSphereRadius) || level.ViewSphereRadius < 0 ||
                        !FlexibleConnectionHookGeometry.Finite(level.ViewingDistance) || level.ViewingDistance < 0) return false;
                    foreach (var subObject in level.SubObjects)
                    {
                        if (subObject == null || subObject.ShapePrimitives == null) return false;
                        foreach (var primitive in subObject.ShapePrimitives)
                        {
                            if (primitive == null || primitive.Material == null || primitive.Hierarchy == null) return false;
                            Matrix product = Matrix.Identity;
                            int index = primitive.HierarchyIndex, steps = 0;
                            while (index >= 0)
                            {
                                if (index >= shape.Matrices.Length || index >= primitive.Hierarchy.Length || ++steps > primitive.Hierarchy.Length) return false;
                                product *= shape.Matrices[index];
                                index = primitive.Hierarchy[index];
                            }
                            if (index != -1 || !FlexibleConnectionHookViewer.Finite(product)) return false;
                            transforms.Add(product);
                        }
                    }
                }
            }
            if (transforms.Count == 0) return false;
            products = transforms.ToArray();
            return true;
        }

        internal bool Retain(FlexibleConnectionHookOwner owner, RenderFrame frame)
        {
            lock (Gate)
            {
                if (Closed || owner.Retired) return false;
                FlexibleConnectionHookFrameReference available = null;
                foreach (var reference in owner.References)
                {
                    if (ReferenceEquals(reference.Frame, frame)) return true;
                    if (reference.Frame == null) available = reference;
                }
                if (available == null)
                {
                    available = new FlexibleConnectionHookFrameReference(this, owner);
                    owner.References.Add(available);
                }
                available.Frame = frame;
                owner.FrameReferences++;
                frame.RegisterFlexibleConnectionHookReference(available);
                return true;
            }
        }

        internal void Release(FlexibleConnectionHookFrameReference reference, RenderFrame frame)
        {
            lock (Gate)
            {
                if (!ReferenceEquals(reference.Frame, frame)) return;
                reference.Frame = null;
                reference.NextInFrame = null;
                reference.Owner.FrameReferences--;
                RemoveRetired(reference.Owner);
            }
        }

        internal void Retire(FlexibleConnectionHookOwner owner)
        {
            lock (Gate) { owner.Retired = true; RemoveRetired(owner); }
        }

        private void RemoveRetired(FlexibleConnectionHookOwner owner)
        {
            if (owner.Retired && owner.FrameReferences == 0 && owner.Loading == 0) Owners.Remove(owner);
        }

        internal void Mark()
        {
            // Loader only, between native Mark reset and Sweep. Holding Gate
            // keeps admission/removal consistent with this mark snapshot.
            lock (Gate)
                foreach (var owner in Owners)
                    foreach (var resource in owner.Resources)
                        if (resource.State == FlexibleConnectionHookResourceState.Ready) resource.Shape.Mark();
        }

        internal void Close()
        {
            lock (Gate)
            {
                Closed = true;
                for (int i = Owners.Count - 1; i >= 0; i--)
                {
                    var owner = Owners[i];
                    owner.Retired = true;
                    RemoveRetired(owner);
                }
            }
        }
    }
}
