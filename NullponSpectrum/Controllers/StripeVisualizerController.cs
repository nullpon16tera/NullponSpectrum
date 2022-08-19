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
    public class StripeVisualizerController : IInitializable, IDisposable
    {
        private int size = 31;

        private Material _stripeMaterial;
        private MaterialPropertyBlock _materialPropertyBlock;
        private int visualizerTintColorID;
        private int visualizerBrightnessID;

        private List<GameObject> leftPlane = new List<GameObject>(31);
        private List<GameObject> rightPlane = new List<GameObject>(31);
        private Material _frameMaterial;
        private Material _floorMaterial;
        private GameObject stripeVisualizerRoot = new GameObject("stripeVisualizerRoot");
        private float leftHSV;
        private float rightHSV;

        public enum FramePosition
        {
            Front,
            Back,
            Left,
            Right,
        };

        private void OnUpdatedRawSpectrums(AudioSpectrum31 obj)
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

        private void UpdateAudioSpectrums(AudioSpectrum31 audio)
        {
            if (!audio)
            {
                return;
            }

            for (int i = 0; i < size; i++)
            {
                var alpha = (this._audioSpectrum.PeakLevels[((size - 1) - i)] * 20f) % 1f;
                ChangeMaterialProperty(leftPlane[i], leftHSV, alpha);
                ChangeMaterialProperty(rightPlane[i], rightHSV, alpha);

            }

        }

        private void ChangeMaterialProperty(GameObject obj, float h, float alpha)
        {
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if (0.15f < alpha)
            {
                var color = Color.HSVToRGB(h, 1f, 1f).ColorWithAlpha(0.5f);
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

            if (!PluginConfig.Instance.StripeVisualizer)
            {
                return;
            }

            // セイバーの色取得
            float leftH, leftS, leftV;
            float rightH, rightS, rightV;

            Color.RGBToHSV(this._colorScheme.saberAColor, out leftH, out leftS, out leftV);
            Color.RGBToHSV(this._colorScheme.saberBColor, out rightH, out rightS, out rightV);
            this.leftHSV = leftH;
            this.rightHSV = rightH;


            this._audioSpectrum.Band = AudioSpectrum31.BandType.ThirtyOneBand;
            this._audioSpectrum.numberOfSamples = 2048;
            this._audioSpectrum.fallSpeed = 1f;
            this._audioSpectrum.sensibility = 0.001f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;


            CreateFloorObject();
            CreateFrameObject();
            CreateMainObject();

            if (PluginConfig.Instance.isFloorHeight)
            {
                var rootPosition = stripeVisualizerRoot.transform.localPosition;
                rootPosition.y = PluginConfig.Instance.floorHeight * 0.01f;
                stripeVisualizerRoot.transform.localPosition = rootPosition;
            }

            this.stripeVisualizerRoot.transform.SetParent(Utilities.FloorAdjustorUtil.NullponSpectrumFloor.transform);
        }

        private void CreateFloorObject()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            MeshRenderer floorRenderer = floor.GetComponent<MeshRenderer>();
            _floorMaterial = new Material(Shader.Find("Custom/Glowing"));
            _floorMaterial.SetColor("_Color", Color.black.ColorWithAlpha(0f));
            floorRenderer.material = _floorMaterial;

            floor.transform.localScale = new Vector3(0.3f, 0.01f, 0.2f);
            floor.transform.localPosition = new Vector3(0f, 0.005f, 0f);
            floor.transform.SetParent(stripeVisualizerRoot.transform);
        }

        private void CreateFrameObject()
        {
            GameObject parent = new GameObject("stripeVisualizerFrame");

            _frameMaterial = new Material(Shader.Find("Custom/Glowing"));
            _frameMaterial.SetColor("_Color", Color.white.ColorWithAlpha(1f));

            for (int i = 0; i < 4; i++)
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
            }
            parent.transform.SetParent(stripeVisualizerRoot.transform);
        }

        private void CreateMainObject()
        {
            _stripeMaterial = new Material(Shader.Find("Custom/SaberBlade"));
            _stripeMaterial.SetColor("_TintColor", Color.black.ColorWithAlpha(1f));
            _stripeMaterial.SetFloat("_Brightness", 0f);

            _materialPropertyBlock = new MaterialPropertyBlock();
            visualizerTintColorID = Shader.PropertyToID("_TintColor");
            visualizerBrightnessID = Shader.PropertyToID("_Brightness");

            var scale = new Vector3(0.0035f, 0.01f, 0.2f);

            // メインオブジェクト生成
            for (int i = 0; i < size; i++)
            {
                GameObject leftObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Transform leftTransform = leftObj.transform;
                leftTransform.localScale = scale;
                leftTransform.localPosition = new Vector3(-(0.0035f + (0.049f * i)), 0.0051f, 0f);
                leftObj.transform.SetParent(stripeVisualizerRoot.transform);
                var leftMeshRenderer = leftObj.GetComponent<MeshRenderer>();
                leftMeshRenderer.material = _stripeMaterial;
                leftPlane.Add(leftObj);

                GameObject rightObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Transform rightTransform = rightObj.transform;
                rightTransform.localScale = scale;
                rightTransform.localPosition = new Vector3((0.0035f + (0.049f * i)), 0.0051f, 0f);
                rightObj.transform.SetParent(stripeVisualizerRoot.transform);
                var rightMeshRenderer = rightObj.GetComponent<MeshRenderer>();
                rightMeshRenderer.material = _stripeMaterial;
                rightPlane.Add(rightObj);
            }
        }

        private bool _disposedValue;
        private ColorScheme _colorScheme;
        private AudioSpectrum31 _audioSpectrum;

        [Inject]
        public void Constructor(ColorScheme scheme, AudioSpectrum31 audioSpectrum)
        {
            this._colorScheme = scheme;
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
