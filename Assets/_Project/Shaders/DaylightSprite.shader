Shader "LinhThu/Daylight Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        [PerRendererData] _SurfaceRelief ("Sun-facing surface", Float) = 0
        [PerRendererData] _WaterSurface ("Pond water", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment DaylightFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            float4 _DaylightTint;
            float4 _DaylightSun;
            float _SurfaceRelief, _WaterSurface;
            fixed4 DaylightFrag(v2f input) : SV_Target
            {
                fixed4 color = SampleSpriteTexture(input.texcoord) * input.color;
                // Authored cel shading is retained; the broad key light follows the hidden sun.
                float side = (input.texcoord.x - .5) * 2;
                float direct = side * _DaylightSun.x * _SurfaceRelief;
                color.rgb *= 1 + direct;
                if (_WaterSurface > .5)
                {
                    // Isolate turquoise water so shore stones, reeds and lilies remain still.
                    float water = smoothstep(.09, .20, color.g - color.r) * smoothstep(.045, .14, color.b - color.r);
                    float2 uv = input.texcoord;
                    float wave = sin(uv.y * 165 + sin(uv.x * 26 + _Time.y * .52) * 2.8 - _Time.y * 1.3);
                    float broken = sin(uv.x * 74 + sin(uv.y * 43 - _Time.y * .37) * 2 + _Time.y * .8);
                    float sunX = .50 + _DaylightSun.x * .17;
                    float reflection = exp(-pow((uv.x - sunX) * 5.2, 2));
                    float glint = smoothstep(.82, .99, wave) * smoothstep(-.1, .8, broken) * reflection * water;
                    float3 reflectedSun = lerp(float3(.80, 1, .97), float3(1, .79, .39), _DaylightSun.z);
                    color.rgb *= 1 + water * sin(uv.y * 32 + sin(uv.x * 18) - _Time.y * .55) * .035;
                    color.rgb = lerp(color.rgb, reflectedSun, glint * lerp(.34, .58, _DaylightSun.z));
                }
                color.rgb *= _DaylightTint.rgb;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
