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

        public CVFFile(string filePath, string basePath)
		{
            using (STFReader stf = new STFReader(filePath, false))
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("tr_cabviewfile", ()=>{ stf.MustMatch("("); stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("position", ()=>{ Locations.Add(stf.ReadVector3Block(STFReader.UNITS.None, new Vector3())); }),
                        new STFReader.TokenProcessor("direction", ()=>{ Directions.Add(stf.ReadVector3Block(STFReader.UNITS.None, new Vector3())); }),
                        new STFReader.TokenProcessor("cabviewfile", ()=>{
                            var fileName = stf.ReadStringBlock(null);
                            var path = Path.Combine(basePath, Path.GetDirectoryName(fileName));
                            var name = Path.GetFileName(fileName);
                            TwoDViews.Add(Path.Combine(path, name));
                            NightViews.Add(Path.Combine(path, Path.Combine("NIGHT", name)));
                            LightViews.Add(Path.Combine(path, Path.Combine("CABLIGHT", name)));
                        }),
                        new STFReader.TokenProcessor("cabviewcontrols", ()=>{ CabViewControls = new CabViewControls(stf, basePath); }),
                    });}),
                });
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
        FRICTION_BRAKE,
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
        DIRECTION_DISPLAY,
        CP_HANDLE,
        PANTOGRAPH2,
        CLOCK,
        SANDING,
        ALERTER_DISPLAY,
        TRACTION_BRAKING,
        ACCELEROMETER,
        WHEELSLIP,
        FRICTION_BRAKING,
        PENALTY_APP,
        EMERGENCY_BRAKE,
        RESET,
        CAB_RADIO,
        OVERSPEED,
        SPEEDLIM_DISPLAY,
        FUEL_GAUGE,
        WHISTLE,
        REGULATOR,
        CYL_COCKS,
        BLOWER,
        STEAM_INJ1,
        STEAM_INJ2,
        DAMPERS_FRONT,
        DAMPERS_BACK,
        WATER_INJECTOR1,
        WATER_INJECTOR2,
        SMALL_EJECTOR,
        STEAM_PR,
        STEAMCHEST_PR,
        TENDER_WATER,
        BOILER_WATER,
        REVERSER_PLATE,
        STEAMHEAT_PRESSURE,
        FIREBOX,
        RPM,
        FIREHOLE
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
        PRESSED,
        ONOFF, 
        _24HOUR, 
        _12HOUR
    }

    public enum CABViewControlUnits
    {
        NONE,
        BAR,
        PSI,
        KILOPASCALS,
        KGS_PER_SQUARE_CM,
        AMPS,
        VOLTS,
        KILOVOLTS,
        KM_PER_HOUR,
        MILES_PER_HOUR, 
        METRESµSECµSEC,
        KMµHOURµHOUR, 
        KMµHOURµSEC, 
        METRESµSECµHOUR,
        NEWTONS, 
        KILO_NEWTONS,
        KILO_LBS,
        METRES_PER_SEC,
        LITRES,
        GALLONS,
        INCHES_OF_MERCURY,
        MILI_AMPS,
        RPM
    }

    public class CabViewControls : List<CabViewControl>
    {
        public CabViewControls(STFReader stf, string basepath)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(STFReader.UNITS.None, null);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("dial", ()=>{ Add(new CVCDial(stf, basepath)); }),
                new STFReader.TokenProcessor("gauge", ()=>{ Add(new CVCGauge(stf, basepath)); }),
                new STFReader.TokenProcessor("lever", ()=>{ Add(new CVCDiscrete(stf, basepath)); }),
                new STFReader.TokenProcessor("twostate", ()=>{ Add(new CVCDiscrete(stf, basepath)); }),
                new STFReader.TokenProcessor("tristate", ()=>{ Add(new CVCDiscrete(stf, basepath)); }),
                new STFReader.TokenProcessor("multistatedisplay", ()=>{ Add(new CVCMultiStateDisplay(stf, basepath)); }),
                new STFReader.TokenProcessor("cabsignaldisplay", ()=>{ Add(new CVCSignal(stf, basepath)); }), 
                new STFReader.TokenProcessor("digital", ()=>{ Add(new CVCDigital(stf, basepath)); }), 
                new STFReader.TokenProcessor("combinedcontrol", ()=>{ Add(new CVCDiscrete(stf, basepath)); }),
                new STFReader.TokenProcessor("firebox", ()=>{ Add(new CVCDiscrete(stf, basepath)); }), 
                new STFReader.TokenProcessor("digitalclock", ()=>{ Add(new CVCDigital(stf, basepath)); })
            });
            //TODO Uncomment when parsed all type
            /*
            if (count != this.Count) STFException.ReportWarning(inf, "CabViewControl count mismatch");
            */
        }
    }
    
    #region CabViewControl
    public class CabViewControl
    {
        public double PositionX = 0;
        public double PositionY = 0;
        public double Width = 0;
        public double Height = 0;

        public double MinValue = 0;
        public double MaxValue = 0;
        public double OldValue = 0;
        public string ACEFile = "";

        public CABViewControlTypes ControlType = CABViewControlTypes.NONE;
        public CABViewControlStyles ControlStyle = CABViewControlStyles.NONE;
        public CABViewControlUnits Units = CABViewControlUnits.NONE;

        protected void ParseType(STFReader stf)
        {
            stf.MustMatch("(");
            try
            {
                ControlType = (CABViewControlTypes)Enum.Parse(typeof(CABViewControlTypes), stf.ReadString());
            }
            catch(ArgumentException)
            {
                stf.StepBackOneItem();
                STFException.TraceWarning(stf, "Skipped unknown ControlType " + stf.ReadString());
                ControlType = CABViewControlTypes.NONE;
            }
            //stf.ReadItem(); // Skip repeated Class Type 
            stf.SkipRestOfBlock();
        }
        protected void ParsePosition(STFReader stf)
        {
            stf.MustMatch("(");
            //PositionX = stf.ReadInt(STFReader.UNITS.None, null);
            //PositionY = stf.ReadInt(STFReader.UNITS.None, null);
            //Width = stf.ReadInt(STFReader.UNITS.None, null);
            //Height = stf.ReadInt(STFReader.UNITS.None, null);
            PositionX = stf.ReadDouble( STFReader.UNITS.None, null );
            PositionY = stf.ReadDouble( STFReader.UNITS.None, null );
            Width = stf.ReadDouble( STFReader.UNITS.None, null );
            Height = stf.ReadDouble( STFReader.UNITS.None, null );

            // Handling middle values
            while (!stf.EndOfBlock())
            {
                STFException.TraceWarning(stf, "Ignored additional positional parameters");
                Width = Height;
                Height = stf.ReadInt(STFReader.UNITS.None, null);
            }
        }
        protected void ParseScaleRange(STFReader stf)
        {
            stf.MustMatch("(");
            MinValue = stf.ReadDouble(STFReader.UNITS.None, null);
            MaxValue = stf.ReadDouble(STFReader.UNITS.None, null);
            stf.SkipRestOfBlock();
        }
        protected void ParseGraphic(STFReader stf, string basepath)
        {
            ACEFile = Path.Combine(basepath, stf.ReadStringBlock(null));
        }
        protected void ParseStyle(STFReader stf)
        {
            stf.MustMatch("(");
            try
            {
                string sStyle = stf.ReadString();
                int checkNumeric = 0;
                if(int.TryParse(sStyle.Substring(0, 1), out checkNumeric) == true)
                {
                    sStyle = sStyle.Insert(0, "_");
                }
                ControlStyle = (CABViewControlStyles)Enum.Parse(typeof(CABViewControlStyles), sStyle);
            }
            catch (ArgumentException)
            {
                stf.StepBackOneItem();
                STFException.TraceWarning(stf, "Skipped unknown ControlStyle " + stf.ReadString());
                ControlStyle = CABViewControlStyles.NONE;
            }
            stf.SkipRestOfBlock();
        }
        protected void ParseUnits(STFReader stf)
        {
            stf.MustMatch("(");
            try
            {
                string sUnits = stf.ReadItem();
                sUnits = sUnits.Replace('/', '?');
                Units = (CABViewControlUnits)Enum.Parse(typeof(CABViewControlUnits), sUnits);
            }
            catch (ArgumentException)
            {
                stf.StepBackOneItem();
                STFException.TraceWarning(stf, "Skipped unknown ControlStyle " + stf.ReadItem());
                Units = CABViewControlUnits.NONE;
            }
            stf.SkipRestOfBlock();
        }
    }
    #endregion

    #region Dial controls
    public class CVCDial : CabViewControl
    {
        public float FromDegree = 0;
        public float ToDegree = 0;
        public float Center = 0;
        public int Direction = 0;
        
        public CVCDial(STFReader stf, string basepath)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("type", ()=>{ ParseType(stf); }),
                new STFReader.TokenProcessor("position", ()=>{ ParsePosition(stf);  }),
                new STFReader.TokenProcessor("scalerange", ()=>{ ParseScaleRange(stf); }),
                new STFReader.TokenProcessor("graphic", ()=>{ ParseGraphic(stf, basepath); }),
                new STFReader.TokenProcessor("style", ()=>{ ParseStyle(stf); }),
                new STFReader.TokenProcessor("units", ()=>{ ParseUnits(stf); }),

                new STFReader.TokenProcessor("pivot", ()=>{ Center = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("dirincrease", ()=>{ Direction = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("scalepos", ()=>{
                    stf.MustMatch("(");
                    FromDegree = stf.ReadFloat(STFReader.UNITS.None, null);
                    ToDegree = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                }),
            });
        }
    }
    #endregion

    #region Gauges
    public class CVCGauge : CabViewControl
    {
        public Rectangle Area = new Rectangle();
        public int ZeroPos = 0;
        public int Orientation = 0;
        public int Direction = 0;

        public CVCGauge(STFReader stf, string basepath)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("type", ()=>{ ParseType(stf); }),
                new STFReader.TokenProcessor("position", ()=>{ ParsePosition(stf);  }),
                new STFReader.TokenProcessor("scalerange", ()=>{ ParseScaleRange(stf); }),
                new STFReader.TokenProcessor("graphic", ()=>{ ParseGraphic(stf, basepath); }),
                new STFReader.TokenProcessor("style", ()=>{ ParseStyle(stf); }),
                new STFReader.TokenProcessor("units", ()=>{ ParseUnits(stf); }),

                new STFReader.TokenProcessor("zeropos", ()=>{ ZeroPos = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("orientation", ()=>{ Orientation = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("dirincrease", ()=>{ Direction = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("area", ()=>{ 
                    stf.MustMatch("(");
                    int x = stf.ReadInt(STFReader.UNITS.None, null);
                    int y = stf.ReadInt(STFReader.UNITS.None, null);
                    int width = stf.ReadInt(STFReader.UNITS.None, null);
                    int height = stf.ReadInt(STFReader.UNITS.None, null);
                    Area = new Rectangle(x, y, width, height);
                    stf.SkipRestOfBlock();
                }),
            });
        }
    }
    #endregion

    #region Digital controls
    public class CVCDigital : CabViewControl
    {
        public int LeadingZeros { get; set; }
        public double Accuracy { get; set; }
        public double AccuracySwitch { get; set; }
        public int Justification { get; set; }
        public color PositiveColor { get; set; }
        public color NegativeColor { get; set; }
        public color DecreaseColor { get; set; }

        public CVCDigital()
        {
        }

        public CVCDigital(STFReader stf, string basepath)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("type", ()=>{ ParseType(stf); }),
                new STFReader.TokenProcessor("position", ()=>{ ParsePosition(stf);  }),
                new STFReader.TokenProcessor("scalerange", ()=>{ ParseScaleRange(stf); }),
                new STFReader.TokenProcessor("graphic", ()=>{ ParseGraphic(stf, basepath); }),
                new STFReader.TokenProcessor("style", ()=>{ ParseStyle(stf); }),
                new STFReader.TokenProcessor("units", ()=>{ ParseUnits(stf); }),
                new STFReader.TokenProcessor("leadingzeros", ()=>{ ParseLeadingZeros(stf); }),
                new STFReader.TokenProcessor("accuracy", ()=>{ ParseAccuracy(stf); }), 
                new STFReader.TokenProcessor("accuracyswitch", ()=>{ ParseAccuracySwitch(stf); }), 
                new STFReader.TokenProcessor("justification", ()=>{ ParseJustification(stf); }),
                new STFReader.TokenProcessor("positivecolour", ()=>{ 
                    stf.MustMatch("(");
                    stf.ReadInt(STFReader.UNITS.None, 0);
                    if(stf.EndOfBlock() == false)
                    {
                        stf.ParseBlock(new STFReader.TokenProcessor[] {
                            new STFReader.TokenProcessor("controlcolour", ()=>{ PositiveColor = ParseControlColor(stf); }) });
                    }
                }),
                new STFReader.TokenProcessor("negativecolour", ()=>{
                    stf.MustMatch("(");
                    stf.ReadInt(STFReader.UNITS.None, 0);
                    if(stf.EndOfBlock() == false)
                    {
                        stf.ParseBlock(new STFReader.TokenProcessor[] {
                            new STFReader.TokenProcessor("controlcolour", ()=>{ NegativeColor = ParseControlColor(stf); }) });
                    }
                }),
                new STFReader.TokenProcessor("decreasecolour", ()=>{
                    stf.MustMatch("(");
                    stf.ReadInt(STFReader.UNITS.None, 0);
                    if(stf.EndOfBlock() == false)
                    {
                        stf.ParseBlock(new STFReader.TokenProcessor[] {
                            new STFReader.TokenProcessor("controlcolour", ()=>{ DecreaseColor = ParseControlColor(stf); }) });
                    }
                })
            });
        }

        protected virtual void ParseLeadingZeros(STFReader stf)
        {
            stf.MustMatch("(");
            LeadingZeros = stf.ReadInt(STFReader.UNITS.None, 0);
            stf.SkipRestOfBlock();
        }

        protected virtual void ParseAccuracy(STFReader stf)
        {
            stf.MustMatch("(");
            Accuracy = stf.ReadDouble(STFReader.UNITS.None, 0);
            stf.SkipRestOfBlock();
        }

        protected virtual void ParseAccuracySwitch(STFReader stf)
        {
            stf.MustMatch("(");
            AccuracySwitch = stf.ReadDouble(STFReader.UNITS.None, 0);
            stf.SkipRestOfBlock();
        }

        protected virtual void ParseJustification(STFReader stf)
        {
            stf.MustMatch("(");
            Justification = stf.ReadInt(STFReader.UNITS.None, 3);
            stf.SkipRestOfBlock();
        }

        protected virtual color ParseControlColor(STFReader stf)
        {
            stf.MustMatch("(");
            color colour = new color { A = 255, R = stf.ReadInt(STFReader.UNITS.None, 0), G = stf.ReadInt(STFReader.UNITS.None, 0), B = stf.ReadInt(STFReader.UNITS.None, 0) };
            stf.SkipRestOfBlock();
            return colour;
        }
    }

    public class CVCDigitalClock : CVCDigital
    {

        public CVCDigitalClock(STFReader stf, string basepath)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("type", ()=>{ ParseType(stf); }),
                new STFReader.TokenProcessor("position", ()=>{ ParsePosition(stf);  }),
                new STFReader.TokenProcessor("style", ()=>{ ParseStyle(stf); }),
                new STFReader.TokenProcessor("accuracy", ()=>{ ParseAccuracy(stf); }), 
                new STFReader.TokenProcessor("controlcolour", ()=>{ PositiveColor = ParseControlColor(stf); })
            });
        }

        
    }
    #endregion

    #region Frames controls
    public abstract class CVCWithFrames : CabViewControl
    {
        private List<double> values = new List<double>();

        public int FramesCount { get; set; }
        public int FramesX { get; set; }
        public int FramesY { get; set; }

        public List<double> Values 
        {
            get
            {
                return values;
            }
        }
    }

    public class CVCDiscrete : CVCWithFrames
    {
        public List<int> Positions = new List<int>();

        private int _PositionsRead = 0;
        private int _ValuesRead = 0;

        public CVCDiscrete(STFReader stf, string basepath)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("type", ()=>{ ParseType(stf); }),
                new STFReader.TokenProcessor("position", ()=>{ ParsePosition(stf);  }),
                new STFReader.TokenProcessor("scalerange", ()=>{ ParseScaleRange(stf); }),
                new STFReader.TokenProcessor("graphic", ()=>{ ParseGraphic(stf, basepath); }),
                new STFReader.TokenProcessor("style", ()=>{ ParseStyle(stf); }),
                new STFReader.TokenProcessor("units", ()=>{ ParseUnits(stf); }),

                new STFReader.TokenProcessor("numframes", ()=>{
                    stf.MustMatch("(");
                    FramesCount = stf.ReadInt(STFReader.UNITS.None, null);
                    FramesX = stf.ReadInt(STFReader.UNITS.None, null);
                    FramesY = stf.ReadInt(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("numpositions", ()=>{
                    stf.MustMatch("(");
                    // If Positions are not filled before by Values
                    bool shouldFill = (Positions.Count == 0);
                    stf.ReadInt(STFReader.UNITS.None, null); // Number of Positions - Ignore it
                    while (!stf.EndOfBlock())
                    {
                        int p = stf.ReadInt(STFReader.UNITS.None, null);
                        // If Positions are not filled before by Values
                        if (shouldFill) Positions.Add(p);
                    }
                }),
                new STFReader.TokenProcessor("numvalues", ()=>{
                    stf.MustMatch("(");
                    stf.ReadDouble(STFReader.UNITS.None, null); // Number of Values - ignore it
                    while (!stf.EndOfBlock())
                    {
                        double v = stf.ReadDouble(STFReader.UNITS.None, null);
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
                }),
            });

            // If no ACE, just don't need any fixup
            // Because Values are tied to the image Frame to be shown
            if (string.IsNullOrEmpty(ACEFile)) return;

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
                        Trace.TraceWarning("Invalid Frames information given for ACE {0} in {1}", ACEFile, stf.FileName);
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
    #endregion

    #region Multistate Display Controls
    public class CVCMultiStateDisplay : CVCWithFrames
    {

        public CVCMultiStateDisplay(STFReader stf, string basepath)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("type", ()=>{ ParseType(stf); }),
                new STFReader.TokenProcessor("position", ()=>{ ParsePosition(stf);  }),
                new STFReader.TokenProcessor("scalerange", ()=>{ ParseScaleRange(stf); }),
                new STFReader.TokenProcessor("graphic", ()=>{ ParseGraphic(stf, basepath); }),
                new STFReader.TokenProcessor("style", ()=>{ ParseStyle(stf); }),
                new STFReader.TokenProcessor("units", ()=>{ ParseUnits(stf); }),

                new STFReader.TokenProcessor("states", ()=>{
                    stf.MustMatch("(");
                    FramesCount = stf.ReadInt(STFReader.UNITS.None, null);
                    FramesX = stf.ReadInt(STFReader.UNITS.None, null);
                    FramesY = stf.ReadInt(STFReader.UNITS.None, null);
                    stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("state", ()=>{ stf.MustMatch("("); stf.ParseBlock( new STFReader.TokenProcessor[] {
                            new STFReader.TokenProcessor("switchval", ()=>{ Values.Add(stf.ReadDoubleBlock(STFReader.UNITS.None, null)); }),
                        });}),
                    });
                    if (Values.Count > 0) MaxValue = Values.Last();
                    for (int i = Values.Count; i < FramesCount; i++)
                        Values.Add(-10000);
                }),
            });
        }
    }
    #endregion

    #region other controls
    public class CVCSignal : CVCDiscrete
    {
        public CVCSignal(STFReader inf, string basepath)
            : base(inf, basepath)
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
    #endregion
} // namespace MSTS

