Shader "LinhThu/Daylight Shadow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        _ShadowFootprint ("Ground footprint", Vector) = (0,0.12,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" "DisableBatching"="True" }
        Cull Off Lighting Off ZWrite Off Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            float4 _SunShadow;
            float _SunOverhead;
            float4 _ShadowFootprint;
            v2f ShadowVert(appdata_t input)
            {
                v2f output = SpriteVert(input);
                float4 origin = mul(unity_ObjectToWorld, float4(0,0,0,1));
                float4 world = mul(unity_ObjectToWorld, UnityFlipSprite(input.vertex, _Flip));
                float2 offset = world.xy - origin.xy;
                float height = offset.y;
                float scaleY = length(mul(unity_ObjectToWorld, float4(0,1,0,0)).xy);
                float profile = saturate(_ShadowFootprint.w);
                float overhead = _SunOverhead * profile;
                float castDepth = _ShadowFootprint.w > 2.5 ? .8 : lerp(.12, .42, profile);
                float depth = lerp(castDepth, _ShadowFootprint.y, overhead);
                float2 ground = float2(offset.x, height * depth - _ShadowFootprint.x * scaleY * overhead * depth);
                ground += _SunShadow.xy * (_ShadowFootprint.w >= 1.5 ? height : max(0, height)) * _SunShadow.z;
                // Trees and rocks use an affine projection: folded triangles would darken their own shadows.
                float project = _ShadowFootprint.w > .5 && _ShadowFootprint.w < 1.5 ? smoothstep(0, max(.001, _ShadowFootprint.z * scaleY), height) : 1;
                world.xy = origin.xy + lerp(offset, ground, project);
                if (_ShadowFootprint.w > 1.5 && _ShadowFootprint.w < 2.5)
                {
                    float canopy = smoothstep(_ShadowFootprint.z * scaleY, _ShadowFootprint.z * scaleY * 1.35, height);
                    output.color.a *= lerp(1, canopy, overhead);
                }
                output.vertex = mul(UNITY_MATRIX_VP, world);
                return output;
            }
            fixed4 ShadowFrag(v2f input) : SV_Target
            {
                fixed alpha = smoothstep(.08, .35, SampleSpriteTexture(input.texcoord).a) * input.color.a * _SunShadow.w;
                return fixed4(0,0,0,alpha);
            }
            ENDCG
        }
    }
}
