using UnityEngine;

namespace NullponSpectrum.Utilities
{
    /// <summary>
    /// 床ビジュアライザは常に負荷軽減方針（ステージ色の間引き・Emit 削減など）。
    /// </summary>
    internal static class XrPerfHelper
    {
        public static bool ShouldReduceVisualizerCost()
        {
            return true;
        }

        /// <summary>ステージメッシュの頂点色を GPU に送るのは 3 フレームに 1 回。</summary>
        public static bool ShouldUploadStageMeshColorsThisFrame()
        {
            return (Time.frameCount % 3) == 0;
        }
    }
}
