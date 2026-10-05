# Huyền Giáp Quy v6

Nguồn được vẽ mới bằng các cụm pixel khai báo trong Lua và thực thi qua Aseprite MCP. Không dùng ảnh sinh AI, ảnh tải ngoài hoặc đổi màu bitmap rùa cũ. `huyen-giap-quy.aseprite` có 9 layer, 90 cel và 10 frame; `create-huyen-giap-quy.lua` tái tạo nguồn. Biến `out` đầu script mặc định trỏ tới `/private/tmp/huyen-giap-quy-v6`; đổi đường dẫn đó khi muốn tái tạo tại một thư mục khác.

Mai đen có ba bậc navy/charcoal, một dải sáng nhỏ trên trái, đường vàng theo scute và một phù ấn trung tâm. Đầu ngọc xanh tối, hàm kem, mắt vàng và hai sừng ngắn. Layer mai và hoa văn chỉ dịch nguyên khối khi nhún; native Lua kiểm tra 18 cặp layer/frame sau bù nhún trả `TOTAL_MISMATCHES=0`.

Canvas 64×64, PPU 16, hướng phải, pivot Unity tính từ dưới trái `(32,12)`, chân thấp nhất luôn ở pixel y52. Footprint của 10 frame là 52×34 hoặc 52×35. Idle 1–4 xuất thành `turtle-idle.png` 256×64; walk 5–10 xuất thành `turtle-walk.png` 384×64. `turtle.png` là frame đầu 64×64. Các PNG/GIF được xuất native từ source Aseprite; ảnh contact và ảnh nền kem chỉ dành cho xem đối chiếu, dùng nearest-neighbour 4×.

Palette nguồn giữ đủ 33 màu của `linhthu-v1`; mỗi frame dùng 13 màu opaque, alpha chỉ 0/255. `pixel-validation.json` ghi kết quả strict của cả 3 PNG: 3 PASS, 0 WARN, 0 FAIL, với `--cell 64x64 --max-colors 16 --strict`. `source-metadata.json` ghi bbox, ground, màu, alpha và hash pixel từng frame; `inspect-package.py` tái tạo phần đo và ảnh contact.

Bộ PNG đã được nhập bằng Unity Pipeline/CLI và chạy trong Game View cạnh môi trường v4. Controller, gameplay, runtime scale và lật hướng giữ nguyên. Bằng chứng kiểm chứng nằm tại `ArtDirection/ArtSample/verification-v6.md` tính từ thư mục dự án.
