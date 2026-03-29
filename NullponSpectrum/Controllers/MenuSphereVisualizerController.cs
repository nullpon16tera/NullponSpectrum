using NullponSpectrum.Configuration;
using NullponSpectrum.AudioSpectrums;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using NullponSpectrum.Utilities;

namespace NullponSpectrum.Controllers
{
    /// <summary>
    /// メニュー用 Sphere ビジュアライザ。床メッシュ用の <see cref="MenuFloorRootController"/> ではなく、
    /// <see cref="FloorAdjustorUtil.NullponSpectrumFloor"/> 直下にルートを置く（床高スライダー用の menuVisualizerFloorRoot には載せない）。
    /// <see cref="PluginConfig.MenuSphereVisualizer"/> は <see cref="LateTick"/> で同期する。
    /// </summary>
    public class MenuSphereVisualizerController : IInitializable, ILateTickable, IDisposable
    {
        private int size = 31;

        /// <summary>メニュー専用: ピーク追従をなめらかに（見た目の「ふんわり」は動き側）。</summary>
        private const float MenuSpherePeakSmoothHz = 5.5f;

        /// <summary>ゲーム用 Sphere よりピークの減衰を遅くし、動きを穏やかに。</summary>
        private const float MenuSphereFallSpeed = 0.065f;

        private readonly float[] _smoothedPeaks = new float[31];

        private Material _sphereMaterial;
        private MaterialPropertyBlock _materialPropertyBlock;
        private int visualizerTintColorID;
        private int visualizerBrightnessID;

        private List<GameObject> leftSpheres = new List<GameObject>(31);
        private List<GameObject> rightSpheres = new List<GameObject>(31);
        private List<Vector3> leftSphereVector = new List<Vector3>(31);
        private List<Vector3> rightSphereVector = new List<Vector3>(31);

        private GameObject _sphereRoot;
        private bool _built;

        private void OnUpdatedRawSpectrums(AudioSpectrum obj)
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }
            if (!PluginConfig.Instance.MenuSphereVisualizer)
            {
                return;
            }
            this.UpdateAudioSpectrums(obj);
        }

        private void UpdateAudioSpectrums(AudioSpectrum audio)
        {
            // メニューでは HasAnyActiveSpectrumFloorVisualizer が false のため GetNeedUpdate が進まない。Menu 用スフィアでは色も毎フレーム更新する。
            var needUpdate = Utilities.VisualizerUtil.GetNeedUpdate() || PluginConfig.Instance.MenuSphereVisualizer;
            if (!audio)
            {
                return;
            }

            float dt = Time.deltaTime;
            float smoothT = 1f - Mathf.Exp(-MenuSpherePeakSmoothHz * Mathf.Max(dt, 1e-5f));

            for (int i = 0; i < size; i++)
            {
                float raw = this._audioSpectrum.PeakLevels[i];
                this._smoothedPeaks[i] = Mathf.Lerp(this._smoothedPeaks[i], raw, smoothT);
            }

            for (int i = 0; i < size; i++)
            {
                float peak = this._smoothedPeaks[i];
                var alpha = (peak * 10f) % 1f;
                var positionSize = peak * 5f;

                var leftSphere = leftSpheres[i].transform;
                var leftPos = leftSphereVector[i];
                leftSphere.localPosition = new Vector3((leftPos.x + positionSize), (leftPos.y + positionSize), (leftPos.z + positionSize));
                leftSphere.localScale = new Vector3(0.05f, peak * (5f + leftPos.z), peak * (5f + leftPos.z));

                var rightSphere = rightSpheres[i].transform;
                var rightPos = rightSphereVector[i];
                rightSphere.localPosition = new Vector3((rightPos.x - positionSize), (rightPos.y + positionSize), (rightPos.z + positionSize));
                rightSphere.localScale = new Vector3(0.05f, peak * (5f + rightPos.z), peak * (5f + rightPos.z));
                if (needUpdate)
                {
                    ChangeMaterialProperty(leftSpheres[i], positionSize, alpha);
                    ChangeMaterialProperty(rightSpheres[i], positionSize, alpha);
                }
            }

            if (needUpdate)
            {
                Utilities.VisualizerUtil.ResetUpdateTime();
            }

        }

        private void ChangeMaterialProperty(GameObject obj, float size, float alpha)
        {
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if (0.05f < size)
            {
                var color = Color.HSVToRGB(alpha, 1f, 1f).ColorWithAlpha(0.6f);
                _materialPropertyBlock.SetColor(visualizerTintColorID, color);
                _materialPropertyBlock.SetFloat(visualizerBrightnessID, 1f);
                renderer.SetPropertyBlock(_materialPropertyBlock);
            }
            else
            {
                var color = Color.HSVToRGB(alpha, 1f, 0f).ColorWithAlpha(0f);
                _materialPropertyBlock.SetColor(visualizerTintColorID, color);
                _materialPropertyBlock.SetFloat(visualizerBrightnessID, 0f);
                renderer.SetPropertyBlock(_materialPropertyBlock);
            }
        }

        public void Initialize()
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }

            this._audioSpectrum.Band = AudioSpectrum.BandType.ThirtyOneBand;
            this._audioSpectrum.fallSpeed = MenuSphereFallSpeed;
            this._audioSpectrum.sensibility = 10f;
        }

        /// <summary>メニュー設定のオンオフに合わせ、球群の生成と破棄を毎フレーム同期する。</summary>
        public void LateTick()
        {
            if (!PluginConfig.Instance.Enable)
            {
                if (this._built)
                {
                    this.TearDownMenuSphereVisuals();
                }

                return;
            }

            bool want = PluginConfig.Instance.MenuSphereVisualizer;
            if (want && !this._built)
            {
                this.BuildMenuSphereVisuals();
            }
            else if (!want && this._built)
            {
                this.TearDownMenuSphereVisuals();
            }
        }

        private void BuildMenuSphereVisuals()
        {
            if (this._built)
            {
                return;
            }

            if (FloorAdjustorUtil.NullponSpectrumFloor == null)
            {
                return;
            }

            Shader shader = VisualizerUtil.GetShader("Custom/MenuSphereSoft")
                ?? VisualizerUtil.GetShader("Custom/SaberBlade");
            if (shader == null)
            {
                return;
            }

            _sphereMaterial = new Material(shader);
            _sphereMaterial.SetColor("_TintColor", Color.black.ColorWithAlpha(1f));
            _sphereMaterial.SetFloat("_Brightness", 0f);

            _materialPropertyBlock = new MaterialPropertyBlock();
            visualizerTintColorID = Shader.PropertyToID("_TintColor");
            visualizerBrightnessID = Shader.PropertyToID("_Brightness");

            this._sphereRoot = new GameObject("sphereVisualizerRoot");
            this._sphereRoot.transform.SetParent(FloorAdjustorUtil.NullponSpectrumFloor.transform, false);
            this._sphereRoot.transform.localPosition = Vector3.zero;
            this._sphereRoot.transform.localRotation = Quaternion.identity;
            this._sphereRoot.transform.localScale = Vector3.one;

            UnityEngine.Random.InitState(RandomSeed());

            for (int s = 0; s < size; s++)
            {
                this._smoothedPeaks[s] = 0f;
            }

            leftSpheres.Clear();
            rightSpheres.Clear();
            leftSphereVector.Clear();
            rightSphereVector.Clear();

            for (int i = 0; i < size; i++)
            {
                // Left Sphere GameObject create
                GameObject leftSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                MeshRenderer leftMeshRenderer = leftSphere.GetComponent<MeshRenderer>();
                leftMeshRenderer.material = _sphereMaterial;

                float randX = UnityEngine.Random.Range(5f, 15f);
                float randY = UnityEngine.Random.Range(-1f, 10f);
                float randZ = UnityEngine.Random.Range(8f, 35f);


                Transform leftSphereTransform = leftSphere.transform;
                leftSphereTransform.localPosition = new Vector3(-(randX), randY, randZ);
                leftSphereTransform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                leftSphereTransform.localRotation = Quaternion.Euler(-45f, 45f, 25f);
                leftSphereVector.Add(leftSphereTransform.localPosition);

                leftSphere.transform.SetParent(this._sphereRoot.transform);

                leftSpheres.Add(leftSphere);

                // Right Sphere GameObject create
                GameObject rightSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                MeshRenderer rightMeshRenderer = rightSphere.GetComponent<MeshRenderer>();
                rightMeshRenderer.material = _sphereMaterial;

                Transform rightSphereTransform = rightSphere.transform;
                rightSphereTransform.localPosition = new Vector3(randX, randY, randZ);
                rightSphereTransform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                rightSphereTransform.localRotation = Quaternion.Euler(-45f, -45f, -25f);
                rightSphereVector.Add(rightSphereTransform.localPosition);

                rightSphere.transform.SetParent(this._sphereRoot.transform);

                rightSpheres.Add(rightSphere);
            }

            this._audioSpectrum.fallSpeed = MenuSphereFallSpeed;
            this._audioSpectrum.sensibility = 10f;

            this._built = true;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;
        }

        private void TearDownMenuSphereVisuals()
        {
            if (!this._built)
            {
                return;
            }

            if (this._audioSpectrum != null)
            {
                this._audioSpectrum.UpdatedRawSpectrums -= this.OnUpdatedRawSpectrums;
            }

            if (this._sphereRoot != null)
            {
                UnityEngine.Object.Destroy(this._sphereRoot);
                this._sphereRoot = null;
            }

            if (this._sphereMaterial != null)
            {
                UnityEngine.Object.Destroy(this._sphereMaterial);
                this._sphereMaterial = null;
            }

            leftSpheres.Clear();
            rightSpheres.Clear();
            leftSphereVector.Clear();
            rightSphereVector.Clear();
            this._built = false;
        }

        public int RandomSeed()
        {
            System.Random rand = new System.Random();
            int next = rand.Next(0, 101);
            float bpm = 120f;
            var map = VisualizerUtil.Currentmap;
            if (map != null && map.beatmapLevel != null)
            {
                bpm = map.beatmapLevel.beatsPerMinute;
            }

            int seed = next * (int)bpm;
            return seed;
        }

        private bool _disposedValue;
        private AudioSpectrum _audioSpectrum;

        [Inject]
        public void Constructor([Inject(Id = AudioSpectrum.BandType.ThirtyOneBand)] AudioSpectrum audioSpectrum)
        {
            this._audioSpectrum = audioSpectrum;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposedValue)
            {
                if (disposing)
                {
                    this.TearDownMenuSphereVisuals();
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
