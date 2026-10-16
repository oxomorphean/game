using System;
using System.Collections.Generic;
using System.Linq;
using LinhThu.Client.Actors;
using LinhThu.Client.Bootstrap;
using LinhThu.Domain.Combat;
using LinhThu.Domain.Core;
using LinhThu.Domain.World;
using UnityEngine;
using EntityId = LinhThu.Domain.Core.EntityId;

namespace LinhThu.Client.Combat
{
    /// <summary>Reads committed events/projectiles. Never applies gameplay effects.</summary>
    public sealed class TechniqueVfxPresenter : MonoBehaviour
    {
        public const float ProjectileHeight = .45f;
        public Material lineMaterial;
        public FloatingTextPresenter floatingTextPrefab;
        private WorldState world;
        private ContentRegistrySO registry;
        private IReadOnlyList<ActorView> actorViews;
        private IDisposable damageSubscription, abilitySubscription;
        private readonly Dictionary<EntityId, LineRenderer> projectiles = new Dictionary<EntityId, LineRenderer>();
        private readonly List<FloatingTextPresenter> texts = new List<FloatingTextPresenter>();
        private readonly List<(LineRenderer line, float until)> pulses = new List<(LineRenderer, float)>();
        public void Bind(WorldState value, ContentRegistrySO content, IReadOnlyList<ActorView> views)
        {
            if (lineMaterial == null || floatingTextPrefab == null)
                throw new InvalidOperationException("Combat presentation references are missing.");
            DisposeSubscriptions();
            foreach (var line in projectiles.Values)
                Destroy(line.gameObject);
            foreach (var pulse in pulses)
                Destroy(pulse.line.gameObject);
            foreach (var label in texts)
                label.gameObject.SetActive(false);
            projectiles.Clear();
            pulses.Clear();
            world = value;
            registry = content;
            actorViews = views ?? throw new ArgumentNullException(nameof(views));
            damageSubscription = world.Events.Subscribe<ActorDamagedEvent>(OnDamage);
            abilitySubscription = world.Events.Subscribe<AbilityExecutedEvent>(OnAbility);
        }

        private LineRenderer Line(Color color, int count)
        {
            var go = new GameObject("Technique effect");
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = lineMaterial;
            line.positionCount = count;
            line.startWidth = .08f;
            line.endWidth = .03f;
            line.startColor = color;
            line.endColor = color;
            line.sortingOrder = World.WorldRenderOrder.Effects;
            line.numCapVertices = 3;
            return line;
        }

        private void OnDamage(ActorDamagedEvent e)
        {
            var view = actorViews.FirstOrDefault(v => v.Entity?.Id == e.TargetId);
            if (view == null)
                return;
            var label = texts.FirstOrDefault(t => !t.gameObject.activeSelf);
            if (label == null)
            {
                if (texts.Count >= 32)
                    label = texts[0];
                else
                {
                    label = Instantiate(floatingTextPrefab, transform);
                    texts.Add(label);
                }
            }

            float offset = 0;
            foreach (var active in texts)
                if (active != label && active.gameObject.activeSelf && active.Owner == view)
                    offset = Mathf.Max(offset, active.TopOffset + .08f);
            label.Show("−" + (e.HpLost + e.ShieldLost).ToString("0"), view, new Color(1, .88f, .45f), offset);
        }

        private void OnAbility(AbilityExecutedEvent e)
        {
            var config = registry.Technique(e.TechniqueId);
            if (config.shape == DeliveryShape.Projectile || !world.TryGetActor(e.CasterId, out var actor))
                return;
            var line = Line(config.effectColor, 33);
            var p = actor.Transform.Position;
            for (int i = 0; i < 33; i++)
            {
                float a = i * Mathf.PI * 2 / 32;
                line.SetPosition(i, new Vector3(p.X + Mathf.Cos(a) * .5f, p.Y + .2f + Mathf.Sin(a) * .25f, 0));
            }

            pulses.Add((line, Time.time + .3f));
        }

        private void LateUpdate()
        {
            if (world == null)
                return;
            var active = new HashSet<EntityId>(world.Combat.Projectiles.Select(p => p.Id));
            foreach (var id in projectiles.Keys.Where(id => !active.Contains(id)).ToArray())
            {
                Destroy(projectiles[id].gameObject);
                projectiles.Remove(id);
            }

            foreach (var p in world.Combat.Projectiles)
            {
                if (!projectiles.TryGetValue(p.Id, out var line))
                {
                    line = Line(registry.Technique(p.TechniqueId).effectColor, 2);
                    projectiles.Add(p.Id, line);
                }

                line.SetPosition(0, new Vector3(p.Position.X, p.Position.Y + ProjectileHeight, 0));
                line.SetPosition(1, new Vector3(p.Position.X - p.Direction.X * .35f, p.Position.Y + ProjectileHeight - p.Direction.Y * .35f, 0));
            }

            for (int i = pulses.Count - 1; i >= 0; i--)
                if (Time.time >= pulses[i].until)
                {
                    Destroy(pulses[i].line.gameObject);
                    pulses.RemoveAt(i);
                }
        }

        private void DisposeSubscriptions()
        {
            damageSubscription?.Dispose();
            abilitySubscription?.Dispose();
        }

        private void OnDestroy() => DisposeSubscriptions();
    }
}
