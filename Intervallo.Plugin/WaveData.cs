using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.Plugin
{
    /// <summary>
    /// 音声データ
    /// </summary>
    public class WaveData
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="wave">音声データ</param>
        /// <param name="sampleRate">サンプリングレート</param>
        public WaveData(double[] wave, int sampleRate)
        {
            Wave = wave;
            SampleRate = sampleRate;
        }

        /// <summary>
        /// 音声データ
        /// </summary>
        public double[] Wave { get; }

        /// <summary>
        /// サンプリングレート
        /// </summary>
        public int SampleRate { get; }
    }
}
