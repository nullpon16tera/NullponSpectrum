// メニュー用スポットライト柱。床→天井（床で細く天井で広い円錐台）。Additive + 軽い埃ノイズ。
// 見た目が逆なら _FlipTaperHeight を 1 に。
Shader "Custom/MenuSpotlightBeam"
{
    Properties
    {
        _TintColor ("Tint", Color) = (1,1,1,1)
        _Brightness ("Brightness", Float) = 0
        _EdgeSoft ("Edge Softness (cap radial)", Range(0.05, 2)) = 0.6
        _ViewEdgePower ("View Edge Softness", Range(0.18, 2.2)) = 0.46
        _RimIntensity ("Rim Glow (silhouette soft)", Range(0, 1.2)) = 0.38
        _RimPower ("Rim Falloff", Range(0.35, 4)) = 1.75
        _VolumeFill ("Volume Fill (even brightness)", Range(0, 0.45)) = 0.12
        _RadiusFloorMul ("Floor Radius Mul", Range(0.02, 1)) = 0.18
        _RadiusCeilMul ("Ceiling Radius Mul", Range(0.15, 2)) = 1.0
        _FlipTaperHeight ("Flip Height Axis", Range(0, 1)) = 0
        _DustNoise ("Dust Noise", Range(0, 0.85)) = 0.35
        _DustScale ("Dust Scale", Range(2, 40)) = 14
        _FlickerSpeed ("Flicker Speed", Range(0, 12)) = 4.5
        _CameraDistanceAtten ("Camera Distance (0=縦グラデ優先)", Range(0, 0.15)) = 0
        _VertFadeFrom ("この高さまではフル (0=床 1=天井、0.5=中間まで)", Range(0, 0.92)) = 0.5
        _VertFadeGamma ("上方向フェードの曲げ (1=線形)", Range(1, 2.6)) = 1
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+110"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
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
            float _Brightness;
            float _EdgeSoft;
            float _ViewEdgePower;
            float _RimIntensity;
            float _RimPower;
            float _VolumeFill;
            float _RadiusFloorMul;
            float _RadiusCeilMul;
            float _FlipTaperHeight;
            float _DustNoise;
            float _DustScale;
            float _FlickerSpeed;
            float _CameraDistanceAtten;
            float _VertFadeFrom;
            float _VertFadeGamma;

            float hash(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float noise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n00 = hash(i + float3(0,0,0));
                float n10 = hash(i + float3(1,0,0));
                float n01 = hash(i + float3(0,1,0));
                float n11 = hash(i + float3(1,1,0));
                float n0 = lerp(lerp(n00, n10, f.x), lerp(n01, n11, f.x), f.y);
                float n00z = hash(i + float3(0,0,1));
                float n10z = hash(i + float3(1,0,1));
                float n01z = hash(i + float3(0,1,1));
                float n11z = hash(i + float3(1,1,1));
                float n1 = lerp(lerp(n00z, n10z, f.x), lerp(n01z, n11z, f.x), f.y);
                return lerp(n0, n1, f.z);
            }

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float radial : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float4 vpos = v.vertex;
                float t = saturate(vpos.y * 0.5 + 0.5);
                float tBlend = lerp(t, 1.0 - t, _FlipTaperHeight);
                float radialMul = lerp(_RadiusFloorMul, _RadiusCeilMul, tBlend);
                vpos.xz *= radialMul;
                o.pos = UnityObjectToClipPos(vpos);
                o.worldPos = mul(unity_ObjectToWorld, vpos).xyz;
                float r = length(vpos.xz);
                o.radial = r;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 wp = i.worldPos;
                float3 N = normalize(i.worldNormal);
                float3 V = normalize(_WorldSpaceCameraPos - wp);
                // 円柱の側面では radial が面内でほぼ一定のため、ラジアル減衰だけだと輪郭が硬い。
                // 視線と法線（筒の外向き）のなす角でフェード + グラージングのリムで縁をぼかす。
                float ndv = saturate(abs(dot(N, V)));
                float facing = pow(ndv, max(0.08, _ViewEdgePower));
                float rimTerm = _RimIntensity * pow(saturate(1.0 - ndv), max(0.2, _RimPower));
                float g = max(1.0, _VertFadeGamma);
                // 頂点カラー補間だと t が歪み「天井付近だけフェード」に見えやすい。ワールド→オブジェクト変換で軸上の高さを出す
                float3 objectPos = mul(unity_WorldToObject, float4(wp, 1.0)).xyz;
                float t = saturate(objectPos.y * 0.5 + 0.5);
                float span = max(1e-4, 1.0 - _VertFadeFrom);
                float hLin = (t <= _VertFadeFrom) ? 1.0 : saturate(1.0 - (t - _VertFadeFrom) / span);
                float h = pow(hLin, g);
                // rim/VolumeFill は視線で明るさが変わるので、縦 h も中身に掛ける
                float facingV = facing * lerp(0.22, 1.0, h);
                float sideCore = saturate(facingV + rimTerm * h + _VolumeFill * h);
                float capFade = smoothstep(0.0, 0.14, i.radial);
                float capRadial = 1.0 - saturate(i.radial * _EdgeSoft * 0.82);
                capRadial = pow(saturate(capRadial), 1.05) * capFade;
                float capBlend = saturate((abs(N.y) - 0.42) / 0.38);
                // 上蓋は capRadial の放射グラデを維持（capRadial*h だと h=0 で円盤全体が消える）
                float core = lerp(sideCore, capRadial, capBlend);

                float3 n3 = wp * _DustScale;
                float n = noise3(n3 + float3(_Time.y * 0.15, _Time.y * 0.35, _Time.y * 0.08));
                n += noise3(n3 * 2.1 + float3(0.0, _Time.y * 0.5, 0.0)) * 0.45;
                float dust = n * _DustNoise * core;

                float flick = sin(_Time.y * _FlickerSpeed + wp.x * 2.1 + wp.z * 1.7) * 0.5 + 0.5;
                flick = lerp(0.88, 1.05, flick);

                float camDist = length(_WorldSpaceCameraPos - wp);
                float distAtt = exp(-camDist * _CameraDistanceAtten);
                float b = saturate(_Brightness) * flick * distAtt * h;
                float3 rgb = _TintColor.rgb * (core * b + dust * b);
                float a = saturate(_TintColor.a * core * b + dust * b * 0.6);
                return fixed4(rgb, a);
            }
            ENDCG
        }
    }
    FallBack Off
}
