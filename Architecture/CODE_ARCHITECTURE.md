# Kiến trúc code hiện hành

Cập nhật 14/10/2026: Core Foundation đã được duyệt. [Kế hoạch và API](CORE_FOUNDATION_PHASES_1_2.md) · [Kết quả thực thi](CORE_FOUNDATION_PROGRESS.md). Không phục hồi gameplay retired; thông số fixture không phải cân bằng sản phẩm.

## Luồng đích và ranh giới

`Client Input → WorldCommand → WorldState → typed Domain events → View/HUD`. Domain sở hữu luật, clock, RNG và runtime state; Client ánh xạ ScriptableObject thành spec bất biến và chuyển WorldPoint sang Vector2. Domain không tham chiếu Unity hoặc file I/O.

`LinhThu.Domain` không tham chiếu engine. `LinhThu.Client` tham chiếu Domain/InputSystem/uGUI/TMP; `LinhThu.DevTools` tham chiếu Client, không có chiều ngược. `LinhThu.Editor` chỉ có inspector MeadowTerrainPathEditor. Hai assembly tests tách Domain và Client integration. MainGame là scene duy nhất trong Build Settings.

## Domain đã hiện thực ở Phase 2

`Scripts/Domain` có mười thư mục tổ chức bảy trụ cột. `WorldFactory.Create` đăng ký bốn effect handler và năm delivery selector; `WorldInitialState` nhận player/realm/map, geometry, faction rules, node và `BeastSeed`. `IDomainContent` cấp immutable spec; không có tên Hero/Huyền Quy hoặc thông số cân bằng sản phẩm hardcode trong mô phỏng. `ActorEntity` dùng chung cho mọi faction; `SetStaggered` hủy cast, còn HP/Qi/Shield nằm trong `VitalResourcePool`.

`WorldState.Execute` nhận command tại boundary; `Tick` nhận delta hữu hạn trong `[0, 0.1]`. Move command chỉ đổi hướng mong muốn; Tick mới resolve vị trí. Cooldown, status, cast/channel, projectile, AI và growth chạy qua WorldState; Client không tick lại từng container. Projectile sinh giữa tick chỉ di chuyển trong thời gian còn lại. Event được queue sau mutation rồi flush; callback lỗi được báo bằng AggregateException, không tự retry command đã commit.

`ActorEntity` giữ `DesiredMovement`, `MovementSpeedFactor`, `Transform2D.CurrentVelocity` và `DashState`. AI cũng gửi ý định cho bước resolve chung. `MovementSpec` nhận gia tốc/phanh/độ nhạy đổi hướng từ SO. `DashCommand` khóa hướng và tích phân vận tốc giảm bậc hai (vị trí ease-out cubic) bằng C# thuần; WorldGeometry chặn/trượt va chạm. Dash có cooldown riêng, không có miễn thương; cast và dash loại trừ nhau. Chết/stagger/portal dọn vận tốc và hủy dash, giữ cooldown.

`ActorAppearance` là readonly struct ba string ID: OutfitId/HairId/EyeId. `AppearanceSpec` cấp wardrobe hợp lệ cho từng actor; beast không modular dùng giá trị rỗng. `ChangeAppearanceCommand` đổi cosmetic nguyên tử, không cấp chỉ số/vật phẩm. Equip đạo bào đổi OutfitId và giữ tóc/mắt; unequip trả outfit mặc định. Domain không giữ Sprite/Material/Color.

`LifeActivitySpec/State` cùng `LifeActivityService` quản lý Meditating/Fishing. `ToggleMeditateCommand`, `FishCommand` và E tại FishingSpot đi qua WorldCommandProcessor. Thiền hồi Qi theo multiplier của actor spec, tích lũy EXP theo tick cấu hình. Câu cá giữ NodeId, water target và thời gian đến lúc cá cắn; nhấn lại mới nhận rewards. Kiểm túi trước khi tiêu hao node, kho đầy giữ cá chờ. Move khác 0, cast/Dash hợp lệ, damage/stagger/death/portal hủy hoạt động. Input lỗi không làm đổi state. Nước là mục tiêu thả phao. `FishingShoreline` là polygon cấu hình trùng footprint hồ; `WorldNodeEntity.InteractionPosition` lấy điểm gần nhất trên bờ, lùi ra ngoài 0.01 để ClearLine không bị chính hồ chặn. Tầm và thứ tự focus đều đo tới điểm này; mọi phía của hồ dùng cùng luật. Actor phải đứng được, vật cản khác vẫn chặn; không dịch actor vào nước. Giữ vị trí node và water target cũ để save schema 2 hiện có còn tương thích.

StatSheet giữ đúng 22 ID, dirty cache và nguồn modifier riêng cho realm/equipment/status/beast. Năm Affinity dùng chung offense/resistance. Status có refresh/add-stack/independent, snapshot nguồn cho periodic effect; equipment swap, harvest và consumable validate trước khi tiêu hao. Beast Follow/Assist đi qua cùng motor/combat, có leash và recall khi chết hoặc giảm cap.

`GameSaveMapper.Capture` yêu cầu boundary đã flush. `SaveSerializer` dùng schema `linhthu.foundation`/2, JSON DTO, giới hạn size/depth/count, không type metadata. DTO giữ đủ appearance, movement intent, velocity, dash direction/time/cooldown. Restore nhận RNG/event bus **mới**, dựng graph riêng, kiểm ID/definition/vị trí/nguồn modifier cùng appearance/dash rồi mới trả world; caller chỉ thay world khi thành công. Realm, equipment, status và bond được tái tạo đúng một lần. Domain chỉ xử lý JSON trong RAM; WorldSaveStore ở Client/Persistence dùng đường dẫn `linhthu.foundation/v2/save.json`, không migration hoặc ghi đè save schema 1.

Schema 2 bổ sung `LifeActivitySaveDto` với mặc định rỗng khi đọc save trước mốc này. Restore kiểm actor có quyền hoạt động, không còn motion/cast, meditation timer hợp lệ; fishing phải khớp node/đích nước/thời gian và actor còn ở bờ. Save không khớp bố cục node của MainGame bị Client từ chối trước khi đổi world; không tự migrate vị trí/bố cục.

Unity Test Framework được pin bản `1.8.0` đã resolve. Domain EditMode có năm fixture nền tảng, Core/Actor, Cultivation, WorldState, InteractionTargetSelector, GameFeel và Appearance; Client integration kiểm prefab/config/game loop và EnvironmentFeel. Counts/XML thực tế xem [báo cáo tiến độ](CORE_FOUNDATION_PROGRESS.md).

## Asset chuẩn

- `Art/Characters/HeroBase`: 52 PNG cùng meta, `Animation/Idle.anim`, `Run.anim`, `HeroBase.controller`.
- `Content/Prefabs/Actors/HeroBase.prefab`: cùng hierarchy xương và controller/clip, 25 slot renderer dùng ba OutfitConfigSO (classic/jade/basic). Đổi sprite, không reset animator hoặc đổi bind coordinates.
- `Art/Environment/Meadow`: PNG/texture/material; `Content/Prefabs/Environment/MeadowMainMap.prefab` và prop riêng giữ GUID/bố cục.
- `ObstacleAuthoring` chuyển contour chân thành WorldGeometry; nền/đường dùng tiling và mesh. Inspector đường chỉnh điểm, không dựng scene.
- Material ở `Art/Materials`, catalogue ở `Content/DevTools`; font hiện có và mọi reference được serialize, không còn Resources.Load trong source project. [Dashboard](OWNER_DASHBOARD.md) đã chuyển vào DevTools.

Các nhánh nhân vật cũ, trial scenes, CartoonCharacterRig/CartoonUiRig, builders/checks và Web QA đã được loại theo manifest. Hai Cartoon rig không nằm trong dependency Unity của chín prefab giữ lại.

## Configs, composition và presentation

`ContentRegistrySO.BuildContent()` kiểm tra ID/reference, chuyển SO thành DomainContent. Asset SO chỉ chứa định nghĩa; từng world giữ state riêng. Sample gồm Luyện Khí → Trúc Cơ, ba đạo bào, ba công pháp người chơi, Thủy Tức của Huyền Quy và các node/vật phẩm. Thêm content cùng loại bằng SO; thêm kiểu effect bằng AbilityEffectData + Domain handler, không switch tên kỹ năng.

`GameBootstrapper.Initialize` đọc prefab scene, tạo geometry từ ObstacleAuthoring, seed player/node/roster/inventory và gắn view theo EntityId. Update nhận Input System, gửi command rồi tích lũy fixed step 1/60s cho WorldState. ActorView/ActorAnimator chỉ trình bày pose/state; BeastView nhún/nghiêng; WorldNodeView thể hiện ready/growth. CultivationHudPresenter đọc HP/Qi/shield, cảnh giới, inventory/cooldown và phản hồi Domain; button cùng đi qua command như bàn phím. TopDownCameraController theo player trong biên map.

ActorAnimator lấy tốc độ thực tế để điều tốc clip và nghiêng node Visual; BeastView tích lũy pha nhún liên tục. ActorView đồng bộ ngoại hình cho mọi actor. ModularEquipmentRenderer đổi đủ 25 slot, thay đồng thời ba lớp tóc và mắt, không rebind/reset clip. Một shared `ModularHero.mat`; MaterialPropertyBlock giữ tint theo slot, tint chất liệu/monochrome cho Mộc Nhân và hit flash từ sự kiện damage. Vòng Tab đọc includeInRobeCycle của outfit: classic/jade, không chọn basic. Không tạo material instance trên nhân vật. Việc giữ material chung không phải bằng chứng draw-call/batching ở quy mô nhiều người; chưa có benchmark mạng hoặc crowds.

HeroBase thu cụm đầu qua xương head (0.78), vai 0.78 bề ngang cũ, tay khép 12°, chân giữ độ dài qua vị trí khớp; mọi xương ngoài head có scale 1, torso không kéo Y; clip Idle/Run giữ GUID/controller và binding paths. Run được hiệu chỉnh bằng IK hai khớp tại authoring: chân tiếp đất lùi 0.6 rig unit trong 0.4s; ở scale 0.625, `baseStrideSpeed=0.9375`. Hông dao động 0.845–0.905 so với Idle 0.9147, gối gập dưới 85° và luôn ở dưới hông; không hạ thân để ép một bước quá dài. `PlaybackSpeed` bằng vận tốc/baseStrideSpeed tới 6 world unit/s; sau đó dùng đường log có đạo hàm liên tục tại ngưỡng, không clamp cứng ở tốc độ thường. Thay scale hoặc stride cần đo lại chuẩn này.

`ActionPoseController` trộn pose serialized trên prefab sau Animator; trả pose animation gốc ở Update trước frame sau để không tích lũy offset. Idle/Moving bỏ ngay pose đặc biệt để Animator sở hữu góc khớp. Danh sách pose theo ActorActionState dùng chung xương cho cả ba outfit. `CharacterToolAttachmentPresenter` đọc ActorView.Entity, giữ cần trúc tại `hand_front/ToolSocket`, dây/phao/gợn theo FishingTarget, particle Linh khí khi thiền. Mọi renderer dùng shared material; visual không cấp item/EXP hay dịch chuyển actor. Bụi đọc phase Run 0/.5 sau Animator, mỗi nhịp phát đúng một lần; reset khi bind/teleport và giữ burst start/brake/turn/dash.

TechniqueVfxPresenter đọc projectile/event sau commit; FloatingTextPresenter chỉ hiện damage, không quyết định sát thương. OwnerDashboard không nằm trong MainGame và không được Client tham chiếu.

`InteractionTargetSelector` suy ra duy nhất `WorldState.FocusedNodeId` từ vị trí/hướng hiện tại, tầm tương tác, đường nhìn và khả năng thao tác của node. Khoảng cách ưu tiên; hướng nhìn rồi EntityId giải quyết trường hợp ngang nhau. Không giữ cache mục tiêu hoặc ngưỡng di chuyển. `InteractCommand` chọn lại qua cùng selector trong WorldCommandProcessor rồi thực hiện harvest/plant/portal; HUD không tự chọn node. `InteractionPromptPresenter` duy nhất trên HUD lấy mục tiêu/tên từ node và neo toàn bộ khung trên `WorldNodeView.LabelPosition`, cập nhật sau camera. Tên Linh dược/Trứng… giữ nguyên; nhãn hồ dùng Thả câu/Thu cần/Giật cần. `labelVisual` cho phép tham chiếu renderer hồ có sẵn mà không đổi màu hồ theo trạng thái node; không dùng marker một phía. Khung có đáy cao hơn đỉnh vật thể, không bám người chơi. Chữ sát thương nhận ActorView từ Composition Root và bám trên đầu actor khi di chuyển. Mọi nhãn gắn nhân vật mới dùng chung quy tắc này; không dùng offset từ chân hoặc tâm thân.

`ActorOverheadPresenter` trên prefab actor chỉ hiện với HostileMonster còn sống: tên config, tỷ lệ HP thật và tên/stack/thời hạn status. ActorView cấp điểm neo từ bounds rig sau scale/pose, không sửa dữ liệu combat. PlayerCultivator/TrainingDummy dùng visual scale 0.625, CollisionRadius 0.1375 và GroundContactShadow kích thước 0.6 × 0.24; sprite swap giữ các giá trị này.

MeadowGroundSurface cung cấp vùng cỏ mát quanh hồ cho cả ground và MeadowTerrainPath bằng MaterialPropertyBlock. Hai material terrain dùng cùng tham số vân cỏ và sắc ấm/mát để mép đường liền nền. Khóm grass-tuft/white-flowers nằm trên lớp sàn, không giữ bóng sprite cũ. Linh Điền nằm ngoài spline; footprint trứng/quặng giữ điểm tương tác ở mép trước, không tự chặn đường nhìn tới node.

Nền/đường dùng meadow-grass/meadow-earth vẽ tay, cùng `_DappledLight` trong world space. InteractiveFoliage đọc trực tiếp collection `WorldState.Actors` được inject (không FindObjects/poll scene), tự nhận thay đổi khi summon/recall/load; đọc vận tốc của mọi actor, đung đưa quanh gốc và hồi phục theo exponential smoothing, không đẩy actor/collider. Bootstrap bind cả prop map lẫn node Linh thảo và bind lại sau load. FootstepDust dùng ParticleSystem tối đa 64 hạt/actor, material chung, phát tại chân khi bắt đầu chạy/phanh/đổi hướng/dash; reset khi bind/teleport/disable. GardenAmbientArt giữ 20 cánh hoa/cây, thêm drift/flutter theo gió; mesh ambience dùng shared material + MPB.

WorldSaveStore ghi JSON UTF-8 vào file pending riêng, flush rồi Move/Replace, giữ `.bak`. Load dựng world riêng, xác nhận map/node/prefab rồi mới đổi live reference; lỗi load giữ nguyên world. F5/F9 và nút HUD là thao tác tường minh, không tự động mở save cũ.

SpiritField, Portal, SpiritEgg và Camp dùng mesh prop riêng; Herb/Ore dùng sprite môi trường thật. Linh Điền được đặt thẳng ở bãi cỏ gần lều, không có bóng chiếu; HUD có thanh ShortcutBar mờ ngay trên ba nút công pháp. Ảnh `field.png` toàn cảnh cũ đã bị xóa sau kiểm dependency và backup. Cả MeadowMainMap và MeadowMap đều dùng tiling/đường mesh; MeadowMap giữ 21 vị trí prop và GUID khi bỏ nền ảnh cũ.

## Dữ liệu mỹ thuật

### Bóng mặt trời dùng chung

`DaylightPresentation` cung cấp giờ, hướng và độ dài bóng cho toàn cảnh. `Shaders/SunShadow.cginc` chỉ chiếu prop lớn (cây/đá): điểm tiếp đất giữ nguyên; phần cao dịch ngược hướng mặt trời; giữa trưa còn diện tích bóng theo hình khối. `SunShadowSettings` lưu chiều cao, bề rộng, chiều sâu và vùng tiếp đất trên prefab/definition. Runtime không chọn công thức bằng tên cây, đá hay nhân vật.

`SunShadowMesh` chia lưới silhouette cho prop lớn, truyền UV atlas và UV local riêng để fade biên 0–0.08/0.92–1 và chiều cao. Giữ alpha nguồn thay vì hard clip rồi gán lại opacity. Chiều dài 0.35–0.85, opacity 0.20–0.26, hướng ngang chuyển liên tục qua trưa. Người/thú dùng một quad `GroundContactShadow` có radial smooth falloff, tâm/độ rộng đổi nhẹ theo nắng và nhỏ lại khi Dash/nâng visual. Bóng là con của actor root, độc lập việc lật rig. Stencil bit 64 hợp vùng bóng để không cộng dồn độ đậm; camera cần xóa depth/stencil mỗi frame. ActorShadow và shader tương thích cũ đã bỏ. `DaylightCanvasEffect` chỉ xử lý màu UI.

Profile tán trên cao dùng `canopyBlueRatio` để chọn hoa anh đào hồng khỏi thân/rễ nâu trong artwork hiện hành. Tán giữ contour qua phép chiếu affine; một khối thân hẹp nối tán với điểm chân trong cùng mesh/pass. Ngưỡng màu này phụ thuộc palette: đổi màu tán hoặc nhập cây khác cần chỉnh profile và kiểm tra ảnh, không áp mặc định cho mọi cây.

Đây là mô phỏng 2.5D theo silhouette và thông số khối do artist chỉnh, không phải hình học 3D thật. Asset mới cần kiểm tra sáng, 11:30, trưa, chiều; nhân vật cần thêm chạy, lật hướng và đổi đồ.


## Kiểm chứng và khôi phục

Dùng Unity CLI/Pipeline trên Editor đúng project: `unity command recompile`, sau đó `unity command recompile_status`. EditMode tests chạy qua Unity Test Runner; log/NUnit XML ở ngoài checkout. Mỗi Phase cần compile 0 lỗi, Phase 2 cần suite đạt; kiểm dependency/reference prefab riêng vì compile không phát hiện mất sprite/material. Chủ dự án đánh giá hiển thị trong Editor.

Không sửa scene/prefab YAML hoặc thêm builder. Save schema 1 và mọi save cũ được giữ nguyên ngoài luồng schema 2. [Quy tắc lưu trữ](REPOSITORY_STORAGE.md) · [Rollback](../ROLLBACK.md).


## Va chạm, thứ tự vẽ và HUD — 14/10/2026

`WorldGeometry.ResolvePenetration` phục hồi circle/box/convex polygon với khe an toàn 0.02; nhiều footprint giao nhau được lặp có giới hạn và tìm vị trí trống có thứ tự cố định nếu push-out luân phiên. `MovementMotor.RecoverOverlap` chạy trước kiểm khóa hành động mỗi Tick, không tính quãng đẩy thoát thành velocity. Đi và Dash trượt bề mặt; displacement hiệu ứng vẫn dùng bước chia nhỏ và tách trục. MainGame không có Collider2D điều khiển song song với Domain.

`YSortRenderer` tính prop từ bounds.min.y + groundContactOffset, actor từ root chân. `ModularMapProp` chuyển quyền sort về component này. Dải world dùng base 2000 và 100 order/unit; SortingGroup giữ các mảnh rig/thú thành một cụm. VFX ở 4000, overhead 5000, damage text 5100; nền/bóng giữ lớp dưới. Các đá có footprint 42% chiều cao phần dưới; đá dưới hồ tại (7.2, -4.25), giữ lối tiếp cận hồ và mỏ khoáng.

TrainingDummy dùng mesh `TrainingPillar` trên prefab riêng và `TrainingDummyRenderer` nhận HitFlash qua ActorView; không còn rig, tóc hoặc mặt người. ID/Stats/appearance lưu trữ hiện có giữ tương thích. SpiritEgg/Portal giữ GUID và gameplay, mesh mượt với material MeadowCraftedProps dùng ánh sáng chung.

`CultivationHudPresenter` đọc World và gửi lệnh như trước; `CultivationHudLayout` dựng UI runtime từ font serialize trên CultivationHud.prefab. `HudFrameSkin` sở hữu texture/sprite 9-slice dùng chung và hủy khi HUD/đồng hồ bị hủy. HUD có 3 thanh sinh lực/linh lực/hộ thuẫn, 5 ô (3 công pháp + Dash + Beast), cooldown theo state thật, túi tự co và cuộn khi quá 10 dòng. Prompt E là nút nhỏ trên vật thể; phím phụ chuyển vào micro-bar. Không còn hierarchy HUD thử cũ hoặc prefab prompt không dùng.
