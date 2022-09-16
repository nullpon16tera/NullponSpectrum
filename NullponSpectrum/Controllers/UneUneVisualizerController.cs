using NullponSpectrum.Configuration;
using NullponSpectrum.AudioSpectrums;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using System.Linq;

namespace NullponSpectrum.Controllers
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
    public class UneUneVisualizerController : IInitializable, IDisposable
    {
        private int size = 31;

        private Material _uneuneMaterial;
        private MaterialPropertyBlock _materialPropertyBlock;
        private int visualizerTintColorID;
        private int visualizerBrightnessID;

        private List<GameObject> uneuneLeftObjects = new List<GameObject>(31);
        private List<GameObject> uneuneRightObjects = new List<GameObject>(31);

        private GameObject uneuneRoot = new GameObject("uneuneVisualizerRoot");

        private int choice = 6;

        private float[] s_shift = new float[31];


        private void OnUpdatedRawSpectrums(AudioSpectrum obj)
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }
            if (!PluginConfig.Instance.UneUneVisualizer)
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

            float tmp = this._audioSpectrum.PeakLevels[choice] * 50f;

            for (int i = 0; i < size; i++)
            {
                float timeSize = Utilities.VisualizerUtil.GetAudioTimeSource().songTime + (float)(i + 1) / size * Mathf.PI;
                float amplitude = Mathf.Cos(timeSize) * 3f + (i * 0.05f);
                var alpha = this._audioSpectrum.PeakLevels[choice] * 10f % 1f;
                int index = 30 - i;
                if (needUpdate) {
                    try {
                        if (index > 0) {
                            this.s_shift[index] = this.s_shift[index - 1];
                        }
                        else {
                            this.s_shift[index] = tmp;
                        }
                    }
                    catch (Exception e) {
                        Plugin.Log.Debug(e);
                    }
                }

                UneUne(uneuneLeftObjects[i], Utilities.VisualizerUtil.GetLeftSaberHSV(), index, alpha, amplitude);
                UneUne(uneuneRightObjects[i], Utilities.VisualizerUtil.GetRightSaberHSV(), index, alpha, amplitude);
            }

            if (needUpdate) {
                Utilities.VisualizerUtil.ResetUpdateTime();
            }
        }

        private void UneUne(GameObject obj,  float[] hsv, int index, float alpha, float amplitude)
        {
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            obj.transform.localScale = new Vector3(0.5f + alpha, 0.2f + this.s_shift[index], 0.5f);
            if (0.5f < obj.transform.localScale.y)
            {
                var color = Color.HSVToRGB(hsv[0], hsv[1], 1f).ColorWithAlpha(0.9f);
                _materialPropertyBlock.SetColor(visualizerTintColorID, color);
                _materialPropertyBlock.SetFloat(visualizerBrightnessID, 1f);
                renderer.SetPropertyBlock(_materialPropertyBlock);
            }
            else
            {
                var color = Color.HSVToRGB(hsv[0], hsv[1], 0f).ColorWithAlpha(0f);
                _materialPropertyBlock.SetColor(visualizerTintColorID, color);
                _materialPropertyBlock.SetFloat(visualizerBrightnessID, 0f);
                renderer.SetPropertyBlock(_materialPropertyBlock);
            }

            var position = obj.transform.localPosition;
            obj.transform.localPosition = new Vector3(position.x, amplitude, position.z);
        }

        public void Initialize()
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }
            if (!PluginConfig.Instance.UneUneVisualizer)
            {
                return;
            }

            this._audioSpectrum.fallSpeed = 1f;
            this._audioSpectrum.sensibility = 10f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;

            choice = PluginConfig.Instance.listChoice;


            // Custom/Glowing Pointer
            // Custom/GlowingInstancedHD
            // Custom/ObstacleCoreLW

            GameObject leftUneUne = new GameObject("leftUneUne");
            GameObject rightUneUne = new GameObject("rightUneUne");

            _uneuneMaterial = new Material(Shader.Find("Custom/SaberBlade"));
            _uneuneMaterial.SetColor("_TintColor", Color.black.ColorWithAlpha(1f));
            _uneuneMaterial.SetFloat("_Brightness", 0f);

            _materialPropertyBlock = new MaterialPropertyBlock();
            visualizerTintColorID = Shader.PropertyToID("_TintColor");
            visualizerBrightnessID = Shader.PropertyToID("_Brightness");

            var scale = new Vector3(0.5f, 0.2f, 0.5f);

            for (int i = 0; i < size; i++)
            {
                GameObject objLeft = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                MeshRenderer leftMeshRenderer = objLeft.GetComponent<MeshRenderer>();
                leftMeshRenderer.material = _uneuneMaterial;
                objLeft.transform.SetParent(leftUneUne.transform, false);
                objLeft.transform.localScale = scale;
                objLeft.transform.localPosition = new Vector3(-1.5f - (i * 0.25f), 0f, (size - i) * 2.5f + 1.2f);
                objLeft.transform.localRotation = Quaternion.Euler(0f, 0f, 25f + (i * 0.6f + 0.5f));
                uneuneLeftObjects.Add(objLeft);

                GameObject objRight = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                MeshRenderer rightMeshRenderer = objRight.GetComponent<MeshRenderer>();
                rightMeshRenderer.material = _uneuneMaterial;
                objRight.transform.SetParent(rightUneUne.transform, false);
                objRight.transform.localScale = scale;
                objRight.transform.localPosition = new Vector3(1.5f + (i * 0.25f), 0f, (size - i) * 2.5f + 1.2f);
                objRight.transform.localRotation = Quaternion.Euler(0f, 0f, -25f - (i * 0.6f + 0.5f));
                uneuneRightObjects.Add(objRight);
            }

            

            leftUneUne.transform.SetParent(uneuneRoot.transform);
            leftUneUne.transform.position = new Vector3(-5f, 2.5f, 7f);
            //leftUneUne.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            rightUneUne.transform.SetParent(uneuneRoot.transform);
            rightUneUne.transform.position = new Vector3(5f, 2.5f, 7f);
            //rightUneUne.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
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
