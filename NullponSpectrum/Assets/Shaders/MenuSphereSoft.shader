// メニュー Sphere 専用。_TintColor / _Brightness はゲームの SaberBlade と同じ意味で扱い、
// 中心を暗くしない（旧版の InnerDim 等は廃止）。縁だけわずかに明るくして立体感だけ足す。
// ワールド座標＋時間でパーティクル風のキラキラ。縁依存なし。_Brightness が高い（動いている）ほど強く。
Shader "Custom/MenuSphereSoft"
{
    Properties
    {
        _TintColor ("Tint", Color) = (0,0,0,1)
        _Brightness ("Brightness", Float) = 0
        _RimPower ("Rim Power", Range(0.5, 10)) = 2.8
        _RimBoost ("Rim Boost", Range(0, 0.85)) = 0.28
        _SparkleIntensity ("Sparkle Intensity", Range(0, 4)) = 1.6
        _SparkleScale ("Sparkle Scale", Range(4, 80)) = 22
        _SparkleSpeed ("Sparkle Speed", Range(0.2, 8)) = 3.5
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+120"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Sphere"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        Fog { Mode Off }
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            fixed4 _TintColor;
            float _Brightness;
            float _RimPower;
            float _RimBoost;
            float _SparkleIntensity;
            float _SparkleScale;
            float _SparkleSpeed;

            float hash13(float3 p3)
            {
                p3 = frac(p3 * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float sparkleCell(float3 worldPos, float time, float scale, float speedMul, float3 phaseOff)
            {
                float3 p = worldPos * scale + phaseOff;
                float3 cell = floor(p);
                float h = hash13(cell);
                float h2 = hash13(cell + float3(13.1, 17.3, 19.7));
                float h3 = hash13(cell + float3(29.0, 41.0, 53.0));
                float t = time * _SparkleSpeed * speedMul;
                float flicker = sin(t * (0.65 + h * 0.9) + h2 * 6.2831853) * 0.5 + 0.5;
                // 以前は rarity * pow(...,14) で値が潰れて全滅していた。ベースのきらめき + 細かい点を分離。
                float grain = pow(flicker, 2.8) * (0.35 + 0.65 * h3);
                float pin = pow(saturate(flicker - 0.35 - h3 * 0.45), 5.0) * 2.2;
                return (grain + pin) * (0.45 + 0.55 * h);
            }

            float sparkles(float3 worldPos, float time)
            {
                float s = _SparkleScale;
                float a =
                    sparkleCell(worldPos, time, s, 1.0, float3(0, 0, 0)) +
                    sparkleCell(worldPos, time, s * 1.37, 1.25, float3(3.1, 5.2, 7.3)) * 0.75 +
                    sparkleCell(worldPos, time, s * 2.1, 1.6, float3(11.0, 13.0, 17.0)) * 0.5;
                return a * _SparkleIntensity;
            }

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 N = normalize(i.worldNormal);
                float3 V = normalize(UnityWorldSpaceViewDir(i.worldPos));
                float ndv = saturate(dot(N, V));
                float rim = pow(1.0 - ndv, max(_RimPower, 0.01));

                float b = saturate(_Brightness);
                float3 rgb = _TintColor.rgb * b;
                rgb *= 1.0 + _RimBoost * rim;

                // 縁ではなく、スペクトルで球が動いている（_Brightness が上がっている）ときほどキラを強く
                float activity = saturate(pow(b, 1.15) * 0.65 + b * 0.45);
                float sp = sparkles(i.worldPos, _Time.y) * activity;
                float3 sparkleCol = lerp(float3(1, 1, 1), _TintColor.rgb, 0.35);
                rgb += sp * sparkleCol;

                float a = _TintColor.a * b;
                return fixed4(saturate(rgb), saturate(a));
            }
            ENDCG
        }
    }
    FallBack Off
}
