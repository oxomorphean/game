# Môi trường có thể bố trí riêng — 2026-10-07

## Kết quả

Nền cỏ và đường đất cố định dùng `ground-road.png`, giữ các mảng đất bất quy tắc cùng đá lát. `pond-cluster.png` chứa nước, đá bờ, cỏ bờ và lá sen trong một asset. Cây anh đào, đá ngoài hồ, bụi cỏ, hoa trắng và bụi cây là các PNG/prefab riêng. Bố cục hiện có 21 gốc đồ vật; cảnh mẫu và HUD bản logic dùng chung `MeadowMap.prefab`.

Unity mở prefab map ở Scene View 2D. Chọn gốc đồ vật, nhấn W để kéo, Cmd+S để lưu. Hướng dẫn và prompt nguồn nằm trong `ArtDirection/CartoonV2/README.md` và `grouped-ground-prompts.json`. Bảy PNG môi trường chiếm 8,467,261 byte (khoảng 8.08 MiB); backup source và scene trước thay đổi chiếm 26,558 byte, không sao chép Library.

## Bằng chứng đã chạy

- `modular-artifacts.json`: 12 đạt, 0 lỗi. Kiểm tra đường thuộc nền; sáu loại prefab rời; pond trọn cụm; góc PNG trong suốt; HUD dựng 21 đồ vật và 7 bóng từ prefab asset; lưu/mở lại vị trí; kéo cây không đổi đá/nền; bóng theo cây; reimport giữ bố cục.
- `modular-runtime.json`: sáu kiểm tra đạt cho rig người/thú, di chuyển, thú đi theo, đổi điều khiển và reset. Dùng virtual movement qua Unity API, không phải kiểm tra bàn phím vật lý.
- `modular-logic.json`: phiên chơi tải được, không có lỗi tải; HUD chứa 20 đồ vật foreground và một pond floor từ 21 gốc layout. Ảnh thực tế: `grouped-map-preview.png`, `grouped-map-logic-final.png`.
- `modular-preview-build.json`: WebGL Succeeded, 0 lỗi build, 50.65 giây. `modular-logic-build.json`: WebGL Succeeded, 0 lỗi build, 6.00 giây. Run-script LogicBuilder có cảnh báo CS0649 ở field JSON-deserialized `CheckEvidence.failed`; không phải lỗi build.
- HTTP ở `127.0.0.1:8788` và `127.0.0.1:8789` trả 200, index khớp bản build trên đĩa, bốn file loader/data/framework/wasm đều trả 200, không còn placeholder.
- `git diff --check` đạt. Các thay đổi có sẵn trong worktree được giữ; không commit hoặc push.

## Phạm vi và giới hạn

Không thay đổi domain, schema save hoặc luật chiến đấu. Đồ trang trí chưa có collision. Chỉnh map được lưu ở prefab dùng chung; bản web cần build lại sau những lần chỉnh tiếp theo. Phiên này không chạy lại toàn bộ domain test, không xác minh thao tác giao diện trình duyệt; kiểm chứng hiển thị và chuyển động trong Unity, build và phục vụ file web. Báo cáo domain mà LogicBuilder đóng gói là bằng chứng từ các lần chạy trước.

## Khôi phục

`before-modular-props.tgz` giữ source và hai scene trước khi tách môi trường. Ảnh cũ được giữ; không tự khôi phục archive đè lên những chỉnh sửa mới.
