using NullponSpectrum.Configuration;
using NullponSpectrum.AudioSpectrums;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using ModestTree;
using TMPro;
using System.Linq;
using NullponSpectrum.Utilities;

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
        private int visualizerColorID;

        private List<GameObject> cubes = new List<GameObject>(4);

        private GameObject cubeVisualizerRoot;

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
            var needUpdate = Utilities.VisualizerUtil.GetNeedUpdate();
            if (!audio)
            {
                return;
            }

            var alpha = (this._audioSpectrum.PeakLevels[0] * size) % 1f;
            var colorLerp = Mathf.Lerp(0.45f, 1f, alpha);
            var peak = this._audioSpectrum.PeakLevels[0] * scale;
            var cubeSize = 0.2f + peak * 1.3f;
            var bpm = Utilities.VisualizerUtil.GetAudioTimeSource().songTime * (60f / Utilities.VisualizerUtil.GetBeatsPerMinute());
            
            for (int i = 0; i < cubes.Count; i++)
            {
                if (needUpdate)
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

            if (needUpdate)
            {
                Utilities.VisualizerUtil.ResetUpdateTime();
            }
        }

        private void ChangeMaterialProperty(GameObject obj, float h, float size)
        {
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if (0.2001f < size)
            {
                var color = Color.HSVToRGB(h, 1f, 1f).ColorWithAlpha(0.7f);
                _materialPropertyBlock.SetColor(visualizerColorID, color);
                renderer.SetPropertyBlock(_materialPropertyBlock);
            }
            else
            {
                var color = Color.HSVToRGB(h, 1f, 0f).ColorWithAlpha(0f);
                _materialPropertyBlock.SetColor(visualizerColorID, color);
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

            this._audioSpectrum.fallSpeed = 1f;
            this._audioSpectrum.sensibility = 10f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;

            cubeVisualizerRoot = new GameObject("cubeVisualizerRoot");
            cubeVisualizerRoot.transform.SetParent(FloorViewController.visualizerFloorRoot.transform, false);
            cubeVisualizerRoot.transform.localPosition = new Vector3(0f, 0.0001f, 0f);

            // Custom/Glowing Pointer
            // Custom/GlowingInstancedHD
            // Custom/ObstacleCoreLW
            Shader _shader = VisualizerUtil.GetShader("Custom/Glowing");
            _cubeMaterial = new Material(_shader);
            _cubeMaterial.SetColor("_Color", Color.black.ColorWithAlpha(0f));

            _materialPropertyBlock = new MaterialPropertyBlock();
            visualizerColorID = Shader.PropertyToID("_Color");

            for (int i = 0; i < size; i++)
            {
                GameObject child = GameObject.CreatePrimitive(PrimitiveType.Cube);
                child.transform.SetParent(cubeVisualizerRoot.transform, false);

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
        private AudioSpectrum _audioSpectrum;

        [Inject]
        public void Constructor([Inject(Id = AudioSpectrum.BandType.FourBand)]AudioSpectrum audioSpectrum)
        {
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
