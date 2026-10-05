# Grass clean v7

Nền cỏ xanh phẳng #83B865, hai mảng bóng đất #79AA5E; không chấm, tick, highlight, dither hoặc pixel rời. Native Aseprite Lua, chưa nhập Unity.

- grass-clean.png: opaque 16×16, PPU16, pivot(8,8).
- grass-shade-a.png / grass-shade-b.png: canvas64×32, transparent, footprint40×14 / 50×18, pivot(32,16). Chỉ dùng 1–2 mảng lớn, đặt lệch mép hoặc gốc cây; không rải theo lưới.
- Ba file .aseprite editable riêng và create-grass-clean.lua tái tạo.
- linhthu-v2.hex/gpl và swatch.png: 34 màu; giữ nguyên33 màu v1, thêm đúng #79AA5E cuối. Palettev1 của project không được sửa.
- preview1x.png / preview4x.png, statistics.json, pixel-validation.json và source-metadata.json là bằng chứng.

Import đề xuất: Point, mipmaps Off, Compression None, PPU16. Gói này chỉ ở /private/tmp; không thay project hay gói ground trước.
