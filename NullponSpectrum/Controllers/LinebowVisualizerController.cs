using NullponSpectrum.Configuration;
using NullponSpectrum.AudioSpectrums;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using System.Linq;
using NullponSpectrum.Utilities;

namespace NullponSpectrum.Controllers
{
    class LinebowVisualizerController : IInitializable, IDisposable
    {
        private int size = 31;

        private Material _material;
        private MaterialPropertyBlock _materialPropertyBlock;
        private int visualizerColorID;

        private List<GameObject> leftObject = new List<GameObject>(31);
        private List<GameObject> rightObject = new List<GameObject>(31);

        private GameObject leftFloorRoot = new GameObject("linebowLeftFloorRoot");
        private GameObject rightFloorRoot = new GameObject("linebowRightFloorRoot");

        private float[] s_shift = new float[31];
        private float[] s_shift2 = new float[31];
        private float[] s_shift3 = new float[31];

        private void OnUpdatedRawSpectrums(AudioSpectrum obj)
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }
            if (!PluginConfig.Instance.LinebowVisualizer)
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

            float tmp = Mathf.Lerp(0f, 90f, this._audioSpectrum.PeakLevels[0] * 5f);
            float tmp2 = Mathf.Lerp(0f, 75f, this._audioSpectrum.PeakLevels[0] * 5f);
            float alpha = this._audioSpectrum.PeakLevels[0] * 10f % 1f;

            for (int i = 0; i < leftObject.Count; i++)
            {
                int index = 30 - i;
                if (needUpdate)
                {
                    try
                    {
                        if (index > 0)
                        {
                            this.s_shift[index] = this.s_shift[index - 1];
                            this.s_shift2[index] = this.s_shift2[index - 1];
                            this.s_shift3[index] = this.s_shift3[index - 1];
                        }
                        else
                        {
                            this.s_shift[index] = tmp;
                            this.s_shift2[index] = tmp2;
                            this.s_shift3[index] = alpha;
                        }
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.Debug(e);
                    }
                }
                ChangeMaterialProperty(leftObject[i], index, true);
                ChangeMaterialProperty(rightObject[i], index, false);
            }

            if (needUpdate)
            {
                Utilities.VisualizerUtil.ResetUpdateTime();
            }
        }

        private void ChangeMaterialProperty(GameObject obj, int index, bool left)
        {
            var child = obj.transform.GetChild(0).gameObject;
            if (child != null)
            {
                MeshRenderer renderer = child.GetComponent<MeshRenderer>();
                if (1f < this.s_shift[index])
                {
                    obj.transform.localRotation = Quaternion.Euler(-this.s_shift[index], (left ? -this.s_shift2[index] : this.s_shift2[index]), 0f);

                    var color = Color.HSVToRGB(this.s_shift3[index], 1, 1f).ColorWithAlpha(0.7f);
                    _materialPropertyBlock.SetColor(visualizerColorID, color);
                    renderer.SetPropertyBlock(_materialPropertyBlock);
                }
                else
                {
                    obj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                    var color = Color.HSVToRGB(0f, 0f, 0f).ColorWithAlpha(0f);
                    _materialPropertyBlock.SetColor(visualizerColorID, color);
                    renderer.SetPropertyBlock(_materialPropertyBlock);
                }
            }
        }

        private float Nomalize(float f)
        {
            var result = Mathf.Lerp(6f, 1f, f);
            return f * result;
        }

        public void Initialize()
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }
            if (!PluginConfig.Instance.LinebowVisualizer)
            {
                return;
            }

            this._audioSpectrum.fallSpeed = 1f;
            this._audioSpectrum.sensibility = 10f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;


            // Custom/Glowing Pointer
            // Custom/GlowingInstancedHD
            // Custom/ObstacleCoreLW

            _material = new Material(Shader.Find("Custom/Glowing"));
            _material.SetColor("_Color", Color.black.ColorWithAlpha(1f));

            _materialPropertyBlock = new MaterialPropertyBlock();
            visualizerColorID = Shader.PropertyToID("_Color");


            leftFloorRoot.transform.SetParent(FloorViewController.visualizerFloorRoot.transform, false);
            leftFloorRoot.transform.localScale = Vector3.one;
            leftFloorRoot.transform.localPosition = new Vector3(-2.3f, -0.05f, 44f);

            GameObject leftFloorCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftFloorCube.transform.SetParent(leftFloorRoot.transform, false);
            leftFloorCube.transform.localScale = new Vector3(0.115f, 0.11f, 50f);
            leftFloorCube.transform.localPosition = new Vector3(0f, 0f, -11.15f);
            MeshRenderer leftFloorCubeRenderer = leftFloorCube.GetComponent<MeshRenderer>();
            leftFloorCubeRenderer.material = _material;
            _materialPropertyBlock.SetColor(visualizerColorID, Color.black.ColorWithAlpha(0f));
            leftFloorCubeRenderer.SetPropertyBlock(_materialPropertyBlock);

            rightFloorRoot.transform.SetParent(FloorViewController.visualizerFloorRoot.transform, false);
            rightFloorRoot.transform.localScale = Vector3.one;
            rightFloorRoot.transform.localPosition = new Vector3(2.3f, -0.05f, 44f);

            GameObject rightFloorCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightFloorCube.transform.SetParent(rightFloorRoot.transform, false);
            rightFloorCube.transform.localScale = new Vector3(0.115f, 0.11f, 50f);
            rightFloorCube.transform.localPosition = new Vector3(0f, 0f, -11.15f);
            MeshRenderer rightFloorCubeRenderer = rightFloorCube.GetComponent<MeshRenderer>();
            rightFloorCubeRenderer.material = _material;
            _materialPropertyBlock.SetColor(visualizerColorID, Color.black.ColorWithAlpha(0f));
            rightFloorCubeRenderer.SetPropertyBlock(_materialPropertyBlock);

            for (int i = 0; i < 31; i++)
            {
                GameObject parentObjectLeft = new GameObject("parentObjectLeft");
                parentObjectLeft.transform.SetParent(leftFloorRoot.transform, false);

                GameObject cubeObjectLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cubeObjectLeft.transform.SetParent(parentObjectLeft.transform, false);

                parentObjectLeft.transform.localScale = new Vector3(0.1f, 0.1f, 3f);
                parentObjectLeft.transform.localPosition = new Vector3(0f, 0f, 0f - (i * 1.2f));

                cubeObjectLeft.transform.localPosition = new Vector3(0f, 0f, 0.5f);
                cubeObjectLeft.transform.localScale = Vector3.one;
                MeshRenderer childMeshRenderer = cubeObjectLeft.GetComponent<MeshRenderer>();
                childMeshRenderer.material = _material;
                leftObject.Add(parentObjectLeft);


                GameObject parentObjectRight = new GameObject("parentObjectRight");
                parentObjectRight.transform.SetParent(rightFloorRoot.transform, false);

                GameObject cubeObjectRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cubeObjectRight.transform.SetParent(parentObjectRight.transform, false);

                parentObjectRight.transform.localScale = new Vector3(0.1f, 0.1f, 3f);
                parentObjectRight.transform.localPosition = new Vector3(0f, 0f, 0f - (i * 1.2f));

                cubeObjectRight.transform.localPosition = new Vector3(0f, 0f, 0.5f);
                cubeObjectRight.transform.localScale = Vector3.one;
                MeshRenderer childMeshRendererRight = cubeObjectRight.GetComponent<MeshRenderer>();
                childMeshRendererRight.material = _material;
                rightObject.Add(parentObjectRight);
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
