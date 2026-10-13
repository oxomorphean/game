# Đề xuất nhân vật 2D tách lớp — 09/10/2026

Trạng thái: chủ dự án đã duyệt lát thử nhìn trước ngày 09/10/2026 và xác nhận cơ thể gốc phải tách khỏi trang phục. Mẫu tách lớp ở [HeroModular2D](../HeroModular2D/README.md); player chính hiện vẫn phát sprite sheet. Tám góc mỹ thuật và luật trang bị/save chưa được thay trong lát thử này.

Hiện có ba hình nguồn trước/nghiêng/sau, trái phản chiếu nghiêng phải, đường chéo dùng lại trước/sau. Tám hướng lệnh được xử lý nhưng chưa có đủ tám góc mỹ thuật. 416 frame được xuất bằng biến dạng vùng của ảnh nguyên khối; không có mắt, tóc, áo/quần độc lập. Kiểm tra hướng/combat không xác nhận thẩm mỹ hoặc tám hình khác nhau.

Đề xuất giữ phong cách minh họa, dùng Unity 2D Animation 16.0.1 đã có trong project: SpriteSkin điều khiển mesh/xương; Sprite Library và Sprite Resolver chọn mảnh theo góc nhìn và ngoại hình. Tách thân/mặt, mắt, tóc trước/sau, áo thân/tay áo, cánh tay/bàn tay, quần/chân/giày và vũ khí thành các phần theo thứ cần thay và khớp cần cử động. Có thể đóng gói chung atlas; không bắt buộc mỗi phần là một file PNG runtime.

Các ảnh thay thế dùng cùng bố cục xương, điểm nối, tỷ lệ và quy ước sorting. Phải vẽ bổ sung vùng bị che ở ảnh gốc để tay/chân chuyển động không lộ lỗ. Mỗi góc nhìn cần artwork tương ứng; rig 2D không tự tạo góc chéo hoặc mặt sau từ một ảnh. Phản chiếu chỉ dùng nếu thiết kế cho phép đổi trái/phải; kiếm hoặc chi tiết bất đối xứng cần giữ đúng tay. Tư thế chém cần pose và đường chuyển động được thiết kế, không chỉ tăng số frame hoặc FPS.

Ví dụ: giữ bộ chuyển động chạy, thay sprite áo và tay áo từ xanh sang trắng; đầu/mắt/tóc/vũ khí tiếp tục theo cùng rig. Một mẫu tóc hoặc áo mới cần các góc nhìn cần thiết và kiểm tra khớp; không cần dựng lại mọi tổ hợp tóc × áo × quần thành bộ sheet nhân vật hoàn chỉnh. Màu mắt có thể dùng tint với lớp mắt riêng; đổi hình mắt cần sprite khác. Animation bổ sung hình/pose khi biến dạng bằng xương không đủ đẹp.

Lát thử đề xuất: một rig nhìn trước với đứng/chạy/chém, hai tóc và hai áo, kiểm tra đổi khi đang chạy. Duyệt hình ở kích thước chơi và mức zoom dự kiến rồi hoàn thiện tám góc, thứ tự tay/kiếm/áo theo hướng. Sau đó đo chi phí renderer/mesh/atlas trên bản WebGL với số người trong một vùng; chưa kết luận sức chứa từ số sprite hoặc test chức năng.

Mỹ thuật sở hữu mảnh hình, xương, pose, góc nhìn và phần render. Logic tiếp tục sở hữu điều kiện trang bị, ID ngoại hình được chọn, lưu/tải và trạng thái combat; phần hiển thị chỉ đọc trạng thái. Chưa mở hệ trang bị/save mới trong lượt này.

Unity hỗ trợ đổi từng sprite và dùng lại skeleton; sprite rig thay thế cần skeleton giống nhau: https://docs.unity3d.com/Packages/com.unity.2d.animation@16.0/manual/SpriteSwapIntro.html .
