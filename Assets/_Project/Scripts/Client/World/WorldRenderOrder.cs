using UnityEngine;

namespace LinhThu.Client.World
{
    /// <summary>Fixed world bands, independent of camera position and map width.</summary>
    public static class WorldRenderOrder
    {
        public const int Grass = -10000, Road = -9500, Surface = -9200;
        public const int CastShadow = -8400, ContactShadow = -8200;
        public const int EntityMinimum = -5000, EntityMaximum = 5000;
        public const int Effects = 6000, Overhead = 6500, Damage = 6600;
        public static int AtGround(float y) => Mathf.Clamp(Mathf.RoundToInt(-y * 100), EntityMinimum, EntityMaximum);
    }
}
