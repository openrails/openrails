/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
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
			f.VerifyStartOfBlock();
			Radius = f.ReadFloat();
			Angle = f.ReadFloat();
			while( f.ReadToken() != ")" );  // MSTS seems to ignore extra params
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
			f.VerifyStartOfBlock();
			Width = f.ReadFloat();
			Length = f.ReadFloat();
			while( f.ReadToken() != ")" );  // MSTS seems to ignore extra params
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
			f.VerifyStartOfBlock();
			SectionIndex = f.ReadUInt();
			string token = f.ReadToken();
			while( token != ")" )
			{
				if( token == "" ) throw ( new STFError( f, "Missing )" ) );
				else if( 0 == String.Compare( token,"SectionSize",true ) )  SectionSize = new SectionSize( f );
				else if( 0 == String.Compare( token,"SectionCurve", true ) ) SectionCurve = new SectionCurve( f );
				else f.SkipBlock();
				token = f.ReadToken();
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
			f.VerifyStartOfBlock();
			f.MustMatch( "SectionCurve" );
			f.VerifyStartOfBlock();
			f.ReadToken(); // 0 or 1
			f.VerifyEndOfBlock();
			SectionIndex = f.ReadUInt();
			SectionSize = new SectionSize();
			float a = f.ReadFloat();
			float b = f.ReadFloat();
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
			f.VerifyEndOfBlock();

		}
	}

	public class TrackSections: ArrayList
	{
        public new TrackSection this[int i]
        {
            get { return (TrackSection)base[i]; }
            set { base[i] = value; }
        }

		public TrackSections( STFReader f )
		{
			f.VerifyStartOfBlock();
			MaxSectionIndex = f.ReadUInt();
			string token = f.ReadToken();
			while( token != ")" ) 
			{
				if( token == "" ) throw ( new STFError( f, "Missing )" ) );
				else if( 0 == String.Compare( token,"TrackSection", true ) ) this.Add( new TrackSection(f) );
				else f.SkipBlock();
				token = f.ReadToken();
			}
		}
		public void AddRouteTrackSections( STFReader f )
		{
			f.VerifyStartOfBlock();
			MaxSectionIndex = f.ReadUInt();
			string token = f.ReadToken();
			while( token != ")" ) 
			{
				if( token == "" ) throw ( new STFError( f, "Missing )" ) );
				else if( 0 == String.Compare( token,"TrackSection", true ) ) this.Add( new RouteTrackSection(f) );
				else f.SkipBlock();
				token = f.ReadToken();
			}
		}

        public static int MissingTrackSectionWarnings = 0;

		public TrackSection Get( uint targetSectionIndex )
		{
			// TODO - do this better - linear search is pretty slow
			for( int i = 0; i < this.Count; ++i )
				if( ((TrackSection)this[i]).SectionIndex == targetSectionIndex )
				{
					return (TrackSection)this[i];
				}
            if( MissingTrackSectionWarnings++ < 5 )
                Console.Error.WriteLine("TDB references track section not listed in global or dynamic TSECTION.DAT: " + targetSectionIndex.ToString());
            return null;
		}
		public uint MaxSectionIndex;
	}
	public class SectionIdx
	{
		public SectionIdx( STFReader f )
		{
			f.VerifyStartOfBlock();
			NoSections = f.ReadUInt();
			X = f.ReadDouble();
			Y = f.ReadDouble();
			Z = f.ReadDouble();
			A = f.ReadDouble();
			TrackSections = new uint[ NoSections ]; 
			for( int i = 0; i < NoSections; ++i )  
			{
       			string token = f.ReadToken();
                if( token == ")" ) 
                {
                    STFError.Report( f, "Missing track section" );
                    return;   // there are many TSECTION.DAT's with missing sections so we will accept this error
                }
    			try
	    		{
                    TrackSections[i] = uint.Parse(token);
				}
				catch( STFError error )  
				{
                    STFError.Report(f, error.Message);
				}
			}
			f.VerifyEndOfBlock();
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
			f.VerifyStartOfBlock();
			ShapeIndex = f.ReadUInt();
			string token = f.ReadToken();
			int nextPath = 0;
			while( token != ")" )
			{
				if( token == "" ) throw ( new STFError( f, "Missing )" ) );
				else if( 0 == String.Compare( token,"FileName", true ) )  FileName = f.ReadStringBlock();
				else if( 0 == String.Compare( token,"NumPaths", true ) ) 
				{
					NumPaths = f.ReadUIntBlock();
					SectionIdxs = new SectionIdx[ NumPaths ];
				}
				else if( 0 == String.Compare( token,"MainRoute",true ) ) MainRoute = f.ReadUIntBlock();
				else if( 0 == String.Compare( token,"ClearanceDist",true ) ) ClearanceDistance = f.ReadDoubleBlock();
				else if( 0 == String.Compare( token,"SectionIdx",true ) ) SectionIdxs[ nextPath++ ] = new SectionIdx( f );
				else if( 0 == String.Compare( token,"TunnelShape",true ) ) TunnelShape = f.ReadBoolBlock();
				else if( 0 == String.Compare( token,"RoadShape",true ) ) RoadShape = f.ReadBoolBlock();
				else f.SkipBlock();
				token = f.ReadToken();
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
			f.VerifyStartOfBlock();
			MaxShapeIndex = f.ReadUInt();
			string token = f.ReadToken();
			while( token != ")" ) 
			{
				if( token == "" ) throw ( new STFError( f, "Missing )" ) );
				else if( 0 == String.Compare( token,"TrackShape",true ) ) this.Add( new TrackShape(f) );
				else f.SkipBlock();
				token = f.ReadToken();
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
			throw( new System.Exception( "ShapeIndex not found" ) );
		}
		public uint MaxShapeIndex;
	}

	public class TSectionDatFile
	{
		public void AddRouteTSectionDatFile( string pathNameExt )
		{
			STFReader f = new STFReader( pathNameExt );
            if (f.Header != "SIMISA@@@@@@@@@@JINX0T0t______")
            {
                Console.Error.WriteLine("Ignoring invalid TSECTION.DAT in route folder.");
                return;
            }
			try
			{
				string token = f.ReadToken();
				while( token != "" ) // EOF
				{
					if( token == "(" ) throw ( new STFError( f, "Unexpected (" ) );
					else if( token == ")" ) throw ( new STFError( f, "Unexpected )" ) );
					else if( 0 == String.Compare( token,"TrackSections",true ) ) TrackSections.AddRouteTrackSections(f);
						// todo read in SectionIdx part of RouteTSectionDat
					else f.SkipBlock();
					token = f.ReadToken();
				}
			}
			finally
			{
				f.Close();
			}
		}
		public TSectionDatFile( string filePath )
		{
			STFReader f = new STFReader( filePath );
			try
			{
				string token = f.ReadToken();
				while( token != "" ) // EOF
				{
					if( token == "(" ) throw ( new STFError( f, "Unexpected (" ) );
					else if( token == ")" ) throw ( new STFError( f, "Unexpected )" ) );
					else if( 0 == String.Compare( token,"TrackSections",true ) ) TrackSections = new TrackSections(f);
					else if( 0 == String.Compare( token,"TrackShapes", true ) ) TrackShapes = new TrackShapes(f);
					else f.SkipBlock();
					token = f.ReadToken();
				}
				if( TrackSections == null ) throw( new STFError( f, "Missing TrackSections" ) );
				if( TrackShapes == null ) throw ( new STFError( f, "Missing TrackShapes" ) );
			}
			finally
			{
				f.Close();
			}
		}
		public TrackSections TrackSections;
		public TrackShapes TrackShapes;
	}


}
