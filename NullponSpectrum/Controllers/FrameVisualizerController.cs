using NullponSpectrum.Configuration;
using NullponSpectrum.AudioSpectrums;
using NullponSpectrum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace NullponSpectrum.Controllers
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
    public class FrameVisualizerController : IInitializable, IDisposable
    {
        private float scale = 2f;
        private int size = 4;

        private Material _frameMaterial;
        private MaterialPropertyBlock _materialPropertyBlock;
        private int visualizerTintColorID;
        private int visualizerBrightnessID;

        private List<GameObject> cubes = new List<GameObject>(4);
        private GameObject frameRoot = new GameObject("frameVisualizerRoot");

        public enum FramePosition {
            Front,
            Back,
            Left,
            Right,
        };

        private void OnUpdatedRawSpectrums(AudioSpectrum4 obj)
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }
            if (!PluginConfig.Instance.FrameVisualizer)
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

            for (int i = 0; i < cubes.Count; i++)
            {
                
                var peak = this._audioSpectrum.PeakLevels[i] * scale;
                var frameSize = 0.25f + ((size - i) * 0.2f);
                var peakSize = frameSize + peak;
                cubes[i].transform.localScale = new Vector3(peakSize, 1f, peakSize);

                var alpha = (this._audioSpectrum.PeakLevels[i] * size) % 1f;
                var colorLerp = Mathf.Lerp(0.45f, 1f, alpha);

                for (int r = 0; r < cubes[i].transform.childCount; r++)
                {
                    var childObj = cubes[i].transform.GetChild(r).gameObject;
                    ChangeMaterialProperty(childObj, colorLerp, frameSize, peakSize);
                }
            }

        }

        private void ChangeMaterialProperty(GameObject obj, float h, float frameSize, float peakSize)
        {
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if ((frameSize + 0.025f) < peakSize)
            {
                var color = Color.HSVToRGB(h, 1f, 1f).ColorWithAlpha(1f);
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

            if (!PluginConfig.Instance.FrameVisualizer)
            {
                return;
            }

            this._audioSpectrum.Band = AudioSpectrum4.BandType.FourBand;
            this._audioSpectrum.fallSpeed = 1f;
            this._audioSpectrum.sensibility = 10f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;

            _frameMaterial = new Material(Shader.Find("Custom/SaberBlade"));
            _frameMaterial.SetColor("_TintColor", Color.black.ColorWithAlpha(1f));
            _frameMaterial.SetFloat("_Brightness", 0f);

            _materialPropertyBlock = new MaterialPropertyBlock();
            visualizerTintColorID = Shader.PropertyToID("_TintColor");
            visualizerBrightnessID = Shader.PropertyToID("_Brightness");

            GameObject parent = new GameObject("framePlaySpace");

            for (int i = 0; i < size; i++)
            {
                GameObject child = GameObject.CreatePrimitive(PrimitiveType.Cube);

                Transform cubeTransform = child.transform;
                if (i == (int)FramePosition.Front || i == (int)FramePosition.Back)
                {
                    cubeTransform.localScale = new Vector3(3.015f, 0.015f, 0.015f);
                }
                if (i == (int)FramePosition.Left || i == (int)FramePosition.Right)
                {
                    cubeTransform.localScale = new Vector3(0.015f, 0.015f, 2.015f);
                }
                switch (i)
                {
                    case (int)FramePosition.Front:
                        cubeTransform.localPosition = new Vector3(0f, 0.005f, 1f);
                        break;
                    case (int)FramePosition.Back:
                        cubeTransform.localPosition = new Vector3(0f, 0.005f, -1f);
                        break;
                    case (int)FramePosition.Left:
                        cubeTransform.localPosition = new Vector3(-1.5f, 0.005f, 0f);
                        break;
                    case (int)FramePosition.Right:
                        cubeTransform.localPosition = new Vector3(1.5f, 0.005f, 0f);
                        break;
                    default:
                        break;
                }
                child.transform.SetParent(parent.transform);
                parent.transform.SetParent(frameRoot.transform);
            }

            for (int j = 0; j < size; j++)
            {
                var clone = Clone(parent);
                for (int r = 0; r < clone.transform.childCount; r++)
                {
                    var childObj = clone.transform.GetChild(r).gameObject;
                    var meshRenderer = childObj.GetComponent<MeshRenderer>();
                    meshRenderer.material = _frameMaterial;
                }
                cubes.Add(clone);
            }

            if (PluginConfig.Instance.isFloorHeight)
            {
                var rootPosition = frameRoot.transform.localPosition;
                rootPosition.y = PluginConfig.Instance.floorHeight * 0.01f;
                frameRoot.transform.localPosition = rootPosition;
            }

            this.frameRoot.transform.SetParent(FloorAdjustorUtil.NullponSpectrumFloor.transform);
        }

        public GameObject Clone(GameObject go)
        {
            var clone = GameObject.Instantiate(go) as GameObject;
            clone.transform.parent = go.transform.parent;
            clone.transform.localPosition = go.transform.localPosition;
            clone.transform.localScale = go.transform.localScale;
            return clone;
        }

        private bool _disposedValue;
        private AudioSpectrum4 _audioSpectrum;

        [Inject]
        public void Constructor(AudioSpectrum4 audioSpectrum)
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
