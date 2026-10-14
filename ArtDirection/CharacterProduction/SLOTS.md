# 3. Quy tắc lắp ghép và phụ kiện

Quy tắc dưới đây áp dụng cho góc 3/4 hướng phải. Góc khác cần bảng thứ tự tương ứng; phản chiếu không được đổi thứ tự tay trước/sau hoặc vị trí grip tùy tiện. ID mẫu cơ thể và góc phải trùng trước khi cho phép lắp.

| Slot | Neo và bố cục | Che khuất / biến dạng |
| --- | --- | --- |
| Back_Prop | Neo ngực hoặc hông được chỉ định | Sau thân; cánh/áo choàng cần bone riêng nếu có chuyển động |
| Hair_Back | Cùng hộp sọ, neo đầu | Sau cổ/vai; vẽ kín chân tóc, kiểm tra áo cổ cao |
| Arm_Back / Sleeve_Back | Vai sau, khuỷu, cổ tay sau | Tay áo che cánh tay theo mask; cần giữ bàn tay tách được |
| Leg_Back / Boot_Back | Hông, gối, cổ chân sau | Quần nằm dưới tà áo, giày chồng cổ chân; không scale theo crop |
| Leg_Front / Boot_Front | Hông, gối, cổ chân trước | Tỷ lệ phối cảnh bám mẫu cơ thể; không kéo chân khi đổi giày |
| Torso / Coat | Neo ngực và hông | Vai/cổ áo che khớp; tà dài tách trước/sau nếu cần |
| Head | Neo cổ và trục mặt | Bản đầu có mặt đầy đủ dưới tóc, không có mắt dính trong texture mặt |
| Eyes / Mouth | Dùng điểm mắt trước/sau và vùng biểu cảm | Đổi mắt giữ khoảng cách/góc; chớp mắt cần trạng thái nhắm riêng |
| Hair_Front | Neo đầu, đường chân tóc | Tách khỏi mắt/da; mái dài kiểm tra che mắt tại kích thước chơi |
| Hat / Hair_Accessory | Neo đầu, vùng đội mũ được quy định | Mũ khai báo tóc nào phải ẩn hoặc dùng biến thể dưới mũ; không hứa mọi tóc hợp mọi mũ |
| Arm_Front / Sleeve_Front | Vai trước, khuỷu và cổ tay trước | Giữ lòng bàn tay và phần áo riêng; thử duỗi/gập tay trước khi nhận |
| Weapon | Điểm grip, trục lưỡi, tay sử dụng | Thân vũ khí sau bàn tay; ngón tay che cán, không dính sẵn trong artwork áo |

Mỗi phụ kiện ghi `body_id`, `view_id`, `slot`, `variant_id`, anchor, lớp trước/sau, các phần cần ẩn, vùng giới hạn và pose đã thử. Phụ kiện cần bone hoặc silhouette mới phải ghi rõ; không tự coi mesh/weight cũ tương thích.

Thứ tự template từ sau ra trước: Back_Prop, Hair_Back, Boot_Back, Boot_Front, Leg_Back, Leg_Front, Arm_Back, Torso, Coat, Sleeve_Back, Head, Eyes, Mouth, Hair_Front, Hair_Accessory, Hat, Weapon, Arm_Front, Sleeve_Front. Đây là thứ tự dựng hình đứng; rig có thể cần tách thêm ngón tay/vạt áo và đổi thứ tự có chủ đích theo pose. Vũ khí phải được kiểm tra cùng tay cầm, không mặc định thứ tự này đúng cho mọi động tác.

[anchors.json](anchors.json) chứa tọa độ ứng viên; [slot-metadata-example.json](slot-metadata-example.json) minh họa thông tin cần bàn giao. Mẫu metadata chưa kết nối runtime và không đánh dấu phụ kiện nào đã đạt kiểm tra.

Đổi ngoại hình không đổi root, kích thước toàn nhân vật hoặc phase animation. Áo mới được thử với ít nhất hai tóc và vũ khí chuẩn; tóc mới được thử với mặt và mũ chuẩn. Có thể giới hạn tổ hợp bằng danh sách tương thích nếu hình không thể ghép đẹp; không để hệ thống lắp tự do tạo xuyên hình.

Vũ khí dùng các mảng lớn, chiều dài thử 0.7–1.0 lần chiều cao nhân vật; grip không đổi khi đổi kiếm. Ánh sáng và viền bám PAINT.md. Không tạo combat trong mốc thử mỹ thuật này.

Thử nghiệm [C2](experiments/C2/README.md) giữ mỗi phần ở canvas 1024 × 1024 và lấy pivot từ cùng manifest. Body, quần và tay áo tách trên/dưới khớp; các phần bị che khai báo `hidden_by`. Kiểm tra đổi ngoại hình phải so cả transform và phase animation, rồi xem hình ở các pose gập lớn: giữ đúng pivot vẫn có thể làm ống quần hoặc tay áo chồng hình xấu. C2 chưa có kết quả tương thích vũ khí/mũ và chưa thay bộ anchor chung.
