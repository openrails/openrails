using System;
using System.Collections.Generic;
using Orts.Formats.Msts;
using Aspose.ThreeD;
using Aspose.ThreeD.Entities;
using Aspose.ThreeD.Utilities;
using System.IO;

namespace ShapeToObj
{
    internal class Program
    {

        static void Main(string[] args)
        {
            string path;
            if (args.Length == 0)
            {
                Console.WriteLine("No arguments given. Put absolute path to shape file:");
                path = Console.ReadLine();
                Convert(path);
            }
            else if (args.Length == 1)
            {
                path = args[0];
                Convert(path);
            }
            else
            {
                foreach (string arg in args)
                {
                    Convert(arg);
                }
            }
        }

        static void Convert(string path)
        {
            Console.WriteLine("Deserializing shape");
            ShapeFile sf = new ShapeFile(path, true);

            Console.WriteLine("Converting");
            Scene scn = new Scene();

            Console.WriteLine("Initializing nodes");
            List<Node> nodes = new List<Node>(new Node[sf.shape.prim_states.Count]);
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i] = new Node(sf.shape.prim_states[i].Name);
            }

            Console.WriteLine("Initializing meshes");
            List<Mesh> meshes = new List<Mesh>(new Mesh[sf.shape.prim_states.Count]);
            List<VertexElementUV> eUVs = new List<VertexElementUV>(new VertexElementUV[sf.shape.prim_states.Count]);
            for (int i = 0; i < meshes.Count; i++)
            {
                meshes[i] = new Mesh();
                eUVs[i] = meshes[i].CreateElementUV(TextureMapping.Diffuse, MappingMode.PolygonVertex, ReferenceMode.IndexToDirect);
                foreach (var point in sf.shape.points)
                {
                    meshes[i].ControlPoints.Add(new Vector4(-point.X, point.Y, point.Z, 1));
                }
                foreach (var point in sf.shape.uv_points)
                {
                    eUVs[i].Data.Add(new Vector4(point.U, -point.V, 0, 1));
                }
            }

            var sub_objects = sf.shape.lod_controls[0].distance_levels[0].sub_objects;
            Console.WriteLine("Converting subojects' meshes");
            foreach (var sub_object in sub_objects)
            {
                foreach (var primitive in sub_object.primitives)
                {
                    foreach (var vtxSet in primitive.indexed_trilist.vertex_idxs)
                    {
                        meshes[primitive.prim_state_idx].CreatePolygon(new int[]
                        {
                            sub_object.vertices[vtxSet.c].ipoint,
                            sub_object.vertices[vtxSet.b].ipoint,
                            sub_object.vertices[vtxSet.a].ipoint
                        });
                        eUVs[primitive.prim_state_idx].Indices.AddRange(
                            new int[]
                            {
                                sub_object.vertices[vtxSet.c].vertex_uvs[0],
                                sub_object.vertices[vtxSet.b].vertex_uvs[0],
                                sub_object.vertices[vtxSet.a].vertex_uvs[0]
                            }
                        );
                    }
                }
            }

            Console.WriteLine("Merging meshes with nodes");

            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].AddEntity(meshes[i]);
                scn.RootNode.AddChildNode(nodes[i]);
            }

            /*
            Console.WriteLine("Applying transforms");
            foreach (var node in nodes)
            {
                var matrix = sf.shape.matrices.FirstOrDefault(m => m.Name == node.Name);
            }
            */


            string exportPath = Path.ChangeExtension(path, ".obj");
            Console.WriteLine("Saving file as: {0}", exportPath);
            scn.Save(exportPath, FileFormat.WavefrontOBJ);

            Console.WriteLine("Finished");

        }
    }
}
