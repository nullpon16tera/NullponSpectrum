// ステージ床：1 枚の水平メッシュ。
// キックのたびに履歴へ追加（最大8）。各輪は独立して外へ伸び age で消える。
// 輪の半径・速度は _Intensity に依存させない（減衰で march が縮み「元に戻る」、新キックで再点灯するのを防ぐ）。
// _Intensity は早期リジェクト用にのみ使う。
Shader "Custom/StageProceduralSparks"
{
    Properties
    {
        _TintColor ("Tint", Color) = (1, 0.92, 0.78, 1)
        _RingRadius ("Ring Radius", Float) = 0.41
        _RingSoft ("Ring Softness", Range(0.001, 0.08)) = 0.022
        _SparkCore ("Ring Line Thickness", Range(0.003, 0.06)) = 0.018
        _SparkTail ("Ripple Reach", Range(0.01, 0.5)) = 0.26
        _Intensity ("Intensity", Float) = 0
        _Tier ("Tier", Float) = 0
        _KickTimesLo ("Kick Times Lo (internal)", Vector) = (-1,-1,-1,-1)
        _KickTimesHi ("Kick Times Hi (internal)", Vector) = (-1,-1,-1,-1)
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+110"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        Fog { Mode Off }
        Blend One One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            fixed4 _TintColor;
            float _RingRadius;
            float _RingSoft;
            float _SparkCore;
            float _SparkTail;
            float _Intensity;
            float _Tier;
            float4 _KickTimesLo;
            float4 _KickTimesHi;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 loc : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.loc = v.vertex.xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = float2(i.loc.x, i.loc.z);
                float r = length(p);

                float tierMul = lerp(0.88, 1.5, saturate(_Tier * 0.5));
                float t = _Time.y;
                float energy = saturate(_Intensity);

                // 外側への広がり：キックからの経過時間だけで決める（energy / 減衰と無関係）
                float expandDelta = _RingRadius * (0.48 + _SparkTail * 2.05);
                // 大きいほど外周まで早く到達（遅いと輪が滞留して重なり過光しやすい）
                float rippleSpeedNorm = 0.92;
                float sigma = max(0.008, min(_SparkCore * (0.95 + 0.1 * tierMul), expandDelta * 0.055));

                float acc = 0.0;

                #define RIPPLE_ONE(tc, dmul) \
                    { \
                        float tKick = tc; \
                        if (tKick > 1e-4) \
                        { \
                            float tr = t - tKick; \
                            if (tr >= 0.0 && tr < 4.2) \
                            { \
                                float march = tr * rippleSpeedNorm * expandDelta; \
                                if (march <= expandDelta * 1.12) \
                                { \
                                    float rf = _RingRadius + march; \
                                    float dr = r - rf; \
                                    float ringBand = exp(-0.5 * dr * dr / max(sigma * sigma, 1e-8)); \
                                    float age = exp(-tr * 0.58); \
                                    float reachF = exp(-2.35 * march / max(expandDelta, 0.06)); \
                                    acc += ringBand * age * reachF * dmul; \
                                } \
                            } \
                        } \
                    }

                RIPPLE_ONE(_KickTimesLo.x, 1.0);
                RIPPLE_ONE(_KickTimesLo.y, 0.94);
                RIPPLE_ONE(_KickTimesLo.z, 0.88);
                RIPPLE_ONE(_KickTimesLo.w, 0.82);
                RIPPLE_ONE(_KickTimesHi.x, 0.76);
                RIPPLE_ONE(_KickTimesHi.y, 0.7);
                RIPPLE_ONE(_KickTimesHi.z, 0.64);
                RIPPLE_ONE(_KickTimesHi.w, 0.58);

                #undef RIPPLE_ONE

                if (acc < 1e-6 && energy < 0.012)
                {
                    return fixed4(0, 0, 0, 0);
                }

                float ringW = max(_RingSoft * tierMul, 0.01);
                float innerEase = smoothstep(_RingRadius - ringW * 2.2, _RingRadius + sigma * 2.5, r);
                float outwardDist = max(0.0, r - _RingRadius);
                float screenFade = exp(-outwardDist / max(expandDelta * 1.25, 0.12));

                float ripple = acc * innerEase * screenFade * 1.38;

                float outCol = ripple * 1.35;
                // energy で全体明度を変えない（再点灯防止）。輪の減衰は age / reachF のみ。
                fixed3 rgb = _TintColor.rgb * outCol * _TintColor.a * 1.22;
                return fixed4(rgb, 0);
            }
            ENDCG
        }
    }
    FallBack Off
}
