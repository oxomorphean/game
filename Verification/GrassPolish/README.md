# Nền cỏ — 13/10/2026

Phạm vi: làm dịu mảng màu nền cỏ và phá cảm giác đối xứng/lặp, giữ nguyên đường đất, kích thước map, đồ vật và footprint.

## Nguyên nhân

Nền chính dùng `MeadowTerrain.shader` lấy trực tiếp `meadow-grass.png` theo tọa độ thế giới. Mảng sáng có mép vụn đã nằm trong texture; sampler Mirror mỗi 8.5 đơn vị tạo các vùng lặp phản chiếu. Lớp biến thiên runtime trước đó chỉ nhân độ sáng, không đổi hình hoặc giảm tương phản các mảng có sẵn.

## Thay đổi

`Assets/_Project/Shaders/MeadowTerrain.shader` trộn hai lần lấy mẫu texture đã lọc mipmap, khác góc và tỷ lệ; giảm ảnh hưởng của mảng nguồn về 24% quanh màu cỏ nền. Biến thiên rộng theo tọa độ thế giới tạo sắc độ nhẹ, không thêm nét cỏ vụn hoặc ô tile. Hai material cỏ/đường cùng shader nên mép hòa cỏ tiếp tục khớp nhau. Phần lấy mẫu đất, alpha/mép đường và màu ánh sáng ban ngày giữ nguyên.

Thông số có thể chỉnh ở material: `_GrassBaseColor` (`.41,.62,.115`), `_GrassContrast` (`.24`), `_GrassMipBias` (`3`), `_GrassVariation` (`.065`), `_GrassPatchScale` (`4.8` đơn vị). Default shader áp dụng cho material hiện tại; không sửa prefab/scene/material bằng YAML, không sinh bitmap mới.

## Kiểm chứng

`git diff --check` đạt; diff chỉ thay phần lấy màu cỏ. Compile tích hợp do root agent chạy đạt 0 lỗi/0 cảnh báo; `Meadow Terrain` không có shader error. Material hiện hành trả đúng các default `_GrassContrast=.24`, `_GrassMipBias=3`, `_GrassVariation=.065`, `_GrassPatchScale=4.8`.

Đã trực tiếp xem [scene tại 11:30](../CanopyAndGrass/editor-1130.jpg), so với [baseline](../TreeGrounding/web-final.jpg): các mảng sáng răng cưa và đối xứng rõ đã dịu đi; nền vẫn có biến thiên màu nhẹ, nhân vật/đồ vật đọc rõ và đất giữ màu/mép hiện hành. Không cần thêm điều chỉnh thiết yếu cho mốc này. Đây là đánh giá mỹ thuật ở khung chơi; chưa phải phép đo FPS hay bằng chứng terrain hoàn chỉnh cho mọi map.

[Lượt tích hợp chung](../CanopyAndGrass/README.md) đã xem cỏ ở 08:00/11:30/16:00 trong Unity và bản WebGL mới gần 11:30. Build đạt 0 lỗi; console trình duyệt 0 lỗi/0 cảnh báo. Capture nằm ngoài `Assets`.
