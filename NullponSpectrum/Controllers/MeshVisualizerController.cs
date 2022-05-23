﻿using NullponSpectrum.Configuration;
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

        private List<GameObject> leftPlane = new List<GameObject>(26);
        private List<GameObject> rightPlane = new List<GameObject>(26);
        private List<Material> _leftMaterials = new List<Material>(26);
        private List<Material> _rightMaterials = new List<Material>(26);
        private Material _lineMaterial;
        private GameObject meshVisualizerRoot = new GameObject("meshVisualizerRoot");
        private float leftHSV;
        private float rightHSV;

        private void OnUpdatedRawSpectrums(AudioSpectrum obj)
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

        private void UpdateAudioSpectrums(AudioSpectrum audio)
        {
            if (!audio)
            {
                return;
            }


            for (int i = 0; i < _leftMaterials.Count; i++)
            {
                var alpha = (this._audioSpectrum.Levels[((size - 1) - i)] * 20f) % 1f;
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


            this._audioSpectrum.Band = AudioSpectrum.BandType.TwentySixBand;
            this._audioSpectrum.numberOfSamples = 2048;
            this._audioSpectrum.fallSpeed = 10f;
            this._audioSpectrum.sensibility = 0.01f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;

            CreateMainObject();
            CreateLineObject();

            this.meshVisualizerRoot.transform.SetParent(Utilities.VMCAvatarUtil.NullponSpectrumFloor.transform);
        }

        private void CreateMainObject()
        {
            // メインマテリアル生成
            for (int r = 0; r < size; r++)
            {
                Material leftMaterial = new Material(Shader.Find("Custom/Glowing"));
                leftMaterial.SetColor("_Color", Color.white.ColorWithAlpha(1f));
                leftMaterial.SetFloat("_EnableColorInstancing", 1f);
                leftMaterial.SetFloat("_WhiteBoostType", 1f);
                leftMaterial.SetFloat("_NoiseDithering", 1f);
                _leftMaterials.Add(leftMaterial);

                Material rightMaterial = new Material(Shader.Find("Custom/Glowing"));
                rightMaterial.SetColor("_Color", Color.white.ColorWithAlpha(1f));
                rightMaterial.SetFloat("_EnableColorInstancing", 1f);
                rightMaterial.SetFloat("_WhiteBoostType", 1f);
                rightMaterial.SetFloat("_NoiseDithering", 1f);
                _rightMaterials.Add(rightMaterial);
            }

            // メインオブジェクト生成
            for (int i = 0; i < size; i++)
            {
                GameObject leftObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Transform leftTransform = leftObj.transform;
                leftTransform.localScale = new Vector3(0.0044f, 0.01f, 0.2f);
                leftTransform.localPosition = new Vector3(-(0.03f + (0.057f * i)), 0.0051f, 0f);

                GameObject rightObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Transform rightTransform = rightObj.transform;
                rightTransform.localScale = new Vector3(0.0044f, 0.01f, 0.2f);
                rightTransform.localPosition = new Vector3((0.03f + (0.057f * i)), 0.0051f, 0f);

                var leftMeshRenderer = leftObj.GetComponent<MeshRenderer>();
                leftMeshRenderer.material = _leftMaterials[i];
                var rightMeshRenderer = rightObj.GetComponent<MeshRenderer>();
                rightMeshRenderer.material = _rightMaterials[i];

                leftObj.transform.SetParent(meshVisualizerRoot.transform);
                rightObj.transform.SetParent(meshVisualizerRoot.transform);
            }

            foreach (GameObject obj in leftPlane)
            {
                obj.SetActive(obj);
            }
            foreach (GameObject obj in rightPlane)
            {
                obj.SetActive(obj);
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
                line.SetActive(line);
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
