using NullponSpectrum.AudioSpectrums;
using NullponSpectrum.Configuration;
using NullponSpectrum.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace NullponSpectrum.Controllers
{
    /// <summary>
    /// メニュー用スポットライト柱（床→天井、床で細く天井で広い円錐台）。
    /// <see cref="SpotlightRingRadius"/> の円上に、<see cref="MenuStageVisualizerController"/> と同様に
    /// 左半円 <see cref="SpotlightPerHalfCount"/> 本 + 右半円同数（計 <see cref="SpotlightCount"/> 本）。
    /// 円の大きさは <see cref="SpotlightRingRadius"/> のみ変更（柱メッシュのローカルスケールは固定）。
    /// </summary>
    public class MenuSpotlightVisualizerController : IInitializable, ILateTickable, IDisposable
    {
        /// <summary>片側の本数（左半円 + 右半円で同数）。</summary>
        private const int SpotlightPerHalfCount = 10;

        private const int SpotlightCount = SpotlightPerHalfCount * 2;

        /// <summary>スペクトラムのバンド数（<see cref="AudioSpectrum.BandType.TenBand"/> と一致）。</summary>
        private const int SpectrumBandCount = 10;

        /// <summary>メニュー専用: ピークのなめらか追従（ふんわり明滅）。</summary>
        private const float MenuSpotlightPeakSmoothHz = 5.2f;

        /// <summary>見た目の明るさ・透明度用。音が出た瞬間にいきなり最大にならないよう、ピークより遅く追従する。</summary>
        private const float MenuSpotlightDisplaySmoothHz = 2.0f;

        /// <summary>無音時の可視性が落ちるスピード（小さいほどフワッと消える）。入るときは VisibilityFadeInHz。</summary>
        private const float MenuSpotlightVisibilityFadeOutHz = 3.2f;

        /// <summary>音あり時に可視性が上がるスピード。</summary>
        private const float MenuSpotlightVisibilityFadeInHz = 16f;

        /// <summary>HSV から求めた表示色・明るさのイージング（大きいほど素早く追従）。</summary>
        private const float MenuSpotlightColorEaseHz = 5.5f;

        private const float MenuSpotlightFallSpeed = 0.062f;

        private const float MenuSpotlightGeometryScale = 1.5f * 1.25f;

        /// <summary>
        /// 円配置の半径（XZ）。ここだけ変えれば円の広がりだけ変わり、柱の太さ・高さ（ローカルスケール）はそのまま。
        /// </summary>
        private const float SpotlightRingRadius = 3f * MenuSpotlightGeometryScale;

        /// <summary>柱メッシュの高さ（Unity 単位）。円半径とは独立。</summary>
        private const float BeamHeight = 10.5f;

        /// <summary>柱の水平スケール（円柱プリミティブの xz）。円半径とは独立。</summary>
        private const float BeamRadiusScale = 1.5f;

        /// <summary>この正規化高さまでは縦フェードを掛けない（0=床 1=天井）。0.5 で「中間〜天井」で消える。</summary>
        private const float MenuSpotlightVertFadeFrom = 0.5f;

        /// <summary>上方向フェードの曲げ。マテリアル初期化用。</summary>
        private const float MenuSpotlightVertFadeGamma = 1f;

        /// <summary>これ未満（peak に 12 倍したあと）なら光を消す。</summary>
        private const float SilenceRevealThreshold = 0.028f;

        /// <summary>中心方向↔外向きへ往復する傾き（度）。</summary>
        private const float TiltMaxDegrees = 11f;

        /// <summary>内↔外の往復 1 周期の秒数。</summary>
        private const float TiltCycleSeconds = 6.5f;

        /// <summary>隣り合う柱の位相差（ラジアン換算の係数）。</summary>
        private const float TiltPhasePerIndex = 0.42f;

        private readonly float[] _smoothedPeaks = new float[SpotlightCount];
        private readonly float[] _visibilitySmoothed = new float[SpotlightCount];
        private readonly float[] _peakDisplay = new float[SpotlightCount];
        private readonly Color[] _smoothedTint = new Color[SpotlightCount];
        private readonly float[] _smoothedBrightness = new float[SpotlightCount];

        private Material _beamMaterial;
        private Material _dustMaterial;
        private MaterialPropertyBlock _propertyBlock;
        private int _tintColorId;
        private int _brightnessId;

        private GameObject _root;
        private readonly List<GameObject> _beamObjects = new List<GameObject>(SpotlightCount);
        private readonly List<ParticleSystem> _dustSystems = new List<ParticleSystem>(SpotlightCount);
        private bool _built;

        private AudioSpectrum _audioSpectrum;

        private void UpdateVisuals()
        {
            if (this._audioSpectrum == null || !this._built)
            {
                return;
            }

            float[] levels = this._audioSpectrum.PeakLevels;
            if (levels == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            float smoothT = 1f - Mathf.Exp(-MenuSpotlightPeakSmoothHz * Mathf.Max(dt, 1e-5f));
            float displaySmoothT = 1f - Mathf.Exp(-MenuSpotlightDisplaySmoothHz * Mathf.Max(dt, 1e-5f));
            float colorEaseT = 1f - Mathf.Exp(-MenuSpotlightColorEaseHz * Mathf.Max(dt, 1e-5f));
            float tiltOmega = (Mathf.PI * 2f) / Mathf.Max(0.25f, TiltCycleSeconds);

            for (int i = 0; i < SpotlightCount; i++)
            {
                int band = BandIndexForSpotlight(i);
                float raw = band < levels.Length ? levels[band] : 0f;
                this._smoothedPeaks[i] = Mathf.Lerp(this._smoothedPeaks[i], raw, smoothT);
            }

            var needUpdate = Utilities.VisualizerUtil.GetNeedUpdate() || PluginConfig.Instance.MenuSpotlightVisualizer;

            for (int i = 0; i < SpotlightCount; i++)
            {
                float peak = Mathf.Clamp01(this._smoothedPeaks[i] * 12f);
                float targetVis = peak > SilenceRevealThreshold ? 1f : 0f;
                float visFadeHz = targetVis > this._visibilitySmoothed[i] ? MenuSpotlightVisibilityFadeInHz : MenuSpotlightVisibilityFadeOutHz;
                float visSmooth = 1f - Mathf.Exp(-visFadeHz * Mathf.Max(dt, 1e-5f));
                this._visibilitySmoothed[i] = Mathf.Lerp(this._visibilitySmoothed[i], targetVis, visSmooth);
                float vis = this._visibilitySmoothed[i] * this._visibilitySmoothed[i];
                this._peakDisplay[i] = Mathf.Lerp(this._peakDisplay[i], peak, displaySmoothT);

                GameObject beamGo = this._beamObjects[i];
                Transform beamTr = beamGo.transform;
                Transform pivotTr = beamTr.parent;
                Transform itemTr = pivotTr != null ? pivotTr.parent : beamTr.parent;
                Vector3 radialOut = itemTr != null ? itemTr.localPosition : Vector3.forward;
                radialOut.y = 0f;
                if (radialOut.sqrMagnitude < 1e-8f)
                {
                    radialOut = Vector3.forward;
                }
                else
                {
                    radialOut.Normalize();
                }

                Vector3 tangent = Vector3.Cross(Vector3.up, radialOut).normalized;
                float tiltAngle = Mathf.Sin((Time.time * tiltOmega) + (i * TiltPhasePerIndex)) * TiltMaxDegrees;
                if (pivotTr != null)
                {
                    pivotTr.localRotation = Quaternion.AngleAxis(tiltAngle, tangent);
                }
                else
                {
                    beamTr.localRotation = Quaternion.AngleAxis(tiltAngle, tangent);
                }

                var beam = beamGo.GetComponent<MeshRenderer>();
                if (beam == null)
                {
                    continue;
                }

                float display = Mathf.Clamp01(this._peakDisplay[i] * vis);
                float soft = Mathf.Pow(display, 1.12f);
                float hue = (display * 7.5f + i * 0.02f) % 1f;
                float value = 0.28f + 0.62f * Mathf.Pow(soft, 0.9f);
                var rgb = Color.HSVToRGB(hue, 0.88f, value);
                float alpha = (0.04f + 0.38f * soft) * vis;
                var targetCol = rgb.ColorWithAlpha(alpha);
                float targetBrightness = (0.02f + 0.68f * soft) * vis;

                this._smoothedTint[i] = Color.Lerp(this._smoothedTint[i], targetCol, colorEaseT);
                this._smoothedBrightness[i] = Mathf.Lerp(this._smoothedBrightness[i], targetBrightness, colorEaseT);

                bool fullyOff = vis < 0.002f && this._smoothedTint[i].a < 0.002f && this._smoothedBrightness[i] < 0.002f;
                if (fullyOff)
                {
                    this._propertyBlock.SetColor(this._tintColorId, Color.black.ColorWithAlpha(0f));
                    this._propertyBlock.SetFloat(this._brightnessId, 0f);
                    beam.SetPropertyBlock(this._propertyBlock);

                    var dustOff = this._dustSystems[i];
                    if (dustOff != null)
                    {
                        var e0 = dustOff.emission;
                        e0.rateOverTime = 0f;
                    }

                    continue;
                }

                this._propertyBlock.SetColor(this._tintColorId, this._smoothedTint[i]);
                this._propertyBlock.SetFloat(this._brightnessId, this._smoothedBrightness[i]);
                beam.SetPropertyBlock(this._propertyBlock);

                var dust = this._dustSystems[i];
                if (dust != null)
                {
                    float rate = (0.8f + this._peakDisplay[i] * 8f) * vis * soft;
                    var e = dust.emission;
                    e.rateOverTime = rate;
                    var dMain = dust.main;
                    float dustA = (0.15f + 0.4f * soft) * vis;
                    dMain.startColor = new Color(this._smoothedTint[i].r, this._smoothedTint[i].g, this._smoothedTint[i].b, dustA);
                }
            }

            if (needUpdate)
            {
                Utilities.VisualizerUtil.ResetUpdateTime();
            }
        }

        /// <summary>
        /// 左半円 i=0..perHalf-1、右半円 i=perHalf..2*perHalf-1。対称位置は同じバンド index。
        /// Stage の角度順に対し、バンド番号を反転（奥(-Z)＝高音、手前(+Z)＝低音）。
        /// </summary>
        private static int BandIndexForSpotlight(int i)
        {
            int j = i < SpotlightPerHalfCount ? i : i - SpotlightPerHalfCount;
            int maxBand = SpectrumBandCount - 1;
            int band = Mathf.Min(maxBand, (j * maxBand) / Mathf.Max(1, SpotlightPerHalfCount - 1));
            return maxBand - band;
        }

        /// <summary>
        /// <see cref="MenuStageVisualizerController"/> の BandAngleRadians と同一（角度の並びは Stage と同じ）。
        /// </summary>
        private static float BandAngleRadians(int col, int columnsPerHalf)
        {
            if (columnsPerHalf <= 1)
            {
                return Mathf.PI;
            }

            int d = columnsPerHalf - 1;
            if (col < columnsPerHalf)
            {
                return Mathf.PI + Mathf.PI * col / d;
            }

            int jj = col - columnsPerHalf;
            return Mathf.PI - Mathf.PI * jj / d;
        }

        public void Initialize()
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }

            this._audioSpectrum.Band = AudioSpectrum.BandType.TenBand;
            this._audioSpectrum.fallSpeed = MenuSpotlightFallSpeed;
            this._audioSpectrum.sensibility = 10f;
        }

        public void LateTick()
        {
            if (!PluginConfig.Instance.Enable)
            {
                if (this._built)
                {
                    this.TearDown();
                }

                return;
            }

            bool want = PluginConfig.Instance.MenuSpotlightVisualizer;
            if (want && !this._built)
            {
                this.Build();
            }
            else if (!want && this._built)
            {
                this.TearDown();
            }

            if (!want || !this._built)
            {
                return;
            }

            this.UpdateVisuals();
        }

        private void Build()
        {
            if (this._built)
            {
                return;
            }

            if (FloorAdjustorUtil.NullponSpectrumFloor == null)
            {
                return;
            }

            Shader beamShader = VisualizerUtil.GetShader("Custom/MenuSpotlightBeam")
                ?? VisualizerUtil.GetShader("Custom/SaberBlade");
            if (beamShader == null)
            {
                return;
            }

            this._beamMaterial = new Material(beamShader);
            // 0: 縦の t によるグラデが主になる。>0 だとカメラ距離でピクセルごとに明るさが変わり縦グラデが潰れやすい
            this._beamMaterial.SetFloat("_CameraDistanceAtten", 0f);
            this._beamMaterial.SetFloat("_VertFadeFrom", MenuSpotlightVertFadeFrom);
            this._beamMaterial.SetFloat("_VertFadeGamma", MenuSpotlightVertFadeGamma);
            this._propertyBlock = new MaterialPropertyBlock();
            this._tintColorId = Shader.PropertyToID("_TintColor");
            this._brightnessId = Shader.PropertyToID("_Brightness");

            Shader dustShader = VisualizerUtil.GetShader("Particles/Additive")
                ?? VisualizerUtil.GetShader("Legacy Shaders/Particles/Additive")
                ?? VisualizerUtil.GetShader("Mobile/Particles/Additive");
            if (dustShader != null)
            {
                this._dustMaterial = new Material(dustShader);
            }

            this._root = new GameObject("menuSpotlightVisualizerRoot");
            this._root.transform.SetParent(FloorAdjustorUtil.NullponSpectrumFloor.transform, false);
            this._root.transform.localPosition = Vector3.zero;
            this._root.transform.localRotation = Quaternion.identity;
            this._root.transform.localScale = Vector3.one;

            for (int s = 0; s < SpotlightCount; s++)
            {
                this._smoothedPeaks[s] = 0f;
                this._visibilitySmoothed[s] = 0f;
                this._peakDisplay[s] = 0f;
                this._smoothedTint[s] = Color.black;
                this._smoothedBrightness[s] = 0f;
            }

            this._beamObjects.Clear();
            this._dustSystems.Clear();

            for (int i = 0; i < SpotlightCount; i++)
            {
                float theta = BandAngleRadians(i, SpotlightPerHalfCount);
                float x = Mathf.Sin(theta) * SpotlightRingRadius;
                float z = Mathf.Cos(theta) * SpotlightRingRadius;
                var pos = new Vector3(x, 0f, z);

                var item = new GameObject($"menuSpotlight_{i}");
                item.transform.SetParent(this._root.transform, false);
                item.transform.localPosition = pos;
                item.transform.localRotation = Quaternion.identity;
                item.transform.localScale = Vector3.one;

                var beamPivot = new GameObject("beamPivot");
                beamPivot.transform.SetParent(item.transform, false);
                beamPivot.transform.localPosition = Vector3.zero;
                beamPivot.transform.localRotation = Quaternion.identity;
                beamPivot.transform.localScale = Vector3.one;

                var beamGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                beamGo.name = "beam";
                beamGo.transform.SetParent(beamPivot.transform, false);
                beamGo.transform.localPosition = new Vector3(0f, BeamHeight * 0.5f, 0f);
                beamGo.transform.localRotation = Quaternion.identity;
                beamGo.transform.localScale = new Vector3(BeamRadiusScale, BeamHeight * 0.5f, BeamRadiusScale);

                var collider = beamGo.GetComponent<Collider>();
                if (collider != null)
                {
                    UnityEngine.Object.Destroy(collider);
                }

                var mr = beamGo.GetComponent<MeshRenderer>();
                mr.material = this._beamMaterial;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;

                this._beamObjects.Add(beamGo);

                if (this._dustMaterial != null)
                {
                    var dustGo = new GameObject("dust");
                    dustGo.transform.SetParent(beamPivot.transform, false);
                    dustGo.transform.localPosition = new Vector3(0f, BeamHeight * 0.35f, 0f);
                    var ps = dustGo.AddComponent<ParticleSystem>();
                    this.SetupDustParticles(ps);
                    var pr = dustGo.GetComponent<ParticleSystemRenderer>();
                    pr.material = this._dustMaterial;
                    pr.renderMode = ParticleSystemRenderMode.Billboard;
                    this._dustSystems.Add(ps);
                }
                else
                {
                    this._dustSystems.Add(null);
                }
            }

            this._audioSpectrum.fallSpeed = MenuSpotlightFallSpeed;
            this._audioSpectrum.sensibility = 10f;

            this._built = true;
        }

        private void SetupDustParticles(ParticleSystem ps)
        {
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 2.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.028f);
            main.maxParticles = 28;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.gravityModifier = -0.02f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            var emission = ps.emission;
            emission.rateOverTime = 3.5f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.11f;
            shape.radiusThickness = 0.85f;
            shape.arc = 360f;
            shape.rotation = new Vector3(90f, 0f, 0f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
            vel.y = new ParticleSystem.MinMaxCurve(0.05f, 0.28f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            noise.frequency = 0.65f;
            noise.scrollSpeed = 0.25f;
            noise.damping = true;
            noise.quality = ParticleSystemNoiseQuality.Medium;

            var sizeOl = ps.sizeOverLifetime;
            sizeOl.enabled = true;
            var shrink = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
            sizeOl.size = new ParticleSystem.MinMaxCurve(1f, shrink);

            var colOl = ps.colorOverLifetime;
            colOl.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.55f, 0.2f),
                    new GradientAlphaKey(0.35f, 0.75f),
                    new GradientAlphaKey(0f, 1f)
                });
            colOl.color = g;
        }

        private void TearDown()
        {
            if (!this._built)
            {
                return;
            }

            if (this._root != null)
            {
                UnityEngine.Object.Destroy(this._root);
                this._root = null;
            }

            if (this._beamMaterial != null)
            {
                UnityEngine.Object.Destroy(this._beamMaterial);
                this._beamMaterial = null;
            }

            if (this._dustMaterial != null)
            {
                UnityEngine.Object.Destroy(this._dustMaterial);
                this._dustMaterial = null;
            }

            this._beamObjects.Clear();
            this._dustSystems.Clear();
            this._built = false;
        }

        private bool _disposedValue;

        [Inject]
        public void Constructor([Inject(Id = AudioSpectrum.BandType.TenBand)] AudioSpectrum audioSpectrum)
        {
            this._audioSpectrum = audioSpectrum;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposedValue)
            {
                if (disposing)
                {
                    this.TearDown();
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
