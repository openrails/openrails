// COPYRIGHT 2010, 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml; 
using System.Xml.Schema;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using MSTS;

namespace ORTS
{
    public class TrackLayer
    {
        TDBFile TDB; // Reference to Track Data Base
        TSectionDatFile TSectionDat; // Reference to tsection.dat file

        public TrackLayer(TDBFile tdb, TSectionDatFile tsdat)
        {
            TDB = tdb;
            TSectionDat = tsdat;

            //AnalyzeTDB();
        }

        public void LaySection(Traveller traveler)
        {
            


        } // end LaySection

        public void AnalyzeTDB()
        {
            // For each track vector node in TDB, check adjacent node type.
            for (int i = 1; i < TDB.TrackDB.TrackNodes.Length; i++) // Skip node-0, which is always null
            {
                TrackNode tn = TDB.TrackDB.TrackNodes[i];
                if (tn.TrVectorNode == null) continue;
                for (int j = 0; j < 2; j++)
                {
                    int link = tn.TrPins[j].Link;
                    TrackNode adjacentNode = TDB.TrackDB.TrackNodes[link];
                    if (adjacentNode.TrVectorNode != null)
                    {
                        // A TrVectorNode is adjacent to a TrVectorNode
                        throw new Exception("TrVectorNode adjacent to TrVectorNode");
                    }
                }
            }
        } // end AnalyzeTDB
    } // end class TrackLayer

} // end namespace ORTS