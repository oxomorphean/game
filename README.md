# Linh Thú Ký — linh quy trong sân sơn môn

Dự án Unity: `/Users/nguyenhoangdacdat/dev/nguthu-unity-demo`.

Scene hiện hành: `Assets/ArtSample/SpiritTurtleGarden.unity`. Linh quy thay linh hồ, giữ sân gạch/cỏ, cổng sơn môn, hai cây anh đào với bóng đổ, đá và hồ nước. Đây là demo mỹ thuật và di chuyển; chưa có chiến đấu hoặc nuôi thú.

## Chơi thử

Mở scene rồi bấm Play trong Unity. Bấm vào Game View và dùng WASD / phím mũi tên, hoặc bấm xuống sân để đi tới. R đưa linh quy về điểm xuất phát.

Bản Unity WebGL ở `WebBuild/`. Từ thư mục dự án chạy `python3 serve.py`, mở `http://127.0.0.1:8788/`. Server chỉ nghe trên máy này.

## Linh quy và nguồn chỉnh sửa

Nguồn tự vẽ qua Aseprite MCP: `ArtDirection/Handmade/SpiritTurtle/spirit-turtle.aseprite`, có 5 layer và 10 frame. Cùng thư mục có script Lua/Python tái tạo, metadata, GIF và báo cáo validator; không dùng ảnh AI hoặc tải asset có sẵn cho linh quy.

PNG game trong `Assets/ArtSample/Sprites/`:

- `turtle.png`: tư thế đầu, 64×64.
- `turtle-idle.png`: 4 frame ngang, 256×64; thở và chớp mắt, 4 frame/giây.
- `turtle-walk.png`: 6 frame ngang, 384×64; chân bước đối pha, đầu/thân nhún, 8 frame/giây.

Canvas 64×64 giữ cùng kích thước demo; hình hữu dụng nhỏ hơn canvas. Sprite dùng palette khóa của dự án, alpha 0/255, PPU 16, Point, không mipmap hoặc nén, scale 1. Pivot Unity đặt theo pixel `(32,12)`. Built-in Pixel Perfect Camera giữ tham chiếu 384×256. Hướng trái lật từ hướng phải; lên/xuống hiện dùng cùng bộ bước chân, chưa có hình riêng cho bốn hướng.

Bộ môi trường vẫn là nguồn ImageGen thử nghiệm trước đây, được chuẩn hóa bằng Unity. Quy tắc mỹ thuật: [ArtDirection/STYLE_GUIDE.md](ArtDirection/STYLE_GUIDE.md). Kế hoạch gameplay: [game_architecture_plan.md](game_architecture_plan.md).

## Dựng lại bằng plugin Unity

`AgentScripts/ArtSampleBuilder.cs` được gọi tường minh, không tự chạy lúc import. Dừng Play Mode trước khi dựng hoặc build:

```sh
unity command run_script --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo --caller plugin --skill sprite-editor --file AgentScripts/ArtSampleBuilder.cs --entry ArtSampleBuilder.CreateScene --format json
unity command run_script --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo --caller plugin --skill unity-cli --file AgentScripts/ArtSampleBuilder.cs --entry ArtSampleBuilder.BuildWeb --timeout_ms 180000 --format json
```

Controller là `Assets/ArtSample/Scripts/SpiritTurtleController.cs`, dùng Input System mới. Scene và metadata được lưu bằng Unity API. Bằng chứng kiểm chứng lần thay linh quy nằm trong `ArtDirection/ArtSample/verification-v4.md`.
