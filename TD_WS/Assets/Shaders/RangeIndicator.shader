Shader "Custom/RangeIndicator"
{
    Properties
    {
        _Color ("Indicator Color", Color) = (0, 0.8, 1, 0.5)
        _RimThickness ("Rim Thickness", Range(0.005, 0.2)) = 0.02
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            fixed4 _Color;
            float _RimThickness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate distance from center (0.5, 0.5)
                float2 uvCenter = i.uv - float2(0.5, 0.5);
                float dist = length(uvCenter);

                // Anti-aliasing helper
                float antiAlias = fwidth(dist);

                // Define boundaries of the thin circle ring
                float outerEdge = 0.5;
                float innerEdge = 0.5 - _RimThickness;

                // Smooth mask for outer circle edge
                float outerMask = smoothstep(outerEdge, outerEdge - antiAlias, dist);
                
                // Smooth mask for inner circle edge (completely transparent inside)
                float innerMask = smoothstep(innerEdge - antiAlias, innerEdge, dist);

                // Combine them to form a perfect, hollow ring
                float ringMask = outerMask * innerMask;

                fixed4 col = _Color;
                col.a *= ringMask;

                return col;
            }
            ENDCG
        }
    }
}

