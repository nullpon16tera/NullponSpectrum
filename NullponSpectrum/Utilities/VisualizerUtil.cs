using NullponSpectrum.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace NullponSpectrum.Utilities
{
    class VisualizerUtil : IInitializable, ITickable
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

        private void SaberColor()
        {
            saberAColor = this._colorScheme.saberAColor;
            saberBColor = this._colorScheme.saberBColor;

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

        public void Tick()
        {
            updateTime += Time.deltaTime;
            var bpmSpeed = -(GetBeatsPerMinute() * 0.00001f);
            needUpdate = (s_updateThresholdTime + bpmSpeed) < updateTime;
        }

        public void Initialize()
        {
            SaberColor();
        }

        private static IAudioTimeSource timeSource;
        public static GameplayCoreSceneSetupData Currentmap { get; private set; }
        private ColorScheme _colorScheme;

        [Inject]
        public void Constructor(IAudioTimeSource source, GameplayCoreSceneSetupData gameplayCoreSceneSetupData, ColorScheme scheme)
        {
            timeSource = source;
            Currentmap = gameplayCoreSceneSetupData;
            this._colorScheme = scheme;

        }
    }
}
