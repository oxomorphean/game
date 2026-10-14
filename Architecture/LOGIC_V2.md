> Lịch sử trước 12/10/2026. Bản hiện hành chỉ thử mỹ thuật; gameplay đã được chuyển ra khỏi source đang dùng. Xem [kiến trúc hiện hành](CODE_ARCHITECTURE.md).

# Logic 02 — đồng hành và vòng chơi local

Bản này mở rộng [logic 01](LOGIC_V1.md) trên cùng scene và tiến trình. Dùng lại toàn bộ sprite hiện có; chưa thêm multiplayer hoặc VPS.

## Chơi thử

Mở `http://127.0.0.1:8789/#choi`. WASD/phím mũi tên di chuyển; Tab đổi người/thú; J/Space/E đánh hoặc tương tác gần; K dùng kỹ năng; G vào/ra Thần Đình. F2 hoặc thanh trên web mở xưởng chủ dự án.

- Thú theo đường bạn vừa đi sau khoảng nửa giây. Khi bạn dừng, thú đi quanh ngắn rồi nghỉ gần bạn. Đổi điều khiển giữ nguyên vị trí, người tự theo thú.
- Thú tự đánh mục tiêu bạn đánh hoặc kẻ tấn công nhóm; thôi đuổi khi mục tiêu quá xa, bị thương hoặc biến mất. Thú cạn máu trở về nghỉ và cần gọi lại sau khi hồi đầy.
- Thú hoang tự đi/nghỉ quanh nơi ở. Khi đuổi quá xa sẽ trở về và hồi máu; có hảo cảm thì không chủ động đánh bạn.
- Nhấn Thả đồ hoặc G, chọn một ô chứa vật phẩm, chọn số lượng rồi thả. Đồ nằm cạnh thân thể ngoài map, kể cả khi bạn đang điều khiển thú. Đóng bảng trở về để nhặt; thức ăn thu hút thú gần đó, hảo cảm chỉ tăng sau khi thú thực sự ăn.
- Đóng/Escape/G đưa bạn ra khỏi Thần Đình. Chỉ nút Đi trong Thần Đình giữ thần hồn ở lại và ẩn bảng.
- Thanh trên trái hiện sinh lực/tu vi/chỉ số actor điều khiển; trên đầu actor và dãy khế ước có máu. Bảng Linh thú hiển thị chỉ số, trạng thái hoạt động và tiêu hóa.

## Dữ liệu và tính nhất quán

`GameSession` là nguồn trạng thái local. `CompanionMovement` giữ đường đi gần đây, độ trễ, đi/nghỉ và mục tiêu tự vệ; `WildMovement` quản lý đi quanh, ăn, giao tranh, quay về và hồi sinh; `GroundItems` thực hiện thả/nhặt/ăn/hết hạn. Trạng thái di chuyển tạm không lưu; vật phẩm, số lượng, thời gian còn lại và dấu chiến lợi phẩm có lưu.

Thả từ đúng stack được chọn, từ chối trước khi trừ kho nếu không còn chỗ trên đất. Nhặt cần đủ chỗ cho cả stack và chỉ thực hiện một lần. Thức ăn cạnh tranh chỉ bị tiêu thụ một lần. Chiến lợi phẩm đánh quái rơi xuống đất; nếu đủ 64 stack thì giữ trên quái chờ một chỗ trống. Chế tạo giữ dấu chiến lợi phẩm theo nguyên liệu thực sự đã tiêu, không theo các stack không dùng tới.

Bản lưu v1 nâng thành schema/trạng thái v2 trong bộ nhớ; giữ đầy đủ trường cũ, thêm cấu hình di chuyển và danh sách đồ trên đất rỗng. Lần tải v1 thành công giữ nguyên bản gốc ở hậu tố `.before-logic-v2` trước lần ghi v2. File/key chính giữ tên v1 để bản web và Editor tìm được tiến trình cũ. Tải lại không tự áp dụng thời gian offline hai lần. Save lỗi không được thay bằng phiên mới im lặng.

Offline chỉ tăng trưởng cây có nước và hồi thú đang nghỉ, tối đa 12 giờ. AI, tiêu hóa, ấp trứng, giao tranh và đồng hồ đồ rơi đứng yên khi mô phỏng không chạy. Trình duyệt và Editor có tiến trình riêng.

## Điều chỉnh và quay lại

Trong xưởng, Luật & dữ liệu có nhóm Di chuyển/thả đồ: độ trễ, khoảng gần, khoảng đuổi kịp, bán kính đi quanh/quay về và thời gian đồ trên đất. Chỉ Áp dụng mới thay đổi cấu hình phiên. Nút cấp đồ/tua thời gian thuộc xưởng local, không nằm trong HUD người chơi.

Nguồn trước logic 02 được sao lưu trong `Verification/LogicV2/before/`; không ghi đè các thay đổi mỹ thuật khác khi khôi phục. Trước khi chạy mã v1 trở lại, giữ một bản v2 riêng và dùng bản save `.before-logic-v2`; mã v1 không đọc schema v2. Không có commit/push tự động.

Hồ sơ kiểm chứng của bản đang chạy: [Verification/LogicV2/verification.md](../Verification/LogicV2/verification.md). Giới hạn: hai vùng thử, một loài, địa hình trang trí chưa có va chạm, chưa có server/tài khoản/giao dịch và chưa đo tải 100–300 người.
