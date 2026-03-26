using NullponSpectrum.AudioSpectrums;
using NullponSpectrum.Configuration;
using NullponSpectrum.Utilities;
using System;
using UnityEngine;
using Zenject;

namespace NullponSpectrum.Controllers
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
    public class StageVisualizerController : IInitializable, IDisposable
    {
        /// <summary>
        /// 各バンド列の奥行き先頭（m=0）の四角の中心が乗る円の半径（XZ）。メッシュはこの値から径方向に伸ばすので、円の大きさはここを変える。
        /// </summary>
        private const float CircleRadius = 0.44f;
        /// <summary>
        /// 1 バンドあたり径方向の四角数。パーティクル無しの床メッシュは頂点数＝列×本数×4 で重いので 24→12 に削減。
        /// 手続き型火花は別オブジェクトの 4 頂点クワッドのまま。
        /// </summary>
        private const int SquaresPerBand = 12;
        /// <summary>各四角の半辺長（接線方向・径方向とも同じで正方形）</summary>
        private const float SquareHalf = 0.016f;
        /// <summary>
        /// バンド列の「中心から外へ」に並ぶ四角同士の、径方向の隙間（面と面）。円周方向の隙間ではない。
        /// 隣セルの中心間隔は <see cref="RadialPitch"/> ＝ 2×<see cref="SquareHalf"/> ＋この値なので、この値を下げると隙間がそのまま縮む。
        /// </summary>
        private const float GapRadial = 0.014f;

        /// <summary>
        /// 径方向の中心間隔（m と m+1）。旧実装は 24 段相当の最外周に合わせて約 (23/11) 倍していたため、
        /// <see cref="GapRadial"/> を変えても隙間がほとんど縮まなかった。今は 2×四角＋隙間の素直なピッチ。
        /// </summary>
        private static float RadialPitch => SquareHalf * 2f + GapRadial;

        /// <summary>
        /// Game 手続き型火花: キックは「平均より瞬間が跳ねる」（Levels−MeanLevels）＋「1 フレームの単帯跳ね」の両方でゲート。
        /// 閾値: StageSparkTransientOverMeanMin / StageSparkMaxBandJumpMin。キック代理 Peak index 9、補助フラックス 7〜11。
        /// </summary>
        private const float StageSparkTransientOverMeanMin = 0.028f;
        private const float StageSparkMaxBandJumpMin = 0.016f;
        private const int StageSparkKickProxyBandIndex = 9;
        private const float StageSparkKickBand8PeakRiseMin = 0.019f;
        private const float StageSparkKickBand8PeakMin = 0.044f;
        /// <summary>この未満までキック代理帯の PeakLevels が落ちたら再武装。</summary>
        private const float StageSparkKickPeakRearmBelow = 0.034f;
        private const int SparkDrumBandIndexFirst = 7;
        private const int SparkDrumBandIndexLast = 11;
        private const float StageSparkPercussiveMinBandEnergy = 0.038f;
        private const float StageSparkPercussiveFluxMin = 0.024f;
        private const float StageSparkPercussiveFluxOverAvgMin = 0.55f;
        private const float StageSparkPercussiveCrestMin = 1.45f;
        private const float StageSparkBurstMinIntervalSeconds = 0.24f;
        /// <summary>低/中/高ティアの境目。Menu より Stage だけ「高」が届きやすいよう MidHigh を下げる。</summary>
        private const float StageSparkTierBoundaryLowMid = 0.42f;
        /// <summary>高ティアは tierMetric がこの値以上。Menu の 0.58 より低くして強めのオンセットで高が選ばれやすくする。</summary>
        private const float StageSparkTierBoundaryMidHigh = 0.52f;
        /// <summary>hitIntensity を tier 用に持ち上げ（Menu 0.86 より大きめ）。</summary>
        private const float StageSparkTierSelectScale = 0.92f;
        /// <summary>1 に近いほど Pow で潰れず高ティアに届きやすい（Menu は 1.12）。</summary>
        private const float StageSparkTierCurveExponent = 1.02f;
        private const float StageSparkTierFluxIntensityMul = 1f;
        private const float StageSparkTierFluxIntensityCap = 0.28f;
        /// <summary>hitIntensity に入れる「平均レベル」側の係数（大きいほど静かなドラムでも強く反応）</summary>
        private const float SparkIntensityFromAvg = 3.85f;
        /// <summary>hitIntensity に入れる「前フレームからの跳ね」側の係数（大きいほどアタック感で強く反応）</summary>
        private const float SparkIntensityFromJump = 7.8f;

        /// <summary>1 バーストあたりの Emit 数の下限 — Menu ステージ火花と同一。</summary>
        private const int SparkEmitMin = 16;
        /// <summary>1 バーストあたりの Emit 数の上限</summary>
        private const int SparkEmitMax = 74;
        private const float SparkMaxRiseMeters = 0.6f;
        private const float SparkParticleSizeMin = 0.011f;
        private const float SparkParticleSizeMax = 0.16f;
        private const float SparkParticleSizeMidTierMaxMul = 0.78f;
        private const float SparkParticleSizeHighTierMaxMul = 0.88f;
        private const float SparkLifetimeMin = 0.1f;
        private const float SparkLifetimeMax = 0.28f;
        private const float StageSparkBakeLifetimeMin = 0.15f;
        private const float StageSparkBakeLifetimeMax = 0.42f;
        private const float StageSparkBakeLifetimeMidTierExtraMul = 1.14f;
        private const float StageSparkBakeLifetimeHighTierExtraMul = 1.32f;
        private const float SparkGravityEffective = 9.81f * 0.35f;
        private const float SparkBurstCountScaleMin = 1f;
        private const float SparkBurstCountScaleMax = 2.25f;
        private const float SparkBurstSpreadMulMin = 0.48f;
        private const float SparkBurstSpreadMulMax = 1.84f;
        private const float SparkBurstUpBlendMin = 1.25f;
        private const float SparkBurstShapeRadiusMulMin = 0.64f;
        private const float SparkBurstShapeRadiusMulMax = 1.84f;
        private const float SparkBurstLifeMulMin = 0.9f;
        private const float SparkBurstLifeMulMax = 1.06f;
        private const int SparkTierCount = 3;
        private const int SparkBakeMaxPerTier = 72;

        private GameObject _root;
        /// <summary>頂点色のみ毎回更新（Sprites/Default フォールバック時）。シェーダー駆動時は null。</summary>
        private Mesh _mesh;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Material _material;
        private Color[] _colors;
        /// <summary>Custom/StageSpectrumFloor が読めたとき true。ピークは GPU 定数、頂点色は使わない。</summary>
        private bool _stageMeshShaderActive;
        /// <summary>Material.SetFloatArray 用（31 バンド + 余裕）。</summary>
        private float[] _peakUploadBuffer;
        /// <summary>床メッシュ頂点色: 列ごとの前回 Peak。差分がなければループをスキップして CPU/GPU 負荷を下げる。</summary>
        private float[] _stageMeshPrevColumnPeak;
        private readonly float[] _stageMeshCacheLeftHsv = new float[3];
        private readonly float[] _stageMeshCacheRightHsv = new float[3];
        private bool _stageMeshHsvPrimed;
        /// <summary>AudioSpectrum のバンド数（例: ThirtyOneBand なら 31）</summary>
        private int _spectrumBandCount;
        /// <summary>左半円に spectrum 本 + 右半円に spectrum 本</summary>
        private int _columnCount;
        private bool _built;
        private ParticleSystem _stageSparks;
        private Material _sparkMaterial;
        private SparkBakedEmit[][] _sparkTierBaked;
        private int[] _sparkTierEmitCounts;
        /// <summary>ドラム帯オンセット用: 前フレームの Levels（瞬時スペクトル）。PeakLevels とは別配列で保持する。</summary>
        private float[] _stageSparkPrevDrumBandInstant;
        private bool _stageSparkPrevDrumInstantPrimed;
        /// <summary>キック代理帯の前フレーム PeakLevels。</summary>
        private float _stageSparkPrevKickPeakBand8;
        /// <summary>キック代理で一度発火したらピークが下がるまで再武装しない（連弾抑制）。</summary>
        private bool _stageSparkKickBand8Armed = true;
        private float _stageSparkNextEmitTime;

        /// <summary>1 粒分、Emit 直前まで固定（位置・速度・寿命・サイズ）。Menu と同一形。</summary>
        private struct SparkBakedEmit
        {
            public Vector3 Position;
            public Vector3 Velocity;
            public float StartLifetime;
            public float StartSize;
        }

        private void OnUpdatedRawSpectrums(AudioSpectrum obj)
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }
            if (!PluginConfig.Instance.StageVisualizer)
            {
                return;
            }

            this.UpdateAudioSpectrums(obj);
        }

        private void UpdateAudioSpectrums(AudioSpectrum audio)
        {
            if (!audio || !_built || _mesh == null)
            {
                return;
            }

            if (!this._stageMeshShaderActive && this._colors == null)
            {
                return;
            }

            var peaks = this._audioSpectrum.PeakLevels;

            // 軽量モード時は頂点色 / シェーダー定数の更新を間引き（オブジェクト生成は Initialize のみでここでは行わない）
            bool uploadMeshColors = XrPerfHelper.ShouldUploadStageMeshColorsThisFrame();
            if (uploadMeshColors)
            {
                this._visualizerUtil.RefreshSaberColorsForSpectrumFrame();

                float[] leftHsv = VisualizerUtil.GetLeftSaberHSV();
                float[] rightHsv = VisualizerUtil.GetRightSaberHSV();

                bool saberOrFirst = !this._stageMeshHsvPrimed
                    || !StageMeshApproxHsv3(this._stageMeshCacheLeftHsv, leftHsv)
                    || !StageMeshApproxHsv3(this._stageMeshCacheRightHsv, rightHsv);
                if (saberOrFirst)
                {
                    StageMeshCopyHsv3(this._stageMeshCacheLeftHsv, leftHsv);
                    StageMeshCopyHsv3(this._stageMeshCacheRightHsv, rightHsv);
                    this._stageMeshHsvPrimed = true;
                }

                if (this._stageMeshShaderActive)
                {
                    this.PushStageSpectrumFloorShaderUniforms(peaks, leftHsv, rightHsv);
                }
                else
                {
                    bool anyVertexDirty = saberOrFirst;
                    for (int col = 0; col < _columnCount; col++)
                    {
                        bool isRight = col >= _spectrumBandCount;
                        int bandIdx = isRight ? col - _spectrumBandCount : col;
                        if (bandIdx < 0 || bandIdx >= peaks.Length)
                        {
                            continue;
                        }

                        float peak = peaks[bandIdx];
                        if (!saberOrFirst && StageMeshPeakNearlyEqual(this._stageMeshPrevColumnPeak[col], peak))
                        {
                            continue;
                        }

                        this._stageMeshPrevColumnPeak[col] = peak;
                        anyVertexDirty = true;

                        float[] hsv = isRight ? rightHsv : leftHsv;
                        float level = DepthFillLevel(peak);

                        for (int m = 0; m < SquaresPerBand; m++)
                        {
                            float segT = (m + 1) / (float)SquaresPerBand;
                            bool lit = level >= segT;
                            Color c = SquareColor(hsv, peak, lit, segT);
                            int b = (col * SquaresPerBand + m) * 4;
                            _colors[b] = c;
                            _colors[b + 1] = c;
                            _colors[b + 2] = c;
                            _colors[b + 3] = c;
                        }
                    }

                    if (anyVertexDirty)
                    {
                        _mesh.colors = _colors;
                    }
                }
            }

            UpdateStageSparks(
                this._audioSpectrum.Levels,
                this._audioSpectrum.PeakLevels,
                this._audioSpectrum.MeanLevels);
        }

        /// <summary>ドラム帯のオンセットでステージ火花パーティクルを Emit（Menu と同じベイク済みバースト）。</summary>
        /// <param name="instantBandLevels">Levels（ドラム帯フラックス・跳ね）</param>
        /// <param name="peakBandLevels">PeakLevels（キック代理帯のピーク上昇＋再武装）</param>
        /// <param name="meanBandLevels">MeanLevels（アタック検出 Levels−Mean）</param>
        private void UpdateStageSparks(float[] instantBandLevels, float[] peakBandLevels, float[] meanBandLevels)
        {
            if (this._stageSparks == null || instantBandLevels == null || instantBandLevels.Length == 0
                || peakBandLevels == null || peakBandLevels.Length != instantBandLevels.Length
                || meanBandLevels == null || meanBandLevels.Length != instantBandLevels.Length)
            {
                return;
            }

            int first = Mathf.Clamp(SparkDrumBandIndexFirst, 0, instantBandLevels.Length - 1);
            int last = Mathf.Clamp(SparkDrumBandIndexLast, 0, instantBandLevels.Length - 1);
            if (last < first)
            {
                return;
            }

            int bandCount = last - first + 1;
            if (this._stageSparkPrevDrumBandInstant == null || this._stageSparkPrevDrumBandInstant.Length != bandCount)
            {
                this._stageSparkPrevDrumBandInstant = new float[bandCount];
                this._stageSparkPrevDrumInstantPrimed = false;
            }

            int kickIx = Mathf.Clamp(StageSparkKickProxyBandIndex, 0, instantBandLevels.Length - 1);
            float lvKick = instantBandLevels[kickIx];
            float peak8 = peakBandLevels[kickIx];

            if (!this._stageSparkPrevDrumInstantPrimed)
            {
                for (int j = 0; j < bandCount; j++)
                {
                    this._stageSparkPrevDrumBandInstant[j] = instantBandLevels[first + j];
                }

                this._stageSparkPrevKickPeakBand8 = peak8;
                this._stageSparkKickBand8Armed = true;
                this._stageSparkPrevDrumInstantPrimed = true;
                return;
            }

            if (peak8 < StageSparkKickPeakRearmBelow)
            {
                this._stageSparkKickBand8Armed = true;
            }

            float kickBand8PeakRise = Mathf.Max(0f, peak8 - this._stageSparkPrevKickPeakBand8);

            float spectralFlux = 0f;
            float maxDrumBandJump = 0f;
            for (int j = 0; j < bandCount; j++)
            {
                float lv = instantBandLevels[first + j];
                float rise = lv - this._stageSparkPrevDrumBandInstant[j];
                if (rise > 0f)
                {
                    spectralFlux += rise;
                }

                if (rise > maxDrumBandJump)
                {
                    maxDrumBandJump = rise;
                }
            }

            this._stageSparkPrevKickPeakBand8 = peak8;
            for (int j = 0; j < bandCount; j++)
            {
                this._stageSparkPrevDrumBandInstant[j] = instantBandLevels[first + j];
            }

            // オンセット判定は毎フレーム行う（間引くと立ち上がりが prev 更新で潰れて遅延・取り逃しになる）。
            // メッシュ頂点色の GPU 送信だけ XrPerfHelper で間引き済み。
            float sum = 0f;
            float bandMax = 0f;
            for (int i = first; i <= last; i++)
            {
                float v = instantBandLevels[i];
                sum += v;
                if (v > bandMax)
                {
                    bandMax = v;
                }
            }

            float drumAverage = sum / bandCount;

            float maxTransientOverMean = 0f;
            for (int i = first; i <= last; i++)
            {
                float tr = instantBandLevels[i] - meanBandLevels[i];
                if (tr > maxTransientOverMean)
                {
                    maxTransientOverMean = tr;
                }
            }

            maxTransientOverMean = Mathf.Max(0f, maxTransientOverMean);
            bool attackLikeKick = maxTransientOverMean >= StageSparkTransientOverMeanMin
                && maxDrumBandJump >= StageSparkMaxBandJumpMin;

            bool energyGate = drumAverage >= StageSparkPercussiveMinBandEnergy
                || peak8 >= StageSparkKickBand8PeakMin;
            if (!energyGate)
            {
                return;
            }

            float crest = bandMax / (drumAverage + 1e-5f);
            float fluxNeed = Mathf.Max(StageSparkPercussiveFluxMin, drumAverage * StageSparkPercussiveFluxOverAvgMin);
            bool band8Onset = attackLikeKick
                && this._stageSparkKickBand8Armed
                && kickBand8PeakRise >= StageSparkKickBand8PeakRiseMin
                && peak8 >= StageSparkKickBand8PeakMin;
            bool drumOnset = attackLikeKick
                && drumAverage >= StageSparkPercussiveMinBandEnergy
                && spectralFlux >= fluxNeed
                && crest >= StageSparkPercussiveCrestMin;
            bool percussiveOnset = band8Onset || drumOnset;
            if (!percussiveOnset)
            {
                return;
            }

            if (Time.time < this._stageSparkNextEmitTime)
            {
                return;
            }

            float levelForHit = Mathf.Max(drumAverage, lvKick, peak8);
            float fluxForHit = Mathf.Max(spectralFlux, kickBand8PeakRise, maxTransientOverMean, maxDrumBandJump);
            float hitIntensity = Mathf.Clamp01(
                (SparkIntensityFromAvg * levelForHit)
                + (SparkIntensityFromJump * Mathf.Min(fluxForHit * StageSparkTierFluxIntensityMul, StageSparkTierFluxIntensityCap)));
            if (XrPerfHelper.ShouldReduceVisualizerCost())
            {
                hitIntensity *= 0.65f;
            }

            float tierLinear = Mathf.Clamp01(hitIntensity * StageSparkTierSelectScale);
            float tierMetric = Mathf.Pow(tierLinear, StageSparkTierCurveExponent);
            int tier = this.SelectSparkTier(tierMetric);

            if (!this._stageSparks.isPlaying)
            {
                this._stageSparks.Play();
            }

            this.EmitStageSparksBurstFromBaked(tier);
            this._stageSparkNextEmitTime = Time.time + StageSparkBurstMinIntervalSeconds;
            if (band8Onset)
            {
                this._stageSparkKickBand8Armed = false;
            }
        }

        /// <summary>ベイク済みパラメータをそのまま Emit（Menu と同一）。</summary>
        private void EmitStageSparksBurstFromBaked(int tier)
        {
            if (this._sparkTierBaked == null || this._sparkTierEmitCounts == null || tier < 0 || tier >= SparkTierCount)
            {
                return;
            }

            int n = this._sparkTierEmitCounts[tier];
            SparkBakedEmit[] layer = this._sparkTierBaked[tier];
            ParticleSystem.EmitParams emitParams = default;
            emitParams.applyShapeToPosition = false;
            for (int i = 0; i < n; i++)
            {
                SparkBakedEmit p = layer[i];
                emitParams.position = p.Position;
                emitParams.startLifetime = p.StartLifetime;
                emitParams.startSize = p.StartSize;
                emitParams.velocity = p.Velocity;

                this._stageSparks.Emit(emitParams, 1);
            }
        }

        /// <summary>低・中・高ティア分を事前生成（Menu の BakeStageSparkBurstTemplates と同一ロジック）。</summary>
        private void BakeStageSparkBurstTemplates()
        {
            this._sparkTierBaked = new SparkBakedEmit[SparkTierCount][];
            this._sparkTierEmitCounts = new int[SparkTierCount];

            UnityEngine.Random.State previous = UnityEngine.Random.state;
            try
            {
                for (int tier = 0; tier < SparkTierCount; tier++)
                {
                    UnityEngine.Random.InitState(54820481 + tier * 11003);

                    float burstRepr = tier == 0 ? 0.30f : (tier == 1 ? 0.47f : 0.64f);
                    int baseCountRepr = tier == 0 ? 24 : (tier == 1 ? 32 : 40);

                    int scaledCount = Mathf.Clamp(
                        Mathf.RoundToInt(baseCountRepr * Mathf.Lerp(SparkBurstCountScaleMin, SparkBurstCountScaleMax, burstRepr)),
                        SparkEmitMin,
                        SparkEmitMax);
                    if (XrPerfHelper.ShouldReduceVisualizerCost())
                    {
                        scaledCount = Mathf.Max(SparkEmitMin, Mathf.RoundToInt(scaledCount * 0.62f));
                    }

                    float spreadMul = Mathf.Lerp(SparkBurstSpreadMulMin, SparkBurstSpreadMulMax, burstRepr);
                    float upBlend = Mathf.Lerp(SparkBurstUpBlendMin, 1f, burstRepr);
                    float lifeMul = Mathf.Lerp(SparkBurstLifeMulMin, SparkBurstLifeMulMax, burstRepr);
                    float tierLifeExtra = tier == 0 ? 1f : (tier == 1 ? StageSparkBakeLifetimeMidTierExtraMul : StageSparkBakeLifetimeHighTierExtraMul);
                    float ringRadius = (CircleRadius * 0.94f) * Mathf.Lerp(SparkBurstShapeRadiusMulMin, SparkBurstShapeRadiusMulMax, burstRepr);
                    float hitIntensityRepr = burstRepr;
                    float particleSizeMax = tier == 0
                        ? SparkParticleSizeMax
                        : (tier == 1 ? SparkParticleSizeMax * SparkParticleSizeMidTierMaxMul : SparkParticleSizeMax * SparkParticleSizeHighTierMaxMul);

                    var arr = new SparkBakedEmit[SparkBakeMaxPerTier];
                    float angleStep = (Mathf.PI * 2f) / Mathf.Max(1, scaledCount);
                    for (int i = 0; i < scaledCount; i++)
                    {
                        float pint = Mathf.Clamp01(hitIntensityRepr * UnityEngine.Random.Range(0.9f, 1.05f));
                        float life = UnityEngine.Random.Range(StageSparkBakeLifetimeMin, StageSparkBakeLifetimeMax) * lifeMul * tierLifeExtra;
                        float vyCap = (SparkMaxRiseMeters / life) + (0.5f * SparkGravityEffective * life);
                        vyCap = Mathf.Min(vyCap, 1.2f);
                        float vy = Mathf.Lerp(0.22f, vyCap, pint * upBlend);

                        Vector2 xz = UnityEngine.Random.insideUnitCircle;
                        if (xz.sqrMagnitude < 1e-8f)
                        {
                            xz = Vector2.right;
                        }

                        xz = xz.normalized * (Mathf.Lerp(0.16f, 0.62f, pint) * spreadMul);

                        float ringAngle = angleStep * (i + 0.5f);
                        Vector3 ringPos = new Vector3(Mathf.Cos(ringAngle) * ringRadius, 0f, Mathf.Sin(ringAngle) * ringRadius);

                        arr[i] = new SparkBakedEmit
                        {
                            Position = ringPos,
                            StartLifetime = life,
                            StartSize = Mathf.Lerp(SparkParticleSizeMin, particleSizeMax, pint),
                            Velocity = new Vector3(xz.x, vy, xz.y)
                        };
                    }

                    this._sparkTierBaked[tier] = arr;
                    this._sparkTierEmitCounts[tier] = scaledCount;
                }
            }
            finally
            {
                UnityEngine.Random.state = previous;
            }
        }

        /// <summary>ステージ火花用 ParticleSystem。Menu の BuildStageSparkParticles と同一構成。</summary>
        private void BuildStageSparkParticles()
        {
            var sparksGo = new GameObject("stageSpectrumSparks");
            sparksGo.transform.SetParent(_root.transform, false);
            sparksGo.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            sparksGo.transform.localRotation = Quaternion.identity;

            this._stageSparks = sparksGo.AddComponent<ParticleSystem>();
            var ps = this._stageSparks;

            var main = ps.main;
            main.playOnAwake = true;
            main.loop = true;
            main.duration = 5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(SparkLifetimeMin, SparkLifetimeMax);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(SparkParticleSizeMin, SparkParticleSizeMax);
            main.maxParticles = 512;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.gravityModifier = 0.35f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.97f, 0.88f, 1f),
                new Color(1f, 0.72f, 0.35f, 1f));

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = CircleRadius * 0.94f;
            shape.radiusThickness = 1f;
            shape.arc = 360f;
            shape.rotation = new Vector3(90f, 0f, 0f);
            shape.randomDirectionAmount = 0f;

            var vel = ps.velocityOverLifetime;
            vel.enabled = false;

            var sizeOl = ps.sizeOverLifetime;
            sizeOl.enabled = true;
            var shrink = new AnimationCurve(
                new Keyframe(0f, 1f, 0f, 0f),
                new Keyframe(0.55f, 1f, 0f, 0f),
                new Keyframe(1f, 0f, 0f, 0f));
            for (int ki = 0; ki < shrink.length; ki++)
            {
                shrink.SmoothTangents(ki, 0.35f);
            }

            sizeOl.size = new ParticleSystem.MinMaxCurve(1f, shrink);

            var colorOl = ps.colorOverLifetime;
            colorOl.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.55f, 0.15f), 1f) },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.92f, 0.5f),
                    new GradientAlphaKey(0.45f, 0.78f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOl.color = g;

            Shader sparkShader = VisualizerUtil.GetShader("Particles/Additive")
                ?? VisualizerUtil.GetShader("Legacy Shaders/Particles/Additive")
                ?? VisualizerUtil.GetShader("Sprites/Default");
            var renderer = sparksGo.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.Facing;
            if (sparkShader != null)
            {
                this._sparkMaterial = new Material(sparkShader);
                this._sparkMaterial.renderQueue = 3200;
                renderer.material = this._sparkMaterial;
            }

            ps.Clear();
            this.BakeStageSparkBurstTemplates();
            ps.Play();
        }

        /// <summary>tierMetric＝Pow(Clamp01(hit×Scale), Exponent) を低/中/高に振り分け。</summary>
        private int SelectSparkTier(float hitIntensity)
        {
            if (hitIntensity < StageSparkTierBoundaryLowMid)
            {
                return 0;
            }

            if (hitIntensity < StageSparkTierBoundaryMidHigh)
            {
                return 1;
            }

            return 2;
        }

        /// <summary>内側から奥（径方向の外）へ、四角が埋まる量 0..1</summary>
        private float DepthFillLevel(float peakLevels)
        {
            if (!PluginConfig.Instance.enableMerihari)
            {
                return Mathf.Clamp01(Mathf.Lerp(0f, 1f, this.Nomalize(peakLevels * 3f)));
            }
            return Mathf.Clamp01(this.Nomalize(Mathf.Lerp(0f, 1f, peakLevels * 5f)));
        }

        private Color SquareColor(float[] hsv, float peakLevels, bool lit, float segT)
        {
            if (!lit)
            {
                return Color.HSVToRGB(hsv[0], hsv[1], 0f).ColorWithAlpha(0f);
            }

            if (!PluginConfig.Instance.enableMerihari)
            {
                return Color.HSVToRGB(hsv[0], hsv[1], 1f).ColorWithAlpha(0.75f);
            }

            var v = Mathf.Lerp(0.35f, 1f, Mathf.Clamp01((segT + peakLevels) * 0.5f));
            var a = Mathf.Lerp(0.25f, 0.85f, this.Nomalize(peakLevels * 4f));
            return Color.HSVToRGB(hsv[0], hsv[1], this.Nomalize(v)).ColorWithAlpha(this.Nomalize(a));
        }

        private float Nomalize(float f)
        {
            var result = Mathf.Lerp(5f, 1f, f);
            return f * result;
        }

        /// <summary>
        /// 列 index col: 左半円 col=0..spectrum-1、右半円 col=spectrum..2*spectrum-1。
        /// 各半円で奥(-Z)→手前(+Z)＝低音→高音。左右は同じバンド数で Z 軸対称（θ_R = π - πj/d, θ_L = π + πi/d）。
        /// </summary>
        private static float BandAngleRadians(int col, int spectrumBandCount)
        {
            if (spectrumBandCount <= 1)
            {
                return Mathf.PI;
            }

            int d = spectrumBandCount - 1;
            if (col < spectrumBandCount)
            {
                return Mathf.PI + Mathf.PI * col / d;
            }

            int j = col - spectrumBandCount;
            return Mathf.PI - Mathf.PI * j / d;
        }

        /// <summary>
        /// 床 XZ 上の正方形を、各バンド方向に沿って径方向（中心→外）へ余白付きで並べる。
        /// </summary>
        private void BuildRadialSquaresMesh()
        {
            _spectrumBandCount = this._audioSpectrum.PeakLevels.Length;
            _columnCount = _spectrumBandCount * 2;
            int quads = _columnCount * SquaresPerBand;
            int vCount = quads * 4;
            int triCount = quads * 6;
            var vertices = new Vector3[vCount];
            var triangles = new int[triCount];
            var uvs = new Vector2[vCount];
            var uv2s = new Vector2[vCount];
            if (this._stageMeshShaderActive)
            {
                this._colors = null;
            }
            else
            {
                this._colors = new Color[vCount];
            }

            for (int col = 0; col < _columnCount; col++)
            {
                float theta = BandAngleRadians(col, _spectrumBandCount);
                float sin = Mathf.Sin(theta);
                float cos = Mathf.Cos(theta);
                var radial = new Vector3(sin, 0f, cos);
                var tangent = new Vector3(cos, 0f, -sin);
                if (col >= _spectrumBandCount)
                {
                    tangent = -tangent;
                }

                int bandIdx = col >= _spectrumBandCount ? col - _spectrumBandCount : col;
                float uv2Right = col >= _spectrumBandCount ? 1f : 0f;

                for (int m = 0; m < SquaresPerBand; m++)
                {
                    float centerDist = CircleRadius + m * RadialPitch;
                    var center = radial * centerDist;

                    int b = (col * SquaresPerBand + m) * 4;
                    vertices[b] = center - SquareHalf * tangent - SquareHalf * radial;
                    vertices[b + 1] = center + SquareHalf * tangent - SquareHalf * radial;
                    vertices[b + 2] = center + SquareHalf * tangent + SquareHalf * radial;
                    vertices[b + 3] = center - SquareHalf * tangent + SquareHalf * radial;

                    var uvBand = new Vector2(bandIdx, m);
                    uvs[b] = uvBand;
                    uvs[b + 1] = uvBand;
                    uvs[b + 2] = uvBand;
                    uvs[b + 3] = uvBand;
                    var uv2Side = new Vector2(uv2Right, 0f);
                    uv2s[b] = uv2Side;
                    uv2s[b + 1] = uv2Side;
                    uv2s[b + 2] = uv2Side;
                    uv2s[b + 3] = uv2Side;

                    int t = (col * SquaresPerBand + m) * 6;
                    triangles[t] = b;
                    triangles[t + 1] = b + 2;
                    triangles[t + 2] = b + 1;
                    triangles[t + 3] = b;
                    triangles[t + 4] = b + 3;
                    triangles[t + 5] = b + 2;
                }
            }

            _mesh = new Mesh();
            _mesh.name = "StageSpectrumRadialSquaresMesh";
            _mesh.MarkDynamic();
            _mesh.vertices = vertices;
            _mesh.triangles = triangles;
            _mesh.uv = uvs;
            _mesh.uv2 = uv2s;
            if (this._colors != null)
            {
                _mesh.colors = _colors;
            }

            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            this._stageMeshPrevColumnPeak = new float[_columnCount];
            for (int i = 0; i < _columnCount; i++)
            {
                this._stageMeshPrevColumnPeak[i] = float.NaN;
            }

            this._stageMeshHsvPrimed = false;
        }

        private static void StageMeshCopyHsv3(float[] dest, float[] src)
        {
            dest[0] = src[0];
            dest[1] = src[1];
            dest[2] = src[2];
        }

        private static bool StageMeshApproxHsv3(float[] a, float[] b)
        {
            float dh = Mathf.Abs(a[0] - b[0]);
            dh = Mathf.Min(dh, 1f - dh);
            return dh < 0.012f && Mathf.Abs(a[1] - b[1]) < 0.02f && Mathf.Abs(a[2] - b[2]) < 0.02f;
        }

        private static bool StageMeshPeakNearlyEqual(float prev, float peak)
        {
            if (float.IsNaN(prev))
            {
                return false;
            }

            return Mathf.Abs(prev - peak) < 0.0025f;
        }

        /// <summary>StageSpectrumFloor シェーダーへピーク・セイバー色・めりはりを渡す。</summary>
        private void PushStageSpectrumFloorShaderUniforms(float[] peaks, float[] leftHsv, float[] rightHsv)
        {
            if (this._material == null || this._peakUploadBuffer == null || peaks == null)
            {
                return;
            }

            int n = Mathf.Min(peaks.Length, 31);
            for (int i = 0; i < n; i++)
            {
                this._peakUploadBuffer[i] = peaks[i];
            }

            for (int i = n; i < 32; i++)
            {
                this._peakUploadBuffer[i] = 0f;
            }

            this._material.SetFloatArray("_PeakLevels", this._peakUploadBuffer);
            this._material.SetVector("_LeftHsv", new Vector4(leftHsv[0], leftHsv[1], leftHsv[2], 0f));
            this._material.SetVector("_RightHsv", new Vector4(rightHsv[0], rightHsv[1], rightHsv[2], 0f));
            this._material.SetFloat("_Merihari", PluginConfig.Instance.enableMerihari ? 1f : 0f);
        }

        public void Initialize()
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }

            if (!PluginConfig.Instance.StageVisualizer)
            {
                return;
            }

            this._audioSpectrum.Band = AudioSpectrum.BandType.ThirtyOneBand;
            this._audioSpectrum.fallSpeed = 1f;
            this._audioSpectrum.sensibility = 10f;

            Shader shader = VisualizerUtil.GetShader("Custom/StageSpectrumFloor")
                ?? VisualizerUtil.GetShader("Sprites/Default");
            if (shader == null)
            {
                return;
            }

            this._stageMeshShaderActive = string.Equals(shader.name, "Custom/StageSpectrumFloor", System.StringComparison.Ordinal);

            BuildRadialSquaresMesh();

            _root = new GameObject("stageSpectrumVisualizer");
            _root.transform.SetParent(FloorViewController.visualizerFloorRoot.transform, false);
            _root.transform.localPosition = new Vector3(0f, 0.0001f, 0f);
            _root.transform.localScale = Vector3.one;

            _meshFilter = _root.AddComponent<MeshFilter>();
            _meshFilter.sharedMesh = _mesh;

            _meshRenderer = _root.AddComponent<MeshRenderer>();
            _material = new Material(shader);
            _meshRenderer.material = _material;

            if (this._stageMeshShaderActive)
            {
                this._peakUploadBuffer = new float[32];
                this._material.SetFloat("_SquaresPerBand", SquaresPerBand);
                this._material.SetFloat("_Merihari", PluginConfig.Instance.enableMerihari ? 1f : 0f);
            }

            this.BuildStageSparkParticles();
            this._stageSparkPrevDrumBandInstant = null;
            this._stageSparkPrevDrumInstantPrimed = false;
            this._stageSparkPrevKickPeakBand8 = 0f;
            this._stageSparkKickBand8Armed = true;
            this._stageSparkNextEmitTime = 0f;

            _built = true;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;
        }

        private bool _disposedValue;
        private AudioSpectrum _audioSpectrum;
        private VisualizerUtil _visualizerUtil;

        [Inject]
        public void Constructor([Inject(Id = AudioSpectrum.BandType.ThirtyOneBand)] AudioSpectrum audioSpectrum, VisualizerUtil visualizerUtil)
        {
            this._audioSpectrum = audioSpectrum;
            this._visualizerUtil = visualizerUtil;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposedValue)
            {
                if (disposing)
                {
                    if (this._audioSpectrum != null)
                    {
                        this._audioSpectrum.UpdatedRawSpectrums -= this.OnUpdatedRawSpectrums;
                    }

                    if (_root != null)
                    {
                        if (_meshFilter != null)
                        {
                            _meshFilter.sharedMesh = null;
                            _meshFilter = null;
                        }

                        _meshRenderer = null;
                        UnityEngine.Object.Destroy(_root);
                        _root = null;
                    }

                    if (_mesh != null)
                    {
                        UnityEngine.Object.Destroy(_mesh);
                        _mesh = null;
                    }

                    if (_material != null)
                    {
                        UnityEngine.Object.Destroy(_material);
                        _material = null;
                    }

                    if (this._sparkMaterial != null)
                    {
                        UnityEngine.Object.Destroy(this._sparkMaterial);
                        this._sparkMaterial = null;
                    }

                    this._stageSparks = null;
                    this._sparkTierBaked = null;
                    this._sparkTierEmitCounts = null;
                    _stageSparkPrevDrumBandInstant = null;
                    _stageSparkPrevDrumInstantPrimed = false;
                    _stageSparkPrevKickPeakBand8 = 0f;
                    _stageSparkKickBand8Armed = true;
                    _stageMeshPrevColumnPeak = null;
                    _stageMeshHsvPrimed = false;
                    _colors = null;
                    _peakUploadBuffer = null;
                    _stageMeshShaderActive = false;
                    _built = false;
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
