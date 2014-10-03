using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !ACTIVITY_EDITOR
using LibAE.Common;
using LibAE.Formats;

namespace ORTS.Formats
{
    public class TrackCircuitElement
    {
        protected TypeItem info;
        public float ElementLocation;
        public GlobalItem refItem;
        public TrackCircuitElement(GlobalItem item, float position)
        {
            refItem = item;
            ElementLocation = position;
        }
    }

    public class TrackCircuitElementConnector : TrackCircuitElement
    {
        public TrackCircuitElementConnector(GlobalItem item, float position)
            : base (item, position)
        {
            info = TypeItem.STATION_CONNECTOR;
        }
    }

    public class TrackCircuitElementPlatform : TrackCircuitElement
    {
        public TrackCircuitElementPlatform(GlobalItem item, float position)
            : base(item, position)
        {
            info = TypeItem.SIDING_START;
        }
    }
}
#endif
