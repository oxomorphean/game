# Kiểm chứng nhân vật tám hướng

Hoàn tất kiểm tra WebGL ngày 09/10/2026. Source, import, compile, tests và build chạy ngày 08/10/2026; sau đó chỉ thêm bằng chứng và thử Blender MCP trên bản sao, không đổi gameplay hoặc sprite đã build.

## Kết quả đã chạy

| Kiểm tra | Kết quả | Bằng chứng |
| --- | --- | --- |
| HeroDirectionChecks | 47 đạt, 0 lỗi | checks.json |
| HeroDirectionRuntime, Play Mode/input thật | 31 đạt, 0 lỗi | runtime.json, editor-direction-0..7.png, editor-attack.png |
| CombatTests, gồm M01–M03 và rơi/nhặt | 78 đạt, 0 lỗi | combat-tests.json |
| AdventureTests | 47 đạt, 0 lỗi | adventure-tests.json |
| CollisionRuntime, giữ S men qua đá | 6 đạt, 0 lỗi | collision-runtime.json; ../CollisionSlide/runtime-tests.json |
| Compile | Hoàn tất, không lỗi C# | Unity recompile_status và build |
| WebGL | Succeeded, 0 error, 5 warning | build.json |
| WebGL thực tế, 1280×720 | Load bản lưu V1; đi xuống/lên/phải/chéo; giữ hướng sau thả phím; J đánh thường, K Hoành Tảo có hao linh lực và cooldown | web-front/back/diagonal/attack/sweep.png |
| Console trình duyệt trong phiên thử | 0 error/warning | web-console.json |

Tổng 209 assertion đạt. Năm warning build gồm Pipeline runtime chưa cấu hình (plugin Editor không chạy trong Player), pragma debug TextMeshPro và ba thông báo IL2CPP tách C++ lớn của TextMeshPro. Không phải lỗi build; không tắt warning để lấy kết quả xanh.

Bản web phục vụ bằng `python3 serve_adventure.py` tại http://127.0.0.1:8791/. Tab cũ từng bị connection refused khi server dừng; sau khởi động, tab thử mới load thành công. Dùng Tiếp tục khám phá, không tạo nhân vật mới hoặc gửi lệnh lưu. Kiểm tra Play Mode dùng campaign RAM tạm và trả lại campaign trước đó; không sửa save người chơi.

## Phạm vi xác minh

Tám hướng chạy/idle, frame tiến, hướng aim khi đánh, đỉnh pose theo windup, sweep, dash, hurt, gục/hồi sinh, pivot chân, sorting và bóng đều có kiểm tra trên state/renderer thật. Cả 416 render nguồn có alpha hữu ích và không chạm mép; sprite nhập qua API Unity. Hai scene chỉ thêm tham chiếu directionalHero (xem các file scene.diff); không chỉnh map, footprint hoặc asset Art khác.

Trên browser đã kiểm tra đi và đánh ở trại; gục/hồi sinh, lướt và đủ tám hướng có bằng chứng Unity/tests, chưa lặp hết trên trình duyệt. Chưa đo thiết bị yếu/mobile hoặc multiplayer. Mỹ thuật là mẫu model chibi/render sprite để chủ dự án duyệt, không coi là asset cuối; chuyển tám góc, không xoay liên tục 360 độ.

## Khôi phục

Source trước thay đổi kể cả untracked nằm ở Recovery/Hero8Directions-20261008/source-before.tgz. Giữ hero.png và rig cũ. Muốn quay lại hình trước: xóa tham chiếu directionalHero trong Inspector của CombatWorldView ở hai scene, rồi build lại; không cần đổi Domain/save. Không bung archive đè thay đổi các phiên khác.
