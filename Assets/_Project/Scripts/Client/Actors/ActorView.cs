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
        public TrainingDummyRenderer trainingDummy;
        public Transform singleSpriteVisual;
        public FootstepDust footsteps;
        public ActionPoseController actionPose;
        public CharacterToolAttachmentPresenter tools;
        public UI.ActorOverheadPresenter overhead;
        public ActorEntity Entity { get; private set; }

        public Vector3 OverheadPosition
        {
            get
            {
                float top = transform.position.y;
                if (equipment != null)
                {
                    foreach (var slot in equipment.slots)
                        if (slot.renderer != null && slot.renderer.sprite != null && slot.renderer.enabled)
                            top = Mathf.Max(top, slot.renderer.bounds.max.y);
                }
                else if (singleSpriteVisual != null)
                {
                    foreach (var renderer in singleSpriteVisual.GetComponentsInChildren<Renderer>())
                        top = Mathf.Max(top, renderer.bounds.max.y);
                }
                return new Vector3(transform.position.x, top + .14f, transform.position.z);
            }
        }

        public void HitFlash()
        {
            equipment?.HitFlash();
            trainingDummy?.HitFlash();
        }

        public void Bind(ActorEntity entity)
        {
            if (entity == null || config == null || entity.DefinitionId != config.Id)
                throw new InvalidOperationException("Actor view/config mismatch.");
            Entity = entity;
            actionPose?.ResetPose();
            tools?.ResetPresentation();
            footsteps?.ResetMotion();
            Sync();
        }

        public void Unbind()
        {
            actionPose?.ResetPose();
            tools?.ResetPresentation();
            footsteps?.ResetMotion();
            Entity = null;
        }
        public void Sync()
        {
            if (Entity == null)
                return;
            var p = Entity.Transform.Position;
            transform.position = new Vector3(p.X, p.Y, transform.position.z);
            equipment?.ApplyAppearance(Entity.Appearance);
            equipment?.SetFacing(Entity.Transform.FacingDirection.X);
            if (singleSpriteVisual != null && Mathf.Abs(Entity.Transform.FacingDirection.X) > .001f)
            {
                var scale = singleSpriteVisual.localScale;
                scale.x = Mathf.Abs(scale.x) * (Entity.Transform.FacingDirection.X < 0 ? -1 : 1);
                singleSpriteVisual.localScale = scale;
            }

            actorAnimator?.Present(Entity);
            footsteps?.Present(Entity);
            overhead?.Refresh();
        }
    }
}
