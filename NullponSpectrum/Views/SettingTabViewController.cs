using NullponSpectrum.Configuration;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberMarkupLanguage.Parser;
using Zenject;

namespace NullponSpectrum.Views
{
    [HotReload]
    internal class SettingTabViewController : BSMLAutomaticViewController, IInitializable
    {
        public string ResourceName => string.Join(".", this.GetType().Namespace, this.GetType().Name);

        private static PluginConfig conf = PluginConfig.Instance;

        [UIParams]
        BSMLParserParams parserParams;

        public void updateUI()
        {
            parserParams.EmitEvent("cancel");
        }

        [UIValue("enable")]
        public bool Enable
        {
            get => conf.Enable;
            set => conf.Enable = value;
        }

        [UIValue("CubeVisualizer")]
        public bool CubeVisualizer
        {
            get => conf.CubeVisualizer;
            set => conf.CubeVisualizer = value;
        }

        [UIValue("FrameVisualizer")]
        public bool FrameVisualizer
        {
            get => conf.FrameVisualizer;
            set => conf.FrameVisualizer = value;
        }

        [UIValue("LineVisualizer")]
        public bool LineVisualizer
        {
            get => conf.LineVisualizer;
            set => conf.LineVisualizer = value;
        }

        [UIValue("MeshVisualizer")]
        public bool MeshVisualizer
        {
            get => conf.MeshVisualizer;
            set
            {
                if (value)
                {
                    if (conf.BarVisualizer) conf.BarVisualizer = false;
                }

                conf.MeshVisualizer = value;

                updateUI();
            }
        }

        [UIValue("BarVisualizer")]
        public bool BarVisualizer
        {
            get => conf.BarVisualizer;
            set
            {
                if (value)
                {
                    if (conf.MeshVisualizer) conf.MeshVisualizer = false;
                }

                conf.BarVisualizer = value;

                updateUI();
            }
        }

        protected override void OnDestroy()
        {
            GameplaySetup.instance.RemoveTab("Nullpon Spectrum");
            base.OnDestroy();
        }

        public void Initialize() => GameplaySetup.instance.AddTab("Nullpon Spectrum", this.ResourceName, this);
    }
}
