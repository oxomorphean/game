> Lịch sử trước 12/10/2026. Bản hiện hành chỉ thử mỹ thuật; gameplay đã được chuyển ra khỏi source đang dùng. Xem [kiến trúc hiện hành](CODE_ARCHITECTURE.md).

# Logic 03 — nghề, vật phẩm và hành trình

Bản local có 25 vật phẩm, 15 công thức thuộc ba nghề và sáu mốc hành trình. Dùng mỹ thuật hiện có; hai vùng khám phá. Đây là vòng chơi cơ bản để chủ dự án thử và sửa luật, chưa phải bản online hoặc nội dung phát hành cuối.

## Thử vòng chơi

1. Thu phục linh quy bị thương cạnh trại. Khám phá nhặt linh thảo, linh khoáng, linh mộc và hạt. Về trại, dùng nút tương tác để bảo toàn chuyến đi.
2. Mở Hành trang → Hành trình để nhận thưởng các mốc đã đạt. Thương nhân bán hạt, đất, Huyết Khế và vật tư; chỉ giao dịch khi thân thể ở trại Trúc Lâm, ngoài giao tranh. Linh tệ khởi đầu là 40; chỉ bán đồ đã bảo toàn.
3. Hành trang → Chế tạo: chọn Luyện đan/Luyện phù/Rèn đồ, chọn công thức, số lượng 1–10 rồi bắt đầu tại trại hoặc trong Thần Đình. Có thể tiếp tục khám phá. Nhận sản phẩm khi hoàn thành; nếu đầy kho, mẻ vẫn giữ nguyên.
4. Mỗi nghề có một mẻ cùng lúc; nghề cấp 2 ở 20 KN, cấp 3 ở 60 KN. Mỗi mẻ cơ bản +5 KN; mẻ hàng loạt nhân theo số lượng. Cấp nghề tăng tốc chế tạo 15% mỗi cấp. Công thức hiện tại chắc chắn thành công, không đốt nguyên liệu theo RNG.
5. Hành trang → Vật phẩm: chọn đồ và mục tiêu là người hoặc một linh thú. Đan hồi máu, tu vi, căn cốt; Linh thú hoàn là thức ăn tiêu hóa. Mỗi actor có hồi vật phẩm riêng 5s; Hỏa cầu phù 3s. Không tiêu đan nếu máu/tu vi đã đầy hoặc căn cốt đạt trần.
6. Phù hộ thân giảm sát thương, thần hành tăng tốc, cường lực tăng công trong thời gian hiển thị. Không chồng cùng hiệu ứng. Hỏa cầu phù đánh mục tiêu khỏe gần người chơi; Quy nguyên phù đưa nhóm về trại sau khi rời giao tranh 5s, chưa tự bảo toàn chiến lợi phẩm.
7. Vũ khí và giáp dành cho người chơi. Trang bị/tháo/đổi là chuyển vật phẩm, không cộng dồn chỉ số vĩnh viễn. Đổi trả món cũ, đầy kho thì từ chối toàn thao tác. Người và thú đầy tu vi vào Thần Đình dùng linh khoáng để đột phá. Mốc Đồng hành đột phá đánh dấu hoàn thành vòng chơi đầu.

WASD di chuyển; Tab đổi người/thú; J/Space/E hành động chính; K kỹ năng; G Thần Đình; H dùng Hồi xuân đan cho actor đang điều khiển; Escape đóng bảng; F2 xưởng chủ dự án. Giao diện người chơi và dashboard vẫn tách riêng.

## Quy tắc dữ liệu

Sản phẩm từ nguyên liệu chuyến đi giữ dấu chuyến đi, kể cả trong mẻ hoặc khi trang bị. Về trại bảo toàn cả kho/mẻ/trang bị; đồ vẫn thả ngoài đất chưa được bảo toàn. Gục mất phần theo tỷ lệ hiện hành; với món đơn lẻ làm tròn lên nên đồ mới đang đeo có thể mất. Món đã bảo toàn giữ nguyên. Mẻ bị mất hết sản phẩm vẫn cho kinh nghiệm nghề khi nhận vì nguyên liệu đã bỏ vào mẻ.

Công việc chụp đầu ra, thời gian, kinh nghiệm và trạng thái chuyến đi lúc bắt đầu. Sửa công thức chỉ ảnh hưởng mẻ mới. Tiến trình offline tối đa 12h cho cây, thú đang nghỉ và chế tạo; hoàn thành không tự nhận thưởng. Chiến đấu, cooldown, phù duy trì và đồ rơi chỉ tiến khi mô phỏng chạy. Save v1/v2 được chuyển lên v3, giữ thông số đã chỉnh, thú, cây, vật phẩm và tiến trình. Linh kiếm cũ chuyển thành trang bị, hộ thân cũ chuyển thành hiệu ứng cho nhóm. Bản gốc giữ ở hậu tố `.before-logic-v3` trước lần ghi mới.

## Dashboard chủ dự án

Mục Vật phẩm & nghề có danh mục phân trang, xem mẻ hiện hành, chỉnh thời gian/đầu ra/KN/cấp nghề/chi phí từng công thức; chỉnh power, duration, giá mua/bán vật phẩm. Chỉ Áp dụng mới đổi phiên chơi. Tua 60s là công cụ kiểm tra local; không mô phỏng chiến đấu. Các cấu hình tên/food/loài cũ ở Luật & dữ liệu vẫn giữ. Những ngưỡng nghề và điều kiện hành trình được cố định trong logic bản đầu, không tự nói rằng dashboard chỉnh được mọi thứ.

## Khôi phục và bảo trì

Kiến trúc đã triển khai: [CODE_ARCHITECTURE.md](CODE_ARCHITECTURE.md). Nguồn trước đổi nằm ở `Verification/LogicV3/before/`, gồm scripts, tài liệu và báo cáo nhúng. Khi quay lại v2, dừng Editor, giữ riêng bản v3, khôi phục nguồn cũ và loại các file chỉ có ở v3; dùng save `.before-logic-v3` đi cùng mã v2. Không đưa save v3 cho code v2 đọc. Chưa commit/push/deploy.

Hạn chế còn lại: cân bằng giá/nhịp chơi chưa qua playtest dài; vật phẩm cùng loại có chỉ số chung, chưa có durability/affix ngẫu nhiên; chưa có nhiều loài, boss, bí cảnh hoàn chỉnh, giao dịch người chơi, server hoặc tài khoản. Các giới hạn này không ngăn thử vòng chơi local đã mô tả.
