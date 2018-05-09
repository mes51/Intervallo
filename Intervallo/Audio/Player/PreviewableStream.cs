using Intervallo.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.Audio.Player
{
    public abstract class PreviewableStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Position { get; set; }

        public virtual int SamplePosition
        {
            get
            {
                return (int)(Position / sizeof(double));
            }
            set
            {
                Position = value * sizeof(double);
            }
        }

        public virtual int SampleCount => (int)(Length / sizeof(double));

        public abstract IReadOnlyList<IntRange> PreviewableSampleRanges { get; }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var position = Position;
            var samplePosition = SamplePosition;
            var samples = new double[(int)Math.Ceiling(count / (double)sizeof(double))];

            var readCount = ReadSamples(samples, samples.Length);
            var readByteCount = Math.Min(Math.Min(count, readCount * sizeof(double)), buffer.Length - offset);
            Position = position + readByteCount;
            Buffer.BlockCopy(samples, (int)(position - samplePosition * sizeof(double)), buffer, offset, readByteCount);

            return readByteCount;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length - offset;
                    break;
            }

            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public abstract int ReadSamples(double[] buffer, int count);

        public double ReadSample()
        {
            var buffer = new double[1];
            if (ReadSamples(buffer, 1) != 0)
            {
                return buffer[0];
            }
            else
            {
                return double.NaN;
            }
        }
    }
}
