using System;
using LinhThu.Client.Configs;
using LinhThu.Domain.World;
using UnityEngine;

namespace LinhThu.Client.World
{
    public sealed class WorldNodeView : MonoBehaviour
    {
        public WorldNodeConfigSO config;
        public SpriteRenderer visual;
        public Renderer[] additionalVisuals = Array.Empty<Renderer>();
        public bool initiallyReady = true;
        public Color readyColor = Color.white;
        public Color growingColor = new Color(.65f, .72f, .65f, .7f);
        public WorldNodeEntity Entity { get; private set; }

        private MaterialPropertyBlock properties;
        public void Bind(WorldNodeEntity entity)
        {
            if (entity == null || config == null || entity.DefinitionId != config.Id || (visual == null && additionalVisuals.Length == 0))
                throw new InvalidOperationException("Node view/config mismatch.");
            Entity = entity;
            Sync();
        }

        public void Sync()
        {
            if (Entity == null)
                return;
            transform.position = new Vector3(Entity.Position.X, Entity.Position.Y, transform.position.z);
            bool full = Entity.IsReady || Entity.Spec.Kind == WorldNodeKind.TeleportPortal;
            var color = full ? readyColor : growingColor;
            if (visual != null)
                visual.color = color;
            if (additionalVisuals.Length > 0)
            {
                if (properties == null)
                    properties = new MaterialPropertyBlock();
                foreach (var renderer in additionalVisuals)
                {
                    renderer.GetPropertyBlock(properties);
                    properties.SetColor("_Color", color);
                    renderer.SetPropertyBlock(properties);
                }
            }
        }
    }
}
