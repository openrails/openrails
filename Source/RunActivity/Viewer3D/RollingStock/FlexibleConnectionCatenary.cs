// ORTS Flexible Connections
//
// Purpose: Solve a static catenary centerline independently of rendering resources.
// Responsibilities: Validate endpoints and length, solve the curve, and sample arc positions.
// Key components: Calculate resets a caller-owned FlexibleConnectionCatenaryResult and
//   fills its point buffer; TryEvaluateAtArcLength samples the retained solved context.
// Related files: FlexibleConnectionViewer.cs builds Hose geometry from these samples;
//   FlexibleConnectionHookViewer.cs and FlexibleConnectionCouplerViewer.cs use them as a guide.
// Lifecycle: Reuse caller-owned work buffers synchronously during frame preparation;
//   this file neither allocates GPU resources nor publishes work buffers to Render.

using System;
using Microsoft.Xna.Framework;

namespace Orts.Viewer3D.RollingStock
{
    internal enum FlexibleConnectionCatenaryStatus
    {
        Success,
        NoSlack,
        Degenerate,
        InvalidInput,
        NumericalFailure
    }

    // =====================================================================
    // ORTS Flexible Connections
    // Purpose: Value-only context for extra samples of an already solved curve.
    // =====================================================================
    internal struct FlexibleConnectionArcContext
    {
        internal bool Valid;
        internal Vector3 Start, End;
        internal double Length, A, Q0, Radius0, HorizontalX, HorizontalZ;
        internal double Horizontal, PositionTolerance;
    }

    internal sealed class FlexibleConnectionCatenaryResult
    {
        internal FlexibleConnectionCatenaryStatus Status;
        internal string Cause = string.Empty;

        // Populated only after all validation succeeds.
        internal Vector3[] Points;

        // ORTS Flexible Connections
        // Purpose: No references or allocations are retained by the extra evaluator.
        internal FlexibleConnectionArcContext ArcContext;

        internal double Distance = double.NaN;
        internal double Horizontal = double.NaN;
        internal double HeightDifference = double.NaN;

        internal double V = double.NaN;
        internal double A = double.NaN;
        internal double X0 = double.NaN;
        internal int Iterations;

        internal double LengthResidual = double.NaN;
        internal double PolylineLength = double.NaN;
        internal double Deficit = double.NaN;
        internal double MinimumSegmentLength = double.NaN;
        internal double MaximumSegmentLength = double.NaN;

        // ORTS Flexible Connections
        // Purpose: Restore fresh-result state before reusing this calculation storage.
        internal void Reset()
        {
            Status = FlexibleConnectionCatenaryStatus.Success;
            Cause = string.Empty;
            Points = null;
            // ORTS Flexible Connections
            // Purpose: Early failures must never expose the preceding pair's solution.
            ArcContext = default(FlexibleConnectionArcContext);
            Distance = double.NaN;
            Horizontal = double.NaN;
            HeightDifference = double.NaN;
            V = double.NaN;
            A = double.NaN;
            X0 = double.NaN;
            Iterations = 0;
            LengthResidual = double.NaN;
            PolylineLength = double.NaN;
            Deficit = double.NaN;
            MinimumSegmentLength = double.NaN;
            MaximumSegmentLength = double.NaN;
        }
    }

    internal static class FlexibleConnectionCatenary
    {
        private const int MaximumIterations = 64;
        private const double MaximumV = 64.0;
        private const double RootRelativeTolerance = 1e-10;
        private const double FloatRelativePrecision = 1.1920928955078125e-7;

        // ORTS Flexible Connections
        // Purpose: Use caller-owned storage; consume the result before the next call.
        // pointBuffer must contain segmentCount + 1 entries and remain updater-only.
        internal static FlexibleConnectionCatenaryResult Calculate(
            Vector3 ca,
            Vector3 cb,
            double length,
            int segmentCount,
            FlexibleConnectionCatenaryResult result,
            Vector3[] pointBuffer)
        {
            result.Reset();

            if (!IsFinite(ca) || !IsFinite(cb) ||
                !IsFinite(length) || length <= 0.0 ||
                segmentCount < 1 || segmentCount == int.MaxValue)
            {
                return Fail(
                    result,
                    FlexibleConnectionCatenaryStatus.InvalidInput,
                    "InvalidEndpointsLengthOrSegmentCount");
            }

            // Convert before subtracting: do not subtract in Vector3/float.
            double dx = (double)cb.X - ca.X;
            double h = (double)cb.Y - ca.Y;
            double dz = (double)cb.Z - ca.Z;

            double horizontal = Hypot(dx, dz);
            double distance = Hypot(horizontal, h);

            result.Distance = distance;
            result.Horizontal = horizontal;
            result.HeightDifference = h;

            if (!IsFinite(horizontal) || !IsFinite(distance))
            {
                return Fail(
                    result,
                    FlexibleConnectionCatenaryStatus.NumericalFailure,
                    "NonFiniteDistance");
            }

            if (distance >= length)
            {
                return Fail(
                    result,
                    FlexibleConnectionCatenaryStatus.NoSlack,
                    "DistanceGreaterThanOrEqualToLength");
            }

            double scale = Math.Max(1.0, length);

            // Numerical tolerance of this prototype, not a physical constant.
            double minimumHorizontal = 1e-6 * scale;

            if (distance <= minimumHorizontal)
            {
                return Fail(
                    result,
                    FlexibleConnectionCatenaryStatus.Degenerate,
                    "CoincidentOrNearlyCoincidentEndpoints");
            }

            if (horizontal <= minimumHorizontal)
            {
                return Fail(
                    result,
                    FlexibleConnectionCatenaryStatus.Degenerate,
                    "HorizontalSpanTooSmall");
            }

            double absoluteH = Math.Abs(h);
            double lengthMinusH = length - absoluteH;
            double lengthPlusH = length + absoluteH;

            if (!(lengthMinusH > 0.0) || !IsFinite(lengthPlusH))
            {
                return Fail(
                    result,
                    FlexibleConnectionCatenaryStatus.NumericalFailure,
                    "InvalidReducedLength");
            }

            double reducedLength =
                Math.Sqrt(lengthMinusH) * Math.Sqrt(lengthPlusH);

            double ratio = reducedLength / horizontal;
            double target = ratio - 1.0;

            // Near tautness, avoid cancellation in R - 1.
            // Restrict this alternative to moderate slopes: for steep
            // geometry, rounding in D is amplified by division by H squared.
            if (target < 1e-4 && absoluteH <= horizontal)
            {
                target =
                    ((length - distance) / horizontal) *
                    ((length + distance) / horizontal) /
                    (ratio + 1.0);
            }

            if (!IsFinite(ratio) || !IsFinite(target) || target <= 0.0)
            {
                return Fail(
                    result,
                    FlexibleConnectionCatenaryStatus.NumericalFailure,
                    "UnresolvablePositiveSlack");
            }

            double lower = 0.0;
            double upper = 1.0;
            double upperResidual = SinhOverXMinusOne(upper) - target;

            while (IsFinite(upperResidual) &&
                   upperResidual < 0.0 &&
                   upper < MaximumV)
            {
                upper *= 2.0;
                upperResidual = SinhOverXMinusOne(upper) - target;
            }

            if (!IsFinite(upperResidual) || upperResidual < 0.0)
            {
                return Fail(
                    result,
                    FlexibleConnectionCatenaryStatus.NumericalFailure,
                    "RootNotBracketed");
            }

            double v = double.NaN;
            bool converged = false;

            for (int iteration = 1;
                 iteration <= MaximumIterations;
                 iteration++)
            {
                result.Iterations = iteration;
                v = 0.5 * (lower + upper);

                double residual = SinhOverXMinusOne(v) - target;

                if (!IsFinite(residual))
                {
                    return Fail(
                        result,
                        FlexibleConnectionCatenaryStatus.NumericalFailure,
                        "NonFiniteRootResidual");
                }

                if (residual == 0.0)
                {
                    converged = true;
                    break;
                }

                if (residual > 0.0)
                    upper = v;
                else
                    lower = v;

                if (upper - lower <=
                    RootRelativeTolerance * Math.Max(v, 1e-12))
                {
                    v = 0.5 * (lower + upper);
                    converged = true;
                    break;
                }
            }

            if (!converged || !IsFinite(v) || v <= 0.0)
            {
                return Fail(
                    result,
                    FlexibleConnectionCatenaryStatus.NumericalFailure,
                    "RootDidNotConverge");
            }

            double a = horizontal / (2.0 * v);

            // Equivalent to 0.5 * log((L + h) / (L - h)).
            // Do not first compute h/L near vertical geometry.
            double m = Math.Sign(h) * 0.5 *
                LogOnePlus(2.0 * absoluteH / lengthMinusH);

            double x0 = horizontal * 0.5 - a * m;
            double q0 = a * Math.Sinh(m - v);
            double radius0 = Hypot(a, q0);

            result.V = v;
            result.A = a;
            result.X0 = x0;

            if (!IsFinite(a) || a <= 0.0 ||
                !IsFinite(m) || !IsFinite(x0) ||
                !IsFinite(q0) || !IsFinite(radius0))
            {
                return Fail(
                    result,
                    FlexibleConnectionCatenaryStatus.NumericalFailure,
                    "NonFiniteCatenaryParameters");
            }

            // L = H * (sinh(v)/v) * cosh(m).
            double analyticalLength =
                horizontal * (1.0 + SinhOverXMinusOne(v)) * Math.Cosh(m);

            result.LengthResidual = analyticalLength - length;

            double solverLengthTolerance = 1e-9 * scale;
            double analyticalPositionTolerance = 1e-8 * scale;

            if (!IsFinite(analyticalLength) ||
                Math.Abs(result.LengthResidual) > solverLengthTolerance)
            {
                return Fail(
                    result,
                    FlexibleConnectionCatenaryStatus.NumericalFailure,
                    "AnalyticalLengthMismatch");
            }

            double horizontalX = dx / horizontal;
            double horizontalZ = dz / horizontal;

            // Coordinate-dependent allowance for conversion to Vector3/float.
            double coordinateScale = Math.Max(
                scale, Math.Max(MaxAbs(ca), MaxAbs(cb)));

            double floatPositionTolerance =
                1e-6 * scale +
                4.0 * FloatRelativePrecision * coordinateScale;

            var points = pointBuffer;

            double previousX = 0.0;
            double previousY = 0.0;
            double analyticalPolylineLength = 0.0;

            for (int i = 0; i <= segmentCount; i++)
            {
                double s = length * ((double)i / segmentCount);
                double q = q0 + s;
                double radius = Hypot(a, q);
                double denominator = radius + radius0;
                double t = s / denominator;

                if (!IsFinite(q) || !IsFinite(radius) ||
                    !IsFinite(denominator) || denominator <= 0.0 ||
                    !IsFinite(t) || t < 0.0)
                {
                    return Fail(
                        result,
                        FlexibleConnectionCatenaryStatus.NumericalFailure,
                        "InvalidArcSample");
                }

                // Avoid subtracting nearly equal asinh values near tautness.
                // For t near one, use asinh instead of atanh.
                double x = t < 0.5
                    ? 2.0 * a * Atanh(t)
                    : a * (Asinh(q / a) - Asinh(q0 / a));

                // Rationalized difference of square roots.
                double y = s * ((2.0 * q0 + s) / denominator);

                if (!IsFinite(x) || !IsFinite(y) ||
                    x < -analyticalPositionTolerance ||
                    x > horizontal + analyticalPositionTolerance ||
                    (i > 0 &&
                     x < previousX - analyticalPositionTolerance))
                {
                    return Fail(
                        result,
                        FlexibleConnectionCatenaryStatus.NumericalFailure,
                        "InvalidOrNonMonotonicSample");
                }

                // Validate the analytical endpoints before any anchoring.
                if ((i == 0 &&
                     Hypot(x, y) > analyticalPositionTolerance) ||
                    (i == segmentCount &&
                     Hypot(x - horizontal, y - h) >
                     analyticalPositionTolerance))
                {
                    return Fail(
                        result,
                        FlexibleConnectionCatenaryStatus.NumericalFailure,
                        "AnalyticalEndpointMismatch");
                }

                if (i > 0)
                {
                    analyticalPolylineLength +=
                        Hypot(x - previousX, y - previousY);
                }

                previousX = x;
                previousY = y;

                double worldX = ca.X + horizontalX * x;
                double worldY = ca.Y + y;
                double worldZ = ca.Z + horizontalZ * x;

                if (!IsFinite(worldX) ||
                    !IsFinite(worldY) ||
                    !IsFinite(worldZ))
                {
                    return Fail(
                        result,
                        FlexibleConnectionCatenaryStatus.NumericalFailure,
                        "NonFiniteWorldSample");
                }

                points[i] = new Vector3(
                    (float)worldX,
                    (float)worldY,
                    (float)worldZ);

                if (!IsFinite(points[i]))
                {
                    return Fail(
                        result,
                        FlexibleConnectionCatenaryStatus.NumericalFailure,
                        "SampleOutsideFloatRange");
                }
            }

            if (!IsFinite(analyticalPolylineLength) ||
                analyticalPolylineLength > length + analyticalPositionTolerance ||
                analyticalPolylineLength < distance - analyticalPositionTolerance)
            {
                return Fail(
                    result,
                    FlexibleConnectionCatenaryStatus.NumericalFailure,
                    "AnalyticalPolylineMismatch");
            }

            if (PointDistance(points[0], ca) > floatPositionTolerance ||
                PointDistance(points[segmentCount], cb) > floatPositionTolerance)
            {
                return Fail(
                    result,
                    FlexibleConnectionCatenaryStatus.NumericalFailure,
                    "FloatEndpointMismatch");
            }

            // Only anchor after the analytical and float checks above.
            points[0] = ca;
            points[segmentCount] = cb;

            double polylineLength = 0.0;
            double minimumSegment = double.PositiveInfinity;
            double maximumSegment = 0.0;
            double previousHorizontal = 0.0;

            for (int i = 1; i <= segmentCount; i++)
            {
                double sampleHorizontal =
                    ((double)points[i].X - ca.X) * horizontalX +
                    ((double)points[i].Z - ca.Z) * horizontalZ;

                if (!IsFinite(sampleHorizontal) ||
                    sampleHorizontal < previousHorizontal - floatPositionTolerance ||
                    sampleHorizontal < -floatPositionTolerance ||
                    sampleHorizontal > horizontal + floatPositionTolerance)
                {
                    return Fail(
                        result,
                        FlexibleConnectionCatenaryStatus.NumericalFailure,
                        "FloatHorizontalMonotonicity");
                }

                previousHorizontal = sampleHorizontal;

                double segmentLength =
                    PointDistance(points[i - 1], points[i]);

                if (!IsFinite(segmentLength) || segmentLength < 0.0)
                {
                    return Fail(
                        result,
                        FlexibleConnectionCatenaryStatus.NumericalFailure,
                        "InvalidFloatSegment");
                }

                polylineLength += segmentLength;
                minimumSegment = Math.Min(minimumSegment, segmentLength);
                maximumSegment = Math.Max(maximumSegment, segmentLength);
            }

            // Each chord can change by the rounding of both its endpoints.
            double floatPolylineTolerance =
                analyticalPositionTolerance +
                2.0 * segmentCount * floatPositionTolerance;

            if (!IsFinite(polylineLength) ||
                polylineLength > length + floatPolylineTolerance ||
                polylineLength < distance - floatPolylineTolerance)
            {
                return Fail(
                    result,
                    FlexibleConnectionCatenaryStatus.NumericalFailure,
                    "FloatPolylineMismatch");
            }

            result.Points = points;
            result.PolylineLength = polylineLength;

            // Keep a signed value: tiny negative values can reflect float rounding.
            result.Deficit = length - polylineLength;

            result.MinimumSegmentLength = minimumSegment;
            result.MaximumSegmentLength = maximumSegment;
            result.Status = FlexibleConnectionCatenaryStatus.Success;
            result.Cause = string.Empty;

            // ORTS Flexible Connections
            // Purpose: Publish only after all original calculation checks succeeded.
            result.ArcContext = new FlexibleConnectionArcContext
            {
                Valid = true, Start = ca, End = cb, Length = length,
                A = a, Q0 = q0, Radius0 = radius0,
                HorizontalX = horizontalX, HorizontalZ = horizontalZ,
                Horizontal = horizontal, PositionTolerance = analyticalPositionTolerance
            };
            return result;
        }

        // =====================================================================
        // ORTS Flexible Connections
        // Purpose: Evaluate extra arc samples from the solved context without solving again.
        // Keep these stable expressions aligned with the original sampling loop.
        // =====================================================================
        internal static bool TryEvaluateAtArcLength(
            ref FlexibleConnectionArcContext context, double s, out Vector3 point)
        {
            point = Vector3.Zero;
            if (!context.Valid || !IsFinite(s) || s < 0.0 || s > context.Length)
                return false;
            if (s == 0.0) { point = context.Start; return true; }
            if (s == context.Length) { point = context.End; return true; }

            double a = context.A;
            double q0 = context.Q0;
            double q = q0 + s;
            double radius = Hypot(a, q);
            double denominator = radius + context.Radius0;
            double t = s / denominator;
            if (!IsFinite(q) || !IsFinite(radius) ||
                !IsFinite(denominator) || denominator <= 0.0 ||
                !IsFinite(t) || t < 0.0)
                return false;

            double x = t < 0.5
                ? 2.0 * a * Atanh(t)
                : a * (Asinh(q / a) - Asinh(q0 / a));
            double y = s * ((2.0 * q0 + s) / denominator);
            if (!IsFinite(x) || !IsFinite(y) ||
                x < -context.PositionTolerance || x > context.Horizontal + context.PositionTolerance)
                return false;

            double worldX = context.Start.X + context.HorizontalX * x;
            double worldY = context.Start.Y + y;
            double worldZ = context.Start.Z + context.HorizontalZ * x;
            if (!IsFinite(worldX) || !IsFinite(worldY) || !IsFinite(worldZ))
                return false;
            point = new Vector3((float)worldX, (float)worldY, (float)worldZ);
            return IsFinite(point);
        }

        private static FlexibleConnectionCatenaryResult Fail(
            FlexibleConnectionCatenaryResult result,
            FlexibleConnectionCatenaryStatus status,
            string cause)
        {
            result.Status = status;
            result.Cause = cause;
            result.Points = null;
            return result;
        }

        private static double SinhOverXMinusOne(double x)
        {
            if (x < 0.1)
            {
                double square = x * x;

                return square * (
                    1.0 / 6.0 + square * (
                    1.0 / 120.0 + square * (
                    1.0 / 5040.0 + square * (
                    1.0 / 362880.0 + square / 39916800.0))));
            }

            return Math.Sinh(x) / x - 1.0;
        }

        private static double LogOnePlus(double x)
        {
            // Compensate rounding in 1 + x, including very small x.
            double sum = 1.0 + x;

            if (sum == 1.0)
                return x;

            return Math.Log(sum) * (x / (sum - 1.0));
        }

        private static double Atanh(double x)
        {
            // Called only for 0 <= x < 0.5.
            return 0.5 * (LogOnePlus(x) - LogOnePlus(-x));
        }

        private static double Asinh(double x)
        {
            double absolute = Math.Abs(x);
            double value;

            if (absolute < 1e-8)
            {
                value = absolute;
            }
            else if (absolute > 1e150)
            {
                value = Math.Log(absolute) + Math.Log(2.0);
            }
            else
            {
                value = LogOnePlus(
                    absolute +
                    absolute * (absolute / (1.0 + Hypot(1.0, absolute))));
            }

            return x < 0.0 ? -value : value;
        }

        private static double Hypot(double x, double y)
        {
            x = Math.Abs(x);
            y = Math.Abs(y);

            double maximum = Math.Max(x, y);
            double minimum = Math.Min(x, y);

            if (maximum == 0.0)
                return 0.0;

            double ratio = minimum / maximum;
            return maximum * Math.Sqrt(1.0 + ratio * ratio);
        }

        private static double PointDistance(Vector3 a, Vector3 b)
        {
            double dx = (double)b.X - a.X;
            double dy = (double)b.Y - a.Y;
            double dz = (double)b.Z - a.Z;

            return Hypot(Hypot(dx, dz), dy);
        }

        private static double MaxAbs(Vector3 point)
        {
            return Math.Max(
                Math.Abs((double)point.X),
                Math.Max(
                    Math.Abs((double)point.Y),
                    Math.Abs((double)point.Z)));
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool IsFinite(Vector3 point)
        {
            return IsFinite(point.X) &&
                   IsFinite(point.Y) &&
                   IsFinite(point.Z);
        }
    }
}
