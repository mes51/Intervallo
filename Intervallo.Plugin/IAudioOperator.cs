using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.Plugin
{
    /// <summary>
    /// 音声の解析・合成を行うプラグインのインターフェース。
    /// プラグインはMEFを利用して読み込まれ、起動時に生成されたインスタンスが再利用されます。
    /// また、このインターフェースを実装したクラスのメソッドはスレッドセーフである必要があります。
    /// </summary>
    public interface IAudioOperator : IPluginInfo
    {
        /// <summary>
        /// 音声の解析を行います
        /// </summary>
        /// <param name="wave">音声データ</param>
        /// <param name="framePeriod">音声の切り出し単位(ms)</param>
        /// <param name="notifyProgress">進捗の通知を行うメソッド(%)</param>
        /// <returns>解析された音声データ</returns>
        AnalyzedAudio Analyze(WaveData wave, double framePeriod, Action<double> notifyProgress);

        /// <summary>
        /// 音声の合成を行います
        /// </summary>
        /// <param name="analyzedAudio">操作済みの解析された音声データ</param>
        /// <param name="notifyProgress">進捗の通知を行うメソッド(%)</param>
        /// <returns>合成された音声データ</returns>
        WaveData Synthesize(AnalyzedAudio analyzedAudio, Action<double> notifyProgress);
    }
}
