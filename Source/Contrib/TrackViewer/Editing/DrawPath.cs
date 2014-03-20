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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MSTS.Formats;
using ORTS.Common;
using ORTS.TrackViewer.Drawing;

namespace ORTS.TrackViewer.Editing
{

    /// <summary>
    /// Class to draw a processed path from PATfile. So drawing the path as defined in the linked-list of nodes.
    /// 
    /// Main method is Draw.
    /// All nodes are drawn using a texture.
    /// The tracks linking nodes are drawn as needed from track database (same as drawing the tracks in the first place).
    /// but here it is a bit more complex because also parts of tracks might need to be drawn in case one or both of the
    /// nodes that need to be linked are somewhere on the track and not on a junction/endnode
    /// 
    /// The amount of points that are drawn can be varied, such that it is easier to follow the path (especially in 
    /// complicated cases.
    /// </summary>
    public class DrawPath
    {
        /// <summary>Return the last drawn node</summary>
        public TrainpathNode CurrentMainNode { get; private set; }

        private TrackDB trackDB;
        private TSectionDatFile tsectionDat;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="trackDB"></param>
        /// <param name="tsectionDat"></param>
         public DrawPath (TrackDB trackDB, TSectionDatFile tsectionDat)
        {
            this.trackDB = trackDB;
            this.tsectionDat = tsectionDat;
        }

 
        /// <summary>
        /// Draw the actual path coded in the PATfile (for a number of nodes that can be extended or reduced)
        /// </summary>
        /// <param name="drawArea">Area to draw upon</param>
        /// <param name="firstNode">The first node of the path to draw</param>
        /// <param name="numberToDraw">The requested number of nodes to draw</param>
        /// <param name="drawnMainNodes">List (to be filled) of main-track nodes that were actually drawn and can therefore be selected for editing</param>
        /// <param name="drawnTrackNodeIndexes">List (to be filled) of tracknode indexes with drawn node pairs on the track</param>
        /// <returns>the number of nodes actually drawn (not taking into account nodes on a siding)</returns>
        public int Draw(DrawArea drawArea, 
                         TrainpathNode firstNode,
                         int numberToDraw, 
                         List<TrainpathNode> drawnMainNodes, 
                         Dictionary<int, List<TrainpathNode>> drawnTrackNodeIndexes)
        {
            //List of all nodes that need to be drawn.
            List<TrainpathNode> drawnNodes = new List<TrainpathNode>();
 
            // start of path
            TrainpathNode currentSidingNode = null; // we start without siding path
            CurrentMainNode = firstNode;
            if (CurrentMainNode == null)
            {
                return 0;
            }
         
            drawnNodes.Add(CurrentMainNode);
            drawnMainNodes.Add(CurrentMainNode);    
            
            // We want to draw only a certain number of nodes. And if there is a siding, for the siding
            // we also want to draw the same number of nodes from where it splits from the main track
            int numberDrawn = 1;
            while (numberDrawn < numberToDraw)
            {
                
                // If we have a current siding track, we draw it 
                if (currentSidingNode != null)
                {
                    //finish the complete siding path if the main path is at end of siding already
                    int sidingNodesToDraw = (CurrentMainNode.Type == TrainpathNodeType.SidingEnd) ? Int32.MaxValue : 1;
                    while (sidingNodesToDraw >= 1)
                    {
                        //while tracking a siding, it has its own next node
                        TrainpathNode nextNodeOnSiding = currentSidingNode.NextSidingNode;
                        if (nextNodeOnSiding != null) // because also this path can run off at the end
                        {
                            DrawPathOnVectorNode(drawArea, DrawColors.colorsPathSiding, currentSidingNode, nextNodeOnSiding, currentSidingNode.NextSidingTvnIndex);
                            drawnNodes.Add(nextNodeOnSiding);
                            sidingNodesToDraw--;
                        }
                        else
                        {
                            sidingNodesToDraw = 0;
                        }
                        currentSidingNode = nextNodeOnSiding;
                        
                    }
                }

                WorldLocation curMainLoc = CurrentMainNode.Location;
                
                // Draw the start of a siding path, so from this main line point to the next siding node.
                // If there is a next siding node, we also reset the currentSidingNode
                // but probably it is not allowed to have siding on a siding
                TrainpathNode nextSidingNode = CurrentMainNode.NextSidingNode;             
                if (nextSidingNode != null)
                {
                    DrawPathOnVectorNode(drawArea, DrawColors.colorsPathSiding, CurrentMainNode, nextSidingNode, CurrentMainNode.NextSidingTvnIndex);
                    drawnNodes.Add(nextSidingNode);
                    currentSidingNode = nextSidingNode;
                }

                // From this mainline point to the next
                TrainpathNode nextMainNode = CurrentMainNode.NextMainNode;
                if (nextMainNode != null)
                {
                    DrawPathOnVectorNode(drawArea, DrawColors.colorsPathMain, CurrentMainNode, nextMainNode, CurrentMainNode.NextMainTvnIndex);
                    drawnNodes.Add(nextMainNode);
                    drawnMainNodes.Add(nextMainNode);
                    NoteAsDrawn(drawnTrackNodeIndexes, CurrentMainNode, nextMainNode);

                    CurrentMainNode = nextMainNode;
                    numberDrawn++;
                }
                else
                {
                    // no more nodes, so leave the loop even if we did not draw the amount of points requested
                    break;
                }
            }

            //Draw all the nodes themselves
            foreach (TrainpathNode node in drawnNodes) {
                DrawNodeItself(drawArea, node);
            }

            return numberDrawn;
        }

        /// <summary>
        /// Add the from and to Node to the list of drawn nodes for the trackindex
        /// </summary>
        private static void NoteAsDrawn(Dictionary<int, List<TrainpathNode>> drawnTrackNodeIndexes, TrainpathNode fromNode, TrainpathNode toNode)
        {
            if (!drawnTrackNodeIndexes.ContainsKey(fromNode.NextMainTvnIndex))
            {
                drawnTrackNodeIndexes[fromNode.NextMainTvnIndex] = new List<TrainpathNode>();
            }
            drawnTrackNodeIndexes[fromNode.NextMainTvnIndex].Add(fromNode);
            drawnTrackNodeIndexes[fromNode.NextMainTvnIndex].Add(toNode);
        }

        /// <summary>
        /// Draw the current path node texture, showing what kind of node it is
        /// </summary>
        /// <param name="drawArea">area to Draw upon</param>
        /// <param name="trainpathNode">current node for which we need to draw our texture</param>
        void DrawNodeItself(DrawArea drawArea, TrainpathNode trainpathNode)
        {
            float pathPointSize = 7f; // in meters
            int minPixelSize = 7;
            float angle = trainpathNode.TrackAngle;

            switch (trainpathNode.Type)
            {
                case TrainpathNodeType.Start:
                    // first node; texture is not rotated
                    drawArea.DrawTexture(trainpathNode.Location, "pathStart", 0, pathPointSize, minPixelSize);
                    break;
                case TrainpathNodeType.End:
                    // formal end node; texture is not rotated
                    drawArea.DrawTexture(trainpathNode.Location, "pathEnd", 0, pathPointSize, minPixelSize);
                    break;
                case TrainpathNodeType.Reverse:
                    drawArea.DrawTexture(trainpathNode.Location, "pathReverse", angle, pathPointSize, minPixelSize);
                    break;
                case TrainpathNodeType.Stop:
                    drawArea.DrawTexture(trainpathNode.Location, "pathWait", 0, pathPointSize, minPixelSize);
                    break;
                case TrainpathNodeType.Uncouple:
                    drawArea.DrawTexture(trainpathNode.Location, "pathUncouple", 0, pathPointSize, minPixelSize);
                    break;
                default:
                    if ((trainpathNode.NextMainNode == null) && (trainpathNode.NextSidingNode != null))
                    {   // siding node;
                        drawArea.DrawTexture(trainpathNode.Location, "pathSiding", angle, pathPointSize, minPixelSize);
                    }
                    else
                    {   // normal node
                        drawArea.DrawTexture(trainpathNode.Location, "pathNormal", angle, pathPointSize, minPixelSize);
                    }
                    break;
            }

            if (trainpathNode.IsBroken)
            {
                drawArea.DrawSimpleTexture(trainpathNode.Location, "crossedRing", pathPointSize, minPixelSize, DrawColors.colorsNormal["brokenNode"]);
            }       
            
        }

        /// <summary>
        /// Draw a path on a vector node, meaning that the vector node will be drawn (possibly partly), in path colors
        /// </summary>
        /// <param name="drawArea">Area to draw upon</param>
        /// <param name="colors">Colorscheme to use</param>
        /// <param name="currentNode">Current path node</param>
        /// <param name="nextNode">Next path Node</param>
        /// <param name="TvnIndex">The index of the track vector node that is between the two path nodes</param>
        /// <remarks>Note that it is not clear yet whether the direction of current to next is the same as the
        /// direction of the vector node</remarks>
        void DrawPathOnVectorNode(DrawArea drawArea, ColorScheme colors, TrainpathNode currentNode, TrainpathNode nextNode, int TvnIndex)
        {
            if (currentNode.IsBroken || nextNode.IsBroken)
            {
                DrawPathBrokenNode(drawArea, colors, currentNode, nextNode);
                return;
            }
            TrackNode tn = trackDB.TrackNodes[TvnIndex];

            //Default situation (and most occuring) is to draw the complete vector node 
            int tvsiStart = 0;
            int tvsiStop = tn.TrVectorNode.TrVectorSections.Length-1;
            float sectionOffsetStart = 0;
            float sectionOffsetStop = -1;
            if (currentNode is TrainpathJunctionNode)
            {
                if (nextNode is TrainpathJunctionNode)
                {
                    // If both ends are junctions, just draw the full track.
                }
                else
                {
                    // Draw from the current junction node to the next mid-point node
                    TrainpathVectorNode nextVectorNode = nextNode as TrainpathVectorNode;
                    if (nextVectorNode.IsEarlierOnTrackThan(currentNode))
                    {   // trackvectornode is oriented the other way as path
                        tvsiStart = nextVectorNode.TrackVectorSectionIndex;
                        sectionOffsetStart = nextVectorNode.TrackSectionOffset;
                    }
                    else
                    {
                        // trackvectornode is oriented in the same way as path
                        tvsiStop = nextVectorNode.TrackVectorSectionIndex;
                        sectionOffsetStop = nextVectorNode.TrackSectionOffset;
                    }
                }
            }
            else
            {
                TrainpathVectorNode currentVectorNode = currentNode as TrainpathVectorNode;
                if (nextNode is TrainpathJunctionNode)
                {
                    // Draw from current mid-point node to next junction node
                    if (currentVectorNode.IsEarlierOnTrackThan(nextNode))
                    {   // trackvectornode is oriented in the same way as path
                        tvsiStart = currentVectorNode.TrackVectorSectionIndex;
                        sectionOffsetStart = currentVectorNode.TrackSectionOffset;
                    }
                    else
                    {   // trackvectornode is oriented the other way around.
                        tvsiStop = currentVectorNode.TrackVectorSectionIndex;
                        sectionOffsetStop = currentVectorNode.TrackSectionOffset;
                    }
                }
                else
                {
                    // Draw from a current mid-point node to next mid-point node. Not sure if this ever happens
                    TrainpathVectorNode nextVectorNode = nextNode as TrainpathVectorNode;
                    if (currentVectorNode.IsEarlierOnTrackThan(nextVectorNode))
                    {   // from current to next is in the direction of the vector node
                        tvsiStart = currentVectorNode.TrackVectorSectionIndex;
                        tvsiStop = nextVectorNode.TrackVectorSectionIndex;
                        sectionOffsetStart = currentVectorNode.TrackSectionOffset;
                        sectionOffsetStop = nextVectorNode.TrackSectionOffset;
                    }
                    else
                    {   // from next to current is in the direction of the vector node
                        tvsiStart = nextVectorNode.TrackVectorSectionIndex;
                        tvsiStop = currentVectorNode.TrackVectorSectionIndex;
                        sectionOffsetStart = nextVectorNode.TrackSectionOffset;
                        sectionOffsetStop = currentVectorNode.TrackSectionOffset;
                    }
                }
            }
            DrawVectorNode(drawArea, tn, colors, tvsiStart, tvsiStop, sectionOffsetStart, sectionOffsetStop);
        }

        /// <summary>
        /// Draw (possibly part of) the track of a MSTS vectorNode (from track database)
        /// </summary>
        /// <param name="drawArea">Area to draw upon</param>
        /// <param name="tn">The tracknode from track database (assumed to be a vector node)</param>
        /// <param name="colors">Colorscheme to use</param>
        /// <param name="tvsiStart">Index of first track vector section to draw (at least partially)</param>
        /// <param name="tvsiStop">Index of last track vector section to draw (at least partially)</param>
        /// <param name="sectionOffsetStart">start-offset in the first track section to draw</param>
        /// <param name="sectionOffsetStop">stop-offset in the last track section to draw</param>
        /// <remarks>Very similar to DrawVectorNode in class DrawTrackDB, but this one allows to draw partial vector nodes.</remarks>
        void DrawVectorNode(DrawArea drawArea, TrackNode tn, ColorScheme colors, int tvsiStart, int tvsiStop,
                float sectionOffsetStart, float sectionOffsetStop)
        {
            TrVectorSection tvs;
            if (tvsiStart == tvsiStop)
            {
                tvs = tn.TrVectorNode.TrVectorSections[tvsiStart];
                DrawTrackSection(drawArea, tn, tvs, colors, sectionOffsetStart, sectionOffsetStop);
            }
            else
            {
                // first section
                tvs = tn.TrVectorNode.TrVectorSections[tvsiStart];
                DrawTrackSection(drawArea, tn, tvs, colors, sectionOffsetStart, -1);

                // all intermediate sections
                for (int tvsi = tvsiStart + 1; tvsi <= tvsiStop - 1; tvsi++)
                {
                    tvs = tn.TrVectorNode.TrVectorSections[tvsi];
                    DrawTrackSection(drawArea, tn, tvs, colors, 0, -1);
                }

                // last section
                tvs = tn.TrVectorNode.TrVectorSections[tvsiStop];
                DrawTrackSection(drawArea, tn, tvs, colors, 0, sectionOffsetStop);
            }
        }

        /// <summary>
        /// Draw (part of) a tracksection (either curved or straight)
        /// </summary>
        /// <param name="drawArea">Area to draw upon</param>
        /// <param name="tn">The tracknode from track database (assumed to be a vector node)</param>
        /// <param name="tvs">The vectorSection itself that needs to be drawn</param>
        /// <param name="colors">Colorscheme to use</param>
        /// <param name="startOffset">Do not draw the first startOffset meters in the section</param>
        /// <param name="stopOffset">Do not draw past stopOffset meters (draw all if stopOffset less than 0)</param>
        /// <remarks>Note that his is very similar to DrawTrackSection in class DrawTrackDB, but this one allows to draw partial sections</remarks>
        private void DrawTrackSection(DrawArea drawArea, TrackNode tn, TrVectorSection tvs, ColorScheme colors,
            float startOffset, float stopOffset)
        {
            TrackSection trackSection = tsectionDat.TrackSections.Get(tvs.SectionIndex);
            if (trackSection == null) return;

            WorldLocation thisLocation = new WorldLocation(tvs.TileX, tvs.TileZ, tvs.X, 0, tvs.Z);
            
            if (trackSection.SectionCurve != null)
            {   //curved section
                float radius = trackSection.SectionCurve.Radius;
                int sign = (trackSection.SectionCurve.Angle < 0) ? -1 : 1;
                float angleLength = (stopOffset < 0) ? trackSection.SectionCurve.Angle : sign*MathHelper.ToDegrees(stopOffset/radius);
                float angleStart = sign*MathHelper.ToDegrees(startOffset / radius);
                angleLength -= angleStart;

                drawArea.DrawArc(trackSection.SectionSize.Width, colors["trackCurved"], thisLocation,
                    radius, tvs.AY, angleLength, angleStart);
            }
            else
            {   // straight section
                float length = (stopOffset < 0) ? trackSection.SectionSize.Length : stopOffset;
                length -= startOffset;
                drawArea.DrawLine(trackSection.SectionSize.Width, colors["trackStraight"], thisLocation,
                    length, tvs.AY, startOffset);
            }
        }

        void DrawPathBrokenNode(DrawArea drawArea, ColorScheme colors, TrainpathNode currentNode, TrainpathNode nextNode)
        {
            drawArea.DrawLine(1f, colors["pathBroken"] , currentNode.Location, nextNode.Location);
        }
    }
}
