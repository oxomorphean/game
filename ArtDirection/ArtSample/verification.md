# Kiểm chứng mẫu mới — 2026-10-04

## Phạm vi đã dựng

Một linh hồ chibi một đuôi, cổng mái xanh chàm, cây hoa hồng và một sprite đá tái sử dụng ở hai vị trí. Các prop là SpriteRenderer riêng, không gộp thành tranh nền. Sân nhỏ là Tilemap grass/stone 16×16; không mở rộng map hoặc thêm logic chiến đấu, nuôi thú.

Scene: `Assets/ArtSample/SpiritFoxGarden.unity`. Controller: `Assets/ArtSample/Scripts/SpiritFoxController.cs`. Builder tường minh: `AgentScripts/ArtSampleBuilder.cs`.

## Kiểm tra đã thực hiện

- Script dựng scene biên dịch và chạy thành công qua Unity plugin `run_script`; không sửa YAML.
- Sprite Editor Data Provider được kiểm tra quyền EditPivot trước khi áp dụng pivot.
- Fox 64×64, gate 128×128, tree 96×128, rock 32×32; toàn bộ sprite game PPU 16, Point, không mipmap, không nén; SpriteRenderer scale 1.
- Camera Built-in Pixel Perfect 384×256, assetsPPU 16, orthographic, không HDR/MSAA; nền dùng lưới vuông và khoảng trống trước cổng.
- Unity Play Mode: click thật đưa pet từ (0,-3) tới (1.8125,-1.4688). Scene mới hiển thị trực tiếp trong Editor.
- Web: trang localhost tải xong và hiển thị đúng bộ asset. Click di chuyển, phím A quay sang trái, nút reset và phím R đưa về vị trí đầu được kiểm tra qua thao tác thật và ảnh chụp. Browser console không có error/warn ở lượt kiểm tra.
- Web build result: Succeeded, 0 lỗi, 50.837 giây; Unity báo tổng kích thước build 35,018,269 byte. `build-result.json` lưu số liệu máy đo.
- 27 file của hai demo cũ đã chuyển khỏi Assets vào Thùng rác bằng API Unity. Hai thư mục build cũ, tài liệu ArtTrial cũ và ảnh thử cũ cũng chuyển vào Thùng rác. STYLE_GUIDE, ảnh tham chiếu, kế hoạch kiến trúc và cấu hình được giữ lại.

Ảnh bằng chứng: `unity-preview.png` và `web-preview.jpg`.

## Giới hạn của mẫu

Linh hồ hiện chỉ có một tư thế; di chuyển dùng sprite tĩnh và lật ngang. Chưa có animation nhiều frame/hướng. Không tuyên bố đã kiểm tra toàn bộ WASD/mũi tên trên mọi trình duyệt hoặc thiết bị.

Ảnh gốc tạo mới bằng ImageGen rồi Unity lấy mẫu nearest-neighbor, giữ tỷ lệ và thêm khoảng trong suốt để về lưới dùng trong game. Bản này chốt bố cục, dáng và màu theo hướng tham chiếu. Chưa làm chỉnh từng pixel hoặc khóa bảng màu sản xuất 24–40 màu; việc giảm ảnh tự động không thay cho công đoạn đó.

Renderer được giữ Built-in theo dự án hiện có; Input System 1.20.0 và 2D Sprite 1.0.0 thêm từ registry Unity chính thức. Bản thử dùng WebGL, chỉ phục vụ localhost, chưa phát hành lên mạng.
