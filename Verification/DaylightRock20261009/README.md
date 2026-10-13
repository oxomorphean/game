# Bóng đá và quỹ đạo mặt trời — 09/10/2026

Theo yêu cầu chủ dự án: mặt trời ẩn mọc bên trái, lên cao lúc trưa rồi lặn bên phải. Bóng sáng ngả phải, bóng chiều ngả trái. Thanh giờ và ánh vàng cuối chiều giữ nguyên.

Bóng đá từng bị ép mỏng bởi cùng độ sâu chiếu dùng cho cây. Shader và Canvas nay dùng độ sâu 0.8 cho đá khi mặt trời thấp, vẫn nội suy về footprint 0.45 và giữ nguyên điểm chân gần trưa. Không thêm elip, không đổi sprite, vị trí map, collider hoặc luật gameplay.

`ArtPresentationChecks.DaylightBefore/DaylightAfter` render hòn đá thật trong bản đồ chính tại 08:00, 11:30, 12:00 và 16:00. Đếm pixel thay đổi khi bật/tắt riêng bóng; diện tích đá nhìn thấy được đo bằng cách bật/tắt sprite, tránh tính vùng alpha rỗng của PNG. Bản trước có 9 kiểm tra không đạt; bản sau đạt 26/26, gồm hướng bóng, hình khối, sát chân, footprint trưa, không elip, ánh hoàng hôn và mesh Canvas không tự chồng lớp. Chiều cao phần bóng nhìn thấy tăng từ khoảng 0.146 lên 0.358 lần chiều cao khung sprite ở sáng/chiều; gần trưa vẫn giữ footprint cũ.

Ảnh đối chiếu nằm trong thư mục này; bản nguồn trước sửa là `before-source.tgz`. `scene-prefab-before.sha256` xác nhận scene chính và hai prefab map không thay đổi. WebGL chính build thành công, 0 lỗi, 5 cảnh báo có sẵn về Pipeline và TextMeshPro; báo cáo ở `ArtDirection/CartoonV2/MainMapMethod2/build.json`. Bản xem dùng cổng localhost 8794, đã tải lại, mở phiên chơi và kiểm tra console browser không có lỗi; ảnh tab hiện tại là `web-preview.jpg`. Console Unity cũng không có lỗi compile hoặc runtime.
