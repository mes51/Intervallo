using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.Audio
{
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

        public void Initialize(int fs, int bit, int sampleLength)
        {
            var format = new WAVEFORMATEX();
            format.wFormatTag = 0x01;
            format.nChannels = 1;
            format.nSamplesPerSec = (uint)fs;
            format.nBlockAlign = (ushort)(bit / 8);
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

    class Wavefile
    {
        public int Fs { get; }

        public int Bit { get; }

        public double[] Data { get; }

        public string Hash { get; }

        public string FilePath { get; }

        public Wavefile(int fs, int bit, double[] data, string hash = "", string filePath = "")
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
            using (var fs = new FileStream(Path.GetFullPath(filePath), FileMode.Open))
            using (var reader = new BinaryReader(fs, ASCIIEncoding.ASCII))
            {
                var headerSize = Marshal.SizeOf<WaveHeader>();
                var headerData = reader.ReadBytes(headerSize);
                var header = Marshal.PtrToStructure<WaveHeader>(Marshal.UnsafeAddrOfPinnedArrayElement(headerData, 0));
                CheckHeader(header);
                if (header.FmtSize == 18)
                {
                    fs.Seek(header.Format.cbSize + 2, SeekOrigin.Current); // seek extra header and 'd', 'a'
                }
                fs.Seek(2, SeekOrigin.Current); // seek 't', 'a'
                var length = reader.ReadInt32();

                var waveStartPos = fs.Position;

                var quantizationSize = header.Format.wBitsPerSample / 8;
                var waveData = new double[length / quantizationSize];

                var scale = Math.Pow(2.0, header.Format.wBitsPerSample - 1);
                switch(quantizationSize)
                {
                    case 1:
                        for (var i = 0; i < waveData.Length; i++)
                        {
                            waveData[i] = reader.ReadSByte() / scale;
                        }
                        break;
                    case 2:
                        for (var i = 0; i < waveData.Length; i++)
                        {
                            waveData[i] = reader.ReadInt16() / scale;
                        }
                        break;
                }

                var hash = "";
                fs.Seek(0, SeekOrigin.Begin);
                using (var algorithm = SHA256.Create())
                {
                    hash = string.Join("", algorithm.ComputeHash(fs).Select((x) => x.ToString("X2")));
                }

                return new Wavefile((int)header.Format.nSamplesPerSec, header.Format.wBitsPerSample, waveData, hash, filePath);
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

                var quantization = (int)Math.Pow(2.0, Bit - 1);
                switch (Bit)
                {
                    case 8:
                        foreach (var sample in Data)
                        {
                            writer.Write((sbyte)Math.Min(sbyte.MaxValue, Math.Max(sbyte.MinValue, (sbyte)(sample * quantization))));
                        }
                        break;
                    case 16:
                        foreach (var sample in Data)
                        {
                            writer.Write((short)Math.Min(short.MaxValue, Math.Max(short.MinValue, (short)(sample * quantization))));
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
            if (header.Format.wFormatTag != 0x01)
            {
                throw new InvalidDataException("invalid fmt Tag");
            }
            if (header.Format.nChannels != 1)
            {
                throw new InvalidDataException("support monaural only");
            }
        }
    }
}
