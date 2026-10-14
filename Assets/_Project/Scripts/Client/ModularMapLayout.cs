using UnityEngine;

namespace LinhThu.Prototype
{
    /// <summary>Shared authored prefab: ground dimensions and independently positioned props.</summary>
    public sealed class ModularMapLayout : MonoBehaviour
    {
        public SpriteRenderer ground;
        public Vector2 size = new Vector2(20, 11.25f);
        private void Awake()
        {
            DaylightPresentation.Ensure(gameObject);
            foreach (var prop in GetComponentsInChildren<ModularMapProp>()) GardenAmbientMesh.Attach(prop);
        }
    }
}
