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
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
    public class FrameVisualizerController : IInitializable, IDisposable
    {
        private float scale = 2f;
        private int size = 4;

        private List<GameObject> cubes = new List<GameObject>(4);
        private List<Material> _materials = new List<Material>(4);
        private GameObject frameRoot = new GameObject("frameVisualizerRoot");

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
            if (!audio)
            {
                return;
            }

            var bandType = this._audioSpectrum.Band;


            for (int i = 0; i < cubes.Count; i++)
            {
                int j = i;
                if (bandType != AudioSpectrum.BandType.FourBand)
                {
                    j = i + 6;
                }
                var peak = this._audioSpectrum.PeakLevels[j] * scale;
                var frameSize = 0.25f + ((size - i) * 0.2f) + (peak);
                cubes[i].transform.localScale = new Vector3(frameSize, 1f, frameSize);

                var alpha = (this._audioSpectrum.PeakLevels[j] * size) % 1f;
                var alphaLerp = Mathf.Lerp(0f, 1f, alpha * 30f);
                var colorLerp = Mathf.Lerp(0.45f, 1f, alpha);
                var color = Color.HSVToRGB(colorLerp, 1f, alphaLerp);
                _materials[i].SetColor("_Color", color.ColorWithAlpha(0.01f + alpha));
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

            this._audioSpectrum.Band = AudioSpectrum.BandType.FourBand;
            this._audioSpectrum.fallSpeed = 1f;
            this._audioSpectrum.sensibility = 10f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;

            GameObject parent = new GameObject("framePlaySpace");

            for (int r = 0; r < 4; r++)
            {
                Material material = new Material(Shader.Find("Custom/Glowing"));
                material.SetColor("_Color", Color.black.ColorWithAlpha(0f));
                material.SetFloat("_EnableColorInstancing", 1f);
                material.SetFloat("_WhiteBoostType", 1f);
                material.SetFloat("_NoiseDithering", 1f);
                _materials.Add(material);
            }

            for (int i = 0; i < size; i++)
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
                parent.transform.SetParent(frameRoot.transform);
            }

            for (int j = 0; j < size; j++)
            {
                var clone = Clone(parent);
                for (int r = 0; r < clone.transform.childCount; r++)
                {
                    var childObj = clone.transform.GetChild(r).gameObject;
                    var meshRenderer = childObj.GetComponent<MeshRenderer>();
                    meshRenderer.material = _materials[j];
                }
                cubes.Add(clone);
            }

            foreach (GameObject obj in cubes)
            {
                obj.SetActive(obj);
            }
            
            this.frameRoot.transform.SetParent(VMCAvatarUtil.NullponSpectrumFloor.transform);
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
        public void Constructor(AudioSpectrum audioSpectrum)
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
