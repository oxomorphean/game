# Thay mẫu nam — 15/10/2026

Yêu cầu được duyệt: dùng ảnh gửi sau cùng `a_clean_isolated_transparent_background_png_illust.png`, tách trong Krita, gắn xương Unity, thay vào game và xóa asset nhân vật cũ không dùng. Chủ dự án chọn giữ cơ thể nền, tóc/đồ bổ sung sau. Không dùng ảnh `Nhân vật chibi tối giản toàn thân.png` gửi trước đó.

Trạng thái tiếp nối 16/10: chủ dự án đã yêu cầu thử tóc/trang phục trên phôi này; xem [lượt thử wardrobe](WARDROBE_PILOT.md) và [các lớp mặt](FACE_FEATURES.md). Các kết quả bên dưới ghi mốc thay cơ thể ngày 15/10.

## Phạm vi và invariant

Thay artwork/pivot/pose của HeroBase, cập nhật cấu hình ngoại hình và binding hiệu ứng. Giữ GUID asset được thay tại chỗ, controller, đường dẫn xương, Visual 0.625, các ID outfit/palette và luật Domain/save. Điều kiện hoàn thành: MainGame dùng mẫu mới, các bộ phận đủ/đúng bind, Idle/Run và action pose hoạt động, test đạt, asset cũ không còn dependency trước khi xóa, không có meta mồ côi. Rủi ro chính là hở khớp ở pose gập và tham chiếu tới renderer bị thay; kiểm bằng Play và NUnit.

## Kết quả sản xuất

- Krita: master 17 lớp gồm 16 phần xuất và một lớp ảnh gốc ẩn/khóa. Canvas 1087×1446 giữ nguyên; bo và bù da tại các khớp bị che để gập được. Đầu/mặt/quần lót theo ảnh nguồn, không thêm tóc/mắt/áo.
- Unity: 16 SpriteRenderer theo xương Transform (cutout rig), PPU 500; đầu, cổ, thân, chậu và hai bên bắp tay/cẳng tay/bàn tay/đùi/cẳng chân/bàn chân. Đây không phải mesh skinning/SpriteSkin. AnimatorController giữ nguyên, Idle/Run được chỉnh lại trong asset cũ; root tại chân, flip toàn rig và scale 0.625.
- Outfit basic/classic/jade giữ ID để không phá item/save; hiện cùng body. Hair/eye có cờ hidden rõ ràng và validation bắt buộc sprite khi không hidden. Các slot trống tiếp tục phục vụ trang phục sau này.
- FootstepDust gắn hai renderer bàn chân mới. Action pose dùng tọa độ khớp mới; gỡ pose vạt áo trống. TrainingDummy được chuyển từ variant sang prefab độc lập cùng GUID để bỏ các override áo/quần người không còn tồn tại; trụ gỗ và hành vi Mộc Nhân giữ nguyên.
- Đã kiểm dependency trực tiếp của 365 asset và xóa 38 PNG cũ cùng 38 meta; thư mục HeroBase còn 16 PNG cơ thể và SpiritSword đang dùng. Không xóa cấu hình/item còn được tham chiếu.

## Khôi phục và nguồn chỉnh sửa

Thư mục ngoài checkout: `/Users/nguyenhoangdacdat/dev/nguthu-backups/core-foundation-backup/20261015-hero-male-refresh/`.

- `source-before-hero-refresh.tar.gz`: 856 file source trước task, gồm dữ liệu chưa commit; `manifest-sha256.json`, `archive-sha256.txt`, trạng thái/diff/index trước task đi kèm. SHA-256 archive: `51ace30fa70a73502757b2446d21df99fdf661e9c4f93abba5d97bd704f28d84`.
- `Source/hero-male-layered.kra`: master chỉnh tiếp trong Krita; `hero-male-source.png`: đúng ảnh gốc; `parts.json`: pivot/lớp/hash export. Script thao tác một lần được giữ cùng master ngoài repo.
- `deleted-assets.json`: manifest asset đã xóa; hash của từng PNG/meta khớp backup trước khi xóa.
- `task-change-manifest.json`: đường dẫn và SHA-256 trước/sau của riêng task. Rollback có chọn lọc từ archive theo manifest này; không reset toàn repo và không ghi đè phần đã sửa từ các công việc trước.

## Kiểm chứng

- Unity compile: **0 lỗi, 0 cảnh báo**, `failed=false`; lần kiểm cuối xác nhận source đã biên dịch và đang up-to-date.
- Unity Test Framework EditMode: **256/256 passed** — 162 Domain + 94 Client, 0 failed/skipped/inconclusive, 21.60 giây. Bao gồm đủ body slots, blank appearance phải khai báo rõ, không giấu sprite hỏng, đổi appearance giữ pose/controller, stride/chân trụ, thiền/câu cá, bụi và tham chiếu prefab.
- Native Unity Play: đã xem Idle, pose Run hai hướng, thiền và câu cá ở camera gốc/phóng lớn. Cần câu neo đúng ToolSocket (`rodGap=0`); cả hai renderer bàn chân đã được gắn trong instance MainGame. Kết quả này là kiểm tra các pose đã xem, không chứng nhận mọi frame hay mức hoàn thiện mỹ thuật; chủ dự án còn nghiệm thu trực tiếp.
- Console hiện tại: **0 errors, 0 warnings**, `compilationFailed=false`. Buffer Pipeline còn 21 lỗi từ các công việc trước buổi sáng; đã đọc lịch sử, không có lỗi runtime mới từ lượt Play này.
- Assets: **0 orphan meta, 0 missing meta, 0 thư mục rỗng**. Đối chiếu 394 GUID được giữ với backup: không đổi. Ảnh tải về gốc giữ nguyên SHA-256. Diff đã review theo baseline trước task, bảo toàn các thay đổi gameplay có sẵn; `git diff --check` đạt khi bỏ qua blank-at-eol của YAML Unity và tắt clean filter LFS trong lệnh đọc.
- Evidence: `/private/tmp/nguthu-core-foundation/20261015-hero-male-refresh/` gồm `compile-final.json`, `tests-final.json`, `console-final.json`, `console-error-history.json`, `asset-cleanup.json`, `dependencies-before-delete.json`, `rig-final.json`, `handoff-scene.json`. Không build WebGL, không commit/push/deploy.
- Bàn giao: Unity mở MainGame, Edit Mode, scene không dirty, camera 5.6 và follow đã khôi phục; bấm Play để xem. 76 file xóa đã gỡ khỏi Git index theo quy tắc purge; các thay đổi khác chưa stage.

## Retrospective

Kiểm pose thiền phát hiện vùng cắt gối cần bo và căn pivot giữa tiết diện chân. Test bước chân bắt được tham chiếu renderer cũ dù rig vẫn hiển thị; đã nối lại và giữ nguyên assertion. Giữ kiểm toàn bộ dependency vì prefab variant có thể chứa override tới asset không còn renderer hoạt động. Không thêm framework hay script QA trong repo.
