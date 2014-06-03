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

/// This module ...
/// 
/// Author: Stéfan Paitoni
/// Updates : 
/// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MSTS;
using MSTS.Formats;
using MSTS.Parsers;
using ORTS.Common;

namespace LibAE
{
    public class MSTSDataConfig : MSTSData
    {
        public MSTSBase TileBase { get; protected set; }
        //public TSectionDatFile TSectionDat { get; protected set; }
        public ORRouteConfig orRouteConfig;


        public MSTSDataConfig(string mstsPath, string Route, TypeEditor interfaceType) : base (mstsPath, Route)
        {
            orRouteConfig = new ORRouteConfig();
            string routePath = Path.Combine(Route, TRK.Tr_RouteFile.FileName);
            TileBase = new MSTSBase(TDB);
            TileBase.reduce(TDB);
            orRouteConfig = ORRouteConfig.loadConfig(routePath, interfaceType);
            {
                orRouteConfig.SetRouteName(TRK.Tr_RouteFile.Name);
            }
            //Signals = new Signals(this);
        }


        public void ReAlignData(AEWidget aeWidget)
        {
            List<StationAreaItem> withConnector = new List<StationAreaItem>();
            List<globalItem> itemWidget = orRouteConfig.GetOrWidget();
            foreach (var item in itemWidget)
            {
                if (typeof(StationItem) == item.GetType() || item.typeWidget == (int)TypeWidget.STATION_WIDGET)
                {
                    List<StationAreaItem> area = ((StationItem)item).getStationArea();
                    foreach (var areaPoint in area)
                    {
                        if (areaPoint.typeWidget == (int)TypeWidget.STATION_INTERFACE)
                        {
                            withConnector.Add(areaPoint);
                        }
                    }
                }
            }
            if (withConnector.Count == 0)
                return;
            List<TrackSegment> linesSegment = aeWidget.segments;
            foreach (var lineSegment in linesSegment)
            {
                //File.AppendAllText(@"F:\temp\AE.txt", "ReAlignData: idxA: " + lineSegment.SectionIdxA + 
                //    " idxB: " + lineSegment.SectionIdxB + "\n");
                foreach (var areaPoint in withConnector)
                {
                    StationConnector stationConnector = areaPoint.getStationConnector();
                    if (stationConnector == null)
                        continue;
                    if (stationConnector.idxMaster == lineSegment.SectionIdxA &&
                        stationConnector.idxSecond == lineSegment.SectionIdxB)
                    {
                        areaPoint.DefineAsInterface(lineSegment);
                        withConnector.Remove(areaPoint);
                        break;
                    }
                }
                if (withConnector.Count <= 0)
                    break;
            }
        }
    }
}
