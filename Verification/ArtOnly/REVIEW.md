# Rà soát kiến trúc và bản mỹ thuật — 12/10/2026

## Kết luận

Bản chạy đã thu gọn thành mẫu duyệt map, nhân vật và animation theo yêu cầu tạm bỏ gameplay. Không còn combat, quái, chỉ số, công pháp, lên cấp, kho, nghề, rơi/nhặt hoặc persistence trong source runtime đang biên dịch. Các clip/pose hình ảnh cũ vẫn được giữ làm dữ liệu mỹ thuật; không có luật sát thương hoặc input đánh.

Git đang ở mốc cũ `5ec5fcf`, phần lớn source mới chưa được theo dõi. Vì vậy không thể quy toàn bộ vấn đề cho Gemini hoặc dùng diff với HEAD để xác định riêng thay đổi của Gemini.

## Phát hiện và xử lý

- **P2 — preview phụ thuộc hệ gameplay:** các renderer/controller nhân vật nằm chung nhóm Combat, mẫu thay đồ dùng CombatArena, nhiều thế hệ GameSession/Adventure/Combat cùng tồn tại. Đã tách Navigation C# thuần, Preview input/camera/HUD và Characters. Giữ môi trường cùng art hiện có. Source runtime giảm từ 88 xuống 23 file; không thêm framework hoặc dependency.
- **P2 — bỏ sót va chạm hình tròn:** phần kiểm tra nhanh dùng HalfSize.Y trong khi phép kiểm tra vòng tròn dùng bán kính HalfSize.X. Khi hai thành phần khác nhau, nhân vật/đoạn thẳng có thể lọt qua phần trên/dưới hình tròn. `WorldGeometry` hiện thống nhất bán kính ở cả hai trục; test tái hiện với kích thước (1, 0.2) đã qua.
- **P2 — chạy tại chỗ khi bị chặn:** `ModularHeroTrial` chọn Run từ input, dù va chạm không cho vị trí thay đổi. Đã dùng độ dịch chuyển thực tế; nút Chạy riêng của mẫu art vẫn cho xem clip tại chỗ có chủ ý. Main preview cũng truyền displacement đã xử lý va chạm vào renderer.
- **Entry scene không còn hợp lệ:** BuildSettings cũ tham chiếu scene SpiritTurtleGarden đã bị gỡ trước task. Đã tạo `ArtPreview` bằng Unity API và chọn nó làm scene duy nhất của bản build.

## Mỹ thuật và animation còn giới hạn

- Artwork, prefab, shader của 150 file được bảo vệ giữ nguyên SHA-256. Map chính giữ nền/đường mesh có thể chỉnh, 21 prop, footprint, ánh sáng và bóng; không quay về ảnh nền ghép toàn cảnh.
- Nhân vật chính có trước/lưng/nghiêng và phản chiếu, ánh xạ tám hướng di chuyển. Đây chưa phải tám góc vẽ độc lập.
- Mẫu `HeroModularTrial` đổi tóc/áo/vũ khí được, nhưng mới có mặt trước và phản chiếu. Chưa dùng mẫu này thay nhân vật nhiều hướng.
- `ModularHeroRig.RigSprite` vẫn tính weight theo chiều cao cục bộ của từng mảnh. Khi tạo thêm quần áo, vùng ráp có thể biến dạng khác nhau nếu crop và tỷ lệ nguồn khác nhau. Đây là rủi ro kỹ thuật từ code; lượt này chưa chứng minh mọi bộ đồ/mọi frame hết lỗi ráp. Hướng xử lý tiếp là chuẩn hóa canvas, bind coordinates và weight chung trước khi mở rộng wardrobe; chưa đổi nguồn art trong task này.

## Bằng chứng kiểm tra

- Compile Unity thành công; không có lỗi C#.
- 18 kiểm tra focused qua: chuẩn hóa đường chéo, idle/reset, input không hợp lệ, va chạm vòng tròn, trượt quanh đá, không xuyên tường mỏng, biên map, tám hướng, namespace runtime, sprite Idle/Run, ba scene không missing script và giữ map/prop. Xem `checks.json`.
- WebGL build cuối đạt **Succeeded, 0 errors, 1 warning**, 105452205 byte theo BuildReport. Warning duy nhất: Pipeline không bật điều khiển từ xa trong Player vì không có RuntimePipelineConfig; không phải lỗi game. Build đầu từng ghi lỗi timeout IPC, đã build lại bằng menu Unity. Xem `build.json`.
- Chơi thật trong Editor: main preview di chuyển/đổi hướng; mẫu modular chạy và đổi áo Trắng ngà/Xích kim, tóc 1/2. Trạng thái runtime xác nhận hair=1, coat=3, clothes=true, motion=Run.
- WebGL tại http://127.0.0.1:8791/: tải được, WASD làm thay đổi vị trí và hướng hình, R về điểm đầu, bấm sân đi đến điểm, dừng lại tại đích. Console trình duyệt không ghi error/warn trong lượt kiểm tra. Ảnh `web-back.jpg`, `web-current.jpg`.
- `git diff --check` qua. Source/art audit ở `source-audit.json`.

Chưa đo FPS trên máy yếu/mobile, chưa kiểm tra mọi contour/mọi frame, chưa kiểm chứng multiplayer hoặc map lớn. Bản này là preview local; không tuyên bố sẵn sàng MMORPG.

## Chạy và khôi phục

Mở http://127.0.0.1:8791/ hoặc scene `Assets/_Project/Content/Scenes/ArtPreview.unity` trong Unity. WASD/mũi tên để đi; bấm nền để chọn điểm; R về điểm xuất phát. Server hiện dùng `python3 serve_adventure.py`.

Archive `Recovery/ArtOnly-20261012/source-before.tgz` khoảng 69 MiB, gồm cả source chưa được Git theo dõi và manifest hash. File gameplay được chuyển vào `retired-source`, không còn nằm trong Assets. Save cũ không bị sửa. Không commit, push hoặc deploy.
