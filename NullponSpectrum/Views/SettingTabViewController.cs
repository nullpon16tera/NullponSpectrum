using NullponSpectrum.Configuration;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using BeatSaberMarkupLanguage.ViewControllers;
using Zenject;


namespace NullponSpectrum.Views
{
    [HotReload]
    internal class SettingTabViewController : BSMLAutomaticViewController, IInitializable
    {
        public string ResourceName => string.Join(".", this.GetType().Namespace, this.GetType().Name);

        [UIValue("enable")]
        public bool Enable
        {
            get => PluginConfig.Instance.Enable;
            set => PluginConfig.Instance.Enable = value;
        }

        [UIValue("CubeVisualizer")]
        public bool CubeVisualizer
        {
            get => PluginConfig.Instance.CubeVisualizer;
            set => PluginConfig.Instance.CubeVisualizer = value;
        }

        [UIValue("FrameVisualizer")]
        public bool FrameVisualizer
        {
            get => PluginConfig.Instance.FrameVisualizer;
            set => PluginConfig.Instance.FrameVisualizer = value;
        }

        protected override void OnDestroy()
        {
            GameplaySetup.instance.RemoveTab("Nullpon Spectrum");
            base.OnDestroy();
        }

        public void Initialize() => GameplaySetup.instance.AddTab("Nullpon Spectrum", this.ResourceName, this);
    }
}
