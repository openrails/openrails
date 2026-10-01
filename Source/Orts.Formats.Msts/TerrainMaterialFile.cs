// Licensed under the GNU General Public License, version 3 or later.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Orts.Parsers.Msts;

namespace Orts.Formats.Msts
{
    public sealed class TerrainMaterialDefinition
    {
        public readonly uint Uid;
        public readonly string Name;
        public readonly string Texture;

        public TerrainMaterialDefinition(uint uid, string name, string texture)
        {
            Uid = uid;
            Name = name;
            Texture = texture;
        }
    }

    public sealed class TerrainMaterialFile
    {
        readonly Dictionary<uint, TerrainMaterialDefinition> materials =
            new Dictionary<uint, TerrainMaterialDefinition>();

        public IReadOnlyDictionary<uint, TerrainMaterialDefinition> Materials { get { return materials; } }
        public ulong NextUid { get; private set; }

        public TerrainMaterialFile(string filename)
        {
            bool rootSeen = false;
            using (var stf = new STFReader(filename, false))
            {
                stf.ParseFile(new[] {
                    new STFReader.TokenProcessor("tsre_terrain_materials", () => {
                        if (rootSeen)
                            throw new STFException(stf, "Duplicate TSRE_Terrain_Materials block");
                        rootSeen = true;
                        ReadRoot(stf);
                    }),
                });
                if (!rootSeen)
                    throw new STFException(stf, "Missing TSRE_Terrain_Materials block");
            }
        }

        void ReadRoot(STFReader stf)
        {
            bool versionSeen = false;
            bool nextUidSeen = false;
            uint version = 0;
            string materialError = null;
            stf.MustMatch("(");
            stf.ParseBlock(new[] {
                new STFReader.TokenProcessor("version", () => {
                    if (versionSeen)
                        throw new STFException(stf, "Duplicate terrain material Version");
                    versionSeen = true;
                    version = stf.ReadUIntBlock(null);
                }),
                new STFReader.TokenProcessor("nextuid", () => {
                    if (nextUidSeen)
                        throw new STFException(stf, "Duplicate terrain material NextUiD");
                    nextUidSeen = true;
                    string value = stf.ReadStringBlock(null);
                    ulong next;
                    if (!UInt64.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out next))
                        throw new STFException(stf, "Invalid terrain material NextUiD");
                    NextUid = next;
                }),
                new STFReader.TokenProcessor("material", () => {
                    string error = ReadMaterial(stf);
                    if (materialError == null)
                        materialError = error;
                }),
            });

            if (materialError != null)
                throw new STFException(stf, materialError);
            if (!versionSeen || version != 1 || !nextUidSeen || NextUid == 0 ||
                NextUid > (ulong)UInt32.MaxValue + 1 ||
                (materials.Count > 0 && NextUid <= MaximumUid()))
                throw new STFException(stf, "Invalid terrain material Version or NextUiD");
        }

        string ReadMaterial(STFReader stf)
        {
            bool uidSeen = false;
            bool nameSeen = false;
            bool textureSeen = false;
            uint uid = 0;
            string name = null;
            string texture = null;

            stf.MustMatch("(");
            stf.ParseBlock(new[] {
                new STFReader.TokenProcessor("uid", () => {
                    if (uidSeen)
                        throw new STFException(stf, "Duplicate terrain material UiD");
                    uidSeen = true;
                    uid = stf.ReadUIntBlock(null);
                }),
                new STFReader.TokenProcessor("name", () => {
                    if (nameSeen)
                        throw new STFException(stf, "Duplicate terrain material Name");
                    nameSeen = true;
                    name = stf.ReadStringBlock(null);
                }),
                new STFReader.TokenProcessor("texture", () => {
                    if (textureSeen)
                        throw new STFException(stf, "Duplicate terrain material Texture");
                    textureSeen = true;
                    texture = stf.ReadStringBlock(null);
                }),
            });

            if (!uidSeen || uid == 0 || !nameSeen || String.IsNullOrEmpty(name) ||
                !textureSeen || !IsSafeRelativePath(texture) || materials.ContainsKey(uid))
                return "Invalid or duplicate terrain material definition";
            materials.Add(uid, new TerrainMaterialDefinition(uid, name, texture));
            return null;
        }

        uint MaximumUid()
        {
            uint maximum = 0;
            foreach (uint uid in materials.Keys)
                maximum = Math.Max(maximum, uid);
            return maximum;
        }

        public static bool IsSafeRelativePath(string name)
        {
            if (String.IsNullOrEmpty(name) || Path.IsPathRooted(name) ||
                name.IndexOf(':') >= 0 || name.IndexOf('\\') >= 0)
                return false;
            string[] parts = name.Split('/');
            foreach (string part in parts)
                if (String.IsNullOrEmpty(part) || part == "." || part == ".." ||
                    part.Trim() != part || part.EndsWith(".", StringComparison.Ordinal))
                    return false;
            return true;
        }
    }
}
