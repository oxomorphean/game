# Kiểm chứng mẫu mỹ thuật v3

Scene: `Assets/ArtSample/SpiritFoxGarden.unity`. Dựng và build bằng plugin Unity CLI trong Editor 6000.6.4f1 đang mở, không sửa YAML scene/asset.

## Kết quả đối chiếu

- Linh hồ: runtime probe quan sát đủ `fox-run-0` đến `fox-run-5` khi di chuyển, sau đó `IsMoving=false` và sprite đứng `fox`. Bằng chứng: `animation-verification-v3.json`. Probe kích hoạt điểm đến trong runtime bằng plugin; kiểm tra thao tác người chơi thực hiện riêng trên bản web.
- Sáu frame đều 64×64, PPU 16, pivot (44,11), Point, không mipmap và không nén. Controller phát 10 frame/giây khi có chuyển động; dừng thì trở về ảnh đứng.
- Hai asset cây cùng có mặt: `Sprites/tree.png` được giữ nguyên; `Sprites/tree-puffy.png` có tán đầy hơn. Hai mesh bóng đổ riêng; nhìn thấy trong Game View và bản web.
- Gạch có hai biến thể mới `stone-moss` và `stone-grass`; hồ nước riêng `Sprites/pond.png` ở phía phải sân.
- Web build: `Succeeded`, 0 lỗi, 29.004 giây. Kết quả gốc ở `build-result.json`; phiên bản trang là 0.3.
- Trình duyệt tại `http://127.0.0.1:8788/`: tải xong, bấm sân cho hồ di chuyển, thấy tư thế chạy, phím A lật hướng và phím R trở lại điểm xuất phát. Console tab kiểm tra không có error/warn. Ảnh: `web-preview-v3.png`.
- Unity vẫn mở scene trong Play Mode, không có lỗi biên dịch hoặc console error trong kiểm tra cuối. Ảnh cửa sổ thực tế: `unity-preview-v3.png`.

## Giới hạn hiện tại

Hướng trái dùng lật ngang; đi lên/xuống dùng cùng bộ chạy, chưa có sprite riêng cho bốn hướng. Hồ nước và tán cây là asset tĩnh. Ảnh nguồn AI được chuẩn hóa bằng Unity, chưa chỉnh từng pixel hoặc khóa bảng màu để sản xuất.

Prompt và đường dẫn nguồn được lưu trong `generation-prompts-v3.json`. Bộ animation giữ canvas/pivot chung để thay ảnh về sau mà không đổi controller.
