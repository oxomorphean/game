# 5. Nghiệm thu nguồn và asset

Mỗi mục ghi PASS, FAIL hoặc NOT TESTED kèm bằng chứng. Không dùng việc file tồn tại, layer đếm đủ hoặc compile thành công thay cho duyệt mỹ thuật. Artwork cuối do chủ dự án duyệt; agent ghi nhận xét và giới hạn, không tự đóng dấu.

| Nhóm | Bài kiểm tra |
| --- | --- |
| Hình thể | Cùng góc cho đầu/thân/chân; dáng tự nhiên; tay trước/sau có độ sâu; không sai cấu trúc |
| Đọc ở kích thước chơi | So cùng chiều cao 55/72/85 px trên xám và map; tóc/mặt/áo tách rõ, không lốm đốm |
| Tính đồng nhất | Palette, nét, hướng sáng, mật độ chi tiết; đối chiếu mẫu chuẩn cạnh nhau |
| Nguồn thực | Mở lại KRA; group/layer có nội dung đúng; tắt mắt không mất da, tắt tóc không mất mặt; thay áo không đổi đầu |
| Chỉnh sửa | Sửa màu mắt, viền áo, hình tay áo; ghi thao tác, thời gian và có phải vẽ lại pixel gộp hay không |
| Ghép phần | Điểm mắt, cổ, vai, chân, grip giữ nguyên; phần bị che được vẽ bù; tóc/mũ/tay/kiếm không xuyên |
| Xuất/nhập | PSD/PNG alpha và màu khớp KRA; sprite ID/pivot không đổi ngoài ý muốn; mở rộng bounds kiểm tra mesh |
| Chuyển động | Idle và toàn chu kỳ Run, gập tay/khuỷu, lật trái/phải; kiểm tra khe ở vai/cổ/hông/gối và chân trượt |
| Đổi giữa clip | Tóc/mắt/áo/vũ khí đổi khi đang chạy; không reset phase, root hoặc tỷ lệ; bật lại nhân vật vẫn chạy |

Ảnh minh họa A/B chưa rig được đánh dấu NOT TESTED ở chuyển động. Sau khi chọn hướng, chỉ nhận asset sản xuất khi đã thử đổi phần giữa animation trong Unity. Mẫu HeroProduction2D cũ có checks kỹ thuật, nhưng kết quả đó không tự áp dụng cho artwork mới.

Một bộ thử mở rộng tối thiểu sau khi duyệt hình gồm: hai tóc, hai mắt, hai áo khác silhouette, hai kiếm và một mũ có quy tắc che tóc. Duyệt mọi tổ hợp được hệ thống cho phép; tổ hợp chưa đạt bị loại hoặc ghi không tương thích. Mốc A/B hiện tại chưa sản xuất toàn bộ catalogue này.

Bằng chứng: ảnh gameplay-size và close-up, nguồn KRA, bảng layer/anchor, clip hoặc ảnh các phase, manifest xuất và báo cáo lỗi. Mọi FAIL liên quan hở khớp, che mặt, sai pivot hoặc mất phần khi đổi phải sửa trước khi đưa vào game; chi tiết trang trí thêm là backlog.
