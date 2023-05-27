namespace LibAE.Formats
{
#if false
    //================================================================================================//
    //
    // class TrackCircuitSection
    //
    //================================================================================================//
    //
    // Class for track circuit and train control
    //

    public class TrackCircuitSection : TrackCircuitSectionXref
    {
        public enum TrackCircuitType
        {
            Normal,
            Junction,
            Crossover,
            EndOfTrack,
            Empty,
        }

        public Signals signalRef;                                 // reference to Signals class //
        //public int Index;                                         // section index              //
        public int OriginalIndex;                                 // original TDB section index //
        public TrackCircuitType CircuitType;                           // type of section            //

        public TrPin[,] Pins = new TrPin[2, 2];                   // next sections              //
        public TrPin[,] ActivePins = new TrPin[2, 2];             // active next sections       //
        public bool[] EndIsTrailingJunction = new bool[2];        // next section is trailing jn//

        public int JunctionDefaultRoute = -1;                     // jn default route, value is out-pin      //
        public int JunctionLastRoute = -1;                        // jn last route, value is out-pin         //
        public int JunctionSetManual = -1;                        // jn set manual, value is out-pin         //

        public float Length;                                      // full length                //
        public float[] OffsetLength = new float[2];               // offset length in orig sect //

        public double Overlap;                                    // overlap for junction nodes //
        public List<int> PlatformIndex = new List<int>();         // platforms along section    //

        public TrackCircuitItems CircuitItems;                    // all items                  //
        public TrackCircuitState CircuitState;                    // normal states              //
        public SignalObject[] EndSignals = new SignalObject[2];   // signals at either end      //
        public List<int> SignalsPassingRoutes;                    // list of signals reading passed junction //
        //================================================================================================//
        //
        // Constructor
        //


        public TrackCircuitSection(TrackNode thisNode, int orgINode,
                        TSectionDatFile tsectiondat, Signals thisSignals)
        {

            //
            // Copy general info
            //

            signalRef = thisSignals;

            Index = orgINode;
            OriginalIndex = orgINode;

            if (thisNode.TrEndNode)
            {
                CircuitType = TrackCircuitType.EndOfTrack;
            }
            else if (thisNode.TrJunctionNode != null)
            {
                CircuitType = TrackCircuitType.Junction;
            }
            else
            {
                CircuitType = TrackCircuitType.Normal;
            }


            //
            // Preset pins, then copy pin info
            //

            for (int direction = 0; direction < 2; direction++)
            {
                for (int pin = 0; pin < 2; pin++)
                {
                    Pins[direction, pin] = new TrPin();
                    Pins[direction, pin].Direction = -1;
                    Pins[direction, pin].Link = -1;
                    ActivePins[direction, pin] = new TrPin();
                    ActivePins[direction, pin].Direction = -1;
                    ActivePins[direction, pin].Link = -1;
                }
            }

            int PinNo = 0;
            for (int pin = 0; pin < Math.Min(thisNode.Inpins, Pins.GetLength(1)); pin++)
            {
                Pins[0, pin] = thisNode.TrPins[PinNo].Copy();
                PinNo++;
            }
            if (PinNo < thisNode.Inpins) PinNo = (int)thisNode.Inpins;
            for (int pin = 0; pin < Math.Min(thisNode.Outpins, Pins.GetLength(1)); pin++)
            {
                Pins[1, pin] = thisNode.TrPins[PinNo].Copy();
                PinNo++;
            }


            //
            // preset no end signals
            // preset no trailing junction
            //

            for (int direction = 0; direction < 2; direction++)
            {
                EndSignals[direction] = null;
                EndIsTrailingJunction[direction] = false;
            }

            //
            // Preset length and offset
            // If section index not in tsectiondat, set length to 0.
            //

            float totalLength = 0.0f;

            if (thisNode.TrVectorNode != null && thisNode.TrVectorNode.TrVectorSections != null)
            {
                foreach (TrVectorSection thisSection in thisNode.TrVectorNode.TrVectorSections)
                {
                    float thisLength = 0.0f;

                    if (tsectiondat.TrackSections.ContainsKey(thisSection.SectionIndex))
                    {
                        TrackSection TS = tsectiondat.TrackSections[thisSection.SectionIndex];

                        if (TS.SectionCurve != null)
                        {
                            thisLength =
                                    MathHelper.ToRadians(Math.Abs(TS.SectionCurve.Angle)) *
                                    TS.SectionCurve.Radius;
                        }
                        else
                        {
                            thisLength = TS.SectionSize.Length;

                        }
                    }

                    totalLength += thisLength;
                }
            }

            Length = totalLength;

            for (int direction = 0; direction < 2; direction++)
            {
                OffsetLength[direction] = 0;
            }

            //
            // set signal list for junctions
            //

            if (CircuitType == TrackCircuitType.Junction)
            {
                SignalsPassingRoutes = new List<int>();
            }
            else
            {
                SignalsPassingRoutes = null;
            }

            // for Junction nodes, obtain default route
            // set switch to default route
            // copy overlap (if set)

            if (CircuitType == TrackCircuitType.Junction)
            {
                uint trackShapeIndex = thisNode.TrJunctionNode.ShapeIndex;
                try
                {
                    TrackShape trackShape = tsectiondat.TrackShapes[trackShapeIndex];
                    JunctionDefaultRoute = (int)trackShape.MainRoute;

                    Overlap = trackShape.ClearanceDistance;
                }
                catch (Exception)
                {
                    JunctionDefaultRoute = 0;
                    Overlap = 0;
                }

                JunctionLastRoute = JunctionDefaultRoute;
                //signalRef.setSwitch(OriginalIndex, JunctionLastRoute, this);
            }

            //
            // Create circuit items
            //

            CircuitItems = new TrackCircuitItems();
            CircuitState = new TrackCircuitState();
        }

        //================================================================================================//
        //
        // Constructor for empty entries
        //

        public TrackCircuitSection(int INode, Signals thisSignals)
        {

            signalRef = thisSignals;

            Index = INode;
            OriginalIndex = -1;
            CircuitType = TrackCircuitType.Empty;

            for (int iDir = 0; iDir < 2; iDir++)
            {
                EndIsTrailingJunction[iDir] = false;
                EndSignals[iDir] = null;
                OffsetLength[iDir] = 0;
            }

            for (int iDir = 0; iDir < 2; iDir++)
            {
                for (int pin = 0; pin < 2; pin++)
                {
                    Pins[iDir, pin] = new TrPin();
                    Pins[iDir, pin].Direction = -1;
                    Pins[iDir, pin].Link = -1;
                    ActivePins[iDir, pin] = new TrPin();
                    ActivePins[iDir, pin].Direction = -1;
                    ActivePins[iDir, pin].Link = -1;
                }
            }

            CircuitItems = new TrackCircuitItems();
            CircuitState = new TrackCircuitState();
        }

        //================================================================================================//
        //
        // Copy basic info only
        //

        public TrackCircuitSection CopyBasic(int INode)
        {
            TrackCircuitSection newSection = new TrackCircuitSection(INode, this.signalRef);

            newSection.OriginalIndex = this.OriginalIndex;
            newSection.CircuitType = this.CircuitType;

            newSection.EndSignals[0] = this.EndSignals[0];
            newSection.EndSignals[1] = this.EndSignals[1];

            newSection.Length = this.Length;

            Array.Copy(this.OffsetLength, newSection.OffsetLength, this.OffsetLength.Length);

            return (newSection);
        }

        //================================================================================================//
        //
        // align pins switch or crossover
        //

        public void alignSwitchPins(int linkedSectionIndex)
        {
            int alignDirection = -1;  // pin direction for leading section
            int alignLink = -1;       // link index for leading section

            for (int iDirection = 0; iDirection <= 1; iDirection++)
            {
                for (int iLink = 0; iLink <= 1; iLink++)
                {
                    if (Pins[iDirection, iLink].Link == linkedSectionIndex)
                    {
                        alignDirection = iDirection;
                        alignLink = iLink;
                    }
                }
            }

            if (alignDirection >= 0)
            {
                ActivePins[alignDirection, 0].Link = -1;
                ActivePins[alignDirection, 1].Link = -1;

                ActivePins[alignDirection, alignLink].Link =
                        Pins[alignDirection, alignLink].Link;
                ActivePins[alignDirection, alignLink].Direction =
                        Pins[alignDirection, alignLink].Direction;

                TrackCircuitSection linkedSection = signalRef.TrackCircuitList[linkedSectionIndex];
                for (int iDirection = 0; iDirection <= 1; iDirection++)
                {
                    for (int iLink = 0; iLink <= 1; iLink++)
                    {
                        if (linkedSection.Pins[iDirection, iLink].Link == Index)
                        {
                            linkedSection.ActivePins[iDirection, iLink].Link = Index;
                            linkedSection.ActivePins[iDirection, iLink].Direction =
                                    linkedSection.Pins[iDirection, iLink].Direction;
                        }
                    }
                }
            }

            // if junction, align physical switch

            if (CircuitType == TrackCircuitType.Junction)
            {
                int switchPos = -1;
                if (ActivePins[1, 0].Link != -1)
                    switchPos = 0;
                if (ActivePins[1, 1].Link != -1)
                    switchPos = 1;

                if (switchPos >= 0)
                {
                    //signalRef.setSwitch(OriginalIndex, switchPos, this);
                }
            }
        }

        //================================================================================================//
        //
        // de-align active switch pins
        //

        public void deAlignSwitchPins()
        {
            for (int iDirection = 0; iDirection <= 1; iDirection++)
            {
                if (Pins[iDirection, 1].Link > 0)     // active switchable end
                {
                    for (int iLink = 0; iLink <= 1; iLink++)
                    {
                        int activeLink = Pins[iDirection, iLink].Link;
                        int activeDirection = Pins[iDirection, iLink].Direction == 0 ? 1 : 0;
                        ActivePins[iDirection, iLink].Link = -1;

                        TrackCircuitSection linkSection = signalRef.TrackCircuitList[activeLink];
                        linkSection.ActivePins[activeDirection, 0].Link = -1;
                    }
                }
            }
        }


        //================================================================================================//
        //
        // Get state of single section
        //

        //================================================================================================//
        //
        // Get next active link
        //

        public TrPin GetNextActiveLink(int direction, int lastIndex)
        {

            // Crossover

            if (CircuitType == TrackCircuitSection.TrackCircuitType.Crossover)
            {
                int inPinIndex = direction == 0 ? 1 : 0;
                if (Pins[inPinIndex, 0].Link == lastIndex)
                {
                    return (ActivePins[direction, 0]);
                }
                else if (Pins[inPinIndex, 1].Link == lastIndex)
                {
                    return (ActivePins[direction, 1]);
                }
                else
                {
                    TrPin dummyPin = new TrPin();
                    dummyPin.Direction = -1;
                    dummyPin.Link = -1;
                    return (dummyPin);
                }
            }

            // All other sections

            if (ActivePins[direction, 0].Link > 0)
            {
                return (ActivePins[direction, 0]);
            }

            return (ActivePins[direction, 1]);
        }

        //================================================================================================//
        //
        // Get distance between objects
        //

        public float GetDistanceBetweenObjects(int startSectionIndex, float startOffset, int startDirection,
            int endSectionIndex, float endOffset)
        {
            int thisSectionIndex = startSectionIndex;
            int direction = startDirection;

            TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisSectionIndex];

            float distanceM = 0.0f;
            int lastIndex = -2;  // set to non-occuring value

            while (thisSectionIndex != endSectionIndex && thisSectionIndex > 0)
            {
                distanceM += thisSection.Length;
                TrPin nextLink = thisSection.GetNextActiveLink(direction, lastIndex);

                lastIndex = thisSectionIndex;
                thisSectionIndex = nextLink.Link;
                direction = nextLink.Direction;

                if (thisSectionIndex > 0)
                    thisSection = signalRef.TrackCircuitList[thisSectionIndex];
            }

            // use found distance, correct for begin and end offset

            if (thisSectionIndex == endSectionIndex)
            {
                distanceM += endOffset - startOffset;
                return (distanceM);
            }

            return (-1.0f);
        }

        //================================================================================================//

    }// class TrackCircuitSection


    //================================================================================================//
    //
    // class TrackCircuitCrossReference
    //
    //================================================================================================//
    //
    // Class for track circuit cross reference, added to TDB info
    //

    public class TrackCircuitCrossReference
    {
        public int CrossRefIndex;
        public float Length;
        public float[] Position = new float[2];

        //================================================================================================//
        //
        // Constructor
        //

        public TrackCircuitCrossReference(TrackCircuitSection thisSection)
        {
            CrossRefIndex = thisSection.Index;
            Length = thisSection.Length;
            Position[0] = thisSection.OffsetLength[0];
            Position[1] = thisSection.OffsetLength[1];
        }

    }

    //================================================================================================//
    //
    // class TrackCircuitXRefList
    //

    public class TrackCircuitXRefList : List<TrackCircuitCrossReference>
    {

        //================================================================================================//
        //
        // get XRef index
        //

        private int GetXRefIndex(float offset, int direction)
        {
            int foundSection = -1;

            if (direction == 0)
            {
                for (int TC = 1; TC < this.Count && foundSection < 0; TC++)
                {
                    TrackCircuitCrossReference thisReference = this[TC];
                    if (thisReference.Position[direction] > offset)
                    {
                        foundSection = TC - 1;
                    }
                }

                if (foundSection < 0)
                {
                    TrackCircuitCrossReference thisReference = this[this.Count - 1];
                    if (offset <= (thisReference.Position[direction] + thisReference.Length))
                    {
                        foundSection = this.Count - 1;
                    }
                }
            }
            else
            {
                for (int TC = this.Count - 2; TC >= 0 && foundSection < 0; TC--)
                {
                    TrackCircuitCrossReference thisReference = this[TC];
                    if (thisReference.Position[direction] > offset)
                    {
                        foundSection = TC + 1;
                    }
                }

                if (foundSection < 0)
                {
                    TrackCircuitCrossReference thisReference = this[0];
                    if (offset <= (thisReference.Position[direction] + thisReference.Length))
                    {
                        foundSection = 0;
                    }
                }
            }

            if (foundSection < 0)
            {
                if (direction == 0)
                {
                    foundSection = 0;
                }
                else
                {
                    foundSection = this.Count - 1;
                }
            }

            return (foundSection);
        }

        //================================================================================================//
        //
        // Get Section index
        //

        public int GetSectionIndex(float offset, int direction)
        {
            int XRefIndex = GetXRefIndex(offset, direction);

            if (XRefIndex >= 0)
            {
                TrackCircuitCrossReference thisReference = this[XRefIndex];
                return (thisReference.CrossRefIndex);
            }
            else
            {
                return (-1);
            }
        }
    } // class TrackCircuitXRefList
    
    //================================================================================================//
    //
    // class TrackCircuitSignalList
    //
    //================================================================================================//
    //
    // Class for track circuit signal list
    //

    public class TrackCircuitSignalList
    {
        public List<TrackCircuitSignalItem> TrackCircuitItem = new List<TrackCircuitSignalItem>();
        // List of signal items //
    }

    //================================================================================================//
    //================================================================================================//
    //
    // class TrackCircuitSignalItem
    //
    //================================================================================================//
    //
    // Class for track circuit signal sideItem
    //

    public class TrackCircuitSignalItem
    {
        public ObjectItemInfo.ObjectItemFindState SignalState;  // returned state // 
        public SignalObject SignalRef;            // related SignalObject     //
        public float SignalLocation;              // relative signal position //


        //================================================================================================//
        //
        // Constructor
        //

        public TrackCircuitSignalItem(SignalObject thisRef, float thisLocation)
        {
            SignalState = ObjectItemInfo.ObjectItemFindState.Object;
            SignalRef = thisRef;
            SignalLocation = thisLocation;
        }


        public TrackCircuitSignalItem(ObjectItemInfo.ObjectItemFindState thisState)
        {
            SignalState = thisState;
        }
    }


    //================================================================================================//
    //
    // class CrossOverItem
    //
    //================================================================================================//
    //
    // Class for cross over items
    //
    //================================================================================================//
    //
    // class TrackCircuitItems
    //
    //================================================================================================//
    //
    // Class for track circuit sideItem storage
    //

    public class TrackCircuitItems
    {
        public TrackCircuitSignalList[,]
            TrackCircuitSignals = new TrackCircuitSignalList[2, (int)MstsSignalFunction.UNKNOWN];
        // List of signals (per direction and per type) //
        public TrackCircuitSignalList[]
            TrackCircuitSpeedPosts = new TrackCircuitSignalList[2];
        // List of speedposts (per direction) //
        public List<TrackCircuitMilepost> Mileposts = new List<TrackCircuitMilepost>();
        // List of mileposts //

        //================================================================================================//
        //
        // Constructor
        //

        public TrackCircuitItems()
        {
            TrackCircuitSignalList thisList;

            for (int iDirection = 0; iDirection <= 1; iDirection++)
            {
                for (int fntype = 0; fntype < (int)MstsSignalFunction.UNKNOWN; fntype++)
                {
                    thisList = new TrackCircuitSignalList();
                    TrackCircuitSignals[iDirection, fntype] = thisList;
                }

                thisList = new TrackCircuitSignalList();
                TrackCircuitSpeedPosts[iDirection] = thisList;
            }
        }
    }
#endif


}
