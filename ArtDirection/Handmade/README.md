# Pixel art viết tay dạng code

`handmade_assets.py` định nghĩa sprite bằng chuỗi ký tự; mỗi ký tự là một màu trong `../Palette/linhthu-v1.gpl`. Sửa chuỗi rồi chạy:

```sh
python3 ArtDirection/Handmade/handmade_assets.py
```

Kết quả trong `out/` (PNG gốc, preview phóng to, ảnh ngữ cảnh, GIF idle) và tự chạy `AgentScripts/pixel/pixel_validate.py`.

| Asset | File | Kích thước | Trạng thái |
| --- | --- | --- | --- |
| Thạch Quy (`beast_turtle_stone`), hướng nam | `out/turtle-stone-32.png` | 32×32 | Mẫu ảnh, validator PASS, **chưa nhập Unity** |
| Thạch Quy idle 4 frame (thường, nhún, thường, chớp mắt) | `out/turtle-stone-idle-32.png` | 128×32 | Mẫu ảnh, validator PASS, chưa nhập Unity |
| Linh Sâm (nút tài nguyên) | `out/herb-linhsam-16.png` | 16×16 | Mẫu ảnh, validator PASS, chưa nhập Unity |

Pivot đề xuất: giữa đáy (rùa: x=16, y=4 tính từ đáy; linh sâm: x=8, y=2). Chưa có hướng bắc/đông, chưa có animation đi.
