# Hai giao diện riêng: người chơi và chủ dự án

Quyết định của chủ sản phẩm ngày 05/10/2026: dashboard là công cụ để hiểu, điều chỉnh và theo dõi dự án trong suốt quá trình phát triển. Dashboard không phải giao diện người chơi.

## Màn chơi

- Map là vùng chính; không có sidebar quản trị thường trực.
- Góc trên trái: người/thú đang điều khiển, sinh lực, tu vi, công/thủ/căn cốt/tốc độ. Mỗi thú có thanh máu trong dãy khế ước; người, thú đồng hành và thú hoang có máu trên đầu. Góc trên phải: vùng hiện tại và thông tin định hướng.
- Nút hành động chính lớn ở dưới phải, đổi nhãn theo đối tượng gần. Linh thú bị thương/có hảo cảm mở bảng thu phục; địch sát bên ưu tiên đánh; đồ rơi ở gần thì nhặt, kế tiếp mới đến điểm tài nguyên; đứng gần cổng thì chuyển vùng; mang chiến lợi phẩm về trại thì bảo toàn.
- Dùng Huyết Khế phải bấm trong bảng thu phục; đứng gần không tự tiêu vật phẩm. Nút luôn báo hành động sắp thực hiện.
- Cạnh nút chính có kỹ năng và Thần Đình. Linh thú và túi đồ mở bảng khi cần, không che map thường trực. Nút Đóng và Escape đóng bảng và thoát Thần Đình nếu đang ở bên trong; G vào/ra Thần Đình. Nút Đi trong Thần Đình là hành động riêng để ẩn bảng mà vẫn điều khiển thần hồn. Tab đổi đối tượng ngoài Thần Đình, giữ vị trí các actor.
- WASD/phím mũi tên di chuyển; J, Space hoặc E dùng hành động chính; K kỹ năng. Có cụm hướng bấm/giữ trên màn hình.
- Nút Thả đồ mở Thần Đình ở mọi nơi được phép nhập. Chọn ô vật phẩm để hiện số lượng, ±1, Chọn 5, Cả stack và nút Thả. Đồ xuất hiện cạnh thân thể ngoài map; nút chính chuyển thành Nhặt khi ở gần. Ô đất tiếp tục hiện công cụ trồng trọt riêng.
- UI trồng trọt, bồi dưỡng, chế tạo và thu phục chỉ chứa thao tác người chơi. Không có cấp đồ, tua giờ, chỉnh tỷ lệ, thử cạn máu hay mở cảnh giới bằng quyền quản lý.
- Tham khảo bố cục HUD/chức năng nút của Soul Knight theo yêu cầu người dùng. Thiết kế riêng, giữ bảng ấm, viền navy và font tiếng Việt; bản này dùng lại asset sân vườn, chưa phải mỹ thuật cuối.

## Xưởng dự án

Trang Tổng quan giải thích vòng chơi, từng hệ thống đã có, giới hạn hiện tại và số ca kiểm thử của bản build. Các trang Thần Đình, Linh thú, Vật phẩm & nghề, Luật & dữ liệu, Nhật ký giúp chủ dự án quan sát và thay đổi dữ liệu thực của phiên thử.

Xưởng có cấp đồ, tăng cảnh giới, tua tăng trưởng, mô phỏng cạn máu, sửa tham số có bước Áp dụng và xem bản lưu. Trang Luật & dữ liệu có hai nhóm: Bồi dưỡng và Di chuyển/thả đồ; hiển thị giới hạn và số stack đang trên đất. Mô phỏng tạm dừng khi mở xưởng; tác vụ thử nghiệm chỉ chạy khi chủ dự án bấm. Quay lại chơi tiếp đúng phiên, không tạo bản sao tiến trình.

## Điều hướng và giới hạn

HUD được tạo bằng Canvas/uGUI với TextMeshPro và font Noto Sans nhúng; xưởng giữ IMGUI riêng. Web dùng cùng một Unity instance, hai màn riêng qua nút trên thanh công cụ: `#choi` và `#dashboard`. Trong Editor dùng F2 chuyển giữa hai màn. Đây là tách giao diện và vai trò sử dụng ở bản local, chưa phải tách quyền truy cập bằng server. Bản online phải loại công cụ owner khỏi client công khai, xác thực/quản quyền dashboard và chỉ cho server thực hiện thay đổi dữ liệu.

Khi thay đổi logic hay trạng thái kiểm thử, cập nhật trang Tổng quan/manifest cùng code. Không ghi hệ thống online là hoàn thành trước khi triển khai và đo tải.
