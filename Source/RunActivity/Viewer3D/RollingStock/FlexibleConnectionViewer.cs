// ORTS Flexible Connections
//
// Purpose: Coordinate Hose, Hook and Coupler visuals for one vehicle.
// Responsibilities: Select publishing Cases, resolve local/remote profiles, track point
//   occupancy, and build continuous Hose meshes with two material ranges and local Halves.
// Key components: FlexibleConnectionViewer delegates rigid families; FlexibleConnectionLifetime
//   builds sealed snapshots; FlexibleConnectionLifetimeManager coordinates resource retirement.
// Related files: MSTSWagonViewer.cs supplies physical endpoints; FlexibleConnection.cs supplies
//   configuration; FlexibleConnectionCatenary.cs supplies Hose samples. Hook/Coupler viewers
//   implement rigid visuals. Trains.cs, RenderFrame.cs and RenderProcess.cs drive resource lifetime.
// Lifecycle: Loader resolves materials and rigid assets; Updater prepares and seals geometry;
//   Render prepares/releases procedural GPU resources. Frame references protect retired owners.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Orts.Simulation.RollingStocks;
using Orts.Viewer3D.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ORTS.Common;

namespace Orts.Viewer3D.RollingStock
{
    internal sealed class FlexibleConnectionViewer
    {
        private const float MinimumLengthM = 0.0001f;
        private readonly int SegmentCount;
        private readonly FlexibleConnectionCatenaryResult CatenaryResult;
        private readonly Vector3[] CatenaryPoints;
        private readonly FlexibleConnectionConfig Configuration;
        private readonly FlexibleConnectionLifetime Lifetime;
        // ORTS Flexible Connections
        // Purpose: Keep Hook visuals and mixed-family diagnostics outside the Hose path.
        private readonly FlexibleConnectionHookViewer HookViewer;
        // ORTS Flexible Connections
        // Purpose: Keep the new visual family and its stable mismatch warnings outside Hook/Hose.
        private readonly FlexibleConnectionCouplerViewer CouplerViewer;
        private readonly Dictionary<FlexibleConnectionMapping, FlexibleConnectionCouplerBinding> CouplerBindings =
            new Dictionary<FlexibleConnectionMapping, FlexibleConnectionCouplerBinding>();
        private readonly Dictionary<FlexibleConnectionMapping, FlexibleConnectionHookBinding> HookBindings =
            new Dictionary<FlexibleConnectionMapping, FlexibleConnectionHookBinding>();
        private readonly Viewer Viewer;
        private readonly SolidColorMaterial FallbackMaterial;
        private readonly Dictionary<FlexibleConnectionLocalProfile, Material> ProfileMaterials =
            new Dictionary<FlexibleConnectionLocalProfile, Material>();
        private readonly Dictionary<FlexibleConnectionLocalProfile, float> ProfileScales =
            new Dictionary<FlexibleConnectionLocalProfile, float>();
        private readonly Dictionary<FlexibleConnectionMapping, RemoteBinding> Bindings =
            new Dictionary<FlexibleConnectionMapping, RemoteBinding>();
        private readonly Dictionary<MSTSWagon, int> BilateralWarnings = new Dictionary<MSTSWagon, int>();
        // Instance-local, reusable occupancy; Front:A and Rear:A never share state.
        private readonly HashSet<string> ConnectedFront = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> ConnectedRear = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        internal void ResetConnectedPoints()
        {
            ConnectedFront.Clear();
            ConnectedRear.Clear();
            // ORTS Flexible Connections
            // Purpose: Reset only local successful-publication state; occupancy marks retain their existing semantics.
            CouplerViewer?.ResetPublication();
        }

        internal void MarkConnectedPoint(FlexibleConnectionPoint point)
        {
            (point.End == FlexibleConnectionEnd.Front ? ConnectedFront : ConnectedRear).Add(point.ID);
        }

        // ORTS Flexible Connections
        // Purpose: Only the local Case owner may publish; equal Owners use consist order once.
        internal FlexibleConnectionCase GetPublishingCase(MSTSWagon remote,
            FlexibleConnectionEnd localEnd, FlexibleConnectionEnd remoteEnd, bool lowerIndex)
        {
            var remoteConfig = remote.FlexibleConnections;
            if (Configuration.ConnectionOwner == null || remoteConfig == null || remoteConfig.ConnectionOwner == null)
                return null;
            FlexibleConnectionCase localCase;
            bool declared = Configuration.ConnectionCases.TryGetValue(remoteConfig.ConnectionOwner, out localCase);
            if (string.Equals(Configuration.ConnectionOwner, remoteConfig.ConnectionOwner, StringComparison.OrdinalIgnoreCase))
                return lowerIndex && declared && localCase.Valid ? localCase : null;
            bool remoteDeclared = remoteConfig.ConnectionCases.ContainsKey(Configuration.ConnectionOwner);
            if (declared && remoteDeclared)
            {
                if (lowerIndex)
                {
                    int mask;
                    BilateralWarnings.TryGetValue(remote, out mask);
                    int bit = 1 << ((int)localEnd * 2 + (int)remoteEnd);
                    if ((mask & bit) == 0)
                    {
                        BilateralWarnings[remote] = mask | bit;
                        Trace.TraceWarning("Flexible connection between {0} and {1} is declared by both rolling stocks. Remove one of the duplicate declarations; interface not rendered.",
                            Configuration.ConnectionOwner, remoteConfig.ConnectionOwner);
                    }
                }
                return null;
            }
            return declared && localCase.Valid ? localCase : null;
        }

        // ORTS Flexible Connections
        // Purpose: Prepare immutable local resources once; remote files are requested through Loader.
        internal FlexibleConnectionViewer(Viewer viewer, int sides, int segments, string wagonFolder,
            FlexibleConnectionConfig configuration)
        {
            Viewer = viewer; SegmentCount = segments; Configuration = configuration;
            // Reserve capacity for every local point, including remotely published connections.
            foreach (var id in configuration.FrontConnectionPoints.Keys) ConnectedFront.Add(id);
            foreach (var id in configuration.RearConnectionPoints.Keys) ConnectedRear.Add(id);
            ResetConnectedPoints();
            // =====================================================================
            // ORTS Flexible Connections
            // Purpose: Classify once; Hook-only vehicles allocate no Hose resources.
            // =====================================================================
            // Purpose: Classify all three families before allocating any Hose resources.
            bool hasHook = false, hasCoupler = false, hasHose = false;
            foreach (var point in configuration.FrontConnectionPoints.Values)
            {
                hasHook |= point.Profile.Family == FlexibleConnectionFamily.Hook;
                hasCoupler |= point.Profile.Family == FlexibleConnectionFamily.Coupler;
                hasHose |= point.Profile.Family == FlexibleConnectionFamily.Hose;
            }
            foreach (var point in configuration.RearConnectionPoints.Values)
            {
                hasHook |= point.Profile.Family == FlexibleConnectionFamily.Hook;
                hasCoupler |= point.Profile.Family == FlexibleConnectionFamily.Coupler;
                hasHose |= point.Profile.Family == FlexibleConnectionFamily.Hose;
            }
            if (hasHook) HookViewer = new FlexibleConnectionHookViewer(viewer, configuration, wagonFolder);
            if (hasCoupler) CouplerViewer = new FlexibleConnectionCouplerViewer(viewer, configuration, wagonFolder);
            if (!hasHose) return;
            CatenaryResult = new FlexibleConnectionCatenaryResult();
            CatenaryPoints = new Vector3[segments + 1];
            FallbackMaterial = new SolidColorMaterial(viewer, 1f, 1f, 1f, 0f);
            foreach (var profile in configuration.LocalProfiles.Values)
                // ORTS Flexible Connections
                // Purpose: Hook profiles never request Hose textures or radius-derived geometry.
                // Purpose: Explicit Hose admission excludes both rigid shape families.
                if (profile != null && profile.Family == FlexibleConnectionFamily.Hose)
                {
                    ProfileMaterials.Add(profile, LoadProfileMaterial(profile, wagonFolder));
                    ProfileScales.Add(profile, (float)(2.0 * profile.RadiusM * Math.Cos(Math.PI / sides)));
                }
            // ORTS Flexible Connections
            // Purpose: Preserve Hose lifetimes while excluding Hook points from Hose topology construction.
            var hosePoints = new List<FlexibleConnectionPoint>();
            foreach (var point in configuration.ConnectedPoints)
                // ORTS Flexible Connections
                // Purpose: Coupler points never enter procedural Hose topology/resources.
                if (point.Profile.Family == FlexibleConnectionFamily.Hose) hosePoints.Add(point);
            Lifetime = viewer.RenderProcess.FlexibleConnections.Register(this, hosePoints, sides, segments);
        }

        // =====================================================================
        // ORTS Flexible Connections
        // Purpose: Handle either Hook family before Hose compatibility/material resolution.
        // A failed Hook publication remains handled and cannot fall through to Hose.
        // =====================================================================
        internal bool TryPrepareHook(RenderFrame frame, FlexibleConnectionMapping mapping, MSTSWagon local,
            MSTSWagon remote, FlexibleConnectionEnd remoteEnd, out FlexibleConnectionPoint point, out bool published)
        {
            point = null; published = false;
            if (remote.FlexibleConnections != null)
                remote.FlexibleConnections.GetConnectionPoints(remoteEnd).TryGetValue(mapping.RemotePoint, out point);
            // ORTS Flexible Connections
            // Purpose: Reuse the native dispatch contract; a failed/mixed Coupler never falls through to Hose.
            bool localCoupler = mapping.Point.Profile.Family == FlexibleConnectionFamily.Coupler;
            bool remoteCoupler = point != null && point.Profile.Family == FlexibleConnectionFamily.Coupler;
            if (localCoupler || remoteCoupler)
            {
                if (localCoupler)
                    published = CouplerViewer.PrepareConnected(frame, mapping, local, remote, remoteEnd, point);
                else
                    FlexibleConnectionCouplerViewer.GetBinding(CouplerBindings, mapping, remote, remoteEnd, point)
                        .Warn(local, FlexibleConnectionCouplerWarning.MixedFamilies, "Coupler requires Coupler at both ends");
                return true;
            }
            bool localHook = mapping.Point.Profile.Hook != null;
            bool remoteHook = point != null && point.Profile.Hook != null;
            if (!localHook && !remoteHook) return false;
            if (localHook)
                published = HookViewer.PrepareConnected(frame, mapping, local, remote, remoteEnd, point);
            else
                FlexibleConnectionHookViewer.GetBinding(HookBindings, mapping, remote, remoteEnd, point)
                    .Warn(local, FlexibleConnectionHookWarning.MixedFamilies, "Hook/Hose families cannot be connected");
            return true;
        }

        private sealed class RemoteBinding
        {
            internal MSTSWagon Vehicle;
            internal FlexibleConnectionEnd End;
            internal FlexibleConnectionPoint Point;
            internal FlexibleConnectionMaterialRequest Request;
            internal RemoteBinding Next;
            internal readonly HashSet<string> GeometryWarnings = new HashSet<string>();
        }

        // Resolve only when the effective neighbor changes. Failed combinations remain memoized too.
        internal bool TryResolve(FlexibleConnectionMapping mapping, MSTSWagon local, MSTSWagon remote,
            FlexibleConnectionEnd remoteEnd, out FlexibleConnectionPoint point, out Material material)
        {
            point = null; material = null;
            if (remote.FlexibleConnections != null)
                remote.FlexibleConnections.GetConnectionPoints(remoteEnd).TryGetValue(mapping.RemotePoint, out point);
            RemoteBinding head;
            Bindings.TryGetValue(mapping, out head);
            RemoteBinding binding = head;
            while (binding != null && (!ReferenceEquals(binding.Vehicle, remote) || binding.End != remoteEnd || !ReferenceEquals(binding.Point, point)))
                binding = binding.Next;
            if (binding == null)
            {
                binding = new RemoteBinding { Vehicle = remote, End = remoteEnd, Point = point, Next = head };
                Bindings[mapping] = binding;
                string label = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "Flexible connection {0} ({1}) {2}:{3} -> {4} ({5}) {6}:{7}",
                    local.CarID, local.WagFilePath, mapping.Point.End, mapping.LocalPoint,
                    remote.CarID, remote.WagFilePath, remoteEnd, mapping.RemotePoint);
                if (point == null)
                    Trace.TraceWarning("{0}: remote point/profile is undefined or invalid; not rendered.", label);
                else if (Compatible(mapping.Point.Profile, point.Profile, label))
                    binding.Request = Lifetime.Manager.RequestMaterial(Lifetime, point.Profile, Path.GetDirectoryName(remote.WagFilePath));
            }
            if (binding.Request == null || !binding.Request.Ready) return false;
            material = binding.Request.Material;
            return material != null;
        }

        // ORTS Flexible Connections
        // Purpose: Report numerical failures once per neighbor/context/cause, not once per frame.
        internal void WarnGeometry(FlexibleConnectionMapping mapping, MSTSWagon remote, FlexibleConnectionEnd end, string cause)
        {
            RemoteBinding binding;
            if (!Bindings.TryGetValue(mapping, out binding)) return;
            while (binding != null && (!ReferenceEquals(binding.Vehicle, remote) || binding.End != end)) binding = binding.Next;
            if (binding != null && binding.GeometryWarnings.Add(cause))
                Trace.TraceWarning("Flexible connection {0}:{1} -> {2} {3}:{4}: {5}; not rendered.",
                    mapping.Point.End, mapping.LocalPoint, remote.CarID, end, mapping.RemotePoint, cause);
        }

        private static bool Compatible(FlexibleConnectionLocalProfile a, FlexibleConnectionLocalProfile b, string label)
        {
            bool valid = true;
            if (a.LengthM != b.LengthM) { WarnMismatch(label, "Length", a.LengthM, b.LengthM); valid = false; }
            if (a.RadiusM != b.RadiusM) { WarnMismatch(label, "Radius", a.RadiusM, b.RadiusM); valid = false; }
            if ((a.Fitting == null) != (b.Fitting == null))
            {
                Trace.TraceWarning("{0}: incompatible fitting presence: local={1}, remote={2}; not rendered.", label, a.Fitting != null, b.Fitting != null);
                valid = false;
            }
            else if (a.Fitting != null)
            {
                if (a.Fitting.LengthM != b.Fitting.LengthM) { WarnMismatch(label, "FittingLength", a.Fitting.LengthM, b.Fitting.LengthM); valid = false; }
                if (a.Fitting.RadiusM != b.Fitting.RadiusM) { WarnMismatch(label, "FittingRadius", a.Fitting.RadiusM, b.Fitting.RadiusM); valid = false; }
                if (a.Fitting.ExtrudeM != b.Fitting.ExtrudeM) { WarnMismatch(label, "FittingExtrude", a.Fitting.ExtrudeM, b.Fitting.ExtrudeM); valid = false; }
            }
            return valid;
        }
        private static void WarnMismatch(string label, string property, float local, float remote)
        {
            Trace.TraceWarning("{0}: incompatible {1}: local={2}, remote={3}; not rendered.", label, property,
                local.ToString("R", System.Globalization.CultureInfo.InvariantCulture), remote.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        }

        // ORTS Flexible Connections
        // Purpose: Solve once and publish one retained snapshot with two material draw ranges.
        internal FlexibleConnectionCatenaryResult PrepareFrame(RenderFrame frame, FlexibleConnectionMapping mapping,
            FlexibleConnectionPoint remotePoint, Material remoteMaterial, Vector3 ca, Vector3 cb, int tileX, int tileZ,
            out bool published)
        {
            published = false;
            var profile = mapping.Point.Profile;
            double length = (double)profile.LengthM + remotePoint.Profile.LengthM;
            var result = FlexibleConnectionCatenary.Calculate(ca, cb, length, SegmentCount, CatenaryResult, CatenaryPoints);
            if (result.Status != FlexibleConnectionCatenaryStatus.Success) return result;
            float scale = ProfileScales[profile];
            var snapshot = Lifetime.BuildSnapshot(frame, mapping.Point, result, scale, MinimumLengthM, ProfileMaterials[profile], remoteMaterial);
            if (snapshot == null) return result;
            float tileSize = (float)WorldPosition.TileSize;
            Vector3 tileOffset = new Vector3((tileX - Viewer.Camera.TileX) * tileSize, 0f, -(tileZ - Viewer.Camera.TileZ) * tileSize);
            Matrix placement = Matrix.CreateTranslation(snapshot.Origin + tileOffset);
            frame.AddPrimitive(snapshot.MaterialA, snapshot.PrimitiveA, RenderPrimitiveGroup.World, ref placement, ShapeFlags.None);
            frame.AddPrimitive(snapshot.MaterialB, snapshot.PrimitiveB, RenderPrimitiveGroup.World, ref placement, ShapeFlags.None);
            published = true;
            return result;
        }

        internal Material LoadProfileMaterial(FlexibleConnectionLocalProfile profile, string wagonFolder)
        {
            string textureName = profile.TextureName;
            string texturePath = null;
            string failure;

            if (string.IsNullOrWhiteSpace(textureName))
            {
                failure = "Texture is missing or empty";
            }
            else
            {
                try
                {
                    // This feature accepts ACE names relative to the owner.
                    // The native manager may use a same-name DDS alternative.
                    if (!string.Equals(
                        Path.GetExtension(textureName), ".ace",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        failure = "Texture must name an ACE file";
                    }
                    else if (Path.IsPathRooted(textureName) ||
                        string.IsNullOrWhiteSpace(wagonFolder))
                    {
                        failure = "Texture must be relative to the owner's folder";
                    }
                    else
                    {
                        texturePath = Path.GetFullPath(
                            Helpers.GetTextureFile(
                                Viewer.Simulator, Helpers.TextureFlags.None,
                                wagonFolder, textureName));

                        // Preflight through the native cache. A successful load
                        // is reused when SceneryMaterial requests the same path.
                        Texture2D texture = Viewer.TextureManager.Get(texturePath, true);
                        if (texture == null ||
                            texture == SharedMaterialManager.MissingTexture)
                        {
                            failure = "Texture manager returned MissingTexture";
                        }
                        else
                        {
                            var options = SceneryMaterialOptions.ShaderImage |
                                SceneryMaterialOptions.Diffuse |
                                SceneryMaterialOptions.TextureAddressModeClamp;

                            Material material = Viewer.MaterialManager.Load(
                                "Scenery", texturePath, (int)options, 0f);

                            // With no night options, this accessor returns the
                            // actual day texture used by SceneryMaterial.
                            if (material is SceneryMaterial &&
                                material.GetShadowTexture() != null &&
                                material.GetShadowTexture() != SharedMaterialManager.MissingTexture)
                                return material;

                            failure = "Scenery material has no valid texture";
                        }
                    }
                }
                catch (Exception error)
                {
                    // A bad optional texture must not suppress a valid connection.
                    failure = "Texture could not be loaded: " + error.Message;
                }
            }

            Trace.TraceWarning(
                "Owner folder '{0}', LocalProfile '{1}', Texture '{2}': {3}. Using yellow fallback.",
                wagonFolder, profile.Name,
                texturePath ?? textureName ?? "(unspecified)", failure);
            return FallbackMaterial;
        }

        private static readonly Matrix FrontHalfRotation = new Matrix(
            -1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f,
            0f, 0f, -1f, 0f, 0f, 0f, 0f, 1f);

        internal void PrepareHalfFrame(RenderFrame frame, WorldPosition position, FlexibleConnectionEnd end)
        {
            var connected = end == FlexibleConnectionEnd.Front ? ConnectedFront : ConnectedRear;
            // ORTS Flexible Connections
            // Purpose: NotHooked uses the final point marks even when there are no Hose Half endpoints.
            HookViewer?.PrepareDisconnected(frame, position, end, connected);
            // ORTS Flexible Connections
            // Purpose: Coupler uses local publication, never bilateral Connected, to hide its hanging asset.
            CouplerViewer?.PrepareDisconnected(frame, position, end);
            bool needed = false;
            foreach (var endpoint in Configuration.GetHalfEndpoints(end))
                if (!connected.Contains(endpoint.PointID)) { needed = true; break; }
            if (!needed) return; // Preserve lazy construction when every Half point is connected.
            var manager = Lifetime.Manager;
            if (manager.BeginHalfBuild(Lifetime))
            {
                FlexibleConnectionHalfBundle created = null;
                try
                {
                    created = new FlexibleConnectionHalfBundle(Lifetime, Configuration, ProfileMaterials, FallbackMaterial);
                }
                finally
                {
                    manager.CompleteHalfBuild(Lifetime, created);
                }
            }
            var half = manager.GetHalf(Lifetime);
            if (half == null) return;
            float tileSize = (float)WorldPosition.TileSize;
            Vector3 tileOffset = new Vector3((position.TileX - Viewer.Camera.TileX) * tileSize,
                0f, -(position.TileZ - Viewer.Camera.TileZ) * tileSize);
            foreach (var resource in half.Resources)
            {
                // ORTS Flexible Connections
                // Purpose: Publish only points without an effective Connected in this preparation.
                if (resource.End != end) continue;
                if (connected.Contains(resource.PointID)) continue;
                Matrix placement;
                if (end == FlexibleConnectionEnd.Rear)
                    placement = Matrix.CreateTranslation(resource.Anchor) * position.XNAMatrix * Matrix.CreateTranslation(tileOffset);
                else
                    placement = FrontHalfRotation * Matrix.CreateTranslation(resource.Anchor) * position.XNAMatrix * Matrix.CreateTranslation(tileOffset);
                if (!manager.PublishHalf(resource, frame)) continue;
                frame.AddPrimitive(resource.Material, resource.Primitive, RenderPrimitiveGroup.World,
                    ref placement, ShapeFlags.None);
            }
        }


        // ORTS Flexible Connections
        // Purpose: Request retirement for Hose, Hook and Coupler owners; rigid-only viewers
        // have no Hose lifetime. Existing frame references protect pending render work.
        // Coupler uses the same rigid-resource manager as Hook, with its own owner.
        internal void RequestRetirement() { Lifetime?.RequestRetirement(); HookViewer?.RequestRetirement(); CouplerViewer?.RequestRetirement(); }
        internal void Mark()
        {
            // ORTS Flexible Connections
            // Purpose: Mark Hose materials here; the shared rigid-resource manager marks Hook
            // and Coupler assets, including those retained by frames after owner retirement.
            FallbackMaterial?.Mark();
            foreach (var material in ProfileMaterials.Values) material.Mark();
        }
    }

    // ORTS Flexible Connections
    // Purpose: Loader publishes a completed material using a volatile ready flag; pending is not failure.
    internal sealed class FlexibleConnectionMaterialRequest
    {
        internal readonly FlexibleConnectionLocalProfile Profile;
        internal readonly string Folder;
        internal Material Material;
        internal volatile bool Ready;
        internal bool Loading;
        internal FlexibleConnectionMaterialRequest(FlexibleConnectionLocalProfile profile, string folder)
        { Profile = profile; Folder = folder; }
    }

    // ORTS Flexible Connections
    // Purpose: Original ring/frame workspace shared by plain and fitted continuous mesh construction.
    internal sealed class FlexibleConnectionGeometry
    {
        internal readonly int Sides;
        internal readonly int Segments;
        internal readonly int VertexCount;
        private readonly Vector2[] Profile;
        private readonly Vector2[] FaceNormals;
        private readonly Vector3[] Ring;

        internal FlexibleConnectionGeometry(int sides, int segments)
        {
            Sides = sides;
            Segments = segments;
            VertexCount = 2 * sides * (segments + 2);
            Profile = new Vector2[sides];
            FaceNormals = new Vector2[sides];
            Ring = new Vector3[sides];

            // Keep the square's original corners and circumferential UV order.
            if (sides == 4)
            {
                Profile[0] = new Vector2(0.5f, -0.5f);
                Profile[1] = new Vector2(0.5f, 0.5f);
                Profile[2] = new Vector2(-0.5f, 0.5f);
                Profile[3] = new Vector2(-0.5f, -0.5f);
            }
            else
            {
                double halfAngle = Math.PI / sides;
                double angleStep = 2.0 * Math.PI / sides;
                double radius = 0.5 / Math.Cos(halfAngle);
                for (int j = 0; j < sides; j++)
                {
                    double angle = halfAngle + angleStep * j;
                    Profile[j] = new Vector2(
                        (float)(radius * Math.Cos(angle)),
                        (float)(radius * Math.Sin(angle)));
                }
            }

            for (int j = 0; j < sides; j++)
            {
                Vector2 edge = Profile[(j + 1) % sides] - Profile[j];
                FaceNormals[j] = Vector2.Normalize(new Vector2(edge.Y, -edge.X));
            }


        }

        internal VertexPositionNormalTexture[] CreateVertices()
        {
            var vertices = new VertexPositionNormalTexture[VertexCount];
            for (int i = 0; i <= Segments; i++)
            {
                float v = (float)i / Segments;
                for (int j = 0; j < Sides; j++)
                {
                    int offset = 2 * (i * Sides + j);
                    vertices[offset].TextureCoordinate = new Vector2((float)j / Sides, v);
                    vertices[offset + 1].TextureCoordinate = new Vector2((float)(j + 1) / Sides, v);
                }
            }

            int rear = 2 * Sides * (Segments + 1);
            for (int j = 0; j < Sides; j++)
            {
                vertices[rear + j].TextureCoordinate = new Vector2(0.5f, 0f);
                vertices[rear + Sides + j].TextureCoordinate = new Vector2(0.5f, 0f);
            }
            return vertices;
        }

        internal bool Build(
            Vector3[] points, float scaleXY, float minimumLength,
            VertexPositionNormalTexture[] vertices)
        {
            if (points == null || points.Length != Segments + 1 ||
                vertices.Length != VertexCount || !Finite(scaleXY) || scaleXY <= 0f)
                return false;

            for (int i = 0; i <= Segments; i++)
            {
                if (!Finite(points[i]))
                    return false;
                if (i > 0)
                {
                    // Preserve the segment guard, including its float length calculation.
                    float length = (points[i] - points[i - 1]).Length();
                    if (!Finite(length) || length < minimumLength)
                        return false;
                }
            }

            Vector3 horizontal;
            if (!Unit(new Vector3(
                (float)((double)points[Segments].X - points[0].X), 0f,
                (float)((double)points[Segments].Z - points[0].Z)), out horizontal))
                return false;

            Vector3 axisX;
            if (!Unit(Vector3.Cross(Vector3.UnitY, horizontal), out axisX))
                return false;

            for (int i = 0; i <= Segments; i++)
            {
                int previous = i == 0 ? 0 : i - 1;
                int next = i == Segments ? Segments : i + 1;
                Vector3 direction = Difference(points[next], points[previous]);
                // Remove only numerical departure from the catenary plane.
                direction -= axisX * Vector3.Dot(direction, axisX);
                Vector3 tangent;
                if (!Unit(direction, out tangent))
                    return false;

                Vector3 axisY;
                if (!Unit(Vector3.Cross(tangent, axisX), out axisY))
                    return false;

                Vector3 center = Difference(points[i], points[0]);
                if (!Finite(center))
                    return false;

                // One position per geometric corner, copied to adjacent faces.
                // This keeps every seam/junction position bit-identical.
                for (int j = 0; j < Sides; j++)
                {
                    Ring[j] = center + axisX * (Profile[j].X * scaleXY) +
                        axisY * (Profile[j].Y * scaleXY);
                    if (!Finite(Ring[j]))
                        return false;
                }

                for (int j = 0; j < Sides; j++)
                {
                    Vector3 normal;
                    if (!Unit(axisX * FaceNormals[j].X + axisY * FaceNormals[j].Y, out normal))
                        return false;
                    int offset = 2 * (i * Sides + j);
                    vertices[offset].Position = Ring[j];
                    vertices[offset + 1].Position = Ring[(j + 1) % Sides];
                    vertices[offset].Normal = normal;
                    vertices[offset + 1].Normal = normal;
                }

                if (i == 0 || i == Segments)
                {
                    int cap = 2 * Sides * (Segments + 1) + (i == 0 ? 0 : Sides);
                    for (int j = 0; j < Sides; j++)
                    {
                        vertices[cap + j].Position = Ring[j];
                        vertices[cap + j].Normal = i == 0 ? -tangent : tangent;
                    }
                }
            }

            return true;
        }

        private static Vector3 Difference(Vector3 a, Vector3 b)
        {
            return new Vector3(
                (float)((double)a.X - b.X),
                (float)((double)a.Y - b.Y),
                (float)((double)a.Z - b.Z));
        }

        private static bool Unit(Vector3 value, out Vector3 unit)
        {
            unit = Vector3.Zero;
            if (!Finite(value))
                return false;
            double length = Math.Sqrt(
                (double)value.X * value.X + (double)value.Y * value.Y + (double)value.Z * value.Z);
            if (double.IsNaN(length) || double.IsInfinity(length) || length <= 0.0)
                return false;
            unit = new Vector3(
                (float)(value.X / length), (float)(value.Y / length), (float)(value.Z / length));
            return Finite(unit);
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool Finite(Vector3 value)
        {
            return Finite(value.X) && Finite(value.Y) && Finite(value.Z);
        }
    }

    // =====================================================================
    // ORTS Flexible Connections
    // Purpose: Preplanned local fitting bands on the existing catenary. The continuous mesh builder,
    // original samples and cross-section resolution remain independent.
    // =====================================================================
    internal enum FlexibleConnectionFittingSurface
    {
        Hose, EntryTransition, Fitting, ExitTransition, EntryShoulder, ExitShoulder
    }

    internal struct FlexibleConnectionFittingSample
    {
        internal readonly double Arc;
        internal readonly int Original;
        internal readonly int Previous;
        internal readonly float Fraction;
        internal readonly float V;

        internal FlexibleConnectionFittingSample(double arc, double length, int segments)
        {
            Arc = arc;
            Original = -1;
            for (int i = 0; i <= segments; i++)
                if (arc == length * ((double)i / segments)) { Original = i; break; }
            double scaled = arc / length * segments;
            Previous = Math.Min(segments - 1, (int)Math.Floor(scaled));
            Fraction = (float)(scaled - Previous);
            V = (float)(arc <= length / 2.0 ? arc / (length / 2.0) : (length - arc) / (length / 2.0));
        }
    }

    internal struct FlexibleConnectionFittingRing
    {
        internal readonly int Sample;
        internal readonly float Scale;
        internal FlexibleConnectionFittingRing(int sample, float scale)
        {
            Sample = sample;
            Scale = scale;
        }
    }

    internal struct FlexibleConnectionFittingBand
    {
        internal readonly FlexibleConnectionFittingRing Start, End;
        internal readonly FlexibleConnectionFittingSurface Surface;
        internal FlexibleConnectionFittingBand(int start, int end, float startScale,
            float endScale, FlexibleConnectionFittingSurface surface)
        {
            Start = new FlexibleConnectionFittingRing(start, startScale);
            End = new FlexibleConnectionFittingRing(end, endScale);
            Surface = surface;
        }
    }

    // ORTS Flexible Connections
    // Purpose: Share index layouts by band count and material split, within one owner.
    internal sealed class FlexibleConnectionFittingTopology
    {
        internal readonly int VertexCount;
        internal readonly ushort[] Indices;
        internal IndexBuffer IndexBuffer;

        // ORTS Flexible Connections
        // Purpose: Two material ranges over one continuous mesh and one immutable index buffer.
        internal readonly int FirstIndexCount;
        internal FlexibleConnectionFittingTopology(int sides, int bands, int firstBands)
        {
            VertexCount = 4 * sides * bands + 2 * sides;
            if (VertexCount > ushort.MaxValue + 1) throw new ArgumentOutOfRangeException("bands");
            Indices = new ushort[6 * sides * bands + 6 * (sides - 2)];
            FirstIndexCount = 6 * sides * firstBands + 3 * (sides - 2);
            int index = 0;
            for (int side = 0; side < 2; side++)
            {
                int start = side == 0 ? 0 : firstBands;
                int end = side == 0 ? firstBands : bands;
                for (int i = start * sides; i < end * sides; i++)
                {
                    int a = 4 * i;
                    Indices[index++] = (ushort)a; Indices[index++] = (ushort)(a + 2); Indices[index++] = (ushort)(a + 3);
                    Indices[index++] = (ushort)a; Indices[index++] = (ushort)(a + 3); Indices[index++] = (ushort)(a + 1);
                }
                int cap = 4 * sides * bands + side * sides;
                for (int j = 1; j < sides - 1; j++)
                {
                    Indices[index++] = (ushort)cap;
                    Indices[index++] = (ushort)(cap + (side == 0 ? j : j + 1));
                    Indices[index++] = (ushort)(cap + (side == 0 ? j + 1 : j));
                }
            }
            Debug.Assert(index == Indices.Length);
        }

        internal void Prepare(GraphicsDevice device)
        {
            IndexBuffer = new IndexBuffer(device, IndexElementSize.SixteenBits,
                Indices.Length, BufferUsage.WriteOnly);
            IndexBuffer.SetData(Indices);
        }

        internal void Release()
        {
            if (IndexBuffer != null) { IndexBuffer.Dispose(); IndexBuffer = null; }
        }
    }

    internal sealed class FlexibleConnectionFittingPlan
    {
        private readonly int Sides, Segments;
        private readonly float HoseScale;
        private readonly FlexibleConnectionFittingSample[] Samples;
        private readonly FlexibleConnectionFittingBand[] Bands;
        internal readonly int CenterSample;
        internal readonly FlexibleConnectionFittingTopology Topology;

        // Updater-only reusable workspace, never referenced by a published frame.
        private readonly Vector3[] Positions, AxisY, OriginalTangents;
        private readonly Vector2[] Profile, FaceNormals;
        private bool Warned;
        private readonly string Label;

        // ORTS Flexible Connections
        // Purpose: Derive symmetric connected dimensions from one compatible local profile.
        internal static FlexibleConnectionFittingPlan Create(
            FlexibleConnectionPoint point, bool withFitting, int sides, int segments,
            Dictionary<long, FlexibleConnectionFittingTopology> topologies)
        {
            var profile = point.Profile;
            if (withFitting && profile.Fitting == null) return null;
            if (withFitting)
            {
                double center = profile.LengthM, length = 2.0 * center;
                var fitting = profile.Fitting;
                double a = center - fitting.LengthM, b = center + fitting.LengthM;
                double start = a - fitting.ExtrudeM, end = b + fitting.ExtrudeM;
                if (!(start > 0.0 && a < center && center < b && end < length) ||
                    (fitting.ExtrudeM > 0f && !(start < a && b < end)))
                {
                    Trace.TraceWarning("Flexible connection {0}:{1}: fitting cuts cannot be represented; using continuous two-material hose.",
                        point.End, point.ID);
                    return null;
                }
            }
            return new FlexibleConnectionFittingPlan(point, withFitting, sides, segments, topologies);
        }

        private FlexibleConnectionFittingPlan(FlexibleConnectionPoint point, bool withFitting,
            int sides, int segments, Dictionary<long, FlexibleConnectionFittingTopology> topologies)
        {
            Sides = sides;
            Segments = segments;
            Label = point.End + ":" + point.ID;
            var profile = point.Profile;
            double length = 2.0 * profile.LengthM;
            HoseScale = (float)(2.0 * profile.RadiusM * Math.Cos(Math.PI / sides));
            var fitting = withFitting ? profile.Fitting : null;
            float fittingScale = fitting == null ? HoseScale : (float)(2.0 * fitting.RadiusM * Math.Cos(Math.PI / sides));
            double c = length / 2.0;
            double a = fitting == null ? c : c - fitting.LengthM;
            double b = fitting == null ? c : c + fitting.LengthM;
            double extrude = fitting == null ? 0.0 : fitting.ExtrudeM;
            double start = a - extrude, end = b + extrude;

            // Constructor only: retain every base sample and add only these five cuts.
            var arcs = new List<double>(segments + 6);
            for (int i = 0; i <= segments; i++) arcs.Add(length * ((double)i / segments));
            arcs.Add(c);
            if (fitting != null) { arcs.Add(start); arcs.Add(a); arcs.Add(b); arcs.Add(end); }
            arcs.Sort();
            var samples = new List<FlexibleConnectionFittingSample>(arcs.Count);
            for (int i = 0; i < arcs.Count; i++)
                if (i == 0 || arcs[i] != arcs[i - 1])
                    samples.Add(new FlexibleConnectionFittingSample(arcs[i], length, segments));
            Samples = samples.ToArray();
            var bands = new List<FlexibleConnectionFittingBand>(segments + 5);
            CenterSample = -1;
            for (int i = 0; i < Samples.Length; i++)
            {
                double s = Samples[i].Arc;
                if (s == c) CenterSample = i;
                if (extrude == 0.0 && fittingScale != HoseScale)
                {
                    if (s == a)
                        bands.Add(new FlexibleConnectionFittingBand(i, i, HoseScale, fittingScale,
                            FlexibleConnectionFittingSurface.EntryShoulder));
                    if (s == b)
                        bands.Add(new FlexibleConnectionFittingBand(i, i, fittingScale, HoseScale,
                            FlexibleConnectionFittingSurface.ExitShoulder));
                }
                if (i + 1 == Samples.Length) break;
                double next = Samples[i + 1].Arc;
                // The explicit limits ensure no longitudinal band crosses a region boundary.
                FlexibleConnectionFittingSurface surface;
                float r0, r1;
                if (next <= start || s >= end)
                {
                    surface = FlexibleConnectionFittingSurface.Hose;
                    r0 = r1 = HoseScale;
                }
                else if (s >= a && next <= b)
                {
                    surface = FlexibleConnectionFittingSurface.Fitting;
                    r0 = r1 = fittingScale;
                }
                else if (next <= a)
                {
                    surface = FlexibleConnectionFittingSurface.EntryTransition;
                    r0 = InterpolateScale(HoseScale, fittingScale, (s - start) / extrude);
                    r1 = next == a ? fittingScale : InterpolateScale(HoseScale, fittingScale, (next - start) / extrude);
                }
                else
                {
                    surface = FlexibleConnectionFittingSurface.ExitTransition;
                    r0 = InterpolateScale(fittingScale, HoseScale, (s - b) / extrude);
                    r1 = next == end ? HoseScale : InterpolateScale(fittingScale, HoseScale, (next - b) / extrude);
                }
                bands.Add(new FlexibleConnectionFittingBand(i, i + 1, r0, r1, surface));
            }
            Bands = bands.ToArray();
            Debug.Assert(CenterSample >= 0);
            int firstBands = 0;
            foreach (var band in Bands) if (Samples[band.End.Sample].Arc <= c) firstBands++;
            long topologyKey = ((long)Bands.Length << 32) | (uint)firstBands;
            FlexibleConnectionFittingTopology topology;
            if (!topologies.TryGetValue(topologyKey, out topology))
            {
                topology = new FlexibleConnectionFittingTopology(sides, Bands.Length, firstBands);
                topologies.Add(topologyKey, topology);
            }
            Topology = topology;
            Positions = new Vector3[Samples.Length];
            AxisY = new Vector3[Samples.Length];
            OriginalTangents = new Vector3[segments + 1];
            Profile = new Vector2[sides];
            FaceNormals = new Vector2[sides];
            if (sides == 4)
            {
                Profile[0] = new Vector2(0.5f, -0.5f);
                Profile[1] = new Vector2(0.5f, 0.5f);
                Profile[2] = new Vector2(-0.5f, 0.5f);
                Profile[3] = new Vector2(-0.5f, -0.5f);
            }
            else
            {
                double halfAngle = Math.PI / sides;
                double angleStep = 2.0 * Math.PI / sides;
                double radius = 0.5 / Math.Cos(halfAngle);
                for (int j = 0; j < sides; j++)
                {
                    double angle = halfAngle + angleStep * j;
                    Profile[j] = new Vector2((float)(radius * Math.Cos(angle)), (float)(radius * Math.Sin(angle)));
                }
            }
            for (int j = 0; j < sides; j++)
            {
                Vector2 edge = Profile[(j + 1) % sides] - Profile[j];
                FaceNormals[j] = Vector2.Normalize(new Vector2(edge.Y, -edge.X));
            }
        }

        private static float InterpolateScale(float a, float b, double t)
        {
            if (t <= 0.0) return a;
            if (t >= 1.0) return b;
            return (float)(a + ((double)b - a) * t);
        }

        internal VertexPositionNormalTexture[] CreateVertices()
        {
            var vertices = new VertexPositionNormalTexture[Topology.VertexCount];
            for (int i = 0; i < Bands.Length; i++)
                for (int j = 0; j < Sides; j++)
                {
                    int offset = 4 * (i * Sides + j);
                    float u0 = (float)j / Sides, u1 = (float)(j + 1) / Sides;
                    float v0 = Samples[Bands[i].Start.Sample].V, v1 = Samples[Bands[i].End.Sample].V;
                    vertices[offset].TextureCoordinate = new Vector2(u0, v0);
                    vertices[offset + 1].TextureCoordinate = new Vector2(u1, v0);
                    vertices[offset + 2].TextureCoordinate = new Vector2(u0, v1);
                    vertices[offset + 3].TextureCoordinate = new Vector2(u1, v1);
                }
            return vertices;
        }

        internal bool Build(FlexibleConnectionCatenaryResult result,
            VertexPositionNormalTexture[] original, VertexPositionNormalTexture[] vertices)
        {
            Vector3[] points = result.Points;
            Vector3 horizontal, axisX;
            if (!Unit(new Vector3((float)((double)points[Segments].X - points[0].X), 0f,
                (float)((double)points[Segments].Z - points[0].Z)), out horizontal) ||
                !Unit(Vector3.Cross(Vector3.UnitY, horizontal), out axisX)) return false;

            // Exactly the original neighbor selection: extra cuts never change base frames.
            for (int i = 0; i <= Segments; i++)
            {
                int previous = i == 0 ? 0 : i - 1;
                int next = i == Segments ? Segments : i + 1;
                Vector3 direction = Difference(points[next], points[previous]);
                direction -= axisX * Vector3.Dot(direction, axisX);
                if (!Unit(direction, out OriginalTangents[i])) return false;
            }
            for (int i = 0; i < Samples.Length; i++)
            {
                var sample = Samples[i];
                Vector3 position, tangent;
                if (sample.Original >= 0)
                {
                    position = points[sample.Original];
                    tangent = OriginalTangents[sample.Original];
                }
                else
                {
                    if (!FlexibleConnectionCatenary.TryEvaluateAtArcLength(ref result.ArcContext,
                        sample.Arc, out position)) return false;
                    Vector3 direction = Vector3.Lerp(OriginalTangents[sample.Previous],
                        OriginalTangents[sample.Previous + 1], sample.Fraction);
                    direction -= axisX * Vector3.Dot(direction, axisX);
                    if (!Unit(direction, out tangent)) return false;
                }
                Positions[i] = Difference(position, points[0]);
                if (!Finite(Positions[i]) || !Unit(Vector3.Cross(tangent, axisX), out AxisY[i])) return false;
                // Distinct arc samples must remain distinct after conversion to float.
                if (i > 0 && Positions[i] == Positions[i - 1]) return false;
            }
            for (int i = 0; i < Bands.Length; i++)
            {
                var band = Bands[i];
                for (int j = 0; j < Sides; j++)
                {
                    int offset = 4 * (i * Sides + j);
                    Vector3 p0 = Corner(band.Start, j, axisX, original);
                    Vector3 p1 = Corner(band.Start, (j + 1) % Sides, axisX, original);
                    Vector3 p2 = Corner(band.End, j, axisX, original);
                    Vector3 p3 = Corner(band.End, (j + 1) % Sides, axisX, original);
                    if (!Finite(p0) || !Finite(p1) || !Finite(p2) || !Finite(p3)) return false;
                    vertices[offset].Position = p0; vertices[offset + 1].Position = p1;
                    vertices[offset + 2].Position = p2; vertices[offset + 3].Position = p3;
                    // A collapsed fitting face is a geometry failure and must never be submitted.
                    Vector3 first, second;
                    if (!Unit(Vector3.Cross(p3 - p0, p2 - p0), out first) ||
                        !Unit(Vector3.Cross(p1 - p0, p3 - p0), out second)) return false;
                    Vector3 n0, n1;
                    if (band.Surface == FlexibleConnectionFittingSurface.Hose ||
                        band.Surface == FlexibleConnectionFittingSurface.Fitting)
                    {
                        if (!RadialNormal(band.Start.Sample, j, axisX, original, out n0) ||
                            !RadialNormal(band.End.Sample, j, axisX, original, out n1)) return false;
                    }
                    else
                    {
                        // Clockwise exterior winding: use reversed crosses for outward normals.
                        if (!Unit(first + second, out n0)) return false;
                        n1 = n0;
                    }
                    vertices[offset].Normal = vertices[offset + 1].Normal = n0;
                    vertices[offset + 2].Normal = vertices[offset + 3].Normal = n1;
                }
            }
            // Copy the original endpoint caps, including positions, normals and UV.
            Array.Copy(original, 2 * Sides * (Segments + 1), vertices,
                4 * Sides * Bands.Length, 2 * Sides);
            return true;
        }

        private Vector3 Corner(FlexibleConnectionFittingRing ring, int corner,
            Vector3 axisX, VertexPositionNormalTexture[] original)
        {
            int index = Samples[ring.Sample].Original;
            if (index >= 0 && ring.Scale == HoseScale)
                return original[2 * (index * Sides + corner)].Position;
            return Positions[ring.Sample] + axisX * (Profile[corner].X * ring.Scale) +
                AxisY[ring.Sample] * (Profile[corner].Y * ring.Scale);
        }

        private bool RadialNormal(int sample, int face, Vector3 axisX,
            VertexPositionNormalTexture[] original, out Vector3 normal)
        {
            int index = Samples[sample].Original;
            if (index >= 0)
            {
                normal = original[2 * (index * Sides + face)].Normal;
                return true;
            }
            return Unit(axisX * FaceNormals[face].X + AxisY[sample] * FaceNormals[face].Y, out normal);
        }

        internal void WarnFailure()
        {
            if (Warned) return;
            Warned = true;
            Trace.TraceWarning("Flexible connection point {0}: procedural fitting could not be built for this pose. Using continuous two-material hose geometry; fitting will be retried on subsequent frames.", Label);
        }

        private static Vector3 Difference(Vector3 a, Vector3 b)
        {
            return new Vector3((float)((double)a.X - b.X),
                (float)((double)a.Y - b.Y), (float)((double)a.Z - b.Z));
        }

        private static bool Finite(Vector3 value)
        {
            return !float.IsNaN(value.X) && !float.IsInfinity(value.X) &&
                !float.IsNaN(value.Y) && !float.IsInfinity(value.Y) &&
                !float.IsNaN(value.Z) && !float.IsInfinity(value.Z);
        }

        private static bool Unit(Vector3 value, out Vector3 unit)
        {
            unit = Vector3.Zero;
            if (!Finite(value)) return false;
            double length = Math.Sqrt((double)value.X * value.X +
                (double)value.Y * value.Y + (double)value.Z * value.Z);
            if (double.IsNaN(length) || double.IsInfinity(length) || length <= 0.0) return false;
            unit = new Vector3((float)(value.X / length), (float)(value.Y / length), (float)(value.Z / length));
            return Finite(unit);
        }
    }

    // =====================================================================
    // ORTS Flexible Connections
    // Purpose: Rigid local Half meshes. All geometry is constructed once, lazily.
    // The anchor is OPEN; only the outward terminal has a cap.
    // =====================================================================
    internal enum FlexibleConnectionHalfGpuState { Pending, Preparing, Ready, Failed }

    internal sealed class FlexibleConnectionHalfTopology
    {
        internal readonly ushort[] Indices;
        internal readonly int VertexCount;
        internal IndexBuffer Buffer;

        internal FlexibleConnectionHalfTopology(int sides, int bands)
        {
            VertexCount = 4 * sides * bands + sides;
            if (VertexCount > ushort.MaxValue + 1) throw new ArgumentOutOfRangeException("bands");
            Indices = new ushort[6 * sides * bands + 3 * (sides - 2)];
            int k = 0;
            for (int i = 0; i < bands * sides; i++)
            {
                int a = 4 * i;
                Indices[k++] = (ushort)a; Indices[k++] = (ushort)(a + 2); Indices[k++] = (ushort)(a + 3);
                Indices[k++] = (ushort)a; Indices[k++] = (ushort)(a + 3); Indices[k++] = (ushort)(a + 1);
            }
            // Only the final cap. Independent vertices preserve its hard edge.
            int cap = 4 * sides * bands;
            for (int j = 1; j < sides - 1; j++)
            {
                Indices[k++] = (ushort)cap;
                Indices[k++] = (ushort)(cap + j + 1);
                Indices[k++] = (ushort)(cap + j);
            }
            Debug.Assert(k == Indices.Length);
        }

        internal void Prepare(GraphicsDevice device)
        {
            Buffer = new IndexBuffer(device, IndexElementSize.SixteenBits, Indices.Length, BufferUsage.WriteOnly);
            Buffer.SetData(Indices);
        }

        internal void Release()
        {
            if (Buffer != null) { Buffer.Dispose(); Buffer = null; }
        }
    }

    // ORTS Flexible Connections
    // Purpose: Use local lengths/fitting and full local UV, preserving the historical Half curve and grid.
    internal static class FlexibleConnectionHalfGeometry
    {
        internal static bool TryBuild(FlexibleConnectionHalfEndpoint endpoint, int sides, int segments,
            Dictionary<int, FlexibleConnectionHalfTopology> topologies,
            out FlexibleConnectionHalfTopology topology, out VertexPositionNormalTexture[] vertices)
        {
            topology = null; vertices = null;
            double half = endpoint.Profile.LengthM;
            double length = 2.0 * half;
            double bendRadius = half / (1.0 + Math.PI / 2.0);
            double bendEnd = Math.PI * bendRadius / 2.0;
            // ORTS Flexible Connections
            // Purpose: Apply Half FallAngle only at lazy construction; preserve the original zero-angle arithmetic.
            float fallAngleDegrees = endpoint.Profile.FallAngleDegrees;
            double alpha = 0.0, bendDrop = 0.0, bendOutward = 0.0;
            // ORTS Flexible Connections
            // Purpose: Generalize only nonvertical endings, retaining all existing 90-degree arithmetic.
            float finalAngleDegrees = endpoint.Profile.FallFinalAngleDegrees;
            double finalSin = 0.0, finalCos = 0.0;
            if (finalAngleDegrees < 90f)
            {
                alpha = fallAngleDegrees * (Math.PI / 180.0);
                double gamma = finalAngleDegrees * (Math.PI / 180.0);
                finalSin = Math.Sin(gamma);
                finalCos = Math.Cos(gamma);
                if (fallAngleDegrees == finalAngleDegrees)
                {
                    bendEnd = 0.0;
                }
                else
                {
                    double beta = gamma - alpha;
                    bendRadius = half / (1.0 + beta);
                    bendEnd = bendRadius * beta;
                    double chord = 2.0 * bendRadius * Math.Sin(beta / 2.0);
                    double middle = alpha + beta / 2.0;
                    bendDrop = chord * Math.Sin(middle);
                    bendOutward = chord * Math.Cos(middle);
                }
            }
            else if (fallAngleDegrees == 90f)
            {
                bendEnd = 0.0;
            }
            else if (fallAngleDegrees > 0f)
            {
                alpha = fallAngleDegrees * (Math.PI / 180.0);
                double beta = Math.PI / 2.0 - alpha;
                bendRadius = half / (1.0 + beta);
                bendEnd = bendRadius * beta;
                bendDrop = bendRadius * Math.Sin(beta);
                double sinHalfBeta = Math.Sin(beta / 2.0);
                bendOutward = 2.0 * bendRadius * sinHalfBeta * sinHalfBeta;
            }
            float hoseScale = (float)(2.0 * endpoint.Profile.RadiusM * Math.Cos(Math.PI / sides));
            float fittingScale = hoseScale;
            var fitting = endpoint.Profile.Fitting;
            double start = half, body = half, extrude = 0.0;
            if (fitting != null)
            {
                if ((double)fitting.LengthM + fitting.ExtrudeM >= half)
                {
                    Trace.TraceWarning("Flexible connection Half {0}: fitting does not fit effective Length. Rendering closed Half hose without fitting.", endpoint.PointID);
                    fitting = null;
                }
                else
                {
                    extrude = fitting.ExtrudeM;
                    body = half - fitting.LengthM;
                    start = body - extrude;
                    if (!(start > 0.0 && body < half) || (extrude > 0.0 && !(start < body))) return false;
                    fittingScale = (float)(2.0 * fitting.RadiusM * Math.Cos(Math.PI / sides));
                }
            }

            // First activation only: clip the original full-length grid at L/2.
            var arcs = new List<double>(segments / 2 + 6);
            for (int i = 0; i <= segments; i++)
            {
                double arc = length * ((double)i / segments);
                if (arc >= half) break;
                arcs.Add(arc);
            }
            arcs.Add(half); arcs.Add(bendEnd);
            if (fitting != null) { arcs.Add(start); arcs.Add(body); }
            arcs.Sort();
            var unique = new List<double>(arcs.Count);
            for (int i = 0; i < arcs.Count; i++)
                if (i == 0 || arcs[i] != arcs[i - 1]) unique.Add(arcs[i]);
            var bands = new List<FlexibleConnectionFittingBand>(unique.Count + 1);
            for (int i = 0; i < unique.Count - 1; i++)
            {
                double arc = unique[i], next = unique[i + 1];
                if (fitting != null && extrude == 0.0 && arc == body && fittingScale != hoseScale)
                    bands.Add(new FlexibleConnectionFittingBand(i, i, hoseScale, fittingScale,
                        FlexibleConnectionFittingSurface.EntryShoulder));
                float r0, r1;
                FlexibleConnectionFittingSurface surface;
                if (fitting == null || next <= start)
                {
                    r0 = r1 = hoseScale; surface = FlexibleConnectionFittingSurface.Hose;
                }
                else if (arc >= body)
                {
                    r0 = r1 = fittingScale; surface = FlexibleConnectionFittingSurface.Fitting;
                }
                else
                {
                    r0 = (float)(hoseScale + ((double)fittingScale - hoseScale) * ((arc - start) / extrude));
                    r1 = next == body ? fittingScale :
                        (float)(hoseScale + ((double)fittingScale - hoseScale) * ((next - start) / extrude));
                    surface = FlexibleConnectionFittingSurface.EntryTransition;
                }
                bands.Add(new FlexibleConnectionFittingBand(i, i + 1, r0, r1, surface));
            }

            var positions = new Vector3[unique.Count];
            var axisY = new Vector3[unique.Count];
            for (int i = 0; i < unique.Count; i++)
            {
                double arc = unique[i];
                // ORTS Flexible Connections
                // Purpose: Explicit vertical Half at 90 degrees; stable circular differences for positive angles.
                // Purpose: Follow the configured terminal tangent, including explicit straight equal-angle Halves.
                if (finalAngleDegrees < 90f)
                {
                    if (fallAngleDegrees == finalAngleDegrees)
                    {
                        positions[i] = new Vector3(0f, (float)(-arc * finalSin), (float)(arc * finalCos));
                        axisY[i] = new Vector3(0f, (float)finalCos, (float)finalSin);
                    }
                    else if (arc >= bendEnd)
                    {
                        double straight = arc - bendEnd;
                        positions[i] = new Vector3(0f, (float)(-bendDrop - straight * finalSin),
                            (float)(bendOutward + straight * finalCos));
                        axisY[i] = new Vector3(0f, (float)finalCos, (float)finalSin);
                    }
                    else
                    {
                        double delta = arc / bendRadius;
                        double theta = alpha + delta;
                        double middle = alpha + delta / 2.0;
                        double chord = 2.0 * bendRadius * Math.Sin(delta / 2.0);
                        positions[i] = new Vector3(0f, (float)(-chord * Math.Sin(middle)),
                            (float)(chord * Math.Cos(middle)));
                        axisY[i] = new Vector3(0f, (float)Math.Cos(theta), (float)Math.Sin(theta));
                    }
                }
                else if (fallAngleDegrees == 90f)
                {
                    positions[i] = new Vector3(0f, (float)-arc, 0f);
                    axisY[i] = Vector3.UnitZ;
                }
                else if (fallAngleDegrees > 0f)
                {
                    if (arc >= bendEnd)
                    {
                        positions[i] = new Vector3(0f, (float)(-bendDrop - (arc - bendEnd)), (float)bendOutward);
                        axisY[i] = Vector3.UnitZ;
                    }
                    else
                    {
                        double delta = arc / bendRadius;
                        double theta = alpha + delta;
                        double middle = alpha + delta / 2.0;
                        double chord = 2.0 * bendRadius * Math.Sin(delta / 2.0);
                        positions[i] = new Vector3(0f, (float)(-chord * Math.Sin(middle)),
                            (float)(chord * Math.Cos(middle)));
                        axisY[i] = new Vector3(0f, (float)Math.Cos(theta), (float)Math.Sin(theta));
                    }
                }
                else if (arc >= bendEnd)
                {
                    positions[i] = new Vector3(0f, (float)(-bendRadius - (arc - bendEnd)), (float)bendRadius);
                    axisY[i] = Vector3.UnitZ;
                }
                else
                {
                    double angle = arc / bendRadius;
                    positions[i] = new Vector3(0f, (float)(-bendRadius * (1.0 - Math.Cos(angle))),
                        (float)(bendRadius * Math.Sin(angle)));
                    axisY[i] = new Vector3(0f, (float)Math.Cos(angle), (float)Math.Sin(angle));
                }
                if (!Finite(positions[i]) || (i > 0 && positions[i] == positions[i - 1])) return false;
            }
            var profile = new Vector2[sides];
            var faceNormals = new Vector2[sides];
            if (sides == 4)
            {
                profile[0] = new Vector2(0.5f, -0.5f); profile[1] = new Vector2(0.5f, 0.5f);
                profile[2] = new Vector2(-0.5f, 0.5f); profile[3] = new Vector2(-0.5f, -0.5f);
            }
            else
            {
                double halfAngle = Math.PI / sides, step = 2.0 * Math.PI / sides;
                double radius = 0.5 / Math.Cos(halfAngle);
                for (int j = 0; j < sides; j++)
                    profile[j] = new Vector2((float)(radius * Math.Cos(halfAngle + step * j)),
                        (float)(radius * Math.Sin(halfAngle + step * j)));
            }
            for (int j = 0; j < sides; j++)
            {
                Vector2 edge = profile[(j + 1) % sides] - profile[j];
                faceNormals[j] = Vector2.Normalize(new Vector2(edge.Y, -edge.X));
            }
            bool newTopology = !topologies.TryGetValue(bands.Count, out topology);
            if (newTopology) topology = new FlexibleConnectionHalfTopology(sides, bands.Count);
            vertices = new VertexPositionNormalTexture[topology.VertexCount];
            for (int i = 0; i < bands.Count; i++)
            {
                var band = bands[i];
                for (int j = 0; j < sides; j++)
                {
                    int k = 4 * (i * sides + j), next = (j + 1) % sides;
                    Vector3 p0 = Corner(band.Start, j, positions, axisY, profile);
                    Vector3 p1 = Corner(band.Start, next, positions, axisY, profile);
                    Vector3 p2 = Corner(band.End, j, positions, axisY, profile);
                    Vector3 p3 = Corner(band.End, next, positions, axisY, profile);
                    if (!Finite(p0) || !Finite(p1) || !Finite(p2) || !Finite(p3)) return false;
                    Vector3 first, second, n0, n1;
                    if (!Unit(Vector3.Cross(p3 - p0, p2 - p0), out first) ||
                        !Unit(Vector3.Cross(p1 - p0, p3 - p0), out second)) return false;
                    if (band.Surface == FlexibleConnectionFittingSurface.Hose || band.Surface == FlexibleConnectionFittingSurface.Fitting)
                    {
                        if (!Unit(Vector3.UnitX * faceNormals[j].X + axisY[band.Start.Sample] * faceNormals[j].Y, out n0) ||
                            !Unit(Vector3.UnitX * faceNormals[j].X + axisY[band.End.Sample] * faceNormals[j].Y, out n1)) return false;
                    }
                    else
                    {
                        if (!Unit(first + second, out n0)) return false;
                        n1 = n0;
                    }
                    float u0 = (float)j / sides, u1 = (float)(j + 1) / sides;
                    float v0 = (float)(unique[band.Start.Sample] / half), v1 = (float)(unique[band.End.Sample] / half);
                    vertices[k] = new VertexPositionNormalTexture(p0, n0, new Vector2(u0, v0));
                    vertices[k + 1] = new VertexPositionNormalTexture(p1, n0, new Vector2(u1, v0));
                    vertices[k + 2] = new VertexPositionNormalTexture(p2, n1, new Vector2(u0, v1));
                    vertices[k + 3] = new VertexPositionNormalTexture(p3, n1, new Vector2(u1, v1));
                }
            }
            var terminal = bands[bands.Count - 1].End;
            // ORTS Flexible Connections
            // Purpose: Match the oriented terminal ring; retain the exact fixed normal for 90-degree endings.
            Vector3 terminalNormal = finalAngleDegrees < 90f
                ? new Vector3(0f, (float)-finalSin, (float)finalCos) : -Vector3.UnitY;
            for (int j = 0; j < sides; j++)
                vertices[4 * sides * bands.Count + j] = new VertexPositionNormalTexture(
                    Corner(terminal, j, positions, axisY, profile), terminalNormal, new Vector2(0.5f, 1f));
            // Failed meshes must not leave unused GPU topologies in the lazy bundle.
            if (newTopology) topologies.Add(bands.Count, topology);
            return true;
        }

        private static Vector3 Corner(FlexibleConnectionFittingRing ring, int corner,
            Vector3[] positions, Vector3[] axisY, Vector2[] profile)
        {
            return positions[ring.Sample] + Vector3.UnitX * (profile[corner].X * ring.Scale) +
                axisY[ring.Sample] * (profile[corner].Y * ring.Scale);
        }

        internal static bool Finite(Vector3 value)
        {
            return !float.IsNaN(value.X) && !float.IsInfinity(value.X) &&
                !float.IsNaN(value.Y) && !float.IsInfinity(value.Y) &&
                !float.IsNaN(value.Z) && !float.IsInfinity(value.Z);
        }

        private static bool Unit(Vector3 value, out Vector3 normal)
        {
            normal = Vector3.Zero;
            if (!Finite(value)) return false;
            double length = Math.Sqrt((double)value.X * value.X + (double)value.Y * value.Y + (double)value.Z * value.Z);
            if (double.IsNaN(length) || double.IsInfinity(length) || length <= 0.0) return false;
            normal = new Vector3((float)(value.X / length), (float)(value.Y / length), (float)(value.Z / length));
            return Finite(normal);
        }
    }

    internal sealed class FlexibleConnectionHalfResource
    {
        internal readonly FlexibleConnectionLifetime Owner;
        internal readonly FlexibleConnectionHalfTopology Topology;
        internal readonly VertexPositionNormalTexture[] Vertices;
        internal readonly Material Material;
        internal readonly Vector3 Anchor;
        internal readonly FlexibleConnectionSnapshot First, Second;
        internal readonly FlexibleConnectionHalfPrimitive Primitive;
        internal int FrameReferences;
        internal VertexBuffer Buffer;
        internal VertexBufferBinding[] Bindings;

        // ORTS Flexible Connections
        // Purpose: Retain immutable point identity for filtered publication from the shared Half bundle.
        internal readonly FlexibleConnectionEnd End;
        internal readonly string PointID;

        internal FlexibleConnectionHalfResource(FlexibleConnectionLifetime owner, FlexibleConnectionHalfEndpoint endpoint,
            FlexibleConnectionHalfTopology topology, VertexPositionNormalTexture[] vertices, Material material)
        {
            Owner = owner; Topology = topology; Vertices = vertices; Material = material;
            Anchor = new Vector3(endpoint.Point.X, endpoint.Point.Y, -endpoint.Point.Z);
            End = endpoint.End;
            PointID = endpoint.PointID;
            First = new FlexibleConnectionSnapshot(owner, this);
            Second = new FlexibleConnectionSnapshot(owner, this);
            Primitive = new FlexibleConnectionHalfPrimitive(this);
        }

        internal void Prepare(GraphicsDevice device)
        {
            Buffer = new VertexBuffer(device, VertexPositionNormalTexture.VertexDeclaration, Vertices.Length, BufferUsage.WriteOnly);
            Buffer.SetData(Vertices);
            Bindings = new[] { new VertexBufferBinding(Buffer), new VertexBufferBinding(RenderPrimitive.GetDummyVertexBuffer(device)) };
        }

        internal void Release()
        {
            if (Buffer != null) { Buffer.Dispose(); Buffer = null; }
            Bindings = null;
        }
    }

    internal sealed class FlexibleConnectionHalfPrimitive : RenderPrimitive
    {
        private readonly FlexibleConnectionHalfResource Resource;
        internal FlexibleConnectionHalfPrimitive(FlexibleConnectionHalfResource resource) { Resource = resource; }
        public override void Draw(GraphicsDevice graphicsDevice)
        {
            if (Resource.Buffer == null || Resource.Topology.Buffer == null)
                throw new InvalidOperationException("Flexible Connection Half GPU resources are not ready.");
            graphicsDevice.SetVertexBuffers(Resource.Bindings);
            graphicsDevice.Indices = Resource.Topology.Buffer;
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, Resource.Topology.Indices.Length / 3);
        }
    }

    internal sealed class FlexibleConnectionHalfBundle
    {
        internal readonly FlexibleConnectionHalfResource[] Resources;
        private readonly Dictionary<int, FlexibleConnectionHalfTopology> Topologies;
        internal FlexibleConnectionHalfGpuState State = FlexibleConnectionHalfGpuState.Pending;
        internal int FrameReferences;

        internal FlexibleConnectionHalfBundle(FlexibleConnectionLifetime owner, FlexibleConnectionConfig config,
            Dictionary<FlexibleConnectionLocalProfile, Material> materials, Material fallback)
        {
            Topologies = new Dictionary<int, FlexibleConnectionHalfTopology>();
            var resources = new List<FlexibleConnectionHalfResource>();
            foreach (var endpoint in config.HalfEndpoints)
            {
                FlexibleConnectionHalfTopology topology;
                VertexPositionNormalTexture[] vertices;
                if (!FlexibleConnectionHalfGeometry.Finite(endpoint.Point) ||
                    !FlexibleConnectionHalfGeometry.TryBuild(endpoint, config.Sides, config.Segments, Topologies, out topology, out vertices))
                {
                    Trace.TraceWarning("Flexible connection Half {0}:{1}: local geometry is not representable. This endpoint will not render Half.", endpoint.End, endpoint.PointID);
                    continue;
                }
                Material material;
                if (!materials.TryGetValue(endpoint.Profile, out material)) material = fallback;
                resources.Add(new FlexibleConnectionHalfResource(owner, endpoint, topology, vertices, material));
            }
            Resources = resources.ToArray();
        }

        internal void Prepare(GraphicsDevice device)
        {
            try
            {
                foreach (var topology in Topologies.Values) topology.Prepare(device);
                foreach (var resource in Resources) resource.Prepare(device);
            }
            catch
            {
                Release();
                throw;
            }
        }

        internal void Release()
        {
            foreach (var resource in Resources) resource.Release();
            foreach (var topology in Topologies.Values) topology.Release();
        }
    }

    // ORTS Flexible Connections
    // Purpose: Frame-owned CPU snapshots and deferred Render-thread GPU lifetime.
    // All ownership transitions use the manager gate; mathematical work stays outside.
    internal sealed class FlexibleConnectionSnapshot
    {
        internal readonly FlexibleConnectionLifetime Owner;
        internal readonly FlexibleConnectionSlots Connection;
        internal readonly FlexibleConnectionPrimitive PrimitiveA, PrimitiveB;
        internal Material MaterialA, MaterialB;
        internal VertexPositionNormalTexture[] ActiveVertices;
        internal readonly VertexPositionNormalTexture[] PlainVertices;
        internal long Version;
        internal RenderFrame Frame;
        internal FlexibleConnectionSnapshot NextInFrame;
        internal bool Building;
        internal Vector3 Origin;
        internal readonly VertexPositionNormalTexture[] Vertices;

        // ORTS Flexible Connections
        // Purpose: Keep both paths preallocated; only sealed snapshot state reaches Render.
        internal readonly VertexPositionNormalTexture[] FittingVertices;
        internal FlexibleConnectionFittingTopology ActiveTopology;

        internal FlexibleConnectionSnapshot(FlexibleConnectionLifetime owner, FlexibleConnectionSlots connection)
        {
            Owner = owner;
            Connection = connection;
            Vertices = owner.Geometry.CreateVertices();
            // ORTS Flexible Connections
            // Purpose: No extra vertex array exists for a point without a fitting.
            if (connection.Fitting != null)
                FittingVertices = connection.Fitting.CreateVertices();
            PlainVertices = connection.Plain.CreateVertices();
            PrimitiveA = new FlexibleConnectionPrimitive(this, false);
            PrimitiveB = new FlexibleConnectionPrimitive(this, true);
        }

        // ORTS Flexible Connections
        // Purpose: Half uses frame retention tokens without allocating Connected mesh arrays.
        internal readonly FlexibleConnectionHalfResource HalfResource;

        internal FlexibleConnectionSnapshot(FlexibleConnectionLifetime owner, FlexibleConnectionHalfResource resource)
        {
            Owner = owner;
            HalfResource = resource;
        }

        internal void Release(RenderFrame frame)
        {
            Owner.Manager.Release(this, frame);
        }
    }

    internal sealed class FlexibleConnectionSlots
    {
        internal readonly FlexibleConnectionSnapshot First;
        internal readonly FlexibleConnectionSnapshot Second;

        // ORTS Flexible Connections
        // Purpose: Stable per-point plan; workspace remains updater-only.
        internal readonly FlexibleConnectionFittingPlan Plain, Fitting;
        internal bool WarnedBase;

        // One GPU buffer per used local point, shared by both sealed CPU snapshots.
        internal DynamicVertexBuffer VertexBuffer;
        internal VertexBufferBinding[] Bindings;
        internal long PublishedVersion;
        internal long UploadedVersion;

        // ORTS Flexible Connections
        // Purpose: Set the optional plan before allocating the two snapshots.
        internal FlexibleConnectionSlots(FlexibleConnectionLifetime owner, FlexibleConnectionFittingPlan plain, FlexibleConnectionFittingPlan fitting)
        {
            Plain = plain;
            Fitting = fitting;
            First = new FlexibleConnectionSnapshot(owner, this);
            Second = new FlexibleConnectionSnapshot(owner, this);
        }

        internal void PrepareOnRenderThread(GraphicsDevice graphicsDevice, int vertexCount)
        {
            // ORTS Flexible Connections
            // Purpose: One GPU buffer accommodates either geometry without runtime recreation.
            vertexCount = Math.Max(vertexCount, Plain.Topology.VertexCount);
            if (Fitting != null)
                vertexCount = Math.Max(vertexCount, Fitting.Topology.VertexCount);
            VertexBuffer = new DynamicVertexBuffer(graphicsDevice,
                VertexPositionNormalTexture.VertexDeclaration, vertexCount, BufferUsage.WriteOnly);
            Bindings = new[] { new VertexBufferBinding(VertexBuffer),
                new VertexBufferBinding(RenderPrimitive.GetDummyVertexBuffer(graphicsDevice)) };
        }

        internal void ReleaseOnRenderThread()
        {
            if (VertexBuffer != null)
            {
                VertexBuffer.Dispose();
                VertexBuffer = null;
            }
            Bindings = null;
            UploadedVersion = 0;
        }
    }

    // ORTS Flexible Connections
    // Purpose: Stable per-slot draw proxies keep overlapping frames on their own snapshots.
    // Both proxies share the single connection-owned GPU buffer.
    internal sealed class FlexibleConnectionPrimitive : RenderPrimitive
    {
        private readonly FlexibleConnectionSnapshot Snapshot;

        private readonly bool SecondHalf;
        internal FlexibleConnectionPrimitive(FlexibleConnectionSnapshot snapshot, bool secondHalf)
        {
            Snapshot = snapshot;
            SecondHalf = secondHalf;
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            var connection = Snapshot.Connection;
            // Surface lifecycle/programming failures instead of silently using a segmented fallback.
            if (connection.VertexBuffer == null)
                throw new InvalidOperationException("Flexible Connection GPU resources are not prepared or have been released.");

            // ORTS Flexible Connections
            // Purpose: Select only frame-owned data; never inspect mutable updater plans here.
            var topology = Snapshot.ActiveTopology;
            var vertices = Snapshot.ActiveVertices;
            var indexBuffer = topology.IndexBuffer;
            int startIndex = SecondHalf ? topology.FirstIndexCount : 0;
            int indexCount = SecondHalf ? topology.Indices.Length - startIndex : topology.FirstIndexCount;
            if (indexBuffer == null)
                throw new InvalidOperationException("Flexible Connection fitting GPU resources are not prepared or have been released.");
            if (connection.UploadedVersion != Snapshot.Version || connection.VertexBuffer.IsContentLost)
            {
                connection.VertexBuffer.SetData(vertices, 0, vertices.Length,
                    SetDataOptions.Discard);
                connection.UploadedVersion = Snapshot.Version;
            }
            graphicsDevice.SetVertexBuffers(connection.Bindings);
            graphicsDevice.Indices = indexBuffer;
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                baseVertex: 0, startIndex: startIndex, primitiveCount: indexCount / 3);
        }
    }

    internal sealed class FlexibleConnectionLifetime
    {
        internal readonly FlexibleConnectionLifetimeManager Manager;
        internal readonly FlexibleConnectionViewer Visual;
        internal readonly List<FlexibleConnectionMaterialRequest> Materials = new List<FlexibleConnectionMaterialRequest>();
        internal readonly Dictionary<FlexibleConnectionPoint, FlexibleConnectionSlots> Connections =
            new Dictionary<FlexibleConnectionPoint, FlexibleConnectionSlots>();
        internal readonly FlexibleConnectionGeometry Geometry;
        // ORTS Flexible Connections
        // Purpose: Share immutable continuous topologies by band count and material boundary.
        private readonly Dictionary<long, FlexibleConnectionFittingTopology> FittingTopologies =
            new Dictionary<long, FlexibleConnectionFittingTopology>();
        internal bool Retired;
        internal bool Prepared;
        internal int FrameReferences;
        // ORTS Flexible Connections
        // Purpose: Half readiness is independent of the original one-time GPU preparation.
        internal bool HalfBuildAttempted;
        internal FlexibleConnectionHalfBundle Half;

        internal FlexibleConnectionLifetime(
            FlexibleConnectionLifetimeManager manager, FlexibleConnectionViewer visual,
            IEnumerable<FlexibleConnectionPoint> points, int sides, int segments)
        {
            Manager = manager;
            Visual = visual;
            Geometry = new FlexibleConnectionGeometry(sides, segments);
            // ORTS Flexible Connections
            // Purpose: Share one Connected resource across mappings using the same local point.
            foreach (var point in points)
            {
                var plain = FlexibleConnectionFittingPlan.Create(point, false, sides, segments, FittingTopologies);
                var fitting = FlexibleConnectionFittingPlan.Create(point, true, sides, segments, FittingTopologies);
                Connections.Add(point, new FlexibleConnectionSlots(this, plain, fitting));
            }
        }

        // ORTS Flexible Connections
        // Purpose: Use the solved context only during synchronous geometry construction.
        internal FlexibleConnectionSnapshot BuildSnapshot(
            RenderFrame frame, FlexibleConnectionPoint point, FlexibleConnectionCatenaryResult result,
            float scaleXY, float minimumLength, Material materialA, Material materialB)
        {
            var points = result.Points;
            var slot = Manager.Acquire(this, frame, point);
            if (slot == null)
                return null;

            bool valid = false;
            bool published = false;
            try
            {
                // No lifetime lock is held during geometry work. The reservation
                // keeps retirement pending until publication or cancellation.
                bool built = Geometry.Build(points, scaleXY, minimumLength, slot.Vertices);
                slot.ActiveTopology = null;
                slot.ActiveVertices = null;
                if (built)
                {
                    slot.Origin = points[0];
                    var fitting = slot.Connection.Fitting;
                    if (fitting != null && fitting.Build(result, slot.Vertices, slot.FittingVertices))
                    {
                        slot.ActiveTopology = fitting.Topology;
                        slot.ActiveVertices = slot.FittingVertices;
                    }
                    else
                    {
                        if (fitting != null) fitting.WarnFailure();
                        built = slot.Connection.Plain.Build(result, slot.Vertices, slot.PlainVertices);
                        if (built)
                        {
                            slot.ActiveTopology = slot.Connection.Plain.Topology;
                            slot.ActiveVertices = slot.PlainVertices;
                        }
                    }
                }
                if (!built && !slot.Connection.WarnedBase)
                {
                    slot.Connection.WarnedBase = true;
                    Trace.TraceWarning("Flexible connection {0}:{1}: continuous base geometry failed; not rendered.",
                        point.End, point.ID);
                }
                slot.MaterialA = materialA;
                slot.MaterialB = materialB;
                valid = built; // Publish only after all active geometry and material fields are complete.
            }
            finally
            {
                published = Manager.Complete(slot, frame, valid);
            }
            return published ? slot : null;
        }

        internal void RequestRetirement()
        {
            Manager.Retire(this);
        }

        internal void PrepareOnRenderThread(GraphicsDevice graphicsDevice)
        {
            try
            {
                // ORTS Flexible Connections
                // Purpose: Allocate static fitting indices once on Render under existing lifetime.
                foreach (var topology in FittingTopologies.Values)
                    topology.Prepare(graphicsDevice);
                foreach (var connection in Connections.Values)
                    connection.PrepareOnRenderThread(graphicsDevice, Geometry.VertexCount);
            }
            catch
            {
                // Dispose partial creation on Render, then preserve the failure.
                ReleaseOnRenderThread();
                throw;
            }
        }

        internal void ReleaseOnRenderThread()
        {
            foreach (var connection in Connections.Values)
                connection.ReleaseOnRenderThread();
            // ORTS Flexible Connections
            // Purpose: Release shared fitting indices after the existing reference retirement.
            foreach (var topology in FittingTopologies.Values)
                topology.Release();
            // ORTS Flexible Connections
            // Purpose: Retire cached Half GPU resources with the same protected owner.
            if (Half != null) Half.Release();
            // The shared native dummy buffer is not owned here.
        }
    }

    internal sealed class FlexibleConnectionLifetimeManager
    {
        // ORTS Flexible Connections
        // Purpose: Service Hook and Coupler owners through the shared rigid-resource manager
        // in FlexibleConnectionHookViewer.cs, independently of procedural Hose GPU resources.
        internal readonly FlexibleConnectionHookResourceManager Hooks = new FlexibleConnectionHookResourceManager();
        private readonly object Gate = new object();
        private readonly List<FlexibleConnectionLifetime> Owners =
            new List<FlexibleConnectionLifetime>();
        private bool Closed;

        internal FlexibleConnectionLifetime Register(FlexibleConnectionViewer visual, IEnumerable<FlexibleConnectionPoint> points, int sides, int segments)
        {
            var owner = new FlexibleConnectionLifetime(this, visual, points, sides, segments);
            lock (Gate)
            {
                owner.Retired = Closed;
                if (!Closed)
                    Owners.Add(owner);
            }
            return owner;
        }

        // ORTS Flexible Connections
        // Purpose: Reserve and load remote materials outside Draw and outside the manager lock.
        internal FlexibleConnectionMaterialRequest RequestMaterial(FlexibleConnectionLifetime owner,
            FlexibleConnectionLocalProfile profile, string folder)
        {
            lock (Gate)
            {
                if (Closed || owner.Retired) return null;
                foreach (var request in owner.Materials)
                    if (ReferenceEquals(request.Profile, profile) && string.Equals(request.Folder, folder, StringComparison.OrdinalIgnoreCase)) return request;
                var created = new FlexibleConnectionMaterialRequest(profile, folder);
                owner.Materials.Add(created);
                return created;
            }
        }

        // Loader only; retired owners cannot gain new publications. An in-flight load holds one reservation.
        internal void ProcessPendingMaterials()
        {
            // ORTS Flexible Connections
            // Purpose: Service Hook and Coupler shape requests even when no Hose material is pending.
            Hooks.ProcessPending();
            while (true)
            {
                FlexibleConnectionLifetime pending = null;
                FlexibleConnectionMaterialRequest request = null;
                lock (Gate)
                {
                    if (Closed) return;
                    foreach (var owner in Owners)
                    {
                        if (owner.Retired) continue;
                        foreach (var candidate in owner.Materials)
                            if (!candidate.Ready && !candidate.Loading) { pending = owner; request = candidate; break; }
                        if (request != null) break;
                    }
                    if (request == null) return;
                    request.Loading = true;
                    pending.FrameReferences++;
                }
                try
                {
                    var material = pending.Visual.LoadProfileMaterial(request.Profile, request.Folder);
                    lock (Gate)
                    {
                        request.Material = material;
                        request.Ready = true;
                    }
                }
                finally
                {
                    lock (Gate)
                    {
                        request.Loading = false;
                        pending.FrameReferences--;
                    }
                }
            }
        }

        // Loader only, between the native mark reset and sweep. Include retired owners with retained frames.
        internal void MarkMaterials()
        {
            // ORTS Flexible Connections
            // Purpose: Mark outside the Hose lock; retain Hook and Coupler assets used by pending frames.
            Hooks.Mark();
            lock (Gate)
                foreach (var owner in Owners)
                {
                    owner.Visual.Mark();
                    foreach (var request in owner.Materials)
                        if (request.Ready && request.Material != null) request.Material.Mark();
                }
        }

        // Updater only. A reservation is not yet a published frame reference.
        // The same counter protects both reservations and sealed frame references.
        internal FlexibleConnectionSnapshot Acquire(
            FlexibleConnectionLifetime owner, RenderFrame frame, FlexibleConnectionPoint point)
        {
            lock (Gate)
            {
                if (Closed || owner.Retired)
                    return null;

                FlexibleConnectionSlots slots;
                if (!owner.Connections.TryGetValue(point, out slots))
                    return null;

                if (slots.First.Frame == frame || slots.Second.Frame == frame)
                    return null;

                var slot = slots.First.Frame == null && !slots.First.Building ? slots.First :
                    slots.Second.Frame == null && !slots.Second.Building ? slots.Second : null;
                if (slot == null)
                    return null;

                slot.Building = true;
                owner.FrameReferences++;
                return slot;
            }
        }

        internal bool Complete(FlexibleConnectionSnapshot slot, RenderFrame frame, bool valid)
        {
            lock (Gate)
            {
                Debug.Assert(slot.Building && slot.Frame == null);
                slot.Building = false;
                if (!valid || Closed || slot.Owner.Retired)
                {
                    slot.Owner.FrameReferences--;
                    return false;
                }

                // Seal only complete geometry. Reusable catenary work buffers are never published to Render.
                slot.Version = ++slot.Connection.PublishedVersion;
                slot.Frame = frame;
                frame.RegisterFlexibleConnectionSnapshot(slot);
                return true;
            }
        }

        // =====================================================================
        // ORTS Flexible Connections
        // Purpose: CPU lazy reservations and Half frame references use the existing gate.
        // Resource construction and GPU work never hold this lock.
        // =====================================================================
        internal bool BeginHalfBuild(FlexibleConnectionLifetime owner)
        {
            lock (Gate)
            {
                if (Closed || owner.Retired || owner.HalfBuildAttempted) return false;
                owner.HalfBuildAttempted = true;
                owner.FrameReferences++; // Protect an in-flight CPU construction from retirement.
                return true;
            }
        }

        internal void CompleteHalfBuild(FlexibleConnectionLifetime owner, FlexibleConnectionHalfBundle half)
        {
            lock (Gate)
            {
                if (!Closed && !owner.Retired) owner.Half = half;
                owner.FrameReferences--;
            }
        }

        internal FlexibleConnectionHalfBundle GetHalf(FlexibleConnectionLifetime owner)
        {
            lock (Gate)
            {
                if (Closed || owner.Retired || owner.Half == null ||
                    owner.Half.State == FlexibleConnectionHalfGpuState.Failed) return null;
                return owner.Half;
            }
        }

        internal bool PublishHalf(FlexibleConnectionHalfResource resource, RenderFrame frame)
        {
            lock (Gate)
            {
                var owner = resource.Owner;
                if (Closed || owner.Retired || owner.Half == null ||
                    owner.Half.State == FlexibleConnectionHalfGpuState.Failed) return false;
                if (resource.First.Frame == frame || resource.Second.Frame == frame) return false;
                var token = resource.First.Frame == null ? resource.First :
                    resource.Second.Frame == null ? resource.Second : null;
                if (token == null) return false;
                owner.FrameReferences++;
                owner.Half.FrameReferences++;
                resource.FrameReferences++;
                token.Frame = frame;
                frame.RegisterFlexibleConnectionSnapshot(token);
                return true;
            }
        }

        internal void Release(FlexibleConnectionSnapshot slot, RenderFrame frame)
        {
            lock (Gate)
            {
                if (slot.Frame != frame)
                    return;
                slot.NextInFrame = null;
                slot.Frame = null;
                slot.Owner.FrameReferences--;
                // ORTS Flexible Connections
                // Purpose: Release only the Half-specific references for a Half token.
                if (slot.HalfResource != null)
                {
                    slot.HalfResource.FrameReferences--;
                    slot.Owner.Half.FrameReferences--;
                }
            }
        }

        internal void Retire(FlexibleConnectionLifetime owner)
        {
            lock (Gate)
                owner.Retired = true;
        }

        // Render only. No lock is held across GPU resource operations.
        internal void ProcessPending(GraphicsDevice graphicsDevice)
        {
            while (true)
            {
                FlexibleConnectionLifetime pending = null;
                bool release = false;
                // ORTS Flexible Connections
                // Purpose: A Prepared owner may have newly registered Half work.
                FlexibleConnectionHalfBundle halfPending = null;
                lock (Gate)
                {
                    if (Closed)
                        return;
                    for (int i = 0; i < Owners.Count; i++)
                    {
                        var owner = Owners[i];
                        if (owner.Retired && owner.FrameReferences == 0)
                        {
                            Owners.RemoveAt(i);
                            pending = owner;
                            release = true;
                            break;
                        }
                        // Retirement still rejects publication. Prepare only to serve
                        // existing references when retirement preceded GPU creation.
                        if (!owner.Prepared && (!owner.Retired || owner.FrameReferences > 0))
                        {
                            owner.Prepared = true;
                            pending = owner;
                            break;
                        }
                        // ORTS Flexible Connections
                        // Purpose: Retired owners can prepare Half only for already published frames.
                        if (owner.Half != null && owner.Half.State == FlexibleConnectionHalfGpuState.Pending &&
                            (!owner.Retired || owner.Half.FrameReferences > 0))
                        {
                            pending = owner;
                            halfPending = owner.Half;
                            halfPending.State = FlexibleConnectionHalfGpuState.Preparing;
                            owner.FrameReferences++; // Protect the GPU operation until completion.
                            break;
                        }
                    }
                }

                if (pending == null)
                    return;
                if (release)
                    pending.ReleaseOnRenderThread();
                // ORTS Flexible Connections
                // Purpose: GPU creation is exclusively on Render, never in a primitive's Draw.
                else if (halfPending != null)
                {
                    bool ready = false;
                    try
                    {
                        halfPending.Prepare(graphicsDevice);
                        ready = true;
                    }
                    finally
                    {
                        lock (Gate)
                        {
                            halfPending.State = ready ? FlexibleConnectionHalfGpuState.Ready : FlexibleConnectionHalfGpuState.Failed;
                            pending.FrameReferences--;
                        }
                    }
                }
                else
                    pending.PrepareOnRenderThread(graphicsDevice);
            }
        }

        // Render only, terminal shutdown: no later Draw or GPU creation is allowed.
        // CPU tokens may still be released by an updater finishing its last Clear.
        internal void Close()
        {
            lock (Gate)
                Closed = true;

            // ORTS Flexible Connections
            // Purpose: Close Hook and Coupler admission before any empty-Hose early return.
            Hooks.Close();
            while (true)
            {
                FlexibleConnectionLifetime owner;
                lock (Gate)
                {
                    if (Owners.Count == 0)
                        return;
                    int last = Owners.Count - 1;
                    owner = Owners[last];
                    Owners.RemoveAt(last);
                    owner.Retired = true;
                }
                owner.ReleaseOnRenderThread();
            }
        }
    }

}
