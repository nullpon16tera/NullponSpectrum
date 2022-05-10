using System;
using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace NullponSpectrum.Configuration
{
    internal class PluginConfig
    {
        public static PluginConfig Instance { get; set; }
        public virtual bool Enable { get; set; } = false; // Must be 'virtual' if you want BSIPA to detect a value change and save the config automatically.
        public virtual bool CubeVisualizer { get; set; } = false;
        public virtual bool FrameVisualizer { get; set; } = false;
        public virtual bool MeshVisualizer { get; set; } = false;

        public event Action<PluginConfig> OnReloaded;

        /// <summary>
        /// This is called whenever BSIPA reads the config from disk (including when file changes are detected).
        /// </summary>
        public virtual void OnReload()
        {
            // Do stuff after config is read from disk.
            this.OnReloaded?.Invoke(this);
        }

        /// <summary>
        /// Call this to force BSIPA to update the config file. This is also called by BSIPA if it detects the file was modified.
        /// </summary>
        public virtual void Changed()
        {
            // Do stuff when the config is changed.
        }

        /// <summary>
        /// Call this to have BSIPA copy the values from <paramref name="other"/> into this config.
        /// </summary>
        public virtual void CopyFrom(PluginConfig other)
        {
            // This instance's members populated from other
            this.Enable = other.Enable;
        }
    }
}
