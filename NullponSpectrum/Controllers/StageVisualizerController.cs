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
        /// <summary>1 バンドあたり奥行（径方向）に並べる四角の個数</summary>
        private const int SquaresPerBand = 24;
        /// <summary>各四角の半辺長（接線方向・径方向とも同じで正方形）</summary>
        private const float SquareHalf = 0.008f;
        /// <summary>隣り合う四角のあいだの径方向の余白（面と面の間）</summary>
        private const float GapRadial = 0.007f;

        private static float RadialPitch => SquareHalf * 2f + GapRadial;

        /// <summary>
        /// 火花用ドラム帯の PeakLevels 先頭 index。5〜11 帯は中心周波数おおよそ 63〜250Hz（キックの胴・スネア下寄り）。
        /// この帯の平均が前フレームから跳ねたとき Emit。
        /// </summary>
        private const int SparkDrumBandIndexFirst = 5;
        /// <summary>ドラム帯域平均を取るときの PeakLevels の終端 index（含む）</summary>
        private const int SparkDrumBandIndexLast = 11;
        /// <summary>帯域平均がこの値未満なら火花なし（小さいノイズでの誤爆を抑える）</summary>
        private const float SparkMinDrumAverage = 0.034f;
        /// <summary>前フレーム比の上がり幅（jump）がこの値未満なら火花なし（ゆるい変化では出さない）</summary>
        private const float SparkDrumJumpMin = 0.014f;
        /// <summary>1 バーストあたりの Emit 数の下限</summary>
        private const int SparkEmitMin = 16;
        /// <summary>1 バーストあたりの Emit 数の上限</summary>
        private const int SparkEmitMax = 74;
        /// <summary>上方向初速の上限を決めるときの「目標上昇量」（m 相当。1 Unity unit ≒ 1m 想定）</summary>
        private const float SparkMaxRiseMeters = 0.6f;
        /// <summary>Emit 時 startSize の下限（弱い粒の見た目の大きさ）</summary>
        private const float SparkParticleSizeMin = 0.011f;
        /// <summary>Emit 時 startSize の上限（強い粒の見た目の大きさ）</summary>
        private const float SparkParticleSizeMax = 0.16f;
        /// <summary>パーティクルが消えるまでの時間（秒）の下限（Main のデフォルト帯。実 Emit は EmitParams で上書き）</summary>
        private const float SparkLifetimeMin = 0.1f;
        /// <summary>パーティクル寿命（秒）の上限</summary>
        private const float SparkLifetimeMax = 0.28f;
        /// <summary>上方向キャップ計算用の等価重力（g × main.gravityModifier）。軌道のざっくり見積りに使う</summary>
        private const float SparkGravityEffective = 9.81f * 0.35f;
        /// <summary>hitIntensity に入れる「平均レベル」側の係数（大きいほど静かなドラムでも強く反応）</summary>
        private const float SparkIntensityFromAvg = 3f;
        /// <summary>hitIntensity に入れる「前フレームからの跳ね」側の係数（大きいほどアタック感で強く反応）</summary>
        private const float SparkIntensityFromJump = 6f;
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
        /// <summary>AudioSpectrum のバンド数（例: ThirtyOneBand なら 31）</summary>
        private int _spectrumBandCount;
        /// <summary>左半円に spectrum 本 + 右半円に spectrum 本</summary>
        private int _columnCount;
        private bool _built;
        private ParticleSystem _stageSparks;
        private Material _sparkMaterial;
        private float _sparkPrevDrumAverage;
        private SparkBakedEmit[][] _sparkTierBaked;
        private int[] _sparkTierEmitCounts;

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
            if (!audio || !_built || _mesh == null || _colors == null)
            {
                return;
            }

            var peaks = this._audioSpectrum.PeakLevels;

            // 軽量モード時は頂点色 GPU アップロードを間引き（オブジェクト生成は Initialize のみでここでは行わない）
            bool uploadMeshColors = XrPerfHelper.ShouldUploadStageMeshColorsThisFrame();
            if (uploadMeshColors)
            {
                this._visualizerUtil.RefreshSaberColorsNow();

                float[] leftHsv = VisualizerUtil.GetLeftSaberHSV();
                float[] rightHsv = VisualizerUtil.GetRightSaberHSV();

                for (int col = 0; col < _columnCount; col++)
                {
                    bool isRight = col >= _spectrumBandCount;
                    int bandIdx = isRight ? col - _spectrumBandCount : col;
                    if (bandIdx < 0 || bandIdx >= peaks.Length)
                    {
                        continue;
                    }

                    float[] hsv = isRight ? rightHsv : leftHsv;
                    float peak = peaks[bandIdx];
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

                _mesh.colors = _colors;
            }

            UpdateStageSparks(peaks);
        }

        /// <summary>ドラム帯域のレベルと「前フレームからの跳ね」で火花バーストの有無・強さを決める。</summary>
        private void UpdateStageSparks(float[] peaks)
        {
            if (_stageSparks == null || peaks == null || peaks.Length == 0)
            {
                return;
            }

            int first = Mathf.Clamp(SparkDrumBandIndexFirst, 0, peaks.Length - 1);
            int last = Mathf.Clamp(SparkDrumBandIndexLast, 0, peaks.Length - 1);
            if (last < first)
            {
                return;
            }

            int bandCount = last - first + 1;
            float sum = 0f;
            for (int i = first; i <= last; i++)
            {
                sum += peaks[i];
            }

            // 指定バンドのピーク平均（キック〜スネア下寄り帯の「今の太さ」）
            float drumAverage = sum / bandCount;

            // 1 フレーム前の平均からの増分（アタック・立ち上がりを検出）
            float jump = drumAverage - _sparkPrevDrumAverage;
            _sparkPrevDrumAverage = drumAverage;

            if (drumAverage < SparkMinDrumAverage || jump < SparkDrumJumpMin)
            {
                return;
            }

            if (!_stageSparks.isPlaying)
            {
                _stageSparks.Play();
            }

            // 0..1 の総合強さ → 3 ティアのどれか（粒パラメータは Initialize 時にベイク済み）
            float hitIntensity = Mathf.Clamp01((SparkIntensityFromAvg * drumAverage) + (SparkIntensityFromJump * jump));
            this.EmitStageSparksBurstFromBaked(this.SelectSparkTier(hitIntensity));
        }

        /// <summary>音圧（hitIntensity）で低・中・高のどのベイクセットを使うか。</summary>
        private int SelectSparkTier(float hitIntensity)
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

        /// <summary>ベイク済みパラメータをそのまま Emit（Random・spread 再計算なし）。</summary>
        private void EmitStageSparksBurstFromBaked(int tier)
        {
            if (this._sparkTierBaked == null || this._sparkTierEmitCounts == null || tier < 0 || tier >= SparkTierCount)
            {
                return;
            }

            int n = this._sparkTierEmitCounts[tier];
            SparkBakedEmit[] layer = this._sparkTierBaked[tier];
            for (int i = 0; i < n; i++)
            {
                SparkBakedEmit p = layer[i];
                var emitParams = new ParticleSystem.EmitParams
                {
                    position = p.Position,
                    startLifetime = p.StartLifetime,
                    startSize = p.StartSize,
                    velocity = p.Velocity,
                    applyShapeToPosition = false
                };

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

                    // ティアごとの代表「音の強さ」0..1 と、旧ロジックに近い代表ベース粒数
                    float burstRepr = tier == 0 ? 0.24f : (tier == 1 ? 0.52f : 0.96f);
                    int baseCountRepr = tier == 0 ? 20 : (tier == 1 ? 38 : 58);

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
                    float ringRadius = (CircleRadius * 0.94f) * Mathf.Lerp(SparkBurstShapeRadiusMulMin, SparkBurstShapeRadiusMulMax, burstRepr);
                    float hitIntensityRepr = burstRepr;

                    var arr = new SparkBakedEmit[SparkBakeMaxPerTier];
                    // 発生位置はリング上に等間隔（n 角形の頂点）。半径のばらつきは付けずステージ円に揃える。
                    float angleStep = (Mathf.PI * 2f) / Mathf.Max(1, scaledCount);
                    for (int i = 0; i < scaledCount; i++)
                    {
                        float pint = Mathf.Clamp01(hitIntensityRepr * UnityEngine.Random.Range(0.9f, 1.05f));
                        float life = UnityEngine.Random.Range(SparkLifetimeMin, SparkLifetimeMax) * lifeMul;
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
                            StartSize = Mathf.Lerp(SparkParticleSizeMin, SparkParticleSizeMax, pint),
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
            // ステージ円の中心付近（わずかに Y 上げて床めり込みを避ける）
            sparksGo.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            sparksGo.transform.localRotation = Quaternion.identity;

            _stageSparks = sparksGo.AddComponent<ParticleSystem>();
            var ps = _stageSparks;

            // --- Main: デフォルト値（EmitParams が無い経路はほぼ無いが、カーブ・重力・色の基準）
            var main = ps.main;
            main.playOnAwake = true; // シーン上で常時シミュレーション可能に
            main.loop = true; // duration を繰り返し（継続 Emit 向け）
            main.duration = 5f; // 1 ループの長さ（loop とセット）
            main.startLifetime = new ParticleSystem.MinMaxCurve(SparkLifetimeMin, SparkLifetimeMax); // Emit 省略時の寿命レンジ
            main.startSpeed = 0f; // 初速は EmitParams.velocity に任せる（二重加算を防ぐ）
            main.startSize = new ParticleSystem.MinMaxCurve(SparkParticleSizeMin, SparkParticleSizeMax); // Emit 省略時のサイズレンジ
            main.maxParticles = 512; // 同時に生存できる粒の上限
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

            // --- Shape: 手動 Emit では applyShapeToPosition=false のため位置には使わない。Play 時の見た目・デバッグ用の基準円
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = CircleRadius * 0.94f; // ステージ円に合わせた半径
            shape.radiusThickness = 1f; // 1 = 円周近傍から出る（塗りつぶし円ではない）
            shape.arc = 360f; // 全周
            shape.rotation = new Vector3(90f, 0f, 0f); // デフォルトの円面を XZ（水平リング）に立てる
            shape.randomDirectionAmount = 0f; // 形状からのランダム方向成分なし（方向は velocity で指定）

            // --- Velocity over Lifetime: オフ（初速は Emit のみ、軌道は重力で曲がる）
            var vel = ps.velocityOverLifetime;
            vel.enabled = false;

            // --- Size over Lifetime: 寿命に応じてサイズを 1→0（きらっと消える）
            var sizeOl = ps.sizeOverLifetime;
            sizeOl.enabled = true;
            AnimationCurve shrink = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
            sizeOl.size = new ParticleSystem.MinMaxCurve(1f, shrink); // startSize × カーブ

            // --- Color over Lifetime: 色を白→橙、透明度を 1→0（終端でフェードアウト）
            var colorOl = ps.colorOverLifetime;
            colorOl.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.55f, 0.15f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
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
            _colors = new Color[vCount];

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

                for (int m = 0; m < SquaresPerBand; m++)
                {
                    float centerDist = CircleRadius + m * RadialPitch;
                    var center = radial * centerDist;

                    int b = (col * SquaresPerBand + m) * 4;
                    vertices[b] = center - SquareHalf * tangent - SquareHalf * radial;
                    vertices[b + 1] = center + SquareHalf * tangent - SquareHalf * radial;
                    vertices[b + 2] = center + SquareHalf * tangent + SquareHalf * radial;
                    vertices[b + 3] = center - SquareHalf * tangent + SquareHalf * radial;

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
            _mesh.colors = _colors;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
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

            Shader shader = VisualizerUtil.GetShader("Sprites/Default");
            if (shader == null)
            {
                return;
            }

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

            BuildStageSparkParticles();
            _sparkPrevDrumAverage = 0f;

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

                    if (_sparkMaterial != null)
                    {
                        UnityEngine.Object.Destroy(_sparkMaterial);
                        _sparkMaterial = null;
                    }

                    _stageSparks = null;
                    _sparkTierBaked = null;
                    _sparkTierEmitCounts = null;
                    _colors = null;
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
