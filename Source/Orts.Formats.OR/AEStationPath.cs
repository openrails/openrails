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

using Newtonsoft.Json;
using Orts.Formats.Msts;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Orts.Formats.OR
{
    public class Possibility : Dictionary<string, StationPath>
    {
        public Possibility Add(StationPath path)
        {
            if (ContainsKey(path.PathName))
            {
                this[path.PathName] = path;
                return this;
            }
            else
            {
                base.Add(path.PathName, path);
                return this;
            }
        }
    }

    public class DestinationPoint : Dictionary<string, Possibility> 
    {
        public Possibility Add(string desti) 
        {
            if (ContainsKey(desti))
            {
                return this[desti];
            }

            var destination = new Possibility();
            base.Add(desti,destination); 
            return destination; 
        }
    }

    public class OriginPoint : Dictionary<string, DestinationPoint>
    {
        public DestinationPoint Add(string origin)
        {
            if (ContainsKey(origin)) 
            { 
                return this[origin]; 
            }
            var desti = new DestinationPoint();
            base.Add(origin, desti);
            return desti;
        }
    }

    public class StationPathsHelper
    {
        public OriginPoint DefinedPath;
        public OriginPoint StepInPaths;
        public OriginPoint UndefinedPath;

        public delegate void GetPaths();
        GetPaths parentFunct;

        public StationPathsHelper(GetPaths f)
        {
            DefinedPath = new OriginPoint();
            StepInPaths = new OriginPoint();
            UndefinedPath = new OriginPoint();
            parentFunct = f;
        }

        public void Clear()
        {
            // DefinedPath.Clear(); //  Do not clear this one
            StepInPaths.Clear();
            UndefinedPath.Clear();
        }

        public void Add(string inLabel, List<StationPath> paths)
        {
            if (paths == null)
                return;
            foreach (var path in paths)
            {
                string outLabel = path.outLabel;
                if (path.IsDefined())
                    DefinedPath.Add(inLabel).Add(outLabel).Add(path);
                else
                    UndefinedPath.Add(inLabel).Add(outLabel).Add(path);
            }
        }

        public void Modify(string inLabel, StationPath path)
        {
            try
            {
                if (path == null)
                    return;
                string outLabel = path.outLabel;
            }
            catch
            {
            }
        }

        public void Reload()
        {
            parentFunct();
        }
    }

    public class StationPaths
    {
        // Fields
        [JsonProperty("componentPath")]
        private List<StationPath> paths = new List<StationPath>();
        [JsonProperty("MaxPassing")]
        public double MaxPassingYard;
        [JsonProperty("ShortPassing")]
        public double ShortPassingYard;

        // Methods
        public StationPaths()
        {
            MaxPassingYard = 0;
            ShortPassingYard = double.PositiveInfinity;
        }

        public void Clear()
        {
            if (paths == null)
            {
                paths = new List<StationPath>();
            }
            foreach (StationPath path in paths)
            {
                path.Clear();
            }
            paths.Clear();
            MaxPassingYard = 0;
            ShortPassingYard = double.PositiveInfinity;
        }

        public List<StationPath> explore(AETraveller myTravel, List<TrackSegment> listConnector, MSTSItems aeItems, StationItem parent)
        {
            List<AEJunctionItem> insideJunction = new List<AEJunctionItem>();
            Stopwatch stopWatch = new Stopwatch();
            TimeSpan ts;
            string elapsedTime;
            stopWatch.Start();
            TrackNode currentNode = myTravel.GetCurrentNode();
            int pathChecked = 0;
            int trackNodeIndex = myTravel.TrackNodeIndex;
            int lastCommonTrack = trackNodeIndex;
            int trackVectorSectionIndex = myTravel.TrackVectorSectionIndex;
            TrVectorSection currentSection = myTravel.GetCurrentSection();
            GlobalItem startNode = aeItems.GetTrackSegment(currentNode, trackVectorSectionIndex);
            //paths.Add(new StationPath(startNode, myTravel));
            paths.Add(new StationPath(myTravel));
            paths[0].LastCommonTrack = trackNodeIndex;
            while ((pathChecked < paths.Count && !paths[pathChecked].complete) && paths.Count < 100)
            {
                TrackNode node2 = paths[pathChecked].explore(aeItems, listConnector, trackNodeIndex, parent);
                ts = stopWatch.Elapsed;

                // Format and display the TimeSpan value. 
                elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);
                Console.WriteLine("RunTime " + elapsedTime);

                if (node2.TrJunctionNode != null)
                {
                    AEJunctionItem junction = (AEJunctionItem)paths[pathChecked].ComponentItem[paths[pathChecked].ComponentItem.Count-1];
                    if (!insideJunction.Contains(junction))
                    {
                        insideJunction.Add(junction);
                    }
                    if (node2.TrPins[0].Link == lastCommonTrack)
                    {
                        paths[pathChecked].jctnIdx = paths[pathChecked].ComponentItem.Count - 1;
                        paths[pathChecked].LastCommonTrack = lastCommonTrack;
                        paths.Add(new StationPath(paths[pathChecked]));
                        paths[pathChecked].directionJunction = 0;
                        paths[pathChecked].switchJnct(0);
                    }
                    else
                        paths[pathChecked].NextNode();
                }
                else if (node2.TrEndNode)
                {
                    AEBufferItem buffer = (AEBufferItem)paths[pathChecked].ComponentItem[paths[pathChecked].ComponentItem.Count-1];
                    if (!buffer.Configured || buffer.DirBuffer == AllowedDir.OUT)
                    {
                        //AEJunctionItem junction = (AEJunctionItem)paths[pathChecked].ComponentItem[paths[pathChecked].jctnIdx];
                        paths.RemoveAt(pathChecked);
                    }
                    else
                    {
                        paths[pathChecked].setComplete(buffer);
                        pathChecked++;
                    }
                    if (pathChecked < paths.Count)
                    {
                        paths[pathChecked].switchJnct(paths[pathChecked].directionJunction);
                        if (paths[pathChecked].ComponentItem.Count > 1)
                            lastCommonTrack = (int)paths[pathChecked].LastCommonTrack;
                        else
                            lastCommonTrack = trackNodeIndex;

                    }
                }
                else 
                {
                    int lastIndex = (int)node2.Index;
                    //lastCommonTrack = (int)node2.Index;
                    if (paths[pathChecked].complete)
                    {
                        TrackSegment segment = (TrackSegment)paths[pathChecked].ComponentItem[paths[pathChecked].ComponentItem.Count - 1];
                        
                        if (segment.HasConnector == null ||
                            (segment.HasConnector != null && 
                            (segment.HasConnector.dirConnector == AllowedDir.IN || !segment.HasConnector.isConfigured())))
                        {
                            paths.RemoveAt(pathChecked);
                        }
                        else
                        {
                            pathChecked++;
                        }
                        //pathChecked++;
                        if (pathChecked < paths.Count)
                        {
                            lastIndex = (int)paths[pathChecked].ComponentItem[paths[pathChecked].ComponentItem.Count - 2].associateNode.Index;
                            paths[pathChecked].switchJnct(paths[pathChecked].directionJunction);
                        }
                    }
                    if (pathChecked < paths.Count)
                    {
                        if (paths[pathChecked].ComponentItem.Count > 1)
                        {
                            lastCommonTrack = lastIndex;
                            //lastCommonTrack = (int)paths[pathChecked].ComponentItem[paths[pathChecked].ComponentItem.Count - 2].associateNode.Index;
                        }
                        else
                            lastCommonTrack = trackNodeIndex;
                    }
                }
            }
            stopWatch.Stop();
            ts = stopWatch.Elapsed;

            // Format and display the TimeSpan value. 
            elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            Console.WriteLine("RunTime " + elapsedTime);

            pathChecked = 1;
            foreach (StationPath path in paths)
            {
                if (path.PassingYard > MaxPassingYard)
                    MaxPassingYard = path.PassingYard;
                if (path.PassingYard < ShortPassingYard)
                    ShortPassingYard = path.PassingYard;
           }
            return paths;
        }

        public void highlightTrackFromArea(MSTSItems aeItems)
        {
            foreach (StationPath path in paths)
            {
                path.highlightTrackFromArea(aeItems);
            }
        }

        public List<StationPath> getPaths()
        {
            return paths;
        }
    }

    public class StationPath
    {
        // Fields
        [JsonIgnore]
        public List<GlobalItem> ComponentItem { get; protected set; }
        [JsonIgnore]
        public List<SideItem> SidesItem { get; protected set; }
        // Properties
        [JsonProperty("label")]
        public string outLabel;
        [JsonProperty("PathName")]
        public string PathName;
        [JsonIgnore]
        public bool complete { get; protected set; }
        [JsonIgnore]
        public double Siding { get; protected set; }
        [JsonIgnore]
        public double Platform { get; protected set; }
        [JsonIgnore]
        public double PassingYard { get; protected set; }
        [JsonIgnore]
        public int jctnIdx { get; set; }
        [JsonIgnore]
        public int LastCommonTrack { get; set; }
        [JsonIgnore]
        public short directionJunction { get; set; }
        [JsonProperty("NbrPlatform")]
        public int NbrPlatform { get; protected set; }
        [JsonProperty("NbrSiding")]
        public int NbrSiding { get; protected set; }
        [JsonProperty("NbrPassingYard")]
        public int NbrPassingYard { get; protected set; }
        [JsonProperty("IsMainPath")]
        public bool MainPath { get; protected set; }
        [JsonIgnore]
        public AETraveller traveller { get; private set; }
        [JsonIgnore]
        protected StationPaths parent;

        // Methods
        public StationPath()
        {
            ComponentItem = new List<GlobalItem>();
            SidesItem = new List<SideItem>();
            complete = false;
            jctnIdx = -1;
            traveller = null;
            Siding = 0;
            Platform = 0;
            PassingYard = 0;
            NbrPlatform = 0;
            NbrSiding = 0;
            MainPath = true;
            LastCommonTrack = 0;
            directionJunction = 0;
            PathName = "";
        }

        public StationPath(StationPath original)
        {
            traveller = new AETraveller(original.traveller);
            MainPath = false;
            ComponentItem = new List<GlobalItem>();
            SidesItem = new List<SideItem>();
            PathName = "";
            if (original.ComponentItem.Count > 0)
            {
                foreach (GlobalItem componentItem in original.ComponentItem)
                {
                    ComponentItem.Add(componentItem);
                }
                foreach (SideItem sideItem in original.SidesItem)
                {
                    SidesItem.Add(sideItem);
                }
                complete = false;
                if (original.ComponentItem[original.ComponentItem.Count - 1].GetType() == typeof(AEJunctionItem))
                {
                    jctnIdx = original.ComponentItem.Count - 1;
                }
                else
                {
                    jctnIdx = -1;
                }
                NbrPlatform = original.NbrPlatform;
                NbrSiding = original.NbrSiding;
                Siding = original.Siding;
                Platform = original.Platform;
                PassingYard = original.PassingYard;
                LastCommonTrack = original.LastCommonTrack;
                directionJunction = 1;
            }
        }

        public StationPath(GlobalItem startNode, AETraveller travel)
        {
            ComponentItem = new List<GlobalItem>();
            SidesItem = new List<SideItem>();
            ComponentItem.Add(startNode);
            complete = false;
            jctnIdx = -1;
            traveller = new AETraveller(travel);
            Siding = 0;
            Platform = 0;
            PassingYard = 0;
            NbrPlatform = 0;
            NbrSiding = 0;
            LastCommonTrack = 0;
            directionJunction = 0;
            PathName = "";
        }

        public StationPath(AETraveller travel)
        {
            ComponentItem = new List<GlobalItem>();
            SidesItem = new List<SideItem>();
            complete = false;
            jctnIdx = -1;
            traveller = travel;
            Siding = 0;
            Platform = 0;
            PassingYard = 0;
            NbrPlatform = 0;
            NbrSiding = 0;
            LastCommonTrack = 0;
            PathName = "";
        }

        public void Clear()
        {
            if (ComponentItem == null)
            {
                ComponentItem = new List<GlobalItem>();
            }
            ComponentItem.Clear();
            SidesItem.Clear();
            PathName = "";
        }

        public TrackNode explore(MSTSItems aeItems, List<TrackSegment> listConnector, int entryNode, StationItem parent)
        {
#if false
            Stopwatch stopWatch = new Stopwatch();
            TimeSpan ts;
            string elapsedTime;
            
#endif
            TrackNode currentNode = traveller.GetCurrentNode();
            if ((currentNode.TrJunctionNode == null) && !currentNode.TrEndNode)
            {
                do
                {
                    int sectionIdx = traveller.TrackVectorSectionIndex;
                    TrackSegment item = (TrackSegment)aeItems.GetTrackSegment(currentNode, sectionIdx);
                    foreach (TrackSegment conSeg in listConnector)
                    {
                        if (conSeg.associateNodeIdx == entryNode)
                            continue;
                        //  Il faut tester que l'on change bien d'index de node pour quitter  mais pas pour le premier et aussi l'idx de la section
                        if (currentNode.Index == conSeg.associateNodeIdx && sectionIdx == conSeg.associateSectionIdx)
                        {
                            setComplete(conSeg);
                            break;
                        }
                    }

                    item.inStationArea = true;
                    ComponentItem.Add(item);
                    ((TrackSegment)item).InStation(parent);

                    foreach (var trItem in item.sidings)
                    {
                        SidesItem.Add(trItem);
                        if (trItem.typeSiding == (int)TypeSiding.SIDING_START)
                        {
                            PathName = trItem.Name;
                            NbrSiding++;
                            if (trItem.sizeSiding > Siding)
                                Siding = trItem.sizeSiding;
                        }
                        else if (trItem.typeSiding == (int)TypeSiding.PLATFORM_START)
                        {
                            PathName = trItem.Name;
                            NbrPlatform++;
                            if (trItem.sizeSiding > Platform)
                                Platform = trItem.sizeSiding;
                        }
                    }
                    //yard += sideItem.lengthSegment;
#if false
                    ts = stopWatch.Elapsed;

                    // Format and display the TimeSpan value. 
                    elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                        ts.Hours, ts.Minutes, ts.Seconds,
                        ts.Milliseconds / 10);
                    elapse.Add(elapsedTime);
                    
#endif
                } while (traveller.NextVectorSection() && !complete) ;
                if (currentNode.Index != entryNode && !complete && traveller.TrackNodeLength > PassingYard)
                    PassingYard = traveller.TrackNodeLength;
                traveller.NextTrackNode();
            }
            else
            {
                GlobalItem item = aeItems.GetTrackSegment(currentNode, -1);
                item.inStationArea = true;
                ComponentItem.Add(item);
            }
            if (currentNode.TrEndNode)
            {
                complete = true;
            }
            return currentNode;
        }

        public void highlightTrackFromArea(MSTSItems aeItems)
        {
            foreach (GlobalItem item in ComponentItem)
            {
                if (item.GetType() == typeof(TrackSegment))
                {
                    ((TrackSegment)item).setAreaSnaps(aeItems);
                }
            }
        }

        public void setComplete(GlobalItem segment)
        {
            complete = true;
            
            if (segment.GetType() == typeof(TrackSegment) && ((TrackSegment)segment).HasConnector != null)
            {
                outLabel = ((TrackSegment)segment).HasConnector.label;
            }
            else if (segment.GetType() == typeof(AEBufferItem))
            {
                outLabel = ((AEBufferItem)segment).NameBuffer;
            }
        }

        public AETraveller switchJnct(short direction)
        {
            TrackNode junction = traveller.GetCurrentNode();
            if (junction.TrJunctionNode != null)
            {
                junction.TrJunctionNode.SelectedRoute = direction;
            }
            traveller.NextTrackNode();
            return traveller;
        }

        public void NextNode()
        {
            traveller.NextTrackNode();
        }

        public bool IsDefined()
        {
            if ((NbrPlatform + NbrSiding + NbrPassingYard) > 0)
                return true;
            else
                return false;
        }
    }
}
