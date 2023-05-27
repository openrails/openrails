// COPYRIGHT 2013, 2015 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Orts.Parsers.Msts;

// TODO - UV_OPS

namespace Orts.Formats.Msts
{
    public class ShapeFile
    {
        public shape shape;

        void Validate(string filename)
        {
            if (shape.lod_controls.Count < 1)
                Trace.TraceWarning("Missing at least one LOD Control element in shape {0}", filename);

            for (var distanceLevelIndex = 0; distanceLevelIndex < shape.lod_controls[0].distance_levels.Count; distanceLevelIndex++)
            {
                var distanceLevel = shape.lod_controls[0].distance_levels[distanceLevelIndex];

                if (distanceLevel.distance_level_header.hierarchy.Length != shape.matrices.Count)
                    Trace.TraceWarning("Expected {2} hierarchy elements; got {3} in distance level {1} in shape {0}", filename, distanceLevelIndex, shape.matrices.Count, distanceLevel.distance_level_header.hierarchy.Length);

                for (var hierarchyIndex = 0; hierarchyIndex < distanceLevel.distance_level_header.hierarchy.Length; hierarchyIndex++)
                {
                    var matrixIndex = distanceLevel.distance_level_header.hierarchy[hierarchyIndex];
                    if (matrixIndex < -1 || matrixIndex >= shape.matrices.Count)
                        Trace.TraceWarning("Hierarchy element {2} out of range (expected {3} to {4}; got {5}) in distance level {1} in shape {0}", filename, distanceLevelIndex, hierarchyIndex, -1, shape.matrices.Count - 1, matrixIndex);
                }

                for (var subObjectIndex = 0; subObjectIndex < distanceLevel.sub_objects.Count; subObjectIndex++)
                {
                    var subObject = distanceLevel.sub_objects[subObjectIndex];

                    if (subObject.sub_object_header.geometry_info.geometry_node_map.Length != shape.matrices.Count)
                        Trace.TraceWarning("Expected {3} geometry node map elements; got {4} in sub-object {2} in distance level {1} in shape {0}", filename, distanceLevelIndex, subObjectIndex, shape.matrices.Count, subObject.sub_object_header.geometry_info.geometry_node_map.Length);

                    var geometryNodeMap = subObject.sub_object_header.geometry_info.geometry_node_map;
                    for (var geometryNodeMapIndex = 0; geometryNodeMapIndex < geometryNodeMap.Length; geometryNodeMapIndex++)
                    {
                        var geometryNode = geometryNodeMap[geometryNodeMapIndex];
                        if (geometryNode < -1 || geometryNode >= subObject.sub_object_header.geometry_info.geometry_nodes.Count)
                            Trace.TraceWarning("Geometry node map element {3} out of range (expected {4} to {5}; got {6}) in sub-object {2} in distance level {1} in shape {0}", filename, distanceLevelIndex, subObjectIndex, geometryNodeMapIndex, -1, subObject.sub_object_header.geometry_info.geometry_nodes.Count - 1, geometryNode);
                    }

                    var vertices = subObject.vertices;
                    for (var vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
                    {
                        var vertex = vertices[vertexIndex];

                        if (vertex.ipoint < 0 || vertex.ipoint >= shape.points.Count)
                            Trace.TraceWarning("Point index out of range (expected {4} to {5}; got {6}) in vertex {3} in sub-object {2} in distance level {1} in shape {0}", filename, distanceLevelIndex, subObjectIndex, vertexIndex, 0, shape.points.Count - 1, vertex.ipoint);

                        if (vertex.inormal < 0 || vertex.inormal >= shape.normals.Count)
                            Trace.TraceWarning("Normal index out of range (expected {4} to {5}; got {6}) in vertex {3} in sub-object {2} in distance level {1} in shape {0}", filename, distanceLevelIndex, subObjectIndex, vertexIndex, 0, shape.normals.Count - 1, vertex.inormal);

                        if (vertex.vertex_uvs.Length < 1)
                            Trace.TraceWarning("Missing UV index in vertex {3} in sub-object {2} in distance level {1} in shape {0}", filename, distanceLevelIndex, subObjectIndex, vertexIndex);
                        else if (vertex.vertex_uvs[0] < 0 || vertex.vertex_uvs[0] >= shape.uv_points.Count)
                            Trace.TraceWarning("UV index out of range (expected {4} to {5}; got {6}) in vertex {3} in sub-object {2} in distance level {1} in shape {0}", filename, distanceLevelIndex, subObjectIndex, vertexIndex, 0, shape.uv_points.Count - 1, vertex.vertex_uvs[0]);
                    }

                    for (var primitiveIndex = 0; primitiveIndex < subObject.primitives.Count; primitiveIndex++)
                    {
                        var triangleList = subObject.primitives[primitiveIndex].indexed_trilist;
                        for (var triangleListIndex = 0; triangleListIndex < triangleList.vertex_idxs.Count; triangleListIndex++)
                        {
                            if (triangleList.vertex_idxs[triangleListIndex].a < 0 || triangleList.vertex_idxs[triangleListIndex].a >= vertices.Count)
                                Trace.TraceWarning("Vertex out of range (expected {4} to {5}; got {6}) in primitive {3} in sub-object {2} in distance level {1} in shape {0}", filename, distanceLevelIndex, subObjectIndex, primitiveIndex, 0, vertices.Count - 1, triangleList.vertex_idxs[triangleListIndex].a);
                        }
                    }
                }
            }
        }

        public ShapeFile(string filename, bool suppressShapeWarnings)
        {
            var file = SBR.Open(filename);
            shape = new shape(file.ReadSubBlock());
            file.VerifyEndOfBlock();
            if (!suppressShapeWarnings) Validate(filename);
        }

        public void ReadAnimationBlock(string orFileName)
        {
            var file = SBR.Open(orFileName);
            shape.animations = new animations(file.ReadSubBlock());
            file.VerifyEndOfBlock();
        }


    }

    public class shape
    {
        public shape_header shape_header;
        public volumes volumes;
        public shader_names shader_names;
        public texture_filter_names texture_filter_names;
        public points points;
        public uv_points uv_points;
        public normals normals;
        public sort_vectors sort_vectors;
        public colors colors;
        public matrices matrices;
        public images images;
        public textures textures;
        public light_materials light_materials;
        public light_model_cfgs light_model_cfgs;
        public vtx_states vtx_states;
        public prim_states prim_states;
        public lod_controls lod_controls;
        public animations animations;

        public shape(SBR block)
        {
            block.VerifyID(TokenID.shape);
            shape_header = new shape_header(block.ReadSubBlock());
            volumes = new volumes(block.ReadSubBlock());
            shader_names = new shader_names(block.ReadSubBlock());
            texture_filter_names = new texture_filter_names(block.ReadSubBlock());
            points = new points(block.ReadSubBlock());
            uv_points = new uv_points(block.ReadSubBlock());
            normals = new normals(block.ReadSubBlock());
            sort_vectors = new sort_vectors(block.ReadSubBlock());
            colors = new colors(block.ReadSubBlock());
            matrices = new matrices(block.ReadSubBlock());
            images = new images(block.ReadSubBlock());
            textures = new textures(block.ReadSubBlock());
            light_materials = new light_materials(block.ReadSubBlock());
            light_model_cfgs = new light_model_cfgs(block.ReadSubBlock());
            vtx_states = new vtx_states(block.ReadSubBlock());
            prim_states = new prim_states(block.ReadSubBlock());
            lod_controls = new lod_controls(block.ReadSubBlock());
            if (!block.EndOfBlock())
                animations = new animations(block.ReadSubBlock());
            block.VerifyEndOfBlock();
        }
    }

    public class shape_header
    {
        public UInt32 flags1;
        public UInt32 flags2;

        public shape_header(SBR block)
        {
            block.VerifyID(TokenID.shape_header);
            flags1 = block.ReadFlags();
            if (!block.EndOfBlock())
                flags2 = block.ReadFlags();
            block.VerifyEndOfBlock();
        }
    }

    public class volumes : List<vol_sphere>
    {
        public volumes(SBR block)
        {
            block.VerifyID(TokenID.volumes);
            var count = Capacity = block.ReadInt();
            while (count-- > 0) Add(new vol_sphere(block.ReadSubBlock()));
            block.VerifyEndOfBlock();
        }
    }

    public class vol_sphere
    {
        public vector vector;
        public float Radius;

        public vol_sphere(SBR block)
        {
            block.VerifyID(TokenID.vol_sphere);
            var vectorBlock = block.ReadSubBlock();
            vector.X = vectorBlock.ReadFloat();
            vector.Y = vectorBlock.ReadFloat();
            vector.Z = vectorBlock.ReadFloat();
            vectorBlock.VerifyEndOfBlock();
            Radius = block.ReadFloat();
            block.VerifyEndOfBlock();
        }
    }

    public class shader_names : List<string>
    {
        public shader_names(SBR block)
        {
            block.VerifyID(TokenID.shader_names);
            var count = Capacity = block.ReadInt();
            while (count-- > 0)
            {
                var subBlock = block.ReadSubBlock();
                subBlock.VerifyID(TokenID.named_shader);
                Add(subBlock.ReadString());
                subBlock.VerifyEndOfBlock();
            }
            block.VerifyEndOfBlock();
        }
    }

    public class texture_filter_names : List<string>
    {
        public texture_filter_names(SBR block)
        {
            block.VerifyID(TokenID.texture_filter_names);
            var count = Capacity = block.ReadInt();
            while (count-- > 0)
            {
                var subBlock = block.ReadSubBlock();
                subBlock.VerifyID(TokenID.named_filter_mode);
                Add(subBlock.ReadString());
                subBlock.VerifyEndOfBlock();
            }
            block.VerifyEndOfBlock();
        }
    }

    public class points : List<point>
    {
        public points(SBR block)
        {
            block.VerifyID(TokenID.points);
            var count = Capacity = block.ReadInt();
            while (count-- > 0)
            {
                var subBlock = block.ReadSubBlock();
                subBlock.VerifyID(TokenID.point);
                Add(new point(subBlock.ReadFloat(), subBlock.ReadFloat(), subBlock.ReadFloat()));
                subBlock.VerifyEndOfBlock();
            }
            block.VerifyEndOfBlock();
        }
    }

    public struct point
    {
        public float X, Y, Z;

        public bool Matches(point point)
        {
            if (Math.Abs(point.X - X) > 0.0001) return false;
            if (Math.Abs(point.Y - Y) > 0.0001) return false;
            if (Math.Abs(point.Z - Z) > 0.0001) return false;
            return true;
        }

        public point(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public point(double x, double y, double z)
        {
            X = (float)x;
            Y = (float)y;
            Z = (float)z;
        }
    }

    public class uv_points : List<uv_point>
    {
        public uv_points(SBR block)
        {
            block.VerifyID(TokenID.uv_points);
            var count = Capacity = block.ReadInt();
            while (count-- > 0)
            {
                var subBlock = block.ReadSubBlock();
                subBlock.VerifyID(TokenID.uv_point);
                Add(new uv_point(subBlock.ReadFloat(), subBlock.ReadFloat()));
                subBlock.VerifyEndOfBlock();
            }
            block.VerifyEndOfBlock();
        }
    }

    public struct uv_point
    {
        public float U, V;

        public uv_point(float u, float v)
        {
            U = u;
            V = v;
        }
        public uv_point(double u, double v)
        {
            U = (float)u;
            V = (float)v;
        }

        public bool Matches(uv_point uv_point)
        {
            if (Math.Abs(uv_point.U - U) > 0.0001) return false;
            if (Math.Abs(uv_point.V - V) > 0.0001) return false;
            return true;
        }
    }

    public class normals : List<vector>
    {
        public normals(SBR block)
        {
            block.VerifyID(TokenID.normals);
            var count = Capacity = block.ReadInt();
            while (count-- > 0)
            {
                var subBlock = block.ReadSubBlock();
                subBlock.VerifyID(TokenID.vector);
                Add(new vector(subBlock.ReadFloat(), subBlock.ReadFloat(), subBlock.ReadFloat()));
                subBlock.VerifyEndOfBlock();
            }
            block.VerifyEndOfBlock();
        }
    }

    public struct vector
    {
        public float X, Y, Z;

        public bool Matches(vector vector)
        {
            if (Math.Abs(vector.X - X) > 0.00001) return false;
            if (Math.Abs(vector.Y - Y) > 0.00001) return false;
            if (Math.Abs(vector.Z - Z) > 0.00001) return false;
            return true;
        }

        public vector(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        public vector(double x, double y, double z)
        {
            X = (float)x;
            Y = (float)y;
            Z = (float)z;
        }
    }

    public class sort_vectors : List<vector>
    {
        public sort_vectors(SBR block)
        {
            block.VerifyID(TokenID.sort_vectors);
            var count = Capacity = block.ReadInt();
            while (count-- > 0)
            {
                var subBlock = block.ReadSubBlock();
                subBlock.VerifyID(TokenID.vector);
                Add(new vector(subBlock.ReadFloat(), subBlock.ReadFloat(), subBlock.ReadFloat()));
                subBlock.VerifyEndOfBlock();
            }
            block.VerifyEndOfBlock();
        }
    }

    public class colors : List<color>
    {
        public colors(SBR block)
        {
            block.VerifyID(TokenID.colours);
            var count = Capacity = block.ReadInt();
            while (count-- > 0)
            {
                var subBlock = block.ReadSubBlock();
                subBlock.VerifyID(TokenID.colour);
                Add(new color()
                {
                    A = subBlock.ReadFloat(),
                    R = subBlock.ReadFloat(),
                    G = subBlock.ReadFloat(),
                    B = subBlock.ReadFloat(),
                });
                subBlock.VerifyEndOfBlock();
            }
            block.VerifyEndOfBlock();
        }
    }

    public struct color
    {
        public float R, G, B, A;
    }

    public class matrices : List<matrix>
    {
        public matrices(SBR block)
        {
            block.VerifyID(TokenID.matrices);
            var count = Capacity = block.ReadInt();
            while (count-- > 0) Add(new matrix(block.ReadSubBlock()));
            block.VerifyEndOfBlock();
        }
    }

    public class matrix
    {
        public string Name;
        public float AX, AY, AZ, BX, BY, BZ, CX, CY, CZ, DX, DY, DZ;

        public float this[int i, int j]
        {
            get
            {
                switch (i * 4 + j)
                {
                    case 0: return AX;
                    case 1: return AY;
                    case 2: return AZ;
                    case 3: return 0.0f;

                    case 4: return BX;
                    case 5: return BY;
                    case 6: return BZ;
                    case 7: return 0.0f;

                    case 8: return CX;
                    case 9: return CY;
                    case 10: return CZ;
                    case 11: return 0.0f;

                    case 12: return DX;
                    case 13: return DY;
                    case 14: return DZ;
                    case 15: return 1.0f;

                    default: throw new System.Exception("Array index out of bounds");
                }
            }
            set
            {
                switch (i * 4 + j)
                {
                    case 0: AX = value; break;
                    case 1: AY = value; break;
                    case 2: AZ = value; break;
                    case 3: break;

                    case 4: BX = value; break;
                    case 5: BY = value; break;
                    case 6: BZ = value; break;
                    case 7: break;

                    case 8: CX = value; break;
                    case 9: CY = value; break;
                    case 10: CZ = value; break;
                    case 11: break;

                    case 12: DX = value; break;
                    case 13: DY = value; break;
                    case 14: DZ = value; break;
                    case 15: break;
                }

            }
        }

        public matrix()
        {
            AX = 1; AY = 0; AZ = 0;
            BX = 0; BY = 1; BZ = 0;
            CX = 0; CY = 0; CZ = 1;
            DX = 0; DY = 0; DZ = 0;
        }

        public matrix(SBR block)
        {
            block.VerifyID(TokenID.matrix);

            if (block.Label != null)
                Name = block.Label;

            AX = block.ReadFloat();
            AY = block.ReadFloat();
            AZ = block.ReadFloat();
            BX = block.ReadFloat();
            BY = block.ReadFloat();
            BZ = block.ReadFloat();
            CX = block.ReadFloat();
            CY = block.ReadFloat();
            CZ = block.ReadFloat();
            DX = block.ReadFloat();
            DY = block.ReadFloat();
            DZ = block.ReadFloat();

            block.VerifyEndOfBlock();
        }

        public matrix(string name)
        {
            // identity matrix
            AX = 1; AY = 0; AZ = 0;
            BX = 0; BY = 1; BZ = 0;
            CX = 0; CY = 0; CZ = 1;
            DX = 0; DY = 0; DZ = 0;

            Name = name;
        }

        /// <summary>
        /// Ignores name
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool Matches(matrix target)
        {
            return AX == target.AX && AY == target.AY && AZ == target.AZ
                && BX == target.BX && BY == target.BY && BZ == target.BZ
                && CX == target.CX && CY == target.CY && CZ == target.CZ
                && DX == target.DX && DY == target.DY && DZ == target.DZ;
        }
    }

    public class images : List<string>
    {
        public images(SBR block)
        {
            block.VerifyID(TokenID.images);
            var count = Capacity = block.ReadInt();
            while (count-- > 0)
            {
                var subBlock = block.ReadSubBlock();
                subBlock.VerifyID(TokenID.image);
                Add(subBlock.ReadString());
                subBlock.VerifyEndOfBlock();
            }
            block.VerifyEndOfBlock();
        }
    }

    public class textures : List<texture>
    {
        public textures(SBR block)
        {
            block.VerifyID(TokenID.textures);
            var count = Capacity = block.ReadInt();
            while (count-- > 0) Add(new texture(block.ReadSubBlock()));
            block.VerifyEndOfBlock();
        }
    }

    public class texture
    {
        /*texture                 ==> :uint,ImageIdx :uint,FilterMode :float,MipMapLODBias [:dword,BorderColor] .
				// Provides attributes for each image
				eg	texture ( 1 0 -3 ff000000 )

				MipMapLODBias  -3  fixes blurring, 0  can cause some texture blurring
         */
        public int iImage;
        public int FilterMode;
        public float MipMapLODBias;
        public UInt32 BorderColor;

        public texture(SBR block)
        {
            block.VerifyID(TokenID.texture);
            iImage = block.ReadInt();
            FilterMode = block.ReadInt();
            MipMapLODBias = block.ReadFloat();
            if (!block.EndOfBlock())
                BorderColor = block.ReadFlags();
            block.VerifyEndOfBlock();
        }

        public texture(int newiImage)
        {
            iImage = newiImage;
            FilterMode = 0;
            MipMapLODBias = -3;
            BorderColor = 0xff000000U;
        }

        public bool Matches(texture texture)
        {
            if (iImage != texture.iImage) return false;
            if (FilterMode != texture.FilterMode) return false;
            if (MipMapLODBias != texture.MipMapLODBias) return false;
            if (BorderColor != texture.BorderColor) return false;
            return true;
        }
    }

    public class light_materials : List<light_material>
    {
        public light_materials(SBR block)
        {
            block.VerifyID(TokenID.light_materials);
            var count = Capacity = block.ReadInt();
            while (count-- > 0) Add(new light_material(block.ReadSubBlock()));
            block.VerifyEndOfBlock();
        }
    }

    public class light_material
    {
        /*light_material          ==> :dword,flags :uint,DiffColIdx :uint,AmbColIdx :uint,SpecColIdx :uint,EmissiveColIdx :float,SpecPower .
				// Never seen it used
				eg	light_materials ( 0 )
         */
        public UInt32 flags;
        public int DiffColIdx;
        public int AmbColIdx;
        public int SpecColIdx;
        public int EmissiveColIdx;
        public float SpecPower;

        public light_material(SBR block)
        {
            block.VerifyID(TokenID.light_material);
            flags = block.ReadFlags();
            DiffColIdx = block.ReadInt();
            AmbColIdx = block.ReadInt();
            SpecColIdx = block.ReadInt();
            EmissiveColIdx = block.ReadInt();
            SpecPower = block.ReadFloat();
            block.VerifyEndOfBlock();
        }
    }

    public class light_model_cfgs : List<light_model_cfg>
    {
        public light_model_cfgs(SBR block)
        {
            block.VerifyID(TokenID.light_model_cfgs);
            var count = Capacity = block.ReadInt();
            while (count-- > 0) Add(new light_model_cfg(block.ReadSubBlock()));
            block.VerifyEndOfBlock();
        }
    }

    public class light_model_cfg
    {
        public UInt32 flags;
        public uv_ops uv_ops;

        public light_model_cfg(SBR block)
        {
            block.VerifyID(TokenID.light_model_cfg);
            flags = block.ReadFlags();
            uv_ops = new uv_ops(block.ReadSubBlock());
            block.VerifyEndOfBlock();
        }
    }

    public class uv_ops : List<uv_op>
    {
        public uv_ops(SBR block)
        {
            block.VerifyID(TokenID.uv_ops);
            var count = Capacity = block.ReadInt();
            while (count-- > 0)
            {
                var subBlock = block.ReadSubBlock();
                switch (subBlock.ID)
                {
                    case TokenID.uv_op_copy: Add(new uv_op_copy(subBlock)); break;
                    case TokenID.uv_op_reflectmapfull: Add(new uv_op_reflectmapfull(subBlock)); break;
                    case TokenID.uv_op_reflectmap: Add(new uv_op_reflectmap(subBlock)); break;
                    case TokenID.uv_op_uniformscale: this.Add(new uv_op_uniformscale(subBlock)); break;
                    case TokenID.uv_op_nonuniformscale: this.Add(new uv_op_nonuniformscale(subBlock)); break;
                    default: throw new System.Exception("Unexpected uv_op: " + subBlock.ID.ToString());
                }
            }
            block.VerifyEndOfBlock();
        }
    }

    public abstract class uv_op
    {
        public int TexAddrMode;
    }

    // TODO  Add a bunch more uv_ops

    public class uv_op_copy : uv_op
    {
        public int SrcUVIdx;

        public uv_op_copy(SBR block)
        {
            block.VerifyID(TokenID.uv_op_copy);
            TexAddrMode = block.ReadInt();
            SrcUVIdx = block.ReadInt();
            block.VerifyEndOfBlock();
        }
    }

    public class uv_op_reflectmapfull : uv_op
    {
        public uv_op_reflectmapfull(SBR block)
        {
            block.VerifyID(TokenID.uv_op_reflectmapfull);
            TexAddrMode = block.ReadInt();
            block.VerifyEndOfBlock();
        }
    }

    public class uv_op_reflectmap : uv_op
    {
        public uv_op_reflectmap(SBR block)
        {
            block.VerifyID(TokenID.uv_op_reflectmap);
            TexAddrMode = block.ReadInt();
            block.VerifyEndOfBlock();
        }
    }

    public class uv_op_uniformscale : uv_op
    {
        public int SrcUVIdx;
        public float UnknownParameter3;
        public float UnknownParameter4;

        public uv_op_uniformscale(SBR block)
        {
            block.VerifyID(TokenID.uv_op_uniformscale);
            TexAddrMode = block.ReadInt();
            SrcUVIdx = block.ReadInt();
            UnknownParameter3 = block.ReadFloat();
            block.VerifyEndOfBlock();
            block.TraceInformation(String.Format("{0} was treated as uv_op_copy", block.ID.ToString()));
        }
    }

    public class uv_op_nonuniformscale : uv_op
    {
        public int SrcUVIdx;
        public float UnknownParameter3;
        public float UnknownParameter4;

        public uv_op_nonuniformscale(SBR block)
        {
            block.VerifyID(TokenID.uv_op_nonuniformscale);
            TexAddrMode = block.ReadInt();
            SrcUVIdx = block.ReadInt();
            UnknownParameter3 = block.ReadFloat();
            UnknownParameter4 = block.ReadFloat();
            block.VerifyEndOfBlock();
            block.TraceInformation(String.Format("{0} was treated as uv_op_copy", block.ID.ToString()));
        }
    }

    public class vtx_states : List<vtx_state>
    {
        public vtx_states(SBR block)
        {
            block.VerifyID(TokenID.vtx_states);
            var count = Capacity = block.ReadInt();
            while (count-- > 0) Add(new vtx_state(block.ReadSubBlock()));
            block.VerifyEndOfBlock();
        }
    }

    public class vtx_state
    {
        // eg	vtx_state ( 00000000 0 -5 0 00000002 )
        // dword,flags :uint,MatrixIdx :sint,LightMatIdx :uint,LightCfgIdx :dword,LightFlags [:sint,matrix2] .
        public UInt32 flags;
        public int imatrix;
        public int LightMatIdx = -5;
        public int lighting { get { return LightMatIdx; } }
        public int LightCfgIdx;
        public UInt32 LightFlags = 2;
        public int Matrix2 = -1;

        public vtx_state()
        {
        }

        public vtx_state(int newimatrix)
        {
            imatrix = newimatrix;
        }

        public vtx_state(SBR block)
        {
            block.VerifyID(TokenID.vtx_state);
            flags = block.ReadFlags();
            imatrix = block.ReadInt();
            LightMatIdx = block.ReadInt();
            LightCfgIdx = block.ReadInt();
            LightFlags = block.ReadFlags();
            if (!block.EndOfBlock())
                Matrix2 = block.ReadInt();
            block.VerifyEndOfBlock();
        }
        public vtx_state(vtx_state copy)
        {
            flags = copy.flags;
            imatrix = copy.imatrix;
            LightMatIdx = copy.LightMatIdx;
            LightCfgIdx = copy.LightCfgIdx;
            LightFlags = copy.LightFlags;
        }

        public bool Matches(vtx_state target)
        {
            return flags == target.flags
                && imatrix == target.imatrix
                && LightMatIdx == target.LightMatIdx
                && LightCfgIdx == target.LightCfgIdx
                && LightFlags == target.LightFlags;
        }
    }

    public class prim_states : List<prim_state>
    {
        public prim_states(SBR block)
        {
            block.VerifyID(TokenID.prim_states);
            var count = Capacity = block.ReadInt();
            while (count-- > 0) Add(new prim_state(block.ReadSubBlock()));
            block.VerifyEndOfBlock();
        }
    }

    public class prim_state
    {/* prim_state              ==> :dword,flags :uint,ShaderIdx :tex_idxs :float,ZBias :sint,VertStateIdx [:uint,alphatestmode] [:uint,LightCfgIdx] [:uint,ZBufMode] .
        tex_idxs                ==> :uint,NumTexIdxs [{:uint}] .
				eg  	prim_state ( 00000000 0
						tex_idxs ( 1 0 ) 0 0 0 0 1
					)*/

        public string Name;
        public UInt32 flags;
        public int ishader;
        public int[] tex_idxs;
        public float ZBias;
        public int ivtx_state;
        public int alphatestmode;
        public int LightCfgIdx;
        public int ZBufMode;
        public int itexture { get { return tex_idxs[0]; } }

        public prim_state(int newiTexture, int newiShader, int newiVtxState)
        {
            flags = 0;
            ishader = newiShader;
            tex_idxs = new int[1];
            tex_idxs[0] = newiTexture;
            ZBias = 0;
            ivtx_state = newiVtxState;
            alphatestmode = 0;
            LightCfgIdx = 0;
            ZBufMode = 1;
        }

        public prim_state(SBR block)
        {
            block.VerifyID(TokenID.prim_state);

            Name = block.Label;

            flags = block.ReadFlags();
            ishader = block.ReadInt();
            {
                var subBlock = block.ReadSubBlock();
                subBlock.VerifyID(TokenID.tex_idxs);
                tex_idxs = new int[subBlock.ReadInt()];
                for (var i = 0; i < tex_idxs.Length; ++i) tex_idxs[i] = subBlock.ReadInt();
                subBlock.VerifyEndOfBlock();
            }
            ZBias = block.ReadFloat();
            ivtx_state = block.ReadInt();
            alphatestmode = block.ReadInt();
            LightCfgIdx = block.ReadInt();
            ZBufMode = block.ReadInt();
            block.VerifyEndOfBlock();
        }


        public prim_state(prim_state copy)
        {
            flags = copy.flags;
            ishader = copy.ishader;
            tex_idxs = (int[])copy.tex_idxs.Clone();
            ZBias = copy.ZBias;
            ivtx_state = copy.ivtx_state;
            alphatestmode = copy.alphatestmode;
            LightCfgIdx = copy.LightCfgIdx;
            ZBufMode = copy.ZBufMode;
        }

        public bool Matches(prim_state prim_state)
        {
            if (flags != prim_state.flags) return false;
            if (ishader != prim_state.ishader) return false;
            if (tex_idxs.Length != prim_state.tex_idxs.Length) return false;
            if (!tex_idxs.SequenceEqual(prim_state.tex_idxs)) return false;
            if (ZBias != prim_state.ZBias) return false;
            if (ivtx_state != prim_state.ivtx_state) return false;
            if (alphatestmode != prim_state.alphatestmode) return false;
            if (LightCfgIdx != prim_state.LightCfgIdx) return false;
            if (ZBufMode != prim_state.ZBufMode) return false;
            return true;
        }
    }

    public class lod_controls : List<lod_control>
    {
        public lod_controls(SBR block)
        {
            block.VerifyID(TokenID.lod_controls);
            var count = Capacity = block.ReadInt();
            while (count-- > 0) Add(new lod_control(block.ReadSubBlock()));
            block.VerifyEndOfBlock();
        }
    }

    public class lod_control
    {
        public distance_levels_header distance_levels_header;
        public distance_levels distance_levels;

        public lod_control(SBR block)
        {
            block.VerifyID(TokenID.lod_control);
            distance_levels_header = new distance_levels_header(block.ReadSubBlock());
            distance_levels = new distance_levels(block.ReadSubBlock());
            block.VerifyEndOfBlock();
        }
    }

    public class distance_levels_header
    {
        public int DlevBias;

        public distance_levels_header(SBR block)
        {
            block.VerifyID(TokenID.distance_levels_header);
            DlevBias = block.ReadInt();
            block.VerifyEndOfBlock();
        }
    }

    public class distance_levels : List<distance_level>
    {
        public distance_levels(SBR block)
        {
            block.VerifyID(TokenID.distance_levels);
            var count = Capacity = block.ReadInt();
            while (count-- > 0) Add(new distance_level(block.ReadSubBlock()));
            block.VerifyEndOfBlock();
        }
    }

    public class distance_level
    {
        public distance_level_header distance_level_header;
        public sub_objects sub_objects;

        public distance_level(SBR block)
        {
            block.VerifyID(TokenID.distance_level);
            distance_level_header = new distance_level_header(block.ReadSubBlock());
            sub_objects = new sub_objects(block.ReadSubBlock());
            block.VerifyEndOfBlock();
        }
    }

    public class distance_level_header
    {
        public float dlevel_selection;
        public int[] hierarchy;

        public distance_level_header(SBR block)
        {
            block.VerifyID(TokenID.distance_level_header);
            {
                var subBlock = block.ReadSubBlock();
                subBlock.VerifyID(TokenID.dlevel_selection);
                dlevel_selection = subBlock.ReadFloat();
                subBlock.VerifyEndOfBlock();
            }
            {
                var subBlock = block.ReadSubBlock();
                subBlock.VerifyID(TokenID.hierarchy);
                hierarchy = new int[subBlock.ReadInt()];
                for (var i = 0; i < hierarchy.Length; ++i) hierarchy[i] = subBlock.ReadInt();
                subBlock.VerifyEndOfBlock();
            }
            block.VerifyEndOfBlock();
        }
    }

    public class sub_objects : List<sub_object>
    {
        public sub_objects(SBR block)
        {
            block.VerifyID(TokenID.sub_objects);
            var count = Capacity = block.ReadInt();
            while (count-- > 0) Add(new sub_object(block.ReadSubBlock()));
            block.VerifyEndOfBlock();
        }
    }

    public class sub_object
    {
        public sub_object_header sub_object_header;
        public vertices vertices;
        public vertex_sets vertex_sets;
        public primitives primitives;

        public sub_object(SBR block)
        {
            block.VerifyID(TokenID.sub_object);
            sub_object_header = new sub_object_header(block.ReadSubBlock());
            vertices = new vertices(block.ReadSubBlock());
            vertex_sets = new vertex_sets(block.ReadSubBlock());
            primitives = new primitives(block.ReadSubBlock());
            block.VerifyEndOfBlock();
        }
    }

    public class sub_object_header
    {
        //:dword,flags :sint,SortVectorIdx :sint,VolIdx :dword,SrcVtxFmtFlags :dword,DstVtxFmtFlags :geometry_info,GeomInfo 
        //                               [:subobject_shaders,SubObjShaders] [:subobject_light_cfgs,SubObjLightCfgs] [:uint,SubObjID] .
        public UInt32 flags;
        public int SortVectorIdx;
        public int VolIdx;
        public UInt32 SrcVtxFmtFlags;
        public UInt32 DstVtxFmtFlags;
        public geometry_info geometry_info;
        public int[] subobject_shaders;
        public int[] subobject_light_cfgs;
        public int SubObjID;

        public sub_object_header(SBR block)
        {
            block.VerifyID(TokenID.sub_object_header);

            flags = block.ReadFlags();
            SortVectorIdx = block.ReadInt();
            VolIdx = block.ReadInt();
            SrcVtxFmtFlags = block.ReadFlags();
            DstVtxFmtFlags = block.ReadFlags();
            geometry_info = new geometry_info(block.ReadSubBlock());

            if (!block.EndOfBlock())
            {
                var subBlock = block.ReadSubBlock();
                subBlock.VerifyID(TokenID.subobject_shaders);
                subobject_shaders = new int[subBlock.ReadInt()];
                for (var i = 0; i < subobject_shaders.Length; ++i)
                    subobject_shaders[i] = subBlock.ReadInt();
                subBlock.VerifyEndOfBlock();
            }

            if (!block.EndOfBlock())
            {
                var subBlock = block.ReadSubBlock();
                subBlock.VerifyID(TokenID.subobject_light_cfgs);
                subobject_light_cfgs = new int[subBlock.ReadInt()];
                for (var i = 0; i < subobject_light_cfgs.Length; ++i)
                    subobject_light_cfgs[i] = subBlock.ReadInt();
                subBlock.VerifyEndOfBlock();
            }

            if (!block.EndOfBlock())
                SubObjID = block.ReadInt();

            block.VerifyEndOfBlock();
        }
    }

    public class geometry_info
    {
        public int FaceNormals;
        public int TxLightCmds;
        public int NodeXTxLightCmds;
        public int TrilistIdxs;
        public int LineListIdxs;
        public int NodeXTrilistIdxs;
        public int Trilists;
        public int LineLists;
        public int PtLists;
        public int NodeXTrilists;

        public geometry_nodes geometry_nodes;
        public int[] geometry_node_map;

        public geometry_info(SBR block)
        {
            block.VerifyID(TokenID.geometry_info);
            FaceNormals = block.ReadInt();
            TxLightCmds = block.ReadInt();
            NodeXTrilistIdxs = block.ReadInt();
            TrilistIdxs = block.ReadInt();
            LineListIdxs = block.ReadInt();
            NodeXTrilistIdxs = block.ReadInt();
            Trilists = block.ReadInt();
            LineLists = block.ReadInt();
            PtLists = block.ReadInt();
            NodeXTrilists = block.ReadInt();
            geometry_nodes = new geometry_nodes(block.ReadSubBlock());
            {
                var subBlock = block.ReadSubBlock();
                subBlock.VerifyID(TokenID.geometry_node_map);
                geometry_node_map = new int[subBlock.ReadInt()];
                for (var i = 0; i < geometry_node_map.Length; ++i)
                    geometry_node_map[i] = subBlock.ReadInt();
                subBlock.VerifyEndOfBlock();
            }
            block.VerifyEndOfBlock();
        }
    }

    public class geometry_nodes : List<geometry_node>
    {
        public geometry_nodes(SBR block)
        {
            block.VerifyID(TokenID.geometry_nodes);
            var count = Capacity = block.ReadInt();
            while (count-- > 0) Add(new geometry_node(block.ReadSubBlock()));
            block.VerifyEndOfBlock();
        }
    }

    public class geometry_node
    {
        public int TxLightCmds;
        public int NodeXTxLightCmds;
        public int TriLists;
        public int LineLists;
        public int PtLists;
        public cullable_prims cullable_prims;

        public geometry_node()
        {
            TxLightCmds = 1;
            NodeXTxLightCmds = 0;
            TriLists = 0;
            LineLists = 0;
            PtLists = 0;
            cullable_prims = new cullable_prims();
        }

        public geometry_node(SBR block)
        {
            block.VerifyID(TokenID.geometry_node);
            TxLightCmds = block.ReadInt();
            NodeXTxLightCmds = block.ReadInt();
            TriLists = block.ReadInt();
            LineLists = block.ReadInt();
            PtLists = block.ReadInt();
            cullable_prims = new cullable_prims(block.ReadSubBlock());
            block.VerifyEndOfBlock();
        }
    }

    public class cullable_prims
    {
        public int NumPrims;
        public int NumFlatSections;
        public int NumPrimIdxs;

        public cullable_prims()
        {
            NumPrims = 0;
            NumFlatSections = 0;
            NumPrimIdxs = 0;
        }

        public cullable_prims(SBR block)
        {
            block.VerifyID(TokenID.cullable_prims);
            NumPrims = block.ReadInt();
            NumFlatSections = block.ReadInt();
            NumPrimIdxs = block.ReadInt();
            block.VerifyEndOfBlock();
        }
    }

    public class vertices : List<vertex>
    {
        public vertices(SBR block)
        {
            block.VerifyID(TokenID.vertices);
            var count = Capacity = block.ReadInt();
            while (count-- > 0) Add(new vertex(block.ReadSubBlock()));
            block.VerifyEndOfBlock();
        }
    }

    public class vertex
    {
        public UInt32 flags;
        public int ipoint;
        public int inormal;
        public UInt32 Color1;
        public UInt32 Color2;
        public int[] vertex_uvs;

        public vertex(SBR block)
        {
            block.VerifyID(TokenID.vertex);
            flags = block.ReadFlags();
            ipoint = block.ReadInt();
            inormal = block.ReadInt();
            Color1 = block.ReadFlags();
            Color2 = block.ReadFlags();
            {
                var subBlock = block.ReadSubBlock();
                subBlock.VerifyID(TokenID.vertex_uvs);
                vertex_uvs = new int[subBlock.ReadInt()];
                for (var i = 0; i < vertex_uvs.Length; ++i) vertex_uvs[i] = subBlock.ReadInt();
                subBlock.VerifyEndOfBlock();
            }
            block.VerifyEndOfBlock();
        }

        public vertex(vertex copy)
        {
            flags = copy.flags;
            ipoint = copy.ipoint;
            inormal = copy.inormal;
            Color1 = copy.Color1;
            Color2 = copy.Color2;
            vertex_uvs = (int[])copy.vertex_uvs.Clone();
        }

        public vertex()
        {
            flags = 0;
            ipoint = 0;
            inormal = 0;
            Color1 = (uint)0xffffffffU;
            Color2 = (uint)0xff000000U;
            vertex_uvs = new int[1];
            vertex_uvs[0] = 0;
        }

        public bool MatchesContent(vertex vertex)
        {
            if (flags != vertex.flags) return false;
            if (ipoint != vertex.ipoint) return false;
            if (inormal != vertex.inormal) return false;
            if (Color1 != vertex.Color1) return false;
            if (Color2 != vertex.Color2) return false;
            if (!vertex_uvs.SequenceEqual(vertex.vertex_uvs)) return false;
            return true;
        }
    }

    public class vertex_sets : List<vertex_set>
    {
        public vertex_sets(SBR block)
        {
            block.VerifyID(TokenID.vertex_sets);
            var count = Capacity = block.ReadInt();
            while (count-- > 0) Add(new vertex_set(block.ReadSubBlock()));
            block.VerifyEndOfBlock();
        }
    }

    public class vertex_set
    {
        public int VtxStateIdx;
        public int StartVtxIdx;
        public int VtxCount;

        public vertex_set()
        {
            VtxStateIdx = 0;
            StartVtxIdx = 0;
            VtxCount = 0;
        }

        public vertex_set(SBR block)
        {
            block.VerifyID(TokenID.vertex_set);
            VtxStateIdx = block.ReadInt();
            StartVtxIdx = block.ReadInt();
            VtxCount = block.ReadInt();
            block.VerifyEndOfBlock();
        }
    }

    public class primitives : List<primitive>
    {
        public primitives(SBR block)
        {
            block.VerifyID(TokenID.primitives);
            var last_prim_state_idx = 0;
            var count = Capacity = block.ReadInt();
            while (count-- > 0)
            {
                var subBlock = block.ReadSubBlock();
                switch (subBlock.ID)
                {
                    case TokenID.prim_state_idx: last_prim_state_idx = subBlock.ReadInt(); subBlock.VerifyEndOfBlock(); break;
                    case TokenID.indexed_trilist: Add(new primitive(subBlock, last_prim_state_idx)); break;
                    default: throw new System.Exception("Unexpected primitive type " + subBlock.ID.ToString());
                }
            }
            block.VerifyEndOfBlock();
        }
    }

    public class primitive
    {
        public int prim_state_idx;
        public indexed_trilist indexed_trilist;

        public primitive(SBR block, int last_prim_state_idx)
        {
            prim_state_idx = last_prim_state_idx;
            indexed_trilist = new indexed_trilist(block);
        }
    }

    public class indexed_trilist
    {
        public vertex_idxs vertex_idxs;
        public int[] normal_idxs;
        public UInt32[] flags;

        public indexed_trilist(SBR block)
        {
            block.VerifyID(TokenID.indexed_trilist);
            vertex_idxs = new vertex_idxs(block.ReadSubBlock());
            {
                var subBlock = block.ReadSubBlock();
                subBlock.VerifyID(TokenID.normal_idxs);
                normal_idxs = new int[subBlock.ReadInt()];
                for (var i = 0; i < normal_idxs.Length; ++i)
                {
                    normal_idxs[i] = subBlock.ReadInt();
                    subBlock.ReadInt(); // skip the '3' value - its purpose unknown
                }
                subBlock.VerifyEndOfBlock();
            }
            {
                var subBlock = block.ReadSubBlock();
                subBlock.VerifyID(TokenID.flags);
                flags = new UInt32[subBlock.ReadInt()];
                for (var i = 0; i < flags.Length; ++i) flags[i] = subBlock.ReadFlags();
                subBlock.VerifyEndOfBlock();
            }
            block.VerifyEndOfBlock();
        }
    }

    public class vertex_idxs : List<vertex_idx>
    {
        public vertex_idxs(SBR block)
        {
            block.VerifyID(TokenID.vertex_idxs);
            var count = Capacity = block.ReadInt() / 3;
            while (count-- > 0) Add(new vertex_idx(block));
            block.VerifyEndOfBlock();
        }
    }

    public class vertex_idx
    {
        public int a, b, c;

        public vertex_idx(SBR block)
        {
            a = block.ReadInt();
            b = block.ReadInt();
            c = block.ReadInt();
        }

        public vertex_idx(int ia, int ib, int ic)
        {
            a = ia;
            b = ib;
            c = ic;
        }
    }

    public class animations : List<animation>
    {
        public animations(SBR block)
        {
            block.VerifyID(TokenID.animations);
            var count = Capacity = block.ReadInt();
            while (count-- > 0) Add(new animation(block.ReadSubBlock()));
            block.VerifyEndOfBlock();
        }
    }

    public class animation
    {
        public int FrameCount;          // :uint,num_frames
        public int FrameRate;           // :uint,frame_rate 
        public anim_nodes anim_nodes;    // :anim_nodes,AnimNodes .

        public animation(SBR block)
        {
            block.VerifyID(TokenID.animation);
            FrameCount = block.ReadInt();
            FrameRate = block.ReadInt();
            anim_nodes = new anim_nodes(block.ReadSubBlock());
            block.VerifyEndOfBlock();
        }
    }

    public class anim_nodes : List<anim_node>
    {
        public anim_nodes(SBR block)
        {
            block.VerifyID(TokenID.anim_nodes);
            var count = Capacity = block.ReadInt();
            while (count-- > 0) Add(new anim_node(block.ReadSubBlock()));
            block.VerifyEndOfBlock();
        }
    }

    public class anim_node
    {
        public string Name;
        public controllers controllers;

        public anim_node(SBR block)
        {
            block.VerifyID(TokenID.anim_node);
            Name = block.Label;
            controllers = new controllers(block.ReadSubBlock());
            block.VerifyEndOfBlock();
        }
    }

    public class controllers : List<controller>
    {
        public controllers(SBR block)
        {
            block.VerifyID(TokenID.controllers);
            var count = Capacity = block.ReadInt();
            while (count-- > 0)
            {
                var subBlock = block.ReadSubBlock();
                switch (subBlock.ID)
                {
                    case TokenID.linear_pos: Add(new linear_pos(subBlock)); break;
                    case TokenID.tcb_rot: Add(new tcb_rot(subBlock)); break;
                    default: throw new System.Exception("Unexpected animation controller " + subBlock.ID.ToString());
                }
            }
            block.VerifyEndOfBlock();
        }
    }

    public abstract class controller : List<KeyPosition>
    {
    }

    public abstract class KeyPosition
    {
        public int Frame;
    }

    public class tcb_rot : controller
    {
        public tcb_rot(SBR block)
        {
            block.VerifyID(TokenID.tcb_rot);
            var count = Capacity = block.ReadInt();
            while (count-- > 0)
            {
                var subBlock = block.ReadSubBlock();
                switch (subBlock.ID)
                {
                    case TokenID.slerp_rot: Add(new slerp_rot(subBlock)); break;
                    case TokenID.tcb_key: Add(new tcb_key(subBlock)); break;
                    default: throw new System.Exception("Unexpected block " + subBlock.ID.ToString());
                }
            }
            block.VerifyEndOfBlock();
        }
    }

    public class slerp_rot : KeyPosition
    {
        public float X, Y, Z, W;   //:float,x :float,y :float,z :float,w .

        public slerp_rot(SBR block)
        {
            block.VerifyID(TokenID.slerp_rot);
            Frame = block.ReadInt();
            X = block.ReadFloat();
            Y = block.ReadFloat();
            Z = block.ReadFloat();
            W = block.ReadFloat();
            block.VerifyEndOfBlock();
        }
    }

    public class linear_pos : controller
    {
        public linear_pos(SBR block)
        {
            block.VerifyID(TokenID.linear_pos);
            var count = Capacity = block.ReadInt();
            while (count-- > 0) Add(new linear_key(block.ReadSubBlock()));
            block.VerifyEndOfBlock();
        }
    }

    public class linear_key : KeyPosition
    {
        public float X, Y, Z;   //:float,x :float,y :float,z

        public linear_key(SBR block)
        {
            block.VerifyID(TokenID.linear_key);
            Frame = block.ReadInt();
            X = block.ReadFloat();
            Y = block.ReadFloat();
            Z = block.ReadFloat();
            block.VerifyEndOfBlock();
        }
    }

    public class tcb_pos : controller
    {
        public tcb_pos(SBR block)
        {
            block.VerifyID(TokenID.tcb_pos);
            var count = Capacity = block.ReadInt();
            while (count-- > 0) Add(new tcb_key(block.ReadSubBlock()));
            block.VerifyEndOfBlock();
        }
    }

    public class tcb_key : KeyPosition
    {
        public float X, Y, Z, W;   //:float,x :float,y :float,z :float,w
        public float Tension, Continuity, Bias, In, Out; // :float,tension :float,continuity :float,bias :float,in :float,out .

        public tcb_key(SBR block)
        {
            block.VerifyID(TokenID.tcb_key);
            Frame = block.ReadInt();
            X = block.ReadFloat();
            Y = block.ReadFloat();
            Z = block.ReadFloat();
            W = block.ReadFloat();
            Tension = block.ReadFloat();
            Continuity = block.ReadFloat();
            Bias = block.ReadFloat();
            In = block.ReadFloat();
            Out = block.ReadFloat();
            block.VerifyEndOfBlock();
        }
    }
}
