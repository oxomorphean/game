# Linh Quy — sprite tự tạo bằng Aseprite MCP

Thiết kế gốc cho Linh Thú Ký theo `ArtDirection/STYLE_GUIDE.md`: rùa chibi nhìn cao từ trước, đầu quay phải, mai xanh ngọc chia mảng lớn, dấu trán vàng. Viền navy; ánh sáng trên trái. Không tải asset, không dùng ImageGen.

- `spirit-turtle.aseprite`: nguồn RGB 64×64, 5 layer chỉnh được, 10 frame; tag `idle` 1–4 và `walk` 5–10.
- `create-spirit-turtle.lua`: vẽ từng cụm pixel qua Aseprite Lua API; `create-spirit-turtle.py` gọi server Aseprite MCP bằng transport STDIO để tạo và xuất.
- `Assets/ArtSample/Sprites/turtle.png`: idle frame đầu, 64×64.
- `Assets/ArtSample/Sprites/turtle-idle.png`: sheet ngang 4 frame, 256×64. Nhún nhẹ, chớp mắt và đổi đầu/đuôi; thời gian frame 380/380/130/380 ms.
- `Assets/ArtSample/Sprites/turtle-walk.png`: sheet ngang 6 frame, 384×64, 140 ms/frame. Hai cặp chân bước đối pha và có frame nhấc chân; đầu/thân nhún 1 pixel.
- `turtle-idle.json`, `turtle-walk.json`: hình chữ nhật và thời gian frame do Aseprite xuất. `*-preview-5x.png`, `turtle-preview-8x.png` và GIF phóng nguyên lần để duyệt.

Import Unity: PPU16, pivot `(0.5, 0.18)`, Point, mipmap tắt, compression None. Tiếp đất cố định y52 trên canvas có gốc trên trái. Animation giữ cùng canvas, silhouette và palette; flip ngang chỉ áp dụng cho hướng trái/phải của bản demo.

`verification.json` ghi các call MCP thật và kiểm tra ảnh xuất: đủ 4/6 frame, mọi frame khác nhau, 11–13 màu mỗi frame (tối đa 16), đúng palette khóa, alpha chỉ 0/255. `pixel-validation.json` là kết quả chạy validator của dự án với `--cell 64x64 --strict`: cả 3 PNG PASS, không WARN/FAIL. Đã xem preview ở kích thước gốc và phóng nguyên lần; kiểm tra Unity/Game View do agent ghép scene thực hiện riêng.

Tạo lại bằng môi trường MCP đã cài:

```sh
/Users/nguyenhoangdacdat/dev/tools/aseprite-mcp/.venv/bin/python -B \
  /Users/nguyenhoangdacdat/dev/nguthu-unity-demo/ArtDirection/Handmade/SpiritTurtle/create-spirit-turtle.py
```
