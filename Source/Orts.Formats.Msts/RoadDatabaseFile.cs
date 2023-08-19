// COPYRIGHT 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
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

using System;
using System.Diagnostics;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using Orts.Parsers.Msts;

namespace Orts.Formats.Msts
{
	/// <summary>
	/// RDBFile is a representation of the .rdb file, that contains the road data base.
    /// The database contains the same kind of objects as TDBFile, apart from a few road-specific items.
	/// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Keeping identifier consistent to use in MSTS")]
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Disposable only used in using statement, known FcCop bug")]
    public class RoadDatabaseFile
	{
        /// <summary>
        /// Contains the Database with all the road tracks.
        /// Warning, the first RoadTrackDB entry is always null.
        /// </summary>
        public RoadTrackDB RoadTrackDB { get; set; }

        /// <summary>
        /// Constructor from file
        /// </summary>
        /// <param name="filenamewithpath">Full file name of the .rdb file</param>
		public RoadDatabaseFile(string filenamewithpath)
		{
			using (STFReader stf = new STFReader(filenamewithpath, false))
				stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("trackdb", ()=>{ RoadTrackDB = new RoadTrackDB(stf); }),
                });
		}
	}

    /// <summary>
    /// This class represents the Road Track Database. This is pretty similar to the (rail) Track Database. So for more details see there
    /// </summary>
	public class RoadTrackDB
	{
        /// <summary>
        /// Array of all TrackNodes in the road database
        /// Warning, the first TrackNode is always null.
        /// </summary>
        public TrackNode[] TrackNodes;

        /// <summary>
        /// Array of all Track Items (TrItem) in the road database
        /// </summary>
        public TrItem[] TrItemTable;

        /// <summary>
        /// Default constructor used during file parsing.
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        public RoadTrackDB(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tracknodes", ()=>{
                    stf.MustMatch("(");
                    int count = stf.ReadInt(null);
                    TrackNodes = new TrackNode[count + 1];
                    int idx = 1;
                    stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("tracknode", ()=>{ TrackNodes[idx] = new TrackNode(stf, idx, count); ++idx; }),
                    });
                }),
                new STFReader.TokenProcessor("tritemtable", ()=>{
                    stf.MustMatch("(");
                    int count = stf.ReadInt(null);
                    TrItemTable = new TrItem[count];
                    int idx = -1;
                    stf.ParseBlock(()=> ++idx == -1, new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("levelcritem", ()=>{ TrItemTable[idx] = new RoadLevelCrItem(stf,idx); }),
                        new STFReader.TokenProcessor("emptyitem", ()=>{ TrItemTable[idx] = new EmptyItem(stf,idx); }),
                        new STFReader.TokenProcessor("carspawneritem", ()=>{ TrItemTable[idx] = new CarSpawnerItem(stf,idx); })
                    });
                }),
            });
        }
	}

    /// <summary>
    /// Represents a Level crossing Item on the road (i.e. where cars must stop when a train is passing).
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Keeping identifier consistent to use in MSTS")]
    public class RoadLevelCrItem : TrItem
	{
        /// <summary>Direction along track: 0 or 1 depending on which way signal is facing</summary>
        public uint Direction { get; set; }
        /// <summary>index to Sigal Object Table</summary>
        public int SigObj { get; set; }

        /// <summary>
        /// Default constructor used during file parsing.
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        /// <param name="idx">The index of this TrItem in the list of TrItems</param>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Keeping identifier consistent to use in MSTS")]
        public RoadLevelCrItem(STFReader stf, int idx)
		{
            SigObj = -1;
			ItemType = trItemType.trXING;
			stf.MustMatch("(");
			stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tritemid", ()=>{ ParseTrItemID(stf, idx); }),
                new STFReader.TokenProcessor("tritemrdata", ()=>{ TrItemRData(stf); }),
                new STFReader.TokenProcessor("tritemsdata", ()=>{ TrItemSData(stf); }),
                new STFReader.TokenProcessor("tritempdata", ()=>{ TrItemPData(stf); })
            });
		}
	}

    /// <summary>
    /// Represent a Car Spawner: the place where cars start to appear or disappear again
    /// </summary>
	public class CarSpawnerItem : TrItem
	{
        /// <summary>
        /// Default constructor used during file parsing.
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        /// <param name="idx">The index of this TrItem in the list of TrItems</param>
		public CarSpawnerItem(STFReader stf, int idx)
		{
			ItemType = trItemType.trCARSPAWNER;
			stf.MustMatch("(");
			stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tritemid", ()=>{ ParseTrItemID(stf, idx); }),
                new STFReader.TokenProcessor("tritemrdata", ()=>{ TrItemRData(stf); }),
                new STFReader.TokenProcessor("tritemsdata", ()=>{ TrItemSData(stf); })
            });
		}
	}
}
