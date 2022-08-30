using NullponSpectrum.Configuration;
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

        private Material _cubeMaterial;
        private MaterialPropertyBlock _materialPropertyBlock;
        private int visualizerTintColorID;
        private int visualizerBrightnessID;

        private List<GameObject> cubes = new List<GameObject>(4);

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

                ChangeMaterialProperty(cube, colorLerp, cubeSize);
            }

        }

        private void ChangeMaterialProperty(GameObject obj, float h, float size)
        {
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if (0.21f < size)
            {
                var color = Color.HSVToRGB(h, 1f, 1f).ColorWithAlpha(0.8f);
                _materialPropertyBlock.SetColor(visualizerTintColorID, color);
                _materialPropertyBlock.SetFloat(visualizerBrightnessID, 1f);
                renderer.SetPropertyBlock(_materialPropertyBlock);
            }
            else
            {
                var color = Color.HSVToRGB(h, 1f, 0f).ColorWithAlpha(0f);
                _materialPropertyBlock.SetColor(visualizerTintColorID, color);
                _materialPropertyBlock.SetFloat(visualizerBrightnessID, 0f);
                renderer.SetPropertyBlock(_materialPropertyBlock);
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

            this._audioSpectrum.Band = AudioSpectrum4.BandType.FourBand;
            this._audioSpectrum.fallSpeed = 1f;
            this._audioSpectrum.sensibility = 10f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;

            // Custom/Glowing Pointer
            // Custom/GlowingInstancedHD
            // Custom/ObstacleCoreLW
            _cubeMaterial = new Material(Shader.Find("Custom/SaberBlade"));
            _cubeMaterial.SetColor("_TintColor", Color.black.ColorWithAlpha(1f));
            _cubeMaterial.SetFloat("_Brightness", 0f);

            _materialPropertyBlock = new MaterialPropertyBlock();
            visualizerTintColorID = Shader.PropertyToID("_TintColor");
            visualizerBrightnessID = Shader.PropertyToID("_Brightness");

            for (int i = 0; i < size; i++)
            {
                GameObject child = GameObject.CreatePrimitive(PrimitiveType.Cube);
                child.transform.SetParent(FloorViewController.visualizerFloorRoot.transform);

                MeshRenderer childMeshRenderer = child.GetComponent<MeshRenderer>();
                childMeshRenderer.material = _cubeMaterial;
                
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

                cubes.Add(child);
            }
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
