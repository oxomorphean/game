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
        [PerRendererData] _AppearanceTint ("Appearance Tint", Color) = (1,1,1,1)
        [PerRendererData] _HitFlash ("Hit Flash", Range(0,1)) = 0
        [PerRendererData] _HitFlashColor ("Hit Flash Color", Color) = (1,.95,.8,1)
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
            float4 _DaylightTint, _AppearanceTint, _HitFlashColor;
            float _HitFlash;
            fixed4 HeroFrag(v2f input) : SV_Target
            {
                fixed4 color = SampleSpriteTexture(input.texcoord) * input.color * _AppearanceTint;
                float3 daylight = lerp(float3(1,1,1), _DaylightTint.rgb, step(.001, dot(_DaylightTint.rgb, float3(1,1,1))));
                color.rgb *= daylight;
                color.rgb = lerp(color.rgb, _HitFlashColor.rgb, saturate(_HitFlash));
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
