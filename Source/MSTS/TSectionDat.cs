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
using ORTS.Interlocking;

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
		public SectionCurve(STFReader stf)
		{
			stf.MustMatch("(");
            Radius = stf.ReadFloat(STFReader.UNITS.None, null);
            Angle = stf.ReadFloat(STFReader.UNITS.None, null);
            stf.SkipRestOfBlock();
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
		public SectionSize(STFReader stf)
		{
			stf.MustMatch("(");
            Width = stf.ReadFloat(STFReader.UNITS.Distance, null);
            Length = stf.ReadFloat(STFReader.UNITS.Distance, null);
            stf.SkipRestOfBlock();
		}
		public float Width;
		public float Length;
	}
	
	public class TrackSection
	{
		public TrackSection()
		{
		}
		
		public TrackSection(STFReader stf)
		{
			stf.MustMatch("(");
            SectionIndex = stf.ReadUInt(STFReader.UNITS.None, null);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("sectionsize", ()=>{ SectionSize = new SectionSize(stf); }),
                new STFReader.TokenProcessor("sectioncurve", ()=>{ SectionCurve = new SectionCurve(stf); }),
            });
			//if( SectionSize == null )
			//	throw( new STFError( stf, "Missing SectionSize" ) );
			//  note- default TSECTION.DAT does have some missing sections
		}
		public uint SectionIndex;
		public SectionSize SectionSize;
		public SectionCurve SectionCurve;
	}
	
	public class RouteTrackSection: TrackSection
	{
		public RouteTrackSection(STFReader stf)
		{
			stf.MustMatch("(");
			stf.MustMatch( "SectionCurve" );
			stf.SkipBlock();
            SectionIndex = stf.ReadUInt(STFReader.UNITS.None, null);
			SectionSize = new SectionSize();
            float a = stf.ReadFloat(STFReader.UNITS.Distance, null);
            float b = stf.ReadFloat(STFReader.UNITS.None, null);
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
			stf.SkipRestOfBlock();
		}
	}

	public class TrackSections: Dictionary<uint, TrackSection>
	{
		public TrackSections(STFReader stf)
		{
			stf.MustMatch("(");
            MaxSectionIndex = stf.ReadUInt(STFReader.UNITS.None, null);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tracksection", ()=>{ AddSection(stf, new TrackSection(stf)); }),
            });
		}

		public void AddRouteTrackSections(STFReader stf)
		{
			stf.MustMatch("(");
            MaxSectionIndex = stf.ReadUInt(STFReader.UNITS.None, null);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tracksection", ()=>{ AddSection(stf, new RouteTrackSection(stf)); }),
            });
		}
        private void AddSection(STFReader stf, TrackSection section)
        {
            if (ContainsKey(section.SectionIndex))
            {
                STFException.TraceWarning(stf, "Duplicate SectionIndex of " + section.SectionIndex);
            }
            this[section.SectionIndex] = section;
        }

        public static int MissingTrackSectionWarnings = 0;

		public TrackSection Get( uint targetSectionIndex )
		{
            TrackSection ts;
            if (TryGetValue(targetSectionIndex, out ts))
                return ts;
			if (MissingTrackSectionWarnings++ < 5)
				Trace.TraceWarning("TDB references track section not listed in global or dynamic TSECTION.DAT: " + targetSectionIndex.ToString());
            return null;
		}
		public uint MaxSectionIndex;
	}
	public class SectionIdx
	{
		public SectionIdx(STFReader stf)
		{
			stf.MustMatch("(");
            NoSections = stf.ReadUInt(STFReader.UNITS.None, null);
            X = stf.ReadDouble(STFReader.UNITS.None, null);
            Y = stf.ReadDouble(STFReader.UNITS.None, null);
            Z = stf.ReadDouble(STFReader.UNITS.None, null);
            A = stf.ReadDouble(STFReader.UNITS.None, null);
			TrackSections = new uint[NoSections]; 
			for( int i = 0; i < NoSections; ++i )  
			{
       			string token = stf.ReadString();
                if( token == ")" ) 
                {
                    STFException.TraceWarning(stf, "Missing track section");
                    return;   // there are many TSECTION.DAT's with missing sections so we will accept this error
                }
                if (!uint.TryParse(token, out TrackSections[i]))
                    STFException.TraceWarning(stf, "Invalid Track Section " + token);
			}
			stf.SkipRestOfBlock();
		}
		public uint NoSections;
		public double X,Y,Z;  // Offset
		public double A;  // Angular offset 
		public uint[] TrackSections;
	}
	

   [DebuggerDisplay("TrackShape {ShapeIndex}")]
	public class TrackShape
	{
		public TrackShape(STFReader stf)
		{
			stf.MustMatch("(");
            ShapeIndex = stf.ReadUInt(STFReader.UNITS.None, null);
			int nextPath = 0;
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("filename", ()=>{ FileName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("numpaths", ()=>{ SectionIdxs = new SectionIdx[NumPaths = stf.ReadUIntBlock(STFReader.UNITS.None, null)]; }),
                new STFReader.TokenProcessor("mainroute", ()=>{ MainRoute = stf.ReadUIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("clearancedist", ()=>{ ClearanceDistance = stf.ReadDoubleBlock(STFReader.UNITS.Distance, null); }),
                new STFReader.TokenProcessor("sectionidx", ()=>{ SectionIdxs[nextPath++] = new SectionIdx(stf); }),
                new STFReader.TokenProcessor("tunnelshape", ()=>{ TunnelShape = stf.ReadBoolBlock(true); }),
                new STFReader.TokenProcessor("roadshape", ()=>{ RoadShape = stf.ReadBoolBlock(true); }),
            });
			// TODO - this was removed since TrackShape( 183 ) is blank
			//if( FileName == null )	throw( new STFError( stf, "Missing FileName" ) );
			//if( SectionIdxs == null )	throw( new STFError( stf, "Missing SectionIdxs" ) );
			//if( NumPaths == 0 ) throw( new STFError( stf, "No Paths in TrackShape" ) );
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
	
	public class TrackShapes: Dictionary<uint, TrackShape>
	{

      public uint MaxShapeIndex;

      
		public TrackShapes(STFReader stf)
		{
            stf.MustMatch("(");
            MaxShapeIndex = stf.ReadUInt(STFReader.UNITS.None, null);
            stf.ParseBlock(new STFReader.TokenProcessor[] 
            {
                new STFReader.TokenProcessor("trackshape", ()=>{ Add(new TrackShape(stf)); }),
            });
		}

      private void Add(TrackShape trackShape)
      {
         Add(trackShape.ShapeIndex, trackShape);
      }


      /// <summary>
      /// Returns the TrackShape corresponding to the given index value.
      /// </summary>
      /// <param name="targetShapeIndex">The index value of the desired TrackShape.</param>
      /// <returns>The requested TrackShape.</returns>
		public TrackShape Get(uint targetShapeIndex )
		{
         TrackShape returnValue = null;

         if (ContainsKey(targetShapeIndex))
         {
            returnValue = this[targetShapeIndex];
         }
         else
         {
            throw new InvalidDataException("ShapeIndex not found");
         }

         return returnValue;
		}

		
	}

	public class TSectionDatFile
	{
		public void AddRouteTSectionDatFile( string pathNameExt )
		{
            using (STFReader stf = new STFReader(pathNameExt, false))
            {
                if (stf.SimisSignature != "SIMISA@@@@@@@@@@JINX0T0t______")
                {
                    Trace.TraceWarning("Ignoring invalid TSECTION.DAT in route folder.");
                    return;
                }
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("tracksections", ()=>{ TrackSections.AddRouteTrackSections(stf); }),
                    new STFReader.TokenProcessor("sectionidx", ()=>{ TSectionIdx = new TSectionIdx(stf); }),
                    // todo read in SectionIdx part of RouteTSectionDat
                });
            }
		}
        public TSectionDatFile(string filePath)
        {
            using (STFReader stf = new STFReader(filePath, false))
            {
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("tracksections", ()=>{ TrackSections = new TrackSections(stf); }),
                    new STFReader.TokenProcessor("trackshapes", ()=>{ TrackShapes = new TrackShapes(stf); }),
                });
                //TODO This should be changed to STFException.TraceError() with defaults values created
                if (TrackSections == null) throw new STFException(stf, "Missing TrackSections");
                if (TrackShapes == null) throw new STFException(stf, "Missing TrackShapes");
            }
        }
		public TrackSections TrackSections;
		public TrackShapes TrackShapes;
		public TSectionIdx TSectionIdx; //route's tsection.dat

	}

	public class TSectionIdx //SectionIdx in the route's tsection.dat
	{

		public TSectionIdx(STFReader stf)
		{
			stf.MustMatch("(");
			NoSections = stf.ReadUInt(STFReader.UNITS.None, null);
			TrackPaths = new Dictionary<uint, TrackPath>((int)NoSections);
			stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("trackpath", ()=>{ AddPath(stf, new TrackPath(stf)); }),
            });
			stf.SkipRestOfBlock();
		}

		private void AddPath(STFReader stf, TrackPath path)
		{
			try
			{
				TrackPaths.Add(path.DynamicSectionIndex, path);
			}
			catch (Exception e)
			{
				System.Console.WriteLine("Warning: in route tsection.dat " + e.Message);
			}
		}
		public uint NoSections;
		public Dictionary<uint, TrackPath> TrackPaths;
	}

	public class TrackPath //SectionIdx in the route's tsection.dat
	{

		public TrackPath(STFReader stf)
		{
			stf.MustMatch("(");
			DynamicSectionIndex = stf.ReadUInt(STFReader.UNITS.None, null);
			NoSections = stf.ReadUInt(STFReader.UNITS.None, null);
			TrackSections = new uint[NoSections];
			for (int i = 0; i < NoSections; ++i)
			{
				string token = stf.ReadString();
				if (token == ")")
				{
					STFException.TraceWarning(stf, "Missing track section");
					return;   // there are many TSECTION.DAT's with missing sections so we will accept this error
				}
				if (!uint.TryParse(token, out TrackSections[i]))
					STFException.TraceWarning(stf, "Invalid Track Section " + token);
			}
			stf.SkipRestOfBlock();

		}
		public uint DynamicSectionIndex;
		public uint NoSections;
		public uint[] TrackSections;
	}
}
