// Full-screen dimmer with a single soft-edged rounded-rect "hole" that is left undarkened.
// Based on Unity's built-in UI-Default.shader (clip-rect handling, stencil block) with an added
// rounded-box SDF cutout in the fragment stage. _HoleRect/_CornerRadius/_Softness are driven at
// runtime by TutorialSpotlightMaskController; _HasHole toggles between "spotlight" and a flat
// full-screen dim (no bounds supplied).
Shader "Watermelon/UI/TutorialSpotlightMask"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _DimColor ("Dim Color", Color) = (0,0,0,0.75)
        _HoleRect ("Hole Rect (xMin,yMin,xMax,yMax)", Vector) = (0,0,0,0)
        _CornerRadius ("Corner Radius", Float) = 24
        _Softness ("Softness", Float) = 12
        _HasHole ("Has Hole", Float) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

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
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _DimColor;
            float4 _HoleRect;
            float _CornerRadius;
            float _Softness;
            float _HasHole;
            float4 _ClipRect;
            float4 _MainTex_ST;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                // Unity's stock UI shaders (mis)name this "worldPosition" but it's really the
                // vertex in the graphic's own local rect space — the same space RectTransform
                // corner math (InverseTransformPoint) and RectTransformUtility's local-point
                // conversion produce, which is exactly the space TutorialBoundsHelper's rects
                // and the raycast filter's hit test are computed in.
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            // Rounded-box SDF (Inigo Quilez) — negative inside the box, positive outside.
            float RoundedBoxSDF(float2 p, float2 halfSize, float radius)
            {
                float2 q = abs(p) - halfSize + radius;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 color = _DimColor;

                if (_HasHole > 0.5)
                {
                    float2 holeCenter = (_HoleRect.xy + _HoleRect.zw) * 0.5;
                    float2 holeHalfSize = max((_HoleRect.zw - _HoleRect.xy) * 0.5, 0.0);

                    float dist = RoundedBoxSDF(IN.worldPosition.xy - holeCenter, holeHalfSize, _CornerRadius);
                    color.a *= smoothstep(0.0, max(_Softness, 0.001), dist);
                }

                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);

                return color;
            }
            ENDCG
        }
    }
}
