# Thử nghiệm A/B — 13/10/2026

Goal: chọn cách tạo artwork đẹp hơn mẫu thân bằng script, đồng thời giữ khả năng biên tập trong Krita. Hai mẫu là thử nghiệm nguồn và hình đứng; chưa thay scene/player.

Baseline: `../HeroProduction2D/assembled-ivory.png`. A và B cùng ngự thú sư, một thiết kế, một góc 3/4, một bộ áo và cùng kích thước hiển thị. ImageGen tạo bản tham chiếu thiết kế cho A; B dùng chính thiết kế đó làm underlay để khác biệt chủ yếu đến từ cách thực hiện, không phải prompt hoặc trang phục khác nhau.

A: ImageGen built-in, chỉnh/sắp lớp trong Krita; ghi phần còn gộp nét/màu, các chỗ phải vẽ lại hoặc thiếu phía bị che. B: thao tác công cụ vẽ Krita để dựng hình; script chỉ tạo tài liệu/layer, đặt tham chiếu, metadata, xuất và kiểm tra. Không vẽ lại B bằng QPainter hoặc SVG sinh từ code rồi gọi đó là vẽ bằng công cụ.

Giới hạn hai vòng chỉnh hình cho mỗi mẫu. Ghi thời điểm bắt đầu/kết thúc, số lần sinh, thao tác chỉnh và lỗi còn lại; không tuyên bố cùng thời gian nếu thực tế khác. Giữ output trước/sau, không chọn ảnh đại diện che mất lỗi.

Đầu ra: A.kra, B.kra, PNG cùng scale, bảng so sánh trên xám và map, source metadata, nhận xét theo ACCEPTANCE.md. Kiểm tra biên tập tối thiểu gồm ẩn/hiện tóc/mắt/áo và một thay đổi màu ở vùng đã tách; không gọi đổi màu là đã chứng minh đổi silhouette.

Chọn hướng theo hình đọc ở 72 px, phối cảnh/thẩm mỹ, đồng nhất nét/màu, mức chỉnh độc lập và công sửa. Nếu cả hai chưa đạt, báo đúng kết quả và đề xuất artist hoàn thiện mẫu gốc; không mở rộng hàng loạt asset để che vấn đề.

Rollback: chỉ thêm thư mục CharacterProduction; phần style guide bổ sung liên kết tới chuẩn này. Không sửa source/scene của mẫu HeroProduction2D. Chưa có build mới cho A/B cho tới khi chọn hình và chuẩn hóa rig.
