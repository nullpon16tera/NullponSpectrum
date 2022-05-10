using NullponSpectrum.Configuration;
using NullponSpectrum.AudioSpectrums;
using System;
using System.Linq;
using UnityEngine;
using Zenject;
using System.Collections.Generic;

namespace NullponSpectrum.Controllers
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
    public class FanceVisualizerController : IInitializable, IDisposable
    {
        private float scale = 2f;
        private int size = 16;

        private List<GameObject> leftPlane = new List<GameObject>(16);
        private List<GameObject> rightPlane = new List<GameObject>(16);
        private List<Material> _materials = new List<Material>(16);
        private GameObject fanceRoot = new GameObject("fanceVisualizerRoot");

        private void OnUpdatedRawSpectrums(AudioSpectrum obj)
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }
            if (!PluginConfig.Instance.FanceVisualizer)
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


            for (int i = 0; i < _materials.Count; i++)
            {
                var alpha = (this._audioSpectrum.PeakLevels[i] * size) % 1f;
                var alphaLerp = Mathf.Lerp(0f, 1f, alpha * 16f);
                var colorLerp = Mathf.Lerp(0.38f, 1f, alpha);
                var color = Color.HSVToRGB(colorLerp, alphaLerp, alphaLerp);
                _materials[i].SetColor("_Color", color.ColorWithAlpha(0.25f + alpha));
            }

        }


        public void Initialize()
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }

            if (!PluginConfig.Instance.FanceVisualizer)
            {
                return;
            }

            this._audioSpectrum.Band = AudioSpectrum.BandType.TwentySixBand;
            this._audioSpectrum.fallSpeed = 0.3f;
            this._audioSpectrum.sensibility = 10f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;

            // マテリアル生成
            for (int r = 0; r < size; r++)
            {
                Material material = new Material(Shader.Find("Custom/Glowing"));
                material.SetColor("_Color  ", Color.white.ColorWithAlpha(1f));
                material.SetFloat("_EnableColorInstancing", 1f);
                material.SetFloat("_WhiteBoostType", 1f);
                material.SetFloat("_NoiseDithering", 1f);
                _materials.Add(material);
            }

            for (int i = 0; i < size; i++)
            {
                GameObject leftObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Transform leftTransform = leftObj.transform;
                leftTransform.localScale = new Vector3(0.0025f, 0.01f, 0.2f);
                leftTransform.localPosition = new Vector3(-(0.05f + (0.095f * i)), 0.005f, 0f);

                GameObject rightObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Transform rightTransform = rightObj.transform;
                rightTransform.localScale = new Vector3(0.0025f, 0.01f, 0.2f);
                rightTransform.localPosition = new Vector3((0.05f + (0.095f * i)), 0.005f, 0f);

                var leftMeshRenderer = leftObj.GetComponent<MeshRenderer>();
                leftMeshRenderer.material = _materials[i];
                var rightMeshRenderer = rightObj.GetComponent<MeshRenderer>();
                rightMeshRenderer.material = _materials[i];

                leftObj.transform.SetParent(fanceRoot.transform);
                rightObj.transform.SetParent(fanceRoot.transform);
            }

            

            foreach (GameObject obj in leftPlane)
            {
                obj.SetActive(obj);
            }
            foreach (GameObject obj in rightPlane)
            {
                obj.SetActive(obj);
            }

            this.fanceRoot.transform.SetParent(Utilities.VMCAvatarUtil.NullponSpectrumFloor.transform);
        }

        private bool _disposedValue;
        private AudioSpectrum _audioSpectrum;

        [Inject]
        public void Constructor(AudioSpectrum audioSpectrum)
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
