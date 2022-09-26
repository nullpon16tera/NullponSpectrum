using NullponSpectrum.Configuration;
using NullponSpectrum.AudioSpectrums;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using SiraUtil.Sabers;
using System.Linq;

namespace NullponSpectrum.Controllers
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
    public class RainbowBugVisualizerController : IInitializable, IDisposable
    {
        private int size = 28;

        private Material _material;
        private MaterialPropertyBlock _materialPropertyBlock;
        private int visualizerColorID;
        private int visualizerScaleID;

        private List<GameObject> leftObject = new List<GameObject>(28);
        private List<GameObject> rightObject = new List<GameObject>(28);

        private GameObject leftFloorRoot = new GameObject("rogLeftFloorRoot");
        private GameObject rightFloorRoot = new GameObject("rogRightFloorRoot");


        private float[] s_shift = new float[28];

        private float Nomalize(float f)
        {
            var result = Mathf.Lerp(6f, 1f, f);
            return f * result;
        }

        private void OnUpdatedRawSpectrums(AudioSpectrum obj)
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }
            if (!PluginConfig.Instance.RainbowBugVisualizer)
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

            for (int i = 0; i < size; i++)
            {
                var alpha = this._audioSpectrum.PeakLevels[4] * 10f % 1f;
                int index = 27 - i;
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
                            this.s_shift[index] = alpha;
                        }
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.Debug(e);
                    }
                }
                float tmp = Mathf.Lerp(0f, 1.5f, this._audioSpectrum.PeakLevels[27 - i] * 6f);

                ChangeMaterialProperty(leftObject[i], this.s_shift, index, 0.002f, this.Nomalize(tmp));
                ChangeMaterialProperty(rightObject[i], this.s_shift, index, 0.002f, this.Nomalize(tmp));
            }

            if (needUpdate)
            {
                Utilities.VisualizerUtil.ResetUpdateTime();
            }
        }

        private void ChangeMaterialProperty(GameObject obj, float[] h, int index, float min, float tmp)
        {
            var scale = obj.transform.localScale;
            obj.transform.localScale = new Vector3(h[index] / 2, tmp, h[index] / 2);

            var child = obj.transform.GetChild(0).gameObject;
            if (child != null)
            {
                MeshRenderer renderer = child.GetComponent<MeshRenderer>();
                if (min < tmp)
                {
                    obj.SetActive(true);
                    var color = Color.HSVToRGB(h[index], 1f, 1f).ColorWithAlpha(0.3f);
                    _materialPropertyBlock.SetColor(visualizerColorID, color);
                    renderer.SetPropertyBlock(_materialPropertyBlock);
                }
                else
                {
                    obj.SetActive(false);
                    var color = Color.HSVToRGB(h[index], 1f, 0f).ColorWithAlpha(0f);
                    _materialPropertyBlock.SetColor(visualizerColorID, color);
                    renderer.SetPropertyBlock(_materialPropertyBlock);
                }
            }
        }

        public void Initialize()
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }
            if (!PluginConfig.Instance.RainbowBugVisualizer)
            {
                return;
            }

            this._audioSpectrum.fallSpeed = 1f;
            this._audioSpectrum.sensibility = 10f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;


            // Custom/Glowing Pointer
            // Custom/GlowingInstancedHD
            // Custom/ObstacleCoreLW

            CreateMainObject();
        }

        private void CreateMainObject()
        {
            _material = new Material(Shader.Find("Custom/UnlitSpectrogram"));
            _material.SetColor("_Color", Color.red.ColorWithAlpha(0f));

            _materialPropertyBlock = new MaterialPropertyBlock();
            visualizerColorID = Shader.PropertyToID("_Color");

            leftFloorRoot.transform.SetParent(FloorViewController.visualizerFloorRoot.transform, false);
            leftFloorRoot.transform.localScale = Vector3.one;
            leftFloorRoot.transform.localPosition = new Vector3(-1.5f, 0.0001f, 0f);
            leftFloorRoot.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            rightFloorRoot.transform.SetParent(FloorViewController.visualizerFloorRoot.transform, false);
            rightFloorRoot.transform.localScale = Vector3.one;
            rightFloorRoot.transform.localPosition = new Vector3(1.5f, 0.0001f, 0f);
            rightFloorRoot.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);


            for (int i = 0; i < size; i++)
            {
                GameObject parentObjectLeft = new GameObject("rogParentObjectLeft");
                parentObjectLeft.transform.SetParent(leftFloorRoot.transform, false);
                GameObject childObjectLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
                childObjectLeft.transform.SetParent(parentObjectLeft.transform, false);

                parentObjectLeft.transform.localScale = new Vector3(0.05f, 1f, 0.0001f);
                parentObjectLeft.transform.localPosition = new Vector3(-0.975f + (i * 0.0725f), 0f, 0f);
                parentObjectLeft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                childObjectLeft.transform.localPosition = new Vector3(0f, 0.5f, -0.5f);
                childObjectLeft.transform.localScale = Vector3.one;
                MeshRenderer childMeshRenderer = childObjectLeft.GetComponent<MeshRenderer>();
                childMeshRenderer.material = _material;
                leftObject.Add(parentObjectLeft);


                GameObject parentObjectRight = new GameObject("rogParentObjectRight");
                parentObjectRight.transform.SetParent(rightFloorRoot.transform, false);
                GameObject childObjectRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
                childObjectRight.transform.SetParent(parentObjectRight.transform, false);

                parentObjectRight.transform.localScale = new Vector3(0.05f, 1f, 0.0001f);
                parentObjectRight.transform.localPosition = new Vector3(0.975f - (i * 0.0725f), 0f, 0f);
                parentObjectRight.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);


                childObjectRight.transform.localPosition = new Vector3(0f, 0.5f, -0.5f);
                childObjectRight.transform.localScale = Vector3.one;
                MeshRenderer childRightMeshRenderer = childObjectRight.GetComponent<MeshRenderer>();
                childRightMeshRenderer.material = _material;
                rightObject.Add(parentObjectRight);
            }
        }

        private bool _disposedValue;
        private AudioSpectrum _audioSpectrum;

        [Inject]
        public void Constructor([Inject(Id = AudioSpectrum.BandType.ThirtyOneBand)] AudioSpectrum audioSpectrum)
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
