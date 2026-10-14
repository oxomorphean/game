# Thanh thời gian tương tác — 13/10/2026

Thanh giờ trong scene `ArtPreview` dùng uGUI Slider, chọn liên tục từ 06:00 đến 18:00. Bấm hoặc kéo giữ tại giờ đã chọn; Chạy tiếp tục từ giờ đó, Dừng giữ lại để xem bóng/nắng. Thanh giờ có GraphicRaycaster và dùng EventSystem/InputSystemUIInputModule hiện có. Không tạo đồng hồ gameplay hoặc ghi save.

`DaylightPresentation` sở hữu giờ và nút Chạy/Dừng; `DaylightSkyGraphic` giữ vùng kéo; `ArtPreviewHost` chặn input và hủy đích đi khi thao tác trên thanh. Giờ tự chạy chỉ cập nhật slider bằng `SetValueWithoutNotify` để tránh tự kích hoạt thao tác chọn giờ.

Đã biên dịch C# 0 lỗi/0 cảnh báo; [6 kiểm tra runtime](runtime-checks.json) đạt về giới hạn giờ, slider giữ 11:30, resume không đổi giờ và khóa input tới hết frame thả chuột. Đã bấm/kéo giữa các giờ và dùng Chạy/Dừng trong Editor.

Trên WebGL đã kéo vượt mép phải về [18:00](web-1800.jpg), vượt mép trái về [06:00](web-0600.jpg), bấm chọn 11:30 rồi Chạy tiếp tục từ 11:31. Nhân vật giữ nguyên vị trí trong thao tác thanh; nắng và bóng đổi ngay. Build cuối [0 lỗi, 2 cảnh báo có sẵn](../TreeGrounding/build.json). [Kết quả sửa cây](../TreeGrounding/README.md).

[Bằng chứng browser](browser-check.json) ghi thao tác, thời điểm kiểm tra, token asset và console trống. Các capture nằm ngoài `Assets`.
