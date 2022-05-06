using NullponSpectrum.Configuration;
using NullponSpectrum.AudioSpectrums;
using System;
using System.Linq;
using UnityEngine;
using Zenject;
using System.Collections.Generic;

namespace NullponSpectrum.Controllers
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
    public class FrameVisualizerController : IInitializable, IDisposable
    {
        private float scale = 2f;
        private bool _disposedValue;
        private int size = 4;

        private List<GameObject> cubes = new List<GameObject>(4);
        private List<Material> _materials = new List<Material>(4);
        private GameObject frameRoot;

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


            for (int i = 0; i < cubes.Count; i++)
            {
                var peak = this._audioSpectrum.PeakLevels[i] * scale;
                var frameSize = 0.25f + ((size - i) * 0.2f) + (peak * 1.1f);
                cubes[i].transform.localScale = new Vector3(frameSize, 1f, frameSize);

                var alpha = (this._audioSpectrum.PeakLevels[i] * size) % 1f;
                var alphaLerp = Mathf.Lerp(0f, 1f, alpha * 30f);
                var colorLerp = Mathf.Lerp(0.38f, 1f, alpha + alpha);
                var color = Color.HSVToRGB(colorLerp, alphaLerp, alphaLerp);
                _materials[i].SetColor("_Color", color.ColorWithAlpha(0.25f + alpha));
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
            this._audioSpectrum.fallSpeed = 0.3f;
            this._audioSpectrum.sensibility = 10f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;


            this.frameRoot = new GameObject("frameVisualizerRoot");
            GameObject parent = new GameObject("framePlaySpace");

            for (int r = 0; r < 4; r++)
            {
                Material material = new Material(Shader.Find("Custom/Glowing"));
                material.SetColor("_Color  ", Color.white.ColorWithAlpha(1f));
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

            string[] array = new string[]
            {
                "Environment/PlayersPlace",
                "CustomPlatforms"
            };
            GameObject[] source = Resources.FindObjectsOfTypeAll<GameObject>();
            string[] array2 = array;
            for (int i = 0; i < array2.Length; i++)
            {
                string floorObjectName = array2[i];
                GameObject gameObject = (from o in source
                                         where o.GetFullPath(false) == floorObjectName
                                         select o).FirstOrDefault<GameObject>();
                if (gameObject)
                {
                    Plugin.Log.Debug("AdjustFloorHeight: " + floorObjectName + " found.");
                    Vector3 a;
                    if (this._initialPositions.TryGetValue(gameObject, out a))
                    {
                        Plugin.Log.Debug("AdjustFloorHeight: Found initial position " + a.ToString("F3"));
                        gameObject.transform.localPosition = a;
                    }
                    else
                    {
                        Plugin.Log.Debug("AdjustFloorHeight: Register initial position " + gameObject.transform.localPosition.ToString("F3"));
                        this._initialPositions[gameObject] = gameObject.transform.localPosition;
                        this.frameRoot.transform.localPosition = gameObject.transform.localPosition;
                        Plugin.Log.Debug("FrameVisualizer AdjustFloorHeight " + this.frameRoot.transform.localPosition.ToString("F3"));
                    }
                }

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

        private Dictionary<GameObject, Vector3> _initialPositions = new Dictionary<GameObject, Vector3>();

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
