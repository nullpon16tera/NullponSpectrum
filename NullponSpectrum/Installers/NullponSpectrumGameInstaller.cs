using NullponSpectrum.Configuration;
using NullponSpectrum.AudioSpectrums;
using NullponSpectrum.Controllers;
using Zenject;

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
            //this.Container.BindInterfacesAndSelfTo<AudioSpectrum>().FromNewComponentOn(new UnityEngine.GameObject(nameof(AudioSpectrum))).AsCached();
            if (PluginConfig.Instance.CubeVisualizer || PluginConfig.Instance.FrameVisualizer || PluginConfig.Instance.LineVisualizer)
            {
                this.Container.BindInterfacesAndSelfTo<AudioSpectrum4>().FromNewComponentOn(new UnityEngine.GameObject(nameof(AudioSpectrum4))).AsCached();
                if (PluginConfig.Instance.CubeVisualizer)
                {
                    this.Container.BindInterfacesAndSelfTo<CubeVisualizerController>().AsCached().NonLazy();
                }
                if (PluginConfig.Instance.FrameVisualizer)
                {
                    this.Container.BindInterfacesAndSelfTo<FrameVisualizerController>().AsCached().NonLazy();
                }
                if (PluginConfig.Instance.LineVisualizer)
                {
                    this.Container.BindInterfacesAndSelfTo<LineVisualizerController>().AsCached().NonLazy();
                }
            }
            if (PluginConfig.Instance.TileVisualizer)
            {
                this.Container.BindInterfacesAndSelfTo<AudioSpectrum8>().FromNewComponentOn(new UnityEngine.GameObject(nameof(AudioSpectrum8))).AsCached();
                if (PluginConfig.Instance.TileVisualizer)
                {
                    this.Container.BindInterfacesAndSelfTo<TileVisualizerController>().AsCached().NonLazy();
                }
            }
            if (PluginConfig.Instance.MeshVisualizer)
            {
                this.Container.BindInterfacesAndSelfTo<AudioSpectrum26>().FromNewComponentOn(new UnityEngine.GameObject(nameof(AudioSpectrum26))).AsCached();
                if (PluginConfig.Instance.MeshVisualizer)
                {
                    this.Container.BindInterfacesAndSelfTo<MeshVisualizerController>().AsCached().NonLazy();
                }
            }
            if (PluginConfig.Instance.StripeVisualizer || PluginConfig.Instance.SphereVisualizer || PluginConfig.Instance.UneUneVisualizer)
            {
                this.Container.BindInterfacesAndSelfTo<AudioSpectrum31>().FromNewComponentOn(new UnityEngine.GameObject(nameof(AudioSpectrum31))).AsCached();
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
            }
        }
    }
}
