using System;
using LinhThu.Client.Configs;
using LinhThu.Domain.Actors;
using UnityEngine;

namespace LinhThu.Client.Actors
{
    public sealed class ActorView : MonoBehaviour
    {
        public ActorBaseConfigSO config;
        public ActorFaction faction;
        public bool playerControlled;
        public bool rosterCompanion;
        public ActorAnimator actorAnimator;
        public ModularEquipmentRenderer equipment;
        public Transform singleSpriteVisual;
        public ActorEntity Entity { get; private set; }

        public void Bind(ActorEntity entity)
        {
            if (entity == null || config == null || entity.DefinitionId != config.Id)
                throw new InvalidOperationException("Actor view/config mismatch.");
            Entity = entity;
            Sync();
        }

        public void Unbind() => Entity = null;
        public void Sync()
        {
            if (Entity == null)
                return;
            var p = Entity.Transform.Position;
            transform.position = new Vector3(p.X, p.Y, transform.position.z);
            equipment?.SetFacing(Entity.Transform.FacingDirection.X);
            if (singleSpriteVisual != null && Mathf.Abs(Entity.Transform.FacingDirection.X) > .001f)
            {
                var scale = singleSpriteVisual.localScale;
                scale.x = Mathf.Abs(scale.x) * (Entity.Transform.FacingDirection.X < 0 ? -1 : 1);
                singleSpriteVisual.localScale = scale;
            }

            actorAnimator?.Present(Entity);
        }
    }
}
