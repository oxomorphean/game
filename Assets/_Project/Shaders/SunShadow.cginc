#ifndef LINHTHU_SUN_SHADOW_INCLUDED
#define LINHTHU_SUN_SHADOW_INCLUDED
#include "UnitySprites.cginc"

float4 _SunShadow, _DaylightShade, _GroundOrigin, _ShadowVolume, _ShadowContact;
float _SunOverhead;
float4 _MainTex_TexelSize;

fixed FoliageCoverage(float2 uv)
{
    fixed4 sample = SampleSpriteTexture(uv);
    return sample.a * step(sample.r * _ShadowContact.z, sample.b)
        * step(sample.g + .025, sample.b) * step(.32, max(sample.r, sample.b));
}

struct SunShadowVaryings
{
    float4 vertex : SV_POSITION;
    float2 uv : TEXCOORD0;
    float height : TEXCOORD1;
    fixed4 color : COLOR;
    UNITY_VERTEX_OUTPUT_STEREO
};

SunShadowVaryings SunShadowVert(appdata_t input)
{
    SunShadowVaryings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    float4 world = mul(unity_ObjectToWorld, UnityFlipSprite(input.vertex, _Flip));
    float2 foot = lerp(mul(unity_ObjectToWorld, float4(0,0,0,1)).xy, _GroundOrigin.xy, _GroundOrigin.w);
    float2 offset = world.xy - foot;
    float referenceHeight = max(.001, _ShadowVolume.x);
    float contact = smoothstep(0, referenceHeight * _ShadowContact.x, offset.y);
    // Reconstruct ground depth separately from elevation. Ground-level points stay fixed.
    // Low cross-sections stay narrow; upper silhouettes retain their full width.
    float level = smoothstep(0, referenceHeight * _ShadowContact.x * 2, offset.y);
    float sectionWidth = lerp(1, max(_ShadowContact.x, level), _SunOverhead);
    float across = offset.x / max(.001, referenceHeight * _ShadowContact.w * .5 * sectionWidth);
    float thickness = sqrt(saturate(1 - across * across));
    float groundY = lerp(offset.y, (offset.y - referenceHeight * _ShadowVolume.w * _SunOverhead) * _ShadowVolume.z * thickness, contact);
    float canopy = step(.001, _ShadowContact.z);
    float trunk = canopy * (1 - step(.5, input.color.a));
    // Elevated foliage keeps an affine silhouette; a tapered trunk volume joins it to the foot.
    float foliageY = (offset.y - referenceHeight * _ShadowVolume.w * _SunOverhead) * _ShadowVolume.z;
    float trunkY = offset.y * _ShadowVolume.z * (1 - _SunOverhead) + (input.color.r - .5) * referenceHeight * _ShadowContact.x * .35;
    groundY = lerp(groundY, lerp(foliageY, trunkY, trunk), canopy);
    float elevation = max(0, offset.y - groundY) * _ShadowVolume.y;
    world.xy = foot + float2(offset.x, groundY) + _SunShadow.xy * elevation * _SunShadow.z;
    output.vertex = mul(UNITY_MATRIX_VP, world);
    output.uv = input.texcoord;
    output.height = offset.y / referenceHeight;
    output.color = input.color * _Color * _RendererColor;
    return output;
}

fixed4 SunShadowFrag(SunShadowVaryings input) : SV_Target
{
    float cutoff = _ShadowContact.y;
    float canopy = step(.001, _ShadowContact.z);
    float trunk = canopy * (1 - step(.5, input.color.a));
    fixed alpha = SampleSpriteTexture(input.uv).a * input.color.a;
    if (canopy > .5 && trunk < .5)
    {
        float2 radius = _MainTex_TexelSize.xy * 8;
        fixed coverage = FoliageCoverage(input.uv);
        coverage = max(coverage, FoliageCoverage(input.uv + float2(radius.x, 0)));
        coverage = max(coverage, FoliageCoverage(input.uv - float2(radius.x, 0)));
        coverage = max(coverage, FoliageCoverage(input.uv + float2(0, radius.y)));
        coverage = max(coverage, FoliageCoverage(input.uv - float2(0, radius.y)));
        alpha = coverage * input.color.a;
    }
    alpha = lerp(alpha, 1, trunk);
    clip(lerp(input.height - cutoff, 1, trunk));
    clip(alpha - .12);
    alpha = _SunShadow.w;
    return fixed4(_DaylightShade.rgb * alpha, alpha);
}
#endif
