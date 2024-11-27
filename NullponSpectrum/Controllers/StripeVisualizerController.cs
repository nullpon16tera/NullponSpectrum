using NullponSpectrum.Configuration;
using NullponSpectrum.AudioSpectrums;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using NullponSpectrum.Utilities;

namespace NullponSpectrum.Controllers
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
    public class StripeVisualizerController : IInitializable, IDisposable
    {
        private int size = 31;

        private Material _stripeMaterial;
        private MaterialPropertyBlock _materialPropertyBlock;
        private int visualizerColorID;

        private List<GameObject> leftPlane = new List<GameObject>(31);
        private List<GameObject> rightPlane = new List<GameObject>(31);

        private GameObject stripeVisualizerRoot;

        public enum FramePosition
        {
            Front,
            Back,
            Left,
            Right,
        };

        private void OnUpdatedRawSpectrums(AudioSpectrum obj)
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }
            if (!PluginConfig.Instance.StripeVisualizer)
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

            for (int i = 0; i < size; i++)
            {
                var peakLevels = this._audioSpectrum.PeakLevels[size - 1 - i];
                
                ChangeMaterialProperty(leftPlane[i], Utilities.VisualizerUtil.GetLeftSaberHSV(), peakLevels);
                ChangeMaterialProperty(rightPlane[i], Utilities.VisualizerUtil.GetRightSaberHSV(), peakLevels);

            }
        }

        private float Nomalize(float f)
        {
            var result = Mathf.Lerp(5f, 1f, f);
            return f * result;
        }

        private void ChangeMaterialProperty(GameObject obj, float[] hsv, float peakLevels)
        {
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();

            if (!PluginConfig.Instance.enableMerihari)
            {
                ChangeMerihari(renderer, hsv, peakLevels);
                return;
            }

            var alphaLerp = Mathf.Lerp(0f, 1f, this.Nomalize(peakLevels * 3f));
            if (0.15f < alphaLerp)
            {
                obj.SetActive(true);
                var color = Color.HSVToRGB(hsv[0], hsv[1], 1f).ColorWithAlpha(0.5f);
                _materialPropertyBlock.SetColor(visualizerColorID, color);
                renderer.SetPropertyBlock(_materialPropertyBlock);
            }
            else
            {
                obj.SetActive(false);
                var color = Color.HSVToRGB(hsv[0], hsv[1], 0f).ColorWithAlpha(0f);
                _materialPropertyBlock.SetColor(visualizerColorID, color);
                renderer.SetPropertyBlock(_materialPropertyBlock);
            }
        }

        private void ChangeMerihari(MeshRenderer renderer, float[] hsv, float peakLevels)
        {
            var colorLerp = Mathf.Lerp(0f, 1f, peakLevels * 5f);
            var color = Color.HSVToRGB(hsv[0], hsv[1], this.Nomalize(colorLerp)).ColorWithAlpha(this.Nomalize(colorLerp));
            _materialPropertyBlock.SetColor(visualizerColorID, color);
            renderer.SetPropertyBlock(_materialPropertyBlock);
        }

        public void Initialize()
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }

            if (!PluginConfig.Instance.StripeVisualizer)
            {
                return;
            }

            this._audioSpectrum.numberOfSamples = 2048;
            this._audioSpectrum.fallSpeed = 1f;
            this._audioSpectrum.sensibility = 0.001f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;

            stripeVisualizerRoot = new GameObject("stripeVisualizerRoot");
            stripeVisualizerRoot.transform.SetParent(FloorViewController.visualizerFloorRoot.transform, false);
            stripeVisualizerRoot.transform.localPosition = new Vector3(0f, 0.0001f, 0f);

            CreateMainObject();
        }

        private void CreateMainObject()
        {
            Shader _shader = VisualizerUtil.GetShader("Custom/Glowing");
            _stripeMaterial = new Material(_shader);
            _stripeMaterial.SetColor("_Color", Color.black.ColorWithAlpha(0f));

            _materialPropertyBlock = new MaterialPropertyBlock();
            visualizerColorID = Shader.PropertyToID("_Color");

            var scale = new Vector3(0.0035f, 0.01f, 0.2f);

            // メインオブジェクト生成
            for (int i = 0; i < size; i++)
            {
                GameObject leftObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                leftObj.transform.SetParent(stripeVisualizerRoot.transform, false);
                Transform leftTransform = leftObj.transform;
                leftTransform.localScale = scale;
                leftTransform.localPosition = new Vector3(-(0.0035f + (0.049f * i)), 0f, 0f);
                var leftMeshRenderer = leftObj.GetComponent<MeshRenderer>();
                leftMeshRenderer.material = _stripeMaterial;
                leftPlane.Add(leftObj);

                GameObject rightObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                rightObj.transform.SetParent(stripeVisualizerRoot.transform, false);
                Transform rightTransform = rightObj.transform;
                rightTransform.localScale = scale;
                rightTransform.localPosition = new Vector3((0.0035f + (0.049f * i)), 0f, 0f);
                var rightMeshRenderer = rightObj.GetComponent<MeshRenderer>();
                rightMeshRenderer.material = _stripeMaterial;
                rightPlane.Add(rightObj);
            }
        }

        private bool _disposedValue;
        private AudioSpectrum _audioSpectrum;

        [Inject]
        public void Constructor([Inject(Id = AudioSpectrum.BandType.ThirtyOneBand)]AudioSpectrum audioSpectrum)
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
