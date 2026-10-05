# Hiệu ứng khu vườn v7

`Assets/ArtSample/Scripts/LivingGarden.cs` giữ sẵn 14 SpriteRenderer cánh hoa và 3 SpriteRenderer gợn nước. Pool được tạo một lần trong Start; Update dùng lại đối tượng, không LINQ hay cấp phát tham chiếu mới. Cánh hoa rơi 9–13 giây, gợn nước dùng 4 frame ở 5 frame/giây. Vị trí snap 1/16 world unit; giữ rotation, scale và màu sprite.

Builder gán hai gốc cây (-7,0.75), (7,1.25) và ba tâm gợn nước (6.25,-4.25), (7.75,-4.625), (8.75,-4.25). Mặc định của component có thể khác; scene được builder gán tường minh. Pool mặc định có 17 renderer; khoảng cấu hình cho phép 12–16 cánh hoa, giữ 3 gợn nước.

Tham chiếu thiếu hoặc cấu hình không hợp lệ tắt component và báo lỗi trước khi dựng pool. Tắt component ẩn các renderer và dừng đồng hồ hiệu ứng; bật lại dùng đúng pool cũ.

`AgentScripts/GardenRuntimeProbe.cs` kiểm tra Play Mode trong cửa sổ lấy mẫu 1.5 giây; tạm bật runInBackground để Unity vẫn cập nhật khi tool đang chạy, sau đó khôi phục giá trị cũ. Bằng chứng chạy thật ở `ambient-verification-v7.json`: 14 cánh hoa đổi vị trí, đủ 4 frame nước, không thiếu sprite, không lệch grid, tắt/bật giữ 17 renderer. Đây không phải phép đo FPS hay hiệu năng thiết bị người chơi.
