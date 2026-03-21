using NullponSpectrum.AudioSpectrums;
using NullponSpectrum.Configuration;
using NullponSpectrum.Utilities;
using System;
using UnityEngine;
using Zenject;

namespace NullponSpectrum.Controllers
{
    /// <summary>
    /// 床面（XZ）上の横向き長方形（幅 3・奥行 1.5）の範囲内で、ドラム寄り帯域のスペクトラムに反応してパーティクルを出す。
    /// StageVisualizerController の火花と同じ ThirtyOneBand index 5〜11・しきい値ロジックを踏襲。
    /// </summary>
    public class ParticleVisualizerController : IInitializable, IDisposable
    {
        /// <summary>長方形の半幅（全幅 3）</summary>
        private const float RectHalfWidth = 1.5f;
        /// <summary>長方形の半奥行（全奥行 1.5）</summary>
        private const float RectHalfDepth = 0.75f;

        /// <summary>ドラム反応に使う PeakLevels の先頭 index（ThirtyOneBand。5〜11 は約 63〜250Hz 帯＝キック胴・スネア下寄り）</summary>
        private const int DrumBandIndexFirst = 5;
        /// <summary>ドラム反応に使う PeakLevels の終端 index（含む）</summary>
        private const int DrumBandIndexLast = 11;
        /// <summary>上記バンド平均がこの値未満なら Emit しない（小さいノイズでの誤爆を抑える）</summary>
        private const float MinDrumAverage = 0.024f;
        /// <summary>前フレーム比の上がり幅（jump）がこの値未満なら Emit しない（ゆるい変化では出さない）</summary>
        private const float DrumJumpMin = 0.01f;
        /// <summary>1 バーストあたりの Emit 数の下限（UpdateDrumPlaneParticles の Clamp に効く）</summary>
        private const int EmitMin = 32;
        /// <summary>1 バーストあたりの Emit 数の上限</summary>
        private const int EmitMax = 128;
        /// <summary>Emit 時 startSize の下限（弱いヒット寄りの粒の見た目）</summary>
        private const float ParticleSizeMin = 0.03f;
        /// <summary>Emit 時 startSize の上限（強いヒット寄りの粒の見た目）</summary>
        private const float ParticleSizeMax = 0.064f;
        /// <summary>パーティクル寿命（秒）の下限（EmitParams で上書き。Main はデフォルト帯）</summary>
        private const float LifetimeMin = 0.15f;
        /// <summary>パーティクル寿命（秒）の上限</summary>
        private const float LifetimeMax = 0.45f;
        /// <summary>床面（XZ）方向の初速の下限（m/s 相当の目安。Y 成分は 0）</summary>
        private const float PlaneSpeedMin = 0.12f;
        /// <summary>床面（XZ）方向の初速の上限</summary>
        private const float PlaneSpeedMax = 0.95f;
        /// <summary>hitIntensity に入れる「帯域平均」側の係数（大きいほど静かなドラムでも強く反応）</summary>
        private const float IntensityFromAvg = 3f;
        /// <summary>hitIntensity に入れる「前フレームからの跳ね」側の係数（大きいほどアタック感で強く反応）</summary>
        private const float IntensityFromJump = 6f;
        /// <summary>発生位置のローカル Y（床めり込み・Z ファイトを避けるためわずかに浮かせる）</summary>
        private const float EmitY = 0.02f;
        /// <summary>count 計算式「jump × この値」: 跳ね幅に対する粒数の感度（大きいほど同じ jump で粒が増える）</summary>
        private const float EmitCountJumpScale = 640f;
        /// <summary>count 計算式「drumAverage × この値」: 平均レベルに対する粒数の感度</summary>
        private const float EmitCountAvgScale = 64f;
        /// <summary>1 スペクトラムコールバックあたり最大 Emit 回数（バースト全体を複数フレームに分散してフレームタイムのギザつきを抑える）</summary>
        private const int EmitParticlesPerFrameBudget = 28;
        /// <summary>未処理 Emit のキュー上限（溜まりすぎ防止）</summary>
        private const int EmitPendingQueueMax = 220;

        private GameObject _root;
        private ParticleSystem _particles;
        private ParticleSystemRenderer _particleRenderer;
        private Material _particleMaterial;
        /// <summary>ホタル風の柔らかい発光用（中心明るく外周フェード）。実行時生成し Dispose で破棄。</summary>
        private Texture2D _particleGlowTexture;
        private float _prevDrumAverage;
        private int _pendingEmitCount;
        private float _pendingEmitPint;
        private bool _built;

        private AudioSpectrum _audioSpectrum;
        private VisualizerUtil _visualizerUtil;

        [Inject]
        public void Constructor([Inject(Id = AudioSpectrum.BandType.ThirtyOneBand)] AudioSpectrum audioSpectrum, VisualizerUtil visualizerUtil)
        {
            this._audioSpectrum = audioSpectrum;
            this._visualizerUtil = visualizerUtil;
        }

        private void OnUpdatedRawSpectrums(AudioSpectrum obj)
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }
            if (!PluginConfig.Instance.ParticleVisualizer)
            {
                return;
            }
            this.UpdateFromSpectrum(obj);
        }

        private void UpdateFromSpectrum(AudioSpectrum audio)
        {
            if (!audio || !this._built || this._particles == null)
            {
                return;
            }

            // ドラムでキューに積み、毎フレームは上限数だけ Emit（1 フレームに集中させない）
            this.UpdateDrumPlaneParticles(this._audioSpectrum.PeakLevels);
            this.DrainParticleEmitBudget();
        }

        /// <summary>指定ドラム帯の平均と「前フレームからの跳ね」で Emit の有無・粒数を決める（Stage 火花と同系）。</summary>
        private void UpdateDrumPlaneParticles(float[] peaks)
        {
            if (this._particles == null || peaks == null || peaks.Length == 0)
            {
                return;
            }

            int first = Mathf.Clamp(DrumBandIndexFirst, 0, peaks.Length - 1);
            int last = Mathf.Clamp(DrumBandIndexLast, 0, peaks.Length - 1);
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

            // 帯域内ピークの平均＝「今のドラムの太さ」
            float drumAverage = sum / bandCount;
            // 1 フレーム前との差＝アタック・立ち上がり
            float jump = drumAverage - this._prevDrumAverage;
            this._prevDrumAverage = drumAverage;

            if (drumAverage < MinDrumAverage || jump < DrumJumpMin)
            {
                return;
            }

            if (!this._particles.isPlaying)
            {
                this._particles.Play();
            }

            // 1 バーストの粒数: 跳ね（アタック）と平均レベルの両方で増やし、EmitMin〜EmitMax でクリップ
            int count = Mathf.Clamp(
                Mathf.RoundToInt(jump * EmitCountJumpScale + drumAverage * EmitCountAvgScale),
                EmitMin,
                EmitMax);
            if (XrPerfHelper.ShouldReduceVisualizerCost())
            {
                int capMax = Mathf.Max(EmitMin, EmitMax / 2);
                int capMin = Mathf.Max(8, EmitMin / 2);
                count = Mathf.Clamp(Mathf.RoundToInt(count * 0.5f), capMin, capMax);
            }
            // 0..1 の総合強さ（初速・サイズ・アルファのブレンドに使う）
            float hitIntensity = Mathf.Clamp01((IntensityFromAvg * drumAverage) + (IntensityFromJump * jump));
            this._pendingEmitCount = Mathf.Min(this._pendingEmitCount + count, EmitPendingQueueMax);
            this._pendingEmitPint = Mathf.Max(this._pendingEmitPint, hitIntensity);
        }

        /// <summary>キューに溜まった Emit を 1 フレーム分だけ処理する。</summary>
        private void DrainParticleEmitBudget()
        {
            if (this._particles == null || this._pendingEmitCount <= 0)
            {
                return;
            }

            int n = Mathf.Min(EmitParticlesPerFrameBudget, this._pendingEmitCount);
            this._pendingEmitCount -= n;
            float pint = this._pendingEmitPint;
            if (this._pendingEmitCount <= 0)
            {
                this._pendingEmitPint = 0f;
            }

            this.EmitPlaneBurst(n, pint);
        }

        /// <summary>長方形内のランダム位置から、床面方向（XZ）へ飛ぶ粒を手動 Emit。Shape は無効のため位置はすべてここで指定。</summary>
        private void EmitPlaneBurst(int count, float hitIntensity)
        {
            this._visualizerUtil.RefreshSaberColorsNow();

            float[] leftHsv = VisualizerUtil.GetLeftSaberHSV();
            float[] rightHsv = VisualizerUtil.GetRightSaberHSV();
            Color colorLeft = Color.HSVToRGB(leftHsv[0], leftHsv[1], 1f);
            Color colorRight = Color.HSVToRGB(rightHsv[0], rightHsv[1], 1f);

            // このバースト全体の強さ 0..1（粒ごとにさらにランダムを掛ける）
            float pint = Mathf.Clamp01(hitIntensity);

            for (int i = 0; i < count; i++)
            {
                // 左右セイバー色の間をランダム補間
                float t = UnityEngine.Random.value;
                Color c = Color.Lerp(colorLeft, colorRight, t);
                // 強いヒットほど不透明に近づける（アルファの下限 0.65 〜 上限 0.95）
                c.a = Mathf.Lerp(0.65f, 0.95f, pint);

                // 指定長方形（XZ）内のランダム座標
                float x = UnityEngine.Random.Range(-RectHalfWidth, RectHalfWidth);
                float z = UnityEngine.Random.Range(-RectHalfDepth, RectHalfDepth);
                Vector3 pos = new Vector3(x, EmitY, z);

                // 床面内の飛び方向（単位ベクトル）
                Vector2 dir2 = UnityEngine.Random.insideUnitCircle;
                if (dir2.sqrMagnitude < 1e-6f)
                {
                    dir2 = Vector2.right;
                }
                dir2.Normalize();

                // 初速: Min〜Max を pint で補間し、粒ごとに 0.85〜1.1 倍でばらつき
                float speed = Mathf.Lerp(PlaneSpeedMin, PlaneSpeedMax, pint * UnityEngine.Random.Range(0.85f, 1.1f));
                // 弱いヒットでは全体を 0.55〜1 倍で抑える
                speed *= Mathf.Lerp(0.55f, 1f, pint);
                // Y は 0 のまま＝平面上のスライド
                Vector3 vel = new Vector3(dir2.x * speed, 0f, dir2.y * speed);

                float life = UnityEngine.Random.Range(LifetimeMin, LifetimeMax);

                var emitParams = new ParticleSystem.EmitParams
                {
                    position = pos,
                    startLifetime = life,
                    // 粒ごとにサイズに ±5% のばらつき
                    startSize = Mathf.Lerp(ParticleSizeMin, ParticleSizeMax, pint * UnityEngine.Random.Range(0.9f, 1.05f)),
                    velocity = vel,
                    startColor = c,
                    // false: Shape モジュールを位置に足さない（長方形内の手動座標と二重にならない）
                    applyShapeToPosition = false
                };

                this._particles.Emit(emitParams, 1);
            }
        }

        /// <summary>
        /// 四角いデフォルトクワッドをやめ、中心が明るく外側へ滑らかに消える円形アルファ（＋弱いハロー）のテクスチャ。
        /// Particles/Additive と組み合わせてホタル・小さな発光粒の見た目にする。
        /// </summary>
        private static Texture2D CreateFireflyGlowTexture(int size = 64)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "NullponParticleFireflyGlow";
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

        /// <summary>矩形ドラムパーティクル用 ParticleSystem の初期状態。連続放出はオフで、スペクトラムに応じて Emit のみ。</summary>
        private void BuildParticleSystem()
        {
            var go = new GameObject("particleRectDrumParticles");
            go.transform.SetParent(this._root.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            this._particles = go.AddComponent<ParticleSystem>();
            var ps = this._particles;

            // --- Main: デフォルトの寿命・サイズ帯（実際の 1 粒は多くが EmitParams で上書き）
            var main = ps.main;
            main.playOnAwake = true; // シーン上でシミュレーション可能に
            main.loop = true; // duration を繰り返し
            main.duration = 5f; // 1 ループの長さ（loop とセット）
            main.startLifetime = new ParticleSystem.MinMaxCurve(LifetimeMin, LifetimeMax); // Emit 省略時の寿命レンジ
            main.startSpeed = 0f; // 初速は EmitParams.velocity のみ（二重加算を防ぐ）
            main.startSize = new ParticleSystem.MinMaxCurve(ParticleSizeMin, ParticleSizeMax); // Emit 省略時のサイズレンジ
            // 同時生存数の上限。EmitMax やヒット頻度を上げるなら一緒に増やすこと
            // 同時生存を抑えめにしてオーバードロー・シミュレーション負荷のピークを平らにする
            main.maxParticles = 420;
            main.simulationSpace = ParticleSystemSimulationSpace.Local; // 親（床ルート）に追従
            main.gravityModifier = 0f; // 床面演出のため落下なし（平面上の初速だけ）
            // 円形ソフトテクスチャなので回転しても同じ見た目（ランダム回転オフでわずかに軽量化）
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 0f);

            // --- Emission: 毎フレームの自動放出はゼロ（ドラム検出時だけ Emit）
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            // --- Shape: 位置は EmitParams で指定するため無効（有効にすると発生位置の解釈が変わる）
            var shape = ps.shape;
            shape.enabled = false;

            // --- Size over Lifetime: 寿命に応じて startSize × (1→0) で縮小
            var sizeOl = ps.sizeOverLifetime;
            sizeOl.enabled = true;
            AnimationCurve shrink = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
            sizeOl.size = new ParticleSystem.MinMaxCurve(1f, shrink);

            // --- Color over Lifetime: Emit のセイバー色を活かしつつ、寿命後半でやわらかく消える（ホタルっぽい残光）
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
            this._particleRenderer = go.GetComponent<ParticleSystemRenderer>();
            this._particleRenderer.renderMode = ParticleSystemRenderMode.Billboard; // 常にカメラ向きの板ポリ
            this._particleRenderer.alignment = ParticleSystemRenderSpace.Facing; // ビュー方向に合わせる
            if (shader != null)
            {
                this._particleMaterial = new Material(shader);
                this._particleMaterial.renderQueue = 3200; // 透明オブジェクトの描画順の目安
                this._particleGlowTexture = CreateFireflyGlowTexture(64);
                this._particleMaterial.mainTexture = this._particleGlowTexture;
                this._particleRenderer.material = this._particleMaterial;
            }

            ps.Clear();
            ps.Play();

            if (XrPerfHelper.ShouldReduceVisualizerCost())
            {
                var mainCap = this._particles.main;
                mainCap.maxParticles = 300;
            }
        }

        public void Initialize()
        {
            if (!PluginConfig.Instance.Enable)
            {
                return;
            }
            if (!PluginConfig.Instance.ParticleVisualizer)
            {
                return;
            }

            this._audioSpectrum.Band = AudioSpectrum.BandType.ThirtyOneBand;
            this._audioSpectrum.fallSpeed = 1f;
            this._audioSpectrum.sensibility = 10f;

            this._root = new GameObject("particleRectVisualizer");
            this._root.transform.SetParent(FloorViewController.visualizerFloorRoot.transform, false);
            this._root.transform.localPosition = Vector3.zero;
            this._root.transform.localScale = Vector3.one;

            this.BuildParticleSystem();
            this._prevDrumAverage = 0f;
            this._pendingEmitCount = 0;
            this._pendingEmitPint = 0f;
            this._built = true;
            this._audioSpectrum.UpdatedRawSpectrums += this.OnUpdatedRawSpectrums;
        }

        private bool _disposedValue;

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

                    if (this._root != null)
                    {
                        UnityEngine.Object.Destroy(this._root);
                        this._root = null;
                    }

                    if (this._particleMaterial != null)
                    {
                        UnityEngine.Object.Destroy(this._particleMaterial);
                        this._particleMaterial = null;
                    }

                    if (this._particleGlowTexture != null)
                    {
                        UnityEngine.Object.Destroy(this._particleGlowTexture);
                        this._particleGlowTexture = null;
                    }

                    this._particles = null;
                    this._particleRenderer = null;
                    this._built = false;
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
