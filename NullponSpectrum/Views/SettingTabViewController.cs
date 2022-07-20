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

        [UIValue("isFloorHeight")]
        public bool isFloorHeight
        {
            get => conf.isFloorHeight;
            set => conf.isFloorHeight = value;
        }

        [UIValue("floorHeight")]
        public float floorHeight
        {
            get => conf.floorHeight;
            set => conf.floorHeight = value;
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

        [UIValue("TileVisualizer")]
        public bool TileVisualizer
        {
            get => conf.TileVisualizer;
            set
            {
                if (value)
                {
                    if (conf.MeshVisualizer) conf.MeshVisualizer = false;
                    if (conf.StripeVisualizer) conf.StripeVisualizer = false;
                }

                conf.TileVisualizer = value;

                updateUI();
            }
        }

        [UIValue("MeshVisualizer")]
        public bool MeshVisualizer
        {
            get => conf.MeshVisualizer;
            set
            {
                if (value)
                {
                    if (conf.StripeVisualizer) conf.StripeVisualizer = false;
                    if (conf.TileVisualizer) conf.TileVisualizer = false;
                }

                conf.MeshVisualizer = value;

                updateUI();
            }
        }

        [UIValue("StripeVisualizer")]
        public bool StripeVisualizer
        {
            get => conf.StripeVisualizer;
            set
            {
                if (value)
                {
                    if (conf.MeshVisualizer) conf.MeshVisualizer = false;
                    if (conf.TileVisualizer) conf.TileVisualizer = false;
                }

                conf.StripeVisualizer = value;

                updateUI();
            }
        }

        [UIValue("SphereVisualizer")]
        public bool SphereVisualizer
        {
            get => conf.SphereVisualizer;
            set => conf.SphereVisualizer = value;
        }

        [UIValue("StageVisualizer")]
        public bool StageVisualizer
        {
            get => conf.StageVisualizer;
            set => conf.StageVisualizer = value;
        }

        protected override void OnDestroy()
        {
            GameplaySetup.instance.RemoveTab("Nullpon Spectrum");
            base.OnDestroy();
        }

        public void Initialize() => GameplaySetup.instance.AddTab("Nullpon Spectrum", this.ResourceName, this);
    }
}
