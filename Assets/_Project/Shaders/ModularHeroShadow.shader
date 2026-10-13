Shader "LinhThu/Modular Hero Shadow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        [PerRendererData] _GroundOrigin ("Actor Foot", Vector) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" "DisableBatching"="True" }
        Cull Off Lighting Off ZWrite Off Blend One OneMinusSrcAlpha
        Stencil { Ref 234 Comp NotEqual Pass Replace }
        Pass
        {
            CGPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            float4 _SunShadow, _GroundOrigin;
            v2f ShadowVert(appdata_t input)
            {
                v2f output = SpriteVert(input);
                float4 world = mul(unity_ObjectToWorld, UnityFlipSprite(input.vertex, _Flip));
                float2 offset = world.xy - _GroundOrigin.xy;
                float height = max(0, offset.y);
                world.xy = _GroundOrigin.xy + float2(offset.x, height * .16) + _SunShadow.xy * height * _SunShadow.z;
                output.vertex = mul(UNITY_MATRIX_VP, world);
                return output;
            }
            fixed4 ShadowFrag(v2f input) : SV_Target
            {
                fixed alpha = SampleSpriteTexture(input.texcoord).a;
                clip(alpha - .12);
                return fixed4(0,0,0, _SunShadow.w * input.color.a);
            }
            ENDCG
        }
    }
}
