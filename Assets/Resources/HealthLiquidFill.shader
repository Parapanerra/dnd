Shader "UI/TaruckLiquidFill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Fill ("Fill", Range(0,1)) = 1
        _Phase ("Phase", Float) = 0
        _Motion ("Motion", Range(0,1)) = 0
        _Impact ("Impact", Range(-1,1)) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _ClipRect;
            float _Fill;
            float _Phase;
            float _Motion;
            float _Impact;

            v2f vert(appdata input)
            {
                v2f output;
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 uv = input.texcoord;
                float wave = sin(uv.y * 11.0 + _Phase) * (0.025 + _Motion * 0.045);
                wave += sin(uv.y * 19.0 - _Phase * 1.3) * (0.008 + _Motion * 0.012);
                float edge = saturate(_Fill + wave);
                float inside = step(uv.x, edge);

                float distortion = sin(uv.x * 28.0 + uv.y * 7.0 - _Phase) * 0.03;
                distortion += sin(uv.x * 41.0 - uv.y * 9.0 + _Phase * 0.7) * 0.01;
                fixed4 color = tex2D(_MainTex, uv + float2(0.0, distortion)) * input.color;
                float current = sin(uv.x * 25.0 - _Phase * 1.7 + uv.y * 5.0);
                color.rgb *= 1.0 + current * (0.18 + _Motion * 0.10);

                float lineY = 0.52 + sin(uv.x * 22.0 - _Time.y * 3.0) * 0.1;
                lineY += sin(uv.x * 41.0 + _Time.y * 2.1) * 0.025;
                float flowingLine = 1.0 - smoothstep(0.035, 0.11, abs(uv.y - lineY));
                color.rgb = lerp(color.rgb, saturate(color.rgb + float3(0.35, 0.35, 0.35)),
                    flowingLine * (0.38 + _Motion * 0.17));

                float impactFront = 1.0 - smoothstep(0.0, 0.23, abs(uv.x - edge));
                float ripple = 0.65 + 0.35 * sin((edge - uv.x) * 70.0 - _Time.y * 24.0);
                float isBlue = step(color.r, color.b);
                float3 healTint = lerp(float3(1.0, 0.78, 0.56), float3(0.5, 0.9, 1.0), isBlue);
                float3 damageTint = lerp(float3(1.0, 0.38, 0.28), float3(0.58, 0.9, 1.0), isBlue);
                float3 impactTint = _Impact >= 0.0 ? healTint : damageTint;
                float bodyPulse = 0.25 + 0.13 * sin(uv.x * 55.0 - _Time.y * 30.0);
                color.rgb = lerp(color.rgb, impactTint,
                    saturate(abs(_Impact) * (impactFront * ripple * 0.75 + bodyPulse)));
                color.a *= inside;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
