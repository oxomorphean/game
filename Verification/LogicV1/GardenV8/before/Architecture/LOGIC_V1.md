# Bản logic 01 — cách chơi và quản lý

Trạng thái: đã chạy vòng chơi local trong Editor và WebGL ngày 05/10/2026. Kiểm thử logic 33/33, kiểm thử HUD 7/7; xem [bằng chứng](../Verification/LogicV1/verification.md). Chưa phải game online hoặc bằng chứng chịu tải 100–300 người.

## Mục tiêu hữu hạn

1. Hai vùng thử nghiệm dùng asset sân vườn, người chơi và tối đa ba linh thú, chuyển điều khiển và tự đi theo nhau.
2. Khám phá lấy tài nguyên; đánh thường/kỹ năng; thu phục mục tiêu bị thương, cho ăn tạo hảo cảm, ấp trứng.
3. Bồi dưỡng bằng nguyên liệu, tiêu hóa dần, căn cốt và tu vi; thú cạn máu tự về nghỉ.
4. Thần Đình 10×10: stack vật phẩm hoặc đất, cây trồng, nước, trang trí theo lớp; thần hồn có thể bước vào.
5. Lưu/khôi phục local, tiến trình offline tối đa 12 giờ, chỉ mất chiến lợi phẩm chuyến đi; ba công thức nghề thử nghiệm.
6. Xưởng chủ dự án tách khỏi HUD người chơi: xem hệ thống, kiểm thử, chỉnh luật/dữ liệu, cấp đồ thử, tua thời gian và nhật ký.

Các mục này tạo một vòng chơi đủ để chủ sản phẩm đánh giá. Không tự mở rộng sang đấu giá, giao dịch nhiều người, PvP, nhiều loài hay 20–30 map trong lượt này.

## Cấu trúc và nguồn dữ liệu

- `Assets/_Project/Scripts/Domain`: dữ liệu, luật, kiểm tra command, vòng mô phỏng C# thuần; assembly không tham chiếu UnityEngine. `GameSession` là chủ sở hữu trạng thái của một phiên; các phần `Realm`, `Nurture`, `Expedition` chia theo chức năng của cùng aggregate.
- `Assets/_Project/Scripts/Client`: Unity input, hiển thị sprite/HUD và dashboard và bộ lưu local. UI gọi command, không tự tính phần thưởng hay tăng trưởng.
- `AgentScripts/LogicBuilder.cs`: tạo scene bằng Unity API và build riêng `WebBuildLogic`; không sửa scene mỹ thuật.
- `AgentScripts/LogicTests.cs`: kiểm tra luật, thất bại, lặp thao tác, lưu và offline qua Unity Pipeline.
- `AgentScripts/HudTests.cs`: kiểm tra modal, D-pad, EventSystem và chuyển xưởng/màn chơi trong Play Mode. `FontAudit.cs` kiểm tra glyph tiếng Việt từ font đóng gói.
- `Architecture/GAMEPLAY_SPEC.md`: luật đã xác nhận và lựa chọn tạm được chủ sản phẩm giao tự quyết.

Trong bản này, authority chạy cục bộ cùng Unity client. Tách assembly và command giúp tái dùng luật khi bổ sung server; chưa có FishNet, API, PostgreSQL, idempotency qua mạng hoặc phân quyền admin online. Bản web chứa quyền quản lý dành cho chủ dự án nên không được công bố như một client online chống gian lận. [Thiết kế online](ONLINE_ARCHITECTURE.md) vẫn là đích triển khai sau.

## Bảo toàn dữ liệu

- Ô đất không chứa stack; ô đồ không trồng cây. Cây trồng và trang trí là hai lớp độc lập trên đất.
- Kiểm tra điều kiện và sức chứa trước khi tiêu nguyên liệu/cấp sản phẩm. Thu hoạch thất bại do đầy kho giữ nguyên cây.
- Khế ước cộng trứng đang ấp không vượt giới hạn; tối đa ba, không gỡ khế ước trong bản thử.
- Cạn máu lập tức thu hồi thú và trả điều khiển cho người; chỉ triệu hồi lại khi hồi đầy.
- Mọi stack có số lượng tổng và số lượng thuộc chuyến đi hiện tại. Cất trong Thần Đình không tự bảo toàn phần mới kiếm; về trại và hoàn tất chuyến đi mới bỏ dấu này. Chế tạo từ tài nguyên còn rủi ro giữ dấu cho sản phẩm.
- Offline áp dụng một lần từ mốc đã lưu đến hiện tại, chặn thời gian âm và giới hạn 12h. Chỉ cây đủ nước và thú đang nghỉ tăng trưởng khi offline; không tự thu hoạch, tu luyện, tiêu hóa hoặc ấp trứng.
- Cấu hình mới chỉ tác động công việc mới. Bữa ăn/cây đang chạy giữ thời gian và phần thưởng lúc bắt đầu.
- Bản lưu sai phiên bản hoặc dữ liệu không hợp lệ phải báo lỗi và giữ nguyên file, không âm thầm xóa. Tạo mới có xác nhận và sao lưu phiên trước.

## Cách mở

Trong Unity, mở `Assets/_Project/Content/Scenes/ThanDinhPrototype.unity` rồi Play. Mẫu mỹ thuật cũ vẫn ở `Assets/ArtSample/SpiritTurtleGarden.unity`.

Build bằng Unity đang mở, dừng Play trước:

```sh
unity command run_script --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo --caller plugin --skill unity-cli --file AgentScripts/LogicBuilder.cs --entry LogicBuilder.CreateScene --format json
unity command run_script --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo --caller plugin --skill unity-cli --file AgentScripts/LogicTests.cs --entry LogicTests.Run --format json
unity command run_script --project-path /Users/nguyenhoangdacdat/dev/nguthu-unity-demo --caller plugin --skill unity-cli --timeout 240 --file AgentScripts/LogicBuilder.cs --entry LogicBuilder.BuildWeb --timeout_ms 180000 --format json
python3 serve_logic.py
```

Bản logic: `http://127.0.0.1:8789/#choi`; dashboard: `http://127.0.0.1:8789/#dashboard`. Khi build lại, mỗi bộ file WebGL có tham số phiên bản riêng để tránh trình duyệt dùng lẫn cache cũ/mới. Chỉ tải lại trang sau khi build hoàn tất. Bản mỹ thuật ở cổng 8788 được giữ riêng. Trên máy tính, chọn Toàn màn hình nếu panel trình duyệt đang hẹp; F2 đổi giữa chơi và xưởng. Giao diện hiện ưu tiên màn hình ngang máy tính, chưa tối ưu cho điện thoại. Unity sinh scene bằng API; không sửa YAML scene bằng tay.

## Chơi vòng đầu

1. Đi sang phải đến linh quy bị thương gần trại, nhấn E và dùng Huyết Khế. Có thể thất bại và tiêu khế; dashboard cho chỉnh tỷ lệ hoặc cấp thêm đồ thử.
2. Tab chuyển giữa người và linh thú. WASD hoặc phím mũi tên di chuyển; người/thú còn lại đi theo. J/Space/E dùng nút chính theo ngữ cảnh: đánh, nhặt, thu phục hoặc qua cổng. K dùng kỹ năng.
3. Trang Linh thú: cho ăn thường hoặc ngọc linh thảo, quan sát tiêu hóa và chỉ số. Nút tua 5 phút chỉ có trong xưởng chủ dự án. Trứng thu thập ở Bờ Linh Hồ trong phiên mới.
4. G nhập Thần Đình, mở trang Thần Đình, chọn ô trống → đặt đất → gieo hạt → tưới → chờ → thu hoạch. Cây/ao cảnh đặt trên đất và giữ nguyên cây trồng.
5. Tu luyện ở trại hoặc Thần Đình, dùng đan dược và đột phá bên trong Thần Đình khi đủ tu vi + 3 linh khoáng. Mở lần lượt một, hai, ba khế ước.
6. Về đèn trại ở vùng đầu và bấm bảo toàn chiến lợi phẩm. Nếu gục, chỉ mất phần nguyên liệu mới còn dấu chuyến đi.

## Dashboard chủ dự án

Mở bằng nút Dashboard chủ dự án trên thanh web, hoặc F2 trong Editor. Màn Chơi thử có HUD riêng. Hai màn dùng chung phiên; xưởng tạm dừng mô phỏng. Xem [quy tắc UX](UX_SPEC.md).

- Tổng quan: sơ đồ chức năng, trạng thái phiên và báo cáo kiểm thử khi build.
- Thần Đình: lưới 100 ô, chọn/di chuyển cả ô, trồng, tưới, thu hoạch, đặt/gỡ trang trí.
- Linh thú: máu, tu vi, căn cốt, chỉ số, thức ăn, điều khiển/triệu hồi, hồi phục và ấp trứng.
- Vật phẩm & nghề: tổng đồ, phần chưa bảo toàn, luyện đan/rèn/luyện phù, sử dụng sản phẩm.
- Luật & dữ liệu: nhịp chơi, dữ liệu thức ăn, chỉ số cơ bản của loài; thay đổi chỉ có hiệu lực khi bấm Áp dụng & lưu. Cấp đồ và mở ba khế ước có nhãn chủ dự án.
- Nhật ký: kết quả gần nhất, những luật tạm chọn, vị trí lưu, tạo mới và khôi phục trước lần tạo mới.

Bản Editor lưu file riêng trong `Application.persistentDataPath/thandinh-v1.json` (đường dẫn cụ thể có trong dashboard). Bản web lưu PlayerPrefs/IndexedDB trên trình duyệt và origin hiện tại. Hai bản không dùng chung tiến trình. Xóa dữ liệu trình duyệt sẽ mất bản web; đây chưa là cloud save.

## Kiểm chứng và rollback

Bằng chứng được ghi trong `Verification/LogicV1/`. Điều kiện hoàn thành: các ca logic đạt, scene chạy không lỗi, WebGL build thành công, kiểm tra thao tác trên bản web thật và UI đọc được. Test tải online/VPS nằm ngoài lượt này.

Mã logic đang ở working tree trên nhánh `codex/thandinh-logic-v1`; chưa commit hoặc push trong lượt này. Mốc mỹ thuật được giữ ở `art-v7-living-garden`. Chỉ cần mở scene mỹ thuật để quay lại hình ảnh cũ; bản build logic và save tách riêng. Không cần xóa hoặc ghi đè dữ liệu cũ để đổi scene.

## Asset tạm cho map logic

`PrototypeMapArt` chỉ dựng hình từ sprite sẵn có: cỏ phẳng, sân/đường lát, hai cây anh đào, bóng, đá, đèn, cổng và hồ. Người chơi và mọi linh quy dùng cùng bộ rùa idle/walk; tên xanh cho người, vàng cho thú liên kết và đỏ cho thú hoang. Vị trí tương tác vẫn lấy từ `GameSession`; hình trang trí không thêm va chạm hoặc thay luật. Bản đồ hai vùng dùng chung bố trí tạm, chưa phải thiết kế phòng cuối.
