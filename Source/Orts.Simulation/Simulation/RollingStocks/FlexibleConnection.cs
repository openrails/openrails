// ORTS Flexible Connections
//
// Purpose: Parse and validate vehicle-local cosmetic connection definitions.
// Responsibilities: Resolve profiles, physical points and owner-selected Cases for
//   Hose, Hook and Coupler. Shared configuration is treated as read-only after parsing.
// Key components: FlexibleConnectionConfig parses and validates; ParseProfile delegates
//   rigid-family properties to FlexibleConnectionHookParser and FlexibleConnectionCouplerParser.
// Related files: MSTSWagon.cs stores and shares this configuration on cached copies;
//   FlexibleConnectionViewer.cs consumes it through MSTSWagonViewer.cs.
// Lifecycle: Build definitions during vehicle loading; keep per-frame state in viewers.
using Microsoft.Xna.Framework;
using Orts.Parsers.Msts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Orts.Simulation.RollingStocks
{


    // =====================================================================
    // ORTS Flexible Connections
    // Purpose: Parse local definitions and independently validate topology contexts.
    // Treat this shared configuration as read-only after parsing; occupancy belongs to viewers.
    // =====================================================================
    public class FlexibleConnectionConfig
    {
        // ORTS Flexible Connections
        // Purpose: Change Sides in the vehicle configuration to change faces; 4 remains the compatibility fallback.
        private const int DefaultSides = 4;
        private const int MinimumSides = 3;
        private const int MaximumSides = 64;

        public int Sides { get; private set; } = DefaultSides;

        // ORTS Flexible Connections
        // Purpose: Configure longitudinal subdivisions for the whole vehicle.
        private const int DefaultSegments = 16;
        private const int MinimumSegments = 2;
        private const int MaximumSegments = 128;

        public int Segments { get; private set; } = DefaultSegments;


        public readonly Dictionary<string, FlexibleConnectionLocalProfile> LocalProfiles =
            new Dictionary<string, FlexibleConnectionLocalProfile>(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, FlexibleConnectionPoint> RearConnectionPoints =
            new Dictionary<string, FlexibleConnectionPoint>(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, FlexibleConnectionPoint> FrontConnectionPoints =
            new Dictionary<string, FlexibleConnectionPoint>(StringComparer.OrdinalIgnoreCase);
        public string ConnectionOwner { get; private set; }
        private int OwnerDeclarations;
        public readonly Dictionary<string, FlexibleConnectionCase> ConnectionCases =
            new Dictionary<string, FlexibleConnectionCase>(StringComparer.OrdinalIgnoreCase);
        public readonly FlexibleConnectionPoint[] ConnectedPoints;
        public readonly FlexibleConnectionHalfEndpoint[] HalfEndpoints, RearHalfEndpoints, FrontHalfEndpoints;
        private readonly Dictionary<string, PendingPoint>[] PendingPoints = {
            new Dictionary<string, PendingPoint>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, PendingPoint>(StringComparer.OrdinalIgnoreCase) };

        private sealed class PendingPoint
        {
            internal string ID, Profile;
            internal Vector3 Position;
            internal bool Valid;
            // ORTS Flexible Connections
            // Purpose: Retain point-local disconnected shape syntax until profile resolution.
            internal string NotHookedShapeName;
            internal bool NotHookedDeclared, NotHookedValid = true;
            // ORTS Flexible Connections
            // Purpose: Preserve point-local Coupler syntax and invalid declarations until resolution.
            internal string NotCoupledShapeName;
            internal bool NotCoupledDeclared, NotCoupledValid = true;
        }

        public Dictionary<string, FlexibleConnectionPoint> GetConnectionPoints(FlexibleConnectionEnd end)
        { return end == FlexibleConnectionEnd.Rear ? RearConnectionPoints : FrontConnectionPoints; }
        public FlexibleConnectionHalfEndpoint[] GetHalfEndpoints(FlexibleConnectionEnd end)
        { return end == FlexibleConnectionEnd.Rear ? RearHalfEndpoints : FrontHalfEndpoints; }

        public FlexibleConnectionConfig(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new[] {
                new STFReader.TokenProcessor("sides", () => ParseSides(stf)),
                new STFReader.TokenProcessor("segments", () => ParseSegments(stf)),
                new STFReader.TokenProcessor("localprofiles", () => {
                    stf.MustMatch("(");
                    stf.ParseBlock(new[] { new STFReader.TokenProcessor("profile", () => ParseProfile(stf)) });
                }),
                new STFReader.TokenProcessor("rearconnectionpoints", () => ParsePoints(stf, FlexibleConnectionEnd.Rear)),
                new STFReader.TokenProcessor("frontconnectionpoints", () => ParsePoints(stf, FlexibleConnectionEnd.Front)),
                new STFReader.TokenProcessor("connectionowner", () => ParseOwner(stf)),
                new STFReader.TokenProcessor("connectioncases", () => {
                    stf.MustMatch("(");
                    stf.ParseBlock(new[] { new STFReader.TokenProcessor("case", () => ParseCase(stf)) });
                })
            });
            RearHalfEndpoints = ResolvePoints(stf, FlexibleConnectionEnd.Rear);
            FrontHalfEndpoints = ResolvePoints(stf, FlexibleConnectionEnd.Front);
            HalfEndpoints = new FlexibleConnectionHalfEndpoint[RearHalfEndpoints.Length + FrontHalfEndpoints.Length];
            Array.Copy(RearHalfEndpoints, HalfEndpoints, RearHalfEndpoints.Length);
            Array.Copy(FrontHalfEndpoints, 0, HalfEndpoints, RearHalfEndpoints.Length, FrontHalfEndpoints.Length);
            if (OwnerDeclarations != 1 || ConnectionOwner == null)
            {
                ConnectionOwner = null;
                STFException.TraceWarning(stf, "Flexible connection ConnectionOwner must occur exactly once with one valid ID. Connected disabled; local Half remains available.");
            }
            var used = new HashSet<FlexibleConnectionPoint>();
            var connected = new List<FlexibleConnectionPoint>();
            foreach (var connectionCase in ConnectionCases.Values)
                if (connectionCase.Valid)
                    foreach (FlexibleConnectionEnd local in new[] { FlexibleConnectionEnd.Front, FlexibleConnectionEnd.Rear })
                        foreach (FlexibleConnectionEnd remote in new[] { FlexibleConnectionEnd.Front, FlexibleConnectionEnd.Rear })
                        {
                            ValidateMappings(stf, connectionCase, local, remote);
                            if (ConnectionOwner != null)
                                foreach (var mapping in connectionCase.GetMappings(local, remote))
                                    if (used.Add(mapping.Point)) connected.Add(mapping.Point);
                        }
            ConnectedPoints = connected.ToArray();
        }

        static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        static void Duplicate(STFReader stf, HashSet<string> seen, string token, string label)
        {
            if (!seen.Add(token)) STFException.TraceWarning(stf, label + ": repeated " + token + "; using last declaration.");
        }
        void ParseProfile(STFReader stf)
        {
            stf.MustMatch("(");
            string name = ReadIdentifier(stf);
            if (name == null) { STFException.TraceWarning(stf, "LocalProfile needs a name."); return; }
            float length = float.NaN, radius = float.NaN, f = 0f, fr = float.NaN, e = 0f;
            float initial = 0f, final = 90f;
            string texture = null;
            var type = FlexibleConnectionDisconnectedType.None;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // ORTS Flexible Connections
            // Purpose: Classify only after reading all properties, independently of token order.
            var hookParser = new FlexibleConnectionHookParser();
            // ORTS Flexible Connections
            // Purpose: Delegate the exclusive family and ordered rigid assets to their own parser.
            var couplerParser = new FlexibleConnectionCouplerParser();
            stf.ParseBlock(new[] {
                // ORTS Flexible Connections
                // Purpose: Delegate strict Hook blocks without changing Hose readers or duplicates.
                // Purpose: Keep Coupler parsing segregated; Links is classified after all properties.
                new STFReader.TokenProcessor("iscoupler", () => couplerParser.Read(stf, "IsCoupler", name)),
                new STFReader.TokenProcessor("couplershapes", () => couplerParser.Read(stf, "CouplerShapes", name)),
                new STFReader.TokenProcessor("notcoupledshape", () => couplerParser.RejectPointProperty(stf, name)),
                // ORTS Flexible Connections
                // Purpose: Delegate strict, Hook-only ChainParity parsing.
                new STFReader.TokenProcessor("chainparity", () => hookParser.Read(stf, "ChainParity", name)),
                new STFReader.TokenProcessor("ishook", () => hookParser.Read(stf, "IsHook", name)),
                new STFReader.TokenProcessor("links", () => hookParser.Read(stf, "Links", name)),
                new STFReader.TokenProcessor("linkshape", () => hookParser.Read(stf, "LinkShape", name)),
                new STFReader.TokenProcessor("hookshape", () => hookParser.Read(stf, "HookShape", name)),
                new STFReader.TokenProcessor("nothookedshape", () => hookParser.RejectPointProperty(stf, name)),
                new STFReader.TokenProcessor("length", () => { Duplicate(stf, seen, "Length", name); length = stf.ReadFloatBlock(STFReader.UNITS.Distance, float.NaN); }),
                new STFReader.TokenProcessor("radius", () => { Duplicate(stf, seen, "Radius", name); radius = stf.ReadFloatBlock(STFReader.UNITS.Distance, float.NaN); }),
                new STFReader.TokenProcessor("texture", () => { Duplicate(stf, seen, "Texture", name); texture = stf.ReadStringBlock(string.Empty); }),
                new STFReader.TokenProcessor("type", () => { Duplicate(stf, seen, "Type", name); type = ParseDisconnectedType(stf); }),
                new STFReader.TokenProcessor("fallangle", () => { Duplicate(stf, seen, "FallAngle", name); ParseFallAngle(stf, out initial, out final); }),
                new STFReader.TokenProcessor("fittinglength", () => { Duplicate(stf, seen, "FittingLength", name); f = stf.ReadFloatBlock(STFReader.UNITS.Distance, float.NaN); }),
                new STFReader.TokenProcessor("fittingradius", () => { Duplicate(stf, seen, "FittingRadius", name); fr = stf.ReadFloatBlock(STFReader.UNITS.Distance, float.NaN); }),
                new STFReader.TokenProcessor("fittingextrude", () => { Duplicate(stf, seen, "FittingExtrude", name); e = stf.ReadFloatBlock(STFReader.UNITS.Distance, float.NaN); })
            });
            // =====================================================================
            // ORTS Flexible Connections
            // Purpose: Keep receiver-only Hook profiles valid; Hose retains its validation.
            // =====================================================================
            // Purpose: Resolve family before Links range; never send a Coupler into Hose validation.
            FlexibleConnectionHookConfig hook = null;
            FlexibleConnectionCouplerConfig coupler;
            bool familyValid = couplerParser.Resolve(stf, name, hookParser, seen.Count != 0, out coupler);
            bool hookValid = familyValid && coupler == null && hookParser.Resolve(stf, name, seen.Count != 0, out hook);
            FlexibleConnectionLocalProfile profile = null;
            if (familyValid && coupler != null)
                profile = new FlexibleConnectionLocalProfile(name, coupler);
            else if (hookValid && hook != null)
                profile = new FlexibleConnectionLocalProfile(name, hook);
            else if (hookValid)
            {
                bool valid = Finite(length) && length > 0f && Finite(radius) && radius >= 0.0001f && radius <= 0.5f;
                bool fittingValid = Finite(f) && f >= 0f && Finite(e) && e >= 0f &&
                    (!seen.Contains("FittingRadius") || (Finite(fr) && fr >= 0.0001f && fr <= 0.5f)) &&
                    (f == 0f || (Finite(fr) && fr >= 0.0001f && fr <= 0.5f && (double)f + e < length));
                if (!valid || !fittingValid)
                    STFException.TraceWarning(stf, "LocalProfile '" + name + "' invalid: Length must be positive, Radius 0.0001m..0.5m, and a fitting needs valid dimensions with FittingLength + FittingExtrude < local Length. Profile rejected.");
                profile = valid && fittingValid ? new FlexibleConnectionLocalProfile(name, type, length, radius, texture,
                    f > 0f ? new FlexibleConnectionFittingConfig(f, fr, e) : null, initial, final) : null;
            } // ORTS Flexible Connections: end of unchanged Hose validation path.
            if (LocalProfiles.ContainsKey(name))
            {
                STFException.TraceWarning(stf, "Duplicate LocalProfile '" + name + "'. This profile identity is invalid.");
                profile = null;
            }
            LocalProfiles[name] = profile;
        }

        void ParsePoints(STFReader stf, FlexibleConnectionEnd end)
        {
            stf.MustMatch("(");
            string id;
            while ((id = ReadIdentifier(stf)) != null)
            {
                stf.MustMatch("(");
                var point = new PendingPoint { ID = id, Position = new Vector3(float.NaN, float.NaN, float.NaN) };
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                // Require the named Position/Profile structure after native comment handling.
                string first = ReadIdentifier(stf);
                if (first != null)
                {
                    stf.StepBackOneItem();
                    if (!string.Equals(first, "Position", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(first, "Profile", StringComparison.OrdinalIgnoreCase) &&
                        // ORTS Flexible Connections
                        // Purpose: Permit the new point property before Position/Profile as well.
                        !string.Equals(first, "NotHookedShape", StringComparison.OrdinalIgnoreCase) &&
                        // ORTS Flexible Connections
                        // Purpose: Allow the point-local disconnected asset in any property order.
                        !string.Equals(first, "NotCoupledShape", StringComparison.OrdinalIgnoreCase))
                    {
                        STFException.TraceWarning(stf, end + ":" + id + ": invalid point syntax. Use Position (...) and Profile (...). Point rejected.");
                        stf.SkipRestOfBlock();
                    }
                    else
                    {
                        bool supported = true;
                        var processors = new List<STFReader.TokenProcessor> {
                            // ORTS Flexible Connections
                            // Purpose: Read one unique disconnected asset per physical point.
                            // Purpose: Reuse strict single-shape filename syntax, retaining failures.
                            new STFReader.TokenProcessor("notcoupledshape", () => {
                                string shape;
                                bool valid = FlexibleConnectionHookParser.ReadNotHookedShape(stf, out shape);
                                point.NotCoupledValid &= valid && !point.NotCoupledDeclared;
                                point.NotCoupledDeclared = true;
                                point.NotCoupledShapeName = shape;
                                if (!point.NotCoupledValid)
                                    STFException.TraceWarning(stf, end + ":" + id + ": duplicate or invalid NotCoupledShape. Point rejected.");
                            }),
                            new STFReader.TokenProcessor("nothookedshape", () => {
                                string shape;
                                bool valid = FlexibleConnectionHookParser.ReadNotHookedShape(stf, out shape);
                                point.NotHookedValid &= valid && !point.NotHookedDeclared;
                                point.NotHookedDeclared = true;
                                point.NotHookedShapeName = shape;
                                if (!point.NotHookedValid)
                                    STFException.TraceWarning(stf, end + ":" + id + ": duplicate or invalid NotHookedShape. Point rejected.");
                            }),
                            new STFReader.TokenProcessor("position", () => {
                                Duplicate(stf, seen, "Position", end + ":" + id);
                                stf.MustMatch("(");
                                float x = stf.ReadFloat(STFReader.UNITS.Distance, float.NaN);
                                float y = stf.ReadFloat(STFReader.UNITS.Distance, float.NaN);
                                float z = stf.ReadFloat(STFReader.UNITS.Distance, float.NaN);
                                stf.SkipRestOfBlock();
                                point.Position = new Vector3(x,y,z);
                            }),
                            new STFReader.TokenProcessor("profile", () => {
                                Duplicate(stf, seen, "Profile", end + ":" + id);
                                point.Profile = stf.ReadStringBlock(string.Empty);
                            })
                        };
                        foreach (string name in new[] { "type", "length", "radius", "texture", "fittinglength", "fittingradius", "fittingextrude", "fallangle" })
                        {
                            string token = name;
                            processors.Add(new STFReader.TokenProcessor(token, () => {
                                STFException.TraceWarning(stf, end + ":" + id + ": " + token + " must be defined in LocalProfile; point overrides are unsupported. Point rejected.");
                                supported = false; stf.SkipBlock();
                            }));
                        }
                        stf.ParseBlock(processors);
                        point.Valid = supported && ValidID(id, true) && Finite(point.Position.X) && Finite(point.Position.Y) && Finite(point.Position.Z) && !string.IsNullOrWhiteSpace(point.Profile);
                        // ORTS Flexible Connections
                        // Purpose: Preserve a failed new declaration until point resolution.
                        point.Valid &= point.NotHookedValid;
                        // ORTS Flexible Connections
                        // Purpose: A malformed disconnected declaration invalidates the whole point.
                        point.Valid &= point.NotCoupledValid;
                    }
                }
                var pending = PendingPoints[(int)end];
                if (pending.ContainsKey(id))
                {
                    STFException.TraceWarning(stf, "Duplicate ConnectionPoint " + end + ":" + id + ". Identity rejected.");
                    point.Valid = false;
                }
                pending[id] = point;
            }
        }

        FlexibleConnectionHalfEndpoint[] ResolvePoints(STFReader stf, FlexibleConnectionEnd end)
        {
            var halves = new List<FlexibleConnectionHalfEndpoint>();
            // =====================================================================
            // ORTS Flexible Connections
            // Purpose: Allow one publisher-capable Coupler point plus passive
            //          receiver-only Coupler points on the same vehicle end.
            // =====================================================================
            int couplerPublishers = 0;
            foreach (var candidate in PendingPoints[(int)end].Values)
            {
                FlexibleConnectionLocalProfile candidateProfile;
                if (candidate.Valid && !candidate.NotHookedDeclared && candidate.Profile != null &&
                    LocalProfiles.TryGetValue(candidate.Profile, out candidateProfile) && candidateProfile != null &&
                    candidateProfile.Family == FlexibleConnectionFamily.Coupler && candidateProfile.Coupler.CanPublish)
                    couplerPublishers++;
            }
            if (couplerPublishers > 1)
                STFException.TraceWarning(stf, end + ": more than one publisher-capable Coupler ConnectionPoint. All publisher-capable Coupler points on this end rejected; valid receiver-only points retained.");
            foreach (var pending in PendingPoints[(int)end].Values)
            {
                FlexibleConnectionLocalProfile profile;
                if (!pending.Valid || !LocalProfiles.TryGetValue(pending.Profile, out profile) || profile == null)
                {
                    STFException.TraceWarning(stf, "ConnectionPoint " + end + ":" + pending.ID + " has invalid Position or missing/invalid Profile '" + pending.Profile + "'. Point rejected.");
                    continue;
                }
                // ORTS Flexible Connections
                // Purpose: Reject misuse locally; never rescue a disconnected visual from an invalid point.
                if (pending.NotHookedDeclared && profile.Hook == null)
                {
                    STFException.TraceWarning(stf, "ConnectionPoint " + end + ":" + pending.ID + ": NotHookedShape requires a Hook Profile. Point rejected.");
                    continue;
                }
                // ORTS Flexible Connections
                // Purpose: Validate disconnected ownership and the per-end limit without affecting Hook/Hose.
                if (pending.NotCoupledDeclared && profile.Family != FlexibleConnectionFamily.Coupler)
                {
                    STFException.TraceWarning(stf, "ConnectionPoint " + end + ":" + pending.ID + ": NotCoupledShape requires a Coupler Profile. Point rejected.");
                    continue;
                }
                if (profile.Family == FlexibleConnectionFamily.Coupler && profile.Coupler.CanPublish && couplerPublishers > 1) continue;
                var point = new FlexibleConnectionPoint(pending.ID, end, pending.Position, profile, pending.NotHookedShapeName, pending.NotCoupledShapeName);
                GetConnectionPoints(end).Add(point.ID, point);
                if (profile.Type == FlexibleConnectionDisconnectedType.Half) halves.Add(new FlexibleConnectionHalfEndpoint(point));
            }
            return halves.ToArray();
        }

        // ORTS Flexible Connections
        // Purpose: Isolate compact Case syntax from the native STF tokenizer.
        static bool ValidID(string value, bool point)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            foreach (char c in value)
                if (char.IsWhiteSpace(c) || c == ',' || c == '(' || c == ')' || (point && c == '-')) return false;
            return true;
        }

        static string ReadCompactBlock(STFReader stf, out bool valid)
        {
            valid = true;
            var text = new StringBuilder();
            string opening = stf.ReadItem();
            if (opening != "(")
            {
                valid = false;
                if (opening.Length > 0) stf.StepBackOneItem();
                return string.Empty;
            }
            bool closed = false;
            while (!stf.Eof)
            {
                // Read the actual ')' token: EndOfBlock also returns true at EOF.
                string token = stf.ReadItem();
                if (token == STFReader.EndBlockCommentSentinel) continue;
                if (token == ")") { closed = true; break; }
                if (token == "(")
                {
                    valid = false;
                    stf.SkipRestOfBlock();
                    continue;
                }
                if (text.Length > 0) text.Append(' ');
                text.Append(token);
            }
            valid &= closed;
            return text.ToString();
        }

        void ParseOwner(STFReader stf)
        {
            OwnerDeclarations++;
            bool valid;
            string owner = ReadCompactBlock(stf, out valid).Trim();
            ConnectionOwner = OwnerDeclarations == 1 && valid && ValidID(owner, false) ? owner : null;
        }

        void ParseCase(STFReader stf)
        {
            bool valid;
            string text = ReadCompactBlock(stf, out valid);
            string[] fields = text.Split(new[] { ',' }, StringSplitOptions.None);
            string destination = fields[0].Trim();
            if (!ValidID(destination, false))
            {
                STFException.TraceWarning(stf, "Flexible connection Case has a missing or malformed Destination ID. Case rejected.");
                return;
            }
            FlexibleConnectionCase existing;
            if (ConnectionCases.TryGetValue(destination, out existing))
            {
                if (!existing.Duplicate)
                    STFException.TraceWarning(stf, "Duplicate flexible connection Case for '" + destination + "'. Destination remains declared but invalid.");
                existing.Duplicate = true;
                existing.Valid = false;
                existing.ClearMappings();
                return;
            }
            var connectionCase = new FlexibleConnectionCase(destination);
            ConnectionCases.Add(destination, connectionCase);
            if (!valid || fields.Length != 5)
            {
                connectionCase.Valid = false;
                STFException.TraceWarning(stf, "Flexible connection Case '" + destination + "' requires exactly five comma-separated fields and a complete block. Destination remains declared but invalid.");
                return;
            }
            for (int orientation = 0; orientation < 4; orientation++)
            {
                var mappings = connectionCase.Mappings[orientation];
                foreach (string token in fields[orientation + 1].Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] ids = token.Split('-');
                    if (ids.Length != 2 || !ValidID(ids[0], true) || !ValidID(ids[1], true))
                    {
                        STFException.TraceWarning(stf, "Flexible connection Case '" + destination + "': invalid mapping '" + token + "'. Expected LOCAL-REMOTE; mapping rejected.");
                        continue;
                    }
                    mappings.Add(new FlexibleConnectionMapping(ids[0], ids[1]));
                }
            }
        }

        void ValidateMappings(STFReader stf, FlexibleConnectionCase connectionCase,
            FlexibleConnectionEnd localEnd, FlexibleConnectionEnd remoteEnd)
        {
            var mappings = connectionCase.GetMappings(localEnd, remoteEnd);
            var unique = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var localCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var remoteCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < mappings.Count;)
            {
                var mapping = mappings[i];
                HashSet<string> destinations;
                if (!unique.TryGetValue(mapping.LocalPoint, out destinations))
                    unique.Add(mapping.LocalPoint, destinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                if (!destinations.Add(mapping.RemotePoint)) { mappings.RemoveAt(i); continue; }
                int count;
                localCounts.TryGetValue(mapping.LocalPoint, out count); localCounts[mapping.LocalPoint] = count + 1;
                remoteCounts.TryGetValue(mapping.RemotePoint, out count); remoteCounts[mapping.RemotePoint] = count + 1;
                i++;
            }
            for (int i = mappings.Count - 1; i >= 0; i--)
            {
                var mapping = mappings[i];
                FlexibleConnectionPoint point;
                string label = "Case '" + connectionCase.DestinationID + "' " + localEnd + "->" + remoteEnd;
                if (localCounts[mapping.LocalPoint] > 1 || remoteCounts[mapping.RemotePoint] > 1)
                {
                    STFException.TraceWarning(stf, label + ": conflicting mapping " + mapping.LocalPoint + "->" + mapping.RemotePoint + ". Relation rejected in this context only.");
                    mappings.RemoveAt(i);
                }
                else if (!GetConnectionPoints(localEnd).TryGetValue(mapping.LocalPoint, out point))
                {
                    STFException.TraceWarning(stf, label + ": undefined/invalid local point " + mapping.LocalPoint + ". Mapping rejected.");
                    mappings.RemoveAt(i);
                }
                else mapping.Point = point;
            }
        }
        void ParseSides(STFReader stf)
        {
            int sides = stf.ReadIntBlock(DefaultSides);

            if (sides < MinimumSides || sides > MaximumSides)
            {
                STFException.TraceWarning(
                    stf,
                    string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "Sides value {0} is outside the valid range " +
                        "{1}..{2}. Using fallback {3}.",
                        sides,
                        MinimumSides,
                        MaximumSides,
                        DefaultSides));

                sides = DefaultSides;
            }

            Sides = sides;
        }

        // ORTS Flexible Connections
        // Purpose: Validate each declaration during loading; the last wins.
        void ParseSegments(STFReader stf)
        {
            // An invalid sentinel also catches empty or malformed declarations.
            int segments = stf.ReadIntBlock(0);
            if (segments < MinimumSegments || segments > MaximumSegments)
            {
                STFException.TraceWarning(
                    stf,
                    string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "Segments must be an integer in the range {0}..{1}. " +
                        "Using fallback {2}.",
                        MinimumSegments, MaximumSegments, DefaultSegments));
                segments = DefaultSegments;
            }

            Segments = segments;
        }

        static FlexibleConnectionDisconnectedType ParseDisconnectedType(STFReader stf)
        {
            string value = stf.ReadStringBlock(string.Empty);
            if (string.Equals(value, "Half", StringComparison.OrdinalIgnoreCase))
                return FlexibleConnectionDisconnectedType.Half;
            if (!string.Equals(value, "None", StringComparison.OrdinalIgnoreCase))
                STFException.TraceWarning(stf, "Unknown FlexibleConnection Type '" + value + "'. Using None.");
            return FlexibleConnectionDisconnectedType.None;
        }

        // ORTS Flexible Connections
        // Purpose: Read one or two Half angles atomically; invalid blocks restore the full 0/90 fallback.
        static void ParseFallAngle(STFReader stf, out float initialDegrees, out float finalDegrees)
        {
            initialDegrees = 0f;
            finalDegrees = 90f;
            float initial = 0f, final = 90f;
            int count = 0;
            bool valid = true, closed = false;
            string opening = stf.ReadItem();
            if (opening == "(")
            {
                while (!stf.Eof)
                {
                    if (stf.EndOfBlock())
                    {
                        closed = true;
                        break;
                    }
                    string value = stf.ReadString();
                    if (value == "(")
                    {
                        valid = false;
                        stf.SkipRestOfBlock();
                        continue;
                    }
                    count++;
                    float degrees;
                    if (count > 2 || !float.TryParse(value, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out degrees) ||
                        float.IsNaN(degrees) || float.IsInfinity(degrees))
                    {
                        valid = false;
                        continue;
                    }
                    if (count == 1) initial = degrees;
                    else final = degrees;
                }
            }
            else
            {
                valid = false;
                if (opening.Length > 0) stf.StepBackOneItem();
            }
            if (valid && closed && count >= 1 && count <= 2 &&
                initial >= 0f && initial <= final && final <= 90f)
            {
                initialDegrees = initial;
                finalDegrees = final;
                return;
            }
            STFException.TraceWarning(stf,
                "Invalid LocalProfile FallAngle. Expected one or two finite degree values with 0 <= Initial <= Final <= 90. Using 0/90 degrees.");
        }

        static string ReadIdentifier(STFReader stf)
        {
            while (!stf.Eof && !stf.EndOfBlock())
            {
                string identifier = stf.ReadItem();

                if (identifier == STFReader.EndBlockCommentSentinel)
                    continue;

                if (identifier == ")")
                    return null;

                return identifier;
            }

            return null;
        }
    }

    // ORTS Flexible Connections
    // Purpose: Immutable local definitions; sharing a profile never shares a physical point.
    public enum FlexibleConnectionEnd { Rear, Front }
    public enum FlexibleConnectionDisconnectedType { None, Half }
    public sealed class FlexibleConnectionLocalProfile
    {
        // ORTS Flexible Connections
        // Purpose: Optional Hook configuration is the family discriminator; Hose constructor is preserved.
        public readonly FlexibleConnectionHookConfig Hook;
        // ORTS Flexible Connections
        // Purpose: Explicit family identity keeps rigid Coupler profiles out of all Hose resource paths.
        public readonly FlexibleConnectionCouplerConfig Coupler;
        public FlexibleConnectionFamily Family { get { return Coupler != null ? FlexibleConnectionFamily.Coupler :
            Hook != null ? FlexibleConnectionFamily.Hook : FlexibleConnectionFamily.Hose; } }
        public FlexibleConnectionLocalProfile(string name, FlexibleConnectionCouplerConfig coupler)
        { Name = name; Coupler = coupler; }
        public FlexibleConnectionLocalProfile(string name, FlexibleConnectionHookConfig hook)
        { Name = name; Hook = hook; }
        public readonly string Name, TextureName;
        public readonly float LengthM, RadiusM, FallAngleDegrees, FallFinalAngleDegrees;
        public readonly FlexibleConnectionDisconnectedType Type;
        public readonly FlexibleConnectionFittingConfig Fitting;
        public FlexibleConnectionLocalProfile(string name, FlexibleConnectionDisconnectedType type, float length, float radius,
            string texture, FlexibleConnectionFittingConfig fitting, float initial, float final)
        { Name = name; Type = type; LengthM = length; RadiusM = radius; TextureName = texture; Fitting = fitting; FallAngleDegrees = initial; FallFinalAngleDegrees = final; }
    }
    public sealed class FlexibleConnectionPoint
    {
        // ORTS Flexible Connections
        // Purpose: Disconnected Hook assets belong to individual points, not shared profiles.
        public readonly string NotHookedShapeName;
        // ORTS Flexible Connections
        // Purpose: Keep disconnected Coupler asset identity attached to its physical point.
        public readonly string NotCoupledShapeName;
        public FlexibleConnectionPoint(string id, FlexibleConnectionEnd end, Vector3 position,
            FlexibleConnectionLocalProfile profile, string notHookedShapeName, string notCoupledShapeName)
            : this(id, end, position, profile, notHookedShapeName)
        { NotCoupledShapeName = notCoupledShapeName; }
        public FlexibleConnectionPoint(string id, FlexibleConnectionEnd end, Vector3 position,
            FlexibleConnectionLocalProfile profile, string notHookedShapeName) : this(id, end, position, profile)
        { NotHookedShapeName = notHookedShapeName; }
        public readonly string ID;
        public readonly FlexibleConnectionEnd End;
        public readonly Vector3 Position;
        public readonly FlexibleConnectionLocalProfile Profile;
        public FlexibleConnectionPoint(string id, FlexibleConnectionEnd end, Vector3 position, FlexibleConnectionLocalProfile profile)
        { ID = id; End = end; Position = position; Profile = profile; }
    }
    public sealed class FlexibleConnectionFittingConfig
    {
        public readonly float LengthM, RadiusM, ExtrudeM;
        public FlexibleConnectionFittingConfig(float length, float radius, float extrude)
        { LengthM = length; RadiusM = radius; ExtrudeM = extrude; }
    }
    public sealed class FlexibleConnectionHalfEndpoint
    {
        public readonly string PointID;
        public readonly Vector3 Point;
        public readonly FlexibleConnectionLocalProfile Profile;
        public readonly FlexibleConnectionEnd End;
        public FlexibleConnectionHalfEndpoint(FlexibleConnectionPoint point)
        { PointID = point.ID; Point = point.Position; Profile = point.Profile; End = point.End; }
    }
    public sealed class FlexibleConnectionMapping
    {
        public readonly string LocalPoint, RemotePoint;
        public FlexibleConnectionPoint Point { get; internal set; }
        public FlexibleConnectionMapping(string local, string remote) { LocalPoint = local; RemotePoint = remote; }
    }
    public sealed class FlexibleConnectionCase
    {
        public readonly string DestinationID;
        public bool Valid { get; internal set; } = true;
        internal bool Duplicate;
        // Fixed order: FF, RR, FR, RF. No orientation is inferred from another.
        internal readonly List<FlexibleConnectionMapping>[] Mappings = {
            new List<FlexibleConnectionMapping>(), new List<FlexibleConnectionMapping>(),
            new List<FlexibleConnectionMapping>(), new List<FlexibleConnectionMapping>() };
        public FlexibleConnectionCase(string destination) { DestinationID = destination; }
        public List<FlexibleConnectionMapping> GetMappings(FlexibleConnectionEnd local, FlexibleConnectionEnd remote)
        {
            int index = local == FlexibleConnectionEnd.Front ?
                (remote == FlexibleConnectionEnd.Front ? 0 : 2) :
                (remote == FlexibleConnectionEnd.Rear ? 1 : 3);
            return Mappings[index];
        }
        internal void ClearMappings()
        { foreach (var mappings in Mappings) mappings.Clear(); }
    }
}
