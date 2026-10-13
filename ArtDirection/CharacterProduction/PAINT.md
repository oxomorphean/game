# 4. Nét, màu và vật liệu

Xem [bảng mẫu trực quan](paint-standard.png), [palette](character-v1.gpl) và [thiết lập brush đã dùng](brush-settings.json). Màu là điểm khởi đầu cho thử nghiệm, chưa là bảng màu được duyệt cuối. Bảng vật liệu là sơ đồ minh họa quy tắc, không phải mẫu artwork đạt chuẩn.

| Vai trò | Base | Shadow | Highlight |
| --- | --- | --- | --- |
| Viền | #20283B | — | — |
| Tóc bạc | #EDF0EF | #ABB9CD | #FFFBEF |
| Da | #F3C5A2 | #CD927D | #FFE2BE |
| Vải ngà | #F2E8CD | #B4B8AA | #FFF5D8 |
| Vải xanh đen | #334561 | #202D46 | #566B80 |
| Ngọc | #438F85 | #255960 | #9BD3B7 |
| Kim loại vàng | #C9A365 | #866747 | #EDD5A0 |

Canvas 1024, nhân vật dự kiến cao khoảng 800 px: nét viền ngoài 7–10 px, nét trong 3–5 px; điều chỉnh một lần theo mẫu được duyệt, rồi giữ cùng độ dày thị giác trên tất cả slot. Viền ngoài không nhất thiết đều như máy: giảm ở điểm sáng, nhấn ở nơi chồng lớp. Khớp bị che không có viền riêng gây đường đen kép.

Dùng brush nét đặc không texture, opacity 100%, chống răng cưa; đường cong có chủ ý và ít đỉnh. Mẫu công cụ gợi ý: Bezier/Freehand Path cho contour, polygonal selection để chia mảng, brush tròn đặc để sửa đầu nét. Bản vẽ bằng chuột được ghi là điều khiển công cụ, không mô tả thành nét bút của artist có bảng vẽ. Thiết lập thực dùng được ghi trong kết quả A/B.

Tô cel 2–3 sắc độ, mỗi mảng bóng có nguyên nhân theo khối. Tóc thành 5–8 khối lớn, không rải sợi nhỏ. Vải mềm có nếp tại khuỷu/đai/hông, không dựng tay áo thành ống giáp đối xứng. Da ít nét, không bóng nhựa. Kim loại có một cạnh sáng và một mặt tối rõ; không rắc glint khắp áo.

Ánh sáng vẽ sẵn nhẹ từ trên trái theo mẫu để mô tả thể tích. Không bake bóng mặt đất vào tóc/áo/giày. Bóng thời gian trong game do shader/scene xử lý; không vẽ rim light khác hướng ở từng phụ kiện.

Ví dụ loại: đầu nhiều nếp nhưng thân chỉ vài khối phẳng vô lý; hoa văn nhỏ biến thành nhiễu ở 72 px; viền áo dày hơn toàn bộ mặt; thêm dây/trang sức không có chức năng; hai phía cổ áo khác cấu trúc; thân chính diện và đầu 3/4. Hình mẫu đúng phải có cùng ngôn ngữ nét, ánh sáng và phối cảnh trên toàn thân.

Với nguồn SVG, không đặt chỉ tiêu số path. Mỗi đường phải phục vụ silhouette, cấu trúc, sáng tối hoặc chi tiết còn đọc được. Dùng ít nút Bezier đủ kiểm soát đường cong; giữ khối tóc lớn, góc nhọn có chủ ý và đường cắt bị che không có viền đen. Cơ thể và phụ kiện được vẽ trong cùng tọa độ, nhưng đánh giá bằng hình ghép và chuyển động trong Unity. Số lớp/path hoặc việc sửa được vector không thay cho chất lượng hình.
