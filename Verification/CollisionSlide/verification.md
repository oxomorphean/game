# Di chuyển men theo vật cản — 2026-10-08

Đã thay hitbox đá, gốc anh đào và hồ bằng đường bao lồi theo phần tiếp đất. Giữ hướng di chuyển thì bước đi men theo mép; lướt/đòn lao tiếp tục bị vật cản chặn. Bán kính chân và tốc độ nhân vật không đổi. Nền cỏ, hoa, bóng và tán cây không phát sinh collider.

## Đã kiểm chứng

- Compile trong Unity 6000.6.4f1: 0 lỗi, 0 cảnh báo.
- CombatTests: 78/78; gồm tốc độ, trượt theo bốn hướng, thả phím, quay ra khỏi tường, góc hẹp, tường mỏng, lướt, đường đánh, công pháp và rơi/nhặt.
- AdventureTests: 47/47, hồi quy gameplay V1 và nền nhiều người chơi hiện có.
- Footprint: 38/38 trên sáu vật cản thật, gồm giữ hướng quanh ba đá, hai cây, một hồ và trường hợp đá cạnh cổng Lôi Cốc. Mỗi prop chỉ còn một collider kế thừa từ prefab.
- Play Mode: 6/6. Input System giữ S đưa actor qua đá; toàn bộ mẫu vị trí ở ngoài footprint; renderer khớp state; animation chạy; thả phím dừng. Phiên kiểm tra dùng campaign RAM, khôi phục campaign trước khi Stop, không gọi lệnh ghi save.
- WebGL: build Succeeded, 0 lỗi; 5 cảnh báo từ Pipeline runtime bị tắt và TMP/shader/IL2CPP. Chi tiết ở build.json; dung lượng file thực tế ở build-files.json.
- Trình duyệt: mở build mới tại http://127.0.0.1:8791/, dùng Tiếp tục, giữ phím thật để đi từ trên đá cạnh cổng sang bên rồi xuống dưới; console không có error/warning trong lần kiểm tra. Screenshot web-verified.png là toàn bộ viewport sau khi vượt đá.
- Giữ nguyên transform/SpriteRenderer của ba prefab, source bóng hiện hành và toàn bộ phần ngoài phạm vi theo preservation.json. Không commit/push/deploy.

Test cũ kiểm tra vị trí tuyệt đối sau khi đè tường được đổi sang đo quãng đường theo tiếp tuyến trước đầu mút tường: hành vi mới chủ động đi dọc tường, nên fixture cũ đã đi vòng qua đầu mút một cách hợp lệ. Lần thất bại ban đầu được giữ trong domain-tests-first.json.

## Cấu hình và giới hạn

Mức hỗ trợ trượt tối thiểu 45% và các mốc UV footprint là thông số thử. Đây là hỗ trợ bám mép cục bộ, không phải tìm đường qua mê cung hoặc tự tìm cửa. Polygon là đường bao phần tiếp đất, không chặn toàn bộ ảnh tán lá/chiều cao đá. Chưa đo tải đồng thời 100–300 người hoặc kiểm thử điều khiển cảm ứng trên điện thoại.

## Chơi và khôi phục

Mở http://127.0.0.1:8791/, chọn Tiếp tục; tới đá cạnh cổng Lôi Cốc rồi giữ S. Đường xanh trong ảnh Editor chỉ là overlay QA tạm, không nằm trong build.

Mốc nguồn trước sửa: Recovery/collision-before-20261008, gồm source chưa được Git theo dõi, scene và prefab. Khi cần quay lại, chỉ khôi phục các file của mốc này liệt kê trong preservation.json và gỡ các file mới CombatFootprintBuilder.cs (+ .meta), AgentScripts/CollisionFootprints.cs và AgentScripts/CollisionRuntime.cs, rồi compile/build lại. Không reset toàn bộ worktree vì các phiên khác có thay đổi riêng.
