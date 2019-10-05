// COPYRIGHT 2013 by the Open Rails project.
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
using System.IO;

namespace ORTS.IO
{
    /// <summary>
    /// A <see cref="Stream"/> which buffers both reads and writes in memory (as a <see cref="MemoryStream"/>).
    /// </summary>
    /// <remarks>
    /// <para>Treat this stream like any other; <see cref="Read"/>, <see cref="Write"/>, <see cref="Seek"/> all work normally (or close enough) in most
    /// cases. There are some specific exceptions, which shouldn't be a problem normally, outlined below.</para>
    /// <para>When reading, if the underlying stream does not report a length itself, the value reported by <see cref="Length"/> represents the amount
    /// buffered. The <see cref="BufferedInMemoryStream"/> attempts to keep the <see cref="Length"/> beyond the current seek location at all times.</para>
    /// <para>when reading, <see cref="Seek"/> will operate normally but only within the data already buffered; attempts to seek beyond this will raise
    /// an exception. There is no way to force a certain amount of data to be buffered.</para>
    /// <para>When writing, <see cref="Flush"/> is a no-op and <see cref="SetLength"/> is not implemented. None of the <see cref="Stream"/> methods and
    /// properties will cause data to be written to the underlying stream.</para>
    /// <para>When writing, to cause all buffered data to be written to the underlying stream, call <see cref="RealFlush"/>. This non-standard behavior
    /// is due to unfortunate existing code which calls <see cref="Flush"/> after writing to a <see cref="Stream"/>, which would otherwise cause us a
    /// problem with write-once underlying streams (which we're specifically trying to support here).</para>
    /// </remarks>
    public class BufferedInMemoryStream : Stream
    {
        MemoryStream Memory;
        Stream Base;
        long WritePosition;
        const int ChunkSize = 1024;

        public BufferedInMemoryStream(Stream stream)
        {
            Memory = new MemoryStream();
            Base = stream;
        }

        public override void Close()
        {
            base.Close();
            Base.Close();
        }

        void ReadChunk(int chunk)
        {
            var buffer = new byte[chunk];
            var bytes = Base.Read(buffer, 0, chunk);
            var oldPosition = Memory.Position;
            Memory.Seek(0, SeekOrigin.End);
            Memory.Write(buffer, 0, bytes);
            Memory.Seek(oldPosition, SeekOrigin.Begin);
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return Base.CanWrite; }
        }

        public override void Flush()
        {
        }

        public void RealFlush()
        {
            if (Memory.Position > WritePosition)
            {
                var currentPosition = Memory.Position;
                Memory.Seek(WritePosition, SeekOrigin.Begin);
                var buffer = new byte[currentPosition - WritePosition];
                Memory.Read(buffer, 0, buffer.Length);
                Base.Write(buffer, 0, buffer.Length);
                WritePosition = currentPosition;
                Memory.Position = currentPosition;
            }

            Base.Flush();
        }

        public override long Length
        {
            get
            {
                if (Base.CanSeek) return Base.Length;
                if (Base.CanRead && (Memory.Position + ChunkSize >= Memory.Length)) ReadChunk(ChunkSize);
                return Memory.Length;
            }
        }

        public override long Position
        {
            get
            {
                return Memory.Position;
            }
            set
            {
                Memory.Position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Memory.Position + count > Memory.Length) ReadChunk(count);
            return Memory.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return Memory.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Memory.Write(buffer, offset, count);
        }
    }
}
