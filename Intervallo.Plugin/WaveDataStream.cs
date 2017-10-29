using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.Plugin
{
    /// <summary>
    /// 音声データを出力するStreamの基底クラス。
    /// </summary>
    public abstract class WaveDataStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Position { get; set; }

        public virtual int SampleCount
        {
            get
            {
                return (int)(Length / sizeof(double));
            }
        }

        public int SamplePosition
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

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var position = Position;
            var samplePosition = SamplePosition;
            var sampleBuffer = new double[(int)Math.Ceiling(count / (double)sizeof(double))];

            var readCount = ReadSamples(sampleBuffer, sampleBuffer.Length);
            var readByteCount = Math.Min(Math.Min(count, readCount * sizeof(double)), buffer.Length - offset);
            Position = position + readByteCount;
            Buffer.BlockCopy(sampleBuffer, (int)(position - samplePosition * sizeof(double)), buffer, offset, readByteCount);

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

        /// <summary>
        /// 非対応
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 非対応
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 音声データを読み込みます
        /// </summary>
        /// <param name="buffer">読み込み先のバッファ</param>
        /// <param name="offset">バッファのオフセット</param>
        /// <param name="count">読み込む最大サンプル数</param>
        /// <returns>読み込んだサンプル数</returns>
        public abstract int ReadSamples(double[] buffer, int count);

        /// <summary>
        /// 音声データを読み込みます
        /// </summary>
        /// <returns>読み込んだサンプル。サンプルがなかった場合はNan</returns>
        public virtual double ReadSample()
        {
            var buffer = new double[1];
            var read = ReadSamples(buffer, buffer.Length);
            if (read > 0)
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
