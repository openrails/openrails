// COPYRIGHT 2014 by the Open Rails project.
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

using Orts.Formats.Msts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ORTS.ContentManager.Models
{
    public class Path
    {
        public readonly string Name;
        public readonly string StartName;
        public readonly string EndName;

        public readonly IEnumerable<Node> Nodes;

        public Path(Content content)
        {
            Debug.Assert(content.Type == ContentType.Path);
            if (System.IO.Path.GetExtension(content.PathName).Equals(".pat", StringComparison.OrdinalIgnoreCase))
            {
                var file = new PathFile(content.PathName);
                Name = file.Name;
                StartName = file.Start;
                EndName = file.End;

                var nodes = new List<Node>(file.TrPathNodes.Count);
                var nodeNexts = new List<List<Node>>(file.TrPathNodes.Count);
                foreach (var node in file.TrPathNodes)
                {
                    var pdp = file.TrackPDPs[(int)node.fromPDP];
                    var location = String.Format("{0:D} {1:D} ({2:F0},{3:F0},{4:F0})", pdp.TileX, pdp.TileZ, pdp.X, pdp.Y, pdp.Z);
                    var flags = Flags.None;
                    if ((node.pathFlags & 0x01) != 0) flags |= Flags.Reverse;
                    if ((node.pathFlags & 0x02) != 0) flags |= Flags.Wait;
                    if ((node.pathFlags & 0x04) != 0) flags |= Flags.Intermediate;
                    if ((node.pathFlags & 0x08) != 0) flags |= Flags.OtherExit;
                    if ((node.pathFlags & 0x10) != 0) flags |= Flags.Optional;
                    var waitTime = (int)((node.pathFlags >> 16) & 0xFFFF);
                    var next = new List<Node>();
                    nodes.Add(new Node(location, flags, waitTime, next));
                    nodeNexts.Add(next);
                }
                for (var i = 0; i < file.TrPathNodes.Count; i++)
                {
                    if (file.TrPathNodes[i].HasNextMainNode)
                        nodeNexts[i].Add(nodes[(int)file.TrPathNodes[i].nextMainNode]);
                    if (file.TrPathNodes[i].HasNextSidingNode)
                        nodeNexts[i].Add(nodes[(int)file.TrPathNodes[i].nextSidingNode]);
                }
                Nodes = nodes;
            }
        }

        // Values must not overlap in binary, so always use powers of 2.
        [Flags]
        public enum Flags
        {
            None = 0,
            Reverse = 1,
            Wait = 2,
            Intermediate = 4,
            OtherExit = 8,
            Optional = 16,
        }

        public class Node
        {
            public readonly string Location;
            public readonly Flags Flags;
            public readonly int WaitTime;
            public readonly IEnumerable<Node> Next;

            internal Node(string location, Flags flags, int waitTime, IEnumerable<Node> next)
            {
                Location = location;
                Flags = flags;
                WaitTime = waitTime;
                Next = next;
            }
        }
    }
}
