Shader "LinhThu/Meadow Terrain"
{
    Properties
    {
        _GrassTex ("Grass", 2D) = "white" {}
        _EarthTex ("Earth", 2D) = "white" {}
        _GrassScale ("Grass tile metres", Float) = 8.5
        _EarthScale ("Earth tile metres", Float) = 4.8
        _Path ("Earth path", Float) = 0
        _Color ("Tint", Color) = (1,1,1,1)
        _MapHalfSize ("Map limits", Vector) = (10,5.625,0,0)
    }
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
            sampler2D _GrassTex, _EarthTex;
            float _GrassScale, _EarthScale, _Path;
            float4 _Color, _DaylightTint, _MapHalfSize;
            struct appdata { float4 vertex : POSITION; float2 edge : TEXCOORD1; };
            struct v2f { float4 vertex : SV_POSITION; float2 world : TEXCOORD0; float2 edge : TEXCOORD1; float2 local : TEXCOORD2; };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xy;
                o.local = v.vertex.xy;
                o.edge = v.edge;
                return o;
            }
            float hash(float2 p) { return frac(sin(dot(p, float2(127.1,311.7))) * 43758.5453); }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p); f = f*f*(3-2*f);
                return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float3 grass = tex2D(_GrassTex, i.world / _GrassScale).rgb;
                grass *= lerp(.95,1.045,noise(i.world*.38));
                float alpha = 1;
                float3 color = grass;
                if (_Path > .5)
                {
                    clip(_MapHalfSize.xy - abs(i.local));
                    float rough = (noise(i.world * 7.5)*.72 + noise(i.world * 21)*.28 - .5) * .42;
                    float inset = (1 - abs(i.edge.x)) * i.edge.y + rough;
                    alpha = smoothstep(-.045,.02,inset);
                    float3 earth = tex2D(_EarthTex, i.world / _EarthScale).rgb;
                    color = lerp(grass,earth,smoothstep(.015,.17,inset));
                }
                float3 daylight = lerp(float3(1,1,1),_DaylightTint.rgb,step(.01,_DaylightTint.a));
                return fixed4(color * _Color.rgb * daylight * alpha,alpha);
            }
            ENDCG
        }
    }
}
