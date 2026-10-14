using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinhThu.Client.Actors;
using LinhThu.Client.Environment;
using LinhThu.Client.Persistence;
using LinhThu.Client.UI;
using LinhThu.Client.Combat;
using LinhThu.Client.World;
using LinhThu.Domain.Beasts;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Combat;
using LinhThu.Domain.Core;
using LinhThu.Domain.Inventory;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.Stats;
using LinhThu.Domain.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using EntityId = LinhThu.Domain.Core.EntityId;

namespace LinhThu.Client.Bootstrap
{
    public sealed class GameBootstrapper : MonoBehaviour
    {
        public ContentRegistrySO registry;
        public ModularMapLayout map;
        public ActorView playerView;
        public ActorView[] sceneActors = Array.Empty<ActorView>();
        public WorldNodeView[] sceneNodes = Array.Empty<WorldNodeView>();
        public UnityEngine.Camera gameCamera;
        public CultivationHudPresenter hud;
        public TechniqueVfxPresenter effects;
        public WorldState World { get; private set; }

        public EntityId? FocusedNodeId => World?.FocusedNodeId;

        public bool TryGetNodeView(EntityId id, out WorldNodeView view) => nodeViews.TryGetValue(id, out view);

        public const float StepSeconds = 1f / 60;
        private readonly List<ActorView> actorViews = new List<ActorView>();
        private readonly Dictionary<EntityId, WorldNodeView> nodeViews = new Dictionary<EntityId, WorldNodeView>();
        private WorldSaveStore saveStore;
        private double accumulator;
        private IDisposable damageSubscription;
        private void Start() => Initialize();
        public void Initialize()
        {
            if (World != null)
                return;
            if (registry == null || map == null || playerView == null || gameCamera == null || hud == null || effects == null)
                throw new InvalidOperationException("MainGame is missing composition references.");
            if (map.transform.position != Vector3.zero || map.transform.lossyScale != Vector3.one)
                throw new InvalidOperationException("World map must have origin zero and unit scale.");
            var geometry = new WorldGeometry(new WorldPoint(map.size.x / 2, map.size.y / 2), gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ObstacleAuthoring>()).Select(o => o.Definition()));
            var initial = new WorldInitialState(registry.player.Id, registry.startingRealm.Id, registry.mapDefinitionId, Point(playerView.transform.position), registry.inventoryCapacity, registry.CreateRules(), registry.CreateRelations(), sceneNodes.Select(n => new NodeSpawn(n.config.Id, Point(n.transform.position), n.initiallyReady)), registry.CreateBeastSeeds());
            var world = WorldFactory.Create(registry.BuildContent(), geometry, new SeededRandomSource(registry.randomSeed), new DomainEventBus(), initial);
            Require(world.Player.Inventory.TryAddAll(registry.startingItems.Select(i => new ItemStack(i.item.Id, i.count)).ToArray()));
            Require(world.Execute(new EquipCommand(world.Player.ActorId, registry.startingOutfit.equipmentSlot, registry.startingOutfit.Id)));
            foreach (var view in sceneActors.Where(v => !v.rosterCompanion))
            {
                var spawn = world.TrySpawnActor(view.config.Id, view.faction, Point(view.transform.position));
                if (!spawn.Succeeded)
                    throw new InvalidOperationException("Cannot spawn " + view.name + ": " + spawn.ErrorCode);
                view.Bind(spawn.Value);
            }

            foreach (var beast in world.Player.Beasts.All)
                Require(world.Execute(new SummonCommand(world.Player.ActorId, beast.Id)));
            var nodes = world.Nodes.Values.OrderBy(n => n.Id).ToArray();
            for (int i = 0; i < nodes.Length; i++)
            {
                nodeViews.Add(nodes[i].Id, sceneNodes[i]);
                sceneNodes[i].Bind(nodes[i]);
            }

            actorViews.Add(playerView);
            actorViews.AddRange(sceneActors);
            World = world;
            BindFoliage();
            playerView.Bind(World.PlayerActor);
            saveStore = new WorldSaveStore(Path.Combine(Application.persistentDataPath, "linhthu.foundation", "v2", "save.json"));
            effects.Bind(World, registry, actorViews);
            BindDamageFeedback();
            hud.Bind(this);
            SyncViews();
        }

        private static void Require(DomainResult result)
        {
            if (!result.Succeeded)
                throw new InvalidOperationException("Invalid initial content: " + result.ErrorCode + " " + result.Message);
        }

        private static WorldPoint Point(Vector3 p) => new WorldPoint(p.x, p.y);
        private void Update()
        {
            if (World == null)
                return;
            var keyboard = Keyboard.current;
            var movement = Vector2.zero;
            if (keyboard != null && Application.isFocused)
            {
                movement.x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
                movement.y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
                if (keyboard.spaceKey.wasPressedThisFrame || keyboard.leftShiftKey.wasPressedThisFrame)
                    Dash(new WorldPoint(movement.x, movement.y));
                if (keyboard.digit1Key.wasPressedThisFrame)
                    Cast(0, PointerDirection());
                if (keyboard.digit2Key.wasPressedThisFrame)
                    Cast(1, PointerDirection());
                if (keyboard.digit3Key.wasPressedThisFrame)
                    Cast(2, PointerDirection());
                if (keyboard.eKey.wasPressedThisFrame)
                    InteractFocused();
                if (keyboard.xKey.wasPressedThisFrame)
                    ToggleMeditate();
                if (keyboard.fKey.wasPressedThisFrame)
                    Fish();
                if (keyboard.rKey.wasPressedThisFrame)
                    ToggleBeast();
                if (keyboard.tabKey.wasPressedThisFrame)
                    CycleOutfit();
                if (keyboard.hKey.wasPressedThisFrame)
                    CycleHair();
                if (keyboard.yKey.wasPressedThisFrame)
                    CycleEyes();
                if (keyboard.f5Key.wasPressedThisFrame)
                    SaveGame();
                if (keyboard.f9Key.wasPressedThisFrame)
                    LoadGame();
            }

            if (Application.isFocused && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
                Cast(0, PointerDirection());
            accumulator += Time.deltaTime;
            while (accumulator >= StepSeconds)
            {
                World.Execute(new MoveCommand(World.Player.ActorId, new WorldPoint(movement.x, movement.y)));
                World.Tick(StepSeconds);
                accumulator -= StepSeconds;
            }

            SyncViews();
        }

        private WorldPoint PointerDirection()
        {
            if (Mouse.current == null)
                return World.PlayerActor.Transform.FacingDirection;
            var mouse = Mouse.current.position.ReadValue();
            var target = gameCamera.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, -gameCamera.transform.position.z));
            var delta = Point(target) - World.PlayerActor.Transform.Position;
            return delta.LengthSquared > .001f ? delta.Normalized : World.PlayerActor.Transform.FacingDirection;
        }

        public DomainResult Cast(int slot, WorldPoint direction)
        {
            if (slot < 0 || slot >= registry.hotbar.Length)
                return DomainResult.Fail(DomainError.InvalidInput);
            return Submit(new CastCommand(World.Player.ActorId, registry.hotbar[slot].Id, new AimRequest(direction)));
        }

        public DomainResult Dash(WorldPoint direction) => Submit(new DashCommand(World.Player.ActorId, direction));

        public void CastFromHud(int slot)
        {
            var target = World.Actors.Values.Where(a => a.Vitals.IsAlive && World.Relations.IsHostile(World.PlayerActor.Faction, a.Faction)).OrderBy(a => (a.Transform.Position - World.PlayerActor.Transform.Position).LengthSquared).FirstOrDefault();
            Cast(slot, target == null ? World.PlayerActor.Transform.FacingDirection : (target.Transform.Position - World.PlayerActor.Transform.Position).Normalized);
        }

        public DomainResult InteractFocused()
        {
            var node = World.FocusedNode;
            if (node == null)
                return DomainResult.Fail(DomainError.NotFound);
            if (node.Spec.Kind == WorldNodeKind.FishingSpot)
                return Fish(node.Id);
            bool planting = node.Spec.Kind == WorldNodeKind.SpiritFieldPlot && node.CropDefinitionId == null;
            return Submit(new InteractCommand(World.Player.ActorId, registry.fieldCrop.Id),
                node.Spec.Kind == WorldNodeKind.TeleportPortal ? "Đã dịch chuyển." : planting ? "Đã gieo linh thảo." : "Đã thu hoạch.");
        }

        public DomainResult ToggleBeast()
        {
            var beast = World.Player.Beasts.All.FirstOrDefault();
            if (beast == null)
                return DomainResult.Fail(DomainError.NotFound);
            bool recall = beast.State == BeastState.SummonedActive;
            string name = registry.beasts.First(b => b.Id == beast.DefinitionId).displayName;
            return Submit(recall ? (WorldCommand)new RecallCommand(World.Player.ActorId, beast.Id) : new SummonCommand(World.Player.ActorId, beast.Id), name + (recall ? " đã nghỉ." : " xuất chiến."));
        }

        public DomainResult ToggleMeditate() => Submit(new ToggleMeditateCommand(World.Player.ActorId),
            World.PlayerActor.State == ActorActionState.Meditating ? "Đã đứng dậy." : "Ngồi thiền · WASD để đứng dậy.");

        public DomainResult Fish(EntityId? nodeId = null)
        {
            bool biting = World.PlayerActor.LifeActivity.FishBiting;
            var result = Submit(new FishCommand(World.Player.ActorId, nodeId));
            if (result.Succeeded)
                hud.Notify(World.PlayerActor.State == ActorActionState.Fishing ? "Đã thả câu · chờ phao động rồi nhấn F/E." : biting ? "Đã thu cá vào túi." : "Đã cất cần câu.", 1);
            return result;
        }

        public DomainResult CycleOutfit()
        {
            var robes = registry.items.Where(i => i.outfit != null).ToArray();
            var current = World.Player.Equipment.Slots.TryGetValue(registry.startingOutfit.equipmentSlot, out var equipped) ? equipped.ItemId : null;
            int start = Array.FindIndex(robes, i => i.Id == current);
            for (int i = 1; i <= robes.Length; i++)
            {
                var next = robes[(start + i) % robes.Length];
                if (World.Player.Inventory.Count(next.Id) > 0)
                    return Submit(new EquipCommand(World.Player.ActorId, next.equipmentSlot, next.Id), next.displayName);
            }

            hud.Notify("Chưa có đạo bào khác trong túi.");
            return DomainResult.Fail(DomainError.NoEffect);
        }

        public DomainResult ChangeAppearance(ActorAppearance appearance) => Submit(new ChangeAppearanceCommand(World.Player.ActorId, appearance));
        public DomainResult CycleHair()
        {
            var actor = World.PlayerActor;
            var ids = actor.Spec.Appearance.HairIds;
            int index = Array.IndexOf(ids.ToArray(), actor.Appearance.HairId);
            return ChangeAppearance(new ActorAppearance(actor.Appearance.OutfitId, ids[(index + 1) % ids.Count], actor.Appearance.EyeId));
        }
        public DomainResult CycleEyes()
        {
            var actor = World.PlayerActor;
            var ids = actor.Spec.Appearance.EyeIds;
            int index = Array.IndexOf(ids.ToArray(), actor.Appearance.EyeId);
            return ChangeAppearance(new ActorAppearance(actor.Appearance.OutfitId, actor.Appearance.HairId, ids[(index + 1) % ids.Count]));
        }

        private void BindDamageFeedback()
        {
            damageSubscription?.Dispose();
            damageSubscription = World.Events.Subscribe<ActorDamagedEvent>(e => actorViews.FirstOrDefault(v => v.Entity?.Id == e.TargetId)?.equipment?.HitFlash());
        }

        private void BindFoliage()
        {
            foreach (var root in gameObject.scene.GetRootGameObjects())
                foreach (var foliage in root.GetComponentsInChildren<InteractiveFoliage>(true))
                    foliage.Bind(World);
        }

        private void OnDestroy() => damageSubscription?.Dispose();

        private DomainResult Submit(WorldCommand command, string success = null)
        {
            var result = World.Execute(command);
            if (!result.Succeeded)
                hud.Notify(CultivationHudPresenter.ErrorText(result.ErrorCode));
            else if (success != null)
                hud.Notify(success);
            SyncViews();
            return result;
        }

        public void SyncViews()
        {
            foreach (var view in actorViews.Where(v => v.Entity != null && !World.Actors.ContainsKey(v.Entity.Id)))
            {
                view.Unbind();
                view.gameObject.SetActive(false);
            }

            foreach (var actor in World.Actors.Values)
            {
                var view = actorViews.FirstOrDefault(v => v.Entity?.Id == actor.Id);
                if (view == null)
                {
                    view = actorViews.FirstOrDefault(v => v.Entity == null && v.config.Id == actor.DefinitionId && v.faction == actor.Faction);
                    if (view == null)
                    {
                        view = Instantiate(registry.Actor(actor.DefinitionId).prefab);
                        actorViews.Add(view);
                    }

                    view.gameObject.SetActive(true);
                    view.Bind(actor);
                }

                view.overhead?.Bind(view, registry);
                view.Sync();
            }

            foreach (var pair in nodeViews)
                pair.Value.Sync();
            hud.Refresh();
        }

        public void SaveGame()
        {
            try
            {
                saveStore.Write(World);
                hud.Notify("Đã lưu hành trình (F5).");
            }
            catch (IOException e)
            {
                hud.Notify("Không lưu được: " + e.Message);
            }
            catch (UnauthorizedAccessException e)
            {
                hud.Notify("Không lưu được: " + e.Message);
            }
        }

        public void LoadGame()
        {
            var restored = saveStore.Read(World.Content, World.Geometry, registry.randomSeed);
            if (!restored.Succeeded)
            {
                hud.Notify("Không đọc được bản lưu: " + CultivationHudPresenter.ErrorText(restored.ErrorCode));
                return;
            }

            var world = restored.Value;
            if (world.MapDefinitionId != registry.mapDefinitionId || world.PlayerActor.DefinitionId != registry.player.Id || world.Nodes.Count != nodeViews.Count || world.Nodes.Any(n => !nodeViews.TryGetValue(n.Key, out var v) || v.config.Id != n.Value.DefinitionId || (n.Value.Position - Point(v.transform.position)).Length > .001f))
            {
                hud.Notify("Bản lưu không khớp cấu hình MainGame.");
                return;
            }

            foreach (var actor in world.Actors.Values)
                if (registry.Actor(actor.DefinitionId).prefab == null)
                {
                    hud.Notify("Bản lưu thiếu prefab nhân vật.");
                    return;
                }

            foreach (var view in actorViews)
            {
                view.Unbind();
                view.gameObject.SetActive(false);
            }

            World = world;
            BindFoliage();
            accumulator = 0;
            playerView.gameObject.SetActive(true);
            playerView.Bind(World.PlayerActor);
            foreach (var node in World.Nodes)
                nodeViews[node.Key].Bind(node.Value);
            effects.Bind(World, registry, actorViews);
            BindDamageFeedback();
            SyncViews();
            hud.Notify("Đã đọc hành trình (F9).");
        }
    }
}
