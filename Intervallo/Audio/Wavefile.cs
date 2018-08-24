using Intervallo.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static Intervallo.Markup.EnumerationLangKeyExtension;

namespace Intervallo.Audio
{
    public enum WaveBit : int
    {
        [LangKey(nameof(LangResources.WaveBits_Bit8))]
        Bit8 = 8,
        [LangKey(nameof(LangResources.WaveBits_Bit16))]
        Bit16 = 16,
        [LangKey(nameof(LangResources.WaveBits_Bit24))]
        Bit24 = 24,
        [LangKey(nameof(LangResources.WaveBits_Bit32))]
        Bit32 = 32,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    struct WaveHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public char[] RawRIFF;
        public int TotalSize;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public char[] RawWAVE;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public char[] Rawfmt;
        public int FmtSize;
        public WAVEFORMATEX Format;

        public string RIFF => new string(RawRIFF);

        public string WAVE => new string(RawWAVE);

        public string fmt => new string(Rawfmt);

        public void Initialize(int fs, WaveBit bit, int sampleLength)
        {
            var format = new WAVEFORMATEX();
            format.wFormatTag = (ushort)(bit != WaveBit.Bit32 ? 0x01 : 0x03);
            format.nChannels = 0x01;
            format.nSamplesPerSec = (uint)fs;
            format.nBlockAlign = (ushort)((int)bit / 8);
            format.wBitsPerSample = (ushort)bit;
            format.nAvgBytesPerSec = (uint)(fs * format.nBlockAlign);

            RawRIFF = "RIFF".ToCharArray();
            RawWAVE = "WAVE".ToCharArray();
            Rawfmt = "fmt ".ToCharArray();
            TotalSize = sampleLength * format.nBlockAlign + Marshal.SizeOf<WaveHeader>() - 2; // 8 - sizeof(format.cbSize);
            FmtSize = 16;
            Format = format;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct WAVEFORMATEX
    {
        public ushort wFormatTag;
        public ushort nChannels;
        public uint nSamplesPerSec;
        public uint nAvgBytesPerSec;
        public ushort nBlockAlign;
        public ushort wBitsPerSample;
        public ushort cbSize;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    struct WaveChunkHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public char[] ChunkID;
        public int Size;

        public string ChunkIDStr => new string(ChunkID);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Int24 : IComparable, IComparable<Int24>, IEquatable<Int24>
    {
        public static readonly Int24 MaxValue = new Int24(0x7FFFFF);

        public static readonly Int24 MinValue = new Int24(-0x800000);

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] Data;

        public Int24(int value)
        {
            Data = new byte[]
            {
                (byte)(value & 0xff),
                (byte)((value >> 8) & 0xff),
                (byte)((value >> 16) & 0xff)
            };
        }

        public Int24(byte[] data)
        {
            Data = new byte[] { data[0], data[1], data[2] };
        }

        public int Int32Value
        {
            get
            {
                if ((Data[2] & 0x80) != 0)
                {
                    return 0xFF << 24 | Data[2] << 16 | Data[1] << 8 | Data[0];
                }
                else
                {
                    return Data[2] << 16 | Data[1] << 8 | Data[0];
                }
            }
        }

        public int CompareTo(object obj)
        {
            if (obj is Int24)
            {
                return CompareTo((Int24)obj);
            }
            else
            {
                throw new ArgumentException("not same type");
            }
        }

        public int CompareTo(Int24 other)
        {
            return Int32Value.CompareTo(other.Int32Value);
        }

        public bool Equals(Int24 other)
        {
            return Data[0] == other.Data[0] && Data[1] == other.Data[1] && Data[2] == other.Data[2];
        }

        public override bool Equals(object obj)
        {
            if (obj is Int24)
            {
                return Equals((Int24)obj);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Int32Value << 8 | Data[0];
        }

        public override string ToString()
        {
            return Int32Value.ToString();
        }

        public static implicit operator int(Int24 value)
        {
            return value.Int32Value;
        }

        public static implicit operator Int24(int value)
        {
            return new Int24(value);
        }

        public static Int24 Max(Int24 a, Int24 b)
        {
            if (a > b)
            {
                return a;
            }
            else
            {
                return b;
            }
        }

        public static Int24 Min(Int24 a, Int24 b)
        {
            if (a < b)
            {
                return a;
            }
            else
            {
                return b;
            }
        }
    }

    class Wavefile
    {
        public int Fs { get; }

        public WaveBit Bit { get; }

        public double[] Data { get; }

        public string Hash { get; }

        public string FilePath { get; }

        public Wavefile(int fs, WaveBit bit, double[] data, string hash = "", string filePath = "")
        {
            Fs = fs;
            Bit = bit;
            Data = data;
            Hash = hash;
            FilePath = filePath;
        }

        /// <summary>
        /// Read wave file
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>Read wave file data</returns>
        /// <exception cref="InvalidDataException">File is not wave file, or not supported format</exception>
        public static Wavefile Read(string filePath)
        {
            using (var fs = new FileStream(Path.GetFullPath(filePath), FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(fs, ASCIIEncoding.ASCII))
            {
                var header = reader.ReadStruct<WaveHeader>();
                CheckHeader(header);
                if (header.FmtSize == 18)
                {
                    fs.Seek(header.Format.cbSize, SeekOrigin.Current);
                }
                else
                {
                    fs.Seek(-2, SeekOrigin.Current);
                }

                for (var chunkHeader = reader.ReadStruct<WaveChunkHeader>(); chunkHeader.ChunkIDStr != "data"; chunkHeader = reader.ReadStruct<WaveChunkHeader>())
                {
                    fs.Seek(chunkHeader.Size, SeekOrigin.Current);
                }

                fs.Seek(-4, SeekOrigin.Current);
                var length = reader.ReadInt32();

                var quantizationSize = header.Format.wBitsPerSample / 8;
                var waveData = new double[length / quantizationSize / header.Format.nChannels];

                var scale = Math.Pow(2.0, header.Format.wBitsPerSample - 1) * header.Format.nChannels;
                switch(quantizationSize)
                {
                    case 1:
                        for (var i = 0; i < waveData.Length; i++)
                        {
                            var combinedSample = 0;
                            for (var c = 0; c < header.Format.nChannels; c++)
                            {
                                combinedSample += reader.ReadByte() - 128;
                            }
                            waveData[i] = combinedSample / scale;
                        }
                        break;
                    case 2:
                        for (var i = 0; i < waveData.Length; i++)
                        {
                            var combinedSample = 0;
                            for (var c = 0; c < header.Format.nChannels; c++)
                            {
                                combinedSample += reader.ReadInt16();
                            }
                            waveData[i] = combinedSample / scale;
                        }
                        break;
                    case 3:
                        for (var i = 0; i < waveData.Length; i++)
                        {
                            var combinedSample = 0;
                            for (var c = 0; c < header.Format.nChannels; c++)
                            {
                                combinedSample += new Int24(reader.ReadBytes(3));
                            }
                            waveData[i] = combinedSample / scale;
                        }
                        break;
                    case 4:
                        for (var i = 0; i < waveData.Length; i++)
                        {
                            var combinedSample = 0.0;
                            for (var c = 0; c < header.Format.nChannels; c++)
                            {
                                combinedSample += reader.ReadSingle();
                            }
                            waveData[i] = combinedSample / header.Format.nChannels;
                        }
                        break;
                }

                var hash = "";
                fs.Seek(0, SeekOrigin.Begin);
                using (var algorithm = SHA256.Create())
                {
                    hash = string.Join("", algorithm.ComputeHash(fs).Select((x) => x.ToString("X2")));
                }

                return new Wavefile((int)header.Format.nSamplesPerSec, (WaveBit)header.Format.wBitsPerSample, waveData, hash, filePath);
            }
        }

        public void Write(string fileName)
        {
            var header = new WaveHeader();
            header.Initialize(Fs, Bit, Data.Length);

            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(fs, Encoding.ASCII))
            {
                WriteHeader(writer, header);

                var quantization = (int)Math.Pow(2.0, (int)Bit - 1);
                switch (Bit)
                {
                    case WaveBit.Bit8:
                        foreach (var sample in Data)
                        {
                            writer.Write((byte)(sample * quantization + 128));
                        }
                        break;
                    case WaveBit.Bit16:
                        foreach (var sample in Data)
                        {
                            writer.Write((short)(sample * quantization));
                        }
                        break;
                    case WaveBit.Bit24:
                        foreach (var sample in Data)
                        {
                            writer.Write(((Int24)(int)(sample * quantization)).Data);
                        }
                        break;
                    case WaveBit.Bit32:
                        foreach (var sample in Data)
                        {
                            writer.Write((float)sample);
                        }
                        break;
                }
            }
        }

        void WriteHeader(BinaryWriter writer, WaveHeader header)
        {
            writer.Write(header.RawRIFF, 0, 4);
            writer.Write(header.TotalSize);
            writer.Write(header.RawWAVE, 0, 4);
            writer.Write(header.Rawfmt, 0, 4);
            writer.Write(header.FmtSize);

            writer.Write(header.Format.wFormatTag);
            writer.Write(header.Format.nChannels);
            writer.Write(header.Format.nSamplesPerSec);
            writer.Write(header.Format.nAvgBytesPerSec);
            writer.Write(header.Format.nBlockAlign);
            writer.Write(header.Format.wBitsPerSample);

            writer.Write("data".ToCharArray(), 0, 4);
            writer.Write(header.TotalSize - (Marshal.SizeOf<WaveHeader>() - 2));
        }

        static void CheckHeader(WaveHeader header)
        {
            if (header.RIFF != "RIFF" || header.WAVE != "WAVE" || header.fmt != "fmt ")
            {
                throw new InvalidDataException("invalid header");
            }
            if (header.Format.wFormatTag != 0x01 && header.Format.wFormatTag != 0x03)
            {
                throw new InvalidDataException("invalid fmt Tag");
            }
        }
    }

    static class BinaryReaderExtension
    {
        public static T ReadStruct<T>(this BinaryReader reader) where T : struct
        {
            var data = reader.ReadBytes(Marshal.SizeOf<T>());
            var ptr = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<T>(Marshal.UnsafeAddrOfPinnedArrayElement(data, 0));
            }
            finally
            {
                ptr.Free();
            }
        }
    }
}
