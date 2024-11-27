﻿using NullponSpectrum.Configuration;
using NullponSpectrum.AudioSpectrums;
using NullponSpectrum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace NullponSpectrum.Controllers
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
    public class FrameVisualizerController : IInitializable, IDisposable
    {
        private float scale = 2f;
        private int size = 4;

        private Material _frameMaterial;
        private MaterialPropertyBlock _materialPropertyBlock;
        private int visualizerColorID;

        private List<GameObject> cubes = new List<GameObject>(4);

        private GameObject frameVisualizerRoot;

        public enum FramePosition {
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
            if (!PluginConfig.Instance.FrameVisualizer)
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

            for (int i = 0; i < cubes.Count; i++)
            {
                var peak = this._audioSpectrum.PeakLevels[i] * scale;
                var frameSize = 0.25f + ((size - i) * 0.2f);
                var peakSize = frameSize + peak;
                var alpha = (this._audioSpectrum.PeakLevels[i] * 10);
                var colorLerp = Mathf.Lerp(0.45f, 1f, alpha);

                if (needUpdate)
                {
                    cubes[i].transform.localScale = new Vector3(peakSize, 1f, peakSize);
                }
                for (int r = 0; r < cubes[i].transform.childCount; r++)
                {
                    var childObj = cubes[i].transform.GetChild(r).gameObject;
                    ChangeMaterialProperty(childObj, colorLerp, frameSize, peakSize);
                }
            }

            if (needUpdate)
            {
                Utilities.VisualizerUtil.ResetUpdateTime();
            }
        }

        private float Nomalize(float f)
        {
            var result = Mathf.Lerp(5f, 1f, f);
            return f * result;
        }

        private void ChangeMaterialProperty(GameObject obj, float h, float frameSize, float peakSize)
        {
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();

            if ((frameSize + 0.025f) < peakSize)
            {
                obj.SetActive(true);
                var color = Color.HSVToRGB(h, 1f, 1f).ColorWithAlpha(0.8f);
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

            if (!PluginConfig.Instance.FrameVisualizer)
            {
                return;
            }

            this._audioSpectrum.fallSpeed = 1f;
            this._audioSpectrum.sensibility = 10f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;

            Shader _shader = VisualizerUtil.GetShader("Custom/Glowing");
            _frameMaterial = new Material(_shader);
            _frameMaterial.SetColor("_Color", Color.black.ColorWithAlpha(0f));

            _materialPropertyBlock = new MaterialPropertyBlock();
            visualizerColorID = Shader.PropertyToID("_Color");

            frameVisualizerRoot = new GameObject("frameVisualizerRoot");
            frameVisualizerRoot.transform.SetParent(FloorViewController.visualizerFloorRoot.transform, false);
            frameVisualizerRoot.transform.localPosition = new Vector3(0f, 0.0001f, 0f);

            for (int i = 0; i < size; i++)
            {
                GameObject child = GameObject.CreatePrimitive(PrimitiveType.Cube);
                child.transform.SetParent(frameVisualizerRoot.transform, false);
                child.SetActive(false);

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
                        cubeTransform.localPosition = new Vector3(0f, 0f, 1f);
                        break;
                    case (int)FramePosition.Back:
                        cubeTransform.localPosition = new Vector3(0f, 0f, -1f);
                        break;
                    case (int)FramePosition.Left:
                        cubeTransform.localPosition = new Vector3(-1.5f, 0f, 0f);
                        break;
                    case (int)FramePosition.Right:
                        cubeTransform.localPosition = new Vector3(1.5f, 0f, 0f);
                        break;
                    default:
                        break;
                }
            }

            for (int j = 0; j < size; j++)
            {
                var clone = Clone(frameVisualizerRoot);
                for (int r = 0; r < clone.transform.childCount; r++)
                {
                    var childObj = clone.transform.GetChild(r).gameObject;
                    childObj.SetActive(true);
                    var meshRenderer = childObj.GetComponent<MeshRenderer>();
                    meshRenderer.material = _frameMaterial;
                }
                cubes.Add(clone);
            }
        }

        public GameObject Clone(GameObject go)
        {
            var clone = GameObject.Instantiate(go) as GameObject;
            clone.transform.parent = go.transform.parent;
            clone.transform.localPosition = go.transform.localPosition;
            clone.transform.localScale = go.transform.localScale;
            return clone;
        }

        private bool _disposedValue;
        private AudioSpectrum _audioSpectrum;

        [Inject]
        public void Constructor([Inject(Id = AudioSpectrum.BandType.FourBand)]AudioSpectrum audioSpectrum)
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
