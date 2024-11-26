﻿// COPYRIGHT 2009, 2010, 2011, 2013 by the Open Rails project.
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

using System.Collections.Generic;
using System.Diagnostics;
using Orts.Parsers.Msts;
using System.IO;
using ORTS.Common;
using System.Linq;

namespace Orts.Formats.Msts
{
    public class RouteFile
    {
        public RouteFile(string filename)
        {
            string dir = Path.GetDirectoryName(filename);
            string file = Path.GetFileName(filename);
            string orFile = dir + @"\openrails\" + file;
            if (File.Exists(orFile))
                filename = orFile;
            try
            {
                using (STFReader stf = new STFReader(filename, false))
                {
                    stf.ParseFile(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("tr_routefile", ()=>{ Tr_RouteFile = new Tr_RouteFile(stf); }),
                        new STFReader.TokenProcessor("_OpenRails", ()=>{ ORTRKData = new ORTRKData(stf); }),
                    });
                    //TODO This should be changed to STFException.TraceError() with defaults values created
                    if (Tr_RouteFile == null) throw new STFException(stf, "Missing Tr_RouteFile");
                }
            }
            finally
            {
                if (ORTRKData == null)
                    ORTRKData = new ORTRKData();
            }
        }
        public Tr_RouteFile Tr_RouteFile;
        public ORTRKData ORTRKData;
    }

    public class ORTRKData
    {
        public float MaxViewingDistance = float.MaxValue;  // disables local route override

        public ORTRKData()
        {
        }

        public ORTRKData(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("ortsmaxviewingdistance", ()=>{ MaxViewingDistance = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
            });
        }
    }

    public class Tr_RouteFile
    {
        public Tr_RouteFile(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("routeid", ()=>{ RouteID = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("filename", ()=>{ FileName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("description", ()=>{ Description = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("maxlinevoltage", ()=>{ MaxLineVoltage = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("routestart", ()=>{ if (RouteStart == null) RouteStart = new RouteStart(stf); }),
                new STFReader.TokenProcessor("environment", ()=>{ Environment = new TRKEnvironment(stf); }),
                new STFReader.TokenProcessor("milepostunitskilometers", ()=>{ MilepostUnitsMetric = true; }),
				new STFReader.TokenProcessor("electrified", ()=>{ Electrified = stf.ReadBoolBlock(false); }),
                new STFReader.TokenProcessor("overheadwireheight", ()=>{ OverheadWireHeight = stf.ReadFloatBlock(STFReader.UNITS.Distance, 6.0f);}),
 				new STFReader.TokenProcessor("speedlimit", ()=>{ SpeedLimit = stf.ReadFloatBlock(STFReader.UNITS.Speed, 500.0f); }),
                new STFReader.TokenProcessor("defaultcrossingsms", ()=>{ DefaultCrossingSMS = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("defaultcoaltowersms", ()=>{ DefaultCoalTowerSMS = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("defaultdieseltowersms", ()=>{ DefaultDieselTowerSMS = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("defaultwatertowersms", ()=>{ DefaultWaterTowerSMS = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("defaultsignalsms", ()=>{ DefaultSignalSMS = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("temprestrictedspeed", ()=>{ TempRestrictedSpeed = stf.ReadFloatBlock(STFReader.UNITS.Speed, -1f); }),
                // values for tunnel operation
                new STFReader.TokenProcessor("ortssingletunnelarea", ()=>{ SingleTunnelAreaM2 = stf.ReadFloatBlock(STFReader.UNITS.AreaDefaultFT2, null); }),
                new STFReader.TokenProcessor("ortssingletunnelperimeter", ()=>{ SingleTunnelPerimeterM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                new STFReader.TokenProcessor("ortsdoubletunnelarea", ()=>{ DoubleTunnelAreaM2 = stf.ReadFloatBlock(STFReader.UNITS.AreaDefaultFT2, null); }),
                new STFReader.TokenProcessor("ortsdoubletunnelperimeter", ()=>{ DoubleTunnelPerimeterM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                // if > 0 indicates distance from track without forest trees
				new STFReader.TokenProcessor("ortsuserpreferenceforestcleardistance", ()=>{ ForestClearDistance = stf.ReadFloatBlock(STFReader.UNITS.Distance, 0); }),
                // if true removes forest trees also from roads
				new STFReader.TokenProcessor("ortsuserpreferenceremoveforesttreesfromroads", ()=>{ RemoveForestTreesFromRoads = stf.ReadBoolBlock(false); }),
                // values for superelevation
                new STFReader.TokenProcessor("ortstracksuperelevation", ()=>{ SuperElevationHgtpRadiusM = new Interpolator(stf); }),
                // New superelevation standard, will overwrite ORTSTrackSuperElevation
                new STFReader.TokenProcessor("ortssuperelevation", ()=>{ SuperElevation.Add(new SuperElevationStandard(stf)); }),
                new STFReader.TokenProcessor("trackgauge", ()=>{ RouteGaugeM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                new STFReader.TokenProcessor("ortsforcesuperelevation", ()=>{ SuperElevationMode = stf.ReadBoolBlock(false) ? 1 : 0; }),
                // images
                new STFReader.TokenProcessor("graphic", ()=>{ Thumbnail = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("loadingscreen", ()=>{ LoadingScreen = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("ortsloadingscreenwide", ()=>{ LoadingScreenWide = stf.ReadStringBlock(null); }),
                 // values for OHLE
                new STFReader.TokenProcessor("ortsdoublewireenabled", ()=>{ DoubleWireEnabled = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("ortsdoublewireheight", ()=>{ DoubleWireHeight = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                new STFReader.TokenProcessor("ortstriphaseenabled", ()=>{ TriphaseEnabled = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("ortstriphasewidth", ()=>{ TriphaseWidth = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                // default sms file for turntables and transfertables
                new STFReader.TokenProcessor("ortsdefaultturntablesms", ()=>{ DefaultTurntableSMS = stf.ReadStringBlock(null); }),
                // sms file number in Ttype.dat when train over switch
                new STFReader.TokenProcessor("ortsswitchsmsnumber", ()=>{ SwitchSMSNumber = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("ortscurvesmsnumber", ()=>{ CurveSMSNumber = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("ortscurveswitchsmsnumber", ()=>{ CurveSwitchSMSNumber = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("ortsopendoorsinaitrains", ()=>{ OpenDoorsInAITrains = stf.ReadBoolBlock(false); }),

           });
            //TODO This should be changed to STFException.TraceError() with defaults values created
            if (RouteID == null) throw new STFException(stf, "Missing RouteID");
            if (Name == null) throw new STFException(stf, "Missing Name");
            if (Description == null) throw new STFException(stf, "Missing Description");
            if (RouteStart == null) throw new STFException(stf, "Missing RouteStart");
            if (ForestClearDistance == 0 && RemoveForestTreesFromRoads) Trace.TraceWarning("You must define also ORTSUserPreferenceForestClearDistance to avoid trees on roads");
            if (SuperElevation.Count <= 0)
            {
                // No superelevation standard defined, create the default one
                if (SuperElevationHgtpRadiusM != null)
                    SuperElevation.Add(new SuperElevationStandard(SuperElevationHgtpRadiusM, MilepostUnitsMetric, SpeedLimit > 45.0f));
                else
                    SuperElevation.Add(new SuperElevationStandard(MilepostUnitsMetric, SpeedLimit > 45.0f));
            }
        }

        public string RouteID;  // ie JAPAN1  - used for TRK file and route folder name
        public string FileName; // ie OdakyuSE - used for MKR,RDB,REF,RIT,TDB,TIT
        public string Name;
        public string Description;
        public RouteStart RouteStart;
        public TRKEnvironment Environment;
        public bool MilepostUnitsMetric;
        public double MaxLineVoltage;
		public bool Electrified = true;
		public double OverheadWireHeight = 6.0;
		public double SpeedLimit = 500.0f; //global speed limit m/s.
        public string DefaultCrossingSMS;
        public string DefaultCoalTowerSMS;
        public string DefaultDieselTowerSMS;
        public string DefaultWaterTowerSMS;
        public string DefaultSignalSMS;
		public float TempRestrictedSpeed = -1f;
        public float RouteGaugeM; // Track gauge used by this route in meters. FUTURE: Better handling of track gauge to support routes with mixed gauges
        public int SuperElevationMode = -1; // -1: use simulator setting, 0: force disable visual superelevation, 1: force enable visual superelevation
        public Interpolator SuperElevationHgtpRadiusM; // Superelevation of tracks as a function of radius, deprecated
        public List<SuperElevationStandard> SuperElevation = new List<SuperElevationStandard>();

        // Values for calculating Tunnel Resistance - will override default values.
        public float SingleTunnelAreaM2; 
        public float SingleTunnelPerimeterM;
        public float DoubleTunnelAreaM2;
        public float DoubleTunnelPerimeterM; 

        public float ForestClearDistance = 0;
        public bool RemoveForestTreesFromRoads = false;

        // images
        public string Thumbnail;
        public string LoadingScreen;
        public string LoadingScreenWide;

        // Values for OHLE
        public string DoubleWireEnabled;
        public float DoubleWireHeight;
        public string TriphaseEnabled;
        public float TriphaseWidth;

        public string DefaultTurntableSMS;
        public bool ? OpenDoorsInAITrains; // true if option active

        public int SwitchSMSNumber = -1; // defines the number of the switch SMS files in file ttypedat
        public int CurveSMSNumber = -1; // defines the number of the curve SMS files in file ttype.dat
        public int CurveSwitchSMSNumber = -1; // defines the number of the curve-switch SMS files in file ttype.dat

    }


    public class RouteStart
    {
        public RouteStart(STFReader stf)
        {
            stf.MustMatch("(");
            WX = stf.ReadDouble(null);   // tilex
            WZ = stf.ReadDouble(null);   // tilez
            X = stf.ReadDouble(null);
            Z = stf.ReadDouble(null);
            stf.SkipRestOfBlock();
        }
        public double WX, WZ, X, Z;
    }

    public class TRKEnvironment
    {
        Dictionary<string, string> ENVFileNames = new Dictionary<string, string>();

        public TRKEnvironment(STFReader stf)
        {
            stf.MustMatch("(");
            for (int i = 0; i < 12; ++i)
            {
                var envfilekey = stf.ReadString();
                var envfile = stf.ReadStringBlock(null);
                ENVFileNames.Add(envfilekey, envfile);
                //                Trace.TraceInformation("Environments array key {0} equals file name {1}", envfilekey, envfile);
            }
            stf.SkipRestOfBlock();
        }

        public string ENVFileName(SeasonType seasonType, WeatherType weatherType)
        {
            //int index = (int)seasonType * 3 + (int)weatherType;
            //return ENVFileNames[index];
            var envfilekey = seasonType.ToString() + weatherType.ToString();
            var envfile = ENVFileNames[envfilekey];
            //            Trace.TraceInformation("Selected Environment file is {1}", envfilekey, envfile);
            return envfile;
        }
    }

    public class SuperElevationStandard
    {
        public float MaxFreightUnderbalanceM = float.PositiveInfinity; // Limit to be set elsewhere
        public float MaxPaxUnderbalanceM = float.PositiveInfinity; // Limit to be set elsewhere
        public float MinCantM = 0.010f; // Default 10 mm (0.5 inches on imperial routes)
        public float MaxCantM = 0.180f; // Default limit on superelevation is 180 mm (5 inches on imperial routes)
        public float MinSpeedMpS = MpS.FromKpH(25.5f); // Default 25 kmh (15 mph on imperial routes)
        public float MaxSpeedMpS = float.PositiveInfinity; // Default unlimited
        public float PrecisionM = 0.005f; // Default 5 mm (0.25 inches on imperial routes)
        public float RunoffSlope = 0.003f; // Maximum rate of change of superelevation per track length, default 0.3%
        public float RunoffSpeedMpS = 0.055f; // Maximum rate of change of superelevation per second, default 55 mm / sec (1.5 inches / sec on imperial routes)
        public bool UseLegacyCalculation = true; // Should ORTSTrackSuperElevation be used for superelevation calculations?
        public bool DefaultStandard = true; // Is this standard a default (auto-generated) one?

        // Initialize new instance with default values (default metric values)
        public SuperElevationStandard(bool metric = true, bool highSpeed = false)
        {
            if (metric)
            {
                // Set underbalance to millimeter values for metric routes
                if (highSpeed)
                {
                    RunoffSpeedMpS = 0.080f; // 80 mm / sec for higher speed routes
                    MaxFreightUnderbalanceM = 0.110f; // 110 mm for higher speed routes
                    MaxPaxUnderbalanceM = 0.150f; // 150 mm for higher speed routes
                }
                else
                {
                    MaxFreightUnderbalanceM = 0.100f; // Default 100 mm
                    MaxPaxUnderbalanceM = 0.130f; // Default 130 mm
                }
                // Other parameters are already metric by default

            }
            else
            {
                // Set values in imperial units
                MinCantM = Me.FromIn(0.5f);
                MaxCantM = Me.FromIn(6.0f);
                MinSpeedMpS = MpS.FromMpH(15.5f);
                PrecisionM = Me.FromIn(0.25f);

                if (highSpeed)
                {
                    RunoffSpeedMpS = MpS.FromMpH(0.1136f); // 2.0 inches for higher speed routes
                    MaxFreightUnderbalanceM = Me.FromIn(3.0f); // 3 inches for higher speed routes
                    MaxPaxUnderbalanceM = Me.FromIn(5.0f); // 5 inches for higher speed routes
                }
                else
                {
                    RunoffSpeedMpS = MpS.FromMpH(0.0852f); // Default 1.5 inches per second
                    MaxFreightUnderbalanceM = Me.FromIn(2.0f); // Default 2 inches
                    MaxPaxUnderbalanceM = Me.FromIn(3.0f); // Default 3 inches
                }
            }
        }

        // Initialize new instance from superelevation interpolator
        // Interpolator X values should be curve radius, Y values amount of superelevation in meters
        public SuperElevationStandard(Interpolator elevTable, bool metric = true, bool highSpeed = false)
        {
            MinCantM = elevTable.Y.Min();
            MaxCantM = elevTable.Y.Max();

            MinSpeedMpS = 0.0f;

            if (metric)
            {
                if (highSpeed)
                    RunoffSpeedMpS = 0.080f; // 80 mm / sec of runoff allows for shorter build up on high speed routes
            }
            else
            {
                // Some extra data still required, use imperial units if the route uses it
                PrecisionM = Me.FromIn(0.25f);
                if (highSpeed)
                    RunoffSpeedMpS = MpS.FromMpH(0.1136f); // 2.0 inches per second of runoff allows for shorter build up on high speed routes
                else
                    RunoffSpeedMpS = MpS.FromMpH(0.0852f); // 1.5 inches per second normal
            }
        }

        // Read data for new instance using STF format
        public SuperElevationStandard(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("maxfreightunderbalance", () => { MaxFreightUnderbalanceM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                new STFReader.TokenProcessor("maxpassengerunderbalance", () => { MaxPaxUnderbalanceM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                new STFReader.TokenProcessor("minimumcant", () => { MinCantM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                new STFReader.TokenProcessor("maximumcant", () => { MaxCantM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                new STFReader.TokenProcessor("minimumspeed", () => { MinSpeedMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, null); }),
                new STFReader.TokenProcessor("maximumspeed", () => { MaxSpeedMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, null); }),
                new STFReader.TokenProcessor("precision", () => { PrecisionM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                new STFReader.TokenProcessor("maxrunoffslope", () => { RunoffSlope = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("maxrunoffspeed", () => { RunoffSpeedMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, null); }),
            });

            DefaultStandard = false;

            // Disable legacy superelevation calculations if sufficient data is given
            if (MaxFreightUnderbalanceM <= 10.0f || MaxPaxUnderbalanceM <= 10.0f)
                UseLegacyCalculation = false;
            // Sanity check values of underbalance
            if (MaxFreightUnderbalanceM > 10.0f)
            {
                if (MaxPaxUnderbalanceM > 10.0f)
                {
                    // Neither underbalance has been defined, assume some defaults
                    MaxFreightUnderbalanceM = 0.05f; // Default 5 cm ~ 2 inches
                    MaxPaxUnderbalanceM = 0.075f; // Default 7.5 cm ~ 3 inches
                }
                else // Only passenger was defined
                    MaxFreightUnderbalanceM = MaxPaxUnderbalanceM;
            }
            else if (MaxPaxUnderbalanceM > 10.0f) // Only freight was defined
                MaxPaxUnderbalanceM = MaxFreightUnderbalanceM;
        }
    }

}
