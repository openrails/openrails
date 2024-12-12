using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orts.Formats.Msts;
using Aspose.ThreeD;
using Aspose.ThreeD.Entities;
using Aspose.ThreeD.Utilities;

namespace ShapeToObj
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string path = Console.ReadLine();
            Console.WriteLine("Deserializing shape");
            ShapeFile sf = new ShapeFile(path, true);

            Console.WriteLine("Converting");
            Scene scn = new Scene();

            Console.WriteLine("Initializing nodes");
            List<Node> nodes = new List<Node>(new Node[sf.shape.prim_states.Count]);
            for (int i = 0; i < nodes.Count; i++) {
                nodes[i] = new Node(sf.shape.prim_states[i].Name);
            }

            Console.WriteLine("Initializing meshes");
            List<Mesh> meshes = new List<Mesh>(new Mesh[sf.shape.prim_states.Count]);
            for (int i = 0; i < meshes.Count; i++)
            {
                meshes[i] = new Mesh();
                foreach (var point in sf.shape.points)
                {
                    meshes[i].ControlPoints.Add(new Vector4(point.X, point.Y, point.Z, 1));
                }
            }

            var sub_objects = sf.shape.lod_controls[0].distance_levels[0].sub_objects;
            Console.WriteLine("Converting subojects");
            foreach (var sub_object in sub_objects)
            {
                foreach (var primitive in sub_object.primitives)
                {
                    foreach (var vtxSet in primitive.indexed_trilist.vertex_idxs)
                    {
                        meshes[primitive.prim_state_idx].CreatePolygon(new int[]
                            {
                                sub_object.vertices[vtxSet.a].ipoint,
                                sub_object.vertices[vtxSet.b].ipoint,
                                sub_object.vertices[vtxSet.c].ipoint
                            });
                    }
                }
            }

            Console.WriteLine("Performing final merges");

            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].AddEntity(meshes[i]);
                scn.RootNode.AddChildNode(nodes[i]);
            }

            Console.WriteLine("Saving file");
            scn.Save("a.obj", FileFormat.WavefrontOBJ);

            Console.WriteLine("Finished");
        }
    }
}
