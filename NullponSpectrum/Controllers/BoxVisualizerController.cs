using NullponSpectrum.Configuration;
using NullponSpectrum.AudioSpectrums;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace NullponSpectrum.Controllers
{
    class BoxVisualizerController : IInitializable, IDisposable
    {
        private Material _material;
        private MaterialPropertyBlock _materialPropertyBlock;
        private int visualizerColorID;

        private List<GameObject> panelsLeft = new List<GameObject>(30);
        private List<GameObject> panelsRight = new List<GameObject>(30);

        private GameObject boxVisualizerRoot;


        private float[] s_shift = new float[30];

        private void OnUpdatedRawSpectrums(AudioSpectrum obj)
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }
            if (!PluginConfig.Instance.BoxVisualizer)
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


            float tmp = Mathf.Lerp(0f, 1f, this._audioSpectrum.PeakLevels[0] * 5f);
            for (int i = 0; i < 30; i++)
            {
                int index = 29 - i;
                if (needUpdate)
                {
                    try
                    {
                        if (index > 0)
                        {
                            this.s_shift[index] = this.s_shift[index - 1];
                        }
                        else
                        {
                            this.s_shift[index] = tmp;
                        }
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.Debug(e);
                    }
                }
                ChangeMaterialProperty(panelsLeft[i], this.Nomalize(this.s_shift[index]));
                ChangeMaterialProperty(panelsRight[i], this.Nomalize(this.s_shift[index]));
            }

            if (needUpdate)
            {
                Utilities.VisualizerUtil.ResetUpdateTime();
            }
        }

        private float Nomalize(float f)
        {
            var result = Mathf.Lerp(5f, 1f, f);
            return f * result;
        }

        private void ChangeMaterialProperty(GameObject obj, float h)
        {
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if (0.1f < h)
            {
                var color = Color.HSVToRGB(h, h, h).ColorWithAlpha(h);
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

            if (!PluginConfig.Instance.BoxVisualizer)
            {
                return;
            }

            this._audioSpectrum.fallSpeed = 1f;
            this._audioSpectrum.sensibility = 10f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;

            boxVisualizerRoot = new GameObject("boxVisualizerRoot");
            boxVisualizerRoot.transform.SetParent(FloorViewController.visualizerFloorRoot.transform, false);
            boxVisualizerRoot.transform.localPosition = new Vector3(0f, 0f, 0f);

            // Custom/Glowing Pointer
            // Custom/GlowingInstancedHD
            // Custom/ObstacleCoreLW
            _material = new Material(Shader.Find("Custom/UnlitSpectrogram"));
            _material.SetColor("_Color", Color.red.ColorWithAlpha(0.6f));

            _materialPropertyBlock = new MaterialPropertyBlock();
            visualizerColorID = Shader.PropertyToID("_Color");

            GameObject boxLeft = new GameObject("boxLeft");
            boxLeft.transform.SetParent(boxVisualizerRoot.transform, false);
            boxLeft.transform.localPosition = new Vector3(-2.975f, -25.75f, 0f);
            boxLeft.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            GameObject boxRight = new GameObject("boxRight");
            boxRight.transform.SetParent(boxVisualizerRoot.transform, false);
            boxRight.transform.localPosition = new Vector3(2.975f, -25.75f, 0f);
            boxRight.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);

            for (int i = 0; i < 30; i++)
            {
                GameObject boxObjectLeft = GameObject.CreatePrimitive(PrimitiveType.Quad);
                boxObjectLeft.transform.SetParent(boxLeft.transform, false);
                boxObjectLeft.transform.localScale = new Vector3(0.5f + (i * 0.05f), 50f + (i * 0.05f), 1f);
                boxObjectLeft.transform.localPosition = new Vector3(0f, 0f, 0f + (i * 0.05f));
                MeshRenderer boxLeftMeshRenderer = boxObjectLeft.GetComponent<MeshRenderer>();
                boxLeftMeshRenderer.material = _material;
                panelsLeft.Add(boxObjectLeft);

                GameObject boxObjectRight = GameObject.CreatePrimitive(PrimitiveType.Quad);
                boxObjectRight.transform.SetParent(boxRight.transform, false);
                boxObjectRight.transform.localScale = new Vector3(0.5f + (i * 0.05f), 50f + (i * 0.05f), 1f);
                boxObjectRight.transform.localPosition = new Vector3(0f, 0f, 0f + (i * 0.05f));
                MeshRenderer boxRightMeshRenderer = boxObjectRight.GetComponent<MeshRenderer>();
                boxRightMeshRenderer.material = _material;
                panelsRight.Add(boxObjectRight);
            }
        }

        private bool _disposedValue;
        private AudioSpectrum _audioSpectrum;

        [Inject]
        public void Constructor([Inject(Id = AudioSpectrum.BandType.EightBand)] AudioSpectrum audioSpectrum)
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
