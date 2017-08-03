using Intervallo.Plugin.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.Plugin
{
    /// <summary>
    /// 音階データを読み込むプラグインのインターフェース。
    /// プラグインはMEFを利用して読み込まれ、起動時に生成されたインスタンスが再利用されます。
    /// また、このインターフェースを実装したクラスのメソッドはスレッドセーフである必要があります。
    /// </summary>
    public interface IScaleLoader : IPluginInfo
    {
        /// <summary>
        /// 読み込みに対応している拡張子
        /// </summary>
        string[] SupportedFileExtensions { get; }

        /// <summary>
        /// 音階データを読み込みます。
        /// </summary>
        /// <param name="filePath">読み込む音階データが含まれるファイルのパス。</param>
        /// <param name="framePeriod">音階の切り出し単位(ms)</param>
        /// <param name="maxFrameLength">読み込む最大のフレーム数</param>
        /// <returns>読み込まれた音階データ</returns>
        /// <exception cref="ScaleLoadException">読み込みに対応していないファイル、または破損していて読み込みに失敗した</exception>
        double[] Load(string filePath, double framePeriod, int maxFrameLength);
    }

    public class ScaleLoadException : Exception
    {
        public ScaleLoadException() : base(LangResource.ScaleLoadException_DefaultMessage) { }

        public ScaleLoadException(string message) : base(message) { }

        public ScaleLoadException(string message, Exception inner) : base(message, inner) { }
    }
}
