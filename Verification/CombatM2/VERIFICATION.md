# Kiểm chứng mốc 02 — tinh hạch

Ngày 07/10/2026. Kết quả của scene `PlayerCombatTrial`, kiểm tra sau khi tích hợp đủ logic, cấu hình, input, HUD và hiển thị đồ rơi.

## Kết quả

| Kiểm tra | Bằng chứng |
| --- | --- |
| Compile | `compile.json`: 0 lỗi C#, 0 cảnh báo compile. Console sau ca cuối: 0 lỗi; không đang compile, compilationFailed=false. |
| Domain | `domain-tests.json`: **43/43 đạt**, gồm 25 ca combat và 18 ca tinh hạch. Rơi đúng cấu hình; mỗi đời quái chỉ xét một lần; xác suất hụt không được roll lại; nhiều quái, hồi sinh và sự kiện cũ không phát trùng. |
| Hành trang | Domain kiểm tra gộp ô, chuyển ô, kho đầy, cả gói không đủ chỗ, nhặt lặp, đúng tầm/LOS, người chơi đang gục/ra đòn. `InventoryModule.Add` hiện có nhận đủ gói trước khi đồ dưới đất bị xóa. |
| Input và HUD thật | `python3 AgentScripts/combat_runtime_smoke.py --loot-only`: **15/15 đạt, exit 0**. WASD/J/K, quái chết tạo đồ, E nhặt, nhặt lặp, quái hồi sinh, nút HUD nhận đồ và không đánh kèm; kho đầy giữ nguyên ID/số lượng/visual qua E rồi J. |
| Hình ảnh | `full-bag.png`: kho đã đầy 1/1, tinh hạch còn trên đất, nhãn NHẶT và thông báo giữ đồ. Chữ tiếng Việt đọc được. Icon tinh hạch là hình tạm ghép từ sprite có sẵn. |
| Fixture | Chỉ trong Play Mode: 1 ô ×1. Sau test đã xác nhận host trở lại đúng asset gốc, 4 ô ×3, kho/đồ dưới đất trống. Không lưu fixture vào scene hay asset. |
| WebGL | `build.json`: Succeeded, 0 lỗi, 5 cảnh báo, 66.90 giây. Build trong cùng project đã bao gồm code mốc 02 và thay đổi mỹ thuật từ phiên riêng. |
| HTTP | `http.json`: localhost:8790 trả 200 cho loader/data/framework/wasm; hash khớp bản trên đĩa. |
| Giữ dữ liệu | `preservation.json`: hash bốn file save native, scene ThanDinhPrototype, GameSession, LocalSaveStore và InventoryModule không đổi; Git HEAD không đổi. `git diff --check` đạt. |

Cảnh báo build hiện có: Pipeline không có cấu hình runtime (không nhúng điều khiển Editor vào bản Player); pragma shader TMP cũ; ba thông báo IL2CPP tách hàm TMP lớn để compile. Không có lỗi build. Danh sách từ lần build chung được lưu ở `build-messages.json`.

## Phạm vi và giới hạn

- Domain test dùng RNG điều khiển để kiểm tra 0%, 100%, hai phía ngưỡng và loại/số lượng tinh hạch khác. Mặc định chơi thử là Yêu Quy rơi 100% ×1; thông số này chưa phải cân bằng sản phẩm.
- Các lần chạy `runtime-input.json` trước đó bị ngắt khi Editor được phiên mỹ thuật bật/tắt/đổi trạng thái; chúng không được tính là cả bộ hồi quy đã đạt. Ca soak gục/hồi sinh người chơi không chạy lại trong nhóm `--loot-only`; hành vi đó vẫn có test domain. File nghiệm thu hiện hành của mốc này là `runtime-loot.json`.
- Đã kiểm tra render và input trong Unity Editor. HTTP/build không chứng minh render, input hay hiệu năng trên mọi trình duyệt; chưa có bài điều khiển WebGL tự động hoàn chỉnh cho mốc này.
- Kho và đồ rơi chỉ nằm trong RAM; R hoặc tải lại trang xóa phiên thử. Không có dung hợp, tẩy luyện hoặc migration save.
- Các thay đổi ánh sáng, bóng cây và scene từ phiên Mỹ thuật được giữ; không quy chúng thành thay đổi của mốc logic này.

## Chơi thử và khôi phục

Mở hoặc tải lại http://127.0.0.1:8790/. WASD di chuyển; chuột ngắm; J/chuột trái đánh, đứng gần tinh hạch thì nhặt; E chỉ nhặt; K dùng kỹ năng; R bắt đầu phiên mới. Kho hiện ở góc phải. Chỉnh `Assets/_Project/Content/Combat/CrystalLootConfig.asset` trong Inspector; R lấy cấu hình mới trong Editor, bản web cần build lại.

Bản khôi phục: `Recovery/crystal-m2-20261007-160329/source-before.tgz`, 423 file gồm source chưa được Git theo dõi, khoảng 20.5 MB. Giải nén ra thư mục tạm rồi khôi phục có chọn lọc; không reset cây Git hoặc ghi đè công việc phiên khác. Không commit, push hay deploy trong mốc này.
