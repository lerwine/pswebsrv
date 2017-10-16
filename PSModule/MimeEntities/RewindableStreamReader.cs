using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Erwine.Leonard.T.PSWebSrv.MimeEntities
{
    public class RewindableStreamReader : TextReader
    {
        public class ValueBuffer<T>
            where T : struct, IComparable<T>, IEquatable<T>, IComparable, IConvertible
        {
            private object _syncRoot = new object();
            private T[] _buffer;
            private int _readPosition = 0;
            private int _count = 0;

            public int Capacity { get { return _buffer.Length; } } 
            
            public int Count { get { return _count; } } 
            
            public int Free
            {
                get
                {
                    Monitor.Enter(_syncRoot);
                    try { return _buffer.Length - _count; }
                    finally { Monitor.Exit(_syncRoot); }
                }
            }
            
            public ValueBuffer(int size)
            {
                _buffer = new T[(size < MinBufferSize) ? MinBufferSize : size];
            }

            public T[] Peek(int count)
            {
                Monitor.Enter(_syncRoot);
                try { return _Peek(count).ToArray(); }
                finally { Monitor.Exit(_syncRoot); }
            }

            private IEnumerable<T> _Peek(int count)
            {
                int c = _count;
                if (count < 1 || c == 0)
                    yield break;
                if (count > c)
                    count = c;
                int start = _readPosition;
                int end = start + count;
                if (end >= _buffer.Length)
                    end -= _buffer.Length;
                if (start < end)
                {
                    for (int i = start; i < end; i++)
                        yield return _buffer[i];
                }
                else
                {
                    for (int i = start; i < _buffer.Length; i++)
                        yield return _buffer[i];
                    for (int i = 0; i < end; i++)
                        yield return _buffer[i];
                }
            }

            public T[] Read(int count)
            {
                Monitor.Enter(_syncRoot);
                try { return _Read(count).ToArray(); }
                finally { Monitor.Exit(_syncRoot); }
            }

            private IEnumerable<T> _Read(int count)
            {
                int c = _count;
                if (count < 1 || c == 0)
                    yield break;
                if (count > c)
                    count = c;
                int start = _readPosition;
                int end = start + count;
                if (end >= _buffer.Length)
                    end -= _buffer.Length;
                if (start < end)
                {
                    for (int i = start; i < end; i++)
                        yield return _buffer[i];
                }
                else
                {
                    for (int i = start; i < _buffer.Length; i++)
                        yield return _buffer[i];
                    for (int i = 0; i < end; i++)
                        yield return _buffer[i];
                }

                _readPosition = end;
                _count -= count;
            }

            public int Purge(int count)
            {
                Monitor.Enter(_syncRoot);
                try
                {
                    if (count < 1 || _count == 0)
                        return 0;
                    if (count > _count)
                        count = _count;
                    int end = _readPosition + count;
                    if (end >= _buffer.Length)
                        end -= _buffer.Length;

                    _readPosition = end;
                    _count -= count;
                }
                finally { Monitor.Exit(_syncRoot); }

                return count;
            }

            public int Write(params T[] values) { return Write(values as IEnumerable<T>); }

            public int Write(int count, params T[] values) { return Write(values as IEnumerable<T>, count); }

            public int Write(IEnumerable<T> values, int count = -1)
            {
                if (values == null || count == 0)
                    return 0;
                int c = 0;
                Monitor.Enter(_syncRoot);
                try
                {
                    int r = _buffer.Length - _count;
                    if (r == 0)
                        return 0;
                    if (count > 0 && count < r)
                        r = count;
                    int end = _readPosition + _count;
                    if (end >= _buffer.Length)
                        end -= _buffer.Length;
                    foreach (T v in values.Take(r))
                    {
                        c++;
                        _buffer[end] = v;
                        end++;
                        if (end == _buffer.Length)
                            end = 0;
                    }
                    _count += c;
                }
                finally { Monitor.Exit(_syncRoot); }
                return c;
            }

            public bool StartsWith(params T[] buffer) { return StartsWith(0, buffer); }

            public bool StartsWith(int startIndex, params T[] buffer)
            {
                if (buffer == null || buffer.Length == 0)
                    return false;
                
                Monitor.Enter(_syncRoot);
                try
                {
                    IEnumerable<T> en = _Peek(buffer.Length);
                    if (startIndex > 0)
                        en = en.Skip(startIndex);
                    using (IEnumerator<T> e = en.GetEnumerator())
                    {
                        foreach (T src in buffer)
                        {
                            if (!(e.MoveNext() && src.Equals(e.Current)))
                                return false;
                        }
                    }
                }
                finally { Monitor.Exit(_syncRoot); }

                return true;
            }
        }
        
        #region Fields

        public const int MinBufferSize = 128;
        public const int DefaultBufferSize = 4096;

        private object _syncRoot = new object();
        private static ReadOnlyCollection<Tuple<Encoding, byte[]>> _detectableEncodings = null;
        private static Encoding _defaultEncoding = null;
        private readonly Stream _innerStream;
        private readonly Encoding _encoding;
        private readonly bool _leaveOpen;
        private readonly ValueBuffer<byte> _rawBuffer;
        private readonly ValueBuffer<char> _charBuffer;
        private long _charPosition = 0L;
        private long _lineIndex = 0L;
        private LinkedList<Tuple<long, long>> _linePositions = new LinkedList<Tuple<long, long>>();
        private readonly int _maxSingleCharByteCount;
        private readonly bool _omitCrFromNewLine;
        private readonly char[] _newLineCharSequence;
        private readonly byte[] _newLineByteSequence;
        private bool _crFlag = false;

        #endregion

        #region Properties

        protected internal static ReadOnlyCollection<Tuple<Encoding, byte[]>> DetectableEncodings
        {
            get
            {
                if (_detectableEncodings == null)
                {
                    Tuple<Encoding, byte[]>[] de = Encoding.GetEncodings().Select(i =>
                    {
                        Encoding e = i.GetEncoding();
                        return new Tuple<Encoding, byte[]>(e, e.GetPreamble());
                    }).Where(t => t.Item2.Length > 0 && t.Item2.Length <= MinBufferSize).OrderByDescending(t => t.Item2.Length).ToArray();
                    if (de.Length == 0)
                    {
                        de = new Tuple<Encoding, byte[]>[]
                        {
                            new Tuple<Encoding, byte[]>(Encoding.UTF32, new byte[] { 255, 254, 0, 0 }),
                            new Tuple<Encoding, byte[]>(Encoding.UTF8, new byte[] { 239, 187, 191 }),
                            new Tuple<Encoding, byte[]>(Encoding.BigEndianUnicode, new byte[] { 254, 255 }),
                            new Tuple<Encoding, byte[]>(Encoding.Unicode, new byte[] { 255, 254 })
                        };
                    }
                    _detectableEncodings = new ReadOnlyCollection<Tuple<Encoding, byte[]>>(de);
                }
                return _detectableEncodings;
            }
        }

        public static Encoding DefaultEncoding
        {
            get
            {
                Encoding defaultEncoding = _defaultEncoding;
                if (defaultEncoding == null)
                {
                    defaultEncoding = new UTF8Encoding(false, true);
                    _defaultEncoding = defaultEncoding;
                }
                return defaultEncoding;
            }
            set { _defaultEncoding = value; }
        }
        
        public Encoding CurrentEncoding { get { return _encoding.Clone() as Encoding; } }

        public long BytePosition
        {
            get
            {
                Monitor.Enter(_syncRoot);
                try
                {
                    if (_lineIndex < -1)
                        throw new ObjectDisposedException(GetType().AssemblyQualifiedName);

                    if (_lineIndex < 0)
                        throw new InvalidOperationException("Stream reader has already been closed.");

                    return _innerStream.Position - (long)(_rawBuffer.Count) - (long)(_encoding.GetByteCount(_charBuffer.Peek(_charBuffer.Count)));
                }
                finally { Monitor.Exit(_syncRoot); }
            }
        }

        public long CharPostion { get { return _charPosition; } }

        public long LineIndex { get { return _lineIndex; } }

        #endregion

        #region Constructors

        public RewindableStreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize, bool leaveOpen,
            bool omitCrFromNewLine)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            
            if (!stream.CanRead)
                throw new ArgumentException("The stream does not support reading.", "stream");
            
            if (!stream.CanSeek)
                throw new ArgumentException("The stream does not support seeking.", "stream");
            
            if (bufferSize < 1)
                throw new ArgumentOutOfRangeException("bufferSize", "Buffer size must be greater than zero.");
            
            _leaveOpen = leaveOpen;
            _rawBuffer = new ValueBuffer<byte>(bufferSize);
            _charBuffer = new ValueBuffer<char>(bufferSize);

            if (detectEncodingFromByteOrderMarks)
            {
                int readLen;
                try
                {
                    ReadOnlyCollection<Tuple<Encoding, byte[]>> detectableEncodings = DetectableEncodings;
                    readLen = DetectableEncodings[0].Item2.Length;
                    EnsureRawBuffer(readLen);
                }
                catch (ObjectDisposedException exception)
                {
                    throw new InvalidOperationException("Stream has already been disposed", exception);
                }
                if (_rawBuffer.Count >= DetectableEncodings[DetectableEncodings.Count - 1].Item2.Length)
                {
                    byte[] bom = _rawBuffer.Peek(readLen);
                    Encoding e = (bom.Length > 0) ? DetectEncodingFromBytes(bom, 0, bom.Length) : null;
                    if (e != null)
                    {
                        bom = e.GetPreamble();
                        if (bom.Length > 0 && _rawBuffer.StartsWith(bom))
                        _rawBuffer.Purge(bom.Length);
                        encoding = e;
                    }
                }
            }

            if (encoding == null)
                encoding = DefaultEncoding;
            _encoding = encoding.Clone() as Encoding;
            _encoding.DecoderFallback = new DecoderExceptionFallback();
            _encoding.EncoderFallback = new EncoderExceptionFallback();
            _maxSingleCharByteCount = (_encoding.IsSingleByte) ? 1 : _encoding.GetMaxByteCount(1);
            _omitCrFromNewLine = omitCrFromNewLine;
            _newLineCharSequence = (omitCrFromNewLine) ? new char[] { '\n' } : new char[] { '\r', '\n' };
            _newLineByteSequence = _encoding.GetBytes(_newLineCharSequence);
            _innerStream = stream;
        }

        public RewindableStreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize, bool omitCrFromNewLine)
            : this(CreateStreamFromPath(path, bufferSize), encoding, detectEncodingFromByteOrderMarks, bufferSize, false, omitCrFromNewLine) { }

        public RewindableStreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, bool leaveOpen, bool omitCrFromNewLine)
            : this(stream, encoding, detectEncodingFromByteOrderMarks, DefaultBufferSize, leaveOpen, omitCrFromNewLine) { }

        public RewindableStreamReader(Stream stream, Encoding encoding, int bufferSize, bool leaveOpen, bool omitCrFromNewLine) : this(stream, encoding, true, bufferSize, leaveOpen, omitCrFromNewLine) { }

        public RewindableStreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize, bool omitCrFromNewLine)
            : this(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, false, omitCrFromNewLine) { }

        public RewindableStreamReader(Stream stream, bool detectEncodingFromByteOrderMarks, int bufferSize, bool leaveOpen, bool omitCrFromNewLine)
            : this(stream, null, detectEncodingFromByteOrderMarks, bufferSize, leaveOpen, omitCrFromNewLine) { }

        public RewindableStreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize) : this(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, false) { }

        public RewindableStreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize) : this(path, encoding, detectEncodingFromByteOrderMarks, bufferSize, false) { }

        public RewindableStreamReader(string path, bool detectEncodingFromByteOrderMarks, int bufferSize, bool omitCrFromNewLine)
            : this(path, null, detectEncodingFromByteOrderMarks, bufferSize, omitCrFromNewLine) { }

        public RewindableStreamReader(Stream stream, bool detectEncodingFromByteOrderMarks, int bufferSize, bool omitCrFromNewLine)
            : this(stream, detectEncodingFromByteOrderMarks, bufferSize, false, omitCrFromNewLine) { }

        public RewindableStreamReader(Stream stream, Encoding encoding, bool leaveOpen, bool omitCrFromNewLine) : this(stream, encoding, false, leaveOpen, omitCrFromNewLine) { }

        public RewindableStreamReader(Stream stream, Encoding encoding, int bufferSize, bool omitCrFromNewLine) : this(stream, encoding, bufferSize, false, omitCrFromNewLine) { }

        public RewindableStreamReader(Stream stream, bool detectEncodingFromByteOrderMarks, bool leaveOpen, bool omitCrFromNewLine)
            : this(stream, null, detectEncodingFromByteOrderMarks, leaveOpen, omitCrFromNewLine) { }

        public RewindableStreamReader(string path, Encoding encoding, int bufferSize, bool omitCrFromNewLine) : this(path, encoding, true, bufferSize, omitCrFromNewLine) { }

        public RewindableStreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks, bool omitCrFromNewLine)
            : this(path, encoding, detectEncodingFromByteOrderMarks, DefaultBufferSize, omitCrFromNewLine) { }

        public RewindableStreamReader(Stream stream, int bufferSize, bool leaveOpen, bool omitCrFromNewLine) : this(stream, true, bufferSize, leaveOpen, omitCrFromNewLine) { }

        public RewindableStreamReader(string path, Encoding encoding, int bufferSize) : this(path, encoding, bufferSize, false) { }

        public RewindableStreamReader(Stream stream, bool leaveOpen, bool omitCrFromNewLine) : this(stream, false, leaveOpen, omitCrFromNewLine) { }

        public RewindableStreamReader(string path, int bufferSize, bool omitCrFromNewLine) : this(path, true, bufferSize, omitCrFromNewLine) { }

        public RewindableStreamReader(Stream stream, int bufferSize, bool omitCrFromNewLine) : this(stream, bufferSize, false, omitCrFromNewLine) { }

        public RewindableStreamReader(Stream stream, Encoding encoding, bool omitCrFromNewLine) : this(stream, encoding, false, omitCrFromNewLine) { }

        public RewindableStreamReader(string path, bool detectEncodingFromByteOrderMarks, bool omitCrFromNewLine) : this(path, null, detectEncodingFromByteOrderMarks, omitCrFromNewLine) { }

        public RewindableStreamReader(string path, bool detectEncodingFromByteOrderMarks, int bufferSize) : this(path, detectEncodingFromByteOrderMarks, bufferSize, false) { }

        public RewindableStreamReader(Stream stream, Encoding encoding, int bufferSize) : this(stream, encoding, bufferSize, false) { }

        public RewindableStreamReader(Stream stream, bool detectEncodingFromByteOrderMarks, int bufferSize) : this(stream, detectEncodingFromByteOrderMarks, bufferSize, false) { }

        public RewindableStreamReader(string path, Encoding encoding, bool omitCrFromNewLine) : this(path, encoding, true, omitCrFromNewLine) { }

        public RewindableStreamReader(Stream stream, int bufferSize) : this(stream, bufferSize, false) { }

        public RewindableStreamReader(Stream stream, Encoding encoding) : this(stream, encoding, false) { }

        public RewindableStreamReader(string path, Encoding encoding) : this(path, encoding, false) { }

        public RewindableStreamReader(string path, int bufferSize) : this(path, bufferSize, false) { }

        public RewindableStreamReader(string path, bool omitCrFromNewLine) : this(path, null, omitCrFromNewLine) { }

        public RewindableStreamReader(Stream stream, bool omitCrFromNewLine) : this(stream, false, omitCrFromNewLine) { }

        public RewindableStreamReader(string path) : this(path, false) { }

        public RewindableStreamReader(Stream stream) : this(stream, false) { }

        #endregion
        
        #region FromBuffer

        public static RewindableStreamReader FromBuffer(byte[] buffer, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize, bool omitCrFromNewLine)
        {
            MemoryStream memoryStream = new MemoryStream((buffer == null) ? new byte[0] : buffer);
            try { return new RewindableStreamReader(memoryStream, encoding, detectEncodingFromByteOrderMarks, bufferSize, omitCrFromNewLine); }
            catch
            {
                memoryStream.Dispose();
                throw;
            }
        }

        public static RewindableStreamReader FromBuffer(byte[] buffer, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize)
        {
            return FromBuffer(buffer, encoding, detectEncodingFromByteOrderMarks, bufferSize, false);
        }

        public static RewindableStreamReader FromBuffer(byte[] buffer, Encoding encoding, bool detectEncodingFromByteOrderMarks, bool omitCrFromNewLine)
        {
            return FromBuffer(buffer, encoding, detectEncodingFromByteOrderMarks, DefaultBufferSize, omitCrFromNewLine);
        }

        public static RewindableStreamReader FromBuffer(byte[] buffer, Encoding encoding, int bufferSize, bool omitCrFromNewLine)
        {
            return FromBuffer(buffer, encoding, true, bufferSize, omitCrFromNewLine);
        }

        public static RewindableStreamReader FromBuffer(byte[] buffer, bool detectEncodingFromByteOrderMarks, int bufferSize, bool omitCrFromNewLine)
        {
            return FromBuffer(buffer, null, detectEncodingFromByteOrderMarks, bufferSize, omitCrFromNewLine);
        }

        public static RewindableStreamReader FromBuffer(byte[] buffer, Encoding encoding, bool detectEncodingFromByteOrderMarks)
        {
            return FromBuffer(buffer, encoding, detectEncodingFromByteOrderMarks, false);
        }

        public static RewindableStreamReader FromBuffer(byte[] buffer, Encoding encoding, int bufferSize)
        {
            return FromBuffer(buffer, encoding, true, bufferSize);
        }

        public static RewindableStreamReader FromBuffer(byte[] buffer, bool detectEncodingFromByteOrderMarks, int bufferSize)
        {
            return FromBuffer(buffer, null, detectEncodingFromByteOrderMarks, bufferSize);
        }

        public static RewindableStreamReader FromBuffer(byte[] buffer, bool detectEncodingFromByteOrderMarks, bool omitCrFromNewLine)
        {
            return FromBuffer(buffer, null, detectEncodingFromByteOrderMarks, omitCrFromNewLine);
        }

        public static RewindableStreamReader FromBuffer(byte[] buffer, int bufferSize, bool omitCrFromNewLine)
        {
            return FromBuffer(buffer, null, bufferSize, omitCrFromNewLine);
        }


        public static RewindableStreamReader FromBuffer(byte[] buffer, bool detectEncodingFromByteOrderMarks)
        {
            return FromBuffer(buffer, null, detectEncodingFromByteOrderMarks);
        }

        public static RewindableStreamReader FromBuffer(byte[] buffer, Encoding encoding)
        {
            return FromBuffer(buffer, encoding, true);
        }

        public static RewindableStreamReader FromBuffer(byte[] buffer, int bufferSize)
        {
            return FromBuffer(buffer, null, bufferSize);
        }

        public static RewindableStreamReader FromBuffer(byte[] buffer)
        {
            return FromBuffer(buffer, null);
        }

        #endregion

        #region FromString
        
        public static RewindableStreamReader FromString(string text, Encoding encoding, int bufferSize, bool omitCrFromNewLine)
        {
            if (encoding == null)
                encoding = DefaultEncoding;
            MemoryStream memoryStream = new MemoryStream(encoding.GetBytes(text ?? ""));
            try { return new RewindableStreamReader(memoryStream, encoding, bufferSize, omitCrFromNewLine); }
            catch
            {
                memoryStream.Dispose();
                throw;
            }
        }

        public static RewindableStreamReader FromString(string text, Encoding encoding, int bufferSize)
        {
            return FromString(text, encoding, bufferSize, false);
        }

        public static RewindableStreamReader FromString(string text, Encoding encoding, bool omitCrFromNewLine)
        {
            return FromString(text, encoding, DefaultBufferSize, omitCrFromNewLine);
        }

        public static RewindableStreamReader FromString(string text, int bufferSize, bool omitCrFromNewLine)
        {
            return FromString(text, null, bufferSize, omitCrFromNewLine);
        }

        public static RewindableStreamReader FromString(string text, Encoding encoding)
        {
            return FromString(text, encoding, false);
        }

        public static RewindableStreamReader FromString(string text, int bufferSize)
        {
            return FromString(text, null, bufferSize);
        }

        public static RewindableStreamReader FromString(string text, bool omitCrFromNewLine)
        {
            return FromString(text, null, omitCrFromNewLine);
        }

        public static RewindableStreamReader FromString(string text)
        {
            return FromString(text, false);
        }

        #endregion

        private static FileStream CreateStreamFromPath(string path, int bufferSize)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            
            if (bufferSize < 1)
                throw new ArgumentOutOfRangeException("bufferSize", "Buffer size must be greater than zero.");
            
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, (bufferSize < MinBufferSize) ? MinBufferSize : bufferSize);
        }


        public static Encoding DetectEncodingFromBytes(byte[] buffer, int startIndex, int length)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException("startIndex", "Start index must be greater than or equal to zero.");
            
            if (length < 0)
                throw new ArgumentOutOfRangeException("length", "Length must be greater than or equal to zero.");
            
            if (startIndex + length > buffer.Length)
                throw new ArgumentOutOfRangeException("length", "Start index plus length must be less than or equal to the buffer length.");
            
            if (buffer == null || length == 0)
                return null;
            
            return DetectableEncodings.Where(t => t.Item2.Length <= length && buffer.Take(t.Item2.Length).SequenceEqual(t.Item2))
                .Select(t => t.Item1).FirstOrDefault();
        }

        private int EnsureRawBuffer(int requestedLength)
        {
            Monitor.Enter(_syncRoot);
            try
            {
                if (_lineIndex < -1)
                    throw new ObjectDisposedException(GetType().AssemblyQualifiedName);

                if (_lineIndex < 0)
                    throw new InvalidOperationException("Stream reader has already been closed.");

                long availableBytes;
                int margin;
                if (requestedLength <= _rawBuffer.Count || _rawBuffer.Free == 0 || (availableBytes = _innerStream.Length - _innerStream.Position) == 0L)
                    return _rawBuffer.Count;
                
                requestedLength = _rawBuffer.Free;
                if (availableBytes < (long)requestedLength)
                    requestedLength = (int)availableBytes;
                
                byte[] buffer = new byte[_rawBuffer.Free];
                requestedLength = _innerStream.Read(buffer, 0, requestedLength);
                if (requestedLength < 1)
                    return 0;
                margin = _rawBuffer.Write(requestedLength, buffer);
                margin = (margin > 0) ? requestedLength - margin : requestedLength;
                if (margin > 0)
                {
                    availableBytes = _innerStream.Position - (long)margin;
                    _innerStream.Seek((availableBytes > 0L) ? availableBytes : 0L, SeekOrigin.Begin);
                }

                return _rawBuffer.Count;
            }
            finally { Monitor.Exit(_syncRoot); }
        }

        private int EnsureCharBuffer(int requestedLength)
        {
            Monitor.Enter(_syncRoot);
            try
            {
                if (_lineIndex < -1)
                    throw new ObjectDisposedException(GetType().AssemblyQualifiedName);

                if (_lineIndex < 0)
                    throw new InvalidOperationException("Stream reader has already been closed.");

                int availableSpace;
                int bytesInRequestedChars = _encoding.GetMaxByteCount(requestedLength);
                if (requestedLength <= _charBuffer.Count || _charBuffer.Free == 0 || (availableSpace = EnsureRawBuffer(bytesInRequestedChars)) == 0)
                    return _charBuffer.Count;
                
                requestedLength = _charBuffer.Free;
                char[] convertedChars;
                try { convertedChars = _encoding.GetChars(_rawBuffer.Peek(bytesInRequestedChars)); }
                catch { convertedChars = _encoding.GetChars(_rawBuffer.Peek(bytesInRequestedChars - 1)); }
                int charAddCount = _charBuffer.Write(convertedChars);
                if (charAddCount < requestedLength)
                    convertedChars = convertedChars.Take(charAddCount).ToArray();
                _rawBuffer.Purge(_encoding.GetByteCount(convertedChars));
                return _charBuffer.Count;
            }
            finally { Monitor.Exit(_syncRoot); }
        }

        public int RewindLines(int lineCount)
        {
            throw new NotImplementedException();
        }

        public int RewindChars(int charCount)
        {
            throw new NotImplementedException();
        }

        #region Overrides
        
        public override void Close()
        {
            Monitor.Enter(_syncRoot);
            try
            {
                base.Close();

                if (_lineIndex < -1)
                    throw new ObjectDisposedException(GetType().AssemblyQualifiedName);

                if (_lineIndex < 0)
                    return;

                _lineIndex = -1;
                if (!_leaveOpen)
                    _innerStream.Close();
            }
            finally { Monitor.Exit(_syncRoot); }
        }

        public override int Peek()
        {
            Monitor.Enter(_syncRoot);
            try
            {
                if (_lineIndex < -1)
                    throw new ObjectDisposedException(GetType().AssemblyQualifiedName);

                if (_lineIndex < 0)
                    throw new InvalidOperationException("Stream reader has already been closed.");

                EnsureCharBuffer(1);
                return _charBuffer.Peek(1).Cast<int>().DefaultIfEmpty(-1).First();
            }
            finally { Monitor.Exit(_syncRoot); }
        }

        public override int Read()
        {
            Monitor.Enter(_syncRoot);
            try
            {
                if (_lineIndex < -1)
                    throw new ObjectDisposedException(GetType().AssemblyQualifiedName);

                if (_lineIndex < 0)
                    throw new InvalidOperationException("Stream reader has already been closed.");
                
                EnsureCharBuffer(1);
                char[] c = _charBuffer.Read(1);
                if (c.Length == 0)
                    return -1;
                _charPosition++;
                if (c[0] == '\r')
                {
                    if (_crFlag)
                    {
                        _linePositions.AddLast(new Tuple<long, long>(BytePosition, _charPosition));
                        _lineIndex++;
                    }
                    else
                        _crFlag = true;
                }
                else if (c[0] == '\n')
                {
                    _linePositions.AddLast(new Tuple<long, long>(BytePosition, _charPosition));
                    _lineIndex++;
                    _crFlag = false;
                }
                else if (_crFlag)
                {
                    _linePositions.AddLast(new Tuple<long, long>(BytePosition, _charPosition));
                    _lineIndex++;
                    _crFlag = false;
                }
                return (int)(c[0]);
            }
            finally { Monitor.Exit(_syncRoot); }
        }

        public override int Read(char[] buffer, int index, int count) 
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", "Index must be greater than or equal to zero");
            
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", "Count must be greater than or equal to zero");
            
            if (buffer.Length - index < count)
                throw new ArgumentException("Index plus count must be less than or equal to the total length of the buffer");
                
            int n = 0;
            Monitor.Enter(_syncRoot);
            try
            {
                if (_lineIndex < -1)
                    throw new ObjectDisposedException(GetType().AssemblyQualifiedName);

                if (_lineIndex < 0)
                    throw new InvalidOperationException("Stream reader has already been closed.");
                    
                do
                {
                    int ch = Read();
                    if (ch == -1)
                        break;
                    buffer[index + n++] = (char)ch;
                } while (n < count);
            }
            finally { Monitor.Exit(_syncRoot); }

            return n;
        }

        public override string ReadToEnd()
        {
            char[] chars = new char[4096];
            int len;
            StringBuilder sb = new StringBuilder(4096);
            Monitor.Enter(_syncRoot);
            try
            {
                if (_lineIndex < -1)
                    throw new ObjectDisposedException(GetType().AssemblyQualifiedName);

                if (_lineIndex < 0)
                    throw new InvalidOperationException("Stream reader has already been closed.");
                    
                while((len=Read(chars, 0, chars.Length)) != 0) 
                {
                    sb.Append(chars, 0, len);
                }
            }
            finally { Monitor.Exit(_syncRoot); }
            return sb.ToString();
        }

        public override int ReadBlock(char[] buffer, int index, int count) 
        {
            int i, n = 0;
            Monitor.Enter(_syncRoot);
            try
            {
                if (_lineIndex < -1)
                    throw new ObjectDisposedException(GetType().AssemblyQualifiedName);

                if (_lineIndex < 0)
                    throw new InvalidOperationException("Stream reader has already been closed.");
                    
                do {
                    n += (i = Read(buffer, index + n, count - n));
                } while (i > 0 && n < count);
            }
            finally { Monitor.Exit(_syncRoot); }
            return n;
        }
 
        public override String ReadLine() 
        {
            StringBuilder sb = new StringBuilder();
            Monitor.Enter(_syncRoot);
            try
            {
                if (_lineIndex < -1)
                    throw new ObjectDisposedException(GetType().AssemblyQualifiedName);

                if (_lineIndex < 0)
                    throw new InvalidOperationException("Stream reader has already been closed.");
                    
                while (true) {
                    int ch = Read();
                    if (ch == -1) break;
                    if (ch == '\r' || ch == '\n') 
                    {
                        if (ch == '\r' && Peek() == '\n') Read();
                        return sb.ToString();
                    }
                    sb.Append((char)ch);
                }
            }
            finally { Monitor.Exit(_syncRoot); }
            if (sb.Length > 0) return sb.ToString();
            return null;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            Monitor.Enter(_syncRoot);
            try
            {
                base.Dispose(disposing);

                if (_lineIndex < -1)
                    return;

                _lineIndex = -2;
                if (!_leaveOpen)
                    _innerStream.Dispose();
            }
            finally { Monitor.Exit(_syncRoot); }
        }

        #endregion
    }
}