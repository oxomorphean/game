# Mốc 03 — kiểm chứng ngày 07/10/2026

Đã triển khai nền sáu chỉ số, nguồn modifier, học/chọn bốn công pháp, Hoành Tảo có chi phí, hai bộ pháp, HUD và input. Cấu hình thử và interface: [TECHNIQUES_M3.md](../../Architecture/TECHNIQUES_M3.md).

- `CombatTests.Run`: **72/72** domain behavior cases. Gồm 43 hồi quy M01/M02 và 29 chỉ số/công pháp/chi phí/khóa/CD/va chạm/gục/hồi sinh.
- `combat_runtime_smoke.py`: **27/27** ca chạy Unity qua phím/pointer. Sau sửa màu HUD, `--techniques-only`: **10/10**. Lần đầu phím J bị hướng chuột sai do Editor không foreground; đưa Unity lên trước rồi chạy lại trọn bộ đạt, không đổi domain để làm test qua.
- Compile ban đầu 0 lỗi/cảnh báo. Recompile sau đổi màu/scaler có lúc CLI mất kết nối khi domain reload; `recompile_status` báo completed, failed=false. WebGL cuối thành công 0 lỗi, 4 warning: Pipeline runtime không bật và 3 cảnh báo kích thước hàm TMP từ IL2CPP; không có exception gameplay.
- Bản WebGL thật tại 8790 đã reload, thấy M03, R reset/C catalogue, click học/chọn nền và Khinh Vân; Space bật làm tốc độ từ 3.3 lên 6 và tiêu hao linh lực. Console browser `[]` warn/error. Ảnh `web-techniques.jpg`; Game View `catalogue.png`.
- Thử viewport 1280×720 và tab hẹp; sửa CanvasScaler Expand để panel không chồng. Tab dọc hẹp chữ nhỏ, V1 nhắm desktop ngang, chưa UI mobile riêng.
- 88 source scene/prefab/art/module kho/persistence được đối chiếu và 4 save native giữ hash nguyên (`preservation.json`). Art ambient mới được giữ và tích hợp từ phiên Mỹ thuật.
- Build là local, chưa commit/push/deploy. Không có multiplayer hoặc số đo hiệu năng V1; các phần đó ngoài M03/đang nằm trong mục tiêu V1 mới.
