# Kết quả thử nghiệm nguồn nhân vật — 13/10/2026

**Đề xuất: dùng A làm tham chiếu tạo hình, rồi hoàn thiện một nguồn Krita chuẩn để sản xuất.** A có dáng, tay áo và khối quần tốt hơn baseline thân dựng bằng script. B chứng minh tôi có thể thao tác công cụ Krita, nhưng chất lượng vẽ hiện chưa đạt yêu cầu. Thử nghiệm này không chứng minh điều khiển công cụ tự động thay được tay nghề artist.

Hai mẫu vẫn là hình đứng để đánh giá. Chưa mẫu nào đủ điều kiện thay đồ trong game; không thay player, scene hoặc build hiện tại.

## Xem kết quả

- [So sánh baseline, A, B](comparison.png): cùng chiều cao 55/72/85/400 px, nền xám và texture cỏ; đây là ảnh bố cục thử, không phải screenshot Unity.
- [Ẩn tóc, mắt, trang phục và đổi màu mắt](editability.png): giữ nguyên tọa độ toàn canvas khi tắt từng phần, không phóng lại hình để che lỗi.
- [Template Krita](character-template.kra), [guide 22 điểm](anchor-guide.png), [nét và vật liệu](paint-standard.png).
- Nguồn A: [KRA](experiments/A/A.kra), [PSD](experiments/A/A.psd), [PNG](experiments/A/A.png), [phương pháp](experiments/A/source-method.json).
- Nguồn B: [KRA](experiments/B/B.kra), [PSD](experiments/B/B.psd), [PNG](experiments/B/B.png), [phương pháp](experiments/B/source-method.json).

## Những gì thực sự đã làm

A dùng một lần ImageGen để tạo tham chiếu toàn thân. Ảnh được chuẩn hóa bằng một phép biến đổi đồng nhất, rồi phân vùng thành 18 nhóm có nội dung qua Krita API. Nét, màu và bóng vẫn gộp trong raster của từng phần. Mảng da dưới mắt và da đầu là phần bù bằng script, được đặt tên là ứng viên; chưa phải sửa hình bằng tay đạt chất lượng artist. Không gọi việc chia ảnh thành nhiều layer là đã có nguồn sản xuất hoàn chỉnh.

B được vẽ bằng Bezier, brush, tô màu và lấy màu trong giao diện Krita, dùng A làm lớp tham chiếu. Có 27 lớp nét/mảng trước khi tổ chức thành 17 nhóm có nội dung. Lớp tham chiếu được ẩn khi xuất. Script chỉ chuẩn bị tài liệu, tổ chức pixel đã vẽ, xuất và kiểm tra; không sinh đường vẽ B bằng QPainter/SVG. Nét và màu vẫn gộp ở nhiều lớp, còn Cloth_Details đi qua nhiều slot.

Thiết lập B: Basic-5 Size Opacity, opacity 100%, viền ngoài 8 px, nét trong 4 px, viền vàng 7 px trên canvas 1024. Đây là điều khiển bằng chuột/công cụ, không phải thao tác bút của artist dùng bảng vẽ.

## Đánh giá

| Tiêu chí | A | B |
| --- | --- | --- |
| Hình thể và thẩm mỹ — nhận xét của agent | Đủ tốt để đề xuất duyệt hướng; nếp tóc còn nhiều và cần thống nhất nét | FAIL: dáng cứng, mắt thiếu tự nhiên, mảng bóng và nếp áo còn sơ sài |
| Đọc ở 55/72/85 px | Tóc, mặt và trang phục phân biệt được; chi tiết nhỏ vẫn cần giản lược | Nhận diện được nhân vật nhưng mất biểu cảm, khối thân kém A |
| Mở lại nguồn KRA | PASS | PASS |
| Bật/tắt riêng mắt, tóc, quần áo | PASS thao tác; chưa chứng minh phần còn lại sạch | PASS thao tác; chưa chứng minh khả năng đổi bộ khác |
| Tắt tóc để đổi kiểu tóc | FAIL: còn mảnh tóc dính trong Head và da đầu bù chưa sạch | Tách được tóc; hình hộp sọ vẫn cần sửa |
| Vùng cơ thể dưới trang phục và vùng chồng khớp | FAIL: thiếu thân/cánh tay đầy đủ | FAIL: thiếu thân/cánh tay đầy đủ |
| Sửa màu mắt | PASS: 760 pixel đổi, nguồn được phục hồi chính xác | PASS: 649 pixel đổi, nguồn được phục hồi chính xác |
| Nét/màu/bóng chỉnh độc lập đúng template | FAIL: phần lớn vẫn gộp raster | FAIL: phần lớn nét/màu gộp; chi tiết áo chưa nằm hết trong slot |
| PSD xuất rồi mở lại | PASS về hình nhìn thấy và alpha | PASS về hình nhìn thấy và alpha |
| Sửa viền áo hoặc thay hình tay áo | NOT TESTED | NOT TESTED |
| Rig, Idle/Run, đổi silhouette giữa clip, lật hướng | NOT TESTED | NOT TESTED |

Các kiểm tra nguồn có số liệu ở [source-checks.json](evidence/source-checks.json). Sau khi mở lại PSD, sai khác alpha và hình tổng hợp trên nền đen/xám/trắng đều bằng 0 ở cả hai mẫu. Một số RGB trong pixel hoàn toàn trong suốt của B thay đổi khi qua PSD; hình nhìn thấy không đổi. Số nhóm hoặc kết quả xuất file không thay thế duyệt mỹ thuật.

[Kiểm tra gói bàn giao](evidence/package-checks.json) xác nhận template có 19 nhóm với đủ năm lớp mẫu, archive KRA hợp lệ, PSD 1024 × 1024 và ba file baseline giữ nguyên SHA-256. [Manifest A](experiments/A/export-manifest.json) và [manifest B](experiments/B/export-manifest.json) ghi phiên bản nguồn, slot, ngày xuất và checksum; trạng thái đều chưa được nhận vào sản xuất.

## Giới hạn thử nghiệm

Hai mẫu dùng cùng thiết kế tham chiếu, không phải hai thiết kế độc lập. B có lợi thế nhìn sẵn A nhưng vẫn không đạt chất lượng mong muốn. Không thể từ hai mẫu này suy ra mọi artist vẽ bằng Krita sẽ thua AI.

Mốc lưu file theo giờ Việt Nam: ảnh A gốc 08:46, tham chiếu chuẩn hóa 08:52, hai KRA phân lớp 09:30, kiểm tra nguồn 09:38 ngày 13/10/2026. Đây là mốc artifact, không phải số phút lao động: chưa ghi giờ bắt đầu/kết thúc riêng đủ chính xác để so tốc độ A/B. Không dùng chúng làm kết luận về năng suất. Một lượt sinh A; một lượt dựng B và một lượt thêm nét/bóng. Giữ ảnh A gốc và các lớp vẽ B gốc trong archive; dừng thử hình ở đây.

## Hướng sản xuất đề xuất

Khóa một mẫu đẹp trước, rồi artist hoàn thiện toàn bộ cơ thể với trang phục cơ bản, mặt dưới tóc và vùng chồng khớp trong cùng canvas Krita. Tách Ink/Flats/Shadow khi vẽ lại các phần cần chỉnh; không dùng mask tự động làm bước cuối. Mọi tóc, mắt, áo và vũ khí mới bám mẫu đó, cùng điểm gắn, góc nhìn và nét/màu. ImageGen có thể hỗ trợ tham khảo thiết kế; không sinh riêng từng frame hoặc từng món rồi co kéo để ghép.

Với chất lượng B hiện tại, tôi nên đảm nhiệm chuẩn nguồn, thao tác biên tập có giới hạn, xuất/nhập và kiểm tra; phần hoàn thiện tạo hình chủ lực nên có artist 2D. Nếu tiếp tục chỉ bằng AI và công cụ, cần duyệt từng mẫu ở kích thước chơi trước khi nhân rộng, không hứa trước chất lượng ngang artist.

Sau khi duyệt hình, bài chứng minh kế tiếp là cùng một rig đổi hai áo khác silhouette, hai tóc, hai mắt và hai kiếm trong Idle/Run. Chỉ khi các tổ hợp đó không hở khớp, lệch điểm gắn hoặc đổi tỷ lệ mới đưa vào game. Kết quả kiểm tra rig cũ không được áp dụng cho A/B mới.

Quy tắc rút ra cho mốc sau: kiểm tra tắt từng slot ngay khi dựng nguồn, trước khi làm rig; việc đó phát hiện vùng bị che còn thiếu sớm hơn việc nhìn ảnh ghép đầy đủ.
