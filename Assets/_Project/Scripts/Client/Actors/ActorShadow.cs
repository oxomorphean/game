using System;
using LinhThu.Client.Environment;
using UnityEngine;

namespace LinhThu.Client.Actors
{
    public sealed class ActorShadow : MonoBehaviour
    {
        public SpriteRenderer[] sources = Array.Empty<SpriteRenderer>();
        public Material shadowMaterial;
        public SunShadowSettings settings = SunShadowSettings.Default;
        public float referenceHeight = 2.6f;
        private SpriteRenderer[] silhouettes;
        private SunShadowMesh[] shadows;
        private void Start()
        {
            if (shadowMaterial == null || sources.Length == 0)
                throw new InvalidOperationException("Actor shadow references are missing.");
            silhouettes = new SpriteRenderer[sources.Length];
            shadows = new SunShadowMesh[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                var node = new GameObject("Actor silhouette " + i);
                node.transform.SetParent(transform, false);
                var renderer = node.AddComponent<SpriteRenderer>();
                renderer.sharedMaterial = shadowMaterial;
                renderer.sortingOrder = -30;
                silhouettes[i] = renderer;
                shadows[i] = new SunShadowMesh(renderer);
            }
        }

        private void LateUpdate()
        {
            if (shadows == null)
                return;
            for (int i = 0; i < shadows.Length; i++)
            {
                var source = sources[i];
                var silhouette = silhouettes[i];
                silhouette.sprite = source.enabled && source.gameObject.activeInHierarchy ? source.sprite : null;
                silhouette.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
                var parentScale = transform.lossyScale;
                var worldScale = source.transform.lossyScale;
                silhouette.transform.localScale = new Vector3(worldScale.x / parentScale.x, worldScale.y / parentScale.y, 1);
                shadows[i].Refresh(settings, transform.position, referenceHeight);
            }
        }

        private void OnDestroy()
        {
            if (shadows != null)
                foreach (var shadow in shadows)
                    shadow?.Dispose();
        }
    }
}
