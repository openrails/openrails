// COPYRIGHT 2014, 2015 by the Open Rails project.
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

using System.Linq;
using Orts.Formats.Msts;

namespace Orts.Formats.OR
{
    public static class TrackNodeAE
    {
        /// <summary>
        /// Permet de récupérer les coordonnées du node au format MSTSCoord propre à l'éditeur d'activité
        /// Return the MSTSCoord for the current node as needed by Activity Editor
        ///     For vectorNode with multiple section, the direction is used to give the first encountered section
        /// </summary>
        public static MSTSCoord getMSTSCoord(this TrackNode node, int direction)
        {
            MSTSCoord coord = new MSTSCoord();
            if (node.TrEndNode || node.TrJunctionNode != null || node.TrVectorNode.TrVectorSections == null)
            {
                coord.TileX = node.UiD.TileX;
                coord.TileY = node.UiD.TileZ;
                coord.X = node.UiD.X;
                coord.Y = node.UiD.Z;
            }
            else if (node.TrVectorNode != null && node.TrVectorNode.TrVectorSections.Length > 1)
            {
                if (direction == 0)
                {
                    TrVectorSection section = node.TrVectorNode.TrVectorSections.Last();
                    coord.TileX = section.TileX;
                    coord.TileY = section.TileZ;
                    coord.X = section.X;
                    coord.Y = section.Z;

                }
                else
                {
                    TrVectorSection section = node.TrVectorNode.TrVectorSections.First();
                    coord.TileX = section.TileX;
                    coord.TileY = section.TileZ;
                    coord.X = section.X;
                    coord.Y = section.Z;

                }
            }
            else if (node.TrVectorNode != null)
            {
                coord.TileX = node.TrVectorNode.TrVectorSections[0].TileX;
                coord.TileY = node.TrVectorNode.TrVectorSections[0].TileZ;
                coord.X = node.TrVectorNode.TrVectorSections[0].X;
                coord.Y = node.TrVectorNode.TrVectorSections[0].Z;

            }
            return coord;
        }

        public static uint getShapeIdx(this TrackNode node, int direction)
        {
            uint shapeIdx = 0;
            if (node.TrEndNode || node.TrJunctionNode != null)
            {
                shapeIdx = 0;
            }
            else if (node.TrVectorNode.TrVectorSections.Length > 1)
            {
                if (direction == 0)
                {
                    TrVectorSection section = node.TrVectorNode.TrVectorSections.Last();
                    shapeIdx = section.ShapeIndex;
                }
                else
                {
                    TrVectorSection section = node.TrVectorNode.TrVectorSections.First();
                    shapeIdx = section.ShapeIndex;
                }

            }
            else
            {
                shapeIdx = 0;

            }
            return shapeIdx;

        }

        public static int getIndex(this TrackNode node)
        {
            return (int)node.Index;
        }

        public static uint getSectionIndex(this TrackNode node, int direction)
        {
            uint index = 0;
            if (node.TrEndNode)
            {
                index = (uint)node.UiD.WorldId;
            }
            else if (node.TrJunctionNode != null)
            {
                index = (uint)node.TrJunctionNode.Idx;
            }
            else if (node.TrVectorNode.TrVectorSections != null && node.TrVectorNode.TrVectorSections.Length > 1)
            {
                if (direction == 0)
                {
                    TrVectorSection section = node.TrVectorNode.TrVectorSections.Last();
                    index = section.SectionIndex;
                }
                else
                {
                    TrVectorSection section = node.TrVectorNode.TrVectorSections.First();
                    index = section.SectionIndex;
                }
            }
            else if (node.TrVectorNode.TrVectorSections != null)
            {
                index = node.TrVectorNode.TrVectorSections[0].SectionIndex;

            }
            else
            {
                index = (uint)node.UiD.WorldId;
            }
            return index;
        }

        public static TrVectorSection getVectorSection(this TrackNode node)
        {
            TrVectorSection section = null;
            if (node.TrEndNode)
            {
                //section = TrVectorNode.TrVectorSections[0];
            }
            else if (node.TrJunctionNode != null)
            {
            }
            else if (node.TrVectorNode.TrVectorSections == null)
            {
            }
            else if (node.TrVectorNode.TrVectorSections.Length > 1)
            {
            }
            else
            {
                section = node.TrVectorNode.TrVectorSections[0];

            }
            return section;
        }

        public static uint getWorldFileUiD(this TrackNode node)
        {
            if (node.getVectorSection() != null)
                return node.getVectorSection().WorldFileUiD;
            return 0;
        }

        public static void reduce(this TrackNode node, double tileX, double tileY)
        {
            if (node.Reduced)
                return;
            node.Reduced = true;
            if (node.TrEndNode || node.TrJunctionNode != null)
            {
                //File.AppendAllText(@"F:\temp\AE.txt", "Original TileX: " + this.UiD.TileX + ".." + this.UiD.X + "/ TileY: " + this.UiD.TileZ + ".." + this.UiD.Z + "\n");
                int intermX = node.UiD.TileX;
                int intermY = node.UiD.TileZ;
                node.UiD.TileX = intermX - (int)tileX;
                node.UiD.TileZ = intermY - (int)tileY;
                node.UiD.WorldTileX = node.UiD.TileX;
                node.UiD.WorldTileZ = node.UiD.TileZ;
                //File.AppendAllText(@"F:\temp\AE.txt", "reduced end or junction node TileX: " + this.UiD.TileX + ".." + this.UiD.X + "/ TileY: " + this.UiD.TileZ + ".." + this.UiD.Z + "\n");


            }
            else if (node.TrVectorNode.TrVectorSections.Length > 1)
            {
                foreach (var section in node.TrVectorNode.TrVectorSections)
                {
                    if (section.Reduced)
                        continue;
                    //File.AppendAllText(@"F:\temp\AE.txt", "Original TileX: " + section.TileX + ".." + section.X + "/ TileY: " + section.TileZ + ".." + section.Z + "\n");
                    int intermX = section.TileX;
                    int intermY = section.TileZ;
                    section.TileX = intermX - (int)tileX;
                    section.TileZ = intermY - (int)tileY;
                    section.WFNameX = section.TileX;
                    section.WFNameZ = section.TileZ;
                    section.Reduced = true;
                    //File.AppendAllText(@"F:\temp\AE.txt", "reduced section TileX: " + section.TileX + ".." + section.X + "/ TileY: " + section.TileZ + ".." + section.Z + "\n");
                }
            }
            else
            {
                if (!node.TrVectorNode.TrVectorSections[0].Reduced)
                {
                    //File.AppendAllText(@"F:\temp\AE.txt", "Original TileX: " + TrVectorNode.TrVectorSections[0].TileX +
                    //    ".." + TrVectorNode.TrVectorSections[0].X + "/ TileY: " +
                    //    TrVectorNode.TrVectorSections[0].TileZ + ".." +
                    //    TrVectorNode.TrVectorSections[0].Z + "\n");
                    int intermX = node.TrVectorNode.TrVectorSections[0].TileX;
                    int intermY = node.TrVectorNode.TrVectorSections[0].TileZ;
                    node.TrVectorNode.TrVectorSections[0].TileX = intermX - (int)tileX;
                    node.TrVectorNode.TrVectorSections[0].TileZ = intermY - (int)tileY;
                    node.TrVectorNode.TrVectorSections[0].WFNameX = node.TrVectorNode.TrVectorSections[0].TileX;
                    node.TrVectorNode.TrVectorSections[0].WFNameZ = node.TrVectorNode.TrVectorSections[0].TileZ;
                    node.TrVectorNode.TrVectorSections[0].Reduced = true;
                    //File.AppendAllText(@"F:\temp\AE.txt", "Reduced simple section TileX: " + TrVectorNode.TrVectorSections[0].TileX +
                    //    ".." + TrVectorNode.TrVectorSections[0].X + "/ TileY: " +
                    //    TrVectorNode.TrVectorSections[0].TileZ + ".." +
                    //    TrVectorNode.TrVectorSections[0].Z + "\n");
                }
                else
                {
                    ;
                }
            }

        }

        public static int searchIdx(this TrackNode node, TrVectorSection currentSection)
        {
            TrVectorNode nodes = node.TrVectorNode;
            if (nodes == null || nodes.TrVectorSections == null)
                return 0;
            for (int cnt = 0; cnt < nodes.TrVectorSections.Count(); cnt++)
            {
                if (nodes.TrVectorSections[cnt].SectionIndex == currentSection.SectionIndex &&
                    nodes.TrVectorSections[cnt].WorldFileUiD == currentSection.WorldFileUiD)
                    return cnt;
            }
            return 0;
        }
    }

}
