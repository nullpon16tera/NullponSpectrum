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
            this.Container.BindInterfacesAndSelfTo<Utilities.FloorAdjustorUtil>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
            this.Container.BindInterfacesAndSelfTo<AudioSpectrum>().FromNewComponentOn(new UnityEngine.GameObject(nameof(AudioSpectrum))).AsCached();
            this.Container.BindInterfacesAndSelfTo<CubeVisualizerController>().AsCached().NonLazy();
            this.Container.BindInterfacesAndSelfTo<FrameVisualizerController>().AsCached().NonLazy();
            this.Container.BindInterfacesAndSelfTo<LineVisualizerController>().AsCached().NonLazy();
            this.Container.BindInterfacesAndSelfTo<TileVisualizerController>().AsCached().NonLazy();
            this.Container.BindInterfacesAndSelfTo<MeshVisualizerController>().AsCached().NonLazy();
            this.Container.BindInterfacesAndSelfTo<StripeVisualizerController>().AsCached().NonLazy();
        }
    }
}
