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
    public class UneUneVisualizerController : IInitializable, IDisposable
    {
        private int size = 31;

        private List<GameObject> uneuneLeftObjects = new List<GameObject>(31);
        private List<GameObject> uneuneRightObjects = new List<GameObject>(31);
        private List<Material> uneuneLeftMaterials = new List<Material>(31);
        private List<Material> uneuneRightMaterials = new List<Material>(31);

        private GameObject uneuneRoot = new GameObject("uneuneVisualizerRoot");

        private float leftHSV;
        private float rightHSV;
        private float[] s_shift = new float[31];
        private float updateTime = 0;
        /// <summary>
        /// 波形をずらす秒数の閾値(sec)
        /// </summary>
        /// <remarks>設定ファイルに逃がしてもいいし、曲のBPMと連動させてもいい</remarks>
        private static readonly float s_updateThresholdTime = 0.025f;


        private void OnUpdatedRawSpectrums(AudioSpectrum31 obj)
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }

            this.UpdateAudioSpectrums(obj);
        }

        private void UpdateAudioSpectrums(AudioSpectrum31 audio)
        {
            this.updateTime += Time.deltaTime;
            var needUpdate = s_updateThresholdTime < updateTime;
            if (!audio)
            {
                return;
            }

            float tmp = this._audioSpectrum.PeakLevels[7] * 50f;

            for (int i = 0; i < size; i++)
            {
                float timeSize = _timeSource.songTime + (float)(i + 1) / size * Mathf.PI;
                float amplitude = Mathf.Cos(timeSize) * 3f + (i * 0.05f);
                var alpha = this._audioSpectrum.PeakLevels[8] * 10f % 1f;
                int index = 30 - i;
                if (needUpdate) {
                    try {
                        if (index > 0) {
                            this.s_shift[index] = this.s_shift[index - 1];
                        }
                        else {
                            this.s_shift[index] = tmp;
                        }
                    }
                    catch (Exception e) {
                        Plugin.Log.Debug(e);
                    }
                }

                uneuneLeftMaterials[i].SetColor("_Color", Color.HSVToRGB(this.leftHSV, 1f, Lighting(alpha, 1f)).ColorWithAlpha(Lighting(alpha, 0.9f)));
                uneuneRightMaterials[i].SetColor("_Color", Color.HSVToRGB(this.rightHSV, 1f, Lighting(alpha, 1f)).ColorWithAlpha(Lighting(alpha, 0.9f)));
                UneUne(uneuneLeftObjects[i], index, alpha, amplitude);
                UneUne(uneuneRightObjects[i], index, alpha, amplitude);
            }

            if (needUpdate) {
                updateTime = 0;
            }
        }

        private void UneUne(GameObject obj, int index, float alpha, float amplitude)
        {
            
            obj.transform.localScale = new Vector3(0.5f + alpha, 0.2f + this.s_shift[index], 0.5f);

            var position = obj.transform.localPosition;
            obj.transform.localPosition = new Vector3(position.x, amplitude, position.z);
        }

        private float Lighting(float alpha, float withAlpha)
        {
            return 0.13f < alpha ? withAlpha : 0f;
        }



        public void Initialize()
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }


            this._audioSpectrum.Band = AudioSpectrum31.BandType.ThirtyOneBand;
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

            GameObject leftUneUne = new GameObject("leftUneUne");
            GameObject rightUneUne = new GameObject("rightUneUne");


            for (int i = 0; i < size; i++)
            {
                Material uneuneLeftMaterial = new Material(Shader.Find("Custom/Glowing"));
                uneuneLeftMaterial.SetColor("_Color", new Color(1f, 1f, 1f).ColorWithAlpha(1f));
                uneuneLeftMaterial.SetFloat("_EnableColorInstancing", 1f);
                uneuneLeftMaterial.SetFloat("_WhiteBoostType", 1f);
                uneuneLeftMaterial.SetFloat("_NoiseDithering", 1f);
                uneuneLeftMaterials.Add(uneuneLeftMaterial);

                Material uneuneRightMaterial = new Material(Shader.Find("Custom/Glowing"));
                uneuneRightMaterial.SetColor("_Color", new Color(1f, 1f, 1f).ColorWithAlpha(1f));
                uneuneRightMaterial.SetFloat("_EnableColorInstancing", 1f);
                uneuneRightMaterial.SetFloat("_WhiteBoostType", 1f);
                uneuneRightMaterial.SetFloat("_NoiseDithering", 1f);
                uneuneRightMaterials.Add(uneuneRightMaterial);

                GameObject uneuneLeftObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                MeshRenderer uneuneLeftMeshRenderer = uneuneLeftObject.GetComponent<MeshRenderer>();
                uneuneLeftMeshRenderer.material = uneuneLeftMaterials[i];
                uneuneLeftObject.transform.SetParent(leftUneUne.transform);
                uneuneLeftObject.transform.localScale = new Vector3(0.5f, 0.2f, 0.5f);
                uneuneLeftObject.transform.localPosition = new Vector3(-1.5f - (i * 0.25f), 0f, (size - i) * 2.5f + 1.2f);
                uneuneLeftObject.transform.localRotation = Quaternion.Euler(0f, 0f, 25f + (i * 0.6f + 0.5f));
                uneuneLeftObjects.Add(uneuneLeftObject);

                GameObject uneuneRightObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                MeshRenderer uneuneRightMeshRenderer = uneuneRightObject.GetComponent<MeshRenderer>();
                uneuneRightMeshRenderer.material = uneuneRightMaterials[i];
                uneuneRightObject.transform.SetParent(rightUneUne.transform);
                uneuneRightObject.transform.localScale = new Vector3(0.5f, 0.2f, 0.5f);
                uneuneRightObject.transform.localPosition = new Vector3(1.5f + (i * 0.25f), 0f, (size - i) * 2.5f + 1.2f);
                uneuneRightObject.transform.localRotation = Quaternion.Euler(0f, 0f, -25f - (i * 0.6f + 0.5f));
                uneuneRightObjects.Add(uneuneRightObject);
            }

            leftUneUne.transform.SetParent(uneuneRoot.transform);
            leftUneUne.transform.position = new Vector3(-5f, 2.5f, 7f);
            //leftUneUne.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            rightUneUne.transform.SetParent(uneuneRoot.transform);
            rightUneUne.transform.position = new Vector3(5f, 2.5f, 7f);
            //rightUneUne.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);

            if (PluginConfig.Instance.isFloorHeight)
            {
                var rootPosition = uneuneRoot.transform.localPosition;
                rootPosition.y = PluginConfig.Instance.floorHeight * 0.01f;
                uneuneRoot.transform.localPosition = rootPosition;
            }

            this.uneuneRoot.transform.SetParent(Utilities.FloorAdjustorUtil.NullponSpectrumFloor.transform);
        }

        private bool _disposedValue;
        private IAudioTimeSource _timeSource;
        private ColorScheme _colorScheme;
        private AudioSpectrum31 _audioSpectrum;

        [Inject]
        public void Constructor(IAudioTimeSource source, ColorScheme scheme, AudioSpectrum31 audioSpectrum)
        {
            this._timeSource = source;
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
