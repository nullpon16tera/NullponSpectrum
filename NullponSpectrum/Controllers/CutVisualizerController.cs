using NullponSpectrum.Configuration;
using NullponSpectrum.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace NullponSpectrum.Controllers
{
    /// <summary>
    /// ノーツを斬ったとき、床ローカルで幅 3・奥行 1.5 の矩形内にパーティクルを Emit する。
    /// 見た目は <see cref="ParticleVisualizerController"/> と同じホタル風（発光テクスチャ・Additive・床面スライド）。
    /// 粒パラメータは 3 ティア分 Initialize でベイクし、更新時はキュー＋色のみ。
    /// </summary>
    public class CutVisualizerController : IInitializable, IDisposable, ILateTickable
    {
        private const float RectHalfWidth = 1.5f;
        private const float RectHalfDepth = 0.75f;
        private const float EmitY = 0.02f;

        private const int CutEmitMin = 14;
        private const int CutEmitMax = 42;

        private const float ParticleSizeMin = 0.03f;
        private const float ParticleSizeMax = 0.064f;
        private const float LifetimeMin = 0.15f;
        private const float LifetimeMax = 0.45f;
        private const float PlaneSpeedMin = 0.12f;
        private const float PlaneSpeedMax = 0.95f;

        private const int CutTierCount = 3;
        private const int CutBakeMaxPerTier = 40;
        private const int EmitCutParticlesPerFrameBudget = 22;
        private const int CutEmitPendingQueueMax = 96;

        private struct CutBakedEmit
        {
            public Vector3 Velocity;
            public float StartLifetime;
            public float StartSize;
            public float WhiteMixT;
        }

        private struct PendingCutBurst
        {
            public int Tier;
            public ColorType NoteColorType;
            public int Emitted;
            public int Total;
        }

        private static readonly float[] CutTierPintRepr = { 0.38f, 0.58f, 0.94f };
        private static readonly int[] CutTierBaseCountRepr = { 16, 23, 30 };

        private GameObject _root;
        private ParticleSystem _particles;
        private ParticleSystemRenderer _particleRenderer;
        private Material _particleMaterial;
        private Texture2D _particleGlowTexture;
        private BeatmapObjectManager _beatmapObjectManager;
        private VisualizerUtil _visualizerUtil;

        private CutBakedEmit[][] _cutTierBaked;
        private int[] _cutTierEmitCounts;
        private readonly List<PendingCutBurst> _pendingCutBursts = new List<PendingCutBurst>();

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
            this._beatmapObjectManager.noteWasCutEvent += this.OnNoteWasCut;
        }

        public void LateTick()
        {
            if (!PluginConfig.Instance.Enable || !PluginConfig.Instance.CutVisualizer || this._particles == null)
            {
                return;
            }

            this.DrainCutEmitBudget();
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
            int tier = this.SelectCutTier(hitIntensity);
            this.EnqueueCutBurst(tier, blockData.colorType);
        }

        private int SelectCutTier(float hitIntensity)
        {
            if (hitIntensity < 0.36f)
            {
                return 0;
            }

            if (hitIntensity < 0.68f)
            {
                return 1;
            }

            return 2;
        }

        private void EnqueueCutBurst(int tier, ColorType noteColorType)
        {
            if (tier < 0 || tier >= CutTierCount || this._cutTierEmitCounts == null)
            {
                return;
            }

            int total = this._cutTierEmitCounts[tier];
            int pending = 0;
            for (int i = 0; i < this._pendingCutBursts.Count; i++)
            {
                PendingCutBurst b = this._pendingCutBursts[i];
                pending += b.Total - b.Emitted;
            }

            while (pending + total > CutEmitPendingQueueMax && this._pendingCutBursts.Count > 0)
            {
                PendingCutBurst drop = this._pendingCutBursts[0];
                pending -= drop.Total - drop.Emitted;
                this._pendingCutBursts.RemoveAt(0);
            }

            if (pending + total <= CutEmitPendingQueueMax)
            {
                this._pendingCutBursts.Add(new PendingCutBurst
                {
                    Tier = tier,
                    NoteColorType = noteColorType,
                    Emitted = 0,
                    Total = total
                });
            }
        }

        private void DrainCutEmitBudget()
        {
            if (this._particles == null || this._pendingCutBursts.Count == 0)
            {
                return;
            }

            int budget = EmitCutParticlesPerFrameBudget;
            while (budget > 0 && this._pendingCutBursts.Count > 0)
            {
                PendingCutBurst head = this._pendingCutBursts[0];
                int remain = head.Total - head.Emitted;
                int take = Mathf.Min(budget, remain);
                this.EmitCutBurstSlice(head.Tier, head.Emitted, take, head.NoteColorType);
                head.Emitted += take;
                budget -= take;
                if (head.Emitted >= head.Total)
                {
                    this._pendingCutBursts.RemoveAt(0);
                }
                else
                {
                    this._pendingCutBursts[0] = head;
                }
            }
        }

        private void EmitCutBurstSlice(int tier, int offset, int count, ColorType noteColorType)
        {
            if (count <= 0 || this._cutTierBaked == null || tier < 0 || tier >= CutTierCount)
            {
                return;
            }

            this._visualizerUtil.RefreshSaberColorsNow();
            float[] hsv = noteColorType == ColorType.ColorA
                ? VisualizerUtil.GetLeftSaberHSV()
                : VisualizerUtil.GetRightSaberHSV();
            Color noteColor = Color.HSVToRGB(hsv[0], hsv[1], 1f);
            float pint = Mathf.Clamp01(CutTierPintRepr[tier]);

            if (!this._particles.isPlaying)
            {
                this._particles.Play();
            }

            for (int i = 0; i < count; i++)
            {
                CutBakedEmit e = this._cutTierBaked[tier][offset + i];
                Color c = Color.Lerp(noteColor, Color.white, Mathf.Lerp(0.12f, 0.38f, e.WhiteMixT));
                c.a = Mathf.Lerp(0.65f, 0.95f, pint);

                float px = UnityEngine.Random.Range(-RectHalfWidth, RectHalfWidth);
                float pz = UnityEngine.Random.Range(-RectHalfDepth, RectHalfDepth);
                var emitParams = new ParticleSystem.EmitParams
                {
                    position = new Vector3(px, EmitY, pz),
                    startLifetime = e.StartLifetime,
                    startSize = e.StartSize,
                    velocity = e.Velocity,
                    startColor = c,
                    applyShapeToPosition = false
                };

                this._particles.Emit(emitParams, 1);
            }
        }

        private void BakeCutBurstTemplates()
        {
            this._cutTierBaked = new CutBakedEmit[CutTierCount][];
            this._cutTierEmitCounts = new int[CutTierCount];

            UnityEngine.Random.State previous = UnityEngine.Random.state;
            try
            {
                for (int tier = 0; tier < CutTierCount; tier++)
                {
                    UnityEngine.Random.InitState(99102031 + tier * 17003);
                    float pint = Mathf.Clamp01(CutTierPintRepr[tier]);
                    int scaledCount = Mathf.Clamp(CutTierBaseCountRepr[tier], CutEmitMin, CutEmitMax);
                    if (XrPerfHelper.ShouldReduceVisualizerCost())
                    {
                        scaledCount = Mathf.Max(CutEmitMin, Mathf.RoundToInt(scaledCount * 0.62f));
                    }

                    var arr = new CutBakedEmit[CutBakeMaxPerTier];
                    for (int i = 0; i < scaledCount; i++)
                    {
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

                        arr[i] = new CutBakedEmit
                        {
                            Velocity = vel,
                            StartLifetime = life,
                            StartSize = Mathf.Lerp(ParticleSizeMin, ParticleSizeMax, pint * UnityEngine.Random.Range(0.9f, 1.05f)),
                            WhiteMixT = UnityEngine.Random.value
                        };
                    }

                    this._cutTierBaked[tier] = arr;
                    this._cutTierEmitCounts[tier] = scaledCount;
                }
            }
            finally
            {
                UnityEngine.Random.state = previous;
            }
        }

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
            this.BakeCutBurstTemplates();
            ps.Play();

            if (XrPerfHelper.ShouldReduceVisualizerCost())
            {
                var mainCap = ps.main;
                mainCap.maxParticles = 300;
            }
        }

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
                        this._beatmapObjectManager.noteWasCutEvent -= this.OnNoteWasCut;
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
                    this._cutTierBaked = null;
                    this._cutTierEmitCounts = null;
                    this._pendingCutBursts.Clear();
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
