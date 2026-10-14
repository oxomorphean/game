# Bản logic 01 — hồ sơ kiểm chứng

Ngày 05/10/2026. Unity 6000.6.4f1, bản local tại `http://127.0.0.1:8789/`. Main agent triển khai và chạy Unity/browser; subagent gpt-6.1-sol max rà font, đọc UX và bổ sung ba ca kiểm thử độc lập. Scene mỹ thuật được giữ riêng.

## Kiểm tra đã chạy

- `LogicTests.Run`: **33 đạt, 0 lỗi**, chi tiết trong [domain-tests.json](domain-tests.json).
- `HudTests.Run` trong Play Mode: **7 đạt, 0 lỗi**, chi tiết trong [hud-tests.json](hud-tests.json). Kiểm tra qua nhiều frame, không chỉ kiểm tra cấu hình tĩnh.
- `FontAudit.Run`: 235 ký tự, 3.760 mẫu glyph ở cỡ/chữ thường/đậm; thiếu 0, fallback 0; font TMP tải được với một atlas. [font-check.json](font-check.json).
- `LogicBuilder.BuildWeb`: **Succeeded**, IL2CPP/WebGL, xem thời gian/dung lượng/lỗi chính xác trong [build.json](build.json). Bản thử chưa nén, khoảng 63,4 MB; chưa phải ngân sách dung lượng sản phẩm.
- Unity Console không có lỗi trong Play Mode sau tích hợp. `simulate_key` A down/up làm X giảm từ 22,3505 xuống 8,5380, Y giữ 3; HUD không chiếm phím di chuyển.
- `git diff --check` đạt; scene và sprite của mẫu mỹ thuật không nằm trong diff.

## Đối chiếu yêu cầu

| Yêu cầu | Bằng chứng đã có |
| --- | --- |
| Người và tối đa 3 thú, chuyển điều khiển và theo nhau | Test cap/control cycling/follower/diagonal; trên web thu phục một thú rồi đổi điều khiển, thấy người đi theo |
| Thu phục bị thương/hảo cảm, trứng | Test range, xác suất, thất bại tiêu khế, cap/trứng đặt chỗ; web nút chính chuyển Thu phục, xác nhận dùng một Huyết Khế |
| Thức ăn, tiêu hóa, tu vi, căn cốt, đột phá | Test chất lượng/bù nền tảng/sức chứa/giữ tổng tu vi; web cho ăn ngọc, tu vi lên khoảng 22, căn cốt 22, công 15,3/thủ 7,5 |
| Cạn máu thu hồi và nghỉ | Test thu hồi, khóa gọi/điều khiển, hồi chậm; offline không tự gọi thú ra |
| Lưới 100 ô và các lớp | Test đồ/đất/cây/trang trí, chuyển nguyên ô, nước, đầy kho; web ô 23 đặt đất/gieo/tưới/đặt ao, thu hoạch 3 ngọc vẫn giữ đất và ao |
| Offline tối đa 12h | Test cap 20h còn 12h, timestamp lặp/đồng hồ lùi, nước giới hạn; tiêu hóa/tu luyện/trứng không tiến khi offline |
| Chỉ mất chiến lợi phẩm mới | Test đồ gốc được giữ, về trại bảo toàn, Thần Đình không xóa dấu, chế tạo giữ rủi ro chuyến đi |
| Dashboard của chủ dự án | Web tổng quan/số liệu, chỉnh thu phục 85→71 và Áp dụng, tải lại vẫn 71; đã đưa lại về mức mặc định sau thử. Test owner tạm dừng mô phỏng |
| HUD người chơi riêng | Canvas/uGUI riêng: HP/tu vi/đội, di chuyển, nút chính theo ngữ cảnh, kỹ năng, Thần Đình, linh thú và đồ; không nút cấp đồ/tua giờ/chỉnh luật |
| Lưu/khôi phục | Test roundtrip/layers/timers/rules và từ chối bản sai. Web tải lại giữ thú/đất/đồ; tạo mới có xác nhận xuống 0 thú, Khôi phục trả đúng một thú, chỉ số và 5 ngọc |
| Tiếng Việt | Audit TTF trực tiếp và TMP; trên web đọc được tên, dấu tiếng Việt, modal và dashboard |
| Bảo toàn mỹ thuật | Mã và scene logic dưới `_Project`, build riêng `WebBuildLogic`, save riêng; không sửa `Assets/ArtSample` |

## Hai màn thực tế

[HUD người chơi](gameplay-final.jpg) và [dashboard chủ dự án](dashboard-final.jpg) chụp trực tiếp từ bản web. Ảnh dùng khối màu theo phạm vi logic, không phải mẫu mỹ thuật mới.

## Lỗi đã phát hiện và xử lý

- Unity JsonUtility ghi một bữa ăn null thành object toàn giá trị zero. Chuẩn hóa riêng trường hợp đó trước validation, vẫn từ chối bữa ăn sai; thêm ca regression cho thú không có bữa ăn rồi save/reload.
- Trình duyệt có thể dùng lẫn framework/WASM/data từ cache sau build. Builder gắn cùng phiên bản mới vào cả bốn URL; tải mới giữ tiến trình và không tái diễn lỗi khởi động.
- Font hệ thống thiếu glyph tiếng Việt được thay bằng Noto Sans đóng gói cùng OFL; HUD dùng TMP atlas riêng, dashboard dùng cùng TTF.

## Giới hạn còn lại

Đây là prototype local cho chủ dự án. Chưa kiểm thử multiplayer, tải 100–300 người, server authority, cloud save hoặc thiết bị di động thật. Hai vùng mẫu kiểm chứng vòng chơi, không đại diện kích thước thế giới online cuối cùng. UI owner được tách theo cách sử dụng; chưa là cơ chế phân quyền qua mạng. Kiểm thử offline/die/ba khế ước/hảo cảm/trứng/đột phá có bằng chứng tự động; không tuyên bố đã bấm thử mọi trường hợp đó trên trình duyệt.

## Rút kinh nghiệm

Bộ logic C# tách khỏi hiển thị giúp chạy các luật độc lập. Roundtrip với trạng thái có thú nhưng không có bữa ăn là ca thiếu ban đầu; đã thêm regression. Build thành công chưa đủ: tải lại bản web và khôi phục dữ liệu cần nằm trong lần nghiệm thu. Bộ URL có phiên bản là cơ chế tự động ngăn dùng lẫn cache; không cần thêm quy tắc rộng vào AGENTS.md.
