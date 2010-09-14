/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using MSTS;

namespace ORTS
{
	/// <summary>
	/// Encapsulates the Tiles folder
	/// </summary>
	public class Tiles
	{
		private TileBuffer TileBuffer;

		public Tiles(string folderNameSlash)
		{
			TileBuffer = new TileBuffer(folderNameSlash);
		}

		public float GetElevation(WorldLocation location)
		{
			return GetElevation(location.TileX, location.TileZ, (1024 + location.Location.X) / 8, (1024 - location.Location.Z) / 8);
		}

		public float GetElevation(int tileX, int tileZ, int x, int z)
		{
			// normalize x,y coordinates
			while (x > 255) { x -= 256; ++tileX; }
			while (x < 0) { x += 256; --tileX; }
			while (z > 255) { z -= 256; --tileZ; }
			while (z < 0) { z += 256; ++tileZ; }

			Tile tile = GetTile(tileX, tileZ);

			if (tile != null)
				return tile.GetElevation(x, z);
			else
				return 0;
		}

		public float GetElevation(int tileX, int tileZ, float x, float z)
		{
			// Start with the north west corner.
			int ux = (int)Math.Floor(x);
			int uz = (int)Math.Floor(z);
			float nw = GetElevation(tileX, tileZ, ux, uz);
			float ne = GetElevation(tileX, tileZ, ux + 1, uz);
			float sw = GetElevation(tileX, tileZ, ux, uz + 1);
			float se = GetElevation(tileX, tileZ, ux + 1, uz + 1);
			float e;

			// Condition must match TerrainPatch.SetupPatchIndexBuffer's condition.
			if (((ux & 1) == (uz & 1)))
			{
				// Split NW-SE
				if ((x - ux) > (z - uz))
					// NE side
					e = nw + (ne - nw) * (x - ux) + (se - ne) * (z - uz);
				else
					// SW side
					e = nw + (se - sw) * (x - ux) + (sw - nw) * (z - uz);
			}
			else
			{
				// Split NE-SW
				if ((x - ux) + (z - uz) < 1)
					// NW side
					e = nw + (ne - nw) * (x - ux) + (sw - nw) * (z - uz);
				else
					// SE side
					e = se + (sw - se) * (1 - x + ux) + (ne - se) * (1 - z + uz);
			}

			return e;
		}

		public Tile GetTile(int tileX, int tileZ)
		{
			return TileBuffer.GetTile(tileX, tileZ);
		}

	} // class Tiles

	/// <summary>
	/// This class speeds up access to tiles by caching the ones in the vicinity of 
	/// the most recently used tiles.
	/// </summary>
	public class TileBuffer
	{
		private int bufferTileX, bufferTileZ;  // coordinates of Tile[0,0]
		private const int bufferSize = 8;
		private Tile[,] tileBuffer = new Tile[bufferSize, bufferSize];  // null means we haven't read it yet

		string TileFolderNameSlash;

		/// <summary>
		/// Create the buffer
		/// </summary>
		/// <param name="tileFolderNameSlash"></param>
		public TileBuffer(string tileFolderNameSlash)
		{
			TileFolderNameSlash = tileFolderNameSlash;

			bufferTileX = 0;
			bufferTileZ = 0;
			for (int x = 0; x < bufferSize; ++x)
				for (int z = 0; z < bufferSize; ++z)
					tileBuffer[x, z] = null;
		}

		/// <summary>
		/// Get a tile from the buffer at X,Z
		/// Returns null if their is not tile 
		/// at the specified coordinates.
		/// </summary>
		/// <param name="tileX"></param>
		/// <param name="tileZ"></param>
		/// <returns></returns>
		public Tile GetTile(int tileX, int tileZ)
		{
			// Reposition the buffer if necessary to include these coordinates
			if (!Contains(tileX, tileZ))
				Reposition(tileX, tileZ);

			// If we haven't read the tile yet, then read it.
			Tile tile = GetBuffer(tileX, tileZ);
			if (tile == null)
			{
				tile = new Tile(tileX, tileZ, TileFolderNameSlash);
				int x = tileX - bufferTileX;
				int z = tileZ - bufferTileZ;
				tileBuffer[x, z] = tile;
			}

			return tile;
		}

		/// <summary>
		/// Get the raw buffer contents at the specified coordinates
		/// Returns null if the tile hasn't been read yet.
		/// </summary>
		/// <param name="tileX"></param>
		/// <param name="tileZ"></param>
		/// <returns></returns>
		private Tile GetBuffer(int tileX, int tileZ)
		{
			int x = tileX - bufferTileX;
			int z = tileZ - bufferTileZ;

			return tileBuffer[x, z];
		}

		/// <summary>
		/// Return true if the buffer encloses these coordinates.
		/// </summary>
		/// <param name="tileX"></param>
		/// <param name="tileZ"></param>
		/// <returns></returns>
		private bool Contains(int tileX, int tileZ)
		{
			int x = tileX - bufferTileX;
			int z = tileZ - bufferTileZ;

			if (x < 0 || x >= bufferSize || z < 0 || z >= bufferSize)
				return false;
			else
				return true;
		}

		/// <summary>
		/// Shift the buffer to enclose the specified coordinates.
		/// </summary>
		/// <param name="tileX"></param>
		/// <param name="tileZ"></param>
		private void Reposition(int tileX, int tileZ)
		{
			// Determine the new corner coordinates
			int newBufferTileX = bufferTileX;
			int newBufferTileZ = bufferTileZ;

			if (tileX < bufferTileX)
				newBufferTileX = tileX;
			else if (tileX >= bufferTileX + bufferSize)
				newBufferTileX = tileX - bufferSize + 1;
			if (tileZ < bufferTileZ)
				newBufferTileZ = tileZ;
			else if (tileZ >= bufferTileZ + bufferSize)
				newBufferTileZ = tileZ - bufferSize + 1;

			// Populate the new tile buffer with data from the old buffer
			Tile[,] newTileBuffer = new Tile[bufferSize, bufferSize];
			for (int newx = 0; newx < bufferSize; ++newx)
				for (int newz = 0; newz < bufferSize; ++newz)
				{
					tileX = newBufferTileX + newx;
					tileZ = newBufferTileZ + newz;
					if (Contains(tileX, tileZ))
						newTileBuffer[newx, newz] = GetBuffer(tileX, tileZ);
					else
						newTileBuffer[newx, newz] = null;  // indicates we haven't read it yet
				}

			// Copy the new buffer to the old buffer
			bufferTileX = newBufferTileX;
			bufferTileZ = newBufferTileZ;
			for (int x = 0; x < bufferSize; ++x)
				for (int z = 0; z < bufferSize; ++z)
					tileBuffer[x, z] = newTileBuffer[x, z];

		}
	}

	public class Tile
	{
		public TFile TFile;
		public YFile YFile;

		public bool IsEmpty = true;

		public Tile(int tileX, int tileZ, string TileFolderNameSlash)
		{
			string tileName = TileNameConversion.GetTileNameFromTileXZ(tileX, tileZ);
			string tileFilePath = TileFolderNameSlash + tileName;

			if (File.Exists(tileFilePath + ".t"))
			{
				TFile = new TFile(tileFilePath + ".t");
				YFile = new YFile(tileFilePath + "_y.raw");
				IsEmpty = false;
			}
		}

		public float GetElevation(int x, int z)
		{
			if (IsEmpty)
			{
				return 0;
			}
			else
			{
				uint e = YFile.GetElevationIndex(x, z);
				return (float)e * TFile.Resolution + TFile.Floor;
			}
		}
	}
}
