using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinhThu.Prototype;
using LinhThu.Prototype.Preview;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LinhThu.Editor
{
    /// <summary>Publishes lightweight art metadata for Editor play and WebGL, without changing art or scenes.</summary>
    public sealed class OwnerCatalogBuilder : IPreprocessBuildWithReport
    {
        public const string Output = "Assets/_Project/Resources/OwnerDashboard/catalog.json";
        public static readonly string[] Roots = { "Assets/_Project/Art", "Assets/_Project/Content", "Assets/_Project/Resources/Art", "Assets/_Project/Shaders" };
        public int callbackOrder => 0;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode) Generate();
        }

        public void OnPreprocessBuild(BuildReport report) => Generate();

        [MenuItem("Demo/Quản trị/Cập nhật danh mục asset")]
        public static void Generate()
        {
            var scenePaths = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var dependencies = new HashSet<string>(AssetDatabase.GetDependencies(scenePaths, true), StringComparer.Ordinal);
            var assets = new List<OwnerAsset>();
            var maps = new List<OwnerMap>();
            foreach (string path in AssetDatabase.FindAssets(string.Empty, Roots).Select(AssetDatabase.GUIDToAssetPath).Distinct().OrderBy(p => p, StringComparer.Ordinal))
            {
                if (AssetDatabase.IsValidFolder(path)) continue;
                string extension = Path.GetExtension(path).ToLowerInvariant();
                var type = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (type == null) continue;
                string kind, group;
                if (typeof(Texture).IsAssignableFrom(type) || extension == ".psd" || extension == ".psb") { kind = "Ảnh / texture"; group = "image"; }
                else if (extension == ".prefab") { kind = "Prefab"; group = "prefab"; }
                else if (extension == ".anim") { kind = "Clip animation"; group = "animation"; }
                else if (extension == ".controller") { kind = "Animator"; group = "animation"; }
                else if (extension == ".mat") { kind = "Material"; group = "other"; }
                else if (extension == ".shader") { kind = "Shader"; group = "other"; }
                else if (extension == ".unity") { kind = "Scene"; group = "other"; }
                else if (typeof(ScriptableObject).IsAssignableFrom(type)) { kind = "Definition"; group = "other"; }
                else continue;
                string id = AssetDatabase.AssetPathToGUID(path);
                bool inBuild = dependencies.Contains(path) || path.StartsWith("Assets/_Project/Resources/Art/", StringComparison.Ordinal);
                var row = new OwnerAsset { id = id, path = path, name = Path.GetFileNameWithoutExtension(path), kind = kind, group = group,
                    collection = path.StartsWith("Assets/_Project/Shaders/", StringComparison.Ordinal) ? "Shaders" : path.Split('/')[3], bytes = new FileInfo(path).Length, inBuild = inBuild };
                if (AssetImporter.GetAtPath(path) is TextureImporter texture) texture.GetSourceTextureWidthAndHeight(out row.width, out row.height);
                assets.Add(row);
                if (extension != ".prefab") continue;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var map = prefab.GetComponent<ModularMapLayout>();
                if (map == null) continue;
                maps.Add(new OwnerMap { id = id, name = row.name, path = path, width = map.size.x, height = map.size.y, inBuild = inBuild,
                    props = map.GetComponentsInChildren<ModularMapProp>(true).Length,
                    obstacles = map.GetComponentsInChildren<ObstacleAuthoring>(true).Length,
                    editableGround = map.GetComponentInChildren<MeadowGroundSurface>(true) != null,
                    pathKnots = map.GetComponentsInChildren<MeadowTerrainPath>(true).Sum(p => p.knots.Length) });
            }
            var catalog = new OwnerCatalog { generatedUtc = DateTime.UtcNow.ToString("O"), assets = assets.ToArray(),
                maps = maps.OrderByDescending(m => m.inBuild).ThenBy(m => m.name, StringComparer.Ordinal).ToArray() };
            Directory.CreateDirectory(Path.GetDirectoryName(Output));
            File.WriteAllText(Output, JsonUtility.ToJson(catalog, true) + "\n");
            AssetDatabase.ImportAsset(Output, ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
