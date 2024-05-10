// COPYRIGHT 2014, 2015, 2017 by the Open Rails project.
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

// This file is the responsibility of the 3D & Environment Team. 

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Simulation;
using ORTS.Common;
using ORTS.Settings;
using System.Collections.Generic;

namespace Orts.Viewer3D.Popups
{
    public class OSDLocations : LayeredWindow
    {
        Matrix Identity = Matrix.Identity;

        internal const float MaximumDistancePlatform = 1000;
        internal const float MaximumDistanceSiding = 500;
        internal const float MinimumDistance = 100;

        public enum DisplayState
        {
            Platforms = 0x1,
            Sidings = 0x2,
            All = 0x3,
            Auto = 0x7,
        }
        private readonly SavingProperty<int> StateProperty;
        private DisplayState State
        {
            get => (DisplayState)StateProperty.Value;
            set
            {
                StateProperty.Value = (int)value;
            }
        }

        Dictionary<TrItemLabel, LabelPrimitive> Labels = new Dictionary<TrItemLabel, LabelPrimitive>();

        int StationStopsCount;
        ActivityTask ActivityCurrentTask;
        Dictionary<string, bool> Platforms;
        Dictionary<string, bool> Sidings;

        int PlatformUpdate = 0;

        public OSDLocations(WindowManager owner)
            : base(owner, 0, 0, "OSD Locations")
        {
            StateProperty = owner.Viewer.Settings.GetSavingProperty<int>("OSDLocationsState");
            UpdateLabelLists();
        }

        void UpdateLabelLists()
        {
            var tdb = Owner.Viewer.Simulator.TDB.TrackDB;
            var stationStops = Owner.Viewer.Simulator.PlayerLocomotive.Train.StationStops;
            var activity = Owner.Viewer.Simulator.ActivityRun;

            // Update every 10s or when the current activity task changes.
            if (--PlatformUpdate <= 0 || stationStops.Count != StationStopsCount || (activity != null && activity.Current != ActivityCurrentTask))
            {
                PlatformUpdate = 40;

                var platforms = new Dictionary<string, bool>();
                var sidings = new Dictionary<string, bool>();

                if (tdb.TrItemTable != null)
                {
                    foreach (var stop in stationStops)
                    {
                        var platformId = stop.PlatformReference;
                        if (0 <= platformId && platformId < tdb.TrItemTable.Length && tdb.TrItemTable[platformId].ItemType == Formats.Msts.TrItem.trItemType.trPLATFORM)
                        {
                            platforms[tdb.TrItemTable[platformId].ItemName] = true;
                        }
                    }

                    if (activity != null && activity.EventList != null)
                    {
                        foreach (var @event in activity.EventList)
                        {
                            var eventAction = @event.ParsedObject as Orts.Formats.Msts.EventCategoryAction;
                            if (eventAction != null)
                            {
                                var sidingId1 = eventAction.SidingId;
                                var sidingId2 = eventAction.WagonList != null && eventAction.WagonList.WorkOrderWagonList.Count > 0 ? eventAction.WagonList.WorkOrderWagonList[0].SidingId : default(uint?);
                                var sidingId = sidingId1.HasValue ? sidingId1.Value : sidingId2.HasValue ? sidingId2.Value : uint.MaxValue;
                                if (0 <= sidingId && sidingId < tdb.TrItemTable.Length && tdb.TrItemTable[sidingId].ItemType == Formats.Msts.TrItem.trItemType.trSIDING)
                                {
                                    sidings[tdb.TrItemTable[sidingId].ItemName] = true;
                                }
                            }
                        }
                    }
                }

                Platforms = platforms;
                Sidings = sidings;

                StationStopsCount = stationStops.Count;
                if (activity != null)
                    ActivityCurrentTask = activity.Current;
            }
        }

        public override bool Interactive
        {
            get
            {
                return false;
            }
        }

        public override void TabAction()
        {
            // All -> Platforms -> Sidings -> [if activity/timetable active] Auto -> All -> ...
            if (State == DisplayState.All) State = DisplayState.Platforms;
            else if (State == DisplayState.Platforms) State = DisplayState.Sidings;
            else if (State == DisplayState.Sidings) State = Platforms.Count + Sidings.Count > 0 ? DisplayState.Auto : DisplayState.All;
            else if (State == DisplayState.Auto) State = DisplayState.All;
        }

        public override void PrepareFrame(RenderFrame frame, ORTS.Common.ElapsedTime elapsedTime, bool updateFull)
        {
            if (updateFull)
            {
                UpdateLabelLists();

                var labels = Labels;
                var newLabels = new Dictionary<TrItemLabel, LabelPrimitive>(labels.Count);
                var worldFiles = Owner.Viewer.World.Scenery.WorldFiles;
                var cameraLocation = Owner.Viewer.Camera.CameraWorldLocation;
                foreach (var worldFile in worldFiles)
                {
                    if ((State & DisplayState.Platforms) != 0 && worldFile.platforms != null)
                    {
                        foreach (var platform in worldFile.platforms)
                        {
                            if (State == DisplayState.Auto && Platforms != null && (!Platforms.ContainsKey(platform.ItemName) || !Platforms[platform.ItemName]))
                                continue;

                            // Calculates distance between camera and platform label.
                            var distance = WorldLocation.GetDistance(platform.Location.WorldLocation, cameraLocation).Length();
                            if (distance <= MaximumDistancePlatform)
                            {
                                if (labels.ContainsKey(platform))
                                    newLabels[platform] = labels[platform];
                                else
                                    newLabels[platform] = new LabelPrimitive(Owner.Label3DMaterial, Color.Yellow, Color.Black, 0) { Position = platform.Location, Text = platform.ItemName };

                                // Change color with distance.
                                var ratio = (MathHelper.Clamp(distance, MinimumDistance, MaximumDistancePlatform) - MinimumDistance) / (MaximumDistancePlatform - MinimumDistance);
                                newLabels[platform].Color.A = newLabels[platform].Outline.A = (byte)MathHelper.Lerp(255, 0, ratio);
                            }
                        }
                    }

                    if ((State & DisplayState.Sidings) != 0 && worldFile.sidings != null)
                    {
                        foreach (var siding in worldFile.sidings)
                        {
                            if (State == DisplayState.Auto && Sidings != null && (!Sidings.ContainsKey(siding.ItemName) || !Sidings[siding.ItemName]))
                                continue;

                            // Calculates distance between camera and siding label.
                            var distance = WorldLocation.GetDistance(siding.Location.WorldLocation, cameraLocation).Length();
                            if (distance <= MaximumDistanceSiding)
                            {
                                if (labels.ContainsKey(siding))
                                    newLabels[siding] = labels[siding];
                                else
                                    newLabels[siding] = new LabelPrimitive(Owner.Label3DMaterial, Color.Orange, Color.Black, 0) { Position = siding.Location, Text = siding.ItemName };

                                // Change color with distance.
                                var ratio = (MathHelper.Clamp(distance, MinimumDistance, MaximumDistanceSiding) - MinimumDistance) / (MaximumDistanceSiding - MinimumDistance);
                                newLabels[siding].Color.A = newLabels[siding].Outline.A = (byte)MathHelper.Lerp(255, 0, ratio);
                            }
                        }
                    }
                }
                Labels = newLabels;
            }

            foreach (var primitive in Labels.Values)
                frame.AddPrimitive(Owner.Label3DMaterial, primitive, RenderPrimitiveGroup.Labels, ref Identity);
        }

        public DisplayState CurrentDisplayState
        {
            get
            {
                return State;
            }
        }
    }
}
