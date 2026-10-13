# Animation, bóng đổ và ánh sáng ban ngày — 07/10/2026

Người có idle thở nhẹ, bước chân luân phiên, lấy đà/chém và gục/trở lại đứng trên rig sáu bone hiện hành. Map dùng bóng silhouette lấy alpha của sprite cây, đá, cỏ, hoa, bụi cây và nhân vật. Cây anh đào không còn bóng elip dưới gốc. Mặt trời di chuyển, bóng đổi hướng/độ dài và chiều có ánh vàng cùng tia nắng; thời gian hình ảnh dừng tại 18h, chưa có buổi tối.

## Phạm vi và bàn giao

Các thay đổi nằm ở rig, renderer, shader, material và prefab mỹ thuật. `SetMotion` và `SetCombatPose` giữ interface hiện có. `DaylightPresentation` là đồng hồ duyệt hình ảnh 6h–18h, không phải thời gian gameplay, không đọc/ghi save. Logic tiếp tục sở hữu vật cản, spawn, cooldown, damage và mọi luật thời gian. Tài liệu gameplay đang được phiên khác cập nhật đồng thời, các thay đổi đó được giữ nguyên.

`ArtPresentationBuilder.Apply()` chuẩn bị hai material trong `Resources/Art`, gán sprite nguồn cho bóng và xóa riêng bóng elip của anh đào qua Unity Editor API. `ModularMapBuilder.PrepareAssets()` gọi bước này sau khi có map. `DaylightCanvasEffect` đưa cùng bóng và màu nắng vào map Canvas. Đây là mô phỏng ánh sáng 2D; không phải bóng từ mô hình 3D. Nhân vật vẫn dùng bitmap trái/phải, chưa có bộ nhìn lên/xuống riêng.

## Bằng chứng

Lượt chốt sau khi xóa elip: 17 kiểm tra mỹ thuật đạt, 0 thất bại; Play Mode có 2 cây và 0 bóng elip. Build combat cuối thành công, 0 lỗi và 5 warning Pipeline/TextMeshPro. Browser tải bản build này, console không có warning/error. Unity console hiện hành có 0 lỗi/0 warning; bộ đếm lịch sử còn 5 warning của build. 28 PNG nguồn cùng 4 file save native giữ nguyên checksum.

Xóa trực tiếp child `Bóng tiếp xúc` trong `cherry-tree.prefab` và scene `PlayerCombatTrial.unity` qua Editor API, kiểm tra lại `MeadowMap.prefab`. Chỉ bóng silhouette của cây được giữ. `tree-ellipse-removal.json` ghi kết quả prefab; `daylight-runtime.json` ghi số elip trong cảnh chạy bằng 0. Preview combat đang mở đã được tải lại.

- `checks.json`: kiểm tra rig người, bước chân, pose combat, bóng cây không có elip và bản Canvas. Chạy bằng `ArtPresentationChecks.After()` trong Play Mode.
- `daylight-runtime.json`: số bóng dùng đúng sprite nguồn và các mốc sáng/trưa/hoàng hôn. Ảnh sáng/trưa được chụp trước lượt xóa elip cuối; `daylight-sunset.png` và `web-daylight-preview.png` là kết quả sau khi xóa.
- `runtime-console.json`, `browser-console.json`: Console Unity/browser ở lượt duyệt cuối.
- `combat-build.json`: bản preview cuối sau xóa elip. `art-build.json` và `logic-build.json` là hai build ánh sáng đã đạt trước lượt xóa trực tiếp cuối. Warning Pipeline/TextMeshPro được ghi trong báo cáo build.
- `web-assets.json`: phản hồi HTTP và checksum bản combat cuối được server localhost phục vụ.
- `human-animation.gif`: 89 frame ghi từ rig thật. Đoạn chạy dùng controller của scene art; các pose combat trong clip được cấp qua interface trình bày để duyệt hình.

Smoke test bàn phím qua Unity Editor không đạt: key D nhận trạng thái nhấn nhưng InputAction trả axis 0. Chưa xác minh nguyên nhân và không sửa input/logic để ép test đạt. Browser đã hiển thị game; việc gửi phím D ngắn không đủ bằng chứng kết luận tốc độ/chuyển động. Kết quả domain combat trong thư mục này thuộc lượt kiểm tra trước khi phiên Logic cập nhật mốc gameplay mới.

## Khôi phục

Checkpoint đầu task được ghi trong `checkpoint.txt`; `presentation-before.tgz` chứa source, prefab và tài liệu trước thay đổi. Nếu cần quay lại, chỉ khôi phục các file mỹ thuật liên quan, không giải nén ghi đè toàn bộ vì có công việc của phiên khác. Không commit, push hoặc publish trong task này.

![Animation người](/Users/nguyenhoangdacdat/dev/nguthu-unity-demo/Verification/ArtMotionGrounding/human-animation.gif)

![Hoàng hôn](/Users/nguyenhoangdacdat/dev/nguthu-unity-demo/Verification/ArtMotionGrounding/daylight-sunset.png)

![Preview web sau xóa elip](/Users/nguyenhoangdacdat/dev/nguthu-unity-demo/Verification/ArtMotionGrounding/web-daylight-preview.png)
