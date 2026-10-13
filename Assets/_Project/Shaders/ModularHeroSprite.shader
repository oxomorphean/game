Shader "LinhThu/Modular Hero Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        [PerRendererData] _EyeTint ("Iris Color", Color) = (.25,.75,.65,1)
        [PerRendererData] _DyeEyes ("Recolor Iris", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment HeroFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            float4 _DaylightTint, _EyeTint;
            float _DyeEyes;
            fixed4 HeroFrag(v2f input) : SV_Target
            {
                fixed4 color = SampleSpriteTexture(input.texcoord) * input.color;
                float iris = smoothstep(.08, .25, max(color.g, color.b) - color.r) * _DyeEyes;
                color.rgb = lerp(color.rgb, _EyeTint.rgb * max(color.r, max(color.g, color.b)), iris);
                color.rgb *= _DaylightTint.rgb * color.a;
                return color;
            }
            ENDCG
        }
    }
}
