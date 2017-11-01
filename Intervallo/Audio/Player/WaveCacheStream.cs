using Intervallo.Plugin;
using Intervallo.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Intervallo.Audio.Player
{
    class WaveCacheStream : PreviewableStream
    {
        const int BufferingSampleCount = 32;

        public WaveCacheStream(WaveDataStream stream)
        {
            Stream = stream;
            Samples = new double[stream.SampleCount];

            BufferingTaskCancellationToken = new CancellationTokenSource();
            var token = BufferingTaskCancellationToken.Token;
            BufferingTask = new Task(() =>
            {
                var totalRange = new IntRange(0, Samples.Length - 1);
                while (!BufferedRange.Any(r => r.IsInclude(totalRange)))
                {
                    if (token.IsCancellationRequested)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var currentSamplePosition = 0;
                    var nextSamplePosition = 0;
                    var sampleCount = 0;
                    var buffer = new double[BufferingSampleCount];
                    lock (Stream)
                    {
                        currentSamplePosition = Stream.SamplePosition;
                        var alreadyProcessedRange = BufferedRange.Find(r => r.IsInclude(currentSamplePosition));
                        if (alreadyProcessedRange != null)
                        {
                            Stream.SamplePosition = alreadyProcessedRange.End;
                            continue;
                        }

                        sampleCount = Stream.ReadSamples(buffer, BufferingSampleCount);
                        nextSamplePosition = Stream.SamplePosition;
                    }
                    if (sampleCount == 0 || Samples.Length <= currentSamplePosition)
                    {
                        Stream.SamplePosition = 0;
                        continue;
                    }

                    var copyCount = Math.Min(sampleCount, Samples.Length - currentSamplePosition);
                    Buffer.BlockCopy(buffer, 0, Samples, currentSamplePosition * sizeof(double), copyCount * sizeof(double));

                    var processedRange = new IntRange(currentSamplePosition, nextSamplePosition);
                    var nearRange = BufferedRange.Where((r) => r.IsInclude(currentSamplePosition) || r.IsInclude(nextSamplePosition) || r.End == currentSamplePosition).ToArray();
                    lock (BufferedRange)
                    {
                        if (nearRange.Length > 0)
                        {
                            var newRange = nearRange.Aggregate(processedRange, (r, m) => m.Union(r));
                            BufferedRange.Add(newRange);
                            BufferedRange.RemoveAll(r => nearRange.Any(br => ReferenceEquals(br, r)));
                        }
                        else
                        {
                            BufferedRange.Add(processedRange);
                        }
                    }
                }
            }, token);
        }

        public override long Length => Stream.Length;

        public override int SamplePosition
        {
            get
            {
                return (int)(Position / sizeof(double));
            }
            set
            {
                lock (Stream)
                {
                    Position = value * sizeof(double);
                    Stream.SamplePosition = value;
                }
            }
        }

        public override int SampleCount => Stream.SampleCount;

        bool Disposed { get; set; }

        WaveDataStream Stream { get; }

        double[] Samples { get; }

        List<IntRange> BufferedRange { get; } = new List<IntRange>();

        CancellationTokenSource BufferingTaskCancellationToken { get; }

        Task BufferingTask { get; }

        public override int ReadSamples(double[] buffer, int count)
        {
            if (BufferingTask.Status == TaskStatus.Created)
            {
                BufferingTask.Start();
            }

            var targetRange = new IntRange(SamplePosition, Math.Min(SamplePosition + count, SampleCount - 1));
            while (!Disposed)
            {
                var exists = false;
                lock (BufferedRange)
                {
                    exists = BufferedRange.Exists((r) => r.IsInclude(targetRange));
                }
                if (exists)
                {
                    break;
                }
                else
                {
                    if (Stream.SamplePosition > SamplePosition || Stream.SamplePosition + BufferingSampleCount < SamplePosition)
                    {
                        lock (Stream)
                        {
                            Stream.SamplePosition = SamplePosition;
                        }
                    }
                    Thread.Sleep(10);
                }
            }

            var copyCount = Math.Min(count, SampleCount - SamplePosition);
            Buffer.BlockCopy(Samples, SamplePosition * sizeof(double), buffer, 0, copyCount * sizeof(double));
            Position += copyCount * sizeof(double);
            return copyCount;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (BufferingTask.Status != TaskStatus.Canceled && BufferingTask.Status != TaskStatus.Faulted && BufferingTask.Status != TaskStatus.RanToCompletion)
            {
                BufferingTaskCancellationToken.Cancel();
                try
                {
                    BufferingTask.Wait();
                }
                catch (AggregateException e)
                {
                    // Note: キャンセルするとOperationCanceledExceptionが飛ぶため、その時だけ無視
                    if (!(e.InnerException is OperationCanceledException))
                    {
                        throw e;
                    }
                }
            }
            BufferingTaskCancellationToken.Dispose();
            BufferingTask.Dispose();
            Stream.Dispose();
            Disposed = true;
        }
    }
}
