# 2. Template Krita và bản xuất

`character-template.kra` là template dự án 1024 × 1024 với các nhóm slot, guide và metadata. Tạo bản sao trước khi vẽ. File `.kra` giữ layer/group/mask; xuất PSD trên bản sao để tránh mất cấu trúc nguồn. Không sửa PSD đã giao rồi bỏ qua KRA.

Trong mỗi nhóm slot dùng `Ink`, `Flats`, `Shadow`, `Highlight`, `Corrections` theo nhu cầu thực tế. Nét nằm trên màu, vùng bóng bị giới hạn trong silhouette của slot. Không tạo layer rỗng để giả vờ đã tách nét/màu. Với nguồn raster AI, ghi rõ `Paint_Source` chứa nét/màu gộp; phải ghi nhận mức khó chỉnh và vẽ lại các phần cần sửa. Template chuẩn không có nghĩa mọi thử nghiệm đã đạt chuẩn.

Nhóm cấp cao: `Guides` (ẩn khi xuất), `References` (ẩn khi xuất), `Character`. Trong Character, slot vẽ từ sau ra trước theo SLOTS.md. Mỗi variant của cùng slot là group riêng; chỉ một variant mặc định hiện. Tên layer không được âm thầm đổi giữa lần xuất vì importer dùng tên để ghép.

Guides gồm trục cơ thể, đỉnh tóc, đường chân và 22 điểm đầu/mắt/cổ/vai/hông/chân/phụ kiện; xem [bảng tọa độ](anchor-guide.png). `anchors.json` ghi điểm trong canvas theo X phải, Y xuống; tọa độ dùng chung, không suy ra pivot từ bounds sau crop. Các điểm hiện đo từ ứng viên A, chưa phải bind pose được kiểm tra trong rig. Khi cần chuẩn hóa kích thước một ảnh tham chiếu, chỉ áp dụng một affine transform đồng nhất cho toàn nhân vật.

Template có 19 nhóm slot trống; mỗi nhóm gồm năm lớp mẫu. Đây là cấu trúc để bắt đầu vẽ, không phải cơ thể đã hoàn thiện. Nhóm References chứa tham chiếu A và mặc định ẩn; guide được khóa để tránh vẽ nhầm. Các file A/B là thử nghiệm, có số nhóm và mức tách màu khác template; RESULTS.md ghi những điểm chưa đạt.

Vẽ đủ các vùng bị che tại cổ, vai, hông và chân tóc. Khớp vai/khuỷu/gối có vùng chồng kín, không viền đậm tại đường cắt bị che. Tóc/áo có phần cần uốn riêng được tách thêm; không cắt mọi chi tiết thành một slot.

Bản giao: raster hóa bản sao mỗi slot thành một layer giữ nguyên canvas/vị trí; xuất PSD 8-bit sRGB với alpha. Ghi ID mẫu, hướng và variant trong manifest. Kiểm tra PSD mở lại, không matte trắng, không đổi màu/alpha, không thiếu chi tiết so với KRA. Unity dùng skeleton tương thích, mesh/weights đã lưu; mở rộng silhouette cần kiểm tra lại SpriteRect và mesh, không chỉ reimport.

Nguồn đã chỉnh bằng tay được lưu bản mới; công cụ setup chỉ tạo file mới hoặc xuất bản sao, không tự sinh lại đường vẽ của artist.
