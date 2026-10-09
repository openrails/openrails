// ORTS Flexible Connections
//
// Purpose: Define screw-coupling content and Coupler-specific geometric admission.
// Responsibilities: Validate Coupler properties and ordered rigid assets, independently
//   of Hook admission rules; this cosmetic model does not change native physics or slack.
// Key components: FlexibleConnectionCouplerParser reads profiles;
//   FlexibleConnectionCouplerGeometry.Prepare supplies lengths and remaining reach bounds.
// Related files: FlexibleConnection.cs selects the family; FlexibleConnectionHook.cs
//   provides shared data types; FlexibleConnectionCouplerViewer.cs consumes the configuration.
// Lifecycle: Load content once; prepare geometric input before shared rigid-chain construction.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using Orts.Parsers.Msts;

namespace Orts.Simulation.RollingStocks
{
    public enum FlexibleConnectionFamily { Hose, Hook, Coupler }

    public static class FlexibleConnectionCouplerConstants
    {
        public const int MinimumCouplerLinks = 1;
        public const int MaximumCouplerLinks = 5;
        public const double CouplerGeometryTolerance = 0.002;
        public const double MaximumCouplerSlopeAngle = 40.0;
        // Sine of the angle between normalized publisher-up and segment direction.
        // Dimensionless: reject unstable transverse normalization, not short pieces.
        public const double MinimumCouplerOrientationCrossMagnitude = 1e-4;
    }

    public sealed class FlexibleConnectionCouplerConfig
    {
        public readonly int? Links;
        // Reuse immutable canonical P1/P2 metadata, not Hook's layout or policies.
        public readonly ReadOnlyCollection<FlexibleConnectionHookShapeConfig> Shapes;
        public bool CanPublish { get { return Links.HasValue && Shapes != null && Shapes.Count == Links.Value; } }

        internal FlexibleConnectionCouplerConfig(int? links, List<FlexibleConnectionHookShapeConfig> shapes)
        {
            Links = links;
            Shapes = shapes == null ? null : new List<FlexibleConnectionHookShapeConfig>(shapes).AsReadOnly();
        }
    }

    internal sealed class FlexibleConnectionCouplerParser
    {
        private readonly HashSet<string> Seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool Invalid;
        private int IsCoupler;
        private List<FlexibleConnectionHookShapeConfig> Shapes;

        internal void Read(STFReader stf, string token, string profile)
        {
            bool unique = Seen.Add(token);
            bool valid;
            if (token == "CouplerShapes") valid = ReadShapes(stf);
            else
            {
                List<string> items;
                valid = FlexibleConnectionHookParser.ReadBlock(stf, out items);
                int value = 0;
                valid = valid && items.Count == 1 && int.TryParse(items[0], NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture, out value);
                IsCoupler = value;
            }
            if (!unique || !valid)
            {
                Invalid = true;
                STFException.TraceWarning(stf, "Coupler Profile '" + profile + "': duplicate or invalid " + token + ". Profile rejected.");
            }
        }

        internal void RejectPointProperty(STFReader stf, string profile)
        {
            List<string> ignored;
            FlexibleConnectionHookParser.ReadBlock(stf, out ignored);
            Invalid = true;
            STFException.TraceWarning(stf, "Profile '" + profile + "': NotCoupledShape belongs to a ConnectionPoint. Profile rejected.");
        }

        private bool ReadShapes(STFReader stf)
        {
            Shapes = new List<FlexibleConnectionHookShapeConfig>();
            if (stf.Eof) return false;
            if (stf.ReadItem() != "(") { stf.StepBackOneItem(); return false; }
            bool valid = true;
            while (!stf.Eof)
            {
                string token = stf.ReadItem();
                if (token == STFReader.EndBlockCommentSentinel) continue;
                if (token == ")") return valid && Shapes.Count >= FlexibleConnectionCouplerConstants.MinimumCouplerLinks;
                if (string.Equals(token, "Shape", StringComparison.OrdinalIgnoreCase))
                {
                    List<string> items;
                    bool blockValid = FlexibleConnectionHookParser.ReadBlock(stf, out items);
                    FlexibleConnectionHookShapeConfig shape;
                    bool shapeValid = FlexibleConnectionHookParser.ParseRigidShape(items,
                        FlexibleConnectionCouplerConstants.CouplerGeometryTolerance, out shape);
                    if (!blockValid || !shapeValid) valid = false;
                    else if (Shapes.Count < FlexibleConnectionCouplerConstants.MaximumCouplerLinks) Shapes.Add(shape);
                    else valid = false;
                }
                else
                {
                    valid = false;
                    // Consume unexpected nested blocks completely, without stealing our closing ')'.
                    if (token == "(")
                    {
                        stf.StepBackOneItem();
                        List<string> ignored;
                        FlexibleConnectionHookParser.ReadBlock(stf, out ignored);
                    }
                }
            }
            return false;
        }

        internal bool Resolve(STFReader stf, string profile, FlexibleConnectionHookParser hook,
            bool hasHoseTokens, out FlexibleConnectionCouplerConfig coupler)
        {
            coupler = null;
            bool active = IsCoupler == 1;
            if ((Seen.Contains("IsCoupler") && hook.HasHookDeclarations) ||
                (active && hasHoseTokens) || (!active && Seen.Contains("CouplerShapes")))
            {
                Invalid = true;
                STFException.TraceWarning(stf, "Profile '" + profile + "': incompatible Coupler/Hook/Hose properties. Profile rejected.");
            }
            if (active && hook.Links.HasValue &&
                (hook.Links.Value < FlexibleConnectionCouplerConstants.MinimumCouplerLinks ||
                 hook.Links.Value > FlexibleConnectionCouplerConstants.MaximumCouplerLinks))
            {
                Invalid = true;
                STFException.TraceWarning(stf, "Coupler Profile '" + profile + "': Links must be an integer in 1..5. Profile rejected.");
            }
            if (active && hook.Links.HasValue && Shapes != null && hook.Links.Value != Shapes.Count)
            {
                Invalid = true;
                STFException.TraceWarning(stf, "Coupler Profile '" + profile + "': Links and CouplerShapes count differ. Profile rejected.");
            }
            if (Invalid || hook.HasInvalidData) return false;
            if (active) coupler = new FlexibleConnectionCouplerConfig(hook.Links, Shapes);
            return true;
        }
    }

    public static class FlexibleConnectionCouplerGeometry
    {
        public static FlexibleConnectionHookGeometryStatus Prepare(double horizontal, double height,
            FlexibleConnectionCouplerConfig config, double[] lengths, double[] suffixMin, double[] suffixMax)
        {
            if (config == null || !config.CanPublish || !FlexibleConnectionHookGeometry.Finite(horizontal) ||
                !FlexibleConnectionHookGeometry.Finite(height) || horizontal < 0)
                return FlexibleConnectionHookGeometryStatus.InvalidInput;
            int count = config.Links.Value;
            if (count < FlexibleConnectionCouplerConstants.MinimumCouplerLinks || count > FlexibleConnectionCouplerConstants.MaximumCouplerLinks ||
                lengths == null || lengths.Length < count || suffixMin == null || suffixMin.Length <= count || suffixMax == null || suffixMax.Length <= count)
                return FlexibleConnectionHookGeometryStatus.InvalidInput;
            double distance = Math.Sqrt(horizontal * horizontal + height * height);
            if (!FlexibleConnectionHookGeometry.Finite(distance)) return FlexibleConnectionHookGeometryStatus.InvalidInput;
            if (distance <= 0 || horizontal <= 0) return FlexibleConnectionHookGeometryStatus.Degenerate;
            if (Math.Atan2(Math.Abs(height), horizontal) * (180.0 / Math.PI) > FlexibleConnectionCouplerConstants.MaximumCouplerSlopeAngle)
                return FlexibleConnectionHookGeometryStatus.ExcessiveSlope;
            suffixMin[count] = suffixMax[count] = 0;
            double largest = 0;
            for (int i = count - 1; i >= 0; i--)
            {
                double length = config.Shapes[i].Length;
                if (!FlexibleConnectionHookGeometry.Finite(length) || length <= 0) return FlexibleConnectionHookGeometryStatus.InvalidInput;
                lengths[i] = length;
                largest = Math.Max(largest, length);
                suffixMax[i] = suffixMax[i + 1] + length;
                suffixMin[i] = Math.Max(0, 2 * largest - suffixMax[i]);
            }
            if (!FlexibleConnectionHookGeometry.Finite(suffixMax[0])) return FlexibleConnectionHookGeometryStatus.InvalidInput;
            // Strict rigid reach: neither the represented-endpoint tolerance nor Hook's 10% margin applies.
            if (distance < suffixMin[0] || distance > suffixMax[0]) return FlexibleConnectionHookGeometryStatus.OutOfReach;
            return FlexibleConnectionHookGeometryStatus.Success;
        }
    }
}
