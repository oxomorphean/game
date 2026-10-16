using UnityEngine;

namespace LinhThu.Client.Environment
{
    /// <summary>Shared authored prefab: ground dimensions and independently positioned props.</summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "LinhThu.Prototype", "LinhThu.Prototype", "ModularMapLayout")]
    public sealed class ModularMapLayout : MonoBehaviour
    {
        public SpriteRenderer ground;
        public Material ambientMaterial;
        public Vector2 worldCenter;
        public Vector2 size = new Vector2(20, 11.25f);
        public LinhThu.Domain.Navigation.WorldBounds WorldBounds => new LinhThu.Domain.Navigation.WorldBounds(
            new LinhThu.Domain.Navigation.WorldPoint(worldCenter.x, worldCenter.y),
            new LinhThu.Domain.Navigation.WorldPoint(size.x * .5f, size.y * .5f));
        // Decorative ground beyond the walkable edge lets the camera show a standing actor in full.
        public Vector2 PresentationPadding => new Vector2(2, 3);
        public LinhThu.Domain.Navigation.WorldBounds PresentationBounds => new LinhThu.Domain.Navigation.WorldBounds(
            new LinhThu.Domain.Navigation.WorldPoint(worldCenter.x, worldCenter.y),
            new LinhThu.Domain.Navigation.WorldPoint(size.x * .5f + PresentationPadding.x, size.y * .5f + PresentationPadding.y));
        public void SynchronizeGroundBounds()
        {
            // Grass is sampled in world metres by MeadowTerrain.shader; growing X changes coverage only.
            foreach (var surface in GetComponentsInChildren<MeadowGroundSurface>())
            { surface.size = size + PresentationPadding * 2; surface.center = worldCenter; surface.Rebuild(); }
            foreach (var path in GetComponentsInChildren<MeadowTerrainPath>())
            { path.mapSize = size; path.Rebuild(); }
        }
        private void Awake()
        {
            SynchronizeGroundBounds();
            if (GetComponent<DaylightPresentation>() == null)
                throw new System.InvalidOperationException("Map requires authored daylight presentation.");
            if (ambientMaterial == null)
                throw new System.InvalidOperationException("Map requires an assigned ambient material.");
            foreach (var prop in GetComponentsInChildren<ModularMapProp>())
                GardenAmbientMesh.Attach(prop, ambientMaterial);
        }
    }
}
