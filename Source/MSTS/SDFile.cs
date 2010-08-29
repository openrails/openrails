/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;

namespace MSTS
{
    public class SDFile
    {
        public SDShape shape;

        public SDFile()  // use for files with no SD file
        {
            shape = new SDShape();
        }

        public SDFile(string filename)
        {
            STFReader f = new STFReader(filename);
            try
            {
                string token = f.ReadToken();
                while (token != "") // EOF
                {
                    if (token == ")") throw (new STFException(f, "Unexpected )"));
                    else if (token == "(") f.SkipBlock();
                    else if (0 == String.Compare(token, "shape", true)) shape = new SDShape(f);
                    else f.SkipBlock();
                    token = f.ReadToken();
                }
                if (shape == null)
                    throw (new STFException(f, "Missing shape statement"));
            }
            finally
            {
                f.Close();
            }
        }

        public class SDShape
        {
            public SDShape()
            {
                ESD_Bounding_Box = new ESD_Bounding_Box();
            }

            public SDShape(STFReader f)
            {
				try
				{
					while (!f.EOF())
					{
						string token = f.ReadToken();
						if (token == "(")
							token = f.ReadToken();
						if (token.EndsWith(".s") || token.EndsWith(".S")) // Ignore the filename string. TODO: Check if it agrees with the SD file name? Is this important?
						{
							while (token != ")")
							{
								token = f.ReadToken();
								if (token == "") throw (new STFException(f, "Missing )"));
								else if (0 == String.Compare(token, "ESD_Detail_Level", true)) ESD_Detail_Level = f.ReadIntBlock();
								else if (0 == String.Compare(token, "ESD_Alternative_Texture", true)) ESD_Alternative_Texture = f.ReadIntBlock();
								else if (0 == String.Compare(token, "ESD_Bounding_Box", true))
								{
									ESD_Bounding_Box = new ESD_Bounding_Box(f);
									if (ESD_Bounding_Box.A == null || ESD_Bounding_Box.B == null)  // ie quietly handle ESD_Bounding_Box()
										ESD_Bounding_Box = null;
								}
								else if (0 == String.Compare(token, "ESD_No_Visual_Obstruction", true)) ESD_No_Visual_Obstruction = f.ReadBoolBlock();
								else if (0 == String.Compare(token, "ESD_Snapable", true)) ESD_Snapable = f.ReadBoolBlock();
								else f.SkipBlock();
							}
						}
					}
					// TODO - some objects have no bounding box - ie JP2BillboardTree1.sd
					//if( ESD_Bounding_Box == null )throw( new STFError( f, "Missing ESD_Bound_Box statement" ) );
				}
				catch (STFException error)
				{
					STFException.ReportError(f, error.Message);
				}
            }
            public int ESD_Detail_Level = 0;
            public int ESD_Alternative_Texture = 0;
            public ESD_Bounding_Box ESD_Bounding_Box = null;
            public bool ESD_No_Visual_Obstruction = false;
            public bool ESD_Snapable = false;
        }

        public class ESD_Bounding_Box
        {
            public ESD_Bounding_Box() // default used for files with no SD file
            {
                A = new TWorldPosition(-10, -10, -10);
                B = new TWorldPosition(10, 10, 10);
            }

            public ESD_Bounding_Box(STFReader f)
            {
                f.VerifyStartOfBlock();
                if (f.PeekPastWhitespace() == ')')
                    return;    // quietly return on ESD_Bounding_Box()
                float X = f.ReadFloat();
                float Y = f.ReadFloat();
                float Z = f.ReadFloat();
                A = new TWorldPosition(X, Y, Z);
                X = f.ReadFloat();
                Y = f.ReadFloat();
                Z = f.ReadFloat();
                B = new TWorldPosition(X, Y, Z);
                // JP2indirt.sd has extra parameters
                for (; ; )
                {
                    string token = f.ReadToken();
                    if (token == "" || token == ")")
                        break;
                }

            }
            public TWorldPosition A = null;
            public TWorldPosition B = null;
        }
    }
}
