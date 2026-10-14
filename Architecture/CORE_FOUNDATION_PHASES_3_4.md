# Core Foundation — thực thi Phase 3–4

Tiếp nối chỉ thị bốn Phase và checkpoint Phase 2 đã báo: compile sạch 0 lỗi, 78/78 EditMode tests. Không thay luật đã duyệt; thông số content bên dưới là dữ liệu mẫu có thể chỉnh trên SO.

## Phase 3 — Configs và Client

1. Lưu snapshot bền vững trước khi sửa prefab; kiểm scene dirty, asset/GUID và rig qua Unity API. Di chuyển source kèm meta: Client/Environment, World, DevTools/Dashboard; đổi LinhThu.Prototype → LinhThu.Client, cập nhật Editor reference và MovedFrom. Không sửa shader hoặc PNG.
2. SO → immutable Domain Spec: ActorBaseConfigSO, RealmConfigSO, TechniqueConfigSO, StatusEffectConfigSO, BeastSpeciesConfigSO, ItemConfigSO, WorldNodeConfigSO. Effect cấu hình bằng SerializeReference với từng lớp chuyển đổi, không switch theo tên kỹ năng. ContentRegistrySO tập hợp, kiểm duplicate/missing reference, dựng DomainContent và luật chiến đấu.
3. OutfitConfigSO ánh xạ SlotId → Sprite. Rig chỉ giữ một renderer cho mỗi slot; giữ hierarchy xương, controller/clip/pivot và thứ tự lớp. Ba cấu hình classic/jade/basic lấy từ 52 PNG HeroBase hiện có. Bộ basic dùng sprite cơ thể ở slot tay/chân và bỏ lớp vạt áo bằng sprite null tường minh. Không reset Animator khi đổi outfit; flip toàn visual.
4. ActorView đồng bộ Domain pose/state; ActorAnimator điều khiển Idle/Run; BeastView chỉ motion trình bày; WorldNodeView đọc growth/ready; YSortRenderer lấy Y chân và SortingGroup. Material/font/catalog đều tham chiếu serialize. DevTools tham chiếu Client; Client không tham chiếu DevTools.
5. Tạo asset mẫu: Luyện Khí/Trúc Cơ; Linh Kiếm, Thanh Phong Bộ, Hộ Thể; Huyền Quy và công pháp hỗ trợ; thảo/khoáng/hạt/đan/trứng/ngọc giản/ba đạo bào. Tạo PlayerCultivator, HuyenQuyBeast, TrainingDummy; Herb/Ore/Egg/Field/Portal/camp prefab từ art có sẵn. Chuẩn hóa map/prop qua PrefabUtility, không tạo builder script.
6. Cửa kiểm chứng: Unity compile 0 lỗi; Domain suite regression; registry chuyển thành world hợp lệ, reference prefab không thiếu, Sprite Swap giữ clip và transform/binding. Kết quả hiển thị vẫn do chủ dự án nghiệm thu trực tiếp.

## Phase 4 — MainGame và vòng chơi

1. GameBootstrapper làm composition root, đọc registry + scene authoring, dựng WorldState, gắn view bằng EntityId. Fixed step 1/60 s với accumulator; input thành command, không tick timer ở view. Spawn player/thú/target/node bằng prefab/config; scene chứa các instance thật, không sinh cấu trúc scene từ builder.
2. WASD/mũi tên di chuyển, 1/2/3 hoặc nút HUD thi triển kỹ năng, E tương tác gần, R triệu hồi/thu hồi, Tab đổi đạo bào. HUD có HP/Qi/shield, cảnh giới/EXP, cooldown, beast state, kho và thông báo lỗi Domain bằng tiếng Việt. Camera top-down theo player; giữ nền tiling/map đang có. Domain event điều khiển VFX/floating text, không gây damage từ animation.
3. Save adapter chỉ namespace/path foundation mới, ghi temp → replace/backup, load world mới trước khi thay live reference. Không tự load/ghi save cũ. UI chỉ lưu/đọc khi người chơi bấm.
4. Tạo duy nhất MainGame.unity bằng Editor API; Build Settings chỉ bật scene này. Nội dung có map/player/Huyền Quy/thảo/khoáng/linh điền/portal/target, prefab HUD; serialized reference đầy đủ.
5. Cửa kiểm chứng: compile 0 lỗi, NUnit regression, kiểm cấu trúc scene/prefab/config và game-loop wiring bằng API/logic thực tế. Không WebGL/headless screenshots. Báo riêng phần visual/Play cần chủ dự án nghiệm thu.

## Invariants, lỗi và rollback

- Không ghi đè baseline ngoài scope; backup trước sửa serialized assets nằm ở `~/dev/nguthu-backups/core-foundation-backup/`. Không commit/push.
- Registry thiếu config, duplicate ID, outfit thiếu slot, spawn blocked hoặc save lỗi phải báo rõ; không fallback Resources/path string che lỗi.
- Domain giữ source of truth; SO không chứa runtime state. Derived stats không nhân đôi sau đổi đồ/triệu hồi/load. Scene/prefab không sửa YAML bằng tay.
- Rollback từng asset bằng snapshot và meta; không reset/clean toàn repo. Không có migration lên save người dùng, network/multiplayer hoặc economy/crafting ngoài foundation.
