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
    public class MeshVisualizerController : IInitializable, IDisposable
    {
        private int size = 26;

        private Material _meshMaterial;
        private MaterialPropertyBlock _materialPropertyBlock;
        private int visualizerTintColorID;
        private int visualizerBrightnessID;

        private List<GameObject> objLeft = new List<GameObject>(26);
        private List<GameObject> objRight = new List<GameObject>(26);
        private Material _lineMaterial;
        private Material _frameMaterial;
        private Material _floorMaterial;
        private GameObject meshVisualizerRoot = new GameObject("meshVisualizerRoot");
        private float leftHSV;
        private float rightHSV;

        public enum FramePosition
        {
            Front,
            Back,
            Left,
            Right,
        };

        private void OnUpdatedRawSpectrums(AudioSpectrum26 obj)
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }
            if (!PluginConfig.Instance.MeshVisualizer)
            {
                return;
            }
            this.UpdateAudioSpectrums(obj);
        }

        private void UpdateAudioSpectrums(AudioSpectrum26 audio)
        {
            if (!audio)
            {
                return;
            }


            for (int i = 0; i < size; i++)
            {
                var alpha = (this._audioSpectrum.PeakLevels[((size - 1) - i)] * 20f) % 1f;
                ChangeMaterialProperty(objLeft[i], leftHSV, alpha);
                ChangeMaterialProperty(objRight[i], rightHSV, alpha);
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

            if (!PluginConfig.Instance.MeshVisualizer)
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


            this._audioSpectrum.Band = AudioSpectrum26.BandType.TwentySixBand;
            this._audioSpectrum.numberOfSamples = 2048;
            this._audioSpectrum.fallSpeed = 1f;
            this._audioSpectrum.sensibility = 0.01f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;

            CreateFloorObject();
            CreateFrameObject();
            CreateMainObject();
            CreateLineObject();

            if (PluginConfig.Instance.isFloorHeight)
            {
                var rootPosition = meshVisualizerRoot.transform.localPosition;
                rootPosition.y = PluginConfig.Instance.floorHeight * 0.01f;
                meshVisualizerRoot.transform.localPosition = rootPosition;
            }

            this.meshVisualizerRoot.transform.SetParent(Utilities.FloorAdjustorUtil.NullponSpectrumFloor.transform);
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
            floor.SetActive(floor);
            floor.transform.SetParent(meshVisualizerRoot.transform);
        }

        private void CreateFrameObject()
        {
            GameObject parent = new GameObject("meshVisualizerFrame");

            _frameMaterial = new Material(Shader.Find("Custom/Glowing"));
            _frameMaterial.SetColor("_Color  ", Color.white.ColorWithAlpha(1f));

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
            parent.transform.SetParent(meshVisualizerRoot.transform);
        }

        private void CreateMainObject()
        {
            _meshMaterial = new Material(Shader.Find("Custom/SaberBlade"));
            _meshMaterial.SetColor("_TintColor", Color.black.ColorWithAlpha(1f));
            _meshMaterial.SetFloat("_Brightness", 0f);

            _materialPropertyBlock = new MaterialPropertyBlock();
            visualizerTintColorID = Shader.PropertyToID("_TintColor");
            visualizerBrightnessID = Shader.PropertyToID("_Brightness");

            var scale = new Vector3(0.0044f, 0.01f, 0.2f);

            // メインオブジェクト生成
            for (int i = 0; i < size; i++)
            {
                GameObject leftObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Transform leftTransform = leftObj.transform;
                leftTransform.localScale = scale;
                leftTransform.localPosition = new Vector3(-(0.03f + (0.057f * i)), 0.0051f, 0f);
                var leftMeshRenderer = leftObj.GetComponent<MeshRenderer>();
                leftMeshRenderer.material = _meshMaterial;
                leftObj.transform.SetParent(meshVisualizerRoot.transform);
                objLeft.Add(leftObj);


                GameObject rightObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Transform rightTransform = rightObj.transform;
                rightTransform.localScale = scale;
                rightTransform.localPosition = new Vector3((0.03f + (0.057f * i)), 0.0051f, 0f);
                var rightMeshRenderer = rightObj.GetComponent<MeshRenderer>();
                rightMeshRenderer.material = _meshMaterial;
                rightObj.transform.SetParent(meshVisualizerRoot.transform);
                objRight.Add(rightObj);
            }
        }

        private void CreateLineObject()
        {
            // メッシュになるようのオブジェクト生成
            var lineColor = Color.HSVToRGB(0.5f, 0f, 0f);
            _lineMaterial = new Material(Shader.Find("Custom/Glowing"));
            _lineMaterial.SetColor("_Color", lineColor.ColorWithAlpha(0f));

            for (int i = 0; i < 20; i++)
            {
                GameObject line = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Transform lineTransform = line.transform;
                lineTransform.localScale = new Vector3(0.3f, 0.01f, 0.0025f);
                lineTransform.localPosition = new Vector3(0f, 0.0052f, -1f + (0.1f * i));
                MeshRenderer lineMeshRendere = line.GetComponent<MeshRenderer>();
                lineMeshRendere.material = _lineMaterial;
                line.transform.SetParent(meshVisualizerRoot.transform);
            }
        }

        private bool _disposedValue;
        private ColorScheme _colorScheme;
        private AudioSpectrum26 _audioSpectrum;

        [Inject]
        public void Constructor(ColorScheme scheme, AudioSpectrum26 audioSpectrum)
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
