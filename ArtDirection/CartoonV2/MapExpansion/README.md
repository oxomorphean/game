# Mẫu nền đồng cỏ phía bắc — 08/10/2026

Theo yêu cầu chủ dự án, lượt này tạo một nền mới để đánh giá trước khi tạo bảy nền còn lại. Meadow giữ cùng góc nhìn, bảng màu và tỷ lệ nền đang dùng; đường mới đi vào cạnh dưới phía phải rồi uốn sang trái ở phía trên. Chỉ có cỏ, đường đất và đá lát cố định trong nền; không có anh đào, hoa, bụi cây, hồ hoặc vật thể lớn.

`meadow-north-01.png` là ảnh ImageGen nguyên bản 1672×941, cùng kích thước nền hiện tại. `current-ground-reference.png` là bản sao byte-identical của ground-road.png (SHA-256 trong generation.json). `index.html` đặt hai PNG cạnh trên/dưới để duyệt sự tiếp nối, phủ một dải hẹp từ mép nền gốc và làm mờ dần trên nền bắc để hòa màu cỏ. Lớp hòa mép thuộc bản xem, chưa nằm trong PNG mới; bản xem không sinh lại hay thay thế nền hiện tại.

Đây là mẫu ảnh, chưa nhập vào Unity, chưa tạo collider hoặc thay catalogue, scene, prefab, biên di chuyển hay dữ liệu lưu. Khi duyệt mẫu, Logic tiếp tục sở hữu kết nối vùng và giới hạn chuyển động. Khi tích hợp cần giữ lớp hòa mép tương ứng và kiểm tra ở đúng tỷ lệ Unity. Prompt cuối và nguồn ảnh được lưu trong generation.json; các bản ghép AI không giữ đúng nền gốc đã loại khỏi đầu ra.

Đã xem bản ghép trực tiếp trên localhost: hai PNG đều tải đúng 1672×941, console không có error/warn. Ảnh bản xem ở preview-connected.png, dữ liệu kiểm tra ở preview-checks.json. Chưa chạy compile/build vì không sửa source hoặc asset chạy trong Unity.
