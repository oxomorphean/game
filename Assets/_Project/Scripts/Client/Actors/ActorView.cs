using System;
using System.Collections.Generic;
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
        public ActorActionFeedback actionFeedback;
        public FacialExpressionPresenter face;
        public ActorEntity Entity { get; private set; }
        private static readonly Dictionary<Sprite, Vector2[]> visibleOutlines = new Dictionary<Sprite, Vector2[]>();

        // Unity's generated sprite outline follows alpha without requiring readable textures.
        // It is presentation geometry only; Domain footprints remain independently authored.
        private static Bounds VisibleBounds(Renderer renderer)
        {
            if (!(renderer is SpriteRenderer spriteRenderer) || spriteRenderer.sprite == null)
                return renderer.bounds;
            var sprite = spriteRenderer.sprite;
            if (!visibleOutlines.TryGetValue(sprite, out var outline))
            {
                var points = new List<Vector2>(); var shape = new List<Vector2>();
                for (int i = 0; i < sprite.GetPhysicsShapeCount(); i++)
                {
                    sprite.GetPhysicsShape(i, shape);
                    points.AddRange(shape);
                }
                outline = points.Count > 0 ? points.ToArray() : sprite.vertices;
                visibleOutlines.Add(sprite, outline);
            }
            if (outline.Length == 0) return renderer.bounds;
            Vector3 TransformPoint(Vector2 p) => renderer.transform.TransformPoint(new Vector3(
                spriteRenderer.flipX ? -p.x : p.x, spriteRenderer.flipY ? -p.y : p.y, 0));
            var bounds = new Bounds(TransformPoint(outline[0]), Vector3.zero);
            for (int i = 1; i < outline.Length; i++) bounds.Encapsulate(TransformPoint(outline[i]));
            return bounds;
        }

        public Bounds VisualBounds
        {
            get
            {
                var bounds = new Bounds(transform.position, Vector3.zero);
                if (equipment != null)
                    foreach (var slot in equipment.slots)
                        if (slot.renderer != null && slot.renderer.enabled && slot.renderer.sprite != null)
                            bounds.Encapsulate(slot.Geometry.IsSkinned ? slot.Geometry.WorldBounds : VisibleBounds(slot.renderer));
                if (singleSpriteVisual != null)
                    foreach (var renderer in singleSpriteVisual.GetComponentsInChildren<Renderer>())
                        if (renderer.enabled) bounds.Encapsulate(VisibleBounds(renderer));
                return bounds;
            }
        }

        public Vector3 OverheadPosition
        {
            get
            {
                return new Vector3(transform.position.x, VisualBounds.max.y + .14f, transform.position.z);
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
            actionFeedback?.ResetPresentation();
            face?.ResetPresentation();
            Sync();
        }

        public void Unbind()
        {
            actionPose?.ResetPose();
            tools?.ResetPresentation();
            footsteps?.ResetMotion();
            actionFeedback?.ResetPresentation();
            face?.ResetPresentation();
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
            actionFeedback?.ObserveState();
            overhead?.Refresh();
        }
    }
}
