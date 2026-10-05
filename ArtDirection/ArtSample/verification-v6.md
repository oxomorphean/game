# Kiểm chứng Huyền Giáp Quy v6 — 05/10/2026

## Phạm vi

Nhánh `codex/huyen-giap-quy` bắt đầu từ v4 `4247dcf`. Giữ cổng, hai cây anh đào, sân gạch/cỏ, đá, hồ và bóng cây v4; thay linh quy xanh bằng Huyền Giáp Quy có mai đen và hoa văn vàng. Đây là mẫu mỹ thuật đang chờ người dùng duyệt, không thêm gameplay.

Agent mỹ thuật và reviewer độc lập dùng `gpt-6.1-sol`, effort `max`. Agent kiểm kê Git dùng `gpt-6-luna`, effort `max`; model tên Lana không có trong danh sách được cung cấp. Agent mỹ thuật chỉ ghi vào thư mục tạm; main agent thực hiện import, scene, build và kiểm tra chơi.

## Asset và Unity

- Nguồn Aseprite đã được mở lại qua MCP: 64×64, 9 layer, 10 frame, tag idle 1–4 và walk 5–10. PNG game được xuất từ nguồn này, không dùng imagegen hoặc tải asset ngoài.
- Main agent chạy `AgentScripts/pixel/pixel_validate.py` với `--kind sprite --cell 64x64 --max-colors 16 --strict`: 3 PASS, 0 WARN, 0 FAIL; cả ba PNG dùng 13 màu, không có lỗi alpha. Kết quả: `pixel-verification-v6.json`.
- So sánh Git blob của 9 PNG môi trường với v4 sau khi dựng scene: tất cả trùng. Kết quả: `environment-verification-v6.json`. `SpiritTurtleController.cs` không có diff so với v4.
- Unity Pipeline/CLI chạy `ArtSampleBuilder.CreateScene`: thành công, không diagnostics. Scene và metadata được lưu bằng Unity API.
- Kiểm tra trực tiếp bằng Unity eval: sprite 64×64, pivot `(32,12)`, PPU 16, Point, không mipmap/nén; Pixel Perfect Camera tham chiếu 384×256, asset PPU 16.
- `TurtleRuntimeProbe.Begin` trong Play Mode: PASS, thấy đủ 4 frame idle và 6 frame walk, di chuyển và reset về spawn đạt. Kết quả: `animation-verification-v6.json`.

Lần probe đầu có frame đi lẫn vào pha đứng do thiết bị input thật can thiệp. Chạy lại cùng probe với Keyboard/Mouse/Pen tạm tắt trong Unity Input System cho kết quả PASS. Tất cả thiết bị đã được bật lại trước khi dừng Play; không sửa controller hoặc giảm điều kiện kiểm tra. Phép thử browser bên dưới dùng input thật riêng.

## WebGL và chơi thử

`ArtSampleBuilder.BuildWeb` chạy bằng Unity Pipeline/CLI: Succeeded, 0 lỗi, 3,43 giây với cache hiện có, 35.031.487 byte đầu ra (~33,4 MiB). Không dùng thời gian này làm cam kết thời gian build mới hoặc tốc độ tải mạng. Kết quả: `build-result-v6.json`.

Reload bản thật tại `http://127.0.0.1:8788/` và kiểm tra qua Codex in-app browser:

- Cổng, hai tán anh đào, bóng cây và hồ v4 hiển thị; Huyền Giáp Quy đen/vàng hiện ở giữa sân: `web-preview-v6.png`.
- Bấm sân: linh thú đi tới vị trí được chọn: `web-pointer-v6.png`.
- Bấm A: linh thú quay trái: `web-left-v6.png`.
- Bấm R: linh thú về vị trí xuất phát: `web-reset-v6.png`.
- Console warn/error sau các thao tác: rỗng, lưu tại `web-console-v6.json`.

Ảnh Game View thực tế trong Editor: `unity-preview-v6.png`. Reviewer mỹ thuật không có finding cần sửa trước khi tích hợp; đánh giá đó không thay thế việc người dùng duyệt mẫu.

## Git và giới hạn

V4 và v5 giữ nguyên trên `main`/`art-v4-before-unify` và `codex/art-style-sample`/`art-v5-candidate`. V6 được lưu trên nhánh riêng và tag `art-v6-huyen-giap-quy`. `ROLLBACK.md` hướng dẫn đổi bản và build lại WebGL. Không push hoặc publish.

Review diff tập trung vào PNG linh thú, tên hiển thị, nguồn editable và tài liệu. Scene được Unity dựng lại nên ID đối tượng và thứ tự serialization thay đổi; không chỉnh YAML bằng tay. Kiểm tra whitespace cho file do agent viết đạt; scene Unity có trailing space của serializer ở trường rỗng nên được loại khỏi phép kiểm tra này. `Assets/_Recovery/` được giữ nguyên trên máy và bỏ qua trong Git.

Hướng trái dùng flip từ phải; lên/xuống vẫn dùng cùng bộ đi như v4. Chưa tạo sprite riêng cho bốn hướng hoặc đo hiệu năng trên điện thoại/máy người chơi khác. Nguồn môi trường v4 là ảnh thử nghiệm đã có; task này chỉ vẽ mới linh thú qua Aseprite.
