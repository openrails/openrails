/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections;
using System.IO;

namespace MSTS
{
	/// <summary>
	/// Work with consist files, contains an ArrayList of ConsistTrainset
	/// </summary>
	public class CONFile: ArrayList /* of ConsistTrainset */
	{
        public new ConsistTrainset this[int i]
        {
            get { return (ConsistTrainset)base[i]; }
            set { base[i] = value; }
        }
        
        /// <summary>
		/// Open a consist file, 
		/// filePath includes full path and extension
		/// </summary>
		/// <param name="filePath"></param>
		public CONFile( string filePath ): base()
		{
			FileName = Path.GetFileNameWithoutExtension( filePath );
			Description = FileName;


			STFReader inf = new STFReader( filePath );
			try
			{
				while( !inf.EOF() )
				{
					inf.ReadToken();

					if( inf.Tree == "Train(TrainCfg(Name(" )
					{
						Description = inf.ReadToken();
						inf.VerifyEndOfBlock();
					}
					if( inf.Tree == "Train(TrainCfg(Engine(EngineData(" )
					{
						string file = inf.ReadToken();
						string folder = inf.ReadToken();
						inf.VerifyEndOfBlock();
						this.Add( new ConsistTrainset( folder, file, true ) );
					}
					if( inf.Tree == "Train(TrainCfg(Wagon(WagonData(" )
					{
						string file = inf.ReadToken();
						string folder = inf.ReadToken();
						inf.VerifyEndOfBlock();
						this.Add( new ConsistTrainset( folder, file, false ) );
					}
				}
			}
			finally
			{
				inf.Close();
			}

		}

		public string FileName;   // no extension, no path
		public string Description;  // form the Name field or label field of the consist file
	} // CONFile

	public class ConsistTrainset
	{
		public bool IsEngine;
		public string Folder;
		public string File;

		public ConsistTrainset( string folder, string file, bool isEngine )
		{
			Folder = folder;
			File = file;
			IsEngine = isEngine;
		}
	}
}

