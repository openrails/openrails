// ORTS Flexible Connections
//
// Purpose: Define Hook content and construct deterministic planar rigid chains.
// Responsibilities: Validate Hook-only tokens, asset endpoints and ChainParity;
//   compute fixed-length joints without changing native coupling physics.
// Key components: FlexibleConnectionHookParser validates profiles; FlexibleConnectionHookGeometry.Prepare
//   checks Hook admission; FlexibleConnectionHookGeometry.BuildRigidChain constructs joints.
// Related files: FlexibleConnection.cs invokes the parser; FlexibleConnectionHookViewer.cs
//   supplies a catenary guide. FlexibleConnectionCouplerViewer.cs shares BuildRigidChain.
// Lifecycle: Parse once on loading; callers supply reusable geometry buffers per frame.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Xna.Framework;
using Orts.Parsers.Msts;

namespace Orts.Simulation.RollingStocks
{
    // ---------------------------------------------------------------------
    // Hook constants and immutable content data
    // ---------------------------------------------------------------------
    public static class FlexibleConnectionHookConstants
    {
        public const int MinimumLinks = 5;
        public const int MaximumLinks = 30;
        public const double LengthThreshold = 0.10;
        public const double GeometryTolerance = 0.002;
        public const double MaximumSlopeAngle = 40.0;
    }

    public sealed class FlexibleConnectionHookShapeConfig
    {
        public readonly string FileName;
        public readonly Vector3 P1, P2;
        public readonly double Length;

        internal FlexibleConnectionHookShapeConfig(string fileName, Vector3 p1, Vector3 p2)
        {
            FileName = fileName; P1 = p1; P2 = p2;
            double x = (double)p2.X - p1.X, y = (double)p2.Y - p1.Y, z = (double)p2.Z - p1.Z;
            Length = Math.Sqrt(x * x + y * y + z * z);
        }
    }

    public sealed class FlexibleConnectionHookConfig
    {
        // ORTS Flexible Connections
        // Purpose: Store the Hook sequence roll phase; zero preserves existing alternation.
        public readonly int ChainParity;
        public readonly int? Links;
        public readonly FlexibleConnectionHookShapeConfig LinkShape, HookShape;
        public bool CanPublish { get { return Links.HasValue && LinkShape != null && HookShape != null; } }

        // ORTS Flexible Connections
        // Purpose: Retain the validated ChainParity with the immutable Hook configuration.
        internal FlexibleConnectionHookConfig(int? links, FlexibleConnectionHookShapeConfig link, FlexibleConnectionHookShapeConfig hook, int chainParity)
        { Links = links; LinkShape = link; HookShape = hook; ChainParity = chainParity; }
    }

    // ---------------------------------------------------------------------
    // Strict Hook parsing; malformed declarations cannot be rehabilitated.
    // The existing Hose duplicate policy is intentionally not used here.
    // ---------------------------------------------------------------------
    internal sealed class FlexibleConnectionHookParser
    {
        private readonly HashSet<string> Seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool Invalid;
        private int IsHook;
        // ORTS Flexible Connections
        // Purpose: Default absent ChainParity to the existing zero-phase alternation.
        private int ChainParity = 0;
        // ORTS Flexible Connections
        // Purpose: Share strict Links syntax; defer its range until the profile family is known.
        internal int? Links { get; private set; }
        internal bool HasInvalidData { get { return Invalid; } }
        // ORTS Flexible Connections
        // Purpose: Include ChainParity in Hook exclusivity, including Coupler family validation.
        internal bool HasHookDeclarations { get { return Seen.Contains("IsHook") || Seen.Contains("LinkShape") || Seen.Contains("HookShape") || Seen.Contains("ChainParity"); } }
        private FlexibleConnectionHookShapeConfig Link, Hook;

        internal void Read(STFReader stf, string token, string profile)
        {
            bool unique = Seen.Add(token);
            List<string> items;
            bool valid = ReadBlock(stf, out items);
            // ORTS Flexible Connections
            // Purpose: Accept one integer 0 or 1 only; retain existing sticky invalid/duplicate handling.
            if (token == "ChainParity")
            {
                int value = 0;
                valid = valid && items.Count == 1 && int.TryParse(items[0], NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture, out value) && (value == 0 || value == 1);
                ChainParity = value;
            }
            else if (token == "IsHook" || token == "Links")
            {
                int value = 0;
                valid = valid && items.Count == 1 && int.TryParse(items[0], NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture, out value);
                if (token == "IsHook") IsHook = value;
                else
                {
                    Links = value;
                }
            }
            else
            {
                FlexibleConnectionHookShapeConfig shape;
                valid = ParseShape(items, out shape) && valid;
                if (token == "LinkShape") Link = shape; else Hook = shape;
            }
            if (!unique || !valid)
            {
                Invalid = true;
                STFException.TraceWarning(stf, "Hook Profile '" + profile + "': duplicate or invalid " + token + ". Profile rejected.");
            }
        }

        internal void RejectPointProperty(STFReader stf, string profile)
        {
            List<string> ignored;
            ReadBlock(stf, out ignored);
            Invalid = true;
            STFException.TraceWarning(stf, "Profile '" + profile + "': NotHookedShape belongs to a ConnectionPoint. Profile rejected.");
        }

        internal bool Resolve(STFReader stf, string profile, bool hasHoseTokens, out FlexibleConnectionHookConfig hook)
        {
            hook = null;
            // ORTS Flexible Connections
            // Purpose: Preserve Hook's range after the shared Links token has been read.
            if (Links.HasValue && (Links.Value < FlexibleConnectionHookConstants.MinimumLinks || Links.Value > FlexibleConnectionHookConstants.MaximumLinks))
            {
                Invalid = true;
                STFException.TraceWarning(stf, "Hook Profile '" + profile + "': duplicate or invalid Links. Profile rejected.");
            }
            // ORTS Flexible Connections
            // Purpose: Reject ChainParity on profiles that do not activate Hook.
            bool hookFields = Seen.Contains("Links") || Seen.Contains("LinkShape") || Seen.Contains("HookShape") || Seen.Contains("ChainParity");
            if ((IsHook == 1 && hasHoseTokens) || (IsHook != 1 && hookFields))
            {
                Invalid = true;
                STFException.TraceWarning(stf, "Profile '" + profile + "': incompatible Hook/Hose properties. Profile rejected.");
            }
            if (Invalid) return false;
            // ORTS Flexible Connections
            // Purpose: Publish the validated roll phase without changing Hook geometry inputs.
            if (IsHook == 1) hook = new FlexibleConnectionHookConfig(Links, Link, Hook, ChainParity);
            return true;
        }

        internal static bool ReadNotHookedShape(STFReader stf, out string fileName)
        {
            List<string> items;
            bool valid = ReadBlock(stf, out items);
            fileName = items.Count == 1 ? items[0] : null;
            return valid && ValidFileName(fileName);
        }

        private static bool ValidFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidPathChars()) >= 0 ||
                name.IndexOf(':') >= 0 || Path.IsPathRooted(name)) return false;
            return string.Equals(Path.GetExtension(name), ".s", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ParseShape(List<string> items, out FlexibleConnectionHookShapeConfig shape)
        { return ParseRigidShape(items, FlexibleConnectionHookConstants.GeometryTolerance, out shape); }

        // ORTS Flexible Connections
        // Purpose: Share canonical rigid asset syntax; Hook keeps its original tolerance wrapper.
        internal static bool ParseRigidShape(List<string> items, double tolerance, out FlexibleConnectionHookShapeConfig shape)
        {
            shape = null;
            // STF preserves quoted filenames as one item. Preserve the filename
            // through its extension, including spaces/commas inside that item;
            // tokenize separators only after the filename.
            var fields = new List<string>();
            if (items.Count == 0) return false;
            int extension = items[0].LastIndexOf(".s", StringComparison.OrdinalIgnoreCase);
            if (extension < 0) return false;
            int fileEnd = extension + 2;
            fields.Add(items[0].Substring(0, fileEnd));
            if (fileEnd < items[0].Length && items[0][fileEnd] != ',') return false;
            for (int itemIndex = 0; itemIndex < items.Count; itemIndex++)
            {
                string item = itemIndex == 0 ? items[0].Substring(fileEnd) : items[itemIndex];
                int start = 0;
                for (int i = 0; i < item.Length; i++)
                    if (item[i] == ',')
                    {
                        if (i > start) fields.Add(item.Substring(start, i - start));
                        fields.Add(","); start = i + 1;
                    }
                if (start < item.Length) fields.Add(item.Substring(start));
            }
            if (fields.Count != 9 || fields[1] != "," || fields[5] != "," || !ValidFileName(fields[0])) return false;
            var v = new float[6];
            for (int i = 0; i < v.Length; i++)
            {
                int index = i < 3 ? i + 2 : i + 3;
                if (!float.TryParse(fields[index], NumberStyles.Float, CultureInfo.InvariantCulture, out v[i]) ||
                    float.IsNaN(v[i]) || float.IsInfinity(v[i])) return false;
            }
            if (v[2] <= 0 || v[5] >= 0 || Math.Abs(v[0]) > tolerance || Math.Abs(v[1]) > tolerance ||
                Math.Abs(v[3]) > tolerance || Math.Abs(v[4]) > tolerance || Math.Abs(((double)v[2] + v[5]) * 0.5) > tolerance) return false;
            shape = new FlexibleConnectionHookShapeConfig(fields[0], new Vector3(v[0], v[1], v[2]), new Vector3(v[3], v[4], v[5]));
            return FlexibleConnectionHookGeometry.Finite(shape.Length) && shape.Length > 0;
        }

        // ORTS Flexible Connections
        // Purpose: Share balanced, strict leaf-block reading without changing Hook recovery.
        internal static bool ReadBlock(STFReader stf, out List<string> items)
        {
            items = new List<string>();
            if (stf.Eof) return false;
            string token = stf.ReadItem();
            if (token != "(") { stf.StepBackOneItem(); return false; }
            int depth = 1;
            bool valid = true;
            while (!stf.Eof)
            {
                token = stf.ReadItem();
                if (token == STFReader.EndBlockCommentSentinel) continue;
                if (token == "(") { depth++; valid = false; }
                else if (token == ")") { if (--depth == 0) return valid; }
                else if (depth == 1) items.Add(token);
            }
            return false;
        }
    }

    // ---------------------------------------------------------------------
    // Rigid chain geometry: double coordinates in the vertical A-B plane.
    // No rendering types, history, physics, scaling or alternative solver.
    // ---------------------------------------------------------------------
    public struct FlexibleConnectionHookVector
    {
        public double X, Y;
        public FlexibleConnectionHookVector(double x, double y) { X = x; Y = y; }
    }

    public enum FlexibleConnectionHookGeometryStatus
    {
        Success, InvalidInput, Degenerate, ExcessiveSlope, InsufficientLength, OutOfReach, NumericalFailure
    }

    public static class FlexibleConnectionHookGeometry
    {
        public static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }

        public static FlexibleConnectionHookGeometryStatus Prepare(double horizontal, double height,
            FlexibleConnectionHookConfig config, double[] lengths, double[] suffixMin, double[] suffixMax)
        {
            if (config == null || !config.CanPublish || !Finite(horizontal) || !Finite(height) || horizontal < 0)
                return FlexibleConnectionHookGeometryStatus.InvalidInput;
            int n = config.Links.Value;
            if (n < FlexibleConnectionHookConstants.MinimumLinks || n > FlexibleConnectionHookConstants.MaximumLinks ||
                lengths == null || lengths.Length < n || suffixMin == null || suffixMin.Length <= n || suffixMax == null || suffixMax.Length <= n)
                return FlexibleConnectionHookGeometryStatus.InvalidInput;
            double d = Math.Sqrt(horizontal * horizontal + height * height);
            if (!Finite(d)) return FlexibleConnectionHookGeometryStatus.InvalidInput;
            if (d <= 0 || horizontal <= 0) return FlexibleConnectionHookGeometryStatus.Degenerate;
            if (Math.Atan2(Math.Abs(height), horizontal) * (180.0 / Math.PI) > FlexibleConnectionHookConstants.MaximumSlopeAngle)
                return FlexibleConnectionHookGeometryStatus.ExcessiveSlope;
            suffixMin[n] = suffixMax[n] = 0;
            double largest = 0;
            for (int i = n - 1; i >= 0; i--)
            {
                double length = i == n / 2 ? config.HookShape.Length : config.LinkShape.Length;
                if (!Finite(length) || length <= 0) return FlexibleConnectionHookGeometryStatus.InvalidInput;
                lengths[i] = length;
                largest = Math.Max(largest, length);
                suffixMax[i] = suffixMax[i + 1] + length;
                suffixMin[i] = Math.Max(0, 2 * largest - suffixMax[i]);
            }
            if (!Finite(suffixMax[0])) return FlexibleConnectionHookGeometryStatus.InvalidInput;
            if (d < suffixMin[0] || d > suffixMax[0]) return FlexibleConnectionHookGeometryStatus.OutOfReach;
            if (suffixMax[0] < d * (1 + FlexibleConnectionHookConstants.LengthThreshold))
                return FlexibleConnectionHookGeometryStatus.InsufficientLength;
            return FlexibleConnectionHookGeometryStatus.Success;
        }

        public static FlexibleConnectionHookGeometryStatus Build(double horizontal, double height, int count,
            double[] lengths, double[] suffixMin, double[] suffixMax,
            FlexibleConnectionHookVector[] guide, FlexibleConnectionHookVector[] joints)
        {
            // ORTS Flexible Connections
            // Purpose: Enforce Hook piece-count admission before shared fixed-length construction.
            if (count < FlexibleConnectionHookConstants.MinimumLinks || count > FlexibleConnectionHookConstants.MaximumLinks)
                return FlexibleConnectionHookGeometryStatus.InvalidInput;
            return BuildRigidChain(horizontal, height, count, lengths, suffixMin, suffixMax, guide, joints);
        }

        // ORTS Flexible Connections
        // Purpose: Construct a planar chain of supplied fixed lengths for Hook or Coupler.
        // Callers provide validated family admission, suffix reach bounds and a geometric guide.
        // Each joint preserves remaining reach; circle intersections select the guide-nearest
        // candidate with deterministic tie-breaking. The guide does not set piece lengths.
        public static FlexibleConnectionHookGeometryStatus BuildRigidChain(double horizontal, double height, int count,
            double[] lengths, double[] suffixMin, double[] suffixMax,
            FlexibleConnectionHookVector[] guide, FlexibleConnectionHookVector[] joints)
        {
            if (!Finite(horizontal) || !Finite(height) || count < 1 || lengths == null || lengths.Length < count ||
                suffixMin == null || suffixMin.Length <= count || suffixMax == null || suffixMax.Length <= count ||
                guide == null || guide.Length <= count || joints == null || joints.Length <= count)
                return FlexibleConnectionHookGeometryStatus.InvalidInput;
            // Only roundoff is tolerated inside the reach construction. The 2 mm
            // acceptance tolerance is deliberately not used to enlarge reach.
            double epsilon = 1024 * 2.2204460492503131e-16 * Math.Max(1, suffixMax[0]);
            joints[0] = new FlexibleConnectionHookVector(0, 0);
            for (int i = 0; i < count; i++)
            {
                var q = joints[i];
                double bx = horizontal - q.X, by = height - q.Y;
                double d = Math.Sqrt(bx * bx + by * by), l = lengths[i];
                if (!Finite(d) || !Finite(l) || l <= 0 || !Finite(guide[i + 1].X) || !Finite(guide[i + 1].Y))
                    return FlexibleConnectionHookGeometryStatus.NumericalFailure;
                if (i == count - 1)
                {
                    if (Math.Abs(d - l) > epsilon) return FlexibleConnectionHookGeometryStatus.NumericalFailure;
                    joints[i + 1] = new FlexibleConnectionHookVector(horizontal, height);
                    continue;
                }
                double low = Math.Max(suffixMin[i + 1], Math.Abs(d - l));
                double high = Math.Min(suffixMax[i + 1], d + l);
                if (!Finite(low) || !Finite(high) || low > high + epsilon)
                    return FlexibleConnectionHookGeometryStatus.NumericalFailure;
                if (low > high) low = high = (low + high) * 0.5;
                double gx = guide[i + 1].X - horizontal, gy = guide[i + 1].Y - height;
                double r = Math.Max(low, Math.Min(high, Math.Sqrt(gx * gx + gy * gy)));
                if (d <= epsilon)
                {
                    // Coincident centers: choose the guide direction, or downward
                    // for an exact tie. No vertical-world fallback is introduced.
                    gx = guide[i + 1].X - q.X; gy = guide[i + 1].Y - q.Y;
                    double g = Math.Sqrt(gx * gx + gy * gy);
                    if (g <= epsilon) { gx = 0; gy = -1; g = 1; }
                    joints[i + 1] = new FlexibleConnectionHookVector(q.X + l * gx / g, q.Y + l * gy / g);
                }
                else
                {
                    double ux = bx / d, uy = by / d;
                    double along = (d + (l - r) * (l + r) / d) * 0.5;
                    double squared = (l - along) * (l + along);
                    if (squared < -epsilon * Math.Max(1, l)) return FlexibleConnectionHookGeometryStatus.NumericalFailure;
                    double across = Math.Sqrt(Math.Max(0, squared));
                    double x = q.X + along * ux, y = q.Y + along * uy;
                    var first = new FlexibleConnectionHookVector(x - across * uy, y + across * ux);
                    var second = new FlexibleConnectionHookVector(x + across * uy, y - across * ux);
                    double a = DistanceSquared(first, guide[i + 1]), b = DistanceSquared(second, guide[i + 1]);
                    joints[i + 1] = a < b || (a == b && (first.Y < second.Y || (first.Y == second.Y && first.X <= second.X))) ? first : second;
                }
                if (!Finite(joints[i + 1].X) || !Finite(joints[i + 1].Y)) return FlexibleConnectionHookGeometryStatus.NumericalFailure;
            }
            for (int i = 0; i < count; i++)
                if (Math.Abs(Math.Sqrt(DistanceSquared(joints[i], joints[i + 1])) - lengths[i]) > epsilon)
                    return FlexibleConnectionHookGeometryStatus.NumericalFailure;
            return FlexibleConnectionHookGeometryStatus.Success;
        }

        private static double DistanceSquared(FlexibleConnectionHookVector a, FlexibleConnectionHookVector b)
        { double x = a.X - b.X, y = a.Y - b.Y; return x * x + y * y; }
    }
}
