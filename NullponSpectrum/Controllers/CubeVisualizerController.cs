using NullponSpectrum.AudioSpectrums;
using System;
using UnityEngine;
using Zenject;
using System.Collections.Generic;
using NullponSpectrum.Configuration;

namespace NullponSpectrum.Controllers
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
    public class CubeVisualizerController : IInitializable, IDisposable
    {
        private float scale = 2f;
        private bool _disposedValue;
        private int size = 4;

        private List<GameObject> cubes = new List<GameObject>(4);
        private Material cubeMaterial;
        private GameObject cubeRoot = new GameObject("cubeVisualizerRoot");

        private void OnUpdatedRawSpectrums(AudioSpectrum obj)
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

        private void UpdateAudioSpectrums(AudioSpectrum audio)
        {
            if (!audio)
            {
                return;
            }

            var alpha = (this._audioSpectrum.PeakLevels[0] * size) % 1f;
            var alphaLerp = Mathf.Lerp(0f, 1f, alpha * 30f);
            var colorLerp = Mathf.Lerp(0.38f, 1f, alpha + alpha);
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
                var color = Color.HSVToRGB(colorLerp, alphaLerp, alphaLerp);

                //meshRenderers[i].material.SetColor("_Color", Color.HSVToRGB(amp, 1f, peakAmp));
                /*meshRenderers[i].material.SetColor("_Color", Color.HSVToRGB(0f, 1f, 0f));
                meshRenderers[i].material.SetColor("_AddColor", Color.HSVToRGB(amp, 1f, 1f));
                meshRenderers[i].material.SetFloat("_TintColorAlpha", alpha);*/
                cubeMaterial.SetColor("_Color", color.ColorWithAlpha(0.25f + alpha));
                
            }

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

            // Custom/Glowing Pointer
            // Custom/GlowingInstancedHD
            // Custom/ObstacleCoreLW
            cubeMaterial = new Material(Shader.Find("Custom/Glowing"));
            cubeMaterial.SetColor("_Color  ", new Color(1f, 1f, 1f).ColorWithAlpha(1f));
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

            foreach (GameObject obj in cubes)
            {
                obj.SetActive(obj);
            }
        }

        private IAudioTimeSource _timeSource;
        public IDifficultyBeatmap Currentmap { get; private set; }
        private AudioSpectrum _audioSpectrum;

        [Inject]
        public void Constructor(IAudioTimeSource source, IDifficultyBeatmap level, AudioSpectrum audioSpectrum)
        {
            this._timeSource = source;
            this.Currentmap = level;
            this._audioSpectrum = audioSpectrum;
            this._audioSpectrum.Band = AudioSpectrum.BandType.FourBand;
            this._audioSpectrum.fallSpeed = 0.3f;
            this._audioSpectrum.sensibility = 10f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;
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
