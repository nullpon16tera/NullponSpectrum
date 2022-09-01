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
        private int visualizerColorID;

        private List<GameObject> objLeft = new List<GameObject>(26);
        private List<GameObject> objRight = new List<GameObject>(26);

        private GameObject meshVisualizerRoot;

        private Material _lineMaterial;
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
                var alpha = (this._audioSpectrum.PeakLevels[((size - 1) - i)] * 10f);
                ChangeMaterialProperty(objLeft[i], leftHSV, alpha);
                ChangeMaterialProperty(objRight[i], rightHSV, alpha);
            }

        }

        private void ChangeMaterialProperty(GameObject obj, float h, float alpha)
        {
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if (0.15f < alpha)
            {
                obj.SetActive(true);
                var color = Color.HSVToRGB(h, 1f, 1f).ColorWithAlpha(0.7f);
                _materialPropertyBlock.SetColor(visualizerColorID, color);
                renderer.SetPropertyBlock(_materialPropertyBlock);
            }
            else
            {
                obj.SetActive(false);
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

            meshVisualizerRoot = new GameObject("meshVisualizerRoot");
            meshVisualizerRoot.transform.SetParent(FloorViewController.visualizerFloorRoot.transform, false);

            CreateMainObject();
            CreateLineObject();
        }

        private void CreateMainObject()
        {
            _meshMaterial = new Material(Shader.Find("Custom/Glowing"));
            _meshMaterial.SetColor("_Color", Color.black.ColorWithAlpha(0f));

            _materialPropertyBlock = new MaterialPropertyBlock();
            visualizerColorID = Shader.PropertyToID("_Color");

            var scale = new Vector3(0.0044f, 0.01f, 0.2f);

            // メインオブジェクト生成
            for (int i = 0; i < size; i++)
            {
                GameObject leftObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                leftObj.transform.SetParent(meshVisualizerRoot.transform, false);
                Transform leftTransform = leftObj.transform;
                leftTransform.localScale = scale;
                leftTransform.localPosition = new Vector3(-(0.03f + (0.057f * i)), 0.0001f, 0f);
                var leftMeshRenderer = leftObj.GetComponent<MeshRenderer>();
                leftMeshRenderer.material = _meshMaterial;
                objLeft.Add(leftObj);


                GameObject rightObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                rightObj.transform.SetParent(meshVisualizerRoot.transform, false);
                Transform rightTransform = rightObj.transform;
                rightTransform.localScale = scale;
                rightTransform.localPosition = new Vector3((0.03f + (0.057f * i)), 0.0001f, 0f);
                var rightMeshRenderer = rightObj.GetComponent<MeshRenderer>();
                rightMeshRenderer.material = _meshMaterial;
                objRight.Add(rightObj);
            }
        }

        private void CreateLineObject()
        {
            // メッシュになるようのオブジェクト生成
            _lineMaterial = new Material(Shader.Find("Custom/Glowing"));
            _lineMaterial.SetColor("_Color", Color.black.ColorWithAlpha(0f));

            for (int i = 0; i < 20; i++)
            {
                GameObject line = GameObject.CreatePrimitive(PrimitiveType.Plane);
                line.transform.SetParent(meshVisualizerRoot.transform, false);
                Transform lineTransform = line.transform;
                lineTransform.localScale = new Vector3(0.3f, 0.01f, 0.0025f);
                lineTransform.localPosition = new Vector3(0f, 0.0002f, -1f + (0.1f * i));
                MeshRenderer lineMeshRendere = line.GetComponent<MeshRenderer>();
                lineMeshRendere.material = _lineMaterial;
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
