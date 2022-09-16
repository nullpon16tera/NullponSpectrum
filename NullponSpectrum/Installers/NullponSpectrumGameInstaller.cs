using NullponSpectrum.Configuration;
using NullponSpectrum.AudioSpectrums;
using NullponSpectrum.Controllers;
using Zenject;
using System;
using System.Linq;
using UnityEngine;

namespace NullponSpectrum.Installers
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
	public class NullponSpectrumGameInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }
            this.Container.BindInterfacesAndSelfTo<Utilities.FloorAdjustorUtil>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
            this.Container.BindInterfacesAndSelfTo<FloorViewController>().AsCached().NonLazy();
            this.Container.BindInterfacesAndSelfTo<Utilities.VisualizerUtil>().AsCached().NonLazy();
            //this.Container.BindInterfacesAndSelfTo<AudioSpectrum>().FromNewComponentOn(new UnityEngine.GameObject(nameof(AudioSpectrum))).AsCached();

            foreach (var bandType in Enum.GetValues(typeof(AudioSpectrum.BandType)).OfType<AudioSpectrum.BandType>())
            {
                var audio = new GameObject("AudioSpectrumBand", typeof(AudioSpectrum)).GetComponent<AudioSpectrum>();
                audio.Band = bandType;
                this.Container.Bind<AudioSpectrum>().WithId(bandType).FromInstance(audio);
            }

            if (PluginConfig.Instance.CubeVisualizer || PluginConfig.Instance.FrameVisualizer)
            {
                if (PluginConfig.Instance.CubeVisualizer)
                {
                    this.Container.BindInterfacesAndSelfTo<CubeVisualizerController>().AsCached().NonLazy();
                }
                if (PluginConfig.Instance.FrameVisualizer)
                {
                    this.Container.BindInterfacesAndSelfTo<FrameVisualizerController>().AsCached().NonLazy();
                }
            }
            if (PluginConfig.Instance.TileVisualizer)
            {
                if (PluginConfig.Instance.TileVisualizer)
                {
                    this.Container.BindInterfacesAndSelfTo<TileVisualizerController>().AsCached().NonLazy();
                }
            }
            if (PluginConfig.Instance.MeshVisualizer)
            {
                if (PluginConfig.Instance.MeshVisualizer)
                {
                    this.Container.BindInterfacesAndSelfTo<MeshVisualizerController>().AsCached().NonLazy();
                }
            }
            if (PluginConfig.Instance.LineVisualizer || PluginConfig.Instance.StripeVisualizer || PluginConfig.Instance.SphereVisualizer || PluginConfig.Instance.UneUneVisualizer || PluginConfig.Instance.RainbowVisualizer)
            {
                if (PluginConfig.Instance.LineVisualizer)
                {
                    this.Container.BindInterfacesAndSelfTo<LineVisualizerController>().AsCached().NonLazy();
                }
                if (PluginConfig.Instance.StripeVisualizer)
                {
                    this.Container.BindInterfacesAndSelfTo<StripeVisualizerController>().AsCached().NonLazy();
                }
                if (PluginConfig.Instance.SphereVisualizer)
                {
                    this.Container.BindInterfacesAndSelfTo<SphereVisualizerController>().AsCached().NonLazy();
                }
                if (PluginConfig.Instance.UneUneVisualizer)
                {
                    this.Container.BindInterfacesAndSelfTo<UneUneVisualizerController>().AsCached().NonLazy();
                }
                if (PluginConfig.Instance.RainbowVisualizer)
                {
                    this.Container.BindInterfacesAndSelfTo<RainbowVisualizerController>().AsCached().NonLazy();
                }
            }
        }
    }
}
