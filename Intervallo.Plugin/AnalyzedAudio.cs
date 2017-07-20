using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.Plugin
{
    /// <summary>
    /// 解析された音声のデータを表す基底クラス
    /// このクラス、および派生クラスはImmutable、かつSerializableである必要があります
    /// </summary>
    [Serializable]
    public abstract class AnalyzedAudio : ICloneable
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="f0">音声の基本周波数の配列</param>
        /// <param name="framePeriod">切り出し単位(ms)</param>
        public AnalyzedAudio(double[] f0, double framePeriod)
        {
            F0 = f0;
            FramePeriod = framePeriod;
        }

        /// <summary>
        /// 基本周波数の配列
        /// </summary>
        public double[] F0 { get; }

        /// <summary>
        /// 切り出し範囲の時間(ms)
        /// </summary>
        public double FramePeriod { get; }

        /// <summary>
        /// このデータに含まれるフレームの数
        /// </summary>
        public virtual int FrameLength
        {
            get
            {
                return F0?.Length ?? 0;
            }
        }

        /// <summary>
        /// オブジェクトの完全コピーを生成します
        /// </summary>
        /// <returns>このオブジェクトの完全コピー</returns>
        public object Clone()
        {
            return Copy();
        }

        /// <summary>
        /// オブジェクトの完全コピーを生成します
        /// </summary>
        /// <returns>このオブジェクトの完全コピー</returns>
        public abstract AnalyzedAudio Copy();

        /// <summary>
        /// 基本周波数を置き換えた新しいオブジェクトを生成します
        /// </summary>
        /// <param name="newF0">新しい基本周波数</param>
        /// <returns>基本周波数が置き換えられた新しいオブジェクト</returns>
        /// <exception cref="ArgumentException">基本周波数の数が一致しない</exception>
        public abstract AnalyzedAudio ReplaceF0(double[] newF0);

        /// <summary>
        /// 指定した範囲のフレームを含む解析された音声データを返します
        /// </summary>
        /// <param name="begin">開始フレーム</param>
        /// <param name="count">取り出すフレームの個数</param>
        /// <returns>切り出された解析済み音声データ</returns>
        /// <exception cref="ArgumentException">取り出すサンプルの数が0</exception>
        /// <exception cref="ArgumentOutOfRangeException">開始フレームが0未満、または個数との合計がFrameLength以上</exception>
        public abstract AnalyzedAudio Slice(int begin, int count);
    }
}
