using NullponSpectrum.Controllers;
using NullponSpectrum.Views;
using SiraUtil;
using Zenject;

namespace NullponSpectrum.Installers
{
    public class NullponSpectrumMenuInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            this.Container.BindInterfacesAndSelfTo<SettingTabViewController>().FromNewComponentAsViewController().AsSingle().NonLazy();
        }
    }
}
