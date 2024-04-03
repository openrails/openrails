// COPYRIGHT 2014, 2018 by the Open Rails project.
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
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Orts.Formats.Msts;
using ORTS.Common;
using ORTS.Orge.Drawing;

namespace ORTS.Orge.Editing
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
        private TrackSectionsFile tsectionDat;
        internal ColorScheme ColorSchemeSiding { get; set; }
        internal ColorScheme ColorSchemeMain { get; set; }
        internal ColorScheme ColorSchemeLast { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public DrawPath (TrackDB trackDB, TrackSectionsFile tsectionDat)
        {
            this.trackDB = trackDB;
            this.tsectionDat = tsectionDat;
            this.ColorSchemeMain = DrawColors.colorsPathMain;
            this.ColorSchemeSiding = DrawColors.colorsPathSiding;
            this.ColorSchemeLast = DrawColors.ShadeColor(DrawColors.otherPathsReferenceColor, 0, 1);
        }

        /// <summary>
        /// Draw the actual path coded in the PATfile, completely.
        /// </summary>
        /// <param name="drawArea">Area to draw upon</param>
        /// <param name="firstNode">The first node of the path to draw</param>
        public void Draw(DrawArea drawArea, TrainpathNode firstNode)
        {
            DrawnPathData dummyData = new DrawnPathData();
            Draw(drawArea, firstNode, null, int.MaxValue, dummyData);
        }
 
        /// <summary>
        /// Draw the actual path coded in the PATfile (for a number of nodes that can be extended or reduced)
        /// </summary>
        /// <param name="drawArea">Area to draw upon</param>
        /// <param name="firstNode">The first node of the path to draw</param>
        /// <param name="firstNodeOfTail">The node that is the start of the tail (if available)</param>
        /// <param name="numberToDraw">The requested number of nodes to draw</param>
        /// <param name="drawnPathData">Data structure that we will fill with information about the path we have drawn</param>
        /// <returns>the number of nodes actually drawn (not taking into account nodes on a siding)</returns>
        public int Draw(DrawArea drawArea, 
                         TrainpathNode firstNode,
                         TrainpathNode firstNodeOfTail,
                         int numberToDraw, 
                         DrawnPathData drawnPathData)
        {
            //List of all nodes that need to be drawn.
            List<TrainpathNode> drawnNodes = new List<TrainpathNode>();
 
            // start of path
            TrainpathNode currentSidingNode = null; // we start without siding path
            CurrentMainNode = firstNode;
            if (CurrentMainNode == null)
            {
                // no path, but there might still be a tail
                DrawTail(drawArea, ColorSchemeMain, null, firstNodeOfTail);
                return 0;
            }
         
            drawnNodes.Add(CurrentMainNode);
            drawnPathData.AddNode(CurrentMainNode);

            // We want to draw only a certain number of nodes. And if there is a siding, for the siding
            // we also want to draw the same number of nodes from where it splits from the main track
            TrainpathNode LastVectorStart= null;
            TrainpathNode LastVectorEnd = null;
            int LastVectorTvn = 0;
            int numberDrawn = 1;
            while (numberDrawn < numberToDraw)
            {
                
                // If we have a current siding track, we draw it 
                if (currentSidingNode != null)
                {
                    //finish the complete siding path if the main path is at end of siding already
                    int sidingNodesToDraw = (CurrentMainNode.NodeType == TrainpathNodeType.SidingEnd) ? Int32.MaxValue : 1;
                    while (sidingNodesToDraw >= 1)
                    {
                        //while tracking a siding, it has its own next node
                        TrainpathNode nextNodeOnSiding = currentSidingNode.NextSidingNode;
                        if (nextNodeOnSiding != null) // because also this path can run off at the end
                        {
                            DrawPathOnVectorNode(drawArea, ColorSchemeSiding, currentSidingNode, nextNodeOnSiding, currentSidingNode.NextSidingTvnIndex);
                            drawnNodes.Add(nextNodeOnSiding);
                            drawnPathData.AddNode(nextNodeOnSiding);
                            //siding nodes will not be added to drawnPathData
                            sidingNodesToDraw--;
                        }
                        else
                        {
                            sidingNodesToDraw = 0;
                        }
                        currentSidingNode = nextNodeOnSiding;
                        
                    }
                }
 
                // Draw the start of a siding path, so from this main line point to the next siding node.
                // If there is a next siding node, we also reset the currentSidingNode
                // but probably it is not allowed to have siding on a siding
                TrainpathNode nextSidingNode = CurrentMainNode.NextSidingNode;             
                if (nextSidingNode != null)
                {
                    DrawPathOnVectorNode(drawArea, ColorSchemeSiding, CurrentMainNode, nextSidingNode, CurrentMainNode.NextSidingTvnIndex);
                    drawnNodes.Add(nextSidingNode);
                    drawnPathData.AddNode(nextSidingNode);
                    currentSidingNode = nextSidingNode;
                }

                // From this mainline point to the next
                TrainpathNode nextMainNode = CurrentMainNode.NextMainNode;
                if (nextMainNode != null)
                {
                    DrawPathOnVectorNode(drawArea, ColorSchemeMain, CurrentMainNode, nextMainNode, CurrentMainNode.NextMainTvnIndex);
                    LastVectorStart = CurrentMainNode;
                    LastVectorEnd = nextMainNode;
                    LastVectorTvn = CurrentMainNode.NextMainTvnIndex;
                    drawnNodes.Add(nextMainNode);
                    drawnPathData.AddNode(nextMainNode);
                    drawnPathData.NoteAsDrawn(CurrentMainNode, nextMainNode);

                    CurrentMainNode = nextMainNode;
                    numberDrawn++;
                }
                else
                {
                    // no more nodes, so leave the loop even if we did not draw the amount of points requested
                    break;
                }
            }

            // Highlight the last drawn tracksection
            if (Properties.Settings.Default.highlightLastPathSection && LastVectorStart != null)
            {
                DrawPathOnVectorNode(drawArea, ColorSchemeLast, LastVectorStart, LastVectorEnd, LastVectorTvn);
            }

            //Draw all the nodes themselves
            TrainpathNode lastNode = null;
            foreach (TrainpathNode node in drawnNodes) {
                DrawNodeItself(drawArea, node, false);
                lastNode = node;
            }

            // Highlight the last drawn node
            if (Properties.Settings.Default.highlightLastPathSection && lastNode != null)
            {
                DrawNodeItself(drawArea, lastNode, true);
            }

            DrawTail(drawArea, ColorSchemeMain, drawnNodes.Last(), firstNodeOfTail);

            return numberDrawn;
        }

        

        /// <summary>
        /// Draw the current path node texture, showing what kind of node it is
        /// </summary>
        /// <param name="drawArea">area to Draw upon</param>
        /// <param name="trainpathNode">current node for which we need to draw our texture</param>
        /// <param name="isLastNode">Is this the last node that will be drawn?</param>
        private void DrawNodeItself(DrawArea drawArea, TrainpathNode trainpathNode, bool isLastNode)
        {
            float pathPointSize = 7f; // in meters
            int minPixelSize = 7;
            int maxPixelSize = 24;
            float angle = trainpathNode.TrackAngle;

            Color colorMain = isLastNode ? this.ColorSchemeLast.TrackStraight : ColorSchemeMain.TrackStraight  ;
            Color colorSiding = this.ColorSchemeSiding.TrackStraight;
            Color colorBroken = this.ColorSchemeMain.BrokenNode;

            switch (trainpathNode.NodeType)
            {
                case TrainpathNodeType.Start:
                    // first node; texture is not rotated
                    drawArea.DrawTexture(trainpathNode.Location, "pathStart", pathPointSize, minPixelSize, maxPixelSize, colorMain);
                    break;
                case TrainpathNodeType.End:
                    // formal end node; texture is not rotated
                    drawArea.DrawTexture(trainpathNode.Location, "pathEnd", pathPointSize, minPixelSize, maxPixelSize, colorMain);
                    break;
                case TrainpathNodeType.Reverse:
                    drawArea.DrawTexture(trainpathNode.Location, "pathReverse", pathPointSize, minPixelSize, maxPixelSize, colorMain, angle);
                    break;
                case TrainpathNodeType.Stop:
                    drawArea.DrawTexture(trainpathNode.Location, "pathWait", pathPointSize, minPixelSize, maxPixelSize, colorMain);
                    break;
                case TrainpathNodeType.Temporary:
                    drawArea.DrawTexture(trainpathNode.Location, "crossedRing", pathPointSize, minPixelSize, maxPixelSize, colorBroken);
                    break;
                default:
                    bool isSidingNode = (trainpathNode.NextMainNode == null) &&
                        ( (trainpathNode.NextSidingNode != null) || trainpathNode.IsBroken);  // The IsBroken condition should indicate a dangling siding node
                    Color normalColor = (isSidingNode) ? colorSiding : colorMain;
                    drawArea.DrawTexture(trainpathNode.Location, "pathNormal", pathPointSize, minPixelSize, maxPixelSize, normalColor, angle);
                    break;
            }

            if (trainpathNode.IsBroken)
            {
                drawArea.DrawTexture(trainpathNode.Location, "crossedRing", pathPointSize, minPixelSize, maxPixelSize, colorBroken);
            }
            //drawArea.DrawExpandingString(trainpathNode.Location, trainpathNode.NodeType.ToString()); //debug only
            
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
        private void DrawPathOnVectorNode(DrawArea drawArea, ColorScheme colors, TrainpathNode currentNode, TrainpathNode nextNode, int TvnIndex)
        {
            if (currentNode.IsBrokenOffTrack || nextNode.IsBrokenOffTrack || (TvnIndex == -1))
            {
                DrawPathBrokenNode(drawArea, colors, currentNode, nextNode);
                return;
            }
            TrackNode tn = trackDB.TrackNodes[TvnIndex];

            TrainpathJunctionNode nextJunctionNode = nextNode as TrainpathJunctionNode;
            TrainpathVectorNode nextVectorNode = nextNode as TrainpathVectorNode;

            //Default situation (and most occuring) is to draw the complete vector node 
            int tvsiStart = 0;
            int tvsiStop = tn.TrVectorNode.TrVectorSections.Length-1;
            float sectionOffsetStart = 0;
            float sectionOffsetStop = -1;
            if (currentNode is TrainpathJunctionNode)
            {
                // If both ends are junctions, just draw the full track. Otherwise:
                if (nextVectorNode != null)
                {
                    // Draw from the current junction node to the next mid-point node
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
                if (nextJunctionNode != null)
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
                if (nextVectorNode != null)
                {
                    // Draw from a current vector node to the next vector node, e.g. for multiple wait points 
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
        private void DrawVectorNode(DrawArea drawArea, TrackNode tn, ColorScheme colors, int tvsiStart, int tvsiStop,
                float sectionOffsetStart, float sectionOffsetStop)
        {
            TrVectorSection tvs;
            if (tvsiStart == tvsiStop)
            {
                tvs = tn.TrVectorNode.TrVectorSections[tvsiStart];
                DrawTrackSection(drawArea, tvs, colors, sectionOffsetStart, sectionOffsetStop);
            }
            else
            {
                // first section
                tvs = tn.TrVectorNode.TrVectorSections[tvsiStart];
                DrawTrackSection(drawArea, tvs, colors, sectionOffsetStart, -1);

                // all intermediate sections
                for (int tvsi = tvsiStart + 1; tvsi <= tvsiStop - 1; tvsi++)
                {
                    tvs = tn.TrVectorNode.TrVectorSections[tvsi];
                    DrawTrackSection(drawArea, tvs, colors, 0, -1);
                }

                // last section
                tvs = tn.TrVectorNode.TrVectorSections[tvsiStop];
                DrawTrackSection(drawArea, tvs, colors, 0, sectionOffsetStop);
            }
        }

        /// <summary>
        /// Draw (part of) a tracksection (either curved or straight)
        /// </summary>
        /// <param name="drawArea">Area to draw upon</param>
        /// <param name="tvs">The vectorSection itself that needs to be drawn</param>
        /// <param name="colors">Colorscheme to use</param>
        /// <param name="startOffset">Do not draw the first startOffset meters in the section</param>
        /// <param name="stopOffset">Do not draw past stopOffset meters (draw all if stopOffset less than 0)</param>
        /// <remarks>Note that his is very similar to DrawTrackSection in class DrawTrackDB, but this one allows to draw partial sections</remarks>
        private void DrawTrackSection(DrawArea drawArea, TrVectorSection tvs, ColorScheme colors,
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

                drawArea.DrawArc(trackSection.SectionSize.Width, colors.TrackCurved, thisLocation,
                    radius, tvs.AY, angleLength, angleStart);
            }
            else
            {   // straight section
                float length = (stopOffset < 0) ? trackSection.SectionSize.Length : stopOffset;
                length -= startOffset;
                drawArea.DrawLine(trackSection.SectionSize.Width, colors.TrackStraight, thisLocation,
                    length, tvs.AY, startOffset);
            }
        }

        /// <summary>
        /// Draw the connection between two nodes in case something is broken
        /// </summary>
        /// <param name="drawArea">Area to draw upon</param>
        /// <param name="colors">Colorscheme to use</param>
        /// <param name="currentNode">node to draw from</param>
        /// <param name="nextNode">node to draw to</param>
        private static void DrawPathBrokenNode(DrawArea drawArea, ColorScheme colors, TrainpathNode currentNode, TrainpathNode nextNode)
        {
            drawArea.DrawLine(1f, colors.BrokenPath , currentNode.Location, nextNode.Location);
        }

        /// <summary>
        /// Draw the tail, and the connection to the tail from the last drawn node
        /// </summary>
        /// <param name="drawArea">Area to draw upon</param>
        /// <param name="colors">Colors to use for drawing</param>
        /// <param name="lastDrawnNode">Last drawn node, used as a starting point of connecting dashed line. Can be null</param>
        /// <param name="firstTailNode">Node where the tail starts</param>
        private void DrawTail(DrawArea drawArea, ColorScheme colors, TrainpathNode lastDrawnNode, TrainpathNode firstTailNode)
        {
            if (firstTailNode == null) return;

            if (lastDrawnNode != null)
            {
                drawArea.DrawDashedLine(1f, colors.BrokenPath, lastDrawnNode.Location, firstTailNode.Location);
            }
            DrawNodeItself(drawArea, firstTailNode, false);
            drawArea.DrawTexture(firstTailNode.Location, "ring", 8f, 7, colors.BrokenPath);

        }
    }

    /// <summary>
    /// This is a datastructure where we can store information about the path that has actually been drawn.
    /// </summary>
    public class DrawnPathData
    {
        /// <summary>
        /// List of main-track nodes that were actually drawn and can therefore be selected for editing
        /// </summary>     
        public Collection<TrainpathNode> DrawnNodes { get; private set; }
        
        /// <summary>
        /// Keys are tracknode indexes, value is a list of (main) track node (in pairs) that are both
        /// on the tracknode and the path between them has been drawn 
        /// </summary>
        Dictionary<int, List<TrainpathNode>> DrawnTrackIndexes = new Dictionary<int, List<TrainpathNode>>();

        /// <summary>
        /// Constructor, just creates new empty collections
        /// </summary>
        public DrawnPathData()
        {
            DrawnNodes = new Collection<TrainpathNode>();
            DrawnTrackIndexes = new Dictionary<int, List<TrainpathNode>>();

        }

        /// <summary>
        /// Add a node to the 'list' of stored drawn nodes
        /// </summary>
        /// <param name="node">The node to add</param>
        public void AddNode(TrainpathNode node)
        {
            DrawnNodes.Add(node);
        }

        /// <summary>
        /// Add the fromNode and toNode to the list of drawn nodes indexed for the trackindex
        /// </summary>
        /// <param name="fromNode">The starting node of a drawn path-section</param>
        /// <param name="toNode">The end node of a drawn path-section</param>
        public void NoteAsDrawn(TrainpathNode fromNode, TrainpathNode toNode)
        {
            if (!DrawnTrackIndexes.ContainsKey(fromNode.NextMainTvnIndex))
            {
                DrawnTrackIndexes[fromNode.NextMainTvnIndex] = new List<TrainpathNode>();
            }
            DrawnTrackIndexes[fromNode.NextMainTvnIndex].Add(fromNode);
            DrawnTrackIndexes[fromNode.NextMainTvnIndex].Add(toNode);
        }

        /// <summary>
        /// Return whether a part of the path has been drawn on the track with given tracknodeindex
        /// </summary>
        /// <param name="trackNodeIndex">The index of the trackNode of interest</param>
        /// <returns>true if indeed the path has been drawn over the tracknode</returns>
        public bool TrackHasBeenDrawn(int trackNodeIndex)
        {
            return DrawnTrackIndexes.ContainsKey(trackNodeIndex);
        }

        /// <summary>
        /// Return a collection of trainpathnodes that have been drawn on a tracknode
        /// </summary>
        /// <param name="trackNodeIndex">The index of the tracknode for which to get the nodes</param>
        public Collection<TrainpathNode> NodesOnTrack(int trackNodeIndex)
        {
            return new Collection<TrainpathNode>(DrawnTrackIndexes[trackNodeIndex]);
        }
    }
}
