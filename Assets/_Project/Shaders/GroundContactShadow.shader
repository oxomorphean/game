Shader "LinhThu/Ground Contact Shadow"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off Lighting Off ZWrite Off Blend One OneMinusSrcAlpha
        Pass
        {
            // Shared coverage bit keeps intersecting ground shadows from stacking darkness.
            Stencil { Ref 64 ReadMask 64 WriteMask 64 Comp NotEqual Pass Replace }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _DaylightShade, _SunShadow;
            struct Input { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Output { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            Output vert(Input input)
            {
                Output output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }
            fixed4 frag(Output input) : SV_Target
            {
                float radius = length(input.uv * 2 - 1);
                float alpha = (1 - smoothstep(.12, 1, radius)) * clamp(_SunShadow.w, .18, .28);
                clip(alpha - .001);
                return fixed4(_DaylightShade.rgb * alpha, alpha);
            }
            ENDCG
        }
    }
}
