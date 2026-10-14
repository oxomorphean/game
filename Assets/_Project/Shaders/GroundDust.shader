Shader "LinhThu/Ground Dust"
{
    Properties { _Color ("Tint", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
            fixed4 _Color;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.color=v.color*_Color; o.uv=v.uv; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=(i.uv-.5)*2;
                float radius=dot(p,p);
                fixed4 c=i.color;
                c.a *= 1-smoothstep(.05,1,radius);
                return c;
            }
            ENDCG
        }
    }
}
