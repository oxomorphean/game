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
