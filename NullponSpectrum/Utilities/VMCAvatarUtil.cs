using IPA.Loader;
using System;
using System.Reflection;
using UnityEngine;
using Zenject;

namespace NullponSpectrum.Utilities
{
    public class VMCAvatarUtil
    {
        public bool IsInstallVRMAvatar { get; private set; }
        private readonly object _loader;
        public bool Enabled => this._loader != null && (bool)this._loader.GetType().GetProperty("Enabled").GetValue(this._loader);
        private static readonly Type s_vrmAvatar;
        private static readonly PropertyInfo s_vrmAvatarFloorOffsetInfo;
        public Vector3 FloorOffset { get; private set; } = Vector3.zero;

        static VMCAvatarUtil()
        {
            s_vrmAvatar = Type.GetType("VRMAvatar.AvatarController, VRMAvatar");
            Plugin.Log.Debug($"VRMAvatar: {s_vrmAvatar}");
            if (s_vrmAvatar == null)
            {
                s_vrmAvatarFloorOffsetInfo = null;
            }
            else
            {
                s_vrmAvatarFloorOffsetInfo = s_vrmAvatar.GetProperty("FloorOffset", BindingFlags.Instance | BindingFlags.Public);
            }
        }

        [Inject]
        public VMCAvatarUtil(DiContainer container)
        {
            this.IsInstallVRMAvatar = PluginManager.GetPluginFromId("VRMAvatar") != null;
        }

        public void GetFloorOffset()
        {
            if (!this.IsInstallVRMAvatar)
            {
                return;
            }
            if (s_vrmAvatar == null || s_vrmAvatarFloorOffsetInfo == null)
            {
                return;
            }
            
            FloorOffset = (Vector3)s_vrmAvatarFloorOffsetInfo.GetValue(s_vrmAvatar);
            Plugin.Log.Debug($"FloorOffset: {FloorOffset}");
        }
    }
}
