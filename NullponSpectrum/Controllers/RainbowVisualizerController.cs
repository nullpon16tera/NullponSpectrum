using NullponSpectrum.Configuration;
using NullponSpectrum.AudioSpectrums;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using System.Linq;

namespace NullponSpectrum.Controllers
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
    public class RainbowVisualizerController : IInitializable, IDisposable
    {
        private int size = 28;

        private Material _material;
        private MaterialPropertyBlock _materialPropertyBlock;
        private int visualizerTintColorID;
        private int visualizerBrightnessID;

        private List<GameObject> leftObject = new List<GameObject>(28);
        private List<GameObject> rightObject = new List<GameObject>(28);

        private GameObject leftFloorRoot = new GameObject("ponponLeftFloorRoot");
        private GameObject rightFloorRoot = new GameObject("ponponRightFloorRoot");

        private GameObject ponponRoot = new GameObject("ponponVisualizerRoot");

        private float[] s_shift = new float[28];
        private float updateTime = 0;
        /// <summary>
        /// 波形をずらす秒数の閾値(sec)
        /// </summary>
        /// <remarks>設定ファイルに逃がしてもいいし、曲のBPMと連動させてもいい</remarks>
        private static readonly float s_updateThresholdTime = 0.025f;

        public enum FramePosition
        {
            Front,
            Back,
            Left,
            Right,
        };

        private float Nomalize(float f)
        {
            var result = Mathf.Lerp(6f, 0f, f);
            return f * result;
        }

        private void OnUpdatedRawSpectrums(AudioSpectrum31 obj)
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }
            if (!PluginConfig.Instance.RainbowVisualizer)
            {
                return;
            }

            this.UpdateAudioSpectrums(obj);
        }

        private void UpdateAudioSpectrums(AudioSpectrum31 audio)
        {
            this.updateTime += Time.deltaTime;
            var bpmSpeed = -(this.Currentmap.level.beatsPerMinute * 0.00001f);
            var needUpdate = (s_updateThresholdTime + bpmSpeed) < updateTime;
            if (!audio)
            {
                return;
            }

            

            for (int i = 0; i < size; i++)
            {
                var alpha = this._audioSpectrum.PeakLevels[6] * 10f % 1f;
                int index = 27 - i;
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
                            this.s_shift[index] = alpha;
                        }
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.Debug(e);
                    }
                }
                float tmp = Mathf.Lerp(0f, 1.5f, this._audioSpectrum.PeakLevels[27 - i] * 6f);

                UneUne(leftObject[i], this.s_shift, index, 0.002f, this.Nomalize(tmp));
                UneUne(rightObject[i], this.s_shift, index, 0.002f, this.Nomalize(tmp));
            }

            if (needUpdate)
            {
                updateTime = 0;
            }
        }

        private void UneUne(GameObject obj, float[] h, int index, float min, float tmp)
        {
            var scale = obj.transform.localScale;
            obj.transform.localScale = new Vector3(scale.x, tmp, scale.z);

            var child = obj.transform.GetChild(0).gameObject;
            if (child != null)
            {
                MeshRenderer renderer = child.GetComponent<MeshRenderer>();
                if (min < tmp)
                {
                    obj.SetActive(true);
                    var color = Color.HSVToRGB(h[index], 1f, 1f).ColorWithAlpha(0.8f);
                    _materialPropertyBlock.SetColor(visualizerTintColorID, color);
                    _materialPropertyBlock.SetFloat(visualizerBrightnessID, 1f);
                    renderer.SetPropertyBlock(_materialPropertyBlock);
                }
                else
                {
                    obj.SetActive(false);
                    var color = Color.HSVToRGB(h[index], 1f, 0f).ColorWithAlpha(0f);
                    _materialPropertyBlock.SetColor(visualizerTintColorID, color);
                    _materialPropertyBlock.SetFloat(visualizerBrightnessID, 0f);
                    renderer.SetPropertyBlock(_materialPropertyBlock);
                }
            }
        }

        public void Initialize()
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }
            if (!PluginConfig.Instance.RainbowVisualizer)
            {
                return;
            }


            this._audioSpectrum.Band = AudioSpectrum31.BandType.ThirtyOneBand;
            this._audioSpectrum.fallSpeed = 1f;
            this._audioSpectrum.sensibility = 10f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;


            // Custom/Glowing Pointer
            // Custom/GlowingInstancedHD
            // Custom/ObstacleCoreLW

            CreateFrameObject();
            CreateMainObject();


            if (PluginConfig.Instance.isFloorHeight)
            {
                var rootPosition = ponponRoot.transform.localPosition;
                rootPosition.y = PluginConfig.Instance.floorHeight * 0.01f;
                ponponRoot.transform.localPosition = rootPosition;
            }

            this.ponponRoot.transform.SetParent(Utilities.FloorAdjustorUtil.NullponSpectrumFloor.transform, false);
        }

        private void CreateMainObject()
        {
            _material = new Material(Shader.Find("Custom/SaberBlade"));
            _material.SetColor("_TintColor", Color.red.ColorWithAlpha(1f));
            _material.SetFloat("_Brightness", 0f);

            _materialPropertyBlock = new MaterialPropertyBlock();
            visualizerTintColorID = Shader.PropertyToID("_TintColor");
            visualizerBrightnessID = Shader.PropertyToID("_Brightness");

            leftFloorRoot.transform.SetParent(null, false);
            leftFloorRoot.transform.localScale = Vector3.one;
            leftFloorRoot.transform.localPosition = new Vector3(-1.5f, 0.01f, 0f);
            leftFloorRoot.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            rightFloorRoot.transform.SetParent(ponponRoot.transform, false);
            rightFloorRoot.transform.localScale = Vector3.one;
            rightFloorRoot.transform.localPosition = new Vector3(1.5f, 0.01f, 0f);
            rightFloorRoot.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);


            for (int i = 0; i < size; i++)
            {
                GameObject parentObjectLeft = new GameObject("ponponParentObjectLeft");
                parentObjectLeft.transform.SetParent(leftFloorRoot.transform, false);
                GameObject childObjectLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
                childObjectLeft.transform.SetParent(parentObjectLeft.transform, false);

                parentObjectLeft.transform.localScale = new Vector3(0.05f, 1f, 0.0001f);
                parentObjectLeft.transform.localPosition = new Vector3(-0.975f + (i * 0.0725f), 0f, 0f);
                parentObjectLeft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                childObjectLeft.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                childObjectLeft.transform.localScale = Vector3.one;
                MeshRenderer childMeshRenderer = childObjectLeft.GetComponent<MeshRenderer>();
                childMeshRenderer.material = _material;
                leftObject.Add(parentObjectLeft);


                GameObject parentObjectRight = new GameObject("ponponParentObjectRight");
                parentObjectRight.transform.SetParent(rightFloorRoot.transform, false);
                GameObject childObjectRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
                childObjectRight.transform.SetParent(parentObjectRight.transform, false);

                parentObjectRight.transform.localScale = new Vector3(0.05f, 1f, 0.0001f);
                parentObjectRight.transform.localPosition = new Vector3(0.975f - (i * 0.0725f), 0f, 0f);
                parentObjectRight.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                
                childObjectRight.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                childObjectRight.transform.localScale = Vector3.one;
                MeshRenderer childRightMeshRenderer = childObjectRight.GetComponent<MeshRenderer>();
                childRightMeshRenderer.material = _material;
                rightObject.Add(parentObjectRight);
            }
        }

        private void CreateFloorObject()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            MeshRenderer floorRenderer = floor.GetComponent<MeshRenderer>();
            floorRenderer.material = new Material(Shader.Find("Custom/SaberBlade"));
            floorRenderer.material.SetColor("_TintColor", Color.black.ColorWithAlpha(0f));
            floorRenderer.material.SetFloat("_Brightness", 0f);

            floor.transform.localScale = new Vector3(0.3f, 0.01f, 0.2f);
            floor.transform.localPosition = new Vector3(0f, 0.005f, 0f);
            floor.transform.SetParent(ponponRoot.transform);
        }

        private void CreateFrameObject()
        {
            GameObject parent = new GameObject("ponponVisualizerFrame");

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
            parent.transform.SetParent(ponponRoot.transform);
        }

        private bool _disposedValue;
        public IDifficultyBeatmap Currentmap { get; private set; }
        private AudioSpectrum31 _audioSpectrum;

        [Inject]
        public void Constructor(IDifficultyBeatmap level, AudioSpectrum31 audioSpectrum)
        {
            this.Currentmap = level;
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
