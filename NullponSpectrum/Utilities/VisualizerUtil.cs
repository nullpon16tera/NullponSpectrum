using System;
using System.Linq;
using NullponSpectrum.Configuration;
using SiraUtil.Sabers;
using UnityEngine;
using Zenject;

namespace NullponSpectrum.Utilities
{
    public class VisualizerUtil : IInitializable, ILateTickable
    {

        public static Color saberAColor { get; private set; }
        public static Color saberBColor { get; private set; }
        private static float[] leftHSV = new float[3];
        private static float[] rightHSV = new float[3];

        private static float updateTime = 0;
        private static bool needUpdate = false;

        /// <summary>
        /// 波形をずらす秒数の閾値(sec)
        /// </summary>
        /// <remarks>設定ファイルに逃がしてもいいし、曲のBPMと連動させてもいい</remarks>
        private static readonly float s_updateThresholdTime = 0.025f;

        public static bool GetNeedUpdate()
        {
            return needUpdate;
        }

        public static Shader GetShader(string name)
        {
            if (ShaderBundleLoader.TryGetShaderFromBundle(name, out Shader fromBundle))
            {
                return fromBundle;
            }

            var shaderes = Resources.FindObjectsOfTypeAll<Shader>();
            Shader shader = shaderes.FirstOrDefault(x => x.name == name);
            return shader;
        }

        public static void SetUpdateTime(float time)
        {
            updateTime = time;
        }

        public static void ResetUpdateTime()
        {
            updateTime = 0;
        }

        public static float[] GetLeftSaberHSV()
        {
            return leftHSV;
        }

        public static float[] GetRightSaberHSV()
        {
            return rightHSV;
        }

        /// <summary>プレイ中の左右セイバー色を反映（Initialize やセイバー差し替え直後など、列挙のフルスキャンが必要なとき）。</summary>
        public void RefreshSaberColorsNow()
        {
            this.RefreshSaberColorsFromGame(forceSaberRescan: true);
        }

        /// <summary>
        /// 床スペクトラムの更新フレーム用。Saber の列挙は間引きキャッシュを使い、毎回 FindObjectsOfTypeAll しない。
        /// </summary>
        public void RefreshSaberColorsForSpectrumFrame()
        {
            this.RefreshSaberColorsFromGame(forceSaberRescan: false);
        }

        public static IAudioTimeSource GetAudioTimeSource()
        {
            return timeSource;
        }

        public static GameplayCoreSceneSetupData GetCurrentmap()
        {
            return Currentmap;
        }

        public static float GetBeatsPerMinute()
        {
            return Currentmap.beatmapLevel.beatsPerMinute;
        }

        /// <summary>SiraUtil の物理セイバー色 → Zenject 注入の ColorManager → ColorScheme。</summary>
        /// <param name="forceSaberRescan">true のときは毎回 Saber を列挙（明示更新）。LateTick は false で間引き。</param>
        private void RefreshSaberColorsFromGame(bool forceSaberRescan = false)
        {
            if (this.TryGetSiraPhysicalSaberColors(out Color cA, out Color cB, forceSaberRescan))
            {
                saberAColor = cA;
                saberBColor = cB;
            }
            else if (this._colorManager != null)
            {
                saberAColor = this._colorManager.ColorForSaberType(SaberType.SaberA);
                saberBColor = this._colorManager.ColorForSaberType(SaberType.SaberB);
            }
            else
            {
                saberAColor = this._colorScheme.saberAColor;
                saberBColor = this._colorScheme.saberBColor;
            }

            float leftH, leftS, leftV;
            float rightH, rightS, rightV;

            Color.RGBToHSV(saberAColor, out leftH, out leftS, out leftV);
            Color.RGBToHSV(saberBColor, out rightH, out rightS, out rightV);
            leftHSV[0] = leftH;
            rightHSV[0] = rightH;
            leftHSV[1] = leftS;
            rightHSV[1] = rightS;
            leftHSV[2] = leftV;
            rightHSV[2] = rightV;
        }

        /// <summary>SiraUtil が入っていて Saber が揃うとき、モデル実色（カスタムセイバー等）を優先。</summary>
        private bool TryGetSiraPhysicalSaberColors(out Color colorA, out Color colorB, bool forceRescan)
        {
            colorA = default;
            colorB = default;
            if (this._saberModelManager == null)
            {
                this._cachedSaberA = null;
                this._cachedSaberB = null;
                return false;
            }

            int frame = Time.frameCount;
            bool cacheValid = !forceRescan
                && this._cachedSaberA != null
                && this._cachedSaberB != null
                && frame - this._cachedSaberScanFrame < SaberRescanIntervalFrames;

            if (cacheValid)
            {
                colorA = this._saberModelManager.GetPhysicalSaberColor(this._cachedSaberA);
                colorB = this._saberModelManager.GetPhysicalSaberColor(this._cachedSaberB);
                return true;
            }

            Saber saberA = null;
            Saber saberB = null;
            Saber[] sabers = Resources.FindObjectsOfTypeAll<Saber>();
            for (int i = 0; i < sabers.Length; i++)
            {
                Saber s = sabers[i];
                if (s == null)
                {
                    continue;
                }

                if (!s.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (s.saberType == SaberType.SaberA && saberA == null)
                {
                    saberA = s;
                }
                else if (s.saberType == SaberType.SaberB && saberB == null)
                {
                    saberB = s;
                }

                if (saberA != null && saberB != null)
                {
                    break;
                }
            }

            this._cachedSaberScanFrame = frame;
            this._cachedSaberA = saberA;
            this._cachedSaberB = saberB;

            if (saberA == null || saberB == null)
            {
                return false;
            }

            colorA = this._saberModelManager.GetPhysicalSaberColor(saberA);
            colorB = this._saberModelManager.GetPhysicalSaberColor(saberB);
            return true;
        }

        public void LateTick()
        {
            if (!PluginConfig.Instance.HasAnyActiveSpectrumFloorVisualizer())
            {
                needUpdate = false;
                return;
            }

            if (PluginConfig.Instance.RealtimeSaberColorUpdates)
            {
                this.RefreshSaberColorsFromGame(forceSaberRescan: false);
            }

            updateTime += Time.deltaTime;
            var bpmSpeed = -(GetBeatsPerMinute() * 0.00001f);
            needUpdate = (s_updateThresholdTime + bpmSpeed) < updateTime;
        }

        public void Initialize()
        {
            this.RefreshSaberColorsFromGame(forceSaberRescan: true);
        }

        private static IAudioTimeSource timeSource;
        public static GameplayCoreSceneSetupData Currentmap { get; private set; }
        private ColorScheme _colorScheme;
        private ColorManager _colorManager;
        private SaberModelManager _saberModelManager;
        private Saber _cachedSaberA;
        private Saber _cachedSaberB;
        private int _cachedSaberScanFrame = int.MinValue;
        private const int SaberRescanIntervalFrames = 48;

        [Inject]
        public void Constructor(IAudioTimeSource source, GameplayCoreSceneSetupData gameplayCoreSceneSetupData, ColorScheme scheme, [InjectOptional] ColorManager colorManager, [InjectOptional] SaberModelManager saberModelManager)
        {
            timeSource = source;
            Currentmap = gameplayCoreSceneSetupData;
            this._colorScheme = scheme;
            this._colorManager = colorManager;
            this._saberModelManager = saberModelManager;
        }
    }
}
