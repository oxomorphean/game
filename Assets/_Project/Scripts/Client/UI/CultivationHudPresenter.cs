using System;
using System.Linq;
using LinhThu.Client.Bootstrap;
using LinhThu.Domain.Beasts;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Core;
using LinhThu.Domain.Stats;
using LinhThu.Domain.World;
using TMPro;
using UnityEngine;

namespace LinhThu.Client.UI
{
    public sealed class CultivationHudPresenter : MonoBehaviour
    {
        public TMP_Text title, vitals, progression, inventory, beastState, interaction, message;
        public UnityEngine.UI.Image hpFill, qiFill;
        public UnityEngine.UI.Button[] hotbar = Array.Empty<UnityEngine.UI.Button>();
        public TMP_Text[] hotbarLabels = Array.Empty<TMP_Text>();
        public UnityEngine.UI.Button interactButton, beastButton, outfitButton, saveButton, loadButton;
        public TMP_Text interactButtonLabel;
        public InteractionPromptPresenter interactionPrompt;
        private GameBootstrapper game;
        private float messageUntil;
        public void Bind(GameBootstrapper bootstrapper)
        {
            if (game != null)
                throw new InvalidOperationException("HUD already bound.");
            game = bootstrapper;
            if (hotbar.Length != game.registry.hotbar.Length || hotbarLabels.Length != hotbar.Length)
                throw new InvalidOperationException("HUD hotbar does not match registry.");
            for (int i = 0; i < hotbar.Length; i++)
            {
                int slot = i;
                hotbar[i].onClick.AddListener(() => game.CastFromHud(slot));
            }

            interactButton.onClick.AddListener(() => game.InteractFocused());
            beastButton.onClick.AddListener(() => game.ToggleBeast());
            outfitButton.onClick.AddListener(() => game.CycleOutfit());
            saveButton.onClick.AddListener(game.SaveGame);
            loadButton.onClick.AddListener(game.LoadGame);
            Notify("WASD đi • Space lướt • X thiền • F câu cá • E tương tác • 1/2/3 công pháp");
        }

        public void Refresh()
        {
            if (game == null || game.World == null)
                return;
            var world = game.World;
            var actor = world.PlayerActor;
            var pool = actor.Vitals;
            hpFill.fillAmount = pool.CurrentHp / pool.Maximum(VitalType.Hp);
            qiFill.fillAmount = pool.Maximum(VitalType.Qi) > 0 ? pool.CurrentQi / pool.Maximum(VitalType.Qi) : 0;
            title.text = "LINH THÚ KÝ  ·  Thanh Khê";
            vitals.text = $"Sinh lực {pool.CurrentHp:0}/{pool.Maximum(VitalType.Hp):0}   •   Linh lực {pool.CurrentQi:0}/{pool.Maximum(VitalType.Qi):0}\nHộ thuẫn {pool.CurrentShield:0}   •   Công {actor.Stats.Get(StatId.Attack):0}   •   Thủ {actor.Stats.Get(StatId.Defense):0}";
            string[] stages = {"Sơ kỳ", "Trung kỳ", "Hậu kỳ", "Viên mãn"};
            progression.text = game.registry.Realm(world.Player.Cultivation.CurrentRealmId).displayName + " · " + stages[(int)world.Player.Cultivation.SubStage] + $"   |   Tu vi {world.Player.Cultivation.CurrentCultivationExp}";
            inventory.text = $"TÚI ĐỒ  {world.Player.Inventory.Slots.Count}/{world.Player.Inventory.Capacity}\n" + string.Join("\n", world.Player.Inventory.Slots.GroupBy(i => i.DefinitionId).Select(g => game.registry.Item(g.Key).displayName + "  ×" + g.Sum(i => i.Count)));
            var beast = world.Player.Beasts.All.FirstOrDefault();
            beastState.text = beast == null ? "Chưa có linh thú" : game.registry.beasts.First(b => b.Id == beast.DefinitionId).displayName + (beast.State == BeastState.SummonedActive ? " · Đồng hành" : " · Đang nghỉ");
            var node = world.FocusedNode;
            string nodeName = node == null ? "" : game.registry.nodes.First(n => n.Id == node.DefinitionId).displayName;
            interaction.text = nodeName;
            string action = node?.Spec.Kind == WorldNodeKind.FishingSpot
                ? actor.State == ActorActionState.Fishing ? actor.LifeActivity.FishBiting ? "Giật cần" : "Thu cần" : "Thả câu"
                : "Tương tác";
            interactButtonLabel.text = "E · " + action + (node == null ? "" : "\n" + nodeName);
            interactButton.interactable = node != null;
            LinhThu.Client.World.WorldNodeView nodeView = null;
            if (node != null)
                game.TryGetNodeView(node.Id, out nodeView);
            interactionPrompt.SetTarget(nodeView, game.gameCamera, node == null ? "" : "[E] " + (node.Spec.Kind == WorldNodeKind.FishingSpot ? action : nodeName));
            beastButton.interactable = actor.CanAct && beast != null;
            outfitButton.interactable = actor.CanAct;
            for (int i = 0; i < hotbar.Length; i++)
            {
                var technique = game.registry.hotbar[i];
                var remaining = actor.Abilities.GetCooldownRemaining(technique.Id);
                hotbarLabels[i].text = (i + 1) + " · " + technique.displayName + "\n" + (remaining > 0 ? $"{remaining:0.0}s" : $"{technique.qiCost:0} linh lực");
                hotbar[i].interactable = actor.CanAct && !actor.Dash.Active && remaining <= 0 && pool.CurrentQi >= technique.qiCost;
            }

            if (Time.unscaledTime <= messageUntil) return;
            if (actor.State == ActorActionState.Meditating)
                message.text = "Đang thiền · hồi Linh lực và Tu vi   |   X / WASD để đứng dậy";
            else if (actor.State == ActorActionState.Fishing)
                message.text = actor.LifeActivity.FishBiting ? "Cá cắn câu! Nhấn F hoặc E để giật cần." : $"Chờ cá cắn câu · {actor.LifeActivity.FishingRemaining:0.0}s   |   F / WASD để cất cần";
            else
                message.text = actor.Dash.CooldownRemaining > 0 ? $"Space · hồi {actor.Dash.CooldownRemaining:0.0}s   |   X thiền • F câu cá • Tab áo • H tóc • Y mắt" : "Space lướt • X thiền • F câu cá • Tab áo • H tóc • Y mắt";
        }

        public void Notify(string text, float seconds = 4)
        {
            message.text = text;
            messageUntil = Time.unscaledTime + seconds;
        }

        public static string ErrorText(DomainError error)
        {
            switch (error)
            {
                case DomainError.InsufficientResource:
                    return "Chưa đủ vật phẩm hoặc linh lực.";
                case DomainError.OnCooldown:
                    return "Chiêu thức đang hồi.";
                case DomainError.OutOfRange:
                    return "Hãy đến gần hơn.";
                case DomainError.Blocked:
                    return "Có vật cản trên đường.";
                case DomainError.CapacityExceeded:
                    return "Đã hết chỗ trống.";
                case DomainError.InvalidState:
                    return "Chưa thể thực hiện lúc này.";
                case DomainError.NotFound:
                    return "Không tìm thấy dữ liệu.";
                case DomainError.NoEffect:
                    return "Không có thay đổi.";
                case DomainError.InvalidSave:
                    return "Bản lưu không hợp lệ.";
                case DomainError.UnsupportedSchema:
                    return "Phiên bản bản lưu chưa được hỗ trợ.";
                default:
                    return "Không thể thực hiện: " + error;
            }
        }
    }
}
