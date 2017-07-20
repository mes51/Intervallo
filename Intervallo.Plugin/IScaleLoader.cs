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
        /// 音階データを読み込みます。
        /// </summary>
        /// <param name="fileName">読み込む音階データが含まれるファイルのパス。</param>
        /// <param name="framePeriod">音階の切り出し単位(ms)</param>
        /// <returns>読み込まれた音階データ</returns>
        double[] Load(string filePath, double framePeriod);
    }
}
