// COPYRIGHT 2023 by the Open Rails project.
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
using System.Diagnostics;
using System.Drawing;
using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using Orts.MultiPlayer;
using Orts.Simulation.Physics;
using Orts.Simulation.Signalling;
using Orts.Viewer3D.Debugging;

namespace Orts.Viewer3D.Map
{
    public class MapDataProvider
    {
        public MapViewer F { get; set; } // Shortest possible abbreviation so code is easier to read

        public MapDataProvider(MapViewer form)
        {
            F = form;
        }

        public void SetControls()
        {
            UpdateControlVisibility();
        }

        private void UpdateControlVisibility()
        {
            var isMultiPlayer = MPManager.IsMultiPlayer();

            if (isMultiPlayer)
            {
                F.playersPanel.Visible = true;
            }

            if (isMultiPlayer && MPManager.IsServer())
            {
                F.playerRolePanel.Visible = true;
                F.messagesPanel.Visible = true;
                F.multiplayerSettingsPanel.Visible = true;
                //F.playersPanel.Visible = true;
            }
        }

        public void PopulateItemLists()
        {
            foreach (var item in F.simulator.TDB.TrackDB.TrItemTable)
            {
                switch (item.ItemType)
                {
                    case TrItem.trItemType.trSIGNAL:
                        if (item is SignalItem si)
                        {
                            if (si.SigObj >= 0 && si.SigObj < F.simulator.Signals.SignalObjects.Length)
                            {
                                var s = F.simulator.Signals.SignalObjects[si.SigObj];
                                if (s != null && s.Type == SignalObjectType.Signal && s.isSignalNormal())
                                    F.signals.Add(new SignalWidget(si, s));
                            }
                        }
                        break;

                    case TrItem.trItemType.trSIDING:
                        // Sidings have 2 ends but are not always listed in pairs in the *.tdb file
                        // Neither are their names unique (e.g. Bernina Bahn).
                        // Find whether this siding is a new one or the other end of an old one.
                        // If other end, then find the right-hand one as the location for a single label.
                        // Note: Find() within a foreach() loop is O(n^2) but is only done at start.
                        var oldSidingIndex = F.sidings.FindIndex(r => r.LinkId == item.TrItemId && r.Name == item.ItemName);
                        if (oldSidingIndex < 0)
                        {
                            var newSiding = new SidingWidget(item as SidingItem);
                            F.sidings.Add(newSiding);
                        }
                        else
                        {
                            var oldSiding = F.sidings[oldSidingIndex];
                            var oldLocation = oldSiding.Location;
                            var newLocation = new PointF((item.TileX * 2048) + item.X, (item.TileZ * 2048) + item.Z);

                            // Because these are structs, not classes, compiler won't let you overwrite them.
                            // Instead create a single item which replaces the 2 platform items.
                            var replacement = new SidingWidget(item as SidingItem)
                            {
                                Location = GetMidPoint(oldLocation, newLocation)
                            };

                            // Replace the old siding item with the replacement
                            F.sidings.RemoveAt(oldSidingIndex);
                            F.sidings.Add(replacement);
                        }
                        break;

                    case TrItem.trItemType.trPLATFORM:
                        // Platforms have 2 ends but are not always listed in pairs in the *.tdb file
                        // Neither are their names unique (e.g. Bernina Bahn).
                        // Find whether this platform is a new one or the other end of an old one.
                        // If other end, then find the right-hand one as the location for a single label.
                        var oldPlatformIndex = F.platforms.FindIndex(r => r.LinkId == item.TrItemId && r.Name == item.ItemName);
                        if (oldPlatformIndex < 0)
                        {
                            var newPlatform = new PlatformWidget(item as PlatformItem)
                            {
                                Extent1 = new PointF((item.TileX * 2048) + item.X, (item.TileZ * 2048) + item.Z)
                            };
                            F.platforms.Add(newPlatform);
                        }
                        else
                        {
                            var oldPlatform = F.platforms[oldPlatformIndex];
                            var oldLocation = oldPlatform.Location;
                            var newLocation = new PointF((item.TileX * 2048) + item.X, (item.TileZ * 2048) + item.Z);

                            // Because these are structs, not classes, compiler won't let you overwrite them.
                            // Instead create a single item which replaces the 2 platform items.
                            var replacement = new PlatformWidget(item as PlatformItem)
                            {
                                Extent1 = oldLocation
                                ,
                                Extent2 = newLocation
                                // Give it the right-hand location
                                ,
                                Location = GetRightHandPoint(oldLocation, newLocation)
                            };

                            // Replace the old platform item with the replacement
                            F.platforms.RemoveAt(oldPlatformIndex);
                            F.platforms.Add(replacement);
                        }
                        break;

                    default:
                        break;
                }
            }

            foreach (var p in F.platforms)
                if (p.Extent1.IsEmpty || p.Extent2.IsEmpty)
                    Trace.TraceWarning("Platform '{0}' is incomplete as the two ends do not match. It will not show in full in the Timetable Tab of the Map Window", p.Name);
        }

        /// <summary>
        /// Returns the mid-point between two locations
        /// </summary>
        /// <param name="location"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private PointF GetMidPoint(PointF location1, PointF location2)
        {
            return new PointF()
            {
                X = (location1.X + location2.X) / 2
                ,
                Y = (location1.Y + location2.Y) / 2
            };
        }

        private PointF GetRightHandPoint(PointF location1, PointF location2)
        {
            return location1.X > location2.X ? location1 : location2;
        }

        public void ShowSimulationTime()
        {
            var ct = TimeSpan.FromSeconds(Program.Simulator.ClockTime);
            F.timeLabel.Text = $"{ct:hh}:{ct:mm}:{ct:ss}";
        }

        /// <summary>
        /// In case of missing X,Y values, just draw a blob at the non-zero end.
        /// </summary>
        public void FixForBadData(float width, ref PointF scaledA, ref PointF scaledB, PointF Extent1, PointF Extent2)
        {
            if (Extent1.X == 0 || Extent1.Y == 0)
            {
                scaledA.X = scaledB.X + width;
                scaledA.Y = scaledB.Y + width;
            }
            else if (Extent2.X == 0 || Extent2.Y == 0)
            {
                scaledB.X = scaledA.X + width;
                scaledB.Y = scaledA.Y + width;
            }
        }

        public bool IsActiveTrain(Simulation.AIs.AITrain t)
        {
            return t != null
&& (t.MovementState != Simulation.AIs.AITrain.AI_MOVEMENT_STATE.AI_STATIC
                        && !(t.TrainType == Train.TRAINTYPE.AI_INCORPORATED && !t.IncorporatingTrain.IsPathless)

                    || t.TrainType == Train.TRAINTYPE.PLAYER);
        }

        /*
		 * # section is claimed by a train which is waiting for a signal.
		 * & section is occupied by more than one train.
		 * deadlock info (always linked to a switch node):
		 * · * possible deadlock location - start of a single track section shared with a train running in opposite direction.
		 * · ^ active deadlock - train from opposite direction is occupying or has reserved at least part of the common
		 *     single track section. Train will be stopped at this location – generally at the last signal ahead of this node.
		 * · ~ active deadlock at that location for other train - can be significant as this other train can block this
		 *     train’s path.
		*/
        private static readonly char[] DeadlockIndicators = "#&*^~".ToCharArray();

        public static bool ContainsDeadlockIndicators(string text)
        {
            return text.IndexOfAny(DeadlockIndicators) >= 0;
        }

        // The canvas is split into equally pitched rows. 
        // Each row has an array of 4 slots with StartX, EndX positions and a count of how many slots have been filled.
        // Arrays are used instead of lists to avoid delays for memory management.
        public void CleanTextCells()
        {
            if (F.alignedTextY == null || F.alignedTextY.Length != F.mapCanvas.Height / DispatchViewer.spacing) //first time to put text, or the text height has changed
            {
                F.alignedTextY = new Vector2[F.mapCanvas.Height / DispatchViewer.spacing][];
                F.alignedTextNum = new int[F.mapCanvas.Height / DispatchViewer.spacing];
                for (var i = 0; i < F.mapCanvas.Height / DispatchViewer.spacing; i++)
                    F.alignedTextY[i] = new Vector2[5]; //each line has at most 5 slots
            }
            for (var i = 0; i < F.mapCanvas.Height / DispatchViewer.spacing; i++)
                F.alignedTextNum[i] = 0;
        }

        // Returns a vertical position for the text that doesn't clash or returns -1
        // If the preferred space for text is occupied, then the slot above (-ve Y) is tested, then 2 sltos above, then 1 below.
        public float GetUnusedYLocation(float startX, float wantY, string name)
        {
            const float noFreeSlotFound = -1f;

            var desiredPositionY = (int)(wantY / DispatchViewer.spacing);  // The positionY of the ideal row for the text.
            var endX = startX + (name.Length * F.trainFont.Size);
            //out of drawing area
            if (endX < 0)
                return noFreeSlotFound;

            var positionY = desiredPositionY;
            while (positionY >= 0 && positionY < F.alignedTextY.Length)
            {
                //if the line contains no text yet, put it there
                if (F.alignedTextNum[positionY] == 0)
                    return SaveLabelLocation(startX, endX, positionY);

                var conflict = false;

                //check if it intersects with any labels already in this row
                for (var col = 0; col < F.alignedTextNum[positionY]; col++)
                {
                    var v = F.alignedTextY[positionY][col];
                    //check conflict with a text, v.X is the start of the text, v.Y is the end of the text
                    if (endX >= v.X && startX <= v.Y)
                    {
                        conflict = true;
                        break;
                    }
                }

                if (conflict)
                {
                    positionY--; // Try a different row: -1, -2, +2, +1

                    if (positionY - desiredPositionY <= -2) // Cannot move up (-ve Y), so try to move it down (+ve Y)
                        positionY = desiredPositionY + 2;   // Try +2 then +1

                    if (positionY == desiredPositionY) // Back to original position again
                        return noFreeSlotFound;
                }
                else
                {
                    // Check that row has an unused column in its fixed size array
                    return F.alignedTextNum[positionY] >= F.alignedTextY[positionY].Length ? noFreeSlotFound : SaveLabelLocation(startX, endX, positionY);
                }
            }
            return noFreeSlotFound;
        }

        private float SaveLabelLocation(float startX, float endX, int positionY)
        {
            // add start and end location for the new label
            F.alignedTextY[positionY][F.alignedTextNum[positionY]] = new Vector2 { X = startX, Y = endX };

            F.alignedTextNum[positionY]++;

            return positionY * DispatchViewer.spacing;
        }
    }
}
