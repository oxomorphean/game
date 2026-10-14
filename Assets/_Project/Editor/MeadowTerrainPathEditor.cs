using LinhThu.Prototype;
using UnityEditor;
using UnityEngine;

namespace LinhThu.Editor
{
    [CustomEditor(typeof(MeadowTerrainPath))]
    public sealed class MeadowTerrainPathEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Kéo các điểm trong Scene để đổi đường; kéo tay nắm vàng để đổi bề rộng. Cây và collider không di chuyển theo đường.", MessageType.Info);
            if (DrawDefaultInspector()) ((MeadowTerrainPath)target).Rebuild();
        }

        private void OnSceneGUI()
        {
            var path = (MeadowTerrainPath)target;
            for (int i = 0; i < path.knots.Length; i++)
            {
                var knot = path.knots[i];
                var world = path.transform.TransformPoint(knot.position);
                Handles.color = new Color(.95f,.75f,.28f);
                Handles.Label(world + Vector3.up*.15f, "Đường " + (i + 1));
                EditorGUI.BeginChangeCheck();
                var moved = Handles.PositionHandle(world, Quaternion.identity);
                var direction = path.knots.Length < 2 ? Vector2.right : i == path.knots.Length - 1 ? knot.position - path.knots[i - 1].position : path.knots[i + 1].position - knot.position;
                var normal = new Vector3(-direction.y,direction.x,0).normalized;
                float width = Handles.ScaleSlider(knot.halfWidth,world,normal,Quaternion.identity,HandleUtility.GetHandleSize(world)*.65f,.05f);
                if (!EditorGUI.EndChangeCheck()) continue;
                Undo.RecordObject(path,"Chỉnh đường đất");
                knot.position = path.transform.InverseTransformPoint(moved);
                knot.halfWidth = Mathf.Max(.1f,width);
                path.knots[i] = knot;
                path.Rebuild();
                EditorUtility.SetDirty(path);
                PrefabUtility.RecordPrefabInstancePropertyModifications(path);
            }
        }
    }
}
