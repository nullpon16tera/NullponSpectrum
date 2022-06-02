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
    public class TileVisualizerController : IInitializable, IDisposable
    {
        private int size = 6;

        private List<GameObject> leftPlaneA = new List<GameObject>(6);
        private List<GameObject> rightPlaneA = new List<GameObject>(6);
        private List<GameObject> leftPlaneB = new List<GameObject>(6);
        private List<GameObject> rightPlaneB = new List<GameObject>(6);
        private List<Material> _leftMaterials = new List<Material>(6);
        private List<Material> _rightMaterials = new List<Material>(6);
        private Material _lineMaterial;
        private Material _frameMaterial;
        private Material _floorMaterial;
        private GameObject tileVisualizerRoot = new GameObject("tileVisualizerRoot");
        private float leftHSV;
        private float rightHSV;

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
            if (!PluginConfig.Instance.TileVisualizer)
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

            for (int i = 0; i < _leftMaterials.Count; i++)
            {
                var alpha = (this._audioSpectrum.Levels[((size) - i)] * 20f) % 1f;
                var leftColor = Color.HSVToRGB(leftHSV, 1f, alpha);
                var rightColor = Color.HSVToRGB(rightHSV, 1f, alpha);
                _leftMaterials[i].SetColor("_Color", leftColor.ColorWithAlpha(alpha));
                _rightMaterials[i].SetColor("_Color", rightColor.ColorWithAlpha(alpha));
            }

        }


        public void Initialize()
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }

            if (!PluginConfig.Instance.TileVisualizer)
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


            this._audioSpectrum.Band = AudioSpectrum.BandType.EightBand;
            this._audioSpectrum.numberOfSamples = 512;
            this._audioSpectrum.fallSpeed = 0.15f;
            this._audioSpectrum.sensibility = 5f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;


            CreateFloorObject();
            CreateFrameObject();
            CreateMainObject();
            CreateLineObject();

            this.tileVisualizerRoot.transform.SetParent(Utilities.FloorAdjustorUtil.NullponSpectrumFloor.transform);
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
            floor.transform.SetParent(tileVisualizerRoot.transform);
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
            parent.transform.SetParent(tileVisualizerRoot.transform);
        }

        private void CreateMainObject()
        {
            // メインマテリアル生成
            for (int r = 0; r < size; r++)
            {
                Material leftMaterial = new Material(Shader.Find("Custom/Glowing"));
                leftMaterial.SetColor("_Color", Color.black.ColorWithAlpha(1f));
                leftMaterial.SetFloat("_EnableColorInstancing", 1f);
                leftMaterial.SetFloat("_WhiteBoostType", 1f);
                leftMaterial.SetFloat("_NoiseDithering", 1f);
                _leftMaterials.Add(leftMaterial);

                Material rightMaterial = new Material(Shader.Find("Custom/Glowing"));
                rightMaterial.SetColor("_Color", Color.black.ColorWithAlpha(1f));
                rightMaterial.SetFloat("_EnableColorInstancing", 1f);
                rightMaterial.SetFloat("_WhiteBoostType", 1f);
                rightMaterial.SetFloat("_NoiseDithering", 1f);
                _rightMaterials.Add(rightMaterial);
            }

            // メインオブジェクト生成
            for (int i = 0; i < size; i++)
            {
                // Left object area
                GameObject leftObjA = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Transform leftTransformA = leftObjA.transform;
                leftTransformA.localScale = new Vector3(0.05f, 0.01f, 0.05f);
                if (i % 2 == 0)
                {
                    leftTransformA.localPosition = new Vector3(-(0.25f + (0.25f * i)), 0.0051f, 0.25f);
                }
                else
                {
                    leftTransformA.localPosition = new Vector3(-(0f + (0.25f * i)), 0.0051f, 0.75f);
                }

                GameObject rightObjA = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Transform rightTransformA = rightObjA.transform;
                rightTransformA.localScale = new Vector3(0.05f, 0.01f, 0.05f);
                if (i % 2 == 0)
                {
                    rightTransformA.localPosition = new Vector3(-(0.25f + (0.25f * i)), 0.0051f, -0.25f);
                }
                else
                {
                    rightTransformA.localPosition = new Vector3(-(0f + (0.25f * i)), 0.0051f, -0.75f);
                }

                var leftMeshRenderer = leftObjA.GetComponent<MeshRenderer>();
                leftMeshRenderer.material = _leftMaterials[i];
                var rightMeshRenderer = rightObjA.GetComponent<MeshRenderer>();
                rightMeshRenderer.material = _rightMaterials[i];

                leftObjA.transform.SetParent(tileVisualizerRoot.transform);
                rightObjA.transform.SetParent(tileVisualizerRoot.transform);


                // Right object area
                GameObject leftObjB = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Transform leftTransformB = leftObjB.transform;
                leftTransformB.localScale = new Vector3(0.05f, 0.01f, 0.05f);
                if (i % 2 == 0)
                {
                    leftTransformB.localPosition = new Vector3((0.25f + (0.25f * i)), 0.0051f, -0.25f);
                }
                else
                {
                    leftTransformB.localPosition = new Vector3((0f + (0.25f * i)), 0.0051f, -0.75f);
                }

                GameObject rightObjB = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Transform rightTransformB = rightObjB.transform;
                rightTransformB.localScale = new Vector3(0.05f, 0.01f, 0.05f);
                if (i % 2 == 0)
                {
                    rightTransformB.localPosition = new Vector3((0.25f + (0.25f * i)), 0.0051f, 0.25f);
                }
                else
                {
                    rightTransformB.localPosition = new Vector3((0f + (0.25f * i)), 0.0051f, 0.75f);
                }

                var leftMeshRendererB = leftObjB.GetComponent<MeshRenderer>();
                leftMeshRendererB.material = _leftMaterials[i];
                var rightMeshRendererB = rightObjB.GetComponent<MeshRenderer>();
                rightMeshRendererB.material = _rightMaterials[i];

                leftObjB.transform.SetParent(tileVisualizerRoot.transform);
                rightObjB.transform.SetParent(tileVisualizerRoot.transform);
            }
        }

        private void CreateLineObject()
        {
            // メッシュになるようのオブジェクト生成
            var lineColor = Color.HSVToRGB(0.5f, 0f, 0f);
            _lineMaterial = new Material(Shader.Find("Custom/Glowing"));
            _lineMaterial.SetColor("_Color", lineColor.ColorWithAlpha(0f));

            for (int i = 0; i < 5; i++)
            {
                GameObject line = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Transform lineTransform = line.transform;
                lineTransform.localScale = new Vector3(0.0025f, 0.01f, 0.2f);
                lineTransform.localPosition = new Vector3(-1f + (0.5f * i), 0.0052f, 0f);
                MeshRenderer lineMeshRendere = line.GetComponent<MeshRenderer>();
                lineMeshRendere.material = _lineMaterial;
                line.transform.SetParent(tileVisualizerRoot.transform);
                line.SetActive(line);
            }

            for (int i = 0; i < 3; i++)
            {
                GameObject line2 = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Transform lineTransform2 = line2.transform;
                lineTransform2.localScale = new Vector3(0.3f, 0.01f, 0.0025f);
                lineTransform2.localPosition = new Vector3(0f, 0.0052f, -0.5f + (0.5f * i));
                MeshRenderer lineMeshRendere2 = line2.GetComponent<MeshRenderer>();
                lineMeshRendere2.material = _lineMaterial;
                line2.transform.SetParent(tileVisualizerRoot.transform);
                line2.SetActive(line2);
            }
        }

        private bool _disposedValue;
        private ColorScheme _colorScheme;
        private AudioSpectrum _audioSpectrum;

        [Inject]
        public void Constructor(ColorScheme scheme, AudioSpectrum audioSpectrum)
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
