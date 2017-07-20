using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.Plugin
{
    /// <summary>
    /// プラグインの情報を提示するインターフェース
    /// </summary>
    public interface IPluginInfo
    {
        /// <summary>
        /// プラグイン名
        /// </summary>
        string PluginName { get; }

        /// <summary>
        /// プラグインの説明
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Copyright
        /// </summary>
        string Copyright { get; }

        /// <summary>
        /// プラグインのバージョン
        /// </summary>
        Version Version { get; }
    }
}
