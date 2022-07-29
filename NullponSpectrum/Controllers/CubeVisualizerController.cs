﻿using NullponSpectrum.Configuration;
using NullponSpectrum.AudioSpectrums;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace NullponSpectrum.Controllers
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
    public class CubeVisualizerController : IInitializable, IDisposable
    {
        private float scale = 2f;
        private int size = 4;

        private List<GameObject> cubes = new List<GameObject>(4);
        private Material cubeMaterial;
        private GameObject cubeRoot = new GameObject("cubeVisualizerRoot");

        private void OnUpdatedRawSpectrums(AudioSpectrum4 obj)
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }
            if (!PluginConfig.Instance.CubeVisualizer)
            {
                return;
            }
            this.UpdateAudioSpectrums(obj);
        }

        private void UpdateAudioSpectrums(AudioSpectrum4 audio)
        {
            if (!audio)
            {
                return;
            }

            var alpha = (this._audioSpectrum.PeakLevels[0] * size) % 1f;
            var alphaLerp = Mathf.Lerp(0f, 1f, alpha * 30f);
            var colorLerp = Mathf.Lerp(0.45f, 1f, alpha);
            var peak = this._audioSpectrum.PeakLevels[0] * scale;
            var cubeSize = 0.2f + peak * 1.3f;
            var bpm = _timeSource.songTime * (60f / this.Currentmap.level.beatsPerMinute);
            
            for (int i = 0; i < cubes.Count; i++)
            {
                var cube = cubes[i];
                var rotate = bpm * 360f + 45f;
                var rotateRog = (i == 0 || i == 2 ? rotate : -(rotate));
                var cubePosition = cube.transform.localPosition;
                cubePosition.y = cubeSize;
                cube.transform.localScale = new Vector3(cubeSize, cubeSize, cubeSize);
                cube.transform.localRotation = Quaternion.Euler(rotateRog, rotateRog, rotateRog);
                cube.transform.localPosition = cubePosition;

                var color = Color.HSVToRGB(colorLerp, 1f, Lighting(alpha, 1f));
                //meshRenderers[i].material.SetColor("_Color", Color.HSVToRGB(amp, 1f, peakAmp));
                /*meshRenderers[i].material.SetColor("_Color", Color.HSVToRGB(0f, 1f, 0f));
                meshRenderers[i].material.SetColor("_AddColor", Color.HSVToRGB(amp, 1f, 1f));
                meshRenderers[i].material.SetFloat("_TintColorAlpha", alpha);*/
                cubeMaterial.SetColor("_Color", color.ColorWithAlpha(Lighting(alpha, 0.6f)));
                
            }

        }

        private float Lighting(float alpha, float withAlpha)
        {
            return 0.15f < alpha ? withAlpha : 0f;
        }

        public void Initialize()
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }

            if (!PluginConfig.Instance.CubeVisualizer)
            {
                return;
            }

            this._audioSpectrum.Band = AudioSpectrum4.BandType.FourBand;
            this._audioSpectrum.fallSpeed = 1f;
            this._audioSpectrum.sensibility = 10f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;

            // Custom/Glowing Pointer
            // Custom/GlowingInstancedHD
            // Custom/ObstacleCoreLW
            cubeMaterial = new Material(Shader.Find("Custom/Glowing"));
            cubeMaterial.SetColor("_Color", new Color(1f, 1f, 1f).ColorWithAlpha(1f));
            cubeMaterial.SetFloat("_EnableColorInstancing", 1f);
            cubeMaterial.SetFloat("_WhiteBoostType", 1f);
            cubeMaterial.SetFloat("_NoiseDithering", 1f);
            //childMeshRenderer.material.SetColor("_Color", Color.HSVToRGB(1f, 1f, 1f));
            //childMeshRenderer.material.SetColor("_AddColor", Color.HSVToRGB(amp, 1f, 1f));
            //childMeshRenderer.material.SetFloat("_TintColorAlpha", 0f);

            for (int i = 0; i < size; i++)
            {
                GameObject child = GameObject.CreatePrimitive(PrimitiveType.Cube);
                MeshRenderer childMeshRenderer = child.GetComponent<MeshRenderer>();
                childMeshRenderer.material = cubeMaterial;
                
                Transform cubeTransform = child.transform;
                cubeTransform.localPosition = new Vector3(0f, 0.3f, 0f);
                cubeTransform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                cubeTransform.localRotation = Quaternion.Euler(45f, 45f, 45f);

                switch (i)
                {
                    case 0:
                        cubeTransform.localPosition = new Vector3(-1.5f, 0.3f, 1f);
                        break;
                    case 1:
                        cubeTransform.localPosition = new Vector3(1.5f, 0.3f, 1f);
                        break;
                    case 2:
                        cubeTransform.localPosition = new Vector3(-1.5f, 0.3f, -1f);
                        break;
                    case 3:
                        cubeTransform.localPosition = new Vector3(1.5f, 0.3f, -1f);
                        break;
                    default:
                        break;
                }
                child.transform.SetParent(cubeRoot.transform);

                cubes.Add(child);
            }

            if (PluginConfig.Instance.isFloorHeight)
            {
                var rootPosition = cubeRoot.transform.localPosition;
                rootPosition.y = PluginConfig.Instance.floorHeight * 0.01f;
                cubeRoot.transform.localPosition = rootPosition;
            }

            this.cubeRoot.transform.SetParent(Utilities.FloorAdjustorUtil.NullponSpectrumFloor.transform);
        }

        private bool _disposedValue;
        private IAudioTimeSource _timeSource;
        public IDifficultyBeatmap Currentmap { get; private set; }
        private AudioSpectrum4 _audioSpectrum;

        [Inject]
        public void Constructor(IAudioTimeSource source, IDifficultyBeatmap level, AudioSpectrum4 audioSpectrum)
        {
            this._timeSource = source;
            this.Currentmap = level;
            this._audioSpectrum = audioSpectrum;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposedValue)
            {
                if (disposing)
                {
                    this._audioSpectrum.UpdatedRawSpectrums -= this.OnUpdatedRawSpectrums;
                }
                this._disposedValue = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
