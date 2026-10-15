using UnityEngine;

namespace LinhThu.Client.Environment
{
    /// <summary>Shared authored prefab: ground dimensions and independently positioned props.</summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "LinhThu.Prototype", "LinhThu.Prototype", "ModularMapLayout")]
    public sealed class ModularMapLayout : MonoBehaviour
    {
        public SpriteRenderer ground;
        public Material ambientMaterial;
        public Vector2 size = new Vector2(20, 11.25f);
        public LinhThu.Domain.Navigation.WorldBounds WorldBounds => new LinhThu.Domain.Navigation.WorldBounds(
            new LinhThu.Domain.Navigation.WorldPoint(size.x * .5f, size.y * .5f));
        public void SynchronizeGroundBounds()
        {
            // Grass is sampled in world metres by MeadowTerrain.shader; growing X changes coverage only.
            foreach (var surface in GetComponentsInChildren<MeadowGroundSurface>())
            { surface.size = size; surface.Rebuild(); }
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
