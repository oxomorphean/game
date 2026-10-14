Shader "LinhThu/Meadow Terrain"
{
    Properties
    {
        _GrassTex ("Grass", 2D) = "white" {}
        _EarthTex ("Earth", 2D) = "white" {}
        _GrassScale ("Grass tile metres", Float) = 5.6
        _GrassBaseColor ("Grass base colour", Color) = (.41,.62,.115,1)
        _GrassContrast ("Grass texture contrast", Range(0,1)) = .7
        _GrassMipBias ("Grass texture softness", Range(0,5)) = .35
        _GrassVariation ("Grass broad colour variation", Range(0,.5)) = .24
        _GrassPatchScale ("Grass patch metres", Float) = 4.8
        _GrassWarmTint ("Sunlit grass tint", Color) = (1.12,1.06,.79,1)
        _GrassCoolTint ("Cool grass tint", Color) = (.70,.88,.90,1)
        _CoolPatch ("Cool shore patch: centre XY / radius XY", Vector) = (7.25,-.4,3,2)
        _DappledLight ("Dappled strength / metres / drift", Vector) = (.14,2.8,.025,0)
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
            float _GrassContrast, _GrassMipBias, _GrassVariation, _GrassPatchScale;
            float4 _GrassBaseColor, _GrassWarmTint, _GrassCoolTint, _CoolPatch;
            float4 _Color, _DaylightTint, _MapHalfSize, _DappledLight;
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
            float3 meadowGrass(float2 world)
            {
                float2 patch = world / max(.1,_GrassPatchScale);
                float2 bend = float2(noise(patch*.83+float2(12.7,4.1)),noise(patch*.83+float2(-3.2,19.8))) - .5;
                float broad = noise(patch+bend*.9+float2(27.4,9.6));
                float tone = broad*.78 + noise(patch*1.73+float2(-8.6,31.2))*.22;

                // Different orientations break the reflected repeats in the painted source.
                float2 uv = world / max(.1,_GrassScale);
                float2 first = float2(uv.x*.9323-uv.y*.3616,uv.x*.3616+uv.y*.9323)+float2(.173,.417);
                float2 second = float2(uv.x*.6381+uv.y*.7699,-uv.x*.7699+uv.y*.6381)*1.27+float2(.637,.291);
                float3 textureA = tex2Dbias(_GrassTex,float4(first,0,_GrassMipBias)).rgb;
                float3 textureB = tex2Dbias(_GrassTex,float4(second,0,_GrassMipBias)).rgb;
                float3 painted = lerp(textureA,textureB,.18+broad*.24);
                float3 grass = lerp(_GrassBaseColor.rgb,painted,_GrassContrast);
                float shore = 1-smoothstep(.3,1.35,length((world-_CoolPatch.xy)/max(_CoolPatch.zw,float2(.1,.1))));
                float sunlight = saturate(smoothstep(.15,.85,tone)-shore*.4);
                float3 tint = lerp(_GrassCoolTint.rgb,_GrassWarmTint.rgb,sunlight);
                return grass*lerp(float3(1,1,1),tint,saturate(_GrassVariation*2.5))*(1+(tone-.5)*_GrassVariation);
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float3 grass = meadowGrass(i.world);
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
                // Shared world-space light keeps the painted grass/path blend continuous.
                float2 drift = float2(.8,.35) * _Time.y * _DappledLight.z;
                float2 canopy = i.world / max(.1,_DappledLight.y) + drift;
                float shade = noise(canopy) * .7 + noise(canopy*2.1+float2(17,9)) * .3;
                float light = 1 + (smoothstep(.2,.8,shade)-.5) * _DappledLight.x;
                return fixed4(color * _Color.rgb * daylight * light * alpha,alpha);
            }
            ENDCG
        }
    }
}
