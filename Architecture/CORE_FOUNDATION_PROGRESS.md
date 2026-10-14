# Tiến độ Core Foundation — Phase 1 & Phase 2

Kế hoạch: [CORE_FOUNDATION_PHASES_1_2.md](CORE_FOUNDATION_PHASES_1_2.md). Chủ dự án duyệt ngày 14/10/2026 với ba điều chỉnh ghi trong AGENTS.md. Không commit/push; thực thi tại checkout chứa art/map chưa commit.

## P1.1 — backup và baseline

- Backup: `/Users/nguyenhoangdacdat/dev/nguthu-backups/core-foundation-backup/20261014T014359Z/source-before-phase-1.tar.gz`.
- Manifest: `manifest-sha256.json` cùng thư mục. Đã đọc lại archive và đối chiếu SHA-256 **1.235 file**, gồm master KRA/PSD/SVG và file chưa tracked; đối chiếu lại source sau tạo archive không có file đổi trong lúc backup.
- Archive SHA-256: `de36f8aea624bcf54cfd70f275b5ce2ea09ef91c99eb046e348f8b08caf6c8f1`.
- Lưu `status.z`, `unstaged.patch`, `staged.patch`, `tracked-files.z` cùng backup. Không reset thay đổi baseline.
- Pipeline đã nối lại bằng Restart server trong Project Settings; scene ArtPreview không dirty trước khi đóng. Không restart Editor hoặc ghi đè scene chưa lưu.

## Quyết định thực thi

- Dùng checkout hiện tại thay worktree sạch vì tài nguyên chuẩn nằm trong thay đổi chưa commit; backup riêng bảo toàn trạng thái này. Không tự commit theo workflow skill.
- Dùng báo cáo này làm ledger ngắn; không thêm framework/script điều phối hoặc thư mục QA vào repository.
- Giữ đúng 22 StatId; năm Affinity cùng dùng cho offenseScale/resistanceScale theo phê duyệt.

## Cửa kiểm chứng

- **P1.5 đạt**: lượt compile sạch `RequestScriptCompilation(CleanBuildCache)` → `recompile_status` **completed, failed=false, errors=[], compilationFailed=false**.
- Unity kiểm 9 prefab: 0 missing script/object reference; HeroBase giữ 52 parts, Idle/Run và 38 animation bindings hợp lệ. Build Settings rỗng đúng giai đoạn chuyển tiếp.
- 169 file di chuyển; 87 file meta giữ nguyên byte. PNG/prefab/controller/clip di chuyển giữ nguyên SHA-256 so với backup; prefab môi trường không đổi byte. Không phụ thuộc hai Cartoon rig → đã xóa cả hai.
- 823 file tồn tại ở baseline bị xóa theo manifest; ArtDirection chỉ còn STYLE_GUIDE.md; root folder rỗng không tồn tại nên no-op. Giữ Recovery, cache, WebBuild và save cũ.
- Bằng chứng: `/private/tmp/nguthu-core-foundation/20261014-phase1/{dependencies-before.json,operations.json,references.json,compile.json}`. Đối chiếu cấu trúc trước/sau bằng byte hash từ archive và kiểm reference Unity sau move; không dựa vào snapshot vector bị serializer chuyển thành chuỗi.
- `git diff --check` đạt. Tài liệu hiện hành đã bỏ builder/Web QA và ghi rõ chưa có scene chơi.
- **P2.1–P2.7 đạt** sau cửa P1.5; kết quả thực tế và phạm vi bàn giao bên dưới.

## Phase 2 — kết quả thực tế

- Hoàn thiện 7 trụ cột trong 10 thư mục Domain: Core, Stats, Actors, Cultivation, Combat, Beasts, Inventory, World, Navigation, Persistence. Có 65 file C# Domain, 8 fixture và một file dữ liệu test dùng chung.
- Giữ **22 StatId**; Metal/Wood/Water/Fire/EarthAffinity dùng chung cho công và kháng. Không thêm ID Resistance. Modifier realm/equipment/status/bond dùng nguồn riêng, replace/remove đúng nguồn; current HP/Qi/Shield tách khỏi StatSheet.
- WorldFactory đăng ký 4 effect handler và 5 delivery selector. WorldState nhận command typed, tick mô phỏng, event sau commit; AI Follow/Assist dùng cùng geometry/combat. WorldInitialState nhận BeastSeed, không hardcode loài thú.
- Save schema `linhthu.foundation`/1 chỉ serialize/restore trong RAM. Restore graph mới, kiểm dữ liệu trước khi nhận, tái tạo derived modifier một lần; lưu được status/field assignment/cast/channel/projectile/hit history/RNG/clock/counters. Không đọc/ghi hoặc migrate save cũ của người dùng.
- Pin Unity Test Framework **1.8.0** đã resolve; Newtonsoft **3.2.2** có sẵn. Assembly Domain thực tế chỉ tham chiếu **netstandard, Newtonsoft.Json**, không Unity. Test assembly chỉ Editor.

### Compile và NUnit

- Unity **6000.6.4f1**, Editor đang mở đúng project, Pipeline **0.8.0-exp.1**.
- Compile cuối bằng `RequestScriptCompilation(CleanBuildCache)` → `recompile_status`: **completed; failed=false; errors=[]; compilationFailed=false**. Không thấy C# compiler warning trong log cuối; Pipeline trả warnings rỗng.
- Lượt NUnit cuối: **78 total / 78 passed / 0 failed / 0 skipped / 0 inconclusive**, thời lượng **2.1015763 giây**. Bắt đầu **14/10/2026 09:55:05**, kết thúc **09:55:08** (Asia/Ho_Chi_Minh).
- Chạy qua Unity Test Framework trong **EditMode**, filter assembly `LinhThu.Tests.EditMode`; XML được ghi bằng callback `ResultsSavingCallbacks`/`TestRunnerApi.SaveResultToFile` có sẵn. Kiểm tra sau lượt chạy không còn instance callback xuất tạm trong Editor; không thêm runner/QA script vào project.

| Fixture | Passed | Phạm vi chính |
| --- | ---: | --- |
| StatSheetTests | 4 | Công thức 154.44/128.7, cộng/trừ ba loại modifier, cache, nguồn, vitals/shield/death |
| CombatAndStatusTests | 24 | Damage 55, đủ năm Affinity, crit/Physical, năm shape, cooldown, stack policies, channel cancel, projectile/LOS, effect ghép và rejection |
| BeastAiAndRosterTests | 9 | Link +2, cap/recall/death, owner/egg/field, spawn blocked, Follow/Assist/ưu tiên attacker, leash an toàn và fallback |
| MovementAndCollisionTests | 6 | Input chéo, trượt circle/box/convex, vật cản mỏng, góc hẹp/map edge, điểm spawn và input lỗi |
| InventoryAndNodeTests | 10 | Stack/overflow, giao dịch kho đầy, herb/ore/egg, plant/growth/assignment/portal, pill/scroll/equipment, JSON round-trip |
| CoreAndActorTests | 3 | ID/RNG, event/unsubscribe/listener lỗi, actor chung cho faction và death đúng một lần |
| CultivationTests | 3 | Substage/EXP/overflow, breakthrough thay nguồn và giữ dư, missing/dead/terminal rejection |
| WorldStateTests | 19 | Command/tick tích hợp, deterministic seed, save giữa combat, không hit lặp, status/field qua nhiều lần load, save lỗi/schema/size/depth |

### Review và sửa lỗi trong quá trình kiểm chứng

- Lượt đầu 29/30: fixture damage đặt base Attack=40 rồi cộng thêm realm +5. Sửa fixture base=35 và assert final Attack=40; giữ nguyên công thức đã duyệt. Các lượt tiếp theo 68/68, 75/75 và cuối 78/78.
- Sửa projectile sinh giữa tick để chỉ nhận thời gian sau windup/pulse; test split delta xác nhận không đi sớm. Death/stagger hủy channel và không chạy thêm pulse.
- Thêm dự tính nhóm modifier status trước khi cast/use item để buff ghép lỗi không hồi máu một phần hoặc tiêu Qi/cooldown/item/source ID. Bổ sung rejection nguyên tử khi equip/breakthrough/summon gặp stat không hợp lệ.
- Chuẩn hóa format bằng Roslyn có sẵn, kiểm token trước/sau giống nhau. Tự review luồng command → mutation → event, tick order, source ownership, restore và assembly. Reviewer độc lập không chạy được do giới hạn tài khoản; không coi đây là kết quả review độc lập.
- `git diff --check` và `git diff --cached --check` đạt. Kiểm source không có Unity/file I/O/Resources.Load trong Domain, không TODO/NotImplemented hoặc builder/Checks mới. `StringReader` chỉ đọc chuỗi JSON trong bộ nhớ.
- Đối chiếu cuối với backup: **261 file Assets giữ lại/di chuyển không đổi byte**, không có art/prefab đổi byte, không có file baseline mất ngoài manifest, không thiếu meta. Archive SHA-256 vẫn khớp, gồm **81 file thiết kế gốc KRA/PSD/SVG**. Giữ nguyên các thay đổi mỹ thuật/shader có sẵn của chủ dự án.

### Bằng chứng và bàn giao

- Log/XML: `/private/tmp/nguthu-core-foundation/20261014-phase2/` gồm `compile.json`, `editmode.xml`, `result-final.json`, `assembly-and-export.json`, `source-sha256.json`, `verification.json`, `editor-cleanup.json`.
- Backup bền vững vẫn tại `/Users/nguyenhoangdacdat/dev/nguthu-backups/core-foundation-backup/20261014T014359Z/`, không ở tmp. Manifest chứa SHA-256 từng file và archive; hướng khôi phục xem [ROLLBACK.md](../ROLLBACK.md).
- Đã hoàn thành điểm bàn giao **Phase 1 và Phase 2**. **Phase 3–4 chưa triển khai**: Client vẫn tên assembly chuyển tiếp, Material/JSON Resources còn trong Client chờ chuyển reference serialize, chưa có Config SO/DevTools/MainGame/GameBootstrapper/HUD mới. Không có scene chơi ở cuối Phase 2 theo kế hoạch; chưa nghiệm thu visuals/Play Mode.
- Không commit, push, deploy hoặc build WebGL. Không chạy migration hay ghi save người dùng.

## Phase 3 — đạt ngày 14/10/2026

- 30 config assets, ba outfit Sprite Swap trên 25 slot, giữ controller/clip/binding HeroBase. 9 prefab actor/node/camp mới; chuẩn hóa 8 map/prop cũ. Client và DevTools tách assembly; không còn Resources.Load trong source project.
- Backup trước sửa prefab: `~/dev/nguthu-backups/core-foundation-backup/phase3-20261014T030912Z/source-before-phase-3.tar.gz`, SHA-256 `f4ff36ab72f51e6790b6d60ff4add1cc92a5c80ac0b5ed249db8a9459648d0cb`.
- Unity compile 0 lỗi; NUnit **84/84 passed**, 0 failed/skipped, 2.9010635 giây (03:28:18–03:28:21 UTC). Gồm 78 Domain và 6 Client integration tests. Lần đầu test null Sprite dùng so sánh tham chiếu .NET sai; đã đổi assertion sang equality Unity, giữ đầy đủ kiểm chứng clip/pose/binding.
- Bằng chứng: `/private/tmp/nguthu-core-foundation/20261014-phase3/{compile.json,editmode.xml,test-status.json,source-sha256.json,prefab-authoring.json,outfit-migration.json}`. Visual vẫn cần chủ dự án nghiệm thu bằng Play.

## Phase 4 — đạt ngày 14/10/2026

- MainGame là scene duy nhất trong Assets/_Project và Build Settings; đủ MeadowMainMap, PlayerCultivator, HuyenQuyBeast, TrainingDummy, SpiritHerb/SpiritOre/SpiritEgg/SpiritField/Portal và Camp. GameBootstrapper đọc ContentRegistrySO, nhận Input System, fixed step 1/60s, đồng bộ view/HUD theo Domain.
- Cây chuẩn: Prefabs/WorldNodes, UI/CultivationHud; Client/Combat, Camera/TopDownCameraController, UI/CultivationHudPresenter. Mọi bước đổi tên đều giữ meta.
- Bổ sung WorldSaveStore cho F5/F9 và nút HUD: ghi pending → replace + backup, restore graph riêng rồi đổi world. Tests dùng thư mục tạm riêng và xóa sau chạy; không ghi save thật của người dùng.
- Rà soát phát hiện field.png là ảnh toàn cảnh và icon cũ là ô màu: thay reference runtime bằng mesh riêng cho linh điền/cổng/trứng/trại, sprite thật cho thảo/quặng. Cả MeadowMainMap/MeadowMap dùng tiling + mesh; MeadowMap giữ GUID và 21 vị trí prop. Quặng/trại có footprint chân; điểm tương tác quặng đặt ở mép trước để không bị chính footprint chặn.
- Unity **clean compile 0 lỗi**. NUnit cuối: **91/91 passed**, 0 failed/skipped/inconclusive, **3.201004 giây**, 04:01:31–04:01:34 UTC. Gồm **78 Domain + 13 Client/scene integration**. Kiểm scene thật, nút HUD, damage/cooldown, harvest/plant/portal, recall/bond/outfit, atomic save và corrupted save giữ live world.
- Play thực tế qua API: đủ 3 actor/5 node, đổi outfit; recall/resummon giữ attack; Hộ Thể tạo 60 shield; projectile có trong world; 68 shadow mesh hoạt động. Input System với bàn phím mô phỏng D đi qua Update, X từ −1.8 → 0.359999, đã tháo thiết bị. World tiến đến tick 5915. Đã dừng Play, trả runInBackground về false; không thay PlayerSettings này.
- Lúc Editor mất focus, runInBackground=false khiến tick dừng; bật tạm trong lượt kiểm tra mới có update liên tục. Đây là điều kiện môi trường, không sửa luật mô phỏng. HUD không còn text overflow; lượt Play cuối không có error/warning mới, Unity Console hiện 0 error/0 warning. Không chụp hình tự nghiệm thu.
- Đối chiếu lại backup SHA-256: hai archive đúng hash. 87 meta di chuyển Phase 1 và 19 meta Phase 3 còn nguyên byte. 164 file PNG/meta/animation/shader so với snapshot Phase 3 nguyên byte. Kiểm tên cấm, Domain engine dependency, Resources.Load, file nguồn mỹ thuật cấm và path đã purge đều đạt.
- Source/docs diff kiểm whitespace đạt; toàn bộ diff còn whitespace rỗng `m_Name: ` do Unity tự serialize prefab. Không sửa YAML để xóa khoảng trắng.
- Bằng chứng tại `/private/tmp/nguthu-core-foundation/20261014-phase4/`: compile.json, editmode.xml, test-status.json, final-source-sha256.json, completion-audit-static.json, completion-audit-unity.json, meta-preservation.json, play-actions.json, play-input.json, console-final-play.json. Output tạm có thể mất sau reboot; backup artwork bền vững vẫn ở thư mục đã duyệt.

## Đối chiếu hoàn thành

| Yêu cầu | Bằng chứng |
| --- | --- |
| Cập nhật AGENTS, kế hoạch 1–2 và phê duyệt trước triển khai | AGENTS.md, CORE_FOUNDATION_PHASES_1_2.md, phê duyệt chủ dự án trong phiên |
| Phase 1: purge/hợp nhất, meta/GUID, Cartoon dependency, backup master ngoài tmp | Manifest, compile/dependency checkpoint Phase 1; audit cuối và hai archive SHA-256 |
| Phase 2: bảy trụ cột, 22 StatId, năm fixture bắt buộc, JSON DTO | 65 source Domain, assembly refs chỉ netstandard/Newtonsoft.Json; 78 tests vẫn đạt trong suite cuối |
| Phase 3: SO mẫu, ba outfit, Client views/prefab và DevTools | 30 config assets, 20 prefab toàn project, Client integration tests, reference scan |
| Phase 4: scene duy nhất, composition/input/game loop/HUD | Scene/Build Settings audit, 7 game-loop integration tests và Play/Input System trực tiếp |
| Compile 0 lỗi mỗi Phase | checkpoint Phase 1–3 và clean compile cuối Phase 4 |

Hiển thị chưa được chủ dự án duyệt: xem idle/run, seams, flip, swap giữa clip, tiếp đất/bóng và giao diện trên màn hình thật. Compile/tests chứng minh cấu trúc/logic và wiring; không tuyên bố đạt chuẩn mỹ thuật. Config là thông số mẫu, không mở rộng thêm luật kiếm tu vi/crafting/economy ngoài chỉ thị. Không commit/push/build WebGL.

Retrospective: test scene thật phát hiện lỗi tương tác do footprint mới; kiểm asset trực tiếp phát hiện placeholder mà unit test không biết. Giữ test hồi quy cho hai trường hợp và quy tắc nghiệm thu tách logic/hiển thị đã có trong AGENTS; không thêm framework/QA script.

## Vệ sinh repository và tinh chỉnh tương tác/hiển thị — 14/10/2026

- Đã xóa 13 tài liệu lịch sử Architecture theo chỉ thị, 9 meta ở gốc Assets/ArtTrial và meta mồ côi Content/Combat. Architecture chỉ còn 11 tài liệu được giữ; sửa các liên kết cũ và bổ sung ba quy tắc Periodic Repository Hygiene vào AGENTS. Quét cuối không có meta mồ côi, asset thiếu meta hoặc thư mục Assets rỗng.
- Domain có InteractionTargetSelector và InteractCommand: một mục tiêu sẵn sàng trong tầm/đường nhìn, ưu tiên khoảng cách, hướng nhìn và EntityId khi ngang nhau. Nhích 0.002 đơn vị đổi mục tiêu; thu hoạch tự chọn node kế tiếp ngay, không cần di chuyển/Tick. Nhãn nổi, nút E và command dùng cùng kết quả.
- PlayerCultivator/TrainingDummy dùng visual scale 0.625, bán kính chân 0.1375, chiều cao tham chiếu bóng 1.625. Giữ rig, controller, clip và ba outfit. Mộc Nhân Luyện Công có tên/HP/status thật; không thêm luật Thiêu đốt vào dữ liệu mẫu.
- Hai material terrain tăng độ rõ vân cỏ, giảm mip bias, thêm sắc ấm/mát và vùng mát quanh hồ; đường dùng cùng tham số với nền. Thêm 12 khóm gồm 36 prop cỏ/hoa nhỏ. Dời lều khỏi tán cây, bố trí lại năm node, thêm footprint trứng. Linh Điền trước đây nằm trên đường; đã dời khỏi cả vùng rìa spline. Loại 13 bóng sprite cũ trong MeadowMainMap và bóng trong prefab cỏ/hoa.
- Unity compile cuối **0 lỗi**; NUnit **104/104 passed** (85 Domain + 19 Client), 0 failed/skipped/inconclusive, 3.396436 giây, 07:00:58–07:01:01 UTC. Thêm 7 test Domain và 6 test scene integration. Bao gồm chọn/đổi mục tiêu, cooldown/depleted/LOS, thất bại nguyên tử, HUD/command đồng bộ, HP/status, outfit/scale, footprint và khoảng cách Linh Điền–đường/lều–tán cây.
- Play trong Unity Editor: nhãn neo sai lệch **0 pixel**, E chuyển Trứng Huyền Quy → Linh Điền tức thì, cả năm tên cụ thể vừa nút E (cao chữ 32.69 trong vùng 34). HP sau thi triển và tiến mô phỏng 0.5 giây là 972.8003/1000, bar 0.9728003; hiện Hàn khí 2.0s. Không còn bóng cỏ, text overflow hoặc thông báo lỗi shader. Console groundTruth cuối **0 error/0 warning**; log đoạn Play không có exception hay cảnh báo meta. Đã dừng Play và trả runInBackground về false.
- PreviewScene của EditMode không khởi tạo kích thước/scale Canvas Overlay; kiểm pixel và text overflow được thực hiện trong Play có Canvas thật. Không dùng kết quả PreviewScene làm bằng chứng bố cục. Hiển thị đẹp/đúng ý vẫn do chủ dự án mở MainGame bấm Play nghiệm thu; không build/chụp WebGL.
- Backup trước tinh chỉnh: `~/dev/nguthu-backups/core-foundation-backup/20261014T045511Z-visual-polish/`, gồm archive, manifest SHA-256 và diff có sẵn. 230 meta trong bản đối chiếu giữ nguyên byte. Bằng chứng compile/XML/Play/hygiene tại `/private/tmp/nguthu-core-foundation/20261014T045511Z-visual-polish/`; không đưa log hoặc backup vào Git.
- Không ghi save người dùng. Save chứa bố cục node cũ vẫn bị load validation từ chối, giữ nguyên live world và file save; chưa có migration vị trí. Không commit/push trong đợt tinh chỉnh này.

Retrospective: test khoảng cách phát hiện Linh Điền còn chạm rìa đường; Play phát hiện text overflow và bóng sprite còn trong instance dù prefab gốc đã sạch. Đã sửa asset và giữ assertion hồi quy về footprint/bóng; tiếp tục tách kiểm logic trong EditMode khỏi Canvas thật/đánh giá mỹ thuật trong Editor.

## Core Gameplay Feel / Appearance / Meadow — 14/10/2026

Chỉ thị: `/Users/nguyenhoangdacdat/.codex/attachments/e1a22ed2-a3fc-48b7-ac5f-fc28cee28a27/pasted-text-1.txt`. Baseline `be8b0ff`; working tree sạch. Thực thi trực tiếp theo thứ tự A → B → C đã được yêu cầu. Giữ InteractionTargetSelector, nhãn E, overhead HP/status, scale 0.625, GUID/rig/clip; không làm Dashboard, Spring Bone, netcode hoặc ghi save cũ.

- **A:** `MovementSpec` cấu hình acceleration/deceleration/turn và dash; `Transform2D.CurrentVelocity`, `ActorEntity.Dash`, `DashCommand`. Input chỉ gửi command, mọi actor đi qua motor/WorldState; view lấy vận tốc và state. Thử duration 0.2s, tốc độ đỉnh 4× đi bộ, cooldown 1s; không có miễn thương. Dash khóa hướng, tích phân ease-out thuần C#, dùng WorldGeometry để chặn/trượt. Test gia tốc/phanh/đổi hướng, cooldown độc lập, va chạm, chết/stagger, round-trip đang dash.
- **B:** `ActorAppearance` gồm OutfitId/HairId/EyeId; config cấp danh sách ID hợp lệ. Equipment cập nhật outfit và giữ hair/eye; `ChangeAppearanceCommand` kiểm toàn bộ trước mutation. Renderer đổi sprite theo slot, MPB tint/flash, giữ shared material và animator. Tab đổi áo; H/Y đổi tóc/mắt. Schema 2 lưu appearance/vận tốc/dash, từ chối schema cũ nếu chưa có migration; Client dùng file phiên bản riêng. Test ID lỗi/đổi slot/round-trip và prefab/MPB.
- **C:** giữ texture meadow-grass/meadow-earth hiện có, thêm dappled light thống nhất nền/đường; InteractiveFoliage đọc mọi actor và hồi phục sau tiếp xúc. FootstepDust chỉ đọc thay đổi vận tốc/Dashing; petals tập trung quanh cherry-tree. Authoring prefab qua Editor API, không YAML. Test wiring, shared material, foliage/dust activation, bảo toàn các phần đã duyệt.
- **Cửa hoàn tất:** Unity compile 0 lỗi, toàn bộ EditMode pass, đọc lại wiring/prefab thực tế, hygiene/meta/GUID và git diff/status. Không dùng kết quả 104 tests trước đây thay kết quả của đợt này. Kế hoạch/ledger giữ ngay tại tài liệu này theo convention repo, không tạo framework/QA builder hoặc commit tự động.

Kết quả đã kiểm chứng:

- A: compile 0 lỗi, 110/110 EditMode đạt. B: compile 0 lỗi, 118/118 đạt (kết quả sau sửa assertion Unity Object null được lưu ở `b-passing-status.json`; XML lần B đầu còn ghi test lỗi, không dùng XML đó làm bằng chứng pass).
- Toàn bộ A–B–C: compile **0 lỗi**; NUnit **122/122 passed** (97 Domain + 25 Client), 0 failed/skipped/inconclusive. XML 08:41:36–08:41:40 UTC, 3.8938366 giây. `GameFeelTests`, `AppearanceTests`, `EnvironmentFeelTests` và integration tests kiểm gia/phanh/đổi hướng, dash khóa hướng/cooldown/va chạm/save, 12 tổ hợp appearance, ID lỗi, sprite/MPB/rig, cỏ phản ứng với player và companion, bụi start/brake/dash, prefab/material/texture.
- Play MainGame thật: frame tăng khi quan sát; tốc độ đi 0.56 → 3.6, phanh về 0 và trượt thêm 0.2945; dash đầu 11.664, hết sau 0.2s, lệnh kế tiếp trả OnCooldown. Animator speed theo vận tốc và Visual có tilt. Đổi H/Y/Tab thành jade/ink/amber giữ scale 0.625, toàn bộ shared material giữ nguyên. Cast từ HUD giảm HP Mộc Nhân 995.3198 → 954.5195, hit flash=1; overhead vẫn hiện.
- Bụi runtime phát 3 hạt khi bắt đầu, tăng khi phanh/dash, mỗi hệ tối đa 64 hạt. InteractiveFoliage có mặt ở mọi grass-tuft/bush dùng trong scene, gồm node Linh thảo; kiểm runtime contact angle -3.448° ở vận tốc 1.4. Hai cây cherry có mesh 246 đỉnh/cây; vị trí cánh hoa đổi qua hai frame quan sát. Cả ba shader sửa/thêm supported, không có shader diagnostic. HUD message không overflow. Console ground truth cuối **0 error/0 warning**, compilationFailed=false; các lỗi test cũ trong buffer không được tính thành lỗi Play mới.
- Review độc lập phần Domain/persistence/appearance không phát hiện critical; phát hiện nhịp nhún BeastView nhảy pha khi vận tốc đổi đã sửa thành pha tích lũy deltaTime. ParticleSystem.EmitParams bỏ qua khi hệ dừng đã sửa bằng Play(false) trước emit; test hồi quy đạt. Đã dừng Play, trả runInBackground=false, scene không dirty. Không ghi save người dùng.
- Hygiene: 0 meta mồ côi, 0 asset/folder thiếu meta, 0 thư mục Assets rỗng; **381 meta cũ giữ nguyên byte**. Assets chỉ còn _Project và TextMesh Pro cùng meta. `git diff --check` sạch cho C#/shader/Markdown; serializer Unity sinh 19 dòng m_Name rỗng có whitespace trong prefab, không sửa YAML bằng tay. Không có thay đổi DevTools/OWNER_DASHBOARD, không thêm Spring Bone/builder/Resources.Load.
- Backup trước thay đổi: `~/dev/nguthu-backups/core-foundation-backup/20261014T080217Z-game-feel/source-before-game-feel.tar.gz` cùng `manifest-sha256.json`, đã xác minh archive/447 file. Bằng chứng compile/NUnit/Play/hygiene tại `/private/tmp/nguthu-core-foundation/20261014T080217Z-game-feel/`. README và CODE_ARCHITECTURE đã đồng bộ phím mới, state pipeline và save schema 2.

Giới hạn: các thông số gia tốc/phanh/dash/gió/bụi vẫn là tuning mẫu; nghiệm thu cảm giác và mỹ thuật cuối do chủ dự án Play trong Editor. Không có benchmark batching/crowd hoặc netcode; chỉ xác minh dùng chung material, không tạo material instance khi đổi ngoại hình/flash. Save schema 1 không được tự migrate.

Retrospective: preview test phát hiện hệ hạt chưa chạy và node dùng sprite cỏ ngoài nhánh map; binding đã mở rộng đúng scene và particles khởi động khi emit. Test vật liệu kiểm identity chung; không suy ra số draw calls từ identity.

## Tỷ lệ Tu Tiên / Stride / Thiền và câu cá — 14/10/2026

Chỉ thị: `/Users/nguyenhoangdacdat/.codex/attachments/795b34b0-b95f-4ec5-acbe-867df72f199a/pasted-text-1.txt`. Tiếp tục trên working tree của Core Gameplay Feel, bảo toàn các thay đổi chưa commit; triển khai A → B → C → D. Không mở rộng Dashboard hoặc hệ đặt Linh Điền. Domain vẫn `noEngineReferences: true`; mọi luật hành động, thời gian và phần thưởng thuộc WorldState/command.

- **A — Tỷ lệ:** toàn cụm đầu và sáu slot con còn 78%; thân dài thêm 5%, chân 8%, vai thu hẹp và tay hạ gần thân. Giữ visual scale 0.625, outfit mặc định classic, toàn bộ appearance/hair/eye và MPB. Sửa pose nghỉ trong prefab và clip qua Unity API, giữ GUID, controller và đường dẫn xương.
- **B — Stride:** MoveSpeed mẫu 2.8, acceleration 20, deceleration 28. Hiệu chỉnh chân Run bằng IK hai khớp: chân trụ đi ngược 0.8 đơn vị rig trong 0.4 giây; scale 0.625 cho BaseStrideSpeed **1.25**. Animator speed = vận tốc thực / 1.25 đến ngưỡng 6; trên ngưỡng dùng hàm log có đạo hàm liên tục. Giữ dash ease-out 0.2 giây, cooldown riêng và va chạm Domain. Test cả hai chân ở tốc độ 0.2/1.4/2.8/4.2/6: sai lệch chân trụ ngang/dọc dưới 0.003 đơn vị trong khoảng đo.
- **C — Động tác:** thêm LifeActivityState/Service, ToggleMeditateCommand và FishCommand. X thiền hồi Qi ×3 và 1 Tu vi/giây; WASD, dash, cast hợp lệ, damage/stagger hủy hoạt động. F hoặc E tại Bờ câu Thanh Khê thả câu; sau 4 giây bấm lại để nhận 1 Linh Ngư. Thu trước khi cá cắn thì cất cần; túi đầy giữ cá đang cắn và node, không mất thưởng. Đây là tuning mẫu, chưa là cân bằng sản phẩm.
- ActionPoseController blend tư thế trong 0.22 giây; ToolSocket nằm dưới hand_front. CharacterToolAttachmentPresenter đọc state để hiện cần trúc/dây/phao/gợn sóng và hạt linh khí. Bờ câu ở (5.4, −2.4), phao ở (6.8, −1.6) trong hồ; MainGame hiện có 6 node. Save schema 2 thêm dữ liệu activity và validate khi restore; tiếp tục lưu đủ outfit/hair/eye. Không ghi save người dùng trong kiểm chứng.
- **D — Meadow:** giữ texture cỏ vẽ tay và ánh sáng loang, foliage phản ứng với tu sĩ/linh thú từ mốc trước. FootstepDust bổ sung bụi theo hai lần chân tiếp đất mỗi chu kỳ Run, chỉ phát một lần khi qua pha; vẫn có bụi đổi hướng/phanh/dash. Không đưa điều khiển chuyển động vào presentation.

Kiểm chứng thực tế:

- Unity compile **0 lỗi** (`failed=false`, `compilationFailed=false`). NUnit cuối sau sửa prefab overhead: **142/142 passed**, gồm **113 Domain + 29 Client**, 0 failed/skipped/inconclusive; 10:28:29–10:28:33 UTC, **4.4984701 giây**. Thêm 16 ca Domain và 4 ca Client về hủy hoạt động, Qi/EXP, câu cá/túi đầy/thu đúng một lần, save appearance/activity, tỷ lệ, stride, pose/tool và bụi theo nhịp chân.
- Play MainGame qua API: classic, đầu 0.78, MoveSpeed 2.8, BaseStrideSpeed 1.25. Sau 53 giây thiền: Tu vi 53, Qi từ 30.05 hồi tới giới hạn 70, pose weight 1 và 19 hạt linh khí. MoveCommand đưa state khỏi thiền; câu cá hiện đúng cần/phao, sai lệch điểm cầm **0**, phao cách đích nước 0.00271. InteractFocused (cùng đường xử lý E) nhận đúng 1 Linh Ngư rồi cất cần. Không mô phỏng bàn phím phần X/F trong lượt Play này; mapping đã kiểm tra source.
- HUD thông báo thiền, cá cắn, nhãn bờ câu và nút giật cần không tràn chữ. Kiểm Play phát hiện nhãn overhead “Hàn khí” cần 17.71 nhưng khung cao 16; đã tăng riêng chiều cao Statuses trong ActorOverhead.prefab lên 20 qua Unity API, giữ font và bố cục còn lại. Play cuối xác nhận “Hàn khí 1.4s” không overflow; quét mọi nhãn đang hiện và có chữ không còn overflow.
- Khi trở về Idle, hai pose weight đều 0, pelvis trở về vị trí clip, cần đã ẩn. Scene có 3 actor/6 node, **0 missing script**, không shader unsupported/error. Console ground truth **0 error/0 warning**, compilationFailed=false. Đã thoát Play, trả runInBackground=false; scene không dirty. Nghiệm thu hình ảnh/cảm giác cuối vẫn do chủ dự án Play trong Editor.
- Hygiene: **0 meta mồ côi, 0 asset/folder thiếu meta, 0 thư mục Assets rỗng**. **348 meta có trong backup đầu lượt giữ nguyên byte**. Assets chỉ có _Project và TextMesh Pro cùng meta; không đổi DevTools/OWNER_DASHBOARD, không thêm Resources.Load hoặc builder. C#/shader/Markdown qua `git diff --check`; các dòng YAML do Unity serialize không sửa tay.
- Backup bền vững trước lượt: `~/dev/nguthu-backups/core-foundation-backup/20261014T092338Z-stride-life-actions/`, gồm 680 file, manifest và diff baseline. Archive SHA-256 đã đối chiếu: `826d4a2d046881cecef2af594867a536a7dc3a67a5853f327fd20cccde84095c`. Kết quả compile/NUnit/Play/hygiene ở `/private/tmp/nguthu-core-foundation/20261014T092338Z-stride-life-actions/`. Không commit/push.

Giới hạn save: schema 2 cũ thiếu trường activity vẫn mặc định không hoạt động; save MainGame có bố cục 5 node cũ bị Client từ chối vì scene nay có 6 node. File cũ và live world được giữ; chưa làm migration bố cục.

Retrospective: test đo chân trụ kiểm chứng calibration từ clip thay vì chỉ kiểm công thức animator.speed. Test đúng mốc 4 giây phát hiện sai số cộng float ở timer câu cá; đã chặn sai số biên và giữ assertion về thời gian/thu thưởng. Play bổ sung bằng chứng hạt/ToolSocket/HUD trên vòng Update thực tế; không thay việc chủ dự án đánh giá hình ảnh.

## Sửa dáng chạy, nhãn vật thể và câu quanh hồ — 14/10/2026

Phản hồi mới của chủ dự án thay cách đặt nhãn lên người chơi: giữ tên Linh dược/Trứng… trên từng vật thể; “Thả câu” nằm trên hồ và người chơi có thể câu từ mọi phía quanh bờ. Tiếp tục trên working tree chưa commit của mốc trước.

- Clip Run cũ hạ hông xuống khoảng 0.72, gập gối trước tới 122°, tạo dáng co chân. Đã chỉnh clip qua Unity API: hông 0.845–0.905, gần Idle 0.9147; gối dưới hông, góc gập dưới 85°. Chân trụ lùi 0.6 rig unit/0.4 giây; scale 0.625 cho `baseStrideSpeed=0.9375` trên HeroBase/PlayerCultivator/TrainingDummy. Thông số 1.25 của mốc trước được thay bằng hiệu chỉnh này. Giữ MoveSpeed 2.8, controller, GUID, rig và outfit.
- `WorldNodeView.LabelPosition` neo toàn bộ khung tương tác trên vật thể; tên item giữ nguyên. FishingSpot tham chiếu renderer hồ qua `labelVisual`, bỏ ShoreMarker một phía. `FloatingTextPresenter` đặt chữ sát thương trên `ActorView.OverheadPosition` sau animation, quy đổi TMP bounds từ local sang world.
- `FishingShoreline` có 12 đỉnh trùng footprint hồ. Domain đo tầm và chọn focus tới mép bờ gần nhất; ClearLine dừng ngay ngoài bờ để hồ không tự chặn tương tác. Người chơi phải đứng được; đá/vật cản khác vẫn chặn. E/F đều dùng cùng luật, không dịch nhân vật vào nước. Giữ vị trí node và đích phao cũ; không đổi schema hoặc ghi save người dùng.
- Compile cuối **0 lỗi**. NUnit **148/148 passed**, **116 Domain + 32 Client**, 0 failed/skipped/inconclusive, **11:02:46–11:02:50 UTC**, **4.3946026 giây**. Kiểm E/F tại **60 vị trí** trên 12 cạnh/góc của hồ thật, tất cả đều đứng được và chọn FishingSpot. Kiểm thêm ngoài tầm, vật cản, polygon lỗi, save/restore khi đang câu, thu đúng một lần, tên/anchor item, chữ sát thương theo actor, độ cao hông/gối và stride cả hai chân ở năm tốc độ.
- Play MainGame thật: nhãn hồ `[E] Thả câu` và item `[E] Trứng Huyền Quy` có đáy cao hơn đỉnh ảnh **17.26 px** ở Game view 1339×712, không overflow. Nhãn đổi thành Thu cần/Giật cần theo Domain; giật cần nhận đúng **1 Linh Ngư**. Chữ sát thương có đáy tại y=2.06198049, đầu actor y=2.06198072 (sai số float dưới 0.000001). Không mô phỏng phím vật lý; gọi cùng entry point của HUD/input.
- Test ban đầu phát hiện sai hệ tọa độ TMP bounds; đã sửa code và xác minh lại. Hai fixture đi tới hồ theo đường thẳng bị mỏ khoáng chặn; đã cho đi theo đường trống và assert tới đúng bờ. PreviewScene không nhận SendMessage lifecycle nên test chữ gọi trực tiếp pha LateUpdate, giữ kiểm tra vị trí thực. Không bỏ qua assertion/log lỗi để làm test xanh.
- AGENTS, GAME_DESIGN, STYLE_GUIDE, README và CODE_ARCHITECTURE đã đồng bộ quy tắc nhãn vật thể. Backup bền vững và SHA-256 tại `~/dev/nguthu-backups/core-foundation-backup/20261014T104039Z-run-pose-overhead/`; compile/NUnit/Play log tại `/private/tmp/nguthu-core-foundation/20261014T104039Z-run-pose-overhead/`. Không commit/push.
- Sau kiểm chứng: MainGame đang mở ở Edit Mode, không dirty, runInBackground=false, 0 missing script. Đoạn Editor.log của lượt Play mới không có error/warning/exception/assertion. Hygiene cuối: 0 meta mồ côi/thiếu, 0 thư mục Assets rỗng; 23 meta trong hai manifest giữ nguyên byte, cả hai archive khớp SHA-256. C#/Markdown/shader/asmdef qua `git diff --check`; giữ nguyên whitespace serializer Unity trong YAML. Git có 113 đường dẫn thay đổi gồm công việc kế thừa, không có file staged hoặc log/fixture tạm trong repo.

Giới hạn: thông số pose là hiệu chỉnh triển khai; chủ dự án vẫn nghiệm thu cảm giác chạy trực tiếp trong Editor. Các số đo/test xác minh tư thế và chuyển động, không thay cho duyệt mỹ thuật.
