# Mỹ thuật Linh Thú Ký

Đọc [STYLE_GUIDE.md](STYLE_GUIDE.md) và ảnh trong `References/` trước mọi công việc mỹ thuật. Đó là chuẩn hiện hành.

Scene mẫu: `Assets/ArtSample/SpiritFoxGarden.unity` — linh hồ chạy sáu frame, cổng, hai asset cây anh đào, đá và hồ nước. Sân Tilemap nhỏ có cỏ/rêu xen gạch; bóng cây tách riêng để bố trí được trong Unity.

Ảnh gốc do ImageGen tạo mới nằm trong `Sources/`; prompt nằm trong `ArtSample/generation-prompts.json` và `ArtSample/generation-prompts-v3.json`. Ảnh chụp, kết quả build và ghi chú kiểm chứng nằm trong `ArtSample/`.

Unity chuẩn hóa ảnh nguồn về lưới sprite dùng trong game. Sáu frame chạy có canvas và pivot cố định. Giảm kích thước tự động chưa thay thế chỉnh pixel thủ công và duyệt animation cho bộ asset sản xuất.
