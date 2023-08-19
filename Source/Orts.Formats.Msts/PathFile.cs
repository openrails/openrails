// COPYRIGHT 2009, 2010, 2011, 2012 by the Open Rails project.
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

// Track Paths
// 
// The PAT file contains a series of waypoints ( x,y,z coordinates ) for
// the train.   The path starts at TrPathNodes[0].   This node contains 
// an index to a TrackPDB.  That TrackPDB defines the starting coordinates 
// for the path.  The TrPathNode also contains a link to the next TrPathNode.  
// Open the next TrPathNode and read the PDP that defines the next waypoint.
// The last TrPathNode is marked with a 4294967295 ( -1L or 0xFFFFFFFF) in its next field.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Orts.Parsers.Msts;


namespace Orts.Formats.Msts
{
    // This relates to TrPathFlags, which is not always present in .pat file
    // It is unclear what the other flags (bits) mean
    [Flags]
    public enum PathFlags
    {
        NotPlayerPath = 0x20,  
    }

	/// <summary>
	/// Paths for both player train as well as AI trains.
    /// This class reads and stores the MSTS .pat file. Because of the format of the .pat file 
    /// it is easier to have an intermediate format that just contains the data of the .pat file
    /// and create the wanted data scructure from that.
    /// It is the intention that it is only used for
    ///     * ORTS main menu
    ///     * postprocessing by TrainPath.
	/// </summary>
// Typical simple .PATfile (the example helps to understand the code below)
/*
    SIMISA@@@@@@@@@@JINX0P0t______

Serial ( 1 )
TrackPDPs (
	TrackPDP ( -12557 14761 -6.1249 1173.74 72.5884 2 0 )
	TrackPDP ( -12557 14761 -204.363 1173.74 976.083 2 0 )
	TrackPDP ( -12557 14762 -287.228 1173.74 -971.75 2 0 )
	TrackPDP ( -12558 14763 278.107 1155.51 -941.416 2 0 )
	TrackPDP ( -12557 14761 -49.6355 1173.74 -164.577 1 1 )
	TrackPDP ( -12558 14763 245.63 1139.65 -387.96 1 1 )
)
TrackPath (
	TrPathName ( EsxPincal )
	Name ( "Essex - Pinnacle" )
	TrPathStart ( Essex )
	TrPathEnd ( Pinnacle )
	TrPathNodes ( 6
		TrPathNode ( 00000000 1 4294967295 4 )
		TrPathNode ( 00000000 2 4294967295 0 )
		TrPathNode ( 00000000 3 4294967295 1 )
		TrPathNode ( 00000000 4 4294967295 2 )
		TrPathNode ( 00000000 5 4294967295 3 )
		TrPathNode ( 00000000 4294967295 4294967295 5 )
	)

)
*/
    // TrackPDP format is : TrackPDP ( tileX tileZ x y z flag1 flag2)
    //      Precise meaning of flag1 and flag2 is unknown. 
    //          2 0 seems to be junction
    //          1 1 is an end point or return point.
    //              When bit 3 is set for flag2 (so 8, 9, 12, 13), it seems to denote a broken (or perhaps unfinished) path. Perhaps route was changed afterwards.
    //
    // TrPathNode format is : TrPathNode ( flags nextMainNode nextSidingNode correspondingPDP)
    //          Note 4294967295 = 2^32-1 = 0xFFFFFFFF  denotes that there is no next...Node.
    //          For an (partly) explanation of the flags, see AIPath.cs
    //
    // flags:
    //      Possible interpretation (as found on internet, by krausyao)
    //      TrPathNode ( AAAABBBB mainIdx passingIdx pdpIdx )
    //      AAAA wait time seconds in hexidecimal
    //      BBBB (Also hexidecimal, so 16 bits)
    //      Bit 0 - connected pdp-entry references a reversal-point (1/x1)
    //      Bit 1 - waiting point (2/x2)
    //      Bit 2 - intermediate point between switches (4/x4)
    //      Bit 3 - 'other exit' is used (8/x8)
    //      Bit 4 - 'optional Route' active (16/x10)
    //
    // Most common flag combinations
    //      PDPflags    TrPathflags Interpretation
    //      2 0         0       node is at junction
    //      1 1         0       node is start or end point
    //      1 1         4       node is some intermediate point, not at junction
    //      1 1         1       Reversal point
    //      1 12        x       Seems to be indicating a path that is broken (or perhaps simply unfinished)
    //      1 9         x       Sometimes seen this as a end or beginning of route, but not always
    //      2 0         8       (e.g. Shiatsu, not clear why). It does not seem to be 'other exit'
    //  

    
	public class PathFile
    {
        #region Fields

        private List<TrackPDP> trackPDPs = new List<TrackPDP>();
        private List<TrPathNode> trPathNodes = new List<TrPathNode>();

        #endregion

        #region Properties

        public string PathID { get; set; }
        public string Name { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public PathFlags Flags { get; set; }
        public bool IsPlayerPath { get { return (Flags & PathFlags.NotPlayerPath) == 0; } }

        public List<TrackPDP> TrackPDPs
        {
            get
            {
                return trackPDPs;
            }
        }

        public List<TrPathNode> TrPathNodes
        {
            get
            {
                return trPathNodes;
            }
        }

        #endregion
        /// <summary>
		/// Open a PAT file, parse it and store it
		/// </summary>
		/// <param name="filePath">path to the PAT file, including full path and extension</param>
        public PathFile(string filePath)
        {
            try
            {
                using (STFReader stf = new STFReader(filePath, false))
                    stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("trackpdps", ()=>{ stf.MustMatch("("); stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("trackpdp", ()=>{ trackPDPs.Add(new TrackPDP(stf)); }),
                    });}),
                    new STFReader.TokenProcessor("trackpath", ()=>{ stf.MustMatch("("); stf.ParseBlock(new STFReader.TokenProcessor[] {
						new STFReader.TokenProcessor("trpathname", ()=>{ PathID = stf.ReadStringBlock(null); }),
                        new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
						new STFReader.TokenProcessor("trpathflags", ()=>{ Flags = (PathFlags)stf.ReadHexBlock(null); }),
						new STFReader.TokenProcessor("trpathstart", ()=>{ Start = stf.ReadStringBlock(null); }),
						new STFReader.TokenProcessor("trpathend", ()=>{ End = stf.ReadStringBlock(null); }),
                        new STFReader.TokenProcessor("trpathnodes", ()=>{
                            stf.MustMatch("(");
                            var count = stf.ReadInt(null);
                            stf.ParseBlock(new STFReader.TokenProcessor[] {
                                new STFReader.TokenProcessor("trpathnode", ()=>{
                                    if (--count < 0)
                                        STFException.TraceWarning(stf, "Skipped extra TrPathNodes");
                                    else
                                        trPathNodes.Add(new TrPathNode(stf));
                                }),
                            });
                            if (count > 0)
                                STFException.TraceWarning(stf, count + " missing TrPathNodes(s)");
                        }),
                    });}),
                });
            }
            catch (Exception error)
            {
                Trace.TraceWarning(error.Message);
            }
        }

        public override string ToString()
        {
            return this.Name;
        }
	}

    // for explanation of TrackPDP, see class PATfile
	public class TrackPDP
	{
        //We are not using WorldLocation to keep MSTS file parsing independent of other parts of the code
        public int TileX;
        public int TileZ;
        public float X,Y,Z;
        public int junctionFlag, invalidFlag;

        #region Properties
        //Note : these flags are not understood in all detail
        public bool IsJunction { get { return junctionFlag == 2; } }
        public bool IsInvalid { get { return invalidFlag == 9; } } //TODO: probably also 12 is invalid.
        #endregion

        public TrackPDP(STFReader stf)
		{
            stf.MustMatch("(");
            TileX = stf.ReadInt(null);
            TileZ = stf.ReadInt(null);
            X = stf.ReadFloat(STFReader.UNITS.None, null);
            Y = stf.ReadFloat(STFReader.UNITS.None, null);
            Z = stf.ReadFloat(STFReader.UNITS.None, null);
            junctionFlag = stf.ReadInt(null);
            invalidFlag = stf.ReadInt(null);
            stf.SkipRestOfBlock();
        }

        public TrackPDP(TrItem item)
        {
            TileX = item.TileX;
            TileZ = item.TileZ;
            X = item.X;
            Y = item.Y;
            Z = item.Z;
            junctionFlag = 0;
            invalidFlag = 0;
        }
	}

    // for an explanation, see class PATfile 
    public class TrPathNode
    {
        public uint pathFlags,nextMainNode,nextSidingNode,fromPDP;
        
        // Note, pathFlags is a complicated beast, which is not fully understood, see AIPath.cs

        public bool HasNextMainNode   { get { return (nextMainNode   != 0xffffffff); } }
        public bool HasNextSidingNode { get { return (nextSidingNode != 0xffffffff); } }

        public TrPathNode(STFReader stf)
        {
            stf.MustMatch("(");
            pathFlags = stf.ReadHex(0);
            nextMainNode = stf.ReadUInt(null);
            nextSidingNode = stf.ReadUInt(null);
            fromPDP = stf.ReadUInt(null);
            stf.SkipRestOfBlock();
        }

        public TrPathNode (uint flags, uint nextNode, uint nextSiding, uint pdp)
        {
            pathFlags = flags;
            nextMainNode = nextNode;
            nextSidingNode = nextSiding;
            fromPDP = pdp;
        }
    }
}
