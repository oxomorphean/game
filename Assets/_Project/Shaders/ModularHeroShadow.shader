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
        [PerRendererData] _GroundOrigin ("Ground contact", Vector) = (0,0,0,0)
        [PerRendererData] _ShadowVolume ("Height, elevation scale, depth, center", Vector) = (1,1,.4,.52)
        [PerRendererData] _ShadowContact ("Contact band, silhouette cutoff, width", Vector) = (.12,-1,0,.75)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" "DisableBatching"="True" }
        Cull Off Lighting Off ZWrite Off Blend One OneMinusSrcAlpha
        UsePass "LinhThu/Daylight Shadow/SUNSHADOW"
    }
}
