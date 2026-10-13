# Kiểm chứng cartoon — 06/10/2026

Unity 6000.6.4f1 arm64, 2D Animation 16.0.1 từ Unity Registry https://packages.unity.com. Biên dịch 0 lỗi/0 cảnh báo. Hai build cuối thành công, mỗi build 0 lỗi; lệnh run_script chuẩn dùng timeout_ms=180000. Lần gọi build đầu bằng eval hết thời gian chờ RPC 5 giây dù Unity vẫn build tiếp; build cuối đã chạy lại bằng run_script và không còn lỗi đó.

8 kiểm tra runtime/gỡ bỏ đạt trong runtime-checks.json. SpriteSkin có 6 bone và 475 vertex được biến dạng; thời gian tăng từ 19,14 tới 69,85 giây, vị trí x tăng từ 0 tới 5,9, góc đầu thay đổi. Logic scene tải phiên thành công, có 3 actor dùng mesh motion Canvas; xem logic-smoke.json. Domain và save schema không sửa; không chạy lại toàn bộ 155 test logic cũ cho thay đổi mỹ thuật này.

Browser mẫu tại cổng 8788 tải xong, thấy nền và rùa, bấm sân làm rùa đi từ dưới tới giữa; console browser không có error/warning tại thời điểm kiểm tra. Bản logic ở 8789 đã build lại để thay asset cũ. Ảnh Unity nằm ở unity-preview.png và logic-preview.png.

Aseprite.app, thư mục MCP và đăng ký mcp_servers.aseprite đã gỡ; không xóa Python/uv/Ninja dùng chung. Assets/ArtSample và GardenV8 đã gỡ bằng AssetDatabase. Nguồn pixel cũ chỉ còn trong bản nén trước thay đổi 7,7 MiB và lịch sử Git/kiểm chứng cũ. Không commit/push/deploy.

storage.json ghi dung lượng filesystem: animation 54,6 MiB, 2D Common 3,8 MiB, asset mới 3,3 MiB, mỗi web build khoảng 53 MiB. Đây không phải RAM runtime hoặc benchmark so sánh FPS. Nền là ảnh phẳng để thử phong cách, thú chỉ có hình trái/phải; chưa có chứng minh hiệu năng thế giới online đông người.
