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
using System.IO;
using Microsoft.Xna.Framework;
using Orts.Parsers.Msts;

namespace Orts.Formats.Msts
{
    /// <summary>
    /// Route-level pole and wire configurations used by native MSTS
    /// Telepole world objects.
    /// </summary>
    public class TelepoleDataFile
    {
        public readonly List<TelepoleConfig> Configurations =
            new List<TelepoleConfig>();

        public TelepoleDataFile(string filePath, string shapePath)
        {
            using (var stf = new STFReader(filePath, false))
            {
                bool foundData = false;
                stf.ParseFile(new[]
                {
                    new STFReader.TokenProcessor("tpoleconfigdata", () =>
                    {
                        foundData = true;
                        ReadConfigurations(stf, shapePath);
                    }),
                });
                if (!foundData)
                    throw new STFException(stf,
                        "Missing TPoleConfigData");
            }
        }

        void ReadConfigurations(STFReader stf, string shapePath)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(null);
            stf.ParseBlock(new[]
            {
                new STFReader.TokenProcessor("tpoleconfig", () =>
                {
                    if (--count < 0)
                    {
                        STFException.TraceWarning(stf,
                            "Loaded extra TPoleConfig");
                        count = 0;
                    }
                    Configurations.Add(new TelepoleConfig(stf,
                        shapePath));
                }),
            });
            if (count > 0)
                STFException.TraceWarning(stf,
                    count + " missing TPoleConfig(s)");
        }
    }

    public class TelepoleConfig
    {
        public string ShapeFilePath;
        public string ShadowFilePath;
        public float SeparationM;
        public readonly List<Vector3> Wires = new List<Vector3>();

        public TelepoleConfig(STFReader stf, string shapePath)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(null);
            stf.ParseBlock(new[]
            {
                new STFReader.TokenProcessor("filename", () =>
                {
                    ShapeFilePath = ResolveShape(stf,
                        stf.ReadStringBlock(null), shapePath);
                }),
                new STFReader.TokenProcessor("shadow", () =>
                {
                    ShadowFilePath = ResolveShape(stf,
                        stf.ReadStringBlock(null), shapePath);
                }),
                new STFReader.TokenProcessor("separation", () =>
                {
                    SeparationM = stf.ReadFloatBlock(
                        STFReader.UNITS.Distance, null);
                }),
                new STFReader.TokenProcessor("wire", () =>
                {
                    if (--count < 0)
                    {
                        STFException.TraceWarning(stf,
                            "Loaded extra Telepole Wire");
                        count = 0;
                    }
                    stf.MustMatch("(");
                    Wires.Add(new Vector3(
                        stf.ReadFloat(STFReader.UNITS.Distance, null),
                        stf.ReadFloat(STFReader.UNITS.Distance, null),
                        stf.ReadFloat(STFReader.UNITS.Distance, null)));
                    stf.SkipRestOfBlock();
                }),
            });
            if (count > 0)
                STFException.TraceWarning(stf,
                    count + " missing Telepole Wire(s)");
            if (SeparationM <= 0 || float.IsNaN(SeparationM) ||
                float.IsInfinity(SeparationM))
                STFException.TraceWarning(stf,
                    "Telepole Separation must be positive");
        }

        static string ResolveShape(STFReader stf, string fileName,
            string shapePath)
        {
            if (String.IsNullOrEmpty(fileName))
                return null;
            string path = Path.Combine(shapePath, fileName);
            if (!File.Exists(path))
            {
                STFException.TraceWarning(stf,
                    String.Format("Non-existent shape file {0} referenced",
                        path));
                return null;
            }
            return path;
        }
    }
}
