# Phạm vi mẫu cartoon

Goal: gỡ asset pixel và Aseprite/MCP được người dùng yêu cầu; cài Unity 2D Animation chính thức; mở một scene mẫu rùa chuyển động trên nền mới và xuất web chơi được.

Invariants: giữ nguyên Domain, tiến trình và save; scene được chỉnh bằng Unity API; gói lấy từ packages.unity.com; bản nén trước thay đổi dùng để quay lại. Không thêm server, gameplay hoặc công cụ trả phí.

Acceptance: package 16.0.1/source Registry; ứng dụng/MCP cũ đã gỡ; hai ảnh mới có trong dự án; SpriteSkin hợp lệ và thời gian/di chuyển thực sự tăng; build web thành công; browser mở được; dung lượng đo trên filesystem. Nền là một ảnh phẳng cho mẫu thử, hướng thú tạm trái/phải.

Rollback: giải nén pixel-assets-before.tgz tại gốc dự án, phục hồi các source đã lưu; gói có thể Remove qua Unity Package Manager. Aseprite không nằm trong bản sao và cần cài lại nếu quay về công cụ đó.
