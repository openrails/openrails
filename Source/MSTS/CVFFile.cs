/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace MSTS
{

	// TODO - this is an incomplete parse of the cvf file.
	public class CVFFile
	{
        public List<Vector3> Locations = new List<Vector3>();   // Head locations for front, left and right views
        public List<Vector3> Directions = new List<Vector3>();  // Head directions for each view
        public List<string> TwoDViews = new List<string>();     // 2D CAB Views - by GeorgeS
        public List<string> NightViews = new List<string>();    // Night CAB Views - by GeorgeS
        public List<string> LightViews = new List<string>();    // Light CAB Views - by GeorgeS
        public CabViewControls CabViewControls = null;     // Controls in CAB - by GeorgeS

        public CVFFile(string filePath)
		{
            STFReader inf = new STFReader( filePath );
            string Path = filePath.Substring(0, filePath.LastIndexOf('\\') + 1);
            try
            {
                inf.MustMatch( "Tr_CabViewFile" );
                inf.VerifyStartOfBlock();
                while( !inf.EOF() )
                {
                    string token = inf.ReadToken();
                    if (0 == string.Compare(token, "Position", true))
                        Locations.Add(inf.ReadVector3Block());

                    else if (0 == string.Compare(token, "Direction", true))
                        Directions.Add(inf.ReadVector3Block());

                    // Read CAB View files for 2D cab views - by GeorgeS
                    else if (0 == string.Compare(token, "CabViewFile", true))
                    {
                        string fName = inf.ReadStringBlock();
                        TwoDViews.Add(Path + fName);
                        NightViews.Add(Path + "night\\" + fName);
                        LightViews.Add(Path + "cablight\\" + fName);
                    }
                    else if (string.Compare(token, "CabViewControls", true) == 0)
                    {
                        CabViewControls = new CabViewControls(inf, Path);
                    }
                    else
                        inf.SkipBlock();  // TODO, complete parse

                }
            }
            finally
            {
                inf.Close();
            }
		}

	} // class CVFFile

    public enum CABViewControlTypes
    {
        NONE,
        SPEEDOMETER,
        MAIN_RES,
        EQ_RES,
        BRAKE_CYL,
        BRAKE_PIPE,
        LINE_VOLTAGE,
        AMMETER,
        LOAD_METER,
        THROTTLE,
        PANTOGRAPH,
        TRAIN_BRAKE,
        ENGINE_BRAKE,
        DYNAMIC_BRAKE,
        DYNAMIC_BRAKE_DISPLAY,
        SANDERS,
        WIPERS,
        HORN,
        BELL,
        FRONT_HLIGHT,
        DIRECTION,
        ASPECT_DISPLAY,
        THROTTLE_DISPLAY,
        CPH_DISPLAY,
        PANTO_DISPLAY,
        DIRECTION_DISPLAY
    }

    public enum CABViewControlStyles
    {
        NONE,
        NEEDLE,
        POINTER,
        SOLID,
        LIQUID,
        SPRUNG,
        NOT_SPRUNG,
        WHILE_PRESSED,
        ONOFF
    }

    public enum CABViewControlUnits
    {
        NONE,
        BAR,
        PSI,
        KILOPASCALS,
        KGS_PER_SQUARE_CM,
        APMS,
        VOLTS,
        KILOVOLTS,
        KM_PER_HOUR,
        MILES_PER_HOUR
    }

    public class CabViewControls : List<CabViewControl>
    {
        public CabViewControls(STFReader inf, string basePath)
        {
            inf.VerifyStartOfBlock();

            int count = inf.ReadInt();

            try
            {
                while (!inf.EndOfBlock())
                {
                    string token = inf.ReadToken();
                    if (string.Compare(token, "Dial", true) == 0)
                    {
                        CVCDial dial = new CVCDial(inf, basePath);
                        Add(dial);
                    }
                    else if (string.Compare(token, "Gauge", true) == 0)
                    {
                        CVCGauge gauge = new CVCGauge(inf, basePath);
                        Add(gauge);
                    }
                    else if (string.Compare(token, "Lever", true) == 0)
                    {
                        CVCDiscrete lever = new CVCDiscrete(inf, basePath);
                        Add(lever);
                    }
                    else if (string.Compare(token, "TwoState", true) == 0)
                    {
                        CVCDiscrete twostate = new CVCDiscrete(inf, basePath);
                        Add(twostate);
                    }
                    else if (string.Compare(token, "TriState", true) == 0)
                    {
                        CVCDiscrete tristate = new CVCDiscrete(inf, basePath);
                        Add(tristate);
                    }
                    else if (string.Compare(token, "MultiStateDisplay", true) == 0)
                    {
                        CVCMultiStateDisplay multi = new CVCMultiStateDisplay(inf, basePath);
                        Add(multi);
                    }
                    else if (string.Compare(token, "CabSignalDisplay", true) == 0)
                    {
                        CVCSignal cabsignal = new CVCSignal(inf, basePath);
                        Add(cabsignal);
                    }
                    else if (string.Compare(token, "Digital", true) == 0)
                    {
                        CVCDigital digital = new CVCDigital(inf, basePath);
                        Add(digital);
                    }
                    else
                    {
                        inf.SkipBlock();
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(string.Format("Error reading CAB View file {0}", inf.FileName));
                Trace.WriteLine(ex);
            }

            /*
             * Uncomment when parsed all type
            if (count != this.Count)
                STFException.ReportError(inf, "CabViewControl count mismatch");
            */
        }
    }
    
    public class CabViewControl
    {
        public double PositionX = 0;
        public double PositionY = 0;
        public double Width = 0;
        public double Height = 0;

        public double MinValue = 0;
        public double MaxValue = 0;

        public string ACEFile = "";

        public CABViewControlTypes ControlType = CABViewControlTypes.NONE;
        public CABViewControlStyles ControlStyle = CABViewControlStyles.NONE;
        public CABViewControlUnits Units = CABViewControlUnits.NONE;

        public void Parse(string token, STFReader inf, string basePath)
        {
            List<CABViewControlStyles> ccss = new List<CABViewControlStyles>((IEnumerable<CABViewControlStyles>)Enum.GetValues(typeof(CABViewControlStyles)));
            List<CABViewControlTypes> ccts = new List<CABViewControlTypes>((IEnumerable<CABViewControlTypes>)Enum.GetValues(typeof(CABViewControlTypes)));
            List<CABViewControlUnits> ccms = new List<CABViewControlUnits>((IEnumerable<CABViewControlUnits>)Enum.GetValues(typeof(CABViewControlUnits)));
            string s;

            if (string.Compare(token, "Type", true) == 0)
            {
                inf.VerifyStartOfBlock();
                s = inf.ReadString();
                var qtr = (from ctc in ccts
                           where ctc.ToString().ToLower() == s.ToLower()
                           select ctc).FirstOrDefault();

                ControlType = qtr;

                s = inf.ReadString(); // Skip again Type 
                inf.VerifyEndOfBlock();
            }
            else if (string.Compare(token, "Position", true) == 0)
            {
                inf.VerifyStartOfBlock();
                PositionX = inf.ReadInt();
                PositionY = inf.ReadInt();
                Width = inf.ReadInt();
                Height = inf.ReadInt();
                inf.VerifyEndOfBlock();
            }
            else if (string.Compare(token, "ScaleRange", true) == 0)
            {
                inf.VerifyStartOfBlock();
                MinValue = inf.ReadInt();
                MaxValue = inf.ReadInt();
                inf.VerifyEndOfBlock();
            }
            else if (string.Compare(token, "Graphic", true) == 0)
            {
                ACEFile = basePath + inf.ReadStringBlock();
            }
            else if (string.Compare(token, "Style", true) == 0)
            {
                inf.VerifyStartOfBlock();
                s = inf.ReadString();
                var qsr = (from cts in ccss
                           where cts.ToString().ToLower() == s.ToLower()
                           select cts).FirstOrDefault();

                ControlStyle = qsr;

                inf.VerifyEndOfBlock();
            }
            else if (string.Compare(token, "Units", true) == 0)
            {
                inf.VerifyStartOfBlock();
                s = inf.ReadString();
                var qmr = (from ctm in ccms
                           where ctm.ToString().ToLower() == s.ToLower()
                           select ctm).FirstOrDefault();

                Units = qmr;

                inf.VerifyEndOfBlock();
            }
            else
            {
                inf.SkipBlock();
            }
        }
    }

    public class CVCDial : CabViewControl
    {
        public float FromDegree = 0;
        public float ToDegree = 0;
        public int Center = 0;
        public int Direction = 0;
        
        public CVCDial(STFReader inf, string basePath)
        {
            inf.VerifyStartOfBlock();

            while (!inf.EndOfBlock())
            {
                string token = inf.ReadToken();
                if (string.Compare(token, "ScalePos", true) == 0)
                {
                    inf.VerifyStartOfBlock();
                    FromDegree = inf.ReadInt();
                    ToDegree = inf.ReadInt();
                    inf.VerifyEndOfBlock();
                }
                else if (string.Compare(token, "Pivot", true) == 0)
                {
                    Center = inf.ReadIntBlock();
                }
                else if (string.Compare(token, "DirIncrease", true) == 0)
                {
                    Direction = inf.ReadIntBlock();
                }
                else
                {
                    base.Parse(token, inf, basePath);
                }
            } // while
        }
    }

    public class CVCGauge : CabViewControl
    {
        public Rectangle Area = new Rectangle();
        public int ZeroPos = 0;
        public int Orientation = 0;
        public int Direction = 0;

        public CVCGauge(STFReader inf, string basePath)
        {
            inf.VerifyStartOfBlock();

            while (!inf.EndOfBlock())
            {
                string token = inf.ReadToken();
                if (string.Compare(token, "Area", true) == 0)
                {
                    inf.VerifyStartOfBlock();
                    int x = inf.ReadInt();
                    int y = inf.ReadInt();
                    int width = inf.ReadInt();
                    int height = inf.ReadInt();
                    Area = new Rectangle(x, y, width, height);
                    inf.VerifyEndOfBlock();
                }
                else if (string.Compare(token, "ZeroPos", true) == 0)
                {
                    ZeroPos = inf.ReadIntBlock();
                }
                else if (string.Compare(token, "Orientation", true) == 0)
                {
                    Orientation = inf.ReadIntBlock();
                }
                else if (string.Compare(token, "DirIncrease", true) == 0)
                {
                    Direction = inf.ReadIntBlock();
                }
                else
                {
                    base.Parse(token, inf, basePath);
                }
            } // while
        }
    }

    public abstract class CVCWithFrames : CabViewControl
    {
        public int FramesCount = 0;
        public int FramesX = 0;
        public int FramesY = 0;

        public List<double> Values = new List<double>();
    }

    public class CVCDigital : CabViewControl
    {
        public CVCDigital(STFReader inf, string basePath)
        {
            inf.VerifyStartOfBlock();

            while (!inf.EndOfBlock())
            {
                string token = inf.ReadToken();
                base.Parse(token, inf, basePath);
            }
        }
    }

    public class CVCDiscrete : CVCWithFrames
    {
        public List<int> Positions = new List<int>();

        private int _PositionsRead = 0;
        private int _ValuesRead = 0;

        public CVCDiscrete(STFReader inf, string basePath)
        {
            inf.VerifyStartOfBlock();

            while (!inf.EndOfBlock())
            {
                string token = inf.ReadToken();
                if (string.Compare(token, "NumFrames", true) == 0)
                {
                    inf.VerifyStartOfBlock();
                    FramesCount = inf.ReadInt();
                    FramesX = inf.ReadInt();
                    FramesY = inf.ReadInt();
                    inf.VerifyEndOfBlock();
                }
                else if (string.Compare(token, "NumPositions", true) == 0)
                {
                    inf.VerifyStartOfBlock();

                    // If Positions are not filled before by Values
                    bool shouldFill = Positions.Count == 0;

                    // Number of Positions - Ignore it
                    int p = inf.ReadInt();
                    while (!inf.EndOfBlock())
                    {
                        p = inf.ReadInt();

                        // If Positions are not filled before by Values
                        if (shouldFill)
                            Positions.Add(p);
                    }
                }
                else if (string.Compare(token, "NumValues", true) == 0)
                {
                    inf.VerifyStartOfBlock();
                    
                    // Number of Values - ignore it
                    double v = inf.ReadDouble();

                    while (!inf.EndOfBlock())
                    {
                        v = inf.ReadDouble();
                        // If the Positions are less than expected add new Position(s)
                        while (Positions.Count <= _ValuesRead)
                        {
                            Positions.Add(_ValuesRead);
                            _PositionsRead++;
                        }

                        // Avoid later repositioning, put every value to its Position
                        // But before resize Values if needed
                        while (Values.Count <= Positions[_ValuesRead])
                        {
                            Values.Add(0);
                        }
                        // Avoid later repositioning, put every value to its Position
                        Values[Positions[_ValuesRead]] = v;
                        _ValuesRead++;
                    }
                }
                else
                {
                    base.Parse(token, inf, basePath);
                }
            } // while

            // If no ACE, just don't need any fixup
            // Because Values are tied to the image Frame to be shown
            if (string.IsNullOrEmpty(ACEFile))
                return;

            // Now, we have an ACE.

            // If read any Values, or the control requires Values to control
            //     The twostate, tristate, signal displays are not in these
            // Need check the Values collection for validity
            if (_ValuesRead > 0 || ControlStyle == CABViewControlStyles.SPRUNG || ControlStyle == CABViewControlStyles.NOT_SPRUNG)
            {
                // Check max number of Frames
                if (FramesCount == 0)
                {
                    // Check valid Frame information
                    if (FramesX == 0 || FramesY == 0)
                    {
                        // Give up, it won't work
                        // Because later we won't know how to display frames from that
                        Trace.TraceError(string.Format("Invalid Frames information given for ACE {0} in file {1}.", ACEFile, inf.FileName));

                        ACEFile = "";
                        return;
                    }

                    // Valid frames info, set FramesCount
                    FramesCount = FramesX * FramesY;
                }

                // Now we have an ACE and Frames for it.

                // Fixup Positions and Values collections first

                // If the read Positions and Values are not match
                // Or we didn't read Values but have Frames to draw
                // Do not test if FramesCount equals Values count, we trust in the creator -
                //     maybe did not want to display all Frames
                // (If there are more Values than Frames it will checked at draw time)
                // Need to fix the whole Values
                if (Positions.Count != _ValuesRead || (FramesCount > 0 && Values.Count == 0))
                {
                    // Clear existing
                    Positions.Clear();
                    Values.Clear();

                    // Add the two sure positions, the two ends
                    Positions.Add(0);
                    // We will need the FramesCount later!
                    // We use Positions only here
                    Positions.Add(FramesCount);

                    // Fill empty Values
                    for (int i = 0; i < FramesCount; i++)
                        Values.Add(0);
                    Values[0] = MinValue;

                    Values.Add(MaxValue);
                }
                // The Positions, Values are correct
                else
                {
                    // Check if read Values at all
                    if (Values.Count > 0)
                        // Set Min for sure
                        Values[0] = MinValue;
                    else
                        Values.Add(MinValue);

                    // Fill empty Values
                    for (int i = Values.Count; i < FramesCount; i++)
                        Values.Add(0);

                    // Add the maximums to the end, the Value will be removed
                    // We use Positions only here
                    Values.Add(MaxValue);
                    Positions.Add(FramesCount);
                }

                // OK, we have a valid size of Positions and Values

                // Now it is the time for checking holes in the given data
                if (Positions.Count < FramesCount - 1)
                {
                    int j = 1;
                    int p = 0;
                    // Skip the 0 element, that is the default MinValue
                    for (int i = 1; i < Positions.Count; i++)
                    {
                        // Found a hole
                        if (Positions[i] != p + 1)
                        {
                            // Iterate to the next valid data and fill the hole
                            for (j = p + 1; j < Positions[i]; j++)
                            {
                                // Extrapolate into the hole
                                Values[j] = MathHelper.Lerp((float)Values[p], (float)Values[Positions[i]], (float)j / (float)Positions[i]);
                            }
                        }
                        p = Positions[i];
                    }
                }

                // Don't need the MaxValue added before, remove it
                Values.RemoveAt(FramesCount);

            } // End of Need check the Values collection for validity
        } // End of Constructor
    }

    public class CVCMultiStateDisplay : CVCWithFrames
    {

        public CVCMultiStateDisplay(STFReader inf, string basePath)
        {
            inf.VerifyStartOfBlock();

            while (!inf.EndOfBlock())
            {
                string token = inf.ReadToken();
                if (string.Compare(token, "States", true) == 0)
                {
                    inf.VerifyStartOfBlock();
                    FramesCount = inf.ReadInt();
                    FramesX = inf.ReadInt();
                    FramesY = inf.ReadInt();

                    token = inf.ReadToken();
                    while (string.Compare(token, "State", true) == 0)
                    {
                        inf.VerifyStartOfBlock();
                        while (!inf.EndOfBlock())
                        {
                            token = inf.ReadToken();
                            if (string.Compare(token, "SwitchVal", true) == 0)
                            {
                                Values.Add(inf.ReadDoubleBlock());
                            }
                            else
                            {
                                inf.SkipBlock();
                            }
                        }
                        token = inf.ReadToken();
                    }
                    //inf.VerifyEndOfBlock();

                    if (Values.Count > 0)
                    {
                        MaxValue = Values.Last();
                    }
                    
                    for (int i = Values.Count; i < FramesCount; i++)
                        Values.Add(-10000);
                }
                else
                {
                    base.Parse(token, inf, basePath);
                }
            } // while
        }
    }

    public class CVCSignal : CVCDiscrete
    {
        public CVCSignal(STFReader inf, string basePath)
            : base(inf, basePath)
        {
            FramesCount = 8;
            FramesX = 4;
            FramesY = 2;

            MinValue = 0;
            MaxValue = 1;

            Positions.Add(1);
            Values.Add(1);
        }
    }

} // namespace MSTS

