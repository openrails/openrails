/// Track Paths
/// 
/// The PAT file contains a series of waypoints ( x,y,z coordinates ) for
/// the train.   The path starts at TrPathNodes[0].   This node contains 
/// an index to a TrackPDB.  That TrackPDB defines the starting coordinates 
/// for the path.  The TrPathNode also contains a link to the next TrPathNode.  
/// Open the next TrPathNode and read the PDP that defines the next waypoint.
/// The last TrPathNode is marked with a 4294967295 ( -1L ) in its next field.

/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;


namespace MSTS
{
    public class PATTraveller
    {
        public int TileX { get { return CurrentTrackPDP.TileX; } }
        public int TileZ { get { return CurrentTrackPDP.TileZ; } }
        public float X { get { return CurrentTrackPDP.X; } }
        public float Y { get { return CurrentTrackPDP.Y; } }
        public float Z { get { return CurrentTrackPDP.Z; } }

        /// <summary>
        /// Initializes the traveller to the first waypoint 
        /// in the specified path file.
        /// </summary>
        /// <param name="PATFilePath"></param>
        public PATTraveller(string PATFilePath)
        {
            PATFile = new PATFile(PATFilePath);
            CurrentTrPathNode = PATFile.TrPathNodes[0];
            CurrentTrackPDP = PATFile.TrackPDPs[(int)CurrentTrPathNode.FromPDP];
        }

        public PATTraveller(PATTraveller copy)
        {
            PATFile = copy.PATFile;
            CurrentTrPathNode = copy.CurrentTrPathNode;
            CurrentTrackPDP = copy.CurrentTrackPDP;
        }

        public bool IsLastWaypoint()
        {
            return CurrentTrPathNode.NextNode == 4294967295U;
        }

        public void NextWaypoint()
        {
			if (IsLastWaypoint())
				throw new InvalidOperationException("Attempt to read past end of path");
            CurrentTrPathNode = PATFile.TrPathNodes[(int)CurrentTrPathNode.NextNode];
            CurrentTrackPDP = PATFile.TrackPDPs[(int)CurrentTrPathNode.FromPDP];
        }

        PATFile PATFile;
        TrPathNode CurrentTrPathNode;
        TrackPDP CurrentTrackPDP;
    }

	/// <summary>
	/// Work with consist files, contains an ArrayList of ConsistTrainset
	/// </summary>
	public class PATFile
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
        public uint Flags { get; set; }
		public bool IsPlayerPath { get { return (Flags & 0x20) == 0; } }

        public List<TrackPDP> TrackPDPs
        {
            get
            {
                return trackPDPs;
            }
            set
            {
                trackPDPs = value;
            }
        }

        public List<TrPathNode> TrPathNodes
        {
            get
            {
                return trPathNodes;
            }
            set
            {
                trPathNodes = value;
            }
        }

        #endregion
        /// <summary>
		/// Open a PAT file, 
		/// filePath includes full path and extension
		/// </summary>
		/// <param name="filePath"></param>
		public PATFile( string filePath )
		{
            using (STFReader stf = new STFReader(filePath, false))
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("trackpdps", ()=>{ stf.MustMatch("("); stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("trackpdp", ()=>{ trackPDPs.Add(new TrackPDP(stf)); }),
                    });}),
                    new STFReader.TokenProcessor("trackpath", ()=>{ stf.MustMatch("("); stf.ParseBlock(new STFReader.TokenProcessor[] {
						new STFReader.TokenProcessor("trpathname", ()=>{ PathID = stf.ReadStringBlock(null); }),
                        new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
						new STFReader.TokenProcessor("trpathflags", ()=>{ Flags = stf.ReadHexBlock(null); }),
						new STFReader.TokenProcessor("trpathstart", ()=>{ Start = stf.ReadStringBlock(null); }),
						new STFReader.TokenProcessor("trpathend", ()=>{ End = stf.ReadStringBlock(null); }),
                        new STFReader.TokenProcessor("trpathnodes", ()=>{
                            stf.MustMatch("(");
                            int count = stf.ReadInt(STFReader.UNITS.None, null);
                            stf.ParseBlock(new STFReader.TokenProcessor[] {
                                new STFReader.TokenProcessor("trpathnode", ()=>{ --count; trPathNodes.Add(new TrPathNode(stf)); }),
                            });
                            if (count != 0)
                                STFException.TraceWarning(stf, "TrPathNodes count incorrect");
                        }),
                    });}),
                });
          }

        public override string ToString()
        {
            return this.Name;
        }
	} // Class CONFile

	public class TrackPDP
	{

        public int TileX;
        public int TileZ;
        public float X,Y,Z;
        public int A,B;

		public TrackPDP(STFReader stf)
		{
            stf.MustMatch("(");
            TileX = stf.ReadInt(STFReader.UNITS.None, null);
            TileZ = stf.ReadInt(STFReader.UNITS.None, null);
            X = stf.ReadFloat(STFReader.UNITS.None, null);
            Y = stf.ReadFloat(STFReader.UNITS.None, null);
            Z = stf.ReadFloat(STFReader.UNITS.None, null);
            A = stf.ReadInt(STFReader.UNITS.None, null);
            B = stf.ReadInt(STFReader.UNITS.None, null);
            stf.SkipRestOfBlock();
        }
	}
    public class TrPathNode
    {

        public uint A,NextNode,C,FromPDP;  // TODO, we don't really understand these

        public TrPathNode(STFReader stf)
        {
            stf.MustMatch("(");
            A = stf.ReadHex(0);
            NextNode = stf.ReadUInt(STFReader.UNITS.None, null);
            C = stf.ReadUInt(STFReader.UNITS.None, null);
            FromPDP = stf.ReadUInt(STFReader.UNITS.None, null);
            stf.SkipRestOfBlock();
        }
    }
}
