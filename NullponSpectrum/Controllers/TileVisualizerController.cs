using NullponSpectrum.Configuration;
using NullponSpectrum.AudioSpectrums;
using NullponSpectrum.Utilities;
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

        private Material _tileMaterial;
        private MaterialPropertyBlock _materialPropertyBlock;
        private int visualizerColorID;

        private List<GameObject> objLeftA = new List<GameObject>(6);
        private List<GameObject> objRightA = new List<GameObject>(6);
        private List<GameObject> objLeftB = new List<GameObject>(6);
        private List<GameObject> objRightB = new List<GameObject>(6);

        private GameObject tileFloorRoot;
        private Material _lineMaterial;
        private float[] leftHSV = new float[3];
        private float[] rightHSV = new float[3];

        private void OnUpdatedRawSpectrums(AudioSpectrum8 obj)
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

        private void UpdateAudioSpectrums(AudioSpectrum8 audio)
        {
            if (!audio)
            {
                return;
            }

            for (int i = 0; i < size; i++)
            {
                var alpha = (this._audioSpectrum.Levels[size - i] * 10f) % 1f;
                ChangeMaterialProperty(objLeftA[i], leftHSV, alpha);
                ChangeMaterialProperty(objRightA[i], rightHSV, alpha);
                ChangeMaterialProperty(objLeftB[i], leftHSV, alpha);
                ChangeMaterialProperty(objRightB[i], rightHSV, alpha);

            }

        }

        private void ChangeMaterialProperty(GameObject obj, float[] hsv, float alpha)
        {
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if (0.15f < alpha)
            {
                var color = Color.HSVToRGB(hsv[0], hsv[1], 1f).ColorWithAlpha(0.7f);
                _materialPropertyBlock.SetColor(visualizerColorID, color);
                renderer.SetPropertyBlock(_materialPropertyBlock);
            }
            else
            {
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

            if (!PluginConfig.Instance.TileVisualizer)
            {
                return;
            }

            // セイバーの色取得
            float leftH, leftS, leftV;
            float rightH, rightS, rightV;

            Color.RGBToHSV(this._colorScheme.saberAColor, out leftH, out leftS, out leftV);
            Color.RGBToHSV(this._colorScheme.saberBColor, out rightH, out rightS, out rightV);
            this.leftHSV[0] = leftH;
            this.rightHSV[0] = rightH;
            this.leftHSV[1] = leftS;
            this.rightHSV[1] = rightS;
            this.leftHSV[2] = leftV;
            this.rightHSV[2] = rightV;


            this._audioSpectrum.Band = AudioSpectrum8.BandType.EightBand;
            this._audioSpectrum.numberOfSamples = 512;
            this._audioSpectrum.fallSpeed = 0.15f;
            this._audioSpectrum.sensibility = 10f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;

            tileFloorRoot = new GameObject("tileFloorRoot");
            tileFloorRoot.transform.SetParent(FloorViewController.visualizerFloorRoot.transform, false);
            tileFloorRoot.transform.localPosition = new Vector3(0f, 0.0001f, 0f);

            CreateMainObject();
            CreateLineObject();
        }

        private void CreateMainObject()
        {

            _tileMaterial = new Material(Shader.Find("Custom/Glowing"));
            _tileMaterial.SetColor("_Color", Color.black.ColorWithAlpha(1f));

            _materialPropertyBlock = new MaterialPropertyBlock();
            visualizerColorID = Shader.PropertyToID("_Color");


            // メインオブジェクト生成
            for (int i = 0; i < size; i++)
            {
                // Left object area
                GameObject leftObjA = GameObject.CreatePrimitive(PrimitiveType.Plane);
                leftObjA.transform.SetParent(tileFloorRoot.transform, false);
                Transform leftTransformA = leftObjA.transform;
                leftTransformA.localScale = new Vector3(0.05f, 0.01f, 0.05f);
                if (i % 2 == 0)
                {
                    leftTransformA.localPosition = new Vector3(-(0.25f + (0.25f * i)), 0f, 0.25f);
                }
                else
                {
                    leftTransformA.localPosition = new Vector3(-(0f + (0.25f * i)), 0f, 0.75f);
                }

                GameObject rightObjA = GameObject.CreatePrimitive(PrimitiveType.Plane);
                rightObjA.transform.SetParent(tileFloorRoot.transform, false);
                Transform rightTransformA = rightObjA.transform;
                rightTransformA.localScale = new Vector3(0.05f, 0.01f, 0.05f);
                if (i % 2 == 0)
                {
                    rightTransformA.localPosition = new Vector3(-(0.25f + (0.25f * i)), 0f, -0.25f);
                }
                else
                {
                    rightTransformA.localPosition = new Vector3(-(0f + (0.25f * i)), 0f, -0.75f);
                }

                var leftMeshRenderer = leftObjA.GetComponent<MeshRenderer>();
                leftMeshRenderer.material = _tileMaterial;
                var rightMeshRenderer = rightObjA.GetComponent<MeshRenderer>();
                rightMeshRenderer.material = _tileMaterial;

                objLeftA.Add(leftObjA);
                objRightA.Add(rightObjA);

                // Right object area
                GameObject leftObjB = GameObject.CreatePrimitive(PrimitiveType.Plane);
                leftObjB.transform.SetParent(tileFloorRoot.transform, false);
                Transform leftTransformB = leftObjB.transform;
                leftTransformB.localScale = new Vector3(0.05f, 0.01f, 0.05f);
                if (i % 2 == 0)
                {
                    leftTransformB.localPosition = new Vector3((0.25f + (0.25f * i)), 0f, -0.25f);
                }
                else
                {
                    leftTransformB.localPosition = new Vector3((0f + (0.25f * i)), 0f, -0.75f);
                }

                GameObject rightObjB = GameObject.CreatePrimitive(PrimitiveType.Plane);
                rightObjB.transform.SetParent(tileFloorRoot.transform, false);
                Transform rightTransformB = rightObjB.transform;
                rightTransformB.localScale = new Vector3(0.05f, 0.01f, 0.05f);
                if (i % 2 == 0)
                {
                    rightTransformB.localPosition = new Vector3((0.25f + (0.25f * i)), 0f, 0.25f);
                }
                else
                {
                    rightTransformB.localPosition = new Vector3((0f + (0.25f * i)), 0f, 0.75f);
                }

                var leftMeshRendererB = leftObjB.GetComponent<MeshRenderer>();
                leftMeshRendererB.material = _tileMaterial;
                var rightMeshRendererB = rightObjB.GetComponent<MeshRenderer>();
                rightMeshRendererB.material = _tileMaterial;

                objLeftB.Add(leftObjB);
                objRightB.Add(rightObjB);
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
                line.transform.SetParent(tileFloorRoot.transform, false);
                Transform lineTransform = line.transform;
                lineTransform.localScale = new Vector3(0.0025f, 0.01f, 0.2f);
                lineTransform.localPosition = new Vector3(-1f + (0.5f * i), 0.0001f, 0f);
                MeshRenderer lineMeshRendere = line.GetComponent<MeshRenderer>();
                lineMeshRendere.material = _lineMaterial;
                line.SetActive(line);
            }

            for (int i = 0; i < 3; i++)
            {
                GameObject line2 = GameObject.CreatePrimitive(PrimitiveType.Plane);
                line2.transform.SetParent(tileFloorRoot.transform, false);
                Transform lineTransform2 = line2.transform;
                lineTransform2.localScale = new Vector3(0.3f, 0.01f, 0.0025f);
                lineTransform2.localPosition = new Vector3(0f, 0.0002f, -0.5f + (0.5f * i));
                MeshRenderer lineMeshRendere2 = line2.GetComponent<MeshRenderer>();
                lineMeshRendere2.material = _lineMaterial;
                line2.SetActive(line2);
            }
        }

        private bool _disposedValue;
        private ColorScheme _colorScheme;
        private AudioSpectrum8 _audioSpectrum;

        [Inject]
        public void Constructor(ColorScheme scheme, AudioSpectrum8 audioSpectrum)
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
