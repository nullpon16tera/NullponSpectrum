using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using IPALogger = IPA.Logging.Logger;
using UnityEngine;

namespace NullponSpectrum.Utilities
{
    /// <summary>
    /// DLL に埋め込んだ AssetBundle、または Plugins フォルダの同名ファイルからシェーダを読み込み、
    /// <see cref="VisualizerUtil.GetShader"/> が名前解決できるようにする。
    /// </summary>
    internal static class ShaderBundleLoader
    {
        /// <summary>
        /// Beat Saber/Plugins/ に置くバンドルファイル名（拡張子なしが一般的）。Unity 側の AssetBundle 名と一致させる。</summary>
        public const string BundleFileName = "nullponspectrum_shaders";

        private static AssetBundle _bundle;
        /// <summary>バンドル内のシェーダをその場で解決する（FindObjectsOfTypeAll の順序・重複に依存しない）。</summary>
        private static Dictionary<string, Shader> _bundleShadersByName;

        /// <summary>バンドルが存在すれば読み込み。無くても例外にしない（カスタムシェーダは任意）。</summary>
        public static void TryLoad(IPALogger log)
        {
            try
            {
                string pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(pluginDir))
                {
                    log?.Warn("ShaderBundle: プラグインディレクトリを取得できませんでした。");
                    return;
                }

                string path = Path.Combine(pluginDir, BundleFileName);
                if (File.Exists(path))
                {
                    _bundle = AssetBundle.LoadFromFile(path);
                    if (_bundle == null)
                    {
                        log?.Warn($"ShaderBundle: LoadFromFile に失敗しました: {path}");
                        return;
                    }

                    LogLoadedShaders(log, "Plugins ファイル");
                    return;
                }

                if (TryLoadFromEmbedded(log, out AssetBundle fromEmbed))
                {
                    _bundle = fromEmbed;
                    LogLoadedShaders(log, "埋め込みリソース");
                    return;
                }

                log?.Info($"ShaderBundle: '{BundleFileName}' が Plugins にも DLL 埋め込みにもありません（カスタムシェーダ未同梱）。{pluginDir}");
            }
            catch (Exception ex)
            {
                log?.Error(ex);
            }
        }

        private static bool TryLoadFromEmbedded(IPALogger log, out AssetBundle bundle)
        {
            bundle = null;
            Assembly asm = Assembly.GetExecutingAssembly();
            string resourceName = FindBundleResourceName(asm);
            if (resourceName == null)
            {
                return false;
            }

            using (Stream stream = asm.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    log?.Warn($"ShaderBundle: 埋め込みストリームを開けませんでした: {resourceName}");
                    return false;
                }

                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    bundle = AssetBundle.LoadFromMemory(ms.ToArray());
                }
            }

            if (bundle == null)
            {
                log?.Warn("ShaderBundle: LoadFromMemory に失敗しました（埋め込みデータが壊れている可能性）。");
                return false;
            }

            return true;
        }

        private static string FindBundleResourceName(Assembly asm)
        {
            string[] names = asm.GetManifestResourceNames();
            if (names == null || names.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] != null && names[i].EndsWith(BundleFileName, StringComparison.OrdinalIgnoreCase))
                {
                    return names[i];
                }
            }

            return null;
        }

        private static void LogLoadedShaders(IPALogger log, string sourceLabel)
        {
            Shader[] shaders = _bundle.LoadAllAssets<Shader>();
            RegisterBundleShaders(shaders);
            int n = shaders != null ? shaders.Length : 0;
            log?.Info($"ShaderBundle: '{BundleFileName}'（{sourceLabel}）からシェーダ {n} 個を読み込みました。");
            for (int i = 0; i < n; i++)
            {
                if (shaders[i] != null)
                {
                    log?.Debug($"  - {shaders[i].name}");
                }
            }
        }

        private static void RegisterBundleShaders(Shader[] shaders)
        {
            if (_bundleShadersByName == null)
            {
                _bundleShadersByName = new Dictionary<string, Shader>(StringComparer.Ordinal);
            }
            else
            {
                _bundleShadersByName.Clear();
            }

            if (shaders == null)
            {
                return;
            }

            for (int i = 0; i < shaders.Length; i++)
            {
                Shader s = shaders[i];
                if (s != null && !string.IsNullOrEmpty(s.name))
                {
                    _bundleShadersByName[s.name] = s;
                }
            }
        }

        /// <summary>バンドルから読み込んだシェーダを名前で取得（あれば）。</summary>
        internal static bool TryGetShaderFromBundle(string shaderName, out Shader shader)
        {
            shader = null;
            if (string.IsNullOrEmpty(shaderName) || _bundleShadersByName == null)
            {
                return false;
            }

            return _bundleShadersByName.TryGetValue(shaderName, out shader) && shader != null;
        }

        internal static void UnloadQuietly()
        {
            _bundleShadersByName?.Clear();
            _bundleShadersByName = null;
            if (_bundle != null)
            {
                _bundle.Unload(false);
                _bundle = null;
            }
        }
    }
}
