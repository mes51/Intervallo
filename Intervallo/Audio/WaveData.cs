using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.Audio
{
    [Serializable]
    public class WaveData
    {
        public WaveData(string fileName, double[] wave, int sampleRate)
        {
            Wave = wave;
            SampleRate = sampleRate;

            using (ReadOnlyDoubleMemoryStream ms = new ReadOnlyDoubleMemoryStream(wave))
            using (var algorithm = SHA256.Create())
            {
                var hash = algorithm.ComputeHash(ms);
                Hash = string.Join("", hash.Select((x) => x.ToString("X2")));
            }
        }

        public string FileName { get; }

        public string Hash { get; }

        public double[] Wave { get; }

        public int SampleRate { get; }

        public override bool Equals(object obj)
        {
            return Hash == (obj as WaveData)?.Hash;
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        public override string ToString()
        {
            return $"{{ Length: {Wave.Length}, Hash: {Hash} }}";
        }
    }

    class ReadOnlyDoubleMemoryStream : Stream
    {
        const int DoubleSize = sizeof(double);

        public ReadOnlyDoubleMemoryStream(double[] data)
        {
            Data = data;
        }

        public double[] Data { get; }

        public override bool CanRead { get; } = true;

        public override bool CanSeek { get; } = true;

        public override bool CanWrite { get; } = false;

        public override long Length => Data.Length * DoubleSize;

        public override long Position { get; set; }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (offset + Position > Length)
            {
                throw new ArgumentException(nameof(offset));
            }
            else if (offset < 0)
            {
                throw new IndexOutOfRangeException(nameof(offset));
            }
            else if (count < 0)
            {
                throw new IndexOutOfRangeException(nameof(count));
            }

            count = Math.Min(count, (int)(Length - offset - Position));
            Buffer.BlockCopy(Data, (int)(Position + offset), buffer, 0, count);
            Seek(count, SeekOrigin.Current);
            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position = Math.Max(Math.Min(Position + offset, Length), 0);
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
    }
}
