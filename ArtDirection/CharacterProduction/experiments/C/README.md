# Mẫu C — thử vẽ nhân vật bằng SVG

Mẫu C dùng A làm tham chiếu thiết kế để so sánh cùng nhân vật. Ảnh bài đăng do chủ dự án gửi là ví dụ về cách trình bày quá trình vẽ; không dùng nhân vật trong bài đăng làm thiết kế game.

**Kết quả:** C có đường nét, mảng màu và từng phần cơ thể được mô tả bằng vector thực, không nhúng ảnh raster và không dùng thuật toán trace ảnh. Tôi đánh giá C tốt hơn mẫu B vẽ bằng công cụ Krita trước đó; A vẫn tự nhiên hơn ở tóc, nếp áo và biểu cảm. Đây là nhận xét mỹ thuật của agent, chưa phải chủ dự án duyệt mẫu mới.

## Xem và chỉnh nguồn

- [So sánh A–C](comparison-A-C.png), cùng chiều cao 530 px và 55/72/85 px; bounds tính theo alpha > 16 để không lệch kích thước do pixel trong suốt gần mép ảnh A.
- [C-editable.svg](C-editable.svg): 177 đường gốc, 17 nhóm có tên. Dùng bản này để chỉnh hình và màu.
- [C-vector.kra](C-vector.kra): 17 lớp vector thực đã nhập vào Krita. Dùng Edit Shapes Tool để sửa điểm/đường cong; Select Shapes Tool để chọn hình và đổi thuộc tính. [Hướng dẫn vector layer của Krita](https://docs.krita.org/en/reference_manual/layers_and_masks/vector_layers.html).
- [C.svg](C.svg): bản 1.600 phần tử để minh họa quá trình, cùng thiết kế với bản dễ biên tập.
- [C.png](C.png): bản raster xem nhanh 1024 × 1024, alpha trong suốt.
- [GIF 39 giây](drawing-process.gif) và [trình phát có thanh tua](process.html). Trình phát không cần mạng và mở được trực tiếp từ file.
- [Kiểm tra tắt tóc/mắt trong Krita](editability-vector.png), [số liệu](verification.json).

## Cách vẽ và cách đếm

Đường Bezier được viết từ quan sát ảnh A, theo các nhóm tóc sau → giày/quần → thân/tay/áo → mặt/mắt → tóc trước/phụ kiện. Sau khi xem render đầu tiên, tôi tăng độ rõ mí mắt và giảm độ dày nét bên trong tóc; bản trước chỉnh ở [revisions/C-before-review.svg](revisions/C-before-review.svg). Import Krita phát hiện tám mảng có khai báo stroke không hợp lệ; nguồn đã sửa và kiểm tra lại.

Animation được tạo sau bản vẽ để **mô phỏng năm bước**: phác tỷ lệ, đặt màu lớn, chia bóng/khối, vẽ mắt/phụ kiện và hoàn thiện nét. Đây không phải video quay màn hình thao tác trực tiếp, cũng không phải animation chuyển động của nhân vật. Không dùng nó làm bằng chứng rằng từng nét đã được vẽ trong Krita theo thứ tự đang phát.

Bản 1.600 phần tử gồm **129 mảng màu và 1.471 đoạn nét**. Các đoạn nét được chia từ đường gốc để phát dần. Vì vậy, con số đáp ứng lượng path của bản xuất nhưng **không tương đương 1.600 chi tiết được thiết kế độc lập**. Tôi chưa tái hiện độ phức tạp minh họa 1.500–1.700 chi tiết như ví dụ bài đăng. Bản 177 đường là nguồn hợp lý hơn để tiếp tục biên tập nhân vật game này; tăng số phần tử không tự làm hình đẹp hơn.

## Kiểm chứng và giới hạn

Đã kiểm tra XML hợp lệ, ID không trùng, màu/stroke hợp lệ, số path và không có thẻ image nhúng raster. KRA lưu 17 nguồn SVG riêng, mở lại được; tắt Eyes hoặc ba lớp Hair giữ phần đầu/mặt. Sau các lần bật/tắt, projection được phục hồi chính xác. Đã kiểm tra nút phát, tạm dừng, tua giai đoạn, slider và phóng lớn trong trình duyệt.

Raster hóa trong trình duyệt/librsvg và Krita có khác biệt khử răng cưa ở mép nét; không tuyên bố pixel-identical giữa hai renderer. Lưu rồi mở lại KRA lệch tối đa 4/255 ở một số pixel mép. Bản SVG chia đoạn và bản Bezier gốc cũng có sai khác khử răng cưa; ưu tiên C-editable.svg và C-vector.kra làm nguồn chỉnh sửa. Số liệu đầy đủ trong verification.json.

C có hộp sọ và mặt đầy đủ dưới tóc, thuận tiện hơn nguồn A cắt từ raster. Tuy nhiên, thân/cánh tay bị áo che chưa được dựng đầy đủ; vị trí nối khớp chưa qua rig và mới có một góc 3/4. Chưa thử áo/tóc khác silhouette, Idle/Run hay đổi đồ giữa clip. Không nhập mẫu này vào Unity hoặc thay nguồn A/B/baseline.

## Kết luận cho hướng làm tiếp

SVG là một hướng bổ sung đáng thử cho dự án này, nhất là khi cần sửa màu, viền và tỷ lệ có kiểm soát. Có thể dùng ImageGen để chốt tham chiếu rồi dựng lại có chọn lọc bằng vector trong cùng canvas Krita, tiếp tục vẽ bù phần bị che và rig. Mẫu C hiện chưa chứng minh quy trình đó đủ đẹp hoặc đủ nhanh cho toàn bộ catalogue; cần duyệt hình và thử một bộ đồ khác trước khi mở rộng.

Nguồn biên tập là C-editable.svg hoặc C-vector.kra sau khi artist sửa. Các script trong thư mục là công cụ tái lập bản thử; không chạy đè lên nguồn đã chỉnh. Khi thay đổi thiết kế, lưu phiên bản mới. Giữ hướng ImageGen + biên tập Krita đã được chủ dự án chọn; SVG hiện là thử nghiệm bổ sung.
