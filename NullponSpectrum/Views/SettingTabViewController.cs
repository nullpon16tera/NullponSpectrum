using NullponSpectrum.Controllers;
using NullponSpectrum.Configuration;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberMarkupLanguage.Parser;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using System.Collections.Generic;
using System.Linq;

namespace NullponSpectrum.Views
{
    [HotReload]
    internal class SettingTabViewController : BSMLAutomaticViewController, IInitializable
    {
        public string ResourceName => string.Join(".", this.GetType().Namespace, this.GetType().Name);

        private static PluginConfig conf = PluginConfig.Instance;

        [UIParams]
        BSMLParserParams parserParams;

        /// <summary>Gameplay Setup のタブ内で ScrollRect に親からの高さが付かないため、LayoutElement で確保する。</summary>
        private const float ScrollPreferredHeightSettings = 54f;
        private const float ScrollPreferredHeightVisualizer = 46f;

        [UIComponent("nullpon-scroll-settings")]
        private RectTransform _scrollSettingsContent;

        [UIComponent("nullpon-scroll-visualizer")]
        private RectTransform _scrollVisualizerContent;

        [UIAction("#post-parse")]
        private void PostParseScrollHeights()
        {
            this.ApplyScrollPreferredHeight(this._scrollSettingsContent, ScrollPreferredHeightSettings);
            this.ApplyScrollPreferredHeight(this._scrollVisualizerContent, ScrollPreferredHeightVisualizer);
        }

        private void ApplyScrollPreferredHeight(RectTransform content, float preferredHeight)
        {
            if (content == null)
            {
                return;
            }

            ScrollRect scrollRect = content.GetComponentInParent<ScrollRect>();
            if (scrollRect == null)
            {
                return;
            }

            var scrollRoot = scrollRect.transform as RectTransform;
            LayoutElement layout = scrollRoot.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = scrollRoot.gameObject.AddComponent<LayoutElement>();
            }

            layout.preferredHeight = preferredHeight;
            layout.minHeight = Mathf.Max(28f, preferredHeight * 0.5f);
            layout.flexibleHeight = 0f;
        }

        public void updateUI()
        {
            parserParams.EmitEvent("cancel");
        }

        [UIValue("enable")]
        public bool Enable
        {
            get => conf.Enable;
            set
            {
                if (conf.Enable == value) return;
                conf.Enable = value;
            }
        }

        [UIValue("realtimeSaberColorUpdates")]
        public bool RealtimeSaberColorUpdates
        {
            get => conf.RealtimeSaberColorUpdates;
            set
            {
                if (conf.RealtimeSaberColorUpdates == value) return;
                conf.RealtimeSaberColorUpdates = value;
            }
        }

        [UIValue("isFloorHeight")]
        public bool isFloorHeight
        {
            get => conf.isFloorHeight;
            set
            {
                if (conf.isFloorHeight == value) return;
                conf.isFloorHeight = value;
                MenuFloorRootController.ApplyMenuFloorHeightFromConfig();
            }
        }

        [UIValue("floorHeight")]
        public float floorHeight
        {
            get => conf.floorHeight;
            set
            {
                if (Mathf.Approximately(conf.floorHeight, value)) return;
                conf.floorHeight = value;
                MenuFloorRootController.ApplyMenuFloorHeightFromConfig();
            }
        }

        [UIValue("enableFloorObject")]
        public bool enableFloorObject
        {
            get => conf.enableFloorObject;
            set
            {
                if (conf.enableFloorObject == value) return;
                conf.enableFloorObject = value;
            }
        }

        [UIValue("enableMerihari")]
        public bool enableMerihari
        {
            get => conf.enableMerihari;
            set
            {
                if (conf.enableMerihari == value) return;
                conf.enableMerihari = value;
            }
        }

        [UIValue("listOptions")]
        private List<object> options = new object[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 }.ToList();

        [UIValue("listChoice")]
        public int listChoice
        {
            get => conf.listChoice;
            set
            {
                if (conf.listChoice == value) return;
                conf.listChoice = value;
            }
        }



        [UIValue("CubeVisualizer")]
        public bool CubeVisualizer
        {
            get => conf.CubeVisualizer;
            set
            {
                if (conf.CubeVisualizer == value) return;
                conf.CubeVisualizer = value;
            }
        }

        [UIValue("FrameVisualizer")]
        public bool FrameVisualizer
        {
            get => conf.FrameVisualizer;
            set
            {
                if (conf.FrameVisualizer == value) return;
                conf.FrameVisualizer = value;
            }
        }

        [UIValue("FrameFlowingVisualizer")]
        public bool FrameFlowingVisualizer
        {
            get => conf.FrameFlowingVisualizer;
            set
            {
                if (conf.FrameFlowingVisualizer == value) return;
                conf.FrameFlowingVisualizer = value;
            }
        }

        [UIValue("LineVisualizer")]
        public bool LineVisualizer
        {
            get => conf.LineVisualizer;
            set
            {
                if (conf.LineVisualizer == value) return;
                conf.LineVisualizer = value;
            }
        }

        [UIValue("ParticleVisualizer")]
        public bool ParticleVisualizer
        {
            get => conf.ParticleVisualizer;
            set
            {
                if (conf.ParticleVisualizer == value) return;
                if (value)
                {
                    if (conf.CutVisualizer) conf.CutVisualizer = false;
                }

                conf.ParticleVisualizer = value;

                updateUI();
            }
        }

        [UIValue("CutVisualizer")]
        public bool CutVisualizer
        {
            get => conf.CutVisualizer;
            set
            {
                if (conf.CutVisualizer == value) return;
                if (value)
                {
                    if (conf.ParticleVisualizer) conf.ParticleVisualizer = false;
                }

                conf.CutVisualizer = value;

                updateUI();
            }
        }

        [UIValue("TileVisualizer")]
        public bool TileVisualizer
        {
            get => conf.TileVisualizer;
            set
            {
                if (conf.TileVisualizer == value) return;
                if (value)
                {
                    if (conf.MeshVisualizer) conf.MeshVisualizer = false;
                    if (conf.StripeVisualizer) conf.StripeVisualizer = false;
                    if (conf.RainbowVisualizer) conf.RainbowVisualizer = false;
                    if (conf.RainbowBugVisualizer) conf.RainbowBugVisualizer = false;
                    if (conf.StageVisualizer) conf.StageVisualizer = false;
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
                if (conf.MeshVisualizer == value) return;
                if (value)
                {
                    if (conf.StripeVisualizer) conf.StripeVisualizer = false;
                    if (conf.TileVisualizer) conf.TileVisualizer = false;
                    if (conf.RainbowVisualizer) conf.RainbowVisualizer = false;
                    if (conf.RainbowBugVisualizer) conf.RainbowBugVisualizer = false;
                    if (conf.StageVisualizer) conf.StageVisualizer = false;
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
                if (conf.StripeVisualizer == value) return;
                if (value)
                {
                    if (conf.MeshVisualizer) conf.MeshVisualizer = false;
                    if (conf.TileVisualizer) conf.TileVisualizer = false;
                    if (conf.RainbowVisualizer) conf.RainbowVisualizer = false;
                    if (conf.RainbowBugVisualizer) conf.RainbowBugVisualizer = false;
                    if (conf.StageVisualizer) conf.StageVisualizer = false;
                }

                conf.StripeVisualizer = value;

                updateUI();
            }
        }

        [UIValue("StageVisualizer")]
        public bool StageVisualizer
        {
            get => conf.StageVisualizer;
            set
            {
                if (conf.StageVisualizer == value) return;
                if (value)
                {
                    if (conf.MeshVisualizer) conf.MeshVisualizer = false;
                    if (conf.TileVisualizer) conf.TileVisualizer = false;
                    if (conf.RainbowVisualizer) conf.RainbowVisualizer = false;
                    if (conf.RainbowBugVisualizer) conf.RainbowBugVisualizer = false;
                    if (conf.StripeVisualizer) conf.StripeVisualizer = false;
                }

                conf.StageVisualizer = value;

                updateUI();
            }
        }

        [UIValue("MenuStageVisualizer")]
        public bool MenuStageVisualizer
        {
            get => conf.MenuStageVisualizer;
            set
            {
                if (conf.MenuStageVisualizer == value) return;
                conf.MenuStageVisualizer = value;
            }
        }

        [UIValue("MenuSphereVisualizer")]
        public bool MenuSphereVisualizer
        {
            get => conf.MenuSphereVisualizer;
            set
            {
                if (conf.MenuSphereVisualizer == value) return;
                conf.MenuSphereVisualizer = value;
            }
        }

        [UIValue("MenuSpotlightVisualizer")]
        public bool MenuSpotlightVisualizer
        {
            get => conf.MenuSpotlightVisualizer;
            set
            {
                if (conf.MenuSpotlightVisualizer == value) return;
                conf.MenuSpotlightVisualizer = value;
            }
        }

        [UIValue("SphereVisualizer")]
        public bool SphereVisualizer
        {
            get => conf.SphereVisualizer;
            set
            {
                if (conf.SphereVisualizer == value) return;
                conf.SphereVisualizer = value;
            }
        }

        [UIValue("UneUneVisualizer")]
        public bool UneUneVisualizer
        {
            get => conf.UneUneVisualizer;
            set
            {
                if (conf.UneUneVisualizer == value) return;
                conf.UneUneVisualizer = value;
            }
        }

        [UIValue("LinebowVisualizer")]
        public bool LinebowVisualizer
        {
            get => conf.LinebowVisualizer;
            set
            {
                if (conf.LinebowVisualizer == value) return;
                conf.LinebowVisualizer = value;
            }
        }

        [UIValue("BoxVisualizer")]
        public bool BoxVisualizer
        {
            get => conf.BoxVisualizer;
            set
            {
                if (conf.BoxVisualizer == value) return;
                conf.BoxVisualizer = value;
            }
        }


        [UIValue("RainbowVisualizer")]
        public bool RainbowVisualizer
        {
            get => conf.RainbowVisualizer;
            set
            {
                if (conf.RainbowVisualizer == value) return;
                if (value)
                {
                    if (conf.MeshVisualizer) conf.MeshVisualizer = false;
                    if (conf.TileVisualizer) conf.TileVisualizer = false;
                    if (conf.StripeVisualizer) conf.StripeVisualizer = false;
                    if (conf.RainbowBugVisualizer) conf.RainbowBugVisualizer = false;
                    if (conf.StageVisualizer) conf.StageVisualizer = false;
                }
                conf.RainbowVisualizer = value;
                updateUI();
            }
        }

        [UIValue("RainbowBugVisualizer")]
        public bool RainbowBugVisualizer
        {
            get => conf.RainbowBugVisualizer;
            set
            {
                if (conf.RainbowBugVisualizer == value) return;
                if (value)
                {
                    if (conf.MeshVisualizer) conf.MeshVisualizer = false;
                    if (conf.TileVisualizer) conf.TileVisualizer = false;
                    if (conf.StripeVisualizer) conf.StripeVisualizer = false;
                    if (conf.RainbowVisualizer) conf.RainbowVisualizer = false;
                    if (conf.StageVisualizer) conf.StageVisualizer = false;
                }
                conf.RainbowBugVisualizer = value;
                updateUI();
            }
        }

        protected override void OnDestroy()
        {
            GameplaySetup.Instance.RemoveTab("Nullpon Spectrum");
            base.OnDestroy();
        }

        public void Initialize() => GameplaySetup.Instance.AddTab("Nullpon Spectrum", this.ResourceName, this);
    }
}
