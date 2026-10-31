// COPYRIGHT 2026 by the Open Rails project.
//
// This file is part of Open Rails.
//
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Orts.Parsers.Msts;

namespace Orts.Viewer3D
{
    /// <summary>
    /// Describes a rigid placement made by a Template3D profile entry.
    /// </summary>
    public sealed class TrackProfileTemplatePlacement
    {
        public TrackProfileTemplate3D.PlacementLocation Location;
        public TrackProfileTemplate3D.PlacementFacing Facing;
    }

    /// <summary>
    /// Describes OBJ geometry generated alongside the traditional profile
    /// polylines in one LOD item.
    /// </summary>
    public sealed class TrackProfileTemplate3D
    {
        public enum GenerationModes
        {
            Sweep,
            Stretch,
            Repeat,
            Place,
        }

        public enum GeometryModes
        {
            Baked,
            Shared,
        }

        public enum ShapeSelectionModes
        {
            First,
            ByObject,
            Cycle,
            DeterministicRandom,
        }

        public enum PlacementLocation
        {
            SpanStart,
            SpanEnd,
            PathStart,
            PathEnd,
            Nodes,
        }

        public enum PlacementFacing
        {
            AlongPath,
            AgainstPath,
            Outward,
            Inward,
        }

        public GenerationModes GenerationMode;
        public GeometryModes GeometryMode = GeometryModes.Baked;
        public ShapeSelectionModes ShapeSelectionMode = ShapeSelectionModes.First;
        public readonly List<string> Shapes = new List<string>();
        public readonly List<TrackProfileTemplatePlacement> Placements =
            new List<TrackProfileTemplatePlacement>();
        public Vector3 OffsetM;
        public float SpacingM;
        public float PhaseM;
        public bool IsValid { get; private set; }

        bool GenerationModeDefined;
        bool DefinitionValid = true;

        public TrackProfileTemplate3D(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new[] {
                new STFReader.TokenProcessor("generationmode", () =>
                    ReadGenerationMode(stf)),
                new STFReader.TokenProcessor("geometrymode", () =>
                    ReadGeometryMode(stf)),
                new STFReader.TokenProcessor("shapeselectionmode", () =>
                    ReadShapeSelectionMode(stf)),
                new STFReader.TokenProcessor("shape", () =>
                    Shapes.Add(stf.ReadStringBlock(null))),
                new STFReader.TokenProcessor("offset", () =>
                    ReadOffset(stf)),
                new STFReader.TokenProcessor("spacing", () =>
                    SpacingM = stf.ReadFloatBlock(
                        STFReader.UNITS.Distance, null)),
                new STFReader.TokenProcessor("phase", () =>
                    PhaseM = stf.ReadFloatBlock(
                        STFReader.UNITS.Distance, null)),
                new STFReader.TokenProcessor("placement", () =>
                    ReadPlacement(stf)),
            });

            IsValid = Validate();
            if (!IsValid)
                STFException.TraceWarning(stf,
                    "Ignored invalid Template3D definition.");
        }

        void ReadGenerationMode(STFReader stf)
        {
            string value = stf.ReadStringBlock(null);
            GenerationModeDefined = true;
            switch (value.ToUpperInvariant())
            {
                case "SWEEP": GenerationMode = GenerationModes.Sweep; break;
                case "STRETCH": GenerationMode = GenerationModes.Stretch; break;
                case "REPEAT": GenerationMode = GenerationModes.Repeat; break;
                case "PLACE": GenerationMode = GenerationModes.Place; break;
                default:
                    DefinitionValid = false;
                    STFException.TraceWarning(stf,
                        "Skipped unknown Template3D GenerationMode " + value + ".");
                    break;
            }
        }

        void ReadGeometryMode(STFReader stf)
        {
            string value = stf.ReadStringBlock(null);
            switch (value.ToUpperInvariant())
            {
                case "BAKED": GeometryMode = GeometryModes.Baked; break;
                case "SHARED": GeometryMode = GeometryModes.Shared; break;
                default:
                    DefinitionValid = false;
                    STFException.TraceWarning(stf,
                        "Skipped unknown Template3D GeometryMode " + value + ".");
                    break;
            }
        }

        void ReadShapeSelectionMode(STFReader stf)
        {
            string value = stf.ReadStringBlock(null);
            switch (value.ToUpperInvariant())
            {
                case "FIRST": ShapeSelectionMode = ShapeSelectionModes.First; break;
                case "BYOBJECT": ShapeSelectionMode = ShapeSelectionModes.ByObject; break;
                case "CYCLE": ShapeSelectionMode = ShapeSelectionModes.Cycle; break;
                case "DETERMINISTICRANDOM":
                    ShapeSelectionMode = ShapeSelectionModes.DeterministicRandom;
                    break;
                default:
                    DefinitionValid = false;
                    STFException.TraceWarning(stf,
                        "Skipped unknown Template3D ShapeSelectionMode " +
                        value + ".");
                    break;
            }
        }

        void ReadOffset(STFReader stf)
        {
            stf.MustMatch("(");
            OffsetM.X = stf.ReadFloat(STFReader.UNITS.Distance, null);
            OffsetM.Y = stf.ReadFloat(STFReader.UNITS.Distance, null);
            OffsetM.Z = stf.ReadFloat(STFReader.UNITS.Distance, null);
            stf.SkipRestOfBlock();
        }

        void ReadPlacement(STFReader stf)
        {
            stf.MustMatch("(");
            var values = new List<string>();
            while (!stf.EndOfBlock())
                values.Add(stf.ReadString());

            if (values.Count != 2)
            {
                DefinitionValid = false;
                return;
            }

            PlacementFacing facing;
            switch (values[1].ToUpperInvariant())
            {
                case "ALONGPATH": facing = PlacementFacing.AlongPath; break;
                case "AGAINSTPATH": facing = PlacementFacing.AgainstPath; break;
                case "OUTWARD": facing = PlacementFacing.Outward; break;
                case "INWARD": facing = PlacementFacing.Inward; break;
                default:
                    DefinitionValid = false;
                    return;
            }

            switch (values[0].ToUpperInvariant())
            {
                case "SPANSTART":
                    AddPlacement(PlacementLocation.SpanStart, facing);
                    break;
                case "SPANEND":
                    AddPlacement(PlacementLocation.SpanEnd, facing);
                    break;
                case "PATHSTART":
                    AddPlacement(PlacementLocation.PathStart, facing);
                    break;
                case "PATHEND":
                    AddPlacement(PlacementLocation.PathEnd, facing);
                    break;
                case "SPANBOTH":
                    AddPlacement(PlacementLocation.SpanStart, facing);
                    AddPlacement(PlacementLocation.SpanEnd, facing);
                    break;
                case "PATHBOTH":
                    AddPlacement(PlacementLocation.PathStart, facing);
                    AddPlacement(PlacementLocation.PathEnd, facing);
                    break;
                case "NODES":
                    if (facing == PlacementFacing.Outward ||
                        facing == PlacementFacing.Inward)
                    {
                        DefinitionValid = false;
                        return;
                    }
                    AddPlacement(PlacementLocation.Nodes, facing);
                    break;
                default:
                    DefinitionValid = false;
                    break;
            }
        }

        void AddPlacement(PlacementLocation location, PlacementFacing facing)
        {
            Placements.Add(new TrackProfileTemplatePlacement
            {
                Location = location,
                Facing = facing,
            });
        }

        bool Validate()
        {
            if (!DefinitionValid || !GenerationModeDefined || Shapes.Count == 0)
                return false;
            foreach (string shape in Shapes)
                if (string.IsNullOrWhiteSpace(shape))
                    return false;
            if (!IsFinite(OffsetM.X) || !IsFinite(OffsetM.Y) ||
                !IsFinite(OffsetM.Z) || !IsFinite(SpacingM) ||
                !IsFinite(PhaseM))
                return false;
            if (GenerationMode == GenerationModes.Repeat && SpacingM <= 0)
                return false;
            if (GenerationMode == GenerationModes.Place && Placements.Count == 0)
                return false;
            if (GeometryMode == GeometryModes.Shared &&
                GenerationMode != GenerationModes.Repeat &&
                GenerationMode != GenerationModes.Place)
                return false;
            return true;
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    /// <summary>
    /// Carries object, authored-span, and complete-path ownership into
    /// procedural generation. Rulers additionally supply averaged endpoint
    /// directions for Stretch and Nodes placement without changing the actual
    /// straight span centerline.
    /// </summary>
    public sealed class TrackProfilePathContext
    {
        public int ObjectIndex;
        public int SpanIndex;
        public bool IsPointPath;
        public bool OwnStartNode = true;
        public bool OwnEndNode = true;
        public bool OwnPathStart = true;
        public bool OwnPathEnd = true;
        public Vector3? StartDirection;
        public Vector3? EndDirection;
    }
}
