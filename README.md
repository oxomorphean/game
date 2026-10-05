# Linh Thú Ký — Huyền Giáp Quy trong sân sơn môn

Dự án Unity: `/Users/nguyenhoangdacdat/dev/nguthu-unity-demo`.

Scene hiện hành: `Assets/ArtSample/SpiritTurtleGarden.unity`. Môi trường v7 giữ cổng nhiều chi tiết, hai cây anh đào và bóng cây, đá và hồ của v4; thêm sân tile, cỏ phẳng xanh mướt với một mảng đậm nhẹ gần hồ, hoa thưa, hai đèn đá, cánh hoa rơi và gợn nước. Huyền Giáp Quy có mai đen và hoa văn vàng kim thay linh quy xanh ngọc; đây là mẫu mỹ thuật và di chuyển, chưa có chiến đấu hoặc nuôi thú.

## Chơi thử

Mở scene rồi bấm Play trong Unity. Bấm vào Game View và dùng WASD / phím mũi tên, hoặc bấm xuống sân để đi tới. R đưa linh thú về điểm xuất phát.

Bản Unity WebGL ở `WebBuild/`. Từ thư mục dự án chạy `python3 serve.py`, mở `http://127.0.0.1:8788/`. Server chỉ nghe trên máy này.

## Nguồn mỹ thuật

Huyền Giáp Quy được tự vẽ qua Aseprite MCP. Nguồn có layer, animation, script tái tạo và bằng chứng nằm trong `ArtDirection/Handmade/HuyenGiapQuy/`. Bộ PNG game giữ tên để ổn định tham chiếu Unity:

- `Assets/ArtSample/Sprites/turtle.png`: 64×64.
- `turtle-idle.png`: 4 frame ngang, 256×64, 4 frame/giây.
- `turtle-walk.png`: 6 frame ngang, 384×64, 8 frame/giây.

Các frame dùng palette khóa, alpha 0/255, cùng canvas và pivot `(32,12)`, PPU 16, Point, không mipmap/nén, scale 1. Pixel Perfect Camera Built-in tham chiếu 384×256. Trái lật từ phải; lên/xuống hiện dùng cùng bộ bước chân, chưa có hình riêng cho bốn hướng.

Nguồn linh quy xanh cũ vẫn ở `ArtDirection/Handmade/SpiritTurtle/`. Cổng, cây, đá và hồ giữ nguồn ImageGen thử nghiệm trước đây trong `ArtDirection/Sources/`. Tile, chi tiết và hiệu ứng mới được tự vẽ qua Aseprite MCP; nguồn editable ở `ArtDirection/Handmade/GardenV7/`. Cỏ có nét vụn rải đều đã bị loại và chỉ giữ ngoài Assets làm lịch sử. Palette hiện hành v2 thêm một bậc cỏ tối nhẹ, giữ toàn bộ màu v1. [Quy tắc mỹ thuật](ArtDirection/STYLE_GUIDE.md) giữ mức chi tiết v4 và yêu cầu mai đen/vàng mới. [Kế hoạch gameplay](game_architecture_plan.md) giữ nguyên.

## Dựng lại bằng Unity Pipeline/CLI

Dừng Play Mode trước khi dựng hoặc build. `AgentScripts/ArtSampleBuilder.cs` chỉ chạy khi gọi tường minh:

```sh
unity command run_script --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo --caller plugin --skill sprite-editor --file AgentScripts/ArtSampleBuilder.cs --entry ArtSampleBuilder.CreateScene --format json
unity command run_script --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo --caller plugin --skill unity-cli --file AgentScripts/ArtSampleBuilder.cs --entry ArtSampleBuilder.BuildWeb --timeout_ms 180000 --timeout 240 --format json
```

Controller là `Assets/ArtSample/Scripts/SpiritTurtleController.cs`, không thay logic di chuyển. Scene và metadata được lưu bằng Unity API. Bằng chứng v6 giữ trong `ArtDirection/ArtSample/verification-v6.md`; cỏ v7 đã qua reviewer độc lập, xem `ArtDirection/ArtSample/verification-v7.md`, `aesthetic-review-v7.md` và ảnh Unity cuối.

## Phiên bản

V4: `main`, tag `art-v4-before-unify`. V5: `codex/art-style-sample`, tag `art-v5-candidate`. V6: `codex/huyen-giap-quy`, tag `art-v6-huyen-giap-quy`. V7: `codex/living-garden-v7`, tag `art-v7-living-garden`. Chỉ lưu Git cục bộ. Xem [ROLLBACK.md](ROLLBACK.md) để đổi bản và dựng lại WebGL; `WebBuild/` là đầu ra build, không nằm trong Git.
