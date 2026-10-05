# Kiểm chứng khu vườn v7 — 05/10/2026

Phạm vi: bổ sung chi tiết và hiệu ứng cho sân mẫu; sau phản hồi người dùng, thay cỏ có nét vụn bằng mặt xanh phẳng và đúng một mảng tối nhẹ gần hồ. Cổng, hai cây chi tiết và bóng cây, hồ, đá, bộ Huyền Giáp Quy đen–vàng và controller giữ nguyên. Chưa mở rộng map hoặc thêm gameplay.

## Bằng chứng đã chạy

- Unity Pipeline/CLI trong Editor 6000.6.4f1 lưu scene và metadata; builder compile không có diagnostics.
- `scene-verification-v7.json`: 384 tile, 5 loại nền; 10 texture mới dùng PPU16/Point/không mipmap/không nén; Pixel Perfect Camera384×256.
- `protected-verification-v7.json`: 9 file gốc khớp blob baseline v6 `6b1417a`, gồm cổng, hai cây, đá, hồ, ba sheet rùa và controller.
- Validator cỏ phẳng/mảng tối PASS tại `clean-grass-*-verification-v7.json`: palette đúng, alpha0/255, kích thước đúng. Palettev2 giữ 33 màu v1, thêm đúng #79AA5E. Tile xanh dùng một màu #83B865, không có nét vụn hoặc dither. Các validator nguồn trong GardenV7 ghi rõ cả mẫu thử không dùng.
- `aesthetic-review-v7.md`: reviewer độc lập GPT-6.1 Sol max đối chiếu ảnh người dùng và ảnh Unity cuối, PASS riêng phần cỏ sau khi bỏ mảng tối rời bên trái. Ảnh này chưa được người dùng duyệt cuối.
- `ambient-verification-v7.json`: chạy Play Mode, 14 cánh hoa đổi vị trí, dùng đủ 4 frame nước, không thiếu sprite/lệch grid; tắt/bật giữ đúng pool17 renderer.
- `build-result-v7.json`: BuildWeb thực sự Succeeded, không lỗi. WebBuild là output bỏ qua bởi Git.
- `web-verification-v7.json`: bản web cuối với debugSymbolMode Off mở được ở localhost8788, loading bị gỡ và reset enabled; không có console error/warning kể từ mốc tải mới. Đã xem pet đi tới vị trí bấm và trở về bằng phímR/thao tác nút trực tiếp; ảnh pointer/final nằm cùng thư mục.

## Giới hạn

Chưa đo FPS, mức RAM, hiệu năng điện thoại hoặc trình duyệt khác. Hình lên/xuống của rùa vẫn dùng bộ bước chân hiện có. Mật độ nét trên gạch có thể tinh chỉnh ở một lượt khác; phản hồi hiện tại chỉ yêu cầu sửa cỏ.

Lúc kiểm tra web có stack overflow khởi động; đã phân lập bằng tắt riêng hiệu ứng, clean build và xuất symbol rồi trở lại Off. Bản cuối chạy với wasm có cùng hash; chưa xác định được nguyên nhân ban đầu, không kết luận symbol mode đã sửa lỗi hay runtime hiệu ứng có đệ quy. Console của công cụ giữ lịch sử qua reload, nên kiểm chứng cuối chỉ tính log từ lượt tải mới và đợi loading được gỡ. Tất cả cấu hình chẩn đoán tạm được khôi phục; ProjectSettings/Packages không có diff.

Scene có ID/whitespace do Unity sinh tự động; không sửa YAML bằng tay. Diff text ngoài scene được kiểm tra whitespace. Nguyên tắc cho các lần sau: nền cỏ được reviewer thẩm mỹ xem ở kích thước chơi, vì validator palette không chứng minh bố cục đẹp.
