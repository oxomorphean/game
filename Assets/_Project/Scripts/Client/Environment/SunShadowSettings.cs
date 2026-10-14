using System;
using UnityEngine;

namespace LinhThu.Client.Environment
{
    /// <summary>Authored volume approximation for a 2D silhouette, relative to its reference height.</summary>
    [Serializable]
    public struct SunShadowSettings
    {
        [Min(0)]
        public float heightScale;
        [Min(.05f)]
        public float width;
        [Range(.05f, 1)]
        public float depth;
        [Range(0, 1)]
        public float center;
        [Range(.01f, .5f)]
        public float contactHeight;
        [Range(-1, 1)]
        public float baseCutoff;
        [Tooltip("Selects a cool-colored elevated canopy from warm wood; zero uses the full silhouette.")]
        [Range(0, 1)]
        public float canopyBlueRatio;
        public static SunShadowSettings Default => new SunShadowSettings{heightScale = 1, width = .75f, depth = .4f, center = .52f, contactHeight = .12f, baseCutoff = -1};
        /// <summary>All layers of one object use the same foot position, dimensions and projection data.</summary>
        public void Apply(MaterialPropertyBlock properties, Vector3 foot, float referenceHeight)
        {
            properties.SetVector("_GroundOrigin", new Vector4(foot.x, foot.y, foot.z, 1));
            properties.SetVector("_ShadowVolume", new Vector4(referenceHeight, heightScale, depth, center));
            properties.SetVector("_ShadowContact", new Vector4(contactHeight, baseCutoff, canopyBlueRatio, width));
        }
    }
}
