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
        [PerRendererData] _GroundOrigin ("Ground contact", Vector) = (0,0,0,0)
        [PerRendererData] _ShadowVolume ("Height, elevation scale, depth, center", Vector) = (1,1,.4,.52)
        [PerRendererData] _ShadowContact ("Contact band, silhouette cutoff, width", Vector) = (.12,-1,0,.75)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" "DisableBatching"="True" }
        Cull Off Lighting Off ZWrite Off Blend One OneMinusSrcAlpha
        Pass
        {
            Name "SunShadow"
            // One sun: overlapping body parts and casters cover the ground once.
            Stencil { Ref 64 ReadMask 64 WriteMask 64 Comp NotEqual Pass Replace }
            CGPROGRAM
            #pragma vertex SunShadowVert
            #pragma fragment SunShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "SunShadow.cginc"
            ENDCG
        }
    }
}
