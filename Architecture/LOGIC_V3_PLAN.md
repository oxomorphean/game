> Lịch sử trước 12/10/2026. Bản hiện hành chỉ thử mỹ thuật; gameplay đã được chuyển ra khỏi source đang dùng. Xem [kiến trúc hiện hành](CODE_ARCHITECTURE.md).

# Logic 03 — vòng chơi chế tạo và bồi dưỡng

Phạm vi được giao tự quyết: hoàn thiện bản local bằng nguyên liệu/công thức/đan/phù/trang bị và mục tiêu hành trình. Giữ mỹ thuật, hai vùng, luật khế ước và mất nguyên liệu chuyến đi. Không thêm server, tài khoản, chợ người chơi hay công cụ mới. Parser tái sử dụng Newtonsoft đã có trong project.

## Hiện trạng và kiến trúc

Hai assembly hiện có là Domain C# thuần và Client Unity. GameSession đang là lớp partial lớn; file riêng chưa tạo ra module độc lập. Tiến tới modular monolith trong cùng repository: Domain/Inventory, Crafting, Equipment, Items, Progression là các dịch vụ nhận dependency tường minh; GameSession điều phối phiên và vòng tick. Domain không tham chiếu Unity. Client chỉ gọi hành động và trình bày; lưu local là adapter riêng, save có phiên bản. Chưa dựng abstract networking hoặc microservice khi chưa cần.

## Kết quả hữu hạn

1. Kho đồ thực hiện giao dịch nguyên tử, dùng chung cho hệ mới và hệ cũ.
2. Ba nghề có cấp 1–3, công thức nhiều nguyên liệu, mẻ tối đa 10, một công việc/nghề, thời gian và nhận sản phẩm thủ công. Đầy kho giữ sản phẩm trong công việc.
3. Đan hồi máu/tu vi/căn cốt; thức ăn tốt cho thú. Phù phòng ngự/tăng tốc/tăng công/tấn công/hồi trại. Hiệu ứng có thời hạn, cooldown và mục tiêu rõ ràng.
4. Vũ khí và giáp có trang bị/tháo/đổi, không nhân chỉ số sau tải lại; giữ dấu chuyến đi.
5. Cửa hàng NPC tại trại mua vật tư, bán đồ đã bảo toàn; mốc hành trình nhận thưởng đúng một lần.
6. HUD người chơi có các tab riêng; dashboard chủ dự án xem/chỉnh công thức và hiệu ứng. Có hướng dẫn vòng chơi.

## Invariant và failure cases

Không mất nguyên liệu khi lệnh bị từ chối; không nhận sản phẩm/phần thưởng hai lần; không tự xóa đồ khi kho đầy; sản phẩm từ đồ chuyến đi giữ dấu. Không bán đồ chuyến đi lấy tiền an toàn. Trang bị không phải biến cộng dồn; đổi đồ trả lại món cũ hoặc từ chối trọn thao tác. Công việc giữ thông số lúc bắt đầu. Dữ liệu đầu vào/save phải hữu hạn, ID hợp lệ, số lượng/cooldown có trần. V1/V2 nâng tuần tự lên V3, giữ bản gốc trước lần ghi mới; save hỏng báo lỗi, không tự tạo lại. Offline tối đa 12h, chỉ cây/thú nghỉ/công việc chế tạo; không giả lập chiến đấu.

## Kiểm chứng và khôi phục

Theo yêu cầu chủ dự án: viết xong tất cả logic/UI/tài liệu rồi chạy kiểm tra. Kiểm tra domain, giao dịch kho, nghề/hiệu ứng/trang bị, migration v1/v2/v3 và callback HUD; hồi quy logic 01/02; font, build WebGL và thử UI thật. Snapshot trước thay đổi tại Verification/LogicV3/before. Không commit/push/deploy. Phục hồi source từ snapshot cùng save .before-logic-v3 khi cần quay lại.
