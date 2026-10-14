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
        private void Awake()
        {
            if (GetComponent<DaylightPresentation>() == null)
                throw new System.InvalidOperationException("Map requires authored daylight presentation.");
            if (ambientMaterial == null)
                throw new System.InvalidOperationException("Map requires an assigned ambient material.");
            foreach (var prop in GetComponentsInChildren<ModularMapProp>())
                GardenAmbientMesh.Attach(prop, ambientMaterial);
        }
    }
}
