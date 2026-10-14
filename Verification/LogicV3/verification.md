# Kiểm chứng Logic 03 — 06/10/2026

Bản local đã build bằng Unity 6000.6.4f1 và chạy tại http://127.0.0.1:8789/. Không dùng subagent; không commit/push/deploy. Nguồn trước thay đổi ở [before](before/).

## Kiểm tra đã chạy

| Bộ kiểm tra | Đạt | Lỗi |
| --- | ---: | ---: |
| LogicTests — thu phục, Thần Đình, trồng trọt, tu vi | 33 | 0 |
| LogicV2Tests — AI, đi theo, đồ rơi, tự vệ | 33 | 0 |
| LogicV3Tests — kho nguyên tử, nghề, trang bị, đan/phù, kinh tế | 34 | 0 |
| SaveV3Tests — nâng v1/v2, round trip, save sai và trường hợp biên | 16 | 0 |
| HudTests — phân tách dashboard và điều khiển | 7 | 0 |
| HudV2Tests — linh thú, Thần Đình, thả/nhặt | 19 | 0 |
| HudV3Tests — chế tạo, dùng đan, trang bị, cửa hàng, mốc thưởng | 13 | 0 |
| Tổng | 155 | 0 |

Chạy qua Unity Pipeline `run_script` với các file cùng tên trong AgentScripts. Các ca HUD dùng frame thật; V2/V3 dùng fixture riêng và chặn lưu fixture. Các báo cáo hiện hành và SHA-256 của bản build nằm trong [verification.json](verification.json).

FontAudit: 238 ký tự nguồn và tiếng Việt, 3.808 lần rasterize ở cỡ chữ thường/đậm; không thiếu glyph. Unity Console sau kiểm tra HUD: 0 error, 0 warning. Build cuối Succeeded, 0 error, 48,45 giây, Unity báo 81.786.786 byte; build hiện chưa bật nén tải web. Compiler của script builder có cảnh báo CS0649 cho field báo cáo được JsonUtility điền; không phải lỗi game.

## Kiểm tra trực tiếp

Unity: mở bảng nghề, đọc công thức và trạng thái, xem dashboard; tăng nguyên liệu bản nháp 3→4 rồi bỏ nháp trở lại 3. Ảnh [HUD](workshop-editor.png) và [dashboard](dashboard-editor.png).

WebGL desktop 1280×850: phiên cũ còn một linh quy, đồ và ô đất. Vào Thần Đình, luyện một Tụ khí đan: linh thảo 5→2, hoàn thành sau 8 giây, nhận một đan và +5 KN; mẻ biến mất sau nhận. Bấm Lưu ngay trong dashboard và tải lại trang: vẫn cấp nghề 1/5 KN, không có mẻ đang chạy. Console trình duyệt không thấy runtime error; có cảnh báo deprecation từ Unity về đồng bộ persistentDataPath, chưa cản lưu/tải thực tế.

## Review và giới hạn

Đối chiếu code với snapshot: UI gọi luật qua GameSession; kho lập kế hoạch trước commit; job chụp thông số; trang bị chuyển vật phẩm; mốc thưởng một lần; save versioned. Tự review phát hiện và đã sửa tốc độ follower nhận sai hiệu ứng, JsonUtility tự tạo object khi thiếu trường, và save từ công thức 1 giây ở cấp nghề 3. Có ca hồi quy cho các lỗi này. `git diff --check` không có lỗi.

Đây là bản local hai vùng và một loài để thử vòng chơi. Chưa xác minh tải 100–300 người, chưa có server/anti-cheat online, chưa playtest cân bằng dài hoặc kiểm tra điện thoại. Không xem số lượng test là bằng chứng hoàn thiện sản phẩm thương mại.

Bài học áp dụng trong mã: kiểm tra trường JSON trước JsonUtility và giữ cùng khoảng giá trị giữa cấu hình, job runtime và save. Những kiểm tra này đã có test; không thêm quy trình agent hay công cụ mới.
