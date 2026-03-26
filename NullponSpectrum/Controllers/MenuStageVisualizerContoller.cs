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
    public class MenuStageVisualizerController : IInitializable, ILateTickable, IDisposable
    {
        /// <summary>メッシュ・火花リング・初速・高さなどの長さスケール（一段さらに拡大するときはこちら）。</summary>
        private const float MenuStageGeometryScale = 1.5f * 1.25f;

        /// <summary>
        /// 各バンド列の奥行き先頭（m=0）の四角の中心が乗る円の半径（XZ）。メッシュはこの値から径方向に伸ばすので、円の大きさはここを変える。
        /// </summary>
        private const float CircleRadius = 0.44f * MenuStageGeometryScale;
        /// <summary>1 バンドあたり奥行（径方向）に並べる四角の個数</summary>
        private const int SquaresPerBand = 24;
        /// <summary>各四角の半辺長（接線方向・径方向とも同じで正方形）</summary>
        private const float SquareHalf = 0.016f * MenuStageGeometryScale;
        /// <summary>隣り合う四角のあいだの径方向の余白（面と面の間）</summary>
        private const float GapRadial = 0.014f * MenuStageGeometryScale;

        private static float RadialPitch => SquareHalf * 2f + GapRadial;

        /// <summary>
        /// ドラム帯・キック代理 index は <see cref="StageVisualizerController"/> と同一。しきい値だけ MenuSpark*。
        /// </summary>
        private const int SparkDrumBandIndexFirst = 7;
        private const int SparkDrumBandIndexLast = 11;
        private const int StageSparkKickProxyBandIndex = 9;
        /// <summary>帯全体の平均がこれ未満なら無視（無音付近のノイズ）</summary>
        private const float MenuSparkPercussiveMinBandEnergy = 0.019f;
        /// <summary>フラックスの絶対しきい値（帯 7〜11 の立ち上がり合計の目安）</summary>
        private const float MenuSparkPercussiveFluxMin = 0.014f;
        /// <summary>フラックスが平均エネルギーのこの倍率未満なら無視（相対的に小さい揺れを弾く）</summary>
        private const float MenuSparkPercussiveFluxOverAvgMin = 0.175f;
        /// <summary>クレスト bandMax/drumAverage の下限。全帯が均一に伸びる持続音を弱める</summary>
        private const float MenuSparkPercussiveCrestMin = 1.0f;
        /// <summary>連続バーストの最短間隔（秒）</summary>
        private const float MenuSparkBurstMinIntervalSeconds = 0.072f;
        private const float MenuSparkTransientOverMeanMin = 0.019f;
        private const float MenuSparkMaxBandJumpMin = 0.01f;
        private const float MenuSparkKickBandPeakRiseMin = 0.013f;
        private const float MenuSparkKickBandPeakMin = 0.029f;
        private const float MenuSparkKickPeakRearmBelow = 0.022f;

        /// <summary>1 バーストあたりの Emit 数の下限</summary>
        private const int SparkEmitMin = 16;
        /// <summary>1 バーストあたりの Emit 数の上限</summary>
        private const int SparkEmitMax = 74;
        /// <summary>上方向初速の上限を決めるときの「目標上昇量」（m 相当。1 Unity unit ≒ 1m 想定）— Stage と同一。</summary>
        private const float SparkMaxRiseMeters = 0.6f;
        /// <summary>Emit 時 startSize の下限 — Stage と同一。</summary>
        private const float SparkParticleSizeMin = 0.011f;
        /// <summary>Emit 時 startSize の上限 — Stage と同一。</summary>
        private const float SparkParticleSizeMax = 0.16f;
        /// <summary>中ティア bake 時、SparkParticleSizeMax に掛ける上限 — Stage と同一。</summary>
        private const float SparkParticleSizeMidTierMaxMul = 0.78f;
        /// <summary>高ティア bake 時、SparkParticleSizeMax に掛ける上限 — Stage と同一。</summary>
        private const float SparkParticleSizeHighTierMaxMul = 0.88f;
        /// <summary>パーティクル寿命の下限／上限（Main のデフォルト）。ベイクは MenuSpark* を使用。</summary>
        private const float SparkLifetimeMin = 0.1f;
        private const float SparkLifetimeMax = 0.28f;
        /// <summary>メニュー火花ベイク用寿命（秒）。中〜高が短すぎるとパッと消えるので長め。</summary>
        private const float MenuSparkLifetimeMin = 0.15f;
        private const float MenuSparkLifetimeMax = 0.42f;
        /// <summary>中ティアの寿命に掛ける追加倍率（低との差は従来の lifeMul 側）</summary>
        private const float MenuSparkLifetimeMidTierExtraMul = 1.14f;
        /// <summary>高ティアの寿命に掛ける追加倍率</summary>
        private const float MenuSparkLifetimeHighTierExtraMul = 1.32f;
        /// <summary>上方向キャップ計算用の等価重力（g × main.gravityModifier）。軌道のざっくり見積りに使う</summary>
        private const float SparkGravityEffective = 9.81f * 0.35f;
        /// <summary>hitIntensity 係数 — Game（StageVisualizerController）と同一。</summary>
        private const float SparkIntensityFromAvg = 3.85f;
        /// <summary>hitIntensity 係数 — Game と同一。</summary>
        private const float SparkIntensityFromJump = 7.8f;
        /// <summary>弱いヒット時のバースト粒数倍率（1 = ベース count のまま）</summary>
        private const float SparkBurstCountScaleMin = 1f;
        /// <summary>強いヒット時のバースト粒数倍率</summary>
        private const float SparkBurstCountScaleMax = 2.25f;
        /// <summary>弱いヒット時の水平広がり（XZ 初速スケール）の倍率</summary>
        private const float SparkBurstSpreadMulMin = 0.48f;
        /// <summary>強いヒット時の水平広がりの倍率</summary>
        private const float SparkBurstSpreadMulMax = 1.84f;
        /// <summary>弱いヒット時、上方向初速ブレンドの下限（小さいほど跳ねにくい）</summary>
        private const float SparkBurstUpBlendMin = 1.25f;
        /// <summary>弱いヒット時、発生リング半径の倍率（ステージ円に対する割合）</summary>
        private const float SparkBurstShapeRadiusMulMin = 0.64f;
        /// <summary>強いヒット時、発生リング半径の倍率</summary>
        private const float SparkBurstShapeRadiusMulMax = 1.84f;
        /// <summary>弱いヒット時の寿命倍率（短め）</summary>
        private const float SparkBurstLifeMulMin = 0.9f;
        /// <summary>強いヒット時の寿命倍率（やや長め）</summary>
        private const float SparkBurstLifeMulMax = 1.06f;
        /// <summary>火花バーストを音圧で 3 段階に分け、Initialize でベイクする（更新時は Random・再計算なし）。</summary>
        private const int SparkTierCount = 3;
        /// <summary>各ティアの最大 Emit 数（SparkEmitMax × バースト倍率の余裕）</summary>
        private const int SparkBakeMaxPerTier = 72;

        /// <summary>
        /// メニュー火花の tier 境界（Pow 後の tierMetric）。低・中・高のバランス用（極端に寄せない）。
        /// </summary>
        private const float MenuSparkTierBoundaryLowMid = 0.34f;
        private const float MenuSparkTierBoundaryMidHigh = 0.45f;
        /// <summary>hitIntensity を tierMetric 化する前の持ち上げ。</summary>
        private const float MenuSparkTierSelectScale = 0.93f;
        /// <summary>1 より少し大きいと弱めのヒットは tierMetric がやや下がり低寄り（中間の 1.04 前後）。</summary>
        private const float MenuSparkTierCurveExponent = 1.03f;
        /// <summary>hitIntensity のフラックス項 — Game と同一（しきい値は Tier 境界など MenuSpark*）。</summary>
        private const float MenuSparkTierFluxIntensityMul = 1f;
        private const float MenuSparkTierFluxIntensityCap = 0.28f;

        /// <summary>1 粒分、Emit 直前まで固定（位置・速度・寿命・サイズ）。</summary>
        private struct SparkBakedEmit
        {
            public Vector3 Position;
            public Vector3 Velocity;
            public float StartLifetime;
            public float StartSize;
        }

        private GameObject _root;
        /// <summary>頂点色のみ毎回更新。メッシュ本体は Build 時に確定した参照のまま。</summary>
        private Mesh _mesh;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Material _material;
        private Color[] _colors;
        /// <summary>Custom/StageSpectrumFloor が読めたとき true。床は Levels を GPU に渡す。</summary>
        private bool _menuMeshShaderActive;
        private float[] _menuPeakUploadBuffer;
        /// <summary>AudioSpectrum のバンド数（例: ThirtyOneBand なら 31）</summary>
        private int _spectrumBandCount;
        /// <summary>左半円に spectrum 本 + 右半円に spectrum 本</summary>
        private int _columnCount;
        private bool _built;
        private ParticleSystem _stageSparks;
        private Material _sparkMaterial;
        /// <summary>ドラム帯: 前フレームの Levels（瞬時スペクトル）。</summary>
        private float[] _menuSparkPrevDrumBandInstant;
        private bool _menuSparkPrevDrumInstantPrimed;
        private float _menuSparkPrevKickPeakAtProxy;
        private bool _menuSparkKickProxyArmed = true;
        private float _menuSparkNextEmitTime;
        private SparkBakedEmit[][] _sparkTierBaked;
        private int[] _sparkTierEmitCounts;

        /// <summary>メニュー用: 設定のセイバー色から取った HSV（リアルタイム追従しない）。</summary>
        private readonly float[] _menuLeftHsv = new float[3];
        private readonly float[] _menuRightHsv = new float[3];
        private ColorManager _colorManager;
        private ColorScheme _colorScheme;

        private void OnUpdatedRawSpectrums(AudioSpectrum obj)
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }
            if (!PluginConfig.Instance.MenuStageVisualizer)
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

            if (!this._menuMeshShaderActive && this._colors == null)
            {
                return;
            }

            // 軽量モード時は頂点色 / シェーダー定数の更新を間引き（オブジェクト生成は Build のみでここでは行わない）
            bool uploadMeshColors = XrPerfHelper.ShouldUploadStageMeshColorsThisFrame();
            if (uploadMeshColors)
            {
                if (this._menuMeshShaderActive)
                {
                    this.PushMenuSpectrumFloorShaderUniforms(this._audioSpectrum.Levels);
                }
                else
                {
                    float[] levels = this._audioSpectrum.Levels;
                    if (levels == null)
                    {
                        return;
                    }

                    for (int col = 0; col < _columnCount; col++)
                    {
                        bool isRight = col >= _spectrumBandCount;
                        int bandIdx = isRight ? col - _spectrumBandCount : col;
                        if (bandIdx < 0 || bandIdx >= levels.Length)
                        {
                            continue;
                        }

                        float[] hsv = isRight ? this._menuRightHsv : this._menuLeftHsv;
                        float inst = levels[bandIdx];
                        float level = DepthFillLevel(inst);

                        for (int m = 0; m < SquaresPerBand; m++)
                        {
                            float segT = (m + 1) / (float)SquaresPerBand;
                            bool lit = level >= segT;
                            Color c = SquareColor(hsv, inst, lit, segT);
                            int b = (col * SquaresPerBand + m) * 4;
                            _colors[b] = c;
                            _colors[b + 1] = c;
                            _colors[b + 2] = c;
                            _colors[b + 3] = c;
                        }
                    }

                    _mesh.colors = _colors;
                }
            }

            this.StopIdleStageSparksIfNeeded();
            this.UpdateStageSparks(
                this._audioSpectrum.Levels,
                this._audioSpectrum.PeakLevels,
                this._audioSpectrum.MeanLevels);
        }

        /// <summary>StageSpectrumFloor と同じプロパティ名。中身は瞬時 Levels（Game 床と同じ）。</summary>
        private void PushMenuSpectrumFloorShaderUniforms(float[] levels)
        {
            if (this._material == null || this._menuPeakUploadBuffer == null || levels == null)
            {
                return;
            }

            int n = Mathf.Min(levels.Length, 31);
            for (int i = 0; i < n; i++)
            {
                this._menuPeakUploadBuffer[i] = levels[i];
            }

            for (int i = n; i < 32; i++)
            {
                this._menuPeakUploadBuffer[i] = 0f;
            }

            this._material.SetFloatArray("_PeakLevels", this._menuPeakUploadBuffer);
            this._material.SetVector("_LeftHsv", new Vector4(this._menuLeftHsv[0], this._menuLeftHsv[1], this._menuLeftHsv[2], 0f));
            this._material.SetVector("_RightHsv", new Vector4(this._menuRightHsv[0], this._menuRightHsv[1], this._menuRightHsv[2], 0f));
            this._material.SetFloat("_Merihari", PluginConfig.Instance.enableMerihari ? 1f : 0f);
        }

        /// <summary>生存粒が無く再生中だけ止め、アイドル時のシミュ負荷を抑える（バースト直前に Play する）。</summary>
        private void StopIdleStageSparksIfNeeded()
        {
            if (_stageSparks == null || !_stageSparks.isPlaying)
            {
                return;
            }

            if (_stageSparks.particleCount > 0)
            {
                return;
            }

            _stageSparks.Stop(withChildren: false, ParticleSystemStopBehavior.StopEmitting);
        }

        /// <summary>
        /// Game 側と同じく Levels / PeakLevels / MeanLevels でオンセット。しきい値はメニュー用定数。
        /// </summary>
        private void UpdateStageSparks(float[] instantBandLevels, float[] peakBandLevels, float[] meanBandLevels)
        {
            if (_stageSparks == null || instantBandLevels == null || instantBandLevels.Length == 0
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
            if (this._menuSparkPrevDrumBandInstant == null || this._menuSparkPrevDrumBandInstant.Length != bandCount)
            {
                this._menuSparkPrevDrumBandInstant = new float[bandCount];
                this._menuSparkPrevDrumInstantPrimed = false;
            }

            int kickIx = Mathf.Clamp(StageSparkKickProxyBandIndex, 0, instantBandLevels.Length - 1);
            float lvKick = instantBandLevels[kickIx];
            float peakProxy = peakBandLevels[kickIx];

            if (!this._menuSparkPrevDrumInstantPrimed)
            {
                for (int j = 0; j < bandCount; j++)
                {
                    this._menuSparkPrevDrumBandInstant[j] = instantBandLevels[first + j];
                }

                this._menuSparkPrevKickPeakAtProxy = peakProxy;
                this._menuSparkKickProxyArmed = true;
                this._menuSparkPrevDrumInstantPrimed = true;
                return;
            }

            if (peakProxy < MenuSparkKickPeakRearmBelow)
            {
                this._menuSparkKickProxyArmed = true;
            }

            float kickProxyPeakRise = Mathf.Max(0f, peakProxy - this._menuSparkPrevKickPeakAtProxy);

            float spectralFlux = 0f;
            float maxDrumBandJump = 0f;
            for (int j = 0; j < bandCount; j++)
            {
                float lv = instantBandLevels[first + j];
                float rise = lv - this._menuSparkPrevDrumBandInstant[j];
                if (rise > 0f)
                {
                    spectralFlux += rise;
                }

                if (rise > maxDrumBandJump)
                {
                    maxDrumBandJump = rise;
                }
            }

            this._menuSparkPrevKickPeakAtProxy = peakProxy;
            for (int j = 0; j < bandCount; j++)
            {
                this._menuSparkPrevDrumBandInstant[j] = instantBandLevels[first + j];
            }

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
            bool attackLikeKick = maxTransientOverMean >= MenuSparkTransientOverMeanMin
                && maxDrumBandJump >= MenuSparkMaxBandJumpMin;

            bool energyGate = drumAverage >= MenuSparkPercussiveMinBandEnergy
                || peakProxy >= MenuSparkKickBandPeakMin;
            if (!energyGate)
            {
                return;
            }

            float crest = bandMax / (drumAverage + 1e-5f);
            float fluxNeed = Mathf.Max(MenuSparkPercussiveFluxMin, drumAverage * MenuSparkPercussiveFluxOverAvgMin);
            bool kickProxyOnset = attackLikeKick
                && this._menuSparkKickProxyArmed
                && kickProxyPeakRise >= MenuSparkKickBandPeakRiseMin
                && peakProxy >= MenuSparkKickBandPeakMin;
            bool drumOnset = attackLikeKick
                && drumAverage >= MenuSparkPercussiveMinBandEnergy
                && spectralFlux >= fluxNeed
                && crest >= MenuSparkPercussiveCrestMin;
            bool percussiveOnset = kickProxyOnset || drumOnset;
            if (!percussiveOnset)
            {
                return;
            }

            if (Time.time < this._menuSparkNextEmitTime)
            {
                return;
            }

            if (!_stageSparks.isPlaying)
            {
                _stageSparks.Play();
            }

            float levelForHit = Mathf.Max(drumAverage, lvKick, peakProxy);
            float fluxForHit = Mathf.Max(spectralFlux, kickProxyPeakRise, maxTransientOverMean, maxDrumBandJump);
            float hitIntensity = Mathf.Clamp01(
                (SparkIntensityFromAvg * levelForHit)
                + (SparkIntensityFromJump * Mathf.Min(fluxForHit * MenuSparkTierFluxIntensityMul, MenuSparkTierFluxIntensityCap)));
            if (XrPerfHelper.ShouldReduceVisualizerCost())
            {
                hitIntensity *= 0.65f;
            }

            float tierLinear = Mathf.Clamp01(hitIntensity * MenuSparkTierSelectScale);
            float tierMetric = Mathf.Pow(tierLinear, MenuSparkTierCurveExponent);
            this.EmitStageSparksBurstFromBaked(this.SelectSparkTier(tierMetric));
            this._menuSparkNextEmitTime = Time.time + MenuSparkBurstMinIntervalSeconds;
            if (kickProxyOnset)
            {
                this._menuSparkKickProxyArmed = false;
            }
        }

        /// <summary>音圧（Scale＋Pow 済み tierMetric）で低・中・高のベイクセットを選ぶ。</summary>
        private int SelectSparkTier(float hitIntensity)
        {
            if (hitIntensity < MenuSparkTierBoundaryLowMid)
            {
                return 0;
            }

            if (hitIntensity < MenuSparkTierBoundaryMidHigh)
            {
                return 1;
            }

            return 2;
        }

        /// <summary>ベイク済みパラメータをそのまま Emit（Random・spread 再計算なし）。</summary>
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

        /// <summary>
        /// 低・中・高の代表 hit（burst）と代表ベース粒数で 3 セット分を事前生成。軽量モード時の粒数削減もここで反映。
        /// </summary>
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

                    // ティアごとの代表「音の強さ」0..1 と代表ベース粒数（段差を詰めて低・中・高の差が付きすぎないように）
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
                    float tierLifeExtra = tier == 0 ? 1f : (tier == 1 ? MenuSparkLifetimeMidTierExtraMul : MenuSparkLifetimeHighTierExtraMul);
                    float ringRadius = (CircleRadius * 0.94f) * Mathf.Lerp(SparkBurstShapeRadiusMulMin, SparkBurstShapeRadiusMulMax, burstRepr);
                    float hitIntensityRepr = burstRepr;
                    float particleSizeMax = tier == 0
                        ? SparkParticleSizeMax
                        : (tier == 1 ? SparkParticleSizeMax * SparkParticleSizeMidTierMaxMul : SparkParticleSizeMax * SparkParticleSizeHighTierMaxMul);

                    var arr = new SparkBakedEmit[SparkBakeMaxPerTier];
                    // 発生位置はリング上に等間隔（n 角形の頂点）。半径のばらつきは付けずステージ円に揃える。
                    float angleStep = (Mathf.PI * 2f) / Mathf.Max(1, scaledCount);
                    for (int i = 0; i < scaledCount; i++)
                    {
                        float pint = Mathf.Clamp01(hitIntensityRepr * UnityEngine.Random.Range(0.9f, 1.05f));
                        float life = UnityEngine.Random.Range(MenuSparkLifetimeMin, MenuSparkLifetimeMax) * lifeMul * tierLifeExtra;
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

        /// <summary>ステージ火花用 ParticleSystem の初期状態。実際の発生は rate 0 + EmitParams で行う。</summary>
        private void BuildStageSparkParticles()
        {
            var sparksGo = new GameObject("stageSpectrumSparks");
            sparksGo.transform.SetParent(_root.transform, false);
            // ステージ円の中心付近（わずかに Y 上げて床めり込みを避ける）— StageVisualizer と同一
            sparksGo.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            sparksGo.transform.localRotation = Quaternion.identity;

            _stageSparks = sparksGo.AddComponent<ParticleSystem>();
            var ps = _stageSparks;

            // --- Main: デフォルト値（EmitParams が無い経路はほぼ無いが、カーブ・重力・色の基準）
            var main = ps.main;
            main.playOnAwake = true; // シーン上で常時シミュレーション可能に
            main.loop = true; // duration を繰り返し（継続 Emit 向け）
            main.duration = 5f; // 1 ループの長さ（loop とセット）
            main.startLifetime = new ParticleSystem.MinMaxCurve(MenuSparkLifetimeMin, MenuSparkLifetimeMax); // Emit 省略時の寿命レンジ
            main.startSpeed = 0f; // 初速は EmitParams.velocity に任せる（二重加算を防ぐ）
            main.startSize = new ParticleSystem.MinMaxCurve(SparkParticleSizeMin, SparkParticleSizeMax); // Emit 省略時のサイズレンジ
            main.maxParticles = 512; // 同時に生存できる粒の上限（Stage と同一）
            main.simulationSpace = ParticleSystemSimulationSpace.Local; // 親の位置・回転に追従
            main.gravityModifier = 0.35f; // 世界重力に対する倍率（下向き加速）
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f); // スプライトの向きのばらつき（ランダム回転）
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.97f, 0.88f, 1f), // 明るい白〜クリーム
                new Color(1f, 0.72f, 0.35f, 1f)); // オレンジ寄り（火花の色味）

            // --- Emission: 自動連続放出はオフ（スペクトラムイベントで Emit のみ）
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            // --- Shape: 手動 Emit では applyShapeToPosition=false のため位置には使わない。Play 時の見た目・デバッグ用の基準円 — Stage と同一
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = CircleRadius * 0.94f; // ステージ円に合わせた半径（メニューは拡大後の CircleRadius）
            shape.radiusThickness = 1f; // 1 = 円周近傍から出る（塗りつぶし円ではない）
            shape.arc = 360f; // 全周
            shape.rotation = new Vector3(90f, 0f, 0f); // デフォルトの円面を XZ（水平リング）に立てる
            shape.randomDirectionAmount = 0f; // 形状からのランダム方向成分なし（方向は velocity で指定）

            // --- Velocity over Lifetime: オフ（初速は Emit のみ、軌道は重力で曲がる）
            var vel = ps.velocityOverLifetime;
            vel.enabled = false;

            // --- Size over Lifetime: 前半はほぼそのまま、終盤だけしぼむ（中〜高がパッと消えない）
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

            sizeOl.size = new ParticleSystem.MinMaxCurve(1f, shrink); // startSize × カーブ

            // --- Color over Lifetime: 終わりに向けてゆるくフェード（きらっと一瞬で消えない）
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
            renderer.renderMode = ParticleSystemRenderMode.Billboard; // 常にカメラ向きの板
            renderer.alignment = ParticleSystemRenderSpace.Facing; // ビュー方向に合わせる
            if (sparkShader != null)
            {
                _sparkMaterial = new Material(sparkShader);
                _sparkMaterial.renderQueue = 3200; // 透明メッシュより手前に描きたいときの目安
                renderer.material = _sparkMaterial;
            }

            ps.Clear();
            this.BakeStageSparkBurstTemplates();
            ps.Play();
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
            if (this._menuMeshShaderActive)
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
        }

        public void Initialize()
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }

            this._audioSpectrum.Band = AudioSpectrum.BandType.ThirtyOneBand;
            this._audioSpectrum.fallSpeed = 1f;
            this._audioSpectrum.sensibility = 10f;
        }

        /// <summary>メニュー設定のオンオフに合わせ、同一セッション内でメッシュ／火花の生成と破棄を毎フレーム同期する。</summary>
        public void LateTick()
        {
            if (!PluginConfig.Instance.Enable)
            {
                if (this._built)
                {
                    this.TearDownMenuStageVisuals();
                }

                return;
            }

            bool want = PluginConfig.Instance.MenuStageVisualizer;
            if (want && !this._built)
            {
                this.BuildMenuStageVisuals();
            }
            else if (!want && this._built)
            {
                this.TearDownMenuStageVisuals();
            }
        }

        private void BuildMenuStageVisuals()
        {
            if (this._built)
            {
                return;
            }

            if (MenuFloorRootController.MenuVisualizerFloorRoot == null)
            {
                return;
            }

            Shader shader = VisualizerUtil.GetShader("Custom/StageSpectrumFloor")
                ?? VisualizerUtil.GetShader("Sprites/Default");
            if (shader == null)
            {
                return;
            }

            this._menuMeshShaderActive = string.Equals(shader.name, "Custom/StageSpectrumFloor", StringComparison.Ordinal);

            this.BuildRadialSquaresMesh();

            this._root = new GameObject("stageSpectrumVisualizer");
            this._root.transform.SetParent(MenuFloorRootController.MenuVisualizerFloorRoot.transform, false);
            this._root.transform.localPosition = new Vector3(0f, 0.0001f, 0f);
            this._root.transform.localScale = Vector3.one;

            this._meshFilter = this._root.AddComponent<MeshFilter>();
            this._meshFilter.sharedMesh = this._mesh;

            this._meshRenderer = this._root.AddComponent<MeshRenderer>();
            this._material = new Material(shader);
            this._meshRenderer.material = this._material;

            if (this._menuMeshShaderActive)
            {
                this._menuPeakUploadBuffer = new float[32];
                this._material.SetFloat("_SquaresPerBand", SquaresPerBand);
                this._material.SetFloat("_Merihari", PluginConfig.Instance.enableMerihari ? 1f : 0f);
            }

            this.BuildStageSparkParticles();
            this._menuSparkPrevDrumBandInstant = null;
            this._menuSparkPrevDrumInstantPrimed = false;
            this._menuSparkPrevKickPeakAtProxy = 0f;
            this._menuSparkKickProxyArmed = true;
            this._menuSparkNextEmitTime = 0f;

            this.RefreshMenuSaberHsvFromSettings();

            this._built = true;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;
        }

        private void TearDownMenuStageVisuals()
        {
            if (!this._built)
            {
                return;
            }

            if (this._audioSpectrum != null)
            {
                this._audioSpectrum.UpdatedRawSpectrums -= this.OnUpdatedRawSpectrums;
            }

            if (this._root != null)
            {
                if (this._meshFilter != null)
                {
                    this._meshFilter.sharedMesh = null;
                    this._meshFilter = null;
                }

                this._meshRenderer = null;
                UnityEngine.Object.Destroy(this._root);
                this._root = null;
            }

            if (this._mesh != null)
            {
                UnityEngine.Object.Destroy(this._mesh);
                this._mesh = null;
            }

            if (this._material != null)
            {
                UnityEngine.Object.Destroy(this._material);
                this._material = null;
            }

            if (this._sparkMaterial != null)
            {
                UnityEngine.Object.Destroy(this._sparkMaterial);
                this._sparkMaterial = null;
            }

            this._stageSparks = null;
            this._sparkTierBaked = null;
            this._sparkTierEmitCounts = null;
            this._menuSparkPrevDrumBandInstant = null;
            this._menuSparkPrevDrumInstantPrimed = false;
            this._menuSparkPrevKickPeakAtProxy = 0f;
            this._menuSparkKickProxyArmed = true;
            this._menuPeakUploadBuffer = null;
            this._menuMeshShaderActive = false;
            this._colors = null;
            this._built = false;
        }

        /// <summary>
        /// プレイ中のセイバー列挙はせず、ゲーム設定のセイバー色だけ反映（メニュー向け）。
        /// ColorManager → ColorScheme の順。どちらも無いときは赤／青の既定。
        /// </summary>
        private void RefreshMenuSaberHsvFromSettings()
        {
            Color a;
            Color b;
            if (this._colorManager != null)
            {
                a = this._colorManager.ColorForSaberType(SaberType.SaberA);
                b = this._colorManager.ColorForSaberType(SaberType.SaberB);
            }
            else if (this._colorScheme != null)
            {
                a = this._colorScheme.saberAColor;
                b = this._colorScheme.saberBColor;
            }
            else
            {
                a = new Color(0.65882355f, 0.1254902f, 0.1254902f, 1f);
                b = new Color(0.1254902f, 0.3921569f, 0.65882355f, 1f);
            }

            Color.RGBToHSV(a, out this._menuLeftHsv[0], out this._menuLeftHsv[1], out this._menuLeftHsv[2]);
            Color.RGBToHSV(b, out this._menuRightHsv[0], out this._menuRightHsv[1], out this._menuRightHsv[2]);
        }

        private bool _disposedValue;
        private AudioSpectrum _audioSpectrum;

        [Inject]
        public void Constructor(
            [Inject(Id = AudioSpectrum.BandType.ThirtyOneBand)] AudioSpectrum audioSpectrum,
            [InjectOptional] ColorManager colorManager,
            [InjectOptional] ColorScheme colorScheme)
        {
            this._audioSpectrum = audioSpectrum;
            this._colorManager = colorManager;
            this._colorScheme = colorScheme;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposedValue)
            {
                if (disposing)
                {
                    this.TearDownMenuStageVisuals();
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
