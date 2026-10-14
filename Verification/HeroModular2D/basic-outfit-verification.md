# Sửa dáng và đồ cơ bản — 09/10/2026

Yêu cầu: sửa dáng cơ thể mẫu nhìn trước, thay cách hiển thị đồ lót bằng áo/quần đơn giản kín đáo và giữ khả năng thay trang phục riêng.

Nguồn mới: `Assets/_Project/Art/HeroModular2D/body-basic-front.png`, 1254 × 1254 RGBA, alpha 0–255. ImageGen tích hợp tạo từ atlas thân cũ và turnaround đã duyệt; prompt ở `ArtDirection/HeroModular2D/prompt-basic-outfit.txt`. Unity slice sáu mảnh thân và sáu mảnh đồ cơ bản. Không chỉnh ảnh bằng Python; Pillow chỉ đọc bounds, alpha, kích thước và hash.

Mẫu có 25 mảnh, vẫn dùng 14 xương. Thân/tay/chân đổi tỷ lệ và tay vẽ sau thân để che đường nối vai. Áo trắng ngà, hai tay áo, eo quần và hai ống quần ngắn là lớp `BasicClothes` riêng. Bộ cơ bản hiện mặc định, đổi sang bộ ngoài ẩn toàn bộ lớp cơ bản; cơ thể nền không thay ảnh hoặc reset pose. Các enum cũ giữ giá trị bằng cách thêm nhóm mới vào cuối.

`HeroDirectionRuntime.Modular`: 23 đạt / 0 lỗi trong `runtime.json`. Kiểm tra vertex thật của thân và quần ngắn, hai bộ đồ, đổi tóc/áo khi chạy, giữ pose, tay/kiếm chém, input, scene button serialization và camera trong nền. Hai lượt kiểm tra đầu truy vấn buffer của mảnh ẩn hoặc ngay sau khi bật đồ; kiểm tra cuối chờ frame native và chỉ yêu cầu buffer ở mảnh đang hiển thị. Không đổi animation để né kiểm tra.

Builder tạo/import bằng Unity API. WebGL build thành công trong 63.08 giây, 0 lỗi, 5 cảnh báo. Cảnh báo gồm Pipeline không có cấu hình runtime Player, pragma cũ của shader TextMeshPro và ba thông báo IL2CPP tách file C++ của TextMeshPro; chi tiết ở `basic-build-warnings.json`. Unity báo compilationFailed=false và consoleErrors=0 sau build.

Trình duyệt thật ở `http://127.0.0.1:8795/`: kiểm tra bộ cơ bản mặc định, Chạy, đổi tóc/áo ngoài khi chạy, trở lại đồ cơ bản, Chém, mức zoom chơi và zoom gần. `web-console.json` không có error/warn. Ảnh `web-basic-idle`, `web-basic-run`, `web-basic-swap`, `web-basic-strike`, `web-basic-normal`, `web-basic-final` ghi các trạng thái đã xem. Mẫu bàn giao đứng, tóc 1, đồ cơ bản, zoom gần.

Hash bốn scene/prefab chính so với `basic-protected-before.sha256` đều khớp. Editor trả về `CultivationV1` ở trạng thái Stop. Không đổi Domain, save, player chính hoặc bản 8794. Đây là mẫu nhìn trước, chưa chứng minh tám hướng artwork hoặc chất lượng mọi góc combat. Backup trước sửa ở `Recovery/HeroModular2D-20261009/basic-outfit-before/`.
