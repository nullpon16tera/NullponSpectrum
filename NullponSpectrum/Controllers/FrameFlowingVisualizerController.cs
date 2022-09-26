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
    class FrameFlowingVisualizerController : IInitializable, IDisposable
    {
        private float scale = 2f;
        private int size = 4;

        private Material _material;
        private MaterialPropertyBlock _materialPropertyBlock;
        private int visualizerColorID;

        private List<GameObject> frontLeft = new List<GameObject>(15);
        private List<GameObject> frontRight= new List<GameObject>(15);
        private List<GameObject> rearLeft = new List<GameObject>(15);
        private List<GameObject> rearRight = new List<GameObject>(15);

        private List<GameObject> topLeft = new List<GameObject>(10);
        private List<GameObject> topRight = new List<GameObject>(10);
        private List<GameObject> bottomLeft = new List<GameObject>(10);
        private List<GameObject> bottomRight = new List<GameObject>(10);

        private GameObject frameFlowingVisualizerRoot = new GameObject("frameFlowingVisualizerRoot");

        private enum Flash
        {
            None,
            On,
            Off
        };

        private Flash flashDone;

        private float[] s_shift = new float[15];
        private float[] s_prev = new float[15];

        private void OnUpdatedRawSpectrums(AudioSpectrum obj)
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }
            if (!PluginConfig.Instance.FrameFlowingVisualizer)
            {
                return;
            }
            this.UpdateAudioSpectrums(obj);
        }

        private void UpdateAudioSpectrums(AudioSpectrum audio)
        {
            var needUpdate = VisualizerUtil.GetNeedUpdate();
            if (!audio)
            {
                return;
            }

            float tmp = Mathf.Lerp(0f, 1.5f, this._audioSpectrum.PeakLevels[0] * 6f);
            //float tmp = this._audioSpectrum.PeakLevels[0] * 10f;
            for (int i = 0; i < frontLeft.Count; i++)
            {
                int index = 14 - i;
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
            }
            
            if (this.s_shift.All(x => this.Nomalize(x) > 0.5f))
            {
                switch (this.flashDone)
                {
                    case Flash.None:
                    case Flash.On:
                        this.flashDone = Flash.Off;
                        for (int i = 0; i < frontLeft.Count; i++)
                        {
                            FlashMaterial(frontLeft[i], VisualizerUtil.GetLeftSaberHSV(), false);
                            FlashMaterial(frontRight[i], VisualizerUtil.GetRightSaberHSV(), false);
                            FlashMaterial(rearLeft[i], VisualizerUtil.GetRightSaberHSV(), false);
                            FlashMaterial(rearRight[i], VisualizerUtil.GetLeftSaberHSV(), false);
                        }
                        for (int i = 0; i < topLeft.Count; i++)
                        {
                            FlashMaterial(topLeft[i], VisualizerUtil.GetRightSaberHSV(), false);
                            FlashMaterial(bottomLeft[i], VisualizerUtil.GetLeftSaberHSV(), false);
                            FlashMaterial(topRight[i], VisualizerUtil.GetLeftSaberHSV(), false);
                            FlashMaterial(bottomRight[i], VisualizerUtil.GetRightSaberHSV(), false);
                        }
                        break;
                    case Flash.Off:
                        this.flashDone = Flash.On;
                        for (int i = 0; i < frontLeft.Count; i++)
                        {
                            FlashMaterial(frontLeft[i], VisualizerUtil.GetLeftSaberHSV(), true);
                            FlashMaterial(frontRight[i], VisualizerUtil.GetRightSaberHSV(), true);
                            FlashMaterial(rearLeft[i], VisualizerUtil.GetRightSaberHSV(), true);
                            FlashMaterial(rearRight[i], VisualizerUtil.GetLeftSaberHSV(), true);
                        }
                        for (int i = 0; i < topLeft.Count; i++)
                        {
                            FlashMaterial(topLeft[i], VisualizerUtil.GetRightSaberHSV(), true);
                            FlashMaterial(bottomLeft[i], VisualizerUtil.GetLeftSaberHSV(), true);
                            FlashMaterial(topRight[i], VisualizerUtil.GetLeftSaberHSV(), true);
                            FlashMaterial(bottomRight[i], VisualizerUtil.GetRightSaberHSV(), true);
                        }
                        break;
                }
            }
            else
            {
                this.flashDone = Flash.None;

                for (int i = 0; i < frontLeft.Count; i++)
                {
                    int index = 14 - i;

                    ChangeMaterialProperty(frontLeft[i], VisualizerUtil.GetLeftSaberHSV(), this.Nomalize(this.s_shift[index]));
                    ChangeMaterialProperty(frontRight[i], VisualizerUtil.GetRightSaberHSV(), this.Nomalize(this.s_shift[index]));

                    ChangeMaterialProperty(rearLeft[i], VisualizerUtil.GetRightSaberHSV(), this.Nomalize(this.s_shift[index]));
                    ChangeMaterialProperty(rearRight[i], VisualizerUtil.GetLeftSaberHSV(), this.Nomalize(this.s_shift[index]));
                }

                for (int i = 0; i < topLeft.Count; i++)
                {
                    int index = 9 - i;

                    ChangeMaterialProperty(topLeft[i], VisualizerUtil.GetRightSaberHSV(), this.Nomalize(this.s_shift[index]));
                    ChangeMaterialProperty(bottomLeft[i], VisualizerUtil.GetLeftSaberHSV(), this.Nomalize(this.s_shift[index]));

                    ChangeMaterialProperty(topRight[i], VisualizerUtil.GetLeftSaberHSV(), this.Nomalize(this.s_shift[index]));
                    ChangeMaterialProperty(bottomRight[i], VisualizerUtil.GetRightSaberHSV(), this.Nomalize(this.s_shift[index]));
                }
            }

            if (needUpdate)
            {
                VisualizerUtil.ResetUpdateTime();
            }
        }

        private float Nomalize(float f)
        {
            var result = Mathf.Lerp(6f, 1f, f);
            return f * result;
        }

        private void FlashMaterial(GameObject obj, float[] hsv, bool allOn)
        {
            if (allOn)
            {
                ChangeMaterialProperty(obj, hsv, 1f);
            }
            else
            {
                ChangeMaterialProperty(obj, hsv, 0f);
            }
        }

        private void ChangeMaterialProperty(GameObject obj, float[] hsv, float tmp)
        {
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            /*var scale = obj.transform.localScale;
            obj.transform.localScale = new Vector3(scale.x, 0.01f + tmp * 0.3f, scale.z);*/
            if (0.5f < tmp)
            {
                obj.SetActive(true);
                var color = Color.HSVToRGB(hsv[0], hsv[1], 1f).ColorWithAlpha(0.7f);
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

        public void Initialize()
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }
            if (!PluginConfig.Instance.FrameFlowingVisualizer)
            {
                return;
            }

            this._audioSpectrum.fallSpeed = 1f;
            this._audioSpectrum.sensibility = 10f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;
            CreateMainObject();
        }

        private void CreateMainObject()
        {
            _material = new Material(Shader.Find("Custom/Glowing"));
            _material.SetColor("_Color", Color.white);

            _materialPropertyBlock = new MaterialPropertyBlock();
            visualizerColorID = Shader.PropertyToID("_Color");

            frameFlowingVisualizerRoot.transform.SetParent(FloorViewController.visualizerFloorRoot.transform, false);
            frameFlowingVisualizerRoot.transform.localPosition = new Vector3(0f, 0.005f, 0f);

            GameObject frontLeftRoot = new GameObject("frontLeftRoot");
            frontLeftRoot.transform.SetParent(frameFlowingVisualizerRoot.transform, false);
            frontLeftRoot.transform.localPosition = new Vector3(-0.05f, 0f, 1f);

            GameObject frontRightRoot = new GameObject("frontRightRoot");
            frontRightRoot.transform.SetParent(frameFlowingVisualizerRoot.transform, false);
            frontRightRoot.transform.localPosition = new Vector3(0.05f, 0f, 1f);

            GameObject rearLeftRoot = new GameObject("rearLeftRoot");
            rearLeftRoot.transform.SetParent(frameFlowingVisualizerRoot.transform, false);
            rearLeftRoot.transform.localPosition = new Vector3(-0.05f, 0f, -1f);

            GameObject rearRightRoot = new GameObject("rearRightRoot");
            rearRightRoot.transform.SetParent(frameFlowingVisualizerRoot.transform, false);
            rearRightRoot.transform.localPosition = new Vector3(0.05f, 0f, -1f);

            GameObject topLeftRoot = new GameObject("topLeftRoot");
            topLeftRoot.transform.SetParent(frameFlowingVisualizerRoot.transform, false);
            topLeftRoot.transform.localPosition = new Vector3(-1.5f, 0f, 0.05f);
            topLeftRoot.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            GameObject bottomLeftRoot = new GameObject("bottomLeftRoot");
            bottomLeftRoot.transform.SetParent(frameFlowingVisualizerRoot.transform, false);
            bottomLeftRoot.transform.localPosition = new Vector3(-1.5f, 0f, -0.05f);
            bottomLeftRoot.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            GameObject topRightRoot = new GameObject("topRightRoot");
            topRightRoot.transform.SetParent(frameFlowingVisualizerRoot.transform, false);
            topRightRoot.transform.localPosition = new Vector3(1.5f, 0f, 0.05f);
            topRightRoot.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            GameObject bottomRightRoot = new GameObject("bottomRightRoot");
            bottomRightRoot.transform.SetParent(frameFlowingVisualizerRoot.transform, false);
            bottomRightRoot.transform.localPosition = new Vector3(1.5f, 0f, -0.05f);
            bottomRightRoot.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);


            for (int i = 0; i < 15; i++)
            {
                GameObject frontLeftChild = GameObject.CreatePrimitive(PrimitiveType.Cube);
                frontLeftChild.transform.SetParent(frontLeftRoot.transform, false);
                frontLeftChild.transform.localPosition = new Vector3(-(0f + (0.1f * i)), 0f, 0f);
                frontLeftChild.transform.localScale = new Vector3(0.08f, 0.01f, 0.05f);
                MeshRenderer meshFrontLeft = frontLeftChild.GetComponent<MeshRenderer>();
                meshFrontLeft.material = _material;
                frontLeft.Add(frontLeftChild);

                GameObject frontRightChild = GameObject.CreatePrimitive(PrimitiveType.Cube);
                frontRightChild.transform.SetParent(frontRightRoot.transform, false);
                frontRightChild.transform.localPosition = new Vector3((0f + (0.1f * i)), 0f, 0f);
                frontRightChild.transform.localScale = new Vector3(0.08f, 0.01f, 0.05f);
                MeshRenderer meshFrontRight = frontRightChild.GetComponent<MeshRenderer>();
                meshFrontRight.material = _material;
                frontRight.Add(frontRightChild);

                GameObject rearLeftChild = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rearLeftChild.transform.SetParent(rearLeftRoot.transform, false);
                rearLeftChild.transform.localPosition = new Vector3(-(0f + (0.1f * i)), 0f, 0f);
                rearLeftChild.transform.localScale = new Vector3(0.08f, 0.01f, 0.05f);
                MeshRenderer meshRearLeft = rearLeftChild.GetComponent<MeshRenderer>();
                meshRearLeft.material = _material;
                rearLeft.Add(rearLeftChild);

                GameObject rearRightChild = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rearRightChild.transform.SetParent(rearRightRoot.transform, false);
                rearRightChild.transform.localPosition = new Vector3((0f + (0.1f * i)), 0f, 0f);
                rearRightChild.transform.localScale = new Vector3(0.08f, 0.01f, 0.05f);
                MeshRenderer meshRearRight = rearRightChild.GetComponent<MeshRenderer>();
                meshRearRight.material = _material;
                rearRight.Add(rearRightChild);
            }

            for (int i = 0; i < 10; i++)
            {
                GameObject topLeftChild = GameObject.CreatePrimitive(PrimitiveType.Cube);
                topLeftChild.transform.SetParent(topLeftRoot.transform, false);
                topLeftChild.transform.localPosition = new Vector3(-(0f + (0.1f * i)), 0f, 0f);
                topLeftChild.transform.localScale = new Vector3(0.08f, 0.01f, 0.05f);
                MeshRenderer meshTopLeft = topLeftChild.GetComponent<MeshRenderer>();
                meshTopLeft.material = _material;
                topLeft.Add(topLeftChild);

                GameObject bottomLeftChild = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bottomLeftChild.transform.SetParent(bottomLeftRoot.transform, false);
                bottomLeftChild.transform.localPosition = new Vector3(0f + (0.1f * i), 0f, 0f);
                bottomLeftChild.transform.localScale = new Vector3(0.08f, 0.01f, 0.05f);
                MeshRenderer meshBottomLeft = bottomLeftChild.GetComponent<MeshRenderer>();
                meshBottomLeft.material = _material;
                bottomLeft.Add(bottomLeftChild);

                GameObject topRightChild = GameObject.CreatePrimitive(PrimitiveType.Cube);
                topRightChild.transform.SetParent(topRightRoot.transform, false);
                topRightChild.transform.localPosition = new Vector3(-(0f + (0.1f * i)), 0f, 0f);
                topRightChild.transform.localScale = new Vector3(0.08f, 0.01f, 0.05f);
                MeshRenderer meshTopRight = topRightChild.GetComponent<MeshRenderer>();
                meshTopRight.material = _material;
                topRight.Add(topRightChild);

                GameObject bottomRightChild = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bottomRightChild.transform.SetParent(bottomRightRoot.transform, false);
                bottomRightChild.transform.localPosition = new Vector3(0f + (0.1f * i), 0f, 0f);
                bottomRightChild.transform.localScale = new Vector3(0.08f, 0.01f, 0.05f);
                MeshRenderer meshBottomRight = bottomRightChild.GetComponent<MeshRenderer>();
                meshBottomRight.material = _material;
                bottomRight.Add(bottomRightChild);
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
