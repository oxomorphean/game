# Core Foundation — kế hoạch thực thi Phase 1 & Phase 2

> **Trạng thái: PHASE 1–2 ĐÃ QUA KIỂM CHỨNG ngày 14/10/2026 — compile 0 lỗi, 78/78 EditMode tests đạt.**
> Chi tiết bằng chứng và giới hạn: [CORE_FOUNDATION_PROGRESS.md](CORE_FOUNDATION_PROGRESS.md).
> **For agentic workers:** sau khi kế hoạch được duyệt, dùng `superpowers:executing-plans` để thực hiện tuần tự các task có checkbox. Đề xuất một agent tích hợp trực tiếp vì asset/GUID và interface có phụ thuộc chặt; không mặc định mở nhiều agent sửa chung.

**Goal:** hợp nhất tài nguyên còn dùng và xây nền Domain có thể mở rộng bằng definition/effect module, với Unity compile 0 lỗi cuối từng Phase và EditMode tests kiểm chứng bảy trụ cột.

**Architecture:** Domain là Pure C#, Client chuyển cấu hình và input thành dữ liệu/lệnh. Giữ thuật toán hình học hiện hành, rig HeroC2, map Meadow và shader; loại các nhánh thử cùng công cụ dựng scene. `WorldState` sở hữu mô phỏng; SO, prefab và Composition Root sẽ nối vào ở Phase 3–4.

**Tech Stack:** Unity `6000.6.4f1`; C#; Unity Test Framework `1.8.0`/NUnit đang được resolve; Newtonsoft JSON package `3.2.2` đã có; Input System/2D Animation/uGUI hiện có. Không thêm DI/ECS/event framework hoặc dependency trả phí.

**Spec:** chỉ thị kiến trúc Phần 1–4 đính kèm và lộ trình Phần 5 của chủ dự án ngày **14/10/2026**, được ghi thành quy chuẩn tại [AGENTS.md](../AGENTS.md). Các lựa chọn kỹ thuật ở mục 2 và 5 đã được chủ dự án duyệt, kèm ba điều chỉnh bắt buộc ghi trong AGENTS.md. Đây không phải tuyên bố đã triển khai hoặc đã được nghiệm thu.

## 1. Hiện trạng đã khảo sát

- Branch `main`; đầu lượt khảo sát có **66 file sửa, 925 file đã bị xóa, 18 mục untracked** theo `git status --porcelain`. Đây là baseline có sẵn, không phải thay đổi do kế hoạch này tạo. Không khôi phục các file đã xóa, không reset/clean/stash toàn repo. Recheck trước khi thực thi vì phiên khác có thể tiếp tục làm việc.
- `GAME_DESIGN.md` và `Architecture/CODE_ARCHITECTURE.md` hiện còn mô tả bản art-only ngày 12/10. Chỉ thị mới cho phép xây lại gameplay theo bốn Phase; không khôi phục nguyên gameplay cũ trong `Recovery/`.
- Domain đang biên dịch chỉ có `Navigation/WorldGeometry.cs` và `Navigation/PreviewMovement.cs`; đã có `LinhThu.Domain.asmdef` với `noEngineReferences: true`. File `WorldGeometry.cs` hiện định nghĩa `WorldPoint`, `WorldObstacle`, `MovementArena`, chưa có class tên `WorldGeometry`.
- Client hiện là `LinhThu.Prototype.asmdef`. Luồng chạy: `ArtPreviewHost → PreviewMovement → DirectionalHeroView`; `ArtPreviewHud` tạo `OwnerDashboard`. Dashboard gọi trực tiếp `ArtPreviewHost` và nạp JSON catalogue do `OwnerCatalogBuilder` sinh.
- Build Settings chỉ bật `Content/Scenes/ArtPreview.unity`. Bốn scene thử khác: `HeroModularTrial`, `HeroProductionTrial`, `CartoonArtTrial`, và `Content/Characters/HeroC2/HeroC2Trial.unity`.
- HeroC2 có 52 PNG với `.meta`, prefab, controller và Idle/Run đã lưu. Prefab dùng `HeroC2Preview`, đổi trang phục bằng bật/tắt các layer; **chưa phải Sprite Swap theo `OutfitConfigSO`**. Giữ xương/pivot/animation, chuyển renderer thành pipeline yêu cầu ở Phase 3.
- `MeadowMainMap.prefab` tham chiếu `MeadowGroundSurface`, `MeadowTerrainPath`, `ModularMapLayout`, `ModularMapProp`, `ObstacleAuthoring`, material trong `Art/CartoonV2/Terrain` và `Resources/Art`. Không được xóa các phụ thuộc này theo tên thư mục cũ.
- Hai PSD trong Assets thuộc nhánh sẽ xóa: `Art/HeroProduction2D/{hero-three-quarter.psd,import-probe.psd}`. `ArtDirection` còn master KRA/PSD/SVG và script; yêu cầu “chỉ giữ STYLE_GUIDE.md” bao gồm cả chúng, nên phải có bản khôi phục ngoài checkout trước khi xóa.
- Các thư mục root `ArtTrial`, `DemoArt`, `Editor`, `Scenes`, `Scripts` và `.meta` tương ứng **không tồn tại** tại thời điểm khảo sát. Bước này hiện là kiểm tra/no-op; không suy rộng sang thư mục có nội dung dưới `Assets/`.
- Unity CLI và binary Editor đúng phiên bản có trên máy. `unity status --json` chưa phát hiện instance Pipeline và có cảnh báo không kiểm tra được editor đang chạy; đây không phải bằng chứng Editor đã đóng. Lượt lập kế hoạch **chưa chạy compile/tests**.

## 2. Các quyết định đề xuất để duyệt

1. **Tái dùng có kiểm soát:** giữ hình học, art/rig/map/shader còn dùng; xây mới bảy hệ Domain. Không nhập lại nguyên gameplay retired. Làm lại cả geometry gây rủi ro hồi quy không cần thiết; giữ toàn bộ runtime cũ thì tiếp tục mang nhánh thử và phụ thuộc cũ.
2. **Tọa độ:** `Transform2D.Position/FacingDirection` dùng `WorldPoint` hiện có thay cho `UnityEngine.Vector2`. Client chuyển đổi tại biên. Không tạo kiểu vector mới hoặc dependency toán học chỉ để đổi tên.
3. **Tên save:** thống nhất `GameSaveDto`, theo cây thư mục và yêu cầu test. `SaveStateDto` trong mô tả là cùng một khái niệm, không tạo schema thứ hai. Schema mới `linhthu.foundation`, phiên bản đầu `1`; save cũ không tự load/migrate/ghi đè.
4. **Giới hạn lời hứa mở rộng:** thêm realm/technique/beast/item dùng dữ liệu; thêm loại effect mới thêm handler và đăng ký ở composition, không sửa resolver bằng switch theo ID công pháp. Vì đặc tả yêu cầu `StatId` là enum, thêm một stat mới vẫn cần thêm enum member, còn thuật toán `StatSheet` không đổi. Không tuyên bố mọi cơ chế tương lai đều chỉ cần SO.
5. **Chính sách mặc định cần duyệt:** công thức Ngũ hành, stack, cộng hưởng, chết/thu hồi và đột phá ở mục 5. Các con số trong unit test chỉ là fixture; dữ liệu cân bằng Luyện Khí/Trúc Cơ và ba công pháp được tạo tại Phase 3.
6. **Khoảng chuyển tiếp:** cuối Phase 1–2 yêu cầu compile/test, chưa có scene chơi. Xóa các scene cũ và bỏ entry Build Settings trước; `MainGame.unity` được tạo đúng Phase 4. Không tạo scene rỗng rồi báo đã có game loop.
7. **Hai sửa Client tối thiểu trong Phase 1:** bỏ phụ thuộc dashboard vào host bị xóa; đổi tên và gỡ UI thử của `HeroC2Preview` nhưng giữ serialized data/GUID. Đây là điều kiện dọn sạch mà không làm vỡ compile/prefab; toàn bộ SO/Sprite Swap/DevTools assembly vẫn thực hiện Phase 3.

## Global Constraints

- Không thực thi manifest xóa/di chuyển hoặc implementation trước khi được duyệt. Không commit, push, publish hoặc sửa dữ liệu save cũ.
- Giữ thay đổi có sẵn và tạo điểm khôi phục cho cả file chưa tracked. Chỉ xử lý đúng đường dẫn trong manifest; path mới xuất hiện hoặc đang được phiên khác sửa phải được đối chiếu trước.
- Mọi move/rename trong Assets đi kèm `.meta` và đối chiếu GUID/subasset. `git mv` chỉ stage cặp đường dẫn được di chuyển; không `git add -A` hoặc sửa index của thay đổi ngoài task.
- Không sửa YAML scene/prefab bằng tay; dùng Unity Editor/Pipeline API. Không thêm script Scene-Builder/Checks mới để thực hiện refactor.
- Không tạo `AgentScripts/`, `Verification/`, Web QA hoặc ảnh nghiệm thu tự động. Compile log/NUnit XML đặt tại `/private/tmp/nguthu-core-foundation/<run-id>/`. **Backup archive và manifest SHA-256 bắt buộc ở `~/dev/nguthu-backups/core-foundation-backup/`; tuyệt đối không dùng `/private/tmp/` cho backup.**
- Domain không Unity/input/rendering/file I/O; định nghĩa bất biến, runtime state riêng, clock/random được truyền vào. `Try...` bị từ chối phải có mã lý do và không làm thay đổi trạng thái.
- Runtime dùng tên chính thức; exception cho tên NUnit fixture trong `Tests/EditMode`. Không đổi asset bên thứ ba hoặc file trong `Recovery/` chỉ để áp quy tắc naming.
- Kết thúc mỗi Phase: Unity compile 0 lỗi từ lần chạy mới; chỉ chuyển Phase khi có bằng chứng. Báo warning riêng. Phase 2 còn phải đạt toàn bộ EditMode tests, không pass bằng cách bỏ/ignore fixture.

## Review Focus

- Asset giữ lại tham chiếu nhánh sắp xóa: kiểm dependency và missing reference ở bước P1.2/P1.5; compile không phát hiện hết mất Sprite/material.
- Modifier trùng nguồn sau đổi đồ/thu hồi/load: test replace/remove/restore và cộng hưởng ở P2.1/P2.4/P2.7.
- Tick chạm đúng mốc cooldown/hết buff hoặc nhiều tick trong một delta: test ranh giới ở P2.3, không dựa vào `Time.time`.
- Kho gần đầy hoặc thu hoạch hai lần cùng một node: test giao dịch nhận đủ hoặc không đổi gì ở P2.5.
- Save sai schema/thiếu definition/đang có cast hoặc projectile: test restore vào world mới và bảo toàn world hiện tại khi lỗi ở P2.7.

## 3. Phase 1 — manifest thao tác

Các đường dẫn rút gọn `Art/...`, `Scripts/...`, `Content/...`, `Editor/...` trong bảng này đều tính từ **`Assets/_Project/`**; đường dẫn được đánh dấu root là từ repository root. Mọi file/thư mục trong Assets bị xóa hoặc chuyển đều xử lý cả `.meta` tương ứng.

### P1.1. Chốt baseline và điểm khôi phục

- [x] Kiểm tra lại status/index, scene mở và scene dirty; chụp danh sách file/GUID/subasset của toàn bộ nhánh giữ/chuyển/xóa. Không ghi đè một scene chưa lưu.
- [x] Lưu archive ngoài checkout của các đường dẫn sẽ sửa/xóa, gồm master art, file untracked và `.meta`; lưu diff/index baseline và SHA-256. Kiểm tra đọc được archive và manifest trước bước xóa; không dựa vào Git HEAD để khôi phục file chưa commit.
- [x] Đối chiếu danh sách bên dưới với filesystem mới nhất; nếu khác baseline, bổ sung chênh lệch vào manifest trước khi thao tác. Không xóa `Recovery/`, `Library/`, `Temp/`, `UserSettings/`, save máy hoặc `WebBuild*` trong lượt này.

### P1.2. Di chuyển và cứu phụ thuộc trước khi xóa

| Nguồn hiện hành | Đích / thao tác | Ràng buộc |
| --- | --- | --- |
| `Art/HeroC2/` + `Art/HeroC2.meta` | `Art/Characters/HeroBase/` + `.meta` | Giữ 52 PNG, importer, pivot, sprite ID; folder cha mới được Unity tạo meta. |
| `Art/CartoonV2/` + `.meta` | `Art/Environment/Meadow/` + `.meta` | Giữ cả `Terrain`, PNG quy, đồ vật, material và GUID. Không dùng ảnh nền ghép làm map mới. |
| `Content/Characters/HeroC2/HeroC2Trial.unity` + `.meta` | Xóa theo P1.3 trước khi chuyển phần còn lại của folder | Không chuyển scene thử sang thư mục art. |
| `Content/Characters/HeroC2/` + `.meta`, sau khi bỏ scene | `Art/Characters/HeroBase/Animation/` + `.meta` | Giữ GUID thư mục; giữ `Idle.anim`, `Run.anim`. |
| `.../Animation/HeroC2.controller` + `.meta` | `.../Animation/HeroBase.controller` + `.meta` | Không đổi controller, state, parameter, clip binding. |
| `.../Animation/HeroC2.prefab` + `.meta` | `Content/Prefabs/Actors/HeroBase.prefab` + `.meta` | Đây là rig nguồn; `PlayerCultivator.prefab` được chuẩn hóa tại Phase 3. |
| `Scripts/Client/Preview/HeroC2Preview.cs` + `.meta` | `Scripts/Client/Actors/ModularEquipmentRenderer.cs` + `.meta` | Rename class, bỏ `OnGUI/showControls`; giữ các field `animator/visual/parts/outfit/hair/eyes`, xương và transform path. Bảo toàn component reference bằng GUID và `MovedFrom` khi đổi namespace/class; mở prefab bằng Unity để xác nhận. Chưa thay cách đổi layer bằng SO trước Phase 3. |
| `Scripts/Domain/Navigation/PreviewMovement.cs` + `.meta` | `Scripts/Domain/Navigation/MovementMotor.cs` + `.meta` | Phase 1 chỉ đổi tên class và call site còn sống; API/ownership hoàn chỉnh theo P2.2. |
| Class `MovementArena` trong `Navigation/WorldGeometry.cs` | Rename thành `WorldGeometry`; cập nhật call site còn sống | Giữ thuật toán trượt, không viết lại hình học trong bước dọn. |
| `Editor/MeadowTerrainPathEditor.cs` + `.meta` | `Scripts/Editor/Inspectors/MeadowTerrainPathEditor.cs` + `.meta` | Tạo `Scripts/Editor/LinhThu.Editor.asmdef`, Editor-only, tham chiếu tên assembly Client hiện tại `LinhThu.Prototype`; cập nhật thành `LinhThu.Client` ở Phase 3. |

`HeroBase.prefab` hiện không có reference material vào ba nhánh Hero cũ theo quét GUID tĩnh. Vẫn dùng `AssetDatabase.GetDependencies` để kiểm tra cả dependency Unity/subasset trước khi xóa. Các material `Resources/Art/{DaylightSprite,DaylightShadow}.mat` và shader **được giữ**; chuyển reference serialize/nơi lưu ở Phase 3.

**Dashboard trước khi xóa host:** sửa `Scripts/Client/Preview/OwnerDashboard.cs` thành nhận `[SerializeField] ModularMapLayout map` và phát `event Action<bool> OpenChanged`, giữ `bool IsOpen`. Thay các lần đọc `host.map` bằng `map`; bỏ `GetComponent<ArtPreviewHost>` và gọi `host.SetOwnerDashboardOpen`. Khi disable phát trạng thái đóng nếu cần. Không tạo host giả hoặc catch lỗi để compile qua. `OwnerCatalog.cs` được giữ; không tạo dashboard trong scene ở Phase 1. Loại thông báo catalogue “tự cập nhật khi Play/build” vì builder bị bỏ; Phase 3 thay bằng dữ liệu serialize được cung cấp rõ ràng.

### P1.3. Danh sách xóa

| Nhóm | Đường dẫn chính xác / phạm vi |
| --- | --- |
| Root tooling/output | Toàn bộ root `AgentScripts/`, `Verification/`, `Web/`; root `serve.py`, `serve_adventure.py`. `AgentScripts` hiện gồm `MainMapMethod2.cs`, `GroupPondAndPath.cs`, `HeroDirectionBuilder.cs`, `HighViewPrepare.cs`, `CartoonTrialBuild.cs`, `ModularMapChecks.cs`, `PaintedHeroBaker.cs` và file phụ trong thư mục. |
| ArtDirection | Root `ArtDirection/CartoonV1/`, `CartoonV2/`, `CharacterProduction/`, `HeroEightDirections/`, `HeroModular2D/`, `HeroPainted2D/`, `HeroProduction2D/`, `References/`, `README.md`. Chỉ giữ và sửa link/nội dung `ArtDirection/STYLE_GUIDE.md`; không giữ lén master ở thư mục con. |
| Root rỗng | Root `ArtTrial`, `DemoArt`, `Editor`, `Scenes`, `Scripts` và `.meta`, **chỉ nếu có và thực sự rỗng**. Hiện không tồn tại. Nếu xuất hiện nội dung mới: báo chênh lệch, không xóa cả cây theo tên. |
| Ba nhánh art cũ | `Art/HeroModular2D/`, `Art/HeroPainted2D/`, `Art/HeroProduction2D/` và ba folder `.meta`. PSD bị cấm nằm trong nhánh Production và được xóa cùng nhánh sau backup. |
| Definition/prefab nhân vật cũ | `Content/Characters/HeroProduction2D/`, `Content/Characters/Hero8Directions.asset`, `Content/Characters/ModularHeroFront.asset`, kèm `.meta`. Xóa `Content/Characters/` và `.meta` chỉ khi đã chuyển rig C2 và không còn nội dung. |
| Scene cũ | `Content/Scenes/ArtPreview.unity`, `HeroModularTrial.unity`, `HeroProductionTrial.unity`, `CartoonArtTrial.unity`; `Content/Characters/HeroC2/HeroC2Trial.unity`; kèm `.meta`. Gỡ scene khỏi Build Settings qua Editor API. |
| Script nhân vật cũ | `Scripts/Client/Characters/DirectionalHeroDefinition.cs`, `DirectionalHeroView.cs`, `ModularHeroDefinition.cs`, `ModularHeroRig.cs`, `ModularHeroTrial.cs`, `AuthoredHeroView.cs`, `AuthoredHeroPreview.cs`; kèm `.meta`. Xóa folder khi rỗng. |
| Controller/HUD thử | `Scripts/Client/CartoonTrialDirector.cs`, `Scripts/Client/Preview/ArtPreviewHost.cs`, `Scripts/Client/Preview/ArtPreviewHud.cs`; kèm `.meta`, sau khi bỏ dependency dashboard. `HeroC2Preview.cs` được **chuyển/đổi tên**, không xóa mất dữ liệu rig. |
| Editor helpers | `Editor/ArtPresentationBuilder.cs`, `ArtPreviewBuilder.cs`, `ArtPreviewChecks.cs`, `CartoonArtBuilder.cs`, `HeroC2Builder.cs`, `HeroC2RebuildCheck.cs`, `HeroProductionBuilder.cs`, `HeroProductionChecks.cs`, `ModularHeroBuilder.cs`, `ModularMapBuilder.cs`, `OwnerCatalogBuilder.cs`; kèm `.meta`. `HeroC2RebuildCheck.cs` là số ít nên wildcard `*Checks.cs` không bắt được; vẫn xóa theo yêu cầu chỉ giữ inspector. |

Sau di chuyển inspector và xóa helpers, xóa `Assets/_Project/Editor/` cùng `.meta` nếu rỗng. **Không xóa root/Assets bằng wildcard chung.** Không giữ script bị xóa bằng `#if false`, đổi đuôi hoặc chuyển vào assembly không compile.

**Điều chỉnh đã duyệt:** dùng `AssetDatabase.GetDependencies` từ HeroBase/MeadowMainMap/các prop; nếu không phụ thuộc `CartoonCharacterRig.cs` hoặc `CartoonUiRig.cs` thì xóa script tương ứng ngay Phase 1; nếu còn phụ thuộc ghi rõ để xóa dứt điểm Phase 3.

**Giữ qua Phase 1 (hai script Cartoon áp điều kiện trên):** `MeadowMainMap.prefab`, các prefab prop, `MeadowMap.prefab` nếu còn dùng để đối chiếu asset, toàn bộ shaders/font/material còn dùng, `CartoonCharacterRig`, các component `GardenAmbient*`, `CartoonUiRig`, `Daylight*`, `SunShadow*`, `Meadow*`, `ModularMap*`, `ObstacleAuthoring`, `OwnerDashboard`, `OwnerCatalog`. Những component này chưa được thay bằng `ActorView/BeastView` ở Phase 1; không tự coi chúng là Trial chỉ vì từng được scene thử sử dụng.

### P1.4. Đồng bộ tài liệu và quy chuẩn

- [x] Sửa `GAME_DESIGN.md` phần ưu tiên hiện hành: chỉ thị mới, phạm vi foundation và thông số chưa chốt; giữ lịch sử, không chép fixture thành cân bằng chính thức.
- [x] Sửa `Architecture/CODE_ARCHITECTURE.md`, `Architecture/REPOSITORY_STORAGE.md`, `Architecture/OWNER_DASHBOARD.md`, root `README.md`, `ROLLBACK.md` và `ArtDirection/STYLE_GUIDE.md`: bỏ hướng dẫn builder/Web/Verification đang hoạt động; mô tả trạng thái chuyển tiếp chưa có scene; thay link hỏng tới nguồn bị xóa bằng ghi chú lịch sử hoặc quy chuẩn còn trong file.
- [x] Trong STYLE_GUIDE giữ mô tả thị giác; một góc 3/4 + flip + Sprite Swap thay quy định nhiều hướng. Ảnh `References/soul-strike-high-view.png` sẽ bị xóa theo manifest, nên không để link bắt buộc tới file đã mất hoặc tự tải ảnh thay thế.
- [x] Sửa `.gitignore` theo quy chuẩn mới để output/backup không quay lại Git; không di trú Git LFS, rewrite history hoặc xóa `WebBuild*` ngoài yêu cầu. Báo cáo ngắn đặt `Architecture/`, không tạo hệ thống QA mới.

### P1.5. Cửa kiểm chứng cuối Phase 1

- [x] Unity import/recompile thành công, **0 compile errors**, không dùng kết quả từ log cũ.
- [x] Qua Unity API, mở/kiểm tra `HeroBase.prefab`, controller/Idle/Run và `MeadowMainMap.prefab`: không missing script, sprite, material hoặc clip binding; GUID trước/sau trùng với manifest. Không rebuild rig hoặc map.
- [x] Các đường dẫn xóa không còn; `ArtDirection` chỉ còn STYLE_GUIDE; không còn `*Builder.cs`, `*Checks.cs`, helper RebuildCheck trong source dự án; không còn tên runtime bị cấm thuộc phạm vi đã chuyển.
- [x] Build Settings không tham chiếu scene đã xóa; không tuyên bố có scene chơi trước Phase 4. Domain/Client/Editor đều được compile, không loại assembly khỏi compilation để làm xanh.
- [x] Review diff và status so với baseline; log lỗi và bản khôi phục nằm ngoài checkout. Báo manifest thực tế và bằng chứng compile rồi mới chuyển Phase 2.

**Rollback Phase 1:** dừng thao tác import, khôi phục chỉ các path trong manifest từ bản khôi phục trước Phase; restore file + meta + nội dung Build Settings đã lưu; import/recompile lại. Không `git reset --hard`, không ghi đè thay đổi ngoài task hoặc xóa file mới của phiên khác. Nếu chưa thể kết nối Editor/Pipeline để sửa/kiểm prefab, dừng trước bước xóa phụ thuộc; không chuyển sang sửa YAML tay.

## 4. Phase 2 — cấu trúc và dependency

Tất cả path ở mục 4–6 tính từ `Assets/_Project/Scripts/`. Một type công khai chính/một trách nhiệm mỗi file; enum nhỏ/DTO liên quan có thể cùng file. Tạo `.meta` bằng Unity import, không tự phát GUID trùng.

| Task | File/thành phần chính | Phụ thuộc |
| --- | --- | --- |
| P2.1 Core, Stats, Actors | `Core/{EntityId,EntityIdAllocator,SimulationClock,DomainEventBus,DomainResult,SourceIdAllocator,IRandomSource,SeededRandomSource,IDomainContent,DomainEvents}.cs`; `Stats/{StatId,StatModifier,StatSheet,VitalResourcePool}.cs`; `Actors/{Transform2D,ActorEntity,ActorSpec,ActorFaction,ActorActionState}.cs` | C# BCL; actor liên kết container ability/status được hoàn chỉnh ở P2.3. Không để implementation rỗng hoặc throw thay chức năng ở cuối Phase. |
| P2.2 Navigation | `Navigation/{WorldGeometry,MovementMotor}.cs`; tách `WorldPoint`/`WorldObstacle` nếu cần để rõ ownership, giữ thuật toán | Core/Actors |
| P2.3 Combat & Status | `Combat/{ElementType,ElementInteractionTable,CombatRules,DeliveryShape,TechniqueSpec,AbilityBook,AbilityEffectSpec,IAbilityEffect,AbilityEffectRegistry,CombatResolver,StatusEffectSpec,StatusEffectContainer,ProjectileState}.cs`; các handler trong `Combat/Effects/` và selector trong `Combat/Targeting/` | Stats/Actors/Navigation/Core |
| P2.4 Cultivation & Beasts | `Cultivation/{RealmSpec,CultivationProgress,CultivationService}.cs`; `Beasts/{BeastSpeciesSpec,BeastInstance,BeastRoster,BeastAiController,BondStatLink}.cs` | Stats/Actors/Combat |
| P2.5 Inventory & Nodes | `Inventory/{ItemSpec,ItemCategory,ItemStack,InventoryContainer,EquipmentLoadout,ItemUseService}.cs`; `World/{WorldNodeSpec,WorldNodeEntity,WorldInteractionService}.cs` | Stats/Combat/Cultivation/Beasts |
| P2.6 World orchestration | `World/{WorldFactory,WorldState,PlayerState,WorldCommand,WorldCommandProcessor,WorldTick}.cs` | Các hệ trên, không Client |
| P2.7 Persistence | `Persistence/{GameSaveDto,GameSaveMapper,SaveSerializer,SaveValidation,SaveMigration}.cs` | DTO/specs và Newtonsoft JSON thuần .NET |
| Tests | `Tests/EditMode/LinhThu.Tests.EditMode.asmdef`; 5 fixture bắt buộc + `CoreAndActorTests.cs`, `CultivationTests.cs`, `WorldStateTests.cs` | Domain + NUnit; không prefab/scene/Resources |

Assembly Domain giữ `noEngineReferences: true`. Serializer dùng `Newtonsoft.Json.dll` từ package hiện có, khai báo precompiled reference rõ ràng nếu cần; không dùng `JsonUtility` trong Domain. Test Framework `1.8.0` đang là dependency gián tiếp: đưa thành dependency trực tiếp cùng phiên bản trong `Packages/manifest.json` để suite không mất khi thay package khác; cập nhật lock qua Unity. Test asmdef Editor-only, `references: ["LinhThu.Domain"]`, `optionalUnityReferences: ["TestAssemblies"]`, không nhập test vào Player.

Luồng đích:

```text
Client Input → WorldCommand → WorldCommandProcessor (validate, commit)
                                  ↓
WorldState.Tick → cooldown/status/cast/projectile → Beast AI → movement/nodes
                                  ↓
                           typed domain events
                                  ↓
                     Client view / HUD / audio
```

Tầng Domain không tham chiếu Client/DevTools. World chạy một luồng; không tuyên bố thread-safe/multiplayer. Domain command kiểm ownership và entity/definition tại nơi hành động; đây là validation mô phỏng local, không thay authentication server.

## 5. Thiết kế API của bảy trụ cột

Các khối dưới là **chữ ký thiết kế**, không phải source đã viết. DTO/spec có constructor validate và collection chỉ đọc; kết quả `DomainResult`/`DomainResult<T>` chứa `Succeeded`, `ErrorCode`, `Message` và `Value` khi thành công. `ErrorCode` có giá trị cụ thể cho `InvalidInput`, `NotFound`, `WrongOwner`, `InvalidState`, `InsufficientResource`, `OnCooldown`, `OutOfRange`, `Blocked`, `CapacityExceeded`, `MissingDefinition`, `InvalidSave`, `UnsupportedSchema`, `NoEffect`.

### Trụ 1 — Stats

`Stats/StatId.cs`: enum đủ 22 ID yêu cầu, explicit numeric value ổn định; serialize tên, không phụ thuộc ordinal:

```csharp
enum StatId {
    MaxHp = 1, HpRegen = 2, MaxQi = 3, QiRegen = 4, MaxShield = 5, Lifespan = 6,
    Attack = 7, Defense = 8, MoveSpeed = 9, AttackSpeed = 10, CritRate = 11, CritDamage = 12,
    CooldownReduction = 13, PickupRadius = 14,
    MetalAffinity = 15, WoodAffinity = 16, WaterAffinity = 17, FireAffinity = 18, EarthAffinity = 19,
    Comprehension = 20, BeastCommandCap = 21, BeastStatLinkRatio = 22
}
enum ModifierType { FlatAdd, PercentAdd, PercentMult }
readonly struct StatModifier {
    StatId StatId { get; } ModifierType Type { get; }
    float Value { get; } int SourceId { get; }
    StatModifier(StatId statId, ModifierType type, float value, int sourceId);
}
sealed class StatSheet {
    IReadOnlyDictionary<StatId, float> BaseValues { get; }
    int Revision { get; }
    StatSheet(IReadOnlyDictionary<StatId, float> baseValues);
    float Get(StatId statId);
    void SetBase(StatId statId, float value);
    void AddModifier(StatModifier modifier);
    void SetModifiersFromSource(int sourceId, IReadOnlyList<StatModifier> modifiers);
    int RemoveAllModifiersFromSource(int sourceId);
    IReadOnlyList<StatModifier> GetModifiers();
}
enum VitalType { Hp, Qi, Shield }
sealed class VitalResourcePool {
    float CurrentHp { get; } float CurrentQi { get; } float CurrentShield { get; }
    bool IsAlive { get; }
    VitalResourcePool(StatSheet stats, float hp, float qi, float shield);
    bool TrySpend(VitalType vital, float amount);
    float Restore(VitalType vital, float amount);
    DamageApplied ApplyDamage(float amount); // shield trước, HP sau
    void ClampToLimits();
    void Regenerate(float deltaTime);
}
```

- `Get` dùng đúng công thức yêu cầu, cache theo stat và invalidate khi base/modifier đổi. Stat chưa có base = 0. Reject NaN/Infinity; không clamp âm tùy tiện trong phép tính tổng quát. Consumer áp giới hạn phù hợp: max HP tối thiểu 1; Qi/shield/speed tối thiểu 0; rate trong [0,1]; crit multiplier tối thiểu 1; cooldown reduction có cap từ `CombatRules`.
- `SetModifiersFromSource` validate toàn bộ rồi replace một lần; không append cùng nguồn mỗi tick. `Remove...` gọi lại là no-op. `SourceIdAllocator` cấp ID dương độc nhất theo instance nguồn, không dùng `GetHashCode` hoặc DefinitionId làm ID chung cho mọi buff cùng loại.
- Vitals clamp khi giới hạn giảm; tăng MaxHp không tự hồi máu. `DamageApplied` chứa `ShieldLost`, `HpLost`, `DiedNow`; death chỉ xảy ra một lần cho mỗi chuyển sống → chết. Regen thông thường không hồi sinh actor đã chết.
- Fixture: base 100, flat +20, PercentAdd +0.10/+0.20, PercentMult +0.10/−0.10 → **154.44**; gỡ flat source → **128.7**. Test cả trừ, nguồn không tồn tại và đổi base sau khi đã đọc cache.

### Trụ 2 — Actors, cùng một gốc thực thể

```csharp
readonly struct EntityId { long Value { get; } EntityId(long value); }
enum ActorFaction { Player, PlayerBeast, WildBeast, HostileMonster, Neutral }
enum ActorActionState { Idle, Moving, Casting, Channeling, Staggered, Dead }
sealed class Transform2D {
    WorldPoint Position { get; } WorldPoint FacingDirection { get; }
    float CollisionRadius { get; }
    Transform2D(WorldPoint position, WorldPoint facingDirection, float collisionRadius);
}
sealed class ActorSpec {
    string DefinitionId { get; } ElementType PrimaryElement { get; }
    IReadOnlyDictionary<StatId, float> BaseStats { get; }
    IReadOnlyList<string> AbilityIds { get; }
    float CollisionRadius { get; }
}
sealed class ActorEntity {
    EntityId Id { get; } string DefinitionId { get; } ActorFaction Faction { get; }
    Transform2D Transform { get; } StatSheet Stats { get; }
    VitalResourcePool Vitals { get; } ActorActionState State { get; }
    StatusEffectContainer StatusEffects { get; } AbilityBook Abilities { get; }
    EntityId? OwnerId { get; }
    ActorEntity(EntityId id, ActorSpec spec, ActorFaction faction,
                WorldPoint spawn, EntityId? ownerId, SourceIdAllocator sources);
    bool CanAct { get; }
    DomainResult SetStaggered(bool staggered); // hủy cast/channel; Domain điều khiển bắt đầu/kết thúc stagger
}
```

Vị trí, State và HP không có public setter cho UI; Domain service có đường mutate nội bộ. Không tạo `PlayerHp`, `BeastHp` hoặc hệ stat riêng cho quái. Khóa quan hệ thù địch bằng `FactionRelations` (trong `ActorFaction.cs`) được truyền từ cấu hình; không mặc định mọi `WildBeast/Neutral` đều thù địch. `EntityId` là ID instance, `DefinitionId` là ID loại nội dung; allocator không tái dùng ID trong cùng world.

**Navigation hỗ trợ các trụ**, `Navigation/WorldGeometry.cs` và `MovementMotor.cs`:

```csharp
sealed class WorldGeometry {
    WorldGeometry(WorldPoint halfSize, IEnumerable<WorldObstacle> obstacles);
    bool CanStand(WorldPoint point, float radius);
    bool ClearLine(WorldPoint from, WorldPoint to);
    WorldPoint Move(WorldPoint from, WorldPoint delta, float radius, bool slideAround = false);
    bool TryFindStandableNear(WorldPoint center, float radius, float searchRadius, out WorldPoint point);
}
enum MovementMode { Walk, Displacement }
readonly struct MovementResult { WorldPoint Position { get; } WorldPoint Displacement { get; } }
sealed class MovementMotor {
    MovementMotor(WorldGeometry geometry);
    MovementResult Move(Transform2D transform, WorldPoint direction,
                        float speed, float deltaTime, MovementMode mode);
    MovementResult Displace(Transform2D transform, WorldPoint delta);
}
```

Một source of truth vị trí trên `Transform2D`; motor ghi kết quả đã resolve, không giữ bản position thứ hai. Chuẩn hóa input chéo, input 0 giữ facing. Walk dùng trượt hiện tại; dash/knockback dùng swept/substep collision, không tự men vòng và không xuyên vật cản. `TryFindStandableNear` tìm hữu hạn, thứ tự ổn định, có thể thất bại; không teleport vào collider. Giữ hỗ trợ circle/box/convex polygon và map bounds; reject radius/coordinates/delta không hợp lệ.

### Trụ 3 — Cultivation

```csharp
enum CultivationSubStage { Early, Middle, Late, Perfection }
sealed class CultivationProgress {
    string CurrentRealmId { get; } CultivationSubStage SubStage { get; }
    long CurrentCultivationExp { get; }
    CultivationProgress(RealmSpec realm, long experience);
    DomainResult AddExperience(long amount);
}
sealed class RealmSpec {
    string DefinitionId { get; } string NextRealmId { get; }
    IReadOnlyList<long> SubStageThresholds { get; }
    long BreakthroughCost { get; }
    IReadOnlyList<StatModifierSpec> Modifiers { get; }
}
sealed class CultivationService {
    CultivationService(IDomainContent content, DomainEventBus events);
    DomainResult TryBreakthrough(ActorEntity actor, CultivationProgress progress, int realmSourceId);
}
```

`StatModifierSpec` nằm trong `StatModifier.cs`, gồm stat/type/value, chưa có SourceId; runtime materialize bằng source của cảnh giới. Progress giữ RealmSpec bất biến để `AddExperience` cập nhật substage theo threshold mà không cần Unity hoặc registry static. Ngưỡng substage/chi phí/realm kế là dữ liệu. Đề xuất: chỉ đột phá khi Viên mãn, đủ cost, còn sống và realm kế hợp lệ; trừ cost, giữ EXP dư, tính substage theo ngưỡng realm kế và thay toàn bộ modifier realm một lần. Không tự thêm random thất bại hoặc mất cảnh giới. Validate realm kế/modifier trước khi trừ EXP; terminal realm trả lỗi không đổi gì. Event `RealmBreakthroughEvent` chỉ phát sau thành công.

### Trụ 4 — Combat, delivery và status lắp ghép

```csharp
enum ElementType { Physical, Metal, Wood, Water, Fire, Earth } // Physical tương ứng None trong chỉ thị
enum DeliveryShape { CircleAoE, ConeSector, LineRay, Projectile, Self }
sealed class TechniqueSpec {
    string DefinitionId { get; } DeliverySpec Delivery { get; }
    float QiCost { get; } float Cooldown { get; }
    float CastDuration { get; } float ChannelDuration { get; } float ChannelInterval { get; }
    IReadOnlyList<AbilityEffectSpec> Effects { get; }
}
sealed class AbilityBook {
    DomainResult Learn(string techniqueId, IDomainContent content);
    bool Knows(string techniqueId);
    float GetCooldownRemaining(string techniqueId);
    void Tick(float deltaTime);
}
abstract class AbilityEffectSpec { } // các spec bất biến, không có MonoBehaviour
interface IAbilityEffect {
    Type SpecType { get; }
    DomainResult Validate(AbilityEffectSpec spec, AbilityExecutionContext context);
    void Apply(AbilityEffectSpec spec, AbilityExecutionContext context);
}
sealed class AbilityEffectRegistry {
    void Register(IAbilityEffect handler);
    IAbilityEffect Resolve(Type specType);
}
sealed class CombatResolver {
    CombatResolver(IDomainContent content, CombatRules rules, AbilityEffectRegistry effects,
                   TargetSelectorRegistry targeting, IRandomSource random, DomainEventBus events);
    DomainResult TryCast(WorldState world, EntityId casterId, string techniqueId, AimRequest aim);
    void Tick(WorldState world, float deltaTime);
    DamageResult CalculateDamage(ActorEntity caster, ActorEntity target, DamageEffectSpec spec);
}
enum StatusStackPolicy { RefreshDuration, AddStacksRefreshDuration, IndependentInstances }
sealed class StatusEffectSpec {
    string DefinitionId { get; } float Duration { get; } float TickInterval { get; }
    int MaxStacks { get; } StatusStackPolicy StackPolicy { get; } bool ClearOnDeath { get; }
    IReadOnlyList<StatModifierSpec> Modifiers { get; }
    IReadOnlyList<AbilityEffectSpec> TickEffects { get; }
}
sealed class StatusEffectContainer {
    IReadOnlyList<StatusInstance> Active { get; }
    StatusEffectContainer(ActorEntity owner, SourceIdAllocator sources);
    DomainResult Apply(StatusEffectSpec spec, EntityId casterId, EffectSourceSnapshot source);
    bool Remove(int sourceId);
    void Tick(float deltaTime, WorldState world);
    void Clear();
}
```

Các kiểu phụ trong đúng file của hệ: `DeliverySpec` = shape/range/radius/angle/projectileSpeed/maxTargets/targetFilter; `AimRequest` = hướng + target ID tùy chọn; `AbilityExecutionContext` = world/caster/target/hướng/source snapshot; `DamageResult` = lượng tính được + cờ crit, không tự trừ HP. `StatusInstance` dùng `int SourceId` độc nhất làm ID instance, có DefinitionId, caster, stack, remaining duration, time-to-next-tick; `EffectSourceSnapshot` lưu các stat scaling cần dùng tại lúc áp status, không giữ reference actor đã bị thu hồi. `ProjectileState` lưu caster/source, vị trí/hướng, thời hạn, tốc độ và tập target đã trúng.

**Bốn cặp spec/handler ở `Combat/Effects/`:**

- `DamageEffectSpec(StatId scalingStat, float coefficient, float flatDamage, ElementType element)` / `DamageEffect`.
- `ApplyStatusEffectSpec(string statusDefinitionId)` / `ApplyStatusEffect`.
- `DisplacementEffectSpec(float distance, DisplacementTarget target)` / `DisplacementEffect`; target `Caster` hoặc `Target`, luôn đi qua MovementMotor.
- `RestoreVitalEffectSpec(VitalType vital, float amount)` / `RestoreVitalEffect`; hỗ trợ shield qua VitalType, ngoài HP/Qi yêu cầu.

`Combat/Targeting/ITargetSelector.cs` có `DeliveryShape Shape` và `IReadOnlyList<EntityId> Select(WorldState world, ActorEntity caster, DeliverySpec delivery, AimRequest aim)`. `TargetSelectorRegistry.cs` cung cấp `void Register(ITargetSelector selector)` và `ITargetSelector Resolve(DeliveryShape shape)`. Có selector cho đủ năm shape, đăng ký theo shape; lọc faction/range/line-of-sight, target duy nhất, thứ tự khoảng cách rồi EntityId. Projectile resolve va chạm theo đoạn di chuyển từng tick, không chỉ kiểm điểm cuối. Phase 2 đăng ký đủ handler/selector trong WorldFactory dùng cho world/tests; Phase 3 gọi cùng factory tại composition, không chỉ có handlers trong test. Thêm definition dùng các effect này không sửa resolver. `CombatRules` giữ `DefenseScale`, `OffenseScale`, `ResistanceScale`, `CooldownReductionCap` và `ElementInteractionTable`; bảng cung cấp `float Get(ElementType attack, ElementType defense)` và ánh xạ element → Affinity stat, reject hệ số âm/không hữu hạn.

**Công thức đề xuất, mọi hệ số ở `CombatRules`/`ElementInteractionTable`:**

```text
raw        = max(0, flatDamage + caster[scalingStat] × coefficient)
mitigated  = max(0, raw − max(0, target[Defense]) × defenseScale)
element    = interaction[attackElement, targetPrimaryElement]
             × max(0, 1 + caster[affinityOfElement] × offenseScale)
             / (1 + max(0, target[affinityOfElement]) × resistanceScale)
damage     = mitigated × element × (critical ? max(1, caster[CritDamage]) : 1)
```

Physical có hệ số nguyên tố 1. Tương sinh/tương khắc là bảng có hướng, không mã hóa giá trị theo tên skill; target element là dữ liệu ActorSpec. Đã chốt dùng chung năm Affinity cho công/kháng, giữ nguyên 22 StatId; không thêm năm ID Resistance. Crit lấy từ random được inject, không `UnityEngine.Random`. Ví dụ test: công 40, hệ số 1.5, thủ 10, bảng 1.2, công affinity .1, kháng .2, ba scale = 1, không crit → **55** damage.

**Chính sách cast/status đề xuất:**

- Kiểm alive/state/known ability/target/chi phí/cooldown/definition trước khi accept. Reject không tiêu Qi/cooldown/random hoặc phát executed event. Accept trừ Qi và bắt cooldown đúng một lần; mục tiêu rời vùng trong cast là đánh hụt, không tự hoàn phí.
- Cooldown lấy theo ID công pháp và modifier lúc bắt đầu; không giảm hai lần vì vừa tick world vừa tick view. Channel có số nhịp hữu hạn; chết/stagger hủy cast/channel theo policy, không phát lại hiệu ứng đã chạy.
- Refresh giữ stack, reset duration; AddStacks tăng tới MaxStacks và reset duration; Independent tạo instance/source riêng. Hai policy gộp dùng khóa `(statusDefinitionId, casterId)`, không để caster khác ghi đè nguồn nhau. Hết hạn/gỡ xóa đúng modifier/source.
- Stack nhân giá trị FlatAdd/PercentAdd và hiệu lực periodic theo StackCount; PercentMult lặp đúng số stack để giữ phép tích. `TickInterval = 0` nghĩa không periodic, chỉ hợp lệ khi TickEffects rỗng; interval dương phải hữu hạn. Duration dương, MaxStacks ít nhất 1; modifier/status định nghĩa lỗi bị từ chối trước khi gắn nguồn.
- Mốc periodic tick được xử lý khi `tickTime <= expiry`; delta đi qua nhiều mốc thì xử lý đủ trong khoảng hiệu lực rồi expire. Không tick thêm sau hết hạn; test tách delta cho kết quả giống delta gộp.
- Buff shield ban đầu dùng modifier MaxShield + hiệu ứng hồi Shield, gỡ buff clamp shield về giới hạn; chưa phân chia nhiều lớp shield theo nguồn hấp thụ. Buff/debuff chết được clear theo spec; không đặt vô hạn bằng duration âm.

### Trụ 5 — Beasts và cộng hưởng

```csharp
enum BeastState { InEgg, Resting, SummonedActive, AssignedToField }
sealed class BeastInstance {
    EntityId Id { get; } EntityId OwnerId { get; } string DefinitionId { get; }
    string QualityId { get; } string BloodlineId { get; } float Bond { get; }
    BeastState State { get; } EntityId? ActorId { get; } EntityId? AssignedFieldId { get; }
}
sealed class BeastRoster {
    IReadOnlyList<BeastInstance> All { get; }
    DomainResult Add(BeastInstance beast);
    DomainResult TryHatch(EntityId beastId, WorldState world);
    DomainResult TrySummon(EntityId beastId, WorldState world);
    DomainResult TryRecall(EntityId beastId, WorldState world);
    DomainResult TryAssignToField(EntityId beastId, EntityId fieldId, WorldState world);
    DomainResult TryUnassignFromField(EntityId beastId, WorldState world);
}
enum BeastAiState { FollowOwner, CombatAssist }
sealed class BeastAiController {
    BeastAiState State { get; }
    BeastAiController(EntityId beastActorId, EntityId ownerActorId, BeastAiSpec spec);
    void Tick(WorldState world, float deltaTime);
}
sealed class BondStatLink {
    void Refresh(ActorEntity owner, ActorEntity beast, BeastSpeciesSpec spec, int sourceId);
    void Remove(ActorEntity owner, int sourceId);
}
```

`BeastSpeciesSpec` chứa ActorDefinitionId, dữ liệu phẩm chất/huyết mạch, BondModifiers và `BeastAiSpec` (follow distance, leash, search radius, catch-up multiplier, assist technique IDs). Đề xuất cộng hưởng: lấy **chỉ các stat được species cấu hình**, nhân giá trị thú với `max(0, owner[BeastStatLinkRatio])` và hệ số bond cấu hình. `BeastStatLinkRatio/BeastCommandCap` không là đầu ra link để tránh vòng phụ thuộc. Đổi revision stat thú hoặc ratio chủ thì replace nguồn cũ, không append; không lấy chỉ số chủ đã được link để cộng hưởng ngược sang thú.

Summon chỉ từ Resting, đúng chủ, dưới `floor(max(0, BeastCommandCap))`, tìm được spawn hợp lệ; tạo đúng một actor + nguồn modifier hoặc không thay gì. Triệu hồi lại trả InvalidState, không nhân actor/buff. Đề xuất chết/recall: remove active actor/link, đưa thú về Resting và clear trạng thái tạm; hồi sinh/hồi đầy theo species policy trước lần summon sau, không giả lập progress vĩnh viễn bằng reset tùy tiện. Nếu cap giảm dưới số active: thu hồi thú xuất chiến sau cùng đến khi hợp lệ, có event từng lần.

Follow dùng MovementMotor; quá leash tìm vị trí cạnh chủ có thể đứng rồi dịch chuyển, tìm không được thì tăng tốc có giới hạn, không xuyên collider. Assist ưu tiên kẻ vừa gây damage lên chủ còn hợp lệ; nếu không, lấy hostile gần chủ trong bán kính/line-of-sight; mục tiêu chết/mất/ra leash thì trở lại Follow. AI gọi cùng `TryCast` như người chơi, không bypass Qi/cooldown. AssignedToField không đồng thời có active actor hoặc bond combat; trường hợp field/owner mất phải trả lỗi/dọn quan hệ rõ ràng.

### Trụ 6 — Inventory, World Nodes và WorldState

```csharp
enum ItemCategory { SpiritStone, Herb, Ore, Pill, Artifact, BeastEgg, TechniqueScroll }
readonly struct ItemStack {
    string DefinitionId { get; } int Count { get; }
    ItemStack(string definitionId, int count);
}
sealed class InventoryContainer {
    int Capacity { get; } IReadOnlyList<ItemStack> Slots { get; }
    InventoryContainer(int capacity, IDomainContent content);
    DomainResult TryAdd(ItemStack stack);
    DomainResult TryAddAll(IReadOnlyList<ItemStack> stacks);
    DomainResult TryRemove(string definitionId, int count);
    int Count(string definitionId);
}
sealed class EquipmentLoadout {
    DomainResult TryEquip(string slotId, string itemId, InventoryContainer inventory, ActorEntity owner);
    DomainResult TryUnequip(string slotId, InventoryContainer inventory, ActorEntity owner);
}
sealed class ItemUseService {
    DomainResult TryUse(WorldState world, EntityId ownerId, string itemId);
}
enum WorldNodeKind { HerbNode, OreNode, SpiritEggNode, SpiritFieldPlot, TeleportPortal }
sealed class WorldNodeEntity {
    EntityId Id { get; } string DefinitionId { get; } WorldPoint Position { get; }
    float GrowthTimer { get; } bool IsReady { get; } EntityId? AssignedBeastId { get; }
    void Tick(float deltaTime);
}
sealed class WorldInteractionService {
    DomainResult TryHarvest(WorldState world, EntityId actorId, EntityId nodeId);
    DomainResult TryPlant(WorldState world, EntityId actorId, EntityId fieldId, string cropId);
    DomainResult TryUsePortal(WorldState world, EntityId actorId, EntityId portalId);
}
sealed class WorldState {
    SimulationClock Clock { get; } DomainEventBus Events { get; }
    PlayerState Player { get; } WorldGeometry Geometry { get; }
    IReadOnlyDictionary<EntityId, ActorEntity> Actors { get; }
    IReadOnlyDictionary<EntityId, WorldNodeEntity> Nodes { get; }
    WorldState(IDomainContent content, WorldGeometry geometry, IRandomSource random,
               DomainEventBus events, WorldInitialState initialState,
               AbilityEffectRegistry effects, TargetSelectorRegistry targeting);
    DomainResult Execute(WorldCommand command);
    void Tick(float deltaTime);
    bool TryGetActor(EntityId id, out ActorEntity actor);
}
```

`ItemSpec` = ID/category/maxStack/usable effects/equipment slot/modifiers/technique hoặc beast definition khi cần. `WorldNodeSpec` = ID/kind/interaction radius/growth/respawn/reward/crop/portal destination. `PlayerState` = ActorId, Inventory, Equipment, Cultivation, BeastRoster. `WorldInitialState` gồm spawn/ActorSpecId, inventory capacity, initial nodes và realm/beasts (`BeastSeed` gồm species/quality/bloodline/bond, trạng thái InEgg hoặc Resting); không hardcode Hero/Huyền Quy trong WorldState. `WorldCommand` là gốc các command typed trong `WorldCommand.cs`: Move, Cast, Harvest, Plant, Portal, Summon, Recall, AssignField, UseItem, Equip, Breakthrough. Dispatcher gọi service tương ứng; không switch theo tên item/technique.

`MoveCommand` chỉ cập nhật hướng muốn đi; position thay một lần trong bước movement của Tick. `CastCommand` validate/accept một lần, resolver thực thi theo cast timer; không để command và Tick cùng gây damage. Node/slot/equipment definition validate đầy đủ trước khi world hoạt động.

`WorldFactory.Create(IDomainContent content, WorldGeometry geometry, IRandomSource random, DomainEventBus events, WorldInitialState initialState) : WorldState` tạo bộ registry mặc định đầy đủ. Registry là input constructor WorldState nên module bổ sung có thể đăng ký ở composition khác mà không sửa resolver hoặc WorldState. Mapper restore dùng cùng bộ handler của factory; custom registry về sau phải được truyền thống nhất vào cả tạo mới và restore.

Giao dịch là trong RAM, một luồng: validate + dự tính kết quả trên bản sao nhỏ → commit toàn bộ → queue event. Kho đầy, số lượng âm/tràn int, node non/đã thu, out-of-range/blocked hoặc sai chủ không đổi kho/node. `TryAddAll` không nhận một phần rồi mất phần còn lại. Nhấn hai lần: lần đầu commit, lần sau thấy node không ready. Artifact swap phải nhận lại đồ cũ hoặc từ chối toàn thao tác; consumable không có hiệu lực/scroll đã học không tiêu hao. Không thêm crafting/luyện đan/shop ngoài foundation.

Portal cùng map kiểm đích an toàn rồi di chuyển; portal khác map trả một yêu cầu chuyển vùng đã validate qua event `PortalTravelRequestedEvent`, chờ Client loader Phase 4 xử lý, không giả vờ đã tới map chưa tải. Fixture Phase 2 chỉ cần portal cùng map và rejection đích không hợp lệ. SpiritField hỗ trợ plant/growth/harvest và beast assignment; không mở hệ thống kinh tế nông nghiệp ngoài dữ liệu mẫu.

World tick dùng thời gian mô phỏng: nhận delta hữu hạn trong [0, 0.1]; Client Phase 4 dùng accumulator/fixed step, không âm thầm bỏ thời gian chậm frame. Thứ tự cố định: clock → regen/cooldown/status và dọn chết → cast/channel/projectile → AI → resolve movement → growth/bond → flush events. Mỗi actor/service được tick đúng một lần. Danh sách entity được chụp trước khi iterate; thêm/xóa áp ở boundary để không invalid enumeration. Pause/đóng HUD không tự thay giờ Domain.

### Trụ 7 — Core Events và Persistence

```csharp
sealed class SimulationClock {
    double ElapsedSeconds { get; } long TickIndex { get; }
    void Advance(float deltaTime);
}
interface IRandomSource { float Next01(); RandomState Capture(); void Restore(RandomState state); }
sealed class SourceIdAllocator { int Next { get; } SourceIdAllocator(int next = 1); int Allocate(); }
sealed class EntityIdAllocator { long Next { get; } EntityIdAllocator(long next = 1); EntityId Allocate(); }
interface IDomainContent {
    bool TryGetActor(string id, out ActorSpec spec);
    bool TryGetRealm(string id, out RealmSpec spec);
    bool TryGetTechnique(string id, out TechniqueSpec spec);
    bool TryGetStatus(string id, out StatusEffectSpec spec);
    bool TryGetBeast(string id, out BeastSpeciesSpec spec);
    bool TryGetItem(string id, out ItemSpec spec);
    bool TryGetNode(string id, out WorldNodeSpec spec);
}
sealed class DomainEventBus {
    IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct;
    void Publish<TEvent>(TEvent value) where TEvent : struct; // queue sau mutation
    void Flush();
}
sealed class GameSaveDto {
    string SchemaId; int SchemaVersion; string MapDefinitionId;
    double SimulationSeconds; long TickIndex; long NextEntityId; int NextSourceId;
    RandomStateDto Random; PlayerSaveDto Player;
    List<ActorSaveDto> Actors; List<WorldNodeSaveDto> Nodes;
    List<ProjectileSaveDto> Projectiles;
}
sealed class GameSaveMapper {
    GameSaveDto Capture(WorldState world);
    DomainResult<WorldState> TryRestore(GameSaveDto dto, IDomainContent content,
        WorldGeometry geometry, IRandomSource random, DomainEventBus events);
}
sealed class SaveSerializer {
    string Serialize(GameSaveDto dto);
    DomainResult<GameSaveDto> TryDeserialize(string json);
}
interface ISaveMigration {
    int FromVersion { get; } int ToVersion { get; }
    DomainResult<GameSaveDto> Migrate(GameSaveDto source);
}
```

**Events typed trong `Core/DomainEvents.cs`:** `ActorDamagedEvent(TargetId, SourceId, HpLost, ShieldLost)`, `ActorDiedEvent(ActorId, KillerId)`, `AbilityExecutedEvent(CasterId, TechniqueId)`, `NodeHarvestedEvent(ActorId, NodeId, Rewards)`, `RealmBreakthroughEvent(ActorId, FromRealmId, ToRealmId)`, `BeastTamedEvent(OwnerId, BeastId)`, cùng summon/recall/portal khi cần. ID nguồn damage là EntityId, phân biệt với `int SourceId` của modifier. Payload immutable/copy collections; unsubscribe bằng Dispose; subscribe/unsubscribe khi flush không làm hỏng vòng lặp. Lỗi listener được thu thập rồi báo ra ngoài, không nuốt exception hoặc chạy lại command đã commit; event không lưu delegate vào save.

`SeededRandomSource` hiện thực `IRandomSource` bằng PRNG nhỏ có trạng thái xác định (đề xuất xorshift32, seed khác 0); `RandomState/RandomStateDto` giữ AlgorithmId và state, `Next01` trả [0,1). Không dùng `System.Random` rồi giả định có thể khôi phục từ seed sau khi đã tiêu nhiều số. Test capture/restore chứng minh phần chuỗi tiếp theo giống nhau; validate counter allocator chống overflow trước khi cấp ID.

**Dữ liệu lưu bắt buộc:** player realm/substage/EXP; inventory/equipment; roster phẩm chất/huyết mạch/bond/state/assignment; actor identity/faction/owner/position/facing/stats gốc/vitals; learned abilities/cooldowns/cast/channel; active status + stack/duration/next tick/source snapshot; node growth/respawn/crop; projectile và target đã trúng; clock/random/counter ID. Derived modifier từ realm/equipment/beast/status được **tái tạo một lần từ trạng thái nguồn**, không vừa lưu tổng đã cộng vừa cộng lại khi load. Mỗi source instance giữ ID trong DTO; loader kiểm tra collision và NextSourceId lớn hơn mọi ID đã dùng. Snapshot ở boundary sau tick/flush, không giữa một harvest transaction.

**Load/compatibility:** JSON chỉ gồm DTO primitive/list/string/ID; `TypeNameHandling.None`, không deserialize type tùy ý. Có giới hạn kích thước/count/depth từ `SaveValidation`; reject NaN, ID trùng, vị trí blocked, inventory vượt stack/capacity, beast state/actor/assignment không nhất quán, thiếu required DefinitionId. Thêm definition mới không làm invalid save cũ; field optional mới có default rõ. Schema tương lai hoặc schema gameplay retired chưa có migrator → UnsupportedSchema; không xóa save/khởi tạo world rỗng âm thầm. Restore tạo graph mới, validate đủ rồi caller đổi reference; lỗi giữ world cũ và input JSON nguyên vẹn. Không dùng object random/event bus đang gắn world cũ trong quá trình restore thử; inject instance mới để tránh thay RNG/subscription trước khi validate xong.

Phase 2 chỉ serialize/restore trong bộ nhớ; ghi file atomically bằng temp → replace/backup thuộc Client save adapter Phase 3–4. Không gọi `File.*`, `PlayerPrefs` hoặc đọc persistentDataPath trong Domain/test. Chưa có migration thực tế được chạy trên save người dùng; interface migration không giả vờ hỗ trợ schema cũ.

## 6. Phase 2 — task và test nghiệm thu

Tạo fixture trước implementation tương ứng. Dùng NUnit chuẩn và fixture data nhỏ trong assembly tests, không sinh test runner riêng. Gom compile và suite theo lát cắt phụ thuộc/ cuối Phase; chỉ chạy thêm giữa chừng khi có lỗi hoặc cần chứng minh một regression. Không commit giữa task khi chưa có lệnh commit.

### P2.1 — Core/Stats/Actor contract

- [x] Tạo Core/Stats và identity/transform/actor spec; hoàn chỉnh các container actor khi tích hợp P2.3.
- [x] `StatSheetTests`: formula **154.44**, remove → **128.7**, nhiều nguồn cùng stat, replace không cộng gấp đôi, dirty cache khi đổi base/add/remove, số âm hữu hạn đúng công thức, NaN/Infinity bị từ chối mà không đổi state.
- [x] `CoreAndActorTests`: ID không trùng, enum/spec actor chung cho các faction, zero input không tự đổi hướng, vitals shield trước HP, clamp sau giảm max, chết đúng một lần, dead không hành động; event typed, unsubscribe và listener lỗi không nuốt.
- [x] Các constructor/service reject definition bất hợp lệ; không tạo state nửa chừng rồi trả success.

### P2.2 — Movement/Collision

- [x] Chuyển API motor sang `Transform2D`, giữ thuật toán geometry đã đọc; validator bán kính/delta và tìm điểm an toàn có giới hạn.
- [x] `MovementAndCollisionTests`: quãng đường thẳng bằng chéo đã normalize; trượt tròn/box/convex theo fixture thực; giữ CanStand sau từng bước; map edge; góc hẹp; chạm chính diện chọn hướng ổn định; spawn blocked bị từ chối.
- [x] Dash/knockback/projectile segment không xuyên obstacle mỏng; displacement không tự vòng. Input NaN/Infinity/radius 0/negative hoặc delta ngoài [0,.1] bị từ chối, không treo substep loop. Zero delta không làm di chuyển.

### P2.3 — Combat/Status

- [x] Viết effect specs/registry/selectors, ability book, cast/cooldown/status/projectile trước khi nối WorldState; hoàn chỉnh ActorEntity.
- [x] `CombatAndStatusTests`: damage Ngũ hành fixture **55**, Physical, quan hệ đảo chiều theo bảng, crit 0/1 bằng random fake, shield/death; đủ cả năm delivery shape, unique hit và line-of-sight bị chặn.
- [x] Cast thiếu Qi/chưa học/dead/cooldown không mutation; cast được trừ đúng một lần. Ví dụ cooldown 1s và reduction .2 → .8s; .79s chưa dùng lại, đủ .8s thì dùng được (epsilon test float rõ ràng).
- [x] Đủ bốn effect chạy độc lập và ghép; dash collision; restore không vượt max; channel hủy đúng; projectile đánh trúng theo đường di chuyển, không đánh lại cùng target.
- [x] Status duration 3s/interval 1s tick đúng ở 1,2,3 rồi hết; stack cap/refresh/independent/two casters; gỡ/hết hạn bỏ đúng nguồn. Fixture phụ duration .06s/interval .02s dùng .06 một lần hoặc .02 ba lần cho cùng kết quả trong giới hạn world delta. Snapshot source vẫn hoạt động khi actor nguồn đã bị thu hồi.

### P2.4 — Cultivation/Beasts

- [x] `CultivationTests`: substage theo threshold, đủ EXP đột phá thay source realm một lần, giữ EXP dư; thiếu EXP/missing realm/dead/terminal realm không đổi stat/EXP/event. Dùng hai realm fixture Luyện Khí → Trúc Cơ, không chốt con số sản phẩm.
- [x] `BeastAiAndRosterTests`: thú có Attack 20, link ratio .1 cho +2 vào stat chủ khi species cho phép; refresh/summon lặp không +4; recall/death/load gỡ/khôi phục đúng; modifier nguồn khác còn nguyên.
- [x] Cap 1 từ chối thú thứ hai; invalid owner/egg/field/blocked spawn không tạo orphan actor hoặc modifier. Assigned và active loại trừ nhau; cap giảm reconciles xác định.
- [x] Follow có vùng dừng, không rung ở khoảng cách mục tiêu; trượt vật cản; leash teleport chỉ tới điểm hợp lệ, thất bại có fallback hữu hạn. Assist ưu tiên kẻ đánh chủ rồi hostile gần; không đánh Neutral, actor chết hoặc qua tường; dùng đúng cooldown/Qi.

### P2.5 — Inventory/Nodes

- [x] `InventoryAndNodeTests`: merge stack, remove vừa đủ/không đủ, reject zero/negative/overflow, kho đầy không đổi một phần.
- [x] Thu hoạch herb/ore đưa đủ reward vào kho, chuyển node sang cooldown; hai harvest liên tiếp chỉ có một thưởng/event. Còn một slot nhưng hai loại reward → không thêm loại đầu rồi thất bại loại sau.
- [x] Node non, out-of-range, bị vật cản, không có owner/inventory trả lỗi; growth/respawn đúng clock. SpiritEgg ra egg; field plant/grow/harvest/assignment; portal cùng map tôn trọng geometry, đích invalid không chuyển.
- [x] Pill/scroll/equipment dùng effect/spec: full vital hoặc đã biết skill không tiêu hao; đổi pháp bảo thay modifier đúng nguồn và không mất đồ khi túi không chứa được đồ tháo ra.

### P2.6 — World integration

- [x] Wire các service qua constructor; xây WorldCommandProcessor và tick order, không dependency Unity và không registry static toàn cục.
- [x] `WorldStateTests`: move → cast → beast assist → harvest → breakthrough chạy trên cùng world fixture; mỗi frame mỗi timer chỉ tick một lần. Xóa actor trong tick không lỗi enumeration; command sai owner không mutation.
- [x] Với seed và chuỗi lệnh/delta giống nhau, trạng thái kết quả tương đương. Tick 0 không tiến thời gian; invalid delta bị từ chối. Event chỉ quan sát state đã commit; không emit AbilityExecuted khi bị từ chối.

### P2.7 — Persistence và chốt suite

- [x] Thêm DTO/mapper/serializer/validation; pin dependency test framework đã resolve. Không tự tạo file save người dùng.
- [x] `InventoryAndNodeTests`: round-trip `GameSaveDto` bằng JSON khôi phục đầy đủ kho/node/cultivation/roster/HP/position/learned/cooldown; active status/assigned beast/equipment không nhân modifier sau nhiều vòng load.
- [x] `WorldStateTests`: save khi cast/channel/projectile đang chạy → restore → tick tiếp cho kết quả như world không save; clock/RNG/ID tiếp tục nhất quán, không trúng lại target đã hit.
- [x] JSON lỗi/trống/quá lớn/schema mới/missing definition/ID trùng/count âm/blocked spawn/NextSourceId sai → lỗi rõ ràng và world cũ không đổi. Add definition mới hoặc optional field có default không làm hỏng save hợp lệ; schema retired không được tự nhận dạng thành schema mới.
- [x] Chạy toàn bộ EditMode suite và compile cuối Phase 2, review diff/assembly dependencies, đối chiếu từng test bắt buộc. Ghi counts/pass/fail thực tế và thời gian vào báo cáo ngắn, không ghi số dự kiến như kết quả đã đạt.

## 7. Lệnh và bằng chứng kiểm chứng

Các lệnh dưới được **đề xuất để chạy sau duyệt**, không phải đã chạy thành công. CLI `unity recompile --help`, `unity test --help`, package lock và binary Editor đã được kiểm tra tại lượt lập kế hoạch.

Khi Editor đang mở đúng project và có Pipeline:

```sh
unity recompile --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo --json
```

Phải đọc kết quả cấu trúc/console và chờ compilation hoàn tất, 0 errors. Kiểm reference bằng Unity Editor/Pipeline API, không viết file `*Checks.cs`. Không mở thêm batch Editor trên project đang bị một Editor khác giữ; kiểm tra trạng thái trước, không dùng `unity close` làm mất scene chưa lưu.

Khi dùng Test Runner trong Editor: chọn **EditMode → LinhThu.Tests.EditMode → Run All**, xuất NUnit XML ngoài checkout. Khi Editor đã đóng sạch, dùng CLI chuẩn:

```sh
unity test /Users/nguyenhoangdacdat/dev/nguthu-unity-demo \
  --mode EditMode --filter LinhThu.Tests.EditMode \
  --output /private/tmp/nguthu-core-foundation/phase-2-editmode.xml \
  --report-format nunit
```

Tạo thư mục output trước khi chạy, không dùng kết quả XML cũ. Nếu Pipeline không dùng được, compile batch khi project không bị lock:

```sh
/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath /Users/nguyenhoangdacdat/dev/nguthu-unity-demo \
  -logFile /private/tmp/nguthu-core-foundation/phase-1-compile.log
```

Phase 2 dùng log riêng tương ứng. Xác minh exit code, import/compilation hoàn tất và log không có compiler errors; batch mở được không tự chứng minh prefab hoặc visuals đúng. Test run cần số test > 0, đúng assembly/fixture, failed = 0 và không skip các yêu cầu bắt buộc. Lỗi môi trường/timeout/không có XML không tính là pass.

Quy tắc assembly dựa trên [Unity Assembly Definition reference](https://docs.unity3d.com/6000.0/Documentation/Manual/class-AssemblyDefinitionImporter.html): `noEngineReferences` để loại Unity references; dependency giữa assembly phải khai báo đúng khi đổi tên. Các option CLI cụ thể lấy từ bản `unity` cài trên máy và Test Framework `1.8.0` được resolve, không suy ra từ hướng dẫn builder cũ.

## 8. Bàn giao cho Phase 3–4 và điều kiện duyệt

**Phase 3, sau khi Phase 2 đạt:** đổi assembly Client sang `LinhThu.Client`; di chuyển component về Actors/Beasts/World/Environment/Camera/UI; tách `OwnerDashboard/OwnerCatalog` sang `LinhThu.DevTools`, không tạo Client → DevTools reference. Tạo SO/config registry, hai cảnh giới, ba outfit `classic/jade/basic` từ HeroBase, ba công pháp cơ bản, Huyền Quy, item/node mẫu. Hoàn chỉnh `ModularEquipmentRenderer` thành Sprite Swap thực sự; giữ animation phase/pivot, flip toàn rig thống nhất. Bỏ `Resources.Load` Material/JSON khỏi cả `CartoonCharacterRig`, `GardenAmbientMesh`, dashboard và các code được giữ. Prefab dùng serialized refs. Save file adapter mới dùng namespace/path riêng và ghi atomically; không đụng save cũ. Cuối Phase compile 0 lỗi và test regression Domain đạt.

**Phase 4:** tạo `Content/Scenes/MainGame.unity` qua Editor/Pipeline, chỉ scene này trong Build Settings; chứa `MeadowMainMap`, player, Huyền Quy, herb/ore/field/portal, mục tiêu combat và HUD. `GameBootstrapper` đọc registry, dựng world, input di chuyển/cast/thu hoạch/summon/recall/đổi đồ; không nhúng luật combat vào MonoBehaviour. Dùng prefab/config đã chuẩn hóa, không quay lại builder. Compile 0 lỗi; chủ dự án bấm Play để nghiệm thu hình ảnh và cảm giác chơi.

**Ngoài phạm vi:** multiplayer/network/backend, scale 100–300 người, phát hành, economy/crafting hoàn chỉnh, crystal/talent redesign, phục hồi mọi gameplay cũ, tự mua/tải art, chỉnh shader hoặc hướng mỹ thuật ngoài chỉ thị, migration save thật, Git history/LFS migration, cleanup cache/build ngoài manifest.

**Để duyệt:** chấp thuận manifest Phase 1 (bao gồm master ArtDirection và các scene cũ), các sửa phụ thuộc tối thiểu, API/chính sách đề xuất ở mục 2 và 5, và cửa compile/test cuối mỗi Phase. Khi được duyệt sẽ bắt đầu P1.1; không coi việc ghi kế hoạch hoặc cập nhật AGENTS là đã bắt đầu Phase 1.
