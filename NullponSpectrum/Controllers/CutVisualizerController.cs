using NullponSpectrum.Configuration;
using NullponSpectrum.Utilities;
using System;
using UnityEngine;
using Zenject;

namespace NullponSpectrum.Controllers
{
    /// <summary>
    /// ノーツを斬ったとき、床ローカルで幅 3・奥行 1.5 の矩形内にパーティクルを Emit する。
    /// 見た目は <see cref="ParticleVisualizerController"/> と同じホタル風（発光テクスチャ・Additive・床面スライド）。
    /// </summary>
	public class CutVisualizerController : IInitializable, IDisposable
    {
        /// <summary>長方形の半幅（全幅 3）</summary>
        private const float RectHalfWidth = 1.5f;
        /// <summary>長方形の半奥行（全奥行 1.5）</summary>
        private const float RectHalfDepth = 0.75f;
        /// <summary>ParticleVisualizer と同じ発生 Y</summary>
        private const float EmitY = 0.02f;

        /// <summary>1 カットあたりの Emit 数の下限</summary>
        private const int CutEmitMin = 14;
        /// <summary>1 カットあたりの Emit 数の上限</summary>
        private const int CutEmitMax = 56;

        /// <summary>ParticleVisualizer に合わせた粒サイズ</summary>
        private const float ParticleSizeMin = 0.03f;
        private const float ParticleSizeMax = 0.064f;
        private const float LifetimeMin = 0.15f;
        private const float LifetimeMax = 0.45f;
        private const float PlaneSpeedMin = 0.12f;
        private const float PlaneSpeedMax = 0.95f;

        private GameObject _root;
        private ParticleSystem _particles;
        private ParticleSystemRenderer _particleRenderer;
        private Material _particleMaterial;
        private Texture2D _particleGlowTexture;
        /// <summary>ノートカット通知。古いビルドは Main の ScoreController、新しめは GameplayCore の BeatmapObjectManager に載っている。</summary>
        private BeatmapObjectManager _beatmapObjectManager;
        private VisualizerUtil _visualizerUtil;

        [Inject]
        public void Constructor(BeatmapObjectManager beatmapObjectManager, VisualizerUtil visualizerUtil)
        {
            this._beatmapObjectManager = beatmapObjectManager;
            this._visualizerUtil = visualizerUtil;
        }

        public void Initialize()
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }

            if (!PluginConfig.Instance.CutVisualizer)
            {
                return;
            }

            _root = new GameObject("cutSparkVisualizer");
            _root.transform.SetParent(FloorViewController.visualizerFloorRoot.transform, false);
            _root.transform.localPosition = Vector3.zero;
            _root.transform.localScale = Vector3.one;

            BuildCutParticleSystem();
            _beatmapObjectManager.noteWasCutEvent += OnNoteWasCut;
        }

        private void OnNoteWasCut(NoteController noteController, in NoteCutInfo noteCutInfo)
        {
            if (!PluginConfig.Instance.Enable || !PluginConfig.Instance.CutVisualizer)
            {
                return;
            }

            if (_particles == null)
            {
                return;
            }

            if (!(noteController is GameNoteController))
            {
                return;
            }

            if (!(noteController.noteData is NoteData blockData))
            {
                return;
            }

            float speedNorm = Mathf.Clamp01(noteCutInfo.saberSpeed / 42f);
            float okMul = noteCutInfo.allIsOK ? 1f : 0.62f;
            float hitIntensity = Mathf.Clamp01(0.35f + 0.65f * speedNorm) * okMul;

            int count = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(CutEmitMin, CutEmitMax, hitIntensity)), CutEmitMin, CutEmitMax);
            if (XrPerfHelper.ShouldReduceVisualizerCost())
            {
                count = Mathf.Max(CutEmitMin, Mathf.RoundToInt(count * 0.62f));
            }

            EmitCutPlaneBurst(count, hitIntensity, blockData.colorType);
        }

        /// <summary>ParticleVisualizer.EmitPlaneBurst と同系。ノーツ色を基調に矩形内・床面 XZ で Emit。</summary>
        private void EmitCutPlaneBurst(int count, float hitIntensity, ColorType noteColorType)
        {
            this._visualizerUtil.RefreshSaberColorsNow();

            float[] hsv = noteColorType == ColorType.ColorA
                ? VisualizerUtil.GetLeftSaberHSV()
                : VisualizerUtil.GetRightSaberHSV();
            Color noteColor = Color.HSVToRGB(hsv[0], hsv[1], 1f);

            float pint = Mathf.Clamp01(hitIntensity);

            if (!_particles.isPlaying)
            {
                _particles.Play();
            }

            for (int i = 0; i < count; i++)
            {
                float t = UnityEngine.Random.value;
                Color c = Color.Lerp(noteColor, Color.white, Mathf.Lerp(0.12f, 0.38f, t));
                c.a = Mathf.Lerp(0.65f, 0.95f, pint);

                float x = UnityEngine.Random.Range(-RectHalfWidth, RectHalfWidth);
                float z = UnityEngine.Random.Range(-RectHalfDepth, RectHalfDepth);
                Vector3 pos = new Vector3(x, EmitY, z);

                Vector2 dir2 = UnityEngine.Random.insideUnitCircle;
                if (dir2.sqrMagnitude < 1e-6f)
                {
                    dir2 = Vector2.right;
                }
                dir2.Normalize();

                float speed = Mathf.Lerp(PlaneSpeedMin, PlaneSpeedMax, pint * UnityEngine.Random.Range(0.85f, 1.1f));
                speed *= Mathf.Lerp(0.55f, 1f, pint);
                Vector3 vel = new Vector3(dir2.x * speed, 0f, dir2.y * speed);

                float life = UnityEngine.Random.Range(LifetimeMin, LifetimeMax);

                var emitParams = new ParticleSystem.EmitParams
                {
                    position = pos,
                    startLifetime = life,
                    startSize = Mathf.Lerp(ParticleSizeMin, ParticleSizeMax, pint * UnityEngine.Random.Range(0.9f, 1.05f)),
                    velocity = vel,
                    startColor = c,
                    applyShapeToPosition = false
                };

                _particles.Emit(emitParams, 1);
            }
        }

        /// <summary>ParticleVisualizer.BuildParticleSystem と同じ方針（ホタル用テクスチャ・グラデーション）。</summary>
        private void BuildCutParticleSystem()
        {
            var go = new GameObject("cutFireflyParticles");
            go.transform.SetParent(_root.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            _particles = go.AddComponent<ParticleSystem>();
            var ps = _particles;

            var main = ps.main;
            main.playOnAwake = true;
            main.loop = true;
            main.duration = 5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(LifetimeMin, LifetimeMax);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(ParticleSizeMin, ParticleSizeMax);
            main.maxParticles = 420;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.gravityModifier = 0f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 0f);

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.enabled = false;

            var sizeOl = ps.sizeOverLifetime;
            sizeOl.enabled = true;
            AnimationCurve shrink = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
            sizeOl.size = new ParticleSystem.MinMaxCurve(1f, shrink);

            var colorOl = ps.colorOverLifetime;
            colorOl.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 0.55f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.92f, 0.45f), new GradientAlphaKey(0f, 1f) });
            colorOl.color = g;

            Shader shader = VisualizerUtil.GetShader("Particles/Additive")
                ?? VisualizerUtil.GetShader("Legacy Shaders/Particles/Additive")
                ?? VisualizerUtil.GetShader("Mobile/Particles/Additive")
                ?? VisualizerUtil.GetShader("Sprites/Default");
            _particleRenderer = go.GetComponent<ParticleSystemRenderer>();
            _particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            _particleRenderer.alignment = ParticleSystemRenderSpace.Facing;
            if (shader != null)
            {
                _particleMaterial = new Material(shader);
                _particleMaterial.renderQueue = 3200;
                _particleGlowTexture = CreateFireflyGlowTexture(64);
                _particleMaterial.mainTexture = _particleGlowTexture;
                _particleRenderer.material = _particleMaterial;
            }

            ps.Clear();
            ps.Play();

            if (XrPerfHelper.ShouldReduceVisualizerCost())
            {
                var mainCap = ps.main;
                mainCap.maxParticles = 300;
            }
        }

        /// <summary>ParticleVisualizer と同じ円形発光アルファテクスチャ。</summary>
        private static Texture2D CreateFireflyGlowTexture(int size = 64)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "NullponCutFireflyGlow";
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            float inv = 1f / (size - 1);
            float invSqrt2 = 1f / Mathf.Sqrt(2f);

            for (int y = 0; y < size; y++)
            {
                float fy = y * inv * 2f - 1f;
                for (int x = 0; x < size; x++)
                {
                    float fx = x * inv * 2f - 1f;
                    float d = Mathf.Sqrt(fx * fx + fy * fy);
                    float r = Mathf.Clamp01(d * invSqrt2);
                    float core = Mathf.Exp(-5.2f * r * r);
                    float halo = 0.42f * Mathf.Exp(-1.35f * r * r);
                    float a = Mathf.Clamp01(core + halo);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            tex.Apply(false, true);
            return tex;
        }

        private bool _disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposedValue)
            {
                if (disposing)
                {
                    if (this._beatmapObjectManager != null)
                    {
                        this._beatmapObjectManager.noteWasCutEvent -= OnNoteWasCut;
                    }

                    if (_root != null)
                    {
                        UnityEngine.Object.Destroy(_root);
                        _root = null;
                    }

                    if (_particleMaterial != null)
                    {
                        UnityEngine.Object.Destroy(_particleMaterial);
                        _particleMaterial = null;
                    }

                    if (_particleGlowTexture != null)
                    {
                        UnityEngine.Object.Destroy(_particleGlowTexture);
                        _particleGlowTexture = null;
                    }

                    _particles = null;
                    _particleRenderer = null;
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
