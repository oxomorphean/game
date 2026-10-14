using System.Linq;
using LinhThu.Client.Actors;
using LinhThu.Client.Bootstrap;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LinhThu.Client.UI
{
    [DefaultExecutionOrder(110)]
    public sealed class ActorOverheadPresenter : MonoBehaviour
    {
        public GameObject panel;
        public TMP_Text actorName, statusText;
        public Image hpFill;
        private ActorView view;
        private ContentRegistrySO registry;

        public void Bind(ActorView actor, ContentRegistrySO content)
        {
            view = actor;
            registry = content;
        }

        public void Refresh()
        {
            var actor = view != null ? view.Entity : null;
            bool visible = actor != null && actor.Faction == ActorFaction.HostileMonster && actor.Vitals.IsAlive;
            panel.SetActive(visible);
            if (!visible)
                return;
            transform.position = view.OverheadPosition;
            actorName.text = view.config.displayName;
            hpFill.fillAmount = Mathf.Clamp01(actor.Vitals.CurrentHp / actor.Vitals.Maximum(VitalType.Hp));
            statusText.text = string.Join(" · ", actor.StatusEffects.Active.Select(s =>
                registry.statuses.First(c => c.Id == s.DefinitionId).displayName +
                (s.StackCount > 1 ? " ×" + s.StackCount : "") + $" {s.RemainingDuration:0.0}s"));
        }

        private void LateUpdate() => Refresh();
    }
}
