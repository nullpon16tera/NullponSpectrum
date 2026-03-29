using System;
using System.Linq;
using NullponSpectrum.AudioSpectrums;
using NullponSpectrum.Configuration;
using NullponSpectrum.Controllers;
using NullponSpectrum.Utilities;
using NullponSpectrum.Views;
using SiraUtil;
using UnityEngine;
using Zenject;

namespace NullponSpectrum.Installers
{
    public class NullponSpectrumMenuInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            this.Container.BindInterfacesAndSelfTo<SettingTabViewController>().FromNewComponentAsViewController().AsSingle().NonLazy();

            if (!PluginConfig.Instance.Enable)
            {
                return;
            }

            // メニューで MenuStageVisualizer をオンオフしても反映されるよう、常にバインド（表示はコントローラ側で同期）
            this.Container.BindInterfacesAndSelfTo<FloorAdjustorUtil>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
            this.Container.BindInterfacesAndSelfTo<MenuFloorRootController>().AsCached().NonLazy();

            foreach (AudioSpectrum.BandType bandType in Enum.GetValues(typeof(AudioSpectrum.BandType)).Cast<AudioSpectrum.BandType>())
            {
                var audio = new GameObject("AudioSpectrumBand", typeof(AudioSpectrum)).GetComponent<AudioSpectrum>();
                audio.Band = bandType;
                this.Container.Bind<AudioSpectrum>().WithId(bandType).FromInstance(audio);
            }

            this.Container.BindInterfacesAndSelfTo<MenuStageVisualizerController>().AsCached().NonLazy();
            this.Container.BindInterfacesAndSelfTo<MenuSphereVisualizerController>().AsCached().NonLazy();
            this.Container.BindInterfacesAndSelfTo<MenuSpotlightVisualizerController>().AsCached().NonLazy();
        }
    }
}
