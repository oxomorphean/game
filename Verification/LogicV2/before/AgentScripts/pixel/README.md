# Pipeline pixel art — Linh Thú Ký

Script Python chạy ngoài Unity, nằm ngoài `Assets/` nên Unity không import. Cần Pillow và numpy (`pip install -r AgentScripts/pixel/requirements.txt`).

| File | Vai trò |
| --- | --- |
| `../../ArtDirection/Palette/linhthu-v2.gpl` | Palette hiện hành 34 màu (v2 giữ 33 màu v1, thêm một bậc cỏ tối nhẹ) (bản `.hex` và swatch PNG nằm cùng thư mục, dùng cho Aseprite/Lospec/PixelLab). |
| `pixelize.py` | Chuẩn hóa ảnh AI hoặc sprite PixelLab về palette, alpha 0/255, ô cố định, điểm tiếp đất chung. Tự chạy validator ở cuối. |
| `pixel_validate.py` | Kiểm tra sprite. Mã thoát 1 nếu có FAIL. |
| `pixel_common.py` | Hàm dùng chung (đọc palette, map màu OKLab, tiện ích alpha). |

## Luồng chuẩn

```text
Concept (ImageGen) ─┐
                    ├─> pixelize.py ─> pixel_validate.py ─> Aseprite chỉnh tay ─> pixel_validate.py ─> Assets/
PixelLab (native) ──┘   (--native)
```

1. **Ảnh AI lớn (concept, sheet nhiều frame):**
   ```sh
   python3 AgentScripts/pixel/pixelize.py ArtDirection/Sources/fox-run-v3.png \
       --grid 3x2 --cell 48x48 -o ArtDirection/Work/fox-run-48.png --preview 6
   ```
2. **Sprite PixelLab đã đúng kích thước:** chỉ khóa palette và dọn alpha, không resize.
   ```sh
   python3 AgentScripts/pixel/pixelize.py pixellab-fox-idle.png --native --grid 4x1 -o ArtDirection/Work/fox-idle.png
   ```
3. **Prop/kiến trúc:** giữ viền gốc, cho phép nhiều màu hơn.
   ```sh
   python3 AgentScripts/pixel/pixelize.py ArtDirection/Sources/gate-v2.png \
       --cell 96x96 --outline keep --max-colors 20 -o ArtDirection/Work/gate-96.png
   ```
4. **Kiểm tra trước khi đưa vào `Assets/`:**
   ```sh
   python3 AgentScripts/pixel/pixel_validate.py Assets/ArtSample/Sprites/*.png
   python3 AgentScripts/pixel/pixel_validate.py fox-run.png --cell 48x48 --frames 6
   python3 AgentScripts/pixel/pixel_validate.py grass.png --kind tile --size 16x16
   ```

## Validator kiểm tra gì

| Mức | Mã | Ý nghĩa |
| --- | --- | --- |
| FAIL | `semi_alpha` | Có pixel alpha 1–254 (dấu hiệu ảnh thu nhỏ, không phải pixel art). |
| FAIL | `off_palette` | Màu không thuộc palette khóa. |
| FAIL | `too_many_colors` | Một frame vượt `--max-colors` (mặc định 16). |
| FAIL | `size` / `cell_mismatch` / `empty` | Sai kích thước, sheet không chia hết ô, frame rỗng. |
| WARN | `no_padding` | Sprite chạm mép ô. |
| WARN | `weak_outline` | Dưới 60% pixel mép ngoài là màu tối. |
| WARN | `orphan_pixels` | Pixel lẻ đứng một mình. |
| WARN | `anchor_drift` / `size_drift` | Chân lệch hoặc kích thước dao động giữa các frame. |
| WARN | `grid` / `tile_hole` | Kích thước không là bội số 8; tile có lỗ trong suốt. |

Ảnh hoàn toàn đặc được tự coi là tile (`--kind auto`), nên không bị cảnh báo padding/viền.

## Tham số `pixelize.py` hay dùng

- `--cell WxH`: ô đầu ra. Theo style guide: linh thú nhỏ 32×32, linh thú đuôi dài 48×48, NPC 32×48.
- `--outline recolor|add|keep`: mặc định `recolor` tô mép ngoài thành `#263046`; prop có viền đẹp sẵn thì dùng `keep`.
- `--align bottom|union`: `bottom` cho mọi frame cùng đường chân; `union` giữ chuyển động nhún lên xuống của nguồn.
- `--max-colors N`, `--despeckle 0.16` (0 = tắt), `--dark-boost 1.0` (tăng để giữ nét mảnh khi thu nhỏ mạnh).
- Mỗi lần chạy ghi kèm file `.json` (ô, số frame, pivot chuẩn hóa) để dùng khi cắt sprite trong Unity.

## Giới hạn

- Thu nhỏ tự động không tạo ra thiết kế tốt ở độ phân giải thấp. Linh hồ thu từ ảnh AI xuống 32×32 mất nét mặt; 48×48 giữ được nhưng vẫn cần chỉnh tay. Sprite sản xuất nên sinh ở đúng kích thước (PixelLab) rồi qua `--native`.
- PASS validator nghĩa là **đúng kỹ thuật**, chưa phải **đạt mỹ thuật**. Vẫn đối chiếu checklist trong `ArtDirection/STYLE_GUIDE.md`.
