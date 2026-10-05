# Nguồn mỹ thuật khu vườn v7

Bản đang dùng theo yêu cầu sửa cỏ ngày 05/10/2026:

- `CleanGrass/grass-clean`: tile 16×16 chỉ có màu nền #83B865.
- `CleanGrass/grass-shade-b`: một mảng tối nhẹ #79AA5E gần hồ; shade-a là phương án chưa dùng.
- `Ground/paving-a`, `paving-b`, `paving-moss`, `paving-edge`: bốn tile sân; mật độ nét trên sân còn có thể tinh chỉnh ở lượt khác.
- `Details/flower-cluster`, `lantern`, `petal`, `water-ripple`: hoa thưa, đèn đá, cánh hoa rơi và bốn frame gợn nước.

Các file `.aseprite` và Lua là nguồn editable/tái tạo qua Aseprite MCP. Palette hiện hành nằm tại `ArtDirection/Palette/linhthu-v2.gpl`: giữ nguyên 33 màu v1, thêm đúng một màu cỏ tối nhẹ.

`Ground/grass-a/b/c/d` có nét nhỏ rải đều và đã bị người dùng loại; `earth-path`, `Details/grass-tuft-a/b`, `fern` là mẫu không nhập vào cảnh cuối. Giữ chúng ngoài `Assets/` làm lịch sử thử nghiệm, không dùng làm chuẩn. Các preview và validator trong từng thư mục ghi lại toàn bộ gói nguồn, kể cả mẫu không dùng. Ảnh Unity cuối và kiểm chứng hiện hành nằm ở `ArtDirection/ArtSample/`.
