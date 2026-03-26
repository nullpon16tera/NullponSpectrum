using System;
using NullponSpectrum.Configuration;
using NullponSpectrum.Utilities;
using UnityEngine;
using Zenject;

namespace NullponSpectrum.Controllers
{
    /// <summary>
    /// メニュー用ビジュアライザの親。プレイ用 <see cref="FloorViewController"/> とは別に、空の Root のみ生成する。
    /// 位置・親は FloorViewController と同様（NullponSpectrumFloor 直下、local Y = 0.0051f ± 床高設定）。
    /// 床高は <see cref="ApplyMenuFloorHeightFromConfig"/>（設定タブ）と <see cref="PluginConfig.OnReloaded"/> で追従。LateTick ポーリングは行わない。
    /// </summary>
    internal class MenuFloorRootController : IInitializable, IDisposable
    {
        public static GameObject MenuVisualizerFloorRoot { get; private set; }

        public void Initialize()
        {
            if (FloorAdjustorUtil.NullponSpectrumFloor == null)
            {
                return;
            }

            MenuVisualizerFloorRoot = new GameObject("menuVisualizerFloorRoot");
            MenuVisualizerFloorRoot.transform.SetParent(FloorAdjustorUtil.NullponSpectrumFloor.transform, false);

            ApplyMenuFloorRootLocalY();
            if (PluginConfig.Instance != null)
            {
                PluginConfig.Instance.OnReloaded += this.OnPluginConfigReloaded;
            }
        }

        private void OnPluginConfigReloaded(PluginConfig _)
        {
            ApplyMenuFloorRootLocalY();
        }

        /// <summary>設定タブで床高が変わった直後に呼ぶ。メニュー床の Y を即反映する。</summary>
        public static void ApplyMenuFloorHeightFromConfig()
        {
            ApplyMenuFloorRootLocalY();
        }

        /// <summary><see cref="FloorViewController.Initialize"/> と同じ Y オフセット（床高スライダー × 0.01）。</summary>
        private static void ApplyMenuFloorRootLocalY()
        {
            if (MenuVisualizerFloorRoot == null)
            {
                return;
            }

            float y = 0.0051f;
            if (PluginConfig.Instance != null && PluginConfig.Instance.isFloorHeight)
            {
                y += PluginConfig.Instance.floorHeight * 0.01f;
            }

            Vector3 p = MenuVisualizerFloorRoot.transform.localPosition;
            p.y = y;
            MenuVisualizerFloorRoot.transform.localPosition = p;
        }

        public void Dispose()
        {
            if (PluginConfig.Instance != null)
            {
                PluginConfig.Instance.OnReloaded -= this.OnPluginConfigReloaded;
            }
        }
    }
}
