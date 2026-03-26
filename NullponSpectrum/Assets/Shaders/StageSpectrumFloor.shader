// ステージ床メッシュ: 頂点 UV にバンド index・径方向段 index を持たせ、ピークは CPU から float 配列で渡す。
// 旧 CPU の DepthFillLevel / SquareColor / Nomalize と同じ式（StageVisualizerController と整合）。
Shader "Custom/StageSpectrumFloor"
{
    Properties
    {
        _SquaresPerBand ("Squares Per Band", Float) = 12
        _Merihari ("Merihari", Float) = 1
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+50"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
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

            float _SquaresPerBand;
            float _Merihari;
            float3 _LeftHsv;
            float3 _RightHsv;
            float _PeakLevels[32];

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float isRight : TEXCOORD1;
            };

            float Nomalize(float f)
            {
                float result = lerp(5.0, 1.0, f);
                return f * result;
            }

            float DepthFillLevel(float peakLevels)
            {
                if (_Merihari < 0.5)
                {
                    return saturate(lerp(0.0, 1.0, Nomalize(peakLevels * 3.0)));
                }
                return saturate(Nomalize(lerp(0.0, 1.0, peakLevels * 5.0)));
            }

            float3 HsvToRgb(float3 hsv)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(hsv.xxx + K.xyz) * 6.0 - K.www);
                return hsv.z * lerp(K.xxx, saturate(p - K.xxx), hsv.y);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.isRight = v.uv2.x;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                int bandIdx = (int)floor(i.uv.x + 0.5);
                bandIdx = clamp(bandIdx, 0, 31);
                float peak = _PeakLevels[bandIdx];

                int m = (int)floor(i.uv.y + 0.5);
                float spb = max(_SquaresPerBand, 1.0);
                m = clamp(m, 0, (int)spb - 1);
                float segT = (m + 1) / spb;

                float level = DepthFillLevel(peak);
                float3 hsv = i.isRight > 0.5 ? _RightHsv : _LeftHsv;

                if (level < segT)
                {
                    float3 rgbOff = HsvToRgb(float3(hsv.x, hsv.y, 0.0));
                    return fixed4(rgbOff, 0.0);
                }

                if (_Merihari < 0.5)
                {
                    float3 rgb = HsvToRgb(float3(hsv.x, hsv.y, 1.0));
                    return fixed4(rgb, 0.75);
                }

                float v = lerp(0.35, 1.0, saturate((segT + peak) * 0.5));
                float a = lerp(0.25, 0.85, Nomalize(peak * 4.0));
                float3 rgbLit = HsvToRgb(float3(hsv.x, hsv.y, Nomalize(v)));
                return fixed4(rgbLit, Nomalize(a));
            }
            ENDCG
        }
    }
    FallBack Off
}
