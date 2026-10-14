# Kiến trúc code hiện hành

Cập nhật 14/10/2026: Core Foundation đã được duyệt. [Kế hoạch và API](CORE_FOUNDATION_PHASES_1_2.md) · [Kết quả thực thi](CORE_FOUNDATION_PROGRESS.md). Không phục hồi gameplay retired; thông số fixture không phải cân bằng sản phẩm.

## Luồng đích và ranh giới

`Client Input → WorldCommand → WorldState → typed Domain events → View/HUD`. Domain sở hữu luật, clock, RNG và runtime state; Client ánh xạ ScriptableObject thành spec bất biến và chuyển WorldPoint sang Vector2. Domain không tham chiếu Unity hoặc file I/O.

`LinhThu.Domain` không tham chiếu engine. `LinhThu.Client` tham chiếu Domain/InputSystem/uGUI/TMP; `LinhThu.DevTools` tham chiếu Client, không có chiều ngược. `LinhThu.Editor` chỉ có inspector MeadowTerrainPathEditor. Hai assembly tests tách Domain và Client integration. MainGame là scene duy nhất trong Build Settings.

## Domain đã hiện thực ở Phase 2

`Scripts/Domain` có mười thư mục tổ chức bảy trụ cột. `WorldFactory.Create` đăng ký bốn effect handler và năm delivery selector; `WorldInitialState` nhận player/realm/map, geometry, faction rules, node và `BeastSeed`. `IDomainContent` cấp immutable spec; không có tên Hero/Huyền Quy hoặc thông số cân bằng sản phẩm hardcode trong mô phỏng. `ActorEntity` dùng chung cho mọi faction; `SetStaggered` hủy cast, còn HP/Qi/Shield nằm trong `VitalResourcePool`.

`WorldState.Execute` nhận command tại boundary; `Tick` nhận delta hữu hạn trong `[0, 0.1]`. Move command chỉ đổi hướng mong muốn; Tick mới resolve vị trí. Cooldown, status, cast/channel, projectile, AI và growth chạy qua WorldState; Client không tick lại từng container. Projectile sinh giữa tick chỉ di chuyển trong thời gian còn lại. Event được queue sau mutation rồi flush; callback lỗi được báo bằng AggregateException, không tự retry command đã commit.

StatSheet giữ đúng 22 ID, dirty cache và nguồn modifier riêng cho realm/equipment/status/beast. Năm Affinity dùng chung offense/resistance. Status có refresh/add-stack/independent, snapshot nguồn cho periodic effect; equipment swap, harvest và consumable validate trước khi tiêu hao. Beast Follow/Assist đi qua cùng motor/combat, có leash và recall khi chết hoặc giảm cap.

`GameSaveMapper.Capture` yêu cầu boundary đã flush. `SaveSerializer` dùng schema `linhthu.foundation`/1, JSON DTO, giới hạn size/depth/count, không type metadata. Restore nhận RNG/event bus **mới**, dựng graph riêng, kiểm ID/definition/vị trí/nguồn modifier rồi mới trả world; caller chỉ thay world khi thành công. Realm, equipment, status và bond được tái tạo đúng một lần. Domain chỉ xử lý JSON trong RAM; `StringReader` không đọc file. Phase 4 thêm WorldSaveStore ở Client/Persistence; không có migration save người dùng.

Unity Test Framework được pin bản `1.8.0` đã resolve. Tám fixture EditMode gồm năm fixture bắt buộc, cộng Core/Actor, Cultivation và WorldState; counts/XML thực tế xem [báo cáo Phase 2](CORE_FOUNDATION_PROGRESS.md).

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

TechniqueVfxPresenter đọc projectile/event sau commit; FloatingTextPresenter chỉ hiện damage, không quyết định sát thương. OwnerDashboard không nằm trong MainGame và không được Client tham chiếu.

WorldSaveStore ghi JSON UTF-8 vào file pending riêng, flush rồi Move/Replace, giữ `.bak`. Load dựng world riêng, xác nhận map/node/prefab rồi mới đổi live reference; lỗi load giữ nguyên world. F5/F9 và nút HUD là thao tác tường minh, không tự động mở save cũ.

SpiritField, Portal, SpiritEgg và Camp dùng mesh prop riêng; Herb/Ore dùng sprite môi trường thật. Các ô màu placeholder và ảnh `field.png` toàn cảnh cũ không nằm trong dependency MainGame. Cả MeadowMainMap và MeadowMap đều dùng tiling/đường mesh; MeadowMap giữ 21 vị trí prop và GUID khi bỏ nền ảnh cũ.

## Dữ liệu mỹ thuật

### Bóng mặt trời dùng chung

`DaylightPresentation` cung cấp giờ, hướng và độ dài bóng cho toàn cảnh. `Shaders/SunShadow.cginc` là phép chiếu duy nhất cho cây, đá và nhân vật: điểm tiếp đất giữ nguyên; phần cao dịch ngược hướng mặt trời; giữa trưa còn diện tích bóng theo hình khối. `SunShadowSettings` lưu chiều cao, bề rộng, chiều sâu và vùng tiếp đất trên prefab/definition. Runtime không chọn công thức bằng tên cây, đá hay nhân vật.

`SunShadowMesh` chia lưới silhouette cho prop và nhân vật sprite sheet. Rig HeroBase giữ transform hierarchy, SpriteRenderer và Animator; các lớp dùng chung điểm chân. Hướng SpriteSkin trước đây không phải mô tả rig hiện hành. Stencil bit 64 hợp vùng bóng để các lớp chồng nhau không làm bóng đậm thêm; camera cần depth/stencil được xóa mỗi frame. `ModularHeroShadow.shader` chỉ còn dẫn tới pass chung để tương thích material cũ. `DaylightCanvasEffect` chỉ xử lý màu UI.

Profile tán trên cao dùng `canopyBlueRatio` để chọn hoa anh đào hồng khỏi thân/rễ nâu trong artwork hiện hành. Tán giữ contour qua phép chiếu affine; một khối thân hẹp nối tán với điểm chân trong cùng mesh/pass. Ngưỡng màu này phụ thuộc palette: đổi màu tán hoặc nhập cây khác cần chỉnh profile và kiểm tra ảnh, không áp mặc định cho mọi cây.

Đây là mô phỏng 2.5D theo silhouette và thông số khối do artist chỉnh, không phải hình học 3D thật. Asset mới cần kiểm tra sáng, 11:30, trưa, chiều; nhân vật cần thêm chạy, lật hướng và đổi đồ.


## Kiểm chứng và khôi phục

Dùng Unity CLI/Pipeline trên Editor đúng project: `unity command recompile`, sau đó `unity command recompile_status`. EditMode tests chạy qua Unity Test Runner; log/NUnit XML ở ngoài checkout. Mỗi Phase cần compile 0 lỗi, Phase 2 cần suite đạt; kiểm dependency/reference prefab riêng vì compile không phát hiện mất sprite/material. Chủ dự án đánh giá hiển thị trong Editor.

Không sửa scene/prefab YAML hoặc thêm builder. Save foundation phiên bản đầu `linhthu.foundation`/1 không tự đọc/ghi đè save cũ. [Quy tắc lưu trữ](REPOSITORY_STORAGE.md) · [Rollback](../ROLLBACK.md).
