/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MSTSMath;

namespace MSTS
{
	// GLOBAL TSECTION DAT

	public class SectionCurve
	{
		public SectionCurve()
		{
			Radius = 0;
			Angle = 0;
		}
		public SectionCurve( STFReader f )
		{
			f.MustMatch("(");
            Radius = f.ReadFloat(STFReader.UNITS.None, null);
            Angle = f.ReadFloat(STFReader.UNITS.None, null);
            f.SkipRestOfBlock();
		}
		public float Radius;	// meters
		public float Angle;	// degrees
	}

	public class SectionSize
	{
		public SectionSize()
		{
			Width = 1.5F;
			Length = 0;
		}
		public SectionSize( STFReader f )
		{
			f.MustMatch("(");
            Width = f.ReadFloat(STFReader.UNITS.Distance, null);
            Length = f.ReadFloat(STFReader.UNITS.Distance, null);
            f.SkipRestOfBlock();
		}
		public float Width;
		public float Length;
	}
	
	public class TrackSection
	{
		public TrackSection()
		{
		}
		
		public TrackSection( STFReader f )
		{
			f.MustMatch("(");
            SectionIndex = f.ReadUInt(STFReader.UNITS.None, null);
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "sectionsize": SectionSize = new SectionSize(f); break;
                    case "sectioncurve": SectionCurve = new SectionCurve(f); break;
                    case "(": f.SkipRestOfBlock(); break;
                }
			//if( SectionSize == null )
			//	throw( new STFError( f, "Missing SectionSize" ) );
			//  note- default TSECTION.DAT does have some missing sections

		}
		public uint SectionIndex;
		public SectionSize SectionSize;
		public SectionCurve SectionCurve;
	}
	
	public class RouteTrackSection: TrackSection
	{
		public RouteTrackSection( STFReader f )
		{
			f.MustMatch("(");
			f.MustMatch( "SectionCurve" );
			f.MustMatch("(");
			f.ReadItem(); // 0 or 1
			f.SkipRestOfBlock();
            SectionIndex = f.ReadUInt(STFReader.UNITS.None, null);
			SectionSize = new SectionSize();
            float a = f.ReadFloat(STFReader.UNITS.Distance, null);
            float b = f.ReadFloat(STFReader.UNITS.None, null);
			if( b == 0 )
				// Its straight
			{
				SectionSize.Length = a;
			}
			else
				// its curved
			{
				SectionCurve = new SectionCurve();
				SectionCurve.Radius = b;
				SectionCurve.Angle = (float)M.Degrees( a );
			}
			f.SkipRestOfBlock();

		}
	}

	public class TrackSections: Dictionary<uint, TrackSection>
	{
		public TrackSections( STFReader f )
		{
			f.MustMatch("(");
            MaxSectionIndex = f.ReadUInt(STFReader.UNITS.None, null);
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "tracksection": AddSection(f, new TrackSection(f)); break;
                    case "(": f.SkipRestOfBlock(); break;
                }
		}

		public void AddRouteTrackSections( STFReader f )
		{
			f.MustMatch("(");
            MaxSectionIndex = f.ReadUInt(STFReader.UNITS.None, null);
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "tracksection": AddSection(f, new RouteTrackSection(f)); break;
                    case "(": f.SkipRestOfBlock(); break;
                }
		}
        private void AddSection(STFReader f, TrackSection section)
        {
            if (ContainsKey(section.SectionIndex))
            {
                STFException.TraceWarning(f, "Duplicate SectionIndex of " + section.SectionIndex);
            }
            this[section.SectionIndex] = section;
        }

        public static int MissingTrackSectionWarnings = 0;

		public TrackSection Get( uint targetSectionIndex )
		{
			if (ContainsKey(targetSectionIndex))
				return this[targetSectionIndex];
			if (MissingTrackSectionWarnings++ < 5)
				Trace.TraceWarning("TDB references track section not listed in global or dynamic TSECTION.DAT: " + targetSectionIndex.ToString());
            return null;
		}
		public uint MaxSectionIndex;
	}
	public class SectionIdx
	{
		public SectionIdx( STFReader f )
		{
			f.MustMatch("(");
            NoSections = f.ReadUInt(STFReader.UNITS.None, null);
            X = f.ReadDouble(STFReader.UNITS.None, null);
            Y = f.ReadDouble(STFReader.UNITS.None, null);
            Z = f.ReadDouble(STFReader.UNITS.None, null);
            A = f.ReadDouble(STFReader.UNITS.None, null);
			TrackSections = new uint[ NoSections ]; 
			for( int i = 0; i < NoSections; ++i )  
			{
       			string token = f.ReadItem();
                if( token == ")" ) 
                {
                    STFException.TraceError( f, "Missing track section" );
                    return;   // there are many TSECTION.DAT's with missing sections so we will accept this error
                }
                if (!uint.TryParse(token, out TrackSections[i]))
                    STFException.TraceWarning(f, "Invalid Track Section " + token);
			}
			f.SkipRestOfBlock();
		}
		public uint NoSections;
		public double X,Y,Z;  // Offset
		public double A;  // Angular offset 
		public uint[] TrackSections;
	}
	
	public class TrackShape
	{
		public TrackShape( STFReader f )
		{
			f.MustMatch("(");
            ShapeIndex = f.ReadUInt(STFReader.UNITS.None, null);
			int nextPath = 0;
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "filename": FileName = f.ReadItemBlock(null); break;
                    case "numpaths":
                        NumPaths = f.ReadUIntBlock(STFReader.UNITS.None, null);
                        SectionIdxs = new SectionIdx[NumPaths];
                        break;
                    case "mainroute": MainRoute = f.ReadUIntBlock(STFReader.UNITS.None, null); break;
                    case "clearancedist": ClearanceDistance = f.ReadDoubleBlock(STFReader.UNITS.Distance, null); break;
                    case "sectionidx": SectionIdxs[nextPath++] = new SectionIdx(f); break;
                    case "tunnelshape": TunnelShape = f.ReadBoolBlock(true); break;
                    case "roadshape": RoadShape = f.ReadBoolBlock(true); break;
                    case "(": f.SkipRestOfBlock(); break;
                }
			// TODO - this was removed since TrackShape( 183 ) is blank
			//if( FileName == null )	throw( new STFError( f, "Missing FileName" ) );
			//if( SectionIdxs == null )	throw( new STFError( f, "Missing SectionIdxs" ) );
			//if( NumPaths == 0 ) throw( new STFError( f, "No Paths in TrackShape" ) );
		}
		public uint ShapeIndex;
		public string FileName;
		public uint NumPaths = 0;
		public uint MainRoute = 0;
		public double ClearanceDistance = 0.0;
		public SectionIdx[] SectionIdxs;
		public bool TunnelShape = false;
		public bool RoadShape = false;
	}
	
	public class TrackShapes: ArrayList
	{
		public TrackShapes( STFReader f )
		{
            f.MustMatch("(");
            MaxShapeIndex = f.ReadUInt(STFReader.UNITS.None, null);
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "trackshape": Add(new TrackShape(f)); break;
                    case "(": f.SkipRestOfBlock(); break;
                }
		}
		public TrackShape Get( uint targetShapeIndex )
		{
			// TODO - do this better - linear search is slow
			for( int i = 0; i < this.Count; ++i )
				if( ((TrackShape)this[i]).ShapeIndex == targetShapeIndex )
				{
					return (TrackShape)this[i];
				}
			throw new InvalidDataException("ShapeIndex not found");
		}
		public uint MaxShapeIndex;
	}

	public class TSectionDatFile
	{
		public void AddRouteTSectionDatFile( string pathNameExt )
		{
            using (STFReader f = new STFReader(pathNameExt, false))
            {
                if (f.SIMISsignature != "SIMISA@@@@@@@@@@JINX0T0t______")
                {
                    Trace.TraceWarning("Ignoring invalid TSECTION.DAT in route folder.");
                    return;
                }
                while (!f.EOF)
                    switch (f.ReadItem().ToLower())
                    {
                        case "tracksections": TrackSections.AddRouteTrackSections(f); break;
                        // todo read in SectionIdx part of RouteTSectionDat
                        case "(": f.SkipRestOfBlock(); break;
                    }
            }
		}
        public TSectionDatFile(string filePath)
        {
            using (STFReader f = new STFReader(filePath, false))
            {
                while (!f.EOF)
                    switch (f.ReadItem().ToLower())
                    {
                        case "tracksections": TrackSections = new TrackSections(f); break;
                        case "trackshapes": TrackShapes = new TrackShapes(f); break;
                        case "(": f.SkipRestOfBlock(); break;
                    }
                //TODO This should be changed to STFException.TraceError() with defaults values created
                if (TrackSections == null) throw new STFException(f, "Missing TrackSections");
                if (TrackShapes == null) throw new STFException(f, "Missing TrackShapes");
            }
        }
		public TrackSections TrackSections;
		public TrackShapes TrackShapes;
	}


}
