using System;
using System.Linq;

namespace LinhThu.Prototype.Preview
{
    /// <summary>Build-time metadata for the owner's art inventory; contains no runtime asset references.</summary>
    [Serializable]
    public sealed class OwnerCatalog
    {
        public string generatedUtc;
        public OwnerAsset[] assets = Array.Empty<OwnerAsset>();
        public OwnerMap[] maps = Array.Empty<OwnerMap>();

        public OwnerAsset[] Search(string query, string group, bool buildOnly)
        {
            string term = (query ?? string.Empty).Trim();
            return assets.Where(a => (!buildOnly || a.inBuild) && (group == "all" || a.group == group)
                && (term.Length == 0 || a.name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0
                    || a.path.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();
        }
    }

    [Serializable]
    public sealed class OwnerAsset
    {
        public string id, name, path, kind, group, collection;
        public long bytes;
        public int width, height;
        public bool inBuild;
    }

    [Serializable]
    public sealed class OwnerMap
    {
        public string id, name, path;
        public float width, height;
        public int props, obstacles, pathKnots;
        public bool inBuild, editableGround;
    }
}
