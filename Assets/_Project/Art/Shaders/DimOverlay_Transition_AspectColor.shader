Shader "Custom/UI/DimOverlay_Transition_AspectColor"
{
    Properties
    {
        // UI(Image/MaskableGraphic) 필수
        _MainTex ("Main Texture", 2D) = "white" {}

        _MaskTex ("Hole Mask (A)", 2D) = "white" {}
        _InvertMask ("Invert Mask", Float) = 0

        // 트랜지션 진행도
        _Progress ("Progress", Range(0,1)) = 1

        // 구멍 최소/최대 크기
        _MinScale ("Min Hole Scale", Range(0.001, 3)) = 0.2
        _MaxScale ("Max Hole Scale", Range(0.001, 30)) = 8

        // 옵션: Progress를 알파에 곱할지
        _UseProgressAlpha ("Use Progress As Alpha", Float) = 1

        _DimColor ("Dim Color (RGBA)", Color) = (0,0,0,0.8)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask RGBA

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
            };

            sampler2D _MainTex;

            sampler2D _MaskTex;
            float4 _MaskTex_TexelSize;
            float _InvertMask;

            float _Progress;
            float _MinScale;
            float _MaxScale;
            float _UseProgressAlpha;

            fixed4 _DimColor;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = saturate(_Progress);

                // 구멍 스케일
                float holeScale = lerp(_MaxScale, _MinScale, t);

                float2 centered = i.uv - 0.5;

                // 비율 유지 (Fit)
                float targetAspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);
                float texAspect    = _MaskTex_TexelSize.z / max(_MaskTex_TexelSize.w, 1.0);

                if (targetAspect > texAspect)
                    centered.x *= (targetAspect / texAspect);
                else
                    centered.y *= (texAspect / targetAspect);

                centered /= max(holeScale, 0.001);
                float2 scaledUV = centered + 0.5;

                float outside =
                    (scaledUV.x < 0.0 || scaledUV.x > 1.0 ||
                     scaledUV.y < 0.0 || scaledUV.y > 1.0) ? 1.0 : 0.0;

                float m = tex2D(_MaskTex, scaledUV).a;
                if (_InvertMask > 0.5) m = 1.0 - m;

                m = max(m, outside);
                m = saturate(m);

                fixed4 col = _DimColor;

                // 🔑 핵심 옵션
                float alphaMul = lerp(1.0, t, step(0.5, _UseProgressAlpha));
                col.a *= (m * alphaMul);

                return col;
            }
            ENDCG
        }
    }

    FallBack Off
}
