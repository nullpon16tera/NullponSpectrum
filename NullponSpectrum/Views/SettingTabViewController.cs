﻿using NullponSpectrum.Configuration;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberMarkupLanguage.Parser;
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

        [UIValue("enableFloorObject")]
        public bool enableFloorObject
        {
            get => conf.enableFloorObject;
            set => conf.enableFloorObject = value;
        }

        [UIValue("enableMerihari")]
        public bool enableMerihari
        {
            get => conf.enableMerihari;
            set => conf.enableMerihari = value;
        }

        [UIValue("listOptions")]
        private List<object> options = new object[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 }.ToList();

        [UIValue("listChoice")]
        public int listChoice
        {
            get => conf.listChoice;
            set => conf.listChoice = value;
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

        [UIValue("FrameFlowingVisualizer")]
        public bool FrameFlowingVisualizer
        {
            get => conf.FrameFlowingVisualizer;
            set => conf.FrameFlowingVisualizer = value;
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
                    if (conf.RainbowVisualizer) conf.RainbowVisualizer = false;
                    if (conf.RainbowBugVisualizer) conf.RainbowBugVisualizer = false;
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
                    if (conf.RainbowVisualizer) conf.RainbowVisualizer = false;
                    if (conf.RainbowBugVisualizer) conf.RainbowBugVisualizer = false;
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
                    if (conf.RainbowVisualizer) conf.RainbowVisualizer = false;
                    if (conf.RainbowBugVisualizer) conf.RainbowBugVisualizer = false;
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

        [UIValue("UneUneVisualizer")]
        public bool UneUneVisualizer
        {
            get => conf.UneUneVisualizer;
            set => conf.UneUneVisualizer = value;
        }

        [UIValue("LinebowVisualizer")]
        public bool LinebowVisualizer
        {
            get => conf.LinebowVisualizer;
            set => conf.LinebowVisualizer = value;
        }

        [UIValue("BoxVisualizer")]
        public bool BoxVisualizer
        {
            get => conf.BoxVisualizer;
            set => conf.BoxVisualizer = value;
        }


        [UIValue("RainbowVisualizer")]
        public bool RainbowVisualizer
        {
            get => conf.RainbowVisualizer;
            set
            {
                if (value)
                {
                    if (conf.MeshVisualizer) conf.MeshVisualizer = false;
                    if (conf.TileVisualizer) conf.TileVisualizer = false;
                    if (conf.StripeVisualizer) conf.StripeVisualizer = false;
                    if (conf.RainbowBugVisualizer) conf.RainbowBugVisualizer = false;
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
                if (value)
                {
                    if (conf.MeshVisualizer) conf.MeshVisualizer = false;
                    if (conf.TileVisualizer) conf.TileVisualizer = false;
                    if (conf.StripeVisualizer) conf.StripeVisualizer = false;
                    if (conf.RainbowVisualizer) conf.RainbowVisualizer = false;
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
