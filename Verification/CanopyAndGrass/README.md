# Tán anh đào và nền cỏ — 13/10/2026

Phạm vi: bỏ vệt bóng ngang do rễ anh đào, giữ hình các cụm hoa trong bóng tán và làm dịu mảng màu cỏ nền chính. Giữ vị trí map/đồ vật, footprint đã chỉnh và gameplay.

## Thay đổi

Bóng trước đó dùng toàn sprite cây, gồm rễ sát đất, và suy ra tiết diện tròn theo bề rộng khiến silhouette tán bị biến dạng. Profile tán hiện chọn hoa hồng khỏi gỗ nâu/viền rễ tối theo màu nguồn, chiếu contour tán bằng phép biến đổi affine; khối thân hẹp nối tán về chân trong cùng `SunShadowMesh`. Tất cả vẫn dùng giờ, hướng mặt trời và pass/stencil chung; không thêm bóng ellipse.

`SunShadowSettings.canopyBlueRatio=.65` dành cho palette anh đào hiện hành. Đây là mô phỏng 2.5D: đổi palette hoặc loại cây cần chỉnh profile và xem ảnh lại. Nó không thay thế mask/hình học chiều sâu do artist author cho các asset mới.

[Agent nền cỏ](../GrassPolish/README.md) giảm tương phản texture, trộn hai góc/tỷ lệ lấy mẫu và thêm biến thiên màu rộng. Nền cỏ và mép đường dùng cùng hàm; không sinh bitmap mới hoặc thêm nét vụn.

## Kiểm chứng

- Compile C# tích hợp: 0 lỗi, 0 cảnh báo; shader terrain không có lỗi.
- [69 kiểm tra bóng](../UnifiedShadows/checks.json) đạt. Các kiểm tra riêng dùng render GPU để xác nhận pixel gỗ nâu và viền rễ tối bị loại khỏi tán, còn khối thân vẫn đổ bóng. Có 49 mốc giờ 06:00–18:00, hợp lớp bóng và lật hướng.
- [14 kiểm tra footprint cây](../TreeGrounding/collision-checks.json) đạt; dữ liệu va chạm giữ nguyên trong lượt này.
- [Bốn render cây thật](../TreeGrounding/shadow-checks.json): đoạn ngang ở đáy 6 px tại 08:00, 11:30, 12:00 và 16:00; vùng bóng chính nối chân với sai số tối đa .018 đơn vị. Số đo được đối chiếu với hình thực tế.
- Đã xem scene chính ở [08:00](editor-0800.jpg), [11:30](editor-1130.jpg) và [16:00](editor-1600.jpg). Nền dịu, không còn mảng phản chiếu nổi rõ; bóng giữ các cụm tán và đổi phía sáng/chiều. Agent cỏ đã review ảnh 11:30 và chốt shader hiện hành.

- [Build WebGL](build.json) lúc 22:16 đạt `Succeeded`, 0 lỗi, 5 cảnh báo có sẵn của Pipeline/TextMeshPro, 105,649,135 byte.
- Đã tải lại [bản local](http://127.0.0.1:8791/) đúng token `293c69539f424d03b36c382efc80ef3f`, xem và giữ thanh giờ gần 11:30 (ảnh hiển thị 11:26). [Ảnh WebGL](web-1130.jpg) cho thấy nền cỏ dịu và bóng tán không còn vệt ngang ở rễ. [Console trình duyệt](browser-check.json): 0 lỗi, 0 cảnh báo.
