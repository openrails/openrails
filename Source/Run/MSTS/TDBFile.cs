/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections;
using System.Diagnostics;
using MSTSMath;
using Microsoft.Xna.Framework;

namespace MSTS
{
    /// <summary>
    /// Summary description for TDBFile.
    /// </summary>
    /// 

    public class TDBFile
    {
        public TDBFile(string filenamewithpath)
        {
            STFReader f = new STFReader(filenamewithpath);
            try
            {
                string token = f.ReadToken();
                while (token != "") // EOF
                {
                    if (token == ")") throw (new STFError(f, "Unexpected )"));
                    else if (token == "(") f.SkipBlock();
                    else if (0 == String.Compare(token, "TrackDB", true)) TrackDB = new TrackDB(f);
                    else f.SkipBlock();
                    token = f.ReadToken();
                }
            }
            finally
            {
                f.Close();
            }
        }

        /// <summary>
        /// Provide a link to the TrJunctionNode for the switch track with 
        /// the specified UiD on the specified tile.
        /// 
        /// Called by switch track shapes to determine the correct position of the points.
        /// </summary>
        /// <param name="tileX"></param>
        /// <param name="tileZ"></param>
        /// <param name="UiD"></param>
        /// <returns></returns>
        public TrJunctionNode GetTrJunctionNode(int tileX, int tileZ, int UiD )
        {
            foreach( TrackNode tn in TrackDB.TrackNodes )
                if (tn != null && tn.TrJunctionNode != null)
                {
                    if ( tileX == tn.UiD.WorldTileX
                        && tileZ == tn.UiD.WorldTileZ
                        && UiD == tn.UiD.WorldID )
                        return tn.TrJunctionNode;
                }
            throw new System.Exception("TDB Error, could not find junction.");
        }

        public TrackDB TrackDB;  // Warning, the first TDB entry is always null
    }



    public class TrackDB
    {
        public TrackDB(STFReader f)
        {
            f.VerifyStartOfBlock();
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));
                else if (0 == String.Compare(token, "TrackNodes", true))
                {
                    f.VerifyStartOfBlock();
                    int count = f.ReadInt();
                    TrackNodes = new TrackNode[count + 1];
                    count = 1;
                    token = f.ReadToken();
                    while (token != ")")
                    {
                        if (token == "") throw (new STFError(f, "Missing )"));
                        else if (0 == String.Compare(token, "TrackNode", true))
                        {
                            TrackNodes[count] = new TrackNode(f, count);
                            ++count;
                        }
                        else f.SkipBlock();
                        token = f.ReadToken();
                    }
                }
                else f.SkipBlock();
                token = f.ReadToken();
            }
        }
        public TrackNode[] TrackNodes;
    }


    public class TrackNode
    {
        public TrackNode(STFReader f, int count)
        {
            f.VerifyStartOfBlock();
            uint index = f.ReadUInt();
            Debug.Assert(count == index, "TrackNode Index Mismatch");
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));
                else if (0 == String.Compare(token, "TrJunctionNode", true)) TrJunctionNode = new TrJunctionNode(f);
                else if (0 == String.Compare(token, "TrVectorNode", true)) TrVectorNode = new TrVectorNode(f);
                else if (0 == String.Compare(token, "UiD", true)) UiD = new UiD(f);
                else if (0 == String.Compare(token, "TrEndNode", true)) TrEndNode = new TrEndNode(f);
                else if (0 == String.Compare(token, "TrPins", true))
                {
                    f.VerifyStartOfBlock();
                    Inpins = f.ReadUInt();
                    Outpins = f.ReadUInt();
                    TrPins = new TrPin[Inpins + Outpins];
                    for (int i = 0; i < Inpins + Outpins; ++i)
                    {
                        f.MustMatch("TrPin");
                        TrPins[i] = new TrPin(f);
                    }
                    f.MustMatch(")");
                }
                else f.SkipBlock();
                token = f.ReadToken();
            }
            // TODO DEBUG CODE
            if (TrVectorNode != null && TrPins.Length != 2)
                Console.Error.WriteLine("TDB DEBUG TVN={0} has {1} pins.", UiD, TrPins.Length);
        }
        public TrJunctionNode TrJunctionNode = null;
        public TrVectorNode TrVectorNode = null;
        public TrEndNode TrEndNode = null;
        public TrPin[] TrPins;
        public UiD UiD = null;  // only provided for TrJunctionNode and TrEndNode type of TrackNodes
        public uint Inpins;
        public uint Outpins;
    }

    public class TrPin
    {
        public TrPin(STFReader f)
        {
            f.VerifyStartOfBlock();
            Link = f.ReadInt();
            Direction = f.ReadInt();
            f.MustMatch(")");
        }
        public int Link;
        public int Direction;
    }

    public class UiD
    {
        public int TileX, TileZ;   // location of the junction 
        public float X, Y, Z;
        public float AX, AY, AZ;

        public int WorldTileX, WorldTileZ, WorldID;  // cross reference to the entry in the world file

        public UiD(STFReader f)
        {
            // UiD ( -11283 14482 5 0 -11283 14482 -445.573 239.861 186.111 0 -3.04199 0 )

            f.VerifyStartOfBlock();
            WorldTileX = f.ReadInt();            // -11283
            WorldTileZ = f.ReadInt();            // 14482
            WorldID = f.ReadInt();              // 5
            f.ReadInt();                        // 0
            TileX = f.ReadInt();            // -11283
            TileZ = f.ReadInt();            // 14482
            X = f.ReadFloat();         // -445.573
            Y = f.ReadFloat();         // 239.861
            Z = f.ReadFloat();         // 186.111
            AX = f.ReadFloat();         // 0
            AY = f.ReadFloat();         // -3.04199
            AZ = f.ReadFloat();         // 0
            f.MustMatch(")");
        }

    }

    public class TrJunctionNode
    {
        public int SelectedRoute = 0;

        public TrJunctionNode(STFReader f)
        {
            f.VerifyStartOfBlock();
            f.ReadToken();
            ShapeIndex = f.ReadUInt();
            f.ReadToken();
            f.MustMatch(")");
        }
        public uint ShapeIndex;
    }

    public class TrVectorNode
    {
        public TrVectorNode(STFReader f)
        {
            f.VerifyStartOfBlock();
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));
                else if (0 == String.Compare(token, "TrVectorSections", true))
                {
                    f.VerifyStartOfBlock();
                    int count = f.ReadInt();
                    TrVectorSections = new TrVectorSection[count];
                    for (int i = 0; i < count; ++i)
                    {
                        TrVectorSections[i] = new TrVectorSection(f);
                    }
                    f.MustMatch(")");
                }
                else f.SkipBlock();
                token = f.ReadToken();
            }
        }
        public TrVectorSection[] TrVectorSections;
    }

    public class TrVectorSection
    {
        public TrVectorSection(STFReader f)
        {
            SectionIndex = f.ReadUInt();
            ShapeIndex = f.ReadUInt();
            f.ReadToken(); // worldfilenamex
            f.ReadToken(); // worldfilenamez
            WorldFileUiD = f.ReadUInt(); // UID in worldfile
            flag1 = f.ReadInt(); // 0
            flag2 = f.ReadInt(); // 1
            f.ReadToken(); // 00 
            TileX = f.ReadInt();
            TileZ = f.ReadInt();
            X = f.ReadFloat();
            Y = f.ReadFloat();
            Z = f.ReadFloat();
            AX = f.ReadFloat();
            AY = f.ReadFloat();
            AZ = f.ReadFloat();
        }
        public int flag1;   // usually 0, - may point to the connecting pin entry in a junction
        public int flag2;  // usually 1, but set to 0 when curve track is flipped around
        // I have also seen 2's in both locations
        public uint SectionIndex;
        public uint ShapeIndex;
        public int TileX;
        public int TileZ;
        public float X, Y, Z;
        public float AX, AY, AZ;
        public uint WorldFileUiD;
    }


    public class TrEndNode
    {
        public TrEndNode(STFReader f)
        {
            f.VerifyStartOfBlock();
            f.ReadToken();
            f.MustMatch(")");
        }

    }

}
