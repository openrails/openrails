/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 
/// Principal Author:
///    Wayne Campbell
/// Contributors:
///    Rick Grout
///     

using System;
using System.Collections;
using System.IO;
using System.Diagnostics;
using MSTSMath;


namespace MSTS
{
    public class WFile
    {
        public int TileX, TileZ;
        public Tr_Worldfile Tr_Worldfile;

        public WFile(string filename)
        {
            // Parse the tile location out of the filename
            int p = filename.ToUpper().LastIndexOf("\\WORLD\\W");
            TileX = int.Parse(filename.Substring(p + 8, 7));
            TileZ = int.Parse(filename.Substring(p + 15, 7));

            using (SBR f = SBR.Open(filename))
            {
                using (SBR block = f.ReadSubBlock())
                {
                    Tr_Worldfile = new Tr_Worldfile( block );
                }
            }
        }
    }

    public class Tr_Worldfile : ArrayList  
    {
        public new WorldObject this[int i]
        {
            get { return (WorldObject)base[i]; }
            set { base[i] = value; }
        }

        public Tr_Worldfile(SBR block)
        {
            block.VerifyID(TokenID.Tr_Worldfile);

            int currentWatermark = 0;

            while (!block.EndOfBlock())
            {
                using (SBR subBlock = block.ReadSubBlock())
                {
                    switch (subBlock.ID)
                    {
                        case TokenID.CollideObject:
                        case TokenID.Static: Add(new StaticObj(subBlock, currentWatermark)); break;
                        case TokenID.TrackObj: Add(new TrackObj(subBlock, currentWatermark)); break;
                        case TokenID.CarSpawner: subBlock.Skip(); break; // TODO
                        case TokenID.Siding: subBlock.Skip(); break; // TODO
                        case TokenID.Forest: // Unicode
                            Add(new ForestObj(subBlock, currentWatermark)); 
                            break;
                        case (TokenID)308: // Binary
                            Add(new ForestObj(subBlock, currentWatermark));
                            break;
                        case TokenID.LevelCr: Add(new StaticObj(subBlock, currentWatermark)); break; // TODO temp code
                        case TokenID.Dyntrack: // Unicode
                            Add(new DyntrackObj(subBlock, currentWatermark, true));
                            break;
                        case (TokenID)306: // Binary
                            Add(new DyntrackObj(subBlock, currentWatermark, false));
                            break;
                        case TokenID.Transfer: subBlock.Skip(); break; // TODO
                        case TokenID.Gantry: Add(new StaticObj(subBlock, currentWatermark)); break; // TODO temp code
                        case TokenID.Pickup: Add(new StaticObj(subBlock, currentWatermark)); break; // TODO temp code
                        case TokenID.Signal: Add(new StaticObj(subBlock, currentWatermark)); break; // TODO temp code
                        case TokenID.Speedpost: Add(new StaticObj(subBlock, currentWatermark)); break; // TODO temp code
                        case TokenID.Tr_Watermark: currentWatermark = subBlock.ReadInt(); break;
                        default: subBlock.Skip(); break;
                    }
                }
            }
        }
    }

    public class StaticObj : WorldObject
    {
        public StaticObj(SBR block, int detailLevel )
        {
            //f.VerifyID(TokenID.Static); it could be CollideObject or Static object

            StaticDetailLevel = detailLevel;

            while (!block.EndOfBlock())
            {
                using (SBR subBlock = block.ReadSubBlock())
                {
                    switch (subBlock.ID)
                    {
                        case TokenID.UiD: UID = subBlock.ReadUInt(); break;
                        case TokenID.FileName: FileName = subBlock.ReadString(); break;
                        case TokenID.Position: Position = new STFPositionItem(subBlock); break;
                        case TokenID.QDirection: QDirection = new STFQDirectionItem(subBlock); break;
                        case TokenID.Matrix3x3: Matrix3x3 = new Matrix3x3(subBlock); break;
                        case TokenID.VDbId: VDbId = subBlock.ReadUInt(); break;
                        default: subBlock.Skip(); break;
                    }
                }
            }
            // TODO verify that we got all needed parameters otherwise null pointer failures will occur
            // TODO, do this for all objects that iterate using a while loop
        }
    }

    public class TrackObj : WorldObject
    {
        public uint SectionIdx;
        public float Elevation;
        public uint CollideFlags;
        public JNodePosn JNodePosn = null;

        public TrackObj(SBR block, int detailLevel )
        {
            block.VerifyID(TokenID.TrackObj);

            StaticDetailLevel = detailLevel;

            while (!block.EndOfBlock())
            {
                using (SBR subBlock = block.ReadSubBlock())
                {
                    switch (subBlock.ID)
                    {
                        case TokenID.UiD: UID = subBlock.ReadUInt(); break;
                        case TokenID.SectionIdx: SectionIdx = subBlock.ReadUInt(); break;
                        case TokenID.Elevation: Elevation = subBlock.ReadFloat(); break;
                        case TokenID.CollideFlags: CollideFlags = subBlock.ReadUInt(); break;
                        case TokenID.FileName: FileName = subBlock.ReadString(); break;
                        case TokenID.StaticFlags: StaticFlags = subBlock.ReadUInt(); break;
                        case TokenID.Position: Position = new STFPositionItem(subBlock); break;
                        case TokenID.QDirection: QDirection = new STFQDirectionItem(subBlock); break;
                        case TokenID.Matrix3x3: Matrix3x3 = new Matrix3x3(subBlock); break;
                        case TokenID.VDbId: VDbId = subBlock.ReadUInt(); break;
                        case TokenID.JNodePosn: JNodePosn = new JNodePosn(subBlock); break;
                        default: subBlock.Skip(); break;
                    }
                }
            }
            // TODO verify that we got all needed parameters otherwise null pointer failures will occur
            // TODO, do this for all objects that iterate using a while loop
        }
    }

    public class DyntrackObj : WorldObject
    {
        public uint SectionIdx;
        public float Elevation;
        public uint CollideFlags;
        public TrackSections trackSections;

        public DyntrackObj(SBR block, int detailLevel, bool isUnicode)
        {
            SBR localBlock = block;
            if (isUnicode)
                localBlock.VerifyID(TokenID.Dyntrack);
            StaticDetailLevel = detailLevel;

            while (!block.EndOfBlock())
            {
                using (SBR subBlock = block.ReadSubBlock())
                {
                    switch (subBlock.ID)
                    {
                        case TokenID.UiD: UID = subBlock.ReadUInt(); break;
                        case TokenID.SectionIdx: SectionIdx = subBlock.ReadUInt(); break;
                        case TokenID.Elevation: Elevation = subBlock.ReadFloat(); break;
                        case TokenID.CollideFlags: CollideFlags = subBlock.ReadUInt(); break;
                        case TokenID.StaticFlags: StaticFlags = subBlock.ReadUInt(); break;
                        case TokenID.Position: Position = new STFPositionItem(subBlock); break;
                        case TokenID.QDirection: QDirection = new STFQDirectionItem(subBlock); break;
                        case TokenID.VDbId: VDbId = subBlock.ReadUInt(); break;
                        case TokenID.TrackSections: trackSections = new TrackSections(subBlock, isUnicode); break;
                        default: subBlock.Skip(); break;
                    }
                }
            }
        }

        public class TrackSections : ArrayList
        {
            public new TrackSection this[int i]
            {
                get { return (TrackSection)base[i]; }
                set { base[i] = value; }
            }

            public TrackSections()
            {
            }

            public TrackSections(SBR block, bool isUnicode)
            {
                block.VerifyID(TokenID.TrackSections);
                int count = 5;
                while (count-- > 0) this.Add(new TrackSection(block.ReadSubBlock(), isUnicode, count));
                block.VerifyEndOfBlock();
            }

        }//TrackSections

        public class TrackSection
        {
            // TrackSection  ==> :SectionCurve :uint,UiD :float,param1 :float,param2
            // SectionCurve  ==> :uint,isCurved
            // eg:  TrackSection (
            //	       SectionCurve ( 1 ) 40002 -0.3 120
            //      )
            // isCurve = 0 for straight, 1 for curved
            // param1 = length (m) for straight, arc (radians) for curved
            // param2 = 0 for straight, radius (m) for curved

            public uint isCurved;
            public uint UiD;
            public float param1;
            public float param2;

            public TrackSection(SBR block, bool isUnicode, int count)
            {
                block.VerifyID(TokenID.TrackSection);
                 // SectionCurve
                {
                    SBR subBlock = block.ReadSubBlock();
                    if (isUnicode)
                    {
                        subBlock.VerifyID(TokenID.SectionCurve);
                        isCurved = block.ReadUInt();
                        subBlock.VerifyEndOfBlock();
                    }
                    else
                    {
                        subBlock.Skip();
                        isCurved = (uint)count % 2;
                    }
                }
                UiD = block.ReadUInt();
                param1 = block.ReadFloat();
                param2 = block.ReadFloat();
                block.VerifyEndOfBlock();
            }
        }//TrackSection
    }//DyntrackObj

    public class ForestObj : WorldObject
    {
        // Variables for use by other classes
        public string TreeTexture;
        public int Population;
        public ScaleRange scaleRange;
        public ForestArea forestArea;
        public TreeSize treeSize;

        public ForestObj(SBR block, int detailLevel)
        {
            SBR localBlock = block;
            StaticDetailLevel = detailLevel;

            while (!block.EndOfBlock())
            {
                using (SBR subBlock = block.ReadSubBlock())
                {
                    switch (subBlock.ID)
                    {
                        case TokenID.UiD: UID = subBlock.ReadUInt(); break;
                        case TokenID.TreeTexture: TreeTexture = subBlock.ReadString(); break;
                        case TokenID.ScaleRange: scaleRange = new ScaleRange(subBlock); break;
                        case TokenID.Area: forestArea = new ForestArea(subBlock); break;
                        case TokenID.Population: Population = subBlock.ReadInt(); break;
                        case TokenID.TreeSize: treeSize = new TreeSize(subBlock); break;
                        case TokenID.StaticFlags: StaticFlags = subBlock.ReadUInt(); break;
                        case TokenID.Position: Position = new STFPositionItem(subBlock); break;
                        case TokenID.QDirection: 
                            QDirection = new STFQDirectionItem(subBlock);
                            // Set B to 0 (straight up) to avoid billboarding problems.
                            //QDirection.B = 0;
                            break;
                        case TokenID.VDbId: VDbId = subBlock.ReadUInt(); break;
                        default: subBlock.Skip(); break;
                    }
                }
            }
        }

        public class ScaleRange
        {
            public float scaleRange1;
            public float scaleRange2;

            public ScaleRange(SBR block)
            {
                block.VerifyID(TokenID.ScaleRange);
                scaleRange1 = block.ReadFloat();
                scaleRange2 = block.ReadFloat();
                block.VerifyEndOfBlock();
            }

        }//ScaleRange

        public class ForestArea
        {
            public float areaDim1;
            public float areaDim2;

            public ForestArea(SBR block)
            {
                block.VerifyID(TokenID.Area);
                areaDim1 = block.ReadFloat();
                areaDim2 = block.ReadFloat();
                block.VerifyEndOfBlock();
            }

        }//ForestArea

        public class TreeSize
        {
            public float treeSize1;
            public float treeSize2;

            public TreeSize(SBR block)
            {
                block.VerifyID(TokenID.TreeSize);
                treeSize1 = block.ReadFloat();
                treeSize2 = block.ReadFloat();
                block.VerifyEndOfBlock();
            }

        }//TreeSize
    }//ForestObj

    public abstract class WorldObject
    {
        public string FileName;
        public uint UID;
        public STFPositionItem Position;
        public STFQDirectionItem QDirection;
        public Matrix3x3 Matrix3x3;
        public int StaticDetailLevel = 0;
        public uint StaticFlags = 0;
        public uint VDbId;
    }


	public class STFPositionItem: TWorldPosition
	{
		public STFPositionItem( TWorldPosition p ): base( p )
		{
		}

		public STFPositionItem( SBR block )
		{
            block.VerifyID(TokenID.Position);
			X = block.ReadFloat();
			Y = block.ReadFloat();
			Z = block.ReadFloat();
            block.VerifyEndOfBlock();
		}
	}
	
	public class STFQDirectionItem: TWorldDirection
	{
		public STFQDirectionItem( TWorldDirection d ): base( d )
		{
		}

		public STFQDirectionItem( SBR block )
		{
            block.VerifyID(TokenID.QDirection);
			A = block.ReadFloat();
			B = block.ReadFloat();
			C = block.ReadFloat();
			D = block.ReadFloat();
            block.VerifyEndOfBlock();
		}
    }

	public class Matrix3x3
	{
		public Matrix3x3( SBR block )
		{
            block.VerifyID(TokenID.Matrix3x3);
			AX = block.ReadFloat();
			AY = block.ReadFloat();
			AZ = block.ReadFloat();
			BX = block.ReadFloat();
			BY = block.ReadFloat();
			BZ = block.ReadFloat();
			CX = block.ReadFloat();
			CY = block.ReadFloat();
			CZ = block.ReadFloat();
            block.VerifyEndOfBlock();
		}
		public float AX,AY,AZ, BX,BY,BZ, CX,CY,CZ;
	}

    public class JNodePosn
    {
        public int TileX, TileZ;
        public float X, Y, Z;

        public JNodePosn(SBR block)
        {
            block.VerifyID(TokenID.JNodePosn);
            TileX = block.ReadInt();
            TileZ = block.ReadInt();
            X = block.ReadFloat();
            Y = block.ReadFloat();
            Z = block.ReadFloat();
            block.VerifyEndOfBlock();
        }
    }

	public class TWorldDirection
	{
		public float A;
		public float B;
		public float C;
		public float D;
		public TWorldDirection( float a, float b, float c, float d ){ A =a; B = b; C = c; D = d;}
		public TWorldDirection(){ A = 0; B = 0; C = 0; D = 1; }
		public TWorldDirection( TWorldDirection d ){ A=d.A; B=d.B; C=d.C; D=d.D; }

		public void SetBearing( float compassRad )
		{
			float slope = GetSlope();
			SetAngles( compassRad, slope );
		}

		public void SetBearing( float dx, float dz )
		{
			float slope = GetSlope();
			float compassRad = M.AngleDxDz( dx, dz );
			SetAngles( compassRad, slope );
		}

		public void Rotate( float radians )  // Rotate around world vertical axis - +degrees is clockwise
			// This rotates about the surface normal
		{
			SetBearing( GetBearing()+radians );
		}

		public void Pivot( float radians )	// This rotates about object Y axis
		{
			radians += GetBearing();
			float slope = GetSlope();
			SetAngles( radians, -slope );
		}

		public void SetAngles( float compassRad, float tiltRad )  // + rad is tilted up or rotated east
			// from http://www.euclideanspace.com/maths/geometry/rotations/conversions/eulerToQuaternion/
			/*
			 *  w = Math.sqrt(1.0 + C1 * C2 + C1*C3 - S1 * S2 * S3 + C2*C3) / 2
				x = (C2 * S3 + C1 * S3 + S1 * S2 * C3) / (4.0 * w) 
				y = (S1 * C2 + S1 * C3 + C1 * S2 * S3) / (4.0 * w)
				z = (-S1 * S3 + C1 * S2 * C3 + S2) /(4.0 * w) 


				where:

				C1 = cos(heading) 
				C2 = cos(attitude) 
				C3 = cos(bank) 
				S1 = sin(heading) 
				S2 = sin(attitude) 
				S3 = sin(bank)     it seems in MSTS - tilt forward back is bank
				
			Applied in order of heading, attitude then bank 
			*/

		{
			float a1 = compassRad;
			float a2 = 0;   
			float a3 = tiltRad;

            float C1 = (float)Math.Cos(a1);
            float S1 = (float)Math.Sin(a1);
            float C2 = (float)Math.Cos(a2);
            float S2 = (float)Math.Sin(a2);
            float C3 = (float)Math.Cos(a3);
            float S3 = (float)Math.Sin(a3);

            float w = (float)Math.Sqrt(1.0 + C1 * C2 + C1 * C3 - S1 * S2 * S3 + C2 * C3) / 2.0f;
			float x;
			float y;
			float z;

			if ( Math.Abs( w ) < .000005 )
			{
				A = 0.0f;
				B = -1.0f;
				C = 0.0f;
				D = 0.0f;
			}
			else
			{
                x = (float)(-(C2 * S3 + C1 * S3 + S1 * S2 * C3) / (4.0 * w));
                y = (float)(-(S1 * C2 + S1 * C3 + C1 * S2 * S3) / (4.0 * w));
                z = (float)(-(-S1 * S3 + C1 * S2 * C3 + S2) / (4.0 * w));

				A = x;
				B = y;
				C = z; 
				D = w;
			}
		}

		public void SetSlope( float tiltRad ) // +v is tilted up
		{
			float compassAngleRad = M.AngleDxDz( DX(), DZ() );
			SetAngles( compassAngleRad, tiltRad );
		}

		public void Tilt( float radians )   // Tilt up the specified number of radians
		{
			SetSlope( GetSlope() + radians );
		}

		public void MakeLevel()  // Remove any tilt from the direction.
		{
			SetSlope( 0 );
		}
		public float DY()
		{	

			float x = -A; // imaginary i part of quaternion
			float y = -B; // imaginary j part of quaternion
			float z = -C; // imaginary k part of quaternion
			float w =  D; // real part of quaternionfloat 
			

			//From http://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToEuler/
			//p2.x = ( w*w*p1.x + 2*y*w*p1.z - 2*z*w*p1.y + x*x*p1.x + 2*y*x*p1.y + 2*z*x*p1.z - z*z*p1.x - y*y*p1.x );	
			//p2.y = ( 2*x*y*p1.x + y*y*p1.y + 2*z*y*p1.z + 2*w*z*p1.x - z*z*p1.y + w*w*p1.y - 2*x*w*p1.z - x*x*p1.y );	
			//p2.z = ( 2*x*z*p1.x + 2*y*z*p1.y + z*z*p1.z - 2*w*y*p1.x - y*y*p1.z + 2*w*x*p1.y - x*x*p1.z + w*w*p1.z );

			float dy = ( 2*z*y - 2*x*w );	
			return dy;
		}

		public float DX()
		{

			// WAS return -2.0*B*D; 
			
			/* Was
			float x = C;
			float y = A;
			float z = B;
			float w = D;

			return -2.0 * ( x * y + z * w );
			*/

			float x = -A;
			float y = -B;
			float z = -C;
			float w = D;

			float dX = ( 2*y*w + 2*z*x );	
			return dX;

		}
		public float DZ()
		{ 
			float x = -A;
			float y = -B;
			float z = -C;
			float w = D;

			return z*z - y*y - x*x + w*w;

		}
		public float GetSlope( )   // Return the slope, +radians is tilted up
		{
			// see http://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToEuler/

			float qx = -A;
			float qy = -B;
			float qz = -C;
			float qw = D;

			//float heading;
			//float attitude;
			float bank;

			if( Math.Abs( qx*qy + qz*qw - 0.5 ) < .00001 ) 
			{
				//heading = 2 * Math.Atan2(qx,qw);
				bank = 0;
			}
			else if( Math.Abs( qx*qy + qz*qw + 0.5 ) < .00001 )
			{
				//heading = -2 * Math.Atan2(qx,qw);
				bank = 0;
			}

			//heading = Math.Atan2(2*qy*qw-2*qx*qz , 1 - 2*qy*qy - 2*qz*qz);
			//attitude = Math.Asin(2*qx*qy + 2*qz*qw);
            bank = (float)Math.Atan2(2 * qx * qw - 2 * qy * qz, 1 - 2 * qx * qx - 2 * qz * qz);

			return bank;
		}

		/* OLD METHOD
		public float GetBearing( )  // Return the compass bearing +radians is east of north
		{
			return M.AngleDxDz( DX(), DZ() );
		}
		*/ 

		public float GetBearing( )   // Return the bearing
		{
			// see http://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToEuler/

			float qx = -A;
			float qy = -B;
			float qz = -C;
			float qw = D;

			float heading;
			//float attitude;
			//float bank;

			if( Math.Abs( qx*qy + qz*qw - 0.5 ) < .00001 ) 
			{
                heading = 2f * (float)Math.Atan2(qx, qw);
				//bank = 0;
			}
			else if( Math.Abs( qx*qy + qz*qw + 0.5 ) < .00001 )
			{
                heading = -2f * (float)Math.Atan2(qx, qw);
				//bank = 0;
			}
			else
			{
                heading = (float)Math.Atan2(2 * qy * qw - 2 * qx * qz, 1 - 2 * qy * qy - 2 * qz * qz);
				//attitude = Math.Asin(2*qx*qy + 2*qz*qw);
				//bank = Math.Atan2(2*qx*qw-2*qy*qz , 1 - 2*qx*qx - 2*qz*qz);
			}

			return heading;
		}

		public static float AngularDistance( TWorldDirection d1, TWorldDirection d2 )
			// number of radians separating angle one and angle two - always positive
		{
			float a1 = d1.GetBearing();
			float a2 = d2.GetBearing();

			float a = a1-a2;

			a = Math.Abs( a );

			while( a > Math.PI )
				a -= 2.0f*(float)Math.PI;

			return (float)Math.Abs(a);
		}

		/// <summary>
		/// Rotate the specified point in model space to a new location according to the quaternion 
		/// Center of rotation is 0,0,0 in model space
		/// Example   xyz = 0,1,2 rotated 90 degrees east becomes 2,1,0
		/// </summary>
		/// <param name="p1"></param>
		private TWorldPosition RotatePoint( TWorldPosition p1 )
		{

			float x = -A; // imaginary i part of quaternion
			float y = -B; // imaginary j part of quaternion
			float z = -C; // imaginary k part of quaternion
			float w =  D; // real part of quaternionfloat 
			
			TWorldPosition p2 = new TWorldPosition();

			p2.X = ( w*w*p1.X + 2*y*w*p1.Z - 2*z*w*p1.Y + x*x*p1.X + 2*y*x*p1.Y + 2*z*x*p1.Z - z*z*p1.X - y*y*p1.X );	
			p2.Y = ( 2*x*y*p1.X + y*y*p1.Y + 2*z*y*p1.Z + 2*w*z*p1.X - z*z*p1.Y + w*w*p1.Y - 2*x*w*p1.Z - x*x*p1.Y );	
			p2.Z = ( 2*x*z*p1.X + 2*y*z*p1.Y + z*z*p1.Z - 2*w*y*p1.X - y*y*p1.Z + 2*w*x*p1.Y - x*x*p1.Z + w*w*p1.Z );
			
			return p2;
		}

	}

	public class TWorldPosition
	{
		public float X;
		public float Y;
		public float Z;
		public TWorldPosition( float x, float y, float z ){ X = x; Y = y; Z = z;}
		public TWorldPosition( ){ X = 0.0f; Y = 0.0f; Z = 0.0f; }
		public TWorldPosition( TWorldPosition p )
		{
			X = p.X;
			Y = p.Y;
			Z = p.Z;
		}

		public void Move( TWorldDirection q, float distance )
		{

			X += ( q.DX() * distance );
			Y += ( q.DY() * distance );
			Z += ( q.DZ() * distance );
		}

		public void Offset( TWorldDirection d, float distanceRight )
		{
			TWorldDirection DRight = new TWorldDirection( d );
			DRight.Rotate( M.Radians( 90 ) );
			Move( DRight, distanceRight ); 
		}

		public static float PointDistance( TWorldPosition p1, TWorldPosition p2 )
			// distance between p1 and p2 along the surface
		{
			float dX = p1.X - p2.X;
			float dZ = p1.Z - p2.Z;
            return (float)Math.Sqrt(dX * dX + dZ * dZ);
		}
	}

}