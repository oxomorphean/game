Shader "LinhThu/Dash Afterimage"
{
    Properties { _MainTex ("Pose sprite", 2D) = "white" {} _Color ("Jade tint", Color) = (.52,.94,.82,.28) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _Color;
            struct input { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
            output vert(input v) { output o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
            fixed4 frag(output i):SV_Target { float a=tex2D(_MainTex,i.uv).a*_Color.a; return fixed4(_Color.rgb*a,a); }
            ENDCG
        }
    }
}
