Shader "LinhThu/Ground Contact Shadow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Radial footprint", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off Lighting Off ZWrite Off Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment frag
            #include "UnitySprites.cginc"
            fixed4 frag(v2f input) : SV_Target
            {
                float radius = length(input.texcoord * 2 - 1);
                float alpha = (1 - smoothstep(.05, 1, radius)) * input.color.a;
                clip(alpha - .001);
                return fixed4(input.color.rgb * alpha, alpha);
            }
            ENDCG
        }
    }
}
