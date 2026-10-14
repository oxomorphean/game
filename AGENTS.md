# Linh Thú Ký — hướng dẫn cho agent

## Phối hợp các phiên

- Trước mỗi task trong project, đọc [GAME_DESIGN.md](GAME_DESIGN.md), gồm mục Phối hợp bốn phiên; xác định vai trò theo yêu cầu của người dùng và giữ đúng phạm vi task.
- Chỉ coi quyết định được chủ dự án duyệt là luật chính thức; đề xuất và thông số thử nghiệm phải được ghi riêng.
- Thay đổi ảnh hưởng phiên khác cần ghi ngắn kết quả, file/asset hoặc interface thay đổi và phần cần bàn giao trong tài liệu của phần đó.
- Một phiên sửa một scene, prefab hoặc file chung tại một thời điểm; kiểm tra thay đổi có sẵn trước khi sửa và không ghi đè công việc của phiên khác.

## Chỉ thị kiến trúc hiện hành — 14/10/2026

- Áp dụng chỉ thị Core Foundation của chủ dự án: hợp nhất tài nguyên, xây 7 trụ cột Domain, Configs/Client rồi kết nối game loop. Chỉ thị này thay phần xung đột của chế độ chỉ xem mỹ thuật ngày 12/10, pipeline nhiều hướng, scene thử, builder và Web QA trong tài liệu cũ; không tự phục hồi gameplay từ `Recovery/`.
- Kế hoạch [Core Foundation Phase 1–2](Architecture/CORE_FOUNDATION_PHASES_1_2.md) đã được chủ dự án duyệt ngày 14/10/2026; thực hiện Phase 1 rồi Phase 2 và báo kết quả thực tế cuối Phase 2. Ba điều chỉnh bắt buộc: backup bền vững ngoài repo, giữ 22 StatId với 5 Affinity dùng chung công/kháng, và kiểm dependency Unity để xóa hai script Cartoon ngay khi không còn dùng.
- Cả bốn Phase đã có implementation và checkpoint; xem [bằng chứng](Architecture/CORE_FOUNDATION_PROGRESS.md). MainGame là entry hiện hành; nghiệm thu mỹ thuật vẫn cần chủ dự án xem trực tiếp.
- Thực hiện tuần tự: **1. Purge & Consolidate → 2. Domain + EditMode tests → 3. Configs + Client + DevTools → 4. MainGame + GameBootstrapper**. Cuối mỗi Phase phải có kết quả Unity compile **0 lỗi** trước khi chuyển Phase; cuối Phase 2 phải có NUnit EditMode tests đạt. Không dùng kết quả cũ thay cho kiểm chứng của Phase hiện tại.
- Những công thức/cân bằng hoặc chính sách chưa được chỉ thị xác định phải ghi là đề xuất trong kế hoạch, không tự biến thành luật chính thức. Cập nhật tài liệu hiện hành theo từng Phase sau khi được duyệt.

## Kiến trúc đích và ranh giới assembly

- Toàn bộ source/asset chạy game nằm trong `Assets/_Project/`: `Art/Characters/HeroBase`, `Art/Environment/Meadow`, `Content/Configs`, `Content/Prefabs`, `Content/Scenes`, `Scripts` và `Shaders`.
- `Scripts/Domain/LinhThu.Domain.asmdef`: Pure C#, `noEngineReferences: true`; không phụ thuộc `UnityEngine`, `UnityEditor`, `MonoBehaviour`, `ScriptableObject`, input, renderer hoặc API lưu file của Unity. Clock, random và dữ liệu cấu hình được truyền vào; Domain sở hữu trạng thái và kiểm tra lệnh.
- `Scripts/Client/LinhThu.Client.asmdef` tham chiếu Domain. `Client/Configs` chứa class ScriptableObject; asset ở `Content/Configs/{Actors,Cultivation,Combat,Beasts,Items}`. Chuyển SO thành Domain Spec; không lưu trạng thái chơi lên asset cấu hình.
- `Scripts/DevTools/LinhThu.DevTools.asmdef` sở hữu `Dashboard/OwnerDashboard` và `OwnerCatalog`; tách khỏi HUD chơi. Domain/Client không tham chiếu ngược DevTools.
- `Scripts/Editor/LinhThu.Editor.asmdef` chỉ chạy Editor; giữ `Inspectors/MeadowTerrainPathEditor`. `Scripts/Tests/EditMode/LinhThu.Tests.EditMode.asmdef` chỉ chạy EditMode với NUnit và tham chiếu Domain.
- `Content/Scenes/MainGame.unity` là scene gameplay chính thức duy nhất sau Phase 4. `Client/Bootstrap/GameBootstrapper` là Composition Root: đọc `ContentRegistrySO`, dựng `WorldState`, chuyển input thành lệnh Domain và đồng bộ view/HUD từ trạng thái thật. Không đưa luật game vào UI/animation.

## Bảy trụ cột Domain

Mười thư mục `Core`, `Stats`, `Actors`, `Cultivation`, `Combat`, `Beasts`, `Inventory`, `World`, `Navigation`, `Persistence` tổ chức bảy trụ cột, không phải mười hệ gameplay độc lập:

1. **Stats:** `StatId`, `StatModifier`, `StatSheet`, `VitalResourcePool`. Chỉ số mở rộng theo ID; không tạo nhóm field cố định `hp/attack/...` thay cho StatSheet. Công thức: `(Base + Sum(FlatAdd)) * (1 + Sum(PercentAdd)) * Product(1 + PercentMult)`; dirty cache và `RemoveAllModifiersFromSource(int sourceId)`. HP/Qi/Shield hiện tại tách khỏi chỉ số tối đa. SourceId phải phân biệt được từng nguồn, không cộng lại modifier mỗi frame.
2. **Actors:** một gốc `ActorEntity` cho tu sĩ, thú đồng hành, yêu thú và boss; giữ `EntityId`, `DefinitionId`, faction, `Transform2D`, stats, vitals, action state, status container và ability book. Tọa độ Domain dùng kiểu C# không phụ thuộc Unity.
3. **Cultivation:** `CultivationProgress`, `RealmSpec`, `TryBreakthrough`; thay modifier cảnh giới theo nguồn khi đột phá thành công. Lệnh thất bại không đổi tiến trình hoặc chỉ số.
4. **Combat:** Ngũ hành bằng dữ liệu; delivery `CircleAoE/ConeSector/LineRay/Projectile/Self`; công pháp ghép `Damage/ApplyStatus/Displacement/RestoreVital` qua effect spec/handler. Không dùng switch khổng lồ theo tên công pháp. Status hỗ trợ thời hạn, nhịp tick, stack và gắn/gỡ modifier đúng nguồn; cooldown thuộc instance, không thuộc SO.
5. **Beasts:** `BeastInstance`, `BeastRoster`, `BeastAiController`; trạng thái trứng/nghỉ/xuất chiến/làm linh điền, cộng hưởng chủ nhân đúng một lần khi xuất chiến; Follow/Assist và leash dùng hình học Domain. Thu hồi/chết phải dọn actor và modifier theo chính sách đã duyệt.
6. **Inventory + World:** `ItemStack`, `InventoryContainer`, `WorldNodeEntity`, `WorldState`; nhóm linh thạch, thảo, khoáng, đan, pháp bảo, trứng, ngọc giản; node thảo/khoáng/trứng/linh điền/cổng có dữ liệu và trạng thái riêng. Thu hoạch chỉ tiêu hao node khi túi nhận đủ; không mất đồ khi thất bại. `Navigation/WorldGeometry` và `MovementMotor` dùng footprint chân để xử lý đi/trượt va chạm.
7. **Core + Persistence:** event bus kiểu mạnh, ID ổn định, clock mô phỏng; DTO JSON dùng `DefinitionId` và phiên bản schema, không serialize Unity object. Dùng thống nhất tên `GameSaveDto` của cây thư mục chỉ thị, không tạo thêm schema song song mang tên `SaveStateDto`. Load phải validate trước khi thay thế world; không đọc/ghi đè save cũ khi chưa có migration được duyệt.

## Pipeline mỹ thuật bắt buộc

- Nhân vật chuẩn từ HeroC2, chuyển thành `HeroBase`: **một góc 3/4 top-down**, lật trái/phải bằng FlipX/phép phản chiếu chung của toàn rig; không làm tám hướng artwork riêng. Bộ phận đầu, thân, tay/chân/giày chia front/back dùng chung xương, canvas/bind coordinates và điểm chân.
- `OutfitConfigSO` điều khiển **Sprite Swap theo dữ liệu** trên các slot/xương tương ứng. Đổi đồ giữ `AnimatorController`, thời điểm clip, pivot và rig; không sinh lại animation hoặc reset clip. Cách bật/tắt lớp ở prototype chỉ là trạng thái cũ cần chuyển đổi.
- Linh thú dùng cùng `ActorEntity`; hình ảnh có thể rig nhiều mảnh hoặc sprite đơn với procedural motion/`SunShadowMesh`. Presentation không sở hữu luật AI/combat.
- Map dùng tiling + `MeadowGroundSurface`/`MeadowTerrainPath` hoặc Tilemap; không ghép ảnh nền nguyên mảng. Cây/bụi/đá/hồ/thảo/khoáng/linh điền/trại/cổng là prefab độc lập, pivot tại chân, Y-sorting theo Y; collision chỉ ở phần gốc qua `WorldGeometry`.

## Guardrails khi hợp nhất

- Không đặt tên file/class runtime chứa `Trial`, `Preview`, `Test`, `Demo`, `Temp`, `C2`, `V1`, `V2`, `Method2`. Tên fixture `*Tests.cs` trong assembly tests được phép; không đổi tên third-party package để áp quy tắc này.
- Không giữ hoặc tạo Editor Scene-Builder thay kiến trúc runtime: loại `*Builder.cs`, `*Checks.cs` và helper thử tương ứng theo manifest được duyệt; giữ inspector đường. Cấu hình/prefab là source of truth.
- Không dùng `Resources.Load` để nạp Material/JSON trong kiến trúc đích. Truyền reference serialize trên SO/prefab hoặc qua Composition Root; không dùng string path fallback che lỗi thiếu dữ liệu.
- Di chuyển/đổi tên asset trong `Assets/` bằng `git mv` kèm `.meta`, bao gồm `.meta` của thư mục; giữ GUID và sprite subasset ID. Với file chưa tracked, di chuyển cặp file/meta và đối chiếu manifest, không tự stage thay đổi của phiên khác. Sửa scene/prefab qua Unity Editor/Pipeline API; không sửa YAML bằng tay.
- Trước thao tác xóa, kiểm kê dependency của asset giữ lại và lưu bản khôi phục ngoài checkout cho cả dữ liệu chưa commit. Không xóa phụ thuộc dùng chung theo tên thư mục. Không reset/clean/stash toàn repository; không commit/push/deploy nếu chưa được yêu cầu.
- Backup archive và manifest SHA-256 phải lưu tại `~/dev/nguthu-backups/core-foundation-backup/`, tuyệt đối không lưu backup trong `/private/tmp/`. Chỉ compile log và NUnit XML được lưu tạm tại `/private/tmp/nguthu-core-foundation/<run-id>/`.
- Dùng `AssetDatabase.GetDependencies` kiểm tra `CartoonCharacterRig.cs` và `CartoonUiRig.cs` từ HeroBase, MeadowMainMap và các prefab prop được giữ. Không phụ thuộc thì xóa ngay Phase 1; nếu còn phụ thuộc thì ghi rõ và xóa dứt điểm Phase 3.

## Làm nhanh và dứt khoát

- Làm theo nhịp phát triển game: khảo sát vừa đủ để xác định đúng luồng hiện hành, triển khai trọn một tính năng hoặc mốc chơi được, rồi kiểm tra và sửa lỗi. Tránh kéo dài phân tích các khả năng chưa có dấu hiệu xảy ra.
- Ưu tiên API Unity, pattern game phổ biến và cách tổ chức đang có. Với thao tác quen thuộc, áp dụng cách chuẩn ngay; không thử riêng từng bước để xác nhận lại hành vi đã rõ.
- Dùng Unity CLI/Pipeline, Unity Test Framework và NUnit có sẵn; không tạo builder, script QA, framework hoặc quy trình riêng thay cho công cụ tiêu chuẩn.
- Với code, bố cục UI, animation và thông số thử nghiệm dễ sửa lại, tự chọn phương án hợp lý trong phạm vi task và triển khai liền mạch. Ghi ngắn assumption cần thiết; chỉ hỏi khi lựa chọn ảnh hưởng đáng kể luật chưa chốt, dữ liệu, kiến trúc hoặc chi phí.
- Gom compile và test liên quan vào cuối mốc/Phase; không sửa vài dòng rồi build/test/chụp ảnh lặp lại. Chỉ kiểm tra chen giữa khi có lỗi thực tế hoặc một bước tích hợp chưa rõ cản phần tiếp theo. Hiển thị do chủ dự án mở Unity Editor và bấm Play nghiệm thu; compile không chứng minh hình ảnh đạt chuẩn.
- Save/migration và thao tác có nguy cơ mất dữ liệu vẫn cần bản khôi phục và kiểm tra riêng trước khi áp dụng. Sửa lỗi thuộc phạm vi trước khi bàn giao; báo đúng phần đã và chưa kiểm chứng.

## Mỹ thuật

- Trước task tạo hoặc sửa nhân vật, linh thú, map, tile, đồ vật, hiệu ứng hay UI, đọc [ArtDirection/STYLE_GUIDE.md](ArtDirection/STYLE_GUIDE.md) và ảnh tham chiếu được liên kết trong đó.
- STYLE_GUIDE.md giữ chuẩn thị giác; các pipeline đã được chỉ thị ngày 14/10/2026 thay thế áp dụng theo các mục trên. Ảnh, prompt và README mẫu cũ là lịch sử, không tự dùng làm hướng mặc định.
- Giữ thiết kế nhất quán qua asset và animation; đối chiếu đủ checklist trong style guide trước khi báo cáo đạt chuẩn.
- Ghi rõ asset mới chỉ là mẫu ảnh hay đã nhập/chạy trong Unity. Không suy ra sprite có thể dùng ngay chỉ vì ảnh sinh ra trông đẹp.
- Không tự đổi hướng mỹ thuật đã chọn. Yêu cầu trực tiếp mới của người dùng được ưu tiên.

## Phạm vi dự án

- Dự án nằm tại `/Users/nguyenhoangdacdat/dev/nguthu-unity-demo`.
- Gameplay hiện hành theo [GAME_DESIGN.md](GAME_DESIGN.md), kiến trúc theo [Architecture/CODE_ARCHITECTURE.md](Architecture/CODE_ARCHITECTURE.md); [game_architecture_plan.md](game_architecture_plan.md) là bản tham khảo khi không xung đột với tài liệu hiện hành. Style guide sở hữu quyết định mỹ thuật.
- Chỉ xóa/thay scene và asset trong manifest Phase đã duyệt. Dùng Unity Editor/Pipeline để thao tác scene/prefab; không sửa YAML bằng tay.
- Task nhỏ làm trực tiếp, kiểm tra vừa đủ. Không tự thêm gameplay, dependency trả phí hoặc tải bộ asset ngoài phạm vi yêu cầu.

## Tài nguyên và dung lượng

- Không tạo lại `Verification/`, `AgentScripts/`, `Web/*.html`, `serve.py`, `serve_adventure.py`; không build WebGL hoặc chạy trình duyệt headless chụp ảnh để tự nghiệm thu. Log compile/NUnit XML đặt ngoài checkout hoặc trong thư mục output đã ignore; báo cáo Markdown ngắn đặt trong `Architecture/`.
- `ArtDirection/` chỉ giữ `STYLE_GUIDE.md` sau Phase 1. Không đặt `.kra`, `.psd`, `.svg`, `.py` trong `Assets/`; master/backup nếu cần lưu phải ở ngoài checkout, không trong Git. Giữ export game thực sự dùng cùng `.meta` và shader hiện có. Không tự xóa cache, `Recovery/` hoặc save cũ ngoài manifest được duyệt.
- [Quy tắc lưu trữ](Architecture/REPOSITORY_STORAGE.md) và các liên kết bằng chứng cũ cần đồng bộ trong Phase 1; phần yêu cầu giữ master trong ArtDirection hoặc ghi Verification đã bị chỉ thị mới thay thế.
