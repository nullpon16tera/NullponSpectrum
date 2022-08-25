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
    public class PonPonVisualizerController : IInitializable, IDisposable
    {
        private int size = 50;

        private Material _material;
        private MaterialPropertyBlock _materialPropertyBlock;
        private int visualizerTintColorID;
        private int visualizerBrightnessID;

        private List<GameObject> leftObject1 = new List<GameObject>(50);
        private List<GameObject> rightObject1 = new List<GameObject>(50);
        private List<GameObject> leftObject2 = new List<GameObject>(50);
        private List<GameObject> rightObject2 = new List<GameObject>(50);
        private List<GameObject> leftObject3 = new List<GameObject>(50);
        private List<GameObject> rightObject3 = new List<GameObject>(50);
        private List<GameObject> leftObject4 = new List<GameObject>(50);
        private List<GameObject> rightObject4 = new List<GameObject>(50);

        private GameObject leftChild1 = new GameObject("ponponLeftChild1");
        private GameObject rightChild1 = new GameObject("ponponRightChild1");
        private GameObject leftChild2 = new GameObject("ponponLeftChild2");
        private GameObject rightChild2 = new GameObject("ponponRightChild2");
        private GameObject leftChild3 = new GameObject("ponponLeftChild3");
        private GameObject rightChild3 = new GameObject("ponponRightChild3");
        private GameObject leftChild4 = new GameObject("ponponLeftChild4");
        private GameObject rightChild4 = new GameObject("ponponRightChild4");

        private GameObject ponponRoot = new GameObject("ponponVisualizerRoot");

        private float leftHSV;
        private float rightHSV;
        private float[] s_shift = new float[50];
        private float[] s_shift2 = new float[50];
        private float[] s_shift3 = new float[50];
        private float[] s_shift4 = new float[50];
        private float updateTime = 0;
        /// <summary>
        /// 波形をずらす秒数の閾値(sec)
        /// </summary>
        /// <remarks>設定ファイルに逃がしてもいいし、曲のBPMと連動させてもいい</remarks>
        private static readonly float s_updateThresholdTime = 0.025f;


        private void OnUpdatedRawSpectrums(AudioSpectrum8 obj)
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }
            if (!PluginConfig.Instance.PonPonVisualizer)
            {
                return;
            }

            this.UpdateAudioSpectrums(obj);
        }

        private void UpdateAudioSpectrums(AudioSpectrum8 audio)
        {
            this.updateTime += Time.deltaTime;
            var bpmSpeed = -(this.Currentmap.level.beatsPerMinute * 0.00001f);
            var needUpdate = (s_updateThresholdTime + bpmSpeed) < updateTime;
            if (!audio)
            {
                return;
            }

            float tmp = this._audioSpectrum.PeakLevels[0] * 10f;
            float tmp2 = this._audioSpectrum.PeakLevels[2] * 20f;
            float tmp3 = this._audioSpectrum.PeakLevels[4] * 25f;
            float tmp4 = this._audioSpectrum.PeakLevels[6] * 30f;

            for (int i = 0; i < size; i++)
            {
                //var alpha = this._audioSpectrum.PeakLevels[6] * 10f % 1f;
                int index = 49 - i;
                if (needUpdate)
                {
                    try
                    {
                        if (index > 0)
                        {
                            this.s_shift[index] = this.s_shift[index - 1];
                            this.s_shift2[index] = this.s_shift2[index - 1];
                            this.s_shift3[index] = this.s_shift3[index - 1];
                            this.s_shift4[index] = this.s_shift4[index - 1];
                        }
                        else
                        {
                            this.s_shift[index] = tmp;
                            this.s_shift2[index] = tmp2;
                            this.s_shift3[index] = tmp3;
                            this.s_shift4[index] = tmp4;
                        }
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.Debug(e);
                    }
                }

                UneUne(leftObject1[i], this.leftHSV, index, true, 0.2f, this.s_shift);
                UneUne(rightObject1[i], this.rightHSV, index, false, 0.2f, this.s_shift);
                UneUne(leftObject2[i], this.leftHSV, index, true, 0.2f, this.s_shift2);
                UneUne(rightObject2[i], this.rightHSV, index, false, 0.2f, this.s_shift2);
                UneUne(leftObject3[i], this.leftHSV, index, true, 0.2f, this.s_shift3);
                UneUne(rightObject3[i], this.rightHSV, index, false, 0.2f, this.s_shift3);
                UneUne(leftObject4[i], this.leftHSV, index, true, 0.2f, this.s_shift4);
                UneUne(rightObject4[i], this.rightHSV, index, false, 0.2f, this.s_shift4);
            }

            if (needUpdate)
            {
                updateTime = 0;
            }
        }

        private void UneUne(GameObject obj, float h, int index, bool left, float min, float[] shift)
        {
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            //obj.transform.localScale = new Vector3(0.5f + alpha, 0.2f + this.s_shift[index], 0.5f);
            var pos = obj.transform.localPosition;
            if (left)
            {
                obj.transform.localPosition = new Vector3(0f, shift[index], pos.z);
            }
            else
            {
                obj.transform.localPosition = new Vector3(0f, shift[index], pos.z);
            }

            if (min < shift[index])
            {
                obj.SetActive(true);
                var color = Color.HSVToRGB(h, 1f, 1f).ColorWithAlpha(0.9f);
                _materialPropertyBlock.SetColor(visualizerTintColorID, color);
                _materialPropertyBlock.SetFloat(visualizerBrightnessID, 1f);
                renderer.SetPropertyBlock(_materialPropertyBlock);
            }
            else
            {
                obj.SetActive(false);
                var color = Color.HSVToRGB(h, 1f, 0f).ColorWithAlpha(0f);
                _materialPropertyBlock.SetColor(visualizerTintColorID, color);
                _materialPropertyBlock.SetFloat(visualizerBrightnessID, 0f);
                renderer.SetPropertyBlock(_materialPropertyBlock);
            }

            //var position = obj.transform.localPosition;
            //obj.transform.localPosition = new Vector3(position.x, amplitude, position.z);
        }

        public void Initialize()
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }
            if (!PluginConfig.Instance.PonPonVisualizer)
            {
                return;
            }


            this._audioSpectrum.Band = AudioSpectrum8.BandType.EightBand;
            this._audioSpectrum.fallSpeed = 1f;
            this._audioSpectrum.sensibility = 10f;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;

            // セイバーの色取得
            float leftH, leftS, leftV;
            float rightH, rightS, rightV;

            Color.RGBToHSV(this._colorScheme.saberAColor, out leftH, out leftS, out leftV);
            Color.RGBToHSV(this._colorScheme.saberBColor, out rightH, out rightS, out rightV);
            this.leftHSV = leftH;
            this.rightHSV = rightH;


            // Custom/Glowing Pointer
            // Custom/GlowingInstancedHD
            // Custom/ObstacleCoreLW

            _material = new Material(Shader.Find("Custom/SaberBlade"));
            _material.SetColor("_TintColor", Color.black.ColorWithAlpha(1f));
            _material.SetFloat("_Brightness", 0f);

            _materialPropertyBlock = new MaterialPropertyBlock();
            visualizerTintColorID = Shader.PropertyToID("_TintColor");
            visualizerBrightnessID = Shader.PropertyToID("_Brightness");

            var scale = new Vector3(0.2f, 0.2f, 0.2f);

            for (int i = 0; i < size; i++)
            {
                // Band 6
                GameObject objLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
                MeshRenderer leftMeshRenderer = objLeft.GetComponent<MeshRenderer>();
                leftMeshRenderer.material = _material;
                objLeft.transform.SetParent(leftChild1.transform);
                objLeft.transform.localScale = scale;
                objLeft.transform.localPosition = new Vector3(0f, 0f, (size - i) * 1.5f + 1.2f);
                leftObject1.Add(objLeft);

                GameObject objRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
                MeshRenderer rightMeshRenderer = objRight.GetComponent<MeshRenderer>();
                rightMeshRenderer.material = _material;
                objRight.transform.SetParent(rightChild1.transform);
                objRight.transform.localScale = scale;
                objRight.transform.localPosition = new Vector3(0f, 0f, (size - i) * 1.5f + 1.2f);
                rightObject1.Add(objRight);

                // Band 12
                GameObject objLeft2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                MeshRenderer leftMeshRenderer2 = objLeft2.GetComponent<MeshRenderer>();
                leftMeshRenderer2.material = _material;
                objLeft2.transform.SetParent(leftChild2.transform);
                objLeft2.transform.localScale = scale;
                objLeft2.transform.localPosition = new Vector3(0f, 0f, (size - i) * 1.5f + 1.2f);
                leftObject2.Add(objLeft2);

                GameObject objRight2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                MeshRenderer rightMeshRenderer2 = objRight2.GetComponent<MeshRenderer>();
                rightMeshRenderer2.material = _material;
                objRight2.transform.SetParent(rightChild2.transform);
                objRight2.transform.localScale = scale;
                objRight2.transform.localPosition = new Vector3(0f, 0f, (size - i) * 1.5f + 1.2f);
                rightObject2.Add(objRight2);

                // Band 18
                GameObject objLeft3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                MeshRenderer leftMeshRenderer3 = objLeft3.GetComponent<MeshRenderer>();
                leftMeshRenderer3.material = _material;
                objLeft3.transform.SetParent(leftChild3.transform);
                objLeft3.transform.localScale = scale;
                objLeft3.transform.localPosition = new Vector3(0f, 0f, (size - i) * 1.5f + 1.2f);
                leftObject3.Add(objLeft3);

                GameObject objRight3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                MeshRenderer rightMeshRenderer3 = objRight3.GetComponent<MeshRenderer>();
                rightMeshRenderer3.material = _material;
                objRight3.transform.SetParent(rightChild3.transform);
                objRight3.transform.localScale = scale;
                objRight3.transform.localPosition = new Vector3(0f, 0f, (size - i) * 1.5f + 1.2f);
                rightObject3.Add(objRight3);

                // Band 24
                GameObject objLeft4 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                MeshRenderer leftMeshRenderer4 = objLeft4.GetComponent<MeshRenderer>();
                leftMeshRenderer4.material = _material;
                objLeft4.transform.SetParent(leftChild4.transform);
                objLeft4.transform.localScale = scale;
                objLeft4.transform.localPosition = new Vector3(0f, 0f, (size - i) * 1.5f + 1.2f);
                leftObject4.Add(objLeft4);

                GameObject objRight4 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                MeshRenderer rightMeshRenderer4 = objRight4.GetComponent<MeshRenderer>();
                rightMeshRenderer4.material = _material;
                objRight4.transform.SetParent(rightChild4.transform);
                objRight4.transform.localScale = scale;
                objRight4.transform.localPosition = new Vector3(0f, 0f, (size - i) * 1.5f + 1.2f);
                rightObject4.Add(objRight4);
            }



            leftChild1.transform.SetParent(ponponRoot.transform);
            leftChild1.transform.position = new Vector3(-3.5f, 0f, 5f);
            //leftUneUne.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            rightChild1.transform.SetParent(ponponRoot.transform);
            rightChild1.transform.position = new Vector3(3.5f, 0f, 5f);
            //rightUneUne.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);


            leftChild2.transform.SetParent(ponponRoot.transform);
            leftChild2.transform.position = new Vector3(-3.8f, 0f, 5f);
            //leftChild2.transform.localRotation = Quaternion.Euler(-3.5f, -3f, 0f);

            rightChild2.transform.SetParent(ponponRoot.transform);
            rightChild2.transform.position = new Vector3(3.8f, 0f, 5f);
            //rightChild2.transform.localRotation = Quaternion.Euler(-3.5f, 3f, 0f);

            leftChild3.transform.SetParent(ponponRoot.transform);
            leftChild3.transform.position = new Vector3(-4.12f, 0f, 5f);
            //leftChild3.transform.localRotation = Quaternion.Euler(-6f, -6.2f, 0f);

            rightChild3.transform.SetParent(ponponRoot.transform);
            rightChild3.transform.position = new Vector3(4.12f, 0f, 5f);
            //rightChild3.transform.localRotation = Quaternion.Euler(-6f, 6.2f, 0f);

            leftChild4.transform.SetParent(ponponRoot.transform);
            leftChild4.transform.position = new Vector3(-4.47f, 0f, 5f);
            //leftChild4.transform.localRotation = Quaternion.Euler(-9f, -9f, 0f);

            rightChild4.transform.SetParent(ponponRoot.transform);
            rightChild4.transform.position = new Vector3(4.47f, 0f, 5f);
            //rightChild4.transform.localRotation = Quaternion.Euler(-9f, 9f, 0f);

            if (PluginConfig.Instance.isFloorHeight)
            {
                var rootPosition = ponponRoot.transform.localPosition;
                rootPosition.y = PluginConfig.Instance.floorHeight * 0.01f;
                ponponRoot.transform.localPosition = rootPosition;
            }

            this.ponponRoot.transform.SetParent(Utilities.FloorAdjustorUtil.NullponSpectrumFloor.transform);
        }

        private bool _disposedValue;
        private IAudioTimeSource _timeSource;
        public IDifficultyBeatmap Currentmap { get; private set; }
        private ColorScheme _colorScheme;
        private AudioSpectrum8 _audioSpectrum;

        [Inject]
        public void Constructor(IAudioTimeSource source, IDifficultyBeatmap level, ColorScheme scheme, AudioSpectrum8 audioSpectrum)
        {
            this._timeSource = source;
            this.Currentmap = level;
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
