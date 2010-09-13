///
/// Description will come here soon.
/// GeorgeS
/// 
/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
//#define DEBUGSCR
//#define DEBUGOPENCLOSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ORTS
{
    // Overriden file factory to provide overriden Stream
    public class WAVIrrKlangFileFactory : IrrKlang.IFileFactory
    {
        // Place to store our streams, needed to control the behavior of the looping
        public static Dictionary<string, WAVFileStream> FileStreams = new Dictionary<string, WAVFileStream>();

        private static Dictionary<string, MemoryStream> _Streams = new Dictionary<string, MemoryStream>();
        private static Dictionary<long, long> _Positions = new Dictionary<long, long>();

        /// <summary>
        /// IFileFactory Interface memeber to open the given file
        /// </summary>
        /// <param name="filename">Name of the file</param>
        /// <returns>The opened stream</returns>
        public System.IO.Stream openFile(String filename)
        {
            // Separate SoundCommand UID from filename
            int sep = filename.LastIndexOf('*');
            WAVFileStream wfs;

            bool isLooping = false;
            bool isInternalLoop = false;

            // Don't keep the previous instance, release it
            if (sep != 0 && FileStreams.Keys.Contains(filename))
            {
                wfs = FileStreams[filename];
                isLooping = wfs.IsLooping;
                isInternalLoop = wfs.IsInternalLoop;
#if DEBUGSCR
                Console.WriteLine("Implicit stopping file: " + filename.Substring(filename.LastIndexOf('\\')));
#endif
                wfs.Release();
            }

            // Create the new Stream
            wfs = new WAVFileStream(sep >= 0 ? filename.Substring(0, sep) : filename);

            wfs.IsLooping = isLooping;
            wfs.IsInternalLoop = isInternalLoop;

            // Add or replace the Stream in our Dictionary
            if (FileStreams.Keys.Contains(filename))
            {
#if DEBUGSCR
                Console.WriteLine("Replacing file: " + filename.Substring(filename.LastIndexOf('\\')));
#endif
                FileStreams[filename] = wfs;
            }
            else
            {
#if DEBUGSCR
                Console.WriteLine("Adding file: " + filename.Substring(filename.LastIndexOf('\\')));
#endif
                FileStreams.Add(filename, wfs);
            }

            return wfs;
        }

        /// <summary>
        /// Check if a sound file is still playing
        /// </summary>
        /// <param name="FileName">Name of the file</param>
        /// <returns>True if still playing</returns>
        public static bool isPlaying(string FileName)
        {
            if (FileStreams.Keys.Contains(FileName))
            {
                return FileStreams[FileName].isPlaying;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to find a suitable number to represent the Length
        /// </summary>
        /// <param name="FileName">Name of the file</param>
        /// <returns>The relative weight</returns>
        public static int Weigth(string FileName)
        {
            if (FileStreams.Keys.Contains(FileName))
            {
                WAVFileStream wfs = FileStreams[FileName];
                int x = (int)Math.Log10(wfs.LoopCount * wfs.LoopedLength);
                x += 5;
#if DEBUGSCR
                Console.WriteLine("Stopping X is: " + x.ToString());
#endif
                return x;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Provide error free call to BeginLoop on Stream
        /// </summary>
        /// <param name="FileName">Name of the file, also key in dictionary</param>
        public static void StartLoopRelease(string FileName)
        {
            if (!FileStreams.Keys.Contains(FileName))
            {
                int sep = FileName.LastIndexOf('*');
                string PureName = sep >= 0 ? FileName.Substring(0, sep) : FileName;
                if (File.Exists(PureName))
                {
                    WAVFileStream wfs = new WAVFileStream(PureName);
                    FileStreams.Add(FileName, wfs);
                }
            }

            if (FileStreams.Keys.Contains(FileName))
            {
#if DEBUGSCR
                Console.WriteLine("Start Loop Release: " + FileName.Substring(FileName.LastIndexOf('\\')));
#endif
                FileStreams[FileName].StartLoop();
            }
        }

        /// <summary>
        /// Provide error free call to BeginLoop on Stream
        /// </summary>
        /// <param name="FileName">Name of the file, also key in dictionary</param>
        public static void StartLoop(string FileName)
        {
            if (!FileStreams.Keys.Contains(FileName))
            {
                int sep = FileName.LastIndexOf('*');
                string PureName = sep >= 0 ? FileName.Substring(0, sep) : FileName;
                if (File.Exists(PureName))
                {
                    WAVFileStream wfs = new WAVFileStream(PureName);
                    FileStreams.Add(FileName, wfs);
                }
            }

            if (FileStreams.Keys.Contains(FileName))
            {
#if DEBUGSCR
                Console.WriteLine("Start Loop: " + FileName.Substring(FileName.LastIndexOf('\\')));
#endif
                FileStreams[FileName].StartLoop();
                FileStreams[FileName].IsInternalLoop = false;
            }
        }

        /// <summary>
        /// Provide error free call to Release on Stream
        /// </summary>
        /// <param name="FileName">Name of the file, also key in dictionary</param>
        public static void Release(string FileName)
        {
            if (FileStreams.Keys.Contains(FileName))
            {
#if DEBUGSCR
                Console.WriteLine("Release: " + FileName.Substring(FileName.LastIndexOf('\\')));
#endif
                FileStreams[FileName].Release();
            }
        }

        /// <summary>
        /// Provide error free call to ReleaseWithJump on Stream
        /// </summary>
        /// <param name="FileName">Name of the file, also key in dictionary</param>
        public static void ReleaseWithJump(string FileName)
        {
            if (FileStreams.Keys.Contains(FileName))
            {
#if DEBUGSCR
                Console.WriteLine("Release with Jump: " + FileName.Substring(FileName.LastIndexOf('\\')));
#endif
                FileStreams[FileName].ReleaseWithJump();
            }
        }

        public static void EnsureStream(string FileName)
        {
            if (!_Streams.Keys.Contains(FileName))
            {
#if DEBUGOPENCLOSE
                Console.WriteLine("(Opening file: " + FileName.Substring(FileName.LastIndexOf('\\')) + ")");
#endif
                FileStream fs = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                MemoryStream ms = new MemoryStream();
                ms.SetLength(fs.Length);
                fs.Read(ms.GetBuffer(), 0, (int)fs.Length);
                ms.Flush();
                fs.Close();
                ms.Seek(0, SeekOrigin.Begin);
                _Streams.Add(FileName, ms);
            }
        }

        public static MemoryStream PrepareStream(string FileName, long hndl)
        {
            MemoryStream s;
            if (_Positions.Keys.Contains(hndl))
            {
                s = _Streams[FileName];
                long p = _Positions[hndl];
                s.Seek(p, SeekOrigin.Begin);
                return s;
            }
            else
            {
                EnsureStream(FileName);
                _Positions.Add(hndl, 0);
                s = _Streams[FileName];
                s.Seek(0, SeekOrigin.Begin);
                return s;
            }
        }

        public static void SavePos(string FileName, long hndl)
        {
            MemoryStream s;
            if (_Positions.Keys.Contains(hndl))
            {
                s = _Streams[FileName];
                long p = s.Position;
                _Positions[hndl] = p;
            }
            else
            {
                _Positions.Add(hndl, 0);
            }
        }
    }
    
    /// <summary>
    /// File Stream implementation to provide CUE markers and looping functionality
    /// </summary>
    public class WAVFileStream : Stream
    {
        private string Name;
        private MemoryStream _ms;
        private long ID;
        private static long lastID = 0;

        // Stored positions, may handle just two markers
        private long _AbsoluteBeginPosition = 0;
        private long _Marker1Position = 0;
        private long _Marker2Position = 0;
        private long _InternalLength = 0;

        private bool _isNextLength = false;

        // Keeps info about how to exit from playing when in loop mode
        private bool _isShouldFinish;
        private bool _isInInternalLoop;

        // Bytes per second
        private int _BPS = 2;
        // Samples per second
        private int _SPS = 2;

        public bool isClosed = false;
        public bool isPlaying = true;

        public int LoopCount;
        
        /// <summary>
        /// Contructor, resets loop information, also calls base constructor
        /// </summary>
        /// <param name="filename">Name of the file to be streamed</param>
        public WAVFileStream(String filename)
//            : base(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
        {
            Name = filename;
#if DEBUGOPENCLOSE1
            Console.WriteLine("(Using file: " + this.Name.Substring(this.Name.LastIndexOf('\\')) + ")");
#endif
            _isShouldFinish = true;
            _isInInternalLoop = false;
            LoopCount = 1;
            WAVIrrKlangFileFactory.EnsureStream(Name);
            ID = lastID++;
            WAVIrrKlangFileFactory.SavePos(Name, ID);
        }

        public override void Close()
        {
#if DEBUGOPENCLOSE1
            Console.WriteLine("(Deusing file: " + this.Name.Substring(this.Name.LastIndexOf('\\')) + ")");
#endif
            isClosed = true;
            isPlaying = false;
            base.Close();
        }

        /// <summary>
        /// Gets looped length
        /// </summary>
        public long LoopedLength
        {
            get
            {
                return _InternalLength;
            }
        }

        /// <summary>
        /// Overriden Read, it handles basic info reading, also emulate loop
        /// </summary>
        /// <param name="array">Where to read bytes</param>
        /// <param name="offset">Offset in array</param>
        /// <param name="count">Number of bytes to read</param>
        /// <returns></returns>
        public override int Read(byte[] array, int offset, int count)
        {
            _ms = WAVIrrKlangFileFactory.PrepareStream(Name, ID);
            int rdb = 0;
            byte[] preread = new byte[4];

            try
            {

                // Check the overread case, if trying to read after the second marker or the end of the file
                if (LoopedEndPosition != 0 && (_ms.Position + count > LoopedEndPosition))
                {
                    // Just for sure, reverse check, if already overrun
                    if (LoopedEndPosition - _ms.Position > 0)
                        rdb = _ms.Read(array, offset, (int)(LoopedEndPosition - _ms.Position));
                }
                else
                {
                    if (count < 4)
                    {
#if DEBUGSCR
                        Console.WriteLine("(Now using preread on file {0})", this.Name.Substring(this.Name.LastIndexOf('\\')));
#endif
                        rdb = _ms.Read(preread, 0, 4);
                        _ms.Seek(4 - count, SeekOrigin.Current);
                        for (int i = 0; i < count; i++)
                        {
                            array[i] = preread[i];
                        }
                    }
                    else
                    {
                        // May read without problems
                        rdb = _ms.Read(array, offset, count);
                        for (int i = 0; i < (rdb >= 4 ? 4 : count); i++)
                        {
                            preread[i] = array[i];
                        }
                    }
                }

                // Length operation functions and CUE read
                #region Length and CUE operations
                // Previously set flag if the length of the wav will be the next info
                if (_isNextLength)
                {
#if DEBUGSCR
                    Console.WriteLine("Using file: " + Name.Substring(Name.LastIndexOf('\\')));
#endif
                    // Store the original legth
                    _InternalLength = FromArray(array);

                    // Set the provided length to an enough high number - it's more than six hours
                    array[0] = 0;
                    array[1] = 0;
                    array[2] = 0;
                    array[3] = 0xF0;
                    // Reset flag and store the positions
                    // Also set markers to default
                    _isNextLength = false;
                    _AbsoluteBeginPosition = _ms.Position;
                    _Marker1Position = 0;
                    _Marker2Position = _InternalLength;

                    // Load cue info
                    FindCUE();
                }
                // data, nex is length
                //else if (count == 4 && array[0] == 100 && array[1] == 97 && array[2] == 116 && array[3] == 97)
                else if (preread[0] == 100 && preread[1] == 97 && preread[2] == 116 && preread[3] == 97)
                {
                    _isNextLength = true;
                }
                // fmt, may read the BPS
                else if (preread[0] == 102 && preread[1] == 109 && preread[2] == 116)
                {
                    long pos = _ms.Position;
                    _ms.Seek(6, SeekOrigin.Current);
                    _SPS = (int)FromReadArray(2);
                    _ms.Seek(10, SeekOrigin.Current);
                    _BPS = (int)FromReadArray(2) / 8;
                    _ms.Seek(pos, SeekOrigin.Begin);
                }
                #endregion

                // Check if loop, if is, read the rest to the buffer from the begining
                // If not, it is the end of the loop, so no more data
                while (count > rdb && !_isShouldFinish)
                {
                    LoopCount++;
                    _ms.Seek(BeginPosition, SeekOrigin.Begin);
                    //rdb += base.Read(array, offset + rdb, count - rdb);
                    // Check the overread case, if trying to read after the second marker or the end of the file
                    if (LoopedEndPosition != 0 && (_ms.Position + (count - rdb) > LoopedEndPosition))
                    {
                        // Just for sure, reverse check, if already overrun
                        if (LoopedEndPosition - _ms.Position > 0)
                            rdb += _ms.Read(array, offset + rdb, (int)(LoopedEndPosition - _ms.Position));
                    }
                    else
                    {
                        // May read without problems
                        rdb += _ms.Read(array, offset + rdb, count - rdb);
                    }
                }

                if (count > rdb && _isShouldFinish && _ms.Position < LoopedEndPosition)
                {
                    rdb += _ms.Read(array, offset + rdb, (int)(LoopedEndPosition - _ms.Position));
                }

                isPlaying = rdb != 0;

                // return the read bytes number, if less than expected, it will indicate the end of the stream
                return rdb;
            }
            catch (Exception e)
            {
#if DEBUGSCR
                Console.WriteLine("Exception caught on file {0}.", this.Name.Substring(this.Name.LastIndexOf('\\')));
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
#endif
                return rdb;
            }
            finally
            {
                WAVIrrKlangFileFactory.SavePos(Name, ID);
            }
        }

        /// <summary>
        /// Indicates if the Stream is in internal - between markers - loop
        /// </summary>
        public bool IsInternalLoop
        {
            get
            {
                return _isInInternalLoop;
            }
            set
            {
                _isInInternalLoop = value;
            }
        }

        public bool IsLooping
        {
            get
            {
                return !_isShouldFinish;
            }
            set
            {
                _isShouldFinish = !value;
            }
        }

        /// <summary>
        /// Gets the begin position in file, absolute or marker
        /// </summary>
        private long BeginPosition
        {
            get
            {
                return _isInInternalLoop ? _Marker1Position + _AbsoluteBeginPosition : _AbsoluteBeginPosition;
            }
        }

        /// <summary>
        /// Gets the end position, marker or the absolute end
        /// </summary>
        private long LoopedEndPosition
        {
            get
            {
                return (_isInInternalLoop ? _Marker2Position + _AbsoluteBeginPosition : _InternalLength);
            }
        }

        /// <summary>
        /// Sets loop begin, between markers
        /// </summary>
        public void StartLoop()
        {
            _isShouldFinish = false;
            _isInInternalLoop = true;
        }
        
        /// <summary>
        /// Set loop end, between markers
        /// </summary>
        public void Release()
        {
            _isShouldFinish = true;
        }

        /// <summary>
        /// Set loop end, between marker and end of file
        /// </summary>
        public void ReleaseWithJump()
        {
            _isInInternalLoop = false;
            _isShouldFinish = true;
        }

        /// <summary>
        /// Gets long from array of bytes
        /// </summary>
        /// <param name="array">byte array</param>
        /// <returns>The converted long</returns>
        protected internal long FromArray(byte[] array)
        {
            return array[0] + (array[1] << 8) + (array[2] << 16) + (array[3] << 24);
        }

        /// <summary>
        /// Tries to read long from file
        /// </summary>
        /// <param name="len">Length of the long</param>
        /// <returns></returns>
        protected internal long? FromReadArray(int len)
        {
            byte[] array = new byte[4];
            int rd = _ms.Read(array, 0, len);
            if (rd == -1)
                return null;
            return FromArray(array);
        }

        /// <summary>
        /// Finds CUE points, read the information
        /// </summary>
        private void FindCUE()
        {
            byte[] array = new byte[4];
            long curpos = Position;
            int rd;
            long? tmp;

            // Seek to end of the data chunk
            _ms.Seek(_InternalLength + _AbsoluteBeginPosition, SeekOrigin.Begin);
            rd = _ms.Read(array, 0, 4);

            // Read until find cue chunk or eof
            while (rd != 0 && !(array[0] == 99 && array[1] == 117 && array[2] == 101) )
            {
                tmp = FromReadArray(4);
                if (tmp == null)
                {
                    rd = 0;
                    break;
                }
                _ms.Seek(tmp.Value, SeekOrigin.Current);
                rd = _ms.Read(array, 0, 4);
            }

            // cue chunk found
            if (rd != 0)
            {
                // Read cue point number
                _ms.Seek(4, SeekOrigin.Current);
                tmp = FromReadArray(4);

                List<CUE> lc = new List<CUE>();

                // Read all cue
                while (tmp > 0)
                {
                    lc.Add(new CUE(this, _BPS, _SPS));

                    tmp--;
                }

                // If more than one cue, try to find suitable cues
                if (lc.Count > 1)
                {
                    var q = (from CUE cue in lc
                             orderby cue.Order ascending, cue.Position ascending
                             select cue).ToList();

                    //_Marker1Position = q[q.Count - 2].Position;
                    //_Marker2Position = q[q.Count - 1].Position;
                    _Marker1Position = q.First().Position;
                    _Marker2Position = q.Last().Position;
                }
            }

            // Return to the saved position, the irr wants load from there
            _ms.Seek(curpos, SeekOrigin.Begin);
        }

        public override bool CanRead
        {
            get { return WAVIrrKlangFileFactory.PrepareStream(Name, ID).CanRead; }
        }

        public override bool CanSeek
        {
            get { return WAVIrrKlangFileFactory.PrepareStream(Name, ID).CanSeek; }
        }

        public override bool CanWrite
        {
            get { throw new NotImplementedException(); }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Length
        {
            get
            {
                MemoryStream ms = WAVIrrKlangFileFactory.PrepareStream(Name, ID);
                return ms.Length;
            }
        }

        public override long Position
        {
            get
            {
                MemoryStream ms = WAVIrrKlangFileFactory.PrepareStream(Name, ID);
                return ms.Position;
            }
            set
            {
                MemoryStream ms = WAVIrrKlangFileFactory.PrepareStream(Name, ID);
                ms.Position = value;
                WAVIrrKlangFileFactory.SavePos(Name, ID);
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            MemoryStream ms = WAVIrrKlangFileFactory.PrepareStream(Name, ID);
            long p = ms.Seek(offset, origin);
            WAVIrrKlangFileFactory.SavePos(Name, ID);
            return p;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Representing a CUE
    /// </summary>
    class CUE
    {
        // CUE data we need
        public long Position = 0;
        public long ID = 0;
        public long Order = 0;

        public CUE(WAVFileStream fs, int BPS, int SPS)
        {
            long? tmp;
            long tmpp;

            // Fail safe reading from file - ID, Order and Position
            tmp = fs.FromReadArray(4);
            if (tmp.HasValue)
                ID = tmp.Value;
            else
                return;

            tmp = fs.FromReadArray(4);
            if (tmp.HasValue)
                Order = tmp.Value;
            else
                return;

            //fs.Seek(4, SeekOrigin.Current);
            fs.FromReadArray(4);
            tmp = fs.FromReadArray(4);
            if (tmp.HasValue)
                tmpp = tmp.Value;
            else
                return;
            tmp = fs.FromReadArray(4);
            if (tmp.HasValue)
                tmpp += tmp.Value;
            else
                return;
            tmp = fs.FromReadArray(4);
            if (tmp.HasValue)
                tmpp += tmp.Value;
            else
                return;

            tmpp *= BPS * SPS;
            Position = tmpp;
        }
    }
}
