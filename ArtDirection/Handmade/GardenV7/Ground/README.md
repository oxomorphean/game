# Garden ground v7 — Linh Thú Ký

Bộ tile nền vẽ trực tiếp qua Aseprite Lua, theo STYLE_GUIDE.md và palette 33 màu hiện hành. Chỉ có pixel native; không dùng ảnh sinh hoặc asset tải ngoài. Gói này chưa được nhập hay kiểm tra trong Unity.

## Asset

Mỗi PNG dưới đây opaque 16×16, PPU16, pivot đề xuất (8,8) pixel / (0.5,0.5). Import: Sprite, Single, Point, mipmaps Off, Compression None.

| PNG | Cách dùng |
| --- | --- |
| grass-a.png, grass-b.png, grass-c.png, grass-d.png | Trộn các cụm cỏ. Cả bốn mép giữ #83B865 để ghép mọi cặp không lộ ô. |
| paving-a.png, paving-b.png | Sân đá kem. Biến thể cùng màu nền và mép; vệt bevel/rạn ngắt đoạn, không có viền kín quanh ô. |
| paving-moss.png | Cụm rêu nhỏ ở góc; đặt thưa quanh props/perimeter, giữ giữa sân thoáng. |
| paving-edge.png | Mép dọc: cỏ bên trái, đá bên phải. Mép trên/dưới khớp; đặt ở hàng biên trái sân. Không rải giữa sân. |
| earth-path.png | Đất ấm sáng, ít tương phản quanh hồ/đá. Không phải bộ autotile chuyển tiếp đầy đủ. |

Không xoay các tile có bevel chỉ để thay hướng ánh sáng; hướng sáng đã đặt ở trên trái. Native source garden-ground.aseprite có 6 layer chất liệu và 9 slice đúng tên PNG, pivot slice (8,8), atlas 48×48.

## Preview và kiểm chứng

- garden-ground-atlas.png / garden-ground-atlas4x.png: atlas 1×/4×.
- tiled-preview1x.png / tiled-preview4x.png: mảng cỏ, đá và đất ghép không đổi tint từng tile.
- create-garden-ground.lua: tái tạo source, PNG và preview bằng Aseprite.
- pixel-validation.json: kết quả validator của project; strict, kind tile, cell16×16, size16×16, max16 màu.
- statistics.json và inspect-package.py: số pixel/màu, alpha, hash, so khớp crop atlas, các assertion mép ghép; chỉ đọc sprite cũ để đối chiếu màu.
- source-metadata.json: thông tin layer/slice/palette đọc lại từ Aseprite MCP.
- linhthu-v1.hex: bản sao palette khóa, không sửa palette project.

Strict validator: 9 PASS, 0 WARN, 0 FAIL. Tile dùng 2–6 màu, toàn bộ alpha255. Preview đã được xem ở 1× và 4×. Không thay cổng, cây, Huyền Giáp Quy hoặc scene hiện có.

## Tái tạo và kiểm tra

Chạy create-garden-ground.lua qua Aseprite run_lua_script. Script ghi cố định vào /private/tmp/nguthu-garden-ground-v7/.

```sh
python3 /private/tmp/nguthu-garden-ground-v7/inspect-package.py
python3 /Users/nguyenhoangdacdat/dev/nguthu-unity-demo/AgentScripts/pixel/pixel_validate.py /private/tmp/nguthu-garden-ground-v7/grass-?.png /private/tmp/nguthu-garden-ground-v7/paving-*.png /private/tmp/nguthu-garden-ground-v7/earth-path.png --strict --kind tile --cell 16x16 --size 16x16 --max-colors 16 --json /private/tmp/nguthu-garden-ground-v7/pixel-validation.json
```
