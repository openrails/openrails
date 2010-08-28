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
        public List<TrackPDP> TrackPDPs = new List<TrackPDP>();
        public List<TrPathNode> TrPathNodes = new List<TrPathNode>();

        /// <summary>
		/// Open a PAT file, 
		/// filePath includes full path and extension
		/// </summary>
		/// <param name="filePath"></param>
		public PATFile( string filePath )
		{
            STFReader f = new STFReader(filePath);
            try
            {
                string token = f.ReadToken();
                while (token != "") // EOF
                {
                    if (token == "(") throw (new STFException(f, "Unexpected ("));
                    else if (token == ")") throw (new STFException(f, "Unexpected )"));
                    else if (0 == String.Compare(token, "TrackPDPs", true)) ReadTrackPDPs(f);
                    else if (0 == String.Compare(token, "TrackPath", true)) ReadTrackPath(f);
                    else f.SkipBlock();  // TODO for now we are skipping unknown items
                    token = f.ReadToken();
                }
            }
            finally
            {
                f.Close();
            }
      }

        public void ReadTrackPDPs( STFReader f )
        {
            f.VerifyStartOfBlock();
            string token = f.ReadToken();
            while (token != ")")
            {
                if (0 == String.Compare(token, "TrackPDP", true))
                {
                    TrackPDPs.Add(new TrackPDP(f));
                }
                else
                {
                    throw (new STFException(f, "Unexpected " + token + "in TrackPDBs"));
                }
                token = f.ReadToken();
            }
        }

        public void ReadTrackPath(STFReader f)
        {
            f.VerifyStartOfBlock();
            string token = f.ReadToken();
            while (token != ")") 
            {
                if (token == "(") throw (new STFException(f, "Unexpected ("));
                else if (0 == String.Compare(token, "TrPathNodes", true)) ReadTrPathNodes(f);
                else f.SkipBlock();  // TODO for now we are skipping unknown items
                token = f.ReadToken();
            }
        }

        public void ReadTrPathNodes(STFReader f)
        {
            f.VerifyStartOfBlock();
            int count = f.ReadInt();
            string token = f.ReadToken();
            while (token != ")")
            {
                if (0 == String.Compare(token, "TrPathNode", true))
                {
                    TrPathNodes.Add(new TrPathNode(f));
                    count--;
                }
                else
                {
                    throw (new STFException(f, "Unexpected " + token + "in TrPathNodes"));
                }
                token = f.ReadToken();
            }
            if (count != 0)
                throw new STFException(f, "TrPathNodes count incorrect");
        }


	} // Class CONFile

	public class TrackPDP
	{

        public int TileX;
        public int TileZ;
        public float X,Y,Z;
        public int A,B;

		public TrackPDP( STFReader f )
		{
            f.VerifyStartOfBlock();
            TileX = f.ReadInt();
            TileZ = f.ReadInt();
            X = f.ReadFloat();
            Y = f.ReadFloat();
            Z = f.ReadFloat();
            A = f.ReadInt();
            B = f.ReadInt();
            f.MustMatch(")");
        }
	}
    public class TrPathNode
    {

        public uint A,NextNode,C,FromPDP;  // TODO, we don't really understand these

        public TrPathNode(STFReader f)
        {
            f.VerifyStartOfBlock();
            A = f.ReadFlags();
            NextNode = f.ReadUInt();
            C = f.ReadUInt();
            FromPDP = f.ReadUInt();
            f.MustMatch(")");
        }
    }
}

