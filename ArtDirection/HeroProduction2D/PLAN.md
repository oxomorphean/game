# Mẫu nhân vật nhiều lớp — 12/10/2026

Trạng thái: chủ dự án yêu cầu tiếp tục ngày 12/10 sau khi xác nhận ImageGen chỉ làm tham chiếu, bản nguồn phải được biên tập trong Krita. PSD Importer 15.0.1 đã thêm; bộ cài Krita chính thức đã tải từ KDE và xác minh chữ ký Apple. `master-front.png` chỉ là ảnh tham chiếu, không phải nguồn production hoặc hình nghiệm thu.

Phạm vi được giao: ngự thú sư tóc bạc, hai bộ trang phục trên cùng nguồn và rig, clip Idle/Run bằng keyframe. Yêu cầu bổ sung mới chọn góc trình mẫu 3/4 nghiêng theo [STYLE_GUIDE.md](../STYLE_GUIDE.md#nhân-vật-nhiều-lớp--bổ-sung-theo-yêu-cầu-12102026); không dùng ảnh chính diện đã sinh làm kết quả cuối. Mẫu được duyệt riêng trước khi thay player chính. Không thêm gameplay hoặc sửa Domain/map.

Nguồn chỉnh sửa là Krita `.kra`, bản giao Unity là `.psd` nhiều lớp raster. Lớp giữ chung canvas, vị trí, tỷ lệ, tên và vùng chồng tại khớp. Skeleton, mesh, weights, prefab và AnimationClip được lưu khi authoring; runtime chỉ chọn ngoại hình và phát clip. Điểm chân trên root không bị animation dịch chuyển. Mẫu di chuyển đọc displacement sau va chạm; chạy tại chỗ là chế độ xem clip có nhãn riêng.

Các bước: cài Krita và PSD Importer phù hợp; kiểm tra nhập vài lớp; chuẩn bị nguồn và hai bộ đồ; author rig/clip/prefab/scene riêng; kiểm tra reimport, đổi đồ giữa clip, tiếp đất và khớp ở kích thước chơi và zoom; build WebGL mẫu và lưu bằng chứng.

Điều kiện bàn giao: `.kra` mở được với các lớp riêng, `.psd` nhập đúng bố cục, rig và clips lưu thành asset; đổi đồ không đổi pivot/tỷ lệ hoặc reset nhịp chạy; compile/build thành công; xem ảnh chạy thật và ghi rõ giới hạn. Artwork/animation không được đánh giá đạt chỉ từ compile hoặc sự tồn tại của file.

Bản khôi phục cấu hình trước thêm package ở `Recovery/HeroProduction2D-20261012/config-before.tgz`; hash các scene/map/cấu hình trước lượt làm ở `Verification/HeroProduction2D/protected-before.sha256`. Asset mẫu cũ giữ nguyên.

Cập nhật 13/10: nguồn KRA/PSD, rig lưu sẵn, hai trang phục và clip Idle/Run đã chạy trong scene riêng. Kiểm tra asset qua 40 assertion; runtime qua 37 assertion, gồm hồi quy bật lại hero khi đang chạy. Review độc lập và nhánh phục hồi khi công cụ kiểm tra lỗi đã xử lý. Bản Web và giới hạn mỹ thuật được bàn giao trong [README.md](README.md); việc duyệt chất lượng hoặc thay player chính là mốc riêng.
