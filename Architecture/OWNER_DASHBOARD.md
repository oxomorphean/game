# Dashboard chủ dự án trong game

Yêu cầu 13/10/2026: nút **Quản trị** luôn hiện trong bản chơi mỹ thuật; mở bảng quản trị ngay trong cùng Unity instance. Bản đầu có Tổng quan, Bản đồ và Asset. Không khôi phục gameplay hoặc dashboard cũ.

## Phạm vi và dữ liệu

- Tổng quan: tổng asset mỹ thuật, các bố cục map, vật thể trên map hiện hành và số nội dung được bản chơi tham chiếu.
- Bản đồ: danh sách prefab có `ModularMapLayout`, kích thước, số đồ vật, footprint, đường mesh và các đồ vật thực trong map đang mở.
- Asset: tìm theo tên/đường dẫn, lọc nhóm và nội dung trong bản chơi, xem ID, đường dẫn nguồn Unity, loại, dung lượng file và kích thước texture.

Catalogue đếm mỗi file asset Unity một lần trong `Assets/_Project/Art`, `Content` và `Resources/Art`; không đếm `.meta`, mã nguồn, font, ảnh nguồn ngoài `Assets`, hay mỗi sprite con trong sprite sheet. Các bản mỹ thuật cũ còn trong thư mục được tính nhưng đánh dấu riêng khi không được scene build tham chiếu. Số lượt đặt đồ vật được trình bày riêng với số asset gốc.

`OwnerCatalogBuilder` đọc AssetDatabase, xuất metadata JSON vào Resources khi vào Play và trước build WebGL. Client đọc bản chụp đó; không cần đóng gói toàn bộ texture chỉ để đếm. Trạng thái “Trong bản chơi” dựa trên dependency của scene đang bật trong Build Settings và nội dung Resources/Art. Metadata là thông tin cấp dự án; vị trí người/vật thể trên map được đọc từ phiên đang mở.

## Tương tác và nghiệm thu

Nút Quản trị và F2 mở/đóng; Escape đóng. Khi mở bảng, dừng nhận lệnh đi/reset và hủy đích bấm sân cũ; hiệu ứng môi trường tiếp tục. Đóng bảng trả về đúng phiên và vị trí. Tìm kiếm không phát lệnh di chuyển. Bảng hiện chỉ quan sát, chưa có thao tác sửa/xóa asset hoặc lưu bố cục.

Kiểm tra catalogue không trùng ID/path, tổng và nhóm khớp file thật, map khớp prefab; kiểm tra trong Play và WebGL việc mở/đóng, tìm kiếm không có kết quả, lọc, cuộn và chặn input đi lại. Xem bố cục ở cửa sổ ngang rộng và hẹp. Các scene/prefab mỹ thuật hiện có không bị dựng lại để thêm bảng.

Đây là công cụ local dành cho chủ dự án, hiển thị theo yêu cầu hiện tại; chưa phải hệ thống tài khoản/quyền quản trị online. Có thể bỏ các file dashboard và lời gọi tạo trong `ArtPreviewHud` để khôi phục HUD trước đó, không có migration hoặc thay đổi save.
